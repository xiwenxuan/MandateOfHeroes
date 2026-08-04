using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class CivilianFreightDispatchRequest
    {
        public string BuyOrderId;
        public string SellOrderId;
        public string DemandId;
        public string CarrierOfferId;
        public string CarrierPersonId;
        public string TransportInventoryContainerId;
        public string RouteId;
        public List<string> RouteIds = new List<string>();
        public long Quantity;
        public long FreightFee;
    }

    public sealed class PublicReliefFreightDispatchRequest
    {
        public string DestinationCountyGovernanceId;
        public string BuyerOrganizationId;
        public string DestinationInventoryContainerId;
        public string SourcePublicReliefEventId;
        public string SourcePublicReliefCommandId;
        public string PublicReliefRecoveryId;
        public bool IsSupplemental;
        public string SellOrderId;
        public string CarrierPersonId;
        public string TransportInventoryContainerId;
        public List<string> RouteIds = new List<string>();
        public long Quantity;
        public long FreightFee;
    }

    public sealed class CivilianCarrierRegistrationRequest
    {
        public string CarrierPersonId;
        public string TransportInventoryContainerId;
        public long BaseFee;
        public long FeePerKilometer;
        public long FeePerHundredUnits;
        public int MaximumDistanceKilometers;
        public string RoutePolicyId =
            CivilianFreightRoutePolicyIds.ShortestKnown;
        public List<string> KnownRouteIds = new List<string>();
    }

    public sealed class CivilianFreightSystem
    {
        private const string RandomSystemId = "civilian_freight";
        private const long LossDenominator = 1_000_000_000L;

        private readonly ProductionContentRegistry _content;
        private readonly FoodInventorySystem _foodInventory;
        private readonly FormalCountyMarketSystem _market;
        private readonly NamedRandom _random;

        public CivilianFreightSystem(
            ulong masterSeed,
            ProductionContentRegistry content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _foodInventory = new FoodInventorySystem(content);
            _market = new FormalCountyMarketSystem(content);
            _random = new NamedRandom(masterSeed);
        }

        public CivilianCarrierRegistrationState RegisterCarrier(
            WorldState world,
            CivilianCarrierRegistrationRequest request)
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
            var carrier = ProductInventorySystem.FindPerson(
                world, request.CarrierPersonId);
            var family = ProductInventorySystem.FindFamily(
                world, carrier.FamilyId);
            var container = ProductInventorySystem.FindContainer(
                world, request.TransportInventoryContainerId);
            var knownRoutes = SortedUniqueRouteIds(
                world, request.KnownRouteIds);
            if (!carrier.IsAlive ||
                container.OwnerFamilyId != family.Id ||
                !string.IsNullOrEmpty(container.OwnerOrganizationId) ||
                container.CarrierPersonId != carrier.Id ||
                request.BaseFee < 0 || request.FeePerKilometer < 0 ||
                request.FeePerHundredUnits < 0 ||
                request.MaximumDistanceKilometers <= 0 ||
                !IsSupportedRoutePolicy(request.RoutePolicyId) ||
                knownRoutes.Count == 0)
            {
                throw new InvalidOperationException(
                    "The civilian carrier registration is invalid.");
            }
            for (var i = 0; i < world.CivilianCarrierRegistrations.Count; i++)
            {
                var existing = world.CivilianCarrierRegistrations[i];
                if (existing.Active &&
                    (existing.CarrierPersonId == carrier.Id ||
                     existing.TransportInventoryContainerId == container.Id))
                {
                    throw new InvalidOperationException(
                        "The carrier or transport container is already registered.");
                }
            }

            var registration = new CivilianCarrierRegistrationState
            {
                Id = $"civilian_carrier.{world.AbsoluteDay}." +
                    $"{world.CivilianCarrierRegistrations.Count:D6}",
                CarrierPersonId = carrier.Id,
                CarrierFamilyId = family.Id,
                TransportInventoryContainerId = container.Id,
                BaseFee = request.BaseFee,
                FeePerKilometer = request.FeePerKilometer,
                FeePerHundredUnits = request.FeePerHundredUnits,
                MaximumDistanceKilometers =
                    request.MaximumDistanceKilometers,
                RoutePolicyId = request.RoutePolicyId,
                RegisteredDay = world.AbsoluteDay,
                KnownRouteIds = knownRoutes
            };
            world.CivilianCarrierRegistrations.Add(registration);
            world.Validate();
            return registration;
        }

        public void ProcessDailyPlanning(
            WorldState world,
            int maximumNewDemands = 64,
            int maximumNewOffers = 256,
            int maximumDispatches = 32)
        {
            ExpireInvalidDemands(world);
            GenerateDemands(world, maximumNewDemands);
            GenerateOffers(world, maximumNewOffers);
            DispatchBestOffers(world, maximumDispatches);
        }

        public bool HasDailyPlanningWork(WorldState world)
        {
            RequireFormalWorld(world);
            for (var demandIndex = 0;
                 demandIndex < world.CivilianFreightDemands.Count;
                 demandIndex++)
            {
                if (world.CivilianFreightDemands[demandIndex].Status ==
                    CivilianFreightDemandStatus.Active)
                {
                    return true;
                }
            }

            var orders = new List<FormalMarketOrderState>(
                world.FormalMarketOrders);
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var buyIndex = 0; buyIndex < orders.Count; buyIndex++)
            {
                var buy = orders[buyIndex];
                if (buy.Status != FormalMarketOrderStatus.Active ||
                    buy.Side != FormalMarketOrderSide.Buy ||
                    buy.ExpiryDay < world.AbsoluteDay ||
                    buy.RemainingQuantity <= 0 ||
                    HasActiveDemandForOrder(world, buy.Id))
                {
                    continue;
                }
                for (var sellIndex = 0; sellIndex < orders.Count; sellIndex++)
                {
                    var sell = orders[sellIndex];
                    if (sell.Status == FormalMarketOrderStatus.Active &&
                        sell.Side == FormalMarketOrderSide.Sell &&
                        sell.ExpiryDay >= world.AbsoluteDay &&
                        sell.CountyGovernanceId != buy.CountyGovernanceId &&
                        sell.ProductDefinitionId == buy.ProductDefinitionId &&
                        sell.UnitPrice <= buy.UnitPrice &&
                        sell.RemainingQuantity > 0 &&
                        SumRemainingReservations(sell) > 0 &&
                        ReservationsMeetQuality(world, sell, buy) &&
                        !HasActiveDemandForOrder(world, sell.Id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void ValidateDailyPlanning(WorldState world, long expectedDay)
        {
            RequireFormalWorld(world);
            if (expectedDay != world.AbsoluteDay)
            {
                throw new InvalidOperationException(
                    "Civilian freight planning command is no longer on its expected day.");
            }
        }

        public int GenerateDemands(
            WorldState world,
            int maximumNewDemands = 64)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (maximumNewDemands < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumNewDemands));
            }
            world.Validate();
            var orders = new List<FormalMarketOrderState>(
                world.FormalMarketOrders);
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var generated = 0;
            for (var buyIndex = 0;
                 buyIndex < orders.Count && generated < maximumNewDemands;
                 buyIndex++)
            {
                var buy = orders[buyIndex];
                if (buy.Status != FormalMarketOrderStatus.Active ||
                    buy.Side != FormalMarketOrderSide.Buy ||
                    buy.ExpiryDay < world.AbsoluteDay ||
                    HasActiveDemandForOrder(world, buy.Id))
                {
                    continue;
                }
                FormalMarketOrderState selectedSell = null;
                for (var sellIndex = 0;
                     sellIndex < orders.Count;
                     sellIndex++)
                {
                    var sell = orders[sellIndex];
                    if (sell.Status != FormalMarketOrderStatus.Active ||
                        sell.Side != FormalMarketOrderSide.Sell ||
                        sell.ExpiryDay < world.AbsoluteDay ||
                        sell.CountyGovernanceId == buy.CountyGovernanceId ||
                        sell.ProductDefinitionId != buy.ProductDefinitionId ||
                        sell.UnitPrice > buy.UnitPrice ||
                        sell.RemainingQuantity <= 0 ||
                        SumRemainingReservations(sell) <= 0 ||
                        !ReservationsMeetQuality(world, sell, buy) ||
                        HasActiveDemandForOrder(world, sell.Id))
                    {
                        continue;
                    }
                    if (selectedSell == null ||
                        CompareSellOrders(sell, selectedSell) < 0)
                    {
                        selectedSell = sell;
                    }
                }
                if (selectedSell == null)
                {
                    continue;
                }
                var quantity = Math.Min(
                    buy.RemainingQuantity,
                    Math.Min(
                        selectedSell.RemainingQuantity,
                        SumRemainingReservations(selectedSell)));
                if (quantity <= 0)
                {
                    continue;
                }
                var buyer = ProductInventorySystem.FindFamily(
                    world, buy.OwnerFamilyId);
                var seller = ProductInventorySystem.FindFamily(
                    world, selectedSell.OwnerFamilyId);
                var goodsValue = checked(
                    quantity * selectedSell.UnitPrice);
                var maximumFee = Math.Min(
                    buyer.Wealth,
                    Math.Max(1, goodsValue / 10));
                world.CivilianFreightDemands.Add(
                    new CivilianFreightDemandState
                    {
                        Id = $"civilian_freight_demand.{world.AbsoluteDay}." +
                            $"{world.CivilianFreightDemands.Count:D6}",
                        Status = CivilianFreightDemandStatus.Active,
                        BuyOrderId = buy.Id,
                        SellOrderId = selectedSell.Id,
                        OriginCountyGovernanceId =
                            selectedSell.CountyGovernanceId,
                        DestinationCountyGovernanceId =
                            buy.CountyGovernanceId,
                        OriginLocationId = seller.LocationId,
                        DestinationLocationId = buyer.LocationId,
                        ProductDefinitionId = buy.ProductDefinitionId,
                        Quantity = quantity,
                        MaximumFreightFee = maximumFee,
                        RoutePolicyId =
                            CivilianFreightRoutePolicyIds.ShortestKnown,
                        CreatedDay = world.AbsoluteDay,
                        ExpiryDay = Math.Min(
                            buy.ExpiryDay, selectedSell.ExpiryDay)
                    });
                generated++;
            }
            world.Validate();
            return generated;
        }

        public int GenerateOffers(
            WorldState world,
            int maximumNewOffers = 256)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (maximumNewOffers < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumNewOffers));
            }
            world.Validate();
            var demands = ActiveDemands(world);
            var registrations = ActiveRegistrations(world);
            var generated = 0;
            for (var demandIndex = 0;
                 demandIndex < demands.Count && generated < maximumNewOffers;
                 demandIndex++)
            {
                var demand = demands[demandIndex];
                for (var registrationIndex = 0;
                     registrationIndex < registrations.Count &&
                        generated < maximumNewOffers;
                     registrationIndex++)
                {
                    var registration = registrations[registrationIndex];
                    if (HasOffer(world, demand.Id, registration.Id) ||
                        registration.RoutePolicyId != demand.RoutePolicyId ||
                        !CanCarrierServeDemand(
                            world, registration, demand, out var productWeight))
                    {
                        continue;
                    }
                    var path = FindKnownPath(
                        world,
                        registration.KnownRouteIds,
                        demand.OriginLocationId,
                        demand.DestinationLocationId,
                        demand.RoutePolicyId,
                        registration.MaximumDistanceKilometers);
                    if (path == null)
                    {
                        continue;
                    }
                    if (CalculateContainerWeight(
                            world,
                            registration.TransportInventoryContainerId) +
                        checked(demand.Quantity * productWeight) >
                        ProductInventorySystem.FindContainer(
                            world,
                            registration.TransportInventoryContainerId)
                            .CapacityWeight)
                    {
                        continue;
                    }
                    var quotedFee = CalculateQuotedFee(
                        registration, demand.Quantity, path.TotalDistance);
                    if (quotedFee > demand.MaximumFreightFee)
                    {
                        continue;
                    }
                    world.CivilianCarrierOffers.Add(
                        new CivilianCarrierOfferState
                        {
                            Id = $"civilian_carrier_offer.{world.AbsoluteDay}." +
                                $"{world.CivilianCarrierOffers.Count:D6}",
                            Status = CivilianCarrierOfferStatus.Active,
                            DemandId = demand.Id,
                            CarrierRegistrationId = registration.Id,
                            CarrierPersonId = registration.CarrierPersonId,
                            CarrierFamilyId = registration.CarrierFamilyId,
                            TransportInventoryContainerId = registration
                                .TransportInventoryContainerId,
                            RoutePolicyId = demand.RoutePolicyId,
                            PlannedRouteIds = path.RouteIds,
                            TotalDistanceKilometers = path.TotalDistance,
                            MinimumSecurityBasisPoints = path.MinimumSecurity,
                            QuotedFreightFee = quotedFee,
                            CreatedDay = world.AbsoluteDay
                        });
                    generated++;
                }
            }
            world.Validate();
            return generated;
        }

        public int DispatchBestOffers(
            WorldState world,
            int maximumDispatches = 32)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (maximumDispatches < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDispatches));
            }
            world.Validate();
            var demands = ActiveDemands(world);
            var dispatched = 0;
            for (var demandIndex = 0;
                 demandIndex < demands.Count &&
                    dispatched < maximumDispatches;
                 demandIndex++)
            {
                var demand = demands[demandIndex];
                var offers = ActiveOffersFor(world, demand.Id);
                offers.Sort(CompareOffers);
                for (var offerIndex = 0;
                     offerIndex < offers.Count;
                     offerIndex++)
                {
                    var offer = offers[offerIndex];
                    var registration = FindRegistration(
                        world, offer.CarrierRegistrationId);
                    if (!CanCarrierServeDemand(
                            world, registration, demand, out _))
                    {
                        offer.Status = CivilianCarrierOfferStatus.Withdrawn;
                        offer.ClosedDay = world.AbsoluteDay;
                        continue;
                    }
                    var buy = FindOrder(world, demand.BuyOrderId);
                    if (ProductInventorySystem.FindFamily(
                            world, buy.OwnerFamilyId).Wealth <
                        offer.QuotedFreightFee)
                    {
                        offer.Status = CivilianCarrierOfferStatus.Withdrawn;
                        offer.ClosedDay = world.AbsoluteDay;
                        continue;
                    }
                    Dispatch(
                        world,
                        new CivilianFreightDispatchRequest
                        {
                            BuyOrderId = demand.BuyOrderId,
                            SellOrderId = demand.SellOrderId,
                            DemandId = demand.Id,
                            CarrierOfferId = offer.Id,
                            CarrierPersonId = offer.CarrierPersonId,
                            TransportInventoryContainerId =
                                offer.TransportInventoryContainerId,
                            RouteIds = new List<string>(
                                offer.PlannedRouteIds),
                            Quantity = demand.Quantity,
                            FreightFee = offer.QuotedFreightFee
                        });
                    dispatched++;
                    break;
                }
            }
            world.Validate();
            return dispatched;
        }

        public CivilianFreightState Dispatch(
            WorldState world,
            CivilianFreightDispatchRequest request)
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
            _content.ValidateWorldReferences(world);
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                throw new InvalidOperationException(
                    "Civilian freight requires formal food inventory authority.");
            }

            var buy = FindOrder(world, request.BuyOrderId);
            var sell = FindOrder(world, request.SellOrderId);
            var carrier = ProductInventorySystem.FindPerson(
                world, request.CarrierPersonId);
            var carrierFamily = ProductInventorySystem.FindFamily(
                world, carrier.FamilyId);
            var buyer = ProductInventorySystem.FindFamily(
                world, buy.OwnerFamilyId);
            var seller = ProductInventorySystem.FindFamily(
                world, sell.OwnerFamilyId);
            var buyerStorage = ProductInventorySystem.FindFacility(
                world, buy.StorageFacilityId);
            var sellerStorage = ProductInventorySystem.FindFacility(
                world, sell.StorageFacilityId);
            var container = ProductInventorySystem.FindContainer(
                world, request.TransportInventoryContainerId);
            var product = _content.GetProduct(sell.ProductDefinitionId);
            var food = _content.GetFood(sell.ProductDefinitionId);
            var requestedRouteIds = RequestRouteIds(request);
            var routePlan = BuildRoutePlan(
                world,
                requestedRouteIds,
                seller.LocationId,
                buyer.LocationId);
            var route = FindRoute(world, requestedRouteIds[0]);
            var demand = string.IsNullOrEmpty(request.DemandId)
                ? null
                : FindDemand(world, request.DemandId);
            var offer = string.IsNullOrEmpty(request.CarrierOfferId)
                ? null
                : FindOffer(world, request.CarrierOfferId);
            var validPlanningLink = demand == null && offer == null ||
                demand != null && offer != null &&
                demand.Status == CivilianFreightDemandStatus.Active &&
                offer.Status == CivilianCarrierOfferStatus.Active &&
                offer.DemandId == demand.Id &&
                demand.BuyOrderId == buy.Id &&
                demand.SellOrderId == sell.Id &&
                demand.Quantity == request.Quantity &&
                offer.CarrierPersonId == carrier.Id &&
                offer.TransportInventoryContainerId == container.Id &&
                offer.QuotedFreightFee == request.FreightFee &&
                RouteIdsEqual(offer.PlannedRouteIds, requestedRouteIds);
            if (request.Quantity <= 0 || request.FreightFee < 0 ||
                !validPlanningLink ||
                demand == null &&
                    (HasActiveDemandForOrder(world, buy.Id) ||
                     HasActiveDemandForOrder(world, sell.Id)) ||
                buy.Status != FormalMarketOrderStatus.Active ||
                sell.Status != FormalMarketOrderStatus.Active ||
                buy.Side != FormalMarketOrderSide.Buy ||
                sell.Side != FormalMarketOrderSide.Sell ||
                buy.CountyGovernanceId == sell.CountyGovernanceId ||
                buy.ProductDefinitionId != sell.ProductDefinitionId ||
                sell.UnitPrice > buy.UnitPrice ||
                request.Quantity > buy.RemainingQuantity ||
                request.Quantity > sell.RemainingQuantity ||
                SumRemainingReservations(sell) < request.Quantity ||
                !ReservationsMeetQuality(world, sell, buy) ||
                !carrier.IsAlive || carrier.LocationId != seller.LocationId ||
                HasJourney(world, carrier.Id) ||
                container.OwnerFamilyId != carrierFamily.Id ||
                !string.IsNullOrEmpty(container.OwnerOrganizationId) ||
                container.CarrierPersonId != carrier.Id ||
                container.LocationId != seller.LocationId ||
                buyerStorage.OwnerFamilyId != buyer.Id ||
                sellerStorage.OwnerFamilyId != seller.Id ||
                buyer.Wealth < request.FreightFee ||
                CalculateContainerWeight(world, container.Id) + checked(
                    request.Quantity * product.BaseWeight) >
                    container.CapacityWeight)
            {
                throw new InvalidOperationException(
                    "The cross-county civilian freight request is invalid.");
            }

            var freightId = $"civilian_freight.{world.AbsoluteDay}." +
                $"{world.CivilianFreights.Count:D6}";
            var dispatch = _foodInventory.DispatchReservedCivilianFreight(
                world,
                seller.Id,
                sellerStorage.Id,
                buyer.Id,
                container.Id,
                carrier.Id,
                sell.BatchReservations,
                request.Quantity,
                sell.Id,
                freightId,
                sell.CountyGovernanceId);
            buyer.Wealth -= request.FreightFee;
            var trade = _market.SettleCrossCountyDispatch(
                world,
                buy,
                sell,
                request.Quantity,
                dispatch.InventoryTransactionId,
                freightId);
            var destinationLocationId = buyer.LocationId;
            var journey = new JourneyState
            {
                Id = $"journey.{freightId}.leg.0000",
                PersonId = carrier.Id,
                RouteId = route.Id,
                OriginLocationId = routePlan.LegOrigins[0],
                DestinationLocationId = routePlan.LegDestinations[0],
                Mode = TravelMode.Caravan,
                RemainingKilometers = route.DistanceKilometers,
                StartedDay = world.AbsoluteDay,
                StartedSegment = world.Segment
            };
            var freight = new CivilianFreightState
            {
                Id = freightId,
                Status = CivilianFreightStatus.InTransit,
                BuyOrderId = buy.Id,
                SellOrderId = sell.Id,
                FormalMarketTradeId = trade.Id,
                DemandId = demand?.Id ?? string.Empty,
                CarrierOfferId = offer?.Id ?? string.Empty,
                OriginCountyGovernanceId = sell.CountyGovernanceId,
                DestinationCountyGovernanceId = buy.CountyGovernanceId,
                OriginLocationId = seller.LocationId,
                DestinationLocationId = destinationLocationId,
                BuyerFamilyId = buyer.Id,
                BuyerOrganizationId = string.Empty,
                SellerFamilyId = seller.Id,
                BuyerStorageFacilityId = buyerStorage.Id,
                DestinationInventoryContainerId = string.Empty,
                SellerStorageFacilityId = sellerStorage.Id,
                PublicReliefProcurementTradeId = string.Empty,
                SourcePublicReliefEventId = string.Empty,
                SourcePublicReliefCommandId = string.Empty,
                CarrierPersonId = carrier.Id,
                CarrierFamilyId = carrierFamily.Id,
                TransportInventoryContainerId = container.Id,
                RouteId = route.Id,
                JourneyId = journey.Id,
                PlannedRouteIds = new List<string>(requestedRouteIds),
                CurrentRouteIndex = 0,
                DispatchInventoryTransactionId =
                    dispatch.InventoryTransactionId,
                ProductDefinitionId = sell.ProductDefinitionId,
                DispatchedQuantity = request.Quantity,
                RemainingCargoQuantity = request.Quantity,
                GoodsUnitPrice = sell.UnitPrice,
                GoodsMoneyTransferred = trade.MoneyTransferred,
                FreightFee = request.FreightFee,
                FreightFeeEscrow = request.FreightFee,
                ProductPerishabilityBasisPoints =
                    product.PerishabilityBasisPoints,
                FoodSpoilageSensitivityBasisPoints =
                    food.SpoilageSensitivityBasisPoints,
                CargoUnitWeight = product.BaseWeight,
                CreatedDay = world.AbsoluteDay,
                DispatchedDay = world.AbsoluteDay,
                LastLossDay = world.AbsoluteDay
            };
            world.CivilianFreights.Add(freight);
            world.Journeys.Add(journey);
            if (demand != null)
            {
                demand.Status = CivilianFreightDemandStatus.Dispatched;
                demand.AcceptedOfferId = offer.Id;
                demand.CivilianFreightId = freight.Id;
                demand.ClosedDay = world.AbsoluteDay;
                offer.Status = CivilianCarrierOfferStatus.Accepted;
                offer.CivilianFreightId = freight.Id;
                offer.ClosedDay = world.AbsoluteDay;
                CloseCompetingOffers(world, demand.Id, offer.Id);
            }
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.Dispatched,
                carrier.Id,
                dispatch.InventoryTransactionId,
                request.Quantity,
                trade.MoneyTransferred,
                "Cross-county goods dispatched at origin.");
            world.Validate();
            _content.ValidateWorldReferences(world);
            return freight;
        }

        public bool TryPlanKnownRoute(
            WorldState world,
            CivilianCarrierRegistrationState registration,
            string originLocationId,
            string destinationLocationId,
            out List<string> routeIds,
            out int totalDistance,
            out int minimumSecurity)
        {
            var path = registration == null
                ? null
                : FindKnownPath(
                    world,
                    registration.KnownRouteIds,
                    originLocationId,
                    destinationLocationId,
                    registration.RoutePolicyId,
                    registration.MaximumDistanceKilometers);
            routeIds = path == null
                ? new List<string>()
                : new List<string>(path.RouteIds);
            totalDistance = path?.TotalDistance ?? 0;
            minimumSecurity = path?.MinimumSecurity ?? 0;
            return path != null && path.RouteIds.Count > 0;
        }

        public long CalculateRegisteredFreightFee(
            CivilianCarrierRegistrationState registration,
            long quantity,
            int totalDistance)
        {
            if (registration == null || quantity <= 0 || totalDistance <= 0)
            {
                throw new InvalidOperationException(
                    "Registered freight fee inputs are invalid.");
            }
            return CalculateQuotedFee(registration, quantity, totalDistance);
        }

        public long CalculateAvailableQuantityCapacity(
            WorldState world,
            CivilianCarrierRegistrationState registration,
            string productDefinitionId)
        {
            var container = ProductInventorySystem.FindContainer(
                world, registration.TransportInventoryContainerId);
            var product = _content.GetProduct(productDefinitionId);
            return Math.Max(
                0L,
                (container.CapacityWeight -
                    CalculateContainerWeight(world, container.Id)) /
                product.BaseWeight);
        }

        public CivilianFreightState DispatchPublicRelief(
            WorldState world,
            PublicReliefFreightDispatchRequest request)
        {
            if (world == null || request == null)
            {
                throw new ArgumentNullException(
                    world == null ? nameof(world) : nameof(request));
            }
            world.Validate();
            _content.ValidateWorldReferences(world);
            var destination = FindGovernance(
                world, request.DestinationCountyGovernanceId);
            var government = FindOrganization(
                world, request.BuyerOrganizationId);
            var destinationContainer = ProductInventorySystem.FindContainer(
                world, request.DestinationInventoryContainerId);
            var sell = FindOrder(world, request.SellOrderId);
            var seller = ProductInventorySystem.FindFamily(
                world, sell.OwnerFamilyId);
            var sellerStorage = ProductInventorySystem.FindFacility(
                world, sell.StorageFacilityId);
            var carrier = ProductInventorySystem.FindPerson(
                world, request.CarrierPersonId);
            var carrierFamily = ProductInventorySystem.FindFamily(
                world, carrier.FamilyId);
            var transport = ProductInventorySystem.FindContainer(
                world, request.TransportInventoryContainerId);
            var product = _content.GetProduct(sell.ProductDefinitionId);
            var food = _content.GetFood(sell.ProductDefinitionId);
            var routePlan = BuildRoutePlan(
                world, request.RouteIds, seller.LocationId,
                destination.CountyLocationId);
            var firstRoute = FindRoute(world, request.RouteIds[0]);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                government.Type != OrganizationType.Government ||
                destination.GovernmentOrganizationId != government.Id ||
                destination.GranaryInventoryContainerId !=
                    destinationContainer.Id ||
                destinationContainer.OwnerOrganizationId != government.Id ||
                request.Quantity <= 0 || request.FreightFee < 0 ||
                sell.Side != FormalMarketOrderSide.Sell ||
                sell.Status != FormalMarketOrderStatus.Active ||
                sell.CountyGovernanceId == destination.Id ||
                sell.RemainingQuantity < request.Quantity ||
                SumRemainingReservations(sell) < request.Quantity ||
                !carrier.IsAlive || carrier.LocationId != seller.LocationId ||
                HasJourney(world, carrier.Id) ||
                transport.OwnerFamilyId != carrierFamily.Id ||
                !string.IsNullOrEmpty(transport.OwnerOrganizationId) ||
                transport.CarrierPersonId != carrier.Id ||
                transport.LocationId != seller.LocationId ||
                sellerStorage.OwnerFamilyId != seller.Id ||
                government.Treasury < checked(
                    request.Quantity * sell.UnitPrice +
                    request.FreightFee) ||
                CalculateContainerWeight(world, transport.Id) + checked(
                    request.Quantity * product.BaseWeight) >
                    transport.CapacityWeight)
            {
                throw new InvalidOperationException(
                    "The public relief civilian freight request is invalid.");
            }

            var freightId = $"civilian_freight.{world.AbsoluteDay}." +
                $"{world.CivilianFreights.Count:D6}";
            var dispatch = _foodInventory
                .DispatchReservedPublicReliefFreight(
                    world,
                    seller.Id,
                    sellerStorage.Id,
                    government.Id,
                    transport.Id,
                    carrier.Id,
                    sell.BatchReservations,
                    request.Quantity,
                    sell.Id,
                    freightId,
                    sell.CountyGovernanceId);
            var goodsMoney = checked(request.Quantity * sell.UnitPrice);
            government.Treasury = checked(
                government.Treasury - goodsMoney - request.FreightFee);
            seller.Wealth = checked(seller.Wealth + goodsMoney);
            sell.RemainingQuantity = checked(
                sell.RemainingQuantity - request.Quantity);
            sell.FilledQuantity = checked(
                sell.FilledQuantity + request.Quantity);
            sell.SettledMoney = checked(sell.SettledMoney + goodsMoney);
            if (sell.RemainingQuantity == 0)
            {
                sell.Status = FormalMarketOrderStatus.Filled;
                sell.ClosedDay = world.AbsoluteDay;
                sell.CloseReason = "filled_by_external_public_relief";
            }

            var trade = new PublicReliefProcurementTradeState
            {
                Id = $"public_relief_procurement_trade.{world.AbsoluteDay}." +
                    $"{world.PublicReliefProcurementTrades.Count:D6}",
                Day = world.AbsoluteDay,
                CountyGovernanceId = destination.Id,
                SourceCountyGovernanceId = sell.CountyGovernanceId,
                BuyerOrganizationId = government.Id,
                DestinationInventoryContainerId = destinationContainer.Id,
                SourceShortfallEventId = request.SourcePublicReliefEventId,
                SourceCommandId = request.SourcePublicReliefCommandId,
                SellOrderId = sell.Id,
                SellerFamilyId = seller.Id,
                ProductDefinitionId = sell.ProductDefinitionId,
                Quantity = request.Quantity,
                UnitPrice = sell.UnitPrice,
                MoneyTransferred = goodsMoney,
                InventoryTransactionId = dispatch.InventoryTransactionId,
                CivilianFreightId = freightId,
                FreightFee = request.FreightFee,
                PublicReliefRecoveryId =
                    request.PublicReliefRecoveryId ?? string.Empty,
                IsSupplementalPublicReliefProcurement =
                    request.IsSupplemental
            };
            world.PublicReliefProcurementTrades.Add(trade);
            UpdatePublicReliefMarketPrice(world, trade);
            var journey = new JourneyState
            {
                Id = $"journey.{freightId}.leg.0000",
                PersonId = carrier.Id,
                RouteId = firstRoute.Id,
                OriginLocationId = routePlan.LegOrigins[0],
                DestinationLocationId = routePlan.LegDestinations[0],
                Mode = TravelMode.Caravan,
                RemainingKilometers = firstRoute.DistanceKilometers,
                StartedDay = world.AbsoluteDay,
                StartedSegment = world.Segment
            };
            var freight = new CivilianFreightState
            {
                Id = freightId,
                Status = CivilianFreightStatus.InTransit,
                BuyOrderId = string.Empty,
                SellOrderId = sell.Id,
                FormalMarketTradeId = string.Empty,
                DemandId = string.Empty,
                CarrierOfferId = string.Empty,
                OriginCountyGovernanceId = sell.CountyGovernanceId,
                DestinationCountyGovernanceId = destination.Id,
                OriginLocationId = seller.LocationId,
                DestinationLocationId = destination.CountyLocationId,
                BuyerFamilyId = string.Empty,
                BuyerOrganizationId = government.Id,
                SellerFamilyId = seller.Id,
                BuyerStorageFacilityId = string.Empty,
                DestinationInventoryContainerId = destinationContainer.Id,
                SellerStorageFacilityId = sellerStorage.Id,
                PublicReliefProcurementTradeId = trade.Id,
                SourcePublicReliefEventId = request.SourcePublicReliefEventId,
                SourcePublicReliefCommandId = request.SourcePublicReliefCommandId,
                PublicReliefRecoveryId =
                    request.PublicReliefRecoveryId ?? string.Empty,
                IsSupplementalPublicReliefFreight = request.IsSupplemental,
                CarrierPersonId = carrier.Id,
                CarrierFamilyId = carrierFamily.Id,
                TransportInventoryContainerId = transport.Id,
                RouteId = firstRoute.Id,
                JourneyId = journey.Id,
                PlannedRouteIds = new List<string>(request.RouteIds),
                CurrentRouteIndex = 0,
                DispatchInventoryTransactionId = dispatch.InventoryTransactionId,
                ProductDefinitionId = sell.ProductDefinitionId,
                DispatchedQuantity = request.Quantity,
                RemainingCargoQuantity = request.Quantity,
                GoodsUnitPrice = sell.UnitPrice,
                GoodsMoneyTransferred = goodsMoney,
                FreightFee = request.FreightFee,
                FreightFeeEscrow = request.FreightFee,
                ProductPerishabilityBasisPoints =
                    product.PerishabilityBasisPoints,
                FoodSpoilageSensitivityBasisPoints =
                    food.SpoilageSensitivityBasisPoints,
                CargoUnitWeight = product.BaseWeight,
                CreatedDay = world.AbsoluteDay,
                DispatchedDay = world.AbsoluteDay,
                LastLossDay = world.AbsoluteDay
            };
            world.CivilianFreights.Add(freight);
            world.Journeys.Add(journey);
            AddLedger(
                world, freight, CivilianFreightLedgerType.Dispatched,
                carrier.Id, dispatch.InventoryTransactionId,
                request.Quantity, goodsMoney,
                "Cross-county public relief goods dispatched at origin.");
            return freight;
        }

        public void ResolveDailyTransit(WorldState world)
        {
            var active = ActiveFreights(world);
            for (var i = 0; i < active.Count; i++)
            {
                var freight = active[i];
                if (freight.Status != CivilianFreightStatus.InTransit ||
                    !HasJourney(world, freight.CarrierPersonId))
                {
                    continue;
                }
                for (var day = freight.LastLossDay + 1;
                     day <= world.AbsoluteDay;
                     day++)
                {
                    ApplyNaturalLoss(world, freight, day);
                    freight.LastLossDay = day;
                }
            }
        }

        public void ResolveArrivals(WorldState world)
        {
            var active = ActiveFreights(world);
            for (var i = 0; i < active.Count; i++)
            {
                var freight = active[i];
                if (freight.Status ==
                    CivilianFreightStatus.AwaitingNextLeg)
                {
                    StartNextLeg(world, freight);
                    continue;
                }
                if (freight.Status == CivilianFreightStatus.InTransit)
                {
                    if (HasJourney(world, freight.CarrierPersonId))
                    {
                        continue;
                    }
                    var carrier = ProductInventorySystem.FindPerson(
                        world, freight.CarrierPersonId);
                    var container = ProductInventorySystem.FindContainer(
                        world, freight.TransportInventoryContainerId);
                    if (carrier.LocationId != freight.DestinationLocationId ||
                        container.LocationId != freight.DestinationLocationId)
                    {
                        continue;
                    }
                    freight.Status = CivilianFreightStatus.AwaitingReceipt;
                    freight.ArrivedDay = world.AbsoluteDay;
                }
                if (freight.Status != CivilianFreightStatus.AwaitingReceipt)
                {
                    continue;
                }
                if (freight.RemainingCargoQuantity > 0)
                {
                    var delivery = string.IsNullOrEmpty(
                            freight.BuyerOrganizationId)
                        ? _foodInventory.DeliverCivilianFreight(
                            world,
                            freight.Id,
                            freight.DispatchInventoryTransactionId,
                            freight.BuyerFamilyId,
                            freight.BuyerStorageFacilityId,
                            freight.TransportInventoryContainerId,
                            freight.ProductDefinitionId,
                            freight.CarrierPersonId,
                            freight.RemainingCargoQuantity)
                        : _foodInventory.DeliverPublicReliefFreight(
                            world,
                            freight.Id,
                            freight.DispatchInventoryTransactionId,
                            freight.BuyerOrganizationId,
                            freight.DestinationInventoryContainerId,
                            freight.TransportInventoryContainerId,
                            freight.ProductDefinitionId,
                            freight.CarrierPersonId,
                            freight.RemainingCargoQuantity);
                    if (delivery.TransferredPhysicalQuantity > 0)
                    {
                        freight.RemainingCargoQuantity -=
                            delivery.TransferredPhysicalQuantity;
                        freight.DeliveredQuantity +=
                            delivery.TransferredPhysicalQuantity;
                        AddLedger(
                            world,
                            freight,
                            CivilianFreightLedgerType.Delivered,
                            freight.CarrierPersonId,
                            delivery.InventoryTransactionId,
                            delivery.TransferredPhysicalQuantity,
                            0,
                            "Civilian freight unloaded into the buyer granary.");
                    }
                }
                if (freight.RemainingCargoQuantity == 0)
                {
                    Complete(world, freight);
                }
            }
        }

        private static void StartNextLeg(
            WorldState world,
            CivilianFreightState freight)
        {
            var nextIndex = freight.CurrentRouteIndex + 1;
            if (freight.PlannedRouteIds == null ||
                nextIndex >= freight.PlannedRouteIds.Count)
            {
                throw new InvalidOperationException(
                    $"Civilian freight {freight.Id} has no next route leg.");
            }
            var routePlan = BuildRoutePlan(
                world,
                freight.PlannedRouteIds,
                freight.OriginLocationId,
                freight.DestinationLocationId);
            var route = FindRoute(
                world, freight.PlannedRouteIds[nextIndex]);
            var carrier = ProductInventorySystem.FindPerson(
                world, freight.CarrierPersonId);
            var container = ProductInventorySystem.FindContainer(
                world, freight.TransportInventoryContainerId);
            if (carrier.LocationId != routePlan.LegOrigins[nextIndex] ||
                container.LocationId != routePlan.LegOrigins[nextIndex] ||
                HasJourney(world, carrier.Id))
            {
                throw new InvalidOperationException(
                    $"Civilian freight {freight.Id} cannot start its next leg.");
            }
            var journey = new JourneyState
            {
                Id = $"journey.{freight.Id}.leg.{nextIndex:D4}",
                PersonId = carrier.Id,
                RouteId = route.Id,
                OriginLocationId = routePlan.LegOrigins[nextIndex],
                DestinationLocationId = routePlan.LegDestinations[nextIndex],
                Mode = TravelMode.Caravan,
                RemainingKilometers = route.DistanceKilometers,
                StartedDay = world.AbsoluteDay,
                StartedSegment = world.Segment
            };
            freight.CurrentRouteIndex = nextIndex;
            freight.RouteId = route.Id;
            freight.JourneyId = journey.Id;
            freight.Status = CivilianFreightStatus.InTransit;
            world.Journeys.Add(journey);
            world.Validate();
        }

        private void ApplyNaturalLoss(
            WorldState world,
            CivilianFreightState freight,
            long day)
        {
            if (freight.RemainingCargoQuantity <= 0)
            {
                return;
            }
            var numerator = checked(
                freight.RemainingCargoQuantity *
                freight.ProductPerishabilityBasisPoints *
                (long)freight.FoodSpoilageSensitivityBasisPoints);
            var loss = numerator / LossDenominator;
            var remainderChance = (int)Math.Min(
                10_000L,
                numerator % LossDenominator * 10_000L / LossDenominator);
            if (loss == 0 && remainderChance > 0 &&
                _random.CheckBasisPoints(
                    RandomSystemId,
                    new StableId(freight.Id),
                    day,
                    "natural_loss_rounding",
                    remainderChance))
            {
                loss = 1;
            }
            loss = Math.Min(loss, freight.RemainingCargoQuantity);
            if (loss <= 0)
            {
                return;
            }
            var result = string.IsNullOrEmpty(freight.BuyerOrganizationId)
                ? _foodInventory.LoseCivilianFreight(
                    world,
                    freight.Id,
                    freight.DispatchInventoryTransactionId,
                    freight.BuyerFamilyId,
                    freight.TransportInventoryContainerId,
                    freight.ProductDefinitionId,
                    freight.CarrierPersonId,
                    loss)
                : _foodInventory.LosePublicReliefFreight(
                    world,
                    freight.Id,
                    freight.DispatchInventoryTransactionId,
                    freight.BuyerOrganizationId,
                    freight.TransportInventoryContainerId,
                    freight.ProductDefinitionId,
                    freight.CarrierPersonId,
                    loss);
            if (result.TransferredPhysicalQuantity != loss)
            {
                throw new InvalidOperationException(
                    $"Civilian freight {freight.Id} loss could not be applied.");
            }
            freight.RemainingCargoQuantity -= loss;
            freight.NaturalLossQuantity += loss;
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.NaturalLoss,
                freight.CarrierPersonId,
                result.InventoryTransactionId,
                loss,
                0,
                "Deterministic natural transit loss.");
        }

        private static void Complete(
            WorldState world,
            CivilianFreightState freight)
        {
            var carrierFamily = ProductInventorySystem.FindFamily(
                world, freight.CarrierFamilyId);
            carrierFamily.Wealth = checked(
                carrierFamily.Wealth + freight.FreightFeeEscrow);
            freight.FreightFeePaid = checked(
                freight.FreightFeePaid + freight.FreightFeeEscrow);
            var paid = freight.FreightFeeEscrow;
            freight.FreightFeeEscrow = 0;
            freight.Status = CivilianFreightStatus.Completed;
            freight.CompletedDay = world.AbsoluteDay;
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.FreightFeePaid,
                freight.CarrierPersonId,
                string.Empty,
                0,
                paid,
                "Freight fee released to the carrier family.");
        }

        private static void AddLedger(
            WorldState world,
            CivilianFreightState freight,
            CivilianFreightLedgerType type,
            string actorPersonId,
            string inventoryTransactionId,
            long quantity,
            long money,
            string summary)
        {
            world.CivilianFreightLedgerEntries.Add(
                new CivilianFreightLedgerEntryState
                {
                    Id = $"civilian_freight_ledger.{world.AbsoluteDay}." +
                         $"{world.CivilianFreightLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = type,
                    CivilianFreightId = freight.Id,
                    ActorPersonId = actorPersonId,
                    InventoryTransactionId = inventoryTransactionId,
                    Quantity = quantity,
                    Money = money,
                    Summary = summary
                });
        }

        private static void UpdatePublicReliefMarketPrice(
            WorldState world,
            PublicReliefProcurementTradeState trade)
        {
            FormalMarketPriceState price = null;
            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                var candidate = world.FormalMarketPrices[i];
                if (candidate.CountyGovernanceId ==
                        trade.SourceCountyGovernanceId &&
                    candidate.ProductDefinitionId ==
                        trade.ProductDefinitionId)
                {
                    price = candidate;
                    break;
                }
            }
            if (price == null)
            {
                price = new FormalMarketPriceState
                {
                    Id = "formal_market_price." +
                        trade.SourceCountyGovernanceId + "." +
                        trade.ProductDefinitionId,
                    CountyGovernanceId = trade.SourceCountyGovernanceId,
                    ProductDefinitionId = trade.ProductDefinitionId,
                    EquilibriumUnitPrice = trade.UnitPrice,
                    LastTradeUnitPrice = trade.UnitPrice,
                    LastTradeDay = trade.Day
                };
                world.FormalMarketPrices.Add(price);
            }
            price.LastTradeUnitPrice = trade.UnitPrice;
            price.LastTradeDay = trade.Day;
            price.CumulativeTradedQuantity = checked(
                price.CumulativeTradedQuantity + trade.Quantity);
            price.CumulativeTurnover = checked(
                price.CumulativeTurnover + trade.MoneyTransferred);
        }

        private static List<CivilianFreightState> ActiveFreights(
            WorldState world)
        {
            var result = new List<CivilianFreightState>();
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                if (world.CivilianFreights[i].Status !=
                    CivilianFreightStatus.Completed)
                {
                    result.Add(world.CivilianFreights[i]);
                }
            }
            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static long SumRemainingReservations(
            FormalMarketOrderState order)
        {
            long result = 0;
            for (var i = 0; i < order.BatchReservations.Count; i++)
            {
                result = checked(
                    result + order.BatchReservations[i].RemainingQuantity);
            }
            return result;
        }

        private static bool ReservationsMeetQuality(
            WorldState world,
            FormalMarketOrderState sell,
            FormalMarketOrderState buy)
        {
            for (var i = 0; i < sell.BatchReservations.Count; i++)
            {
                var reservation = sell.BatchReservations[i];
                if (reservation.RemainingQuantity > 0 &&
                    FindBatch(world, reservation.BatchId).QualityBasisPoints <
                    buy.MinimumQualityBasisPoints)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasJourney(WorldState world, string personId)
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

        private static long CalculateContainerWeight(
            WorldState world,
            string containerId)
        {
            long result = 0;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].InventoryContainerId == containerId)
                {
                    result = checked(
                        result + world.ProductBatches[i].Quantity *
                        world.ProductBatches[i].UnitWeight);
                }
            }
            return result;
        }

        private void ExpireInvalidDemands(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            for (var i = 0; i < world.CivilianFreightDemands.Count; i++)
            {
                var demand = world.CivilianFreightDemands[i];
                if (demand.Status != CivilianFreightDemandStatus.Active)
                {
                    continue;
                }
                var buy = TryFindOrder(world, demand.BuyOrderId);
                var sell = TryFindOrder(world, demand.SellOrderId);
                var expired = world.AbsoluteDay > demand.ExpiryDay;
                var invalid = buy == null || sell == null ||
                    buy.Status != FormalMarketOrderStatus.Active ||
                    sell.Status != FormalMarketOrderStatus.Active ||
                    buy.RemainingQuantity < demand.Quantity ||
                    sell.RemainingQuantity < demand.Quantity ||
                    SumRemainingReservations(sell) < demand.Quantity;
                if (!expired && !invalid)
                {
                    continue;
                }
                demand.Status = expired
                    ? CivilianFreightDemandStatus.Expired
                    : CivilianFreightDemandStatus.Cancelled;
                demand.ClosedDay = world.AbsoluteDay;
                for (var offerIndex = 0;
                     offerIndex < world.CivilianCarrierOffers.Count;
                     offerIndex++)
                {
                    var offer = world.CivilianCarrierOffers[offerIndex];
                    if (offer.DemandId == demand.Id &&
                        offer.Status == CivilianCarrierOfferStatus.Active)
                    {
                        offer.Status = CivilianCarrierOfferStatus.Withdrawn;
                        offer.ClosedDay = world.AbsoluteDay;
                    }
                }
            }
            world.Validate();
        }

        private static bool HasActiveDemandForOrder(
            WorldState world,
            string orderId)
        {
            for (var i = 0; i < world.CivilianFreightDemands.Count; i++)
            {
                var demand = world.CivilianFreightDemands[i];
                if (demand.Status == CivilianFreightDemandStatus.Active &&
                    (demand.BuyOrderId == orderId ||
                     demand.SellOrderId == orderId))
                {
                    return true;
                }
            }
            return false;
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
            return day != 0
                ? day
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static List<CivilianFreightDemandState> ActiveDemands(
            WorldState world)
        {
            var result = new List<CivilianFreightDemandState>();
            for (var i = 0; i < world.CivilianFreightDemands.Count; i++)
            {
                if (world.CivilianFreightDemands[i].Status ==
                    CivilianFreightDemandStatus.Active)
                {
                    result.Add(world.CivilianFreightDemands[i]);
                }
            }
            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static List<CivilianCarrierRegistrationState>
            ActiveRegistrations(WorldState world)
        {
            var result = new List<CivilianCarrierRegistrationState>();
            for (var i = 0;
                 i < world.CivilianCarrierRegistrations.Count;
                 i++)
            {
                if (world.CivilianCarrierRegistrations[i].Active)
                {
                    result.Add(world.CivilianCarrierRegistrations[i]);
                }
            }
            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static bool HasOffer(
            WorldState world,
            string demandId,
            string registrationId)
        {
            for (var i = 0; i < world.CivilianCarrierOffers.Count; i++)
            {
                var offer = world.CivilianCarrierOffers[i];
                if (offer.DemandId == demandId &&
                    offer.CarrierRegistrationId == registrationId)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanCarrierServeDemand(
            WorldState world,
            CivilianCarrierRegistrationState registration,
            CivilianFreightDemandState demand,
            out int productWeight)
        {
            productWeight = 0;
            if (!registration.Active ||
                registration.RoutePolicyId != demand.RoutePolicyId)
            {
                return false;
            }
            var carrier = ProductInventorySystem.FindPerson(
                world, registration.CarrierPersonId);
            var container = ProductInventorySystem.FindContainer(
                world, registration.TransportInventoryContainerId);
            if (!carrier.IsAlive || HasJourney(world, carrier.Id) ||
                carrier.LocationId != demand.OriginLocationId ||
                container.LocationId != demand.OriginLocationId ||
                container.CarrierPersonId != carrier.Id ||
                container.OwnerFamilyId != registration.CarrierFamilyId)
            {
                return false;
            }
            productWeight = _content.GetProduct(
                demand.ProductDefinitionId).BaseWeight;
            return CalculateContainerWeight(world, container.Id) + checked(
                    demand.Quantity * productWeight) <=
                container.CapacityWeight;
        }

        private static long CalculateQuotedFee(
            CivilianCarrierRegistrationState registration,
            long quantity,
            int totalDistance)
        {
            return checked(
                registration.BaseFee +
                registration.FeePerKilometer * totalDistance +
                registration.FeePerHundredUnits * ((quantity + 99) / 100));
        }

        private static List<CivilianCarrierOfferState> ActiveOffersFor(
            WorldState world,
            string demandId)
        {
            var result = new List<CivilianCarrierOfferState>();
            for (var i = 0; i < world.CivilianCarrierOffers.Count; i++)
            {
                var offer = world.CivilianCarrierOffers[i];
                if (offer.DemandId == demandId &&
                    offer.Status == CivilianCarrierOfferStatus.Active)
                {
                    result.Add(offer);
                }
            }
            return result;
        }

        private static int CompareOffers(
            CivilianCarrierOfferState left,
            CivilianCarrierOfferState right)
        {
            var fee = left.QuotedFreightFee.CompareTo(
                right.QuotedFreightFee);
            if (fee != 0)
            {
                return fee;
            }
            var security = right.MinimumSecurityBasisPoints.CompareTo(
                left.MinimumSecurityBasisPoints);
            if (security != 0)
            {
                return security;
            }
            var distance = left.TotalDistanceKilometers.CompareTo(
                right.TotalDistanceKilometers);
            return distance != 0
                ? distance
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static void CloseCompetingOffers(
            WorldState world,
            string demandId,
            string acceptedOfferId)
        {
            for (var i = 0; i < world.CivilianCarrierOffers.Count; i++)
            {
                var offer = world.CivilianCarrierOffers[i];
                if (offer.DemandId == demandId &&
                    offer.Id != acceptedOfferId &&
                    offer.Status == CivilianCarrierOfferStatus.Active)
                {
                    offer.Status = CivilianCarrierOfferStatus.Rejected;
                    offer.ClosedDay = world.AbsoluteDay;
                }
            }
        }

        private static List<string> RequestRouteIds(
            CivilianFreightDispatchRequest request)
        {
            if (request.RouteIds != null && request.RouteIds.Count > 0)
            {
                return new List<string>(request.RouteIds);
            }
            if (!string.IsNullOrEmpty(request.RouteId))
            {
                return new List<string> { request.RouteId };
            }
            throw new InvalidOperationException(
                "Civilian freight requires at least one route.");
        }

        private static List<string> SortedUniqueRouteIds(
            WorldState world,
            IList<string> routeIds)
        {
            if (routeIds == null)
            {
                throw new InvalidOperationException(
                    "Known civilian freight routes are required.");
            }
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();
            for (var i = 0; i < routeIds.Count; i++)
            {
                FindRoute(world, routeIds[i]);
                if (!unique.Add(routeIds[i]))
                {
                    throw new InvalidOperationException(
                        "Known civilian freight routes must be unique.");
                }
                result.Add(routeIds[i]);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static bool IsSupportedRoutePolicy(string routePolicyId)
        {
            return routePolicyId ==
                    CivilianFreightRoutePolicyIds.ShortestKnown ||
                routePolicyId == CivilianFreightRoutePolicyIds.SafestKnown;
        }

        private static bool RouteIdsEqual(
            IList<string> left,
            IList<string> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }
            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static CivilianRoutePath BuildRoutePlan(
            WorldState world,
            IList<string> routeIds,
            string originLocationId,
            string destinationLocationId)
        {
            if (routeIds == null || routeIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Civilian freight route plan is empty.");
            }
            var result = new CivilianRoutePath();
            var visited = new HashSet<string>(StringComparer.Ordinal)
            {
                originLocationId
            };
            var current = originLocationId;
            result.MinimumSecurity = 10_000;
            for (var i = 0; i < routeIds.Count; i++)
            {
                var route = FindRoute(world, routeIds[i]);
                string next;
                if (route.FromLocationId == current)
                {
                    next = route.ToLocationId;
                }
                else if (route.Bidirectional &&
                    route.ToLocationId == current)
                {
                    next = route.FromLocationId;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Civilian freight route plan is not continuous.");
                }
                if (!visited.Add(next))
                {
                    throw new InvalidOperationException(
                        "Civilian freight route plan contains a cycle.");
                }
                result.RouteIds.Add(route.Id);
                result.LegOrigins.Add(current);
                result.LegDestinations.Add(next);
                result.TotalDistance = checked(
                    result.TotalDistance + route.DistanceKilometers);
                result.MinimumSecurity = Math.Min(
                    result.MinimumSecurity, route.SecurityBasisPoints);
                current = next;
            }
            if (current != destinationLocationId)
            {
                throw new InvalidOperationException(
                    "Civilian freight route plan does not reach its destination.");
            }
            return result;
        }

        private static CivilianRoutePath FindKnownPath(
            WorldState world,
            IList<string> knownRouteIds,
            string originLocationId,
            string destinationLocationId,
            string routePolicyId,
            int maximumDistance)
        {
            if (!IsSupportedRoutePolicy(routePolicyId))
            {
                return null;
            }
            var knownRoutes = new List<RouteState>();
            for (var i = 0; i < knownRouteIds.Count; i++)
            {
                knownRoutes.Add(FindRoute(world, knownRouteIds[i]));
            }
            knownRoutes.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var start = new CivilianRoutePath
            {
                CurrentLocationId = originLocationId,
                MinimumSecurity = 10_000
            };
            var open = new List<CivilianRoutePath> { start };
            var labelsByLocation =
                new Dictionary<string, List<CivilianRoutePath>>(
                    StringComparer.Ordinal)
            {
                [originLocationId] = new List<CivilianRoutePath> { start }
            };
            while (open.Count > 0)
            {
                open.Sort((left, right) =>
                    ComparePaths(left, right, routePolicyId));
                var current = open[0];
                open.RemoveAt(0);
                if (!labelsByLocation.TryGetValue(
                        current.CurrentLocationId, out var currentLabels) ||
                    !currentLabels.Contains(current))
                {
                    continue;
                }
                if (current.CurrentLocationId == destinationLocationId)
                {
                    return current;
                }
                for (var routeIndex = 0;
                     routeIndex < knownRoutes.Count;
                     routeIndex++)
                {
                    var route = knownRoutes[routeIndex];
                    string next = null;
                    if (route.FromLocationId == current.CurrentLocationId)
                    {
                        next = route.ToLocationId;
                    }
                    else if (route.Bidirectional &&
                        route.ToLocationId == current.CurrentLocationId)
                    {
                        next = route.FromLocationId;
                    }
                    if (next == null)
                    {
                        continue;
                    }
                    var distance = checked(
                        current.TotalDistance + route.DistanceKilometers);
                    if (distance > maximumDistance)
                    {
                        continue;
                    }
                    var candidate = current.Copy();
                    candidate.RouteIds.Add(route.Id);
                    candidate.LegOrigins.Add(current.CurrentLocationId);
                    candidate.LegDestinations.Add(next);
                    candidate.CurrentLocationId = next;
                    candidate.TotalDistance = distance;
                    candidate.MinimumSecurity = Math.Min(
                        current.MinimumSecurity,
                        route.SecurityBasisPoints);
                    if (TryRecordPath(
                        labelsByLocation, candidate, routePolicyId))
                    {
                        open.Add(candidate);
                    }
                }
            }
            return null;
        }

        private static bool TryRecordPath(
            IDictionary<string, List<CivilianRoutePath>> labelsByLocation,
            CivilianRoutePath candidate,
            string routePolicyId)
        {
            if (!labelsByLocation.TryGetValue(
                    candidate.CurrentLocationId, out var labels))
            {
                labels = new List<CivilianRoutePath>();
                labelsByLocation.Add(candidate.CurrentLocationId, labels);
            }

            for (var i = 0; i < labels.Count; i++)
            {
                var existing = labels[i];
                var dominated = routePolicyId ==
                        CivilianFreightRoutePolicyIds.SafestKnown
                    ? existing.MinimumSecurity >= candidate.MinimumSecurity &&
                      existing.TotalDistance <= candidate.TotalDistance &&
                      (existing.MinimumSecurity != candidate.MinimumSecurity ||
                       existing.TotalDistance != candidate.TotalDistance ||
                       ComparePathKeys(existing, candidate) <= 0)
                    : ComparePaths(existing, candidate, routePolicyId) <= 0;
                if (dominated)
                {
                    return false;
                }
            }

            for (var i = labels.Count - 1; i >= 0; i--)
            {
                var existing = labels[i];
                var candidateDominates = routePolicyId ==
                        CivilianFreightRoutePolicyIds.SafestKnown
                    ? candidate.MinimumSecurity >= existing.MinimumSecurity &&
                      candidate.TotalDistance <= existing.TotalDistance &&
                      (candidate.MinimumSecurity != existing.MinimumSecurity ||
                       candidate.TotalDistance != existing.TotalDistance ||
                       ComparePathKeys(candidate, existing) < 0)
                    : ComparePaths(candidate, existing, routePolicyId) < 0;
                if (candidateDominates)
                {
                    labels.RemoveAt(i);
                }
            }
            labels.Add(candidate);
            return true;
        }

        private static int ComparePaths(
            CivilianRoutePath left,
            CivilianRoutePath right,
            string routePolicyId)
        {
            if (routePolicyId ==
                CivilianFreightRoutePolicyIds.SafestKnown)
            {
                var security = right.MinimumSecurity.CompareTo(
                    left.MinimumSecurity);
                if (security != 0)
                {
                    return security;
                }
                var safestDistance = left.TotalDistance.CompareTo(
                    right.TotalDistance);
                if (safestDistance != 0)
                {
                    return safestDistance;
                }
            }
            else
            {
                var distance = left.TotalDistance.CompareTo(
                    right.TotalDistance);
                if (distance != 0)
                {
                    return distance;
                }
                var security = right.MinimumSecurity.CompareTo(
                    left.MinimumSecurity);
                if (security != 0)
                {
                    return security;
                }
            }
            return ComparePathKeys(left, right);
        }

        private static int ComparePathKeys(
            CivilianRoutePath left,
            CivilianRoutePath right)
        {
            return string.CompareOrdinal(
                string.Join("|", left.RouteIds),
                string.Join("|", right.RouteIds));
        }

        private static CivilianFreightDemandState FindDemand(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.CivilianFreightDemands.Count; i++)
            {
                if (world.CivilianFreightDemands[i].Id == id)
                {
                    return world.CivilianFreightDemands[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown civilian freight demand {id}.");
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].Id == id)
                {
                    return world.CountyGovernances[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown county governance {id}.");
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
            throw new InvalidOperationException(
                $"Unknown organization {id}.");
        }

        private static CivilianCarrierOfferState FindOffer(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.CivilianCarrierOffers.Count; i++)
            {
                if (world.CivilianCarrierOffers[i].Id == id)
                {
                    return world.CivilianCarrierOffers[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown civilian carrier offer {id}.");
        }

        private static CivilianCarrierRegistrationState FindRegistration(
            WorldState world,
            string id)
        {
            for (var i = 0;
                 i < world.CivilianCarrierRegistrations.Count;
                 i++)
            {
                if (world.CivilianCarrierRegistrations[i].Id == id)
                {
                    return world.CivilianCarrierRegistrations[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown civilian carrier registration {id}.");
        }

        private static FormalMarketOrderState TryFindOrder(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                if (world.FormalMarketOrders[i].Id == id)
                {
                    return world.FormalMarketOrders[i];
                }
            }
            return null;
        }

        private sealed class CivilianRoutePath
        {
            public string CurrentLocationId;
            public List<string> RouteIds = new List<string>();
            public List<string> LegOrigins = new List<string>();
            public List<string> LegDestinations = new List<string>();
            public int TotalDistance;
            public int MinimumSecurity;

            public CivilianRoutePath Copy()
            {
                return new CivilianRoutePath
                {
                    CurrentLocationId = CurrentLocationId,
                    RouteIds = new List<string>(RouteIds),
                    LegOrigins = new List<string>(LegOrigins),
                    LegDestinations = new List<string>(LegDestinations),
                    TotalDistance = TotalDistance,
                    MinimumSecurity = MinimumSecurity
                };
            }
        }

        private static FormalMarketOrderState FindOrder(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                if (world.FormalMarketOrders[i].Id == id)
                {
                    return world.FormalMarketOrders[i];
                }
            }
            throw new InvalidOperationException(
                $"Unknown formal market order {id}.");
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
            throw new InvalidOperationException($"Unknown route {id}.");
        }

        private static ProductBatchState FindBatch(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == id)
                {
                    return world.ProductBatches[i];
                }
            }
            throw new InvalidOperationException($"Unknown batch {id}.");
        }

        private void RequireFormalWorld(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            _content.ValidateWorldReferences(world);
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                throw new InvalidOperationException(
                    "Civilian freight planning requires formal food inventory authority.");
            }
        }
    }

    public sealed class CivilianFreightPlanningCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.civilian_freight.plan_daily";
        public const string IssuerId = "system.civilian_freight_planning";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string MaximumNewDemandsArgumentId =
            "maximum_new_demands";
        public const string MaximumNewOffersArgumentId = "maximum_new_offers";
        public const string MaximumDispatchesArgumentId =
            "maximum_dispatches";
        public const string TransactionKindId =
            "mandate.transaction.civilian_freight.plan_daily";
        public const string EventTypeId =
            "mandate.event.civilian_freight.planning_resolved";
        public const string ProjectionHandlerId =
            "mandate.handler.civilian_freight.planning_projection";

        private const int DefaultMaximumNewDemands = 64;
        private const int DefaultMaximumNewOffers = 256;
        private const int DefaultMaximumDispatches = 32;

        private readonly CivilianFreightSystem _freight;

        public CivilianFreightPlanningCommandScheduler(
            CivilianFreightSystem freight)
        {
            _freight = freight ?? throw new ArgumentNullException(
                nameof(freight));
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
                !_freight.HasDailyPlanningWork(world))
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
                    5,
                    new Dictionary<string, string>
                    {
                        {
                            ExpectedDayArgumentId,
                            Invariant(world.AbsoluteDay)
                        },
                        {
                            MaximumNewDemandsArgumentId,
                            Invariant(DefaultMaximumNewDemands)
                        },
                        {
                            MaximumNewOffersArgumentId,
                            Invariant(DefaultMaximumNewOffers)
                        },
                        {
                            MaximumDispatchesArgumentId,
                            Invariant(DefaultMaximumDispatches)
                        }
                    }));
            return true;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new CivilianFreightPlanningCommandHandler(_freight);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new CivilianFreightPlanningProjectionHandler();

        public static string DailyCommandId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "civilian_freight.planning_command.{0:D10}",
            day);

        public static string DailyTransactionId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "civilian_freight.planning_transaction.{0:D10}",
            day);

        public static string DailyEventId(long day) => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "civilian_freight.planning_resolved.{0:D10}",
            day);

        private static string Invariant(long value) => value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        private sealed class CivilianFreightPlanningCommandHandler :
            IWorldCommandHandler
        {
            private readonly CivilianFreightSystem _freight;

            public CivilianFreightPlanningCommandHandler(
                CivilianFreightSystem freight)
            {
                _freight = freight;
            }

            public string CommandTypeId =>
                CivilianFreightPlanningCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 4 ||
                    !TryReadLong(
                        command, ExpectedDayArgumentId, out var expectedDay) ||
                    !TryReadInt(
                        command,
                        MaximumNewDemandsArgumentId,
                        out var maximumNewDemands) ||
                    !TryReadInt(
                        command,
                        MaximumNewOffersArgumentId,
                        out var maximumNewOffers) ||
                    !TryReadInt(
                        command,
                        MaximumDispatchesArgumentId,
                        out var maximumDispatches))
                {
                    throw new InvalidOperationException(
                        "Civilian freight planning command arguments are invalid.");
                }

                transactions.Add(new CivilianFreightPlanningTransaction(
                    _freight,
                    expectedDay,
                    maximumNewDemands,
                    maximumNewOffers,
                    maximumDispatches));
            }

            private static bool TryReadLong(
                WorldCommandEnvelope command,
                string key,
                out long value)
            {
                value = 0;
                return command.Arguments.TryGetValue(key, out var text) &&
                    long.TryParse(
                        text,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value) &&
                    value >= 0;
            }

            private static bool TryReadInt(
                WorldCommandEnvelope command,
                string key,
                out int value)
            {
                value = 0;
                return command.Arguments.TryGetValue(key, out var text) &&
                    int.TryParse(
                        text,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value) &&
                    value >= 0;
            }
        }

        private sealed class CivilianFreightPlanningTransaction :
            IWorldTransaction
        {
            private readonly CivilianFreightSystem _freight;
            private readonly long _expectedDay;
            private readonly int _maximumNewDemands;
            private readonly int _maximumNewOffers;
            private readonly int _maximumDispatches;

            public CivilianFreightPlanningTransaction(
                CivilianFreightSystem freight,
                long expectedDay,
                int maximumNewDemands,
                int maximumNewOffers,
                int maximumDispatches)
            {
                _freight = freight;
                _expectedDay = expectedDay;
                _maximumNewDemands = maximumNewDemands;
                _maximumNewOffers = maximumNewOffers;
                _maximumDispatches = maximumDispatches;
                Id = DailyTransactionId(expectedDay);
            }

            public string Id { get; }

            public string KindId => TransactionKindId;

            public int Priority => 5;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _freight.ValidateDailyPlanning(world, _expectedDay);
                validation.Reserve(
                    "civilian_freight.daily_planning." +
                        Invariant(_expectedDay),
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _freight.ProcessDailyPlanning(
                    world,
                    _maximumNewDemands,
                    _maximumNewOffers,
                    _maximumDispatches);
                events.Add(new WorldRuntimeEvent(
                    DailyEventId(_expectedDay),
                    EventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
            }
        }

        private sealed class CivilianFreightPlanningProjectionHandler :
            IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;

            public string EventTypeId =>
                CivilianFreightPlanningCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // The transaction owns all freight planning writes. This
                // consumer establishes the committed projection boundary.
            }
        }
    }
}
