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
    }
}
