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
                    item.OperationId == "supply.reference_arrival" &&
                    IsClosureFood(item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var harvested = loaded.InventoryFlows.Where(item =>
                    item.OperationId == "production.crop_harvest" &&
                    IsClosureFood(item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var processingLoss = loaded.InventoryFlows.Where(item =>
                    item.OperationId == "production.recipe_settlement" &&
                    IsClosureFood(item.ProductId))
                .Sum(item => item.LossMilliunits);
            var consumed = loaded.Households.Sum(item =>
                item.CumulativeFoodConsumedMilliunits);
            var closing = loaded.Inventories.Where(item =>
                IsClosureFood(item.ProductId)).Sum(item => item.QuantityMilliunits);
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
                    processing_loss_milliunits = processingLoss,
                    left_milliunits = imported + harvested,
                    right_milliunits = consumed + closing + processingLoss,
                    balanced = imported + harvested ==
                               consumed + closing + processingLoss
                },
                checkpoint = new
                {
                    path = checkpoint.CheckpointPath,
                    checkpoint.Bytes,
                    checkpoint.Sha256,
                    round_trip_counts_match = loaded.Workforce.Count ==
                        runtime.Workforce.Count && loaded.Households.Count ==
                        runtime.Households.Count && loaded.Facilities.Count ==
                        runtime.Facilities.Count
                }
            };
            Luoyang184LivingWorldEvidenceStore.Write(evidencePath, evidence);

            Assert.That(File.Exists(evidencePath), Is.True);
            Assert.That(imported + harvested,
                Is.EqualTo(consumed + closing + processingLoss));
            Assert.That(loaded.Workforce.Count, Is.EqualTo(400000));
        }

        private static bool IsClosureFood(string productId) =>
            productId == "product.food.millet_grain" ||
            productId == "product.food.wheat_grain" ||
            productId == "product.food.broomcorn_grain" ||
            productId == "product.food.bean" ||
            productId == CoreProductionContent.WheatFlourProductId ||
            productId == CoreProductionContent.DryRationProductId ||
            productId == CoreProductionContent.FreshMuttonProductId ||
            productId == CoreProductionContent.OffalProductId;
    }
}
