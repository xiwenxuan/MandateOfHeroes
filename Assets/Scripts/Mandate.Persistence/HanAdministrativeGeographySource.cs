using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanAdministrativeGeographySource :
        ICellAdministrativeAssignmentSource, IDisposable
    {
        public const string ExpectedSchema =
            "mandate.han-administrative-geography-runtime.v1";

        private readonly WorldMapDataReader _reader;
        private readonly AdministrativeRegionDefinition[] _provinces;
        private readonly AdministrativeRegionDefinition[] _commanderies;
        private readonly AdministrativeRegionDefinition[] _counties;

        public HanAdministrativeGeographySource(string worldPackageRoot,
            int scenarioStartYear)
        {
            if (string.IsNullOrWhiteSpace(worldPackageRoot))
                throw new ArgumentException(nameof(worldPackageRoot));
            _reader = new WorldMapDataReader(worldPackageRoot);
            var metadataPath = Path.Combine(worldPackageRoot, "metadata",
                "administrative_regions_v1.json");
            Metadata = JsonConvert.DeserializeObject<
                HanAdministrativeGeographyRuntimeRecord>(
                    File.ReadAllText(metadataPath)) ??
                throw new InvalidDataException(
                    "Administrative geography metadata is empty.");
            if (!string.Equals(Metadata.Schema, ExpectedSchema,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(Metadata.RevisionId))
                throw new InvalidDataException(
                    "Unsupported administrative geography metadata.");

            var periods = Metadata.NamePeriods.Select(item =>
                new HistoricalNamePeriod(item.StableId, item.DisplayName,
                    item.ValidFromYear, item.ValidToYear)).ToArray();
            var frozenNames = new FrozenWorldDisplayNameCatalog(
                scenarioStartYear,
                Metadata.Regions.Select(item =>
                    new KeyValuePair<string, string>(item.Id,
                        item.FallbackDisplayName)), periods);
            var definitions = Metadata.Regions.Select(item =>
                new AdministrativeRegionDefinition(item.Id,
                    ParseLevel(item.Level), item.RegionType,
                    item.ParentRegionId, item.StableGeographyId,
                    frozenNames.Resolve(item.Id),
                    ParseGeometryStatus(item.GeometryStatus),
                    item.SourceGeometryStatus, item.Confidence,
                    item.Provisional)).ToArray();
            RegionCatalog = new AdministrativeRegionCatalog(definitions);
            ScenarioStartYear = scenarioStartYear;
            _provinces = ResolveCodeCatalog(_reader.AdminCatalog.Provinces,
                AdministrativeRegionLevel.Province);
            _commanderies = ResolveCodeCatalog(
                _reader.AdminCatalog.Commanderies,
                AdministrativeRegionLevel.CommanderyEquivalent);
            _counties = ResolveCodeCatalog(_reader.AdminCatalog.Counties,
                AdministrativeRegionLevel.County);
            ValidateDeclaredCounts();
        }

        public HanAdministrativeGeographyRuntimeRecord Metadata { get; }
        public int ScenarioStartYear { get; }
        public int Rows => _reader.Manifest.Rows;
        public int Columns => _reader.Manifest.Columns;
        public int ChunkSize => _reader.Manifest.ChunkSize;
        public string RevisionId => Metadata.RevisionId + ":" +
            _reader.Manifest.GridVersion;
        public AdministrativeRegionCatalog RegionCatalog { get; }
        public ushort NoneCode => _reader.AdminCatalog.NoneCode;
        public WorldMapLocationFeatureCollection Cities => _reader.Cities;

        public CellAdministrativeAssignment ReadAssignment(int row,
            int column)
        {
            var codes = _reader.ReadAdministrativeCodes(row, column);
            var allNone = codes.ProvinceCode == NoneCode &&
                codes.CommanderyCode == NoneCode &&
                codes.CountyCode == NoneCode;
            if (allNone)
                return new CellAdministrativeAssignment(NoneCode, NoneCode,
                    NoneCode, NoneCode, string.Empty, string.Empty,
                    string.Empty);
            if (codes.ProvinceCode == NoneCode ||
                codes.CommanderyCode == NoneCode ||
                codes.CountyCode == NoneCode)
                throw new InvalidDataException(
                    "Cell has a partial administrative assignment at " +
                    row + "," + column + ".");
            var province = Resolve(_provinces, codes.ProvinceCode, "Province");
            var commandery = Resolve(_commanderies, codes.CommanderyCode,
                "CommanderyEquivalent");
            var county = Resolve(_counties, codes.CountyCode, "County");
            if (!string.Equals(county.ParentRegionId, commandery.Id,
                    StringComparison.Ordinal) ||
                !string.Equals(commandery.ParentRegionId, province.Id,
                    StringComparison.Ordinal))
                throw new InvalidDataException(
                    "Cell administrative hierarchy is inconsistent at " +
                    row + "," + column + ".");
            return new CellAdministrativeAssignment(codes.ProvinceCode,
                codes.CommanderyCode, codes.CountyCode, NoneCode,
                province.Id, commandery.Id, county.Id);
        }

        public bool TryGetCountyAtCell(int row, int column,
            out AdministrativeRegionDefinition county)
        {
            county = null;
            var assignment = ReadAssignment(row, column);
            if (!assignment.IsMapped) return false;
            county = RegionCatalog.Get(assignment.CountyRegionId);
            return true;
        }

        public byte ReadRoadClass(int row, int column) =>
            _reader.ReadRoadClass(row, column);

        public void Dispose() => _reader.Dispose();

        private AdministrativeRegionDefinition[] ResolveCodeCatalog(
            IReadOnlyList<string> ids, AdministrativeRegionLevel level)
        {
            if (ids == null) throw new InvalidDataException(
                "Administrative code catalog is missing.");
            var result = new AdministrativeRegionDefinition[ids.Count];
            for (var index = 0; index < ids.Count; index++)
            {
                var definition = RegionCatalog.Get(ids[index]);
                if (definition.Level != level)
                    throw new InvalidDataException(
                        "Administrative code level mismatch: " + ids[index]);
                result[index] = definition;
            }
            return result;
        }

        private void ValidateDeclaredCounts()
        {
            if (Metadata.ProvinceCount != _provinces.Length ||
                Metadata.CommanderyEquivalentCount != _commanderies.Length ||
                Metadata.CountyCount != _counties.Length ||
                RegionCatalog.Count != _provinces.Length +
                    _commanderies.Length + _counties.Length)
                throw new InvalidDataException(
                    "Administrative runtime metadata counts do not match HanWorldV1.");
        }

        private static AdministrativeRegionDefinition Resolve(
            AdministrativeRegionDefinition[] values, ushort code,
            string level)
        {
            if (code >= values.Length)
                throw new InvalidDataException(level +
                    " code is outside its catalog: " + code);
            return values[code];
        }

        private static AdministrativeRegionLevel ParseLevel(string value)
        {
            if (!Enum.TryParse(value, true,
                    out AdministrativeRegionLevel result))
                throw new InvalidDataException(
                    "Unknown administrative level: " + value);
            return result;
        }

        private static AdministrativeGeometryStatus ParseGeometryStatus(
            string value)
        {
            if (!Enum.TryParse(value, true,
                    out AdministrativeGeometryStatus result))
                throw new InvalidDataException(
                    "Unknown administrative geometry status: " + value);
            return result;
        }
    }

    public sealed class HanAdministrativeGeographyRuntimeRecord
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("revision_id")] public string RevisionId { get; set; }
        [JsonProperty("province_count")] public int ProvinceCount { get; set; }
        [JsonProperty("commandery_equivalent_count")]
        public int CommanderyEquivalentCount { get; set; }
        [JsonProperty("county_count")] public int CountyCount { get; set; }
        [JsonProperty("regions")]
        public List<HanAdministrativeRegionRuntimeRecord> Regions { get; set; } =
            new List<HanAdministrativeRegionRuntimeRecord>();
        [JsonProperty("name_periods")]
        public List<HanAdministrativeNamePeriodRecord> NamePeriods { get; set; } =
            new List<HanAdministrativeNamePeriodRecord>();
    }

    public sealed class HanAdministrativeRegionRuntimeRecord
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("level")] public string Level { get; set; }
        [JsonProperty("region_type")] public string RegionType { get; set; }
        [JsonProperty("parent_region_id")] public string ParentRegionId { get; set; }
        [JsonProperty("stable_geography_id")]
        public string StableGeographyId { get; set; }
        [JsonProperty("fallback_display_name")]
        public string FallbackDisplayName { get; set; }
        [JsonProperty("geometry_status")] public string GeometryStatus { get; set; }
        [JsonProperty("source_geometry_status")]
        public string SourceGeometryStatus { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("provisional")] public bool Provisional { get; set; }
    }

    public sealed class HanAdministrativeNamePeriodRecord
    {
        [JsonProperty("stable_id")] public string StableId { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("valid_from_year")] public int ValidFromYear { get; set; }
        [JsonProperty("valid_to_year")] public int ValidToYear { get; set; }
    }
}
