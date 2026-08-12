using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class StrategicDelegationPriorityImpact
    {
        public string PriorityId;
        public int ImpactBasisPoints;
    }

    public sealed class StrategicDelegationCandidate
    {
        public string Id;
        public string OrderId;
        public int BaseUtilityBasisPoints;
        public List<StrategicDelegationPriorityImpact> PriorityImpacts =
            new List<StrategicDelegationPriorityImpact>();
    }

    public sealed class StrategicDelegationDecision
    {
        public bool HasSelection;
        public string CandidateId;
        public long ScoreBasisPoints;
    }

    public sealed class StrategicDelegationPolicySystem
    {
        public StrategicDelegationDecision Select(
            StrategicDelegationPolicyDefinitionState policy,
            IList<StrategicDelegationCandidate> candidates)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            policy.Validate();
            var allowedOrders = new HashSet<string>(
                policy.AllowedOrderIds,
                StringComparer.Ordinal);
            var weights = BuildWeights(policy.PriorityWeights);
            StrategicDelegationCandidate selected = null;
            var selectedScore = long.MinValue;
            var candidateIds = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                ValidateCandidate(candidate, candidateIds);
                if (!allowedOrders.Contains(candidate.OrderId))
                {
                    continue;
                }

                var score = Score(candidate, weights);
                if (selected == null || score > selectedScore ||
                    score == selectedScore && string.CompareOrdinal(
                        candidate.Id,
                        selected.Id) < 0)
                {
                    selected = candidate;
                    selectedScore = score;
                }
            }

            return selected == null
                ? new StrategicDelegationDecision()
                : new StrategicDelegationDecision
                {
                    HasSelection = true,
                    CandidateId = selected.Id,
                    ScoreBasisPoints = selectedScore
                };
        }

        private static Dictionary<string, int> BuildWeights(
            IList<StrategicDelegationPriorityWeightState> priorities)
        {
            var weights = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < priorities.Count; i++)
            {
                weights.Add(
                    priorities[i].PriorityId,
                    priorities[i].WeightBasisPoints);
            }

            return weights;
        }

        private static void ValidateCandidate(
            StrategicDelegationCandidate candidate,
            ISet<string> candidateIds)
        {
            if (candidate == null)
            {
                throw new InvalidOperationException(
                    "Strategic delegation candidates cannot be null.");
            }

            _ = new StableId(candidate.Id);
            _ = new StableId(candidate.OrderId);
            if (!candidateIds.Add(candidate.Id))
            {
                throw new InvalidOperationException(
                    "Strategic delegation candidate IDs must be unique.");
            }

            if (candidate.BaseUtilityBasisPoints < -10_000 ||
                candidate.BaseUtilityBasisPoints > 10_000)
            {
                throw new InvalidOperationException(
                    "Candidate base utility must be between -10000 and 10000 basis points.");
            }

            var priorityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < candidate.PriorityImpacts.Count; i++)
            {
                var impact = candidate.PriorityImpacts[i];
                if (impact == null)
                {
                    throw new InvalidOperationException(
                        "Strategic delegation priority impacts cannot be null.");
                }

                _ = new StableId(impact.PriorityId);
                if (!priorityIds.Add(impact.PriorityId))
                {
                    throw new InvalidOperationException(
                        "Candidate priority impact IDs must be unique.");
                }

                if (impact.ImpactBasisPoints < -10_000 ||
                    impact.ImpactBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        "Candidate impacts must be between -10000 and 10000 basis points.");
                }
            }
        }

        private static long Score(
            StrategicDelegationCandidate candidate,
            IReadOnlyDictionary<string, int> weights)
        {
            long score = candidate.BaseUtilityBasisPoints;
            for (var i = 0; i < candidate.PriorityImpacts.Count; i++)
            {
                var impact = candidate.PriorityImpacts[i];
                if (weights.TryGetValue(impact.PriorityId, out var weight))
                {
                    score += (long)weight * impact.ImpactBasisPoints / 10_000;
                }
            }

            return score;
        }
    }
}
