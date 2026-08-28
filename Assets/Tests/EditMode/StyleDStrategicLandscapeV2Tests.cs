using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class StyleDStrategicLandscapeV2Tests
    {
        [Test]
        public void VisualTerrainDetail_RefinesPresentationWithoutCreatingCellsOrMutatingSource()
        {
            var source = BuildTile(0d);
            var original = source.Vertices.Select(value => value.SourceElevationMetres).ToArray();
            var profile = VisualTerrainDetailCatalog.Get(VisualTerrainDetailLevel.City);
            var refined = new VisualTerrainDetailGenerator().Refine(source, profile);

            Assert.That(profile.SubdivisionsPerCell, Is.EqualTo(4));
            Assert.That(profile.VisualSampleSpacingMetres, Is.EqualTo(500d));
            Assert.That(profile.CreatesSimulationSubCells, Is.False);
            Assert.That(refined.Tile.FirstRow, Is.EqualTo(source.Tile.FirstRow));
            Assert.That(refined.Tile.FirstColumn, Is.EqualTo(source.Tile.FirstColumn));
            Assert.That(refined.Vertices, Has.Length.EqualTo(25));
            Assert.That(refined.Triangles, Has.Length.EqualTo(96));
            Assert.That(source.Vertices.Select(value => value.SourceElevationMetres), Is.EqualTo(original));
            Assert.That(GlobalSpatialFoundationV1.Rows * GlobalSpatialFoundationV1.Columns,
                Is.EqualTo(7211264));
            Assert.That(GlobalSpatialFoundationV1.CellSizeMetres, Is.EqualTo(2000));
        }

        [Test]
        public void VisualTerrainDetail_IsDeterministicAndContinuousAcrossTileBoundary()
        {
            var profile = VisualTerrainDetailCatalog.Get(VisualTerrainDetailLevel.ClosePreview);
            var left = new VisualTerrainDetailGenerator().Refine(BuildTile(0d), profile);
            var right = new VisualTerrainDetailGenerator().Refine(BuildTile(2000d), profile);
            var repeat = new VisualTerrainDetailGenerator().Refine(BuildTile(0d), profile);
            Assert.That(repeat.Vertices.Select(value => value.PresentationElevationMetres),
                Is.EqualTo(left.Vertices.Select(value => value.PresentationElevationMetres)));
            var side = profile.SubdivisionsPerCell + 1;
            for (var row = 0; row < side; row++)
            {
                var a = left.Vertices[row * side + side - 1];
                var b = right.Vertices[row * side];
                Assert.That(a.GlobalX, Is.EqualTo(b.GlobalX).Within(0.0001d));
                Assert.That(a.GlobalY, Is.EqualTo(b.GlobalY).Within(0.0001d));
                Assert.That(a.PresentationElevationMetres,
                    Is.EqualTo(b.PresentationElevationMetres).Within(0.0001d));
            }
        }

        [Test]
        public void RiverMeshV2_SharpBendUsesBoundedJoinAndValidTerrainConformingMesh()
        {
            var catalog = new GlobalRiverPresentationCatalog();
            catalog.Features.Add(new GlobalRiverPresentationFeature
            {
                RiverId = "river.test.sharp-bend.v2",
                DisplayTier = "REGION",
                WidthMetres = 760d,
                Segments = new List<List<ProjectedPoint>>
                {
                    new List<ProjectedPoint>
                    {
                        new ProjectedPoint(0d, 0d), new ProjectedPoint(3000d, 0d),
                        new ProjectedPoint(3200d, 160d), new ProjectedPoint(3000d, 3200d),
                        new ProjectedPoint(6000d, 3600d)
                    }
                }
            });
            var generator = new GlobalRiverVisualGenerator();
            var mesh = generator.BuildCombinedMesh(catalog, new GlobalProjectedCoordinate(0d, 0d),
                2000d, null, (x, y) => (float)(0.2d + x / 100000d + y / 120000d), 1, 1f,
                RiverMeshBuildOptions.For(VisualTerrainDetailLevel.City));
            var diagnostic = generator.LastDiagnostics;
            Assert.That(mesh.vertexCount, Is.GreaterThan(20));
            Assert.That(diagnostic.AdaptiveSamples, Is.GreaterThan(5));
            Assert.That(diagnostic.ExtremeMiterCount, Is.Zero);
            Assert.That(diagnostic.InvalidTriangleCount, Is.Zero);
            Assert.That(diagnostic.DegenerateTriangleCount, Is.Zero);
            Assert.That(diagnostic.NaNVertexCount, Is.Zero);
            Assert.That(diagnostic.TriangleHoleCount, Is.Zero);
            Assert.That(diagnostic.WidthDiscontinuityErrorCount, Is.Zero);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ForestLod_UsesGlobalDensityAndCityProducesIndividualTreeDetail()
        {
            var source = new UniformForestSource();
            var generator = new GlobalVegetationGenerator();
            var origin = new GlobalProjectedCoordinate(source.OriginX + 4d * 2000d,
                source.OriginY - 4d * 2000d);
            var region = generator.BuildCombinedMesh(source, 0, 0, 8, 8, origin,
                2000d, 250d, (x, y) => 0.2f, 1, 1f,
                ForestPresentationLod.RegionCanopyCluster);
            var city = generator.BuildCombinedMesh(source, 0, 0, 8, 8, origin,
                2000d, 250d, (x, y) => 0.2f, 4, 1f,
                ForestPresentationLod.CityIndividualTrees);
            var cityRepeat = generator.BuildCombinedMesh(source, 0, 0, 8, 8, origin,
                2000d, 250d, (x, y) => 0.2f, 4, 1f,
                ForestPresentationLod.CityIndividualTrees);
            Assert.That(region.vertexCount, Is.GreaterThan(0));
            Assert.That(city.vertexCount, Is.GreaterThan(region.vertexCount));
            Assert.That(city.vertices, Is.EqualTo(cityRepeat.vertices));
            Object.DestroyImmediate(region);
            Object.DestroyImmediate(city);
            Object.DestroyImmediate(cityRepeat);
        }

        [Test]
        public void StyleDV2_ProfileAndRequiredCamerasAreFrozen()
        {
            Assert.That(HanWorldArtProfileCatalog.StyleDId,
                Is.EqualTo("art.han-world.zhonghua-sanguozhi-fusion.v2"));
            foreach (var id in new[]
            {
                ZhonghuaFusionCameraRig.World, ZhonghuaFusionCameraRig.Region,
                ZhonghuaFusionCameraRig.CityDistance, ZhonghuaFusionCameraRig.Mountain,
                ZhonghuaFusionCameraRig.RiverStraight, ZhonghuaFusionCameraRig.RiverGentle,
                ZhonghuaFusionCameraRig.RiverSharpBend,
                ZhonghuaFusionCameraRig.ForestWorld, ZhonghuaFusionCameraRig.ForestRegion,
                ZhonghuaFusionCameraRig.ForestCity, ZhonghuaFusionCameraRig.Plain,
                ZhonghuaFusionCameraRig.TerrainDetail, ZhonghuaFusionCameraRig.WorldToCityMid,
                ZhonghuaFusionCameraRig.GridOff
            }) Assert.That(ZhonghuaFusionCameraRig.Get(id).Id, Is.EqualTo(id));
            Assert.That(ZhonghuaFusionCameraRig.DetailLevelFor(ZhonghuaFusionCameraRig.CityDistance),
                Is.EqualTo(VisualTerrainDetailLevel.City));
            Assert.That(ZhonghuaFusionCameraRig.DetailLevelFor(ZhonghuaFusionCameraRig.TerrainDetail),
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
            Assert.That(ZhonghuaFusionCameraRig.Get(ZhonghuaFusionCameraRig.RiverStraight).Size,
                Is.LessThan(ZhonghuaFusionCameraRig.Get(ZhonghuaFusionCameraRig.RiverGentle).Size));
        }

        private static NaturalTerrainMeshData BuildTile(double minX)
        {
            var surface = new NaturalSurfaceBlend(NaturalSurfaceIds.Grassland,
                NaturalSurfaceIds.SparseWoodland, NaturalLandformIds.Plain, 0.18d);
            var baseElevation = 100d + minX / 2000d * 20d;
            return new NaturalTerrainMeshData
            {
                Tile = new TerrainTileDefinition(new TerrainTileId(0, (int)(minX / 2000d)),
                    0, 0, (int)(minX / 2000d), (int)(minX / 2000d), minX, 0d,
                    minX + 2000d, 2000d),
                Vertices = new[]
                {
                    new NaturalTerrainVertex(minX, 2000d, baseElevation, baseElevation * 1.2d, surface),
                    new NaturalTerrainVertex(minX + 2000d, 2000d, baseElevation + 20d,
                        (baseElevation + 20d) * 1.2d, surface),
                    new NaturalTerrainVertex(minX, 0d, baseElevation + 30d,
                        (baseElevation + 30d) * 1.2d, surface),
                    new NaturalTerrainVertex(minX + 2000d, 0d, baseElevation + 50d,
                        (baseElevation + 50d) * 1.2d, surface)
                },
                Triangles = new[] { 0, 2, 1, 1, 2, 3 },
                SourceCellReadCount = 4
            };
        }

        private sealed class UniformForestSource : IGlobalNaturalCellSource
        {
            private readonly CellGridIndex _grid = GlobalSpatialFoundationV1.CreateCellGrid();
            public int Rows => _grid.Rows;
            public int Columns => _grid.Columns;
            public double OriginX => _grid.OriginX;
            public double OriginY => _grid.OriginY;
            public int CellSizeMetres => 2000;
            public NaturalMapCellSample ReadSample(int row, int column)
            {
                _grid.GetCenter(row, column, out var x, out var y);
                var cell = new WorldMapCellRecord(_grid.ToCellId(row, column), row, column,
                    x, y, 520, 3, 2, 0, 0, 0, 0, 0, _grid.GridSchemaVersion);
                return new NaturalMapCellSample(cell, 500d);
            }
        }
    }
}
