using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    /// <summary>
    /// Projects the protected Luoyang 184 packages through one read-only source
    /// contract. Runtime changes are written to a derived living-world checkpoint,
    /// never back into the protected initialization binaries.
    /// </summary>
    public sealed class Luoyang184LivingWorldSourceAdapter :
        ILuoyang184LivingWorldSource
    {
        private readonly string rootPath;
        private readonly Luoyang184MetropolitanPopulationStore population;
        private readonly List<Luoyang184LivingFacilitySourceRecord> facilities;
        private readonly List<Luoyang184T4SupplierSourceRecord> externalSuppliers;
        private readonly List<Luoyang184FamilyOrganizationSourceRecord>
            familyOrganizations;
        private readonly List<ulong> developableCellIds;

        public Luoyang184LivingWorldSourceAdapter(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("A package root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            population = new Luoyang184MetropolitanPopulationStore(this.rootPath);
            facilities = ReadFacilities();
            externalSuppliers = ReadExternalSuppliers();
            familyOrganizations = ReadFamilyOrganizations();
            developableCellIds = ReadDevelopableCellIds();
            if (facilities.Count != population.Source.Manifest.FacilityCount)
                throw new InvalidDataException("Living-world facility projection count mismatch.");
        }

        public string PackageId => Luoyang184MetropolitanPopulationStore.PackageId;
        public string ProtectedPackageDigest =>
            population.OpenCurrent().ManifestSha256;
        public int PersonCount => population.Source.Manifest.PersonCount;
        public int HouseholdCount => population.Source.Manifest.HouseholdCount;
        public int FacilityCount => population.Source.Manifest.FacilityCount;
        public IReadOnlyList<Luoyang184LivingFacilitySourceRecord> Facilities =>
            facilities;
        public IReadOnlyList<Luoyang184MetropolitanAgricultureRecord> Agriculture =>
            population.Source.Agriculture;
        public IReadOnlyList<Luoyang184MetropolitanSupplyChainRecord> SupplyChains =>
            population.Source.SupplyChains;
        public IReadOnlyList<Luoyang184T4SupplierSourceRecord> ExternalSuppliers =>
            externalSuppliers;
        public IReadOnlyList<Luoyang184FamilyOrganizationSourceRecord>
            FamilyOrganizations => familyOrganizations;
        public IReadOnlyList<ulong> DevelopableCellIds => developableCellIds;

        public IEnumerable<Luoyang184PermanentPersonRecord> ReadPersons(
            int startOrdinal, int count) =>
            population.Source.ReadPersons(startOrdinal, count);

        public IEnumerable<Luoyang184HouseholdRecord> ReadHouseholds(
            int startOrdinal, int count) =>
            population.Source.ReadHouseholds(startOrdinal, count);

        public string GetPersonId(uint ordinal) =>
            population.Source.GetPersonId(ordinal);

        public string GetHouseholdId(uint ordinal) =>
            population.GetHouseholdId(ordinal);

        public string GetFacilityId(uint facilityIndex) =>
            population.GetFacilityId(facilityIndex);

        public string GetActivityId(ushort activityIndex) =>
            population.GetActivityId(activityIndex);

        public string GetOccupationId(ushort occupationIndex) =>
            population.GetOccupationId(occupationIndex);

        private List<Luoyang184LivingFacilitySourceRecord> ReadFacilities()
        {
            var urbanRoot = Path.GetFullPath(Path.Combine(
                rootPath,
                population.Source.MetropolitanManifest.BasePackageRelativePath));
            var urban = ReadArray(Path.Combine(urbanRoot, "facilities.json"));
            var metropolitan = ReadArray(Path.Combine(rootPath, "facilities.json"));
            var all = urban.Concat(metropolitan).OrderBy(item =>
                item["global_facility_index"]?.Value<int>() ??
                urban.IndexOf(item)).ToList();
            var result = new List<Luoyang184LivingFacilitySourceRecord>(all.Count);
            for (var index = 0; index < all.Count; index++)
            {
                var token = all[index];
                var facilityId = Text(token, "facility_id");
                if (!string.Equals(facilityId,
                        population.GetFacilityId(checked((uint)index)),
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Living-world facility index mismatch at " + index + ".");
                result.Add(new Luoyang184LivingFacilitySourceRecord
                {
                    FacilityIndex = index,
                    FacilityId = facilityId,
                    DefinitionId = Text(token, "definition_id"),
                    CategoryId = Text(token, "category_id"),
                    OwnerId = Text(token, "owner_id"),
                    ControllerId = Text(token, "controller_id"),
                    SettlementId = Text(token, "settlement_id"),
                    CellId64 = token["cell_id64"]?.Value<ulong>() ?? 0,
                    ResidentCapacity = Integer(token,
                        "recommended_residential_capacity",
                        Integer(token, "residential_capacity_persons", 0)),
                    CurrentResidents = Integer(token, "current_residents", 0),
                    WorkerCapacity = Integer(token,
                        "recommended_worker_capacity",
                        Integer(token, "worker_capacity", 0)),
                    MinimumWorkers = Integer(token,
                        "minimum_workers_for_normal_operation", 0),
                    CurrentWorkers = Integer(token, "current_workers", 0),
                    StorageCapacity = Long(token, "storage_capacity_units",
                        Long(token, "storage_capacity", 0)),
                    Operational = token["active"] == null ||
                                  token["active"].Value<bool>()
                });
            }
            return result;
        }

        private List<Luoyang184T4SupplierSourceRecord> ReadExternalSuppliers()
        {
            var path = Path.GetFullPath(Path.Combine(rootPath,
                "..", "Luoyang184T4SupplyNetworkV1", "suppliers.json"));
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The formal Luoyang T4 supplier package is missing.", path);
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = new List<Luoyang184T4SupplierSourceRecord>();
            foreach (var token in (root["suppliers"] ?? new JArray())
                         .Children<JObject>())
            {
                result.Add(new Luoyang184T4SupplierSourceRecord
                {
                    SupplierId = Text(token, "supplier_id"),
                    Level = (LuoyangSupplierMaterializationLevel)Enum.Parse(
                        typeof(LuoyangSupplierMaterializationLevel),
                        Text(token, "level")),
                    CountyId = Text(token, "county_id"),
                    SettlementId = Text(token, "settlement_id"),
                    FacilityId = Text(token, "facility_id"),
                    InventoryId = Text(token, "inventory_id"),
                    OrganizationId = Text(token, "organization_id"),
                    ManagerPersonId = token["manager_person_ordinal"] != null
                        ? population.Source.GetPersonId(token[
                            "manager_person_ordinal"].Value<uint>())
                        : Text(token, "manager_person_id"),
                    ManagerHouseholdId = token["manager_household_ordinal"] != null
                        ? population.GetHouseholdId(token[
                            "manager_household_ordinal"].Value<uint>())
                        : Text(token, "manager_household_id"),
                    ProductId = Text(token, "product_id"),
                    OpeningQuantityMilliunits = Long(token,
                        "opening_quantity_milliunits", 0),
                    StorageCapacityMilliunits = Long(token,
                        "storage_capacity_milliunits", 0),
                    DailyProductionMilliunits = Long(token,
                        "daily_production_milliunits", 0),
                    RouteId = Text(token, "route_id"),
                    DistanceKilometers = Integer(token,
                        "distance_kilometers", 0),
                    TravelDays = Integer(token, "travel_days", 0),
                    NaturalLossBasisPoints = Integer(token,
                        "natural_loss_basis_points", 0),
                    RiskLossBasisPoints = Integer(token,
                        "risk_loss_basis_points", 0),
                    EvidenceGrade = Text(token, "evidence_grade"),
                    SourceReferenceId = Text(token, "source_reference_id")
                });
            }
            if (result.Count == 0)
                throw new InvalidDataException(
                    "The formal Luoyang T4 supplier package is empty.");
            return result;
        }

        private List<Luoyang184FamilyOrganizationSourceRecord>
            ReadFamilyOrganizations()
        {
            var urbanRoot = Path.GetFullPath(Path.Combine(rootPath,
                population.Source.MetropolitanManifest.BasePackageRelativePath));
            var all = ReadNamedArray(Path.Combine(urbanRoot,
                    "family_organizations.json"), "organizations")
                .Concat(ReadNamedArray(Path.Combine(rootPath,
                    "family_organizations.json"), "organizations")).ToList();
            var result = new List<Luoyang184FamilyOrganizationSourceRecord>();
            for (var index = 0; index < all.Count; index++)
            {
                var token = all[index];
                result.Add(new Luoyang184FamilyOrganizationSourceRecord
                {
                    Index = checked((ushort)index),
                    Id = Text(token, "family_organization_id"),
                    HeadPersonId = Text(token, "head_person_id"),
                    Funds = Long(token, "family_treasury", 0),
                    AssetValue = Long(token, "family_assets", 0),
                    FacilityIds = (token["family_facility_ids"] ?? new JArray())
                        .Values<string>().Where(item =>
                            !string.IsNullOrWhiteSpace(item)).ToList()
                });
            }
            return result;
        }

        private List<ulong> ReadDevelopableCellIds()
        {
            var path = Path.GetFullPath(Path.Combine(rootPath,
                "..", "LuoyangWorldV1", "luoyang_world.json"));
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The canonical Luoyang Cell world is missing.", path);
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var result = (root["cells"] ?? new JArray()).Children<JObject>()
                .Where(item => item["developable"]?.Value<bool>() == true)
                .Select(item => item["cell_id64"]?.Value<ulong>() ?? 0)
                .Where(item => item != 0)
                .Distinct().OrderBy(item => item).ToList();
            var declared = root["capacity_validation"]?["developable_cells"]
                ?.Value<int>() ?? result.Count;
            if (result.Count == 0 || result.Count != declared)
                throw new InvalidDataException(
                    "Canonical Luoyang developable Cell count mismatch.");
            return result;
        }

        private static List<JObject> ReadArray(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["facilities"] ?? new JArray()).Children<JObject>().ToList();
        }

        private static List<JObject> ReadNamedArray(string path, string name)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root[name] ?? new JArray()).Children<JObject>().ToList();
        }

        private static string Text(JToken token, string name) =>
            token[name]?.Value<string>() ?? string.Empty;

        private static int Integer(JToken token, string name, int fallback) =>
            token[name]?.Value<int?>() ?? fallback;

        private static long Long(JToken token, string name, long fallback) =>
            token[name]?.Value<long?>() ?? fallback;
    }
}
