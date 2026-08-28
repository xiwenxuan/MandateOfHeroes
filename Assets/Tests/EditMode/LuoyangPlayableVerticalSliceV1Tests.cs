using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangVisual_FacilityVisualBindingUsesRuntimeIds()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var system = new LuoyangVisualPresentationSystem();
            var view = system.BuildProjection(runtime, 24, 32);
            Assert.That(view.FacilityAnchors, Is.Not.Empty);
            Assert.That(view.FacilityAnchors.All(anchor =>
                runtime.Facilities.Exists(item => item.FacilityId ==
                    anchor.FacilityId && item.CellId64 == anchor.CellId64)), Is.True);
        }

        [Test]
        public void LuoyangVisual_BuildBlueprintBindingsAreReusableAndSeparated()
        {
            var system = new LuoyangVisualPresentationSystem();
            Assert.That(system.Blueprints.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(system.Blueprints.Where(item =>
                (item.Availability & BuildAvailability.Player) != 0),
                Has.All.Matches<BuildBlueprintDefinition>(item =>
                    (item.Availability & BuildAvailability.Ai) != 0 &&
                    (item.Availability & BuildAvailability.HistoricalInit) != 0 &&
                    system.Profiles.Any(profile => profile.VisualProfileId ==
                        item.VisualProfileId)));
        }

        [Test]
        public void LuoyangVisual_HistoricalPalaceCannotBePlayerBuilt()
        {
            var blueprint = new LuoyangVisualPresentationSystem().GetBlueprint(
                "blueprint.han.palace.historical.nangong.v1");
            Assert.That(blueprint, Is.Not.Null);
            Assert.That((blueprint.Availability & BuildAvailability.Player),
                Is.EqualTo(BuildAvailability.None));
            Assert.That(blueprint.HistoricalRestrictionId,
                Is.EqualTo("historical_init_only"));
        }

        [Test]
        public void LuoyangVisual_VisualAnchorDoesNotCreateSubCell()
        {
            var fields = typeof(FacilityVisualAnchor).GetFields()
                .Select(item => item.Name).ToArray();
            Assert.That(fields, Does.Contain("CellId64"));
            Assert.That(fields, Does.Contain("LocalX"));
            Assert.That(fields, Has.None.Contains("SubCell"));
        }

        [Test]
        public void LuoyangVisual_CropVisualStateIncludesEarlyHarvest80()
        {
            var crop = new LuoyangCropRuntimeState
            {
                EarlyHarvestMinimumBasisPoints = 8_000,
                MaturityBasisPoints = 8_200,
                Phase = LuoyangCropPhase.Harvestable
            };
            Assert.That(LuoyangVisualPresentationRules.ResolveCropStage(crop),
                Is.EqualTo(CropVisualStage.Harvestable80));
            crop.MaturityBasisPoints = 10_000;
            Assert.That(LuoyangVisualPresentationRules.ResolveCropStage(crop),
                Is.EqualTo(CropVisualStage.Mature));
        }

        [Test]
        public void LuoyangVisual_FacilityLifecycleChangesVisualState()
        {
            var facility = new LuoyangFacilityProductionRuntimeState
                { ConditionBasisPoints = 10_000,
                    Status = LuoyangProductionRuntimeStatus.InProgress };
            Assert.That(LuoyangVisualPresentationRules.ResolveFacilityState(facility),
                Is.EqualTo(FacilityRuntimeVisualState.Working));
            facility.ConditionBasisPoints = 6_000;
            Assert.That(LuoyangVisualPresentationRules.ResolveFacilityState(facility),
                Is.EqualTo(FacilityRuntimeVisualState.Damaged));
            facility.ConditionBasisPoints = 0;
            Assert.That(LuoyangVisualPresentationRules.ResolveFacilityState(facility),
                Is.EqualTo(FacilityRuntimeVisualState.Abandoned));
        }

        [Test]
        public void LuoyangVisual_ShipmentRepresentationUsesRealCargo()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var view = new LuoyangVisualPresentationSystem().BuildProjection(runtime);
            foreach (var representation in view.Shipments)
            {
                var shipment = runtime.Shipments.Find(item => item.Id ==
                    representation.ShipmentId);
                Assert.That(shipment, Is.Not.Null);
                Assert.That(representation.CargoMilliunits,
                    Is.EqualTo(shipment.ShippedQuantityMilliunits));
                Assert.That(representation.RouteId, Is.EqualTo(shipment.RouteId));
            }
        }

        [Test]
        public void LuoyangVisual_PersonActorsUsePermanentPersonOrdinals()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var view = new LuoyangVisualPresentationSystem().BuildProjection(runtime);
            Assert.That(view.Actors, Is.Not.Empty);
            Assert.That(view.Actors.All(item =>
                item.PersonOrdinal < runtime.Workforce.Count &&
                item.RuntimePersonId.EndsWith(item.PersonOrdinal.ToString("D6"),
                    StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void LuoyangVisual_ZoomLodDoesNotChangeRuntimeIdentity()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var system = new LuoyangVisualPresentationSystem();
            var near = system.BuildProjection(runtime, 96, 96);
            var far = system.BuildProjection(runtime, 16, 32);
            var runtimeIds = runtime.Facilities.Select(item => item.FacilityId)
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
            Assert.That(near.FacilityAnchors.All(item =>
                runtimeIds.Contains(item.FacilityId)), Is.True);
            Assert.That(far.FacilityAnchors.All(item =>
                runtimeIds.Contains(item.FacilityId)), Is.True);
            Assert.That(runtime.Facilities.Select(item => item.FacilityId)
                .OrderBy(item => item, StringComparer.Ordinal),
                Is.EqualTo(runtimeIds));
        }

        [Test]
        public void LuoyangVisual_RiverAndRoadSplinesAreContinuousAndBound()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var view = new LuoyangVisualPresentationSystem().BuildProjection(runtime);
            Assert.That(view.RiverSplines, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(view.RoadSplines, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(view.RiverSplines.Concat(view.RoadSplines), Has.All
                .Matches<RuntimeVisualSpline>(item =>
                    !string.IsNullOrWhiteSpace(item.RuntimeBindingId) &&
                    item.Points.Count >= 2 && item.Width > 0));
        }

        [Test]
        public void LuoyangVisual_PlayerAndAiUseSameBlueprintConstructionExecutor()
        {
            var playerRuntime = CreateLuoyangVisualRuntime();
            var aiRuntime = CreateLuoyangVisualRuntime();
            EnsureVisualTestConstructionMaterials(playerRuntime);
            EnsureVisualTestConstructionMaterials(aiRuntime);
            var system = new LuoyangVisualPresentationSystem();
            var owner = playerRuntime.GovernmentEconomy.OrganizationId;
            var playerCell = playerRuntime.CellProperties.Where(item =>
                    item.OwnerId == owner && string.IsNullOrEmpty(item.FacilityId))
                .OrderBy(item => item.CellId64).First();
            var aiCell = aiRuntime.CellProperties.Where(item =>
                    item.OwnerId == owner &&
                    string.IsNullOrEmpty(item.FacilityId))
                .OrderBy(item => item.CellId64).First();
            var player = system.StartFromBlueprint(playerRuntime,
                "blueprint.han.residence.general.v1", playerCell.CellId64,
                owner, "player.person.0");
            var ai = system.StartFromBlueprint(aiRuntime,
                "blueprint.han.residence.general.v1", aiCell.CellId64,
                owner, "agent.settlement.test");
            Assert.That(player.FacilityDefinitionId,
                Is.EqualTo(ai.FacilityDefinitionId));
            Assert.That(player.Materials.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(ai.RequiredLaborers, Is.EqualTo(4));
        }

        [Test]
        public void LuoyangVisual_FourPlayerBlueprintsCompleteEndToEnd()
        {
            var blueprintIds = new[]
            {
                "blueprint.han.residence.general.v1",
                "blueprint.han.warehouse.general.v1",
                "blueprint.han.workshop.general.v1",
                "blueprint.han.market.general.v1"
            };
            foreach (var blueprintId in blueprintIds)
            {
                var runtime = CreateLuoyangVisualRuntime();
                var visual = new LuoyangVisualPresentationSystem();
                var owner = runtime.GovernmentEconomy.OrganizationId;
                var cell = runtime.CellProperties.Where(item =>
                        item.OwnerId == owner &&
                        string.IsNullOrEmpty(item.FacilityId))
                    .OrderBy(item => item.CellId64).First();
                var before = runtime.Facilities.Count;
                var arrival = visual.OrderMissingConstructionMaterials(runtime,
                    blueprintId, owner, "player.person.0");
                var living = new Luoyang184LivingWorldSystem(
                    new Luoyang184LivingWorldSourceAdapter(Path.Combine(
                        Directory.GetCurrentDirectory(), "Assets",
                        "StreamingAssets", "WorldMap",
                        "Luoyang184MetropolitanInitializationV1")));
                living.AdvanceTo(runtime, arrival);
                var project = visual.StartFromBlueprint(runtime, blueprintId,
                    cell.CellId64, owner, "player.person.0");
                living.AdvanceTo(runtime, project.CompletionDay);
                Assert.That(project.Completed, Is.True, blueprintId);
                Assert.That(runtime.Facilities.Count, Is.EqualTo(before + 1),
                    blueprintId);
                Assert.That(project.Materials, Has.Count.GreaterThanOrEqualTo(2),
                    blueprintId);
            }
        }

        [Test]
        public void LuoyangVisual_ConstructionStagesProgressFromRuntimeTime()
        {
            var project = new LuoyangCompactConstructionProjectState
                { StartedDay = 10, CompletionDay = 30 };
            Assert.That(LuoyangVisualPresentationRules.ResolveConstructionStage(
                project, 10), Is.EqualTo(ConstructionVisualStage.SitePreparation));
            Assert.That(LuoyangVisualPresentationRules.ResolveConstructionStage(
                project, 20), Is.EqualTo(ConstructionVisualStage.Frame));
            project.Completed = true;
            Assert.That(LuoyangVisualPresentationRules.ResolveConstructionStage(
                project, 30), Is.EqualTo(ConstructionVisualStage.Complete));
        }

        [Test]
        public void LuoyangVisual_SaveLoadRebuildsProjectionFromRuntime()
        {
            var runtime = CreateLuoyangVisualRuntime();
            var before = new LuoyangVisualPresentationSystem()
                .BuildProjection(runtime, 32, 40);
            var root = Path.Combine(Path.GetTempPath(), "mandate-luoyang-visual-" +
                Guid.NewGuid().ToString("N"));
            try
            {
                var result = new Luoyang184LivingWorldCheckpointStore().Save(runtime,
                    root);
                var loaded = new Luoyang184LivingWorldCheckpointStore().Load(
                    result.CheckpointPath);
                var after = new LuoyangVisualPresentationSystem()
                    .BuildProjection(loaded, 32, 40);
                Assert.That(after.FacilityAnchors.Select(item => item.FacilityId),
                    Is.EqualTo(before.FacilityAnchors.Select(item => item.FacilityId)));
                Assert.That(after.Actors.Select(item => item.PersonOrdinal),
                    Is.EqualTo(before.Actors.Select(item => item.PersonOrdinal)));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static Luoyang184LivingWorldRuntimeState CreateLuoyangVisualRuntime()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap",
                "Luoyang184MetropolitanInitializationV1");
            var system = new Luoyang184LivingWorldSystem(
                new Luoyang184LivingWorldSourceAdapter(root));
            var runtime = system.CreateRuntime(184);
            system.AdvanceTo(runtime, 4);
            return runtime;
        }

        private static void EnsureVisualTestConstructionMaterials(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var products = new[]
            {
                CoreProductionContent.TimberMaterialProductId,
                "product.reference.building_material"
            };
            foreach (var product in products)
            {
                var inventory = runtime.Inventories.FirstOrDefault(item =>
                    item.ProductId == product &&
                    item.OwnerId == runtime.GovernmentEconomy.OrganizationId);
                if (inventory == null)
                {
                    inventory = new LuoyangInventoryBalanceState
                    {
                        Id = "inventory.test.visual." + product,
                        OwnerKind = LuoyangInventoryOwnerKind.Government,
                        OwnerId = runtime.GovernmentEconomy.OrganizationId,
                        FacilityId = runtime.Facilities[0].FacilityId,
                        ProductId = product,
                        CapacityMilliunits = 100_000
                    };
                    runtime.Inventories.Add(inventory);
                }
                inventory.CapacityMilliunits = Math.Max(
                    inventory.CapacityMilliunits, 100_000);
                inventory.QuantityMilliunits = Math.Max(
                    inventory.QuantityMilliunits, 50_000);
            }
        }
    }
}
