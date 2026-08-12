using System;
using System.Collections.Generic;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void GenerateDecisionArenaEvidenceTest()
        {
            var output = Path.Combine(
                Environment.CurrentDirectory, "tmp", "world-decision-arena-v1");
            Directory.CreateDirectory(output);
            var model = new NeuralPolicyModelReader().Read(Path.Combine(
                Environment.CurrentDirectory,
                "Docs", "HISTORICAL_WORLD_REFERENCE",
                "WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1",
                "MODEL", "model.json"));
            var runPath = Path.Combine(output, "arena_runs.jsonl");
            var tracePath = Path.Combine(output, "decision_traces.jsonl");
            var metricPath = Path.Combine(output, "arena_metrics.csv");
            var totalRuns = 0;
            var totalDecisions = 0L;
            var totalChanged = 0L;
            using (var writer = new StreamWriter(runPath, false))
            using (var traceWriter = new StreamWriter(tracePath, false))
            using (var metricWriter = new StreamWriter(metricPath, false))
            {
                metricWriter.WriteLine(
                    "benchmark_id,seed,policy_id,day,living_persons,households," +
                    "food_stock,facilities,trade_volume,family_assets," +
                    "government_reserve,settlement_population");
                foreach (var benchmarkId in WorldSimulationArenaBenchmarkIds.All)
                {
                    for (ulong seed = 1; seed <= 100; seed++)
                    {
                        foreach (var policyId in ArenaPolicyIds())
                        {
                            var world = CreatePolicyWorld(seed);
                            ApplyArenaBenchmark(world, benchmarkId, seed);
                            var policy = CreateArenaPolicy(policyId, model, world);
                            var executor = new LivingWorldActionExecutor();
                            var generator = new LivingWorldCandidateGenerator();
                            var run = new WorldSimulationArena().Run(
                                world,
                                new WorldSimulationArenaScenario
                                {
                                    Id = benchmarkId,
                                    WorldSeed = seed,
                                    DurationDays = 3650,
                                    DecisionCadenceDays = 365,
                                    PolicySetId = policyId,
                                    AgentStateIds = world.WorldDecisionAgents
                                        .ConvertAll(item => item.Id),
                                    CheckpointDays = new List<int>
                                    {
                                        365, 1825, 3650
                                    }
                                },
                                policy,
                                (state, agent) =>
                                {
                                    var context = ArenaContext(state, agent);
                                    return generator.Generate(state, agent, context);
                                },
                                null,
                                (state, agent, action) =>
                                {
                                    var result = executor.Execute(state, action);
                                    executor.RecordOutcome(
                                        state, agent, action, result,
                                        result.WorldChanged ? 1_000 : 0,
                                        0);
                                    return result;
                                });
                            totalRuns++;
                            totalDecisions += run.DecisionTrace.Count;
                            totalChanged += run.DecisionTrace.FindAll(item =>
                                item.WorldChanged).Count;
                            var actionCounts = new SortedDictionary<string, int>(
                                StringComparer.Ordinal);
                            for (var i = 0; i < run.DecisionTrace.Count; i++)
                            {
                                var trace = run.DecisionTrace[i];
                                var action = trace.ActionTypeId;
                                actionCounts.TryGetValue(action, out var count);
                                actionCounts[action] = count + 1;
                                traceWriter.WriteLine(
                                    ArenaDecisionTraceJson(
                                        benchmarkId, seed, policyId, trace));
                            }
                            for (var i = 0; i < run.Metrics.Count; i++)
                            {
                                metricWriter.WriteLine(ArenaMetricCsv(
                                    benchmarkId, seed, policyId,
                                    run.Metrics[i]));
                            }
                            writer.WriteLine(ArenaRunJson(
                                benchmarkId,
                                seed,
                                policyId,
                                run,
                                actionCounts));
                        }
                    }
                }
            }
            File.WriteAllText(
                Path.Combine(output, "aggregate.json"),
                "{\n" +
                "  \"schema\": \"world-decision-arena-evidence-v1\",\n" +
                "  \"benchmark_count\": 10,\n" +
                "  \"seed_count\": 100,\n" +
                "  \"policy_count\": 4,\n" +
                "  \"run_count\": " + totalRuns + ",\n" +
                "  \"decision_count\": " + totalDecisions + ",\n" +
                "  \"changed_world_count\": " + totalChanged + ",\n" +
                "  \"checkpoint_days\": [365,1825,3650],\n" +
                "  \"deterministic_seed_contract\": " +
                    "\"MasterSeed+agentId+decisionSequence+absoluteDay+actionId\",\n" +
                "  \"online_learning\": false\n" +
                "}\n");
            File.WriteAllText(
                Path.Combine(output, "counterfactuals.json"),
                "{\n" +
                "  \"cases\": [\"" +
                string.Join("\",\"", new[]
                {
                    WorldSimulationArenaCounterfactualIds.LowLuoyangFood,
                    WorldSimulationArenaCounterfactualIds.RouteBlock,
                    WorldSimulationArenaCounterfactualIds.HenanCropFailure,
                    WorldSimulationArenaCounterfactualIds.InMigration,
                    WorldSimulationArenaCounterfactualIds.MassFlight,
                    WorldSimulationArenaCounterfactualIds.KeyPersonDeath,
                    WorldSimulationArenaCounterfactualIds.GovernmentFunds,
                    WorldSimulationArenaCounterfactualIds.FamilyAssets
                }) + "\"],\n" +
                "  \"method\": \"paired same-seed initial-world perturbation\",\n" +
                "  \"year_feature_used\": false\n" +
                "}\n");
            Assert.That(totalRuns, Is.EqualTo(4_000));
            Assert.That(totalDecisions, Is.GreaterThan(0));
        }

        [Test]
        public void GenerateHistoricalEventOutcomeEvidenceTest()
        {
            var output = Path.Combine(
                Environment.CurrentDirectory, "tmp", "world-decision-arena-v1");
            Directory.CreateDirectory(output);
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            using (var writer = new StreamWriter(
                Path.Combine(output, "historical_event_outcomes.jsonl"), false))
            using (var eventWriter = new StreamWriter(
                Path.Combine(output, "event_traces.jsonl"), false))
            {
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    var world = CreateEventWorld();
                    world.MasterSeed = seed;
                    var earliest = seed % 4 == 3 ? 10 : 0;
                    if (seed % 4 == 0)
                    {
                        world.Facilities[0].LifecycleStatus =
                            FacilityLifecycleStatus.Destroyed;
                    }
                    else if (seed % 4 == 1)
                    {
                        world.People[0].LocationId = "location.changan";
                    }
                    world.HistoricalEventDefinitions.Add(
                        CreatePrototype(earliest));
                    new HistoricalEventSystem().ResolveEligibleEvents(world);
                    var anchor = world.HistoricalAnchors[0];
                    var outcome = anchor.OutcomeKind + "/" + anchor.Status;
                    counts.TryGetValue(outcome, out var count);
                    counts[outcome] = count + 1;
                    writer.WriteLine(
                        "{\"seed\":" + seed +
                        ",\"event_id\":\"event.luoyang_189_190\"" +
                        ",\"outcome_kind\":\"" + anchor.OutcomeKind + "\"" +
                        ",\"status\":\"" + anchor.Status + "\"" +
                        ",\"preconditions_required\":true" +
                        ",\"year_only_rule\":false}");
                    eventWriter.WriteLine(
                        "{\"seed\":" + seed +
                        ",\"day\":" + world.AbsoluteDay +
                        ",\"event_id\":\"event.luoyang_189_190\"" +
                        ",\"outcome_kind\":\"" + anchor.OutcomeKind + "\"" +
                        ",\"status\":\"" + anchor.Status + "\"" +
                        ",\"actual_outcome_id\":\"" +
                            EscapeJson(anchor.ActualOutcome) + "\"}");
                }
            }
            var rows = new List<string>();
            foreach (var pair in counts)
            {
                rows.Add("\"" + EscapeJson(pair.Key) + "\":" + pair.Value);
            }
            File.WriteAllText(
                Path.Combine(output, "historical_event_aggregate.json"),
                "{\"seed_count\":100,\"event_id\":" +
                "\"event.luoyang_189_190\",\"outcomes\":{" +
                string.Join(",", rows) + "}}\n");
            Assert.That(counts.Count, Is.GreaterThanOrEqualTo(3));
        }

        private static string[] ArenaPolicyIds() => new[]
        {
            DecisionPolicyIds.Rule,
            DecisionPolicyIds.Utility,
            DecisionPolicyIds.RandomizedUtilityV1,
            DecisionPolicyIds.NeuralAdapter
        };

        private static string ArenaRunJson(
            string benchmarkId,
            ulong seed,
            string policyId,
            WorldSimulationArenaRun run,
            SortedDictionary<string, int> actionCounts)
        {
            var actions = new List<string>();
            foreach (var pair in actionCounts)
            {
                actions.Add("\"" + EscapeJson(pair.Key) + "\":" + pair.Value);
            }
            var metrics = new List<string>();
            for (var i = 0; i < run.Metrics.Count; i++)
            {
                var metric = run.Metrics[i];
                metrics.Add(
                    "{\"day\":" + metric.Day +
                    ",\"living_persons\":" + metric.LivingPersons +
                    ",\"households\":" + metric.HouseholdCount +
                    ",\"food_stock\":" + metric.FoodStock +
                    ",\"facilities\":" + metric.FacilityCount +
                    ",\"trade_volume\":" + metric.TradeVolume +
                    ",\"family_assets\":" + metric.FamilyOrganizationAssets +
                    ",\"government_reserve\":" + metric.GovernmentReserve +
                    ",\"settlement_population\":" + metric.SettlementPopulation +
                    "}");
            }
            return "{" +
                "\"benchmark_id\":\"" + EscapeJson(benchmarkId) + "\"," +
                "\"source_slice_id\":\"" +
                    (benchmarkId == WorldSimulationArenaBenchmarkIds.Luoyang189190
                        ? "location.luoyang/location.henan"
                        : "synthetic_contract_fixture") + "\"," +
                "\"seed\":" + seed + "," +
                "\"policy_id\":\"" + EscapeJson(policyId) + "\"," +
                "\"duration_days\":3650," +
                "\"decision_cadence_days\":365," +
                "\"elapsed_milliseconds\":" + run.ElapsedMilliseconds + "," +
                "\"decision_count\":" + run.DecisionTrace.Count + "," +
                "\"changed_world_count\":" +
                    run.DecisionTrace.FindAll(item => item.WorldChanged).Count + "," +
                "\"action_counts\":{" + string.Join(",", actions) + "}," +
                "\"checkpoints\":[" + string.Join(",", metrics) + "]" +
                "}";
        }

        private static string ArenaDecisionTraceJson(
            string benchmarkId,
            ulong seed,
            string policyId,
            WorldSimulationArenaTraceEntry trace) =>
            "{" +
            "\"benchmark_id\":\"" + EscapeJson(benchmarkId) + "\"," +
            "\"seed\":" + seed + "," +
            "\"policy_id\":\"" + EscapeJson(policyId) + "\"," +
            "\"day\":" + trace.Day + "," +
            "\"agent_id\":\"" + EscapeJson(trace.AgentId) + "\"," +
            "\"decision_sequence\":" + trace.DecisionSequence + "," +
            "\"action_id\":\"" + EscapeJson(trace.ActionId) + "\"," +
            "\"action_type_id\":\"" +
                EscapeJson(trace.ActionTypeId) + "\"," +
            "\"validation_reason_id\":\"" +
                EscapeJson(trace.ValidationReasonId) + "\"," +
            "\"execution_reason_id\":\"" +
                EscapeJson(trace.ExecutionReasonId) + "\"," +
            "\"world_changed\":" +
                (trace.WorldChanged ? "true" : "false") + "," +
            "\"selected_score_basis_points\":" +
                trace.SelectedScoreBasisPoints + "," +
            "\"score_explanation\":\"" +
                EscapeJson(trace.ScoreExplanation) + "\"}";

        private static string ArenaMetricCsv(
            string benchmarkId,
            ulong seed,
            string policyId,
            WorldSimulationArenaMetric metric) =>
            benchmarkId + "," + seed + "," + policyId + "," +
            metric.Day + "," + metric.LivingPersons + "," +
            metric.HouseholdCount + "," + metric.FoodStock + "," +
            metric.FacilityCount + "," + metric.TradeVolume + "," +
            metric.FamilyOrganizationAssets + "," +
            metric.GovernmentReserve + "," + metric.SettlementPopulation;

        private static string EscapeJson(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static IDecisionPolicy CreateArenaPolicy(
            string policyId,
            NeuralPolicyModelDefinition model,
            WorldState world)
        {
            switch (policyId)
            {
                case DecisionPolicyIds.Rule:
                    return new RuleDecisionPolicy();
                case DecisionPolicyIds.Utility:
                    return new UtilityDecisionPolicy();
                case DecisionPolicyIds.RandomizedUtilityV1:
                    return new RandomizedUtilityDecisionPolicy();
                case DecisionPolicyIds.NeuralAdapter:
                    return new NeuralDecisionPolicyAdapter(
                        new NeuralPolicyModelScorer(model, world));
                default:
                    throw new InvalidOperationException(policyId);
            }
        }

        private static WorldDecisionContext ArenaContext(
            WorldState world,
            WorldDecisionAgentState agent)
        {
            string locationId;
            if (agent.AgentKind == WorldAgentKind.Household)
            {
                locationId = world.Families.Find(item => item.Id == agent.AgentId)
                    .LocationId;
            }
            else if (agent.AgentKind == WorldAgentKind.Settlement)
            {
                locationId = agent.AgentId;
            }
            else
            {
                locationId = world.Organizations.Find(item =>
                    item.Id == agent.AgentId).HeadquartersLocationId;
            }
            return new LivingWorldSignalCalculator().BuildContext(
                world,
                agent.AgentId,
                agent.AgentKind,
                locationId,
                agent.DecisionSequence);
        }

        private static void ApplyArenaBenchmark(
            WorldState world,
            string benchmarkId,
            ulong seed)
        {
            var location = world.Locations[0];
            var batch = world.ProductBatches[0];
            switch (benchmarkId)
            {
                case WorldSimulationArenaBenchmarkIds.PopulationGrowth:
                    location.Population = 100_000 + (int)seed * 10;
                    break;
                case WorldSimulationArenaBenchmarkIds.PopulationDecline:
                    location.Population = 1;
                    break;
                case WorldSimulationArenaBenchmarkIds.FoodShortage:
                    batch.Quantity = 5;
                    break;
                case WorldSimulationArenaBenchmarkIds.FoodSurplus:
                    batch.Quantity = 100_000;
                    break;
                case WorldSimulationArenaBenchmarkIds.HousingPressure:
                    location.Population = 1_000_000;
                    break;
                case WorldSimulationArenaBenchmarkIds.TradeOpportunity:
                    location.GrainPrice = 1_000;
                    break;
                case WorldSimulationArenaBenchmarkIds.RouteBlocked:
                    world.Routes[0].SecurityBasisPoints = 0;
                    break;
                case WorldSimulationArenaBenchmarkIds.WarPressure:
                    location.PublicOrderBasisPoints = 0;
                    break;
                case WorldSimulationArenaBenchmarkIds.FamilyExpansion:
                    world.FamilyOrganizationProfiles[0].FamilyAssets = 100_000;
                    break;
                case WorldSimulationArenaBenchmarkIds.Luoyang189190:
                    RemapArenaToLuoyang(world);
                    world.Locations[0].Population = 400_000;
                    world.Locations[0].PublicOrderBasisPoints =
                        2_000 + (int)(seed % 8) * 1_000;
                    break;
            }
        }

        private static void RemapArenaToLuoyang(WorldState world)
        {
            world.Locations[0].Id = "location.luoyang";
            world.Locations[0].DisplayName = "Luoyang";
            world.Locations[1].Id = "location.henan";
            world.Locations[1].DisplayName = "Henan";
            world.People[0].LocationId = "location.luoyang";
            world.People[0].BirthLocationId = "location.luoyang";
            world.Families[0].LocationId = "location.luoyang";
            world.Routes[0].FromLocationId = "location.luoyang";
            world.Routes[0].ToLocationId = "location.henan";
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                world.Organizations[i].HeadquartersLocationId = "location.luoyang";
            }
            world.CountyGovernances[0].CountyLocationId = "location.luoyang";
            world.Facilities[0].SettlementId = "location.luoyang";
            world.InventoryContainers[0].LocationId = "location.luoyang";
            world.ProductBatches[0].OriginLocationId = "location.luoyang";
            world.WorldDecisionAgents.Find(item =>
                item.AgentKind == WorldAgentKind.Settlement).AgentId =
                    "location.luoyang";
        }
    }
}
