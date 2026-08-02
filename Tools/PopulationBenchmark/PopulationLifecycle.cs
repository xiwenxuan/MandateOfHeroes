using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationLifecycle
{
    internal static class PopulationLifecycleProgram
    {
        public static int Main(string[] args)
        {
            try
            {
                LifecycleOptions options = LifecycleOptions.Parse(args);
                if (options.SelfTest)
                {
                    LifecycleSelfTests.Run(options.OutputPath);
                    return 0;
                }

                var profile = new PopulationCapacityProfile
                {
                    HistoricalReferencePopulation = options.HistoricalReferencePopulation,
                    PopulationScaleBasisPoints = options.PopulationScaleBasisPoints,
                    ProjectionYears = options.ProjectionYears,
                    AnnualBirthRateBasisPoints = options.AnnualBirthRateBasisPoints,
                    AnnualDeathRateBasisPoints = options.AnnualDeathRateBasisPoints,
                    OfficialSupportedCumulativeTarget = 50_000_000L,
                    WarningThresholdBasisPoints = 9_000,
                    MemoryBudgetBytes = options.MemoryBudgetBytes,
                    DiskBudgetBytes = options.DiskBudgetBytes
                };
                CapacityEstimate estimate = PopulationCapacityEstimator.Estimate(profile);
                if (estimate.InitialLivingPopulation != options.InitialLivingPopulation)
                {
                    throw new InvalidOperationException(
                        "The capacity profile opening population does not match --initial-living. " +
                        "Choose historical-reference and scale values that produce the same result.");
                }

                var stopwatch = Stopwatch.StartNew();
                LifecycleRunEvidence evidence;
                using (var store = LifecycleStore.OpenOrCreate(
                    options.WorkspacePath,
                    options.InitialLivingPopulation,
                    options.Seed,
                    options.BatchRecords,
                    options.ProgressPath))
                {
                    store.AdvanceTo(options.TargetCumulativePopulation);
                    evidence = store.BuildEvidence(estimate, stopwatch.ElapsedMilliseconds);
                }

                WriteJson(options.OutputPath, evidence);
                Console.WriteLine(
                    "RESULT m15-p5=passed initial={0} current={1} cumulative={2} cold={3} elapsed_ms={4}",
                    evidence.InitialLivingPopulation,
                    evidence.CurrentLivingPopulation,
                    evidence.CumulativePersonCount,
                    evidence.DeceasedColdArchiveCount,
                    evidence.ElapsedMilliseconds);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void WriteJson(string path, object value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(
                path,
                JsonConvert.SerializeObject(value, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }

    internal sealed class PopulationCapacityProfile
    {
        public long HistoricalReferencePopulation { get; set; }
        public int PopulationScaleBasisPoints { get; set; }
        public int ProjectionYears { get; set; }
        public int AnnualBirthRateBasisPoints { get; set; }
        public int AnnualDeathRateBasisPoints { get; set; }
        public long OfficialSupportedCumulativeTarget { get; set; }
        public int WarningThresholdBasisPoints { get; set; }
        public long MemoryBudgetBytes { get; set; }
        public long DiskBudgetBytes { get; set; }
    }

    internal sealed class CapacityEstimate
    {
        public long HistoricalReferencePopulation { get; set; }
        public int PopulationScaleBasisPoints { get; set; }
        public long InitialLivingPopulation { get; set; }
        public long ProjectedCurrentLivingPopulation { get; set; }
        public long ProjectedPeakLivingPopulation { get; set; }
        public long ProjectedBirths { get; set; }
        public long ProjectedDeaths { get; set; }
        public long ProjectedCumulativePersonCount { get; set; }
        public int ProjectionYears { get; set; }
        public long OfficialSupportedCumulativeTarget { get; set; }
        public string SupportStatus { get; set; }
        public bool PerformanceWarning { get; set; }
        public bool BirthsAllowed { get; set; }
        public long EstimatedMemoryBytes { get; set; }
        public long EstimatedDiskBytes { get; set; }
        public string HardwarePressure { get; set; }
    }

    internal static class PopulationCapacityEstimator
    {
        public static CapacityEstimate Estimate(PopulationCapacityProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.HistoricalReferencePopulation <= 0) throw new ArgumentOutOfRangeException(nameof(profile.HistoricalReferencePopulation));
            if (profile.PopulationScaleBasisPoints <= 0 || profile.PopulationScaleBasisPoints > 10_000) throw new ArgumentOutOfRangeException(nameof(profile.PopulationScaleBasisPoints));
            if (profile.ProjectionYears <= 0 || profile.ProjectionYears > 1_000) throw new ArgumentOutOfRangeException(nameof(profile.ProjectionYears));
            if (profile.AnnualBirthRateBasisPoints < 0 || profile.AnnualBirthRateBasisPoints > 10_000) throw new ArgumentOutOfRangeException(nameof(profile.AnnualBirthRateBasisPoints));
            if (profile.AnnualDeathRateBasisPoints < 0 || profile.AnnualDeathRateBasisPoints > 10_000) throw new ArgumentOutOfRangeException(nameof(profile.AnnualDeathRateBasisPoints));
            if (profile.OfficialSupportedCumulativeTarget <= 0) throw new ArgumentOutOfRangeException(nameof(profile.OfficialSupportedCumulativeTarget));
            if (profile.WarningThresholdBasisPoints <= 0 || profile.WarningThresholdBasisPoints > 10_000) throw new ArgumentOutOfRangeException(nameof(profile.WarningThresholdBasisPoints));

            long initial = Scale(profile.HistoricalReferencePopulation, profile.PopulationScaleBasisPoints);
            long living = initial;
            long peak = initial;
            long births = 0;
            long deaths = 0;
            for (var year = 0; year < profile.ProjectionYears; year++)
            {
                long annualBirths = Scale(living, profile.AnnualBirthRateBasisPoints);
                long annualDeaths = Math.Min(SaturatingAdd(living, annualBirths), Scale(living, profile.AnnualDeathRateBasisPoints));
                births = SaturatingAdd(births, annualBirths);
                deaths = SaturatingAdd(deaths, annualDeaths);
                living = SaturatingAdd(living, annualBirths);
                living = Math.Max(0, living - annualDeaths);
                peak = Math.Max(peak, living);
            }

            long cumulative = SaturatingAdd(initial, births);
            long warningAt = Scale(profile.OfficialSupportedCumulativeTarget, profile.WarningThresholdBasisPoints);
            string support = cumulative > profile.OfficialSupportedCumulativeTarget
                ? "beyond_official_guarantee"
                : cumulative >= warningAt ? "approaching_official_target" : "within_official_target";

            long estimatedMemory = SaturatingAdd(SaturatingMultiply(living, LifecycleFormat.HotRecordBytes), SaturatingMultiply(Math.Min(cumulative, 1_000_000L), 16L));
            long estimatedDisk = SaturatingAdd(
                SaturatingMultiply(cumulative, LifecycleFormat.CoreRecordBytes),
                SaturatingAdd(SaturatingMultiply(deaths, LifecycleFormat.ColdRecordBytes), SaturatingMultiply(living, LifecycleFormat.HotRecordBytes)));

            return new CapacityEstimate
            {
                HistoricalReferencePopulation = profile.HistoricalReferencePopulation,
                PopulationScaleBasisPoints = profile.PopulationScaleBasisPoints,
                InitialLivingPopulation = initial,
                ProjectedCurrentLivingPopulation = living,
                ProjectedPeakLivingPopulation = peak,
                ProjectedBirths = births,
                ProjectedDeaths = deaths,
                ProjectedCumulativePersonCount = cumulative,
                ProjectionYears = profile.ProjectionYears,
                OfficialSupportedCumulativeTarget = profile.OfficialSupportedCumulativeTarget,
                SupportStatus = support,
                PerformanceWarning = cumulative >= warningAt,
                BirthsAllowed = true,
                EstimatedMemoryBytes = estimatedMemory,
                EstimatedDiskBytes = estimatedDisk,
                HardwarePressure = HardwarePressure(estimatedMemory, estimatedDisk, profile.MemoryBudgetBytes, profile.DiskBudgetBytes)
            };
        }

        private static string HardwarePressure(long memory, long disk, long memoryBudget, long diskBudget)
        {
            if (memoryBudget <= 0 || diskBudget <= 0) return "unknown";
            if (memory > memoryBudget || disk > diskBudget) return "over_budget";
            if (RatioAtLeast(memory, memoryBudget, 3, 4) || RatioAtLeast(disk, diskBudget, 3, 4)) return "high";
            if (RatioAtLeast(memory, memoryBudget, 1, 2) || RatioAtLeast(disk, diskBudget, 1, 2)) return "moderate";
            return "low";
        }

        private static bool RatioAtLeast(long value, long budget, long numerator, long denominator)
        {
            long quotient = budget / denominator;
            long remainder = budget % denominator;
            long threshold = (quotient * numerator) + ((remainder * numerator + denominator - 1L) / denominator);
            return value >= threshold;
        }

        internal static long Scale(long value, int basisPoints)
        {
            long quotient = value / 10_000L;
            long remainder = value % 10_000L;
            return SaturatingAdd(SaturatingMultiply(quotient, basisPoints), ((remainder * basisPoints) + 5_000L) / 10_000L);
        }

        private static long SaturatingAdd(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
            return left + right;
        }

        private static long SaturatingMultiply(long left, long right)
        {
            if (left == 0 || right == 0) return 0;
            if (left > long.MaxValue / right) return long.MaxValue;
            return left * right;
        }
    }

    internal static class LifecycleFormat
    {
        public const string SchemaVersion = "m15.p5.lifecycle.v1";
        public const int FileHeaderBytes = 20;
        public const int CoreRecordBytes = 41;
        public const int ColdRecordBytes = 13;
        public const int HotRecordBytes = 45;
        public static readonly byte[] CoreMagic = Encoding.ASCII.GetBytes("M15P5CR1");
        public static readonly byte[] ColdMagic = Encoding.ASCII.GetBytes("M15P5CD1");
        public static readonly byte[] HotMagic = Encoding.ASCII.GetBytes("M15P5HT1");
    }

    internal sealed class PersonCoreRecord
    {
        public long PersonId;
        public long FatherId;
        public long MotherId;
        public long HouseholdId;
        public int BirthDay;
        public int BirthRegion;
        public byte Gender;
    }

    internal sealed class HotPersonRecord
    {
        public PersonCoreRecord Core;
        public int NextLifecycleDay;
    }

    internal sealed class ColdDeathRecord
    {
        public long PersonId;
        public int DeathDay;
        public byte Reason;
    }

    internal sealed class LifecycleManifest
    {
        public string SchemaVersion { get; set; }
        public long Seed { get; set; }
        public long InitialLivingPopulation { get; set; }
        public long CurrentLivingPopulation { get; set; }
        public long PeakLivingPopulation { get; set; }
        public long CumulativePersonCount { get; set; }
        public long DeceasedColdArchiveCount { get; set; }
        public long TotalBirths { get; set; }
        public long TotalDeaths { get; set; }
        public long CurrentDay { get; set; }
        public int HotCursor { get; set; }
        public int BirthsPerDay { get; set; }
        public int RegionCount { get; set; }
        public int HotSlot { get; set; }
        public string CoreSha256 { get; set; }
        public string HotSha256 { get; set; }
        public string ColdSha256 { get; set; }
    }

    internal sealed class LifecycleRunEvidence
    {
        public string SchemaVersion { get; set; }
        public string Status { get; set; }
        public long Seed { get; set; }
        public long InitialLivingPopulation { get; set; }
        public long CurrentLivingPopulation { get; set; }
        public long PeakLivingPopulation { get; set; }
        public long CumulativePersonCount { get; set; }
        public long DeceasedColdArchiveCount { get; set; }
        public long TotalBirths { get; set; }
        public long TotalDeaths { get; set; }
        public long CurrentDay { get; set; }
        public long CoreBytes { get; set; }
        public long HotBytes { get; set; }
        public long ColdBytes { get; set; }
        public string CoreSha256 { get; set; }
        public string HotSha256 { get; set; }
        public string ColdSha256 { get; set; }
        public bool ConservationPassed { get; set; }
        public bool ColdQueryPassed { get; set; }
        public bool HotQueryPassed { get; set; }
        public bool ParentQueryPassed { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public CapacityEstimate CapacityEstimate { get; set; }
    }

    internal sealed class LifecycleStore : IDisposable
    {
        private readonly string _workspace;
        private readonly string _manifestPath;
        private readonly string _corePath;
        private readonly string _coldPath;
        private readonly string _progressPath;
        private readonly int _batchRecords;
        private LifecycleManifest _manifest;
        private HotPersonRecord[] _hot;

        private LifecycleStore(string workspace, int batchRecords, string progressPath)
        {
            _workspace = workspace;
            _manifestPath = Path.Combine(workspace, "manifest.json");
            _corePath = Path.Combine(workspace, "permanent-core.bin");
            _coldPath = Path.Combine(workspace, "deceased-cold.bin");
            _progressPath = progressPath;
            _batchRecords = batchRecords;
        }

        public static LifecycleStore OpenOrCreate(string workspace, int initialLiving, long seed, int batchRecords, string progressPath)
        {
            if (initialLiving <= 1) throw new ArgumentOutOfRangeException(nameof(initialLiving));
            if (batchRecords <= 0) throw new ArgumentOutOfRangeException(nameof(batchRecords));
            Directory.CreateDirectory(workspace);
            var store = new LifecycleStore(workspace, batchRecords, progressPath);
            if (File.Exists(store._manifestPath)) store.Load(seed, initialLiving);
            else store.Create(seed, initialLiving);
            store.ValidateState();
            store.WriteProgress("ready");
            return store;
        }

        public void AdvanceTo(long targetCumulative)
        {
            if (targetCumulative < _manifest.CumulativePersonCount)
            {
                throw new InvalidOperationException("Target cumulative population cannot be below the durable checkpoint.");
            }
            while (_manifest.CumulativePersonCount < targetCumulative)
            {
                long remaining = targetCumulative - _manifest.CumulativePersonCount;
                int count = (int)Math.Min(remaining, _batchRecords);
                AppendLifecycleBatch(count);
                int nextHotSlot = 1 - _manifest.HotSlot;
                SaveHot(nextHotSlot);
                _manifest.HotSlot = nextHotSlot;
                SaveManifest(false);
                WriteProgress("checkpoint");
            }

            ValidateState();
            _manifest.CoreSha256 = HashFile(_corePath);
            _manifest.HotSha256 = HashFile(CurrentHotPath());
            _manifest.ColdSha256 = HashFile(_coldPath);
            SaveManifest(true);
            WriteProgress("completed");
        }

        public LifecycleRunEvidence BuildEvidence(CapacityEstimate estimate, long elapsedMilliseconds)
        {
            ValidateState();
            long deadProbeId = _manifest.TotalDeaths == 0 ? 0 : Math.Min(_manifest.TotalDeaths, Math.Max(1L, _manifest.TotalDeaths / 2L));
            long hotProbeId = _manifest.CumulativePersonCount;
            bool coldPassed = deadProbeId == 0 || QueryCold(deadProbeId).PersonId == deadProbeId;
            PersonCoreRecord deadCore = deadProbeId == 0 ? null : QueryCore(deadProbeId);
            HotPersonRecord hot = QueryHot(hotProbeId);
            bool parentPassed = hot.Core.FatherId > 0 && hot.Core.MotherId > 0 &&
                QueryCore(hot.Core.FatherId).PersonId == hot.Core.FatherId &&
                QueryCore(hot.Core.MotherId).PersonId == hot.Core.MotherId;
            if (deadCore != null && deadCore.PersonId != deadProbeId) coldPassed = false;

            return new LifecycleRunEvidence
            {
                SchemaVersion = LifecycleFormat.SchemaVersion + ".evidence.v1",
                Status = "passed",
                Seed = _manifest.Seed,
                InitialLivingPopulation = _manifest.InitialLivingPopulation,
                CurrentLivingPopulation = _manifest.CurrentLivingPopulation,
                PeakLivingPopulation = _manifest.PeakLivingPopulation,
                CumulativePersonCount = _manifest.CumulativePersonCount,
                DeceasedColdArchiveCount = _manifest.DeceasedColdArchiveCount,
                TotalBirths = _manifest.TotalBirths,
                TotalDeaths = _manifest.TotalDeaths,
                CurrentDay = _manifest.CurrentDay,
                CoreBytes = new FileInfo(_corePath).Length,
                HotBytes = new FileInfo(CurrentHotPath()).Length,
                ColdBytes = new FileInfo(_coldPath).Length,
                CoreSha256 = _manifest.CoreSha256,
                HotSha256 = _manifest.HotSha256,
                ColdSha256 = _manifest.ColdSha256,
                ConservationPassed = ConservationPassed(),
                ColdQueryPassed = coldPassed,
                HotQueryPassed = hot.Core.PersonId == hotProbeId,
                ParentQueryPassed = parentPassed,
                ElapsedMilliseconds = elapsedMilliseconds,
                CapacityEstimate = estimate
            };
        }

        private void Create(long seed, int initialLiving)
        {
            _manifest = new LifecycleManifest
            {
                SchemaVersion = LifecycleFormat.SchemaVersion,
                Seed = seed,
                InitialLivingPopulation = initialLiving,
                CurrentLivingPopulation = initialLiving,
                PeakLivingPopulation = initialLiving,
                CumulativePersonCount = initialLiving,
                DeceasedColdArchiveCount = 0,
                TotalBirths = 0,
                TotalDeaths = 0,
                CurrentDay = 0,
                HotCursor = 0,
                BirthsPerDay = Math.Max(1, initialLiving / 365),
                RegionCount = 1_182,
                HotSlot = 0
            };
            _hot = new HotPersonRecord[initialLiving];
            using (var coreStream = new FileStream(_corePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var core = new BinaryWriter(coreStream, Encoding.UTF8))
            {
                WriteHeader(core, LifecycleFormat.CoreMagic, initialLiving);
                for (var index = 0; index < initialLiving; index++)
                {
                    long id = index + 1L;
                    var person = InitialPerson(id, seed, _manifest.RegionCount);
                    WriteCore(core, person);
                    _hot[index] = new HotPersonRecord { Core = person, NextLifecycleDay = NextDay(id, seed) };
                }
            }
            using (var coldStream = new FileStream(_coldPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var cold = new BinaryWriter(coldStream, Encoding.UTF8)) { WriteHeader(cold, LifecycleFormat.ColdMagic, 0); }
            SaveHot(0);
            SaveManifest(false);
        }

        private void Load(long seed, int initialLiving)
        {
            _manifest = JsonConvert.DeserializeObject<LifecycleManifest>(File.ReadAllText(_manifestPath, Encoding.UTF8));
            if (_manifest == null || _manifest.SchemaVersion != LifecycleFormat.SchemaVersion) throw new InvalidDataException("Unsupported lifecycle manifest.");
            if (_manifest.Seed != seed || _manifest.InitialLivingPopulation != initialLiving) throw new InvalidOperationException("Resume seed or initial population does not match the durable checkpoint.");
            RecoverToManifest();
            _hot = ReadHot();
        }

        private void RecoverToManifest()
        {
            ValidateRecoverableFile(_corePath, LifecycleFormat.CoreMagic, _manifest.CumulativePersonCount, LifecycleFormat.CoreRecordBytes);
            ValidateRecoverableFile(_coldPath, LifecycleFormat.ColdMagic, _manifest.TotalDeaths, LifecycleFormat.ColdRecordBytes);
            ValidateExactFile(CurrentHotPath(), LifecycleFormat.HotMagic, _manifest.CurrentLivingPopulation, LifecycleFormat.HotRecordBytes);
            WriteRecordCount(_corePath, _manifest.CumulativePersonCount);
            WriteRecordCount(_coldPath, _manifest.TotalDeaths);
        }

        private static void ValidateRecoverableFile(string path, byte[] magic, long manifestCount, int recordBytes)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Lifecycle checkpoint file is missing.", path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                long headerCount = ReadHeader(reader, magic);
                if (headerCount < manifestCount) throw new InvalidDataException("Lifecycle checkpoint header is behind the manifest: " + path);
                long expected = LifecycleFormat.FileHeaderBytes + (manifestCount * recordBytes);
                if (stream.Length < expected) throw new InvalidDataException("Lifecycle checkpoint file is truncated: " + path);
                if (stream.Length > expected) stream.SetLength(expected);
            }
        }

        private static void ValidateExactFile(string path, byte[] magic, long manifestCount, int recordBytes)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Lifecycle checkpoint file is missing.", path);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                long headerCount = ReadHeader(reader, magic);
                long expected = LifecycleFormat.FileHeaderBytes + (manifestCount * recordBytes);
                if (headerCount != manifestCount || stream.Length != expected) throw new InvalidDataException("Lifecycle checkpoint file does not match the manifest: " + path);
            }
        }

        private void AppendLifecycleBatch(int count)
        {
            using (var coreStream = new FileStream(_corePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var coldStream = new FileStream(_coldPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var core = new BinaryWriter(coreStream, Encoding.UTF8))
            using (var cold = new BinaryWriter(coldStream, Encoding.UTF8))
            {
                coreStream.Position = coreStream.Length;
                coldStream.Position = coldStream.Length;
                for (var step = 0; step < count; step++)
                {
                    int victimIndex = _manifest.HotCursor;
                    HotPersonRecord victim = _hot[victimIndex];
                    long birthOrdinal = _manifest.TotalBirths + 1L;
                    int currentDay = (int)Math.Min(int.MaxValue, birthOrdinal / _manifest.BirthsPerDay);
                    cold.Write(victim.Core.PersonId);
                    cold.Write(currentDay);
                    cold.Write((byte)(1 + (StableMix((ulong)_manifest.Seed ^ (ulong)victim.Core.PersonId) % 4UL)));

                    long nextId = _manifest.CumulativePersonCount + 1L;
                    HotPersonRecord father = _hot[(victimIndex + 1) % _hot.Length];
                    HotPersonRecord mother = _hot[(victimIndex + 2) % _hot.Length];
                    var child = new PersonCoreRecord
                    {
                        PersonId = nextId,
                        FatherId = father.Core.PersonId,
                        MotherId = mother.Core.PersonId,
                        HouseholdId = father.Core.HouseholdId,
                        BirthDay = currentDay,
                        BirthRegion = (int)(StableMix((ulong)_manifest.Seed ^ (ulong)nextId) % (ulong)_manifest.RegionCount),
                        Gender = (byte)(StableMix((ulong)nextId) & 1UL)
                    };
                    WriteCore(core, child);
                    _hot[victimIndex] = new HotPersonRecord { Core = child, NextLifecycleDay = currentDay + NextDay(nextId, _manifest.Seed) };

                    _manifest.HotCursor = (victimIndex + 1) % _hot.Length;
                    _manifest.TotalBirths++;
                    _manifest.TotalDeaths++;
                    _manifest.CumulativePersonCount++;
                    _manifest.DeceasedColdArchiveCount++;
                    _manifest.CurrentDay = currentDay;
                }
                UpdateHeaderCount(coreStream, _manifest.CumulativePersonCount);
                UpdateHeaderCount(coldStream, _manifest.TotalDeaths);
                coreStream.Flush(true);
                coldStream.Flush(true);
            }
        }

        private void SaveHot(int slot)
        {
            string destination = HotPath(slot);
            string temporary = destination + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteHeader(writer, LifecycleFormat.HotMagic, _hot.LongLength);
                for (var index = 0; index < _hot.Length; index++)
                {
                    WriteCore(writer, _hot[index].Core);
                    writer.Write(_hot[index].NextLifecycleDay);
                }
                stream.Flush(true);
            }
            ReplaceFile(temporary, destination);
        }

        private HotPersonRecord[] ReadHot()
        {
            using (var stream = new FileStream(CurrentHotPath(), FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                long count = ReadHeader(reader, LifecycleFormat.HotMagic);
                if (count != _manifest.CurrentLivingPopulation || count > int.MaxValue) throw new InvalidDataException("Hot index count does not match the manifest.");
                var values = new HotPersonRecord[(int)count];
                for (var index = 0; index < values.Length; index++) values[index] = new HotPersonRecord { Core = ReadCore(reader), NextLifecycleDay = reader.ReadInt32() };
                if (stream.Position != stream.Length) throw new InvalidDataException("Hot index contains trailing bytes.");
                return values;
            }
        }

        private PersonCoreRecord QueryCore(long personId)
        {
            if (personId <= 0 || personId > _manifest.CumulativePersonCount) throw new ArgumentOutOfRangeException(nameof(personId));
            using (var stream = new FileStream(_corePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = LifecycleFormat.FileHeaderBytes + ((personId - 1L) * LifecycleFormat.CoreRecordBytes);
                PersonCoreRecord value = ReadCore(reader);
                if (value.PersonId != personId) throw new InvalidDataException("Permanent core direct index is inconsistent.");
                return value;
            }
        }

        private ColdDeathRecord QueryCold(long personId)
        {
            if (personId <= 0 || personId > _manifest.TotalDeaths) throw new ArgumentOutOfRangeException(nameof(personId));
            using (var stream = new FileStream(_coldPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                stream.Position = LifecycleFormat.FileHeaderBytes + ((personId - 1L) * LifecycleFormat.ColdRecordBytes);
                var value = new ColdDeathRecord { PersonId = reader.ReadInt64(), DeathDay = reader.ReadInt32(), Reason = reader.ReadByte() };
                if (value.PersonId != personId) throw new InvalidDataException("Cold archive direct index is inconsistent.");
                return value;
            }
        }

        private HotPersonRecord QueryHot(long personId)
        {
            HotPersonRecord value = _hot.FirstOrDefault(item => item.Core.PersonId == personId);
            if (value == null) throw new InvalidDataException("Living person is missing from the hot index.");
            return value;
        }

        private void ValidateState()
        {
            if (!ConservationPassed()) throw new InvalidDataException("Lifecycle population conservation failed.");
            if (_hot == null || _hot.LongLength != _manifest.CurrentLivingPopulation) throw new InvalidDataException("Hot population count is inconsistent.");
            if (_manifest.HotCursor < 0 || _manifest.HotCursor >= _hot.Length) throw new InvalidDataException("Hot cursor is outside the living index.");
            ValidateLength(_corePath, LifecycleFormat.FileHeaderBytes + (_manifest.CumulativePersonCount * LifecycleFormat.CoreRecordBytes));
            ValidateLength(_coldPath, LifecycleFormat.FileHeaderBytes + (_manifest.TotalDeaths * LifecycleFormat.ColdRecordBytes));
            ValidateExactFile(CurrentHotPath(), LifecycleFormat.HotMagic, _manifest.CurrentLivingPopulation, LifecycleFormat.HotRecordBytes);
        }

        private bool ConservationPassed()
        {
            return _manifest.CumulativePersonCount == _manifest.InitialLivingPopulation + _manifest.TotalBirths &&
                _manifest.CurrentLivingPopulation == _manifest.CumulativePersonCount - _manifest.TotalDeaths &&
                _manifest.DeceasedColdArchiveCount == _manifest.TotalDeaths &&
                _manifest.PeakLivingPopulation >= _manifest.CurrentLivingPopulation;
        }

        private void SaveManifest(bool includeHashes)
        {
            if (!includeHashes)
            {
                _manifest.CoreSha256 = null;
                _manifest.HotSha256 = null;
                _manifest.ColdSha256 = null;
            }
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
                schema_version = "m15.p5.progress.v1",
                phase,
                cumulative_person_count = _manifest.CumulativePersonCount,
                current_living_population = _manifest.CurrentLivingPopulation,
                deceased_cold_archive_count = _manifest.DeceasedColdArchiveCount,
                updated_at_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            File.WriteAllText(_progressPath, JsonConvert.SerializeObject(value), new UTF8Encoding(false));
        }

        private static PersonCoreRecord InitialPerson(long id, long seed, int regionCount)
        {
            return new PersonCoreRecord
            {
                PersonId = id,
                FatherId = 0,
                MotherId = 0,
                HouseholdId = ((id - 1L) / 5L) + 1L,
                BirthDay = -(int)(StableMix((ulong)seed ^ (ulong)id) % 18_250UL),
                BirthRegion = (int)(StableMix((ulong)seed ^ ((ulong)id * 17UL)) % (ulong)regionCount),
                Gender = (byte)(StableMix((ulong)id) & 1UL)
            };
        }

        private static int NextDay(long id, long seed) { return 30 + (int)(StableMix((ulong)seed ^ (ulong)id) % 365UL); }

        private static ulong StableMix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }

        private static void WriteHeader(BinaryWriter writer, byte[] magic, long count)
        {
            writer.Write(magic);
            writer.Write(1);
            writer.Write(count);
        }

        private static long ReadHeader(BinaryReader reader, byte[] expectedMagic)
        {
            byte[] magic = reader.ReadBytes(expectedMagic.Length);
            if (!magic.SequenceEqual(expectedMagic) || reader.ReadInt32() != 1) throw new InvalidDataException("Lifecycle file header is invalid.");
            return reader.ReadInt64();
        }

        private static void UpdateHeaderCount(FileStream stream, long count)
        {
            long position = stream.Position;
            stream.Position = 12;
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) writer.Write(count);
            stream.Position = position;
        }

        private static void WriteRecordCount(string path, long count)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read)) UpdateHeaderCount(stream, count);
        }

        private static void WriteCore(BinaryWriter writer, PersonCoreRecord value)
        {
            writer.Write(value.PersonId);
            writer.Write(value.FatherId);
            writer.Write(value.MotherId);
            writer.Write(value.HouseholdId);
            writer.Write(value.BirthDay);
            writer.Write(value.BirthRegion);
            writer.Write(value.Gender);
        }

        private static PersonCoreRecord ReadCore(BinaryReader reader)
        {
            return new PersonCoreRecord
            {
                PersonId = reader.ReadInt64(),
                FatherId = reader.ReadInt64(),
                MotherId = reader.ReadInt64(),
                HouseholdId = reader.ReadInt64(),
                BirthDay = reader.ReadInt32(),
                BirthRegion = reader.ReadInt32(),
                Gender = reader.ReadByte()
            };
        }

        private static string HashFile(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void ReplaceFile(string temporary, string destination)
        {
            if (File.Exists(destination)) File.Replace(temporary, destination, null, true);
            else File.Move(temporary, destination);
        }

        private static void ValidateLength(string path, long expected)
        {
            long actual = new FileInfo(path).Length;
            if (actual != expected) throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unexpected file length for {0}: expected {1}, actual {2}.", path, expected, actual));
        }

        public void Dispose() { }

        private string HotPath(int slot)
        {
            if (slot != 0 && slot != 1) throw new InvalidDataException("Hot index slot must be zero or one.");
            return Path.Combine(_workspace, "living-hot-" + slot.ToString(CultureInfo.InvariantCulture) + ".bin");
        }

        private string CurrentHotPath() { return HotPath(_manifest.HotSlot); }
    }

    internal sealed class LifecycleOptions
    {
        public bool SelfTest { get; private set; }
        public string WorkspacePath { get; private set; }
        public string OutputPath { get; private set; }
        public string ProgressPath { get; private set; }
        public int InitialLivingPopulation { get; private set; }
        public long TargetCumulativePopulation { get; private set; }
        public int BatchRecords { get; private set; }
        public long Seed { get; private set; }
        public long HistoricalReferencePopulation { get; private set; }
        public int PopulationScaleBasisPoints { get; private set; }
        public int ProjectionYears { get; private set; }
        public int AnnualBirthRateBasisPoints { get; private set; }
        public int AnnualDeathRateBasisPoints { get; private set; }
        public long MemoryBudgetBytes { get; private set; }
        public long DiskBudgetBytes { get; private set; }

        public static LifecycleOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool selfTest = false;
            for (var index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], "--self-test", StringComparison.OrdinalIgnoreCase)) { selfTest = true; continue; }
                if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length) throw new ArgumentException("Invalid lifecycle argument: " + args[index]);
                values.Add(args[index], args[++index]);
            }

            string output = Required(values, "--output");
            if (selfTest) return new LifecycleOptions { SelfTest = true, OutputPath = output };
            long historical = ParseLong(values, "--historical-reference", 50_000_000L);
            int scale = ParseInt(values, "--scale-basis-points", 20);
            int initial = ParseInt(values, "--initial-living", (int)PopulationCapacityEstimator.Scale(historical, scale));
            return new LifecycleOptions
            {
                WorkspacePath = Required(values, "--workspace"),
                OutputPath = output,
                ProgressPath = Optional(values, "--progress"),
                InitialLivingPopulation = initial,
                TargetCumulativePopulation = ParseLong(values, "--target-cumulative", -1),
                BatchRecords = ParseInt(values, "--batch-records", 250_000),
                Seed = ParseLong(values, "--seed", 14000015L),
                HistoricalReferencePopulation = historical,
                PopulationScaleBasisPoints = scale,
                ProjectionYears = ParseInt(values, "--projection-years", 125),
                AnnualBirthRateBasisPoints = ParseInt(values, "--birth-rate-bp", 300),
                AnnualDeathRateBasisPoints = ParseInt(values, "--death-rate-bp", 300),
                MemoryBudgetBytes = ParseLong(values, "--memory-budget-bytes", 8L * 1024L * 1024L * 1024L),
                DiskBudgetBytes = ParseLong(values, "--disk-budget-bytes", 50L * 1024L * 1024L * 1024L)
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

        private static int ParseInt(IDictionary<string, string> values, string key, int fallback)
        {
            long parsed = ParseLong(values, key, fallback);
            if (parsed < int.MinValue || parsed > int.MaxValue) throw new ArgumentOutOfRangeException(key);
            return (int)parsed;
        }
    }

    internal static class LifecycleSelfTests
    {
        public static void Run(string outputPath)
        {
            int passed = 0;
            var names = new List<string>();
            RunTest("scale_uses_historical_reference", () =>
            {
                CapacityEstimate value = Estimate(56_486_856L, 100, 125, 300, 300);
                Equal(564_869L, value.InitialLivingPopulation);
            }, names, ref passed);
            RunTest("projection_requires_horizon", () =>
            {
                bool rejected = false;
                try { Estimate(1_000_000L, 1_000, 0, 300, 300); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                True(rejected, "Zero-year projection was accepted.");
            }, names, ref passed);
            RunTest("capacity_estimate_is_deterministic", () =>
            {
                CapacityEstimate first = Estimate(50_000_000L, 20, 125, 300, 300);
                CapacityEstimate second = Estimate(50_000_000L, 20, 125, 300, 300);
                Equal(JsonConvert.SerializeObject(first), JsonConvert.SerializeObject(second));
            }, names, ref passed);
            RunTest("warning_does_not_block_births", () =>
            {
                CapacityEstimate value = Estimate(50_000_000L, 10_000, 2, 1_000, 0);
                True(value.ProjectedCumulativePersonCount > 50_000_000L, "Test did not exceed the official target.");
                True(value.PerformanceWarning, "Performance warning was not raised.");
                True(value.BirthsAllowed, "Capacity warning blocked births.");
                Equal("beyond_official_guarantee", value.SupportStatus);
            }, names, ref passed);
            RunTest("hot_cold_conservation_and_queries", () =>
            {
                string root = TemporaryDirectory("conservation");
                try
                {
                    using (var store = LifecycleStore.OpenOrCreate(root, 100, 17L, 73, null))
                    {
                        store.AdvanceTo(500);
                        LifecycleRunEvidence evidence = store.BuildEvidence(Estimate(10_000L, 1_000, 10, 300, 300), 0);
                        Equal(500L, evidence.CumulativePersonCount);
                        Equal(100L, evidence.CurrentLivingPopulation);
                        Equal(400L, evidence.DeceasedColdArchiveCount);
                        True(evidence.ConservationPassed && evidence.ColdQueryPassed && evidence.HotQueryPassed && evidence.ParentQueryPassed, "Lifecycle query or conservation failed.");
                    }
                }
                finally { DeleteOwnedTemporary(root); }
            }, names, ref passed);
            RunTest("resume_matches_continuous_digest", () =>
            {
                string resumed = TemporaryDirectory("resumed");
                string continuous = TemporaryDirectory("continuous");
                try
                {
                    using (var store = LifecycleStore.OpenOrCreate(resumed, 120, 29L, 97, null)) store.AdvanceTo(700);
                    LifecycleRunEvidence resumedEvidence;
                    using (var store = LifecycleStore.OpenOrCreate(resumed, 120, 29L, 89, null)) { store.AdvanceTo(1_200); resumedEvidence = store.BuildEvidence(Estimate(12_000L, 1_000, 10, 300, 300), 0); }
                    LifecycleRunEvidence continuousEvidence;
                    using (var store = LifecycleStore.OpenOrCreate(continuous, 120, 29L, 1_080, null)) { store.AdvanceTo(1_200); continuousEvidence = store.BuildEvidence(Estimate(12_000L, 1_000, 10, 300, 300), 0); }
                    Equal(continuousEvidence.CoreSha256, resumedEvidence.CoreSha256);
                    Equal(continuousEvidence.HotSha256, resumedEvidence.HotSha256);
                    Equal(continuousEvidence.ColdSha256, resumedEvidence.ColdSha256);
                }
                finally { DeleteOwnedTemporary(resumed); DeleteOwnedTemporary(continuous); }
            }, names, ref passed);
            RunTest("resume_rejects_changed_seed", () =>
            {
                string root = TemporaryDirectory("seed");
                try
                {
                    using (var store = LifecycleStore.OpenOrCreate(root, 50, 41L, 10, null)) store.AdvanceTo(75);
                    bool rejected = false;
                    try { using (LifecycleStore.OpenOrCreate(root, 50, 42L, 10, null)) { } }
                    catch (InvalidOperationException) { rejected = true; }
                    True(rejected, "Resume accepted a changed seed.");
                }
                finally { DeleteOwnedTemporary(root); }
            }, names, ref passed);
            RunTest("interrupted_append_recovers_to_manifest", () =>
            {
                string root = TemporaryDirectory("recovery");
                try
                {
                    using (var store = LifecycleStore.OpenOrCreate(root, 100, 53L, 75, null)) store.AdvanceTo(300);
                    AppendUncommittedRecord(Path.Combine(root, "permanent-core.bin"), LifecycleFormat.CoreRecordBytes, 301L);
                    AppendUncommittedRecord(Path.Combine(root, "deceased-cold.bin"), LifecycleFormat.ColdRecordBytes, 201L);
                    using (var store = LifecycleStore.OpenOrCreate(root, 100, 53L, 75, null))
                    {
                        LifecycleRunEvidence evidence = store.BuildEvidence(Estimate(10_000L, 1_000, 10, 300, 300), 0);
                        Equal(300L, evidence.CumulativePersonCount);
                        Equal(200L, evidence.DeceasedColdArchiveCount);
                        True(evidence.ConservationPassed, "Recovery changed population conservation.");
                    }
                    Equal(
                        LifecycleFormat.FileHeaderBytes + (300L * LifecycleFormat.CoreRecordBytes),
                        new FileInfo(Path.Combine(root, "permanent-core.bin")).Length);
                    Equal(
                        LifecycleFormat.FileHeaderBytes + (200L * LifecycleFormat.ColdRecordBytes),
                        new FileInfo(Path.Combine(root, "deceased-cold.bin")).Length);
                }
                finally { DeleteOwnedTemporary(root); }
            }, names, ref passed);

            var evidence = new { schema_version = "m15.p5.self-test.v1", status = "passed", passed, failed = 0, tests = names };
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, JsonConvert.SerializeObject(evidence, Formatting.Indented), new UTF8Encoding(false));
            Console.WriteLine("RESULT passed={0} failed=0", passed);
        }

        private static CapacityEstimate Estimate(long historical, int scale, int years, int births, int deaths)
        {
            return PopulationCapacityEstimator.Estimate(new PopulationCapacityProfile
            {
                HistoricalReferencePopulation = historical,
                PopulationScaleBasisPoints = scale,
                ProjectionYears = years,
                AnnualBirthRateBasisPoints = births,
                AnnualDeathRateBasisPoints = deaths,
                OfficialSupportedCumulativeTarget = 50_000_000L,
                WarningThresholdBasisPoints = 9_000,
                MemoryBudgetBytes = 8L * 1024L * 1024L * 1024L,
                DiskBudgetBytes = 50L * 1024L * 1024L * 1024L
            });
        }

        private static void RunTest(string name, Action action, ICollection<string> names, ref int passed)
        {
            action();
            names.Add(name);
            passed++;
        }

        private static string TemporaryDirectory(string suffix)
        {
            string path = Path.Combine(Path.GetTempPath(), "mandate-m15-p5-" + suffix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteOwnedTemporary(string path)
        {
            string full = Path.GetFullPath(path);
            string temp = Path.GetFullPath(Path.GetTempPath());
            if (!full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(full).IndexOf("mandate-m15-p5-", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new InvalidOperationException("Refusing to remove a non-P5 temporary directory.");
            }
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }

        private static void AppendUncommittedRecord(string path, int recordBytes, long headerCount)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                stream.Position = stream.Length;
                stream.Write(new byte[recordBytes], 0, recordBytes);
                stream.Position = 12;
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) writer.Write(headerCount);
                stream.Flush(true);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
