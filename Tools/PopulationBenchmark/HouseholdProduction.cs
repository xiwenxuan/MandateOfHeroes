using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal sealed class HouseholdProductionProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("owned_land_basis_points")] public int OwnedLandBasisPoints { get; set; }
        [JsonProperty("opening_seed_conversion_basis_points")] public int OpeningSeedConversionBasisPoints { get; set; }
        [JsonProperty("new_household_land_and_seed_transfer_basis_points")] public int NewHouseholdLandAndSeedTransferBasisPoints { get; set; }
        [JsonProperty("leased_land_rent_basis_points")] public int LeasedLandRentBasisPoints { get; set; }
        [JsonProperty("seed_retention_target_basis_points")] public int SeedRetentionTargetBasisPoints { get; set; }
        [JsonProperty("minimum_order_land_milli_mu")] public int MinimumOrderLandMilliMu { get; set; }
        [JsonProperty("opening_seed_vigor_min_basis_points")] public int OpeningSeedVigorMinBasisPoints { get; set; }
        [JsonProperty("opening_seed_vigor_max_basis_points")] public int OpeningSeedVigorMaxBasisPoints { get; set; }
        [JsonProperty("opening_seed_purity_min_basis_points")] public int OpeningSeedPurityMinBasisPoints { get; set; }
        [JsonProperty("opening_seed_purity_max_basis_points")] public int OpeningSeedPurityMaxBasisPoints { get; set; }

        public static HouseholdProductionProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<HouseholdProductionProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.household-production-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                !BasisPoints(value.OwnedLandBasisPoints) ||
                !BasisPoints(value.OpeningSeedConversionBasisPoints) ||
                !BasisPoints(value.NewHouseholdLandAndSeedTransferBasisPoints) ||
                !BasisPoints(value.LeasedLandRentBasisPoints) ||
                value.SeedRetentionTargetBasisPoints < 10_000 ||
                value.SeedRetentionTargetBasisPoints > 50_000 ||
                value.MinimumOrderLandMilliMu <= 0 ||
                !Range(value.OpeningSeedVigorMinBasisPoints,
                    value.OpeningSeedVigorMaxBasisPoints) ||
                !Range(value.OpeningSeedPurityMinBasisPoints,
                    value.OpeningSeedPurityMaxBasisPoints))
            {
                throw new InvalidDataException("The household production profile is invalid.");
            }
            return value;
        }

        private static bool BasisPoints(int value)
        {
            return value >= 0 && value <= 10_000;
        }

        private static bool Range(int minimum, int maximumExclusive)
        {
            return minimum > 0 && maximumExclusive > minimum && maximumExclusive <= 10_001;
        }
    }

    internal sealed class AgriculturalContentBinding
    {
        public string CropId;
        public string VarietyId;
        public string SeedProductId;
        public string HarvestProductId;
        public string RecipeId;
        public string MethodId;
        public long StableIdentity;
        public long SeedQuantity;
        public long HarvestQuantity;
        public int MethodYieldBasisPoints;
    }

    internal sealed class FoodProductContentBinding
    {
        public string ProductId;
        public long StableIdentity;
    }

    internal sealed class ProductionContentProjection
    {
        public string PackageId { get; private set; }
        public string PackageVersion { get; private set; }
        public string ContentSha256 { get; private set; }
        public List<AgriculturalContentBinding> Bindings { get; private set; }
        public List<FoodProductContentBinding> FoodProducts { get; private set; }

        public static ProductionContentProjection Load(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                throw new ArgumentException("At least one production content path is required.");
            var roots = paths.Select(path => JObject.Parse(
                File.ReadAllText(path, Encoding.UTF8))).ToList();
            var crops = RequiredById(roots, "Crops");
            var varieties = RequiredById(roots, "CropVarieties");
            var products = RequiredById(roots, "Products");
            var recipes = RequiredById(roots, "Recipes");
            var methods = RequiredById(roots, "Methods");
            var foodProducts = products.Values
                .Where(item => Tags(item).Contains("product.food"))
                .OrderBy(item => (string)item["Id"], StringComparer.Ordinal)
                .Select(item => new FoodProductContentBinding
                {
                    ProductId = (string)item["Id"],
                    StableIdentity = StableIdentity((string)item["Id"])
                })
                .ToList();
            var bindings = new List<AgriculturalContentBinding>();
            foreach (JObject recipe in recipes.Values
                .Where(item => !string.IsNullOrWhiteSpace((string)item["CropDefinitionId"]))
                .OrderBy(item => (string)item["Id"], StringComparer.Ordinal))
            {
                string cropId = (string)recipe["CropDefinitionId"];
                string recipeId = (string)recipe["Id"];
                if (!crops.ContainsKey(cropId))
                    throw new InvalidDataException("A farm recipe references a missing crop: " + cropId);
                JObject variety = varieties.Values
                    .Where(item => (string)item["CropDefinitionId"] == cropId)
                    .OrderBy(item => (string)item["Id"], StringComparer.Ordinal)
                    .FirstOrDefault();
                JArray inputs = (JArray)recipe["Inputs"];
                JArray outputs = (JArray)recipe["Outputs"];
                if (variety == null || inputs == null || inputs.Count != 1 ||
                    outputs == null || outputs.Count != 1)
                {
                    throw new InvalidDataException(
                        "A farm recipe requires one variety, one seed input, and one harvest output: " +
                        recipeId);
                }
                string seedProductId = (string)inputs[0]["ProductDefinitionId"];
                string harvestProductId = (string)outputs[0]["ProductDefinitionId"];
                JObject seedProduct;
                JObject harvestProduct;
                if (!products.TryGetValue(seedProductId, out seedProduct) ||
                    !products.TryGetValue(harvestProductId, out harvestProduct) ||
                    !Tags(seedProduct).Contains("product.seed") ||
                    !Tags(harvestProduct).Contains("product.food"))
                {
                    throw new InvalidDataException(
                        "A farm recipe input/output does not resolve to seed and food products: " +
                        recipeId);
                }
                JObject method = methods.Values
                    .Where(item => ((JArray)item["RecipeDefinitionIds"] ?? new JArray())
                        .Values<string>().Contains(recipeId, StringComparer.Ordinal))
                    .OrderBy(item => (string)item["Id"], StringComparer.Ordinal)
                    .FirstOrDefault();
                long seedQuantity = (long)inputs[0]["QuantityPerLandUnit"];
                long harvestQuantity = (long)outputs[0]["QuantityPerLandUnit"];
                if (method == null || seedQuantity <= 0 || harvestQuantity <= seedQuantity)
                    throw new InvalidDataException("A farm recipe has an invalid method or quantity: " + recipeId);
                bindings.Add(new AgriculturalContentBinding
                {
                    CropId = cropId,
                    VarietyId = (string)variety["Id"],
                    SeedProductId = seedProductId,
                    HarvestProductId = harvestProductId,
                    RecipeId = recipeId,
                    MethodId = (string)method["Id"],
                    StableIdentity = StableIdentity(
                        cropId, (string)variety["Id"], seedProductId,
                        harvestProductId, recipeId, (string)method["Id"]),
                    SeedQuantity = seedQuantity,
                    HarvestQuantity = harvestQuantity,
                    MethodYieldBasisPoints = (int?)method["YieldBasisPoints"] ?? 10_000
                });
            }
            if (bindings.Count == 0 || foodProducts.Count == 0)
                throw new InvalidDataException("The production content contains no agricultural binding.");
            if (bindings.Select(item => item.StableIdentity).Distinct().Count() != bindings.Count)
                throw new InvalidDataException("Agricultural binding stable identities collide.");
            return new ProductionContentProjection
            {
                PackageId = string.Join("+", roots.Select(root =>
                    RequiredString(root, "PackageId"))),
                PackageVersion = string.Join("+", roots.Select(root =>
                    RequiredString(root, "Version"))),
                ContentSha256 = HashFiles(paths),
                Bindings = bindings,
                FoodProducts = foodProducts
            };
        }

        private static Dictionary<string, JObject> RequiredById(
            IEnumerable<JObject> roots,
            string name)
        {
            var result = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JObject root in roots)
            {
                var array = root[name] as JArray;
                if (array == null)
                    throw new InvalidDataException(
                        "Missing production content array: " + name);
                foreach (JObject item in array)
                {
                    string id = RequiredString(item, "Id");
                    if (!id.Contains(".") || result.ContainsKey(id))
                        throw new InvalidDataException(
                            "Duplicate or invalid production content ID: " + id);
                    result.Add(id, item);
                }
            }
            return result;
        }

        private static HashSet<string> Tags(JObject product)
        {
            return new HashSet<string>(
                ((JArray)product["CategoryTags"] ?? new JArray()).Values<string>(),
                StringComparer.Ordinal);
        }

        private static string RequiredString(JObject value, string name)
        {
            string result = (string)value[name];
            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidDataException("Missing production content value: " + name);
            return result;
        }

        private static string HashFiles(IEnumerable<string> paths)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var builder = new StringBuilder();
                foreach (string path in paths)
                {
                    using (var stream = File.OpenRead(path))
                    {
                        foreach (byte value in sha.ComputeHash(stream))
                            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                    }
                }
                foreach (byte value in sha.ComputeHash(
                    Encoding.UTF8.GetBytes(builder.ToString())))
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString(builder.Length - 64, 64);
            }
        }

        private static long StableIdentity(params string[] values)
        {
            ulong hash = 14695981039346656037UL;
            string canonical = string.Join("\u001f", values);
            foreach (byte value in Encoding.UTF8.GetBytes(canonical))
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
            long result = unchecked((long)(hash & 0x7FFFFFFFFFFFFFFFUL));
            return result == 0 ? 1 : result;
        }
    }

    internal sealed partial class HouseholdRecord
    {
        public long OwnedArableLandMilliMu;
        public long LeasedArableLandMilliMu;
        public long SeedInventoryMilliRations;
        public int SeedVigorBasisPoints;
        public int SeedPurityBasisPoints;
        public int CropBindingIndex;
        public int LastHarvestYearIndex;
        public long SeedBatchStableId;
        public long LastHarvestBatchStableId;
        public long LastHarvestAvailableMilliRations;
        public long CumulativeSeedConsumedMilliRations;
        public long CumulativeGrossHarvestMilliRations;
        public long CumulativeSeedRetainedMilliRations;
        public long CumulativeLandRentMilliRations;
        public long CumulativeFarmWorkOrders;
    }

    internal sealed partial class CountySubsistenceState
    {
        public long PublicArableLandMilliMu;
        public long GovernmentSeedInventoryMilliRations;
    }

    internal sealed partial class AnnualCountyResourceRecord
    {
        [JsonProperty("farm_work_order_count", NullValueHandling = NullValueHandling.Ignore)] public long? FarmWorkOrderCount { get; set; }
        [JsonProperty("seed_consumed_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? SeedConsumedMilliRations { get; set; }
        [JsonProperty("seed_retained_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? SeedRetainedMilliRations { get; set; }
        [JsonProperty("land_rent_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? LandRentMilliRations { get; set; }
        [JsonProperty("closing_household_seed_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? ClosingHouseholdSeedMilliRations { get; set; }
        [JsonProperty("closing_government_seed_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? ClosingGovernmentSeedMilliRations { get; set; }
        [JsonProperty("assigned_owned_land_milli_mu", NullValueHandling = NullValueHandling.Ignore)] public long? AssignedOwnedLandMilliMu { get; set; }
        [JsonProperty("assigned_leased_land_milli_mu", NullValueHandling = NullValueHandling.Ignore)] public long? AssignedLeasedLandMilliMu { get; set; }
        [JsonProperty("public_unassigned_land_milli_mu", NullValueHandling = NullValueHandling.Ignore)] public long? PublicUnassignedLandMilliMu { get; set; }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("household_production_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string HouseholdProductionProfileId { get; set; }
        [JsonProperty("production_content_package_id", NullValueHandling = NullValueHandling.Ignore)] public string ProductionContentPackageId { get; set; }
        [JsonProperty("production_content_package_version", NullValueHandling = NullValueHandling.Ignore)] public string ProductionContentPackageVersion { get; set; }
        [JsonProperty("production_content_sha256", NullValueHandling = NullValueHandling.Ignore)] public string ProductionContentSha256 { get; set; }
        [JsonProperty("agricultural_binding_count", NullValueHandling = NullValueHandling.Ignore)] public int? AgriculturalBindingCount { get; set; }
        [JsonProperty("total_farm_work_orders", NullValueHandling = NullValueHandling.Ignore)] public long? TotalFarmWorkOrders { get; set; }
        [JsonProperty("total_seed_consumed_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalSeedConsumedMilliRations { get; set; }
        [JsonProperty("total_seed_retained_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalSeedRetainedMilliRations { get; set; }
        [JsonProperty("total_land_rent_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? TotalLandRentMilliRations { get; set; }
        [JsonProperty("final_seed_inventory_milli_rations", NullValueHandling = NullValueHandling.Ignore)] public long? FinalSeedInventoryMilliRations { get; set; }
        [JsonProperty("household_production", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence HouseholdProduction { get; set; }
        [JsonProperty("farm_work_orders", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence FarmWorkOrders { get; set; }
        [JsonProperty("household_production_digest", NullValueHandling = NullValueHandling.Ignore)] public string HouseholdProductionDigest { get; set; }
    }

    internal sealed partial class DemographicWorldRunner
    {
        private const int HouseholdProductionRecordBytes = 116;
        private const int FarmWorkOrderRecordBytes = 92;
        private HouseholdProductionProfile _householdProductionProfile;
        private ProductionContentProjection _productionContent;
        private FarmWorkOrderWriter _farmWorkOrderWriter;
        private long _totalFarmWorkOrders;
        private long _totalSeedConsumed;
        private long _totalSeedRetained;
        private long _totalLandRent;

        public void ConfigureHouseholdProduction(
            HouseholdProductionProfile profile,
            ProductionContentProjection content)
        {
            _householdProductionProfile = profile;
            _productionContent = content;
        }

        private void InitializeHouseholdProduction()
        {
            var members = new long[_households.Count];
            for (var i = 0; i < _people.Count; i++)
                if (_people[i].Alive) members[checked((int)_people[i].HouseholdId - 1)]++;
            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                List<int> indexes = _householdIndexesByCounty[countyIndex];
                var weights = new long[indexes.Count];
                long totalMembers = 0;
                for (var h = 0; h < indexes.Count; h++)
                {
                    weights[h] = members[indexes[h]];
                    totalMembers += weights[h];
                }
                long land = _countySubsistence[countyIndex].ArableLandMilliMu;
                long[] shares = AllocateLocal(land, weights, totalMembers);
                for (var h = 0; h < indexes.Count; h++)
                {
                    HouseholdRecord household = _households[indexes[h]];
                    household.OwnedArableLandMilliMu = checked(
                        shares[h] * _householdProductionProfile.OwnedLandBasisPoints / 10_000L);
                    household.LeasedArableLandMilliMu =
                        shares[h] - household.OwnedArableLandMilliMu;
                    long seed = checked(
                        household.FoodInventoryMilliRations *
                        _householdProductionProfile.OpeningSeedConversionBasisPoints / 10_000L);
                    household.FoodInventoryMilliRations -= seed;
                    household.SeedInventoryMilliRations = seed;
                    household.SeedVigorBasisPoints = StableRandom.Range(
                        _options.Seed, 601UL, household.Id, countyIndex,
                        _householdProductionProfile.OpeningSeedVigorMinBasisPoints,
                        _householdProductionProfile.OpeningSeedVigorMaxBasisPoints);
                    household.SeedPurityBasisPoints = StableRandom.Range(
                        _options.Seed, 602UL, household.Id, countyIndex,
                        _householdProductionProfile.OpeningSeedPurityMinBasisPoints,
                        _householdProductionProfile.OpeningSeedPurityMaxBasisPoints);
                    household.CropBindingIndex = SelectFoodEcologyCropBinding(
                        household.Id, countyIndex);
                    household.SeedBatchStableId = seed == 0
                        ? 0
                        : BatchIdentity(
                            household.Id,
                            _productionContent.Bindings[household.CropBindingIndex]
                                .StableIdentity,
                            0,
                            1);
                }
            }
        }

        private void TransferProductionAssetsOnMarriage(
            HouseholdRecord manHousehold,
            HouseholdRecord womanHousehold,
            HouseholdRecord newHousehold)
        {
            if (_householdProductionProfile == null) return;
            int basisPoints = _householdProductionProfile
                .NewHouseholdLandAndSeedTransferBasisPoints;
            long manOwned = manHousehold.OwnedArableLandMilliMu * basisPoints / 10_000L;
            long womanOwned = womanHousehold.OwnedArableLandMilliMu * basisPoints / 10_000L;
            long manLeased = manHousehold.LeasedArableLandMilliMu * basisPoints / 10_000L;
            long womanLeased = womanHousehold.LeasedArableLandMilliMu * basisPoints / 10_000L;
            long manSeed = manHousehold.SeedInventoryMilliRations * basisPoints / 10_000L;
            long womanSeed = womanHousehold.SeedInventoryMilliRations * basisPoints / 10_000L;
            manHousehold.OwnedArableLandMilliMu -= manOwned;
            womanHousehold.OwnedArableLandMilliMu -= womanOwned;
            manHousehold.LeasedArableLandMilliMu -= manLeased;
            womanHousehold.LeasedArableLandMilliMu -= womanLeased;
            manHousehold.SeedInventoryMilliRations -= manSeed;
            womanHousehold.SeedInventoryMilliRations -= womanSeed;
            newHousehold.OwnedArableLandMilliMu = checked(manOwned + womanOwned);
            newHousehold.LeasedArableLandMilliMu = checked(manLeased + womanLeased);
            newHousehold.SeedInventoryMilliRations = checked(manSeed + womanSeed);
            long seedTotal = newHousehold.SeedInventoryMilliRations;
            newHousehold.SeedVigorBasisPoints = seedTotal == 0
                ? manHousehold.SeedVigorBasisPoints
                : checked((int)((manSeed * manHousehold.SeedVigorBasisPoints +
                    womanSeed * womanHousehold.SeedVigorBasisPoints) / seedTotal));
            newHousehold.SeedPurityBasisPoints = seedTotal == 0
                ? manHousehold.SeedPurityBasisPoints
                : checked((int)((manSeed * manHousehold.SeedPurityBasisPoints +
                    womanSeed * womanHousehold.SeedPurityBasisPoints) / seedTotal));
            newHousehold.CropBindingIndex = manSeed >= womanSeed
                ? manHousehold.CropBindingIndex
                : womanHousehold.CropBindingIndex;
            newHousehold.SeedBatchStableId = seedTotal == 0
                ? 0
                : BatchIdentity(
                    newHousehold.Id,
                    _productionContent.Bindings[newHousehold.CropBindingIndex]
                        .StableIdentity,
                    newHousehold.FoundedDay,
                    1);
        }

        private void TransferExtinctProductionAssets(HouseholdRecord household)
        {
            if (_householdProductionProfile == null) return;
            CountySubsistenceState county = _countySubsistence[household.CountyIndex];
            TrackExtinctFoodEcologySeedTransfer(household, county);
            county.PublicArableLandMilliMu = checked(
                county.PublicArableLandMilliMu + household.OwnedArableLandMilliMu +
                household.LeasedArableLandMilliMu);
            county.GovernmentSeedInventoryMilliRations = checked(
                county.GovernmentSeedInventoryMilliRations +
                household.SeedInventoryMilliRations);
            household.OwnedArableLandMilliMu = 0;
            household.LeasedArableLandMilliMu = 0;
            household.SeedInventoryMilliRations = 0;
            household.SeedBatchStableId = 0;
        }

        private long ApplyProductionStockLoss(int countyIndex, int basisPoints)
        {
            if (_householdProductionProfile == null) return 0;
            long total = 0;
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
            {
                HouseholdRecord household = _households[indexes[h]];
                long loss = checked(
                    household.SeedInventoryMilliRations * basisPoints / 10_000L);
                household.SeedInventoryMilliRations -= loss;
                total = checked(total + loss);
            }
            CountySubsistenceState county = _countySubsistence[countyIndex];
            long governmentLoss = checked(
                county.GovernmentSeedInventoryMilliRations * basisPoints / 10_000L);
            county.GovernmentSeedInventoryMilliRations -= governmentLoss;
            ApplyFoodEcologyGovernmentSeedLoss(county, governmentLoss);
            return checked(total + governmentLoss);
        }

        private long CountyProductionSeed(int countyIndex)
        {
            if (_householdProductionProfile == null) return 0;
            long total = _countySubsistence[countyIndex].GovernmentSeedInventoryMilliRations;
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            for (var h = 0; h < indexes.Count; h++)
                total = checked(total + _households[indexes[h]].SeedInventoryMilliRations);
            return total;
        }

        private ProductionYearResult ExecuteHouseholdProduction(
            int countyIndex,
            int yearIndex,
            int weatherBasisPoints,
            long[] householdWorkers)
        {
            var result = new ProductionYearResult();
            List<int> indexes = _householdIndexesByCounty[countyIndex];
            long[] seasonalPublicLand = AllocateSeasonalPublicLand(
                countyIndex, householdWorkers);
            for (var h = 0; h < indexes.Count; h++)
            {
                int householdIndex = indexes[h];
                HouseholdRecord household = _households[householdIndex];
                long land = checked(
                    household.OwnedArableLandMilliMu +
                    household.LeasedArableLandMilliMu +
                    seasonalPublicLand[h]);
                long workers = householdWorkers[householdIndex];
                long cultivated = Math.Min(
                    land,
                    checked(workers * _subsistenceProfile.LaborCapacityMilliMuPerWorker));
                if (cultivated < _householdProductionProfile.MinimumOrderLandMilliMu ||
                    household.SeedInventoryMilliRations <= 0) continue;
                AgriculturalContentBinding binding =
                    _productionContent.Bindings[household.CropBindingIndex];
                long baseGross = checked(
                    cultivated * _subsistenceProfile.GrossYieldMilliRationsPerMu / 1_000L);
                long seedRequired = Math.Max(
                    1L, checked(baseGross * binding.SeedQuantity / binding.HarvestQuantity));
                if (household.SeedInventoryMilliRations < seedRequired)
                {
                    cultivated = checked(
                        cultivated * household.SeedInventoryMilliRations / seedRequired);
                    if (cultivated < _householdProductionProfile.MinimumOrderLandMilliMu)
                        continue;
                    baseGross = checked(
                        cultivated * _subsistenceProfile.GrossYieldMilliRationsPerMu / 1_000L);
                    seedRequired = Math.Max(
                        1L, checked(baseGross * binding.SeedQuantity / binding.HarvestQuantity));
                }
                seedRequired = Math.Min(seedRequired, household.SeedInventoryMilliRations);
                household.SeedInventoryMilliRations -= seedRequired;
                long gross = checked(baseGross * weatherBasisPoints / 10_000L);
                gross = checked(gross * binding.MethodYieldBasisPoints / 10_000L);
                gross = ApplyFoodEcologyHarvestAdjustment(
                    household, countyIndex, yearIndex, gross);
                gross = checked(gross * household.SeedVigorBasisPoints / 10_000L);
                gross = checked(gross * household.SeedPurityBasisPoints / 10_000L);
                long targetSeed = checked(
                    seedRequired * _householdProductionProfile.SeedRetentionTargetBasisPoints /
                    10_000L);
                long retained = Math.Min(
                    gross, Math.Max(0, targetSeed - household.SeedInventoryMilliRations));
                household.SeedInventoryMilliRations = checked(
                    household.SeedInventoryMilliRations + retained);
                long edible = gross - retained;
                long leasedCultivated = land == 0
                    ? 0
                    : checked(cultivated * checked(
                        household.LeasedArableLandMilliMu + seasonalPublicLand[h]) / land);
                long seasonalCultivated = land == 0
                    ? 0
                    : checked(cultivated * seasonalPublicLand[h] / land);
                long rent = cultivated == 0
                    ? 0
                    : checked(edible * leasedCultivated / cultivated *
                        _householdProductionProfile.LeasedLandRentBasisPoints / 10_000L);
                long afterRent = edible - rent;
                long tax = checked(
                    afterRent * _marketReliefProfile.GrainTaxBasisPoints / 10_000L);
                long available = afterRent - tax;
                long harvestBatchStableId = BatchIdentity(
                    household.Id, binding.StableIdentity, yearIndex, 2);
                household.FoodInventoryMilliRations = checked(
                    household.FoodInventoryMilliRations + available);
                TrackHouseholdFoodAdded(
                    household, binding.HarvestProductId, available, true);
                CountySubsistenceState county = _countySubsistence[countyIndex];
                county.GovernmentGranaryFoodMilliRations = checked(
                    county.GovernmentGranaryFoodMilliRations + rent + tax);
                TrackGovernmentFoodAdded(
                    county, binding.HarvestProductId, rent + tax, true);
                household.CumulativeTaxFoodMilliRations = checked(
                    household.CumulativeTaxFoodMilliRations + tax);
                household.CumulativeSeedConsumedMilliRations = checked(
                    household.CumulativeSeedConsumedMilliRations + seedRequired);
                household.CumulativeGrossHarvestMilliRations = checked(
                    household.CumulativeGrossHarvestMilliRations + gross);
                household.CumulativeSeedRetainedMilliRations = checked(
                    household.CumulativeSeedRetainedMilliRations + retained);
                household.CumulativeLandRentMilliRations = checked(
                    household.CumulativeLandRentMilliRations + rent);
                household.CumulativeFarmWorkOrders++;
                household.LastHarvestYearIndex = yearIndex;
                household.LastHarvestBatchStableId = harvestBatchStableId;
                household.LastHarvestAvailableMilliRations = available;
                household.SeedBatchStableId = household.SeedInventoryMilliRations == 0
                    ? 0
                    : BatchIdentity(
                        household.Id, binding.StableIdentity, yearIndex, 1);
                _farmWorkOrderWriter.Write(
                    household.Id, yearIndex, countyIndex,
                    checked((int)Math.Min(int.MaxValue, workers)),
                    binding.StableIdentity, harvestBatchStableId,
                    cultivated, seedRequired,
                    gross, retained, rent, tax, available);
                result.WorkOrderCount++;
                result.CultivatedLandMilliMu = checked(
                    result.CultivatedLandMilliMu + cultivated);
                result.GrossHarvestMilliRations = checked(
                    result.GrossHarvestMilliRations + gross);
                result.SeedConsumedMilliRations = checked(
                    result.SeedConsumedMilliRations + seedRequired);
                result.SeedRetainedMilliRations = checked(
                    result.SeedRetainedMilliRations + retained);
                result.LandRentMilliRations = checked(
                    result.LandRentMilliRations + rent);
                result.SeasonalPublicLandCultivatedMilliMu = checked(
                    result.SeasonalPublicLandCultivatedMilliMu + seasonalCultivated);
                result.GrainTaxMilliRations = checked(
                    result.GrainTaxMilliRations + tax);
            }
            _totalFarmWorkOrders = checked(_totalFarmWorkOrders + result.WorkOrderCount);
            _totalSeedConsumed = checked(
                _totalSeedConsumed + result.SeedConsumedMilliRations);
            _totalSeedRetained = checked(
                _totalSeedRetained + result.SeedRetainedMilliRations);
            _totalLandRent = checked(_totalLandRent + result.LandRentMilliRations);
            return result;
        }

        private void WriteHouseholdProduction(string path)
        {
            using (var stream = new FileStream(
                path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4D323457);
                writer.Write(1);
                writer.Write((long)_households.Count);
                for (var i = 0; i < _households.Count; i++)
                {
                    HouseholdRecord value = _households[i];
                    writer.Write(value.Id);
                    writer.Write(value.OwnedArableLandMilliMu);
                    writer.Write(value.LeasedArableLandMilliMu);
                    writer.Write(value.SeedInventoryMilliRations);
                    writer.Write(value.SeedVigorBasisPoints);
                    writer.Write(value.SeedPurityBasisPoints);
                    writer.Write(_productionContent.Bindings[value.CropBindingIndex]
                        .StableIdentity);
                    writer.Write(value.LastHarvestYearIndex);
                    writer.Write(value.SeedBatchStableId);
                    writer.Write(value.LastHarvestBatchStableId);
                    writer.Write(value.LastHarvestAvailableMilliRations);
                    writer.Write(value.CumulativeSeedConsumedMilliRations);
                    writer.Write(value.CumulativeGrossHarvestMilliRations);
                    writer.Write(value.CumulativeSeedRetainedMilliRations);
                    writer.Write(value.CumulativeLandRentMilliRations);
                    writer.Write(value.CumulativeFarmWorkOrders);
                }
                stream.Flush(true);
            }
        }

        private void ValidateHouseholdProduction()
        {
            if (_householdProductionProfile == null) return;
            if (_farmWorkOrderWriter != null ||
                _farmWorkOrderCountAtClose != _totalFarmWorkOrders ||
                _households.Sum(item => item.CumulativeFarmWorkOrders) !=
                    _totalFarmWorkOrders ||
                _households.Sum(item => item.CumulativeSeedConsumedMilliRations) !=
                    _totalSeedConsumed ||
                _households.Sum(item => item.CumulativeSeedRetainedMilliRations) !=
                    _totalSeedRetained ||
                _households.Sum(item => item.CumulativeLandRentMilliRations) !=
                    _totalLandRent ||
                _households.Any(item => item.SeedInventoryMilliRations < 0 ||
                    item.OwnedArableLandMilliMu < 0 || item.LeasedArableLandMilliMu < 0 ||
                    item.CropBindingIndex < 0 ||
                    item.CropBindingIndex >= _productionContent.Bindings.Count ||
                    item.LastHarvestAvailableMilliRations < 0 ||
                    item.SeedInventoryMilliRations > 0 && item.SeedBatchStableId <= 0 ||
                    item.LastHarvestYearIndex > 0 && item.LastHarvestBatchStableId <= 0))
            {
                throw new InvalidOperationException("Household production totals do not reconcile.");
            }
            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                long assigned = _householdIndexesByCounty[countyIndex].Sum(index => checked(
                    _households[index].OwnedArableLandMilliMu +
                    _households[index].LeasedArableLandMilliMu));
                if (checked(assigned + _countySubsistence[countyIndex].PublicArableLandMilliMu) !=
                    _countySubsistence[countyIndex].ArableLandMilliMu)
                {
                    throw new InvalidOperationException("County household land does not conserve.");
                }
            }
        }

        private long _farmWorkOrderCountAtClose;

        private static long BatchIdentity(
            long householdId,
            long bindingStableId,
            int timeCoordinate,
            int purpose)
        {
            ulong value = unchecked((ulong)householdId) * 0x9E3779B97F4A7C15UL;
            value ^= unchecked((ulong)bindingStableId) * 0xBF58476D1CE4E5B9UL;
            value ^= unchecked((ulong)timeCoordinate) * 0x94D049BB133111EBUL;
            value ^= unchecked((ulong)purpose) * 0xD6E8FEB86659FD93UL;
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            long result = unchecked((long)(value & 0x7FFFFFFFFFFFFFFFUL));
            return result == 0 ? 1 : result;
        }

        private sealed class ProductionYearResult
        {
            public long WorkOrderCount;
            public long CultivatedLandMilliMu;
            public long GrossHarvestMilliRations;
            public long SeedConsumedMilliRations;
            public long SeedRetainedMilliRations;
            public long LandRentMilliRations;
            public long GrainTaxMilliRations;
            public long SeasonalPublicLandCultivatedMilliMu;
        }

        private sealed class FarmWorkOrderWriter : IDisposable
        {
            private readonly FileStream _stream;
            private readonly BinaryWriter _writer;
            public long Count { get; private set; }

            public FarmWorkOrderWriter(string path)
            {
                _stream = new FileStream(
                    path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1 << 20);
                _writer = new BinaryWriter(_stream, Encoding.UTF8);
                _writer.Write(0x4D32345A);
                _writer.Write(1);
                _writer.Write(0L);
            }

            public void Write(
                long householdId,
                int yearIndex,
                int countyIndex,
                int workerCount,
                long cropBindingStableId,
                long harvestBatchStableId,
                long cultivated,
                long seedInput,
                long gross,
                long retained,
                long rent,
                long tax,
                long householdAvailable)
            {
                if (householdId <= 0 || yearIndex <= 0 || countyIndex < 0 ||
                    workerCount <= 0 || cropBindingStableId <= 0 ||
                    harvestBatchStableId <= 0 || cultivated <= 0 ||
                    seedInput <= 0 || gross < 0 || retained < 0 || rent < 0 || tax < 0 ||
                    householdAvailable < 0 ||
                    gross != checked(retained + rent + tax + householdAvailable))
                {
                    throw new InvalidOperationException("A farm work order is invalid.");
                }
                _writer.Write(householdId);
                _writer.Write(yearIndex);
                _writer.Write(countyIndex);
                _writer.Write(workerCount);
                _writer.Write(cropBindingStableId);
                _writer.Write(harvestBatchStableId);
                _writer.Write(cultivated);
                _writer.Write(seedInput);
                _writer.Write(gross);
                _writer.Write(retained);
                _writer.Write(rent);
                _writer.Write(tax);
                _writer.Write(householdAvailable);
                Count++;
            }

            public void Dispose()
            {
                _writer.Flush();
                _stream.Position = 8;
                _writer.Write(Count);
                _writer.Flush();
                _stream.Flush(true);
                _writer.Dispose();
                _stream.Dispose();
            }
        }
    }
}
