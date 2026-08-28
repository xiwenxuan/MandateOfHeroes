using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class HanWorldNaturalBasemapV1Tests
    {
        private static string WorldRoot => Path.Combine(Application.streamingAssetsPath, "WorldMap", "HanWorldV1");
        private static string NaturalRoot => Path.Combine(Application.streamingAssetsPath, "WorldMap", "NaturalBasemapV1");
        private static string EvidenceRoot => Path.Combine(Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE", "HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1");

        [Test]
        public void NaturalPackage_UsesFrozenGlobalGridAndLicensedSources()
        {
            using (var source = new HanWorldNaturalMapSource(WorldRoot, NaturalRoot))
            {
                Assert.That(source.Rows, Is.EqualTo(2176));
                Assert.That(source.Columns, Is.EqualTo(3314));
                Assert.That(source.OriginX, Is.EqualTo(GlobalSpatialFoundationV1.OriginX));
                Assert.That(source.OriginY, Is.EqualTo(GlobalSpatialFoundationV1.OriginY));
                Assert.That(source.Config.TerrainTileCellsPerSide, Is.EqualTo(8));
                Assert.That(source.Config.BackgroundPolicy, Is.EqualTo("NO_LEGACY_BACKGROUND_REQUIRED"));
                Assert.That(source.Rivers.Features, Has.Count.GreaterThan(200));
                Assert.That(source.Rivers.SourceGaps, Has.Some.Matches<GlobalRiverSourceGap>(item =>
                    item.RiverId.EndsWith("luo", StringComparison.Ordinal) &&
                    item.Status == "NOT_PROVEN_SOURCE_GAP"));
            }
        }

        [Test]
        public void RealDem_SharedTileEdgesHaveZeroHeightError()
        {
            using (var source = new HanWorldNaturalMapSource(WorldRoot, NaturalRoot))
            {
                var index = new TerrainTileIndex(GlobalSpatialFoundationV1.CreateCellGrid());
                var generator = new HanWorldTerrainGenerator(source);
                var samples = new[] { new TerrainTileId(138, 261), new TerrainTileId(173, 213),
                    new TerrainTileId(145, 246), new TerrainTileId(155, 255) };
                foreach (var sample in samples)
                {
                    var west = generator.GenerateTile(index.Get(sample));
                    var east = generator.GenerateTile(index.Get(new TerrainTileId(sample.Row, sample.Column + 1)));
                    for (var row = 0; row <= 8; row++)
                        Assert.That(east.Vertices[row * 9].PresentationElevationMetres,
                            Is.EqualTo(west.Vertices[row * 9 + 8].PresentationElevationMetres));
                }
            }
        }

        [Test]
        public void RealDem_BenchmarkCandidatesAndResidentWindowsAreMeasuredInUnity()
        {
            Directory.CreateDirectory(EvidenceRoot);
            var rows = new List<string>();
            var samples = new[]
            {
                new Sample("NORTH_CHINA_PLAIN", 1110, 2090),
                new Sample("MOUNTAIN_HILL", 1390, 1710),
                new Sample("MAJOR_RIVER", 1160, 1970),
                new Sample("HENAN_LUOYANG", 1241, 2043)
            };
            using (var source = new HanWorldNaturalMapSource(WorldRoot, NaturalRoot))
            {
                var generator = new HanWorldTerrainGenerator(source);
                foreach (var size in new[] { 4, 8, 16 })
                foreach (var residentSide in new[] { 3, 5 })
                foreach (var sample in samples)
                {
                    var grid = GlobalSpatialFoundationV1.CreateCellGrid();
                    var index = new TerrainTileIndex(grid, size);
                    var center = index.FromCell(sample.Row, sample.Column);
                    var stopwatch = Stopwatch.StartNew();
                    long vertexCount = 0;
                    long triangleCount = 0;
                    long readCount = 0;
                    long before = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    var radius = residentSide / 2;
                    for (var row = Math.Max(0, center.Row-radius);
                         row <= Math.Min(index.TileRows-1, center.Row+radius); row++)
                    for (var column = Math.Max(0, center.Column-radius);
                         column <= Math.Min(index.TileColumns-1, center.Column+radius); column++)
                    {
                        var mesh = generator.GenerateTile(index.Get(new TerrainTileId(row, column)));
                        vertexCount += mesh.Vertices.Length;
                        triangleCount += mesh.Triangles.Length / 3;
                        readCount += mesh.SourceCellReadCount;
                    }
                    stopwatch.Stop();
                    var after = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    rows.Add("{\"candidate_cells_per_side\":" + size +
                        ",\"resident_window\":\"" + residentSide + "x" + residentSide + "\"" +
                        ",\"sample\":\"" + sample.Name + "\"" +
                        ",\"generation_ms_unity\":" + stopwatch.Elapsed.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ",\"managed_memory_delta_bytes\":" + Math.Max(0L, after-before) +
                        ",\"vertices\":" + vertexCount + ",\"triangles\":" + triangleCount +
                        ",\"source_cell_reads\":" + readCount +
                        ",\"collider_triangles\":" + triangleCount +
                        ",\"draw_call_upper_bound\":" + residentSide*residentSide + "}");
                }
            }
            var json = "{\n  \"schema\": \"hanworld.terrain-tile-unity-benchmark.v1\",\n" +
                       "  \"engine\": \"Unity 2022.3.62f3c1\",\n" +
                       "  \"gpu_timing_status\": \"PROXY_ONLY_BATCHMODE_NO_GPU_TIMESTAMP\",\n" +
                       "  \"load_unload_gc_contract\": \"REGION_REBUILDS_BOUNDED_3X3_OR_5X5_TILE_WINDOWS\",\n" +
                       "  \"results\": [\n    " + string.Join(",\n    ", rows) + "\n  ]\n}\n";
            File.WriteAllText(Path.Combine(EvidenceRoot, "unity_terrain_benchmark.json"), json, Encoding.UTF8);
            Assert.That(rows, Has.Count.EqualTo(24));
        }

        [Test]
        public void TerrainCellBinding_MapsRealLuoyangAndDistantFloatingOrigins()
        {
            var binding = new TerrainCellBinding(GlobalSpatialFoundationV1.CreateCellGrid());
            var luoyang = new GlobalProjectedCoordinate(670561.5475446532d, 3717065.2005044892d);
            Assert.That(binding.TryGlobalToCell(luoyang, out var canonical), Is.True);
            Assert.That(canonical.Value, Is.EqualTo(4114717UL));
            foreach (var origin in new[]
            {
                new GlobalProjectedCoordinate(GlobalSpatialFoundationV1.OriginX, GlobalSpatialFoundationV1.OriginY),
                new GlobalProjectedCoordinate(670000d, 3717000d),
                new GlobalProjectedCoordinate(2500000d, 2500000d)
            })
            {
                var local = binding.GlobalToUnity(luoyang, 150d, origin);
                var restored = binding.UnityToGlobal(local, origin);
                Assert.That(binding.TryGlobalToCell(restored, out var after), Is.True);
                Assert.That(after, Is.EqualTo(canonical));
            }
        }

        private readonly struct Sample
        {
            public Sample(string name, int row, int column) { Name = name; Row = row; Column = column; }
            public string Name { get; }
            public int Row { get; }
            public int Column { get; }
        }
    }
}
