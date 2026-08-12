using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class Luoyang184HistoricalPrototypeReader
    {
        private readonly Dictionary<ulong, Luoyang184CellRecord> _cellsById;
        private readonly Dictionary<string, Luoyang184FacilityRecord> _facilitiesById;

        public Luoyang184HistoricalPrototypeReader(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
                throw new ArgumentException("Historical Luoyang package root is required.", nameof(packageRoot));
            PackageRoot = Path.GetFullPath(packageRoot);
            World = JsonConvert.DeserializeObject<Luoyang184HistoricalPrototype>(
                File.ReadAllText(Path.Combine(PackageRoot, "luoyang_184_world.json")))
                ?? throw new InvalidDataException("Historical Luoyang prototype is empty.");
            if (World.Schema != "mandate.luoyang-184-historical-world.v1")
                throw new InvalidDataException("Unsupported historical Luoyang schema: " + World.Schema);
            _cellsById = new Dictionary<ulong, Luoyang184CellRecord>(World.Cells.Count);
            foreach (var cell in World.Cells)
                if (!_cellsById.TryAdd(cell.CellId64, cell))
                    throw new InvalidDataException("Duplicate historical Luoyang CellId64: " + cell.CellId64);
            _facilitiesById = new Dictionary<string, Luoyang184FacilityRecord>(StringComparer.Ordinal);
            foreach (var facility in World.Facilities)
                if (!_facilitiesById.TryAdd(facility.FacilityId, facility))
                    throw new InvalidDataException("Duplicate historical Luoyang Facility ID: " + facility.FacilityId);
        }

        public string PackageRoot { get; }
        public Luoyang184HistoricalPrototype World { get; }
        public bool TryGetCell(ulong cellId64, out Luoyang184CellRecord cell) => _cellsById.TryGetValue(cellId64, out cell);
        public bool TryGetFacility(string facilityId, out Luoyang184FacilityRecord facility)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
            {
                facility = null;
                return false;
            }
            return _facilitiesById.TryGetValue(facilityId, out facility);
        }
    }

    public sealed class Luoyang184HistoricalPrototype
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("scenario_year")] public int ScenarioYear { get; set; }
        [JsonProperty("scenario_polity_id")] public string ScenarioPolityId { get; set; }
        [JsonProperty("grid_schema_version")] public string GridSchemaVersion { get; set; }
        [JsonProperty("grid_version")] public string GridVersion { get; set; }
        [JsonProperty("cell_size_m")] public int CellSizeMetres { get; set; }
        [JsonProperty("columns")] public int Columns { get; set; }
        [JsonProperty("rows")] public int Rows { get; set; }
        [JsonProperty("city_id")] public string CityId { get; set; }
        [JsonProperty("city_anchor_cell_id64")] public ulong CityAnchorCellId64 { get; set; }
        [JsonProperty("hulao_cell_id64")] public ulong HulaoCellId64 { get; set; }
        [JsonProperty("population_profile")] public Luoyang184PopulationProfile PopulationProfile { get; set; }
        [JsonProperty("ai_pressure")] public Luoyang184AiPressureRecord AiPressure { get; set; }
        [JsonProperty("cells")] public List<Luoyang184CellRecord> Cells { get; set; } = new List<Luoyang184CellRecord>();
        [JsonProperty("facilities")] public List<Luoyang184FacilityRecord> Facilities { get; set; } = new List<Luoyang184FacilityRecord>();
        [JsonProperty("fortification_networks")] public List<Luoyang184FortificationNetworkRecord> FortificationNetworks { get; set; } = new List<Luoyang184FortificationNetworkRecord>();
        [JsonProperty("blueprints")] public List<Luoyang184BlueprintRecord> Blueprints { get; set; } = new List<Luoyang184BlueprintRecord>();
    }

    public sealed class Luoyang184PopulationProfile
    {
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("total_persons")] public int TotalPersons { get; set; }
        [JsonProperty("total_households")] public int TotalHouseholds { get; set; }
        [JsonProperty("effective_workers")] public int EffectiveWorkers { get; set; }
        [JsonProperty("employed_workers")] public int EmployedWorkers { get; set; }
        [JsonProperty("unemployed_workers")] public int UnemployedWorkers { get; set; }
        [JsonProperty("housed_persons")] public int HousedPersons { get; set; }
        [JsonProperty("unhoused_persons")] public int UnhousedPersons { get; set; }
        [JsonProperty("civilian_residential_capacity_persons")] public int CivilianResidentialCapacityPersons { get; set; }
        [JsonProperty("active_military_barracks_capacity_persons")] public int ActiveMilitaryBarracksCapacityPersons { get; set; }
    }

    public sealed class Luoyang184AiPressureRecord
    {
        [JsonProperty("unhoused_persons")] public int UnhousedPersons { get; set; }
        [JsonProperty("available_residential_person_slots")] public int AvailableResidentialPersonSlots { get; set; }
        [JsonProperty("unemployed_workers")] public int UnemployedWorkers { get; set; }
        [JsonProperty("vacant_job_slots")] public int VacantJobSlots { get; set; }
        [JsonProperty("skill_shortage_slots")] public int SkillShortageSlots { get; set; }
        [JsonProperty("food_days_basis_points")] public int FoodDaysBasisPoints { get; set; }
        [JsonProperty("security_basis_points")] public int SecurityBasisPoints { get; set; }
        [JsonProperty("recommended_action_ids")] public List<string> RecommendedActionIds { get; set; } = new List<string>();
    }

    public sealed class Luoyang184CellRecord
    {
        [JsonProperty("cell_id64")] public ulong CellId64 { get; set; }
        [JsonProperty("grid_x")] public int GridX { get; set; }
        [JsonProperty("grid_y")] public int GridY { get; set; }
        [JsonProperty("terrain_class")] public int TerrainClass { get; set; }
        [JsonProperty("slope_class")] public int SlopeClass { get; set; }
        [JsonProperty("water_class")] public int WaterClass { get; set; }
        [JsonProperty("elevation")] public int Elevation { get; set; }
        [JsonProperty("road_class")] public int RoadClass { get; set; }
        [JsonProperty("fertility")] public int Fertility { get; set; }
        [JsonProperty("developable")] public bool Developable { get; set; }
        [JsonProperty("owner_id")] public string OwnerId { get; set; }
        [JsonProperty("facility_id")] public string FacilityId { get; set; }
        [JsonProperty("facility_definition_id")] public string FacilityDefinitionId { get; set; }
        [JsonProperty("facility_name")] public string FacilityName { get; set; }
        [JsonProperty("facility_category_id")] public string FacilityCategoryId { get; set; }
        [JsonProperty("historical_confidence")] public string HistoricalConfidence { get; set; }
        [JsonProperty("population")] public int Population { get; set; }
        [JsonProperty("resident_capacity_persons")] public int ResidentCapacityPersons { get; set; }
        [JsonProperty("current_workers")] public int CurrentWorkers { get; set; }
        [JsonProperty("required_workers")] public int RequiredWorkers { get; set; }
        [JsonProperty("wall_state")] public string WallState { get; set; }
        [JsonProperty("gate_state")] public string GateState { get; set; }
        [JsonProperty("moat_state")] public string MoatState { get; set; }
    }

    public sealed class Luoyang184FacilityRecord
    {
        [JsonProperty("facility_id")] public string FacilityId { get; set; }
        [JsonProperty("definition_id")] public string DefinitionId { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("category_id")] public string CategoryId { get; set; }
        [JsonProperty("cell_id64")] public ulong CellId64 { get; set; }
        [JsonProperty("grid_x")] public int GridX { get; set; }
        [JsonProperty("grid_y")] public int GridY { get; set; }
        [JsonProperty("owner_id")] public string OwnerId { get; set; }
        [JsonProperty("controller_id")] public string ControllerId { get; set; }
        [JsonProperty("historical_confidence")] public string HistoricalConfidence { get; set; }
        [JsonProperty("spatial_precision")] public string SpatialPrecision { get; set; }
        [JsonProperty("source_ids")] public List<string> SourceIds { get; set; } = new List<string>();
        [JsonProperty("purpose_ids")] public List<string> PurposeIds { get; set; } = new List<string>();
        [JsonProperty("capability_ids")] public List<string> CapabilityIds { get; set; } = new List<string>();
        [JsonProperty("future_hook_ids")] public List<string> FutureHookIds { get; set; } = new List<string>();
        [JsonProperty("worker_capacity")] public int WorkerCapacity { get; set; }
        [JsonProperty("minimum_workers_for_normal_operation")] public int MinimumWorkersForNormalOperation { get; set; }
        [JsonProperty("worker_person_ids")] public List<string> WorkerPersonIds { get; set; } = new List<string>();
        [JsonProperty("residential_capacity_persons")] public int ResidentialCapacityPersons { get; set; }
        [JsonProperty("allowed_resident_type_ids")] public List<string> AllowedResidentTypeIds { get; set; } = new List<string>();
        [JsonProperty("resident_person_ids")] public List<string> ResidentPersonIds { get; set; } = new List<string>();
        [JsonProperty("job_definition_ids")] public List<string> JobDefinitionIds { get; set; } = new List<string>();
    }

    public sealed class Luoyang184FortificationNetworkRecord
    {
        [JsonProperty("network_id")] public string NetworkId { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("parent_network_id")] public string ParentNetworkId { get; set; }
        [JsonProperty("wall_facility_ids")] public List<string> WallFacilityIds { get; set; } = new List<string>();
        [JsonProperty("gate_facility_ids")] public List<string> GateFacilityIds { get; set; } = new List<string>();
        [JsonProperty("moat_feature_ids")] public List<string> MoatFeatureIds { get; set; } = new List<string>();
    }

    public sealed class Luoyang184BlueprintRecord
    {
        [JsonProperty("blueprint_id")] public string BlueprintId { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("orientation")] public string Orientation { get; set; }
        [JsonProperty("cell_count")] public int CellCount { get; set; }
        [JsonProperty("construction_stages")] public List<string> ConstructionStages { get; set; } = new List<string>();
        [JsonProperty("shared_placement_modes")] public List<string> SharedPlacementModes { get; set; } = new List<string>();
    }
}
