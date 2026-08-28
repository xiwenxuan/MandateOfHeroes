using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public readonly struct GeographicCoordinate : IEquatable<GeographicCoordinate>
    {
        public GeographicCoordinate(double longitudeDegrees, double latitudeDegrees)
        {
            if (double.IsNaN(longitudeDegrees) || double.IsInfinity(longitudeDegrees) ||
                longitudeDegrees < -180d || longitudeDegrees > 180d)
                throw new ArgumentOutOfRangeException(nameof(longitudeDegrees));
            if (double.IsNaN(latitudeDegrees) || double.IsInfinity(latitudeDegrees) ||
                latitudeDegrees < -90d || latitudeDegrees > 90d)
                throw new ArgumentOutOfRangeException(nameof(latitudeDegrees));
            LongitudeDegrees = longitudeDegrees;
            LatitudeDegrees = latitudeDegrees;
        }

        public double LongitudeDegrees { get; }
        public double LatitudeDegrees { get; }
        public bool Equals(GeographicCoordinate other) =>
            LongitudeDegrees.Equals(other.LongitudeDegrees) && LatitudeDegrees.Equals(other.LatitudeDegrees);
        public override bool Equals(object obj) => obj is GeographicCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(LongitudeDegrees, LatitudeDegrees);
    }

    public readonly struct GlobalProjectedCoordinate : IEquatable<GlobalProjectedCoordinate>
    {
        public GlobalProjectedCoordinate(double eastingMetres, double northingMetres)
        {
            if (double.IsNaN(eastingMetres) || double.IsInfinity(eastingMetres) ||
                double.IsNaN(northingMetres) || double.IsInfinity(northingMetres))
                throw new ArgumentOutOfRangeException(nameof(eastingMetres));
            EastingMetres = eastingMetres;
            NorthingMetres = northingMetres;
        }

        public double EastingMetres { get; }
        public double NorthingMetres { get; }
        public bool Equals(GlobalProjectedCoordinate other) =>
            EastingMetres.Equals(other.EastingMetres) && NorthingMetres.Equals(other.NorthingMetres);
        public override bool Equals(object obj) => obj is GlobalProjectedCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(EastingMetres, NorthingMetres);
    }

    public readonly struct LocalPlanarCoordinate : IEquatable<LocalPlanarCoordinate>
    {
        public LocalPlanarCoordinate(double xMetres, double yMetres)
        {
            XMetres = xMetres;
            YMetres = yMetres;
        }
        public double XMetres { get; }
        public double YMetres { get; }
        public bool Equals(LocalPlanarCoordinate other) =>
            XMetres.Equals(other.XMetres) && YMetres.Equals(other.YMetres);
        public override bool Equals(object obj) => obj is LocalPlanarCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(XMetres, YMetres);
    }

    public readonly struct UnityLocalPosition : IEquatable<UnityLocalPosition>
    {
        public UnityLocalPosition(double xMetres, double elevationMetres, double zMetres)
        {
            XMetres = xMetres;
            ElevationMetres = elevationMetres;
            ZMetres = zMetres;
        }
        public double XMetres { get; }
        public double ElevationMetres { get; }
        public double ZMetres { get; }
        public bool Equals(UnityLocalPosition other) => XMetres.Equals(other.XMetres) &&
            ElevationMetres.Equals(other.ElevationMetres) && ZMetres.Equals(other.ZMetres);
        public override bool Equals(object obj) => obj is UnityLocalPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(XMetres, ElevationMetres, ZMetres);
    }

    public readonly struct GlobalChunkId : IEquatable<GlobalChunkId>
    {
        public GlobalChunkId(int row, int column)
        {
            if (row < 0 || column < 0) throw new ArgumentOutOfRangeException(nameof(row));
            Row = row;
            Column = column;
        }
        public int Row { get; }
        public int Column { get; }
        public string PermanentId => $"chunk.hanworld.global.v1.r{Row:D3}.c{Column:D3}";
        public bool Equals(GlobalChunkId other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is GlobalChunkId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Row, Column);
        public override string ToString() => PermanentId;
    }

    public sealed class GlobalChunkGridIndex
    {
        public GlobalChunkGridIndex(CellGridIndex cells, int cellsPerChunk = 16)
        {
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            if (cellsPerChunk <= 0) throw new ArgumentOutOfRangeException(nameof(cellsPerChunk));
            CellsPerChunk = cellsPerChunk;
        }
        public CellGridIndex Cells { get; }
        public int CellsPerChunk { get; }
        public int ChunkRows => (Cells.Rows + CellsPerChunk - 1) / CellsPerChunk;
        public int ChunkColumns => (Cells.Columns + CellsPerChunk - 1) / CellsPerChunk;
        public int ChunkCount => checked(ChunkRows * ChunkColumns);
        public string SemanticStatus => GlobalSpatialFoundationV1.Block16SemanticStatus;
        public string CurrentPurpose => GlobalSpatialFoundationV1.Block16CurrentPurpose;
        public bool IsWorldFact => false;
        public bool IsSimulationAggregation => CellsPerChunk ==
            GlobalSpatialFoundationV1.SimulationAggregationBlockSizeCells;
        public bool IsTerrainTile => false;
        public bool IsStreamingUnit => false;
        public bool IsStorageBlock => false;
        public GlobalChunkId FromCell(int row, int column)
        {
            if (!Cells.Contains(row, column)) throw new ArgumentOutOfRangeException(nameof(row));
            return new GlobalChunkId(row / CellsPerChunk, column / CellsPerChunk);
        }
        public void GetGlobalOrigin(GlobalChunkId chunk, out double x, out double y)
        {
            if (chunk.Row >= ChunkRows || chunk.Column >= ChunkColumns)
                throw new ArgumentOutOfRangeException(nameof(chunk));
            x = Cells.OriginX + chunk.Column * CellsPerChunk * Cells.CellSize;
            y = Cells.OriginY - chunk.Row * CellsPerChunk * Cells.CellSize;
        }
    }

    public sealed class GlobalRegionSpatialDefinition
    {
        public string RegionId;
        public string DisplayName;
        public int MinRow;
        public int MaxRow;
        public int MinColumn;
        public int MaxColumn;
        public GlobalProjectedCoordinate LocalOrigin;
        public List<ulong> IncludedGlobalCellIds = new List<ulong>();
        public List<string> IncludedGlobalChunkIds = new List<string>();
        public string IncludedGlobalChunkIdsSemantics = "DERIVED_TECHNICAL_INDEX";
        public List<string> PrimaryPlaces = new List<string>();
        public string ProductionStatus;
        public string TerrainDetailTarget;
        public string ArtDetailTarget;
    }

    public enum GlobalCellEdgeDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public readonly struct RegionBoundaryEdge : IEquatable<RegionBoundaryEdge>
    {
        public RegionBoundaryEdge(string regionId, WorldMapCellId memberCellId,
            GlobalCellEdgeDirection direction, WorldMapCellId? neighborCellId)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            MemberCellId = memberCellId;
            Direction = direction;
            NeighborCellId = neighborCellId;
        }

        public string RegionId { get; }
        public WorldMapCellId MemberCellId { get; }
        public GlobalCellEdgeDirection Direction { get; }
        public WorldMapCellId? NeighborCellId { get; }

        public bool Equals(RegionBoundaryEdge other) =>
            string.Equals(RegionId, other.RegionId, StringComparison.Ordinal) &&
            MemberCellId.Equals(other.MemberCellId) && Direction == other.Direction &&
            Nullable.Equals(NeighborCellId, other.NeighborCellId);
        public override bool Equals(object obj) => obj is RegionBoundaryEdge other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RegionId, MemberCellId, Direction, NeighborCellId);
    }

    public sealed class RegionCellBoundaryIndex
    {
        private static readonly GlobalCellEdgeDirection[] CardinalDirections =
        {
            GlobalCellEdgeDirection.North,
            GlobalCellEdgeDirection.East,
            GlobalCellEdgeDirection.South,
            GlobalCellEdgeDirection.West
        };

        private readonly CellGridIndex _grid;
        private readonly Dictionary<string, HashSet<WorldMapCellId>> _membersByRegion =
            new Dictionary<string, HashSet<WorldMapCellId>>(StringComparer.Ordinal);
        private readonly Dictionary<WorldMapCellId, List<string>> _regionsByCell =
            new Dictionary<WorldMapCellId, List<string>>();

        public RegionCellBoundaryIndex(CellGridIndex grid,
            IEnumerable<GlobalRegionSpatialDefinition> regions)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (regions == null) throw new ArgumentNullException(nameof(regions));

            foreach (var region in regions)
            {
                if (region == null || string.IsNullOrWhiteSpace(region.RegionId))
                    throw new ArgumentException("Region and RegionId are required.", nameof(regions));
                if (_membersByRegion.ContainsKey(region.RegionId))
                    throw new ArgumentException("Duplicate RegionId: " + region.RegionId, nameof(regions));

                var members = new HashSet<WorldMapCellId>();
                foreach (var value in region.IncludedGlobalCellIds)
                {
                    var cellId = new WorldMapCellId(value);
                    if (!_grid.TryDecode(cellId, out _, out _))
                        throw new ArgumentOutOfRangeException(nameof(regions),
                            "Region references an invalid Global Cell: " + value);
                    if (!members.Add(cellId))
                        throw new ArgumentException("Duplicate Global Cell in Region: " + value,
                            nameof(regions));
                    if (!_regionsByCell.TryGetValue(cellId, out var regionIds))
                    {
                        regionIds = new List<string>();
                        _regionsByCell.Add(cellId, regionIds);
                    }
                    regionIds.Add(region.RegionId);
                }
                _membersByRegion.Add(region.RegionId, members);
            }
        }

        public WorldMapCellId? GetNeighborCell(WorldMapCellId cellId,
            GlobalCellEdgeDirection direction)
        {
            if (!_grid.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(cellId));
            Offset(direction, out var rowOffset, out var columnOffset);
            var neighborRow = row + rowOffset;
            var neighborColumn = column + columnOffset;
            return _grid.Contains(neighborRow, neighborColumn)
                ? _grid.ToCellId(neighborRow, neighborColumn)
                : (WorldMapCellId?)null;
        }

        public IReadOnlyList<RegionBoundaryEdge> GetRegionBoundaryEdges(string regionId)
        {
            var members = GetMembers(regionId);
            var sortedMembers = new List<WorldMapCellId>(members);
            sortedMembers.Sort((left, right) => left.Value.CompareTo(right.Value));
            var result = new List<RegionBoundaryEdge>();
            foreach (var member in sortedMembers)
            {
                foreach (var direction in CardinalDirections)
                {
                    var neighbor = GetNeighborCell(member, direction);
                    if (!neighbor.HasValue || !members.Contains(neighbor.Value))
                        result.Add(new RegionBoundaryEdge(regionId, member, direction, neighbor));
                }
            }
            return result;
        }

        public IReadOnlyList<string> GetAdjacentRegions(string regionId)
        {
            var adjacent = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in GetRegionBoundaryEdges(regionId))
            {
                if (!edge.NeighborCellId.HasValue ||
                    !_regionsByCell.TryGetValue(edge.NeighborCellId.Value, out var regionIds))
                    continue;
                foreach (var candidate in regionIds)
                    if (!string.Equals(candidate, regionId, StringComparison.Ordinal))
                        adjacent.Add(candidate);
            }
            var result = new List<string>(adjacent);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public IReadOnlyList<WorldMapCellId> GetNeighborCellsAcrossRegionBoundary(string regionId)
        {
            var neighborCells = new HashSet<WorldMapCellId>();
            foreach (var edge in GetRegionBoundaryEdges(regionId))
                if (edge.NeighborCellId.HasValue)
                    neighborCells.Add(edge.NeighborCellId.Value);
            var result = new List<WorldMapCellId>(neighborCells);
            result.Sort((left, right) => left.Value.CompareTo(right.Value));
            return result;
        }

        private HashSet<WorldMapCellId> GetMembers(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId) ||
                !_membersByRegion.TryGetValue(regionId, out var members))
                throw new KeyNotFoundException("Unknown RegionId: " + regionId);
            return members;
        }

        private static void Offset(GlobalCellEdgeDirection direction, out int row, out int column)
        {
            switch (direction)
            {
                case GlobalCellEdgeDirection.North: row = -1; column = 0; return;
                case GlobalCellEdgeDirection.East: row = 0; column = 1; return;
                case GlobalCellEdgeDirection.South: row = 1; column = 0; return;
                case GlobalCellEdgeDirection.West: row = 0; column = -1; return;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }

    public static class GlobalSpatialFoundationV1
    {
        public const string FoundationId = "global_spatial_foundation.v1";
        public const string FrozenStatus = "GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN";
        public const string CrsId = "hanworld.albers.china.v0";
        public const string GlobalOriginMeaning = "GLOBAL_GRID_NORTHWEST_CORNER";
        public const string GlobalRowDirection = "north_to_south";
        public const string GlobalColumnDirection = "west_to_east";
        public const string GridSchemaVersion = "hanworld.square-grid.v1";
        public const string GridVersion = "HanWorldV1";
        public const int Rows = 2176;
        public const int Columns = 3314;
        public const int CellSizeMetres = 2000;
        public const int SimulationAggregationBlockSizeCells = 16;
        public const int CanonicalChunkSizeCells = SimulationAggregationBlockSizeCells;
        public const int LegacyStorageBlockSizeCells = 64;
        public const string Block16SemanticStatus = "SUPERSEDED_SEMANTICALLY_RECLASSIFIED";
        public const string Block16CurrentPurpose = "TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK";
        public const string Block16LegacyName = "CANONICAL_GLOBAL_CHUNK_16";
        public const string Block16CurrentCanonicalName = "SIMULATION_AGGREGATION_BLOCK_16";
        public const string TerrainTileSizeStatus =
            "TERRAIN_TILE_SIZE_V1_FROZEN_FROM_REAL_DEM_BENCHMARK_8X8";
        public const string StreamingUnitSizeStatus =
            "PROVISIONAL_24X24_CELLS_DISTINCT_FROM_TERRAIN_TILE";
        public const string RegionBoundaryAuthority = "CELL_MEMBERSHIP";
        public const string RegionBoundaryModel = "CELL_EDGE_DERIVED";
        public const double OriginX = -3417344.395965772d;
        public const double OriginY = 6199580.451937504d;
        public const double GlobalMinX = OriginX;
        public const double GlobalMaxX = OriginX + Columns * CellSizeMetres;
        public const double GlobalMinY = OriginY - Rows * CellSizeMetres;
        public const double GlobalMaxY = OriginY;
        public const string HenanYinRegionId = "HENAN_YIN_REGION";
        public const int HenanYinMinRow = 1152;
        public const int HenanYinMaxRow = 1343;
        public const int HenanYinMinColumn = 1840;
        public const int HenanYinMaxColumn = 2143;
        public const int HenanYinIncludedCellCount = 58368;
        public const double HenanYinOriginX = 262655.6040342278d;
        public const double HenanYinOriginY = 3511580.451937504d;
        public const ulong HenanYinOriginCellId = 4452542UL;
        public const string LuoyangCanonicalPlaceId = "C027";
        public const ulong LuoyangCanonicalCellId = 4114717UL;

        public static CellGridIndex CreateCellGrid() => new CellGridIndex(
            Rows, Columns, OriginX, OriginY, CellSizeMetres, GridSchemaVersion);
        public static GlobalChunkGridIndex CreateChunkGrid() =>
            new GlobalChunkGridIndex(CreateCellGrid(), CanonicalChunkSizeCells);
    }
}
