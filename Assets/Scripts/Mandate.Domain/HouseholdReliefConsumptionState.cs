using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class HouseholdReliefAllocationPolicyIds
    {
        public const string LegacyHouseholdShared =
            "mandate.relief_allocation.legacy_household_shared.v1";
        public const string ProportionalIndividualNeed =
            "mandate.relief_allocation.proportional_individual_need.v1";
    }

    public static class HouseholdReliefCareDeliveryPolicyIds
    {
        public const string LegacySelfService =
            "mandate.relief_care.legacy_self_service.v1";
        public const string AgeHealthDependency =
            "mandate.relief_care.age_health_dependency.v1";
    }

    public static class HouseholdReliefCareDeliverySourceIds
    {
        public const string TracedFoodTransaction =
            "mandate.relief_care_source.traced_food_transaction.v1";
        public const string PreparedNutrition =
            "mandate.relief_care_source.prepared_nutrition.v1";
    }

    public enum HouseholdReliefConsumptionStatus : byte
    {
        Waiting,
        PartiallyConsumed,
        Fulfilled
    }

    [Serializable]
    public sealed class HouseholdReliefAffectedPersonState
    {
        public string PersonId;
        public bool RequiresCaregiverDelivery;
        public long RequiredNutritionBasisUnits;
        public long AllocatedNutritionBasisUnits;
        public long ConsumedNutritionBasisUnits;
        public int AppliedHealthDamageBasisPoints;
        public int AppliedLivelihoodPressureBasisPoints;
        public int RecoveredHealthBasisPoints;
        public int RecoveredLivelihoodBasisPoints;
    }

    [Serializable]
    public sealed class HouseholdReliefConsumptionState
    {
        public string Id;
        public HouseholdReliefConsumptionStatus Status;
        public string PickupId;
        public string SourceShortfallEventId;
        public string VillageId;
        public string FamilyId;
        public string AllocationPolicyId;
        public string CareDeliveryPolicyId;
        public long SettlementDay;
        public long RequestedNutritionBasisUnits;
        public long ConsumedNutritionBasisUnits;
        public long PreparedNutritionBasisUnits;
        public long ConsumedPhysicalQuantity;
        public long RemainingNutritionBasisUnits;
        public string LastConsumerPersonId;
        public long LastConsumptionDay = -1;
        public List<string> InventoryTransactionIds = new List<string>();
        public List<HouseholdReliefAffectedPersonState> AffectedPeople =
            new List<HouseholdReliefAffectedPersonState>();
    }

    [Serializable]
    public sealed class HouseholdReliefCareDeliveryState
    {
        public string Id;
        public string HouseholdReliefConsumptionId;
        public string RecipientPersonId;
        public string CaregiverPersonId;
        public long Day;
        public long NutritionBasisUnits;
        public string SourceKindId;
        public string SourceInventoryTransactionId;
    }
}
