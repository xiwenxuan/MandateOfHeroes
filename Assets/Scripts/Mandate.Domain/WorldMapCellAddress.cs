using System;

namespace Mandate.Domain
{
    public readonly struct WorldMapCellAddress : IEquatable<WorldMapCellAddress>
    {
        public WorldMapCellAddress(string gridSchemaVersion, int gridX, int gridY, ulong cellId64)
        {
            if (string.IsNullOrWhiteSpace(gridSchemaVersion))
            {
                throw new ArgumentException("Grid schema version is required.", nameof(gridSchemaVersion));
            }
            if (gridX < 0 || gridY < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridX));
            }

            GridSchemaVersion = gridSchemaVersion;
            GridX = gridX;
            GridY = gridY;
            CellId64 = cellId64;
        }

        public string GridSchemaVersion { get; }
        public int GridX { get; }
        public int GridY { get; }
        public ulong CellId64 { get; }

        public static WorldMapCellAddress FromGrid(CellGridIndex grid, int gridX, int gridY)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            var id = grid.ToCellId(gridY, gridX);
            return new WorldMapCellAddress(grid.GridSchemaVersion, gridX, gridY, id.Value);
        }

        public bool Equals(WorldMapCellAddress other) =>
            CellId64 == other.CellId64 && GridX == other.GridX && GridY == other.GridY &&
            string.Equals(GridSchemaVersion, other.GridSchemaVersion, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is WorldMapCellAddress other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GridSchemaVersion, GridX, GridY, CellId64);
        public override string ToString() => $"{GridSchemaVersion}:{GridX}:{GridY}:{CellId64}";
        public static bool operator ==(WorldMapCellAddress left, WorldMapCellAddress right) => left.Equals(right);
        public static bool operator !=(WorldMapCellAddress left, WorldMapCellAddress right) => !left.Equals(right);
    }
}
