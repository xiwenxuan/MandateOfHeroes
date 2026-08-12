using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class WorldSimulationArenaScenario
    {
        public string Id;
        public ulong WorldSeed;
        public int DurationDays;
        public int DecisionCadenceDays = 1;
        public string PolicySetId;
        public List<string> AgentStateIds = new List<string>();
        public Dictionary<string, string> PolicyIdByAgentStateId =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public bool HistoricalEventsEnabled = true;
        public List<int> CheckpointDays = new List<int>();
    }

    public sealed class WorldSimulationArenaMetric
    {
        public long Day;
        public int LivingPersons;
        public long ProductQuantity;
        public int ActiveOrders;
        public int InTransitShipments;
        public int CompletedHistoricalEvents;
        public int HouseholdCount;
        public long CultivatedLandUnits;
        public int FacilityCount;
        public int OperationalFacilityCount;
        public int EmployedPersons;
        public long FoodStock;
        public int AverageFoodSecurityBasisPoints;
        public int AverageMarketPrice;
        public long TradeVolume;
        public long MerchantCapital;
        public long FamilyOrganizationAssets;
        public long GovernmentReserve;
        public int ActiveMigrations;
        public int SettlementPopulation;
    }

    public sealed class WorldSimulationArenaTraceEntry
    {
        public long Day;
        public string AgentId;
        public long DecisionSequence;
        public string PolicyId;
        public string ActionId;
        public string ActionTypeId;
        public string ValidationReasonId;
        public string ExecutionReasonId;
        public bool WorldChanged;
        public int SelectedScoreBasisPoints;
        public string ScoreExplanation;
    }

    public sealed class WorldSimulationArenaRun
    {
        public string ScenarioId;
        public ulong WorldSeed;
        public string PolicySetId;
        public long ElapsedMilliseconds;
        public List<WorldSimulationArenaMetric> Metrics =
            new List<WorldSimulationArenaMetric>();
        public List<WorldSimulationArenaTraceEntry> DecisionTrace =
            new List<WorldSimulationArenaTraceEntry>();
        public List<AiTrainingFeatureRow> TrainingRows =
            new List<AiTrainingFeatureRow>();
        public List<WorldSimulationArenaEventTraceEntry> EventTrace =
            new List<WorldSimulationArenaEventTraceEntry>();
    }

    public sealed class AiTrainingFeatureRow
    {
        public string ScenarioId;
        public ulong WorldSeed;
        public long Day;
        public string AgentId;
        public long DecisionSequence;
        public string PolicyId;
        public string PolicyVersion;
        public string ModelVersion;
        public string ActionId;
        public int ActionScoreBasisPoints;
        public string SignalVector;
        public string ValidationReason;
        public string CandidateActionIds;
        public string CandidateScores;
        public string ExecutionReason;
        public int ShortOutcomeBasisPoints;
        public int LongOutcomeBasisPoints;
        public string EventContext;
    }

    public sealed class WorldSimulationArenaEventTraceEntry
    {
        public long Day;
        public string EventId;
        public string Outcome;
        public string ActualOutcomeId;
    }

    public static class WorldSimulationArenaBenchmarkIds
    {
        public const string PopulationGrowth = "BENCH_POP_GROWTH";
        public const string PopulationDecline = "BENCH_POP_DECLINE";
        public const string FoodShortage = "BENCH_FOOD_SHORTAGE";
        public const string FoodSurplus = "BENCH_FOOD_SURPLUS";
        public const string HousingPressure = "BENCH_HOUSING_PRESSURE";
        public const string TradeOpportunity = "BENCH_TRADE_OPPORTUNITY";
        public const string RouteBlocked = "BENCH_ROUTE_BLOCKED";
        public const string WarPressure = "BENCH_WAR_PRESSURE";
        public const string FamilyExpansion = "BENCH_FAMILY_EXPANSION";
        public const string Luoyang189190 = "BENCH_LUOYANG_189_190";

        public static readonly string[] All =
        {
            PopulationGrowth,
            PopulationDecline,
            FoodShortage,
            FoodSurplus,
            HousingPressure,
            TradeOpportunity,
            RouteBlocked,
            WarPressure,
            FamilyExpansion,
            Luoyang189190
        };
    }

    public static class WorldSimulationArenaCounterfactualIds
    {
        public const string LowLuoyangFood = "CF_A_LOW_LUOYANG_FOOD";
        public const string RouteBlock = "CF_B_ROUTE_BLOCK";
        public const string HenanCropFailure = "CF_C_HENAN_CROP_FAILURE";
        public const string InMigration = "CF_D_IN_MIGRATION";
        public const string MassFlight = "CF_E_MASS_FLIGHT";
        public const string KeyPersonDeath = "CF_F_KEY_PERSON_DEATH";
        public const string GovernmentFunds = "CF_G_GOVERNMENT_FUNDS";
        public const string FamilyAssets = "CF_H_FAMILY_ASSETS";
    }

    public sealed class WorldSimulationArena
    {
        private readonly LivingWorldSignalCalculator _signals =
            new LivingWorldSignalCalculator();
        private readonly LivingWorldDecisionService _decisions =
            new LivingWorldDecisionService();

        public WorldSimulationArenaRun Run(
            WorldState world,
            WorldSimulationArenaScenario scenario,
            IDecisionPolicy policy,
            Func<WorldState, WorldDecisionAgentState,
                IReadOnlyList<WorldActionIntent>> candidateProvider,
            Action<WorldState> advanceOneDay = null,
            Func<WorldState, WorldDecisionAgentState, WorldActionIntent,
                WorldActionExecutionResult> executeAction = null)
        {
            if (world == null || scenario == null || policy == null ||
                candidateProvider == null)
            {
                throw new ArgumentNullException(
                    "Simulation Arena input cannot be null.");
            }
            if (scenario.WorldSeed == 0 || scenario.DurationDays < 0 ||
                scenario.DecisionCadenceDays <= 0 ||
                string.IsNullOrWhiteSpace(scenario.Id) ||
                string.IsNullOrWhiteSpace(scenario.PolicySetId))
            {
                throw new InvalidOperationException(
                    "Simulation Arena scenario is invalid.");
            }
            if (world.MasterSeed != 0 && world.MasterSeed != scenario.WorldSeed)
            {
                throw new InvalidOperationException(
                    "Arena World Seed must match the scenario seed.");
            }
            world.MasterSeed = scenario.WorldSeed;

            var run = new WorldSimulationArenaRun
            {
                ScenarioId = scenario.Id,
                WorldSeed = scenario.WorldSeed,
                PolicySetId = scenario.PolicySetId
            };
            var watch = Stopwatch.StartNew();
            for (var day = 0; day <= scenario.DurationDays; day++)
            {
                RunDueAgents(
                    world,
                    scenario,
                    policy,
                    candidateProvider,
                    run,
                    executeAction);
                if (scenario.CheckpointDays.Count == 0 ||
                    scenario.CheckpointDays.Contains(day) ||
                    day == scenario.DurationDays)
                {
                    run.Metrics.Add(CaptureMetric(world));
                }
                CaptureEventTrace(world, run);
                if (day < scenario.DurationDays)
                {
                    if (advanceOneDay != null)
                    {
                        advanceOneDay(world);
                    }
                    else
                    {
                        world.AbsoluteDay = checked(world.AbsoluteDay + 1);
                        world.Revision = checked(world.Revision + 1);
                    }
                }
            }
            watch.Stop();
            run.ElapsedMilliseconds = watch.ElapsedMilliseconds;
            return run;
        }

        private void RunDueAgents(
            WorldState world,
            WorldSimulationArenaScenario scenario,
            IDecisionPolicy policy,
            Func<WorldState, WorldDecisionAgentState,
                IReadOnlyList<WorldActionIntent>> candidateProvider,
            WorldSimulationArenaRun run,
            Func<WorldState, WorldDecisionAgentState, WorldActionIntent,
                WorldActionExecutionResult> executeAction)
        {
            var agents = new List<WorldDecisionAgentState>();
            if (world.AbsoluteDay % scenario.DecisionCadenceDays != 0)
            {
                return;
            }
            for (var i = 0; i < scenario.AgentStateIds.Count; i++)
            {
                var id = scenario.AgentStateIds[i];
                var state = world.WorldDecisionAgents.Find(item => item.Id == id);
                if (state == null)
                {
                    throw new InvalidOperationException(
                        $"Missing arena decision agent {id}.");
                }
                agents.Add(state);
            }
            agents.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                var locationId = ResolveLocation(world, agent);
                if (string.IsNullOrEmpty(locationId))
                {
                    continue;
                }
                var sequence = agent.DecisionSequence;
                var context = _signals.BuildContext(
                    world,
                    agent.AgentId,
                    agent.AgentKind,
                    locationId,
                    sequence);
                var candidates = candidateProvider(world, agent);
                var agentPolicy = ResolvePolicy(scenario, agent, policy);
                var decision = _decisions.Decide(
                    world,
                    agent,
                    context,
                    agentPolicy,
                    candidates);
                var selectedScore = decision.SelectedAction == null
                    ? null
                    : decision.Scores.Find(item =>
                        item.ActionId == decision.SelectedAction.Id);
                var execution = decision.SelectedAction == null || executeAction == null
                    ? null
                    : executeAction(world, agent, decision.SelectedAction);
                run.DecisionTrace.Add(new WorldSimulationArenaTraceEntry
                {
                    Day = world.AbsoluteDay,
                    AgentId = agent.AgentId,
                    DecisionSequence = sequence,
                    PolicyId = decision.PolicyId,
                    ActionId = decision.SelectedAction?.Id ?? string.Empty,
                    ActionTypeId = decision.SelectedAction?.ActionTypeId ?? string.Empty,
                    ValidationReasonId = selectedScore?.Explanation ?? string.Empty,
                    ExecutionReasonId = execution?.ReasonId ?? "not_executed",
                    WorldChanged = execution?.WorldChanged ?? false,
                    SelectedScoreBasisPoints =
                        selectedScore?.ScoreBasisPoints ?? int.MinValue,
                    ScoreExplanation = selectedScore == null
                        ? string.Empty
                        : Explain(selectedScore)
                });
                run.TrainingRows.Add(BuildTrainingRow(
                    scenario,
                    context,
                    decision,
                    candidates,
                    selectedScore,
                    execution));
            }
        }

        private static IDecisionPolicy ResolvePolicy(
            WorldSimulationArenaScenario scenario,
            WorldDecisionAgentState agent,
            IDecisionPolicy fallback)
        {
            if (!scenario.PolicyIdByAgentStateId.TryGetValue(
                    agent.Id, out var configuredPolicyId) ||
                string.IsNullOrWhiteSpace(configuredPolicyId) ||
                configuredPolicyId == fallback.PolicyId)
            {
                return fallback;
            }
            switch (configuredPolicyId)
            {
                case DecisionPolicyIds.Rule:
                    return new RuleDecisionPolicy();
                case DecisionPolicyIds.Utility:
                    return new UtilityDecisionPolicy();
                case DecisionPolicyIds.RandomizedUtilityV1:
                    return new RandomizedUtilityDecisionPolicy();
                case DecisionPolicyIds.NeuralAdapter:
                    return new NeuralDecisionPolicyAdapter(null);
                default:
                    throw new InvalidOperationException(
                        "Unknown Arena policy mapping " + configuredPolicyId + ".");
            }
        }

        private static string ResolveLocation(
            WorldState world,
            WorldDecisionAgentState agent)
        {
            switch (agent.AgentKind)
            {
                case WorldAgentKind.Person:
                    return world.People.Find(item => item.Id == agent.AgentId)
                        ?.LocationId;
                case WorldAgentKind.Household:
                    return world.Families.Find(item => item.Id == agent.AgentId)
                        ?.LocationId;
                case WorldAgentKind.Force:
                    return world.Armies.Find(item => item.Id == agent.AgentId)
                        ?.LocationId;
                case WorldAgentKind.Settlement:
                    return agent.AgentId;
                case WorldAgentKind.Organization:
                case WorldAgentKind.Government:
                    return world.Organizations.Find(item =>
                        item.Id == agent.AgentId)?.HeadquartersLocationId;
                default:
                    return world.Locations.Count == 0
                        ? string.Empty
                        : world.Locations[0].Id;
            }
        }

        private static WorldSimulationArenaMetric CaptureMetric(WorldState world)
        {
            var living = 0;
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].IsAlive)
                {
                    living++;
                }
            }
            var quantity = 0L;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                quantity = checked(quantity + world.ProductBatches[i].Quantity);
            }
            var orders = 0;
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                if (world.FormalMarketOrders[i].Status ==
                    FormalMarketOrderStatus.Active)
                {
                    orders++;
                }
            }
            var shipments = 0;
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                if (world.CivilianFreights[i].Status !=
                    CivilianFreightStatus.Completed)
                {
                    shipments++;
                }
            }
            var events = 0;
            for (var i = 0; i < world.HistoricalAnchors.Count; i++)
            {
                var status = world.HistoricalAnchors[i].Status;
                if (status == HistoricalAnchorStatus.Resolved ||
                    status == HistoricalAnchorStatus.CompletedCanonical ||
                    status == HistoricalAnchorStatus.Variant ||
                    status == HistoricalAnchorStatus.Transformed ||
                    status == HistoricalAnchorStatus.Prevented ||
                    status == HistoricalAnchorStatus.Expired)
                {
                    events++;
                }
            }
            var cultivatedLand = 0L;
            var foodSecurity = 0L;
            var familyWealth = 0L;
            for (var i = 0; i < world.Families.Count; i++)
            {
                cultivatedLand += world.Families[i].CultivatedLandUnits;
                foodSecurity += world.Families[i].FoodSecurityBasisPoints;
                familyWealth += world.Families[i].Wealth;
            }
            var operationalFacilities = 0;
            var employed = 0;
            for (var i = 0; i < world.Facilities.Count; i++)
            {
                if (world.Facilities[i].LifecycleStatus ==
                    FacilityLifecycleStatus.Operational)
                {
                    operationalFacilities++;
                }
                employed += world.Facilities[i].WorkerPersonCount;
            }
            var priceTotal = 0L;
            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                priceTotal += world.FormalMarketPrices[i].LastTradeUnitPrice > 0
                    ? world.FormalMarketPrices[i].LastTradeUnitPrice
                    : world.FormalMarketPrices[i].EquilibriumUnitPrice;
            }
            var tradeVolume = 0L;
            for (var i = 0; i < world.FormalMarketTrades.Count; i++)
            {
                tradeVolume += world.FormalMarketTrades[i].Quantity;
            }
            var familyAssets = 0L;
            for (var i = 0; i < world.FamilyOrganizationProfiles.Count; i++)
            {
                familyAssets += world.FamilyOrganizationProfiles[i].FamilyAssets;
            }
            var governmentReserve = 0L;
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Type == OrganizationType.Government)
                {
                    governmentReserve += world.Organizations[i].Treasury;
                }
            }
            return new WorldSimulationArenaMetric
            {
                Day = world.AbsoluteDay,
                LivingPersons = living,
                ProductQuantity = quantity,
                ActiveOrders = orders,
                InTransitShipments = shipments,
                CompletedHistoricalEvents = events,
                HouseholdCount = world.Families.Count,
                CultivatedLandUnits = cultivatedLand,
                FacilityCount = world.Facilities.Count,
                OperationalFacilityCount = operationalFacilities,
                EmployedPersons = employed,
                FoodStock = quantity,
                AverageFoodSecurityBasisPoints = world.Families.Count == 0
                    ? 0
                    : (int)(foodSecurity / world.Families.Count),
                AverageMarketPrice = world.FormalMarketPrices.Count == 0
                    ? 0
                    : (int)(priceTotal / world.FormalMarketPrices.Count),
                TradeVolume = tradeVolume,
                MerchantCapital = familyWealth,
                FamilyOrganizationAssets = familyAssets,
                GovernmentReserve = governmentReserve,
                ActiveMigrations = world.Journeys.Count,
                SettlementPopulation = world.Locations.Count == 0
                    ? 0
                    : world.Locations[0].Population
            };
        }

        private static string Explain(WorldDecisionScore score)
        {
            var parts = new List<string>();
            for (var i = 0; i < score.Components.Count; i++)
            {
                parts.Add(score.Components[i].ComponentId + "=" +
                    score.Components[i].ContributionBasisPoints);
            }
            return string.Join(";", parts);
        }

        private static AiTrainingFeatureRow BuildTrainingRow(
            WorldSimulationArenaScenario scenario,
            WorldDecisionContext context,
            WorldDecisionResult decision,
            IReadOnlyList<WorldActionIntent> candidates,
            WorldDecisionScore selectedScore,
            WorldActionExecutionResult execution)
        {
            var candidateIds = new List<string>();
            for (var i = 0; i < candidates.Count; i++)
            {
                candidateIds.Add(candidates[i].Id);
            }
            var scores = new List<string>();
            for (var i = 0; i < decision.Scores.Count; i++)
            {
                scores.Add(decision.Scores[i].ActionId + "=" +
                    decision.Scores[i].ScoreBasisPoints);
            }
            var signals = new List<string>();
            for (var i = 0; i < context.Signals.Count; i++)
            {
                signals.Add(context.Signals[i].SignalId + "=" +
                    context.Signals[i].ValueBasisPoints);
            }
            return new AiTrainingFeatureRow
            {
                ScenarioId = scenario.Id,
                WorldSeed = scenario.WorldSeed,
                Day = context.AbsoluteDay,
                AgentId = context.AgentId,
                DecisionSequence = context.DecisionSequence,
                PolicyId = decision.PolicyId,
                PolicyVersion = decision.PolicyVersion,
                ModelVersion = decision.ModelVersion,
                ActionId = decision.SelectedAction?.Id ?? string.Empty,
                ActionScoreBasisPoints =
                    selectedScore?.ScoreBasisPoints ?? int.MinValue,
                SignalVector = string.Join(";", signals),
                ValidationReason = selectedScore?.Explanation ?? string.Empty,
                CandidateActionIds = string.Join(";", candidateIds),
                CandidateScores = string.Join(";", scores),
                ExecutionReason = execution?.ReasonId ?? "not_executed",
                EventContext = scenario.HistoricalEventsEnabled
                    ? "historical_events_enabled"
                    : "historical_events_disabled"
            };
        }

        private static void CaptureEventTrace(
            WorldState world,
            WorldSimulationArenaRun run)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < run.EventTrace.Count; i++)
            {
                known.Add(run.EventTrace[i].EventId + "\n" +
                    run.EventTrace[i].Outcome);
            }
            for (var i = 0; i < world.HistoricalAnchors.Count; i++)
            {
                var anchor = world.HistoricalAnchors[i];
                var key = anchor.Id + "\n" + anchor.Status;
                if (!known.Add(key))
                {
                    continue;
                }
                run.EventTrace.Add(new WorldSimulationArenaEventTraceEntry
                {
                    Day = world.AbsoluteDay,
                    EventId = anchor.Id,
                    Outcome = anchor.Status.ToString(),
                    ActualOutcomeId = anchor.ActualOutcome ?? string.Empty
                });
            }
        }
    }

    public sealed class WorldSimulationArenaBatchRequest
    {
        public string BenchmarkId;
        public int DurationDays;
        public int DecisionCadenceDays = 1;
        public List<ulong> Seeds = new List<ulong>();
        public List<string> PolicyIds = new List<string>();
        public List<int> CheckpointDays = new List<int>();
    }

    public sealed class WorldSimulationArenaBatchResult
    {
        public string BenchmarkId;
        public List<WorldSimulationArenaRun> Runs =
            new List<WorldSimulationArenaRun>();
        public long ElapsedMilliseconds;
        public long ManagedMemoryDeltaBytes;
    }

    public sealed class WorldSimulationArenaBatchRunner
    {
        public WorldSimulationArenaBatchResult Run(
            WorldSimulationArenaBatchRequest request,
            Func<ulong, WorldState> worldFactory,
            Func<string, WorldState, IDecisionPolicy> policyFactory,
            Func<WorldState, List<string>> agentStateIdProvider,
            Action<WorldState> advanceOneDay = null)
        {
            if (request == null || worldFactory == null || policyFactory == null ||
                agentStateIdProvider == null)
            {
                throw new ArgumentNullException("Arena batch input is null.");
            }
            var beforeMemory = GC.GetTotalMemory(true);
            var watch = Stopwatch.StartNew();
            var result = new WorldSimulationArenaBatchResult
            {
                BenchmarkId = request.BenchmarkId
            };
            var generator = new LivingWorldCandidateGenerator();
            for (var seedIndex = 0; seedIndex < request.Seeds.Count; seedIndex++)
            {
                for (var policyIndex = 0;
                     policyIndex < request.PolicyIds.Count;
                     policyIndex++)
                {
                    var world = worldFactory(request.Seeds[seedIndex]);
                    var policyId = request.PolicyIds[policyIndex];
                    var policy = policyFactory(policyId, world);
                    var executor = new LivingWorldActionExecutor();
                    var scenario = new WorldSimulationArenaScenario
                    {
                        Id = request.BenchmarkId,
                        WorldSeed = request.Seeds[seedIndex],
                        DurationDays = request.DurationDays,
                        DecisionCadenceDays = request.DecisionCadenceDays,
                        PolicySetId = policyId,
                        AgentStateIds = agentStateIdProvider(world),
                        CheckpointDays = new List<int>(request.CheckpointDays)
                    };
                    var run = new WorldSimulationArena().Run(
                        world,
                        scenario,
                        policy,
                        (state, agent) =>
                        {
                            var location = ResolveAgentLocation(state, agent);
                            if (string.IsNullOrEmpty(location))
                            {
                                return new List<WorldActionIntent>();
                            }
                            var context = new LivingWorldSignalCalculator()
                                .BuildContext(
                                    state,
                                    agent.AgentId,
                                    agent.AgentKind,
                                    location,
                                    agent.DecisionSequence);
                            return generator.Generate(state, agent, context);
                        },
                        advanceOneDay,
                        (state, agent, action) =>
                        {
                            var execution = executor.Execute(state, action);
                            executor.RecordOutcome(
                                state, agent, action, execution,
                                execution.WorldChanged ? 1_000 : 0,
                                0);
                            return execution;
                        });
                    result.Runs.Add(run);
                }
            }
            watch.Stop();
            result.ElapsedMilliseconds = watch.ElapsedMilliseconds;
            result.ManagedMemoryDeltaBytes = Math.Max(
                0L, GC.GetTotalMemory(true) - beforeMemory);
            return result;
        }

        private static string ResolveAgentLocation(
            WorldState world,
            WorldDecisionAgentState agent)
        {
            if (agent.AgentKind == WorldAgentKind.Household)
            {
                return world.Families.Find(item => item.Id == agent.AgentId)
                    ?.LocationId;
            }
            if (agent.AgentKind == WorldAgentKind.Settlement)
            {
                return agent.AgentId;
            }
            var organization = world.Organizations.Find(item =>
                item.Id == agent.AgentId);
            return organization?.HeadquartersLocationId ??
                (world.Locations.Count == 0 ? string.Empty : world.Locations[0].Id);
        }
    }
}
