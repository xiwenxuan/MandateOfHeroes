using System;
using System.Collections.Generic;
using System.Linq;
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
        public ulong OriginCellId64;
        public ulong TargetCellId64;
        public string MovementCapabilityId = MovementCapabilityIds.PackAnimal;
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
        public ulong OriginCellId64;
        public ulong TargetCellId64;
        public string MovementCapabilityId = MovementCapabilityIds.PackAnimal;
        public long Quantity;
        public long FreightFee;
    }

    public sealed class MerchantOwnedFreightDispatchRequest
    {
        public string GoalId;
        public string CarrierPersonId;
        public string TransportInventoryContainerId;
        public string RouteId;
        public string CellRouteAssetRouteId;
        public ulong OriginCellId64;
        public ulong TargetCellId64;
        public string MovementCapabilityId = MovementCapabilityIds.PackAnimal;
        public string ProductDefinitionId;
        public string CommodityId;
        public long Quantity;
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
        private readonly CellTraversalPlan _cellTraversalPlan;
        private readonly CellTraversalPlanner _cellTraversalPlanner;
        private readonly IStrategicCellRouteProvider
            _strategicCellRouteProvider;
        private readonly TradingSystem _trading = new TradingSystem();

        public CivilianFreightSystem(
            ulong masterSeed,
            ProductionContentRegistry content,
            CellTraversalPlan cellTraversalPlan = null,
            IStrategicCellRouteProvider strategicCellRouteProvider = null)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _foodInventory = new FoodInventorySystem(content);
            _market = new FormalCountyMarketSystem(content);
            _random = new NamedRandom(masterSeed);
            _cellTraversalPlan = cellTraversalPlan;
            _cellTraversalPlanner = cellTraversalPlan == null
                ? null
                : new CellTraversalPlanner(cellTraversalPlan);
            _strategicCellRouteProvider = strategicCellRouteProvider;
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
            _content.TryGetFood(sell.ProductDefinitionId, out var food);
            var requestedRouteIds = RequestRouteIds(request);
            var routePlan = BuildRoutePlan(
                world,
                requestedRouteIds,
                seller.LocationId,
                buyer.LocationId);
            var route = FindRoute(world, requestedRouteIds[0]);
            var cellRoutePlan = BuildRequestedCellRoute(
                world,
                seller.LocationId,
                buyer.LocationId,
                string.Empty,
                route.Id,
                request.OriginCellId64,
                request.TargetCellId64,
                request.MovementCapabilityId);
            if (cellRoutePlan != null && requestedRouteIds.Count != 1)
                throw new InvalidOperationException(
                    "Cell-routed civilian freight currently requires one continuous formal market route leg.");
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
                PurposeId =
                    CivilianFreightPurposeIds.FormalMarketDelivery,
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
                    food == null ? 0 : food.SpoilageSensitivityBasisPoints,
                CargoUnitWeight = product.BaseWeight,
                CreatedDay = world.AbsoluteDay,
                DispatchedDay = world.AbsoluteDay,
                LastLossDay = world.AbsoluteDay
            };
            world.CivilianFreights.Add(freight);
            world.Journeys.Add(journey);
            if (cellRoutePlan != null)
                BindCellRoute(world, freight, journey, carrier,
                    cellRoutePlan.Route, false, cellRoutePlan.VersionId,
                    cellRoutePlan.AssetHash);
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

        public bool CanBuildStrategicRoute(WorldState world,
            string assetRouteId, string formalWorldRouteId,
            ulong originCellId64, ulong targetCellId64,
            string movementCapabilityId)
        {
            if (world == null || _strategicCellRouteProvider == null ||
                !world.Routes.Any(item => item.Id == formalWorldRouteId))
                return false;
            return _strategicCellRouteProvider.TryBuildRoute(
                assetRouteId,
                formalWorldRouteId,
                originCellId64,
                targetCellId64,
                movementCapabilityId,
                out _,
                out _);
        }

        public CivilianFreightState DispatchMerchantOwnedCargo(
            WorldState world, MerchantOwnedFreightDispatchRequest request)
        {
            if (world == null || request == null)
                throw new ArgumentNullException(
                    world == null ? nameof(world) : nameof(request));
            world.Validate();
            _content.ValidateWorldReferences(world);
            var carrier = ProductInventorySystem.FindPerson(
                world, request.CarrierPersonId);
            var family = ProductInventorySystem.FindFamily(
                world, carrier.FamilyId);
            var container = ProductInventorySystem.FindContainer(
                world, request.TransportInventoryContainerId);
            var route = FindRoute(world, request.RouteId);
            var product = _content.GetProduct(request.ProductDefinitionId);
            _content.TryGetFood(request.ProductDefinitionId, out var food);
            var destinationLocationId = route.FromLocationId ==
                carrier.LocationId
                ? route.ToLocationId
                : route.Bidirectional && route.ToLocationId ==
                    carrier.LocationId
                    ? route.FromLocationId
                    : string.Empty;
            var cellRoutePlan = BuildRequestedCellRoute(
                world,
                carrier.LocationId,
                destinationLocationId,
                request.CellRouteAssetRouteId,
                route.Id,
                request.OriginCellId64,
                request.TargetCellId64,
                request.MovementCapabilityId);
            var sourceBatches = FindMerchantCargoBatches(
                world, family.Id, container.Id, product.Id,
                string.Empty);
            var available = sourceBatches.Sum(item => item.Quantity);
            if (string.IsNullOrWhiteSpace(request.GoalId) ||
                string.IsNullOrEmpty(destinationLocationId) ||
                request.Quantity <= 0 || request.Quantity > available ||
                !carrier.IsAlive || HasJourney(world, carrier.Id) ||
                HasActiveFreight(world, carrier.Id) ||
                container.OwnerFamilyId != family.Id ||
                !string.IsNullOrEmpty(container.OwnerOrganizationId) ||
                container.CarrierPersonId != carrier.Id ||
                container.LocationId != carrier.LocationId ||
                cellRoutePlan == null)
                throw new InvalidOperationException(
                    "The merchant-owned civilian freight request is invalid.");

            var unitCost = FindLastMerchantPurchaseUnitPrice(
                world, carrier.Id, request.CommodityId);
            var freightId = $"civilian_freight.{world.AbsoluteDay}." +
                $"{world.CivilianFreights.Count:D6}";
            var dispatch = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.CivilianFreightDispatched,
                carrier.Id,
                string.Empty,
                0,
                0,
                0,
                "Merchant-owned cargo entered formal civilian freight.");
            dispatch.SourceCivilianFreightId = freightId;
            var createdBatches = ReSourceMerchantCargo(
                world, sourceBatches, dispatch, request.Quantity,
                carrier.LocationId);
            var journey = new JourneyState
            {
                Id = $"journey.{freightId}.leg.0000",
                PersonId = carrier.Id,
                RouteId = route.Id,
                OriginLocationId = carrier.LocationId,
                DestinationLocationId = destinationLocationId,
                Mode = TravelMode.Caravan,
                RemainingKilometers = route.DistanceKilometers,
                StartedDay = world.AbsoluteDay,
                StartedSegment = world.Segment
            };
            var freight = new CivilianFreightState
            {
                Id = freightId,
                PurposeId = CivilianFreightPurposeIds.MerchantOwnerCarriage,
                Status = CivilianFreightStatus.InTransit,
                BuyOrderId = string.Empty,
                SellOrderId = string.Empty,
                FormalMarketTradeId = string.Empty,
                DemandId = string.Empty,
                CarrierOfferId = string.Empty,
                OriginCountyGovernanceId = string.Empty,
                DestinationCountyGovernanceId = string.Empty,
                OriginLocationId = carrier.LocationId,
                DestinationLocationId = destinationLocationId,
                BuyerFamilyId = family.Id,
                BuyerOrganizationId = string.Empty,
                SellerFamilyId = string.Empty,
                BuyerStorageFacilityId = string.Empty,
                DestinationInventoryContainerId = string.Empty,
                SellerStorageFacilityId = string.Empty,
                PublicReliefProcurementTradeId = string.Empty,
                SourcePublicReliefEventId = string.Empty,
                SourcePublicReliefCommandId = string.Empty,
                PublicReliefRecoveryId = string.Empty,
                CarrierPersonId = carrier.Id,
                CarrierFamilyId = family.Id,
                TransportInventoryContainerId = container.Id,
                RouteId = route.Id,
                JourneyId = journey.Id,
                PlannedRouteIds = new List<string> { route.Id },
                CurrentRouteIndex = 0,
                DispatchInventoryTransactionId = dispatch.Id,
                ProductDefinitionId = product.Id,
                DispatchedQuantity = request.Quantity,
                RemainingCargoQuantity = request.Quantity,
                GoodsUnitPrice = unitCost,
                GoodsMoneyTransferred = checked(request.Quantity * unitCost),
                FreightFee = 0,
                FreightFeeEscrow = 0,
                FreightFeePaid = 0,
                ProductPerishabilityBasisPoints =
                    product.PerishabilityBasisPoints,
                FoodSpoilageSensitivityBasisPoints =
                    food == null ? 0 : food.SpoilageSensitivityBasisPoints,
                CargoUnitWeight = product.BaseWeight,
                CreatedDay = world.AbsoluteDay,
                DispatchedDay = world.AbsoluteDay,
                LastLossDay = world.AbsoluteDay
            };
            world.InventoryTransactions.Add(dispatch);
            world.ProductBatches.AddRange(createdBatches);
            world.CivilianFreights.Add(freight);
            world.Journeys.Add(journey);
            BindCellRoute(world, freight, journey, carrier,
                cellRoutePlan.Route, false, cellRoutePlan.VersionId,
                cellRoutePlan.AssetHash);
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.Dispatched,
                carrier.Id,
                dispatch.Id,
                request.Quantity,
                freight.GoodsMoneyTransferred,
                "Merchant-owned cargo dispatched with purchase cost basis.");
            world.Validate();
            _content.ValidateWorldReferences(world);
            return freight;
        }

        public bool RecordMerchantOwnedCargoLoss(WorldState world,
            string carrierPersonId, string commodityId, long quantity)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var freight = FindActiveMerchantOwnedFreight(
                world, carrierPersonId);
            if (freight == null || quantity <= 0 ||
                quantity > freight.RemainingCargoQuantity)
                return false;
            if (!_trading.LoseMerchantFreightCargo(
                    world, freight, commodityId, checked((int)quantity),
                    out var transaction))
                return false;
            freight.RemainingCargoQuantity -= quantity;
            freight.NaturalLossQuantity += quantity;
            freight.LastLossDay = world.AbsoluteDay;
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.NaturalLoss,
                carrierPersonId,
                transaction.Id,
                quantity,
                0,
                "Player travel event caused formal freight cargo loss.");
            world.Validate();
            return true;
        }

        public TradeResult SettleMerchantOwnedCargoSale(WorldState world,
            string carrierPersonId, string commodityId, long quantity)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var freight = FindActiveMerchantOwnedFreight(
                world, carrierPersonId);
            if (freight == null ||
                freight.Status != CivilianFreightStatus.AwaitingReceipt ||
                quantity <= 0 || quantity > freight.RemainingCargoQuantity)
                return new TradeResult(
                    false, 0, 0,
                    "正式商旅尚未到达，或交付数量不合法。");
            var result = _trading.SellMerchantFreightCargo(
                world,
                freight,
                commodityId,
                checked((int)quantity),
                out var transaction);
            if (!result.Success) return result;
            freight.RemainingCargoQuantity -= quantity;
            freight.DeliveredQuantity += quantity;
            AddLedger(
                world,
                freight,
                CivilianFreightLedgerType.Delivered,
                carrierPersonId,
                transaction.Id,
                quantity,
                0,
                "Merchant-owned freight sold into the destination market.");
            if (freight.RemainingCargoQuantity == 0)
                Complete(world, freight);
            world.Validate();
            return result;
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
            var cellRoutePlan = BuildRequestedCellRoute(
                world,
                seller.LocationId,
                destination.CountyLocationId,
                string.Empty,
                request.RouteIds[0],
                request.OriginCellId64,
                request.TargetCellId64,
                request.MovementCapabilityId);
            if (cellRoutePlan != null && request.RouteIds.Count != 1)
                throw new InvalidOperationException(
                    "Cell-routed public relief freight currently requires one continuous formal market route leg.");
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
                PurposeId =
                    CivilianFreightPurposeIds.PublicReliefProcurement,
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
            if (cellRoutePlan != null)
                BindCellRoute(world, freight, journey, carrier,
                    cellRoutePlan.Route, false, cellRoutePlan.VersionId,
                    cellRoutePlan.AssetHash);
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
                if (freight.Status == CivilianFreightStatus.InTransit &&
                    freight.UsesCellRoute && freight.CellRouteWaiting)
                    TryRerouteCellFreight(world, freight);
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
                if (freight.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage)
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

        public bool TryRerouteCellFreight(WorldState world,
            CivilianFreightState freight)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (freight == null) throw new ArgumentNullException(nameof(freight));
            if (_cellTraversalPlanner == null || !freight.UsesCellRoute ||
                !freight.CellRouteWaiting ||
                freight.Status != CivilianFreightStatus.InTransit)
                return false;
            if (LuoyangCellTraversalRules.CanTraverseCondition(
                    world,
                    freight.CellRouteSegments[
                        freight.CurrentCellRouteSegmentIndex]
                        .TraversalConditionId,
                    freight.CellRouteWaitingOnFormalWorldObjectId))
                return false;
            if (!_cellTraversalPlanner.TryFindRoute(
                    freight.CellRouteCurrentCellId64,
                    freight.CellRouteTargetCellId64,
                    freight.CellRouteMovementCapabilityId,
                    port => LuoyangCellTraversalRules.IsPortAvailable(
                        world, port),
                    out var route,
                    out _)) return false;
            var journey = FindJourney(world, freight.JourneyId);
            var carrier = ProductInventorySystem.FindPerson(
                world, freight.CarrierPersonId);
            BindCellRoute(world, freight, journey, carrier, route, true);
            return true;
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

        private static bool HasActiveFreight(
            WorldState world, string carrierPersonId) =>
            FindActiveMerchantOwnedFreight(world, carrierPersonId) != null ||
            world.CivilianFreights.Any(item =>
                item.CarrierPersonId == carrierPersonId &&
                item.Status != CivilianFreightStatus.Completed);

        private static CivilianFreightState FindActiveMerchantOwnedFreight(
            WorldState world, string carrierPersonId)
        {
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                if (freight.PurposeId ==
                        CivilianFreightPurposeIds.MerchantOwnerCarriage &&
                    freight.CarrierPersonId == carrierPersonId &&
                    freight.Status != CivilianFreightStatus.Completed)
                    return freight;
            }
            return null;
        }

        private static List<ProductBatchState> FindMerchantCargoBatches(
            WorldState world, string familyId, string containerId,
            string productDefinitionId, string sourceTransactionId)
        {
            var result = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == familyId &&
                    string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
                    batch.InventoryContainerId == containerId &&
                    batch.ProductDefinitionId == productDefinitionId &&
                    batch.Quantity > 0 &&
                    (string.IsNullOrEmpty(sourceTransactionId) ||
                     batch.SourceTransactionId == sourceTransactionId))
                    result.Add(batch);
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

        private static int FindLastMerchantPurchaseUnitPrice(
            WorldState world, string personId, string commodityId)
        {
            for (var i = world.TradeRecords.Count - 1; i >= 0; i--)
            {
                var trade = world.TradeRecords[i];
                if (trade.PersonId == personId &&
                    trade.CommodityId == commodityId &&
                    trade.IsPurchase && trade.UnitPrice > 0)
                    return trade.UnitPrice;
            }
            throw new InvalidOperationException(
                "Merchant freight lacks a formal purchase cost basis.");
        }

        private static List<ProductBatchState> ReSourceMerchantCargo(
            WorldState world, IList<ProductBatchState> sources,
            InventoryTransactionState transaction, long requestedQuantity,
            string originLocationId)
        {
            var created = new List<ProductBatchState>();
            var remaining = requestedQuantity;
            for (var i = 0; i < sources.Count && remaining > 0; i++)
            {
                var source = sources[i];
                var moved = Math.Min(source.Quantity, remaining);
                source.Quantity -= moved;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    source, -moved, 0));
                var target = new ProductBatchState
                {
                    Id = $"product_batch.{world.AbsoluteDay}." +
                        $"{world.ProductBatches.Count + created.Count:D6}",
                    ProductDefinitionId = source.ProductDefinitionId,
                    OwnerFamilyId = source.OwnerFamilyId,
                    OwnerOrganizationId = string.Empty,
                    StorageFacilityId = string.Empty,
                    InventoryContainerId = source.InventoryContainerId,
                    OriginLocationId = originLocationId,
                    SourceWorkOrderId = string.Empty,
                    SourceTransactionId = transaction.Id,
                    CropVarietyDefinitionId =
                        source.CropVarietyDefinitionId,
                    UnitId = source.UnitId,
                    UnitWeight = source.UnitWeight,
                    ProducedDay = world.AbsoluteDay,
                    Quantity = moved,
                    ReservedQuantity = 0,
                    QualityBasisPoints = source.QualityBasisPoints,
                    FreshnessBasisPoints = source.FreshnessBasisPoints,
                    SeedVigorBasisPoints = source.SeedVigorBasisPoints,
                    SeedPurityBasisPoints = source.SeedPurityBasisPoints,
                    NextFoodStorageAssessmentDay =
                        source.NextFoodStorageAssessmentDay,
                    QualityDimensions = source.QualityDimensions.Select(
                        item => new ProductQualityDimensionState
                        {
                            QualityDimensionId = item.QualityDimensionId,
                            ValueBasisPoints = item.ValueBasisPoints
                        }).ToList()
                };
                transaction.Lines.Add(ProductInventorySystem.Line(
                    target, moved, 0));
                created.Add(target);
                remaining -= moved;
            }
            if (remaining != 0)
                throw new InvalidOperationException(
                    "Merchant cargo changed during formal dispatch.");
            return created;
        }

        private static JourneyState FindJourney(
            WorldState world, string journeyId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (string.Equals(world.Journeys[i].Id, journeyId,
                        StringComparison.Ordinal))
                    return world.Journeys[i];
            }
            throw new InvalidOperationException(
                "Missing civilian freight Journey " + journeyId + ".");
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

        private StrategicCellRoutePlan BuildRequestedCellRoute(
            WorldState world,
            string originLocationId,
            string targetLocationId,
            string assetRouteId,
            string formalWorldRouteId,
            ulong requestedOriginCellId64,
            ulong requestedTargetCellId64,
            string movementCapabilityId)
        {
            var originCellId64 = requestedOriginCellId64;
            var targetCellId64 = requestedTargetCellId64;
            if (originCellId64 == 0)
                TryParseCellLocation(originLocationId, out originCellId64);
            if (targetCellId64 == 0)
                TryParseCellLocation(targetLocationId, out targetCellId64);
            if (originCellId64 == 0 && targetCellId64 == 0) return null;
            if (originCellId64 == 0 || targetCellId64 == 0)
                throw new InvalidOperationException(
                    "Cell-routed freight requires two formal Cell IDs.");
            var capabilityId = string.IsNullOrWhiteSpace(movementCapabilityId)
                ? MovementCapabilityIds.PackAnimal
                : movementCapabilityId;
            if (!MovementCapabilityIds.All.Contains(capabilityId))
                throw new InvalidOperationException(
                    "Civilian freight uses an unsupported movement capability.");
            if (!string.IsNullOrWhiteSpace(assetRouteId))
            {
                if (_strategicCellRouteProvider == null)
                    throw new InvalidOperationException(
                        "Strategic CellRoute assets are unavailable.");
                if (!_strategicCellRouteProvider.TryBuildRoute(
                        assetRouteId,
                        formalWorldRouteId,
                        originCellId64,
                        targetCellId64,
                        capabilityId,
                        out var strategicPlan,
                        out var strategicFailureReasonId))
                    throw new InvalidOperationException(
                        "Strategic CellRoute planning failed: " +
                        strategicFailureReasonId + ".");
                return strategicPlan;
            }
            if (_cellTraversalPlanner == null)
                throw new InvalidOperationException(
                    "Cell-routed freight requires one formal Cell traversal plan.");
            if (!_cellTraversalPlanner.TryFindRoute(
                    originCellId64,
                    targetCellId64,
                    capabilityId,
                    port => LuoyangCellTraversalRules.IsPortAvailable(
                        world, port),
                    out var route,
                    out var failureReasonId))
                throw new InvalidOperationException(
                    "Civilian freight CellRoute planning failed: " +
                    failureReasonId + ".");
            return new StrategicCellRoutePlan(
                _cellTraversalPlan.VersionId,
                _cellTraversalPlan.AssetHash,
                _cellTraversalPlan.VersionId,
                formalWorldRouteId,
                route);
        }

        private void BindCellRoute(WorldState world,
            CivilianFreightState freight,
            JourneyState journey,
            PersonState carrier,
            CellRoute route,
            bool reroute,
            string planVersionId = null,
            string assetHash = null)
        {
            if (route == null || route.Segments.Count == 0)
                throw new InvalidOperationException(
                    "Civilian freight cannot bind an empty CellRoute.");
            var segments = new List<CivilianFreightCellRouteSegmentState>(
                route.Segments.Count);
            long remaining = 0;
            for (var i = 0; i < route.Segments.Count; i++)
            {
                var source = route.Segments[i];
                var segment = new CivilianFreightCellRouteSegmentState
                {
                    Sequence = i,
                    Id = source.Id,
                    KindId = source.KindId,
                    FromCellId64 = source.FromCellId64,
                    ToCellId64 = source.ToCellId64,
                    DistanceCentimetres = source.DistanceCentimetres,
                    TraversalCostPermille = source.TraversalCostPermille,
                    TraversalConditionId = source.TraversalConditionId,
                    FormalWorldObjectId = source.FormalWorldObjectId
                };
                segments.Add(segment);
                remaining = checked(
                    remaining + segment.WeightedDistanceCentimetres);
            }
            freight.UsesCellRoute = true;
            freight.CellRoutePlanVersionId = planVersionId ??
                _cellTraversalPlan?.VersionId ?? throw new
                    InvalidOperationException(
                        "CellRoute plan version metadata is unavailable.");
            freight.CellRouteAssetHash = assetHash ??
                _cellTraversalPlan?.AssetHash ?? throw new
                    InvalidOperationException(
                        "CellRoute asset hash metadata is unavailable.");
            freight.CellRouteMovementCapabilityId =
                route.MovementCapabilityId;
            if (!reroute)
                freight.CellRouteOriginCellId64 = route.OriginCellId64;
            freight.CellRouteTargetCellId64 = route.TargetCellId64;
            freight.CellRouteCurrentCellId64 = route.OriginCellId64;
            freight.CellRouteSegments = segments;
            freight.CurrentCellRouteSegmentIndex = 0;
            freight.CurrentCellRouteSegmentRemainingWeightedCentimetres =
                segments[0].WeightedDistanceCentimetres;
            freight.CellRouteRemainingWeightedCentimetres = remaining;
            freight.CellRouteWaiting = false;
            freight.CellRouteWaitingReasonId = string.Empty;
            freight.CellRouteWaitingOnFormalWorldObjectId = string.Empty;
            if (reroute) freight.CellRouteRevision++;
            journey.RemainingKilometers = checked((int)Math.Max(
                1L, (remaining + 99_999L) / 100_000L));
            carrier.CurrentCellId64 = route.OriginCellId64;
        }

        private static bool TryParseCellLocation(
            string locationId, out ulong cellId64)
        {
            const string prefix = "cell.id64.";
            cellId64 = 0;
            return !string.IsNullOrEmpty(locationId) &&
                locationId.StartsWith(prefix, StringComparison.Ordinal) &&
                ulong.TryParse(locationId.Substring(prefix.Length),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out cellId64) && cellId64 != 0;
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

    public sealed class LuoyangSupplyCatchmentSelection
    {
        public const string V1Id = "luoyang.supply-catchment.v1";
        public const int InclusivePopulationTarget = 700_000;

        public string Id = V1Id;
        public List<ulong> CellIds = new List<ulong>();
        public List<string> SupplyLocationIds = new List<string>();
        public List<string> CityLocationIds = new List<string>();
        public List<string> SettlementIds = new List<string>();
        public List<string> FacilityIds = new List<string>();

        public void Normalize()
        {
            CellIds = CellIds.Distinct().OrderBy(item => item).ToList();
            SupplyLocationIds = SortedUnique(SupplyLocationIds);
            CityLocationIds = SortedUnique(CityLocationIds);
            SettlementIds = SortedUnique(SettlementIds);
            FacilityIds = SortedUnique(FacilityIds);
        }

        private static List<string> SortedUnique(IEnumerable<string> values) =>
            (values ?? Array.Empty<string>()).Where(item =>
                    !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToList();
    }

    public sealed class LuoyangSupplyCatchmentAudit
    {
        public int CellCount;
        public int SettlementCount;
        public int FacilityCount;
        public int PermanentPersonCount;
        public int HouseholdCount;
        public int ProductionOrderCount;
        public int StorageFacilityCount;
        public int TraversalCoveredCellCount;
        public List<string> CriticalReferenceErrors = new List<string>();
        public bool Passed => CriticalReferenceErrors.Count == 0;
    }

    public sealed class LuoyangCitySupplyProjection
    {
        public long ProjectionDay;
        public long SourceWorldRevision;
        public long CurrentUsableFoodStock;
        public long CurrentUsableNutritionBasisUnits;
        public long DailyFoodDemandNutritionBasisUnits;
        public double DaysOfSupply;
        public long PrivateFoodStock;
        public long HouseholdStoredFood;
        public long GovernmentFoodStock;
        public long OtherCivilianFoodStock;
        public long IncomingFreightQuantity;
        public long BlockedFreightQuantity;
        public long DelayedFreightQuantity;
        public int DelayedFreightCount;
        public int BlockedFreightCount;
        public int HouseholdShortfallCount;
        public int FoodShortfallPersonCount;
        public long PublicGranaryStock;
        public long MarketAvailableSupply;
        public long PrivateMarketAvailableFood;
        public int ActiveProcurementCount;
        public int ActiveCarrierCount;
        public int ActiveFreightCount;
        public int SupplySourceCount;
        public int MainFoodPriceIndexBasisPoints;
        public List<LuoyangFoodPriceProjection> ProductPrices =
            new List<LuoyangFoodPriceProjection>();
    }

    public sealed class LuoyangFoodPriceProjection
    {
        public string ProductDefinitionId;
        public long EquilibriumUnitPrice;
        public long LastTradeUnitPrice;
        public long LastTradeDay;
        public long ActiveBuyDemand;
        public long ActiveSellSupply;
        public long RecentTradeQuantity;
        public int HouseholdShortfallCount;
        public int BlockedInboundFreightCount;
        public List<string> ExplanationFactorIds = new List<string>();
    }

    public sealed class LuoyangPlayerSupplyInterventionResult
    {
        public string PlayerPersonId;
        public string PlayerFamilyId;
        public string CivilianFreightId;
        public string ProductDefinitionId;
        public long Quantity;
        public long UnitPrice;
        public long FreightFee;
        public long DispatchDay;
    }

    /// <summary>
    /// Thin player-action adapter over the formal market freight service. It
    /// owns no inventory, price, route, command or receipt state.
    /// </summary>
    public sealed class LuoyangPlayerSupplyInterventionService
    {
        public LuoyangPlayerSupplyInterventionResult DispatchMarketFreight(
            WorldState world,
            CivilianFreightSystem freightSystem,
            CivilianFreightDispatchRequest request)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (freightSystem == null) throw new ArgumentNullException(
                nameof(freightSystem));
            if (request == null) throw new ArgumentNullException(
                nameof(request));
            if (string.IsNullOrEmpty(world.PlayerPersonId) ||
                request.CarrierPersonId != world.PlayerPersonId)
                throw new InvalidOperationException(
                    "The formal supply intervention carrier must be the current player Person.");
            var player = world.People.Find(item => item != null &&
                item.Id == world.PlayerPersonId) ??
                throw new InvalidOperationException(
                    "The current player Person is unavailable.");
            var freight = freightSystem.Dispatch(world, request);
            return new LuoyangPlayerSupplyInterventionResult
            {
                PlayerPersonId = player.Id,
                PlayerFamilyId = player.FamilyId,
                CivilianFreightId = freight.Id,
                ProductDefinitionId = freight.ProductDefinitionId,
                Quantity = freight.DispatchedQuantity,
                UnitPrice = freight.GoodsUnitPrice,
                FreightFee = freight.FreightFee,
                DispatchDay = freight.DispatchedDay
            };
        }
    }

    public sealed class LuoyangSupplyProjectionSystem
    {
        private const int DaysPerYear = 360;
        private const int DaysPerHouseholdSettlement = 30;
        private readonly ProductionContentRegistry _content;
        private readonly IPersonRepository _people;
        private IPersonRepository _fallbackPeople;
        private WorldState _fallbackPeopleWorld;

        public LuoyangSupplyProjectionSystem(
            ProductionContentRegistry content,
            IPersonRepository people = null)
        {
            _content = content ?? throw new ArgumentNullException(
                nameof(content));
            _people = people;
        }

        public LuoyangSupplyCatchmentAudit AuditCatchment(
            WorldState world,
            LuoyangSupplyCatchmentSelection selection,
            CellTraversalPlan traversalPlan)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (selection == null) throw new ArgumentNullException(
                nameof(selection));
            selection.Normalize();
            var result = new LuoyangSupplyCatchmentAudit
            {
                CellCount = selection.CellIds.Count,
                SettlementCount = selection.SettlementIds.Count,
                FacilityCount = selection.FacilityIds.Count
            };
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            for (var i = 0; i < selection.CellIds.Count; i++)
            {
                if (!grid.TryDecode(new WorldMapCellId(
                        selection.CellIds[i]), out _, out _))
                    result.CriticalReferenceErrors.Add(
                        "invalid-cell:" + selection.CellIds[i]);
                if (traversalPlan != null &&
                    traversalPlan.ProfilesByCellId.ContainsKey(
                        selection.CellIds[i]))
                    result.TraversalCoveredCellCount++;
                else
                    result.CriticalReferenceErrors.Add(
                        "missing-traversal:" + selection.CellIds[i]);
            }
            for (var i = 0; i < selection.SettlementIds.Count; i++)
                if (!world.Villages.Any(item => item != null &&
                        item.Id == selection.SettlementIds[i]))
                    result.CriticalReferenceErrors.Add(
                        "missing-settlement:" + selection.SettlementIds[i]);
            for (var i = 0; i < selection.FacilityIds.Count; i++)
            {
                var id = selection.FacilityIds[i];
                if (!world.Facilities.Any(item => item != null &&
                        item.Id == id) &&
                    !world.VillageFacilities.Any(item => item != null &&
                        item.Id == id))
                    result.CriticalReferenceErrors.Add(
                        "missing-facility:" + id);
            }
            var locations = new HashSet<string>(
                selection.SupplyLocationIds, StringComparer.Ordinal);
            result.PermanentPersonCount = world.People.Count(item =>
                item != null && item.CountsTowardPopulation &&
                locations.Contains(item.LocationId));
            result.HouseholdCount = world.Families.Count(item =>
                item != null && locations.Contains(item.LocationId));
            result.ProductionOrderCount = world.AgricultureWorkOrders.Count(
                item => item != null && world.Villages.Any(village =>
                    village.Id == item.VillageId &&
                    locations.Contains(village.LocationId))) +
                world.ResourceExtractionOrders.Count(item => item != null &&
                    locations.Contains(ResourceOrderLocation(world, item)));
            result.StorageFacilityCount = world.VillageFacilities.Count(item =>
                item != null && item.Kind ==
                    VillageFacilityKind.HouseholdGranary &&
                world.Villages.Any(village => village.Id == item.VillageId &&
                    locations.Contains(village.LocationId)));
            return result;
        }

        public LuoyangCitySupplyProjection BuildCityProjection(
            WorldState world,
            LuoyangSupplyCatchmentSelection selection)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (selection == null) throw new ArgumentNullException(
                nameof(selection));
            selection.Normalize();
            var cityLocations = new HashSet<string>(
                selection.CityLocationIds, StringComparer.Ordinal);
            var cityFamilies = world.Families.Where(item => item != null &&
                    cityLocations.Contains(item.LocationId))
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
            var cityFamilyIds = new HashSet<string>(
                cityFamilies.Select(item => item.Id), StringComparer.Ordinal);
            var result = new LuoyangCitySupplyProjection
            {
                ProjectionDay = world.AbsoluteDay,
                SourceWorldRevision = world.Revision
            };
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch == null || batch.Quantity <= 0 ||
                    batch.QualityBasisPoints <= 0 ||
                    !_content.TryGetFood(
                        batch.ProductDefinitionId, out var food) ||
                    !cityLocations.Contains(BatchLocation(world, batch)) ||
                    IsMilitaryOwned(world, batch)) continue;
                var usable = Math.Max(0L,
                    batch.Quantity - batch.ReservedQuantity);
                result.CurrentUsableFoodStock = checked(
                    result.CurrentUsableFoodStock + usable);
                result.CurrentUsableNutritionBasisUnits = checked(
                    result.CurrentUsableNutritionBasisUnits +
                    usable * food.NutritionBasisPoints);
                if (IsGovernmentOwned(world, batch))
                {
                    result.PublicGranaryStock = checked(
                        result.PublicGranaryStock + usable);
                    result.GovernmentFoodStock = checked(
                        result.GovernmentFoodStock + usable);
                }
                else if (!string.IsNullOrEmpty(batch.OwnerFamilyId))
                {
                    result.PrivateFoodStock = checked(
                        result.PrivateFoodStock + usable);
                    result.HouseholdStoredFood = checked(
                        result.HouseholdStoredFood + usable);
                }
                else
                {
                    result.OtherCivilianFoodStock = checked(
                        result.OtherCivilianFoodStock + usable);
                }
            }
            long monthlyDemand = 0;
            var people = PeopleFor(world);
            for (var familyIndex = 0;
                 familyIndex < cityFamilies.Length;
                 familyIndex++)
            {
                var family = cityFamilies[familyIndex];
                var shortfall = family.FoodSecurityBasisPoints < 10_000;
                if (shortfall) result.HouseholdShortfallCount++;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (!person.IsAlive) continue;
                    var age = Math.Max(
                        0L, (world.AbsoluteDay - person.BirthDay) /
                            DaysPerYear);
                    monthlyDemand = checked(monthlyDemand +
                        (age < 15 || age > 60 ? 2L : 3L) * 10_000L);
                    if (shortfall) result.FoodShortfallPersonCount++;
                }
            }
            result.DailyFoodDemandNutritionBasisUnits =
                (monthlyDemand + DaysPerHouseholdSettlement - 1) /
                DaysPerHouseholdSettlement;
            result.DaysOfSupply = result.DailyFoodDemandNutritionBasisUnits == 0
                ? 0d
                : (double)result.CurrentUsableNutritionBasisUnits /
                    result.DailyFoodDemandNutritionBasisUnits;
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                if (freight == null || freight.Status ==
                        CivilianFreightStatus.Completed ||
                    !cityLocations.Contains(freight.DestinationLocationId) ||
                    !_content.TryGetFood(
                        freight.ProductDefinitionId, out _)) continue;
                result.IncomingFreightQuantity = checked(
                    result.IncomingFreightQuantity +
                    freight.RemainingCargoQuantity);
                result.ActiveFreightCount++;
                if (freight.CellRouteWaiting)
                {
                    result.DelayedFreightCount++;
                    result.BlockedFreightCount++;
                    result.DelayedFreightQuantity = checked(
                        result.DelayedFreightQuantity +
                        freight.RemainingCargoQuantity);
                    result.BlockedFreightQuantity = checked(
                        result.BlockedFreightQuantity +
                        freight.RemainingCargoQuantity);
                }
                else if (freight.Status ==
                    CivilianFreightStatus.AwaitingReceipt)
                {
                    result.DelayedFreightCount++;
                    result.DelayedFreightQuantity = checked(
                        result.DelayedFreightQuantity +
                        freight.RemainingCargoQuantity);
                }
            }
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                var order = world.FormalMarketOrders[i];
                if (order == null || order.Side != FormalMarketOrderSide.Sell ||
                    order.Status != FormalMarketOrderStatus.Active ||
                    !_content.TryGetFood(order.ProductDefinitionId, out _) ||
                    !cityLocations.Contains(FamilyLocation(
                        world, order.OwnerFamilyId))) continue;
                result.MarketAvailableSupply = checked(
                    result.MarketAvailableSupply + order.RemainingQuantity);
            }
            result.PrivateMarketAvailableFood =
                result.MarketAvailableSupply;
            result.ActiveCarrierCount = world.CivilianCarrierRegistrations
                .Count(item => item != null && item.Active);
            result.ActiveProcurementCount =
                CountActiveProcurement(world);
            result.SupplySourceCount = CountSupplySources(
                world, selection);
            BuildPriceProjection(
                world, cityLocations, cityFamilyIds, result);
            return result;
        }

        private void BuildPriceProjection(
            WorldState world,
            HashSet<string> cityLocations,
            HashSet<string> cityFamilyIds,
            LuoyangCitySupplyProjection result)
        {
            var governanceIds = new HashSet<string>(
                world.CountyGovernances.Where(governance =>
                        cityLocations.Contains(governance.CountyLocationId) ||
                        world.Locations.Any(location =>
                            cityLocations.Contains(location.Id) &&
                            location.ParentLocationId ==
                                governance.CountyLocationId))
                    .Select(governance => governance.Id),
                StringComparer.Ordinal);
            long indexTotal = 0;
            var indexCount = 0;
            var prices = world.FormalMarketPrices.Where(price =>
                    price != null && governanceIds.Contains(
                        price.CountyGovernanceId) &&
                    _content.TryGetFood(
                        price.ProductDefinitionId, out _))
                .OrderBy(price => price.ProductDefinitionId,
                    StringComparer.Ordinal)
                .ThenBy(price => price.CountyGovernanceId,
                    StringComparer.Ordinal);
            foreach (var price in prices)
            {
                var item = new LuoyangFoodPriceProjection
                {
                    ProductDefinitionId = price.ProductDefinitionId,
                    EquilibriumUnitPrice = price.EquilibriumUnitPrice,
                    LastTradeUnitPrice = price.LastTradeUnitPrice,
                    LastTradeDay = price.LastTradeDay,
                    HouseholdShortfallCount =
                        result.HouseholdShortfallCount,
                    BlockedInboundFreightCount =
                        result.BlockedFreightCount
                };
                for (var orderIndex = 0;
                     orderIndex < world.FormalMarketOrders.Count;
                     orderIndex++)
                {
                    var order = world.FormalMarketOrders[orderIndex];
                    if (order == null || order.Status !=
                            FormalMarketOrderStatus.Active ||
                        order.ProductDefinitionId !=
                            price.ProductDefinitionId ||
                        (!governanceIds.Contains(order.CountyGovernanceId) &&
                         !cityFamilyIds.Contains(order.OwnerFamilyId)))
                        continue;
                    if (order.Side == FormalMarketOrderSide.Buy)
                        item.ActiveBuyDemand = checked(
                            item.ActiveBuyDemand + order.RemainingQuantity);
                    else
                        item.ActiveSellSupply = checked(
                            item.ActiveSellSupply + order.RemainingQuantity);
                }
                for (var tradeIndex = 0;
                     tradeIndex < world.FormalMarketTrades.Count;
                     tradeIndex++)
                {
                    var trade = world.FormalMarketTrades[tradeIndex];
                    if (trade != null && trade.ProductDefinitionId ==
                            price.ProductDefinitionId &&
                        governanceIds.Contains(trade.CountyGovernanceId) &&
                        trade.Day >= world.AbsoluteDay - 30)
                        item.RecentTradeQuantity = checked(
                            item.RecentTradeQuantity + trade.Quantity);
                }
                item.ExplanationFactorIds.Add(
                    "formal.available-stock");
                item.ExplanationFactorIds.Add(
                    "formal.active-order-demand");
                item.ExplanationFactorIds.Add(
                    "formal.recent-trades");
                if (item.HouseholdShortfallCount > 0)
                    item.ExplanationFactorIds.Add(
                        "formal.household-shortfall");
                if (item.BlockedInboundFreightCount > 0)
                    item.ExplanationFactorIds.Add(
                        "formal.transport-disruption");
                result.ProductPrices.Add(item);
                if (price.EquilibriumUnitPrice > 0 &&
                    price.LastTradeUnitPrice >= 0)
                {
                    indexTotal = checked(indexTotal + Math.Min(
                        100_000L,
                        price.LastTradeUnitPrice * 10_000L /
                        price.EquilibriumUnitPrice));
                    indexCount++;
                }
            }
            result.MainFoodPriceIndexBasisPoints = indexCount == 0
                ? 0
                : checked((int)(indexTotal / indexCount));
        }

        private static int CountActiveProcurement(WorldState world)
        {
            var pendingCommands = world.PersistentWorldCommands.Count(
                command => command != null && command.Status ==
                    PersistentWorldCommandStatus.Pending &&
                    (command.CommandTypeId ==
                         PublicReliefProcurementContractIds.CommandTypeId ||
                     command.CommandTypeId ==
                         PublicReliefProcurementContractIds
                             .ExternalProcurementCommandTypeId));
            return checked(pendingCommands +
                world.PublicReliefRecoveries.Count(recovery =>
                    recovery != null &&
                    (recovery.Status ==
                         PublicReliefRecoveryStatus.SupplementalInTransit ||
                     recovery.Status ==
                         PublicReliefRecoveryStatus.DistributionBlocked)));
        }

        private int CountSupplySources(
            WorldState world,
            LuoyangSupplyCatchmentSelection selection)
        {
            var supplyLocations = new HashSet<string>(
                selection.SupplyLocationIds, StringComparer.Ordinal);
            var sources = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch == null || batch.Quantity <= 0 ||
                    !_content.TryGetFood(
                        batch.ProductDefinitionId, out _)) continue;
                var location = BatchLocation(world, batch);
                if (supplyLocations.Contains(location)) sources.Add(location);
            }
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                if (freight != null && freight.Status !=
                        CivilianFreightStatus.Completed &&
                    _content.TryGetFood(
                        freight.ProductDefinitionId, out _))
                    sources.Add(freight.OriginLocationId);
            }
            return sources.Count;
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            if (_people != null) return _people;
            if (!ReferenceEquals(_fallbackPeopleWorld, world))
            {
                _fallbackPeopleWorld = world;
                _fallbackPeople = new WorldStatePersonRepository(world);
            }
            return _fallbackPeople;
        }

        private static string ResourceOrderLocation(
            WorldState world, ResourceExtractionOrderState order)
        {
            if (!string.IsNullOrEmpty(order.StorageFacilityId))
            {
                var storage = world.VillageFacilities.FirstOrDefault(item =>
                    item.Id == order.StorageFacilityId);
                var village = storage == null ? null :
                    world.Villages.FirstOrDefault(item =>
                        item.Id == storage.VillageId);
                return village?.LocationId ?? string.Empty;
            }
            var site = world.ProductionSites.FirstOrDefault(item =>
                item.Id == order.ProductionSiteId);
            return site?.LocationId ?? string.Empty;
        }

        private static string BatchLocation(
            WorldState world, ProductBatchState batch)
        {
            if (!string.IsNullOrEmpty(batch.StorageFacilityId))
            {
                var storage = world.VillageFacilities.FirstOrDefault(item =>
                    item.Id == batch.StorageFacilityId);
                var village = storage == null ? null :
                    world.Villages.FirstOrDefault(item =>
                        item.Id == storage.VillageId);
                if (village != null) return village.LocationId;
            }
            var container = world.InventoryContainers.FirstOrDefault(item =>
                item.Id == batch.InventoryContainerId);
            return container?.LocationId ?? string.Empty;
        }

        private static bool IsMilitaryOwned(
            WorldState world, ProductBatchState batch) =>
            !string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
            world.Organizations.Any(item => item.Id ==
                batch.OwnerOrganizationId && item.Type ==
                OrganizationType.Military);

        private static bool IsGovernmentOwned(
            WorldState world, ProductBatchState batch) =>
            !string.IsNullOrEmpty(batch.OwnerOrganizationId) &&
            world.Organizations.Any(item => item.Id ==
                batch.OwnerOrganizationId && item.Type ==
                OrganizationType.Government);

        private static string FamilyLocation(
            WorldState world, string familyId) =>
            world.Families.FirstOrDefault(item => item.Id == familyId)
                ?.LocationId ?? string.Empty;
    }
}
