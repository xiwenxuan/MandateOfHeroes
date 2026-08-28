using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class LuoyangFamilyAssetRuntimeState
    {
        public string Id;
        public string FamilyOrganizationId;
        public string AssetKindId;
        public string AssetId;
        public string OwnerId;
        public string ControllerId;
        public long AcquiredDay;
    }

    [Serializable]
    public sealed class LuoyangPersonDevelopmentRuntimeState
    {
        public uint PersonOrdinal;
        public string CurrentActivityId;
        public string CurrentLocationId = "location.capital.luoyang";
        public string ResidenceFacilityId;
        public string SocialRoleId;
        public int FatigueBasisPoints;
        public int KnowledgeBasisPoints;
        public int SkillBasisPoints;
        public long StudyMinutes;
        public long TrainingMinutes;
        public List<string> KnownRecipeIds = new List<string>();
        public List<string> BookInventoryIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangOfficeRuntimeState
    {
        public string Id;
        public string OfficeKindId;
        public string JurisdictionId;
        public string AuthorityId;
        public string GovernmentFacilityId;
        public uint HolderPersonOrdinal;
        public string CurrentActivityId;
        public long SalaryExpense;
    }

    [Serializable]
    public sealed class LuoyangTaxRuntimeState
    {
        public string Id;
        public long Day;
        public string TaxKindId;
        public string PayerId;
        public string GovernmentId;
        public string ProductId;
        public long MoneyPaid;
        public long ProductQuantityMilliunits;
        public string DestinationInventoryId;
    }

    [Serializable]
    public sealed class LuoyangMilitaryForceRuntimeState
    {
        public string Id;
        public string OrganizationId;
        public string CurrentLocationId = "location.capital.luoyang";
        public string TransitDestinationId;
        public long TransitArrivalDay = -1;
        public string BarracksFacilityId;
        public string ArsenalFacilityId;
        public int PermanentPersonCount;
        public int DefenseBasisPoints;
        public bool GatesClosed;
        public long FoodConsumedMilliunits;
        public List<string> InventoryIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangSocialPressureRuntimeState
    {
        public long Day;
        public int FoodShortageBasisPoints;
        public int UnemploymentBasisPoints;
        public int WarBasisPoints;
        public int DisplacementBasisPoints;
        public int CompositeBasisPoints;
        public string PublicOrderStatusId;
    }

    [Serializable]
    public sealed class LuoyangHistoricalEventRuntimeState
    {
        public string Id;
        public string DefinitionId;
        public string StatusId;
        public string OutcomeId;
        public long EarliestDay;
        public long ResolvedDay = -1;
        public bool AppliedOffscreen;
        public List<string> AppliedChangeIds = new List<string>();
    }

    [Serializable]
    public sealed class LuoyangPlayerCommandRuntimeState
    {
        public string Id;
        public long Day;
        public uint PersonOrdinal;
        public string CommandTypeId;
        public string TargetId;
        public string StatusId;
        public string ResultId;
    }
}
