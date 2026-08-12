using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class M26P3StrategicDelegationPolicyTests
    {
        [Test]
        public void CommercePolicy_SelectsCommerceAndRejectsCampaign()
        {
            var policy = FindPolicy(
                StrategicDelegationPolicyIds.CommerceGrowth);
            var candidates = new List<StrategicDelegationCandidate>
            {
                Candidate(
                    "candidate.manage_agriculture",
                    StrategicDelegationOrderIds.ManageAgriculture,
                    StrategicDelegationPriorityIds.Agriculture),
                Candidate(
                    "candidate.manage_commerce",
                    StrategicDelegationOrderIds.ManageCommerce,
                    StrategicDelegationPriorityIds.Commerce),
                Candidate(
                    "candidate.launch_campaign",
                    StrategicDelegationOrderIds.LaunchCampaign,
                    StrategicDelegationPriorityIds.Expansion)
            };

            var decision = new StrategicDelegationPolicySystem().Select(
                policy,
                candidates);

            Assert.That(decision.HasSelection, Is.True);
            Assert.That(
                decision.CandidateId,
                Is.EqualTo("candidate.manage_commerce"));
        }

        [Test]
        public void EqualScores_UseStableCandidateIdOrder()
        {
            var policy = FindPolicy(StrategicDelegationPolicyIds.Balanced);
            var candidates = new List<StrategicDelegationCandidate>
            {
                Candidate(
                    "candidate.z",
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationPriorityIds.Logistics),
                Candidate(
                    "candidate.a",
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationPriorityIds.Logistics)
            };

            var decision = new StrategicDelegationPolicySystem().Select(
                policy,
                candidates);

            Assert.That(decision.CandidateId, Is.EqualTo("candidate.a"));
        }

        [Test]
        public void DuplicateCandidateIds_AreRejected()
        {
            var policy = FindPolicy(StrategicDelegationPolicyIds.Balanced);
            var candidates = new List<StrategicDelegationCandidate>
            {
                Candidate(
                    "candidate.same",
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationPriorityIds.Logistics),
                Candidate(
                    "candidate.same",
                    StrategicDelegationOrderIds.TransferFunds,
                    StrategicDelegationPriorityIds.Logistics)
            };

            Assert.Throws<InvalidOperationException>(() =>
                new StrategicDelegationPolicySystem().Select(
                    policy,
                    candidates));
        }

        [Test]
        public void CorePolicies_UseValidUniqueStableIds()
        {
            var policies = StrategicDelegationPolicyCatalog.CreateCore();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(policies.Count, Is.EqualTo(3));
            for (var i = 0; i < policies.Count; i++)
            {
                policies[i].Validate();
                Assert.That(ids.Add(policies[i].Id), Is.True);
            }
        }

        private static StrategicDelegationCandidate Candidate(
            string id,
            string orderId,
            string priorityId)
        {
            return new StrategicDelegationCandidate
            {
                Id = id,
                OrderId = orderId,
                PriorityImpacts = new List<StrategicDelegationPriorityImpact>
                {
                    new StrategicDelegationPriorityImpact
                    {
                        PriorityId = priorityId,
                        ImpactBasisPoints = 10_000
                    }
                }
            };
        }

        private static StrategicDelegationPolicyDefinitionState FindPolicy(
            string id)
        {
            return StrategicDelegationPolicyCatalog.CreateCore().Find(
                item => item.Id == id);
        }
    }
}
