using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class CellTraversalIds
    {
        public const string ContractId =
            "mandate.spatial.cell-traversal-port.v1";
        public const string PlanVersionId =
            "luoyang.cell-traversal.master.v1";
        public const string StaticConditionId =
            "cell-traversal.condition.static.v1";
        public const string FormalRoadConditionId =
            "cell-traversal.condition.formal-road.v1";
        public const string FormalPassageConditionId =
            "cell-traversal.condition.formal-passage.v1";
        public const string FormalFacilityConditionId =
            "cell-traversal.condition.formal-facility.v1";
        public const string InternalSegmentKindId =
            "cell-route.segment.internal.v1";
        public const string BoundarySegmentKindId =
            "cell-route.segment.boundary.v1";
    }

    public static class MovementCapabilityIds
    {
        public const string Foot = "movement.capability.foot.v1";
        public const string Horse = "movement.capability.horse.v1";
        public const string Cart = "movement.capability.cart.v1";
        public const string PackAnimal = "movement.capability.pack-animal.v1";
        public const string Military = "movement.capability.military.v1";
    }

    public static class FacilityAccessRequirementIds
    {
        public const string None = "facility-access.requirement.none.v1";
        public const string Optional =
            "facility-access.requirement.optional.v1";
        public const string RoadRequired =
            "facility-access.requirement.road-required.v1";
        public const string VehicleRoadRequired =
            "facility-access.requirement.vehicle-road-required.v1";

        public static readonly IReadOnlyList<string> All = new[]
        {
            None, Optional, RoadRequired, VehicleRoadRequired
        };
    }

    public static class CellTraversalPortRoleIds
    {
        public const string TerrainBoundary =
            "cell-port.role.terrain-boundary.v1";
        public const string RoadConnection =
            "cell-port.role.road-connection.v1";
        public const string FacilityEntrance =
            "cell-port.role.facility-entrance.v1";
        public const string Passage = "cell-port.role.passage.v1";
        public const string Blocked = "cell-port.role.blocked.v1";
    }

    public enum CellTraversalDirection : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public enum CellInternalTopology : byte
    {
        Terminal = 0,
        Straight = 1,
        Corner = 2,
        TIntersection = 3,
        Cross = 4,
        OpenArea = 5,
        Custom = 6
    }

    [Serializable]
    public sealed class CellTraversalPort
    {
        public CellTraversalDirection Direction;
        public bool Enabled;
        public bool AllowsEntry;
        public bool AllowsExit;
        public string RoleId = CellTraversalPortRoleIds.Blocked;
        public string AccessPolicyId = FacilityAccessRequirementIds.None;
        public string TraversalConditionId = CellTraversalIds.StaticConditionId;
        public string FormalWorldObjectId = string.Empty;
        public int AdditionalDistanceCentimetres;
        public int WidthCentimetres;
        public int CapacityClass;
        public List<string> MovementCapabilityIds = new List<string>();

        public bool Supports(string movementCapabilityId) => Enabled &&
            MovementCapabilityIds.Contains(movementCapabilityId,
                StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class CellInternalPortConnection
    {
        public CellTraversalDirection First;
        public CellTraversalDirection Second;
    }

    [Serializable]
    public sealed class CellTraversalProfile
    {
        public ulong CellId64;
        public string TerrainCapabilityId;
        public string FacilityId = string.Empty;
        public string FacilityDefinitionId = string.Empty;
        public string FacilityCapabilityId = string.Empty;
        public string AccessRequirementId =
            FacilityAccessRequirementIds.None;
        public bool PassThroughAllowed = true;
        public CellInternalTopology InternalTopology =
            CellInternalTopology.OpenArea;
        public int TraversalDistanceCentimetres;
        public Dictionary<string, int> TraversalCostPermilleByCapability =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public List<CellTraversalPort> Ports = new List<CellTraversalPort>();
        public List<CellInternalPortConnection> CustomConnections =
            new List<CellInternalPortConnection>();

        public CellTraversalPort Port(CellTraversalDirection direction) =>
            Ports.Single(item => item.Direction == direction);

        public int CostPermille(string capabilityId) =>
            TraversalCostPermilleByCapability.TryGetValue(capabilityId,
                out var value) ? value : int.MaxValue;

        public bool AllowsInternal(CellTraversalDirection entry,
            CellTraversalDirection exit)
        {
            if (!PassThroughAllowed || entry == exit) return false;
            switch (InternalTopology)
            {
                case CellInternalTopology.Terminal:
                    return false;
                case CellInternalTopology.Straight:
                    return CellTraversalDirections.Opposite(entry) == exit;
                case CellInternalTopology.Corner:
                    return CellTraversalDirections.Opposite(entry) != exit;
                case CellInternalTopology.TIntersection:
                case CellInternalTopology.Cross:
                case CellInternalTopology.OpenArea:
                    return true;
                case CellInternalTopology.Custom:
                    return CustomConnections.Any(item =>
                        item.First == entry && item.Second == exit ||
                        item.First == exit && item.Second == entry);
                default:
                    return false;
            }
        }

        public bool AllowsInternal(CellTraversalDirection entry,
            CellTraversalDirection exit, string movementCapabilityId)
        {
            if (string.Equals(FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Road,
                    StringComparison.Ordinal) &&
                !string.Equals(movementCapabilityId,
                    MovementCapabilityIds.Cart,
                    StringComparison.Ordinal) &&
                !string.Equals(movementCapabilityId,
                    MovementCapabilityIds.PackAnimal,
                    StringComparison.Ordinal))
                return PassThroughAllowed && entry != exit &&
                    Port(entry).Enabled && Port(exit).Enabled;
            return AllowsInternal(entry, exit);
        }
    }

    public static class CellTraversalDirections
    {
        public static readonly IReadOnlyList<CellTraversalDirection> All =
            new[]
            {
                CellTraversalDirection.North,
                CellTraversalDirection.East,
                CellTraversalDirection.South,
                CellTraversalDirection.West
            };

        public static CellTraversalDirection Opposite(
            CellTraversalDirection direction) =>
            (CellTraversalDirection)(((int)direction + 2) % 4);

        public static WorldMapCellId? Neighbor(CellGridIndex grid,
            ulong cellId64, CellTraversalDirection direction)
        {
            var service = new CellNeighborService(grid);
            return service.GetNeighborCell(new WorldMapCellId(cellId64),
                (GlobalCellEdgeDirection)(int)direction);
        }

        public static int EastCentimetres(CellTraversalDirection direction) =>
            direction == CellTraversalDirection.East ? 200_000 :
            direction == CellTraversalDirection.West ? 0 : 100_000;

        public static int NorthCentimetres(
            CellTraversalDirection direction) =>
            direction == CellTraversalDirection.North ? 200_000 :
            direction == CellTraversalDirection.South ? 0 : 100_000;
    }

    public sealed class CellTraversalPlan
    {
        public CellTraversalPlan(IReadOnlyList<CellTraversalProfile> profiles,
            string assetHash)
        {
            Profiles = profiles ?? throw new ArgumentNullException(
                nameof(profiles));
            AssetHash = assetHash ?? throw new ArgumentNullException(
                nameof(assetHash));
            ProfilesByCellId = profiles.ToDictionary(item => item.CellId64);
            Validate();
        }

        public string ContractId => CellTraversalIds.ContractId;
        public string VersionId => CellTraversalIds.PlanVersionId;
        public string AssetHash { get; }
        public IReadOnlyList<CellTraversalProfile> Profiles { get; }
        public IReadOnlyDictionary<ulong, CellTraversalProfile>
            ProfilesByCellId { get; }

        private void Validate()
        {
            if (Profiles.Count == 0 || AssetHash.Length != 64 ||
                ProfilesByCellId.Count != Profiles.Count)
                throw new InvalidOperationException(
                    "The Cell traversal plan is incomplete.");
            foreach (var profile in Profiles)
            {
                if (profile == null || profile.CellId64 == 0 ||
                    profile.TraversalDistanceCentimetres <= 0 ||
                    profile.Ports == null || profile.Ports.Count != 4 ||
                    profile.Ports.Select(item => item.Direction).Distinct()
                        .Count() != 4 ||
                    !FacilityAccessRequirementIds.All.Contains(
                        profile.AccessRequirementId,
                        StringComparer.Ordinal) ||
                    profile.TraversalCostPermilleByCapability.Any(item =>
                        item.Value < 1000 || item.Value > 10_000) ||
                    profile.Ports.Any(item => item == null ||
                        item.AdditionalDistanceCentimetres < 0 ||
                        item.WidthCentimetres < 0 || item.CapacityClass < 0 ||
                        item.MovementCapabilityIds == null ||
                        item.MovementCapabilityIds.Distinct(
                            StringComparer.Ordinal).Count() !=
                        item.MovementCapabilityIds.Count))
                    throw new InvalidOperationException(
                        "Invalid Cell traversal profile: " +
                        profile?.CellId64);
            }
        }
    }

    public sealed class CellRouteSegment
    {
        public CellRouteSegment()
        {
        }

        public CellRouteSegment(string id, ulong fromCellId64,
            ulong toCellId64, string traversalConditionId,
            string formalWorldObjectId)
        {
            Id = id;
            FromCellId64 = fromCellId64;
            ToCellId64 = toCellId64;
            TraversalConditionId = traversalConditionId;
            FormalWorldObjectId = formalWorldObjectId;
        }

        public int Sequence { get; internal set; }
        public string Id { get; internal set; }
        public string KindId { get; internal set; }
        public ulong FromCellId64 { get; internal set; }
        public ulong ToCellId64 { get; internal set; }
        public CellTraversalDirection? FromPort { get; internal set; }
        public CellTraversalDirection? ToPort { get; internal set; }
        public int FromEastCentimetres { get; internal set; }
        public int FromNorthCentimetres { get; internal set; }
        public int ToEastCentimetres { get; internal set; }
        public int ToNorthCentimetres { get; internal set; }
        public int DistanceCentimetres { get; internal set; }
        public int TraversalCostPermille { get; internal set; }
        public string TraversalConditionId { get; internal set; }
        public string FormalWorldObjectId { get; internal set; }
    }

    public sealed class CellRoute
    {
        internal CellRoute(ulong originCellId64, ulong targetCellId64,
            string movementCapabilityId,
            IReadOnlyList<CellRouteSegment> segments)
        {
            OriginCellId64 = originCellId64;
            TargetCellId64 = targetCellId64;
            MovementCapabilityId = movementCapabilityId;
            Segments = segments;
            DistanceCentimetres = segments.Sum(item =>
                (long)item.DistanceCentimetres);
            WeightedDistanceCentimetres = segments.Sum(item =>
                (long)item.DistanceCentimetres *
                item.TraversalCostPermille / 1000L);
        }

        public ulong OriginCellId64 { get; }
        public ulong TargetCellId64 { get; }
        public string MovementCapabilityId { get; }
        public IReadOnlyList<CellRouteSegment> Segments { get; }
        public long DistanceCentimetres { get; }
        public long WeightedDistanceCentimetres { get; }
    }

    public sealed class CellTraversalPlanner
    {
        private readonly CellTraversalPlan _plan;
        private readonly CellGridIndex _grid;
        private static readonly IComparer<Tuple<long, string>> QueueComparer =
            Comparer<Tuple<long, string>>.Create((left, right) =>
            {
                var cost = left.Item1.CompareTo(right.Item1);
                return cost != 0 ? cost : string.CompareOrdinal(left.Item2,
                    right.Item2);
            });

        public CellTraversalPlanner(CellTraversalPlan plan,
            CellGridIndex grid = null)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _grid = grid ?? GlobalSpatialFoundationV1.CreateCellGrid();
        }

        public bool TryFindRoute(ulong originCellId64, ulong targetCellId64,
            string movementCapabilityId,
            Func<CellTraversalPort, bool> isPortAvailable,
            out CellRoute route, out string failureReasonId)
        {
            route = null;
            if (!_plan.ProfilesByCellId.ContainsKey(originCellId64) ||
                !_plan.ProfilesByCellId.ContainsKey(targetCellId64))
            {
                failureReasonId = "cell-route.failure.unknown-cell.v1";
                return false;
            }
            if (originCellId64 == targetCellId64)
            {
                failureReasonId = "cell-route.failure.same-cell.v1";
                return false;
            }
            if (string.IsNullOrWhiteSpace(movementCapabilityId))
                throw new ArgumentException(
                    "A movement capability ID is required.",
                    nameof(movementCapabilityId));
            isPortAvailable ??= _ => true;

            var startKey = Key(originCellId64, null);
            var distance = new Dictionary<string, long>(StringComparer.Ordinal)
                { [startKey] = 0L };
            var states = new Dictionary<string, TraversalState>(
                StringComparer.Ordinal)
            {
                [startKey] = new TraversalState(originCellId64, null)
            };
            var previous = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var previousExit = new Dictionary<string,
                CellTraversalDirection>(StringComparer.Ordinal);
            var queue = new SortedSet<Tuple<long, string>>(QueueComparer)
                { Tuple.Create(0L, startKey) };
            string targetKey = null;
            while (queue.Count > 0)
            {
                var queued = queue.Min;
                queue.Remove(queued);
                if (!distance.TryGetValue(queued.Item2, out var known) ||
                    known != queued.Item1) continue;
                var state = states[queued.Item2];
                var profile = _plan.ProfilesByCellId[state.CellId64];
                if (state.CellId64 == targetCellId64 &&
                    state.Incoming.HasValue)
                {
                    targetKey = queued.Item2;
                    break;
                }
                foreach (var direction in CellTraversalDirections.All)
                {
                    var port = profile.Port(direction);
                    if (!port.AllowsExit ||
                        !port.Supports(movementCapabilityId) ||
                        !isPortAvailable(port) ||
                        state.Incoming.HasValue && !profile.AllowsInternal(
                            state.Incoming.Value, direction,
                            movementCapabilityId)) continue;
                    var neighborId = CellTraversalDirections.Neighbor(_grid,
                        state.CellId64, direction);
                    if (!neighborId.HasValue ||
                        !_plan.ProfilesByCellId.TryGetValue(
                            neighborId.Value.Value, out var neighbor))
                        continue;
                    var incoming = CellTraversalDirections.Opposite(direction);
                    var neighborPort = neighbor.Port(incoming);
                    if (!neighborPort.AllowsEntry ||
                        !neighborPort.Supports(movementCapabilityId) ||
                        !isPortAvailable(neighborPort)) continue;
                    var nextKey = Key(neighbor.CellId64, incoming);
                    states[nextKey] = new TraversalState(neighbor.CellId64,
                        incoming);
                    var candidate = checked(known + TransitionCost(profile,
                        port, neighborPort, movementCapabilityId,
                        state.Incoming.HasValue));
                    if (distance.TryGetValue(nextKey, out var old) &&
                        candidate >= old) continue;
                    distance[nextKey] = candidate;
                    previous[nextKey] = queued.Item2;
                    previousExit[nextKey] = direction;
                    queue.Add(Tuple.Create(candidate, nextKey));
                }
            }
            if (targetKey == null)
            {
                failureReasonId = "cell-route.failure.unreachable.v1";
                return false;
            }
            var reverse = new List<string>();
            for (var cursor = targetKey;; cursor = previous[cursor])
            {
                reverse.Add(cursor);
                if (cursor == startKey) break;
            }
            reverse.Reverse();
            var segments = BuildSegments(reverse, states, previousExit,
                movementCapabilityId, isPortAvailable);
            route = new CellRoute(originCellId64, targetCellId64,
                movementCapabilityId, segments);
            failureReasonId = string.Empty;
            return true;
        }

        private IReadOnlyList<CellRouteSegment> BuildSegments(
            IReadOnlyList<string> path,
            IReadOnlyDictionary<string, TraversalState> states,
            IReadOnlyDictionary<string, CellTraversalDirection> previousExit,
            string capabilityId,
            Func<CellTraversalPort, bool> isPortAvailable)
        {
            var result = new List<CellRouteSegment>();
            for (var index = 0; index + 1 < path.Count; index++)
            {
                var current = states[path[index]];
                var next = states[path[index + 1]];
                var profile = _plan.ProfilesByCellId[current.CellId64];
                var exit = previousExit[path[index + 1]];
                var port = profile.Port(exit);
                AddInternal(result, profile, current.Incoming, exit,
                    capabilityId);
                var nextPort = _plan.ProfilesByCellId[next.CellId64].Port(
                    next.Incoming.Value);
                var formal = ResolveFormal(port, nextPort);
                result.Add(new CellRouteSegment
                {
                    Sequence = result.Count,
                    Id = "cell-route.boundary.v1." + current.CellId64 + "." +
                         ((int)exit) + "." + next.CellId64,
                    KindId = CellTraversalIds.BoundarySegmentKindId,
                    FromCellId64 = current.CellId64,
                    ToCellId64 = next.CellId64,
                    FromPort = exit,
                    ToPort = next.Incoming,
                    FromEastCentimetres = CellTraversalDirections
                        .EastCentimetres(exit),
                    FromNorthCentimetres = CellTraversalDirections
                        .NorthCentimetres(exit),
                    ToEastCentimetres = CellTraversalDirections
                        .EastCentimetres(next.Incoming.Value),
                    ToNorthCentimetres = CellTraversalDirections
                        .NorthCentimetres(next.Incoming.Value),
                    DistanceCentimetres = Math.Max(100,
                        port.AdditionalDistanceCentimetres +
                        nextPort.AdditionalDistanceCentimetres),
                    TraversalCostPermille = Math.Max(1000,
                        Math.Max(profile.CostPermille(capabilityId),
                            _plan.ProfilesByCellId[next.CellId64]
                                .CostPermille(capabilityId))),
                    TraversalConditionId = formal.Item1,
                    FormalWorldObjectId = formal.Item2
                });
            }
            var target = states[path[path.Count - 1]];
            AddInternal(result, _plan.ProfilesByCellId[target.CellId64],
                target.Incoming, null, capabilityId);
            for (var index = 0; index < result.Count; index++)
                result[index].Sequence = index;
            return result;
        }

        private static void AddInternal(ICollection<CellRouteSegment> result,
            CellTraversalProfile profile,
            CellTraversalDirection? entry,
            CellTraversalDirection? exit, string capabilityId)
        {
            if (!entry.HasValue && !exit.HasValue) return;
            var condition = InternalCondition(profile);
            var fromEast = entry.HasValue
                ? CellTraversalDirections.EastCentimetres(entry.Value)
                : 100_000;
            var fromNorth = entry.HasValue
                ? CellTraversalDirections.NorthCentimetres(entry.Value)
                : 100_000;
            var toEast = exit.HasValue
                ? CellTraversalDirections.EastCentimetres(exit.Value)
                : 100_000;
            var toNorth = exit.HasValue
                ? CellTraversalDirections.NorthCentimetres(exit.Value)
                : 100_000;
            var collection = (List<CellRouteSegment>)result;
            collection.Add(new CellRouteSegment
            {
                Sequence = collection.Count,
                Id = "cell-route.internal.v1." + profile.CellId64 + "." +
                     (entry.HasValue ? ((int)entry.Value).ToString() : "c") +
                     "." +
                     (exit.HasValue ? ((int)exit.Value).ToString() : "c"),
                KindId = CellTraversalIds.InternalSegmentKindId,
                FromCellId64 = profile.CellId64,
                ToCellId64 = profile.CellId64,
                FromPort = entry,
                ToPort = exit,
                FromEastCentimetres = fromEast,
                FromNorthCentimetres = fromNorth,
                ToEastCentimetres = toEast,
                ToNorthCentimetres = toNorth,
                DistanceCentimetres = Math.Max(100,
                    entry.HasValue && exit.HasValue
                        ? profile.TraversalDistanceCentimetres
                        : profile.TraversalDistanceCentimetres / 2),
                TraversalCostPermille = profile.CostPermille(capabilityId),
                TraversalConditionId = condition.Item1,
                FormalWorldObjectId = condition.Item2
            });
        }

        private static Tuple<string, string> InternalCondition(
            CellTraversalProfile profile)
        {
            if (string.Equals(profile.FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal) ||
                string.Equals(profile.FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal))
                return Tuple.Create(
                    CellTraversalIds.FormalPassageConditionId,
                    profile.FacilityId);
            if (!string.IsNullOrEmpty(profile.FacilityId))
                return Tuple.Create(
                    CellTraversalIds.FormalFacilityConditionId,
                    profile.FacilityId);
            return Tuple.Create(CellTraversalIds.StaticConditionId,
                string.Empty);
        }

        private static Tuple<string, string> ResolveFormal(
            CellTraversalPort first, CellTraversalPort second)
        {
            foreach (var port in new[] { first, second })
                if (string.Equals(port.TraversalConditionId,
                        CellTraversalIds.FormalPassageConditionId,
                        StringComparison.Ordinal))
                    return Tuple.Create(port.TraversalConditionId,
                        port.FormalWorldObjectId);
            foreach (var port in new[] { first, second })
                if (string.Equals(port.TraversalConditionId,
                        CellTraversalIds.FormalRoadConditionId,
                        StringComparison.Ordinal))
                    return Tuple.Create(port.TraversalConditionId,
                        port.FormalWorldObjectId);
            return Tuple.Create(CellTraversalIds.StaticConditionId,
                string.Empty);
        }

        private static long TransitionCost(CellTraversalProfile profile,
            CellTraversalPort exit, CellTraversalPort entry,
            string capabilityId, bool hasIncoming)
        {
            var distance = (hasIncoming
                    ? profile.TraversalDistanceCentimetres
                    : profile.TraversalDistanceCentimetres / 2) +
                Math.Max(100, exit.AdditionalDistanceCentimetres +
                              entry.AdditionalDistanceCentimetres);
            return Math.Max(1L, (long)distance *
                profile.CostPermille(capabilityId) / 1000L);
        }

        private static string Key(ulong cellId64,
            CellTraversalDirection? incoming) => cellId64 + ":" +
            (incoming.HasValue ? ((int)incoming.Value).ToString() : "x");

        private sealed class TraversalState
        {
            public TraversalState(ulong cellId64,
                CellTraversalDirection? incoming)
            {
                CellId64 = cellId64;
                Incoming = incoming;
            }
            public ulong CellId64 { get; }
            public CellTraversalDirection? Incoming { get; }
        }
    }

    public static class LuoyangCellTraversalRules
    {
        private static readonly string[] FootCapabilities =
        {
            MovementCapabilityIds.Foot, MovementCapabilityIds.Horse,
            MovementCapabilityIds.Military
        };
        private static readonly string[] RoadCapabilities =
        {
            MovementCapabilityIds.Foot, MovementCapabilityIds.Horse,
            MovementCapabilityIds.Cart, MovementCapabilityIds.PackAnimal,
            MovementCapabilityIds.Military
        };

        public static CellTraversalPlan CreatePlan(
            LuoyangHumanScaleLocalMapPlan localMap,
            LuoyangRoadTraversalRefinementPlan strategicRoads)
        {
            if (localMap == null) throw new ArgumentNullException(
                nameof(localMap));
            if (strategicRoads == null) throw new ArgumentNullException(
                nameof(strategicRoads));
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var mapCellIds = new HashSet<ulong>(localMap.LocalSpaces.Select(
                item => item.ParentCellId64));
            var capabilities = localMap.FacilityCapabilities.ToDictionary(
                item => item.CellId64);
            var directRoads = DirectRoadBoundaries(strategicRoads, grid);
            var profiles = new List<CellTraversalProfile>(
                localMap.LocalSpaces.Count);
            foreach (var space in localMap.LocalSpaces.OrderBy(item =>
                         item.ParentCellId64))
            {
                capabilities.TryGetValue(space.ParentCellId64,
                    out var capability);
                profiles.Add(CreateProfile(space.ParentCellId64, capability,
                    capabilities, mapCellIds, directRoads, grid));
            }
            var hash = ComputeHash(profiles);
            var plan = new CellTraversalPlan(profiles, hash);
            ValidateLuoyang(plan, localMap, grid);
            return plan;
        }

        public static bool IsPortAvailable(WorldState world,
            CellTraversalPort port)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (port == null || !port.Enabled) return false;
            if (string.Equals(port.TraversalConditionId,
                    CellTraversalIds.FormalRoadConditionId,
                    StringComparison.Ordinal))
                return LuoyangHumanScaleWorldTraversalRules
                    .CanTraverseStrategicEdge(world,
                        port.FormalWorldObjectId);
            if (string.Equals(port.TraversalConditionId,
                    CellTraversalIds.FormalPassageConditionId,
                    StringComparison.Ordinal))
                return LuoyangHumanScaleWorldTraversalRules
                    .CanTraversePassage(world, port.FormalWorldObjectId);
            return true;
        }

        public static bool CanTraverseSegment(WorldState world,
            CellRouteSegment segment)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (segment == null) return false;
            if (string.Equals(segment.TraversalConditionId,
                    CellTraversalIds.StaticConditionId,
                    StringComparison.Ordinal)) return true;
            if (string.Equals(segment.TraversalConditionId,
                    CellTraversalIds.FormalRoadConditionId,
                    StringComparison.Ordinal))
                return LuoyangHumanScaleWorldTraversalRules
                    .CanTraverseStrategicEdge(world,
                        segment.FormalWorldObjectId);
            if (string.Equals(segment.TraversalConditionId,
                    CellTraversalIds.FormalPassageConditionId,
                    StringComparison.Ordinal))
                return LuoyangHumanScaleWorldTraversalRules
                    .CanTraversePassage(world,
                        segment.FormalWorldObjectId);
            if (string.Equals(segment.TraversalConditionId,
                    CellTraversalIds.FormalFacilityConditionId,
                    StringComparison.Ordinal))
                return LuoyangHumanScaleWorldTraversalRules
                    .IsFacilityAccessible(world,
                        segment.FormalWorldObjectId);
            return false;
        }

        private static CellTraversalProfile CreateProfile(ulong cellId64,
            LuoyangFacilitySpatialCapability capability,
            IReadOnlyDictionary<ulong, LuoyangFacilitySpatialCapability>
                facilitiesByCell,
            ISet<ulong> mapCellIds,
            IReadOnlyDictionary<string, string> directRoads,
            CellGridIndex grid)
        {
            var spatial = capability?.CapabilityId ?? string.Empty;
            var road = string.Equals(spatial,
                FacilitySpatialCapabilityIds.Road,
                StringComparison.Ordinal);
            var gate = string.Equals(spatial,
                FacilitySpatialCapabilityIds.Gate,
                StringComparison.Ordinal);
            var bridge = string.Equals(spatial,
                FacilitySpatialCapabilityIds.Bridge,
                StringComparison.Ordinal);
            var open = string.Equals(spatial,
                FacilitySpatialCapabilityIds.OpenArea,
                StringComparison.Ordinal);
            var productive = string.Equals(spatial,
                FacilitySpatialCapabilityIds.ProductiveLand,
                StringComparison.Ordinal);
            var blocked = string.Equals(spatial,
                    FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal) ||
                string.Equals(spatial,
                    FacilitySpatialCapabilityIds.MoatOrWater,
                    StringComparison.Ordinal);
            var building = capability != null && !road && !gate && !bridge &&
                !open && !productive && !blocked;
            var hasAdjacentRoad = CellTraversalDirections.All.Any(direction =>
            {
                var neighbor = CellTraversalDirections.Neighbor(grid,
                    cellId64, direction);
                return neighbor.HasValue && facilitiesByCell.TryGetValue(
                    neighbor.Value.Value, out var adjacent) &&
                    string.Equals(adjacent.CapabilityId,
                        FacilitySpatialCapabilityIds.Road,
                        StringComparison.Ordinal);
            });
            var access = ResolveAccessRequirement(capability,
                hasAdjacentRoad);
            var passThrough = capability == null || road || gate || bridge ||
                open || productive;
            var profile = new CellTraversalProfile
            {
                CellId64 = cellId64,
                TerrainCapabilityId = productive
                    ? "terrain.traversal.productive-land.v1"
                    : blocked ? "terrain.traversal.blocked.v1"
                    : road ? "terrain.traversal.road.v1"
                    : "terrain.traversal.open-ground.v1",
                FacilityId = capability?.FacilityId ?? string.Empty,
                FacilityDefinitionId = capability?.FacilityDefinitionId ??
                                       string.Empty,
                FacilityCapabilityId = spatial,
                AccessRequirementId = access,
                PassThroughAllowed = passThrough,
                TraversalDistanceCentimetres = road ? 10_000 :
                    gate ? 4_000 : bridge ? 8_000 :
                    building ? 3_000 : open ? 8_000 :
                    productive ? 18_000 : 12_000
            };
            AddCosts(profile, road, gate, bridge, building, productive,
                blocked);
            var enabled = EnabledDirections(cellId64, capability, access,
                facilitiesByCell, mapCellIds, grid);
            foreach (var direction in CellTraversalDirections.All)
            {
                var isEnabled = !blocked && enabled.Contains(direction);
                var roadConnection = road && IsRoadConnectionDirection(
                    cellId64, direction, facilitiesByCell, directRoads, grid);
                var movement = gate || bridge || roadConnection
                    ? RoadCapabilities : FootCapabilities;
                var formal = ResolvePortFormal(cellId64, direction,
                    capability, directRoads, grid);
                profile.Ports.Add(new CellTraversalPort
                {
                    Direction = direction,
                    Enabled = isEnabled,
                    AllowsEntry = isEnabled,
                    AllowsExit = isEnabled,
                    RoleId = !isEnabled
                        ? CellTraversalPortRoleIds.Blocked
                        : gate || bridge
                            ? CellTraversalPortRoleIds.Passage
                            : roadConnection
                                ? CellTraversalPortRoleIds.RoadConnection
                                : building
                                    ? CellTraversalPortRoleIds
                                        .FacilityEntrance
                                    : CellTraversalPortRoleIds
                                        .TerrainBoundary,
                    AccessPolicyId = access,
                    TraversalConditionId = formal.Item1,
                    FormalWorldObjectId = formal.Item2,
                    AdditionalDistanceCentimetres = gate ? 500 :
                        bridge ? 800 : road ? 100 : 300,
                    WidthCentimetres = road ? 800 : gate ? 600 :
                        bridge ? 500 : building ? 250 : 400,
                    CapacityClass = road ? 4 : gate || bridge ? 3 :
                        building ? 1 : 2,
                    MovementCapabilityIds = isEnabled
                        ? movement.ToList() : new List<string>()
                });
            }
            profile.InternalTopology = ResolveTopology(road
                    ? profile.Ports.Where(item => string.Equals(item.RoleId,
                        CellTraversalPortRoleIds.RoadConnection,
                        StringComparison.Ordinal)).ToArray()
                    : profile.Ports,
                passThrough, open);
            return profile;
        }

        private static bool IsRoadConnectionDirection(ulong cellId64,
            CellTraversalDirection direction,
            IReadOnlyDictionary<ulong, LuoyangFacilitySpatialCapability>
                facilitiesByCell,
            IReadOnlyDictionary<string, string> directRoads,
            CellGridIndex grid)
        {
            var neighbor = CellTraversalDirections.Neighbor(grid, cellId64,
                direction);
            if (!neighbor.HasValue) return false;
            if (directRoads.ContainsKey(Pair(cellId64,
                    neighbor.Value.Value))) return true;
            if (!facilitiesByCell.TryGetValue(neighbor.Value.Value,
                    out var facility)) return false;
            return string.Equals(facility.CapabilityId,
                       FacilitySpatialCapabilityIds.Road,
                       StringComparison.Ordinal) ||
                   string.Equals(facility.CapabilityId,
                       FacilitySpatialCapabilityIds.Gate,
                       StringComparison.Ordinal) ||
                   string.Equals(facility.CapabilityId,
                       FacilitySpatialCapabilityIds.Bridge,
                       StringComparison.Ordinal);
        }

        private static HashSet<CellTraversalDirection> EnabledDirections(
            ulong cellId64, LuoyangFacilitySpatialCapability capability,
            string accessRequirementId,
            IReadOnlyDictionary<ulong, LuoyangFacilitySpatialCapability>
                facilitiesByCell,
            ISet<ulong> mapCellIds,
            CellGridIndex grid)
        {
            var all = new HashSet<CellTraversalDirection>(
                CellTraversalDirections.All.Where(direction =>
                {
                    var neighbor = CellTraversalDirections.Neighbor(grid,
                        cellId64, direction);
                    return neighbor.HasValue && mapCellIds.Contains(
                        neighbor.Value.Value);
                }));
            if (capability == null) return all;
            var spatial = capability.CapabilityId;
            if (string.Equals(spatial, FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal) ||
                string.Equals(spatial,
                    FacilitySpatialCapabilityIds.MoatOrWater,
                    StringComparison.Ordinal)) return new HashSet<
                        CellTraversalDirection>();
            if (string.Equals(spatial, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal) ||
                string.Equals(spatial, FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal))
            {
                var northSouth = RoadNeighborScore(cellId64,
                    CellTraversalDirection.North, facilitiesByCell, grid) +
                    RoadNeighborScore(cellId64,
                        CellTraversalDirection.South, facilitiesByCell, grid);
                var eastWest = RoadNeighborScore(cellId64,
                    CellTraversalDirection.East, facilitiesByCell, grid) +
                    RoadNeighborScore(cellId64,
                        CellTraversalDirection.West, facilitiesByCell, grid);
                var selected = northSouth >= eastWest
                    ? new HashSet<CellTraversalDirection>(new[]
                    {
                        CellTraversalDirection.North,
                        CellTraversalDirection.South
                    })
                    : new HashSet<CellTraversalDirection>(new[]
                    {
                        CellTraversalDirection.East,
                        CellTraversalDirection.West
                    });
                selected.IntersectWith(all);
                return selected;
            }
            if (!string.Equals(accessRequirementId,
                    FacilityAccessRequirementIds.RoadRequired,
                    StringComparison.Ordinal) &&
                !string.Equals(accessRequirementId,
                    FacilityAccessRequirementIds.VehicleRoadRequired,
                    StringComparison.Ordinal)) return all;
            return new HashSet<CellTraversalDirection>(
                all.Where(direction =>
                    RoadNeighborScore(cellId64, direction, facilitiesByCell,
                        grid) > 0));
        }

        private static int RoadNeighborScore(ulong cellId64,
            CellTraversalDirection direction,
            IReadOnlyDictionary<ulong, LuoyangFacilitySpatialCapability>
                facilitiesByCell, CellGridIndex grid)
        {
            var neighbor = CellTraversalDirections.Neighbor(grid, cellId64,
                direction);
            if (!neighbor.HasValue || !facilitiesByCell.TryGetValue(
                    neighbor.Value.Value, out var facility)) return 0;
            return string.Equals(facility.CapabilityId,
                FacilitySpatialCapabilityIds.Road,
                StringComparison.Ordinal) ? 1 : 0;
        }

        private static string ResolveAccessRequirement(
            LuoyangFacilitySpatialCapability capability,
            bool hasAdjacentRoad)
        {
            if (capability == null || string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Road,
                    StringComparison.Ordinal) ||
                string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal) ||
                string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.MoatOrWater,
                    StringComparison.Ordinal))
                return FacilityAccessRequirementIds.None;
            var highLogistics = string.Equals(
                    capability.FacilityDefinitionId,
                    "facility.military.fortified_manor",
                    StringComparison.Ordinal) ||
                (capability.FacilityDefinitionId.IndexOf("warehouse",
                     StringComparison.Ordinal) >= 0 ||
                 capability.FacilityDefinitionId.IndexOf("granary",
                     StringComparison.Ordinal) >= 0) && hasAdjacentRoad;
            return highLogistics
                ? FacilityAccessRequirementIds.RoadRequired
                : FacilityAccessRequirementIds.Optional;
        }

        private static void AddCosts(CellTraversalProfile profile, bool road,
            bool gate, bool bridge, bool building, bool productive,
            bool blocked)
        {
            if (blocked) return;
            var foot = road ? 1000 : gate ? 1100 : bridge ? 1150 :
                building ? 1100 : productive ? 1500 : 1250;
            profile.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Foot] = foot;
            profile.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Horse] = road ? 1000 :
                productive ? 1800 : 1450;
            profile.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Military] = road ? 1000 :
                productive ? 1700 : 1400;
            if (road || gate || bridge)
            {
                profile.TraversalCostPermilleByCapability[
                    MovementCapabilityIds.Cart] = road ? 1000 : 1200;
                profile.TraversalCostPermilleByCapability[
                    MovementCapabilityIds.PackAnimal] = road ? 1000 : 1150;
            }
        }

        private static CellInternalTopology ResolveTopology(
            IReadOnlyList<CellTraversalPort> ports, bool passThrough,
            bool openArea)
        {
            if (!passThrough) return CellInternalTopology.Terminal;
            if (openArea) return CellInternalTopology.OpenArea;
            var enabled = ports.Where(item => item.Enabled).Select(item =>
                item.Direction).ToArray();
            if (enabled.Length <= 1) return CellInternalTopology.Terminal;
            if (enabled.Length == 2)
                return CellTraversalDirections.Opposite(enabled[0]) ==
                       enabled[1]
                    ? CellInternalTopology.Straight
                    : CellInternalTopology.Corner;
            return enabled.Length == 3
                ? CellInternalTopology.TIntersection
                : CellInternalTopology.Cross;
        }

        private static Tuple<string, string> ResolvePortFormal(ulong cellId64,
            CellTraversalDirection direction,
            LuoyangFacilitySpatialCapability capability,
            IReadOnlyDictionary<string, string> directRoads,
            CellGridIndex grid)
        {
            if (capability != null &&
                (string.Equals(capability.CapabilityId,
                     FacilitySpatialCapabilityIds.Gate,
                     StringComparison.Ordinal) ||
                 string.Equals(capability.CapabilityId,
                     FacilitySpatialCapabilityIds.Bridge,
                     StringComparison.Ordinal)))
                return Tuple.Create(
                    CellTraversalIds.FormalPassageConditionId,
                    capability.FacilityId);
            var neighbor = CellTraversalDirections.Neighbor(grid, cellId64,
                direction);
            if (neighbor.HasValue && directRoads.TryGetValue(Pair(cellId64,
                    neighbor.Value.Value), out var edgeId))
                return Tuple.Create(CellTraversalIds.FormalRoadConditionId,
                    edgeId);
            return Tuple.Create(CellTraversalIds.StaticConditionId,
                string.Empty);
        }

        private static IReadOnlyDictionary<string, string>
            DirectRoadBoundaries(LuoyangRoadTraversalRefinementPlan roads,
                CellGridIndex grid)
        {
            var nodes = roads.NavigationNodes.ToDictionary(item =>
                item.NodeId, StringComparer.Ordinal);
            var result = new Dictionary<string, string>(
                StringComparer.Ordinal);
            foreach (var edge in roads.NavigationEdges.OrderBy(item =>
                         item.EdgeId, StringComparer.Ordinal))
            {
                var first = nodes[edge.FromNodeId];
                var second = nodes[edge.ToNodeId];
                if (Math.Abs(first.GridRow - second.GridRow) +
                    Math.Abs(first.GridColumn - second.GridColumn) != 1)
                    continue;
                result[Pair(first.CellId64, second.CellId64)] = edge.EdgeId;
            }
            return result;
        }

        private static string Pair(ulong first, ulong second) =>
            first < second ? first + ":" + second : second + ":" + first;

        private static string ComputeHash(
            IReadOnlyList<CellTraversalProfile> profiles)
        {
            var builder = new StringBuilder(CellTraversalIds.PlanVersionId);
            foreach (var profile in profiles)
            {
                builder.Append('|').Append(profile.CellId64).Append(':')
                    .Append(profile.TerrainCapabilityId).Append(':')
                    .Append(profile.FacilityId).Append(':')
                    .Append(profile.AccessRequirementId).Append(':')
                    .Append(profile.PassThroughAllowed).Append(':')
                    .Append((int)profile.InternalTopology).Append(':')
                    .Append(profile.TraversalDistanceCentimetres);
                foreach (var port in profile.Ports.OrderBy(item =>
                             item.Direction))
                    builder.Append(':').Append((int)port.Direction)
                        .Append(',').Append(port.Enabled)
                        .Append(',').Append(port.TraversalConditionId)
                        .Append(',').Append(port.FormalWorldObjectId);
            }
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                    builder.ToString())).Select(item =>
                    item.ToString("x2",
                        System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static void ValidateLuoyang(CellTraversalPlan plan,
            LuoyangHumanScaleLocalMapPlan localMap, CellGridIndex grid)
        {
            if (plan.Profiles.Count != localMap.LocalSpaces.Count ||
                plan.Profiles.Count !=
                    LuoyangHumanScaleLocalMapIds.LocalSpaceCount)
                throw new InvalidOperationException(
                    "Luoyang Cell traversal coverage is incomplete.");
            var facilityProfiles = plan.Profiles.Where(item =>
                !string.IsNullOrEmpty(item.FacilityId)).ToArray();
            if (facilityProfiles.Length != localMap.FacilityCapabilities.Count ||
                facilityProfiles.Select(item => item.FacilityId).Distinct(
                    StringComparer.Ordinal).Count() != facilityProfiles.Length)
                throw new InvalidOperationException(
                    "Luoyang Facility traversal coverage is incomplete.");
            foreach (var profile in facilityProfiles.Where(item =>
                         string.Equals(item.AccessRequirementId,
                             FacilityAccessRequirementIds.RoadRequired,
                             StringComparison.Ordinal) ||
                         string.Equals(item.AccessRequirementId,
                             FacilityAccessRequirementIds.VehicleRoadRequired,
                             StringComparison.Ordinal)))
                if (!profile.Ports.Any(item => item.Enabled))
                    throw new InvalidOperationException(
                        "Road-required Facility has no road Port: " +
                        profile.FacilityId);
        }
    }
}
