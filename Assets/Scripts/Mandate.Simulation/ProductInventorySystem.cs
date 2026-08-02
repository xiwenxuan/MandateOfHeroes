using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class ProductInventorySystem
    {
        private readonly ProductionContentRegistry _content;

        public ProductInventorySystem(ProductionContentRegistry content = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public ProductBatchState ConvertLegacyBalanceToBatch(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string actorPersonId,
            string productDefinitionId,
            long quantity,
            string cropVarietyDefinitionId = null)
        {
            RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var family = FindFamily(world, familyId);
            var storage = FindFacility(world, storageFacilityId);
            var actor = FindPerson(world, actorPersonId);
            var product = _content.GetProduct(productDefinitionId);
            if (storage.Kind != VillageFacilityKind.HouseholdGranary ||
                storage.OwnerFamilyId != family.Id ||
                actor.FamilyId != family.Id || !actor.IsAlive ||
                storage.InventoryUnits != CalculatePhysicalInventoryUnits(
                    world, storage.Id, family.Id, _content))
            {
                throw new InvalidOperationException(
                    "Legacy inventory conversion requires a consistent family granary.");
            }

            long grainDelta = 0;
            long seedDelta = 0;
            if (product.Id == CoreProductionContent.WheatGrainProductId)
            {
                if (family.Grain < quantity)
                {
                    throw new InvalidOperationException("Insufficient legacy grain balance.");
                }

                grainDelta = -quantity;
            }
            else if (product.Id == CoreProductionContent.WheatSeedProductId)
            {
                if (family.SeedGrain < quantity ||
                    string.IsNullOrWhiteSpace(cropVarietyDefinitionId))
                {
                    throw new InvalidOperationException(
                        "Seed conversion requires sufficient balance and a variety.");
                }

                var variety = _content.GetCropVariety(cropVarietyDefinitionId);
                if (variety.CropDefinitionId != CoreProductionContent.WheatCropId)
                {
                    throw new InvalidOperationException(
                        "The seed variety is incompatible with wheat seed.");
                }

                seedDelta = -quantity;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Product {product.Id} has no legacy family balance adapter.");
            }

            var transaction = NewTransaction(
                world,
                InventoryTransactionType.LegacyBalanceConverted,
                actor.Id,
                string.Empty,
                grainDelta,
                seedDelta,
                0,
                $"Converted {quantity} {product.Id} from the legacy family balance.");
            var batch = NewBatch(
                world,
                product,
                family,
                storage,
                transaction.Id,
                string.Empty,
                quantity,
                cropVarietyDefinitionId,
                product.Id == CoreProductionContent.WheatSeedProductId
                    ? 8_000
                    : 0,
                product.Id == CoreProductionContent.WheatSeedProductId
                    ? 9_000
                    : 0);
            transaction.Lines.Add(Line(batch, quantity, 0));
            family.Grain = checked(family.Grain + grainDelta);
            family.SeedGrain = checked(family.SeedGrain + seedDelta);
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(transaction);
            return batch;
        }

        public long MarketableQuantity(
            WorldState world,
            string locationId,
            string productDefinitionId)
        {
            RequireWorld(world);
            var product = _content.GetProduct(productDefinitionId);
            if (!product.CategoryTags.Contains("product.market"))
            {
                return 0;
            }

            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var facility = FindFacility(world, batch.StorageFacilityId);
                var village = FindVillage(world, facility.VillageId);
                if (village.LocationId == locationId &&
                    batch.ProductDefinitionId == productDefinitionId)
                {
                    total = checked(total + batch.Quantity - batch.ReservedQuantity);
                }
            }

            return total;
        }

        public static long CalculateTrackedBatchWeight(
            WorldState world,
            string storageFacilityId,
            ProductionContentRegistry content = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.StorageFacilityId == storageFacilityId)
                {
                    total = checked(total + batch.Quantity * batch.UnitWeight);
                }
            }

            return total;
        }

        public static long CalculatePhysicalInventoryUnits(
            WorldState world,
            string storageFacilityId,
            string familyId,
            ProductionContentRegistry content = null)
        {
            var family = FindFamily(world, familyId);
            return checked(
                family.Grain + family.SeedGrain +
                CalculateTrackedBatchWeight(world, storageFacilityId, content));
        }

        internal static InventoryTransactionState NewTransaction(
            WorldState world,
            InventoryTransactionType type,
            string actorPersonId,
            string workOrderId,
            long legacyGrainDelta,
            long legacySeedDelta,
            long facilityDelta,
            string summary)
        {
            return new InventoryTransactionState
            {
                Id = $"inventory_transaction.{world.AbsoluteDay}." +
                     $"{world.InventoryTransactions.Count:D6}",
                Day = world.AbsoluteDay,
                Type = type,
                ActorPersonId = actorPersonId,
                SourceWorkOrderId = workOrderId,
                LegacyFamilyGrainDelta = legacyGrainDelta,
                LegacyFamilySeedGrainDelta = legacySeedDelta,
                FacilityInventoryDelta = facilityDelta,
                Summary = summary
            };
        }

        internal static InventoryTransactionLineState Line(
            ProductBatchState batch,
            long quantityDelta,
            long reservedDelta)
        {
            return new InventoryTransactionLineState
            {
                BatchId = batch.Id,
                ProductDefinitionId = batch.ProductDefinitionId,
                OwnerFamilyId = batch.OwnerFamilyId,
                OwnerOrganizationId = batch.OwnerOrganizationId,
                StorageFacilityId = batch.StorageFacilityId,
                InventoryContainerId = batch.InventoryContainerId,
                UnitId = batch.UnitId,
                QuantityDelta = quantityDelta,
                ReservedQuantityDelta = reservedDelta
            };
        }

        internal static ProductBatchState NewBatch(
            WorldState world,
            ProductDefinition product,
            FamilyState family,
            VillageFacilityState storage,
            string sourceTransactionId,
            string sourceWorkOrderId,
            long quantity,
            string cropVarietyDefinitionId,
            int seedVigor,
            int seedPurity)
        {
            var village = FindVillage(world, storage.VillageId);
            return new ProductBatchState
            {
                Id = $"product_batch.{world.AbsoluteDay}." +
                     $"{world.ProductBatches.Count:D6}",
                ProductDefinitionId = product.Id,
                OwnerFamilyId = family.Id,
                StorageFacilityId = storage.Id,
                OriginLocationId = village.LocationId,
                SourceWorkOrderId = sourceWorkOrderId,
                SourceTransactionId = sourceTransactionId,
                CropVarietyDefinitionId = cropVarietyDefinitionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_000,
                FreshnessBasisPoints = 10_000,
                SeedVigorBasisPoints = seedVigor,
                SeedPurityBasisPoints = seedPurity
            };
        }

        internal static ProductBatchState NewOrganizationBatch(
            WorldState world,
            ProductDefinition product,
            InventoryContainerState container,
            string sourceTransactionId,
            string sourceWorkOrderId,
            long quantity,
            int qualityBasisPoints)
        {
            return new ProductBatchState
            {
                Id = $"product_batch.{world.AbsoluteDay}." +
                     $"{world.ProductBatches.Count:D6}",
                ProductDefinitionId = product.Id,
                OwnerOrganizationId = container.OwnerOrganizationId,
                InventoryContainerId = container.Id,
                OriginLocationId = container.LocationId,
                SourceWorkOrderId = sourceWorkOrderId,
                SourceTransactionId = sourceTransactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = qualityBasisPoints,
                FreshnessBasisPoints = 10_000
            };
        }

        internal static void RequireWorld(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
        }

        internal static FamilyState FindFamily(WorldState world, string id)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == id)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {id}.");
        }

        internal static PersonState FindPerson(WorldState world, string id)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == id)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {id}.");
        }

        internal static VillageFacilityState FindFacility(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                if (world.VillageFacilities[i].Id == id)
                {
                    return world.VillageFacilities[i];
                }
            }

            throw new InvalidOperationException($"Missing facility {id}.");
        }

        internal static VillageState FindVillage(WorldState world, string id)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == id)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {id}.");
        }
    }

    public sealed class ProcessingProductionSystem
    {
        private readonly ProductionContentRegistry _content;

        public ProcessingProductionSystem(ProductionContentRegistry content = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public ProcessingWorkOrderState CreateOrder(
            WorldState world,
            string recipeDefinitionId,
            string methodDefinitionId,
            string familyId,
            string storageFacilityId,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (runCount <= 0 ||
                !Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(runCount));
            }

            var recipe = _content.GetRecipe(recipeDefinitionId);
            var method = _content.GetMethod(methodDefinitionId);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            var storage = ProductInventorySystem.FindFacility(
                world, storageFacilityId);
            var manager = ProductInventorySystem.FindPerson(world, managerPersonId);
            if (!string.IsNullOrEmpty(recipe.CropDefinitionId) ||
                !method.RecipeDefinitionIds.Contains(recipe.Id) ||
                method.YieldBasisPoints != 10_000 ||
                storage.Kind != VillageFacilityKind.HouseholdGranary ||
                storage.OwnerFamilyId != family.Id ||
                manager.FamilyId != family.Id || !manager.IsAlive ||
                !recipe.FacilityTags.Contains(
                    VillageFacilityTags.FromKind(storage.Kind)))
            {
                throw new InvalidOperationException(
                    "Processing order references incompatible content or actors.");
            }

            var reservations = BuildReservations(
                world, recipe, family.Id, string.Empty, storage.Id,
                string.Empty, runCount);
            var order = new ProcessingWorkOrderState
            {
                Id = $"processing.{world.AbsoluteDay}." +
                     $"{world.ProcessingWorkOrders.Count:D6}",
                RecipeDefinitionId = recipe.Id,
                MethodDefinitionId = method.Id,
                OwnerFamilyId = family.Id,
                StorageFacilityId = storage.Id,
                ManagerPersonId = manager.Id,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                FinishDay = checked(world.AbsoluteDay +
                    (long)recipe.DurationDays * runCount),
                RunCount = runCount,
                InputReservations = reservations
            };
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.Reserved,
                manager.Id,
                order.Id,
                0,
                0,
                0,
                $"Reserved inputs for {order.Id}.");
            for (var i = 0; i < reservations.Count; i++)
            {
                var batch = FindBatch(world, reservations[i].BatchId);
                batch.ReservedQuantity = checked(
                    batch.ReservedQuantity + reservations[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, reservations[i].Quantity));
            }

            world.ProcessingWorkOrders.Add(order);
            world.InventoryTransactions.Add(transaction);
            return order;
        }

        public ProcessingWorkOrderState CreateOrganizationOrder(
            WorldState world,
            string recipeDefinitionId,
            string methodDefinitionId,
            string organizationId,
            string productionSiteId,
            string inventoryContainerId,
            string managerPersonId,
            ProductionControlMode controlMode,
            int runCount)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (runCount <= 0 ||
                !Enum.IsDefined(typeof(ProductionControlMode), controlMode))
            {
                throw new ArgumentOutOfRangeException(nameof(runCount));
            }

            var recipe = _content.GetRecipe(recipeDefinitionId);
            var method = _content.GetMethod(methodDefinitionId);
            var site = FindProductionSite(world, productionSiteId);
            var container = FindContainer(world, inventoryContainerId);
            var manager = ProductInventorySystem.FindPerson(world, managerPersonId);
            if (!string.IsNullOrEmpty(recipe.CropDefinitionId) ||
                !method.RecipeDefinitionIds.Contains(recipe.Id) ||
                method.YieldBasisPoints != 10_000 ||
                site.OwnerOrganizationId != organizationId ||
                site.InventoryContainerId != container.Id ||
                site.ManagerPersonId != manager.Id ||
                site.LocationId != container.LocationId ||
                container.OwnerOrganizationId != organizationId ||
                !string.IsNullOrEmpty(container.CarrierPersonId) ||
                !manager.IsAlive || manager.LocationId != site.LocationId ||
                !HasMembership(world, manager.Id, organizationId) ||
                !HasCompatibleFacilityTag(recipe, site) ||
                ActiveOrdersAtSite(world, site.Id) >=
                    site.ConcurrentOrderCapacity)
            {
                throw new InvalidOperationException(
                    "Organization processing order references incompatible content, site, inventory, or actors.");
            }

            var reservations = BuildReservations(
                world, recipe, string.Empty, organizationId, string.Empty,
                container.Id, runCount);
            var order = new ProcessingWorkOrderState
            {
                Id = $"processing.{world.AbsoluteDay}." +
                     $"{world.ProcessingWorkOrders.Count:D6}",
                RecipeDefinitionId = recipe.Id,
                MethodDefinitionId = method.Id,
                OwnerOrganizationId = organizationId,
                ProductionSiteId = site.Id,
                InventoryContainerId = container.Id,
                ManagerPersonId = manager.Id,
                ControlMode = controlMode,
                Status = ProductionOrderStatus.Active,
                CreatedDay = world.AbsoluteDay,
                FinishDay = checked(world.AbsoluteDay +
                    (long)recipe.DurationDays * runCount),
                RunCount = runCount,
                InputReservations = reservations
            };
            Reserve(world, order, reservations);
            return order;
        }

        private static void Reserve(
            WorldState world,
            ProcessingWorkOrderState order,
            List<BatchReservationState> reservations)
        {
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.Reserved,
                order.ManagerPersonId,
                order.Id,
                0,
                0,
                0,
                $"Reserved inputs for {order.Id}.");
            for (var i = 0; i < reservations.Count; i++)
            {
                var batch = FindBatch(world, reservations[i].BatchId);
                batch.ReservedQuantity = checked(
                    batch.ReservedQuantity + reservations[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, 0, reservations[i].Quantity));
            }

            world.ProcessingWorkOrders.Add(order);
            world.InventoryTransactions.Add(transaction);
        }

        public void ResolveDueOrders(WorldState world)
        {
            ProductInventorySystem.RequireWorld(world);
            var due = new List<ProcessingWorkOrderState>();
            for (var i = 0; i < world.ProcessingWorkOrders.Count; i++)
            {
                var order = world.ProcessingWorkOrders[i];
                if (order.Status == ProductionOrderStatus.Active &&
                    order.FinishDay <= world.AbsoluteDay &&
                    CanSettle(world, order))
                {
                    due.Add(order);
                }
            }

            due.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < due.Count; i++)
            {
                Settle(world, due[i]);
            }
        }

        private void Settle(WorldState world, ProcessingWorkOrderState order)
        {
            var recipe = _content.GetRecipe(order.RecipeDefinitionId);
            var organizationOrder =
                !string.IsNullOrEmpty(order.OwnerOrganizationId);
            FamilyState family = null;
            VillageFacilityState storage = null;
            InventoryContainerState container = null;
            if (organizationOrder)
            {
                container = FindContainer(world, order.InventoryContainerId);
            }
            else
            {
                family = ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId);
                storage = ProductInventorySystem.FindFacility(
                    world, order.StorageFacilityId);
            }
            long inputWeight = 0;
            var minimumQuality = 10_000;
            for (var i = 0; i < order.InputReservations.Count; i++)
            {
                var reservation = order.InputReservations[i];
                var batch = FindBatch(world, reservation.BatchId);
                if (batch.ReservedQuantity < reservation.Quantity ||
                    batch.Quantity < reservation.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Reserved batch {batch.Id} is no longer available.");
                }

                inputWeight = checked(inputWeight + reservation.Quantity *
                    _content.GetProduct(batch.ProductDefinitionId).BaseWeight);
                minimumQuality = Math.Min(minimumQuality, batch.QualityBasisPoints);
            }

            long outputWeight = 0;
            for (var i = 0; i < recipe.Outputs.Count; i++)
            {
                var output = recipe.Outputs[i];
                outputWeight = checked(outputWeight +
                    output.QuantityPerLandUnit * order.RunCount *
                    _content.GetProduct(output.ProductDefinitionId).BaseWeight);
            }

            if (inputWeight != outputWeight)
            {
                throw new InvalidOperationException(
                    $"Processing order {order.Id} does not conserve weight.");
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.RecipeSettled,
                order.ManagerPersonId,
                order.Id,
                0,
                0,
                0,
                $"Settled processing order {order.Id}.");
            for (var i = 0; i < order.InputReservations.Count; i++)
            {
                var reservation = order.InputReservations[i];
                var batch = FindBatch(world, reservation.BatchId);
                batch.Quantity -= reservation.Quantity;
                batch.ReservedQuantity -= reservation.Quantity;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, -reservation.Quantity, -reservation.Quantity));
            }

            for (var i = 0; i < recipe.Outputs.Count; i++)
            {
                var output = recipe.Outputs[i];
                var product = _content.GetProduct(output.ProductDefinitionId);
                var quantity = checked(
                    output.QuantityPerLandUnit * order.RunCount);
                var batch = organizationOrder
                    ? ProductInventorySystem.NewOrganizationBatch(
                        world, product, container, transaction.Id, order.Id,
                        quantity, minimumQuality)
                    : ProductInventorySystem.NewBatch(
                        world, product, family, storage, transaction.Id,
                        order.Id, quantity, string.Empty, 0, 0);
                batch.QualityBasisPoints = minimumQuality;
                world.ProductBatches.Add(batch);
                order.OutputBatchIds.Add(batch.Id);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, batch.Quantity, 0));
            }

            order.Status = ProductionOrderStatus.Completed;
            order.SettledDay = world.AbsoluteDay;
            world.InventoryTransactions.Add(transaction);
        }

        private List<BatchReservationState> BuildReservations(
            WorldState world,
            RecipeDefinition recipe,
            string familyId,
            string organizationId,
            string storageId,
            string containerId,
            int runCount)
        {
            var result = new List<BatchReservationState>();
            for (var inputIndex = 0; inputIndex < recipe.Inputs.Count; inputIndex++)
            {
                var input = recipe.Inputs[inputIndex];
                var remaining = checked(input.QuantityPerLandUnit * runCount);
                var candidates = new List<ProductBatchState>();
                for (var i = 0; i < world.ProductBatches.Count; i++)
                {
                    var batch = world.ProductBatches[i];
                    var ownershipMatches =
                        !string.IsNullOrEmpty(familyId) &&
                        batch.OwnerFamilyId == familyId &&
                        batch.StorageFacilityId == storageId ||
                        !string.IsNullOrEmpty(organizationId) &&
                        batch.OwnerOrganizationId == organizationId &&
                        batch.InventoryContainerId == containerId;
                    if (ownershipMatches &&
                        batch.ProductDefinitionId == input.ProductDefinitionId &&
                        batch.Quantity > batch.ReservedQuantity)
                    {
                        candidates.Add(batch);
                    }
                }

                candidates.Sort((left, right) => string.CompareOrdinal(
                    left.Id, right.Id));
                for (var i = 0; i < candidates.Count && remaining > 0; i++)
                {
                    long alreadyPlanned = 0;
                    for (var plannedIndex = 0;
                         plannedIndex < result.Count;
                         plannedIndex++)
                    {
                        if (result[plannedIndex].BatchId == candidates[i].Id)
                        {
                            alreadyPlanned += result[plannedIndex].Quantity;
                        }
                    }

                    var available = candidates[i].Quantity -
                                    candidates[i].ReservedQuantity -
                                    alreadyPlanned;
                    if (available <= 0)
                    {
                        continue;
                    }

                    var quantity = Math.Min(available, remaining);
                    BatchReservationState existing = null;
                    for (var plannedIndex = 0;
                         plannedIndex < result.Count;
                         plannedIndex++)
                    {
                        if (result[plannedIndex].BatchId == candidates[i].Id)
                        {
                            existing = result[plannedIndex];
                            break;
                        }
                    }

                    if (existing == null)
                    {
                        result.Add(new BatchReservationState
                        {
                            BatchId = candidates[i].Id,
                            Quantity = quantity
                        });
                    }
                    else
                    {
                        existing.Quantity = checked(existing.Quantity + quantity);
                    }

                    remaining -= quantity;
                }

                if (remaining != 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient batch input {input.ProductDefinitionId}.");
                }
            }

            return result;
        }

        private static bool CanSettle(
            WorldState world,
            ProcessingWorkOrderState order)
        {
            var manager = ProductInventorySystem.FindPerson(
                world, order.ManagerPersonId);
            if (!manager.IsAlive)
            {
                return false;
            }

            if (string.IsNullOrEmpty(order.OwnerOrganizationId))
            {
                return manager.FamilyId == order.OwnerFamilyId;
            }

            var site = FindProductionSite(world, order.ProductionSiteId);
            var container = FindContainer(world, order.InventoryContainerId);
            return manager.LocationId == site.LocationId &&
                   container.LocationId == site.LocationId &&
                   HasMembership(world, manager.Id, order.OwnerOrganizationId);
        }

        private static bool HasCompatibleFacilityTag(
            RecipeDefinition recipe,
            ProductionSiteState site)
        {
            for (var i = 0; i < recipe.FacilityTags.Count; i++)
            {
                if (site.FacilityTags.Contains(recipe.FacilityTags[i]))
                {
                    return true;
                }
            }

            return false;
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

        internal static bool HasMembership(
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

        internal static ProductionSiteState FindProductionSite(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.ProductionSites.Count; i++)
            {
                if (world.ProductionSites[i].Id == id)
                {
                    return world.ProductionSites[i];
                }
            }

            throw new InvalidOperationException($"Missing production site {id}.");
        }

        internal static InventoryContainerState FindContainer(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return world.InventoryContainers[i];
                }
            }

            throw new InvalidOperationException($"Missing inventory container {id}.");
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
    }
}
