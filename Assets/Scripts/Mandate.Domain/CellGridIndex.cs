using System;

namespace Mandate.Domain
{
    public sealed class CellGridIndex
    {
        public CellGridIndex(int rows, int columns, double originX, double originY, double cellSize,
            string gridSchemaVersion = "hanworld.square-grid.v1")
        {
            if (rows <= 0 || columns <= 0 || cellSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            Rows = rows;
            Columns = columns;
            OriginX = originX;
            OriginY = originY;
            CellSize = cellSize;
            GridSchemaVersion = string.IsNullOrWhiteSpace(gridSchemaVersion)
                ? throw new ArgumentException("Grid schema version is required.", nameof(gridSchemaVersion))
                : gridSchemaVersion;
        }

        public int Rows { get; }
        public int Columns { get; }
        public double OriginX { get; }
        public double OriginY { get; }
        public double CellSize { get; }
        public string GridSchemaVersion { get; }
        public ulong CellCount => checked((ulong)Rows * (ulong)Columns);

        public bool Contains(int row, int column) => row >= 0 && row < Rows && column >= 0 && column < Columns;

        public WorldMapCellId ToCellId(int row, int column)
        {
            if (!Contains(row, column))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            return WorldMapCellId.FromRowColumn(row, column, Columns);
        }

        public bool TryDecode(WorldMapCellId id, out int row, out int column)
        {
            id.Decode(Columns, out row, out column);
            return Contains(row, column);
        }

        public bool TryFromProjected(double x, double y, out WorldMapCellId id)
        {
            var column = (int)Math.Floor((x - OriginX) / CellSize);
            var row = (int)Math.Floor((OriginY - y) / CellSize);
            if (!Contains(row, column))
            {
                id = default;
                return false;
            }

            id = ToCellId(row, column);
            return true;
        }

        public void GetCenter(int row, int column, out double x, out double y)
        {
            if (!Contains(row, column))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            x = OriginX + (column + 0.5d) * CellSize;
            y = OriginY - (row + 0.5d) * CellSize;
        }
    }
}
