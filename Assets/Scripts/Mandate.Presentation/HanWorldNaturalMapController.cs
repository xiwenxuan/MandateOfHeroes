using System;
using System.Diagnostics;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public enum HanNaturalMapView
    {
        World,
        Region
    }

    public enum StrategicCellGridLod
    {
        Off,
        NationwideGuide32,
        ExactCell
    }

    public sealed partial class HanWorldNaturalMapController : MonoBehaviour
    {
        private const double HorizontalMetresPerUnit = 2000d;
        private const double VerticalMetresPerUnit = 250d;
        private HanWorldNaturalMapSource _source;
        private HanWorldTerrainGenerator _terrainGenerator;
        private WorldTerrainLodController _lodController;
        private TerrainSurfaceBlendController _surfaceBlend;
        private TerrainCellBinding _cellBinding;
        private TerrainTileIndex _tileIndex;
        private GameObject _terrainRoot;
        private GameObject _riverRoot;
        private GameObject _vegetationRoot;
        private GameObject _gridRoot;
        private GameObject _lightingRoot;
        private Camera _camera;
        private Material _terrainMaterial;
        private Material _riverMaterial;
        private Material _vegetationMaterial;
        private Material _strategicCellMaterial;
        private Light _sun;
        private HanWorldArtProfile _activeArtProfile =
            HanWorldArtProfileCatalog.Get(HanWorldArtStyle.ChineseSemiRealistic);
        private GlobalProjectedCoordinate _floatingOrigin;
        private int _focusRow;
        private int _focusColumn;
        private double _lastTerrainGenerationMilliseconds;
        private double _lastTransitionMilliseconds;
        private long _lastManagedGcDeltaBytes;
        private long _residentTerrainMeshBytes;
        private NaturalTerrainLodSet _worldLodCache;
        private long _lastFusionFeatureVertexCount;
        private long _lastVisualTerrainVertexCount;
        private VisualTerrainDetailLevel _visualDetailLevel = VisualTerrainDetailLevel.World;
        private RiverMeshDiagnostics _lastRiverDiagnostics = new RiverMeshDiagnostics();
        private WorldMapCellId? _hoveredCellId;
        private WorldMapCellId? _selectedCellId;
        private int _visibleStrategicCellCount;
        private ulong _strategicCellCoverageCount;
        private int _strategicGridStepCells;
        private int _p0ReviewPieceIndex;
        private int _p0ReviewAngleIndex;
        private int _p0LandmarkSecondBatchReviewPieceIndex;
        private int _p0LandmarkSecondBatchReviewAngleIndex;
        private int _p0NamedGateFourthBatchReviewPieceIndex;
        private int _p0NamedGateFourthBatchReviewAngleIndex;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public HanNaturalMapView View { get; private set; }
        public bool CellOverlayVisible { get; private set; }
        public bool PresentationUiVisible { get; private set; } = true;
        public bool UsesLegacyBackground => false;
        public bool UsesAdministrativeOverlay => false;
        public int RuntimeTerrainObjectCount => _terrainRoot == null ? 0 : _terrainRoot.transform.childCount;
        public int RuntimeVegetationObjectCount => _vegetationRoot == null ? 0 : _vegetationRoot.transform.childCount;
        public int RuntimeRiverMeshCount => _riverRoot == null ? 0 : _riverRoot.transform.childCount;
        public int RuntimeCellOverlayObjectCount => _gridRoot == null ? 0 : _gridRoot.transform.childCount;
        public int IndexedTerrainTileCount => _tileIndex?.TileCount ?? 0;
        public GlobalProjectedCoordinate FloatingOrigin => _floatingOrigin;
        public TerrainTileIndex TileIndex => _tileIndex;
        public HanWorldArtStyle ActiveArtStyle => _activeArtProfile.Style;
        public string ActiveArtProfileId => _activeArtProfile.ProfileId;
        public string ProductionStatus => !IsReady
            ? "NOT_READY"
            : LuoyangP0NamedGateFourthBatchPreviewVisible
                ? LuoyangP0NamedGateFourthBatchIds.StatusId
            : LuoyangP0LandmarkThirdBatchPreviewVisible
                ? LuoyangP0LandmarkThirdBatchIds.StatusId
            : LuoyangP0LandmarkSecondBatchPreviewVisible
                ? LuoyangP0LandmarkSecondBatchIds.StatusId
            : LuoyangP0FinalAssetVerticalSlicePreviewVisible
                ? "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1"
            : LuoyangFinalAssetReviewPreviewVisible
                ? "LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1_READY_FOR_USER_REVIEW"
            : LuoyangFinalCivicPreviewVisible
                ? "LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1_READY_FOR_USER_REVIEW"
            : LuoyangResourceAgriculturePreviewVisible
                ? "LUOYANG_RESOURCE_AGRICULTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"
            : LuoyangLowFrequencyDefensePreviewVisible
                ? "LUOYANG_LOW_FREQUENCY_DEFENSE_PRODUCTION_V1_READY_FOR_USER_REVIEW"
            : LuoyangInfrastructurePreviewVisible
                ? "LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1_READY_FOR_USER_REVIEW"
            : LuoyangBuildingPerformancePreviewVisible
                ? LuoyangRoadConnectorPassageTraversalIds.StatusId
            : LuoyangMediumFrequencyUrbanFabricPreviewVisible
                ? "LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1_READY_FOR_USER_REVIEW"
            : LuoyangGateIdentityPreviewVisible
                ? "LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1_READY_FOR_USER_REVIEW"
            : LuoyangHistoricalLandmarkPreviewVisible
                ? "LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1_READY_FOR_USER_REVIEW"
            : LuoyangFacilityCoveragePreviewVisible
                ? "LUOYANG_FACILITY_MODEL_COVERAGE_V1_READY_FOR_USER_REVIEW"
            : BuildableFacilityPreviewVisible
                ? "LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1_READY_FOR_USER_REVIEW"
            : CellOverlayVisible && StrategicGridLod == StrategicCellGridLod.NationwideGuide32
                ? "HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1_READY_FOR_USER_REVIEW"
                : CellOverlayVisible
                ? "HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1_READY_FOR_USER_REVIEW"
                : ActiveArtStyle == HanWorldArtStyle.ZhonghuaSanguozhiFusion
                    ? "STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW"
                    : "HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY";
        public long LastFusionFeatureVertexCount => _lastFusionFeatureVertexCount;
        public long LastVisualTerrainVertexCount => _lastVisualTerrainVertexCount;
        public VisualTerrainDetailLevel VisualDetailLevel => _visualDetailLevel;
        public RiverMeshDiagnostics LastRiverDiagnostics => _lastRiverDiagnostics;
        public string StrategicCellContractId => ExplicitStrategicCellMapV1.ContractId;
        public string NationwideStrategicCellContractId =>
            ExplicitStrategicCellMapV1.NationwideContractId;
        public int VisibleStrategicCellCount => _visibleStrategicCellCount;
        public WorldMapCellId? HoveredCellId => _hoveredCellId;
        public WorldMapCellId? SelectedCellId => _selectedCellId;
        public ulong StrategicCellCoverageCount => _strategicCellCoverageCount;
        public int StrategicGridStepCells => _strategicGridStepCells;
        public StrategicCellGridLod StrategicGridLod { get; private set; }
        public string P0MultiAngleReviewContractId =>
            LuoyangP0MultiAngleReviewRig.ContractId;
        public int P0ReviewPieceIndex => _p0ReviewPieceIndex;
        public int P0ReviewAngleIndex => _p0ReviewAngleIndex;
        public string ActiveP0ReviewCameraId =>
            LuoyangP0MultiAngleReviewRig.GetCameraId(_p0ReviewPieceIndex,
                _p0ReviewAngleIndex);
        public string ActiveP0ReviewPieceLabel =>
            LuoyangP0MultiAngleReviewRig.GetPieceLabel(_p0ReviewPieceIndex);
        public string ActiveP0ReviewAngleLabel =>
            LuoyangP0MultiAngleReviewRig.GetAngleLabel(_p0ReviewAngleIndex);
        public string P0LandmarkSecondBatchMultiAngleReviewContractId =>
            LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.ContractId;
        public int P0LandmarkSecondBatchReviewPieceIndex =>
            _p0LandmarkSecondBatchReviewPieceIndex;
        public int P0LandmarkSecondBatchReviewAngleIndex =>
            _p0LandmarkSecondBatchReviewAngleIndex;
        public string ActiveP0LandmarkSecondBatchReviewCameraId =>
            LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetCameraId(
                _p0LandmarkSecondBatchReviewPieceIndex,
                _p0LandmarkSecondBatchReviewAngleIndex);
        public string ActiveP0LandmarkSecondBatchReviewPieceLabel =>
            LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetPieceLabel(
                _p0LandmarkSecondBatchReviewPieceIndex);
        public string ActiveP0LandmarkSecondBatchReviewAngleLabel =>
            LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetAngleLabel(
                _p0LandmarkSecondBatchReviewAngleIndex);
        public string P0NamedGateFourthBatchMultiAngleReviewContractId =>
            LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.ContractId;
        public int P0NamedGateFourthBatchReviewPieceIndex =>
            _p0NamedGateFourthBatchReviewPieceIndex;
        public int P0NamedGateFourthBatchReviewAngleIndex =>
            _p0NamedGateFourthBatchReviewAngleIndex;
        public string ActiveP0NamedGateFourthBatchReviewCameraId =>
            LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.GetCameraId(
                _p0NamedGateFourthBatchReviewPieceIndex,
                _p0NamedGateFourthBatchReviewAngleIndex);
        public string ActiveP0NamedGateFourthBatchReviewPieceLabel =>
            LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.GetPieceLabel(
                _p0NamedGateFourthBatchReviewPieceIndex);
        public string ActiveP0NamedGateFourthBatchReviewAngleLabel =>
            LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.GetAngleLabel(
                _p0NamedGateFourthBatchReviewAngleIndex);

        private void Start() => TryInitialize();

        private void Update()
        {
            if (!IsReady || !CellOverlayVisible || View != HanNaturalMapView.Region ||
                _camera == null) return;
            var uiGuardHeight = _luoyangP0FinalAssetVerticalSlicePreviewVisible ||
                                _luoyangP0LandmarkSecondBatchPreviewVisible ||
                                _luoyangP0NamedGateFourthBatchPreviewVisible
                ? 158f
                : 108f;
            if (PresentationUiVisible && Input.mousePosition.y >=
                Screen.height - uiGuardHeight) return;
            WorldMapCellId? hovered = null;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, _camera.farClipPlane) &&
                TryPickGlobalCell(hit.point, out var picked)) hovered = picked;
            if (Input.GetMouseButtonDown(0) &&
                _luoyangBuildingPerformancePreviewVisible)
                TrySelectLuoyangFacility(ray);
            var select = Input.GetMouseButtonDown(0) && hovered.HasValue;
            if (Nullable.Equals(hovered, _hoveredCellId) && !select) return;
            _hoveredCellId = hovered;
            if (select) _selectedCellId = hovered;
            RefreshStrategicCellGrid();
        }

        public bool TryInitialize(string worldRoot = null, string naturalRoot = null)
        {
            if (IsReady) return true;
            try
            {
                worldRoot ??= Path.Combine(Application.streamingAssetsPath, "WorldMap", "HanWorldV1");
                naturalRoot ??= Path.Combine(Application.streamingAssetsPath, "WorldMap", "NaturalBasemapV1");
                _source = new HanWorldNaturalMapSource(worldRoot, naturalRoot);
                if (_source.Rows != GlobalSpatialFoundationV1.Rows ||
                    _source.Columns != GlobalSpatialFoundationV1.Columns ||
                    Math.Abs(_source.OriginX - GlobalSpatialFoundationV1.OriginX) > 0.001d ||
                    Math.Abs(_source.OriginY - GlobalSpatialFoundationV1.OriginY) > 0.001d)
                    throw new InvalidDataException("Natural basemap source does not match frozen Global Cell grid.");
                _terrainGenerator = new HanWorldTerrainGenerator(_source);
                var grid = GlobalSpatialFoundationV1.CreateCellGrid();
                _cellBinding = new TerrainCellBinding(grid);
                _tileIndex = new TerrainTileIndex(grid, _source.Config.TerrainTileCellsPerSide);
                _lodController = new WorldTerrainLodController(_source, _terrainGenerator, _tileIndex);
                _surfaceBlend = new TerrainSurfaceBlendController();
                _focusRow = 1241;
                _focusColumn = 2043;
                EnsureSceneObjects();
                ApplyArtProfileParameters();
                SetWorldView();
                IsReady = true;
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                UnityEngine.Debug.LogError("Han natural map V2 initialization failed: " + LastError);
                return false;
            }
        }

        public void SetWorldView()
        {
            _buildableFacilityPreviewVisible = false;
            _luoyangFacilityCoveragePreviewVisible = false;
            _luoyangHistoricalLandmarkPreviewVisible = false;
            _luoyangGateIdentityPreviewVisible = false;
            _luoyangMediumFrequencyUrbanFabricPreviewVisible = false;
            _luoyangBuildingPerformancePreviewVisible = false;
            _luoyangInfrastructurePreviewVisible = false;
            _luoyangLowFrequencyDefensePreviewVisible = false;
            _luoyangResourceAgriculturePreviewVisible = false;
            _luoyangFinalCivicPreviewVisible = false;
            _luoyangFinalAssetReviewPreviewVisible = false;
            _luoyangP0FinalAssetVerticalSlicePreviewVisible = false;
            _luoyangP0LandmarkSecondBatchPreviewVisible = false;
            _luoyangP0LandmarkThirdBatchPreviewVisible = false;
            _luoyangP0NamedGateFourthBatchPreviewVisible = false;
            View = HanNaturalMapView.World;
            _visualDetailLevel = VisualTerrainDetailLevel.World;
            ApplyArtProfileParameters();
            var center = new GlobalProjectedCoordinate(
                (GlobalSpatialFoundationV1.GlobalMinX + GlobalSpatialFoundationV1.GlobalMaxX) * 0.5d,
                (GlobalSpatialFoundationV1.GlobalMinY + GlobalSpatialFoundationV1.GlobalMaxY) * 0.5d);
            _floatingOrigin = center;
            var before = GC.GetTotalMemory(false);
            var lod = _worldLodCache ?? (_worldLodCache = _lodController.GenerateWorld(
                Math.Max(4, _source.Config.WorldLodSampleStepCells),
                _activeArtProfile.WorldVerticalExaggeration));
            ReplaceTerrain(lod, false);
            ReplaceRivers(null);
            ReplaceVegetation(null);
            if (CellOverlayVisible) ReplaceNationwideGrid();
            else ReplaceGrid(null);
            RefreshBuildableFacilityPreview();
            _lastTerrainGenerationMilliseconds = lod.GenerationMilliseconds;
            _lastManagedGcDeltaBytes = Math.Max(0L, GC.GetTotalMemory(false) - before);
            var preset = VisualAcceptanceCameraRig.Get(VisualAcceptanceCameraRig.WorldFull);
            ConfigureCamera(Vector3.zero, preset.Size, preset.Pitch, preset.Yaw);
            ConfigureAtmosphere(true);
        }

        public void SetRegionView(int centerRow, int centerColumn) =>
            SetRegionView(centerRow, centerColumn, VisualTerrainDetailLevel.Region);

        private void SetRegionView(int centerRow, int centerColumn,
            VisualTerrainDetailLevel detailLevel)
        {
            if (centerRow < 0 || centerRow >= _source.Rows || centerColumn < 0 || centerColumn >= _source.Columns)
                throw new ArgumentOutOfRangeException(nameof(centerRow));
            View = HanNaturalMapView.Region;
            _visualDetailLevel = detailLevel < VisualTerrainDetailLevel.Region
                ? VisualTerrainDetailLevel.Region : detailLevel;
            ApplyArtProfileParameters();
            _focusRow = centerRow;
            _focusColumn = centerColumn;
            var center = _source.ReadSample(centerRow, centerColumn).Cell;
            _floatingOrigin = new GlobalProjectedCoordinate(center.CenterX, center.CenterY);
            var before = GC.GetTotalMemory(false);
            var residentRadius = _visualDetailLevel >= VisualTerrainDetailLevel.City
                ? Math.Max(2, _source.Config.RegionResidentTileRadius)
                : Math.Max(1, _source.Config.RegionResidentTileRadius);
            var lod = _lodController.GenerateRegion(centerRow, centerColumn,
                residentRadius,
                Math.Max(72, _source.Config.RegionFarSpanCells),
                Math.Max(1, _source.Config.RegionFarSampleStepCells),
                _activeArtProfile.RegionVerticalExaggeration);
            ReplaceTerrain(lod, true);
            var halfSpan = Math.Max(72, _source.Config.RegionFarSpanCells) *
                           _source.CellSizeMetres * 0.55d;
            ReplaceRivers((x, y) => Math.Abs(x - _floatingOrigin.EastingMetres) <= halfSpan &&
                                    Math.Abs(y - _floatingOrigin.NorthingMetres) <= halfSpan);
            var vegetationSpan = _visualDetailLevel >= VisualTerrainDetailLevel.City
                ? 32 : Math.Max(96, _source.Config.RegionFarSpanCells);
            ReplaceVegetation(new RegionWindow(Math.Max(0, centerRow - vegetationSpan / 2),
                Math.Max(0, centerColumn - vegetationSpan / 2), vegetationSpan, vegetationSpan));
            ReplaceGrid(CellOverlayVisible
                ? new RegionWindow(centerRow - 12, centerColumn - 12, 24, 24)
                : (RegionWindow?)null);
            RefreshBuildableFacilityPreview();
            _lastTerrainGenerationMilliseconds = lod.GenerationMilliseconds;
            _lastManagedGcDeltaBytes = Math.Max(0L, GC.GetTotalMemory(false) - before);
            ConfigureCamera(Vector3.zero, 31f, 58f, -12f);
            ConfigureAtmosphere(false);
        }

        public void LocateLuoyang() => SetRegionView(1241, 2043);
        public void LocateNorthChinaPlain() => SetRegionView(1110, 2090);
        public void LocateMountainRegion() => SetRegionView(1390, 1710);
        public void LocateMajorRiverRegion() => SetRegionView(1209, 2148);
        public void LocateForestRegion() => SetRegionView(1460, 1970);
        public void LocateHenanYin() => SetRegionView(1247, 1992);

        public void SetArtStyle(HanWorldArtStyle style)
        {
            if (_activeArtProfile.Style == style && IsReady) return;
            _activeArtProfile = HanWorldArtProfileCatalog.Get(style);
            _worldLodCache = null;
            ApplyArtProfileParameters();
            if (!IsReady) return;
            if (View == HanNaturalMapView.World) SetWorldView();
            else SetRegionView(_focusRow, _focusColumn, _visualDetailLevel);
        }

        public void ApplyArtSampleCamera(ArtDirectionSample sample, HanNaturalMapView view)
        {
            var preset = HanWorldArtDirectionCameraRig.Get(sample, view);
            if (view == HanNaturalMapView.World)
            {
                SetWorldView();
                ConfigureCamera(GlobalCellCenterToLocal(preset.Row, preset.Column), preset.Size,
                    preset.Pitch, preset.Yaw);
            }
            else
            {
                SetRegionView(preset.Row, preset.Column);
                ConfigureCamera(Vector3.zero, preset.Size, preset.Pitch, preset.Yaw);
            }
        }

        public void ApplyZhonghuaFusionCamera(string presetId)
        {
            var preset = ZhonghuaFusionCameraRig.Get(presetId);
            if (ZhonghuaFusionCameraRig.IsWorldView(presetId))
            {
                SetWorldView();
                var target = presetId == ZhonghuaFusionCameraRig.World
                    ? Vector3.zero
                    : GlobalCellCenterToLocal(preset.Row, preset.Column);
                ConfigureCamera(target, preset.Size, preset.Pitch, preset.Yaw);
                return;
            }
            SetRegionView(preset.Row, preset.Column, ZhonghuaFusionCameraRig.DetailLevelFor(presetId));
            ConfigureCamera(Vector3.zero, preset.Size, preset.Pitch, preset.Yaw);
        }

        public void SetStyleDWorldToCityTransition(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            var stopwatch = Stopwatch.StartNew();
            if (normalized < 0.62f)
            {
                SetWorldView();
                var target = GlobalCellCenterToLocal(1247, 1992);
                var progress = Mathf.SmoothStep(0f, 1f, normalized / 0.62f);
                ConfigureCamera(Vector3.Lerp(Vector3.zero, target, progress),
                    Mathf.Lerp(1160f, 96f, progress), Mathf.Lerp(68f, 61f, progress),
                    Mathf.Lerp(0f, -10f, progress));
            }
            else
            {
                var detail = normalized > 0.87f
                    ? VisualTerrainDetailLevel.City : VisualTerrainDetailLevel.Region;
                SetRegionView(1247, 1992, detail);
                var progress = (normalized - 0.62f) / 0.38f;
                ConfigureCamera(Vector3.zero, Mathf.Lerp(72f, 12f, progress),
                    Mathf.Lerp(61f, 54f, progress), -12f);
            }
            stopwatch.Stop();
            _lastTransitionMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        }

        public void ApplyCameraPreset(string presetId)
        {
            var preset = VisualAcceptanceCameraRig.Get(presetId);
            if (presetId == VisualAcceptanceCameraRig.WorldFull ||
                presetId == VisualAcceptanceCameraRig.WorldNorthChina)
            {
                SetWorldView();
                var target = presetId == VisualAcceptanceCameraRig.WorldFull
                    ? Vector3.zero
                    : GlobalCellCenterToLocal(preset.Row, preset.Column);
                ConfigureCamera(target, preset.Size, preset.Pitch, preset.Yaw);
                return;
            }
            SetRegionView(preset.Row, preset.Column);
            ConfigureCamera(Vector3.zero, preset.Size, preset.Pitch, preset.Yaw);
        }

        public void SetHenanYinTransition(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            var stopwatch = Stopwatch.StartNew();
            if (normalized < 0.76f)
            {
                SetWorldView();
                var target = GlobalCellCenterToLocal(1247, 1992);
                var progress = Mathf.SmoothStep(0f, 1f, normalized / 0.76f);
                ConfigureCamera(Vector3.Lerp(Vector3.zero, target, progress),
                    Mathf.Lerp(1160f, 92f, progress), Mathf.Lerp(68f, 59f, progress),
                    Mathf.Lerp(0f, -10f, progress));
            }
            else
            {
                SetRegionView(1247, 1992);
                var progress = (normalized - 0.76f) / 0.24f;
                ConfigureCamera(Vector3.zero, Mathf.Lerp(62f, 31f, progress),
                    Mathf.Lerp(61f, 58f, progress), -12f);
            }
            stopwatch.Stop();
            _lastTransitionMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        }

        public void SetCellOverlayVisible(bool visible)
        {
            CellOverlayVisible = visible;
            if (View == HanNaturalMapView.Region) SetRegionView(_focusRow, _focusColumn, _visualDetailLevel);
            else if (visible) ReplaceNationwideGrid();
            else ReplaceGrid(null);
        }

        public void SetStrategicCellInteraction(WorldMapCellId? hoveredCell,
            WorldMapCellId? selectedCell)
        {
            ValidateCellId(hoveredCell, nameof(hoveredCell));
            ValidateCellId(selectedCell, nameof(selectedCell));
            _hoveredCellId = hoveredCell;
            _selectedCellId = selectedCell;
            RefreshStrategicCellGrid();
        }

        public void ApplyStrategicCellCamera(string presetId)
        {
            var preset = StrategicCellCameraRig.Get(presetId);
            if (LuoyangP0MultiAngleReviewRig.TryGetIndexes(presetId,
                    out var reviewPieceIndex, out var reviewAngleIndex))
            {
                _p0ReviewPieceIndex = reviewPieceIndex;
                _p0ReviewAngleIndex = reviewAngleIndex;
            }
            if (LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.TryGetIndexes(
                    presetId, out var batchPieceIndex, out var batchAngleIndex))
            {
                _p0LandmarkSecondBatchReviewPieceIndex = batchPieceIndex;
                _p0LandmarkSecondBatchReviewAngleIndex = batchAngleIndex;
            }
            if (LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.TryGetIndexes(
                    presetId, out var gatePieceIndex, out var gateAngleIndex))
            {
                _p0NamedGateFourthBatchReviewPieceIndex = gatePieceIndex;
                _p0NamedGateFourthBatchReviewAngleIndex = gateAngleIndex;
            }
            var infrastructureReview =
                StrategicCellCameraRig.IsLuoyangInfrastructureReview(presetId);
            var defenseReview =
                StrategicCellCameraRig.IsLuoyangLowFrequencyDefenseReview(
                    presetId);
            var resourceAgricultureReview =
                StrategicCellCameraRig.IsLuoyangResourceAgricultureReview(
                    presetId);
            var finalCivicReview =
                StrategicCellCameraRig.IsLuoyangFinalCivicReview(presetId);
            var finalAssetReview =
                StrategicCellCameraRig.IsLuoyangFinalAssetReview(presetId);
            var p0FinalAssetReview =
                StrategicCellCameraRig.IsLuoyangP0FinalAssetVerticalSlice(
                    presetId);
            var p0LandmarkSecondBatchReview =
                StrategicCellCameraRig.IsLuoyangP0LandmarkSecondBatch(
                    presetId);
            var p0LandmarkThirdBatchReview =
                StrategicCellCameraRig.IsLuoyangP0LandmarkThirdBatch(
                    presetId);
            var p0NamedGateFourthBatchReview =
                StrategicCellCameraRig.IsLuoyangP0NamedGateFourthBatch(
                    presetId);
            _buildableFacilityPreviewVisible =
                presetId == StrategicCellCameraRig.BuildableFacilityReview ||
                presetId == StrategicCellCameraRig.LuoyangFacilityCoverageReview ||
                presetId == StrategicCellCameraRig.LuoyangHistoricalLandmarkReview ||
                presetId == StrategicCellCameraRig.LuoyangGateIdentityReview ||
                presetId == StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview ||
                presetId == StrategicCellCameraRig.LuoyangBuildingPerformanceReview ||
                infrastructureReview || defenseReview ||
                resourceAgricultureReview || finalCivicReview ||
                finalAssetReview || p0FinalAssetReview ||
                p0LandmarkSecondBatchReview || p0LandmarkThirdBatchReview ||
                p0NamedGateFourthBatchReview;
            _luoyangFacilityCoveragePreviewVisible =
                presetId == StrategicCellCameraRig.LuoyangFacilityCoverageReview;
            _luoyangHistoricalLandmarkPreviewVisible =
                presetId == StrategicCellCameraRig.LuoyangHistoricalLandmarkReview;
            _luoyangGateIdentityPreviewVisible =
                presetId == StrategicCellCameraRig.LuoyangGateIdentityReview;
            _luoyangMediumFrequencyUrbanFabricPreviewVisible =
                presetId == StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview;
            _luoyangBuildingPerformancePreviewVisible =
                presetId == StrategicCellCameraRig.LuoyangBuildingPerformanceReview;
            _luoyangInfrastructurePreviewVisible = infrastructureReview;
            _luoyangLowFrequencyDefensePreviewVisible = defenseReview;
            _luoyangResourceAgriculturePreviewVisible =
                resourceAgricultureReview;
            _luoyangFinalCivicPreviewVisible = finalCivicReview;
            _luoyangFinalAssetReviewPreviewVisible = finalAssetReview;
            _luoyangP0FinalAssetVerticalSlicePreviewVisible =
                p0FinalAssetReview;
            _luoyangP0LandmarkSecondBatchPreviewVisible =
                p0LandmarkSecondBatchReview;
            _luoyangP0LandmarkThirdBatchPreviewVisible =
                p0LandmarkThirdBatchReview;
            _luoyangP0NamedGateFourthBatchPreviewVisible =
                p0NamedGateFourthBatchReview;
            if (_activeArtProfile.Style != HanWorldArtStyle.ZhonghuaSanguozhiFusion)
            {
                _activeArtProfile = HanWorldArtProfileCatalog.Get(
                    HanWorldArtStyle.ZhonghuaSanguozhiFusion);
                _worldLodCache = null;
            }
            CellOverlayVisible = true;
            if (preset.IsWorldView)
            {
                _selectedCellId = null;
                _hoveredCellId = null;
                SetWorldView();
                ConfigureCamera(Vector3.zero, preset.Size, preset.Pitch, preset.Yaw);
                return;
            }
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            _selectedCellId = grid.ToCellId(preset.Row, preset.Column);
            _hoveredCellId = grid.ToCellId(Math.Max(0, preset.Row - 2),
                Math.Min(grid.Columns - 1, preset.Column + 3));
            SetRegionView(preset.Row, preset.Column, preset.DetailLevel);
            var cameraFocus = (p0FinalAssetReview && presetId !=
                StrategicCellCameraRig.LuoyangP0FinalAssetVerticalSliceOverview) ||
                (p0LandmarkSecondBatchReview && presetId !=
                    StrategicCellCameraRig.LuoyangP0LandmarkSecondBatchOverview) ||
                (p0LandmarkThirdBatchReview && presetId !=
                    StrategicCellCameraRig.LuoyangP0LandmarkThirdBatchOverview) ||
                (p0NamedGateFourthBatchReview && presetId !=
                    StrategicCellCameraRig.LuoyangP0NamedGateFourthBatchOverview)
                ? ResolveP0CloseupCameraFocus(_selectedCellId.Value)
                : Vector3.zero;
            ConfigureCamera(cameraFocus, preset.Size, preset.Pitch, preset.Yaw);
        }

        public void ApplyP0MultiAngleReviewCamera(int pieceIndex,
            int angleIndex)
        {
            ApplyStrategicCellCamera(LuoyangP0MultiAngleReviewRig.GetCameraId(
                pieceIndex, angleIndex));
        }

        public void StepP0ReviewPiece(int delta)
        {
            _p0ReviewPieceIndex = WrapIndex(_p0ReviewPieceIndex + delta,
                LuoyangP0MultiAngleReviewRig.PieceCount);
            ApplyP0MultiAngleReviewCamera(_p0ReviewPieceIndex,
                _p0ReviewAngleIndex);
        }

        public void StepP0ReviewAngle(int delta)
        {
            _p0ReviewAngleIndex = WrapIndex(_p0ReviewAngleIndex + delta,
                LuoyangP0MultiAngleReviewRig.AngleCount);
            ApplyP0MultiAngleReviewCamera(_p0ReviewPieceIndex,
                _p0ReviewAngleIndex);
        }

        public void ApplyP0LandmarkSecondBatchMultiAngleReviewCamera(
            int pieceIndex, int angleIndex)
        {
            ApplyStrategicCellCamera(
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetCameraId(
                    pieceIndex, angleIndex));
        }

        public void StepP0LandmarkSecondBatchReviewPiece(int delta)
        {
            _p0LandmarkSecondBatchReviewPieceIndex = WrapIndex(
                _p0LandmarkSecondBatchReviewPieceIndex + delta,
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.PieceCount);
            ApplyP0LandmarkSecondBatchMultiAngleReviewCamera(
                _p0LandmarkSecondBatchReviewPieceIndex,
                _p0LandmarkSecondBatchReviewAngleIndex);
        }

        public void StepP0LandmarkSecondBatchReviewAngle(int delta)
        {
            _p0LandmarkSecondBatchReviewAngleIndex = WrapIndex(
                _p0LandmarkSecondBatchReviewAngleIndex + delta,
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.AngleCount);
            ApplyP0LandmarkSecondBatchMultiAngleReviewCamera(
                _p0LandmarkSecondBatchReviewPieceIndex,
                _p0LandmarkSecondBatchReviewAngleIndex);
        }

        public void ApplyP0NamedGateFourthBatchMultiAngleReviewCamera(
            int pieceIndex, int angleIndex)
        {
            ApplyStrategicCellCamera(
                LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.GetCameraId(
                    pieceIndex, angleIndex));
        }

        public void StepP0NamedGateFourthBatchReviewPiece(int delta)
        {
            _p0NamedGateFourthBatchReviewPieceIndex = WrapIndex(
                _p0NamedGateFourthBatchReviewPieceIndex + delta,
                LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.PieceCount);
            ApplyP0NamedGateFourthBatchMultiAngleReviewCamera(
                _p0NamedGateFourthBatchReviewPieceIndex,
                _p0NamedGateFourthBatchReviewAngleIndex);
        }

        public void StepP0NamedGateFourthBatchReviewAngle(int delta)
        {
            _p0NamedGateFourthBatchReviewAngleIndex = WrapIndex(
                _p0NamedGateFourthBatchReviewAngleIndex + delta,
                LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.AngleCount);
            ApplyP0NamedGateFourthBatchMultiAngleReviewCamera(
                _p0NamedGateFourthBatchReviewPieceIndex,
                _p0NamedGateFourthBatchReviewAngleIndex);
        }

        public void FocusStrategicCell(WorldMapCellId cellId)
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            if (!grid.TryDecode(cellId, out var row, out var column))
                throw new ArgumentOutOfRangeException(nameof(cellId));
            CellOverlayVisible = true;
            _selectedCellId = cellId;
            _hoveredCellId = null;
            SetRegionView(row, column, VisualTerrainDetailLevel.City);
            ConfigureCamera(Vector3.zero, 17.5f, 57f, -18f);
        }

        public void SetPresentationUiVisible(bool visible) => PresentationUiVisible = visible;

        public bool TryPickGlobalCell(Vector3 unityLocalPosition, out WorldMapCellId cellId)
        {
            var global = _cellBinding.UnityToGlobal(
                new UnityLocalPosition(unityLocalPosition.x, unityLocalPosition.y, unityLocalPosition.z),
                _floatingOrigin, HorizontalMetresPerUnit);
            return _cellBinding.TryGlobalToCell(global, out cellId);
        }

        public NaturalMapPerformanceSnapshot GetPerformanceSnapshot(float observedFrameMilliseconds = 0f)
        {
            return new NaturalMapPerformanceSnapshot
            {
                Mode = View.ToString().ToUpperInvariant(),
                ArtProfileId = ActiveArtProfileId,
                TerrainGenerationMilliseconds = _lastTerrainGenerationMilliseconds,
                ObservedFrameMilliseconds = observedFrameMilliseconds,
                CpuFrameMilliseconds = observedFrameMilliseconds,
                // Unity batch Game View does not expose a reliable GPU timestamp on every backend.
                // Keep zero as an explicit unavailable value instead of inventing a measurement.
                GpuFrameMilliseconds = 0d,
                ResidentTerrainMeshes = RuntimeTerrainObjectCount,
                TerrainMeshBytes = _residentTerrainMeshBytes,
                VegetationDrawBatches = RuntimeVegetationObjectCount,
                RiverMeshCount = RuntimeRiverMeshCount,
                ManagedGcDeltaBytes = _lastManagedGcDeltaBytes,
                WorldRegionTransitionMilliseconds = _lastTransitionMilliseconds,
                DrawCalls = RuntimeTerrainObjectCount + RuntimeRiverMeshCount +
                            RuntimeVegetationObjectCount + RuntimeCellOverlayObjectCount +
                            RuntimeBuildableFacilityRendererCount,
                MaterialCount = 3 + (CellOverlayVisible ? 1 : 0) +
                                (BuildableFacilityPreviewVisible
                                    ? BuildableFacilityMaterialCount : 0),
                ShaderVariantCount = 1 + (CellOverlayVisible ? 1 : 0),
                VisualDetailLevel = _visualDetailLevel.ToString().ToUpperInvariant(),
                VisualTerrainVertices = _lastVisualTerrainVertexCount,
                RiverAdaptiveSamples = _lastRiverDiagnostics.AdaptiveSamples,
                RiverBevelFallbacks = _lastRiverDiagnostics.BevelFallbacks
            };
        }

        public void CaptureEvidence(string absolutePath, int width = 1280, int height = 720)
        {
            if (_camera == null) throw new InvalidOperationException("Natural map camera is not initialized.");
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? ".");
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var priorTarget = _camera.targetTexture;
            var priorActive = RenderTexture.active;
            try
            {
                _camera.targetTexture = target;
                _camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                DestroyImmediate(texture);
            }
            finally
            {
                _camera.targetTexture = priorTarget;
                RenderTexture.active = priorActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private void EnsureSceneObjects()
        {
            _terrainRoot = NewRoot("Natural Terrain LOD");
            _riverRoot = NewRoot("Natural River And Bank Features");
            _vegetationRoot = NewRoot("Batched Natural Forest Canopy");
            _gridRoot = NewRoot("Explicit Strategic Cell Overlay");
            InitializeBuildableFacilityModelKit();
            _lightingRoot = NewRoot("Natural World Lighting");
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
            }
            _camera.backgroundColor = new Color(0.25f, 0.47f, 0.57f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 6500f;
            _camera.allowHDR = true;
            _terrainMaterial = CreateNaturalMaterial("Natural Terrain V2 Material", 0.10f);
            _riverMaterial = CreateNaturalMaterial("Natural River And Bank V2 Material", 0.025f);
            _vegetationMaterial = CreateNaturalMaterial("Natural Forest V2 Material", 0.08f);
            var cellShader = Shader.Find("Mandate/Strategic Cell Overlay") ??
                             Shader.Find("Sprites/Default");
            _strategicCellMaterial = new Material(cellShader)
                { name = "Explicit Strategic Cell Overlay Material" };
            var lightObject = new GameObject("Strategic Sun");
            lightObject.transform.SetParent(_lightingRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            _sun = lightObject.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        private GameObject NewRoot(string name)
        {
            var value = new GameObject(name);
            value.transform.SetParent(transform, false);
            return value;
        }

        private static Material CreateNaturalMaterial(string name, float noiseStrength)
        {
            var shader = Shader.Find("Mandate/Natural Terrain V2") ??
                         Shader.Find("Sprites/Default") ?? Shader.Find("Diffuse");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", Color.white);
            if (material.HasProperty("_NoiseStrength")) material.SetFloat("_NoiseStrength", noiseStrength);
            return material;
        }

        private void ApplyArtProfileParameters()
        {
            if (_terrainMaterial == null) return;
            ApplyMaterial(_terrainMaterial, _activeArtProfile.TerrainTint,
                _activeArtProfile.TerrainNoiseStrength);
            ApplyMaterial(_riverMaterial, _activeArtProfile.RiverTint,
                _activeArtProfile.RiverNoiseStrength);
            ApplyMaterial(_vegetationMaterial, _activeArtProfile.ForestTint,
                _activeArtProfile.ForestNoiseStrength);
            if (_sun != null)
            {
                _sun.color = _activeArtProfile.SunColor;
                _sun.intensity = _activeArtProfile.SunIntensity;
            }
            RenderSettings.ambientLight = _activeArtProfile.AmbientColor;
            if (_camera != null) _camera.backgroundColor = _activeArtProfile.BackgroundColor;
        }

        private void ApplyMaterial(Material material, Color tint, float noiseStrength)
        {
            if (material == null) return;
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", tint);
            if (material.HasProperty("_NoiseStrength")) material.SetFloat("_NoiseStrength", noiseStrength);
            if (material.HasProperty("_Saturation")) material.SetFloat("_Saturation", _activeArtProfile.Saturation);
            if (material.HasProperty("_SlopeStrength")) material.SetFloat("_SlopeStrength", _activeArtProfile.SlopeStrength);
            if (material.HasProperty("_CurvatureStrength")) material.SetFloat("_CurvatureStrength", _activeArtProfile.CurvatureStrength);
            if (material.HasProperty("_RidgeStrength")) material.SetFloat("_RidgeStrength", _activeArtProfile.RidgeStrength);
            if (material.HasProperty("_ValleyStrength")) material.SetFloat("_ValleyStrength", _activeArtProfile.ValleyStrength);
            if (material.HasProperty("_MacroScale")) material.SetFloat("_MacroScale", _activeArtProfile.MacroNoiseScale);
            if (material.HasProperty("_MacroStrength")) material.SetFloat("_MacroStrength", _activeArtProfile.MacroNoiseStrength);
            if (material.HasProperty("_FusionMode")) material.SetFloat("_FusionMode",
                _activeArtProfile.Style == HanWorldArtStyle.ZhonghuaSanguozhiFusion ? 1f : 0f);
            if (material.HasProperty("_FusionStrength")) material.SetFloat("_FusionStrength", _activeArtProfile.FusionStrength);
            if (material.HasProperty("_FusionMountainTint")) material.SetColor("_FusionMountainTint", _activeArtProfile.FusionMountainTint);
            if (material.HasProperty("_FusionForestTint")) material.SetColor("_FusionForestTint", _activeArtProfile.FusionForestTint);
            if (material.HasProperty("_FusionRiverValleyTint")) material.SetColor("_FusionRiverValleyTint", _activeArtProfile.FusionRiverValleyTint);
            if (material.HasProperty("_FusionPlainTint")) material.SetColor("_FusionPlainTint", _activeArtProfile.FusionPlainTint);
            if (material.HasProperty("_VisualDetail")) material.SetFloat("_VisualDetail",
                (float)_visualDetailLevel / (float)VisualTerrainDetailLevel.ClosePreview);
        }

        private void ReplaceTerrain(NaturalTerrainLodSet lod, bool addFormalTileColliders)
        {
            ClearChildren(_terrainRoot.transform);
            _residentTerrainMeshBytes = 0L;
            _lastFusionFeatureVertexCount = 0L;
            _lastVisualTerrainVertexCount = 0L;
            if (lod.FarOrWorld != null)
                AddTerrainMesh(lod.FarOrWorld, lod.FormalTiles.Count == 0
                    ? "LOD0 WORLD TERRAIN"
                    : "LOD0 REGION CONTINUOUS CELL TERRAIN", false, 0f);
            foreach (var data in lod.FormalTiles)
            {
                if (_activeArtProfile.Style == HanWorldArtStyle.ZhonghuaSanguozhiFusion)
                {
                    var profile = VisualTerrainDetailCatalog.Get(_visualDetailLevel);
                    var refined = new VisualTerrainDetailGenerator().Refine(data, profile);
                    AddTerrainMesh(refined, data.Tile.Id.StableId + ".visual." +
                        _visualDetailLevel.ToString().ToLowerInvariant(), addFormalTileColliders,
                        0.004f, renderSurface: true);
                }
                else AddTerrainMesh(data, data.Tile.Id.StableId, addFormalTileColliders, 0f,
                    renderSurface: false);
            }
        }

        private void AddTerrainMesh(NaturalTerrainMeshData data, string name, bool addCollider,
            float yOffset, bool renderSurface = true)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_terrainRoot.transform, false);
            gameObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var mesh = BuildUnityMesh(data);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _terrainMaterial;
            renderer.enabled = renderSurface;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            if (addCollider) gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            _residentTerrainMeshBytes += mesh.vertexCount * 36L + mesh.triangles.Length * 4L;
            _lastVisualTerrainVertexCount += mesh.vertexCount;
        }

        private Mesh BuildUnityMesh(NaturalTerrainMeshData data)
        {
            var vertices = new Vector3[data.Vertices.Length];
            var colours = new Color32[data.Vertices.Length];
            var uvs = new Vector2[data.Vertices.Length];
            for (var index = 0; index < data.Vertices.Length; index++)
            {
                var source = data.Vertices[index];
                var local = _cellBinding.GlobalToUnity(
                    new GlobalProjectedCoordinate(source.GlobalX, source.GlobalY),
                    source.PresentationElevationMetres, _floatingOrigin,
                    HorizontalMetresPerUnit, VerticalMetresPerUnit);
                vertices[index] = new Vector3((float)local.XMetres,
                    (float)local.ElevationMetres, (float)local.ZMetres);
                colours[index] = _surfaceBlend.Evaluate(source);
                uvs[index] = new Vector2((float)(source.GlobalX / 400000d),
                    (float)(source.GlobalY / 400000d));
            }
            var mesh = new Mesh { name = data.Tile.Id.StableId };
            if (vertices.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.colors32 = colours;
            mesh.uv = uvs;
            var fusionFeatures = ZhonghuaFusionTerrainFeatureAnalyzer.Analyze(data);
            mesh.SetUVs(1, fusionFeatures.Primary);
            mesh.SetUVs(2, fusionFeatures.Secondary);
            _lastFusionFeatureVertexCount += data.Vertices.Length;
            mesh.triangles = data.Triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void ReplaceRivers(Func<double, double, bool> filter)
        {
            ClearChildren(_riverRoot.transform);
            var gameObject = new GameObject("River V2 Water And Bank Batch");
            gameObject.transform.SetParent(_riverRoot.transform, false);
            var generator = new GlobalRiverVisualGenerator();
            var mesh = generator.BuildCombinedMesh(_source.Rivers,
                _floatingOrigin, HorizontalMetresPerUnit, filter, GetPresentationHeightForGlobal,
                Math.Max(1, _source.Config.RiverSmoothingIterations),
                _activeArtProfile.RiverWidthScale, RiverMeshBuildOptions.For(_visualDetailLevel));
            _lastRiverDiagnostics = generator.LastDiagnostics;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _riverMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void ReplaceVegetation(RegionWindow? window)
        {
            ClearChildren(_vegetationRoot.transform);
            if (!window.HasValue) return;
            var value = window.Value;
            var styleD = _activeArtProfile.Style == HanWorldArtStyle.ZhonghuaSanguozhiFusion;
            var forestLod = styleD && _visualDetailLevel >= VisualTerrainDetailLevel.City
                ? ForestPresentationLod.CityIndividualTrees
                : ForestPresentationLod.RegionCanopyCluster;
            var gameObject = new GameObject(forestLod == ForestPresentationLod.CityIndividualTrees
                ? "Forest V2 City Individual Tree Batch"
                : "Forest V2 Region Canopy Cluster Batch");
            gameObject.transform.SetParent(_vegetationRoot.transform, false);
            var mesh = new GlobalVegetationGenerator().BuildCombinedMesh(_source,
                value.FirstRow, value.FirstColumn, value.Rows, value.Columns, _floatingOrigin,
                HorizontalMetresPerUnit, VerticalMetresPerUnit, GetPresentationHeightForGlobal,
                forestLod == ForestPresentationLod.CityIndividualTrees ? 4 :
                    Math.Max(1, _source.Config.ForestLatticePerCell),
                styleD ? Math.Max(0.60f, _activeArtProfile.ForestCanopyScale) :
                    _activeArtProfile.ForestCanopyScale, forestLod);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _vegetationMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private void ReplaceGrid(RegionWindow? window)
        {
            DestroyGridMeshes();
            ClearChildren(_gridRoot.transform);
            _visibleStrategicCellCount = 0;
            _strategicCellCoverageCount = 0UL;
            _strategicGridStepCells = 0;
            StrategicGridLod = StrategicCellGridLod.Off;
            if (!window.HasValue) return;
            var value = window.Value;
            var geometry = ExplicitStrategicCellMapV1.BuildGeometry(
                GlobalSpatialFoundationV1.CreateCellGrid(), value.FirstRow, value.FirstColumn,
                value.Rows, value.Columns, _floatingOrigin, HorizontalMetresPerUnit,
                GetPresentationHeightForGlobal, _hoveredCellId, _selectedCellId);
            _visibleStrategicCellCount = geometry.VisibleCellIds.Count;
            _strategicCellCoverageCount = GlobalSpatialFoundationV1.CreateCellGrid().CellCount;
            _strategicGridStepCells = 1;
            StrategicGridLod = StrategicCellGridLod.ExactCell;
            AddStrategicCellMesh("Strategic Cell Faces", geometry.CreateFaceMesh());
            AddStrategicCellMesh("Strategic Cell Edges And Highlights", geometry.CreateEdgeMesh());
        }

        private void ReplaceNationwideGrid()
        {
            DestroyGridMeshes();
            ClearChildren(_gridRoot.transform);
            var geometry = ExplicitStrategicCellMapV1.BuildNationwideOverviewGeometry(
                GlobalSpatialFoundationV1.CreateCellGrid(),
                ExplicitStrategicCellMapV1.NationwideOverviewStepCells,
                _floatingOrigin, HorizontalMetresPerUnit, GetPresentationHeightForGlobal);
            _visibleStrategicCellCount = 0;
            _strategicCellCoverageCount = geometry.CoveredCellCount;
            _strategicGridStepCells = geometry.DisplayStepCells;
            StrategicGridLod = StrategicCellGridLod.NationwideGuide32;
            AddStrategicCellMesh("Nationwide Strategic Cell Guide LOD 32x32",
                geometry.CreateEdgeMesh());
        }

        private void AddStrategicCellMesh(string name, Mesh mesh)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_gridRoot.transform, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _strategicCellMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void RefreshStrategicCellGrid()
        {
            if (!CellOverlayVisible || View != HanNaturalMapView.Region) return;
            ReplaceGrid(new RegionWindow(_focusRow - 12, _focusColumn - 12,
                ExplicitStrategicCellMapV1.ReviewWindowCells,
                ExplicitStrategicCellMapV1.ReviewWindowCells));
        }

        private void DestroyGridMeshes()
        {
            if (_gridRoot == null) return;
            for (var index = 0; index < _gridRoot.transform.childCount; index++)
            {
                var filter = _gridRoot.transform.GetChild(index).GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    DestroyImmediate(filter.sharedMesh);
            }
        }

        private void ValidateCellId(WorldMapCellId? id, string parameterName)
        {
            if (!id.HasValue) return;
            if (!GlobalSpatialFoundationV1.CreateCellGrid().TryDecode(id.Value, out _, out _))
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private void ConfigureCamera(Vector3 focus, float orthographicSize, float pitch, float yaw)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var distance = Mathf.Max(40f, orthographicSize * 1.45f);
            _camera.transform.rotation = rotation;
            _camera.transform.position = focus - rotation * Vector3.forward * distance;
            _camera.orthographicSize = orthographicSize;
        }

        private Vector3 ResolveP0CloseupCameraFocus(WorldMapCellId cellId)
        {
            if (_buildableFacilityRoot == null) return Vector3.zero;
            var instances = _buildableFacilityRoot.GetComponentsInChildren<
                HanBuildableFacilityModelInstance>(true);
            foreach (var instance in instances)
            {
                if (instance.CellId64 != cellId.Value) continue;
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return instance.transform.position;
                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                    bounds.Encapsulate(renderers[index].bounds);
                return bounds.center;
            }
            return Vector3.zero;
        }

        private void ConfigureAtmosphere(bool world)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = world
                ? _activeArtProfile.FogColor
                : Color.Lerp(_activeArtProfile.FogColor, _activeArtProfile.TerrainTint, 0.22f);
            RenderSettings.fogStartDistance = world
                ? _activeArtProfile.WorldFogStart : _activeArtProfile.RegionFogStart;
            RenderSettings.fogEndDistance = world
                ? _activeArtProfile.WorldFogEnd : _activeArtProfile.RegionFogEnd;
            _camera.backgroundColor = _activeArtProfile.BackgroundColor;
        }

        private Vector3 GlobalCellCenterToLocal(int row, int column)
        {
            var cell = _source.ReadSample(row, column).Cell;
            return new Vector3(
                (float)((cell.CenterX - _floatingOrigin.EastingMetres) / HorizontalMetresPerUnit),
                0f,
                (float)((cell.CenterY - _floatingOrigin.NorthingMetres) / HorizontalMetresPerUnit));
        }

        private float GetPresentationHeightForGlobal(double x, double y)
        {
            var column = (int)Math.Floor((x - _source.OriginX) / _source.CellSizeMetres);
            var row = (int)Math.Floor((_source.OriginY - y) / _source.CellSizeMetres);
            if (row < 0 || row >= _source.Rows || column < 0 || column >= _source.Columns)
                return 0.08f;
            var nextRow = Math.Min(_source.Rows - 1, row + 1);
            var nextColumn = Math.Min(_source.Columns - 1, column + 1);
            var columnT = Math.Max(0d, Math.Min(1d,
                (x - (_source.OriginX + column * _source.CellSizeMetres)) / _source.CellSizeMetres));
            var rowT = Math.Max(0d, Math.Min(1d,
                ((_source.OriginY - row * _source.CellSizeMetres) - y) / _source.CellSizeMetres));
            var exaggeration = View == HanNaturalMapView.World
                ? _activeArtProfile.WorldVerticalExaggeration
                : _activeArtProfile.RegionVerticalExaggeration;
            var topLeft = _source.ReadSample(row, column);
            var topRight = _source.ReadSample(row, nextColumn);
            var bottomLeft = _source.ReadSample(nextRow, column);
            var bottomRight = _source.ReadSample(nextRow, nextColumn);
            var enhanced = Bilinear(Enhance(topLeft.Cell.Elevation, exaggeration),
                Enhance(topRight.Cell.Elevation, exaggeration),
                Enhance(bottomLeft.Cell.Elevation, exaggeration),
                Enhance(bottomRight.Cell.Elevation, exaggeration), columnT, rowT);
            if (_activeArtProfile.Style == HanWorldArtStyle.ZhonghuaSanguozhiFusion)
            {
                var profile = VisualTerrainDetailCatalog.Get(_visualDetailLevel);
                var surface = new NaturalSurfaceClassifier().Classify(columnT < 0.5d
                    ? rowT < 0.5d ? topLeft : bottomLeft
                    : rowT < 0.5d ? topRight : bottomRight);
                enhanced += VisualTerrainDetailGenerator.MicroRelief(x, y, surface,
                    profile.MicroReliefAmplitudeMetres);
            }
            return (float)(enhanced / VerticalMetresPerUnit + 0.055d);
        }

        private static double Enhance(double elevation, double exaggeration) => elevation <= 0d
            ? Math.Max(-60d, elevation * 0.12d)
            : Math.Min(elevation, 300d) * (0.65d + 0.35d * exaggeration) +
              Math.Max(0d, elevation - 300d) * exaggeration;

        private static double Bilinear(double topLeft, double topRight, double bottomLeft,
            double bottomRight, double x, double y) =>
            (topLeft + (topRight - topLeft) * x) * (1d - y) +
            (bottomLeft + (bottomRight - bottomLeft) * x) * y;

        private static double PositiveOr(double value, double fallback) => value > 0d ? value : fallback;

        private static int WrapIndex(int value, int count)
        {
            var wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
                DestroyImmediate(root.GetChild(index).gameObject);
        }

        private void OnGUI()
        {
            if (!IsReady || !PresentationUiVisible) return;
            GUI.Box(new Rect(12, 12, 1260, 88), string.Empty);
            if (GUI.Button(new Rect(20, 21, 70, 30), "WORLD")) SetWorldView();
            if (GUI.Button(new Rect(96, 21, 82, 30), "LUOYANG")) LocateLuoyang();
            if (GUI.Button(new Rect(184, 21, 92, 30), "HENAN YIN")) LocateHenanYin();
            if (GUI.Button(new Rect(282, 21, 96, 30), CellOverlayVisible ? "CELLS OFF" : "CELL MAP"))
                SetCellOverlayVisible(!CellOverlayVisible);
            if (GUI.Button(new Rect(384, 21, 104, 30), "BUILDINGS"))
                ApplyStrategicCellCamera(StrategicCellCameraRig.BuildableFacilityReview);
            if (GUI.Button(new Rect(494, 21, 112, 30), "LUOYANG KIT"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFacilityCoverageReview);
            if (GUI.Button(new Rect(612, 21, 110, 30), "LANDMARKS"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangHistoricalLandmarkReview);
            if (GUI.Button(new Rect(728, 21, 100, 30), "GATES"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangGateIdentityReview);
            if (GUI.Button(new Rect(834, 21, 100, 30), "FABRIC"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview);
            if (GUI.Button(new Rect(940, 21, 100, 30), "CITY"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            if (GUI.Button(new Rect(1046, 21, 94, 30), "INFRA"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangInfrastructureOverview);
            if (GUI.Button(new Rect(1146, 21, 110, 30), "P0 BATCH 2"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangP0LandmarkSecondBatchOverview);
            if (GUI.Button(new Rect(20, 59, 110, 30), "STYLE A"))
                SetArtStyle(HanWorldArtStyle.RealisticNatural);
            if (GUI.Button(new Rect(136, 59, 110, 30), "STYLE B"))
                SetArtStyle(HanWorldArtStyle.ChineseSemiRealistic);
            if (GUI.Button(new Rect(252, 59, 110, 30), "STYLE C"))
                SetArtStyle(HanWorldArtStyle.StrategicSandbox);
            if (GUI.Button(new Rect(368, 59, 110, 30), "STYLE D"))
                SetArtStyle(HanWorldArtStyle.ZhonghuaSanguozhiFusion);
            if (GUI.Button(new Rect(484, 59, 120, 30), "DEFENSE"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangLowFrequencyDefenseOverview);
            if (GUI.Button(new Rect(610, 59, 120, 30), "RESOURCES"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangResourceAgricultureOverview);
            if (GUI.Button(new Rect(736, 59, 110, 30), "CIVIC"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFinalCivicOverview);
            if (GUI.Button(new Rect(852, 59, 118, 30), "ASSET QA"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewAll);
            if (GUI.Button(new Rect(976, 59, 120, 30), "P0 SLICE"))
                ApplyStrategicCellCamera(
                    StrategicCellCameraRig
                        .LuoyangP0FinalAssetVerticalSliceOverview);
            GUI.Label(new Rect(1102, 61, 154, 26),
                _activeArtProfile.ProfileName);
            if (_luoyangP0FinalAssetVerticalSlicePreviewVisible)
                DrawP0MultiAngleReviewControls();
            if (_luoyangP0LandmarkSecondBatchPreviewVisible)
                DrawP0LandmarkSecondBatchReviewControls();
            if (_luoyangP0NamedGateFourthBatchPreviewVisible)
                DrawP0NamedGateFourthBatchReviewControls();
        }

        private void DrawP0NamedGateFourthBatchReviewControls()
        {
            GUI.Box(new Rect(12, 106, 1260, 44), string.Empty);
            if (GUI.Button(new Rect(20, 113, 92, 30), "OVERVIEW"))
                ApplyStrategicCellCamera(StrategicCellCameraRig
                    .LuoyangP0NamedGateFourthBatchOverview);
            if (GUI.Button(new Rect(120, 113, 38, 30), "<"))
                StepP0NamedGateFourthBatchReviewPiece(-1);
            GUI.Label(new Rect(164, 118, 222, 24),
                ActiveP0NamedGateFourthBatchReviewPieceLabel + "  " +
                (_p0NamedGateFourthBatchReviewPieceIndex + 1) + "/" +
                LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.PieceCount);
            if (GUI.Button(new Rect(392, 113, 38, 30), ">"))
                StepP0NamedGateFourthBatchReviewPiece(1);
            if (GUI.Button(new Rect(446, 113, 38, 30), "<"))
                StepP0NamedGateFourthBatchReviewAngle(-1);
            GUI.Label(new Rect(490, 118, 198, 24),
                ActiveP0NamedGateFourthBatchReviewAngleLabel + "  " +
                (_p0NamedGateFourthBatchReviewAngleIndex + 1) + "/" +
                LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.AngleCount);
            if (GUI.Button(new Rect(694, 113, 38, 30), ">"))
                StepP0NamedGateFourthBatchReviewAngle(1);
            GUI.Label(new Rect(754, 118, 494, 24),
                "USER DECISION: PENDING  |  FINAL ART APPROVAL: FALSE");
        }

        private void DrawP0LandmarkSecondBatchReviewControls()
        {
            GUI.Box(new Rect(12, 106, 1260, 44), string.Empty);
            if (GUI.Button(new Rect(20, 113, 92, 30), "OVERVIEW"))
                ApplyStrategicCellCamera(StrategicCellCameraRig
                    .LuoyangP0LandmarkSecondBatchOverview);
            if (GUI.Button(new Rect(120, 113, 38, 30), "<"))
                StepP0LandmarkSecondBatchReviewPiece(-1);
            GUI.Label(new Rect(164, 118, 222, 24),
                ActiveP0LandmarkSecondBatchReviewPieceLabel + "  " +
                (_p0LandmarkSecondBatchReviewPieceIndex + 1) + "/" +
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.PieceCount);
            if (GUI.Button(new Rect(392, 113, 38, 30), ">"))
                StepP0LandmarkSecondBatchReviewPiece(1);
            if (GUI.Button(new Rect(446, 113, 38, 30), "<"))
                StepP0LandmarkSecondBatchReviewAngle(-1);
            GUI.Label(new Rect(490, 118, 198, 24),
                ActiveP0LandmarkSecondBatchReviewAngleLabel + "  " +
                (_p0LandmarkSecondBatchReviewAngleIndex + 1) + "/" +
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.AngleCount);
            if (GUI.Button(new Rect(694, 113, 38, 30), ">"))
                StepP0LandmarkSecondBatchReviewAngle(1);
            GUI.Label(new Rect(754, 118, 494, 24),
                "USER DECISION: ACCEPTED  |  FINAL ART APPROVAL: TRUE");
        }

        private void DrawP0MultiAngleReviewControls()
        {
            GUI.Box(new Rect(12, 106, 1260, 44), string.Empty);
            if (GUI.Button(new Rect(20, 113, 92, 30), "OVERVIEW"))
                ApplyStrategicCellCamera(StrategicCellCameraRig
                    .LuoyangP0FinalAssetVerticalSliceOverview);
            if (GUI.Button(new Rect(120, 113, 38, 30), "<"))
                StepP0ReviewPiece(-1);
            GUI.Label(new Rect(164, 118, 222, 24),
                ActiveP0ReviewPieceLabel + "  " +
                (_p0ReviewPieceIndex + 1) + "/" +
                LuoyangP0MultiAngleReviewRig.PieceCount);
            if (GUI.Button(new Rect(392, 113, 38, 30), ">"))
                StepP0ReviewPiece(1);
            if (GUI.Button(new Rect(446, 113, 38, 30), "<"))
                StepP0ReviewAngle(-1);
            GUI.Label(new Rect(490, 118, 198, 24),
                ActiveP0ReviewAngleLabel + "  " +
                (_p0ReviewAngleIndex + 1) + "/" +
                LuoyangP0MultiAngleReviewRig.AngleCount);
            if (GUI.Button(new Rect(694, 113, 38, 30), ">"))
                StepP0ReviewAngle(1);
            GUI.Label(new Rect(754, 118, 494, 24),
                "USER DECISION: ACCEPTED  |  FINAL ART APPROVAL: TRUE");
        }

        private void OnDestroy()
        {
            _source?.Dispose();
            DisposeBuildableFacilityModelKit();
            if (_terrainMaterial != null) DestroyImmediate(_terrainMaterial);
            if (_riverMaterial != null) DestroyImmediate(_riverMaterial);
            if (_vegetationMaterial != null) DestroyImmediate(_vegetationMaterial);
            if (_strategicCellMaterial != null) DestroyImmediate(_strategicCellMaterial);
        }

        private readonly struct RegionWindow
        {
            public RegionWindow(int firstRow, int firstColumn, int rows, int columns)
            {
                FirstRow = firstRow;
                FirstColumn = firstColumn;
                Rows = rows;
                Columns = columns;
            }
            public int FirstRow { get; }
            public int FirstColumn { get; }
            public int Rows { get; }
            public int Columns { get; }
        }
    }
}
