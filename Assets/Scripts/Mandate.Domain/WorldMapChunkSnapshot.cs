using System;

namespace Mandate.Domain
{
    public sealed class WorldMapChunkSnapshot
    {
        public WorldMapChunkSnapshot(int chunkRow, int chunkColumn, int rowCount, int columnCount,
            WorldMapCellRecord[] cells)
        {
            if (rowCount <= 0 || columnCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            if (cells == null || cells.Length != rowCount * columnCount)
                throw new ArgumentException("Chunk Cell array does not match its dimensions.", nameof(cells));
            ChunkRow = chunkRow;
            ChunkColumn = chunkColumn;
            RowCount = rowCount;
            ColumnCount = columnCount;
            Cells = cells;
        }

        public int ChunkRow { get; }
        public int ChunkColumn { get; }
        public int RowCount { get; }
        public int ColumnCount { get; }
        public WorldMapCellRecord[] Cells { get; }

        public WorldMapCellRecord GetLocal(int row, int column)
        {
            if (row < 0 || row >= RowCount || column < 0 || column >= ColumnCount)
                throw new ArgumentOutOfRangeException(nameof(row));
            return Cells[row * ColumnCount + column];
        }
    }
}
