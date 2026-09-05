using System;
using System.Globalization;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    /// <summary>
    /// Rebuildable player-facing renderer over the formal HanWorldV1 map and
    /// the current formal CellRoute. It never mutates WorldState.
    /// </summary>
    public sealed class PlayableFormalWorldMapController : MonoBehaviour
    {
        public const string ContractId =
            "presentation.playable-formal-world-map.v1";

        private WorldState _world;
        private string _personId;
        private MerchantHouseholdContentRegistry _merchantContent;
        private IStrategicCellRouteProvider _strategicRouteProvider;
        private HanWorldNaturalMapController _naturalMap;
        private Camera _mapCamera;
        private RenderTexture _target;
        private GameObject _routeRoot;
        private Material _travelledMaterial;
        private Material _remainingMaterial;
        private Material _markerMaterial;
        private string _projectionSignature = string.Empty;
        private bool _worldView = true;

        public bool IsReady => _naturalMap != null && _naturalMap.IsReady;
        public string LastError => _naturalMap == null
            ? "正式全国地图尚未初始化。"
            : _naturalMap.LastError;
        public bool UsesHanWorldV1 => IsReady &&
            _naturalMap.StrategicCellContractId ==
                ExplicitStrategicCellMapV1.ContractId;
        public bool IsWorldView => _worldView;
        public bool IsCountyPlanning => IsReady &&
            _naturalMap.AdministrativeMapViewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning;
        public bool AdministrativeOverlayVisible => IsReady &&
            _naturalMap.AdministrativeOverlayVisible;
        public AdministrativeSelectionProjection AdministrativeSelection =>
            IsReady ? _naturalMap.AdministrativeSelection : null;
        public AdministrativeMapViewState AdministrativeMapViewState =>
            IsReady ? _naturalMap.AdministrativeMapViewState : null;
        public AdministrativeMapLabelLevel AdministrativeLabelLevel =>
            IsReady
                ? _naturalMap.AdministrativeMapViewState.LabelLevel
                : AdministrativeMapLabelLevel.Province;
        public AdministrativeBoundaryTopology AdministrativeBoundaryTopology =>
            IsReady ? _naturalMap.AdministrativeBoundaryTopology : null;
        public int AdministrativeScenarioStartYear => IsReady
            ? _naturalMap.AdministrativeScenarioStartYear : 0;
        public double AdministrativeBoundaryBuildMilliseconds => IsReady
            ? _naturalMap.AdministrativeBoundaryBuildMilliseconds : 0d;
        public double AdministrativeRenderBuildMilliseconds => IsReady
            ? _naturalMap.AdministrativeRenderBuildMilliseconds : 0d;
        public long AdministrativeBoundaryCacheBytes => IsReady
            ? _naturalMap.AdministrativeBoundaryCacheBytes : 0L;
        public long AdministrativeRenderGcDeltaBytes => IsReady
            ? _naturalMap.AdministrativeRenderGcDeltaBytes : 0L;
        public int AdministrativeRenderObjectCount => IsReady
            ? _naturalMap.AdministrativeRenderObjectCount : 0;
        public int AdministrativeRenderedChunkCount => IsReady
            ? _naturalMap.AdministrativeRenderedChunkCount : 0;
        public int AdministrativeRenderedSegmentCount => IsReady
            ? _naturalMap.AdministrativeRenderedSegmentCount : 0;
        public bool CellOverlayVisible => IsReady &&
            _naturalMap.CellOverlayVisible;
        public int CellGridStep => IsReady
            ? _naturalMap.StrategicGridStepCells
            : 0;
        public Texture CurrentTexture => _target;
        public PlayableWorldMapProjection RouteProjection { get; private set; }

        public bool EnsureInitialized()
        {
            if (IsReady) return true;
            if (_naturalMap != null) return false;

            var cameraObject = new GameObject("Playable Formal World Map Camera");
            cameraObject.transform.SetParent(transform, false);
            _mapCamera = cameraObject.AddComponent<Camera>();
            _mapCamera.enabled = false;
            _mapCamera.allowHDR = true;
            _mapCamera.clearFlags = CameraClearFlags.SolidColor;
            _mapCamera.backgroundColor = new Color(0.17f, 0.24f, 0.21f);

            var mapObject = new GameObject("Playable HanWorldV1 Map");
            mapObject.transform.SetParent(transform, false);
            _naturalMap = mapObject.AddComponent<HanWorldNaturalMapController>();
            _naturalMap.SetPresentationCamera(_mapCamera);
            if (!_naturalMap.TryInitialize()) return false;
            _naturalMap.SetPresentationUiVisible(false);
            // This controller is rendered inside SimulationDashboard rather
            // than as a full-screen natural-map scene.  Disable the embedded
            // component's automatic Update/OnGUI loop so clicks elsewhere in
            // the player UI cannot change hover/selection presentation state.
            // Its explicit view and rendering methods remain available.
            _naturalMap.enabled = false;
            _naturalMap.SetArtStyle(
                HanWorldArtStyle.ZhonghuaSanguozhiFusion);
            _naturalMap.SetCellOverlayVisible(true);
            _naturalMap.SetWorldView();
            _worldView = true;

            _routeRoot = new GameObject("Formal Player CellRoute Overlay");
            _routeRoot.transform.SetParent(transform, false);
            _travelledMaterial = CreateMaterial(
                "Formal Route Travelled", new Color(0.13f, 0.67f, 0.78f));
            _remainingMaterial = CreateMaterial(
                "Formal Route Remaining", new Color(0.93f, 0.57f, 0.12f));
            _markerMaterial = CreateMaterial(
                "Formal Route Marker", new Color(1f, 0.82f, 0.16f));
            return true;
        }

        public void Bind(WorldState world, string personId,
            MerchantHouseholdContentRegistry merchantContent,
            IStrategicCellRouteProvider strategicRouteProvider)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _personId = personId ?? throw new ArgumentNullException(
                nameof(personId));
            _merchantContent = merchantContent ??
                throw new ArgumentNullException(nameof(merchantContent));
            _strategicRouteProvider = strategicRouteProvider ??
                throw new ArgumentNullException(nameof(strategicRouteProvider));
            _projectionSignature = string.Empty;
            RefreshFromWorld();
        }

        public void RefreshFromWorld()
        {
            if (_world == null || string.IsNullOrEmpty(_personId) ||
                _merchantContent == null || _strategicRouteProvider == null ||
                !EnsureInitialized())
                return;
            var signature = BuildProjectionSignature();
            if (signature == _projectionSignature) return;
            var before = _world.Revision;
            RouteProjection = PlayableWorldMapProjectionSystem.Build(
                _world, _personId, _merchantContent,
                _strategicRouteProvider);
            if (_world.Revision != before)
                throw new InvalidOperationException(
                    "The playable map projection changed formal world state.");
            _projectionSignature = signature;
            RebuildRouteVisuals();
        }

        public Texture Render(int width, int height)
        {
            if (!EnsureInitialized()) return null;
            RefreshFromWorld();
            EnsureTarget(width, height);
            _mapCamera.targetTexture = _target;
            _mapCamera.Render();
            return _target;
        }

        public void ShowNationwide()
        {
            if (!EnsureInitialized()) return;
            _naturalMap.SetWorldView();
            if (!_naturalMap.CellOverlayVisible)
                _naturalMap.SetCellOverlayVisible(true);
            _worldView = true;
            RebuildRouteVisuals();
        }

        public bool FollowPlayerOrCaravan()
        {
            if (!EnsureInitialized()) return false;
            RefreshFromWorld();
            var cellId64 = RouteProjection?.CurrentCellId64 ?? 0UL;
            if (cellId64 == 0) return false;
            if (!_naturalMap.CellOverlayVisible)
                _naturalMap.SetCellOverlayVisible(true);
            if (!_naturalMap.FocusCell(cellId64)) return false;
            _worldView = false;
            RebuildRouteVisuals();
            return true;
        }

        public void ToggleCellOverlay()
        {
            if (!EnsureInitialized()) return;
            _naturalMap.SetCellOverlayVisible(
                !_naturalMap.CellOverlayVisible);
            RebuildRouteVisuals();
        }

        public void ToggleAdministrativeOverlay()
        {
            if (!EnsureInitialized()) return;
            _naturalMap.SetAdministrativeOverlayVisible(
                !_naturalMap.AdministrativeOverlayVisible);
        }

        public void SetAdministrativeLabelLevel(
            AdministrativeMapLabelLevel level)
        {
            if (!EnsureInitialized() || IsCountyPlanning) return;
            _naturalMap.SetAdministrativeLabelLevel(level);
        }

        public bool TrySelectAdministrativeRegion(Vector2 viewportPoint)
        {
            if (!EnsureInitialized()) return false;
            return _naturalMap.TrySelectAdministrativeRegion(viewportPoint);
        }

        public bool AdjustAdministrativeZoom(float wheelDelta,
            Vector2 anchorViewport)
        {
            if (!EnsureInitialized() ||
                !_naturalMap.AdjustAdministrativeZoom(wheelDelta,
                    anchorViewport)) return false;
            _worldView = _naturalMap.View == HanNaturalMapView.World;
            RebuildRouteVisuals();
            return true;
        }

        public void PanAdministrativeMap(Vector2 viewportDelta)
        {
            if (!EnsureInitialized()) return;
            _naturalMap.PanAdministrativeMap(viewportDelta);
        }

        public void RotateAdministrativeMap(float yawDegrees)
        {
            if (!EnsureInitialized()) return;
            _naturalMap.RotateAdministrativeMap(yawDegrees);
        }

        public bool SelectAdministrativeRegion(string regionId)
        {
            if (!EnsureInitialized()) return false;
            return _naturalMap.SelectAdministrativeRegion(regionId);
        }

        public bool EnterSelectedCountyPlanning()
        {
            if (!EnsureInitialized()) return false;
            var selection = _naturalMap.AdministrativeSelection;
            if (selection == null ||
                selection.Level != AdministrativeRegionLevel.County)
                return false;
            if (!_naturalMap.EnterCountyPlanning(selection.RegionId))
                return false;
            _worldView = false;
            RebuildRouteVisuals();
            return true;
        }

        public void ExitCountyPlanning()
        {
            if (!EnsureInitialized()) return;
            _naturalMap.ExitCountyPlanning();
            _worldView = true;
            RebuildRouteVisuals();
        }

        public System.Collections.Generic.IReadOnlyList<
            AdministrativeMapLabelProjection> GetAdministrativeLabels() =>
            IsReady
                ? _naturalMap.GetVisibleAdministrativeLabels()
                : Array.Empty<AdministrativeMapLabelProjection>();

        public bool TryGetAdministrativeLabelViewport(
            AdministrativeMapLabelProjection label, out Vector2 point)
        {
            point = default;
            return IsReady && _naturalMap.TryGetAdministrativeLabelViewport(
                label, out point);
        }

        public bool TryGetViewportPoint(ulong cellId64, out Vector2 point)
        {
            point = default;
            if (!IsReady || cellId64 == 0 ||
                !_naturalMap.TryGetCellLocalPosition(cellId64,
                    out var local))
                return false;
            var viewport = _mapCamera.WorldToViewportPoint(local);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                return false;
            point = new Vector2(viewport.x, viewport.y);
            return true;
        }

        private void RebuildRouteVisuals()
        {
            if (_routeRoot == null) return;
            ClearChildren(_routeRoot.transform);
            var projection = RouteProjection;
            if (projection == null || !projection.HasRoute) return;

            var positions = new Vector3[projection.CellIds.Count];
            for (var i = 0; i < projection.CellIds.Count; i++)
            {
                if (!_naturalMap.TryGetCellLocalPosition(
                        projection.CellIds[i], out positions[i]))
                    return;
                positions[i].y += _worldView ? 1.5f : 0.16f;
            }
            var current = Math.Max(0, Math.Min(
                projection.CurrentCellSequence, positions.Length - 1));
            if (current > 0)
                CreateLine("已走正式路线", positions, 0, current + 1,
                    _travelledMaterial);
            if (current < positions.Length - 1)
                CreateLine("剩余正式路线", positions, current,
                    positions.Length - current, _remainingMaterial);
            CreateMarker("商队当前位置", positions[current],
                _worldView ? 16f : 0.68f);
        }

        private void CreateLine(string name, Vector3[] source, int start,
            int count, Material material)
        {
            if (count < 2) return;
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(_routeRoot.transform, false);
            var line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.startColor = material.color;
            line.endColor = material.color;
            line.startWidth = _worldView ? 3.4f : 0.22f;
            line.endWidth = line.startWidth;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.positionCount = count;
            var positions = new Vector3[count];
            Array.Copy(source, start, positions, 0, count);
            line.SetPositions(positions);
        }

        private void CreateMarker(string name, Vector3 position, float scale)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(_routeRoot.transform, false);
            marker.transform.position = position + Vector3.up * scale * 0.18f;
            marker.transform.localScale = new Vector3(
                scale, scale * 0.22f, scale);
            var collider = marker.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);
            marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
        }

        private string BuildProjectionSignature()
        {
            CivilianFreightState freight = null;
            for (var i = 0; i < _world.CivilianFreights.Count; i++)
            {
                var candidate = _world.CivilianFreights[i];
                if (candidate.CarrierPersonId != _personId ||
                    candidate.PurposeId !=
                        CivilianFreightPurposeIds.MerchantOwnerCarriage)
                    continue;
                if (freight == null || candidate.CreatedDay >
                    freight.CreatedDay)
                    freight = candidate;
            }
            return _world.Revision.ToString(CultureInfo.InvariantCulture) +
                ":" + _world.CivilianFreights.Count + ":" +
                (freight?.Id ?? string.Empty) + ":" +
                (freight?.CellRouteRevision ?? 0) + ":" +
                (freight?.CurrentCellRouteSegmentIndex ?? 0) + ":" +
                (freight?.CellRouteRemainingWeightedCentimetres ?? 0L);
        }

        private void EnsureTarget(int width, int height)
        {
            width = Mathf.Clamp(width, 320, 1600);
            height = Mathf.Clamp(height, 240, 1000);
            if (_target != null && _target.width == width &&
                _target.height == height)
                return;
            ReleaseTarget();
            _target = new RenderTexture(
                width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Playable Formal HanWorldV1 Map",
                antiAliasing = 1,
                useMipMap = false
            };
            _target.Create();
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color");
            return new Material(shader) { name = name, color = color };
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
        }

        private void ReleaseTarget()
        {
            if (_target == null) return;
            if (_mapCamera != null && _mapCamera.targetTexture == _target)
                _mapCamera.targetTexture = null;
            _target.Release();
            DestroyImmediate(_target);
            _target = null;
        }

        private void OnDestroy()
        {
            ReleaseTarget();
            if (_travelledMaterial != null)
                DestroyImmediate(_travelledMaterial);
            if (_remainingMaterial != null)
                DestroyImmediate(_remainingMaterial);
            if (_markerMaterial != null) DestroyImmediate(_markerMaterial);
        }
    }
}
