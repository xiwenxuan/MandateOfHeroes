using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Tools.PopulationBenchmark
{
    internal static class PopulationSchedulingProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                P2Options options = P2Options.Parse(args);
                Stopwatch generationWatch = Stopwatch.StartNew();
                InputAudit audit = M13InputReader.ReadAndAudit(Path.Combine(options.ProjectRoot, "Data", "HistoricalPopulation"));
                BenchmarkDataset generated = DeterministicPopulationGenerator.Generate(audit.PopulationBuckets, options.Count, options.Seed);
                P2DatasetContext context = P2DatasetAdapter.Prepare(generated, options.Seed);
                generationWatch.Stop();
                P4Progress.Write(options, "generation_complete", null);
                var evidence = new List<P2BackendEvidence>();
                foreach (string backend in options.Backends)
                {
                    P2BackendEvidence item = RunCandidate(backend, context, options, Path.Combine(options.WorkspaceRoot, backend));
                    evidence.Add(item);
                    Console.WriteLine("{0}: people={1} births={2} deaths={3} scanned={4} core={5}", backend,
                        item.FinalPersonCount, item.TotalBirths, item.TotalDeaths, item.Windows.Sum(value => value.ScannedNodes), item.CoreDigest);
                }

                string core = evidence[0].CoreDigest;
                string events = evidence[0].EventDigest;
                string windowSignature = WindowSignature(evidence[0].Windows);
                bool deterministic = evidence.All(value => value.CoreDigest == core && value.EventDigest == events &&
                    WindowSignature(value.Windows) == windowSignature && value.FinalPersonCount == evidence[0].FinalPersonCount &&
                    value.FinalAliveCount == evidence[0].FinalAliveCount);
                if (!deterministic) throw new InvalidOperationException("P2 candidate backends produced different scheduling facts.");

                JObject report = new JObject
                {
                    ["schema_version"] = options.Stage == "M15-P4" ? "m15.p4.candidate.v1" :
                        options.Stage == "M15-P3" ? "m15.p3.candidate.v1" : "m15.p2.report.v1",
                    ["stage"] = options.Stage,
                    ["status"] = "passed",
                    ["source_layer"] = "pressure_test_v1",
                    ["parameters"] = new JObject
                    {
                        ["person_count"] = options.Count,
                        ["master_seed"] = options.Seed,
                        ["backends"] = new JArray(options.Backends),
                        ["advance_days"] = new JArray(1, 30, 365)
                    },
                    ["generation"] = new JObject
                    {
                        ["elapsed_milliseconds"] = generationWatch.ElapsedMilliseconds,
                        ["managed_memory_bytes"] = GC.GetTotalMemory(false),
                        ["process_peak_working_set_bytes"] = Process.GetCurrentProcess().PeakWorkingSet64
                    },
                    ["input_audit"] = JObject.FromObject(audit),
                    ["fixture"] = JObject.FromObject(context.Fixture),
                    ["backends"] = JArray.FromObject(evidence),
                    ["cross_backend_determinism"] = new JObject
                    {
                        ["core_digest"] = core,
                        ["event_digest"] = events,
                        ["window_signature"] = windowSignature,
                        ["passed"] = deterministic
                    },
                    ["decision"] = options.Stage == "M15-P4"
                        ? "This optimized candidate completed the requested P4 scale contract. Formal backend selection remains external to this result."
                        : options.Stage == "M15-P3" ? "This candidate completed the requested P3 scale contract. Cross-candidate decision remains external to this result."
                        : "M15-P2 scheduling, partition-reference and query contracts passed at 10,000 people. P3 scale evidence is still required before backend selection."
                };
                Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath));
                File.WriteAllText(options.OutputPath, report.ToString(Formatting.Indented), new UTF8Encoding(false));
                Console.WriteLine("Result: " + options.OutputPath);
                P4Progress.Write(options, "completed", null);
                Console.WriteLine("RESULT status=passed stage={0} backends={1} people={2} deterministic=true", options.Stage, options.Backends.Count, options.Count);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                Console.Error.WriteLine("RESULT status=failed stage=M15-P2-P3-or-P4");
                return 1;
            }
        }

        private static P2BackendEvidence RunCandidate(string backend, P2DatasetContext context, P2Options options, string root)
        {
            long managedBefore = GC.GetTotalMemory(false);
            Directory.CreateDirectory(root);
            using (ICandidatePopulationStore store = CandidateStoreFactory.Create(backend, root))
            {
                int initialPersonCount = context.InitialPersonCount;
                int initialHouseholdCount = context.InitialHouseholdCount;
                int initialEventCount = context.InitialEventCount;
                Stopwatch loadWatch = Stopwatch.StartNew();
                if (options.Stage == "M15-P4") store.BeginBulkLoad();
                try
                {
                    foreach (PopulationPartition partition in context.Dataset.Partitions.OrderBy(value => value.PartitionId, StringComparer.Ordinal))
                        store.BatchWrite(partition);
                    if (options.Stage == "M15-P4") store.EndBulkLoad();
                }
                catch
                {
                    throw;
                }
                loadWatch.Stop();
                P4Progress.Write(options, "initial_load_complete", backend);
                store.ValidatePhysicalStore(initialPersonCount, initialHouseholdCount, initialEventCount);

                StoreDigest initialDigest = store.ComputeDigest();
                long initialCheckpointBytes;
                if (options.Stage == "M15-P4")
                {
                    initialCheckpointBytes = store.CreateCheckpointDirectory(Path.Combine(options.WorkspaceRoot, "checkpoints", backend + "-initial"));
                    store.CreateIncrementalSaveFile(Path.Combine(options.WorkspaceRoot, "increments", backend + "-initial.json"));
                }
                else
                {
                    byte[] initialCheckpoint = store.CreateCheckpoint();
                    initialCheckpointBytes = initialCheckpoint.Length;
                    store.CreateIncrementalSave();
                }
                P4Progress.Write(options, "initial_checkpoint_complete", backend);
                CrossReferenceEvidence initialReferences = AuditCrossReferences(store, context.CrossPartitionPersonIds,
                    context.PersonPartitions, context.PartitionByLocation);

                if (options.Stage == "M15-P4")
                {
                    store.EnableSettlementOverlay();
                    context.ReleaseLoadContainers();
                }

                var scheduler = new P2Scheduler(store, options.Seed, context.Locations, context.PartitionByLocation,
                    new Dictionary<string, string>(context.PersonPartitions, StringComparer.Ordinal));
                var windows = new List<P2WindowEvidence>();
                foreach (int targetDay in new[] { 1, 30, 365 })
                {
                    windows.Add(scheduler.AdvanceTo(targetDay));
                    P4Progress.Write(options, "scheduled_day_" + targetDay.ToString(CultureInfo.InvariantCulture), backend);
                }
                P4Progress.Write(options, "annual_scheduling_complete", backend);

                int births = windows.Sum(value => value.Births);
                int deaths = windows.Sum(value => value.Deaths);
                List<string> allIds = context.InitialPersonIds.Concat(scheduler.NewPersonIds)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
                if (allIds.Distinct(StringComparer.Ordinal).Count() != allIds.Count)
                    throw new InvalidDataException(backend + " produced duplicate permanent person IDs.");
                List<PermanentPersonRecord> finalPeople = allIds.Select(store.GetPerson).ToList();
                int finalAlive = finalPeople.Count(value => value.Alive);
                if (finalPeople.Count != initialPersonCount + births)
                    throw new InvalidDataException(backend + " failed permanent population conservation.");
                if (finalAlive != initialPersonCount + births - deaths)
                    throw new InvalidDataException(backend + " failed alive population conservation.");
                if (finalPeople.GroupBy(value => value.CurrentLocationId, StringComparer.Ordinal).Sum(group => group.Count()) != finalPeople.Count)
                    throw new InvalidDataException(backend + " failed location population conservation.");
                if (store.ReadDueEvents(365).ScannedNodeCount != 0)
                    throw new InvalidDataException(backend + " retained completed events at or before day 365.");

                PermanentPersonRecord laborProbe = finalPeople.First(value => value.Alive && value.Available &&
                    value.HealthSummary == "stable" && value.LaborSummary == "available");
                var laborQuery = new PopulationQuery
                {
                    LocationId = laborProbe.CurrentLocationId,
                    Occupation = laborProbe.Occupation,
                    HealthSummary = "stable",
                    LaborSummary = "available",
                    Alive = true,
                    Available = true
                };
                QueryResult labor = store.QueryPeople(laborQuery);
                if (!labor.People.Any(value => value.PersonId == laborProbe.PersonId) || labor.People.Any(value =>
                    !value.Alive || !value.Available || value.HealthSummary != "stable" || value.LaborSummary != "available" ||
                    value.CurrentLocationId != laborProbe.CurrentLocationId || value.Occupation != laborProbe.Occupation))
                    throw new InvalidDataException(backend + " labor query returned an ineligible person.");

                QueryResult organization = store.QueryPeople(new PopulationQuery { OrganizationId = laborProbe.PrimaryOrganizationId });
                if (organization.People.Count == 0 || organization.People.Any(value => value.PrimaryOrganizationId != laborProbe.PrimaryOrganizationId))
                    throw new InvalidDataException(backend + " organization query failed.");
                QueryResult children = store.QueryChildren(context.ParentProbeId);
                if (children.People.Count == 0 || children.People.Any(value => value.FatherId != context.ParentProbeId && value.MotherId != context.ParentProbeId))
                    throw new InvalidDataException(backend + " child reverse query failed.");
                P4Progress.Write(options, "queries_complete", backend);

                CrossReferenceEvidence finalReferences = AuditCrossReferences(store, context.CrossPartitionPersonIds,
                    scheduler.PersonPartitions, context.PartitionByLocation);
                StoreDigest beforeAttention = store.ComputeDigest();
                string[] attentionIds = allIds.Take(32).ToArray();
                IReadOnlyList<AttentionPersonView> attention = store.ExpandAttention(attentionIds);
                store.ReleaseAttention(attentionIds);
                if (attention.Count != attentionIds.Length || store.ComputeDigest().CoreDigest != beforeAttention.CoreDigest)
                    throw new InvalidDataException(backend + " attention round trip changed permanent facts.");

                long incrementalBytes;
                if (options.Stage == "M15-P4")
                    incrementalBytes = store.CreateIncrementalSaveFile(Path.Combine(options.WorkspaceRoot, "increments", backend + "-final.json"));
                else incrementalBytes = store.CreateIncrementalSave().Length;
                P4Progress.Write(options, "final_increment_complete", backend);
                StoreDigest beforeRestore = store.ComputeDigest();
                long checkpointBytes;
                string checkpointDirectory = null;
                byte[] checkpoint = null;
                if (options.Stage == "M15-P4")
                {
                    checkpointDirectory = Path.Combine(options.WorkspaceRoot, "checkpoints", backend + "-final");
                    checkpointBytes = store.CreateCheckpointDirectory(checkpointDirectory);
                }
                else
                {
                    checkpoint = store.CreateCheckpoint();
                    checkpointBytes = checkpoint.Length;
                }
                P4Progress.Write(options, "final_checkpoint_complete", backend);
                int finalPersonCount = finalPeople.Count;
                int laborCandidateCount = labor.CandidateCount;
                int laborResultCount = labor.People.Count;
                int organizationResultCount = organization.People.Count;
                int childResultCount = children.People.Count;
                finalPeople = null;
                labor = null;
                organization = null;
                children = null;
                attention = null;
                Stopwatch restoreWatch = Stopwatch.StartNew();
                if (options.Stage == "M15-P4") store.RestoreCheckpointDirectory(checkpointDirectory);
                else store.RestoreCheckpoint(checkpoint);
                restoreWatch.Stop();
                P4Progress.Write(options, "restore_complete", backend);
                StoreDigest afterRestore = store.ComputeDigest();
                if (beforeRestore.CoreDigest != afterRestore.CoreDigest || beforeRestore.EventDigest != afterRestore.EventDigest)
                    throw new InvalidDataException(backend + " checkpoint restore changed P2 facts.");
                AuditCrossReferences(store, context.CrossPartitionPersonIds, scheduler.PersonPartitions, context.PartitionByLocation);

                return new P2BackendEvidence
                {
                    Backend = backend,
                    InitialLoadMilliseconds = loadWatch.ElapsedMilliseconds,
                    InitialCheckpointBytes = initialCheckpointBytes,
                    InitialCoreDigest = initialDigest.CoreDigest,
                    Windows = windows,
                    InitialCrossReferences = initialReferences,
                    FinalCrossReferences = finalReferences,
                    InitialPersonCount = initialPersonCount,
                    FinalPersonCount = finalPersonCount,
                    FinalAliveCount = finalAlive,
                    TotalBirths = births,
                    TotalDeaths = deaths,
                    TotalMigrations = windows.Sum(value => value.Migrations),
                    LaborCandidateCount = laborCandidateCount,
                    LaborResultCount = laborResultCount,
                    OrganizationResultCount = organizationResultCount,
                    ChildResultCount = childResultCount,
                    IncrementalBytes = incrementalBytes,
                    CheckpointBytes = checkpointBytes,
                    RestoreMilliseconds = restoreWatch.ElapsedMilliseconds,
                    PhysicalStorageBytes = store.GetPhysicalStorageBytes(),
                    ManagedMemoryBeforeBytes = managedBefore,
                    ManagedMemoryAfterBytes = GC.GetTotalMemory(false),
                    ProcessPeakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
                    CoreDigest = afterRestore.CoreDigest,
                    EventDigest = afterRestore.EventDigest,
                    ContractChecks = new List<string>
                    {
                        "due_only_scheduling", "specific_person_writeback", "birth_permanent_identity",
                        "death_preserves_identity", "migration_conserves_population", "cross_partition_references",
                        "parent_child_reverse_query", "organization_query", "eligible_labor_query",
                        "attention_preserves_facts", "checkpoint_preserves_facts"
                    }
                };
            }
        }

        private static CrossReferenceEvidence AuditCrossReferences(ICandidatePopulationStore store, IEnumerable<string> personIds,
            IDictionary<string, string> personPartitions, IDictionary<string, string> partitionByLocation)
        {
            int people = 0;
            int cross = 0;
            int parentLinks = 0;
            int householdLinks = 0;
            var loaded = new HashSet<string>(StringComparer.Ordinal);
            foreach (string personId in personIds.Take(32))
            {
                PermanentPersonRecord person = store.GetPerson(personId);
                HouseholdRecord household = store.GetHousehold(person.HouseholdId);
                people++;
                householdLinks++;
                string personPartition = personPartitions[person.PersonId];
                string householdPartition = partitionByLocation[household.LocationId];
                foreach (string partition in new[] { personPartition, householdPartition }.Distinct(StringComparer.Ordinal))
                {
                    store.UnloadPartition(partition);
                    store.LoadPartition(partition);
                    loaded.Add(partition);
                }
                if (personPartition != householdPartition) cross++;
                foreach (string parentId in new[] { person.FatherId, person.MotherId }.Where(value => !string.IsNullOrEmpty(value)))
                {
                    PermanentPersonRecord parent = store.GetPerson(parentId);
                    parentLinks++;
                    string parentPartition = personPartitions[parent.PersonId];
                    if (parentPartition != personPartition) cross++;
                    QueryResult children = store.QueryChildren(parentId);
                    if (!children.People.Any(value => value.PersonId == person.PersonId))
                        throw new InvalidDataException("Cross-partition reverse parent reference is missing for " + person.PersonId);
                }
            }
            return new CrossReferenceEvidence
            {
                SamplePeople = people,
                CrossPartitionLinks = cross,
                ParentLinksResolved = parentLinks,
                HouseholdLinksResolved = householdLinks,
                LoadedPartitionCount = loaded.Count
            };
        }

        private static string WindowSignature(IEnumerable<P2WindowEvidence> windows)
        {
            return string.Join(";", windows.Select(value => string.Format(CultureInfo.InvariantCulture,
                "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", value.TargetDay, value.ScannedNodes, value.DueEvents,
                value.ChangedPeople, value.Births, value.Deaths, value.Migrations, value.WriteBackRecords)));
        }
    }

    internal sealed class P2Scheduler
    {
        private static readonly string[] Occupations = { "farmer", "artisan", "merchant", "clerk", "healer", "laborer" };
        private readonly ICandidatePopulationStore _store;
        private readonly long _seed;
        private readonly List<string> _locations;
        private readonly IDictionary<string, string> _partitionByLocation;
        private readonly Dictionary<string, string> _personPartitions;
        private readonly List<string> _newPersonIds = new List<string>();

        public IReadOnlyList<string> NewPersonIds { get { return _newPersonIds; } }
        public IDictionary<string, string> PersonPartitions { get { return _personPartitions; } }

        public P2Scheduler(ICandidatePopulationStore store, long seed, IEnumerable<string> locations,
            IDictionary<string, string> partitionByLocation, Dictionary<string, string> personPartitions)
        {
            _store = store;
            _seed = seed;
            _locations = locations.OrderBy(value => value, StringComparer.Ordinal).ToList();
            _partitionByLocation = partitionByLocation;
            _personPartitions = personPartitions;
        }

        public P2WindowEvidence AdvanceTo(int targetDay)
        {
            Stopwatch watch = Stopwatch.StartNew();
            DueReadResult due = _store.ReadDueEvents(targetDay);
            if (due.ScannedNodeCount != due.Events.Count || due.Events.Any(value => value.DueDay > targetDay))
                throw new InvalidDataException("Scheduler scanned a non-due event.");
            var batch = new PopulationChangeBatch { AbsoluteDay = targetDay, RuleVersion = "m15.p2.scheduler.v1" };
            var households = new Dictionary<string, HouseholdRecord>(StringComparer.Ordinal);
            var loaded = new HashSet<string>(StringComparer.Ordinal);
            int births = 0;
            int deaths = 0;
            int migrations = 0;
            foreach (DueEventRecord dueEvent in due.Events.OrderBy(value => value.DueDay).ThenBy(value => value.EventId, StringComparer.Ordinal))
            {
                PermanentPersonRecord original = _store.GetPerson(dueEvent.PersonId);
                PermanentPersonRecord person = ClonePerson(original);
                LoadReferences(person, loaded);
                bool scheduleSuccessor = true;
                if (dueEvent.Reason == "birth")
                {
                    PermanentPersonRecord child = CreateChild(person, dueEvent);
                    string partition = _partitionByLocation[child.CurrentLocationId];
                    batch.People.Add(new PersonPartitionWrite { Person = child, PartitionId = partition });
                    _newPersonIds.Add(child.PersonId);
                    _personPartitions.Add(child.PersonId, partition);
                    HouseholdRecord household;
                    if (!households.TryGetValue(child.HouseholdId, out household))
                    {
                        household = CloneHousehold(_store.GetHousehold(child.HouseholdId));
                        households.Add(household.HouseholdId, household);
                    }
                    household.MemberIds.Add(child.PersonId);
                    batch.AddedEvents.Add(CreateSuccessor(child, dueEvent, "child_review"));
                    births++;
                }
                else if (dueEvent.Reason == "death")
                {
                    if (person.Alive)
                    {
                        person.Alive = false;
                        person.DeathDay = dueEvent.DueDay;
                        person.Available = false;
                        person.LaborSummary = "not_available";
                        deaths++;
                    }
                    scheduleSuccessor = false;
                }
                else if (dueEvent.Reason == "migration")
                {
                    person.CurrentLocationId = PickOtherLocation(person.CurrentLocationId, dueEvent.EventId);
                    migrations++;
                }
                else if (dueEvent.Reason == "disease")
                {
                    person.HealthSummary = "limited";
                    person.Available = false;
                    person.LaborSummary = "not_available";
                }
                else if (dueEvent.Reason == "service")
                {
                    person.Occupation = "soldier";
                    person.Available = false;
                    person.LaborSummary = "not_available";
                }
                else if (dueEvent.Reason == "occupation_change")
                {
                    person.Occupation = Occupations[(int)(StableHash.UInt64(_seed, "p2_occupation", dueEvent.EventId, 0) % (ulong)Occupations.Length)];
                    person.Available = person.Alive && person.HealthSummary == "stable";
                    person.LaborSummary = person.Available ? "available" : "not_available";
                }

                person.NextDueDay = dueEvent.DueDay + 365;
                person.NextDueReason = scheduleSuccessor ? "annual_review" : "none";
                person.RecordVersion++;
                string personPartition = _partitionByLocation[person.CurrentLocationId];
                batch.People.Add(new PersonPartitionWrite { Person = person, PartitionId = personPartition });
                _personPartitions[person.PersonId] = personPartition;
                if (scheduleSuccessor) batch.AddedEvents.Add(CreateSuccessor(person, dueEvent, "annual_review"));
            }
            batch.Households.AddRange(households.Values.OrderBy(value => value.HouseholdId, StringComparer.Ordinal));
            _store.ApplyChangeBatch(batch);
            _store.CommitDueChanges(due.Events);
            if (_store.ReadDueEvents(targetDay).ScannedNodeCount != 0)
                throw new InvalidDataException("Committed events remained in the due queue.");
            watch.Stop();
            return new P2WindowEvidence
            {
                TargetDay = targetDay,
                ScannedNodes = due.ScannedNodeCount,
                DueEvents = due.Events.Count,
                ChangedPeople = batch.People.Count,
                ChangedHouseholds = batch.Households.Count,
                AddedEvents = batch.AddedEvents.Count,
                LoadedPartitions = loaded.Count,
                WriteBackRecords = batch.People.Count + batch.Households.Count + batch.AddedEvents.Count + due.Events.Count,
                Births = births,
                Deaths = deaths,
                Migrations = migrations,
                ElapsedMilliseconds = watch.ElapsedMilliseconds
            };
        }

        private void LoadReferences(PermanentPersonRecord person, ISet<string> loaded)
        {
            var partitions = new HashSet<string>(StringComparer.Ordinal) { _personPartitions[person.PersonId] };
            HouseholdRecord household = _store.GetHousehold(person.HouseholdId);
            partitions.Add(_partitionByLocation[household.LocationId]);
            foreach (string parentId in new[] { person.FatherId, person.MotherId }.Where(value => !string.IsNullOrEmpty(value)))
                partitions.Add(_personPartitions[parentId]);
            foreach (string partition in partitions.OrderBy(value => value, StringComparer.Ordinal))
            {
                _store.LoadPartition(partition);
                loaded.Add(partition);
            }
        }

        private PermanentPersonRecord CreateChild(PermanentPersonRecord parent, DueEventRecord dueEvent)
        {
            string id = StableHash.Id("person.p2.birth", _seed, dueEvent.EventId + "|" + dueEvent.DueDay.ToString(CultureInfo.InvariantCulture));
            string gender = StableHash.UInt64(_seed, "p2_birth_gender", id, 0) % 2UL == 0UL ? "male" : "female";
            return new PermanentPersonRecord
            {
                PersonId = id,
                NameIndex = (int)(StableHash.UInt64(_seed, "p2_birth_name", id, 0) % 1000000UL),
                Gender = gender,
                BirthDay = dueEvent.DueDay,
                Alive = true,
                BirthLocationId = parent.CurrentLocationId,
                CurrentLocationId = parent.CurrentLocationId,
                HouseholdId = parent.HouseholdId,
                FatherId = parent.Gender == "male" ? parent.PersonId : null,
                MotherId = parent.Gender == "female" ? parent.PersonId : null,
                Occupation = "dependent",
                LaborSummary = "not_available",
                HealthSummary = "stable",
                PrimaryOrganizationId = parent.PrimaryOrganizationId,
                NextDueDay = dueEvent.DueDay + 365,
                NextDueReason = "child_review",
                RecordVersion = 1,
                Available = false,
                SourceLayer = "pressure_test_v1",
                SourcePopulationAdminUnitId = parent.SourcePopulationAdminUnitId
            };
        }

        private DueEventRecord CreateSuccessor(PermanentPersonRecord person, DueEventRecord source, string reason)
        {
            int day = source.DueDay + 365;
            return new DueEventRecord
            {
                EventId = StableHash.Id("event.p2.next", _seed, source.EventId + "|" + person.PersonId + "|" + reason),
                PersonId = person.PersonId,
                DueDay = day,
                Reason = reason,
                RuleVersion = "m15.p2.scheduler.v1",
                ActionCoordinate = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}/m15.p2", _seed, person.PersonId, day, reason),
                SourceLayer = "pressure_test_v1"
            };
        }

        private string PickOtherLocation(string current, string eventId)
        {
            int index = (int)(StableHash.UInt64(_seed, "p2_migration", eventId, 0) % (ulong)_locations.Count);
            if (_locations[index] == current) index = (index + 1) % _locations.Count;
            return _locations[index];
        }

        private static PermanentPersonRecord ClonePerson(PermanentPersonRecord value)
        {
            return new PermanentPersonRecord
            {
                PersonId = value.PersonId, NameIndex = value.NameIndex, Gender = value.Gender, BirthDay = value.BirthDay,
                Alive = value.Alive, DeathDay = value.DeathDay, BirthLocationId = value.BirthLocationId,
                CurrentLocationId = value.CurrentLocationId, HouseholdId = value.HouseholdId, FatherId = value.FatherId,
                MotherId = value.MotherId, Occupation = value.Occupation, LaborSummary = value.LaborSummary,
                HealthSummary = value.HealthSummary, PrimaryOrganizationId = value.PrimaryOrganizationId,
                NextDueDay = value.NextDueDay, NextDueReason = value.NextDueReason, RecordVersion = value.RecordVersion,
                Available = value.Available, SourceLayer = value.SourceLayer,
                SourcePopulationAdminUnitId = value.SourcePopulationAdminUnitId
            };
        }

        private static HouseholdRecord CloneHousehold(HouseholdRecord value)
        {
            return new HouseholdRecord
            {
                HouseholdId = value.HouseholdId, LocationId = value.LocationId,
                MemberIds = new List<string>(value.MemberIds), RecordVersion = value.RecordVersion + 1,
                SourceLayer = value.SourceLayer
            };
        }
    }

    internal static class P2DatasetAdapter
    {
        public static P2DatasetContext Prepare(BenchmarkDataset dataset, long seed)
        {
            List<string> locations = dataset.Partitions.Select(value => value.StableRegionId).OrderBy(value => value, StringComparer.Ordinal).ToList();
            Dictionary<string, string> partitionByLocation = dataset.Partitions.ToDictionary(value => value.StableRegionId, value => value.PartitionId, StringComparer.Ordinal);
            List<PermanentPersonRecord> movable = dataset.People.Where(value => !string.IsNullOrEmpty(value.FatherId) && !string.IsNullOrEmpty(value.MotherId))
                .OrderBy(value => value.PersonId, StringComparer.Ordinal).Take(64).ToList();
            for (int index = 0; index < movable.Count; index++)
            {
                int current = locations.IndexOf(movable[index].CurrentLocationId);
                movable[index].CurrentLocationId = locations[(current + 1 + index % (locations.Count - 1)) % locations.Count];
            }
            List<PermanentPersonRecord> eligible = dataset.People.Where(value => value.Available && value.HealthSummary == "stable")
                .OrderBy(value => value.PersonId, StringComparer.Ordinal).Take(64).ToList();
            foreach (PermanentPersonRecord person in eligible.Take(32)) { person.Available = false; person.LaborSummary = "traveling"; }
            foreach (PermanentPersonRecord person in eligible.Skip(32)) { person.Available = false; person.LaborSummary = "reserved"; }

            Dictionary<string, DueEventRecord> eventByPerson = dataset.Events.ToDictionary(value => value.PersonId, StringComparer.Ordinal);
            foreach (PopulationPartition partition in dataset.Partitions)
            {
                partition.People = dataset.People.Where(value => value.CurrentLocationId == partition.StableRegionId)
                    .OrderBy(value => value.PersonId, StringComparer.Ordinal).ToList();
                partition.Households = dataset.Households.Where(value => value.LocationId == partition.StableRegionId)
                    .OrderBy(value => value.HouseholdId, StringComparer.Ordinal).ToList();
                partition.Events = partition.People.Select(value => eventByPerson[value.PersonId])
                    .OrderBy(value => value.EventId, StringComparer.Ordinal).ToList();
            }
            Dictionary<string, string> personPartitions = dataset.Partitions.SelectMany(partition => partition.People.Select(person => new { person.PersonId, partition.PartitionId }))
                .ToDictionary(value => value.PersonId, value => value.PartitionId, StringComparer.Ordinal);
            var parentIds = new HashSet<string>(dataset.People.SelectMany(value => new[] { value.FatherId, value.MotherId })
                .Where(value => !string.IsNullOrEmpty(value)), StringComparer.Ordinal);
            string parentProbe = dataset.People.Where(value => parentIds.Contains(value.PersonId))
                .OrderBy(value => value.PersonId, StringComparer.Ordinal).First().PersonId;
            return new P2DatasetContext
            {
                Dataset = dataset,
                InitialPersonIds = dataset.People.Select(value => value.PersonId).OrderBy(value => value, StringComparer.Ordinal).ToList(),
                InitialPersonCount = dataset.People.Count,
                InitialHouseholdCount = dataset.Households.Count,
                InitialEventCount = dataset.Events.Count,
                Locations = locations,
                PartitionByLocation = partitionByLocation,
                PersonPartitions = personPartitions,
                CrossPartitionPersonIds = movable.Select(value => value.PersonId).ToList(),
                ParentProbeId = parentProbe,
                Fixture = new P2FixtureEvidence
                {
                    CrossPartitionPersonCount = movable.Count,
                    TravelingPersonCount = eligible.Take(32).Count(),
                    ReservedPersonCount = eligible.Skip(32).Count()
                }
            };
        }
    }

    internal sealed class P2Options
    {
        public string ProjectRoot { get; private set; }
        public string WorkspaceRoot { get; private set; }
        public string OutputPath { get; private set; }
        public int Count { get; private set; }
        public long Seed { get; private set; }
        public string Stage { get; private set; }
        public string ProgressPath { get; private set; }
        public List<string> Backends { get; private set; }

        public static P2Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Arguments must be supplied as --name value pairs.");
                values[args[index].Substring(2)] = args[index + 1];
            }
            string project = Path.GetFullPath(values.ContainsKey("project-root") ? values["project-root"] : Directory.GetCurrentDirectory());
            int count = values.ContainsKey("count") ? int.Parse(values["count"], CultureInfo.InvariantCulture) : 10000;
            long seed = values.ContainsKey("seed") ? long.Parse(values["seed"], CultureInfo.InvariantCulture) : 14000015L;
            if (count < 1000 || count > 1000000) throw new ArgumentOutOfRangeException("count");
            string stage = values.ContainsKey("stage") ? values["stage"] : "M15-P2";
            if (stage != "M15-P2" && stage != "M15-P3" && stage != "M15-P4") throw new ArgumentOutOfRangeException("stage");
            string backend = values.ContainsKey("backend") ? values["backend"] : "all";
            if (backend != "all" && backend != "sqlite" && backend != "binary" && backend != "hybrid")
                throw new ArgumentOutOfRangeException("backend");
            List<string> backends = backend == "all" ? new List<string> { "sqlite", "binary", "hybrid" } : new List<string> { backend };
            if (stage == "M15-P4" && backends.Count != 1) throw new InvalidOperationException("M15-P4 requires one backend per process.");
            string folder = stage == "M15-P4" ? "m15-p4" : stage == "M15-P3" ? "m15-p3" : "m15-p2";
            string allowed = Path.GetFullPath(Path.Combine(project, "tmp", folder)) + Path.DirectorySeparatorChar;
            string workspace = Path.GetFullPath(values.ContainsKey("workspace") ? values["workspace"] : Path.Combine(project, "tmp", folder, "work"));
            string output = Path.GetFullPath(values.ContainsKey("output") ? values["output"] : Path.Combine(project, "tmp", folder, "result.json"));
            string progress = Path.GetFullPath(values.ContainsKey("progress") ? values["progress"] : Path.Combine(project, "tmp", folder, "progress.json"));
            if (!workspace.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || !output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || !progress.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Population scheduling workspace, output and progress must remain under the stage tmp directory.");
            return new P2Options { ProjectRoot = project, WorkspaceRoot = workspace, OutputPath = output, ProgressPath = progress, Count = count, Seed = seed, Stage = stage, Backends = backends };
        }
    }

    internal sealed class P2DatasetContext
    {
        public BenchmarkDataset Dataset { get; set; }
        public List<string> InitialPersonIds { get; set; }
        public int InitialPersonCount { get; set; }
        public int InitialHouseholdCount { get; set; }
        public int InitialEventCount { get; set; }
        public List<string> Locations { get; set; }
        public Dictionary<string, string> PartitionByLocation { get; set; }
        public Dictionary<string, string> PersonPartitions { get; set; }
        public List<string> CrossPartitionPersonIds { get; set; }
        public string ParentProbeId { get; set; }
        public P2FixtureEvidence Fixture { get; set; }

        public void ReleaseLoadContainers()
        {
            if (Dataset == null) return;
            Dataset.People = null;
            Dataset.Households = null;
            Dataset.Events = null;
            Dataset.Partitions = null;
            GC.Collect();
        }
    }

    internal sealed class P2FixtureEvidence
    {
        public int CrossPartitionPersonCount { get; set; }
        public int TravelingPersonCount { get; set; }
        public int ReservedPersonCount { get; set; }
    }

    internal sealed class P2WindowEvidence
    {
        public int TargetDay { get; set; }
        public int ScannedNodes { get; set; }
        public int DueEvents { get; set; }
        public int ChangedPeople { get; set; }
        public int ChangedHouseholds { get; set; }
        public int AddedEvents { get; set; }
        public int LoadedPartitions { get; set; }
        public int WriteBackRecords { get; set; }
        public int Births { get; set; }
        public int Deaths { get; set; }
        public int Migrations { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    internal sealed class CrossReferenceEvidence
    {
        public int SamplePeople { get; set; }
        public int CrossPartitionLinks { get; set; }
        public int ParentLinksResolved { get; set; }
        public int HouseholdLinksResolved { get; set; }
        public int LoadedPartitionCount { get; set; }
    }

    internal sealed class P2BackendEvidence
    {
        public string Backend { get; set; }
        public long InitialLoadMilliseconds { get; set; }
        public long InitialCheckpointBytes { get; set; }
        public string InitialCoreDigest { get; set; }
        public List<P2WindowEvidence> Windows { get; set; }
        public CrossReferenceEvidence InitialCrossReferences { get; set; }
        public CrossReferenceEvidence FinalCrossReferences { get; set; }
        public int InitialPersonCount { get; set; }
        public int FinalPersonCount { get; set; }
        public int FinalAliveCount { get; set; }
        public int TotalBirths { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalMigrations { get; set; }
        public int LaborCandidateCount { get; set; }
        public int LaborResultCount { get; set; }
        public int OrganizationResultCount { get; set; }
        public int ChildResultCount { get; set; }
        public long IncrementalBytes { get; set; }
        public long CheckpointBytes { get; set; }
        public long RestoreMilliseconds { get; set; }
        public long PhysicalStorageBytes { get; set; }
        public long ManagedMemoryBeforeBytes { get; set; }
        public long ManagedMemoryAfterBytes { get; set; }
        public long ProcessPeakWorkingSetBytes { get; set; }
        public string CoreDigest { get; set; }
        public string EventDigest { get; set; }
        public List<string> ContractChecks { get; set; }
    }

    internal static class P4Progress
    {
        public static void Write(P2Options options, string phase, string backend)
        {
            if (options.Stage != "M15-P4") return;
            Directory.CreateDirectory(Path.GetDirectoryName(options.ProgressPath));
            var value = new JObject
            {
                ["schema_version"] = "m15.p4.progress.v1",
                ["stage"] = options.Stage,
                ["phase"] = phase,
                ["backend"] = backend,
                ["person_count"] = options.Count,
                ["updated_at_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            File.WriteAllText(options.ProgressPath, value.ToString(Formatting.None), new UTF8Encoding(false));
            Console.WriteLine("PHASE " + phase + (backend == null ? "" : " backend=" + backend));
            Console.Out.Flush();
        }
    }
}
