using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class LivingWorldRuntimePlayModeTests
    {
        [Test]
        public void ConditionalHistoricalEventAppliesOffscreenInPlayMode()
        {
            var world = CreateWorld();
            world.HistoricalEventDefinitions.Add(
                Luoyang189190HistoricalEventPrototype.Create(
                    new Luoyang189190PrototypeBindings
                    {
                        EmperorPersonId = "person.emperor",
                        LuoyangLocationId = "location.luoyang",
                        AlternateLocationId = "location.changan",
                        GovernmentOrganizationId = "organization.han",
                        PalaceFacilityId = "facility.palace",
                        DestroyedFacilityId = "facility.gate",
                        EarliestDay = 0,
                        LatestDay = 30
                    }));

            var resolved = new HistoricalEventSystem().ResolveEligibleEvents(world);

            Assert.That(resolved.Count, Is.EqualTo(1));
            Assert.That(resolved[0].AppliedOffscreen, Is.True);
            Assert.That(world.Facilities.Find(item => item.Id == "facility.gate")
                .LifecycleStatus, Is.EqualTo(FacilityLifecycleStatus.Destroyed));
        }

        [Test]
        public void SimulationArenaRunsDeterministicDecisionInPlayMode()
        {
            var world = CreateWorld();
            var agent = new WorldDecisionAgentState
            {
                Id = "decision.person.emperor",
                AgentId = "person.emperor",
                AgentKind = WorldAgentKind.Person
            };
            world.WorldDecisionAgents.Add(agent);
            var run = new WorldSimulationArena().Run(
                world,
                new WorldSimulationArenaScenario
                {
                    Id = "arena.playmode.smoke",
                    WorldSeed = world.MasterSeed,
                    DurationDays = 1,
                    PolicySetId = DecisionPolicyIds.Rule,
                    AgentStateIds = new List<string> { agent.Id }
                },
                new RuleDecisionPolicy(),
                (_, __) => new[]
                {
                    new WorldActionIntent
                    {
                        Id = "action.observe.playmode",
                        ActionTypeId = WorldActionTypeIds.Observe,
                        AgentId = "person.emperor",
                        AgentKind = WorldAgentKind.Person,
                        LocationId = "location.luoyang"
                    }
                });

            Assert.That(run.DecisionTrace.Count, Is.EqualTo(2));
            Assert.That(run.DecisionTrace[0].ActionId,
                Is.EqualTo("action.observe.playmode"));
        }

        private static WorldState CreateWorld()
        {
            var world = WorldState.Create(184);
            world.Locations.Add(new LocationState
            {
                Id = "location.luoyang",
                DisplayName = "Luoyang",
                Population = 1,
                PublicOrderBasisPoints = 8_000
            });
            world.Locations.Add(new LocationState
            {
                Id = "location.changan",
                DisplayName = "Changan",
                Population = 1,
                PublicOrderBasisPoints = 8_000
            });
            world.People.Add(new PersonState
            {
                Id = "person.emperor",
                DisplayName = "Emperor",
                LocationId = "location.luoyang",
                BirthLocationId = "location.luoyang",
                FamilyId = "family.imperial"
            });
            world.Families.Add(new FamilyState
            {
                Id = "family.imperial",
                DisplayName = "Imperial family",
                HeadPersonId = "person.emperor",
                LocationId = "location.luoyang",
                MemberIds = new List<string> { "person.emperor" }
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = "organization.han",
                DisplayName = "Han government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = "location.luoyang",
                LeaderPersonId = "person.emperor"
            });
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = "facility_definition.palace",
                DisplayName = "Palace"
            });
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = "facility_definition.gate",
                DisplayName = "Gate"
            });
            world.Facilities.Add(new FacilityState
            {
                Id = "facility.palace",
                DisplayName = "Palace",
                DefinitionId = "facility_definition.palace",
                OwnerId = "organization.han",
                ControllerId = "organization.han",
                SettlementId = "location.luoyang"
            });
            world.Facilities.Add(new FacilityState
            {
                Id = "facility.gate",
                DisplayName = "Gate",
                DefinitionId = "facility_definition.gate",
                OwnerId = "organization.han",
                ControllerId = "organization.han",
                SettlementId = "location.luoyang"
            });
            return world;
        }
    }
}
