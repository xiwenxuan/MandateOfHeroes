using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryProcurementAudit
    {
        public long BuyerPaid;
        public long SupplierReceived;
        public int DispatchedQuantity;
        public int DeliveredQuantity;

        public bool MoneyBalanced => BuyerPaid == SupplierReceived;
        public bool QuantityBalanced =>
            DeliveredQuantity >= 0 && DeliveredQuantity <= DispatchedQuantity;
        public bool IsBalanced => MoneyBalanced && QuantityBalanced;
    }

    public sealed class MilitaryProcurementSystem
    {
        public const string PrototypeContainerId =
            "inventory_container.zhongshan_merchants.caravan_001";
        public const string CaravanContainerKindId =
            "inventory_container_kind.caravan";

        private readonly IPersonRepository _people;
        private readonly TravelSystem _travel;
        private readonly MilitaryAuthoritySystem _authority =
            new MilitaryAuthoritySystem();

        public MilitaryProcurementSystem(IPersonRepository people = null)
        {
            _people = people;
            _travel = new TravelSystem(people);
        }

        public void InitializePrototypeSupply(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (FindContainer(world, PrototypeContainerId, false) != null)
            {
                return;
            }

            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = PrototypeContainerId,
                KindId = CaravanContainerKindId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                CarrierPersonId = "person.zhang_shiping",
                LocationId = "location.zhongshan",
                CapacityWeight = 120
            });

            var definitions = new List<MilitaryEquipmentDefinitionState>(
                world.MilitaryEquipmentDefinitions);
            definitions.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < definitions.Count; i++)
            {
                AddOpeningBatch(world, definitions[i], 4);
            }

            world.Validate();
        }

        public MilitaryProcurementOrderState CreateOrderAndDispatch(
            WorldState world,
            StableId issuerPersonId,
            StableId carrierPersonId,
            StableId targetArmyId,
            StableId equipmentDefinitionId,
            int quantity,
            long unitPrice,
            StableId routeId,
            StableId destinationLocationId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (quantity <= 0 || unitPrice <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantity), "Quantity and unit price must be positive.");
            }

            var army = FindArmy(world, targetArmyId.Value);
            if (_authority.GetAuthority(
                    world, issuerPersonId, targetArmyId) <
                MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    $"{issuerPersonId} lacks army procurement authority.");
            }

            var definition = FindEquipment(
                world, equipmentDefinitionId.Value);
            var carrierContainer = FindContainerByCarrier(
                world, carrierPersonId.Value);
            var carrier = PeopleFor(world).GetRequired(carrierPersonId.Value);
            if (!carrier.IsAlive ||
                carrier.LocationId != carrierContainer.LocationId)
            {
                throw new InvalidOperationException(
                    "The carrier and cargo container must be alive and co-located.");
            }

            if (!HasMembership(
                    world, carrier.Id,
                    carrierContainer.OwnerOrganizationId))
            {
                throw new InvalidOperationException(
                    "The carrier must belong to the supplier organization.");
            }

            var batch = FindAvailableSupplierBatch(
                world,
                carrierContainer,
                definition.ProductDefinitionId,
                quantity);
            var sourceContainer = FindContainer(
                world, batch.InventoryContainerId, true);
            var cargoWeight = checked((long)definition.UnitWeight * quantity);
            var carrierLoad = CalculateContainerWeight(
                world, carrierContainer.Id);
            if (sourceContainer.Id != carrierContainer.Id &&
                checked(carrierLoad + cargoWeight) >
                    carrierContainer.CapacityWeight)
            {
                throw new InvalidOperationException(
                    "The carrier container lacks capacity for workshop cargo.");
            }
            var route = FindRoute(world, routeId.Value);
            var connects = route.FromLocationId == carrier.LocationId &&
                           route.ToLocationId == destinationLocationId.Value ||
                           route.Bidirectional &&
                           route.ToLocationId == carrier.LocationId &&
                           route.FromLocationId == destinationLocationId.Value;
            if (!connects || !ArmyCanReceiveAt(
                    world, army, destinationLocationId.Value))
            {
                throw new InvalidOperationException(
                    "The selected route or army rendezvous is invalid.");
            }

            var buyer = FindOrganization(world, army.OrganizationId);
            var supplier = FindOrganization(
                world, sourceContainer.OwnerOrganizationId);
            var totalPaid = checked(unitPrice * quantity);
            if (buyer.Treasury < totalPaid)
            {
                throw new InvalidOperationException(
                    "The buyer organization lacks procurement funds.");
            }
            var buyerTreasuryAfter = checked(buyer.Treasury - totalPaid);
            var supplierTreasuryAfter = checked(
                supplier.Treasury + totalPaid);

            var orderId =
                $"military_procurement.{world.AbsoluteDay}." +
                $"{world.MilitaryProcurementOrders.Count}";
            var journey = _travel.StartJourney(
                world,
                carrierPersonId,
                routeId,
                destinationLocationId,
                TravelMode.Caravan);
            var order = new MilitaryProcurementOrderState
            {
                Id = orderId,
                CreatedDay = world.AbsoluteDay,
                BuyerOrganizationId = buyer.Id,
                SupplierOrganizationId = supplier.Id,
                IssuerPersonId = issuerPersonId.Value,
                CarrierPersonId = carrierPersonId.Value,
                TargetArmyId = army.Id,
                EquipmentDefinitionId = definition.Id,
                ProductDefinitionId = definition.ProductDefinitionId,
                SourceBatchId = batch.Id,
                InventoryContainerId = sourceContainer.Id,
                RouteId = route.Id,
                JourneyId = journey.Id,
                OriginLocationId = journey.OriginLocationId,
                DestinationLocationId = journey.DestinationLocationId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPaid = totalPaid,
                Status = MilitaryProcurementStatus.InTransit
            };

            batch.Quantity -= quantity;
            buyer.Treasury = buyerTreasuryAfter;
            supplier.Treasury = supplierTreasuryAfter;
            world.MilitaryProcurementOrders.Add(order);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = $"inventory_transaction.{order.Id}.dispatch",
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.MilitaryProcurementDispatched,
                ActorPersonId = carrier.Id,
                SourceMilitaryProcurementId = order.Id,
                Summary = "Finished military equipment dispatched by caravan.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = -quantity
                    }
                }
            });
            world.MilitaryProcurementLedgerEntries.Add(
                new MilitaryProcurementLedgerEntryState
                {
                    Id = $"military_procurement_ledger.{order.Id}.payment",
                    Day = world.AbsoluteDay,
                    Type = MilitaryProcurementLedgerType.DispatchPayment,
                    ProcurementOrderId = order.Id,
                    BuyerOrganizationId = buyer.Id,
                    SupplierOrganizationId = supplier.Id,
                    BuyerMoneyDelta = -totalPaid,
                    SupplierMoneyDelta = totalPaid,
                    Summary = "Buyer paid supplier when cargo was dispatched."
                });
            world.Validate();
            return order;
        }

        public void ResolveArrivals(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var orders = new List<MilitaryProcurementOrderState>(
                world.MilitaryProcurementOrders);
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var changed = false;
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Status == MilitaryProcurementStatus.Delivered ||
                    FindJourney(world, order.JourneyId) != null)
                {
                    continue;
                }

                var army = FindArmy(world, order.TargetArmyId);
                var container = FindContainerByCarrier(
                    world, order.CarrierPersonId);
                if (army.LocationId != order.DestinationLocationId ||
                    container.LocationId != order.DestinationLocationId)
                {
                    if (order.Status != MilitaryProcurementStatus.AwaitingArmy)
                    {
                        order.Status = MilitaryProcurementStatus.AwaitingArmy;
                        changed = true;
                    }

                    continue;
                }

                Deliver(world, order);
                changed = true;
            }

            if (changed)
            {
                world.Validate();
            }
        }

        public MilitaryProcurementAudit Audit(WorldState world, string orderId)
        {
            var order = FindOrder(world, orderId);
            var audit = new MilitaryProcurementAudit
            {
                DispatchedQuantity = order.Quantity,
                DeliveredQuantity = order.Status ==
                    MilitaryProcurementStatus.Delivered
                    ? order.Quantity
                    : 0
            };
            for (var i = 0;
                 i < world.MilitaryProcurementLedgerEntries.Count;
                 i++)
            {
                var entry = world.MilitaryProcurementLedgerEntries[i];
                if (entry.ProcurementOrderId != orderId)
                {
                    continue;
                }

                audit.BuyerPaid -= entry.BuyerMoneyDelta;
                audit.SupplierReceived += entry.SupplierMoneyDelta;
            }

            return audit;
        }

        private static void AddOpeningBatch(
            WorldState world,
            MilitaryEquipmentDefinitionState definition,
            int quantity)
        {
            var transactionId =
                $"inventory_transaction.prototype_equipment.{definition.Id}";
            var batch = new ProductBatchState
            {
                Id = $"product_batch.prototype_equipment.{definition.Id}",
                ProductDefinitionId = definition.ProductDefinitionId,
                OwnerOrganizationId = "organization.zhongshan_merchants",
                InventoryContainerId = PrototypeContainerId,
                OriginLocationId = "location.zhongshan",
                SourceTransactionId = transactionId,
                UnitId = CoreProductionContent.ItemUnitId,
                UnitWeight = definition.UnitWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 10_000
            };
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                definition.ProductDefinitionId);
            batch.QualityDimensions = ProductQualityRules.CreateUniform(
                product, batch.QualityBasisPoints);
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = "person.zhang_shiping",
                Summary = "Prototype finished equipment opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
        }

        private static void Deliver(
            WorldState world,
            MilitaryProcurementOrderState order)
        {
            var stock = FindStock(
                world, order.TargetArmyId, order.EquipmentDefinitionId);
            stock.AvailableQuantity = checked(
                stock.AvailableQuantity + order.Quantity);
            order.Status = MilitaryProcurementStatus.Delivered;
            order.DeliveredDay = world.AbsoluteDay;
            world.MilitaryEquipmentTransactions.Add(
                new MilitaryEquipmentTransactionState
                {
                    Id = $"equipment_transaction.{order.Id}.receipt",
                    Day = world.AbsoluteDay,
                    Type = MilitaryEquipmentTransactionType.ProcurementReceipt,
                    EquipmentDefinitionId = order.EquipmentDefinitionId,
                    Quantity = order.Quantity,
                    ToArmyId = order.TargetArmyId,
                    SourceProcurementOrderId = order.Id,
                    Summary = "Procured equipment received into army armory."
                });
            world.MilitaryProcurementLedgerEntries.Add(
                new MilitaryProcurementLedgerEntryState
                {
                    Id = $"military_procurement_ledger.{order.Id}.receipt",
                    Day = world.AbsoluteDay,
                    Type = MilitaryProcurementLedgerType.ArmoryReceipt,
                    ProcurementOrderId = order.Id,
                    BuyerOrganizationId = order.BuyerOrganizationId,
                    SupplierOrganizationId = order.SupplierOrganizationId,
                    ArmoryQuantityDelta = order.Quantity,
                    Summary = "Cargo received into target army armory."
                });
        }

        private IPersonRepository PeopleFor(WorldState world) =>
            _people ?? new WorldStatePersonRepository(world);

        private static bool ArmyCanReceiveAt(
            WorldState world,
            ArmyState army,
            string destinationId)
        {
            if (army.LocationId == destinationId)
            {
                return true;
            }

            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == army.Id &&
                    world.ArmyMarches[i].DestinationLocationId == destinationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMembership(
            WorldState world,
            string personId,
            string organizationId)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == personId &&
                    world.Memberships[i].OrganizationId == organizationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static ProductBatchState FindAvailableSupplierBatch(
            WorldState world,
            InventoryContainerState carrierContainer,
            string productId,
            int quantity)
        {
            ProductBatchState fallback = null;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerOrganizationId !=
                        carrierContainer.OwnerOrganizationId ||
                    batch.ProductDefinitionId != productId ||
                    batch.Quantity - batch.ReservedQuantity < quantity)
                {
                    continue;
                }

                var container = FindContainer(
                    world, batch.InventoryContainerId, true);
                if (container.LocationId != carrierContainer.LocationId)
                {
                    continue;
                }

                if (container.Id == carrierContainer.Id)
                {
                    return batch;
                }

                if (fallback == null || string.CompareOrdinal(
                        batch.Id, fallback.Id) < 0)
                {
                    fallback = batch;
                }
            }

            if (fallback != null)
            {
                return fallback;
            }

            throw new InvalidOperationException(
                "The supplier lacks an available product batch.");
        }

        private static long CalculateContainerWeight(
            WorldState world,
            string containerId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].InventoryContainerId == containerId)
                {
                    total = checked(total +
                        world.ProductBatches[i].Quantity *
                        world.ProductBatches[i].UnitWeight);
                }
            }

            return total;
        }

        private static InventoryContainerState FindContainerByCarrier(
            WorldState world,
            string carrierId)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].CarrierPersonId == carrierId)
                {
                    return world.InventoryContainers[i];
                }
            }

            throw new InvalidOperationException(
                $"Carrier {carrierId} has no inventory container.");
        }

        private static InventoryContainerState FindContainer(
            WorldState world,
            string id,
            bool required)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return world.InventoryContainers[i];
                }
            }

            if (required)
            {
                throw new InvalidOperationException(
                    $"Missing inventory container {id}.");
            }

            return null;
        }

        private static MilitaryProcurementOrderState FindOrder(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryProcurementOrders.Count; i++)
            {
                if (world.MilitaryProcurementOrders[i].Id == id)
                {
                    return world.MilitaryProcurementOrders[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military procurement order {id}.");
        }

        private static JourneyState FindJourney(WorldState world, string id)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].Id == id)
                {
                    return world.Journeys[i];
                }
            }

            return null;
        }

        private static RouteState FindRoute(WorldState world, string id)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == id)
                {
                    return world.Routes[i];
                }
            }

            throw new InvalidOperationException($"Missing route {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == id)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException($"Missing organization {id}.");
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

        private static MilitaryEquipmentDefinitionState FindEquipment(
            WorldState world,
            string id)
        {
            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                if (world.MilitaryEquipmentDefinitions[i].Id == id)
                {
                    return world.MilitaryEquipmentDefinitions[i];
                }
            }

            throw new InvalidOperationException($"Missing equipment {id}.");
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
