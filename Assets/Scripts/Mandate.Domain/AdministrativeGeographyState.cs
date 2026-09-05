using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public enum AdministrativeRegionLevel : byte
    {
        Province = 1,
        CommanderyEquivalent = 2,
        County = 3
    }

    public enum AdministrativeGeometryStatus : byte
    {
        None = 0,
        Approximate = 1,
        Provisional = 2,
        Verified = 3
    }

    [Flags]
    public enum AdministrativeBoundaryLevels : byte
    {
        None = 0,
        County = 1,
        CommanderyEquivalent = 2,
        Province = 4
    }

    public sealed class HistoricalNamePeriod
    {
        public HistoricalNamePeriod(string stableId, string displayName,
            int validFromYear, int validToYear)
        {
            StableId = Require(stableId, nameof(stableId));
            DisplayName = Require(displayName, nameof(displayName));
            if (validToYear < validFromYear)
                throw new ArgumentOutOfRangeException(nameof(validToYear));
            ValidFromYear = validFromYear;
            ValidToYear = validToYear;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public int ValidFromYear { get; }
        public int ValidToYear { get; }

        public bool Contains(int year) =>
            year >= ValidFromYear && year <= ValidToYear;

        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("A value is required.", name)
                : value.Trim();
    }

    public static class HistoricalDisplayNameResolver
    {
        public static string Resolve(string stableId, string fallbackName,
            int scenarioStartYear, IEnumerable<HistoricalNamePeriod> periods)
        {
            if (string.IsNullOrWhiteSpace(stableId))
                throw new ArgumentException(nameof(stableId));
            if (string.IsNullOrWhiteSpace(fallbackName))
                throw new ArgumentException(nameof(fallbackName));
            HistoricalNamePeriod selected = null;
            if (periods != null)
            {
                foreach (var candidate in periods)
                {
                    if (candidate == null ||
                        !string.Equals(candidate.StableId, stableId,
                            StringComparison.Ordinal) ||
                        !candidate.Contains(scenarioStartYear))
                        continue;
                    if (selected == null ||
                        candidate.ValidFromYear > selected.ValidFromYear ||
                        candidate.ValidFromYear == selected.ValidFromYear &&
                        string.CompareOrdinal(candidate.DisplayName,
                            selected.DisplayName) < 0)
                        selected = candidate;
                }
            }
            return selected?.DisplayName ?? fallbackName.Trim();
        }
    }

    public sealed class FrozenWorldDisplayNameCatalog
    {
        private readonly Dictionary<string, string> _names;

        public FrozenWorldDisplayNameCatalog(int scenarioStartYear,
            IEnumerable<KeyValuePair<string, string>> fallbacks,
            IEnumerable<HistoricalNamePeriod> periods)
        {
            ScenarioStartYear = scenarioStartYear;
            _names = new Dictionary<string, string>(StringComparer.Ordinal);
            var materializedPeriods = periods == null
                ? new List<HistoricalNamePeriod>()
                : new List<HistoricalNamePeriod>(periods);
            if (fallbacks == null) throw new ArgumentNullException(nameof(fallbacks));
            foreach (var fallback in fallbacks)
            {
                if (_names.ContainsKey(fallback.Key))
                    throw new ArgumentException("Duplicate stable name ID: " +
                        fallback.Key, nameof(fallbacks));
                _names.Add(fallback.Key, HistoricalDisplayNameResolver.Resolve(
                    fallback.Key, fallback.Value, scenarioStartYear,
                    materializedPeriods));
            }
        }

        public int ScenarioStartYear { get; }

        public string Resolve(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) ||
                !_names.TryGetValue(stableId, out var value))
                throw new KeyNotFoundException("Unknown stable name ID: " + stableId);
            return value;
        }
    }

    public sealed class AdministrativeRegionDefinition
    {
        public AdministrativeRegionDefinition(string id,
            AdministrativeRegionLevel level, string regionType,
            string parentRegionId, string stableGeographyId,
            string displayName, AdministrativeGeometryStatus geometryStatus,
            string sourceGeometryStatus, string confidence, bool provisional)
        {
            Id = Require(id, nameof(id));
            Level = level;
            RegionType = Require(regionType, nameof(regionType));
            ParentRegionId = (parentRegionId ?? string.Empty).Trim();
            StableGeographyId = (stableGeographyId ?? string.Empty).Trim();
            DisplayName = Require(displayName, nameof(displayName));
            GeometryStatus = geometryStatus;
            SourceGeometryStatus = string.IsNullOrWhiteSpace(sourceGeometryStatus)
                ? "none" : sourceGeometryStatus.Trim();
            Confidence = string.IsNullOrWhiteSpace(confidence)
                ? "unknown" : confidence.Trim();
            Provisional = provisional;
        }

        public string Id { get; }
        public AdministrativeRegionLevel Level { get; }
        public string RegionType { get; }
        public string ParentRegionId { get; }
        public string StableGeographyId { get; }
        public string DisplayName { get; }
        public AdministrativeGeometryStatus GeometryStatus { get; }
        public string SourceGeometryStatus { get; }
        public string Confidence { get; }
        public bool Provisional { get; }

        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("A value is required.", name)
                : value.Trim();
    }

    public sealed class AdministrativeRegionCatalog
    {
        private readonly Dictionary<string, AdministrativeRegionDefinition>
            _regionsById = new Dictionary<string, AdministrativeRegionDefinition>(
                StringComparer.Ordinal);

        public AdministrativeRegionCatalog(
            IEnumerable<AdministrativeRegionDefinition> regions)
        {
            if (regions == null) throw new ArgumentNullException(nameof(regions));
            foreach (var region in regions)
            {
                if (region == null) throw new ArgumentException(
                    "Administrative regions cannot contain null.", nameof(regions));
                if (_regionsById.ContainsKey(region.Id))
                    throw new ArgumentException("Duplicate RegionId: " + region.Id,
                        nameof(regions));
                _regionsById.Add(region.Id, region);
            }
            ValidateParentsAndCycles();
        }

        public int Count => _regionsById.Count;
        public IEnumerable<AdministrativeRegionDefinition> Regions =>
            _regionsById.Values;

        public AdministrativeRegionDefinition Get(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId) ||
                !_regionsById.TryGetValue(regionId, out var value))
                throw new KeyNotFoundException("Unknown AdministrativeRegion: " +
                    regionId);
            return value;
        }

        public bool TryGet(string regionId,
            out AdministrativeRegionDefinition value)
        {
            value = null;
            return !string.IsNullOrWhiteSpace(regionId) &&
                _regionsById.TryGetValue(regionId, out value);
        }

        public void ResolveCountyHierarchy(string countyId,
            out AdministrativeRegionDefinition county,
            out AdministrativeRegionDefinition commandery,
            out AdministrativeRegionDefinition province)
        {
            county = Get(countyId);
            if (county.Level != AdministrativeRegionLevel.County)
                throw new ArgumentException("The selected region is not a County.",
                    nameof(countyId));
            commandery = Get(county.ParentRegionId);
            province = Get(commandery.ParentRegionId);
        }

        private void ValidateParentsAndCycles()
        {
            foreach (var pair in _regionsById)
            {
                var region = pair.Value;
                if (region.Level == AdministrativeRegionLevel.Province)
                {
                    if (!string.IsNullOrEmpty(region.ParentRegionId))
                        throw new InvalidOperationException(
                            "Province cannot have an administrative parent: " +
                            region.Id);
                }
                else
                {
                    if (!_regionsById.TryGetValue(region.ParentRegionId,
                            out var parent))
                        throw new InvalidOperationException(
                            "Missing administrative parent for " + region.Id);
                    var expected = region.Level ==
                        AdministrativeRegionLevel.County
                        ? AdministrativeRegionLevel.CommanderyEquivalent
                        : AdministrativeRegionLevel.Province;
                    if (parent.Level != expected)
                        throw new InvalidOperationException(
                            "Administrative parent level mismatch for " + region.Id);
                }

                var visited = new HashSet<string>(StringComparer.Ordinal);
                var cursor = region;
                while (cursor != null)
                {
                    if (!visited.Add(cursor.Id))
                        throw new InvalidOperationException(
                            "Administrative parent cycle at " + cursor.Id);
                    cursor = string.IsNullOrEmpty(cursor.ParentRegionId)
                        ? null : Get(cursor.ParentRegionId);
                }
            }
        }
    }

    public readonly struct CellAdministrativeAssignment : IEquatable<
        CellAdministrativeAssignment>
    {
        public CellAdministrativeAssignment(ushort provinceCode,
            ushort commanderyCode, ushort countyCode, ushort noneCode,
            string provinceRegionId, string commanderyRegionId,
            string countyRegionId)
        {
            ProvinceCode = provinceCode;
            CommanderyCode = commanderyCode;
            CountyCode = countyCode;
            NoneCode = noneCode;
            ProvinceRegionId = provinceRegionId ?? string.Empty;
            CommanderyRegionId = commanderyRegionId ?? string.Empty;
            CountyRegionId = countyRegionId ?? string.Empty;
            var allMapped = provinceCode != noneCode &&
                commanderyCode != noneCode && countyCode != noneCode;
            var allNamed = !string.IsNullOrEmpty(ProvinceRegionId) &&
                !string.IsNullOrEmpty(CommanderyRegionId) &&
                !string.IsNullOrEmpty(CountyRegionId);
            if (allMapped != allNamed)
                throw new ArgumentException(
                    "Administrative Cell assignment must resolve all three levels or none.");
        }

        public ushort ProvinceCode { get; }
        public ushort CommanderyCode { get; }
        public ushort CountyCode { get; }
        public ushort NoneCode { get; }
        public string ProvinceRegionId { get; }
        public string CommanderyRegionId { get; }
        public string CountyRegionId { get; }
        public bool IsMapped => CountyCode != NoneCode;

        public bool Equals(CellAdministrativeAssignment other) =>
            ProvinceCode == other.ProvinceCode &&
            CommanderyCode == other.CommanderyCode &&
            CountyCode == other.CountyCode && NoneCode == other.NoneCode &&
            string.Equals(ProvinceRegionId, other.ProvinceRegionId,
                StringComparison.Ordinal) &&
            string.Equals(CommanderyRegionId, other.CommanderyRegionId,
                StringComparison.Ordinal) &&
            string.Equals(CountyRegionId, other.CountyRegionId,
                StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is CellAdministrativeAssignment other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ProvinceCode,
            CommanderyCode, CountyCode, NoneCode, ProvinceRegionId,
            CommanderyRegionId, CountyRegionId);
    }

    public readonly struct CellAdministrativeCodes
    {
        public CellAdministrativeCodes(ushort provinceCode,
            ushort commanderyCode, ushort countyCode)
        {
            ProvinceCode = provinceCode;
            CommanderyCode = commanderyCode;
            CountyCode = countyCode;
        }
        public ushort ProvinceCode { get; }
        public ushort CommanderyCode { get; }
        public ushort CountyCode { get; }
    }

    public interface ICellAdministrativeAssignmentSource
    {
        int Rows { get; }
        int Columns { get; }
        int ChunkSize { get; }
        string RevisionId { get; }
        AdministrativeRegionCatalog RegionCatalog { get; }
        CellAdministrativeAssignment ReadAssignment(int row, int column);
    }

    public readonly struct AdministrativeBoundarySegment
    {
        public AdministrativeBoundarySegment(int row, int column,
            GlobalCellEdgeDirection direction,
            AdministrativeBoundaryLevels levels,
            CellAdministrativeAssignment first,
            CellAdministrativeAssignment second)
        {
            if (direction != GlobalCellEdgeDirection.East &&
                direction != GlobalCellEdgeDirection.South)
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (levels == AdministrativeBoundaryLevels.None)
                throw new ArgumentOutOfRangeException(nameof(levels));
            Row = row;
            Column = column;
            Direction = direction;
            Levels = levels;
            First = first;
            Second = second;
        }

        public int Row { get; }
        public int Column { get; }
        public GlobalCellEdgeDirection Direction { get; }
        public AdministrativeBoundaryLevels Levels { get; }
        public CellAdministrativeAssignment First { get; }
        public CellAdministrativeAssignment Second { get; }

        public AdministrativeRegionLevel HighestLevel =>
            (Levels & AdministrativeBoundaryLevels.Province) != 0
                ? AdministrativeRegionLevel.Province
                : (Levels & AdministrativeBoundaryLevels.CommanderyEquivalent) != 0
                    ? AdministrativeRegionLevel.CommanderyEquivalent
                    : AdministrativeRegionLevel.County;

        public bool TouchesCounty(string countyRegionId) =>
            string.Equals(First.CountyRegionId, countyRegionId,
                StringComparison.Ordinal) ||
            string.Equals(Second.CountyRegionId, countyRegionId,
                StringComparison.Ordinal);
    }

    public sealed class AdministrativeBoundaryChunk
    {
        public AdministrativeBoundaryChunk(int chunkRow, int chunkColumn)
        {
            ChunkRow = chunkRow;
            ChunkColumn = chunkColumn;
        }
        public int ChunkRow { get; }
        public int ChunkColumn { get; }
        public List<AdministrativeBoundarySegment> Segments { get; } =
            new List<AdministrativeBoundarySegment>();
    }

    public sealed class AdministrativeRegionSpatialSummary
    {
        public AdministrativeRegionSpatialSummary(
            AdministrativeRegionDefinition region)
        {
            Region = region ?? throw new ArgumentNullException(nameof(region));
            MinRow = int.MaxValue;
            MinColumn = int.MaxValue;
            MaxRow = -1;
            MaxColumn = -1;
        }

        public AdministrativeRegionDefinition Region { get; }
        public long CellCount { get; private set; }
        public int MinRow { get; private set; }
        public int MaxRow { get; private set; }
        public int MinColumn { get; private set; }
        public int MaxColumn { get; private set; }
        public int CenterRow => CellCount == 0 ? -1 :
            (int)Math.Round(_rowSum / (double)CellCount);
        public int CenterColumn => CellCount == 0 ? -1 :
            (int)Math.Round(_columnSum / (double)CellCount);
        private long _rowSum;
        private long _columnSum;

        internal void Include(int row, int column)
        {
            CellCount++;
            MinRow = Math.Min(MinRow, row);
            MaxRow = Math.Max(MaxRow, row);
            MinColumn = Math.Min(MinColumn, column);
            MaxColumn = Math.Max(MaxColumn, column);
            _rowSum += row;
            _columnSum += column;
        }
    }

    public sealed class AdministrativeBoundaryTopology
    {
        public AdministrativeBoundaryTopology(string revisionId, int rows,
            int columns, int chunkSize,
            List<AdministrativeBoundaryChunk> chunks,
            Dictionary<string, AdministrativeRegionSpatialSummary> summaries,
            long mappedCellCount, long provinceBoundaryCount,
            long commanderyBoundaryCount, long countyBoundaryCount,
            string deterministicSummary)
        {
            RevisionId = revisionId;
            Rows = rows;
            Columns = columns;
            ChunkSize = chunkSize;
            Chunks = chunks;
            RegionSummaries = summaries;
            MappedCellCount = mappedCellCount;
            ProvinceBoundaryCount = provinceBoundaryCount;
            CommanderyBoundaryCount = commanderyBoundaryCount;
            CountyBoundaryCount = countyBoundaryCount;
            DeterministicSummary = deterministicSummary;
        }

        public string RevisionId { get; }
        public int Rows { get; }
        public int Columns { get; }
        public int ChunkSize { get; }
        public IReadOnlyList<AdministrativeBoundaryChunk> Chunks { get; }
        public IReadOnlyDictionary<string, AdministrativeRegionSpatialSummary>
            RegionSummaries { get; }
        public long MappedCellCount { get; }
        public long ProvinceBoundaryCount { get; }
        public long CommanderyBoundaryCount { get; }
        public long CountyBoundaryCount { get; }
        public string DeterministicSummary { get; }
        public long SegmentCount
        {
            get
            {
                long count = 0;
                foreach (var chunk in Chunks) count += chunk.Segments.Count;
                return count;
            }
        }

        public AdministrativeRegionSpatialSummary GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId) ||
                !RegionSummaries.TryGetValue(regionId, out var value))
                throw new KeyNotFoundException("Region has no mapped Cells: " +
                    regionId);
            return value;
        }
    }

    public static class AdministrativeBoundaryTopologyBuilder
    {
        public static AdministrativeBoundaryTopology Build(
            ICellAdministrativeAssignmentSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Rows <= 0 || source.Columns <= 0 ||
                source.ChunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(source));

            var summaries = new Dictionary<string,
                AdministrativeRegionSpatialSummary>(StringComparer.Ordinal);
            foreach (var region in source.RegionCatalog.Regions)
                summaries.Add(region.Id,
                    new AdministrativeRegionSpatialSummary(region));

            var chunks = new Dictionary<long, AdministrativeBoundaryChunk>();
            long mapped = 0;
            long provinceBoundaries = 0;
            long commanderyBoundaries = 0;
            long countyBoundaries = 0;
            var hashText = new StringBuilder();
            hashText.Append(source.RevisionId).Append('|')
                .Append(source.Rows).Append('x').Append(source.Columns)
                .Append('|').Append(source.ChunkSize).Append('\n');

            var current = ReadRow(source, 0);
            for (var row = 0; row < source.Rows; row++)
            {
                var next = row + 1 < source.Rows
                    ? ReadRow(source, row + 1) : null;
                for (var column = 0; column < source.Columns; column++)
                {
                    var cell = current[column];
                    if (cell.IsMapped)
                    {
                        mapped++;
                        Include(summaries, cell.ProvinceRegionId, row, column);
                        Include(summaries, cell.CommanderyRegionId, row, column);
                        Include(summaries, cell.CountyRegionId, row, column);
                    }

                    if (column + 1 < source.Columns)
                        AddIfBoundary(row, column,
                            GlobalCellEdgeDirection.East, cell,
                            current[column + 1], source.ChunkSize, chunks,
                            hashText, ref provinceBoundaries,
                            ref commanderyBoundaries, ref countyBoundaries);
                    if (next != null)
                        AddIfBoundary(row, column,
                            GlobalCellEdgeDirection.South, cell, next[column],
                            source.ChunkSize, chunks, hashText,
                            ref provinceBoundaries, ref commanderyBoundaries,
                            ref countyBoundaries);
                }
                current = next;
            }

            var orderedChunks = new List<AdministrativeBoundaryChunk>(
                chunks.Values);
            orderedChunks.Sort((left, right) =>
            {
                var row = left.ChunkRow.CompareTo(right.ChunkRow);
                return row != 0 ? row :
                    left.ChunkColumn.CompareTo(right.ChunkColumn);
            });
            var nonEmptySummaries = new Dictionary<string,
                AdministrativeRegionSpatialSummary>(StringComparer.Ordinal);
            foreach (var pair in summaries)
                if (pair.Value.CellCount > 0)
                    nonEmptySummaries.Add(pair.Key, pair.Value);
            hashText.Append("mapped=").Append(mapped)
                .Append("|p=").Append(provinceBoundaries)
                .Append("|m=").Append(commanderyBoundaries)
                .Append("|c=").Append(countyBoundaries);
            return new AdministrativeBoundaryTopology(source.RevisionId,
                source.Rows, source.Columns, source.ChunkSize, orderedChunks,
                nonEmptySummaries, mapped, provinceBoundaries,
                commanderyBoundaries, countyBoundaries,
                Sha256(hashText.ToString()));
        }

        private static CellAdministrativeAssignment[] ReadRow(
            ICellAdministrativeAssignmentSource source, int row)
        {
            var result = new CellAdministrativeAssignment[source.Columns];
            for (var column = 0; column < source.Columns; column++)
                result[column] = source.ReadAssignment(row, column);
            return result;
        }

        private static void Include(Dictionary<string,
            AdministrativeRegionSpatialSummary> summaries, string regionId,
            int row, int column)
        {
            if (!summaries.TryGetValue(regionId, out var summary))
                throw new InvalidOperationException(
                    "Cell references an unknown AdministrativeRegion: " +
                    regionId);
            summary.Include(row, column);
        }

        private static void AddIfBoundary(int row, int column,
            GlobalCellEdgeDirection direction,
            CellAdministrativeAssignment first,
            CellAdministrativeAssignment second, int chunkSize,
            Dictionary<long, AdministrativeBoundaryChunk> chunks,
            StringBuilder hashText, ref long provinceBoundaries,
            ref long commanderyBoundaries, ref long countyBoundaries)
        {
            // The authored package uses None outside the current administrative
            // coverage. Coastlines and package edges are not invented as
            // historical administrative boundaries.
            if (!first.IsMapped || !second.IsMapped) return;
            var levels = AdministrativeBoundaryLevels.None;
            if (first.CountyCode != second.CountyCode)
            {
                levels |= AdministrativeBoundaryLevels.County;
                countyBoundaries++;
            }
            if (first.CommanderyCode != second.CommanderyCode)
            {
                levels |= AdministrativeBoundaryLevels.CommanderyEquivalent;
                commanderyBoundaries++;
            }
            if (first.ProvinceCode != second.ProvinceCode)
            {
                levels |= AdministrativeBoundaryLevels.Province;
                provinceBoundaries++;
            }
            if (levels == AdministrativeBoundaryLevels.None) return;

            var chunkRow = row / chunkSize;
            var chunkColumn = column / chunkSize;
            var key = ((long)chunkRow << 32) | (uint)chunkColumn;
            if (!chunks.TryGetValue(key, out var chunk))
            {
                chunk = new AdministrativeBoundaryChunk(chunkRow, chunkColumn);
                chunks.Add(key, chunk);
            }
            var segment = new AdministrativeBoundarySegment(row, column,
                direction, levels, first, second);
            chunk.Segments.Add(segment);
            hashText.Append(row).Append(',').Append(column).Append(',')
                .Append((byte)direction).Append(',').Append((byte)levels)
                .Append(',').Append(first.ProvinceCode).Append(',')
                .Append(first.CommanderyCode).Append(',')
                .Append(first.CountyCode).Append(',')
                .Append(second.ProvinceCode).Append(',')
                .Append(second.CommanderyCode).Append(',')
                .Append(second.CountyCode).Append('\n');
        }

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2"));
                return result.ToString();
            }
        }
    }
}
