using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum ResearchControlMode : byte
    {
        PersonalLabor,
        DirectAssignment,
        WorkOrder,
        TargetInstruction,
        DelegatedPolicy
    }

    public enum ResearchProjectStatus : byte
    {
        Active,
        Completed,
        Failed,
        Cancelled
    }

    public enum ResearchLedgerEntryType : byte
    {
        KnowledgeLearned,
        FundingCommitted,
        ProgressAdded,
        TechnologyMastered,
        TechnologyApplied
    }

    [Serializable]
    public sealed class SkillDefinition
    {
        public string Id;
        public string DisplayName;
        public string FieldId;
        public string HistoricalStatus;
        public string SourceNote;
    }

    [Serializable]
    public sealed class KnowledgeDefinition
    {
        public string Id;
        public string DisplayName;
        public string FieldId;
        public string HistoricalStatus;
        public string SourceNote;
    }

    [Serializable]
    public sealed class TechnologyEffectDefinition
    {
        public string Id;
        public string TargetFacilityTag;
        public string RecipeDefinitionId;
        public string MethodDefinitionId;
        public int YieldBasisPoints = 10_000;
        public int LaborBasisPoints = 10_000;
    }

    [Serializable]
    public sealed class TechnologyDefinition
    {
        public string Id;
        public string DisplayName;
        public string FieldId;
        public string Description;
        public string HistoricalStatus;
        public string SourceNote;
        public string RequiredSkillDefinitionId;
        public int RequiredSkillBasisPoints;
        public int RequiredKnowledgeMasteryBasisPoints = 5_000;
        public int ResearchPointsRequired;
        public long FundingCost;
        public long ApplicationFundingCost;
        public List<string> RequiredKnowledgeDefinitionIds =
            new List<string>();
        public List<string> ResearchFacilityTags = new List<string>();
        public List<TechnologyEffectDefinition> Effects =
            new List<TechnologyEffectDefinition>();
    }

    [Serializable]
    public sealed class SkillMasteryState
    {
        public string SkillDefinitionId;
        public int MasteryBasisPoints;
        public long LastChangedDay;
        public string SourceId;
    }

    [Serializable]
    public sealed class KnowledgeMasteryState
    {
        public string KnowledgeDefinitionId;
        public int MasteryBasisPoints;
        public long LearnedDay;
        public string SourceId;
    }

    [Serializable]
    public sealed class TechnologyMasteryState
    {
        public string TechnologyDefinitionId;
        public long MasteredDay;
        public string ResearchProjectId;
        public string SourceId;
    }

    [Serializable]
    public sealed class ResearchProjectState
    {
        public string Id;
        public string TechnologyDefinitionId;
        public string LeadPersonId;
        public string ResearchFacilityId;
        public ResearchControlMode ControlMode;
        public ResearchProjectStatus Status = ResearchProjectStatus.Active;
        public long StartedDay;
        public long LastProgressDay = -1;
        public long CompletedDay = -1;
        public int RequiredResearchPoints;
        public int ProgressResearchPoints;
        public long FundingCommitted;
    }

    [Serializable]
    public sealed class TechnologyApplicationState
    {
        public string Id;
        public string TechnologyDefinitionId;
        public string TargetFacilityId;
        public string AppliedByPersonId;
        public long AppliedDay;
        public bool IsActive = true;
    }

    [Serializable]
    public sealed class ResearchLedgerEntryState
    {
        public string Id;
        public ResearchLedgerEntryType Type;
        public long Day;
        public string ResearchProjectId;
        public string TechnologyApplicationId;
        public string KnowledgeDefinitionId;
        public string TechnologyDefinitionId;
        public string PersonId;
        public string FacilityId;
        public long FundingDelta;
        public int ProgressDelta;
        public string Summary;
    }

    public readonly struct ProductionTechnologyFactors
    {
        public int YieldBasisPoints { get; }
        public int LaborBasisPoints { get; }
        public IReadOnlyList<string> AppliedTechnologyIds { get; }

        public ProductionTechnologyFactors(
            int yieldBasisPoints,
            int laborBasisPoints,
            IReadOnlyList<string> appliedTechnologyIds)
        {
            YieldBasisPoints = yieldBasisPoints;
            LaborBasisPoints = laborBasisPoints;
            AppliedTechnologyIds = appliedTechnologyIds;
        }
    }

    public static class CoreSkillIds
    {
        public const string Agriculture = "skill.agriculture";
        public const string FoodProcessing = "skill.production.food_processing";
        public const string Metalworking = "skill.production.metalworking";
        public const string Woodworking = "skill.production.woodworking";
        public const string Bowmaking = "skill.production.bowmaking";
        public const string Armoring = "skill.production.armoring";
        public const string Husbandry = "skill.production.husbandry";
        public const string Tanning = "skill.production.tanning";
        public const string HerbalProcessing =
            "skill.production.herbal_processing";
    }

    public static class CoreKnowledgeIds
    {
        public const string SeasonalObservation =
            "knowledge.agriculture.seasonal_observation";
    }

    public static class CoreTechnologyIds
    {
        public const string SeedSelection =
            "technology.agriculture.seed_selection";
        public const string RidgeSowing =
            "technology.agriculture.ridge_sowing";
        public const string CoordinatedFieldwork =
            "technology.agriculture.coordinated_fieldwork";
    }

    public static class VillageFacilityTags
    {
        public const string Farmland = "facility.farmland";
        public const string Irrigation = "facility.irrigation";
        public const string Granary = "facility.granary";
        public const string HouseholdGranary = "facility.household_granary";
        public const string Smithy = "facility.smithy";
        public const string Clinic = "facility.clinic";
        public const string AssemblyHall = "facility.assembly_hall";

        public static string FromKind(VillageFacilityKind kind)
        {
            switch (kind)
            {
                case VillageFacilityKind.Farmland:
                    return Farmland;
                case VillageFacilityKind.Irrigation:
                    return Irrigation;
                case VillageFacilityKind.Granary:
                    return Granary;
                case VillageFacilityKind.HouseholdGranary:
                    return HouseholdGranary;
                case VillageFacilityKind.Smithy:
                    return Smithy;
                case VillageFacilityKind.Clinic:
                    return Clinic;
                case VillageFacilityKind.AssemblyHall:
                    return AssemblyHall;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind), kind, "Unknown village facility kind.");
            }
        }
    }

    public static class SkillMasteryAccess
    {
        public static int Get(PersonState person, string skillDefinitionId)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (string.IsNullOrWhiteSpace(skillDefinitionId))
            {
                throw new ArgumentException(
                    "Skill definition ID cannot be empty.",
                    nameof(skillDefinitionId));
            }

            if (skillDefinitionId == CoreSkillIds.Agriculture)
            {
                return person.ProfessionalSkills?.Agriculture ?? 0;
            }

            var masteries = person.SkillMasteries;
            if (masteries == null)
            {
                return 0;
            }

            for (var i = 0; i < masteries.Count; i++)
            {
                if (masteries[i].SkillDefinitionId == skillDefinitionId)
                {
                    return masteries[i].MasteryBasisPoints;
                }
            }

            return 0;
        }

        public static int GetKnowledgeMastery(
            PersonState person,
            string knowledgeDefinitionId)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            var masteries = person.KnowledgeMasteries;
            if (masteries == null)
            {
                return 0;
            }

            for (var i = 0; i < masteries.Count; i++)
            {
                if (masteries[i].KnowledgeDefinitionId == knowledgeDefinitionId)
                {
                    return masteries[i].MasteryBasisPoints;
                }
            }

            return 0;
        }

        public static bool HasTechnology(
            PersonState person,
            string technologyDefinitionId)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            var masteries = person.TechnologyMasteries;
            if (masteries == null)
            {
                return false;
            }

            for (var i = 0; i < masteries.Count; i++)
            {
                if (masteries[i].TechnologyDefinitionId == technologyDefinitionId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
