using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitarySupplyResult
    {
        public bool Success { get; }
        public int ProvisionsAdded { get; }
        public long MoneyChange { get; }
        public long RealizedProfit { get; }
        public string Message { get; }

        public MilitarySupplyResult(
            bool success,
            int provisionsAdded,
            long moneyChange,
            long realizedProfit,
            string message)
        {
            Success = success;
            ProvisionsAdded = provisionsAdded;
            MoneyChange = moneyChange;
            RealizedProfit = realizedProfit;
            Message = message ?? string.Empty;
        }
    }

    public sealed class MilitarySupplySystem
    {
        public const int ProvisionsPerGrainUnit = 10;

        public int ApplyTaskDelivery(
            WorldState world,
            TaskInstanceState task,
            TaskDefinitionState definition)
        {
            if (definition.ArmyProvisionReward <= 0)
            {
                return 0;
            }

            var army = FindArmy(world, definition.TargetArmyId);
            army.Provisions = checked(
                army.Provisions + definition.ArmyProvisionReward);
            AddSupplyRecord(
                world,
                MilitarySupplyType.TaskDelivery,
                army,
                task.AssigneePersonId,
                task.Id,
                definition.ArmyProvisionReward / ProvisionsPerGrainUnit,
                definition.ArmyProvisionReward,
                0,
                0,
                $"任务{definition.DisplayName}为{army.DisplayName}" +
                $"补充军粮{definition.ArmyProvisionReward}。");
            return definition.ArmyProvisionReward;
        }

        public MilitarySupplyResult SellGrainToArmy(
            WorldState world,
            StableId supplierPersonId,
            StableId armyId,
            int grainUnits)
        {
            ValidateQuantity(world, grainUnits);
            var supplier = FindPerson(world, supplierPersonId.Value);
            var army = FindArmy(world, armyId.Value);
            var invalid = ValidateArmyAccess(world, army, supplier.LocationId);
            if (invalid != null)
            {
                return invalid;
            }

            if (IsTraveling(world, supplier.Id))
            {
                return Failure("旅途中的商人不能向军队交货。");
            }

            var stack = FindInventory(
                world, supplier.Id, "commodity.grain");
            if (stack == null || stack.Quantity < grainUnits)
            {
                return Failure("商人携带的粮食不足。");
            }

            var listing = FindGrainListing(world, army.LocationId);
            var unitPrice = checked((listing.Price * 120 + 99) / 100);
            var totalPaid = checked((long)unitPrice * grainUnits);
            var organization = FindOrganization(world, army.OrganizationId);
            if (organization.Treasury < totalPaid)
            {
                return Failure("军队所属组织的财政不足。");
            }

            var averageCost = stack.AverageUnitCost;
            stack.Quantity -= grainUnits;
            if (stack.Quantity == 0)
            {
                world.Inventories.Remove(stack);
            }

            supplier.Wealth = checked(supplier.Wealth + totalPaid);
            organization.Treasury -= totalPaid;
            var provisionsAdded = checked(grainUnits * ProvisionsPerGrainUnit);
            army.Provisions = checked(army.Provisions + provisionsAdded);
            var profit = checked((long)(unitPrice - averageCost) * grainUnits);
            AddTradeRecord(
                world, supplier, grainUnits, unitPrice, totalPaid);
            AddSupplyRecord(
                world,
                MilitarySupplyType.MerchantSale,
                army,
                supplier.Id,
                string.Empty,
                grainUnits,
                provisionsAdded,
                unitPrice,
                totalPaid,
                $"{supplier.DisplayName}向{army.DisplayName}售粮{grainUnits}单位。");
            world.Validate();
            return new MilitarySupplyResult(
                true,
                provisionsAdded,
                totalPaid,
                profit,
                $"军队以每单位{unitPrice}钱收购{grainUnits}单位粮食，" +
                $"增加军粮{provisionsAdded}。");
        }

        public MilitarySupplyResult PurchaseLocalGrain(
            WorldState world,
            StableId armyId,
            int grainUnits)
        {
            ValidateQuantity(world, grainUnits);
            var army = FindArmy(world, armyId.Value);
            var invalid = ValidateArmyAccess(world, army, army.LocationId);
            if (invalid != null)
            {
                return invalid;
            }

            var listing = FindGrainListing(world, army.LocationId);
            if (listing.Stock < grainUnits)
            {
                return Failure("当地市场粮食库存不足。");
            }

            var organization = FindOrganization(world, army.OrganizationId);
            var totalPaid = checked((long)listing.Price * grainUnits);
            if (organization.Treasury < totalPaid)
            {
                return Failure("军队所属组织的财政不足。");
            }

            var unitPrice = listing.Price;
            organization.Treasury -= totalPaid;
            listing.Stock -= grainUnits;
            listing.Price = checked(
                listing.Price +
                Math.Max(
                    1,
                    listing.EquilibriumPrice * grainUnits /
                    Math.Max(1, listing.TargetStock * 4)));
            SyncLegacyGrainPrice(world, listing);
            var provisionsAdded = checked(grainUnits * ProvisionsPerGrainUnit);
            army.Provisions = checked(army.Provisions + provisionsAdded);
            AddSupplyRecord(
                world,
                MilitarySupplyType.LocalMarketPurchase,
                army,
                string.Empty,
                string.Empty,
                grainUnits,
                provisionsAdded,
                unitPrice,
                totalPaid,
                $"{army.DisplayName}从当地市场购粮{grainUnits}单位。");
            world.Validate();
            return new MilitarySupplyResult(
                true,
                provisionsAdded,
                -totalPaid,
                0,
                $"军队购粮成功，增加军粮{provisionsAdded}。");
        }

        private static MilitarySupplyResult ValidateArmyAccess(
            WorldState world,
            ArmyState army,
            string supplierLocationId)
        {
            if (!army.IsMobilized || army.Troops <= 0)
            {
                return Failure("只有已动员且尚有兵力的军队能够接收补给。");
            }

            if (army.LocationId != supplierLocationId)
            {
                return Failure("商人与军队不在同一地点。");
            }

            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == army.Id)
                {
                    return Failure("行军中的军队暂时无法进行大宗补给。");
                }
            }

            return null;
        }

        private static void ValidateQuantity(WorldState world, int grainUnits)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (grainUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(grainUnits));
            }

            world.Validate();
        }

        private static void AddTradeRecord(
            WorldState world,
            PersonState supplier,
            int grainUnits,
            int unitPrice,
            long totalPaid)
        {
            world.TradeRecords.Add(new TradeRecordState
            {
                Id =
                    $"trade_record.{world.AbsoluteDay}.{supplier.Id}." +
                    $"commodity.grain.military_sell.{world.TradeRecords.Count}",
                Day = world.AbsoluteDay,
                PersonId = supplier.Id,
                LocationId = supplier.LocationId,
                CommodityId = "commodity.grain",
                Quantity = grainUnits,
                UnitPrice = unitPrice,
                IsPurchase = false,
                MoneyChange = totalPaid
            });
        }

        private static void AddSupplyRecord(
            WorldState world,
            MilitarySupplyType type,
            ArmyState army,
            string supplierPersonId,
            string sourceTaskInstanceId,
            int grainUnits,
            int provisionsAdded,
            int unitPrice,
            long totalPaid,
            string summary)
        {
            world.MilitarySupplies.Add(new MilitarySupplyRecordState
            {
                Id =
                    $"military_supply.{world.AbsoluteDay}.{army.Id}." +
                    $"{type.ToString().ToLowerInvariant()}." +
                    $"{world.MilitarySupplies.Count}",
                Day = world.AbsoluteDay,
                Type = type,
                ArmyId = army.Id,
                SupplierPersonId = supplierPersonId,
                SourceTaskInstanceId = sourceTaskInstanceId,
                GrainUnits = grainUnits,
                ProvisionsAdded = provisionsAdded,
                UnitPrice = unitPrice,
                TotalPaid = totalPaid,
                Summary = summary
            });
        }

        private static bool IsTraveling(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return true;
                }
            }

            return false;
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

        private static ArmyState FindArmy(WorldState world, string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
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

        private static MarketListingState FindGrainListing(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == locationId &&
                    listing.CommodityId == "commodity.grain")
                {
                    return listing;
                }
            }

            throw new InvalidOperationException(
                $"Missing grain market at {locationId}.");
        }

        private static void SyncLegacyGrainPrice(
            WorldState world,
            MarketListingState listing)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == listing.LocationId)
                {
                    world.Locations[i].GrainPrice = listing.Price;
                    return;
                }
            }
        }

        private static MilitarySupplyResult Failure(string message)
        {
            return new MilitarySupplyResult(false, 0, 0, 0, message);
        }
    }
}
