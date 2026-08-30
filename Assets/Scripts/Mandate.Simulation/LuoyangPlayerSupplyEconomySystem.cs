using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LuoyangSupplyRouteDefinition
    {
        public LuoyangSupplyRouteDefinition(string routeId,
            ulong originCellId64, ulong targetCellId64,
            string movementCapabilityId)
        {
            if (string.IsNullOrWhiteSpace(routeId))
                throw new ArgumentException("A route ID is required.",
                    nameof(routeId));
            RouteId = routeId;
            OriginCellId64 = originCellId64;
            TargetCellId64 = targetCellId64;
            MovementCapabilityId = movementCapabilityId ??
                MovementCapabilityIds.Cart;
        }

        public string RouteId { get; }
        public ulong OriginCellId64 { get; }
        public ulong TargetCellId64 { get; }
        public string MovementCapabilityId { get; }
    }

    public sealed class LuoyangSupplyRouteAssessment
    {
        public string RouteId;
        public bool CanTraverse;
        public string FailureReasonId;
        public string BlockingFormalObjectId;
        public string PhysicalRouteSignature;
        public int SegmentCount;
        public long WeightedDistanceCentimetres;
    }

    public interface ILuoyangSupplyRouteAccess
    {
        LuoyangSupplyRouteAssessment Assess(string routeId);
    }

    public sealed class LuoyangOpenSupplyRouteAccess :
        ILuoyangSupplyRouteAccess
    {
        public static readonly LuoyangOpenSupplyRouteAccess Instance =
            new LuoyangOpenSupplyRouteAccess();

        private LuoyangOpenSupplyRouteAccess()
        {
        }

        public LuoyangSupplyRouteAssessment Assess(string routeId) =>
            new LuoyangSupplyRouteAssessment
            {
                RouteId = routeId ?? string.Empty,
                CanTraverse = !string.IsNullOrWhiteSpace(routeId),
                FailureReasonId = string.IsNullOrWhiteSpace(routeId)
                    ? "supply.route.missing.v1"
                    : string.Empty,
                PhysicalRouteSignature = routeId ?? string.Empty
            };
    }

    /// <summary>
    /// Read-only adapter from logical supply routes to the formal CellTraversal
    /// planner. Gate, bridge and road availability is always read from the
    /// supplied WorldState; no economy-layer passability flag is stored here.
    /// </summary>
    public sealed class LuoyangFormalCellSupplyRouteAccess :
        ILuoyangSupplyRouteAccess
    {
        private readonly WorldState world;
        private readonly CellTraversalPlan plan;
        private readonly CellTraversalPlanner planner;
        private readonly IReadOnlyDictionary<string,
            LuoyangSupplyRouteDefinition> definitions;

        public LuoyangFormalCellSupplyRouteAccess(WorldState world,
            CellTraversalPlan plan,
            IEnumerable<LuoyangSupplyRouteDefinition> definitions)
        {
            this.world = world ?? throw new ArgumentNullException(
                nameof(world));
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (definitions == null) throw new ArgumentNullException(
                nameof(definitions));
            this.definitions = definitions.ToDictionary(item => item.RouteId,
                StringComparer.Ordinal);
            if (this.definitions.Count == 0)
                throw new ArgumentException(
                    "At least one formal supply route is required.",
                    nameof(definitions));
            planner = new CellTraversalPlanner(plan);
        }

        public LuoyangSupplyRouteAssessment Assess(string routeId)
        {
            if (string.IsNullOrWhiteSpace(routeId) ||
                !definitions.TryGetValue(routeId, out var definition))
                return new LuoyangSupplyRouteAssessment
                {
                    RouteId = routeId ?? string.Empty,
                    FailureReasonId = "supply.route.unknown.v1"
                };
            if (!planner.TryFindRoute(definition.OriginCellId64,
                    definition.TargetCellId64,
                    definition.MovementCapabilityId,
                    port => LuoyangCellTraversalRules.IsPortAvailable(
                        world, port), out var route, out var failure))
                return new LuoyangSupplyRouteAssessment
                {
                    RouteId = routeId,
                    FailureReasonId = failure,
                    BlockingFormalObjectId = FirstBlockedFormalObject()
                };
            return new LuoyangSupplyRouteAssessment
            {
                RouteId = routeId,
                CanTraverse = true,
                PhysicalRouteSignature = ComputeSignature(route),
                SegmentCount = route.Segments.Count,
                WeightedDistanceCentimetres =
                    route.WeightedDistanceCentimetres
            };
        }

        private string FirstBlockedFormalObject()
        {
            return plan.Profiles.SelectMany(item => item.Ports)
                .Where(item => item.Enabled &&
                    !string.IsNullOrWhiteSpace(item.FormalWorldObjectId) &&
                    !LuoyangCellTraversalRules.IsPortAvailable(world, item))
                .OrderBy(item => item.FormalWorldObjectId,
                    StringComparer.Ordinal)
                .Select(item => item.FormalWorldObjectId)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string ComputeSignature(CellRoute route)
        {
            var text = string.Join("|", route.Segments.Select(item =>
                item.Id + ":" + item.TraversalConditionId + ":" +
                item.FormalWorldObjectId));
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        text)).Select(item => item.ToString("x2",
                        System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    public sealed class LuoyangPlayerSupplyProjection
    {
        public long Day;
        public long AuthorityRevision;
        public string StatusId;
        public long CityFoodStockMilliunits;
        public long DailyDemandMilliunits;
        public int StockDays;
        public int RepresentativePriceBasisPoints;
        public long RepresentativeUnitPrice;
        public long KnownIncomingMilliunits;
        public int KnownIncomingShipmentCount;
        public int KnownBlockedShipmentCount;
        public int KnownStorageWaitingShipmentCount;
        public long CurrentDailyShortageMilliunits;
        public int CurrentHouseholdShortageCount;
        public bool PublicProcurementActive;
        public bool ReliefActive;
        public bool IsLimitedKnowledge;
        public IReadOnlyList<string> PublicReasonIds;
    }

    /// <summary>
    /// Player-facing, read-only projection over formal batch authority. Route
    /// detail is limited to routes known by the selected player's carrier.
    /// </summary>
    public static class LuoyangPlayerSupplyProjectionSystem
    {
        public static LuoyangPlayerSupplyProjection Build(
            Luoyang184LivingWorldRuntimeState runtime,
            IEnumerable<string> knownRouteIds = null)
        {
            if (runtime?.FormalEconomy == null ||
                !runtime.FormalEconomy.IsPhysicalAuthority)
                throw new InvalidOperationException(
                    "Formal economy authority is required.");
            var limited = knownRouteIds != null;
            var known = new HashSet<string>(knownRouteIds ??
                Array.Empty<string>(), StringComparer.Ordinal);
            var publicCitySourceIds = new HashSet<string>(runtime.Inventories
                .Where(item => item.OwnerKind ==
                        LuoyangInventoryOwnerKind.Market ||
                    item.OwnerKind == LuoyangInventoryOwnerKind.Government)
                .Select(item => item.Id), StringComparer.Ordinal);
            var cityContainers = new HashSet<string>(runtime.FormalEconomy
                .InventoryBindings.Where(item =>
                    publicCitySourceIds.Contains(item.SourceId))
                .Select(item => item.InventoryContainerId),
                StringComparer.Ordinal);
            var stock = runtime.FormalEconomy.ProductBatches.Where(item =>
                    cityContainers.Contains(item.InventoryContainerId) &&
                    LuoyangFormalEconomySystem.IsFood(
                        item.ProductDefinitionId))
                .Sum(item => item.Quantity);
            var visibleShipments = runtime.Shipments.Where(item =>
                !item.Delivered && (!limited || known.Contains(item.RouteId)))
                .ToArray();
            var current = runtime.DaySnapshots.LastOrDefault(item =>
                item.Day == runtime.AbsoluteDay);
            var previous = runtime.DaySnapshots.Where(item =>
                    item.Day < runtime.AbsoluteDay)
                .OrderByDescending(item => item.Day).FirstOrDefault();
            var shortage = Math.Max(0L,
                (current?.FoodShortageMilliunits ?? 0L) -
                (previous?.FoodShortageMilliunits ?? 0L));
            var reasons = new List<string>();
            if (visibleShipments.Any(item => item.RouteWaiting))
                reasons.Add("supply.reason.route-blocked.v1");
            if (visibleShipments.Any(item => item.AwaitingReceipt))
                reasons.Add("supply.reason.storage-waiting.v1");
            if (shortage > 0)
                reasons.Add("supply.reason.household-shortage.v1");
            var market = runtime.Markets.Where(item =>
                    LuoyangFormalEconomySystem.IsFood(item.ProductId))
                .OrderByDescending(item => item.RecentTradeQuantityMilliunits)
                .ThenBy(item => item.ProductId, StringComparer.Ordinal)
                .FirstOrDefault();
            var procurement = runtime.SupplyOrders.Any(item =>
                item.Status == LuoyangSupplyOrderStatus.InTransit &&
                item.RequestedByAgentId != null &&
                item.RequestedByAgentId.IndexOf("government",
                    StringComparison.OrdinalIgnoreCase) >= 0);
            var relief = runtime.ShortageResponses.Any(item =>
                item.DetectedDay == runtime.AbsoluteDay &&
                item.ResponseActionId != null &&
                item.ResponseActionId.IndexOf("relief",
                    StringComparison.OrdinalIgnoreCase) >= 0);
            var status = shortage > 0 ? "supply.status.shortage.v1" :
                visibleShipments.Any(item => item.RouteWaiting)
                    ? "supply.status.route-delayed.v1" :
                visibleShipments.Any(item => item.AwaitingReceipt)
                    ? "supply.status.storage-delayed.v1" :
                stock < runtime.DailyFoodDemandMilliunits * 30L
                    ? "supply.status.tight.v1"
                    : "supply.status.normal.v1";
            return new LuoyangPlayerSupplyProjection
            {
                Day = runtime.AbsoluteDay,
                AuthorityRevision = runtime.FormalEconomy.Revision,
                StatusId = status,
                CityFoodStockMilliunits = stock,
                DailyDemandMilliunits = runtime.DailyFoodDemandMilliunits,
                StockDays = runtime.DailyFoodDemandMilliunits <= 0
                    ? 0
                    : (int)Math.Min(int.MaxValue,
                        stock / runtime.DailyFoodDemandMilliunits),
                RepresentativePriceBasisPoints =
                    market?.CurrentPriceBasisPoints ?? 10_000,
                RepresentativeUnitPrice = market == null
                    ? 0
                    : Math.Max(1L, market.BasePrice *
                        (long)market.CurrentPriceBasisPoints / 10_000L),
                KnownIncomingMilliunits = visibleShipments.Sum(item =>
                    item.RemainingCargoQuantityMilliunits),
                KnownIncomingShipmentCount = visibleShipments.Length,
                KnownBlockedShipmentCount = visibleShipments.Count(item =>
                    item.RouteWaiting),
                KnownStorageWaitingShipmentCount = visibleShipments.Count(
                    item => item.AwaitingReceipt),
                CurrentDailyShortageMilliunits = shortage,
                CurrentHouseholdShortageCount =
                    current?.HouseholdShortageCount ?? 0,
                PublicProcurementActive = procurement,
                ReliefActive = relief,
                IsLimitedKnowledge = limited,
                PublicReasonIds = reasons
            };
        }
    }

    public enum LuoyangMerchantDispatchFailure : byte
    {
        None,
        InvalidRequest,
        CarrierBusy,
        UnknownRoute,
        RouteBlocked,
        InsufficientCash,
        InsufficientCargo,
        CarrierCapacityExceeded,
        DestinationFull,
        NoMarketDemand
    }

    public sealed class LuoyangMerchantDispatchResult
    {
        public bool Succeeded;
        public LuoyangMerchantDispatchFailure Failure;
        public string ReasonId;
        public string ShipmentId;
        public long QuantityMilliunits;
        public long PurchaseCost;
        public long ExpectedArrivalDay;
    }
}
