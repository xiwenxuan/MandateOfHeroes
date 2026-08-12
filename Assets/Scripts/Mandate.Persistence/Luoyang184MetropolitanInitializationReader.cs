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
    public sealed class Luoyang184MetropolitanInitializationReader : ILuoyang184UrbanPopulationSource
    {
        public const string ExpectedSchema = "mandate.luoyang-184-metropolitan-initialization.v1";
        private const int HeaderSize = 32;
        private readonly string rootPath;
        private readonly string baseRootPath;

        public Luoyang184MetropolitanInitializationReader(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("A package root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            MetropolitanManifest = ReadMetropolitanManifest(Path.Combine(this.rootPath, "manifest.json"));
            ValidateMetropolitanManifest();
            baseRootPath = Path.GetFullPath(Path.Combine(this.rootPath, MetropolitanManifest.BasePackageRelativePath));
            BaseReader = new Luoyang184UrbanInitializationReader(baseRootPath);
            ValidateHeader(Path.Combine(this.rootPath, "outer_persons.bin"), "MOHLYM01",
                Luoyang184UrbanInitializationReader.ExpectedPersonRecordSize, MetropolitanManifest.AddedPersonCount);
            ValidateHeader(Path.Combine(this.rootPath, "outer_households.bin"), "MOHLYK01",
                Luoyang184UrbanInitializationReader.ExpectedHouseholdRecordSize, MetropolitanManifest.AddedHouseholdCount);

            Manifest = new Luoyang184UrbanInitializationManifest
            {
                Schema = MetropolitanManifest.Schema,
                FormatVersion = MetropolitanManifest.FormatVersion,
                ScenarioId = MetropolitanManifest.ScenarioId,
                ScenarioYear = MetropolitanManifest.ScenarioYear,
                WorldId = MetropolitanManifest.WorldId,
                CityId = MetropolitanManifest.CityId,
                DataOrigin = "HistoricalReconstruction",
                PopulationProfileId = MetropolitanManifest.PopulationProfileId,
                WalledCityPopulation = MetropolitanManifest.WalledCityPopulation,
                UrbanAreaPopulation = MetropolitanManifest.UrbanAreaPopulation,
                MetropolitanPlanPopulation = MetropolitanManifest.MetropolitanPopulation,
                SupplyRegionPlanPopulation = MetropolitanManifest.SupplyRegionPlanPopulation,
                PersonRecordSize = MetropolitanManifest.PersonRecordSize,
                PersonCount = MetropolitanManifest.PersonCount,
                HouseholdRecordSize = MetropolitanManifest.HouseholdRecordSize,
                HouseholdCount = MetropolitanManifest.HouseholdCount,
                HistoricalPersonCount = MetropolitanManifest.HistoricalPersonCount,
                FacilityCount = MetropolitanManifest.FacilityCount,
                FamilyOrganizationCount = 15,
                ForceCount = BaseReader.Manifest.ForceCount,
                EventCount = BaseReader.Manifest.EventCount,
            };

            Facilities = ReadFacilities(Path.Combine(this.rootPath, "facilities.json"));
            Routes = ReadRoutes(Path.Combine(this.rootPath, "roads_logistics.json"));
            Agriculture = ReadAgriculture(Path.Combine(this.rootPath, "agriculture_supply.json"));
            SupplyChains = ReadSupplyChains(Path.Combine(this.rootPath, "roads_logistics.json"));
            EventImpacts = ReadEventImpacts(Path.Combine(this.rootPath, "event_impacts.json"));
        }

        public Luoyang184MetropolitanInitializationManifest MetropolitanManifest { get; }
        public Luoyang184UrbanInitializationReader BaseReader { get; }
        public Luoyang184UrbanInitializationManifest Manifest { get; }
        public IReadOnlyList<Luoyang184MetropolitanFacilityRecord> Facilities { get; }
        public IReadOnlyList<Luoyang184MetropolitanRouteRecord> Routes { get; }
        public IReadOnlyList<Luoyang184MetropolitanAgricultureRecord> Agriculture { get; }
        public IReadOnlyList<Luoyang184MetropolitanSupplyChainRecord> SupplyChains { get; }
        public IReadOnlyList<Luoyang184MetropolitanEventImpact> EventImpacts { get; }

        public string GetPersonId(uint ordinal)
        {
            if (ordinal >= Manifest.PersonCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
            return ordinal < MetropolitanManifest.BasePersonCount
                ? BaseReader.GetPersonId(ordinal)
                : "person.luoyang.184.metropolitan." + (ordinal + 1).ToString("D6", CultureInfo.InvariantCulture);
        }

        public IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, Manifest.PersonCount, nameof(startOrdinal));
            var remaining = count;
            var cursor = startOrdinal;
            if (cursor < MetropolitanManifest.BasePersonCount && remaining > 0)
            {
                var baseCount = Math.Min(remaining, MetropolitanManifest.BasePersonCount - cursor);
                foreach (var record in BaseReader.ReadPersons(cursor, baseCount)) yield return record;
                cursor += baseCount;
                remaining -= baseCount;
            }
            if (remaining <= 0) yield break;
            var outerOffset = cursor - MetropolitanManifest.BasePersonCount;
            using (var stream = File.OpenRead(Path.Combine(rootPath, "outer_persons.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)outerOffset * Luoyang184UrbanInitializationReader.ExpectedPersonRecordSize;
                for (var index = 0; index < remaining; index++) yield return ReadPerson(reader);
            }
        }

        public IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(int startOrdinal, int count)
        {
            ValidateRange(startOrdinal, count, Manifest.HouseholdCount, nameof(startOrdinal));
            var remaining = count;
            var cursor = startOrdinal;
            if (cursor < MetropolitanManifest.BaseHouseholdCount && remaining > 0)
            {
                var baseCount = Math.Min(remaining, MetropolitanManifest.BaseHouseholdCount - cursor);
                foreach (var record in BaseReader.ReadHouseholds(cursor, baseCount)) yield return record;
                cursor += baseCount;
                remaining -= baseCount;
            }
            if (remaining <= 0) yield break;
            var outerOffset = cursor - MetropolitanManifest.BaseHouseholdCount;
            using (var stream = File.OpenRead(Path.Combine(rootPath, "outer_households.bin")))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                stream.Position = HeaderSize + (long)outerOffset * Luoyang184UrbanInitializationReader.ExpectedHouseholdRecordSize;
                for (var index = 0; index < remaining; index++) yield return ReadHousehold(reader);
            }
        }

        public IReadOnlyList<string> ValidatePackageFiles()
        {
            var failures = new List<string>();
            ValidateFiles(rootPath, MetropolitanManifest.Files, "metropolitan/", failures);
            ValidateFiles(baseRootPath, MetropolitanManifest.BasePackageFiles, "urban/", failures);
            foreach (var failure in BaseReader.ValidatePackageFiles()) failures.Add("urban-reader/" + failure);
            return failures;
        }

        private void ValidateMetropolitanManifest()
        {
            if (!string.Equals(MetropolitanManifest.Schema, ExpectedSchema, StringComparison.Ordinal)
                || MetropolitanManifest.FormatVersion != 1
                || MetropolitanManifest.BasePersonCount != 270000
                || MetropolitanManifest.AddedPersonCount != 130000
                || MetropolitanManifest.PersonCount != 400000
                || MetropolitanManifest.UrbanAreaPopulation != 270000
                || MetropolitanManifest.MetropolitanPopulation != 400000
                || MetropolitanManifest.SupplyRegionPlanPopulation != 700000
                || MetropolitanManifest.PersonRecordSize != Luoyang184UrbanInitializationReader.ExpectedPersonRecordSize
                || MetropolitanManifest.HouseholdRecordSize != Luoyang184UrbanInitializationReader.ExpectedHouseholdRecordSize)
            {
                throw new InvalidDataException("Unsupported Luoyang 184 metropolitan initialization contract.");
            }
        }

        private static void ValidateFiles(string root, IEnumerable<Luoyang184UrbanPackageFile> files,
            string prefix, ICollection<string> failures)
        {
            foreach (var item in files)
            {
                var path = Path.Combine(root, item.Path);
                if (!File.Exists(path)) { failures.Add(prefix + item.Path + ":missing"); continue; }
                if (new FileInfo(path).Length != item.Bytes) { failures.Add(prefix + item.Path + ":size"); continue; }
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actual = ToLowerHex(sha.ComputeHash(stream));
                    if (!string.Equals(actual, item.Sha256, StringComparison.Ordinal)) failures.Add(prefix + item.Path + ":sha256");
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

        private static void ValidateRange(int start, int count, int total, string parameterName)
        {
            if (start < 0 || count < 0 || start > total - count) throw new ArgumentOutOfRangeException(parameterName);
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
                    throw new InvalidDataException("Binary package header does not match its manifest: " + path);
            }
        }

        private static Luoyang184MetropolitanInitializationManifest ReadMetropolitanManifest(string path)
        {
            var token = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = new Luoyang184MetropolitanInitializationManifest
            {
                Schema = (string)token["schema"], FormatVersion = (int)token["format_version"],
                ScenarioId = (string)token["scenario_id"], ScenarioYear = (int)token["scenario_year"],
                WorldId = (string)token["world_id"], CityId = (string)token["city_id"],
                PopulationProfileId = (string)token["population_profile_id"],
                BasePackageRelativePath = (string)token["base_package_relative_path"],
                BasePersonCount = (int)token["base_person_count"], AddedPersonCount = (int)token["added_person_count"], PersonCount = (int)token["person_count"],
                BaseHouseholdCount = (int)token["base_household_count"], AddedHouseholdCount = (int)token["added_household_count"], HouseholdCount = (int)token["household_count"],
                BaseFacilityCount = (int)token["base_facility_count"], AddedFacilityCount = (int)token["added_facility_count"], FacilityCount = (int)token["facility_count"],
                PersonRecordSize = (int)token["person_record_size"], HouseholdRecordSize = (int)token["household_record_size"],
                WalledCityPopulation = (int)token["walled_city_population"], UrbanAreaPopulation = (int)token["urban_area_population"],
                MetropolitanPopulation = (int)token["metropolitan_population"], SupplyRegionPlanPopulation = (int)token["supply_region_plan_population"],
                HistoricalPersonCount = (int)token["historical_person_count"],
            };
            ReadPackageFiles(token["base_package_files"], result.BasePackageFiles);
            ReadPackageFiles(token["files"], result.Files);
            return result;
        }

        private static void ReadPackageFiles(JToken token, ICollection<Luoyang184UrbanPackageFile> result)
        {
            foreach (var file in token ?? new JArray()) result.Add(new Luoyang184UrbanPackageFile
            {
                Path = (string)file["path"], Bytes = (long)file["bytes"], Sha256 = (string)file["sha256"],
            });
        }

        private static List<Luoyang184MetropolitanFacilityRecord> ReadFacilities(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["facilities"] ?? new JArray()).Select(item => new Luoyang184MetropolitanFacilityRecord
            {
                GlobalFacilityIndex = (int)item["global_facility_index"], FacilityId = (string)item["facility_id"],
                DefinitionId = (string)item["definition_id"], CategoryId = (string)item["category_id"], CellId64 = (ulong)item["cell_id64"],
                OwnerId = (string)item["owner_id"], AdministrativeControllerId = (string)item["administrative_controller_id"],
                AreaType = (string)item["area_type"], SettlementId = (string)item["settlement_id"],
                ResidentialCapacity = (int)item["residential_capacity_persons"], CurrentResidents = (int)item["current_residents"],
                WorkerCapacity = (int)item["worker_capacity"], CurrentWorkers = (int)item["current_workers"],
                StorageCapacity = (long)item["storage_capacity_units"],
            }).ToList();
        }

        private static List<Luoyang184MetropolitanRouteRecord> ReadRoutes(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = new List<Luoyang184MetropolitanRouteRecord>();
            foreach (var item in root["routes"] ?? new JArray())
            {
                var route = new Luoyang184MetropolitanRouteRecord
                {
                    RouteId = (string)item["route_id"], SettlementId = (string)item["settlement_id"],
                    GateFacilityId = (string)item["gate_facility_id"], DistanceMetres = (int)item["distance_m"],
                    TravelMinutes = (int)item["travel_minutes"], UsesGateComplexTransition = (bool)item["uses_gate_complex_transition"],
                    GateComplexTransitionSpanCells = (int)item["gate_complex_transition_span_cells"],
                };
                foreach (var cell in item["cell_ids"] ?? new JArray()) route.CellIds.Add((ulong)cell);
                result.Add(route);
            }
            return result;
        }

        private static List<Luoyang184MetropolitanAgricultureRecord> ReadAgriculture(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = new List<Luoyang184MetropolitanAgricultureRecord>();
            foreach (var item in root["fields"] ?? new JArray())
            {
                var field = new Luoyang184MetropolitanAgricultureRecord
                {
                    FieldId = (string)item["field_id"], FacilityId = (string)item["facility_id"], CellId64 = (ulong)item["cell_id64"],
                    ProductDefinitionId = (string)item["product_definition_id"], PlantedDay = (int)item["planted_day"],
                    MaturityDay = (int)item["maturity_day"], EarlyHarvestMinimumBasisPoints = (int)item["early_harvest_minimum_basis_points"],
                    FullYieldUnits = (long)item["full_yield_units"], InventoryContainerId = (string)item["inventory_container_id"],
                };
                foreach (var person in item["worker_person_ordinals"] ?? new JArray()) field.WorkerPersonOrdinals.Add((uint)person);
                result.Add(field);
            }
            return result;
        }

        private static List<Luoyang184MetropolitanSupplyChainRecord> ReadSupplyChains(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["supply_chains"] ?? new JArray()).Select(item => new Luoyang184MetropolitanSupplyChainRecord
            {
                ChainId = (string)item["chain_id"], ProductDefinitionId = (string)item["product_definition_id"],
                ProducerFacilityId = (string)item["producer_facility_id"], WarehouseFacilityId = (string)item["warehouse_facility_id"],
                CarrierPersonOrdinal = (uint)item["carrier_person_ordinal"], GateFacilityId = (string)item["gate_facility_id"],
                DestinationFacilityId = (string)item["destination_facility_id"], ShippedUnits = (long)item["shipped_units"],
                CarrierConsumptionUnits = (long)item["carrier_consumption_units"], NaturalLossUnits = (long)item["natural_loss_units"],
                RoadLossUnits = (long)item["road_loss_units"], DeliveredUnits = (long)item["delivered_units"],
            }).ToList();
        }

        private static List<Luoyang184MetropolitanEventImpact> ReadEventImpacts(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["impacts"] ?? new JArray()).Select(item =>
            {
                var effects = item["effects"] ?? new JObject();
                return new Luoyang184MetropolitanEventImpact
                {
                    EventId = (string)item["event_id"], RecruitmentPersons = (int?)effects["recruitment_persons"] ?? 0,
                    TransportCapacityDelta = (int?)effects["transport_capacity_delta"] ?? 0,
                    GrainPriceBasisPoints = (int?)effects["grain_price_basis_points"] ?? 0,
                    MilitarySupplyUnits = (int?)effects["military_supply_units"] ?? 0,
                    RoadCapacityDelta = (int?)effects["road_capacity_delta"] ?? 0,
                    AgriculturalLaborDelta = (int?)effects["agricultural_labor_delta"] ?? 0,
                    RefugeePressure = (int?)effects["refugee_pressure"] ?? 0,
                    SecurityPressure = (int?)effects["security_pressure"] ?? 0,
                    RoadInspectionPressure = (int?)effects["road_inspection_pressure"] ?? 0,
                };
            }).ToList();
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }
}
