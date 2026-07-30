using System;
using System.Text;

namespace Mandate.Domain
{
    /// <summary>
    /// Stateless deterministic random values. Every draw is addressed by a complete key,
    /// so adding a draw to one subsystem cannot shift another subsystem's results.
    /// </summary>
    public sealed class NamedRandom
    {
        public const int AlgorithmVersion = 1;

        private readonly ulong _masterSeed;

        public NamedRandom(ulong masterSeed)
        {
            _masterSeed = masterSeed;
        }

        public ulong NextUInt64(
            string systemId,
            StableId entityId,
            long absoluteDay,
            string purposeId,
            uint drawIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("System ID cannot be empty.", nameof(systemId));
            }

            if (absoluteDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteDay));
            }

            if (string.IsNullOrWhiteSpace(purposeId))
            {
                throw new ArgumentException("Purpose ID cannot be empty.", nameof(purposeId));
            }

            var hash = Mix(_masterSeed ^ (ulong)AlgorithmVersion);
            hash = HashText(hash, systemId);
            hash = HashText(hash, entityId.Value);
            hash = Mix(hash ^ (ulong)absoluteDay);
            hash = HashText(hash, purposeId);
            return Mix(hash ^ drawIndex);
        }

        public int Range(
            string systemId,
            StableId entityId,
            long absoluteDay,
            string purposeId,
            int minInclusive,
            int maxExclusive,
            uint drawIndex = 0)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            var span = (ulong)((long)maxExclusive - minInclusive);
            var value = NextUInt64(systemId, entityId, absoluteDay, purposeId, drawIndex);
            return (int)(value % span) + minInclusive;
        }

        public bool CheckBasisPoints(
            string systemId,
            StableId entityId,
            long absoluteDay,
            string purposeId,
            int chanceBasisPoints,
            uint drawIndex = 0)
        {
            if (chanceBasisPoints < 0 || chanceBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException(nameof(chanceBasisPoints));
            }

            return Range(
                systemId,
                entityId,
                absoluteDay,
                purposeId,
                0,
                10_000,
                drawIndex) < chanceBasisPoints;
        }

        private static ulong HashText(ulong seed, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var hash = seed ^ 14695981039346656037UL;
            for (var i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 1099511628211UL;
            }

            return Mix(hash ^ (ulong)bytes.Length);
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
