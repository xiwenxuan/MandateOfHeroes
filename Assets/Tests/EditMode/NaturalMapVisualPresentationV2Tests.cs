using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class NaturalMapVisualPresentationV2Tests
    {
        private static string WorldRoot => Path.Combine(Application.streamingAssetsPath, "WorldMap", "HanWorldV1");
        private static string NaturalRoot => Path.Combine(Application.streamingAssetsPath, "WorldMap", "NaturalBasemapV1");

        [Test]
        public void VisualV2Config_PreservesFrozenGridAndAddsThreeLodPresentation()
        {
            using (var source = new HanWorldNaturalMapSource(WorldRoot, NaturalRoot))
            {
                Assert.That(source.Config.Schema, Is.EqualTo("hanworld.natural-basemap-config.v2"));
                Assert.That(source.Config.Status, Is.EqualTo("HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2"));
                Assert.That(source.Config.TerrainTileCellsPerSide, Is.EqualTo(8));
                Assert.That(source.Config.WorldLodSampleStepCells, Is.EqualTo(8));
                Assert.That(source.Config.RegionFarSpanCells, Is.GreaterThanOrEqualTo(72));
                Assert.That(source.Config.RegionFarSampleStepCells, Is.GreaterThanOrEqualTo(1));
                Assert.That(source.Rows * source.Columns, Is.EqualTo(7211264));
                Assert.That(source.OriginX, Is.EqualTo(GlobalSpatialFoundationV1.OriginX));
                Assert.That(source.OriginY, Is.EqualTo(GlobalSpatialFoundationV1.OriginY));
            }
        }

        [Test]
        public void SurfaceBlend_UsesContinuousGlobalCoordinatesInsteadOfCellPaint()
        {
            var controller = new TerrainSurfaceBlendController();
            var surface = new NaturalSurfaceBlend(NaturalSurfaceIds.Grassland,
                NaturalSurfaceIds.SparseWoodland, NaturalLandformIds.Plain, 0.28d);
            var a = controller.Evaluate(new NaturalTerrainVertex(500000d, 3600000d, 120d, 150d, surface));
            var b = controller.Evaluate(new NaturalTerrainVertex(500001d, 3600001d, 120d, 150d, surface));
            Assert.That(ChannelDistance(a, b), Is.LessThanOrEqualTo(2),
                "One-metre movement must not jump to a new Cell colour block.");
            var hasBroadVariation = false;
            for (var index = 1; index <= 12; index++)
            {
                var sample = controller.Evaluate(new NaturalTerrainVertex(
                    500000d + index * 73000d, 3600000d + index * 41000d, 120d, 150d, surface));
                if (ChannelDistance(a, sample) > 0) { hasBroadVariation = true; break; }
            }
            Assert.That(hasBroadVariation, Is.True,
                "Broad natural variation remains deterministic and visible.");
        }

        [Test]
        public void RiverV2_SmoothsCenterlineAndBuildsBanksWithVariableWidth()
        {
            var source = new List<ProjectedPoint>
            {
                new ProjectedPoint(0d, 0d),
                new ProjectedPoint(2000d, 0d),
                new ProjectedPoint(2000d, 2000d)
            };
            var smooth = GlobalRiverVisualGenerator.SmoothCenterline(source, 2);
            Assert.That(smooth.Count, Is.GreaterThan(source.Count));
            Assert.That(smooth[0].X, Is.EqualTo(source[0].X));
            Assert.That(smooth[0].Y, Is.EqualTo(source[0].Y));
            Assert.That(smooth[smooth.Count - 1].X, Is.EqualTo(source[source.Count - 1].X));
            Assert.That(smooth[smooth.Count - 1].Y, Is.EqualTo(source[source.Count - 1].Y));

            var catalog = new GlobalRiverPresentationCatalog();
            catalog.Features.Add(new GlobalRiverPresentationFeature
            {
                RiverId = "river.test.v2",
                DisplayTier = "REGION",
                WidthMetres = 900d,
                Segments = new List<List<ProjectedPoint>> { source }
            });
            var mesh = new GlobalRiverVisualGenerator().BuildCombinedMesh(catalog,
                new GlobalProjectedCoordinate(0d, 0d), 2000d, null, (x, y) => 0.2f, 2);
            Assert.That(mesh.vertexCount, Is.EqualTo(smooth.Count * 4));
            Assert.That(mesh.colors32, Has.Some.Matches<Color32>(colour => colour.r > 100 && colour.g > 100));
            Assert.That(mesh.colors32, Has.Some.Matches<Color32>(colour => colour.b > colour.r));
            var firstWidth = Vector3.Distance(mesh.vertices[0], mesh.vertices[3]);
            var last = mesh.vertexCount - 4;
            var lastWidth = Vector3.Distance(mesh.vertices[last], mesh.vertices[last + 3]);
            Assert.That(Math.Abs(firstWidth - lastWidth), Is.GreaterThan(0.001f));
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ForestDensity_IsContinuousDeterministicAndBatched()
        {
            var source = new FormulaForestSource();
            var sampler = new GlobalForestDensitySampler(source);
            var left = sampler.Sample(20.999d, 30.999d);
            var right = sampler.Sample(21.001d, 31.001d);
            Assert.That(Math.Abs(left - right), Is.LessThan(0.02d));
            Assert.That(sampler.Sample(20.999d, 30.999d), Is.EqualTo(left));
            var generator = new GlobalVegetationGenerator();
            var a = generator.BuildCombinedMesh(source, 16, 24, 12, 12,
                new GlobalProjectedCoordinate(source.OriginX + 30 * 2000d,
                    source.OriginY - 22 * 2000d), 2000d, 250d, (x, y) => 0.2f, 2);
            var b = generator.BuildCombinedMesh(source, 16, 24, 12, 12,
                new GlobalProjectedCoordinate(source.OriginX + 30 * 2000d,
                    source.OriginY - 22 * 2000d), 2000d, 250d, (x, y) => 0.2f, 2);
            Assert.That(a.vertexCount, Is.GreaterThan(0));
            Assert.That(b.vertices, Is.EqualTo(a.vertices));
            Assert.That(a.subMeshCount, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }

        [Test]
        public void CameraRig_DefinesAllFrozenVisualAcceptancePresets()
        {
            foreach (var id in new[]
            {
                VisualAcceptanceCameraRig.WorldFull,
                VisualAcceptanceCameraRig.WorldNorthChina,
                VisualAcceptanceCameraRig.HenanRegion,
                VisualAcceptanceCameraRig.HenanMountain,
                VisualAcceptanceCameraRig.HenanRiver,
                VisualAcceptanceCameraRig.HenanForest,
                VisualAcceptanceCameraRig.TileSeam
            })
            {
                var preset = VisualAcceptanceCameraRig.Get(id);
                Assert.That(preset.Id, Is.EqualTo(id));
                Assert.That(preset.Size, Is.GreaterThan(0f));
                Assert.That(preset.Pitch, Is.InRange(45f, 80f));
            }
        }

        private static int ChannelDistance(Color32 a, Color32 b) =>
            Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);

        private sealed class FormulaForestSource : IGlobalNaturalCellSource
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
                var elevation = (short)(650 + (row + column) % 280);
                var cell = new WorldMapCellRecord(_grid.ToCellId(row, column), row, column,
                    x, y, elevation, 3, 1, 0, 0, 0, 0, 0, _grid.GridSchemaVersion);
                return new NaturalMapCellSample(cell, elevation - 15d);
            }
        }
    }
}
