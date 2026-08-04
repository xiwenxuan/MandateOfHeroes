using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal static class PopulationFiftyYearWorldProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                WorldOptions options = WorldOptions.Parse(args);
                if (options.SelfTest)
                {
                    SelfTests.Run(options);
                    return 0;
                }

                var stopwatch = Stopwatch.StartNew();
                WorldEvidence evidence = DemographicWorldRunner.Run(options);
                evidence.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                JsonFile.Write(options.OutputPath, evidence);
                Console.WriteLine(
                    "RESULT {0}=passed initial={1} living={2} cumulative={3} births={4} deaths={5} years={6} elapsed_ms={7}",
                    options.HasFoodEcology
                        ? "m24-p7"
                        : options.HasFoodProductProvenance
                        ? "m24-p6"
                        : options.HasFormalInventoryBridge
                        ? "m24-p5"
                        : options.HasPopulationResourceCalibration
                        ? "m24-p4"
                        : options.HasHouseholdProduction
                        ? "m24-p3"
                        : options.HasHouseholdMarketRelief
                        ? "m24-p2"
                        : options.HasSubsistencePressure ? "m24-p1" : "m24-p0",
                    evidence.InitialLivingPopulation,
                    evidence.FinalLivingPopulation,
                    evidence.CumulativePersonCount,
                    evidence.TotalBirths,
                    evidence.TotalDeaths,
                    evidence.YearsSimulated,
                    evidence.ElapsedMilliseconds);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }

    internal sealed partial class WorldOptions
    {
        public bool SelfTest { get; private set; }
        public string OutputPath { get; private set; }
        public string ProgressPath { get; private set; }
        public string WorkspacePath { get; private set; }
        public string ProfilePath { get; private set; }
        public string M12InputPath { get; private set; }
        public string AuditPath { get; private set; }
        public string AdministrativeUnitsPath { get; private set; }
        public string SubsistencePressureProfilePath { get; private set; }
        public string HouseholdMarketReliefProfilePath { get; private set; }
        public string HouseholdProductionProfilePath { get; private set; }
        public string PopulationResourceCalibrationProfilePath { get; private set; }
        public string ProductionContentPath { get; private set; }
        public int InitialLivingPopulation { get; private set; }
        public int Years { get; private set; }
        public ulong Seed { get; private set; }
        public bool HasSubsistencePressure
        {
            get { return !string.IsNullOrWhiteSpace(SubsistencePressureProfilePath); }
        }
        public bool HasHouseholdMarketRelief
        {
            get { return !string.IsNullOrWhiteSpace(HouseholdMarketReliefProfilePath); }
        }
        public bool HasHouseholdProduction
        {
            get { return !string.IsNullOrWhiteSpace(HouseholdProductionProfilePath); }
        }
        public bool HasPopulationResourceCalibration
        {
            get { return !string.IsNullOrWhiteSpace(PopulationResourceCalibrationProfilePath); }
        }

        public static WorldOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var selfTest = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--self-test")
                {
                    selfTest = true;
                    continue;
                }

                if (!args[i].StartsWith("--", StringComparison.Ordinal) ||
                    i + 1 >= args.Length)
                {
                    throw new ArgumentException("Invalid command line argument: " + args[i]);
                }
                values.Add(args[i], args[++i]);
            }

            var result = new WorldOptions
            {
                SelfTest = selfTest,
                OutputPath = Required(values, "--output"),
                ProgressPath = Optional(values, "--progress"),
                WorkspacePath = Optional(values, "--workspace"),
                ProfilePath = Required(values, "--profile"),
                M12InputPath = Required(values, "--m12-input"),
                AuditPath = Required(values, "--audit"),
                AdministrativeUnitsPath = Required(values, "--administrative-units"),
                SubsistencePressureProfilePath = Optional(values, "--subsistence-pressure-profile"),
                HouseholdMarketReliefProfilePath = Optional(values, "--household-market-relief-profile"),
                HouseholdProductionProfilePath = Optional(values, "--household-production-profile"),
                PopulationResourceCalibrationProfilePath = Optional(
                    values, "--population-resource-calibration-profile"),
                FormalInventoryBridgeProfilePath = Optional(
                    values, "--formal-inventory-bridge-profile"),
                FoodProductProvenanceProfilePath = Optional(
                    values, "--food-product-provenance-profile"),
                FoodEcologyProfilePath = Optional(
                    values, "--food-ecology-profile"),
                FoodContentExtensionPath = Optional(
                    values, "--food-content-extension"),
                ProductionContentPath = Optional(values, "--production-content"),
                InitialLivingPopulation = ParseInt(values, "--initial-living", 1_000_000),
                Years = ParseInt(values, "--years", 50),
                Seed = ParseULong(values, "--seed", 14_000_024UL)
            };
            if (!selfTest && string.IsNullOrWhiteSpace(result.WorkspacePath))
            {
                throw new ArgumentException("--workspace is required.");
            }
            if (result.InitialLivingPopulation < 1_182 ||
                result.InitialLivingPopulation > 50_000_000)
            {
                throw new ArgumentOutOfRangeException("--initial-living");
            }
            if (result.Years < 1 || result.Years > 200)
            {
                throw new ArgumentOutOfRangeException("--years");
            }
            if (result.HasHouseholdMarketRelief && !result.HasSubsistencePressure)
            {
                throw new ArgumentException(
                    "--household-market-relief-profile requires --subsistence-pressure-profile.");
            }
            if (result.HasHouseholdProduction &&
                (!result.HasHouseholdMarketRelief ||
                 string.IsNullOrWhiteSpace(result.ProductionContentPath)))
            {
                throw new ArgumentException(
                    "--household-production-profile requires market relief and production content.");
            }
            if (result.HasPopulationResourceCalibration && !result.HasHouseholdProduction)
            {
                throw new ArgumentException(
                    "--population-resource-calibration-profile requires household production.");
            }
            if (result.HasFormalInventoryBridge &&
                !result.HasPopulationResourceCalibration)
            {
                throw new ArgumentException(
                    "--formal-inventory-bridge-profile requires population resource calibration.");
            }
            if (result.HasFoodProductProvenance &&
                !result.HasFormalInventoryBridge)
            {
                throw new ArgumentException(
                    "--food-product-provenance-profile requires the formal inventory bridge.");
            }
            if (result.HasFoodEcology &&
                (!result.HasFoodProductProvenance ||
                 string.IsNullOrWhiteSpace(result.FoodContentExtensionPath)))
            {
                throw new ArgumentException(
                    "--food-ecology-profile requires provenance and a food content extension.");
            }
            return result;
        }

        private static string Required(Dictionary<string, string> values, string key)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(key + " is required.");
            }
            return Path.GetFullPath(value);
        }

        private static string Optional(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)
                ? Path.GetFullPath(value)
                : string.Empty;
        }

        private static int ParseInt(
            Dictionary<string, string> values,
            string key,
            int fallback)
        {
            string value;
            return values.TryGetValue(key, out value)
                ? int.Parse(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        private static ulong ParseULong(
            Dictionary<string, string> values,
            string key,
            ulong fallback)
        {
            string value;
            return values.TryGetValue(key, out value)
                ? ulong.Parse(value, CultureInfo.InvariantCulture)
                : fallback;
        }
    }

    internal sealed class DemographyProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("days_per_year")] public int DaysPerYear { get; set; }
        [JsonProperty("female_basis_points")] public int FemaleBasisPoints { get; set; }
        [JsonProperty("minimum_marriage_age_female")] public int MinimumMarriageAgeFemale { get; set; }
        [JsonProperty("maximum_marriage_age_female")] public int MaximumMarriageAgeFemale { get; set; }
        [JsonProperty("minimum_marriage_age_male")] public int MinimumMarriageAgeMale { get; set; }
        [JsonProperty("maximum_marriage_age_male")] public int MaximumMarriageAgeMale { get; set; }
        [JsonProperty("minimum_childbirth_spacing_days")] public int MinimumChildbirthSpacingDays { get; set; }
        [JsonProperty("remarriage_delay_days")] public int RemarriageDelayDays { get; set; }
        [JsonProperty("maximum_age_years")] public int MaximumAgeYears { get; set; }
        [JsonProperty("initial_age_bands")] public List<AgeWeightBand> InitialAgeBands { get; set; }
        [JsonProperty("fertility_bands")] public List<ProbabilityBand> FertilityBands { get; set; }
        [JsonProperty("mortality_bands")] public List<ProbabilityBand> MortalityBands { get; set; }

        public static DemographyProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<DemographyProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.demography-profile.v1" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                value.SourceLayer != "gameplay_completion" ||
                value.DaysPerYear < 300 || value.DaysPerYear > 400 ||
                value.FemaleBasisPoints <= 0 || value.FemaleBasisPoints >= 10_000 ||
                value.MinimumMarriageAgeFemale < 15 ||
                value.MaximumMarriageAgeFemale < value.MinimumMarriageAgeFemale ||
                value.MinimumMarriageAgeMale < 15 ||
                value.MaximumMarriageAgeMale < value.MinimumMarriageAgeMale ||
                value.MinimumChildbirthSpacingDays <= 0 ||
                value.RemarriageDelayDays < 0 ||
                value.MaximumAgeYears < 80 || value.MaximumAgeYears > 130 ||
                value.InitialAgeBands == null || value.FertilityBands == null ||
                value.MortalityBands == null ||
                value.InitialAgeBands.Sum(item => item.WeightBasisPoints) != 10_000)
            {
                throw new InvalidDataException("The demography profile is invalid.");
            }
            ValidateBands(value.InitialAgeBands.Select(item =>
                new ProbabilityBand
                {
                    MinimumAge = item.MinimumAge,
                    MaximumAge = item.MaximumAge,
                    AnnualProbabilityBasisPoints = item.WeightBasisPoints
                }).ToList());
            ValidateBands(value.FertilityBands);
            ValidateBands(value.MortalityBands);
            return value;
        }

        private static void ValidateBands(List<ProbabilityBand> bands)
        {
            for (var i = 0; i < bands.Count; i++)
            {
                if (bands[i].MinimumAge < 0 ||
                    bands[i].MaximumAge < bands[i].MinimumAge ||
                    bands[i].AnnualProbabilityBasisPoints < 0 ||
                    bands[i].AnnualProbabilityBasisPoints > 10_000)
                {
                    throw new InvalidDataException("A demography age band is invalid.");
                }
            }
        }
    }

    internal sealed class AgeWeightBand
    {
        [JsonProperty("minimum_age")] public int MinimumAge { get; set; }
        [JsonProperty("maximum_age")] public int MaximumAge { get; set; }
        [JsonProperty("weight_basis_points")] public int WeightBasisPoints { get; set; }
    }

    internal sealed class ProbabilityBand
    {
        [JsonProperty("minimum_age")] public int MinimumAge { get; set; }
        [JsonProperty("maximum_age")] public int MaximumAge { get; set; }
        [JsonProperty("annual_probability_basis_points")] public int AnnualProbabilityBasisPoints { get; set; }
    }

    internal sealed class ConsumptionBand
    {
        [JsonProperty("minimum_age")] public int MinimumAge { get; set; }
        [JsonProperty("maximum_age")] public int MaximumAge { get; set; }
        [JsonProperty("adult_ration_basis_points")] public int AdultRationBasisPoints { get; set; }
    }

    internal sealed class SubsistencePressureProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("food_product_id")] public string FoodProductId { get; set; }
        [JsonProperty("food_unit_id")] public string FoodUnitId { get; set; }
        [JsonProperty("land_unit_id")] public string LandUnitId { get; set; }
        [JsonProperty("opening_food_reserve_adult_days")] public int OpeningFoodReserveAdultDays { get; set; }
        [JsonProperty("arable_milli_mu_per_opening_person")] public int ArableMilliMuPerOpeningPerson { get; set; }
        [JsonProperty("agricultural_worker_basis_points")] public int AgriculturalWorkerBasisPoints { get; set; }
        [JsonProperty("minimum_worker_age")] public int MinimumWorkerAge { get; set; }
        [JsonProperty("maximum_worker_age")] public int MaximumWorkerAge { get; set; }
        [JsonProperty("labor_capacity_milli_mu_per_worker")] public int LaborCapacityMilliMuPerWorker { get; set; }
        [JsonProperty("gross_yield_milli_rations_per_mu")] public int GrossYieldMilliRationsPerMu { get; set; }
        [JsonProperty("field_seed_loss_basis_points")] public int FieldSeedLossBasisPoints { get; set; }
        [JsonProperty("annual_storage_spoilage_basis_points")] public int AnnualStorageSpoilageBasisPoints { get; set; }
        [JsonProperty("ordinary_weather_min_basis_points")] public int OrdinaryWeatherMinBasisPoints { get; set; }
        [JsonProperty("ordinary_weather_max_basis_points")] public int OrdinaryWeatherMaxBasisPoints { get; set; }
        [JsonProperty("severe_harvest_event_basis_points")] public int SevereHarvestEventBasisPoints { get; set; }
        [JsonProperty("severe_harvest_min_basis_points")] public int SevereHarvestMinBasisPoints { get; set; }
        [JsonProperty("severe_harvest_max_basis_points")] public int SevereHarvestMaxBasisPoints { get; set; }
        [JsonProperty("age_consumption_bands")] public List<ConsumptionBand> AgeConsumptionBands { get; set; }
        [JsonProperty("fertility_zero_below_food_basis_points")] public int FertilityZeroBelowFoodBasisPoints { get; set; }
        [JsonProperty("maximum_famine_mortality_basis_points")] public int MaximumFamineMortalityBasisPoints { get; set; }
        [JsonProperty("disease_outbreak_basis_points")] public int DiseaseOutbreakBasisPoints { get; set; }
        [JsonProperty("shortage_disease_bonus_basis_points")] public int ShortageDiseaseBonusBasisPoints { get; set; }
        [JsonProperty("disease_mortality_basis_points")] public int DiseaseMortalityBasisPoints { get; set; }
        [JsonProperty("local_conflict_basis_points")] public int LocalConflictBasisPoints { get; set; }
        [JsonProperty("conflict_food_seizure_min_basis_points")] public int ConflictFoodSeizureMinBasisPoints { get; set; }
        [JsonProperty("conflict_food_seizure_max_basis_points")] public int ConflictFoodSeizureMaxBasisPoints { get; set; }
        [JsonProperty("conflict_mortality_basis_points")] public int ConflictMortalityBasisPoints { get; set; }

        public static SubsistencePressureProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<SubsistencePressureProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.subsistence-pressure-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                string.IsNullOrWhiteSpace(value.FoodProductId) ||
                string.IsNullOrWhiteSpace(value.FoodUnitId) ||
                string.IsNullOrWhiteSpace(value.LandUnitId) ||
                value.OpeningFoodReserveAdultDays < 0 ||
                value.ArableMilliMuPerOpeningPerson <= 0 ||
                value.AgriculturalWorkerBasisPoints < 0 || value.AgriculturalWorkerBasisPoints > 10_000 ||
                value.MinimumWorkerAge < 0 || value.MaximumWorkerAge < value.MinimumWorkerAge ||
                value.LaborCapacityMilliMuPerWorker <= 0 ||
                value.GrossYieldMilliRationsPerMu <= 0 ||
                !BasisPoints(value.FieldSeedLossBasisPoints) ||
                !BasisPoints(value.AnnualStorageSpoilageBasisPoints) ||
                value.OrdinaryWeatherMinBasisPoints <= 0 ||
                value.OrdinaryWeatherMaxBasisPoints <= value.OrdinaryWeatherMinBasisPoints ||
                !BasisPoints(value.SevereHarvestEventBasisPoints) ||
                value.SevereHarvestMinBasisPoints <= 0 ||
                value.SevereHarvestMaxBasisPoints <= value.SevereHarvestMinBasisPoints ||
                !BasisPoints(value.FertilityZeroBelowFoodBasisPoints) ||
                !BasisPoints(value.MaximumFamineMortalityBasisPoints) ||
                !BasisPoints(value.DiseaseOutbreakBasisPoints) ||
                !BasisPoints(value.ShortageDiseaseBonusBasisPoints) ||
                !BasisPoints(value.DiseaseMortalityBasisPoints) ||
                !BasisPoints(value.LocalConflictBasisPoints) ||
                !BasisPoints(value.ConflictFoodSeizureMinBasisPoints) ||
                value.ConflictFoodSeizureMaxBasisPoints <= value.ConflictFoodSeizureMinBasisPoints ||
                value.ConflictFoodSeizureMaxBasisPoints > 10_001 ||
                !BasisPoints(value.ConflictMortalityBasisPoints) ||
                value.AgeConsumptionBands == null || value.AgeConsumptionBands.Count == 0)
            {
                throw new InvalidDataException("The subsistence pressure profile is invalid.");
            }
            int expectedAge = 0;
            for (var i = 0; i < value.AgeConsumptionBands.Count; i++)
            {
                ConsumptionBand band = value.AgeConsumptionBands[i];
                if (band.MinimumAge != expectedAge || band.MaximumAge < band.MinimumAge ||
                    band.AdultRationBasisPoints <= 0 || band.AdultRationBasisPoints > 20_000)
                {
                    throw new InvalidDataException("A consumption age band is invalid or has a gap.");
                }
                expectedAge = band.MaximumAge + 1;
            }
            if (expectedAge <= 130)
                throw new InvalidDataException("Consumption age bands do not cover the supported lifespan.");
            return value;
        }

        private static bool BasisPoints(int value)
        {
            return value >= 0 && value <= 10_000;
        }
    }

    internal sealed class HouseholdMarketReliefProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("cash_unit_id")] public string CashUnitId { get; set; }
        [JsonProperty("opening_cash_milli_per_person")] public int OpeningCashMilliPerPerson { get; set; }
        [JsonProperty("opening_county_granary_basis_points")] public int OpeningCountyGranaryBasisPoints { get; set; }
        [JsonProperty("opening_household_wealth_min_basis_points")] public int OpeningHouseholdWealthMinBasisPoints { get; set; }
        [JsonProperty("opening_household_wealth_max_basis_points")] public int OpeningHouseholdWealthMaxBasisPoints { get; set; }
        [JsonProperty("new_household_asset_transfer_basis_points")] public int NewHouseholdAssetTransferBasisPoints { get; set; }
        [JsonProperty("grain_tax_basis_points")] public int GrainTaxBasisPoints { get; set; }
        [JsonProperty("household_reserve_target_days")] public int HouseholdReserveTargetDays { get; set; }
        [JsonProperty("base_price_cash_milli_per_ration")] public int BasePriceCashMilliPerRation { get; set; }
        [JsonProperty("minimum_price_basis_points")] public int MinimumPriceBasisPoints { get; set; }
        [JsonProperty("maximum_price_basis_points")] public int MaximumPriceBasisPoints { get; set; }
        [JsonProperty("local_relief_release_basis_points")] public int LocalReliefReleaseBasisPoints { get; set; }
        [JsonProperty("county_granary_reserve_days")] public int CountyGranaryReserveDays { get; set; }
        [JsonProperty("carrier_worker_basis_points")] public int CarrierWorkerBasisPoints { get; set; }
        [JsonProperty("transport_capacity_milli_rations_per_carrier")] public int TransportCapacityMilliRationsPerCarrier { get; set; }
        [JsonProperty("transport_natural_loss_basis_points")] public int TransportNaturalLossBasisPoints { get; set; }
        [JsonProperty("transport_provision_basis_points")] public int TransportProvisionBasisPoints { get; set; }
        [JsonProperty("maximum_outbound_relief_routes_per_county_year")] public int MaximumOutboundReliefRoutesPerCountyYear { get; set; }

        public static HouseholdMarketReliefProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<HouseholdMarketReliefProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.household-market-relief-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                string.IsNullOrWhiteSpace(value.CashUnitId) ||
                value.OpeningCashMilliPerPerson < 0 ||
                !BasisPoints(value.OpeningCountyGranaryBasisPoints) ||
                value.OpeningHouseholdWealthMinBasisPoints <= 0 ||
                value.OpeningHouseholdWealthMaxBasisPoints <=
                    value.OpeningHouseholdWealthMinBasisPoints ||
                value.OpeningHouseholdWealthMaxBasisPoints > 50_001 ||
                !BasisPoints(value.NewHouseholdAssetTransferBasisPoints) ||
                !BasisPoints(value.GrainTaxBasisPoints) ||
                value.HouseholdReserveTargetDays < 0 ||
                value.BasePriceCashMilliPerRation <= 0 ||
                value.MinimumPriceBasisPoints <= 0 ||
                value.MaximumPriceBasisPoints < value.MinimumPriceBasisPoints ||
                !BasisPoints(value.LocalReliefReleaseBasisPoints) ||
                value.CountyGranaryReserveDays < 0 ||
                !BasisPoints(value.CarrierWorkerBasisPoints) ||
                value.TransportCapacityMilliRationsPerCarrier <= 0 ||
                !BasisPoints(value.TransportNaturalLossBasisPoints) ||
                !BasisPoints(value.TransportProvisionBasisPoints) ||
                value.TransportNaturalLossBasisPoints +
                    value.TransportProvisionBasisPoints >= 10_000 ||
                value.MaximumOutboundReliefRoutesPerCountyYear < 1 ||
                value.MaximumOutboundReliefRoutesPerCountyYear > 64)
            {
                throw new InvalidDataException("The household market relief profile is invalid.");
            }
            return value;
        }

        private static bool BasisPoints(int value)
        {
            return value >= 0 && value <= 10_000;
        }
    }

    internal sealed class HistoricalPopulationInput
    {
        public long EffectivePopulation { get; private set; }
        public long EffectiveHouseholds { get; private set; }
        public List<CountyPlan> Counties { get; private set; }

        public static HistoricalPopulationInput Load(
            string m12Path,
            string auditPath,
            string administrativeUnitsPath,
            int targetPopulation)
        {
            var audit = JObject.Parse(File.ReadAllText(auditPath, Encoding.UTF8));
            if ((string)audit["validation_status"] != "passed" ||
                (int)audit["row_counts"]["population_records"] != 105 ||
                (int)audit["county_catalog_audit"]["itemized_count"] != 1_182 ||
                (int)audit["mapping_audit"]["weight_error_count"] != 0)
            {
                throw new InvalidDataException("The M13 population audit did not pass the required contract.");
            }

            var m12 = JObject.Parse(File.ReadAllText(m12Path, Encoding.UTF8));
            if ((string)m12["schema_version"] != "han140.m12-input.v1" ||
                (int)m12["population_source_count"] != 105 ||
                (int)m12["county_catalog_count"] != 1_182)
            {
                throw new InvalidDataException("The M13 to M12 population input is invalid.");
            }
            long effectivePopulation = (long)m12["effective_totals"]["population"];
            long effectiveHouseholds = (long)m12["effective_totals"]["households"];
            var parentPopulations = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (JObject unit in m12["population_units"])
            {
                parentPopulations.Add(
                    (string)unit["admin_unit_id"],
                    (long)unit["effective_population"]);
            }
            if (parentPopulations.Count != 105 ||
                parentPopulations.Values.Sum() != effectivePopulation)
            {
                throw new InvalidDataException("The M13 population totals are inconsistent.");
            }

            List<Dictionary<string, string>> rows = Csv.Read(administrativeUnitsPath);
            var countyParents = rows.Where(item => item["unit_type"] == "county")
                .Select(item => new CountySource
                {
                    Id = item["admin_unit_id"],
                    ParentId = item["parent_admin_unit_id"]
                })
                .OrderBy(item => item.Id, StringComparer.Ordinal)
                .ToList();
            if (countyParents.Count != 1_182 ||
                countyParents.Any(item => !parentPopulations.ContainsKey(item.ParentId)))
            {
                throw new InvalidDataException("The 1182-county catalog does not match the 105 population units.");
            }

            var countyCounts = countyParents.GroupBy(item => item.ParentId)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var weighted = countyParents.Select(item => new AllocationItem
            {
                Id = item.Id,
                WeightNumerator = parentPopulations[item.ParentId],
                WeightDenominator = countyCounts[item.ParentId]
            }).ToList();
            Dictionary<string, int> people = Allocation.AllocateFractional(
                weighted, targetPopulation);
            long targetHouseholds = DivideRounded(
                (long)targetPopulation * effectiveHouseholds,
                effectivePopulation);
            Dictionary<string, int> households = Allocation.AllocateIntegerWeights(
                countyParents.Select(item => new IntegerWeight
                {
                    Id = item.Id,
                    Weight = people[item.Id]
                }).ToList(),
                checked((int)targetHouseholds),
                1);

            var counties = new List<CountyPlan>(countyParents.Count);
            for (var i = 0; i < countyParents.Count; i++)
            {
                counties.Add(new CountyPlan
                {
                    Index = i,
                    Id = countyParents[i].Id,
                    ParentId = countyParents[i].ParentId,
                    OpeningPopulation = people[countyParents[i].Id],
                    OpeningHouseholds = households[countyParents[i].Id]
                });
            }
            if (counties.Sum(item => item.OpeningPopulation) != targetPopulation ||
                counties.Sum(item => item.OpeningHouseholds) != targetHouseholds)
            {
                throw new InvalidDataException("The county allocation is not conserved.");
            }
            return new HistoricalPopulationInput
            {
                EffectivePopulation = effectivePopulation,
                EffectiveHouseholds = effectiveHouseholds,
                Counties = counties
            };
        }

        private static long DivideRounded(long numerator, long denominator)
        {
            return (numerator + denominator / 2L) / denominator;
        }
    }

    internal sealed class CountySource
    {
        public string Id;
        public string ParentId;
    }

    internal sealed class CountyPlan
    {
        public int Index;
        public string Id;
        public string ParentId;
        public int OpeningPopulation;
        public int OpeningHouseholds;
    }

    internal sealed class AllocationItem
    {
        public string Id;
        public long WeightNumerator;
        public int WeightDenominator;
    }

    internal sealed class IntegerWeight
    {
        public string Id;
        public int Weight;
    }

    internal static class Allocation
    {
        public static Dictionary<string, int> AllocateFractional(
            List<AllocationItem> items,
            int total)
        {
            decimal sumWeight = items.Sum(item =>
                (decimal)item.WeightNumerator / item.WeightDenominator);
            var work = items.Select(item =>
            {
                decimal exact = total *
                    ((decimal)item.WeightNumerator / item.WeightDenominator) /
                    sumWeight;
                return new AllocationWork
                {
                    Id = item.Id,
                    Value = (int)decimal.Floor(exact),
                    Remainder = exact - decimal.Floor(exact)
                };
            }).ToList();
            DistributeRemainder(work, total, 0);
            return work.ToDictionary(item => item.Id, item => item.Value, StringComparer.Ordinal);
        }

        public static Dictionary<string, int> AllocateIntegerWeights(
            List<IntegerWeight> items,
            int total,
            int minimum)
        {
            long sum = items.Sum(item => (long)item.Weight);
            var work = items.Select(item =>
            {
                decimal exact = (decimal)total * item.Weight / sum;
                return new AllocationWork
                {
                    Id = item.Id,
                    Value = Math.Max(minimum, (int)decimal.Floor(exact)),
                    Remainder = exact - decimal.Floor(exact)
                };
            }).ToList();
            DistributeRemainder(work, total, minimum);
            return work.ToDictionary(item => item.Id, item => item.Value, StringComparer.Ordinal);
        }

        private static void DistributeRemainder(
            List<AllocationWork> work,
            int total,
            int minimum)
        {
            int current = work.Sum(item => item.Value);
            if (current < total)
            {
                var order = work.OrderByDescending(item => item.Remainder)
                    .ThenBy(item => item.Id, StringComparer.Ordinal).ToList();
                for (var i = 0; current < total; i++, current++)
                {
                    order[i % order.Count].Value++;
                }
            }
            else if (current > total)
            {
                var order = work.OrderBy(item => item.Remainder)
                    .ThenByDescending(item => item.Id, StringComparer.Ordinal).ToList();
                var cursor = 0;
                while (current > total)
                {
                    AllocationWork item = order[cursor++ % order.Count];
                    if (item.Value > minimum)
                    {
                        item.Value--;
                        current--;
                    }
                }
            }
        }
    }

    internal sealed class AllocationWork
    {
        public string Id;
        public int Value;
        public decimal Remainder;
    }

    internal static class Csv
    {
        public static List<Dictionary<string, string>> Read(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) throw new InvalidDataException("CSV is empty: " + path);
            List<string> header = ParseLine(lines[0]);
            var result = new List<Dictionary<string, string>>(lines.Length - 1);
            for (var i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                List<string> values = ParseLine(lines[i]);
                if (values.Count != header.Count)
                {
                    throw new InvalidDataException("CSV column count mismatch at line " + (i + 1));
                }
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var column = 0; column < header.Count; column++)
                {
                    row.Add(header[column], values[column]);
                }
                result.Add(row);
            }
            return result;
        }

        private static List<string> ParseLine(string line)
        {
            var result = new List<string>();
            var value = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                char current = line[i];
                if (current == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (current == ',' && !quoted)
                {
                    result.Add(value.ToString());
                    value.Length = 0;
                }
                else
                {
                    value.Append(current);
                }
            }
            if (quoted) throw new InvalidDataException("Unclosed CSV quote.");
            result.Add(value.ToString());
            return result;
        }
    }

    internal enum Gender : byte
    {
        Male = 0,
        Female = 1
    }

    internal enum DeathCause : byte
    {
        None = 0,
        Natural = 1,
        Famine = 2,
        Disease = 3,
        LocalConflict = 4
    }

    internal sealed class PersonRecord
    {
        public long Id;
        public long HouseholdId;
        public long SpousePersonId;
        public long FatherPersonId;
        public long MotherPersonId;
        public int BirthDay;
        public int DeathDay = -1;
        public int CountyIndex;
        public int LastChildbirthDay = -1;
        public int ChildrenCount;
        public Gender Gender;
        public bool Alive = true;
        public DeathCause DeathCause;
    }

    internal sealed partial class HouseholdRecord
    {
        public long Id;
        public int CountyIndex;
        public int FoundedDay;
        public int LastFoodSatisfactionBasisPoints = 10_000;
        public long CumulativeUnmetFoodMilliRations;
        public long FoodInventoryMilliRations;
        public long CashMilli;
        public long CumulativeMarketPurchasedFoodMilliRations;
        public long CumulativeMarketSoldFoodMilliRations;
        public long CumulativeTaxFoodMilliRations;
        public long CumulativeReliefFoodMilliRations;
        public long CumulativeTransportReliefFoodMilliRations;
    }

    internal enum ScheduledEventType : byte
    {
        Death = 0,
        MarriageReady = 1,
        FertilityCheck = 2
    }

    internal struct ScheduledEvent
    {
        public ScheduledEventType Type;
        public long PersonId;
    }

    internal enum LifeEventType : byte
    {
        Marriage = 1,
        Birth = 2,
        Death = 3
    }

    internal sealed class AnnualPopulationRecord
    {
        [JsonProperty("year_index")] public int YearIndex { get; set; }
        [JsonProperty("calendar_year")] public int CalendarYear { get; set; }
        [JsonProperty("opening_living")] public long OpeningLiving { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("deaths")] public long Deaths { get; set; }
        [JsonProperty("marriages")] public long Marriages { get; set; }
        [JsonProperty("closing_living")] public long ClosingLiving { get; set; }
        [JsonProperty("cumulative_people")] public long CumulativePeople { get; set; }
        [JsonProperty("processed_events")] public long ProcessedEvents { get; set; }
        [JsonProperty("famine_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? FamineDeaths { get; set; }
        [JsonProperty("disease_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? DiseaseDeaths { get; set; }
        [JsonProperty("local_conflict_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? LocalConflictDeaths { get; set; }
    }

    internal sealed partial class CountySubsistenceState
    {
        public long ArableLandMilliMu;
        public long FoodInventoryMilliRations;
        public long GovernmentGranaryFoodMilliRations;
        public long GovernmentTreasuryCashMilli;
        public int LastMarketPriceCashMilliPerRation;
    }

    internal sealed partial class AnnualCountyResourceRecord
    {
        [JsonProperty("year_index")] public int YearIndex { get; set; }
        [JsonProperty("calendar_year")] public int CalendarYear { get; set; }
        [JsonProperty("county_id")] public string CountyId { get; set; }
        [JsonProperty("arable_land_milli_mu")] public long ArableLandMilliMu { get; set; }
        [JsonProperty("agricultural_workers")] public long AgriculturalWorkers { get; set; }
        [JsonProperty("cultivated_land_milli_mu")] public long CultivatedLandMilliMu { get; set; }
        [JsonProperty("opening_food_milli_rations")] public long OpeningFoodMilliRations { get; set; }
        [JsonProperty("gross_harvest_milli_rations")] public long GrossHarvestMilliRations { get; set; }
        [JsonProperty("field_seed_loss_milli_rations")] public long FieldSeedLossMilliRations { get; set; }
        [JsonProperty("storage_spoilage_milli_rations")] public long StorageSpoilageMilliRations { get; set; }
        [JsonProperty("conflict_seizure_milli_rations")] public long ConflictSeizureMilliRations { get; set; }
        [JsonProperty("household_need_milli_rations")] public long HouseholdNeedMilliRations { get; set; }
        [JsonProperty("actual_consumption_milli_rations")] public long ActualConsumptionMilliRations { get; set; }
        [JsonProperty("physical_consumption_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? PhysicalConsumptionMilliRations { get; set; }
        [JsonProperty("unmet_food_milli_rations")] public long UnmetFoodMilliRations { get; set; }
        [JsonProperty("closing_food_milli_rations")] public long ClosingFoodMilliRations { get; set; }
        [JsonProperty("food_satisfaction_basis_points")] public int FoodSatisfactionBasisPoints { get; set; }
        [JsonProperty("weather_basis_points")] public int WeatherBasisPoints { get; set; }
        [JsonProperty("severe_harvest_event")] public bool SevereHarvestEvent { get; set; }
        [JsonProperty("disease_outbreak")] public bool DiseaseOutbreak { get; set; }
        [JsonProperty("local_conflict")] public bool LocalConflict { get; set; }
        [JsonProperty("famine_deaths")] public long FamineDeaths { get; set; }
        [JsonProperty("disease_deaths")] public long DiseaseDeaths { get; set; }
        [JsonProperty("local_conflict_deaths")] public long LocalConflictDeaths { get; set; }
        [JsonProperty("grain_tax_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? GrainTaxMilliRations { get; set; }
        [JsonProperty("market_trade_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? MarketTradeMilliRations { get; set; }
        [JsonProperty("market_cash_transferred_milli", NullValueHandling = NullValueHandling.Ignore)] public long? MarketCashTransferredMilli { get; set; }
        [JsonProperty("market_price_cash_milli_per_ration", NullValueHandling = NullValueHandling.Ignore)] public int? MarketPriceCashMilliPerRation { get; set; }
        [JsonProperty("local_relief_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? LocalReliefMilliRations { get; set; }
        [JsonProperty("transport_relief_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TransportReliefMilliRations { get; set; }
        [JsonProperty("outbound_shipped_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? OutboundShippedMilliRations { get; set; }
        [JsonProperty("inbound_delivered_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? InboundDeliveredMilliRations { get; set; }
        [JsonProperty("transport_loss_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TransportLossMilliRations { get; set; }
        [JsonProperty("transport_provisions_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TransportProvisionsMilliRations { get; set; }
        [JsonProperty("closing_household_food_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? ClosingHouseholdFoodMilliRations { get; set; }
        [JsonProperty("closing_granary_food_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? ClosingGranaryFoodMilliRations { get; set; }
    }

    internal sealed class ReliefTransportRecord
    {
        [JsonProperty("year_index")] public int YearIndex { get; set; }
        [JsonProperty("calendar_year")] public int CalendarYear { get; set; }
        [JsonProperty("source_county_id")] public string SourceCountyId { get; set; }
        [JsonProperty("destination_county_id")] public string DestinationCountyId { get; set; }
        [JsonProperty("parent_commandery_id")] public string ParentCommanderyId { get; set; }
        [JsonProperty("shipped_milli_rations")] public long ShippedMilliRations { get; set; }
        [JsonProperty("delivered_milli_rations")] public long DeliveredMilliRations { get; set; }
        [JsonProperty("natural_loss_milli_rations")] public long NaturalLossMilliRations { get; set; }
        [JsonProperty("provisions_milli_rations")] public long ProvisionsMilliRations { get; set; }
        [JsonProperty("carrier_count")] public long CarrierCount { get; set; }
    }

    internal sealed class FileEvidence
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("bytes")] public long Bytes { get; set; }
        [JsonProperty("sha256")] public string Sha256 { get; set; }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("profile_source_layer")] public string ProfileSourceLayer { get; set; }
        [JsonProperty("master_seed")] public ulong MasterSeed { get; set; }
        [JsonProperty("opening_calendar_year")] public int OpeningCalendarYear { get; set; }
        [JsonProperty("years_simulated")] public int YearsSimulated { get; set; }
        [JsonProperty("county_count")] public int CountyCount { get; set; }
        [JsonProperty("opening_households")] public long OpeningHouseholds { get; set; }
        [JsonProperty("final_households")] public long FinalHouseholds { get; set; }
        [JsonProperty("initial_living_population")] public long InitialLivingPopulation { get; set; }
        [JsonProperty("final_living_population")] public long FinalLivingPopulation { get; set; }
        [JsonProperty("peak_living_population")] public long PeakLivingPopulation { get; set; }
        [JsonProperty("cumulative_person_count")] public long CumulativePersonCount { get; set; }
        [JsonProperty("total_births")] public long TotalBirths { get; set; }
        [JsonProperty("total_deaths")] public long TotalDeaths { get; set; }
        [JsonProperty("total_marriages")] public long TotalMarriages { get; set; }
        [JsonProperty("processed_scheduled_events")] public long ProcessedScheduledEvents { get; set; }
        [JsonProperty("elapsed_milliseconds")] public long ElapsedMilliseconds { get; set; }
        [JsonProperty("yearly_population")] public List<AnnualPopulationRecord> YearlyPopulation { get; set; }
        [JsonProperty("county_final_living_digest")] public string CountyFinalLivingDigest { get; set; }
        [JsonProperty("permanent_core")] public FileEvidence PermanentCore { get; set; }
        [JsonProperty("households")] public FileEvidence Households { get; set; }
        [JsonProperty("life_events")] public FileEvidence LifeEvents { get; set; }
        [JsonProperty("annual_ledger")] public FileEvidence AnnualLedger { get; set; }
        [JsonProperty("subsistence_pressure_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string SubsistencePressureProfileId { get; set; }
        [JsonProperty("subsistence_pressure_source_layer", NullValueHandling = NullValueHandling.Ignore)] public string SubsistencePressureSourceLayer { get; set; }
        [JsonProperty("opening_food_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? OpeningFoodMilliRations { get; set; }
        [JsonProperty("total_gross_harvest_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalGrossHarvestMilliRations { get; set; }
        [JsonProperty("total_field_seed_loss_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalFieldSeedLossMilliRations { get; set; }
        [JsonProperty("total_storage_spoilage_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalStorageSpoilageMilliRations { get; set; }
        [JsonProperty("total_conflict_seizure_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalConflictSeizureMilliRations { get; set; }
        [JsonProperty("total_household_need_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalHouseholdNeedMilliRations { get; set; }
        [JsonProperty("total_actual_consumption_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalActualConsumptionMilliRations { get; set; }
        [JsonProperty("total_physical_consumption_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalPhysicalConsumptionMilliRations { get; set; }
        [JsonProperty("total_unmet_food_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalUnmetFoodMilliRations { get; set; }
        [JsonProperty("final_food_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? FinalFoodMilliRations { get; set; }
        [JsonProperty("total_famine_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? TotalFamineDeaths { get; set; }
        [JsonProperty("total_disease_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? TotalDiseaseDeaths { get; set; }
        [JsonProperty("total_local_conflict_deaths", NullValueHandling = NullValueHandling.Ignore)] public long? TotalLocalConflictDeaths { get; set; }
        [JsonProperty("household_subsistence", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence HouseholdSubsistence { get; set; }
        [JsonProperty("pressure_events", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence PressureEvents { get; set; }
        [JsonProperty("annual_county_resources", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence AnnualCountyResources { get; set; }
        [JsonProperty("subsistence_digest", NullValueHandling = NullValueHandling.Ignore)] public string SubsistenceDigest { get; set; }
        [JsonProperty("household_market_relief_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string HouseholdMarketReliefProfileId { get; set; }
        [JsonProperty("household_market_relief_source_layer", NullValueHandling = NullValueHandling.Ignore)] public string HouseholdMarketReliefSourceLayer { get; set; }
        [JsonProperty("opening_cash_milli", NullValueHandling = NullValueHandling.Ignore)] public long? OpeningCashMilli { get; set; }
        [JsonProperty("final_household_cash_milli", NullValueHandling = NullValueHandling.Ignore)] public long? FinalHouseholdCashMilli { get; set; }
        [JsonProperty("final_government_cash_milli", NullValueHandling = NullValueHandling.Ignore)] public long? FinalGovernmentCashMilli { get; set; }
        [JsonProperty("total_grain_tax_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalGrainTaxMilliRations { get; set; }
        [JsonProperty("total_market_trade_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalMarketTradeMilliRations { get; set; }
        [JsonProperty("total_market_cash_transferred_milli", NullValueHandling = NullValueHandling.Ignore)] public long? TotalMarketCashTransferredMilli { get; set; }
        [JsonProperty("total_local_relief_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalLocalReliefMilliRations { get; set; }
        [JsonProperty("total_transport_relief_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalTransportReliefMilliRations { get; set; }
        [JsonProperty("total_transport_shipped_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalTransportShippedMilliRations { get; set; }
        [JsonProperty("total_transport_delivered_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalTransportDeliveredMilliRations { get; set; }
        [JsonProperty("total_transport_loss_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalTransportLossMilliRations { get; set; }
        [JsonProperty("total_transport_provisions_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalTransportProvisionsMilliRations { get; set; }
        [JsonProperty("household_economy", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence HouseholdEconomy { get; set; }
        [JsonProperty("relief_transports", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence ReliefTransports { get; set; }
        [JsonProperty("market_relief_digest", NullValueHandling = NullValueHandling.Ignore)] public string MarketReliefDigest { get; set; }
        [JsonProperty("invariants")] public List<string> Invariants { get; set; }
    }

    internal sealed class SelfTestEvidence
    {
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("passed")] public int Passed { get; set; }
        [JsonProperty("failed")] public int Failed { get; set; }
        [JsonProperty("tests")] public List<string> Tests { get; set; }
    }

    internal sealed partial class DemographicWorldRunner
    {
        private const int OpeningCalendarYear = 140;
        private const int CoreHeaderBytes = 16;
        private const int CoreRecordBytes = 62;
        private const int SubsistenceCoreRecordBytes = 63;
        private const int HouseholdHeaderBytes = 16;
        private const int HouseholdRecordBytes = 16;
        private const int HouseholdSubsistenceRecordBytes = 20;
        private const int HouseholdEconomyRecordBytes = 76;
        private const int EventHeaderBytes = 16;
        private const int EventRecordBytes = 33;
        private const int PressureEventRecordBytes = 25;

        private readonly WorldOptions _options;
        private readonly DemographyProfile _profile;
        private readonly SubsistencePressureProfile _subsistenceProfile;
        private readonly HouseholdMarketReliefProfile _marketReliefProfile;
        private readonly HistoricalPopulationInput _input;
        private readonly List<PersonRecord> _people;
        private readonly List<HouseholdRecord> _households;
        private readonly List<ScheduledEvent>[] _calendar;
        private readonly SortedSet<long>[] _readyMen;
        private readonly SortedSet<long>[] _readyWomen;
        private readonly HashSet<int> _dirtyMarriageCounties = new HashSet<int>();
        private readonly List<AnnualPopulationRecord> _years;
        private readonly List<AnnualCountyResourceRecord> _countyResourceYears;
        private readonly CountySubsistenceState[] _countySubsistence;
        private readonly List<int>[] _householdIndexesByCounty;
        private readonly Dictionary<string, List<int>> _countiesByParent;
        private readonly List<ReliefTransportRecord> _reliefTransports;
        private readonly int _totalDays;
        private readonly string _stagingPath;
        private readonly string _generationPath;
        private LifeEventWriter _eventWriter;
        private PressureEventWriter _pressureEventWriter;
        private long _nextHouseholdId;
        private long _living;
        private long _peakLiving;
        private long _totalBirths;
        private long _totalDeaths;
        private long _totalMarriages;
        private long _processedEvents;
        private long _yearBirths;
        private long _yearDeaths;
        private long _yearMarriages;
        private long _yearProcessed;
        private long _yearOpeningLiving;
        private long _openingFood;
        private long _totalGrossHarvest;
        private long _totalFieldSeedLoss;
        private long _totalStorageSpoilage;
        private long _totalConflictSeizure;
        private long _totalHouseholdNeed;
        private long _totalActualConsumption;
        private long _totalPhysicalConsumption;
        private long _totalUnmetFood;
        private long _totalFamineDeaths;
        private long _totalDiseaseDeaths;
        private long _totalLocalConflictDeaths;
        private long _yearFamineDeaths;
        private long _yearDiseaseDeaths;
        private long _yearLocalConflictDeaths;
        private long _openingCash;
        private long _totalGrainTax;
        private long _totalMarketTrade;
        private long _totalMarketCashTransferred;
        private long _totalLocalRelief;
        private long _totalTransportRelief;
        private long _totalTransportShipped;
        private long _totalTransportDelivered;
        private long _totalTransportLoss;
        private long _totalTransportProvisions;

        private DemographicWorldRunner(
            WorldOptions options,
            DemographyProfile profile,
            SubsistencePressureProfile subsistenceProfile,
            HouseholdMarketReliefProfile marketReliefProfile,
            HistoricalPopulationInput input,
            string stagingPath,
            string generationPath)
        {
            _options = options;
            _profile = profile;
            _subsistenceProfile = subsistenceProfile;
            _marketReliefProfile = marketReliefProfile;
            _input = input;
            _totalDays = checked(options.Years * profile.DaysPerYear);
            _people = new List<PersonRecord>(checked(options.InitialLivingPopulation * 2));
            _households = new List<HouseholdRecord>(options.InitialLivingPopulation / 4);
            _calendar = new List<ScheduledEvent>[_totalDays + 1];
            _readyMen = new SortedSet<long>[input.Counties.Count];
            _readyWomen = new SortedSet<long>[input.Counties.Count];
            for (var i = 0; i < input.Counties.Count; i++)
            {
                _readyMen[i] = new SortedSet<long>();
                _readyWomen[i] = new SortedSet<long>();
            }
            _years = new List<AnnualPopulationRecord>(options.Years);
            _countyResourceYears = subsistenceProfile == null
                ? null
                : new List<AnnualCountyResourceRecord>(checked(options.Years * input.Counties.Count));
            _countySubsistence = subsistenceProfile == null
                ? null
                : new CountySubsistenceState[input.Counties.Count];
            _householdIndexesByCounty = new List<int>[input.Counties.Count];
            for (var i = 0; i < input.Counties.Count; i++)
                _householdIndexesByCounty[i] = new List<int>();
            _countiesByParent = input.Counties
                .GroupBy(item => item.ParentId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Index)
                        .OrderBy(index => input.Counties[index].Id, StringComparer.Ordinal)
                        .ToList(),
                    StringComparer.Ordinal);
            _reliefTransports = marketReliefProfile == null
                ? null
                : new List<ReliefTransportRecord>();
            _stagingPath = stagingPath;
            _generationPath = generationPath;
        }

        public static WorldEvidence Run(WorldOptions options)
        {
            DemographyProfile profile = DemographyProfile.Load(options.ProfilePath);
            SubsistencePressureProfile subsistenceProfile = options.HasSubsistencePressure
                ? SubsistencePressureProfile.Load(options.SubsistencePressureProfilePath)
                : null;
            HouseholdMarketReliefProfile marketReliefProfile = options.HasHouseholdMarketRelief
                ? HouseholdMarketReliefProfile.Load(options.HouseholdMarketReliefProfilePath)
                : null;
            HouseholdProductionProfile householdProductionProfile = options.HasHouseholdProduction
                ? HouseholdProductionProfile.Load(options.HouseholdProductionProfilePath)
                : null;
            ProductionContentProjection productionContent = options.HasHouseholdProduction
                ? ProductionContentProjection.Load(
                    options.HasFoodEcology
                        ? new[]
                        {
                            options.ProductionContentPath,
                            options.FoodContentExtensionPath
                        }
                        : new[] { options.ProductionContentPath })
                : null;
            PopulationResourceCalibrationProfile calibrationProfile =
                options.HasPopulationResourceCalibration
                ? PopulationResourceCalibrationProfile.Load(
                    options.PopulationResourceCalibrationProfilePath)
                : null;
            FormalInventoryBridgeProfile formalInventoryBridgeProfile =
                options.HasFormalInventoryBridge
                ? FormalInventoryBridgeProfile.Load(
                    options.FormalInventoryBridgeProfilePath)
                : null;
            FoodProductProvenanceProfile foodProductProvenanceProfile =
                options.HasFoodProductProvenance
                ? FoodProductProvenanceProfile.Load(
                    options.FoodProductProvenanceProfilePath)
                : null;
            FoodEcologyProfile foodEcologyProfile = options.HasFoodEcology
                ? FoodEcologyProfile.Load(options.FoodEcologyProfilePath)
                : null;
            HistoricalPopulationInput input = HistoricalPopulationInput.Load(
                options.M12InputPath,
                options.AuditPath,
                options.AdministrativeUnitsPath,
                options.InitialLivingPopulation);
            Directory.CreateDirectory(options.WorkspacePath);
            string generationName = string.Format(
                CultureInfo.InvariantCulture,
                "generation-{0}-{1}-{2}{3}",
                options.Seed,
                options.InitialLivingPopulation,
                options.Years,
                foodEcologyProfile != null
                    ? "-food-ecology"
                    : foodProductProvenanceProfile != null
                    ? "-food-product-provenance"
                    : formalInventoryBridgeProfile != null
                    ? "-formal-inventory-bridge"
                    : calibrationProfile != null
                    ? "-population-resource-calibration"
                    : householdProductionProfile != null
                    ? "-household-production"
                    : marketReliefProfile != null
                    ? "-market-relief"
                    : subsistenceProfile == null ? string.Empty : "-subsistence");
            string generationPath = Path.Combine(options.WorkspacePath, generationName);
            if (Directory.Exists(generationPath))
            {
                throw new IOException("The completed generation already exists. Use the safe runner reset option.");
            }
            string stagingPath = Path.Combine(
                options.WorkspacePath,
                generationName + ".pending-" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(stagingPath);
            var runner = new DemographicWorldRunner(
                options,
                profile,
                subsistenceProfile,
                marketReliefProfile,
                input,
                stagingPath,
                generationPath);
            if (householdProductionProfile != null)
            {
                runner.ConfigureHouseholdProduction(
                    householdProductionProfile, productionContent);
            }
            if (calibrationProfile != null)
                runner.ConfigurePopulationResourceCalibration(calibrationProfile);
            if (formalInventoryBridgeProfile != null)
                runner.ConfigureFormalInventoryBridge(formalInventoryBridgeProfile);
            if (foodProductProvenanceProfile != null)
                runner.ConfigureFoodProductProvenance(foodProductProvenanceProfile);
            if (foodEcologyProfile != null)
                runner.ConfigureFoodEcology(foodEcologyProfile);
            try
            {
                return runner.Execute();
            }
            finally
            {
                runner.DisposeOwnedWriters();
            }
        }

        private void DisposeOwnedWriters()
        {
            if (_eventWriter != null)
            {
                _eventWriterCountAtClose = _eventWriter.Count;
                _eventWriter.Dispose();
                _eventWriter = null;
            }
            if (_pressureEventWriter != null)
            {
                _pressureWriterCountAtClose = _pressureEventWriter.Count;
                _pressureEventWriter.Dispose();
                _pressureEventWriter = null;
            }
            if (_farmWorkOrderWriter != null)
            {
                _farmWorkOrderCountAtClose = _farmWorkOrderWriter.Count;
                _farmWorkOrderWriter.Dispose();
                _farmWorkOrderWriter = null;
            }
        }

        private WorldEvidence Execute()
        {
            var stopwatch = Stopwatch.StartNew();
            string eventsPath = Path.Combine(_stagingPath, "life-events.bin");
            string pressureEventsPath = Path.Combine(_stagingPath, "pressure-events.bin");
            string farmWorkOrdersPath = Path.Combine(_stagingPath, "farm-work-orders.bin");
            _eventWriter = new LifeEventWriter(eventsPath);
            if (_subsistenceProfile != null)
                _pressureEventWriter = new PressureEventWriter(pressureEventsPath);
            if (_householdProductionProfile != null)
                _farmWorkOrderWriter = new FarmWorkOrderWriter(farmWorkOrdersPath);
            GenerateOpeningWorld();
            PairDirtyCounties(0, true);
            if (_subsistenceProfile != null) InitializeSubsistence();
            if (_householdProductionProfile != null) InitializeHouseholdProduction();
            if (_foodProductProvenanceProfile != null)
                InitializeFoodProductProvenance();
            if (_foodEcologyProfile != null)
                InitializeFoodEcology();
            _yearOpeningLiving = _living;
            WriteProgress("opening_generated", 0);

            for (var day = 1; day <= _totalDays; day++)
            {
                ProcessDay(day);
                if (day % _profile.DaysPerYear == 0)
                {
                    if (_subsistenceProfile != null)
                    {
                        if (_marketReliefProfile != null)
                            SettleMarketRelief(day / _profile.DaysPerYear, day);
                        else
                            SettleSubsistence(day / _profile.DaysPerYear, day);
                    }
                    CompleteYear(day / _profile.DaysPerYear);
                }
            }
            DisposeOwnedWriters();

            ValidateWorld();
            string corePath = Path.Combine(_stagingPath, "permanent-people.bin");
            string householdsPath = Path.Combine(_stagingPath, "households.bin");
            string annualPath = Path.Combine(_stagingPath, "annual-population.json");
            string householdSubsistencePath = Path.Combine(_stagingPath, "household-subsistence.bin");
            string countyResourcesPath = Path.Combine(_stagingPath, "annual-county-resources.json");
            string householdEconomyPath = Path.Combine(_stagingPath, "household-economy.bin");
            string reliefTransportsPath = Path.Combine(_stagingPath, "relief-transports.json");
            string householdProductionPath = Path.Combine(
                _stagingPath, "household-production.bin");
            string formalInventoryBatchesPath = Path.Combine(
                _stagingPath, "formal-inventory-batches.bin");
            string formalInventoryTransactionsPath = Path.Combine(
                _stagingPath, "formal-inventory-transactions.bin");
            string foodProductProvenancePath = Path.Combine(
                _stagingPath, "food-product-provenance.json");
            string foodEcologyPath = Path.Combine(
                _stagingPath, "food-ecology.json");
            WritePeople(corePath);
            WriteHouseholds(householdsPath);
            JsonFile.Write(annualPath, _years);
            if (_subsistenceProfile != null)
            {
                WriteHouseholdSubsistence(householdSubsistencePath);
                JsonFile.Write(countyResourcesPath, _countyResourceYears);
            }
            if (_marketReliefProfile != null)
            {
                WriteHouseholdEconomy(householdEconomyPath);
                JsonFile.Write(reliefTransportsPath, _reliefTransports);
            }
            if (_householdProductionProfile != null)
                WriteHouseholdProduction(householdProductionPath);
            if (_formalInventoryBridgeProfile != null)
                WriteFormalInventoryBridge(
                    formalInventoryBatchesPath,
                    formalInventoryTransactionsPath);
            if (_foodProductProvenanceProfile != null)
                WriteFoodProductProvenance(foodProductProvenancePath);
            if (_foodEcologyProfile != null)
                WriteFoodEcology(foodEcologyPath);
            ValidatePhysicalFiles(
                corePath,
                householdsPath,
                eventsPath,
                householdSubsistencePath,
                pressureEventsPath,
                householdEconomyPath,
                householdProductionPath,
                farmWorkOrdersPath);

            var evidence = new WorldEvidence
            {
                Status = "passed",
                SchemaVersion = _foodProductProvenanceProfile != null
                    ? "m24.p6.food-product-provenance-world.v1"
                    : _formalInventoryBridgeProfile != null
                    ? "m24.p5.formal-inventory-bridge-world.v1"
                    : _populationResourceCalibrationProfile != null
                    ? "m24.p4.population-resource-calibration-world.v1"
                    : _householdProductionProfile != null
                    ? "m24.p3.household-production-world.v1"
                    : _marketReliefProfile != null
                    ? "m24.p2.market-relief-world.v1"
                    : _subsistenceProfile == null
                        ? "m24.p0.demographic-world.v1"
                        : "m24.p1.subsistence-world.v1",
                ProfileId = _profile.Id,
                ProfileSourceLayer = _profile.SourceLayer,
                MasterSeed = _options.Seed,
                OpeningCalendarYear = OpeningCalendarYear,
                YearsSimulated = _options.Years,
                CountyCount = _input.Counties.Count,
                OpeningHouseholds = _input.Counties.Sum(item => (long)item.OpeningHouseholds),
                FinalHouseholds = _households.Count,
                InitialLivingPopulation = _options.InitialLivingPopulation,
                FinalLivingPopulation = _living,
                PeakLivingPopulation = _peakLiving,
                CumulativePersonCount = _people.Count,
                TotalBirths = _totalBirths,
                TotalDeaths = _totalDeaths,
                TotalMarriages = _totalMarriages,
                ProcessedScheduledEvents = _processedEvents,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                YearlyPopulation = _years,
                CountyFinalLivingDigest = BuildCountyDigest(),
                PermanentCore = FileInfo(corePath),
                Households = FileInfo(householdsPath),
                LifeEvents = FileInfo(eventsPath),
                AnnualLedger = FileInfo(annualPath),
                Invariants = new List<string>
                {
                    "m13_105_population_sources_and_1182_counties_validated",
                    "opening_population_equals_requested_one_million_scale",
                    "permanent_ids_are_contiguous_and_unique",
                    "every_person_has_county_and_household",
                    "birth_parent_references_are_older_and_preexisting",
                    "yearly_living_equals_opening_plus_births_minus_deaths",
                    "cumulative_equals_initial_plus_births",
                    "living_equals_cumulative_minus_deaths",
                    "dead_people_remain_in_permanent_core",
                    "event_counts_match_birth_death_and_marriage_ledgers",
                    "physical_files_reload_with_expected_counts_and_lengths"
                }
            };
            if (_subsistenceProfile != null)
            {
                evidence.SubsistencePressureProfileId = _subsistenceProfile.Id;
                evidence.SubsistencePressureSourceLayer = _subsistenceProfile.SourceLayer;
                evidence.OpeningFoodMilliRations = _openingFood;
                evidence.TotalGrossHarvestMilliRations = _totalGrossHarvest;
                evidence.TotalFieldSeedLossMilliRations = _totalFieldSeedLoss;
                evidence.TotalStorageSpoilageMilliRations = _totalStorageSpoilage;
                evidence.TotalConflictSeizureMilliRations = _totalConflictSeizure;
                evidence.TotalHouseholdNeedMilliRations = _totalHouseholdNeed;
                evidence.TotalActualConsumptionMilliRations = _totalActualConsumption;
                evidence.TotalPhysicalConsumptionMilliRations =
                    _marketReliefProfile == null
                        ? _totalActualConsumption
                        : _totalPhysicalConsumption;
                evidence.TotalUnmetFoodMilliRations = _totalUnmetFood;
                evidence.FinalFoodMilliRations = TotalFoodInventory();
                evidence.TotalFamineDeaths = _totalFamineDeaths;
                evidence.TotalDiseaseDeaths = _totalDiseaseDeaths;
                evidence.TotalLocalConflictDeaths = _totalLocalConflictDeaths;
                evidence.HouseholdSubsistence = FileInfo(householdSubsistencePath);
                evidence.PressureEvents = FileInfo(pressureEventsPath);
                evidence.AnnualCountyResources = FileInfo(countyResourcesPath);
                evidence.SubsistenceDigest = BuildSubsistenceDigest();
                evidence.Invariants.Add("fixed_county_land_never_negative_or_created");
                evidence.Invariants.Add("county_and_global_food_ledgers_conserve");
                evidence.Invariants.Add("household_consumption_is_derived_from_specific_living_members");
                evidence.Invariants.Add("food_satisfaction_modifies_fertility_and_pressure_mortality");
                evidence.Invariants.Add("pressure_deaths_reference_permanent_people_and_do_not_double_count");
            }
            if (_marketReliefProfile != null)
            {
                evidence.HouseholdMarketReliefProfileId = _marketReliefProfile.Id;
                evidence.HouseholdMarketReliefSourceLayer = _marketReliefProfile.SourceLayer;
                evidence.OpeningCashMilli = _openingCash;
                evidence.FinalHouseholdCashMilli = _households.Sum(item => item.CashMilli);
                evidence.FinalGovernmentCashMilli = _countySubsistence.Sum(
                    item => item.GovernmentTreasuryCashMilli);
                evidence.TotalGrainTaxMilliRations = _totalGrainTax;
                evidence.TotalMarketTradeMilliRations = _totalMarketTrade;
                evidence.TotalMarketCashTransferredMilli = _totalMarketCashTransferred;
                evidence.TotalLocalReliefMilliRations = _totalLocalRelief;
                evidence.TotalTransportReliefMilliRations = _totalTransportRelief;
                evidence.TotalTransportShippedMilliRations = _totalTransportShipped;
                evidence.TotalTransportDeliveredMilliRations = _totalTransportDelivered;
                evidence.TotalTransportLossMilliRations = _totalTransportLoss;
                evidence.TotalTransportProvisionsMilliRations = _totalTransportProvisions;
                evidence.HouseholdEconomy = FileInfo(householdEconomyPath);
                evidence.ReliefTransports = FileInfo(reliefTransportsPath);
                evidence.MarketReliefDigest = BuildMarketReliefDigest();
                evidence.Invariants.Add("household_food_and_cash_never_negative");
                evidence.Invariants.Add("market_food_and_cash_clear_bilaterally");
                evidence.Invariants.Add("grain_tax_and_relief_are_internal_ownership_transfers");
                evidence.Invariants.Add("relief_transport_shipped_equals_delivered_loss_and_provisions");
                evidence.Invariants.Add("global_household_government_cash_conserves");
                evidence.Invariants.Add("global_food_conserves_across_households_granaries_and_transport");
            }
            if (_householdProductionProfile != null)
            {
                evidence.HouseholdProductionProfileId = _householdProductionProfile.Id;
                evidence.ProductionContentPackageId = _productionContent.PackageId;
                evidence.ProductionContentPackageVersion = _productionContent.PackageVersion;
                evidence.ProductionContentSha256 = _productionContent.ContentSha256;
                evidence.AgriculturalBindingCount = _productionContent.Bindings.Count;
                evidence.TotalFarmWorkOrders = _totalFarmWorkOrders;
                evidence.TotalSeedConsumedMilliRations = _totalSeedConsumed;
                evidence.TotalSeedRetainedMilliRations = _totalSeedRetained;
                evidence.TotalLandRentMilliRations = _totalLandRent;
                evidence.FinalSeedInventoryMilliRations =
                    _households.Sum(item => item.SeedInventoryMilliRations) +
                    _countySubsistence.Sum(item => item.GovernmentSeedInventoryMilliRations);
                evidence.HouseholdProduction = FileInfo(householdProductionPath);
                evidence.FarmWorkOrders = FileInfo(farmWorkOrdersPath);
                evidence.HouseholdProductionDigest = HashBytes(Encoding.UTF8.GetBytes(
                    evidence.HouseholdProduction.Sha256 + "|" +
                    evidence.FarmWorkOrders.Sha256 + "|" +
                    evidence.ProductionContentSha256));
                evidence.Invariants.Add("production_content_resolves_by_stable_ids");
                evidence.Invariants.Add("household_owned_leased_and_public_land_conserve");
                evidence.Invariants.Add("farm_orders_are_limited_by_real_land_labor_and_seed");
                evidence.Invariants.Add("farm_order_output_equals_household_seed_rent_and_tax_destinations");
                evidence.Invariants.Add("seed_batches_never_go_negative_or_fabricate_planting_input");
                evidence.Invariants.Add("farm_order_history_is_streamed_without_changing_world_facts");
                EvaluateCalibration(evidence);
                evidence.Invariants.Add("population_resource_diagnostics_do_not_change_world_facts");
                if (_populationResourceCalibrationProfile != null)
                {
                    evidence.Invariants.Add("seasonal_public_land_use_never_exceeds_public_land");
                    evidence.Invariants.Add("calibration_thresholds_are_declared_before_result_evaluation");
                }
                if (_formalInventoryBridgeProfile != null)
                {
                    evidence.FormalInventoryBridgeProfileId =
                        _formalInventoryBridgeProfile.Id;
                    evidence.FormalInventoryContract =
                        _formalInventoryBridgeProfile.FormalSnapshotContract;
                    evidence.FormalInventoryBatchCount =
                        _formalInventoryBridgeAudit.BatchCount;
                    evidence.FormalInventoryTransactionCount =
                        _formalInventoryBridgeAudit.TransactionCount;
                    evidence.FormalInventorySourceFood =
                        _formalInventoryBridgeAudit.SourceFood;
                    evidence.FormalInventorySourceSeed =
                        _formalInventoryBridgeAudit.SourceSeed;
                    evidence.FormalInventoryBatchQuantity =
                        _formalInventoryBridgeAudit.BatchQuantity;
                    evidence.FormalInventorySourceBalanceDelta =
                        _formalInventoryBridgeAudit.SourceBalanceDelta;
                    evidence.FormalInventoryBatches =
                        FileInfo(formalInventoryBatchesPath);
                    evidence.FormalInventoryTransactions =
                        FileInfo(formalInventoryTransactionsPath);
                    evidence.FormalInventoryDigest = HashBytes(
                        Encoding.UTF8.GetBytes(
                            evidence.FormalInventoryBatches.Sha256 + "|" +
                            evidence.FormalInventoryTransactions.Sha256 + "|" +
                            evidence.ProductionContentSha256));
                    evidence.Invariants.Add(
                        "compact_food_and_seed_balances_are_replaced_not_duplicated");
                    evidence.Invariants.Add(
                        "formal_batches_equal_source_balances_and_transaction_lines");
                    evidence.Invariants.Add(
                        "formal_inventory_bridge_uses_stable_content_and_owner_identities");
                }
                if (_foodProductProvenanceProfile != null)
                {
                    var provenance = BuildFoodProductProvenanceRecords();
                    evidence.FoodProductProvenanceProfileId =
                        _foodProductProvenanceProfile.Id;
                    evidence.FoodProductCount = provenance.Count;
                    evidence.FoodProductProvenance =
                        FileInfo(foodProductProvenancePath);
                    evidence.FoodProductProvenanceDigest =
                        evidence.FoodProductProvenance.Sha256;
                    evidence.FoodProductConservationTotal = provenance.Sum(
                        item => item.ClosingHouseholdQuantity +
                            item.ClosingGovernmentQuantity);
                    evidence.Invariants.Add(
                        "food_product_vectors_equal_scalar_compatibility_balances");
                    evidence.Invariants.Add(
                        "market_tax_relief_consumption_loss_and_transport_preserve_product_identity");
                    evidence.Invariants.Add(
                        "each_food_product_conserves_independently");
                }
                if (_foodEcologyProfile != null)
                {
                    FoodEcologyReport ecology = BuildFoodEcologyReport();
                    evidence.FoodEcologyProfileId = _foodEcologyProfile.Id;
                    evidence.FoodEcology = FileInfo(foodEcologyPath);
                    evidence.FoodEcologyDigest = evidence.FoodEcology.Sha256;
                    evidence.FoodEcologyRotationAdjustedWorkOrders =
                        ecology.RotationAdjustedWorkOrders;
                    evidence.FoodEcologyProcessedQuantity =
                        ecology.ProcessedQuantity;
                    evidence.FoodEcologyConsumedNutrition =
                        ecology.ConsumedNutritionMilliRations;
                    evidence.Invariants.Add(
                        "food_ecology_traits_change_yield_nutrition_price_volume_and_loss_selection");
                    evidence.Invariants.Add(
                        "food_processing_preserves_physical_quantity_and_product_provenance");
                    evidence.Invariants.Add(
                        "county_legume_share_provides_bounded_rotation_support");
                }
            }
            Directory.Move(_stagingPath, _generationPath);
            RewritePathsAfterMove(evidence, _generationPath);
            JsonFile.Write(Path.Combine(_generationPath, "manifest.json"), evidence);
            WriteProgress("completed", _options.Years);
            return evidence;
        }

        private void GenerateOpeningWorld()
        {
            var openingSingles = new List<long>();
            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                CountyPlan county = _input.Counties[countyIndex];
                int baseSize = county.OpeningPopulation / county.OpeningHouseholds;
                int extraMembers = county.OpeningPopulation % county.OpeningHouseholds;
                for (var householdOrdinal = 0;
                     householdOrdinal < county.OpeningHouseholds;
                     householdOrdinal++)
                {
                    int size = baseSize + (householdOrdinal < extraMembers ? 1 : 0);
                    long householdId = ++_nextHouseholdId;
                    _households.Add(new HouseholdRecord
                    {
                        Id = householdId,
                        CountyIndex = countyIndex,
                        FoundedDay = 0
                    });
                    _householdIndexesByCounty[countyIndex].Add(_households.Count - 1);
                    var memberIds = new long[size];
                    for (var member = 0; member < size; member++)
                    {
                        memberIds[member] = _people.Count + member + 1L;
                    }
                    int fatherAge = 24 + StableRandom.Range(
                        _options.Seed, 101UL, householdId, countyIndex, 0, 29);
                    int motherAge = 20 + StableRandom.Range(
                        _options.Seed, 102UL, householdId, countyIndex, 0, 23);
                    for (var member = 0; member < size; member++)
                    {
                        long personId = memberIds[member];
                        Gender gender;
                        int age;
                        long fatherId = 0;
                        long motherId = 0;
                        if (member == 0)
                        {
                            gender = Gender.Male;
                            age = fatherAge;
                        }
                        else if (member == 1)
                        {
                            gender = Gender.Female;
                            age = motherAge;
                        }
                        else
                        {
                            gender = StableRandom.CheckBasisPoints(
                                _options.Seed,
                                103UL,
                                personId,
                                countyIndex,
                                _profile.FemaleBasisPoints)
                                ? Gender.Female
                                : Gender.Male;
                            age = ChooseInitialAge(personId, countyIndex);
                            int maximumChildAge = Math.Min(
                                17,
                                Math.Min(fatherAge, motherAge) - 18);
                            if (maximumChildAge >= 0 && age <= maximumChildAge)
                            {
                                fatherId = memberIds[0];
                                motherId = memberIds[1];
                            }
                        }
                        int offset = StableRandom.Range(
                            _options.Seed,
                            104UL,
                            personId,
                            countyIndex,
                            0,
                            _profile.DaysPerYear);
                        var person = new PersonRecord
                        {
                            Id = personId,
                            HouseholdId = householdId,
                            FatherPersonId = fatherId,
                            MotherPersonId = motherId,
                            BirthDay = checked(-age * _profile.DaysPerYear - offset),
                            CountyIndex = countyIndex,
                            Gender = gender
                        };
                        _people.Add(person);
                    }
                    if (size >= 2)
                    {
                        PersonRecord father = GetPerson(memberIds[0]);
                        PersonRecord mother = GetPerson(memberIds[1]);
                        father.SpousePersonId = mother.Id;
                        mother.SpousePersonId = father.Id;
                        _eventWriter.Write(
                            LifeEventType.Marriage,
                            0,
                            mother.Id,
                            father.Id,
                            householdId,
                            countyIndex);
                        _totalMarriages++;
                        ScheduleFirstFertility(mother, 0);
                    }
                    for (var member = 0; member < size; member++)
                    {
                        PersonRecord person = GetPerson(memberIds[member]);
                        ScheduleDeath(person, 0);
                        if (person.SpousePersonId == 0)
                        {
                            int readyDay = MarriageReadyDay(person, 0);
                            if (readyDay == 0) openingSingles.Add(person.Id);
                            else Schedule(readyDay, ScheduledEventType.MarriageReady, person.Id);
                        }
                    }
                }
            }
            if (_people.Count != _options.InitialLivingPopulation)
            {
                throw new InvalidOperationException("Opening population generation did not reach the requested count.");
            }
            _living = _people.Count;
            _peakLiving = _living;
            for (var i = 0; i < openingSingles.Count; i++)
            {
                AddReady(GetPerson(openingSingles[i]));
            }
        }

        private void InitializeSubsistence()
        {
            _openingFood = 0;
            var openingMembers = _marketReliefProfile == null
                ? null
                : new int[_households.Count];
            if (openingMembers != null)
            {
                for (var i = 0; i < _people.Count; i++)
                    openingMembers[checked((int)_people[i].HouseholdId - 1)]++;
            }
            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                CountyPlan county = _input.Counties[countyIndex];
                long land = checked(
                    (long)county.OpeningPopulation *
                    _subsistenceProfile.ArableMilliMuPerOpeningPerson);
                long food = checked(
                    (long)county.OpeningPopulation *
                    _subsistenceProfile.OpeningFoodReserveAdultDays * 1_000L);
                var state = new CountySubsistenceState
                {
                    ArableLandMilliMu = land,
                    FoodInventoryMilliRations = _marketReliefProfile == null ? food : 0,
                    LastMarketPriceCashMilliPerRation = _marketReliefProfile == null
                        ? 0
                        : _marketReliefProfile.BasePriceCashMilliPerRation
                };
                _countySubsistence[countyIndex] = state;
                _openingFood = checked(_openingFood + food);
                if (_marketReliefProfile != null)
                {
                    state.GovernmentGranaryFoodMilliRations = checked(
                        food * _marketReliefProfile.OpeningCountyGranaryBasisPoints / 10_000L);
                    long householdFood = food - state.GovernmentGranaryFoodMilliRations;
                    long allocatedFood = 0;
                    List<int> householdIndexes = _householdIndexesByCounty[countyIndex];
                    long actualOpeningMembers = householdIndexes.Sum(
                        index => (long)openingMembers[index]);
                    for (var h = 0; h < householdIndexes.Count; h++)
                    {
                        int householdIndex = householdIndexes[h];
                        int members = openingMembers[householdIndex];
                        long share = actualOpeningMembers == 0
                            ? 0
                            : checked(householdFood * members / actualOpeningMembers);
                        _households[householdIndex].FoodInventoryMilliRations = share;
                        allocatedFood += share;
                        int wealthBasisPoints = StableRandom.Range(
                            _options.Seed,
                            501UL,
                            _households[householdIndex].Id,
                            countyIndex,
                            _marketReliefProfile.OpeningHouseholdWealthMinBasisPoints,
                            _marketReliefProfile.OpeningHouseholdWealthMaxBasisPoints);
                        long cash = checked(
                            (long)members * _marketReliefProfile.OpeningCashMilliPerPerson *
                            wealthBasisPoints / 10_000L);
                        _households[householdIndex].CashMilli = cash;
                        _openingCash = checked(_openingCash + cash);
                    }
                    long remainder = householdFood - allocatedFood;
                    for (var h = 0; remainder > 0 && h < householdIndexes.Count; h++)
                    {
                        int householdIndex = householdIndexes[h];
                        if (openingMembers[householdIndex] <= 0) continue;
                        _households[householdIndex].FoodInventoryMilliRations++;
                        remainder--;
                        if (h == householdIndexes.Count - 1 && remainder > 0) h = -1;
                    }
                }
            }
        }

        private void SettleMarketRelief(int yearIndex, int day)
        {
            int householdCount = _households.Count;
            int countyCount = _input.Counties.Count;
            var householdNeed = new long[householdCount];
            var householdConsumed = new long[householdCount];
            var householdPhysicalConsumed = new long[householdCount];
            var householdWorkers = new long[householdCount];
            var householdAlive = new long[householdCount];
            var countyNeed = new long[countyCount];
            var countyWorkers = new long[countyCount];
            var countyAlive = new long[countyCount];
            var countyCarriers = new long[countyCount];
            var countyDisease = new bool[countyCount];
            var countyConflict = new bool[countyCount];
            var remainingDeficit = new long[countyCount];
            var inbound = new long[countyCount];
            var outbound = new long[countyCount];
            var transportLoss = new long[countyCount];
            var transportProvisions = new long[countyCount];
            var transportCapacity = new long[countyCount];
            var eligibleReliefDonor = new bool[countyCount];
            var records = new AnnualCountyResourceRecord[countyCount];

            for (var i = 0; i < _people.Count; i++)
            {
                PersonRecord person = _people[i];
                if (!person.Alive) continue;
                int age = AgeAt(person, day);
                int householdIndex = checked((int)person.HouseholdId - 1);
                long need = AnnualFoodNeed(age);
                householdAlive[householdIndex]++;
                householdNeed[householdIndex] = checked(householdNeed[householdIndex] + need);
                countyAlive[person.CountyIndex]++;
                countyNeed[person.CountyIndex] = checked(countyNeed[person.CountyIndex] + need);
                if (age < _subsistenceProfile.MinimumWorkerAge ||
                    age > _subsistenceProfile.MaximumWorkerAge) continue;
                if (StableRandom.CheckBasisPoints(
                    _options.Seed, 401UL, person.Id, 0,
                    _subsistenceProfile.AgriculturalWorkerBasisPoints))
                {
                    householdWorkers[householdIndex]++;
                    countyWorkers[person.CountyIndex]++;
                }
                if (StableRandom.CheckBasisPoints(
                    _options.Seed, 511UL, person.Id, yearIndex,
                    _marketReliefProfile.CarrierWorkerBasisPoints))
                {
                    countyCarriers[person.CountyIndex]++;
                }
            }

            TransferExtinctHouseholdAssets(householdAlive);
            for (var countyIndex = 0; countyIndex < countyCount; countyIndex++)
            {
                CountySubsistenceState county = _countySubsistence[countyIndex];
                long openingFood = CountyFood(countyIndex);
                long spoilage = ApplyStockLoss(
                    countyIndex,
                    _subsistenceProfile.AnnualStorageSpoilageBasisPoints,
                    FoodSinkKind.Spoilage);
                bool severe = StableRandom.CheckBasisPoints(
                    _options.Seed, 402UL, countyIndex, yearIndex,
                    _subsistenceProfile.SevereHarvestEventBasisPoints);
                int weather = severe
                    ? StableRandom.Range(
                        _options.Seed, 403UL, countyIndex, yearIndex,
                        _subsistenceProfile.SevereHarvestMinBasisPoints,
                        _subsistenceProfile.SevereHarvestMaxBasisPoints)
                    : StableRandom.Range(
                        _options.Seed, 404UL, countyIndex, yearIndex,
                        _subsistenceProfile.OrdinaryWeatherMinBasisPoints,
                        _subsistenceProfile.OrdinaryWeatherMaxBasisPoints);
                ProductionYearResult production = null;
                long cultivated;
                long gross;
                long fieldLoss;
                long grainTax;
                if (_householdProductionProfile != null)
                {
                    production = ExecuteHouseholdProduction(
                        countyIndex, yearIndex, weather, householdWorkers);
                    cultivated = production.CultivatedLandMilliMu;
                    gross = production.GrossHarvestMilliRations;
                    fieldLoss = production.SeedConsumedMilliRations;
                    grainTax = production.GrainTaxMilliRations;
                }
                else
                {
                    cultivated = Math.Min(
                        county.ArableLandMilliMu,
                        checked(countyWorkers[countyIndex] *
                            _subsistenceProfile.LaborCapacityMilliMuPerWorker));
                    gross = checked(
                        cultivated * _subsistenceProfile.GrossYieldMilliRationsPerMu /
                        1_000L * weather / 10_000L);
                    fieldLoss = checked(
                        gross * _subsistenceProfile.FieldSeedLossBasisPoints / 10_000L);
                    long netHarvest = gross - fieldLoss;
                    grainTax = checked(
                        netHarvest * _marketReliefProfile.GrainTaxBasisPoints / 10_000L);
                    county.GovernmentGranaryFoodMilliRations = checked(
                        county.GovernmentGranaryFoodMilliRations + grainTax);
                    AllocateHarvest(
                        countyIndex, householdWorkers, netHarvest - grainTax, grainTax);
                }

                ExecuteFoodEcologyProcessing(countyIndex);

                bool conflict = StableRandom.CheckBasisPoints(
                    _options.Seed, 405UL, countyIndex, yearIndex,
                    _subsistenceProfile.LocalConflictBasisPoints);
                long seizure = 0;
                if (conflict)
                {
                    int basisPoints = StableRandom.Range(
                        _options.Seed, 406UL, countyIndex, yearIndex,
                        _subsistenceProfile.ConflictFoodSeizureMinBasisPoints,
                        _subsistenceProfile.ConflictFoodSeizureMaxBasisPoints);
                    seizure = ApplyStockLoss(
                        countyIndex, basisPoints, FoodSinkKind.Conflict);
                }
                countyConflict[countyIndex] = conflict;
                records[countyIndex] = new AnnualCountyResourceRecord
                {
                    YearIndex = yearIndex,
                    CalendarYear = OpeningCalendarYear + yearIndex,
                    CountyId = _input.Counties[countyIndex].Id,
                    ArableLandMilliMu = county.ArableLandMilliMu,
                    AgriculturalWorkers = countyWorkers[countyIndex],
                    CultivatedLandMilliMu = cultivated,
                    OpeningFoodMilliRations = openingFood,
                    GrossHarvestMilliRations = gross,
                    FieldSeedLossMilliRations = fieldLoss,
                    StorageSpoilageMilliRations = spoilage,
                    ConflictSeizureMilliRations = seizure,
                    HouseholdNeedMilliRations = countyNeed[countyIndex],
                    WeatherBasisPoints = weather,
                    SevereHarvestEvent = severe,
                    LocalConflict = conflict,
                    GrainTaxMilliRations = grainTax,
                    MarketTradeMilliRations = 0,
                    MarketCashTransferredMilli = 0,
                    LocalReliefMilliRations = 0,
                    TransportReliefMilliRations = 0,
                    OutboundShippedMilliRations = 0,
                    InboundDeliveredMilliRations = 0,
                    TransportLossMilliRations = 0,
                    TransportProvisionsMilliRations = 0,
                    FarmWorkOrderCount = production == null
                        ? (long?)null
                        : production.WorkOrderCount,
                    SeedConsumedMilliRations = production == null
                        ? (long?)null
                        : production.SeedConsumedMilliRations,
                    SeedRetainedMilliRations = production == null
                        ? (long?)null
                        : production.SeedRetainedMilliRations,
                    LandRentMilliRations = production == null
                        ? (long?)null
                        : production.LandRentMilliRations,
                    SeasonalPublicLandCultivatedMilliMu = production == null ||
                        _populationResourceCalibrationProfile == null
                        ? (long?)null
                        : production.SeasonalPublicLandCultivatedMilliMu
                };
                _totalGrossHarvest = checked(_totalGrossHarvest + gross);
                _totalFieldSeedLoss = checked(_totalFieldSeedLoss + fieldLoss);
                _totalStorageSpoilage = checked(_totalStorageSpoilage + spoilage);
                _totalConflictSeizure = checked(_totalConflictSeizure + seizure);
                _totalGrainTax = checked(_totalGrainTax + grainTax);
            }

            for (var householdIndex = 0; householdIndex < householdCount; householdIndex++)
            {
                HouseholdRecord household = _households[householdIndex];
                long own = Math.Min(
                    householdNeed[householdIndex], household.FoodInventoryMilliRations);
                household.FoodInventoryMilliRations -= own;
                long[] ownProducts = TrackHouseholdFoodRemoved(
                    household, own, FoodSinkKind.Consumption);
                long ownNutrition = FoodNutrition(ownProducts);
                _consumedNutrition = checked(_consumedNutrition + ownNutrition);
                householdConsumed[householdIndex] = Math.Min(
                    householdNeed[householdIndex], ownNutrition);
                householdPhysicalConsumed[householdIndex] = own;
            }

            for (var countyIndex = 0; countyIndex < countyCount; countyIndex++)
            {
                SettleCountyMarket(
                    countyIndex, householdNeed, householdConsumed,
                    householdPhysicalConsumed, records[countyIndex]);
                long deficit = CountyDeficit(countyIndex, householdNeed, householdConsumed);
                long release = Math.Min(
                    deficit,
                    checked(_countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations *
                        _marketReliefProfile.LocalReliefReleaseBasisPoints / 10_000L));
                if (release > 0)
                {
                    long reliefNutrition = TrackReliefConsumption(
                        _countySubsistence[countyIndex], release);
                    DistributeRelief(
                        countyIndex, release, Math.Min(deficit, reliefNutrition),
                        householdNeed, householdConsumed,
                        householdPhysicalConsumed, false);
                    _countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations -= release;
                }
                records[countyIndex].LocalReliefMilliRations = release;
                _totalLocalRelief = checked(_totalLocalRelief + release);
                remainingDeficit[countyIndex] = CountyDeficit(
                    countyIndex, householdNeed, householdConsumed);
                transportCapacity[countyIndex] = checked(
                    countyCarriers[countyIndex] *
                    (long)_marketReliefProfile.TransportCapacityMilliRationsPerCarrier);
                transportCapacity[countyIndex] = AdjustFoodEcologyTransportCapacity(
                    _countySubsistence[countyIndex], transportCapacity[countyIndex]);
                long donorReserve = checked(
                    countyAlive[countyIndex] *
                    _marketReliefProfile.CountyGranaryReserveDays * 1_000L);
                eligibleReliefDonor[countyIndex] =
                    remainingDeficit[countyIndex] == 0 &&
                    _countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations >
                        donorReserve;
            }

            foreach (KeyValuePair<string, List<int>> commandery in
                _countiesByParent.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                for (var donorOrdinal = 0; donorOrdinal < commandery.Value.Count; donorOrdinal++)
                {
                    int donorIndex = commandery.Value[donorOrdinal];
                    if (!eligibleReliefDonor[donorIndex]) continue;
                    int routeCount = 0;
                    for (var recipientOrdinal = 0;
                        recipientOrdinal < commandery.Value.Count &&
                        routeCount < _marketReliefProfile.MaximumOutboundReliefRoutesPerCountyYear;
                        recipientOrdinal++)
                    {
                        int recipientIndex = commandery.Value[recipientOrdinal];
                        if (recipientIndex == donorIndex || remainingDeficit[recipientIndex] <= 0)
                            continue;
                        long reserve = checked(
                            countyAlive[donorIndex] *
                            _marketReliefProfile.CountyGranaryReserveDays * 1_000L);
                        long available = Math.Min(
                            Math.Max(0,
                                _countySubsistence[donorIndex].GovernmentGranaryFoodMilliRations -
                                reserve),
                            transportCapacity[donorIndex]);
                        if (available <= 0) break;
                        int deliveredBasisPoints = 10_000 -
                            _marketReliefProfile.TransportNaturalLossBasisPoints -
                            _marketReliefProfile.TransportProvisionBasisPoints;
                        long requested = checked(
                            (remainingDeficit[recipientIndex] * 10_000L +
                             deliveredBasisPoints - 1L) / deliveredBasisPoints);
                        long shipped = Math.Min(available, requested);
                        long loss = checked(shipped *
                            _marketReliefProfile.TransportNaturalLossBasisPoints / 10_000L);
                        long provisions = checked(shipped *
                            _marketReliefProfile.TransportProvisionBasisPoints / 10_000L);
                        long delivered = shipped - loss - provisions;
                        if (delivered <= 0) continue;
                        TrackTransportShipment(
                            _countySubsistence[donorIndex],
                            _countySubsistence[recipientIndex],
                            shipped, loss, provisions);
                        _countySubsistence[donorIndex].GovernmentGranaryFoodMilliRations -= shipped;
                        _countySubsistence[recipientIndex].GovernmentGranaryFoodMilliRations = checked(
                            _countySubsistence[recipientIndex].GovernmentGranaryFoodMilliRations + delivered);
                        transportCapacity[donorIndex] -= shipped;
                        remainingDeficit[recipientIndex] = Math.Max(
                            0, remainingDeficit[recipientIndex] - delivered);
                        outbound[donorIndex] = checked(outbound[donorIndex] + shipped);
                        inbound[recipientIndex] = checked(inbound[recipientIndex] + delivered);
                        transportLoss[donorIndex] = checked(transportLoss[donorIndex] + loss);
                        transportProvisions[donorIndex] = checked(
                            transportProvisions[donorIndex] + provisions);
                        _totalTransportShipped = checked(_totalTransportShipped + shipped);
                        _totalTransportDelivered = checked(_totalTransportDelivered + delivered);
                        _totalTransportLoss = checked(_totalTransportLoss + loss);
                        _totalTransportProvisions = checked(_totalTransportProvisions + provisions);
                        _reliefTransports.Add(new ReliefTransportRecord
                        {
                            YearIndex = yearIndex,
                            CalendarYear = OpeningCalendarYear + yearIndex,
                            SourceCountyId = _input.Counties[donorIndex].Id,
                            DestinationCountyId = _input.Counties[recipientIndex].Id,
                            ParentCommanderyId = commandery.Key,
                            ShippedMilliRations = shipped,
                            DeliveredMilliRations = delivered,
                            NaturalLossMilliRations = loss,
                            ProvisionsMilliRations = provisions,
                            CarrierCount = countyCarriers[donorIndex]
                        });
                        routeCount++;
                    }
                }
            }

            for (var countyIndex = 0; countyIndex < countyCount; countyIndex++)
            {
                long transportRelief = Math.Min(
                    inbound[countyIndex],
                    CountyDeficit(countyIndex, householdNeed, householdConsumed));
                if (transportRelief > 0)
                {
                    long reliefNutrition = TrackReliefConsumption(
                        _countySubsistence[countyIndex], transportRelief);
                    DistributeRelief(
                        countyIndex,
                        transportRelief,
                        Math.Min(
                            CountyDeficit(countyIndex, householdNeed, householdConsumed),
                            reliefNutrition),
                        householdNeed, householdConsumed,
                        householdPhysicalConsumed, true);
                    _countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations -=
                        transportRelief;
                }
                _totalTransportRelief = checked(_totalTransportRelief + transportRelief);
                FinalizeMarketCounty(
                    countyIndex, yearIndex, householdNeed, householdConsumed,
                    householdPhysicalConsumed,
                    inbound, outbound, transportLoss, transportProvisions,
                    transportRelief, countyDisease, records[countyIndex]);
            }

            ApplyPressureMortality(
                yearIndex, day, countyDisease, countyConflict, records);
            _countyResourceYears.AddRange(records);
            ValidateFoodProductProvenance();
        }

        private void TransferExtinctHouseholdAssets(long[] householdAlive)
        {
            for (var householdIndex = 0; householdIndex < _households.Count; householdIndex++)
            {
                if (householdAlive[householdIndex] != 0) continue;
                HouseholdRecord household = _households[householdIndex];
                TransferExtinctProductionAssets(household);
                CountySubsistenceState county = _countySubsistence[household.CountyIndex];
                TrackExtinctHouseholdFoodTransfer(household, county);
                county.GovernmentGranaryFoodMilliRations = checked(
                    county.GovernmentGranaryFoodMilliRations +
                    household.FoodInventoryMilliRations);
                county.GovernmentTreasuryCashMilli = checked(
                    county.GovernmentTreasuryCashMilli + household.CashMilli);
                household.FoodInventoryMilliRations = 0;
                household.CashMilli = 0;
            }
        }

        private long CountyFood(int countyIndex)
        {
            long total = _countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations;
            total = checked(total + CountyProductionSeed(countyIndex));
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
                total = checked(total + _households[indexes[h]].FoodInventoryMilliRations);
            return total;
        }

        private long ApplyStockLoss(
            int countyIndex,
            int basisPoints,
            FoodSinkKind sink)
        {
            long total = 0;
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
            {
                HouseholdRecord household = _households[indexes[h]];
                long loss = checked(
                    household.FoodInventoryMilliRations * basisPoints / 10_000L);
                household.FoodInventoryMilliRations -= loss;
                TrackHouseholdFoodRemoved(household, loss, sink);
                total = checked(total + loss);
            }
            CountySubsistenceState county = _countySubsistence[countyIndex];
            long granaryLoss = checked(
                county.GovernmentGranaryFoodMilliRations * basisPoints / 10_000L);
            county.GovernmentGranaryFoodMilliRations -= granaryLoss;
            TrackGovernmentFoodRemoved(county, granaryLoss, sink);
            return checked(total + granaryLoss +
                ApplyProductionStockLoss(countyIndex, basisPoints));
        }

        private void AllocateHarvest(
            int countyIndex,
            long[] householdWorkers,
            long householdHarvest,
            long grainTax)
        {
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            var weights = new long[indexes.Count];
            long totalWorkers = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                weights[h] = householdWorkers[indexes[h]];
                totalWorkers += weights[h];
            }
            if (totalWorkers == 0)
            {
                if (householdHarvest != 0 || grainTax != 0)
                    throw new InvalidOperationException("A county produced harvest without farm workers.");
                return;
            }
            long[] harvestShares = AllocateLocal(householdHarvest, weights, totalWorkers);
            long[] taxShares = AllocateLocal(grainTax, weights, totalWorkers);
            for (var h = 0; h < indexes.Count; h++)
            {
                HouseholdRecord household = _households[indexes[h]];
                household.FoodInventoryMilliRations = checked(
                    household.FoodInventoryMilliRations + harvestShares[h]);
                household.CumulativeTaxFoodMilliRations = checked(
                    household.CumulativeTaxFoodMilliRations + taxShares[h]);
            }
        }

        private void SettleCountyMarket(
            int countyIndex,
            long[] householdNeed,
            long[] householdConsumed,
            long[] householdPhysicalConsumed,
            AnnualCountyResourceRecord record)
        {
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            var offer = new long[indexes.Count];
            var demand = new long[indexes.Count];
            long totalOffer = 0;
            long totalDeficit = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                HouseholdRecord household = _households[index];
                long reserve = checked(
                    householdNeed[index] * _marketReliefProfile.HouseholdReserveTargetDays /
                    _profile.DaysPerYear);
                offer[h] = Math.Max(0, household.FoodInventoryMilliRations - reserve);
                demand[h] = householdNeed[index] - householdConsumed[index];
                totalOffer = checked(totalOffer + offer[h]);
                totalDeficit = checked(totalDeficit + demand[h]);
            }
            long rawPriceBasisPoints = totalOffer == 0
                ? _marketReliefProfile.MaximumPriceBasisPoints
                : Math.Min(int.MaxValue, checked(totalDeficit * 10_000L / totalOffer));
            int priceBasisPoints = checked((int)Math.Max(
                _marketReliefProfile.MinimumPriceBasisPoints,
                Math.Min(_marketReliefProfile.MaximumPriceBasisPoints,
                    rawPriceBasisPoints)));
            int price = Math.Max(1, checked(
                _marketReliefProfile.BasePriceCashMilliPerRation *
                priceBasisPoints / 10_000));
            price = AdjustFoodEcologyMarketPrice(countyIndex, price);
            _countySubsistence[countyIndex].LastMarketPriceCashMilliPerRation = price;
            long affordableDemand = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                long affordable = checked(_households[indexes[h]].CashMilli * 1_000L / price);
                demand[h] = Math.Min(demand[h], affordable);
                affordableDemand = checked(affordableDemand + demand[h]);
            }
            long traded = Math.Min(totalOffer, affordableDemand);
            long[] sold = AllocateLocal(traded, offer, totalOffer);
            long[] bought = AllocateLocal(traded, demand, affordableDemand);
            long cashPaid = 0;
            long marketNutrition = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                HouseholdRecord household = _households[indexes[h]];
                long cost = checked(bought[h] * price / 1_000L);
                if (cost > household.CashMilli || sold[h] > household.FoodInventoryMilliRations)
                    throw new InvalidOperationException("A county market exceeded household assets.");
                household.CashMilli -= cost;
                household.FoodInventoryMilliRations -= sold[h];
                marketNutrition = checked(
                    marketNutrition + TrackMarketSale(household, sold[h]));
                household.CumulativeMarketPurchasedFoodMilliRations = checked(
                    household.CumulativeMarketPurchasedFoodMilliRations + bought[h]);
                householdPhysicalConsumed[indexes[h]] = checked(
                    householdPhysicalConsumed[indexes[h]] + bought[h]);
                household.CumulativeMarketSoldFoodMilliRations = checked(
                    household.CumulativeMarketSoldFoodMilliRations + sold[h]);
                cashPaid = checked(cashPaid + cost);
            }
            long nutritionToAllocate = Math.Min(affordableDemand, marketNutrition);
            long[] nutritionBought = AllocateLocal(
                nutritionToAllocate, demand, affordableDemand);
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                householdConsumed[index] = checked(
                    householdConsumed[index] + nutritionBought[h]);
            }
            long[] sellerCash = AllocateLocal(cashPaid, sold, traded);
            for (var h = 0; h < indexes.Count; h++)
                _households[indexes[h]].CashMilli = checked(
                    _households[indexes[h]].CashMilli + sellerCash[h]);
            record.MarketTradeMilliRations = traded;
            record.MarketCashTransferredMilli = cashPaid;
            record.MarketPriceCashMilliPerRation = price;
            _totalMarketTrade = checked(_totalMarketTrade + traded);
            _totalMarketCashTransferred = checked(_totalMarketCashTransferred + cashPaid);
        }

        private long CountyDeficit(
            int countyIndex,
            long[] householdNeed,
            long[] householdConsumed)
        {
            long total = 0;
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                total = checked(total + householdNeed[index] - householdConsumed[index]);
            }
            return total;
        }

        private void DistributeRelief(
            int countyIndex,
            long physicalTotal,
            long nutritionTotal,
            long[] householdNeed,
            long[] householdConsumed,
            long[] householdPhysicalConsumed,
            bool transport)
        {
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            var deficits = new long[indexes.Count];
            long totalDeficit = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                deficits[h] = householdNeed[index] - householdConsumed[index];
                totalDeficit = checked(totalDeficit + deficits[h]);
            }
            if (nutritionTotal > totalDeficit)
                throw new InvalidOperationException("Relief exceeded household deficit.");
            long[] nutritionShares = AllocateLocal(
                nutritionTotal, deficits, totalDeficit);
            long[] physicalShares = AllocateLocal(
                physicalTotal, deficits, totalDeficit);
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                householdConsumed[index] = checked(
                    householdConsumed[index] + nutritionShares[h]);
                householdPhysicalConsumed[index] = checked(
                    householdPhysicalConsumed[index] + physicalShares[h]);
                if (transport)
                {
                    _households[index].CumulativeTransportReliefFoodMilliRations = checked(
                        _households[index].CumulativeTransportReliefFoodMilliRations +
                        physicalShares[h]);
                }
                else
                {
                    _households[index].CumulativeReliefFoodMilliRations = checked(
                        _households[index].CumulativeReliefFoodMilliRations +
                        physicalShares[h]);
                }
            }
        }

        private static long[] AllocateLocal(long total, long[] weights, long totalWeight)
        {
            var output = new long[weights.Length];
            if (total == 0) return output;
            if (total < 0 || totalWeight <= 0)
                throw new InvalidOperationException("Invalid pro-rata allocation.");
            long allocated = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0) continue;
                output[i] = checked(total * weights[i] / totalWeight);
                allocated = checked(allocated + output[i]);
            }
            long remainder = total - allocated;
            for (var i = 0; remainder > 0; i++)
            {
                int index = i % weights.Length;
                if (weights[index] <= 0) continue;
                output[index]++;
                remainder--;
            }
            return output;
        }

        private void FinalizeMarketCounty(
            int countyIndex,
            int yearIndex,
            long[] householdNeed,
            long[] householdConsumed,
            long[] householdPhysicalConsumed,
            long[] inbound,
            long[] outbound,
            long[] transportLoss,
            long[] transportProvisions,
            long transportRelief,
            bool[] countyDisease,
            AnnualCountyResourceRecord record)
        {
            long actual = 0;
            long physical = 0;
            long closingHouseholdFood = 0;
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
            {
                int index = indexes[h];
                HouseholdRecord household = _households[index];
                long unmet = householdNeed[index] - householdConsumed[index];
                household.LastFoodSatisfactionBasisPoints = householdNeed[index] == 0
                    ? 10_000
                    : checked((int)Math.Min(
                        10_000L, householdConsumed[index] * 10_000L / householdNeed[index]));
                household.CumulativeUnmetFoodMilliRations = checked(
                    household.CumulativeUnmetFoodMilliRations + unmet);
                actual = checked(actual + householdConsumed[index]);
                physical = checked(physical + householdPhysicalConsumed[index]);
                closingHouseholdFood = checked(
                    closingHouseholdFood + household.FoodInventoryMilliRations);
            }
            long need = record.HouseholdNeedMilliRations;
            int satisfaction = need == 0
                ? 10_000
                : checked((int)Math.Min(10_000L, actual * 10_000L / need));
            int diseaseProbability = Math.Min(
                10_000,
                _subsistenceProfile.DiseaseOutbreakBasisPoints +
                checked((int)((10_000L - satisfaction) *
                    _subsistenceProfile.ShortageDiseaseBonusBasisPoints / 10_000L)));
            bool disease = StableRandom.CheckBasisPoints(
                _options.Seed, 407UL, countyIndex, yearIndex, diseaseProbability);
            countyDisease[countyIndex] = disease;
            record.ActualConsumptionMilliRations = actual;
            record.PhysicalConsumptionMilliRations = physical;
            record.UnmetFoodMilliRations = need - actual;
            record.ClosingHouseholdFoodMilliRations = closingHouseholdFood;
            record.ClosingGranaryFoodMilliRations =
                _countySubsistence[countyIndex].GovernmentGranaryFoodMilliRations;
            long closingSeed = CountyProductionSeed(countyIndex);
            record.ClosingFoodMilliRations = checked(
                closingHouseholdFood + record.ClosingGranaryFoodMilliRations.Value +
                closingSeed);
            if (_householdProductionProfile != null)
            {
                record.ClosingHouseholdSeedMilliRations = indexes.Sum(
                    index => _households[index].SeedInventoryMilliRations);
                record.ClosingGovernmentSeedMilliRations =
                    _countySubsistence[countyIndex].GovernmentSeedInventoryMilliRations;
                record.AssignedOwnedLandMilliMu = indexes.Sum(
                    index => _households[index].OwnedArableLandMilliMu);
                record.AssignedLeasedLandMilliMu = indexes.Sum(
                    index => _households[index].LeasedArableLandMilliMu);
                record.PublicUnassignedLandMilliMu =
                    _countySubsistence[countyIndex].PublicArableLandMilliMu;
            }
            record.FoodSatisfactionBasisPoints = satisfaction;
            record.DiseaseOutbreak = disease;
            record.TransportReliefMilliRations = transportRelief;
            record.OutboundShippedMilliRations = outbound[countyIndex];
            record.InboundDeliveredMilliRations = inbound[countyIndex];
            record.TransportLossMilliRations = transportLoss[countyIndex];
            record.TransportProvisionsMilliRations = transportProvisions[countyIndex];
            _totalHouseholdNeed = checked(_totalHouseholdNeed + need);
            _totalActualConsumption = checked(_totalActualConsumption + actual);
            _totalPhysicalConsumption = checked(
                _totalPhysicalConsumption + physical);
            _totalUnmetFood = checked(_totalUnmetFood + need - actual);
        }

        private void ApplyPressureMortality(
            int yearIndex,
            int day,
            bool[] countyDisease,
            bool[] countyConflict,
            AnnualCountyResourceRecord[] records)
        {
            int peopleAtSettlement = _people.Count;
            for (var i = 0; i < peopleAtSettlement; i++)
            {
                PersonRecord person = _people[i];
                if (!person.Alive) continue;
                int countyIndex = person.CountyIndex;
                int age = AgeAt(person, day);
                int satisfaction = _households[checked((int)person.HouseholdId - 1)]
                    .LastFoodSatisfactionBasisPoints;
                int famineProbability = ApplyMortalityVulnerability(
                    checked((int)((long)(10_000 - satisfaction) *
                        _subsistenceProfile.MaximumFamineMortalityBasisPoints / 10_000L)),
                    age);
                if (StableRandom.CheckBasisPoints(
                    _options.Seed, 408UL, person.Id, yearIndex, famineProbability))
                {
                    KillPerson(day, person, DeathCause.Famine);
                    records[countyIndex].FamineDeaths++;
                    continue;
                }
                if (countyDisease[countyIndex])
                {
                    int diseaseProbability = ApplyMortalityVulnerability(
                        _subsistenceProfile.DiseaseMortalityBasisPoints, age);
                    if (StableRandom.CheckBasisPoints(
                        _options.Seed, 409UL, person.Id, yearIndex, diseaseProbability))
                    {
                        KillPerson(day, person, DeathCause.Disease);
                        records[countyIndex].DiseaseDeaths++;
                        continue;
                    }
                }
                if (countyConflict[countyIndex] && StableRandom.CheckBasisPoints(
                    _options.Seed, 410UL, person.Id, yearIndex,
                    _subsistenceProfile.ConflictMortalityBasisPoints))
                {
                    KillPerson(day, person, DeathCause.LocalConflict);
                    records[countyIndex].LocalConflictDeaths++;
                }
            }
        }

        private void SettleSubsistence(int yearIndex, int day)
        {
            int householdCount = _households.Count;
            var householdNeed = new long[householdCount];
            var householdConsumed = new long[householdCount];
            var countyNeed = new long[_input.Counties.Count];
            var countyWorkers = new long[_input.Counties.Count];
            var countyActual = new long[_input.Counties.Count];
            var countySatisfaction = new int[_input.Counties.Count];
            var countyDisease = new bool[_input.Counties.Count];
            var countyConflict = new bool[_input.Counties.Count];
            var currentRecords = new AnnualCountyResourceRecord[_input.Counties.Count];

            for (var i = 0; i < _people.Count; i++)
            {
                PersonRecord person = _people[i];
                if (!person.Alive) continue;
                int age = AgeAt(person, day);
                long need = AnnualFoodNeed(age);
                int householdIndex = checked((int)person.HouseholdId - 1);
                householdNeed[householdIndex] = checked(householdNeed[householdIndex] + need);
                countyNeed[person.CountyIndex] = checked(countyNeed[person.CountyIndex] + need);
                if (age >= _subsistenceProfile.MinimumWorkerAge &&
                    age <= _subsistenceProfile.MaximumWorkerAge &&
                    StableRandom.CheckBasisPoints(
                        _options.Seed,
                        401UL,
                        person.Id,
                        0,
                        _subsistenceProfile.AgriculturalWorkerBasisPoints))
                {
                    countyWorkers[person.CountyIndex]++;
                }
            }

            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                CountySubsistenceState county = _countySubsistence[countyIndex];
                long openingFood = county.FoodInventoryMilliRations;
                long cultivated = Math.Min(
                    county.ArableLandMilliMu,
                    checked(countyWorkers[countyIndex] *
                        _subsistenceProfile.LaborCapacityMilliMuPerWorker));
                bool severe = StableRandom.CheckBasisPoints(
                    _options.Seed,
                    402UL,
                    countyIndex,
                    yearIndex,
                    _subsistenceProfile.SevereHarvestEventBasisPoints);
                int weather = severe
                    ? StableRandom.Range(
                        _options.Seed,
                        403UL,
                        countyIndex,
                        yearIndex,
                        _subsistenceProfile.SevereHarvestMinBasisPoints,
                        _subsistenceProfile.SevereHarvestMaxBasisPoints)
                    : StableRandom.Range(
                        _options.Seed,
                        404UL,
                        countyIndex,
                        yearIndex,
                        _subsistenceProfile.OrdinaryWeatherMinBasisPoints,
                        _subsistenceProfile.OrdinaryWeatherMaxBasisPoints);
                long baseGross = checked(
                    cultivated * _subsistenceProfile.GrossYieldMilliRationsPerMu / 1_000L);
                long gross = checked(baseGross * weather / 10_000L);
                long fieldLoss = checked(
                    gross * _subsistenceProfile.FieldSeedLossBasisPoints / 10_000L);
                long spoilage = checked(
                    openingFood * _subsistenceProfile.AnnualStorageSpoilageBasisPoints / 10_000L);
                long available = checked(openingFood + gross - fieldLoss - spoilage);
                bool conflict = StableRandom.CheckBasisPoints(
                    _options.Seed,
                    405UL,
                    countyIndex,
                    yearIndex,
                    _subsistenceProfile.LocalConflictBasisPoints);
                long seizure = 0;
                if (conflict && available > 0)
                {
                    int seizureBasisPoints = StableRandom.Range(
                        _options.Seed,
                        406UL,
                        countyIndex,
                        yearIndex,
                        _subsistenceProfile.ConflictFoodSeizureMinBasisPoints,
                        _subsistenceProfile.ConflictFoodSeizureMaxBasisPoints);
                    seizure = checked(available * seizureBasisPoints / 10_000L);
                    available -= seizure;
                }
                long need = countyNeed[countyIndex];
                long actual = Math.Min(need, available);
                long unmet = need - actual;
                county.FoodInventoryMilliRations = available - actual;
                int satisfaction = need == 0
                    ? 10_000
                    : checked((int)Math.Min(10_000L, actual * 10_000L / need));
                int diseaseProbability = Math.Min(
                    10_000,
                    _subsistenceProfile.DiseaseOutbreakBasisPoints +
                    checked((int)((10_000L - satisfaction) *
                        _subsistenceProfile.ShortageDiseaseBonusBasisPoints / 10_000L)));
                bool disease = StableRandom.CheckBasisPoints(
                    _options.Seed,
                    407UL,
                    countyIndex,
                    yearIndex,
                    diseaseProbability);
                countyActual[countyIndex] = actual;
                countySatisfaction[countyIndex] = satisfaction;
                countyDisease[countyIndex] = disease;
                countyConflict[countyIndex] = conflict;
                currentRecords[countyIndex] = new AnnualCountyResourceRecord
                {
                    YearIndex = yearIndex,
                    CalendarYear = OpeningCalendarYear + yearIndex,
                    CountyId = _input.Counties[countyIndex].Id,
                    ArableLandMilliMu = county.ArableLandMilliMu,
                    AgriculturalWorkers = countyWorkers[countyIndex],
                    CultivatedLandMilliMu = cultivated,
                    OpeningFoodMilliRations = openingFood,
                    GrossHarvestMilliRations = gross,
                    FieldSeedLossMilliRations = fieldLoss,
                    StorageSpoilageMilliRations = spoilage,
                    ConflictSeizureMilliRations = seizure,
                    HouseholdNeedMilliRations = need,
                    ActualConsumptionMilliRations = actual,
                    UnmetFoodMilliRations = unmet,
                    ClosingFoodMilliRations = county.FoodInventoryMilliRations,
                    FoodSatisfactionBasisPoints = satisfaction,
                    WeatherBasisPoints = weather,
                    SevereHarvestEvent = severe,
                    DiseaseOutbreak = disease,
                    LocalConflict = conflict
                };
                _totalGrossHarvest = checked(_totalGrossHarvest + gross);
                _totalFieldSeedLoss = checked(_totalFieldSeedLoss + fieldLoss);
                _totalStorageSpoilage = checked(_totalStorageSpoilage + spoilage);
                _totalConflictSeizure = checked(_totalConflictSeizure + seizure);
                _totalHouseholdNeed = checked(_totalHouseholdNeed + need);
                _totalActualConsumption = checked(_totalActualConsumption + actual);
                _totalUnmetFood = checked(_totalUnmetFood + unmet);
            }

            var remainingActual = (long[])countyActual.Clone();
            for (var householdIndex = 0; householdIndex < householdCount; householdIndex++)
            {
                long need = householdNeed[householdIndex];
                if (need == 0) continue;
                int countyIndex = _households[householdIndex].CountyIndex;
                long countyTotalNeed = countyNeed[countyIndex];
                long consumed = checked(
                    need * countyActual[countyIndex] / countyTotalNeed);
                householdConsumed[householdIndex] = consumed;
                remainingActual[countyIndex] -= consumed;
            }
            for (var householdIndex = 0; householdIndex < householdCount; householdIndex++)
            {
                long need = householdNeed[householdIndex];
                HouseholdRecord household = _households[householdIndex];
                int countyIndex = household.CountyIndex;
                if (need > 0 && remainingActual[countyIndex] > 0)
                {
                    householdConsumed[householdIndex]++;
                    remainingActual[countyIndex]--;
                }
                long unmet = need - householdConsumed[householdIndex];
                household.LastFoodSatisfactionBasisPoints = need == 0
                    ? 10_000
                    : checked((int)Math.Min(
                        10_000L,
                        householdConsumed[householdIndex] * 10_000L / need));
                household.CumulativeUnmetFoodMilliRations = checked(
                    household.CumulativeUnmetFoodMilliRations + unmet);
            }
            if (remainingActual.Any(value => value != 0))
                throw new InvalidOperationException("Household food distribution did not conserve consumption.");

            int peopleAtSettlement = _people.Count;
            for (var i = 0; i < peopleAtSettlement; i++)
            {
                PersonRecord person = _people[i];
                if (!person.Alive) continue;
                int countyIndex = person.CountyIndex;
                int age = AgeAt(person, day);
                int satisfaction = _households[checked((int)person.HouseholdId - 1)]
                    .LastFoodSatisfactionBasisPoints;
                int shortage = 10_000 - satisfaction;
                int famineProbability = checked((int)(
                    (long)shortage * _subsistenceProfile.MaximumFamineMortalityBasisPoints /
                    10_000L));
                famineProbability = ApplyMortalityVulnerability(famineProbability, age);
                if (StableRandom.CheckBasisPoints(
                    _options.Seed, 408UL, person.Id, yearIndex, famineProbability))
                {
                    KillPerson(day, person, DeathCause.Famine);
                    currentRecords[countyIndex].FamineDeaths++;
                    continue;
                }
                if (countyDisease[countyIndex])
                {
                    int diseaseProbability = ApplyMortalityVulnerability(
                        _subsistenceProfile.DiseaseMortalityBasisPoints,
                        age);
                    if (StableRandom.CheckBasisPoints(
                        _options.Seed, 409UL, person.Id, yearIndex, diseaseProbability))
                    {
                        KillPerson(day, person, DeathCause.Disease);
                        currentRecords[countyIndex].DiseaseDeaths++;
                        continue;
                    }
                }
                if (countyConflict[countyIndex] && StableRandom.CheckBasisPoints(
                    _options.Seed,
                    410UL,
                    person.Id,
                    yearIndex,
                    _subsistenceProfile.ConflictMortalityBasisPoints))
                {
                    KillPerson(day, person, DeathCause.LocalConflict);
                    currentRecords[countyIndex].LocalConflictDeaths++;
                }
            }
            _countyResourceYears.AddRange(currentRecords);
        }

        private long AnnualFoodNeed(int age)
        {
            ConsumptionBand band = _subsistenceProfile.AgeConsumptionBands.First(item =>
                age >= item.MinimumAge && age <= item.MaximumAge);
            return checked(
                (long)_profile.DaysPerYear * 1_000L *
                band.AdultRationBasisPoints / 10_000L);
        }

        private static int ApplyMortalityVulnerability(int probability, int age)
        {
            int multiplier = age < 5
                ? 15_000
                : age < 15
                    ? 12_000
                    : age >= 60
                        ? 13_000
                        : 8_000;
            return checked((int)Math.Min(
                10_000L,
                (long)probability * multiplier / 10_000L));
        }

        private int ChooseInitialAge(long personId, int countyIndex)
        {
            int pick = StableRandom.Range(
                _options.Seed, 105UL, personId, countyIndex, 0, 10_000);
            var cumulative = 0;
            for (var i = 0; i < _profile.InitialAgeBands.Count; i++)
            {
                AgeWeightBand band = _profile.InitialAgeBands[i];
                cumulative += band.WeightBasisPoints;
                if (pick < cumulative)
                {
                    return band.MinimumAge + StableRandom.Range(
                        _options.Seed,
                        106UL,
                        personId,
                        countyIndex,
                        0,
                        band.MaximumAge - band.MinimumAge + 1);
                }
            }
            return _profile.InitialAgeBands[_profile.InitialAgeBands.Count - 1].MaximumAge;
        }

        private void ProcessDay(int day)
        {
            _currentDay = day;
            List<ScheduledEvent> events = _calendar[day];
            if (events != null)
            {
                events.Sort((left, right) =>
                {
                    int type = left.Type.CompareTo(right.Type);
                    return type != 0 ? type : left.PersonId.CompareTo(right.PersonId);
                });
                for (var i = 0; i < events.Count; i++)
                {
                    _processedEvents++;
                    _yearProcessed++;
                    switch (events[i].Type)
                    {
                        case ScheduledEventType.Death:
                            ResolveDeath(day, events[i].PersonId);
                            break;
                        case ScheduledEventType.MarriageReady:
                            ResolveMarriageReady(events[i].PersonId);
                            break;
                        case ScheduledEventType.FertilityCheck:
                            ResolveFertility(day, events[i].PersonId);
                            break;
                        default:
                            throw new InvalidOperationException("Unknown demographic event type.");
                    }
                }
                _calendar[day] = null;
            }
            PairDirtyCounties(day, false);
        }

        private void ResolveDeath(int day, long personId)
        {
            PersonRecord person = GetPerson(personId);
            if (!person.Alive) return;
            KillPerson(day, person, DeathCause.Natural);
        }

        private void KillPerson(int day, PersonRecord person, DeathCause cause)
        {
            if (!person.Alive) return;
            person.Alive = false;
            person.DeathDay = day;
            person.DeathCause = cause;
            _living--;
            _totalDeaths++;
            _yearDeaths++;
            if (cause == DeathCause.Famine)
            {
                _totalFamineDeaths++;
                _yearFamineDeaths++;
            }
            else if (cause == DeathCause.Disease)
            {
                _totalDiseaseDeaths++;
                _yearDiseaseDeaths++;
            }
            else if (cause == DeathCause.LocalConflict)
            {
                _totalLocalConflictDeaths++;
                _yearLocalConflictDeaths++;
            }
            RemoveReady(person);
            long spouseId = person.SpousePersonId;
            person.SpousePersonId = 0;
            if (spouseId > 0)
            {
                PersonRecord spouse = GetPerson(spouseId);
                if (spouse.Alive && spouse.SpousePersonId == person.Id)
                {
                    spouse.SpousePersonId = 0;
                    int readyDay = checked(day + _profile.RemarriageDelayDays);
                    if (readyDay <= _totalDays)
                    {
                        Schedule(readyDay, ScheduledEventType.MarriageReady, spouse.Id);
                    }
                }
            }
            _eventWriter.Write(
                LifeEventType.Death,
                day,
                person.Id,
                spouseId,
                person.HouseholdId,
                person.CountyIndex);
            if (cause != DeathCause.Natural && _pressureEventWriter != null)
            {
                _pressureEventWriter.Write(
                    cause,
                    day,
                    person.Id,
                    person.HouseholdId,
                    person.CountyIndex);
            }
        }

        private void ResolveMarriageReady(long personId)
        {
            PersonRecord person = GetPerson(personId);
            if (IsMarriageEligible(person)) AddReady(person);
        }

        private void ResolveFertility(int day, long motherId)
        {
            PersonRecord mother = GetPerson(motherId);
            if (!mother.Alive || mother.Gender != Gender.Female) return;
            int age = AgeAt(mother, day);
            if (age < _profile.MinimumMarriageAgeFemale ||
                age > _profile.MaximumMarriageAgeFemale)
            {
                return;
            }
            PersonRecord father = mother.SpousePersonId > 0
                ? GetPerson(mother.SpousePersonId)
                : null;
            if (father != null && father.Alive &&
                father.CountyIndex == mother.CountyIndex &&
                (mother.LastChildbirthDay < 0 ||
                 day - mother.LastChildbirthDay >= _profile.MinimumChildbirthSpacingDays))
            {
                int probability = FertilityProbability(age);
                int parityPenalty = Math.Max(0, mother.ChildrenCount - 4) * 800;
                probability = Math.Max(0, probability - parityPenalty);
                if (_subsistenceProfile != null)
                {
                    int satisfaction = _households[checked((int)mother.HouseholdId - 1)]
                        .LastFoodSatisfactionBasisPoints;
                    probability = satisfaction <
                        _subsistenceProfile.FertilityZeroBelowFoodBasisPoints
                        ? 0
                        : checked((int)((long)probability * satisfaction / 10_000L));
                }
                if (StableRandom.CheckBasisPoints(
                        _options.Seed,
                        201UL,
                        mother.Id,
                        day,
                        probability))
                {
                    CreateBirth(day, mother, father);
                }
            }
            int next = checked(day + _profile.DaysPerYear);
            if (next <= _totalDays && AgeAt(mother, next) <=
                _profile.MaximumMarriageAgeFemale)
            {
                Schedule(next, ScheduledEventType.FertilityCheck, mother.Id);
            }
        }

        private void CreateBirth(int day, PersonRecord mother, PersonRecord father)
        {
            long childId = _people.Count + 1L;
            var child = new PersonRecord
            {
                Id = childId,
                HouseholdId = mother.HouseholdId,
                FatherPersonId = father.Id,
                MotherPersonId = mother.Id,
                BirthDay = day,
                CountyIndex = mother.CountyIndex,
                Gender = StableRandom.CheckBasisPoints(
                    _options.Seed,
                    202UL,
                    childId,
                    day,
                    _profile.FemaleBasisPoints)
                    ? Gender.Female
                    : Gender.Male
            };
            _people.Add(child);
            mother.LastChildbirthDay = day;
            mother.ChildrenCount++;
            father.ChildrenCount++;
            _living++;
            _peakLiving = Math.Max(_peakLiving, _living);
            _totalBirths++;
            _yearBirths++;
            ScheduleDeath(child, day);
            int readyDay = MarriageReadyDay(child, day);
            Schedule(readyDay, ScheduledEventType.MarriageReady, child.Id);
            _eventWriter.Write(
                LifeEventType.Birth,
                day,
                child.Id,
                mother.Id,
                child.HouseholdId,
                child.CountyIndex);
        }

        private void PairDirtyCounties(int day, bool opening)
        {
            if (_dirtyMarriageCounties.Count == 0) return;
            int[] counties = _dirtyMarriageCounties.OrderBy(item => item).ToArray();
            _dirtyMarriageCounties.Clear();
            for (var i = 0; i < counties.Length; i++)
            {
                PairCounty(counties[i], day, opening);
            }
        }

        private void PairCounty(int countyIndex, int day, bool opening)
        {
            CleanReadySet(_readyMen[countyIndex]);
            CleanReadySet(_readyWomen[countyIndex]);
            while (_readyMen[countyIndex].Count > 0 &&
                   _readyWomen[countyIndex].Count > 0)
            {
                long selectedMan = 0;
                long selectedWoman = 0;
                var inspectedWomen = 0;
                foreach (long womanId in _readyWomen[countyIndex])
                {
                    PersonRecord woman = GetPerson(womanId);
                    foreach (long manId in _readyMen[countyIndex])
                    {
                        PersonRecord man = GetPerson(manId);
                        if (CanMarry(man, woman))
                        {
                            selectedMan = manId;
                            selectedWoman = womanId;
                            break;
                        }
                    }
                    if (selectedMan != 0 || ++inspectedWomen >= 64) break;
                }
                if (selectedMan == 0) break;
                _readyMen[countyIndex].Remove(selectedMan);
                _readyWomen[countyIndex].Remove(selectedWoman);
                CreateMarriage(day, GetPerson(selectedMan), GetPerson(selectedWoman), opening);
            }
        }

        private void CreateMarriage(
            int day,
            PersonRecord man,
            PersonRecord woman,
            bool opening)
        {
            HouseholdRecord manHousehold = _households[checked((int)man.HouseholdId - 1)];
            HouseholdRecord womanHousehold = _households[checked((int)woman.HouseholdId - 1)];
            long householdId = ++_nextHouseholdId;
            _households.Add(new HouseholdRecord
            {
                Id = householdId,
                CountyIndex = man.CountyIndex,
                FoundedDay = day,
                LastFoodSatisfactionBasisPoints = Math.Min(
                    manHousehold.LastFoodSatisfactionBasisPoints,
                    womanHousehold.LastFoodSatisfactionBasisPoints)
            });
            _householdIndexesByCounty[man.CountyIndex].Add(_households.Count - 1);
            if (_marketReliefProfile != null)
            {
                long manFood = checked(
                    manHousehold.FoodInventoryMilliRations *
                    _marketReliefProfile.NewHouseholdAssetTransferBasisPoints / 10_000L);
                long womanFood = checked(
                    womanHousehold.FoodInventoryMilliRations *
                    _marketReliefProfile.NewHouseholdAssetTransferBasisPoints / 10_000L);
                long manCash = checked(
                    manHousehold.CashMilli *
                    _marketReliefProfile.NewHouseholdAssetTransferBasisPoints / 10_000L);
                long womanCash = checked(
                    womanHousehold.CashMilli *
                    _marketReliefProfile.NewHouseholdAssetTransferBasisPoints / 10_000L);
                manHousehold.FoodInventoryMilliRations -= manFood;
                womanHousehold.FoodInventoryMilliRations -= womanFood;
                manHousehold.CashMilli -= manCash;
                womanHousehold.CashMilli -= womanCash;
                HouseholdRecord newHousehold = _households[_households.Count - 1];
                newHousehold.FoodInventoryMilliRations = checked(manFood + womanFood);
                TrackMarriageFoodTransfer(
                    manHousehold, newHousehold, manFood);
                TrackMarriageFoodTransfer(
                    womanHousehold, newHousehold, womanFood);
                newHousehold.CashMilli = checked(manCash + womanCash);
                TransferProductionAssetsOnMarriage(
                    manHousehold, womanHousehold, newHousehold);
            }
            man.SpousePersonId = woman.Id;
            woman.SpousePersonId = man.Id;
            man.HouseholdId = householdId;
            woman.HouseholdId = householdId;
            _totalMarriages++;
            if (!opening) _yearMarriages++;
            _eventWriter.Write(
                LifeEventType.Marriage,
                day,
                woman.Id,
                man.Id,
                householdId,
                man.CountyIndex);
            ScheduleFirstFertility(woman, day);
        }

        private void AddReady(PersonRecord person)
        {
            if (!IsMarriageEligible(person)) return;
            if (person.Gender == Gender.Female)
                _readyWomen[person.CountyIndex].Add(person.Id);
            else
                _readyMen[person.CountyIndex].Add(person.Id);
            _dirtyMarriageCounties.Add(person.CountyIndex);
        }

        private void RemoveReady(PersonRecord person)
        {
            if (person.Gender == Gender.Female)
                _readyWomen[person.CountyIndex].Remove(person.Id);
            else
                _readyMen[person.CountyIndex].Remove(person.Id);
        }

        private void CleanReadySet(SortedSet<long> set)
        {
            long[] invalid = set.Where(id => !IsMarriageEligible(GetPerson(id))).ToArray();
            for (var i = 0; i < invalid.Length; i++) set.Remove(invalid[i]);
        }

        private bool IsMarriageEligible(PersonRecord person)
        {
            if (!person.Alive || person.SpousePersonId != 0) return false;
            int age = AgeAt(person, CurrentProcessingDay());
            return person.Gender == Gender.Female
                ? age >= _profile.MinimumMarriageAgeFemale &&
                  age <= _profile.MaximumMarriageAgeFemale
                : age >= _profile.MinimumMarriageAgeMale &&
                  age <= _profile.MaximumMarriageAgeMale;
        }

        private int _currentDay;

        private int CurrentProcessingDay()
        {
            return _currentDay;
        }

        private bool CanMarry(PersonRecord man, PersonRecord woman)
        {
            return man.Alive && woman.Alive &&
                man.Gender == Gender.Male && woman.Gender == Gender.Female &&
                man.SpousePersonId == 0 && woman.SpousePersonId == 0 &&
                man.CountyIndex == woman.CountyIndex &&
                man.HouseholdId != woman.HouseholdId &&
                man.Id != woman.FatherPersonId &&
                woman.Id != man.FatherPersonId &&
                man.Id != woman.MotherPersonId &&
                woman.Id != man.MotherPersonId &&
                !(man.FatherPersonId != 0 &&
                  man.FatherPersonId == woman.FatherPersonId) &&
                !(man.MotherPersonId != 0 &&
                  man.MotherPersonId == woman.MotherPersonId);
        }

        private int MarriageReadyDay(PersonRecord person, int currentDay)
        {
            int minimumAge = person.Gender == Gender.Female
                ? _profile.MinimumMarriageAgeFemale
                : _profile.MinimumMarriageAgeMale;
            int day = checked(person.BirthDay + minimumAge * _profile.DaysPerYear);
            return Math.Max(currentDay, day);
        }

        private void ScheduleFirstFertility(PersonRecord mother, int day)
        {
            int delay = 1 + StableRandom.Range(
                _options.Seed,
                203UL,
                mother.Id,
                day,
                0,
                _profile.DaysPerYear);
            int due = checked(day + delay);
            if (due <= _totalDays &&
                AgeAt(mother, due) <= _profile.MaximumMarriageAgeFemale)
            {
                Schedule(due, ScheduledEventType.FertilityCheck, mother.Id);
            }
        }

        private void ScheduleDeath(PersonRecord person, int currentDay)
        {
            int currentAge = AgeAt(person, currentDay);
            for (var age = currentAge; age <= _profile.MaximumAgeYears; age++)
            {
                int probability = MortalityProbability(age);
                if (!StableRandom.CheckBasisPoints(
                        _options.Seed,
                        301UL,
                        person.Id,
                        age,
                        probability))
                {
                    continue;
                }
                int intervalStart = Math.Max(
                    currentDay + 1,
                    checked(person.BirthDay + age * _profile.DaysPerYear));
                int intervalEnd = checked(
                    person.BirthDay + (age + 1) * _profile.DaysPerYear - 1);
                if (intervalEnd < intervalStart) continue;
                int day = intervalStart + StableRandom.Range(
                    _options.Seed,
                    302UL,
                    person.Id,
                    age,
                    0,
                    intervalEnd - intervalStart + 1);
                Schedule(day, ScheduledEventType.Death, person.Id);
                return;
            }
            int forced = checked(
                person.BirthDay + (_profile.MaximumAgeYears + 1) *
                _profile.DaysPerYear);
            Schedule(Math.Max(currentDay + 1, forced), ScheduledEventType.Death, person.Id);
        }

        private void Schedule(int day, ScheduledEventType type, long personId)
        {
            if (day < 0 || day > _totalDays) return;
            if (_calendar[day] == null)
                _calendar[day] = new List<ScheduledEvent>();
            _calendar[day].Add(new ScheduledEvent
            {
                Type = type,
                PersonId = personId
            });
        }

        private int FertilityProbability(int age)
        {
            ProbabilityBand band = _profile.FertilityBands.FirstOrDefault(item =>
                age >= item.MinimumAge && age <= item.MaximumAge);
            return band == null ? 0 : band.AnnualProbabilityBasisPoints;
        }

        private int MortalityProbability(int age)
        {
            ProbabilityBand band = _profile.MortalityBands.FirstOrDefault(item =>
                age >= item.MinimumAge && age <= item.MaximumAge);
            return band == null ? 10_000 : band.AnnualProbabilityBasisPoints;
        }

        private int AgeAt(PersonRecord person, int day)
        {
            return Math.Max(0, (day - person.BirthDay) / _profile.DaysPerYear);
        }

        private PersonRecord GetPerson(long id)
        {
            if (id <= 0 || id > _people.Count)
                throw new InvalidOperationException("Unknown person ID " + id);
            return _people[checked((int)id - 1)];
        }

        private void CompleteYear(int yearIndex)
        {
            long expected = _yearOpeningLiving + _yearBirths - _yearDeaths;
            if (_living != expected)
            {
                throw new InvalidOperationException("Annual population conservation failed.");
            }
            _years.Add(new AnnualPopulationRecord
            {
                YearIndex = yearIndex,
                CalendarYear = OpeningCalendarYear + yearIndex,
                OpeningLiving = _yearOpeningLiving,
                Births = _yearBirths,
                Deaths = _yearDeaths,
                Marriages = _yearMarriages,
                ClosingLiving = _living,
                CumulativePeople = _people.Count,
                ProcessedEvents = _yearProcessed,
                FamineDeaths = _subsistenceProfile == null ? (long?)null : _yearFamineDeaths,
                DiseaseDeaths = _subsistenceProfile == null ? (long?)null : _yearDiseaseDeaths,
                LocalConflictDeaths = _subsistenceProfile == null
                    ? (long?)null
                    : _yearLocalConflictDeaths
            });
            WriteProgress("year_completed", yearIndex);
            _yearOpeningLiving = _living;
            _yearBirths = 0;
            _yearDeaths = 0;
            _yearMarriages = 0;
            _yearProcessed = 0;
            _yearFamineDeaths = 0;
            _yearDiseaseDeaths = 0;
            _yearLocalConflictDeaths = 0;
        }

        private void ValidateWorld()
        {
            if (_years.Count != _options.Years ||
                _people.Count != _options.InitialLivingPopulation + _totalBirths ||
                _living != _people.Count - _totalDeaths ||
                _eventWriter != null ||
                _pressureEventWriter != null)
            {
                throw new InvalidOperationException("Final demographic conservation failed.");
            }
            long living = 0;
            long deaths = 0;
            for (var index = 0; index < _people.Count; index++)
            {
                PersonRecord person = _people[index];
                if (person.Id != index + 1L ||
                    person.CountyIndex < 0 || person.CountyIndex >= _input.Counties.Count ||
                    person.HouseholdId <= 0 || person.HouseholdId > _households.Count)
                {
                    throw new InvalidOperationException("A permanent person core is invalid.");
                }
                if (person.Alive)
                {
                    living++;
                    if (person.DeathDay >= 0 || person.DeathCause != DeathCause.None)
                        throw new InvalidOperationException("A living person has a death day.");
                }
                else
                {
                    deaths++;
                    if (person.DeathDay < 0 || person.DeathDay > _totalDays ||
                        person.DeathCause == DeathCause.None)
                        throw new InvalidOperationException("A deceased person has an invalid death day.");
                }
                ValidateParent(person, person.FatherPersonId);
                ValidateParent(person, person.MotherPersonId);
                if (person.SpousePersonId > 0)
                {
                    PersonRecord spouse = GetPerson(person.SpousePersonId);
                    if (!person.Alive || !spouse.Alive || spouse.SpousePersonId != person.Id ||
                        spouse.CountyIndex != person.CountyIndex)
                    {
                        throw new InvalidOperationException("A current spouse reference is invalid.");
                    }
                }
            }
            if (living != _living || deaths != _totalDeaths ||
                _eventWriterCountAtClose != _totalBirths + _totalDeaths + _totalMarriages)
            {
                throw new InvalidOperationException("Permanent and event counts do not reconcile.");
            }
            if (_subsistenceProfile != null)
            {
                long pressureDeaths = checked(
                    _totalFamineDeaths + _totalDiseaseDeaths + _totalLocalConflictDeaths);
                long finalFood = TotalFoodInventory();
                long expectedFinalFood = checked(
                    _openingFood + _totalGrossHarvest - _totalFieldSeedLoss -
                    _totalStorageSpoilage - _totalConflictSeizure -
                    (_marketReliefProfile == null
                        ? _totalActualConsumption
                        : _totalPhysicalConsumption) - _totalTransportLoss -
                    _totalTransportProvisions);
                if (_pressureWriterCountAtClose != pressureDeaths ||
                    finalFood != expectedFinalFood || finalFood < 0 ||
                    _countyResourceYears.Count != _options.Years * _input.Counties.Count ||
                    _households.Sum(item => item.CumulativeUnmetFoodMilliRations) !=
                        _totalUnmetFood)
                {
                    throw new InvalidOperationException("Subsistence totals do not reconcile.");
                }
                for (var i = 0; i < _countyResourceYears.Count; i++)
                {
                    AnnualCountyResourceRecord record = _countyResourceYears[i];
                    long expectedCountyFood = checked(
                        record.OpeningFoodMilliRations + record.GrossHarvestMilliRations -
                        record.FieldSeedLossMilliRations -
                        record.StorageSpoilageMilliRations -
                        record.ConflictSeizureMilliRations -
                        (record.PhysicalConsumptionMilliRations ??
                            record.ActualConsumptionMilliRations));
                    if (_marketReliefProfile != null)
                    {
                        expectedCountyFood = checked(
                            expectedCountyFood - record.OutboundShippedMilliRations.Value +
                            record.InboundDeliveredMilliRations.Value);
                    }
                    if (record.ArableLandMilliMu < 0 ||
                        record.CultivatedLandMilliMu < 0 ||
                        record.CultivatedLandMilliMu > record.ArableLandMilliMu ||
                        record.ClosingFoodMilliRations < 0 ||
                        record.ActualConsumptionMilliRations < 0 ||
                        record.ActualConsumptionMilliRations > record.HouseholdNeedMilliRations ||
                        record.SeasonalPublicLandCultivatedMilliMu.HasValue &&
                        (record.SeasonalPublicLandCultivatedMilliMu.Value < 0 ||
                         record.SeasonalPublicLandCultivatedMilliMu.Value >
                            (record.PublicUnassignedLandMilliMu ?? 0)) ||
                        record.ClosingFoodMilliRations != expectedCountyFood)
                    {
                        throw new InvalidOperationException(string.Format(
                            CultureInfo.InvariantCulture,
                            "A county subsistence record is invalid: county={0} year={1} opening={2} gross={3} field={4} spoilage={5} seizure={6} consumption={7} outbound={8} inbound={9} expected={10} actual={11}.",
                            record.CountyId,
                            record.YearIndex,
                            record.OpeningFoodMilliRations,
                            record.GrossHarvestMilliRations,
                            record.FieldSeedLossMilliRations,
                            record.StorageSpoilageMilliRations,
                            record.ConflictSeizureMilliRations,
                            record.ActualConsumptionMilliRations,
                            record.OutboundShippedMilliRations ?? 0,
                            record.InboundDeliveredMilliRations ?? 0,
                            expectedCountyFood,
                            record.ClosingFoodMilliRations));
                    }
                }
                if (_marketReliefProfile != null)
                {
                    long finalCash = checked(
                        _households.Sum(item => item.CashMilli) +
                        _countySubsistence.Sum(item => item.GovernmentTreasuryCashMilli));
                    if (finalCash != _openingCash ||
                        _households.Any(item =>
                            item.CashMilli < 0 || item.FoodInventoryMilliRations < 0) ||
                        _countySubsistence.Any(item =>
                            item.GovernmentGranaryFoodMilliRations < 0 ||
                            item.GovernmentTreasuryCashMilli < 0) ||
                        _totalTransportShipped != checked(
                            _totalTransportDelivered + _totalTransportLoss +
                            _totalTransportProvisions) ||
                        _households.Sum(item =>
                            item.CumulativeMarketPurchasedFoodMilliRations) !=
                            _totalMarketTrade ||
                        _households.Sum(item =>
                            item.CumulativeMarketSoldFoodMilliRations) !=
                            _totalMarketTrade ||
                        _households.Sum(item => item.CumulativeTaxFoodMilliRations) !=
                            _totalGrainTax ||
                        _households.Sum(item => item.CumulativeReliefFoodMilliRations) !=
                            _totalLocalRelief ||
                        _households.Sum(item =>
                            item.CumulativeTransportReliefFoodMilliRations) !=
                            _totalTransportRelief)
                    {
                        throw new InvalidOperationException(
                            "Household market and relief totals do not reconcile.");
                    }
                    for (var i = 0; i < _reliefTransports.Count; i++)
                    {
                        ReliefTransportRecord transport = _reliefTransports[i];
                        CountyPlan source = _input.Counties.First(item =>
                            item.Id == transport.SourceCountyId);
                        CountyPlan destination = _input.Counties.First(item =>
                            item.Id == transport.DestinationCountyId);
                        if (source.ParentId != destination.ParentId ||
                            source.ParentId != transport.ParentCommanderyId ||
                            transport.ShippedMilliRations != checked(
                                transport.DeliveredMilliRations +
                                transport.NaturalLossMilliRations +
                                transport.ProvisionsMilliRations))
                        {
                            throw new InvalidOperationException(
                                "A relief transport record is invalid.");
                        }
                    }
                }
            }
            ValidateHouseholdProduction();
        }

        private long _eventWriterCountAtClose;
        private long _pressureWriterCountAtClose;

        private void ValidateParent(PersonRecord child, long parentId)
        {
            if (parentId == 0) return;
            if (parentId >= child.Id)
                throw new InvalidOperationException("A parent ID must predate the child ID.");
            PersonRecord parent = GetPerson(parentId);
            if (parent.BirthDay > child.BirthDay - 15 * _profile.DaysPerYear ||
                parent.DeathDay >= 0 && parent.DeathDay < child.BirthDay)
            {
                throw new InvalidOperationException("A birth has an invalid parent timeline.");
            }
        }

        private void WritePeople(string path)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4D323450);
                writer.Write(_subsistenceProfile == null ? 1 : 2);
                writer.Write((long)_people.Count);
                for (var i = 0; i < _people.Count; i++)
                {
                    PersonRecord value = _people[i];
                    writer.Write(value.Id);
                    writer.Write(value.HouseholdId);
                    writer.Write(value.SpousePersonId);
                    writer.Write(value.FatherPersonId);
                    writer.Write(value.MotherPersonId);
                    writer.Write(value.BirthDay);
                    writer.Write(value.DeathDay);
                    writer.Write(value.CountyIndex);
                    writer.Write(value.LastChildbirthDay);
                    writer.Write(value.ChildrenCount);
                    writer.Write((byte)value.Gender);
                    writer.Write(value.Alive ? (byte)1 : (byte)0);
                    if (_subsistenceProfile != null)
                        writer.Write((byte)value.DeathCause);
                }
                stream.Flush(true);
            }
        }

        private void WriteHouseholds(string path)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4D323448);
                writer.Write(1);
                writer.Write((long)_households.Count);
                for (var i = 0; i < _households.Count; i++)
                {
                    writer.Write(_households[i].Id);
                    writer.Write(_households[i].CountyIndex);
                    writer.Write(_households[i].FoundedDay);
                }
                stream.Flush(true);
            }
        }

        private void WriteHouseholdSubsistence(string path)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4D323453);
                writer.Write(1);
                writer.Write((long)_households.Count);
                for (var i = 0; i < _households.Count; i++)
                {
                    HouseholdRecord household = _households[i];
                    writer.Write(household.Id);
                    writer.Write(household.LastFoodSatisfactionBasisPoints);
                    writer.Write(household.CumulativeUnmetFoodMilliRations);
                }
                stream.Flush(true);
            }
        }

        private void WriteHouseholdEconomy(string path)
        {
            using (var stream = new FileStream(
                path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4D323459);
                writer.Write(1);
                writer.Write((long)_households.Count);
                for (var i = 0; i < _households.Count; i++)
                {
                    HouseholdRecord household = _households[i];
                    writer.Write(household.Id);
                    writer.Write(household.FoodInventoryMilliRations);
                    writer.Write(household.CashMilli);
                    writer.Write(household.CumulativeMarketPurchasedFoodMilliRations);
                    writer.Write(household.CumulativeMarketSoldFoodMilliRations);
                    writer.Write(household.CumulativeTaxFoodMilliRations);
                    writer.Write(household.CumulativeReliefFoodMilliRations);
                    writer.Write(household.CumulativeTransportReliefFoodMilliRations);
                    writer.Write(household.CumulativeUnmetFoodMilliRations);
                    writer.Write(household.LastFoodSatisfactionBasisPoints);
                }
                stream.Flush(true);
            }
        }

        private void ValidatePhysicalFiles(
            string corePath,
            string householdsPath,
            string eventsPath,
            string householdSubsistencePath,
            string pressureEventsPath,
            string householdEconomyPath,
            string householdProductionPath,
            string farmWorkOrdersPath)
        {
            ValidateFile(
                corePath,
                0x4D323450,
                _subsistenceProfile == null ? 1 : 2,
                _people.Count,
                CoreHeaderBytes,
                _subsistenceProfile == null ? CoreRecordBytes : SubsistenceCoreRecordBytes);
            ValidateFile(
                householdsPath, 0x4D323448, 1, _households.Count,
                HouseholdHeaderBytes, HouseholdRecordBytes);
            ValidateFile(
                eventsPath, 0x4D323445, 1, _eventWriterCountAtClose,
                EventHeaderBytes, EventRecordBytes);
            if (_subsistenceProfile != null)
            {
                ValidateFile(
                    householdSubsistencePath,
                    0x4D323453,
                    1,
                    _households.Count,
                    HouseholdHeaderBytes,
                    HouseholdSubsistenceRecordBytes);
                ValidateFile(
                    pressureEventsPath,
                    0x4D323458,
                    1,
                    _pressureWriterCountAtClose,
                    EventHeaderBytes,
                    PressureEventRecordBytes);
            }
            if (_marketReliefProfile != null)
            {
                ValidateFile(
                    householdEconomyPath,
                    0x4D323459,
                    1,
                    _households.Count,
                    HouseholdHeaderBytes,
                    HouseholdEconomyRecordBytes);
            }
            if (_householdProductionProfile != null)
            {
                ValidateFile(
                    householdProductionPath,
                    0x4D323457,
                    1,
                    _households.Count,
                    HouseholdHeaderBytes,
                    HouseholdProductionRecordBytes);
                ValidateFile(
                    farmWorkOrdersPath,
                    0x4D32345A,
                    1,
                    _farmWorkOrderCountAtClose,
                    EventHeaderBytes,
                    FarmWorkOrderRecordBytes);
            }
        }

        private static void ValidateFile(
            string path,
            int magic,
            int version,
            long count,
            int headerBytes,
            int recordBytes)
        {
            var info = new FileInfo(path);
            if (info.Length != headerBytes + count * recordBytes)
                throw new InvalidDataException("A demographic binary file has an invalid length.");
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (reader.ReadInt32() != magic || reader.ReadInt32() != version ||
                    reader.ReadInt64() != count)
                {
                    throw new InvalidDataException("A demographic binary file header is invalid.");
                }
            }
        }

        private string BuildCountyDigest()
        {
            var living = new long[_input.Counties.Count];
            for (var i = 0; i < _people.Count; i++)
            {
                if (_people[i].Alive) living[_people[i].CountyIndex]++;
            }
            var builder = new StringBuilder();
            for (var i = 0; i < living.Length; i++)
            {
                builder.Append(_input.Counties[i].Id).Append('|')
                    .Append(living[i].ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            return HashBytes(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private string BuildSubsistenceDigest()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _countySubsistence.Length; i++)
            {
                builder.Append(_input.Counties[i].Id).Append('|')
                    .Append(_countySubsistence[i].ArableLandMilliMu.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(_countySubsistence[i].FoodInventoryMilliRations.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            builder.Append("pressure|")
                .Append(_totalFamineDeaths.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(_totalDiseaseDeaths.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(_totalLocalConflictDeaths.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(_totalUnmetFood.ToString(CultureInfo.InvariantCulture));
            return HashBytes(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private long TotalFoodInventory()
        {
            if (_marketReliefProfile == null)
                return _countySubsistence.Sum(item => item.FoodInventoryMilliRations);
            return checked(
                _households.Sum(item => item.FoodInventoryMilliRations) +
                _countySubsistence.Sum(item => item.GovernmentGranaryFoodMilliRations) +
                (_householdProductionProfile == null
                    ? 0L
                    : _households.Sum(item => item.SeedInventoryMilliRations) +
                      _countySubsistence.Sum(item =>
                          item.GovernmentSeedInventoryMilliRations)));
        }

        private string BuildMarketReliefDigest()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _households.Count; i++)
            {
                HouseholdRecord household = _households[i];
                builder.Append(household.Id.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.FoodInventoryMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.CashMilli.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.CumulativeMarketPurchasedFoodMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.CumulativeMarketSoldFoodMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.CumulativeReliefFoodMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(household.CumulativeTransportReliefFoodMilliRations.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            for (var i = 0; i < _countySubsistence.Length; i++)
            {
                CountySubsistenceState county = _countySubsistence[i];
                builder.Append(_input.Counties[i].Id).Append('|')
                    .Append(county.GovernmentGranaryFoodMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(county.GovernmentTreasuryCashMilli.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(county.LastMarketPriceCashMilliPerRation.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            for (var i = 0; i < _reliefTransports.Count; i++)
            {
                ReliefTransportRecord transport = _reliefTransports[i];
                builder.Append(transport.YearIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(transport.SourceCountyId).Append('|')
                    .Append(transport.DestinationCountyId).Append('|')
                    .Append(transport.ShippedMilliRations.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(transport.DeliveredMilliRations.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            return HashBytes(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static FileEvidence FileInfo(string path)
        {
            var info = new System.IO.FileInfo(path);
            return new FileEvidence
            {
                Path = path,
                Bytes = info.Length,
                Sha256 = HashFile(path)
            };
        }

        private static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return Hex(sha.ComputeHash(stream));
            }
        }

        private static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
        }

        private static string Hex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }

        private static void RewritePathsAfterMove(WorldEvidence value, string generationPath)
        {
            value.PermanentCore.Path = Path.Combine(generationPath, "permanent-people.bin");
            value.Households.Path = Path.Combine(generationPath, "households.bin");
            value.LifeEvents.Path = Path.Combine(generationPath, "life-events.bin");
            value.AnnualLedger.Path = Path.Combine(generationPath, "annual-population.json");
            if (value.HouseholdSubsistence != null)
                value.HouseholdSubsistence.Path = Path.Combine(generationPath, "household-subsistence.bin");
            if (value.PressureEvents != null)
                value.PressureEvents.Path = Path.Combine(generationPath, "pressure-events.bin");
            if (value.AnnualCountyResources != null)
                value.AnnualCountyResources.Path = Path.Combine(generationPath, "annual-county-resources.json");
            if (value.HouseholdEconomy != null)
                value.HouseholdEconomy.Path = Path.Combine(generationPath, "household-economy.bin");
            if (value.ReliefTransports != null)
                value.ReliefTransports.Path = Path.Combine(generationPath, "relief-transports.json");
            if (value.HouseholdProduction != null)
                value.HouseholdProduction.Path = Path.Combine(generationPath, "household-production.bin");
            if (value.FarmWorkOrders != null)
                value.FarmWorkOrders.Path = Path.Combine(generationPath, "farm-work-orders.bin");
            if (value.FormalInventoryBatches != null)
                value.FormalInventoryBatches.Path = Path.Combine(
                    generationPath, "formal-inventory-batches.bin");
            if (value.FormalInventoryTransactions != null)
                value.FormalInventoryTransactions.Path = Path.Combine(
                    generationPath, "formal-inventory-transactions.bin");
            if (value.FoodProductProvenance != null)
                value.FoodProductProvenance.Path = Path.Combine(
                    generationPath, "food-product-provenance.json");
            if (value.FoodEcology != null)
                value.FoodEcology.Path = Path.Combine(
                    generationPath, "food-ecology.json");
        }

        private void WriteProgress(string phase, int year)
        {
            if (string.IsNullOrWhiteSpace(_options.ProgressPath)) return;
            JsonFile.Write(_options.ProgressPath, new
            {
                phase,
                year,
                living = _living,
                cumulative = _people.Count,
                births = _totalBirths,
                deaths = _totalDeaths,
                marriages = _totalMarriages,
                food_inventory_milli_rations = _countySubsistence == null
                    ? (long?)null
                    : TotalFoodInventory(),
                unmet_food_milli_rations = _subsistenceProfile == null
                    ? (long?)null
                    : _totalUnmetFood,
                pressure_deaths = _subsistenceProfile == null
                    ? (long?)null
                    : _totalFamineDeaths + _totalDiseaseDeaths + _totalLocalConflictDeaths
            });
        }

        private sealed class PressureEventWriter : IDisposable
        {
            private readonly FileStream _stream;
            private readonly BinaryWriter _writer;
            public long Count { get; private set; }

            public PressureEventWriter(string path)
            {
                _stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1 << 20);
                _writer = new BinaryWriter(_stream, Encoding.UTF8);
                _writer.Write(0x4D323458);
                _writer.Write(1);
                _writer.Write(0L);
            }

            public void Write(
                DeathCause cause,
                int day,
                long personId,
                long householdId,
                int countyIndex)
            {
                _writer.Write((byte)cause);
                _writer.Write(day);
                _writer.Write(personId);
                _writer.Write(householdId);
                _writer.Write(countyIndex);
                Count++;
            }

            public void Dispose()
            {
                _writer.Flush();
                _stream.Position = 8;
                _writer.Write(Count);
                _writer.Flush();
                _stream.Flush(true);
                _writer.Dispose();
                _stream.Dispose();
            }
        }

        private sealed class LifeEventWriter : IDisposable
        {
            private readonly FileStream _stream;
            private readonly BinaryWriter _writer;
            public long Count { get; private set; }

            public LifeEventWriter(string path)
            {
                _stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1 << 20);
                _writer = new BinaryWriter(_stream, Encoding.UTF8);
                _writer.Write(0x4D323445);
                _writer.Write(1);
                _writer.Write(0L);
            }

            public void Write(
                LifeEventType type,
                int day,
                long personId,
                long relatedPersonId,
                long householdId,
                int countyIndex)
            {
                _writer.Write((byte)type);
                _writer.Write(day);
                _writer.Write(personId);
                _writer.Write(relatedPersonId);
                _writer.Write(householdId);
                _writer.Write(countyIndex);
                Count++;
            }

            public void Dispose()
            {
                _writer.Flush();
                _stream.Position = 8;
                _writer.Write(Count);
                _writer.Flush();
                _stream.Flush(true);
                _writer.Dispose();
                _stream.Dispose();
            }
        }
    }

    internal static class StableRandom
    {
        public static bool CheckBasisPoints(
            ulong seed,
            ulong stream,
            long entity,
            long coordinate,
            int basisPoints)
        {
            if (basisPoints <= 0) return false;
            if (basisPoints >= 10_000) return true;
            return Hash(seed, stream, entity, coordinate) % 10_000UL < (ulong)basisPoints;
        }

        public static int Range(
            ulong seed,
            ulong stream,
            long entity,
            long coordinate,
            int minimum,
            int maximumExclusive)
        {
            if (maximumExclusive <= minimum) throw new ArgumentOutOfRangeException("maximumExclusive");
            return minimum + (int)(Hash(seed, stream, entity, coordinate) %
                (ulong)(maximumExclusive - minimum));
        }

        private static ulong Hash(ulong seed, ulong stream, long entity, long coordinate)
        {
            ulong value = seed ^ (stream * 0x9E3779B97F4A7C15UL);
            value ^= unchecked((ulong)entity) * 0xBF58476D1CE4E5B9UL;
            value ^= unchecked((ulong)coordinate) * 0x94D049BB133111EBUL;
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    internal static class JsonFile
    {
        public static void Write(string path, object value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(value, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }

    internal static class SelfTests
    {
        public static void Run(WorldOptions options)
        {
            var tests = new List<string>();
            string root = Path.Combine(
                Path.GetTempPath(),
                "mandate-m24-p0-self-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                WorldEvidence first = RunSmall(options, Path.Combine(root, "first"), 77UL);
                WorldEvidence second = RunSmall(options, Path.Combine(root, "second"), 77UL);
                True(first.InitialLivingPopulation == 10_000, "opening_count");
                tests.Add("opening_count");
                True(first.YearsSimulated == 10 && first.YearlyPopulation.Count == 10, "ten_years");
                tests.Add("ten_years");
                True(first.CumulativePersonCount == 10_000 + first.TotalBirths, "cumulative_conservation");
                tests.Add("cumulative_conservation");
                True(first.FinalLivingPopulation == first.CumulativePersonCount - first.TotalDeaths, "living_conservation");
                tests.Add("living_conservation");
                True(first.TotalBirths > 0 && first.TotalDeaths > 0 && first.TotalMarriages > 0, "natural_events_occur");
                tests.Add("natural_events_occur");
                True(first.PermanentCore.Sha256 == second.PermanentCore.Sha256, "core_determinism");
                tests.Add("core_determinism");
                True(first.LifeEvents.Sha256 == second.LifeEvents.Sha256, "event_determinism");
                tests.Add("event_determinism");
                True(first.CountyFinalLivingDigest == second.CountyFinalLivingDigest, "county_determinism");
                tests.Add("county_determinism");
                if (options.HasSubsistencePressure)
                {
                    long expectedFinalFood = checked(
                        first.OpeningFoodMilliRations.Value +
                        first.TotalGrossHarvestMilliRations.Value -
                        first.TotalFieldSeedLossMilliRations.Value -
                        first.TotalStorageSpoilageMilliRations.Value -
                        first.TotalConflictSeizureMilliRations.Value -
                        first.TotalPhysicalConsumptionMilliRations.Value -
                        (first.TotalTransportLossMilliRations ?? 0) -
                        (first.TotalTransportProvisionsMilliRations ?? 0));
                    True(first.FinalFoodMilliRations.Value == expectedFinalFood,
                        "food_conservation");
                    tests.Add("food_conservation");
                    True(first.TotalActualConsumptionMilliRations.Value <=
                        first.TotalHouseholdNeedMilliRations.Value,
                        "consumption_bounded_by_need");
                    tests.Add("consumption_bounded_by_need");
                    True(first.HouseholdSubsistence.Sha256 == second.HouseholdSubsistence.Sha256,
                        "household_subsistence_determinism");
                    tests.Add("household_subsistence_determinism");
                    True(first.PressureEvents.Sha256 == second.PressureEvents.Sha256,
                        "pressure_event_determinism");
                    tests.Add("pressure_event_determinism");
                    True(first.SubsistenceDigest == second.SubsistenceDigest,
                        "subsistence_digest_determinism");
                    tests.Add("subsistence_digest_determinism");
                }
                if (options.HasHouseholdMarketRelief)
                {
                    True(first.OpeningCashMilli.Value == checked(
                        first.FinalHouseholdCashMilli.Value +
                        first.FinalGovernmentCashMilli.Value),
                        "cash_conservation");
                    tests.Add("cash_conservation");
                    True(first.TotalTransportShippedMilliRations.Value == checked(
                        first.TotalTransportDeliveredMilliRations.Value +
                        first.TotalTransportLossMilliRations.Value +
                        first.TotalTransportProvisionsMilliRations.Value),
                        "transport_conservation");
                    tests.Add("transport_conservation");
                    True(first.TotalMarketTradeMilliRations.Value >= 0 &&
                        first.TotalLocalReliefMilliRations.Value >= 0 &&
                        first.TotalTransportReliefMilliRations.Value >= 0,
                        "market_relief_totals_nonnegative");
                    tests.Add("market_relief_totals_nonnegative");
                    True(first.HouseholdEconomy.Sha256 == second.HouseholdEconomy.Sha256,
                        "household_economy_determinism");
                    tests.Add("household_economy_determinism");
                    True(first.ReliefTransports.Sha256 == second.ReliefTransports.Sha256,
                        "relief_transport_determinism");
                    tests.Add("relief_transport_determinism");
                    True(first.MarketReliefDigest == second.MarketReliefDigest,
                        "market_relief_digest_determinism");
                    tests.Add("market_relief_digest_determinism");
                }
                if (options.HasHouseholdProduction)
                {
                    True(!string.IsNullOrWhiteSpace(first.ProductionContentPackageId) &&
                        first.AgriculturalBindingCount.Value > 0,
                        "production_content_binding_loaded");
                    tests.Add("production_content_binding_loaded");
                    True(first.TotalFarmWorkOrders.Value > 0,
                        "farm_work_orders_occur");
                    tests.Add("farm_work_orders_occur");
                    True(first.TotalSeedConsumedMilliRations.Value >= 0 &&
                        first.TotalSeedRetainedMilliRations.Value >= 0 &&
                        first.FinalSeedInventoryMilliRations.Value >= 0,
                        "seed_totals_nonnegative");
                    tests.Add("seed_totals_nonnegative");
                    True(first.HouseholdProduction.Sha256 ==
                        second.HouseholdProduction.Sha256,
                        "household_production_determinism");
                    tests.Add("household_production_determinism");
                    True(first.FarmWorkOrders.Sha256 == second.FarmWorkOrders.Sha256,
                        "farm_work_order_determinism");
                    tests.Add("farm_work_order_determinism");
                    True(first.HouseholdProductionDigest ==
                        second.HouseholdProductionDigest,
                        "household_production_digest_determinism");
                    tests.Add("household_production_digest_determinism");
                    True(first.PopulationResourceFeedback.Count == 10 &&
                        first.PopulationResourceFeedbackDigest ==
                        second.PopulationResourceFeedbackDigest,
                        "population_resource_feedback_determinism");
                    tests.Add("population_resource_feedback_determinism");
                    if (!options.HasPopulationResourceCalibration)
                    {
                        True(first.PopulationResourceFeedback.Any(item =>
                            item.Bottleneck != "none"),
                            "population_resource_bottleneck_detected");
                        tests.Add("population_resource_bottleneck_detected");
                    }
                }
                if (options.HasPopulationResourceCalibration)
                {
                    True(first.PopulationResourceCalibrationProfileId != null &&
                        first.CalibrationPassed.HasValue &&
                        first.CalibrationFailures != null,
                        "calibration_evaluation_emitted");
                    tests.Add("calibration_evaluation_emitted");
                    True(first.PopulationResourceFeedback.Any(item =>
                        item.SeasonalPublicLandCultivatedMilliMu > 0),
                        "seasonal_public_land_reused");
                    tests.Add("seasonal_public_land_reused");
                }
                if (options.HasFormalInventoryBridge)
                {
                    True(first.FormalInventoryBatchCount.Value > 0 &&
                        first.FormalInventoryBatchCount ==
                            first.FormalInventoryTransactionCount,
                        "formal_inventory_record_counts_match");
                    tests.Add("formal_inventory_record_counts_match");
                    True(first.FormalInventoryBatchQuantity == checked(
                            first.FormalInventorySourceFood.Value +
                            first.FormalInventorySourceSeed.Value) &&
                        first.FormalInventorySourceBalanceDelta ==
                            -first.FormalInventoryBatchQuantity,
                        "formal_inventory_replaces_compact_balances");
                    tests.Add("formal_inventory_replaces_compact_balances");
                    True(first.FormalInventoryBatches.Sha256 ==
                            second.FormalInventoryBatches.Sha256 &&
                        first.FormalInventoryTransactions.Sha256 ==
                            second.FormalInventoryTransactions.Sha256 &&
                        first.FormalInventoryDigest == second.FormalInventoryDigest,
                        "formal_inventory_bridge_determinism");
                    tests.Add("formal_inventory_bridge_determinism");
                }
                if (options.HasFoodProductProvenance)
                {
                    True(first.FoodProductCount.Value >= 2,
                        "multiple_food_products_tracked");
                    tests.Add("multiple_food_products_tracked");
                    True(first.FoodProductConservationTotal.Value ==
                        first.FormalInventorySourceFood.Value,
                        "food_products_match_formal_bridge_source");
                    tests.Add("food_products_match_formal_bridge_source");
                    True(first.FoodProductProvenance.Sha256 ==
                            second.FoodProductProvenance.Sha256 &&
                        first.FoodProductProvenanceDigest ==
                            second.FoodProductProvenanceDigest,
                        "food_product_provenance_determinism");
                    tests.Add("food_product_provenance_determinism");
                }
                if (options.HasFoodEcology)
                {
                    True(first.AgriculturalBindingCount.Value >= 5 &&
                        first.FoodProductCount.Value >= 6,
                        "food_ecology_loads_multiple_crops_and_products");
                    tests.Add("food_ecology_loads_multiple_crops_and_products");
                    True(first.FoodEcologyRotationAdjustedWorkOrders.Value > 0,
                        "food_ecology_rotation_affects_harvest");
                    tests.Add("food_ecology_rotation_affects_harvest");
                    True(first.FoodEcologyProcessedQuantity.Value > 0,
                        "food_ecology_processing_occurs");
                    tests.Add("food_ecology_processing_occurs");
                    True(first.FoodEcologyConsumedNutrition.Value > 0,
                        "food_ecology_nutrition_is_consumed");
                    tests.Add("food_ecology_nutrition_is_consumed");
                    True(first.FoodEcology.Sha256 == second.FoodEcology.Sha256 &&
                        first.FoodEcologyDigest == second.FoodEcologyDigest,
                        "food_ecology_determinism");
                    tests.Add("food_ecology_determinism");
                }
                JsonFile.Write(options.OutputPath, new SelfTestEvidence
                {
                    Status = "passed",
                    SchemaVersion = options.HasFoodEcology
                        ? "m24.p7.self-test.v1"
                        : options.HasFoodProductProvenance
                        ? "m24.p6.self-test.v1"
                        : options.HasFormalInventoryBridge
                        ? "m24.p5.self-test.v1"
                        : options.HasPopulationResourceCalibration
                        ? "m24.p4.self-test.v1"
                        : options.HasHouseholdProduction
                        ? "m24.p3.self-test.v1"
                        : options.HasHouseholdMarketRelief
                        ? "m24.p2.self-test.v1"
                        : options.HasSubsistencePressure
                            ? "m24.p1.self-test.v1"
                            : "m24.p0.self-test.v1",
                    Passed = tests.Count,
                    Failed = 0,
                    Tests = tests
                });
                Console.WriteLine(
                    "RESULT {0}-self-test=passed passed={1} failed=0",
                    options.HasFoodEcology
                        ? "m24-p7"
                        : options.HasFoodProductProvenance
                        ? "m24-p6"
                        : options.HasFormalInventoryBridge
                        ? "m24-p5"
                        : options.HasPopulationResourceCalibration
                        ? "m24-p4"
                        : options.HasHouseholdProduction
                        ? "m24-p3"
                        : options.HasHouseholdMarketRelief
                        ? "m24-p2"
                        : options.HasSubsistencePressure ? "m24-p1" : "m24-p0",
                    tests.Count);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static WorldEvidence RunSmall(WorldOptions source, string workspace, ulong seed)
        {
            var args = new List<string>
            {
                "--workspace", workspace,
                "--output", Path.Combine(workspace, "result.json"),
                "--profile", source.ProfilePath,
                "--m12-input", source.M12InputPath,
                "--audit", source.AuditPath,
                "--administrative-units", source.AdministrativeUnitsPath,
                "--initial-living", "10000",
                "--years", "10",
                "--seed", seed.ToString(CultureInfo.InvariantCulture)
            };
            if (source.HasSubsistencePressure)
            {
                args.Add("--subsistence-pressure-profile");
                args.Add(source.SubsistencePressureProfilePath);
            }
            if (source.HasHouseholdMarketRelief)
            {
                args.Add("--household-market-relief-profile");
                args.Add(source.HouseholdMarketReliefProfilePath);
            }
            if (source.HasHouseholdProduction)
            {
                args.Add("--household-production-profile");
                args.Add(source.HouseholdProductionProfilePath);
                args.Add("--production-content");
                args.Add(source.ProductionContentPath);
            }
            if (source.HasPopulationResourceCalibration)
            {
                args.Add("--population-resource-calibration-profile");
                args.Add(source.PopulationResourceCalibrationProfilePath);
            }
            if (source.HasFormalInventoryBridge)
            {
                args.Add("--formal-inventory-bridge-profile");
                args.Add(source.FormalInventoryBridgeProfilePath);
            }
            if (source.HasFoodProductProvenance)
            {
                args.Add("--food-product-provenance-profile");
                args.Add(source.FoodProductProvenanceProfilePath);
            }
            if (source.HasFoodEcology)
            {
                args.Add("--food-ecology-profile");
                args.Add(source.FoodEcologyProfilePath);
                args.Add("--food-content-extension");
                args.Add(source.FoodContentExtensionPath);
            }
            return DemographicWorldRunner.Run(WorldOptions.Parse(args.ToArray()));
        }

        private static void True(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("Self-test failed: " + name);
        }
    }
}
