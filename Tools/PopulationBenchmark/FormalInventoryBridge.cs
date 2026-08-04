using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mandate.Tools.PopulationFiftyYearWorld
{
    internal sealed class FormalInventoryBridgeProfile
    {
        [JsonProperty("schema_version")] public string SchemaVersion { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("source_layer")] public string SourceLayer { get; set; }
        [JsonProperty("formal_snapshot_contract")] public string FormalSnapshotContract { get; set; }
        [JsonProperty("require_single_agricultural_binding")] public bool RequireSingleAgriculturalBinding { get; set; }
        [JsonProperty("food_quality_basis_points")] public int FoodQualityBasisPoints { get; set; }
        [JsonProperty("seed_quality_basis_points")] public int SeedQualityBasisPoints { get; set; }
        [JsonProperty("freshness_basis_points")] public int FreshnessBasisPoints { get; set; }

        public static FormalInventoryBridgeProfile Load(string path)
        {
            var value = JsonConvert.DeserializeObject<FormalInventoryBridgeProfile>(
                File.ReadAllText(path, Encoding.UTF8));
            if (value == null ||
                value.SchemaVersion != "mandate.formal-inventory-bridge-profile.v1" ||
                value.SourceLayer != "gameplay_completion" ||
                value.FormalSnapshotContract != "world.snapshot.v10" ||
                string.IsNullOrWhiteSpace(value.Id) ||
                value.FoodQualityBasisPoints < 0 ||
                value.FoodQualityBasisPoints > 10_000 ||
                value.SeedQualityBasisPoints < 0 ||
                value.SeedQualityBasisPoints > 10_000 ||
                value.FreshnessBasisPoints < 0 ||
                value.FreshnessBasisPoints > 10_000)
            {
                throw new InvalidDataException(
                    "The formal inventory bridge profile is invalid.");
            }
            return value;
        }
    }

    internal sealed partial class WorldOptions
    {
        public string FormalInventoryBridgeProfilePath { get; private set; }
        public bool HasFormalInventoryBridge
        {
            get { return !string.IsNullOrWhiteSpace(FormalInventoryBridgeProfilePath); }
        }
    }

    internal sealed partial class WorldEvidence
    {
        [JsonProperty("formal_inventory_bridge_profile_id", NullValueHandling = NullValueHandling.Ignore)] public string FormalInventoryBridgeProfileId { get; set; }
        [JsonProperty("formal_inventory_contract", NullValueHandling = NullValueHandling.Ignore)] public string FormalInventoryContract { get; set; }
        [JsonProperty("formal_inventory_batch_count", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventoryBatchCount { get; set; }
        [JsonProperty("formal_inventory_transaction_count", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventoryTransactionCount { get; set; }
        [JsonProperty("formal_inventory_source_food", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventorySourceFood { get; set; }
        [JsonProperty("formal_inventory_source_seed", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventorySourceSeed { get; set; }
        [JsonProperty("formal_inventory_batch_quantity", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventoryBatchQuantity { get; set; }
        [JsonProperty("formal_inventory_source_balance_delta", NullValueHandling = NullValueHandling.Ignore)] public long? FormalInventorySourceBalanceDelta { get; set; }
        [JsonProperty("formal_inventory_batches", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence FormalInventoryBatches { get; set; }
        [JsonProperty("formal_inventory_transactions", NullValueHandling = NullValueHandling.Ignore)] public FileEvidence FormalInventoryTransactions { get; set; }
        [JsonProperty("formal_inventory_digest", NullValueHandling = NullValueHandling.Ignore)] public string FormalInventoryDigest { get; set; }
    }

    internal sealed partial class DemographicWorldRunner
    {
        private const int FormalBatchRecordBytes = 78;
        private const int FormalTransactionRecordBytes = 78;
        private FormalInventoryBridgeProfile _formalInventoryBridgeProfile;
        private FormalInventoryBridgeAudit _formalInventoryBridgeAudit;

        public void ConfigureFormalInventoryBridge(
            FormalInventoryBridgeProfile profile)
        {
            _formalInventoryBridgeProfile = profile ??
                throw new ArgumentNullException(nameof(profile));
        }

        private void WriteFormalInventoryBridge(
            string batchPath,
            string transactionPath)
        {
            if (_formalInventoryBridgeProfile == null) return;
            if (_productionContent == null ||
                _productionContent.Bindings.Count == 0 ||
                _formalInventoryBridgeProfile.RequireSingleAgriculturalBinding &&
                _productionContent.Bindings.Count != 1 &&
                _foodEcologyProfile == null)
            {
                throw new InvalidOperationException(
                    "The current compact commodity balance can only be bridged when its product binding is unambiguous.");
            }

            var audit = new FormalInventoryBridgeAudit();
            using (var batchStream = new FileStream(
                batchPath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 1 << 20))
            using (var batchWriter = new BinaryWriter(batchStream, Encoding.UTF8))
            using (var transactionStream = new FileStream(
                transactionPath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None, 1 << 20))
            using (var transactionWriter = new BinaryWriter(
                transactionStream, Encoding.UTF8))
            {
                batchWriter.Write(0x4D323442);
                batchWriter.Write(1);
                batchWriter.Write(0L);
                transactionWriter.Write(0x4D323454);
                transactionWriter.Write(1);
                transactionWriter.Write(0L);

                for (var i = 0; i < _households.Count; i++)
                {
                    var household = _households[i];
                    var binding = _productionContent.Bindings[
                        household.CropBindingIndex];
                    if (_foodProductProvenanceProfile == null)
                    {
                        WriteBridgeBalance(
                            batchWriter, transactionWriter, audit,
                            1, household.Id, household.CountyIndex,
                            binding.HarvestProductId, string.Empty,
                            household.FoodInventoryMilliRations,
                            _formalInventoryBridgeProfile.FoodQualityBasisPoints,
                            0, 0, 0);
                    }
                    else
                    {
                        for (var productIndex = 0;
                             productIndex < household.FoodProductQuantities.Length;
                             productIndex++)
                        {
                            WriteBridgeBalance(
                                batchWriter, transactionWriter, audit,
                                1, household.Id, household.CountyIndex,
                                _foodProductProvenanceProfile.Products[productIndex]
                                    .ProductDefinitionId,
                                string.Empty,
                                household.FoodProductQuantities[productIndex],
                                _formalInventoryBridgeProfile.FoodQualityBasisPoints,
                                0, 0, 10 + productIndex);
                        }
                    }
                    WriteBridgeBalance(
                        batchWriter, transactionWriter, audit,
                        1, household.Id, household.CountyIndex,
                        binding.SeedProductId, binding.VarietyId,
                        household.SeedInventoryMilliRations,
                        _formalInventoryBridgeProfile.SeedQualityBasisPoints,
                        household.SeedVigorBasisPoints,
                        household.SeedPurityBasisPoints, 1);
                }

                var publicBinding = _productionContent.Bindings[0];
                for (var countyIndex = 0;
                     countyIndex < _countySubsistence.Length;
                     countyIndex++)
                {
                    var county = _countySubsistence[countyIndex];
                    long ownerId = StableContentIdentity(
                        "organization.county." +
                        _input.Counties[countyIndex].Id + ".government");
                    if (_foodProductProvenanceProfile == null)
                    {
                        WriteBridgeBalance(
                            batchWriter, transactionWriter, audit,
                            2, ownerId, countyIndex,
                            publicBinding.HarvestProductId, string.Empty,
                            county.GovernmentGranaryFoodMilliRations,
                            _formalInventoryBridgeProfile.FoodQualityBasisPoints,
                            0, 0, 2);
                    }
                    else
                    {
                        for (var productIndex = 0;
                             productIndex < county.GovernmentFoodProductQuantities.Length;
                             productIndex++)
                        {
                            WriteBridgeBalance(
                                batchWriter, transactionWriter, audit,
                                2, ownerId, countyIndex,
                                _foodProductProvenanceProfile.Products[productIndex]
                                    .ProductDefinitionId,
                                string.Empty,
                                county.GovernmentFoodProductQuantities[productIndex],
                                _formalInventoryBridgeProfile.FoodQualityBasisPoints,
                                0, 0, 20 + productIndex);
                        }
                    }
                    if (_foodEcologyProfile == null)
                    {
                        WriteBridgeBalance(
                            batchWriter, transactionWriter, audit,
                            2, ownerId, countyIndex,
                            publicBinding.SeedProductId, publicBinding.VarietyId,
                            county.GovernmentSeedInventoryMilliRations,
                            _formalInventoryBridgeProfile.SeedQualityBasisPoints,
                            8_000, 9_000, 3);
                    }
                    else
                    {
                        if (county.GovernmentSeedByCropBinding.Sum() !=
                            county.GovernmentSeedInventoryMilliRations)
                            throw new InvalidOperationException(
                                "Government seed crop bindings do not reconcile.");
                        for (var bindingIndex = 0;
                             bindingIndex < _productionContent.Bindings.Count;
                             bindingIndex++)
                        {
                            var seedBinding = _productionContent.Bindings[bindingIndex];
                            WriteBridgeBalance(
                                batchWriter, transactionWriter, audit,
                                2, ownerId, countyIndex,
                                seedBinding.SeedProductId, seedBinding.VarietyId,
                                county.GovernmentSeedByCropBinding[bindingIndex],
                                _formalInventoryBridgeProfile.SeedQualityBasisPoints,
                                8_000, 9_000, 30 + bindingIndex);
                        }
                    }
                }

                batchWriter.Flush();
                batchStream.Position = 8;
                batchWriter.Write(audit.BatchCount);
                batchWriter.Flush();
                batchStream.Flush(true);
                transactionWriter.Flush();
                transactionStream.Position = 8;
                transactionWriter.Write(audit.TransactionCount);
                transactionWriter.Flush();
                transactionStream.Flush(true);
            }

            audit.SourceFood = checked(
                _households.Sum(item => item.FoodInventoryMilliRations) +
                _countySubsistence.Sum(
                    item => item.GovernmentGranaryFoodMilliRations));
            audit.SourceSeed = checked(
                _households.Sum(item => item.SeedInventoryMilliRations) +
                _countySubsistence.Sum(
                    item => item.GovernmentSeedInventoryMilliRations));
            ValidateFormalInventoryBridge(batchPath, transactionPath, audit);
            _formalInventoryBridgeAudit = audit;
        }

        private void WriteBridgeBalance(
            BinaryWriter batchWriter,
            BinaryWriter transactionWriter,
            FormalInventoryBridgeAudit audit,
            byte ownerKind,
            long ownerId,
            int countyIndex,
            string productId,
            string varietyId,
            long quantity,
            int quality,
            int seedVigor,
            int seedPurity,
            int purpose)
        {
            if (quantity < 0)
                throw new InvalidOperationException("A compact inventory balance is negative.");
            if (quantity == 0) return;
            long productStableId = StableContentIdentity(productId);
            long varietyStableId = string.IsNullOrEmpty(varietyId)
                ? 0
                : StableContentIdentity(varietyId);
            long batchId = BridgeIdentity(
                ownerKind, ownerId, productStableId, purpose, 1);
            long transactionId = BridgeIdentity(
                ownerKind, ownerId, productStableId, purpose, 2);
            long containerId = BridgeIdentity(
                ownerKind, ownerId, productStableId, purpose, 3);
            if (!audit.BatchIds.Add(batchId) ||
                !audit.TransactionIds.Add(transactionId))
            {
                throw new InvalidOperationException(
                    "A formal inventory bridge stable identity collided.");
            }

            batchWriter.Write(batchId);
            batchWriter.Write(ownerKind);
            batchWriter.Write(ownerId);
            batchWriter.Write(countyIndex);
            batchWriter.Write(productStableId);
            batchWriter.Write(varietyStableId);
            batchWriter.Write(containerId);
            batchWriter.Write(transactionId);
            batchWriter.Write(quantity);
            batchWriter.Write(quality);
            batchWriter.Write(_formalInventoryBridgeProfile.FreshnessBasisPoints);
            batchWriter.Write(seedVigor);
            batchWriter.Write(seedPurity);
            batchWriter.Write((byte)(ownerKind == 1 ? 0 : 4));

            long legacyGrainDelta = ownerKind == 1 &&
                string.IsNullOrEmpty(varietyId)
                ? -quantity
                : 0;
            long legacySeedDelta = ownerKind == 1 &&
                !string.IsNullOrEmpty(varietyId)
                ? -quantity
                : 0;
            transactionWriter.Write(transactionId);
            transactionWriter.Write(batchId);
            transactionWriter.Write((byte)(ownerKind == 1 ? 0 : 4));
            transactionWriter.Write(ownerKind);
            transactionWriter.Write(ownerId);
            transactionWriter.Write(countyIndex);
            transactionWriter.Write(productStableId);
            transactionWriter.Write(quantity);
            transactionWriter.Write(legacyGrainDelta);
            transactionWriter.Write(legacySeedDelta);
            transactionWriter.Write(-quantity);
            transactionWriter.Write(quantity);

            audit.BatchCount++;
            audit.TransactionCount++;
            audit.BatchQuantity = checked(audit.BatchQuantity + quantity);
            audit.SourceBalanceDelta = checked(
                audit.SourceBalanceDelta - quantity);
        }

        private static void ValidateFormalInventoryBridge(
            string batchPath,
            string transactionPath,
            FormalInventoryBridgeAudit expected)
        {
            long batchQuantity = 0;
            using (var stream = File.OpenRead(batchPath))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (reader.ReadInt32() != 0x4D323442 ||
                    reader.ReadInt32() != 1 ||
                    reader.ReadInt64() != expected.BatchCount ||
                    stream.Length != 16L +
                        expected.BatchCount * FormalBatchRecordBytes)
                    throw new InvalidDataException("Formal batch file header or length is invalid.");
                for (long i = 0; i < expected.BatchCount; i++)
                {
                    reader.ReadInt64();
                    reader.ReadByte();
                    reader.ReadInt64();
                    reader.ReadInt32();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    batchQuantity = checked(batchQuantity + reader.ReadInt64());
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadByte();
                }
            }

            long sourceDelta = 0;
            long lineQuantity = 0;
            using (var stream = File.OpenRead(transactionPath))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (reader.ReadInt32() != 0x4D323454 ||
                    reader.ReadInt32() != 1 ||
                    reader.ReadInt64() != expected.TransactionCount ||
                    stream.Length != 16L +
                        expected.TransactionCount * FormalTransactionRecordBytes)
                    throw new InvalidDataException("Formal transaction file header or length is invalid.");
                for (long i = 0; i < expected.TransactionCount; i++)
                {
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadInt64();
                    reader.ReadInt32();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    sourceDelta = checked(sourceDelta + reader.ReadInt64());
                    lineQuantity = checked(lineQuantity + reader.ReadInt64());
                }
            }

            if (batchQuantity != expected.BatchQuantity ||
                lineQuantity != expected.BatchQuantity ||
                sourceDelta != expected.SourceBalanceDelta ||
                expected.BatchQuantity != checked(
                    expected.SourceFood + expected.SourceSeed) ||
                expected.SourceBalanceDelta != -expected.BatchQuantity)
            {
                throw new InvalidOperationException(
                    "The formal inventory bridge does not reconcile source balances, batches, and transaction lines.");
            }
        }

        private static long StableContentIdentity(string value)
        {
            ulong hash = 14695981039346656037UL;
            foreach (byte item in Encoding.UTF8.GetBytes(value))
            {
                hash ^= item;
                hash *= 1099511628211UL;
            }
            long result = unchecked((long)(hash & 0x7FFFFFFFFFFFFFFFUL));
            return result == 0 ? 1 : result;
        }

        private static long BridgeIdentity(
            byte ownerKind,
            long ownerId,
            long productId,
            int purpose,
            int recordKind)
        {
            ulong value = unchecked((ulong)ownerId) * 0x9E3779B97F4A7C15UL;
            value ^= unchecked((ulong)productId) * 0xBF58476D1CE4E5B9UL;
            value ^= unchecked((ulong)ownerKind) * 0x94D049BB133111EBUL;
            value ^= unchecked((ulong)purpose) * 0xD6E8FEB86659FD93UL;
            value ^= unchecked((ulong)recordKind) * 0xA0761D6478BD642FUL;
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            long result = unchecked((long)(value & 0x7FFFFFFFFFFFFFFFUL));
            return result == 0 ? 1 : result;
        }

        private sealed class FormalInventoryBridgeAudit
        {
            public long BatchCount;
            public long TransactionCount;
            public long SourceFood;
            public long SourceSeed;
            public long BatchQuantity;
            public long SourceBalanceDelta;
            public readonly HashSet<long> BatchIds = new HashSet<long>();
            public readonly HashSet<long> TransactionIds = new HashSet<long>();
        }
    }
}
