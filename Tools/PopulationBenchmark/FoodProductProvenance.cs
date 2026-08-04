using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal sealed class FoodProductShare
    {
        [JsonProperty("product_definition_id")] public string ProductDefinitionId { get; set; }
        [JsonProperty("opening_share_basis_points")] public int OpeningShareBasisPoints { get; set; }
    }

    internal sealed class FoodProductProvenanceProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("products")] public List<FoodProductShare> Products { get; set; }

        public static FoodProductProvenanceProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<FoodProductProvenanceProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.food-product-provenance-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                value.Products == null || value.Products.Count < 2)
            {
                throw new InvalidDataException(
                    "The food product provenance profile is invalid.");
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var shares = 0;
            for (var i = 0; i < value.Products.Count; i++)
            {
                var product = value.Products[i];
                if (product == null ||
                    string.IsNullOrWhiteSpace(product.ProductDefinitionId) ||
                    product.OpeningShareBasisPoints < 0 ||
                    product.OpeningShareBasisPoints > 10_000 ||
                    !ids.Add(product.ProductDefinitionId))
                {
                    throw new InvalidDataException(
                        "A food product provenance entry is invalid.");
                }
                shares += product.OpeningShareBasisPoints;
            }
            if (shares != 10_000)
                throw new InvalidDataException("Opening food product shares must total 10000.");
            return value;
        }
    }

    internal sealed partial class WorldOptions
    {
        public string FoodProductProvenanceProfilePath { get; private set; }
        public bool HasFoodProductProvenance
        {
            get { return !string.IsNullOrWhiteSpace(FoodProductProvenanceProfilePath); }
        }
    }

    internal sealed partial class HouseholdRecord
    {
        public long[] FoodProductQuantities;
    }

    internal sealed partial class CountySubsistenceState
    {
        public long[] GovernmentFoodProductQuantities;
    }

    internal sealed class FoodProductProvenanceRecord
    {
        [JsonProperty("product_definition_id")] public string ProductDefinitionId { get; set; }
        [JsonProperty("product_stable_identity")] public long ProductStableIdentity { get; set; }
        [JsonProperty("opening_quantity")] public long OpeningQuantity { get; set; }
        [JsonProperty("harvest_quantity")] public long HarvestQuantity { get; set; }
        [JsonProperty("consumed_quantity")] public long ConsumedQuantity { get; set; }
        [JsonProperty("spoilage_quantity")] public long SpoilageQuantity { get; set; }
        [JsonProperty("conflict_seizure_quantity")] public long ConflictSeizureQuantity { get; set; }
        [JsonProperty("transport_loss_quantity")] public long TransportLossQuantity { get; set; }
        [JsonProperty("transport_provisions_quantity")] public long TransportProvisionsQuantity { get; set; }
        [JsonProperty("closing_household_quantity")] public long ClosingHouseholdQuantity { get; set; }
        [JsonProperty("closing_government_quantity")] public long ClosingGovernmentQuantity { get; set; }
        [JsonProperty("market_internal_transfer_quantity")] public long MarketInternalTransferQuantity { get; set; }
        [JsonProperty("tax_and_rent_internal_transfer_quantity")] public long TaxAndRentInternalTransferQuantity { get; set; }
        [JsonProperty("relief_internal_transfer_quantity")] public long ReliefInternalTransferQuantity { get; set; }
        [JsonProperty("transport_internal_transfer_quantity")] public long TransportInternalTransferQuantity { get; set; }
        [JsonProperty("household_lifecycle_internal_transfer_quantity")] public long HouseholdLifecycleInternalTransferQuantity { get; set; }
        [JsonProperty("processing_input_quantity")] public long ProcessingInputQuantity { get; set; }
        [JsonProperty("processing_output_quantity")] public long ProcessingOutputQuantity { get; set; }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("food_product_provenance_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string FoodProductProvenanceProfileId { get; set; }
        [JsonProperty("food_product_count", NullValueHandling = NullValueHandling.Ignore)] public int? FoodProductCount { get; set; }
        [JsonProperty("food_product_provenance", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence FoodProductProvenance { get; set; }
        [JsonProperty("food_product_provenance_digest", NullValueHandling = NullValueHandling.Ignore)] public string FoodProductProvenanceDigest { get; set; }
        [JsonProperty("food_product_conservation_total", NullValueHandling = NullValueHandling.Ignore)] public long? FoodProductConservationTotal { get; set; }
    }

    internal enum FoodSinkKind : byte
    {
        None,
        Consumption,
        Spoilage,
        Conflict,
        TransportLoss,
        TransportProvisions
    }

    internal sealed partial class DemographicWorldRunner
    {
        private FoodProductProvenanceProfile _foodProductProvenanceProfile;
        private Dictionary<string, int> _foodProductIndexById;
        private long[] _foodOpening;
        private long[] _foodHarvest;
        private long[] _foodConsumed;
        private long[] _foodSpoilage;
        private long[] _foodConflict;
        private long[] _foodTransportLoss;
        private long[] _foodTransportProvisions;
        private long[] _foodMarketTransfers;
        private long[] _foodTaxRentTransfers;
        private long[] _foodReliefTransfers;
        private long[] _foodTransportTransfers;
        private long[] _foodLifecycleTransfers;

        public void ConfigureFoodProductProvenance(
            FoodProductProvenanceProfile profile)
        {
            _foodProductProvenanceProfile = profile ??
                throw new ArgumentNullException(nameof(profile));
        }

        private void InitializeFoodProductProvenance()
        {
            if (_foodProductProvenanceProfile == null) return;
            _foodProductIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
            var available = new HashSet<string>(
                _productionContent.FoodProducts.Select(item => item.ProductId),
                StringComparer.Ordinal);
            for (var i = 0; i < _foodProductProvenanceProfile.Products.Count; i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                if (!available.Contains(id))
                    throw new InvalidDataException(
                        "Food provenance references a non-food product: " + id);
                _foodProductIndexById.Add(id, i);
            }

            int count = _foodProductIndexById.Count;
            _foodOpening = new long[count];
            _foodHarvest = new long[count];
            _foodConsumed = new long[count];
            _foodSpoilage = new long[count];
            _foodConflict = new long[count];
            _foodTransportLoss = new long[count];
            _foodTransportProvisions = new long[count];
            _foodMarketTransfers = new long[count];
            _foodTaxRentTransfers = new long[count];
            _foodReliefTransfers = new long[count];
            _foodTransportTransfers = new long[count];
            _foodLifecycleTransfers = new long[count];

            for (var i = 0; i < _households.Count; i++)
            {
                var household = _households[i];
                household.FoodProductQuantities = SplitOpeningFood(
                    household.FoodInventoryMilliRations);
                AddVector(_foodOpening, household.FoodProductQuantities);
            }
            for (var i = 0; i < _countySubsistence.Length; i++)
            {
                var county = _countySubsistence[i];
                county.GovernmentFoodProductQuantities = SplitOpeningFood(
                    county.GovernmentGranaryFoodMilliRations);
                AddVector(_foodOpening, county.GovernmentFoodProductQuantities);
            }
            ValidateFoodProductProvenance();
        }

        private long[] SplitOpeningFood(long total)
        {
            var weights = _foodProductProvenanceProfile.Products
                .Select(item => (long)item.OpeningShareBasisPoints)
                .ToArray();
            return AllocateLocal(total, weights, 10_000);
        }

        private void TrackHouseholdFoodAdded(
            HouseholdRecord household,
            string productId,
            long quantity,
            bool harvest)
        {
            if (_foodProductIndexById == null || quantity == 0) return;
            EnsureFoodVector(household);
            int index = FoodProductIndex(productId);
            household.FoodProductQuantities[index] = checked(
                household.FoodProductQuantities[index] + quantity);
            if (harvest)
                _foodHarvest[index] = checked(_foodHarvest[index] + quantity);
        }

        private void TrackGovernmentFoodAdded(
            CountySubsistenceState county,
            string productId,
            long quantity,
            bool taxOrRent)
        {
            if (_foodProductIndexById == null || quantity == 0) return;
            EnsureFoodVector(county);
            int index = FoodProductIndex(productId);
            county.GovernmentFoodProductQuantities[index] = checked(
                county.GovernmentFoodProductQuantities[index] + quantity);
            if (taxOrRent)
            {
                _foodHarvest[index] = checked(_foodHarvest[index] + quantity);
                _foodTaxRentTransfers[index] = checked(
                    _foodTaxRentTransfers[index] + quantity);
            }
        }

        private long[] TrackHouseholdFoodRemoved(
            HouseholdRecord household,
            long quantity,
            FoodSinkKind sink)
        {
            if (_foodProductIndexById == null) return null;
            EnsureFoodVector(household);
            var removed = RemoveFoodEcologyVector(
                household.FoodProductQuantities, quantity, sink);
            AddSink(removed, sink);
            return removed;
        }

        private long[] TrackGovernmentFoodRemoved(
            CountySubsistenceState county,
            long quantity,
            FoodSinkKind sink)
        {
            if (_foodProductIndexById == null) return null;
            EnsureFoodVector(county);
            var removed = RemoveFoodEcologyVector(
                county.GovernmentFoodProductQuantities, quantity, sink);
            AddSink(removed, sink);
            return removed;
        }

        private void TrackExtinctHouseholdFoodTransfer(
            HouseholdRecord household,
            CountySubsistenceState county)
        {
            if (_foodProductIndexById == null) return;
            EnsureFoodVector(household);
            EnsureFoodVector(county);
            AddVector(county.GovernmentFoodProductQuantities,
                household.FoodProductQuantities);
            AddVector(_foodLifecycleTransfers, household.FoodProductQuantities);
            Array.Clear(household.FoodProductQuantities, 0,
                household.FoodProductQuantities.Length);
        }

        private void TrackMarriageFoodTransfer(
            HouseholdRecord source,
            HouseholdRecord destination,
            long quantity)
        {
            if (_foodProductIndexById == null) return;
            EnsureFoodVector(destination);
            var moved = TrackHouseholdFoodRemoved(
                source, quantity, FoodSinkKind.None);
            AddVector(destination.FoodProductQuantities, moved);
            AddVector(_foodLifecycleTransfers, moved);
        }

        private long TrackMarketSale(HouseholdRecord seller, long quantity)
        {
            if (_foodProductIndexById == null) return quantity;
            var moved = TrackHouseholdFoodRemoved(
                seller, quantity, FoodSinkKind.Consumption);
            AddVector(_foodMarketTransfers, moved);
            long nutrition = FoodNutrition(moved);
            _consumedNutrition = checked(_consumedNutrition + nutrition);
            return nutrition;
        }

        private long TrackReliefConsumption(
            CountySubsistenceState county,
            long quantity)
        {
            if (_foodProductIndexById == null) return quantity;
            var moved = TrackGovernmentFoodRemoved(
                county, quantity, FoodSinkKind.Consumption);
            AddVector(_foodReliefTransfers, moved);
            long nutrition = FoodNutrition(moved);
            _consumedNutrition = checked(_consumedNutrition + nutrition);
            return nutrition;
        }

        private TransportProductMovement TrackTransportShipment(
            CountySubsistenceState donor,
            CountySubsistenceState recipient,
            long shipped,
            long loss,
            long provisions)
        {
            if (_foodProductIndexById == null) return null;
            var moved = TrackGovernmentFoodRemoved(
                donor, shipped, FoodSinkKind.None);
            var lossByProduct = AllocateLocal(loss, moved, shipped);
            var afterLoss = new long[moved.Length];
            for (var i = 0; i < moved.Length; i++)
                afterLoss[i] = moved[i] - lossByProduct[i];
            var provisionsByProduct = AllocateLocal(
                provisions, afterLoss, shipped - loss);
            var deliveredByProduct = new long[moved.Length];
            for (var i = 0; i < moved.Length; i++)
            {
                deliveredByProduct[i] = checked(
                    afterLoss[i] - provisionsByProduct[i]);
            }
            EnsureFoodVector(recipient);
            AddVector(recipient.GovernmentFoodProductQuantities,
                deliveredByProduct);
            AddVector(_foodTransportLoss, lossByProduct);
            AddVector(_foodTransportProvisions, provisionsByProduct);
            AddVector(_foodTransportTransfers, deliveredByProduct);
            return new TransportProductMovement
            {
                Shipped = moved,
                Delivered = deliveredByProduct,
                Loss = lossByProduct,
                Provisions = provisionsByProduct
            };
        }

        private void ValidateFoodProductProvenance()
        {
            if (_foodProductProvenanceProfile == null) return;
            for (var i = 0; i < _households.Count; i++)
            {
                EnsureFoodVector(_households[i]);
                if (_households[i].FoodProductQuantities.Sum() !=
                    _households[i].FoodInventoryMilliRations)
                    throw new InvalidOperationException(
                        "A household food product vector does not match its compatibility balance.");
            }
            for (var i = 0; i < _countySubsistence.Length; i++)
            {
                EnsureFoodVector(_countySubsistence[i]);
                if (_countySubsistence[i].GovernmentFoodProductQuantities.Sum() !=
                    _countySubsistence[i].GovernmentGranaryFoodMilliRations)
                    throw new InvalidOperationException(
                        "A government food product vector does not match its compatibility balance.");
            }
            for (var i = 0; i < _foodOpening.Length; i++)
            {
                long closing = checked(
                    _households.Sum(item => item.FoodProductQuantities[i]) +
                    _countySubsistence.Sum(
                        item => item.GovernmentFoodProductQuantities[i]));
                long expected = checked(
                    _foodOpening[i] + _foodHarvest[i] +
                    FoodEcologyProcessingOutput(i) -
                    _foodConsumed[i] - _foodSpoilage[i] -
                    _foodConflict[i] - _foodTransportLoss[i] -
                    _foodTransportProvisions[i] -
                    FoodEcologyProcessingInput(i));
                if (closing != expected)
                    throw new InvalidOperationException(
                        "Food product provenance does not conserve for " +
                        _foodProductProvenanceProfile.Products[i]
                            .ProductDefinitionId + ".");
            }
        }

        private List<FoodProductProvenanceRecord>
            BuildFoodProductProvenanceRecords()
        {
            ValidateFoodProductProvenance();
            var output = new List<FoodProductProvenanceRecord>(
                _foodProductProvenanceProfile.Products.Count);
            for (var i = 0;
                 i < _foodProductProvenanceProfile.Products.Count;
                 i++)
            {
                string id = _foodProductProvenanceProfile.Products[i]
                    .ProductDefinitionId;
                output.Add(new FoodProductProvenanceRecord
                {
                    ProductDefinitionId = id,
                    ProductStableIdentity = _productionContent.FoodProducts
                        .Find(item => item.ProductId == id).StableIdentity,
                    OpeningQuantity = _foodOpening[i],
                    HarvestQuantity = _foodHarvest[i],
                    ConsumedQuantity = _foodConsumed[i],
                    SpoilageQuantity = _foodSpoilage[i],
                    ConflictSeizureQuantity = _foodConflict[i],
                    TransportLossQuantity = _foodTransportLoss[i],
                    TransportProvisionsQuantity = _foodTransportProvisions[i],
                    ClosingHouseholdQuantity = _households.Sum(
                        item => item.FoodProductQuantities[i]),
                    ClosingGovernmentQuantity = _countySubsistence.Sum(
                        item => item.GovernmentFoodProductQuantities[i]),
                    MarketInternalTransferQuantity = _foodMarketTransfers[i],
                    TaxAndRentInternalTransferQuantity = _foodTaxRentTransfers[i],
                    ReliefInternalTransferQuantity = _foodReliefTransfers[i],
                    TransportInternalTransferQuantity = _foodTransportTransfers[i],
                    HouseholdLifecycleInternalTransferQuantity =
                        _foodLifecycleTransfers[i],
                    ProcessingInputQuantity = FoodEcologyProcessingInput(i),
                    ProcessingOutputQuantity = FoodEcologyProcessingOutput(i)
                });
            }
            return output;
        }

        private void WriteFoodProductProvenance(string path)
        {
            if (_foodProductProvenanceProfile == null) return;
            JsonFile.Write(path, BuildFoodProductProvenanceRecords());
        }

        private void AddSink(long[] values, FoodSinkKind sink)
        {
            if (values == null || sink == FoodSinkKind.None) return;
            long[] destination;
            switch (sink)
            {
                case FoodSinkKind.Consumption: destination = _foodConsumed; break;
                case FoodSinkKind.Spoilage: destination = _foodSpoilage; break;
                case FoodSinkKind.Conflict: destination = _foodConflict; break;
                case FoodSinkKind.TransportLoss: destination = _foodTransportLoss; break;
                case FoodSinkKind.TransportProvisions:
                    destination = _foodTransportProvisions;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(sink));
            }
            AddVector(destination, values);
        }

        private static long[] RemoveVector(long[] source, long quantity)
        {
            if (quantity < 0 || source.Sum() < quantity)
                throw new InvalidOperationException(
                    "A product flow exceeded its source inventory.");
            var removed = new long[source.Length];
            long remaining = quantity;
            for (var i = 0; i < source.Length && remaining > 0; i++)
            {
                long take = Math.Min(source[i], remaining);
                source[i] -= take;
                removed[i] = take;
                remaining -= take;
            }
            if (remaining != 0)
                throw new InvalidOperationException("A product flow did not settle.");
            return removed;
        }

        private static void AddVector(long[] destination, long[] values)
        {
            for (var i = 0; i < destination.Length; i++)
                destination[i] = checked(destination[i] + values[i]);
        }

        private void EnsureFoodVector(HouseholdRecord household)
        {
            if (household.FoodProductQuantities == null)
                household.FoodProductQuantities =
                    new long[_foodProductIndexById.Count];
        }

        private void EnsureFoodVector(CountySubsistenceState county)
        {
            if (county.GovernmentFoodProductQuantities == null)
                county.GovernmentFoodProductQuantities =
                    new long[_foodProductIndexById.Count];
        }

        private int FoodProductIndex(string productId)
        {
            int index;
            if (!_foodProductIndexById.TryGetValue(productId, out index))
                throw new InvalidOperationException(
                    "A product flow references an untracked food product: " +
                    productId);
            return index;
        }

        private sealed class TransportProductMovement
        {
            public long[] Shipped;
            public long[] Delivered;
            public long[] Loss;
            public long[] Provisions;
        }
    }
}
