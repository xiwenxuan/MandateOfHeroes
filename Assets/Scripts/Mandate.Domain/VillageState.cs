using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum VillageOccupation : byte
    {
        Unknown,
        Farmer,
        Artisan,
        Merchant,
        Headman,
        Physician,
        Dependent
    }

    public enum LocalDutyKind : byte
    {
        None,
        Corvee,
        Levy
    }

    public enum VillageFacilityKind : byte
    {
        Farmland,
        Irrigation,
        Granary,
        Smithy,
        Clinic,
        AssemblyHall
    }

    public enum VillageLedgerEntryType : byte
    {
        Planting,
        Harvest,
        FoodConsumption,
        GrainRelief,
        TaxAssessment,
        TaxPayment,
        Corvee,
        Levy,
        ToolWear,
        ToolRepair,
        MedicalCare,
        Marriage,
        Migration
    }

    [Serializable]
    public sealed class VillageState
    {
        public string Id;
        public string DisplayName;
        public string LocationId;
        public string ParentLocationId;
        public List<string> HouseholdIds = new List<string>();
        public long PublicGranaryGrain;
        public long TaxGrainCollected;
        public int CorveeDaysCompleted;
        public int LevyPersonDays;
        public int LivingResidentCount;
        public int WorkingResidentCount;
        public int HouseholdCount;
        public int FoodSecurityBasisPoints = 10_000;
        public long LastSettlementDay = -1;
        public long NextSettlementDay = 30;
        public long LedgerOpeningFamilyGrain;
        public long LedgerOpeningPublicGrain;
    }

    [Serializable]
    public sealed class VillageFacilityState
    {
        public string Id;
        public string VillageId;
        public VillageFacilityKind Kind;
        public string OwnerFamilyId;
        public string ManagerPersonId;
        public int Capacity;
        public int ConditionBasisPoints = 10_000;
        public long InventoryUnits;
    }

    [Serializable]
    public sealed class VillageLedgerEntryState
    {
        public string Id;
        public long Day;
        public VillageLedgerEntryType Type;
        public string VillageId;
        public string FamilyId;
        public string PersonId;
        public long FamilyGrainDelta;
        public long PublicGrainDelta;
        public int Quantity;
        public string Summary;
    }
}
