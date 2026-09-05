using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mandate.Domain;
using Newtonsoft.Json.Linq;

namespace Mandate.Persistence
{
    public sealed class Luoyang50mCountyLayoutPackageSource
    {
        public Luoyang50mCountyLayoutPackageSource(string worldMapRoot)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("World map root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            PackagePath = Path.Combine(Root,
                Luoyang50mCountyLayoutIds.DirectoryName,
                Luoyang50mCountyLayoutIds.FileName);
            if (!File.Exists(PackagePath))
                throw new FileNotFoundException(
                    "Luoyang 50m county layout package is missing.",
                    PackagePath);

            var root = JObject.Parse(File.ReadAllText(PackagePath));
            ValidateSemantics(root);
            var computedFingerprint = ComputeDeclaredFingerprint(root);
            if (!string.Equals(computedFingerprint,
                    Text(root, "layout_fingerprint_sha256"),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Luoyang layout package fingerprint mismatch.");
            var grid = Object(root, "grid");
            var sourceFiles = Array(root, "source_files").Select(item =>
                new Luoyang50mLayoutSourceFile(Text(item, "relative_path"),
                    Text(item, "source_package_id"),
                    Integer(item, "facility_count"), Text(item, "sha256")))
                .ToArray();
            var facilities = Array(root, "facilities").Select(ReadFacility)
                .ToArray();
            var roadNodes = Array(root, "road_nodes").Select(ReadNode)
                .ToArray();
            var roadEdges = Array(root, "road_edges").Select(ReadEdge)
                .ToArray();
            var canalNodes = Array(root, "canal_nodes").Select(ReadNode)
                .ToArray();
            var canalEdges = Array(root, "canal_edges").Select(ReadEdge)
                .ToArray();
            var fortifications = Array(root, "fortification_edges").Select(
                ReadFortification).ToArray();
            var portals = Array(root, "portals").Select(ReadPortal).ToArray();
            var areas = Array(root, "district_areas").Select(ReadArea)
                .ToArray();
            var urbanArea = ReadArea(Object(root, "urban_area_candidate"));
            Package = new Luoyang50mCountyLayoutPackage(
                Text(root, "schema_id"), Text(root, "package_id"),
                Text(root, "status_id"), Text(root, "county_id"),
                Text(root, "historical_placement_gate_id"),
                Integer(grid, "row_count"), Integer(grid, "column_count"),
                Integer(grid, "cell_size_metres"),
                Integer(grid, "minimum_strategic_row"),
                Integer(grid, "minimum_strategic_column"), sourceFiles,
                facilities, roadNodes, roadEdges, canalNodes, canalEdges,
                fortifications, portals, areas, urbanArea,
                Text(root, "layout_fingerprint_sha256"));
            ValidateDeclaredCounts(Object(root, "counts"));
            ValidateSourceFiles();
            ValidateAgainstFormalFacilitySources();
            ValidateNetworksAndAreas();
            PackageFileSha256 = ComputeFileHash(PackagePath);
        }

        public string Root { get; }
        public string PackagePath { get; }
        public string PackageFileSha256 { get; }
        public Luoyang50mCountyLayoutPackage Package { get; }

        private static Luoyang50mLayoutFacility ReadFacility(JToken item) =>
            new Luoyang50mLayoutFacility(Text(item, "facility_id"),
                Text(item, "definition_id"), Text(item, "display_name"),
                Text(item, "category_id"), Text(item, "source_package_id"),
                Array(item, "source_ids").Select(value =>
                    value.Value<string>() ?? string.Empty).ToArray(),
                UnsignedLong(item, "source_cell_id64"),
                Integer(item, "source_row"), Integer(item, "source_column"),
                Text(item, "source_spatial_precision_id"),
                Text(item, "historical_confidence_id"),
                Integer(item, "local_row"), Integer(item, "local_column"),
                Integer(item, "width_centimetres"),
                Integer(item, "depth_centimetres"),
                Integer(item, "height_centimetres"),
                Integer(item, "rotation_quarter_turns"),
                Direction(item, "entrance_direction_id"),
                Text(item, "district_id"),
                Boolean(item, "preserves_source_strategic_tile"),
                Text(item, "placement_provenance_id"),
                Text(item, "footprint_provenance_id"),
                Text(item, "entrance_provenance_id"));

        private static Luoyang50mLayoutNode ReadNode(JToken item) =>
            new Luoyang50mLayoutNode(Text(item, "node_id"),
                Text(item, "facility_id"), Integer(item, "local_row"),
                Integer(item, "local_column"));

        private static Luoyang50mLayoutEdge ReadEdge(JToken item) =>
            new Luoyang50mLayoutEdge(Text(item, "edge_id"),
                Text(item, "from_node_id"), Text(item, "to_node_id"),
                Integer(item, "from_local_row"),
                Integer(item, "from_local_column"),
                Integer(item, "to_local_row"),
                Integer(item, "to_local_column"),
                Integer(item, "source_manhattan_distance"),
                Text(item, "geometry_provenance_id"));

        private static Luoyang50mLayoutFortification ReadFortification(
            JToken item) => new Luoyang50mLayoutFortification(
                Text(item, "edge_id"), Text(item, "facility_id"),
                Text(item, "definition_id"), Integer(item, "local_row"),
                Integer(item, "local_column"), Direction(item, "direction_id"),
                Boolean(item, "is_gate"), Integer(item, "height_centimetres"),
                Integer(item, "thickness_centimetres"),
                Integer(item, "maximum_durability"),
                Text(item, "geometry_provenance_id"));

        private static Luoyang50mLayoutPortal ReadPortal(JToken item) =>
            new Luoyang50mLayoutPortal(Text(item, "portal_id"),
                Text(item, "route_id"), Text(item, "side_id"),
                Integer(item, "local_row"), Integer(item, "local_column"),
                Direction(item, "inward_direction_id"),
                Text(item, "anchor_facility_id"),
                Text(item, "neighbor_county_id"),
                Text(item, "passage_type_id"),
                Text(item, "geometry_provenance_id"));

        private static Luoyang50mLayoutArea ReadArea(JToken item) =>
            new Luoyang50mLayoutArea(Text(item, "urban_area_id"),
                Text(item, "district_id"), Integer(item, "facility_count"),
                Integer(item, "minimum_row"), Integer(item, "maximum_row"),
                Integer(item, "minimum_column"),
                Integer(item, "maximum_column"),
                Array(item, "hull_cells").Select(point =>
                    new PlanningCellCoord(Integer(point, "row"),
                        Integer(point, "column"))).ToArray(),
                Text(item, "geometry_provenance_id"),
                Text(item, "status_id"));

        private void ValidateSemantics(JObject root)
        {
            var semantics = Object(root, "semantics");
            if (!Boolean(semantics, "runtime_authoritative") ||
                Boolean(semantics, "historically_exact") ||
                Boolean(semantics, "mutates_world_state") ||
                Boolean(semantics, "changes_save_schema"))
                throw new InvalidDataException(
                    "Luoyang layout semantics blur runtime authority and historical fact.");
        }

        private void ValidateDeclaredCounts(JToken counts)
        {
            if (Integer(counts, "facility_count") != Package.Facilities.Count ||
                Integer(counts, "road_node_count") != Package.RoadNodes.Count ||
                Integer(counts, "road_edge_count") != Package.RoadEdges.Count ||
                Integer(counts, "canal_node_count") != Package.CanalNodes.Count ||
                Integer(counts, "canal_edge_count") != Package.CanalEdges.Count ||
                Integer(counts, "fortification_edge_count") !=
                    Package.Fortifications.Count ||
                Integer(counts, "portal_count") != Package.Portals.Count ||
                Integer(counts, "district_area_count") !=
                    Package.DistrictAreas.Count)
                throw new InvalidDataException(
                    "Luoyang layout declared counts do not match payload.");
        }

        private void ValidateSourceFiles()
        {
            var rootPrefix = Root.TrimEnd(Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            foreach (var source in Package.SourceFiles)
            {
                var path = Path.GetFullPath(Path.Combine(Root,
                    source.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!path.StartsWith(rootPrefix,
                        StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(path) || !string.Equals(ComputeFileHash(path),
                        source.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "Luoyang layout source file fingerprint mismatch: " +
                        source.RelativePath);
            }
        }

        private void ValidateAgainstFormalFacilitySources()
        {
            var localMap = new LuoyangHumanScaleLocalMapPlanSource(Root);
            var formal = localMap.Performance.Facilities.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            if (formal.Count != Package.Facilities.Count)
                throw new InvalidDataException(
                    "Luoyang layout does not cover the formal Facility catalog.");
            foreach (var item in Package.Facilities)
            {
                if (!formal.TryGetValue(item.FacilityId, out var source) ||
                    !string.Equals(item.DefinitionId,
                        source.FacilityDefinitionId, StringComparison.Ordinal) ||
                    !string.Equals(item.DisplayName, source.DisplayName,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.CategoryId, source.CategoryId,
                        StringComparison.Ordinal) ||
                    item.SourceCellId64 != source.CellId64 ||
                    item.SourceRow != source.GridRow ||
                    item.SourceColumn != source.GridColumn ||
                    !string.Equals(item.SourceSpatialPrecisionId,
                        source.SpatialPrecisionId, StringComparison.Ordinal) ||
                    !string.Equals(item.HistoricalConfidenceId,
                        source.HistoricalConfidenceId, StringComparison.Ordinal) ||
                    item.RotationQuarterTurns !=
                        (int)(source.CellId64 % 4) ||
                    item.EntranceDirection !=
                        (PlanningCellDirection)(source.CellId64 % 4) ||
                    !string.Equals(item.DistrictId,
                        localMap.Composition.AnchorsByFacilityId[
                            item.FacilityId].DistrictId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.PlacementProvenanceId,
                        Luoyang50mCountySpatialPrototypeIds.PlacementProvenanceId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.FootprintProvenanceId,
                        Luoyang50mCountySpatialPrototypeIds.FootprintProvenanceId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.EntranceProvenanceId,
                        Luoyang50mCountyLayoutIds.EntranceProvenanceId,
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Luoyang 50m layout diverges from formal Facility identity: " +
                        item.FacilityId);
            }
        }

        private void ValidateNetworksAndAreas()
        {
            ValidateNetwork(Package.RoadNodes, Package.RoadEdges,
                "facility.public.road");
            ValidateNetwork(Package.CanalNodes, Package.CanalEdges,
                "facility.public.canal");

            var walls = Package.Fortifications.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            foreach (var facility in Package.Facilities.Where(item =>
                         item.DefinitionId.StartsWith(
                             "facility.fortification.",
                             StringComparison.Ordinal)))
            {
                if (!walls.TryGetValue(facility.FacilityId, out var wall) ||
                    wall.LocalRow != facility.LocalRow ||
                    wall.LocalColumn != facility.LocalColumn ||
                    !string.Equals(wall.DefinitionId, facility.DefinitionId,
                        StringComparison.Ordinal) ||
                    !string.Equals(wall.GeometryProvenanceId,
                        Luoyang50mCountyLayoutIds.WallGeometryProvenanceId,
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Invalid Luoyang fortification layout: " +
                        facility.FacilityId);
            }

            if (Package.Portals.Select(item => item.SideId).Distinct(
                    StringComparer.Ordinal).Count() != 4)
                throw new InvalidDataException(
                    "Luoyang layout requires four unique boundary portals.");
            foreach (var portal in Package.Portals)
            {
                if (!Package.FacilitiesById.TryGetValue(
                        portal.AnchorFacilityId, out var anchor) ||
                    anchor.DefinitionId != "facility.public.road" ||
                    !IsBoundaryPortal(portal) ||
                    !string.Equals(portal.GeometryProvenanceId,
                        Luoyang50mCountyLayoutIds.PortalGeometryProvenanceId,
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Invalid Luoyang county boundary Portal: " +
                        portal.PortalId);
            }

            foreach (var area in Package.DistrictAreas)
            {
                var points = Package.Facilities.Where(item =>
                        string.Equals(item.DistrictId, area.DistrictId,
                            StringComparison.Ordinal))
                    .Select(item => new PlanningCellCoord(item.LocalRow,
                        item.LocalColumn)).ToArray();
                ValidateArea(area, points);
            }
            ValidateArea(Package.UrbanAreaCandidate,
                Package.Facilities.Select(item => new PlanningCellCoord(
                    item.LocalRow, item.LocalColumn)).ToArray());
        }

        private void ValidateNetwork(IReadOnlyList<Luoyang50mLayoutNode> nodes,
            IReadOnlyList<Luoyang50mLayoutEdge> edges, string definitionId)
        {
            var nodeById = nodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                if (!Package.FacilitiesById.TryGetValue(node.FacilityId,
                        out var facility) ||
                    !string.Equals(facility.DefinitionId, definitionId,
                        StringComparison.Ordinal) ||
                    node.LocalRow != facility.LocalRow ||
                    node.LocalColumn != facility.LocalColumn)
                    throw new InvalidDataException(
                        "Invalid Luoyang layout network node: " + node.NodeId);
            }
            var actualPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in edges)
            {
                if (!nodeById.TryGetValue(edge.FromNodeId, out var first) ||
                    !nodeById.TryGetValue(edge.ToNodeId, out var second) ||
                    edge.FromLocalRow != first.LocalRow ||
                    edge.FromLocalColumn != first.LocalColumn ||
                    edge.ToLocalRow != second.LocalRow ||
                    edge.ToLocalColumn != second.LocalColumn ||
                    edge.SourceManhattanDistance != 1 ||
                    !string.Equals(edge.GeometryProvenanceId,
                        Luoyang50mCountyLayoutIds.CardinalAdjacencyProvenanceId,
                        StringComparison.Ordinal))
                    throw new InvalidDataException(
                        "Invalid Luoyang layout network edge: " + edge.EdgeId);
                var a = Package.FacilitiesById[first.FacilityId];
                var b = Package.FacilitiesById[second.FacilityId];
                if (Math.Abs(a.SourceRow - b.SourceRow) +
                    Math.Abs(a.SourceColumn - b.SourceColumn) != 1 ||
                    (a.LocalRow != b.LocalRow &&
                     a.LocalColumn != b.LocalColumn))
                    throw new InvalidDataException(
                        "Luoyang network edge is not a cardinal source edge: " +
                        edge.EdgeId);
                actualPairs.Add(Pair(a.FacilityId, b.FacilityId));
            }
            var bySource = nodes.Select(item =>
                    Package.FacilitiesById[item.FacilityId])
                .ToDictionary(item => item.SourceRow + "," +
                                      item.SourceColumn,
                    StringComparer.Ordinal);
            var expectedPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in bySource.Values)
            foreach (var offset in new[] { new[] { 0, 1 }, new[] { 1, 0 } })
                if (bySource.TryGetValue((item.SourceRow + offset[0]) + "," +
                                         (item.SourceColumn + offset[1]),
                        out var other))
                    expectedPairs.Add(Pair(item.FacilityId, other.FacilityId));
            if (!actualPairs.SetEquals(expectedPairs))
                throw new InvalidDataException(
                    "Luoyang layout network adjacency is incomplete.");
        }

        private static string Pair(string first, string second) =>
            string.CompareOrdinal(first, second) < 0
                ? first + "|" + second
                : second + "|" + first;

        private bool IsBoundaryPortal(Luoyang50mLayoutPortal portal)
        {
            switch (portal.SideId)
            {
                case "north":
                    return portal.LocalRow == 0 && portal.InwardDirection ==
                        PlanningCellDirection.South;
                case "south":
                    return portal.LocalRow == Package.Rows - 1 &&
                           portal.InwardDirection == PlanningCellDirection.North;
                case "west":
                    return portal.LocalColumn == 0 && portal.InwardDirection ==
                        PlanningCellDirection.East;
                case "east":
                    return portal.LocalColumn == Package.Columns - 1 &&
                           portal.InwardDirection == PlanningCellDirection.West;
                default:
                    return false;
            }
        }

        private static void ValidateArea(Luoyang50mLayoutArea area,
            IReadOnlyList<PlanningCellCoord> points)
        {
            var expectedHull = BuildHull(points);
            if (area.FacilityCount != points.Count ||
                area.MinimumRow != points.Min(item => item.Row) ||
                area.MaximumRow != points.Max(item => item.Row) ||
                area.MinimumColumn != points.Min(item => item.Column) ||
                area.MaximumColumn != points.Max(item => item.Column) ||
                !area.HullCells.SequenceEqual(expectedHull) ||
                !string.Equals(area.GeometryProvenanceId,
                    Luoyang50mCountyLayoutIds.AreaGeometryProvenanceId,
                    StringComparison.Ordinal) ||
                !string.Equals(area.StatusId,
                    Luoyang50mCountyLayoutIds.StatusId,
                    StringComparison.Ordinal))
                throw new InvalidDataException(
                    "Invalid Luoyang UrbanArea candidate: " + area.UrbanAreaId);
        }

        private static IReadOnlyList<PlanningCellCoord> BuildHull(
            IEnumerable<PlanningCellCoord> input)
        {
            var points = input.Distinct().OrderBy(item => item.Column)
                .ThenBy(item => item.Row).ToArray();
            if (points.Length <= 1) return points;
            var lower = new List<PlanningCellCoord>();
            foreach (var point in points)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2],
                           lower[lower.Count - 1], point) <= 0)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(point);
            }
            var upper = new List<PlanningCellCoord>();
            for (var index = points.Length - 1; index >= 0; index--)
            {
                var point = points[index];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2],
                           upper[upper.Count - 1], point) <= 0)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(point);
            }
            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        private static long Cross(PlanningCellCoord origin,
            PlanningCellCoord first, PlanningCellCoord second) =>
            (long)(first.Column - origin.Column) *
            (second.Row - origin.Row) -
            (long)(first.Row - origin.Row) *
            (second.Column - origin.Column);

        private static PlanningCellDirection Direction(JToken item,
            string property)
        {
            switch (Text(item, property))
            {
                case "north": return PlanningCellDirection.North;
                case "east": return PlanningCellDirection.East;
                case "south": return PlanningCellDirection.South;
                case "west": return PlanningCellDirection.West;
                default: throw new InvalidDataException(
                    "Invalid PlanningCell direction: " + Text(item, property));
            }
        }

        private static JObject Object(JToken item, string property) =>
            item[property] as JObject ?? throw new InvalidDataException(
                "Missing JSON object: " + property);

        private static JArray Array(JToken item, string property) =>
            item[property] as JArray ?? throw new InvalidDataException(
                "Missing JSON array: " + property);

        private static string Text(JToken item, string property)
        {
            var value = item[property]?.Value<string>();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException(
                    "Missing JSON text: " + property);
            return value;
        }

        private static int Integer(JToken item, string property) =>
            item[property]?.Value<int>() ?? throw new InvalidDataException(
                "Missing JSON integer: " + property);

        private static ulong UnsignedLong(JToken item, string property) =>
            item[property]?.Value<ulong>() ?? throw new InvalidDataException(
                "Missing JSON unsigned integer: " + property);

        private static bool Boolean(JToken item, string property) =>
            item[property]?.Value<bool>() ?? throw new InvalidDataException(
                "Missing JSON boolean: " + property);

        private static string ComputeFileHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2")));
        }

        private static string ComputeDeclaredFingerprint(JObject root)
        {
            var grid = Object(root, "grid");
            var lines = new List<string>
            {
                string.Join("|", "H", Text(root, "schema_id"),
                    Text(root, "package_id"), Text(root, "status_id"),
                    Text(root, "county_id"),
                    Integer(grid, "row_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(grid, "column_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(grid, "cell_size_metres").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(grid, "minimum_strategic_row").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(grid, "minimum_strategic_column").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "facility_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "road_node_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "road_edge_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "canal_node_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "canal_edge_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"),
                        "fortification_edge_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"), "portal_count").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(Object(root, "counts"),
                        "district_area_count").ToString(
                        CultureInfo.InvariantCulture))
            };
            foreach (var item in Array(root, "facilities"))
                lines.Add(string.Join("|", "F", Text(item, "facility_id"),
                    Text(item, "definition_id"),
                    Text(item, "source_package_id"),
                    string.Join(",", Array(item, "source_ids").Select(
                        value => value.Value<string>() ?? string.Empty)),
                    UnsignedLong(item, "source_cell_id64").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "source_row").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "source_column").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "local_row").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "local_column").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "width_centimetres").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "depth_centimetres").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "height_centimetres").ToString(
                        CultureInfo.InvariantCulture),
                    Integer(item, "rotation_quarter_turns").ToString(
                        CultureInfo.InvariantCulture),
                    Text(item, "entrance_direction_id"),
                    Text(item, "district_id"),
                    Text(item, "source_spatial_precision_id"),
                    Text(item, "historical_confidence_id"),
                    Text(item, "placement_provenance_id"),
                    Text(item, "footprint_provenance_id"),
                    Text(item, "entrance_provenance_id")));
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(
                        string.Join("\n", lines)))
                    .Select(value => value.ToString("x2")));
        }
    }
}
