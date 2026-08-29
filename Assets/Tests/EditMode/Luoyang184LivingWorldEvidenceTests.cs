using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangFoodConservation_CompatibilityFoodIsPhysicalSource()
        {
            var runtime = new Luoyang184LivingWorldRuntimeState
            {
                AbsoluteDay = 1
            };
            runtime.Households.Add(new LuoyangHouseholdConsumptionState
            {
                HouseholdId = "household.test",
                CumulativeFoodConsumedMilliunits = 10
            });
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.test.compatibility.source",
                Day = 1,
                OperationId = "supply.shipment_delivered",
                ProductId = LuoyangFoodConservationAuditor
                    .CompatibilityFoodProductId,
                QuantityMilliunits = 10
            });

            var audit = new LuoyangFoodConservationAuditor().Audit(runtime);

            Assert.That(audit.LegacyBoundaryDifferenceMilliunits,
                Is.EqualTo(-10));
            Assert.That(audit.DifferenceMilliunits, Is.Zero);
            Assert.That(audit.Balanced, Is.True);
            Assert.That(audit.UnknownPhysicalDeltaCount, Is.Zero);
        }

        [Test]
        public void FoodConservation_UnknownPhysicalDeltaFailsAudit()
        {
            var runtime = new Luoyang184LivingWorldRuntimeState();
            runtime.Inventories.Add(new LuoyangInventoryBalanceState
            {
                Id = "inventory.test.unknown",
                ProductId = "product.food.test",
                QuantityMilliunits = 10,
                CapacityMilliunits = 10
            });
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.test.unknown",
                OperationId = "test.unclassified.physical_change",
                ProductId = "product.food.test",
                DestinationInventoryId = "inventory.test.unknown",
                QuantityMilliunits = 10
            });

            var audit = new LuoyangFoodConservationAuditor().Audit(runtime);

            Assert.That(audit.DifferenceMilliunits, Is.EqualTo(-10));
            Assert.That(audit.UnknownPhysicalDeltaCount, Is.EqualTo(1));
            Assert.That(audit.Balanced, Is.False);
        }

        [Test]
        public void FormalFoodConservation_AllInventoryTransactionTypesClassify()
        {
            foreach (InventoryTransactionType type in Enum.GetValues(
                         typeof(InventoryTransactionType)))
                Assert.That(FormalFoodConservationAuditor.Classify(type),
                    Is.Not.EqualTo(FoodConservationTransactionClass.Unknown),
                    type.ToString());
        }

        [Test]
        public void LuoyangFoodConservation_LegacyBoundaryFirstDivergesOnDay0()
        {
            var system = LuoyangLivingWorldTestFixture.System;
            var runtime = system.CreateRuntime(184UL);
            var auditor = new LuoyangFoodConservationAuditor();
            var openingAudit = auditor.Audit(runtime);
            long firstDivergenceDay = openingAudit
                .LegacyBoundaryDifferenceMilliunits == 0 ? -1 : 0;
            var firstDifference = openingAudit.LegacyBoundaryDifferenceMilliunits;
            for (var day = 1; day <= 30; day++)
            {
                system.AdvanceTo(runtime, day);
                var audit = auditor.Audit(runtime);
                Assert.That(audit.DifferenceMilliunits, Is.Zero,
                    "Corrected physical boundary diverged on day " + day + ".");
                if (firstDivergenceDay >= 0 ||
                    audit.LegacyBoundaryDifferenceMilliunits == 0) continue;
                firstDivergenceDay = day;
                firstDifference = audit.LegacyBoundaryDifferenceMilliunits;
            }

            Assert.That(firstDivergenceDay, Is.Zero);
            Assert.That(firstDifference, Is.EqualTo(970_000));
            Assert.That(runtime.InventoryFlows.Any(item =>
                item.Day == firstDivergenceDay && item.OperationId ==
                "scenario.opening.household_food_allocation" &&
                item.ProductId == "product.food.millet_grain" &&
                item.DestinationInventoryId == LuoyangFoodConservationAuditor
                    .CompactHouseholdInventoryId), Is.True);
            Assert.That(runtime.MarketTrades.Any(item =>
                item.Day == 12 &&
                item.ProductId == LuoyangFoodConservationAuditor
                    .CompatibilityFoodProductId), Is.True);
            var firstFingerprint = FoodAuditFingerprint(
                auditor.Audit(runtime));
            var secondFingerprint = FoodAuditFingerprint(
                auditor.Audit(runtime));
            Assert.That(secondFingerprint, Is.EqualTo(firstFingerprint));
        }

        [Test]
        public void FormalFoodConservation_FormalizedBatchesReplayToClosingStock()
        {
            var content = LoadHanFoodProductionContent();
            var world = VillagePrototypeFactory.Create(200, 25_901);
            world.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(world);

            var beforeAudit = WorldSnapshotSerializer.Serialize(world, content);
            GC.Collect();
            var managedBefore = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();
            var audit = new FormalFoodConservationAuditor()
                .Audit(world, content);
            stopwatch.Stop();
            var managedDelta = Math.Max(0,
                GC.GetTotalMemory(false) - managedBefore);
            var afterAudit = WorldSnapshotSerializer.Serialize(world, content);

            if (string.Equals(Environment.GetEnvironmentVariable(
                    "MANDATE_WRITE_FOOD_CONSERVATION_EVIDENCE"), "1",
                    StringComparison.Ordinal))
            {
                var root = Path.Combine(Directory.GetCurrentDirectory(), "tmp",
                    "food-conservation");
                Luoyang184LivingWorldEvidenceStore.Write(Path.Combine(root,
                    "product-ledger.json"), new
                    {
                        schema = audit.Schema,
                        world_day = audit.WorldDay,
                        authority_mode = audit.AuthorityMode.ToString(),
                        difference = audit.Difference,
                        products = audit.Products
                    });
                Luoyang184LivingWorldEvidenceStore.Write(Path.Combine(root,
                    "inventory-ledger.json"), new
                    {
                        schema = audit.Schema,
                        world_day = audit.WorldDay,
                        inventories = audit.Inventories
                    });
                Luoyang184LivingWorldEvidenceStore.Write(Path.Combine(root,
                    "batch-trace.json"), new
                    {
                        schema = audit.Schema,
                        duplicate_batch_id_count = audit.DuplicateBatchIdCount,
                        negative_batch_count = audit.NegativeBatchCount,
                        invalid_reserved_quantity_count =
                            audit.InvalidReservedQuantityCount,
                        missing_batch_reference_count =
                            audit.MissingBatchReferenceCount,
                        batches = audit.Batches
                    });
                Luoyang184LivingWorldEvidenceStore.Write(Path.Combine(root,
                    "transaction-classification.json"), new
                    {
                        schema = audit.Schema,
                        unknown_physical_delta_count =
                            audit.UnknownPhysicalDeltaCount,
                        internal_transfer_imbalance_count =
                            audit.InternalTransferImbalanceCount,
                        reservation_physical_delta_count =
                            audit.ReservationPhysicalDeltaCount,
                        duplicate_transaction_id_count =
                            audit.DuplicateTransactionIdCount,
                        transactions = audit.Transactions
                    });
                Luoyang184LivingWorldEvidenceStore.Write(Path.Combine(root,
                    "auditor-performance.json"), new
                    {
                        schema = "mandate.food-conservation-auditor-performance.v1",
                        elapsed_milliseconds = stopwatch.ElapsedMilliseconds,
                        managed_memory_delta_bytes = managedDelta,
                        product_count = audit.Products.Count,
                        inventory_count = audit.Inventories.Count,
                        batch_count = audit.Batches.Count,
                        transaction_count = audit.Transactions.Count,
                        output_bytes = new[]
                        {
                            "product-ledger.json",
                            "inventory-ledger.json",
                            "batch-trace.json",
                            "transaction-classification.json"
                        }.Sum(file => new FileInfo(Path.Combine(root, file)).Length)
                    });
            }

            Assert.That(afterAudit, Is.EqualTo(beforeAudit),
                "The read-only auditor changed authoritative world state.");
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30_000));
            Assert.That(managedDelta, Is.LessThan(256L * 1024L * 1024L));
            Assert.That(audit.AuthorityMode, Is.EqualTo(
                FoodInventoryAuthorityMode.FormalProductBatches));
            Assert.That(audit.Difference, Is.Zero);
            Assert.That(audit.Products.All(item => item.Difference == 0),
                Is.True);
            Assert.That(audit.Batches.All(item => item.Difference == 0),
                Is.True);
            Assert.That(audit.UnknownPhysicalDeltaCount, Is.Zero);
            Assert.That(audit.InternalTransferImbalanceCount, Is.Zero);
            Assert.That(audit.ReservationPhysicalDeltaCount, Is.Zero);
            Assert.That(audit.Balanced, Is.True);
        }

        [Test]
        public void FormalFoodConservation_ThirtyDayRuntimeAndSaveLoadStayBalanced()
        {
            var content = LoadHanFoodProductionContent();
            var continuous = VillagePrototypeFactory.Create(200, 25_902);
            var resumed = VillagePrototypeFactory.Create(200, 25_902);
            continuous.ProductionContentManifest = content.CreateManifest();
            resumed.ProductionContentManifest = content.CreateManifest();
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(continuous);
            new FoodStockFormalizationSystem(content)
                .FormalizeLegacyStocks(resumed);

            new WorldSimulator(continuous.MasterSeed, content)
                .AdvanceDays(continuous, 30);
            new WorldSimulator(resumed.MasterSeed, content)
                .AdvanceDays(resumed, 15);
            resumed = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(resumed, content), content);
            new WorldSimulator(resumed.MasterSeed, content)
                .AdvanceDays(resumed, 15);

            var auditor = new FormalFoodConservationAuditor();
            var continuousAudit = auditor.Audit(continuous, content);
            var resumedAudit = auditor.Audit(resumed, content);
            Assert.That(continuousAudit.Balanced, Is.True);
            Assert.That(resumedAudit.Balanced, Is.True);
            Assert.That(resumedAudit.Difference, Is.Zero);
            Assert.That(WorldSnapshotSerializer.Serialize(resumed, content),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(
                    continuous, content)));
        }

        [Test]
        public void LuoyangCheckpoint_DeterministicStateDigestExcludesOnlyPerformance()
        {
            var runtime = new Luoyang184LivingWorldRuntimeState
            {
                AbsoluteDay = 30,
                MasterSeed = 184UL
            };
            runtime.Inventories.Add(new LuoyangInventoryBalanceState
            {
                Id = "inventory.test.food",
                ProductId = "product.food.millet_grain",
                QuantityMilliunits = 1_000,
                CapacityMilliunits = 2_000
            });
            var original = Luoyang184LivingWorldCheckpointStore
                .ComputeDeterministicStateSha256(runtime);

            runtime.Performance.InitializationMilliseconds = 9_999;
            runtime.Performance.ThirtyDayMilliseconds = 8_888;
            runtime.Performance.PeakManagedMemoryBytes = 7_777;
            var performanceChanged = Luoyang184LivingWorldCheckpointStore
                .ComputeDeterministicStateSha256(runtime);

            runtime.Inventories[0].QuantityMilliunits++;
            var authorityChanged = Luoyang184LivingWorldCheckpointStore
                .ComputeDeterministicStateSha256(runtime);

            Assert.That(performanceChanged, Is.EqualTo(original));
            Assert.That(authorityChanged, Is.Not.EqualTo(original));
        }

        [Test]
        public void LuoyangLiving_EvidenceExports365DayClosureAndCheckpoint()
        {
            var project = Directory.GetCurrentDirectory();
            var output = Path.Combine(project, "outputs",
                "LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1");
            Directory.CreateDirectory(output);
            var evidencePath = Path.Combine(output, "runtime_evidence.json");
            if (!string.Equals(Environment.GetEnvironmentVariable(
                    "MANDATE_WRITE_LUOYANG_LIVING_EVIDENCE"), "1",
                    StringComparison.Ordinal))
            {
                Assert.That(File.Exists(evidencePath), Is.True,
                    "Set MANDATE_WRITE_LUOYANG_LIVING_EVIDENCE=1 for an explicit evidence refresh.");
                return;
            }
            var source = LuoyangLivingWorldTestFixture.Source;
            var system = LuoyangLivingWorldTestFixture.System;
            var timings = new[] { 1, 7, 30, 365 }.Select(days =>
            {
                var stopwatch = Stopwatch.StartNew();
                var measured = system.CreateRuntime(184UL);
                system.AdvanceTo(measured, days);
                stopwatch.Stop();
                return new
                {
                    days,
                    elapsed_milliseconds = stopwatch.ElapsedMilliseconds,
                    peak_managed_memory_bytes = measured.Performance
                        .PeakManagedMemoryBytes
                };
            }).ToArray();
            var runtime = LuoyangLivingWorldTestFixture.Day365;
            var checkpointStore = new Luoyang184LivingWorldCheckpointStore();
            var checkpoint = checkpointStore.Save(runtime,
                Path.Combine(output, "runtime_checkpoint"));
            var loaded = checkpointStore.Load(checkpoint.CheckpointPath);
            Luoyang184LivingWorldRules.ValidateRuntime(loaded,
                400000, 80899, 2084);

            var world = WorldState.Create(184);
            var historical = Path.Combine(project, "Assets", "StreamingAssets",
                "HistoricalPersons", "Han135260V1");
            new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                LuoyangLivingWorldTestFixture.MetropolitanRoot, historical)
                .Integrate(world);
            var checkpointRelative = checkpoint.CheckpointPath
                .Substring(project.Length + 1).Replace('\\', '/');
            system.AttachSummary(world, loaded, checkpointRelative,
                checkpoint.Sha256);
            File.WriteAllText(Path.Combine(output, "world_v70_summary.json"),
                WorldSnapshotSerializer.Serialize(world),
                new UTF8Encoding(false));

            var imported = loaded.InventoryFlows.Where(item =>
                    (item.OperationId == "scenario.opening.delivered_stock" ||
                     item.OperationId == "supply.shipment_delivered") &&
                    LuoyangFoodConservationAuditor.IsPhysicalFoodProduct(
                        item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var harvested = loaded.InventoryFlows.Where(item =>
                    item.OperationId == "production.crop_harvest" &&
                    LuoyangFoodConservationAuditor.IsPhysicalFoodProduct(
                        item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var processingLoss = loaded.InventoryFlows.Where(item =>
                    item.OperationId == "production.recipe_settlement" &&
                    LuoyangFoodConservationAuditor.IsPhysicalFoodProduct(
                        item.ProductId))
                .Sum(item => item.LossMilliunits);
            var consumed = loaded.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var closingInventory = loaded.Inventories.Where(item =>
                    LuoyangFoodConservationAuditor.IsPhysicalFoodProduct(
                        item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var closingHouseholdReserve = loaded.Households.Sum(item =>
                item.FoodReserveMilliunits);
            var closing = closingInventory + closingHouseholdReserve;
            var conservationAudit = new LuoyangFoodConservationAuditor()
                .Audit(loaded);
            var summary = system.BuildWorldSummary(loaded, checkpointRelative,
                checkpoint.Sha256);
            var evidence = new
            {
                schema = "mandate.luoyang-184.person-work-production-consumption-closure-evidence.v1",
                generated_at = DateTimeOffset.Now.ToString("O"),
                protected_counts = new
                {
                    persons = source.PersonCount,
                    households = source.HouseholdCount,
                    facilities = source.FacilityCount,
                    protected_package_digest = source.ProtectedPackageDigest,
                    added_persons = 0,
                    added_households = 0,
                    added_facilities = 0
                },
                summary,
                timings,
                workforce_status = loaded.Workforce.GroupBy(item => item.Status)
                    .OrderBy(item => item.Key).Select(item => new
                    {
                        status = item.Key.ToString(),
                        count = item.Count(),
                        average_effective_labor_basis_points = (long)item.Average(
                            value => value.EffectiveLaborBasisPoints)
                    }),
                workforce_age_bands = loaded.Workforce.GroupBy(item =>
                        item.Age < 14 ? "00-13" : item.Age < 20 ? "14-19" :
                        item.Age < 40 ? "20-39" : item.Age < 60 ? "40-59" :
                        item.Age < 70 ? "60-69" : "70+")
                    .OrderBy(item => item.Key).Select(item => new
                    {
                        age_band = item.Key,
                        count = item.Count()
                    }),
                workforce_sample = loaded.Workforce.Where(item =>
                        item.PersonOrdinal < 1000 || item.PersonOrdinal >= 399000)
                    .ToList(),
                facilities = loaded.Facilities,
                crops = loaded.Crops,
                inventories = loaded.Inventories,
                inventory_flows = loaded.InventoryFlows,
                household_summary = new
                {
                    count = loaded.Households.Count,
                    demand_milliunits = loaded.Households.Sum(item =>
                        item.CumulativeFoodDemandMilliunits),
                    consumed_milliunits = consumed,
                    shortage_milliunits = loaded.Households.Sum(item =>
                        item.CumulativeFoodShortageMilliunits),
                    shortage_households = loaded.Households.Count(item =>
                        item.CumulativeFoodShortageMilliunits > 0)
                },
                households = loaded.Households,
                markets = loaded.Markets,
                shortage_responses = loaded.ShortageResponses,
                day_snapshots = loaded.DaySnapshots,
                conservation = new
                {
                    imported_food_milliunits = imported,
                    harvested_food_milliunits = harvested,
                    consumed_food_milliunits = consumed,
                    closing_food_milliunits = closing,
                    closing_inventory_milliunits = closingInventory,
                    closing_household_reserve_milliunits =
                        closingHouseholdReserve,
                    processing_loss_milliunits = processingLoss,
                    left_milliunits = imported + harvested,
                    right_milliunits = consumed + closing + processingLoss,
                    difference_milliunits = conservationAudit
                        .DifferenceMilliunits,
                    legacy_boundary_difference_milliunits = conservationAudit
                        .LegacyBoundaryDifferenceMilliunits,
                    unknown_physical_delta_count = conservationAudit
                        .UnknownPhysicalDeltaCount,
                    balanced = conservationAudit.Balanced
                },
                checkpoint = new
                {
                    path = checkpoint.CheckpointPath,
                    checkpoint.Bytes,
                    checkpoint.Sha256,
                    checkpoint.DeterministicStateSha256,
                    round_trip_counts_match = loaded.Workforce.Count ==
                        runtime.Workforce.Count && loaded.Households.Count ==
                        runtime.Households.Count && loaded.Facilities.Count ==
                        runtime.Facilities.Count
                }
            };
            Luoyang184LivingWorldEvidenceStore.Write(evidencePath, evidence);

            Assert.That(File.Exists(evidencePath), Is.True);
            Assert.That(conservationAudit.DifferenceMilliunits, Is.Zero);
            Assert.That(conservationAudit.UnknownPhysicalDeltaCount, Is.Zero);
            Assert.That(conservationAudit.Balanced, Is.True);
            Assert.That(loaded.Workforce.Count, Is.EqualTo(400000));
        }

        private static string FoodAuditFingerprint(
            LuoyangFoodConservationAuditState audit)
        {
            var builder = new StringBuilder();
            builder.Append(audit.WorldDay).Append('|')
                .Append(audit.SourceMilliunits).Append('|')
                .Append(audit.HouseholdConsumedMilliunits).Append('|')
                .Append(audit.MilitaryConsumedMilliunits).Append('|')
                .Append(audit.ProcessingLossMilliunits).Append('|')
                .Append(audit.ClosingInventoryMilliunits).Append('|')
                .Append(audit.ClosingHouseholdReserveMilliunits).Append('|')
                .Append(audit.DifferenceMilliunits).Append('|')
                .Append(audit.LegacyBoundaryDifferenceMilliunits).Append('|')
                .Append(audit.UnknownPhysicalDeltaCount);
            foreach (var product in audit.Products)
                builder.Append('|').Append(product.ProductId).Append(':')
                    .Append(product.SourceMilliunits).Append(':')
                    .Append(product.ClosingInventoryMilliunits).Append(':')
                    .Append(product.ClosingCompatibilityReserveMilliunits)
                    .Append(':').Append(product.ConsumedMilliunits).Append(':')
                    .Append(product.ProcessingLossMilliunits);
            foreach (var transaction in audit.Transactions)
                builder.Append('|').Append(transaction.FlowId).Append(':')
                    .Append(transaction.Day).Append(':')
                    .Append(transaction.OperationId).Append(':')
                    .Append(transaction.ProductId).Append(':')
                    .Append(transaction.QuantityMilliunits).Append(':')
                    .Append(transaction.ExplicitPhysicalLossMilliunits)
                    .Append(':').Append((int)transaction.Classification);
            return builder.ToString();
        }
    }
}
