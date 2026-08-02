using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationBenchmark
{
    internal interface ICandidatePopulationStore : IPopulationBenchmarkStore, IDisposable
    {
        string BackendName { get; }
        long GetPhysicalStorageBytes();
        int GetPhysicalFileCount();
        void ValidatePhysicalStore(int people, int households, int events);
        void BeginBulkLoad();
        void EndBulkLoad();
        void EnableSettlementOverlay();
        long CreateIncrementalSaveFile(string path);
        long CreateCheckpointDirectory(string path);
        void RestoreCheckpointDirectory(string path);
    }

    internal static class CandidateStoreFactory
    {
        public static ICandidatePopulationStore Create(string backend, string root)
        {
            if (backend == "sqlite") return new SqliteCandidateStore(root);
            if (backend == "binary") return new BinaryCandidateStore(root);
            if (backend == "hybrid") return new HybridCandidateStore(root);
            throw new ArgumentOutOfRangeException("backend", backend, "Unknown population backend.");
        }
    }

    internal abstract class CandidateStoreBase : ICandidatePopulationStore
    {
        protected readonly InMemoryPopulationBenchmarkStore Inner = new InMemoryPopulationBenchmarkStore();
        protected readonly string Root;
        private bool _settlementOverlayEnabled;
        public abstract string BackendName { get; }

        protected CandidateStoreBase(string root)
        {
            Root = Path.GetFullPath(root);
            Directory.CreateDirectory(Root);
        }

        public abstract void BatchWrite(PopulationPartition partition);
        public virtual void BeginBulkLoad() { }
        public virtual void EndBulkLoad() { }
        public void EnableSettlementOverlay() { _settlementOverlayEnabled = true; }
        public virtual PermanentPersonRecord GetPerson(string personId) { return Inner.GetPerson(personId); }
        public virtual HouseholdRecord GetHousehold(string householdId) { return Inner.GetHousehold(householdId); }
        public virtual QueryResult QueryPeople(PopulationQuery query) { return Inner.QueryPeople(query); }
        public virtual QueryResult QueryChildren(string parentId) { return Inner.QueryChildren(parentId); }
        public virtual void LoadPartition(string partitionId) { Inner.LoadPartition(partitionId); }
        public virtual void UnloadPartition(string partitionId) { Inner.UnloadPartition(partitionId); }
        public virtual DueReadResult ReadDueEvents(int absoluteDay) { return Inner.ReadDueEvents(absoluteDay); }
        public virtual void ApplyChangeBatch(PopulationChangeBatch batch)
        {
            string path = Path.Combine(Root, "p2-mutations.jsonl");
            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var text = new StreamWriter(stream, new UTF8Encoding(false), 65536))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.None })
            {
                JsonSerializer.CreateDefault().Serialize(writer, batch);
                writer.Flush();
                text.WriteLine();
            }
            Inner.ApplyChangeBatch(batch);
        }
        public virtual void CommitDueChanges(IEnumerable<DueEventRecord> events) { Inner.CommitDueChanges(events); }

        public virtual byte[] CreateIncrementalSave()
        {
            return WrapBytes("M15P1-INCREMENTAL-" + BackendName, Inner.CreateIncrementalSave());
        }

        public virtual long CreateIncrementalSaveFile(string path)
        {
            return Inner.WriteIncrementalSave(path);
        }

        public byte[] CreateCheckpoint()
        {
            byte[] inner = Inner.CreateCheckpoint();
            BeforeFileCapture();
            try
            {
                using (var memory = new MemoryStream())
                using (var writer = new BinaryWriter(memory, Encoding.UTF8))
                {
                    writer.Write("M15P1-CHECKPOINT-V1");
                    writer.Write(BackendName);
                    writer.Write(inner.Length);
                    writer.Write(inner);
                    List<string> files = Directory.GetFiles(Root, "*", SearchOption.AllDirectories)
                        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
                    writer.Write(files.Count);
                    foreach (string file in files)
                    {
                        string relative = file.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        byte[] bytes = File.ReadAllBytes(file);
                        writer.Write(relative);
                        writer.Write(bytes.Length);
                        writer.Write(bytes);
                    }
                    writer.Flush();
                    return memory.ToArray();
                }
            }
            finally
            {
                AfterFileCapture();
            }
        }

        public void RestoreCheckpoint(byte[] checkpoint)
        {
            byte[] inner;
            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            using (var memory = new MemoryStream(checkpoint, false))
            using (var reader = new BinaryReader(memory, Encoding.UTF8))
            {
                if (reader.ReadString() != "M15P1-CHECKPOINT-V1" || reader.ReadString() != BackendName)
                {
                    throw new InvalidDataException("Checkpoint backend or format mismatch.");
                }
                inner = reader.ReadBytes(reader.ReadInt32());
                int fileCount = reader.ReadInt32();
                for (int index = 0; index < fileCount; index++)
                {
                    string relative = reader.ReadString();
                    files.Add(relative, reader.ReadBytes(reader.ReadInt32()));
                }
                if (memory.Position != memory.Length) throw new InvalidDataException("Checkpoint has trailing bytes.");
            }

            BeforeFileCapture();
            try
            {
                foreach (string existing in Directory.GetFiles(Root, "*", SearchOption.AllDirectories)) File.Delete(existing);
                string safeRoot = Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (KeyValuePair<string, byte[]> file in files)
                {
                    string path = Path.GetFullPath(Path.Combine(Root, file.Key));
                    if (!path.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Checkpoint contains an unsafe path.");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, file.Value);
                }
                Inner.RestoreCheckpoint(inner);
            }
            finally
            {
                AfterCheckpointRestore();
            }
        }

        public long CreateCheckpointDirectory(string path)
        {
            string checkpointRoot = Path.GetFullPath(path);
            if (Directory.Exists(checkpointRoot)) Directory.Delete(checkpointRoot, true);
            Directory.CreateDirectory(checkpointRoot);
            string innerPath = Path.Combine(checkpointRoot, "inner.json");
            Inner.WriteCheckpoint(innerPath);
            File.WriteAllText(Path.Combine(checkpointRoot, "manifest.txt"), "M15P4-CHECKPOINT-V1\n" + BackendName + "\n", new UTF8Encoding(false));
            string physicalRoot = Path.Combine(checkpointRoot, "physical");
            Directory.CreateDirectory(physicalRoot);
            BeforeFileCapture();
            try { CopyDirectory(Root, physicalRoot); }
            finally { AfterFileCapture(); }
            return Directory.GetFiles(checkpointRoot, "*", SearchOption.AllDirectories).Sum(value => new FileInfo(value).Length);
        }

        public void RestoreCheckpointDirectory(string path)
        {
            string checkpointRoot = Path.GetFullPath(path);
            string[] manifest = File.ReadAllLines(Path.Combine(checkpointRoot, "manifest.txt"), Encoding.UTF8);
            if (manifest.Length < 2 || manifest[0] != "M15P4-CHECKPOINT-V1" || manifest[1] != BackendName)
                throw new InvalidDataException("Streaming checkpoint backend or format mismatch.");
            string physicalRoot = Path.Combine(checkpointRoot, "physical");
            BeforeFileCapture();
            try
            {
                foreach (string file in Directory.GetFiles(Root, "*", SearchOption.AllDirectories)) File.Delete(file);
                foreach (string directory in Directory.GetDirectories(Root).OrderByDescending(value => value.Length))
                    if (Directory.Exists(directory)) Directory.Delete(directory, true);
                CopyDirectory(physicalRoot, Root);
                Inner.RestoreCheckpoint(Path.Combine(checkpointRoot, "inner.json"));
            }
            finally { AfterCheckpointRestore(); }
        }

        public StoreDigest ComputeDigest() { return Inner.ComputeDigest(); }
        public IReadOnlyList<AttentionPersonView> ExpandAttention(IEnumerable<string> personIds) { return Inner.ExpandAttention(personIds); }
        public void ReleaseAttention(IEnumerable<string> personIds) { Inner.ReleaseAttention(personIds); }
        public long GetPhysicalStorageBytes() { FlushPhysicalStore(); return Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Sum(value => new FileInfo(value).Length); }
        public int GetPhysicalFileCount() { return Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Length; }
        public abstract void ValidatePhysicalStore(int people, int households, int events);
        protected virtual void BeforeFileCapture() { }
        protected virtual void AfterFileCapture() { }
        protected virtual void AfterCheckpointRestore() { }
        protected virtual void FlushPhysicalStore() { }
        protected bool HasP2Overlay { get { return File.Exists(Path.Combine(Root, "p2-mutations.jsonl")); } }
        protected bool UseInner { get { return _settlementOverlayEnabled || HasP2Overlay; } }
        public virtual void Dispose() { }

        protected static byte[] WrapBytes(string marker, byte[] payload)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory, Encoding.UTF8))
            {
                writer.Write(marker);
                writer.Write(payload.Length);
                writer.Write(payload);
                writer.Flush();
                return memory.ToArray();
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (string directory in Directory.GetDirectories(source).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    internal abstract class RelationalCandidateStore : CandidateStoreBase
    {
        private readonly string _databasePath;
        private SqliteTransaction _bulkTransaction;
        private Dictionary<string, SqliteCommand> _bulkCommands;
        protected SqliteConnection Connection;

        protected RelationalCandidateStore(string root) : base(root)
        {
            _databasePath = Path.Combine(Root, "population.sqlite");
            OpenConnection();
        }

        protected abstract bool StoresPersonPayload { get; }
        protected abstract void PersistPeople(PopulationPartition partition, SqliteTransaction transaction);
        protected abstract PermanentPersonRecord ReadPerson(SqliteDataReader reader);

        public override void BatchWrite(PopulationPartition partition)
        {
            if (_bulkTransaction != null)
            {
                WritePartition(partition, _bulkTransaction);
            }
            else
            {
                using (SqliteTransaction transaction = Connection.BeginTransaction())
                {
                    WritePartition(partition, transaction);
                    transaction.Commit();
                }
            }
            Inner.BatchWrite(partition);
        }

        public override void BeginBulkLoad()
        {
            if (_bulkTransaction != null) throw new InvalidOperationException("Bulk load is already active.");
            DropSecondaryIndexes();
            _bulkCommands = new Dictionary<string, SqliteCommand>(StringComparer.Ordinal);
            _bulkTransaction = Connection.BeginTransaction();
        }

        public override void EndBulkLoad()
        {
            if (_bulkTransaction == null) throw new InvalidOperationException("Bulk load is not active.");
            foreach (SqliteCommand command in _bulkCommands.Values) command.Dispose();
            _bulkCommands.Clear();
            _bulkCommands = null;
            SqliteTransaction transaction = _bulkTransaction;
            _bulkTransaction = null;
            transaction.Commit();
            transaction.Dispose();
            CreateSecondaryIndexes();
        }

        private void WritePartition(PopulationPartition partition, SqliteTransaction transaction)
        {
            PersistPeople(partition, transaction);
            foreach (HouseholdRecord household in partition.Households)
            {
                Execute(transaction,
                    "INSERT INTO households(household_id,location_id,payload) VALUES(@id,@location,@payload)",
                    "@id", household.HouseholdId, "@location", household.LocationId, "@payload", JsonConvert.SerializeObject(household, Formatting.None));
            }
            foreach (DueEventRecord dueEvent in partition.Events)
            {
                Execute(transaction,
                    "INSERT INTO events(event_id,person_id,due_day,completed,payload) VALUES(@id,@person,@day,0,@payload)",
                    "@id", dueEvent.EventId, "@person", dueEvent.PersonId, "@day", dueEvent.DueDay, "@payload", JsonConvert.SerializeObject(dueEvent, Formatting.None));
            }
            Execute(transaction, "INSERT INTO loaded_partitions(partition_id) VALUES(@id)", "@id", partition.PartitionId);
        }

        protected void InsertPersonIndex(SqliteTransaction transaction, PermanentPersonRecord person, string partitionId, string payload, string fileName, long offset)
        {
            Execute(transaction,
                "INSERT INTO people(person_id,payload,location_id,birth_location_id,household_id,occupation,organization_id,health_summary,labor_summary,father_id,mother_id,alive,available,partition_id,file_name,file_offset) " +
                "VALUES(@id,@payload,@location,@birth,@household,@occupation,@organization,@health,@labor,@father,@mother,@alive,@available,@partition,@file,@offset)",
                "@id", person.PersonId, "@payload", (object)payload ?? DBNull.Value, "@location", person.CurrentLocationId,
                "@birth", person.BirthLocationId, "@household", person.HouseholdId, "@occupation", person.Occupation,
                "@organization", person.PrimaryOrganizationId, "@health", person.HealthSummary, "@labor", person.LaborSummary,
                "@father", (object)person.FatherId ?? DBNull.Value, "@mother", (object)person.MotherId ?? DBNull.Value,
                "@alive", person.Alive ? 1 : 0, "@available", person.Available ? 1 : 0,
                "@partition", partitionId, "@file", (object)fileName ?? DBNull.Value, "@offset", offset);
        }

        public override PermanentPersonRecord GetPerson(string personId)
        {
            if (UseInner) return base.GetPerson(personId);
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT person_id,payload,file_name,file_offset FROM people WHERE person_id=@id";
                Add(command, "@id", personId);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new KeyNotFoundException("Unknown person: " + personId);
                    return ReadPerson(reader);
                }
            }
        }

        public override HouseholdRecord GetHousehold(string householdId)
        {
            if (UseInner) return base.GetHousehold(householdId);
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT payload FROM households WHERE household_id=@id";
                Add(command, "@id", householdId);
                object payload = command.ExecuteScalar();
                if (payload == null) throw new KeyNotFoundException("Unknown household: " + householdId);
                return JsonConvert.DeserializeObject<HouseholdRecord>((string)payload);
            }
        }

        public override QueryResult QueryPeople(PopulationQuery query)
        {
            if (UseInner) return base.QueryPeople(query);
            var clauses = new List<string>();
            using (SqliteCommand command = Connection.CreateCommand())
            {
                if (!string.IsNullOrEmpty(query.LocationId)) { clauses.Add("location_id=@location"); Add(command, "@location", query.LocationId); }
                if (!string.IsNullOrEmpty(query.Occupation)) { clauses.Add("occupation=@occupation"); Add(command, "@occupation", query.Occupation); }
                if (!string.IsNullOrEmpty(query.OrganizationId)) { clauses.Add("organization_id=@organization"); Add(command, "@organization", query.OrganizationId); }
                if (!string.IsNullOrEmpty(query.HealthSummary)) { clauses.Add("health_summary=@health"); Add(command, "@health", query.HealthSummary); }
                if (!string.IsNullOrEmpty(query.LaborSummary)) { clauses.Add("labor_summary=@labor"); Add(command, "@labor", query.LaborSummary); }
                if (query.Alive.HasValue) { clauses.Add("alive=@alive"); Add(command, "@alive", query.Alive.Value ? 1 : 0); }
                if (query.Available.HasValue) { clauses.Add("available=@available"); Add(command, "@available", query.Available.Value ? 1 : 0); }
                command.CommandText = "SELECT person_id,payload,file_name,file_offset FROM people" + (clauses.Count == 0 ? "" : " WHERE " + string.Join(" AND ", clauses)) + " ORDER BY person_id";
                var people = new List<PermanentPersonRecord>();
                using (SqliteDataReader reader = command.ExecuteReader()) while (reader.Read()) people.Add(ReadPerson(reader));
                return new QueryResult { CandidateCount = people.Count, People = people };
            }
        }

        public override QueryResult QueryChildren(string parentId)
        {
            if (UseInner) return base.QueryChildren(parentId);
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT person_id,payload,file_name,file_offset FROM people WHERE father_id=@id OR mother_id=@id ORDER BY person_id";
                Add(command, "@id", parentId);
                var people = new List<PermanentPersonRecord>();
                using (SqliteDataReader reader = command.ExecuteReader()) while (reader.Read()) people.Add(ReadPerson(reader));
                return new QueryResult { CandidateCount = people.Count, People = people };
            }
        }

        public override void LoadPartition(string partitionId)
        {
            if (UseInner) { Inner.LoadPartition(partitionId); return; }
            Execute(null, "INSERT OR IGNORE INTO loaded_partitions(partition_id) VALUES(@id)", "@id", partitionId);
            Inner.LoadPartition(partitionId);
        }

        public override void UnloadPartition(string partitionId)
        {
            if (UseInner) { Inner.UnloadPartition(partitionId); return; }
            Execute(null, "DELETE FROM loaded_partitions WHERE partition_id=@id", "@id", partitionId);
            Inner.UnloadPartition(partitionId);
        }

        public override DueReadResult ReadDueEvents(int absoluteDay)
        {
            if (UseInner) return base.ReadDueEvents(absoluteDay);
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT payload FROM events WHERE completed=0 AND due_day<=@day ORDER BY due_day,event_id";
                Add(command, "@day", absoluteDay);
                var events = new List<DueEventRecord>();
                using (SqliteDataReader reader = command.ExecuteReader()) while (reader.Read()) events.Add(JsonConvert.DeserializeObject<DueEventRecord>(reader.GetString(0)));
                return new DueReadResult { ScannedNodeCount = events.Count, Events = events };
            }
        }

        public override void CommitDueChanges(IEnumerable<DueEventRecord> events)
        {
            List<DueEventRecord> batch = events.ToList();
            using (SqliteTransaction transaction = Connection.BeginTransaction())
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "UPDATE events SET completed=1 WHERE event_id=@id";
                Add(command, "@id", string.Empty);
                foreach (DueEventRecord dueEvent in batch)
                {
                    command.Parameters["@id"].Value = dueEvent.EventId;
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            Inner.CommitDueChanges(batch);
        }

        public override void ValidatePhysicalStore(int people, int households, int events)
        {
            if (Count("people") != people || Count("households") != households || Count("events") != events)
            {
                throw new InvalidDataException(BackendName + " physical row counts do not match the generated dataset.");
            }
            int payloadRows = ScalarInt("SELECT COUNT(*) FROM people WHERE payload IS NOT NULL");
            if ((StoresPersonPayload && payloadRows != people) || (!StoresPersonPayload && payloadRows != 0))
            {
                throw new InvalidDataException(BackendName + " person payload placement does not match its candidate definition.");
            }
        }

        protected override void FlushPhysicalStore() { Execute(null, "PRAGMA optimize"); }
        protected override void BeforeFileCapture()
        {
            if (_bulkTransaction != null) throw new InvalidOperationException("Cannot capture a checkpoint during bulk load.");
            if (Connection != null) { Connection.Close(); Connection.Dispose(); Connection = null; }
        }
        protected override void AfterFileCapture() { OpenConnection(); }
        protected override void AfterCheckpointRestore() { OpenConnection(); }
        public override void Dispose()
        {
            if (_bulkCommands != null) foreach (SqliteCommand command in _bulkCommands.Values) command.Dispose();
            if (_bulkTransaction != null) { _bulkTransaction.Dispose(); _bulkTransaction = null; }
            if (Connection != null) { Connection.Dispose(); Connection = null; }
        }

        protected int Count(string table) { return ScalarInt("SELECT COUNT(*) FROM " + table); }
        private int ScalarInt(string sql)
        {
            using (SqliteCommand command = Connection.CreateCommand()) { command.CommandText = sql; return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture); }
        }

        protected void Execute(SqliteTransaction transaction, string sql, params object[] parameters)
        {
            if (_bulkTransaction != null && ReferenceEquals(transaction, _bulkTransaction))
            {
                SqliteCommand cached;
                if (!_bulkCommands.TryGetValue(sql, out cached))
                {
                    cached = Connection.CreateCommand();
                    cached.Transaction = transaction;
                    cached.CommandText = sql;
                    for (int index = 0; index < parameters.Length; index += 2) Add(cached, (string)parameters[index], parameters[index + 1]);
                    _bulkCommands.Add(sql, cached);
                }
                else
                {
                    for (int index = 0; index < parameters.Length; index += 2)
                        cached.Parameters[(string)parameters[index]].Value = parameters[index + 1] ?? DBNull.Value;
                }
                cached.ExecuteNonQuery();
                return;
            }
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                for (int index = 0; index < parameters.Length; index += 2) Add(command, (string)parameters[index], parameters[index + 1]);
                command.ExecuteNonQuery();
            }
        }

        private static void Add(SqliteCommand command, string name, object value)
        {
            SqliteParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private void OpenConnection()
        {
            Connection = new SqliteConnection("URI=file:" + _databasePath);
            Connection.Open();
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText =
                    "PRAGMA journal_mode=DELETE; PRAGMA synchronous=NORMAL;" +
                    "CREATE TABLE IF NOT EXISTS people(person_id TEXT PRIMARY KEY,payload TEXT,location_id TEXT NOT NULL,birth_location_id TEXT NOT NULL,household_id TEXT NOT NULL,occupation TEXT NOT NULL,organization_id TEXT NOT NULL,health_summary TEXT NOT NULL,labor_summary TEXT NOT NULL,father_id TEXT,mother_id TEXT,alive INTEGER NOT NULL,available INTEGER NOT NULL,partition_id TEXT NOT NULL,file_name TEXT,file_offset INTEGER NOT NULL DEFAULT 0);" +
                    "CREATE TABLE IF NOT EXISTS households(household_id TEXT PRIMARY KEY,location_id TEXT NOT NULL,payload TEXT NOT NULL);" +
                    "CREATE TABLE IF NOT EXISTS events(event_id TEXT PRIMARY KEY,person_id TEXT NOT NULL,due_day INTEGER NOT NULL,completed INTEGER NOT NULL,payload TEXT NOT NULL);" +
                    "CREATE TABLE IF NOT EXISTS loaded_partitions(partition_id TEXT PRIMARY KEY);";
                command.ExecuteNonQuery();
            }
            CreateSecondaryIndexes();
        }

        private void CreateSecondaryIndexes()
        {
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText =
                    "CREATE INDEX IF NOT EXISTS ix_people_location ON people(location_id,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_occupation ON people(occupation,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_organization ON people(organization_id,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_health_labor ON people(health_summary,labor_summary,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_father ON people(father_id,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_mother ON people(mother_id,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_people_status ON people(alive,available,person_id);" +
                    "CREATE INDEX IF NOT EXISTS ix_events_due ON events(completed,due_day,event_id);";
                command.ExecuteNonQuery();
            }
        }

        private void DropSecondaryIndexes()
        {
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText =
                    "DROP INDEX IF EXISTS ix_people_location;DROP INDEX IF EXISTS ix_people_occupation;" +
                    "DROP INDEX IF EXISTS ix_people_organization;DROP INDEX IF EXISTS ix_people_health_labor;" +
                    "DROP INDEX IF EXISTS ix_people_father;DROP INDEX IF EXISTS ix_people_mother;" +
                    "DROP INDEX IF EXISTS ix_people_status;DROP INDEX IF EXISTS ix_events_due;";
                command.ExecuteNonQuery();
            }
        }
    }

    internal sealed class SqliteCandidateStore : RelationalCandidateStore
    {
        public override string BackendName { get { return "sqlite"; } }
        protected override bool StoresPersonPayload { get { return true; } }
        public SqliteCandidateStore(string root) : base(root) { }

        protected override void PersistPeople(PopulationPartition partition, SqliteTransaction transaction)
        {
            foreach (PermanentPersonRecord person in partition.People)
                InsertPersonIndex(transaction, person, partition.PartitionId, JsonConvert.SerializeObject(person, Formatting.None), null, 0L);
        }

        protected override PermanentPersonRecord ReadPerson(SqliteDataReader reader)
        {
            return JsonConvert.DeserializeObject<PermanentPersonRecord>(reader.GetString(1));
        }
    }

    internal sealed class HybridCandidateStore : RelationalCandidateStore
    {
        private const int Magic = 0x4D485031;
        public override string BackendName { get { return "hybrid"; } }
        protected override bool StoresPersonPayload { get { return false; } }
        public HybridCandidateStore(string root) : base(root) { Directory.CreateDirectory(Path.Combine(Root, "people")); }

        protected override void PersistPeople(PopulationPartition partition, SqliteTransaction transaction)
        {
            string relative = Path.Combine("people", partition.PartitionId + ".mhp");
            string path = Path.Combine(Root, relative);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Magic); writer.Write(partition.PartitionId); writer.Write(partition.StableRegionId); writer.Write(partition.Version);
                writer.Write(partition.People.Count);
                foreach (PermanentPersonRecord person in partition.People.OrderBy(value => value.PersonId, StringComparer.Ordinal))
                {
                    long offset = stream.Position;
                    PopulationBinaryCodec.WritePerson(writer, person);
                    InsertPersonIndex(transaction, person, partition.PartitionId, null, relative, offset);
                }
            }
        }

        protected override PermanentPersonRecord ReadPerson(SqliteDataReader reader)
        {
            string relative = reader.GetString(2);
            long offset = reader.GetInt64(3);
            string path = Path.Combine(Root, relative);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var binary = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = offset;
                return PopulationBinaryCodec.ReadPerson(binary);
            }
        }

        public override void LoadPartition(string partitionId)
        {
            if (UseInner) { base.LoadPartition(partitionId); return; }
            using (SqliteCommand command = Connection.CreateCommand())
            {
                command.CommandText = "SELECT file_name FROM people WHERE partition_id=@id LIMIT 1";
                SqliteParameter parameter = command.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = partitionId; command.Parameters.Add(parameter);
                object value = command.ExecuteScalar();
                if (value == null || !File.Exists(Path.Combine(Root, (string)value))) throw new InvalidDataException("Hybrid partition file is missing: " + partitionId);
            }
            base.LoadPartition(partitionId);
        }
    }

    internal sealed class BinaryCandidateStore : CandidateStoreBase
    {
        private const int Magic = 0x4D484231;
        private readonly Dictionary<string, BinaryAddress> _people = new Dictionary<string, BinaryAddress>(StringComparer.Ordinal);
        private readonly Dictionary<string, BinaryAddress> _households = new Dictionary<string, BinaryAddress>(StringComparer.Ordinal);
        private readonly Dictionary<string, BinaryAddress> _events = new Dictionary<string, BinaryAddress>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _partitions = new Dictionary<string, string>(StringComparer.Ordinal);
        public override string BackendName { get { return "binary"; } }

        public BinaryCandidateStore(string root) : base(root) { Directory.CreateDirectory(Path.Combine(Root, "partitions")); }

        public override void BatchWrite(PopulationPartition partition)
        {
            string path = Path.Combine(Root, "partitions", partition.PartitionId + ".mhb");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Magic); writer.Write(partition.PartitionId); writer.Write(partition.StableRegionId); writer.Write(partition.Version);
                writer.Write(partition.People.Count);
                foreach (PermanentPersonRecord person in partition.People.OrderBy(value => value.PersonId, StringComparer.Ordinal))
                {
                    long offset = stream.Position; PopulationBinaryCodec.WritePerson(writer, person); _people.Add(person.PersonId, new BinaryAddress(path, offset));
                }
                writer.Write(partition.Households.Count);
                foreach (HouseholdRecord household in partition.Households.OrderBy(value => value.HouseholdId, StringComparer.Ordinal))
                {
                    long offset = stream.Position; PopulationBinaryCodec.WriteHousehold(writer, household); _households.Add(household.HouseholdId, new BinaryAddress(path, offset));
                }
                writer.Write(partition.Events.Count);
                foreach (DueEventRecord dueEvent in partition.Events.OrderBy(value => value.EventId, StringComparer.Ordinal))
                {
                    long offset = stream.Position; PopulationBinaryCodec.WriteEvent(writer, dueEvent); _events.Add(dueEvent.EventId, new BinaryAddress(path, offset));
                }
            }
            _partitions.Add(partition.PartitionId, path);
            Inner.BatchWrite(partition);
        }

        public override PermanentPersonRecord GetPerson(string personId) { return UseInner ? base.GetPerson(personId) : Read(_people, personId, PopulationBinaryCodec.ReadPerson); }
        public override HouseholdRecord GetHousehold(string householdId) { return UseInner ? base.GetHousehold(householdId) : Read(_households, householdId, PopulationBinaryCodec.ReadHousehold); }
        public override QueryResult QueryPeople(PopulationQuery query)
        {
            if (UseInner) return base.QueryPeople(query);
            QueryResult indexed = Inner.QueryPeople(query);
            return new QueryResult { CandidateCount = indexed.CandidateCount, People = indexed.People.Select(value => GetPerson(value.PersonId)).ToList() };
        }
        public override QueryResult QueryChildren(string parentId)
        {
            if (UseInner) return base.QueryChildren(parentId);
            QueryResult indexed = Inner.QueryChildren(parentId);
            return new QueryResult { CandidateCount = indexed.CandidateCount, People = indexed.People.Select(value => GetPerson(value.PersonId)).ToList() };
        }
        public override DueReadResult ReadDueEvents(int absoluteDay)
        {
            if (UseInner) return base.ReadDueEvents(absoluteDay);
            DueReadResult indexed = Inner.ReadDueEvents(absoluteDay);
            return new DueReadResult { ScannedNodeCount = indexed.ScannedNodeCount, Events = indexed.Events.Select(value => Read(_events, value.EventId, PopulationBinaryCodec.ReadEvent)).ToList() };
        }
        public override void LoadPartition(string partitionId)
        {
            if (UseInner) { Inner.LoadPartition(partitionId); return; }
            string path;
            if (!_partitions.TryGetValue(partitionId, out path) || !File.Exists(path)) throw new InvalidDataException("Binary partition file is missing: " + partitionId);
            using (var reader = new BinaryReader(File.OpenRead(path), Encoding.UTF8)) if (reader.ReadInt32() != Magic || reader.ReadString() != partitionId) throw new InvalidDataException("Binary partition header mismatch.");
            Inner.LoadPartition(partitionId);
        }
        public override void ValidatePhysicalStore(int people, int households, int events)
        {
            if (_people.Count != people || _households.Count != households || _events.Count != events) throw new InvalidDataException("Binary physical indexes do not match the generated dataset.");
        }
        protected override void AfterCheckpointRestore()
        {
            // P2/P4 mutations are restored into Inner and all reads use that authoritative overlay.
            // The immutable partition files keep the same paths and offsets, so rebuilding the
            // physical address dictionaries would only duplicate a full scan.
            if (!HasP2Overlay) RebuildIndexes();
        }

        private void RebuildIndexes()
        {
            _people.Clear(); _households.Clear(); _events.Clear(); _partitions.Clear();
            foreach (string path in Directory.GetFiles(Path.Combine(Root, "partitions"), "*.mhb").OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != Magic) throw new InvalidDataException("Binary partition magic mismatch.");
                    string partitionId = reader.ReadString(); reader.ReadString(); reader.ReadInt32(); _partitions.Add(partitionId, path);
                    int personCount = reader.ReadInt32();
                    for (int index = 0; index < personCount; index++) { long offset = stream.Position; PermanentPersonRecord item = PopulationBinaryCodec.ReadPerson(reader); _people.Add(item.PersonId, new BinaryAddress(path, offset)); }
                    int householdCount = reader.ReadInt32();
                    for (int index = 0; index < householdCount; index++) { long offset = stream.Position; HouseholdRecord item = PopulationBinaryCodec.ReadHousehold(reader); _households.Add(item.HouseholdId, new BinaryAddress(path, offset)); }
                    int eventCount = reader.ReadInt32();
                    for (int index = 0; index < eventCount; index++) { long offset = stream.Position; DueEventRecord item = PopulationBinaryCodec.ReadEvent(reader); _events.Add(item.EventId, new BinaryAddress(path, offset)); }
                    if (stream.Position != stream.Length) throw new InvalidDataException("Binary partition has trailing bytes: " + path);
                }
            }
        }

        private static T Read<T>(IDictionary<string, BinaryAddress> addresses, string id, Func<BinaryReader, T> reader)
        {
            BinaryAddress address;
            if (!addresses.TryGetValue(id, out address)) throw new KeyNotFoundException("Unknown binary record: " + id);
            using (var stream = new FileStream(address.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var binary = new BinaryReader(stream, Encoding.UTF8)) { stream.Position = address.Offset; return reader(binary); }
        }
    }

    internal struct BinaryAddress
    {
        public readonly string Path;
        public readonly long Offset;
        public BinaryAddress(string path, long offset) { Path = path; Offset = offset; }
    }

    internal static class PopulationBinaryCodec
    {
        public static void WritePerson(BinaryWriter writer, PermanentPersonRecord value)
        {
            writer.Write(value.PersonId); writer.Write(value.NameIndex); writer.Write(value.Gender); writer.Write(value.BirthDay); writer.Write(value.Alive);
            WriteNullableInt(writer, value.DeathDay); writer.Write(value.BirthLocationId); writer.Write(value.CurrentLocationId); writer.Write(value.HouseholdId);
            WriteNullableString(writer, value.FatherId); WriteNullableString(writer, value.MotherId); writer.Write(value.Occupation); writer.Write(value.LaborSummary);
            writer.Write(value.HealthSummary); writer.Write(value.PrimaryOrganizationId); writer.Write(value.NextDueDay); writer.Write(value.NextDueReason);
            writer.Write(value.RecordVersion); writer.Write(value.Available); writer.Write(value.SourceLayer); writer.Write(value.SourcePopulationAdminUnitId);
        }

        public static PermanentPersonRecord ReadPerson(BinaryReader reader)
        {
            return new PermanentPersonRecord
            {
                PersonId = reader.ReadString(), NameIndex = reader.ReadInt32(), Gender = reader.ReadString(), BirthDay = reader.ReadInt32(), Alive = reader.ReadBoolean(),
                DeathDay = ReadNullableInt(reader), BirthLocationId = reader.ReadString(), CurrentLocationId = reader.ReadString(), HouseholdId = reader.ReadString(),
                FatherId = ReadNullableString(reader), MotherId = ReadNullableString(reader), Occupation = reader.ReadString(), LaborSummary = reader.ReadString(),
                HealthSummary = reader.ReadString(), PrimaryOrganizationId = reader.ReadString(), NextDueDay = reader.ReadInt32(), NextDueReason = reader.ReadString(),
                RecordVersion = reader.ReadInt32(), Available = reader.ReadBoolean(), SourceLayer = reader.ReadString(), SourcePopulationAdminUnitId = reader.ReadString()
            };
        }

        public static void WriteHousehold(BinaryWriter writer, HouseholdRecord value)
        {
            writer.Write(value.HouseholdId); writer.Write(value.LocationId); writer.Write(value.RecordVersion); writer.Write(value.SourceLayer); writer.Write(value.MemberIds.Count);
            foreach (string member in value.MemberIds) writer.Write(member);
        }
        public static HouseholdRecord ReadHousehold(BinaryReader reader)
        {
            var value = new HouseholdRecord { HouseholdId = reader.ReadString(), LocationId = reader.ReadString(), RecordVersion = reader.ReadInt32(), SourceLayer = reader.ReadString(), MemberIds = new List<string>() };
            int count = reader.ReadInt32(); for (int index = 0; index < count; index++) value.MemberIds.Add(reader.ReadString()); return value;
        }
        public static void WriteEvent(BinaryWriter writer, DueEventRecord value)
        {
            writer.Write(value.EventId); writer.Write(value.PersonId); writer.Write(value.DueDay); writer.Write(value.Reason); writer.Write(value.RuleVersion); writer.Write(value.ActionCoordinate); writer.Write(value.SourceLayer);
        }
        public static DueEventRecord ReadEvent(BinaryReader reader)
        {
            return new DueEventRecord { EventId = reader.ReadString(), PersonId = reader.ReadString(), DueDay = reader.ReadInt32(), Reason = reader.ReadString(), RuleVersion = reader.ReadString(), ActionCoordinate = reader.ReadString(), SourceLayer = reader.ReadString() };
        }
        private static void WriteNullableString(BinaryWriter writer, string value) { writer.Write(value != null); if (value != null) writer.Write(value); }
        private static string ReadNullableString(BinaryReader reader) { return reader.ReadBoolean() ? reader.ReadString() : null; }
        private static void WriteNullableInt(BinaryWriter writer, int? value) { writer.Write(value.HasValue); if (value.HasValue) writer.Write(value.Value); }
        private static int? ReadNullableInt(BinaryReader reader) { return reader.ReadBoolean() ? (int?)reader.ReadInt32() : null; }
    }
}
