using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mandate.Domain
{
    public interface IHanNationalPopulationSnapshotSource
    {
        HanPopulationYearSnapshot LoadPopulationSnapshot(int year);
        HanPopulationYearSnapshot LoadScenarioSnapshot(string scenarioId);
    }

    [Serializable]
    public sealed class HanNationalPopulationManifest
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("format_version")] public int FormatVersion { get; set; }
        [JsonProperty("model_version")] public string ModelVersion { get; set; }
        [JsonProperty("year_start")] public int YearStart { get; set; }
        [JsonProperty("year_end")] public int YearEnd { get; set; }
        [JsonProperty("year_count")] public int YearCount { get; set; }
        [JsonProperty("province_count")] public int ProvinceCount { get; set; }
        [JsonProperty("region_count")] public int RegionCount { get; set; }
        [JsonProperty("county_count")] public int CountyCount { get; set; }
        [JsonProperty("county_year_record_count")] public int CountyYearRecordCount { get; set; }
        [JsonProperty("scenario_count")] public int ScenarioCount { get; set; }
        [JsonProperty("national_anchor_140_registered")] public long NationalAnchor140Registered { get; set; }
        [JsonProperty("national_anchor_157_registered")] public long NationalAnchor157Registered { get; set; }
        [JsonProperty("permanent_persons_generated")] public long PermanentPersonsGenerated { get; set; }
        [JsonProperty("snapshot_path_template")] public string SnapshotPathTemplate { get; set; }
        [JsonProperty("files")] public List<HanPopulationPackageFile> Files { get; set; } = new List<HanPopulationPackageFile>();
    }

    [Serializable]
    public sealed class HanPopulationPackageFile
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("bytes")] public long Bytes { get; set; }
        [JsonProperty("sha256")] public string Sha256 { get; set; }
    }

    [Serializable]
    public sealed class HanPopulationYearSnapshot
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("snapshot_moment")] public string SnapshotMoment { get; set; }
        [JsonProperty("national")] public HanNationalPopulationRecord National { get; set; }
        [JsonProperty("provinces")] public List<HanProvincePopulationRecord> Provinces { get; set; } = new List<HanProvincePopulationRecord>();
        [JsonProperty("regions")] public List<HanRegionPopulationRecord> Regions { get; set; } = new List<HanRegionPopulationRecord>();
        [JsonProperty("counties")] public List<HanCountyPopulationRecord> Counties { get; set; } = new List<HanCountyPopulationRecord>();
        [JsonProperty("major_cities")] public List<HanMajorCityPopulationRecord> MajorCities { get; set; } = new List<HanMajorCityPopulationRecord>();
        [JsonProperty("conservation")] public HanPopulationConservationRecord Conservation { get; set; }
    }

    [Serializable]
    public sealed class HanNationalPopulationRecord
    {
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("registered_population_start")] public long RegisteredPopulationStart { get; set; }
        [JsonProperty("registered_population_end")] public long RegisteredPopulationEnd { get; set; }
        [JsonProperty("modeled_actual_population_start")] public long ModeledActualPopulationStart { get; set; }
        [JsonProperty("modeled_actual_population_end")] public long ModeledActualPopulationEnd { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("natural_deaths")] public long NaturalDeaths { get; set; }
        [JsonProperty("war_deaths")] public long WarDeaths { get; set; }
        [JsonProperty("epidemic_deaths")] public long EpidemicDeaths { get; set; }
        [JsonProperty("disaster_deaths")] public long DisasterDeaths { get; set; }
        [JsonProperty("net_migration")] public long NetMigration { get; set; }
        [JsonProperty("registration_loss")] public long RegistrationLoss { get; set; }
        [JsonProperty("registration_recovery")] public long RegistrationRecovery { get; set; }
        [JsonProperty("annual_change")] public long AnnualChange { get; set; }
        [JsonProperty("annual_change_rate")] public double AnnualChangeRate { get; set; }
        [JsonProperty("historical_anchors")] public string HistoricalAnchors { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanProvincePopulationRecord
    {
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("province_permanent_id")] public string ProvincePermanentId { get; set; }
        [JsonProperty("historical_province_name")] public string HistoricalProvinceName { get; set; }
        [JsonProperty("registered_population")] public long RegisteredPopulation { get; set; }
        [JsonProperty("registered_population_end")] public long RegisteredPopulationEnd { get; set; }
        [JsonProperty("modeled_actual_population")] public long ModeledActualPopulation { get; set; }
        [JsonProperty("modeled_actual_population_end")] public long ModeledActualPopulationEnd { get; set; }
        [JsonProperty("national_share")] public double NationalShare { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("natural_deaths")] public long NaturalDeaths { get; set; }
        [JsonProperty("war_deaths")] public long WarDeaths { get; set; }
        [JsonProperty("epidemic_deaths")] public long EpidemicDeaths { get; set; }
        [JsonProperty("disaster_deaths")] public long DisasterDeaths { get; set; }
        [JsonProperty("net_migration")] public long NetMigration { get; set; }
        [JsonProperty("registration_change")] public long RegistrationChange { get; set; }
        [JsonProperty("population_density")] public double PopulationDensity { get; set; }
        [JsonProperty("urban_population")] public long UrbanPopulation { get; set; }
        [JsonProperty("rural_population")] public long RuralPopulation { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanRegionPopulationRecord
    {
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("region_permanent_id")] public string RegionPermanentId { get; set; }
        [JsonProperty("historical_name")] public string HistoricalName { get; set; }
        [JsonProperty("region_type")] public string RegionType { get; set; }
        [JsonProperty("province_permanent_id")] public string ProvincePermanentId { get; set; }
        [JsonProperty("registered_population")] public long RegisteredPopulation { get; set; }
        [JsonProperty("registered_population_end")] public long RegisteredPopulationEnd { get; set; }
        [JsonProperty("modeled_actual_population")] public long ModeledActualPopulation { get; set; }
        [JsonProperty("modeled_actual_population_end")] public long ModeledActualPopulationEnd { get; set; }
        [JsonProperty("urban_population")] public long UrbanPopulation { get; set; }
        [JsonProperty("rural_population")] public long RuralPopulation { get; set; }
        [JsonProperty("population_density")] public double PopulationDensity { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("natural_deaths")] public long NaturalDeaths { get; set; }
        [JsonProperty("war_deaths")] public long WarDeaths { get; set; }
        [JsonProperty("epidemic_deaths")] public long EpidemicDeaths { get; set; }
        [JsonProperty("disaster_deaths")] public long DisasterDeaths { get; set; }
        [JsonProperty("net_migration")] public long NetMigration { get; set; }
        [JsonProperty("registration_loss")] public long RegistrationLoss { get; set; }
        [JsonProperty("registration_recovery")] public long RegistrationRecovery { get; set; }
        [JsonProperty("male_population")] public long MalePopulation { get; set; }
        [JsonProperty("female_population")] public long FemalePopulation { get; set; }
        [JsonProperty("civilian_population")] public long CivilianPopulation { get; set; }
        [JsonProperty("military_active_population")] public long MilitaryActivePopulation { get; set; }
        [JsonProperty("historical_anchor")] public long? HistoricalAnchor { get; set; }
        [JsonProperty("national_anchor_reconciliation")] public long NationalAnchorReconciliation { get; set; }
        [JsonProperty("active_event_ids")] public List<string> ActiveEventIds { get; set; } = new List<string>();
        [JsonProperty("model_method")] public string ModelMethod { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanCountyPopulationRecord
    {
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("county_permanent_id")] public string CountyPermanentId { get; set; }
        [JsonProperty("historical_county_name")] public string HistoricalCountyName { get; set; }
        [JsonProperty("parent_region_permanent_id")] public string ParentRegionPermanentId { get; set; }
        [JsonProperty("province_permanent_id")] public string ProvincePermanentId { get; set; }
        [JsonProperty("registered_population")] public long RegisteredPopulation { get; set; }
        [JsonProperty("modeled_actual_population")] public long ModeledActualPopulation { get; set; }
        [JsonProperty("population_density")] public double PopulationDensity { get; set; }
        [JsonProperty("urban_settlement_population")] public long UrbanSettlementPopulation { get; set; }
        [JsonProperty("town_population")] public long TownPopulation { get; set; }
        [JsonProperty("village_population")] public long VillagePopulation { get; set; }
        [JsonProperty("estate_population")] public long EstatePopulation { get; set; }
        [JsonProperty("dispersed_agricultural_population")] public long DispersedAgriculturalPopulation { get; set; }
        [JsonProperty("pastoral_forest_population")] public long PastoralForestPopulation { get; set; }
        [JsonProperty("special_population")] public long SpecialPopulation { get; set; }
        [JsonProperty("births")] public long Births { get; set; }
        [JsonProperty("deaths")] public long Deaths { get; set; }
        [JsonProperty("migration")] public long Migration { get; set; }
        [JsonProperty("historical_events")] public string HistoricalEvents { get; set; }
        [JsonProperty("county_weight")] public double CountyWeight { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanMajorCityPopulationRecord
    {
        [JsonProperty("city_permanent_id")] public string CityPermanentId { get; set; }
        [JsonProperty("city_name")] public string CityName { get; set; }
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("walled_city_population")] public long WalledCityPopulation { get; set; }
        [JsonProperty("urban_area_population")] public long UrbanAreaPopulation { get; set; }
        [JsonProperty("metropolitan_population")] public long MetropolitanPopulation { get; set; }
        [JsonProperty("county_population")] public long CountyPopulation { get; set; }
        [JsonProperty("county_permanent_id")] public string CountyPermanentId { get; set; }
        [JsonProperty("evidence")] public string Evidence { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("model_method")] public string ModelMethod { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanPopulationConservationRecord
    {
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("national_actual")] public long NationalActual { get; set; }
        [JsonProperty("province_actual")] public long ProvinceActual { get; set; }
        [JsonProperty("region_actual")] public long RegionActual { get; set; }
        [JsonProperty("county_actual")] public long CountyActual { get; set; }
        [JsonProperty("settlement_actual")] public long SettlementActual { get; set; }
        [JsonProperty("actual_error")] public long ActualError { get; set; }
        [JsonProperty("national_registered")] public long NationalRegistered { get; set; }
        [JsonProperty("province_registered")] public long ProvinceRegistered { get; set; }
        [JsonProperty("region_registered")] public long RegionRegistered { get; set; }
        [JsonProperty("county_registered")] public long CountyRegistered { get; set; }
        [JsonProperty("registered_error")] public long RegisteredError { get; set; }
        [JsonProperty("migration_error")] public long MigrationError { get; set; }
        [JsonProperty("negative_population_count")] public int NegativePopulationCount { get; set; }
        [JsonProperty("duplicate_county_count")] public int DuplicateCountyCount { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
    }
}
