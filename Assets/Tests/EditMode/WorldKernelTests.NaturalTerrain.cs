using System;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void NaturalTerrain_TileIndexIsTechnicalAndSelectedByBenchmark()
        {
            var index = new TerrainTileIndex(GlobalSpatialFoundationV1.CreateCellGrid());
            Assert.That(index.CellsPerSide, Is.EqualTo(8));
            Assert.That(index.TileRows, Is.EqualTo(272));
            Assert.That(index.TileColumns, Is.EqualTo(415));
            Assert.That(index.TileCount, Is.EqualTo(112880));
            Assert.That(index.SemanticRole, Is.EqualTo("DERIVED_TECHNICAL_PRESENTATION_INDEX"));
            Assert.That(index.IsRegion, Is.False);
            Assert.That(index.IsSimulationAggregationBlock, Is.False);
            Assert.That(index.IsStorageBlock, Is.False);
            Assert.That(index.Get(new TerrainTileId(0, 0)).MinX,
                Is.EqualTo(GlobalSpatialFoundationV1.OriginX));
            Assert.That(index.Get(new TerrainTileId(271, 414)).LastColumn,
                Is.EqualTo(3313));
        }

        [Test]
        public void NaturalTerrain_SharedTileEdgesUseSameGlobalGridVertices()
        {
            var source = new FormulaNaturalCellSource();
            var index = new TerrainTileIndex(GlobalSpatialFoundationV1.CreateCellGrid());
            var generator = new HanWorldTerrainGenerator(source);
            var west = generator.GenerateTile(index.Get(new TerrainTileId(10, 10)));
            var east = generator.GenerateTile(index.Get(new TerrainTileId(10, 11)));
            for (var row = 0; row <= 8; row++)
            {
                var westVertex = west.Vertices[row * 9 + 8];
                var eastVertex = east.Vertices[row * 9];
                Assert.That(eastVertex.GlobalX, Is.EqualTo(westVertex.GlobalX));
                Assert.That(eastVertex.GlobalY, Is.EqualTo(westVertex.GlobalY));
                Assert.That(eastVertex.PresentationElevationMetres,
                    Is.EqualTo(westVertex.PresentationElevationMetres));
            }
        }

        [Test]
        public void NaturalTerrain_FloatingOriginAndCellPickingPreservePermanentIdentity()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var binding = new TerrainCellBinding(grid);
            var global = new GlobalProjectedCoordinate(670561.5475446532d, 3717065.2005044892d);
            Assert.That(binding.TryGlobalToCell(global, out var before), Is.True);
            var origins = new[]
            {
                new GlobalProjectedCoordinate(0d, 0d),
                new GlobalProjectedCoordinate(670000d, 3717000d),
                new GlobalProjectedCoordinate(-1800000d, 5000000d)
            };
            foreach (var origin in origins)
            {
                var local = binding.GlobalToUnity(global, 160d, origin);
                var restored = binding.UnityToGlobal(local, origin);
                Assert.That(Math.Abs(restored.EastingMetres - global.EastingMetres), Is.LessThan(1e-6));
                Assert.That(Math.Abs(restored.NorthingMetres - global.NorthingMetres), Is.LessThan(1e-6));
                Assert.That(binding.TryGlobalToCell(restored, out var after), Is.True);
                Assert.That(after, Is.EqualTo(before));
            }
            Assert.That(before.Value, Is.EqualTo(GlobalSpatialFoundationV1.LuoyangCanonicalCellId));
        }

        [Test]
        public void NaturalSurface_UsesStableDataDrivenIdsForRequiredClasses()
        {
            Assert.That(NaturalSurfaceIds.All, Does.Contain(NaturalSurfaceIds.Forest));
            Assert.That(NaturalSurfaceIds.All, Does.Contain(NaturalSurfaceIds.Wetland));
            Assert.That(NaturalSurfaceIds.All, Does.Contain(NaturalSurfaceIds.Riverbank));
            Assert.That(NaturalSurfaceIds.All, Does.Contain(NaturalSurfaceIds.Rock));
            Assert.That(Enum.IsDefined(typeof(GlobalCellEdgeDirection), 0), Is.True,
                "Only closed protocol state remains an enum; extensible surfaces use stable IDs.");
        }

        private sealed class FormulaNaturalCellSource : IGlobalNaturalCellSource
        {
            private readonly CellGridIndex _grid = GlobalSpatialFoundationV1.CreateCellGrid();
            public int Rows => _grid.Rows;
            public int Columns => _grid.Columns;
            public double OriginX => _grid.OriginX;
            public double OriginY => _grid.OriginY;
            public int CellSizeMetres => (int)_grid.CellSize;
            public NaturalMapCellSample ReadSample(int row, int column)
            {
                _grid.GetCenter(row, column, out var x, out var y);
                var elevation = (short)((row * 3 + column * 5) % 2600);
                return new NaturalMapCellSample(new WorldMapCellRecord(_grid.ToCellId(row, column),
                    row, column, x, y, elevation, 2, 1, 0, 0, 0, 0, 0,
                    _grid.GridSchemaVersion), elevation);
            }
        }
    }
}
