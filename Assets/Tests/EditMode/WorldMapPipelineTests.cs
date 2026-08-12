using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class WorldMapPipelineTests
    {
        private static string PackageRoot => Path.Combine(
            Application.dataPath, "StreamingAssets", "WorldMap", "HanWorldV0");

        [Test]
        public void CellId_RowMajorEncodingIsStableAndReversible()
        {
            const int columns = 4096;
            var id = WorldMapCellId.FromRowColumn(1930, 3313, columns);

            id.Decode(columns, out var row, out var column);

            Assert.That(row, Is.EqualTo(1930));
            Assert.That(column, Is.EqualTo(3313));
            Assert.That(id.Value, Is.EqualTo((ulong)1930 * columns + 3313));
        }

        [Test]
        public void NeighborService_UsesEightDirectionsAndClipsMapEdges()
        {
            var grid = new CellGridIndex(3, 4, 0, 6, 2);
            var service = new CellNeighborService(grid);

            Assert.That(service.GetNeighbors(grid.ToCellId(1, 1)), Has.Count.EqualTo(8));
            Assert.That(service.GetNeighbors(grid.ToCellId(0, 0)), Has.Count.EqualTo(3));
        }

        [Test]
        public void Package_LoadsRealChunkedCellDataWithoutGameObjects()
        {
            using (var reader = new WorldMapDataReader(PackageRoot))
            {
                Assert.That(reader.Manifest.TotalCells, Is.EqualTo((long)reader.Manifest.Rows * reader.Manifest.Columns));
                Assert.That(reader.Manifest.TotalCells, Is.GreaterThan(6000000));
                Assert.That(reader.Manifest.ChunkSize, Is.EqualTo(64));
                var first = reader.ReadCell(800, 1700);
                var again = reader.ReadCell(first.Id);

                Assert.That(again.Id, Is.EqualTo(first.Id));
                Assert.That(again.Elevation, Is.EqualTo(first.Elevation));
                Assert.That(again.CenterX, Is.EqualTo(first.CenterX));
                Assert.That(reader.ReadChunk(12, 26), Has.Length.EqualTo(4096));
                Assert.That(reader.Cities.Features, Has.Count.EqualTo(77));
                Assert.That(reader.Cities.Features.FindAll(feature => feature.Properties.CellId.HasValue), Has.Count.EqualTo(72));
                foreach (var feature in reader.Cities.Features)
                {
                    if (!feature.Properties.CellId.HasValue)
                    {
                        continue;
                    }

                    Assert.That(feature.Properties.CellId.Value, Is.EqualTo(
                        (long)feature.Properties.Row.Value * reader.Manifest.Columns + feature.Properties.Column.Value));
                }
                Assert.That(UnityEngine.Object.FindObjectsOfType<GameObject>().Length, Is.LessThan(100));
            }
        }

        [Test]
        public void RouteCellPaths_AreContinuousEightNeighborWalks()
        {
            using (var reader = new WorldMapDataReader(PackageRoot))
            {
                var json = File.ReadAllText(Path.Combine(PackageRoot, "locations", "road_edges.json"));
                var routes = JsonUtility.FromJson<RoadEdgesFile>(json).routes;
                Assert.That(routes, Has.Length.EqualTo(18));
                foreach (var route in routes)
                {
                    var values = route.cell_ids;
                    Assert.That(values, Is.Not.Empty, $"{route.route_id} has no raster path");
                    for (var index = 1; index < values.Length; index++)
                    {
                        Decode((ulong)values[index - 1], reader.Manifest.Columns, out var previousRow, out var previousColumn);
                        Decode((ulong)values[index], reader.Manifest.Columns, out var row, out var column);
                        Assert.That(Math.Abs(row - previousRow), Is.LessThanOrEqualTo(1));
                        Assert.That(Math.Abs(column - previousColumn), Is.LessThanOrEqualTo(1));
                    }
                }
            }
        }

        [Test]
        public void HistoricalCatalog_PreservesRequiredCountsAndUnresolvedCoordinates()
        {
            var cities = File.ReadAllText(Path.Combine(PackageRoot, "locations", "cities.json"));
            var counties = File.ReadAllText(Path.Combine(PackageRoot, "locations", "counties.json"));

            Assert.That(Regex.Matches(cities, "\\\"city_id\\\"").Count, Is.EqualTo(77));
            Assert.That(Regex.Matches(counties, "\\\"admin_unit_id\\\"").Count, Is.EqualTo(1182));
            Assert.That(Regex.Matches(cities, "\\\"coordinate_status\\\"\\s*:\\s*\\\"unresolved\\\"").Count, Is.EqualTo(5));
            Assert.That(Regex.Matches(cities, "\\\"geometry\\\"\\s*:\\s*null").Count, Is.EqualTo(5));
        }

        [Test]
        public void Package_PerformanceSmokeIsBoundedAndWritesEvidence()
        {
            var memoryBefore = GC.GetTotalMemory(true);
            var stopwatch = Stopwatch.StartNew();
            using (var reader = new WorldMapDataReader(PackageRoot))
            {
                var loadMilliseconds = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();
                long checksum = 0;
                for (var index = 0; index < 10000; index++)
                {
                    var row = index * 37 % reader.Grid.Rows;
                    var column = index * 71 % reader.Grid.Columns;
                    var cell = reader.ReadCell(row, column);
                    checksum ^= cell.Elevation ^ cell.CountyCode;
                }
                var queryMilliseconds = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                for (var index = 0; index < 1000; index++)
                {
                    checksum ^= reader.Neighbors.GetNeighbors(reader.Grid.ToCellId(index % reader.Grid.Rows, index % reader.Grid.Columns)).Count;
                }
                var neighborMilliseconds = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                for (var index = 0; index < 4; index++)
                {
                    checksum ^= reader.ReadChunk(10 + index, 20 + index).Length;
                }
                var chunkMilliseconds = stopwatch.ElapsedMilliseconds;
                var memoryDelta = Math.Max(0, GC.GetTotalMemory(false) - memoryBefore);

                var output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "tmp", "unity-validation", "world-map-performance.json");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output,
                    $"{{\"load_ms\":{loadMilliseconds},\"cell_queries\":10000,\"cell_query_ms\":{queryMilliseconds}," +
                    $"\"neighbor_queries\":1000,\"neighbor_query_ms\":{neighborMilliseconds},\"chunks\":4," +
                    $"\"chunk_query_ms\":{chunkMilliseconds},\"managed_memory_delta_bytes\":{memoryDelta},\"checksum\":{checksum}}}");

                TestContext.WriteLine(File.ReadAllText(output));
                Assert.That(loadMilliseconds, Is.LessThan(5000));
                Assert.That(queryMilliseconds, Is.LessThan(10000));
                Assert.That(neighborMilliseconds, Is.LessThan(2000));
                Assert.That(chunkMilliseconds, Is.LessThan(10000));
                Assert.That(memoryDelta, Is.LessThan(128L * 1024 * 1024));
            }
        }

        private static void Decode(ulong id, int columns, out int row, out int column)
        {
            row = (int)(id / (ulong)columns);
            column = (int)(id % (ulong)columns);
        }

        [Serializable]
        private sealed class RoadEdgesFile
        {
            public RoadEntry[] routes;
        }

        [Serializable]
        private sealed class RoadEntry
        {
            public string route_id;
            public long[] cell_ids;
        }
    }
}
