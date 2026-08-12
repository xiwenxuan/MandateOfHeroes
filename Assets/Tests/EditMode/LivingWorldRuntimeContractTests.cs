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
        public void HistoricalReferenceDoesNotDriveRuntimeTest()
        {
            var world = CreateWorld();
            var before = world.ProductBatches.Count;
            _ = Context(world, 0);
            Assert.That(world.ProductBatches.Count, Is.EqualTo(before));
        }

        [Test]
        public void ScenarioSnapshotInitializationOnlyTest()
        {
            var world = CreateEventWorld();
            var definition = CreatePrototype(world.AbsoluteDay + 10);
            world.HistoricalEventDefinitions.Add(definition);
            new HistoricalEventSystem().ResolveEligibleEvents(world);
            Assert.That(world.Facilities[0].LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Operational));
        }

        [Test]
        public void FutureSnapshotDoesNotOverwriteRuntimeTest()
        {
            var world = CreateWorld();
            world.Locations[0].GrainPrice = 777;
            _ = Context(world, 0);
            Assert.That(world.Locations[0].GrainPrice, Is.EqualTo(777));
        }

        [Test]
        public void PopulationIncreaseRaisesDemandTest()
        {
            var world = CreateWorld();
            var before = Signal(Context(world, 0), WorldSignalIds.FoodPressure);
            world.Locations[0].Population *= 4;
            var after = Signal(Context(world, 1), WorldSignalIds.FoodPressure);
            Assert.That(after, Is.GreaterThan(before));
        }

        [Test]
        public void PopulationDeclineReducesLaborTest()
        {
            var world = CreateWorld();
            var before = Signal(Context(world, 0),
                WorldSignalIds.LaborAvailability);
            world.People[0].IsAlive = false;
            var after = Signal(Context(world, 1),
                WorldSignalIds.LaborAvailability);
            Assert.That(after, Is.LessThan(before));
        }

        [Test]
        public void DemandDoesNotDirectlyCreateFacilityTest()
        {
            var world = CreateWorld();
            world.Locations[0].Population = 100_000;
            var before = world.Facilities.Count;
            _ = Context(world, 0);
            Assert.That(world.Facilities.Count, Is.EqualTo(before));
        }

        [Test]
        public void ConstructionRequiresLegalActionTest()
        {
            var world = CreateWorld();
            var action = Action(WorldActionTypeIds.BuildFacility);
            action.Arguments.Add(Arg("facility_definition_id", "facility.house"));
            action.Arguments.Add(Arg("owner_id", world.Families[0].Id));
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = "facility.house",
                DisplayName = "House"
            });
            var result = new WorldActionValidator().Validate(world, action);
            Assert.That(result.Status,
                Is.EqualTo(WorldActionValidationStatus.Deferred));
            Assert.That(result.ReasonId, Is.EqualTo("cell_selection_required"));
        }

        [Test]
        public void ShortageDoesNotMagicImportTest()
        {
            var world = CreateWorld();
            world.Families[0].Grain = 0;
            var before = world.ProductBatches.Count;
            Assert.That(Signal(Context(world, 0), WorldSignalIds.FoodPressure),
                Is.GreaterThan(0));
            Assert.That(world.ProductBatches.Count, Is.EqualTo(before));
        }

        [Test]
        public void TradeOrderRequiresRealInventoryTest()
        {
            var world = CreateWorld();
            var action = InventoryAction(WorldActionTypeIds.CreateTradeOrder, 10);
            var result = new WorldActionValidator().Validate(world, action);
            Assert.That(result.Status,
                Is.EqualTo(WorldActionValidationStatus.Invalid));
            Assert.That(result.ReasonId, Is.EqualTo("source_container_missing"));
        }

        [Test]
        public void GovernmentTransferRequiresInventoryTest()
        {
            var world = CreateWorld();
            var action = InventoryAction(
                WorldActionTypeIds.CreateGovernmentPurchase, 10);
            Assert.That(new WorldActionValidator().Validate(world, action).CanExecute,
                Is.False);
        }

        [Test]
        public void MilitarySupplyRequiresInventoryTest()
        {
            var world = CreateWorld();
            var action = InventoryAction(
                WorldActionTypeIds.CreateMilitarySupplyOrder, 10);
            Assert.That(new WorldActionValidator().Validate(world, action).CanExecute,
                Is.False);
        }

        [Test]
        public void RulePolicyProducesLegalActionTest()
        {
            var world = CreateWorld();
            var decision = new RuleDecisionPolicy().Decide(
                world, Context(world, 0), new[] { Action(WorldActionTypeIds.Observe) });
            Assert.That(decision.SelectedAction, Is.Not.Null);
        }

        [Test]
        public void UtilityPolicyProducesCandidateScoresTest()
        {
            var world = CreateWorld();
            var candidates = new[]
            {
                Action(WorldActionTypeIds.Observe, "action.observe.one"),
                Action(WorldActionTypeIds.Observe, "action.observe.two")
            };
            candidates[1].ExpectedBenefitBasisPoints = 1_000;
            var decision = new UtilityDecisionPolicy().Decide(
                world, Context(world, 0), candidates);
            Assert.That(decision.Scores.Count, Is.EqualTo(2));
            Assert.That(decision.SelectedAction.Id, Is.EqualTo(candidates[1].Id));
        }

        [Test]
        public void InvalidNeuralActionRejectedTest()
        {
            var world = CreateWorld();
            var invalid = InventoryAction(WorldActionTypeIds.CreateShipment, 10);
            var legal = Action(WorldActionTypeIds.Observe);
            var decision = new NeuralDecisionPolicyAdapter(
                new PreferShipmentScorer()).Decide(
                    world, Context(world, 0), new[] { invalid, legal });
            Assert.That(decision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.Observe));
        }

        [Test]
        public void RandomizedPolicyCannotRestoreInvalidActionTest()
        {
            var world = CreateWorld();
            var invalid = InventoryAction(WorldActionTypeIds.CreateShipment, 10);
            var legal = Action(WorldActionTypeIds.Observe, "action.observe.legal");
            invalid.BaseUtilityBasisPoints = 100_000;
            var decision = new RandomizedDecisionPolicy(
                new UtilityDecisionPolicy(), 500_000).Decide(
                    world, Context(world, 0), new[] { invalid, legal });
            Assert.That(decision.SelectedAction, Is.Not.Null);
            Assert.That(decision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.Observe));
        }

        [Test]
        public void HistoricalConstraintPolicyFiltersForbiddenCandidateTest()
        {
            var world = CreateWorld();
            var forbidden = Action(WorldActionTypeIds.Observe, "action.forbidden");
            forbidden.ExpectedBenefitBasisPoints = 10_000;
            var allowed = Action(WorldActionTypeIds.Observe, "action.allowed");
            var decision = new HistoricalConstraintDecisionPolicy(
                new UtilityDecisionPolicy(),
                (_, __, action) => action.Id != forbidden.Id).Decide(
                    world, Context(world, 0), new[] { forbidden, allowed });
            Assert.That(decision.PolicyId,
                Is.EqualTo(DecisionPolicyIds.HistoricalConstraint));
            Assert.That(decision.SelectedAction.Id, Is.EqualTo(allowed.Id));
        }

        [Test]
        public void DifferentPersonalityMayChooseDifferentActionTest()
        {
            var world = CreateWorld();
            var safe = Action(WorldActionTypeIds.Observe, "action.safe");
            safe.ExpectedBenefitBasisPoints = 100;
            var risky = Action(WorldActionTypeIds.Observe, "action.risky");
            risky.ExpectedBenefitBasisPoints = 2_000;
            risky.RiskBasisPoints = 3_000;
            world.People[0].Personality.RiskTolerance = 0;
            var cautious = new UtilityDecisionPolicy().Decide(
                world, Context(world, 0), new[] { safe, risky });
            world.People[0].Personality.RiskTolerance = 10_000;
            var bold = new UtilityDecisionPolicy().Decide(
                world, Context(world, 0), new[] { safe, risky });
            Assert.That(cautious.SelectedAction.Id, Is.EqualTo(safe.Id));
            Assert.That(bold.SelectedAction.Id, Is.EqualTo(risky.Id));
        }

        [Test]
        public void SameSeedSamePolicyReproducibilityTest()
        {
            var first = CreateWorld(111);
            var second = CreateWorld(111);
            var policy = new RandomizedDecisionPolicy(
                new UtilityDecisionPolicy(), 10_000);
            var candidates = EqualCandidates();
            var one = policy.Decide(first, Context(first, 0), candidates);
            var two = policy.Decide(second, Context(second, 0), candidates);
            Assert.That(one.SelectedAction.Id, Is.EqualTo(two.SelectedAction.Id));
        }

        [Test]
        public void DifferentSeedProducesDivergentDecisionTraceTest()
        {
            var seedService = new WorldSeedService();
            var first = CreateWorld(111);
            var second = CreateWorld(222);
            var diverged = false;
            for (var sequence = 0; sequence < 32; sequence++)
            {
                if (seedService.DecisionJitter(first, "person.actor", sequence, 1,
                        "action.seed", 10_000) !=
                    seedService.DecisionJitter(second, "person.actor", sequence, 1,
                        "action.seed", 10_000))
                {
                    diverged = true;
                    break;
                }
            }
            Assert.That(diverged, Is.True);
        }

        [Test]
        public void SaveLoadPreservesWorldSeedTest()
        {
            var world = CreateWorld(9123);
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.person.actor",
                AgentId = "person.actor",
                AgentKind = WorldAgentKind.Person,
                DecisionSequence = 7
            });
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.MasterSeed, Is.EqualTo(9123));
            Assert.That(loaded.WorldDecisionAgents[0].DecisionSequence,
                Is.EqualTo(7));
        }

        [Test]
        public void SnapshotMigratesVersionSeventyToLivingWorldContractTest()
        {
            var world = CreateWorld(184);
            world.SchemaVersion = 70;
            world.WorldDecisionAgents = null;
            world.WorldSimulationLodStates = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.WorldDecisionAgents, Is.Empty);
            Assert.That(migrated.WorldSimulationLodStates, Is.Empty);
        }

        [Test]
        public void HistoricalEventNotTriggeredByYearAloneTest()
        {
            var world = CreateEventWorld();
            var definition = new HistoricalEventDefinitionState
            {
                Id = "event.year_only",
                DisplayName = "Forbidden year-only event",
                EarliestDay = 0,
                LatestDay = 10,
                RequiresStructuredPreconditions = true
            };
            var rule = new HistoricalEventOutcomeRuleState
            {
                Id = "event.year_only.rule",
                Outcome = HistoricalEventOutcomeKind.Canonical
            };
            rule.Preconditions.Add(new HistoricalEventConditionState
            {
                Id = "event.year_only.time",
                ConditionTypeId = HistoricalConditionTypeIds.WorldDayAtLeast,
                NumericValue = 0
            });
            definition.OutcomeRules.Add(rule);
            world.HistoricalEventDefinitions.Add(definition);
            new HistoricalEventSystem().ResolveEligibleEvents(world);
            Assert.That(world.HistoricalAnchors[0].Status,
                Is.EqualTo(HistoricalAnchorStatus.Delayed));
        }

        [Test]
        public void CanonicalOutcomeTest()
        {
            var anchor = ResolvePrototype(CreateEventWorld(), CreatePrototype(0));
            Assert.That(anchor.OutcomeKind,
                Is.EqualTo(HistoricalEventOutcomeKind.Canonical));
        }

        [Test]
        public void PreventedOutcomeTest()
        {
            var world = CreateEventWorld();
            world.Facilities[0].LifecycleStatus = FacilityLifecycleStatus.Destroyed;
            var anchor = ResolvePrototype(world, CreatePrototype(0));
            Assert.That(anchor.OutcomeKind,
                Is.EqualTo(HistoricalEventOutcomeKind.Prevented));
        }

        [Test]
        public void DelayedOutcomeTest()
        {
            var world = CreateEventWorld();
            var definition = CreatePrototype(10);
            world.HistoricalEventDefinitions.Add(definition);
            new HistoricalEventSystem().ResolveEligibleEvents(world);
            Assert.That(world.HistoricalAnchors[0].Status,
                Is.EqualTo(HistoricalAnchorStatus.Watching));
        }

        [Test]
        public void VariantOrTransformedOutcomeTest()
        {
            var world = CreateEventWorld();
            world.People[0].LocationId = "location.changan";
            var anchor = ResolvePrototype(world, CreatePrototype(0));
            Assert.That(anchor.OutcomeKind,
                Is.EqualTo(HistoricalEventOutcomeKind.Transformed));
        }

        [Test]
        public void OffscreenEventApplyTest()
        {
            var world = CreateEventWorld();
            world.PlayerPersonId = string.Empty;
            var anchor = ResolvePrototype(world, CreatePrototype(0));
            Assert.That(anchor.AppliedOffscreen, Is.True);
        }

        [Test]
        public void CompletedEventDoesNotRepeatAfterLoadTest()
        {
            var world = CreateEventWorld();
            var anchor = ResolvePrototype(world, CreatePrototype(0));
            var applied = anchor.AppliedChangeOperationIds.Count;
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            new HistoricalEventSystem().ResolveEligibleEvents(loaded);
            Assert.That(loaded.HistoricalAnchors[0].AppliedChangeOperationIds.Count,
                Is.EqualTo(applied));
        }

        [Test]
        public void DestroyedFacilityChangesRealRuntimeStateTest()
        {
            var world = CreateEventWorld();
            _ = ResolvePrototype(world, CreatePrototype(0));
            Assert.That(world.Facilities[1].LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Destroyed));
        }

        [Test]
        public void AlreadyDestroyedTargetHandledSafelyTest()
        {
            var world = CreateEventWorld();
            world.Facilities[1].LifecycleStatus = FacilityLifecycleStatus.Destroyed;
            Assert.DoesNotThrow(() => ResolvePrototype(world, CreatePrototype(0)));
        }

        [Test]
        public void PersonMigrationAffectsLocationTest()
        {
            var world = CreateEventWorld();
            var anchor = new HistoricalAnchorRuntimeState
            {
                Id = "anchor.migration",
                DefinitionId = "event.migration"
            };
            HistoricalChangePackageExecutor.Apply(
                world,
                "1",
                new[]
                {
                    new HistoricalChangeOperationState
                    {
                        Id = "operation.move.actor",
                        OperationTypeId = HistoricalChangeOperationTypeIds.MovePerson,
                        TargetId = "person.actor",
                        StringValue = "location.changan"
                    }
                },
                anchor);
            Assert.That(world.People[0].LocationId,
                Is.EqualTo("location.changan"));
        }

        [Test]
        public void FamilyCenterLossDoesNotDeleteOrganizationTest()
        {
            var world = CreateEventWorld();
            world.FamilyCenters.Add(new FamilyCenterState
            {
                Id = "family_center.han",
                OrganizationId = "organization.han",
                FacilityId = "facility.palace",
                ManagerPersonId = "person.actor",
                Designation = FamilyCenterDesignation.Primary,
                Status = FamilyCenterOperationalStatus.Active
            });
            HistoricalChangePackageExecutor.Apply(
                world,
                "1",
                new[]
                {
                    new HistoricalChangeOperationState
                    {
                        Id = "operation.lose.center",
                        OperationTypeId = HistoricalChangeOperationTypeIds.LoseFamilyCenter,
                        TargetId = "family_center.han"
                    }
                },
                new HistoricalAnchorRuntimeState());
            Assert.That(world.FamilyCenters[0].Status,
                Is.EqualTo(FamilyCenterOperationalStatus.Lost));
            Assert.That(world.Organizations.Exists(item =>
                item.Id == "organization.han"), Is.True);
        }

        [Test]
        public void OfficeRelocationTest()
        {
            var world = CreateEventWorld();
            world.CivilMilitaryOfficeAssignments.Add(
                new CivilMilitaryOfficeAssignmentState
                {
                    Id = "office_assignment.actor",
                    OfficeDefinitionId = "office.test",
                    HolderPersonId = "person.actor",
                    WorkplaceFacilityId = "facility.palace"
                });
            HistoricalChangePackageExecutor.Apply(
                world,
                "1",
                new[]
                {
                    new HistoricalChangeOperationState
                    {
                        Id = "operation.relocate.office",
                        OperationTypeId = HistoricalChangeOperationTypeIds.RelocateOffice,
                        TargetId = "office_assignment.actor",
                        StringValue = "facility.gate"
                    }
                },
                new HistoricalAnchorRuntimeState());
            Assert.That(world.CivilMilitaryOfficeAssignments[0]
                .WorkplaceFacilityId, Is.EqualTo("facility.gate"));
        }

        [Test]
        public void LodTransitionPreservesPersonIdsTest()
        {
            var world = CreateWorld();
            var ids = world.People.ConvertAll(item => item.Id);
            var state = new WorldSimulationLodState
            {
                Id = "lod.person.actor",
                TargetKindId = "person",
                TargetId = "person.actor"
            };
            var scheduler = new WorldSimulationLodScheduler();
            scheduler.ChangeTier(state, WorldSimulationLodTier.Cold, 0);
            scheduler.MarkEvaluated(state, 0);
            scheduler.ChangeTier(state, WorldSimulationLodTier.Hot, 30);
            Assert.That(world.People.ConvertAll(item => item.Id), Is.EqualTo(ids));
        }

        [Test]
        public void ColdToHotDoesNotRegenerateFacilityTest()
        {
            var world = CreateEventWorld();
            var count = world.Facilities.Count;
            var state = new WorldSimulationLodState
            {
                Id = "lod.location.luoyang",
                TargetKindId = "location",
                TargetId = "location.luoyang",
                Tier = WorldSimulationLodTier.Cold
            };
            new WorldSimulationLodScheduler().ChangeTier(
                state, WorldSimulationLodTier.Hot, 1);
            Assert.That(world.Facilities.Count, Is.EqualTo(count));
        }

        [Test]
        public void ColdBatchUsesRealInventoryTest()
        {
            var world = CreateWorld();
            world.ProductBatches.Add(new ProductBatchState
            {
                Id = "batch.real.grain",
                ProductDefinitionId = "product.grain",
                OwnerFamilyId = "family.actor",
                Quantity = 73
            });
            var lod = new WorldSimulationLodState
            {
                Id = "lod.family.actor",
                TargetKindId = "family",
                TargetId = "family.actor"
            };
            var scheduler = new WorldSimulationLodScheduler();
            scheduler.ChangeTier(lod, WorldSimulationLodTier.Cold, 0);
            scheduler.MarkEvaluated(lod, 0);
            Assert.That(world.ProductBatches[0].Quantity, Is.EqualTo(73));
            Assert.That(scheduler.IsDue(lod, 29), Is.False);
            Assert.That(scheduler.IsDue(lod, 30), Is.True);
        }

        [Test]
        public void PresentationActorNotRequiredForSimulationTest()
        {
            var world = CreateWorld(412);
            world.PlayerPersonId = string.Empty;
            var agent = new WorldDecisionAgentState
            {
                Id = "decision.person.actor.offscreen",
                AgentId = "person.actor",
                AgentKind = WorldAgentKind.Person
            };
            world.WorldDecisionAgents.Add(agent);
            var run = new WorldSimulationArena().Run(
                world,
                new WorldSimulationArenaScenario
                {
                    Id = "arena.offscreen",
                    WorldSeed = 412,
                    DurationDays = 0,
                    PolicySetId = DecisionPolicyIds.Rule,
                    AgentStateIds = new List<string> { agent.Id }
                },
                new RuleDecisionPolicy(),
                (_, __) => new[] { Action(WorldActionTypeIds.Observe) });
            Assert.That(run.DecisionTrace.Count, Is.EqualTo(1));
        }

        [Test]
        public void ShipmentSaveLoadContinuationTest()
        {
            Snapshot_RoundTripPreservesMarketCargoAndTradeLedger();
        }

        [Test]
        public void CargoConservationTest()
        {
            CivilianFreight_CrossCountyDeliveryKeepsCargoAndProvisionsSeparate();
        }

        [Test]
        public void NoNegativeInventoryTest()
        {
            Trading_RejectsCargoBeyondCapacityWithoutChangingWorld();
        }

        [Test]
        public void SimulationArenaSmokeTest()
        {
            var world = CreateWorld(333);
            var agent = new WorldDecisionAgentState
            {
                Id = "decision.person.actor",
                AgentId = "person.actor",
                AgentKind = WorldAgentKind.Person
            };
            world.WorldDecisionAgents.Add(agent);
            var run = new WorldSimulationArena().Run(
                world,
                new WorldSimulationArenaScenario
                {
                    Id = "arena.smoke",
                    WorldSeed = 333,
                    DurationDays = 2,
                    PolicySetId = "policy.rule",
                    AgentStateIds = new List<string> { agent.Id }
                },
                new RuleDecisionPolicy(),
                (_, __) => new[] { Action(WorldActionTypeIds.Observe) });
            Assert.That(run.Metrics.Count, Is.EqualTo(3));
            Assert.That(run.DecisionTrace.Count, Is.EqualTo(3));
        }

        [Test]
        public void SimulationArenaScalesToCountyAgentBatchTest()
        {
            AssertArenaAgentBatch(100);
            AssertArenaAgentBatch(1000);
            AssertArenaAgentBatch(1182);
        }

        [Test]
        public void HistoricalEventWatcherBatchPerformanceTest()
        {
            var world = CreateEventWorld();
            for (var i = 0; i < 1000; i++)
            {
                var definition = new HistoricalEventDefinitionState
                {
                    Id = "event.watcher." + i.ToString("D4"),
                    DisplayName = "Watcher " + i,
                    EarliestDay = 0,
                    LatestDay = 30,
                    RequiresStructuredPreconditions = true
                };
                var rule = new HistoricalEventOutcomeRuleState
                {
                    Id = definition.Id + ".rule",
                    Outcome = HistoricalEventOutcomeKind.Canonical
                };
                rule.Preconditions.Add(new HistoricalEventConditionState
                {
                    Id = rule.Id + ".organization",
                    ConditionTypeId = HistoricalConditionTypeIds.OrganizationExists,
                    TargetId = "organization.missing." + i.ToString("D4")
                });
                definition.OutcomeRules.Add(rule);
                world.HistoricalEventDefinitions.Add(definition);
            }
            new HistoricalEventSystem().ResolveEligibleEvents(world);
            Assert.That(world.HistoricalAnchors.Count, Is.EqualTo(1000));
            Assert.That(world.HistoricalAnchors.TrueForAll(item =>
                item.Status == HistoricalAnchorStatus.Delayed), Is.True);
        }

        [Test]
        public void OrderShipmentBatchPerformanceTest()
        {
            var world = CreateWorld(184);
            var batchCount = world.ProductBatches.Count;
            var validator = new WorldActionValidator();
            for (var i = 0; i < 1000; i++)
            {
                var action = InventoryAction(
                    i % 2 == 0
                        ? WorldActionTypeIds.CreateTradeOrder
                        : WorldActionTypeIds.CreateShipment,
                    1);
                action.Id = "action.supply.batch." + i.ToString("D4");
                Assert.That(validator.Validate(world, action).CanExecute,
                    Is.False);
            }
            Assert.That(world.ProductBatches.Count, Is.EqualTo(batchCount));
        }

        private static void AssertArenaAgentBatch(int agentCount)
        {
            var world = WorldState.Create(184);
            var scenario = new WorldSimulationArenaScenario
            {
                Id = "arena.county." + agentCount,
                WorldSeed = 184,
                DurationDays = 0,
                PolicySetId = DecisionPolicyIds.Rule
            };
            for (var i = 0; i < agentCount; i++)
            {
                var locationId = "location.county." + i.ToString("D4");
                var agentId = "decision.county." + i.ToString("D4");
                world.Locations.Add(new LocationState
                {
                    Id = locationId,
                    DisplayName = locationId,
                    Population = 100,
                    PublicOrderBasisPoints = 8_000
                });
                world.WorldDecisionAgents.Add(new WorldDecisionAgentState
                {
                    Id = agentId,
                    AgentId = locationId,
                    AgentKind = WorldAgentKind.Settlement
                });
                scenario.AgentStateIds.Add(agentId);
            }

            var run = new WorldSimulationArena().Run(
                world,
                scenario,
                new RuleDecisionPolicy(),
                (_, agent) => new[]
                {
                    new WorldActionIntent
                    {
                        Id = "action.observe." + agent.Id,
                        ActionTypeId = WorldActionTypeIds.Observe,
                        AgentId = agent.AgentId,
                        AgentKind = WorldAgentKind.Settlement,
                        LocationId = agent.AgentId
                    }
                });

            Assert.That(run.DecisionTrace.Count, Is.EqualTo(agentCount));
            Assert.That(run.DecisionTrace.TrueForAll(item =>
                !string.IsNullOrEmpty(item.ActionId)), Is.True);
        }

        private static WorldState CreateWorld(ulong seed = 184)
        {
            var world = WorldState.Create(seed);
            world.Locations.Add(new LocationState
            {
                Id = "location.test",
                DisplayName = "Test",
                Population = 2,
                GrainPrice = 100,
                PublicOrderBasisPoints = 8_000,
                Features = LocationFeature.Farmland
            });
            world.People.Add(new PersonState
            {
                Id = "person.actor",
                DisplayName = "Actor",
                LocationId = "location.test",
                BirthLocationId = "location.test",
                FamilyId = "family.actor",
                BirthDay = 0
            });
            world.Families.Add(new FamilyState
            {
                Id = "family.actor",
                DisplayName = "Actor family",
                HeadPersonId = "person.actor",
                LocationId = "location.test",
                Grain = 1,
                MemberIds = new List<string> { "person.actor" }
            });
            return world;
        }

        private static WorldState CreateEventWorld()
        {
            var world = CreateWorld();
            world.Locations[0].Id = "location.luoyang";
            world.People[0].LocationId = "location.luoyang";
            world.People[0].BirthLocationId = "location.luoyang";
            world.Families[0].LocationId = "location.luoyang";
            world.Locations.Add(new LocationState
            {
                Id = "location.changan",
                DisplayName = "Changan",
                Population = 1
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = "organization.han",
                DisplayName = "Han government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = "location.luoyang",
                LeaderPersonId = "person.actor"
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

        private static HistoricalEventDefinitionState CreatePrototype(long earliest)
        {
            return Luoyang189190HistoricalEventPrototype.Create(
                new Luoyang189190PrototypeBindings
                {
                    EmperorPersonId = "person.actor",
                    LuoyangLocationId = "location.luoyang",
                    AlternateLocationId = "location.changan",
                    GovernmentOrganizationId = "organization.han",
                    PalaceFacilityId = "facility.palace",
                    DestroyedFacilityId = "facility.gate",
                    EarliestDay = earliest,
                    LatestDay = earliest + 30
                });
        }

        private static HistoricalAnchorRuntimeState ResolvePrototype(
            WorldState world,
            HistoricalEventDefinitionState definition)
        {
            world.HistoricalEventDefinitions.Add(definition);
            var resolved = new HistoricalEventSystem().ResolveEligibleEvents(world);
            Assert.That(resolved.Count, Is.EqualTo(1));
            return resolved[0];
        }

        private static WorldDecisionContext Context(WorldState world, long sequence) =>
            new LivingWorldSignalCalculator().BuildContext(
                world,
                "person.actor",
                WorldAgentKind.Person,
                world.People[0].LocationId,
                sequence);

        private static int Signal(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id).ValueBasisPoints;

        private static WorldActionIntent Action(
            string type,
            string id = "action.observe") =>
            new WorldActionIntent
            {
                Id = id,
                ActionTypeId = type,
                AgentId = "person.actor",
                AgentKind = WorldAgentKind.Person,
                LocationId = "location.test"
            };

        private static WorldActionIntent InventoryAction(string type, long quantity)
        {
            var action = Action(type, "action.inventory");
            action.Arguments.Add(Arg("source_container_id", "container.missing"));
            action.Arguments.Add(Arg("product_definition_id", "product.grain"));
            action.Arguments.Add(Arg("quantity", quantity.ToString()));
            return action;
        }

        private static WorldActionArgument Arg(string key, string value) =>
            new WorldActionArgument { Key = key, Value = value };

        private static WorldActionIntent[] EqualCandidates() =>
            new[]
            {
                Action(WorldActionTypeIds.Observe, "action.equal.one"),
                Action(WorldActionTypeIds.Observe, "action.equal.two")
            };

        private sealed class PreferShipmentScorer : INeuralActionScorer
        {
            public string ModelVersion => "test-model-v1";

            public int Score(WorldDecisionContext context, WorldActionIntent action) =>
                action.ActionTypeId == WorldActionTypeIds.CreateShipment
                    ? 10_000
                    : 1;
        }
    }
}
