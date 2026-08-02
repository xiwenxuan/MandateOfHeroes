using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryEquipmentRepairSystem
    {
        public const string PrototypeWorkshopContainerId =
            "inventory_container.zhongshan_merchants.workshop_store";
        public const string PrototypeWorkshopSiteId =
            "production_site.zhongshan_merchants.armory_workshop";
        public const string WorkshopContainerKindId =
            "inventory_container_kind.workshop_store";
        public const string WorkshopSiteKindId =
            "production_site_kind.integrated_armory_workshop";

        public static void InitializePrototypeWorkshop(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (FindSite(world, PrototypeWorkshopSiteId, false) != null)
            {
                return;
            }

            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = PrototypeWorkshopContainerId,
                KindId = WorkshopContainerKindId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                LocationId = "location.zhongshan",
                CapacityWeight = 10_000
            });
            world.ProductionSites.Add(new ProductionSiteState
            {
                Id = PrototypeWorkshopSiteId,
                KindId = WorkshopSiteKindId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                LocationId = "location.zhongshan",
                ManagerPersonId = "person.su_shuang",
                InventoryContainerId = PrototypeWorkshopContainerId,
                ConcurrentOrderCapacity = 4,
                ConditionBasisPoints = 8_500,
                FacilityTags = new List<string>
                {
                    CoreProductionContent.BlacksmithFacilityTag,
                    CoreProductionContent.WoodworkingFacilityTag,
                    CoreProductionContent.BowmakingFacilityTag,
                    CoreProductionContent.ArmoringFacilityTag
                }
            });
            AddOpeningMaterial(
                world, CoreProductionContent.IronMaterialProductId, 150);
            AddOpeningMaterial(
                world, CoreProductionContent.TimberMaterialProductId, 220);
            AddOpeningMaterial(
                world, CoreProductionContent.LeatherMaterialProductId, 80);
            AddOpeningMaterial(
                world, CoreProductionContent.HornMaterialProductId, 60);
        }

        public MilitaryEquipmentRepairOrderState CreateOrder(
            WorldState world,
            string armyId,
            string equipmentDefinitionId,
            string productionSiteId,
            string managerPersonId,
            ProductionControlMode controlMode,
            int quantity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (quantity <= 0 ||
                !Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var army = FindArmy(world, armyId);
            var definition = FindDefinition(world, equipmentDefinitionId);
            var stock = FindStock(world, army.Id, definition.Id);
            var site = FindSite(world, productionSiteId, true);
            var container = ProcessingProductionSystem.FindContainer(
                world, site.InventoryContainerId);
            var manager = ProductInventorySystem.FindPerson(
                world, managerPersonId);
            if (stock.DamagedQuantity - stock.ReservedDamagedQuantity < quantity ||
                army.LocationId != site.LocationId ||
                site.ManagerPersonId != manager.Id ||
                !manager.IsAlive || manager.LocationId != site.LocationId ||
                container.LocationId != site.LocationId ||
                container.OwnerOrganizationId != site.OwnerOrganizationId ||
                !ProcessingProductionSystem.HasMembership(
                    world, manager.Id, site.OwnerOrganizationId) ||
                !site.FacilityTags.Contains(definition.RepairFacilityTag) ||
                ActiveOrdersAtSite(world, site.Id) >=
                    site.ConcurrentOrderCapacity)
            {
                throw new InvalidOperationException(
                    "Equipment repair requires damaged stock, a co-located army, and a compatible staffed workshop.");
            }

            var reservations = BuildMaterialReservations(
                world,
                container,
                definition.RepairMaterialProductDefinitionId,
                checked((long)definition.RepairMaterialQuantityPerUnit *
                    quantity));
            var order = new MilitaryEquipmentRepairOrderState
            {
                Id = $"equipment_repair.{world.AbsoluteDay}." +
                     $"{world.MilitaryEquipmentRepairOrders.Count:D6}",
                ArmyId = army.Id,
                EquipmentDefinitionId = definition.Id,
                ProductionSiteId = site.Id,
                InventoryContainerId = container.Id,
                ManagerPersonId = manager.Id,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                FinishDay = checked(world.AbsoluteDay +
                    (long)definition.RepairDurationDays * quantity),
                Quantity = quantity,
                MaterialReservations = reservations
            };
            var transaction = NewRepairInventoryTransaction(
                world,
                InventoryTransactionType.EquipmentRepairReserved,
                order,
                "Reserved material for damaged equipment repair.");
            for (var i = 0; i < reservations.Count; i++)
            {
                var batch = FindBatch(world, reservations[i].BatchId);
                batch.ReservedQuantity = checked(
                    batch.ReservedQuantity + reservations[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, reservations[i].Quantity));
            }

            stock.ReservedDamagedQuantity = checked(
                stock.ReservedDamagedQuantity + quantity);
            world.MilitaryEquipmentRepairOrders.Add(order);
            world.InventoryTransactions.Add(transaction);
            world.Validate();
            return order;
        }

        public void ResolveDueOrders(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var due = new List<MilitaryEquipmentRepairOrderState>();
            for (var i = 0; i < world.MilitaryEquipmentRepairOrders.Count; i++)
            {
                var order = world.MilitaryEquipmentRepairOrders[i];
                if (order.Status == ProductionOrderStatus.Active &&
                    order.FinishDay <= world.AbsoluteDay &&
                    CanSettle(world, order))
                {
                    due.Add(order);
                }
            }

            due.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < due.Count; i++)
            {
                Settle(world, due[i]);
            }

            if (due.Count > 0)
            {
                world.Validate();
            }
        }

        private static void Settle(
            WorldState world,
            MilitaryEquipmentRepairOrderState order)
        {
            var stock = FindStock(
                world, order.ArmyId, order.EquipmentDefinitionId);
            var transaction = NewRepairInventoryTransaction(
                world,
                InventoryTransactionType.EquipmentRepairSettled,
                order,
                "Consumed material and completed equipment repair.");
            for (var i = 0; i < order.MaterialReservations.Count; i++)
            {
                var reservation = order.MaterialReservations[i];
                var batch = FindBatch(world, reservation.BatchId);
                if (batch.Quantity < reservation.Quantity ||
                    batch.ReservedQuantity < reservation.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Repair material batch {batch.Id} is no longer available.");
                }

                batch.Quantity -= reservation.Quantity;
                batch.ReservedQuantity -= reservation.Quantity;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, -reservation.Quantity, -reservation.Quantity));
            }

            stock.DamagedQuantity -= order.Quantity;
            stock.ReservedDamagedQuantity -= order.Quantity;
            stock.AvailableQuantity = checked(
                stock.AvailableQuantity + order.Quantity);
            order.Status = ProductionOrderStatus.Completed;
            order.SettledDay = world.AbsoluteDay;
            world.InventoryTransactions.Add(transaction);
            world.MilitaryEquipmentTransactions.Add(
                new MilitaryEquipmentTransactionState
                {
                    Id = $"equipment_transaction.{world.AbsoluteDay}." +
                         $"{world.MilitaryEquipmentTransactions.Count:000000}",
                    Day = world.AbsoluteDay,
                    Type = MilitaryEquipmentTransactionType.Repair,
                    EquipmentDefinitionId = order.EquipmentDefinitionId,
                    Quantity = order.Quantity,
                    FromArmyId = order.ArmyId,
                    ToArmyId = order.ArmyId,
                    SourceRepairOrderId = order.Id,
                    Summary = "Workshop returned repaired equipment to armory."
                });
        }

        private static bool CanSettle(
            WorldState world,
            MilitaryEquipmentRepairOrderState order)
        {
            var army = FindArmy(world, order.ArmyId);
            var site = FindSite(world, order.ProductionSiteId, true);
            var container = ProcessingProductionSystem.FindContainer(
                world, order.InventoryContainerId);
            var manager = ProductInventorySystem.FindPerson(
                world, order.ManagerPersonId);
            return manager.IsAlive && manager.LocationId == site.LocationId &&
                   army.LocationId == site.LocationId &&
                   container.LocationId == site.LocationId &&
                   ProcessingProductionSystem.HasMembership(
                       world, manager.Id, site.OwnerOrganizationId);
        }

        private static int ActiveOrdersAtSite(WorldState world, string siteId)
        {
            var count = 0;
            for (var i = 0; i < world.ProcessingWorkOrders.Count; i++)
            {
                if (world.ProcessingWorkOrders[i].ProductionSiteId == siteId &&
                    world.ProcessingWorkOrders[i].Status ==
                        ProductionOrderStatus.Active)
                {
                    count++;
                }
            }

            for (var i = 0; i < world.MilitaryEquipmentRepairOrders.Count; i++)
            {
                if (world.MilitaryEquipmentRepairOrders[i].ProductionSiteId ==
                        siteId &&
                    world.MilitaryEquipmentRepairOrders[i].Status ==
                        ProductionOrderStatus.Active)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<BatchReservationState> BuildMaterialReservations(
            WorldState world,
            InventoryContainerState container,
            string productDefinitionId,
            long requiredQuantity)
        {
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerOrganizationId ==
                        container.OwnerOrganizationId &&
                    batch.InventoryContainerId == container.Id &&
                    batch.ProductDefinitionId == productDefinitionId &&
                    batch.Quantity > batch.ReservedQuantity)
                {
                    candidates.Add(batch);
                }
            }

            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var reservations = new List<BatchReservationState>();
            var remaining = requiredQuantity;
            for (var i = 0; i < candidates.Count && remaining > 0; i++)
            {
                var quantity = Math.Min(
                    remaining,
                    candidates[i].Quantity - candidates[i].ReservedQuantity);
                reservations.Add(new BatchReservationState
                {
                    BatchId = candidates[i].Id,
                    Quantity = quantity
                });
                remaining -= quantity;
            }

            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient repair material {productDefinitionId}.");
            }

            return reservations;
        }

        private static InventoryTransactionState NewRepairInventoryTransaction(
            WorldState world,
            InventoryTransactionType type,
            MilitaryEquipmentRepairOrderState order,
            string summary)
        {
            return new InventoryTransactionState
            {
                Id = $"inventory_transaction.{world.AbsoluteDay}." +
                     $"{world.InventoryTransactions.Count:D6}",
                Day = world.AbsoluteDay,
                Type = type,
                ActorPersonId = order.ManagerPersonId,
                SourceEquipmentRepairOrderId = order.Id,
                Summary = summary
            };
        }

        private static void AddOpeningMaterial(
            WorldState world,
            string productDefinitionId,
            long quantity)
        {
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                productDefinitionId);
            var transactionId =
                $"inventory_transaction.prototype_material.{productDefinitionId}";
            var container = ProcessingProductionSystem.FindContainer(
                world, PrototypeWorkshopContainerId);
            var batch = ProductInventorySystem.NewOrganizationBatch(
                world,
                product,
                container,
                transactionId,
                string.Empty,
                quantity,
                8_000);
            batch.Id = $"product_batch.prototype_material.{productDefinitionId}";
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.su_shuang",
                Summary = "Prototype workshop material opening balance.",
                Lines =
                {
                    ProductInventorySystem.Line(batch, quantity, 0)
                }
            });
        }

        private static ProductBatchState FindBatch(WorldState world, string id)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == id)
                {
                    return world.ProductBatches[i];
                }
            }

            throw new InvalidOperationException($"Missing product batch {id}.");
        }

        private static ProductionSiteState FindSite(
            WorldState world,
            string id,
            bool required)
        {
            for (var i = 0; i < world.ProductionSites.Count; i++)
            {
                if (world.ProductionSites[i].Id == id)
                {
                    return world.ProductionSites[i];
                }
            }

            if (required)
            {
                throw new InvalidOperationException(
                    $"Missing production site {id}.");
            }

            return null;
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == id)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {id}.");
        }

        private static MilitaryEquipmentDefinitionState FindDefinition(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryEquipmentDefinitions.Count; i++)
            {
                if (world.MilitaryEquipmentDefinitions[i].Id == id)
                {
                    return world.MilitaryEquipmentDefinitions[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing equipment definition {id}.");
        }

        private static MilitaryArmoryStockState FindStock(
            WorldState world,
            string armyId,
            string equipmentId)
        {
            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                var stock = world.MilitaryArmoryStocks[i];
                if (stock.ArmyId == armyId &&
                    stock.EquipmentDefinitionId == equipmentId)
                {
                    return stock;
                }
            }

            throw new InvalidOperationException(
                $"Missing armory stock {armyId}/{equipmentId}.");
        }
    }
}
