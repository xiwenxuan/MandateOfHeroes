using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public sealed class Luoyang50mCountySpatialPrototypeSource
    {
        public Luoyang50mCountySpatialPrototypeSource(string worldMapRoot)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("World map root is required.",
                    nameof(worldMapRoot));
            Root = Path.GetFullPath(worldMapRoot);
            var memoryBefore = GC.GetTotalMemory(false);
            var timer = Stopwatch.StartNew();
            var localMap = new LuoyangHumanScaleLocalMapPlanSource(Root);
            var layoutSource = new Luoyang50mCountyLayoutPackageSource(Root);
            LayoutPackage = layoutSource.Package;
            var facilities = localMap.Performance.Facilities;
            MinimumStrategicTile = new StrategicTileCoord(
                LayoutPackage.MinimumStrategicRow,
                LayoutPackage.MinimumStrategicColumn);

            var projection = new DualScaleCoordinateProjection();
            var partition = new CountySpatialPartition(
                Luoyang50mCountySpatialPrototypeIds.CountyId,
                projection.StrategicTileMinimum(MinimumStrategicTile),
                Luoyang50mCountySpatialPrototypeIds.Rows,
                Luoyang50mCountySpatialPrototypeIds.Columns);
            var roadCells = new bool[partition.PlanningCellCount];
            var waterCells = new bool[partition.PlanningCellCount];
            PopulateAuthoredTerrain(partition, roadCells, waterCells,
                out var roadStrategicCells, out var waterStrategicCells);

            var candidates = new List<
                Luoyang50mFacilityMigrationCandidate>(facilities.Count);
            var roadFacilityCount = 0;
            var fortificationCount = 0;
            var preservedCount = 0;
            foreach (var facility in facilities.OrderBy(item => item.CellId64)
                         .ThenBy(item => item.FacilityId,
                             StringComparer.Ordinal))
            {
                var layout = LayoutPackage.FacilitiesById[
                    facility.FacilityId];
                var localRow = layout.LocalRow;
                var localColumn = layout.LocalColumn;
                var cell = partition.ToGlobalCell(localRow, localColumn);
                var center = projection.PlanningCellCenter(cell);
                var width = layout.WidthCentimetres;
                var depth = layout.DepthCentimetres;
                var height = layout.HeightCentimetres;
                var direction = layout.EntranceDirection;
                var entrance = Entrance(center, direction, width, depth,
                    facility.FacilityId + ".entrance.50m-candidate.v1");
                partition.AddFacilityPlacement(new FacilitySpatialPlacement(
                    facility.FacilityId, partition.CountyId, center, width,
                    depth, layout.RotationQuarterTurns, height,
                    "collision.rectangle.provisional.v1",
                    new[] { entrance }));
                var preserved = layout.PreservesSourceStrategicTile;
                if (preserved) preservedCount++;
                var districtId = layout.DistrictId;
                candidates.Add(new Luoyang50mFacilityMigrationCandidate(
                    facility.FacilityId, facility.FacilityDefinitionId,
                    facility.ModelId, facility.CategoryId, districtId,
                    facility.SpatialPrecisionId, facility.CellId64,
                    facility.GridRow, facility.GridColumn, cell, width, depth,
                    preserved));

                if (string.Equals(facility.FacilityDefinitionId,
                        "facility.public.road", StringComparison.Ordinal))
                {
                    roadFacilityCount++;
                    roadCells[Index(partition, localRow, localColumn)] = true;
                    partition.SetLandUse(localRow, localColumn,
                        PlanningLandUseClass.Road);
                }
                else
                {
                    partition.SetLandUse(localRow, localColumn,
                        LandUse(facility.CategoryId,
                            facility.FacilityDefinitionId));
                }
                if (facility.FacilityDefinitionId.StartsWith(
                        "facility.fortification.", StringComparison.Ordinal))
                {
                    fortificationCount++;
                }
            }
            BuildLayoutNetwork(partition, LayoutPackage.RoadNodes,
                LayoutPackage.RoadEdges, roadCells, false);
            var derivedWaterPlanningCells = BuildLayoutNetwork(partition,
                LayoutPackage.CanalNodes, LayoutPackage.CanalEdges,
                waterCells, true);
            foreach (var wall in LayoutPackage.Fortifications)
                AddFortification(partition, wall);
            ConnectRoadAndWater(partition, roadCells, waterCells);
            ApplyFortificationConnections(partition);
            AddBoundaryPortals(partition, projection, LayoutPackage.Portals,
                roadCells);
            timer.Stop();
            var allocation = Math.Max(0,
                GC.GetTotalMemory(false) - memoryBefore);
            var byDistrict = candidates.GroupBy(item => item.DistrictId,
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(),
                    StringComparer.Ordinal);
            Prototype = new Luoyang50mCountySpatialPrototype(partition,
                candidates, byDistrict, roadStrategicCells,
                waterStrategicCells, derivedWaterPlanningCells,
                roadFacilityCount, fortificationCount,
                preservedCount, timer.Elapsed.TotalMilliseconds, allocation,
                LayoutPackage.DeclaredLayoutFingerprint,
                LayoutPackage.RuntimeDeterministicHash,
                LayoutPackage.RoadEdges.Count, LayoutPackage.CanalEdges.Count,
                LayoutPackage.DistrictAreas.Count);
        }

        public string Root { get; }
        public StrategicTileCoord MinimumStrategicTile { get; }
        public Luoyang50mCountyLayoutPackage LayoutPackage { get; }
        public Luoyang50mCountySpatialPrototype Prototype { get; }

        private void PopulateAuthoredTerrain(CountySpatialPartition partition,
            bool[] roadCells, bool[] waterCells,
            out int roadStrategicCellCount,
            out int waterStrategicCellCount)
        {
            roadStrategicCellCount = 0;
            waterStrategicCellCount = 0;
            using (var reader = new WorldMapDataReader(Path.Combine(Root,
                       "HanWorldV1")))
            {
                for (var tileRow = 0; tileRow <
                     Luoyang50mCountySpatialPrototypeIds.StrategicRows;
                     tileRow++)
                for (var tileColumn = 0; tileColumn <
                     Luoyang50mCountySpatialPrototypeIds.StrategicColumns;
                     tileColumn++)
                {
                    var source = reader.ReadCell(
                        MinimumStrategicTile.Row + tileRow,
                        MinimumStrategicTile.Column + tileColumn);
                    if (source.RoadClass > 0) roadStrategicCellCount++;
                    if (source.WaterClass > 0) waterStrategicCellCount++;
                    for (var childRow = 0; childRow < 40; childRow++)
                    for (var childColumn = 0; childColumn < 40; childColumn++)
                    {
                        var row = tileRow * 40 + childRow;
                        var column = tileColumn * 40 + childColumn;
                        var isWater = source.WaterClass > 0;
                        var isRoad = source.RoadClass > 0 &&
                            (childRow == 20 || childColumn == 20);
                        roadCells[Index(partition, row, column)] = isRoad;
                        waterCells[Index(partition, row, column)] = isWater;
                        partition.SetCell(row, column,
                            checked((ushort)Math.Max(0, Math.Min(
                                ushort.MaxValue, source.Elevation * 10))),
                            Terrain(source), source.SlopeClass,
                            source.Buildable && !isWater,
                            isRoad ? PlanningLandUseClass.Road :
                            PlanningLandUseClass.Unassigned,
                            source.WaterClass, 0);
                    }
                }
            }
        }

        private static void ConnectRoadAndWater(
            CountySpatialPartition partition, bool[] roads, bool[] waters)
        {
            for (var row = 0; row < partition.Rows; row++)
            for (var column = 0; column < partition.Columns; column++)
            {
                if (column + 1 < partition.Columns)
                    SetConnection(partition, roads, waters, row, column,
                        row, column + 1, PlanningCellDirection.East);
                if (row + 1 < partition.Rows)
                    SetConnection(partition, roads, waters, row, column,
                        row + 1, column, PlanningCellDirection.South);
            }
        }

        private static int BuildLayoutNetwork(
            CountySpatialPartition partition,
            IReadOnlyList<Luoyang50mLayoutNode> nodes,
            IReadOnlyList<Luoyang50mLayoutEdge> edges,
            bool[] cells, bool water)
        {
            var before = cells.Count(value => value);
            foreach (var node in nodes)
                Mark(node.LocalRow, node.LocalColumn);
            foreach (var edge in edges.OrderBy(item => item.EdgeId,
                         StringComparer.Ordinal))
            {
                var row = edge.FromLocalRow;
                var column = edge.FromLocalColumn;
                while (true)
                {
                    Mark(row, column);
                    if (row == edge.ToLocalRow &&
                        column == edge.ToLocalColumn) break;
                    if (row != edge.ToLocalRow)
                        row += Math.Sign(edge.ToLocalRow - row);
                    else
                        column += Math.Sign(edge.ToLocalColumn - column);
                }
            }
            return cells.Count(value => value) - before;

            void Mark(int row, int column)
            {
                var index = Index(partition, row, column);
                cells[index] = true;
                if (water) partition.SetWaterState(row, column, 2);
                else partition.SetLandUse(row, column,
                    PlanningLandUseClass.Road);
            }
        }

        private static void SetConnection(CountySpatialPartition partition,
            bool[] roads, bool[] waters, int row, int column,
            int otherRow, int otherColumn, PlanningCellDirection direction)
        {
            var first = Index(partition, row, column);
            var second = Index(partition, otherRow, otherColumn);
            if (waters[first] || waters[second])
                partition.Connections.SetBetween(row, column, direction,
                    PlanningCellConnectionKind.BlockedByWater);
            else if (roads[first] && roads[second])
                partition.Connections.SetBetween(row, column, direction,
                    PlanningCellConnectionKind.OpenByRoad);
        }

        private static void AddFortification(
            CountySpatialPartition partition,
            Luoyang50mLayoutFortification layout)
        {
            var cell = partition.ToGlobalCell(layout.LocalRow,
                layout.LocalColumn);
            var segment = new FortificationSegmentSpatialState(
                layout.EdgeId, partition.CountyId, layout.DefinitionId,
                new PlanningCellEdge(cell, layout.Direction), layout.IsGate,
                layout.HeightCentimetres, layout.ThicknessCentimetres,
                layout.MaximumDurability,
                "organization.luoyang.owner.unknown.v1",
                "organization.luoyang.controller.unknown.v1");
            partition.AddFortification(segment);
        }

        private static void ApplyFortificationConnections(
            CountySpatialPartition partition)
        {
            foreach (var segment in partition.Fortifications.Values
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                if (!partition.TryToLocal(segment.Edge.First,
                        out var row, out var column))
                    continue;
                var direction = segment.Edge.Second.Row >
                                segment.Edge.First.Row
                    ? PlanningCellDirection.South
                    : PlanningCellDirection.East;
                partition.Connections.SetBetween(row, column, direction,
                    segment.PassageKind);
            }
        }

        private static void AddBoundaryPortals(CountySpatialPartition partition,
            DualScaleCoordinateProjection projection,
            IReadOnlyList<Luoyang50mLayoutPortal> portals,
            bool[] roads)
        {
            foreach (var portal in portals.OrderBy(item => item.PortalId,
                         StringComparer.Ordinal))
            {
                var row = portal.LocalRow;
                var column = portal.LocalColumn;
                roads[Index(partition, row, column)] = true;
                partition.SetLandUse(row, column, PlanningLandUseClass.Road);
                partition.Connections.SetBetween(row, column,
                    portal.InwardDirection,
                    PlanningCellConnectionKind.OpenByRoad);
                var global = partition.ToGlobalCell(row, column);
                partition.AddPortal(new CountyPortalSpatialState(
                    portal.PortalId, portal.RouteId, partition.CountyId,
                    portal.NeighborCountyId,
                    global, projection.ToStrategicTile(global),
                    portal.PassageTypeId));
            }
        }

        private static FacilityEntranceSpatialState Entrance(
            GlobalProjectedCoordinate center, PlanningCellDirection direction,
            int widthCentimetres, int depthCentimetres, string id)
        {
            var x = center.EastingMetres;
            var y = center.NorthingMetres;
            if (direction == PlanningCellDirection.North)
                y += depthCentimetres / 200d;
            else if (direction == PlanningCellDirection.South)
                y -= depthCentimetres / 200d;
            else if (direction == PlanningCellDirection.East)
                x += widthCentimetres / 200d;
            else
                x -= widthCentimetres / 200d;
            return new FacilityEntranceSpatialState(id,
                new GlobalProjectedCoordinate(x, y), direction);
        }

        private static PlanningLandUseClass LandUse(string categoryId,
            string definitionId)
        {
            if (definitionId.StartsWith("facility.fortification.",
                    StringComparison.Ordinal) || categoryId == "military")
                return PlanningLandUseClass.Military;
            if (categoryId == "residential")
                return PlanningLandUseClass.Residential;
            if (categoryId == "agriculture" ||
                categoryId == "resource_agriculture")
                return PlanningLandUseClass.Agriculture;
            if (categoryId == "industry" || categoryId == "resource")
                return PlanningLandUseClass.Industry;
            if (categoryId == "government" || categoryId == "public" ||
                categoryId == "ritual" || categoryId == "education" ||
                categoryId == "service" || categoryId == "commercial")
                return PlanningLandUseClass.Government;
            return PlanningLandUseClass.Unassigned;
        }

        private static PlanningTerrainClass Terrain(WorldMapCellRecord cell)
        {
            if (cell.WaterClass > 0) return PlanningTerrainClass.Water;
            if (cell.TerrainClass >= 4) return PlanningTerrainClass.Forest;
            if (cell.SlopeClass >= 2) return PlanningTerrainClass.Hill;
            return PlanningTerrainClass.Plains;
        }

        private static int Index(CountySpatialPartition partition, int row,
            int column) => checked(row * partition.Columns + column);
    }
}
