using System;
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
                stack.AverageUnitCost = checked((int)(totalCost / stack.Quantity));
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
            var commodity = FindCommodity(world, commodityId.Value);
            var listing = FindListing(world, person.LocationId, commodityId.Value);
            var stack = FindInventory(world, person.Id, commodity.Id);
            if (stack == null || stack.Quantity < quantity)
            {
                return Failure("携带货物不足，无法卖出。");
            }

            var unitPrice = listing.Price;
            var revenue = checked((long)unitPrice * quantity);
            var profit = checked(
                (long)(unitPrice - stack.AverageUnitCost) * quantity);
            person.Wealth = checked(person.Wealth + revenue);
            listing.Stock = checked(listing.Stock + quantity);
            stack.Quantity -= quantity;
            if (stack.Quantity == 0)
            {
                world.Inventories.Remove(stack);
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
            var stack = FindInventory(world, personId, commodityId);
            return stack == null ? 0 : stack.Quantity;
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

            return weight;
        }

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
