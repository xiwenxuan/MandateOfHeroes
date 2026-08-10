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
    public sealed class Luoyang184UrbanInitializationReader : ILuoyang184UrbanPopulationSource
    {
        public const string ExpectedSchema = "mandate.luoyang-184-urban-initialization.v1";
        public const int ExpectedPersonRecordSize = 80;
        public const int ExpectedHouseholdRecordSize = 32;
        private const int HeaderSize = 32;
        private readonly string rootPath;
        private readonly List<string> activityIds;
        private readonly List<string> forceIds;
        private readonly List<HistoricalOverlay> historicalPeople;
        private readonly Dictionary<uint, string> historicalPersonIdsByOrdinal;
        private readonly List<ForceOverlay> forces;
        private readonly List<Luoyang184ScenarioEventDefinition> events;

        public Luoyang184UrbanInitializationReader(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("A package root is required.", nameof(rootPath));
            }

            this.rootPath = Path.GetFullPath(rootPath);
            Manifest = ReadManifest(Path.Combine(this.rootPath, "manifest.json"));
            ValidateManifest();
            ValidateHeader(Path.Combine(this.rootPath, "persons.bin"), "MOHLYU01", ExpectedPersonRecordSize, Manifest.PersonCount);
            ValidateHeader(Path.Combine(this.rootPath, "households.bin"), "MOHLYH01", ExpectedHouseholdRecordSize, Manifest.HouseholdCount);

            var catalogs = JObject.Parse(File.ReadAllText(Path.Combine(this.rootPath, "catalogs.json"), Encoding.UTF8));
            activityIds = catalogs["activities"]?.Values<string>().ToList() ?? new List<string>();
            forceIds = catalogs["force_ids"]?.Values<string>().ToList() ?? new List<string>();
            historicalPeople = ReadHistoricalPeople(Path.Combine(this.rootPath, "historical_persons.json"));
            historicalPersonIdsByOrdinal = historicalPeople.ToDictionary(item => item.Ordinal, item => item.PersonId);
            forces = ReadForces(Path.Combine(this.rootPath, "forces.json"));
            events = ReadEvents(Path.Combine(this.rootPath, "scenario_events.json"));
        }

        public Luoyang184UrbanInitializationManifest Manifest { get; }
        public IReadOnlyList<Luoyang184ScenarioEventDefinition> Events => events;

        public string GetPersonId(uint ordinal)
        {
            if (ordinal >= Manifest.PersonCount)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return historicalPersonIdsByOrdinal.TryGetValue(ordinal, out var historicalId)
                ? historicalId
                : "person.luoyang.184.urban." + (ordinal + 1).ToString("D6", CultureInfo.InvariantCulture);
        }

        public IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, Manifest.PersonCount, nameof(startOrdinal));
            using (var stream = File.OpenRead(Path.Combine(rootPath, "persons.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)startOrdinal * ExpectedPersonRecordSize;
                for (var index = 0; index < count; index++)
                {
                    yield return ReadPerson(reader);
                }
            }
        }

        public IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, Manifest.HouseholdCount, nameof(startOrdinal));
            using (var stream = File.OpenRead(Path.Combine(rootPath, "households.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)startOrdinal * ExpectedHouseholdRecordSize;
                for (var index = 0; index < count; index++)
                {
                    yield return ReadHousehold(reader);
                }
            }
        }

        public Luoyang184UrbanScenarioState BuildScenarioState()
        {
            var state = new Luoyang184UrbanScenarioState();
            foreach (var overlay in historicalPeople)
            {
                var person = ReadPersons(checked((int)overlay.Ordinal), 1).Single();
                state.HistoricalPeople.Add(overlay.PersonId, new Luoyang184HistoricalPersonRuntimeState
                {
                    PersonId = overlay.PersonId,
                    Ordinal = overlay.Ordinal,
                    CurrentActivityId = person.ActivityIndex < activityIds.Count ? activityIds[person.ActivityIndex] : string.Empty,
                    CurrentLocationId = "cell.id64." + person.CurrentCellId64.ToString(CultureInfo.InvariantCulture),
                });
            }

            for (ushort index = 0; index < forces.Count; index++)
            {
                var overlay = forces[index];
                var stateEntry = new Luoyang184ForceRuntimeState
                {
                    ForceId = overlay.ForceId,
                    CommanderPersonId = overlay.CommanderPersonId,
                    Status = overlay.Status,
                    DestinationLocationId = overlay.DestinationLocationId,
                    MemberCount = overlay.MemberCount,
                };
                state.Forces.Add(stateEntry.ForceId, stateEntry);
                state.ForceIdsByIndex.Add(index, stateEntry.ForceId);
            }

            return state;
        }

        public IReadOnlyList<string> ValidatePackageFiles()
        {
            var failures = new List<string>();
            foreach (var item in Manifest.Files)
            {
                var path = Path.Combine(rootPath, item.Path);
                if (!File.Exists(path))
                {
                    failures.Add(item.Path + ":missing");
                    continue;
                }

                var info = new FileInfo(path);
                if (info.Length != item.Bytes)
                {
                    failures.Add(item.Path + ":size");
                    continue;
                }

                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actual = ToLowerHex(sha.ComputeHash(stream));
                    if (!string.Equals(actual, item.Sha256, StringComparison.Ordinal))
                    {
                        failures.Add(item.Path + ":sha256");
                    }
                }
            }

            return failures;
        }

        private void ValidateManifest()
        {
            if (!string.Equals(Manifest.Schema, ExpectedSchema, StringComparison.Ordinal)
                || Manifest.FormatVersion != 1
                || Manifest.PersonRecordSize != ExpectedPersonRecordSize
                || Manifest.HouseholdRecordSize != ExpectedHouseholdRecordSize)
            {
                throw new InvalidDataException("Unsupported Luoyang 184 urban initialization contract.");
            }

            if (Manifest.PersonCount != 270000 || Manifest.WalledCityPopulation != 200000
                || Manifest.UrbanAreaPopulation != 270000 || Manifest.MetropolitanPlanPopulation != 400000
                || Manifest.SupplyRegionPlanPopulation != 700000)
            {
                throw new InvalidDataException("The formal population profile does not match the accepted target hierarchy.");
            }
        }

        private static void ValidateRange(int start, int count, int total, string parameterName)
        {
            if (start < 0 || count < 0 || start > total - count)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateHeader(string path, string expectedMagic, int expectedRecordSize, int expectedCount)
        {
            using (var reader = new BinaryReader(File.OpenRead(path), Encoding.UTF8, false))
            {
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
                var version = reader.ReadInt32();
                var recordSize = reader.ReadInt32();
                var count = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadUInt64();
                if (magic != expectedMagic || version != 1 || recordSize != expectedRecordSize || count != expectedCount)
                {
                    throw new InvalidDataException("Binary package header does not match its manifest: " + path);
                }
            }
        }

        private static Luoyang184PermanentPersonRecord ReadPerson(BinaryReader reader)
        {
            return new Luoyang184PermanentPersonRecord(
                reader.ReadUInt32(), reader.ReadInt16(), reader.ReadByte(), reader.ReadByte(), reader.ReadUInt16(),
                reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt64(), reader.ReadUInt32(), reader.ReadUInt32(),
                reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
                reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
                reader.ReadInt64(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadByte(), reader.ReadByte(),
                reader.ReadByte(), reader.ReadByte(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }

        private static Luoyang184HouseholdRecord ReadHousehold(BinaryReader reader)
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
            return new Luoyang184HouseholdRecord(ordinal, head, start, count, family, residence, type, origin, wealth);
        }

        private static Luoyang184UrbanInitializationManifest ReadManifest(string path)
        {
            var token = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = new Luoyang184UrbanInitializationManifest
            {
                Schema = (string)token["schema"],
                FormatVersion = (int)token["format_version"],
                ScenarioId = (string)token["scenario_id"],
                ScenarioYear = (int)token["scenario_year"],
                WorldId = (string)token["world_id"],
                CityId = (string)token["city_id"],
                DataOrigin = (string)token["data_origin"],
                PopulationProfileId = (string)token["population_profile_id"],
                WalledCityPopulation = (int)token["walled_city_population"],
                UrbanAreaPopulation = (int)token["urban_area_population"],
                MetropolitanPlanPopulation = (int)token["metropolitan_plan_population"],
                SupplyRegionPlanPopulation = (int)token["supply_region_plan_population"],
                PersonRecordSize = (int)token["person_record_size"],
                PersonCount = (int)token["person_count"],
                HouseholdRecordSize = (int)token["household_record_size"],
                HouseholdCount = (int)token["household_count"],
                HistoricalPersonCount = (int)token["historical_person_count"],
                ExternalHistoricalAnchorCount = (int)token["external_historical_anchor_count"],
                FacilityCount = (int)token["facility_count"],
                FamilyOrganizationCount = (int)token["family_organization_count"],
                ForceCount = (int)token["force_count"],
                EventCount = (int)token["event_count"],
            };
            foreach (var file in token["files"] ?? new JArray())
            {
                result.Files.Add(new Luoyang184UrbanPackageFile
                {
                    Path = (string)file["path"],
                    Bytes = (long)file["bytes"],
                    Sha256 = (string)file["sha256"],
                });
            }
            return result;
        }

        private static List<HistoricalOverlay> ReadHistoricalPeople(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["people"] ?? new JArray()).Select(item => new HistoricalOverlay
            {
                Ordinal = (uint)item["ordinal"],
                PersonId = (string)item["person_id"],
            }).ToList();
        }

        private static List<ForceOverlay> ReadForces(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["forces"] ?? new JArray()).Select(item => new ForceOverlay
            {
                ForceId = (string)item["force_id"],
                CommanderPersonId = (string)item["commander_person_id"],
                Status = (string)item["status"],
                DestinationLocationId = (string)item["destination_location_id"],
                MemberCount = (int)item["member_count"],
            }).ToList();
        }

        private static List<Luoyang184ScenarioEventDefinition> ReadEvents(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["events"] ?? new JArray()).Select(item => new Luoyang184ScenarioEventDefinition
            {
                EventId = (string)item["event_id"],
                Order = (int)item["order"],
                Label = (string)item["label"],
                InitialStatus = (string)item["status"],
                Actors = item["actors"]?.Values<string>().ToList() ?? new List<string>(),
                Actions = (item["actions"] ?? new JArray()).Select(action => new Luoyang184ScenarioActionDefinition
                {
                    TypeId = (string)action["type_id"],
                    PersonId = (string)action["person_id"],
                    ForceId = (string)action["force_id"],
                    ScopeForceId = (string)action["scope_force_id"],
                    Value = action["value"]?.Type == JTokenType.String ? (string)action["value"] : null,
                    NumericValue = action["value"]?.Type == JTokenType.Integer ? (int)action["value"] : 0,
                }).ToList(),
            }).OrderBy(item => item.Order).ToList();
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private sealed class HistoricalOverlay
        {
            public uint Ordinal { get; set; }
            public string PersonId { get; set; }
        }

        private sealed class ForceOverlay
        {
            public string ForceId { get; set; }
            public string CommanderPersonId { get; set; }
            public string Status { get; set; }
            public string DestinationLocationId { get; set; }
            public int MemberCount { get; set; }
        }
    }
}
