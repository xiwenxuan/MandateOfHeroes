using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public sealed class AdministrativeMapLabelProjection
    {
        public string RegionId;
        public string DisplayName;
        public AdministrativeRegionLevel Level;
        public int Row;
        public int Column;
        public bool Selected;
    }

    public sealed class AdministrativeSelectionProjection
    {
        public string RegionId;
        public string DisplayName;
        public AdministrativeRegionLevel Level;
        public string RegionType;
        public string ParentCommanderyId;
        public string ParentCommanderyName;
        public string ParentProvinceId;
        public string ParentProvinceName;
        public AdministrativeGeometryStatus GeometryStatus;
        public string SourceGeometryStatus;
        public string Confidence;
        public bool Provisional;
        public long CellCount;
        public int BoundarySegmentCount;
        public int PublicRoadCellCount;
        public List<string> PublicMajorSettlements = new List<string>();
        public string ActualControllerSummary = "遵守有限认知：未接入可公开控制情报";
    }

    public sealed class WorldSettlementMarkerProjection
    {
        public string LocationId;
        public string RegionId;
        public string DisplayName;
        public int Row;
        public int Column;
    }

    public sealed partial class HanWorldNaturalMapController
    {
        private HanAdministrativeGeographySource _administrativeSource;
        private AdministrativeBoundaryTopology _administrativeTopology;
        private readonly AdministrativeMapViewState _administrativeMapViewState =
            new AdministrativeMapViewState();
        private GameObject _administrativeBoundaryRoot;
        private GameObject _administrativeSelectionRoot;
        private GameObject _strategicDioramaSettlementRoot;
        private Material _provinceBoundaryMaterial;
        private Material _commanderyBoundaryMaterial;
        private Material _countyBoundaryMaterial;
        private Material _selectedBoundaryMaterial;
        private Material _selectedCountyFillMaterial;
        private Material _strategicDioramaSettlementMaterial;
        private bool _administrativeOverlayVisible = true;
        private double _administrativeBoundaryBuildMilliseconds;
        private double _administrativeRenderBuildMilliseconds;
        private long _administrativeBoundaryCacheBytes;
        private long _administrativeRenderGcDeltaBytes;
        private int _administrativeRenderedChunkCount;
        private int _administrativeRenderedSegmentCount;
        private AdministrativeSelectionProjection _administrativeSelection;
        private int _strategicDioramaSettlementCount;

        public AdministrativeBoundaryTopology AdministrativeBoundaryTopology =>
            _administrativeTopology;
        public AdministrativeMapViewState AdministrativeMapViewState =>
            _administrativeMapViewState;
        public bool AdministrativeOverlayVisible =>
            _administrativeOverlayVisible;
        public AdministrativeSelectionProjection AdministrativeSelection =>
            _administrativeSelection;
        public double AdministrativeBoundaryBuildMilliseconds =>
            _administrativeBoundaryBuildMilliseconds;
        public double AdministrativeRenderBuildMilliseconds =>
            _administrativeRenderBuildMilliseconds;
        public long AdministrativeBoundaryCacheBytes =>
            _administrativeBoundaryCacheBytes;
        public long AdministrativeRenderGcDeltaBytes =>
            _administrativeRenderGcDeltaBytes;
        public int AdministrativeRenderObjectCount =>
            (_administrativeBoundaryRoot?.transform.childCount ?? 0) +
            (_administrativeSelectionRoot?.transform.childCount ?? 0);
        public int AdministrativeRenderedChunkCount =>
            _administrativeRenderedChunkCount;
        public int AdministrativeRenderedSegmentCount =>
            _administrativeRenderedSegmentCount;
        public int AdministrativeScenarioStartYear =>
            _administrativeSource?.ScenarioStartYear ?? 0;
        public int StrategicDioramaSettlementCount =>
            _strategicDioramaSettlementCount;
        public int StrategicDioramaSettlementRenderObjectCount =>
            _strategicDioramaSettlementRoot?.transform.childCount ?? 0;

        public bool TryResolveCountyId(ulong cellId64, out string countyId)
        {
            countyId = string.Empty;
            if (_administrativeSource == null ||
                !GlobalSpatialFoundationV1.CreateCellGrid().TryDecode(
                    new WorldMapCellId(cellId64), out var row,
                    out var column) ||
                !_administrativeSource.TryGetCountyAtCell(row, column,
                    out var county)) return false;
            countyId = county.Id;
            return true;
        }

        public bool TryResolveCountyIdForLocation(string locationId,
            out string countyId)
        {
            countyId = string.Empty;
            if (_administrativeSource == null ||
                string.IsNullOrWhiteSpace(locationId)) return false;
            var normalized = locationId.Trim();
            foreach (var feature in _administrativeSource.Cities.Features ??
                     new List<WorldMapLocationFeature>())
            {
                var item = feature?.Properties;
                if (item == null || !string.Equals(item.CityId, normalized,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(
                        item.AdministrativeRegionId)) continue;
                if (_administrativeSource.RegionCatalog.TryGet(
                        item.AdministrativeRegionId, out var cityCounty) &&
                    cityCounty.Level == AdministrativeRegionLevel.County)
                {
                    countyId = cityCounty.Id;
                    return true;
                }
            }
            const string placePrefix = "place.";
            if (!normalized.StartsWith(placePrefix,
                    StringComparison.Ordinal)) return false;
            var candidate = "admin." + normalized.Substring(
                placePrefix.Length);
            if (!_administrativeSource.RegionCatalog.TryGet(candidate,
                    out var county) ||
                county.Level != AdministrativeRegionLevel.County)
                return false;
            countyId = county.Id;
            return true;
        }

        public bool TryGetAdministrativeRegionDisplayName(string regionId,
            out string displayName)
        {
            displayName = string.Empty;
            if (_administrativeSource == null ||
                string.IsNullOrWhiteSpace(regionId) ||
                !_administrativeSource.RegionCatalog.TryGet(regionId,
                    out var region)) return false;
            displayName = region.DisplayName;
            return true;
        }

        public bool FocusWorldNearCounty(string countyId,
            float orthographicSize = 330f)
        {
            if (!TryEnsureRuntimeReferences("FocusWorldNearCounty") ||
                View != HanNaturalMapView.World ||
                string.IsNullOrWhiteSpace(countyId) ||
                !_administrativeTopology.RegionSummaries.TryGetValue(
                    countyId, out var summary) ||
                summary.Region.Level != AdministrativeRegionLevel.County)
                return false;
            _focusRow = summary.CenterRow;
            _focusColumn = summary.CenterColumn;
            ConfigureCamera(GlobalCellCenterToLocal(summary.CenterRow,
                    summary.CenterColumn),
                Mathf.Clamp(orthographicSize, 180f, 1160f),
                IsHanStrategicDiorama
                    ? orthographicSize <= 220f ? 53f : 56f
                    : 64f,
                IsHanStrategicDiorama
                    ? orthographicSize <= 220f ? -8f : -5f
                    : -8f);
            RefreshWorldStrategicGridForCamera(summary.CenterRow,
                summary.CenterColumn);
            RefreshAdministrativePresentation();
            return true;
        }

        public IReadOnlyList<WorldSettlementMarkerProjection>
            GetVisibleSettlementMarkers()
        {
            if (_administrativeSource == null || _camera == null)
                return Array.Empty<WorldSettlementMarkerProjection>();
            var result = new List<WorldSettlementMarkerProjection>();
            foreach (var feature in _administrativeSource.Cities.Features ??
                     new List<WorldMapLocationFeature>())
            {
                var item = feature?.Properties;
                if (item == null || !item.Row.HasValue ||
                    !item.Column.HasValue ||
                    string.IsNullOrWhiteSpace(item.CityId)) continue;
                if (View != HanNaturalMapView.World &&
                    !IsInsideCurrentRegionWindow(item.Row.Value,
                        item.Column.Value)) continue;
                var assignment = _administrativeSource.ReadAssignment(
                    item.Row.Value, item.Column.Value);
                result.Add(new WorldSettlementMarkerProjection
                {
                    LocationId = item.CityId,
                    RegionId = assignment.CountyRegionId,
                    DisplayName = item.DisplayName ?? item.CityId,
                    Row = item.Row.Value,
                    Column = item.Column.Value
                });
            }
            result.Sort((left, right) => string.CompareOrdinal(
                left.LocationId, right.LocationId));
            return result;
        }

        public bool TryGetSettlementMarkerViewport(
            WorldSettlementMarkerProjection marker, out Vector2 point)
        {
            point = default;
            if (marker == null || _camera == null || _source == null)
                return false;
            var local = GlobalCellCenterToLocal(marker.Row, marker.Column);
            var cell = _source.ReadSample(marker.Row, marker.Column).Cell;
            local.y = GetPresentationHeightForGlobal(cell.CenterX,
                cell.CenterY) + (View == HanNaturalMapView.World ? 2f : 0.5f);
            var viewport = _camera.WorldToViewportPoint(local);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f) return false;
            point = new Vector2(viewport.x, viewport.y);
            return true;
        }

        private void InitializeAdministrativeGeography(string worldRoot,
            int scenarioStartYear)
        {
            _administrativeSource = new HanAdministrativeGeographySource(
                worldRoot, scenarioStartYear);
            var before = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();
            _administrativeTopology = AdministrativeBoundaryTopologyBuilder.Build(
                _administrativeSource);
            stopwatch.Stop();
            _administrativeBoundaryBuildMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds;
            _administrativeBoundaryCacheBytes = Math.Max(0L,
                GC.GetTotalMemory(false) - before);

            _administrativeBoundaryRoot = NewRoot(
                "Administrative Boundary Batched Chunks");
            _administrativeSelectionRoot = NewRoot(
                "Administrative Region Selection");
            _strategicDioramaSettlementRoot = NewRoot(
                "Han Strategic Diorama Settlement Batch");
            _provinceBoundaryMaterial = CreateAdministrativeMaterial(
                "Province Boundary", new Color(0.46f, 0.12f, 0.09f, 0.98f));
            _commanderyBoundaryMaterial = CreateAdministrativeMaterial(
                "Commandery Boundary", new Color(0.81f, 0.55f, 0.20f, 0.94f));
            _countyBoundaryMaterial = CreateAdministrativeMaterial(
                "County Boundary", new Color(0.88f, 0.82f, 0.63f, 0.82f));
            _selectedBoundaryMaterial = CreateAdministrativeMaterial(
                "Selected Administrative Boundary",
                new Color(1f, 0.75f, 0.12f, 1f));
            _selectedCountyFillMaterial = CreateAdministrativeMaterial(
                "Selected County Fill", new Color(0.95f, 0.61f, 0.10f, 0.18f));
            _strategicDioramaSettlementMaterial = CreateNaturalMaterial(
                "Han Strategic Diorama Settlement Material", 0.01f);
            ApplyAdministrativeArtProfile();
        }

        private void ApplyAdministrativeArtProfile()
        {
            if (_provinceBoundaryMaterial == null) return;
            var ink = ActiveArtStyle ==
                HanWorldArtStyle.InkLandscapePrototype;
            _provinceBoundaryMaterial.color = ink
                ? new Color(0.42f, 0.19f, 0.14f, 0.72f)
                : new Color(0.38f, 0.23f, 0.16f, 0.66f);
            _commanderyBoundaryMaterial.color = ink
                ? new Color(0.27f, 0.22f, 0.17f, 0.92f)
                : new Color(0.64f, 0.48f, 0.27f, 0.62f);
            _countyBoundaryMaterial.color = ink
                ? new Color(0.34f, 0.32f, 0.27f, 0.70f)
                : new Color(0.78f, 0.73f, 0.58f, 0.54f);
            _selectedBoundaryMaterial.color = ink
                ? new Color(0.72f, 0.18f, 0.10f, 1f)
                : new Color(1f, 0.75f, 0.12f, 1f);
            _selectedCountyFillMaterial.color = ink
                ? new Color(0.68f, 0.18f, 0.10f, 0.16f)
                : new Color(0.95f, 0.61f, 0.10f, 0.18f);
            if (_strategicDioramaSettlementMaterial != null)
                ApplyMaterial(_strategicDioramaSettlementMaterial,
                    Color.white, 0.01f);
        }

        public void SetAdministrativeOverlayVisible(bool visible)
        {
            _administrativeOverlayVisible = visible;
            RefreshAdministrativePresentation();
        }

        public void SetAdministrativeLabelLevel(
            AdministrativeMapLabelLevel level)
        {
            if (_administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning) return;
            _administrativeMapViewState.SetWorldLabelLevel(level);
            _administrativeMapViewState.ClearSelection();
            _administrativeSelection = null;
            RefreshAdministrativePresentation();
        }

        public bool TrySelectAdministrativeRegion(Vector2 viewportPoint,
            AdministrativeRegionLevel? forcedLevel = null)
        {
            if (!TryResolveAdministrativeViewportCell(viewportPoint,
                    out var cellId) ||
                !GlobalSpatialFoundationV1.CreateCellGrid().TryDecode(cellId,
                    out var row, out var column))
                return false;
            var assignment = _administrativeSource.ReadAssignment(row, column);
            if (!assignment.IsMapped) return false;
            var level = forcedLevel ?? ResolveSelectionLevel();
            var regionId = level == AdministrativeRegionLevel.Province
                ? assignment.ProvinceRegionId
                : level == AdministrativeRegionLevel.CommanderyEquivalent
                    ? assignment.CommanderyRegionId
                    : assignment.CountyRegionId;
            var region = _administrativeSource.RegionCatalog.Get(regionId);
            _administrativeMapViewState.Select(region);
            _administrativeSelection = BuildSelectionProjection(region);
            RefreshAdministrativePresentation();
            return true;
        }

        public bool AdjustAdministrativeZoom(float wheelDelta,
            Vector2 anchorViewport)
        {
            if (_camera == null || Math.Abs(wheelDelta) < 0.001f ||
                anchorViewport.x < 0f || anchorViewport.x > 1f ||
                anchorViewport.y < 0f || anchorViewport.y > 1f)
                return false;
            var zoomIn = wheelDelta < 0f;
            if (View == HanNaturalMapView.World && zoomIn &&
                _camera.orthographicSize <= 205f &&
                TryResolveAdministrativeViewportCell(anchorViewport,
                    out var focusCell) &&
                GlobalSpatialFoundationV1.CreateCellGrid().TryDecode(
                    focusCell, out var row, out var column))
            {
                SetRegionView(row, column, VisualTerrainDetailLevel.Region);
                ConfigureCamera(Vector3.zero, 96f, 58f, -12f);
                _administrativeMapViewState.SetWorldLabelLevel(
                    AdministrativeMapLabelLevel.CommanderyEquivalent);
                RefreshAdministrativePresentation();
                return true;
            }
            if (View == HanNaturalMapView.Region &&
                _administrativeMapViewState.ViewMode !=
                    AdministrativeMapViewMode.CountyPlanning &&
                !zoomIn && _camera.orthographicSize >= 116f)
            {
                SetWorldView();
                return true;
            }

            var hasAnchor = TryGroundPoint(anchorViewport, out var before);
            var factor = zoomIn ? 0.86f : 1.16f;
            var minimum = _administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning ? 0.65f : 1.2f;
            var maximum = View == HanNaturalMapView.World ? 1160f : 120f;
            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize * factor, minimum, maximum);
            if (hasAnchor && TryGroundPoint(anchorViewport, out var after))
                _camera.transform.position += before - after;

            if (_administrativeMapViewState.ViewMode !=
                AdministrativeMapViewMode.CountyPlanning)
            {
                var level = View == HanNaturalMapView.World
                    ? (_camera.orthographicSize > 500f
                        ? AdministrativeMapLabelLevel.Province
                        : AdministrativeMapLabelLevel.CommanderyEquivalent)
                    : (_camera.orthographicSize > 58f
                        ? AdministrativeMapLabelLevel.CommanderyEquivalent
                        : AdministrativeMapLabelLevel.County);
                if (_administrativeMapViewState.LabelLevel != level)
                {
                    _administrativeMapViewState.SetWorldLabelLevel(level);
                    _administrativeMapViewState.ClearSelection();
                    _administrativeSelection = null;
                    RefreshAdministrativePresentation();
                }
            }
            if (View == HanNaturalMapView.World)
                RefreshWorldStrategicGridForCamera();
            else RefreshStrategicCellGrid();
            RefreshStrategicDioramaSettlements();
            return true;
        }

        public void PanAdministrativeMap(Vector2 viewportDelta)
        {
            if (_camera == null) return;
            var right = Vector3.ProjectOnPlane(_camera.transform.right,
                Vector3.up).normalized;
            var up = Vector3.ProjectOnPlane(_camera.transform.up,
                Vector3.up).normalized;
            var vertical = _camera.orthographicSize * 2f;
            var horizontal = vertical * Math.Max(0.2f, _camera.aspect);
            _camera.transform.position -= right *
                (viewportDelta.x * horizontal);
            _camera.transform.position += up *
                (viewportDelta.y * vertical);
        }

        public void RotateAdministrativeMap(float yawDegrees)
        {
            if (_camera == null || Math.Abs(yawDegrees) < 0.001f ||
                !TryGroundPoint(new Vector2(0.5f, 0.5f), out var focus))
                return;
            var rotation = Quaternion.AngleAxis(yawDegrees, Vector3.up);
            _camera.transform.position = focus + rotation *
                (_camera.transform.position - focus);
            _camera.transform.rotation = rotation *
                _camera.transform.rotation;
        }

        private bool TryResolveAdministrativeViewportCell(
            Vector2 viewportPoint, out WorldMapCellId cellId)
        {
            cellId = default;
            return TryGroundPoint(viewportPoint, out var local) &&
                TryPickGlobalCell(local, out cellId);
        }

        private bool TryGroundPoint(Vector2 viewportPoint,
            out Vector3 local)
        {
            local = default;
            if (_camera == null || viewportPoint.x < 0f ||
                viewportPoint.x > 1f || viewportPoint.y < 0f ||
                viewportPoint.y > 1f) return false;
            var ray = _camera.ViewportPointToRay(new Vector3(
                viewportPoint.x, viewportPoint.y, 0f));
            if (Physics.Raycast(ray, out var hit, _camera.farClipPlane))
            {
                local = hit.point;
                return true;
            }
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var distance)) return false;
            local = ray.GetPoint(distance);
            return true;
        }

        public bool SelectAdministrativeRegion(string regionId)
        {
            if (_administrativeSource == null ||
                !_administrativeSource.RegionCatalog.TryGet(regionId,
                    out var region) ||
                !_administrativeTopology.RegionSummaries.ContainsKey(regionId))
                return false;
            _administrativeMapViewState.Select(region);
            _administrativeSelection = BuildSelectionProjection(region);
            RefreshAdministrativePresentation();
            return true;
        }

        public bool EnterCountyPlanning(string countyId)
        {
            if (_administrativeSource == null ||
                !_administrativeSource.RegionCatalog.TryGet(countyId,
                    out var county) ||
                county.Level != AdministrativeRegionLevel.County ||
                !_administrativeTopology.RegionSummaries.TryGetValue(countyId,
                    out var spatial))
                return false;
            _administrativeMapViewState.EnterCountyPlanning(spatial);
            _administrativeSelection = BuildSelectionProjection(county);
            SetRegionView(_administrativeMapViewState.CameraCenterRow,
                _administrativeMapViewState.CameraCenterColumn,
                VisualTerrainDetailLevel.City);
            var span = Math.Max(_administrativeMapViewState.CameraSpanRows,
                _administrativeMapViewState.CameraSpanColumns);
            ConfigureCamera(Vector3.zero,
                Mathf.Clamp(span * 0.68f, 24f, 96f), 58f, -12f);
            RefreshAdministrativePresentation();
            return true;
        }

        public void ExitCountyPlanning()
        {
            _administrativeMapViewState.ExitCountyPlanning();
            _administrativeSelection = null;
            SetWorldView();
        }

        public IReadOnlyList<AdministrativeMapLabelProjection>
            GetVisibleAdministrativeLabels()
        {
            var result = new List<AdministrativeMapLabelProjection>();
            if (_administrativeTopology == null ||
                !_administrativeOverlayVisible) return result;
            var selectedId = _administrativeMapViewState.
                SelectedAdministrativeRegionId;
            var neighborCounties = _administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning
                ? ResolveNeighborCountyIds(
                    _administrativeMapViewState.PlanningCountyId)
                : new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in _administrativeTopology.RegionSummaries)
            {
                var summary = pair.Value;
                var include = ShouldIncludeLabel(summary.Region,
                    neighborCounties);
                if (!include) continue;
                if (View != HanNaturalMapView.World &&
                    !IsInsideCurrentRegionWindow(summary.CenterRow,
                        summary.CenterColumn))
                    continue;
                result.Add(new AdministrativeMapLabelProjection
                {
                    RegionId = summary.Region.Id,
                    DisplayName = summary.Region.DisplayName,
                    Level = summary.Region.Level,
                    Row = summary.CenterRow,
                    Column = summary.CenterColumn,
                    Selected = string.Equals(summary.Region.Id, selectedId,
                        StringComparison.Ordinal)
                });
            }
            result.Sort((left, right) =>
            {
                if (left.Selected != right.Selected)
                    return left.Selected ? -1 : 1;
                var level = left.Level.CompareTo(right.Level);
                return level != 0 ? level :
                    string.CompareOrdinal(left.RegionId, right.RegionId);
            });
            return result;
        }

        public bool TryGetAdministrativeLabelViewport(
            AdministrativeMapLabelProjection label, out Vector2 point)
        {
            point = default;
            if (label == null || _camera == null || _source == null ||
                label.Row < 0 || label.Column < 0) return false;
            var local = GlobalCellCenterToLocal(label.Row, label.Column);
            var cell = _source.ReadSample(label.Row, label.Column).Cell;
            local.y = GetPresentationHeightForGlobal(cell.CenterX,
                cell.CenterY) + 0.4f;
            var viewport = _camera.WorldToViewportPoint(local);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f) return false;
            point = new Vector2(viewport.x, viewport.y);
            return true;
        }

        private void RefreshAdministrativePresentation()
        {
            if (_administrativeBoundaryRoot == null ||
                _administrativeSelectionRoot == null) return;
            ClearAdministrativeMeshes(_administrativeBoundaryRoot.transform);
            ClearAdministrativeMeshes(_administrativeSelectionRoot.transform);
            ClearStrategicDioramaSettlements();
            _administrativeRenderedChunkCount = 0;
            _administrativeRenderedSegmentCount = 0;
            if (!_administrativeOverlayVisible ||
                _administrativeTopology == null) return;

            var before = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();
            var province = new List<AdministrativeBoundarySegment>();
            var commandery = new List<AdministrativeBoundarySegment>();
            var county = new List<AdministrativeBoundarySegment>();
            var selected = new List<AdministrativeBoundarySegment>();
            var selectedId = _administrativeMapViewState.
                SelectedAdministrativeRegionId;
            var visibleChunks = VisibleAdministrativeChunks();
            foreach (var chunk in visibleChunks)
            {
                _administrativeRenderedChunkCount++;
                foreach (var segment in chunk.Segments)
                {
                    var highest = segment.HighestLevel;
                    if (!IsBoundaryLevelVisible(highest)) continue;
                    if (!string.IsNullOrEmpty(selectedId) &&
                        SegmentTouchesRegion(segment, selectedId,
                            _administrativeMapViewState.SelectedLevel))
                        selected.Add(segment);
                    if (highest == AdministrativeRegionLevel.Province)
                        province.Add(segment);
                    else if (highest ==
                             AdministrativeRegionLevel.CommanderyEquivalent)
                        commandery.Add(segment);
                    else county.Add(segment);
                }
            }
            AddBoundaryMesh("Province Boundary Batch", province,
                AdministrativeRegionLevel.Province,
                _provinceBoundaryMaterial, _administrativeBoundaryRoot);
            AddBoundaryMesh("Commandery Boundary Batch", commandery,
                AdministrativeRegionLevel.CommanderyEquivalent,
                _commanderyBoundaryMaterial, _administrativeBoundaryRoot);
            AddBoundaryMesh("County Boundary Batch", county,
                AdministrativeRegionLevel.County, _countyBoundaryMaterial,
                _administrativeBoundaryRoot);
            AddBoundaryMesh("Selected Region Boundary", selected,
                _administrativeMapViewState.SelectedLevel ??
                    AdministrativeRegionLevel.County,
                _selectedBoundaryMaterial, _administrativeSelectionRoot,
                1.9f);
            AddSelectedCountyFill();
            _administrativeRenderedSegmentCount = province.Count +
                commandery.Count + county.Count + selected.Count;
            RefreshStrategicDioramaSettlements();
            stopwatch.Stop();
            _administrativeRenderBuildMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds;
            _administrativeRenderGcDeltaBytes = Math.Max(0L,
                GC.GetTotalMemory(false) - before);
        }

        private IEnumerable<AdministrativeBoundaryChunk>
            VisibleAdministrativeChunks()
        {
            if (View == HanNaturalMapView.World)
                return _administrativeTopology.Chunks;
            var margin = _administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning
                ? Math.Max(_administrativeMapViewState.CameraSpanRows,
                    _administrativeMapViewState.CameraSpanColumns) / 2 + 12
                : Math.Max(72, _source.Config.RegionFarSpanCells / 2 + 8);
            var minRow = Math.Max(0, _focusRow - margin);
            var maxRow = Math.Min(_source.Rows - 1, _focusRow + margin);
            var minColumn = Math.Max(0, _focusColumn - margin);
            var maxColumn = Math.Min(_source.Columns - 1,
                _focusColumn + margin);
            var chunkSize = _administrativeTopology.ChunkSize;
            return _administrativeTopology.Chunks.Where(chunk =>
                chunk.ChunkRow * chunkSize <= maxRow &&
                (chunk.ChunkRow + 1) * chunkSize >= minRow &&
                chunk.ChunkColumn * chunkSize <= maxColumn &&
                (chunk.ChunkColumn + 1) * chunkSize >= minColumn);
        }

        private bool IsBoundaryLevelVisible(AdministrativeRegionLevel level)
        {
            if (_administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning)
                return level != AdministrativeRegionLevel.Province;
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.CommanderyEquivalent)
                return level != AdministrativeRegionLevel.County;
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.Province)
                return level == AdministrativeRegionLevel.Province;
            return true;
        }

        private AdministrativeRegionLevel ResolveSelectionLevel()
        {
            if (_administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning)
                return AdministrativeRegionLevel.County;
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.CommanderyEquivalent)
                return AdministrativeRegionLevel.CommanderyEquivalent;
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.County)
                return AdministrativeRegionLevel.County;
            return AdministrativeRegionLevel.Province;
        }

        private bool ShouldIncludeLabel(AdministrativeRegionDefinition region,
            HashSet<string> neighborCounties)
        {
            if (_administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning)
                return region.Level == AdministrativeRegionLevel.County &&
                    (string.Equals(region.Id,
                         _administrativeMapViewState.PlanningCountyId,
                         StringComparison.Ordinal) ||
                     neighborCounties.Contains(region.Id));
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.CommanderyEquivalent)
                return region.Level ==
                    AdministrativeRegionLevel.CommanderyEquivalent;
            if (_administrativeMapViewState.LabelLevel ==
                AdministrativeMapLabelLevel.Province)
                return region.Level == AdministrativeRegionLevel.Province;
            return region.Level == AdministrativeRegionLevel.County;
        }

        private bool IsInsideCurrentRegionWindow(int row, int column)
        {
            var span = _administrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning
                ? Math.Max(_administrativeMapViewState.CameraSpanRows,
                    _administrativeMapViewState.CameraSpanColumns) / 2 + 12
                : 72;
            return Math.Abs(row - _focusRow) <= span &&
                Math.Abs(column - _focusColumn) <= span;
        }

        private HashSet<string> ResolveNeighborCountyIds(string countyId)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(countyId)) return result;
            foreach (var chunk in _administrativeTopology.Chunks)
                foreach (var segment in chunk.Segments)
                {
                    if (!segment.TouchesCounty(countyId)) continue;
                    if (!string.IsNullOrEmpty(segment.First.CountyRegionId) &&
                        !string.Equals(segment.First.CountyRegionId, countyId,
                            StringComparison.Ordinal))
                        result.Add(segment.First.CountyRegionId);
                    if (!string.IsNullOrEmpty(segment.Second.CountyRegionId) &&
                        !string.Equals(segment.Second.CountyRegionId, countyId,
                            StringComparison.Ordinal))
                        result.Add(segment.Second.CountyRegionId);
                }
            return result;
        }

        private void AddBoundaryMesh(string name,
            List<AdministrativeBoundarySegment> segments,
            AdministrativeRegionLevel level, Material material,
            GameObject parent, float widthMultiplier = 1f)
        {
            if (segments.Count == 0 || material == null) return;
            var width = BoundaryWidth(level) * widthMultiplier;
            var vertices = new List<Vector3>(segments.Count * 4);
            var triangles = new List<int>(segments.Count * 6);
            foreach (var segment in segments)
            {
                ResolveSegmentEndpoints(segment, out var start, out var end);
                AddQuad(vertices, triangles, start, end, width);
            }
            var mesh = new Mesh { name = name,
                indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var value = new GameObject(name);
            value.transform.SetParent(parent.transform, false);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void AddSelectedCountyFill()
        {
            var countyId = _administrativeMapViewState.SelectedLevel ==
                AdministrativeRegionLevel.County
                ? _administrativeMapViewState.SelectedAdministrativeRegionId
                : string.Empty;
            if (string.IsNullOrEmpty(countyId) ||
                !_administrativeTopology.RegionSummaries.TryGetValue(countyId,
                    out var summary)) return;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var row = summary.MinRow; row <= summary.MaxRow; row++)
                for (var column = summary.MinColumn;
                     column <= summary.MaxColumn; column++)
                {
                    var assignment = _administrativeSource.ReadAssignment(row,
                        column);
                    if (!string.Equals(assignment.CountyRegionId, countyId,
                            StringComparison.Ordinal)) continue;
                    AddCellQuad(vertices, triangles, row, column);
                }
            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = "Selected County Cell Fill",
                indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            var value = new GameObject("Selected County Cell Fill");
            value.transform.SetParent(_administrativeSelectionRoot.transform,
                false);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial =
                _selectedCountyFillMaterial;
        }

        private void AddCellQuad(List<Vector3> vertices,
            List<int> triangles, int row, int column)
        {
            var x0 = _source.OriginX + column * _source.CellSizeMetres;
            var x1 = x0 + _source.CellSizeMetres;
            var z0 = _source.OriginY - row * _source.CellSizeMetres;
            var z1 = z0 - _source.CellSizeMetres;
            var centerX = (x0 + x1) * 0.5d;
            var centerZ = (z0 + z1) * 0.5d;
            var height = GetPresentationHeightForGlobal(centerX, centerZ) +
                (View == HanNaturalMapView.World ? 0.5f : 0.16f);
            var index = vertices.Count;
            vertices.Add(ToLocal(x0, height, z0));
            vertices.Add(ToLocal(x1, height, z0));
            vertices.Add(ToLocal(x1, height, z1));
            vertices.Add(ToLocal(x0, height, z1));
            triangles.Add(index); triangles.Add(index + 1);
            triangles.Add(index + 2); triangles.Add(index);
            triangles.Add(index + 2); triangles.Add(index + 3);
        }

        private void ResolveSegmentEndpoints(
            AdministrativeBoundarySegment segment, out Vector3 start,
            out Vector3 end)
        {
            var cellSize = _source.CellSizeMetres;
            var x0 = _source.OriginX + segment.Column * cellSize;
            var z0 = _source.OriginY - segment.Row * cellSize;
            double x1;
            double z1;
            double x2;
            double z2;
            if (segment.Direction == GlobalCellEdgeDirection.East)
            {
                x1 = x0 + cellSize; z1 = z0;
                x2 = x1; z2 = z0 - cellSize;
            }
            else
            {
                x1 = x0; z1 = z0 - cellSize;
                x2 = x0 + cellSize; z2 = z1;
            }
            var height = GetPresentationHeightForGlobal((x1 + x2) * 0.5d,
                (z1 + z2) * 0.5d) +
                (View == HanNaturalMapView.World ? 0.7f : 0.22f);
            start = ToLocal(x1, height, z1);
            end = ToLocal(x2, height, z2);
        }

        private Vector3 ToLocal(double x, float height, double z) =>
            new Vector3((float)((x - _floatingOrigin.EastingMetres) /
                HorizontalMetresPerUnit), height,
                (float)((z - _floatingOrigin.NorthingMetres) /
                HorizontalMetresPerUnit));

        private float BoundaryWidth(AdministrativeRegionLevel level)
        {
            if (View == HanNaturalMapView.World)
            {
                if (IsHanStrategicDiorama)
                {
                    var baseWidth = Mathf.Clamp(
                        (_camera?.orthographicSize ?? 330f) * 0.00155f,
                        0.24f, 1.75f);
                    return level == AdministrativeRegionLevel.Province
                        ? baseWidth * 0.72f
                        : level ==
                          AdministrativeRegionLevel.CommanderyEquivalent
                            ? baseWidth * 0.48f
                            : baseWidth * 0.30f;
                }
                return level == AdministrativeRegionLevel.Province ? 2.8f :
                    level == AdministrativeRegionLevel.CommanderyEquivalent
                        ? 1.6f : 0.8f;
            }
            return level == AdministrativeRegionLevel.Province ? 0.18f :
                level == AdministrativeRegionLevel.CommanderyEquivalent
                    ? 0.11f : 0.055f;
        }

        private static void AddQuad(List<Vector3> vertices,
            List<int> triangles, Vector3 start, Vector3 end, float width)
        {
            var direction = end - start;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f) return;
            direction.Normalize();
            var perpendicular = new Vector3(-direction.z, 0f, direction.x) *
                (width * 0.5f);
            var index = vertices.Count;
            vertices.Add(start - perpendicular);
            vertices.Add(start + perpendicular);
            vertices.Add(end + perpendicular);
            vertices.Add(end - perpendicular);
            triangles.Add(index); triangles.Add(index + 1);
            triangles.Add(index + 2); triangles.Add(index);
            triangles.Add(index + 2); triangles.Add(index + 3);
        }

        private AdministrativeSelectionProjection BuildSelectionProjection(
            AdministrativeRegionDefinition region)
        {
            var summary = _administrativeTopology.GetRegion(region.Id);
            var result = new AdministrativeSelectionProjection
            {
                RegionId = region.Id,
                DisplayName = region.DisplayName,
                Level = region.Level,
                RegionType = region.RegionType,
                GeometryStatus = region.GeometryStatus,
                SourceGeometryStatus = region.SourceGeometryStatus,
                Confidence = region.Confidence,
                Provisional = region.Provisional,
                CellCount = summary.CellCount,
                BoundarySegmentCount = CountBoundarySegments(region)
            };
            if (region.Level == AdministrativeRegionLevel.County)
            {
                _administrativeSource.RegionCatalog.ResolveCountyHierarchy(
                    region.Id, out _, out var commandery, out var province);
                result.ParentCommanderyId = commandery.Id;
                result.ParentCommanderyName = commandery.DisplayName;
                result.ParentProvinceId = province.Id;
                result.ParentProvinceName = province.DisplayName;
                AddPublicCountyContext(result, summary);
            }
            return result;
        }

        private int CountBoundarySegments(
            AdministrativeRegionDefinition region)
        {
            var count = 0;
            foreach (var chunk in _administrativeTopology.Chunks)
                foreach (var segment in chunk.Segments)
                    if (SegmentTouchesRegion(segment, region.Id,
                            region.Level)) count++;
            return count;
        }

        private void AddPublicCountyContext(
            AdministrativeSelectionProjection result,
            AdministrativeRegionSpatialSummary summary)
        {
            for (var row = summary.MinRow; row <= summary.MaxRow; row++)
                for (var column = summary.MinColumn;
                     column <= summary.MaxColumn; column++)
                {
                    var assignment = _administrativeSource.ReadAssignment(row,
                        column);
                    if (!string.Equals(assignment.CountyRegionId,
                            result.RegionId, StringComparison.Ordinal)) continue;
                    if (_administrativeSource.ReadRoadClass(row, column) > 0)
                        result.PublicRoadCellCount++;
                }
            foreach (var feature in _administrativeSource.Cities.Features ??
                     new List<WorldMapLocationFeature>())
            {
                var properties = feature?.Properties;
                if (properties == null || !properties.Row.HasValue ||
                    !properties.Column.HasValue) continue;
                var assignment = _administrativeSource.ReadAssignment(
                    properties.Row.Value, properties.Column.Value);
                if (string.Equals(assignment.CountyRegionId, result.RegionId,
                        StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(properties.DisplayName))
                    result.PublicMajorSettlements.Add(properties.DisplayName);
            }
            result.PublicMajorSettlements.Sort(StringComparer.Ordinal);
        }

        private static bool SegmentTouchesRegion(
            AdministrativeBoundarySegment segment, string regionId,
            AdministrativeRegionLevel? level)
        {
            if (!level.HasValue || string.IsNullOrEmpty(regionId)) return false;
            if (level == AdministrativeRegionLevel.Province)
                return string.Equals(segment.First.ProvinceRegionId, regionId,
                           StringComparison.Ordinal) ||
                       string.Equals(segment.Second.ProvinceRegionId, regionId,
                           StringComparison.Ordinal);
            if (level == AdministrativeRegionLevel.CommanderyEquivalent)
                return string.Equals(segment.First.CommanderyRegionId, regionId,
                           StringComparison.Ordinal) ||
                       string.Equals(segment.Second.CommanderyRegionId, regionId,
                           StringComparison.Ordinal);
            return segment.TouchesCounty(regionId);
        }

        private static Material CreateAdministrativeMaterial(string name,
            Color color)
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Diffuse");
            return new Material(shader) { name = name, color = color };
        }

        private void RefreshStrategicDioramaSettlements()
        {
            ClearStrategicDioramaSettlements();
            if (!IsHanStrategicDiorama || !_administrativeOverlayVisible ||
                _strategicDioramaSettlementRoot == null ||
                _strategicDioramaSettlementMaterial == null) return;
            var markers = GetVisibleSettlementMarkers();
            if (markers.Count == 0) return;
            var vertices = new List<Vector3>(markers.Count * 180);
            var triangles = new List<int>(markers.Count * 270);
            var colours = new List<Color>(markers.Count * 180);
            var radius = View == HanNaturalMapView.World
                ? Mathf.Clamp((_camera?.orthographicSize ?? 330f) * 0.016f,
                    3f, 13f)
                : 0.85f;
            foreach (var marker in markers)
            {
                var center = GlobalCellCenterToLocal(marker.Row,
                    marker.Column);
                var cell = _source.ReadSample(marker.Row,
                    marker.Column).Cell;
                center.y = GetPresentationHeightForGlobal(cell.CenterX,
                    cell.CenterY) + 0.10f;
                AddStrategicSettlementGeometry(vertices, triangles, colours,
                    center, radius);
            }
            var mesh = new Mesh
            {
                name = "Han Strategic Diorama Settlement Combined Mesh",
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colours);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var batch = new GameObject(
                "Han Strategic Diorama Settlements Combined Mesh");
            batch.transform.SetParent(
                _strategicDioramaSettlementRoot.transform, false);
            batch.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = batch.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _strategicDioramaSettlementMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            _strategicDioramaSettlementCount = markers.Count;
        }

        private void ClearStrategicDioramaSettlements()
        {
            _strategicDioramaSettlementCount = 0;
            if (_strategicDioramaSettlementRoot == null) return;
            ClearAdministrativeMeshes(
                _strategicDioramaSettlementRoot.transform);
        }

        private static void AddStrategicSettlementGeometry(
            List<Vector3> vertices, List<int> triangles,
            List<Color> colours, Vector3 center, float radius)
        {
            var sandstone = new Color(0.78f, 0.58f, 0.29f, 1f);
            var darkStone = new Color(0.38f, 0.27f, 0.15f, 1f);
            var roof = new Color(0.63f, 0.17f, 0.09f, 1f);
            var wallHeight = radius * 0.48f;
            var wallThickness = radius * 0.18f;
            AddDioramaBox(vertices, triangles, colours,
                center + new Vector3(0f, wallHeight * 0.5f, -radius),
                new Vector3(radius * 2f, wallHeight, wallThickness),
                sandstone);
            AddDioramaBox(vertices, triangles, colours,
                center + new Vector3(0f, wallHeight * 0.5f, radius),
                new Vector3(radius * 2f, wallHeight, wallThickness),
                sandstone);
            AddDioramaBox(vertices, triangles, colours,
                center + new Vector3(-radius, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, radius * 2f),
                sandstone);
            AddDioramaBox(vertices, triangles, colours,
                center + new Vector3(radius, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, radius * 2f),
                sandstone);
            var keepSize = radius * 0.78f;
            var keepHeight = radius * 0.92f;
            AddDioramaBox(vertices, triangles, colours,
                center + new Vector3(0f, keepHeight * 0.5f, 0f),
                new Vector3(keepSize, keepHeight, keepSize), darkStone);
            AddDioramaPyramid(vertices, triangles, colours,
                center + new Vector3(0f, keepHeight, 0f),
                keepSize * 0.72f, radius * 0.52f, roof);
        }

        private static void AddDioramaBox(List<Vector3> vertices,
            List<int> triangles, List<Color> colours, Vector3 center,
            Vector3 size, Color colour)
        {
            var half = size * 0.5f;
            var corners = new[]
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3( half.x, -half.y, -half.z),
                center + new Vector3( half.x, -half.y,  half.z),
                center + new Vector3(-half.x, -half.y,  half.z),
                center + new Vector3(-half.x,  half.y, -half.z),
                center + new Vector3( half.x,  half.y, -half.z),
                center + new Vector3( half.x,  half.y,  half.z),
                center + new Vector3(-half.x,  half.y,  half.z)
            };
            var faces = new[]
            {
                0, 1, 2, 3, 4, 7, 6, 5, 0, 4, 5, 1,
                1, 5, 6, 2, 2, 6, 7, 3, 3, 7, 4, 0
            };
            for (var face = 0; face < 6; face++)
            {
                var index = vertices.Count;
                for (var corner = 0; corner < 4; corner++)
                {
                    vertices.Add(corners[faces[face * 4 + corner]]);
                    colours.Add(colour);
                }
                triangles.Add(index); triangles.Add(index + 1);
                triangles.Add(index + 2); triangles.Add(index);
                triangles.Add(index + 2); triangles.Add(index + 3);
            }
        }

        private static void AddDioramaPyramid(List<Vector3> vertices,
            List<int> triangles, List<Color> colours, Vector3 baseCenter,
            float halfSize, float height, Color colour)
        {
            var baseIndex = vertices.Count;
            vertices.Add(baseCenter + new Vector3(-halfSize, 0f, -halfSize));
            vertices.Add(baseCenter + new Vector3( halfSize, 0f, -halfSize));
            vertices.Add(baseCenter + new Vector3( halfSize, 0f,  halfSize));
            vertices.Add(baseCenter + new Vector3(-halfSize, 0f,  halfSize));
            vertices.Add(baseCenter + new Vector3(0f, height, 0f));
            for (var index = 0; index < 5; index++) colours.Add(colour);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 4);
            triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 4); triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 4);
            triangles.Add(baseIndex + 3); triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 4); triangles.Add(baseIndex);
        }

        private static void ClearAdministrativeMeshes(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                var filter = child.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private void DisposeAdministrativeGeography()
        {
            _administrativeSource?.Dispose();
            _administrativeSource = null;
            _administrativeTopology = null;
            foreach (var material in new[] { _provinceBoundaryMaterial,
                         _commanderyBoundaryMaterial, _countyBoundaryMaterial,
                         _selectedBoundaryMaterial, _selectedCountyFillMaterial,
                         _strategicDioramaSettlementMaterial })
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
            _provinceBoundaryMaterial = null;
            _commanderyBoundaryMaterial = null;
            _countyBoundaryMaterial = null;
            _selectedBoundaryMaterial = null;
            _selectedCountyFillMaterial = null;
            _strategicDioramaSettlementMaterial = null;
            _strategicDioramaSettlementRoot = null;
        }
    }
}
