using System;
using System.Collections.Generic;
using Mandate.Domain;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangStress_ConstructionRanksPressureAndRejectsProtectedCandidates()
        {
            var pressure = new LuoyangStressPressureState
            {
                HousingPressureBasisPoints = 8_000,
                FoodPressureBasisPoints = 4_000,
                EmploymentPressureBasisPoints = 2_000
            };
            var context = new LuoyangStressConstructionContext
            {
                AvailableTreasury = 1_000,
                AvailableMaterials = 1_000,
                AvailableConstructionWorkers = 50,
                AvailableDevelopableCells = 20
            };
            var candidates = new[]
            {
                Candidate("candidate.housing", "pressure.housing", false),
                Candidate("candidate.food", "pressure.food", false),
                Candidate("candidate.historical", "pressure.housing", true)
            };
            var ranked = LuoyangStressConstructionRules.Rank(pressure, context, candidates, 3);
            Assert.That(ranked[0].CandidateDefinitionId, Is.EqualTo("candidate.housing"));
            Assert.That(ranked[0].IsFeasible, Is.True);
            Assert.That(ranked[2].IsFeasible, Is.False);
            Assert.That(ranked[2].Reasons, Contains.Item("historical_definition_not_a_stress_expansion_candidate"));
        }

        [Test]
        public void LuoyangStress_ConstructionMustTraverseRealLifecycle()
        {
            var project = new LuoyangStressConstructionProjectState
            {
                Id = "construction.one", FacilityId = "facility.one", OwnerId = "owner",
                ControllerId = "owner", CellId64 = 100, CreatedDay = 2,
                Status = StressConstructionStatus.Planned
            };
            Assert.Throws<InvalidOperationException>(() => project.Complete(3));
            project.Approve(2);
            project.Start(3);
            project.Complete(5);
            Assert.That(project.Status, Is.EqualTo(StressConstructionStatus.Completed));
            Assert.That(project.CompletedDay, Is.EqualTo(5));
        }

        [Test]
        public void LuoyangStress_WorkerIndexDoesNotScanWholePopulation()
        {
            var index = new LuoyangStressWorkerIndex();
            for (var number = 0; number < 100; number++)
            {
                index.Add(Person("person." + number, number % 2 == 0 ? "profession.craft" : "profession.trade", number));
            }
            var workers = index.FindByProfession("profession.craft", 5);
            Assert.That(index.Count, Is.EqualTo(100));
            Assert.That(workers.Count, Is.EqualTo(5));
            for (var indexOfWorker = 1; indexOfWorker < workers.Count; indexOfWorker++)
                Assert.That(workers[indexOfWorker - 1].PrimarySkillBasisPoints,
                    Is.GreaterThanOrEqualTo(workers[indexOfWorker].PrimarySkillBasisPoints));
            foreach (var worker in workers)
                Assert.That(worker.ProfessionId, Is.EqualTo("profession.craft"));
        }

        [Test]
        public void LuoyangStress_PermanentPersonValidationRejectsMergeOrDuplicate()
        {
            var person = Person("person.permanent", "profession.craft", 3000);
            LuoyangStressPopulationRules.ValidateCounts(1, 0, 1);
            Assert.Throws<InvalidOperationException>(() =>
                LuoyangStressPopulationRules.ValidateUniqueAssignments(new[] { person, person }));
            Assert.Throws<InvalidOperationException>(() => LuoyangStressPopulationRules.ValidateCounts(10, 8, 1));
        }

        [Test]
        public void LuoyangStress_HousingFacilityGrowthDestructionAndMilitaryTransferPreservePerson()
        {
            var civilianDefinition = new FacilityDefinitionState
            {
                Id = "facility.residential", ResidentialCapacityPersons = 1,
                AllowedResidentTypeIds = new List<string> { FacilityPopulationTypeIds.Civilian }
            };
            var barracksDefinition = new FacilityDefinitionState
            {
                Id = "facility.barracks", ResidentialCapacityPersons = 1,
                AllowedResidentTypeIds = new List<string> { FacilityPopulationTypeIds.ActiveMilitary }
            };
            var firstHome = new FacilityState { Id = "home.one", DefinitionId = civilianDefinition.Id };
            var newHome = new FacilityState { Id = "home.two", DefinitionId = civilianDefinition.Id };
            var barracks = new FacilityState { Id = "barracks.one", DefinitionId = barracksDefinition.Id };
            var person = new FacilityPersonFact
            {
                PersonId = "person.permanent", IsAlive = true,
                PopulationTypeId = FacilityPopulationTypeIds.Civilian
            };
            var existingResident = new FacilityPersonFact
            {
                PersonId = "person.existing", IsAlive = true,
                PopulationTypeId = FacilityPopulationTypeIds.Civilian
            };
            Assert.That(FacilityHousingRules.TryAssign(civilianDefinition, firstHome, existingResident, out _), Is.True);
            Assert.That(FacilityHousingRules.TryAssign(civilianDefinition, firstHome, person, out _), Is.False,
                "full pre-existing housing leaves the permanent Person unhoused");
            Assert.That(FacilityHousingRules.TryAssign(civilianDefinition, newHome, person, out _), Is.True,
                "a newly completed real Facility creates a real additional housing slot");
            Assert.That(FacilityHousingRules.TryRemove(newHome, person.PersonId, out _), Is.True,
                "destroying/invalidating that residence removes capacity without deleting Person");
            Assert.That(person.IsAlive, Is.True);
            person.IsActiveMilitary = true;
            person.PopulationTypeId = FacilityPopulationTypeIds.ActiveMilitary;
            Assert.That(FacilityHousingRules.TryAssign(barracksDefinition, barracks, person, out _), Is.True);
            Assert.That(FacilityHousingRules.TryRemove(barracks, person.PersonId, out _), Is.True);
            person.IsActiveMilitary = false;
            person.PopulationTypeId = FacilityPopulationTypeIds.Civilian;
            Assert.That(FacilityHousingRules.TryAssign(civilianDefinition, firstHome, person, out _), Is.False,
                "retired soldier becomes unhoused when civilian housing is full");
            Assert.That(person.IsAlive, Is.True, "enlistment and retirement move housing, never recreate Person");
        }

        private static LuoyangStressConstructionCandidateDefinition Candidate(string id, string pressure, bool historical)
        {
            return new LuoyangStressConstructionCandidateDefinition
            {
                Id = id, FacilityDefinitionId = "facility." + id, CategoryId = "test",
                PrimaryPressureId = pressure, MinimumPressureBasisPoints = 1_000,
                PressureWeightBasisPoints = 10_000, TreasuryCost = 10, MaterialCost = 10,
                ConstructionWorkerDays = 10, CellCount = 1, HistoricalProtected = historical
            };
        }

        private static LuoyangStressPersonState Person(string id, string profession, int skill)
        {
            return new LuoyangStressPersonState
            {
                PersonId = id, HouseholdId = "household.one", Age = 30, SexId = "sex.male",
                CurrentActivityId = "activity.available", ProfessionId = profession,
                PrimarySkillId = "skill.basic", PrimarySkillBasisPoints = skill,
                IsAlive = true, IsLaborEligible = true, CurrentCellId64 = 1, OriginCellId64 = 1,
                AdministrativeRelationId = "administration.luoyang", DailyConsumptionBasisPoints = 10_000,
                SimulationTier = StressSimulationTier.LowFrequency
            };
        }
    }
}
