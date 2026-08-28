using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    internal static class LuoyangLivingWorldTestFixture
    {
        private static readonly object Sync = new object();
        private static ILuoyang184LivingWorldSource source;
        private static Luoyang184LivingWorldSystem system;
        private static Luoyang184LivingWorldRuntimeState daySeven;
        private static Luoyang184LivingWorldRuntimeState day365;

        public static string MetropolitanRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "WorldMap", "Luoyang184MetropolitanInitializationV1");

        public static ILuoyang184LivingWorldSource Source
        {
            get
            {
                Ensure();
                return source;
            }
        }

        public static Luoyang184LivingWorldSystem System
        {
            get
            {
                Ensure();
                return system;
            }
        }

        public static Luoyang184LivingWorldRuntimeState DaySeven
        {
            get
            {
                Ensure();
                if (daySeven == null)
                {
                    daySeven = system.CreateRuntime(184);
                    system.AdvanceTo(daySeven, 7);
                }
                return daySeven;
            }
        }

        public static Luoyang184LivingWorldRuntimeState Day365
        {
            get
            {
                Ensure();
                if (day365 == null)
                {
                    day365 = system.CreateRuntime(184);
                    system.AdvanceTo(day365, 365);
                }
                return day365;
            }
        }

        public static Luoyang184LivingWorldRuntimeState NewRuntime()
        {
            Ensure();
            return system.CreateRuntime(184);
        }

        private static void Ensure()
        {
            if (system != null) return;
            lock (Sync)
            {
                if (system != null) return;
                source = new Luoyang184LivingWorldSourceAdapter(MetropolitanRoot);
                system = new Luoyang184LivingWorldSystem(source);
            }
        }
    }

    [TestFixture]
    public sealed class Luoyang184PersonWorkProductionConsumptionClosureV1Tests
    {
        [Test]
        public void WorkerAssignmentTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Workforce.Count, Is.EqualTo(400000));
            Assert.That(runtime.Workforce.Select(item => item.PersonOrdinal)
                .Distinct().Count(), Is.EqualTo(400000));
            Assert.That(runtime.Workforce.All(item =>
                item.FacilityIndex == uint.MaxValue ||
                item.FacilityIndex < 2084), Is.True);
            Assert.That(runtime.Workforce.Any(item =>
                item.Status == LuoyangWorkforceStatus.MilitaryDuty), Is.True);
            Assert.That(runtime.Workforce.Where(item =>
                    item.Status == LuoyangWorkforceStatus.MilitaryDuty ||
                    item.Status == LuoyangWorkforceStatus.Official ||
                    item.Status == LuoyangWorkforceStatus.Student ||
                    item.Status == LuoyangWorkforceStatus.FamilyManagement)
                .All(item => item.Status != LuoyangWorkforceStatus.Assigned), Is.True);
        }

        [Test]
        public void ProductionCycleTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Facilities.Any(item =>
                item.Status == LuoyangProductionRuntimeStatus.Completed ||
                item.ProductionProgressBasisPoints > 0), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "production.recipe_settlement"), Is.True,
                string.Join(",", runtime.Facilities
                    .Where(item => !string.IsNullOrEmpty(item.RecipeId))
                    .GroupBy(item => item.Status)
                    .Select(item => item.Key + "=" + item.Count())) +
                ";assigned=" + runtime.Facilities.Where(item =>
                    !string.IsNullOrEmpty(item.RecipeId)).Sum(item =>
                    item.AssignedWorkers) +
                ";workforceAssigned=" + runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.Assigned));
        }

        [Test]
        public void RecipeInputOutputTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            var settlements = runtime.InventoryFlows.Where(item =>
                item.OperationId == "production.recipe_settlement").ToList();
            Assert.That(settlements, Is.Not.Empty);
            Assert.That(settlements.All(item =>
                !string.IsNullOrEmpty(item.SourceInventoryId) &&
                !string.IsNullOrEmpty(item.DestinationInventoryId) &&
                item.QuantityMilliunits > 0), Is.True);
            Assert.That(runtime.Facilities.Any(item =>
                item.Status == LuoyangProductionRuntimeStatus.WaitingInput), Is.True);
        }

        [Test]
        public void InventoryCapacityTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.Day365;
            Assert.That(runtime.Inventories.All(item =>
                item.QuantityMilliunits >= 0 &&
                item.QuantityMilliunits <= item.CapacityMilliunits), Is.True);
            foreach (var group in runtime.Inventories.GroupBy(item => item.FacilityId))
            {
                var sourceFacility = LuoyangLivingWorldTestFixture.Source.Facilities
                    .Single(item => item.FacilityId == group.Key);
                Assert.That(group.Sum(item => item.QuantityMilliunits),
                    Is.LessThanOrEqualTo(sourceFacility.StorageCapacity * 1000L));
            }
        }

        [Test]
        public void CropGrowthTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.Day365;
            Assert.That(runtime.Crops.Count, Is.EqualTo(135));
            Assert.That(runtime.Crops.All(item => item.HarvestedDay >= 0), Is.True);
            Assert.That(runtime.Crops.All(item =>
                item.ActualYieldMilliunits ==
                item.StoredYieldMilliunits + item.SeedRecoveredMilliunits +
                item.LostYieldMilliunits), Is.True);
            Assert.That(runtime.Crops.All(item => item.CycleNumber >= 2), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "production.crop_sowing"), Is.True);
        }

        [Test]
        public void EarlyHarvest80PercentTests()
        {
            Assert.That(Luoyang184LivingWorldRules.CanHarvest(7_990), Is.False);
            Assert.That(Luoyang184LivingWorldRules.CanHarvest(8_000), Is.True);
            Assert.That(Luoyang184LivingWorldRules.CanHarvest(10_000), Is.True);
            Assert.That(Luoyang184LivingWorldRules.CalculateHarvestYield(
                    100_000, 8_000), Is.LessThan(100_000));
            Assert.That(Luoyang184LivingWorldRules.CalculateHarvestQuality(8_000),
                Is.LessThan(Luoyang184LivingWorldRules.CalculateHarvestQuality(10_000)));
        }

        [Test]
        public void HarvestWorkerTests()
        {
            var sourceRuntime = LuoyangLivingWorldTestFixture.NewRuntime();
            var sourceCrop = sourceRuntime.Crops[0];
            var sourceInventory = sourceRuntime.Inventories.Single(item =>
                item.Id == sourceCrop.StorageInventoryId);
            var sourceSeedInventory = sourceRuntime.Inventories.Single(item =>
                item.Id == sourceCrop.SeedInventoryId);
            var runtime = new Luoyang184LivingWorldRuntimeState();
            runtime.Crops.Add(new LuoyangCropRuntimeState
            {
                FieldId = sourceCrop.FieldId,
                FacilityIndex = sourceCrop.FacilityIndex,
                FacilityId = sourceCrop.FacilityId,
                CropProductId = sourceCrop.CropProductId,
                StorageInventoryId = sourceCrop.StorageInventoryId,
                SeedInventoryId = sourceCrop.SeedInventoryId,
                PlantingDay = sourceCrop.PlantingDay,
                FullMaturityDay = sourceCrop.FullMaturityDay,
                CycleDurationDays = sourceCrop.CycleDurationDays,
                EarlyHarvestMinimumBasisPoints = 8_000,
                FullYieldMilliunits = sourceCrop.FullYieldMilliunits,
                AssignedWorkers = 0
            });
            runtime.Inventories.Add(new LuoyangInventoryBalanceState
            {
                Id = sourceInventory.Id,
                OwnerKind = sourceInventory.OwnerKind,
                OwnerId = sourceInventory.OwnerId,
                FacilityId = sourceInventory.FacilityId,
                ProductId = sourceInventory.ProductId,
                CapacityMilliunits = sourceInventory.CapacityMilliunits
            });
            runtime.Inventories.Add(new LuoyangInventoryBalanceState
            {
                Id = sourceSeedInventory.Id,
                OwnerKind = sourceSeedInventory.OwnerKind,
                OwnerId = sourceSeedInventory.OwnerId,
                FacilityId = sourceSeedInventory.FacilityId,
                ProductId = sourceSeedInventory.ProductId,
                CapacityMilliunits = sourceSeedInventory.CapacityMilliunits
            });
            Assert.That(LuoyangLivingWorldTestFixture.System.TryHarvestAtMaturity(
                runtime, sourceCrop.FieldId, 8_000, out _), Is.False);
            runtime.Crops[0].AssignedWorkers = 1;
            Assert.That(LuoyangLivingWorldTestFixture.System.TryHarvestAtMaturity(
                runtime, sourceCrop.FieldId, 8_000, out var harvested), Is.True);
            Assert.That(harvested, Is.GreaterThan(0));
        }

        [Test]
        public void ConsumptionTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Households.All(item =>
                item.CumulativeFoodConsumedMilliunits <=
                item.CumulativeFoodDemandMilliunits), Is.True);
            Assert.That(runtime.Workforce.All(item =>
                item.CumulativeFoodConsumedMilliunits <=
                item.CumulativeFoodDemandMilliunits), Is.True);
            Assert.That(runtime.Households.Any(item =>
                item.CumulativeFoodConsumedMilliunits > 0), Is.True);
        }

        [Test]
        public void HouseholdBatchConsumptionTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Households.Count, Is.EqualTo(80899));
            Assert.That(runtime.Households.All(item =>
                item.MemberCount > 0 && item.DailyFoodDemandMilliunits > 0), Is.True);
            Assert.That(runtime.InventoryFlows.Count(item =>
                item.OperationId == "household.food_consumed"),
                Is.LessThan(runtime.Households.Count * 7),
                "Consumption must be batched by source/day, not one transaction per Person.");
        }

        [Test]
        public void MarketTransferTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Markets.All(item =>
                item.TransferredMilliunits <= item.SupplyMilliunits), Is.True);
            Assert.That(runtime.Markets.All(item =>
                item.FailedDemandMilliunits >= 0 &&
                item.CurrentPriceBasisPoints >= 10_000), Is.True);
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "scenario.opening.delivered_stock"), Is.True);
            Assert.That(runtime.SupplyOrders.Any(), Is.True);
            Assert.That(runtime.Shipments.Any(), Is.True);
        }

        [Test]
        public void ShortageTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.ExternalSuppliers.Count, Is.GreaterThan(0));
            Assert.That(runtime.SupplyOrders.All(item =>
                item.RequestedQuantityMilliunits > 0), Is.True);
        }

        [Test]
        public void NoNegativeInventoryTests()
        {
            Assert.That(LuoyangLivingWorldTestFixture.Day365.Inventories.All(item =>
                item.QuantityMilliunits >= 0), Is.True);
        }

        [Test]
        public void ResourceConservationTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.Day365;
            var imported = runtime.InventoryFlows.Where(item =>
                (item.OperationId == "scenario.opening.delivered_stock" ||
                 item.OperationId == "supply.shipment_delivered") &&
                IsFood(item.ProductId)).Sum(item => item.QuantityMilliunits);
            var harvest = runtime.InventoryFlows.Where(item =>
                item.OperationId == "production.crop_harvest" &&
                IsFood(item.ProductId)).Sum(item => item.QuantityMilliunits);
            var processingLoss = runtime.InventoryFlows.Where(item =>
                item.OperationId == "production.recipe_settlement" &&
                IsFood(item.ProductId)).Sum(item => item.LossMilliunits);
            var consumed = runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var closing = runtime.Inventories.Where(item =>
                IsFood(item.ProductId)).Sum(item => item.QuantityMilliunits);
            Assert.That(imported + harvest,
                Is.EqualTo(consumed + closing + processingLoss));
        }

        [Test]
        public void SaveLoadProductionTests()
        {
            var runtime = LuoyangLivingWorldTestFixture.NewRuntime();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(runtime, 2);
            var inProgress = runtime.Facilities.First(item =>
                item.ProductionProgressBasisPoints > 0 &&
                item.ProductionProgressBasisPoints < 10_000);
            var root = Path.Combine(Application.temporaryCachePath,
                "mandate-luoyang-living-world-test");
            var store = new Luoyang184LivingWorldCheckpointStore();
            var saved = store.Save(runtime, root);
            var loaded = store.Load(saved.CheckpointPath);
            var loadedProgress = loaded.Facilities.Single(item =>
                item.FacilityId == inProgress.FacilityId);
            Assert.That(loadedProgress.ProductionProgressBasisPoints,
                Is.EqualTo(inProgress.ProductionProgressBasisPoints));
            Assert.That(loaded.Crops[0].MaturityBasisPoints,
                Is.EqualTo(runtime.Crops[0].MaturityBasisPoints));
            Luoyang184LivingWorldRules.ValidateRuntime(loaded, 400000, 80899, 2084);
        }

        [Test]
        public void DeterminismTests()
        {
            var left = LuoyangLivingWorldTestFixture.NewRuntime();
            var right = LuoyangLivingWorldTestFixture.NewRuntime();
            LuoyangLivingWorldTestFixture.System.AdvanceTo(left, 30);
            LuoyangLivingWorldTestFixture.System.AdvanceTo(right, 30);
            var leftSummary = LuoyangLivingWorldTestFixture.System
                .BuildWorldSummary(left);
            var rightSummary = LuoyangLivingWorldTestFixture.System
                .BuildWorldSummary(right);
            Assert.That(rightSummary.FoodStockMilliunits,
                Is.EqualTo(leftSummary.FoodStockMilliunits));
            Assert.That(rightSummary.FoodConsumptionMilliunits,
                Is.EqualTo(leftSummary.FoodConsumptionMilliunits));
            Assert.That(rightSummary.FoodShortageMilliunits,
                Is.EqualTo(leftSummary.FoodShortageMilliunits));
            CollectionAssert.AreEqual(
                left.Inventories.OrderBy(item => item.Id).Select(item =>
                    item.Id + ":" + item.QuantityMilliunits),
                right.Inventories.OrderBy(item => item.Id).Select(item =>
                    item.Id + ":" + item.QuantityMilliunits));
        }

        private static bool IsFood(string productId) =>
            productId == "product.food.millet_grain" ||
            productId == "product.food.wheat_grain" ||
            productId == "product.food.broomcorn_grain" ||
            productId == "product.food.bean" ||
            productId == CoreProductionContent.WheatGrainProductId ||
            productId == CoreProductionContent.WheatFlourProductId ||
            productId == CoreProductionContent.DryRationProductId ||
            productId == CoreProductionContent.FreshMuttonProductId ||
            productId == CoreProductionContent.OffalProductId;
    }

    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangLiving_WorkforceUsesProtectedPersonsAndStableFacilities()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.Workforce.Count, Is.EqualTo(400000));
            Assert.That(runtime.Households.Count, Is.EqualTo(80899));
            Assert.That(runtime.Facilities.Count, Is.EqualTo(2084));
            Assert.That(runtime.Workforce.Select(item => item.PersonOrdinal)
                .Distinct().Count(), Is.EqualTo(400000));
        }

        [Test]
        public void LuoyangLiving_ProductionConsumptionAndShortageArePhysical()
        {
            var runtime = LuoyangLivingWorldTestFixture.DaySeven;
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.OperationId == "production.recipe_settlement"), Is.True,
                string.Join(",", runtime.Facilities
                    .Where(item => !string.IsNullOrEmpty(item.RecipeId))
                    .GroupBy(item => item.Status)
                    .Select(item => item.Key + "=" + item.Count())) +
                ";assigned=" + runtime.Facilities.Where(item =>
                    !string.IsNullOrEmpty(item.RecipeId)).Sum(item =>
                    item.AssignedWorkers) +
                ";workforceAssigned=" + runtime.Workforce.Count(item =>
                    item.Status == LuoyangWorkforceStatus.Assigned));
            Assert.That(runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits), Is.GreaterThan(0));
            Assert.That(runtime.Inventories.All(item =>
                item.QuantityMilliunits >= 0), Is.True);
            Assert.That(runtime.ShortageResponses, Is.Not.Empty);
        }

        [Test]
        public void LuoyangLiving_365DayCropAndConservationRemainStable()
        {
            var runtime = LuoyangLivingWorldTestFixture.Day365;
            Assert.That(runtime.DaySnapshots.Select(item => item.Day),
                Is.EquivalentTo(new long[] { 1, 7, 30, 90, 180, 365 }));
            Assert.That(runtime.Crops.All(item => item.HarvestedDay >= 0), Is.True);
            Luoyang184LivingWorldRules.ValidateRuntime(runtime,
                400000, 80899, 2084);
        }

        [Test]
        public void LuoyangLiving_V69MigratesToEmptyV70Contract()
        {
            var world = WorldState.Create(184);
            world.SchemaVersion = 69;
            world.LuoyangLivingWorlds = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            Assert.That(
                migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangLivingWorlds, Is.Empty);
        }
    }
}
