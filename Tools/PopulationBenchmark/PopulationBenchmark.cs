using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Tools.PopulationBenchmark
{
    internal static class Program
    {
        private const string SourceLayer = "pressure_test_v1";
        private const string GeneratorVersion = "m15.p0.generator.v1";

        public static int Main(string[] args)
        {
            try
            {
                BenchmarkOptions options = BenchmarkOptions.Parse(args);
                string dataRoot = Path.Combine(options.ProjectRoot, "Data", "HistoricalPopulation");
                InputAudit audit = M13InputReader.ReadAndAudit(dataRoot);

                RunEvidence first = RunOnce(audit, options.Count, options.Seed);
                RunEvidence second = RunOnce(audit, options.Count, options.Seed);
                bool deterministic = first.CoreDigest == second.CoreDigest &&
                                     first.EventDigest == second.EventDigest &&
                                     first.HouseholdCount == second.HouseholdCount &&
                                     DictionaryEqual(first.LocationCounts, second.LocationCounts) &&
                                     DictionaryEqual(first.OccupationCounts, second.OccupationCounts);
                if (!deterministic)
                {
                    throw new InvalidOperationException("Repeated runs produced different deterministic evidence.");
                }

                JObject report = new JObject
                {
                    ["schema_version"] = "m15.p0.report.v1",
                    ["stage"] = "M15-P0",
                    ["status"] = "passed",
                    ["source_layer"] = SourceLayer,
                    ["generator_version"] = GeneratorVersion,
                    ["parameters"] = new JObject
                    {
                        ["person_count"] = options.Count,
                        ["master_seed"] = options.Seed
                    },
                    ["input_audit"] = JObject.FromObject(audit),
                    ["first_run"] = JObject.FromObject(first),
                    ["second_run"] = JObject.FromObject(second),
                    ["determinism"] = new JObject
                    {
                        ["same_core_digest"] = first.CoreDigest == second.CoreDigest,
                        ["same_event_digest"] = first.EventDigest == second.EventDigest,
                        ["same_household_count"] = first.HouseholdCount == second.HouseholdCount,
                        ["same_location_counts"] = DictionaryEqual(first.LocationCounts, second.LocationCounts),
                        ["same_occupation_counts"] = DictionaryEqual(first.OccupationCounts, second.OccupationCounts),
                        ["passed"] = deterministic
                    },
                    ["scope_statement"] = "This report proves only the M15-P0 10,000-person development smoke. It does not select a formal storage backend or modify V6 saves."
                };

                string outputDirectory = Path.GetDirectoryName(options.OutputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                File.WriteAllText(options.OutputPath, report.ToString(Formatting.Indented), new UTF8Encoding(false));

                Console.WriteLine("M13 audit: population_sources={0} counties={1} stable_regions={2} mappings={3}",
                    audit.PopulationSourceCount, audit.CountyCatalogCount, audit.StableRegionCount, audit.RegionMappingCount);
                Console.WriteLine("Generated: people={0} households={1} partitions={2} events={3}",
                    first.PersonCount, first.HouseholdCount, first.PartitionCount, first.EventCount);
                Console.WriteLine("Core digest: {0}", first.CoreDigest);
                Console.WriteLine("Event digest: {0}", first.EventDigest);
                Console.WriteLine("Result: {0}", options.OutputPath);
                Console.WriteLine("RESULT status=passed stage=M15-P0 people={0} deterministic=true", options.Count);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                Console.Error.WriteLine("RESULT status=failed stage=M15-P0");
                return 1;
            }
        }

        private static RunEvidence RunOnce(InputAudit audit, int count, long seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);
            BenchmarkDataset dataset = DeterministicPopulationGenerator.Generate(audit.PopulationBuckets, count, seed);
            var store = new InMemoryPopulationBenchmarkStore();

            foreach (PopulationPartition partition in dataset.Partitions.OrderBy(value => value.PartitionId, StringComparer.Ordinal))
            {
                store.BatchWrite(partition);
            }

            List<string> invariants = ValidateDataset(store, dataset);
            StoreDigest initialDigest = store.ComputeDigest();

            PermanentPersonRecord probePerson = store.GetPerson(dataset.People[0].PersonId);
            HouseholdRecord probeHousehold = store.GetHousehold(probePerson.HouseholdId);
            if (!probeHousehold.MemberIds.Contains(probePerson.PersonId))
            {
                throw new InvalidOperationException("Person/household read contract failed.");
            }
            invariants.Add("person_and_household_reads_resolve");

            QueryResult query = store.QueryPeople(new PopulationQuery
            {
                LocationId = probePerson.CurrentLocationId,
                Occupation = probePerson.Occupation,
                OrganizationId = probePerson.PrimaryOrganizationId,
                Alive = true,
                Available = probePerson.Available
            });
            if (!query.People.Any(value => value.PersonId == probePerson.PersonId))
            {
                throw new InvalidOperationException("Indexed location/occupation/status query did not return the probe person.");
            }
            invariants.Add("indexed_query_contract_returns_probe");

            byte[] incrementalSave = store.CreateIncrementalSave();
            if (incrementalSave.Length == 0)
            {
                throw new InvalidOperationException("Incremental-save contract produced no initial change batch.");
            }
            invariants.Add("incremental_save_emits_dirty_batch");

            string probePartition = dataset.Partitions[0].PartitionId;
            store.UnloadPartition(probePartition);
            if (store.ComputeDigest().CoreDigest != initialDigest.CoreDigest)
            {
                throw new InvalidOperationException("Unloading a partition changed permanent facts.");
            }
            store.LoadPartition(probePartition);
            if (store.ComputeDigest().CoreDigest != initialDigest.CoreDigest)
            {
                throw new InvalidOperationException("Reloading a partition changed permanent facts.");
            }
            invariants.Add("partition_round_trip_preserves_permanent_facts");

            byte[] checkpoint = store.CreateCheckpoint();
            DueReadResult due = store.ReadDueEvents(30);
            if (due.ScannedNodeCount != due.Events.Count || due.Events.Any(value => value.DueDay > 30))
            {
                throw new InvalidOperationException("Due-event contract scanned or returned a non-due record.");
            }
            store.CommitDueChanges(due.Events);
            if (store.PendingEventCount != dataset.Events.Count - due.Events.Count)
            {
                throw new InvalidOperationException("Due-event commit did not update exactly the due records.");
            }
            DueReadResult afterCommit = store.ReadDueEvents(30);
            if (afterCommit.ScannedNodeCount != 0 || afterCommit.Events.Count != 0)
            {
                throw new InvalidOperationException("Committed due events remained in the active due queue.");
            }
            store.RestoreCheckpoint(checkpoint);
            StoreDigest restoredDigest = store.ComputeDigest();
            if (restoredDigest.CoreDigest != initialDigest.CoreDigest || restoredDigest.EventDigest != initialDigest.EventDigest)
            {
                throw new InvalidOperationException("Checkpoint restore changed deterministic facts.");
            }
            invariants.Add("due_queue_scans_only_due_nodes");
            invariants.Add("committed_due_nodes_leave_active_queue");
            invariants.Add("checkpoint_restore_preserves_digests");

            string[] attentionIds = dataset.People.Take(16).Select(value => value.PersonId).ToArray();
            IReadOnlyList<AttentionPersonView> attention = store.ExpandAttention(attentionIds);
            if (attention.Count != attentionIds.Length)
            {
                throw new InvalidOperationException("Attention expansion did not return every requested person.");
            }
            store.ReleaseAttention(attentionIds);
            StoreDigest releasedDigest = store.ComputeDigest();
            if (releasedDigest.CoreDigest != initialDigest.CoreDigest || releasedDigest.EventDigest != initialDigest.EventDigest)
            {
                throw new InvalidOperationException("Attention release changed permanent facts.");
            }
            invariants.Add("attention_round_trip_preserves_permanent_facts");

            stopwatch.Stop();
            long memoryAfter = GC.GetTotalMemory(true);
            Dictionary<string, int> locationCounts = CountBy(dataset.People, value => value.CurrentLocationId);
            Dictionary<string, int> occupationCounts = CountBy(dataset.People, value => value.Occupation);

            return new RunEvidence
            {
                PersonCount = dataset.People.Count,
                HouseholdCount = dataset.Households.Count,
                PartitionCount = dataset.Partitions.Count,
                EventCount = dataset.Events.Count,
                AliveCount = dataset.People.Count(value => value.Alive),
                HouseholdMemberCount = dataset.Households.Sum(value => value.MemberIds.Count),
                LocationPopulationTotal = locationCounts.Values.Sum(),
                OccupationPopulationTotal = occupationCounts.Values.Sum(),
                DueCutoffDay = 30,
                DueNodeCount = due.Events.Count,
                DueScannedNodeCount = due.ScannedNodeCount,
                QueryCandidateCount = query.CandidateCount,
                QueryResultCount = query.People.Count,
                IncrementalSaveBytes = incrementalSave.Length,
                CheckpointBytes = checkpoint.Length,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                ManagedMemoryDeltaBytes = Math.Max(0L, memoryAfter - memoryBefore),
                CoreDigest = initialDigest.CoreDigest,
                EventDigest = initialDigest.EventDigest,
                LocationCounts = locationCounts,
                OccupationCounts = occupationCounts,
                Invariants = invariants.OrderBy(value => value, StringComparer.Ordinal).ToList()
            };
        }

        private static List<string> ValidateDataset(InMemoryPopulationBenchmarkStore store, BenchmarkDataset dataset)
        {
            var invariants = new List<string>();
            if (store.PersonCount != dataset.People.Count || dataset.People.Select(value => value.PersonId).Distinct(StringComparer.Ordinal).Count() != dataset.People.Count)
            {
                throw new InvalidOperationException("Permanent person IDs are not complete and unique.");
            }
            invariants.Add("permanent_person_ids_unique");

            var peopleById = dataset.People.ToDictionary(value => value.PersonId, StringComparer.Ordinal);
            var householdsById = dataset.Households.ToDictionary(value => value.HouseholdId, StringComparer.Ordinal);
            var locations = new HashSet<string>(dataset.People.Select(value => value.CurrentLocationId), StringComparer.Ordinal);
            foreach (PermanentPersonRecord person in dataset.People)
            {
                HouseholdRecord household;
                if (!householdsById.TryGetValue(person.HouseholdId, out household) || !household.MemberIds.Contains(person.PersonId))
                {
                    throw new InvalidOperationException("Person has an invalid household reference: " + person.PersonId);
                }
                if (string.IsNullOrWhiteSpace(person.BirthLocationId) || string.IsNullOrWhiteSpace(person.CurrentLocationId) || !locations.Contains(person.CurrentLocationId))
                {
                    throw new InvalidOperationException("Person has an invalid stable location: " + person.PersonId);
                }
                ValidateParent(person, person.FatherId, peopleById, household);
                ValidateParent(person, person.MotherId, peopleById, household);
            }
            invariants.Add("household_and_location_references_valid");
            invariants.Add("parent_references_valid_and_older");

            int householdMembers = dataset.Households.Sum(value => value.MemberIds.Count);
            if (householdMembers != dataset.People.Count || dataset.Events.Count != dataset.People.Count)
            {
                throw new InvalidOperationException("Population conservation failed for household membership or due events.");
            }
            invariants.Add("household_membership_conserves_population");
            invariants.Add("one_due_event_per_person");

            if (dataset.People.Any(value => value.SourceLayer != SourceLayer || string.IsNullOrWhiteSpace(value.SourcePopulationAdminUnitId)) ||
                dataset.Events.Any(value => value.SourceLayer != SourceLayer))
            {
                throw new InvalidOperationException("Pressure records are missing their source-layer marker.");
            }
            invariants.Add("pressure_data_separated_from_historical_facts");

            string[] requiredReasons = { "birth", "death", "marriage", "migration", "disease", "service", "occupation_change" };
            var actualReasons = new HashSet<string>(dataset.Events.Select(value => value.Reason), StringComparer.Ordinal);
            if (requiredReasons.Any(value => !actualReasons.Contains(value)))
            {
                throw new InvalidOperationException("Pressure due events do not cover every required life-event reason.");
            }
            invariants.Add("due_events_cover_seven_required_life_changes");
            return invariants;
        }

        private static void ValidateParent(PermanentPersonRecord child, string parentId, IDictionary<string, PermanentPersonRecord> peopleById, HouseholdRecord household)
        {
            if (string.IsNullOrEmpty(parentId))
            {
                return;
            }
            PermanentPersonRecord parent;
            if (!peopleById.TryGetValue(parentId, out parent) || !household.MemberIds.Contains(parentId) || parent.BirthDay >= child.BirthDay)
            {
                throw new InvalidOperationException("Invalid parent reference for " + child.PersonId);
            }
        }

        private static Dictionary<string, int> CountBy(IEnumerable<PermanentPersonRecord> people, Func<PermanentPersonRecord, string> selector)
        {
            return people.GroupBy(selector, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        }

        private static bool DictionaryEqual(IDictionary<string, int> left, IDictionary<string, int> right)
        {
            return left.Count == right.Count && left.All(pair => right.ContainsKey(pair.Key) && right[pair.Key] == pair.Value);
        }
    }

    internal sealed class BenchmarkOptions
    {
        public string ProjectRoot { get; private set; }
        public string OutputPath { get; private set; }
        public int Count { get; private set; }
        public long Seed { get; private set; }

        public static BenchmarkOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Arguments must be provided as --name value pairs.");
                }
                values[args[index].Substring(2)] = args[index + 1];
            }

            string root;
            if (!values.TryGetValue("project-root", out root))
            {
                root = Directory.GetCurrentDirectory();
            }
            root = Path.GetFullPath(root);

            int count = values.ContainsKey("count") ? int.Parse(values["count"], CultureInfo.InvariantCulture) : 10000;
            if (count < 1 || count > 1000000)
            {
                throw new ArgumentOutOfRangeException("count", "Count must be between 1 and 1,000,000.");
            }
            long seed = values.ContainsKey("seed") ? long.Parse(values["seed"], CultureInfo.InvariantCulture) : 14000015L;
            string output = values.ContainsKey("output") ? values["output"] : Path.Combine(root, "tmp", "m15-p0", "result.json");

            return new BenchmarkOptions
            {
                ProjectRoot = root,
                OutputPath = Path.GetFullPath(output),
                Count = count,
                Seed = seed
            };
        }
    }

    internal sealed class InputAudit
    {
        public string ValidationStatus { get; set; }
        public int DatasetYear { get; set; }
        public int PopulationSourceCount { get; set; }
        public int CountyCatalogCount { get; set; }
        public int AdministrativeUnitCount { get; set; }
        public int StableRegionCount { get; set; }
        public int RegionMappingCount { get; set; }
        public int MissingStableRegionReferenceCount { get; set; }
        public long EffectivePopulationTotal { get; set; }
        [JsonIgnore]
        public List<PopulationBucket> PopulationBuckets { get; set; }
    }

    internal sealed class PopulationBucket
    {
        public string AdminUnitId { get; set; }
        public string StableRegionId { get; set; }
        public long EffectivePopulation { get; set; }
    }

    internal static class M13InputReader
    {
        private static readonly string[] RequiredFiles =
        {
            "han_140_m12_population_input.json",
            "han_140_population_records.csv",
            "han_140_administrative_units.csv",
            "stable_population_regions.csv",
            "han_140_region_mapping.csv",
            "han_140_audit_report.json"
        };

        public static InputAudit ReadAndAudit(string dataRoot)
        {
            foreach (string name in RequiredFiles)
            {
                if (!File.Exists(Path.Combine(dataRoot, name)))
                {
                    throw new FileNotFoundException("Required M13 input is missing.", name);
                }
            }

            JObject auditJson = JObject.Parse(File.ReadAllText(Path.Combine(dataRoot, "han_140_audit_report.json"), Encoding.UTF8));
            JObject m12Json = JObject.Parse(File.ReadAllText(Path.Combine(dataRoot, "han_140_m12_population_input.json"), Encoding.UTF8));
            List<Dictionary<string, string>> populationRows = CsvContract.Read(Path.Combine(dataRoot, "han_140_population_records.csv"));
            List<Dictionary<string, string>> adminRows = CsvContract.Read(Path.Combine(dataRoot, "han_140_administrative_units.csv"));
            List<Dictionary<string, string>> stableRows = CsvContract.Read(Path.Combine(dataRoot, "stable_population_regions.csv"));
            List<Dictionary<string, string>> mappingRows = CsvContract.Read(Path.Combine(dataRoot, "han_140_region_mapping.csv"));

            int populationSources = (int)m12Json["population_source_count"];
            int countyCatalog = (int)m12Json["county_catalog_count"];
            int countyRows = adminRows.Count(value => value["unit_type"] == "county");
            string validationStatus = (string)auditJson["validation_status"];
            if (validationStatus != "passed" || populationSources != 105 || populationRows.Count != 105 || countyCatalog != 1182 || countyRows != 1182)
            {
                throw new InvalidDataException("M13 population or county audit no longer matches the accepted 105/1182 contract.");
            }

            var stableIds = new HashSet<string>(stableRows.Select(value => value["stable_region_id"]), StringComparer.Ordinal);
            int missingMappingTargets = mappingRows.Count(value => !stableIds.Contains(value["target_id"]));
            var buckets = new List<PopulationBucket>();
            foreach (JToken unit in (JArray)m12Json["population_units"])
            {
                JArray mappings = (JArray)unit["mappings"];
                if (mappings == null || mappings.Count == 0)
                {
                    throw new InvalidDataException("M12 population unit has no stable-region mapping: " + (string)unit["admin_unit_id"]);
                }
                long effectivePopulation = (long)unit["effective_population"];
                foreach (JToken mapping in mappings)
                {
                    string stableRegionId = (string)mapping["stable_region_id"];
                    if (!stableIds.Contains(stableRegionId))
                    {
                        missingMappingTargets++;
                        continue;
                    }
                    int basisPoints = (int)mapping["weight_basis_points"];
                    long weighted = Math.Max(1L, effectivePopulation * basisPoints / 10000L);
                    buckets.Add(new PopulationBucket
                    {
                        AdminUnitId = (string)unit["admin_unit_id"],
                        StableRegionId = stableRegionId,
                        EffectivePopulation = weighted
                    });
                }
            }
            if (missingMappingTargets != 0 || buckets.Count == 0)
            {
                throw new InvalidDataException("M13 stable geography contains missing references.");
            }

            return new InputAudit
            {
                ValidationStatus = validationStatus,
                DatasetYear = (int)m12Json["dataset_year"],
                PopulationSourceCount = populationSources,
                CountyCatalogCount = countyCatalog,
                AdministrativeUnitCount = adminRows.Count,
                StableRegionCount = stableRows.Count,
                RegionMappingCount = mappingRows.Count,
                MissingStableRegionReferenceCount = missingMappingTargets,
                EffectivePopulationTotal = buckets.Sum(value => value.EffectivePopulation),
                PopulationBuckets = buckets.OrderBy(value => value.AdminUnitId, StringComparer.Ordinal).ThenBy(value => value.StableRegionId, StringComparer.Ordinal).ToList()
            };
        }
    }

    internal static class CsvContract
    {
        public static List<Dictionary<string, string>> Read(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2)
            {
                throw new InvalidDataException("CSV has no data rows: " + path);
            }
            List<string> headers = ParseLine(lines[0]);
            var rows = new List<Dictionary<string, string>>(lines.Length - 1);
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }
                List<string> values = ParseLine(lines[lineIndex]);
                if (values.Count != headers.Count)
                {
                    throw new InvalidDataException("CSV column count mismatch at " + path + ":" + (lineIndex + 1));
                }
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int column = 0; column < headers.Count; column++)
                {
                    row[headers[column]] = values[column];
                }
                rows.Add(row);
            }
            return rows;
        }

        private static List<string> ParseLine(string line)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            bool quoted = false;
            for (int index = 0; index < line.Length; index++)
            {
                char character = line[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        value.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    values.Add(value.ToString());
                    value.Clear();
                }
                else
                {
                    value.Append(character);
                }
            }
            if (quoted)
            {
                throw new InvalidDataException("CSV contains an unterminated quoted field.");
            }
            values.Add(value.ToString());
            return values;
        }
    }

    internal sealed class PermanentPersonRecord
    {
        public string PersonId { get; set; }
        public int NameIndex { get; set; }
        public string Gender { get; set; }
        public int BirthDay { get; set; }
        public bool Alive { get; set; }
        public int? DeathDay { get; set; }
        public string BirthLocationId { get; set; }
        public string CurrentLocationId { get; set; }
        public string HouseholdId { get; set; }
        public string FatherId { get; set; }
        public string MotherId { get; set; }
        public string Occupation { get; set; }
        public string LaborSummary { get; set; }
        public string HealthSummary { get; set; }
        public string PrimaryOrganizationId { get; set; }
        public int NextDueDay { get; set; }
        public string NextDueReason { get; set; }
        public int RecordVersion { get; set; }
        public bool Available { get; set; }
        public string SourceLayer { get; set; }
        public string SourcePopulationAdminUnitId { get; set; }
    }

    internal sealed class HouseholdRecord
    {
        public string HouseholdId { get; set; }
        public string LocationId { get; set; }
        public List<string> MemberIds { get; set; }
        public int RecordVersion { get; set; }
        public string SourceLayer { get; set; }
    }

    internal sealed class DueEventRecord
    {
        public string EventId { get; set; }
        public string PersonId { get; set; }
        public int DueDay { get; set; }
        public string Reason { get; set; }
        public string RuleVersion { get; set; }
        public string ActionCoordinate { get; set; }
        public string SourceLayer { get; set; }
    }

    internal sealed class PopulationPartition
    {
        public string PartitionId { get; set; }
        public string StableRegionId { get; set; }
        public int Version { get; set; }
        public List<PermanentPersonRecord> People { get; set; }
        public List<HouseholdRecord> Households { get; set; }
        public List<DueEventRecord> Events { get; set; }
    }

    internal sealed class BenchmarkDataset
    {
        public List<PermanentPersonRecord> People { get; set; }
        public List<HouseholdRecord> Households { get; set; }
        public List<DueEventRecord> Events { get; set; }
        public List<PopulationPartition> Partitions { get; set; }
    }

    internal static class DeterministicPopulationGenerator
    {
        private static readonly string[] AdultOccupations = { "farmer", "artisan", "merchant", "soldier", "clerk", "healer", "laborer" };
        private static readonly string[] DueReasons = { "birth", "death", "marriage", "migration", "disease", "service", "occupation_change" };

        public static BenchmarkDataset Generate(IReadOnlyList<PopulationBucket> buckets, int personCount, long seed)
        {
            var people = new List<PermanentPersonRecord>(personCount);
            var households = new List<HouseholdRecord>();
            var events = new List<DueEventRecord>(personCount);
            var partitionsByLocation = new Dictionary<string, PopulationPartition>(StringComparer.Ordinal);
            int personOrdinal = 0;
            int householdOrdinal = 0;

            while (personOrdinal < personCount)
            {
                int requestedSize = 2 + (int)(StableHash.UInt64(seed, "household_size", householdOrdinal, 0) % 5UL);
                int householdSize = Math.Min(requestedSize, personCount - personOrdinal);
                PopulationBucket bucket = PickBucket(buckets, seed, householdOrdinal);
                string householdId = StableHash.Id("household.test", seed, householdOrdinal.ToString(CultureInfo.InvariantCulture));
                var memberIds = new List<string>(householdSize);
                PopulationPartition partition;
                if (!partitionsByLocation.TryGetValue(bucket.StableRegionId, out partition))
                {
                    partition = new PopulationPartition
                    {
                        PartitionId = StableHash.Id("partition.test", seed, bucket.StableRegionId),
                        StableRegionId = bucket.StableRegionId,
                        Version = 1,
                        People = new List<PermanentPersonRecord>(),
                        Households = new List<HouseholdRecord>(),
                        Events = new List<DueEventRecord>()
                    };
                    partitionsByLocation.Add(bucket.StableRegionId, partition);
                }
                string fatherId = null;
                string motherId = null;

                for (int member = 0; member < householdSize; member++)
                {
                    int ordinal = personOrdinal++;
                    string personId = StableHash.Id("person.test", seed, ordinal.ToString(CultureInfo.InvariantCulture));
                    string gender = member == 0 ? "male" : member == 1 ? "female" : (StableHash.UInt64(seed, "gender", ordinal, 0) % 2UL == 0UL ? "male" : "female");
                    int age = member < 2
                        ? 28 + (int)(StableHash.UInt64(seed, "adult_age", ordinal, 0) % 29UL)
                        : (int)(StableHash.UInt64(seed, "child_age", ordinal, 0) % 25UL);
                    int birthDay = -(age * 365 + (int)(StableHash.UInt64(seed, "birth_offset", ordinal, 0) % 365UL));
                    string occupation = age < 15 ? "dependent" : AdultOccupations[(int)(StableHash.UInt64(seed, "occupation", ordinal, 0) % (ulong)AdultOccupations.Length)];
                    string health = StableHash.UInt64(seed, "health", ordinal, 0) % 10UL == 0UL ? "limited" : "stable";
                    bool available = age >= 15 && age <= 60 && health == "stable" && occupation != "soldier";
                    int dueDay = 1 + (int)(StableHash.UInt64(seed, "due_day", ordinal, 0) % 365UL);
                    string dueReason = DueReasons[(int)(StableHash.UInt64(seed, "due_reason", ordinal, 0) % (ulong)DueReasons.Length)];

                    var person = new PermanentPersonRecord
                    {
                        PersonId = personId,
                        NameIndex = (int)(StableHash.UInt64(seed, "name", ordinal, 0) % 1000000UL),
                        Gender = gender,
                        BirthDay = birthDay,
                        Alive = true,
                        DeathDay = null,
                        BirthLocationId = bucket.StableRegionId,
                        CurrentLocationId = bucket.StableRegionId,
                        HouseholdId = householdId,
                        FatherId = member >= 2 ? fatherId : null,
                        MotherId = member >= 2 ? motherId : null,
                        Occupation = occupation,
                        LaborSummary = available ? "available" : "not_available",
                        HealthSummary = health,
                        PrimaryOrganizationId = StableHash.Id("org.test", seed, bucket.StableRegionId),
                        NextDueDay = dueDay,
                        NextDueReason = dueReason,
                        RecordVersion = 1,
                        Available = available,
                        SourceLayer = "pressure_test_v1",
                        SourcePopulationAdminUnitId = bucket.AdminUnitId
                    };
                    people.Add(person);
                    partition.People.Add(person);
                    memberIds.Add(personId);
                    if (member == 0)
                    {
                        fatherId = personId;
                    }
                    else if (member == 1)
                    {
                        motherId = personId;
                    }

                    var dueEvent = new DueEventRecord
                    {
                        EventId = StableHash.Id("event.test", seed, ordinal.ToString(CultureInfo.InvariantCulture)),
                        PersonId = personId,
                        DueDay = dueDay,
                        Reason = dueReason,
                        RuleVersion = "m15.p0.due.v1",
                        ActionCoordinate = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}", seed, personId, dueDay, dueReason),
                        SourceLayer = "pressure_test_v1"
                    };
                    events.Add(dueEvent);
                    partition.Events.Add(dueEvent);
                }

                var household = new HouseholdRecord
                {
                    HouseholdId = householdId,
                    LocationId = bucket.StableRegionId,
                    MemberIds = memberIds,
                    RecordVersion = 1,
                    SourceLayer = "pressure_test_v1"
                };
                households.Add(household);
                partition.Households.Add(household);
                householdOrdinal++;
            }

            List<PopulationPartition> partitions = partitionsByLocation.Values.OrderBy(value => value.StableRegionId, StringComparer.Ordinal).ToList();
            foreach (PopulationPartition partition in partitions)
            {
                partition.People.Sort((left, right) => StringComparer.Ordinal.Compare(left.PersonId, right.PersonId));
                partition.Households.Sort((left, right) => StringComparer.Ordinal.Compare(left.HouseholdId, right.HouseholdId));
                partition.Events.Sort((left, right) => StringComparer.Ordinal.Compare(left.EventId, right.EventId));
            }

            return new BenchmarkDataset
            {
                People = people,
                Households = households,
                Events = events,
                Partitions = partitions
            };
        }

        private static PopulationBucket PickBucket(IReadOnlyList<PopulationBucket> buckets, long seed, int householdOrdinal)
        {
            long total = buckets.Sum(value => value.EffectivePopulation);
            ulong pick = StableHash.UInt64(seed, "population_bucket", householdOrdinal, 0) % (ulong)total;
            ulong cumulative = 0UL;
            foreach (PopulationBucket bucket in buckets)
            {
                cumulative += (ulong)bucket.EffectivePopulation;
                if (pick < cumulative)
                {
                    return bucket;
                }
            }
            return buckets[buckets.Count - 1];
        }
    }

    internal static class StableHash
    {
        public static ulong UInt64(long seed, string purpose, int entityOrdinal, int drawIndex)
        {
            return UInt64(string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|m15.p0.hash.v1", seed, purpose, entityOrdinal, drawIndex));
        }

        public static ulong UInt64(long seed, string purpose, string semanticKey, int drawIndex)
        {
            return UInt64(string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|m15.p0.hash.v1", seed, purpose, semanticKey, drawIndex));
        }

        public static string Id(string prefix, long seed, string semanticKey)
        {
            byte[] hash = HashBytes(string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|m15.p0.id.v1", prefix, seed, semanticKey));
            var builder = new StringBuilder(prefix.Length + 25);
            builder.Append(prefix).Append('.');
            for (int index = 0; index < 12; index++)
            {
                builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        public static string Sha256(string value)
        {
            byte[] hash = HashBytes(value);
            return Hex(hash);
        }

        public static string Sha256(IEnumerable<string> segments)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                foreach (string segment in segments)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(segment);
                    algorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
                algorithm.TransformFinalBlock(new byte[0], 0, 0);
                return Hex(algorithm.Hash);
            }
        }

        private static string Hex(byte[] hash)
        {
            var builder = new StringBuilder(64);
            foreach (byte item in hash)
            {
                builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static ulong UInt64(string value)
        {
            byte[] hash = HashBytes(value);
            return ((ulong)hash[0]) | ((ulong)hash[1] << 8) | ((ulong)hash[2] << 16) | ((ulong)hash[3] << 24) |
                   ((ulong)hash[4] << 32) | ((ulong)hash[5] << 40) | ((ulong)hash[6] << 48) | ((ulong)hash[7] << 56);
        }

        private static byte[] HashBytes(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
        }
    }

    internal sealed class PopulationQuery
    {
        public string LocationId { get; set; }
        public string Occupation { get; set; }
        public string OrganizationId { get; set; }
        public string HealthSummary { get; set; }
        public string LaborSummary { get; set; }
        public bool? Alive { get; set; }
        public bool? Available { get; set; }
    }

    internal sealed class PersonPartitionWrite
    {
        public PermanentPersonRecord Person { get; set; }
        public string PartitionId { get; set; }
    }

    internal sealed class PopulationChangeBatch
    {
        public int AbsoluteDay { get; set; }
        public string RuleVersion { get; set; }
        public List<PersonPartitionWrite> People { get; set; } = new List<PersonPartitionWrite>();
        public List<HouseholdRecord> Households { get; set; } = new List<HouseholdRecord>();
        public List<DueEventRecord> AddedEvents { get; set; } = new List<DueEventRecord>();
    }

    internal sealed class QueryResult
    {
        public int CandidateCount { get; set; }
        public List<PermanentPersonRecord> People { get; set; }
    }

    internal sealed class DueReadResult
    {
        public int ScannedNodeCount { get; set; }
        public List<DueEventRecord> Events { get; set; }
    }

    internal sealed class AttentionPersonView
    {
        public string PersonId { get; set; }
        public string HouseholdId { get; set; }
        public string LocationId { get; set; }
        public string Occupation { get; set; }
        public string HealthSummary { get; set; }
    }

    internal sealed class StoreDigest
    {
        public string CoreDigest { get; set; }
        public string EventDigest { get; set; }
    }

    internal interface IPopulationBenchmarkStore
    {
        void BatchWrite(PopulationPartition partition);
        PermanentPersonRecord GetPerson(string personId);
        HouseholdRecord GetHousehold(string householdId);
        QueryResult QueryPeople(PopulationQuery query);
        QueryResult QueryChildren(string parentId);
        void LoadPartition(string partitionId);
        void UnloadPartition(string partitionId);
        DueReadResult ReadDueEvents(int absoluteDay);
        void ApplyChangeBatch(PopulationChangeBatch batch);
        void CommitDueChanges(IEnumerable<DueEventRecord> events);
        byte[] CreateIncrementalSave();
        byte[] CreateCheckpoint();
        void RestoreCheckpoint(byte[] checkpoint);
        StoreDigest ComputeDigest();
        IReadOnlyList<AttentionPersonView> ExpandAttention(IEnumerable<string> personIds);
        void ReleaseAttention(IEnumerable<string> personIds);
    }

    internal sealed class InMemoryPopulationBenchmarkStore : IPopulationBenchmarkStore
    {
        private Dictionary<string, PermanentPersonRecord> _people = NewPersonDictionary();
        private Dictionary<string, HouseholdRecord> _households = NewHouseholdDictionary();
        private Dictionary<string, DueEventRecord> _events = NewEventDictionary();
        private Dictionary<string, string> _personPartitions = new Dictionary<string, string>(StringComparer.Ordinal);
        private HashSet<string> _knownPartitions = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> _loadedPartitions = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> _completedEvents = new HashSet<string>(StringComparer.Ordinal);
        private Dictionary<string, AttentionPersonView> _attention = new Dictionary<string, AttentionPersonView>(StringComparer.Ordinal);
        private Dictionary<string, HashSet<string>> _locationIndex = NewIndex();
        private Dictionary<string, HashSet<string>> _occupationIndex = NewIndex();
        private Dictionary<string, HashSet<string>> _organizationIndex = NewIndex();
        private Dictionary<string, HashSet<string>> _fatherIndex = NewIndex();
        private Dictionary<string, HashSet<string>> _motherIndex = NewIndex();
        private SortedDictionary<int, List<string>> _dueIndex = new SortedDictionary<int, List<string>>();
        private HashSet<string> _dirtyPeople = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> _dirtyHouseholds = new HashSet<string>(StringComparer.Ordinal);
        private HashSet<string> _dirtyEvents = new HashSet<string>(StringComparer.Ordinal);

        public int PersonCount { get { return _people.Count; } }
        public int PendingEventCount { get { return _events.Count - _completedEvents.Count; } }

        public void BatchWrite(PopulationPartition partition)
        {
            foreach (PermanentPersonRecord person in partition.People)
            {
                if (_people.ContainsKey(person.PersonId))
                {
                    throw new InvalidDataException("Duplicate permanent person ID: " + person.PersonId);
                }
                _people.Add(person.PersonId, person);
                _personPartitions.Add(person.PersonId, partition.PartitionId);
                AddIndex(_locationIndex, person.CurrentLocationId, person.PersonId);
                AddIndex(_occupationIndex, person.Occupation, person.PersonId);
                AddIndex(_organizationIndex, person.PrimaryOrganizationId, person.PersonId);
                AddOptionalIndex(_fatherIndex, person.FatherId, person.PersonId);
                AddOptionalIndex(_motherIndex, person.MotherId, person.PersonId);
                _dirtyPeople.Add(person.PersonId);
            }
            _knownPartitions.Add(partition.PartitionId);
            foreach (HouseholdRecord household in partition.Households)
            {
                _households.Add(household.HouseholdId, household);
                _dirtyHouseholds.Add(household.HouseholdId);
            }
            foreach (DueEventRecord dueEvent in partition.Events)
            {
                _events.Add(dueEvent.EventId, dueEvent);
                AddDueIndex(dueEvent);
                _dirtyEvents.Add(dueEvent.EventId);
            }
            _loadedPartitions.Add(partition.PartitionId);
        }

        public PermanentPersonRecord GetPerson(string personId)
        {
            PermanentPersonRecord person;
            if (!_people.TryGetValue(personId, out person))
            {
                throw new KeyNotFoundException("Unknown person: " + personId);
            }
            return person;
        }

        public HouseholdRecord GetHousehold(string householdId)
        {
            HouseholdRecord household;
            if (!_households.TryGetValue(householdId, out household))
            {
                throw new KeyNotFoundException("Unknown household: " + householdId);
            }
            return household;
        }

        public QueryResult QueryPeople(PopulationQuery query)
        {
            IEnumerable<string> candidates;
            HashSet<string> indexed;
            if (!string.IsNullOrEmpty(query.LocationId) && _locationIndex.TryGetValue(query.LocationId, out indexed))
            {
                candidates = indexed;
            }
            else if (!string.IsNullOrEmpty(query.Occupation) && _occupationIndex.TryGetValue(query.Occupation, out indexed))
            {
                candidates = indexed;
            }
            else if (!string.IsNullOrEmpty(query.OrganizationId) && _organizationIndex.TryGetValue(query.OrganizationId, out indexed))
            {
                candidates = indexed;
            }
            else
            {
                candidates = _people.Keys;
            }

            List<string> candidateIds = candidates.OrderBy(value => value, StringComparer.Ordinal).ToList();
            List<PermanentPersonRecord> result = candidateIds.Select(value => _people[value])
                .Where(value => string.IsNullOrEmpty(query.LocationId) || value.CurrentLocationId == query.LocationId)
                .Where(value => string.IsNullOrEmpty(query.Occupation) || value.Occupation == query.Occupation)
                .Where(value => string.IsNullOrEmpty(query.OrganizationId) || value.PrimaryOrganizationId == query.OrganizationId)
                .Where(value => string.IsNullOrEmpty(query.HealthSummary) || value.HealthSummary == query.HealthSummary)
                .Where(value => string.IsNullOrEmpty(query.LaborSummary) || value.LaborSummary == query.LaborSummary)
                .Where(value => !query.Alive.HasValue || value.Alive == query.Alive.Value)
                .Where(value => !query.Available.HasValue || value.Available == query.Available.Value)
                .ToList();
            return new QueryResult { CandidateCount = candidateIds.Count, People = result };
        }

        public QueryResult QueryChildren(string parentId)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> indexed;
            if (_fatherIndex.TryGetValue(parentId, out indexed)) ids.UnionWith(indexed);
            if (_motherIndex.TryGetValue(parentId, out indexed)) ids.UnionWith(indexed);
            List<PermanentPersonRecord> people = ids.OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => _people[value]).ToList();
            return new QueryResult { CandidateCount = people.Count, People = people };
        }

        public void LoadPartition(string partitionId)
        {
            if (!_knownPartitions.Contains(partitionId))
            {
                throw new KeyNotFoundException("Unknown partition: " + partitionId);
            }
            _loadedPartitions.Add(partitionId);
        }

        public void UnloadPartition(string partitionId)
        {
            _loadedPartitions.Remove(partitionId);
        }

        public DueReadResult ReadDueEvents(int absoluteDay)
        {
            var due = new List<DueEventRecord>();
            int scanned = 0;
            foreach (KeyValuePair<int, List<string>> day in _dueIndex)
            {
                if (day.Key > absoluteDay)
                {
                    break;
                }
                foreach (string eventId in day.Value)
                {
                    scanned++;
                    if (!_completedEvents.Contains(eventId))
                    {
                        due.Add(_events[eventId]);
                    }
                }
            }
            return new DueReadResult { ScannedNodeCount = scanned, Events = due };
        }

        public void ApplyChangeBatch(PopulationChangeBatch batch)
        {
            if (batch == null) throw new ArgumentNullException("batch");
            foreach (PersonPartitionWrite write in batch.People.OrderBy(value => value.Person.PersonId, StringComparer.Ordinal))
            {
                PermanentPersonRecord previous;
                if (_people.TryGetValue(write.Person.PersonId, out previous))
                {
                    RemoveIndex(_locationIndex, previous.CurrentLocationId, previous.PersonId);
                    RemoveIndex(_occupationIndex, previous.Occupation, previous.PersonId);
                    RemoveIndex(_organizationIndex, previous.PrimaryOrganizationId, previous.PersonId);
                    RemoveOptionalIndex(_fatherIndex, previous.FatherId, previous.PersonId);
                    RemoveOptionalIndex(_motherIndex, previous.MotherId, previous.PersonId);
                }
                _people[write.Person.PersonId] = write.Person;
                _personPartitions[write.Person.PersonId] = write.PartitionId;
                _knownPartitions.Add(write.PartitionId);
                AddIndex(_locationIndex, write.Person.CurrentLocationId, write.Person.PersonId);
                AddIndex(_occupationIndex, write.Person.Occupation, write.Person.PersonId);
                AddIndex(_organizationIndex, write.Person.PrimaryOrganizationId, write.Person.PersonId);
                AddOptionalIndex(_fatherIndex, write.Person.FatherId, write.Person.PersonId);
                AddOptionalIndex(_motherIndex, write.Person.MotherId, write.Person.PersonId);
                _dirtyPeople.Add(write.Person.PersonId);
            }
            foreach (HouseholdRecord household in batch.Households.OrderBy(value => value.HouseholdId, StringComparer.Ordinal))
            {
                _households[household.HouseholdId] = household;
                _dirtyHouseholds.Add(household.HouseholdId);
            }
            foreach (DueEventRecord dueEvent in batch.AddedEvents.OrderBy(value => value.EventId, StringComparer.Ordinal))
            {
                if (_events.ContainsKey(dueEvent.EventId)) throw new InvalidDataException("Duplicate added event ID: " + dueEvent.EventId);
                _events.Add(dueEvent.EventId, dueEvent);
                AddDueIndex(dueEvent);
                _dirtyEvents.Add(dueEvent.EventId);
            }
        }

        public void CommitDueChanges(IEnumerable<DueEventRecord> events)
        {
            List<DueEventRecord> batch = events.ToList();
            foreach (DueEventRecord dueEvent in batch)
            {
                if (!_events.ContainsKey(dueEvent.EventId))
                {
                    throw new KeyNotFoundException("Unknown due event: " + dueEvent.EventId);
                }
                _completedEvents.Add(dueEvent.EventId);
                _dirtyEvents.Add(dueEvent.EventId);
            }
            foreach (IGrouping<int, DueEventRecord> group in batch.GroupBy(value => value.DueDay))
            {
                List<string> dayEvents;
                if (_dueIndex.TryGetValue(group.Key, out dayEvents))
                {
                    var committed = new HashSet<string>(group.Select(value => value.EventId), StringComparer.Ordinal);
                    dayEvents.RemoveAll(committed.Contains);
                    if (dayEvents.Count == 0)
                    {
                        _dueIndex.Remove(group.Key);
                    }
                }
            }
        }

        public byte[] CreateIncrementalSave()
        {
            var batch = new IncrementalStoreBatch
            {
                People = _dirtyPeople.OrderBy(value => value, StringComparer.Ordinal).Select(value => _people[value]).ToList(),
                Households = _dirtyHouseholds.OrderBy(value => value, StringComparer.Ordinal).Select(value => _households[value]).ToList(),
                Events = _dirtyEvents.OrderBy(value => value, StringComparer.Ordinal).Select(value => _events[value]).ToList(),
                CompletedEventIds = _completedEvents.Where(value => _dirtyEvents.Contains(value)).OrderBy(value => value, StringComparer.Ordinal).ToList()
            };
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(batch, Formatting.None));
            _dirtyPeople.Clear();
            _dirtyHouseholds.Clear();
            _dirtyEvents.Clear();
            return bytes;
        }

        public long WriteIncrementalSave(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var text = new StreamWriter(stream, new UTF8Encoding(false), 65536))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.None })
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault();
                writer.WriteStartObject();
                WriteArray(writer, serializer, "People", _dirtyPeople.OrderBy(value => value, StringComparer.Ordinal).Select(value => _people[value]));
                WriteArray(writer, serializer, "Households", _dirtyHouseholds.OrderBy(value => value, StringComparer.Ordinal).Select(value => _households[value]));
                WriteArray(writer, serializer, "Events", _dirtyEvents.OrderBy(value => value, StringComparer.Ordinal).Select(value => _events[value]));
                WriteArray(writer, serializer, "CompletedEventIds", _completedEvents.Where(value => _dirtyEvents.Contains(value)).OrderBy(value => value, StringComparer.Ordinal));
                writer.WriteEndObject();
            }
            _dirtyPeople.Clear();
            _dirtyHouseholds.Clear();
            _dirtyEvents.Clear();
            return new FileInfo(path).Length;
        }

        public byte[] CreateCheckpoint()
        {
            var checkpoint = new StoreCheckpoint
            {
                People = _people.Values.OrderBy(value => value.PersonId, StringComparer.Ordinal).ToList(),
                Households = _households.Values.OrderBy(value => value.HouseholdId, StringComparer.Ordinal).ToList(),
                Events = _events.Values.OrderBy(value => value.EventId, StringComparer.Ordinal).ToList(),
                PersonPartitions = _personPartitions.OrderBy(value => value.Key, StringComparer.Ordinal).ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal),
                LoadedPartitions = _loadedPartitions.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                CompletedEventIds = _completedEvents.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                DirtyPersonIds = _dirtyPeople.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                DirtyHouseholdIds = _dirtyHouseholds.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                DirtyEventIds = _dirtyEvents.OrderBy(value => value, StringComparer.Ordinal).ToList()
            };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(checkpoint, Formatting.None));
        }

        public long WriteCheckpoint(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var text = new StreamWriter(stream, new UTF8Encoding(false), 65536))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.None })
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault();
                writer.WriteStartObject();
                WriteArray(writer, serializer, "People", _people.Values.OrderBy(value => value.PersonId, StringComparer.Ordinal));
                WriteArray(writer, serializer, "Households", _households.Values.OrderBy(value => value.HouseholdId, StringComparer.Ordinal));
                WriteArray(writer, serializer, "Events", _events.Values.OrderBy(value => value.EventId, StringComparer.Ordinal));
                writer.WritePropertyName("PersonPartitions");
                writer.WriteStartObject();
                foreach (KeyValuePair<string, string> pair in _personPartitions.OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(pair.Key);
                    writer.WriteValue(pair.Value);
                }
                writer.WriteEndObject();
                WriteArray(writer, serializer, "LoadedPartitions", _loadedPartitions.OrderBy(value => value, StringComparer.Ordinal));
                WriteArray(writer, serializer, "CompletedEventIds", _completedEvents.OrderBy(value => value, StringComparer.Ordinal));
                WriteArray(writer, serializer, "DirtyPersonIds", _dirtyPeople.OrderBy(value => value, StringComparer.Ordinal));
                WriteArray(writer, serializer, "DirtyHouseholdIds", _dirtyHouseholds.OrderBy(value => value, StringComparer.Ordinal));
                WriteArray(writer, serializer, "DirtyEventIds", _dirtyEvents.OrderBy(value => value, StringComparer.Ordinal));
                writer.WriteEndObject();
            }
            return new FileInfo(path).Length;
        }

        public void RestoreCheckpoint(byte[] checkpoint)
        {
            StoreCheckpoint value = JsonConvert.DeserializeObject<StoreCheckpoint>(Encoding.UTF8.GetString(checkpoint));
            if (value == null)
            {
                throw new InvalidDataException("Checkpoint could not be deserialized.");
            }
            _people = value.People.ToDictionary(item => item.PersonId, StringComparer.Ordinal);
            _households = value.Households.ToDictionary(item => item.HouseholdId, StringComparer.Ordinal);
            _events = value.Events.ToDictionary(item => item.EventId, StringComparer.Ordinal);
            _personPartitions = new Dictionary<string, string>(value.PersonPartitions, StringComparer.Ordinal);
            _knownPartitions = new HashSet<string>(_personPartitions.Values, StringComparer.Ordinal);
            _loadedPartitions = new HashSet<string>(value.LoadedPartitions, StringComparer.Ordinal);
            _completedEvents = new HashSet<string>(value.CompletedEventIds, StringComparer.Ordinal);
            _dirtyPeople = new HashSet<string>(value.DirtyPersonIds, StringComparer.Ordinal);
            _dirtyHouseholds = new HashSet<string>(value.DirtyHouseholdIds, StringComparer.Ordinal);
            _dirtyEvents = new HashSet<string>(value.DirtyEventIds, StringComparer.Ordinal);
            _attention = new Dictionary<string, AttentionPersonView>(StringComparer.Ordinal);
            RebuildIndexes();
        }

        public void RestoreCheckpoint(string path)
        {
            _people = NewPersonDictionary();
            _households = NewHouseholdDictionary();
            _events = NewEventDictionary();
            _personPartitions = new Dictionary<string, string>(StringComparer.Ordinal);
            _knownPartitions = new HashSet<string>(StringComparer.Ordinal);
            _loadedPartitions = new HashSet<string>(StringComparer.Ordinal);
            _completedEvents = new HashSet<string>(StringComparer.Ordinal);
            _dirtyPeople = new HashSet<string>(StringComparer.Ordinal);
            _dirtyHouseholds = new HashSet<string>(StringComparer.Ordinal);
            _dirtyEvents = new HashSet<string>(StringComparer.Ordinal);
            _attention = new Dictionary<string, AttentionPersonView>(StringComparer.Ordinal);
            _locationIndex = NewIndex();
            _occupationIndex = NewIndex();
            _organizationIndex = NewIndex();
            _fatherIndex = NewIndex();
            _motherIndex = NewIndex();
            _dueIndex = new SortedDictionary<int, List<string>>();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            StoreCheckpoint value;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var text = new StreamReader(stream, Encoding.UTF8, true, 65536))
            using (var reader = new JsonTextReader(text))
                value = JsonSerializer.CreateDefault().Deserialize<StoreCheckpoint>(reader);
            if (value == null) throw new InvalidDataException("Checkpoint could not be deserialized.");
            _people = value.People.ToDictionary(item => item.PersonId, StringComparer.Ordinal);
            _households = value.Households.ToDictionary(item => item.HouseholdId, StringComparer.Ordinal);
            _events = value.Events.ToDictionary(item => item.EventId, StringComparer.Ordinal);
            _personPartitions = new Dictionary<string, string>(value.PersonPartitions, StringComparer.Ordinal);
            _knownPartitions = new HashSet<string>(_personPartitions.Values, StringComparer.Ordinal);
            _loadedPartitions = new HashSet<string>(value.LoadedPartitions, StringComparer.Ordinal);
            _completedEvents = new HashSet<string>(value.CompletedEventIds, StringComparer.Ordinal);
            _dirtyPeople = new HashSet<string>(value.DirtyPersonIds, StringComparer.Ordinal);
            _dirtyHouseholds = new HashSet<string>(value.DirtyHouseholdIds, StringComparer.Ordinal);
            _dirtyEvents = new HashSet<string>(value.DirtyEventIds, StringComparer.Ordinal);
            RebuildIndexes();
        }

        public StoreDigest ComputeDigest()
        {
            IEnumerable<string> CoreLines()
            {
                foreach (PermanentPersonRecord person in _people.Values.OrderBy(value => value.PersonId, StringComparer.Ordinal))
                {
                    var core = new StringBuilder(384);
                    core.Append(person.PersonId).Append('|').Append(person.NameIndex).Append('|').Append(person.Gender).Append('|')
                    .Append(person.BirthDay).Append('|').Append(person.Alive).Append('|').Append(person.DeathDay).Append('|')
                    .Append(person.BirthLocationId).Append('|').Append(person.CurrentLocationId).Append('|').Append(person.HouseholdId).Append('|')
                    .Append(person.FatherId).Append('|').Append(person.MotherId).Append('|').Append(person.Occupation).Append('|')
                    .Append(person.LaborSummary).Append('|').Append(person.HealthSummary).Append('|').Append(person.PrimaryOrganizationId).Append('|')
                    .Append(person.NextDueDay).Append('|').Append(person.NextDueReason).Append('|').Append(person.RecordVersion).Append('|')
                    .Append(person.Available).Append('|').Append(person.SourceLayer).Append('|').Append(person.SourcePopulationAdminUnitId).Append('\n');
                    yield return core.ToString();
                }
                foreach (HouseholdRecord household in _households.Values.OrderBy(value => value.HouseholdId, StringComparer.Ordinal))
                {
                    var core = new StringBuilder(256);
                    core.Append(household.HouseholdId).Append('|').Append(household.LocationId).Append('|')
                    .Append(string.Join(",", household.MemberIds.OrderBy(value => value, StringComparer.Ordinal))).Append('|')
                    .Append(household.RecordVersion).Append('|').Append(household.SourceLayer).Append('\n');
                    yield return core.ToString();
                }
            }
            IEnumerable<string> EventLines()
            {
                foreach (DueEventRecord dueEvent in _events.Values.OrderBy(value => value.DueDay).ThenBy(value => value.EventId, StringComparer.Ordinal))
                {
                    var events = new StringBuilder(256);
                    events.Append(dueEvent.EventId).Append('|').Append(dueEvent.PersonId).Append('|').Append(dueEvent.DueDay).Append('|')
                    .Append(dueEvent.Reason).Append('|').Append(dueEvent.RuleVersion).Append('|').Append(dueEvent.ActionCoordinate).Append('|')
                    .Append(dueEvent.SourceLayer).Append('|').Append(_completedEvents.Contains(dueEvent.EventId)).Append('\n');
                    yield return events.ToString();
                }
            }
            return new StoreDigest { CoreDigest = StableHash.Sha256(CoreLines()), EventDigest = StableHash.Sha256(EventLines()) };
        }

        private static void WriteArray<T>(JsonTextWriter writer, JsonSerializer serializer, string name, IEnumerable<T> values)
        {
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (T value in values) serializer.Serialize(writer, value);
            writer.WriteEndArray();
        }

        public IReadOnlyList<AttentionPersonView> ExpandAttention(IEnumerable<string> personIds)
        {
            var views = new List<AttentionPersonView>();
            foreach (string personId in personIds.OrderBy(value => value, StringComparer.Ordinal))
            {
                PermanentPersonRecord person = GetPerson(personId);
                var view = new AttentionPersonView
                {
                    PersonId = person.PersonId,
                    HouseholdId = person.HouseholdId,
                    LocationId = person.CurrentLocationId,
                    Occupation = person.Occupation,
                    HealthSummary = person.HealthSummary
                };
                _attention[personId] = view;
                views.Add(view);
            }
            return views;
        }

        public void ReleaseAttention(IEnumerable<string> personIds)
        {
            foreach (string personId in personIds)
            {
                _attention.Remove(personId);
            }
        }

        private void RebuildIndexes()
        {
            _locationIndex = NewIndex();
            _occupationIndex = NewIndex();
            _organizationIndex = NewIndex();
            _fatherIndex = NewIndex();
            _motherIndex = NewIndex();
            _dueIndex = new SortedDictionary<int, List<string>>();
            foreach (PermanentPersonRecord person in _people.Values)
            {
                AddIndex(_locationIndex, person.CurrentLocationId, person.PersonId);
                AddIndex(_occupationIndex, person.Occupation, person.PersonId);
                AddIndex(_organizationIndex, person.PrimaryOrganizationId, person.PersonId);
                AddOptionalIndex(_fatherIndex, person.FatherId, person.PersonId);
                AddOptionalIndex(_motherIndex, person.MotherId, person.PersonId);
            }
            foreach (DueEventRecord dueEvent in _events.Values)
            {
                if (!_completedEvents.Contains(dueEvent.EventId))
                {
                    AddDueIndex(dueEvent);
                }
            }
        }

        private static Dictionary<string, PermanentPersonRecord> NewPersonDictionary() { return new Dictionary<string, PermanentPersonRecord>(StringComparer.Ordinal); }
        private static Dictionary<string, HouseholdRecord> NewHouseholdDictionary() { return new Dictionary<string, HouseholdRecord>(StringComparer.Ordinal); }
        private static Dictionary<string, DueEventRecord> NewEventDictionary() { return new Dictionary<string, DueEventRecord>(StringComparer.Ordinal); }
        private static Dictionary<string, HashSet<string>> NewIndex() { return new Dictionary<string, HashSet<string>>(StringComparer.Ordinal); }

        private static void AddOptionalIndex(Dictionary<string, HashSet<string>> index, string key, string personId)
        {
            if (!string.IsNullOrEmpty(key)) AddIndex(index, key, personId);
        }

        private static void RemoveOptionalIndex(Dictionary<string, HashSet<string>> index, string key, string personId)
        {
            if (!string.IsNullOrEmpty(key)) RemoveIndex(index, key, personId);
        }

        private static void RemoveIndex(Dictionary<string, HashSet<string>> index, string key, string personId)
        {
            HashSet<string> values;
            if (index.TryGetValue(key, out values))
            {
                values.Remove(personId);
                if (values.Count == 0) index.Remove(key);
            }
        }

        private static void AddIndex(Dictionary<string, HashSet<string>> index, string key, string personId)
        {
            HashSet<string> ids;
            if (!index.TryGetValue(key, out ids))
            {
                ids = new HashSet<string>(StringComparer.Ordinal);
                index.Add(key, ids);
            }
            ids.Add(personId);
        }

        private void AddDueIndex(DueEventRecord dueEvent)
        {
            List<string> ids;
            if (!_dueIndex.TryGetValue(dueEvent.DueDay, out ids))
            {
                ids = new List<string>();
                _dueIndex.Add(dueEvent.DueDay, ids);
            }
            int insertion = ids.BinarySearch(dueEvent.EventId, StringComparer.Ordinal);
            ids.Insert(insertion < 0 ? ~insertion : insertion, dueEvent.EventId);
        }
    }

    internal sealed class StoreCheckpoint
    {
        public List<PermanentPersonRecord> People { get; set; }
        public List<HouseholdRecord> Households { get; set; }
        public List<DueEventRecord> Events { get; set; }
        public Dictionary<string, string> PersonPartitions { get; set; }
        public List<string> LoadedPartitions { get; set; }
        public List<string> CompletedEventIds { get; set; }
        public List<string> DirtyPersonIds { get; set; }
        public List<string> DirtyHouseholdIds { get; set; }
        public List<string> DirtyEventIds { get; set; }
    }

    internal sealed class IncrementalStoreBatch
    {
        public List<PermanentPersonRecord> People { get; set; }
        public List<HouseholdRecord> Households { get; set; }
        public List<DueEventRecord> Events { get; set; }
        public List<string> CompletedEventIds { get; set; }
    }

    internal sealed class RunEvidence
    {
        public int PersonCount { get; set; }
        public int HouseholdCount { get; set; }
        public int PartitionCount { get; set; }
        public int EventCount { get; set; }
        public int AliveCount { get; set; }
        public int HouseholdMemberCount { get; set; }
        public int LocationPopulationTotal { get; set; }
        public int OccupationPopulationTotal { get; set; }
        public int DueCutoffDay { get; set; }
        public int DueNodeCount { get; set; }
        public int DueScannedNodeCount { get; set; }
        public int QueryCandidateCount { get; set; }
        public int QueryResultCount { get; set; }
        public int IncrementalSaveBytes { get; set; }
        public int CheckpointBytes { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public long ManagedMemoryDeltaBytes { get; set; }
        public string CoreDigest { get; set; }
        public string EventDigest { get; set; }
        public Dictionary<string, int> LocationCounts { get; set; }
        public Dictionary<string, int> OccupationCounts { get; set; }
        public List<string> Invariants { get; set; }
    }
}
