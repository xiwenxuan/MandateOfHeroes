using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mandate.Domain
{
    public static class Luoyang50mCountyLayoutIds
    {
        public const string SchemaId =
            "mandate.luoyang.county-layout-50m.schema.v1";
        public const string PackageId =
            "mandate.luoyang.county-layout-50m.runtime-authority.v1";
        public const string StatusId =
            "gameplay-reconstruction-review-candidate";
        public const string DirectoryName = "Luoyang50mCountyLayoutV1";
        public const string FileName =
            "luoyang_50m_county_layout_v1.json";
        public const string EntranceProvenanceId =
            "spatial-entrance.cell-id-quarter-turn.provisional.v1";
        public const string CardinalAdjacencyProvenanceId =
            "spatial-geometry.source-cardinal-adjacency.provisional.v1";
        public const string AreaGeometryProvenanceId =
            "spatial-geometry.facility-anchor-convex-hull.provisional.v1";
        public const string WallGeometryProvenanceId =
            "spatial-geometry.facility-edge.provisional.v1";
        public const string PortalGeometryProvenanceId =
            "spatial-geometry.nearest-road-boundary-portal.provisional.v1";
        public const int CellSizeMetres = 50;
        public const int RoadNodeCount = 359;
        public const int RoadEdgeCount = 334;
        public const int CanalNodeCount = 19;
        public const int CanalEdgeCount = 17;
        public const int FortificationEdgeCount = 144;
        public const int PortalCount = 4;
        public const int DistrictAreaCount = 6;
    }

    public sealed class Luoyang50mLayoutSourceFile
    {
        public Luoyang50mLayoutSourceFile(string relativePath,
            string sourcePackageId, int facilityCount, string sha256)
        {
            RelativePath = relativePath ?? string.Empty;
            SourcePackageId = new StableId(sourcePackageId).Value;
            FacilityCount = facilityCount;
            Sha256 = sha256 ?? string.Empty;
            if (string.IsNullOrWhiteSpace(RelativePath) ||
                FacilityCount <= 0 || Sha256.Length != 64)
                throw new ArgumentException("Invalid 50m layout source file.");
        }

        public string RelativePath { get; }
        public string SourcePackageId { get; }
        public int FacilityCount { get; }
        public string Sha256 { get; }
    }

    public sealed class Luoyang50mLayoutFacility
    {
        public Luoyang50mLayoutFacility(string facilityId,
            string definitionId, string displayName, string categoryId,
            string sourcePackageId, IReadOnlyList<string> sourceIds,
            ulong sourceCellId64, int sourceRow, int sourceColumn,
            string sourceSpatialPrecisionId, string historicalConfidenceId,
            int localRow, int localColumn, int widthCentimetres,
            int depthCentimetres, int heightCentimetres,
            int rotationQuarterTurns, PlanningCellDirection entranceDirection,
            string districtId, bool preservesSourceStrategicTile,
            string placementProvenanceId, string footprintProvenanceId,
            string entranceProvenanceId)
        {
            FacilityId = new StableId(facilityId).Value;
            DefinitionId = new StableId(definitionId).Value;
            DisplayName = displayName ?? string.Empty;
            CategoryId = categoryId ?? string.Empty;
            SourcePackageId = new StableId(sourcePackageId).Value;
            SourceIds = sourceIds ?? Array.Empty<string>();
            SourceCellId64 = sourceCellId64;
            SourceRow = sourceRow;
            SourceColumn = sourceColumn;
            SourceSpatialPrecisionId = sourceSpatialPrecisionId ?? string.Empty;
            HistoricalConfidenceId = historicalConfidenceId ?? string.Empty;
            LocalRow = localRow;
            LocalColumn = localColumn;
            WidthCentimetres = widthCentimetres;
            DepthCentimetres = depthCentimetres;
            HeightCentimetres = heightCentimetres;
            RotationQuarterTurns = rotationQuarterTurns;
            EntranceDirection = entranceDirection;
            DistrictId = new StableId(districtId).Value;
            PreservesSourceStrategicTile = preservesSourceStrategicTile;
            PlacementProvenanceId = new StableId(placementProvenanceId).Value;
            FootprintProvenanceId = new StableId(footprintProvenanceId).Value;
            EntranceProvenanceId = new StableId(entranceProvenanceId).Value;
            if (SourceCellId64 == 0 || SourceRow < 0 || SourceColumn < 0 ||
                LocalRow < 0 || LocalColumn < 0 || WidthCentimetres <= 0 ||
                DepthCentimetres <= 0 || HeightCentimetres < 0 ||
                RotationQuarterTurns < 0 || RotationQuarterTurns > 3 ||
                SourceIds.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCellId64));
        }

        public string FacilityId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string CategoryId { get; }
        public string SourcePackageId { get; }
        public IReadOnlyList<string> SourceIds { get; }
        public ulong SourceCellId64 { get; }
        public int SourceRow { get; }
        public int SourceColumn { get; }
        public string SourceSpatialPrecisionId { get; }
        public string HistoricalConfidenceId { get; }
        public int LocalRow { get; }
        public int LocalColumn { get; }
        public int WidthCentimetres { get; }
        public int DepthCentimetres { get; }
        public int HeightCentimetres { get; }
        public int RotationQuarterTurns { get; }
        public PlanningCellDirection EntranceDirection { get; }
        public string DistrictId { get; }
        public bool PreservesSourceStrategicTile { get; }
        public string PlacementProvenanceId { get; }
        public string FootprintProvenanceId { get; }
        public string EntranceProvenanceId { get; }
    }

    public sealed class Luoyang50mLayoutNode
    {
        public Luoyang50mLayoutNode(string nodeId, string facilityId,
            int localRow, int localColumn)
        {
            NodeId = new StableId(nodeId).Value;
            FacilityId = new StableId(facilityId).Value;
            LocalRow = localRow;
            LocalColumn = localColumn;
        }

        public string NodeId { get; }
        public string FacilityId { get; }
        public int LocalRow { get; }
        public int LocalColumn { get; }
    }

    public sealed class Luoyang50mLayoutEdge
    {
        public Luoyang50mLayoutEdge(string edgeId, string fromNodeId,
            string toNodeId, int fromLocalRow, int fromLocalColumn,
            int toLocalRow, int toLocalColumn, int sourceManhattanDistance,
            string geometryProvenanceId)
        {
            EdgeId = new StableId(edgeId).Value;
            FromNodeId = new StableId(fromNodeId).Value;
            ToNodeId = new StableId(toNodeId).Value;
            FromLocalRow = fromLocalRow;
            FromLocalColumn = fromLocalColumn;
            ToLocalRow = toLocalRow;
            ToLocalColumn = toLocalColumn;
            SourceManhattanDistance = sourceManhattanDistance;
            GeometryProvenanceId = new StableId(geometryProvenanceId).Value;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public int FromLocalRow { get; }
        public int FromLocalColumn { get; }
        public int ToLocalRow { get; }
        public int ToLocalColumn { get; }
        public int SourceManhattanDistance { get; }
        public string GeometryProvenanceId { get; }
    }

    public sealed class Luoyang50mLayoutFortification
    {
        public Luoyang50mLayoutFortification(string edgeId,
            string facilityId, string definitionId, int localRow,
            int localColumn, PlanningCellDirection direction, bool isGate,
            int heightCentimetres, int thicknessCentimetres,
            int maximumDurability, string geometryProvenanceId)
        {
            EdgeId = new StableId(edgeId).Value;
            FacilityId = new StableId(facilityId).Value;
            DefinitionId = new StableId(definitionId).Value;
            LocalRow = localRow;
            LocalColumn = localColumn;
            Direction = direction;
            IsGate = isGate;
            HeightCentimetres = heightCentimetres;
            ThicknessCentimetres = thicknessCentimetres;
            MaximumDurability = maximumDurability;
            GeometryProvenanceId = new StableId(geometryProvenanceId).Value;
        }

        public string EdgeId { get; }
        public string FacilityId { get; }
        public string DefinitionId { get; }
        public int LocalRow { get; }
        public int LocalColumn { get; }
        public PlanningCellDirection Direction { get; }
        public bool IsGate { get; }
        public int HeightCentimetres { get; }
        public int ThicknessCentimetres { get; }
        public int MaximumDurability { get; }
        public string GeometryProvenanceId { get; }
    }

    public sealed class Luoyang50mLayoutPortal
    {
        public Luoyang50mLayoutPortal(string portalId, string routeId,
            string sideId, int localRow, int localColumn,
            PlanningCellDirection inwardDirection, string anchorFacilityId,
            string neighborCountyId, string passageTypeId,
            string geometryProvenanceId)
        {
            PortalId = new StableId(portalId).Value;
            RouteId = new StableId(routeId).Value;
            SideId = sideId ?? string.Empty;
            LocalRow = localRow;
            LocalColumn = localColumn;
            InwardDirection = inwardDirection;
            AnchorFacilityId = new StableId(anchorFacilityId).Value;
            NeighborCountyId = new StableId(neighborCountyId).Value;
            PassageTypeId = new StableId(passageTypeId).Value;
            GeometryProvenanceId = new StableId(geometryProvenanceId).Value;
        }

        public string PortalId { get; }
        public string RouteId { get; }
        public string SideId { get; }
        public int LocalRow { get; }
        public int LocalColumn { get; }
        public PlanningCellDirection InwardDirection { get; }
        public string AnchorFacilityId { get; }
        public string NeighborCountyId { get; }
        public string PassageTypeId { get; }
        public string GeometryProvenanceId { get; }
    }

    public sealed class Luoyang50mLayoutArea
    {
        public Luoyang50mLayoutArea(string urbanAreaId, string districtId,
            int facilityCount, int minimumRow, int maximumRow,
            int minimumColumn, int maximumColumn,
            IReadOnlyList<PlanningCellCoord> hullCells,
            string geometryProvenanceId, string statusId)
        {
            UrbanAreaId = new StableId(urbanAreaId).Value;
            DistrictId = new StableId(districtId).Value;
            FacilityCount = facilityCount;
            MinimumRow = minimumRow;
            MaximumRow = maximumRow;
            MinimumColumn = minimumColumn;
            MaximumColumn = maximumColumn;
            HullCells = hullCells ?? Array.Empty<PlanningCellCoord>();
            GeometryProvenanceId = new StableId(geometryProvenanceId).Value;
            StatusId = new StableId(statusId).Value;
        }

        public string UrbanAreaId { get; }
        public string DistrictId { get; }
        public int FacilityCount { get; }
        public int MinimumRow { get; }
        public int MaximumRow { get; }
        public int MinimumColumn { get; }
        public int MaximumColumn { get; }
        public IReadOnlyList<PlanningCellCoord> HullCells { get; }
        public string GeometryProvenanceId { get; }
        public string StatusId { get; }
    }

    public sealed class Luoyang50mCountyLayoutPackage
    {
        public Luoyang50mCountyLayoutPackage(string schemaId,
            string packageId, string statusId, string countyId,
            string historicalPlacementGateId, int rows, int columns,
            int cellSizeMetres, int minimumStrategicRow,
            int minimumStrategicColumn,
            IReadOnlyList<Luoyang50mLayoutSourceFile> sourceFiles,
            IReadOnlyList<Luoyang50mLayoutFacility> facilities,
            IReadOnlyList<Luoyang50mLayoutNode> roadNodes,
            IReadOnlyList<Luoyang50mLayoutEdge> roadEdges,
            IReadOnlyList<Luoyang50mLayoutNode> canalNodes,
            IReadOnlyList<Luoyang50mLayoutEdge> canalEdges,
            IReadOnlyList<Luoyang50mLayoutFortification> fortifications,
            IReadOnlyList<Luoyang50mLayoutPortal> portals,
            IReadOnlyList<Luoyang50mLayoutArea> districtAreas,
            Luoyang50mLayoutArea urbanAreaCandidate,
            string declaredLayoutFingerprint)
        {
            SchemaId = schemaId ?? string.Empty;
            PackageId = packageId ?? string.Empty;
            StatusId = statusId ?? string.Empty;
            CountyId = countyId ?? string.Empty;
            HistoricalPlacementGateId = historicalPlacementGateId ?? string.Empty;
            Rows = rows;
            Columns = columns;
            CellSizeMetres = cellSizeMetres;
            MinimumStrategicRow = minimumStrategicRow;
            MinimumStrategicColumn = minimumStrategicColumn;
            SourceFiles = sourceFiles ?? Array.Empty<Luoyang50mLayoutSourceFile>();
            Facilities = facilities ?? Array.Empty<Luoyang50mLayoutFacility>();
            RoadNodes = roadNodes ?? Array.Empty<Luoyang50mLayoutNode>();
            RoadEdges = roadEdges ?? Array.Empty<Luoyang50mLayoutEdge>();
            CanalNodes = canalNodes ?? Array.Empty<Luoyang50mLayoutNode>();
            CanalEdges = canalEdges ?? Array.Empty<Luoyang50mLayoutEdge>();
            Fortifications = fortifications ??
                Array.Empty<Luoyang50mLayoutFortification>();
            Portals = portals ?? Array.Empty<Luoyang50mLayoutPortal>();
            DistrictAreas = districtAreas ?? Array.Empty<Luoyang50mLayoutArea>();
            UrbanAreaCandidate = urbanAreaCandidate ?? throw new ArgumentNullException(
                nameof(urbanAreaCandidate));
            DeclaredLayoutFingerprint = declaredLayoutFingerprint ?? string.Empty;
            FacilitiesById = Facilities.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            RoadNodesById = RoadNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            CanalNodesById = CanalNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            Validate();
            RuntimeDeterministicHash = ComputeRuntimeHash();
        }

        public string SchemaId { get; }
        public string PackageId { get; }
        public string StatusId { get; }
        public string CountyId { get; }
        public string HistoricalPlacementGateId { get; }
        public int Rows { get; }
        public int Columns { get; }
        public int CellSizeMetres { get; }
        public int MinimumStrategicRow { get; }
        public int MinimumStrategicColumn { get; }
        public IReadOnlyList<Luoyang50mLayoutSourceFile> SourceFiles { get; }
        public IReadOnlyList<Luoyang50mLayoutFacility> Facilities { get; }
        public IReadOnlyDictionary<string, Luoyang50mLayoutFacility>
            FacilitiesById { get; }
        public IReadOnlyList<Luoyang50mLayoutNode> RoadNodes { get; }
        public IReadOnlyDictionary<string, Luoyang50mLayoutNode> RoadNodesById
            { get; }
        public IReadOnlyList<Luoyang50mLayoutEdge> RoadEdges { get; }
        public IReadOnlyList<Luoyang50mLayoutNode> CanalNodes { get; }
        public IReadOnlyDictionary<string, Luoyang50mLayoutNode> CanalNodesById
            { get; }
        public IReadOnlyList<Luoyang50mLayoutEdge> CanalEdges { get; }
        public IReadOnlyList<Luoyang50mLayoutFortification> Fortifications
            { get; }
        public IReadOnlyList<Luoyang50mLayoutPortal> Portals { get; }
        public IReadOnlyList<Luoyang50mLayoutArea> DistrictAreas { get; }
        public Luoyang50mLayoutArea UrbanAreaCandidate { get; }
        public string DeclaredLayoutFingerprint { get; }
        public string RuntimeDeterministicHash { get; }
        public bool IsRuntimeAuthoritative => true;
        public bool IsHistoricallyExact => false;

        private void Validate()
        {
            if (!string.Equals(SchemaId, Luoyang50mCountyLayoutIds.SchemaId,
                    StringComparison.Ordinal) ||
                !string.Equals(PackageId, Luoyang50mCountyLayoutIds.PackageId,
                    StringComparison.Ordinal) ||
                !string.Equals(StatusId, Luoyang50mCountyLayoutIds.StatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(CountyId,
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    StringComparison.Ordinal) ||
                Rows != Luoyang50mCountySpatialPrototypeIds.Rows ||
                Columns != Luoyang50mCountySpatialPrototypeIds.Columns ||
                CellSizeMetres != Luoyang50mCountyLayoutIds.CellSizeMetres ||
                SourceFiles.Sum(item => item.FacilityCount) !=
                    Luoyang50mCountySpatialPrototypeIds.FacilityCount ||
                DeclaredLayoutFingerprint.Length != 64)
                throw new InvalidOperationException(
                    "Invalid Luoyang 50m layout package header.");
            if (Facilities.Count !=
                    Luoyang50mCountySpatialPrototypeIds.FacilityCount ||
                FacilitiesById.Count != Facilities.Count ||
                Facilities.Select(item => item.SourceCellId64).Distinct()
                    .Count() != Facilities.Count ||
                Facilities.Any(item => !Contains(item.LocalRow,
                    item.LocalColumn)))
                throw new InvalidOperationException(
                    "Invalid Luoyang 50m Facility layout coverage.");
            if (RoadNodes.Count != Luoyang50mCountyLayoutIds.RoadNodeCount ||
                RoadEdges.Count != Luoyang50mCountyLayoutIds.RoadEdgeCount ||
                CanalNodes.Count != Luoyang50mCountyLayoutIds.CanalNodeCount ||
                CanalEdges.Count != Luoyang50mCountyLayoutIds.CanalEdgeCount ||
                Fortifications.Count !=
                    Luoyang50mCountyLayoutIds.FortificationEdgeCount ||
                Portals.Count != Luoyang50mCountyLayoutIds.PortalCount ||
                DistrictAreas.Count !=
                    Luoyang50mCountyLayoutIds.DistrictAreaCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang 50m network or area counts.");
            if (RoadNodes.Any(item => !Contains(item.LocalRow,
                    item.LocalColumn)) ||
                CanalNodes.Any(item => !Contains(item.LocalRow,
                    item.LocalColumn)) ||
                Fortifications.Any(item => !Contains(item.LocalRow,
                    item.LocalColumn)) ||
                Portals.Any(item => !Contains(item.LocalRow,
                    item.LocalColumn)) ||
                DistrictAreas.Any(item => item.FacilityCount <= 0 ||
                    item.HullCells.Count < 3 || item.HullCells.Any(cell =>
                        !Contains(cell.Row, cell.Column))) ||
                UrbanAreaCandidate.FacilityCount != Facilities.Count ||
                UrbanAreaCandidate.HullCells.Count < 3)
                throw new InvalidOperationException(
                    "Luoyang 50m layout geometry is out of bounds.");
        }

        private bool Contains(int row, int column) => row >= 0 && row < Rows &&
            column >= 0 && column < Columns;

        private string ComputeRuntimeHash()
        {
            var builder = new StringBuilder();
            builder.Append(SchemaId).Append('|').Append(PackageId).Append('|')
                .Append(StatusId).Append('|').Append(CountyId).Append('|')
                .Append(Rows).Append('|').Append(Columns).Append('|')
                .Append(CellSizeMetres);
            foreach (var item in Facilities.OrderBy(value =>
                         value.FacilityId, StringComparer.Ordinal))
                builder.Append("|F:").Append(item.FacilityId).Append(':')
                    .Append(item.SourceCellId64).Append(':')
                    .Append(item.LocalRow).Append(':')
                    .Append(item.LocalColumn).Append(':')
                    .Append(item.WidthCentimetres).Append(':')
                    .Append(item.DepthCentimetres).Append(':')
                    .Append(item.RotationQuarterTurns).Append(':')
                    .Append(item.DistrictId);
            foreach (var edge in RoadEdges.Concat(CanalEdges).OrderBy(
                         item => item.EdgeId, StringComparer.Ordinal))
                builder.Append("|E:").Append(edge.EdgeId).Append(':')
                    .Append(edge.FromLocalRow).Append(':')
                    .Append(edge.FromLocalColumn).Append(':')
                    .Append(edge.ToLocalRow).Append(':')
                    .Append(edge.ToLocalColumn);
            foreach (var wall in Fortifications.OrderBy(item => item.EdgeId,
                         StringComparer.Ordinal))
                builder.Append("|W:").Append(wall.EdgeId).Append(':')
                    .Append(wall.LocalRow).Append(':').Append(wall.LocalColumn)
                    .Append(':').Append((int)wall.Direction);
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        builder.ToString()))
                    .Select(value => value.ToString("x2")));
        }
    }
}
