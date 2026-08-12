using System;

namespace Mandate.Domain
{
    public readonly struct WorldMapCellId : IEquatable<WorldMapCellId>
    {
        public WorldMapCellId(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public static WorldMapCellId FromRowColumn(int row, int column, int columns)
        {
            if (row < 0 || column < 0 || columns <= 0 || column >= columns)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            return new WorldMapCellId(checked((ulong)row * (ulong)columns + (ulong)column));
        }

        public void Decode(int columns, out int row, out int column)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            row = checked((int)(Value / (ulong)columns));
            column = checked((int)(Value % (ulong)columns));
        }

        public bool Equals(WorldMapCellId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is WorldMapCellId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"cell.hanworld.v0.{Value}";
        public static bool operator ==(WorldMapCellId left, WorldMapCellId right) => left.Equals(right);
        public static bool operator !=(WorldMapCellId left, WorldMapCellId right) => !left.Equals(right);
    }
}
