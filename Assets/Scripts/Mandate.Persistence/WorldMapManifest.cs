using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class WorldMapManifest
    {
        [JsonProperty("schema")]
        public string Schema { get; set; }

        [JsonProperty("grid_version")]
        public string GridVersion { get; set; }

        [JsonProperty("grid_schema_version")]
        public string GridSchemaVersion { get; set; }

        [JsonProperty("columns")]
        public int Columns { get; set; }

        [JsonProperty("rows")]
        public int Rows { get; set; }

        [JsonProperty("total_cells")]
        public long TotalCells { get; set; }

        [JsonProperty("cell_size_m")]
        public int CellSizeMetres { get; set; }

        [JsonProperty("chunk_size")]
        public int ChunkSize { get; set; }

        [JsonProperty("crs_id")]
        public string CrsId { get; set; }

        [JsonProperty("origin_x")]
        public double OriginX { get; set; }

        [JsonProperty("origin_y")]
        public double OriginY { get; set; }

        [JsonProperty("cell_id_algorithm")]
        public string CellIdAlgorithm { get; set; }

        [JsonProperty("binary_files")]
        public List<WorldMapBinaryFileManifest> BinaryFiles { get; set; }
    }

    public sealed class WorldMapBinaryFileManifest
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("bytes")]
        public long Bytes { get; set; }

        [JsonProperty("chunk_count")]
        public int ChunkCount { get; set; }

        [JsonProperty("channels")]
        public int Channels { get; set; }

        [JsonProperty("value_size")]
        public int ValueSize { get; set; }
    }

    public sealed class WorldMapAdminCatalog
    {
        [JsonProperty("provinces")]
        public List<string> Provinces { get; set; }

        [JsonProperty("commanderies")]
        public List<string> Commanderies { get; set; }

        [JsonProperty("counties")]
        public List<string> Counties { get; set; }

        [JsonProperty("none_code")]
        public ushort NoneCode { get; set; }
    }

    public sealed class WorldMapLocationFeatureCollection
    {
        [JsonProperty("features")]
        public List<WorldMapLocationFeature> Features { get; set; }
    }

    public sealed class WorldMapLocationFeature
    {
        [JsonProperty("properties")]
        public WorldMapLocationProperties Properties { get; set; }
    }

    public sealed class WorldMapLocationProperties
    {
        [JsonProperty("city_id")]
        public string CityId { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("admin_reference")]
        public string AdministrativeRegionId { get; set; }

        [JsonProperty("stable_region_id")]
        public string StableRegionId { get; set; }

        [JsonProperty("cell_id")]
        public long? CellId { get; set; }

        [JsonProperty("row")]
        public int? Row { get; set; }

        [JsonProperty("column")]
        public int? Column { get; set; }
    }
}
