using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangFacilityInteractionNavigationIds
    {
        public const string ContractId =
            "presentation.luoyang.facility-selection-collision-navigation.v1";
        public const string TaskId =
            "LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1";
        public const string StatusId =
            "LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string TriggerCollisionProfileId =
            "presentation.collision.selection-trigger.v1";
        public const string RoadNodeProfileId =
            "navigation.luoyang.road-cell.v1";
        public const string PassageNodeProfileId =
            "navigation.luoyang.gate-or-bridge-passage.v1";
        public const string StrictRoadEdgeProfileId =
            "navigation.edge.road-cardinal-adjacency.v1";
        public const string ProvisionalConnectorEdgeProfileId =
            "navigation.edge.provisional-road-gap-connector.v1";
        public const string PassageConnectorEdgeProfileId =
            "navigation.edge.gate-or-bridge-to-road.v1";

        public const int SelectionProxyCount = 2084;
        public const int DenseResidentProxyCount = 549;
        public const int RoadNodeCount = 359;
        public const int PassageNodeCount = 20;
        public const int NavigationNodeCount = 379;
        public const int StrictRoadEdgeCount = 334;
        public const int RoadComponentCountBeforeConnectors = 29;
        public const int ProvisionalConnectorEdgeCount = 28;
        public const int PassageConnectorEdgeCount = 20;
        public const int NavigationEdgeCount = 382;
        public const float CellSizeMetres = 2000f;
        public const bool CreatesSimulationSubCells = false;
        public const bool ChangesSaveSchema = false;
    }

    [Serializable]
    public sealed class LuoyangFacilitySelectionProxy
    {
        public string ProxyId;
        public string FacilityId;
        public string FacilityDefinitionId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public float CenterLocalEastMetres;
        public float CenterLocalNorthMetres;
        public float HalfExtentEastMetres;
        public float HalfExtentNorthMetres;
        public float HeightMetres;
        public string CollisionProfileId;
        public bool IsTrigger;
        public bool IsSelectable;
    }

    [Serializable]
    public sealed class LuoyangRoadNavigationNode
    {
        public string NodeId;
        public string FacilityId;
        public string FacilityDefinitionId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
        public string NodeProfileId;
    }

    [Serializable]
    public sealed class LuoyangRoadNavigationEdge
    {
        public string EdgeId;
        public string FromNodeId;
        public string ToNodeId;
        public string EdgeProfileId;
        public float TraversalCostMetres;
        public bool Provisional;
    }

    public sealed class LuoyangFacilityInteractionNavigationPlan
    {
        public LuoyangFacilityInteractionNavigationPlan(
            IReadOnlyList<LuoyangFacilitySelectionProxy> selectionProxies,
            IReadOnlyDictionary<string, LuoyangFacilitySelectionProxy>
                selectionProxiesByFacilityId,
            IReadOnlyList<LuoyangRoadNavigationNode> navigationNodes,
            IReadOnlyDictionary<string, LuoyangRoadNavigationNode>
                navigationNodesByFacilityId,
            IReadOnlyList<LuoyangRoadNavigationEdge> navigationEdges,
            int roadComponentCountBeforeConnectors)
        {
            SelectionProxies = selectionProxies ?? throw new ArgumentNullException(
                nameof(selectionProxies));
            SelectionProxiesByFacilityId = selectionProxiesByFacilityId ??
                throw new ArgumentNullException(nameof(selectionProxiesByFacilityId));
            NavigationNodes = navigationNodes ?? throw new ArgumentNullException(
                nameof(navigationNodes));
            NavigationNodesByFacilityId = navigationNodesByFacilityId ??
                throw new ArgumentNullException(nameof(navigationNodesByFacilityId));
            NavigationEdges = navigationEdges ?? throw new ArgumentNullException(
                nameof(navigationEdges));
            RoadComponentCountBeforeConnectors = roadComponentCountBeforeConnectors;
        }

        public string ContractId => LuoyangFacilityInteractionNavigationIds.ContractId;
        public string TaskId => LuoyangFacilityInteractionNavigationIds.TaskId;
        public string StatusId => LuoyangFacilityInteractionNavigationIds.StatusId;
        public bool CreatesSimulationSubCells =>
            LuoyangFacilityInteractionNavigationIds.CreatesSimulationSubCells;
        public bool ChangesSaveSchema =>
            LuoyangFacilityInteractionNavigationIds.ChangesSaveSchema;
        public IReadOnlyList<LuoyangFacilitySelectionProxy> SelectionProxies { get; }
        public IReadOnlyDictionary<string, LuoyangFacilitySelectionProxy>
            SelectionProxiesByFacilityId { get; }
        public IReadOnlyList<LuoyangRoadNavigationNode> NavigationNodes { get; }
        public IReadOnlyDictionary<string, LuoyangRoadNavigationNode>
            NavigationNodesByFacilityId { get; }
        public IReadOnlyList<LuoyangRoadNavigationEdge> NavigationEdges { get; }
        public int RoadComponentCountBeforeConnectors { get; }
    }

    public static class LuoyangFacilityInteractionNavigationRules
    {
        private static readonly HashSet<string> PassageDefinitionIds =
            new HashSet<string>(new[]
            {
                "facility.fortification.city_gate",
                "facility.fortification.palace_gate",
                "facility.military.gate",
                "facility.public.bridge"
            }, StringComparer.Ordinal);

        public static LuoyangFacilityInteractionNavigationPlan CreatePlan(
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangWholeCityCompositionPlan composition)
        {
            if (wholeCity == null) throw new ArgumentNullException(nameof(wholeCity));
            if (composition == null)
                throw new ArgumentNullException(nameof(composition));
            if (wholeCity.Facilities.Count !=
                    LuoyangFacilityInteractionNavigationIds.SelectionProxyCount ||
                composition.Anchors.Count != wholeCity.Facilities.Count)
                throw new InvalidOperationException(
                    "Luoyang interaction plan requires the complete composed city.");

            var proxies = wholeCity.Facilities.Select(facility =>
            {
                if (!composition.AnchorsByFacilityId.TryGetValue(
                        facility.FacilityId, out var anchor))
                    throw new InvalidOperationException(
                        "Missing whole-city composition anchor: " +
                        facility.FacilityId);
                ResolveProxySize(facility.FacilityDefinitionId,
                    out var halfEast, out var halfNorth, out var height);
                return new LuoyangFacilitySelectionProxy
                {
                    ProxyId = "selection-proxy." + facility.FacilityId,
                    FacilityId = facility.FacilityId,
                    FacilityDefinitionId = facility.FacilityDefinitionId,
                    CellId64 = facility.CellId64,
                    GridColumn = facility.GridColumn,
                    GridRow = facility.GridRow,
                    CenterLocalEastMetres = anchor.VisualLocalEastMetres,
                    CenterLocalNorthMetres = anchor.VisualLocalNorthMetres,
                    HalfExtentEastMetres = halfEast,
                    HalfExtentNorthMetres = halfNorth,
                    HeightMetres = height,
                    CollisionProfileId =
                        LuoyangFacilityInteractionNavigationIds
                            .TriggerCollisionProfileId,
                    IsTrigger = true,
                    IsSelectable = true
                };
            }).OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            var proxiesByFacility = proxies.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);

            var nodes = wholeCity.Facilities.Where(item => IsRoad(item) ||
                    PassageDefinitionIds.Contains(item.FacilityDefinitionId))
                .Select(item => new LuoyangRoadNavigationNode
                {
                    NodeId = "navigation-node." + item.FacilityId,
                    FacilityId = item.FacilityId,
                    FacilityDefinitionId = item.FacilityDefinitionId,
                    CellId64 = item.CellId64,
                    GridColumn = item.GridColumn,
                    GridRow = item.GridRow,
                    NodeProfileId = IsRoad(item)
                        ? LuoyangFacilityInteractionNavigationIds.RoadNodeProfileId
                        : LuoyangFacilityInteractionNavigationIds
                            .PassageNodeProfileId
                }).OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            var nodesByFacility = nodes.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            var roads = nodes.Where(item => string.Equals(
                    item.FacilityDefinitionId, "facility.public.road",
                    StringComparison.Ordinal)).ToArray();
            var roadsByCell = roads.ToDictionary(item =>
                CellKey(item.GridRow, item.GridColumn));
            var edges = new List<LuoyangRoadNavigationEdge>();
            foreach (var road in roads)
            {
                AddStrictNeighbor(road, road.GridRow, road.GridColumn + 1,
                    roadsByCell, edges);
                AddStrictNeighbor(road, road.GridRow + 1, road.GridColumn,
                    roadsByCell, edges);
            }

            var components = RoadComponents(roads, edges);
            var componentCountBeforeConnectors = components.Count;
            while (components.Count > 1)
            {
                var bridge = ClosestComponentPair(components);
                edges.Add(CreateEdge(bridge.From, bridge.To,
                    LuoyangFacilityInteractionNavigationIds
                        .ProvisionalConnectorEdgeProfileId, true));
                var merged = components[bridge.FirstComponent]
                    .Concat(components[bridge.SecondComponent])
                    .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                    .ToList();
                components[bridge.FirstComponent] = merged;
                components.RemoveAt(bridge.SecondComponent);
            }

            foreach (var passage in nodes.Where(item => !string.Equals(
                         item.FacilityDefinitionId, "facility.public.road",
                         StringComparison.Ordinal)))
            {
                var nearest = roads.OrderBy(item => GridDistanceSquared(
                        passage, item))
                    .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                    .First();
                edges.Add(CreateEdge(passage, nearest,
                    LuoyangFacilityInteractionNavigationIds
                        .PassageConnectorEdgeProfileId, false));
            }

            var orderedEdges = edges.OrderBy(item => item.EdgeId,
                StringComparer.Ordinal).ToArray();
            var plan = new LuoyangFacilityInteractionNavigationPlan(proxies,
                proxiesByFacility, nodes, nodesByFacility, orderedEdges,
                componentCountBeforeConnectors);
            Validate(plan, wholeCity);
            return plan;
        }

        public static IReadOnlyList<string> FindFacilityPath(
            LuoyangFacilityInteractionNavigationPlan plan,
            string fromFacilityId, string toFacilityId)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.NavigationNodesByFacilityId.TryGetValue(fromFacilityId,
                    out var from) ||
                !plan.NavigationNodesByFacilityId.TryGetValue(toFacilityId,
                    out var to)) return Array.Empty<string>();
            if (string.Equals(from.NodeId, to.NodeId, StringComparison.Ordinal))
                return new[] { fromFacilityId };

            var adjacency = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                _ => new List<string>(), StringComparer.Ordinal);
            foreach (var edge in plan.NavigationEdges)
            {
                adjacency[edge.FromNodeId].Add(edge.ToNodeId);
                adjacency[edge.ToNodeId].Add(edge.FromNodeId);
            }
            foreach (var neighbors in adjacency.Values)
                neighbors.Sort(StringComparer.Ordinal);
            var queue = new Queue<string>();
            var previous = new Dictionary<string, string>(StringComparer.Ordinal);
            queue.Enqueue(from.NodeId);
            previous[from.NodeId] = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in adjacency[current])
                {
                    if (previous.ContainsKey(neighbor)) continue;
                    previous[neighbor] = current;
                    if (string.Equals(neighbor, to.NodeId,
                            StringComparison.Ordinal))
                    {
                        queue.Clear();
                        break;
                    }
                    queue.Enqueue(neighbor);
                }
            }
            if (!previous.ContainsKey(to.NodeId)) return Array.Empty<string>();
            var nodeById = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            var reversed = new List<string>();
            for (var cursor = to.NodeId; cursor != null;
                 cursor = previous[cursor])
                reversed.Add(nodeById[cursor].FacilityId);
            reversed.Reverse();
            return reversed;
        }

        public static void Validate(LuoyangFacilityInteractionNavigationPlan plan,
            LuoyangBuildingPerformancePlan wholeCity)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (wholeCity == null) throw new ArgumentNullException(nameof(wholeCity));
            if (plan.CreatesSimulationSubCells || plan.ChangesSaveSchema ||
                plan.SelectionProxies.Count !=
                    LuoyangFacilityInteractionNavigationIds.SelectionProxyCount ||
                plan.SelectionProxiesByFacilityId.Count !=
                    plan.SelectionProxies.Count ||
                plan.NavigationNodes.Count !=
                    LuoyangFacilityInteractionNavigationIds.NavigationNodeCount ||
                plan.NavigationNodesByFacilityId.Count !=
                    plan.NavigationNodes.Count ||
                plan.NavigationEdges.Count !=
                    LuoyangFacilityInteractionNavigationIds.NavigationEdgeCount ||
                plan.RoadComponentCountBeforeConnectors !=
                    LuoyangFacilityInteractionNavigationIds
                        .RoadComponentCountBeforeConnectors)
                throw new InvalidOperationException(
                    "Invalid Luoyang interaction/navigation plan totals.");
            var facilityIds = new HashSet<string>(wholeCity.Facilities.Select(
                item => item.FacilityId), StringComparer.Ordinal);
            if (plan.SelectionProxies.Any(item => item == null ||
                    !facilityIds.Contains(item.FacilityId) ||
                    string.IsNullOrWhiteSpace(item.ProxyId) ||
                    item.HalfExtentEastMetres <= 0f ||
                    item.HalfExtentEastMetres >= 1000f ||
                    item.HalfExtentNorthMetres <= 0f ||
                    item.HalfExtentNorthMetres >= 1000f ||
                    item.HeightMetres <= 0f || !item.IsTrigger ||
                    !item.IsSelectable ||
                    !string.Equals(item.CollisionProfileId,
                        LuoyangFacilityInteractionNavigationIds
                            .TriggerCollisionProfileId,
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Invalid Luoyang Facility selection proxy.");
            var nodeIds = new HashSet<string>(plan.NavigationNodes.Select(
                item => item.NodeId), StringComparer.Ordinal);
            if (plan.NavigationEdges.Any(item => item == null ||
                    !nodeIds.Contains(item.FromNodeId) ||
                    !nodeIds.Contains(item.ToNodeId) ||
                    string.Equals(item.FromNodeId, item.ToNodeId,
                        StringComparison.Ordinal) ||
                    item.TraversalCostMetres <= 0f) ||
                plan.NavigationEdges.Select(item => item.EdgeId).Distinct(
                    StringComparer.Ordinal).Count() != plan.NavigationEdges.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang road navigation edge.");
            if (plan.NavigationEdges.Count(item => string.Equals(
                    item.EdgeProfileId,
                    LuoyangFacilityInteractionNavigationIds.StrictRoadEdgeProfileId,
                    StringComparison.Ordinal)) !=
                LuoyangFacilityInteractionNavigationIds.StrictRoadEdgeCount ||
                plan.NavigationEdges.Count(item => item.Provisional) !=
                LuoyangFacilityInteractionNavigationIds
                    .ProvisionalConnectorEdgeCount ||
                plan.NavigationEdges.Count(item => string.Equals(
                    item.EdgeProfileId,
                    LuoyangFacilityInteractionNavigationIds
                        .PassageConnectorEdgeProfileId,
                    StringComparison.Ordinal)) !=
                LuoyangFacilityInteractionNavigationIds
                    .PassageConnectorEdgeCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang navigation edge profile totals.");
        }

        private static void ResolveProxySize(string definitionId,
            out float halfEast, out float halfNorth, out float height)
        {
            if (string.Equals(definitionId, "facility.public.road",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId, "facility.public.canal",
                    StringComparison.Ordinal))
            {
                halfEast = 820f;
                halfNorth = 280f;
                height = 120f;
                return;
            }
            if (definitionId.IndexOf("wall", StringComparison.Ordinal) >= 0)
            {
                halfEast = 850f;
                halfNorth = 240f;
                height = 360f;
                return;
            }
            if (PassageDefinitionIds.Contains(definitionId))
            {
                halfEast = 720f;
                halfNorth = 620f;
                height = 420f;
                return;
            }
            if (definitionId.IndexOf("field", StringComparison.Ordinal) >= 0 ||
                definitionId.IndexOf("pasture", StringComparison.Ordinal) >= 0)
            {
                halfEast = 780f;
                halfNorth = 780f;
                height = 100f;
                return;
            }
            halfEast = 650f;
            halfNorth = 650f;
            height = 300f;
        }

        private static bool IsRoad(LuoyangBuildingPerformanceFacility item) =>
            string.Equals(item.FacilityDefinitionId, "facility.public.road",
                StringComparison.Ordinal);

        private static void AddStrictNeighbor(LuoyangRoadNavigationNode from,
            int row, int column,
            IReadOnlyDictionary<long, LuoyangRoadNavigationNode> roadsByCell,
            ICollection<LuoyangRoadNavigationEdge> edges)
        {
            if (!roadsByCell.TryGetValue(CellKey(row, column), out var to)) return;
            edges.Add(CreateEdge(from, to,
                LuoyangFacilityInteractionNavigationIds.StrictRoadEdgeProfileId,
                false));
        }

        private static LuoyangRoadNavigationEdge CreateEdge(
            LuoyangRoadNavigationNode first, LuoyangRoadNavigationNode second,
            string profileId, bool provisional)
        {
            var from = string.CompareOrdinal(first.NodeId, second.NodeId) <= 0
                ? first : second;
            var to = ReferenceEquals(from, first) ? second : first;
            var row = first.GridRow - second.GridRow;
            var column = first.GridColumn - second.GridColumn;
            return new LuoyangRoadNavigationEdge
            {
                EdgeId = "navigation-edge." + profileId + "." + from.FacilityId +
                         ".to." + to.FacilityId,
                FromNodeId = from.NodeId,
                ToNodeId = to.NodeId,
                EdgeProfileId = profileId,
                TraversalCostMetres = (float)(Math.Sqrt(row * row +
                    column * column) *
                    LuoyangFacilityInteractionNavigationIds.CellSizeMetres),
                Provisional = provisional
            };
        }

        private static List<List<LuoyangRoadNavigationNode>> RoadComponents(
            IReadOnlyList<LuoyangRoadNavigationNode> roads,
            IReadOnlyList<LuoyangRoadNavigationEdge> edges)
        {
            var byId = roads.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            var adjacency = roads.ToDictionary(item => item.NodeId,
                _ => new List<string>(), StringComparer.Ordinal);
            foreach (var edge in edges)
            {
                adjacency[edge.FromNodeId].Add(edge.ToNodeId);
                adjacency[edge.ToNodeId].Add(edge.FromNodeId);
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<List<LuoyangRoadNavigationNode>>();
            foreach (var road in roads.OrderBy(item => item.FacilityId,
                         StringComparer.Ordinal))
            {
                if (!seen.Add(road.NodeId)) continue;
                var component = new List<LuoyangRoadNavigationNode>();
                var queue = new Queue<string>();
                queue.Enqueue(road.NodeId);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(byId[current]);
                    foreach (var next in adjacency[current].OrderBy(item => item,
                                 StringComparer.Ordinal))
                        if (seen.Add(next)) queue.Enqueue(next);
                }
                result.Add(component.OrderBy(item => item.FacilityId,
                    StringComparer.Ordinal).ToList());
            }
            return result.OrderBy(item => item[0].FacilityId,
                StringComparer.Ordinal).ToList();
        }

        private static ComponentBridge ClosestComponentPair(
            IReadOnlyList<List<LuoyangRoadNavigationNode>> components)
        {
            ComponentBridge best = null;
            for (var first = 0; first < components.Count - 1; first++)
            for (var second = first + 1; second < components.Count; second++)
            foreach (var from in components[first])
            foreach (var to in components[second])
            {
                var candidate = new ComponentBridge(first, second, from, to,
                    GridDistanceSquared(from, to));
                if (best == null || candidate.CompareTo(best) < 0)
                    best = candidate;
            }
            return best ?? throw new InvalidOperationException(
                "Cannot connect empty Luoyang road components.");
        }

        private static int GridDistanceSquared(LuoyangRoadNavigationNode first,
            LuoyangRoadNavigationNode second)
        {
            var row = first.GridRow - second.GridRow;
            var column = first.GridColumn - second.GridColumn;
            return row * row + column * column;
        }

        private static long CellKey(int row, int column) =>
            ((long)row << 32) ^ (uint)column;

        private sealed class ComponentBridge : IComparable<ComponentBridge>
        {
            public ComponentBridge(int firstComponent, int secondComponent,
                LuoyangRoadNavigationNode from, LuoyangRoadNavigationNode to,
                int distanceSquared)
            {
                FirstComponent = firstComponent;
                SecondComponent = secondComponent;
                From = from;
                To = to;
                DistanceSquared = distanceSquared;
            }

            public int FirstComponent { get; }
            public int SecondComponent { get; }
            public LuoyangRoadNavigationNode From { get; }
            public LuoyangRoadNavigationNode To { get; }
            public int DistanceSquared { get; }

            public int CompareTo(ComponentBridge other)
            {
                var distance = DistanceSquared.CompareTo(other.DistanceSquared);
                if (distance != 0) return distance;
                var from = string.CompareOrdinal(From.FacilityId,
                    other.From.FacilityId);
                return from != 0 ? from : string.CompareOrdinal(To.FacilityId,
                    other.To.FacilityId);
            }
        }
    }
}
