using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mandate.Domain
{
    public interface IHanHistoricalPersonClanSource
    {
        HanHistoricalPerson GetPerson(string personId);
        HanHistoricalClan GetClan(string clanId);
        HanHistoricalBranch GetBranch(string branchId);
        IReadOnlyList<HanHistoricalPerson> GetPeople();
        IReadOnlyList<HanHistoricalClan> GetClans();
        IReadOnlyList<HanHistoricalBranch> GetBranches();
        IReadOnlyList<HanHistoricalKinship> GetKinship();
        IReadOnlyList<HanHistoricalMarriage> GetMarriages();
        IReadOnlyList<HanHistoricalLocationRecord> GetLocations();
        IReadOnlyList<HanHistoricalCivilOfficeRecord> GetCivilOffices();
        IReadOnlyList<HanHistoricalMilitaryOfficeRecord> GetMilitaryOffices();
        IReadOnlyList<HanHistoricalTitleRecord> GetTitles();
        IReadOnlyList<HanHistoricalAllegianceRecord> GetAllegiances();
        IReadOnlyList<HanHistoricalClanPresenceRecord> GetClanPresence();
        HanHistoricalScenarioSnapshot LoadHistoricalSnapshot(int year);
        HanHistoricalScenarioSnapshot LoadScenarioSnapshot(string scenarioId);
    }

    [Serializable]
    public sealed class HanHistoricalPersonClanManifest
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("format_version")] public int FormatVersion { get; set; }
        [JsonProperty("dataset_id")] public string DatasetId { get; set; }
        [JsonProperty("year_start")] public int YearStart { get; set; }
        [JsonProperty("year_end")] public int YearEnd { get; set; }
        [JsonProperty("source_baseline")] public string SourceBaseline { get; set; }
        [JsonProperty("person_count")] public int PersonCount { get; set; }
        [JsonProperty("clan_count")] public int ClanCount { get; set; }
        [JsonProperty("branch_count")] public int BranchCount { get; set; }
        [JsonProperty("scenario_count")] public int ScenarioCount { get; set; }
        [JsonProperty("family_organization_count")] public int FamilyOrganizationCount { get; set; }
        [JsonProperty("household_count")] public int HouseholdCount { get; set; }
        [JsonProperty("files")] public List<HanHistoricalPackageFile> Files { get; set; } = new List<HanHistoricalPackageFile>();
    }

    [Serializable]
    public sealed class HanHistoricalPackageFile
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("bytes")] public long Bytes { get; set; }
        [JsonProperty("sha256")] public string Sha256 { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalPerson
    {
        [JsonProperty("person_id")] public string PersonId { get; set; }
        [JsonProperty("canonical_name")] public string CanonicalName { get; set; }
        [JsonProperty("surname")] public string Surname { get; set; }
        [JsonProperty("given_name")] public string GivenName { get; set; }
        [JsonProperty("courtesy_name")] public string CourtesyName { get; set; }
        [JsonProperty("gender")] public string Gender { get; set; }
        [JsonProperty("birth_year")] public int? BirthYear { get; set; }
        [JsonProperty("birth_year_low")] public int? BirthYearLow { get; set; }
        [JsonProperty("birth_year_high")] public int? BirthYearHigh { get; set; }
        [JsonProperty("birth_date_precision")] public string BirthDatePrecision { get; set; }
        [JsonProperty("death_year")] public int? DeathYear { get; set; }
        [JsonProperty("death_year_low")] public int? DeathYearLow { get; set; }
        [JsonProperty("death_year_high")] public int? DeathYearHigh { get; set; }
        [JsonProperty("death_date_precision")] public string DeathDatePrecision { get; set; }
        [JsonProperty("is_anonymous")] public bool IsAnonymous { get; set; }
        [JsonProperty("anonymous_description")] public string AnonymousDescription { get; set; }
        [JsonProperty("historical_person_tier")] public string HistoricalPersonTier { get; set; }
        [JsonProperty("birth_clan_id")] public string BirthClanId { get; set; }
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("lineage_branch_id")] public string LineageBranchId { get; set; }
        [JsonProperty("native_place_region_id")] public string NativePlaceRegionId { get; set; }
        [JsonProperty("native_place_county_id")] public string NativePlaceCountyId { get; set; }
        [JsonProperty("native_place_text")] public string NativePlaceText { get; set; }
        [JsonProperty("birth_location_region_id")] public string BirthLocationRegionId { get; set; }
        [JsonProperty("clan_commandery_region_id")] public string ClanCommanderyRegionId { get; set; }
        [JsonProperty("primary_historical_region_id")] public string PrimaryHistoricalRegionId { get; set; }
        [JsonProperty("father_person_id")] public string FatherPersonId { get; set; }
        [JsonProperty("mother_person_id")] public string MotherPersonId { get; set; }
        [JsonProperty("primary_identity")] public string PrimaryIdentity { get; set; }
        [JsonProperty("historical_role_tags")] public List<string> HistoricalRoleTags { get; set; } = new List<string>();
        [JsonProperty("primary_allegiance_text")] public string PrimaryAllegianceText { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("research_status")] public string ResearchStatus { get; set; }
        [JsonProperty("source_id")] public string SourceId { get; set; }
        [JsonProperty("timeline_coverage_level")] public string TimelineCoverageLevel { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalClan
    {
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("canonical_clan_name")] public string CanonicalClanName { get; set; }
        [JsonProperty("surname")] public string Surname { get; set; }
        [JsonProperty("clan_type")] public string ClanType { get; set; }
        [JsonProperty("clan_commandery_region_id")] public string ClanCommanderyRegionId { get; set; }
        [JsonProperty("clan_county_region_id")] public string ClanCountyRegionId { get; set; }
        [JsonProperty("native_origin_description")] public string NativeOriginDescription { get; set; }
        [JsonProperty("traditional_origin")] public string TraditionalOrigin { get; set; }
        [JsonProperty("earliest_known_ancestor_person_id")] public string EarliestKnownAncestorPersonId { get; set; }
        [JsonProperty("founder_person_id")] public string FounderPersonId { get; set; }
        [JsonProperty("start_year")] public int? StartYear { get; set; }
        [JsonProperty("end_year")] public int? EndYear { get; set; }
        [JsonProperty("historical_status")] public string HistoricalStatus { get; set; }
        [JsonProperty("major_clan")] public bool MajorClan { get; set; }
        [JsonProperty("primary_region_id")] public string PrimaryRegionId { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("research_status")] public string ResearchStatus { get; set; }
        [JsonProperty("source_candidate_id")] public string SourceCandidateId { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalBranch
    {
        [JsonProperty("branch_id")] public string BranchId { get; set; }
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("parent_branch_id")] public string ParentBranchId { get; set; }
        [JsonProperty("branch_name")] public string BranchName { get; set; }
        [JsonProperty("founder_person_id")] public string FounderPersonId { get; set; }
        [JsonProperty("origin_region_id")] public string OriginRegionId { get; set; }
        [JsonProperty("start_year")] public int? StartYear { get; set; }
        [JsonProperty("end_year")] public int? EndYear { get; set; }
        [JsonProperty("branch_description")] public string BranchDescription { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalKinship
    {
        [JsonProperty("relation_id")] public string RelationId { get; set; }
        [JsonProperty("person_a_id")] public string PersonAId { get; set; }
        [JsonProperty("person_b_id")] public string PersonBId { get; set; }
        [JsonProperty("relation_type")] public string RelationType { get; set; }
        [JsonProperty("biological")] public bool Biological { get; set; }
        [JsonProperty("adoptive")] public bool Adoptive { get; set; }
        [JsonProperty("legal")] public bool Legal { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("source_id")] public string SourceId { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalMarriage
    {
        [JsonProperty("marriage_id")] public string MarriageId { get; set; }
        [JsonProperty("person_a_id")] public string PersonAId { get; set; }
        [JsonProperty("person_b_id")] public string PersonBId { get; set; }
        [JsonProperty("marriage_type")] public string MarriageType { get; set; }
        [JsonProperty("start_year")] public int? StartYear { get; set; }
        [JsonProperty("end_year")] public int? EndYear { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("source_id")] public string SourceId { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }
    }

    [Serializable]
    public abstract class HanHistoricalTimelineRecord
    {
        [JsonProperty("record_id")] public string RecordId { get; set; }
        [JsonProperty("person_id")] public string PersonId { get; set; }
        [JsonProperty("start_year")] public int? StartYear { get; set; }
        [JsonProperty("start_month")] public int? StartMonth { get; set; }
        [JsonProperty("end_year")] public int? EndYear { get; set; }
        [JsonProperty("end_month")] public int? EndMonth { get; set; }
        [JsonProperty("date_precision")] public string DatePrecision { get; set; }
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("historical_event_id")] public string HistoricalEventId { get; set; }
        [JsonProperty("source_id")] public string SourceId { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }

        public bool ContainsYear(int year)
        {
            return (!StartYear.HasValue || StartYear.Value <= year) && (!EndYear.HasValue || year <= EndYear.Value);
        }
    }

    [Serializable]
    public sealed class HanHistoricalLocationRecord : HanHistoricalTimelineRecord
    {
        [JsonProperty("historical_location_text")] public string HistoricalLocationText { get; set; }
        [JsonProperty("location_type")] public string LocationType { get; set; }
        [JsonProperty("location_reason")] public string LocationReason { get; set; }
        [JsonProperty("region_permanent_id")] public string RegionPermanentId { get; set; }
        [JsonProperty("county_permanent_id")] public string CountyPermanentId { get; set; }
        [JsonProperty("city_id")] public string CityId { get; set; }
        [JsonProperty("resolution_method")] public string ResolutionMethod { get; set; }
        [JsonProperty("model_fallback_location")] public string ModelFallbackLocation { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalCivilOfficeRecord : HanHistoricalTimelineRecord
    {
        [JsonProperty("office_definition_id")] public string OfficeDefinitionId { get; set; }
        [JsonProperty("historical_office_name")] public string HistoricalOfficeName { get; set; }
        [JsonProperty("jurisdiction_text")] public string JurisdictionText { get; set; }
        [JsonProperty("appointment_authority")] public string AppointmentAuthority { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalMilitaryOfficeRecord : HanHistoricalTimelineRecord
    {
        [JsonProperty("military_office_definition_id")] public string MilitaryOfficeDefinitionId { get; set; }
        [JsonProperty("historical_office_name")] public string HistoricalOfficeName { get; set; }
        [JsonProperty("jurisdiction")] public string Jurisdiction { get; set; }
        [JsonProperty("command_scope")] public string CommandScope { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalTitleRecord : HanHistoricalTimelineRecord
    {
        [JsonProperty("title_definition_id")] public string TitleDefinitionId { get; set; }
        [JsonProperty("historical_title_name")] public string HistoricalTitleName { get; set; }
        [JsonProperty("title_type")] public string TitleType { get; set; }
        [JsonProperty("fief_text")] public string FiefText { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalAllegianceRecord : HanHistoricalTimelineRecord
    {
        [JsonProperty("political_role")] public string PoliticalRole { get; set; }
        [JsonProperty("allegiance_target")] public string AllegianceTarget { get; set; }
        [JsonProperty("han_relation")] public string HanRelation { get; set; }
        [JsonProperty("sovereign_claim")] public string SovereignClaim { get; set; }
        [JsonProperty("polity_id")] public string PolityId { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalClanPresenceRecord
    {
        [JsonProperty("presence_id")] public string PresenceId { get; set; }
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("branch_id")] public string BranchId { get; set; }
        [JsonProperty("start_year")] public int StartYear { get; set; }
        [JsonProperty("end_year")] public int EndYear { get; set; }
        [JsonProperty("region_permanent_id")] public string RegionPermanentId { get; set; }
        [JsonProperty("county_permanent_id")] public string CountyPermanentId { get; set; }
        [JsonProperty("presence_type")] public string PresenceType { get; set; }
        [JsonProperty("known_member_count")] public int KnownMemberCount { get; set; }
        [JsonProperty("major_members")] public List<string> MajorMembers { get; set; } = new List<string>();
        [JsonProperty("evidence_level")] public string EvidenceLevel { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; }

        public bool ContainsYear(int year) { return StartYear <= year && year <= EndYear; }
    }

    [Serializable]
    public sealed class HanHistoricalScenarioSnapshot
    {
        [JsonProperty("schema")] public string Schema { get; set; }
        [JsonProperty("scenario_id")] public string ScenarioId { get; set; }
        [JsonProperty("scenario_name")] public string ScenarioName { get; set; }
        [JsonProperty("year")] public int Year { get; set; }
        [JsonProperty("source_timeline_version")] public string SourceTimelineVersion { get; set; }
        [JsonProperty("persons")] public List<HanHistoricalPersonSnapshot> Persons { get; set; } = new List<HanHistoricalPersonSnapshot>();
        [JsonProperty("clans")] public List<HanHistoricalClanSnapshot> Clans { get; set; } = new List<HanHistoricalClanSnapshot>();
    }

    [Serializable]
    public sealed class HanHistoricalPersonSnapshot
    {
        [JsonProperty("person_id")] public string PersonId { get; set; }
        [JsonProperty("alive_state")] public string AliveState { get; set; }
        [JsonProperty("current_location_record_id")] public string CurrentLocationRecordId { get; set; }
        [JsonProperty("current_region_id")] public string CurrentRegionId { get; set; }
        [JsonProperty("current_county_id")] public string CurrentCountyId { get; set; }
        [JsonProperty("current_city_id")] public string CurrentCityId { get; set; }
        [JsonProperty("current_civil_office_record_ids")] public List<string> CurrentCivilOfficeRecordIds { get; set; } = new List<string>();
        [JsonProperty("current_military_office_record_ids")] public List<string> CurrentMilitaryOfficeRecordIds { get; set; } = new List<string>();
        [JsonProperty("current_title_record_ids")] public List<string> CurrentTitleRecordIds { get; set; } = new List<string>();
        [JsonProperty("current_allegiance_record_ids")] public List<string> CurrentAllegianceRecordIds { get; set; } = new List<string>();
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("branch_id")] public string BranchId { get; set; }
        [JsonProperty("historical_role")] public string HistoricalRole { get; set; }
        [JsonProperty("confidence")] public string Confidence { get; set; }
        [JsonProperty("location_conflict")] public bool LocationConflict { get; set; }
    }

    [Serializable]
    public sealed class HanHistoricalClanSnapshot
    {
        [JsonProperty("clan_id")] public string ClanId { get; set; }
        [JsonProperty("active_status")] public string ActiveStatus { get; set; }
        [JsonProperty("core_region_id")] public string CoreRegionId { get; set; }
        [JsonProperty("known_branch_ids")] public List<string> KnownBranchIds { get; set; } = new List<string>();
        [JsonProperty("known_living_member_ids")] public List<string> KnownLivingMemberIds { get; set; } = new List<string>();
        [JsonProperty("known_regional_presence_ids")] public List<string> KnownRegionalPresenceIds { get; set; } = new List<string>();
        [JsonProperty("major_political_member_ids")] public List<string> MajorPoliticalMemberIds { get; set; } = new List<string>();
        [JsonProperty("marriage_ids")] public List<string> MarriageIds { get; set; } = new List<string>();
        [JsonProperty("evidence_coverage")] public string EvidenceCoverage { get; set; }
    }
}
