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
    /// Read-through adapter over the protected 400K Luoyang initialization package.
    /// It exposes the existing binary persons as the formal permanent-population store;
    /// it never regenerates or rewrites the protected package.
    /// </summary>
    public sealed class Luoyang184MetropolitanPopulationStore :
        IPermanentPopulationStore
    {
        public const string PackageId =
            "population.package.luoyang.184.metropolitan.v1";
        public const int PartitionCount = 32;

        private readonly string rootPath;
        private readonly Luoyang184MetropolitanInitializationReader reader;
        private readonly Dictionary<string, uint> historicalOrdinals;
        private readonly Dictionary<string, string> historicalNames;
        private readonly List<string> activities;
        private readonly List<string> occupations;
        private readonly List<string> facilityIds;
        private readonly PopulationPackageManifest manifest;

        public Luoyang184MetropolitanPopulationStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("A package root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            reader = new Luoyang184MetropolitanInitializationReader(this.rootPath);
            var scenario = reader.BaseReader.BuildScenarioState();
            historicalOrdinals = scenario.HistoricalPeople.Values.ToDictionary(
                item => item.PersonId, item => item.Ordinal, StringComparer.Ordinal);
            var urbanRoot = Path.GetFullPath(Path.Combine(
                this.rootPath,
                reader.MetropolitanManifest.BasePackageRelativePath));
            var historicalToken = JObject.Parse(File.ReadAllText(
                Path.Combine(urbanRoot, "historical_persons.json"), Encoding.UTF8));
            historicalNames = historicalToken["people"]
                .Children<JObject>()
                .ToDictionary(
                    item => (string)item["person_id"],
                    item => (string)item["display_name"] ?? (string)item["person_id"],
                    StringComparer.Ordinal);
            var catalogs = JObject.Parse(File.ReadAllText(
                Path.Combine(this.rootPath, "catalogs.json"), Encoding.UTF8));
            activities = Values(catalogs, "activities");
            occupations = Values(catalogs, "occupations");
            facilityIds = Values(catalogs, "facility_ids");
            if (facilityIds.Count != reader.Manifest.FacilityCount)
                throw new InvalidDataException(
                    "The metropolitan facility catalog does not cover all formal facilities.");

            manifest = new PopulationPackageManifest
            {
                PackageId = PackageId,
                PartitionCount = PartitionCount,
                StorageRevision = 1,
                PermanentPersonCount = reader.Manifest.PersonCount,
                LivingPersonCount = reader.Manifest.PersonCount,
                DetailExtensionCount = reader.Manifest.PersonCount,
                ManifestSha256 = Sha256(Path.Combine(this.rootPath, "manifest.json"))
            };
            var partitionSize = GetPartitionSize();
            for (var index = 0; index < PartitionCount; index++)
            {
                var start = index * partitionSize;
                var count = Math.Max(0, Math.Min(partitionSize,
                    reader.Manifest.PersonCount - start));
                manifest.Partitions.Add(new PopulationPartitionManifestEntry
                {
                    PartitionIndex = index,
                    PersonCount = count,
                    LivingPersonCount = count,
                    DetailExtensionCount = count,
                    CoreRelativePath = "adapter://persons/" + index,
                    DetailRelativePath = "adapter://persons/" + index
                });
            }
        }

        public Luoyang184MetropolitanInitializationReader Source => reader;

        public PopulationPackageManifest CommitCheckpoint(
            PopulationCheckpoint checkpoint) =>
            throw new NotSupportedException(
                "The protected Luoyang baseline is read-only. Commit changes to a derived checkpoint package.");

        public PopulationPackageManifest CommitIncrementalCheckpoint(
            PopulationIncrementalCheckpoint checkpoint) =>
            throw new NotSupportedException(
                "The protected Luoyang baseline is read-only. Commit changes to a derived checkpoint package.");

        public PopulationPackageManifest OpenCurrent() => manifest;

        public bool TryReadCore(string personId,
            out PermanentPersonCoreRecord person)
        {
            if (!TryGetOrdinal(personId, out var ordinal))
            {
                person = null;
                return false;
            }

            var record = reader.ReadPersons(checked((int)ordinal), 1).Single();
            person = ToCore(personId, record);
            return true;
        }

        public bool TryReadDetail(string personId, out PersonState person)
        {
            if (!TryReadCore(personId, out var core))
            {
                person = null;
                return false;
            }

            if (!TryGetOrdinal(personId, out var ordinal))
                throw new InvalidOperationException("Resolved Person has no ordinal.");
            var record = reader.ReadPersons(checked((int)ordinal), 1).Single();
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
                CountsTowardPopulation = core.CountsTowardPopulation,
                PopulationOriginLocationId = core.PopulationOriginLocationId,
                VillageOccupation = core.VillageOccupation,
                LaborCapacityBasisPoints = core.LaborCapacityBasisPoints,
                NextIndependentEventDay = core.NextIndependentEventDay,
                NextIndependentEventReason = core.NextIndependentEventReason,
                LocalDuty = core.LocalDuty,
                LocalDutyUntilDay = core.LocalDutyUntilDay
            };
            return true;
        }

        public IReadOnlyList<PermanentPersonCoreRecord> LoadCorePartition(
            int partitionIndex)
        {
            GetPartitionRange(partitionIndex, out var start, out var count);
            var result = new List<PermanentPersonCoreRecord>(count);
            foreach (var record in reader.ReadPersons(start, count))
            {
                var personId = reader.GetPersonId(record.Ordinal);
                result.Add(ToCore(personId, record));
            }
            return result;
        }

        public IReadOnlyList<PersonDetailExtensionRecord> LoadDetailPartition(
            int partitionIndex)
        {
            var cores = LoadCorePartition(partitionIndex);
            var result = new List<PersonDetailExtensionRecord>(cores.Count);
            for (var index = 0; index < cores.Count; index++)
            {
                TryReadDetail(cores[index].PersonId, out var person);
                result.Add(new PersonDetailExtensionRecord
                {
                    StorageRevision = 1,
                    Person = person
                });
            }
            return result;
        }

        public bool TryGetOrdinal(string personId, out uint ordinal)
        {
            if (historicalOrdinals.TryGetValue(personId, out ordinal)) return true;
            const string urbanPrefix = "person.luoyang.184.urban.";
            const string metroPrefix = "person.luoyang.184.metropolitan.";
            var suffix = personId != null && personId.StartsWith(
                urbanPrefix, StringComparison.Ordinal)
                ? personId.Substring(urbanPrefix.Length)
                : personId != null && personId.StartsWith(
                    metroPrefix, StringComparison.Ordinal)
                    ? personId.Substring(metroPrefix.Length)
                    : null;
            if (!uint.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var oneBased) || oneBased == 0 ||
                oneBased > reader.Manifest.PersonCount)
            {
                ordinal = 0;
                return false;
            }
            ordinal = oneBased - 1;
            return string.Equals(reader.GetPersonId(ordinal), personId,
                StringComparison.Ordinal);
        }

        public string GetHouseholdId(uint householdOrdinal) =>
            "household.luoyang.184." + (householdOrdinal + 1)
                .ToString("D6", CultureInfo.InvariantCulture);

        public string GetFacilityId(uint facilityIndex) =>
            facilityIndex == uint.MaxValue || facilityIndex >= facilityIds.Count
                ? string.Empty
                : facilityIds[checked((int)facilityIndex)];

        public string GetActivityId(ushort activityIndex) =>
            activityIndex < activities.Count ? activities[activityIndex] : string.Empty;

        public string GetOccupationId(ushort occupationIndex) =>
            occupationIndex < occupations.Count ? occupations[occupationIndex] : string.Empty;

        private PermanentPersonCoreRecord ToCore(string personId,
            Luoyang184PermanentPersonRecord record)
        {
            var location = "cell.id64." + record.CurrentCellId64
                .ToString(CultureInfo.InvariantCulture);
            return new PermanentPersonCoreRecord
            {
                PersonId = personId,
                DisplayName = historicalNames.TryGetValue(personId, out var name)
                    ? name
                    : personId,
                CurrentLocationId = location,
                BirthLocationId = location,
                FamilyId = GetHouseholdId(record.HouseholdOrdinal),
                BirthDay = checked((long)(record.BirthYear - 184) * 360L),
                IsAlive = true,
                HealthBasisPoints = record.HealthBasisPoints,
                Gender = record.Gender == 1
                    ? PersonGender.Male
                    : record.Gender == 2 ? PersonGender.Female : PersonGender.Unknown,
                FatherPersonId = record.FatherOrdinal >= 0
                    ? reader.GetPersonId(checked((uint)record.FatherOrdinal))
                    : string.Empty,
                MotherPersonId = record.MotherOrdinal >= 0
                    ? reader.GetPersonId(checked((uint)record.MotherOrdinal))
                    : string.Empty,
                SpousePersonId = record.SpouseOrdinal >= 0
                    ? reader.GetPersonId(checked((uint)record.SpouseOrdinal))
                    : string.Empty,
                CountsTowardPopulation = true,
                PopulationOriginLocationId = location,
                VillageOccupation = ToVillageOccupation(
                    GetOccupationId(record.OccupationIndex)),
                LaborCapacityBasisPoints = 10_000,
                NextIndependentEventDay = -1,
                NextIndependentEventReason = string.Empty,
                LocalDuty = LocalDutyKind.None,
                LocalDutyUntilDay = -1
            };
        }

        private static VillageOccupation ToVillageOccupation(string occupationId)
        {
            switch (occupationId)
            {
                case "occupation.agriculture": return VillageOccupation.Farmer;
                case "occupation.crafts": return VillageOccupation.Artisan;
                case "occupation.trade": return VillageOccupation.Merchant;
                case "occupation.medical": return VillageOccupation.Physician;
                default: return VillageOccupation.Unknown;
            }
        }

        private int GetPartitionSize() =>
            (reader.Manifest.PersonCount + PartitionCount - 1) / PartitionCount;

        private void GetPartitionRange(int partitionIndex,
            out int start, out int count)
        {
            if (partitionIndex < 0 || partitionIndex >= PartitionCount)
                throw new ArgumentOutOfRangeException(nameof(partitionIndex));
            var size = GetPartitionSize();
            start = partitionIndex * size;
            count = Math.Max(0, Math.Min(size,
                reader.Manifest.PersonCount - start));
        }

        private static List<string> Values(JObject token, string property) =>
            token[property]?.Values<string>().ToList() ?? new List<string>();

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
