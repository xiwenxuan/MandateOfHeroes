using System;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class TradeResult
    {
        public bool Success { get; }
        public long MoneyChange { get; }
        public long RealizedProfit { get; }
        public string Message { get; }

        public TradeResult(
            bool success,
            long moneyChange,
            long realizedProfit,
            string message)
        {
            Success = success;
            MoneyChange = moneyChange;
            RealizedProfit = realizedProfit;
            Message = message ?? string.Empty;
        }
    }

    public sealed class TradingSystem
    {
        public TradeResult Buy(
            WorldState world,
            StableId personId,
            StableId commodityId,
            int quantity)
        {
            var commonError = ValidateCommon(
                world, personId.Value, commodityId.Value, quantity);
            if (commonError != null)
            {
                return commonError;
            }

            var person = FindPerson(world, personId.Value);
            var commodity = FindCommodity(world, commodityId.Value);
            var listing = FindListing(world, person.LocationId, commodityId.Value);
            if (listing.Stock < quantity)
            {
                return Failure("市场存货不足。");
            }

            var cost = checked((long)listing.Price * quantity);
            if (person.Wealth < cost)
            {
                return Failure("资金不足，无法买入。");
            }

            var currentWeight = CalculateCargoWeight(world, person.Id);
            var addedWeight = checked((long)commodity.UnitWeight * quantity);
            if (currentWeight + addedWeight > person.CargoCapacity)
            {
                return Failure("货物超过人物的载货上限。");
            }

            if (TryGetFormalProductId(commodity, out var productId) &&
                !string.IsNullOrEmpty(person.FamilyId))
            {
                AddFormalCargo(
                    world,
                    person,
                    productId,
                    quantity,
                    listing.Price);
            }
            else
            {
                var stack = FindInventory(world, person.Id, commodity.Id);
                if (stack == null)
                {
                    stack = new InventoryStackState
                    {
                        Id = $"inventory.{person.Id}.{commodity.Id}",
                        OwnerPersonId = person.Id,
                        CommodityId = commodity.Id,
                        Quantity = quantity,
                        AverageUnitCost = listing.Price
                    };
                    world.Inventories.Add(stack);
                }
                else
                {
                    var totalCost =
                        (long)stack.AverageUnitCost * stack.Quantity + cost;
                    stack.Quantity = checked(stack.Quantity + quantity);
                    stack.AverageUnitCost = checked(
                        (int)(totalCost / stack.Quantity));
                }
            }

            var unitPrice = listing.Price;
            person.Wealth -= cost;
            listing.Stock -= quantity;
            ApplyImmediatePriceImpact(listing, quantity, true);
            SyncLegacyGrainPrice(world, listing);
            AddRecord(
                world, person, commodity, quantity, unitPrice, true, -cost);
            world.Validate();
            return new TradeResult(
                true,
                -cost,
                0,
                $"买入{quantity}单位{commodity.DisplayName}，支出{cost}钱。");
        }

        public TradeResult Sell(
            WorldState world,
            StableId personId,
            StableId commodityId,
            int quantity)
        {
            var commonError = ValidateCommon(
                world, personId.Value, commodityId.Value, quantity);
            if (commonError != null)
            {
                return commonError;
            }

            var person = FindPerson(world, personId.Value);
            if (HasActiveMerchantFreight(world, person.Id))
            {
                return Failure("正式商旅货物必须通过到站交付结算。");
            }
            var commodity = FindCommodity(world, commodityId.Value);
            var listing = FindListing(world, person.LocationId, commodityId.Value);
            var stack = FindInventory(world, person.Id, commodity.Id);
            var formalQuantity = GetFormalQuantity(
                world, person, commodity.Id);
            var legacyQuantity = stack == null ? 0 : stack.Quantity;
            if (formalQuantity + legacyQuantity < quantity)
            {
                return Failure("携带货物不足，无法卖出。");
            }

            var unitPrice = listing.Price;
            var revenue = checked((long)unitPrice * quantity);
            var averageUnitCost = stack != null && stack.Quantity > 0
                ? stack.AverageUnitCost
                : FindLastPurchaseUnitPrice(
                    world, person.Id, commodity.Id, unitPrice);
            var profit = checked(
                (long)(unitPrice - averageUnitCost) * quantity);
            person.Wealth = checked(person.Wealth + revenue);
            listing.Stock = checked(listing.Stock + quantity);
            var formalSold = Math.Min(formalQuantity, quantity);
            if (formalSold > 0)
            {
                ConsumeFormalCargo(
                    world,
                    person,
                    commodity.Id,
                    formalSold,
                    InventoryTransactionType.MerchantMarketSold,
                    "Sold merchant cargo into the local market.");
            }
            var legacySold = quantity - formalSold;
            if (legacySold > 0)
            {
                stack.Quantity -= legacySold;
                if (stack.Quantity == 0)
                {
                    world.Inventories.Remove(stack);
                }
            }

            ApplyImmediatePriceImpact(listing, quantity, false);
            SyncLegacyGrainPrice(world, listing);
            AddRecord(
                world, person, commodity, quantity, unitPrice, false, revenue);
            world.Validate();
            return new TradeResult(
                true,
                revenue,
                profit,
                $"卖出{quantity}单位{commodity.DisplayName}，收入{revenue}钱，" +
                $"本批盈亏{profit}钱。");
        }

        public int GetQuantity(
            WorldState world,
            string personId,
            string commodityId)
        {
            var person = FindPerson(world, personId);
            var stack = FindInventory(world, personId, commodityId);
            return checked(
                (stack == null ? 0 : stack.Quantity) +
                GetFormalQuantity(world, person, commodityId));
        }

        public long GetCargoWeight(WorldState world, string personId)
        {
            return CalculateCargoWeight(world, personId);
        }

        public bool LoseCargo(
            WorldState world,
            string personId,
            string commodityId,
            int quantity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var person = FindPerson(world, personId);
            if (HasActiveMerchantFreight(world, person.Id))
            {
                return false;
            }
            if (GetQuantity(world, personId, commodityId) < quantity)
            {
                return false;
            }
            var formalQuantity = GetFormalQuantity(
                world, person, commodityId);
            var formalLoss = Math.Min(formalQuantity, quantity);
            if (formalLoss > 0)
            {
                ConsumeFormalCargo(
                    world,
                    person,
                    commodityId,
                    formalLoss,
                    InventoryTransactionType.MerchantCargoDamaged,
                    "Merchant cargo was lost or damaged during travel.");
            }

            var remaining = quantity - formalLoss;
            if (remaining > 0)
            {
                var stack = FindInventory(world, personId, commodityId);
                if (stack == null || stack.Quantity < remaining)
                {
                    return false;
                }
                stack.Quantity -= remaining;
                if (stack.Quantity == 0)
                {
                    world.Inventories.Remove(stack);
                }
            }

            world.Validate();
            return true;
        }

        internal bool LoseMerchantFreightCargo(WorldState world,
            CivilianFreightState freight, string commodityId, int quantity,
            out InventoryTransactionState transaction)
        {
            transaction = null;
            if (world == null || freight == null || quantity <= 0 ||
                freight.PurposeId !=
                    CivilianFreightPurposeIds.MerchantOwnerCarriage ||
                freight.RemainingCargoQuantity < quantity)
                return false;
            var person = FindPerson(world, freight.CarrierPersonId);
            var commodity = FindCommodity(world, commodityId);
            if (!TryGetFormalProductId(commodity, out var productId) ||
                productId != freight.ProductDefinitionId)
                return false;
            transaction = ConsumeFormalCargo(
                world,
                person,
                commodityId,
                quantity,
                InventoryTransactionType.CivilianFreightNaturalLoss,
                "Merchant-owned formal freight was lost during travel.",
                freight.Id,
                freight.DispatchInventoryTransactionId);
            return true;
        }

        internal TradeResult SellMerchantFreightCargo(WorldState world,
            CivilianFreightState freight, string commodityId, int quantity,
            out InventoryTransactionState transaction)
        {
            transaction = null;
            if (world == null || freight == null || quantity <= 0 ||
                freight.PurposeId !=
                    CivilianFreightPurposeIds.MerchantOwnerCarriage ||
                freight.Status != CivilianFreightStatus.AwaitingReceipt ||
                freight.RemainingCargoQuantity < quantity)
                return Failure("正式商旅货物尚不可交付。");
            var person = FindPerson(world, freight.CarrierPersonId);
            var commodity = FindCommodity(world, commodityId);
            var listing = FindListing(
                world, person.LocationId, commodityId);
            if (!TryGetFormalProductId(commodity, out var productId) ||
                productId != freight.ProductDefinitionId)
                return Failure("交付商品与正式商旅货物不一致。");
            var batches = FindFormalCargoBatches(
                world,
                person,
                productId,
                freight.DispatchInventoryTransactionId);
            if (batches.Sum(item => item.Quantity) < quantity)
                return Failure("正式商旅货物数量不足。");

            var unitPrice = listing.Price;
            var revenue = checked((long)unitPrice * quantity);
            var profit = checked(
                (long)(unitPrice - freight.GoodsUnitPrice) * quantity);
            person.Wealth = checked(person.Wealth + revenue);
            listing.Stock = checked(listing.Stock + quantity);
            transaction = ConsumeFormalCargo(
                world,
                person,
                commodityId,
                quantity,
                InventoryTransactionType.MerchantMarketSold,
                "Merchant-owned formal freight sold into destination market.",
                freight.Id,
                freight.DispatchInventoryTransactionId);
            ApplyImmediatePriceImpact(listing, quantity, false);
            SyncLegacyGrainPrice(world, listing);
            AddRecord(
                world,
                person,
                commodity,
                quantity,
                unitPrice,
                false,
                revenue);
            return new TradeResult(
                true,
                revenue,
                profit,
                $"卖出{quantity}单位{commodity.DisplayName}，收入{revenue}钱，" +
                $"本批盈亏{profit}钱。");
        }

        private static TradeResult ValidateCommon(
            WorldState world,
            string personId,
            string commodityId,
            int quantity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (quantity <= 0)
            {
                return Failure("交易数量必须大于零。");
            }

            var person = FindPerson(world, personId);
            if (!person.IsAlive)
            {
                return Failure("已故人物不能交易。");
            }

            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return Failure("旅途中不能进入市场交易。");
                }
            }

            _ = FindCommodity(world, commodityId);
            _ = FindListing(world, person.LocationId, commodityId);
            return null;
        }

        private static void AddRecord(
            WorldState world,
            PersonState person,
            CommodityState commodity,
            int quantity,
            int unitPrice,
            bool isPurchase,
            long moneyChange)
        {
            var side = isPurchase ? "buy" : "sell";
            world.TradeRecords.Add(new TradeRecordState
            {
                Id =
                    $"trade_record.{world.AbsoluteDay}.{person.Id}." +
                    $"{commodity.Id}.{side}.{world.TradeRecords.Count}",
                Day = world.AbsoluteDay,
                PersonId = person.Id,
                LocationId = person.LocationId,
                CommodityId = commodity.Id,
                Quantity = quantity,
                UnitPrice = unitPrice,
                IsPurchase = isPurchase,
                MoneyChange = moneyChange
            });
        }

        private static void ApplyImmediatePriceImpact(
            MarketListingState listing,
            int quantity,
            bool purchase)
        {
            var impact = Math.Max(
                1,
                checked(listing.EquilibriumPrice * quantity) /
                Math.Max(1, listing.TargetStock * 4));
            listing.Price = Math.Max(
                1,
                listing.Price + (purchase ? impact : -impact));
        }

        private static long CalculateCargoWeight(WorldState world, string personId)
        {
            long weight = 0;
            for (var i = 0; i < world.Inventories.Count; i++)
            {
                var stack = world.Inventories[i];
                if (stack.OwnerPersonId != personId)
                {
                    continue;
                }

                var commodity = FindCommodity(world, stack.CommodityId);
                weight = checked(
                    weight + (long)stack.Quantity * commodity.UnitWeight);
            }

            for (var containerIndex = 0;
                 containerIndex < world.InventoryContainers.Count;
                 containerIndex++)
            {
                var container = world.InventoryContainers[containerIndex];
                if (container.CarrierPersonId != personId ||
                    container.KindId !=
                        "inventory_container.merchant_caravan")
                {
                    continue;
                }
                for (var batchIndex = 0;
                     batchIndex < world.ProductBatches.Count;
                     batchIndex++)
                {
                    var batch = world.ProductBatches[batchIndex];
                    if (batch.InventoryContainerId == container.Id)
                    {
                        weight = checked(
                            weight + batch.Quantity * batch.UnitWeight);
                    }
                }
            }

            return weight;
        }

        private static void AddFormalCargo(
            WorldState world,
            PersonState person,
            string productDefinitionId,
            int quantity,
            int unitPrice)
        {
            var content = ProductionContentRegistry.CreateCore();
            content.ValidateManifest(world.ProductionContentManifest);
            var product = content.GetProduct(productDefinitionId);
            var family = ProductInventorySystem.FindFamily(
                world, person.FamilyId);
            var container = EnsureMerchantContainer(world, person);
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.MerchantMarketPurchased,
                person.Id,
                string.Empty,
                0,
                0,
                0,
                $"Purchased {quantity} {product.Id} at {unitPrice} per unit.");
            var batch = ProductInventorySystem.NewFamilyContainerBatch(
                world,
                product,
                family,
                container,
                transaction.Id,
                string.Empty,
                quantity,
                8_000);
            transaction.Lines.Add(ProductInventorySystem.Line(
                batch, quantity, 0));
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(transaction);
        }

        private static InventoryTransactionState ConsumeFormalCargo(
            WorldState world,
            PersonState person,
            string commodityId,
            int quantity,
            InventoryTransactionType type,
            string summary,
            string sourceCivilianFreightId = null,
            string requiredSourceTransactionId = null)
        {
            var commodity = FindCommodity(world, commodityId);
            if (!TryGetFormalProductId(commodity, out var productId))
            {
                throw new InvalidOperationException(
                    "The commodity has no formal product mapping.");
            }

            var batches = FindFormalCargoBatches(
                world, person, productId, requiredSourceTransactionId);
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                type,
                person.Id,
                string.Empty,
                0,
                0,
                0,
                summary);
            transaction.SourceCivilianFreightId =
                sourceCivilianFreightId ?? string.Empty;
            long remaining = quantity;
            for (var i = 0; i < batches.Count && remaining > 0; i++)
            {
                var consumed = Math.Min(remaining, batches[i].Quantity);
                batches[i].Quantity -= consumed;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batches[i], -consumed, 0));
                remaining -= consumed;
            }
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    "Formal merchant cargo changed before settlement.");
            }
            world.InventoryTransactions.Add(transaction);
            return transaction;
        }

        private static InventoryContainerState EnsureMerchantContainer(
            WorldState world,
            PersonState person)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                var existing = world.InventoryContainers[i];
                if (existing.CarrierPersonId == person.Id &&
                    existing.OwnerFamilyId == person.FamilyId &&
                    existing.KindId == "inventory_container.merchant_caravan")
                {
                    existing.LocationId = person.LocationId;
                    existing.CapacityWeight = Math.Max(
                        existing.CapacityWeight, person.CargoCapacity);
                    return existing;
                }
            }

            var container = new InventoryContainerState
            {
                Id = "inventory_container.merchant_caravan." + person.Id,
                KindId = "inventory_container.merchant_caravan",
                OwnerFamilyId = person.FamilyId,
                CarrierPersonId = person.Id,
                LocationId = person.LocationId,
                CapacityWeight = Math.Max(1, person.CargoCapacity),
                FoodStorageEnvironmentId =
                    "storage.environment.generic_sheltered",
                FoodStorageProtectionBasisPoints = 2_000
            };
            world.InventoryContainers.Add(container);
            return container;
        }

        private static int GetFormalQuantity(
            WorldState world,
            PersonState person,
            string commodityId)
        {
            if (string.IsNullOrEmpty(person.FamilyId) ||
                !TryGetFormalProductId(
                    FindCommodity(world, commodityId), out var productId))
            {
                return 0;
            }

            long quantity = 0;
            var batches = FindFormalCargoBatches(world, person, productId);
            for (var i = 0; i < batches.Count; i++)
            {
                quantity = checked(quantity + batches[i].Quantity);
            }
            return checked((int)quantity);
        }

        private static System.Collections.Generic.List<ProductBatchState>
            FindFormalCargoBatches(
                WorldState world,
                PersonState person,
                string productId,
                string requiredSourceTransactionId = null)
        {
            var containerIds = new System.Collections.Generic.HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                var container = world.InventoryContainers[i];
                if (container.CarrierPersonId == person.Id &&
                    container.OwnerFamilyId == person.FamilyId &&
                    container.KindId ==
                        "inventory_container.merchant_caravan")
                {
                    containerIds.Add(container.Id);
                }
            }

            var result = new System.Collections.Generic.List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == person.FamilyId &&
                    batch.ProductDefinitionId == productId &&
                    batch.Quantity > 0 &&
                    (string.IsNullOrEmpty(requiredSourceTransactionId) ||
                     batch.SourceTransactionId ==
                        requiredSourceTransactionId) &&
                    containerIds.Contains(batch.InventoryContainerId))
                {
                    result.Add(batch);
                }
            }
            result.Sort((left, right) =>
            {
                var day = left.ProducedDay.CompareTo(right.ProducedDay);
                return day != 0
                    ? day
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static int FindLastPurchaseUnitPrice(
            WorldState world,
            string personId,
            string commodityId,
            int fallback)
        {
            for (var i = world.TradeRecords.Count - 1; i >= 0; i--)
            {
                var record = world.TradeRecords[i];
                if (record.PersonId == personId &&
                    record.CommodityId == commodityId &&
                    record.IsPurchase)
                {
                    return record.UnitPrice;
                }
            }
            return fallback;
        }

        private static bool TryGetFormalProductId(
            CommodityState commodity,
            out string productId)
        {
            if (commodity != null &&
                !string.IsNullOrEmpty(commodity.ProductDefinitionId))
            {
                productId = commodity.ProductDefinitionId;
                return true;
            }
            productId = string.Empty;
            return false;
        }

        private static bool HasActiveMerchantFreight(
            WorldState world, string personId) =>
            world.CivilianFreights.Any(item =>
                item.CarrierPersonId == personId &&
                item.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage &&
                item.Status != CivilianFreightStatus.Completed);

        private static InventoryStackState FindInventory(
            WorldState world,
            string personId,
            string commodityId)
        {
            for (var i = 0; i < world.Inventories.Count; i++)
            {
                var stack = world.Inventories[i];
                if (stack.OwnerPersonId == personId &&
                    stack.CommodityId == commodityId)
                {
                    return stack;
                }
            }

            return null;
        }

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private static CommodityState FindCommodity(
            WorldState world,
            string commodityId)
        {
            for (var i = 0; i < world.Commodities.Count; i++)
            {
                if (world.Commodities[i].Id == commodityId)
                {
                    return world.Commodities[i];
                }
            }

            throw new InvalidOperationException($"Missing commodity {commodityId}.");
        }

        private static MarketListingState FindListing(
            WorldState world,
            string locationId,
            string commodityId)
        {
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == locationId &&
                    listing.CommodityId == commodityId)
                {
                    return listing;
                }
            }

            throw new InvalidOperationException(
                $"Missing market listing {locationId}/{commodityId}.");
        }

        private static void SyncLegacyGrainPrice(
            WorldState world,
            MarketListingState listing)
        {
            if (listing.CommodityId != "commodity.grain")
            {
                return;
            }

            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == listing.LocationId)
                {
                    world.Locations[i].GrainPrice = listing.Price;
                    return;
                }
            }
        }

        private static TradeResult Failure(string message)
        {
            return new TradeResult(false, 0, 0, message);
        }
    }
}
