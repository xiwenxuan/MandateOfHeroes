using System;

namespace Mandate.Domain
{
    public enum DaySegment : byte
    {
        Dawn = 0,
        Day = 1,
        Dusk = 2,
        Night = 3
    }

    [Serializable]
    public readonly struct WorldTime : IEquatable<WorldTime>, IComparable<WorldTime>
    {
        public long AbsoluteDay { get; }
        public DaySegment Segment { get; }

        public WorldTime(long absoluteDay, DaySegment segment = DaySegment.Dawn)
        {
            if (absoluteDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteDay));
            }

            if ((byte)segment > (byte)DaySegment.Night)
            {
                throw new ArgumentOutOfRangeException(nameof(segment));
            }

            AbsoluteDay = absoluteDay;
            Segment = segment;
        }

        public WorldTime AdvanceSegments(long segments)
        {
            if (segments < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }

            var current = checked(AbsoluteDay * 4L + (byte)Segment);
            var advanced = checked(current + segments);
            return new WorldTime(advanced / 4L, (DaySegment)(advanced % 4L));
        }

        public WorldTime AdvanceDays(long days)
        {
            if (days < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }

            return new WorldTime(checked(AbsoluteDay + days), Segment);
        }

        public int CompareTo(WorldTime other)
        {
            var dayComparison = AbsoluteDay.CompareTo(other.AbsoluteDay);
            return dayComparison != 0 ? dayComparison : Segment.CompareTo(other.Segment);
        }

        public bool Equals(WorldTime other) =>
            AbsoluteDay == other.AbsoluteDay && Segment == other.Segment;

        public override bool Equals(object obj) =>
            obj is WorldTime other && Equals(other);

        public override int GetHashCode() =>
            unchecked((AbsoluteDay.GetHashCode() * 397) ^ (int)Segment);

        public override string ToString() => $"Day {AbsoluteDay}, {Segment}";
    }
}
