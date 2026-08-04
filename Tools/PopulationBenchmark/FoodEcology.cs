using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal sealed class FoodEcologyProductDefinition
    {
        [JsonProperty("product_definition_id")] public string ProductDefinitionId { get; set; }
        [JsonProperty("opening_share_basis_points")] public int OpeningShareBasisPoints { get; set; }
        [JsonProperty("nutrition_basis_points")] public int NutritionBasisPoints { get; set; }
        [JsonProperty("volume_basis_points")] public int VolumeBasisPoints { get; set; }
        [JsonProperty("spoilage_basis_points")] public int SpoilageBasisPoints { get; set; }
        [JsonProperty("market_value_basis_points")] public int MarketValueBasisPoints { get; set; }
        [JsonProperty("consumption_priority")] public int ConsumptionPriority { get; set; }
    }

    internal sealed class FoodEcologyCropDefinition
    {
        [JsonProperty("recipe_definition_id")] public string RecipeDefinitionId { get; set; }
        [JsonProperty("household_share_basis_points")] public int HouseholdShareBasisPoints { get; set; }
        [JsonProperty("yield_basis_points")] public int YieldBasisPoints { get; set; }
        [JsonProperty("rotation_support_basis_points")] public int RotationSupportBasisPoints { get; set; }
        [JsonProperty("rotation_response_basis_points")] public int RotationResponseBasisPoints { get; set; }
    }

    internal sealed class FoodEcologyProcessingDefinition
    {
        [JsonProperty("input_product_definition_id")] public string InputProductDefinitionId { get; set; }
        [JsonProperty("output_product_definition_id")] public string OutputProductDefinitionId { get; set; }
        [JsonProperty("annual_share_basis_points")] public int AnnualShareBasisPoints { get; set; }
    }

    internal sealed class FoodEcologyProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("products")] public List<FoodEcologyProductDefinition> Products { get; set; }
        [JsonProperty("crops")] public List<FoodEcologyCropDefinition> Crops { get; set; }
        [JsonProperty("processing")] public List<FoodEcologyProcessingDefinition> Processing { get; set; }

        public static FoodEcologyProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<FoodEcologyProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.food-ecology-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                value.Products == null || value.Products.Count < 5 ||
                value.Crops == null || value.Crops.Count < 5 ||
                value.Processing == null)
            {
                throw new InvalidDataException("The food ecology profile is invalid.");
            }
            ValidateProducts(value.Products);
            ValidateCrops(value.Crops);
            ValidateProcessing(value.Processing);
            return value;
        }

        public FoodProductProvenanceProfile CreateProvenanceProfile()
        {
            return new FoodProductProvenanceProfile
            {
                SchemaVersion = "mandate.food-product-provenance-profile.v1",
                Id = Id + ".provenance",
                SourceLayer = "gameplay_completion",
                Description = "Derived from the M24-P7 food ecology profile.",
                Products = Products.Select(item => new FoodProductShare
                {
                    ProductDefinitionId = item.ProductDefinitionId,
                    OpeningShareBasisPoints = item.OpeningShareBasisPoints
                }).ToList()
            };
        }

        private static void ValidateProducts(List<FoodEcologyProductDefinition> products)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            long shares = 0;
            foreach (FoodEcologyProductDefinition item in products)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ProductDefinitionId) ||
                    !ids.Add(item.ProductDefinitionId) ||
                    item.OpeningShareBasisPoints < 0 || item.OpeningShareBasisPoints > 10_000 ||
                    item.NutritionBasisPoints < 1_000 || item.NutritionBasisPoints > 20_000 ||
                    item.VolumeBasisPoints < 1_000 || item.VolumeBasisPoints > 30_000 ||
                    item.SpoilageBasisPoints < 0 || item.SpoilageBasisPoints > 30_000 ||
                    item.MarketValueBasisPoints < 1_000 || item.MarketValueBasisPoints > 30_000 ||
                    item.ConsumptionPriority < 0)
                {
                    throw new InvalidDataException("A food ecology product is invalid.");
                }
                shares += item.OpeningShareBasisPoints;
            }
            if (shares != 10_000)
                throw new InvalidDataException("Food ecology opening shares must total 10000.");
        }

        private static void ValidateCrops(List<FoodEcologyCropDefinition> crops)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            long shares = 0;
            foreach (FoodEcologyCropDefinition item in crops)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.RecipeDefinitionId) ||
                    !ids.Add(item.RecipeDefinitionId) ||
                    item.HouseholdShareBasisPoints < 0 || item.HouseholdShareBasisPoints > 10_000 ||
                    item.YieldBasisPoints < 1_000 || item.YieldBasisPoints > 20_000 ||
                    item.RotationSupportBasisPoints < 0 || item.RotationSupportBasisPoints > 10_000 ||
                    item.RotationResponseBasisPoints < 0 || item.RotationResponseBasisPoints > 10_000)
                {
                    throw new InvalidDataException("A food ecology crop is invalid.");
                }
                shares += item.HouseholdShareBasisPoints;
            }
            if (shares != 10_000)
                throw new InvalidDataException("Food ecology crop shares must total 10000.");
        }

        private static void ValidateProcessing(List<FoodEcologyProcessingDefinition> processing)
        {
            foreach (FoodEcologyProcessingDefinition item in processing)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.InputProductDefinitionId) ||
                    string.IsNullOrWhiteSpace(item.OutputProductDefinitionId) ||
                    item.InputProductDefinitionId == item.OutputProductDefinitionId ||
                    item.AnnualShareBasisPoints < 0 || item.AnnualShareBasisPoints > 10_000)
                {
                    throw new InvalidDataException("A food ecology processing rule is invalid.");
                }
            }
        }
    }

    internal sealed class FoodEcologyProductReport
    {
        [JsonProperty("product_definition_id")] public string ProductDefinitionId { get; set; }
        [JsonProperty("nutrition_basis_points")] public int NutritionBasisPoints { get; set; }
        [JsonProperty("volume_basis_points")] public int VolumeBasisPoints { get; set; }
        [JsonProperty("spoilage_basis_points")] public int SpoilageBasisPoints { get; set; }
        [JsonProperty("market_value_basis_points")] public int MarketValueBasisPoints { get; set; }
        [JsonProperty("processing_input_quantity")] public long ProcessingInputQuantity { get; set; }
        [JsonProperty("processing_output_quantity")] public long ProcessingOutputQuantity { get; set; }
        [JsonProperty("closing_quantity")] public long ClosingQuantity { get; set; }
        [JsonProperty("closing_nutrition_milli_rations")] public long ClosingNutritionMilliRations { get; set; }
        [JsonProperty("closing_volume_milli_units")] public long ClosingVolumeMilliUnits { get; set; }
    }

    internal sealed class FoodEcologyReport
    {
        [JsonProperty("profile_id")] public string ProfileId { get; set; }
        [JsonProperty("crop_binding_count")] public int CropBindingCount { get; set; }
        [JsonProperty("rotation_adjusted_work_orders")] public long RotationAdjustedWorkOrders { get; set; }
        [JsonProperty("processed_quantity")] public long ProcessedQuantity { get; set; }
        [JsonProperty("consumed_nutrition_milli_rations")] public long ConsumedNutritionMilliRations { get; set; }
        [JsonProperty("minimum_market_price_basis_points")] public int MinimumMarketPriceBasisPoints { get; set; }
        [JsonProperty("maximum_market_price_basis_points")] public int MaximumMarketPriceBasisPoints { get; set; }
        [JsonProperty("products")] public List<FoodEcologyProductReport> Products { get; set; }
    }

    internal sealed partial class WorldOptions
    {
        public string FoodEcologyProfilePath { get; private set; }
        public string FoodContentExtensionPath { get; private set; }
        public bool HasFoodEcology
        {
            get { return !string.IsNullOrWhiteSpace(FoodEcologyProfilePath); }
        }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("food_ecology_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string FoodEcologyProfileId { get; set; }
        [JsonProperty("food_ecology", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence FoodEcology { get; set; }
        [JsonProperty("food_ecology_digest", NullValueHandling = NullValueHandling.Ignore)] public string FoodEcologyDigest { get; set; }
        [JsonProperty("food_ecology_rotation_adjusted_work_orders", NullValueHandling = NullValueHandling.Ignore)] public long? FoodEcologyRotationAdjustedWorkOrders { get; set; }
        [JsonProperty("food_ecology_processed_quantity", NullValueHandling = NullValueHandling.Ignore)] public long? FoodEcologyProcessedQuantity { get; set; }
        [JsonProperty("food_ecology_consumed_nutrition", NullValueHandling = NullValueHandling.Ignore)] public long? FoodEcologyConsumedNutrition { get; set; }
    }

    internal sealed partial class CountySubsistenceState
    {
        public long[] GovernmentSeedByCropBinding;
    }

    internal sealed partial class DemographicWorldRunner
    {
        private FoodEcologyProfile _foodEcologyProfile;
        private Dictionary<string, FoodEcologyProductDefinition> _foodEcologyProducts;
        private Dictionary<string, FoodEcologyCropDefinition> _foodEcologyCrops;
        private long[] _foodProcessingInput;
        private long[] _foodProcessingOutput;
        private int[] _countyRotationSupportBasisPoints;
        private long _rotationAdjustedWorkOrders;
        private long _processedFoodQuantity;
        private long _consumedNutrition;
        private int _minimumEcologyMarketPriceBasisPoints = int.MaxValue;
        private int _maximumEcologyMarketPriceBasisPoints;

        public void ConfigureFoodEcology(FoodEcologyProfile profile)
        {
            _foodEcologyProfile = profile ?? throw new ArgumentNullException(nameof(profile));
            _foodEcologyProducts = profile.Products.ToDictionary(
                item => item.ProductDefinitionId, StringComparer.Ordinal);
            _foodEcologyCrops = profile.Crops.ToDictionary(
                item => item.RecipeDefinitionId, StringComparer.Ordinal);
        }

        private int SelectFoodEcologyCropBinding(long householdId, int countyIndex)
        {
            if (_foodEcologyProfile == null)
            {
                return StableRandom.Range(
                    _options.Seed, 603UL, householdId, countyIndex,
                    0, _productionContent.Bindings.Count);
            }
            var weights = new long[_productionContent.Bindings.Count];
            long total = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                FoodEcologyCropDefinition crop;
                if (_foodEcologyCrops.TryGetValue(
                    _productionContent.Bindings[i].RecipeId, out crop))
                {
                    weights[i] = crop.HouseholdShareBasisPoints;
                    total += weights[i];
                }
            }
            if (total != 10_000)
                throw new InvalidDataException(
                    "Food ecology crop bindings do not resolve to a complete share.");
            long roll = StableRandom.Range(
                _options.Seed, 703UL, householdId, countyIndex, 0, 10_000);
            long cursor = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                cursor += weights[i];
                if (roll < cursor) return i;
            }
            return weights.Length - 1;
        }

        private void InitializeFoodEcology()
        {
            if (_foodEcologyProfile == null) return;
            foreach (FoodEcologyProductDefinition product in _foodEcologyProfile.Products)
                FoodProductIndex(product.ProductDefinitionId);
            foreach (FoodEcologyCropDefinition crop in _foodEcologyProfile.Crops)
                if (!_productionContent.Bindings.Any(
                    item => item.RecipeId == crop.RecipeDefinitionId))
                    throw new InvalidDataException(
                        "Food ecology references a missing crop recipe: " +
                        crop.RecipeDefinitionId);
            foreach (FoodEcologyProcessingDefinition rule in _foodEcologyProfile.Processing)
            {
                FoodProductIndex(rule.InputProductDefinitionId);
                FoodProductIndex(rule.OutputProductDefinitionId);
            }
            _foodProcessingInput = new long[_foodProductIndexById.Count];
            _foodProcessingOutput = new long[_foodProductIndexById.Count];
            _countyRotationSupportBasisPoints = new int[_input.Counties.Count];
            for (var countyIndex = 0; countyIndex < _input.Counties.Count; countyIndex++)
            {
                _countySubsistence[countyIndex].GovernmentSeedByCropBinding =
                    new long[_productionContent.Bindings.Count];
                long land = 0;
                long support = 0;
                foreach (int index in _householdIndexesByCounty[countyIndex])
                {
                    HouseholdRecord household = _households[index];
                    long householdLand = checked(
                        household.OwnedArableLandMilliMu +
                        household.LeasedArableLandMilliMu);
                    FoodEcologyCropDefinition crop = _foodEcologyCrops[
                        _productionContent.Bindings[household.CropBindingIndex].RecipeId];
                    land += householdLand;
                    support += checked(householdLand * crop.RotationSupportBasisPoints);
                }
                _countyRotationSupportBasisPoints[countyIndex] = land == 0
                    ? 0
                    : checked((int)(support / land));
            }
        }

        private void TrackExtinctFoodEcologySeedTransfer(
            HouseholdRecord household,
            CountySubsistenceState county)
        {
            if (_foodEcologyProfile == null ||
                household.SeedInventoryMilliRations == 0) return;
            county.GovernmentSeedByCropBinding[household.CropBindingIndex] = checked(
                county.GovernmentSeedByCropBinding[household.CropBindingIndex] +
                household.SeedInventoryMilliRations);
        }

        private void ApplyFoodEcologyGovernmentSeedLoss(
            CountySubsistenceState county,
            long loss)
        {
            if (_foodEcologyProfile == null || loss == 0) return;
            long[] removed = RemoveVector(
                county.GovernmentSeedByCropBinding, loss);
            if (removed.Sum() != loss)
                throw new InvalidOperationException(
                    "Government seed ecology loss did not settle.");
        }

        private long ApplyFoodEcologyHarvestAdjustment(
            HouseholdRecord household,
            int countyIndex,
            int yearIndex,
            long gross)
        {
            if (_foodEcologyProfile == null) return gross;
            AgriculturalContentBinding binding =
                _productionContent.Bindings[household.CropBindingIndex];
            FoodEcologyCropDefinition crop = _foodEcologyCrops[binding.RecipeId];
            long adjusted = checked(gross * crop.YieldBasisPoints / 10_000L);
            int rotation = checked((int)(
                (long)_countyRotationSupportBasisPoints[countyIndex] *
                crop.RotationResponseBasisPoints / 10_000L));
            if (rotation > 0)
            {
                adjusted = checked(adjusted * (10_000L + rotation) / 10_000L);
                _rotationAdjustedWorkOrders++;
            }
            return adjusted;
        }

        private void ExecuteFoodEcologyProcessing(int countyIndex)
        {
            if (_foodEcologyProfile == null) return;
            foreach (int householdIndex in _householdIndexesByCounty[countyIndex])
            {
                HouseholdRecord household = _households[householdIndex];
                EnsureFoodVector(household);
                foreach (FoodEcologyProcessingDefinition rule in
                    _foodEcologyProfile.Processing)
                {
                    int input = FoodProductIndex(rule.InputProductDefinitionId);
                    int output = FoodProductIndex(rule.OutputProductDefinitionId);
                    long quantity = checked(
                        household.FoodProductQuantities[input] *
                        rule.AnnualShareBasisPoints / 10_000L);
                    if (quantity <= 0) continue;
                    household.FoodProductQuantities[input] -= quantity;
                    household.FoodProductQuantities[output] = checked(
                        household.FoodProductQuantities[output] + quantity);
                    _foodProcessingInput[input] = checked(
                        _foodProcessingInput[input] + quantity);
                    _foodProcessingOutput[output] = checked(
                        _foodProcessingOutput[output] + quantity);
                    _processedFoodQuantity = checked(
                        _processedFoodQuantity + quantity);
                }
            }
        }

        private long[] RemoveFoodEcologyVector(
            long[] source,
            long quantity,
            FoodSinkKind sink)
        {
            if (_foodEcologyProfile == null) return RemoveVector(source, quantity);
            IEnumerable<int> order = Enumerable.Range(0, source.Length);
            if (sink == FoodSinkKind.Spoilage || sink == FoodSinkKind.TransportLoss)
            {
                order = order.OrderByDescending(index =>
                    _foodEcologyProducts[_foodProductProvenanceProfile.Products[index]
                        .ProductDefinitionId].SpoilageBasisPoints)
                    .ThenBy(index => index);
            }
            else if (sink == FoodSinkKind.Consumption ||
                     sink == FoodSinkKind.TransportProvisions)
            {
                order = order.OrderBy(index =>
                    _foodEcologyProducts[_foodProductProvenanceProfile.Products[index]
                        .ProductDefinitionId].ConsumptionPriority)
                    .ThenBy(index => index);
            }
            var removed = new long[source.Length];
            long remaining = quantity;
            foreach (int index in order)
            {
                long take = Math.Min(source[index], remaining);
                source[index] -= take;
                removed[index] = take;
                remaining -= take;
                if (remaining == 0) break;
            }
            if (remaining != 0)
                throw new InvalidOperationException(
                    "A food ecology flow exceeded its source inventory.");
            return removed;
        }

        private long FoodNutrition(long[] quantities)
        {
            if (quantities == null) return 0;
            if (_foodEcologyProfile == null) return quantities.Sum();
            long total = 0;
            for (var i = 0; i < quantities.Length; i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                total = checked(total + quantities[i] *
                    _foodEcologyProducts[id].NutritionBasisPoints / 10_000L);
            }
            return total;
        }

        private int AdjustFoodEcologyMarketPrice(int countyIndex, int price)
        {
            if (_foodEcologyProfile == null) return price;
            var totals = new long[_foodProductIndexById.Count];
            foreach (int householdIndex in _householdIndexesByCounty[countyIndex])
            {
                EnsureFoodVector(_households[householdIndex]);
                AddVector(totals, _households[householdIndex].FoodProductQuantities);
            }
            EnsureFoodVector(_countySubsistence[countyIndex]);
            AddVector(totals,
                _countySubsistence[countyIndex].GovernmentFoodProductQuantities);
            long quantity = totals.Sum();
            long weighted = 0;
            for (var i = 0; i < totals.Length; i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                weighted = checked(weighted + totals[i] *
                    _foodEcologyProducts[id].MarketValueBasisPoints);
            }
            int basisPoints = quantity == 0 ? 10_000 : checked((int)(weighted / quantity));
            _minimumEcologyMarketPriceBasisPoints = Math.Min(
                _minimumEcologyMarketPriceBasisPoints, basisPoints);
            _maximumEcologyMarketPriceBasisPoints = Math.Max(
                _maximumEcologyMarketPriceBasisPoints, basisPoints);
            return Math.Max(1, checked((int)(price * (long)basisPoints / 10_000L)));
        }

        private long AdjustFoodEcologyTransportCapacity(
            CountySubsistenceState donor,
            long capacity)
        {
            if (_foodEcologyProfile == null || capacity <= 0) return capacity;
            EnsureFoodVector(donor);
            long quantity = donor.GovernmentFoodProductQuantities.Sum();
            if (quantity == 0) return capacity;
            long weighted = 0;
            for (var i = 0; i < donor.GovernmentFoodProductQuantities.Length; i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                weighted = checked(weighted +
                    donor.GovernmentFoodProductQuantities[i] *
                    _foodEcologyProducts[id].VolumeBasisPoints);
            }
            long average = Math.Max(1, weighted / quantity);
            return checked(capacity * 10_000L / average);
        }

        private long FoodEcologyProcessingInput(int index)
        {
            return _foodProcessingInput == null ? 0 : _foodProcessingInput[index];
        }

        private long FoodEcologyProcessingOutput(int index)
        {
            return _foodProcessingOutput == null ? 0 : _foodProcessingOutput[index];
        }

        private FoodEcologyReport BuildFoodEcologyReport()
        {
            var report = new FoodEcologyReport
            {
                ProfileId = _foodEcologyProfile.Id,
                CropBindingCount = _productionContent.Bindings.Count,
                RotationAdjustedWorkOrders = _rotationAdjustedWorkOrders,
                ProcessedQuantity = _processedFoodQuantity,
                ConsumedNutritionMilliRations = _consumedNutrition,
                MinimumMarketPriceBasisPoints =
                    _minimumEcologyMarketPriceBasisPoints == int.MaxValue
                        ? 10_000 : _minimumEcologyMarketPriceBasisPoints,
                MaximumMarketPriceBasisPoints = _maximumEcologyMarketPriceBasisPoints,
                Products = new List<FoodEcologyProductReport>()
            };
            for (var i = 0; i < _foodProductProvenanceProfile.Products.Count; i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                FoodEcologyProductDefinition definition = _foodEcologyProducts[id];
                long closing = _households.Sum(item => item.FoodProductQuantities[i]) +
                    _countySubsistence.Sum(item => item.GovernmentFoodProductQuantities[i]);
                report.Products.Add(new FoodEcologyProductReport
                {
                    ProductDefinitionId = id,
                    NutritionBasisPoints = definition.NutritionBasisPoints,
                    VolumeBasisPoints = definition.VolumeBasisPoints,
                    SpoilageBasisPoints = definition.SpoilageBasisPoints,
                    MarketValueBasisPoints = definition.MarketValueBasisPoints,
                    ProcessingInputQuantity = _foodProcessingInput[i],
                    ProcessingOutputQuantity = _foodProcessingOutput[i],
                    ClosingQuantity = closing,
                    ClosingNutritionMilliRations = checked(
                        closing * definition.NutritionBasisPoints / 10_000L),
                    ClosingVolumeMilliUnits = checked(
                        closing * definition.VolumeBasisPoints / 10_000L)
                });
            }
            return report;
        }

        private void WriteFoodEcology(string path)
        {
            if (_foodEcologyProfile != null)
                JsonFile.Write(path, BuildFoodEcologyReport());
        }
    }
}
