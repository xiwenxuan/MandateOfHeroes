using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public sealed class HanHistoricalPersonClanDatasetReader : IHanHistoricalPersonClanSource
    {
        public const string ExpectedManifestSchema = "mandate.historical-person-clan-package.v1";
        public const string ExpectedScenarioSchema = "mandate.historical-person-clan-scenario-snapshot.v1";
        private readonly string rootPath;
        private readonly List<HanHistoricalPerson> people;
        private readonly List<HanHistoricalClan> clans;
        private readonly List<HanHistoricalBranch> branches;
        private readonly List<HanHistoricalKinship> kinship;
        private readonly List<HanHistoricalMarriage> marriages;
        private readonly List<HanHistoricalLocationRecord> locations;
        private readonly List<HanHistoricalCivilOfficeRecord> civilOffices;
        private readonly List<HanHistoricalMilitaryOfficeRecord> militaryOffices;
        private readonly List<HanHistoricalTitleRecord> titles;
        private readonly List<HanHistoricalAllegianceRecord> allegiances;
        private readonly List<HanHistoricalClanPresenceRecord> clanPresence;
        private readonly Dictionary<string, HanHistoricalPerson> personById;
        private readonly Dictionary<string, HanHistoricalClan> clanById;
        private readonly Dictionary<string, HanHistoricalBranch> branchById;
        private readonly Dictionary<string, ScenarioIndexRecord> scenarioById;
        private readonly Dictionary<int, HanHistoricalScenarioSnapshot> snapshotCache = new Dictionary<int, HanHistoricalScenarioSnapshot>();

        public HanHistoricalPersonClanDatasetReader(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("A dataset root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            Manifest = Read<HanHistoricalPersonClanManifest>("manifest.json");
            ValidateManifest();
            people = Read<ListPayload<HanHistoricalPerson>>("persons.json").Persons;
            clans = Read<ClanPayload>("clans.json").Clans;
            branches = Read<BranchPayload>("branches.json").Branches;
            kinship = Read<KinshipPayload>("kinship.json").Relations;
            marriages = Read<MarriagePayload>("marriages.json").Marriages;
            locations = Read<TimelinePayload<HanHistoricalLocationRecord>>("person_locations.json").Records;
            civilOffices = Read<TimelinePayload<HanHistoricalCivilOfficeRecord>>("civil_offices.json").Records;
            militaryOffices = Read<TimelinePayload<HanHistoricalMilitaryOfficeRecord>>("military_offices.json").Records;
            titles = Read<TimelinePayload<HanHistoricalTitleRecord>>("titles.json").Records;
            allegiances = Read<TimelinePayload<HanHistoricalAllegianceRecord>>("allegiances.json").Records;
            clanPresence = Read<PresencePayload>("clan_presence.json").Records;
            var scenarioIndex = Read<ScenarioIndexPayload>("scenario_index.json").Scenarios;
            scenarioById = scenarioIndex.ToDictionary(item => item.ScenarioId, StringComparer.Ordinal);
            personById = people.ToDictionary(item => item.PersonId, StringComparer.Ordinal);
            clanById = clans.ToDictionary(item => item.ClanId, StringComparer.Ordinal);
            branchById = branches.ToDictionary(item => item.BranchId, StringComparer.Ordinal);
            ValidateLoadedData();
        }

        public HanHistoricalPersonClanManifest Manifest { get; }

        public HanHistoricalPerson GetPerson(string personId)
        {
            if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("A PersonId is required.", nameof(personId));
            HanHistoricalPerson result;
            if (!personById.TryGetValue(personId, out result)) throw new KeyNotFoundException(personId);
            return result;
        }

        public HanHistoricalClan GetClan(string clanId)
        {
            if (string.IsNullOrWhiteSpace(clanId)) throw new ArgumentException("A ClanId is required.", nameof(clanId));
            HanHistoricalClan result;
            if (!clanById.TryGetValue(clanId, out result)) throw new KeyNotFoundException(clanId);
            return result;
        }

        public HanHistoricalBranch GetBranch(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId)) throw new ArgumentException("A BranchId is required.", nameof(branchId));
            HanHistoricalBranch result;
            if (!branchById.TryGetValue(branchId, out result)) throw new KeyNotFoundException(branchId);
            return result;
        }

        public IReadOnlyList<HanHistoricalPerson> GetPeople() { return people; }
        public IReadOnlyList<HanHistoricalClan> GetClans() { return clans; }
        public IReadOnlyList<HanHistoricalBranch> GetBranches() { return branches; }
        public IReadOnlyList<HanHistoricalKinship> GetKinship() { return kinship; }
        public IReadOnlyList<HanHistoricalMarriage> GetMarriages() { return marriages; }
        public IReadOnlyList<HanHistoricalLocationRecord> GetLocations() { return locations; }
        public IReadOnlyList<HanHistoricalCivilOfficeRecord> GetCivilOffices() { return civilOffices; }
        public IReadOnlyList<HanHistoricalMilitaryOfficeRecord> GetMilitaryOffices() { return militaryOffices; }
        public IReadOnlyList<HanHistoricalTitleRecord> GetTitles() { return titles; }
        public IReadOnlyList<HanHistoricalAllegianceRecord> GetAllegiances() { return allegiances; }
        public IReadOnlyList<HanHistoricalClanPresenceRecord> GetClanPresence() { return clanPresence; }

        public HanHistoricalScenarioSnapshot LoadScenarioSnapshot(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentException("A scenario ID is required.", nameof(scenarioId));
            ScenarioIndexRecord entry;
            if (!scenarioById.TryGetValue(scenarioId, out entry)) throw new KeyNotFoundException(scenarioId);
            var snapshot = Read<HanHistoricalScenarioSnapshot>(entry.Path);
            ValidateSnapshot(snapshot, entry.Year, entry.ScenarioId);
            snapshotCache[entry.Year] = snapshot;
            return snapshot;
        }

        public HanHistoricalScenarioSnapshot LoadHistoricalSnapshot(int year)
        {
            if (year < Manifest.YearStart || year > Manifest.YearEnd) throw new ArgumentOutOfRangeException(nameof(year));
            HanHistoricalScenarioSnapshot cached;
            if (snapshotCache.TryGetValue(year, out cached)) return cached;
            var formal = scenarioById.Values.SingleOrDefault(item => item.Year == year);
            if (formal != null) return LoadScenarioSnapshot(formal.ScenarioId);

            var personSnapshots = new List<HanHistoricalPersonSnapshot>();
            foreach (var person in people)
            {
                var aliveState = GetAliveState(person, year);
                if (aliveState == "NotBorn" || aliveState == "Dead") continue;
                var currentLocations = locations.Where(item => item.PersonId == person.PersonId && item.ContainsYear(year)).ToList();
                var location = currentLocations.Count == 1 ? currentLocations[0] : null;
                personSnapshots.Add(new HanHistoricalPersonSnapshot
                {
                    PersonId = person.PersonId,
                    AliveState = aliveState,
                    CurrentLocationRecordId = location?.RecordId,
                    CurrentRegionId = location?.RegionPermanentId,
                    CurrentCountyId = location?.CountyPermanentId,
                    CurrentCityId = location?.CityId,
                    CurrentCivilOfficeRecordIds = civilOffices.Where(item => item.PersonId == person.PersonId && item.ContainsYear(year)).Select(item => item.RecordId).ToList(),
                    CurrentMilitaryOfficeRecordIds = militaryOffices.Where(item => item.PersonId == person.PersonId && item.ContainsYear(year)).Select(item => item.RecordId).ToList(),
                    CurrentTitleRecordIds = titles.Where(item => item.PersonId == person.PersonId && item.ContainsYear(year)).Select(item => item.RecordId).ToList(),
                    CurrentAllegianceRecordIds = allegiances.Where(item => item.PersonId == person.PersonId && item.ContainsYear(year)).Select(item => item.RecordId).ToList(),
                    ClanId = person.ClanId,
                    BranchId = person.LineageBranchId,
                    HistoricalRole = person.PrimaryIdentity,
                    Confidence = person.EvidenceLevel,
                    LocationConflict = currentLocations.Count > 1
                });
            }
            var living = new HashSet<string>(personSnapshots.Select(item => item.PersonId), StringComparer.Ordinal);
            var clanSnapshots = clans.Select(clan => new HanHistoricalClanSnapshot
            {
                ClanId = clan.ClanId,
                ActiveStatus = people.Any(person => person.ClanId == clan.ClanId && living.Contains(person.PersonId)) ? "Active" : "NoKnownLivingMember",
                CoreRegionId = clan.PrimaryRegionId,
                KnownBranchIds = branches.Where(item => item.ClanId == clan.ClanId).Select(item => item.BranchId).ToList(),
                KnownLivingMemberIds = people.Where(person => person.ClanId == clan.ClanId && living.Contains(person.PersonId)).Select(person => person.PersonId).ToList(),
                KnownRegionalPresenceIds = clanPresence.Where(item => item.ClanId == clan.ClanId && item.ContainsYear(year)).Select(item => item.PresenceId).ToList(),
                MajorPoliticalMemberIds = people.Where(person => person.ClanId == clan.ClanId && living.Contains(person.PersonId) && (person.HistoricalPersonTier == "S" || person.HistoricalPersonTier == "A")).Select(person => person.PersonId).ToList(),
                MarriageIds = marriages.Where(item => personById[item.PersonAId].ClanId == clan.ClanId || personById[item.PersonBId].ClanId == clan.ClanId).Select(item => item.MarriageId).ToList(),
                EvidenceCoverage = "ConservativeV1"
            }).ToList();
            var snapshot = new HanHistoricalScenarioSnapshot
            {
                Schema = ExpectedScenarioSchema,
                ScenarioId = "historical-time-point." + year.ToString(CultureInfo.InvariantCulture),
                ScenarioName = year.ToString(CultureInfo.InvariantCulture) + "历史时间点",
                Year = year,
                SourceTimelineVersion = Manifest.DatasetId,
                Persons = personSnapshots,
                Clans = clanSnapshots
            };
            snapshotCache[year] = snapshot;
            return snapshot;
        }

        public IReadOnlyList<string> ValidatePackageFiles()
        {
            var failures = new List<string>();
            foreach (var item in Manifest.Files)
            {
                var path = ResolveInsideRoot(item.Path);
                if (!File.Exists(path)) { failures.Add(item.Path + ":missing"); continue; }
                if (new FileInfo(path).Length != item.Bytes) { failures.Add(item.Path + ":size"); continue; }
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actual = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
                    if (!string.Equals(actual, item.Sha256, StringComparison.Ordinal)) failures.Add(item.Path + ":sha256");
                }
            }
            return failures;
        }

        private T Read<T>(string relativePath)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(ResolveInsideRoot(relativePath), Encoding.UTF8));
        }

        private string ResolveInsideRoot(string relativePath)
        {
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
            var prefix = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? rootPath : rootPath + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Historical package path escapes its root.");
            return fullPath;
        }

        private void ValidateManifest()
        {
            if (Manifest == null || Manifest.Schema != ExpectedManifestSchema || Manifest.FormatVersion != 1
                || Manifest.YearStart != 135 || Manifest.YearEnd != 260 || Manifest.PersonCount != 1202
                || Manifest.ClanCount != 39 || Manifest.BranchCount != 15 || Manifest.ScenarioCount != 13
                || Manifest.FamilyOrganizationCount != 0 || Manifest.HouseholdCount != 0)
                throw new InvalidDataException("Unsupported HAN-135-260 historical person/clan dataset contract.");
        }

        private void ValidateLoadedData()
        {
            if (people.Count != Manifest.PersonCount || clans.Count != Manifest.ClanCount || branches.Count != Manifest.BranchCount
                || personById.Count != people.Count || clanById.Count != clans.Count || branchById.Count != branches.Count
                || kinship.Any(item => !personById.ContainsKey(item.PersonAId) || !personById.ContainsKey(item.PersonBId) || item.PersonAId == item.PersonBId)
                || marriages.Any(item => !personById.ContainsKey(item.PersonAId) || !personById.ContainsKey(item.PersonBId))
                || branches.Any(item => !clanById.ContainsKey(item.ClanId) || (item.ParentBranchId != null && !branchById.ContainsKey(item.ParentBranchId))))
                throw new InvalidDataException("Historical person/clan package violates stable identity references.");
        }

        private static void ValidateSnapshot(HanHistoricalScenarioSnapshot snapshot, int year, string scenarioId)
        {
            if (snapshot == null || snapshot.Schema != ExpectedScenarioSchema || snapshot.Year != year || snapshot.ScenarioId != scenarioId
                || snapshot.Persons == null || snapshot.Clans == null)
                throw new InvalidDataException("Historical scenario snapshot violates its timeline-derived contract.");
        }

        private static string GetAliveState(HanHistoricalPerson person, int year)
        {
            if (person.BirthYear.HasValue && year < person.BirthYear.Value) return "NotBorn";
            if (person.DeathYear.HasValue && year > person.DeathYear.Value) return "Dead";
            if (person.BirthYear.HasValue && person.DeathYear.HasValue) return "Alive";
            if (person.BirthYear.HasValue || person.DeathYear.HasValue) return "PossiblyAlive";
            return "Unknown";
        }

        [Serializable] private sealed class ListPayload<T> { [JsonProperty("persons")] public List<T> Persons { get; set; } = new List<T>(); }
        [Serializable] private sealed class ClanPayload { [JsonProperty("clans")] public List<HanHistoricalClan> Clans { get; set; } = new List<HanHistoricalClan>(); }
        [Serializable] private sealed class BranchPayload { [JsonProperty("branches")] public List<HanHistoricalBranch> Branches { get; set; } = new List<HanHistoricalBranch>(); }
        [Serializable] private sealed class KinshipPayload { [JsonProperty("relations")] public List<HanHistoricalKinship> Relations { get; set; } = new List<HanHistoricalKinship>(); }
        [Serializable] private sealed class MarriagePayload { [JsonProperty("marriages")] public List<HanHistoricalMarriage> Marriages { get; set; } = new List<HanHistoricalMarriage>(); }
        [Serializable] private sealed class TimelinePayload<T> { [JsonProperty("records")] public List<T> Records { get; set; } = new List<T>(); }
        [Serializable] private sealed class PresencePayload { [JsonProperty("records")] public List<HanHistoricalClanPresenceRecord> Records { get; set; } = new List<HanHistoricalClanPresenceRecord>(); }
        [Serializable] private sealed class ScenarioIndexPayload { [JsonProperty("scenarios")] public List<ScenarioIndexRecord> Scenarios { get; set; } = new List<ScenarioIndexRecord>(); }
        [Serializable] private sealed class ScenarioIndexRecord
        {
            [JsonProperty("scenario_id")] public string ScenarioId { get; set; }
            [JsonProperty("year")] public int Year { get; set; }
            [JsonProperty("path")] public string Path { get; set; }
        }
    }
}
