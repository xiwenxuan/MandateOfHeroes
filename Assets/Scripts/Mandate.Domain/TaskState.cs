using System;

namespace Mandate.Domain
{
    public enum TaskKind : byte
    {
        LocalWork,
        TravelDelivery
    }

    public enum TaskStatus : byte
    {
        Active,
        Completed,
        Failed,
        Abandoned
    }

    [Serializable]
    public sealed class TaskDefinitionState
    {
        public string Id;
        public string DisplayName;
        public TaskKind Kind;
        public string IssuerOrganizationId;
        public string RequiredPositionId;
        public string OriginLocationId;
        public string TargetLocationId;
        public int RequiredProgress;
        public int DurationDays;
        public long RewardMoney;
        public int RewardProvisions;
        public string TargetArmyId;
        public int ArmyProvisionReward;
        public bool RequiresMembership = true;
        public bool IsAvailable = true;
    }

    [Serializable]
    public sealed class TaskInstanceState
    {
        public string Id;
        public string DefinitionId;
        public string AssigneePersonId;
        public TaskStatus Status;
        public long AcceptedDay;
        public long DeadlineDay;
        public int Progress;
        public bool RewardClaimed;
    }
}
