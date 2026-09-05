using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;

namespace Mandate.Presentation
{
    public enum CountyMapPresentationLod : byte
    {
        Far = 0,
        Mid = 1,
        Near = 2
    }

    public enum CountyRoadPresentationClass : byte
    {
        StrategicR0 = 0,
        CountyMainR1 = 1,
        UrbanMainR2 = 2,
        LocalR3 = 3
    }

    public enum CountyFacilityPresentationKind : byte
    {
        Major = 0,
        AggregateRepresentative = 1,
        Detail = 2
    }

    public enum CountyMapPresentationLayerId : byte
    {
        Terrain,
        Water,
        UrbanArea,
        Road,
        Fortification,
        FacilityAggregate,
        FacilityDetail,
        PlanningGrid,
        Draft,
        Selection,
        Debug
    }

    public enum StrategicRoadPresentationMode : byte
    {
        DefaultBackbone,
        TransportOverlay
    }

    public sealed class CountyMapPresentationLayer
    {
        public CountyMapPresentationLayer(
            CountyMapPresentationLayerId id,
            CountyMapPresentationLod minimumDetail,
            CountyMapPresentationLod maximumDetail,
            int priority, string styleId, bool debugOnly = false)
        {
            Id = id;
            MinimumDetail = minimumDetail;
            MaximumDetail = maximumDetail;
            Priority = priority;
            StyleId = styleId ?? string.Empty;
            DebugOnly = debugOnly;
        }

        public CountyMapPresentationLayerId Id { get; }
        public CountyMapPresentationLod MinimumDetail { get; }
        public CountyMapPresentationLod MaximumDetail { get; }
        public int Priority { get; }
        public string StyleId { get; }
        public bool DebugOnly { get; }

        public bool Supports(CountyMapPresentationLod detail) =>
            detail >= MinimumDetail && detail <= MaximumDetail;
    }

    public readonly struct CountyMapViewport
    {
        public CountyMapViewport(float minimumRow, float minimumColumn,
            float rows, float columns, float marginCells = 0f)
        {
            MinimumRow = minimumRow - marginCells;
            MinimumColumn = minimumColumn - marginCells;
            MaximumRow = minimumRow + rows + marginCells;
            MaximumColumn = minimumColumn + columns + marginCells;
        }

        public float MinimumRow { get; }
        public float MinimumColumn { get; }
        public float MaximumRow { get; }
        public float MaximumColumn { get; }

        public bool Contains(int row, int column) =>
            row >= MinimumRow && row <= MaximumRow &&
            column >= MinimumColumn && column <= MaximumColumn;

        public bool Intersects(int minimumRow, int maximumRow,
            int minimumColumn, int maximumColumn) =>
            maximumRow >= MinimumRow && minimumRow <= MaximumRow &&
            maximumColumn >= MinimumColumn &&
            minimumColumn <= MaximumColumn;
    }

    public sealed class CountyMapPresentationLodController
    {
        // These are camera presentation thresholds, not world scale facts.
        // Separate enter/leave values prevent repeated switching at a border.
        public const float FarToMidRows = 200f;
        public const float MidToFarRows = 240f;
        public const float MidToNearRows = 56f;
        public const float NearToMidRows = 72f;

        public CountyMapPresentationLod Current { get; private set; } =
            CountyMapPresentationLod.Near;

        public bool Reset(float visibleRows)
        {
            var resolved = visibleRows >= MidToFarRows
                ? CountyMapPresentationLod.Far
                : visibleRows <= MidToNearRows
                    ? CountyMapPresentationLod.Near
                    : CountyMapPresentationLod.Mid;
            var changed = Current != resolved;
            Current = resolved;
            return changed;
        }

        public bool Update(float visibleRows)
        {
            var next = Current;
            switch (Current)
            {
                case CountyMapPresentationLod.Far:
                    if (visibleRows < FarToMidRows)
                        next = CountyMapPresentationLod.Mid;
                    break;
                case CountyMapPresentationLod.Mid:
                    if (visibleRows > MidToFarRows)
                        next = CountyMapPresentationLod.Far;
                    else if (visibleRows < MidToNearRows)
                        next = CountyMapPresentationLod.Near;
                    break;
                case CountyMapPresentationLod.Near:
                    if (visibleRows > NearToMidRows)
                        next = CountyMapPresentationLod.Mid;
                    break;
            }
            if (next == Current) return false;
            Current = next;
            return true;
        }
    }

    public sealed class CountyRoadPresentationSegment
    {
        public CountyRoadPresentationSegment(Luoyang50mLayoutEdge edge,
            CountyRoadPresentationClass presentationClass)
        {
            Edge = edge ?? throw new ArgumentNullException(nameof(edge));
            PresentationClass = presentationClass;
        }

        public Luoyang50mLayoutEdge Edge { get; }
        public CountyRoadPresentationClass PresentationClass { get; }

        public bool IsVisible(CountyMapViewport viewport) =>
            viewport.Intersects(
                Math.Min(Edge.FromLocalRow, Edge.ToLocalRow),
                Math.Max(Edge.FromLocalRow, Edge.ToLocalRow),
                Math.Min(Edge.FromLocalColumn, Edge.ToLocalColumn),
                Math.Max(Edge.FromLocalColumn, Edge.ToLocalColumn));
    }

    public sealed class CountyFacilityPresentationItem
    {
        public CountyFacilityPresentationItem(
            Luoyang50mLayoutFacility facility,
            CountyFacilityPresentationKind kind)
        {
            Facility = facility ?? throw new ArgumentNullException(
                nameof(facility));
            Kind = kind;
        }

        public Luoyang50mLayoutFacility Facility { get; }
        public CountyFacilityPresentationKind Kind { get; }
    }

    public sealed class CountyMapPresentationSnapshot
    {
        public CountyMapPresentationLod DetailLevel { get; set; }
        public int VisibleRoadSegments { get; set; }
        public int VisibleFacilities { get; set; }
        public int VisibleFortificationSegments { get; set; }
        public int VisibleGates { get; set; }
        public int VisibleGridColumns { get; set; }
        public int VisibleGridRows { get; set; }
        public int VisibleLayerCount { get; set; }
    }

    public static class StrategicRoadPresentationPolicy
    {
        public static bool Includes(string routeId, int cellCount,
            StrategicRoadPresentationMode mode)
        {
            if (string.IsNullOrWhiteSpace(routeId) || cellCount < 2)
                return false;
            if (mode == StrategicRoadPresentationMode.TransportOverlay)
                return true;
            if (!routeId.StartsWith("geo.route.",
                    StringComparison.Ordinal)) return false;
            return routeId.IndexOf("luoyang", StringComparison.Ordinal) >= 0 ||
                   cellCount >= 95;
        }
    }

    /// <summary>
    /// Read-only presentation index over the authoritative Luoyang layout.
    /// It never removes or rewrites roads, facilities or fortifications; it
    /// only selects the subset that is useful at the current camera scale.
    /// </summary>
    public sealed class CountyMapPresentationStack
    {
        private const int DensityBucketCells = 8;
        private readonly Luoyang50mCountyLayoutPackage _layout;
        private readonly CountySpatialPartition _partition;
        private readonly IReadOnlyList<CountyRoadPresentationSegment> _roads;
        private readonly IReadOnlyList<CountyFacilityPresentationItem>
            _farFacilities;
        private readonly IReadOnlyList<CountyFacilityPresentationItem>
            _midFacilities;
        private readonly IReadOnlyList<CountyFacilityPresentationItem>
            _nearFacilities;
        private readonly byte[] _roadClasses;
        private readonly byte[] _urbanDensity;
        private readonly byte[] _agriculturalDensity;
        private readonly sbyte[] _districtWash;
        private readonly IReadOnlyList<IReadOnlyList<PlanningCellCoord>>
            _farFortificationOutlines;

        public CountyMapPresentationStack(
            Luoyang50mCountyLayoutPackage layout,
            CountySpatialPartition partition)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _partition = partition ?? throw new ArgumentNullException(
                nameof(partition));
            if (_layout.Rows != _partition.Rows ||
                _layout.Columns != _partition.Columns ||
                !string.Equals(_layout.CountyId, _partition.CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "County presentation inputs do not describe one space.");

            Layers = BuildLayers();
            _roads = BuildRoadPresentation();
            _roadClasses = BuildRoadCellClasses(_roads);
            BuildFacilityPresentation(out _farFacilities,
                out _midFacilities, out _nearFacilities);
            BuildDensity(out _urbanDensity, out _agriculturalDensity);
            _districtWash = BuildDistrictWash();
            _farFortificationOutlines = BuildFortificationOutlines();
        }

        public IReadOnlyList<CountyMapPresentationLayer> Layers { get; }
        public IReadOnlyList<CountyRoadPresentationSegment> Roads => _roads;
        public IReadOnlyList<CountyFacilityPresentationItem> FarFacilities =>
            _farFacilities;
        public IReadOnlyList<CountyFacilityPresentationItem> MidFacilities =>
            _midFacilities;
        public IReadOnlyList<CountyFacilityPresentationItem> NearFacilities =>
            _nearFacilities;
        public IReadOnlyList<IReadOnlyList<PlanningCellCoord>>
            FarFortificationOutlines => _farFortificationOutlines;

        public CountyRoadPresentationClass RoadClassAt(int row, int column)
        {
            ValidateCell(row, column);
            return (CountyRoadPresentationClass)_roadClasses[
                row * _partition.Columns + column];
        }

        public byte UrbanDensityAt(int row, int column)
        {
            ValidateCell(row, column);
            return _urbanDensity[row * _partition.Columns + column];
        }

        public byte AgriculturalDensityAt(int row, int column)
        {
            ValidateCell(row, column);
            return _agriculturalDensity[row * _partition.Columns + column];
        }

        public int DistrictWashAt(int row, int column)
        {
            ValidateCell(row, column);
            return _districtWash[row * _partition.Columns + column];
        }

        public IReadOnlyList<CountyRoadPresentationSegment> VisibleRoads(
            CountyMapPresentationLod detail, CountyMapViewport viewport)
        {
            var maximum = detail == CountyMapPresentationLod.Far
                ? CountyRoadPresentationClass.CountyMainR1
                : detail == CountyMapPresentationLod.Mid
                    ? CountyRoadPresentationClass.UrbanMainR2
                    : CountyRoadPresentationClass.LocalR3;
            return _roads.Where(item =>
                    item.PresentationClass <= maximum &&
                    item.IsVisible(viewport))
                .ToArray();
        }

        public IReadOnlyList<CountyFacilityPresentationItem>
            VisibleFacilities(CountyMapPresentationLod detail,
                CountyMapViewport viewport)
        {
            var source = detail == CountyMapPresentationLod.Far
                ? _farFacilities
                : detail == CountyMapPresentationLod.Mid
                    ? _midFacilities : _nearFacilities;
            return source.Where(item => viewport.Contains(
                    item.Facility.LocalRow, item.Facility.LocalColumn))
                .ToArray();
        }

        public IReadOnlyList<Luoyang50mLayoutFortification>
            VisibleFortifications(CountyMapPresentationLod detail,
                CountyMapViewport viewport)
        {
            if (detail == CountyMapPresentationLod.Far)
                return Array.Empty<Luoyang50mLayoutFortification>();
            return _layout.Fortifications.Where(item => viewport.Contains(
                    item.LocalRow, item.LocalColumn))
                .OrderBy(item => item.EdgeId, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<Luoyang50mLayoutFortification> VisibleGates(
            CountyMapViewport viewport) => _layout.Fortifications
            .Where(item => item.IsGate && viewport.Contains(
                item.LocalRow, item.LocalColumn))
            .OrderBy(item => item.EdgeId, StringComparer.Ordinal)
            .ToArray();

        public bool IsLayerVisible(CountyMapPresentationLayerId layerId,
            CountyMapPresentationLod detail, CountySubViewMode subView,
            PlanningMapOverlayState overlays, bool debug = false)
        {
            var layer = Layers.Single(item => item.Id == layerId);
            if (!layer.Supports(detail) || layer.DebugOnly && !debug)
                return false;
            if (overlays == null) return true;
            switch (layerId)
            {
                case CountyMapPresentationLayerId.Water:
                    return overlays.RiversVisible;
                case CountyMapPresentationLayerId.Road:
                    return overlays.RoadsVisible;
                case CountyMapPresentationLayerId.Fortification:
                    return overlays.FortificationsVisible;
                case CountyMapPresentationLayerId.PlanningGrid:
                    return subView == CountySubViewMode.Planning &&
                           overlays.GridVisible &&
                           detail == CountyMapPresentationLod.Near;
                case CountyMapPresentationLayerId.Draft:
                    return subView == CountySubViewMode.Planning &&
                           overlays.PlanningVisible;
                default:
                    return true;
            }
        }

        public CountyMapPresentationSnapshot Snapshot(
            CountyMapPresentationLod detail, CountyMapViewport viewport,
            CountySubViewMode subView, PlanningMapOverlayState overlays)
        {
            var roads = IsLayerVisible(CountyMapPresentationLayerId.Road,
                detail, subView, overlays)
                ? VisibleRoads(detail, viewport).Count : 0;
            var facilities = IsLayerVisible(
                detail == CountyMapPresentationLod.Near
                    ? CountyMapPresentationLayerId.FacilityDetail
                    : CountyMapPresentationLayerId.FacilityAggregate,
                detail, subView, overlays)
                ? VisibleFacilities(detail, viewport).Count : 0;
            var fortifications = IsLayerVisible(
                CountyMapPresentationLayerId.Fortification, detail, subView,
                overlays)
                ? detail == CountyMapPresentationLod.Far
                    ? _farFortificationOutlines.Sum(item => item.Count)
                    : VisibleFortifications(detail, viewport).Count
                : 0;
            var gates = IsLayerVisible(
                CountyMapPresentationLayerId.Fortification, detail, subView,
                overlays) ? VisibleGates(viewport).Count : 0;
            var grid = IsLayerVisible(
                CountyMapPresentationLayerId.PlanningGrid, detail, subView,
                overlays);
            return new CountyMapPresentationSnapshot
            {
                DetailLevel = detail,
                VisibleRoadSegments = roads,
                VisibleFacilities = facilities,
                VisibleFortificationSegments = fortifications,
                VisibleGates = gates,
                VisibleGridRows = grid ? Math.Max(0,
                    (int)Math.Ceiling(viewport.MaximumRow -
                                      viewport.MinimumRow)) : 0,
                VisibleGridColumns = grid ? Math.Max(0,
                    (int)Math.Ceiling(viewport.MaximumColumn -
                                      viewport.MinimumColumn)) : 0,
                VisibleLayerCount = Layers.Count(item => IsLayerVisible(
                    item.Id, detail, subView, overlays))
            };
        }

        private IReadOnlyList<CountyRoadPresentationSegment>
            BuildRoadPresentation()
        {
            var adjacency = _layout.RoadNodes.ToDictionary(
                item => item.NodeId,
                item => new List<Tuple<string, string>>(),
                StringComparer.Ordinal);
            foreach (var edge in _layout.RoadEdges)
            {
                adjacency[edge.FromNodeId].Add(Tuple.Create(
                    edge.ToNodeId, edge.EdgeId));
                adjacency[edge.ToNodeId].Add(Tuple.Create(
                    edge.FromNodeId, edge.EdgeId));
            }
            foreach (var list in adjacency.Values)
                list.Sort((first, second) =>
                {
                    var result = string.CompareOrdinal(first.Item2,
                        second.Item2);
                    return result != 0 ? result : string.CompareOrdinal(
                        first.Item1, second.Item1);
                });

            var nodeByFacility = _layout.RoadNodes.ToDictionary(
                item => item.FacilityId, item => item.NodeId,
                StringComparer.Ordinal);
            var nodeById = _layout.RoadNodes.ToDictionary(
                item => item.NodeId, StringComparer.Ordinal);
            var backbone = new HashSet<string>(StringComparer.Ordinal);
            var portalReachable = new HashSet<string>(StringComparer.Ordinal);
            var urbanRow = (_layout.UrbanAreaCandidate.MinimumRow +
                            _layout.UrbanAreaCandidate.MaximumRow) * 0.5d;
            var urbanColumn = (_layout.UrbanAreaCandidate.MinimumColumn +
                               _layout.UrbanAreaCandidate.MaximumColumn) *
                              0.5d;
            foreach (var portal in _layout.Portals.OrderBy(
                         item => item.PortalId, StringComparer.Ordinal))
            {
                if (!nodeByFacility.TryGetValue(portal.AnchorFacilityId,
                        out var seed)) continue;
                var queue = new Queue<string>();
                var predecessor = new Dictionary<string, Tuple<string,
                    string>>(StringComparer.Ordinal);
                var visited = new HashSet<string>(StringComparer.Ordinal)
                    { seed };
                queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var next in adjacency[current])
                    {
                        if (!visited.Add(next.Item1)) continue;
                        predecessor[next.Item1] = Tuple.Create(current,
                            next.Item2);
                        queue.Enqueue(next.Item1);
                    }
                }
                portalReachable.UnionWith(visited);
                var target = visited.OrderBy(nodeId =>
                    {
                        var node = nodeById[nodeId];
                        var row = node.LocalRow - urbanRow;
                        var column = node.LocalColumn - urbanColumn;
                        return row * row + column * column;
                    }).ThenBy(nodeId => nodeId, StringComparer.Ordinal)
                    .First();
                while (!string.Equals(target, seed,
                           StringComparison.Ordinal) &&
                       predecessor.TryGetValue(target, out var previous))
                {
                    backbone.Add(previous.Item2);
                    target = previous.Item1;
                }
            }

            return _layout.RoadEdges.OrderBy(item => item.EdgeId,
                    StringComparer.Ordinal)
                .Select(edge =>
                {
                    CountyRoadPresentationClass classification;
                    if (backbone.Contains(edge.EdgeId))
                        classification = CountyRoadPresentationClass
                            .StrategicR0;
                    else
                    {
                        var degree = Math.Max(
                            adjacency[edge.FromNodeId].Count,
                            adjacency[edge.ToNodeId].Count);
                        var span = Math.Abs(edge.FromLocalRow -
                                            edge.ToLocalRow) +
                                   Math.Abs(edge.FromLocalColumn -
                                            edge.ToLocalColumn);
                        if (portalReachable.Contains(edge.FromNodeId) &&
                            (degree >= 3 || span >= 7))
                            classification = CountyRoadPresentationClass
                                .CountyMainR1;
                        else if (portalReachable.Contains(edge.FromNodeId) ||
                                 portalReachable.Contains(edge.ToNodeId) ||
                                 degree >= 3)
                            classification = CountyRoadPresentationClass
                                .UrbanMainR2;
                        else classification = CountyRoadPresentationClass
                            .LocalR3;
                    }
                    return new CountyRoadPresentationSegment(edge,
                        classification);
                }).ToArray();
        }

        private byte[] BuildRoadCellClasses(
            IReadOnlyList<CountyRoadPresentationSegment> roads)
        {
            var values = Enumerable.Repeat(
                    (byte)CountyRoadPresentationClass.LocalR3,
                    _partition.PlanningCellCount).ToArray();
            var touched = new bool[_partition.PlanningCellCount];
            foreach (var road in roads)
            {
                var edge = road.Edge;
                var row = edge.FromLocalRow;
                var column = edge.FromLocalColumn;
                while (true)
                {
                    Set(row, column, road.PresentationClass);
                    if (row == edge.ToLocalRow &&
                        column == edge.ToLocalColumn) break;
                    if (row != edge.ToLocalRow)
                        row += Math.Sign(edge.ToLocalRow - row);
                    else column += Math.Sign(edge.ToLocalColumn - column);
                }
            }
            foreach (var portal in _layout.Portals)
                Set(portal.LocalRow, portal.LocalColumn,
                    CountyRoadPresentationClass.StrategicR0);
            foreach (var node in _layout.RoadNodes)
                if (!touched[Index(node.LocalRow, node.LocalColumn)])
                    Set(node.LocalRow, node.LocalColumn,
                        CountyRoadPresentationClass.LocalR3);

            // Road cells inherited from the published 2 km parent map are
            // not local layout nodes. They remain the county's strategic
            // skeleton and therefore receive R0 presentation importance.
            for (var row = 0; row < _partition.Rows; row++)
            for (var column = 0; column < _partition.Columns; column++)
            {
                var index = Index(row, column);
                if (_partition.LandUse(row, column) ==
                        PlanningLandUseClass.Road && !touched[index])
                    values[index] =
                        (byte)CountyRoadPresentationClass.StrategicR0;
            }
            return values;

            void Set(int row, int column,
                CountyRoadPresentationClass classification)
            {
                var index = Index(row, column);
                if (!touched[index] || (byte)classification < values[index])
                    values[index] = (byte)classification;
                touched[index] = true;
            }
        }

        private void BuildFacilityPresentation(
            out IReadOnlyList<CountyFacilityPresentationItem> far,
            out IReadOnlyList<CountyFacilityPresentationItem> mid,
            out IReadOnlyList<CountyFacilityPresentationItem> near)
        {
            var renderable = _layout.Facilities.Where(IsRenderableFacility)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToArray();
            var major = new HashSet<string>(renderable.Where(IsMajorFacility)
                .Select(item => item.FacilityId), StringComparer.Ordinal);
            far = renderable.Where(item => major.Contains(item.FacilityId))
                .Select(item => new CountyFacilityPresentationItem(item,
                    CountyFacilityPresentationKind.Major)).ToArray();

            var representatives = renderable.GroupBy(item =>
                    item.LocalRow / DensityBucketCells + ":" +
                    item.LocalColumn / DensityBucketCells,
                    StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(item =>
                        major.Contains(item.FacilityId))
                    .ThenByDescending(item => (long)item.WidthCentimetres *
                                              item.DepthCentimetres)
                    .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                    .First())
                .ToDictionary(item => item.FacilityId,
                    StringComparer.Ordinal);
            foreach (var item in renderable.Where(item =>
                         major.Contains(item.FacilityId)))
                representatives[item.FacilityId] = item;
            mid = representatives.Values.OrderBy(item => item.FacilityId,
                    StringComparer.Ordinal)
                .Select(item => new CountyFacilityPresentationItem(item,
                    major.Contains(item.FacilityId)
                        ? CountyFacilityPresentationKind.Major
                        : CountyFacilityPresentationKind
                            .AggregateRepresentative))
                .ToArray();
            near = renderable.Select(item =>
                    new CountyFacilityPresentationItem(item,
                        CountyFacilityPresentationKind.Detail))
                .ToArray();
        }

        private void BuildDensity(out byte[] urban, out byte[] agriculture)
        {
            var bucketRows = (_partition.Rows + DensityBucketCells - 1) /
                             DensityBucketCells;
            var bucketColumns = (_partition.Columns + DensityBucketCells - 1) /
                                DensityBucketCells;
            var urbanBuckets = new int[bucketRows * bucketColumns];
            var agricultureBuckets = new int[bucketRows * bucketColumns];
            foreach (var facility in _layout.Facilities)
            {
                var bucket = facility.LocalRow / DensityBucketCells *
                             bucketColumns +
                             facility.LocalColumn / DensityBucketCells;
                if (IsAgriculturalFacility(facility))
                    agricultureBuckets[bucket]++;
                else if (IsRenderableFacility(facility))
                    urbanBuckets[bucket]++;
            }
            urban = ExpandDensity(SmoothDensity(urbanBuckets, bucketRows,
                bucketColumns), bucketRows, bucketColumns);
            agriculture = ExpandDensity(SmoothDensity(agricultureBuckets,
                bucketRows, bucketColumns), bucketRows, bucketColumns);
        }

        private int[] SmoothDensity(int[] source, int rows, int columns)
        {
            var result = new int[source.Length];
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var value = 0;
                for (var dr = -1; dr <= 1; dr++)
                for (var dc = -1; dc <= 1; dc++)
                {
                    var r = row + dr;
                    var c = column + dc;
                    if (r < 0 || r >= rows || c < 0 || c >= columns)
                        continue;
                    var weight = dr == 0 && dc == 0 ? 4 :
                        dr == 0 || dc == 0 ? 2 : 1;
                    value += source[r * columns + c] * weight;
                }
                result[row * columns + column] = value;
            }
            return result;
        }

        private byte[] ExpandDensity(int[] buckets, int rows, int columns)
        {
            var maximum = Math.Max(1, buckets.Max());
            var result = new byte[_partition.PlanningCellCount];
            for (var row = 0; row < _partition.Rows; row++)
            for (var column = 0; column < _partition.Columns; column++)
            {
                var value = buckets[row / DensityBucketCells * columns +
                                    column / DensityBucketCells];
                result[Index(row, column)] = (byte)Math.Min(255,
                    value * 255 / maximum);
            }
            return result;
        }

        private sbyte[] BuildDistrictWash()
        {
            var result = Enumerable.Repeat((sbyte)-1,
                _partition.PlanningCellCount).ToArray();
            var ordered = _layout.DistrictAreas.Select((area, index) =>
                    new { Area = area, Index = index })
                .OrderByDescending(item =>
                    (item.Area.MaximumRow - item.Area.MinimumRow + 1L) *
                    (item.Area.MaximumColumn -
                     item.Area.MinimumColumn + 1L))
                .ThenBy(item => item.Area.DistrictId, StringComparer.Ordinal);
            foreach (var item in ordered)
            {
                var area = item.Area;
                for (var row = area.MinimumRow; row <= area.MaximumRow;
                     row++)
                for (var column = area.MinimumColumn;
                     column <= area.MaximumColumn; column++)
                    if (InsidePolygon(row + 0.5d, column + 0.5d,
                            area.HullCells))
                        result[Index(row, column)] = (sbyte)item.Index;
            }
            return result;
        }

        private IReadOnlyList<IReadOnlyList<PlanningCellCoord>>
            BuildFortificationOutlines()
        {
            return _layout.Fortifications.GroupBy(item =>
                    item.DefinitionId.IndexOf("palace",
                        StringComparison.Ordinal) >= 0
                        ? "palace" : "city", StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => (IReadOnlyList<PlanningCellCoord>)
                    ConvexHull(group.Select(item => new PlanningCellCoord(
                        item.LocalRow, item.LocalColumn))))
                .Where(hull => hull.Count >= 3).ToArray();
        }

        private static IReadOnlyList<PlanningCellCoord> ConvexHull(
            IEnumerable<PlanningCellCoord> source)
        {
            var points = source.Distinct().OrderBy(item => item.Column)
                .ThenBy(item => item.Row).ToArray();
            if (points.Length <= 2) return points;
            var hull = new List<PlanningCellCoord>();
            foreach (var point in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2],
                           hull[hull.Count - 1], point) <= 0) hull.RemoveAt(
                    hull.Count - 1);
                hull.Add(point);
            }
            var lower = hull.Count;
            for (var index = points.Length - 2; index >= 0; index--)
            {
                var point = points[index];
                while (hull.Count > lower && Cross(hull[hull.Count - 2],
                           hull[hull.Count - 1], point) <= 0) hull.RemoveAt(
                    hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static long Cross(PlanningCellCoord first,
            PlanningCellCoord second, PlanningCellCoord third) =>
            (long)(second.Column - first.Column) *
            (third.Row - first.Row) -
            (long)(second.Row - first.Row) *
            (third.Column - first.Column);

        private static bool InsidePolygon(double row, double column,
            IReadOnlyList<PlanningCellCoord> polygon)
        {
            var inside = false;
            for (int current = 0, previous = polygon.Count - 1;
                 current < polygon.Count; previous = current++)
            {
                var a = polygon[current];
                var b = polygon[previous];
                if ((a.Row > row) == (b.Row > row)) continue;
                var crossing = (b.Column - a.Column) *
                               (row - a.Row) /
                               (double)(b.Row - a.Row) + a.Column;
                if (column < crossing) inside = !inside;
            }
            return inside;
        }

        private static bool IsRenderableFacility(
            Luoyang50mLayoutFacility facility) =>
            !facility.DefinitionId.StartsWith("facility.public.road",
                StringComparison.Ordinal) &&
            !facility.DefinitionId.StartsWith("facility.public.canal",
                StringComparison.Ordinal) &&
            !facility.DefinitionId.StartsWith("facility.fortification.",
                StringComparison.Ordinal) &&
            !IsAgriculturalFacility(facility);

        private static bool IsAgriculturalFacility(
            Luoyang50mLayoutFacility facility) =>
            facility.CategoryId == "agriculture" ||
            facility.CategoryId == "resource_agriculture" ||
            facility.DefinitionId.StartsWith("facility.agriculture.",
                StringComparison.Ordinal);

        private static bool IsMajorFacility(
            Luoyang50mLayoutFacility facility)
        {
            var id = facility.DefinitionId;
            var area = (long)facility.WidthCentimetres *
                       facility.DepthCentimetres;
            return area >= 40_000_000L ||
                   facility.HeightCentimetres >= 1_500 ||
                   facility.CategoryId == "government" ||
                   facility.CategoryId == "ritual" ||
                   facility.CategoryId == "military" ||
                   id.IndexOf("market", StringComparison.Ordinal) >= 0 ||
                   id.IndexOf("granary", StringComparison.Ordinal) >= 0 ||
                   id.IndexOf("warehouse", StringComparison.Ordinal) >= 0 ||
                   id.IndexOf("palace", StringComparison.Ordinal) >= 0;
        }

        private static IReadOnlyList<CountyMapPresentationLayer>
            BuildLayers() => new[]
            {
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Terrain,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 0,
                    "county.terrain.foundation.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Water,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 10,
                    "county.water.blue-muted.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.UrbanArea,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 20,
                    "county.urban-fabric-density.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Road,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 30,
                    "county.road.earth-screen-width.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Fortification,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 40,
                    "county.fortification.band.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.FacilityAggregate,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Mid, 50,
                    "county.facility.aggregate.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.FacilityDetail,
                    CountyMapPresentationLod.Near,
                    CountyMapPresentationLod.Near, 60,
                    "county.facility.footprint.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.PlanningGrid,
                    CountyMapPresentationLod.Near,
                    CountyMapPresentationLod.Near, 70,
                    "county.planning-grid.visible-chunk.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Draft,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 80,
                    "county.draft.priority.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Selection,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 90,
                    "county.selection.priority.v1"),
                new CountyMapPresentationLayer(
                    CountyMapPresentationLayerId.Debug,
                    CountyMapPresentationLod.Far,
                    CountyMapPresentationLod.Near, 100,
                    "county.debug.hidden.v1", true)
            };

        private int Index(int row, int column) =>
            checked(row * _partition.Columns + column);

        private void ValidateCell(int row, int column)
        {
            if (row < 0 || row >= _partition.Rows || column < 0 ||
                column >= _partition.Columns)
                throw new ArgumentOutOfRangeException(nameof(row));
        }
    }
}
