using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class LuoyangWorldV1Tests
    {
        private static string WorldRoot => Path.Combine(Application.dataPath, "StreamingAssets", "WorldMap", "HanWorldV1");
        private static string LuoyangRoot => Path.Combine(Application.dataPath, "StreamingAssets", "WorldMap", "LuoyangWorldV1");

        [Test]
        public void GridAlignmentTests()
        {
            using var reader = new WorldMapDataReader(WorldRoot);
            Assert.That(reader.Manifest.GridVersion, Is.EqualTo("HanWorldV1"));
            Assert.That(reader.Manifest.GridSchemaVersion, Is.EqualTo("hanworld.square-grid.v1"));
            Assert.That(reader.Grid.Columns, Is.EqualTo(3314));
            Assert.That(reader.Grid.Rows, Is.EqualTo(2176));
            Assert.That(reader.Grid.CellCount, Is.EqualTo(7_211_264UL));
        }

        [Test]
        public void PopulationToCellCapacityTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.PopulationProfile.TotalPersons, Is.EqualTo(20_542));
            Assert.That(world.PopulationProfile.DevelopedCells, Is.LessThan(world.PopulationProfile.DevelopableCells));
            Assert.That(world.PopulationProfile.UnusedDevelopableCells, Is.GreaterThan(3_000));
        }

        [Test]
        public void FacilityWorkerCapacityTests()
        {
            var cells = new LuoyangWorldPrototypeReader(LuoyangRoot).World.Cells.Where(item => item.FacilityId != null);
            Assert.That(cells.All(item => item.CurrentWorkers <= item.WorkerCapacity), Is.True);
        }

        [Test]
        public void ResidentialCapacityTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.PopulationProfile.ResidentialCapacity, Is.GreaterThanOrEqualTo(world.PopulationProfile.TotalPersons));
            Assert.That(world.Cells.Where(item => item.ResidentialCapacity > 0)
                .All(item => item.Population <= item.ResidentialCapacity), Is.True);
        }

        [Test]
        public void HouseholdResidenceTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.PopulationProfile.TotalHouseholds, Is.GreaterThan(4_000));
            Assert.That(world.Cells.Sum(item => item.Households), Is.EqualTo(world.PopulationProfile.TotalHouseholds));
        }

        [Test]
        public void AgriculturalLaborAllocationTests()
        {
            var farms = new LuoyangWorldPrototypeReader(LuoyangRoot).World.Facilities
                .Where(item => item.Category == "agriculture").ToArray();
            Assert.That(farms.Length, Is.GreaterThan(300));
            Assert.That(farms.All(item => item.NormalWorkers <= item.PeakWorkers && item.PeakWorkers <= item.WorkerCapacity), Is.True);
            Assert.That(farms.Any(item => item.MaturityPercent >= 80 && item.GrowthStage == "early_harvest_allowed"), Is.True);
        }

        [Test]
        public void CellOwnershipTests()
        {
            var address = new WorldMapCellAddress("hanworld.square-grid.v1", 100, 200, 200UL * 3314UL + 100UL);
            var state = new WorldCellOccupancyState(address);
            state.TransferOwner("household.owner");
            Assert.Throws<InvalidOperationException>(() => state.BuildFacility("household.other", "facility.invalid"));
            state.BuildFacility("household.owner", "facility.valid");
            Assert.That(state.FacilityId, Is.EqualTo("facility.valid"));
        }

        [Test]
        public void SingleFacilityPerCellTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.Facilities.Select(item => item.CellId64).Distinct().Count(), Is.EqualTo(world.Facilities.Count));
            var state = new WorldCellOccupancyState(new WorldMapCellAddress("hanworld.square-grid.v1", 1, 1, 3315));
            state.TransferOwner("owner");
            state.BuildFacility("owner", "facility.one");
            Assert.Throws<InvalidOperationException>(() => state.BuildFacility("owner", "facility.two"));
        }

        [Test]
        public void ForceSingleOccupancyTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.Forces.Select(item => item.CellId64).Distinct().Count(), Is.EqualTo(world.Forces.Count));
            var state = new WorldCellOccupancyState(new WorldMapCellAddress("hanworld.square-grid.v1", 1, 1, 3315));
            state.PlaceForce("force.one");
            Assert.Throws<InvalidOperationException>(() => state.PlaceForce("force.two"));
        }

        [Test]
        public void CellId64Tests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.Cells.All(item => item.CellId64 == (ulong)item.GridY * (ulong)world.Columns + (ulong)item.GridX), Is.True);
            var huge = WorldMapCellId.FromRowColumn(1_500_000, 1_500_000, 2_000_000);
            Assert.That(huge.Value, Is.GreaterThan(uint.MaxValue));
        }

        [Test]
        public void CityFootprintTests()
        {
            var world = new LuoyangWorldPrototypeReader(LuoyangRoot).World;
            Assert.That(world.CityId, Is.EqualTo("C027"));
            Assert.That(world.CityFootprintCellIds.Count, Is.GreaterThan(10));
            Assert.That(world.CityFootprintCellIds.Distinct().Count(), Is.EqualTo(world.CityFootprintCellIds.Count));
        }

        [Test]
        public void ChunkStreamingTests()
        {
            using var reader = new WorldMapDataReader(WorldRoot);
            var snapshot = reader.ReadChunkSnapshot(20, 20);
            Assert.That(snapshot.Cells.Length, Is.EqualTo(snapshot.RowCount * snapshot.ColumnCount));
            Assert.That(snapshot.GetLocal(0, 0).GridSchemaVersion, Is.EqualTo(reader.Grid.GridSchemaVersion));
        }

        [Test]
        public void CellQueryBenchmarkV1()
        {
            var random = new System.Random(140);
            var stopwatch = Stopwatch.StartNew();
            using var reader = new WorldMapDataReader(WorldRoot);
            for (var index = 0; index < 500; index++)
                reader.ReadCell(random.Next(reader.Grid.Rows), random.Next(reader.Grid.Columns));
            var coldRandom = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            for (var index = 0; index < 5_000; index++)
                reader.ReadCell(random.Next(reader.Grid.Rows), random.Next(reader.Grid.Columns));
            var warmRandom = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            for (var index = 0; index < 10_000; index++) reader.ReadCell(1000, 1000 + index % 100);
            var sequential = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            var batch = reader.ReadChunkSnapshot(15, 15);
            var batchMs = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            ulong checksum = 0;
            for (var index = 0; index < 100_000; index++) checksum ^= batch.Cells[index % batch.Cells.Length].CellId64;
            var cachedChunk = stopwatch.Elapsed.TotalMilliseconds;
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tmp", "unity-validation", "cell-query-benchmark-v1.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,
                $"{{\n  \"cold_random_500_ms\": {coldRandom:F3},\n  \"warm_random_5000_ms\": {warmRandom:F3},\n" +
                $"  \"sequential_10000_ms\": {sequential:F3},\n  \"batch_chunk_ms\": {batchMs:F3},\n" +
                $"  \"cached_chunk_100000_ms\": {cachedChunk:F3},\n  \"checksum\": {checksum}\n}}");
            Assert.That(File.Exists(output), Is.True);
        }
    }
}
