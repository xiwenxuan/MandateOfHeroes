using System;
using System.Collections.Generic;
using Mandate.Domain;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void Luoyang184_HousingCountsPersonsAndBarracksRejectsCivilian()
        {
            var definition = new FacilityDefinitionState
            {
                Id = "facility.historical.barracks",
                ResidentialCapacityPersons = 1,
                AllowedResidentTypeIds = new List<string> { FacilityPopulationTypeIds.ActiveMilitary }
            };
            var facility = new FacilityState { Id = "facility.barracks", DefinitionId = definition.Id };
            var civilian = new FacilityPersonFact
            {
                PersonId = "person.civilian", IsAlive = true, PopulationTypeId = FacilityPopulationTypeIds.Civilian
            };
            var soldier = new FacilityPersonFact
            {
                PersonId = "person.soldier", IsAlive = true, IsActiveMilitary = true,
                PopulationTypeId = FacilityPopulationTypeIds.ActiveMilitary
            };
            Assert.That(FacilityHousingRules.TryAssign(definition, facility, civilian, out _), Is.False);
            Assert.That(FacilityHousingRules.TryAssign(definition, facility, soldier, out _), Is.True);
            Assert.That(facility.ResidentPersonIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void Luoyang184_RealJobsRequireEligibilityAndMinimumWorkers()
        {
            var job = new FacilityJobDefinitionState
            {
                Id = "job.education.scholar", ProfessionId = "profession.scholar",
                PrimarySkillId = "skill.scholar.basic", MinimumSkillBasisPoints = 2_000,
                RequiresSameCell = true
            };
            var person = new FacilityPersonFact
            {
                PersonId = "person.scholar", IsAlive = true, ProfessionId = "profession.scholar", CurrentCellId = "cell.taixue",
                SkillsByDefinitionId = new Dictionary<string, int> { ["skill.scholar.basic"] = 4_000 }
            };
            Assert.That(FacilityJobRules.Evaluate(job, person, "cell.taixue").Eligible, Is.True);
            var definition = new FacilityDefinitionState
            {
                Id = "facility.historical.imperial_academy", MinimumWorkersForNormalOperation = 2, WorkerCapacity = 4
            };
            var facility = new FacilityState { Id = "facility.taixue", DefinitionId = definition.Id };
            facility.WorkerPersonIds.Add(person.PersonId);
            Assert.That(facility.HasNormalProduction(definition), Is.False);
            facility.WorkerPersonIds.Add("person.scholar.two");
            Assert.That(facility.HasNormalProduction(definition), Is.True);
        }

        [Test]
        public void Luoyang184_BlueprintUsesSharedOwnerOccupancyAndRoadValidation()
        {
            var blueprint = new FacilityBlueprintDefinition
            {
                Id = "blueprint.gate", Orientation = BlueprintOrientation.East,
                Cells = new List<BlueprintCellDefinition>
                {
                    new BlueprintCellDefinition { RelativeX = 0, RelativeY = 0, FacilityDefinitionId = "facility.gate", BuildOrder = 1 },
                    new BlueprintCellDefinition { RelativeX = 1, RelativeY = 0, FacilityDefinitionId = "facility.wall", BuildOrder = 2,
                        RequiredRoadConnectionIds = new List<string> { "road.axis" } }
                }
            };
            var facts = new Dictionary<string, BlueprintPlacementCellFact>
            {
                ["10:10"] = new BlueprintPlacementCellFact { CellId64 = 1010, Exists = true, Developable = true, OwnerId = "owner" },
                ["10:11"] = new BlueprintPlacementCellFact { CellId64 = 1110, Exists = true, Developable = true, OwnerId = "owner",
                    RoadConnectionIds = new HashSet<string> { "road.axis" } }
            };
            var result = FacilityBlueprintRules.Validate(blueprint, 10, 10, "owner",
                (x, y) => facts.TryGetValue(x + ":" + y, out var fact) ? fact : null);
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Errors));
            facts["10:11"].FacilityId = "facility.occupied";
            Assert.That(FacilityBlueprintRules.Validate(blueprint, 10, 10, "owner",
                (x, y) => facts.TryGetValue(x + ":" + y, out var fact) ? fact : null).IsValid, Is.False);
        }

        [Test]
        public void Luoyang184_FortificationPassabilityRequiresGateControlLadderOrBreach()
        {
            var wall = new WallFacilityState
            {
                HeightCentimetres = 900, MaximumDurability = 100, CurrentDurability = 100, State = WallState.Intact
            };
            Assert.That(SiegePassabilityRules.CanCrossWall(wall, 899), Is.False);
            Assert.That(SiegePassabilityRules.CanCrossWall(wall, 900), Is.True);
            wall.ApplyDamage(100);
            Assert.That(SiegePassabilityRules.CanCrossWall(wall, 0), Is.True);
            var gate = new GateFacilityState { ControllerId = "garrison", OpenState = GateOpenState.Closed };
            Assert.That(gate.CanPass("garrison"), Is.False);
            gate.SetOpenState("garrison", GateOpenState.Open);
            Assert.That(gate.CanPass("garrison"), Is.True);
            Assert.Throws<InvalidOperationException>(() => gate.SetOpenState("enemy", GateOpenState.Closed));
            var moat = new MoatFeatureState { State = MoatState.Flooded };
            Assert.That(SiegePassabilityRules.CanCrossMoat(moat, false), Is.False);
            Assert.That(SiegePassabilityRules.CanCrossMoat(moat, true), Is.True);
        }

        [Test]
        public void Luoyang184_AiBalanceReadsActualPressureInsteadOfFixedCellRatio()
        {
            var pressure = new LocalDevelopmentPressureState
            {
                TotalPersons = 1_000, HousedPersons = 940, EffectiveWorkers = 600,
                FilledJobs = 540, AvailableResidentialPersonSlots = 20,
                VacantJobSlots = 30, SkillShortageSlots = 18
            };
            Assert.That(pressure.UnhousedPersons, Is.EqualTo(60));
            Assert.That(pressure.NeedsHousing, Is.True);
            Assert.That(pressure.NeedsTraining, Is.True);
        }
    }
}
