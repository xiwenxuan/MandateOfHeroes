using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum StrategicDelegationMandateStatus : byte
    {
        Active,
        Revoked,
        Completed
    }

    public enum StrategicDelegationProposalStatus : byte
    {
        Proposed,
        Accepted,
        Rejected,
        Cancelled
    }

    [Serializable]
    public sealed class StrategicDelegationMandateState
    {
        public string Id;
        public string PolicyId;
        public string IssuerPersonId;
        public string IssuerPositionId;
        public string AssigneePersonId;
        public string AssigneePositionId;
        public string OrganizationId;
        public string JurisdictionLocationId;
        public long IssuedDay;
        public long ExpiresDay;
        public long BudgetLimit;
        public int ReportIntervalDays = 1;
        public long NextReportDay;
        public int IssuerRankAtIssue;
        public int AssigneeRankAtIssue;
        public bool IssuerWasOrganizationLeader;
        public bool PolicyAutoExecuteAuthorized;
        public StrategicDelegationMandateStatus Status =
            StrategicDelegationMandateStatus.Active;
        public List<string> AllowedOrderIdsSnapshot = new List<string>();
        public List<StrategicDelegationPriorityWeightState>
            PriorityWeightsSnapshot =
                new List<StrategicDelegationPriorityWeightState>();

        public void ValidateContract()
        {
            _ = new StableId(Id);
            _ = new StableId(PolicyId);
            _ = new StableId(IssuerPersonId);
            _ = new StableId(AssigneePersonId);
            _ = new StableId(AssigneePositionId);
            _ = new StableId(OrganizationId);
            _ = new StableId(JurisdictionLocationId);
            if (!string.IsNullOrEmpty(IssuerPositionId))
            {
                _ = new StableId(IssuerPositionId);
            }

            if (!Enum.IsDefined(
                    typeof(StrategicDelegationMandateStatus),
                    Status) ||
                IssuedDay < 0 ||
                ExpiresDay < IssuedDay ||
                BudgetLimit < 0 ||
                ReportIntervalDays <= 0 ||
                NextReportDay < IssuedDay ||
                IssuerRankAtIssue < 0 ||
                AssigneeRankAtIssue < 0 ||
                !IssuerWasOrganizationLeader &&
                string.IsNullOrEmpty(IssuerPositionId) ||
                !IssuerWasOrganizationLeader &&
                IssuerRankAtIssue <= AssigneeRankAtIssue)
            {
                throw new InvalidOperationException(
                    $"Invalid strategic delegation mandate {Id}.");
            }

            if (AllowedOrderIdsSnapshot == null ||
                AllowedOrderIdsSnapshot.Count == 0 ||
                PriorityWeightsSnapshot == null)
            {
                throw new InvalidOperationException(
                    $"Mandate {Id} has no valid authority snapshot.");
            }

            var orderIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < AllowedOrderIdsSnapshot.Count; i++)
            {
                var orderId = new StableId(
                    AllowedOrderIdsSnapshot[i]).Value;
                if (!orderIds.Add(orderId))
                {
                    throw new InvalidOperationException(
                        $"Mandate {Id} contains duplicate order authority.");
                }
            }

            var priorityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < PriorityWeightsSnapshot.Count; i++)
            {
                var weight = PriorityWeightsSnapshot[i];
                if (weight == null)
                {
                    throw new InvalidOperationException(
                        $"Mandate {Id} contains a null priority weight.");
                }

                var priorityId = new StableId(weight.PriorityId).Value;
                if (!priorityIds.Add(priorityId) ||
                    weight.WeightBasisPoints < -10_000 ||
                    weight.WeightBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        $"Mandate {Id} contains an invalid priority weight.");
                }
            }
        }
    }

    [Serializable]
    public sealed class StrategicDelegationCommandProposalState
    {
        public string Id;
        public string MandateId;
        public string CandidateId;
        public string OrderId;
        public string CommandTypeId;
        public string ActorPersonId;
        public string OrganizationId;
        public string JurisdictionLocationId;
        public long CreatedDay;
        public long EstimatedCost;
        public long ScoreBasisPoints;
        public StrategicDelegationProposalStatus Status =
            StrategicDelegationProposalStatus.Proposed;
        public List<WorldCommandArgumentState> Arguments =
            new List<WorldCommandArgumentState>();

        public void ValidateContract()
        {
            _ = new StableId(Id);
            _ = new StableId(MandateId);
            _ = new StableId(CandidateId);
            _ = new StableId(OrderId);
            _ = new StableId(CommandTypeId);
            _ = new StableId(ActorPersonId);
            _ = new StableId(OrganizationId);
            _ = new StableId(JurisdictionLocationId);
            if (CreatedDay < 0 || EstimatedCost < 0 ||
                !Enum.IsDefined(
                    typeof(StrategicDelegationProposalStatus),
                    Status) ||
                Arguments == null)
            {
                throw new InvalidOperationException(
                    $"Invalid strategic delegation proposal {Id}.");
            }

            var argumentIds = new HashSet<string>(StringComparer.Ordinal);
            string previousKey = null;
            for (var i = 0; i < Arguments.Count; i++)
            {
                var argument = Arguments[i];
                if (argument == null || argument.Value == null)
                {
                    throw new InvalidOperationException(
                        $"Proposal {Id} contains an invalid argument.");
                }

                var key = new StableId(argument.Key).Value;
                if (!argumentIds.Add(key) ||
                    previousKey != null &&
                    string.CompareOrdinal(previousKey, key) >= 0)
                {
                    throw new InvalidOperationException(
                        $"Proposal {Id} contains duplicate or unordered arguments.");
                }

                previousKey = key;
            }
        }
    }

    public static class OrganizationStrategicDelegationCapabilityCatalog
    {
        public static List<string> CreateAllowedOrders(OrganizationType type)
        {
            switch (type)
            {
                case OrganizationType.Government:
                    return Orders(
                        StrategicDelegationOrderIds.ManageAgriculture,
                        StrategicDelegationOrderIds.ManageCommerce,
                        StrategicDelegationOrderIds.TransferFood,
                        StrategicDelegationOrderIds.TransferFunds,
                        StrategicDelegationOrderIds.RecruitPeople,
                        StrategicDelegationOrderIds.Investigate,
                        StrategicDelegationOrderIds.BuildInfrastructure);
                case OrganizationType.Military:
                    return Orders(
                        StrategicDelegationOrderIds.TransferFood,
                        StrategicDelegationOrderIds.TransferFunds,
                        StrategicDelegationOrderIds.RecruitPeople,
                        StrategicDelegationOrderIds.TrainForces,
                        StrategicDelegationOrderIds.FormMilitary,
                        StrategicDelegationOrderIds.TransferMilitary,
                        StrategicDelegationOrderIds.Investigate,
                        StrategicDelegationOrderIds.BuildInfrastructure,
                        StrategicDelegationOrderIds.LaunchCampaign);
                case OrganizationType.Merchant:
                    return Orders(
                        StrategicDelegationOrderIds.ManageCommerce,
                        StrategicDelegationOrderIds.TransferFood,
                        StrategicDelegationOrderIds.TransferFunds,
                        StrategicDelegationOrderIds.Investigate,
                        StrategicDelegationOrderIds.BuildInfrastructure);
                case OrganizationType.Family:
                    return Orders(
                        StrategicDelegationOrderIds.ManageAgriculture,
                        StrategicDelegationOrderIds.ManageCommerce,
                        StrategicDelegationOrderIds.TransferFood,
                        StrategicDelegationOrderIds.TransferFunds,
                        StrategicDelegationOrderIds.Investigate,
                        StrategicDelegationOrderIds.BuildInfrastructure);
                case OrganizationType.Religious:
                    return Orders(
                        StrategicDelegationOrderIds.ManageAgriculture,
                        StrategicDelegationOrderIds.Investigate,
                        StrategicDelegationOrderIds.BuildInfrastructure);
                case OrganizationType.Intelligence:
                    return Orders(StrategicDelegationOrderIds.Investigate);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static List<string> Orders(params string[] ids) =>
            new List<string>(ids);
    }
}
