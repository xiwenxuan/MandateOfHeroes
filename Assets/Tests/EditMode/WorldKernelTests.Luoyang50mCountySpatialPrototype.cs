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
        private static string Luoyang50mWorldMapRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "WorldMap");

        [Test]
        public void Luoyang50mCountyPrototype_BuildsFull512SquareKilometrePartition()
        {
            var prototype = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot).Prototype;

            Assert.That(prototype.Partition.Rows, Is.EqualTo(320));
            Assert.That(prototype.Partition.Columns, Is.EqualTo(640));
            Assert.That(prototype.Partition.PlanningCellCount,
                Is.EqualTo(204800));
            Assert.That(prototype.Partition.ChunkCount, Is.EqualTo(800));
            Assert.That(prototype.Partition.PackedArrayBytes,
                Is.EqualTo(2457600));
            Assert.That(prototype.Partition.Portals.Count, Is.EqualTo(4));
            Assert.That(prototype.SourceRoadStrategicCellCount,
                Is.GreaterThanOrEqualTo(0));
            Assert.That(prototype.SourceWaterStrategicCellCount,
                Is.GreaterThanOrEqualTo(0));
            Assert.That(prototype.FacilityDerivedWaterPlanningCellCount,
                Is.GreaterThan(0));
            Assert.That(prototype.FortificationFacilityCount,
                Is.EqualTo(144));
        }

        [Test]
        public void Luoyang50mCountyPrototype_MigratesEveryFacilityWithoutChangingSourceIdentity()
        {
            var source = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot);
            var prototype = source.Prototype;
            var coverage = new LuoyangFacilityModelCoverageSource(
                Luoyang50mWorldMapRoot);
            var original = new LuoyangBuildingPerformancePlanSource(
                Luoyang50mWorldMapRoot, coverage.Bindings,
                coverage.CombinedCatalog).Plan.Facilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);

            Assert.That(prototype.Facilities.Count, Is.EqualTo(2084));
            Assert.That(prototype.Partition.FacilityPlacements.Count,
                Is.EqualTo(2084));
            Assert.That(prototype.FacilityCountByDistrict.Count,
                Is.EqualTo(6));
            Assert.That(prototype.RoadFacilityCount, Is.EqualTo(359));
            Assert.That(prototype.Facilities.All(item =>
                original.TryGetValue(item.FacilityId, out var value) &&
                value.CellId64 == item.SourceCellId64 &&
                value.FacilityDefinitionId == item.FacilityDefinitionId &&
                value.ModelId == item.ModelId), Is.True);
            Assert.That(prototype.Facilities.All(item => item.PlacementProvenanceId ==
                Luoyang50mCountySpatialPrototypeIds.PlacementProvenanceId),
                Is.True);
            Assert.That(prototype.HistoricalPlacementGateId, Is.EqualTo(
                Luoyang50mCountySpatialPrototypeIds.HistoricalPlacementGateId));
        }

        [Test]
        public void Luoyang50mCountyPrototype_IsDeterministicAndHasNoCandidateCellCollisions()
        {
            var first = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot).Prototype;
            var second = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot).Prototype;

            Assert.That(second.DeterministicHash,
                Is.EqualTo(first.DeterministicHash));
            Assert.That(first.Facilities.Select(item => item.CandidateCell)
                .Distinct().Count(), Is.EqualTo(2084));
            Assert.That(first.SourceAnchorPreservedCount,
                Is.LessThan(first.Facilities.Count));
        }

        [Test]
        public void Luoyang50mCountyPrototype_HotWarmColdIndexesDoNotMutateSpatialFacts()
        {
            var prototype = new Luoyang50mCountySpatialPrototypeSource(
                Luoyang50mWorldMapRoot).Prototype;
            var result = Luoyang50mCountySpatialPrototypeBenchmark.Run(
                prototype, 3);

            Assert.That(result.HotPlanningCellCount, Is.EqualTo(204800));
            Assert.That(result.HotChunkCount, Is.EqualTo(800));
            Assert.That(result.HotFacilityIndexCount, Is.EqualTo(2084));
            Assert.That(result.SpatialStateUnchanged, Is.True);
            Assert.That(result.HotP95Milliseconds, Is.GreaterThanOrEqualTo(0));
        }
    }
}
