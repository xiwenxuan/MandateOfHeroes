using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    /// <summary>
    /// Read-only composite over the protected 400K metropolitan package and the
    /// additive outer-supply closure package. The composite is the sole source
    /// used by the formal living-world runtime; it does not create a second
    /// simulator or rewrite either initialization package.
    /// </summary>
    public sealed class Luoyang184OuterSupplyRemediationPopulationSource :
        ILuoyang184LivingWorldSource,
        IPermanentPopulationStore
    {
        public const string PopulationPackageId =
            "population.package.luoyang.184.outer_supply_remediation.v1";
        public const string ExpectedSchema =
            "mandate.luoyang-outer-supply-remediation.v1";
        public const int PartitionCount = 56;

        private const int HeaderSize = 32;
        private const int PersonRecordSize = 80;
        private const int HouseholdRecordSize = 32;
        private const string PersonMagic = "MOHLYR01";
        private const string HouseholdMagic = "MOHLYS01";

        private readonly string rootPath;
        private readonly string baseRootPath;
        private readonly JObject manifestToken;
        private readonly Luoyang184LivingWorldSourceAdapter baseSource;
        private readonly Luoyang184MetropolitanPopulationStore baseStore;
        private readonly List<JObject> facilityTokens;
        private readonly List<Luoyang184LivingFacilitySourceRecord> facilities;
        private readonly PopulationPackageManifest populationManifest;
        private readonly int basePersonCount;
        private readonly int addedPersonCount;
        private readonly int baseHouseholdCount;
        private readonly int addedHouseholdCount;
        private readonly int baseFacilityCount;
        private readonly int addedFacilityCount;

        public Luoyang184OuterSupplyRemediationPopulationSource(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("A remediation package root is required.",
                    nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            manifestToken = JObject.Parse(File.ReadAllText(
                Path.Combine(this.rootPath, "manifest.json"), Encoding.UTF8));
            if (!string.Equals(Text(manifestToken, "schema"), ExpectedSchema,
                    StringComparison.Ordinal) ||
                Integer(manifestToken, "format_version") != 1)
                throw new InvalidDataException(
                    "Unsupported Luoyang outer-supply remediation package.");

            baseRootPath = Path.GetFullPath(Path.Combine(this.rootPath,
                Text(manifestToken, "base_package_relative_path")));
            baseSource = new Luoyang184LivingWorldSourceAdapter(baseRootPath);
            baseStore = new Luoyang184MetropolitanPopulationStore(baseRootPath);
            basePersonCount = Integer(manifestToken, "base_person_count");
            addedPersonCount = Integer(manifestToken, "added_person_count");
            baseHouseholdCount = Integer(manifestToken, "base_household_count");
            addedHouseholdCount = Integer(manifestToken, "added_household_count");
            baseFacilityCount = Integer(manifestToken, "base_facility_count");
            addedFacilityCount = Integer(manifestToken, "added_facility_count");
            if (basePersonCount != baseSource.PersonCount ||
                baseHouseholdCount != baseSource.HouseholdCount ||
                baseFacilityCount != baseSource.FacilityCount ||
                PersonCount != Integer(manifestToken, "person_count") ||
                HouseholdCount != Integer(manifestToken, "household_count") ||
                FacilityCount != Integer(manifestToken, "facility_count") ||
                PersonCount != Integer(manifestToken,
                    "inclusive_population_target") ||
                Integer(manifestToken, "person_record_size") != PersonRecordSize ||
                Integer(manifestToken, "household_record_size") != HouseholdRecordSize)
                throw new InvalidDataException(
                    "Luoyang remediation counts do not extend the protected baseline.");

            ValidateHeader(Path.Combine(this.rootPath, "persons.bin"),
                PersonMagic, PersonRecordSize, addedPersonCount);
            ValidateHeader(Path.Combine(this.rootPath, "households.bin"),
                HouseholdMagic, HouseholdRecordSize, addedHouseholdCount);
            facilityTokens = ReadArray(Path.Combine(this.rootPath,
                "facilities.json"), "facilities");
            if (facilityTokens.Count != addedFacilityCount)
                throw new InvalidDataException(
                    "Luoyang remediation Facility count mismatch.");
            facilities = baseSource.Facilities.Concat(
                    facilityTokens.Select(ReadFacility))
                .OrderBy(item => item.FacilityIndex).ToList();
            for (var index = 0; index < facilities.Count; index++)
                if (facilities[index].FacilityIndex != index)
                    throw new InvalidDataException(
                        "Luoyang remediation Facility indexes are not contiguous.");

            populationManifest = new PopulationPackageManifest
            {
                PackageId = PopulationPackageId,
                PartitionCount = PartitionCount,
                StorageRevision = 1,
                PermanentPersonCount = PersonCount,
                LivingPersonCount = PersonCount,
                DetailExtensionCount = PersonCount,
                ManifestSha256 = Sha256(Path.Combine(this.rootPath,
                    "manifest.json"))
            };
            var partitionSize = (PersonCount + PartitionCount - 1) /
                                PartitionCount;
            for (var index = 0; index < PartitionCount; index++)
            {
                var start = index * partitionSize;
                var count = Math.Max(0, Math.Min(partitionSize,
                    PersonCount - start));
                populationManifest.Partitions.Add(
                    new PopulationPartitionManifestEntry
                    {
                        PartitionIndex = index,
                        PersonCount = count,
                        LivingPersonCount = count,
                        DetailExtensionCount = count,
                        CoreRelativePath = "adapter://outer-supply/persons/" +
                                           index,
                        DetailRelativePath = "adapter://outer-supply/persons/" +
                                             index
                    });
            }
        }

        public string PackageId => PopulationPackageId;
        public string ProtectedPackageDigest =>
            populationManifest.ManifestSha256;
        public int PersonCount => checked(basePersonCount + addedPersonCount);
        public int HouseholdCount => checked(baseHouseholdCount +
                                             addedHouseholdCount);
        public int FacilityCount => checked(baseFacilityCount +
                                            addedFacilityCount);
        public int AddedPersonCount => addedPersonCount;
        public int AddedHouseholdCount => addedHouseholdCount;
        public int AddedFacilityCount => addedFacilityCount;
        public string RootPath => rootPath;
        public IReadOnlyList<JObject> AddedFacilityTokens => facilityTokens;
        public IReadOnlyList<Luoyang184LivingFacilitySourceRecord> Facilities =>
            facilities;
        public IReadOnlyList<Luoyang184MetropolitanAgricultureRecord> Agriculture =>
            baseSource.Agriculture;
        public IReadOnlyList<Luoyang184MetropolitanSupplyChainRecord> SupplyChains =>
            baseSource.SupplyChains;
        public IReadOnlyList<Luoyang184T4SupplierSourceRecord> ExternalSuppliers =>
            baseSource.ExternalSuppliers;
        public IReadOnlyList<Luoyang184FamilyOrganizationSourceRecord>
            FamilyOrganizations => baseSource.FamilyOrganizations;
        public IReadOnlyList<ulong> DevelopableCellIds =>
            baseSource.DevelopableCellIds;

        public IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(
            int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, PersonCount,
                nameof(startOrdinal));
            var remaining = count;
            var cursor = startOrdinal;
            if (cursor < basePersonCount && remaining > 0)
            {
                var baseCount = Math.Min(remaining, basePersonCount - cursor);
                foreach (var record in baseSource.ReadPersons(cursor, baseCount))
                    yield return record;
                cursor += baseCount;
                remaining -= baseCount;
            }
            if (remaining <= 0) yield break;
            var offset = cursor - basePersonCount;
            using (var stream = File.OpenRead(Path.Combine(rootPath,
                       "persons.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)offset * PersonRecordSize;
                for (var index = 0; index < remaining; index++)
                    yield return ReadPerson(reader);
            }
        }

        public IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(
            int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, HouseholdCount,
                nameof(startOrdinal));
            var remaining = count;
            var cursor = startOrdinal;
            if (cursor < baseHouseholdCount && remaining > 0)
            {
                var baseCount = Math.Min(remaining,
                    baseHouseholdCount - cursor);
                foreach (var record in baseSource.ReadHouseholds(cursor,
                             baseCount))
                    yield return record;
                cursor += baseCount;
                remaining -= baseCount;
            }
            if (remaining <= 0) yield break;
            var offset = cursor - baseHouseholdCount;
            using (var stream = File.OpenRead(Path.Combine(rootPath,
                       "households.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)offset *
                    HouseholdRecordSize;
                for (var index = 0; index < remaining; index++)
                    yield return ReadHousehold(reader);
            }
        }

        public string GetPersonId(uint ordinal)
        {
            if (ordinal >= PersonCount)
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            return ordinal < basePersonCount
                ? baseSource.GetPersonId(ordinal)
                : "person.luoyang.184.outer_supply." +
                  (ordinal + 1).ToString("D6", CultureInfo.InvariantCulture);
        }

        public string GetHouseholdId(uint ordinal)
        {
            if (ordinal >= HouseholdCount)
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            return "household.luoyang.184." +
                   (ordinal + 1).ToString("D6", CultureInfo.InvariantCulture);
        }

        public string GetFacilityId(uint facilityIndex) =>
            facilityIndex == uint.MaxValue || facilityIndex >= facilities.Count
                ? string.Empty
                : facilities[checked((int)facilityIndex)].FacilityId;

        public string GetActivityId(ushort activityIndex) =>
            baseSource.GetActivityId(activityIndex);

        public string GetOccupationId(ushort occupationIndex) =>
            baseSource.GetOccupationId(occupationIndex);

        public IReadOnlyList<string> ValidatePackageFiles()
        {
            var failures = baseStore.Source.ValidatePackageFiles().Select(item =>
                "base/" + item).ToList();
            foreach (var token in manifestToken["files"] ?? new JArray())
            {
                var relative = Text(token, "path");
                var path = Path.Combine(rootPath, relative);
                if (!File.Exists(path))
                {
                    failures.Add(relative + ":missing");
                    continue;
                }
                if (new FileInfo(path).Length != Long(token, "bytes"))
                {
                    failures.Add(relative + ":size");
                    continue;
                }
                if (!string.Equals(Sha256(path), Text(token, "sha256"),
                        StringComparison.Ordinal))
                    failures.Add(relative + ":sha256");
            }
            if (!string.Equals(Sha256(Path.Combine(baseRootPath,
                    "manifest.json")), Text(manifestToken,
                    "base_manifest_sha256"), StringComparison.Ordinal))
                failures.Add("base/manifest.json:sha256");
            return failures;
        }

        public PopulationPackageManifest OpenCurrent() => populationManifest;

        public PopulationPackageManifest CommitCheckpoint(
            PopulationCheckpoint checkpoint) =>
            throw new NotSupportedException(
                "The Luoyang initialization composite is read-only.");

        public PopulationPackageManifest CommitIncrementalCheckpoint(
            PopulationIncrementalCheckpoint checkpoint) =>
            throw new NotSupportedException(
                "The Luoyang initialization composite is read-only.");

        public bool TryReadCore(string personId,
            out PermanentPersonCoreRecord person)
        {
            if (!TryGetOrdinal(personId, out var ordinal))
            {
                person = null;
                return false;
            }
            if (ordinal < basePersonCount)
                return baseStore.TryReadCore(personId, out person);
            var record = ReadPersons(checked((int)ordinal), 1).Single();
            person = ToCore(personId, record);
            return true;
        }

        public bool TryReadDetail(string personId, out PersonState person)
        {
            if (!TryGetOrdinal(personId, out var ordinal))
            {
                person = null;
                return false;
            }
            if (ordinal < basePersonCount)
                return baseStore.TryReadDetail(personId, out person);
            var record = ReadPersons(checked((int)ordinal), 1).Single();
            var core = ToCore(personId, record);
            person = new PersonState
            {
                Id = core.PersonId,
                DisplayName = core.DisplayName,
                LocationId = core.CurrentLocationId,
                BirthLocationId = core.BirthLocationId,
                FamilyId = core.FamilyId,
                BirthDay = core.BirthDay,
                IsAlive = core.IsAlive,
                HealthBasisPoints = core.HealthBasisPoints,
                Wealth = record.PersonalAssets,
                Provisions = 10,
                Gender = core.Gender,
                FatherPersonId = core.FatherPersonId,
                MotherPersonId = core.MotherPersonId,
                SpousePersonId = core.SpousePersonId,
                CountsTowardPopulation = true,
                PopulationOriginLocationId = core.PopulationOriginLocationId,
                VillageOccupation = core.VillageOccupation,
                LaborCapacityBasisPoints = core.LaborCapacityBasisPoints,
                NextIndependentEventDay = -1,
                NextIndependentEventReason = string.Empty,
                LocalDuty = LocalDutyKind.None,
                LocalDutyUntilDay = -1
            };
            return true;
        }

        public IReadOnlyList<PermanentPersonCoreRecord> LoadCorePartition(
            int partitionIndex)
        {
            GetPartitionRange(partitionIndex, out var start, out var count);
            return ReadPersons(start, count).Select(record =>
                ToCore(GetPersonId(record.Ordinal), record)).ToList();
        }

        public IReadOnlyList<PersonDetailExtensionRecord> LoadDetailPartition(
            int partitionIndex)
        {
            var cores = LoadCorePartition(partitionIndex);
            return cores.Select(core =>
            {
                TryReadDetail(core.PersonId, out var detail);
                return new PersonDetailExtensionRecord
                {
                    StorageRevision = 1,
                    Person = detail
                };
            }).ToList();
        }

        public bool TryGetOrdinal(string personId, out uint ordinal)
        {
            if (baseStore.TryGetOrdinal(personId, out ordinal)) return true;
            const string prefix = "person.luoyang.184.outer_supply.";
            var suffix = personId != null && personId.StartsWith(prefix,
                StringComparison.Ordinal)
                ? personId.Substring(prefix.Length)
                : null;
            if (!uint.TryParse(suffix, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var oneBased) ||
                oneBased <= basePersonCount || oneBased > PersonCount)
            {
                ordinal = 0;
                return false;
            }
            ordinal = oneBased - 1;
            return true;
        }

        private PermanentPersonCoreRecord ToCore(string personId,
            Luoyang184PermanentPersonRecord record)
        {
            var location = "cell.id64." + record.CurrentCellId64.ToString(
                CultureInfo.InvariantCulture);
            return new PermanentPersonCoreRecord
            {
                PersonId = personId,
                DisplayName = personId,
                CurrentLocationId = location,
                BirthLocationId = location,
                FamilyId = GetHouseholdId(record.HouseholdOrdinal),
                BirthDay = checked((long)(record.BirthYear - 184) * 360L),
                IsAlive = true,
                HealthBasisPoints = record.HealthBasisPoints,
                Gender = record.Gender == 1
                    ? PersonGender.Male
                    : record.Gender == 2
                        ? PersonGender.Female
                        : PersonGender.Unknown,
                FatherPersonId = record.FatherOrdinal >= 0
                    ? GetPersonId(checked((uint)record.FatherOrdinal))
                    : string.Empty,
                MotherPersonId = record.MotherOrdinal >= 0
                    ? GetPersonId(checked((uint)record.MotherOrdinal))
                    : string.Empty,
                SpousePersonId = record.SpouseOrdinal >= 0
                    ? GetPersonId(checked((uint)record.SpouseOrdinal))
                    : string.Empty,
                CountsTowardPopulation = true,
                PopulationOriginLocationId = location,
                VillageOccupation = VillageOccupation.Unknown,
                LaborCapacityBasisPoints = 10_000,
                NextIndependentEventDay = -1,
                NextIndependentEventReason = string.Empty,
                LocalDuty = LocalDutyKind.None,
                LocalDutyUntilDay = -1
            };
        }

        private Luoyang184LivingFacilitySourceRecord ReadFacility(
            JObject token)
        {
            var index = Integer(token, "global_facility_index");
            if (index < baseFacilityCount || index >= FacilityCount)
                throw new InvalidDataException(
                    "Remediation Facility index is outside the additive range.");
            return new Luoyang184LivingFacilitySourceRecord
            {
                FacilityIndex = index,
                FacilityId = Text(token, "facility_id"),
                DefinitionId = Text(token, "definition_id"),
                CategoryId = Text(token, "category_id"),
                OwnerId = Text(token, "owner_id"),
                ControllerId = Text(token, "controller_id"),
                SettlementId = Text(token, "settlement_id"),
                CellId64 = token["cell_id64"]?.Value<ulong>() ?? 0,
                ResidentCapacity = Integer(token,
                    "residential_capacity_persons"),
                CurrentResidents = Integer(token, "current_residents"),
                WorkerCapacity = Integer(token, "worker_capacity"),
                MinimumWorkers = Integer(token,
                    "minimum_workers_for_normal_operation"),
                CurrentWorkers = Integer(token, "current_workers"),
                StorageCapacity = Long(token, "storage_capacity_units"),
                Operational = token["normal_operation"]?.Value<bool>() != false
            };
        }

        private void GetPartitionRange(int partitionIndex,
            out int start, out int count)
        {
            if (partitionIndex < 0 || partitionIndex >= PartitionCount)
                throw new ArgumentOutOfRangeException(nameof(partitionIndex));
            var size = (PersonCount + PartitionCount - 1) / PartitionCount;
            start = partitionIndex * size;
            count = Math.Max(0, Math.Min(size, PersonCount - start));
        }

        private static Luoyang184PermanentPersonRecord ReadPerson(
            BinaryReader reader) => new Luoyang184PermanentPersonRecord(
            reader.ReadUInt32(), reader.ReadInt16(), reader.ReadByte(),
            reader.ReadByte(), reader.ReadUInt16(), reader.ReadUInt32(),
            reader.ReadUInt16(), reader.ReadUInt64(), reader.ReadUInt32(),
            reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt16(),
            reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
            reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
            reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadInt64(),
            reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadByte(),
            reader.ReadByte(), reader.ReadByte(), reader.ReadByte(),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

        private static Luoyang184HouseholdRecord ReadHousehold(
            BinaryReader reader)
        {
            var ordinal = reader.ReadUInt32();
            var head = reader.ReadUInt32();
            var start = reader.ReadUInt32();
            var count = reader.ReadUInt16();
            var family = reader.ReadUInt16();
            var residence = reader.ReadUInt32();
            var type = reader.ReadByte();
            var origin = reader.ReadByte();
            reader.ReadUInt16();
            var wealth = reader.ReadInt64();
            return new Luoyang184HouseholdRecord(ordinal, head, start, count,
                family, residence, type, origin, wealth);
        }

        private static void ValidateHeader(string path, string expectedMagic,
            int expectedRecordSize, int expectedCount)
        {
            using (var reader = new BinaryReader(File.OpenRead(path),
                       Encoding.UTF8, false))
            {
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
                var version = reader.ReadInt32();
                var size = reader.ReadInt32();
                var count = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadUInt64();
                if (!string.Equals(magic, expectedMagic,
                        StringComparison.Ordinal) || version != 1 ||
                    size != expectedRecordSize || count != expectedCount)
                    throw new InvalidDataException(
                        "Binary remediation header does not match its manifest: " +
                        path);
            }
        }

        private static void ValidateRange(int start, int count, int total,
            string parameterName)
        {
            if (start < 0 || count < 0 || start > total - count)
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private static List<JObject> ReadArray(string path, string name)
        {
            var token = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (token[name] ?? new JArray()).Children<JObject>().ToList();
        }

        private static string Text(JToken token, string name) =>
            token[name]?.Value<string>() ?? string.Empty;

        private static int Integer(JToken token, string name) =>
            token[name]?.Value<int>() ?? 0;

        private static long Long(JToken token, string name) =>
            token[name]?.Value<long>() ?? 0;

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    builder.Append(value.ToString("x2",
                        CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }
}
