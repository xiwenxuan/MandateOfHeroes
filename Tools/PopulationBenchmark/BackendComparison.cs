using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Tools.PopulationBenchmark
{
    internal static class BackendComparisonProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                BackendComparisonOptions options = BackendComparisonOptions.Parse(args);
                InputAudit audit = M13InputReader.ReadAndAudit(Path.Combine(options.ProjectRoot, "Data", "HistoricalPopulation"));
                BenchmarkDataset dataset = DeterministicPopulationGenerator.Generate(audit.PopulationBuckets, options.Count, options.Seed);
                var comparisons = new List<BackendComparisonEvidence>();
                foreach (string backend in new[] { "sqlite", "binary", "hybrid" })
                {
                    BackendRunEvidence first = RunCandidate(backend, dataset, Path.Combine(options.WorkspaceRoot, backend, "run-1"));
                    BackendRunEvidence second = RunCandidate(backend, dataset, Path.Combine(options.WorkspaceRoot, backend, "run-2"));
                    bool deterministic = first.CoreDigest == second.CoreDigest && first.EventDigest == second.EventDigest;
                    if (!deterministic)
                    {
                        throw new InvalidOperationException(backend + " repeated runs produced different deterministic digests.");
                    }
                    comparisons.Add(new BackendComparisonEvidence
                    {
                        Backend = backend,
                        Dependency = GetDependency(backend),
                        License = GetLicense(backend),
                        PlatformNote = GetPlatformNote(backend),
                        MaintenanceComplexity = GetComplexity(backend),
                        FirstRun = first,
                        SecondRun = second,
                        DeterministicRepeat = true
                    });
                    Console.WriteLine("{0}: bytes={1} write_ms={2} core={3}", backend, first.InitialStorageBytes, first.InitialWriteMilliseconds, first.CoreDigest);
                }

                string coreDigest = comparisons[0].FirstRun.CoreDigest;
                string eventDigest = comparisons[0].FirstRun.EventDigest;
                bool crossBackend = comparisons.All(value => value.FirstRun.CoreDigest == coreDigest && value.FirstRun.EventDigest == eventDigest);
                if (!crossBackend)
                {
                    throw new InvalidOperationException("Candidate backends produced different deterministic facts.");
                }

                JObject report = new JObject
                {
                    ["schema_version"] = "m15.p1.report.v1",
                    ["stage"] = "M15-P1",
                    ["status"] = "passed",
                    ["source_layer"] = "pressure_test_v1",
                    ["parameters"] = new JObject
                    {
                        ["person_count"] = options.Count,
                        ["master_seed"] = options.Seed,
                        ["repeat_count_per_backend"] = 2
                    },
                    ["input_audit"] = JObject.FromObject(audit),
                    ["backends"] = JArray.FromObject(comparisons),
                    ["cross_backend_determinism"] = new JObject
                    {
                        ["same_core_digest"] = crossBackend,
                        ["same_event_digest"] = crossBackend,
                        ["core_digest"] = coreDigest,
                        ["event_digest"] = eventDigest,
                        ["passed"] = crossBackend
                    },
                    ["dependency_boundary"] = "Mono.Data.Sqlite is loaded only by the standalone benchmark executable. No database assembly is added to Unity or game runtime assemblies.",
                    ["decision"] = "All three candidates completed the 10,000-person P1 contract. P1 does not select a formal backend; P2 and P3 evidence is still required."
                };
                Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath));
                File.WriteAllText(options.OutputPath, report.ToString(Formatting.Indented), new System.Text.UTF8Encoding(false));
                Console.WriteLine("Result: " + options.OutputPath);
                Console.WriteLine("RESULT status=passed stage=M15-P1 backends=3 people={0} deterministic=true", options.Count);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                Console.Error.WriteLine("RESULT status=failed stage=M15-P1");
                return 1;
            }
        }

        private static BackendRunEvidence RunCandidate(string backend, BenchmarkDataset dataset, string root)
        {
            Directory.CreateDirectory(root);
            using (ICandidatePopulationStore store = CandidateStoreFactory.Create(backend, root))
            {
                Stopwatch writeWatch = Stopwatch.StartNew();
                foreach (PopulationPartition partition in dataset.Partitions.OrderBy(value => value.PartitionId, StringComparer.Ordinal))
                {
                    store.BatchWrite(partition);
                }
                writeWatch.Stop();
                store.ValidatePhysicalStore(dataset.People.Count, dataset.Households.Count, dataset.Events.Count);
                long initialBytes = store.GetPhysicalStorageBytes();

                List<PermanentPersonRecord> people = dataset.People.OrderBy(value => value.PersonId, StringComparer.Ordinal).ToList();
                List<HouseholdRecord> households = dataset.Households.OrderBy(value => value.HouseholdId, StringComparer.Ordinal).ToList();
                PermanentPersonRecord probe = people[0];
                HouseholdRecord probeHousehold = store.GetHousehold(probe.HouseholdId);
                if (!probeHousehold.MemberIds.Contains(probe.PersonId) || store.GetPerson(probe.PersonId).PersonId != probe.PersonId)
                {
                    throw new InvalidOperationException(backend + " failed person/household lookup.");
                }

                LatencyEvidence personLatency = MeasureLatency(people.Take(100).Select(value => (Action)(() => store.GetPerson(value.PersonId))));
                LatencyEvidence householdLatency = MeasureLatency(households.Take(100).Select(value => (Action)(() => store.GetHousehold(value.HouseholdId))));
                var query = new PopulationQuery
                {
                    LocationId = probe.CurrentLocationId,
                    Occupation = probe.Occupation,
                    OrganizationId = probe.PrimaryOrganizationId,
                    Alive = true,
                    Available = probe.Available
                };
                QueryResult queryResult = store.QueryPeople(query);
                if (!queryResult.People.Any(value => value.PersonId == probe.PersonId))
                {
                    throw new InvalidOperationException(backend + " failed indexed combined query.");
                }
                LatencyEvidence queryLatency = MeasureLatency(Enumerable.Range(0, 50).Select(_ => (Action)(() => store.QueryPeople(query))));

                foreach (PopulationPartition partition in dataset.Partitions.Take(3))
                {
                    store.UnloadPartition(partition.PartitionId);
                    store.LoadPartition(partition.PartitionId);
                }
                StoreDigest before = store.ComputeDigest();

                Stopwatch incrementalWatch = Stopwatch.StartNew();
                byte[] incremental = store.CreateIncrementalSave();
                incrementalWatch.Stop();
                Stopwatch checkpointWatch = Stopwatch.StartNew();
                byte[] checkpoint = store.CreateCheckpoint();
                checkpointWatch.Stop();

                Stopwatch dueWatch = Stopwatch.StartNew();
                DueReadResult due = store.ReadDueEvents(30);
                dueWatch.Stop();
                if (due.Events.Count != due.ScannedNodeCount || due.Events.Any(value => value.DueDay > 30))
                {
                    throw new InvalidOperationException(backend + " due queue scanned non-due nodes.");
                }
                store.CommitDueChanges(due.Events);
                DueReadResult afterCommit = store.ReadDueEvents(30);
                if (afterCommit.Events.Count != 0 || afterCommit.ScannedNodeCount != 0)
                {
                    throw new InvalidOperationException(backend + " retained committed events in the active queue.");
                }

                Stopwatch restoreWatch = Stopwatch.StartNew();
                store.RestoreCheckpoint(checkpoint);
                restoreWatch.Stop();
                StoreDigest restored = store.ComputeDigest();
                if (restored.CoreDigest != before.CoreDigest || restored.EventDigest != before.EventDigest)
                {
                    throw new InvalidOperationException(backend + " checkpoint restore changed permanent facts.");
                }

                string[] attentionIds = people.Take(16).Select(value => value.PersonId).ToArray();
                Stopwatch attentionWatch = Stopwatch.StartNew();
                IReadOnlyList<AttentionPersonView> views = store.ExpandAttention(attentionIds);
                store.ReleaseAttention(attentionIds);
                attentionWatch.Stop();
                if (views.Count != attentionIds.Length || store.ComputeDigest().CoreDigest != before.CoreDigest)
                {
                    throw new InvalidOperationException(backend + " attention round trip changed permanent facts.");
                }

                return new BackendRunEvidence
                {
                    PersonCount = dataset.People.Count,
                    HouseholdCount = dataset.Households.Count,
                    PartitionCount = dataset.Partitions.Count,
                    EventCount = dataset.Events.Count,
                    InitialWriteMilliseconds = writeWatch.ElapsedMilliseconds,
                    InitialStorageBytes = initialBytes,
                    PhysicalFileCount = store.GetPhysicalFileCount(),
                    PersonLookup = personLatency,
                    HouseholdLookup = householdLatency,
                    CombinedQuery = queryLatency,
                    CombinedQueryResultCount = queryResult.People.Count,
                    DueReadMilliseconds = dueWatch.ElapsedMilliseconds,
                    DueNodeCount = due.Events.Count,
                    DueScannedNodeCount = due.ScannedNodeCount,
                    IncrementalSaveMilliseconds = incrementalWatch.ElapsedMilliseconds,
                    IncrementalSaveBytes = incremental.Length,
                    CheckpointMilliseconds = checkpointWatch.ElapsedMilliseconds,
                    CheckpointBytes = checkpoint.Length,
                    RestoreMilliseconds = restoreWatch.ElapsedMilliseconds,
                    AttentionRoundTripMilliseconds = attentionWatch.ElapsedMilliseconds,
                    CoreDigest = before.CoreDigest,
                    EventDigest = before.EventDigest,
                    ContractChecks = new List<string>
                    {
                        "physical_store_counts_match",
                        "person_and_household_lookup",
                        "combined_index_query",
                        "partition_unload_reload",
                        "due_index_only_scans_due_nodes",
                        "committed_due_nodes_leave_active_queue",
                        "incremental_save_created",
                        "checkpoint_restore_preserves_digests",
                        "attention_round_trip_preserves_facts"
                    }
                };
            }
        }

        private static LatencyEvidence MeasureLatency(IEnumerable<Action> actions)
        {
            var values = new List<double>();
            foreach (Action action in actions)
            {
                long start = Stopwatch.GetTimestamp();
                action();
                long end = Stopwatch.GetTimestamp();
                values.Add((end - start) * 1000.0 / Stopwatch.Frequency);
            }
            values.Sort();
            return new LatencyEvidence
            {
                SampleCount = values.Count,
                P50Milliseconds = Percentile(values, 0.50),
                P95Milliseconds = Percentile(values, 0.95)
            };
        }

        private static double Percentile(IReadOnlyList<double> values, double percentile)
        {
            int index = Math.Min(values.Count - 1, Math.Max(0, (int)Math.Ceiling(values.Count * percentile) - 1));
            return Math.Round(values[index], 6, MidpointRounding.AwayFromZero);
        }

        private static string GetDependency(string backend) { return backend == "binary" ? "project-owned binary codec" : "Mono.Data.Sqlite + SQLite"; }
        private static string GetLicense(string backend) { return backend == "binary" ? "project license" : "Mono class library MIT; SQLite public domain; no binary redistribution in repository"; }
        private static string GetPlatformNote(string backend) { return backend == "binary" ? "managed .NET file I/O" : "P1 runner uses the Mono.Data.Sqlite assembly bundled with installed Unity Editor 2022.3.62f3c1"; }
        private static string GetComplexity(string backend) { return backend == "sqlite" ? "low schema complexity, strong ad-hoc queries" : backend == "binary" ? "lowest dependency count, highest custom index/migration burden" : "highest integration complexity, separates compact person core from relational indexes"; }
    }

    internal sealed class BackendComparisonOptions
    {
        public string ProjectRoot { get; private set; }
        public string OutputPath { get; private set; }
        public string WorkspaceRoot { get; private set; }
        public int Count { get; private set; }
        public long Seed { get; private set; }

        public static BackendComparisonOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Arguments must be supplied as --name value pairs.");
                }
                values[args[index].Substring(2)] = args[index + 1];
            }
            string projectRoot = Path.GetFullPath(values.ContainsKey("project-root") ? values["project-root"] : Directory.GetCurrentDirectory());
            int count = values.ContainsKey("count") ? int.Parse(values["count"], CultureInfo.InvariantCulture) : 10000;
            long seed = values.ContainsKey("seed") ? long.Parse(values["seed"], CultureInfo.InvariantCulture) : 14000015L;
            if (count < 1 || count > 1000000) throw new ArgumentOutOfRangeException("count");
            string workspace = Path.GetFullPath(values.ContainsKey("workspace") ? values["workspace"] : Path.Combine(projectRoot, "tmp", "m15-p1", "work"));
            string output = Path.GetFullPath(values.ContainsKey("output") ? values["output"] : Path.Combine(projectRoot, "tmp", "m15-p1", "result.json"));
            string allowed = Path.GetFullPath(Path.Combine(projectRoot, "tmp", "m15-p1")) + Path.DirectorySeparatorChar;
            if (!workspace.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("P1 workspace must remain under the project tmp/m15-p1 directory.");
            }
            return new BackendComparisonOptions { ProjectRoot = projectRoot, Count = count, Seed = seed, WorkspaceRoot = workspace, OutputPath = output };
        }
    }

    internal sealed class BackendComparisonEvidence
    {
        public string Backend { get; set; }
        public string Dependency { get; set; }
        public string License { get; set; }
        public string PlatformNote { get; set; }
        public string MaintenanceComplexity { get; set; }
        public BackendRunEvidence FirstRun { get; set; }
        public BackendRunEvidence SecondRun { get; set; }
        public bool DeterministicRepeat { get; set; }
    }

    internal sealed class BackendRunEvidence
    {
        public int PersonCount { get; set; }
        public int HouseholdCount { get; set; }
        public int PartitionCount { get; set; }
        public int EventCount { get; set; }
        public long InitialWriteMilliseconds { get; set; }
        public long InitialStorageBytes { get; set; }
        public int PhysicalFileCount { get; set; }
        public LatencyEvidence PersonLookup { get; set; }
        public LatencyEvidence HouseholdLookup { get; set; }
        public LatencyEvidence CombinedQuery { get; set; }
        public int CombinedQueryResultCount { get; set; }
        public long DueReadMilliseconds { get; set; }
        public int DueNodeCount { get; set; }
        public int DueScannedNodeCount { get; set; }
        public long IncrementalSaveMilliseconds { get; set; }
        public int IncrementalSaveBytes { get; set; }
        public long CheckpointMilliseconds { get; set; }
        public int CheckpointBytes { get; set; }
        public long RestoreMilliseconds { get; set; }
        public long AttentionRoundTripMilliseconds { get; set; }
        public string CoreDigest { get; set; }
        public string EventDigest { get; set; }
        public List<string> ContractChecks { get; set; }
    }

    internal sealed class LatencyEvidence
    {
        public int SampleCount { get; set; }
        public double P50Milliseconds { get; set; }
        public double P95Milliseconds { get; set; }
    }
}
