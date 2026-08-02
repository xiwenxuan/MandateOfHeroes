using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationAllHot
{
    internal static class PopulationAllHotProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                AllHotOptions options = AllHotOptions.Parse(args);
                if (options.SelfTest)
                {
                    AllHotSelfTests.Run(options.OutputPath);
                    return 0;
                }

                long before;
                long appendMilliseconds;
                AllHotDiskEvidence disk;
                using (var store = AllHotStore.OpenOrCreate(options.WorkspacePath, options.Seed, options.BatchRecords, options.ProgressPath))
                {
                    before = store.PersonCount;
                    appendMilliseconds = store.AdvanceTo(options.TargetLivingPopulation);
                    disk = store.BuildDiskEvidence();
                }

                ResidentHotEvidence resident = options.SkipResident
                    ? ResidentHotEvidence.Skipped()
                    : ResidentHotLoader.TryLoad(options.WorkspacePath, options.TargetLivingPopulation, disk.RollingDigest);
                var evidence = new AllHotRunEvidence
                {
                    SchemaVersion = "m15.p5.all-hot.evidence.v1",
                    Status = "passed",
                    Seed = options.Seed,
                    PreviousLivingPopulation = before,
                    CurrentLivingPopulation = options.TargetLivingPopulation,
                    CumulativePersonCount = options.TargetLivingPopulation,
                    DeceasedColdArchiveCount = 0,
                    AppendedPeople = options.TargetLivingPopulation - before,
                    AppendMilliseconds = appendMilliseconds,
                    CoreBytes = disk.CoreBytes,
                    HotBytes = disk.HotBytes,
                    TotalBytes = disk.CoreBytes + disk.HotBytes,
                    BytesPerLivingPerson = AllHotFormat.CoreRecordBytes + AllHotFormat.HotRecordBytes,
                    RollingDigest = disk.RollingDigest,
                    DirectQueryPassed = disk.DirectQueryPassed,
                    Resident = resident
                };
                WriteJson(options.OutputPath, evidence);
                Console.WriteLine(
                    "RESULT m15-p5-all-hot=passed living={0} appended={1} resident={2} bytes={3}",
                    evidence.CurrentLivingPopulation,
                    evidence.AppendedPeople,
                    evidence.Resident.Status,
                    evidence.TotalBytes);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        internal static void WriteJson(string path, object value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented), new UTF8Encoding(false));
        }
    }

    internal static class AllHotFormat
    {
        public const string SchemaVersion = "m15.p5.all-hot.v1";
        public const int HeaderBytes = 20;
        public const int CoreRecordBytes = 41;
        public const int HotRecordBytes = 45;
        public static readonly byte[] CoreMagic = Encoding.ASCII.GetBytes("M15P5AC1");
        public static readonly byte[] HotMagic = Encoding.ASCII.GetBytes("M15P5AH1");
    }

    internal sealed class AllHotManifest
    {
        public string SchemaVersion { get; set; }
        public long Seed { get; set; }
        public long PersonCount { get; set; }
        public string RollingDigest { get; set; }
    }

    internal sealed class AllHotDiskEvidence
    {
        public long CoreBytes { get; set; }
        public long HotBytes { get; set; }
        public string RollingDigest { get; set; }
        public bool DirectQueryPassed { get; set; }
    }

    internal sealed class ResidentHotEvidence
    {
        public string Status { get; set; }
        public long RequestedPeople { get; set; }
        public long CompactPayloadBytes { get; set; }
        public long LoadMilliseconds { get; set; }
        public long WorkingSetBytesAfterLoad { get; set; }
        public long ManagedBytesAfterLoad { get; set; }
        public string RollingDigest { get; set; }
        public bool DigestMatchesDisk { get; set; }
        public string FailureType { get; set; }
        public string FailureMessage { get; set; }

        public static ResidentHotEvidence Skipped()
        {
            return new ResidentHotEvidence { Status = "skipped" };
        }
    }

    internal sealed class AllHotRunEvidence
    {
        public string SchemaVersion { get; set; }
        public string Status { get; set; }
        public long Seed { get; set; }
        public long PreviousLivingPopulation { get; set; }
        public long CurrentLivingPopulation { get; set; }
        public long CumulativePersonCount { get; set; }
        public long DeceasedColdArchiveCount { get; set; }
        public long AppendedPeople { get; set; }
        public long AppendMilliseconds { get; set; }
        public long CoreBytes { get; set; }
        public long HotBytes { get; set; }
        public long TotalBytes { get; set; }
        public int BytesPerLivingPerson { get; set; }
        public string RollingDigest { get; set; }
        public bool DirectQueryPassed { get; set; }
        public ResidentHotEvidence Resident { get; set; }
    }

    internal sealed class AllHotStore : IDisposable
    {
        private readonly string _workspace;
        private readonly string _manifestPath;
        private readonly string _corePath;
        private readonly string _hotPath;
        private readonly string _progressPath;
        private readonly int _batchRecords;
        private AllHotManifest _manifest;
        private ulong _digest;

        private AllHotStore(string workspace, int batchRecords, string progressPath)
        {
            _workspace = workspace;
            _manifestPath = Path.Combine(workspace, "manifest.json");
            _corePath = Path.Combine(workspace, "all-alive-core.bin");
            _hotPath = Path.Combine(workspace, "all-alive-hot.bin");
            _progressPath = progressPath;
            _batchRecords = batchRecords;
        }

        public long PersonCount { get { return _manifest.PersonCount; } }

        public static AllHotStore OpenOrCreate(string workspace, long seed, int batchRecords, string progressPath)
        {
            if (batchRecords <= 0) throw new ArgumentOutOfRangeException(nameof(batchRecords));
            Directory.CreateDirectory(workspace);
            var store = new AllHotStore(workspace, batchRecords, progressPath);
            if (File.Exists(store._manifestPath)) store.Load(seed);
            else store.Create(seed);
            store.ValidateState();
            store.WriteProgress("ready");
            return store;
        }

        public long AdvanceTo(long target)
        {
            if (target < _manifest.PersonCount) throw new InvalidOperationException("All-hot target cannot be below the durable checkpoint.");
            var watch = Stopwatch.StartNew();
            while (_manifest.PersonCount < target)
            {
                int count = (int)Math.Min(target - _manifest.PersonCount, _batchRecords);
                AppendBatch(count);
                SaveManifest();
                WriteProgress("checkpoint");
            }
            ValidateState();
            WriteProgress("completed");
            return watch.ElapsedMilliseconds;
        }

        public AllHotDiskEvidence BuildDiskEvidence()
        {
            bool direct = _manifest.PersonCount == 0 ||
                (QueryCore(1).PersonId == 1 &&
                 QueryHot(Math.Max(1, _manifest.PersonCount / 2)).PersonId == Math.Max(1, _manifest.PersonCount / 2) &&
                 QueryHot(_manifest.PersonCount).PersonId == _manifest.PersonCount);
            return new AllHotDiskEvidence
            {
                CoreBytes = new FileInfo(_corePath).Length,
                HotBytes = new FileInfo(_hotPath).Length,
                RollingDigest = _manifest.RollingDigest,
                DirectQueryPassed = direct
            };
        }

        private void Create(long seed)
        {
            if (Directory.EnumerateFileSystemEntries(_workspace).Any()) throw new InvalidOperationException("All-hot workspace contains files but no manifest.");
            _digest = AllHotDigest.Offset;
            _manifest = new AllHotManifest
            {
                SchemaVersion = AllHotFormat.SchemaVersion,
                Seed = seed,
                PersonCount = 0,
                RollingDigest = AllHotDigest.ToHex(_digest)
            };
            CreateFile(_corePath, AllHotFormat.CoreMagic);
            CreateFile(_hotPath, AllHotFormat.HotMagic);
            SaveManifest();
        }

        private void Load(long seed)
        {
            _manifest = JsonConvert.DeserializeObject<AllHotManifest>(File.ReadAllText(_manifestPath, Encoding.UTF8));
            if (_manifest == null || _manifest.SchemaVersion != AllHotFormat.SchemaVersion) throw new InvalidDataException("Unsupported all-hot manifest.");
            if (_manifest.Seed != seed) throw new InvalidOperationException("All-hot resume seed does not match the durable checkpoint.");
            _digest = AllHotDigest.Parse(_manifest.RollingDigest);
            RecoverFile(_corePath, AllHotFormat.CoreMagic, _manifest.PersonCount, AllHotFormat.CoreRecordBytes);
            RecoverFile(_hotPath, AllHotFormat.HotMagic, _manifest.PersonCount, AllHotFormat.HotRecordBytes);
        }

        private void AppendBatch(int count)
        {
            using (var coreStream = new FileStream(_corePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 1 << 20, FileOptions.SequentialScan))
            using (var hotStream = new FileStream(_hotPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 1 << 20, FileOptions.SequentialScan))
            using (var core = new BinaryWriter(coreStream, Encoding.UTF8))
            using (var hot = new BinaryWriter(hotStream, Encoding.UTF8))
            {
                coreStream.Position = coreStream.Length;
                hotStream.Position = hotStream.Length;
                long first = _manifest.PersonCount + 1L;
                long last = _manifest.PersonCount + count;
                for (long id = first; id <= last; id++)
                {
                    long father = id <= 2 ? 0 : id - 1;
                    long mother = id <= 2 ? 0 : id - 2;
                    long household = ((id - 1) / 5) + 1;
                    int birthDay = -(int)(AllHotDigest.Mix((ulong)_manifest.Seed ^ (ulong)id) % 18_250UL);
                    int region = (int)(AllHotDigest.Mix((ulong)_manifest.Seed ^ ((ulong)id * 17UL)) % 1_182UL);
                    byte gender = (byte)(AllHotDigest.Mix((ulong)id) & 1UL);
                    int nextDay = 30 + (int)(AllHotDigest.Mix((ulong)_manifest.Seed ^ ((ulong)id * 31UL)) % 365UL);
                    WriteCore(core, id, father, mother, household, birthDay, region, gender);
                    WriteCore(hot, id, father, mother, household, birthDay, region, gender);
                    hot.Write(nextDay);
                    _digest = AllHotDigest.Add(_digest, id, father, mother, household, birthDay, region, gender, nextDay);
                }
                _manifest.PersonCount += count;
                UpdateHeaderCount(coreStream, _manifest.PersonCount);
                UpdateHeaderCount(hotStream, _manifest.PersonCount);
                coreStream.Flush(true);
                hotStream.Flush(true);
            }
            _manifest.RollingDigest = AllHotDigest.ToHex(_digest);
        }

        private void ValidateState()
        {
            ValidateExact(_corePath, AllHotFormat.CoreMagic, _manifest.PersonCount, AllHotFormat.CoreRecordBytes);
            ValidateExact(_hotPath, AllHotFormat.HotMagic, _manifest.PersonCount, AllHotFormat.HotRecordBytes);
        }

        private CoreProbe QueryCore(long id)
        {
            if (id <= 0 || id > _manifest.PersonCount) throw new ArgumentOutOfRangeException(nameof(id));
            using (var stream = new FileStream(_corePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = AllHotFormat.HeaderBytes + ((id - 1) * AllHotFormat.CoreRecordBytes);
                CoreProbe value = ReadCore(reader);
                if (value.PersonId != id) throw new InvalidDataException("All-hot core direct index is inconsistent.");
                return value;
            }
        }

        private CoreProbe QueryHot(long id)
        {
            if (id <= 0 || id > _manifest.PersonCount) throw new ArgumentOutOfRangeException(nameof(id));
            using (var stream = new FileStream(_hotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = AllHotFormat.HeaderBytes + ((id - 1) * AllHotFormat.HotRecordBytes);
                CoreProbe value = ReadCore(reader);
                value.NextDay = reader.ReadInt32();
                if (value.PersonId != id) throw new InvalidDataException("All-hot living direct index is inconsistent.");
                return value;
            }
        }

        private void SaveManifest()
        {
            string temporary = _manifestPath + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(_manifest, Formatting.Indented), new UTF8Encoding(false));
            ReplaceFile(temporary, _manifestPath);
        }

        private void WriteProgress(string phase)
        {
            if (string.IsNullOrEmpty(_progressPath)) return;
            string directory = Path.GetDirectoryName(_progressPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var value = new
            {
                schema_version = "m15.p5.all-hot.progress.v1",
                phase,
                current_living_population = _manifest.PersonCount,
                cumulative_person_count = _manifest.PersonCount,
                deceased_cold_archive_count = 0,
                updated_at_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            File.WriteAllText(_progressPath, JsonConvert.SerializeObject(value), new UTF8Encoding(false));
        }

        private static void CreateFile(string path, byte[] magic)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(magic);
                writer.Write(1);
                writer.Write(0L);
                stream.Flush(true);
            }
        }

        private static void RecoverFile(string path, byte[] magic, long manifestCount, int recordBytes)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("All-hot checkpoint file is missing.", path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                long headerCount = ReadHeader(reader, magic);
                if (headerCount < manifestCount) throw new InvalidDataException("All-hot file header is behind the manifest: " + path);
                long expected = AllHotFormat.HeaderBytes + (manifestCount * recordBytes);
                if (stream.Length < expected) throw new InvalidDataException("All-hot file is shorter than the manifest: " + path);
                if (stream.Length > expected) stream.SetLength(expected);
                UpdateHeaderCount(stream, manifestCount);
            }
        }

        private static void ValidateExact(string path, byte[] magic, long count, int recordBytes)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                long headerCount = ReadHeader(reader, magic);
                long expected = AllHotFormat.HeaderBytes + (count * recordBytes);
                if (headerCount != count || stream.Length != expected) throw new InvalidDataException("All-hot file does not match the manifest: " + path);
            }
        }

        internal static long ReadHeader(BinaryReader reader, byte[] expectedMagic)
        {
            byte[] magic = reader.ReadBytes(expectedMagic.Length);
            if (!magic.SequenceEqual(expectedMagic) || reader.ReadInt32() != 1) throw new InvalidDataException("All-hot file header is invalid.");
            return reader.ReadInt64();
        }

        internal static void UpdateHeaderCount(FileStream stream, long count)
        {
            long position = stream.Position;
            stream.Position = 12;
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) writer.Write(count);
            stream.Position = position;
        }

        internal static void WriteCore(BinaryWriter writer, long id, long father, long mother, long household, int birthDay, int region, byte gender)
        {
            writer.Write(id);
            writer.Write(father);
            writer.Write(mother);
            writer.Write(household);
            writer.Write(birthDay);
            writer.Write(region);
            writer.Write(gender);
        }

        internal static CoreProbe ReadCore(BinaryReader reader)
        {
            return new CoreProbe
            {
                PersonId = reader.ReadInt64(),
                FatherId = reader.ReadInt64(),
                MotherId = reader.ReadInt64(),
                HouseholdId = reader.ReadInt64(),
                BirthDay = reader.ReadInt32(),
                Region = reader.ReadInt32(),
                Gender = reader.ReadByte()
            };
        }

        internal static void ReplaceFile(string temporary, string destination)
        {
            if (File.Exists(destination)) File.Replace(temporary, destination, null, true);
            else File.Move(temporary, destination);
        }

        public void Dispose() { }
    }

    internal sealed class CoreProbe
    {
        public long PersonId;
        public long FatherId;
        public long MotherId;
        public long HouseholdId;
        public int BirthDay;
        public int Region;
        public byte Gender;
        public int NextDay;
    }

    internal static class ResidentHotLoader
    {
        public static ResidentHotEvidence TryLoad(string workspace, long count, string expectedDigest)
        {
            try { return Load(workspace, count, expectedDigest); }
            catch (OutOfMemoryException exception)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                return new ResidentHotEvidence
                {
                    Status = "out_of_memory",
                    RequestedPeople = count,
                    CompactPayloadBytes = count * AllHotFormat.HotRecordBytes,
                    FailureType = exception.GetType().FullName,
                    FailureMessage = exception.Message
                };
            }
        }

        private static ResidentHotEvidence Load(string workspace, long count, string expectedDigest)
        {
            if (count > int.MaxValue) throw new OutOfMemoryException("The resident hot index exceeds CLR array indexing limits.");
            int length = (int)count;
            var watch = Stopwatch.StartNew();
            long[] ids = new long[length];
            long[] fathers = new long[length];
            long[] mothers = new long[length];
            long[] households = new long[length];
            int[] birthDays = new int[length];
            int[] regions = new int[length];
            byte[] genders = new byte[length];
            int[] nextDays = new int[length];
            ulong digest = AllHotDigest.Offset;
            string hotPath = Path.Combine(workspace, "all-alive-hot.bin");
            using (var stream = new FileStream(hotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.SequentialScan))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                long fileCount = AllHotStore.ReadHeader(reader, AllHotFormat.HotMagic);
                if (fileCount != count) throw new InvalidDataException("Resident hot load count does not match the file header.");
                for (var index = 0; index < length; index++)
                {
                    long id = reader.ReadInt64();
                    long father = reader.ReadInt64();
                    long mother = reader.ReadInt64();
                    long household = reader.ReadInt64();
                    int birthDay = reader.ReadInt32();
                    int region = reader.ReadInt32();
                    byte gender = reader.ReadByte();
                    int nextDay = reader.ReadInt32();
                    if (id != index + 1L) throw new InvalidDataException("Resident all-hot IDs are not contiguous.");
                    ids[index] = id;
                    fathers[index] = father;
                    mothers[index] = mother;
                    households[index] = household;
                    birthDays[index] = birthDay;
                    regions[index] = region;
                    genders[index] = gender;
                    nextDays[index] = nextDay;
                    digest = AllHotDigest.Add(digest, id, father, mother, household, birthDay, region, gender, nextDay);
                }
                if (stream.Position != stream.Length) throw new InvalidDataException("Resident hot file has trailing bytes.");
            }
            string digestText = AllHotDigest.ToHex(digest);
            bool matches = string.Equals(digestText, expectedDigest, StringComparison.Ordinal);
            if (!matches) throw new InvalidDataException("Resident hot digest does not match the durable manifest.");
            long managed = GC.GetTotalMemory(false);
            long workingSet = Process.GetCurrentProcess().WorkingSet64;
            long checksumProbe = ids[0] ^ ids[length - 1] ^ fathers[length - 1] ^ mothers[length - 1] ^ households[length - 1] ^ birthDays[length - 1] ^ regions[length - 1] ^ genders[length - 1] ^ nextDays[length - 1];
            GC.KeepAlive(checksumProbe);
            GC.KeepAlive(ids); GC.KeepAlive(fathers); GC.KeepAlive(mothers); GC.KeepAlive(households);
            GC.KeepAlive(birthDays); GC.KeepAlive(regions); GC.KeepAlive(genders); GC.KeepAlive(nextDays);
            return new ResidentHotEvidence
            {
                Status = "passed",
                RequestedPeople = count,
                CompactPayloadBytes = count * AllHotFormat.HotRecordBytes,
                LoadMilliseconds = watch.ElapsedMilliseconds,
                WorkingSetBytesAfterLoad = workingSet,
                ManagedBytesAfterLoad = managed,
                RollingDigest = digestText,
                DigestMatchesDisk = true
            };
        }
    }

    internal static class AllHotDigest
    {
        public const ulong Offset = 1469598103934665603UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Add(ulong digest, long id, long father, long mother, long household, int birthDay, int region, byte gender, int nextDay)
        {
            unchecked
            {
                digest = Step(digest, (ulong)id);
                digest = Step(digest, (ulong)father);
                digest = Step(digest, (ulong)mother);
                digest = Step(digest, (ulong)household);
                digest = Step(digest, (ulong)(uint)birthDay);
                digest = Step(digest, (ulong)(uint)region);
                digest = Step(digest, gender);
                return Step(digest, (ulong)(uint)nextDay);
            }
        }

        private static ulong Step(ulong digest, ulong value) { unchecked { return (digest ^ value) * Prime; } }

        public static ulong Mix(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        public static string ToHex(ulong value) { return value.ToString("x16", CultureInfo.InvariantCulture); }
        public static ulong Parse(string value) { return ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture); }
    }

    internal sealed class AllHotOptions
    {
        public bool SelfTest { get; private set; }
        public bool SkipResident { get; private set; }
        public string WorkspacePath { get; private set; }
        public string OutputPath { get; private set; }
        public string ProgressPath { get; private set; }
        public long TargetLivingPopulation { get; private set; }
        public int BatchRecords { get; private set; }
        public long Seed { get; private set; }

        public static AllHotOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool selfTest = false;
            bool skipResident = false;
            for (var index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], "--self-test", StringComparison.OrdinalIgnoreCase)) { selfTest = true; continue; }
                if (string.Equals(args[index], "--skip-resident", StringComparison.OrdinalIgnoreCase)) { skipResident = true; continue; }
                if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length) throw new ArgumentException("Invalid all-hot argument: " + args[index]);
                values.Add(args[index], args[++index]);
            }
            string output = Required(values, "--output");
            if (selfTest) return new AllHotOptions { SelfTest = true, OutputPath = output };
            long target = ParseLong(values, "--target-living", -1);
            if (target <= 0 || target > 50_000_000L) throw new ArgumentOutOfRangeException("--target-living");
            return new AllHotOptions
            {
                SkipResident = skipResident,
                WorkspacePath = Required(values, "--workspace"),
                OutputPath = output,
                ProgressPath = Optional(values, "--progress"),
                TargetLivingPopulation = target,
                BatchRecords = (int)ParseLong(values, "--batch-records", 1_000_000L),
                Seed = ParseLong(values, "--seed", 14000015L)
            };
        }

        private static string Required(IDictionary<string, string> values, string key)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Missing required argument " + key + ".");
            return Path.GetFullPath(value);
        }

        private static string Optional(IDictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : null;
        }

        private static long ParseLong(IDictionary<string, string> values, string key, long fallback)
        {
            string value;
            if (!values.TryGetValue(key, out value)) return fallback;
            long parsed;
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) throw new ArgumentException("Invalid integer for " + key + ".");
            return parsed;
        }
    }

    internal static class AllHotSelfTests
    {
        public static void Run(string outputPath)
        {
            int passed = 0;
            var tests = new List<string>();
            Run("all_hot_resident_round_trip", () =>
            {
                string root = NewTemp("resident");
                try
                {
                    using (var store = AllHotStore.OpenOrCreate(root, 17L, 333, null))
                    {
                        store.AdvanceTo(1_000);
                        AllHotDiskEvidence disk = store.BuildDiskEvidence();
                        ResidentHotEvidence resident = ResidentHotLoader.TryLoad(root, 1_000, disk.RollingDigest);
                        True(disk.DirectQueryPassed && resident.Status == "passed" && resident.DigestMatchesDisk, "All-hot resident round trip failed.");
                        Equal(AllHotFormat.HeaderBytes + (1_000L * AllHotFormat.CoreRecordBytes), disk.CoreBytes);
                        Equal(AllHotFormat.HeaderBytes + (1_000L * AllHotFormat.HotRecordBytes), disk.HotBytes);
                    }
                }
                finally { DeleteTemp(root); }
            }, tests, ref passed);
            Run("all_hot_resume_matches_continuous", () =>
            {
                string resumed = NewTemp("resumed");
                string continuous = NewTemp("continuous");
                try
                {
                    using (var store = AllHotStore.OpenOrCreate(resumed, 29L, 400, null)) store.AdvanceTo(1_000);
                    string resumedDigest;
                    using (var store = AllHotStore.OpenOrCreate(resumed, 29L, 333, null)) { store.AdvanceTo(2_000); resumedDigest = store.BuildDiskEvidence().RollingDigest; }
                    string continuousDigest;
                    using (var store = AllHotStore.OpenOrCreate(continuous, 29L, 2_000, null)) { store.AdvanceTo(2_000); continuousDigest = store.BuildDiskEvidence().RollingDigest; }
                    Equal(continuousDigest, resumedDigest);
                }
                finally { DeleteTemp(resumed); DeleteTemp(continuous); }
            }, tests, ref passed);
            Run("all_hot_recovers_uncommitted_append", () =>
            {
                string root = NewTemp("recovery");
                try
                {
                    using (var store = AllHotStore.OpenOrCreate(root, 41L, 250, null)) store.AdvanceTo(1_000);
                    AppendGarbage(Path.Combine(root, "all-alive-core.bin"), AllHotFormat.CoreRecordBytes, 1_001);
                    AppendGarbage(Path.Combine(root, "all-alive-hot.bin"), AllHotFormat.HotRecordBytes, 1_001);
                    using (var store = AllHotStore.OpenOrCreate(root, 41L, 250, null))
                    {
                        Equal(1_000L, store.PersonCount);
                        True(store.BuildDiskEvidence().DirectQueryPassed, "Recovered all-hot queries failed.");
                    }
                }
                finally { DeleteTemp(root); }
            }, tests, ref passed);
            PopulationAllHotProgram.WriteJson(outputPath, new { schema_version = "m15.p5.all-hot.self-test.v1", status = "passed", passed, failed = 0, tests });
            Console.WriteLine("RESULT passed={0} failed=0", passed);
        }

        private static void Run(string name, Action action, ICollection<string> tests, ref int passed) { action(); tests.Add(name); passed++; }
        private static void True(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + ", actual " + actual + "."); }

        private static string NewTemp(string suffix)
        {
            string path = Path.Combine(Path.GetTempPath(), "mandate-m15-p5-all-hot-" + suffix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteTemp(string path)
        {
            string full = Path.GetFullPath(path);
            string temp = Path.GetFullPath(Path.GetTempPath());
            if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("mandate-m15-p5-all-hot-", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Refusing to remove a non-all-hot temporary directory.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        private static void AppendGarbage(string path, int bytes, long headerCount)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                stream.Position = stream.Length;
                stream.Write(new byte[bytes], 0, bytes);
                AllHotStore.UpdateHeaderCount(stream, headerCount);
                stream.Flush(true);
            }
        }
    }
}
