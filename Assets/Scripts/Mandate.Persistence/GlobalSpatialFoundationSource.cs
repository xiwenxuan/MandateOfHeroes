using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class GlobalSpatialFoundationSource
    {
        public GlobalSpatialFoundationSource(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot)) throw new ArgumentException(nameof(packageRoot));
            PackageRoot = Path.GetFullPath(packageRoot);
            Contract = JsonConvert.DeserializeObject<GlobalSpatialFoundationRecord>(File.ReadAllText(
                Path.Combine(PackageRoot, "global_spatial_foundation.json"))) ??
                throw new InvalidDataException("Global spatial foundation is empty.");
            Region = JsonConvert.DeserializeObject<GlobalRegionSpatialRecord>(File.ReadAllText(
                Path.Combine(PackageRoot, "henan_yin_region_cell_slice.json"))) ??
                throw new InvalidDataException("Henan Yin region slice is empty.");
            Validate();
        }
        public string PackageRoot { get; }
        public GlobalSpatialFoundationRecord Contract { get; }
        public GlobalRegionSpatialRecord Region { get; }
        public CellGridIndex CreateCellGrid() => new CellGridIndex(Contract.Grid.Rows,
            Contract.Grid.Columns, Contract.Grid.OriginX, Contract.Grid.OriginY,
            Contract.Grid.CellSizeMetres, Contract.Grid.SchemaVersion);
        private void Validate()
        {
            if (Contract.Status != GlobalSpatialFoundationV1.FrozenStatus ||
                Contract.Crs.Id != GlobalSpatialFoundationV1.CrsId ||
                Contract.Grid.Rows != GlobalSpatialFoundationV1.Rows ||
                Contract.Grid.Columns != GlobalSpatialFoundationV1.Columns ||
                Contract.Grid.TotalCells != (long)GlobalSpatialFoundationV1.Rows * GlobalSpatialFoundationV1.Columns ||
                Contract.Grid.CellSizeMetres != GlobalSpatialFoundationV1.CellSizeMetres ||
                Contract.Grid.OriginX != GlobalSpatialFoundationV1.OriginX ||
                Contract.Grid.OriginY != GlobalSpatialFoundationV1.OriginY ||
                Contract.Grid.OriginMeaning != GlobalSpatialFoundationV1.GlobalOriginMeaning ||
                Contract.Grid.RowDirection != GlobalSpatialFoundationV1.GlobalRowDirection ||
                Contract.Grid.ColumnDirection != GlobalSpatialFoundationV1.GlobalColumnDirection ||
                Contract.Grid.FirstCell == null || Contract.Grid.WorldBounds == null ||
                Contract.Grid.FirstCell.CellId != 0UL || Contract.Grid.FirstCell.Row != 0 ||
                Contract.Grid.FirstCell.Column != 0 ||
                Contract.Grid.FirstCell.MinX != GlobalSpatialFoundationV1.OriginX ||
                Contract.Grid.FirstCell.MaxY != GlobalSpatialFoundationV1.OriginY ||
                Contract.Grid.WorldBounds.MinX != GlobalSpatialFoundationV1.GlobalMinX ||
                Contract.Grid.WorldBounds.MaxX != GlobalSpatialFoundationV1.GlobalMaxX ||
                Contract.Grid.WorldBounds.MinY != GlobalSpatialFoundationV1.GlobalMinY ||
                Contract.Grid.WorldBounds.MaxY != GlobalSpatialFoundationV1.GlobalMaxY ||
                Contract.Chunk.CellsPerSide != GlobalSpatialFoundationV1.CanonicalChunkSizeCells ||
                Contract.Chunk.SemanticStatus != GlobalSpatialFoundationV1.Block16SemanticStatus ||
                Contract.Chunk.CurrentPurpose != GlobalSpatialFoundationV1.Block16CurrentPurpose ||
                Contract.Chunk.IsWorldFact || !Contract.Chunk.IsSimulationAggregation ||
                Contract.Chunk.IsTerrainTile || Contract.Chunk.IsStreamingUnit ||
                Contract.Chunk.IsStorageBlock ||
                Contract.Chunk.TerrainTileSize != GlobalSpatialFoundationV1.TerrainTileSizeStatus ||
                Contract.Chunk.StreamingUnitSize != GlobalSpatialFoundationV1.StreamingUnitSizeStatus ||
                Region.RegionId != GlobalSpatialFoundationV1.HenanYinRegionId ||
                Region.Authority != "INCLUDED_GLOBAL_CELL_IDS" ||
                Region.BoundaryAuthority != GlobalSpatialFoundationV1.RegionBoundaryAuthority ||
                Region.BoundaryModel != GlobalSpatialFoundationV1.RegionBoundaryModel ||
                Region.PolygonAuthority || Region.CutsGlobalCells ||
                Region.GlobalBounds == null || Region.RegionLocalOrigin == null ||
                Region.GlobalBounds.MinRow != GlobalSpatialFoundationV1.HenanYinMinRow ||
                Region.GlobalBounds.MaxRow != GlobalSpatialFoundationV1.HenanYinMaxRow ||
                Region.GlobalBounds.MinColumn != GlobalSpatialFoundationV1.HenanYinMinColumn ||
                Region.GlobalBounds.MaxColumn != GlobalSpatialFoundationV1.HenanYinMaxColumn ||
                Region.RegionLocalOrigin.X != GlobalSpatialFoundationV1.HenanYinOriginX ||
                Region.RegionLocalOrigin.Y != GlobalSpatialFoundationV1.HenanYinOriginY ||
                Region.RegionLocalOrigin.CellId != GlobalSpatialFoundationV1.HenanYinOriginCellId ||
                Region.RegionLocalOrigin.LocalX != 0d || Region.RegionLocalOrigin.LocalY != 0d ||
                Region.IncludedCellCount != GlobalSpatialFoundationV1.HenanYinIncludedCellCount ||
                Region.IncludedCellIds == null ||
                Region.IncludedCellIds.Count != GlobalSpatialFoundationV1.HenanYinIncludedCellCount ||
                Region.IncludedGlobalChunkIdsSemantics != "DERIVED_TECHNICAL_INDEX" ||
                Region.GeneratedNewCellCount != 0 || Region.CutCellCount != 0)
                throw new InvalidDataException("Global spatial foundation violates the frozen V1 contract.");

            var uniqueCells = new HashSet<ulong>();
            var grid = CreateCellGrid();
            foreach (var value in Region.IncludedCellIds)
            {
                if (!uniqueCells.Add(value) ||
                    !grid.TryDecode(new WorldMapCellId(value), out _, out _))
                    throw new InvalidDataException("Region membership must reference unique Global Cells.");
            }
        }
    }

    public sealed class GlobalSpatialFoundationRecord
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("reuse_conclusion")] public string ReuseConclusion { get; set; }
        [JsonProperty("crs")] public GlobalCrsRecord Crs { get; set; }
        [JsonProperty("grid")] public GlobalGridRecord Grid { get; set; }
        [JsonProperty("chunk")] public GlobalChunkRecord Chunk { get; set; }
    }
    public sealed class GlobalCrsRecord
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("proj_string")] public string ProjString { get; set; }
    }
    public sealed class GlobalGridRecord
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("rows")] public int Rows { get; set; }
        [JsonProperty("columns")] public int Columns { get; set; }
        [JsonProperty("total_cells")] public long TotalCells { get; set; }
        [JsonProperty("cell_size_m")] public int CellSizeMetres { get; set; }
        [JsonProperty("origin_x")] public double OriginX { get; set; }
        [JsonProperty("origin_y")] public double OriginY { get; set; }
        [JsonProperty("origin_unit")] public string OriginUnit { get; set; }
        [JsonProperty("origin_meaning")] public string OriginMeaning { get; set; }
        [JsonProperty("origin_cell_relation")] public string OriginCellRelation { get; set; }
        [JsonProperty("row_direction")] public string RowDirection { get; set; }
        [JsonProperty("column_direction")] public string ColumnDirection { get; set; }
        [JsonProperty("first_cell")] public GlobalCellBoundsRecord FirstCell { get; set; }
        [JsonProperty("world_bounds")] public GlobalBoundsRecord WorldBounds { get; set; }
        [JsonProperty("valid_world_extent")] public GlobalBoundsRecord ValidWorldExtent { get; set; }
        [JsonProperty("valid_world_mask_definition")] public string ValidWorldMaskDefinition { get; set; }
    }
    public sealed class GlobalChunkRecord
    {
        [JsonProperty("cells_per_side")] public int CellsPerSide { get; set; }
        [JsonProperty("rows")] public int Rows { get; set; }
        [JsonProperty("columns")] public int Columns { get; set; }
        [JsonProperty("semantic_status")] public string SemanticStatus { get; set; }
        [JsonProperty("current_purpose")] public string CurrentPurpose { get; set; }
        [JsonProperty("is_world_fact")] public bool IsWorldFact { get; set; }
        [JsonProperty("is_simulation_aggregation")] public bool IsSimulationAggregation { get; set; }
        [JsonProperty("is_terrain_tile")] public bool IsTerrainTile { get; set; }
        [JsonProperty("is_streaming_unit")] public bool IsStreamingUnit { get; set; }
        [JsonProperty("is_storage_block")] public bool IsStorageBlock { get; set; }
        [JsonProperty("legacy_name")] public string LegacyName { get; set; }
        [JsonProperty("current_canonical_name")] public string CurrentCanonicalName { get; set; }
        [JsonProperty("terrain_tile_size")] public string TerrainTileSize { get; set; }
        [JsonProperty("streaming_unit_size")] public string StreamingUnitSize { get; set; }
    }
    public sealed class GlobalRegionSpatialRecord
    {
        [JsonProperty("region_id")] public string RegionId { get; set; }
        [JsonProperty("region_name")] public string RegionName { get; set; }
        [JsonProperty("authority")] public string Authority { get; set; }
        [JsonProperty("boundary_authority")] public string BoundaryAuthority { get; set; }
        [JsonProperty("boundary_model")] public string BoundaryModel { get; set; }
        [JsonProperty("polygon_authority")] public bool PolygonAuthority { get; set; }
        [JsonProperty("cuts_global_cells")] public bool CutsGlobalCells { get; set; }
        [JsonProperty("global_bounds")] public GlobalRegionBoundsRecord GlobalBounds { get; set; }
        [JsonProperty("region_local_origin")] public GlobalRegionOriginRecord RegionLocalOrigin { get; set; }
        [JsonProperty("included_cell_count")] public int IncludedCellCount { get; set; }
        [JsonProperty("included_cell_ids")] public List<ulong> IncludedCellIds { get; set; }
        [JsonProperty("included_global_chunk_ids")] public List<string> IncludedGlobalChunkIds { get; set; }
        [JsonProperty("included_global_chunk_ids_semantics")] public string IncludedGlobalChunkIdsSemantics { get; set; }
        [JsonProperty("primary_places")] public List<string> PrimaryPlaces { get; set; }
        [JsonProperty("production_status")] public string ProductionStatus { get; set; }
        [JsonProperty("terrain_detail_target")] public string TerrainDetailTarget { get; set; }
        [JsonProperty("art_detail_target")] public string ArtDetailTarget { get; set; }
        [JsonProperty("generated_new_cell_count")] public int GeneratedNewCellCount { get; set; }
        [JsonProperty("cut_cell_count")] public int CutCellCount { get; set; }
    }

    public class GlobalBoundsRecord
    {
        [JsonProperty("min_x")] public double MinX { get; set; }
        [JsonProperty("min_y")] public double MinY { get; set; }
        [JsonProperty("max_x")] public double MaxX { get; set; }
        [JsonProperty("max_y")] public double MaxY { get; set; }
    }

    public sealed class GlobalCellBoundsRecord : GlobalBoundsRecord
    {
        [JsonProperty("cell_id")] public ulong CellId { get; set; }
        [JsonProperty("cell_permanent_id")] public string CellPermanentId { get; set; }
        [JsonProperty("row")] public int Row { get; set; }
        [JsonProperty("column")] public int Column { get; set; }
        [JsonProperty("center_x")] public double CenterX { get; set; }
        [JsonProperty("center_y")] public double CenterY { get; set; }
    }

    public sealed class GlobalRegionBoundsRecord
    {
        [JsonProperty("min_row")] public int MinRow { get; set; }
        [JsonProperty("max_row")] public int MaxRow { get; set; }
        [JsonProperty("min_column")] public int MinColumn { get; set; }
        [JsonProperty("max_column")] public int MaxColumn { get; set; }
    }

    public sealed class GlobalRegionOriginRecord
    {
        [JsonProperty("x")] public double X { get; set; }
        [JsonProperty("y")] public double Y { get; set; }
        [JsonProperty("cell_id")] public ulong CellId { get; set; }
        [JsonProperty("cell_permanent_id")] public string CellPermanentId { get; set; }
        [JsonProperty("cell_row")] public int CellRow { get; set; }
        [JsonProperty("cell_column")] public int CellColumn { get; set; }
        [JsonProperty("corner")] public string Corner { get; set; }
        [JsonProperty("local_x")] public double LocalX { get; set; }
        [JsonProperty("local_y")] public double LocalY { get; set; }
    }
}
