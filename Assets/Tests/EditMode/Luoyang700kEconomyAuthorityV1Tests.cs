using System;
using System.Diagnostics;
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
        public void EconomyAuthorityMatrixTests_IdentifiesEveryFoodWriterAndLeavesNoUnknownAuthority()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Docs",
                "Evidence", "LuoyangIntegratedEconomyV1",
                "economy-authority-matrix.md");
            var text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("UNKNOWN = 0"));
            Assert.That(text, Does.Contain("Luoyang184LivingWorldSystem.cs"));
            Assert.That(text, Does.Contain("FormalCountyMarketSystem.cs"));
            Assert.That(text, Does.Contain("CivilianFreightSystem.cs"));
        }

        [Test]
        public void CompactFormalDoubleWriteTests_HarvestCreatesOneFormalSourceAndOnlyRefreshesProjection()
        {
            var runtime = CreateAuthorityRuntime(71_001UL);
            var crop = runtime.Crops[0];
            var before = runtime.FormalEconomy.CumulativeHarvestedMilliunits;
            Assert.That(new Luoyang184LivingWorldSystem(AuthoritySource())
                .TryHarvestAtMaturity(runtime, crop.FieldId, 10_000,
                    out var harvested), Is.True);
            Assert.That(runtime.FormalEconomy.CumulativeHarvestedMilliunits - before,
                Is.EqualTo(harvested));
            Assert.That(runtime.FormalEconomy.CompactPhysicalMutationCount,
                Is.Zero);
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void CompactFormalDoubleConsumptionTests_DailySettlementCreatesOneFormalSink()
        {
            var runtime = CreateAuthorityRuntime(71_002UL);
            var before = runtime.FormalEconomy.CumulativeConsumedMilliunits;
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 1);
            var actual = runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            Assert.That(runtime.FormalEconomy.CumulativeConsumedMilliunits - before,
                Is.EqualTo(actual));
            Assert.That(runtime.FormalEconomy.CompactPhysicalMutationCount,
                Is.Zero);
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void FormalHarvestAuthorityTests_StoresHarvestAsTraceableProductBatchTransaction()
        {
            var runtime = CreateAuthorityRuntime(71_003UL);
            var crop = runtime.Crops[1];
            new Luoyang184LivingWorldSystem(AuthoritySource())
                .TryHarvestAtMaturity(runtime, crop.FieldId, 10_000, out _);
            Assert.That(runtime.FormalEconomy.InventoryTransactions.Exists(item =>
                item.Type == InventoryTransactionType.FoodHarvested &&
                item.SourceWorkOrderId.Contains(crop.FieldId)), Is.True);
            Assert.That(runtime.FormalEconomy.ProductBatches.Exists(item =>
                item.ProductDefinitionId == crop.CropProductId &&
                (item.SourceWorkOrderId ?? string.Empty).Contains(crop.FieldId)),
                Is.True);
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void FormalConsumptionAuthorityTests_ActualConsumedEqualsFormalNegativeTransaction()
        {
            var runtime = CreateAuthorityRuntime(71_004UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 7);
            var negative = -runtime.FormalEconomy.InventoryTransactions
                .Where(item => item.Type == InventoryTransactionType.FoodConsumed)
                .SelectMany(item => item.Lines)
                .Where(item => item.QuantityDelta < 0)
                .Sum(item => item.QuantityDelta);
            Assert.That(negative,
                Is.EqualTo(runtime.FormalEconomy.CumulativeConsumedMilliunits));
            Assert.That(runtime.Households.Sum(item =>
                    item.CumulativeFoodShortageMilliunits),
                Is.EqualTo(runtime.Households.Sum(item =>
                    item.CumulativeFoodDemandMilliunits -
                    item.CumulativeFoodConsumedMilliunits)));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void FormalMarketAuthorityTests_CompactInflationCannotIncreaseSellableFormalStock()
        {
            var runtime = CreateAuthorityRuntime(71_005UL);
            var inventory = runtime.Inventories.First(item =>
                item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                LuoyangFormalEconomySystem.IsFood(item.ProductId));
            var formal = LuoyangFormalEconomySystem.GetAvailableQuantity(
                runtime, inventory.Id, inventory.ProductId);
            inventory.QuantityMilliunits += 1_000_000;
            Assert.That(LuoyangFormalEconomySystem.GetAvailableQuantity(
                runtime, inventory.Id, inventory.ProductId), Is.EqualTo(formal));
            new LuoyangFormalEconomySystem().RebuildProjection(runtime);
            Assert.That(inventory.QuantityMilliunits, Is.EqualTo(formal));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void FormalFreightAuthorityTests_DispatchMovesBatchIntoFormalMobileContainer()
        {
            var runtime = CreateAuthorityRuntime(71_006UL);
            var supplier = runtime.ExternalSuppliers.First(item =>
                LuoyangFormalEconomySystem.IsFood(item.ProductId));
            var before = LuoyangFormalEconomySystem.GetAvailableQuantity(
                runtime, supplier.InventoryId, supplier.ProductId);
            var shipped = Math.Min(10_000L, before);
            new LuoyangFormalEconomySystem().DispatchFreight(runtime,
                supplier.InventoryId, "shipment.authority.test", supplier.ProductId,
                shipped, 0, "person.authority.carrier");
            Assert.That(LuoyangFormalEconomySystem.GetAvailableQuantity(
                runtime, supplier.InventoryId, supplier.ProductId),
                Is.EqualTo(before - shipped));
            Assert.That(runtime.FormalEconomy.InventoryContainers.Exists(item =>
                item.Id == LuoyangFormalEconomySystem.FreightContainerId(
                    "shipment.authority.test")), Is.True);
            Assert.That(runtime.FormalEconomy.ProductBatches.Where(item =>
                item.InventoryContainerId ==
                    LuoyangFormalEconomySystem.FreightContainerId(
                        "shipment.authority.test"))
                .Sum(item => item.Quantity), Is.EqualTo(shipped));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void CompactProjectionConsistencyTests_FormalAndCompactFoodMatchAtCheckpoints()
        {
            var runtime = CreateAuthorityRuntime(71_007UL);
            var system = new Luoyang184LivingWorldSystem(AuthoritySource());
            foreach (var day in new[] { 1, 7, 30 })
            {
                system.AdvanceTo(runtime, day);
                AssertAuthorityBalanced(runtime);
            }
        }

        [Test]
        public void CompactProjectionRebuildTests_DiscardAndRebuildDoesNotChangeFormalAuthority()
        {
            var runtime = CreateAuthorityRuntime(71_008UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 7);
            var authority = LuoyangFormalEconomySystem.ComputeAuthorityHash(runtime);
            var projection = runtime.FormalEconomy.ProjectionHash;
            foreach (var inventory in runtime.Inventories.Where(item =>
                         LuoyangFormalEconomySystem.IsFood(item.ProductId)))
                inventory.QuantityMilliunits = 0;
            foreach (var household in runtime.Households)
                household.FoodReserveMilliunits = 0;
            new LuoyangFormalEconomySystem().RebuildProjection(runtime);
            Assert.That(LuoyangFormalEconomySystem.ComputeAuthorityHash(runtime),
                Is.EqualTo(authority));
            Assert.That(runtime.FormalEconomy.ProjectionHash,
                Is.EqualTo(projection));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void FormalEconomySaveLoadAuthorityTests_FormalAuthorityOverridesStaleProjection()
        {
            var runtime = CreateAuthorityRuntime(71_009UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 15);
            var expected = LuoyangFormalEconomySystem.ComputeAuthorityHash(runtime);
            runtime.Households[0].FoodReserveMilliunits++;
            var root = Path.Combine(Path.GetTempPath(),
                "mandate-luoyang-authority-" + Guid.NewGuid().ToString("N"));
            try
            {
                var result = new Luoyang184LivingWorldCheckpointStore()
                    .Save(runtime, root);
                var loaded = new Luoyang184LivingWorldCheckpointStore()
                    .Load(result.CheckpointPath);
                Assert.That(LuoyangFormalEconomySystem.ComputeAuthorityHash(loaded),
                    Is.EqualTo(expected));
                AssertAuthorityBalanced(loaded);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void FormalEconomyReplayTests_ThreeRunsHaveIdenticalAuthorityAndProjectionHashes()
        {
            var hashes = Enumerable.Range(0, 3).Select(_ =>
            {
                var runtime = CreateAuthorityRuntime(71_010UL);
                new Luoyang184LivingWorldSystem(AuthoritySource())
                    .AdvanceTo(runtime, 30);
                AssertAuthorityBalanced(runtime);
                return LuoyangFormalEconomySystem.ComputeAuthorityHash(runtime) +
                       ":" + runtime.FormalEconomy.ProjectionHash;
            }).ToArray();
            Assert.That(hashes.Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Luoyang700kNormalSupplyBaselineTests_ThirtyDaysAvoidTechnicalUniversalShortage()
        {
            var runtime = CreateAuthorityRuntime(71_011UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 30);
            WriteNormalSupplyEvidence(runtime, "normal_supply_30d");
            AssertMarketDemandAccounting(runtime);
            Assert.That(runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                Is.LessThan(runtime.Households.Count));
            Assert.That(runtime.FormalEconomy.CumulativeConsumedMilliunits,
                Is.GreaterThan(0));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void Luoyang700kAuthorityOnlyControlTests_ThirtyDaysReproducesUniversalShortageBeforeCalibration()
        {
            var source = AuthoritySource();
            var system = new Luoyang184LivingWorldSystem(source,
                LuoyangNormalSupplyCalibrationProfileState
                    .CreateAuthorityOnly());
            var runtime = system.CreateRuntime(71_014UL);
            system.AdvanceTo(runtime, 30);
            WriteNormalSupplyEvidence(runtime, "authority_only_30d");
            Assert.That(runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                Is.EqualTo(runtime.Households.Count));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void Luoyang700kOneYearEconomyTests_AgricultureMarketFreightConsumptionAndConservationContinue()
        {
            var runtime = CreateAuthorityRuntime(71_012UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 365);
            WriteNormalSupplyEvidence(runtime, "normal_supply_365d");
            AssertMarketDemandAccounting(runtime);
            Assert.That(runtime.FormalEconomy.CumulativeHarvestedMilliunits,
                Is.GreaterThan(0));
            Assert.That(runtime.FormalEconomy.CumulativeConsumedMilliunits,
                Is.GreaterThan(0));
            Assert.That(runtime.FormalEconomy.CumulativeMarketTransferredMilliunits,
                Is.GreaterThan(0));
            Assert.That(runtime.FormalEconomy.CumulativeFreightDispatchedMilliunits,
                Is.GreaterThan(0));
            Assert.That(runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                Is.LessThan(runtime.Households.Count));
            AssertAuthorityBalanced(runtime);
        }

        [Test]
        public void LuoyangEconomyAuthorityPerformanceTests_BatchesAndTransactionsRemainBounded()
        {
            var stopwatch = Stopwatch.StartNew();
            var runtime = CreateAuthorityRuntime(71_013UL);
            new Luoyang184LivingWorldSystem(AuthoritySource()).AdvanceTo(runtime, 30);
            stopwatch.Stop();
            Console.WriteLine(string.Join(" ",
                "EVIDENCE authority_performance",
                "elapsed_ms=" + stopwatch.ElapsedMilliseconds,
                "batches=" + runtime.FormalEconomy.ProductBatches.Count,
                "transactions=" +
                    runtime.FormalEconomy.InventoryTransactions.Count,
                "peak_managed_bytes=" +
                    runtime.FormalEconomy.PeakManagedMemoryBytes,
                "projection_rebuild_ms=" +
                    runtime.FormalEconomy.ProjectionRebuildMilliseconds));
            Assert.That(runtime.FormalEconomy.ProductBatches.Count,
                Is.LessThan(20_000));
            Assert.That(runtime.FormalEconomy.InventoryTransactions.Count,
                Is.LessThan(20_000));
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(300)));
            Assert.That(runtime.FormalEconomy.CompactPhysicalMutationCount,
                Is.Zero);
            AssertAuthorityBalanced(runtime);
        }

        private static Luoyang184OuterSupplyRemediationPopulationSource
            AuthoritySource() =>
            new Luoyang184OuterSupplyRemediationPopulationSource(
                RemediationRoot);

        private static Luoyang184LivingWorldRuntimeState CreateAuthorityRuntime(
            ulong seed) =>
            new Luoyang184LivingWorldSystem(AuthoritySource())
                .CreateRuntime(seed);

        private static void AssertAuthorityBalanced(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var audit = new LuoyangFormalEconomySystem().Audit(runtime);
            Console.WriteLine(string.Join(" ",
                "EVIDENCE authority_audit",
                "day=" + runtime.AbsoluteDay,
                "unknown=" + audit.UnknownPhysicalDeltaCount,
                "invalid=" + audit.InvalidBatchCount,
                "claim_difference=" + audit.HouseholdClaimDifferenceMilliunits,
                "projection_difference=" + audit.ProjectionDifferenceMilliunits,
                "batches=" + audit.BatchCount,
                "transactions=" + audit.TransactionCount));
            var unbound = runtime.Inventories.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId) &&
                    !runtime.FormalEconomy.InventoryBindings.Exists(binding =>
                        binding.SourceId == item.Id))
                .OrderBy(item => item.Id).ToArray();
            Console.WriteLine("EVIDENCE authority_unbound total=" +
                              unbound.Sum(item => item.QuantityMilliunits) +
                              " ids=" + string.Join(",", unbound.Select(item =>
                                  item.Id + ":" + item.QuantityMilliunits)));
            Console.WriteLine(string.Join(" ",
                "EVIDENCE authority_totals",
                "formal=" + audit.FormalFoodQuantityMilliunits,
                "projected=" + audit.ProjectedFoodQuantityMilliunits,
                "household_formal=" + audit.HouseholdFormalQuantityMilliunits,
                "household_claim=" + audit.HouseholdClaimQuantityMilliunits));
            var duplicateContainers = runtime.FormalEconomy.InventoryBindings
                .GroupBy(item => item.InventoryContainerId)
                .Where(group => group.Select(item => item.SourceId).Distinct()
                    .Count() > 1).ToArray();
            var freightBindings = runtime.FormalEconomy.InventoryBindings
                .Where(item => item.ProjectionKind ==
                    LuoyangFormalInventoryProjectionKind.FreightCargo).ToArray();
            Console.WriteLine("EVIDENCE authority_binding_diagnostics " +
                              "duplicate_container_sources=" +
                              duplicateContainers.Length + " freight_bindings=" +
                              freightBindings.Length + " freight_projected=" +
                              freightBindings.Sum(item =>
                                  LuoyangFormalEconomyDomain.Quantity(
                                      runtime.FormalEconomy,
                                      item.InventoryContainerId,
                                      item.ProductId)));
            Assert.That(audit.UnknownPhysicalDeltaCount, Is.Zero);
            Assert.That(audit.InvalidBatchCount, Is.Zero);
            Assert.That(audit.HouseholdClaimDifferenceMilliunits, Is.Zero);
            Assert.That(audit.ProjectionDifferenceMilliunits, Is.Zero);
            Assert.That(runtime.FormalEconomy.ProjectionRevision,
                Is.EqualTo(runtime.FormalEconomy.Revision));
        }

        private static void WriteNormalSupplyEvidence(
            Luoyang184LivingWorldRuntimeState runtime, string label)
        {
            var formal = runtime.FormalEconomy;
            var demand = runtime.Households.Sum(item =>
                item.CumulativeFoodDemandMilliunits);
            var consumed = runtime.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var shortage = runtime.Households.Sum(item =>
                item.CumulativeFoodShortageMilliunits);
            var stock = formal.ProductBatches.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(
                        item.ProductDefinitionId))
                .Sum(item => item.Quantity);
            var prices = runtime.Markets.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .Select(item => item.CurrentPriceBasisPoints).ToArray();
            Console.WriteLine(string.Join(" ",
                "EVIDENCE", label,
                "day=" + runtime.AbsoluteDay,
                "opening=" + formal.OpeningFoodMilliunits,
                "stock=" + stock,
                "days_of_supply=" +
                    (runtime.DailyFoodDemandMilliunits <= 0 ? 0 :
                        stock / runtime.DailyFoodDemandMilliunits),
                "demand=" + demand,
                "consumed=" + consumed,
                "shortage=" + shortage,
                "shortfall_households=" + runtime.Households.Count(item =>
                    item.CumulativeFoodShortageMilliunits > 0),
                "current_shortfall_households=" + runtime.Households.Count(
                    item => item.FoodSecurityBasisPoints < 10_000),
                "harvested=" + formal.CumulativeHarvestedMilliunits,
                "external_produced=" +
                    formal.CumulativeExternalProductionMilliunits,
                "market_transferred=" +
                    formal.CumulativeMarketTransferredMilliunits,
                "freight_dispatched=" +
                    formal.CumulativeFreightDispatchedMilliunits,
                "freight_delivered=" +
                    formal.CumulativeFreightDeliveredMilliunits,
                "transport_loss=" +
                    formal.CumulativeTransportLossMilliunits,
                "tax_transferred=" +
                    formal.CumulativeTaxTransferredMilliunits,
                "production_consumption_bp=" + (consumed <= 0 ? 0 :
                    checked((formal.CumulativeHarvestedMilliunits +
                             formal.CumulativeExternalProductionMilliunits) *
                            10_000 / consumed)),
                "price_min_bp=" + (prices.Length == 0 ? 0 : prices.Min()),
                "price_max_bp=" + (prices.Length == 0 ? 0 : prices.Max()),
                "runtime_ms=" + (runtime.AbsoluteDay >= 365
                    ? runtime.Performance
                        .ThreeHundredSixtyFiveDayMilliseconds
                    : runtime.Performance.ThirtyDayMilliseconds),
                "peak_managed_bytes=" +
                    runtime.Performance.PeakManagedMemoryBytes,
                "batches=" + formal.ProductBatches.Count,
                "transactions=" + formal.InventoryTransactions.Count));
        }

        private static void AssertMarketDemandAccounting(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            Assert.That(runtime.Markets.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .Sum(item => item.DemandMilliunits),
                Is.EqualTo(runtime.DailyFoodDemandMilliunits));
            Assert.That(runtime.Markets.Where(item =>
                    !LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .All(item => item.DemandMilliunits == 0), Is.True);
        }
    }
}
