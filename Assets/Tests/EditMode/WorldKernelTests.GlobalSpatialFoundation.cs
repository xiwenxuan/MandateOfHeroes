using System;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void GlobalSpatial_CellAndChunkIdentitiesAreStableAtBoundaries()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var chunks = GlobalSpatialFoundationV1.CreateChunkGrid();
            Assert.That(grid.CellCount, Is.EqualTo(7211264UL));
            Assert.That(grid.ToCellId(2175, 3313).Value, Is.EqualTo(7211263UL));
            Assert.That(chunks.ChunkRows, Is.EqualTo(136));
            Assert.That(chunks.ChunkColumns, Is.EqualTo(208));
            Assert.That(chunks.ChunkCount, Is.EqualTo(28288));
            Assert.That(chunks.FromCell(15, 15), Is.EqualTo(new GlobalChunkId(0, 0)));
            Assert.That(chunks.FromCell(16, 16), Is.EqualTo(new GlobalChunkId(1, 1)));
        }

        [Test]
        public void GlobalSpatial_Block16KeepsLegacyIdentityButIsOnlyTechnicalAggregation()
        {
            var blocks = GlobalSpatialFoundationV1.CreateChunkGrid();
            Assert.That(blocks.ChunkCount, Is.EqualTo(28288));
            Assert.That(blocks.SemanticStatus,
                Is.EqualTo("SUPERSEDED_SEMANTICALLY_RECLASSIFIED"));
            Assert.That(blocks.CurrentPurpose,
                Is.EqualTo("TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK"));
            Assert.That(blocks.IsWorldFact, Is.False);
            Assert.That(blocks.IsSimulationAggregation, Is.True);
            Assert.That(blocks.IsTerrainTile, Is.False);
            Assert.That(blocks.IsStreamingUnit, Is.False);
            Assert.That(blocks.FromCell(16, 16).PermanentId,
                Is.EqualTo("chunk.hanworld.global.v1.r001.c001"));
            Assert.That(GlobalSpatialFoundationV1.TerrainTileSizeStatus,
                Does.Contain("8X8"));
            Assert.That(GlobalSpatialFoundationV1.StreamingUnitSizeStatus,
                Does.StartWith("PROVISIONAL_24X24"));
        }

        [Test]
        public void RegionBoundary_IsDerivedFromCompleteGlobalCellMembership()
        {
            var grid = new CellGridIndex(3, 4, 0d, 6000d, 2000d);
            var regionA = new GlobalRegionSpatialDefinition { RegionId = "A" };
            regionA.IncludedGlobalCellIds.AddRange(new ulong[] { 0, 1, 4 });
            var regionB = new GlobalRegionSpatialDefinition { RegionId = "B" };
            regionB.IncludedGlobalCellIds.AddRange(new ulong[] { 2, 3, 6, 7 });
            var index = new RegionCellBoundaryIndex(grid,
                new[] { regionA, regionB });

            Assert.That(index.GetNeighborCell(new WorldMapCellId(1),
                GlobalCellEdgeDirection.East).Value.Value, Is.EqualTo(2UL));
            Assert.That(index.GetAdjacentRegions("A"), Is.EqualTo(new[] { "B" }));
            Assert.That(index.GetNeighborCellsAcrossRegionBoundary("A"),
                Does.Contain(new WorldMapCellId(2)));
            Assert.That(index.GetRegionBoundaryEdges("A"), Does.Contain(
                new RegionBoundaryEdge("A", new WorldMapCellId(1),
                    GlobalCellEdgeDirection.East, new WorldMapCellId(2))));
        }

        [Test]
        public void HenanRegionBoundary_UsesAll58368CellsWithoutBlockAlignmentAuthority()
        {
            var region = new GlobalRegionSpatialDefinition
            {
                RegionId = GlobalSpatialFoundationV1.HenanYinRegionId
            };
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            for (var row = GlobalSpatialFoundationV1.HenanYinMinRow;
                row <= GlobalSpatialFoundationV1.HenanYinMaxRow; row++)
            for (var column = GlobalSpatialFoundationV1.HenanYinMinColumn;
                column <= GlobalSpatialFoundationV1.HenanYinMaxColumn; column++)
                region.IncludedGlobalCellIds.Add(grid.ToCellId(row, column).Value);

            var index = new RegionCellBoundaryIndex(
                grid, new[] { region });
            Assert.That(region.IncludedGlobalCellIds,
                Has.Count.EqualTo(GlobalSpatialFoundationV1.HenanYinIncludedCellCount));
            Assert.That(index.GetRegionBoundaryEdges(region.RegionId), Has.Count.EqualTo(992));
            Assert.That(index.GetNeighborCellsAcrossRegionBoundary(region.RegionId),
                Has.Count.EqualTo(992));
            Assert.That(index.GetAdjacentRegions(region.RegionId), Is.Empty);
        }

        [Test]
        public void GlobalSpatial_GisRegionChunkUnityRoundTripIsBounded()
        {
            var service = new GlobalSpatialTransformService();
            var gis = new GeographicCoordinate(112.45d, 34.62d);
            var global = service.GeographicToGlobal(gis);
            Assert.That(service.TryGlobalToCell(global, out var id), Is.True);
            Assert.That(id.Value, Is.EqualTo(4114717UL));
            var regionOrigin = service.CellToGlobalCenter(new WorldMapCellId(3820000UL));
            var region = service.GlobalToRegionLocal(global, regionOrigin);
            var afterRegion = service.RegionLocalToGlobal(region, regionOrigin);
            var chunk = service.CellToGlobalChunk(id);
            var chunkLocal = service.GlobalToChunkLocal(afterRegion, chunk);
            var afterChunk = service.ChunkLocalToGlobal(chunkLocal, chunk);
            var unity = service.GlobalToUnityLocal(afterChunk, regionOrigin, 150d);
            var afterUnity = service.UnityLocalToGlobal(unity, regionOrigin);
            var roundTrip = service.GlobalToGeographic(afterUnity);
            Assert.That(Math.Abs(roundTrip.LongitudeDegrees - gis.LongitudeDegrees), Is.LessThan(1e-8d));
            Assert.That(Math.Abs(roundTrip.LatitudeDegrees - gis.LatitudeDegrees), Is.LessThan(1e-8d));
            Assert.That(Math.Abs(afterUnity.EastingMetres - global.EastingMetres), Is.LessThan(1e-6d));
            Assert.That(Math.Abs(afterUnity.NorthingMetres - global.NorthingMetres), Is.LessThan(1e-6d));
        }

        [Test]
        public void GlobalSpatial_FloatingOriginDoesNotChangeWorldFacts()
        {
            var service = new GlobalSpatialTransformService();
            var global = service.GeographicToGlobal(new GeographicCoordinate(113.15d, 34.82d));
            Assert.That(service.TryGlobalToCell(global, out var beforeCell), Is.True);
            var originA = new GlobalProjectedCoordinate(0d, 0d);
            var originB = new GlobalProjectedCoordinate(global.EastingMetres - 500d, global.NorthingMetres + 750d);
            var localA = service.GlobalToUnityLocal(global, originA);
            var localB = service.GlobalToUnityLocal(global, originB);
            var restoredA = service.UnityLocalToGlobal(localA, originA);
            var restoredB = service.UnityLocalToGlobal(localB, originB);
            Assert.That(restoredA, Is.EqualTo(global));
            Assert.That(restoredB, Is.EqualTo(global));
            Assert.That(service.TryGlobalToCell(restoredB, out var afterCell), Is.True);
            Assert.That(afterCell, Is.EqualTo(beforeCell));
            Assert.That(localA, Is.Not.EqualTo(localB));
        }

        [Test]
        public void GlobalSpatial_OriginEnvelopeRegionAndLuoyangNumbersAreExplicitAndReversible()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            grid.GetCenter(0, 0, out var firstCenterX, out var firstCenterY);
            Assert.That(GlobalSpatialFoundationV1.GlobalOriginMeaning,
                Is.EqualTo("GLOBAL_GRID_NORTHWEST_CORNER"));
            Assert.That(firstCenterX,
                Is.EqualTo(GlobalSpatialFoundationV1.OriginX + 0.5d * grid.CellSize));
            Assert.That(firstCenterY,
                Is.EqualTo(GlobalSpatialFoundationV1.OriginY - 0.5d * grid.CellSize));
            Assert.That(GlobalSpatialFoundationV1.GlobalMaxX - GlobalSpatialFoundationV1.GlobalMinX,
                Is.EqualTo(GlobalSpatialFoundationV1.Columns * grid.CellSize));
            Assert.That(GlobalSpatialFoundationV1.GlobalMaxY - GlobalSpatialFoundationV1.GlobalMinY,
                Is.EqualTo(GlobalSpatialFoundationV1.Rows * grid.CellSize));

            var regionOriginCell = grid.ToCellId(
                GlobalSpatialFoundationV1.HenanYinMaxRow,
                GlobalSpatialFoundationV1.HenanYinMinColumn);
            Assert.That(regionOriginCell.Value,
                Is.EqualTo(GlobalSpatialFoundationV1.HenanYinOriginCellId));
            Assert.That(GlobalSpatialFoundationV1.HenanYinOriginX,
                Is.EqualTo(grid.OriginX + GlobalSpatialFoundationV1.HenanYinMinColumn * grid.CellSize));
            Assert.That(GlobalSpatialFoundationV1.HenanYinOriginY,
                Is.EqualTo(grid.OriginY - (GlobalSpatialFoundationV1.HenanYinMaxRow + 1) * grid.CellSize));

            var service = new GlobalSpatialTransformService();
            var luoyang = service.GeographicToGlobal(new GeographicCoordinate(112.45d, 34.62d));
            var local = service.GlobalToRegionLocal(luoyang, new GlobalProjectedCoordinate(
                GlobalSpatialFoundationV1.HenanYinOriginX,
                GlobalSpatialFoundationV1.HenanYinOriginY));
            var restored = service.RegionLocalToGlobal(local, new GlobalProjectedCoordinate(
                GlobalSpatialFoundationV1.HenanYinOriginX,
                GlobalSpatialFoundationV1.HenanYinOriginY));
            Assert.That(restored, Is.EqualTo(luoyang));
            Assert.That(service.TryGlobalToCell(restored, out var luoyangCell), Is.True);
            Assert.That(luoyangCell.Value, Is.EqualTo(GlobalSpatialFoundationV1.LuoyangCanonicalCellId));
        }
    }
}
