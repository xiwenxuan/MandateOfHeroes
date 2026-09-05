using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
    public enum CountyPlanningFixture : byte
    {
        ValidResidence,
        LargeFacility,
        ExistingFacilityCollision,
        WaterBlocking,
        FortificationBlocking,
        RoadAccessInvalid,
        BeaconNearWall,
        OutsideCounty
    }

    public sealed partial class LuoyangCountyPlanningPresentationController :
        MonoBehaviour
    {
        private Texture2D _mapTexture;
        private DualScaleCoordinateProjection _projection;
        private Luoyang50mCountySpatialPrototype _prototype;
        private PlanningFacilityFootprint _footprint;
        private float _viewMinimumRow;
        private float _viewMinimumColumn;
        private float _viewRotationDegrees;
        private float _viewRows = PlanningViewRows;
        private float _viewColumns = PlanningViewColumns;
        private int _urbanPresentationMinimumRow;
        private int _urbanPresentationMaximumRow;
        private int _urbanPresentationMinimumColumn;
        private int _urbanPresentationMaximumColumn;
        private Luoyang50mCountyLayoutPackage _layoutPackage;
        private IReadOnlyList<FacilityPlacementProfile>
            _playerFacingBuildingProfiles =
                Array.Empty<FacilityPlacementProfile>();
        private LuoyangCountyWorldSpacePresentationController
            _worldSpacePresentation;
        private const int PlanningViewRows = 24;
        private const int PlanningViewColumns = 48;
        // The current review-candidate hull includes rural supply and water
        // anchors at the county edges.  Fitting that whole hull would make the
        // urban sub-view indistinguishable from the county overview, so the
        // presentation camera uses a bounded mid-scale window around its
        // declared centre.  This does not alter the layout package.
        private const int UrbanViewMaximumRows = 160;
        private const int UrbanViewMaximumColumns = 320;
        private readonly Dictionary<CountyPlanningFixture,
            PlanningSelection> _fixtures =
                new Dictionary<CountyPlanningFixture, PlanningSelection>();

        private readonly struct PlanningSelection
        {
            public PlanningSelection(string profileId, int row, int column,
                int rotation)
            {
                ProfileId = profileId;
                Row = row;
                Column = column;
                Rotation = rotation;
            }
            public string ProfileId { get; }
            public int Row { get; }
            public int Column { get; }
            public int Rotation { get; }
        }

        public bool IsReady { get; private set; }
        public bool IsActive { get; private set; }
        public string LastError { get; private set; }
        public FacilityPlacementProfileCatalog Profiles { get; private set; }
        public IReadOnlyList<FacilityPlacementProfile>
            PlayerFacingBuildingProfiles => _playerFacingBuildingProfiles;
        public FacilityPlacementValidator Validator { get; private set; }
        public CountyPlanningSession Session { get; private set; }
        public FacilityPlacementProfile SelectedProfile { get; private set; }
        public PlacementValidationResult Validation { get; private set; }
        public PlanningCellInspection CellInspection { get; private set; }
        public int SelectedLocalRow { get; private set; }
        public int SelectedLocalColumn { get; private set; }
        public int HoveredLocalRow { get; private set; } = -1;
        public int HoveredLocalColumn { get; private set; } = -1;
        public bool HasHoveredPlanningCell => HoveredLocalRow >= 0 &&
            HoveredLocalColumn >= 0;
        public int PreviewLocalRow => HasHoveredPlanningCell
            ? HoveredLocalRow : SelectedLocalRow;
        public int PreviewLocalColumn => HasHoveredPlanningCell
            ? HoveredLocalColumn : SelectedLocalColumn;
        public int RotationQuarterTurns { get; private set; }
        public CountyPlanningPerformanceSnapshot Performance
            { get; private set; }
        public PlanningFacilityFootprint CurrentFootprint => _footprint;
        public IReadOnlyList<DraftBuildingBlueprint> Drafts =>
            Session?.Drafts ?? Array.Empty<DraftBuildingBlueprint>();
        public Texture2D MapTexture => _mapTexture;
        public int PlanningCellGameObjectCount => 0;
        public int CountyMapRenderObjectCount => _worldSpacePresentation != null &&
                                                _worldSpacePresentation.IsBuilt
            ? _worldSpacePresentation.Summary?.TerrainChunkCount ?? 0
            : _mapTexture == null ? 0 : 1;
        public float ViewMinimumRow => _viewMinimumRow;
        public float ViewMinimumColumn => _viewMinimumColumn;
        public float ViewRotationDegrees => _viewRotationDegrees;
        public float ViewRows => _viewRows;
        public float ViewColumns => _viewColumns;
        public CountySubViewMode PresentationMode { get; private set; } =
            CountySubViewMode.Planning;
        public string CountyId => _prototype?.Partition.CountyId;
        public int FacilityCount => _layoutPackage?.Facilities.Count ?? 0;
        public string LayoutFingerprint =>
            _layoutPackage?.DeclaredLayoutFingerprint ?? string.Empty;
        public string UrbanAreaId =>
            _layoutPackage?.UrbanAreaCandidate.UrbanAreaId ?? string.Empty;
        public Luoyang50mLayoutFacility SelectedObservedFacility =>
            _layoutPackage?.Facilities
                .Where(item => Math.Abs(item.LocalRow - SelectedLocalRow) <= 1 &&
                               Math.Abs(item.LocalColumn -
                                        SelectedLocalColumn) <= 1)
                .OrderBy(item => Math.Abs(item.LocalRow - SelectedLocalRow) +
                                 Math.Abs(item.LocalColumn -
                                          SelectedLocalColumn))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .FirstOrDefault();
        public Luoyang50mCountyLayoutPackage LayoutPackage => _layoutPackage;
        public CountySpatialPartition Partition => _prototype?.Partition;
        public LuoyangCountyWorldSpacePresentationController
            WorldSpacePresentation => _worldSpacePresentation;
        public bool UsesWorldSpacePresentation =>
            _worldSpacePresentation != null && _worldSpacePresentation.IsBuilt;
        public bool ShouldDrawBuildingGhostWorldSpace =>
            ShouldDrawBuildingGhost;

        public bool IsInsideUrbanPresentation(int row, int column,
            int marginCells = 0) =>
            row >= _urbanPresentationMinimumRow - marginCells &&
            row <= _urbanPresentationMaximumRow + marginCells &&
            column >= _urbanPresentationMinimumColumn - marginCells &&
            column <= _urbanPresentationMaximumColumn + marginCells;

        public bool Begin(string countyId)
        {
            return Begin(countyId, CountySubViewMode.Planning);
        }

        public bool Begin(string countyId, CountySubViewMode mode)
        {
            if (!string.Equals(countyId,
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    StringComparison.Ordinal))
            {
                LastError = "V1 只开放洛阳县域规划工具。";
                return false;
            }
            try
            {
                // Unity can preserve the auto-property value across a hot
                // reload while non-serializable runtime sources are reset.
                // Treat readiness as a complete runtime contract instead of
                // trusting the flag alone.
                if (!IsReady || _prototype == null ||
                    _layoutPackage == null || Profiles == null ||
                    Validator == null)
                    Initialize();
                Session = new CountyPlanningSession(countyId);
                InitializeVisualPlanningInteraction();
                IsActive = true;
                _viewRotationDegrees = 0f;
                SelectFixture(CountyPlanningFixture.ValidResidence);
                ActivateBuildingTool(SelectedProfile.ProfileId);
                SetPresentationMode(mode);
                Performance = CountyPlanningPerformanceBenchmark.Measure(
                    Validator, SelectedProfile, SelectedLocalRow,
                    SelectedLocalColumn, Session, 64);
                LastError = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
                IsActive = false;
                return false;
            }
        }

        public void End()
        {
            IsActive = false;
            CancelPlanningTool();
            _worldSpacePresentation?.Hide();
        }

        public bool EnsureWorldSpacePresentation(Camera presentationCamera)
        {
            if (!IsReady || presentationCamera == null) return false;
            try
            {
                _worldSpacePresentation = _worldSpacePresentation ??
                    GetComponent<
                        LuoyangCountyWorldSpacePresentationController>() ??
                    gameObject.AddComponent<
                        LuoyangCountyWorldSpacePresentationController>();
                _worldSpacePresentation.Initialize(this, presentationCamera);
                return _worldSpacePresentation.IsBuilt;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
                return false;
            }
        }

        public void SetWorldSpaceDebugVisible(bool visible)
        {
            _worldSpacePresentation?.SetDebugVisible(visible);
        }

        public bool SetPresentationMode(CountySubViewMode mode)
        {
            if (!IsReady) return false;
            PresentationMode = mode;
            if (mode == CountySubViewMode.Overview)
            {
                _viewRows = _prototype.Partition.Rows;
                _viewColumns = _prototype.Partition.Columns;
                SetViewportMinimum(0f, 0f);
            }
            else if (mode == CountySubViewMode.UrbanArea)
            {
                var area = _layoutPackage.UrbanAreaCandidate;
                var rows = Mathf.Clamp(area.MaximumRow - area.MinimumRow + 17,
                    PlanningViewRows, Mathf.Min(UrbanViewMaximumRows,
                        _prototype.Partition.Rows));
                var columns = Mathf.Clamp(
                    Mathf.Max(area.MaximumColumn - area.MinimumColumn + 17,
                        rows * 2), PlanningViewColumns,
                    Mathf.Min(UrbanViewMaximumColumns,
                        _prototype.Partition.Columns));
                _viewRows = rows;
                _viewColumns = columns;
                var urbanCenterRow = (_urbanPresentationMinimumRow +
                                      _urbanPresentationMaximumRow) * 0.5f;
                var urbanCenterColumn = (_urbanPresentationMinimumColumn +
                                         _urbanPresentationMaximumColumn) *
                                        0.5f;
                SetViewportMinimum(
                    urbanCenterRow - _viewRows * 0.5f,
                    urbanCenterColumn - _viewColumns * 0.5f);
            }
            else
            {
                _viewRows = PlanningViewRows;
                _viewColumns = PlanningViewColumns;
                CenterViewport(SelectedLocalRow, SelectedLocalColumn);
            }
            ResetCountyPresentationLod();
            return true;
        }

        public bool FocusGoldenBlockPrototype()
        {
            if (!IsReady) return false;
            if (PresentationMode != CountySubViewMode.UrbanArea)
                SetPresentationMode(CountySubViewMode.UrbanArea);
            var golden = _worldSpacePresentation?.GoldenBlockPlan ??
                         new CountyGoldenBlockPresentationPlan(
                             _layoutPackage);
            SelectedLocalRow = (golden.MinimumRow + golden.MaximumRow) / 2;
            SelectedLocalColumn = (golden.MinimumColumn +
                                   golden.MaximumColumn) / 2;
            HoveredLocalRow = SelectedLocalRow;
            HoveredLocalColumn = SelectedLocalColumn;
            _viewRows = 64f;
            _viewColumns = 128f;
            CenterViewport(SelectedLocalRow, SelectedLocalColumn);
            RefreshValidation();
            ResetCountyPresentationLod();
            return true;
        }

        public bool FocusGoldenBlockBuildMode()
        {
            if (!FocusGoldenBlockPrototype()) return false;
            SetPresentationMode(CountySubViewMode.Planning);
            _viewRows = PlanningViewRows;
            _viewColumns = PlanningViewColumns;
            CenterViewport(SelectedLocalRow, SelectedLocalColumn);
            ResetCountyPresentationLod();
            return true;
        }

        public bool FocusGoldenBlockLot(
            CountyGoldenBlockArchetype archetype, bool near)
        {
            if (!IsReady) return false;
            SetPresentationMode(CountySubViewMode.UrbanArea);
            var golden = _worldSpacePresentation?.GoldenBlockPlan ??
                         new CountyGoldenBlockPresentationPlan(
                             _layoutPackage);
            var lot = golden.Lots
                .Where(item => item.Archetype == archetype)
                .OrderBy(item => item.CenterRow)
                .ThenBy(item => item.CenterColumn)
                .FirstOrDefault();
            if (lot == null) return false;
            SelectedLocalRow = Mathf.RoundToInt(lot.CenterRow);
            SelectedLocalColumn = Mathf.RoundToInt(lot.CenterColumn);
            HoveredLocalRow = SelectedLocalRow;
            HoveredLocalColumn = SelectedLocalColumn;
            _viewRows = near ? 18f : 42f;
            _viewColumns = near ? 36f : 84f;
            CenterViewport(SelectedLocalRow, SelectedLocalColumn);
            RefreshValidation();
            ResetCountyPresentationLod();
            return true;
        }

        public bool SelectProfile(string profileId)
        {
            if (!IsReady || !Profiles.ProfilesById.TryGetValue(profileId,
                    out var profile)) return false;
            SelectedProfile = profile;
            if (!profile.AllowsRotation(RotationQuarterTurns))
                RotationQuarterTurns = profile.AllowedRotationQuarterTurns[0];
            RefreshValidation();
            ActivateBuildingTool(profile.ProfileId);
            return true;
        }

        public bool SelectCell(int localRow, int localColumn)
        {
            return SelectCell(localRow, localColumn, true);
        }

        private bool SelectCell(int localRow, int localColumn,
            bool centerViewport)
        {
            if (!IsReady) return false;
            SelectedLocalRow = localRow;
            SelectedLocalColumn = localColumn;
            HoveredLocalRow = localRow;
            HoveredLocalColumn = localColumn;
            if (centerViewport) CenterViewport(localRow, localColumn);
            RefreshValidation();
            return true;
        }

        public bool SelectCellFromMap(Rect mapRect, Vector2 guiPosition)
        {
            if (!TryResolveCellFromMap(mapRect, guiPosition, out var row,
                    out var column)) return false;
            return SelectCell(row, column, false);
        }

        public bool HoverCellFromMap(Rect mapRect, Vector2 guiPosition)
        {
            if (!TryResolveCellFromMap(mapRect, guiPosition, out var row,
                    out var column)) return false;
            HoveredLocalRow = row;
            HoveredLocalColumn = column;
            RefreshValidation(row, column, false);
            return true;
        }

        public bool SetHoveredPlanningCell(int localRow, int localColumn)
        {
            if (!IsReady || localRow < 0 || localRow >=
                    _prototype.Partition.Rows || localColumn < 0 ||
                localColumn >= _prototype.Partition.Columns) return false;
            HoveredLocalRow = localRow;
            HoveredLocalColumn = localColumn;
            RefreshValidation(localRow, localColumn, false);
            return true;
        }

        public void ClearHoveredPlanningCell()
        {
            HoveredLocalRow = -1;
            HoveredLocalColumn = -1;
            RefreshValidation();
        }

        private bool TryResolveCellFromMap(Rect mapRect, Vector2 guiPosition,
            out int resolvedRow, out int resolvedColumn)
        {
            resolvedRow = -1;
            resolvedColumn = -1;
            if (!IsActive || !mapRect.Contains(guiPosition)) return false;
            if (_worldSpacePresentation != null &&
                _worldSpacePresentation.IsVisible &&
                _worldSpacePresentation.TryGuiPointToCell(mapRect,
                    guiPosition, out var worldRow, out var worldColumn))
            {
                resolvedRow = worldRow;
                resolvedColumn = worldColumn;
                return true;
            }
            var mapPosition = RotateGuiVector(guiPosition - mapRect.center,
                -_viewRotationDegrees) + mapRect.center;
            if (!mapRect.Contains(mapPosition)) return false;
            var column = _viewMinimumColumn + Mathf.FloorToInt(
                (mapPosition.x - mapRect.x) / mapRect.width * _viewColumns);
            var row = _viewMinimumRow + Mathf.FloorToInt(
                (mapPosition.y - mapRect.y) / mapRect.height * _viewRows);
            resolvedRow = Mathf.Clamp(Mathf.FloorToInt(row), 0,
                _prototype.Partition.Rows - 1);
            resolvedColumn = Mathf.Clamp(Mathf.FloorToInt(column), 0,
                _prototype.Partition.Columns - 1);
            return true;
        }

        public bool PanViewportByGuiDelta(Vector2 guiDelta, Rect mapRect)
        {
            if (!IsActive || mapRect.width <= 0f || mapRect.height <= 0f)
                return false;
            var mapDelta = RotateGuiVector(guiDelta,
                -_viewRotationDegrees);
            var previousRow = _viewMinimumRow;
            var previousColumn = _viewMinimumColumn;
            SetViewportMinimum(
                _viewMinimumRow - mapDelta.y / mapRect.height * _viewRows,
                _viewMinimumColumn - mapDelta.x / mapRect.width *
                _viewColumns);
            var changed = !Mathf.Approximately(previousRow,
                              _viewMinimumRow) ||
                          !Mathf.Approximately(previousColumn,
                              _viewMinimumColumn);
            if (changed) RefreshCountyPresentation(false);
            return changed;
        }

        public bool RotateViewportByGuiDelta(Vector2 guiDelta)
        {
            if (!IsActive || Mathf.Abs(guiDelta.x) < 0.001f) return false;
            _viewRotationDegrees = Mathf.Repeat(
                _viewRotationDegrees + guiDelta.x * 0.32f, 360f);
            return true;
        }

        public bool ZoomViewport(float wheelDelta, Vector2 anchor)
        {
            if (!IsActive || Mathf.Abs(wheelDelta) < 0.001f) return false;
            anchor.x = Mathf.Clamp01(anchor.x);
            anchor.y = Mathf.Clamp01(anchor.y);
            var anchorRow = _viewMinimumRow + anchor.y * _viewRows;
            var anchorColumn = _viewMinimumColumn + anchor.x * _viewColumns;
            var factor = wheelDelta > 0f ? 0.82f : 1.22f;
            var newRows = Mathf.Clamp(_viewRows * factor, 12f,
                _prototype.Partition.Rows);
            var newColumns = Mathf.Clamp(_viewColumns * factor, 24f,
                _prototype.Partition.Columns);
            if (newColumns < newRows * 2f)
                newColumns = Mathf.Min(_prototype.Partition.Columns,
                    newRows * 2f);
            if (newRows < newColumns * 0.5f)
                newRows = Mathf.Min(_prototype.Partition.Rows,
                    newColumns * 0.5f);
            _viewRows = newRows;
            _viewColumns = newColumns;
            SetViewportMinimum(anchorRow - anchor.y * _viewRows,
                anchorColumn - anchor.x * _viewColumns);
            UpdateCountyPresentationLod();
            return true;
        }

        public void RotateClockwise()
        {
            if (!IsReady) return;
            for (var offset = 1; offset <= 4; offset++)
            {
                var candidate = (RotationQuarterTurns + offset) % 4;
                if (!SelectedProfile.AllowsRotation(candidate)) continue;
                RotationQuarterTurns = candidate;
                ToolState?.SetRotation(candidate);
                RefreshValidation(PreviewLocalRow, PreviewLocalColumn,
                    false);
                return;
            }
        }

        public DraftBuildingBlueprint CreateDraft()
        {
            if (!IsActive || ToolState == null ||
                ToolState.PrimaryTool != CountyPlanningPrimaryTool.Building ||
                Validation == null || !Validation.IsValid)
                return null;
            var draft = Session.CreateDraft(SelectedProfile, _footprint,
                Validation);
            RefreshValidation();
            return draft;
        }

        public ICountyPlanningDraft Undo()
        {
            var result = Session?.Undo();
            RefreshValidation();
            return result;
        }

        public ICountyPlanningDraft Redo()
        {
            var result = Session?.Redo();
            RefreshValidation();
            return result;
        }

        public bool SelectFixture(CountyPlanningFixture fixture)
        {
            if (!IsReady || !_fixtures.TryGetValue(fixture,
                    out var selection)) return false;
            SelectedProfile = Profiles.ProfilesById[selection.ProfileId];
            SelectedLocalRow = selection.Row;
            SelectedLocalColumn = selection.Column;
            HoveredLocalRow = selection.Row;
            HoveredLocalColumn = selection.Column;
            RotationQuarterTurns = selection.Rotation;
            ToolState?.SetRotation(selection.Rotation);
            CenterViewport(selection.Row, selection.Column);
            RefreshValidation();
            return true;
        }

        public void DrawMap(Rect mapRect)
        {
            if (_worldSpacePresentation != null &&
                _worldSpacePresentation.IsBuilt)
            {
                _worldSpacePresentation.Show(mapRect);
                if (PresentationMode == CountySubViewMode.Planning)
                {
                    GUI.BeginGroup(mapRect);
                    DrawPointerValidationFeedback(new Rect(0f, 0f,
                        mapRect.width, mapRect.height));
                    GUI.EndGroup();
                }
                return;
            }
            if (!IsActive || _mapTexture == null) return;
            GUI.BeginGroup(mapRect);
            var localMapRect = new Rect(0f, 0f, mapRect.width,
                mapRect.height);
            DrawFilled(localMapRect, new Color(0.08f, 0.09f, 0.06f));
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(_viewRotationDegrees,
                localMapRect.center);
            var uv = new Rect(
                _viewMinimumColumn /
                (float)_prototype.Partition.Columns,
                (_prototype.Partition.Rows - _viewMinimumRow - _viewRows) /
                (float)_prototype.Partition.Rows,
                _viewColumns / _prototype.Partition.Columns,
                _viewRows / _prototype.Partition.Rows);
            GUI.DrawTextureWithTexCoords(localMapRect, _mapTexture, uv,
                false);
            DrawUrbanAreaHighlight(localMapRect);
            DrawCountyPresentationLayers(localMapRect);
            if (PresentationMode == CountySubViewMode.Planning &&
                (MapOverlays == null || MapOverlays.PlanningVisible))
            {
                DrawPlanningGridAndOverlays(localMapRect);
                DrawNonBuildingDrafts(localMapRect);
                DrawDrafts(localMapRect);
                if (ShouldDrawBuildingGhost)
                    DrawCurrentPreview(localMapRect);
            }
            if (MapOverlays == null ||
                MapOverlays.AdministrativeVisible)
                DrawOutline(localMapRect,
                    new Color(0.62f, 0.53f, 0.35f, 0.82f), 2f);
            GUI.matrix = previousMatrix;
            if (PresentationMode == CountySubViewMode.Planning)
                DrawPointerValidationFeedback(localMapRect);
            GUI.EndGroup();
        }

        public void CaptureEvidence(string absolutePath, int width = 1280,
            int height = 720)
        {
            if (!IsActive) throw new InvalidOperationException(
                "Planning presentation is not active.");
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var image = new Texture2D(width, height, TextureFormat.RGB24,
                false);
            var pixels = Enumerable.Repeat(new Color32(17, 21, 16, 255),
                width * height).ToArray();
            var mapWidth = Math.Min(640, width - 620);
            var mapHeight = Math.Min(320, height - 220);
            var mapX = (width - mapWidth) / 2;
            var mapY = (height - mapHeight) / 2;
            FillPixelRect(pixels, width, height, 0, height - 78, width, 78,
                new Color32(45, 47, 24, 255));
            FillPixelRect(pixels, width, height, 18, 70, mapX - 34,
                height - 166, new Color32(38, 40, 24, 255));
            FillPixelRect(pixels, width, height, mapX + mapWidth + 16, 70,
                width - mapX - mapWidth - 34, height - 166,
                new Color32(38, 40, 24, 255));
            var source = _mapTexture.GetPixels32();
            for (var y = 0; y < mapHeight; y++)
            for (var x = 0; x < mapWidth; x++)
            {
                var sourceX = Mathf.Clamp(Mathf.FloorToInt(
                    _viewMinimumColumn + x * _viewColumns /
                    (float)mapWidth), 0, _mapTexture.width - 1);
                var localRow = Mathf.Clamp(Mathf.FloorToInt(
                    _viewMinimumRow + (mapHeight - 1 - y) * _viewRows /
                    (float)mapHeight), 0, _mapTexture.height - 1);
                var sourceY = _mapTexture.height - 1 - localRow;
                pixels[(mapY + y) * width + mapX + x] =
                    source[sourceY * _mapTexture.width + sourceX];
            }
            for (var cell = 0; cell <= Mathf.RoundToInt(_viewColumns); cell++)
            {
                var x = mapX + Mathf.RoundToInt(cell * mapWidth /
                    _viewColumns);
                DrawPixelLine(pixels, width, height, x, mapY, x,
                    mapY + mapHeight, new Color32(226, 210, 151, 55), 1);
            }
            for (var cell = 0; cell <= Mathf.RoundToInt(_viewRows); cell++)
            {
                var y = mapY + Mathf.RoundToInt(cell * mapHeight /
                    _viewRows);
                DrawPixelLine(pixels, width, height, mapX, y,
                    mapX + mapWidth, y,
                    new Color32(226, 210, 151, 55), 1);
            }
            DrawNonBuildingDraftEvidence(pixels, width, height, mapX, mapY,
                mapWidth, mapHeight);
            foreach (var draft in Drafts)
                DrawEvidenceBounds(pixels, width, height, mapX, mapY,
                    mapWidth, mapHeight, draft.Bounds,
                    new Color32(65, 217, 244, 220), 2);
            var stateColor = Validation.State ==
                    PlacementValidationState.Valid
                ? new Color32(69, 244, 107, 240)
                : Validation.State == PlacementValidationState.Conditional
                    ? new Color32(255, 193, 51, 240)
                    : new Color32(255, 62, 51, 240);
            DrawEvidenceBounds(pixels, width, height, mapX, mapY, mapWidth,
                mapHeight, _footprint.Bounds, stateColor, 4);
            var entrance = _footprint.Entrances.Single(value => value.Primary);
            EvidencePosition(entrance.Position, mapX, mapY, mapWidth,
                mapHeight, out var entranceX, out var entranceY);
            FillPixelRect(pixels, width, height, entranceX - 4,
                entranceY - 4, 9, 9, new Color32(255, 255, 255, 255));
            if (Validation.RoadAccessResult.DistanceCentimetres >= 0)
            {
                EvidencePosition(Validation.RoadAccessResult
                        .ConnectionPosition, mapX, mapY, mapWidth, mapHeight,
                    out var roadX, out var roadY);
                DrawPixelLine(pixels, width, height, entranceX, entranceY,
                    roadX, roadY, stateColor, 3);
            }
            FillPixelRect(pixels, width, height, 38, height - 58, 220, 16,
                stateColor);
            FillPixelRect(pixels, width, height, width - 286, height - 58,
                248, 16, Drafts.Count > 0
                    ? new Color32(65, 217, 244, 255)
                    : new Color32(104, 104, 82, 255));
            image.SetPixels32(pixels);
            image.Apply(false);
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            if (Application.isPlaying) Object.Destroy(image);
            else Object.DestroyImmediate(image);
        }

        private void DrawEvidenceBounds(Color32[] pixels, int width,
            int height, int mapX, int mapY, int mapWidth, int mapHeight,
            PlanningMetricBounds bounds, Color32 color, int thickness)
        {
            EvidencePosition(new GlobalProjectedCoordinate(
                    bounds.MinimumEasting, bounds.MinimumNorthing), mapX,
                mapY, mapWidth, mapHeight, out var minimumX,
                out var minimumY);
            EvidencePosition(new GlobalProjectedCoordinate(
                    bounds.MaximumEasting, bounds.MaximumNorthing), mapX,
                mapY, mapWidth, mapHeight, out var maximumX,
                out var maximumY);
            var left = Math.Min(minimumX, maximumX);
            var right = Math.Max(minimumX, maximumX);
            var bottom = Math.Min(minimumY, maximumY);
            var top = Math.Max(minimumY, maximumY);
            FillPixelRect(pixels, width, height, left, bottom,
                Math.Max(1, right - left), Math.Max(1, top - bottom),
                new Color32(color.r, color.g, color.b, 90));
            DrawPixelLine(pixels, width, height, left, bottom, right,
                bottom, color, thickness);
            DrawPixelLine(pixels, width, height, right, bottom, right, top,
                color, thickness);
            DrawPixelLine(pixels, width, height, right, top, left, top,
                color, thickness);
            DrawPixelLine(pixels, width, height, left, top, left, bottom,
                color, thickness);
        }

        private void EvidencePosition(GlobalProjectedCoordinate position,
            int mapX, int mapY, int mapWidth, int mapHeight, out int x,
            out int y)
        {
            var partition = _prototype.Partition;
            var origin = _projection.PlanningCellCenter(
                partition.ToGlobalCell(0, 0));
            var half = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres * 0.5d;
            var west = origin.EastingMetres - half;
            var north = origin.NorthingMetres + half;
            var cellSize = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres;
            var localColumn = (position.EastingMetres - west) / cellSize;
            var localRow = (north - position.NorthingMetres) / cellSize;
            x = mapX + (int)Math.Round((localColumn -
                _viewMinimumColumn) / _viewColumns * mapWidth);
            y = mapY + mapHeight - (int)Math.Round((localRow -
                _viewMinimumRow) / _viewRows * mapHeight);
        }

        private static void FillPixelRect(Color32[] pixels, int width,
            int height, int x, int y, int rectWidth, int rectHeight,
            Color32 color)
        {
            var minimumX = Math.Max(0, x);
            var maximumX = Math.Min(width, x + rectWidth);
            var minimumY = Math.Max(0, y);
            var maximumY = Math.Min(height, y + rectHeight);
            for (var row = minimumY; row < maximumY; row++)
            for (var column = minimumX; column < maximumX; column++)
            {
                var index = row * width + column;
                if (color.a == 255)
                    pixels[index] = color;
                else
                {
                    var alpha = color.a / 255f;
                    var previous = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Math.Round(previous.r * (1f - alpha) +
                                         color.r * alpha),
                        (byte)Math.Round(previous.g * (1f - alpha) +
                                         color.g * alpha),
                        (byte)Math.Round(previous.b * (1f - alpha) +
                                         color.b * alpha), 255);
                }
            }
        }

        private static void DrawPixelLine(Color32[] pixels, int width,
            int height, int x0, int y0, int x1, int y1, Color32 color,
            int thickness)
        {
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                FillPixelRect(pixels, width, height,
                    x0 - thickness / 2, y0 - thickness / 2, thickness,
                    thickness, color);
                if (x0 == x1 && y0 == y1) break;
                var doubled = error * 2;
                if (doubled >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (doubled <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private void Initialize()
        {
            var root = Path.Combine(Application.streamingAssetsPath,
                "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            _prototype = source.Prototype;
            _layoutPackage = source.LayoutPackage;
            ResolveUrbanPresentationFrame();
            Profiles = new LuoyangFacilityPlacementProfileSource(root).Catalog;
            var playerFacingDefinitions = new[]
            {
                "facility.residential.urban_quarter",
                "facility.commercial.market",
                "facility.industry.workshop",
                "facility.storage.warehouse",
                "facility.government.local_office"
            };
            _playerFacingBuildingProfiles = playerFacingDefinitions.Select(
                definitionId => Profiles.ProfilesByDefinitionId[definitionId])
                .ToArray();
            _projection = new DualScaleCoordinateProjection();
            Validator = new FacilityPlacementValidator(_prototype,
                source.LayoutPackage, _projection);
            InitializeCountyPresentationStack();
            BuildTexture();
            BuildFixtures();
            IsReady = true;
        }

        private void BuildTexture()
        {
            var partition = _prototype.Partition;
            _mapTexture = new Texture2D(partition.Columns, partition.Rows,
                TextureFormat.RGBA32, false, true)
            {
                name = "Luoyang County Planning Runtime Map V1",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            RebuildPlanningMapTexture();
        }

        private void BuildFixtures()
        {
            var residence = Profiles.ProfilesByDefinitionId[
                "facility.residential.urban_quarter"];
            var large = Profiles.ProfilesByDefinitionId[
                "facility.commercial.market"];
            var valid = FindValidNearRoad(residence);
            _fixtures[CountyPlanningFixture.ValidResidence] = valid;
            _fixtures[CountyPlanningFixture.LargeFacility] =
                FindValidNearRoad(large, new PlanningSelection(
                    large.ProfileId, valid.Row, valid.Column,
                    valid.Rotation));

            var existing = _prototype.Partition.FacilityPlacements.Values
                .OrderBy(value => value.FacilityId, StringComparer.Ordinal)
                .First();
            var existingCell = _projection.ToPlanningCell(existing.Center);
            _prototype.Partition.TryToLocal(existingCell, out var existingRow,
                out var existingColumn);
            _fixtures[CountyPlanningFixture.ExistingFacilityCollision] =
                new PlanningSelection(residence.ProfileId, existingRow,
                    existingColumn, 0);

            var water = FindCell((row, column) =>
                _prototype.Partition.WaterState(row, column) > 0);
            _fixtures[CountyPlanningFixture.WaterBlocking] =
                new PlanningSelection(residence.ProfileId, water.Row,
                    water.Column, 0);

            var wall = _prototype.Partition.Fortifications.Values
                .OrderBy(value => value.Id, StringComparer.Ordinal).First();
            _prototype.Partition.TryToLocal(wall.Edge.First, out var wallRow,
                out var wallColumn);
            var warehouse = Profiles.ProfilesByDefinitionId[
                "facility.storage.warehouse"];
            _fixtures[CountyPlanningFixture.FortificationBlocking] =
                new PlanningSelection(warehouse.ProfileId, wallRow,
                    wallColumn, 0);

            var beacon = Profiles.ProfilesByDefinitionId[
                "facility.military.beacon"];
            _fixtures[CountyPlanningFixture.BeaconNearWall] =
                new PlanningSelection(beacon.ProfileId, wallRow,
                    wallColumn, 0);

            _fixtures[CountyPlanningFixture.RoadAccessInvalid] =
                FindRoadInvalid(residence);
            _fixtures[CountyPlanningFixture.OutsideCounty] =
                new PlanningSelection(residence.ProfileId, -1, -1, 0);
        }

        private void ResolveUrbanPresentationFrame()
        {
            // UrbanAreaCandidate is a county-wide convex hull because its
            // source also contains rural supply and water anchors.  It is a
            // valid data boundary, but not a useful camera target.  The main
            // city wall is the stable presentation frame for Luoyang's urban
            // sub-view and does not create or move any world fact.
            var cityWall = _layoutPackage.Fortifications.Where(item =>
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_wall",
                        StringComparison.Ordinal) ||
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_gate",
                        StringComparison.Ordinal))
                .ToArray();
            if (cityWall.Length > 0)
            {
                _urbanPresentationMinimumRow = cityWall.Min(item =>
                    item.LocalRow);
                _urbanPresentationMaximumRow = cityWall.Max(item =>
                    item.LocalRow);
                _urbanPresentationMinimumColumn = cityWall.Min(item =>
                    item.LocalColumn);
                _urbanPresentationMaximumColumn = cityWall.Max(item =>
                    item.LocalColumn);
                return;
            }

            var area = _layoutPackage.UrbanAreaCandidate;
            _urbanPresentationMinimumRow = area.MinimumRow;
            _urbanPresentationMaximumRow = area.MaximumRow;
            _urbanPresentationMinimumColumn = area.MinimumColumn;
            _urbanPresentationMaximumColumn = area.MaximumColumn;
        }

        private PlanningSelection FindValidNearRoad(
            FacilityPlacementProfile profile,
            PlanningSelection? fallback = null)
        {
            var partition = _prototype.Partition;
            var focusRow = (_urbanPresentationMinimumRow +
                            _urbanPresentationMaximumRow) / 2;
            var focusColumn = (_urbanPresentationMinimumColumn +
                               _urbanPresentationMaximumColumn) / 2;
            PlanningSelection? best = null;
            var bestDistance = int.MaxValue;
            for (var row = 1; row < partition.Rows - 1; row++)
            for (var column = 1; column < partition.Columns - 1; column++)
            {
                if (partition.LandUse(row, column) !=
                    PlanningLandUseClass.Road) continue;
                for (var distance = 1; distance <= 4; distance++)
                {
                    var candidates = new[]
                    {
                        new PlanningSelection(profile.ProfileId,
                            row - distance, column, 0),
                        new PlanningSelection(profile.ProfileId, row,
                            column + distance, 1),
                        new PlanningSelection(profile.ProfileId,
                            row + distance, column, 2),
                        new PlanningSelection(profile.ProfileId, row,
                            column - distance, 3)
                    };
                    foreach (var candidate in candidates)
                    {
                        if (candidate.Row < 0 || candidate.Row >=
                                partition.Rows || candidate.Column < 0 ||
                            candidate.Column >= partition.Columns) continue;
                        var deltaRow = candidate.Row - focusRow;
                        var deltaColumn = candidate.Column - focusColumn;
                        var candidateDistance = deltaRow * deltaRow +
                                                deltaColumn * deltaColumn;
                        if (candidateDistance >= bestDistance) continue;
                        var validation = Validator.Validate(profile,
                            candidate.Row, candidate.Column,
                            candidate.Rotation, null);
                        if (validation.State !=
                            PlacementValidationState.Invalid)
                        {
                            best = candidate;
                            bestDistance = candidateDistance;
                        }
                    }
                }
            }
            if (best.HasValue) return best.Value;
            if (fallback.HasValue) return fallback.Value;
            throw new InvalidOperationException(
                "No valid planning fixture could be found near a real road.");
        }

        private PlanningSelection FindRoadInvalid(
            FacilityPlacementProfile profile)
        {
            var partition = _prototype.Partition;
            for (var row = 8; row < partition.Rows - 8; row += 8)
            for (var column = 8; column < partition.Columns - 8;
                 column += 8)
            {
                var result = Validator.Validate(profile, row, column, 0,
                    null);
                if (result.BlockingReasons.Any(value => value.Code ==
                        PlacementReasonIds.RoadNoRoad || value.Code ==
                        PlacementReasonIds.RoadTooFar || value.Code ==
                        PlacementReasonIds.RoadWrongSide))
                    return new PlanningSelection(profile.ProfileId, row,
                        column, 0);
            }
            return new PlanningSelection(profile.ProfileId, 8, 8, 0);
        }

        private PlanningCellCoord FindCell(Func<int, int, bool> predicate)
        {
            for (var row = 0; row < _prototype.Partition.Rows; row++)
            for (var column = 0; column <
                 _prototype.Partition.Columns; column++)
                if (predicate(row, column))
                    return new PlanningCellCoord(row, column);
            throw new InvalidOperationException(
                "Requested planning fixture Cell does not exist.");
        }

        private void RefreshValidation()
        {
            RefreshValidation(SelectedLocalRow, SelectedLocalColumn, true);
        }

        private void RefreshValidation(int localRow, int localColumn,
            bool updateInspection)
        {
            if (!IsReady || SelectedProfile == null) return;
            _footprint = Validator.CreateFootprint(SelectedProfile,
                localRow, localColumn,
                RotationQuarterTurns);
            var ignoredDraftId = ToolState != null &&
                                 ToolState.PrimaryTool ==
                                 CountyPlanningPrimaryTool.MoveDraft
                ? ToolState.EditingDraftId
                : null;
            Validation = Validator.Validate(_footprint, Session,
                ignoredDraftId);
            if (updateInspection)
                CellInspection = localRow >= 0 &&
                                 localRow < _prototype.Partition.Rows &&
                                 localColumn >= 0 &&
                                 localColumn < _prototype.Partition.Columns
                    ? Validator.InspectCell(localRow, localColumn)
                    : null;
        }

        private void DrawDrafts(Rect mapRect)
        {
            if (Session == null) return;
            foreach (var draft in Session.Drafts)
            {
                var bounds = ToGuiBounds(draft.Bounds, mapRect);
                DrawFilled(bounds, new Color(0.24f, 0.72f, 0.86f, 0.36f));
                DrawOutline(bounds, new Color(0.32f, 0.89f, 1f), 2f);
            }
        }

        private void DrawUrbanAreaHighlight(Rect mapRect)
        {
            var hull = _layoutPackage?.UrbanAreaCandidate?.HullCells;
            if (hull == null || hull.Count < 3) return;
            var color = PresentationMode == CountySubViewMode.UrbanArea
                ? new Color(0.96f, 0.68f, 0.18f, 0.96f)
                : new Color(0.93f, 0.62f, 0.16f, 0.72f);
            for (var index = 0; index < hull.Count; index++)
            {
                var first = hull[index];
                var second = hull[(index + 1) % hull.Count];
                var start = new Vector2(
                    mapRect.x + (first.Column - _viewMinimumColumn) /
                    _viewColumns * mapRect.width,
                    mapRect.y + (first.Row - _viewMinimumRow) /
                    _viewRows * mapRect.height);
                var end = new Vector2(
                    mapRect.x + (second.Column - _viewMinimumColumn) /
                    _viewColumns * mapRect.width,
                    mapRect.y + (second.Row - _viewMinimumRow) /
                    _viewRows * mapRect.height);
                DrawLine(start, end, color,
                    PresentationMode == CountySubViewMode.UrbanArea
                        ? 3f : 2f);
            }
        }

        private void DrawCurrentPreview(Rect mapRect)
        {
            if (_footprint == null || Validation == null) return;
            var bounds = ToGuiBounds(_footprint.Bounds, mapRect);
            var color = Validation.State == PlacementValidationState.Valid
                ? new Color(0.25f, 0.95f, 0.42f, 0.88f)
                : Validation.State == PlacementValidationState.Conditional
                    ? new Color(1f, 0.76f, 0.20f, 0.9f)
                    : new Color(1f, 0.25f, 0.20f, 0.9f);
            DrawFilled(bounds, new Color(color.r, color.g, color.b, 0.38f));
            DrawOutline(bounds, color, 3f);
            foreach (var entrance in _footprint.Entrances)
            {
                var position = ToGuiPosition(entrance.Position, mapRect);
                DrawFilled(new Rect(position.x - 4f, position.y - 4f,
                    8f, 8f), entrance.Primary ? Color.white : color);
            }
            var road = Validation.RoadAccessResult;
            if (road.Status != FacilityRoadAccessStatus.NoRoad &&
                road.Status != FacilityRoadAccessStatus.NotRequired &&
                road.DistanceCentimetres >= 0)
            {
                var start = ToGuiPosition(road.EntrancePosition, mapRect);
                var end = ToGuiPosition(road.ConnectionPosition, mapRect);
                DrawLine(start, end, road.Status ==
                    FacilityRoadAccessStatus.Connected
                    ? new Color(0.45f, 1f, 0.75f) : color, 2f);
            }
            if (bounds.width > 36f && bounds.height > 16f)
                GUI.Label(bounds, SelectedProfile.DisplayName);
        }

        private Rect ToGuiBounds(PlanningMetricBounds bounds, Rect mapRect)
        {
            var northWest = ToGuiPosition(new GlobalProjectedCoordinate(
                bounds.MinimumEasting, bounds.MaximumNorthing), mapRect);
            var southEast = ToGuiPosition(new GlobalProjectedCoordinate(
                bounds.MaximumEasting, bounds.MinimumNorthing), mapRect);
            return Rect.MinMaxRect(northWest.x, northWest.y, southEast.x,
                southEast.y);
        }

        private Vector2 ToGuiPosition(GlobalProjectedCoordinate position,
            Rect mapRect)
        {
            var partition = _prototype.Partition;
            var origin = _projection.PlanningCellCenter(
                partition.ToGlobalCell(0, 0));
            var half = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres * 0.5d;
            var west = origin.EastingMetres - half;
            var north = origin.NorthingMetres + half;
            var cellSize = DualScaleCountySpatialContractV1
                .PlanningCellSizeMetres;
            var localColumn = (position.EastingMetres - west) / cellSize;
            var localRow = (north - position.NorthingMetres) / cellSize;
            return new Vector2(
                mapRect.x + (float)((localColumn - _viewMinimumColumn) /
                    _viewColumns) * mapRect.width,
                mapRect.y + (float)((localRow - _viewMinimumRow) /
                    _viewRows) * mapRect.height);
        }

        private void CenterViewport(int row, int column)
        {
            SetViewportMinimum(row - _viewRows * 0.5f,
                column - _viewColumns * 0.5f);
        }

        private void SetViewportMinimum(float row, float column)
        {
            var maximumMinimumRow = Mathf.Max(0f,
                _prototype.Partition.Rows - _viewRows);
            var maximumMinimumColumn = Mathf.Max(0f,
                _prototype.Partition.Columns - _viewColumns);
            _viewMinimumRow = Mathf.Clamp(row, 0f, maximumMinimumRow);
            _viewMinimumColumn = Mathf.Clamp(column, 0f,
                maximumMinimumColumn);
        }

        private static Vector2 RotateGuiVector(Vector2 value,
            float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Vector2(cosine * value.x - sine * value.y,
                sine * value.x + cosine * value.y);
        }

        private void Paint(Color32[] pixels, int row, int column,
            Color32 color, int radius)
        {
            var rows = _prototype.Partition.Rows;
            var columns = _prototype.Partition.Columns;
            for (var dr = -radius; dr <= radius; dr++)
            for (var dc = -radius; dc <= radius; dc++)
            {
                var targetRow = row + dr;
                var targetColumn = column + dc;
                if (targetRow < 0 || targetRow >= rows ||
                    targetColumn < 0 || targetColumn >= columns) continue;
                pixels[(rows - 1 - targetRow) * columns + targetColumn] =
                    color;
            }
        }

        private static void DrawFilled(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawOutline(Rect rect, Color color,
            float thickness)
        {
            DrawFilled(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawFilled(new Rect(rect.x, rect.yMax - thickness, rect.width,
                thickness), color);
            DrawFilled(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawFilled(new Rect(rect.xMax - thickness, rect.y, thickness,
                rect.height), color);
        }

        private static void DrawLine(Vector2 first, Vector2 second,
            Color color, float width)
        {
            var matrix = GUI.matrix;
            var angle = Vector2.SignedAngle(Vector2.right, second - first);
            GUIUtility.RotateAroundPivot(angle, first);
            DrawFilled(new Rect(first.x, first.y - width * 0.5f,
                Vector2.Distance(first, second), width), color);
            GUI.matrix = matrix;
        }

        private void OnDestroy()
        {
            if (_mapTexture == null) return;
            if (Application.isPlaying) Object.Destroy(_mapTexture);
            else Object.DestroyImmediate(_mapTexture);
        }
    }
}
