using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class GlobalSpatialFoundationV1Tests
    {
        private static string PackageRoot => Path.Combine(Application.streamingAssetsPath,
            "WorldMap", "GlobalSpatialFoundationV1");
        private static string WorldRoot => Path.Combine(Application.streamingAssetsPath,
            "WorldMap", "HanWorldV1");

        [Test]
        public void FrozenPackage_LoadsWithoutCreatingRegionCells()
        {
            var source = new GlobalSpatialFoundationSource(PackageRoot);
            Assert.That(source.Contract.Status, Is.EqualTo(GlobalSpatialFoundationV1.FrozenStatus));
            Assert.That(source.Contract.ReuseConclusion, Is.EqualTo("B_REUSABLE_WITH_NON_ID_MIGRATION"));
            Assert.That(source.Contract.Chunk.CellsPerSide, Is.EqualTo(16));
            Assert.That(source.Contract.Chunk.SemanticStatus,
                Is.EqualTo(GlobalSpatialFoundationV1.Block16SemanticStatus));
            Assert.That(source.Contract.Chunk.CurrentPurpose,
                Is.EqualTo(GlobalSpatialFoundationV1.Block16CurrentPurpose));
            Assert.That(source.Contract.Chunk.IsWorldFact, Is.False);
            Assert.That(source.Contract.Chunk.IsSimulationAggregation, Is.True);
            Assert.That(source.Contract.Chunk.IsTerrainTile, Is.False);
            Assert.That(source.Contract.Chunk.IsStreamingUnit, Is.False);
            Assert.That(source.Contract.Grid.OriginMeaning,
                Is.EqualTo(GlobalSpatialFoundationV1.GlobalOriginMeaning));
            Assert.That(source.Contract.Grid.FirstCell.CellPermanentId,
                Is.EqualTo("cell.hanworld.v0.0"));
            Assert.That(source.Contract.Grid.FirstCell.CenterX,
                Is.EqualTo(GlobalSpatialFoundationV1.OriginX + 1000d));
            Assert.That(source.Contract.Grid.FirstCell.CenterY,
                Is.EqualTo(GlobalSpatialFoundationV1.OriginY - 1000d));
            Assert.That(source.Contract.Grid.ValidWorldExtent.MinX,
                Is.EqualTo(GlobalSpatialFoundationV1.GlobalMinX));
            Assert.That(source.Region.GeneratedNewCellCount, Is.Zero);
            Assert.That(source.Region.RegionLocalOrigin.CellId,
                Is.EqualTo(GlobalSpatialFoundationV1.HenanYinOriginCellId));
            Assert.That(source.Region.RegionLocalOrigin.Corner, Is.EqualTo("SOUTHWEST_CORNER"));
            Assert.That(source.Region.IncludedCellIds, Has.Count.EqualTo(58368));
            Assert.That(source.Region.IncludedGlobalChunkIds, Has.Count.EqualTo(228));
            Assert.That(source.Region.Authority, Is.EqualTo("INCLUDED_GLOBAL_CELL_IDS"));
            Assert.That(source.Region.BoundaryAuthority, Is.EqualTo("CELL_MEMBERSHIP"));
            Assert.That(source.Region.BoundaryModel, Is.EqualTo("CELL_EDGE_DERIVED"));
            Assert.That(source.Region.PolygonAuthority, Is.False);
            Assert.That(source.Region.CutsGlobalCells, Is.False);
            Assert.That(source.Region.IncludedGlobalChunkIdsSemantics,
                Is.EqualTo("DERIVED_TECHNICAL_INDEX"));
        }

        [Test]
        public void AggregationBlocks_AreContinuousAcrossLegacyStorageBlockBoundary()
        {
            using (var reader = new WorldMapDataReader(WorldRoot))
            {
                Assert.That(reader.Manifest.ChunkSize, Is.EqualTo(64),
                    "64 remains physical compression storage only");
                var west = reader.ReadCanonicalGlobalChunk(72, 127);
                var east = reader.ReadCanonicalGlobalChunk(72, 128);
                Assert.That(west.RowCount, Is.EqualTo(16));
                Assert.That(west.ColumnCount, Is.EqualTo(16));
                for (var row = 0; row < 16; row++)
                {
                    var a = west.GetLocal(row, 15);
                    var b = east.GetLocal(row, 0);
                    Assert.That(b.Column - a.Column, Is.EqualTo(1));
                    Assert.That(b.CellId64 - a.CellId64, Is.EqualTo(1UL));
                    Assert.That(b.CenterX - a.CenterX, Is.EqualTo(2000d));
                    Assert.That(b.CenterY, Is.EqualTo(a.CenterY));
                }
            }
        }

        [Test]
        public void TransformService_MapsCanonicalLuoyangAnchor()
        {
            var service = new GlobalSpatialTransformService();
            var global = service.GeographicToGlobal(new GeographicCoordinate(112.45d, 34.62d));
            Assert.That(service.TryGlobalToCell(global, out var id), Is.True);
            Assert.That(id.Value, Is.EqualTo(4114717UL));
            Assert.That(service.CellToSimulationAggregationBlock(id).PermanentId,
                Is.EqualTo("chunk.hanworld.global.v1.r077.c127"));
        }
    }
}
