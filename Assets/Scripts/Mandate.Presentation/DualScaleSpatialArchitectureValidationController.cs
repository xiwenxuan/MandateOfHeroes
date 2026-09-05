using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
    public enum DualScaleValidationEvidenceView : byte
    {
        StrategicTiles,
        PlanningCells,
        FacilityFootprint,
        FourPortTopology,
        WallEdgeAndGate,
        CountyPortalRoute,
        HeightAndLosLow,
        HeightAndLosHigh,
        FacilityGarrisonControl,
        HotWarmCold
    }

    public sealed class DualScaleFacilityVisualMarker : MonoBehaviour
    {
        public string FacilityId;
    }

    public sealed class DualScaleSpatialArchitectureValidationController :
        MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> _facilityVisuals =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly List<Material> _materials = new List<Material>();
        private GameObject _strategicRoot;
        private GameObject _detailRoot;
        private GameObject _gridOverlay;
        private GameObject _portOverlay;
        private GameObject _losLine;
        private GameObject _loadDebug;
        private GameObject _targetMarker;
        private GameObject _gateVisual;
        private TextMesh _statusText;
        private Camera _camera;
        private Light _light;
        private bool _strategicView;
        private bool _gridVisible = true;
        private bool _portsVisible;
        private bool _highObserver;
        private bool _currentLosVisible;
        private string _selectedFacilityId;
        private Vector3 _cameraFocus = new Vector3(40f, 0f, -40f);
        private Vector3 _lastMousePosition;
        private CountySpatialLoadCoordinator _loadCoordinator;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public DualScaleSpatialValidationScenario Scenario { get; private set; }
        public Camera PresentationCamera => _camera;
        public int PlanningCellCount => Scenario?.PlanningCellCount ?? 0;
        public int PlanningCellGameObjectCount => 0;
        public int RuntimePlanningCellRenderObjectCount =>
            _gridOverlay == null ? 0 : 2;
        public int RuntimeChunkCount =>
            (Scenario?.WestCounty.ChunkCount ?? 0) +
            (Scenario?.EastCounty.ChunkCount ?? 0);
        public bool StrategicViewVisible => _strategicView;
        public bool PlanningGridVisible => _gridVisible;
        public bool FourPortDebugVisible => _portsVisible;
        public bool HighObserverEnabled => _highObserver;
        public bool CurrentLosVisible => _currentLosVisible;
        public string SelectedFacilityId => _selectedFacilityId;
        public string WorldSummaryHash => Scenario == null
            ? string.Empty
            : DualScaleWorldSummaryV1.Create(Scenario.World).WorldSummary;
        public CountySpatialCacheHandle WestCountyLoadHandle =>
            Scenario == null ? null : _loadCoordinator.Get(
                Scenario.WestCounty.CountyId);
        public CountySpatialCacheHandle EastCountyLoadHandle =>
            Scenario == null ? null : _loadCoordinator.Get(
                Scenario.EastCounty.CountyId);

        private void Start()
        {
            if (!IsReady) TryInitialize();
        }

        private void OnDestroy()
        {
            foreach (var material in _materials)
                if (material != null)
                {
                    if (Application.isPlaying) Object.Destroy(material);
                    else Object.DestroyImmediate(material);
                }
            _materials.Clear();
        }

        public bool TryInitialize()
        {
            if (IsReady) return true;
            try
            {
                Scenario = DualScaleSpatialValidationScenarioFactory.Create();
                _loadCoordinator = new CountySpatialLoadCoordinator(
                    Scenario.Projection);
                _loadCoordinator.SetLevel(Scenario.WestCounty,
                    CountySpatialLoadLevel.Hot);
                _loadCoordinator.SetLevel(Scenario.EastCounty,
                    CountySpatialLoadLevel.Warm);
                EnsureCameraAndLight();
                BuildStrategicView();
                BuildCountyDetailView();
                ShowCountyDetailView();
                SetSelectedFacility(
                    DualScaleSpatialValidationScenarioFactory
                        .ArrowTowerFacilityId);
                IsReady = true;
                LastError = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
                return false;
            }
        }

        public void ShowStrategicView()
        {
            _strategicView = true;
            if (_strategicRoot != null) _strategicRoot.SetActive(true);
            if (_detailRoot != null) _detailRoot.SetActive(false);
            SetCamera(new Vector3(40f, 72f, -46f),
                new Vector3(40f, 0f, -40f), 50f);
            RefreshStatusText();
        }

        public void ShowCountyDetailView()
        {
            _strategicView = false;
            if (_strategicRoot != null) _strategicRoot.SetActive(false);
            if (_detailRoot != null) _detailRoot.SetActive(true);
            SetCamera(new Vector3(40f, 70f, -50f),
                new Vector3(40f, 0f, -40f), 47f);
            RefreshOverlays();
            RefreshStatusText();
        }

        public void SetPlanningGridVisible(bool visible)
        {
            _gridVisible = visible;
            RefreshOverlays();
        }

        public void SetFourPortDebugVisible(bool visible)
        {
            _portsVisible = visible;
            RefreshOverlays();
        }

        public void SetHighObserver(bool high)
        {
            _highObserver = high;
            RefreshLosLine();
            RefreshStatusText();
        }

        public void SetGateOpen(bool open)
        {
            var gate = Scenario.WestCounty.Fortifications[
                "fortification.validation.gate.v1"];
            gate.SetGateState(open ? GatePassageStateV1.Open :
                GatePassageStateV1.Closed);
            Scenario.WestCounty.Connections.SetBetween(40, 30,
                PlanningCellDirection.East, gate.PassageKind);
            if (_gateVisual != null)
            {
                _gateVisual.GetComponent<Renderer>().sharedMaterial.color = open
                    ? new Color(0.25f, 0.85f, 0.35f)
                    : new Color(0.9f, 0.52f, 0.12f);
                _gateVisual.transform.localScale = open
                    ? new Vector3(0.12f, 0.35f, 0.82f)
                    : new Vector3(0.18f, 0.9f, 0.92f);
            }
            RefreshStatusText();
        }

        public void SetSelectedFacility(string facilityId)
        {
            var facility = Scenario.Facility(facilityId);
            _selectedFacilityId = facility.Id;
            foreach (var item in _facilityVisuals)
            {
                var renderer = item.Value.GetComponent<Renderer>();
                if (renderer == null) continue;
                var material = renderer.sharedMaterial;
                if (material != null && material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor",
                        string.Equals(item.Key, _selectedFacilityId,
                            StringComparison.Ordinal)
                            ? new Color(0.5f, 0.35f, 0.04f)
                            : Color.black);
                    material.EnableKeyword("_EMISSION");
                }
            }
            RefreshStatusText();
        }

        public void SetLoadDebugVisible(bool visible)
        {
            if (_loadDebug != null) _loadDebug.SetActive(visible);
            RefreshStatusText();
        }

        public void ApplyEvidenceView(
            DualScaleValidationEvidenceView view)
        {
            if (!IsReady && !TryInitialize())
                throw new InvalidOperationException(LastError);
            SetLoadDebugVisible(false);
            SetFourPortDebugVisible(false);
            SetPlanningGridVisible(true);
            switch (view)
            {
                case DualScaleValidationEvidenceView.StrategicTiles:
                    ShowStrategicView();
                    break;
                case DualScaleValidationEvidenceView.PlanningCells:
                    ShowCountyDetailView();
                    SetCamera(new Vector3(40f, 74f, -48f),
                        new Vector3(40f, 0f, -40f), 48f);
                    break;
                case DualScaleValidationEvidenceView.FacilityFootprint:
                    ShowCountyDetailView();
                    SetSelectedFacility(
                        DualScaleSpatialValidationScenarioFactory
                            .StorehouseFacilityId);
                    SetCamera(new Vector3(23f, 25f, -57f),
                        new Vector3(23f, 0f, -51f), 10f);
                    break;
                case DualScaleValidationEvidenceView.FourPortTopology:
                    ShowCountyDetailView();
                    SetFourPortDebugVisible(true);
                    SetCamera(new Vector3(30f, 24f, -43f),
                        new Vector3(30f, 0f, -39f), 12f);
                    break;
                case DualScaleValidationEvidenceView.WallEdgeAndGate:
                    ShowCountyDetailView();
                    SetGateOpen(false);
                    SetCamera(new Vector3(31f, 22f, -44f),
                        new Vector3(31f, 0f, -40f), 12f);
                    break;
                case DualScaleValidationEvidenceView.CountyPortalRoute:
                    ShowCountyDetailView();
                    SetCamera(new Vector3(40f, 23f, -45f),
                        new Vector3(40f, 0f, -40f), 13f);
                    break;
                case DualScaleValidationEvidenceView.HeightAndLosLow:
                    ShowCountyDetailView();
                    SetHighObserver(false);
                    SetCamera(new Vector3(31f, 22f, -43f),
                        new Vector3(31f, 0f, -37f), 15f);
                    break;
                case DualScaleValidationEvidenceView.HeightAndLosHigh:
                    ShowCountyDetailView();
                    SetHighObserver(true);
                    SetCamera(new Vector3(31f, 22f, -43f),
                        new Vector3(31f, 0f, -37f), 15f);
                    break;
                case DualScaleValidationEvidenceView
                    .FacilityGarrisonControl:
                    ShowCountyDetailView();
                    SetSelectedFacility(
                        DualScaleSpatialValidationScenarioFactory
                            .ArrowTowerFacilityId);
                    SetCamera(new Vector3(30f, 20f, -39f),
                        new Vector3(30f, 0f, -35f), 10f);
                    break;
                case DualScaleValidationEvidenceView.HotWarmCold:
                    ShowCountyDetailView();
                    SetLoadDebugVisible(true);
                    SetCamera(new Vector3(40f, 58f, -47f),
                        new Vector3(40f, 0f, -40f), 40f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(view));
            }
        }

        public void CaptureEvidence(string path, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(path) || width <= 0 || height <= 0)
                throw new ArgumentException("Capture target is invalid.");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var target = new RenderTexture(width, height, 24,
                RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var previousTarget = _camera.targetTexture;
            target.Create();
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24,
                false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false);
            File.WriteAllBytes(path, image.EncodeToPNG());
            _camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        private void Update()
        {
            if (!IsReady || _camera == null) return;
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                _lastMousePosition = Input.mousePosition;
            if (Input.GetMouseButton(2))
            {
                var delta = Input.mousePosition - _lastMousePosition;
                var right = _camera.transform.right;
                var forward = Vector3.ProjectOnPlane(
                    _camera.transform.forward, Vector3.up).normalized;
                _cameraFocus += (-right * delta.x - forward * delta.y) *
                                (_camera.orthographicSize / 700f);
                ApplyCameraFocus();
                _lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButton(1))
            {
                var delta = Input.mousePosition - _lastMousePosition;
                _camera.transform.RotateAround(_cameraFocus, Vector3.up,
                    delta.x * 0.2f);
                _lastMousePosition = Input.mousePosition;
            }
            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - wheel * 2f, 5f, 70f);
            if (Input.GetMouseButtonDown(0) && !IsPointerOverToolbar())
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    var marker = hit.collider.GetComponent<
                        DualScaleFacilityVisualMarker>();
                    if (marker != null) SetSelectedFacility(marker.FacilityId);
                }
            }
            if (_statusText != null)
                _statusText.transform.rotation = _camera.transform.rotation;
        }

        private void OnGUI()
        {
            if (!IsReady) return;
            GUILayout.BeginArea(new Rect(14f, 14f, 420f, 310f),
                GUI.skin.box);
            GUILayout.Label("双尺度统一世界 · 50m县域架构验证");
            GUILayout.Label("2km战略Tile ↔ 40×40 PlanningCell · V79未升级");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("天下战略视图")) ShowStrategicView();
            if (GUILayout.Button("县域详细视图")) ShowCountyDetailView();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_gridVisible ? "隐藏50m格" : "显示50m格"))
                SetPlanningGridVisible(!_gridVisible);
            if (GUILayout.Button(_portsVisible ? "隐藏四口" : "显示四口"))
                SetFourPortDebugVisible(!_portsVisible);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("关闭城门")) SetGateOpen(false);
            if (GUILayout.Button("打开城门")) SetGateOpen(true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("低位LOS")) SetHighObserver(false);
            if (GUILayout.Button("高台LOS")) SetHighObserver(true);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("驻留等级 HOT / WARM / COLD"))
                SetLoadDebugVisible(!_loadDebug.activeSelf);
            GUILayout.Label("中键平移 · 右键旋转 · 滚轮缩放 · 左键选Facility");
            var selected = Scenario.Facility(_selectedFacilityId);
            GUILayout.Label($"选中：{selected.DisplayName} / {selected.Id}");
            GUILayout.Label($"耐久：{selected.ConditionBasisPoints}/10000  " +
                            $"Owner：{selected.OwnerId}");
            GUILayout.Label($"Controller：{selected.ControllerId}");
            GUILayout.EndArea();
        }

        private void EnsureCameraAndLight()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
            }
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 500f;
            _light = FindObjectOfType<Light>();
            if (_light == null)
            {
                var lightObject = new GameObject("Validation Sun");
                lightObject.transform.SetParent(transform, false);
                _light = lightObject.AddComponent<Light>();
            }
            _light.type = LightType.Directional;
            _light.intensity = 1.15f;
            _light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private void BuildStrategicView()
        {
            _strategicRoot = new GameObject("STRATEGIC_2KM_VIEW");
            _strategicRoot.transform.SetParent(transform, false);
            for (var row = 0; row < 2; row++)
            for (var column = 0; column < 2; column++)
            {
                var tile = CreateCube($"StrategicTile_{row}_{column}",
                    _strategicRoot.transform,
                    new Vector3(20f + column * 40f, -0.5f,
                        -20f - row * 40f),
                    new Vector3(39.4f, 0.8f, 39.4f),
                    row == 0 && column == 0
                        ? new Color(0.30f, 0.43f, 0.25f)
                        : row == 0 && column == 1
                            ? new Color(0.34f, 0.38f, 0.24f)
                            : row == 1 && column == 0
                                ? new Color(0.24f, 0.38f, 0.32f)
                                : new Color(0.30f, 0.34f, 0.40f));
                DestroySafe(tile.GetComponent<Collider>());
                CreateLabel(tile.transform,
                    $"2km TILE\n({row},{column})", new Vector3(0, 1f, 0),
                    22, Color.white);
            }
            CreateCube("Strategic Route", _strategicRoot.transform,
                new Vector3(40f, 0.2f, -40f),
                new Vector3(79f, 0.25f, 1.1f),
                new Color(0.78f, 0.58f, 0.18f));
            CreateCube("County Boundary", _strategicRoot.transform,
                new Vector3(40f, 0.6f, -40f),
                new Vector3(0.22f, 1.0f, 79f),
                new Color(0.15f, 0.78f, 0.85f));
            CreateLabel(_strategicRoot.transform,
                "天下/州郡：2km战略聚合\n同一Facility / Person / Route",
                new Vector3(40f, 3f, -4f), 30,
                new Color(1f, 0.83f, 0.35f));
        }

        private void BuildCountyDetailView()
        {
            _detailRoot = new GameObject("COUNTY_50M_DETAIL_VIEW");
            _detailRoot.transform.SetParent(transform, false);
            BuildTerrainMesh();
            _gridOverlay = CreateLineObject("PlanningCell50m Grid",
                _detailRoot.transform, BuildGridLines(1),
                new Color(0.42f, 0.58f, 0.48f, 0.75f));
            CreateLineObject("StrategicTile2km Borders",
                _detailRoot.transform, BuildGridLines(40),
                new Color(1f, 0.78f, 0.18f));
            CreateCube("County Boundary", _detailRoot.transform,
                new Vector3(40f, 0.75f, -40f),
                new Vector3(0.15f, 1.5f, 80f),
                new Color(0.12f, 0.86f, 0.95f));
            CreateCube("Official Road", _detailRoot.transform,
                new Vector3(40f, 0.24f, -40.5f),
                new Vector3(80f, 0.18f, 0.64f),
                new Color(0.62f, 0.42f, 0.16f));
            BuildFacilities();
            BuildFortifications();
            BuildPortals();
            BuildPeopleAndArmy();
            _portOverlay = CreateLineObject("Four Port Debug",
                _detailRoot.transform, BuildPortDebugLines(),
                new Color(0.3f, 0.95f, 0.78f));
            _portOverlay.SetActive(false);
            BuildLosObjects();
            BuildLoadDebug();
            _loadDebug.SetActive(false);
            var statusObject = new GameObject("Status Label");
            statusObject.transform.SetParent(_detailRoot.transform, false);
            statusObject.transform.position = new Vector3(22f, 7f, -30f);
            _statusText = statusObject.AddComponent<TextMesh>();
            _statusText.anchor = TextAnchor.MiddleCenter;
            _statusText.alignment = TextAlignment.Center;
            _statusText.fontSize = 28;
            _statusText.characterSize = 0.16f;
            _statusText.color = new Color(1f, 0.9f, 0.5f);
            RefreshLosLine();
        }

        private void BuildTerrainMesh()
        {
            var vertices = new Vector3[81 * 81];
            var triangles = new int[80 * 80 * 6];
            for (var row = 0; row <= 80; row++)
            for (var column = 0; column <= 80; column++)
            {
                var sampleRow = Mathf.Clamp(row, 0, 79);
                var sampleColumn = Mathf.Clamp(column, 0, 79);
                vertices[row * 81 + column] = new Vector3(column,
                    Height(sampleRow, sampleColumn), -row);
            }
            var triangle = 0;
            for (var row = 0; row < 80; row++)
            for (var column = 0; column < 80; column++)
            {
                var index = row * 81 + column;
                triangles[triangle++] = index;
                triangles[triangle++] = index + 81;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 81;
                triangles[triangle++] = index + 82;
            }
            var mesh = new Mesh { name = "Packed 6400 PlanningCell Terrain" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var terrainObject = new GameObject("Packed Terrain Chunk Mesh");
            terrainObject.transform.SetParent(_detailRoot.transform, false);
            terrainObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrainObject.AddComponent<MeshRenderer>().sharedMaterial =
                Material(new Color(0.24f, 0.36f, 0.20f));
        }

        private void BuildFacilities()
        {
            foreach (var facility in Scenario.World.Facilities.OrderBy(
                         item => item.Id, StringComparer.Ordinal))
            {
                var placement = Scenario.Placement(facility.Id);
                var cell = Scenario.Projection.ToPlanningCell(
                    placement.Center);
                var local = LocalCell(cell);
                var color = facility.Id.Contains("arrow-tower")
                    ? new Color(0.82f, 0.24f, 0.15f)
                    : facility.Id.Contains("watchtower")
                        ? new Color(0.75f, 0.50f, 0.16f)
                        : facility.Id.Contains("siege-platform")
                            ? new Color(0.45f, 0.25f, 0.12f)
                            : facility.Id.Contains("storehouse")
                                ? new Color(0.52f, 0.46f, 0.30f)
                                : new Color(0.68f, 0.58f, 0.38f);
                var width = placement.WidthCentimetres / 5_000f;
                var depth = placement.DepthCentimetres / 5_000f;
                var height = Mathf.Max(0.55f,
                    placement.StructureHeightCentimetres / 1_000f);
                var visual = CreateCube(facility.DisplayName,
                    _detailRoot.transform,
                    new Vector3(local.x, HeightAt(cell) + height * 0.5f,
                        local.z), new Vector3(width, height, depth), color);
                visual.transform.rotation = Quaternion.Euler(0f,
                    placement.RotationQuarterTurns * 90f, 0f);
                visual.AddComponent<DualScaleFacilityVisualMarker>()
                    .FacilityId = facility.Id;
                _facilityVisuals.Add(facility.Id, visual);
                CreateLabel(visual.transform, facility.DisplayName,
                    new Vector3(0f, 0.7f, 0f), 22, Color.white);
                foreach (var entrance in placement.Entrances)
                {
                    var entranceCell = Scenario.Projection.ToPlanningCell(
                        entrance.Position);
                    var entranceLocal = LocalCell(entranceCell);
                    CreateCube("Entrance", _detailRoot.transform,
                        new Vector3(entranceLocal.x,
                            HeightAt(entranceCell) + 0.12f,
                            entranceLocal.z),
                        new Vector3(0.24f, 0.2f, 0.24f),
                        new Color(0.2f, 0.95f, 0.55f));
                }
            }
        }

        private void BuildFortifications()
        {
            foreach (var item in Scenario.WestCounty.Fortifications.Values
                         .OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var first = LocalCell(item.Edge.First);
                var second = LocalCell(item.Edge.Second);
                var center = (first + second) * 0.5f;
                var height = item.HeightCentimetres / 1_000f;
                var visual = CreateCube(item.Id, _detailRoot.transform,
                    new Vector3(center.x, HeightAt(item.Edge.First) +
                        height * 0.5f, center.z),
                    Mathf.Abs(first.x - second.x) > 0.1f
                        ? new Vector3(0.18f, height, 0.9f)
                        : new Vector3(0.9f, height, 0.18f),
                    item.IsGate ? new Color(0.9f, 0.52f, 0.12f) :
                    new Color(0.56f, 0.48f, 0.35f));
                DestroySafe(visual.GetComponent<Collider>());
                if (item.IsGate) _gateVisual = visual;
            }
        }

        private void BuildPortals()
        {
            foreach (var portal in Scenario.Route.Portals)
            {
                var position = LocalCell(portal.Cell);
                var visual = CreateCube(portal.PortalId,
                    _detailRoot.transform,
                    new Vector3(position.x, HeightAt(portal.Cell) + 0.9f,
                        position.z), new Vector3(0.45f, 1.8f, 0.45f),
                    new Color(0.15f, 0.85f, 0.95f));
                CreateLabel(visual.transform, "PORTAL",
                    new Vector3(0f, 0.8f, 0f), 20,
                    new Color(0.35f, 1f, 1f));
            }
        }

        private void BuildPeopleAndArmy()
        {
            var personCell = Scenario.Projection.ToPlanningCell(
                Scenario.PersonSpatial.LocalPosition);
            var personPosition = LocalCell(personCell);
            var person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.name = "Single World Person";
            person.transform.SetParent(_detailRoot.transform, false);
            person.transform.position = new Vector3(personPosition.x,
                HeightAt(personCell) + 0.35f, personPosition.z);
            person.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            person.GetComponent<Renderer>().sharedMaterial = Material(
                new Color(0.92f, 0.82f, 0.25f));

            var army = CreateCube("Single World Army", _detailRoot.transform,
                new Vector3(43f, Height(40, 43) + 0.5f, -40.5f),
                new Vector3(0.8f, 1f, 0.8f),
                new Color(0.75f, 0.12f, 0.14f));
            CreateLabel(army.transform, "ARMY",
                new Vector3(0f, 0.8f, 0f), 20, Color.white);
        }

        private void BuildLosObjects()
        {
            _targetMarker = CreateCube("LOS Target", _detailRoot.transform,
                new Vector3(37.5f, Height(37, 37) + 0.7f, -37.5f),
                new Vector3(0.6f, 1.4f, 0.6f),
                new Color(0.85f, 0.15f, 0.18f));
            CreateLabel(_targetMarker.transform, "TARGET",
                new Vector3(0f, 0.9f, 0f), 18, Color.white);
            _losLine = CreateLineObject("Height LOS",
                _detailRoot.transform, new List<Vector3>(), Color.red);
        }

        private void BuildLoadDebug()
        {
            _loadDebug = new GameObject("HOT_WARM_COLD_DEBUG");
            _loadDebug.transform.SetParent(_detailRoot.transform, false);
            var labels = new[] { "HOT\n完整50m缓存", "WARM\n门户/主路",
                "COLD\nHeader/Due" };
            var colors = new[] { new Color(0.18f, 0.82f, 0.30f),
                new Color(0.92f, 0.65f, 0.14f),
                new Color(0.30f, 0.42f, 0.58f) };
            for (var index = 0; index < 3; index++)
            {
                var block = CreateCube(labels[index], _loadDebug.transform,
                    new Vector3(16f + index * 12f, 4f, -65f),
                    new Vector3(9f, 0.8f, 6f), colors[index]);
                CreateLabel(block.transform, labels[index],
                    new Vector3(0f, 1.5f, 0f), 26, Color.white);
            }
        }

        private List<Vector3> BuildGridLines(int step)
        {
            var lines = new List<Vector3>();
            for (var index = 0; index <= 80; index += step)
            {
                lines.Add(new Vector3(index, 0.06f, 0f));
                lines.Add(new Vector3(index, 0.06f, -80f));
                lines.Add(new Vector3(0f, 0.06f, -index));
                lines.Add(new Vector3(80f, 0.06f, -index));
            }
            return lines;
        }

        private List<Vector3> BuildPortDebugLines()
        {
            var lines = new List<Vector3>();
            for (var row = 34; row <= 44; row += 2)
            for (var column = 26; column <= 36; column += 2)
            {
                var center = new Vector3(column + 0.5f,
                    Height(row, column) + 0.25f, -row - 0.5f);
                lines.Add(center); lines.Add(center + Vector3.forward * 0.34f);
                lines.Add(center); lines.Add(center + Vector3.right * 0.34f);
                lines.Add(center); lines.Add(center + Vector3.back * 0.34f);
                lines.Add(center); lines.Add(center + Vector3.left * 0.34f);
            }
            return lines;
        }

        private void RefreshLosLine()
        {
            if (_losLine == null || Scenario == null) return;
            var platform = Scenario.Placement(
                DualScaleSpatialValidationScenarioFactory
                    .SiegePlatformFacilityId);
            var targetCell = Scenario.WestCounty.ToGlobalCell(37, 37);
            var targetGlobal = Scenario.Projection.PlanningCellCenter(
                targetCell);
            var observer = new EffectiveElevationSample(platform.Center,
                10_000,
                _highObserver ? platform.StructureHeightCentimetres : 0,
                200);
            var target = new EffectiveElevationSample(targetGlobal,
                10_000, 1_000, 180);
            var wallGlobal = Scenario.Projection.PlanningCellCenter(
                Scenario.WestCounty.ToGlobalCell(37, 30));
            var wall = new SpatialOccluderV1(
                "fortification.validation.wall.los.v1",
                wallGlobal.EastingMetres + 24d,
                wallGlobal.EastingMetres + 26d,
                Math.Min(platform.Center.NorthingMetres,
                    targetGlobal.NorthingMetres) - 5d,
                Math.Max(platform.Center.NorthingMetres,
                    targetGlobal.NorthingMetres) + 5d,
                11_200);
            var visible = new SpatialLineOfSightQueryV1().HasLineOfSight(
                observer, target, new[] { wall }, out _);
            _currentLosVisible = visible;
            var startCell = Scenario.Projection.ToPlanningCell(
                platform.Center);
            var start = LocalCell(startCell) + Vector3.up *
                (_highObserver ? 3.0f : 0.7f);
            var end = LocalCell(targetCell) + Vector3.up * 0.9f;
            var mesh = _losLine.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            mesh.SetVertices(new List<Vector3> { start, end });
            mesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);
            _losLine.GetComponent<Renderer>().sharedMaterial.color = visible
                ? new Color(0.20f, 1f, 0.35f)
                : new Color(1f, 0.15f, 0.12f);
            _targetMarker.GetComponent<Renderer>().sharedMaterial.color = visible
                ? new Color(0.20f, 0.9f, 0.30f)
                : new Color(0.85f, 0.15f, 0.18f);
        }

        private void RefreshOverlays()
        {
            if (_gridOverlay != null)
                _gridOverlay.SetActive(_gridVisible && !_strategicView);
            if (_portOverlay != null)
                _portOverlay.SetActive(_portsVisible && !_strategicView);
        }

        private void RefreshStatusText()
        {
            if (_statusText == null || Scenario == null) return;
            var gate = Scenario.WestCounty.Fortifications[
                "fortification.validation.gate.v1"];
            var facility = Scenario.Facility(_selectedFacilityId ??
                DualScaleSpatialValidationScenarioFactory
                    .ArrowTowerFacilityId);
            _statusText.text = _strategicView
                ? "2km战略聚合 · 世界事实只有一份"
                : $"50m县域分区 · 6400格 / 0个Cell GameObject\n" +
                  $"Gate={gate.GateState}  LOS=" +
                  (_highObserver ? "HIGH" : "LOW") + "\n" +
                  $"Facility={facility.DisplayName}  " +
                  $"Durability={facility.ConditionBasisPoints}  " +
                  $"Garrison={Scenario.ArrowTowerDefense.GarrisonCount}  " +
                  $"Controller={facility.ControllerId}";
        }

        private float Height(int overallRow, int overallColumn)
        {
            var partition = overallColumn < 40
                ? Scenario.WestCounty : Scenario.EastCounty;
            var localColumn = overallColumn < 40
                ? overallColumn : overallColumn - 40;
            return (partition.GroundElevationDecimetres(overallRow,
                localColumn) - 1_000) * 0.02f;
        }

        private float HeightAt(PlanningCellCoord cell)
        {
            var min = Scenario.WestCounty.MinimumCell;
            return Height(cell.Row - min.Row, cell.Column - min.Column);
        }

        private Vector3 LocalCell(PlanningCellCoord cell)
        {
            var min = Scenario.WestCounty.MinimumCell;
            return new Vector3(cell.Column - min.Column + 0.5f,
                HeightAt(cell), -(cell.Row - min.Row + 0.5f));
        }

        private GameObject CreateCube(string name, Transform parent,
            Vector3 position, Vector3 scale, Color color)
        {
            var value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = Material(color);
            return value;
        }

        private GameObject CreateLineObject(string name, Transform parent,
            List<Vector3> vertices, Color color)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name + " Mesh" };
            mesh.SetVertices(vertices);
            var indices = Enumerable.Range(0, vertices.Count).ToArray();
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial =
                Material(color);
            return value;
        }

        private Material Material(Color color)
        {
            var shader = Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard") ??
                         Shader.Find("Sprites/Default");
            var value = new Material(shader) { color = color };
            _materials.Add(value);
            return value;
        }

        private static void CreateLabel(Transform parent, string text,
            Vector3 localPosition, int fontSize, Color color)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.1f;
            mesh.color = color;
        }

        private void SetCamera(Vector3 position, Vector3 focus,
            float orthographicSize)
        {
            _cameraFocus = focus;
            _camera.transform.position = position;
            _camera.transform.LookAt(focus);
            _camera.orthographicSize = orthographicSize;
        }

        private void ApplyCameraFocus()
        {
            var distance = Vector3.Distance(_camera.transform.position,
                _cameraFocus);
            _camera.transform.position = _cameraFocus -
                _camera.transform.forward * distance;
        }

        private static bool IsPointerOverToolbar() =>
            Input.mousePosition.x < 440f &&
            Screen.height - Input.mousePosition.y < 330f;

        private static void DestroySafe(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
