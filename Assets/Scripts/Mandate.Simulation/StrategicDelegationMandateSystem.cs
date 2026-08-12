using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class StrategicDelegationMandateRequest
    {
        public string Id;
        public string IssuerPersonId;
        public string AssigneePersonId;
        public string AssigneePositionId;
        public string OrganizationId;
        public string JurisdictionLocationId;
        public long ExpiresDay;
        public long BudgetLimit;
        public int ReportIntervalDays = 1;
    }

    public sealed class StrategicDelegationBoundCandidate
    {
        public StrategicDelegationCandidate Candidate;
        public string ActorPersonId;
        public string OrganizationId;
        public string PositionId;
        public string JurisdictionLocationId;
        public string CommandTypeId;
        public long EstimatedCost;
        public List<WorldCommandArgumentState> Arguments =
            new List<WorldCommandArgumentState>();
    }

    public sealed class StrategicDelegationProposalResult
    {
        public bool HasProposal { get; }
        public StrategicDelegationCommandProposalState Proposal { get; }
        public string Message { get; }

        public StrategicDelegationProposalResult(
            bool hasProposal,
            StrategicDelegationCommandProposalState proposal,
            string message)
        {
            HasProposal = hasProposal;
            Proposal = proposal;
            Message = message ?? string.Empty;
        }
    }

    public sealed class StrategicDelegationMandateSystem
    {
        private readonly StrategicDelegationPolicySystem _policySystem =
            new StrategicDelegationPolicySystem();

        public StrategicDelegationMandateState CreateMandate(
            WorldState world,
            StrategicDelegationMandateRequest request,
            StrategicDelegationPolicyDefinitionState policy)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            policy.Validate();
            _ = new StableId(request.Id);
            EnsureMissingMandate(world, request.Id);
            var issuer = FindLivingPerson(world, request.IssuerPersonId);
            var assignee = FindLivingPerson(world, request.AssigneePersonId);
            if (issuer.Id == assignee.Id)
            {
                throw new InvalidOperationException(
                    "A strategic delegation mandate needs a distinct assignee.");
            }

            var organization = FindOrganization(
                world,
                request.OrganizationId);
            var assigneePosition = FindPosition(
                world,
                request.AssigneePositionId);
            if (assigneePosition.OrganizationId != organization.Id ||
                !HasExactMembership(
                    world,
                    assignee.Id,
                    organization.Id,
                    assigneePosition.Id))
            {
                throw new InvalidOperationException(
                    "The assignee does not currently hold the delegated position.");
            }

            var issuerMembership = FindMembership(
                world,
                issuer.Id,
                organization.Id);
            var issuerPosition = issuerMembership == null
                ? null
                : FindPosition(world, issuerMembership.PositionId);
            var issuerIsLeader = organization.LeaderPersonId == issuer.Id;
            if (!issuerIsLeader &&
                (issuerPosition == null ||
                 issuerPosition.Rank <= assigneePosition.Rank))
            {
                throw new InvalidOperationException(
                    "The issuer lacks authority over the assignee position.");
            }

            var jurisdiction = FindLocation(
                world,
                request.JurisdictionLocationId);
            if (!IsSameOrDescendant(
                    world,
                    jurisdiction.Id,
                    organization.HeadquartersLocationId))
            {
                throw new InvalidOperationException(
                    "The delegated jurisdiction is outside the organization headquarters scope.");
            }

            if (request.ExpiresDay < world.AbsoluteDay ||
                request.BudgetLimit < 0 ||
                request.BudgetLimit > organization.Treasury ||
                request.ReportIntervalDays <= 0)
            {
                throw new InvalidOperationException(
                    "The strategic delegation duration, budget, or report interval is invalid.");
            }

            var capability = new HashSet<string>(
                OrganizationStrategicDelegationCapabilityCatalog
                    .CreateAllowedOrders(organization.Type),
                StringComparer.Ordinal);
            var allowedOrders = new List<string>();
            for (var i = 0; i < policy.AllowedOrderIds.Count; i++)
            {
                if (capability.Contains(policy.AllowedOrderIds[i]))
                {
                    allowedOrders.Add(policy.AllowedOrderIds[i]);
                }
            }

            allowedOrders.Sort(StringComparer.Ordinal);
            if (allowedOrders.Count == 0)
            {
                throw new InvalidOperationException(
                    "The policy grants no orders supported by this organization.");
            }

            var mandate = new StrategicDelegationMandateState
            {
                Id = request.Id,
                PolicyId = policy.Id,
                IssuerPersonId = issuer.Id,
                IssuerPositionId = issuerPosition?.Id ?? string.Empty,
                AssigneePersonId = assignee.Id,
                AssigneePositionId = assigneePosition.Id,
                OrganizationId = organization.Id,
                JurisdictionLocationId = jurisdiction.Id,
                IssuedDay = world.AbsoluteDay,
                ExpiresDay = request.ExpiresDay,
                BudgetLimit = request.BudgetLimit,
                ReportIntervalDays = request.ReportIntervalDays,
                NextReportDay = checked(
                    world.AbsoluteDay + request.ReportIntervalDays),
                IssuerRankAtIssue = issuerPosition?.Rank ?? 0,
                AssigneeRankAtIssue = assigneePosition.Rank,
                IssuerWasOrganizationLeader = issuerIsLeader,
                PolicyAutoExecuteAuthorized = policy.AutoExecute,
                Status = StrategicDelegationMandateStatus.Active,
                AllowedOrderIdsSnapshot = allowedOrders,
                PriorityWeightsSnapshot = CopyWeights(
                    policy.PriorityWeights)
            };
            mandate.ValidateContract();
            world.StrategicDelegationMandates.Add(mandate);
            try
            {
                world.Validate();
            }
            catch
            {
                world.StrategicDelegationMandates.Remove(mandate);
                throw;
            }
            return mandate;
        }

        public StrategicDelegationProposalResult EvaluateAndRecordProposal(
            WorldState world,
            string mandateId,
            string proposalId,
            IList<StrategicDelegationBoundCandidate> candidates)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            _ = new StableId(proposalId);
            EnsureMissingProposal(world, proposalId);
            var mandate = FindMandate(world, mandateId);
            mandate.ValidateContract();
            if (mandate.Status != StrategicDelegationMandateStatus.Active ||
                world.AbsoluteDay > mandate.ExpiresDay)
            {
                return new StrategicDelegationProposalResult(
                    false,
                    null,
                    "委任已经失效，未生成命令提案。");
            }

            var organization = FindOrganization(world, mandate.OrganizationId);
            var assignee = FindLivingPerson(world, mandate.AssigneePersonId);
            if (!HasExactMembership(
                    world,
                    assignee.Id,
                    mandate.OrganizationId,
                    mandate.AssigneePositionId))
            {
                return new StrategicDelegationProposalResult(
                    false,
                    null,
                    "受任者已经离任，未生成命令提案。");
            }

            var candidateIds = new HashSet<string>(StringComparer.Ordinal);
            var eligiblePolicyCandidates =
                new List<StrategicDelegationCandidate>();
            var boundById = new Dictionary<
                string,
                StrategicDelegationBoundCandidate>(StringComparer.Ordinal);
            for (var i = 0; i < candidates.Count; i++)
            {
                var bound = candidates[i];
                ValidateBinding(world, mandate, bound);
                if (!candidateIds.Add(bound.Candidate.Id))
                {
                    throw new InvalidOperationException(
                        "Strategic delegation candidate IDs must be unique.");
                }

                if (bound.EstimatedCost > mandate.BudgetLimit ||
                    bound.EstimatedCost > organization.Treasury)
                {
                    continue;
                }

                eligiblePolicyCandidates.Add(bound.Candidate);
                boundById.Add(bound.Candidate.Id, bound);
            }

            var policy = new StrategicDelegationPolicyDefinitionState
            {
                Id = mandate.PolicyId,
                DisplayName = mandate.PolicyId,
                Description = "Persisted mandate policy snapshot.",
                AutoExecute = mandate.PolicyAutoExecuteAuthorized,
                AllowedOrderIds = new List<string>(
                    mandate.AllowedOrderIdsSnapshot),
                PriorityWeights = CopyWeights(
                    mandate.PriorityWeightsSnapshot)
            };
            var decision = _policySystem.Select(
                policy,
                eligiblePolicyCandidates);
            if (!decision.HasSelection)
            {
                return new StrategicDelegationProposalResult(
                    false,
                    null,
                    "没有同时满足权限、辖区与预算的命令候选。");
            }

            var selected = boundById[decision.CandidateId];
            var proposal = new StrategicDelegationCommandProposalState
            {
                Id = proposalId,
                MandateId = mandate.Id,
                CandidateId = selected.Candidate.Id,
                OrderId = selected.Candidate.OrderId,
                CommandTypeId = selected.CommandTypeId,
                ActorPersonId = mandate.AssigneePersonId,
                OrganizationId = mandate.OrganizationId,
                JurisdictionLocationId =
                    selected.JurisdictionLocationId,
                CreatedDay = world.AbsoluteDay,
                EstimatedCost = selected.EstimatedCost,
                ScoreBasisPoints = decision.ScoreBasisPoints,
                Status = StrategicDelegationProposalStatus.Proposed,
                Arguments = CopyArguments(selected.Arguments)
            };
            proposal.ValidateContract();
            world.StrategicDelegationCommandProposals.Add(proposal);
            try
            {
                world.Validate();
            }
            catch
            {
                world.StrategicDelegationCommandProposals.Remove(proposal);
                throw;
            }
            return new StrategicDelegationProposalResult(
                true,
                proposal,
                "战略委任已生成待处理命令提案。");
        }

        private static void ValidateBinding(
            WorldState world,
            StrategicDelegationMandateState mandate,
            StrategicDelegationBoundCandidate bound)
        {
            if (bound == null || bound.Candidate == null)
            {
                throw new InvalidOperationException(
                    "Strategic delegation candidates cannot be null.");
            }

            _ = new StableId(bound.Candidate.Id);
            _ = new StableId(bound.Candidate.OrderId);
            _ = new StableId(bound.CommandTypeId);
            if (bound.ActorPersonId != mandate.AssigneePersonId ||
                bound.OrganizationId != mandate.OrganizationId ||
                bound.PositionId != mandate.AssigneePositionId ||
                bound.EstimatedCost < 0 ||
                !IsSameOrDescendant(
                    world,
                    bound.JurisdictionLocationId,
                    mandate.JurisdictionLocationId))
            {
                throw new InvalidOperationException(
                    $"Candidate {bound.Candidate.Id} is outside its mandate binding.");
            }

            ValidateArguments(bound.Arguments);
        }

        private static List<StrategicDelegationPriorityWeightState>
            CopyWeights(
                IList<StrategicDelegationPriorityWeightState> source)
        {
            var result = new List<
                StrategicDelegationPriorityWeightState>();
            for (var i = 0; i < source.Count; i++)
            {
                result.Add(new StrategicDelegationPriorityWeightState
                {
                    PriorityId = source[i].PriorityId,
                    WeightBasisPoints = source[i].WeightBasisPoints
                });
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.PriorityId, right.PriorityId));
            return result;
        }

        private static List<WorldCommandArgumentState> CopyArguments(
            IList<WorldCommandArgumentState> source)
        {
            ValidateArguments(source);
            var result = new List<WorldCommandArgumentState>();
            for (var i = 0; i < source.Count; i++)
            {
                result.Add(new WorldCommandArgumentState
                {
                    Key = source[i].Key,
                    Value = source[i].Value
                });
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static void ValidateArguments(
            IList<WorldCommandArgumentState> arguments)
        {
            if (arguments == null)
            {
                throw new InvalidOperationException(
                    "Strategic command arguments cannot be null.");
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (argument == null || argument.Value == null)
                {
                    throw new InvalidOperationException(
                        "Strategic command arguments cannot contain null values.");
                }

                var key = new StableId(argument.Key).Value;
                if (!keys.Add(key))
                {
                    throw new InvalidOperationException(
                        "Strategic command argument keys must be unique.");
                }
            }
        }

        private static bool IsSameOrDescendant(
            WorldState world,
            string locationId,
            string rootLocationId)
        {
            var cursor = FindLocation(world, locationId);
            while (cursor != null)
            {
                if (cursor.Id == rootLocationId)
                {
                    return true;
                }

                cursor = string.IsNullOrEmpty(cursor.ParentLocationId)
                    ? null
                    : FindLocation(world, cursor.ParentLocationId);
            }

            return false;
        }

        private static bool HasExactMembership(
            WorldState world,
            string personId,
            string organizationId,
            string positionId)
        {
            var membership = FindMembership(world, personId, organizationId);
            return membership != null && membership.PositionId == positionId;
        }

        private static MembershipState FindMembership(
            WorldState world,
            string personId,
            string organizationId)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var item = world.Memberships[i];
                if (item.PersonId == personId &&
                    item.OrganizationId == organizationId)
                {
                    return item;
                }
            }

            return null;
        }

        private static PersonState FindLivingPerson(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    if (!world.People[i].IsAlive)
                    {
                        throw new InvalidOperationException(
                            $"Person {personId} is not alive.");
                    }

                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private static PositionState FindPosition(
            WorldState world,
            string positionId)
        {
            for (var i = 0; i < world.Positions.Count; i++)
            {
                if (world.Positions[i].Id == positionId)
                {
                    return world.Positions[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing position {positionId}.");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing location {locationId}.");
        }

        private static StrategicDelegationMandateState FindMandate(
            WorldState world,
            string mandateId)
        {
            for (var i = 0; i < world.StrategicDelegationMandates.Count; i++)
            {
                if (world.StrategicDelegationMandates[i].Id == mandateId)
                {
                    return world.StrategicDelegationMandates[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing strategic delegation mandate {mandateId}.");
        }

        private static void EnsureMissingMandate(
            WorldState world,
            string mandateId)
        {
            for (var i = 0; i < world.StrategicDelegationMandates.Count; i++)
            {
                if (world.StrategicDelegationMandates[i].Id == mandateId)
                {
                    throw new InvalidOperationException(
                        $"Strategic delegation mandate {mandateId} already exists.");
                }
            }
        }

        private static void EnsureMissingProposal(
            WorldState world,
            string proposalId)
        {
            for (var i = 0;
                 i < world.StrategicDelegationCommandProposals.Count;
                 i++)
            {
                if (world.StrategicDelegationCommandProposals[i].Id ==
                    proposalId)
                {
                    throw new InvalidOperationException(
                        $"Strategic delegation proposal {proposalId} already exists.");
                }
            }
        }
    }
}
