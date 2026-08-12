using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangWorldPrototypeReader
    {
        private readonly Dictionary<ulong, LuoyangCellRecord> _cellsById;
        private readonly Dictionary<string, LuoyangFacilityRecord> _facilitiesById;

        public LuoyangWorldPrototypeReader(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
                throw new ArgumentException("Luoyang prototype package root is required.", nameof(packageRoot));
            PackageRoot = Path.GetFullPath(packageRoot);
            World = JsonConvert.DeserializeObject<LuoyangWorldPrototype>(
                File.ReadAllText(Path.Combine(PackageRoot, "luoyang_world.json")))
                ?? throw new InvalidDataException("Luoyang prototype is empty.");
            if (World.Schema != "mandate.luoyang-world-prototype.v1")
                throw new InvalidDataException($"Unsupported Luoyang prototype schema: {World.Schema}");
            _cellsById = new Dictionary<ulong, LuoyangCellRecord>(World.Cells.Count);
            foreach (var cell in World.Cells)
            {
                if (!_cellsById.TryAdd(cell.CellId64, cell))
                    throw new InvalidDataException($"Duplicate Luoyang CellId64: {cell.CellId64}");
            }
            _facilitiesById = new Dictionary<string, LuoyangFacilityRecord>(StringComparer.Ordinal);
            foreach (var facility in World.Facilities)
            {
                if (!_facilitiesById.TryAdd(facility.FacilityId, facility))
                    throw new InvalidDataException($"Duplicate Luoyang Facility ID: {facility.FacilityId}");
            }
        }

        public string PackageRoot { get; }
        public LuoyangWorldPrototype World { get; }
        public bool TryGetCell(ulong cellId64, out LuoyangCellRecord cell) => _cellsById.TryGetValue(cellId64, out cell);
        public bool TryGetFacility(string facilityId, out LuoyangFacilityRecord facility)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
            {
                facility = null;
                return false;
            }
            return _facilitiesById.TryGetValue(facilityId, out facility);
        }
    }

    public sealed class LuoyangWorldPrototype
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("grid_schema_version")] public string GridSchemaVersion { get; set; }
        [JsonProperty("grid_version")] public string GridVersion { get; set; }
        [JsonProperty("cell_size_m")] public int CellSizeMetres { get; set; }
        [JsonProperty("columns")] public int Columns { get; set; }
        [JsonProperty("rows")] public int Rows { get; set; }
        [JsonProperty("region_bounds")] public LuoyangRegionBounds RegionBounds { get; set; }
        [JsonProperty("city_id")] public string CityId { get; set; }
        [JsonProperty("city_anchor_cell_id64")] public ulong CityAnchorCellId64 { get; set; }
        [JsonProperty("city_footprint_cell_ids")] public List<ulong> CityFootprintCellIds { get; set; } = new List<ulong>();
        [JsonProperty("hulao_cell_id64")] public ulong HulaoCellId64 { get; set; }
        [JsonProperty("population_profile")] public LuoyangPopulationProfile PopulationProfile { get; set; }
        [JsonProperty("cells")] public List<LuoyangCellRecord> Cells { get; set; } = new List<LuoyangCellRecord>();
        [JsonProperty("facilities")] public List<LuoyangFacilityRecord> Facilities { get; set; } = new List<LuoyangFacilityRecord>();
        [JsonProperty("forces")] public List<LuoyangForceRecord> Forces { get; set; } = new List<LuoyangForceRecord>();
    }

    public sealed class LuoyangRegionBounds
    {
        [JsonProperty("min_row")] public int MinRow { get; set; }
        [JsonProperty("max_row")] public int MaxRow { get; set; }
        [JsonProperty("min_column")] public int MinColumn { get; set; }
        [JsonProperty("max_column")] public int MaxColumn { get; set; }
    }

    public sealed class LuoyangPopulationProfile
    {
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("total_persons")] public int TotalPersons { get; set; }
        [JsonProperty("total_households")] public int TotalHouseholds { get; set; }
        [JsonProperty("effective_workers")] public int EffectiveWorkers { get; set; }
        [JsonProperty("employed_workers")] public int EmployedWorkers { get; set; }
        [JsonProperty("unemployed_workers")] public int UnemployedWorkers { get; set; }
        [JsonProperty("residential_capacity")] public int ResidentialCapacity { get; set; }
        [JsonProperty("developable_cells")] public int DevelopableCells { get; set; }
        [JsonProperty("developed_cells")] public int DevelopedCells { get; set; }
        [JsonProperty("unused_developable_cells")] public int UnusedDevelopableCells { get; set; }
    }

    public sealed class LuoyangCellRecord
    {
        [JsonProperty("cell_id64")] public ulong CellId64 { get; set; }
        [JsonProperty("grid_schema_version")] public string GridSchemaVersion { get; set; }
        [JsonProperty("grid_x")] public int GridX { get; set; }
        [JsonProperty("grid_y")] public int GridY { get; set; }
        [JsonProperty("terrain_class")] public int TerrainClass { get; set; }
        [JsonProperty("slope_class")] public int SlopeClass { get; set; }
        [JsonProperty("water_class")] public int WaterClass { get; set; }
        [JsonProperty("elevation")] public int Elevation { get; set; }
        [JsonProperty("road_class")] public int RoadClass { get; set; }
        [JsonProperty("developable")] public bool Developable { get; set; }
        [JsonProperty("fertility")] public int Fertility { get; set; }
        [JsonProperty("resource_ids")] public List<string> ResourceIds { get; set; } = new List<string>();
        [JsonProperty("province_id")] public string ProvinceId { get; set; }
        [JsonProperty("commandery_id")] public string CommanderyId { get; set; }
        [JsonProperty("county_id")] public string CountyId { get; set; }
        [JsonProperty("owner_id")] public string OwnerId { get; set; }
        [JsonProperty("facility_id")] public string FacilityId { get; set; }
        [JsonProperty("facility_type")] public string FacilityType { get; set; }
        [JsonProperty("worker_capacity")] public int WorkerCapacity { get; set; }
        [JsonProperty("current_workers")] public int CurrentWorkers { get; set; }
        [JsonProperty("residential_capacity")] public int ResidentialCapacity { get; set; }
        [JsonProperty("population")] public int Population { get; set; }
        [JsonProperty("households")] public int Households { get; set; }
        [JsonProperty("workers")] public int Workers { get; set; }
        [JsonProperty("employment")] public int Employment { get; set; }
        [JsonProperty("unemployment")] public int Unemployment { get; set; }
        [JsonProperty("facility_worker_demand")] public int FacilityWorkerDemand { get; set; }
        [JsonProperty("force_id")] public string ForceId { get; set; }
    }

    public sealed class LuoyangFacilityRecord
    {
        [JsonProperty("facility_id")] public string FacilityId { get; set; }
        [JsonProperty("definition_id")] public string DefinitionId { get; set; }
        [JsonProperty("category")] public string Category { get; set; }
        [JsonProperty("cell_id64")] public ulong CellId64 { get; set; }
        [JsonProperty("owner_id")] public string OwnerId { get; set; }
        [JsonProperty("manager_person_id")] public string ManagerPersonId { get; set; }
        [JsonProperty("delegation_mode")] public string DelegationMode { get; set; }
        [JsonProperty("worker_capacity")] public int WorkerCapacity { get; set; }
        [JsonProperty("recommended_workers")] public int RecommendedWorkers { get; set; }
        [JsonProperty("normal_workers")] public int NormalWorkers { get; set; }
        [JsonProperty("peak_workers")] public int PeakWorkers { get; set; }
        [JsonProperty("current_required_workers")] public int CurrentRequiredWorkers { get; set; }
        [JsonProperty("residential_capacity_persons")] public int ResidentialCapacityPersons { get; set; }
        [JsonProperty("residential_capacity_households")] public int ResidentialCapacityHouseholds { get; set; }
        [JsonProperty("current_crop_id")] public string CurrentCropId { get; set; }
        [JsonProperty("growth_stage")] public string GrowthStage { get; set; }
        [JsonProperty("maturity_percent")] public int? MaturityPercent { get; set; }
    }

    public sealed class LuoyangForceRecord
    {
        [JsonProperty("force_id")] public string ForceId { get; set; }
        [JsonProperty("cell_id64")] public ulong CellId64 { get; set; }
    }
}
