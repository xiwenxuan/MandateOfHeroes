using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryLogisticsLegRequest
    {
        public StableId CarrierPersonId;
        public StableId RouteId;
        public StableId DestinationLocationId;
        public string CarrierOrganizationId;
        public string SourceProvisionBatchId = string.Empty;
        public int ConvoyProvisionQuantity;
        public int DailyConvoyProvisionUse = 1;
        public string RiskPolicyId = MilitaryLogisticsRiskPolicyIds.None;
        public string ThreatOrganizationId = string.Empty;
        public List<string> EscortPersonIds = new List<string>();
    }

    public sealed class MilitaryLogisticsDispatchRequest
    {
        public StableId IssuerPersonId;
        public StableId CarrierPersonId;
        public StableId TargetArmyId;
        public StableId SourceCargoBatchId;
        public string SourceProvisionBatchId = string.Empty;
        public StableId RouteId;
        public StableId DestinationLocationId;
        public string AcquisitionMethodId;
        public string CarrierOrganizationId;
        public string LossBearerOrganizationId;
        public string LiabilityPolicyId =
            MilitaryLogisticsLiabilityPolicyIds.LossBearerCompensates;
        public string CargoConsumptionPolicyId =
            MilitaryCargoConsumptionPolicyIds.Prohibited;
        public int CargoQuantity;
        public int ConvoyProvisionQuantity;
        public int DailyConvoyProvisionUse = 1;
        public long UnitPrice;
        public bool AutoDeliverAtFinal = true;
        public List<MilitaryLogisticsLegRequest> AdditionalLegs =
            new List<MilitaryLogisticsLegRequest>();
        public string RiskPolicyId = MilitaryLogisticsRiskPolicyIds.None;
        public string ThreatOrganizationId = string.Empty;
        public List<string> EscortPersonIds = new List<string>();
    }

    public sealed class MilitaryLogisticsAudit
    {
        public int DispatchedCargo;
        public int RemainingCargo;
        public int DeliveredCargo;
        public int NaturalLoss;
        public int HostileLoss;
        public int RecoveredCargo;
        public int CargoConsumedAsProvisions;
        public int LoadedConvoyProvisions;
        public int RemainingConvoyProvisions;
        public int ConsumedConvoyProvisions;
        public long BuyerPaid;
        public long SourceReceived;

        public bool CargoBalanced =>
            DispatchedCargo == RemainingCargo + DeliveredCargo +
            NaturalLoss + HostileLoss + CargoConsumedAsProvisions;

        public bool ConvoyProvisionsBalanced =>
            LoadedConvoyProvisions == RemainingConvoyProvisions +
            ConsumedConvoyProvisions;

        public bool MoneyBalanced => BuyerPaid == SourceReceived;

        public bool IsBalanced =>
            CargoBalanced && ConvoyProvisionsBalanced && MoneyBalanced;
    }

    public sealed class MilitaryLogisticsSystem
    {
        private readonly IPersonRepository _people;
        private readonly TravelSystem _travel;
        private readonly MilitaryAuthoritySystem _authority =
            new MilitaryAuthoritySystem();
        private readonly ProductionContentRegistry _content;

        public MilitaryLogisticsSystem(
            ProductionContentRegistry content = null,
            IPersonRepository people = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _people = people;
            _travel = new TravelSystem(people);
        }

        public MilitaryLogisticsOrderState Dispatch(
            WorldState world,
            MilitaryLogisticsDispatchRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            world.Validate();
            if (request.CargoQuantity <= 0 ||
                request.ConvoyProvisionQuantity < 0 ||
                request.DailyConvoyProvisionUse <= 0 ||
                request.UnitPrice < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request), "Logistics quantities and prices are invalid.");
            }

            _ = new StableId(request.AcquisitionMethodId);
            _ = new StableId(request.CarrierOrganizationId);
            _ = new StableId(request.LossBearerOrganizationId);
            _ = new StableId(request.LiabilityPolicyId);
            _ = new StableId(request.CargoConsumptionPolicyId);
            ValidateRiskContract(
                world, request.RiskPolicyId, request.ThreatOrganizationId);

            var army = FindArmy(world, request.TargetArmyId.Value);
            if (_authority.GetAuthority(
                    world, request.IssuerPersonId, request.TargetArmyId) <
                MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The issuer lacks army logistics authority.");
            }

            var carrier = PeopleFor(world).GetRequired(
                request.CarrierPersonId.Value);
            var transportContainer = FindContainerByCarrier(
                world, carrier.Id);
            if (!carrier.IsAlive ||
                carrier.LocationId != transportContainer.LocationId ||
                transportContainer.OwnerOrganizationId !=
                    request.CarrierOrganizationId ||
                !HasMembership(
                    world, carrier.Id, request.CarrierOrganizationId))
            {
                throw new InvalidOperationException(
                    "The carrier, carrier organization and transport container are invalid.");
            }
            var firstEscorts = ValidateEscortPeople(
                world,
                request.EscortPersonIds,
                request.CarrierOrganizationId,
                carrier.LocationId,
                carrier.Id);

            var cargoBatch = FindBatch(
                world, request.SourceCargoBatchId.Value);
            var sourceContainer = FindContainer(
                world, cargoBatch.InventoryContainerId);
            if (cargoBatch.Quantity - cargoBatch.ReservedQuantity <
                    request.CargoQuantity ||
                sourceContainer.LocationId != carrier.LocationId)
            {
                throw new InvalidOperationException(
                    "The source cargo is unavailable or not co-located.");
            }

            var cargoProduct = _content.GetProduct(
                cargoBatch.ProductDefinitionId);
            if (!cargoProduct.CategoryTags.Contains("product.food") ||
                !cargoProduct.CategoryTags.Contains(
                    "product.military_supply"))
            {
                throw new InvalidOperationException(
                    "This logistics slice only accepts food tagged for military supply.");
            }

            ProductBatchState provisionBatch = null;
            ProductDefinition provisionProduct = null;
            if (request.ConvoyProvisionQuantity > 0)
            {
                if (string.IsNullOrWhiteSpace(
                        request.SourceProvisionBatchId))
                {
                    throw new InvalidOperationException(
                        "A convoy provision batch is required.");
                }

                provisionBatch = FindBatch(
                    world, request.SourceProvisionBatchId);
                provisionProduct = _content.GetProduct(
                    provisionBatch.ProductDefinitionId);
                var requiredFromProvisionBatch =
                    provisionBatch.Id == cargoBatch.Id
                        ? checked((long)request.CargoQuantity +
                            request.ConvoyProvisionQuantity)
                        : request.ConvoyProvisionQuantity;
                if (provisionBatch.Quantity -
                        provisionBatch.ReservedQuantity <
                        requiredFromProvisionBatch ||
                    provisionBatch.OwnerOrganizationId !=
                        request.CarrierOrganizationId ||
                    FindContainer(world, provisionBatch.InventoryContainerId)
                        .LocationId != carrier.LocationId ||
                    !provisionProduct.CategoryTags.Contains("product.food"))
                {
                    throw new InvalidOperationException(
                        "The carrier lacks valid self-provisions.");
                }
            }

            var sourceOrganization = FindOrganization(
                world, cargoBatch.OwnerOrganizationId);
            var buyerOrganization = FindOrganization(
                world, army.OrganizationId);
            _ = FindOrganization(world, request.CarrierOrganizationId);
            _ = FindOrganization(world, request.LossBearerOrganizationId);
            if (request.LiabilityPolicyId !=
                    MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk &&
                request.LiabilityPolicyId !=
                    MilitaryLogisticsLiabilityPolicyIds
                        .LossBearerCompensates &&
                request.LiabilityPolicyId !=
                    MilitaryLogisticsLiabilityPolicyIds
                        .LegacyNoRetroactiveSettlement)
            {
                throw new InvalidOperationException(
                    "The logistics liability policy is unsupported.");
            }
            var method = ResolveMethod(
                request.AcquisitionMethodId,
                buyerOrganization.Id,
                sourceOrganization.Id,
                request.UnitPrice,
                request.CargoQuantity);
            var totalPaid = checked(
                request.UnitPrice * request.CargoQuantity);
            if (method.RequiresPayment && buyerOrganization.Treasury < totalPaid)
            {
                throw new InvalidOperationException(
                    "The buyer organization lacks logistics funds.");
            }

            var route = FindRoute(world, request.RouteId.Value);
            if (!RouteConnects(
                    route,
                    carrier.LocationId,
                    request.DestinationLocationId.Value))
            {
                throw new InvalidOperationException(
                    "The first logistics route is invalid.");
            }

            var cargoWeight = checked(
                (long)cargoProduct.BaseWeight * request.CargoQuantity);
            var provisionWeight = provisionProduct == null
                ? 0
                : checked((long)provisionProduct.BaseWeight *
                    request.ConvoyProvisionQuantity);
            var additionalLegs = ValidateAdditionalLegs(
                world,
                request,
                cargoWeight,
                provisionWeight,
                provisionProduct == null ? 0 : provisionProduct.BaseWeight);
            var finalDestinationId = additionalLegs.Count == 0
                ? request.DestinationLocationId.Value
                : additionalLegs[additionalLegs.Count - 1]
                    .Request.DestinationLocationId.Value;
            if (!ArmyCanReceiveAt(world, army, finalDestinationId))
            {
                throw new InvalidOperationException(
                    "The final army rendezvous is invalid.");
            }
            var weightAfterDispatch = checked(
                CalculateTransportLoad(world, transportContainer.Id) +
                cargoWeight + provisionWeight -
                RemovedBatchWeight(
                    cargoBatch,
                    transportContainer.Id,
                    request.CargoQuantity) -
                RemovedBatchWeight(
                    provisionBatch,
                    transportContainer.Id,
                    request.ConvoyProvisionQuantity));
            if (weightAfterDispatch > transportContainer.CapacityWeight)
            {
                throw new InvalidOperationException(
                    "The transport container lacks capacity.");
            }

            var origin = FindLocation(world, carrier.LocationId);
            var appliedPublicOrderDelta = Math.Max(
                -origin.PublicOrderBasisPoints,
                method.OriginPublicOrderDelta);
            var attachedMarch = FindArmyMarch(
                world,
                army.Id,
                request.RouteId.Value,
                request.DestinationLocationId.Value);
            var carrierTravelsWithArmy = HasActiveMilitaryService(
                world, carrier.Id, army.Id);
            if (carrierTravelsWithArmy && additionalLegs.Count > 0)
            {
                throw new InvalidOperationException(
                    "A multi-leg freight plan requires independent carriers.");
            }
            if (carrierTravelsWithArmy && firstEscorts.Count > 0)
            {
                throw new InvalidOperationException(
                    "An army-attached freight leg cannot start independent escort journeys.");
            }
            if (carrierTravelsWithArmy && attachedMarch == null)
            {
                throw new InvalidOperationException(
                    "A serving soldier can haul freight only with the target army march.");
            }

            JourneyState journey = null;
            if (!carrierTravelsWithArmy)
            {
                journey = _travel.StartJourney(
                    world,
                    request.CarrierPersonId,
                    request.RouteId,
                    request.DestinationLocationId,
                    TravelMode.Caravan);
            }
            var firstEscortJourneys = carrierTravelsWithArmy
                ? new List<JourneyState>()
                : StartEscortJourneys(
                    world,
                    firstEscorts,
                    request.RouteId,
                    request.DestinationLocationId);
            var order = new MilitaryLogisticsOrderState
            {
                Id = $"military_logistics.{world.AbsoluteDay}." +
                     $"{world.MilitaryLogisticsOrders.Count}",
                CreatedDay = world.AbsoluteDay,
                AcquisitionMethodId = request.AcquisitionMethodId,
                CargoConsumptionPolicyId =
                    request.CargoConsumptionPolicyId,
                BuyerOrganizationId = buyerOrganization.Id,
                SourceOrganizationId = sourceOrganization.Id,
                CarrierOrganizationId = request.CarrierOrganizationId,
                LossBearerOrganizationId = request.LossBearerOrganizationId,
                LiabilityPolicyId = request.LiabilityPolicyId,
                IssuerPersonId = request.IssuerPersonId.Value,
                CarrierPersonId = carrier.Id,
                TargetArmyId = army.Id,
                CargoProductDefinitionId = cargoBatch.ProductDefinitionId,
                SourceCargoBatchId = cargoBatch.Id,
                SourceProvisionBatchId = provisionBatch == null
                    ? string.Empty
                    : provisionBatch.Id,
                SourceInventoryContainerId = sourceContainer.Id,
                TransportInventoryContainerId = transportContainer.Id,
                RouteId = route.Id,
                JourneyId = journey == null ? string.Empty : journey.Id,
                ArmyMarchId = !carrierTravelsWithArmy
                    ? string.Empty
                    : attachedMarch.Id,
                OriginLocationId = carrier.LocationId,
                DestinationLocationId = request.DestinationLocationId.Value,
                FinalDestinationLocationId = finalDestinationId,
                CurrentLegSequence = 0,
                PlannedLegCount = checked(additionalLegs.Count + 1),
                AutoDeliverAtFinal = request.AutoDeliverAtFinal,
                DispatchedCargoQuantity = request.CargoQuantity,
                RemainingCargoQuantity = request.CargoQuantity,
                ConvoyProvisionsLoaded = request.ConvoyProvisionQuantity,
                ConvoyProvisionsRemaining = request.ConvoyProvisionQuantity,
                DailyConvoyProvisionUse = request.DailyConvoyProvisionUse,
                DailyNaturalLossBasisPoints =
                    cargoProduct.PerishabilityBasisPoints,
                CargoUnitWeightAtDispatch = cargoProduct.BaseWeight,
                ConvoyProvisionUnitWeightAtDispatch =
                    provisionProduct == null ? 0 : provisionProduct.BaseWeight,
                CargoQualityBasisPointsAtDispatch =
                    cargoBatch.QualityBasisPoints,
                CargoFreshnessBasisPointsAtDispatch =
                    cargoBatch.FreshnessBasisPoints,
                CargoQualityDimensionsAtDispatch =
                    CopyQuality(cargoBatch.QualityDimensions),
                UnitPrice = request.UnitPrice,
                TotalPaid = totalPaid,
                OriginPublicOrderDelta = appliedPublicOrderDelta,
                Status = MilitaryLogisticsStatus.InTransit
            };

            cargoBatch.Quantity = checked(
                cargoBatch.Quantity - request.CargoQuantity);
            if (provisionBatch != null)
            {
                provisionBatch.Quantity = checked(
                    provisionBatch.Quantity -
                    request.ConvoyProvisionQuantity);
            }

            for (var i = 0; i < additionalLegs.Count; i++)
            {
                var reservedBatch = additionalLegs[i].ProvisionBatch;
                if (reservedBatch != null)
                {
                    reservedBatch.ReservedQuantity = checked(
                        reservedBatch.ReservedQuantity +
                        additionalLegs[i].Request.ConvoyProvisionQuantity);
                }
            }

            if (totalPaid > 0)
            {
                buyerOrganization.Treasury = checked(
                    buyerOrganization.Treasury - totalPaid);
                sourceOrganization.Treasury = checked(
                    sourceOrganization.Treasury + totalPaid);
            }

            origin.PublicOrderBasisPoints = checked(
                origin.PublicOrderBasisPoints + appliedPublicOrderDelta);
            world.MilitaryLogisticsOrders.Add(order);
            AddLegStates(
                world,
                order,
                request,
                carrier,
                transportContainer,
                route,
                journey,
                additionalLegs);
            AddEscortStates(
                world,
                order,
                request,
                firstEscorts,
                firstEscortJourneys,
                additionalLegs);
            AddPlannedProvisionReservations(
                world, order, additionalLegs);
            AddDispatchInventoryTransaction(
                world, order, cargoBatch, provisionBatch);
            world.MilitaryLogisticsLedgerEntries.Add(
                new MilitaryLogisticsLedgerEntryState
                {
                    Id = $"military_logistics_ledger.{order.Id}.dispatch",
                    Day = world.AbsoluteDay,
                    Type = MilitaryLogisticsLedgerType.Dispatch,
                    LogisticsOrderId = order.Id,
                    ActorPersonId = carrier.Id,
                    CargoDispatchedDelta = request.CargoQuantity,
                    CargoRemainingDelta = request.CargoQuantity,
                    ConvoyProvisionsLoadedDelta =
                        request.ConvoyProvisionQuantity,
                    ConvoyProvisionsRemainingDelta =
                        request.ConvoyProvisionQuantity,
                    BuyerMoneyDelta = -totalPaid,
                    SourceMoneyDelta = totalPaid,
                    OriginPublicOrderDelta = appliedPublicOrderDelta,
                    Summary =
                        "Military freight dispatched with cargo and separate convoy provisions."
                });
            world.Validate();
            return order;
        }

        public ISet<string> ResolveDailyTransit(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var provisionedCarriers = new HashSet<string>(
                StringComparer.Ordinal);
            var orders = new List<MilitaryLogisticsOrderState>(
                world.MilitaryLogisticsOrders);
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Status != MilitaryLogisticsStatus.InTransit ||
                    !HasActiveMovement(world, order))
                {
                    continue;
                }

                ConsumeConvoyProvisions(world, order, provisionedCarriers);
                ApplyNaturalLoss(world, order);
                ResolveHostileRisk(world, order);
            }

            world.Validate();
            return provisionedCarriers;
        }

        public void ResolveArrivals(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var orders = new List<MilitaryLogisticsOrderState>(
                world.MilitaryLogisticsOrders);
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var changed = false;
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Status == MilitaryLogisticsStatus.Delivered ||
                    HasActiveMovement(world, order))
                {
                    continue;
                }

                var army = FindArmy(world, order.TargetArmyId);
                var container = FindContainer(
                    world, order.TransportInventoryContainerId);
                var currentLeg = FindCurrentLeg(world, order);
                if (currentLeg != null &&
                    order.CurrentLegSequence < order.PlannedLegCount - 1)
                {
                    order.Status = MilitaryLogisticsStatus.AwaitingHandoff;
                    currentLeg.Status =
                        MilitaryLogisticsLegStatus.AwaitingHandoff;
                    changed = true;
                    continue;
                }

                if (army.LocationId != order.DestinationLocationId ||
                    container.LocationId != order.DestinationLocationId)
                {
                    order.Status = MilitaryLogisticsStatus.AwaitingArmy;
                    if (currentLeg != null)
                    {
                        currentLeg.Status =
                            MilitaryLogisticsLegStatus.AwaitingReceipt;
                    }
                    changed = true;
                    continue;
                }

                order.Status = MilitaryLogisticsStatus.AwaitingArmy;
                if (currentLeg != null)
                {
                    currentLeg.Status =
                        MilitaryLogisticsLegStatus.AwaitingReceipt;
                }
                if (order.AutoDeliverAtFinal)
                {
                    Deliver(world, order, army, order.RemainingCargoQuantity);
                }
                changed = true;
            }

            if (changed)
            {
                world.Validate();
            }
        }

        public void Handoff(WorldState world, string orderId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var order = FindOrder(world, orderId);
            if (order.Status != MilitaryLogisticsStatus.AwaitingHandoff ||
                order.CurrentLegSequence >= order.PlannedLegCount - 1)
            {
                throw new InvalidOperationException(
                    "The logistics order is not awaiting a planned handoff.");
            }

            var current = FindLeg(
                world, order.Id, order.CurrentLegSequence);
            var next = FindLeg(
                world, order.Id, order.CurrentLegSequence + 1);
            var currentCarrier = PeopleFor(world).GetRequired(
                current.CarrierPersonId);
            var nextCarrier = PeopleFor(world).GetRequired(
                next.CarrierPersonId);
            var currentContainer = FindContainer(
                world, current.TransportInventoryContainerId);
            var nextContainer = FindContainer(
                world, next.TransportInventoryContainerId);
            var currentEscortStates = FindEscortsForLeg(
                world, order.Id, current.Sequence);
            for (var escortIndex = 0;
                 escortIndex < currentEscortStates.Count;
                 escortIndex++)
            {
                var escort = PeopleFor(world).GetRequired(
                    currentEscortStates[escortIndex].PersonId);
                if (currentEscortStates[escortIndex].Status !=
                        MilitaryLogisticsEscortStatus.Arrived ||
                    escort.LocationId != current.DestinationLocationId)
                {
                    throw new InvalidOperationException(
                        "The current leg escort has not reached the handoff.");
                }
            }

            var nextEscortStates = FindEscortsForLeg(
                world, order.Id, next.Sequence);
            var nextEscortIds = new List<string>(nextEscortStates.Count);
            for (var escortIndex = 0;
                 escortIndex < nextEscortStates.Count;
                 escortIndex++)
            {
                nextEscortIds.Add(nextEscortStates[escortIndex].PersonId);
            }
            var nextEscortPeople = ValidateEscortPeople(
                world,
                nextEscortIds,
                next.CarrierOrganizationId,
                next.OriginLocationId,
                next.CarrierPersonId);
            if (!nextCarrier.IsAlive ||
                currentCarrier.LocationId != current.DestinationLocationId ||
                nextCarrier.LocationId != current.DestinationLocationId ||
                currentContainer.LocationId != current.DestinationLocationId ||
                nextContainer.LocationId != current.DestinationLocationId)
            {
                throw new InvalidOperationException(
                    "Both carriers and containers must meet at the handoff location.");
            }

            ProductBatchState provisionBatch = null;
            ProductDefinition provisionProduct = null;
            if (next.PlannedProvisionQuantity > 0)
            {
                provisionBatch = FindBatch(world, next.ProvisionBatchId);
                provisionProduct = _content.GetProduct(
                    provisionBatch.ProductDefinitionId);
                if (provisionBatch.ReservedQuantity <
                        next.PlannedProvisionQuantity ||
                    provisionBatch.Quantity < next.PlannedProvisionQuantity)
                {
                    throw new InvalidOperationException(
                        "The reserved handoff provisions are unavailable.");
                }
            }

            var provisionUnitWeight = provisionProduct == null
                ? order.ConvoyProvisionUnitWeightAtDispatch
                : provisionProduct.BaseWeight;
            var nextLoad = checked(
                CalculateTransportLoad(world, nextContainer.Id) +
                (long)order.RemainingCargoQuantity *
                    order.CargoUnitWeightAtDispatch +
                (long)(order.ConvoyProvisionsRemaining +
                    next.PlannedProvisionQuantity) * provisionUnitWeight);
            if (nextLoad > nextContainer.CapacityWeight)
            {
                throw new InvalidOperationException(
                    "The receiving transport container lacks handoff capacity.");
            }

            var journey = _travel.StartJourney(
                world,
                new StableId(next.CarrierPersonId),
                new StableId(next.RouteId),
                new StableId(next.DestinationLocationId),
                TravelMode.Caravan);
            var nextEscortJourneys = StartEscortJourneys(
                world,
                nextEscortPeople,
                new StableId(next.RouteId),
                new StableId(next.DestinationLocationId));
            if (provisionBatch != null)
            {
                provisionBatch.ReservedQuantity = checked(
                    provisionBatch.ReservedQuantity -
                    next.PlannedProvisionQuantity);
                provisionBatch.Quantity = checked(
                    provisionBatch.Quantity -
                    next.PlannedProvisionQuantity);
                order.ConvoyProvisionsLoaded = checked(
                    order.ConvoyProvisionsLoaded +
                    next.PlannedProvisionQuantity);
                order.ConvoyProvisionsRemaining = checked(
                    order.ConvoyProvisionsRemaining +
                    next.PlannedProvisionQuantity);
                order.ConvoyProvisionUnitWeightAtDispatch =
                    provisionProduct.BaseWeight;
                world.InventoryTransactions.Add(
                    new InventoryTransactionState
                    {
                        Id = $"inventory_transaction.{order.Id}.handoff." +
                             $"{next.Sequence}",
                        Day = world.AbsoluteDay,
                        Type = InventoryTransactionType
                            .MilitaryLogisticsHandoffLoaded,
                        ActorPersonId = next.CarrierPersonId,
                        SourceMilitaryLogisticsOrderId = order.Id,
                        Lines = new List<InventoryTransactionLineState>
                        {
                            Line(
                                provisionBatch,
                                -next.PlannedProvisionQuantity)
                        },
                        Summary =
                            "Reserved provisions loaded by the receiving carrier at handoff."
                    });
                world.InventoryTransactions[world.InventoryTransactions.Count - 1]
                    .Lines[0].ReservedQuantityDelta =
                        -next.PlannedProvisionQuantity;
            }

            current.CargoTransferredQuantity =
                order.RemainingCargoQuantity;
            current.CompletedDay = world.AbsoluteDay;
            current.Status = MilitaryLogisticsLegStatus.Completed;
            next.CargoReceivedQuantity = order.RemainingCargoQuantity;
            next.LoadedProvisionQuantity = next.PlannedProvisionQuantity;
            next.StartedDay = world.AbsoluteDay;
            next.JourneyId = journey.Id;
            next.Status = MilitaryLogisticsLegStatus.InTransit;
            for (var escortIndex = 0;
                 escortIndex < nextEscortStates.Count;
                 escortIndex++)
            {
                nextEscortStates[escortIndex].JourneyId =
                    nextEscortJourneys[escortIndex].Id;
                nextEscortStates[escortIndex].EscortPowerAtDeparture =
                    CalculateEscortPower(nextEscortPeople[escortIndex]);
                nextEscortStates[escortIndex].StartedDay = world.AbsoluteDay;
                nextEscortStates[escortIndex].Status =
                    MilitaryLogisticsEscortStatus.InTransit;
            }
            order.CurrentLegSequence = next.Sequence;
            order.CarrierPersonId = next.CarrierPersonId;
            order.CarrierOrganizationId = next.CarrierOrganizationId;
            order.TransportInventoryContainerId =
                next.TransportInventoryContainerId;
            order.SourceProvisionBatchId = next.ProvisionBatchId;
            order.RouteId = next.RouteId;
            order.JourneyId = journey.Id;
            order.ArmyMarchId = string.Empty;
            order.OriginLocationId = next.OriginLocationId;
            order.DestinationLocationId = next.DestinationLocationId;
            order.DailyConvoyProvisionUse = next.DailyProvisionUse;
            order.Status = MilitaryLogisticsStatus.InTransit;
            world.MilitaryLogisticsLedgerEntries.Add(
                new MilitaryLogisticsLedgerEntryState
                {
                    Id = $"military_logistics_ledger.{order.Id}.handoff." +
                         $"{next.Sequence}",
                    Day = world.AbsoluteDay,
                    Type = MilitaryLogisticsLedgerType.Handoff,
                    LogisticsOrderId = order.Id,
                    ActorPersonId = next.CarrierPersonId,
                    ConvoyProvisionsLoadedDelta =
                        next.PlannedProvisionQuantity,
                    ConvoyProvisionsRemainingDelta =
                        next.PlannedProvisionQuantity,
                    Summary =
                        "Cargo custody transferred to the next co-located carrier."
                });
            world.Validate();
        }

        public int DeliverPartial(
            WorldState world,
            string orderId,
            int requestedQuantity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (requestedQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedQuantity));
            }

            world.Validate();
            var order = FindOrder(world, orderId);
            var army = FindArmy(world, order.TargetArmyId);
            var container = FindContainer(
                world, order.TransportInventoryContainerId);
            if (order.Status != MilitaryLogisticsStatus.AwaitingArmy ||
                order.CurrentLegSequence != order.PlannedLegCount - 1 ||
                order.DestinationLocationId !=
                    order.FinalDestinationLocationId ||
                army.LocationId != order.FinalDestinationLocationId ||
                container.LocationId != order.FinalDestinationLocationId)
            {
                throw new InvalidOperationException(
                    "The freight is not ready for final receipt.");
            }

            var delivered = Math.Min(
                requestedQuantity, order.RemainingCargoQuantity);
            Deliver(world, order, army, delivered);
            world.Validate();
            return delivered;
        }

        public int AttemptArmyRecovery(
            WorldState world,
            StableId issuerPersonId,
            string incidentId,
            IList<string> participantPersonIds)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _ = new StableId(incidentId);
            world.Validate();
            var incident = FindIncident(world, incidentId);
            var order = FindOrder(world, incident.LogisticsOrderId);
            var leg = FindCurrentLeg(world, order);
            var custody = incident.SeizedCargoQuantity -
                incident.RecoveredCargoQuantity;
            if (incident.OutcomeId !=
                    MilitaryLogisticsIncidentOutcomeIds.CargoSeized ||
                custody <= 0 ||
                order.Status != MilitaryLogisticsStatus.InTransit ||
                leg == null || leg.Id != incident.LogisticsLegId ||
                HasRecoveryClash(world, incident.Id))
            {
                throw new InvalidOperationException(
                    "The intercepted cargo is not eligible for recovery.");
            }

            if (_authority.GetAuthority(
                    world, issuerPersonId, new StableId(order.TargetArmyId)) <
                MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The issuer lacks authority to detach a recovery party.");
            }

            var march = FindArmyMarch(
                world,
                order.TargetArmyId,
                incident.RouteId,
                leg.DestinationLocationId);
            if (march == null)
            {
                throw new InvalidOperationException(
                    "The target army is not marching on the intercepted route.");
            }

            var participants = ValidateRecoveryParticipants(
                world, order.TargetArmyId, participantPersonIds);
            var defenderPower = CalculateGroupPower(participants);
            var random = new NamedRandom(world.MasterSeed);
            var incidentStableId = new StableId(incident.Id);
            var threatVariation = random.Range(
                "military_logistics.recovery",
                incidentStableId,
                world.AbsoluteDay,
                "threat.0",
                9_000,
                11_001);
            var effectiveThreat = Math.Max(
                1,
                (int)((long)incident.ThreatPower *
                    threatVariation / 10_000));
            var succeeded = defenderPower >= effectiveThreat;
            var recovered = succeeded ? custody : 0;

            _authority.IssueOrder(
                world,
                issuerPersonId,
                new StableId(order.TargetArmyId),
                MilitaryOrderType.RecoverLogistics,
                MilitaryAuthorityLevel.Army,
                targetLocationId: leg.DestinationLocationId);

            var clash = new MilitaryLogisticsClashState
            {
                Id = $"military_logistics_clash.{incident.Id}.recovery.0",
                Day = world.AbsoluteDay,
                LogisticsOrderId = order.Id,
                LogisticsLegId = leg.Id,
                IncidentId = incident.Id,
                TypeId = MilitaryLogisticsClashTypeIds.RecoveryAttempt,
                OutcomeId = succeeded
                    ? MilitaryLogisticsClashOutcomeIds.CargoRecovered
                    : MilitaryLogisticsClashOutcomeIds.RecoveryFailed,
                IssuerPersonId = issuerPersonId.Value,
                DefenderOrganizationId =
                    FindArmy(world, order.TargetArmyId).OrganizationId,
                DefenderPower = defenderPower,
                ThreatPower = effectiveThreat,
                CargoRecoveredQuantity = recovered,
                Summary = succeeded
                    ? "An authorized army detachment recovered the intercepted cargo."
                    : "The authorized recovery detachment failed to retake the cargo."
            };
            for (var i = 0; i < participants.Count; i++)
            {
                clash.DefenderPersonIds.Add(participants[i].Id);
            }
            ApplyClashInjuries(
                world,
                clash,
                participants,
                !succeeded,
                "recovery");
            world.MilitaryLogisticsClashes.Add(clash);

            if (recovered > 0)
            {
                incident.RecoveredCargoQuantity = checked(
                    incident.RecoveredCargoQuantity + recovered);
                order.RemainingCargoQuantity = checked(
                    order.RemainingCargoQuantity + recovered);
                order.HostileLossQuantity = checked(
                    order.HostileLossQuantity - recovered);
                order.RecoveredCargoQuantity = checked(
                    order.RecoveredCargoQuantity + recovered);
                leg.HostileLossQuantity = checked(
                    leg.HostileLossQuantity - recovered);
                leg.RecoveredCargoQuantity = checked(
                    leg.RecoveredCargoQuantity + recovered);
                world.MilitaryLogisticsLedgerEntries.Add(
                    new MilitaryLogisticsLedgerEntryState
                    {
                        Id = $"military_logistics_ledger.{order.Id}." +
                             $"hostile_recovery.{incident.Id}",
                        Day = world.AbsoluteDay,
                        Type = MilitaryLogisticsLedgerType
                            .HostileCargoRecovered,
                        LogisticsOrderId = order.Id,
                        ActorPersonId = issuerPersonId.Value,
                        CargoRemainingDelta = recovered,
                        CargoHostileLossDelta = -recovered,
                        CargoRecoveredDelta = recovered,
                        Summary =
                            "Intercepted cargo returned to the active freight manifest."
                    });
            }

            world.Validate();
            return recovered;
        }

        public MilitaryLogisticsAudit Audit(
            WorldState world,
            string orderId)
        {
            var order = FindOrder(world, orderId);
            var audit = new MilitaryLogisticsAudit();
            for (var i = 0;
                 i < world.MilitaryLogisticsLedgerEntries.Count;
                 i++)
            {
                var entry = world.MilitaryLogisticsLedgerEntries[i];
                if (entry.LogisticsOrderId != order.Id)
                {
                    continue;
                }

                audit.DispatchedCargo += entry.CargoDispatchedDelta;
                audit.RemainingCargo += entry.CargoRemainingDelta;
                audit.DeliveredCargo += entry.CargoDeliveredDelta;
                audit.NaturalLoss += entry.CargoNaturalLossDelta;
                audit.HostileLoss += entry.CargoHostileLossDelta;
                audit.RecoveredCargo += entry.CargoRecoveredDelta;
                audit.CargoConsumedAsProvisions +=
                    entry.CargoConsumedAsProvisionsDelta;
                audit.LoadedConvoyProvisions +=
                    entry.ConvoyProvisionsLoadedDelta;
                audit.RemainingConvoyProvisions +=
                    entry.ConvoyProvisionsRemainingDelta;
                audit.ConsumedConvoyProvisions +=
                    entry.ConvoyProvisionsConsumedDelta;
                audit.BuyerPaid -= entry.BuyerMoneyDelta;
                audit.SourceReceived += entry.SourceMoneyDelta;
            }

            return audit;
        }

        private static void ConsumeConvoyProvisions(
            WorldState world,
            MilitaryLogisticsOrderState order,
            ISet<string> provisionedCarriers)
        {
            if (order.ConvoyProvisionsRemaining <= 0)
            {
                return;
            }

            var consumed = Math.Min(
                order.DailyConvoyProvisionUse,
                order.ConvoyProvisionsRemaining);
            order.ConvoyProvisionsRemaining -= consumed;
            order.ConvoyProvisionsConsumed = checked(
                order.ConvoyProvisionsConsumed + consumed);
            var leg = FindCurrentLeg(world, order);
            if (leg != null)
            {
                leg.ConsumedProvisionQuantity = checked(
                    leg.ConsumedProvisionQuantity + consumed);
            }
            provisionedCarriers.Add(order.CarrierPersonId);
            world.MilitaryLogisticsLedgerEntries.Add(
                new MilitaryLogisticsLedgerEntryState
                {
                    Id = $"military_logistics_ledger.{order.Id}.provision." +
                         $"{world.AbsoluteDay}",
                    Day = world.AbsoluteDay,
                    Type = MilitaryLogisticsLedgerType.ConvoyProvisionConsumed,
                    LogisticsOrderId = order.Id,
                    ActorPersonId = order.CarrierPersonId,
                    ConvoyProvisionsRemainingDelta = -consumed,
                    ConvoyProvisionsConsumedDelta = consumed,
                    Summary = "Convoy consumed its separately loaded provisions."
                });
        }

        private static void ApplyNaturalLoss(
            WorldState world,
            MilitaryLogisticsOrderState order)
        {
            if (order.RemainingCargoQuantity <= 0 ||
                order.DailyNaturalLossBasisPoints <= 0)
            {
                return;
            }

            var numerator = checked(
                (long)order.RemainingCargoQuantity *
                order.DailyNaturalLossBasisPoints +
                order.NaturalLossRemainderBasisPoints);
            var lost = (int)Math.Min(
                order.RemainingCargoQuantity,
                numerator / 10_000);
            order.NaturalLossRemainderBasisPoints = numerator % 10_000;
            if (lost <= 0)
            {
                return;
            }

            order.RemainingCargoQuantity -= lost;
            order.NaturalLossQuantity = checked(
                order.NaturalLossQuantity + lost);
            var leg = FindCurrentLeg(world, order);
            if (leg != null)
            {
                leg.NaturalLossQuantity = checked(
                    leg.NaturalLossQuantity + lost);
            }
            world.MilitaryLogisticsLedgerEntries.Add(
                new MilitaryLogisticsLedgerEntryState
                {
                    Id = $"military_logistics_ledger.{order.Id}.loss." +
                         $"{world.AbsoluteDay}",
                    Day = world.AbsoluteDay,
                    Type = MilitaryLogisticsLedgerType.NaturalLoss,
                    LogisticsOrderId = order.Id,
                    ActorPersonId = order.CarrierPersonId,
                    CargoRemainingDelta = -lost,
                    CargoNaturalLossDelta = lost,
                    Summary =
                        "Cargo quantity lost to snapshotted natural perishability."
                });
        }

        private void ResolveHostileRisk(
            WorldState world,
            MilitaryLogisticsOrderState order)
        {
            var leg = FindCurrentLeg(world, order);
            if (leg == null ||
                leg.RiskPolicyId == MilitaryLogisticsRiskPolicyIds.None ||
                order.RemainingCargoQuantity <= 0)
            {
                return;
            }

            var incidentId = $"military_logistics_incident.{order.Id}." +
                             $"{leg.Sequence}.{world.AbsoluteDay}";
            for (var i = 0; i < world.MilitaryLogisticsIncidents.Count; i++)
            {
                if (world.MilitaryLogisticsIncidents[i].Id == incidentId)
                {
                    return;
                }
            }

            var route = FindRoute(world, leg.RouteId);
            var routeRisk = Math.Max(
                0, 10_000 - route.SecurityBasisPoints);
            var cargoAttraction = Math.Min(
                2_500, order.RemainingCargoQuantity * 10);
            var attackChance = Math.Min(
                10_000, routeRisk + cargoAttraction);
            var random = new NamedRandom(world.MasterSeed);
            var entityId = new StableId(order.Id);
            var attackRoll = random.Range(
                "military_logistics.risk",
                entityId,
                world.AbsoluteDay,
                $"attack.{leg.Sequence}",
                0,
                10_000);
            var escortPower = 0;
            var escorts = FindEscortsForLeg(
                world, order.Id, leg.Sequence);
            for (var i = 0; i < escorts.Count; i++)
            {
                if (escorts[i].Status ==
                    MilitaryLogisticsEscortStatus.InTransit)
                {
                    escortPower = checked(
                        escortPower + escorts[i].EscortPowerAtDeparture);
                }
            }

            var threatPower = Math.Min(
                12_000,
                checked(routeRisk + cargoAttraction + random.Range(
                    "military_logistics.risk",
                    entityId,
                    world.AbsoluteDay,
                    $"threat.{leg.Sequence}",
                    0,
                    5_001)));
            var outcome = MilitaryLogisticsIncidentOutcomeIds.Avoided;
            var seized = 0;
            if (attackRoll < attackChance)
            {
                if (escortPower >= threatPower)
                {
                    outcome = MilitaryLogisticsIncidentOutcomeIds.Repelled;
                }
                else
                {
                    outcome =
                        MilitaryLogisticsIncidentOutcomeIds.CargoSeized;
                    var severityBasisPoints = Math.Min(
                        7_500,
                        Math.Max(1_000, threatPower - escortPower));
                    seized = Math.Min(
                        order.RemainingCargoQuantity,
                        Math.Max(
                            1,
                            (int)((long)order.RemainingCargoQuantity *
                                severityBasisPoints / 10_000)));
                    order.RemainingCargoQuantity -= seized;
                    order.HostileLossQuantity = checked(
                        order.HostileLossQuantity + seized);
                    leg.HostileLossQuantity = checked(
                        leg.HostileLossQuantity + seized);
                    world.MilitaryLogisticsLedgerEntries.Add(
                        new MilitaryLogisticsLedgerEntryState
                        {
                            Id = $"military_logistics_ledger.{order.Id}." +
                                 $"hostile_loss.{leg.Sequence}." +
                                 $"{world.AbsoluteDay}",
                            Day = world.AbsoluteDay,
                            Type = MilitaryLogisticsLedgerType
                                .HostileCargoLoss,
                            LogisticsOrderId = order.Id,
                            ActorPersonId = order.CarrierPersonId,
                            CargoRemainingDelta = -seized,
                            CargoHostileLossDelta = seized,
                            Summary =
                                "Cargo seized by the recorded route threat organization."
                        });
                }
            }

            var incident = new MilitaryLogisticsIncidentState
            {
                Id = incidentId,
                Day = world.AbsoluteDay,
                LogisticsOrderId = order.Id,
                LogisticsLegId = leg.Id,
                RouteId = leg.RouteId,
                IncidentTypeId =
                    MilitaryLogisticsIncidentTypeIds.BanditAttack,
                OutcomeId = outcome,
                ThreatOrganizationId = leg.ThreatOrganizationId,
                AttackChanceBasisPoints = attackChance,
                AttackRollBasisPoints = attackRoll,
                EscortPower = escortPower,
                ThreatPower = threatPower,
                SeizedCargoQuantity = seized,
                Summary = seized > 0
                    ? "Route attackers seized part of the military cargo."
                    : outcome == MilitaryLogisticsIncidentOutcomeIds.Repelled
                        ? "The real escort repelled a route attack."
                        : "The convoy avoided a possible route attack."
            };
            world.MilitaryLogisticsIncidents.Add(incident);
            if (outcome != MilitaryLogisticsIncidentOutcomeIds.Avoided)
            {
                CreateInitialClash(world, order, leg, incident);
            }
        }

        private void CreateInitialClash(
            WorldState world,
            MilitaryLogisticsOrderState order,
            MilitaryLogisticsLegState leg,
            MilitaryLogisticsIncidentState incident)
        {
            var participantIds = new List<string> { order.CarrierPersonId };
            var escorts = FindEscortsForLeg(
                world, order.Id, leg.Sequence);
            for (var i = 0; i < escorts.Count; i++)
            {
                if (escorts[i].Status ==
                    MilitaryLogisticsEscortStatus.InTransit)
                {
                    participantIds.Add(escorts[i].PersonId);
                }
            }
            participantIds.Sort(StringComparer.Ordinal);
            var participants = new List<PersonState>(participantIds.Count);
            for (var i = 0; i < participantIds.Count; i++)
            {
                participants.Add(PeopleFor(world).GetRequired(
                    participantIds[i]));
            }

            var defendersHeld = incident.OutcomeId ==
                MilitaryLogisticsIncidentOutcomeIds.Repelled;
            var clash = new MilitaryLogisticsClashState
            {
                Id = $"military_logistics_clash.{incident.Id}.initial",
                Day = world.AbsoluteDay,
                LogisticsOrderId = order.Id,
                LogisticsLegId = leg.Id,
                IncidentId = incident.Id,
                TypeId = MilitaryLogisticsClashTypeIds.InitialDefense,
                OutcomeId = defendersHeld
                    ? MilitaryLogisticsClashOutcomeIds.DefendersHeld
                    : MilitaryLogisticsClashOutcomeIds.AttackersSeizedCargo,
                IssuerPersonId = string.Empty,
                DefenderOrganizationId = order.CarrierOrganizationId,
                DefenderPower = incident.EscortPower,
                ThreatPower = incident.ThreatPower,
                Summary = defendersHeld
                    ? "The carrier and real escorts held the freight line."
                    : "The carrier party was defeated and cargo was seized."
            };
            for (var i = 0; i < participants.Count; i++)
            {
                clash.DefenderPersonIds.Add(participants[i].Id);
            }
            ApplyClashInjuries(
                world,
                clash,
                participants,
                !defendersHeld,
                "initial");
            world.MilitaryLogisticsClashes.Add(clash);
        }

        private void ApplyClashInjuries(
            WorldState world,
            MilitaryLogisticsClashState clash,
            IList<PersonState> participants,
            bool defendersLost,
            string purpose)
        {
            var random = new NamedRandom(world.MasterSeed);
            var clashId = new StableId(clash.Id);
            for (var i = 0; i < participants.Count; i++)
            {
                var person = participants[i];
                var roll = random.Range(
                    "military_logistics.clash_injury",
                    clashId,
                    world.AbsoluteDay,
                    $"{purpose}.{person.Id}.roll",
                    0,
                    10_000);
                var shouldInjure = roll < (defendersLost ? 6_500 : 1_500);
                if (defendersLost && i == 0 && clash.Injuries.Count == 0)
                {
                    shouldInjure = true;
                }
                if (!shouldInjure || person.HealthBasisPoints <= 1)
                {
                    continue;
                }

                var damage = random.Range(
                    "military_logistics.clash_injury",
                    clashId,
                    world.AbsoluteDay,
                    $"{purpose}.{person.Id}.damage",
                    defendersLost ? 1_000 : 500,
                    defendersLost ? 2_501 : 1_501);
                var healthAfter = Math.Max(
                    1, person.HealthBasisPoints - damage);
                var service = FindActiveService(world, person.Id);
                clash.Injuries.Add(new MilitaryLogisticsInjuryState
                {
                    PersonId = person.Id,
                    MilitaryServiceId = service?.Id ?? string.Empty,
                    HealthBeforeBasisPoints = person.HealthBasisPoints,
                    HealthAfterBasisPoints = healthAfter
                });
                PeopleFor(world).GetRequiredForUpdate(person.Id)
                    .HealthBasisPoints = healthAfter;
                if (service != null)
                {
                    service.Status = MilitaryServiceStatus.Wounded;
                    service.LastStatusChangeDay = world.AbsoluteDay;
                }
            }

            var affectedArmies = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < clash.Injuries.Count; i++)
            {
                var injury = clash.Injuries[i];
                if (string.IsNullOrEmpty(injury.MilitaryServiceId))
                {
                    continue;
                }
                var service = FindService(world, injury.MilitaryServiceId);
                if (affectedArmies.Add(service.ArmyId))
                {
                    new MilitaryServiceSystem(_people)
                        .SynchronizeArmyCaches(world, service.ArmyId);
                }
            }
        }

        private static void Deliver(
            WorldState world,
            MilitaryLogisticsOrderState order,
            ArmyState army,
            int delivered)
        {
            if (delivered < 0 || delivered > order.RemainingCargoQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(delivered));
            }

            var provisionsAdded = checked(
                delivered * MilitarySupplySystem.ProvisionsPerGrainUnit);
            order.RemainingCargoQuantity = checked(
                order.RemainingCargoQuantity - delivered);
            order.DeliveredCargoQuantity = checked(
                order.DeliveredCargoQuantity + delivered);
            var currentLeg = FindCurrentLeg(world, order);
            if (currentLeg != null)
            {
                currentLeg.CargoTransferredQuantity = checked(
                    currentLeg.CargoTransferredQuantity + delivered);
            }
            if (order.RemainingCargoQuantity == 0)
            {
                order.Status = MilitaryLogisticsStatus.Delivered;
                order.DeliveredDay = world.AbsoluteDay;
                if (currentLeg != null)
                {
                    currentLeg.CompletedDay = world.AbsoluteDay;
                    currentLeg.Status = MilitaryLogisticsLegStatus.Completed;
                }
            }
            army.Provisions = checked(army.Provisions + provisionsAdded);
            world.MilitaryLogisticsLedgerEntries.Add(
                new MilitaryLogisticsLedgerEntryState
                {
                    Id = $"military_logistics_ledger.{order.Id}.delivery." +
                         $"{world.MilitaryLogisticsLedgerEntries.Count}",
                    Day = world.AbsoluteDay,
                    Type = MilitaryLogisticsLedgerType.Delivery,
                    LogisticsOrderId = order.Id,
                    ActorPersonId = order.CarrierPersonId,
                    CargoRemainingDelta = -delivered,
                    CargoDeliveredDelta = delivered,
                    ArmyProvisionsDelta = provisionsAdded,
                    Summary =
                        "Remaining freight received into the target army supply bridge."
                });
            if (provisionsAdded > 0)
            {
                world.MilitarySupplies.Add(new MilitarySupplyRecordState
                {
                    Id = $"military_supply.{world.AbsoluteDay}.{army.Id}." +
                         $"logistics.{world.MilitarySupplies.Count}",
                    Day = world.AbsoluteDay,
                    Type = MilitarySupplyType.LogisticsDelivery,
                    ArmyId = army.Id,
                    SupplierPersonId = order.CarrierPersonId,
                    SourceLogisticsOrderId = order.Id,
                    GrainUnits = delivered,
                    ProvisionsAdded = provisionsAdded,
                    UnitPrice = (int)Math.Min(int.MaxValue, order.UnitPrice),
                    TotalPaid = checked(order.UnitPrice * delivered),
                    Summary = "Audited logistics freight delivered to the army."
                });
            }
        }

        private static void AddDispatchInventoryTransaction(
            WorldState world,
            MilitaryLogisticsOrderState order,
            ProductBatchState cargoBatch,
            ProductBatchState provisionBatch)
        {
            var transaction = new InventoryTransactionState
            {
                Id = $"inventory_transaction.{order.Id}.dispatch",
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.MilitaryLogisticsDispatched,
                ActorPersonId = order.CarrierPersonId,
                SourceMilitaryLogisticsOrderId = order.Id,
                Summary =
                    "Cargo and carrier provisions transferred into an in-transit military freight manifest."
            };
            transaction.Lines.Add(Line(
                cargoBatch, -order.DispatchedCargoQuantity));
            if (provisionBatch != null &&
                order.ConvoyProvisionsLoaded > 0)
            {
                transaction.Lines.Add(Line(
                    provisionBatch, -order.ConvoyProvisionsLoaded));
            }

            world.InventoryTransactions.Add(transaction);
        }

        private static void AddPlannedProvisionReservations(
            WorldState world,
            MilitaryLogisticsOrderState order,
            IList<PlannedLeg> plannedLegs)
        {
            for (var i = 0; i < plannedLegs.Count; i++)
            {
                var planned = plannedLegs[i];
                if (planned.ProvisionBatch == null ||
                    planned.Request.ConvoyProvisionQuantity <= 0)
                {
                    continue;
                }

                var line = Line(planned.ProvisionBatch, 0);
                line.ReservedQuantityDelta =
                    planned.Request.ConvoyProvisionQuantity;
                world.InventoryTransactions.Add(
                    new InventoryTransactionState
                    {
                        Id = $"inventory_transaction.{order.Id}.reserve." +
                             $"{i + 1}",
                        Day = world.AbsoluteDay,
                        Type = InventoryTransactionType
                            .MilitaryLogisticsHandoffReserved,
                        ActorPersonId = order.IssuerPersonId,
                        SourceMilitaryLogisticsOrderId = order.Id,
                        Lines = new List<InventoryTransactionLineState>
                        {
                            line
                        },
                        Summary =
                            "Downstream carrier provisions reserved for a planned handoff."
                    });
            }
        }

        private static InventoryTransactionLineState Line(
            ProductBatchState batch,
            long delta)
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
                QuantityDelta = delta
            };
        }

        private static List<ProductQualityDimensionState> CopyQuality(
            IList<ProductQualityDimensionState> source)
        {
            var result = new List<ProductQualityDimensionState>(
                source == null ? 0 : source.Count);
            if (source == null)
            {
                return result;
            }

            for (var i = 0; i < source.Count; i++)
            {
                result.Add(new ProductQualityDimensionState
                {
                    QualityDimensionId = source[i].QualityDimensionId,
                    ValueBasisPoints = source[i].ValueBasisPoints
                });
            }

            return result;
        }

        private List<PlannedLeg> ValidateAdditionalLegs(
            WorldState world,
            MilitaryLogisticsDispatchRequest request,
            long cargoWeight,
            long initialProvisionWeight,
            int initialProvisionUnitWeight)
        {
            var result = new List<PlannedLeg>();
            var reservations = new Dictionary<string, long>(
                StringComparer.Ordinal);
            var previousDestination = request.DestinationLocationId.Value;
            var previousCarrier = request.CarrierPersonId.Value;
            var previousContainer = FindContainerByCarrier(
                world, previousCarrier).Id;
            var cumulativeProvisionWeight = initialProvisionWeight;
            var provisionUnitWeight = initialProvisionUnitWeight;
            var legs = request.AdditionalLegs ??
                new List<MilitaryLogisticsLegRequest>();
            for (var i = 0; i < legs.Count; i++)
            {
                var leg = legs[i] ?? throw new InvalidOperationException(
                    "A logistics leg request cannot be null.");
                if (leg.ConvoyProvisionQuantity < 0 ||
                    leg.DailyConvoyProvisionUse <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(request), "A logistics leg has invalid provisions.");
                }

                _ = new StableId(leg.CarrierOrganizationId);
                ValidateRiskContract(
                    world, leg.RiskPolicyId, leg.ThreatOrganizationId);
                var carrier = PeopleFor(world).GetRequired(
                    leg.CarrierPersonId.Value);
                var container = FindContainerByCarrier(world, carrier.Id);
                var route = FindRoute(world, leg.RouteId.Value);
                if (!carrier.IsAlive || carrier.LocationId != previousDestination ||
                    container.LocationId != previousDestination ||
                    container.OwnerOrganizationId !=
                        leg.CarrierOrganizationId ||
                    !HasMembership(
                        world, carrier.Id, leg.CarrierOrganizationId) ||
                    carrier.Id == previousCarrier ||
                    container.Id == previousContainer ||
                    HasActiveMilitaryService(
                        world, carrier.Id, request.TargetArmyId.Value) ||
                    !RouteConnects(
                        route,
                        previousDestination,
                        leg.DestinationLocationId.Value))
                {
                    throw new InvalidOperationException(
                        "A planned logistics handoff leg is invalid.");
                }
                var escorts = ValidateEscortPeople(
                    world,
                    leg.EscortPersonIds,
                    leg.CarrierOrganizationId,
                    previousDestination,
                    carrier.Id);

                ProductBatchState provisionBatch = null;
                ProductDefinition provisionProduct = null;
                if (leg.ConvoyProvisionQuantity > 0)
                {
                    if (string.IsNullOrWhiteSpace(
                            leg.SourceProvisionBatchId) ||
                        leg.SourceProvisionBatchId ==
                            request.SourceCargoBatchId.Value)
                    {
                        throw new InvalidOperationException(
                            "A downstream leg requires a separate provision batch.");
                    }

                    provisionBatch = FindBatch(
                        world, leg.SourceProvisionBatchId);
                    provisionProduct = _content.GetProduct(
                        provisionBatch.ProductDefinitionId);
                    reservations.TryGetValue(
                        provisionBatch.Id, out var alreadyPlanned);
                    if (provisionBatch.Quantity -
                            provisionBatch.ReservedQuantity - alreadyPlanned <
                            leg.ConvoyProvisionQuantity ||
                        provisionBatch.OwnerOrganizationId !=
                            leg.CarrierOrganizationId ||
                        FindContainer(
                            world,
                            provisionBatch.InventoryContainerId).LocationId !=
                            previousDestination ||
                        !provisionProduct.CategoryTags.Contains("product.food") ||
                        provisionUnitWeight > 0 &&
                            provisionUnitWeight != provisionProduct.BaseWeight)
                    {
                        throw new InvalidOperationException(
                            "A downstream carrier lacks compatible reserved provisions.");
                    }

                    provisionUnitWeight = provisionProduct.BaseWeight;
                    reservations[provisionBatch.Id] = checked(
                        alreadyPlanned + leg.ConvoyProvisionQuantity);
                    cumulativeProvisionWeight = checked(
                        cumulativeProvisionWeight +
                        (long)provisionProduct.BaseWeight *
                        leg.ConvoyProvisionQuantity);
                }

                if (CalculateTransportLoad(world, container.Id) + cargoWeight +
                    cumulativeProvisionWeight > container.CapacityWeight)
                {
                    throw new InvalidOperationException(
                        "A downstream transport container lacks capacity.");
                }

                result.Add(new PlannedLeg(
                    leg, carrier, container, route, provisionBatch,
                    provisionProduct, escorts));
                previousDestination = leg.DestinationLocationId.Value;
                previousCarrier = carrier.Id;
                previousContainer = container.Id;
            }

            return result;
        }

        private static void AddLegStates(
            WorldState world,
            MilitaryLogisticsOrderState order,
            MilitaryLogisticsDispatchRequest request,
            PersonState firstCarrier,
            InventoryContainerState firstContainer,
            RouteState firstRoute,
            JourneyState firstJourney,
            IList<PlannedLeg> additionalLegs)
        {
            world.MilitaryLogisticsLegs.Add(new MilitaryLogisticsLegState
            {
                Id = $"{order.Id}.leg.0",
                LogisticsOrderId = order.Id,
                Sequence = 0,
                OriginLocationId = order.OriginLocationId,
                DestinationLocationId = request.DestinationLocationId.Value,
                RouteId = firstRoute.Id,
                CarrierPersonId = firstCarrier.Id,
                CarrierOrganizationId = request.CarrierOrganizationId,
                TransportInventoryContainerId = firstContainer.Id,
                ProvisionBatchId = request.SourceProvisionBatchId,
                PlannedProvisionQuantity = request.ConvoyProvisionQuantity,
                LoadedProvisionQuantity = request.ConvoyProvisionQuantity,
                CargoReceivedQuantity = order.DispatchedCargoQuantity,
                DailyProvisionUse = request.DailyConvoyProvisionUse,
                RiskPolicyId = request.RiskPolicyId,
                ThreatOrganizationId = request.ThreatOrganizationId,
                JourneyId = firstJourney == null
                    ? string.Empty
                    : firstJourney.Id,
                StartedDay = world.AbsoluteDay,
                Status = MilitaryLogisticsLegStatus.InTransit
            });
            for (var i = 0; i < additionalLegs.Count; i++)
            {
                var planned = additionalLegs[i];
                world.MilitaryLogisticsLegs.Add(
                    new MilitaryLogisticsLegState
                    {
                        Id = $"{order.Id}.leg.{i + 1}",
                        LogisticsOrderId = order.Id,
                        Sequence = i + 1,
                        OriginLocationId = i == 0
                            ? request.DestinationLocationId.Value
                            : additionalLegs[i - 1].Request
                                .DestinationLocationId.Value,
                        DestinationLocationId =
                            planned.Request.DestinationLocationId.Value,
                        RouteId = planned.Route.Id,
                        CarrierPersonId = planned.Carrier.Id,
                        CarrierOrganizationId =
                            planned.Request.CarrierOrganizationId,
                        TransportInventoryContainerId = planned.Container.Id,
                        ProvisionBatchId =
                            planned.Request.SourceProvisionBatchId,
                        PlannedProvisionQuantity =
                            planned.Request.ConvoyProvisionQuantity,
                        DailyProvisionUse =
                            planned.Request.DailyConvoyProvisionUse,
                        RiskPolicyId = planned.Request.RiskPolicyId,
                        ThreatOrganizationId =
                            planned.Request.ThreatOrganizationId,
                        JourneyId = string.Empty,
                        Status = MilitaryLogisticsLegStatus.Planned
                    });
            }
        }

        private static void AddEscortStates(
            WorldState world,
            MilitaryLogisticsOrderState order,
            MilitaryLogisticsDispatchRequest request,
            IList<PersonState> firstEscorts,
            IList<JourneyState> firstEscortJourneys,
            IList<PlannedLeg> additionalLegs)
        {
            for (var i = 0; i < firstEscorts.Count; i++)
            {
                world.MilitaryLogisticsEscorts.Add(
                    new MilitaryLogisticsEscortState
                    {
                        Id = $"{order.Id}.leg.0.escort.{firstEscorts[i].Id}",
                        LogisticsOrderId = order.Id,
                        LogisticsLegId = $"{order.Id}.leg.0",
                        LegSequence = 0,
                        PersonId = firstEscorts[i].Id,
                        JourneyId = firstEscortJourneys[i].Id,
                        EscortPowerAtDeparture =
                            CalculateEscortPower(firstEscorts[i]),
                        StartedDay = world.AbsoluteDay,
                        Status = MilitaryLogisticsEscortStatus.InTransit
                    });
            }

            for (var legIndex = 0;
                 legIndex < additionalLegs.Count;
                 legIndex++)
            {
                var planned = additionalLegs[legIndex];
                for (var escortIndex = 0;
                     escortIndex < planned.Escorts.Count;
                     escortIndex++)
                {
                    var escort = planned.Escorts[escortIndex];
                    world.MilitaryLogisticsEscorts.Add(
                        new MilitaryLogisticsEscortState
                        {
                            Id = $"{order.Id}.leg.{legIndex + 1}.escort." +
                                 escort.Id,
                            LogisticsOrderId = order.Id,
                            LogisticsLegId =
                                $"{order.Id}.leg.{legIndex + 1}",
                            LegSequence = legIndex + 1,
                            PersonId = escort.Id,
                            JourneyId = string.Empty,
                            Status = MilitaryLogisticsEscortStatus.Planned
                        });
                }
            }
        }

        private List<PersonState> ValidateEscortPeople(
            WorldState world,
            IList<string> escortPersonIds,
            string carrierOrganizationId,
            string originLocationId,
            string carrierPersonId)
        {
            var ids = new List<string>(escortPersonIds ??
                new List<string>());
            ids.Sort(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<PersonState>(ids.Count);
            for (var i = 0; i < ids.Count; i++)
            {
                _ = new StableId(ids[i]);
                var person = PeopleFor(world).GetRequired(ids[i]);
                if (!seen.Add(person.Id) || person.Id == carrierPersonId ||
                    !person.IsAlive || person.LocationId != originLocationId ||
                    !HasMembership(
                        world, person.Id, carrierOrganizationId) ||
                    HasActiveJourneyForPerson(world, person.Id) ||
                    HasAnyActiveMilitaryService(world, person.Id))
                {
                    throw new InvalidOperationException(
                        "A logistics escort assignment is invalid.");
                }

                result.Add(person);
            }

            return result;
        }

        private List<JourneyState> StartEscortJourneys(
            WorldState world,
            IList<PersonState> escorts,
            StableId routeId,
            StableId destinationId)
        {
            var result = new List<JourneyState>(escorts.Count);
            for (var i = 0; i < escorts.Count; i++)
            {
                result.Add(_travel.StartJourney(
                    world,
                    new StableId(escorts[i].Id),
                    routeId,
                    destinationId,
                    TravelMode.Caravan));
            }

            return result;
        }

        private static int CalculateEscortPower(PersonState person)
        {
            var attributes = StrategicAttributeCalculator.Calculate(person);
            return Math.Max(
                0,
                attributes.Martial + attributes.Leadership / 2);
        }

        private static void ValidateRiskContract(
            WorldState world,
            string riskPolicyId,
            string threatOrganizationId)
        {
            _ = new StableId(riskPolicyId);
            if (riskPolicyId == MilitaryLogisticsRiskPolicyIds.None)
            {
                if (!string.IsNullOrEmpty(threatOrganizationId))
                {
                    throw new InvalidOperationException(
                        "A no-risk leg cannot name a threat organization.");
                }

                return;
            }

            if (riskPolicyId != MilitaryLogisticsRiskPolicyIds.Standard ||
                string.IsNullOrWhiteSpace(threatOrganizationId))
            {
                throw new InvalidOperationException(
                    "The logistics risk contract is unsupported.");
            }

            _ = new StableId(threatOrganizationId);
            _ = FindOrganization(world, threatOrganizationId);
        }

        private static AcquisitionMethodRules ResolveMethod(
            string methodId,
            string buyerOrganizationId,
            string sourceOrganizationId,
            long unitPrice,
            int cargoQuantity)
        {
            if (methodId == MilitarySupplyAcquisitionMethodIds.CommercialPurchase)
            {
                RequireDifferentOrganizations(
                    buyerOrganizationId, sourceOrganizationId, methodId);
                RequirePositivePrice(unitPrice, methodId);
                return new AcquisitionMethodRules(true, 0);
            }

            if (methodId ==
                MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer)
            {
                if (buyerOrganizationId != sourceOrganizationId ||
                    unitPrice != 0)
                {
                    throw new InvalidOperationException(
                        "Internal transfer requires common ownership and no purchase price.");
                }

                return new AcquisitionMethodRules(false, 0);
            }

            RequireDifferentOrganizations(
                buyerOrganizationId, sourceOrganizationId, methodId);
            if (methodId ==
                MilitarySupplyAcquisitionMethodIds.CompensatedRequisition)
            {
                RequirePositivePrice(unitPrice, methodId);
                return new AcquisitionMethodRules(
                    true, -Math.Max(1, cargoQuantity / 20));
            }

            if (methodId ==
                MilitarySupplyAcquisitionMethodIds.ForcedRequisition)
            {
                RequireZeroPrice(unitPrice, methodId);
                return new AcquisitionMethodRules(
                    false, -Math.Max(20, cargoQuantity / 5));
            }

            if (methodId == MilitarySupplyAcquisitionMethodIds.Plunder)
            {
                RequireZeroPrice(unitPrice, methodId);
                return new AcquisitionMethodRules(
                    false, -Math.Max(50, cargoQuantity / 2));
            }

            throw new InvalidOperationException(
                $"Unsupported military supply acquisition method {methodId}.");
        }

        private static void RequireDifferentOrganizations(
            string buyer,
            string source,
            string method)
        {
            if (buyer == source)
            {
                throw new InvalidOperationException(
                    $"Method {method} requires a separate source organization.");
            }
        }

        private static void RequirePositivePrice(long price, string method)
        {
            if (price <= 0)
            {
                throw new InvalidOperationException(
                    $"Method {method} requires a positive unit price.");
            }
        }

        private static void RequireZeroPrice(long price, string method)
        {
            if (price != 0)
            {
                throw new InvalidOperationException(
                    $"Method {method} cannot create a purchase payment.");
            }
        }

        private static long CalculateTransportLoad(
            WorldState world,
            string containerId)
        {
            long total = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId == containerId)
                {
                    total = checked(total + batch.Quantity * batch.UnitWeight);
                }
            }

            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                var order = world.MilitaryLogisticsOrders[i];
                if (order.TransportInventoryContainerId == containerId &&
                    (order.RemainingCargoQuantity > 0 ||
                     order.ConvoyProvisionsRemaining > 0))
                {
                    total = checked(total +
                        (long)order.RemainingCargoQuantity *
                        order.CargoUnitWeightAtDispatch +
                        (long)order.ConvoyProvisionsRemaining *
                        order.ConvoyProvisionUnitWeightAtDispatch);
                }
            }

            return total;
        }

        private static long RemovedBatchWeight(
            ProductBatchState batch,
            string containerId,
            int quantity)
        {
            return batch != null &&
                   batch.InventoryContainerId == containerId
                ? checked((long)batch.UnitWeight * quantity)
                : 0;
        }

        private IPersonRepository PeopleFor(WorldState world) =>
            _people ?? new WorldStatePersonRepository(world);

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

        private static InventoryContainerState FindContainer(
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

        private static ArmyMarchState FindArmyMarch(
            WorldState world,
            string armyId,
            string routeId,
            string destinationId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                var march = world.ArmyMarches[i];
                if (march.ArmyId == armyId &&
                    march.RouteId == routeId &&
                    march.DestinationLocationId == destinationId)
                {
                    return march;
                }
            }

            return null;
        }

        private static bool HasActiveMovement(
            WorldState world,
            MilitaryLogisticsOrderState order)
        {
            if (!string.IsNullOrEmpty(order.JourneyId) &&
                FindJourney(world, order.JourneyId) != null)
            {
                return true;
            }

            if (string.IsNullOrEmpty(order.ArmyMarchId))
            {
                return false;
            }

            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].Id == order.ArmyMarchId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActiveJourneyForPerson(
            WorldState world,
            string personId)
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

        private static bool HasAnyActiveMilitaryService(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == personId &&
                    (service.Status == MilitaryServiceStatus.Mustering ||
                     service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Wounded))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActiveMilitaryService(
            WorldState world,
            string personId,
            string armyId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == personId &&
                    service.ArmyId == armyId &&
                    (service.Status == MilitaryServiceStatus.Mustering ||
                     service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Wounded))
                {
                    return true;
                }
            }

            return false;
        }

        private List<PersonState> ValidateRecoveryParticipants(
            WorldState world,
            string armyId,
            IList<string> participantPersonIds)
        {
            var ids = new List<string>(participantPersonIds ??
                new List<string>());
            if (ids.Count == 0 || ids.Count > 20)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(participantPersonIds),
                    "A recovery party requires one to twenty real people.");
            }

            ids.Sort(StringComparer.Ordinal);
            var result = new List<PersonState>(ids.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ids.Count; i++)
            {
                _ = new StableId(ids[i]);
                var person = PeopleFor(world).GetRequired(ids[i]);
                var service = FindActiveService(world, person.Id);
                if (!seen.Add(person.Id) || !person.IsAlive ||
                    person.HealthBasisPoints <= 0 || service == null ||
                    service.ArmyId != armyId)
                {
                    throw new InvalidOperationException(
                        "A recovery-party assignment is invalid.");
                }
                result.Add(person);
            }

            return result;
        }

        private static int CalculateGroupPower(IList<PersonState> people)
        {
            long power = 0;
            for (var i = 0; i < people.Count; i++)
            {
                power += CalculateEscortPower(people[i]);
            }

            return (int)Math.Min(int.MaxValue, power);
        }

        private static MilitaryServiceState FindActiveService(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == personId &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering))
                {
                    return service;
                }
            }

            return null;
        }

        private static MilitaryServiceState FindService(
            WorldState world,
            string serviceId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                if (world.MilitaryServices[i].Id == serviceId)
                {
                    return world.MilitaryServices[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military service {serviceId}.");
        }

        private static ArmyMarchState FindArmyMarch(
            WorldState world,
            string marchId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].Id == marchId)
                {
                    return world.ArmyMarches[i];
                }
            }

            return null;
        }

        private static MilitaryLogisticsIncidentState FindIncident(
            WorldState world,
            string incidentId)
        {
            for (var i = 0; i < world.MilitaryLogisticsIncidents.Count; i++)
            {
                if (world.MilitaryLogisticsIncidents[i].Id == incidentId)
                {
                    return world.MilitaryLogisticsIncidents[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics incident {incidentId}.");
        }

        private static bool HasRecoveryClash(
            WorldState world,
            string incidentId)
        {
            for (var i = 0; i < world.MilitaryLogisticsClashes.Count; i++)
            {
                var clash = world.MilitaryLogisticsClashes[i];
                if (clash.IncidentId == incidentId &&
                    clash.TypeId ==
                        MilitaryLogisticsClashTypeIds.RecoveryAttempt)
                {
                    return true;
                }
            }

            return false;
        }

        private static MilitaryLogisticsOrderState FindOrder(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                if (world.MilitaryLogisticsOrders[i].Id == id)
                {
                    return world.MilitaryLogisticsOrders[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics order {id}.");
        }

        private static MilitaryLogisticsLegState FindCurrentLeg(
            WorldState world,
            MilitaryLogisticsOrderState order)
        {
            if (order.PlannedLegCount <= 0)
            {
                return null;
            }

            return FindLeg(world, order.Id, order.CurrentLegSequence);
        }

        private static MilitaryLogisticsLegState FindLeg(
            WorldState world,
            string orderId,
            int sequence)
        {
            for (var i = 0; i < world.MilitaryLogisticsLegs.Count; i++)
            {
                var leg = world.MilitaryLogisticsLegs[i];
                if (leg.LogisticsOrderId == orderId &&
                    leg.Sequence == sequence)
                {
                    return leg;
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics leg {orderId}#{sequence}.");
        }

        private static List<MilitaryLogisticsEscortState> FindEscortsForLeg(
            WorldState world,
            string orderId,
            int sequence)
        {
            var result = new List<MilitaryLogisticsEscortState>();
            for (var i = 0; i < world.MilitaryLogisticsEscorts.Count; i++)
            {
                var escort = world.MilitaryLogisticsEscorts[i];
                if (escort.LogisticsOrderId == orderId &&
                    escort.LegSequence == sequence)
                {
                    result.Add(escort);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.PersonId, right.PersonId));
            return result;
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

        private static LocationState FindLocation(WorldState world, string id)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == id)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {id}.");
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

        private static bool RouteConnects(
            RouteState route,
            string origin,
            string destination)
        {
            return route.FromLocationId == origin &&
                   route.ToLocationId == destination ||
                   route.Bidirectional &&
                   route.ToLocationId == origin &&
                   route.FromLocationId == destination;
        }

        private static bool ArmyCanReceiveAt(
            WorldState world,
            ArmyState army,
            string destination)
        {
            if (army.LocationId == destination)
            {
                return true;
            }

            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == army.Id &&
                    world.ArmyMarches[i].DestinationLocationId == destination)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class AcquisitionMethodRules
        {
            public bool RequiresPayment { get; }
            public int OriginPublicOrderDelta { get; }

            public AcquisitionMethodRules(
                bool requiresPayment,
                int originPublicOrderDelta)
            {
                RequiresPayment = requiresPayment;
                OriginPublicOrderDelta = originPublicOrderDelta;
            }
        }

        private sealed class PlannedLeg
        {
            public MilitaryLogisticsLegRequest Request { get; }
            public PersonState Carrier { get; }
            public InventoryContainerState Container { get; }
            public RouteState Route { get; }
            public ProductBatchState ProvisionBatch { get; }
            public ProductDefinition ProvisionProduct { get; }
            public List<PersonState> Escorts { get; }

            public PlannedLeg(
                MilitaryLogisticsLegRequest request,
                PersonState carrier,
                InventoryContainerState container,
                RouteState route,
                ProductBatchState provisionBatch,
                ProductDefinition provisionProduct,
                List<PersonState> escorts)
            {
                Request = request;
                Carrier = carrier;
                Container = container;
                Route = route;
                ProvisionBatch = provisionBatch;
                ProvisionProduct = provisionProduct;
                Escorts = escorts;
            }
        }
    }
}
