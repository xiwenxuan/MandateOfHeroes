using System;

namespace Mandate.Domain
{
    public enum MilitaryServiceRole : byte
    {
        Commander,
        Officer,
        Soldier,
        Medic,
        Quartermaster,
        Messenger
    }

    public enum MilitaryServiceStatus : byte
    {
        Mustering,
        Active,
        Wounded,
        Straggler,
        Deserter,
        Captured,
        Retired,
        Dead
    }

    public enum MilitaryFormationKind : byte
    {
        Army,
        Detachment,
        Unit
    }

    public enum MilitaryAuthorityLevel : byte
    {
        None,
        Self,
        Formation,
        Army
    }

    public enum MilitaryOrderType : byte
    {
        March,
        Engage,
        Retreat,
        Resupply
    }

    public enum MilitaryOrderResult : byte
    {
        Authorized,
        Rejected
    }

    [Serializable]
    public sealed class MilitaryFormationState
    {
        public string Id;
        public string ArmyId;
        public string ParentFormationId;
        public string DisplayName;
        public MilitaryFormationKind Kind;
        public string CommanderPersonId;
        public int AuthorizedStrength;
        public int DisplayOrder;
    }

    [Serializable]
    public sealed class MilitaryServiceState
    {
        public string Id;
        public string PersonId;
        public string ArmyId;
        public string FormationId;
        public MilitaryServiceRole Role;
        public int Rank;
        public MilitaryServiceStatus Status;
        public int DisciplineBasisPoints = 5_000;
        public int LoyaltyBasisPoints = 5_000;
        public int ServiceExperienceBasisPoints;
        public long EnlistedDay;
        public long LastStatusChangeDay;
    }

    [Serializable]
    public sealed class MilitaryOrderState
    {
        public string Id;
        public long Day;
        public string IssuerPersonId;
        public string ArmyId;
        public string FormationId;
        public MilitaryOrderType Type;
        public MilitaryAuthorityLevel RequiredAuthority;
        public MilitaryAuthorityLevel ActualAuthority;
        public MilitaryOrderResult Result;
        public string TargetLocationId;
        public string TargetArmyId;
        public string Summary;
    }
}
