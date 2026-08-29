using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public enum LuoyangFormalInventoryProjectionKind : byte
    {
        CompactInventory,
        ExternalSupplier,
        HouseholdAggregate,
        FreightCargo
    }

    [Serializable]
    public sealed class LuoyangFormalInventoryBindingState
    {
        public string SourceId;
        public string ProductId;
        public string InventoryContainerId;
        public LuoyangFormalInventoryProjectionKind ProjectionKind;
    }

    [Serializable]
    public sealed class LuoyangFormalEconomyRuntimeState
    {
        public const int ContractVersion = 1;
        public const string AuthorityId =
            "authority.luoyang.formal_product_batch.v1";

        public int Version = ContractVersion;
        public string PhysicalAuthorityId = AuthorityId;
        public bool IsPhysicalAuthority;
        public long ActivatedDay;
        public long Revision;
        public long ProjectionRevision;
        public string AuthorityHash = string.Empty;
        public string ProjectionHash = string.Empty;
        public string HouseholdOrderHash = string.Empty;
        public string CalibrationProfileId = string.Empty;
        public List<InventoryContainerState> InventoryContainers =
            new List<InventoryContainerState>();
        public List<LuoyangFormalInventoryBindingState> InventoryBindings =
            new List<LuoyangFormalInventoryBindingState>();
        public List<ProductBatchState> ProductBatches =
            new List<ProductBatchState>();
        public List<InventoryTransactionState> InventoryTransactions =
            new List<InventoryTransactionState>();
        public List<long> HouseholdFoodClaimsMilliunits =
            new List<long>();
        public long OpeningFoodMilliunits;
        public long CumulativeHarvestedMilliunits;
        public long CumulativeExternalProductionMilliunits;
        public long CumulativeConsumedMilliunits;
        public long CumulativeTransportLossMilliunits;
        public long CumulativeStorageLossMilliunits;
        public long CumulativeProcessingLossMilliunits;
        public long CumulativeMarketTransferredMilliunits;
        public long CumulativeFreightDispatchedMilliunits;
        public long CumulativeFreightDeliveredMilliunits;
        public long CumulativeTaxTransferredMilliunits;
        public long CumulativeReliefTransferredMilliunits;
        public long CompactPhysicalMutationCount;
        public long ReverseProjectionWriteCount;
        public long ProjectionRebuildCount;
        public long ProjectionRebuildMilliseconds;
        public long PeakManagedMemoryBytes;
    }

    [Serializable]
    public sealed class LuoyangFormalEconomyAuditResult
    {
        public long FormalFoodQuantityMilliunits;
        public long ProjectedFoodQuantityMilliunits;
        public long ProjectionDifferenceMilliunits;
        public long HouseholdClaimQuantityMilliunits;
        public long HouseholdFormalQuantityMilliunits;
        public long HouseholdClaimDifferenceMilliunits;
        public int InvalidBatchCount;
        public int DuplicateBatchCount;
        public int DuplicateTransactionCount;
        public int MissingContainerCount;
        public int UnknownPhysicalDeltaCount;
        public int BatchCount;
        public int TransactionCount;
        public string AuthorityHash;
        public string ProjectionHash;
    }

    [Serializable]
    public sealed class LuoyangNormalSupplyCalibrationProfileState
    {
        public const string ProfileId =
            "calibration.luoyang.normal_supply.v1";

        public string Id = ProfileId;
        public int OpeningReserveDays = 210;
        public int HouseholdOpeningShareBasisPoints = 9_850;
        public int MarketOpeningShareBasisPoints = 100;
        public int PublicOpeningShareBasisPoints = 50;
        public int AgricultureYieldUnitScale = 200;
        public int ExternalProductionScale = 24;
        public int FoodStorageCapacityUnitScale = 200;
        public int SupplierStorageCapacityScale = 24;
        public int AgricultureInitialStageWindowDays = 110;
        public int HarvestMarketReleaseBasisPoints = 8_500;
        public int HouseholdMarketBufferDays = 14;
        public int MarketTargetStockDays = 30;
        public string EvidenceBasis =
            "M24-P4 accepted 210-day opening reserve; Luoyang field records " +
            "currently encode 1/200 of the ration-unit yield implied by their " +
            "labour and 2km agriculture-unit coverage; external modeled supplier " +
            "throughput is scaled independently from physical local harvest.";

        public static LuoyangNormalSupplyCalibrationProfileState
            CreateAuthorityOnly()
        {
            return new LuoyangNormalSupplyCalibrationProfileState
            {
                Id = "calibration.luoyang.authority_only.v1",
                OpeningReserveDays = 0,
                AgricultureYieldUnitScale = 1,
                ExternalProductionScale = 1,
                FoodStorageCapacityUnitScale = 1,
                SupplierStorageCapacityScale = 1,
                AgricultureInitialStageWindowDays = 0,
                HarvestMarketReleaseBasisPoints = 0,
                HouseholdMarketBufferDays = 0,
                MarketTargetStockDays = 0,
                EvidenceBasis =
                    "Control candidate: formal authority enabled without " +
                    "normal-supply balance changes."
            };
        }
    }

    public static class LuoyangFormalEconomyContract
    {
        public const string HouseholdContainerId =
            "inventory.formal.luoyang.households.aggregate";

        private static readonly HashSet<string> Foods =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "product.food.millet_grain",
                "product.food.wheat_grain",
                "product.food.broomcorn_grain",
                "product.food.bean",
                "product.food.dry_ration",
                "product.reference.food_equivalent",
                CoreProductionContent.WheatGrainProductId,
                CoreProductionContent.WheatFlourProductId,
                CoreProductionContent.DryRationProductId,
                CoreProductionContent.FreshMuttonProductId,
                CoreProductionContent.OffalProductId
            };

        public static bool IsFood(string productId) =>
            !string.IsNullOrWhiteSpace(productId) && Foods.Contains(productId);

        public static string FreightContainerId(string shipmentId) =>
            "inventory.formal.luoyang.freight." + shipmentId;

        public static string ContainerId(string sourceId) =>
            sourceId != null && sourceId.StartsWith("inventory.formal.",
                StringComparison.Ordinal)
                ? sourceId
                : "inventory.formal.luoyang." + sourceId;
    }

    public static class LuoyangFormalEconomyDomain
    {
        public static void ActivateFromCompact(
            Luoyang184LivingWorldRuntimeState runtime,
            string migrationSummary)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.FormalEconomy != null &&
                runtime.FormalEconomy.IsPhysicalAuthority)
                return;

            var formal = new LuoyangFormalEconomyRuntimeState
            {
                IsPhysicalAuthority = true,
                ActivatedDay = runtime.AbsoluteDay,
                HouseholdOrderHash = ComputeHouseholdOrderHash(runtime)
            };
            runtime.FormalEconomy = formal;
            var opening = new InventoryTransactionState
            {
                Id = "transaction.luoyang.formal.activation." +
                     runtime.AbsoluteDay,
                Day = runtime.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                Summary = string.IsNullOrWhiteSpace(migrationSummary)
                    ? "Formalized Luoyang compact closing balances."
                    : migrationSummary
            };

            foreach (var inventory in runtime.Inventories
                         .Where(item => item != null &&
                             LuoyangFormalEconomyContract.IsFood(
                                 item.ProductId))
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                AddBinding(formal, inventory.Id, inventory.ProductId,
                    LuoyangFormalInventoryProjectionKind.CompactInventory,
                    inventory.OwnerId, inventory.FacilityId,
                    inventory.CurrentLocationId, inventory.CapacityMilliunits);
                AddOpeningBatch(formal, opening, inventory.Id,
                    inventory.ProductId, inventory.QuantityMilliunits,
                    runtime.AbsoluteDay, inventory.OwnerId,
                    inventory.FacilityId);
            }

            foreach (var supplier in runtime.ExternalSuppliers
                         .Where(item => item != null &&
                             LuoyangFormalEconomyContract.IsFood(
                                 item.ProductId))
                         .OrderBy(item => item.SupplierId,
                             StringComparer.Ordinal))
            {
                AddBinding(formal, supplier.InventoryId, supplier.ProductId,
                    LuoyangFormalInventoryProjectionKind.ExternalSupplier,
                    supplier.OrganizationId, supplier.FacilityId,
                    supplier.SettlementId,
                    supplier.StorageCapacityMilliunits);
                AddOpeningBatch(formal, opening, supplier.InventoryId,
                    supplier.ProductId,
                    supplier.InventoryQuantityMilliunits,
                    runtime.AbsoluteDay, supplier.OrganizationId,
                    supplier.FacilityId);
            }

            var claimTotal = 0L;
            formal.HouseholdFoodClaimsMilliunits.Capacity =
                runtime.Households.Count;
            foreach (var household in runtime.Households)
            {
                var claim = Math.Max(0, household.FoodReserveMilliunits);
                formal.HouseholdFoodClaimsMilliunits.Add(claim);
                claimTotal = checked(claimTotal + claim);
            }
            AddBinding(formal,
                LuoyangFormalEconomyContract.HouseholdContainerId,
                "product.reference.food_equivalent",
                LuoyangFormalInventoryProjectionKind.HouseholdAggregate,
                "organization.households.luoyang.aggregate", string.Empty,
                "location.capital.luoyang", long.MaxValue);
            AddOpeningBatch(formal, opening,
                LuoyangFormalEconomyContract.HouseholdContainerId,
                "product.reference.food_equivalent", claimTotal,
                runtime.AbsoluteDay,
                "organization.households.luoyang.aggregate", string.Empty);

            foreach (var shipment in runtime.Shipments
                         .Where(item => item != null && !item.Delivered &&
                             LuoyangFormalEconomyContract.IsFood(
                                 item.ProductId))
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var containerId =
                    LuoyangFormalEconomyContract.FreightContainerId(
                        shipment.Id);
                AddBinding(formal, containerId, shipment.ProductId,
                    LuoyangFormalInventoryProjectionKind.FreightCargo,
                    string.Empty, string.Empty,
                    "transit:" + shipment.RouteId,
                    shipment.DeliveredQuantityMilliunits);
                AddOpeningBatch(formal, opening, containerId,
                    shipment.ProductId,
                    shipment.DeliveredQuantityMilliunits,
                    runtime.AbsoluteDay, string.Empty, string.Empty);
            }

            if (opening.Lines.Count > 0)
                formal.InventoryTransactions.Add(opening);
            formal.OpeningFoodMilliunits = formal.ProductBatches
                .Where(item => LuoyangFormalEconomyContract.IsFood(
                    item.ProductDefinitionId)).Sum(item => item.Quantity);
            formal.Revision = 1;
            formal.PeakManagedMemoryBytes = GC.GetTotalMemory(false);
            RebuildProjection(runtime);
        }

        public static void RebuildProjection(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime?.FormalEconomy == null ||
                !runtime.FormalEconomy.IsPhysicalAuthority)
                throw new InvalidOperationException(
                    "Luoyang formal economy authority is not active.");
            var formal = runtime.FormalEconomy;
            foreach (var binding in formal.InventoryBindings)
            {
                // A compact row may intentionally project an aggregate formal
                // container (for example a public granary that holds several
                // grain products).  Project the whole container, never whichever
                // product binding happened to be visited last.
                var quantity = binding.ProjectionKind ==
                               LuoyangFormalInventoryProjectionKind
                                   .CompactInventory ||
                               binding.ProjectionKind ==
                               LuoyangFormalInventoryProjectionKind
                                   .ExternalSupplier
                    ? Quantity(formal, binding.InventoryContainerId)
                    : Quantity(formal, binding.InventoryContainerId,
                        binding.ProductId);
                switch (binding.ProjectionKind)
                {
                    case LuoyangFormalInventoryProjectionKind.CompactInventory:
                    {
                        var inventory = runtime.Inventories.Find(item =>
                            item.Id == binding.SourceId);
                        if (inventory != null)
                            inventory.QuantityMilliunits = quantity;
                        break;
                    }
                    case LuoyangFormalInventoryProjectionKind.ExternalSupplier:
                    {
                        var supplier = runtime.ExternalSuppliers.Find(item =>
                            item.InventoryId == binding.SourceId);
                        if (supplier != null)
                            supplier.InventoryQuantityMilliunits = quantity;
                        break;
                    }
                }
            }
            if (formal.HouseholdFoodClaimsMilliunits.Count !=
                runtime.Households.Count)
                throw new InvalidOperationException(
                    "Formal household claim count does not match households.");
            for (var i = 0; i < runtime.Households.Count; i++)
                runtime.Households[i].FoodReserveMilliunits =
                    formal.HouseholdFoodClaimsMilliunits[i];
            formal.ProjectionRevision = formal.Revision;
            formal.ProjectionHash = ComputeProjectionHash(runtime);
            formal.AuthorityHash = ComputeAuthorityHash(runtime);
            formal.ProjectionRebuildCount++;
        }

        public static string ComputeAuthorityHash(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime?.FormalEconomy == null) return string.Empty;
            var formal = runtime.FormalEconomy;
            var builder = new StringBuilder(4096);
            builder.Append(formal.PhysicalAuthorityId).Append('|')
                .Append(formal.Revision).Append('|')
                .Append(formal.ActivatedDay).Append('\n');
            foreach (var container in formal.InventoryContainers
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
                builder.Append("C|").Append(container.Id).Append('|')
                    .Append(container.OwnerFamilyId).Append('|')
                    .Append(container.OwnerOrganizationId).Append('|')
                    .Append(container.LocationId).Append('|')
                    .Append(container.CapacityWeight).Append('\n');
            foreach (var batch in formal.ProductBatches
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
                builder.Append("B|").Append(batch.Id).Append('|')
                    .Append(batch.ProductDefinitionId).Append('|')
                    .Append(batch.InventoryContainerId).Append('|')
                    .Append(batch.SourceWorkOrderId).Append('|')
                    .Append(batch.ProducedDay).Append('|')
                    .Append(batch.Quantity).Append('|')
                    .Append(batch.ReservedQuantity).Append('|')
                    .Append(batch.QualityBasisPoints).Append('\n');
            foreach (var transaction in formal.InventoryTransactions
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                builder.Append("T|").Append(transaction.Id).Append('|')
                    .Append(transaction.Day).Append('|')
                    .Append((byte)transaction.Type).Append('|')
                    .Append(transaction.SourceWorkOrderId).Append('|')
                    .Append(transaction.SourceCivilianFreightId).Append('\n');
                foreach (var line in transaction.Lines)
                    builder.Append("L|").Append(line.BatchId).Append('|')
                        .Append(line.ProductDefinitionId).Append('|')
                        .Append(line.InventoryContainerId).Append('|')
                        .Append(line.QuantityDelta).Append('|')
                        .Append(line.ReservedQuantityDelta).Append('\n');
            }
            for (var i = 0;
                 i < formal.HouseholdFoodClaimsMilliunits.Count; i++)
                builder.Append("H|").Append(i).Append('|')
                    .Append(formal.HouseholdFoodClaimsMilliunits[i])
                    .Append('\n');
            return Sha256(builder.ToString());
        }

        public static string ComputeProjectionHash(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var builder = new StringBuilder(4096);
            foreach (var inventory in runtime.Inventories
                         .Where(item => LuoyangFormalEconomyContract.IsFood(
                             item.ProductId))
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
                builder.Append("I|").Append(inventory.Id).Append('|')
                    .Append(inventory.ProductId).Append('|')
                    .Append(inventory.QuantityMilliunits).Append('\n');
            foreach (var supplier in runtime.ExternalSuppliers
                         .Where(item => LuoyangFormalEconomyContract.IsFood(
                             item.ProductId))
                         .OrderBy(item => item.SupplierId,
                             StringComparer.Ordinal))
                builder.Append("S|").Append(supplier.InventoryId).Append('|')
                    .Append(supplier.ProductId).Append('|')
                    .Append(supplier.InventoryQuantityMilliunits).Append('\n');
            for (var i = 0; i < runtime.Households.Count; i++)
                builder.Append("H|").Append(i).Append('|')
                    .Append(runtime.Households[i].FoodReserveMilliunits)
                    .Append('\n');
            if (runtime.FormalEconomy != null)
                foreach (var binding in runtime.FormalEconomy.InventoryBindings
                             .Where(item => item.ProjectionKind ==
                                 LuoyangFormalInventoryProjectionKind
                                     .FreightCargo)
                             .OrderBy(item => item.SourceId,
                                 StringComparer.Ordinal))
                    builder.Append("F|").Append(binding.SourceId).Append('|')
                        .Append(binding.ProductId).Append('|')
                        .Append(Quantity(runtime.FormalEconomy,
                            binding.InventoryContainerId,
                            binding.ProductId)).Append('\n');
            return Sha256(builder.ToString());
        }

        public static long Quantity(
            LuoyangFormalEconomyRuntimeState formal,
            string containerId,
            string productId = null) =>
            formal.ProductBatches.Where(item =>
                    item.InventoryContainerId == containerId &&
                    (string.IsNullOrWhiteSpace(productId) ||
                     item.ProductDefinitionId == productId))
                .Sum(item => item.Quantity);

        public static string ComputeHouseholdOrderHash(
            Luoyang184LivingWorldRuntimeState runtime) =>
            Sha256(string.Join("\n", runtime.Households.Select(
                item => item.HouseholdOrdinal + ":" + item.HouseholdId)));

        private static void AddBinding(
            LuoyangFormalEconomyRuntimeState formal,
            string sourceId,
            string productId,
            LuoyangFormalInventoryProjectionKind kind,
            string ownerId,
            string facilityId,
            string locationId,
            long capacity)
        {
            var containerId = kind ==
                              LuoyangFormalInventoryProjectionKind
                                  .HouseholdAggregate ||
                              kind == LuoyangFormalInventoryProjectionKind
                                  .FreightCargo
                ? sourceId
                : LuoyangFormalEconomyContract.ContainerId(sourceId);
            if (!formal.InventoryContainers.Exists(item =>
                    item.Id == containerId))
                formal.InventoryContainers.Add(new InventoryContainerState
                {
                    Id = containerId,
                    KindId = "inventory.kind.luoyang.formal.aggregate",
                    OwnerOrganizationId = ownerId ?? string.Empty,
                    LocationId = string.IsNullOrWhiteSpace(locationId)
                        ? "location.capital.luoyang"
                        : locationId,
                    CapacityWeight = Math.Max(0, capacity)
                });
            if (!formal.InventoryBindings.Exists(item =>
                    item.SourceId == sourceId &&
                    item.ProductId == productId))
                formal.InventoryBindings.Add(
                    new LuoyangFormalInventoryBindingState
                    {
                        SourceId = sourceId,
                        ProductId = productId,
                        InventoryContainerId = containerId,
                        ProjectionKind = kind
                    });
        }

        private static void AddOpeningBatch(
            LuoyangFormalEconomyRuntimeState formal,
            InventoryTransactionState transaction,
            string sourceId,
            string productId,
            long quantity,
            long day,
            string ownerId,
            string facilityId)
        {
            if (quantity <= 0) return;
            var binding = formal.InventoryBindings.Find(item =>
                item.SourceId == sourceId && item.ProductId == productId) ??
                throw new InvalidOperationException(
                    "Missing formal inventory binding for " + sourceId + ".");
            var id = "batch.luoyang.formal.opening." +
                     formal.ProductBatches.Count.ToString("D6");
            var batch = new ProductBatchState
            {
                Id = id,
                ProductDefinitionId = productId,
                OwnerOrganizationId = ownerId ?? string.Empty,
                StorageFacilityId = facilityId ?? string.Empty,
                InventoryContainerId = binding.InventoryContainerId,
                OriginLocationId = "location.capital.luoyang",
                SourceTransactionId = transaction.Id,
                UnitId = "unit.milliunit",
                UnitWeight = 1,
                ProducedDay = day,
                Quantity = quantity
            };
            formal.ProductBatches.Add(batch);
            transaction.Lines.Add(Line(batch, quantity));
        }

        public static InventoryTransactionLineState Line(
            ProductBatchState batch, long delta) =>
            new InventoryTransactionLineState
            {
                BatchId = batch.Id,
                ProductDefinitionId = batch.ProductDefinitionId,
                OwnerFamilyId = batch.OwnerFamilyId,
                OwnerOrganizationId = batch.OwnerOrganizationId,
                StorageFacilityId = batch.StorageFacilityId,
                InventoryContainerId = batch.InventoryContainerId,
                UnitId = batch.UnitId,
                QuantityDelta = delta
            };

        private static string Sha256(string text)
        {
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
