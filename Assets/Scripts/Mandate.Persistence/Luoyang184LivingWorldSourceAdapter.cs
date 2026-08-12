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

        public Luoyang184LivingWorldSourceAdapter(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("A package root is required.", nameof(rootPath));
            this.rootPath = Path.GetFullPath(rootPath);
            population = new Luoyang184MetropolitanPopulationStore(this.rootPath);
            facilities = ReadFacilities();
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

        private static List<JObject> ReadArray(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            return (root["facilities"] ?? new JArray()).Children<JObject>().ToList();
        }

        private static string Text(JToken token, string name) =>
            token[name]?.Value<string>() ?? string.Empty;

        private static int Integer(JToken token, string name, int fallback) =>
            token[name]?.Value<int?>() ?? fallback;

        private static long Long(JToken token, string name, long fallback) =>
            token[name]?.Value<long?>() ?? fallback;
    }
}
