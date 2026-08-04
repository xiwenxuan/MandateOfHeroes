using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal sealed class PopulationResourceCalibrationProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("enable_seasonal_public_land_leasing")] public bool EnableSeasonalPublicLandLeasing { get; set; }
        [JsonProperty("minimum_final_population_basis_points")] public int MinimumFinalPopulationBasisPoints { get; set; }
        [JsonProperty("maximum_final_population_basis_points")] public int MaximumFinalPopulationBasisPoints { get; set; }
        [JsonProperty("minimum_five_year_average_food_satisfaction_basis_points")] public int MinimumFiveYearAverageFoodSatisfactionBasisPoints { get; set; }
        [JsonProperty("maximum_five_year_average_famine_death_basis_points")] public int MaximumFiveYearAverageFamineDeathBasisPoints { get; set; }
        [JsonProperty("low_cultivation_basis_points")] public int LowCultivationBasisPoints { get; set; }
        [JsonProperty("high_unused_public_land_basis_points")] public int HighUnusedPublicLandBasisPoints { get; set; }

        public static PopulationResourceCalibrationProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<PopulationResourceCalibrationProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.population-resource-calibration-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                !PopulationRatio(value.MinimumFinalPopulationBasisPoints) ||
                !PopulationRatio(value.MaximumFinalPopulationBasisPoints) ||
                value.MaximumFinalPopulationBasisPoints < value.MinimumFinalPopulationBasisPoints ||
                !BasisPoints(value.MinimumFiveYearAverageFoodSatisfactionBasisPoints) ||
                !BasisPoints(value.MaximumFiveYearAverageFamineDeathBasisPoints) ||
                !BasisPoints(value.LowCultivationBasisPoints) ||
                !BasisPoints(value.HighUnusedPublicLandBasisPoints))
            {
                throw new InvalidDataException("The population resource calibration profile is invalid.");
            }
            return value;
        }

        private static bool BasisPoints(int value)
        {
            return value >= 0 && value <= 10_000;
        }

        private static bool PopulationRatio(int value)
        {
            return value >= 0 && value <= 50_000;
        }
    }

    internal sealed class AnnualPopulationResourceFeedback
    {
        [JsonProperty("year_index")] public int YearIndex { get; set; }
        [JsonProperty("calendar_year")] public int CalendarYear { get; set; }
        [JsonProperty("opening_living")] public long OpeningLiving { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("deaths")] public long Deaths { get; set; }
        [JsonProperty("famine_deaths")] public long FamineDeaths { get; set; }
        [JsonProperty("closing_living")] public long ClosingLiving { get; set; }
        [JsonProperty("net_population_change_basis_points")] public int NetPopulationChangeBasisPoints { get; set; }
        [JsonProperty("famine_death_basis_points")] public int FamineDeathBasisPoints { get; set; }
        [JsonProperty("food_satisfaction_basis_points")] public int FoodSatisfactionBasisPoints { get; set; }
        [JsonProperty("gross_harvest_to_need_basis_points")] public int GrossHarvestToNeedBasisPoints { get; set; }
        [JsonProperty("cultivated_land_basis_points")] public int CultivatedLandBasisPoints { get; set; }
        [JsonProperty("public_land_basis_points")] public int PublicLandBasisPoints { get; set; }
        [JsonProperty("unused_public_land_basis_points")] public int UnusedPublicLandBasisPoints { get; set; }
        [JsonProperty("labor_capacity_basis_points")] public int LaborCapacityBasisPoints { get; set; }
        [JsonProperty("closing_seed_coverage_basis_points")] public int ClosingSeedCoverageBasisPoints { get; set; }
        [JsonProperty("seasonal_public_land_cultivated_milli_mu")] public long SeasonalPublicLandCultivatedMilliMu { get; set; }
        [JsonProperty("bottleneck")] public string Bottleneck { get; set; }
    }

    internal sealed partial class AnnualCountyResourceRecord
    {
        [JsonProperty("seasonal_public_land_cultivated_milli_mu", NullValueHandling = NullValueHandling.Ignore)]
        public long? SeasonalPublicLandCultivatedMilliMu { get; set; }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("population_resource_calibration_profile_id", NullValueHandling = NullValueHandling.Ignore)]
        public string PopulationResourceCalibrationProfileId { get; set; }
        [JsonProperty("population_resource_feedback", NullValueHandling = NullValueHandling.Ignore)]
        public List<AnnualPopulationResourceFeedback> PopulationResourceFeedback { get; set; }
        [JsonProperty("population_resource_feedback_digest", NullValueHandling = NullValueHandling.Ignore)]
        public string PopulationResourceFeedbackDigest { get; set; }
        [JsonProperty("calibration_passed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CalibrationPassed { get; set; }
        [JsonProperty("calibration_failures", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> CalibrationFailures { get; set; }
    }

    internal sealed partial class DemographicWorldRunner
    {
        private PopulationResourceCalibrationProfile _populationResourceCalibrationProfile;

        public void ConfigurePopulationResourceCalibration(
            PopulationResourceCalibrationProfile profile)
        {
            _populationResourceCalibrationProfile = profile;
        }

        private long[] AllocateSeasonalPublicLand(
            int countyIndex,
            long[] householdWorkers)
        {
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            var allocation = new long[indexes.Count];
            if (_populationResourceCalibrationProfile == null ||
                !_populationResourceCalibrationProfile.EnableSeasonalPublicLandLeasing ||
                _countySubsistence[countyIndex].PublicArableLandMilliMu <= 0)
            {
                return allocation;
            }
            var spareCapacity = new long[indexes.Count];
            long totalSpare = 0;
            for (var h = 0; h < indexes.Count; h++)
            {
                HouseholdRecord household = _households[indexes[h]];
                long laborCapacity = checked(
                    householdWorkers[indexes[h]] *
                    _subsistenceProfile.LaborCapacityMilliMuPerWorker);
                long controlledLand = checked(
                    household.OwnedArableLandMilliMu +
                    household.LeasedArableLandMilliMu);
                spareCapacity[h] = Math.Max(0, laborCapacity - controlledLand);
                totalSpare = checked(totalSpare + spareCapacity[h]);
            }
            long total = Math.Min(
                _countySubsistence[countyIndex].PublicArableLandMilliMu,
                totalSpare);
            return AllocateLocal(total, spareCapacity, totalSpare);
        }

        private List<AnnualPopulationResourceFeedback> BuildPopulationResourceFeedback()
        {
            if (_householdProductionProfile == null) return null;
            long minimumSeedNumerator = _productionContent.Bindings.Min(item =>
                checked(item.SeedQuantity * 1_000_000L / item.HarvestQuantity));
            var output = new List<AnnualPopulationResourceFeedback>(_years.Count);
            for (var yearIndex = 1; yearIndex <= _years.Count; yearIndex++)
            {
                List<AnnualCountyResourceRecord> records = _countyResourceYears
                    .Where(item => item.YearIndex == yearIndex).ToList();
                AnnualPopulationRecord population = _years[yearIndex - 1];
                long land = records.Sum(item => item.ArableLandMilliMu);
                long cultivated = records.Sum(item => item.CultivatedLandMilliMu);
                long publicLand = records.Sum(item => item.PublicUnassignedLandMilliMu ?? 0);
                long seasonalPublicLand = records.Sum(item =>
                    item.SeasonalPublicLandCultivatedMilliMu ?? 0);
                long workers = records.Sum(item => item.AgriculturalWorkers);
                long need = records.Sum(item => item.HouseholdNeedMilliRations);
                long consumed = records.Sum(item => item.ActualConsumptionMilliRations);
                long gross = records.Sum(item => item.GrossHarvestMilliRations);
                long closingSeed = records.Sum(item =>
                    (item.ClosingHouseholdSeedMilliRations ?? 0) +
                    (item.ClosingGovernmentSeedMilliRations ?? 0));
                long theoreticalSeed = checked(
                    land * _subsistenceProfile.GrossYieldMilliRationsPerMu / 1_000L *
                    minimumSeedNumerator / 1_000_000L);
                int satisfaction = RatioBasisPoints(consumed, need);
                int cultivation = RatioBasisPoints(cultivated, land);
                int publicLandBasisPoints = RatioBasisPoints(publicLand, land);
                int unusedPublic = RatioBasisPoints(
                    Math.Max(0, publicLand - seasonalPublicLand), land);
                int laborCapacity = RatioBasisPoints(checked(
                    workers * _subsistenceProfile.LaborCapacityMilliMuPerWorker), land);
                int seedCoverage = RatioBasisPoints(closingSeed, theoreticalSeed);
                output.Add(new AnnualPopulationResourceFeedback
                {
                    YearIndex = yearIndex,
                    CalendarYear = population.CalendarYear,
                    OpeningLiving = population.OpeningLiving,
                    Births = population.Births,
                    Deaths = population.Deaths,
                    FamineDeaths = population.FamineDeaths ?? 0,
                    ClosingLiving = population.ClosingLiving,
                    NetPopulationChangeBasisPoints = SignedRatioBasisPoints(
                        population.Births - population.Deaths,
                        population.OpeningLiving),
                    FamineDeathBasisPoints = RatioBasisPoints(
                        population.FamineDeaths ?? 0,
                        population.OpeningLiving),
                    FoodSatisfactionBasisPoints = satisfaction,
                    GrossHarvestToNeedBasisPoints = RatioBasisPoints(gross, need),
                    CultivatedLandBasisPoints = cultivation,
                    PublicLandBasisPoints = publicLandBasisPoints,
                    UnusedPublicLandBasisPoints = unusedPublic,
                    LaborCapacityBasisPoints = laborCapacity,
                    ClosingSeedCoverageBasisPoints = seedCoverage,
                    SeasonalPublicLandCultivatedMilliMu = seasonalPublicLand,
                    Bottleneck = ClassifyBottleneck(
                        satisfaction, cultivation, unusedPublic,
                        laborCapacity, seedCoverage, RatioBasisPoints(gross, need))
                });
            }
            return output;
        }

        private string ClassifyBottleneck(
            int satisfaction,
            int cultivation,
            int unusedPublic,
            int laborCapacity,
            int seedCoverage,
            int grossToNeed)
        {
            int lowCultivation = _populationResourceCalibrationProfile == null
                ? 6_000
                : _populationResourceCalibrationProfile.LowCultivationBasisPoints;
            int highPublic = _populationResourceCalibrationProfile == null
                ? 1_500
                : _populationResourceCalibrationProfile.HighUnusedPublicLandBasisPoints;
            if (satisfaction >= 8_500) return "none";
            if (cultivation < lowCultivation && unusedPublic >= highPublic)
                return "unused_public_land";
            if (cultivation < lowCultivation && seedCoverage < lowCultivation)
                return "seed_access_or_household_fragmentation";
            if (cultivation < lowCultivation && laborCapacity < lowCultivation)
                return "agricultural_labor_capacity";
            if (grossToNeed < 10_000) return "harvest_capacity";
            return "distribution_or_household_access";
        }

        private void EvaluateCalibration(WorldEvidence evidence)
        {
            evidence.PopulationResourceFeedback = BuildPopulationResourceFeedback();
            evidence.PopulationResourceFeedbackDigest = HashBytes(Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(evidence.PopulationResourceFeedback,
                    Formatting.None)));
            if (_populationResourceCalibrationProfile == null) return;
            evidence.PopulationResourceCalibrationProfileId =
                _populationResourceCalibrationProfile.Id;
            var failures = new List<string>();
            long finalBasisPoints = checked(
                evidence.FinalLivingPopulation * 10_000L /
                evidence.InitialLivingPopulation);
            List<AnnualPopulationResourceFeedback> finalYears = evidence
                .PopulationResourceFeedback
                .Skip(Math.Max(0, evidence.PopulationResourceFeedback.Count - 5))
                .ToList();
            int satisfaction = checked((int)finalYears.Average(item =>
                item.FoodSatisfactionBasisPoints));
            int famine = checked((int)finalYears.Average(item =>
                item.FamineDeathBasisPoints));
            if (finalBasisPoints <
                _populationResourceCalibrationProfile.MinimumFinalPopulationBasisPoints)
                failures.Add("final_population_below_declared_minimum");
            if (finalBasisPoints >
                _populationResourceCalibrationProfile.MaximumFinalPopulationBasisPoints)
                failures.Add("final_population_above_declared_maximum");
            if (satisfaction < _populationResourceCalibrationProfile
                .MinimumFiveYearAverageFoodSatisfactionBasisPoints)
                failures.Add("final_five_year_food_satisfaction_below_declared_minimum");
            if (famine > _populationResourceCalibrationProfile
                .MaximumFiveYearAverageFamineDeathBasisPoints)
                failures.Add("final_five_year_famine_death_rate_above_declared_maximum");
            evidence.CalibrationFailures = failures;
            evidence.CalibrationPassed = failures.Count == 0;
        }

        private static int RatioBasisPoints(long numerator, long denominator)
        {
            if (denominator <= 0) return numerator <= 0 ? 10_000 : int.MaxValue;
            return checked((int)Math.Min(
                int.MaxValue,
                checked(numerator * 10_000L / denominator)));
        }

        private static int SignedRatioBasisPoints(long numerator, long denominator)
        {
            if (denominator <= 0) return 0;
            long value = checked(numerator * 10_000L / denominator);
            return checked((int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value)));
        }
    }
}
