using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Mandate.Tools.PopulationAllHot;

namespace Mandate.Tools.PopulationAllHotScheduling
{
    internal static class PopulationAllHotSchedulingProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                SchedulingOptions options = SchedulingOptions.Parse(args);
                if (options.Mode == "self-test")
                {
                    SchedulingSelfTests.Run(options.OutputPath);
                    return 0;
                }

                object evidence;
                if (options.Mode == "build")
                {
                    evidence = SchedulingIndexBuilder.Build(
                        options.SourceWorkspace,
                        options.IndexWorkspace,
                        options.ExpectedPeople,
                        options.Seed,
                        options.ProgressPath);
                }
                else
                {
                    int days = options.Mode == "day" ? 1 : options.Mode == "month" ? 30 : 365;
                    evidence = SchedulingRunner.Run(
                        options.SourceWorkspace,
                        options.IndexWorkspace,
                        options.RunWorkspace,
                        options.ExpectedPeople,
                        options.Seed,
                        days,
                        options.ProgressPath);
                }

                PopulationAllHotProgram.WriteJson(options.OutputPath, evidence);
                Console.WriteLine("RESULT m15-p5-full-living={0} people={1}", options.Mode, options.ExpectedPeople);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }

    internal static class SchedulingFormat
    {
        public const string IndexSchema = "m15.p5.full-living-index.v1";
        public const string EvidenceSchema = "m15.p5.full-living-scheduling.evidence.v1";
        public const int Regions = 1182;
        public const int Occupations = 16;
        public const int Days = 365;
        public const int HouseholdMembers = 5;
        public const int HouseholdRecordBytes = 24;
        public const int EventRecordBytes = 20;
    }

    internal sealed class SchedulingIndexManifest
    {
        public string SchemaVersion { get; set; }
        public long Seed { get; set; }
        public long PersonCount { get; set; }
        public long HouseholdCount { get; set; }
        public string SourceDigest { get; set; }
        public long[] RegionCounts { get; set; }
        public long[] OccupationCounts { get; set; }
        public long[] DueCounts { get; set; }
        public string IndexDigest { get; set; }
    }

    internal sealed class SchedulingBuildEvidence
    {
        public string SchemaVersion { get; set; }
        public string Status { get; set; }
        public long PersonCount { get; set; }
        public long HouseholdCount { get; set; }
        public long OccupationMembershipCount { get; set; }
        public long RegionMembershipCount { get; set; }
        public long DueMembershipCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public long IndexBytes { get; set; }
        public string SourceDigest { get; set; }
        public string IndexDigest { get; set; }
        public bool DirectQueryPassed { get; set; }
    }

    internal sealed class SchedulingRunEvidence
    {
        public string SchemaVersion { get; set; }
        public string Status { get; set; }
        public string Window { get; set; }
        public int WindowDays { get; set; }
        public long ResidentPeople { get; set; }
        public long ResidentPayloadBytes { get; set; }
        public long ResidentLoadMilliseconds { get; set; }
        public long DueRecordsScanned { get; set; }
        public long ChangedPeople { get; set; }
        public long WritebackRecords { get; set; }
        public long UntouchedPeople { get; set; }
        public long EventBytes { get; set; }
        public long SimulationMilliseconds { get; set; }
        public long WorkingSetBytesWhileResident { get; set; }
        public long ManagedBytesWhileResident { get; set; }
        public string SourceDigest { get; set; }
        public string EventDigest { get; set; }
        public bool UsedPartitionedDueQueue { get; set; }
        public bool AllLivingRemainedResident { get; set; }
        public bool InvariantsPassed { get; set; }
    }

    internal static class SchedulingIndexBuilder
    {
        public static SchedulingBuildEvidence Build(string sourceWorkspace, string indexWorkspace, long expectedPeople, long seed, string progressPath)
        {
            var watch = Stopwatch.StartNew();
            AllHotManifest source = LoadSource(sourceWorkspace, expectedPeople, seed);
            string manifestPath = Path.Combine(indexWorkspace, "index-manifest.json");
            if (File.Exists(manifestPath))
            {
                SchedulingIndexManifest existing = LoadAndValidate(indexWorkspace, expectedPeople, seed, source.RollingDigest);
                return Evidence(existing, indexWorkspace, watch.ElapsedMilliseconds);
            }

            string staging = indexWorkspace + ".staging";
            if (Directory.Exists(staging)) throw new InvalidOperationException("Incomplete staging index exists. Run the safe wrapper with -ResetIndexes.");
            if (Directory.Exists(indexWorkspace)) throw new InvalidOperationException("Index directory exists without a committed manifest. Run the safe wrapper with -ResetIndexes.");
            Directory.CreateDirectory(staging);
            WriteProgress(progressPath, "building", 0, expectedPeople);

            long[] regionCounts = new long[SchedulingFormat.Regions];
            long[] occupationCounts = new long[SchedulingFormat.Occupations];
            long[] dueCounts = new long[SchedulingFormat.Days];
            ulong digest = AllHotDigest.Offset;
            string hotPath = Path.Combine(sourceWorkspace, "all-alive-hot.bin");
            string occupationPath = Path.Combine(staging, "person-occupation.bin");
            string householdPath = Path.Combine(staging, "households.bin");
            string occupationDirectory = Path.Combine(staging, "occupations");
            string dueDirectory = Path.Combine(staging, "due-days");
            Directory.CreateDirectory(occupationDirectory);
            Directory.CreateDirectory(dueDirectory);

            BinaryWriter[] occupationWriters = OpenPartitionWriters(occupationDirectory, "occupation", SchedulingFormat.Occupations);
            BinaryWriter[] dueWriters = OpenPartitionWriters(dueDirectory, "day", SchedulingFormat.Days);
            try
            {
                using (var hotStream = new FileStream(hotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.SequentialScan))
                using (var reader = new BinaryReader(hotStream, Encoding.UTF8))
                using (var occupationStream = new FileStream(occupationPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1 << 20, FileOptions.SequentialScan))
                using (var householdsStream = new FileStream(householdPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1 << 20, FileOptions.SequentialScan))
                using (var occupationWriter = new BinaryWriter(occupationStream, Encoding.UTF8))
                using (var householdWriter = new BinaryWriter(householdsStream, Encoding.UTF8))
                {
                    long count = AllHotStore.ReadHeader(reader, AllHotFormat.HotMagic);
                    if (count != expectedPeople) throw new InvalidDataException("All-hot source count does not match the requested index size.");
                    for (long expectedId = 1; expectedId <= count; expectedId++)
                    {
                        long id = reader.ReadInt64();
                        long father = reader.ReadInt64();
                        long mother = reader.ReadInt64();
                        long household = reader.ReadInt64();
                        int birthDay = reader.ReadInt32();
                        int region = reader.ReadInt32();
                        byte gender = reader.ReadByte();
                        int sourceNextDay = reader.ReadInt32();
                        if (id != expectedId) throw new InvalidDataException("All-hot source IDs are not contiguous.");
                        if (household != ((id - 1L) / SchedulingFormat.HouseholdMembers) + 1L) throw new InvalidDataException("Source household membership is invalid.");
                        if (region < 0 || region >= SchedulingFormat.Regions) throw new InvalidDataException("Source region is outside the index range.");

                        byte occupation = (byte)(AllHotDigest.Mix((ulong)seed ^ ((ulong)id * 97UL)) % SchedulingFormat.Occupations);
                        int dueDay = 1 + ((sourceNextDay - 1) % SchedulingFormat.Days);
                        occupationWriter.Write(occupation);
                        occupationWriters[occupation].Write(id);
                        dueWriters[dueDay - 1].Write(id);
                        occupationCounts[occupation]++;
                        regionCounts[region]++;
                        dueCounts[dueDay - 1]++;

                        if (((id - 1L) % SchedulingFormat.HouseholdMembers) == 0)
                        {
                            long members = Math.Min(SchedulingFormat.HouseholdMembers, count - id + 1L);
                            householdWriter.Write(household);
                            householdWriter.Write(id);
                            householdWriter.Write((int)members);
                            householdWriter.Write(region);
                        }

                        digest = AddIndexDigest(digest, id, household, occupation, region, dueDay);
                        if ((id % 1_000_000L) == 0) WriteProgress(progressPath, "building", id, expectedPeople);
                    }
                    if (hotStream.Position != hotStream.Length) throw new InvalidDataException("All-hot source contains trailing bytes.");
                    occupationStream.Flush(true);
                    householdsStream.Flush(true);
                }
            }
            finally
            {
                DisposeWriters(occupationWriters);
                DisposeWriters(dueWriters);
            }

            var manifest = new SchedulingIndexManifest
            {
                SchemaVersion = SchedulingFormat.IndexSchema,
                Seed = seed,
                PersonCount = expectedPeople,
                HouseholdCount = (expectedPeople + SchedulingFormat.HouseholdMembers - 1L) / SchedulingFormat.HouseholdMembers,
                SourceDigest = source.RollingDigest,
                RegionCounts = regionCounts,
                OccupationCounts = occupationCounts,
                DueCounts = dueCounts,
                IndexDigest = AllHotDigest.ToHex(digest)
            };
            ValidateStaging(staging, manifest);
            PopulationAllHotProgram.WriteJson(Path.Combine(staging, "index-manifest.json"), manifest);
            Directory.Move(staging, indexWorkspace);
            SchedulingIndexManifest committed = LoadAndValidate(indexWorkspace, expectedPeople, seed, source.RollingDigest);
            WriteProgress(progressPath, "completed", expectedPeople, expectedPeople);
            return Evidence(committed, indexWorkspace, watch.ElapsedMilliseconds);
        }

        internal static SchedulingIndexManifest LoadAndValidate(string indexWorkspace, long expectedPeople, long seed, string sourceDigest)
        {
            string manifestPath = Path.Combine(indexWorkspace, "index-manifest.json");
            if (!File.Exists(manifestPath)) throw new FileNotFoundException("Committed scheduling index manifest is missing.", manifestPath);
            var manifest = JsonConvert.DeserializeObject<SchedulingIndexManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
            if (manifest == null || manifest.SchemaVersion != SchedulingFormat.IndexSchema) throw new InvalidDataException("Unsupported scheduling index manifest.");
            if (manifest.PersonCount != expectedPeople || manifest.Seed != seed || !string.Equals(manifest.SourceDigest, sourceDigest, StringComparison.Ordinal)) throw new InvalidDataException("Scheduling index does not match the all-hot source.");
            ValidateStaging(indexWorkspace, manifest);
            if (manifest.RegionCounts.Sum() != expectedPeople || manifest.OccupationCounts.Sum() != expectedPeople || manifest.DueCounts.Sum() != expectedPeople) throw new InvalidDataException("Scheduling index membership totals are not conserved.");
            return manifest;
        }

        internal static AllHotManifest LoadSource(string sourceWorkspace, long expectedPeople, long seed)
        {
            string manifestPath = Path.Combine(sourceWorkspace, "manifest.json");
            var source = JsonConvert.DeserializeObject<AllHotManifest>(File.ReadAllText(manifestPath, Encoding.UTF8));
            if (source == null || source.SchemaVersion != AllHotFormat.SchemaVersion || source.PersonCount != expectedPeople || source.Seed != seed) throw new InvalidDataException("All-hot source manifest does not match this run.");
            string hotPath = Path.Combine(sourceWorkspace, "all-alive-hot.bin");
            long expectedLength = AllHotFormat.HeaderBytes + expectedPeople * AllHotFormat.HotRecordBytes;
            if (!File.Exists(hotPath) || new FileInfo(hotPath).Length != expectedLength) throw new InvalidDataException("All-hot source file length is invalid.");
            return source;
        }

        private static void ValidateStaging(string root, SchedulingIndexManifest manifest)
        {
            RequireLength(Path.Combine(root, "person-occupation.bin"), manifest.PersonCount);
            RequireLength(Path.Combine(root, "households.bin"), manifest.HouseholdCount * SchedulingFormat.HouseholdRecordBytes);
            for (var index = 0; index < SchedulingFormat.Occupations; index++) RequireLength(PartitionPath(Path.Combine(root, "occupations"), "occupation", index), manifest.OccupationCounts[index] * 8L);
            for (var index = 0; index < SchedulingFormat.Days; index++) RequireLength(PartitionPath(Path.Combine(root, "due-days"), "day", index), manifest.DueCounts[index] * 8L);
        }

        private static SchedulingBuildEvidence Evidence(SchedulingIndexManifest manifest, string root, long milliseconds)
        {
            bool direct = QueryHousehold(root, 1).Item1 == 1L && QueryHousehold(root, manifest.HouseholdCount).Item1 == manifest.HouseholdCount;
            return new SchedulingBuildEvidence
            {
                SchemaVersion = SchedulingFormat.EvidenceSchema,
                Status = "passed",
                PersonCount = manifest.PersonCount,
                HouseholdCount = manifest.HouseholdCount,
                OccupationMembershipCount = manifest.OccupationCounts.Sum(),
                RegionMembershipCount = manifest.RegionCounts.Sum(),
                DueMembershipCount = manifest.DueCounts.Sum(),
                ElapsedMilliseconds = milliseconds,
                IndexBytes = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length),
                SourceDigest = manifest.SourceDigest,
                IndexDigest = manifest.IndexDigest,
                DirectQueryPassed = direct
            };
        }

        private static Tuple<long, long> QueryHousehold(string root, long householdId)
        {
            using (var stream = new FileStream(Path.Combine(root, "households.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = (householdId - 1L) * SchedulingFormat.HouseholdRecordBytes;
                long id = reader.ReadInt64();
                long first = reader.ReadInt64();
                reader.ReadInt32(); reader.ReadInt32();
                return Tuple.Create(id, first);
            }
        }

        private static BinaryWriter[] OpenPartitionWriters(string directory, string prefix, int count)
        {
            var writers = new BinaryWriter[count];
            for (var index = 0; index < count; index++) writers[index] = new BinaryWriter(new FileStream(PartitionPath(directory, prefix, index), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan), Encoding.UTF8);
            return writers;
        }

        private static void DisposeWriters(IEnumerable<BinaryWriter> writers)
        {
            foreach (BinaryWriter writer in writers) if (writer != null) writer.Dispose();
        }

        internal static string PartitionPath(string directory, string prefix, int zeroBased)
        {
            return Path.Combine(directory, string.Format(CultureInfo.InvariantCulture, "{0}-{1:D3}.bin", prefix, zeroBased + 1));
        }

        private static void RequireLength(string path, long expected)
        {
            if (!File.Exists(path) || new FileInfo(path).Length != expected) throw new InvalidDataException("Scheduling index file length is invalid: " + path);
        }

        private static ulong AddIndexDigest(ulong digest, long id, long household, byte occupation, int region, int dueDay)
        {
            unchecked { return AllHotDigest.Mix(digest ^ (ulong)id ^ ((ulong)household << 1) ^ ((ulong)occupation << 33) ^ ((ulong)(uint)region << 40) ^ ((ulong)(uint)dueDay << 52)); }
        }

        internal static void WriteProgress(string path, string phase, long current, long total)
        {
            if (string.IsNullOrEmpty(path)) return;
            PopulationAllHotProgram.WriteJson(path, new { schema_version = "m15.p5.full-living.progress.v1", phase, current, total, updated_at_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) });
        }
    }

    internal sealed class ResidentSchedulingWorld
    {
        public long[] Ids;
        public long[] Fathers;
        public long[] Mothers;
        public long[] Households;
        public int[] BirthDays;
        public int[] Regions;
        public byte[] Genders;
        public int[] NextDays;
        public byte[] Occupations;
        public string Digest;
        public long LoadMilliseconds;

        public static ResidentSchedulingWorld Load(string sourceWorkspace, string indexWorkspace, long count, string expectedDigest)
        {
            if (count > int.MaxValue) throw new OutOfMemoryException("Resident scheduling arrays exceed CLR indexing limits.");
            var watch = Stopwatch.StartNew();
            int length = (int)count;
            var world = new ResidentSchedulingWorld
            {
                Ids = new long[length], Fathers = new long[length], Mothers = new long[length], Households = new long[length],
                BirthDays = new int[length], Regions = new int[length], Genders = new byte[length], NextDays = new int[length]
            };
            ulong digest = AllHotDigest.Offset;
            using (var stream = new FileStream(Path.Combine(sourceWorkspace, "all-alive-hot.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.SequentialScan))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (AllHotStore.ReadHeader(reader, AllHotFormat.HotMagic) != count) throw new InvalidDataException("Resident source count mismatch.");
                for (var index = 0; index < length; index++)
                {
                    long id = reader.ReadInt64(); long father = reader.ReadInt64(); long mother = reader.ReadInt64(); long household = reader.ReadInt64();
                    int birthDay = reader.ReadInt32(); int region = reader.ReadInt32(); byte gender = reader.ReadByte(); int nextDay = reader.ReadInt32();
                    if (id != index + 1L) throw new InvalidDataException("Resident IDs are not contiguous.");
                    world.Ids[index] = id; world.Fathers[index] = father; world.Mothers[index] = mother; world.Households[index] = household;
                    world.BirthDays[index] = birthDay; world.Regions[index] = region; world.Genders[index] = gender; world.NextDays[index] = nextDay;
                    digest = AllHotDigest.Add(digest, id, father, mother, household, birthDay, region, gender, nextDay);
                }
            }
            world.Occupations = File.ReadAllBytes(Path.Combine(indexWorkspace, "person-occupation.bin"));
            if (world.Occupations.LongLength != count) throw new InvalidDataException("Resident occupation index count mismatch.");
            world.Digest = AllHotDigest.ToHex(digest);
            if (!string.Equals(world.Digest, expectedDigest, StringComparison.Ordinal)) throw new InvalidDataException("Resident source digest mismatch.");
            world.LoadMilliseconds = watch.ElapsedMilliseconds;
            return world;
        }
    }

    internal static class SchedulingRunner
    {
        public static SchedulingRunEvidence Run(string sourceWorkspace, string indexWorkspace, string runWorkspace, long expectedPeople, long seed, int days, string progressPath)
        {
            if (Directory.Exists(runWorkspace) && Directory.EnumerateFileSystemEntries(runWorkspace).Any()) throw new InvalidOperationException("Run workspace must be empty.");
            Directory.CreateDirectory(runWorkspace);
            AllHotManifest source = SchedulingIndexBuilder.LoadSource(sourceWorkspace, expectedPeople, seed);
            SchedulingIndexManifest indexes = SchedulingIndexBuilder.LoadAndValidate(indexWorkspace, expectedPeople, seed, source.RollingDigest);
            SchedulingIndexBuilder.WriteProgress(progressPath, "resident-load", 0, expectedPeople);
            ResidentSchedulingWorld world = ResidentSchedulingWorld.Load(sourceWorkspace, indexWorkspace, expectedPeople, source.RollingDigest);
            long residentPayload = expectedPeople * AllHotFormat.HotRecordBytes + expectedPeople;
            long managed = GC.GetTotalMemory(false);
            long workingSet = Process.GetCurrentProcess().WorkingSet64;
            var watch = Stopwatch.StartNew();
            long scanned = 0;
            ulong eventDigest = AllHotDigest.Offset;
            string eventsPath = Path.Combine(runWorkspace, "events.bin");
            using (var stream = new FileStream(eventsPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1 << 20, FileOptions.SequentialScan))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                for (var day = 1; day <= days; day++)
                {
                    string duePath = SchedulingIndexBuilder.PartitionPath(Path.Combine(indexWorkspace, "due-days"), "day", day - 1);
                    using (var dueStream = new FileStream(duePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.SequentialScan))
                    using (var reader = new BinaryReader(dueStream, Encoding.UTF8))
                    {
                        long expectedForDay = indexes.DueCounts[day - 1];
                        for (long dueIndex = 0; dueIndex < expectedForDay; dueIndex++)
                        {
                            long id = reader.ReadInt64();
                            int index = checked((int)(id - 1L));
                            if (index < 0 || index >= world.Ids.Length || world.Ids[index] != id) throw new InvalidDataException("Due queue references an invalid person.");
                            long household = world.Households[index];
                            int region = world.Regions[index];
                            byte occupation = world.Occupations[index];
                            if (household != ((id - 1L) / SchedulingFormat.HouseholdMembers) + 1L || region < 0 || region >= SchedulingFormat.Regions || occupation >= SchedulingFormat.Occupations) throw new InvalidDataException("Due person index membership is invalid.");
                            ulong mixed = AllHotDigest.Mix((ulong)seed ^ (ulong)id ^ ((ulong)(uint)day << 32));
                            int nextDue = day + 30 + (int)(mixed % 365UL);
                            byte eventType = (byte)((mixed >> 12) % 8UL);
                            writer.Write(id); writer.Write(day); writer.Write(nextDue); writer.Write(eventType); writer.Write(occupation); writer.Write((ushort)region);
                            eventDigest = AllHotDigest.Mix(eventDigest ^ (ulong)id ^ ((ulong)(uint)day << 8) ^ ((ulong)(uint)nextDue << 24) ^ ((ulong)eventType << 56) ^ ((ulong)occupation << 60) ^ (ulong)(uint)region);
                            scanned++;
                        }
                        if (dueStream.Position != dueStream.Length) throw new InvalidDataException("Due queue partition contains trailing records.");
                    }
                    SchedulingIndexBuilder.WriteProgress(progressPath, "simulating", day, days);
                }
                stream.Flush(true);
            }
            watch.Stop();
            long expectedScanned = indexes.DueCounts.Take(days).Sum();
            long eventBytes = new FileInfo(eventsPath).Length;
            bool invariant = scanned == expectedScanned && eventBytes == scanned * SchedulingFormat.EventRecordBytes && (days != SchedulingFormat.Days || scanned == expectedPeople);
            if (!invariant) throw new InvalidDataException("Scheduling simulation conservation invariant failed.");
            workingSet = Math.Max(workingSet, Process.GetCurrentProcess().WorkingSet64);
            managed = Math.Max(managed, GC.GetTotalMemory(false));
            long checksumProbe = world.Ids[0] ^ world.Ids[world.Ids.Length - 1] ^ world.Households[world.Households.Length - 1] ^ world.NextDays[world.NextDays.Length - 1] ^ world.Occupations[world.Occupations.Length - 1];
            GC.KeepAlive(checksumProbe); GC.KeepAlive(world);
            SchedulingIndexBuilder.WriteProgress(progressPath, "completed", days, days);
            return new SchedulingRunEvidence
            {
                SchemaVersion = SchedulingFormat.EvidenceSchema,
                Status = "passed",
                Window = days == 1 ? "day" : days == 30 ? "month" : "year",
                WindowDays = days,
                ResidentPeople = expectedPeople,
                ResidentPayloadBytes = residentPayload,
                ResidentLoadMilliseconds = world.LoadMilliseconds,
                DueRecordsScanned = scanned,
                ChangedPeople = scanned,
                WritebackRecords = scanned,
                UntouchedPeople = expectedPeople - scanned,
                EventBytes = eventBytes,
                SimulationMilliseconds = watch.ElapsedMilliseconds,
                WorkingSetBytesWhileResident = workingSet,
                ManagedBytesWhileResident = managed,
                SourceDigest = source.RollingDigest,
                EventDigest = AllHotDigest.ToHex(eventDigest),
                UsedPartitionedDueQueue = true,
                AllLivingRemainedResident = true,
                InvariantsPassed = true
            };
        }
    }

    internal sealed class SchedulingOptions
    {
        public string Mode { get; private set; }
        public string SourceWorkspace { get; private set; }
        public string IndexWorkspace { get; private set; }
        public string RunWorkspace { get; private set; }
        public string OutputPath { get; private set; }
        public string ProgressPath { get; private set; }
        public long ExpectedPeople { get; private set; }
        public long Seed { get; private set; }

        public static SchedulingOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < args.Length; index++)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length) throw new ArgumentException("Invalid scheduling argument: " + args[index]);
                values.Add(args[index], args[++index]);
            }
            string mode = Required(values, "--mode", false);
            string output = Required(values, "--output", true);
            if (mode == "self-test") return new SchedulingOptions { Mode = mode, OutputPath = output };
            if (mode != "build" && mode != "day" && mode != "month" && mode != "year") throw new ArgumentException("Unsupported scheduling mode.");
            return new SchedulingOptions
            {
                Mode = mode,
                SourceWorkspace = Required(values, "--source-workspace", true),
                IndexWorkspace = Required(values, "--index-workspace", true),
                RunWorkspace = mode == "build" ? null : Required(values, "--run-workspace", true),
                OutputPath = output,
                ProgressPath = Optional(values, "--progress"),
                ExpectedPeople = ParseLong(values, "--expected-people", 50_000_000L),
                Seed = ParseLong(values, "--seed", 14_000_015L)
            };
        }

        private static string Required(IDictionary<string, string> values, string key, bool path)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Missing required argument " + key + ".");
            return path ? Path.GetFullPath(value) : value.ToLowerInvariant();
        }

        private static string Optional(IDictionary<string, string> values, string key)
        {
            string value; return values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : null;
        }

        private static long ParseLong(IDictionary<string, string> values, string key, long fallback)
        {
            string value; if (!values.TryGetValue(key, out value)) return fallback;
            long parsed; if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed <= 0 || parsed > 50_000_000L) throw new ArgumentException("Invalid integer for " + key + ".");
            return parsed;
        }
    }

    internal static class SchedulingSelfTests
    {
        public static void Run(string outputPath)
        {
            string root = Path.Combine(Path.GetTempPath(), "m15-p5-scheduling-" + Guid.NewGuid().ToString("N"));
            var passed = new List<string>();
            try
            {
                string source = Path.Combine(root, "source"); string indexes = Path.Combine(root, "indexes");
                using (var store = AllHotStore.OpenOrCreate(source, 71L, 2000, null)) store.AdvanceTo(10_000);
                SchedulingBuildEvidence build = SchedulingIndexBuilder.Build(source, indexes, 10_000, 71L, null);
                Assert(build.PersonCount == 10_000 && build.HouseholdCount == 2_000 && build.DirectQueryPassed, "Index build failed.");
                passed.Add("build_indexes_conserves_membership");
                SchedulingRunEvidence day = SchedulingRunner.Run(source, indexes, Path.Combine(root, "day"), 10_000, 71L, 1, null);
                SchedulingRunEvidence month = SchedulingRunner.Run(source, indexes, Path.Combine(root, "month"), 10_000, 71L, 30, null);
                SchedulingRunEvidence year = SchedulingRunner.Run(source, indexes, Path.Combine(root, "year"), 10_000, 71L, 365, null);
                Assert(day.DueRecordsScanned < month.DueRecordsScanned && month.DueRecordsScanned < 10_000 && year.DueRecordsScanned == 10_000, "Scheduling windows did not use bounded due partitions.");
                passed.Add("day_month_year_scan_due_partitions_only");
                SchedulingRunEvidence repeat = SchedulingRunner.Run(source, indexes, Path.Combine(root, "year-repeat"), 10_000, 71L, 365, null);
                Assert(year.EventDigest == repeat.EventDigest && year.WritebackRecords == repeat.WritebackRecords, "Scheduling replay is not deterministic.");
                passed.Add("year_replay_is_deterministic");
                PopulationAllHotProgram.WriteJson(outputPath, new { schema_version = "m15.p5.full-living.self-test.v1", status = "passed", passed = passed.Count, failed = 0, tests = passed });
                Console.WriteLine("RESULT m15-p5-full-living-self-test=passed tests={0}", passed.Count);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
