using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class LuoyangPopulationStressPrototypeReader
    {
        private readonly Dictionary<string, LuoyangStressProfileSummary> _profiles =
            new Dictionary<string, LuoyangStressProfileSummary>(StringComparer.Ordinal);

        public LuoyangPopulationStressPrototypeReader(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
                throw new ArgumentException("Luoyang stress package root is required.", nameof(packageRoot));
            PackageRoot = Path.GetFullPath(packageRoot);
            Manifest = JsonConvert.DeserializeObject<LuoyangStressManifest>(
                File.ReadAllText(Path.Combine(PackageRoot, "stress_manifest.json")))
                ?? throw new InvalidDataException("Luoyang stress manifest is empty.");
            if (Manifest.Schema != "mandate.luoyang-population-stress-manifest.v1")
                throw new InvalidDataException("Unsupported Luoyang stress schema: " + Manifest.Schema);
            if (Manifest.HistoricalScenarioPopulation != 20_542 || Manifest.CellSizeMetres != 2_000)
                throw new InvalidDataException("Luoyang stress package changed the protected historical baseline or Cell size.");

            foreach (var entry in Manifest.Profiles)
            {
                if (string.IsNullOrWhiteSpace(entry.ProfileId) || string.IsNullOrWhiteSpace(entry.SummaryRelativePath))
                    throw new InvalidDataException("Invalid Luoyang stress profile manifest entry.");
                var summary = JsonConvert.DeserializeObject<LuoyangStressProfileSummary>(
                    File.ReadAllText(Path.Combine(PackageRoot, entry.SummaryRelativePath)))
                    ?? throw new InvalidDataException("Luoyang stress profile summary is empty: " + entry.ProfileId);
                if (summary.ProfileId != entry.ProfileId || summary.PersonCount != entry.PersonCount)
                    throw new InvalidDataException("Luoyang stress profile summary does not match its manifest entry.");
                if (!_profiles.TryAdd(entry.ProfileId, summary))
                    throw new InvalidDataException("Duplicate Luoyang stress profile: " + entry.ProfileId);
            }
        }

        public string PackageRoot { get; }
        public LuoyangStressManifest Manifest { get; }
        public IReadOnlyCollection<LuoyangStressProfileSummary> Profiles => _profiles.Values;

        public bool TryGetProfile(string profileId, out LuoyangStressProfileSummary profile)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                profile = null;
                return false;
            }
            return _profiles.TryGetValue(profileId, out profile);
        }
    }

    public sealed class LuoyangStressManifest
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("grid_schema_version")] public string GridSchemaVersion { get; set; }
        [JsonProperty("grid_version")] public string GridVersion { get; set; }
        [JsonProperty("cell_size_m")] public int CellSizeMetres { get; set; }
        [JsonProperty("historical_scenario_population")] public int HistoricalScenarioPopulation { get; set; }
        [JsonProperty("historical_package_id")] public string HistoricalPackageId { get; set; }
        [JsonProperty("developable_cells")] public int DevelopableCells { get; set; }
        [JsonProperty("profiles")] public List<LuoyangStressManifestEntry> Profiles { get; set; } = new List<LuoyangStressManifestEntry>();
    }

    public sealed class LuoyangStressManifestEntry
    {
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("person_count")] public int PersonCount { get; set; }
        [JsonProperty("summary_relative_path")] public string SummaryRelativePath { get; set; }
        [JsonProperty("person_binary_relative_path")] public string PersonBinaryRelativePath { get; set; }
    }

    public sealed class LuoyangStressProfileSummary
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("profile_label")] public string ProfileLabel { get; set; }
        [JsonProperty("person_count")] public int PersonCount { get; set; }
        [JsonProperty("household_count")] public int HouseholdCount { get; set; }
        [JsonProperty("historical_scenario_population")] public int HistoricalScenarioPopulation { get; set; }
        [JsonProperty("is_stress_population")] public bool IsStressPopulation { get; set; }
        [JsonProperty("fixed_mode")] public LuoyangStressModeSummary FixedMode { get; set; }
        [JsonProperty("adaptive_mode")] public LuoyangStressModeSummary AdaptiveMode { get; set; }
        [JsonProperty("benchmarks")] public LuoyangStressBenchmarkSummary Benchmarks { get; set; }
        [JsonProperty("lod")] public LuoyangStressLodSummary Lod { get; set; }
        [JsonProperty("memory")] public LuoyangStressMemorySummary Memory { get; set; }
        [JsonProperty("save_load")] public LuoyangStressSaveLoadSummary SaveLoad { get; set; }
    }

    public sealed class LuoyangStressModeSummary
    {
        [JsonProperty("facility_count")] public int FacilityCount { get; set; }
        [JsonProperty("facilities_added")] public int FacilitiesAdded { get; set; }
        [JsonProperty("occupied_facility_cells")] public int OccupiedFacilityCells { get; set; }
        [JsonProperty("residential_cells")] public int ResidentialCells { get; set; }
        [JsonProperty("agriculture_cells")] public int AgricultureCells { get; set; }
        [JsonProperty("industrial_cells")] public int IndustrialCells { get; set; }
        [JsonProperty("commercial_cells")] public int CommercialCells { get; set; }
        [JsonProperty("warehouse_cells")] public int WarehouseCells { get; set; }
        [JsonProperty("military_cells")] public int MilitaryCells { get; set; }
        [JsonProperty("other_cells")] public int OtherCells { get; set; }
        [JsonProperty("cell_utilization_percent")] public double CellUtilizationPercent { get; set; }
        [JsonProperty("residential_capacity")] public int ResidentialCapacity { get; set; }
        [JsonProperty("housed_population")] public int HousedPopulation { get; set; }
        [JsonProperty("unhoused_population")] public int UnhousedPopulation { get; set; }
        [JsonProperty("total_jobs")] public int TotalJobs { get; set; }
        [JsonProperty("employed_workers")] public int EmployedWorkers { get; set; }
        [JsonProperty("unemployed_workers")] public int UnemployedWorkers { get; set; }
        [JsonProperty("open_jobs")] public int OpenJobs { get; set; }
        [JsonProperty("food_demand")] public long FoodDemand { get; set; }
        [JsonProperty("food_production")] public long FoodProduction { get; set; }
        [JsonProperty("food_deficit")] public long FoodDeficit { get; set; }
        [JsonProperty("storage_capacity")] public long StorageCapacity { get; set; }
        [JsonProperty("used_storage")] public long UsedStorage { get; set; }
        [JsonProperty("pressures")] public Dictionary<string, int> Pressures { get; set; } = new Dictionary<string, int>();
        [JsonProperty("added_by_category")] public Dictionary<string, int> AddedByCategory { get; set; } = new Dictionary<string, int>();
        [JsonProperty("construction_reason_counts")] public Dictionary<string, int> ConstructionReasonCounts { get; set; } = new Dictionary<string, int>();
        [JsonProperty("simulation_days")] public int SimulationDays { get; set; }
        [JsonProperty("simulation_status")] public string SimulationStatus { get; set; }
        [JsonProperty("ai_update_ms")] public double AiUpdateMilliseconds { get; set; }
        [JsonProperty("stability_findings")] public List<string> StabilityFindings { get; set; } = new List<string>();
    }

    public sealed class LuoyangStressBenchmarkSummary
    {
        [JsonProperty("job_match_10000_ms")] public double JobMatch10000Milliseconds { get; set; }
        [JsonProperty("job_candidates_scanned")] public long JobCandidatesScanned { get; set; }
        [JsonProperty("housing_10000_changes_ms")] public double Housing10000ChangesMilliseconds { get; set; }
        [JsonProperty("person_query_10000_ms")] public double PersonQuery10000Milliseconds { get; set; }
        [JsonProperty("daily_tick_ms")] public double DailyTickMilliseconds { get; set; }
        [JsonProperty("weekly_tick_ms")] public double WeeklyTickMilliseconds { get; set; }
        [JsonProperty("monthly_tick_ms")] public double MonthlyTickMilliseconds { get; set; }
    }

    public sealed class LuoyangStressLodSummary
    {
        [JsonProperty("permanent_person_count")] public int PermanentPersonCount { get; set; }
        [JsonProperty("low_frequency_person_count")] public int LowFrequencyPersonCount { get; set; }
        [JsonProperty("medium_frequency_person_count")] public int MediumFrequencyPersonCount { get; set; }
        [JsonProperty("high_frequency_actor_count")] public int HighFrequencyActorCount { get; set; }
        [JsonProperty("maximum_visual_actor_count")] public int MaximumVisualActorCount { get; set; }
    }

    public sealed class LuoyangStressMemorySummary
    {
        [JsonProperty("person_data_bytes")] public long PersonDataBytes { get; set; }
        [JsonProperty("person_index_bytes")] public long PersonIndexBytes { get; set; }
        [JsonProperty("facility_bytes")] public long FacilityBytes { get; set; }
        [JsonProperty("total_process_working_set_bytes")] public long TotalProcessWorkingSetBytes { get; set; }
        [JsonProperty("estimated_mb_per_10000_persons")] public double EstimatedMegabytesPer10000Persons { get; set; }
    }

    public sealed class LuoyangStressSaveLoadSummary
    {
        [JsonProperty("save_size_bytes")] public long SaveSizeBytes { get; set; }
        [JsonProperty("save_time_ms")] public double SaveTimeMilliseconds { get; set; }
        [JsonProperty("load_time_ms")] public double LoadTimeMilliseconds { get; set; }
        [JsonProperty("round_trip_consistent")] public bool RoundTripConsistent { get; set; }
    }

    public sealed class LuoyangStressPersonBinaryRecord
    {
        public string PersonId { get; internal set; }
        public string HouseholdId { get; internal set; }
        public ulong CurrentCellId64 { get; internal set; }
        public ulong OriginCellId64 { get; internal set; }
        public int Age { get; internal set; }
        public int HealthBasisPoints { get; internal set; }
        public byte SexCode { get; internal set; }
        public byte ActivityCode { get; internal set; }
        public int ResidenceFacilityOrdinal { get; internal set; }
        public int WorkFacilityOrdinal { get; internal set; }
        public byte ProfessionCode { get; internal set; }
        public int SkillBasisPoints { get; internal set; }
        public bool IsActiveMilitary { get; internal set; }
        public bool IsLaborEligible { get; internal set; }
        public int AdministrativeRelationCode { get; internal set; }
        public int DailyConsumptionBasisPoints { get; internal set; }
        public long NextScheduledUpdateDay { get; internal set; }
        public StressSimulationTier SimulationTier { get; internal set; }
    }

    public sealed class LuoyangStressPersonBinaryReader : IDisposable
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LYPSTR01");
        public const int CurrentVersion = 1;
        public const int RecordSize = 72;
        public const int HeaderSize = 32;

        private readonly FileStream _stream;
        private readonly BinaryReader _reader;
        private readonly string _profileKey;

        public LuoyangStressPersonBinaryReader(string path, string profileKey)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Person binary path is required.", nameof(path));
            if (string.IsNullOrWhiteSpace(profileKey)) throw new ArgumentException("Profile key is required.", nameof(profileKey));
            _profileKey = profileKey;
            _stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
            _reader = new BinaryReader(_stream, Encoding.UTF8, true);
            var magic = _reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length || !Equal(magic, Magic)) throw new InvalidDataException("Invalid Luoyang stress person binary magic.");
            Version = _reader.ReadInt32();
            var recordSize = _reader.ReadInt32();
            PersonCount = _reader.ReadInt32();
            HistoricalPersonCount = _reader.ReadInt32();
            Seed = _reader.ReadInt64();
            if (Version != CurrentVersion || recordSize != RecordSize || PersonCount <= 0 ||
                HistoricalPersonCount != 20_542 || HistoricalPersonCount > PersonCount ||
                _stream.Length != HeaderSize + (long)PersonCount * RecordSize)
                throw new InvalidDataException("Invalid Luoyang stress person binary header or length.");
        }

        public int Version { get; }
        public int PersonCount { get; }
        public int HistoricalPersonCount { get; }
        public long Seed { get; }

        public LuoyangStressPersonBinaryRecord Read(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= PersonCount) throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            _stream.Position = HeaderSize + (long)zeroBasedIndex * RecordSize;
            var sequence = _reader.ReadUInt64();
            var household = _reader.ReadUInt64();
            var record = new LuoyangStressPersonBinaryRecord
            {
                PersonId = sequence <= (ulong)HistoricalPersonCount
                    ? $"person.luoyang.v1.recommended.{sequence:00000000}"
                    : $"person.luoyang.stress.v1.{_profileKey}.{sequence:00000000}",
                HouseholdId = household <= 4_498
                    ? $"household.luoyang.v1.recommended.{household:0000000}"
                    : $"household.luoyang.stress.v1.{_profileKey}.{household:0000000}",
                CurrentCellId64 = _reader.ReadUInt64(),
                OriginCellId64 = _reader.ReadUInt64(),
                Age = _reader.ReadInt32(),
                HealthBasisPoints = _reader.ReadUInt16(),
                SexCode = _reader.ReadByte(),
                ActivityCode = _reader.ReadByte(),
                ResidenceFacilityOrdinal = _reader.ReadInt32(),
                WorkFacilityOrdinal = _reader.ReadInt32(),
                ProfessionCode = _reader.ReadByte(),
                SkillBasisPoints = _reader.ReadUInt16(),
                IsActiveMilitary = _reader.ReadByte() != 0,
                IsLaborEligible = _reader.ReadByte() != 0,
                AdministrativeRelationCode = _reader.ReadInt32(),
                DailyConsumptionBasisPoints = _reader.ReadInt32(),
                NextScheduledUpdateDay = _reader.ReadInt64(),
                SimulationTier = (StressSimulationTier)_reader.ReadByte()
            };
            _reader.ReadBytes(2);
            if (sequence != (ulong)zeroBasedIndex + 1 ||
                household == 0 ||
                record.Age < 0 || record.Age > 130 ||
                record.HealthBasisPoints > 10_000 ||
                !Enum.IsDefined(typeof(StressSimulationTier), record.SimulationTier))
                throw new InvalidDataException("Invalid Luoyang stress permanent Person record.");
            return record;
        }

        public void Dispose()
        {
            _reader.Dispose();
            _stream.Dispose();
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }
}
