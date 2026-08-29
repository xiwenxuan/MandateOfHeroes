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
        private static string WorldMapRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "WorldMap");

        private static string RemediationRoot => Path.Combine(
            WorldMapRoot, "LuoyangOuterSupplyRemediationV1");

        [Test]
        public void OuterSupplyTargetPopulationTests_MaterializesInclusiveTargetWithValidHouseholdsAndCapacity()
        {
            var stopwatch = Stopwatch.StartNew();
            var source =
                new Luoyang184OuterSupplyRemediationPopulationSource(
                    RemediationRoot);
            Assert.That(source.ValidatePackageFiles(), Is.Empty);
            Assert.That(source.PersonCount, Is.EqualTo(700_000));
            Assert.That(source.AddedPersonCount, Is.EqualTo(300_000));
            Assert.That(source.HouseholdCount, Is.EqualTo(142_980));
            Assert.That(source.AddedHouseholdCount, Is.EqualTo(62_081));
            Assert.That(source.FacilityCount, Is.EqualTo(2_779));
            Assert.That(source.AddedFacilityCount, Is.EqualTo(695));
            Assert.That(source.OpenCurrent().PermanentPersonCount,
                Is.EqualTo(700_000));
            Assert.That(source.OpenCurrent().PartitionCount, Is.EqualTo(56));

            var addedFacilities = source.Facilities.Skip(2_084).ToArray();
            Assert.That(addedFacilities.Select(item => item.FacilityId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(695));
            Assert.That(addedFacilities.Select(item => item.CellId64)
                .Distinct().Count(), Is.EqualTo(695));
            Assert.That(addedFacilities.Select(item => item.SettlementId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(33));
            Assert.That(addedFacilities.Sum(item => item.CurrentResidents),
                Is.EqualTo(300_000));
            Assert.That(addedFacilities.Sum(item => item.ResidentCapacity),
                Is.GreaterThanOrEqualTo(300_000));
            Assert.That(addedFacilities.All(item =>
                item.CurrentResidents <= item.ResidentCapacity), Is.True);

            var households = source.ReadHouseholds(80_899, 62_081).ToArray();
            Assert.That(households.Select(item => item.Ordinal).Distinct()
                .Count(), Is.EqualTo(households.Length));
            Assert.That(households.All(item => item.MemberCount > 0 &&
                item.HeadOrdinal >= item.MemberStartOrdinal &&
                item.HeadOrdinal < item.MemberStartOrdinal + item.MemberCount &&
                item.ResidenceFacilityIndex >= 2_084 &&
                item.ResidenceFacilityIndex < source.FacilityCount), Is.True);
            Assert.That(households.Sum(item => (long)item.MemberCount),
                Is.EqualTo(300_000));

            var expectedOrdinal = 400_000u;
            var laborCapable = 0;
            foreach (var person in source.ReadPersons(400_000, 300_000))
            {
                Assert.That(person.Ordinal, Is.EqualTo(expectedOrdinal++));
                Assert.That(person.HouseholdOrdinal,
                    Is.InRange(80_899u, 142_979u));
                Assert.That(person.ResidenceFacilityIndex,
                    Is.InRange(2_084u, 2_778u));
                Assert.That(person.CurrentCellId64,
                    Is.EqualTo(source.Facilities[checked((int)
                        person.ResidenceFacilityIndex)].CellId64));
                if (person.BirthYear <= 170 && person.BirthYear > 114)
                    laborCapable++;
            }
            Assert.That(expectedOrdinal, Is.EqualTo(700_000u));
            Assert.That(laborCapable, Is.EqualTo(217_802));
            Assert.That(source.TryReadCore(
                "person.luoyang.184.outer_supply.700000", out var last),
                Is.True);
            Assert.That(last.FamilyId,
                Does.StartWith("household.luoyang.184."));
            stopwatch.Stop();
            Console.WriteLine("EXPANDED_POPULATION_AUDIT ms=" +
                                  stopwatch.ElapsedMilliseconds +
                                  " persons=700000 households=142980 " +
                                  "facilities=2779 labor_capable=" +
                                  laborCapable);
        }

        [Test]
        public void LegacyFoodDefinitionTests_ResolvesEveryOuterAgricultureProductWithoutSilentRemap()
        {
            var corePath = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "Resources", "Content", "Core", "Production",
                "core-production.json");
            var hanPath = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "Resources", "Content", "Scenario", "HanFood",
                "han-food-production.json");
            var content = ProductionContentRegistry.FromJson(
                File.ReadAllText(corePath), File.ReadAllText(hanPath));
            var catchment = new LuoyangOuterSupplyCatchmentV1Reader(
                Path.Combine(WorldMapRoot, "LuoyangOuterSupplyCatchmentV1"));
            var audit = catchment.Audit();
            Assert.That(audit.CriticalReferencesPassed, Is.True,
                string.Join(",", audit.CriticalReferenceErrors));
            Assert.That(audit.FormalContentBridgeComplete, Is.True);
            Assert.That(audit.UnresolvedContentDefinitionIds, Is.Empty);
            Assert.That(audit.PopulationTargetMaterialized, Is.True);
            var legacyFoods = new[]
            {
                CoreProductionContent.LegacyOuterBeanFoodProductId,
                CoreProductionContent.LegacyOuterBroomcornFoodProductId,
                CoreProductionContent.LegacyOuterMilletFoodProductId
            };
            foreach (var id in legacyFoods)
            {
                Assert.That(content.GetProduct(id), Is.Not.Null);
                Assert.That(content.GetFood(id).OpeningShareBasisPoints,
                    Is.Zero);
                Assert.That(catchment.Definition.ContentIdCrosswalks.Any(item =>
                    item.SourceId == id), Is.False,
                    id + " must remain its own stable product identity.");
            }
            foreach (var field in catchment.Metropolitan.Agriculture)
            {
                var productId = field.ProductDefinitionId ==
                                "product.food.wheat_grain"
                    ? CoreProductionContent.WheatGrainProductId
                    : field.ProductDefinitionId;
                Assert.That(content.GetProduct(productId), Is.Not.Null,
                    field.FieldId);
            }
            Assert.That(catchment.Metropolitan.Agriculture.Count,
                Is.EqualTo(135));
        }

        [Test]
        public void OuterSupplyV78PersistenceTests_WorldSnapshotKeepsPopulationHouseholdsAndResidenceCapacity()
        {
            var metropolitanRoot = Path.Combine(WorldMapRoot,
                "Luoyang184MetropolitanInitializationV1");
            var historicalRoot = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "HistoricalPersons",
                "Han135260V1");
            var world = WorldState.Create(184);
            new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                metropolitanRoot, historicalRoot).Integrate(world);
            var result = new Luoyang184OuterSupplyRemediationBootstrap(
                RemediationRoot).Integrate(world);
            Assert.That(result.PermanentPersonCount, Is.EqualTo(700_000));
            Assert.That(result.HouseholdCount, Is.EqualTo(142_980));
            Assert.That(result.FacilityCount, Is.EqualTo(2_779));
            Assert.That(result.AddedResidenceCapacity,
                Is.GreaterThanOrEqualTo(300_000));
            Assert.That(world.PopulationStorage.PackageId, Is.EqualTo(
                Luoyang184OuterSupplyRemediationPopulationSource
                    .PopulationPackageId));
            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(loaded.PopulationStorage.PermanentPersonCount,
                Is.EqualTo(700_000));
            Assert.That(loaded.Facilities.Count, Is.EqualTo(2_779));
            Assert.That(loaded.Facilities.Skip(2_084).Sum(item =>
                item.ResidentPersonCount), Is.EqualTo(300_000));
            Assert.That(loaded.HistoricalPersonFamilyIntegrations.Single()
                .HouseholdCount, Is.EqualTo(142_980));
            loaded.Validate();
        }

        [Test]
        public void OuterAgricultureSchedulingTests_AllRecordsAdvanceThirtyDaysAndRoundTripSchedule()
        {
            var source =
                new Luoyang184OuterSupplyRemediationPopulationSource(
                    RemediationRoot);
            var system = new Luoyang184LivingWorldSystem(source);
            var runtime = system.CreateRuntime(184UL);
            var oldDemand = 400_000L * 1_000L;
            Assert.That(runtime.Workforce.Count, Is.EqualTo(700_000));
            Assert.That(runtime.Households.Count, Is.EqualTo(142_980));
            Assert.That(runtime.Facilities.Count, Is.EqualTo(2_779));
            Assert.That(runtime.DailyFoodDemandMilliunits,
                Is.GreaterThan(oldDemand));
            Assert.That(runtime.Crops.Count, Is.EqualTo(135));
            Assert.That(runtime.AgricultureDueEntries.Count,
                Is.EqualTo(135));
            Assert.That(runtime.Crops.All(item => item.NextDueDay > 0),
                Is.True);

            system.AdvanceTo(runtime, 30);
            Assert.That(runtime.AgricultureScheduleDispatchCount,
                Is.GreaterThanOrEqualTo(135 * 3));
            Assert.That(runtime.Crops.All(item =>
                item.MaturityBasisPoints > 0 &&
                item.NextDueDay > runtime.AbsoluteDay), Is.True);
            Assert.That(runtime.AgricultureDueEntries.Count,
                Is.EqualTo(135));
            Assert.That(new LuoyangFoodConservationAuditor().Audit(runtime)
                .DifferenceMilliunits, Is.Zero);

            var directory = Path.Combine(Path.GetTempPath(),
                "moh-outer-supply-remediation-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new Luoyang184LivingWorldCheckpointStore();
                var saved = store.Save(runtime, directory);
                var loaded = store.Load(saved.CheckpointPath);
                Assert.That(loaded.Crops.Select(CropScheduleDigest),
                    Is.EqualTo(runtime.Crops.Select(CropScheduleDigest)));
                Assert.That(loaded.AgricultureDueEntries.Select(item =>
                        item.DueDay + ":" + item.CropIndex + ":" +
                        item.ScheduleRevision),
                    Is.EqualTo(runtime.AgricultureDueEntries.Select(item =>
                        item.DueDay + ":" + item.CropIndex + ":" +
                        item.ScheduleRevision)));
                Assert.That(
                    Luoyang184LivingWorldCheckpointStore
                        .ComputeDeterministicStateSha256(loaded),
                    Is.EqualTo(saved.DeterministicStateSha256));
                system.AdvanceTo(loaded, 31);
                Luoyang184LivingWorldRules.ValidateRuntime(loaded,
                    source.PersonCount, source.HouseholdCount,
                    source.FacilityCount);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            Console.WriteLine("EXPANDED_30D init_ms=" +
                runtime.Performance.InitializationMilliseconds +
                " advance_ms=" + runtime.Performance.ThirtyDayMilliseconds +
                " memory=" + runtime.Performance.PeakManagedMemoryBytes +
                " dispatches=" + runtime.AgricultureScheduleDispatchCount +
                " demand=" + runtime.DailyFoodDemandMilliunits);
        }

        [Test]
        public void OuterAgricultureLongRunTests_AllRecordsRunForOneWorldYearWithoutDuplicateHarvest()
        {
            var source =
                new Luoyang184OuterSupplyRemediationPopulationSource(
                    RemediationRoot);
            var system = new Luoyang184LivingWorldSystem(source);
            var runtime = system.CreateRuntime(184UL);
            system.AdvanceTo(runtime, 365);
            Assert.That(runtime.Crops.Count, Is.EqualTo(135));
            Assert.That(runtime.Crops.All(item =>
                item.CumulativeYieldMilliunits > 0 &&
                item.CycleNumber >= 2 &&
                item.NextDueDay > runtime.AbsoluteDay), Is.True);
            var harvests = runtime.InventoryFlows.Where(item =>
                item.OperationId == "production.crop_harvest").ToArray();
            Assert.That(harvests.Select(item => item.Id).Distinct(
                StringComparer.Ordinal).Count(), Is.EqualTo(harvests.Length));
            Assert.That(harvests.Select(item => item.FacilityId).Distinct(
                StringComparer.Ordinal).Count(), Is.EqualTo(135));
            var conservation = new LuoyangFoodConservationAuditor().Audit(
                runtime);
            Assert.That(conservation.DifferenceMilliunits, Is.Zero);
            Console.WriteLine("EXPANDED_1Y advance_ms=" +
                runtime.Performance.ThreeHundredSixtyFiveDayMilliseconds +
                " memory=" + runtime.Performance.PeakManagedMemoryBytes +
                " dispatches=" + runtime.AgricultureScheduleDispatchCount +
                " harvested_farms=" + harvests.Select(item => item.FacilityId)
                    .Distinct(StringComparer.Ordinal).Count() +
                " food_difference=" + conservation.DifferenceMilliunits);
        }

        [Test]
        public void OuterAgricultureReplayTests_ThreeExpandedRunsHaveIdenticalWorldStateHash()
        {
            string expected = null;
            for (var run = 0; run < 3; run++)
            {
                var source =
                    new Luoyang184OuterSupplyRemediationPopulationSource(
                        RemediationRoot);
                var system = new Luoyang184LivingWorldSystem(source);
                var runtime = system.CreateRuntime(184UL);
                system.AdvanceTo(runtime, 30);
                var actual = Luoyang184LivingWorldCheckpointStore
                    .ComputeDeterministicStateSha256(runtime);
                if (expected == null) expected = actual;
                else Assert.That(actual, Is.EqualTo(expected));
                runtime = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static string CropScheduleDigest(
            LuoyangCropRuntimeState crop) => string.Join(":",
            crop.FieldId, crop.CycleNumber, crop.Phase,
            crop.MaturityBasisPoints, crop.NextDueDay,
            crop.ScheduleRevision, crop.CumulativeYieldMilliunits,
            crop.CumulativeStoredYieldMilliunits,
            crop.CumulativeSeedRecoveredMilliunits,
            crop.CumulativeLostYieldMilliunits);
    }
}
