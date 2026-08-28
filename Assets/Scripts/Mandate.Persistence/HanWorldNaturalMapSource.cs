using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanWorldNaturalMapSource : IGlobalNaturalCellSource, IDisposable
    {
        private readonly WorldMapDataReader _reader;

        public HanWorldNaturalMapSource(string worldPackageRoot, string naturalPackageRoot = null)
        {
            _reader = new WorldMapDataReader(worldPackageRoot);
            NaturalPackageRoot = naturalPackageRoot ?? Path.Combine(
                Directory.GetParent(worldPackageRoot)?.FullName ?? worldPackageRoot,
                "NaturalBasemapV1");
            var configPath = Path.Combine(NaturalPackageRoot, "natural_basemap_config.json");
            Config = File.Exists(configPath)
                ? JsonConvert.DeserializeObject<HanWorldNaturalBasemapConfig>(File.ReadAllText(configPath))
                : HanWorldNaturalBasemapConfig.CreateDefault();
            var riversPath = Path.Combine(NaturalPackageRoot, "global_rivers_projected.json");
            Rivers = File.Exists(riversPath)
                ? JsonConvert.DeserializeObject<GlobalRiverPresentationCatalog>(File.ReadAllText(riversPath))
                : new GlobalRiverPresentationCatalog();
        }

        public string NaturalPackageRoot { get; }
        public HanWorldNaturalBasemapConfig Config { get; }
        public GlobalRiverPresentationCatalog Rivers { get; }
        public int Rows => _reader.Manifest.Rows;
        public int Columns => _reader.Manifest.Columns;
        public double OriginX => _reader.Manifest.OriginX;
        public double OriginY => _reader.Manifest.OriginY;
        public int CellSizeMetres => _reader.Manifest.CellSizeMetres;

        public NaturalMapCellSample ReadSample(int row, int column)
        {
            var cell = _reader.ReadCell(row, column);
            long sum = 0;
            var count = 0;
            for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                var neighbourRow = row + rowOffset;
                if (neighbourRow < 0 || neighbourRow >= Rows) continue;
                for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    var neighbourColumn = column + columnOffset;
                    if (neighbourColumn < 0 || neighbourColumn >= Columns) continue;
                    var elevation = _reader.ReadCell(neighbourRow, neighbourColumn).Elevation;
                    if (elevation <= -32000) continue;
                    sum += elevation;
                    count++;
                }
            }
            return new NaturalMapCellSample(cell, count == 0 ? cell.Elevation : sum / (double)count);
        }

        public void Dispose() => _reader.Dispose();
    }

    public sealed class HanWorldNaturalBasemapConfig
    {
        [JsonProperty("schema")] public string Schema;
        [JsonProperty("status")] public string Status;
        [JsonProperty("terrain_tile_cells_per_side")] public int TerrainTileCellsPerSide;
        [JsonProperty("terrain_tile_size_metres")] public int TerrainTileSizeMetres;
        [JsonProperty("streaming_unit_cells_per_side_provisional")] public int StreamingUnitCellsPerSide;
        [JsonProperty("world_lod_sample_step_cells")] public int WorldLodSampleStepCells;
        [JsonProperty("region_resident_tile_radius")] public int RegionResidentTileRadius;
        [JsonProperty("elevation_exaggeration")] public double ElevationExaggeration;
        [JsonProperty("world_vertical_exaggeration")] public double WorldVerticalExaggeration;
        [JsonProperty("region_vertical_exaggeration")] public double RegionVerticalExaggeration;
        [JsonProperty("region_far_span_cells")] public int RegionFarSpanCells;
        [JsonProperty("region_far_sample_step_cells")] public int RegionFarSampleStepCells;
        [JsonProperty("river_smoothing_iterations")] public int RiverSmoothingIterations;
        [JsonProperty("forest_lattice_per_cell")] public int ForestLatticePerCell;
        [JsonProperty("source_dem_relative_path")] public string SourceDemRelativePath;
        [JsonProperty("background_policy")] public string BackgroundPolicy;

        public static HanWorldNaturalBasemapConfig CreateDefault() => new HanWorldNaturalBasemapConfig
        {
            Schema = "hanworld.natural-basemap-config.v2",
            Status = "HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2",
            TerrainTileCellsPerSide = TerrainTileIndex.FrozenCellsPerSideV1,
            TerrainTileSizeMetres = 16000,
            StreamingUnitCellsPerSide = 24,
            WorldLodSampleStepCells = 8,
            RegionResidentTileRadius = 1,
            ElevationExaggeration = 1.35d,
            WorldVerticalExaggeration = 2.10d,
            RegionVerticalExaggeration = 1.48d,
            RegionFarSpanCells = 112,
            RegionFarSampleStepCells = 2,
            RiverSmoothingIterations = 2,
            ForestLatticePerCell = 1,
            SourceDemRelativePath = "MapData/HanWorld_Master_V0/physical/elevation_master.tif",
            BackgroundPolicy = "NO_LEGACY_BACKGROUND_REQUIRED"
        };
    }

    public sealed class GlobalRiverPresentationCatalog
    {
        [JsonProperty("schema")] public string Schema = "hanworld.global-rivers-projected.v1";
        [JsonProperty("crs_id")] public string CrsId = GlobalSpatialFoundationV1.CrsId;
        [JsonProperty("features")] public List<GlobalRiverPresentationFeature> Features =
            new List<GlobalRiverPresentationFeature>();
        [JsonProperty("source_gaps")] public List<GlobalRiverSourceGap> SourceGaps =
            new List<GlobalRiverSourceGap>();
    }

    public sealed class GlobalRiverPresentationFeature
    {
        [JsonProperty("river_id")] public string RiverId;
        [JsonProperty("name")] public string Name;
        [JsonProperty("name_zh")] public string NameZh;
        [JsonProperty("display_tier")] public string DisplayTier;
        [JsonProperty("width_metres")] public double WidthMetres;
        [JsonProperty("source_id")] public string SourceId;
        [JsonProperty("historical_claim")] public bool HistoricalClaim;
        [JsonProperty("geometry_status")] public string GeometryStatus;
        [JsonProperty("segments")] public List<List<ProjectedPoint>> Segments =
            new List<List<ProjectedPoint>>();
    }

    public sealed class GlobalRiverSourceGap
    {
        [JsonProperty("river_id")] public string RiverId;
        [JsonProperty("name_zh")] public string NameZh;
        [JsonProperty("status")] public string Status;
        [JsonProperty("reason")] public string Reason;
    }

    public readonly struct ProjectedPoint
    {
        [JsonConstructor]
        public ProjectedPoint([JsonProperty("x")] double x, [JsonProperty("y")] double y)
        {
            X = x;
            Y = y;
        }
        [JsonProperty("x")] public double X { get; }
        [JsonProperty("y")] public double Y { get; }
    }
}
