using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class HouseholdReliefPriorityPolicyIds
    {
        public const string LegacySettlementFamilyOrder =
            "mandate.relief_priority.legacy_settlement_family_order.v1";
        public const string NeedSeverityVulnerability =
            "mandate.relief_priority.need_severity_vulnerability.v1";
    }

    public static class HouseholdReliefAuthorizationPolicyIds
    {
        public const string LegacySystem =
            "mandate.relief_authority.legacy_system.v1";
        public const string CountyGovernmentLeader =
            "mandate.relief_authority.county_government_leader.v1";
        public const string EmergencySystem =
            "mandate.relief_authority.emergency_system.v1";
    }

    public enum HouseholdReliefPickupStatus : byte
    {
        Waiting,
        PartiallyDelivered,
        Fulfilled
    }

    [Serializable]
    public sealed class HouseholdReliefPickupState
    {
        public string Id;
        public HouseholdReliefPickupStatus Status;
        public string SourceShortfallEventId;
        public string VillageId;
        public string FamilyId;
        public string PriorityPolicyId;
        public string AuthorizationPolicyId;
        public string AuthorizingOrganizationId;
        public string AuthorizingPersonId;
        public long AuthorizedDay = -1;
        public int ShortfallSeverityBasisPoints;
        public int VulnerableAffectedPersonCount;
        public int AffectedPersonCountAtAuthorization;
        public long SettlementDay;
        public long RequestedNutritionBasisUnits;
        public long DeliveredNutritionBasisUnits;
        public long DeliveredPhysicalQuantity;
        public long RemainingNutritionBasisUnits;
        public string LastCollectorPersonId;
        public long LastPickupDay = -1;
        public List<string> InventoryTransactionIds = new List<string>();
    }
}
