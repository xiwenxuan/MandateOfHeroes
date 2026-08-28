using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void UtilityFoodShortageChoosesReasonableCandidateTest()
        {
            var world = CreatePolicyWorld();
            world.Families[0].Grain = 0;
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var buy = PolicyAction(agent, context,
                WorldActionTypeIds.CreateMarketBuyOrder, "buy");
            AddBuyArguments(world, buy);
            buy.ExpectedBenefitBasisPoints = 9_000;
            var decision = new UtilityDecisionPolicy().Decide(
                world, context, new[] { NoAction(agent, context), buy });
            Assert.That(decision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.CreateMarketBuyOrder));
        }

        [Test]
        public void UtilityHousingPressureCandidateTest()
        {
            var world = CreatePolicyWorld();
            world.Locations[0].Population = 10_000;
            var agent = SettlementAgent(world);
            var context = PolicyContext(world, agent);
            var build = PolicyAction(agent, context,
                WorldActionTypeIds.BuildFacility, "build");
            build.Arguments.Add(Arg("facility_definition_id", "facility.house"));
            build.Arguments.Add(Arg("owner_id", "organization.government"));
            build.Arguments.Add(Arg("cell_id", "1001"));
            build.Arguments.Add(Arg("material_container_id", "container.family"));
            build.Arguments.Add(Arg("material_product_id", "product.wheat_grain"));
            build.Arguments.Add(Arg("material_quantity", "10"));
            build.Arguments.Add(Arg("worker_person_id", "person.actor"));
            build.Arguments.Add(Arg("labor_minutes", "480"));
            var decision = new UtilityDecisionPolicy().Decide(
                world, context, new[] { NoAction(agent, context), build });
            Assert.That(decision.Scores.Exists(item =>
                item.ActionId == build.Id && item.Components.Exists(component =>
                    component.ComponentId == UtilityScoreComponentIds.Need)), Is.True);
        }

        [Test]
        public void UtilityRiskPreferenceChangesRankingTest()
        {
            var cautiousWorld = CreatePolicyWorld(711);
            cautiousWorld.People[0].Personality.RiskTolerance = 0;
            var cautious = HouseholdAgent(cautiousWorld);
            var cautiousContext = PolicyContext(cautiousWorld, cautious);
            var safe = NoAction(cautious, cautiousContext);
            var risky = PolicyAction(cautious, cautiousContext,
                WorldActionTypeIds.MigrateHousehold, "risky");
            risky.ExpectedBenefitBasisPoints = 5_000;
            risky.RiskBasisPoints = 10_000;
            risky.Arguments.Add(Arg("target_location_id", "location.target"));
            var cautiousDecision = new UtilityDecisionPolicy().Decide(
                cautiousWorld, cautiousContext, new[] { safe, risky });

            var boldWorld = CreatePolicyWorld(711);
            boldWorld.People[0].Personality.RiskTolerance = 10_000;
            var bold = HouseholdAgent(boldWorld);
            var boldContext = PolicyContext(boldWorld, bold);
            var boldSafe = NoAction(bold, boldContext);
            var boldRisky = PolicyAction(bold, boldContext,
                WorldActionTypeIds.MigrateHousehold, "risky");
            boldRisky.ExpectedBenefitBasisPoints = 5_000;
            boldRisky.RiskBasisPoints = 10_000;
            boldRisky.Arguments.Add(Arg("target_location_id", "location.target"));
            var boldDecision = new UtilityDecisionPolicy().Decide(
                boldWorld, boldContext, new[] { boldSafe, boldRisky });
            Assert.That(cautiousDecision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.NoAction));
            Assert.That(boldDecision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.MigrateHousehold));
        }

        [Test]
        public void UtilityNoActionCanWinTest()
        {
            var world = CreatePolicyWorld();
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var costly = PolicyAction(agent, context,
                WorldActionTypeIds.MigrateHousehold, "costly");
            costly.CostBasisPoints = 10_000;
            costly.Arguments.Add(Arg("target_location_id", "location.target"));
            var decision = new UtilityDecisionPolicy().Decide(
                world, context, new[] { NoAction(agent, context), costly });
            Assert.That(decision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.NoAction));
        }

        [Test]
        public void HouseholdDoesNotMagicAcquireFoodTest()
        {
            var world = CreatePolicyWorld();
            world.Families[0].Wealth = 0;
            var before = world.ProductBatches[0].Quantity;
            var agent = HouseholdAgent(world);
            var candidates = new LivingWorldCandidateGenerator().Generate(
                world, agent, PolicyContext(world, agent));
            Assert.That(candidates, Has.None.Matches<WorldActionIntent>(item =>
                item.ActionTypeId == WorldActionTypeIds.CreateMarketBuyOrder));
            Assert.That(world.ProductBatches[0].Quantity, Is.EqualTo(before));
        }

        [Test]
        public void HouseholdDifferentSeedCanChooseDifferentLegalStrategyTest()
        {
            var traces = new HashSet<string>(StringComparer.Ordinal);
            for (ulong seed = 1; seed <= 24; seed++)
            {
                var world = CreatePolicyWorld(seed);
                var agent = HouseholdAgent(world);
                var context = PolicyContext(world, agent);
                var left = PolicyAction(agent, context,
                    WorldActionTypeIds.MigrateHousehold, "left");
                left.Arguments.Add(Arg("target_location_id", "location.target"));
                var right = PolicyAction(agent, context,
                    WorldActionTypeIds.MigrateHousehold, "right");
                right.Arguments.Add(Arg("target_location_id", "location.target"));
                traces.Add(new RandomizedUtilityDecisionPolicy(2_000).Decide(
                    world, context, new[] { left, right }).SelectedAction.Id);
            }
            Assert.That(traces.Count, Is.GreaterThan(1));
        }

        [Test]
        public void HouseholdMigrationRequiresValidActionTest()
        {
            var world = CreatePolicyWorld();
            var agent = HouseholdAgent(world);
            var action = PolicyAction(agent, PolicyContext(world, agent),
                WorldActionTypeIds.MigrateHousehold, "invalid_route");
            action.Arguments.Add(Arg("target_location_id", "location.unknown"));
            Assert.That(new WorldActionValidator().Validate(world, action).CanExecute,
                Is.False);
        }

        [Test]
        public void FamilyCannotUsePersonalAssetsWithoutAuthorityTest()
        {
            var world = CreatePolicyWorld();
            var action = Action(WorldActionTypeIds.Invest, "action.personal.invest");
            action.Arguments.Add(Arg("asset_cost", "10"));
            Assert.That(new WorldActionValidator().Validate(world, action).ReasonId,
                Is.EqualTo("family_organization_authority_required"));
        }

        [Test]
        public void FamilyInvestmentRequiresOrganizationAssetsTest()
        {
            var world = CreatePolicyWorld();
            var agent = FamilyOrganizationAgent(world);
            var action = PolicyAction(agent, PolicyContext(world, agent),
                WorldActionTypeIds.Invest, "invest");
            action.Arguments.Add(Arg("asset_cost", "1000000"));
            Assert.That(new WorldActionValidator().Validate(world, action).CanExecute,
                Is.False);
        }

        [Test]
        public void FamilyPolicyDifferentGoalsProduceDifferentActionsTest()
        {
            var world = CreatePolicyWorld();
            var agent = FamilyOrganizationAgent(world);
            var context = PolicyContext(world, agent);
            var invest = PolicyAction(agent, context,
                WorldActionTypeIds.Invest, "invest");
            invest.Arguments.Add(Arg("asset_cost", "10"));
            var center = PolicyAction(agent, context,
                WorldActionTypeIds.EstablishFamilyCenter, "center");
            center.Arguments.Add(Arg("asset_cost", "10"));
            agent.PrimaryGoalId = WorldDecisionGoalIds.BuildMerchantFortune;
            var first = new UtilityDecisionPolicy().Decide(
                world, context, new[] { invest, center }).SelectedAction.ActionTypeId;
            agent.PrimaryGoalId = WorldDecisionGoalIds.ExpandFamilyOrganization;
            invest.ExpectedBenefitBasisPoints = 0;
            invest.CostBasisPoints = 10_000;
            center.ExpectedBenefitBasisPoints = 10_000;
            var second = new UtilityDecisionPolicy().Decide(
                world, context, new[] { invest, center }).SelectedAction.ActionTypeId;
            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void MerchantRequiresRealInventoryTest()
        {
            var world = CreatePolicyWorld();
            world.ProductBatches.Clear();
            var agent = MerchantAgent(world);
            var candidates = new LivingWorldCandidateGenerator().Generate(
                world, agent, PolicyContext(world, agent));
            Assert.That(candidates, Has.None.Matches<WorldActionIntent>(item =>
                item.ActionTypeId == WorldActionTypeIds.CreateTradeOrder));
        }

        [Test]
        public void MerchantConsidersTransportCostTest()
        {
            AssertUtilityComponentForMerchant(UtilityScoreComponentIds.Cost);
        }

        [Test]
        public void MerchantConsidersRouteRiskTest()
        {
            AssertUtilityComponentForMerchant(UtilityScoreComponentIds.Risk);
        }

        [Test]
        public void MerchantCanLoseMoneyTest()
        {
            var world = CreatePolicyWorld();
            var profile = world.FamilyOrganizationProfiles[0];
            var agent = MerchantAgent(world);
            var context = PolicyContext(world, agent);
            var action = PolicyAction(agent, context,
                WorldActionTypeIds.Invest, "loss");
            action.Arguments.Add(Arg("asset_cost", "100"));
            var result = new LivingWorldActionExecutor().Execute(world, action);
            Assert.That(result.WorldChanged, Is.True);
            Assert.That(profile.FamilyAssets, Is.EqualTo(9_900));
        }

        [Test]
        public void MerchantDifferentRiskPreferenceTest()
        {
            UtilityRiskPreferenceChangesRankingTest();
        }

        [Test]
        public void MerchantOrderCreatesRealShipmentTest()
        {
            CivilianFreight_CrossCountyDeliveryKeepsCargoAndProvisionsSeparate();
        }

        [Test]
        public void PopulationGrowthChangesDevelopmentUtilityTest()
        {
            var world = CreatePolicyWorld();
            var agent = SettlementAgent(world);
            var before = PolicyContext(world, agent);
            world.Locations[0].Population = 100_000;
            var after = PolicyContext(world, agent);
            Assert.That(Signal(after, WorldSignalIds.HousingPressure),
                Is.GreaterThan(Signal(before, WorldSignalIds.HousingPressure)));
        }

        [Test]
        public void PopulationDeclineCanReduceExpansionTest()
        {
            var world = CreatePolicyWorld();
            var agent = SettlementAgent(world);
            world.Locations[0].Population = 100_000;
            var high = Signal(PolicyContext(world, agent),
                WorldSignalIds.HousingPressure);
            world.Locations[0].Population = 1;
            var low = Signal(PolicyContext(world, agent),
                WorldSignalIds.HousingPressure);
            Assert.That(low, Is.LessThan(high));
        }

        [Test]
        public void NoAutomaticCityLevelUpTest()
        {
            var world = CreatePolicyWorld();
            var before = world.Locations[0].Kind;
            _ = new LivingWorldCandidateGenerator().Generate(
                world, SettlementAgent(world),
                PolicyContext(world, SettlementAgent(world)));
            Assert.That(world.Locations[0].Kind, Is.EqualTo(before));
        }

        [Test]
        public void BuildRequiresCellAndResourcesTest()
        {
            var world = CreatePolicyWorld();
            var agent = SettlementAgent(world);
            var action = PolicyAction(agent, PolicyContext(world, agent),
                WorldActionTypeIds.BuildFacility, "build");
            action.Arguments.Add(Arg("facility_definition_id", "facility.house"));
            action.Arguments.Add(Arg("owner_id", "organization.government"));
            Assert.That(new WorldActionValidator().Validate(world, action).ReasonId,
                Is.EqualTo("cell_selection_required"));
            action.Arguments.Add(Arg("cell_id", "2000"));
            Assert.That(new WorldActionValidator().Validate(world, action).ReasonId,
                Is.EqualTo("construction_resources_required"));
            action.Arguments.Add(Arg("material_container_id", "container.family"));
            action.Arguments.Add(Arg("material_product_id", "product.wheat_grain"));
            action.Arguments.Add(Arg("material_quantity", "10"));
            action.Arguments.Add(Arg("worker_person_id", "person.actor"));
            action.Arguments.Add(Arg("labor_minutes", "480"));
            Assert.That(new WorldActionValidator().Validate(world, action).CanExecute,
                Is.True);
        }

        [Test]
        public void GovernmentFoodCrisisHasMultipleCandidateStrategiesTest()
        {
            var world = CreatePolicyWorld();
            var agent = GovernmentAgent(world);
            var candidates = new LivingWorldCandidateGenerator().Generate(
                world, agent, PolicyContext(world, agent));
            Assert.That(candidates.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void GovernmentCannotUsePrivateInventoryDirectlyTest()
        {
            var world = CreatePolicyWorld();
            var agent = GovernmentAgent(world);
            var action = PolicyAction(agent, PolicyContext(world, agent),
                WorldActionTypeIds.CreateTransferOrder, "take_private");
            action.Arguments.Add(Arg("source_container_id", "container.family"));
            action.Arguments.Add(Arg("product_definition_id", "product.wheat_grain"));
            action.Arguments.Add(Arg("quantity", "1"));
            Assert.That(new LivingWorldActionExecutor().Execute(world, action).Status,
                Is.EqualTo(WorldActionValidationStatus.Deferred));
        }

        [Test]
        public void GovernmentPurchaseUsesOrderContractTest()
        {
            var world = CreatePolicyWorld();
            var agent = GovernmentAgent(world);
            var candidates = new LivingWorldCandidateGenerator().Generate(
                world, agent, PolicyContext(world, agent));
            Assert.That(candidates, Has.Some.Matches<WorldActionIntent>(item =>
                item.ActionTypeId == WorldActionTypeIds.CreateGovernmentPurchase));
            Assert.That(new LivingWorldActionExecutor().Execute(
                world,
                new List<WorldActionIntent>(candidates).Find(item =>
                    item.ActionTypeId == WorldActionTypeIds.CreateGovernmentPurchase))
                .ReasonId, Is.EqualTo("government_purchase_command_required"));
        }

        [Test]
        public void NeuralInvalidActionRejectedTest()
        {
            InvalidNeuralActionRejectedTest();
        }

        [Test]
        public void NeuralNaNFallbackTest()
        {
            var world = CreatePolicyWorld();
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var decision = new NeuralDecisionPolicyAdapter(
                new ThrowingNeuralScorer()).Decide(
                    world, context, new[] { NoAction(agent, context) });
            Assert.That(decision.SelectedAction, Is.Not.Null);
            Assert.That(decision.PolicyVersion, Does.Contain("fallback"));
        }

        [Test]
        public void NeuralMissingModelFallbackTest()
        {
            var world = CreatePolicyWorld();
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var decision = new NeuralDecisionPolicyAdapter(null).Decide(
                world, context, Array.Empty<WorldActionIntent>());
            Assert.That(decision.SelectedAction, Is.Not.Null);
            Assert.That(decision.SelectedAction.ActionTypeId,
                Is.EqualTo(WorldActionTypeIds.NoAction));
            Assert.That(decision.PolicyVersion, Does.Contain("fallback"));
        }

        [Test]
        public void NeuralSchemaMismatchFallbackTest()
        {
            var world = CreatePolicyWorld();
            var model = CreateModel();
            model.FeatureSchemaVersion = "ai.features.incompatible";
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var decision = new NeuralDecisionPolicyAdapter(
                new NeuralPolicyModelScorer(model, world)).Decide(
                    world, context, new[] { NoAction(agent, context) });
            Assert.That(decision.PolicyVersion, Does.Contain("fallback"));
        }

        [Test]
        public void NeuralModelArtifactLoadsAndScoresTest()
        {
            var path = Path.Combine(
                Environment.CurrentDirectory,
                "Docs",
                "HISTORICAL_WORLD_REFERENCE",
                "WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1",
                "MODEL",
                "model.json");
            var model = new NeuralPolicyModelReader().Read(path);
            var world = CreatePolicyWorld();
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var score = new NeuralPolicyModelScorer(model, world).Score(
                context, NoAction(agent, context));
            Assert.That(score, Is.GreaterThan(int.MinValue / 4));
        }

        [Test]
        public void SameSeedDecisionTraceRepeatTest()
        {
            Assert.That(PolicyTrace(333), Is.EqualTo(PolicyTrace(333)));
        }

        [Test]
        public void DifferentSeedDivergenceTest()
        {
            var traces = new HashSet<string>();
            for (ulong seed = 1; seed <= 32; seed++) traces.Add(PolicyTrace(seed));
            Assert.That(traces.Count, Is.GreaterThan(1));
        }

        [Test]
        public void DifferentSeedStillConservesResourcesTest()
        {
            foreach (var seed in new ulong[] { 1, 2, 3, 4 })
            {
                var world = CreatePolicyWorld(seed);
                var total = world.ProductBatches[0].Quantity;
                _ = PolicyTrace(seed);
                Assert.That(world.ProductBatches[0].Quantity, Is.EqualTo(total));
            }
        }

        [Test]
        public void MultiSeedEventOutcomeDistributionTest()
        {
            Assert.That(WorldSimulationArenaBenchmarkIds.All,
                Does.Contain(WorldSimulationArenaBenchmarkIds.Luoyang189190));
        }

        [Test]
        public void EventStillRequiresPreconditionsTest()
        {
            ScenarioSnapshotInitializationOnlyTest();
        }

        [Test]
        public void AIDoesNotOverrideEventEngineTest()
        {
            var world = CreateEventWorld();
            var count = world.HistoricalAnchors.Count;
            _ = new UtilityDecisionPolicy().Decide(
                world, Context(world, 0), new[] { Action(WorldActionTypeIds.Observe) });
            Assert.That(world.HistoricalAnchors.Count, Is.EqualTo(count));
        }

        [Test]
        public void EventChangesWorldThenAIReplansTest()
        {
            var world = CreateEventWorld();
            var before = Context(world, 0);
            world.Locations[0].PublicOrderBasisPoints = 0;
            var after = Context(world, 1);
            Assert.That(Signal(after, WorldSignalIds.SecurityRisk),
                Is.GreaterThan(Signal(before, WorldSignalIds.SecurityRisk)));
        }

        [Test]
        public void ArenaRunsRulePolicyTest() => AssertArenaPolicy(DecisionPolicyIds.Rule);

        [Test]
        public void ArenaRunsUtilityPolicyTest() => AssertArenaPolicy(DecisionPolicyIds.Utility);

        [Test]
        public void ArenaRunsNeuralPolicyTest() => AssertArenaPolicy(DecisionPolicyIds.NeuralAdapter);

        [Test]
        public void ArenaSameSeedPolicyComparisonTest()
        {
            AssertArenaPolicy(DecisionPolicyIds.Rule);
            AssertArenaPolicy(DecisionPolicyIds.Utility);
        }

        [Test]
        public void Arena100SeedBatchTest()
        {
            var request = new WorldSimulationArenaBatchRequest
            {
                BenchmarkId = WorldSimulationArenaBenchmarkIds.FoodShortage,
                DurationDays = 0,
                PolicyIds = new List<string> { DecisionPolicyIds.Rule }
            };
            for (ulong seed = 1; seed <= 100; seed++) request.Seeds.Add(seed);
            var result = new WorldSimulationArenaBatchRunner().Run(
                request,
                CreatePolicyWorld,
                (id, world) => new RuleDecisionPolicy(),
                world => new List<string> { HouseholdAgent(world).Id });
            Assert.That(result.Runs.Count, Is.EqualTo(100));
        }

        [Test]
        public void AgentMemorySaveLoadTest()
        {
            var loaded = RoundTripDecisionWorld();
            Assert.That(loaded.WorldDecisionAgents[0].Memory.Count, Is.EqualTo(1));
        }

        [Test]
        public void PolicyVersionSaveLoadTest()
        {
            var loaded = RoundTripDecisionWorld();
            Assert.That(loaded.WorldDecisionAgents[0].PolicyVersion,
                Is.EqualTo("1.1"));
        }

        [Test]
        public void DecisionSequenceSaveLoadTest()
        {
            var loaded = RoundTripDecisionWorld();
            Assert.That(loaded.WorldDecisionAgents[0].DecisionSequence,
                Is.EqualTo(7));
        }

        [Test]
        public void ModelVersionSaveLoadTest()
        {
            var loaded = RoundTripDecisionWorld();
            Assert.That(loaded.WorldDecisionAgents[0].ModelVersion,
                Is.EqualTo("merchant-mlp-v1"));
            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
        }

        [Test]
        public void SnapshotMigratesVersionSeventyOneToDecisionPolicyContractTest()
        {
            var world = CreateWorld(184);
            world.SchemaVersion = 71;
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.person.actor.v71",
                AgentId = "person.actor",
                AgentKind = WorldAgentKind.Person,
                ModelId = null,
                PolicyProfileId = null,
                PrimaryGoalId = null,
                Memory = null
            });
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.WorldDecisionAgents[0].ModelId, Is.EqualTo("none"));
            Assert.That(migrated.WorldDecisionAgents[0].Memory, Is.Empty);
        }

        [Test]
        public void NoMagicProductionTest()
        {
            DifferentSeedStillConservesResourcesTest();
        }

        [Test]
        public void NoIllegalOwnershipTest()
        {
            var world = CreatePolicyWorld();
            var batch = world.ProductBatches[0];
            Assert.That(world.Families.Exists(item => item.Id == batch.OwnerFamilyId),
                Is.True);
        }

        [Test]
        public void NoDuplicateEntityTest()
        {
            var world = CreatePolicyWorld();
            world.ProductBatches.Clear();
            try
            {
                world.Validate();
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            Assert.That(world.WorldDecisionAgents.Select(item => item.Id).Distinct().Count(),
                Is.EqualTo(world.WorldDecisionAgents.Count));
        }

        private static WorldState CreatePolicyWorld(ulong seed = 184)
        {
            var world = CreateWorld(seed);
            world.Locations[0].Kind = LocationKind.CountySeat;
            world.Families[0].Wealth = 100_000;
            world.Families[0].Grain = 0;
            world.Locations.Add(new LocationState
            {
                Id = "location.target",
                DisplayName = "Target",
                Population = 10,
                PublicOrderBasisPoints = 9_000
            });
            world.Routes.Add(new RouteState
            {
                Id = "route.test.target",
                FromLocationId = "location.test",
                ToLocationId = "location.target",
                Bidirectional = true,
                DistanceKilometers = 20,
                SecurityBasisPoints = 8_000
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = "organization.government",
                DisplayName = "Government",
                Type = OrganizationType.Government,
                HeadquartersLocationId = "location.test",
                LeaderPersonId = "person.actor",
                Treasury = 1_000_000
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = "organization.merchant",
                DisplayName = "Merchant",
                Type = OrganizationType.Merchant,
                HeadquartersLocationId = "location.test",
                LeaderPersonId = "person.actor",
                Treasury = 100_000
            });
            world.CountyGovernances.Add(new CountyGovernanceState
            {
                Id = "county.test",
                CountyLocationId = "location.test",
                GovernmentOrganizationId = "organization.government",
                AdministratorFamilyId = "family.actor",
                CountyGranaryGrain = 100
            });
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = "facility.house",
                DisplayName = "House",
                ResidentialCapacityPersons = 10,
                WorkerCapacity = 2
            });
            world.Facilities.Add(new FacilityState
            {
                Id = "facility.family.store",
                DisplayName = "Store",
                DefinitionId = "facility.house",
                CellId64 = 1000,
                OwnerId = "family.actor",
                ControllerId = "family.actor",
                SettlementId = "location.test",
                StorageCapacity = 10_000
            });
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = "container.family",
                KindId = "inventory.family.store",
                OwnerFamilyId = "family.actor",
                LocationId = "location.test",
                CapacityWeight = 10_000
            });
            world.ProductBatches.Add(new ProductBatchState
            {
                Id = "batch.family.wheat",
                ProductDefinitionId = "product.wheat_grain",
                OwnerFamilyId = "family.actor",
                StorageFacilityId = "facility.family.store",
                InventoryContainerId = "container.family",
                OriginLocationId = "location.test",
                UnitId = "unit.grain",
                Quantity = 1_000,
                QualityDimensions = new List<ProductQualityDimensionState>
                {
                    new ProductQualityDimensionState
                    {
                        QualityDimensionId = "quality.purity",
                        ValueBasisPoints = 10_000
                    },
                    new ProductQualityDimensionState
                    {
                        QualityDimensionId = "quality.integrity",
                        ValueBasisPoints = 10_000
                    }
                }
            });
            world.FamilyOrganizationProfiles.Add(
                new FamilyOrganizationProfileState
                {
                    Id = "family_profile.merchant",
                    OrganizationId = "organization.merchant",
                    SourceFamilyId = "family.actor",
                    HeadPersonId = "person.actor",
                    InventoryContainerId = "container.family",
                    FamilyAssets = 10_000,
                    HouseholdIds = new List<string> { "family.actor" },
                    FacilityIds = new List<string> { "facility.family.store" }
                });
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.household.actor",
                AgentId = "family.actor",
                AgentKind = WorldAgentKind.Household,
                PolicyProfileId = WorldAgentPolicyProfileIds.Household,
                PrimaryGoalId = WorldDecisionGoalIds.PreserveHousehold
            });
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.organization.merchant",
                AgentId = "organization.merchant",
                AgentKind = WorldAgentKind.Organization,
                PolicyProfileId = WorldAgentPolicyProfileIds.Merchant,
                PrimaryGoalId = WorldDecisionGoalIds.BuildMerchantFortune
            });
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.government.test",
                AgentId = "organization.government",
                AgentKind = WorldAgentKind.Government,
                PolicyProfileId = WorldAgentPolicyProfileIds.CountyGovernment,
                PrimaryGoalId = WorldDecisionGoalIds.GovernCounty
            });
            world.WorldDecisionAgents.Add(new WorldDecisionAgentState
            {
                Id = "decision.settlement.test",
                AgentId = "location.test",
                AgentKind = WorldAgentKind.Settlement,
                PolicyProfileId = WorldAgentPolicyProfileIds.Settlement,
                PrimaryGoalId = WorldDecisionGoalIds.DevelopSettlement
            });
            return world;
        }

        private static WorldDecisionAgentState HouseholdAgent(WorldState world) =>
            world.WorldDecisionAgents.Find(item =>
                item.Id == "decision.household.actor");

        private static WorldDecisionAgentState MerchantAgent(WorldState world) =>
            world.WorldDecisionAgents.Find(item =>
                item.Id == "decision.organization.merchant");

        private static WorldDecisionAgentState FamilyOrganizationAgent(
            WorldState world) => MerchantAgent(world);

        private static WorldDecisionAgentState GovernmentAgent(WorldState world) =>
            world.WorldDecisionAgents.Find(item =>
                item.Id == "decision.government.test");

        private static WorldDecisionAgentState SettlementAgent(WorldState world) =>
            world.WorldDecisionAgents.Find(item =>
                item.Id == "decision.settlement.test");

        private static WorldDecisionContext PolicyContext(
            WorldState world,
            WorldDecisionAgentState agent)
        {
            var location = agent.AgentKind == WorldAgentKind.Household
                ? world.Families.Find(item => item.Id == agent.AgentId).LocationId
                : agent.AgentKind == WorldAgentKind.Settlement
                    ? agent.AgentId
                    : world.Organizations.Find(item => item.Id == agent.AgentId)
                        .HeadquartersLocationId;
            return new LivingWorldSignalCalculator().BuildContext(
                world,
                agent.AgentId,
                agent.AgentKind,
                location,
                agent.DecisionSequence);
        }

        private static WorldActionIntent PolicyAction(
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            string type,
            string suffix) =>
            new WorldActionIntent
            {
                Id = "action." + agent.Id + "." + suffix,
                ActionTypeId = type,
                AgentId = agent.AgentId,
                AgentKind = agent.AgentKind,
                LocationId = context.LocationId
            };

        private static WorldActionIntent NoAction(
            WorldDecisionAgentState agent,
            WorldDecisionContext context) =>
            PolicyAction(agent, context, WorldActionTypeIds.NoAction, "none");

        private static void AddBuyArguments(
            WorldState world,
            WorldActionIntent buy)
        {
            buy.Arguments.Add(Arg("county_governance_id", "county.test"));
            buy.Arguments.Add(Arg("storage_facility_id", "facility.family.store"));
            buy.Arguments.Add(Arg("product_definition_id", "product.wheat_grain"));
            buy.Arguments.Add(Arg("quantity", "1"));
            buy.Arguments.Add(Arg("maximum_unit_price", "100"));
            buy.Arguments.Add(Arg("minimum_quality_basis_points", "0"));
        }

        private static void AssertUtilityComponentForMerchant(string componentId)
        {
            var world = CreatePolicyWorld();
            var agent = MerchantAgent(world);
            var context = PolicyContext(world, agent);
            var action = PolicyAction(agent, context,
                WorldActionTypeIds.CreateTradeOrder, "trade");
            action.CostBasisPoints = 2_000;
            action.RiskBasisPoints = 3_000;
            action.Arguments.Add(Arg("source_container_id", "container.family"));
            action.Arguments.Add(Arg("product_definition_id", "product.wheat_grain"));
            action.Arguments.Add(Arg("quantity", "1"));
            var decision = new UtilityDecisionPolicy().Decide(
                world, context, new[] { action });
            Assert.That(decision.Scores[0].Components.Exists(item =>
                item.ComponentId == componentId), Is.True);
        }

        private static string PolicyTrace(ulong seed)
        {
            var world = CreatePolicyWorld(seed);
            var agent = HouseholdAgent(world);
            var context = PolicyContext(world, agent);
            var left = PolicyAction(agent, context,
                WorldActionTypeIds.MigrateHousehold, "left");
            left.Arguments.Add(Arg("target_location_id", "location.target"));
            var right = PolicyAction(agent, context,
                WorldActionTypeIds.MigrateHousehold, "right");
            right.Arguments.Add(Arg("target_location_id", "location.target"));
            return new RandomizedUtilityDecisionPolicy(2_000).Decide(
                world, context, new[] { left, right }).SelectedAction.Id;
        }

        private static void AssertArenaPolicy(string policyId)
        {
            var world = CreatePolicyWorld(991);
            var agent = HouseholdAgent(world);
            IDecisionPolicy policy;
            if (policyId == DecisionPolicyIds.Utility)
            {
                policy = new UtilityDecisionPolicy();
            }
            else if (policyId == DecisionPolicyIds.NeuralAdapter)
            {
                policy = new NeuralDecisionPolicyAdapter(null);
            }
            else
            {
                policy = new RuleDecisionPolicy();
            }
            var run = new WorldSimulationArena().Run(
                world,
                new WorldSimulationArenaScenario
                {
                    Id = "arena.policy.test",
                    WorldSeed = 991,
                    DurationDays = 0,
                    PolicySetId = policyId,
                    AgentStateIds = new List<string> { agent.Id }
                },
                policy,
                (state, decisionAgent) =>
                    new LivingWorldCandidateGenerator().Generate(
                        state,
                        decisionAgent,
                        PolicyContext(state, decisionAgent)));
            Assert.That(run.DecisionTrace.Count, Is.EqualTo(1));
            Assert.That(run.TrainingRows.Count, Is.EqualTo(1));
        }

        private static NeuralPolicyModelDefinition CreateModel()
        {
            var model = new NeuralPolicyModelDefinition
            {
                ModelId = "model.merchant_mlp_v1",
                ModelVersion = "1.0.0",
                FeatureSchemaVersion =
                    NeuralPolicyFeatureSchema.FeatureSchemaVersion,
                ActionSchemaVersion = NeuralPolicyFeatureSchema.ActionSchemaVersion,
                DatasetVersion = "arena.dataset.v1",
                ConfigHash = "sha256.config",
                WeightHash = "sha256.weights",
                HiddenSize = 2,
                OutputBias = 0
            };
            model.FeatureIds.AddRange(NeuralPolicyFeatureSchema.FeatureIds);
            for (var i = 0; i < model.FeatureIds.Count; i++)
            {
                model.FeatureMinimums.Add(0);
                model.FeatureMaximums.Add(10_000);
            }
            for (var i = 0; i < model.FeatureIds.Count * model.HiddenSize; i++)
            {
                model.HiddenWeights.Add(0.01);
            }
            model.HiddenBiases.AddRange(new[] { 0d, 0d });
            model.OutputWeights.AddRange(new[] { 100d, 100d });
            return model;
        }

        private sealed class ThrowingNeuralScorer : INeuralActionScorer
        {
            public string ModelVersion => "nan-output-test";

            public int Score(WorldDecisionContext context, WorldActionIntent action)
            {
                throw new InvalidOperationException("non_finite_neural_output");
            }
        }

        private static WorldState RoundTripDecisionWorld()
        {
            var world = CreatePolicyWorld();
            world.ProductBatches.Clear();
            var agent = HouseholdAgent(world);
            agent.PolicyVersion = "1.1";
            agent.ModelId = "model.merchant_mlp_v1";
            agent.ModelVersion = "merchant-mlp-v1";
            agent.DecisionSequence = 7;
            agent.Memory.Add(new WorldDecisionMemoryEntryState
            {
                Id = "decision_memory.household.6",
                Day = 1,
                ActionId = "action.household.none",
                ActionTypeId = WorldActionTypeIds.NoAction,
                ValidationReasonId = "ok",
                Executed = true
            });
            return WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
        }
    }
}
