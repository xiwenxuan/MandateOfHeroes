using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Sparse formal ProductBatch/InventoryTransaction authority for the
    /// partition-friendly Luoyang runtime. Compact balances are projections.
    /// </summary>
    public sealed class LuoyangFormalEconomySystem
    {
        private const string HouseholdContainerId =
            LuoyangFormalEconomyContract.HouseholdContainerId;

        public static bool IsFood(string productId) =>
            LuoyangFormalEconomyContract.IsFood(productId);

        public static string FreightContainerId(string shipmentId) =>
            LuoyangFormalEconomyContract.FreightContainerId(shipmentId);

        public static string ComputeAuthorityHash(
            Luoyang184LivingWorldRuntimeState runtime) =>
            LuoyangFormalEconomyDomain.ComputeAuthorityHash(runtime);

        public void ActivateFromBootstrap(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            LuoyangFormalEconomyDomain.ActivateFromCompact(runtime,
                "Luoyang scenario bootstrap formalization; compact balances " +
                "become derived projections after this transaction.");
        }

        public void ApplyCapacityCalibrationBeforeActivation(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangNormalSupplyCalibrationProfileState profile)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (runtime.FormalEconomy != null &&
                runtime.FormalEconomy.IsPhysicalAuthority)
                throw new InvalidOperationException(
                    "Storage calibration must precede formal activation.");
            foreach (var inventory in runtime.Inventories.Where(item =>
                         IsFood(item.ProductId)))
                inventory.CapacityMilliunits = checked(
                    inventory.CapacityMilliunits *
                    profile.FoodStorageCapacityUnitScale);
            foreach (var supplier in runtime.ExternalSuppliers.Where(item =>
                         IsFood(item.ProductId)))
                supplier.StorageCapacityMilliunits = checked(
                    supplier.StorageCapacityMilliunits *
                    profile.SupplierStorageCapacityScale);
        }

        public void ApplyNormalSupplyOpeningReserve(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangNormalSupplyCalibrationProfileState profile)
        {
            Ensure(runtime);
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.HouseholdOpeningShareBasisPoints +
                profile.MarketOpeningShareBasisPoints +
                profile.PublicOpeningShareBasisPoints != 10_000)
                throw new InvalidOperationException(
                    "Normal-supply opening shares must total 10000.");
            var formal = runtime.FormalEconomy;
            formal.CalibrationProfileId = profile.Id;
            var target = checked(runtime.DailyFoodDemandMilliunits *
                                 profile.OpeningReserveDays);
            var current = formal.ProductBatches.Where(item =>
                IsFood(item.ProductDefinitionId)).Sum(item => item.Quantity);
            var missing = Math.Max(0, target - current);
            if (missing == 0)
            {
                RebuildProjection(runtime);
                return;
            }

            var household = missing *
                profile.HouseholdOpeningShareBasisPoints / 10_000;
            var market = missing *
                profile.MarketOpeningShareBasisPoints / 10_000;
            var publicFood = missing - household - market;
            var marketAdded = AddOpeningToInventories(runtime,
                runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    IsFood(item.ProductId)), market,
                "market_opening_reserve");
            var publicAdded = AddOpeningToInventories(runtime,
                runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Government &&
                    IsFood(item.ProductId)), publicFood,
                "public_opening_reserve");
            household = checked(household + (market - marketAdded) +
                                (publicFood - publicAdded));
            AddHouseholdOpeningReserve(runtime, household,
                "household_opening_reserve");
            formal.OpeningFoodMilliunits = formal.ProductBatches.Where(item =>
                IsFood(item.ProductDefinitionId)).Sum(item => item.Quantity);
            RebuildProjection(runtime);
        }

        public long Produce(
            Luoyang184LivingWorldRuntimeState runtime,
            string destinationSourceId,
            string productId,
            long quantity,
            InventoryTransactionType type,
            string operationId,
            string sourceWorkOrderId = "")
        {
            Ensure(runtime);
            if (quantity <= 0) return 0;
            var binding = EnsureBinding(runtime, destinationSourceId,
                productId);
            var capacity = AvailableCapacity(runtime.FormalEconomy,
                binding.InventoryContainerId);
            var stored = Math.Min(quantity, capacity);
            if (stored <= 0) return 0;
            var transaction = NewTransaction(runtime, type, operationId);
            transaction.SourceWorkOrderId = sourceWorkOrderId ?? string.Empty;
            var batch = AddOrMergeBatch(runtime, binding.InventoryContainerId,
                productId, stored, transaction.Id, sourceWorkOrderId,
                runtime.AbsoluteDay, 10_000, 10_000);
            transaction.Lines.Add(
                LuoyangFormalEconomyDomain.Line(batch, stored));
            runtime.FormalEconomy.InventoryTransactions.Add(transaction);
            if (type == InventoryTransactionType.FoodHarvested)
                runtime.FormalEconomy.CumulativeHarvestedMilliunits = checked(
                    runtime.FormalEconomy.CumulativeHarvestedMilliunits + stored);
            else if (type == InventoryTransactionType.RecipeSettled)
                runtime.FormalEconomy.CumulativeExternalProductionMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeExternalProductionMilliunits + stored);
            Commit(runtime, destinationSourceId, productId);
            return stored;
        }

        public long ConsumeInventory(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string productId,
            long quantity,
            InventoryTransactionType type,
            string operationId,
            string sourceFreightId = "")
        {
            Ensure(runtime);
            if (quantity <= 0) return 0;
            var binding = FindBinding(runtime.FormalEconomy, sourceId,
                productId) ?? EnsureBinding(runtime, sourceId, productId);
            var transaction = NewTransaction(runtime, type, operationId);
            transaction.SourceCivilianFreightId =
                sourceFreightId ?? string.Empty;
            var consumed = RemoveFromBatches(runtime.FormalEconomy,
                binding.InventoryContainerId, productId, quantity,
                transaction);
            if (consumed <= 0) return 0;
            runtime.FormalEconomy.InventoryTransactions.Add(transaction);
            if (type == InventoryTransactionType.CivilianFreightNaturalLoss)
                runtime.FormalEconomy.CumulativeTransportLossMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeTransportLossMilliunits + consumed);
            else if (type == InventoryTransactionType.FoodStorageNaturalLoss)
                runtime.FormalEconomy.CumulativeStorageLossMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeStorageLossMilliunits + consumed);
            else if (type == InventoryTransactionType.FoodConsumed)
                runtime.FormalEconomy.CumulativeConsumedMilliunits = checked(
                    runtime.FormalEconomy.CumulativeConsumedMilliunits +
                    consumed);
            Commit(runtime, sourceId, productId);
            return consumed;
        }

        public long Transfer(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string destinationId,
            string productId,
            long quantity,
            InventoryTransactionType type,
            string operationId,
            string sourceFreightId = "")
        {
            Ensure(runtime);
            if (quantity <= 0 || sourceId == destinationId) return 0;
            var source = FindBinding(runtime.FormalEconomy, sourceId,
                productId) ?? EnsureBinding(runtime, sourceId, productId);
            var destination = FindBinding(runtime.FormalEconomy,
                destinationId, productId) ?? EnsureBinding(runtime,
                destinationId, productId);
            var available = GetAvailableQuantity(runtime, sourceId, productId);
            var capacity = AvailableCapacity(runtime.FormalEconomy,
                destination.InventoryContainerId);
            var moved = Math.Min(quantity, Math.Min(available, capacity));
            if (moved <= 0) return 0;
            var transaction = NewTransaction(runtime, type, operationId);
            transaction.SourceCivilianFreightId =
                sourceFreightId ?? string.Empty;
            var remaining = moved;
            foreach (var batch in runtime.FormalEconomy.ProductBatches
                         .Where(item => item.InventoryContainerId ==
                                        source.InventoryContainerId &&
                                        item.ProductDefinitionId == productId &&
                                        item.Quantity > item.ReservedQuantity)
                         .OrderBy(item => item.ProducedDay)
                         .ThenBy(item => item.Id, StringComparer.Ordinal)
                         .ToArray())
            {
                if (remaining <= 0) break;
                var part = Math.Min(remaining,
                    batch.Quantity - batch.ReservedQuantity);
                batch.Quantity -= part;
                transaction.Lines.Add(
                    LuoyangFormalEconomyDomain.Line(batch, -part));
                var target = AddOrMergeBatch(runtime,
                    destination.InventoryContainerId, productId, part,
                    transaction.Id, batch.SourceWorkOrderId,
                    batch.ProducedDay, batch.QualityBasisPoints,
                    batch.FreshnessBasisPoints);
                transaction.Lines.Add(
                    LuoyangFormalEconomyDomain.Line(target, part));
                remaining -= part;
            }
            if (remaining != 0)
                throw new InvalidOperationException(
                    "Formal transfer could not consume its planned source.");
            runtime.FormalEconomy.InventoryTransactions.Add(transaction);
            if (type == InventoryTransactionType.FoodMarketTransferred)
                runtime.FormalEconomy.CumulativeMarketTransferredMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeMarketTransferredMilliunits + moved);
            else if (type == InventoryTransactionType.FoodTaxTransferred ||
                     type == InventoryTransactionType.FoodTaxRemitted)
                runtime.FormalEconomy.CumulativeTaxTransferredMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeTaxTransferredMilliunits + moved);
            else if (type ==
                         InventoryTransactionType
                             .FoodVillageReliefTransferred ||
                     type ==
                         InventoryTransactionType
                             .FoodCountyReliefTransferred)
                runtime.FormalEconomy.CumulativeReliefTransferredMilliunits =
                    checked(runtime.FormalEconomy
                        .CumulativeReliefTransferredMilliunits + moved);
            Commit(runtime, sourceId, productId);
            RefreshProjection(runtime, destinationId, productId);
            return moved;
        }

        public long TransferToHousehold(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            int householdIndex,
            string productId,
            long quantity,
            InventoryTransactionType type,
            string operationId)
        {
            Ensure(runtime);
            if (householdIndex < 0 ||
                householdIndex >= runtime.Households.Count)
                throw new ArgumentOutOfRangeException(nameof(householdIndex));
            var moved = Transfer(runtime, sourceId, HouseholdContainerId,
                productId, quantity, type, operationId);
            if (moved <= 0) return 0;
            runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[
                householdIndex] = checked(runtime.FormalEconomy
                    .HouseholdFoodClaimsMilliunits[householdIndex] + moved);
            runtime.Households[householdIndex].FoodReserveMilliunits =
                runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[
                    householdIndex];
            runtime.FormalEconomy.ProjectionRevision =
                runtime.FormalEconomy.Revision;
            return moved;
        }

        public long SupplyHouseholdBucketFromMarkets(
            Luoyang184LivingWorldRuntimeState runtime,
            int reserveBufferDays)
        {
            Ensure(runtime);
            if (reserveBufferDays <= 0) return 0;
            var selected = new List<int>();
            var start = (int)(runtime.AbsoluteDay % 30);
            for (var index = start; index < runtime.Households.Count;
                 index += 30)
                selected.Add(index);
            if (selected.Count == 0) return 0;

            long acquiredTotal = 0;
            foreach (var source in runtime.Inventories.Where(item =>
                         item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                         IsFood(item.ProductId))
                         .OrderBy(item =>
                         {
                             var market = runtime.Markets.Find(value =>
                                 value.ProductId == item.ProductId);
                             return Math.Max(1L, (market?.BasePrice ?? 1) *
                                 (market?.CurrentPriceBasisPoints ?? 10_000) /
                                 10_000L);
                         })
                         .ThenBy(item => item.Id, StringComparer.Ordinal))
            {
                var available = GetAvailableQuantity(runtime, source.Id,
                    source.ProductId);
                if (available <= 0) continue;
                var market = runtime.Markets.Find(item =>
                    item.ProductId == source.ProductId);
                var unitPrice = Math.Max(1L, (market?.BasePrice ?? 1) *
                    (market?.CurrentPriceBasisPoints ?? 10_000) / 10_000L);
                var allocations = new List<HouseholdAllocation>();
                long planned = 0;
                foreach (var householdIndex in selected)
                {
                    if (planned >= available) break;
                    var household = runtime.Households[householdIndex];
                    var elapsed = runtime.AbsoluteDay -
                                  household.LastConsumptionSettlementDay;
                    if (elapsed <= 0) continue;
                    var target = checked(household.DailyFoodDemandMilliunits *
                        (elapsed + reserveBufferDays));
                    var claim = runtime.FormalEconomy
                        .HouseholdFoodClaimsMilliunits[householdIndex];
                    var needed = Math.Max(0, target - claim);
                    var affordable = household.Wealth <= 0 ? 0 : checked(
                        household.Wealth * 1_000 / unitPrice);
                    var amount = Math.Min(needed,
                        Math.Min(affordable, available - planned));
                    if (amount <= 0) continue;
                    allocations.Add(new HouseholdAllocation(householdIndex,
                        amount));
                    planned += amount;
                }
                if (planned <= 0) continue;
                var moved = Transfer(runtime, source.Id, HouseholdContainerId,
                    source.ProductId, planned,
                    InventoryTransactionType.FoodMarketTransferred,
                    "market.household.formal_bucket." + runtime.AbsoluteDay +
                    "." + source.Id);
                var remaining = moved;
                long money = 0;
                foreach (var allocation in allocations)
                {
                    if (remaining <= 0) break;
                    var quantity = Math.Min(allocation.Quantity, remaining);
                    var household = runtime.Households[allocation.HouseholdIndex];
                    var cost = checked((quantity * unitPrice + 999) / 1_000);
                    cost = Math.Min(cost, household.Wealth);
                    household.Wealth -= cost;
                    household.CumulativeMoneySpent += cost;
                    household.CumulativeFoodAcquiredMilliunits += quantity;
                    household.LastAcquisitionSourceId = source.Id;
                    runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[
                        allocation.HouseholdIndex] = checked(runtime
                        .FormalEconomy.HouseholdFoodClaimsMilliunits[
                            allocation.HouseholdIndex] + quantity);
                    household.FoodReserveMilliunits = runtime.FormalEconomy
                        .HouseholdFoodClaimsMilliunits[
                            allocation.HouseholdIndex];
                    money += cost;
                    remaining -= quantity;
                }
                if (remaining != 0)
                    throw new InvalidOperationException(
                        "Household market allocation did not consume transfer.");
                if (market != null)
                {
                    market.CashBalance += money;
                    market.RecentTradeQuantityMilliunits += moved;
                    market.RecentTradeValue += money;
                    market.TransferredMilliunits += moved;
                }
                runtime.MarketTrades.Add(new LuoyangMarketTradeRuntimeState
                {
                    Id = "market_trade.household_formal_batch." +
                         runtime.AbsoluteDay + "." +
                         runtime.MarketTrades.Count.ToString("D6"),
                    Day = runtime.AbsoluteDay,
                    ProductId = source.ProductId,
                    BuyerId = "household.batch.luoyang.184",
                    SellerId = source.OwnerId,
                    SourceInventoryId = source.Id,
                    QuantityMilliunits = moved,
                    UnitPrice = unitPrice,
                    MoneyTransferred = money,
                    TradeOrderId = "trade_order.household_formal_batch." +
                                   runtime.AbsoluteDay + "." + source.Id
                });
                acquiredTotal += moved;
            }
            runtime.FormalEconomy.ProjectionRevision =
                runtime.FormalEconomy.Revision;
            return acquiredTotal;
        }

        public long CollectHouseholdTax(
            Luoyang184LivingWorldRuntimeState runtime,
            string destinationId,
            long maximum,
            string operationId)
        {
            Ensure(runtime);
            var capacity = AvailableCapacity(runtime.FormalEconomy,
                EnsureBinding(runtime, destinationId,
                    ProductForBinding(runtime, destinationId))
                    .InventoryContainerId);
            var target = Math.Min(maximum, capacity);
            long planned = 0;
            var dues = new long[runtime.Households.Count];
            for (var i = 0; i < dues.Length && planned < target; i++)
            {
                var claim = runtime.FormalEconomy
                    .HouseholdFoodClaimsMilliunits[i];
                var due = Math.Min(claim, Math.Max(0L, (claim + 99) / 100));
                due = Math.Min(due, target - planned);
                dues[i] = due;
                planned += due;
            }
            if (planned <= 0) return 0;
            long moved = 0;
            foreach (var product in runtime.FormalEconomy.ProductBatches
                         .Where(item => item.InventoryContainerId ==
                                        HouseholdContainerId &&
                                        IsFood(item.ProductDefinitionId) &&
                                        item.Quantity > item.ReservedQuantity)
                         .OrderBy(item => item.ProducedDay)
                         .ThenBy(item => item.ProductDefinitionId,
                             StringComparer.Ordinal)
                         .Select(item => item.ProductDefinitionId)
                         .Distinct(StringComparer.Ordinal).ToArray())
            {
                if (moved >= planned) break;
                moved += Transfer(runtime, HouseholdContainerId,
                    destinationId, product, planned - moved,
                    InventoryTransactionType.FoodTaxTransferred,
                    operationId + "." + product);
            }
            var remaining = moved;
            for (var i = 0; i < dues.Length && remaining > 0; i++)
            {
                var paid = Math.Min(dues[i], remaining);
                runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[i] -= paid;
                runtime.Households[i].FoodReserveMilliunits =
                    runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[i];
                remaining -= paid;
            }
            return moved;
        }

        public void SettleHouseholdConsumption(
            Luoyang184LivingWorldRuntimeState runtime,
            bool settleAll,
            out long consumedTotal,
            out long shortageTotal)
        {
            Ensure(runtime);
            consumedTotal = 0;
            shortageTotal = 0;
            var start = settleAll ? 0 : (int)(runtime.AbsoluteDay % 30);
            var step = settleAll ? 1 : 30;
            for (var index = start; index < runtime.Households.Count;
                 index += step)
            {
                var household = runtime.Households[index];
                var elapsed = runtime.AbsoluteDay -
                              household.LastConsumptionSettlementDay;
                if (elapsed <= 0) continue;
                var demand = checked(household.DailyFoodDemandMilliunits *
                                     elapsed);
                var claim = runtime.FormalEconomy
                    .HouseholdFoodClaimsMilliunits[index];
                var consumed = Math.Min(demand, claim);
                var shortage = demand - consumed;
                runtime.FormalEconomy.HouseholdFoodClaimsMilliunits[index] =
                    claim - consumed;
                household.FoodReserveMilliunits = claim - consumed;
                household.CumulativeFoodDemandMilliunits += demand;
                household.CumulativeFoodConsumedMilliunits += consumed;
                household.CumulativeFoodShortageMilliunits += shortage;
                household.FoodSecurityBasisPoints = demand <= 0
                    ? 10_000
                    : (int)Math.Min(10_000,
                        consumed * 10_000 / demand);
                household.LastAcquisitionSourceId = consumed > 0
                    ? "formal.household.aggregate"
                    : "acquisition.required";
                household.AiResponseActionId = shortage <= 0
                    ? "household.consume_and_monitor"
                    : household.CumulativeFoodShortageMilliunits > demand * 7
                        ? "household.seek_relief_or_migration"
                        : "household.seek_market_or_relief";
                household.LastConsumptionSettlementDay = runtime.AbsoluteDay;
                consumedTotal += consumed;
                shortageTotal += shortage;
            }
            if (consumedTotal > 0)
            {
                var transaction = NewTransaction(runtime,
                    InventoryTransactionType.FoodConsumed,
                    "household.formal_consumption." + runtime.AbsoluteDay +
                    "." + (settleAll ? "final" : "bucket"));
                var removed = RemoveFromBatches(runtime.FormalEconomy,
                    HouseholdContainerId, null, consumedTotal, transaction);
                if (removed != consumedTotal)
                    throw new InvalidOperationException(
                        "Formal household claims exceed physical household food.");
                runtime.FormalEconomy.InventoryTransactions.Add(transaction);
                runtime.FormalEconomy.CumulativeConsumedMilliunits = checked(
                    runtime.FormalEconomy.CumulativeConsumedMilliunits +
                    consumedTotal);
                Commit(runtime, HouseholdContainerId, null);
            }
        }

        public long DispatchFreight(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string shipmentId,
            string productId,
            long shippedQuantity,
            long explicitLoss,
            string carrierPersonId)
        {
            Ensure(runtime);
            var cargoId = FreightContainerId(shipmentId);
            EnsureFreightBinding(runtime, cargoId, productId,
                shippedQuantity);
            var moved = Transfer(runtime, sourceId, cargoId, productId,
                shippedQuantity,
                InventoryTransactionType.CivilianFreightDispatched,
                "freight.dispatch." + shipmentId, shipmentId);
            if (moved != shippedQuantity)
                throw new InvalidOperationException(
                    "Formal freight source cannot supply planned cargo.");
            var loss = Math.Min(explicitLoss, moved);
            if (loss > 0)
                ConsumeInventory(runtime, cargoId, productId, loss,
                    InventoryTransactionType.CivilianFreightNaturalLoss,
                    "freight.loss." + shipmentId, shipmentId);
            runtime.FormalEconomy.CumulativeFreightDispatchedMilliunits =
                checked(runtime.FormalEconomy
                    .CumulativeFreightDispatchedMilliunits + moved);
            return moved;
        }

        public long ReceiveFreight(
            Luoyang184LivingWorldRuntimeState runtime,
            string shipmentId,
            string destinationId,
            string productId,
            long quantity)
        {
            Ensure(runtime);
            var moved = Transfer(runtime, FreightContainerId(shipmentId),
                destinationId, productId, quantity,
                InventoryTransactionType.CivilianFreightDelivered,
                "freight.delivery." + shipmentId, shipmentId);
            runtime.FormalEconomy.CumulativeFreightDeliveredMilliunits =
                checked(runtime.FormalEconomy
                    .CumulativeFreightDeliveredMilliunits + moved);
            return moved;
        }

        public static long GetAvailableQuantity(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string productId = null)
        {
            if (runtime?.FormalEconomy == null ||
                !runtime.FormalEconomy.IsPhysicalAuthority)
                throw new InvalidOperationException(
                    "Formal economy authority is not active.");
            var binding = FindBinding(runtime.FormalEconomy, sourceId,
                productId);
            if (binding == null) return 0;
            return runtime.FormalEconomy.ProductBatches.Where(item =>
                    item.InventoryContainerId == binding.InventoryContainerId &&
                    (string.IsNullOrWhiteSpace(productId) ||
                     item.ProductDefinitionId == productId))
                .Sum(item => item.Quantity - item.ReservedQuantity);
        }

        public void RebuildProjection(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var stopwatch = Stopwatch.StartNew();
            LuoyangFormalEconomyDomain.RebuildProjection(runtime);
            stopwatch.Stop();
            runtime.FormalEconomy.ProjectionRebuildMilliseconds = checked(
                runtime.FormalEconomy.ProjectionRebuildMilliseconds +
                stopwatch.ElapsedMilliseconds);
            runtime.FormalEconomy.PeakManagedMemoryBytes = Math.Max(
                runtime.FormalEconomy.PeakManagedMemoryBytes,
                GC.GetTotalMemory(false));
        }

        public LuoyangFormalEconomyAuditResult Audit(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            Ensure(runtime);
            var formal = runtime.FormalEconomy;
            var result = new LuoyangFormalEconomyAuditResult
            {
                BatchCount = formal.ProductBatches.Count,
                TransactionCount = formal.InventoryTransactions.Count,
                FormalFoodQuantityMilliunits = formal.ProductBatches
                    .Where(item => IsFood(item.ProductDefinitionId))
                    .Sum(item => item.Quantity),
                HouseholdFormalQuantityMilliunits = formal.ProductBatches
                    .Where(item => item.InventoryContainerId ==
                                   HouseholdContainerId &&
                                   IsFood(item.ProductDefinitionId))
                    .Sum(item => item.Quantity),
                HouseholdClaimQuantityMilliunits = formal
                    .HouseholdFoodClaimsMilliunits.Sum(),
                AuthorityHash = ComputeAuthorityHash(runtime),
                ProjectionHash =
                    LuoyangFormalEconomyDomain.ComputeProjectionHash(runtime)
            };
            result.ProjectedFoodQuantityMilliunits = ProjectedFood(runtime);
            result.ProjectionDifferenceMilliunits = checked(
                result.ProjectedFoodQuantityMilliunits -
                result.FormalFoodQuantityMilliunits);
            result.HouseholdClaimDifferenceMilliunits = checked(
                result.HouseholdClaimQuantityMilliunits -
                result.HouseholdFormalQuantityMilliunits);
            result.DuplicateBatchCount = formal.ProductBatches
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .Count(group => group.Count() != 1);
            result.DuplicateTransactionCount = formal.InventoryTransactions
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .Count(group => group.Count() != 1);
            var containerIds = new HashSet<string>(
                formal.InventoryContainers.Select(item => item.Id),
                StringComparer.Ordinal);
            result.MissingContainerCount = formal.ProductBatches.Count(item =>
                !containerIds.Contains(item.InventoryContainerId));
            result.InvalidBatchCount = formal.ProductBatches.Count(item =>
                item == null || string.IsNullOrWhiteSpace(item.Id) ||
                item.Quantity < 0 || item.ReservedQuantity < 0 ||
                item.ReservedQuantity > item.Quantity) +
                result.DuplicateBatchCount + result.MissingContainerCount;
            result.UnknownPhysicalDeltaCount = formal.InventoryTransactions
                .Count(item => !IsKnownTransaction(item.Type));
            return result;
        }

        private static long AddOpeningToInventories(
            Luoyang184LivingWorldRuntimeState runtime,
            IEnumerable<LuoyangInventoryBalanceState> candidates,
            long quantity,
            string reason)
        {
            long remaining = quantity;
            foreach (var inventory in candidates
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                if (remaining <= 0) break;
                var added = new LuoyangFormalEconomySystem().Produce(runtime,
                    inventory.Id, inventory.ProductId, remaining,
                    InventoryTransactionType.OpeningBalance,
                    "normal_supply." + reason + "." + inventory.Id);
                remaining -= added;
            }
            return quantity - remaining;
        }

        private static void AddHouseholdOpeningReserve(
            Luoyang184LivingWorldRuntimeState runtime,
            long quantity,
            string reason)
        {
            if (quantity <= 0) return;
            var formal = runtime.FormalEconomy;
            var transaction = NewTransaction(runtime,
                InventoryTransactionType.OpeningBalance,
                "normal_supply." + reason);
            var products = new[]
            {
                new ProductShare("product.food.millet_grain", 6_000),
                new ProductShare("product.food.wheat_grain", 2_500),
                new ProductShare("product.food.broomcorn_grain", 1_000),
                new ProductShare("product.food.bean", 500)
            };
            long assigned = 0;
            foreach (var share in products)
            {
                var amount = share == products[products.Length - 1]
                    ? quantity - assigned
                    : quantity * share.BasisPoints / 10_000;
                assigned += amount;
                if (amount <= 0) continue;
                var batch = AddOrMergeBatch(runtime, HouseholdContainerId,
                    share.ProductId, amount, transaction.Id,
                    "scenario.normal_supply.opening", runtime.AbsoluteDay,
                    10_000, 10_000);
                transaction.Lines.Add(
                    LuoyangFormalEconomyDomain.Line(batch, amount));
            }
            formal.InventoryTransactions.Add(transaction);
            var totalDemand = Math.Max(1L, runtime.DailyFoodDemandMilliunits);
            long claimsAssigned = 0;
            for (var i = 0; i < runtime.Households.Count; i++)
            {
                var amount = i == runtime.Households.Count - 1
                    ? quantity - claimsAssigned
                    : quantity * runtime.Households[i]
                        .DailyFoodDemandMilliunits / totalDemand;
                claimsAssigned += amount;
                formal.HouseholdFoodClaimsMilliunits[i] = checked(
                    formal.HouseholdFoodClaimsMilliunits[i] + amount);
                runtime.Households[i].FoodReserveMilliunits =
                    formal.HouseholdFoodClaimsMilliunits[i];
            }
            formal.Revision++;
            formal.ProjectionRevision = formal.Revision;
        }

        private static ProductBatchState AddOrMergeBatch(
            Luoyang184LivingWorldRuntimeState runtime,
            string containerId,
            string productId,
            long quantity,
            string transactionId,
            string sourceWorkOrderId,
            long producedDay,
            int quality,
            int freshness)
        {
            var formal = runtime.FormalEconomy;
            var existing = formal.ProductBatches.Find(item =>
                item.InventoryContainerId == containerId &&
                item.ProductDefinitionId == productId &&
                item.SourceWorkOrderId == (sourceWorkOrderId ?? string.Empty) &&
                item.ProducedDay == producedDay &&
                item.QualityBasisPoints == quality &&
                item.FreshnessBasisPoints == freshness &&
                item.ReservedQuantity == 0);
            if (existing != null)
            {
                existing.Quantity = checked(existing.Quantity + quantity);
                return existing;
            }
            var container = formal.InventoryContainers.Find(item =>
                item.Id == containerId) ?? throw new InvalidOperationException(
                "Missing formal inventory container " + containerId + ".");
            var batch = new ProductBatchState
            {
                Id = "batch.luoyang.formal." +
                     formal.ProductBatches.Count.ToString("D8"),
                ProductDefinitionId = productId,
                OwnerFamilyId = container.OwnerFamilyId ?? string.Empty,
                OwnerOrganizationId =
                    container.OwnerOrganizationId ?? string.Empty,
                InventoryContainerId = containerId,
                OriginLocationId = container.LocationId,
                SourceWorkOrderId = sourceWorkOrderId ?? string.Empty,
                SourceTransactionId = transactionId,
                UnitId = "unit.milliunit",
                UnitWeight = 1,
                ProducedDay = producedDay,
                Quantity = quantity,
                QualityBasisPoints = quality,
                FreshnessBasisPoints = freshness
            };
            formal.ProductBatches.Add(batch);
            return batch;
        }

        private static long RemoveFromBatches(
            LuoyangFormalEconomyRuntimeState formal,
            string containerId,
            string productId,
            long quantity,
            InventoryTransactionState transaction)
        {
            var remaining = quantity;
            foreach (var batch in formal.ProductBatches.Where(item =>
                         item.InventoryContainerId == containerId &&
                         (string.IsNullOrWhiteSpace(productId) ||
                          item.ProductDefinitionId == productId) &&
                         item.Quantity > item.ReservedQuantity)
                         .OrderBy(item => item.ProducedDay)
                         .ThenBy(item => item.Id, StringComparer.Ordinal)
                         .ToArray())
            {
                if (remaining <= 0) break;
                var part = Math.Min(remaining,
                    batch.Quantity - batch.ReservedQuantity);
                batch.Quantity -= part;
                transaction.Lines.Add(
                    LuoyangFormalEconomyDomain.Line(batch, -part));
                remaining -= part;
            }
            return quantity - remaining;
        }

        private static InventoryTransactionState NewTransaction(
            Luoyang184LivingWorldRuntimeState runtime,
            InventoryTransactionType type,
            string operationId)
        {
            var baseId = "transaction.luoyang.formal." + operationId;
            var id = baseId;
            var suffix = 0;
            while (runtime.FormalEconomy.InventoryTransactions.Exists(item =>
                       item.Id == id))
                id = baseId + "." + (++suffix);
            return new InventoryTransactionState
            {
                Id = id,
                Day = runtime.AbsoluteDay,
                Type = type,
                Summary = operationId
            };
        }

        private static LuoyangFormalInventoryBindingState EnsureBinding(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string productId)
        {
            var formal = runtime.FormalEconomy;
            var existing = FindBinding(formal, sourceId, productId);
            if (existing != null) return existing;
            var inventory = runtime.Inventories.Find(item =>
                item.Id == sourceId);
            var supplier = runtime.ExternalSuppliers.Find(item =>
                item.InventoryId == sourceId);
            var kind = inventory != null
                ? LuoyangFormalInventoryProjectionKind.CompactInventory
                : supplier != null
                    ? LuoyangFormalInventoryProjectionKind.ExternalSupplier
                    : sourceId == HouseholdContainerId
                        ? LuoyangFormalInventoryProjectionKind.HouseholdAggregate
                        : sourceId.StartsWith("inventory.formal.luoyang.freight.",
                            StringComparison.Ordinal)
                            ? LuoyangFormalInventoryProjectionKind.FreightCargo
                            : throw new InvalidOperationException(
                                "Unknown formal inventory source " + sourceId +
                                ".");
            var containerId = kind ==
                              LuoyangFormalInventoryProjectionKind
                                  .CompactInventory ||
                              kind == LuoyangFormalInventoryProjectionKind
                                  .ExternalSupplier
                ? LuoyangFormalEconomyContract.ContainerId(sourceId)
                : sourceId;
            if (!formal.InventoryContainers.Exists(item =>
                    item.Id == containerId))
                formal.InventoryContainers.Add(new InventoryContainerState
                {
                    Id = containerId,
                    KindId = "inventory.kind.luoyang.formal.aggregate",
                    OwnerOrganizationId = inventory?.OwnerId ??
                                          supplier?.OrganizationId ?? string.Empty,
                    LocationId = inventory?.CurrentLocationId ??
                                 supplier?.SettlementId ??
                                 "location.capital.luoyang",
                    CapacityWeight = inventory?.CapacityMilliunits ??
                                     supplier?.StorageCapacityMilliunits ??
                                     long.MaxValue
                });
            var binding = new LuoyangFormalInventoryBindingState
            {
                SourceId = sourceId,
                ProductId = productId,
                InventoryContainerId = containerId,
                ProjectionKind = kind
            };
            formal.InventoryBindings.Add(binding);
            return binding;
        }

        private static void EnsureFreightBinding(
            Luoyang184LivingWorldRuntimeState runtime,
            string cargoId,
            string productId,
            long capacity)
        {
            if (FindBinding(runtime.FormalEconomy, cargoId, productId) != null)
                return;
            runtime.FormalEconomy.InventoryContainers.Add(
                new InventoryContainerState
                {
                    Id = cargoId,
                    KindId = "inventory.kind.mobile.freight",
                    LocationId = "transit",
                    CapacityWeight = Math.Max(0, capacity)
                });
            runtime.FormalEconomy.InventoryBindings.Add(
                new LuoyangFormalInventoryBindingState
                {
                    SourceId = cargoId,
                    ProductId = productId,
                    InventoryContainerId = cargoId,
                    ProjectionKind =
                        LuoyangFormalInventoryProjectionKind.FreightCargo
                });
        }

        private static LuoyangFormalInventoryBindingState FindBinding(
            LuoyangFormalEconomyRuntimeState formal,
            string sourceId,
            string productId) =>
            formal.InventoryBindings.Find(item =>
                (item.SourceId == sourceId ||
                 item.InventoryContainerId == sourceId) &&
                (string.IsNullOrWhiteSpace(productId) ||
                 item.ProductId == productId));

        private static string ProductForBinding(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId) =>
            runtime.FormalEconomy.InventoryBindings
                .Where(item => item.SourceId == sourceId &&
                               IsFood(item.ProductId))
                .OrderBy(item => item.ProductId, StringComparer.Ordinal)
                .Select(item => item.ProductId).FirstOrDefault() ??
            runtime.Inventories.Find(item => item.Id == sourceId)?.ProductId ??
            throw new InvalidOperationException(
                "Formal inventory has no food product binding.");

        private static string FirstAvailableProduct(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId)
        {
            var binding = FindBinding(runtime.FormalEconomy, sourceId, null) ??
                          throw new InvalidOperationException(
                              "Formal source has no product binding.");
            return runtime.FormalEconomy.ProductBatches
                .Where(item => item.InventoryContainerId ==
                               binding.InventoryContainerId &&
                               IsFood(item.ProductDefinitionId) &&
                               item.Quantity > item.ReservedQuantity)
                .OrderBy(item => item.ProducedDay)
                .ThenBy(item => item.ProductDefinitionId,
                    StringComparer.Ordinal)
                .Select(item => item.ProductDefinitionId).FirstOrDefault() ??
                throw new InvalidOperationException(
                    "Formal household inventory has no available food.");
        }

        private static long AvailableCapacity(
            LuoyangFormalEconomyRuntimeState formal,
            string containerId)
        {
            var container = formal.InventoryContainers.Find(item =>
                item.Id == containerId) ?? throw new InvalidOperationException(
                "Missing formal container " + containerId + ".");
            if (container.CapacityWeight == long.MaxValue) return long.MaxValue;
            var used = formal.ProductBatches.Where(item =>
                item.InventoryContainerId == containerId).Sum(item =>
                checked(item.Quantity * Math.Max(1, item.UnitWeight)));
            return Math.Max(0, container.CapacityWeight - used);
        }

        private static void Commit(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string productId)
        {
            runtime.FormalEconomy.Revision++;
            RefreshProjection(runtime, sourceId, productId);
            runtime.FormalEconomy.ProjectionRevision =
                runtime.FormalEconomy.Revision;
        }

        private static void RefreshProjection(
            Luoyang184LivingWorldRuntimeState runtime,
            string sourceId,
            string productId)
        {
            var binding = FindBinding(runtime.FormalEconomy, sourceId,
                productId);
            if (binding == null) return;
            var quantity = binding.ProjectionKind ==
                           LuoyangFormalInventoryProjectionKind
                               .CompactInventory ||
                           binding.ProjectionKind ==
                           LuoyangFormalInventoryProjectionKind
                               .ExternalSupplier
                ? LuoyangFormalEconomyDomain.Quantity(runtime.FormalEconomy,
                    binding.InventoryContainerId)
                : LuoyangFormalEconomyDomain.Quantity(runtime.FormalEconomy,
                    binding.InventoryContainerId, binding.ProductId);
            if (binding.ProjectionKind ==
                LuoyangFormalInventoryProjectionKind.CompactInventory)
            {
                var inventory = runtime.Inventories.Find(item =>
                    item.Id == binding.SourceId);
                if (inventory != null) inventory.QuantityMilliunits = quantity;
            }
            else if (binding.ProjectionKind ==
                     LuoyangFormalInventoryProjectionKind.ExternalSupplier)
            {
                var supplier = runtime.ExternalSuppliers.Find(item =>
                    item.InventoryId == binding.SourceId);
                if (supplier != null)
                    supplier.InventoryQuantityMilliunits = quantity;
            }
        }

        private static long ProjectedFood(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var compact = runtime.Inventories.Where(item =>
                    IsFood(item.ProductId)).Sum(item => item.QuantityMilliunits) +
                runtime.ExternalSuppliers.Where(item =>
                    IsFood(item.ProductId)).Sum(item =>
                    item.InventoryQuantityMilliunits) +
                runtime.Households.Sum(item => item.FoodReserveMilliunits);
            var freight = runtime.FormalEconomy.InventoryBindings.Where(item =>
                    item.ProjectionKind ==
                    LuoyangFormalInventoryProjectionKind.FreightCargo &&
                    IsFood(item.ProductId))
                .Sum(item => LuoyangFormalEconomyDomain.Quantity(
                    runtime.FormalEconomy, item.InventoryContainerId,
                    item.ProductId));
            return checked(compact + freight);
        }

        private static bool IsKnownTransaction(InventoryTransactionType type)
        {
            switch (type)
            {
                case InventoryTransactionType.OpeningBalance:
                case InventoryTransactionType.FoodHarvested:
                case InventoryTransactionType.RecipeSettled:
                case InventoryTransactionType.FoodConsumed:
                case InventoryTransactionType.FoodTaxTransferred:
                case InventoryTransactionType.FoodTaxRemitted:
                case InventoryTransactionType.FoodVillageReliefTransferred:
                case InventoryTransactionType.FoodCountyReliefTransferred:
                case InventoryTransactionType.FoodMarketTransferred:
                case InventoryTransactionType.CivilianFreightDispatched:
                case InventoryTransactionType.CivilianFreightNaturalLoss:
                case InventoryTransactionType.CivilianFreightDelivered:
                case InventoryTransactionType.FoodStorageNaturalLoss:
                    return true;
                default:
                    return false;
            }
        }

        private static void Ensure(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.FormalEconomy == null ||
                !runtime.FormalEconomy.IsPhysicalAuthority)
                throw new InvalidOperationException(
                    "Luoyang formal economy authority is not active.");
        }

        private sealed class ProductShare
        {
            public readonly string ProductId;
            public readonly int BasisPoints;

            public ProductShare(string productId, int basisPoints)
            {
                ProductId = productId;
                BasisPoints = basisPoints;
            }
        }

        private sealed class HouseholdAllocation
        {
            public readonly int HouseholdIndex;
            public readonly long Quantity;

            public HouseholdAllocation(int householdIndex, long quantity)
            {
                HouseholdIndex = householdIndex;
                Quantity = quantity;
            }
        }
    }
}
