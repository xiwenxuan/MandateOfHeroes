using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class LuoyangHumanScaleLocalMapIds
    {
        public const string ContractId =
            "mandate.luoyang.human-scale-local-map-navigation.v1";
        public const string TaskId =
            "LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1";
        public const string MapVersionId = "luoyang.local-map.master.v1";
        public const string SettlementLocationId =
            "place.han140.sili.henan.luoyang";
        public const string StatusId =
            "LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1_IMPLEMENTED_PENDING_FINAL_VALIDATION";

        public const string EntranceNodeTypeId =
            "local-nav.node.facility-entrance.v1";
        public const string RoadNodeTypeId = "local-nav.node.road.v1";
        public const string IntersectionNodeTypeId =
            "local-nav.node.intersection.v1";
        public const string GateInsideNodeTypeId =
            "local-nav.node.gate-inside.v1";
        public const string GatePassageNodeTypeId =
            "local-nav.node.gate-passage.v1";
        public const string GateOutsideNodeTypeId =
            "local-nav.node.gate-outside.v1";
        public const string BridgeEntranceANodeTypeId =
            "local-nav.node.bridge-entrance-a.v1";
        public const string BridgeDeckNodeTypeId =
            "local-nav.node.bridge-deck.v1";
        public const string BridgeEntranceBNodeTypeId =
            "local-nav.node.bridge-entrance-b.v1";

        public const string PrimaryRoadClassId =
            "local-road.class.primary.v1";
        public const string SecondaryRoadClassId =
            "local-road.class.secondary.v1";
        public const string AlleyRoadClassId = "local-road.class.alley.v1";
        public const string FacilityAccessRoadClassId =
            "local-road.class.facility-access.v1";
        public const string GatePassageRoadClassId =
            "local-road.class.gate-passage.v1";
        public const string BridgePassageRoadClassId =
            "local-road.class.bridge-passage.v1";

        public const string GroundLocationTypeId =
            "person-location.precision.local-ground.v1";
        public const string FacilityEntranceLocationTypeId =
            "person-location.precision.facility-entrance.v1";
        public const string StrategicLocationTypeId =
            "person-location.precision.strategic.v1";

        public const int FacilityCount = 2084;
        public const int MajorHistoricalGateReviewCount = 12;
        public const int WorldMetresPerUnityUnit = 10;
        public const int StreamingRadiusCells = 1;
        public const int StreamingResidentCellCount = 9;
        public const int CoordinateCentimetresPerMetre = 100;
        public const int MaxWalkableSlopePermille = 350;
        public const int MapMinColumn = 2013;
        public const int MapMaxColumn = 2104;
        public const int MapMinRow = 1202;
        public const int MapMaxRow = 1266;
        public const int LocalSpaceCount = 5980;
        public const bool CreatesSimulationSubCells = false;
    }

    public static class FacilitySpatialCapabilityIds
    {
        public const string Building = "facility-spatial.building.v1";
        public const string Gate = "facility-spatial.gate.v1";
        public const string Bridge = "facility-spatial.bridge.v1";
        public const string Road = "facility-spatial.road.v1";
        public const string Wall = "facility-spatial.wall.v1";
        public const string MoatOrWater = "facility-spatial.moat-water.v1";
        public const string OpenArea = "facility-spatial.open-area.v1";
        public const string ProductiveLand =
            "facility-spatial.productive-land.v1";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Building, Gate, Bridge, Road, Wall, MoatOrWater, OpenArea,
            ProductiveLand
        };
    }

    public static class FacilitySpatialAccessKindIds
    {
        public const string Primary = "facility-access.primary.v1";
        public const string Area = "facility-access.area.v1";
        public const string Work = "facility-access.work.v1";
        public const string GateInside = "facility-access.gate-inside.v1";
        public const string GateOutside = "facility-access.gate-outside.v1";
        public const string BridgeA = "facility-access.bridge-a.v1";
        public const string BridgeB = "facility-access.bridge-b.v1";
    }

    public static class LocalTraversalConditionIds
    {
        public const string StaticWalkable =
            "local-traversal.condition.static-walkable.v1";
        public const string FormalRoadOpen =
            "local-traversal.condition.formal-road-open.v1";
        public const string FormalPassageAvailable =
            "local-traversal.condition.formal-passage-available.v1";
    }

    public static class SettlementSpatialCoordinateSystemIds
    {
        public const string NormalizedTownBasisPoints =
            "settlement-spatial.normalized-town-basis-points.v1";
        public const string StrategicCellLocalCentimetres =
            "settlement-spatial.strategic-cell-local-centimetres.v1";
    }

    public sealed class SettlementFacilitySpatialProjection
    {
        public string FacilityId { get; internal set; }
        public string SettlementLocationId { get; internal set; }
        public ulong ParentCellId64 { get; internal set; }
        public string AnchorSpaceId { get; internal set; }
        public string CoordinateSystemId { get; internal set; }
        public int CoordinateExtentUnits { get; internal set; }
        public int CenterXUnits { get; internal set; }
        public int CenterYUnits { get; internal set; }
        public int FootprintWidthUnits { get; internal set; }
        public int FootprintHeightUnits { get; internal set; }
        public string CapabilityId { get; internal set; }
        public bool RequiresAccess { get; internal set; }
        public bool HasBlockingGeometry { get; internal set; }
        public string SourceContractId { get; internal set; }
    }

    public static class SettlementSpatialCompatibility
    {
        public static SettlementFacilitySpatialProjection Project(
            TownFacilityState facility)
        {
            if (facility == null) throw new ArgumentNullException(
                nameof(facility));
            if (!facility.HasMapPlacement)
                throw new InvalidOperationException(
                    "An unplaced Town Facility has no spatial projection.");
            return new SettlementFacilitySpatialProjection
            {
                FacilityId = facility.Id,
                SettlementLocationId = facility.LocationId,
                AnchorSpaceId = facility.LocationId,
                CoordinateSystemId = SettlementSpatialCoordinateSystemIds
                    .NormalizedTownBasisPoints,
                CoordinateExtentUnits = 10_000,
                CenterXUnits = facility.MapXBasisPoints,
                CenterYUnits = facility.MapYBasisPoints,
                FootprintWidthUnits = facility.FootprintWidthBasisPoints,
                FootprintHeightUnits = facility.FootprintHeightBasisPoints,
                CapabilityId = ResolveTownCapability(facility.KindId),
                RequiresAccess = true,
                HasBlockingGeometry = !string.Equals(facility.KindId,
                    TownFacilityKindIds.Market, StringComparison.Ordinal),
                SourceContractId = "mandate.m26-p5b.town-spatial.v68"
            };
        }

        public static SettlementFacilitySpatialProjection Project(
            LuoyangFacilitySpatialCapability capability,
            LuoyangFacilityLocalFootprint footprint)
        {
            if (capability == null) throw new ArgumentNullException(
                nameof(capability));
            if (footprint == null || footprint.FacilityId !=
                capability.FacilityId)
                throw new InvalidOperationException(
                    "Facility spatial projection references do not match.");
            return new SettlementFacilitySpatialProjection
            {
                FacilityId = capability.FacilityId,
                SettlementLocationId = capability.SettlementLocationId,
                ParentCellId64 = capability.CellId64,
                AnchorSpaceId = capability.LocalSpaceId,
                CoordinateSystemId = SettlementSpatialCoordinateSystemIds
                    .StrategicCellLocalCentimetres,
                CoordinateExtentUnits = checked(
                    GlobalSpatialFoundationV1.CellSizeMetres * 100),
                CenterXUnits = checked((int)Math.Round(
                    footprint.CenterEastMetres * 100d)),
                CenterYUnits = checked((int)Math.Round(
                    footprint.CenterNorthMetres * 100d)),
                FootprintWidthUnits = checked((int)Math.Round(
                    footprint.HalfExtentEastMetres * 200d)),
                FootprintHeightUnits = checked((int)Math.Round(
                    footprint.HalfExtentNorthMetres * 200d)),
                CapabilityId = capability.CapabilityId,
                RequiresAccess = capability.RequiresAccess,
                HasBlockingGeometry = capability.HasBlockingGeometry,
                SourceContractId = LuoyangHumanScaleLocalMapIds.ContractId
            };
        }

        private static string ResolveTownCapability(string kindId) =>
            string.Equals(kindId, TownFacilityKindIds.Market,
                StringComparison.Ordinal)
                ? FacilitySpatialCapabilityIds.OpenArea
                : FacilitySpatialCapabilityIds.Building;
    }

    public readonly struct LuoyangLocalCoordinate :
        IEquatable<LuoyangLocalCoordinate>
    {
        public LuoyangLocalCoordinate(string localSpaceId, double eastMetres,
            double northMetres, double elevationMetres = 0d)
        {
            LocalSpaceId = new StableId(localSpaceId).Value;
            if (!Finite(eastMetres) || !Finite(northMetres) ||
                !Finite(elevationMetres) || eastMetres < 0d ||
                eastMetres > GlobalSpatialFoundationV1.CellSizeMetres ||
                northMetres < 0d ||
                northMetres > GlobalSpatialFoundationV1.CellSizeMetres)
                throw new ArgumentOutOfRangeException(nameof(eastMetres));
            EastMetres = eastMetres;
            NorthMetres = northMetres;
            ElevationMetres = elevationMetres;
        }

        public string LocalSpaceId { get; }
        public double EastMetres { get; }
        public double NorthMetres { get; }
        public double ElevationMetres { get; }

        public bool Equals(LuoyangLocalCoordinate other) =>
            string.Equals(LocalSpaceId, other.LocalSpaceId,
                StringComparison.Ordinal) &&
            EastMetres.Equals(other.EastMetres) &&
            NorthMetres.Equals(other.NorthMetres) &&
            ElevationMetres.Equals(other.ElevationMetres);
        public override bool Equals(object obj) =>
            obj is LuoyangLocalCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(LocalSpaceId,
            EastMetres, NorthMetres, ElevationMetres);

        private static bool Finite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class LuoyangWorldScale
    {
        public string Id => "world-scale.luoyang-human-scale.v1";
        public int WorldMetresPerUnityUnit =>
            LuoyangHumanScaleLocalMapIds.WorldMetresPerUnityUnit;

        public UnityLocalPosition WorldToUnity(
            GlobalProjectedCoordinate world,
            double elevationMetres,
            GlobalProjectedCoordinate floatingOrigin)
        {
            var scale = WorldMetresPerUnityUnit;
            return new UnityLocalPosition(
                (world.EastingMetres - floatingOrigin.EastingMetres) / scale,
                elevationMetres / scale,
                (world.NorthingMetres - floatingOrigin.NorthingMetres) / scale);
        }

        public GlobalProjectedCoordinate UnityToWorld(
            UnityLocalPosition unity,
            GlobalProjectedCoordinate floatingOrigin) =>
            new GlobalProjectedCoordinate(
                floatingOrigin.EastingMetres +
                unity.XMetres * WorldMetresPerUnityUnit,
                floatingOrigin.NorthingMetres +
                unity.ZMetres * WorldMetresPerUnityUnit);
    }

    [Serializable]
    public sealed class LuoyangHumanScaleLocalSpace
    {
        public string Id;
        public ulong ParentCellId64;
        public string SettlementLocationId;
        public int GridColumn;
        public int GridRow;
        public double OriginEastingMetres;
        public double OriginNorthingMetres;
        public int WidthMetres;
        public int HeightMetres;
        public int RotationMilliDegrees;
        public int ScaleBasisPoints;
        public string VersionId;
        public string SurfaceProfileId;
        public bool IsStrategicCell;
    }

    [Serializable]
    public sealed class LuoyangFacilityLocalFootprint
    {
        public string Id;
        public string FacilityId;
        public string LocalSpaceId;
        public ulong CellId64;
        public double CenterEastMetres;
        public double CenterNorthMetres;
        public double HalfExtentEastMetres;
        public double HalfExtentNorthMetres;
        public int RotationMilliDegrees;
        public bool BlocksPedestrian;
        public string AreaProfileId;

        public bool Contains(double eastMetres, double northMetres,
            double clearanceMetres = 0d)
        {
            var radians = -RotationMilliDegrees / 1000d * Math.PI / 180d;
            var dx = eastMetres - CenterEastMetres;
            var dy = northMetres - CenterNorthMetres;
            var localX = dx * Math.Cos(radians) - dy * Math.Sin(radians);
            var localY = dx * Math.Sin(radians) + dy * Math.Cos(radians);
            return Math.Abs(localX) <= HalfExtentEastMetres + clearanceMetres &&
                   Math.Abs(localY) <= HalfExtentNorthMetres + clearanceMetres;
        }
    }

    [Serializable]
    public sealed class LuoyangFacilitySpatialCapability
    {
        public string Id;
        public string FacilityId;
        public string FacilityDefinitionId;
        public string SettlementLocationId;
        public string LocalSpaceId;
        public ulong CellId64;
        public string CapabilityId;
        public bool RequiresAccess;
        public bool HasBlockingGeometry;
        public string FormalWorldObjectId;
    }

    [Serializable]
    public sealed class LuoyangFacilityLocalEntrance
    {
        public string Id;
        public string FacilityId;
        public string LocalSpaceId;
        public ulong CellId64;
        public double EastMetres;
        public double NorthMetres;
        public double ElevationMetres;
        public int FacingMilliDegrees;
        public string EntranceTypeId;
        public string AccessNodeId;
        public bool IsPrimary;
    }

    [Serializable]
    public sealed class LuoyangLocalRoutePoint
    {
        public int Sequence;
        public string LocalSpaceId;
        public ulong CellId64;
        public double LocalEastMetres;
        public double LocalNorthMetres;
        public double ElevationMetres;
        public double GlobalEastingMetres;
        public double GlobalNorthingMetres;
    }

    [Serializable]
    public sealed class LuoyangLocalNavNode
    {
        public string Id;
        public string NodeTypeId;
        public string FacilityId;
        public string FacilityDefinitionId;
        public string LocalSpaceId;
        public ulong CellId64;
        public double LocalEastMetres;
        public double LocalNorthMetres;
        public double ElevationMetres;
    }

    [Serializable]
    public sealed class LuoyangLocalNavEdge
    {
        public string Id;
        public string FromNodeId;
        public string ToNodeId;
        public string RoadClassId;
        public string SourceStrategicEdgeId;
        public string PassageFacilityId;
        public string FormalWorldObjectId;
        public string TraversalConditionId;
        public int DistanceCentimetres;
        public int TraversalCostPermille;
        public int WidthCentimetres;
        public bool IsWalkable;
        public bool CrossesStrategicCellBoundary;
        public List<LuoyangLocalRoutePoint> Geometry =
            new List<LuoyangLocalRoutePoint>();
    }

    [Serializable]
    public sealed class LuoyangLocalTransitionPoint
    {
        public string Id;
        public string EdgeId;
        public int Sequence;
        public ulong FromCellId64;
        public ulong ToCellId64;
        public string FromLocalSpaceId;
        public string ToLocalSpaceId;
        public double SourceEastMetres;
        public double SourceNorthMetres;
        public double TargetEastMetres;
        public double TargetNorthMetres;
        public double SourceGlobalEastingMetres;
        public double SourceGlobalNorthingMetres;
        public double TargetGlobalEastingMetres;
        public double TargetGlobalNorthingMetres;
        public string ConnectedPathId;
        public string FormalWorldObjectId;
        public string TraversalConditionId;
    }

    public sealed class LuoyangHumanScaleLocalMapPlan
    {
        internal LuoyangHumanScaleLocalMapPlan(
            IReadOnlyList<LuoyangHumanScaleLocalSpace> localSpaces,
            IReadOnlyList<LuoyangFacilitySpatialCapability> capabilities,
            IReadOnlyList<LuoyangFacilityLocalFootprint> footprints,
            IReadOnlyList<LuoyangFacilityLocalEntrance> entrances,
            IReadOnlyList<LuoyangLocalNavNode> nodes,
            IReadOnlyList<LuoyangLocalNavEdge> edges,
            IReadOnlyList<LuoyangLocalTransitionPoint> transitions,
            IReadOnlyList<string> rejectedStrategicEdgeIds,
            string assetHash)
        {
            LocalSpaces = localSpaces;
            FacilityCapabilities = capabilities;
            Footprints = footprints;
            Entrances = entrances;
            Nodes = nodes;
            Edges = edges;
            Transitions = transitions;
            RejectedStrategicEdgeIds = rejectedStrategicEdgeIds;
            AssetHash = assetHash;
            LocalSpacesByCellId = localSpaces.ToDictionary(
                item => item.ParentCellId64);
            LocalSpacesById = localSpaces.ToDictionary(item => item.Id,
                StringComparer.Ordinal);
            FootprintsByFacilityId = footprints.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            FacilityCapabilitiesByFacilityId = capabilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            AccessPointsByFacilityId = entrances.GroupBy(
                    item => item.FacilityId, StringComparer.Ordinal)
                .ToDictionary(item => item.Key,
                    item => (IReadOnlyList<LuoyangFacilityLocalEntrance>)
                        item.OrderBy(value => value.Id, StringComparer.Ordinal)
                            .ToArray(), StringComparer.Ordinal);
            EntrancesByFacilityId = AccessPointsByFacilityId.ToDictionary(
                item => item.Key,
                item => item.Value.First(value => value.IsPrimary),
                StringComparer.Ordinal);
            NodesById = nodes.ToDictionary(item => item.Id,
                StringComparer.Ordinal);
            EntranceNodesByFacilityId = EntrancesByFacilityId.ToDictionary(
                item => item.Key,
                item => NodesById[item.Value.AccessNodeId],
                StringComparer.Ordinal);
            NavigationNodesByFacilityId = nodes.Where(item =>
                    !string.IsNullOrEmpty(item.FacilityId))
                .GroupBy(item => item.FacilityId, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item =>
                    EntranceNodesByFacilityId.TryGetValue(item.Key,
                        out var access) ? access : item.OrderBy(value =>
                        value.Id, StringComparer.Ordinal).First(),
                    StringComparer.Ordinal);
            EdgesById = edges.ToDictionary(item => item.Id,
                StringComparer.Ordinal);
        }

        public string ContractId => LuoyangHumanScaleLocalMapIds.ContractId;
        public string TaskId => LuoyangHumanScaleLocalMapIds.TaskId;
        public string MapVersionId =>
            LuoyangHumanScaleLocalMapIds.MapVersionId;
        public string StatusId => LuoyangHumanScaleLocalMapIds.StatusId;
        public string AssetHash { get; }
        public bool CreatesSimulationSubCells => false;
        public LuoyangWorldScale WorldScale { get; } =
            new LuoyangWorldScale();
        public IReadOnlyList<LuoyangHumanScaleLocalSpace> LocalSpaces { get; }
        public IReadOnlyList<LuoyangFacilitySpatialCapability>
            FacilityCapabilities { get; }
        public IReadOnlyList<LuoyangFacilityLocalFootprint> Footprints { get; }
        public IReadOnlyList<LuoyangFacilityLocalEntrance> Entrances { get; }
        public IReadOnlyList<LuoyangLocalNavNode> Nodes { get; }
        public IReadOnlyList<LuoyangLocalNavEdge> Edges { get; }
        public IReadOnlyList<LuoyangLocalTransitionPoint> Transitions { get; }
        public IReadOnlyList<string> RejectedStrategicEdgeIds { get; }
        public IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace>
            LocalSpacesByCellId { get; }
        public IReadOnlyDictionary<string, LuoyangHumanScaleLocalSpace>
            LocalSpacesById { get; }
        public IReadOnlyDictionary<string, LuoyangFacilityLocalFootprint>
            FootprintsByFacilityId { get; }
        public IReadOnlyDictionary<string, LuoyangFacilitySpatialCapability>
            FacilityCapabilitiesByFacilityId { get; }
        public IReadOnlyDictionary<string,
            IReadOnlyList<LuoyangFacilityLocalEntrance>>
            AccessPointsByFacilityId { get; }
        public IReadOnlyDictionary<string, LuoyangFacilityLocalEntrance>
            EntrancesByFacilityId { get; }
        public IReadOnlyDictionary<string, LuoyangLocalNavNode> NodesById
            { get; }
        public IReadOnlyDictionary<string, LuoyangLocalNavNode>
            EntranceNodesByFacilityId { get; }
        public IReadOnlyDictionary<string, LuoyangLocalNavNode>
            NavigationNodesByFacilityId { get; }
        public IReadOnlyDictionary<string, LuoyangLocalNavEdge> EdgesById
            { get; }
        public CellTraversalPlan CellTraversal { get; internal set; }
    }

    public sealed class LuoyangStrategicLocalCoordinateService
    {
        private readonly CellGridIndex _grid;
        private readonly IReadOnlyDictionary<ulong,
            LuoyangHumanScaleLocalSpace> _spaces;

        public LuoyangStrategicLocalCoordinateService(
            LuoyangHumanScaleLocalMapPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            _grid = GlobalSpatialFoundationV1.CreateCellGrid();
            _spaces = plan.LocalSpacesByCellId;
        }

        public LuoyangLocalCoordinate StrategicToLocal(ulong cellId64,
            GlobalProjectedCoordinate world, double elevationMetres = 0d)
        {
            if (!_spaces.TryGetValue(cellId64, out var space))
                throw new KeyNotFoundException(
                    "Unknown Luoyang LocalSpace Cell: " + cellId64);
            return new LuoyangLocalCoordinate(space.Id,
                world.EastingMetres - space.OriginEastingMetres,
                world.NorthingMetres - space.OriginNorthingMetres,
                elevationMetres);
        }

        public GlobalProjectedCoordinate LocalToStrategic(
            LuoyangLocalCoordinate local)
        {
            if (!_spaces.Values.Any(item => string.Equals(item.Id,
                    local.LocalSpaceId, StringComparison.Ordinal)))
                throw new KeyNotFoundException(
                    "Unknown Luoyang LocalSpace: " + local.LocalSpaceId);
            var space = _spaces.Values.First(item => string.Equals(item.Id,
                local.LocalSpaceId, StringComparison.Ordinal));
            return new GlobalProjectedCoordinate(
                space.OriginEastingMetres + local.EastMetres,
                space.OriginNorthingMetres + local.NorthMetres);
        }

        public LuoyangHumanScaleLocalSpace GetSpace(int row, int column)
        {
            var id = _grid.ToCellId(row, column).Value;
            return _spaces.TryGetValue(id, out var space) ? space : null;
        }
    }

    public static class LuoyangHumanScaleLocalMapRules
    {
        private const string RoadDefinitionId = "facility.public.road";
        private const string BridgeDefinitionId = "facility.public.bridge";
        private const double EntranceClearanceMetres = 6d;
        private const double LaneInsetMetres = 32d;

        public static LuoyangHumanScaleLocalMapPlan CreatePlan(
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangWholeCityCompositionPlan composition,
            LuoyangRoadTraversalRefinementPlan strategicRoads)
        {
            if (wholeCity == null) throw new ArgumentNullException(
                nameof(wholeCity));
            if (composition == null) throw new ArgumentNullException(
                nameof(composition));
            if (strategicRoads == null) throw new ArgumentNullException(
                nameof(strategicRoads));
            if (wholeCity.Facilities.Count !=
                    LuoyangHumanScaleLocalMapIds.FacilityCount ||
                composition.Anchors.Count != wholeCity.Facilities.Count)
                throw new InvalidOperationException(
                    "The human-scale map requires all 2,084 Facilities.");

            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var spaces = CreateSpaces(grid);
            var spacesByCell = spaces.ToDictionary(
                item => item.ParentCellId64);
            var capabilities = new List<LuoyangFacilitySpatialCapability>(
                wholeCity.Facilities.Count);
            var footprints = new List<LuoyangFacilityLocalFootprint>(
                wholeCity.Facilities.Count);
            var entrances = new List<LuoyangFacilityLocalEntrance>(
                wholeCity.Facilities.Count + 32);
            foreach (var facility in wholeCity.Facilities.OrderBy(
                         item => item.FacilityId, StringComparer.Ordinal))
            {
                var anchor = composition.AnchorsByFacilityId[
                    facility.FacilityId];
                var capabilityId = ResolveCapability(
                    facility.FacilityDefinitionId);
                ResolveFootprint(facility.FacilityDefinitionId,
                    out var halfEast, out var halfNorth, out var blocks);
                var centerEast = GlobalSpatialFoundationV1.CellSizeMetres /
                                 2d + anchor.VisualLocalEastMetres;
                var centerNorth = GlobalSpatialFoundationV1.CellSizeMetres /
                                  2d + anchor.VisualLocalNorthMetres;
                var rotation = (int)Math.Round(anchor.RotationDegrees * 1000d);
                var footprint = new LuoyangFacilityLocalFootprint
                {
                    Id = "local-footprint." + facility.FacilityId,
                    FacilityId = facility.FacilityId,
                    LocalSpaceId = spacesByCell[facility.CellId64].Id,
                    CellId64 = facility.CellId64,
                    CenterEastMetres = centerEast,
                    CenterNorthMetres = centerNorth,
                    HalfExtentEastMetres = halfEast,
                    HalfExtentNorthMetres = halfNorth,
                    RotationMilliDegrees = rotation,
                    BlocksPedestrian = blocks,
                    AreaProfileId = blocks
                        ? "local-area.blocked.facility-footprint.v1"
                        : "local-area.walkable.infrastructure.v1"
                };
                footprints.Add(footprint);
                var requiresAccess = RequiresAccess(capabilityId);
                capabilities.Add(new LuoyangFacilitySpatialCapability
                {
                    Id = "facility-spatial-capability." +
                         facility.FacilityId,
                    FacilityId = facility.FacilityId,
                    FacilityDefinitionId = facility.FacilityDefinitionId,
                    SettlementLocationId =
                        LuoyangHumanScaleLocalMapIds.SettlementLocationId,
                    LocalSpaceId = footprint.LocalSpaceId,
                    CellId64 = facility.CellId64,
                    CapabilityId = capabilityId,
                    RequiresAccess = requiresAccess,
                    HasBlockingGeometry = blocks,
                    FormalWorldObjectId = facility.FacilityId
                });
                AddAccessPoints(entrances, facility.FacilityId,
                    capabilityId, footprint, rotation);
            }

            var nodes = CreateNodes(wholeCity, strategicRoads, entrances,
                spacesByCell);
            var nodeById = nodes.ToDictionary(item => item.Id,
                StringComparer.Ordinal);
            var edges = CreateEdges(wholeCity, strategicRoads, entrances,
                capabilities, footprints, nodeById, spacesByCell, grid,
                out var rejectedStrategicEdges);
            MarkIntersections(nodes, edges);
            var transitions = CreateTransitions(edges, grid);
            var orderedSpaces = spaces.OrderBy(item => item.ParentCellId64)
                .ToArray();
            var orderedCapabilities = capabilities.OrderBy(
                item => item.FacilityId, StringComparer.Ordinal).ToArray();
            var orderedFootprints = footprints.OrderBy(item => item.FacilityId,
                StringComparer.Ordinal).ToArray();
            var orderedEntrances = entrances.OrderBy(item => item.FacilityId,
                StringComparer.Ordinal).ToArray();
            var orderedNodes = nodes.OrderBy(item => item.Id,
                StringComparer.Ordinal).ToArray();
            var orderedEdges = edges.OrderBy(item => item.Id,
                StringComparer.Ordinal).ToArray();
            var orderedTransitions = transitions.OrderBy(item => item.Id,
                StringComparer.Ordinal).ToArray();
            var orderedRejectedStrategicEdges = rejectedStrategicEdges
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
            var hash = ComputeHash(orderedSpaces, orderedCapabilities,
                orderedFootprints,
                orderedEntrances, orderedNodes, orderedEdges,
                orderedTransitions, orderedRejectedStrategicEdges);
            var plan = new LuoyangHumanScaleLocalMapPlan(orderedSpaces,
                orderedCapabilities,
                orderedFootprints, orderedEntrances, orderedNodes,
                orderedEdges, orderedTransitions,
                orderedRejectedStrategicEdges, hash);
            plan.CellTraversal = LuoyangCellTraversalRules.CreatePlan(plan,
                strategicRoads);
            Validate(plan, wholeCity, strategicRoads);
            return plan;
        }

        public static void Validate(LuoyangHumanScaleLocalMapPlan plan,
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangRoadTraversalRefinementPlan strategicRoads)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.CreatesSimulationSubCells ||
                plan.LocalSpaces.Count !=
                    LuoyangHumanScaleLocalMapIds.LocalSpaceCount ||
                plan.FacilityCapabilities.Count !=
                    LuoyangHumanScaleLocalMapIds.FacilityCount ||
                plan.Footprints.Count !=
                    LuoyangHumanScaleLocalMapIds.FacilityCount ||
                plan.AssetHash == null || plan.AssetHash.Length != 64)
                throw new InvalidOperationException(
                    "Invalid Luoyang human-scale map coverage.");
            if (plan.LocalSpaces.Any(item => item == null ||
                    item.ParentCellId64 == 0 || !item.IsStrategicCell ||
                    item.WidthMetres != 2000 || item.HeightMetres != 2000 ||
                    item.ScaleBasisPoints != 10_000 ||
                    !string.Equals(item.VersionId, plan.MapVersionId,
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "A LocalSpace drifted from its strategic Cell.");
            var facilities = new HashSet<string>(wholeCity.Facilities.Select(
                item => item.FacilityId), StringComparer.Ordinal);
            if (plan.FacilityCapabilities.Any(item => item == null ||
                    !facilities.Contains(item.FacilityId) ||
                    !FacilitySpatialCapabilityIds.All.Contains(
                        item.CapabilityId) ||
                    item.FormalWorldObjectId != item.FacilityId ||
                    !plan.LocalSpacesById.ContainsKey(item.LocalSpaceId)) ||
                plan.FacilityCapabilities.Select(item => item.FacilityId)
                    .Distinct(StringComparer.Ordinal).Count() !=
                LuoyangHumanScaleLocalMapIds.FacilityCount)
                throw new InvalidOperationException(
                    "Invalid Facility spatial capability coverage.");
            foreach (var entrance in plan.Entrances)
            {
                if (!facilities.Contains(entrance.FacilityId) ||
                    !plan.LocalSpacesById.ContainsKey(entrance.LocalSpaceId) ||
                    !plan.NodesById.ContainsKey(entrance.AccessNodeId) ||
                    !Finite(entrance.EastMetres) ||
                    !Finite(entrance.NorthMetres) ||
                    entrance.EastMetres < 0d || entrance.EastMetres > 2000d ||
                    entrance.NorthMetres < 0d ||
                    entrance.NorthMetres > 2000d)
                    throw new InvalidOperationException(
                        "Invalid Facility entrance: " + entrance.FacilityId);
                var footprint = plan.FootprintsByFacilityId[
                    entrance.FacilityId];
                if (footprint.BlocksPedestrian && footprint.Contains(
                        entrance.EastMetres, entrance.NorthMetres, -0.001d))
                    throw new InvalidOperationException(
                        "Facility entrance is inside its blocking footprint: " +
                        entrance.FacilityId);
            }
            foreach (var capability in plan.FacilityCapabilities.Where(
                         item => item.RequiresAccess))
                if (!plan.AccessPointsByFacilityId.TryGetValue(
                        capability.FacilityId, out var accessPoints) ||
                    accessPoints.Count == 0 ||
                    accessPoints.Count(item => item.IsPrimary) != 1)
                    throw new InvalidOperationException(
                        "A Facility requiring access has no valid primary " +
                        "access: " + capability.FacilityId);
            var nodeIds = new HashSet<string>(plan.Nodes.Select(item => item.Id),
                StringComparer.Ordinal);
            if (plan.Edges.Any(item => item == null || !item.IsWalkable ||
                    !nodeIds.Contains(item.FromNodeId) ||
                    !nodeIds.Contains(item.ToNodeId) ||
                    item.FromNodeId == item.ToNodeId ||
                    item.DistanceCentimetres <= 0 ||
                    item.TraversalCostPermille < 1000 ||
                    item.WidthCentimetres < 180 ||
                    item.Geometry == null || item.Geometry.Count < 2) ||
                plan.Edges.Select(item => item.Id).Distinct(
                    StringComparer.Ordinal).Count() != plan.Edges.Count)
                throw new InvalidOperationException(
                    "Invalid local navigation edge.");
            var strategicEdgeIds = new HashSet<string>(strategicRoads
                .NavigationEdges.Select(item => item.EdgeId),
                StringComparer.Ordinal);
            var mappedStrategicEdgeIds = new HashSet<string>(plan.Edges
                .Where(item => !string.IsNullOrEmpty(
                    item.SourceStrategicEdgeId))
                .Select(item => item.SourceStrategicEdgeId),
                StringComparer.Ordinal);
            var rejectedStrategicEdgeIds = new HashSet<string>(
                plan.RejectedStrategicEdgeIds, StringComparer.Ordinal);
            if (mappedStrategicEdgeIds.Overlaps(rejectedStrategicEdgeIds) ||
                mappedStrategicEdgeIds.Concat(rejectedStrategicEdgeIds)
                    .Any(item => !strategicEdgeIds.Contains(item)) ||
                mappedStrategicEdgeIds.Concat(rejectedStrategicEdgeIds)
                    .Distinct(StringComparer.Ordinal).Count() !=
                strategicEdgeIds.Count)
                throw new InvalidOperationException(
                    "Strategic road local-expression audit is incomplete.");
            foreach (var classId in new[]
                     {
                         LuoyangHumanScaleLocalMapIds.PrimaryRoadClassId,
                         LuoyangHumanScaleLocalMapIds.SecondaryRoadClassId,
                         LuoyangHumanScaleLocalMapIds.AlleyRoadClassId,
                         LuoyangHumanScaleLocalMapIds.FacilityAccessRoadClassId
                     })
                if (!plan.Edges.Any(item => string.Equals(item.RoadClassId,
                        classId, StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        "Local road class is empty: " + classId);
            var gates = wholeCity.Facilities.Count(item => IsGate(
                item.FacilityDefinitionId));
            var bridges = wholeCity.Facilities.Count(item => string.Equals(
                item.FacilityDefinitionId, BridgeDefinitionId,
                StringComparison.Ordinal));
            if (gates <= 0 || bridges <= 0 ||
                plan.FacilityCapabilities.Count(item => string.Equals(
                    item.CapabilityId, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal)) != gates ||
                plan.FacilityCapabilities.Count(item => string.Equals(
                    item.CapabilityId, FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal)) != bridges)
                throw new InvalidOperationException(
                    "Luoyang gate/bridge identity coverage drifted.");
            if (plan.Transitions.Any(item => item.FromCellId64 == 0 ||
                    item.ToCellId64 == 0 ||
                    item.FromCellId64 == item.ToCellId64 ||
                    item.ConnectedPathId != item.EdgeId ||
                    string.IsNullOrEmpty(item.TraversalConditionId) ||
                    Math.Abs(item.SourceGlobalEastingMetres -
                             item.TargetGlobalEastingMetres) > 0.001d ||
                    Math.Abs(item.SourceGlobalNorthingMetres -
                             item.TargetGlobalNorthingMetres) > 0.001d ||
                    !plan.LocalSpacesByCellId.ContainsKey(item.FromCellId64) ||
                    !plan.LocalSpacesByCellId.ContainsKey(item.ToCellId64)))
                throw new InvalidOperationException(
                    "Invalid LocalSpace transition.");
            var adjacency = plan.Nodes.ToDictionary(item => item.Id,
                _ => new List<string>(), StringComparer.Ordinal);
            foreach (var edge in plan.Edges)
            {
                adjacency[edge.FromNodeId].Add(edge.ToNodeId);
                adjacency[edge.ToNodeId].Add(edge.FromNodeId);
            }
            var origin = plan.Entrances[0].AccessNodeId;
            var reachable = new HashSet<string>(StringComparer.Ordinal)
                { origin };
            var pending = new Queue<string>();
            pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var next in adjacency[current])
                    if (reachable.Add(next)) pending.Enqueue(next);
            }
            var unreachable = plan.Entrances.FirstOrDefault(item =>
                !reachable.Contains(item.AccessNodeId));
            if (unreachable != null)
                throw new InvalidOperationException(
                    "Facility entrance is not reachable: " +
                    unreachable.FacilityId);
        }

        public static IReadOnlyList<LuoyangHumanScaleLocalSpace>
            SelectStreamingWindow(LuoyangHumanScaleLocalMapPlan plan,
                ulong centerCellId64)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.LocalSpacesByCellId.TryGetValue(centerCellId64,
                    out var center))
                throw new KeyNotFoundException(
                    "The streaming center is outside Luoyang Local Map.");
            var result = new List<LuoyangHumanScaleLocalSpace>();
            for (var row = center.GridRow - 1; row <= center.GridRow + 1; row++)
            for (var column = center.GridColumn - 1;
                 column <= center.GridColumn + 1; column++)
            {
                var candidate = plan.LocalSpaces.FirstOrDefault(item =>
                    item.GridRow == row && item.GridColumn == column);
                if (candidate != null) result.Add(candidate);
            }
            return result.OrderBy(item => item.ParentCellId64).ToArray();
        }

        private static List<LuoyangHumanScaleLocalSpace> CreateSpaces(
            CellGridIndex grid)
        {
            var result = new List<LuoyangHumanScaleLocalSpace>(
                LuoyangHumanScaleLocalMapIds.LocalSpaceCount);
            for (var row = LuoyangHumanScaleLocalMapIds.MapMinRow;
                 row <= LuoyangHumanScaleLocalMapIds.MapMaxRow; row++)
            for (var column = LuoyangHumanScaleLocalMapIds.MapMinColumn;
                 column <= LuoyangHumanScaleLocalMapIds.MapMaxColumn; column++)
            {
                var cellId = grid.ToCellId(row, column).Value;
                result.Add(new LuoyangHumanScaleLocalSpace
                {
                    Id = LocalSpaceId(cellId),
                    ParentCellId64 = cellId,
                    SettlementLocationId =
                        LuoyangHumanScaleLocalMapIds.SettlementLocationId,
                    GridColumn = column,
                    GridRow = row,
                    OriginEastingMetres = grid.OriginX + column * 2000d,
                    OriginNorthingMetres = grid.OriginY - (row + 1) * 2000d,
                    WidthMetres = 2000,
                    HeightMetres = 2000,
                    RotationMilliDegrees = 0,
                    ScaleBasisPoints = 10_000,
                    VersionId = LuoyangHumanScaleLocalMapIds.MapVersionId,
                    SurfaceProfileId =
                        "local-surface.luoyang.terrain-sampled.v1",
                    IsStrategicCell = true
                });
            }
            return result;
        }

        private static List<LuoyangLocalNavNode> CreateNodes(
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangRoadTraversalRefinementPlan strategicRoads,
            IReadOnlyList<LuoyangFacilityLocalEntrance> entrances,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces)
        {
            var result = entrances.Where(item => item.AccessNodeId.StartsWith(
                    "local-nav.node.access.", StringComparison.Ordinal))
                .Select(item => new LuoyangLocalNavNode
            {
                Id = item.AccessNodeId,
                NodeTypeId = LuoyangHumanScaleLocalMapIds.EntranceNodeTypeId,
                FacilityId = item.FacilityId,
                FacilityDefinitionId = wholeCity.Facilities.Single(
                    value => value.FacilityId == item.FacilityId)
                    .FacilityDefinitionId,
                LocalSpaceId = item.LocalSpaceId,
                CellId64 = item.CellId64,
                LocalEastMetres = item.EastMetres,
                LocalNorthMetres = item.NorthMetres,
                ElevationMetres = item.ElevationMetres
            }).ToList();
            foreach (var strategic in strategicRoads.NavigationNodes)
            {
                if (string.Equals(strategic.FacilityDefinitionId,
                        RoadDefinitionId, StringComparison.Ordinal))
                {
                    result.Add(Node("local-nav.node.road." +
                        strategic.FacilityId,
                        LuoyangHumanScaleLocalMapIds.RoadNodeTypeId,
                        strategic, spaces, 1000d, 1000d));
                    continue;
                }
                var bridge = string.Equals(strategic.FacilityDefinitionId,
                    BridgeDefinitionId, StringComparison.Ordinal);
                var prefix = bridge ? "bridge" : "gate";
                var firstType = bridge
                    ? LuoyangHumanScaleLocalMapIds.BridgeEntranceANodeTypeId
                    : LuoyangHumanScaleLocalMapIds.GateInsideNodeTypeId;
                var middleType = bridge
                    ? LuoyangHumanScaleLocalMapIds.BridgeDeckNodeTypeId
                    : LuoyangHumanScaleLocalMapIds.GatePassageNodeTypeId;
                var lastType = bridge
                    ? LuoyangHumanScaleLocalMapIds.BridgeEntranceBNodeTypeId
                    : LuoyangHumanScaleLocalMapIds.GateOutsideNodeTypeId;
                result.Add(Node("local-nav.node." + prefix + ".a." +
                    strategic.FacilityId, firstType, strategic, spaces,
                    1000d, 700d));
                result.Add(Node("local-nav.node." + prefix + ".middle." +
                    strategic.FacilityId, middleType, strategic, spaces,
                    1000d, 1000d));
                result.Add(Node("local-nav.node." + prefix + ".b." +
                    strategic.FacilityId, lastType, strategic, spaces,
                    1000d, 1300d));
            }
            return result;
        }

        private static List<LuoyangLocalNavEdge> CreateEdges(
            LuoyangBuildingPerformancePlan wholeCity,
            LuoyangRoadTraversalRefinementPlan strategicRoads,
            IReadOnlyList<LuoyangFacilityLocalEntrance> entrances,
            IReadOnlyList<LuoyangFacilitySpatialCapability> capabilities,
            IReadOnlyList<LuoyangFacilityLocalFootprint> footprints,
            IReadOnlyDictionary<string, LuoyangLocalNavNode> nodes,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid,
            out IReadOnlyList<string> rejectedStrategicEdgeIds)
        {
            var result = new List<LuoyangLocalNavEdge>();
            var rejected = new List<string>();
            var strategicNodes = strategicRoads.NavigationNodes.ToDictionary(
                item => item.NodeId, StringComparer.Ordinal);
            var roadNodes = strategicRoads.NavigationNodes.Where(item =>
                    string.Equals(item.FacilityDefinitionId, RoadDefinitionId,
                        StringComparison.Ordinal))
                .ToDictionary(item => item.FacilityId,
                    item => nodes["local-nav.node.road." + item.FacilityId],
                    StringComparer.Ordinal);
            var networkTerminalsByCell = nodes.Values.Where(item =>
                    string.Equals(item.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds.RoadNodeTypeId,
                        StringComparison.Ordinal) ||
                    string.Equals(item.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds.GateInsideNodeTypeId,
                        StringComparison.Ordinal) ||
                    string.Equals(item.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds.GateOutsideNodeTypeId,
                        StringComparison.Ordinal) ||
                    string.Equals(item.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds
                            .BridgeEntranceANodeTypeId,
                        StringComparison.Ordinal) ||
                    string.Equals(item.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds
                            .BridgeEntranceBNodeTypeId,
                        StringComparison.Ordinal))
                .GroupBy(item => item.CellId64)
                .ToDictionary(item => item.Key,
                    item => (IReadOnlyList<LuoyangLocalNavNode>)item
                        .OrderBy(value => value.Id, StringComparer.Ordinal)
                        .ToArray());
            var strategicDegree = strategicRoads.NavigationNodes.ToDictionary(
                item => item.FacilityId, _ => 0, StringComparer.Ordinal);
            var capabilityByFacility = capabilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var footprintByFacility = footprints.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            var blockingFootprintByCell = capabilities.Where(item =>
                    item.HasBlockingGeometry)
                .ToDictionary(item => item.CellId64,
                    item => footprintByFacility[item.FacilityId]);
            foreach (var source in strategicRoads.NavigationEdges)
            {
                strategicDegree[strategicNodes[source.FromNodeId].FacilityId]++;
                strategicDegree[strategicNodes[source.ToNodeId].FacilityId]++;
            }
            foreach (var source in strategicRoads.NavigationEdges.OrderBy(
                         item => item.EdgeId, StringComparer.Ordinal))
            {
                var fromStrategic = strategicNodes[source.FromNodeId];
                var toStrategic = strategicNodes[source.ToNodeId];
                var fromPassage = !string.Equals(
                    fromStrategic.FacilityDefinitionId, RoadDefinitionId,
                    StringComparison.Ordinal);
                var toPassage = !string.Equals(
                    toStrategic.FacilityDefinitionId, RoadDefinitionId,
                    StringComparison.Ordinal);
                if (!fromPassage && !toPassage)
                {
                    var from = roadNodes[fromStrategic.FacilityId];
                    var to = roadNodes[toStrategic.FacilityId];
                    var primary = strategicDegree[from.FacilityId] >= 3 ||
                                  strategicDegree[to.FacilityId] >= 3;
                    if (!TryFindCellPathBetweenFormalNodes(from, to,
                            capabilityByFacility, spaces, grid,
                            out var formalRoadCellPath))
                    {
                        rejected.Add(source.EdgeId);
                        continue;
                    }
                    var geometry = BuildAccessGeometryAlongCells(from, to,
                        formalRoadCellPath, spaces, blockingFootprintByCell);
                    result.Add(CreateEdge(
                        "local-nav.edge.strategic." + source.EdgeId,
                        from, to, primary
                            ? LuoyangHumanScaleLocalMapIds.PrimaryRoadClassId
                            : LuoyangHumanScaleLocalMapIds.SecondaryRoadClassId,
                        source.EdgeId, string.Empty,
                        primary ? 1000 : 1040, primary ? 1800 : 1200,
                        geometry, grid));
                    continue;
                }
                var passage = fromPassage ? fromStrategic : toStrategic;
                var road = fromPassage ? toStrategic : fromStrategic;
                var approachEdges = strategicRoads.NavigationEdges.Where(
                        item => item.FromNodeId == passage.NodeId ||
                                item.ToNodeId == passage.NodeId)
                    .OrderBy(item => item.EdgeId, StringComparer.Ordinal)
                    .ToArray();
                var approachIndex = Array.FindIndex(approachEdges,
                    item => item.EdgeId == source.EdgeId);
                var bridge = string.Equals(passage.FacilityDefinitionId,
                    BridgeDefinitionId, StringComparison.Ordinal);
                var prefix = bridge ? "bridge" : "gate";
                var side = approachIndex <= 0 ? "a" : "b";
                var passageNode = nodes["local-nav.node." + prefix + "." +
                                        side + "." + passage.FacilityId];
                var roadNode = roadNodes[road.FacilityId];
                result.Add(CreateEdge("local-nav.edge.approach." +
                    source.EdgeId, roadNode, passageNode,
                    LuoyangHumanScaleLocalMapIds.PrimaryRoadClassId,
                    source.EdgeId, passage.FacilityId, 1000,
                    bridge ? 800 : 1200,
                    BuildStrategicGeometry(roadNode, passageNode, spaces, grid),
                    grid));
            }

            foreach (var passage in strategicRoads.NavigationNodes.Where(
                         item => !string.Equals(item.FacilityDefinitionId,
                             RoadDefinitionId, StringComparison.Ordinal)))
            {
                var bridge = string.Equals(passage.FacilityDefinitionId,
                    BridgeDefinitionId, StringComparison.Ordinal);
                var prefix = bridge ? "bridge" : "gate";
                var a = nodes["local-nav.node." + prefix + ".a." +
                              passage.FacilityId];
                var middle = nodes["local-nav.node." + prefix + ".middle." +
                                   passage.FacilityId];
                var b = nodes["local-nav.node." + prefix + ".b." +
                              passage.FacilityId];
                var roadClass = bridge
                    ? LuoyangHumanScaleLocalMapIds.BridgePassageRoadClassId
                    : LuoyangHumanScaleLocalMapIds.GatePassageRoadClassId;
                var width = bridge ? 800 : 1200;
                result.Add(CreateEdge("local-nav.edge." + prefix + ".a." +
                    passage.FacilityId, a, middle, roadClass, string.Empty,
                    passage.FacilityId, 1000, width,
                    BuildStraightGeometry(a, middle, spaces), grid));
                result.Add(CreateEdge("local-nav.edge." + prefix + ".b." +
                    passage.FacilityId, middle, b, roadClass, string.Empty,
                    passage.FacilityId, 1000, width,
                    BuildStraightGeometry(middle, b, spaces), grid));
            }

            foreach (var entrance in entrances)
            {
                var facility = wholeCity.Facilities.Single(item =>
                    item.FacilityId == entrance.FacilityId);
                if (IsGate(facility.FacilityDefinitionId) ||
                    string.Equals(facility.FacilityDefinitionId,
                        BridgeDefinitionId, StringComparison.Ordinal))
                    continue;
                var from = nodes[entrance.AccessNodeId];
                LuoyangLocalNavNode to;
                if (string.Equals(facility.FacilityDefinitionId,
                        RoadDefinitionId, StringComparison.Ordinal))
                    to = roadNodes[facility.FacilityId];
                else if (string.Equals(facility.FacilityDefinitionId,
                             BridgeDefinitionId, StringComparison.Ordinal))
                    to = nodes["local-nav.node.bridge.middle." +
                               facility.FacilityId];
                else if (IsGate(facility.FacilityDefinitionId))
                    to = nodes["local-nav.node.gate.middle." +
                               facility.FacilityId];
                else
                {
                    var cellPath = FindCellPathToNetwork(from,
                        capabilityByFacility, networkTerminalsByCell,
                        spaces, grid, out var targetNodeId);
                    to = nodes[targetNodeId];
                    var isLocalAlley = facility.FacilityDefinitionId.StartsWith(
                        "facility.residential.", StringComparison.Ordinal);
                    var localRoadClass = isLocalAlley
                        ? LuoyangHumanScaleLocalMapIds.AlleyRoadClassId
                        : LuoyangHumanScaleLocalMapIds
                            .FacilityAccessRoadClassId;
                    result.Add(CreateEdge("local-nav.edge.access." +
                        facility.FacilityId, from, to, localRoadClass,
                        string.Empty, string.Empty,
                        isLocalAlley ? 1120 : 1080,
                        isLocalAlley ? 400 : 600,
                        BuildAccessGeometryAlongCells(from, to, cellPath,
                            spaces, blockingFootprintByCell), grid));
                    continue;
                }
                var alley = facility.FacilityDefinitionId.StartsWith(
                    "facility.residential.", StringComparison.Ordinal);
                var roadClass = alley
                    ? LuoyangHumanScaleLocalMapIds.AlleyRoadClassId
                    : LuoyangHumanScaleLocalMapIds
                        .FacilityAccessRoadClassId;
                result.Add(CreateEdge("local-nav.edge.access." +
                    facility.FacilityId, from, to, roadClass, string.Empty,
                    string.Empty, alley ? 1120 : 1080,
                    alley ? 400 : 600,
                    BuildAccessGeometry(from, to, spaces, grid), grid));
            }
            rejectedStrategicEdgeIds = rejected;
            return result;
        }

        private static IReadOnlyList<ulong> FindCellPathToNetwork(
            LuoyangLocalNavNode source,
            IReadOnlyDictionary<string, LuoyangFacilitySpatialCapability>
                capabilities,
            IReadOnlyDictionary<ulong,
                IReadOnlyList<LuoyangLocalNavNode>> networkTerminalsByCell,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid,
            out string targetNodeId)
        {
            var blocked = new HashSet<ulong>(capabilities.Values.Where(
                    BlocksWholeCellTraversal)
                .Select(item => item.CellId64));
            blocked.Remove(source.CellId64);

            var pending = new Queue<ulong>();
            var previous = new Dictionary<ulong, ulong>();
            pending.Enqueue(source.CellId64);
            previous[source.CellId64] = 0;
            ulong targetCellId = 0;
            while (pending.Count > 0)
            {
                var currentId = pending.Dequeue();
                if (currentId != source.CellId64 &&
                    networkTerminalsByCell.ContainsKey(currentId))
                {
                    targetCellId = currentId;
                    break;
                }
                var current = Space(spaces, currentId);
                var neighbours = new List<ulong>(4);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow - 1, current.GridColumn);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow, current.GridColumn - 1);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow, current.GridColumn + 1);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow + 1, current.GridColumn);
                neighbours.Sort();
                foreach (var neighbour in neighbours)
                {
                    if (blocked.Contains(neighbour) &&
                        !networkTerminalsByCell.ContainsKey(neighbour) ||
                        previous.ContainsKey(neighbour)) continue;
                    previous[neighbour] = currentId;
                    pending.Enqueue(neighbour);
                }
            }
            if (targetCellId == 0)
                throw new InvalidOperationException(
                    "Facility access cannot reach the formal road network " +
                    "without crossing blocking geometry: " +
                    source.FacilityId);

            var reverse = new List<ulong>();
            for (var cursor = targetCellId; cursor != 0;
                 cursor = previous[cursor])
                reverse.Add(cursor);
            reverse.Reverse();
            var approachCell = reverse.Count > 1
                ? Space(spaces, reverse[reverse.Count - 2])
                : Space(spaces, source.CellId64);
            var targetSpace = Space(spaces, targetCellId);
            var approachGlobalEast = approachCell.OriginEastingMetres + 1000d;
            var approachGlobalNorth = approachCell.OriginNorthingMetres +
                                      1000d;
            targetNodeId = networkTerminalsByCell[targetCellId]
                .OrderBy(item =>
                {
                    var dx = targetSpace.OriginEastingMetres +
                             item.LocalEastMetres - approachGlobalEast;
                    var dy = targetSpace.OriginNorthingMetres +
                             item.LocalNorthMetres - approachGlobalNorth;
                    return dx * dx + dy * dy;
                })
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .First().Id;
            return reverse;
        }

        private static bool BlocksWholeCellTraversal(
            LuoyangFacilitySpatialCapability capability) =>
            string.Equals(capability.CapabilityId,
                FacilitySpatialCapabilityIds.Wall,
                StringComparison.Ordinal) ||
            string.Equals(capability.CapabilityId,
                FacilitySpatialCapabilityIds.MoatOrWater,
                StringComparison.Ordinal) ||
            string.Equals(capability.CapabilityId,
                FacilitySpatialCapabilityIds.Gate,
                StringComparison.Ordinal) ||
            string.Equals(capability.CapabilityId,
                FacilitySpatialCapabilityIds.Bridge,
                StringComparison.Ordinal);

        private static bool TryFindCellPathBetweenFormalNodes(
            LuoyangLocalNavNode source,
            LuoyangLocalNavNode target,
            IReadOnlyDictionary<string, LuoyangFacilitySpatialCapability>
                capabilities,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid,
            out IReadOnlyList<ulong> path)
        {
            if (source.CellId64 == target.CellId64)
            {
                path = new[] { source.CellId64 };
                return true;
            }
            var blocked = new HashSet<ulong>(capabilities.Values.Where(
                    BlocksWholeCellTraversal)
                .Select(item => item.CellId64));
            blocked.Remove(source.CellId64);
            blocked.Remove(target.CellId64);
            var pending = new Queue<ulong>();
            var previous = new Dictionary<ulong, ulong>();
            pending.Enqueue(source.CellId64);
            previous[source.CellId64] = 0;
            while (pending.Count > 0 &&
                   !previous.ContainsKey(target.CellId64))
            {
                var currentId = pending.Dequeue();
                var current = Space(spaces, currentId);
                var neighbours = new List<ulong>(4);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow - 1, current.GridColumn);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow, current.GridColumn - 1);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow, current.GridColumn + 1);
                AddNeighbour(neighbours, spaces, grid,
                    current.GridRow + 1, current.GridColumn);
                neighbours.Sort();
                foreach (var neighbour in neighbours)
                {
                    if (blocked.Contains(neighbour) ||
                        previous.ContainsKey(neighbour)) continue;
                    previous[neighbour] = currentId;
                    pending.Enqueue(neighbour);
                }
            }
            if (!previous.ContainsKey(target.CellId64))
            {
                path = null;
                return false;
            }
            var reverse = new List<ulong>();
            for (var cursor = target.CellId64; cursor != 0;
                 cursor = previous[cursor])
                reverse.Add(cursor);
            reverse.Reverse();
            path = reverse;
            return true;
        }

        private static void AddNeighbour(ICollection<ulong> result,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid, int row, int column)
        {
            if (row < LuoyangHumanScaleLocalMapIds.MapMinRow ||
                row > LuoyangHumanScaleLocalMapIds.MapMaxRow ||
                column < LuoyangHumanScaleLocalMapIds.MapMinColumn ||
                column > LuoyangHumanScaleLocalMapIds.MapMaxColumn) return;
            var cellId = grid.ToCellId(row, column).Value;
            if (spaces.ContainsKey(cellId)) result.Add(cellId);
        }

        private static List<LuoyangLocalRoutePoint>
            BuildAccessGeometryAlongCells(
                LuoyangLocalNavNode from,
                LuoyangLocalNavNode to,
                IReadOnlyList<ulong> cellPath,
                IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace>
                    spaces,
                IReadOnlyDictionary<ulong, LuoyangFacilityLocalFootprint>
                    blockingFootprints)
        {
            if (cellPath == null || cellPath.Count == 0 ||
                cellPath[0] != from.CellId64 ||
                cellPath[cellPath.Count - 1] != to.CellId64)
                throw new InvalidOperationException(
                    "Invalid Facility access Cell path.");
            var result = new List<LuoyangLocalRoutePoint>
            {
                Point(from, spaces)
            };
            if (cellPath.Count == 1)
            {
                result.Add(Point(to, spaces));
                return RemoveDuplicateGlobalPoints(result);
            }

            for (var index = 1; index < cellPath.Count; index++)
            {
                var current = Space(spaces, cellPath[index - 1]);
                var next = Space(spaces, cellPath[index]);
                ResolveBoundaryPoints(current, next, out var sourceEast,
                    out var sourceNorth, out var targetEast,
                    out var targetNorth, out var exitDirection);
                if (index == 1 && blockingFootprints.TryGetValue(
                        current.ParentCellId64, out var sourceFootprint) &&
                    sourceFootprint.FacilityId == from.FacilityId)
                    AppendSourcePerimeterEscape(result, current, from,
                        sourceFootprint, exitDirection, sourceEast,
                        sourceNorth);
                else
                {
                    if (index > 1 && blockingFootprints.TryGetValue(
                            current.ParentCellId64,
                            out var intermediateFootprint))
                        AppendIntermediatePerimeterDetour(result, current,
                            intermediateFootprint, exitDirection, sourceEast,
                            sourceNorth);
                    else if (index > 1)
                        result.Add(Point(current, 1000d, 1000d));
                    if (result.Count == 0 ||
                        result[result.Count - 1].CellId64 !=
                            current.ParentCellId64 ||
                        Math.Abs(result[result.Count - 1].LocalEastMetres -
                                 sourceEast) > 0.001d ||
                        Math.Abs(result[result.Count - 1].LocalNorthMetres -
                                 sourceNorth) > 0.001d)
                        result.Add(Point(current, sourceEast, sourceNorth));
                }
                result.Add(Point(next, targetEast, targetNorth));
            }
            result.Add(Point(to, spaces));
            return RemoveDuplicateGlobalPoints(result);
        }

        private static void ResolveBoundaryPoints(
            LuoyangHumanScaleLocalSpace current,
            LuoyangHumanScaleLocalSpace next,
            out double sourceEast,
            out double sourceNorth,
            out double targetEast,
            out double targetNorth,
            out int direction)
        {
            var columnStep = next.GridColumn - current.GridColumn;
            var rowStep = next.GridRow - current.GridRow;
            if (columnStep == 1 && rowStep == 0)
            {
                sourceEast = 2000d; sourceNorth = 1000d;
                targetEast = 0d; targetNorth = 1000d; direction = 0; return;
            }
            if (columnStep == -1 && rowStep == 0)
            {
                sourceEast = 0d; sourceNorth = 1000d;
                targetEast = 2000d; targetNorth = 1000d;
                direction = 2; return;
            }
            if (rowStep == -1 && columnStep == 0)
            {
                sourceEast = 1000d; sourceNorth = 2000d;
                targetEast = 1000d; targetNorth = 0d;
                direction = 1; return;
            }
            if (rowStep == 1 && columnStep == 0)
            {
                sourceEast = 1000d; sourceNorth = 0d;
                targetEast = 1000d; targetNorth = 2000d;
                direction = 3; return;
            }
            throw new InvalidOperationException(
                "Local Cell path contains a non-cardinal transition.");
        }

        private static void AppendSourcePerimeterEscape(
            ICollection<LuoyangLocalRoutePoint> result,
            LuoyangHumanScaleLocalSpace space,
            LuoyangLocalNavNode access,
            LuoyangFacilityLocalFootprint footprint,
            int exitDirection,
            double exitEast,
            double exitNorth)
        {
            var dx = access.LocalEastMetres - footprint.CenterEastMetres;
            var dy = access.LocalNorthMetres - footprint.CenterNorthMetres;
            var accessDirection = Math.Abs(dx) >= Math.Abs(dy)
                ? dx >= 0d ? 0 : 2
                : dy >= 0d ? 1 : 3;
            var accessBoundaryEast = accessDirection == 0 ? 2000d :
                accessDirection == 2 ? 0d : access.LocalEastMetres;
            var accessBoundaryNorth = accessDirection == 1 ? 2000d :
                accessDirection == 3 ? 0d : access.LocalNorthMetres;
            result.Add(Point(space, accessBoundaryEast,
                accessBoundaryNorth));
            AppendPerimeterRoute(result, space, accessDirection,
                exitDirection, exitEast, exitNorth);
        }

        private static void AppendIntermediatePerimeterDetour(
            ICollection<LuoyangLocalRoutePoint> result,
            LuoyangHumanScaleLocalSpace space,
            LuoyangFacilityLocalFootprint footprint,
            int exitDirection,
            double exitEast,
            double exitNorth)
        {
            var last = result.Last();
            var entryDirection = BoundaryDirection(last);
            if (footprint.Contains(last.LocalEastMetres,
                    last.LocalNorthMetres, -0.001d))
                throw new InvalidOperationException(
                    "A blocking footprint reaches a LocalSpace boundary: " +
                    footprint.FacilityId);
            AppendPerimeterRoute(result, space, entryDirection,
                exitDirection, exitEast, exitNorth);
        }

        private static int BoundaryDirection(LuoyangLocalRoutePoint point)
        {
            if (Math.Abs(point.LocalEastMetres - 2000d) < 0.001d) return 0;
            if (Math.Abs(point.LocalNorthMetres - 2000d) < 0.001d) return 1;
            if (Math.Abs(point.LocalEastMetres) < 0.001d) return 2;
            if (Math.Abs(point.LocalNorthMetres) < 0.001d) return 3;
            throw new InvalidOperationException(
                "A Cell transition point is not on a Cell boundary.");
        }

        private static void AppendPerimeterRoute(
            ICollection<LuoyangLocalRoutePoint> result,
            LuoyangHumanScaleLocalSpace space,
            int startDirection,
            int exitDirection,
            double exitEast,
            double exitNorth)
        {
            if (startDirection == exitDirection)
            {
                result.Add(Point(space, exitEast, exitNorth));
                return;
            }
            if ((startDirection + 2) % 4 == exitDirection)
            {
                var viaDirection = startDirection == 0 ||
                                   startDirection == 2 ? 1 : 0;
                AddSharedCorner(result, space, startDirection,
                    viaDirection);
                AddSharedCorner(result, space, viaDirection,
                    exitDirection);
            }
            else
                AddSharedCorner(result, space, startDirection,
                    exitDirection);
            result.Add(Point(space, exitEast, exitNorth));
        }

        private static void AddSharedCorner(
            ICollection<LuoyangLocalRoutePoint> result,
            LuoyangHumanScaleLocalSpace space,
            int firstDirection,
            int secondDirection)
        {
            var east = firstDirection == 0 || secondDirection == 0
                ? 2000d : 0d;
            var north = firstDirection == 1 || secondDirection == 1
                ? 2000d : 0d;
            result.Add(Point(space, east, north));
        }

        private static LuoyangLocalNavEdge CreateEdge(string id,
            LuoyangLocalNavNode from, LuoyangLocalNavNode to,
            string roadClassId, string sourceStrategicEdgeId,
            string passageFacilityId, int costPermille,
            int widthCentimetres, List<LuoyangLocalRoutePoint> geometry,
            CellGridIndex grid)
        {
            var distance = 0d;
            for (var index = 1; index < geometry.Count; index++)
            {
                var dx = geometry[index].GlobalEastingMetres -
                         geometry[index - 1].GlobalEastingMetres;
                var dy = geometry[index].GlobalNorthingMetres -
                         geometry[index - 1].GlobalNorthingMetres;
                distance += Math.Sqrt(dx * dx + dy * dy);
            }
            return new LuoyangLocalNavEdge
            {
                Id = id,
                FromNodeId = from.Id,
                ToNodeId = to.Id,
                RoadClassId = roadClassId,
                SourceStrategicEdgeId = sourceStrategicEdgeId ?? string.Empty,
                PassageFacilityId = passageFacilityId ?? string.Empty,
                FormalWorldObjectId = !string.IsNullOrEmpty(
                    passageFacilityId) ? passageFacilityId :
                    sourceStrategicEdgeId ?? string.Empty,
                TraversalConditionId = !string.IsNullOrEmpty(
                    passageFacilityId)
                    ? LocalTraversalConditionIds.FormalPassageAvailable
                    : !string.IsNullOrEmpty(sourceStrategicEdgeId)
                        ? LocalTraversalConditionIds.FormalRoadOpen
                        : LocalTraversalConditionIds.StaticWalkable,
                DistanceCentimetres = Math.Max(1,
                    checked((int)Math.Round(distance * 100d))),
                TraversalCostPermille = costPermille,
                WidthCentimetres = widthCentimetres,
                IsWalkable = true,
                CrossesStrategicCellBoundary = geometry.Select(
                    item => item.CellId64).Distinct().Count() > 1,
                Geometry = geometry
            };
        }

        private static List<LuoyangLocalRoutePoint> BuildStraightGeometry(
            LuoyangLocalNavNode from, LuoyangLocalNavNode to,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces) =>
            new List<LuoyangLocalRoutePoint>
            {
                Point(from, spaces), Point(to, spaces)
            };

        private static List<LuoyangLocalRoutePoint> BuildStrategicGeometry(
            LuoyangLocalNavNode from, LuoyangLocalNavNode to,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid)
        {
            return BuildAccessGeometry(from, to, spaces, grid);
        }

        private static List<LuoyangLocalRoutePoint> BuildAccessGeometry(
            LuoyangLocalNavNode from, LuoyangLocalNavNode to,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            CellGridIndex grid)
        {
            if (from.CellId64 == to.CellId64)
                return BuildStraightGeometry(from, to, spaces);
            var fromSpace = Space(spaces, from.CellId64);
            var toSpace = Space(spaces, to.CellId64);
            var result = new List<LuoyangLocalRoutePoint>
            {
                Point(from, spaces)
            };
            var current = fromSpace;
            var row = current.GridRow;
            var column = current.GridColumn;
            var currentGlobalNorth = result[0].GlobalNorthingMetres;
            while (column != toSpace.GridColumn)
            {
                var step = Math.Sign(toSpace.GridColumn - column);
                var next = Space(spaces,
                    grid.ToCellId(row, column + step).Value);
                var sourceEast = step > 0 ? 2000d : 0d;
                var targetEast = step > 0 ? 0d : 2000d;
                var sourceNorth = Clamp(currentGlobalNorth -
                    current.OriginNorthingMetres, 0d, 2000d);
                var targetNorth = Clamp(currentGlobalNorth -
                    next.OriginNorthingMetres, 0d, 2000d);
                result.Add(Point(current, sourceEast, sourceNorth));
                result.Add(Point(next, targetEast, targetNorth));
                current = next;
                column += step;
            }
            var targetGlobalEast = toSpace.OriginEastingMetres +
                                   to.LocalEastMetres;
            var targetColumnEast = Clamp(targetGlobalEast -
                current.OriginEastingMetres, 0d, 2000d);
            result.Add(Point(current, targetColumnEast,
                Clamp(currentGlobalNorth - current.OriginNorthingMetres,
                    0d, 2000d)));
            while (row != toSpace.GridRow)
            {
                var step = Math.Sign(toSpace.GridRow - row);
                var next = Space(spaces,
                    grid.ToCellId(row + step, column).Value);
                var sourceNorth = step > 0 ? 0d : 2000d;
                var targetNorth = step > 0 ? 2000d : 0d;
                var sourceEast = Clamp(targetGlobalEast -
                    current.OriginEastingMetres, 0d, 2000d);
                var targetEast = Clamp(targetGlobalEast -
                    next.OriginEastingMetres, 0d, 2000d);
                result.Add(Point(current, sourceEast, sourceNorth));
                result.Add(Point(next, targetEast, targetNorth));
                current = next;
                row += step;
            }
            result.Add(Point(to, spaces));
            return RemoveDuplicateGlobalPoints(result);
        }

        private static List<LuoyangLocalRoutePoint> RemoveDuplicateGlobalPoints(
            IEnumerable<LuoyangLocalRoutePoint> source)
        {
            var result = new List<LuoyangLocalRoutePoint>();
            foreach (var point in source)
            {
                if (result.Count > 0 &&
                    result[result.Count - 1].CellId64 == point.CellId64 &&
                    Math.Abs(result[result.Count - 1]
                        .GlobalEastingMetres - point.GlobalEastingMetres) <
                        0.0001d && Math.Abs(result[result.Count - 1]
                        .GlobalNorthingMetres - point.GlobalNorthingMetres) <
                        0.0001d) continue;
                point.Sequence = result.Count;
                result.Add(point);
            }
            return result;
        }

        private static List<LuoyangLocalTransitionPoint> CreateTransitions(
            IReadOnlyList<LuoyangLocalNavEdge> edges, CellGridIndex grid)
        {
            var result = new List<LuoyangLocalTransitionPoint>();
            foreach (var edge in edges)
            {
                var edgeTransitionSequence = 0;
                for (var index = 1; index < edge.Geometry.Count; index++)
                {
                    var first = edge.Geometry[index - 1];
                    var second = edge.Geometry[index];
                    if (first.CellId64 == second.CellId64) continue;
                    if (Math.Abs(first.GlobalEastingMetres -
                                 second.GlobalEastingMetres) > 0.001d ||
                        Math.Abs(first.GlobalNorthingMetres -
                                 second.GlobalNorthingMetres) > 0.001d)
                        throw new InvalidOperationException(
                            "A LocalSpace transition is not spatially " +
                            "continuous: " + edge.Id);
                    result.Add(new LuoyangLocalTransitionPoint
                    {
                        Id = "local-transition." + edge.Id + "." +
                             edgeTransitionSequence.ToString("D4",
                                 System.Globalization.CultureInfo
                                     .InvariantCulture),
                        EdgeId = edge.Id,
                        Sequence = edgeTransitionSequence++,
                        FromCellId64 = first.CellId64,
                        ToCellId64 = second.CellId64,
                        FromLocalSpaceId = first.LocalSpaceId,
                        ToLocalSpaceId = second.LocalSpaceId,
                        SourceEastMetres = first.LocalEastMetres,
                        SourceNorthMetres = first.LocalNorthMetres,
                        TargetEastMetres = second.LocalEastMetres,
                        TargetNorthMetres = second.LocalNorthMetres,
                        SourceGlobalEastingMetres = first.GlobalEastingMetres,
                        SourceGlobalNorthingMetres = first.GlobalNorthingMetres,
                        TargetGlobalEastingMetres = second.GlobalEastingMetres,
                        TargetGlobalNorthingMetres =
                            second.GlobalNorthingMetres,
                        ConnectedPathId = edge.Id,
                        FormalWorldObjectId = edge.FormalWorldObjectId,
                        TraversalConditionId = edge.TraversalConditionId
                    });
                }
            }
            return result;
        }

        private static void MarkIntersections(
            IEnumerable<LuoyangLocalNavNode> nodes,
            IEnumerable<LuoyangLocalNavEdge> edges)
        {
            var degree = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var edge in edges.Where(item =>
                         string.Equals(item.RoadClassId,
                             LuoyangHumanScaleLocalMapIds.PrimaryRoadClassId,
                             StringComparison.Ordinal) ||
                         string.Equals(item.RoadClassId,
                             LuoyangHumanScaleLocalMapIds.SecondaryRoadClassId,
                             StringComparison.Ordinal)))
            {
                degree[edge.FromNodeId] = degree.TryGetValue(edge.FromNodeId,
                    out var from) ? from + 1 : 1;
                degree[edge.ToNodeId] = degree.TryGetValue(edge.ToNodeId,
                    out var to) ? to + 1 : 1;
            }
            foreach (var node in nodes)
                if (string.Equals(node.NodeTypeId,
                        LuoyangHumanScaleLocalMapIds.RoadNodeTypeId,
                        StringComparison.Ordinal) &&
                    degree.TryGetValue(node.Id, out var count) && count >= 3)
                    node.NodeTypeId =
                        LuoyangHumanScaleLocalMapIds.IntersectionNodeTypeId;
        }

        private static string ComputeHash(
            IEnumerable<LuoyangHumanScaleLocalSpace> spaces,
            IEnumerable<LuoyangFacilitySpatialCapability> capabilities,
            IEnumerable<LuoyangFacilityLocalFootprint> footprints,
            IEnumerable<LuoyangFacilityLocalEntrance> entrances,
            IEnumerable<LuoyangLocalNavNode> nodes,
            IEnumerable<LuoyangLocalNavEdge> edges,
            IEnumerable<LuoyangLocalTransitionPoint> transitions,
            IEnumerable<string> rejectedStrategicEdgeIds)
        {
            var builder = new StringBuilder(1_000_000);
            builder.Append(LuoyangHumanScaleLocalMapIds.MapVersionId)
                .Append('|').Append(
                    LuoyangHumanScaleLocalMapIds.WorldMetresPerUnityUnit);
            foreach (var item in spaces) builder.Append('|').Append(item.Id)
                .Append(':').Append(item.ParentCellId64);
            foreach (var item in capabilities) builder.Append('|')
                .Append(item.Id).Append(':').Append(item.CapabilityId)
                .Append(':').Append(item.RequiresAccess)
                .Append(':').Append(item.HasBlockingGeometry);
            foreach (var item in footprints) builder.Append('|').Append(item.Id)
                .Append(':').Append(item.CenterEastMetres.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture))
                .Append(':').Append(item.CenterNorthMetres.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture));
            foreach (var item in entrances) builder.Append('|').Append(item.Id)
                .Append(':').Append(item.EastMetres.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture))
                .Append(':').Append(item.NorthMetres.ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture));
            foreach (var item in nodes) builder.Append('|').Append(item.Id)
                .Append(':').Append(item.NodeTypeId);
            foreach (var item in edges) builder.Append('|').Append(item.Id)
                .Append(':').Append(item.DistanceCentimetres).Append(':')
                .Append(item.SourceStrategicEdgeId).Append(':')
                .Append(item.PassageFacilityId).Append(':')
                .Append(item.TraversalConditionId);
            foreach (var item in transitions) builder.Append('|')
                .Append(item.Id).Append(':').Append(item.FromCellId64)
                .Append(':').Append(item.ToCellId64).Append(':')
                .Append(item.ConnectedPathId).Append(':')
                .Append(item.TraversalConditionId);
            foreach (var item in rejectedStrategicEdgeIds) builder.Append('|')
                .Append("rejected-strategic-edge:").Append(item);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(
                builder.ToString()));
            return string.Concat(bytes.Select(item => item.ToString("x2",
                System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static LuoyangLocalNavNode Node(string id, string typeId,
            LuoyangRoadNavigationNode strategic,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            double localEast, double localNorth) => new LuoyangLocalNavNode
        {
            Id = id,
            NodeTypeId = typeId,
            FacilityId = strategic.FacilityId,
            FacilityDefinitionId = strategic.FacilityDefinitionId,
            LocalSpaceId = Space(spaces, strategic.CellId64).Id,
            CellId64 = strategic.CellId64,
            LocalEastMetres = localEast,
            LocalNorthMetres = localNorth,
            ElevationMetres = 0d
        };

        private static LuoyangLocalRoutePoint Point(
            LuoyangLocalNavNode node,
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces) =>
            Point(Space(spaces, node.CellId64), node.LocalEastMetres,
                node.LocalNorthMetres, node.ElevationMetres);

        private static LuoyangLocalRoutePoint Point(
            LuoyangHumanScaleLocalSpace space, double east, double north,
            double elevation = 0d) => new LuoyangLocalRoutePoint
        {
            LocalSpaceId = space.Id,
            CellId64 = space.ParentCellId64,
            LocalEastMetres = Clamp(east, 0d, 2000d),
            LocalNorthMetres = Clamp(north, 0d, 2000d),
            ElevationMetres = elevation,
            GlobalEastingMetres = space.OriginEastingMetres +
                                  Clamp(east, 0d, 2000d),
            GlobalNorthingMetres = space.OriginNorthingMetres +
                                   Clamp(north, 0d, 2000d)
        };

        private static LuoyangHumanScaleLocalSpace Space(
            IReadOnlyDictionary<ulong, LuoyangHumanScaleLocalSpace> spaces,
            ulong cellId64) => spaces.TryGetValue(cellId64, out var space)
            ? space
            : throw new InvalidOperationException(
                "A Luoyang map object lies outside the LocalSpace master: " +
                cellId64);

        private static string LocalSpaceId(ulong cellId64) =>
            "local-space.luoyang.v1.cell." + cellId64;

        private static int GridDistance(int firstRow, int firstColumn,
            int secondRow, int secondColumn) =>
            Math.Abs(firstRow - secondRow) +
            Math.Abs(firstColumn - secondColumn);

        private static bool IsGate(string definitionId) => string.Equals(
                definitionId, "facility.fortification.city_gate",
                StringComparison.Ordinal) || string.Equals(definitionId,
                "facility.fortification.palace_gate", StringComparison.Ordinal) ||
            string.Equals(definitionId, "facility.military.gate",
                StringComparison.Ordinal);

        public static string ResolveCapability(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException(
                    "A Facility definition ID is required.",
                    nameof(definitionId));
            if (string.Equals(definitionId, RoadDefinitionId,
                    StringComparison.Ordinal))
                return FacilitySpatialCapabilityIds.Road;
            if (IsGate(definitionId))
                return FacilitySpatialCapabilityIds.Gate;
            if (string.Equals(definitionId, BridgeDefinitionId,
                    StringComparison.Ordinal))
                return FacilitySpatialCapabilityIds.Bridge;
            if (definitionId.IndexOf("wall", StringComparison.Ordinal) >= 0)
                return FacilitySpatialCapabilityIds.Wall;
            if (string.Equals(definitionId, "facility.public.canal",
                    StringComparison.Ordinal) ||
                definitionId.IndexOf("moat", StringComparison.Ordinal) >= 0 ||
                definitionId.IndexOf("water", StringComparison.Ordinal) >= 0)
                return FacilitySpatialCapabilityIds.MoatOrWater;
            if (definitionId.StartsWith("facility.agriculture.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.resource.",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId, "facility.public.garden",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId,
                    "facility.historical.imperial_garden",
                    StringComparison.Ordinal))
                return FacilitySpatialCapabilityIds.ProductiveLand;
            if (string.Equals(definitionId, "facility.commercial.market",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId, "facility.historical.market",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId, "facility.public.plaza",
                    StringComparison.Ordinal) ||
                string.Equals(definitionId, "facility.public.courtyard",
                    StringComparison.Ordinal))
                return FacilitySpatialCapabilityIds.OpenArea;
            return FacilitySpatialCapabilityIds.Building;
        }

        private static bool RequiresAccess(string capabilityId) =>
            !string.Equals(capabilityId, FacilitySpatialCapabilityIds.Road,
                StringComparison.Ordinal) &&
            !string.Equals(capabilityId, FacilitySpatialCapabilityIds.Wall,
                StringComparison.Ordinal) &&
            !string.Equals(capabilityId,
                FacilitySpatialCapabilityIds.MoatOrWater,
                StringComparison.Ordinal);

        private static void AddAccessPoints(
            ICollection<LuoyangFacilityLocalEntrance> result,
            string facilityId,
            string capabilityId,
            LuoyangFacilityLocalFootprint footprint,
            int rotationMilliDegrees)
        {
            if (!RequiresAccess(capabilityId)) return;
            if (string.Equals(capabilityId, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal))
            {
                AddAccessPoint(result, facilityId, footprint, "inside",
                    FacilitySpatialAccessKindIds.GateInside,
                    "local-nav.node.gate.a." + facilityId,
                    1000d, 700d, rotationMilliDegrees, true);
                AddAccessPoint(result, facilityId, footprint, "outside",
                    FacilitySpatialAccessKindIds.GateOutside,
                    "local-nav.node.gate.b." + facilityId,
                    1000d, 1300d, rotationMilliDegrees + 180_000, false);
                return;
            }
            if (string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal))
            {
                AddAccessPoint(result, facilityId, footprint, "a",
                    FacilitySpatialAccessKindIds.BridgeA,
                    "local-nav.node.bridge.a." + facilityId,
                    1000d, 700d, rotationMilliDegrees, true);
                AddAccessPoint(result, facilityId, footprint, "b",
                    FacilitySpatialAccessKindIds.BridgeB,
                    "local-nav.node.bridge.b." + facilityId,
                    1000d, 1300d, rotationMilliDegrees + 180_000, false);
                return;
            }

            var radians = rotationMilliDegrees / 1000d * Math.PI / 180d;
            var facingEast = Math.Sin(radians);
            var facingNorth = Math.Cos(radians);
            var forwardExtent = Math.Abs(facingEast) *
                                footprint.HalfExtentEastMetres +
                                Math.Abs(facingNorth) *
                                footprint.HalfExtentNorthMetres;
            var east = Clamp(footprint.CenterEastMetres + facingEast *
                (forwardExtent + EntranceClearanceMetres), 1d, 1999d);
            var north = Clamp(footprint.CenterNorthMetres + facingNorth *
                (forwardExtent + EntranceClearanceMetres), 1d, 1999d);
            var kindId = string.Equals(capabilityId,
                FacilitySpatialCapabilityIds.OpenArea,
                StringComparison.Ordinal)
                ? FacilitySpatialAccessKindIds.Area
                : string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.ProductiveLand,
                    StringComparison.Ordinal)
                    ? FacilitySpatialAccessKindIds.Work
                    : FacilitySpatialAccessKindIds.Primary;
            AddAccessPoint(result, facilityId, footprint, "primary", kindId,
                "local-nav.node.access." + facilityId, east, north,
                rotationMilliDegrees, true);
        }

        private static void AddAccessPoint(
            ICollection<LuoyangFacilityLocalEntrance> result,
            string facilityId,
            LuoyangFacilityLocalFootprint footprint,
            string suffix,
            string kindId,
            string nodeId,
            double east,
            double north,
            int facingMilliDegrees,
            bool primary)
        {
            result.Add(new LuoyangFacilityLocalEntrance
            {
                Id = "local-access." + suffix + "." + facilityId,
                FacilityId = facilityId,
                LocalSpaceId = footprint.LocalSpaceId,
                CellId64 = footprint.CellId64,
                EastMetres = Clamp(east, 0d, 2000d),
                NorthMetres = Clamp(north, 0d, 2000d),
                ElevationMetres = 0d,
                FacingMilliDegrees = facingMilliDegrees,
                EntranceTypeId = kindId,
                AccessNodeId = nodeId,
                IsPrimary = primary
            });
        }

        private static void ResolveFootprint(string definitionId,
            out double halfEast, out double halfNorth, out bool blocks)
        {
            var capabilityId = ResolveCapability(definitionId);
            blocks = string.Equals(capabilityId,
                         FacilitySpatialCapabilityIds.Building,
                         StringComparison.Ordinal) ||
                     string.Equals(capabilityId,
                         FacilitySpatialCapabilityIds.Wall,
                         StringComparison.Ordinal) ||
                     string.Equals(capabilityId,
                         FacilitySpatialCapabilityIds.MoatOrWater,
                         StringComparison.Ordinal);
            if (string.Equals(capabilityId, FacilitySpatialCapabilityIds.Road,
                    StringComparison.Ordinal))
            {
                halfEast = 900d; halfNorth = 9d; blocks = false; return;
            }
            if (string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.MoatOrWater,
                    StringComparison.Ordinal))
            {
                halfEast = 900d; halfNorth = 18d; return;
            }
            if (string.Equals(capabilityId, FacilitySpatialCapabilityIds.Wall,
                    StringComparison.Ordinal))
            {
                halfEast = 900d; halfNorth = 30d; return;
            }
            if (string.Equals(capabilityId, FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal))
            {
                halfEast = 80d; halfNorth = 60d; blocks = false; return;
            }
            if (string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal))
            {
                halfEast = 60d; halfNorth = 25d; blocks = false; return;
            }
            if (string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.ProductiveLand,
                    StringComparison.Ordinal))
            {
                halfEast = 700d; halfNorth = 700d; blocks = false; return;
            }
            if (string.Equals(capabilityId,
                    FacilitySpatialCapabilityIds.OpenArea,
                    StringComparison.Ordinal))
            {
                halfEast = 180d; halfNorth = 150d; blocks = false; return;
            }
            if (definitionId.IndexOf("palace", StringComparison.Ordinal) >= 0)
            {
                halfEast = 180d; halfNorth = 140d; return;
            }
            if (definitionId.StartsWith("facility.residential.",
                    StringComparison.Ordinal))
            {
                halfEast = 60d; halfNorth = 50d; return;
            }
            if (definitionId.StartsWith("facility.commercial.",
                    StringComparison.Ordinal) ||
                definitionId.StartsWith("facility.industry.",
                    StringComparison.Ordinal))
            {
                halfEast = 100d; halfNorth = 80d; return;
            }
            halfEast = 90d; halfNorth = 70d;
        }

        private static double Clamp(double value, double minimum,
            double maximum) => Math.Max(minimum, Math.Min(maximum, value));
        private static bool Finite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class LuoyangHumanScaleLocalRoute
    {
        internal LuoyangHumanScaleLocalRoute(string startFacilityId,
            string targetFacilityId, IReadOnlyList<string> nodeIds,
            IReadOnlyList<LuoyangLocalNavEdge> edges,
            IReadOnlyList<LuoyangLocalRoutePoint> points,
            CellRoute cellRoute = null)
        {
            StartFacilityId = startFacilityId;
            TargetFacilityId = targetFacilityId;
            NodeIds = nodeIds;
            Edges = edges;
            Points = points;
            CellRoute = cellRoute;
            DistanceCentimetres = edges.Sum(item =>
                (long)item.DistanceCentimetres);
            WeightedDistanceCentimetres = edges.Sum(item =>
                (long)item.DistanceCentimetres *
                item.TraversalCostPermille / 1000L);
        }
        public string StartFacilityId { get; }
        public string TargetFacilityId { get; }
        public IReadOnlyList<string> NodeIds { get; }
        public IReadOnlyList<LuoyangLocalNavEdge> Edges { get; }
        public IReadOnlyList<LuoyangLocalRoutePoint> Points { get; }
        public CellRoute CellRoute { get; }
        public long DistanceCentimetres { get; }
        public long WeightedDistanceCentimetres { get; }
    }

    public sealed class LuoyangHumanScaleLocalRoutePlanner
    {
        private readonly LuoyangHumanScaleLocalMapPlan _plan;
        private readonly CellTraversalPlanner _cellPlanner;

        public LuoyangHumanScaleLocalRoutePlanner(
            LuoyangHumanScaleLocalMapPlan plan)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _cellPlanner = new CellTraversalPlanner(plan.CellTraversal ??
                throw new InvalidOperationException(
                    "The Local Map has no Cell traversal authority."));
        }

        public bool TryFindRoute(string startFacilityId,
            string targetFacilityId,
            Func<string, bool> canTraverseStrategicEdge,
            Func<string, bool> canTraversePassage,
            out LuoyangHumanScaleLocalRoute route,
            out string failureReasonId)
        {
            route = null;
            if (!_plan.FacilityCapabilitiesByFacilityId.TryGetValue(
                    startFacilityId, out var start) ||
                !_plan.FacilityCapabilitiesByFacilityId.TryGetValue(
                    targetFacilityId, out var target) ||
                !_plan.EntrancesByFacilityId.ContainsKey(startFacilityId) ||
                !_plan.EntrancesByFacilityId.ContainsKey(targetFacilityId))
            {
                failureReasonId =
                    "local-route.failure.unknown-facility-entrance.v1";
                return false;
            }
            if (start.CellId64 == target.CellId64)
            {
                failureReasonId = "local-route.failure.same-location.v1";
                return false;
            }
            bool Available(CellTraversalPort port)
            {
                if (string.Equals(port.TraversalConditionId,
                        CellTraversalIds.FormalRoadConditionId,
                        StringComparison.Ordinal))
                    return canTraverseStrategicEdge == null ||
                        canTraverseStrategicEdge(port.FormalWorldObjectId);
                if (string.Equals(port.TraversalConditionId,
                        CellTraversalIds.FormalPassageConditionId,
                        StringComparison.Ordinal))
                    return canTraversePassage == null ||
                        canTraversePassage(port.FormalWorldObjectId);
                return true;
            }
            if (!_cellPlanner.TryFindRoute(start.CellId64, target.CellId64,
                    MovementCapabilityIds.Foot, Available, out var cellRoute,
                    out failureReasonId)) return false;
            route = ExpandPresentationRoute(startFacilityId,
                targetFacilityId, cellRoute);
            failureReasonId = string.Empty;
            return true;
        }

        public bool TryFindRoute(WorldState world, string startFacilityId,
            string targetFacilityId,
            out LuoyangHumanScaleLocalRoute route,
            out string failureReasonId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!LuoyangHumanScaleWorldTraversalRules.IsFacilityAccessible(
                    world, startFacilityId) ||
                !LuoyangHumanScaleWorldTraversalRules.IsFacilityAccessible(
                    world, targetFacilityId))
            {
                route = null;
                failureReasonId =
                    "local-route.failure.facility-inaccessible.v1";
                return false;
            }
            if (!_cellPlanner.TryFindRoute(
                    _plan.FacilityCapabilitiesByFacilityId[startFacilityId]
                        .CellId64,
                    _plan.FacilityCapabilitiesByFacilityId[targetFacilityId]
                        .CellId64,
                    MovementCapabilityIds.Foot,
                    port => LuoyangCellTraversalRules.IsPortAvailable(world,
                        port), out var cellRoute, out failureReasonId))
            {
                route = null;
                return false;
            }
            route = ExpandPresentationRoute(startFacilityId,
                targetFacilityId, cellRoute);
            failureReasonId = string.Empty;
            return true;
        }

        private LuoyangHumanScaleLocalRoute ExpandPresentationRoute(
            string startFacilityId, string targetFacilityId,
            CellRoute cellRoute)
        {
            var start = _plan.EntrancesByFacilityId[startFacilityId];
            var target = _plan.EntrancesByFacilityId[targetFacilityId];
            var points = new List<LuoyangLocalRoutePoint>();
            var nodeIds = new List<string>();
            var edges = new List<LuoyangLocalNavEdge>();
            for (var index = 0; index < cellRoute.Segments.Count; index++)
            {
                var segment = cellRoute.Segments[index];
                var fromEast = segment.FromEastCentimetres;
                var fromNorth = segment.FromNorthCentimetres;
                var toEast = segment.ToEastCentimetres;
                var toNorth = segment.ToNorthCentimetres;
                if (index == 0)
                {
                    fromEast = Centimetres(start.EastMetres);
                    fromNorth = Centimetres(start.NorthMetres);
                }
                if (index + 1 == cellRoute.Segments.Count)
                {
                    toEast = Centimetres(target.EastMetres);
                    toNorth = Centimetres(target.NorthMetres);
                }
                var from = Point(segment.FromCellId64, fromEast, fromNorth,
                    points.Count);
                var to = Point(segment.ToCellId64, toEast, toNorth,
                    points.Count + 1);
                var fromNodeId = index == 0 ? start.AccessNodeId :
                    AnchorId(from);
                var toNodeId = index + 1 == cellRoute.Segments.Count
                    ? target.AccessNodeId : AnchorId(to);
                if (points.Count == 0)
                {
                    points.Add(from);
                    nodeIds.Add(fromNodeId);
                }
                points.Add(to);
                nodeIds.Add(toNodeId);
                edges.Add(new LuoyangLocalNavEdge
                {
                    Id = segment.Id,
                    FromNodeId = fromNodeId,
                    ToNodeId = toNodeId,
                    RoadClassId = RoadClass(segment),
                    SourceStrategicEdgeId = string.Equals(
                        segment.TraversalConditionId,
                        CellTraversalIds.FormalRoadConditionId,
                        StringComparison.Ordinal)
                        ? segment.FormalWorldObjectId : string.Empty,
                    PassageFacilityId = string.Equals(
                        segment.TraversalConditionId,
                        CellTraversalIds.FormalPassageConditionId,
                        StringComparison.Ordinal)
                        ? segment.FormalWorldObjectId : string.Empty,
                    FormalWorldObjectId = segment.FormalWorldObjectId,
                    TraversalConditionId = segment.TraversalConditionId,
                    DistanceCentimetres = segment.DistanceCentimetres,
                    TraversalCostPermille = segment.TraversalCostPermille,
                    WidthCentimetres = 400,
                    IsWalkable = true,
                    CrossesStrategicCellBoundary =
                        segment.FromCellId64 != segment.ToCellId64,
                    Geometry = new List<LuoyangLocalRoutePoint>
                    {
                        from, to
                    }
                });
            }
            return new LuoyangHumanScaleLocalRoute(startFacilityId,
                targetFacilityId, nodeIds.ToArray(), edges.ToArray(),
                points.ToArray(), cellRoute);
        }

        private LuoyangLocalRoutePoint Point(ulong cellId64,
            int eastCentimetres, int northCentimetres, int sequence)
        {
            var space = _plan.LocalSpacesByCellId[cellId64];
            var east = eastCentimetres / 100d;
            var north = northCentimetres / 100d;
            return new LuoyangLocalRoutePoint
            {
                Sequence = sequence,
                LocalSpaceId = space.Id,
                CellId64 = cellId64,
                LocalEastMetres = east,
                LocalNorthMetres = north,
                ElevationMetres = 0d,
                GlobalEastingMetres = space.OriginEastingMetres + east,
                GlobalNorthingMetres = space.OriginNorthingMetres + north
            };
        }

        private static string AnchorId(LuoyangLocalRoutePoint point) =>
            "cell-route.anchor.v1." + point.CellId64 + "." +
            Centimetres(point.LocalEastMetres) + "." +
            Centimetres(point.LocalNorthMetres);

        private static string RoadClass(CellRouteSegment segment) =>
            string.Equals(segment.TraversalConditionId,
                CellTraversalIds.FormalRoadConditionId,
                StringComparison.Ordinal)
                ? LuoyangHumanScaleLocalMapIds.PrimaryRoadClassId
                : string.Equals(segment.TraversalConditionId,
                    CellTraversalIds.FormalPassageConditionId,
                    StringComparison.Ordinal)
                    ? LuoyangHumanScaleLocalMapIds.GatePassageRoadClassId
                    : LuoyangHumanScaleLocalMapIds
                        .FacilityAccessRoadClassId;

        private static int Centimetres(double metres) => checked(
            (int)Math.Round(metres * 100d,
                MidpointRounding.AwayFromZero));
    }

    public static class LuoyangHumanScaleWorldTraversalRules
    {
        public static bool IsFacilityAccessible(WorldState world,
            string facilityId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var facility = world.Facilities.FirstOrDefault(item => item != null &&
                string.Equals(item.Id, facilityId, StringComparison.Ordinal));
            return facility != null &&
                facility.LifecycleStatus == FacilityLifecycleStatus.Operational &&
                facility.ConditionBasisPoints > 0;
        }

        public static bool CanTraverseStrategicEdge(WorldState world,
            string edgeId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var road = world.LuoyangRoadOperationalSegments.FirstOrDefault(
                item => item != null && string.Equals(item.EdgeId, edgeId,
                    StringComparison.Ordinal));
            return road != null && road.CanTraverse;
        }

        public static bool CanTraversePassage(WorldState world,
            string facilityId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var passage = world.LuoyangPassageTraversals.FirstOrDefault(
                item => item != null && string.Equals(item.FacilityId,
                    facilityId, StringComparison.Ordinal));
            return passage != null &&
                (string.Equals(passage.TraversalStatusId,
                     LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                     StringComparison.Ordinal) ||
                 string.Equals(passage.TraversalStatusId,
                     LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                     StringComparison.Ordinal));
        }

        public static bool CanTraverseLocalEdge(WorldState world,
            LuoyangLocalNavEdge edge)
        {
            if (edge == null) return false;
            if (string.Equals(edge.TraversalConditionId,
                    LocalTraversalConditionIds.StaticWalkable,
                    StringComparison.Ordinal))
                return edge.IsWalkable;
            if (string.Equals(edge.TraversalConditionId,
                    LocalTraversalConditionIds.FormalRoadOpen,
                    StringComparison.Ordinal))
                return edge.IsWalkable && CanTraverseStrategicEdge(world,
                    edge.SourceStrategicEdgeId);
            if (string.Equals(edge.TraversalConditionId,
                    LocalTraversalConditionIds.FormalPassageAvailable,
                    StringComparison.Ordinal))
                return edge.IsWalkable && CanTraversePassage(world,
                    edge.PassageFacilityId);
            return false;
        }
    }

    public static class LuoyangLocalTargetKindIds
    {
        public const string Ground = "local-target.ground.v1";
        public const string Road = "local-target.road.v1";
        public const string Facility = "local-target.facility.v1";
        public const string Gate = "local-target.gate.v1";
        public const string Bridge = "local-target.bridge.v1";
    }

    public sealed class LuoyangResolvedLocalTarget
    {
        public string KindId { get; internal set; }
        public string FacilityId { get; internal set; }
        public string LocalNodeId { get; internal set; }
        public string LocalSpaceId { get; internal set; }
        public ulong CellId64 { get; internal set; }
        public int EastCentimetres { get; internal set; }
        public int NorthCentimetres { get; internal set; }
        public string FailureReasonId { get; internal set; }
        public bool IsValid => string.IsNullOrEmpty(FailureReasonId);
    }

    public sealed class LuoyangLocalTargetResolver
    {
        private readonly LuoyangHumanScaleLocalMapPlan _plan;

        public LuoyangLocalTargetResolver(
            LuoyangHumanScaleLocalMapPlan plan) => _plan = plan ??
            throw new ArgumentNullException(nameof(plan));

        public LuoyangResolvedLocalTarget ResolveFacility(string facilityId)
        {
            if (!_plan.FacilityCapabilitiesByFacilityId.TryGetValue(
                    facilityId, out var capability) ||
                !capability.RequiresAccess ||
                !_plan.EntrancesByFacilityId.TryGetValue(facilityId,
                    out var access))
                return Failed("local-target.failure.no-access.v1");
            var kind = string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal)
                ? LuoyangLocalTargetKindIds.Gate
                : string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal)
                    ? LuoyangLocalTargetKindIds.Bridge
                    : string.Equals(capability.CapabilityId,
                        FacilitySpatialCapabilityIds.Road,
                        StringComparison.Ordinal)
                        ? LuoyangLocalTargetKindIds.Road
                        : LuoyangLocalTargetKindIds.Facility;
            return FromAccess(kind, access);
        }

        public LuoyangResolvedLocalTarget ResolveGround(ulong cellId64,
            double eastMetres, double northMetres,
            double maximumSnapMetres = 80d)
        {
            if (!_plan.LocalSpacesByCellId.ContainsKey(cellId64) ||
                double.IsNaN(eastMetres) || double.IsInfinity(eastMetres) ||
                double.IsNaN(northMetres) ||
                double.IsInfinity(northMetres) || eastMetres < 0d ||
                eastMetres > GlobalSpatialFoundationV1.CellSizeMetres ||
                northMetres < 0d ||
                northMetres > GlobalSpatialFoundationV1.CellSizeMetres ||
                maximumSnapMetres <= 0d)
                return Failed("local-target.failure.outside-map.v1");
            if (_plan.Footprints.Any(item => item.CellId64 == cellId64 &&
                    item.BlocksPedestrian && item.Contains(eastMetres,
                        northMetres)))
                return Failed("local-target.failure.blocking-geometry.v1");
            var nearest = _plan.Nodes.Where(item =>
                    item.CellId64 == cellId64 &&
                    !string.IsNullOrEmpty(item.FacilityId))
                .Select(item => new
                {
                    Node = item,
                    DistanceSquared =
                        (item.LocalEastMetres - eastMetres) *
                        (item.LocalEastMetres - eastMetres) +
                        (item.LocalNorthMetres - northMetres) *
                        (item.LocalNorthMetres - northMetres)
                }).OrderBy(item => item.DistanceSquared)
                .ThenBy(item => item.Node.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (nearest == null || nearest.DistanceSquared >
                maximumSnapMetres * maximumSnapMetres)
                return Failed("local-target.failure.no-nearby-walkable.v1");
            return new LuoyangResolvedLocalTarget
            {
                KindId = LuoyangLocalTargetKindIds.Ground,
                FacilityId = nearest.Node.FacilityId,
                LocalNodeId = nearest.Node.Id,
                LocalSpaceId = nearest.Node.LocalSpaceId,
                CellId64 = nearest.Node.CellId64,
                EastCentimetres = Centimetres(nearest.Node.LocalEastMetres),
                NorthCentimetres = Centimetres(
                    nearest.Node.LocalNorthMetres),
                FailureReasonId = string.Empty
            };
        }

        public LuoyangResolvedLocalTarget ResolveRoad(string edgeId,
            int geometryPointIndex = -1)
        {
            if (!_plan.EdgesById.TryGetValue(edgeId, out var edge) ||
                edge.Geometry == null || edge.Geometry.Count == 0)
                return Failed("local-target.failure.unknown-road.v1");
            var index = geometryPointIndex < 0
                ? edge.Geometry.Count / 2
                : Math.Min(geometryPointIndex, edge.Geometry.Count - 1);
            var point = edge.Geometry[index];
            var from = _plan.NodesById[edge.FromNodeId];
            var to = _plan.NodesById[edge.ToNodeId];
            var facilityId = !string.IsNullOrEmpty(from.FacilityId)
                ? from.FacilityId : to.FacilityId;
            if (string.IsNullOrEmpty(facilityId))
                return Failed("local-target.failure.road-has-no-formal-anchor.v1");
            return new LuoyangResolvedLocalTarget
            {
                KindId = LuoyangLocalTargetKindIds.Road,
                FacilityId = facilityId,
                LocalNodeId = index * 2 < edge.Geometry.Count
                    ? from.Id : to.Id,
                LocalSpaceId = point.LocalSpaceId,
                CellId64 = point.CellId64,
                EastCentimetres = Centimetres(point.LocalEastMetres),
                NorthCentimetres = Centimetres(point.LocalNorthMetres),
                FailureReasonId = string.Empty
            };
        }

        private static LuoyangResolvedLocalTarget FromAccess(string kindId,
            LuoyangFacilityLocalEntrance access) =>
            new LuoyangResolvedLocalTarget
            {
                KindId = kindId,
                FacilityId = access.FacilityId,
                LocalNodeId = access.AccessNodeId,
                LocalSpaceId = access.LocalSpaceId,
                CellId64 = access.CellId64,
                EastCentimetres = Centimetres(access.EastMetres),
                NorthCentimetres = Centimetres(access.NorthMetres),
                FailureReasonId = string.Empty
            };

        private static LuoyangResolvedLocalTarget Failed(string reasonId) =>
            new LuoyangResolvedLocalTarget
            {
                KindId = string.Empty,
                FacilityId = string.Empty,
                LocalNodeId = string.Empty,
                LocalSpaceId = string.Empty,
                FailureReasonId = reasonId
            };

        private static int Centimetres(double metres) => checked(
            (int)Math.Round(metres * 100d,
                MidpointRounding.AwayFromZero));
    }

    public sealed class LuoyangLocalStreamingUpdate
    {
        public IReadOnlyList<ulong> LoadedCellIds { get; internal set; }
        public IReadOnlyList<ulong> UnloadedCellIds { get; internal set; }
        public IReadOnlyList<ulong> ResidentCellIds { get; internal set; }
        public int ResidentFacilityCount { get; internal set; }
        public int ResidentNodeCount { get; internal set; }
        public int ResidentEdgeCount { get; internal set; }
        public string MapAssetHash { get; internal set; }
    }

    public sealed class LuoyangHumanScaleStreamingSession
    {
        private readonly LuoyangHumanScaleLocalMapPlan _plan;
        private readonly HashSet<ulong> _residentCellIds = new HashSet<ulong>();

        public LuoyangHumanScaleStreamingSession(
            LuoyangHumanScaleLocalMapPlan plan) => _plan = plan ??
            throw new ArgumentNullException(nameof(plan));

        public IReadOnlyCollection<ulong> ResidentCellIds =>
            _residentCellIds;
        public string MapAssetHash => _plan.AssetHash;

        public LuoyangLocalStreamingUpdate MoveWindow(ulong centerCellId64)
        {
            var next = new HashSet<ulong>(
                LuoyangHumanScaleLocalMapRules.SelectStreamingWindow(_plan,
                    centerCellId64).Select(item => item.ParentCellId64));
            var loaded = next.Except(_residentCellIds).OrderBy(item => item)
                .ToArray();
            var unloaded = _residentCellIds.Except(next)
                .OrderBy(item => item).ToArray();
            _residentCellIds.Clear();
            foreach (var cell in next) _residentCellIds.Add(cell);
            return new LuoyangLocalStreamingUpdate
            {
                LoadedCellIds = loaded,
                UnloadedCellIds = unloaded,
                ResidentCellIds = next.OrderBy(item => item).ToArray(),
                ResidentFacilityCount = _plan.FacilityCapabilities.Count(
                    item => next.Contains(item.CellId64)),
                ResidentNodeCount = _plan.Nodes.Count(item =>
                    next.Contains(item.CellId64)),
                ResidentEdgeCount = _plan.Edges.Count(item =>
                    next.Contains(_plan.NodesById[item.FromNodeId].CellId64) ||
                    next.Contains(_plan.NodesById[item.ToNodeId].CellId64)),
                MapAssetHash = _plan.AssetHash
            };
        }
    }
}
