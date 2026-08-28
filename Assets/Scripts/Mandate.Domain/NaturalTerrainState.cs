using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class NaturalSurfaceIds
    {
        public const string Sea = "surface.natural.sea";
        public const string River = "surface.natural.river";
        public const string Lake = "surface.natural.lake";
        public const string Wetland = "surface.natural.wetland";
        public const string Riverbank = "surface.natural.riverbank";
        public const string Sand = "surface.natural.sand";
        public const string Grassland = "surface.natural.grassland";
        public const string SparseWoodland = "surface.natural.sparse_woodland";
        public const string Forest = "surface.natural.forest";
        public const string BareLand = "surface.natural.bare_land";
        public const string Rock = "surface.natural.rock";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Sea, River, Lake, Wetland, Riverbank, Sand, Grassland,
            SparseWoodland, Forest, BareLand, Rock
        };
    }

    public static class NaturalLandformIds
    {
        public const string Plain = "landform.plain";
        public const string Hill = "landform.hill";
        public const string Mountain = "landform.mountain";
        public const string Basin = "landform.basin";
        public const string Valley = "landform.valley";
    }

    public readonly struct NaturalMapCellSample
    {
        public NaturalMapCellSample(WorldMapCellRecord cell, double neighbourhoodMeanElevation)
        {
            Cell = cell;
            NeighbourhoodMeanElevation = neighbourhoodMeanElevation;
        }

        public WorldMapCellRecord Cell { get; }
        public double NeighbourhoodMeanElevation { get; }
    }

    public interface IGlobalNaturalCellSource
    {
        int Rows { get; }
        int Columns { get; }
        double OriginX { get; }
        double OriginY { get; }
        int CellSizeMetres { get; }
        NaturalMapCellSample ReadSample(int row, int column);
    }

    public readonly struct NaturalSurfaceBlend
    {
        public NaturalSurfaceBlend(string primarySurfaceId, string secondarySurfaceId,
            string landformId, double secondaryWeight)
        {
            if (string.IsNullOrWhiteSpace(primarySurfaceId))
                throw new ArgumentException("Primary surface ID is required.", nameof(primarySurfaceId));
            if (secondaryWeight < 0d || secondaryWeight > 1d)
                throw new ArgumentOutOfRangeException(nameof(secondaryWeight));
            PrimarySurfaceId = primarySurfaceId;
            SecondarySurfaceId = secondarySurfaceId ?? primarySurfaceId;
            LandformId = landformId ?? NaturalLandformIds.Plain;
            SecondaryWeight = secondaryWeight;
        }

        public string PrimarySurfaceId { get; }
        public string SecondarySurfaceId { get; }
        public string LandformId { get; }
        public double SecondaryWeight { get; }
    }

    public sealed class NaturalSurfaceClassifier
    {
        public NaturalSurfaceBlend Classify(NaturalMapCellSample sample)
        {
            var cell = sample.Cell;
            var landform = ClassifyLandform(cell.Elevation, cell.SlopeClass,
                sample.NeighbourhoodMeanElevation);
            if ((cell.WaterClass & 1) != 0)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.Sea, NaturalSurfaceIds.Sand,
                    landform, 0.08d);
            if ((cell.WaterClass & 4) != 0)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.Lake, NaturalSurfaceIds.Wetland,
                    landform, 0.18d);
            if ((cell.WaterClass & 2) != 0)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.River, NaturalSurfaceIds.Riverbank,
                    landform, 0.30d);

            var variation = StableVariation(cell.Row, cell.Column);
            if (cell.SlopeClass >= 3 || cell.Elevation >= 2300)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.Rock, NaturalSurfaceIds.BareLand,
                    landform, 0.18d + variation * 0.22d);
            if (cell.Elevation >= 1050 || cell.SlopeClass == 2)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.Forest, NaturalSurfaceIds.Rock,
                    landform, 0.12d + variation * 0.30d);
            if (cell.Elevation >= 420 || cell.SlopeClass == 1)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.SparseWoodland, NaturalSurfaceIds.Grassland,
                    landform, 0.25d + variation * 0.30d);
            if (cell.Elevation < 15 && variation > 0.68d)
                return new NaturalSurfaceBlend(NaturalSurfaceIds.Wetland, NaturalSurfaceIds.Grassland,
                    landform, 0.25d);
            return new NaturalSurfaceBlend(NaturalSurfaceIds.Grassland, NaturalSurfaceIds.SparseWoodland,
                landform, 0.08d + variation * 0.24d);
        }

        private static string ClassifyLandform(short elevation, byte slopeClass, double neighbourMean)
        {
            var delta = elevation - neighbourMean;
            if (delta < -75d) return NaturalLandformIds.Valley;
            if (delta < -28d && slopeClass <= 1) return NaturalLandformIds.Basin;
            if (elevation >= 1200 || slopeClass >= 2) return NaturalLandformIds.Mountain;
            if (elevation >= 260 || slopeClass == 1) return NaturalLandformIds.Hill;
            return NaturalLandformIds.Plain;
        }

        private static double StableVariation(int row, int column)
        {
            unchecked
            {
                uint value = (uint)(row * 73856093) ^ (uint)(column * 19349663) ^ 0x9E3779B9u;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return (value & 0xFFFFu) / 65535d;
            }
        }
    }

    public readonly struct TerrainTileId : IEquatable<TerrainTileId>
    {
        public TerrainTileId(int row, int column)
        {
            if (row < 0 || column < 0) throw new ArgumentOutOfRangeException(nameof(row));
            Row = row;
            Column = column;
        }
        public int Row { get; }
        public int Column { get; }
        public string StableId => $"terrain.tile.hanworld.natural.v1.r{Row:D4}.c{Column:D4}";
        public bool Equals(TerrainTileId other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is TerrainTileId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Row, Column);
        public override string ToString() => StableId;
    }

    public readonly struct TerrainTileDefinition
    {
        public TerrainTileDefinition(TerrainTileId id, int firstRow, int lastRow,
            int firstColumn, int lastColumn, double minX, double minY, double maxX, double maxY)
        {
            Id = id;
            FirstRow = firstRow;
            LastRow = lastRow;
            FirstColumn = firstColumn;
            LastColumn = lastColumn;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
        public TerrainTileId Id { get; }
        public int FirstRow { get; }
        public int LastRow { get; }
        public int FirstColumn { get; }
        public int LastColumn { get; }
        public int CellRows => LastRow - FirstRow + 1;
        public int CellColumns => LastColumn - FirstColumn + 1;
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public bool IsWorldIdentity => false;
        public string SemanticRole => "DERIVED_TERRAIN_PRESENTATION_TILE";
    }

    public sealed class TerrainTileIndex
    {
        public const int FrozenCellsPerSideV1 = 8;
        public const string FrozenStatus = "TERRAIN_TILE_SIZE_V1_FROZEN_FROM_REAL_DEM_BENCHMARK";
        private readonly CellGridIndex _grid;

        public TerrainTileIndex(CellGridIndex grid, int cellsPerSide = FrozenCellsPerSideV1)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (cellsPerSide <= 0) throw new ArgumentOutOfRangeException(nameof(cellsPerSide));
            CellsPerSide = cellsPerSide;
        }
        public int CellsPerSide { get; }
        public int TileRows => (_grid.Rows + CellsPerSide - 1) / CellsPerSide;
        public int TileColumns => (_grid.Columns + CellsPerSide - 1) / CellsPerSide;
        public int TileCount => checked(TileRows * TileColumns);
        public string SemanticRole => "DERIVED_TECHNICAL_PRESENTATION_INDEX";
        public bool IsRegion => false;
        public bool IsSimulationAggregationBlock => false;
        public bool IsStorageBlock => false;

        public TerrainTileId FromCell(int row, int column)
        {
            if (!_grid.Contains(row, column)) throw new ArgumentOutOfRangeException(nameof(row));
            return new TerrainTileId(row / CellsPerSide, column / CellsPerSide);
        }

        public TerrainTileDefinition Get(TerrainTileId id)
        {
            if (id.Row >= TileRows || id.Column >= TileColumns)
                throw new ArgumentOutOfRangeException(nameof(id));
            var firstRow = id.Row * CellsPerSide;
            var firstColumn = id.Column * CellsPerSide;
            var lastRow = Math.Min(_grid.Rows - 1, firstRow + CellsPerSide - 1);
            var lastColumn = Math.Min(_grid.Columns - 1, firstColumn + CellsPerSide - 1);
            var minX = _grid.OriginX + firstColumn * _grid.CellSize;
            var maxX = _grid.OriginX + (lastColumn + 1) * _grid.CellSize;
            var maxY = _grid.OriginY - firstRow * _grid.CellSize;
            var minY = _grid.OriginY - (lastRow + 1) * _grid.CellSize;
            return new TerrainTileDefinition(id, firstRow, lastRow, firstColumn, lastColumn,
                minX, minY, maxX, maxY);
        }
    }

    public readonly struct NaturalTerrainVertex
    {
        public NaturalTerrainVertex(double globalX, double globalY, double sourceElevationMetres,
            double presentationElevationMetres, NaturalSurfaceBlend surface)
        {
            GlobalX = globalX;
            GlobalY = globalY;
            SourceElevationMetres = sourceElevationMetres;
            PresentationElevationMetres = presentationElevationMetres;
            Surface = surface;
        }
        public double GlobalX { get; }
        public double GlobalY { get; }
        public double SourceElevationMetres { get; }
        public double PresentationElevationMetres { get; }
        public NaturalSurfaceBlend Surface { get; }
    }

    public sealed class NaturalTerrainMeshData
    {
        public TerrainTileDefinition Tile;
        public NaturalTerrainVertex[] Vertices;
        public int[] Triangles;
        public long SourceCellReadCount;
    }

    public sealed class TerrainCellBinding
    {
        private readonly CellGridIndex _grid;
        public TerrainCellBinding(CellGridIndex grid) => _grid = grid ?? throw new ArgumentNullException(nameof(grid));

        public bool TryGlobalToCell(GlobalProjectedCoordinate global, out WorldMapCellId cellId) =>
            _grid.TryFromProjected(global.EastingMetres, global.NorthingMetres, out cellId);

        public UnityLocalPosition GlobalToUnity(GlobalProjectedCoordinate global, double elevationMetres,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit = 2000d,
            double verticalMetresPerUnit = 250d)
        {
            if (horizontalMetresPerUnit <= 0d || verticalMetresPerUnit <= 0d)
                throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));
            return new UnityLocalPosition(
                (global.EastingMetres - floatingOrigin.EastingMetres) / horizontalMetresPerUnit,
                elevationMetres / verticalMetresPerUnit,
                (global.NorthingMetres - floatingOrigin.NorthingMetres) / horizontalMetresPerUnit);
        }

        public GlobalProjectedCoordinate UnityToGlobal(UnityLocalPosition local,
            GlobalProjectedCoordinate floatingOrigin, double horizontalMetresPerUnit = 2000d)
        {
            if (horizontalMetresPerUnit <= 0d) throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));
            return new GlobalProjectedCoordinate(
                floatingOrigin.EastingMetres + local.XMetres * horizontalMetresPerUnit,
                floatingOrigin.NorthingMetres + local.ZMetres * horizontalMetresPerUnit);
        }
    }
}
