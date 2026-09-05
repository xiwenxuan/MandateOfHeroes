using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class CountyPlanningDraftReasonIds
    {
        public const string ExistingFacility =
            "planning.geometry.existing_facility";
        public const string WaterCrossing =
            "planning.road.water_crossing_requires_bridge";
        public const string WallCrossing =
            "planning.road.wall_crossing_requires_gate";
        public const string ExistingWall =
            "planning.wall.already_fortified";
        public const string CanalUphill = "planning.canal.uphill";
        public const string CanalDisconnected =
            "planning.canal.water_connection_warning";
        public const string OutsideCounty = "planning.geometry.outside_county";
    }

    /// <summary>
    /// Deterministic draft-only geometry builder. It reads formal county
    /// facts but never mutates them.
    /// </summary>
    public sealed class CountyPlanningDraftGeometryService
    {
        private readonly CountySpatialPartition _partition;
        private readonly Luoyang50mCountyLayoutPackage _layout;
        private readonly FacilityPlacementValidator _validator;

        public CountyPlanningDraftGeometryService(
            Luoyang50mCountySpatialPrototype prototype,
            Luoyang50mCountyLayoutPackage layout,
            FacilityPlacementValidator validator)
        {
            _partition = prototype?.Partition ?? throw new ArgumentNullException(
                nameof(prototype));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _validator = validator ?? throw new ArgumentNullException(
                nameof(validator));
            if (!string.Equals(_partition.CountyId, _layout.CountyId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Draft geometry and county layout do not match.");
        }

        public IReadOnlyList<PlanningCellCoord> BuildStablePath(
            int startLocalRow, int startLocalColumn, int endLocalRow,
            int endLocalColumn)
        {
            var cells = new List<PlanningCellCoord>();
            var row = startLocalRow;
            var column = startLocalColumn;
            AddGlobal(cells, row, column);
            // The fixed column-first rule makes identical input deterministic.
            while (column != endLocalColumn)
            {
                column += Math.Sign(endLocalColumn - column);
                AddGlobal(cells, row, column);
            }
            while (row != endLocalRow)
            {
                row += Math.Sign(endLocalRow - row);
                AddGlobal(cells, row, column);
            }
            return cells;
        }

        public PlanningDraftValidation ValidateRoad(
            IReadOnlyList<PlanningCellCoord> path)
        {
            var blocking = new List<PlacementIssue>();
            var warnings = new List<PlacementIssue>();
            foreach (var cell in path ?? Array.Empty<PlanningCellCoord>())
            {
                if (!TryInspect(cell, out var row, out var column,
                        out var inspection))
                {
                    Add(blocking, CountyPlanningDraftReasonIds.OutsideCounty,
                        "道路草案超出当前县域。", 10);
                    continue;
                }
                if (inspection.FacilityIds.Count > 0)
                    Add(blocking,
                        CountyPlanningDraftReasonIds.ExistingFacility,
                        "道路草案穿过既有设施；规划阶段不会自动拆房。", 20);
                if (inspection.WaterState > 0 || inspection.Terrain ==
                        PlanningTerrainClass.Water)
                    Add(blocking, CountyPlanningDraftReasonIds.WaterCrossing,
                        "道路跨越水体需要桥梁或合法跨越设施。", 30);
                if (inspection.FortificationIds.Count > 0)
                    Add(blocking, CountyPlanningDraftReasonIds.WallCrossing,
                        "道路穿越城墙需要既有城门或合法通道。", 40);
                if (!_partition.IsBuildable(row, column) &&
                    inspection.WaterState == 0)
                    Add(warnings, PlacementReasonIds.CellNotBuildable,
                        "部分地块需要额外整地工程。", 100);
            }
            return new PlanningDraftValidation(blocking, warnings);
        }

        public IReadOnlyList<DraftFortificationSegment> BuildWallSegments(
            IReadOnlyList<PlanningCellCoord> path)
        {
            var result = new List<DraftFortificationSegment>();
            if (path == null) return result;
            for (var index = 0; index < path.Count - 1; index++)
            {
                var current = path[index];
                var next = path[index + 1];
                var direction = next.Column != current.Column
                    ? PlanningCellDirection.North
                    : PlanningCellDirection.West;
                result.Add(new DraftFortificationSegment(current, direction));
            }
            return result;
        }

        public PlanningDraftValidation ValidateWall(
            IReadOnlyList<DraftFortificationSegment> segments)
        {
            var blocking = new List<PlacementIssue>();
            var existing = new HashSet<string>(_layout.Fortifications.Select(
                value => EdgeKey(value.LocalRow, value.LocalColumn,
                    value.Direction)), StringComparer.Ordinal);
            foreach (var segment in segments ??
                         Array.Empty<DraftFortificationSegment>())
            {
                if (!_partition.TryToLocal(segment.Cell, out var row,
                        out var column))
                {
                    Add(blocking, CountyPlanningDraftReasonIds.OutsideCounty,
                        "城墙草案超出当前县域。", 10);
                    continue;
                }
                if (existing.Contains(EdgeKey(row, column,
                        segment.EdgeDirection)))
                    Add(blocking, CountyPlanningDraftReasonIds.ExistingWall,
                        "该Cell边已经存在正式城防，可连接但不能重复覆盖。", 20);
            }
            return new PlanningDraftValidation(blocking,
                Array.Empty<PlacementIssue>());
        }

        public PlanningDraftValidation ValidateCanal(
            IReadOnlyList<PlanningCellCoord> path)
        {
            var blocking = new List<PlacementIssue>();
            var warnings = new List<PlacementIssue>();
            ushort? previousElevation = null;
            var connected = false;
            foreach (var cell in path ?? Array.Empty<PlanningCellCoord>())
            {
                if (!TryInspect(cell, out _, out _, out var inspection))
                {
                    Add(blocking, CountyPlanningDraftReasonIds.OutsideCounty,
                        "水渠草案超出当前县域。", 10);
                    continue;
                }
                if (inspection.FacilityIds.Count > 0)
                    Add(blocking,
                        CountyPlanningDraftReasonIds.ExistingFacility,
                        "水渠草案穿过既有设施。", 20);
                connected |= inspection.WaterState > 0 ||
                             inspection.IrrigationState > 0;
                if (previousElevation.HasValue &&
                    inspection.ElevationDecimetres >
                    previousElevation.Value + 20)
                    Add(blocking, CountyPlanningDraftReasonIds.CanalUphill,
                        "水渠路径出现明显逆坡，请从高处向低处规划。", 30);
                previousElevation = inspection.ElevationDecimetres;
            }
            if (!connected)
                Add(warnings, CountyPlanningDraftReasonIds.CanalDisconnected,
                    "当前草案尚未连接已有水系；正式水利审批前需补齐水源。", 100);
            return new PlanningDraftValidation(blocking, warnings);
        }

        public IReadOnlyList<PlanningCellCoord> BuildRectangle(
            int firstLocalRow, int firstLocalColumn, int secondLocalRow,
            int secondLocalColumn)
        {
            var minimumRow = Math.Max(0, Math.Min(firstLocalRow,
                secondLocalRow));
            var maximumRow = Math.Min(_partition.Rows - 1,
                Math.Max(firstLocalRow, secondLocalRow));
            var minimumColumn = Math.Max(0, Math.Min(firstLocalColumn,
                secondLocalColumn));
            var maximumColumn = Math.Min(_partition.Columns - 1,
                Math.Max(firstLocalColumn, secondLocalColumn));
            var result = new List<PlanningCellCoord>(checked(
                (maximumRow - minimumRow + 1) *
                (maximumColumn - minimumColumn + 1)));
            for (var row = minimumRow; row <= maximumRow; row++)
            for (var column = minimumColumn; column <= maximumColumn;
                 column++)
                result.Add(_partition.ToGlobalCell(row, column));
            return result;
        }

        public ICountyPlanningDraft FindDraftAt(CountyPlanningSession session,
            PlanningCellCoord globalCell)
        {
            if (session == null) return null;
            foreach (var draft in session.AllDrafts.Reverse())
            {
                if (draft is DraftBuildingBlueprint building &&
                    building.CoveredPlanningCells.Contains(globalCell))
                    return draft;
                if (draft is CountyLinearDraft linear &&
                    linear.Path.Contains(globalCell)) return draft;
                if (draft is DraftFortification wall && wall.Segments.Any(
                        value => value.Cell.Equals(globalCell))) return draft;
                if (draft is DraftPlanningZone zone &&
                    zone.Cells.Contains(globalCell)) return draft;
            }
            return null;
        }

        private bool TryInspect(PlanningCellCoord globalCell, out int row,
            out int column, out PlanningCellInspection inspection)
        {
            if (!_partition.TryToLocal(globalCell, out row, out column))
            {
                inspection = null;
                return false;
            }
            inspection = _validator.InspectCell(row, column);
            return true;
        }

        private void AddGlobal(ICollection<PlanningCellCoord> cells,
            int localRow, int localColumn)
        {
            if (localRow < 0 || localRow >= _partition.Rows ||
                localColumn < 0 || localColumn >= _partition.Columns)
                cells.Add(new PlanningCellCoord(
                    _partition.MinimumCell.Row + localRow,
                    _partition.MinimumCell.Column + localColumn));
            else cells.Add(_partition.ToGlobalCell(localRow, localColumn));
        }

        private static string EdgeKey(int row, int column,
            PlanningCellDirection direction) => row + ":" + column + ":" +
                                                (int)direction;

        private static void Add(ICollection<PlacementIssue> issues,
            string code, string message, int priority)
        {
            if (issues.Any(value => string.Equals(value.Code, code,
                    StringComparison.Ordinal))) return;
            issues.Add(new PlacementIssue(code, message, priority));
        }
    }
}
