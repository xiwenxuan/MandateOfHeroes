using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class FormalCountyMarketSystem
    {
        private readonly ProductionContentRegistry _content;
        private readonly FoodInventorySystem _foodInventory;

        public FormalCountyMarketSystem(ProductionContentRegistry content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _foodInventory = new FoodInventorySystem(content);
        }

        public FormalMarketOrderState CreateSellOrder(
            WorldState world,
            string countyGovernanceId,
            string familyId,
            string storageFacilityId,
            string productDefinitionId,
            long quantity,
            long minimumUnitPrice,
            int minimumQualityBasisPoints,
            long expiryDay)
        {
            ValidateOrderRequest(
                world,
                countyGovernanceId,
                familyId,
                storageFacilityId,
                productDefinitionId,
                quantity,
                minimumUnitPrice,
                minimumQualityBasisPoints,
                expiryDay);
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == familyId &&
                    batch.StorageFacilityId == storageFacilityId &&
                    batch.ProductDefinitionId == productDefinitionId &&
                    batch.QualityBasisPoints >= minimumQualityBasisPoints &&
                    batch.Quantity > batch.ReservedQuantity)
                {
                    candidates.Add(batch);
                }
            }
            candidates.Sort(CompareSellBatches);

            var reservations = new List<FormalMarketBatchReservationState>();
            long remaining = quantity;
            for (var i = 0; i < candidates.Count && remaining > 0; i++)
            {
                var available = candidates[i].Quantity -
                    candidates[i].ReservedQuantity;
                var reserve = Math.Min(available, remaining);
                if (reserve <= 0)
                {
                    continue;
                }
                reservations.Add(new FormalMarketBatchReservationState
                {
                    BatchId = candidates[i].Id,
                    OriginalQuantity = reserve,
                    RemainingQuantity = reserve
                });
                remaining -= reserve;
            }
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    "The family does not have enough unreserved matching food.");
            }

            var order = NewOrder(
                world,
                countyGovernanceId,
                familyId,
                storageFacilityId,
                productDefinitionId,
                FormalMarketOrderSide.Sell,
                quantity,
                minimumUnitPrice,
                minimumQualityBasisPoints,
                expiryDay);
            order.BatchReservations = reservations;
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodMarketReserved,
                ProductInventorySystem.FindFamily(world, familyId).HeadPersonId,
                string.Empty,
                0,
                0,
                0,
                $"Reserved {quantity} units for formal market sell order {order.Id}.");
            transaction.SourceFormalMarketOrderId = order.Id;
            transaction.SourceCountyGovernanceId = countyGovernanceId;
            for (var i = 0; i < reservations.Count; i++)
            {
                var batch = FindBatch(world, reservations[i].BatchId);
                batch.ReservedQuantity = checked(
                    batch.ReservedQuantity + reservations[i].OriginalQuantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, reservations[i].OriginalQuantity));
            }

            world.FormalMarketOrders.Add(order);
            world.InventoryTransactions.Add(transaction);
            EnsurePrice(world, countyGovernanceId, productDefinitionId);
            return order;
        }

        public FormalMarketOrderState CreateBuyOrder(
            WorldState world,
            string countyGovernanceId,
            string familyId,
            string storageFacilityId,
            string productDefinitionId,
            long quantity,
            long maximumUnitPrice,
            int minimumQualityBasisPoints,
            long expiryDay)
        {
            ValidateOrderRequest(
                world,
                countyGovernanceId,
                familyId,
                storageFacilityId,
                productDefinitionId,
                quantity,
                maximumUnitPrice,
                minimumQualityBasisPoints,
                expiryDay);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            var escrow = checked(quantity * maximumUnitPrice);
            if (family.Wealth < escrow)
            {
                throw new InvalidOperationException(
                    "The family cannot fund the formal market buy order.");
            }

            var order = NewOrder(
                world,
                countyGovernanceId,
                familyId,
                storageFacilityId,
                productDefinitionId,
                FormalMarketOrderSide.Buy,
                quantity,
                maximumUnitPrice,
                minimumQualityBasisPoints,
                expiryDay);
            family.Wealth -= escrow;
            order.EscrowMoney = escrow;
            world.FormalMarketOrders.Add(order);
            EnsurePrice(world, countyGovernanceId, productDefinitionId);
            return order;
        }

        public void CancelOrder(
            WorldState world,
            string orderId,
            string reason = "cancelled")
        {
            CloseOrder(
                world,
                FindOrder(world, orderId),
                FormalMarketOrderStatus.Cancelled,
                reason);
        }

        public void ResolveDaily(WorldState world)
        {
            RequireFormalWorld(world);
            var active = ActiveOrders(world);
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].ExpiryDay < world.AbsoluteDay)
                {
                    CloseOrder(
                        world,
                        active[i],
                        FormalMarketOrderStatus.Expired,
                        "expired");
                }
            }

            var buys = ActiveOrders(world);
            buys.RemoveAll(order => order.Side != FormalMarketOrderSide.Buy);
            buys.Sort(CompareBuyOrders);
            for (var buyIndex = 0; buyIndex < buys.Count; buyIndex++)
            {
                var buy = buys[buyIndex];
                while (buy.Status == FormalMarketOrderStatus.Active &&
                       buy.RemainingQuantity > 0)
                {
                    var sell = FindBestSell(world, buy);
                    if (sell == null)
                    {
                        break;
                    }

                    var requested = Math.Min(
                        buy.RemainingQuantity, sell.RemainingQuantity);
                    var delivery = _foodInventory
                        .TransferReservedFamilyToFamily(
                            world,
                            sell.OwnerFamilyId,
                            sell.StorageFacilityId,
                            buy.OwnerFamilyId,
                            buy.StorageFacilityId,
                            ProductInventorySystem.FindFamily(
                                world, sell.OwnerFamilyId).HeadPersonId,
                            sell.BatchReservations,
                            requested,
                            sell.Id,
                            sell.CountyGovernanceId);
                    if (delivery.TransferredPhysicalQuantity <= 0)
                    {
                        break;
                    }

                    SettleTrade(
                        world,
                        buy,
                        sell,
                        delivery.TransferredPhysicalQuantity,
                        sell.UnitPrice,
                        delivery.InventoryTransactionId,
                        buy.CountyGovernanceId,
                        string.Empty);
                }
            }
        }

        public bool HasDailyWork(WorldState world)
        {
            RequireFormalWorld(world);
            var active = ActiveOrders(world);
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].ExpiryDay < world.AbsoluteDay)
                {
                    return true;
                }
            }

            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].Side == FormalMarketOrderSide.Buy &&
                    active[i].RemainingQuantity > 0 &&
                    FindBestSell(world, active[i]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void ValidateDailyResolution(
            WorldState world,
            long expectedDay)
        {
            RequireFormalWorld(world);
            if (expectedDay != world.AbsoluteDay)
            {
                throw new InvalidOperationException(
                    "Formal market daily command is no longer on its expected day.");
            }
        }

        public FormalMarketTradeState SettleCrossCountyDispatch(
            WorldState world,
            FormalMarketOrderState buy,
            FormalMarketOrderState sell,
            long quantity,
            string inventoryTransactionId,
            string civilianFreightId)
        {
            RequireFormalWorld(world);
            if (buy == null || sell == null || quantity <= 0 ||
                buy.Status != FormalMarketOrderStatus.Active ||
                sell.Status != FormalMarketOrderStatus.Active ||
                buy.Side != FormalMarketOrderSide.Buy ||
                sell.Side != FormalMarketOrderSide.Sell ||
                buy.CountyGovernanceId == sell.CountyGovernanceId ||
                buy.ProductDefinitionId != sell.ProductDefinitionId ||
                buy.OwnerFamilyId == sell.OwnerFamilyId ||
                sell.UnitPrice > buy.UnitPrice ||
                quantity > buy.RemainingQuantity ||
                quantity > sell.RemainingQuantity ||
                string.IsNullOrEmpty(inventoryTransactionId) ||
                string.IsNullOrEmpty(civilianFreightId))
            {
                throw new InvalidOperationException(
                    "Cross-county market settlement is invalid.");
            }

            return SettleTrade(
                world,
                buy,
                sell,
                quantity,
                sell.UnitPrice,
                inventoryTransactionId,
                buy.CountyGovernanceId,
                civilianFreightId);
        }

        public static int CalculateMarketPressureBasisPoints(
            WorldState world,
            string countyGovernanceId)
        {
            long current = 0;
            long equilibrium = 0;
            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                var price = world.FormalMarketPrices[i];
                if (price.CountyGovernanceId != countyGovernanceId)
                {
                    continue;
                }
                current = checked(current + price.LastTradeUnitPrice);
                equilibrium = checked(
                    equilibrium + price.EquilibriumUnitPrice);
            }
            return equilibrium == 0
                ? 10_000
                : (int)Math.Min(40_000L, current * 10_000L / equilibrium);
        }

        private FormalMarketTradeState SettleTrade(
            WorldState world,
            FormalMarketOrderState buy,
            FormalMarketOrderState sell,
            long quantity,
            long unitPrice,
            string inventoryTransactionId,
            string destinationCountyGovernanceId,
            string civilianFreightId)
        {
            var money = checked(quantity * unitPrice);
            if (money > buy.EscrowMoney)
            {
                throw new InvalidOperationException(
                    "Formal market settlement exceeds buyer escrow.");
            }

            var seller = ProductInventorySystem.FindFamily(
                world, sell.OwnerFamilyId);
            buy.EscrowMoney -= money;
            seller.Wealth = checked(seller.Wealth + money);
            buy.RemainingQuantity -= quantity;
            sell.RemainingQuantity -= quantity;
            buy.FilledQuantity += quantity;
            sell.FilledQuantity += quantity;
            buy.SettledMoney += money;
            sell.SettledMoney += money;
            var trade = new FormalMarketTradeState
            {
                Id = $"formal_market_trade.{world.AbsoluteDay}." +
                     $"{world.FormalMarketTrades.Count:D6}",
                Day = world.AbsoluteDay,
                CountyGovernanceId = sell.CountyGovernanceId,
                DestinationCountyGovernanceId =
                    destinationCountyGovernanceId,
                BuyOrderId = buy.Id,
                SellOrderId = sell.Id,
                BuyerFamilyId = buy.OwnerFamilyId,
                SellerFamilyId = sell.OwnerFamilyId,
                ProductDefinitionId = buy.ProductDefinitionId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                MoneyTransferred = money,
                SellerProceeds = money,
                InventoryTransactionId = inventoryTransactionId,
                CivilianFreightId = civilianFreightId ?? string.Empty
            };
            world.FormalMarketTrades.Add(trade);
            var price = EnsurePrice(
                world, sell.CountyGovernanceId, buy.ProductDefinitionId);
            price.LastTradeUnitPrice = unitPrice;
            price.LastTradeDay = world.AbsoluteDay;
            price.CumulativeTradedQuantity = checked(
                price.CumulativeTradedQuantity + quantity);
            price.CumulativeTurnover = checked(
                price.CumulativeTurnover + money);

            if (sell.RemainingQuantity == 0)
            {
                CloseFilledOrder(world, sell);
            }
            if (buy.RemainingQuantity == 0)
            {
                CloseFilledOrder(world, buy);
            }
            return trade;
        }

        private void CloseFilledOrder(
            WorldState world,
            FormalMarketOrderState order)
        {
            order.Status = FormalMarketOrderStatus.Filled;
            order.ClosedDay = world.AbsoluteDay;
            order.CloseReason = "filled";
            if (order.Side == FormalMarketOrderSide.Buy &&
                order.EscrowMoney > 0)
            {
                var family = ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId);
                family.Wealth = checked(family.Wealth + order.EscrowMoney);
                order.EscrowMoney = 0;
            }
        }

        private void CloseOrder(
            WorldState world,
            FormalMarketOrderState order,
            FormalMarketOrderStatus status,
            string reason)
        {
            if (order.Status != FormalMarketOrderStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Formal market order {order.Id} is already closed.");
            }

            if (order.Side == FormalMarketOrderSide.Buy)
            {
                var family = ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId);
                family.Wealth = checked(family.Wealth + order.EscrowMoney);
                order.EscrowMoney = 0;
            }
            else
            {
                ReleaseReservations(world, order);
            }
            order.Status = status;
            order.ClosedDay = world.AbsoluteDay;
            order.CloseReason = reason ?? string.Empty;
        }

        private void ReleaseReservations(
            WorldState world,
            FormalMarketOrderState order)
        {
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodMarketReservationReleased,
                ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId).HeadPersonId,
                string.Empty,
                0,
                0,
                0,
                $"Released remaining reservations for {order.Id}.");
            transaction.SourceFormalMarketOrderId = order.Id;
            transaction.SourceCountyGovernanceId = order.CountyGovernanceId;
            for (var i = 0; i < order.BatchReservations.Count; i++)
            {
                var reservation = order.BatchReservations[i];
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }
                var batch = FindBatch(world, reservation.BatchId);
                if (batch.ReservedQuantity < reservation.RemainingQuantity)
                {
                    throw new InvalidOperationException(
                        $"Formal market reservation underflow for {batch.Id}.");
                }
                batch.ReservedQuantity -= reservation.RemainingQuantity;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, -reservation.RemainingQuantity));
                reservation.RemainingQuantity = 0;
            }
            if (transaction.Lines.Count > 0)
            {
                world.InventoryTransactions.Add(transaction);
            }
        }

        private void ValidateOrderRequest(
            WorldState world,
            string countyGovernanceId,
            string familyId,
            string storageFacilityId,
            string productDefinitionId,
            long quantity,
            long unitPrice,
            int minimumQualityBasisPoints,
            long expiryDay)
        {
            RequireFormalWorld(world);
            if (quantity <= 0 || unitPrice <= 0 ||
                minimumQualityBasisPoints < 0 ||
                minimumQualityBasisPoints > 10_000 ||
                expiryDay < world.AbsoluteDay)
            {
                throw new InvalidOperationException(
                    "Formal market order parameters are invalid.");
            }
            var product = _content.GetProduct(productDefinitionId);
            if (!product.CategoryTags.Contains("product.market"))
            {
                throw new InvalidOperationException(
                    $"Formal county market does not support {productDefinitionId}.");
            }
            var governance = FindGovernance(world, countyGovernanceId);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            var storage = ProductInventorySystem.FindFacility(
                world, storageFacilityId);
            if (storage.Kind != VillageFacilityKind.HouseholdGranary ||
                storage.OwnerFamilyId != family.Id ||
                !FamilyBelongsToCounty(
                    world, familyId, governance.CountyLocationId) ||
                storage.InventoryUnits != ProductInventorySystem
                    .CalculatePhysicalInventoryUnits(
                        world, storage.Id, family.Id, _content))
            {
                throw new InvalidOperationException(
                    "Formal market order requires a consistent household granary in the county.");
            }
        }

        private void RequireFormalWorld(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                throw new InvalidOperationException(
                    "Formal county market requires formal food inventory authority.");
            }
        }

        private FormalMarketPriceState EnsurePrice(
            WorldState world,
            string countyGovernanceId,
            string productDefinitionId)
        {
            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                var existing = world.FormalMarketPrices[i];
                if (existing.CountyGovernanceId == countyGovernanceId &&
                    existing.ProductDefinitionId == productDefinitionId)
                {
                    return existing;
                }
            }
            var governance = FindGovernance(world, countyGovernanceId);
            var basePrice = Math.Max(
                1L, FindLocation(world, governance.CountyLocationId).GrainPrice);
            long equilibrium;
            if (_content.TryGetFood(productDefinitionId, out var food))
            {
                equilibrium = Math.Max(
                    1L, basePrice * food.MarketValueBasisPoints / 10_000L);
            }
            else
            {
                var product = _content.GetProduct(productDefinitionId);
                equilibrium = Math.Max(
                    1L, checked(basePrice * product.BaseWeight));
            }
            var result = new FormalMarketPriceState
            {
                Id = $"formal_market_price.{countyGovernanceId}." +
                     productDefinitionId,
                CountyGovernanceId = countyGovernanceId,
                ProductDefinitionId = productDefinitionId,
                EquilibriumUnitPrice = equilibrium,
                LastTradeUnitPrice = equilibrium
            };
            world.FormalMarketPrices.Add(result);
            return result;
        }

        private static FormalMarketOrderState NewOrder(
            WorldState world,
            string countyGovernanceId,
            string familyId,
            string storageFacilityId,
            string productDefinitionId,
            FormalMarketOrderSide side,
            long quantity,
            long unitPrice,
            int minimumQualityBasisPoints,
            long expiryDay)
        {
            return new FormalMarketOrderState
            {
                Id = $"formal_market_order.{world.AbsoluteDay}." +
                     $"{world.FormalMarketOrders.Count:D6}",
                CountyGovernanceId = countyGovernanceId,
                OwnerFamilyId = familyId,
                StorageFacilityId = storageFacilityId,
                ProductDefinitionId = productDefinitionId,
                Side = side,
                Status = FormalMarketOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                ExpiryDay = expiryDay,
                OriginalQuantity = quantity,
                RemainingQuantity = quantity,
                UnitPrice = unitPrice,
                MinimumQualityBasisPoints = minimumQualityBasisPoints,
                CloseReason = string.Empty
            };
        }

        private static FormalMarketOrderState FindBestSell(
            WorldState world,
            FormalMarketOrderState buy)
        {
            FormalMarketOrderState best = null;
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                var candidate = world.FormalMarketOrders[i];
                if (candidate.Status != FormalMarketOrderStatus.Active ||
                    candidate.Side != FormalMarketOrderSide.Sell ||
                    candidate.CountyGovernanceId != buy.CountyGovernanceId ||
                    candidate.ProductDefinitionId != buy.ProductDefinitionId ||
                    candidate.OwnerFamilyId == buy.OwnerFamilyId ||
                    candidate.UnitPrice > buy.UnitPrice ||
                    candidate.RemainingQuantity <= 0 ||
                    !HasRequiredQuality(world, candidate, buy))
                {
                    continue;
                }
                if (best == null || CompareSellOrders(candidate, best) < 0)
                {
                    best = candidate;
                }
            }
            return best;
        }

        private static bool HasRequiredQuality(
            WorldState world,
            FormalMarketOrderState sell,
            FormalMarketOrderState buy)
        {
            var hasRemaining = false;
            for (var i = 0; i < sell.BatchReservations.Count; i++)
            {
                var reservation = sell.BatchReservations[i];
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }
                hasRemaining = true;
                if (FindBatch(world, reservation.BatchId)
                        .QualityBasisPoints < buy.MinimumQualityBasisPoints)
                {
                    return false;
                }
            }
            return hasRemaining;
        }

        private static List<FormalMarketOrderState> ActiveOrders(WorldState world)
        {
            var result = new List<FormalMarketOrderState>();
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                if (world.FormalMarketOrders[i].Status ==
                    FormalMarketOrderStatus.Active)
                {
                    result.Add(world.FormalMarketOrders[i]);
                }
            }
            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static int CompareBuyOrders(
            FormalMarketOrderState left,
            FormalMarketOrderState right)
        {
            var price = right.UnitPrice.CompareTo(left.UnitPrice);
            if (price != 0)
            {
                return price;
            }
            var day = left.CreatedDay.CompareTo(right.CreatedDay);
            return day != 0 ? day : string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareSellOrders(
            FormalMarketOrderState left,
            FormalMarketOrderState right)
        {
            var price = left.UnitPrice.CompareTo(right.UnitPrice);
            if (price != 0)
            {
                return price;
            }
            var day = left.CreatedDay.CompareTo(right.CreatedDay);
            return day != 0 ? day : string.CompareOrdinal(left.Id, right.Id);
        }

        private static int CompareSellBatches(
            ProductBatchState left,
            ProductBatchState right)
        {
            var produced = left.ProducedDay.CompareTo(right.ProducedDay);
            return produced != 0
                ? produced
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static FormalMarketOrderState FindOrder(
            WorldState world,
            string orderId)
        {
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                if (world.FormalMarketOrders[i].Id == orderId)
                {
                    return world.FormalMarketOrders[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown formal market order {orderId}.");
        }

        private static ProductBatchState FindBatch(
            WorldState world,
            string batchId)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == batchId)
                {
                    return world.ProductBatches[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown product batch {batchId}.");
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world,
            string governanceId)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].Id == governanceId)
                {
                    return world.CountyGovernances[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown county governance {governanceId}.");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown location {locationId}.");
        }

        private static bool FamilyBelongsToCounty(
            WorldState world,
            string familyId,
            string countyLocationId)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].ParentLocationId == countyLocationId &&
                    world.Villages[i].HouseholdIds.Contains(familyId))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class FormalMarketDailyCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.formal_market.resolve_daily";
        public const string IssuerId = "system.formal_county_market";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string TransactionKindId =
            "mandate.transaction.formal_market.resolve_daily";
        public const string EventTypeId =
            "mandate.event.formal_market.daily_resolved";
        public const string ProjectionHandlerId =
            "mandate.handler.formal_market.daily_projection";

        private readonly FormalCountyMarketSystem _market;

        public FormalMarketDailyCommandScheduler(
            FormalCountyMarketSystem market)
        {
            _market = market ?? throw new ArgumentNullException(nameof(market));
        }

        public bool EnsureDueCommand(
            WorldState world,
            WorldCommandRuntime commandRuntime)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (commandRuntime == null)
            {
                throw new ArgumentNullException(nameof(commandRuntime));
            }
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                !_market.HasDailyWork(world))
            {
                return false;
            }

            var commandId = DailyCommandId(world.AbsoluteDay);
            for (var i = 0; i < world.PersistentWorldCommands.Count; i++)
            {
                if (world.PersistentWorldCommands[i].Id == commandId)
                {
                    return false;
                }
            }

            commandRuntime.Enqueue(
                world,
                new WorldCommandEnvelope(
                    commandId,
                    CommandTypeId,
                    IssuerId,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment,
                    40,
                    new Dictionary<string, string>
                    {
                        {
                            ExpectedDayArgumentId,
                            world.AbsoluteDay.ToString(
                                System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }));
            return true;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new FormalMarketDailyCommandHandler(_market);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new FormalMarketDailyProjectionHandler();

        public static string DailyCommandId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "formal_market.daily_command.{0:D10}",
            day);

        public static string DailyTransactionId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "formal_market.daily_transaction.{0:D10}",
            day);

        public static string DailyEventId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "formal_market.daily_resolved.{0:D10}",
            day);

        private sealed class FormalMarketDailyCommandHandler :
            IWorldCommandHandler
        {
            private readonly FormalCountyMarketSystem _market;

            public FormalMarketDailyCommandHandler(
                FormalCountyMarketSystem market)
            {
                _market = market;
            }

            public string CommandTypeId =>
                FormalMarketDailyCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 1 ||
                    !command.Arguments.TryGetValue(
                        ExpectedDayArgumentId,
                        out var expectedDayText) ||
                    !long.TryParse(
                        expectedDayText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var expectedDay) ||
                    expectedDay < 0)
                {
                    throw new InvalidOperationException(
                        "Formal market daily command arguments are invalid.");
                }

                transactions.Add(new FormalMarketDailyTransaction(
                    _market,
                    expectedDay));
            }
        }

        private sealed class FormalMarketDailyTransaction : IWorldTransaction
        {
            private readonly FormalCountyMarketSystem _market;
            private readonly long _expectedDay;

            public FormalMarketDailyTransaction(
                FormalCountyMarketSystem market,
                long expectedDay)
            {
                _market = market;
                _expectedDay = expectedDay;
                Id = DailyTransactionId(expectedDay);
            }

            public string Id { get; }

            public string KindId => TransactionKindId;

            public int Priority => 40;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _market.ValidateDailyResolution(world, _expectedDay);
                validation.Reserve(
                    "formal_market.daily_resolution." +
                        _expectedDay.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _market.ResolveDaily(world);
                events.Add(new WorldRuntimeEvent(
                    DailyEventId(_expectedDay),
                    EventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
            }
        }

        private sealed class FormalMarketDailyProjectionHandler :
            IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;

            public string EventTypeId =>
                FormalMarketDailyCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // The market transaction already owns all business writes.
                // This consumer only establishes the committed projection boundary.
            }
        }
    }
}
