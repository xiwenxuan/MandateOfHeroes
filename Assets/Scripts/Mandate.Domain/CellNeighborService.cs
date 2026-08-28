using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public sealed class CellNeighborService
    {
        private static readonly (int Row, int Column)[] Directions =
        {
            (-1, 0), (-1, 1), (0, 1), (1, 1),
            (1, 0), (1, -1), (0, -1), (-1, -1)
        };

        private readonly CellGridIndex _grid;

        public CellNeighborService(CellGridIndex grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public IReadOnlyList<WorldMapCellId> GetNeighbors(WorldMapCellId id)
        {
            if (!_grid.TryDecode(id, out var row, out var column))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            var result = new List<WorldMapCellId>(8);
            foreach (var direction in Directions)
            {
                var neighborRow = row + direction.Row;
                var neighborColumn = column + direction.Column;
                if (_grid.Contains(neighborRow, neighborColumn))
                {
                    result.Add(_grid.ToCellId(neighborRow, neighborColumn));
                }
            }

            return result;
        }

        public WorldMapCellId? GetNeighborCell(WorldMapCellId id,
            GlobalCellEdgeDirection direction)
        {
            if (!_grid.TryDecode(id, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(id));

            var rowOffset = 0;
            var columnOffset = 0;
            switch (direction)
            {
                case GlobalCellEdgeDirection.North: rowOffset = -1; break;
                case GlobalCellEdgeDirection.East: columnOffset = 1; break;
                case GlobalCellEdgeDirection.South: rowOffset = 1; break;
                case GlobalCellEdgeDirection.West: columnOffset = -1; break;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }

            var neighborRow = row + rowOffset;
            var neighborColumn = column + columnOffset;
            return _grid.Contains(neighborRow, neighborColumn)
                ? _grid.ToCellId(neighborRow, neighborColumn)
                : (WorldMapCellId?)null;
        }
    }
}
