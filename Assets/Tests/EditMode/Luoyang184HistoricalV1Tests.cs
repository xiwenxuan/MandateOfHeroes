using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class Luoyang184HistoricalV1Tests
    {
        private static string PackageRoot => Path.Combine(Application.dataPath, "StreamingAssets", "WorldMap", "Luoyang184HistoricalV1");

        [Test]
        public void ScenarioUsesEasternHan184AndUnifiedWorldGrid()
        {
            var world = new Luoyang184HistoricalPrototypeReader(PackageRoot).World;
            Assert.That(world.ScenarioYear, Is.EqualTo(184));
            Assert.That(world.ScenarioPolityId, Is.EqualTo("polity.eastern_han"));
            Assert.That(world.GridSchemaVersion, Is.EqualTo("hanworld.square-grid.v1"));
            Assert.That(world.CellSizeMetres, Is.EqualTo(2000));
            Assert.That(world.PopulationProfile.TotalPersons, Is.EqualTo(20_542));
            Assert.That(world.PopulationProfile.TotalHouseholds, Is.EqualTo(4_498));
        }

        [Test]
        public void TwelveMainCityGatesAreIndependentFacilities()
        {
            var world = new Luoyang184HistoricalPrototypeReader(PackageRoot).World;
            var network = world.FortificationNetworks.Single(item => item.NetworkId == "fortification.luoyang.main_wall");
            Assert.That(network.GateFacilityIds, Has.Count.EqualTo(12));
            Assert.That(network.GateFacilityIds.Distinct().Count(), Is.EqualTo(12));
            Assert.That(network.WallFacilityIds, Has.Count.GreaterThan(40));
            Assert.That(world.FortificationNetworks.Count, Is.EqualTo(3));
        }

        [Test]
        public void HistoricalFacilitiesHaveFunctionsWorkersAndFutureHooks()
        {
            var world = new Luoyang184HistoricalPrototypeReader(PackageRoot).World;
            var historical = world.Facilities.Where(item => item.FacilityId.StartsWith("facility.instance.luoyang.184.", StringComparison.Ordinal)).ToArray();
            Assert.That(historical.Length, Is.GreaterThan(150));
            Assert.That(historical.All(item => item.PurposeIds.Count > 0 && item.CapabilityIds.Count > 0 && item.FutureHookIds.Count > 0), Is.True);
            Assert.That(historical.Any(item => item.DisplayName == "北宫"), Is.True);
            Assert.That(historical.Any(item => item.DisplayName == "太学"), Is.True);
            Assert.That(historical.Any(item => item.DisplayName == "明堂"), Is.True);
        }

        [Test]
        public void HousingCapacityCountsPersonsAndBarracksRejectCivilians()
        {
            var barracks = new FacilityDefinitionState
            {
                Id = "facility.historical.barracks",
                ResidentialCapacityPersons = 1,
                AllowedResidentTypeIds = new List<string> { FacilityPopulationTypeIds.ActiveMilitary }
            };
            var state = new FacilityState { Id = "facility.one", DefinitionId = barracks.Id };
            var civilian = new FacilityPersonFact
            {
                PersonId = "person.civilian", IsAlive = true,
                PopulationTypeId = FacilityPopulationTypeIds.Civilian
            };
            var soldier = new FacilityPersonFact
            {
                PersonId = "person.soldier", IsAlive = true, IsActiveMilitary = true,
                PopulationTypeId = FacilityPopulationTypeIds.ActiveMilitary
            };
            Assert.That(FacilityHousingRules.TryAssign(barracks, state, civilian, out var civilianReason), Is.False);
            Assert.That(civilianReason, Is.EqualTo("resident_not_eligible"));
            Assert.That(FacilityHousingRules.TryAssign(barracks, state, soldier, out _), Is.True);
            Assert.That(state.ResidentPersonIds, Is.EqualTo(new[] { "person.soldier" }));
        }

        [Test]
        public void FacilityWithoutMinimumRealWorkersCannotOperateNormally()
        {
            var definition = new FacilityDefinitionState
            {
                Id = "facility.historical.taicang", MinimumWorkersForNormalOperation = 2, WorkerCapacity = 5
            };
            var facility = new FacilityState { Id = "taicang", DefinitionId = definition.Id };
            Assert.That(facility.HasNormalProduction(definition), Is.False);
            facility.WorkerPersonIds.Add("person.one");
            Assert.That(facility.HasNormalProduction(definition), Is.False);
            facility.WorkerPersonIds.Add("person.two");
            Assert.That(facility.HasNormalProduction(definition), Is.True);
        }

        [Test]
        public void JobEligibilityAndFitReadProfessionSkillAndPresence()
        {
            var job = new FacilityJobDefinitionState
            {
                Id = "job.education.scholar", ProfessionId = "profession.scholar",
                PrimarySkillId = "skill.scholar.basic", MinimumSkillBasisPoints = 2_000,
                RequiresSameCell = true
            };
            var person = new FacilityPersonFact
            {
                PersonId = "person.scholar", IsAlive = true, ProfessionId = "profession.scholar",
                CurrentCellId = "cell.1",
                SkillsByDefinitionId = new Dictionary<string, int> { ["skill.scholar.basic"] = 4_000 }
            };
            var fit = FacilityJobRules.Evaluate(job, person, "cell.1");
            Assert.That(fit.Eligible, Is.True);
            Assert.That(fit.FitBasisPoints, Is.GreaterThan(5_000));
            person.CurrentCellId = "cell.2";
            Assert.That(FacilityJobRules.Evaluate(job, person, "cell.1").Eligible, Is.False);
        }

        [Test]
        public void AiPressureUsesFactsRatherThanFixedCellRatio()
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

        [Test]
        public void MultiCellBlueprintValidatesOwnerOccupancyAndRoadConnections()
        {
            var blueprint = new FacilityBlueprintDefinition
            {
                Id = "blueprint.test", Orientation = BlueprintOrientation.East,
                Cells = new List<BlueprintCellDefinition>
                {
                    new BlueprintCellDefinition { RelativeX = 0, RelativeY = 0, FacilityDefinitionId = "facility.gate", BuildOrder = 1 },
                    new BlueprintCellDefinition { RelativeX = 1, RelativeY = 0, FacilityDefinitionId = "facility.wall", BuildOrder = 2,
                        RequiredRoadConnectionIds = new List<string> { "road.axis" } }
                }
            };
            var facts = new Dictionary<string, BlueprintPlacementCellFact>
            {
                ["10:10"] = new BlueprintPlacementCellFact { X = 10, Y = 10, CellId64 = 1010, Exists = true, Developable = true, OwnerId = "owner" },
                ["10:11"] = new BlueprintPlacementCellFact { X = 10, Y = 11, CellId64 = 1110, Exists = true, Developable = true, OwnerId = "owner",
                    RoadConnectionIds = new HashSet<string> { "road.axis" } }
            };
            var result = FacilityBlueprintRules.Validate(blueprint, 10, 10, "owner",
                (x, y) => facts.TryGetValue(x + ":" + y, out var fact) ? fact : null);
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Errors));
            facts["10:11"].FacilityId = "occupied";
            Assert.That(FacilityBlueprintRules.Validate(blueprint, 10, 10, "owner",
                (x, y) => facts.TryGetValue(x + ":" + y, out var fact) ? fact : null).IsValid, Is.False);
        }

        [Test]
        public void WallGateMoatAndLadderRulesCreateIndependentPassability()
        {
            var wall = new WallFacilityState
            {
                HeightCentimetres = 900, MaximumDurability = 100, CurrentDurability = 100, State = WallState.Intact
            };
            Assert.That(SiegePassabilityRules.CanCrossWall(wall, 899), Is.False);
            Assert.That(SiegePassabilityRules.CanCrossWall(wall, 900), Is.True);
            wall.ApplyDamage(100);
            Assert.That(wall.State, Is.EqualTo(WallState.Breached));
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
    }
}
