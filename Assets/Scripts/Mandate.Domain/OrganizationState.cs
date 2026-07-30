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
}
