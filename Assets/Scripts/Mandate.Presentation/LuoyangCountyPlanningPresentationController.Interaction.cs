using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed class CountyPlanningInteractionPerformanceSnapshot
    {
        public int Samples { get; set; }
        public double BuildingGhostUpdateP50Milliseconds { get; set; }
        public double BuildingGhostUpdateP95Milliseconds { get; set; }
        public double RoadPreviewP50Milliseconds { get; set; }
        public double RoadPreviewP95Milliseconds { get; set; }
        public double WallPreviewP50Milliseconds { get; set; }
        public double WallPreviewP95Milliseconds { get; set; }
        public double CanalPreviewP50Milliseconds { get; set; }
        public double CanalPreviewP95Milliseconds { get; set; }
        public double ZoneBrushP50Milliseconds { get; set; }
        public double ZoneBrushP95Milliseconds { get; set; }
        public long ManagedAllocationBytes { get; set; }
    }

    public sealed partial class LuoyangCountyPlanningPresentationController
    {
        private CountyPlanningDraftGeometryService _draftGeometry;
        private IReadOnlyList<PlanningCellCoord> _linearPreview =
            Array.Empty<PlanningCellCoord>();
        private IReadOnlyList<DraftFortificationSegment> _wallPreview =
            Array.Empty<DraftFortificationSegment>();
        private PlanningDraftValidation _geometryValidation;
        private readonly HashSet<string> _selectedDraftIds =
            new HashSet<string>(StringComparer.Ordinal);
        private Vector2 _pointerLocalGui;
        private bool _pointerOnMap;
        private int _dragCurrentRow;
        private int _dragCurrentColumn;
        private double _lastOverlaySwitchMilliseconds;

        public PlanningToolState ToolState { get; private set; }
        public PlanningMapOverlayState MapOverlays { get; private set; }
        public IReadOnlyCollection<string> SelectedDraftIds =>
            _selectedDraftIds;
        public IReadOnlyList<PlanningCellCoord> LinearPreview =>
            _linearPreview;
        public PlanningDraftValidation GeometryValidation =>
            _geometryValidation;
        public double LastOverlaySwitchMilliseconds =>
            _lastOverlaySwitchMilliseconds;
        public bool ShouldDrawBuildingGhost => ToolState != null &&
            (ToolState.PrimaryTool == CountyPlanningPrimaryTool.Building ||
             ToolState.PrimaryTool == CountyPlanningPrimaryTool.MoveDraft);

        private void InitializeVisualPlanningInteraction()
        {
            ToolState = new PlanningToolState();
            MapOverlays = new PlanningMapOverlayState();
            _draftGeometry = new CountyPlanningDraftGeometryService(
                _prototype, _layoutPackage, Validator);
            _selectedDraftIds.Clear();
            ClearTransientPreview();
            RebuildPlanningMapTexture();
        }

        public void ActivateBuildingTool(string profileId)
        {
            if (ToolState == null || Profiles == null ||
                !Profiles.ProfilesById.TryGetValue(profileId,
                    out var profile)) return;
            SelectedProfile = profile;
            ToolState.Activate(CountyPlanningPrimaryTool.Building, profileId);
            ToolState.SetRotation(RotationQuarterTurns);
            _selectedDraftIds.Clear();
            ClearTransientPreview();
            RefreshValidation(PreviewLocalRow, PreviewLocalColumn, false);
        }

        public void ActivateTool(CountyPlanningPrimaryTool tool)
        {
            if (ToolState == null) return;
            ToolState.Activate(tool);
            _selectedDraftIds.Clear();
            ClearTransientPreview();
        }

        public void ActivateZoneTool(CountyPlanningZoneKind zoneKind)
        {
            if (ToolState == null) return;
            ToolState.ActivateZone(zoneKind);
            _selectedDraftIds.Clear();
            ClearTransientPreview();
        }

        public void CancelPlanningTool()
        {
            if (ToolState == null) return;
            ToolState.CancelCurrentAction();
            ClearTransientPreview();
            RefreshValidation();
        }

        public bool SetOverlayVisible(string overlayId, bool visible)
        {
            if (MapOverlays == null) return false;
            var before = MapOverlays.Version;
            switch (overlayId)
            {
                case "administrative":
                    MapOverlays.SetAdministrativeVisible(visible);
                    break;
                case "roads":
                    MapOverlays.SetRoadsVisible(visible);
                    break;
                case "rivers":
                    MapOverlays.SetRiversVisible(visible);
                    break;
                case "grid":
                    MapOverlays.SetGridVisible(visible);
                    break;
                case "fortifications":
                    MapOverlays.SetFortificationsVisible(visible);
                    break;
                case "planning":
                    MapOverlays.SetPlanningVisible(visible);
                    break;
                case "terrain":
                    MapOverlays.SetTerrainAnalysisVisible(visible);
                    break;
                default:
                    return false;
            }
            if (MapOverlays.Version == before) return true;
            var timer = Stopwatch.StartNew();
            RefreshCountyPresentation(true);
            timer.Stop();
            _lastOverlaySwitchMilliseconds = timer.Elapsed.TotalMilliseconds;
            return true;
        }

        public string HandleMapGuiEvent(Rect mapRect, Event current)
        {
            if (!IsActive || current == null) return string.Empty;
            var overMap = mapRect.Contains(current.mousePosition);
            var pointerChanged = current.type == EventType.MouseMove ||
                                 current.type == EventType.MouseDown ||
                                 current.type == EventType.MouseDrag ||
                                 current.type == EventType.MouseUp;
            if (overMap && pointerChanged)
            {
                _pointerOnMap = true;
                _pointerLocalGui = current.mousePosition - mapRect.position;
                HoverCellFromMap(mapRect, current.mousePosition);
                if ((current.type == EventType.MouseDown &&
                     current.button == 0) || ToolState != null &&
                    ToolState.IsDragging)
                    SelectCell(HoveredLocalRow, HoveredLocalColumn, false);
                if (ToolState != null && ToolState.IsDragging)
                {
                    _dragCurrentRow = SelectedLocalRow;
                    _dragCurrentColumn = SelectedLocalColumn;
                    RefreshDragPreview();
                }
            }
            else if (current.type == EventType.MouseLeaveWindow)
            {
                _pointerOnMap = false;
                ClearHoveredPlanningCell();
            }

            if (current.type == EventType.MouseUp && current.button == 1 &&
                !current.alt && overMap)
            {
                CancelPlanningTool();
                current.Use();
                return "已取消当前建设工具；规划草案保持不变。";
            }

            if (!overMap || current.button != 0) return string.Empty;
            if (current.type == EventType.MouseDown)
            {
                var message = PrimaryDown();
                current.Use();
                return message;
            }
            if (current.type == EventType.MouseDrag && ToolState != null &&
                ToolState.IsDragging)
            {
                RefreshDragPreview();
                current.Use();
                return GeometryFeedback();
            }
            if (current.type == EventType.MouseUp && ToolState != null &&
                ToolState.IsDragging)
            {
                var message = CompleteDrag();
                current.Use();
                return message;
            }
            return string.Empty;
        }

        public bool DeleteSelectedDrafts()
        {
            if (Session == null || _selectedDraftIds.Count == 0) return false;
            var removed = Session.RemoveDrafts(_selectedDraftIds);
            _selectedDraftIds.Clear();
            RefreshValidation();
            return removed > 0;
        }

        public DraftRoadGeometry CreateRoadDraft(int startRow,
            int startColumn, int endRow, int endColumn)
        {
            var path = _draftGeometry.BuildStablePath(startRow, startColumn,
                endRow, endColumn);
            var validation = _draftGeometry.ValidateRoad(path);
            return validation.IsValid
                ? Session.CreateRoadDraft(path, validation) : null;
        }

        public DraftFortification CreateWallDraft(int startRow,
            int startColumn, int endRow, int endColumn)
        {
            var path = _draftGeometry.BuildStablePath(startRow, startColumn,
                endRow, endColumn);
            var segments = _draftGeometry.BuildWallSegments(path);
            var validation = _draftGeometry.ValidateWall(segments);
            return validation.IsValid
                ? Session.CreateFortificationDraft(segments, validation)
                : null;
        }

        public DraftCanalGeometry CreateCanalDraft(int startRow,
            int startColumn, int endRow, int endColumn)
        {
            var path = _draftGeometry.BuildStablePath(startRow, startColumn,
                endRow, endColumn);
            var validation = _draftGeometry.ValidateCanal(path);
            return validation.IsValid
                ? Session.CreateCanalDraft(path, validation) : null;
        }

        public DraftPlanningZone CreateZoneDraft(
            CountyPlanningZoneKind kind, int firstRow, int firstColumn,
            int secondRow, int secondColumn) => Session.CreateZoneDraft(kind,
            _draftGeometry.BuildRectangle(firstRow, firstColumn, secondRow,
                secondColumn));

        public PlanningDraftValidation PreviewDraftTool(
            CountyPlanningPrimaryTool tool, int startRow, int startColumn,
            int endRow, int endColumn,
            CountyPlanningZoneKind zoneKind =
                CountyPlanningZoneKind.Residential)
        {
            if (tool != CountyPlanningPrimaryTool.Road &&
                tool != CountyPlanningPrimaryTool.Wall &&
                tool != CountyPlanningPrimaryTool.Canal &&
                tool != CountyPlanningPrimaryTool.Zone)
                throw new ArgumentOutOfRangeException(nameof(tool));
            if (tool == CountyPlanningPrimaryTool.Zone)
                ActivateZoneTool(zoneKind);
            else ActivateTool(tool);
            SelectCell(startRow, startColumn);
            ToolState.BeginDrag(CurrentGlobalCell());
            _dragCurrentRow = endRow;
            _dragCurrentColumn = endColumn;
            RefreshDragPreview();
            return _geometryValidation;
        }

        public bool PreviewDraftToolIsValid(
            CountyPlanningPrimaryTool tool, int startRow, int startColumn,
            int endRow, int endColumn,
            CountyPlanningZoneKind zoneKind =
                CountyPlanningZoneKind.Residential) => PreviewDraftTool(tool,
            startRow, startColumn, endRow, endColumn, zoneKind)?.IsValid ==
            true;

        public bool IsCurrentBuildingPlacementValid =>
            Validation?.IsValid == true;

        public string FirstExistingFacilityIdAtSelection() =>
            CellInspection?.FacilityIds.FirstOrDefault();

        public DraftBuildingBlueprint MoveBuildingDraft(string draftId,
            int row, int column, int rotation)
        {
            var source = Session.FindDraft(draftId) as DraftBuildingBlueprint;
            if (source == null || !Profiles.ProfilesById.TryGetValue(
                    source.ProfileId, out var profile)) return null;
            var footprint = Validator.CreateFootprint(profile, row, column,
                rotation);
            var validation = Validator.Validate(footprint, Session, draftId);
            return validation.IsValid
                ? Session.MoveBuildingDraft(draftId, profile, footprint,
                    validation)
                : null;
        }

        public DraftBuildingBlueprint CopyBuildingDraft(string draftId,
            int row, int column, int rotation)
        {
            var source = Session.FindDraft(draftId) as DraftBuildingBlueprint;
            if (source == null || !Profiles.ProfilesById.TryGetValue(
                    source.ProfileId, out var profile)) return null;
            var footprint = Validator.CreateFootprint(profile, row, column,
                rotation);
            var validation = Validator.Validate(footprint, Session);
            return validation.IsValid
                ? Session.CopyBuildingDraft(profile, footprint, validation)
                : null;
        }

        public bool EyedropperExistingFacility(string facilityId)
        {
            if (!_layoutPackage.FacilitiesById.TryGetValue(facilityId,
                    out var facility) ||
                !Profiles.ProfilesByDefinitionId.TryGetValue(
                    facility.DefinitionId, out var profile)) return false;
            ActivateBuildingTool(profile.ProfileId);
            return true;
        }

        public CountyPlanningInteractionPerformanceSnapshot
            MeasureInteractionPerformance(int row, int column,
                int samples = 64)
        {
            if (!IsActive || _draftGeometry == null)
                throw new InvalidOperationException(
                    "County planning is not active.");
            if (samples < 8) throw new ArgumentOutOfRangeException(
                nameof(samples));
            var ghost = new double[samples];
            var road = new double[samples];
            var wall = new double[samples];
            var canal = new double[samples];
            var zone = new double[samples];
            var originalRow = SelectedLocalRow;
            var originalColumn = SelectedLocalColumn;
            var memoryBefore = GC.GetTotalMemory(false);
            for (var index = 0; index < samples; index++)
            {
                var endColumn = Math.Min(_prototype.Partition.Columns - 1,
                    column + 2 + index % 3);
                var endRow = Math.Min(_prototype.Partition.Rows - 1,
                    row + 2 + index % 3);
                var timer = Stopwatch.StartNew();
                SelectCell(row, column);
                timer.Stop();
                ghost[index] = timer.Elapsed.TotalMilliseconds;

                timer.Restart();
                var path = _draftGeometry.BuildStablePath(row, column, row,
                    endColumn);
                _draftGeometry.ValidateRoad(path);
                timer.Stop();
                road[index] = timer.Elapsed.TotalMilliseconds;

                timer.Restart();
                var wallPath = _draftGeometry.BuildStablePath(row, column,
                    endRow, column);
                _draftGeometry.ValidateWall(
                    _draftGeometry.BuildWallSegments(wallPath));
                timer.Stop();
                wall[index] = timer.Elapsed.TotalMilliseconds;

                timer.Restart();
                var canalPath = _draftGeometry.BuildStablePath(row, column,
                    row, endColumn);
                _draftGeometry.ValidateCanal(canalPath);
                timer.Stop();
                canal[index] = timer.Elapsed.TotalMilliseconds;

                timer.Restart();
                _draftGeometry.BuildRectangle(row, column, endRow,
                    endColumn);
                timer.Stop();
                zone[index] = timer.Elapsed.TotalMilliseconds;
            }
            SelectCell(originalRow, originalColumn);
            Array.Sort(ghost);
            Array.Sort(road);
            Array.Sort(wall);
            Array.Sort(canal);
            Array.Sort(zone);
            return new CountyPlanningInteractionPerformanceSnapshot
            {
                Samples = samples,
                BuildingGhostUpdateP50Milliseconds = Percentile(ghost,
                    0.50d),
                BuildingGhostUpdateP95Milliseconds = Percentile(ghost,
                    0.95d),
                RoadPreviewP50Milliseconds = Percentile(road, 0.50d),
                RoadPreviewP95Milliseconds = Percentile(road, 0.95d),
                WallPreviewP50Milliseconds = Percentile(wall, 0.50d),
                WallPreviewP95Milliseconds = Percentile(wall, 0.95d),
                CanalPreviewP50Milliseconds = Percentile(canal, 0.50d),
                CanalPreviewP95Milliseconds = Percentile(canal, 0.95d),
                ZoneBrushP50Milliseconds = Percentile(zone, 0.50d),
                ZoneBrushP95Milliseconds = Percentile(zone, 0.95d),
                ManagedAllocationBytes = Math.Max(0,
                    GC.GetTotalMemory(false) - memoryBefore)
            };
        }

        private static double Percentile(IReadOnlyList<double> values,
            double percentile) => values[Math.Min(values.Count - 1,
            Math.Max(0, (int)Math.Ceiling(values.Count * percentile) - 1))];

        private string PrimaryDown()
        {
            if (ToolState == null) return string.Empty;
            switch (ToolState.PrimaryTool)
            {
                case CountyPlanningPrimaryTool.Building:
                    var building = CreateDraft();
                    return building == null
                        ? Validation?.PrimaryReason ?? "当前位置不能规划建筑。"
                        : "建筑草案已加入方案；当前工具保持激活，可继续放置。";
                case CountyPlanningPrimaryTool.Road:
                case CountyPlanningPrimaryTool.Wall:
                case CountyPlanningPrimaryTool.Canal:
                case CountyPlanningPrimaryTool.Zone:
                case CountyPlanningPrimaryTool.Select:
                    ToolState.BeginDrag(CurrentGlobalCell());
                    _dragCurrentRow = SelectedLocalRow;
                    _dragCurrentColumn = SelectedLocalColumn;
                    RefreshDragPreview();
                    return "拖动鼠标预览，松开左键确认草案。";
                case CountyPlanningPrimaryTool.DemolishDraft:
                    return DemolishAtPointer();
                case CountyPlanningPrimaryTool.MoveDraft:
                    return MoveAtPointer();
                case CountyPlanningPrimaryTool.CopyDraft:
                    return CopyAtPointer();
                case CountyPlanningPrimaryTool.Eyedropper:
                    return PickProfileAtPointer();
                default:
                    return "请先从底部建设栏选择一种工具。";
            }
        }

        private string CompleteDrag()
        {
            if (ToolState == null) return string.Empty;
            var tool = ToolState.PrimaryTool;
            ToolState.EndDrag();
            if (tool == CountyPlanningPrimaryTool.Select)
            {
                SelectDraftRectangle();
                ClearTransientPreview();
                return _selectedDraftIds.Count == 0
                    ? "框选范围内没有规划草案；正式设施不会被选中。"
                    : "已选择 " + _selectedDraftIds.Count +
                      " 个规划草案，可批量删除。";
            }
            if (tool == CountyPlanningPrimaryTool.Zone)
            {
                var cells = _draftGeometry.BuildRectangle(
                    LocalRow(ToolState.DragStart),
                    LocalColumn(ToolState.DragStart), _dragCurrentRow,
                    _dragCurrentColumn);
                Session.CreateZoneDraft(ToolState.ZoneKind, cells);
                ClearTransientPreview();
                return "区域规划草案已涂刷；不会自动生成建筑或修改正式用地。";
            }
            if (_geometryValidation == null ||
                !_geometryValidation.IsValid)
            {
                var reason = GeometryFeedback();
                ClearTransientPreview();
                return reason;
            }
            if (tool == CountyPlanningPrimaryTool.Road)
                Session.CreateRoadDraft(_linearPreview,
                    _geometryValidation);
            else if (tool == CountyPlanningPrimaryTool.Wall)
                Session.CreateFortificationDraft(_wallPreview,
                    _geometryValidation);
            else if (tool == CountyPlanningPrimaryTool.Canal)
                Session.CreateCanalDraft(_linearPreview,
                    _geometryValidation);
            ClearTransientPreview();
            RefreshValidation();
            return tool == CountyPlanningPrimaryTool.Road
                ? "道路蓝图已加入方案；正式道路没有改变。"
                : tool == CountyPlanningPrimaryTool.Wall
                    ? "城墙边蓝图已加入方案；正式通行口没有改变。"
                    : "水渠蓝图已加入方案；正式水系没有改变。";
        }

        private void RefreshDragPreview()
        {
            if (ToolState == null || !ToolState.IsDragging) return;
            var startRow = LocalRow(ToolState.DragStart);
            var startColumn = LocalColumn(ToolState.DragStart);
            if (ToolState.PrimaryTool == CountyPlanningPrimaryTool.Zone ||
                ToolState.PrimaryTool == CountyPlanningPrimaryTool.Select)
            {
                _linearPreview = _draftGeometry.BuildRectangle(startRow,
                    startColumn, _dragCurrentRow, _dragCurrentColumn);
                _geometryValidation = new PlanningDraftValidation(
                    Array.Empty<PlacementIssue>(),
                    Array.Empty<PlacementIssue>());
                return;
            }
            _linearPreview = _draftGeometry.BuildStablePath(startRow,
                startColumn, _dragCurrentRow, _dragCurrentColumn);
            if (_linearPreview.Count < 2)
            {
                _geometryValidation = new PlanningDraftValidation(new[]
                {
                    new PlacementIssue("planning.geometry.too_short",
                        "请拖动至少一个格。", 1)
                }, Array.Empty<PlacementIssue>());
                return;
            }
            if (ToolState.PrimaryTool == CountyPlanningPrimaryTool.Road)
                _geometryValidation = _draftGeometry.ValidateRoad(
                    _linearPreview);
            else if (ToolState.PrimaryTool ==
                     CountyPlanningPrimaryTool.Wall)
            {
                _wallPreview = _draftGeometry.BuildWallSegments(
                    _linearPreview);
                _geometryValidation = _draftGeometry.ValidateWall(
                    _wallPreview);
            }
            else if (ToolState.PrimaryTool ==
                     CountyPlanningPrimaryTool.Canal)
                _geometryValidation = _draftGeometry.ValidateCanal(
                    _linearPreview);
        }

        private string DemolishAtPointer()
        {
            var draft = _draftGeometry.FindDraftAt(Session,
                CurrentGlobalCell());
            if (draft != null)
            {
                Session.RemoveDraft(draft.DraftId);
                RefreshValidation();
                return "已删除规划草案；正式设施没有变化。";
            }
            return CellInspection != null &&
                   CellInspection.FacilityIds.Count > 0
                ? "正式设施不能在规划草案阶段直接拆除。"
                : "这里没有可删除的规划草案。";
        }

        private string MoveAtPointer()
        {
            if (string.IsNullOrWhiteSpace(ToolState.EditingDraftId))
            {
                var source = _draftGeometry.FindDraftAt(Session,
                    CurrentGlobalCell()) as DraftBuildingBlueprint;
                if (source == null)
                    return "移动工具只能移动尚未施工的建筑草案。";
                if (!Profiles.ProfilesById.TryGetValue(source.ProfileId,
                        out var profile)) return "草案的建筑类型不可用。";
                SelectedProfile = profile;
                RotationQuarterTurns = source.RotationQuarterTurns;
                ToolState.BeginDraftEdit(
                    CountyPlanningPrimaryTool.MoveDraft, source.DraftId);
                ToolState.SetRotation(RotationQuarterTurns);
                RefreshValidation();
                return "移动预览已启用；左键新位置确认，右键取消。";
            }
            var moved = Session.MoveBuildingDraft(ToolState.EditingDraftId,
                SelectedProfile, _footprint, Validation);
            if (moved == null)
                return Validation?.PrimaryReason ?? "新位置不能放置草案。";
            ToolState.Activate(CountyPlanningPrimaryTool.MoveDraft);
            RefreshValidation();
            return "建筑草案已移动并重新完成空间校验。";
        }

        private string CopyAtPointer()
        {
            var source = _draftGeometry.FindDraftAt(Session,
                CurrentGlobalCell()) as DraftBuildingBlueprint;
            if (source == null)
                return "复制工具只复制建筑草案配置，不复制正式设施状态。";
            if (!Profiles.ProfilesById.TryGetValue(source.ProfileId,
                    out var profile)) return "草案的建筑类型不可用。";
            RotationQuarterTurns = source.RotationQuarterTurns;
            ActivateBuildingTool(profile.ProfileId);
            return "已复制建筑类型和朝向；请在新位置放置新的草案ID。";
        }

        private string PickProfileAtPointer()
        {
            var draft = _draftGeometry.FindDraftAt(Session,
                CurrentGlobalCell()) as DraftBuildingBlueprint;
            string definitionId = draft?.FacilityDefinitionId;
            if (string.IsNullOrWhiteSpace(definitionId) &&
                CellInspection != null && CellInspection.FacilityIds.Count > 0)
            {
                var facilityId = CellInspection.FacilityIds[0];
                if (_layoutPackage.FacilitiesById.TryGetValue(facilityId,
                        out var facility)) definitionId = facility.DefinitionId;
            }
            if (string.IsNullOrWhiteSpace(definitionId) ||
                !Profiles.ProfilesByDefinitionId.TryGetValue(definitionId,
                    out var profile))
                return "该对象没有可用的建筑放置资料。";
            ActivateBuildingTool(profile.ProfileId);
            return "吸管已读取建筑类型；产权、库存、人员和设施ID均未复制。";
        }

        private void SelectDraftRectangle()
        {
            _selectedDraftIds.Clear();
            var cells = new HashSet<PlanningCellCoord>(_draftGeometry
                .BuildRectangle(LocalRow(ToolState.DragStart),
                    LocalColumn(ToolState.DragStart), _dragCurrentRow,
                    _dragCurrentColumn));
            foreach (var draft in Session.AllDrafts)
            {
                var intersects = draft is DraftBuildingBlueprint building
                    ? building.CoveredPlanningCells.Any(cells.Contains)
                    : draft is CountyLinearDraft linear
                        ? linear.Path.Any(cells.Contains)
                        : draft is DraftFortification wall
                            ? wall.Segments.Any(value => cells.Contains(
                                value.Cell))
                            : draft is DraftPlanningZone zone &&
                              zone.Cells.Any(cells.Contains);
                if (intersects) _selectedDraftIds.Add(draft.DraftId);
            }
        }

        private void ClearTransientPreview()
        {
            _linearPreview = Array.Empty<PlanningCellCoord>();
            _wallPreview = Array.Empty<DraftFortificationSegment>();
            _geometryValidation = null;
        }

        private string GeometryFeedback() => _geometryValidation == null
            ? string.Empty
            : _geometryValidation.IsValid
                ? _geometryValidation.Warnings.Count > 0
                    ? _geometryValidation.PrimaryReason
                    : "草案路径有效，松开左键确认。"
                : _geometryValidation.PrimaryReason;

        private PlanningCellCoord CurrentGlobalCell() =>
            _prototype.Partition.ToGlobalCell(SelectedLocalRow,
                SelectedLocalColumn);

        private int LocalRow(PlanningCellCoord global) =>
            global.Row - _prototype.Partition.MinimumCell.Row;

        private int LocalColumn(PlanningCellCoord global) =>
            global.Column - _prototype.Partition.MinimumCell.Column;

        private void DrawNonBuildingDrafts(Rect mapRect)
        {
            if (Session == null) return;
            foreach (var zone in Session.ZoneDrafts)
            foreach (var cell in zone.Cells)
                DrawPlanningCell(cell, mapRect, ZoneColor(zone.ZoneKind));
            foreach (var road in Session.RoadDrafts)
                DrawPath(road.Path, mapRect,
                    new Color(0.25f, 0.92f, 1f, 0.96f), 4f);
            foreach (var canal in Session.CanalDrafts)
                DrawPath(canal.Path, mapRect,
                    new Color(0.16f, 0.68f, 1f, 0.96f), 4f);
            foreach (var wall in Session.FortificationDrafts)
                DrawWall(wall.Segments, mapRect,
                    new Color(0.88f, 0.78f, 0.52f, 0.98f), 4f);

            if (ToolState == null || !ToolState.IsDragging) return;
            var previewColor = _geometryValidation == null ||
                               _geometryValidation.IsValid
                ? new Color(0.35f, 1f, 0.55f, 0.9f)
                : new Color(1f, 0.24f, 0.18f, 0.92f);
            if (ToolState.PrimaryTool == CountyPlanningPrimaryTool.Wall)
                DrawWall(_wallPreview, mapRect, previewColor, 5f);
            else if (ToolState.PrimaryTool == CountyPlanningPrimaryTool.Zone ||
                     ToolState.PrimaryTool == CountyPlanningPrimaryTool.Select)
                foreach (var cell in _linearPreview)
                    DrawPlanningCell(cell, mapRect,
                        new Color(previewColor.r, previewColor.g,
                            previewColor.b, 0.28f));
            else DrawPath(_linearPreview, mapRect, previewColor, 5f);
        }

        private void DrawPlanningGridAndOverlays(Rect mapRect)
        {
            if (MapOverlays == null) return;
            if (MapOverlays.RoadsVisible)
            {
                foreach (var portal in _layoutPackage.Portals)
                {
                    var cell = _prototype.Partition.ToGlobalCell(
                        portal.LocalRow, portal.LocalColumn);
                    DrawPlanningCell(cell, mapRect,
                        new Color(1f, 0.72f, 0.22f, 0.50f));
                }
                var road = Validation?.RoadAccessResult;
                if (road != null && road.DistanceCentimetres >= 0)
                    DrawLine(ToGuiPosition(road.EntrancePosition, mapRect),
                        ToGuiPosition(road.ConnectionPosition, mapRect),
                        new Color(1f, 0.88f, 0.35f, 0.85f), 2f);
            }
            if (!ShouldShowPlanningGrid) return;
            var verticalCount = Mathf.CeilToInt(_viewColumns);
            var horizontalCount = Mathf.CeilToInt(_viewRows);
            var gridColor = new Color(0.93f, 0.87f, 0.66f, 0.20f);
            for (var column = 0; column <= verticalCount; column++)
            {
                var x = mapRect.x + column / _viewColumns * mapRect.width;
                DrawLine(new Vector2(x, mapRect.y),
                    new Vector2(x, mapRect.yMax), gridColor, 1f);
            }
            for (var row = 0; row <= horizontalCount; row++)
            {
                var y = mapRect.y + row / _viewRows * mapRect.height;
                DrawLine(new Vector2(mapRect.x, y),
                    new Vector2(mapRect.xMax, y), gridColor, 1f);
            }
        }

        private void DrawPointerValidationFeedback(Rect mapRect)
        {
            if (!_pointerOnMap || ToolState == null ||
                ToolState.PrimaryTool == CountyPlanningPrimaryTool.None)
                return;
            string text;
            PlacementValidationState state;
            if (ShouldDrawBuildingGhost && Validation != null)
            {
                state = Validation.State;
                text = state == PlacementValidationState.Invalid
                    ? "无法规划\n" + Validation.PrimaryReason
                    : state == PlacementValidationState.Conditional
                        ? "条件性草案\n" + Validation.PrimaryReason
                        : "可以规划\n左键放置，R旋转";
            }
            else if (_geometryValidation != null)
            {
                state = _geometryValidation.State;
                text = GeometryFeedback();
            }
            else return;
            var width = 250f;
            var height = 52f;
            var x = Mathf.Min(mapRect.width - width - 8f,
                _pointerLocalGui.x + 18f);
            var y = Mathf.Min(mapRect.height - height - 8f,
                _pointerLocalGui.y + 18f);
            var color = state == PlacementValidationState.Invalid
                ? new Color(0.55f, 0.10f, 0.08f, 0.94f)
                : state == PlacementValidationState.Conditional
                    ? new Color(0.47f, 0.34f, 0.07f, 0.94f)
                    : new Color(0.09f, 0.39f, 0.18f, 0.94f);
            DrawFilled(new Rect(x, y, width, height), color);
            GUI.Label(new Rect(x + 8f, y + 5f, width - 16f, height - 10f),
                text);
        }

        private void DrawPath(IReadOnlyList<PlanningCellCoord> path,
            Rect mapRect, Color color, float width)
        {
            if (path == null || path.Count < 2) return;
            for (var index = 0; index < path.Count - 1; index++)
                DrawLine(ToGuiPosition(CellCenter(path[index]), mapRect),
                    ToGuiPosition(CellCenter(path[index + 1]), mapRect),
                    color, width);
        }

        private void DrawWall(
            IReadOnlyList<DraftFortificationSegment> segments,
            Rect mapRect, Color color, float width)
        {
            foreach (var segment in segments ??
                         Array.Empty<DraftFortificationSegment>())
            {
                var center = CellCenter(segment.Cell);
                var half = DualScaleCountySpatialContractV1
                    .PlanningCellSizeMetres * 0.5d;
                GlobalProjectedCoordinate first;
                GlobalProjectedCoordinate second;
                if (segment.EdgeDirection == PlanningCellDirection.North ||
                    segment.EdgeDirection == PlanningCellDirection.South)
                {
                    var north = center.NorthingMetres +
                        (segment.EdgeDirection == PlanningCellDirection.North
                            ? half : -half);
                    first = new GlobalProjectedCoordinate(
                        center.EastingMetres - half, north);
                    second = new GlobalProjectedCoordinate(
                        center.EastingMetres + half, north);
                }
                else
                {
                    var east = center.EastingMetres +
                        (segment.EdgeDirection == PlanningCellDirection.East
                            ? half : -half);
                    first = new GlobalProjectedCoordinate(east,
                        center.NorthingMetres - half);
                    second = new GlobalProjectedCoordinate(east,
                        center.NorthingMetres + half);
                }
                DrawLine(ToGuiPosition(first, mapRect),
                    ToGuiPosition(second, mapRect), color, width);
            }
        }

        private void DrawPlanningCell(PlanningCellCoord cell, Rect mapRect,
            Color color)
        {
            var center = CellCenter(cell);
            var half = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres * 0.49d;
            var bounds = new PlanningMetricBounds(
                center.EastingMetres - half, center.EastingMetres + half,
                center.NorthingMetres - half, center.NorthingMetres + half);
            DrawFilled(ToGuiBounds(bounds, mapRect), color);
        }

        private GlobalProjectedCoordinate CellCenter(PlanningCellCoord cell) =>
            _projection.PlanningCellCenter(cell);

        private static Color ZoneColor(CountyPlanningZoneKind kind)
        {
            switch (kind)
            {
                case CountyPlanningZoneKind.Production:
                    return new Color(0.70f, 0.35f, 0.16f, 0.28f);
                case CountyPlanningZoneKind.Storage:
                    return new Color(0.55f, 0.48f, 0.76f, 0.28f);
                case CountyPlanningZoneKind.Agriculture:
                    return new Color(0.36f, 0.68f, 0.23f, 0.28f);
                default:
                    return new Color(0.82f, 0.68f, 0.28f, 0.28f);
            }
        }

        private void RebuildPlanningMapTexture()
        {
            if (_mapTexture == null || _prototype == null) return;
            var partition = _prototype.Partition;
            var pixels = new Color32[partition.Rows * partition.Columns];
            for (var row = 0; row < partition.Rows; row++)
            for (var column = 0; column < partition.Columns; column++)
            {
                var terrain = partition.Terrain(row, column);
                Color32 color;
                if (MapOverlays != null &&
                    MapOverlays.TerrainAnalysisVisible)
                {
                    var slope = partition.SlopeBasis(row, column);
                    color = !partition.IsBuildable(row, column)
                        ? new Color32(111, 60, 54, 255)
                        : new Color32((byte)(74 + slope),
                            (byte)Mathf.Clamp(132 - slope, 45, 132), 67, 255);
                }
                else if (terrain == PlanningTerrainClass.Hill)
                    color = new Color32(99, 105, 62, 255);
                else if (terrain == PlanningTerrainClass.Forest)
                    color = new Color32(49, 91, 54, 255);
                else color = new Color32(89, 116, 64, 255);

                color = ApplyCountySurfaceWash(color, row, column);

                if ((MapOverlays == null || MapOverlays.RiversVisible) &&
                    partition.WaterState(row, column) > 0)
                    color = new Color32(45, 113, 151, 255);
                pixels[(partition.Rows - 1 - row) * partition.Columns +
                       column] = color;
            }
            _mapTexture.SetPixels32(pixels);
            _mapTexture.Apply(false, false);
        }

        private void DrawNonBuildingDraftEvidence(Color32[] pixels,
            int width, int height, int mapX, int mapY, int mapWidth,
            int mapHeight)
        {
            if (Session == null) return;
            var cellWidth = Math.Max(1, (int)Math.Round(mapWidth /
                Math.Max(1f, _viewColumns)));
            var cellHeight = Math.Max(1, (int)Math.Round(mapHeight /
                Math.Max(1f, _viewRows)));
            foreach (var zone in Session.ZoneDrafts)
            foreach (var cell in zone.Cells)
            {
                EvidencePosition(CellCenter(cell), mapX, mapY, mapWidth,
                    mapHeight, out var x, out var y);
                FillPixelRect(pixels, width, height, x - cellWidth / 2,
                    y - cellHeight / 2, cellWidth, cellHeight,
                    new Color32(151, 178, 70, 130));
            }
            foreach (var road in Session.RoadDrafts)
                DrawEvidencePath(pixels, width, height, mapX, mapY,
                    mapWidth, mapHeight, road.Path,
                    new Color32(58, 230, 255, 255));
            foreach (var canal in Session.CanalDrafts)
                DrawEvidencePath(pixels, width, height, mapX, mapY,
                    mapWidth, mapHeight, canal.Path,
                    new Color32(42, 154, 255, 255));
            foreach (var wall in Session.FortificationDrafts)
                DrawEvidencePath(pixels, width, height, mapX, mapY,
                    mapWidth, mapHeight,
                    wall.Segments.Select(value => value.Cell).ToArray(),
                    new Color32(232, 201, 127, 255));
        }

        private void DrawEvidencePath(Color32[] pixels, int width,
            int height, int mapX, int mapY, int mapWidth, int mapHeight,
            IReadOnlyList<PlanningCellCoord> path, Color32 color)
        {
            if (path == null || path.Count == 0) return;
            for (var index = 0; index < path.Count - 1; index++)
            {
                EvidencePosition(CellCenter(path[index]), mapX, mapY,
                    mapWidth, mapHeight, out var x0, out var y0);
                EvidencePosition(CellCenter(path[index + 1]), mapX, mapY,
                    mapWidth, mapHeight, out var x1, out var y1);
                DrawPixelLine(pixels, width, height, x0, y0, x1, y1,
                    color, 4);
            }
            if (path.Count == 1)
            {
                EvidencePosition(CellCenter(path[0]), mapX, mapY, mapWidth,
                    mapHeight, out var x, out var y);
                FillPixelRect(pixels, width, height, x - 2, y - 2, 5, 5,
                    color);
            }
        }
    }
}
