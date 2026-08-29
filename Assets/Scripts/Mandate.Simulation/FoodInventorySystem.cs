using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class FoodInventorySummary
    {
        public long PhysicalQuantity { get; internal set; }
        public long NutritionBasisUnits { get; internal set; }
        public long VolumeBasisUnits { get; internal set; }
        public long MarketValueBasisUnits { get; internal set; }
    }

    public sealed class FoodConsumptionResult
    {
        public long RequiredNutritionBasisUnits { get; internal set; }
        public long ProvidedNutritionBasisUnits { get; internal set; }
        public long ConsumedPhysicalQuantity { get; internal set; }
        public bool Fulfilled =>
            ProvidedNutritionBasisUnits >= RequiredNutritionBasisUnits;
        public string InventoryTransactionId { get; internal set; }
    }

    public sealed class FoodTransferResult
    {
        public long RequestedPhysicalQuantity { get; internal set; }
        public long RequestedNutritionBasisUnits { get; internal set; }
        public long TransferredPhysicalQuantity { get; internal set; }
        public long TransferredNutritionBasisUnits { get; internal set; }
        public string InventoryTransactionId { get; internal set; }
    }

    public sealed class FoodInventorySystem
    {
        private readonly ProductionContentRegistry _content;

        public FoodInventorySystem(ProductionContentRegistry content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public FoodInventorySummary SummarizeFamilyGranary(
            WorldState world,
            string familyId,
            string storageFacilityId)
        {
            ValidateFamilyGranary(
                world, familyId, storageFacilityId, string.Empty, false);
            var summary = new FoodInventorySummary();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId != familyId ||
                    batch.StorageFacilityId != storageFacilityId ||
                    !_content.TryGetFood(
                        batch.ProductDefinitionId, out var food))
                {
                    continue;
                }

                var quantity = batch.Quantity - batch.ReservedQuantity;
                summary.PhysicalQuantity = checked(
                    summary.PhysicalQuantity + quantity);
                summary.NutritionBasisUnits = checked(
                    summary.NutritionBasisUnits +
                    quantity * food.NutritionBasisPoints);
                summary.VolumeBasisUnits = checked(
                    summary.VolumeBasisUnits +
                    quantity * food.VolumeBasisPoints);
                summary.MarketValueBasisUnits = checked(
                    summary.MarketValueBasisUnits +
                    quantity * food.MarketValueBasisPoints);
            }

            return summary;
        }

        public FoodInventorySummary SummarizeContainer(
            WorldState world,
            string inventoryContainerId)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            _ = ProductInventorySystem.FindContainer(
                world, inventoryContainerId);
            return Summarize(world, string.Empty, string.Empty,
                inventoryContainerId);
        }

        public FoodTransferResult TransferFamilyToContainer(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string destinationContainerId,
            string actorPersonId,
            long physicalQuantity,
            InventoryTransactionType transactionType,
            string sourceVillageId,
            string sourceCountyGovernanceId = "")
        {
            if (physicalQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalQuantity));
            }

            var storage = ValidateFamilyGranary(
                world, familyId, storageFacilityId, actorPersonId, true);
            var destination = ProductInventorySystem.FindContainer(
                world, destinationContainerId);
            var village = FindVillage(world, sourceVillageId);
            if (transactionType !=
                    InventoryTransactionType.FoodTaxTransferred ||
                !string.IsNullOrEmpty(sourceCountyGovernanceId) ||
                !village.HouseholdIds.Contains(familyId) ||
                village.PublicGranaryInventoryContainerId != destination.Id)
            {
                throw new InvalidOperationException(
                    "Family food tax must move from a governed household to its village granary.");
            }
            return Transfer(
                world,
                familyId,
                storage.Id,
                string.Empty,
                string.Empty,
                string.Empty,
                destination.Id,
                actorPersonId,
                physicalQuantity,
                0,
                transactionType,
                sourceVillageId,
                sourceCountyGovernanceId);
        }

        public FoodTransferResult TransferContainerToFamilyByNutrition(
            WorldState world,
            string sourceContainerId,
            string familyId,
            string storageFacilityId,
            string actorPersonId,
            long nutritionBasisUnits,
            InventoryTransactionType transactionType,
            string sourceVillageId,
            string sourceCountyGovernanceId = "")
        {
            if (nutritionBasisUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nutritionBasisUnits));
            }

            _ = ProductInventorySystem.FindContainer(world, sourceContainerId);
            var storage = ValidateFamilyGranary(
                world, familyId, storageFacilityId, actorPersonId, true);
            var village = FindVillage(world, sourceVillageId);
            if (transactionType != InventoryTransactionType
                    .FoodVillageReliefTransferred ||
                !string.IsNullOrEmpty(sourceCountyGovernanceId) ||
                village.PublicGranaryInventoryContainerId !=
                    sourceContainerId ||
                !village.HouseholdIds.Contains(familyId))
            {
                throw new InvalidOperationException(
                    "Village relief must move from the local granary to a local household.");
            }
            return Transfer(
                world,
                string.Empty,
                string.Empty,
                sourceContainerId,
                familyId,
                storage.Id,
                string.Empty,
                actorPersonId,
                0,
                nutritionBasisUnits,
                transactionType,
                sourceVillageId,
                sourceCountyGovernanceId);
        }

        public FoodTransferResult TransferContainerToContainer(
            WorldState world,
            string sourceContainerId,
            string destinationContainerId,
            string actorPersonId,
            long physicalQuantity,
            InventoryTransactionType transactionType,
            string sourceVillageId,
            string sourceCountyGovernanceId)
        {
            if (physicalQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalQuantity));
            }

            _ = ProductInventorySystem.FindContainer(world, sourceContainerId);
            _ = ProductInventorySystem.FindContainer(
                world, destinationContainerId);
            if (sourceContainerId == destinationContainerId)
            {
                throw new InvalidOperationException(
                    "A food transfer requires different containers.");
            }

            var village = FindVillage(world, sourceVillageId);
            var governance = FindGovernance(
                world, sourceCountyGovernanceId);
            var sameCounty = village.ParentLocationId ==
                governance.CountyLocationId;
            var validRelief = transactionType == InventoryTransactionType
                    .FoodCountyReliefTransferred &&
                sourceContainerId == governance.GranaryInventoryContainerId &&
                destinationContainerId ==
                    village.PublicGranaryInventoryContainerId;
            var validRemittance = transactionType ==
                    InventoryTransactionType.FoodTaxRemitted &&
                sourceContainerId ==
                    village.PublicGranaryInventoryContainerId &&
                destinationContainerId ==
                    governance.GranaryInventoryContainerId;
            if (!sameCounty || !validRelief && !validRemittance)
            {
                throw new InvalidOperationException(
                    "County food transfer must stay within its governed village boundary.");
            }

            return Transfer(
                world,
                string.Empty,
                string.Empty,
                sourceContainerId,
                string.Empty,
                string.Empty,
                destinationContainerId,
                actorPersonId,
                physicalQuantity,
                0,
                transactionType,
                sourceVillageId,
                sourceCountyGovernanceId);
        }

        public FoodTransferResult TransferReservedFamilyToFamily(
            WorldState world,
            string sellerFamilyId,
            string sellerStorageFacilityId,
            string buyerFamilyId,
            string buyerStorageFacilityId,
            string actorPersonId,
            IList<FormalMarketBatchReservationState> reservations,
            long physicalQuantity,
            string formalMarketOrderId,
            string countyGovernanceId)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                reservations == null || reservations.Count == 0 ||
                physicalQuantity <= 0 ||
                string.IsNullOrEmpty(formalMarketOrderId))
            {
                throw new InvalidOperationException(
                    "Formal market delivery requires reserved formal food stock.");
            }

            var sellerStorage = ValidateFamilyGranary(
                world,
                sellerFamilyId,
                sellerStorageFacilityId,
                actorPersonId,
                false);
            var buyerStorage = ValidateFamilyGranary(
                world,
                buyerFamilyId,
                buyerStorageFacilityId,
                string.Empty,
                false);
            var governance = FindGovernance(world, countyGovernanceId);
            if (sellerFamilyId == buyerFamilyId ||
                !FamilyBelongsToCounty(
                    world, sellerFamilyId, governance.CountyLocationId) ||
                !FamilyBelongsToCounty(
                    world, buyerFamilyId, governance.CountyLocationId))
            {
                throw new InvalidOperationException(
                    "Formal market delivery must remain between different families in one county.");
            }

            var capacityWeight = Math.Max(
                0L, buyerStorage.Capacity - buyerStorage.InventoryUnits);
            var plan = new List<FoodTransferPlanLine>();
            long plannedQuantity = 0;
            long plannedNutrition = 0;
            long plannedWeight = 0;
            for (var i = 0;
                 i < reservations.Count && plannedQuantity < physicalQuantity;
                 i++)
            {
                var reservation = reservations[i] ??
                    throw new InvalidOperationException(
                        "A formal market reservation cannot be null.");
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }

                var batch = FindBatch(world, reservation.BatchId);
                if (batch.OwnerFamilyId != sellerFamilyId ||
                    batch.StorageFacilityId != sellerStorageFacilityId ||
                    batch.ReservedQuantity < reservation.RemainingQuantity)
                {
                    throw new InvalidOperationException(
                        $"Invalid formal market reservation for {batch.Id}.");
                }

                var product = _content.GetProduct(
                    batch.ProductDefinitionId);
                if (!product.CategoryTags.Contains("product.market"))
                {
                    throw new InvalidOperationException(
                        $"Product {product.Id} is not enabled for formal market transfer.");
                }

                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var take = Math.Min(
                    reservation.RemainingQuantity,
                    Math.Min(
                        physicalQuantity - plannedQuantity,
                        byCapacity));
                if (take <= 0)
                {
                    continue;
                }

                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedQuantity = checked(plannedQuantity + take);
                if (_content.TryGetFood(
                        batch.ProductDefinitionId, out var food))
                {
                    plannedNutrition = checked(
                        plannedNutrition + take * food.NutritionBasisPoints);
                }
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
            }

            var result = new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                TransferredPhysicalQuantity = plannedQuantity,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = string.Empty
            };
            if (plan.Count == 0)
            {
                return result;
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodMarketTransferred,
                actorPersonId ?? string.Empty,
                string.Empty,
                0,
                0,
                0,
                $"Delivered {plannedQuantity} reserved product units through the formal county market.");
            transaction.SourceFormalMarketOrderId = formalMarketOrderId;
            transaction.SourceCountyGovernanceId = countyGovernanceId;
            for (var i = 0; i < plan.Count; i++)
            {
                var sourceBatch = plan[i].Batch;
                var reservation = FindReservation(
                    reservations, sourceBatch.Id);
                sourceBatch.Quantity = checked(
                    sourceBatch.Quantity - plan[i].Quantity);
                sourceBatch.ReservedQuantity = checked(
                    sourceBatch.ReservedQuantity - plan[i].Quantity);
                reservation.RemainingQuantity = checked(
                    reservation.RemainingQuantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    sourceBatch,
                    -plan[i].Quantity,
                    -plan[i].Quantity));
                var destinationBatch = CloneForDestination(
                    world,
                    sourceBatch,
                    plan[i].Quantity,
                    transaction.Id,
                    buyerFamilyId,
                    buyerStorageFacilityId,
                    null);
                world.ProductBatches.Add(destinationBatch);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    destinationBatch, plan[i].Quantity, 0));
            }

            sellerStorage.InventoryUnits = checked(
                sellerStorage.InventoryUnits - plannedWeight);
            buyerStorage.InventoryUnits = checked(
                buyerStorage.InventoryUnits + plannedWeight);
            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        public FoodTransferResult TransferReservedFamilyToCountyGranary(
            WorldState world,
            string sellerFamilyId,
            string sellerStorageFacilityId,
            string countyGranaryContainerId,
            string actorPersonId,
            IList<FormalMarketBatchReservationState> reservations,
            long physicalQuantity,
            string formalMarketOrderId,
            string countyGovernanceId)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                reservations == null || reservations.Count == 0 ||
                physicalQuantity <= 0 ||
                string.IsNullOrEmpty(formalMarketOrderId))
            {
                throw new InvalidOperationException(
                    "Public relief procurement requires reserved formal food stock.");
            }

            var sellerStorage = ValidateFamilyGranary(
                world,
                sellerFamilyId,
                sellerStorageFacilityId,
                actorPersonId,
                false);
            var governance = FindGovernance(world, countyGovernanceId);
            var destination = ProductInventorySystem.FindContainer(
                world, countyGranaryContainerId);
            if (governance.GranaryInventoryContainerId != destination.Id ||
                destination.OwnerOrganizationId !=
                    governance.GovernmentOrganizationId ||
                !string.IsNullOrEmpty(destination.OwnerFamilyId) ||
                !FamilyBelongsToCounty(
                    world, sellerFamilyId, governance.CountyLocationId))
            {
                throw new InvalidOperationException(
                    "Public relief procurement must deliver local family stock to the county granary.");
            }

            var capacityWeight = Math.Max(
                0L,
                destination.CapacityWeight -
                CalculateContainerWeight(world, destination.Id));
            var plan = new List<FoodTransferPlanLine>();
            long plannedQuantity = 0;
            long plannedNutrition = 0;
            long plannedWeight = 0;
            for (var i = 0;
                 i < reservations.Count && plannedQuantity < physicalQuantity;
                 i++)
            {
                var reservation = reservations[i] ??
                    throw new InvalidOperationException(
                        "A formal market reservation cannot be null.");
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }

                var batch = FindBatch(world, reservation.BatchId);
                if (batch.OwnerFamilyId != sellerFamilyId ||
                    batch.StorageFacilityId != sellerStorageFacilityId ||
                    batch.ReservedQuantity < reservation.RemainingQuantity ||
                    !_content.TryGetFood(batch.ProductDefinitionId, out var food))
                {
                    throw new InvalidOperationException(
                        $"Invalid public relief procurement reservation for {batch.Id}.");
                }

                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var take = Math.Min(
                    reservation.RemainingQuantity,
                    Math.Min(
                        physicalQuantity - plannedQuantity,
                        byCapacity));
                if (take <= 0)
                {
                    continue;
                }

                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedQuantity = checked(plannedQuantity + take);
                plannedNutrition = checked(
                    plannedNutrition + take * food.NutritionBasisPoints);
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
            }

            var result = new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                TransferredPhysicalQuantity = plannedQuantity,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = string.Empty
            };
            if (plan.Count == 0)
            {
                return result;
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodPublicReliefProcurementTransferred,
                actorPersonId ?? string.Empty,
                string.Empty,
                0,
                0,
                0,
                $"Delivered {plannedQuantity} reserved food units to the county relief granary.");
            transaction.SourceFormalMarketOrderId = formalMarketOrderId;
            transaction.SourceCountyGovernanceId = countyGovernanceId;
            for (var i = 0; i < plan.Count; i++)
            {
                var sourceBatch = plan[i].Batch;
                var reservation = FindReservation(
                    reservations, sourceBatch.Id);
                sourceBatch.Quantity = checked(
                    sourceBatch.Quantity - plan[i].Quantity);
                sourceBatch.ReservedQuantity = checked(
                    sourceBatch.ReservedQuantity - plan[i].Quantity);
                reservation.RemainingQuantity = checked(
                    reservation.RemainingQuantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    sourceBatch,
                    -plan[i].Quantity,
                    -plan[i].Quantity));
                var destinationBatch = CloneForDestination(
                    world,
                    sourceBatch,
                    plan[i].Quantity,
                    transaction.Id,
                    string.Empty,
                    string.Empty,
                    destination);
                world.ProductBatches.Add(destinationBatch);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    destinationBatch, plan[i].Quantity, 0));
            }

            sellerStorage.InventoryUnits = checked(
                sellerStorage.InventoryUnits - plannedWeight);
            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        public FoodTransferResult DispatchReservedCivilianFreight(
            WorldState world,
            string sellerFamilyId,
            string sellerStorageFacilityId,
            string buyerFamilyId,
            string transportContainerId,
            string actorPersonId,
            IList<FormalMarketBatchReservationState> reservations,
            long physicalQuantity,
            string sellOrderId,
            string civilianFreightId,
            string originCountyGovernanceId)
        {
            var sellerStorage = ValidateFamilyGranary(
                world,
                sellerFamilyId,
                sellerStorageFacilityId,
                actorPersonId,
                false);
            _ = ProductInventorySystem.FindFamily(world, buyerFamilyId);
            var container = ProductInventorySystem.FindContainer(
                world, transportContainerId);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                reservations == null || reservations.Count == 0 ||
                physicalQuantity <= 0 ||
                string.IsNullOrEmpty(sellOrderId) ||
                string.IsNullOrEmpty(civilianFreightId))
            {
                throw new InvalidOperationException(
                    "Civilian freight dispatch requires reserved formal food stock.");
            }

            var capacityWeight = Math.Max(
                0L,
                container.CapacityWeight -
                CalculateContainerWeight(world, container.Id));
            var plan = new List<FoodTransferPlanLine>();
            long plannedQuantity = 0;
            long plannedNutrition = 0;
            long plannedWeight = 0;
            for (var i = 0;
                 i < reservations.Count && plannedQuantity < physicalQuantity;
                 i++)
            {
                var reservation = reservations[i] ??
                    throw new InvalidOperationException(
                        "A civilian freight reservation cannot be null.");
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }
                var batch = FindBatch(world, reservation.BatchId);
                if (batch.OwnerFamilyId != sellerFamilyId ||
                    batch.StorageFacilityId != sellerStorageFacilityId ||
                    batch.ReservedQuantity < reservation.RemainingQuantity)
                {
                    throw new InvalidOperationException(
                        $"Invalid civilian freight reservation for {batch.Id}.");
                }
                _content.TryGetFood(
                    batch.ProductDefinitionId, out var food);
                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var take = Math.Min(
                    reservation.RemainingQuantity,
                    Math.Min(
                        physicalQuantity - plannedQuantity,
                        byCapacity));
                if (take <= 0)
                {
                    continue;
                }
                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedQuantity = checked(plannedQuantity + take);
                plannedNutrition = checked(
                    plannedNutrition + take *
                    (food == null ? 0 : food.NutritionBasisPoints));
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
            }
            if (plannedQuantity != physicalQuantity)
            {
                throw new InvalidOperationException(
                    "The civilian freight container cannot load the requested reserved quantity.");
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.CivilianFreightDispatched,
                actorPersonId ?? string.Empty,
                string.Empty,
                0,
                0,
                0,
                $"Dispatched {plannedQuantity} food units on civilian freight {civilianFreightId}.");
            transaction.SourceFormalMarketOrderId = sellOrderId;
            transaction.SourceCivilianFreightId = civilianFreightId;
            transaction.SourceCountyGovernanceId = originCountyGovernanceId;
            for (var i = 0; i < plan.Count; i++)
            {
                var source = plan[i].Batch;
                var reservation = FindReservation(reservations, source.Id);
                source.Quantity = checked(source.Quantity - plan[i].Quantity);
                source.ReservedQuantity = checked(
                    source.ReservedQuantity - plan[i].Quantity);
                reservation.RemainingQuantity = checked(
                    reservation.RemainingQuantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    source, -plan[i].Quantity, -plan[i].Quantity));
                var cargo = CloneForDestination(
                    world,
                    source,
                    plan[i].Quantity,
                    transaction.Id,
                    buyerFamilyId,
                    string.Empty,
                    container);
                world.ProductBatches.Add(cargo);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    cargo, plan[i].Quantity, 0));
            }
            sellerStorage.InventoryUnits = checked(
                sellerStorage.InventoryUnits - plannedWeight);
            transaction.FacilityInventoryDelta = -plannedWeight;
            world.InventoryTransactions.Add(transaction);
            return new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                TransferredPhysicalQuantity = plannedQuantity,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = transaction.Id
            };
        }

        public FoodTransferResult DispatchReservedPublicReliefFreight(
            WorldState world,
            string sellerFamilyId,
            string sellerStorageFacilityId,
            string buyerOrganizationId,
            string transportContainerId,
            string actorPersonId,
            IList<FormalMarketBatchReservationState> reservations,
            long physicalQuantity,
            string sellOrderId,
            string civilianFreightId,
            string originCountyGovernanceId)
        {
            var sellerStorage = ValidateFamilyGranary(
                world, sellerFamilyId, sellerStorageFacilityId,
                actorPersonId, false);
            var container = ProductInventorySystem.FindContainer(
                world, transportContainerId);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                string.IsNullOrEmpty(buyerOrganizationId) ||
                reservations == null || reservations.Count == 0 ||
                physicalQuantity <= 0 || string.IsNullOrEmpty(sellOrderId) ||
                string.IsNullOrEmpty(civilianFreightId))
            {
                throw new InvalidOperationException(
                    "Public relief freight requires reserved formal food stock and a government buyer.");
            }

            var capacityWeight = Math.Max(
                0L, container.CapacityWeight -
                    CalculateContainerWeight(world, container.Id));
            var plan = new List<FoodTransferPlanLine>();
            long plannedQuantity = 0;
            long plannedNutrition = 0;
            long plannedWeight = 0;
            for (var i = 0;
                 i < reservations.Count && plannedQuantity < physicalQuantity;
                 i++)
            {
                var reservation = reservations[i] ??
                    throw new InvalidOperationException(
                        "A public relief freight reservation cannot be null.");
                if (reservation.RemainingQuantity <= 0)
                {
                    continue;
                }
                var batch = FindBatch(world, reservation.BatchId);
                if (batch.OwnerFamilyId != sellerFamilyId ||
                    batch.StorageFacilityId != sellerStorageFacilityId ||
                    batch.ReservedQuantity < reservation.RemainingQuantity ||
                    !_content.TryGetFood(batch.ProductDefinitionId, out var food))
                {
                    throw new InvalidOperationException(
                        $"Invalid public relief freight reservation for {batch.Id}.");
                }
                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var take = Math.Min(
                    reservation.RemainingQuantity,
                    Math.Min(physicalQuantity - plannedQuantity, byCapacity));
                if (take <= 0)
                {
                    continue;
                }
                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedQuantity = checked(plannedQuantity + take);
                plannedNutrition = checked(
                    plannedNutrition + take * food.NutritionBasisPoints);
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
            }
            if (plannedQuantity != physicalQuantity)
            {
                throw new InvalidOperationException(
                    "The public relief freight container cannot load the requested quantity.");
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.CivilianFreightDispatched,
                actorPersonId ?? string.Empty,
                string.Empty,
                0, 0, 0,
                $"Dispatched {plannedQuantity} public relief food units on civilian freight {civilianFreightId}.");
            transaction.SourceFormalMarketOrderId = sellOrderId;
            transaction.SourceCivilianFreightId = civilianFreightId;
            transaction.SourceCountyGovernanceId = originCountyGovernanceId;
            for (var i = 0; i < plan.Count; i++)
            {
                var source = plan[i].Batch;
                var reservation = FindReservation(reservations, source.Id);
                source.Quantity = checked(source.Quantity - plan[i].Quantity);
                source.ReservedQuantity = checked(
                    source.ReservedQuantity - plan[i].Quantity);
                reservation.RemainingQuantity = checked(
                    reservation.RemainingQuantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    source, -plan[i].Quantity, -plan[i].Quantity));
                var cargo = CloneForDestination(
                    world, source, plan[i].Quantity, transaction.Id,
                    string.Empty, string.Empty, container,
                    buyerOrganizationId);
                world.ProductBatches.Add(cargo);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    cargo, plan[i].Quantity, 0));
            }
            sellerStorage.InventoryUnits = checked(
                sellerStorage.InventoryUnits - plannedWeight);
            transaction.FacilityInventoryDelta = -plannedWeight;
            world.InventoryTransactions.Add(transaction);
            return new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                TransferredPhysicalQuantity = plannedQuantity,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = transaction.Id
            };
        }

        public FoodTransferResult LoseCivilianFreight(
            WorldState world,
            string civilianFreightId,
            string dispatchTransactionId,
            string buyerFamilyId,
            string transportContainerId,
            string productDefinitionId,
            string actorPersonId,
            long physicalQuantity)
        {
            return RemoveCivilianCargo(
                world,
                civilianFreightId,
                dispatchTransactionId,
                buyerFamilyId,
                string.Empty,
                transportContainerId,
                productDefinitionId,
                actorPersonId,
                physicalQuantity,
                null,
                null);
        }

        public FoodTransferResult LosePublicReliefFreight(
            WorldState world,
            string civilianFreightId,
            string dispatchTransactionId,
            string buyerOrganizationId,
            string transportContainerId,
            string productDefinitionId,
            string actorPersonId,
            long physicalQuantity)
        {
            return RemoveCivilianCargo(
                world, civilianFreightId, dispatchTransactionId,
                string.Empty, buyerOrganizationId, transportContainerId,
                productDefinitionId, actorPersonId, physicalQuantity,
                null, null);
        }

        public FoodTransferResult DeliverCivilianFreight(
            WorldState world,
            string civilianFreightId,
            string dispatchTransactionId,
            string buyerFamilyId,
            string buyerStorageFacilityId,
            string transportContainerId,
            string productDefinitionId,
            string actorPersonId,
            long physicalQuantity)
        {
            var destination = ValidateFamilyGranary(
                world,
                buyerFamilyId,
                buyerStorageFacilityId,
                string.Empty,
                false);
            return RemoveCivilianCargo(
                world,
                civilianFreightId,
                dispatchTransactionId,
                buyerFamilyId,
                string.Empty,
                transportContainerId,
                productDefinitionId,
                actorPersonId,
                physicalQuantity,
                destination,
                null);
        }

        public FoodTransferResult DeliverPublicReliefFreight(
            WorldState world,
            string civilianFreightId,
            string dispatchTransactionId,
            string buyerOrganizationId,
            string destinationInventoryContainerId,
            string transportContainerId,
            string productDefinitionId,
            string actorPersonId,
            long physicalQuantity)
        {
            var destination = ProductInventorySystem.FindContainer(
                world, destinationInventoryContainerId);
            if (destination.OwnerOrganizationId != buyerOrganizationId ||
                !string.IsNullOrEmpty(destination.CarrierPersonId))
            {
                throw new InvalidOperationException(
                    "Public relief freight destination is invalid.");
            }
            return RemoveCivilianCargo(
                world, civilianFreightId, dispatchTransactionId,
                string.Empty, buyerOrganizationId, transportContainerId,
                productDefinitionId, actorPersonId, physicalQuantity,
                null, destination);
        }

        private FoodTransferResult RemoveCivilianCargo(
            WorldState world,
            string civilianFreightId,
            string dispatchTransactionId,
            string buyerFamilyId,
            string buyerOrganizationId,
            string transportContainerId,
            string productDefinitionId,
            string actorPersonId,
            long physicalQuantity,
            VillageFacilityState destination,
            InventoryContainerState organizationDestination)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            var container = ProductInventorySystem.FindContainer(
                world, transportContainerId);
            if (physicalQuantity <= 0 ||
                string.IsNullOrEmpty(civilianFreightId) ||
                string.IsNullOrEmpty(dispatchTransactionId))
            {
                throw new InvalidOperationException(
                    "Civilian freight cargo removal is invalid.");
            }
            var capacityWeight = destination == null &&
                    organizationDestination == null
                ? long.MaxValue
                : destination != null
                    ? Math.Max(0L,
                        destination.Capacity - destination.InventoryUnits)
                    : Math.Max(0L,
                        organizationDestination.CapacityWeight -
                        CalculateContainerWeight(
                            world, organizationDestination.Id));
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == buyerFamilyId &&
                    batch.OwnerOrganizationId == buyerOrganizationId &&
                    batch.InventoryContainerId == container.Id &&
                    batch.ProductDefinitionId == productDefinitionId &&
                    batch.SourceTransactionId == dispatchTransactionId &&
                    batch.Quantity > 0)
                {
                    candidates.Add(batch);
                }
            }
            candidates.Sort(CompareCivilianCargo);
            var plan = new List<FoodTransferPlanLine>();
            long plannedQuantity = 0;
            long plannedWeight = 0;
            long plannedNutrition = 0;
            for (var i = 0;
                 i < candidates.Count && plannedQuantity < physicalQuantity;
                 i++)
            {
                var batch = candidates[i];
                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var take = Math.Min(
                    batch.Quantity,
                    Math.Min(physicalQuantity - plannedQuantity, byCapacity));
                if (take <= 0)
                {
                    continue;
                }
                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedQuantity = checked(plannedQuantity + take);
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
                plannedNutrition = checked(
                    plannedNutrition + take *
                    (_content.TryGetFood(
                        batch.ProductDefinitionId, out var food)
                        ? food.NutritionBasisPoints
                        : 0));
            }
            var result = new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                TransferredPhysicalQuantity = plannedQuantity,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = string.Empty
            };
            if (plan.Count == 0)
            {
                return result;
            }
            var delivering = destination != null ||
                organizationDestination != null;
            var type = !delivering
                ? InventoryTransactionType.CivilianFreightNaturalLoss
                : InventoryTransactionType.CivilianFreightDelivered;
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                type,
                actorPersonId ?? string.Empty,
                string.Empty,
                0,
                0,
                0,
                !delivering
                    ? $"Lost {plannedQuantity} units from civilian freight {civilianFreightId}."
                    : $"Delivered {plannedQuantity} units from civilian freight {civilianFreightId}.");
            transaction.SourceCivilianFreightId = civilianFreightId;
            for (var i = 0; i < plan.Count; i++)
            {
                var source = plan[i].Batch;
                source.Quantity = checked(source.Quantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    source, -plan[i].Quantity, 0));
                if (delivering)
                {
                    var delivered = CloneForDestination(
                        world,
                        source,
                        plan[i].Quantity,
                        transaction.Id,
                        buyerFamilyId,
                        destination == null ? string.Empty : destination.Id,
                        organizationDestination,
                        buyerOrganizationId);
                    world.ProductBatches.Add(delivered);
                    transaction.Lines.Add(ProductInventorySystem.Line(
                        delivered, plan[i].Quantity, 0));
                }
            }
            if (destination != null)
            {
                destination.InventoryUnits = checked(
                    destination.InventoryUnits + plannedWeight);
                transaction.FacilityInventoryDelta = plannedWeight;
            }
            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        public long CalculateTransportQuantityCapacity(
            string productDefinitionId,
            long baseVolumeCapacity)
        {
            if (baseVolumeCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseVolumeCapacity));
            }

            var food = _content.GetFood(productDefinitionId);
            return checked(
                baseVolumeCapacity * 10_000L /
                food.VolumeBasisPoints);
        }

        public FoodConsumptionResult ConsumeFamilyFood(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string actorPersonId,
            long requiredNutritionBasisUnits)
        {
            if (requiredNutritionBasisUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredNutritionBasisUnits));
            }

            var storage = ValidateFamilyGranary(
                world, familyId, storageFacilityId, actorPersonId, true);
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == familyId &&
                    batch.StorageFacilityId == storageFacilityId &&
                    batch.Quantity > batch.ReservedQuantity &&
                    _content.TryGetFood(batch.ProductDefinitionId, out _))
                {
                    candidates.Add(batch);
                }
            }

            candidates.Sort(CompareForConsumption);
            var result = new FoodConsumptionResult
            {
                RequiredNutritionBasisUnits = requiredNutritionBasisUnits,
                InventoryTransactionId = string.Empty
            };
            if (candidates.Count == 0)
            {
                return result;
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodConsumed,
                actorPersonId,
                string.Empty,
                0,
                0,
                0,
                $"Consumed family food toward {requiredNutritionBasisUnits} nutrition basis units.");
            long removedWeight = 0;
            for (var i = 0;
                 i < candidates.Count && !result.Fulfilled;
                 i++)
            {
                var batch = candidates[i];
                var food = _content.GetFood(batch.ProductDefinitionId);
                var remaining = checked(
                    requiredNutritionBasisUnits -
                    result.ProvidedNutritionBasisUnits);
                var available = batch.Quantity - batch.ReservedQuantity;
                var take = Math.Min(
                    available,
                    DivideRoundUp(remaining, food.NutritionBasisPoints));
                if (take <= 0)
                {
                    continue;
                }

                batch.Quantity = checked(batch.Quantity - take);
                result.ConsumedPhysicalQuantity = checked(
                    result.ConsumedPhysicalQuantity + take);
                result.ProvidedNutritionBasisUnits = checked(
                    result.ProvidedNutritionBasisUnits +
                    take * food.NutritionBasisPoints);
                removedWeight = checked(
                    removedWeight + take * batch.UnitWeight);
                transaction.Lines.Add(
                    ProductInventorySystem.Line(batch, -take, 0));
            }

            if (transaction.Lines.Count == 0)
            {
                return result;
            }

            storage.InventoryUnits = checked(
                storage.InventoryUnits - removedWeight);
            transaction.FacilityInventoryDelta = -removedWeight;
            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        public FoodConsumptionResult ConsumeHouseholdReliefFood(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string actorPersonId,
            string recipientPersonId,
            long requiredNutritionBasisUnits,
            ICollection<string> sourcePickupTransactionIds,
            string sourceHouseholdReliefConsumptionId)
        {
            if (requiredNutritionBasisUnits <= 0 ||
                sourcePickupTransactionIds == null ||
                sourcePickupTransactionIds.Count == 0 ||
                string.IsNullOrEmpty(sourceHouseholdReliefConsumptionId))
            {
                throw new InvalidOperationException(
                    "Household relief consumption requires a positive, traced claim.");
            }

            var storage = ValidateFamilyGranary(
                world, familyId, storageFacilityId, actorPersonId, true);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            if (!string.IsNullOrEmpty(recipientPersonId) &&
                !family.MemberIds.Contains(recipientPersonId))
            {
                throw new InvalidOperationException(
                    "Household relief recipient must belong to the receiving family.");
            }
            var allowedSources = new HashSet<string>(
                sourcePickupTransactionIds, StringComparer.Ordinal);
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == familyId &&
                    batch.StorageFacilityId == storageFacilityId &&
                    batch.Quantity > batch.ReservedQuantity &&
                    allowedSources.Contains(batch.SourceTransactionId) &&
                    _content.TryGetFood(batch.ProductDefinitionId, out _))
                {
                    candidates.Add(batch);
                }
            }

            candidates.Sort(CompareForConsumption);
            var result = new FoodConsumptionResult
            {
                RequiredNutritionBasisUnits = requiredNutritionBasisUnits,
                InventoryTransactionId = string.Empty
            };
            if (candidates.Count == 0)
            {
                return result;
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.FoodConsumed,
                actorPersonId,
                string.Empty,
                0,
                0,
                0,
                $"Consumed household relief food for {sourceHouseholdReliefConsumptionId}.");
            transaction.SourceHouseholdReliefConsumptionId =
                sourceHouseholdReliefConsumptionId;
            transaction.HouseholdReliefRecipientPersonId = recipientPersonId;
            long removedWeight = 0;
            for (var i = 0;
                 i < candidates.Count && !result.Fulfilled;
                 i++)
            {
                var batch = candidates[i];
                var food = _content.GetFood(batch.ProductDefinitionId);
                var remaining = checked(
                    requiredNutritionBasisUnits -
                    result.ProvidedNutritionBasisUnits);
                var available = batch.Quantity - batch.ReservedQuantity;
                var take = Math.Min(
                    available,
                    DivideRoundUp(remaining, food.NutritionBasisPoints));
                if (take <= 0)
                {
                    continue;
                }

                batch.Quantity = checked(batch.Quantity - take);
                result.ConsumedPhysicalQuantity = checked(
                    result.ConsumedPhysicalQuantity + take);
                result.ProvidedNutritionBasisUnits = checked(
                    result.ProvidedNutritionBasisUnits +
                    take * food.NutritionBasisPoints);
                removedWeight = checked(
                    removedWeight + take * batch.UnitWeight);
                transaction.Lines.Add(
                    ProductInventorySystem.Line(batch, -take, 0));
            }

            if (transaction.Lines.Count == 0)
            {
                return result;
            }

            storage.InventoryUnits = checked(
                storage.InventoryUnits - removedWeight);
            transaction.FacilityInventoryDelta = -removedWeight;
            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        private VillageFacilityState ValidateFamilyGranary(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string actorPersonId,
            bool requireActor)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            var family = ProductInventorySystem.FindFamily(world, familyId);
            var storage = ProductInventorySystem.FindFacility(
                world, storageFacilityId);
            if (storage.Kind != VillageFacilityKind.HouseholdGranary ||
                storage.OwnerFamilyId != family.Id ||
                storage.InventoryUnits !=
                    ProductInventorySystem.CalculatePhysicalInventoryUnits(
                        world, storage.Id, family.Id, _content))
            {
                throw new InvalidOperationException(
                    "Food inventory requires a consistent family granary.");
            }

            if (requireActor)
            {
                var actor = ProductInventorySystem.FindPerson(
                    world, actorPersonId);
                if (!actor.IsAlive || actor.FamilyId != family.Id)
                {
                    throw new InvalidOperationException(
                        "Food inventory actor must be a living family member.");
                }
            }

            return storage;
        }

        private FoodTransferResult Transfer(
            WorldState world,
            string sourceFamilyId,
            string sourceFacilityId,
            string sourceContainerId,
            string destinationFamilyId,
            string destinationFacilityId,
            string destinationContainerId,
            string actorPersonId,
            long physicalQuantity,
            long nutritionBasisUnits,
            InventoryTransactionType transactionType,
            string sourceVillageId,
            string sourceCountyGovernanceId)
        {
            ProductInventorySystem.RequireWorld(world);
            _content.ValidateManifest(world.ProductionContentManifest);
            if (!IsTransferType(transactionType) ||
                (physicalQuantity > 0) == (nutritionBasisUnits > 0))
            {
                throw new InvalidOperationException(
                    "Food transfer requires one supported target quantity.");
            }

            if (!string.IsNullOrEmpty(actorPersonId))
            {
                _ = ProductInventorySystem.FindPerson(world, actorPersonId);
            }

            var source = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var matchesFamily = !string.IsNullOrEmpty(sourceFamilyId) &&
                    batch.OwnerFamilyId == sourceFamilyId &&
                    batch.StorageFacilityId == sourceFacilityId;
                var matchesContainer = !string.IsNullOrEmpty(sourceContainerId) &&
                    batch.InventoryContainerId == sourceContainerId;
                if ((matchesFamily || matchesContainer) &&
                    batch.Quantity > batch.ReservedQuantity &&
                    _content.TryGetFood(batch.ProductDefinitionId, out _))
                {
                    source.Add(batch);
                }
            }
            source.Sort(CompareForConsumption);

            long capacityWeight;
            VillageFacilityState destinationFacility = null;
            InventoryContainerState destinationContainer = null;
            if (!string.IsNullOrEmpty(destinationFamilyId))
            {
                destinationFacility = ProductInventorySystem.FindFacility(
                    world, destinationFacilityId);
                capacityWeight = Math.Max(
                    0L,
                    destinationFacility.Capacity -
                    destinationFacility.InventoryUnits);
            }
            else
            {
                destinationContainer = ProductInventorySystem.FindContainer(
                    world, destinationContainerId);
                capacityWeight = Math.Max(
                    0L,
                    destinationContainer.CapacityWeight -
                    CalculateContainerWeight(world, destinationContainer.Id));
            }

            var plan = new List<FoodTransferPlanLine>();
            long plannedPhysical = 0;
            long plannedNutrition = 0;
            long plannedWeight = 0;
            for (var i = 0; i < source.Count; i++)
            {
                if (physicalQuantity > 0 &&
                        plannedPhysical >= physicalQuantity ||
                    nutritionBasisUnits > 0 &&
                        plannedNutrition >= nutritionBasisUnits)
                {
                    break;
                }

                var batch = source[i];
                var food = _content.GetFood(batch.ProductDefinitionId);
                var available = batch.Quantity - batch.ReservedQuantity;
                var byCapacity = (capacityWeight - plannedWeight) /
                    batch.UnitWeight;
                var target = physicalQuantity > 0
                    ? physicalQuantity - plannedPhysical
                    : DivideRoundUp(
                        nutritionBasisUnits - plannedNutrition,
                        food.NutritionBasisPoints);
                var take = Math.Min(available, Math.Min(byCapacity, target));
                if (take <= 0)
                {
                    continue;
                }

                plan.Add(new FoodTransferPlanLine(batch, take));
                plannedPhysical = checked(plannedPhysical + take);
                plannedNutrition = checked(
                    plannedNutrition + take * food.NutritionBasisPoints);
                plannedWeight = checked(
                    plannedWeight + take * batch.UnitWeight);
            }

            var result = new FoodTransferResult
            {
                RequestedPhysicalQuantity = physicalQuantity,
                RequestedNutritionBasisUnits = nutritionBasisUnits,
                TransferredPhysicalQuantity = plannedPhysical,
                TransferredNutritionBasisUnits = plannedNutrition,
                InventoryTransactionId = string.Empty
            };
            if (plan.Count == 0)
            {
                return result;
            }

            var transaction = ProductInventorySystem.NewTransaction(
                world,
                transactionType,
                actorPersonId ?? string.Empty,
                string.Empty,
                0,
                0,
                0,
                $"Transferred {plannedPhysical} food units.");
            transaction.SourceVillageId = sourceVillageId ?? string.Empty;
            transaction.SourceCountyGovernanceId =
                sourceCountyGovernanceId ?? string.Empty;
            for (var i = 0; i < plan.Count; i++)
            {
                var sourceBatch = plan[i].Batch;
                sourceBatch.Quantity = checked(
                    sourceBatch.Quantity - plan[i].Quantity);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    sourceBatch, -plan[i].Quantity, 0));
                var destinationBatch = CloneForDestination(
                    world,
                    sourceBatch,
                    plan[i].Quantity,
                    transaction.Id,
                    destinationFamilyId,
                    destinationFacilityId,
                    destinationContainer);
                world.ProductBatches.Add(destinationBatch);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    destinationBatch, plan[i].Quantity, 0));
            }

            if (!string.IsNullOrEmpty(sourceFamilyId))
            {
                var facility = ProductInventorySystem.FindFacility(
                    world, sourceFacilityId);
                facility.InventoryUnits = checked(
                    facility.InventoryUnits - plannedWeight);
                transaction.FacilityInventoryDelta = checked(
                    transaction.FacilityInventoryDelta - plannedWeight);
            }
            if (destinationFacility != null)
            {
                destinationFacility.InventoryUnits = checked(
                    destinationFacility.InventoryUnits + plannedWeight);
                transaction.FacilityInventoryDelta = checked(
                    transaction.FacilityInventoryDelta + plannedWeight);
            }

            world.InventoryTransactions.Add(transaction);
            result.InventoryTransactionId = transaction.Id;
            return result;
        }

        private FoodInventorySummary Summarize(
            WorldState world,
            string familyId,
            string storageFacilityId,
            string inventoryContainerId)
        {
            var summary = new FoodInventorySummary();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var matchesFamily = !string.IsNullOrEmpty(familyId) &&
                    batch.OwnerFamilyId == familyId &&
                    batch.StorageFacilityId == storageFacilityId;
                var matchesContainer =
                    !string.IsNullOrEmpty(inventoryContainerId) &&
                    batch.InventoryContainerId == inventoryContainerId;
                if ((!matchesFamily && !matchesContainer) ||
                    !_content.TryGetFood(
                        batch.ProductDefinitionId, out var food))
                {
                    continue;
                }

                var quantity = batch.Quantity - batch.ReservedQuantity;
                summary.PhysicalQuantity = checked(
                    summary.PhysicalQuantity + quantity);
                summary.NutritionBasisUnits = checked(
                    summary.NutritionBasisUnits +
                    quantity * food.NutritionBasisPoints);
                summary.VolumeBasisUnits = checked(
                    summary.VolumeBasisUnits +
                    quantity * food.VolumeBasisPoints);
                summary.MarketValueBasisUnits = checked(
                    summary.MarketValueBasisUnits +
                    quantity * food.MarketValueBasisPoints);
            }

            return summary;
        }

        private static long CalculateContainerWeight(
            WorldState world,
            string containerId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId == containerId)
                {
                    total = checked(
                        total + batch.Quantity * batch.UnitWeight);
                }
            }

            return total;
        }

        private static ProductBatchState CloneForDestination(
            WorldState world,
            ProductBatchState source,
            long quantity,
            string sourceTransactionId,
            string destinationFamilyId,
            string destinationFacilityId,
            InventoryContainerState destinationContainer,
            string destinationOrganizationId = "")
        {
            var result = new ProductBatchState
            {
                Id = $"product_batch.{world.AbsoluteDay}." +
                     $"{world.ProductBatches.Count:D6}",
                ProductDefinitionId = source.ProductDefinitionId,
                OwnerFamilyId = destinationFamilyId,
                OwnerOrganizationId = !string.IsNullOrEmpty(
                        destinationOrganizationId)
                    ? destinationOrganizationId
                    : destinationContainer == null ||
                        !string.IsNullOrEmpty(destinationFamilyId)
                        ? string.Empty
                        : destinationContainer.OwnerOrganizationId,
                StorageFacilityId = destinationFacilityId,
                InventoryContainerId = destinationContainer == null
                    ? string.Empty
                    : destinationContainer.Id,
                OriginLocationId = source.OriginLocationId,
                SourceWorkOrderId = source.SourceWorkOrderId,
                SourceTransactionId = sourceTransactionId,
                CropVarietyDefinitionId = source.CropVarietyDefinitionId,
                UnitId = source.UnitId,
                UnitWeight = source.UnitWeight,
                ProducedDay = source.ProducedDay,
                Quantity = quantity,
                ReservedQuantity = 0,
                QualityBasisPoints = source.QualityBasisPoints,
                FreshnessBasisPoints = source.FreshnessBasisPoints,
                SeedVigorBasisPoints = source.SeedVigorBasisPoints,
                SeedPurityBasisPoints = source.SeedPurityBasisPoints,
                NextFoodStorageAssessmentDay = checked(
                    world.AbsoluteDay + 30)
            };
            for (var i = 0; i < source.QualityDimensions.Count; i++)
            {
                result.QualityDimensions.Add(
                    new ProductQualityDimensionState
                    {
                        QualityDimensionId = source.QualityDimensions[i]
                            .QualityDimensionId,
                        ValueBasisPoints = source.QualityDimensions[i]
                            .ValueBasisPoints
                    });
            }
            return result;
        }

        private static bool IsTransferType(InventoryTransactionType type)
        {
            return type == InventoryTransactionType.FoodTaxTransferred ||
                   type == InventoryTransactionType
                       .FoodVillageReliefTransferred ||
                   type == InventoryTransactionType
                       .FoodCountyReliefTransferred ||
                   type == InventoryTransactionType.FoodTaxRemitted;
        }

        private static VillageState FindVillage(
            WorldState world,
            string villageId)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == villageId)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException(
                $"Unknown village {villageId} for food transfer.");
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
                $"Unknown county governance {governanceId} for food transfer.");
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
                $"Unknown product batch {batchId} for food transfer.");
        }

        private static FormalMarketBatchReservationState FindReservation(
            IList<FormalMarketBatchReservationState> reservations,
            string batchId)
        {
            for (var i = 0; i < reservations.Count; i++)
            {
                if (reservations[i].BatchId == batchId)
                {
                    return reservations[i];
                }
            }

            throw new InvalidOperationException(
                $"Unknown formal market reservation {batchId}.");
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

        private int CompareForConsumption(
            ProductBatchState left,
            ProductBatchState right)
        {
            var leftFood = _content.GetFood(left.ProductDefinitionId);
            var rightFood = _content.GetFood(right.ProductDefinitionId);
            var order = leftFood.ConsumptionPriority.CompareTo(
                rightFood.ConsumptionPriority);
            if (order != 0)
            {
                return order;
            }

            order = left.ProducedDay.CompareTo(right.ProducedDay);
            return order != 0
                ? order
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static long DivideRoundUp(long numerator, int denominator)
        {
            return checked(
                numerator / denominator +
                (numerator % denominator == 0 ? 0 : 1));
        }

        private static int CompareCivilianCargo(
            ProductBatchState left,
            ProductBatchState right)
        {
            var produced = left.ProducedDay.CompareTo(right.ProducedDay);
            return produced != 0
                ? produced
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private sealed class FoodTransferPlanLine
        {
            public FoodTransferPlanLine(
                ProductBatchState batch,
                long quantity)
            {
                Batch = batch;
                Quantity = quantity;
            }

            public ProductBatchState Batch { get; }
            public long Quantity { get; }
        }
    }
}
