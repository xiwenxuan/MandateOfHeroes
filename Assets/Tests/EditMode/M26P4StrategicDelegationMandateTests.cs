using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void M26P4_MerchantLeaderCreatesBoundedPersistentMandate()
        {
            var world = PrototypeWorldFactory.Create184World();
            var organization = world.Organizations.Find(
                item => item.Id == "organization.zhongshan_merchants");
            var treasuryBefore = organization.Treasury;

            var mandate = M26P4CreateMerchantMandate(world);

            Assert.That(
                mandate.OrganizationId,
                Is.EqualTo("organization.zhongshan_merchants"));
            Assert.That(
                mandate.AssigneePersonId,
                Is.EqualTo("person.su_shuang"));
            Assert.That(mandate.IssuerWasOrganizationLeader, Is.True);
            Assert.That(
                mandate.AllowedOrderIdsSnapshot,
                Does.Contain(StrategicDelegationOrderIds.ManageCommerce));
            Assert.That(
                mandate.AllowedOrderIdsSnapshot,
                Does.Not.Contain(StrategicDelegationOrderIds.LaunchCampaign));
            Assert.That(organization.Treasury, Is.EqualTo(treasuryBefore));
            Assert.That(world.PersistentWorldCommands, Is.Empty);
        }

        [Test]
        public void M26P4_EvaluationRecordsProposalWithoutExecutingWorldCommand()
        {
            var world = PrototypeWorldFactory.Create184World();
            var mandate = M26P4CreateMerchantMandate(world);
            var organization = world.Organizations.Find(
                item => item.Id == mandate.OrganizationId);
            var treasuryBefore = organization.Treasury;
            var candidates = new List<StrategicDelegationBoundCandidate>
            {
                M26P4Candidate(
                    "candidate.zhongshan.transfer_funds",
                    StrategicDelegationOrderIds.TransferFunds,
                    StrategicDelegationPriorityIds.Logistics,
                    "command.strategic.transfer_funds",
                    500),
                M26P4Candidate(
                    "candidate.zhongshan.manage_commerce",
                    StrategicDelegationOrderIds.ManageCommerce,
                    StrategicDelegationPriorityIds.Commerce,
                    "command.strategic.manage_commerce",
                    800),
                M26P4Candidate(
                    "candidate.zhongshan.launch_campaign",
                    StrategicDelegationOrderIds.LaunchCampaign,
                    StrategicDelegationPriorityIds.Expansion,
                    "command.strategic.launch_campaign",
                    100)
            };

            var result = new StrategicDelegationMandateSystem()
                .EvaluateAndRecordProposal(
                    world,
                    mandate.Id,
                    "proposal.zhongshan.commerce.day_0",
                    candidates);

            Assert.That(result.HasProposal, Is.True);
            Assert.That(
                result.Proposal.CandidateId,
                Is.EqualTo("candidate.zhongshan.manage_commerce"));
            Assert.That(
                result.Proposal.CommandTypeId,
                Is.EqualTo("command.strategic.manage_commerce"));
            Assert.That(world.StrategicDelegationCommandProposals.Count,
                Is.EqualTo(1));
            Assert.That(world.PersistentWorldCommands, Is.Empty);
            Assert.That(organization.Treasury, Is.EqualTo(treasuryBefore));
        }

        [Test]
        public void M26P4_CandidateOutsideAssigneeBindingIsRejectedAtomically()
        {
            var world = PrototypeWorldFactory.Create184World();
            var mandate = M26P4CreateMerchantMandate(world);
            var candidate = M26P4Candidate(
                "candidate.zhongshan.invalid_actor",
                StrategicDelegationOrderIds.ManageCommerce,
                StrategicDelegationPriorityIds.Commerce,
                "command.strategic.manage_commerce",
                800);
            candidate.ActorPersonId = "person.zhang_shiping";

            Assert.Throws<InvalidOperationException>(() =>
                new StrategicDelegationMandateSystem()
                    .EvaluateAndRecordProposal(
                        world,
                        mandate.Id,
                        "proposal.zhongshan.invalid_actor",
                        new[] { candidate }));
            Assert.That(world.StrategicDelegationCommandProposals, Is.Empty);
            Assert.That(world.PersistentWorldCommands, Is.Empty);
        }

        [Test]
        public void M26P4_OverBudgetCandidateProducesNoProposalOrMutation()
        {
            var world = PrototypeWorldFactory.Create184World();
            var mandate = M26P4CreateMerchantMandate(world);
            var candidate = M26P4Candidate(
                "candidate.zhongshan.over_budget",
                StrategicDelegationOrderIds.ManageCommerce,
                StrategicDelegationPriorityIds.Commerce,
                "command.strategic.manage_commerce",
                mandate.BudgetLimit + 1);

            var result = new StrategicDelegationMandateSystem()
                .EvaluateAndRecordProposal(
                    world,
                    mandate.Id,
                    "proposal.zhongshan.over_budget",
                    new[] { candidate });

            Assert.That(result.HasProposal, Is.False);
            Assert.That(world.StrategicDelegationCommandProposals, Is.Empty);
            Assert.That(world.PersistentWorldCommands, Is.Empty);
        }

        [Test]
        public void M26P4_SnapshotRoundTripPreservesMandateAndProposal()
        {
            var world = PrototypeWorldFactory.Create184World();
            var mandate = M26P4CreateMerchantMandate(world);
            var candidate = M26P4Candidate(
                "candidate.zhongshan.round_trip",
                StrategicDelegationOrderIds.ManageCommerce,
                StrategicDelegationPriorityIds.Commerce,
                "command.strategic.manage_commerce",
                800);
            new StrategicDelegationMandateSystem().EvaluateAndRecordProposal(
                world,
                mandate.Id,
                "proposal.zhongshan.round_trip",
                new[] { candidate });

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.StrategicDelegationMandates.Count,
                Is.EqualTo(1));
            Assert.That(loaded.StrategicDelegationCommandProposals.Count,
                Is.EqualTo(1));
            Assert.That(
                loaded.StrategicDelegationCommandProposals[0].CandidateId,
                Is.EqualTo("candidate.zhongshan.round_trip"));
        }

        [Test]
        public void M26P4_MigratesVersionSixtyFiveWithoutInventingDelegation()
        {
            var world = PrototypeWorldFactory.Create184World();
            var json = WorldSnapshotSerializer.Serialize(world).Replace(
                "\"SchemaVersion\": " + WorldState.CurrentSchemaVersion,
                "\"SchemaVersion\": 65");

            var loaded = WorldSnapshotSerializer.Deserialize(json);

            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.StrategicDelegationMandates, Is.Empty);
            Assert.That(loaded.StrategicDelegationCommandProposals, Is.Empty);
        }

        private static StrategicDelegationMandateState
            M26P4CreateMerchantMandate(WorldState world)
        {
            var policy = StrategicDelegationPolicyCatalog.CreateCore().Find(
                item => item.Id ==
                    StrategicDelegationPolicyIds.CommerceGrowth);
            return new StrategicDelegationMandateSystem().CreateMandate(
                world,
                new StrategicDelegationMandateRequest
                {
                    Id = "mandate.strategic.zhongshan_merchant",
                    IssuerPersonId = "person.zhang_shiping",
                    AssigneePersonId = "person.su_shuang",
                    AssigneePositionId = "position.zhongshan_trader",
                    OrganizationId = "organization.zhongshan_merchants",
                    JurisdictionLocationId = "location.zhongshan",
                    ExpiresDay = checked(world.AbsoluteDay + 30),
                    BudgetLimit = 10_000,
                    ReportIntervalDays = 5
                },
                policy);
        }

        private static StrategicDelegationBoundCandidate M26P4Candidate(
            string candidateId,
            string orderId,
            string priorityId,
            string commandTypeId,
            long estimatedCost)
        {
            return new StrategicDelegationBoundCandidate
            {
                Candidate = new StrategicDelegationCandidate
                {
                    Id = candidateId,
                    OrderId = orderId,
                    PriorityImpacts = new List<
                        StrategicDelegationPriorityImpact>
                    {
                        new StrategicDelegationPriorityImpact
                        {
                            PriorityId = priorityId,
                            ImpactBasisPoints = 10_000
                        }
                    }
                },
                ActorPersonId = "person.su_shuang",
                OrganizationId = "organization.zhongshan_merchants",
                PositionId = "position.zhongshan_trader",
                JurisdictionLocationId = "location.zhongshan",
                CommandTypeId = commandTypeId,
                EstimatedCost = estimatedCost,
                Arguments = new List<WorldCommandArgumentState>
                {
                    new WorldCommandArgumentState
                    {
                        Key = "argument.location_id",
                        Value = "location.zhongshan"
                    }
                }
            };
        }
    }
}
