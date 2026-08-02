using System;

namespace Mandate.Domain
{
    public enum OrganizationType : byte
    {
        Government,
        Military,
        Merchant,
        Religious,
        Family,
        Intelligence
    }

    [Serializable]
    public sealed class OrganizationState
    {
        public string Id;
        public string DisplayName;
        public OrganizationType Type;
        public string HeadquartersLocationId;
        public string LeaderPersonId;
        public long Treasury;
        public int ReputationBasisPoints = 5_000;
    }

    [Serializable]
    public sealed class PositionState
    {
        public string Id;
        public string OrganizationId;
        public string DisplayName;
        public int Rank;
        public int Capacity = 1;
    }

    [Serializable]
    public sealed class MembershipState
    {
        public string Id;
        public string PersonId;
        public string OrganizationId;
        public string PositionId;
        public long JoinedDay;
        public int LoyaltyBasisPoints = 5_000;
    }

    public enum CountyFiscalEntryType : byte
    {
        HouseholdAssessment,
        HouseholdPayment,
        GrainRemittance,
        AdministrationStipend,
        GrainRelief
    }

    [Serializable]
    public sealed class CountyGovernanceState
    {
        public string Id;
        public string CountyLocationId;
        public string GovernmentOrganizationId;
        public string AdministratorFamilyId;
        public int AnnualCashTaxRateBasisPoints = 300;
        public int LocalGrainRetentionBasisPoints = 4_000;
        public int RegistrationCoverageBasisPoints = 9_000;
        public int AdministrativeEfficiencyBasisPoints = 8_000;
        public int GentryInfluenceBasisPoints;
        public int LastMarketPressureBasisPoints = 10_000;
        public long CountyGranaryGrain;
        public long TotalMoneyTaxCollected;
        public long TotalGrainTaxReceived;
        public long TotalAdministrationPaid;
        public long TotalReliefGrain;
        public int LastPublicOrderChange;
        public long LastSettlementDay = -1;
        public long NextSettlementDay = 30;
    }

    [Serializable]
    public sealed class CountyGentryHouseState
    {
        public string Id;
        public string CountyGovernanceId;
        public string FamilyId;
        public int InfluenceBasisPoints;
        public int TaxComplianceBasisPoints = 10_000;
        public long TotalAssessmentReductionMoney;
    }

    [Serializable]
    public sealed class CountyHouseholdTaxState
    {
        public string Id;
        public string CountyGovernanceId;
        public string FamilyId;
        public long AssessedMoney;
        public long PaidMoney;
        public long ArrearsMoney;
        public long LastAssessmentDay = -1;
    }

    [Serializable]
    public sealed class CountyFiscalLedgerEntryState
    {
        public string Id;
        public long Day;
        public CountyFiscalEntryType Type;
        public string CountyGovernanceId;
        public string FamilyId;
        public string VillageId;
        public long FamilyMoneyDelta;
        public long GovernmentMoneyDelta;
        public long VillageGrainDelta;
        public long CountyGrainDelta;
        public long Amount;
        public string Summary;
    }
}
