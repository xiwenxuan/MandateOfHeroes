using System;

namespace Mandate.Domain
{
    /// <summary>
    /// A permanent, display-name-independent identifier used by saves and content.
    /// </summary>
    [Serializable]
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        public string Value { get; }

        public StableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID cannot be empty.", nameof(value));
            }

            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                var valid = character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '.'
                    || character == '_'
                    || character == '-';

                if (!valid)
                {
                    throw new ArgumentException(
                        "Stable IDs may contain lowercase ASCII letters, digits, '.', '_' and '-'.",
                        nameof(value));
                }
            }

            Value = value;
        }

        public int CompareTo(StableId other) =>
            string.CompareOrdinal(Value, other.Value);

        public bool Equals(StableId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is StableId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(StableId left, StableId right) => left.Equals(right);

        public static bool operator !=(StableId left, StableId right) => !left.Equals(right);
    }
}
