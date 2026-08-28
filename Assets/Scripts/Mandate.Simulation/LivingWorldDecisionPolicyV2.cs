using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class UtilityScoreComponentIds
    {
        public const string Need = "need";
        public const string Benefit = "benefit";
        public const string Cost = "cost";
        public const string Risk = "risk";
        public const string Time = "time";
        public const string Goal = "goal";
        public const string Personality = "personality";
        public const string RecentTrend = "recent_trend";
        public const string Feasibility = "feasibility";
        public const string Opportunity = "opportunity";
        public const string StableNoise = "stable_noise";
    }

    public sealed class UtilityWeightProfile
    {
        public string Id;
        public int Need = 10_000;
        public int Benefit = 8_000;
        public int Cost = 7_000;
        public int Risk = 7_000;
        public int Time = 3_000;
        public int Goal = 6_000;
        public int Personality = 4_000;
        public int RecentTrend = 3_000;
        public int Feasibility = 10_000;
        public int Opportunity = 5_000;
        public int NearTieNoise = 75;
    }

    public sealed class ResolvedAgentDecisionProfile
    {
        public string PolicyProfileId;
        public string GoalId;
        public int GoalWeightBasisPoints;
        public PersonalityState Personality;
        public WorldDecisionAgentState PersistedState;
    }

    public sealed class WorldAgentDecisionProfileResolver
    {
        public ResolvedAgentDecisionProfile Resolve(
            WorldState world,
            WorldDecisionContext context)
        {
            var state = world.WorldDecisionAgents.Find(item =>
                item.AgentId == context.AgentId &&
                item.AgentKind == context.AgentKind);
            var person = RepresentativePerson(world, context);
            var profileId = state?.PolicyProfileId;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                profileId = DefaultProfileId(world, context);
            }
            var goalId = state?.PrimaryGoalId;
            if (string.IsNullOrWhiteSpace(goalId))
            {
                goalId = DefaultGoalId(world, context, person);
            }
            return new ResolvedAgentDecisionProfile
            {
                PolicyProfileId = profileId,
                GoalId = goalId,
                GoalWeightBasisPoints = state == null
                    ? 5_000
                    : state.PrimaryGoalWeightBasisPoints,
                Personality = person?.Personality ?? new PersonalityState(),
                PersistedState = state
            };
        }

        private static PersonState RepresentativePerson(
            WorldState world,
            WorldDecisionContext context)
        {
            if (context.AgentKind == WorldAgentKind.Person)
            {
                return world.People.Find(item => item.Id == context.AgentId);
            }
            if (context.AgentKind == WorldAgentKind.Household)
            {
                var family = world.Families.Find(item => item.Id == context.AgentId);
                return family == null
                    ? null
                    : world.People.Find(item => item.Id == family.HeadPersonId);
            }
            if (context.AgentKind == WorldAgentKind.Organization ||
                context.AgentKind == WorldAgentKind.Government)
            {
                var organization = world.Organizations.Find(item =>
                    item.Id == context.AgentId);
                return organization == null
                    ? null
                    : world.People.Find(item =>
                        item.Id == organization.LeaderPersonId);
            }
            return null;
        }

        private static string DefaultProfileId(
            WorldState world,
            WorldDecisionContext context)
        {
            if (context.AgentKind == WorldAgentKind.Household)
            {
                return WorldAgentPolicyProfileIds.Household;
            }
            if (context.AgentKind == WorldAgentKind.Settlement)
            {
                return WorldAgentPolicyProfileIds.Settlement;
            }
            var organization = world.Organizations.Find(item =>
                item.Id == context.AgentId);
            if (organization?.Type == OrganizationType.Merchant)
            {
                return WorldAgentPolicyProfileIds.Merchant;
            }
            if (organization?.Type == OrganizationType.Government)
            {
                return WorldAgentPolicyProfileIds.CountyGovernment;
            }
            return WorldAgentPolicyProfileIds.FamilyOrganization;
        }

        private static string DefaultGoalId(
            WorldState world,
            WorldDecisionContext context,
            PersonState person)
        {
            if (person != null && person.LifeGoal == LifeGoalKind.BuildFortune)
            {
                return WorldDecisionGoalIds.BuildMerchantFortune;
            }
            if (context.AgentKind == WorldAgentKind.Household)
            {
                return WorldDecisionGoalIds.PreserveHousehold;
            }
            if (context.AgentKind == WorldAgentKind.Settlement)
            {
                return WorldDecisionGoalIds.DevelopSettlement;
            }
            var organization = world.Organizations.Find(item =>
                item.Id == context.AgentId);
            if (organization?.Type == OrganizationType.Merchant)
            {
                return WorldDecisionGoalIds.BuildMerchantFortune;
            }
            if (organization?.Type == OrganizationType.Government)
            {
                return WorldDecisionGoalIds.GovernCounty;
            }
            return WorldDecisionGoalIds.ExpandFamilyOrganization;
        }
    }

    public sealed class UtilityWeightProfileRegistry
    {
        public UtilityWeightProfile Resolve(string profileId)
        {
            var profile = new UtilityWeightProfile { Id = profileId };
            switch (profileId)
            {
                case WorldAgentPolicyProfileIds.Household:
                    profile.Need = 12_000;
                    profile.Goal = 7_000;
                    profile.Risk = 8_500;
                    profile.Cost = 8_000;
                    break;
                case WorldAgentPolicyProfileIds.FamilyOrganization:
                    profile.Goal = 9_000;
                    profile.RecentTrend = 5_000;
                    profile.Time = 4_000;
                    break;
                case WorldAgentPolicyProfileIds.Merchant:
                    profile.Benefit = 11_000;
                    profile.Opportunity = 9_000;
                    profile.Risk = 8_000;
                    profile.Cost = 9_000;
                    profile.NearTieNoise = 120;
                    break;
                case WorldAgentPolicyProfileIds.Settlement:
                    profile.Need = 11_000;
                    profile.Feasibility = 12_000;
                    profile.Time = 5_000;
                    break;
                case WorldAgentPolicyProfileIds.CountyGovernment:
                    profile.Need = 12_000;
                    profile.Risk = 9_000;
                    profile.Feasibility = 12_000;
                    profile.Goal = 8_000;
                    break;
            }
            return profile;
        }
    }

    public sealed class UtilityDecisionEngine
    {
        private readonly WorldActionValidator _validator;
        private readonly WorldAgentDecisionProfileResolver _profiles;
        private readonly UtilityWeightProfileRegistry _weights;
        private readonly WorldSeedService _seed;

        public UtilityDecisionEngine(
            WorldActionValidator validator,
            WorldAgentDecisionProfileResolver profiles = null,
            UtilityWeightProfileRegistry weights = null,
            WorldSeedService seed = null)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _profiles = profiles ?? new WorldAgentDecisionProfileResolver();
            _weights = weights ?? new UtilityWeightProfileRegistry();
            _seed = seed ?? new WorldSeedService();
        }

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates,
            IDecisionPolicy policy)
        {
            var result = RuleDecisionPolicy.NewResult(policy);
            var ordered = RuleDecisionPolicy.CopyAndSort(candidates);
            var agent = _profiles.Resolve(world, context);
            var weights = _weights.Resolve(agent.PolicyProfileId);
            var best = int.MinValue;
            for (var i = 0; i < ordered.Count; i++)
            {
                var action = ordered[i];
                var validation = _validator.Validate(world, action);
                var score = new WorldDecisionScore
                {
                    ActionId = action.Id,
                    Explanation = validation.ReasonId
                };
                if (!validation.CanExecute)
                {
                    score.ScoreBasisPoints = int.MinValue / 2;
                    Add(score, UtilityScoreComponentIds.Feasibility,
                        -10_000, weights.Feasibility, validation.ReasonId);
                    result.Scores.Add(score);
                    continue;
                }

                Add(score, UtilityScoreComponentIds.Need,
                    Need(context, action.ActionTypeId), weights.Need,
                    "authoritative world signals");
                Add(score, UtilityScoreComponentIds.Benefit,
                    action.ExpectedBenefitBasisPoints, weights.Benefit,
                    "candidate expected benefit");
                Add(score, UtilityScoreComponentIds.Cost,
                    -Math.Max(0, action.CostBasisPoints), weights.Cost,
                    "money/material/time cost");
                var riskAversion = 10_000 - agent.Personality.RiskTolerance;
                Add(score, UtilityScoreComponentIds.Risk,
                    -Math.Max(0, action.RiskBasisPoints) * riskAversion / 10_000,
                    weights.Risk, "action risk adjusted by existing personality");
                Add(score, UtilityScoreComponentIds.Time,
                    -Math.Min(10_000, ParseInt(action, "time_days") * 100),
                    weights.Time, "explicit action duration");
                Add(score, UtilityScoreComponentIds.Goal,
                    GoalAffinity(agent.GoalId, action.ActionTypeId) *
                    agent.GoalWeightBasisPoints / 10_000,
                    weights.Goal, agent.GoalId);
                Add(score, UtilityScoreComponentIds.Personality,
                    PersonalityAffinity(agent.Personality, action.ActionTypeId),
                    weights.Personality, "existing five-axis personality");
                Add(score, UtilityScoreComponentIds.RecentTrend,
                    RecentTrend(agent.PersistedState, action.ActionTypeId),
                    weights.RecentTrend, "bounded persisted decision memory");
                Add(score, UtilityScoreComponentIds.Feasibility,
                    validation.Status == WorldActionValidationStatus.Valid
                        ? 10_000
                        : 5_000,
                    weights.Feasibility, validation.ReasonId);
                Add(score, UtilityScoreComponentIds.Opportunity,
                    Opportunity(context, action.ActionTypeId),
                    weights.Opportunity, "world opportunity signals");
                if (action.BaseUtilityBasisPoints != 0)
                {
                    score.ScoreBasisPoints = checked(
                        score.ScoreBasisPoints + action.BaseUtilityBasisPoints);
                }
                var noise = weights.NearTieNoise == 0
                    ? 0
                    : _seed.DecisionJitter(
                        world,
                        context.AgentId,
                        context.DecisionSequence,
                        context.AbsoluteDay,
                        action.Id,
                        weights.NearTieNoise);
                Add(score, UtilityScoreComponentIds.StableNoise,
                    noise, 10_000,
                    "WorldSeed+Agent+Day+Sequence+Action");
                result.Scores.Add(score);
                if (score.ScoreBasisPoints > best)
                {
                    best = score.ScoreBasisPoints;
                    result.SelectedAction = action;
                }
            }
            return result;
        }

        private static void Add(
            WorldDecisionScore score,
            string id,
            int raw,
            int weight,
            string evidence)
        {
            var contribution = checked(raw * weight / 10_000);
            score.ScoreBasisPoints = checked(
                score.ScoreBasisPoints + contribution);
            score.Components.Add(new WorldDecisionScoreComponent
            {
                ComponentId = id,
                RawBasisPoints = raw,
                WeightBasisPoints = weight,
                ContributionBasisPoints = contribution,
                Evidence = evidence
            });
        }

        private static int Need(
            WorldDecisionContext context,
            string actionTypeId)
        {
            switch (actionTypeId)
            {
                case WorldActionTypeIds.CreateMarketBuyOrder:
                case WorldActionTypeIds.CreateGovernmentPurchase:
                    return Signal(context, WorldSignalIds.FoodPressure);
                case WorldActionTypeIds.BuildFacility:
                    return Math.Max(
                        Signal(context, WorldSignalIds.HousingPressure),
                        Signal(context, WorldSignalIds.EmploymentPressure));
                case WorldActionTypeIds.MigrateHousehold:
                case WorldActionTypeIds.MovePerson:
                    return Signal(context, WorldSignalIds.MigrationPressure);
                case WorldActionTypeIds.NoAction:
                    return 1_000;
                default:
                    return Signal(context, WorldSignalIds.InventoryPressure);
            }
        }

        private static int Opportunity(
            WorldDecisionContext context,
            string actionTypeId)
        {
            switch (actionTypeId)
            {
                case WorldActionTypeIds.CreateTradeOrder:
                case WorldActionTypeIds.Invest:
                    return Signal(context, WorldSignalIds.ProfitOpportunity);
                case WorldActionTypeIds.BuildFacility:
                    return Math.Max(
                        Signal(context, WorldSignalIds.LandAvailability),
                        Signal(context, WorldSignalIds.ResourceOpportunity));
                default:
                    return 0;
            }
        }

        internal static int GoalAffinity(string goalId, string actionTypeId)
        {
            if (goalId == WorldDecisionGoalIds.PreserveHousehold)
            {
                return actionTypeId == WorldActionTypeIds.CreateMarketBuyOrder ||
                    actionTypeId == WorldActionTypeIds.MigrateHousehold
                    ? 9_000 : 0;
            }
            if (goalId == WorldDecisionGoalIds.BuildMerchantFortune)
            {
                return actionTypeId == WorldActionTypeIds.CreateTradeOrder ||
                    actionTypeId == WorldActionTypeIds.Invest
                    ? 9_000 : 0;
            }
            if (goalId == WorldDecisionGoalIds.ExpandFamilyOrganization)
            {
                return actionTypeId == WorldActionTypeIds.Invest ||
                    actionTypeId == WorldActionTypeIds.EstablishFamilyCenter
                    ? 9_000 : 0;
            }
            if (goalId == WorldDecisionGoalIds.DevelopSettlement)
            {
                return actionTypeId == WorldActionTypeIds.BuildFacility ||
                    actionTypeId == WorldActionTypeIds.RepairFacility
                    ? 9_000 : 0;
            }
            if (goalId == WorldDecisionGoalIds.GovernCounty)
            {
                return actionTypeId == WorldActionTypeIds.CreateGovernmentPurchase ||
                    actionTypeId == WorldActionTypeIds.GovernmentPolicy
                    ? 9_000 : 0;
            }
            return 0;
        }

        private static int PersonalityAffinity(
            PersonalityState personality,
            string actionTypeId)
        {
            if (actionTypeId == WorldActionTypeIds.MigrateHousehold ||
                actionTypeId == WorldActionTypeIds.CreateTradeOrder ||
                actionTypeId == WorldActionTypeIds.Invest)
            {
                return personality.RiskTolerance - 5_000;
            }
            if (actionTypeId == WorldActionTypeIds.CreateGovernmentPurchase)
            {
                return personality.Benevolence - 5_000;
            }
            if (actionTypeId == WorldActionTypeIds.EstablishFamilyCenter)
            {
                return personality.FamilyDuty - 5_000;
            }
            if (actionTypeId == WorldActionTypeIds.BuildFacility)
            {
                return personality.Ambition - 5_000;
            }
            return 0;
        }

        private static int RecentTrend(
            WorldDecisionAgentState state,
            string actionTypeId)
        {
            if (state?.Memory == null)
            {
                return 0;
            }
            var total = 0;
            var count = 0;
            for (var i = state.Memory.Count - 1; i >= 0 && count < 8; i--)
            {
                var memory = state.Memory[i];
                if (memory.ActionTypeId != actionTypeId)
                {
                    continue;
                }
                total += memory.ShortOutcomeBasisPoints +
                    memory.LongOutcomeBasisPoints;
                count += 2;
            }
            return count == 0
                ? 0
                : Math.Max(-10_000, Math.Min(10_000, total / count));
        }

        private static int Signal(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id)
                ?.ValueBasisPoints ?? 0;

        private static int ParseInt(WorldActionIntent action, string key)
        {
            var value = action.Arguments.Find(item => item.Key == key)?.Value;
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed
                : 0;
        }
    }

    public sealed class RandomizedUtilityDecisionPolicy : IDecisionPolicy
    {
        private readonly RandomizedDecisionPolicy _inner;

        public RandomizedUtilityDecisionPolicy(
            int jitterMagnitude = 350,
            WorldActionValidator validator = null)
        {
            _inner = new RandomizedDecisionPolicy(
                new UtilityDecisionPolicy(validator),
                jitterMagnitude,
                validator: validator);
        }

        public string PolicyId => DecisionPolicyIds.RandomizedUtilityV1;
        public string PolicyVersion => "1";
        public string ModelVersion => "none";

        public WorldDecisionResult Decide(
            WorldState world,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            var result = _inner.Decide(world, context, candidates);
            result.PolicyId = PolicyId;
            result.PolicyVersion = PolicyVersion;
            return result;
        }
    }

    public sealed class LivingWorldCandidateGenerator
    {
        public IReadOnlyList<WorldActionIntent> Generate(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context)
        {
            if (world == null || agent == null || context == null)
            {
                throw new ArgumentNullException("Candidate generation input is null.");
            }
            var candidates = new List<WorldActionIntent>
            {
                NewAction(agent, context, WorldActionTypeIds.NoAction, "no_action")
            };
            switch (agent.AgentKind)
            {
                case WorldAgentKind.Household:
                    AddHousehold(world, agent, context, candidates);
                    break;
                case WorldAgentKind.Organization:
                    AddOrganization(world, agent, context, candidates);
                    break;
                case WorldAgentKind.Government:
                    AddGovernment(world, agent, context, candidates);
                    break;
                case WorldAgentKind.Settlement:
                    AddSettlement(world, agent, context, candidates);
                    break;
            }
            return candidates;
        }

        private static void AddHousehold(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            ICollection<WorldActionIntent> candidates)
        {
            var family = world.Families.Find(item => item.Id == agent.AgentId);
            if (family == null)
            {
                return;
            }
            var governance = world.CountyGovernances.Find(item =>
                item.CountyLocationId == family.LocationId);
            var storage = world.Facilities.Find(item =>
                item.SettlementId == family.LocationId &&
                (item.OwnerId == family.Id || item.ControllerId == family.Id));
            if (governance != null && storage != null && family.Wealth > 0)
            {
                var quantity = Math.Max(1L, Math.Min(30L, family.Wealth / 100L));
                var buy = NewAction(
                    agent, context, WorldActionTypeIds.CreateMarketBuyOrder,
                    "buy_food");
                buy.ExpectedBenefitBasisPoints =
                    Signal(context, WorldSignalIds.FoodPressure);
                buy.CostBasisPoints = 2_000;
                AddArg(buy, "county_governance_id", governance.Id);
                AddArg(buy, "storage_facility_id", storage.Id);
                AddArg(buy, "product_definition_id", "product.wheat_grain");
                AddArg(buy, "quantity", quantity.ToString());
                AddArg(buy, "maximum_unit_price", "100");
                AddArg(buy, "minimum_quality_basis_points", "0");
                AddArg(buy, "time_days", "1");
                candidates.Add(buy);
            }
            if (Signal(context, WorldSignalIds.MigrationPressure) >= 5_000)
            {
                var routes = AdjacentRoutes(world, family.LocationId);
                for (var i = 0; i < routes.Count && i < 3; i++)
                {
                    var target = routes[i].FromLocationId == family.LocationId
                        ? routes[i].ToLocationId
                        : routes[i].FromLocationId;
                    var migration = NewAction(
                        agent, context, WorldActionTypeIds.MigrateHousehold,
                        "migrate." + target);
                    migration.ExpectedBenefitBasisPoints = 5_000;
                    migration.CostBasisPoints = 4_000;
                    migration.RiskBasisPoints =
                        Math.Max(0, 10_000 - routes[i].SecurityBasisPoints);
                    AddArg(migration, "target_location_id", target);
                    AddArg(migration, "route_id", routes[i].Id);
                    AddArg(migration, "time_days",
                        Math.Max(1, routes[i].DistanceKilometers / 20).ToString());
                    candidates.Add(migration);
                }
            }
        }

        private static void AddOrganization(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            ICollection<WorldActionIntent> candidates)
        {
            var organization = world.Organizations.Find(item =>
                item.Id == agent.AgentId);
            if (organization == null)
            {
                return;
            }
            var profile = world.FamilyOrganizationProfiles.Find(item =>
                item.OrganizationId == organization.Id);
            if (organization.Type == OrganizationType.Merchant && profile != null)
            {
                AddMerchantSale(world, agent, context, profile, candidates);
            }
            if (profile != null && profile.FamilyAssets > 0)
            {
                var invest = NewAction(
                    agent, context, WorldActionTypeIds.Invest, "family_invest");
                invest.ExpectedBenefitBasisPoints = 5_000;
                invest.CostBasisPoints = 3_000;
                AddArg(invest, "asset_cost",
                    Math.Max(1L, profile.FamilyAssets / 10L).ToString());
                AddArg(invest, "time_days", "30");
                candidates.Add(invest);
            }
        }

        private static void AddMerchantSale(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            FamilyOrganizationProfileState profile,
            ICollection<WorldActionIntent> candidates)
        {
            var governance = world.CountyGovernances.Find(item =>
                item.CountyLocationId == context.LocationId);
            if (governance == null)
            {
                return;
            }
            var batches = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == profile.SourceFamilyId &&
                    batch.Quantity > batch.ReservedQuantity &&
                    !string.IsNullOrEmpty(batch.StorageFacilityId))
                {
                    batches.Add(batch);
                }
            }
            batches.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < batches.Count && i < 3; i++)
            {
                var quantity = Math.Max(
                    1L,
                    Math.Min(20L, batches[i].Quantity - batches[i].ReservedQuantity));
                var sale = NewAction(
                    agent, context, WorldActionTypeIds.CreateTradeOrder,
                    "sell." + batches[i].Id);
                sale.ExpectedBenefitBasisPoints =
                    Signal(context, WorldSignalIds.ProfitOpportunity);
                sale.CostBasisPoints = 1_000;
                sale.RiskBasisPoints =
                    Signal(context, WorldSignalIds.RouteRisk);
                AddArg(sale, "county_governance_id", governance.Id);
                AddArg(sale, "family_id", profile.SourceFamilyId);
                AddArg(sale, "source_container_id", batches[i].InventoryContainerId);
                AddArg(sale, "storage_facility_id", batches[i].StorageFacilityId);
                AddArg(sale, "product_definition_id", batches[i].ProductDefinitionId);
                AddArg(sale, "quantity", quantity.ToString());
                AddArg(sale, "minimum_unit_price", "100");
                AddArg(sale, "minimum_quality_basis_points", "0");
                AddArg(sale, "time_days", "1");
                candidates.Add(sale);
            }
        }

        private static void AddGovernment(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            ICollection<WorldActionIntent> candidates)
        {
            var governance = world.CountyGovernances.Find(item =>
                item.GovernmentOrganizationId == agent.AgentId);
            if (governance == null)
            {
                return;
            }
            var purchase = NewAction(
                agent, context, WorldActionTypeIds.CreateGovernmentPurchase,
                "food_relief_purchase");
            purchase.ExpectedBenefitBasisPoints =
                Signal(context, WorldSignalIds.FoodPressure);
            purchase.CostBasisPoints = 4_000;
            AddArg(purchase, "county_governance_id", governance.Id);
            AddArg(purchase, "quantity", "100");
            AddArg(purchase, "maximum_unit_price", "100");
            AddArg(purchase, "product_definition_id", "product.wheat_grain");
            AddArg(purchase, "time_days", "1");
            candidates.Add(purchase);
        }

        private static void AddSettlement(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            ICollection<WorldActionIntent> candidates)
        {
            if (Signal(context, WorldSignalIds.HousingPressure) <= 0 ||
                world.FacilityDefinitions.Count == 0)
            {
                return;
            }
            var property = world.CellProperties.Find(item =>
                item.LocationId == context.LocationId &&
                !world.Facilities.Exists(facility =>
                    facility.CellId64 == item.CellId64) &&
                !world.FacilityConstructionProjects.Exists(project =>
                    project.CellId64 == item.CellId64 &&
                    project.Status != FacilityConstructionStatus.Cancelled));
            if (property == null)
            {
                return;
            }
            var container = world.InventoryContainers.Find(item =>
                item.LocationId == context.LocationId &&
                (item.OwnerFamilyId == property.OwnerId ||
                 item.OwnerOrganizationId == property.OwnerId));
            var batch = container == null
                ? null
                : world.ProductBatches.Find(item =>
                    item.InventoryContainerId == container.Id &&
                    item.Quantity > item.ReservedQuantity);
            var worker = world.People.Find(item => item.IsAlive &&
                item.LocationId == context.LocationId &&
                !world.Journeys.Exists(journey =>
                    journey.PersonId == item.Id));
            if (container == null || batch == null || worker == null)
            {
                return;
            }
            var build = NewAction(
                agent, context, WorldActionTypeIds.BuildFacility, "build_pressure");
            build.ExpectedBenefitBasisPoints =
                Signal(context, WorldSignalIds.HousingPressure);
            build.CostBasisPoints = 6_000;
            AddArg(build, "facility_definition_id",
                world.FacilityDefinitions[0].Id);
            AddArg(build, "owner_id", property.OwnerId);
            AddArg(build, "cell_id", property.CellId64.ToString());
            AddArg(build, "material_container_id", container.Id);
            AddArg(build, "material_product_id", batch.ProductDefinitionId);
            AddArg(build, "material_quantity", Math.Min(
                10L, batch.Quantity - batch.ReservedQuantity).ToString());
            AddArg(build, "worker_person_id", worker.Id);
            AddArg(build, "labor_minutes", "480");
            AddArg(build, "construction_days", "90");
            AddArg(build, "money_cost", "0");
            AddArg(build, "time_days", "90");
            candidates.Add(build);
        }

        private static WorldActionIntent NewAction(
            WorldDecisionAgentState agent,
            WorldDecisionContext context,
            string actionTypeId,
            string suffix) =>
            new WorldActionIntent
            {
                Id = "action." + agent.Id + "." + context.DecisionSequence + "." + suffix,
                ActionTypeId = actionTypeId,
                AgentId = agent.AgentId,
                AgentKind = agent.AgentKind,
                LocationId = context.LocationId
            };

        private static void AddArg(
            WorldActionIntent action,
            string key,
            string value) =>
            action.Arguments.Add(new WorldActionArgument { Key = key, Value = value });

        private static int Signal(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id)
                ?.ValueBasisPoints ?? 0;

        private static List<RouteState> AdjacentRoutes(
            WorldState world,
            string locationId)
        {
            var routes = new List<RouteState>();
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].FromLocationId == locationId ||
                    world.Routes[i].Bidirectional &&
                    world.Routes[i].ToLocationId == locationId)
                {
                    routes.Add(world.Routes[i]);
                }
            }
            routes.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return routes;
        }
    }

    public sealed class WorldActionExecutionResult
    {
        public WorldActionValidationStatus Status;
        public string ReasonId;
        public string CreatedEntityId;
        public bool WorldChanged;
    }

    public sealed class LivingWorldActionExecutor
    {
        private readonly WorldActionValidator _validator;
        private readonly ProductionContentRegistry _content;

        public LivingWorldActionExecutor(
            ProductionContentRegistry content = null,
            WorldActionValidator validator = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _validator = validator ?? new WorldActionValidator();
        }

        public WorldActionExecutionResult Execute(
            WorldState world,
            WorldActionIntent action)
        {
            var validation = _validator.Validate(world, action);
            if (!validation.CanExecute)
            {
                return Result(validation.Status, validation.ReasonId, null, false);
            }
            try
            {
                switch (action.ActionTypeId)
                {
                    case WorldActionTypeIds.NoAction:
                    case WorldActionTypeIds.Observe:
                        return Result(
                            WorldActionValidationStatus.Valid,
                            "no_world_mutation",
                            null,
                            false);
                    case WorldActionTypeIds.CreateMarketBuyOrder:
                        return ExecuteBuy(world, action);
                    case WorldActionTypeIds.CreateTradeOrder:
                        return ExecuteSell(world, action, validation.ExecutableQuantity);
                    case WorldActionTypeIds.Invest:
                        return ExecuteFamilyInvestment(world, action);
                    case WorldActionTypeIds.MigrateHousehold:
                        return ExecuteHouseholdMigration(world, action);
                    case WorldActionTypeIds.BuildFacility:
                        return ExecuteConstruction(world, action);
                    case WorldActionTypeIds.CreateGovernmentPurchase:
                        return ExecuteGovernmentPurchase(world, action);
                    default:
                        return Result(
                            WorldActionValidationStatus.Deferred,
                            "domain_command_adapter_required",
                            null,
                            false);
                }
            }
            catch (InvalidOperationException)
            {
                return Result(
                    WorldActionValidationStatus.Deferred,
                    "domain_precondition_not_ready",
                    null,
                    false);
            }
        }

        public void RecordOutcome(
            WorldState world,
            WorldDecisionAgentState agent,
            WorldActionIntent action,
            WorldActionExecutionResult result,
            int shortOutcomeBasisPoints = 0,
            int longOutcomeBasisPoints = 0)
        {
            if (world == null || agent == null || action == null || result == null)
            {
                throw new ArgumentNullException(
                    "Decision outcome recording input is null.");
            }
            shortOutcomeBasisPoints = Math.Max(
                -10_000, Math.Min(10_000, shortOutcomeBasisPoints));
            longOutcomeBasisPoints = Math.Max(
                -10_000, Math.Min(10_000, longOutcomeBasisPoints));
            agent.Memory ??= new List<WorldDecisionMemoryEntryState>();
            agent.Memory.Add(new WorldDecisionMemoryEntryState
            {
                Id = "decision_memory." + agent.Id + "." +
                    Math.Max(0, agent.DecisionSequence - 1),
                Day = world.AbsoluteDay,
                ActionId = action.Id,
                ActionTypeId = action.ActionTypeId,
                ValidationReasonId = result.ReasonId ?? string.Empty,
                Executed = result.Status == WorldActionValidationStatus.Valid &&
                    (result.WorldChanged ||
                     action.ActionTypeId == WorldActionTypeIds.NoAction ||
                     action.ActionTypeId == WorldActionTypeIds.Observe),
                ShortOutcomeBasisPoints = shortOutcomeBasisPoints,
                LongOutcomeBasisPoints = longOutcomeBasisPoints
            });
            const int maximumMemoryEntries = 32;
            if (agent.Memory.Count > maximumMemoryEntries)
            {
                agent.Memory.RemoveRange(
                    0, agent.Memory.Count - maximumMemoryEntries);
            }
        }

        private WorldActionExecutionResult ExecuteBuy(
            WorldState world,
            WorldActionIntent action)
        {
            var market = new FormalCountyMarketSystem(_content);
            var order = market.CreateBuyOrder(
                world,
                Arg(action, "county_governance_id"),
                action.AgentId,
                Arg(action, "storage_facility_id"),
                Arg(action, "product_definition_id"),
                ParseLong(action, "quantity"),
                ParseLong(action, "maximum_unit_price"),
                ParseInt(action, "minimum_quality_basis_points"),
                checked(world.AbsoluteDay + Math.Max(1, ParseInt(action, "time_days"))));
            return Result(
                WorldActionValidationStatus.Valid,
                "formal_market_buy_order_created",
                order.Id,
                true);
        }

        private WorldActionExecutionResult ExecuteSell(
            WorldState world,
            WorldActionIntent action,
            long executableQuantity)
        {
            var market = new FormalCountyMarketSystem(_content);
            var order = market.CreateSellOrder(
                world,
                Arg(action, "county_governance_id"),
                Arg(action, "family_id"),
                Arg(action, "storage_facility_id"),
                Arg(action, "product_definition_id"),
                Math.Min(ParseLong(action, "quantity"), executableQuantity),
                ParseLong(action, "minimum_unit_price"),
                ParseInt(action, "minimum_quality_basis_points"),
                checked(world.AbsoluteDay + Math.Max(1, ParseInt(action, "time_days"))));
            return Result(
                WorldActionValidationStatus.Valid,
                "formal_market_sell_order_created",
                order.Id,
                true);
        }

        private static WorldActionExecutionResult ExecuteFamilyInvestment(
            WorldState world,
            WorldActionIntent action)
        {
            var profile = world.FamilyOrganizationProfiles.Find(item =>
                item.OrganizationId == action.AgentId);
            var cost = ParseLong(action, "asset_cost");
            profile.FamilyAssets = checked(profile.FamilyAssets - cost);
            world.Revision = checked(world.Revision + 1);
            return Result(
                WorldActionValidationStatus.Valid,
                "family_organization_assets_invested",
                null,
                true);
        }

        private static WorldActionExecutionResult ExecuteHouseholdMigration(
            WorldState world,
            WorldActionIntent action)
        {
            var migration = new HouseholdMigrationSystem().Start(
                world,
                action.AgentId,
                Arg(action, "target_location_id"),
                Arg(action, "route_id"));
            return Result(
                WorldActionValidationStatus.Valid,
                "household_migration_journeys_started",
                migration.Id,
                true);
        }

        private static WorldActionExecutionResult ExecuteConstruction(
            WorldState world,
            WorldActionIntent action)
        {
            var construction = new PropertyConstructionSystem();
            var project = construction.StartProject(
                world,
                action.LocationId,
                ulong.Parse(Arg(action, "cell_id")),
                Arg(action, "facility_definition_id"),
                Arg(action, "owner_id"),
                Arg(action, "worker_person_id"),
                Arg(action, "material_container_id"),
                Arg(action, "material_product_id"),
                ParseLong(action, "material_quantity"),
                ParseInt(action, "labor_minutes"),
                ParseInt(action, "construction_days"),
                ParseLong(action, "money_cost"));
            construction.ContributeLabor(
                world,
                project.Id,
                Arg(action, "worker_person_id"),
                ParseInt(action, "labor_minutes"));
            return Result(
                WorldActionValidationStatus.Valid,
                "facility_construction_project_started",
                project.Id,
                true);
        }

        private WorldActionExecutionResult ExecuteGovernmentPurchase(
            WorldState world,
            WorldActionIntent action)
        {
            var governanceId = Arg(action, "county_governance_id");
            var sourceEvent = WorldActionValidator
                .FindLatestReliefShortfallEvent(world, governanceId) ??
                throw new InvalidOperationException(
                    "Committed relief shortfall event is missing.");
            var scheduler = new PublicReliefProcurementCommandScheduler(
                new PublicReliefProcurementSystem(_content));
            var runtime = new WorldCommandRuntime();
            runtime.RegisterHandler(scheduler.CreateCommandHandler());
            var commandId = PublicReliefProcurementCommandScheduler.CommandId(
                sourceEvent.Day, governanceId);
            runtime.Enqueue(world, new WorldCommandEnvelope(
                commandId,
                PublicReliefProcurementCommandScheduler.CommandTypeId,
                action.AgentId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                5,
                new Dictionary<string, string>
                {
                    { PublicReliefProcurementCommandScheduler.ExpectedDayArgumentId,
                        world.AbsoluteDay.ToString() },
                    { PublicReliefProcurementCommandScheduler.GovernanceIdArgumentId,
                        governanceId },
                    { PublicReliefProcurementCommandScheduler.SourceEventIdArgumentId,
                        sourceEvent.Id },
                    { PublicReliefProcurementCommandScheduler.MaximumQuantityArgumentId,
                        ParseLong(action, "quantity").ToString() },
                    { PublicReliefProcurementCommandScheduler.MaximumBudgetArgumentId,
                        checked(ParseLong(action, "quantity") *
                            ParseLong(action, "maximum_unit_price")).ToString() },
                    { PublicReliefProcurementCommandScheduler.MaximumUnitPriceArgumentId,
                        ParseLong(action, "maximum_unit_price").ToString() }
                }));
            runtime.ProcessDue(world);
            return Result(
                WorldActionValidationStatus.Valid,
                "government_purchase_command_executed",
                commandId,
                true);
        }

        private static WorldActionExecutionResult Result(
            WorldActionValidationStatus status,
            string reason,
            string created,
            bool changed) =>
            new WorldActionExecutionResult
            {
                Status = status,
                ReasonId = reason,
                CreatedEntityId = created,
                WorldChanged = changed
            };

        private static string Arg(WorldActionIntent action, string key) =>
            action.Arguments.Find(item => item.Key == key)?.Value ?? string.Empty;

        private static long ParseLong(WorldActionIntent action, string key) =>
            long.Parse(Arg(action, key));

        private static int ParseInt(WorldActionIntent action, string key)
        {
            var value = Arg(action, key);
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }
    }

    public static class NeuralPolicyFeatureSchema
    {
        public const string FeatureSchemaVersion = "ai.features.v1";
        public const string ActionSchemaVersion = "ai.actions.v1";
        public static readonly string[] FeatureIds =
        {
            "feature.food_pressure",
            "feature.housing_pressure",
            "feature.population_pressure",
            "feature.profit_opportunity",
            "feature.route_risk",
            "feature.security_risk",
            "feature.expected_benefit",
            "feature.cost",
            "feature.action_risk",
            "feature.agent_risk_tolerance",
            "feature.goal_affinity",
            "feature.validation_feasibility"
        };
    }

    public sealed class NeuralPolicyModelScorer : INeuralActionScorer
    {
        private readonly NeuralPolicyModelDefinition _model;
        private readonly WorldState _world;
        private readonly WorldActionValidator _validator;
        private readonly WorldAgentDecisionProfileResolver _profiles;

        public NeuralPolicyModelScorer(
            NeuralPolicyModelDefinition model,
            WorldState world,
            WorldActionValidator validator = null)
        {
            LivingWorldRuntimeRules.ValidateNeuralModel(model);
            _model = model;
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _validator = validator ?? new WorldActionValidator();
            _profiles = new WorldAgentDecisionProfileResolver();
        }

        public string ModelVersion => _model.ModelVersion;

        public int Score(WorldDecisionContext context, WorldActionIntent action)
        {
            if (_model.FeatureSchemaVersion !=
                    NeuralPolicyFeatureSchema.FeatureSchemaVersion ||
                _model.ActionSchemaVersion !=
                    NeuralPolicyFeatureSchema.ActionSchemaVersion ||
                _model.FeatureIds.Count !=
                    NeuralPolicyFeatureSchema.FeatureIds.Length)
            {
                throw new InvalidOperationException("neural_schema_mismatch");
            }
            for (var i = 0; i < _model.FeatureIds.Count; i++)
            {
                if (_model.FeatureIds[i] !=
                    NeuralPolicyFeatureSchema.FeatureIds[i])
                {
                    throw new InvalidOperationException("neural_feature_order_mismatch");
                }
            }
            var raw = BuildFeatures(context, action);
            var normalized = new double[raw.Length];
            for (var i = 0; i < raw.Length; i++)
            {
                normalized[i] = Math.Max(
                    0d,
                    Math.Min(
                        1d,
                        (raw[i] - _model.FeatureMinimums[i]) /
                        (_model.FeatureMaximums[i] - _model.FeatureMinimums[i])));
            }
            var hidden = new double[_model.HiddenSize];
            for (var h = 0; h < hidden.Length; h++)
            {
                var value = _model.HiddenBiases[h];
                for (var f = 0; f < normalized.Length; f++)
                {
                    value += normalized[f] *
                        _model.HiddenWeights[h * normalized.Length + f];
                }
                hidden[h] = Math.Max(0d, value);
            }
            var output = _model.OutputBias;
            for (var h = 0; h < hidden.Length; h++)
            {
                output += hidden[h] * _model.OutputWeights[h];
            }
            if (double.IsNaN(output) || double.IsInfinity(output))
            {
                throw new InvalidOperationException("neural_output_not_finite");
            }
            return checked((int)Math.Max(
                int.MinValue / 4d,
                Math.Min(int.MaxValue / 4d, Math.Round(output))));
        }

        private double[] BuildFeatures(
            WorldDecisionContext context,
            WorldActionIntent action)
        {
            var profile = _profiles.Resolve(_world, context);
            var validation = _validator.Validate(_world, action);
            return new[]
            {
                (double)Signal(context, WorldSignalIds.FoodPressure),
                Signal(context, WorldSignalIds.HousingPressure),
                Signal(context, WorldSignalIds.PopulationPressure),
                Signal(context, WorldSignalIds.ProfitOpportunity),
                Signal(context, WorldSignalIds.RouteRisk),
                Signal(context, WorldSignalIds.SecurityRisk),
                action.ExpectedBenefitBasisPoints,
                action.CostBasisPoints,
                action.RiskBasisPoints,
                profile.Personality.RiskTolerance,
                UtilityDecisionEngine.GoalAffinity(
                    profile.GoalId, action.ActionTypeId),
                validation.CanExecute ? 10_000d : 0d
            };
        }

        private static int Signal(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id)
                ?.ValueBasisPoints ?? 0;
    }
}
