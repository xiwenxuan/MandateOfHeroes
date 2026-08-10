using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public sealed class Luoyang184UrbanInitializationManifest
    {
        public string Schema { get; set; }
        public int FormatVersion { get; set; }
        public string ScenarioId { get; set; }
        public int ScenarioYear { get; set; }
        public string WorldId { get; set; }
        public string CityId { get; set; }
        public string DataOrigin { get; set; }
        public string PopulationProfileId { get; set; }
        public int WalledCityPopulation { get; set; }
        public int UrbanAreaPopulation { get; set; }
        public int MetropolitanPlanPopulation { get; set; }
        public int SupplyRegionPlanPopulation { get; set; }
        public int PersonRecordSize { get; set; }
        public int PersonCount { get; set; }
        public int HouseholdRecordSize { get; set; }
        public int HouseholdCount { get; set; }
        public int HistoricalPersonCount { get; set; }
        public int ExternalHistoricalAnchorCount { get; set; }
        public int FacilityCount { get; set; }
        public int FamilyOrganizationCount { get; set; }
        public int ForceCount { get; set; }
        public int EventCount { get; set; }
        public List<Luoyang184UrbanPackageFile> Files { get; set; } = new List<Luoyang184UrbanPackageFile>();
    }

    public sealed class Luoyang184UrbanPackageFile
    {
        public string Path { get; set; }
        public long Bytes { get; set; }
        public string Sha256 { get; set; }
    }

    public readonly struct Luoyang184PermanentPersonRecord
    {
        public Luoyang184PermanentPersonRecord(
            uint ordinal, short birthYear, byte gender, byte ageStage, ushort healthBasisPoints,
            uint householdOrdinal, ushort familyOrganizationIndex, ulong currentCellId64,
            uint residenceFacilityIndex, uint workFacilityIndex, ushort occupationIndex,
            ushort activityIndex, ushort civilOfficeIndex, ushort militaryOfficeIndex,
            ushort titleIndex, ushort allegianceIndex, ushort forceIndex, ushort reserveForceIndex,
            ushort skillProfileIndex, ushort knowledgeProfileIndex, long personalAssets,
            ushort naturalLifespan, ushort politicalRoleIndex, byte dataOriginIndex,
            byte residenceStatusIndex, byte employmentStatusIndex, byte locationStatusIndex,
            int fatherOrdinal, int motherOrdinal, int spouseOrdinal)
        {
            Ordinal = ordinal;
            BirthYear = birthYear;
            Gender = gender;
            AgeStage = ageStage;
            HealthBasisPoints = healthBasisPoints;
            HouseholdOrdinal = householdOrdinal;
            FamilyOrganizationIndex = familyOrganizationIndex;
            CurrentCellId64 = currentCellId64;
            ResidenceFacilityIndex = residenceFacilityIndex;
            WorkFacilityIndex = workFacilityIndex;
            OccupationIndex = occupationIndex;
            ActivityIndex = activityIndex;
            CivilOfficeIndex = civilOfficeIndex;
            MilitaryOfficeIndex = militaryOfficeIndex;
            TitleIndex = titleIndex;
            AllegianceIndex = allegianceIndex;
            ForceIndex = forceIndex;
            ReserveForceIndex = reserveForceIndex;
            SkillProfileIndex = skillProfileIndex;
            KnowledgeProfileIndex = knowledgeProfileIndex;
            PersonalAssets = personalAssets;
            NaturalLifespan = naturalLifespan;
            PoliticalRoleIndex = politicalRoleIndex;
            DataOriginIndex = dataOriginIndex;
            ResidenceStatusIndex = residenceStatusIndex;
            EmploymentStatusIndex = employmentStatusIndex;
            LocationStatusIndex = locationStatusIndex;
            FatherOrdinal = fatherOrdinal;
            MotherOrdinal = motherOrdinal;
            SpouseOrdinal = spouseOrdinal;
        }

        public uint Ordinal { get; }
        public short BirthYear { get; }
        public byte Gender { get; }
        public byte AgeStage { get; }
        public ushort HealthBasisPoints { get; }
        public uint HouseholdOrdinal { get; }
        public ushort FamilyOrganizationIndex { get; }
        public ulong CurrentCellId64 { get; }
        public uint ResidenceFacilityIndex { get; }
        public uint WorkFacilityIndex { get; }
        public ushort OccupationIndex { get; }
        public ushort ActivityIndex { get; }
        public ushort CivilOfficeIndex { get; }
        public ushort MilitaryOfficeIndex { get; }
        public ushort TitleIndex { get; }
        public ushort AllegianceIndex { get; }
        public ushort ForceIndex { get; }
        public ushort ReserveForceIndex { get; }
        public ushort SkillProfileIndex { get; }
        public ushort KnowledgeProfileIndex { get; }
        public long PersonalAssets { get; }
        public ushort NaturalLifespan { get; }
        public ushort PoliticalRoleIndex { get; }
        public byte DataOriginIndex { get; }
        public byte ResidenceStatusIndex { get; }
        public byte EmploymentStatusIndex { get; }
        public byte LocationStatusIndex { get; }
        public int FatherOrdinal { get; }
        public int MotherOrdinal { get; }
        public int SpouseOrdinal { get; }
    }

    public readonly struct Luoyang184HouseholdRecord
    {
        public Luoyang184HouseholdRecord(uint ordinal, uint headOrdinal, uint memberStartOrdinal,
            ushort memberCount, ushort familyOrganizationIndex, uint residenceFacilityIndex,
            byte householdTypeIndex, byte dataOriginIndex, long wealth)
        {
            Ordinal = ordinal;
            HeadOrdinal = headOrdinal;
            MemberStartOrdinal = memberStartOrdinal;
            MemberCount = memberCount;
            FamilyOrganizationIndex = familyOrganizationIndex;
            ResidenceFacilityIndex = residenceFacilityIndex;
            HouseholdTypeIndex = householdTypeIndex;
            DataOriginIndex = dataOriginIndex;
            Wealth = wealth;
        }

        public uint Ordinal { get; }
        public uint HeadOrdinal { get; }
        public uint MemberStartOrdinal { get; }
        public ushort MemberCount { get; }
        public ushort FamilyOrganizationIndex { get; }
        public uint ResidenceFacilityIndex { get; }
        public byte HouseholdTypeIndex { get; }
        public byte DataOriginIndex { get; }
        public long Wealth { get; }
    }

    public interface ILuoyang184UrbanPopulationSource
    {
        Luoyang184UrbanInitializationManifest Manifest { get; }
        IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(int startOrdinal, int count);
        IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(int startOrdinal, int count);
    }

    public sealed class Luoyang184HistoricalPersonRuntimeState
    {
        public string PersonId { get; set; }
        public uint Ordinal { get; set; }
        public string CurrentActivityId { get; set; }
        public string CurrentLocationId { get; set; }
    }

    public sealed class Luoyang184ForceRuntimeState
    {
        public string ForceId { get; set; }
        public string CommanderPersonId { get; set; }
        public string Status { get; set; }
        public string DestinationLocationId { get; set; }
        public int MemberCount { get; set; }
    }

    public sealed class Luoyang184ScenarioActionDefinition
    {
        public string TypeId { get; set; }
        public string PersonId { get; set; }
        public string ForceId { get; set; }
        public string ScopeForceId { get; set; }
        public string Value { get; set; }
        public int NumericValue { get; set; }
    }

    public sealed class Luoyang184ScenarioEventDefinition
    {
        public string EventId { get; set; }
        public int Order { get; set; }
        public string Label { get; set; }
        public string InitialStatus { get; set; }
        public List<string> Actors { get; set; } = new List<string>();
        public List<Luoyang184ScenarioActionDefinition> Actions { get; set; } = new List<Luoyang184ScenarioActionDefinition>();
    }

    public sealed class Luoyang184UrbanScenarioState
    {
        public Dictionary<string, Luoyang184HistoricalPersonRuntimeState> HistoricalPeople { get; } =
            new Dictionary<string, Luoyang184HistoricalPersonRuntimeState>(StringComparer.Ordinal);

        public Dictionary<string, Luoyang184ForceRuntimeState> Forces { get; } =
            new Dictionary<string, Luoyang184ForceRuntimeState>(StringComparer.Ordinal);

        public Dictionary<ushort, string> ForceIdsByIndex { get; } = new Dictionary<ushort, string>();

        public HashSet<string> AppliedEventIds { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> PausedWorkForceIds { get; } = new HashSet<string>(StringComparer.Ordinal);
        public int MilitarySupplyPressure { get; set; }
        public int TransportPressure { get; set; }

        public bool IsWorkPaused(Luoyang184PermanentPersonRecord person)
        {
            if (person.ForceIndex == ushort.MaxValue)
            {
                return false;
            }

            return ForceIdsByIndex.TryGetValue(person.ForceIndex, out var forceId)
                && PausedWorkForceIds.Contains(forceId);
        }
    }
}
