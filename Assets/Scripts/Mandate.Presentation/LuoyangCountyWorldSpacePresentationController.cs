using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
    /// <summary>
    /// Disposable, read-only 2.5D rendering cache for the authoritative
    /// Luoyang 50 m county layout. No object in this hierarchy is a world
    /// entity and none is serialized into a WorldState snapshot.
    /// </summary>
    public sealed class LuoyangCountyWorldSpacePresentationController :
        MonoBehaviour
    {
        private const float HeightScale = 0.060f;
        private const float SurfaceLift = 0.035f;
        private const float CameraPitch = 49f;
        private const float CameraFieldOfView = 27f;
        private const int NearRefreshCellDistance = 8;

        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private readonly List<Material> _ownedMaterials = new List<Material>();
        private readonly Dictionary<string, Transform> _layers =
            new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<CountyRoadPresentationClass, GameObject>
            _roadLodRoots =
                new Dictionary<CountyRoadPresentationClass, GameObject>();
        private readonly Dictionary<string, Material> _categoryMaterials =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        private LuoyangCountyPlanningPresentationController _source;
        private CountyWorldSpacePresentationPlan _plan;
        private GameObject _worldRoot;
        private GameObject _farFacilityRoot;
        private GameObject _citywideBuildingLanguageRoot;
        private GameObject _farLandmarkRoot;
        private GameObject _midFacilityRoot;
        private GameObject _nearFacilityRoot;
        private GameObject _goldenBlockRoot;
        private GameObject _strategicWallRoot;
        private GameObject _fortificationDetailRoot;
        private GameObject _fallbackEvidenceRoot;
        private GameObject _planningGridRoot;
        private GameObject _ghostRoot;
        private GameObject _draftRoot;
        private GameObject _selectionRoot;
        private GameObject _debugRoot;
        private Camera _camera;
        private HanBuildableFacilityModelFactory _modelFactory;
        private LuoyangFacilityModelBindingResolver _modelResolver;
        private Material _terrainMaterial;
        private Material _waterMaterial;
        private Material _canalMaterial;
        private Material _roadMaterial;
        private Material _roadShoulderMaterial;
        private Material _wallMaterial;
        private Material _gateMaterial;
        private Material _fieldMaterial;
        private Material _fallbackMaterial;
        private Material _midClusterMaterial;
        private Material _vegetationMaterial;
        private Material _gridMaterial;
        private Material _gridHoverMaterial;
        private Material _gridSelectedMaterial;
        private Material _validGhostMaterial;
        private Material _warningGhostMaterial;
        private Material _invalidGhostMaterial;
        private Material _draftRoadMaterial;
        private Material _draftWallMaterial;
        private Material _draftCanalMaterial;
        private Rect _lastGuiViewport;
        private float _minimumElevationMetres;
        private float _maximumElevationMetres;
        private int _lastNearRow = int.MinValue;
        private int _lastNearColumn = int.MinValue;
        private int _lastPlanningSignature = int.MinValue;
        private int _lastSelectionSignature = int.MinValue;
        private CountyMapPresentationLod _lastLod =
            (CountyMapPresentationLod)byte.MaxValue;
        private bool _debugVisible;
        private bool _built;

        public bool IsBuilt => _built;
        public bool IsVisible => _worldRoot != null && _worldRoot.activeSelf;
        public bool DebugVisible => _debugVisible;
        public string CacheKey => _plan?.CacheKey ?? string.Empty;
        public int CacheBuildCount { get; private set; }
        public int DetailedFacilityObjectCount => _nearFacilityRoot == null
            ? 0
            : _nearFacilityRoot.GetComponentsInChildren<
                HanBuildableFacilityModelInstance>(true).Length;
        public int PlanningGridGameObjectCount => _planningGridRoot == null
            ? 0
            : _planningGridRoot.transform.childCount;
        public int FarOrdinaryFacilityDetailObjectCount => 0;
        public int FarAggregateRendererCount => _farFacilityRoot == null
            ? 0
            : _farFacilityRoot.GetComponentsInChildren<MeshRenderer>(true)
                .Length;
        public int CitywideBuildingLanguageRendererCount =>
            _citywideBuildingLanguageRoot == null ? 0 :
                _citywideBuildingLanguageRoot.GetComponentsInChildren<
                    MeshRenderer>(true).Length;
        public int CitywideStyledFacilityCount =>
            CitywideBuildingLanguagePlan?.Entries.Count ?? 0;
        public int CitywideContextFacilityCount { get; private set; }
        public int CitywideBuildingLanguageModuleCount { get; private set; }
        public int CitywideBuildingLanguageTriangleCount { get; private set; }
        public int CitywideBuildingLanguageMaterialCount { get; private set; }
        public int GoldenBlockRendererCount => _goldenBlockRoot == null
            ? 0
            : _goldenBlockRoot.GetComponentsInChildren<MeshRenderer>(true)
                .Length;
        public int GoldenBlockVisibleModuleCount { get; private set; }
        public int GoldenBlockPropCount { get; private set; }
        public int GoldenBlockVegetationInstanceCount { get; private set; }
        public int GoldenBlockTriangleCount { get; private set; }
        public int GoldenBlockMaterialCount { get; private set; }
        public string CurrentGhostPresentationProfileId { get; private set; }
            = string.Empty;
        public CountyGoldenBlockPresentationPlan GoldenBlockPlan {
            get; private set;
        }
        public CountyCitywideBuildingLanguagePlan CitywideBuildingLanguagePlan
            { get; private set; }
        public CountyWorldSpacePresentationSummary Summary { get; private set; }
        public double LastBuildMilliseconds { get; private set; }
        public double LastWarmEnterMilliseconds { get; private set; }
        public float MinimumElevationMetres => _minimumElevationMetres;
        public float MaximumElevationMetres => _maximumElevationMetres;
        public Transform WorldRoot => _worldRoot == null
            ? null
            : _worldRoot.transform;
        public int RendererCount => _worldRoot == null
            ? 0
            : _worldRoot.GetComponentsInChildren<MeshRenderer>(true).Length;

        public void Initialize(
            LuoyangCountyPlanningPresentationController source,
            Camera presentationCamera)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _camera = presentationCamera ?? throw new ArgumentNullException(
                nameof(presentationCamera));
            if (!_source.IsReady || _source.PresentationStack == null ||
                _source.LayoutPackage == null || _source.Partition == null)
                throw new InvalidOperationException(
                    "The county presentation source is not ready.");

            var cacheKey = _source.LayoutFingerprint + ":" +
                           CountyWorldSpacePresentationPlan.Version;
            if (_built && string.Equals(CacheKey, cacheKey,
                    StringComparison.Ordinal))
                return;
            ReleasePresentation();
            _source = source;
            _camera = presentationCamera;
            _plan = new CountyWorldSpacePresentationPlan(
                source.LayoutPackage, source.Partition,
                source.PresentationStack);
            GoldenBlockPlan = new CountyGoldenBlockPresentationPlan(
                source.LayoutPackage);
            CitywideBuildingLanguagePlan =
                new CountyCitywideBuildingLanguagePlan(source.LayoutPackage,
                    _plan.FarLandmarks.Select(item => item.FacilityId));
            BuildPresentation();
        }

        public void Show(Rect guiViewport)
        {
            if (!_built) throw new InvalidOperationException(
                "County world-space presentation has not been built.");
            var watch = Stopwatch.StartNew();
            _lastGuiViewport = guiViewport;
            _worldRoot.SetActive(true);
            ApplyCameraViewport(guiViewport);
            Synchronize();
            watch.Stop();
            LastWarmEnterMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        public void Hide()
        {
            if (_worldRoot != null) _worldRoot.SetActive(false);
            if (_camera != null) _camera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        private void LateUpdate()
        {
            // Unity can preserve the generated hierarchy while clearing the
            // non-serialized runtime source during an in-Play-Mode domain
            // reload. The owning planning controller rebuilds those
            // references on the next clean entry; until then this component
            // must stay inert instead of producing one exception per frame.
            if (!_built || !IsVisible || _source == null || _plan == null ||
                _camera == null) return;

            // DrawMap is invoked from IMGUI, after the normal camera render
            // loop.  Applying the county camera only there lets the shared
            // strategic camera keep its previous pose, viewport and fog for
            // the frame that is actually rendered.  Reassert ownership in
            // LateUpdate so the 50 m world is configured before culling and
            // rendering, including editor-driven evidence captures.
            ApplyCameraPose();
        }

        public void SetDebugVisible(bool visible)
        {
            _debugVisible = visible;
            if (_debugRoot != null) _debugRoot.SetActive(visible);
        }

        public void SetFallbackEvidenceVisible(bool visible)
        {
            if (_fallbackEvidenceRoot == null) return;
            ClearChildren(_fallbackEvidenceRoot.transform);
            _fallbackEvidenceRoot.SetActive(visible);
            if (!visible) return;
            var fallback = new CountyMeshAccumulator();
            var center = CellCenter(_source.SelectedLocalRow,
                _source.SelectedLocalColumn, SurfaceLift * 3f);
            fallback.AddBox(center + Vector3.up * 0.42f,
                new Vector3(0.82f, 0.84f, 0.64f));
            fallback.AddBox(center + Vector3.up * 0.91f,
                new Vector3(0.68f, 0.14f, 0.72f));
            CreateAccumulatorRenderer("Fallback Contract Evidence Proxy",
                _fallbackEvidenceRoot.transform, fallback,
                _fallbackMaterial, true);
        }

        public bool TryGuiPointToCell(Rect guiViewport, Vector2 guiPoint,
            out int row, out int column)
        {
            row = -1;
            column = -1;
            if (!_built || _camera == null || !guiViewport.Contains(guiPoint))
                return false;
            var screen = new Vector3(guiPoint.x,
                Screen.height - guiPoint.y, 0f);
            var ray = _camera.ScreenPointToRay(screen);
            var point = RayPlaneIntersection(ray, 0f);
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var candidateRow = _source.Partition.Rows * 0.5f - point.z;
                var candidateColumn = point.x +
                                      _source.Partition.Columns * 0.5f;
                if (candidateRow < -1f ||
                    candidateRow > _source.Partition.Rows + 1f ||
                    candidateColumn < -1f ||
                    candidateColumn > _source.Partition.Columns + 1f)
                    return false;
                point = RayPlaneIntersection(ray,
                    WorldHeight(candidateRow, candidateColumn));
            }
            row = Mathf.Clamp(Mathf.FloorToInt(
                    _source.Partition.Rows * 0.5f - point.z), 0,
                _source.Partition.Rows - 1);
            column = Mathf.Clamp(Mathf.FloorToInt(
                    point.x + _source.Partition.Columns * 0.5f), 0,
                _source.Partition.Columns - 1);
            return true;
        }

        public float WorldHeight(float row, float column) =>
            (_plan.SurfaceHeight(row, column) - _minimumElevationMetres) *
            HeightScale;

        public Vector3 CellCenter(int row, int column, float lift = 0f) =>
            new Vector3(column + 0.5f -
                        _source.Partition.Columns * 0.5f,
                WorldHeight(row, column) + lift,
                _source.Partition.Rows * 0.5f - row - 0.5f);

        public void Synchronize()
        {
            if (!_built || !IsVisible) return;
            ApplyCameraPose();
            ApplyLayerVisibility();
            RefreshNearDetailsIfNeeded();
            RefreshSelectionIfNeeded();
            RefreshPlanningPresentationIfNeeded();
        }

        private void BuildPresentation()
        {
            var watch = Stopwatch.StartNew();
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=start");
            ResolveElevationRange();
            CreateMaterials();
            CreateHierarchy();
            CreateLighting();
            InitializeModelSystem();
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=model-system");
            BuildTerrain();
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=terrain");
            BuildWater();
            BuildRoads();
            BuildFortifications();
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=infrastructure");
            BuildFacilityAggregates();
            BuildCitywideBuildingLanguage();
            BuildGoldenBlockPrototype();
            BuildFarLandmarkModels();
            BuildModelFacilityBatches();
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=facilities");
            BuildAgriculture();
            BuildVegetation();
            BuildCountyBoundary();
            BuildDebugGeometry();
            Summary = _plan.CreateSummary(ModelCanResolve);
            _built = true;
            CacheBuildCount++;
            _worldRoot.SetActive(false);
            watch.Stop();
            LastBuildMilliseconds = watch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log("LUOYANG_COUNTY_WORLDSPACE_BUILD stage=complete ms=" +
                      LastBuildMilliseconds.ToString("0.0"));
        }

        private void CreateHierarchy()
        {
            _worldRoot = new GameObject(
                "Luoyang County Strategic Sandbox Presentation V2");
            _worldRoot.transform.SetParent(transform, false);
            foreach (var name in new[]
                     {
                         "Terrain", "Water", "Roads", "Fortifications",
                         "Urban Fabric", "Facilities", "Villages",
                         "Agriculture", "Vegetation", "Planning Overlay",
                         "Selection", "Debug"
                     })
                _layers.Add(name, NewChild(name, _worldRoot.transform));
            _farFacilityRoot = NewChild("Far Aggregates",
                _layers["Urban Fabric"]).gameObject;
            _citywideBuildingLanguageRoot = NewChild(
                "Citywide Five-Family Building Language V1",
                _layers["Urban Fabric"]).gameObject;
            _goldenBlockRoot = NewChild("Luoyang Golden Block V2",
                _layers["Urban Fabric"]).gameObject;
            _farLandmarkRoot = NewChild("Far Landmark Models",
                _layers["Facilities"]).gameObject;
            _midFacilityRoot = NewChild("Mid Model Batches",
                _layers["Facilities"]).gameObject;
            _nearFacilityRoot = NewChild("Near Detailed Models",
                _layers["Facilities"]).gameObject;
            _strategicWallRoot = NewChild("Strategic City Wall Outline",
                _layers["Fortifications"]).gameObject;
            _fortificationDetailRoot = NewChild("Detailed Wall Segments",
                _layers["Fortifications"]).gameObject;
            _fallbackEvidenceRoot = NewChild("Fallback Evidence Proxy",
                _layers["Facilities"]).gameObject;
            _fallbackEvidenceRoot.SetActive(false);
            _planningGridRoot = NewChild("Local 50m Grid",
                _layers["Planning Overlay"]).gameObject;
            _ghostRoot = NewChild("Building Ghost",
                _layers["Planning Overlay"]).gameObject;
            _draftRoot = NewChild("Draft Geometry",
                _layers["Planning Overlay"]).gameObject;
            _selectionRoot = _layers["Selection"].gameObject;
            _debugRoot = _layers["Debug"].gameObject;
            _debugRoot.SetActive(false);
        }

        private static Transform NewChild(string name, Transform parent)
        {
            var value = new GameObject(name).transform;
            value.SetParent(parent, false);
            return value;
        }

        private void CreateLighting()
        {
            var lightRoot = NewChild("County Lighting", _worldRoot.transform);
            var key = new GameObject("Warm Directional Key");
            key.transform.SetParent(lightRoot, false);
            key.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.88f, 0.68f);
            keyLight.intensity = 0.92f;
            keyLight.shadows = LightShadows.Soft;
            var fill = new GameObject("Cool Directional Fill");
            fill.transform.SetParent(lightRoot, false);
            fill.transform.rotation = Quaternion.Euler(62f, 145f, 0f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.54f, 0.65f, 0.62f);
            fillLight.intensity = 0.32f;
            fillLight.shadows = LightShadows.None;
        }

        private void CreateMaterials()
        {
            var terrainShader = Shader.Find(
                "Mandate/County World Space Terrain V1") ??
                                Shader.Find("Diffuse");
            _terrainMaterial = Own(new UnityEngine.Material(terrainShader)
            {
                name = "County Terrain Shared V1"
            });
            _waterMaterial = Material("River Water", new Color(
                0.12f, 0.37f, 0.47f), 0.78f, true);
            _canalMaterial = Material("Canal Water", new Color(
                0.18f, 0.48f, 0.53f), 0.88f, true);
            _roadShoulderMaterial = Material("Road Shoulder", new Color(
                0.28f, 0.22f, 0.14f), 1f);
            _roadMaterial = Material("Compacted Earth Road", new Color(
                0.56f, 0.43f, 0.25f), 1f);
            _wallMaterial = Material("Rammed Earth Wall", new Color(
                0.48f, 0.34f, 0.21f), 1f);
            _gateMaterial = Material("Gatehouse Timber", new Color(
                0.34f, 0.16f, 0.10f), 1f);
            _fieldMaterial = Material("Derived Field Patches", new Color(
                0.48f, 0.49f, 0.20f), 1f);
            _fallbackMaterial = Material("Deterministic Facility Proxy",
                new Color(0.55f, 0.38f, 0.22f), 1f);
            _midClusterMaterial = Material("Mid Facility Cluster",
                new Color(0.47f, 0.30f, 0.18f), 1f);
            _vegetationMaterial = Material("Vegetation Batch", new Color(
                0.18f, 0.31f, 0.16f), 1f);
            _gridMaterial = Material("Local Planning Grid", new Color(
                0.72f, 0.66f, 0.43f), 0.32f, true);
            _gridHoverMaterial = Material("Planning Cell Hover", new Color(
                0.96f, 0.83f, 0.33f), 0.74f, true);
            _gridSelectedMaterial = Material("Planning Cell Selected",
                new Color(1f, 0.92f, 0.60f), 0.92f, true);
            _validGhostMaterial = Material("Valid Building Ghost", new Color(
                0.20f, 0.86f, 0.40f), 0.48f, true);
            _warningGhostMaterial = Material("Warning Building Ghost", new Color(
                0.95f, 0.65f, 0.12f), 0.52f, true);
            _invalidGhostMaterial = Material("Invalid Building Ghost", new Color(
                0.88f, 0.16f, 0.10f), 0.52f, true);
            _draftRoadMaterial = Material("Road Draft", new Color(
                0.12f, 0.82f, 0.88f), 0.62f, true);
            _draftWallMaterial = Material("Wall Draft", new Color(
                0.26f, 0.72f, 0.92f), 0.48f, true);
            _draftCanalMaterial = Material("Canal Draft", new Color(
                0.16f, 0.67f, 0.82f), 0.56f, true);
        }

        private Material Material(string name, Color color, float alpha,
            bool transparent = false)
        {
            color.a = alpha;
            var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var result = Own(new UnityEngine.Material(shader)
                { name = name, color = color });
            if (transparent && result.HasProperty("_Mode"))
            {
                result.SetFloat("_Mode", 3f);
                result.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                result.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                result.SetInt("_ZWrite", 0);
                result.DisableKeyword("_ALPHATEST_ON");
                result.EnableKeyword("_ALPHABLEND_ON");
                result.renderQueue = 3000;
            }
            return result;
        }

        private Material Own(Material material)
        {
            _ownedMaterials.Add(material);
            return material;
        }

        private void ResolveElevationRange()
        {
            _minimumElevationMetres = float.MaxValue;
            _maximumElevationMetres = float.MinValue;
            for (var row = 0; row < _source.Partition.Rows; row += 4)
            for (var column = 0; column < _source.Partition.Columns; column += 4)
            {
                var elevation = _source.Partition.GroundElevationDecimetres(
                    row, column) / 10f;
                _minimumElevationMetres = Mathf.Min(_minimumElevationMetres,
                    elevation);
                _maximumElevationMetres = Mathf.Max(_maximumElevationMetres,
                    elevation);
            }
            if (_minimumElevationMetres == float.MaxValue)
                _minimumElevationMetres = _maximumElevationMetres = 0f;
        }

        private void BuildTerrain()
        {
            var parent = _layers["Terrain"];
            var chunkSize = CountyWorldSpacePresentationPlan.TerrainChunkCells;
            var step = CountyWorldSpacePresentationPlan.TerrainSampleStepCells;
            for (var startRow = 0; startRow < _source.Partition.Rows;
                 startRow += chunkSize)
            for (var startColumn = 0; startColumn < _source.Partition.Columns;
                 startColumn += chunkSize)
            {
                var endRow = Mathf.Min(_source.Partition.Rows,
                    startRow + chunkSize);
                var endColumn = Mathf.Min(_source.Partition.Columns,
                    startColumn + chunkSize);
                var rows = (endRow - startRow) / step;
                var columns = (endColumn - startColumn) / step;
                var vertices = new List<Vector3>((rows + 1) * (columns + 1));
                var colors = new List<Color>((rows + 1) * (columns + 1));
                var triangles = new List<int>(rows * columns * 6);
                for (var localRow = 0; localRow <= rows; localRow++)
                for (var localColumn = 0; localColumn <= columns; localColumn++)
                {
                    var row = Mathf.Min(endRow - 1,
                        startRow + localRow * step);
                    var column = Mathf.Min(endColumn - 1,
                        startColumn + localColumn * step);
                    vertices.Add(new Vector3(
                        startColumn + localColumn * step -
                        _source.Partition.Columns * 0.5f,
                        WorldHeight(row, column),
                        _source.Partition.Rows * 0.5f -
                        (startRow + localRow * step)));
                    colors.Add(SurfaceColor(_plan.SurfaceClass(row, column)));
                }
                for (var row = 0; row < rows; row++)
                for (var column = 0; column < columns; column++)
                {
                    var first = row * (columns + 1) + column;
                    CountyWorldSpacePresentationPlan
                        .AppendUpwardTerrainQuadTriangles(triangles, first,
                            columns);
                }
                var mesh = OwnMesh(new Mesh
                {
                    name = $"County Terrain {startRow / chunkSize}-" +
                           $"{startColumn / chunkSize}",
                    indexFormat = IndexFormat.UInt32
                });
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                CreateRenderer(mesh.name, parent, mesh, _terrainMaterial, true);
            }
        }

        private static Color SurfaceColor(CountySurfaceVisualClass value)
        {
            switch (value)
            {
                case CountySurfaceVisualClass.Hill:
                    return new Color(0.31f, 0.39f, 0.22f);
                case CountySurfaceVisualClass.Forest:
                    return new Color(0.19f, 0.33f, 0.18f);
                case CountySurfaceVisualClass.Farmland:
                    return new Color(0.51f, 0.49f, 0.23f);
                case CountySurfaceVisualClass.BuiltUp:
                    return new Color(0.47f, 0.42f, 0.31f);
                case CountySurfaceVisualClass.Waterside:
                    return new Color(0.30f, 0.45f, 0.30f);
                case CountySurfaceVisualClass.Water:
                    return new Color(0.18f, 0.38f, 0.40f);
                default:
                    return new Color(0.43f, 0.50f, 0.28f);
            }
        }

        private void BuildWater()
        {
            var river = new CountyMeshAccumulator();
            for (var row = 0; row < _source.Partition.Rows; row++)
            for (var column = 0; column < _source.Partition.Columns; column++)
            {
                if (_source.Partition.WaterState(row, column) == 0) continue;
                var center = CellCenter(row, column, SurfaceLift * 2f);
                river.AddHorizontalQuad(center, 1.04f, 1.04f);
            }
            CreateAccumulatorRenderer("River Surface", _layers["Water"],
                river, _waterMaterial, false);

            var canal = new CountyMeshAccumulator();
            foreach (var edge in _source.LayoutPackage.CanalEdges.OrderBy(
                         item => item.EdgeId, StringComparer.Ordinal))
                AddSurfaceRibbon(canal, edge.FromLocalRow,
                    edge.FromLocalColumn, edge.ToLocalRow,
                    edge.ToLocalColumn, 0.18f, SurfaceLift * 3f);
            CreateAccumulatorRenderer("Canal Ribbons", _layers["Water"],
                canal, _canalMaterial, false);
        }

        private void BuildRoads()
        {
            foreach (CountyRoadPresentationClass roadClass in Enum.GetValues(
                         typeof(CountyRoadPresentationClass)))
            {
                var root = NewChild(roadClass.ToString(), _layers["Roads"])
                    .gameObject;
                _roadLodRoots.Add(roadClass, root);
                var shoulder = new CountyMeshAccumulator();
                var surface = new CountyMeshAccumulator();
                foreach (var road in _source.PresentationStack.Roads.Where(
                             item => item.PresentationClass == roadClass)
                             .OrderBy(item => item.Edge.EdgeId,
                                 StringComparer.Ordinal))
                {
                    var width = RoadWidth(roadClass);
                    AddSurfaceRibbon(shoulder,
                        road.Edge.FromLocalRow, road.Edge.FromLocalColumn,
                        road.Edge.ToLocalRow, road.Edge.ToLocalColumn,
                        width + 0.18f, SurfaceLift * 2.0f);
                    AddSurfaceRibbon(surface,
                        road.Edge.FromLocalRow, road.Edge.FromLocalColumn,
                        road.Edge.ToLocalRow, road.Edge.ToLocalColumn,
                        width, SurfaceLift * 2.8f);
                }
                CreateAccumulatorRenderer(roadClass + " Shoulders",
                    root.transform, shoulder, _roadShoulderMaterial, false);
                CreateAccumulatorRenderer(roadClass + " Road Surface",
                    root.transform, surface, _roadMaterial, false);
            }

            var degrees = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var edge in _source.LayoutPackage.RoadEdges)
            {
                degrees[edge.FromNodeId] = degrees.TryGetValue(edge.FromNodeId,
                    out var first) ? first + 1 : 1;
                degrees[edge.ToNodeId] = degrees.TryGetValue(edge.ToNodeId,
                    out var second) ? second + 1 : 1;
            }
            var junctions = new CountyMeshAccumulator();
            foreach (var node in _source.LayoutPackage.RoadNodes.Where(item =>
                         degrees.TryGetValue(item.NodeId, out var degree) &&
                         degree >= 3))
                junctions.AddDisc(CellCenter(node.LocalRow, node.LocalColumn,
                    SurfaceLift * 3.2f), 0.72f, 12);
            CreateAccumulatorRenderer("Road Junctions", _layers["Roads"],
                junctions, _roadMaterial, false);
        }

        private static float RoadWidth(CountyRoadPresentationClass value)
        {
            switch (value)
            {
                case CountyRoadPresentationClass.StrategicR0: return 1.90f;
                case CountyRoadPresentationClass.CountyMainR1: return 1.18f;
                case CountyRoadPresentationClass.UrbanMainR2: return 0.58f;
                default: return 0.30f;
            }
        }

        private void AddSurfaceRibbon(CountyMeshAccumulator mesh,
            int firstRow, int firstColumn, int secondRow, int secondColumn,
            float width, float lift)
        {
            var distance = Mathf.Max(Mathf.Abs(secondRow - firstRow),
                Mathf.Abs(secondColumn - firstColumn));
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / 4f));
            var points = new List<Vector3>(steps + 1);
            for (var index = 0; index <= steps; index++)
            {
                var t = index / (float)steps;
                var row = Mathf.Lerp(firstRow, secondRow, t);
                var column = Mathf.Lerp(firstColumn, secondColumn, t);
                points.Add(new Vector3(column + 0.5f -
                                       _source.Partition.Columns * 0.5f,
                    WorldHeight(row, column) + lift,
                    _source.Partition.Rows * 0.5f - row - 0.5f));
            }
            mesh.AddRibbon(points, width);
        }

        private void BuildFortifications()
        {
            var walls = new CountyMeshAccumulator();
            foreach (var wall in _source.LayoutPackage.Fortifications.OrderBy(
                         item => item.EdgeId, StringComparer.Ordinal))
            {
                var center = CellCenter(wall.LocalRow, wall.LocalColumn);
                var eastWest = wall.Direction == PlanningCellDirection.North ||
                               wall.Direction == PlanningCellDirection.South;
                if (wall.Direction == PlanningCellDirection.North) center.z += 0.5f;
                if (wall.Direction == PlanningCellDirection.South) center.z -= 0.5f;
                if (wall.Direction == PlanningCellDirection.East) center.x += 0.5f;
                if (wall.Direction == PlanningCellDirection.West) center.x -= 0.5f;
                var height = Mathf.Max(0.34f,
                    wall.HeightCentimetres / 5000f);
                var thickness = Mathf.Max(0.10f,
                    wall.ThicknessCentimetres / 5000f);
                if (!wall.IsGate)
                    walls.AddBox(center + Vector3.up * height * 0.5f,
                        eastWest
                            ? new Vector3(1.02f, height, thickness)
                            : new Vector3(thickness, height, 1.02f));
                else
                    CreateGateEntity(wall, center, eastWest, height,
                        thickness);
            }
            CreateAccumulatorRenderer("Fortification Wall Segments",
                _fortificationDetailRoot.transform, walls, _wallMaterial,
                true);
            BuildStrategicCityWallOutline();
        }

        private void BuildStrategicCityWallOutline()
        {
            var cityEdges = _source.LayoutPackage.Fortifications.Where(item =>
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_wall",
                        StringComparison.Ordinal) ||
                    string.Equals(item.DefinitionId,
                        "facility.fortification.city_gate",
                        StringComparison.Ordinal))
                .ToArray();
            if (cityEdges.Length == 0) return;

            var minimumRow = cityEdges.Min(item => item.LocalRow);
            var maximumRow = cityEdges.Max(item => item.LocalRow);
            var minimumColumn = cityEdges.Min(item => item.LocalColumn);
            var maximumColumn = cityEdges.Max(item => item.LocalColumn);
            var wall = new CountyMeshAccumulator();
            const float height = 0.52f;
            const float thickness = 0.34f;

            // The authoritative fortification records are sampled anchors,
            // not one record per intervening 50 m cell.  Far/Mid therefore
            // need a continuous presentation outline derived from those same
            // anchors; the sparse authoritative edge objects remain intact
            // and are expanded again in the detailed Near layer.
            for (var column = minimumColumn; column <= maximumColumn;
                 column++)
            {
                AddStrategicWallCell(wall, minimumRow, column,
                    new Vector3(1.03f, height, thickness), height);
                AddStrategicWallCell(wall, maximumRow, column,
                    new Vector3(1.03f, height, thickness), height);
            }
            for (var row = minimumRow + 1; row < maximumRow; row++)
            {
                AddStrategicWallCell(wall, row, minimumColumn,
                    new Vector3(thickness, height, 1.03f), height);
                AddStrategicWallCell(wall, row, maximumColumn,
                    new Vector3(thickness, height, 1.03f), height);
            }
            CreateAccumulatorRenderer("Continuous Luoyang City Wall",
                _strategicWallRoot.transform, wall, _wallMaterial, true);
        }

        private void AddStrategicWallCell(CountyMeshAccumulator target,
            int row, int column, Vector3 scale, float height)
        {
            var center = CellCenter(row, column, SurfaceLift * 3f);
            target.AddBox(center + Vector3.up * height * 0.5f, scale);
        }

        private void CreateGateEntity(Luoyang50mLayoutFortification gate,
            Vector3 center, bool eastWest, float wallHeight, float thickness)
        {
            var root = NewChild("Gate " + gate.FacilityId,
                _fortificationDetailRoot.transform);
            var gateMesh = new CountyMeshAccumulator();
            var axis = eastWest ? Vector3.right : Vector3.forward;
            var sideSize = eastWest
                ? new Vector3(0.29f, wallHeight * 1.45f,
                    Mathf.Max(0.24f, thickness * 1.8f))
                : new Vector3(Mathf.Max(0.24f, thickness * 1.8f),
                    wallHeight * 1.45f, 0.29f);
            gateMesh.AddBox(-axis * 0.36f + Vector3.up * sideSize.y * 0.5f,
                sideSize);
            gateMesh.AddBox(axis * 0.36f + Vector3.up * sideSize.y * 0.5f,
                sideSize);
            var beamSize = eastWest
                ? new Vector3(1.04f, 0.16f, sideSize.z * 1.08f)
                : new Vector3(sideSize.x * 1.08f, 0.16f, 1.04f);
            gateMesh.AddBox(Vector3.up * wallHeight * 1.28f, beamSize);
            var mesh = gateMesh.CreateMesh("Gatehouse " + gate.EdgeId);
            if (mesh == null) return;
            OwnMesh(mesh);
            root.position = center;
            CreateRenderer(mesh.name, root, mesh, _gateMaterial, true);
        }

        private void InitializeModelSystem()
        {
            var worldMapRoot = Path.Combine(Application.streamingAssetsPath,
                "WorldMap");
            var coverage = new LuoyangFacilityModelCoverageSource(worldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(
                worldMapRoot, coverage.CombinedCatalog);
            var landmarks = new LuoyangHistoricalLandmarkKitSource(
                worldMapRoot, coverage.CombinedCatalog);
            var gates = new LuoyangGateIdentityKitSource(worldMapRoot,
                coverage.CombinedCatalog);
            var urban = new LuoyangMediumFrequencyUrbanFabricKitSource(
                worldMapRoot, coverage.CombinedCatalog);
            _modelResolver = new LuoyangFacilityModelBindingResolver(
                coverage.Bindings, coverage.CombinedCatalog);
            _modelFactory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production.Catalog,
                landmarks.Catalog, gates.Catalog, urban.Catalog);
        }

        private bool ModelCanResolve(Luoyang50mLayoutFacility facility)
        {
            try
            {
                var model = _modelResolver.ResolveModelId(
                    facility.DefinitionId, facility.FacilityId);
                return !string.IsNullOrWhiteSpace(model) &&
                       _modelFactory.GetModel(model) != null;
            }
            catch
            {
                return false;
            }
        }

        private void BuildFacilityAggregates()
        {
            var batches = new Dictionary<string, FarAggregateBatch>(
                StringComparer.Ordinal);
            foreach (var aggregate in _plan.FarAggregates)
            {
                if (GoldenBlockPlan != null && GoldenBlockPlan.ContainsBucket(
                        aggregate.BucketRow, aggregate.BucketColumn))
                    continue;
                var chunkRow = aggregate.BucketRow *
                               CountyWorldSpacePresentationPlan
                                   .FarAggregateBucketCells /
                               CountyWorldSpacePresentationPlan
                                   .TerrainChunkCells;
                var chunkColumn = aggregate.BucketColumn *
                                  CountyWorldSpacePresentationPlan
                                      .FarAggregateBucketCells /
                                  CountyWorldSpacePresentationPlan
                                      .TerrainChunkCells;
                var rural = !IsUrbanPresentationAggregate(aggregate);
                var key = (rural ? "rural:" : "urban:") + chunkRow + ":" +
                          chunkColumn + ":" + aggregate.Kind;
                if (!batches.TryGetValue(key, out var batch))
                {
                    batch = new FarAggregateBatch(aggregate.Kind, rural,
                        chunkRow, chunkColumn);
                    batches.Add(key, batch);
                }
                AddFarNeighbourhood(aggregate, !rural, batch.Bodies,
                    batch.Roofs);
            }
            foreach (var batch in batches.Values
                         .OrderBy(item => item.Rural)
                         .ThenBy(item => item.ChunkRow)
                         .ThenBy(item => item.ChunkColumn)
                         .ThenBy(item => item.Kind))
            {
                var parent = batch.Rural
                    ? _layers["Villages"] : _farFacilityRoot.transform;
                var prefix = batch.Rural ? "Far Village Chunk " :
                    "Far Urban Chunk ";
                var suffix = batch.ChunkRow + "-" + batch.ChunkColumn +
                             " " + batch.Kind;
                CreateAccumulatorRenderer(prefix + suffix + " Bodies",
                    parent, batch.Bodies, FarAggregateMaterial(batch.Kind),
                    true);
                CreateAccumulatorRenderer(prefix + suffix + " Roofs",
                    parent, batch.Roofs,
                    CategoryMaterial(batch.Rural ? "far-village-roof" :
                            "far-roof", batch.Rural
                            ? new Color(0.30f, 0.17f, 0.09f)
                            : new Color(0.25f, 0.12f, 0.075f)), true);
            }
        }

        private void BuildCitywideBuildingLanguage()
        {
            CitywideContextFacilityCount = 0;
            CitywideBuildingLanguageModuleCount = 0;
            CitywideBuildingLanguageTriangleCount = 0;
            CitywideBuildingLanguageMaterialCount = 0;
            if (CitywideBuildingLanguagePlan == null ||
                _citywideBuildingLanguageRoot == null) return;

            var mesh = new CitywideBuildingAccumulators();
            foreach (var entry in CitywideBuildingLanguagePlan.ContextEntries)
            {
                var facility = entry.Facility;
                if (GoldenBlockPlan != null &&
                    facility.LocalRow >= GoldenBlockPlan.MinimumRow &&
                    facility.LocalRow <= GoldenBlockPlan.MaximumRow &&
                    facility.LocalColumn >= GoldenBlockPlan.MinimumColumn &&
                    facility.LocalColumn <= GoldenBlockPlan.MaximumColumn)
                    continue;
                AddCitywideProfileCompound(entry, mesh);
                CitywideContextFacilityCount++;
                CitywideBuildingLanguageModuleCount +=
                    entry.Modules.Modules.Count;
            }

            CreateAccumulatorRenderer("Citywide Domestic Earth Courts",
                _citywideBuildingLanguageRoot.transform, mesh.DomesticGround,
                CategoryMaterial("citywide-ground-domestic",
                    new Color(0.47f, 0.34f, 0.20f)), false);
            CreateAccumulatorRenderer("Citywide Working Hardstands",
                _citywideBuildingLanguageRoot.transform, mesh.WorkGround,
                CategoryMaterial("citywide-ground-work",
                    new Color(0.40f, 0.34f, 0.24f)), false);
            CreateAccumulatorRenderer("Citywide Formal Courts",
                _citywideBuildingLanguageRoot.transform, mesh.FormalGround,
                CategoryMaterial("citywide-ground-formal",
                    new Color(0.52f, 0.45f, 0.32f)), false);
            CreateAccumulatorRenderer("Citywide Enclosures",
                _citywideBuildingLanguageRoot.transform, mesh.Walls,
                CategoryMaterial("citywide-enclosures",
                    new Color(0.50f, 0.35f, 0.22f)), true);
            CreateAccumulatorRenderer("Citywide Building Bodies",
                _citywideBuildingLanguageRoot.transform, mesh.Bodies,
                CategoryMaterial("citywide-building-bodies",
                    new Color(0.61f, 0.44f, 0.28f)), true);
            CreateAccumulatorRenderer("Citywide Timber Frames",
                _citywideBuildingLanguageRoot.transform, mesh.Timber,
                CategoryMaterial("citywide-timber",
                    new Color(0.29f, 0.13f, 0.075f)), true);
            CreateAccumulatorRenderer("Citywide Warm Tile Roofs",
                _citywideBuildingLanguageRoot.transform, mesh.RoofWarm,
                CategoryMaterial("citywide-roof-warm",
                    new Color(0.34f, 0.13f, 0.075f)), true);
            CreateAccumulatorRenderer("Citywide Dark Tile Roofs",
                _citywideBuildingLanguageRoot.transform, mesh.RoofDark,
                CategoryMaterial("citywide-roof-dark",
                    new Color(0.20f, 0.12f, 0.09f)), true);
            CreateAccumulatorRenderer("Citywide Weathered Tile Roofs",
                _citywideBuildingLanguageRoot.transform, mesh.RoofWeathered,
                CategoryMaterial("citywide-roof-weathered",
                    new Color(0.42f, 0.31f, 0.22f)), true);
            CreateAccumulatorRenderer("Citywide Activity Props",
                _citywideBuildingLanguageRoot.transform, mesh.Accents,
                CategoryMaterial("citywide-activity",
                    new Color(0.67f, 0.46f, 0.19f)), true);
            CreateAccumulatorRenderer("Citywide Courtyard Vegetation",
                _citywideBuildingLanguageRoot.transform, mesh.Vegetation,
                _vegetationMaterial, true);

            var filters = _citywideBuildingLanguageRoot
                .GetComponentsInChildren<MeshFilter>(true);
            CitywideBuildingLanguageTriangleCount = filters.Sum(item =>
                item.sharedMesh == null ? 0 :
                    item.sharedMesh.triangles.Length / 3);
            CitywideBuildingLanguageMaterialCount =
                _citywideBuildingLanguageRoot
                    .GetComponentsInChildren<MeshRenderer>(true)
                    .SelectMany(item => item.sharedMaterials)
                    .Where(item => item != null).Distinct().Count();
        }

        private void AddCitywideProfileCompound(
            CountyCitywideBuildingLanguageEntry entry,
            CitywideBuildingAccumulators mesh)
        {
            var facility = entry.Facility;
            var profile = entry.Profile;
            var modulePlan = entry.Modules;
            var origin = CellCenter(facility.LocalRow, facility.LocalColumn,
                SurfaceLift * 1.7f);
            var rotation = facility.RotationQuarterTurns & 3;
            var physical = FacilityScale(facility);
            var footprintWidth = Mathf.Clamp(physical.x, 0.34f, 1.72f);
            var footprintDepth = Mathf.Clamp(physical.z, 0.34f, 1.72f);
            var scaleX = footprintWidth / 1.36f;
            var scaleZ = footprintDepth / 1.36f;
            var maximumModuleHeight = Mathf.Max(0.01f,
                modulePlan.Modules.Max(item => item.Height));
            var requestedHeight = Mathf.Clamp(physical.y, 0.28f, 1.52f);
            var scaleY = requestedHeight / maximumModuleHeight;
            var ground = mesh.Ground(profile.GroundTreatment);
            var roof = mesh.Roof(modulePlan.RoofVariation);
            var foundationHeight = profile.FoundationFamily ==
                                   CountyBuildingFoundationFamily.CivicTerrace
                ? 0.12f : profile.FoundationFamily ==
                    CountyBuildingFoundationFamily.Formal ? 0.075f : 0.04f;
            var wallHeight = (profile.WallFamily ==
                              CountyBuildingWallFamily.Formal ? 0.27f : 0.20f)
                             * Mathf.Clamp(scaleY, 0.72f, 1.28f);
            var wallThickness = profile.WallFamily ==
                                CountyBuildingWallFamily.TimberFence
                ? 0.04f : 0.065f;
            var gateWidth = profile.GateFamily ==
                            CountyBuildingGateFamily.Gatehouse ? 0.54f :
                profile.GateFamily == CountyBuildingGateFamily.Wide
                    ? 0.42f : 0.28f;

            ground.AddHorizontalQuad(origin + Vector3.up * 0.01f,
                footprintWidth * 1.04f, footprintDepth * 1.04f);
            AddGoldenEnclosure(mesh.Walls, mesh.Timber, origin, rotation,
                wallHeight, wallThickness, gateWidth, profile.GateFamily,
                scaleX, scaleZ);
            foreach (var module in modulePlan.Modules)
            {
                var offset = new Vector3(module.OffsetX * scaleX, 0f,
                    module.OffsetZ * scaleZ);
                var width = module.Width * scaleX;
                var depth = module.Depth * scaleZ;
                var height = module.Height * scaleY;
                switch (module.Kind)
                {
                    case CountyBuildingModuleKind.Hall:
                    case CountyBuildingModuleKind.SideHouse:
                    case CountyBuildingModuleKind.LongWarehouse:
                    case CountyBuildingModuleKind.WorkshopShed:
                    case CountyBuildingModuleKind.Gatehouse:
                        AddGoldenHall(ground, mesh.Bodies, mesh.Timber, roof,
                            origin, offset, width, depth, height,
                            foundationHeight, rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.OpenShed:
                        AddGoldenOpenShed(mesh.Timber, roof, origin, offset,
                            width, depth, height, rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.Tree:
                        mesh.Vegetation.AddTree(origin + RotateGoldenOffset(
                            offset, rotation), Mathf.Max(0.18f, height));
                        break;
                    default:
                        AddGoldenBox(mesh.Accents, origin,
                            offset + Vector3.up * height * 0.5f,
                            new Vector3(width, height, depth), rotation);
                        break;
                }
            }
        }

        private bool IsUrbanPresentationAggregate(
            CountyFarUrbanAggregate aggregate)
        {
            return _source.IsInsideUrbanPresentation(
                Mathf.RoundToInt(aggregate.CenterRow),
                Mathf.RoundToInt(aggregate.CenterColumn), 5);
        }

        private void AddFarNeighbourhood(CountyFarUrbanAggregate aggregate,
            bool urban, CountyMeshAccumulator body,
            CountyMeshAccumulator roofs)
        {
            var profile = CountyBuildingPresentationProfileCatalog
                .HanLuoyangV2.Resolve(aggregate.Kind);
            var modulePlan = profile.Resolve("far:" +
                aggregate.StableSignature, aggregate.RotationQuarterTurns);
            var buildingModules = modulePlan.Modules.Where(item =>
                    IsBuildingMassModule(item.Kind))
                .ToArray();
            var density = Mathf.Clamp01(aggregate.Density / 255f * 0.74f +
                                        profile.Density * 0.26f);
            var maximum = urban ? 9 : 2;
            var minimum = urban ? 3 : 1;
            var buildingCount = Mathf.Clamp((urban ? 3 : 1) +
                Mathf.CeilToInt(aggregate.FacilityCount / 2f) +
                Mathf.FloorToInt(density * 3f), minimum, maximum);
            var center = new Vector3(aggregate.CenterColumn + 0.5f -
                                     _source.Partition.Columns * 0.5f,
                WorldHeight(aggregate.CenterRow, aggregate.CenterColumn) +
                SurfaceLift,
                _source.Partition.Rows * 0.5f - aggregate.CenterRow - 0.5f);
            var slots = new[]
            {
                new Vector2(-2.25f, -1.75f), new Vector2(0f, -1.75f),
                new Vector2(2.25f, -1.75f), new Vector2(-2.25f, 0.35f),
                new Vector2(2.25f, 0.35f), new Vector2(-2.25f, 2.35f),
                new Vector2(0f, 2.35f), new Vector2(2.25f, 2.35f),
                new Vector2(0f, 0.35f)
            };
            var start = (int)(aggregate.StableSignature %
                              (ulong)slots.Length);
            var added = 0;
            for (var attempt = 0;
                 attempt < slots.Length * 2 && added < buildingCount;
                 attempt++)
            {
                var slot = slots[(start + attempt) % slots.Length];
                if ((aggregate.RotationQuarterTurns & 1) != 0)
                    slot = new Vector2(slot.y, -slot.x);
                var candidateRow = Mathf.Clamp(Mathf.RoundToInt(
                        aggregate.CenterRow - slot.y), 0,
                    _source.Partition.Rows - 1);
                var candidateColumn = Mathf.Clamp(Mathf.RoundToInt(
                        aggregate.CenterColumn + slot.x), 0,
                    _source.Partition.Columns - 1);
                if (_source.Partition.LandUse(candidateRow, candidateColumn) ==
                    PlanningLandUseClass.Road) continue;
                var stable = aggregate.StableSignature +
                             (ulong)(attempt * 0x9E37);
                var jitterX = ((int)((stable >> 8) % 17) - 8) * 0.035f;
                var jitterZ = ((int)((stable >> 16) % 17) - 8) * 0.035f;
                var position = center + new Vector3(slot.x + jitterX, 0f,
                    slot.y + jitterZ);
                position.y = WorldHeight(candidateRow, candidateColumn) +
                             SurfaceLift;
                // Far is a strategic diorama: one local unit still maps to a
                // 50 m PlanningCell, while aggregate masses are deliberately
                // bolder than individual footprints so a neighbourhood is
                // legible when the complete 16 x 32 km county is visible.
                var module = buildingModules.Length == 0 ? null :
                    buildingModules[added % buildingModules.Length];
                var moduleWidth = module?.Width ?? 0.84f;
                var moduleDepth = module?.Depth ?? 0.72f;
                var moduleHeight = module?.Height ?? 0.42f;
                var width = moduleWidth * (urban ? 2.15f : 1.12f) +
                            (stable % 5) * (urban ? 0.08f : 0.035f);
                var depth = moduleDepth * (urban ? 2.05f : 1.08f) +
                            ((stable >> 4) % 5) *
                            (urban ? 0.075f : 0.035f);
                var wallHeight = moduleHeight * (urban ? 2.05f : 1.15f) +
                                 Mathf.Lerp(0.10f, 0.42f, density) +
                                 Mathf.Min(0.28f,
                                     aggregate.MaximumHeightCentimetres /
                                     11000f);
                body.AddBox(position + Vector3.up * wallHeight * 0.5f,
                    (aggregate.RotationQuarterTurns & 1) == 0
                        ? new Vector3(width, wallHeight, depth)
                        : new Vector3(depth, wallHeight, width));
                var roofShape = module?.RoofShape ??
                                CountyBuildingRoofShape.Gable;
                var roofHeightRatio = roofShape ==
                                      CountyBuildingRoofShape.LowGable
                    ? 0.20f : roofShape ==
                        CountyBuildingRoofShape.LongGable ? 0.26f : 0.34f;
                if (roofShape == CountyBuildingRoofShape.Hip)
                    roofs.AddHippedRoof(position + Vector3.up * wallHeight,
                        width * 1.18f, depth * 1.18f,
                        Mathf.Max(0.14f, wallHeight * 0.36f),
                        aggregate.RotationQuarterTurns);
                else
                    roofs.AddGabledRoof(position + Vector3.up * wallHeight,
                        width * 1.16f, depth * 1.16f,
                        Mathf.Max(0.11f, wallHeight * roofHeightRatio),
                        aggregate.RotationQuarterTurns);
                added++;
            }
        }

        private static bool IsBuildingMassModule(
            CountyBuildingModuleKind kind) =>
            kind == CountyBuildingModuleKind.Hall ||
            kind == CountyBuildingModuleKind.SideHouse ||
            kind == CountyBuildingModuleKind.LongWarehouse ||
            kind == CountyBuildingModuleKind.WorkshopShed ||
            kind == CountyBuildingModuleKind.OpenShed ||
            kind == CountyBuildingModuleKind.Gatehouse;

        private Material FarAggregateMaterial(CountyFarAggregateKind kind)
        {
            switch (kind)
            {
                case CountyFarAggregateKind.Residential:
                    return CategoryMaterial("far-residential",
                        new Color(0.57f, 0.39f, 0.23f));
                case CountyFarAggregateKind.Commercial:
                    return CategoryMaterial("far-commercial",
                        new Color(0.63f, 0.46f, 0.24f));
                case CountyFarAggregateKind.Workshop:
                    return CategoryMaterial("far-workshop",
                        new Color(0.43f, 0.34f, 0.27f));
                case CountyFarAggregateKind.Storage:
                    return CategoryMaterial("far-storage",
                        new Color(0.54f, 0.45f, 0.30f));
                case CountyFarAggregateKind.Civic:
                    return CategoryMaterial("far-civic",
                        new Color(0.64f, 0.48f, 0.31f));
                case CountyFarAggregateKind.Military:
                    return CategoryMaterial("far-military",
                        new Color(0.42f, 0.27f, 0.20f));
                default:
                    return CategoryMaterial("far-mixed",
                        new Color(0.51f, 0.38f, 0.25f));
            }
        }

        private void BuildGoldenBlockPrototype()
        {
            if (GoldenBlockPlan == null || _goldenBlockRoot == null) return;
            var ground = new CountyMeshAccumulator();
            var courtyardGround = new CountyMeshAccumulator();
            var lanes = new CountyMeshAccumulator();
            var walls = new CountyMeshAccumulator();
            var earthBodies = new CountyMeshAccumulator();
            var timber = new CountyMeshAccumulator();
            var roofWarm = new CountyMeshAccumulator();
            var roofDark = new CountyMeshAccumulator();
            var roofGrey = new CountyMeshAccumulator();
            var accents = new CountyMeshAccumulator();
            var vegetation = new CountyMeshAccumulator();

            var centerRow = GoldenBlockPlan.MinimumRow +
                            CountyGoldenBlockPresentationPlan.BlockSizeCells /
                            2f;
            var centerColumn = GoldenBlockPlan.MinimumColumn +
                               CountyGoldenBlockPresentationPlan
                                   .BlockSizeCells / 2f;
            var center = GoldenPosition(centerRow, centerColumn,
                SurfaceLift * 1.2f);
            ground.AddHorizontalQuad(center, 7.86f, 7.86f);

            AddGoldenLane(lanes, centerRow,
                GoldenBlockPlan.MinimumColumn + 0.1f, centerRow,
                GoldenBlockPlan.MaximumColumn + 0.9f, 0.24f);
            AddGoldenLane(lanes, GoldenBlockPlan.MinimumRow + 0.1f,
                centerColumn, GoldenBlockPlan.MaximumRow + 0.9f,
                centerColumn, 0.24f);
            for (var offset = 2f; offset <= 6f; offset += 2f)
            {
                AddGoldenLane(lanes, GoldenBlockPlan.MinimumRow + offset,
                    GoldenBlockPlan.MinimumColumn + 0.15f,
                    GoldenBlockPlan.MinimumRow + offset,
                    GoldenBlockPlan.MaximumColumn + 0.85f, 0.075f);
                AddGoldenLane(lanes,
                    GoldenBlockPlan.MinimumRow + 0.15f,
                    GoldenBlockPlan.MinimumColumn + offset,
                    GoldenBlockPlan.MaximumRow + 0.85f,
                    GoldenBlockPlan.MinimumColumn + offset, 0.075f);
            }

            foreach (var lot in GoldenBlockPlan.Lots)
                AddGoldenCompound(lot, courtyardGround, walls, earthBodies,
                    timber,
                    roofWarm, roofDark, roofGrey, accents, vegetation);

            CreateAccumulatorRenderer("Packed Earth Block Ground",
                _goldenBlockRoot.transform, ground,
                CategoryMaterial("golden-ground",
                    new Color(0.42f, 0.34f, 0.20f)), false);
            CreateAccumulatorRenderer("Courtyard Ground Treatments",
                _goldenBlockRoot.transform, courtyardGround,
                CategoryMaterial("golden-courtyard-ground",
                    new Color(0.53f, 0.43f, 0.27f)), false);
            CreateAccumulatorRenderer("Street and Alley Network",
                _goldenBlockRoot.transform, lanes,
                CategoryMaterial("golden-lanes",
                    new Color(0.31f, 0.24f, 0.15f)), false);
            CreateAccumulatorRenderer("Courtyard Rammed Earth Walls",
                _goldenBlockRoot.transform, walls,
                CategoryMaterial("golden-wall",
                    new Color(0.62f, 0.48f, 0.30f)), true);
            CreateAccumulatorRenderer("Five-Family Building Bodies",
                _goldenBlockRoot.transform, earthBodies,
                CategoryMaterial("golden-earth-body",
                    new Color(0.58f, 0.42f, 0.25f)), true);
            CreateAccumulatorRenderer("Timber Frames and Gates",
                _goldenBlockRoot.transform, timber,
                CategoryMaterial("golden-timber",
                    new Color(0.25f, 0.12f, 0.065f)), true);
            CreateAccumulatorRenderer("Warm Tile Roof Variants",
                _goldenBlockRoot.transform, roofWarm,
                CategoryMaterial("golden-roof-warm",
                    new Color(0.34f, 0.17f, 0.09f)), true);
            CreateAccumulatorRenderer("Dark Tile Roof Variants",
                _goldenBlockRoot.transform, roofDark,
                CategoryMaterial("golden-roof-dark",
                    new Color(0.18f, 0.10f, 0.075f)), true);
            CreateAccumulatorRenderer("Weathered Tile Roof Variants",
                _goldenBlockRoot.transform, roofGrey,
                CategoryMaterial("golden-roof-grey",
                    new Color(0.31f, 0.29f, 0.24f)), true);
            CreateAccumulatorRenderer("Market Workshop and Civic Props",
                _goldenBlockRoot.transform, accents,
                CategoryMaterial("golden-accent",
                    new Color(0.65f, 0.40f, 0.15f)), true);
            CreateAccumulatorRenderer("Courtyard Trees",
                _goldenBlockRoot.transform, vegetation,
                CategoryMaterial("golden-vegetation",
                    new Color(0.16f, 0.30f, 0.14f)), true);
            GoldenBlockVisibleModuleCount = GoldenBlockPlan.Lots.Sum(item =>
                item.ModulePlan.Modules.Count);
            GoldenBlockPropCount = GoldenBlockPlan.Lots.Sum(item =>
                item.ModulePlan.PropCount);
            GoldenBlockVegetationInstanceCount = GoldenBlockPlan.Lots.Sum(
                item => item.ModulePlan.VegetationCount);
            GoldenBlockTriangleCount = _goldenBlockRoot
                .GetComponentsInChildren<MeshFilter>(true)
                .Where(item => item.sharedMesh != null)
                .Sum(item => item.sharedMesh.triangles.Length / 3);
            GoldenBlockMaterialCount = _goldenBlockRoot
                .GetComponentsInChildren<MeshRenderer>(true)
                .SelectMany(item => item.sharedMaterials)
                .Where(item => item != null).Distinct().Count();
        }

        private void AddGoldenLane(CountyMeshAccumulator target,
            float firstRow, float firstColumn, float secondRow,
            float secondColumn, float width)
        {
            target.AddRibbon(new[]
            {
                GoldenPosition(firstRow, firstColumn, SurfaceLift * 1.8f),
                GoldenPosition(secondRow, secondColumn, SurfaceLift * 1.8f)
            }, width);
        }

        private Vector3 GoldenPosition(float row, float column, float lift)
        {
            return new Vector3(column + 0.5f -
                               _source.Partition.Columns * 0.5f,
                WorldHeight(row, column) + lift,
                _source.Partition.Rows * 0.5f - row - 0.5f);
        }

        private void AddGoldenCompound(CountyGoldenBlockLot lot,
            CountyMeshAccumulator ground, CountyMeshAccumulator walls,
            CountyMeshAccumulator bodies,
            CountyMeshAccumulator timber, CountyMeshAccumulator roofWarm,
            CountyMeshAccumulator roofDark, CountyMeshAccumulator roofGrey,
            CountyMeshAccumulator accents, CountyMeshAccumulator vegetation)
        {
            var origin = GoldenPosition(lot.CenterRow, lot.CenterColumn,
                SurfaceLift * 2.1f);
            var rotation = lot.RotationQuarterTurns & 3;
            var profile = GoldenBlockPlan.Profiles.Resolve(lot.Archetype);
            var modulePlan = lot.ModulePlan ?? profile.Resolve(
                lot.SourceFacilityId, lot.Variant);
            var roof = modulePlan.RoofVariation == 0 ? roofWarm :
                modulePlan.RoofVariation == 1 ? roofDark : roofGrey;
            var foundationHeight = profile.FoundationFamily ==
                                   CountyBuildingFoundationFamily.CivicTerrace
                ? 0.14f : profile.FoundationFamily ==
                    CountyBuildingFoundationFamily.Formal ? 0.085f : 0.045f;
            var wallHeight = profile.WallFamily ==
                             CountyBuildingWallFamily.Formal ? 0.30f : 0.22f;
            var wallThickness = profile.WallFamily ==
                                CountyBuildingWallFamily.TimberFence
                ? 0.045f : 0.075f;
            var gateWidth = profile.GateFamily ==
                            CountyBuildingGateFamily.Gatehouse ? 0.54f :
                profile.GateFamily == CountyBuildingGateFamily.Wide
                    ? 0.42f : 0.28f;
            ground.AddHorizontalQuad(origin + Vector3.up * 0.012f, 1.30f,
                1.30f);
            AddGoldenEnclosure(walls, timber, origin, rotation, wallHeight,
                wallThickness, gateWidth, profile.GateFamily);

            foreach (var module in modulePlan.Modules)
            {
                var offset = new Vector3(module.OffsetX, 0f, module.OffsetZ) *
                             profile.ScaleCalibration;
                var width = module.Width * profile.ScaleCalibration;
                var depth = module.Depth * profile.ScaleCalibration;
                var height = module.Height * profile.ScaleCalibration;
                switch (module.Kind)
                {
                    case CountyBuildingModuleKind.Hall:
                    case CountyBuildingModuleKind.SideHouse:
                    case CountyBuildingModuleKind.LongWarehouse:
                    case CountyBuildingModuleKind.WorkshopShed:
                    case CountyBuildingModuleKind.Gatehouse:
                        AddGoldenHall(ground, bodies, timber, roof, origin,
                            offset, width, depth, height, foundationHeight,
                            rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.OpenShed:
                        AddGoldenOpenShed(timber, roof, origin, offset, width,
                            depth, height, rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.MarketStall:
                        AddGoldenBox(accents, origin,
                            offset + Vector3.up * height * 0.32f,
                            new Vector3(width, height * 0.64f, depth),
                            rotation);
                        AddGoldenBox(timber, origin,
                            offset + Vector3.up * height,
                            new Vector3(width * 1.18f, 0.045f,
                                depth * 1.16f), rotation);
                        break;
                    case CountyBuildingModuleKind.Tree:
                        vegetation.AddTree(origin + RotateGoldenOffset(offset,
                            rotation), height);
                        break;
                    default:
                        AddGoldenBox(accents, origin,
                            offset + Vector3.up * height * 0.5f,
                            new Vector3(width, height, depth), rotation);
                        break;
                }
            }
        }

        private static void AddGoldenEnclosure(CountyMeshAccumulator walls,
            CountyMeshAccumulator timber, Vector3 origin, int rotation,
            float height, float thickness, float gateWidth,
            CountyBuildingGateFamily gateFamily, float scaleX = 1f,
            float scaleZ = 1f)
        {
            AddGoldenBox(walls, origin, new Vector3(-0.68f * scaleX,
                    height * 0.5f, 0f),
                new Vector3(thickness, height, 1.36f * scaleZ), rotation);
            AddGoldenBox(walls, origin, new Vector3(0.68f * scaleX,
                    height * 0.5f, 0f),
                new Vector3(thickness, height, 1.36f * scaleZ), rotation);
            AddGoldenBox(walls, origin, new Vector3(0f,
                    height * 0.5f, 0.68f * scaleZ),
                new Vector3(1.36f * scaleX, height, thickness), rotation);
            var scaledGateWidth = gateWidth * scaleX;
            var sideWidth = (1.36f * scaleX - scaledGateWidth) * 0.5f;
            var sideOffset = (scaledGateWidth + sideWidth) * 0.5f;
            AddGoldenBox(walls, origin, new Vector3(-sideOffset,
                    height * 0.5f, -0.68f * scaleZ),
                new Vector3(sideWidth, height, thickness), rotation);
            AddGoldenBox(walls, origin, new Vector3(sideOffset,
                    height * 0.5f, -0.68f * scaleZ),
                new Vector3(sideWidth, height, thickness), rotation);
            var postHeight = gateFamily == CountyBuildingGateFamily.Gatehouse
                ? 0.48f : 0.36f;
            AddGoldenBox(timber, origin,
                new Vector3(-scaledGateWidth * 0.46f,
                    postHeight * 0.5f, -0.68f * scaleZ),
                new Vector3(0.065f, postHeight, 0.085f), rotation);
            AddGoldenBox(timber, origin,
                new Vector3(scaledGateWidth * 0.46f,
                    postHeight * 0.5f, -0.68f * scaleZ),
                new Vector3(0.065f, postHeight, 0.085f), rotation);
            AddGoldenBox(timber, origin,
                new Vector3(0f, postHeight, -0.68f * scaleZ),
                new Vector3(scaledGateWidth * 1.12f, 0.07f, 0.11f),
                rotation);
        }

        private static void AddGoldenHall(CountyMeshAccumulator foundations,
            CountyMeshAccumulator bodies,
            CountyMeshAccumulator timber, CountyMeshAccumulator roof,
            Vector3 origin, Vector3 offset, float width, float depth,
            float height, float foundationHeight, int rotation,
            CountyBuildingRoofShape roofShape)
        {
            AddGoldenBox(foundations, origin,
                offset + Vector3.up * foundationHeight * 0.5f,
                new Vector3(width * 1.12f, foundationHeight,
                    depth * 1.14f), rotation);
            AddGoldenBox(bodies, origin,
                offset + Vector3.up * (foundationHeight + height * 0.5f),
                new Vector3(width, height, depth), rotation);
            AddGoldenBox(timber, origin,
                offset + new Vector3(0f, foundationHeight + height * 0.72f,
                    -depth * 0.51f),
                new Vector3(width * 0.90f, 0.055f, 0.045f), rotation);
            var roofCenter = origin + RotateGoldenOffset(offset, rotation) +
                             Vector3.up * (foundationHeight + height);
            AddGoldenRoof(roof, origin, offset, roofCenter, width, depth,
                height, foundationHeight, rotation, roofShape);
            if (foundationHeight >= 0.08f)
            {
                AddGoldenBox(foundations, origin,
                    offset + new Vector3(0f, foundationHeight * 0.42f,
                        -depth * 0.67f),
                    new Vector3(width * 0.34f, foundationHeight * 0.84f,
                        depth * 0.22f), rotation);
            }
        }

        private static void AddGoldenOpenShed(CountyMeshAccumulator timber,
            CountyMeshAccumulator roof, Vector3 origin, Vector3 offset,
            float width, float depth, float height, int rotation,
            CountyBuildingRoofShape roofShape)
        {
            foreach (var x in new[] { -0.42f, 0.42f })
            foreach (var z in new[] { -0.42f, 0.42f })
                AddGoldenBox(timber, origin,
                    offset + new Vector3(x * width, height * 0.5f,
                        z * depth), new Vector3(0.045f, height, 0.045f),
                    rotation);
            var roofCenter = origin + RotateGoldenOffset(offset, rotation) +
                             Vector3.up * height;
            AddGoldenRoof(roof, origin, offset, roofCenter, width, depth,
                height, 0f, rotation, roofShape);
        }

        private static void AddGoldenRoof(CountyMeshAccumulator roof,
            Vector3 origin, Vector3 offset, Vector3 roofCenter, float width,
            float depth, float height, float foundationHeight, int rotation,
            CountyBuildingRoofShape shape)
        {
            var ratio = shape == CountyBuildingRoofShape.LowGable ? 0.20f :
                shape == CountyBuildingRoofShape.LongGable ? 0.26f : 0.34f;
            var roofHeight = Mathf.Max(0.11f, height * ratio);
            if (shape == CountyBuildingRoofShape.Hip)
                roof.AddHippedRoof(roofCenter, width * 1.20f, depth * 1.22f,
                    roofHeight * 1.18f, rotation);
            else
                roof.AddGabledRoof(roofCenter, width * 1.18f, depth * 1.22f,
                    roofHeight, rotation);
            AddGoldenBox(roof, origin,
                offset + new Vector3(0f, foundationHeight + height +
                    roofHeight, 0f),
                new Vector3(0.055f, 0.055f, depth * 1.30f), rotation);
            AddGoldenBox(roof, origin,
                offset + new Vector3(0f, foundationHeight + height + 0.01f,
                    -depth * 0.61f),
                new Vector3(width * 1.22f, 0.045f, 0.055f), rotation);
            AddGoldenBox(roof, origin,
                offset + new Vector3(0f, foundationHeight + height + 0.01f,
                    depth * 0.61f),
                new Vector3(width * 1.22f, 0.045f, 0.055f), rotation);
        }

        private static void AddGoldenBox(CountyMeshAccumulator target,
            Vector3 origin, Vector3 offset, Vector3 size, int rotation)
        {
            if ((rotation & 1) != 0)
                size = new Vector3(size.z, size.y, size.x);
            target.AddBox(origin + RotateGoldenOffset(offset, rotation), size);
        }

        private static Vector3 RotateGoldenOffset(Vector3 value, int rotation)
        {
            return Quaternion.Euler(0f, (rotation & 3) * 90f, 0f) * value;
        }

        private void BuildFarLandmarkModels()
        {
            var groups = new Dictionary<string, FacilityBatchGroup>(
                StringComparer.Ordinal);
            var fallbackBodies = new CountyMeshAccumulator();
            var fallbackRoofs = new CountyMeshAccumulator();
            foreach (var facility in _plan.FarLandmarks)
            {
                var modelId = _modelResolver.ResolveModelId(
                    facility.DefinitionId, facility.FacilityId);
                if (string.IsNullOrWhiteSpace(modelId) ||
                    !ModelCanResolve(facility))
                {
                    var scale = FacilityScale(facility);
                    var center = CellCenter(facility.LocalRow,
                        facility.LocalColumn, SurfaceLift);
                    fallbackBodies.AddBox(center +
                        Vector3.up * scale.y * 0.5f, scale);
                    fallbackRoofs.AddGabledRoof(center + Vector3.up * scale.y,
                        scale.x * 1.12f, scale.z * 1.12f,
                        Mathf.Max(0.16f, scale.y * 0.28f),
                        facility.RotationQuarterTurns);
                    continue;
                }
                IReadOnlyList<HanBuildableFacilityBatchModule> modules;
                try
                {
                    modules = _modelFactory.GetWorldBatchModules(modelId,
                        facility.FacilityId);
                }
                catch (Exception)
                {
                    continue;
                }
                var matrix = Matrix4x4.TRS(CellCenter(facility.LocalRow,
                        facility.LocalColumn, SurfaceLift),
                    Quaternion.Euler(0f,
                        facility.RotationQuarterTurns * 90f, 0f),
                    FacilityModelScale(facility));
                foreach (var module in modules)
                {
                    var key = module.MaterialId;
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new FacilityBatchGroup(module.Material, 0, 0,
                            module.MaterialId);
                        groups.Add(key, group);
                    }
                    group.Instances.Add(new CombineInstance
                    {
                        mesh = module.Mesh,
                        subMeshIndex = 0,
                        transform = matrix * module.LocalMatrix
                    });
                }
            }
            foreach (var group in groups.Values.OrderBy(item =>
                         item.MaterialId, StringComparer.Ordinal))
            {
                var mesh = OwnMesh(new Mesh
                {
                    name = "Far Landmark Batch " + group.MaterialId,
                    indexFormat = IndexFormat.UInt32
                });
                mesh.CombineMeshes(group.Instances.ToArray(), true, true,
                    false);
                mesh.RecalculateBounds();
                CreateRenderer(mesh.name, _farLandmarkRoot.transform, mesh,
                    group.Material, true);
            }
            CreateAccumulatorRenderer("Far Landmark Fallback Bodies",
                _farLandmarkRoot.transform, fallbackBodies,
                CategoryMaterial("far-landmark-body",
                    new Color(0.62f, 0.43f, 0.24f)), true);
            CreateAccumulatorRenderer("Far Landmark Fallback Roofs",
                _farLandmarkRoot.transform, fallbackRoofs,
                CategoryMaterial("far-landmark-roof",
                    new Color(0.24f, 0.10f, 0.06f)), true);
        }

        private bool IsInsideUrbanCandidate(int row, int column)
        {
            var area = _source.LayoutPackage.UrbanAreaCandidate;
            return row >= area.MinimumRow && row <= area.MaximumRow &&
                   column >= area.MinimumColumn && column <= area.MaximumColumn;
        }

        private void BuildModelFacilityBatches()
        {
            var groups = new Dictionary<string, FacilityBatchGroup>(
                StringComparer.Ordinal);
            var fallbacks = new CountyMeshAccumulator();
            var highValueFacilityIds = new HashSet<string>(
                _source.PresentationStack.MidFacilities
                    .OrderBy(item => item.Kind)
                    .ThenBy(item => item.Facility.FacilityId,
                        StringComparer.Ordinal)
                    .Take(32)
                     .Select(item => item.Facility.FacilityId),
                StringComparer.Ordinal);
            foreach (var landmark in _plan.FarLandmarks)
                highValueFacilityIds.Add(landmark.FacilityId);
            foreach (var facility in _source.PresentationStack.MidFacilities
                         .Select(item => item.Facility)
                         .OrderBy(item => item.FacilityId,
                             StringComparer.Ordinal))
            {
                if (CountyWorldSpacePresentationPlan.IsSpecializedInfrastructure(
                        facility.DefinitionId)) continue;
                if (!highValueFacilityIds.Contains(facility.FacilityId))
                {
                    // The shared urban aggregate layer supplies readable
                    // street-block fabric in Mid.  Drawing a second proxy for
                    // every ordinary Facility here merely turns the city back
                    // into a field of technical dots.
                    continue;
                }
                var modelId = _modelResolver.ResolveModelId(
                    facility.DefinitionId, facility.FacilityId);
                if (string.IsNullOrWhiteSpace(modelId) ||
                    !ModelCanResolve(facility))
                {
                    AddFacilityFallback(fallbacks, facility, false);
                    continue;
                }
                IReadOnlyList<HanBuildableFacilityBatchModule> modules;
                try
                {
                    modules = _modelFactory.GetWorldBatchModules(modelId,
                        facility.FacilityId);
                }
                catch (Exception)
                {
                    AddFacilityFallback(fallbacks, facility, false);
                    continue;
                }
                var scale = FacilityModelScale(facility);
                var matrix = Matrix4x4.TRS(CellCenter(facility.LocalRow,
                        facility.LocalColumn, SurfaceLift),
                    Quaternion.Euler(0f,
                        facility.RotationQuarterTurns * 90f, 0f), scale);
                var chunkRow = facility.LocalRow /
                               CountyWorldSpacePresentationPlan.TerrainChunkCells;
                var chunkColumn = facility.LocalColumn /
                                  CountyWorldSpacePresentationPlan.TerrainChunkCells;
                foreach (var module in modules)
                {
                    var key = chunkRow + ":" + chunkColumn + ":" +
                              module.MaterialId;
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new FacilityBatchGroup(module.Material,
                            chunkRow, chunkColumn, module.MaterialId);
                        groups.Add(key, group);
                    }
                    group.Instances.Add(new CombineInstance
                    {
                        mesh = module.Mesh,
                        subMeshIndex = 0,
                        transform = matrix * module.LocalMatrix
                    });
                }
            }
            foreach (var group in groups.Values.OrderBy(item => item.ChunkRow)
                         .ThenBy(item => item.ChunkColumn)
                         .ThenBy(item => item.MaterialId,
                             StringComparer.Ordinal))
            {
                var mesh = OwnMesh(new Mesh
                {
                    name = "Facility Batch " + group.ChunkRow + "-" +
                           group.ChunkColumn + " " + group.MaterialId,
                    indexFormat = IndexFormat.UInt32
                });
                mesh.CombineMeshes(group.Instances.ToArray(), true, true,
                    false);
                mesh.RecalculateBounds();
                CreateRenderer(mesh.name, _midFacilityRoot.transform, mesh,
                    group.Material, true);
            }
            CreateAccumulatorRenderer("Deterministic Facility Fallbacks",
                _midFacilityRoot.transform, fallbacks, _fallbackMaterial,
                true);
        }

        private void AddFacilityFallback(CountyMeshAccumulator target,
            Luoyang50mLayoutFacility facility, bool clusterPresentation)
        {
            var scale = FacilityScale(facility);
            var minimumFootprint = clusterPresentation ? 1.15f : 0.92f;
            var minimumHeight = clusterPresentation ? 0.72f : 0.58f;
            var width = Mathf.Clamp(scale.x, minimumFootprint, 1.8f);
            var depth = Mathf.Clamp(scale.z, minimumFootprint, 1.8f);
            var height = Mathf.Clamp(scale.y, minimumHeight, 1.4f);
            var center = CellCenter(facility.LocalRow,
                facility.LocalColumn, SurfaceLift);
            target.AddBox(center + Vector3.up * height * 0.5f,
                new Vector3(width, height, depth));
            target.AddBox(center + Vector3.up * (height + 0.08f),
                new Vector3(width * 0.82f, 0.10f, depth * 0.82f));
        }

        private static Vector3 FacilityScale(
            Luoyang50mLayoutFacility facility)
        {
            var width = Mathf.Clamp(facility.WidthCentimetres / 5000f,
                0.20f, 3.2f);
            var depth = Mathf.Clamp(facility.DepthCentimetres / 5000f,
                0.20f, 3.2f);
            var height = Mathf.Clamp(facility.HeightCentimetres / 5000f,
                0.24f, 2.8f);
            return new Vector3(width, height, depth);
        }

        private static Vector3 FacilityModelScale(
            Luoyang50mLayoutFacility facility)
        {
            var physical = FacilityScale(facility);
            // Catalog models use a normalized 0..1 footprint but their roof
            // ridge is commonly only 0.20..0.35 local units high.  Applying
            // the physical world height directly a second time flattened a
            // 6.5 m courtyard to roughly 1.5 m.  Compensate only the model's
            // normalized Y axis; X/Z remain the authoritative footprint.
            return new Vector3(physical.x,
                Mathf.Clamp(physical.y * 3.6f, 0.72f, 6.4f), physical.z);
        }

        private void BuildAgriculture()
        {
            var fields = new CountyMeshAccumulator();
            foreach (var facility in _source.LayoutPackage.Facilities.Where(
                         item => string.Equals(item.CategoryId, "agriculture",
                                     StringComparison.Ordinal) ||
                                 item.DefinitionId.IndexOf("agriculture",
                                     StringComparison.Ordinal) >= 0)
                         .OrderBy(item => item.FacilityId,
                             StringComparer.Ordinal))
            {
                var center = CellCenter(facility.LocalRow,
                    facility.LocalColumn, SurfaceLift * 1.25f);
                var width = Mathf.Clamp(facility.WidthCentimetres / 5000f,
                    0.72f, 2.4f);
                var depth = Mathf.Clamp(facility.DepthCentimetres / 5000f,
                    0.72f, 2.4f);
                fields.AddHorizontalQuad(center, width, depth);
                var strips = 4;
                for (var strip = 1; strip < strips; strip++)
                {
                    var z = center.z - depth * 0.5f +
                            depth * strip / strips;
                    fields.AddBox(new Vector3(center.x,
                            center.y + 0.015f, z),
                        new Vector3(width * 0.92f, 0.018f, 0.025f));
                }
            }
            CreateAccumulatorRenderer("Field Patches and Ridges",
                _layers["Agriculture"], fields, _fieldMaterial, false);
        }

        private void BuildVegetation()
        {
            var vegetation = new CountyMeshAccumulator();
            for (var row = 0; row < _source.Partition.Rows; row += 6)
            for (var column = 0; column < _source.Partition.Columns; column += 6)
            {
                var surface = _plan.SurfaceClass(row, column);
                if (surface != CountySurfaceVisualClass.Forest &&
                    surface != CountySurfaceVisualClass.Waterside) continue;
                if (CountyWorldSpacePresentationPlan.StableModulo(row,
                        column, 3) == 0) continue;
                var jitterX = (CountyWorldSpacePresentationPlan.StableModulo(
                                   row, column, 11) - 5) * 0.035f;
                var jitterZ = (CountyWorldSpacePresentationPlan.StableModulo(
                                   column, row, 11) - 5) * 0.035f;
                var center = CellCenter(row, column, SurfaceLift) +
                             new Vector3(jitterX, 0f, jitterZ);
                vegetation.AddTree(center, surface ==
                                           CountySurfaceVisualClass.Forest
                    ? 0.62f : 0.42f);
            }
            CreateAccumulatorRenderer("Chunk Vegetation Batch",
                _layers["Vegetation"], vegetation, _vegetationMaterial, true);
        }

        private void BuildCountyBoundary()
        {
            var boundary = new CountyMeshAccumulator();
            var minimumX = -_source.Partition.Columns * 0.5f;
            var maximumX = _source.Partition.Columns * 0.5f;
            var minimumZ = -_source.Partition.Rows * 0.5f;
            var maximumZ = _source.Partition.Rows * 0.5f;
            boundary.AddRibbon(new[]
            {
                new Vector3(minimumX, SurfaceLift * 5f, minimumZ),
                new Vector3(maximumX, SurfaceLift * 5f, minimumZ),
                new Vector3(maximumX, SurfaceLift * 5f, maximumZ),
                new Vector3(minimumX, SurfaceLift * 5f, maximumZ),
                new Vector3(minimumX, SurfaceLift * 5f, minimumZ)
            }, 0.18f);
            CreateAccumulatorRenderer("Subtle County Boundary",
                _layers["Planning Overlay"], boundary,
                CategoryMaterial("county-boundary",
                    new Color(0.62f, 0.53f, 0.34f)), false);
        }

        private void BuildDebugGeometry()
        {
            var hull = new CountyMeshAccumulator();
            var cells = _source.LayoutPackage.UrbanAreaCandidate.HullCells;
            if (cells.Count > 2)
            {
                var points = cells.Select(item => CellCenter(item.Row,
                        item.Column, SurfaceLift * 8f)).ToList();
                points.Add(points[0]);
                hull.AddRibbon(points, 0.18f);
            }
            CreateAccumulatorRenderer("Urban Candidate Hull DEBUG",
                _debugRoot.transform, hull,
                CategoryMaterial("debug-hull",
                    new Color(0.92f, 0.58f, 0.10f)), false);
        }

        private void ApplyCameraViewport(Rect guiViewport)
        {
            if (_camera == null || Screen.width <= 0 || Screen.height <= 0)
                return;
            _camera.pixelRect = new Rect(guiViewport.x,
                Screen.height - guiViewport.yMax, guiViewport.width,
                guiViewport.height);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.08f, 0.09f, 0.06f);
            _camera.orthographic = false;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.nearClipPlane = 0.08f;
            _camera.farClipPlane = 2200f;

            // The strategic 2 km map uses short linear fog ranges measured
            // in its compressed world units.  The county camera can sit more
            // than 600 local cells from the terrain in Far LOD, so retaining
            // that fog collapses the entire county into one flat fog colour.
            // County terrain has its own vertex colours and controlled
            // lighting; disable global strategic fog while this renderer owns
            // the shared camera.  World/person routes restore their own
            // atmosphere when the county presentation is hidden.
            RenderSettings.fog = false;
        }

        private void ApplyCameraPose()
        {
            if (_camera == null) return;
            ApplyCameraViewport(_lastGuiViewport);
            var centerRow = _source.ViewMinimumRow + _source.ViewRows * 0.5f;
            var centerColumn = _source.ViewMinimumColumn +
                               _source.ViewColumns * 0.5f;
            var focus = new Vector3(centerColumn -
                                    _source.Partition.Columns * 0.5f,
                WorldHeight(centerRow, centerColumn),
                _source.Partition.Rows * 0.5f - centerRow);
            var aspect = Mathf.Max(0.35f,
                _lastGuiViewport.width / Mathf.Max(1f, _lastGuiViewport.height));
            var extent = Mathf.Max(_source.ViewRows,
                _source.ViewColumns / aspect);
            var distance = extent /
                           (2f * Mathf.Tan(CameraFieldOfView * 0.5f *
                                           Mathf.Deg2Rad));
            distance *= 0.88f;
            var rotation = Quaternion.Euler(CameraPitch,
                -_source.ViewRotationDegrees, 0f);
            _camera.transform.rotation = rotation;
            _camera.transform.position = focus -
                                         rotation * Vector3.forward * distance;
        }

        private void ApplyLayerVisibility()
        {
            var lod = _source.PresentationLod;
            // Urban aggregates are the non-interactive street-block fabric
            // shared by all county zoom levels.  Mid/Near add authoritative
            // selectable Facility models on top instead of falling back to a
            // sparse technical point cloud.
            _farFacilityRoot.SetActive(lod == CountyMapPresentationLod.Far);
            _citywideBuildingLanguageRoot.SetActive(
                lod == CountyMapPresentationLod.Mid);
            _goldenBlockRoot.SetActive(true);
            _farLandmarkRoot.SetActive(lod == CountyMapPresentationLod.Far);
            _midFacilityRoot.SetActive(lod == CountyMapPresentationLod.Mid);
            _nearFacilityRoot.SetActive(lod == CountyMapPresentationLod.Near);
            _strategicWallRoot.SetActive(lod != CountyMapPresentationLod.Near);
            _fortificationDetailRoot.SetActive(
                lod != CountyMapPresentationLod.Far);
            _layers["Villages"].gameObject.SetActive(
                lod == CountyMapPresentationLod.Far);
            _layers["Roads"].gameObject.SetActive(
                _source.MapOverlays == null ||
                _source.MapOverlays.RoadsVisible);
            _layers["Water"].gameObject.SetActive(
                _source.MapOverlays == null ||
                _source.MapOverlays.RiversVisible);
            _layers["Fortifications"].gameObject.SetActive(
                _source.MapOverlays == null ||
                _source.MapOverlays.FortificationsVisible);
            foreach (var pair in _roadLodRoots)
            {
                var maximum = lod == CountyMapPresentationLod.Far
                    ? CountyRoadPresentationClass.CountyMainR1
                    : lod == CountyMapPresentationLod.Mid
                        ? CountyRoadPresentationClass.UrbanMainR2
                        : CountyRoadPresentationClass.LocalR3;
                pair.Value.SetActive(pair.Key <= maximum);
            }
            _planningGridRoot.SetActive(_source.ShouldShowPlanningGrid);
            _ghostRoot.SetActive(_source.PresentationMode ==
                                 CountySubViewMode.Planning &&
                                 _source.ShouldDrawBuildingGhostWorldSpace);
            _draftRoot.SetActive(_source.PresentationMode ==
                                 CountySubViewMode.Planning);
            _debugRoot.SetActive(_debugVisible);
            _lastLod = lod;
        }

        private void RefreshNearDetailsIfNeeded()
        {
            if (_source.PresentationLod != CountyMapPresentationLod.Near)
                return;
            var row = Mathf.RoundToInt(_source.ViewMinimumRow +
                                       _source.ViewRows * 0.5f);
            var column = Mathf.RoundToInt(_source.ViewMinimumColumn +
                                          _source.ViewColumns * 0.5f);
            if (Mathf.Abs(row - _lastNearRow) < NearRefreshCellDistance &&
                Mathf.Abs(column - _lastNearColumn) < NearRefreshCellDistance &&
                _lastLod == CountyMapPresentationLod.Near) return;
            _lastNearRow = row;
            _lastNearColumn = column;
            ClearChildren(_nearFacilityRoot.transform);
            var fallbacks = new CountyMeshAccumulator();
            var viewport = new CountyMapViewport(_source.ViewMinimumRow,
                _source.ViewMinimumColumn, _source.ViewRows,
                _source.ViewColumns, 3f);
            var candidates = _source.PresentationStack.VisibleFacilities(
                    CountyMapPresentationLod.Near, viewport)
                .Where(item => !CountyWorldSpacePresentationPlan
                    .IsSpecializedInfrastructure(item.Facility.DefinitionId))
                .OrderBy(item => DistanceSquared(item.Facility, row, column))
                .ThenBy(item => item.Facility.FacilityId,
                    StringComparer.Ordinal)
                .Take(CountyWorldSpacePresentationPlan
                    .MaximumNearDetailedFacilities)
                .ToArray();
            foreach (var item in candidates)
            {
                var facility = item.Facility;
                var modelId = _modelResolver.ResolveModelId(
                    facility.DefinitionId, facility.FacilityId);
                if (string.IsNullOrWhiteSpace(modelId))
                {
                    AddNearFacilityFallback(fallbacks, facility);
                    continue;
                }
                try
                {
                    var instance = _modelFactory.Create(modelId,
                        _nearFacilityRoot.transform, facility.FacilityId,
                        facility.SourceCellId64, true);
                    instance.transform.localPosition = CellCenter(
                        facility.LocalRow, facility.LocalColumn,
                        SurfaceLift * 2f);
                    instance.transform.localRotation = Quaternion.Euler(0f,
                        facility.RotationQuarterTurns * 90f, 0f);
                    instance.transform.localScale =
                        FacilityModelScale(facility);
                    // Factory models carry turntable-oriented LOD thresholds.
                    // At a 24x48-cell county camera those thresholds cull a
                    // historically sized building even though Near explicitly
                    // requests detail.  The Near budget is already capped at
                    // 96 objects, so force its detailed renderer here.
                    foreach (var group in instance.GetComponentsInChildren<
                                 LODGroup>(true))
                        group.ForceLOD(0);
                }
                catch (Exception)
                {
                    AddNearFacilityFallback(fallbacks, facility);
                }
            }
            CreateAccumulatorRenderer("Near Facility Fallbacks",
                _nearFacilityRoot.transform, fallbacks, _fallbackMaterial,
                true);
        }

        private void AddNearFacilityFallback(CountyMeshAccumulator target,
            Luoyang50mLayoutFacility facility)
        {
            var scale = FacilityScale(facility);
            var width = Mathf.Clamp(scale.x, 0.30f, 3.2f);
            var depth = Mathf.Clamp(scale.z, 0.30f, 3.2f);
            var height = Mathf.Clamp(scale.y, 0.34f, 2.8f);
            var center = CellCenter(facility.LocalRow,
                facility.LocalColumn, SurfaceLift * 2f);
            target.AddBox(center + Vector3.up * height * 0.5f,
                new Vector3(width, height, depth));
            target.AddGabledRoof(center + Vector3.up * height,
                width * 1.12f, depth * 1.12f,
                Mathf.Max(0.14f, height * 0.3f),
                facility.RotationQuarterTurns);
        }

        private static int DistanceSquared(
            Luoyang50mLayoutFacility facility, int row, int column)
        {
            var dr = facility.LocalRow - row;
            var dc = facility.LocalColumn - column;
            return dr * dr + dc * dc;
        }

        private void RefreshPlanningPresentationIfNeeded()
        {
            if (_source.PresentationMode != CountySubViewMode.Planning) return;
            unchecked
            {
                var signature = _source.SelectedLocalRow * 397 ^
                                 _source.SelectedLocalColumn;
                signature = signature * 397 ^ _source.PreviewLocalRow;
                signature = signature * 397 ^ _source.PreviewLocalColumn;
                signature = signature * 397 ^ _source.RotationQuarterTurns;
                signature = signature * 397 ^
                            (_source.Session?.AllDrafts.Count ?? 0);
                signature = signature * 397 ^
                            (int)(_source.ToolState?.PrimaryTool ?? 0);
                signature = signature * 397 ^
                            (int)(_source.Validation?.State ?? 0);
                signature = signature * 397 ^ (int)
                    CountyBuildingPresentationStableHash.Text(
                        _source.SelectedProfile?.ProfileId ?? string.Empty);
                if (signature == _lastPlanningSignature) return;
                _lastPlanningSignature = signature;
            }
            BuildLocalPlanningGrid();
            BuildGhost();
            BuildDrafts();
        }

        private void RefreshSelectionIfNeeded()
        {
            var signature = _source.SelectedLocalRow * 397 ^
                            _source.SelectedLocalColumn;
            if (signature == _lastSelectionSignature) return;
            _lastSelectionSignature = signature;
            ClearChildren(_selectionRoot.transform);
            var marker = new CountyMeshAccumulator();
            var row = _source.SelectedLocalRow;
            var column = _source.SelectedLocalColumn;
            marker.AddRibbon(new[]
            {
                CellCorner(row, column, SurfaceLift * 10f),
                CellCorner(row, column + 1, SurfaceLift * 10f),
                CellCorner(row + 1, column + 1, SurfaceLift * 10f),
                CellCorner(row + 1, column, SurfaceLift * 10f),
                CellCorner(row, column, SurfaceLift * 10f)
            }, 0.10f);
            var facility = _source.SelectedObservedFacility;
            if (facility != null)
            {
                var center = CellCenter(facility.LocalRow,
                    facility.LocalColumn, SurfaceLift * 12f);
                var width = Mathf.Clamp(
                    facility.WidthCentimetres / 5000f, 0.45f, 3.4f);
                var depth = Mathf.Clamp(
                    facility.DepthCentimetres / 5000f, 0.45f, 3.4f);
                if ((facility.RotationQuarterTurns & 1) != 0)
                {
                    var temporary = width;
                    width = depth;
                    depth = temporary;
                }
                marker.AddRibbon(new[]
                {
                    center + new Vector3(-width * 0.58f, 0f,
                        -depth * 0.58f),
                    center + new Vector3(width * 0.58f, 0f,
                        -depth * 0.58f),
                    center + new Vector3(width * 0.58f, 0f,
                        depth * 0.58f),
                    center + new Vector3(-width * 0.58f, 0f,
                        depth * 0.58f),
                    center + new Vector3(-width * 0.58f, 0f,
                        -depth * 0.58f)
                }, 0.14f);
            }
            CreateAccumulatorRenderer("Selected 50m Cell",
                _selectionRoot.transform, marker,
                CategoryMaterial("selection",
                    new Color(0.96f, 0.79f, 0.24f)), false);
        }

        private void BuildLocalPlanningGrid()
        {
            ClearChildren(_planningGridRoot.transform);
            if (!_source.ShouldShowPlanningGrid) return;
            var cells = _plan.LocalPlanningGrid(_source.PreviewLocalRow,
                _source.PreviewLocalColumn);
            if (cells.Count == 0) return;
            var minimumRow = cells.Min(item => item.Row);
            var maximumRow = cells.Max(item => item.Row) + 1;
            var minimumColumn = cells.Min(item => item.Column);
            var maximumColumn = cells.Max(item => item.Column) + 1;
            var vertices = new List<Vector3>();
            for (var row = minimumRow; row <= maximumRow; row++)
            for (var column = minimumColumn; column < maximumColumn; column++)
            {
                vertices.Add(CellCorner(row, column, SurfaceLift * 6f));
                vertices.Add(CellCorner(row, column + 1, SurfaceLift * 6f));
            }
            for (var column = minimumColumn; column <= maximumColumn; column++)
            for (var row = minimumRow; row < maximumRow; row++)
            {
                vertices.Add(CellCorner(row, column, SurfaceLift * 6f));
                vertices.Add(CellCorner(row + 1, column, SurfaceLift * 6f));
            }
            var mesh = OwnMesh(new Mesh { name = "Local Planning Grid Mesh" });
            mesh.SetVertices(vertices);
            mesh.SetIndices(Enumerable.Range(0, vertices.Count).ToArray(),
                MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            CreateRenderer(mesh.name, _planningGridRoot.transform, mesh,
                _gridMaterial, false);

            var hover = new CountyMeshAccumulator();
            var selected = new CountyMeshAccumulator();
            var covered = new CountyMeshAccumulator();
            var footprint = new CountyMeshAccumulator();
            if (_source.HasHoveredPlanningCell)
                AddPlanningCellOutline(hover, _source.HoveredLocalRow,
                    _source.HoveredLocalColumn, SurfaceLift * 8f, 0.075f);
            AddPlanningCellOutline(selected, _source.SelectedLocalRow,
                _source.SelectedLocalColumn, SurfaceLift * 9f, 0.095f);
            if (_source.Validation != null)
                foreach (var cell in _source.Validation.CoveredCells)
                    AddPlanningCellOutline(covered, cell.Row, cell.Column,
                        SurfaceLift * 7f, 0.055f);
            if (_source.CurrentFootprint != null)
            {
                var center = CellCenter(_source.PreviewLocalRow,
                    _source.PreviewLocalColumn, SurfaceLift * 10f);
                var halfWidth = (float)_source.CurrentFootprint.WidthMetres /
                                100f;
                var halfDepth = (float)_source.CurrentFootprint.LengthMetres /
                                100f;
                footprint.AddRibbon(new[]
                {
                    center + new Vector3(-halfWidth, 0f, -halfDepth),
                    center + new Vector3(halfWidth, 0f, -halfDepth),
                    center + new Vector3(halfWidth, 0f, halfDepth),
                    center + new Vector3(-halfWidth, 0f, halfDepth),
                    center + new Vector3(-halfWidth, 0f, -halfDepth)
                }, 0.085f);
            }
            CreateAccumulatorRenderer("Hovered Formal 50m Cell",
                _planningGridRoot.transform, hover, _gridHoverMaterial,
                false);
            CreateAccumulatorRenderer("Selected Formal 50m Cell",
                _planningGridRoot.transform, selected, _gridSelectedMaterial,
                false);
            var validationMaterial = _source.Validation?.State ==
                                     PlacementValidationState.Valid
                ? _validGhostMaterial : _source.Validation?.State ==
                    PlacementValidationState.Conditional
                    ? _warningGhostMaterial : _invalidGhostMaterial;
            CreateAccumulatorRenderer("Covered Planning Cells",
                _planningGridRoot.transform, covered, validationMaterial,
                false);
            CreateAccumulatorRenderer("True Metric Footprint",
                _planningGridRoot.transform, footprint, validationMaterial,
                false);
        }

        private void AddPlanningCellOutline(CountyMeshAccumulator target,
            int row, int column, float lift, float width)
        {
            target.AddRibbon(new[]
            {
                CellCorner(row, column, lift),
                CellCorner(row, column + 1, lift),
                CellCorner(row + 1, column + 1, lift),
                CellCorner(row + 1, column, lift),
                CellCorner(row, column, lift)
            }, width);
        }

        private Vector3 CellCorner(int row, int column, float lift)
        {
            var sampleRow = Mathf.Clamp(row, 0,
                _source.Partition.Rows - 1);
            var sampleColumn = Mathf.Clamp(column, 0,
                _source.Partition.Columns - 1);
            return new Vector3(column - _source.Partition.Columns * 0.5f,
                WorldHeight(sampleRow, sampleColumn) + lift,
                _source.Partition.Rows * 0.5f - row);
        }

        private void BuildGhost()
        {
            ClearChildren(_ghostRoot.transform);
            CurrentGhostPresentationProfileId = string.Empty;
            if (!_source.ShouldDrawBuildingGhostWorldSpace ||
                _source.SelectedProfile == null ||
                _source.Validation == null) return;
            var placement = _source.SelectedProfile;
            var presentation = CountyBuildingPresentationProfileCatalog
                .HanLuoyangV2.Resolve(placement.FacilityDefinitionId,
                    placement.PlacementCategoryId);
            CurrentGhostPresentationProfileId = presentation.ProfileId;
            var modulePlan = presentation.Resolve(placement.ProfileId,
                _source.RotationQuarterTurns);
            var center = CellCenter(_source.PreviewLocalRow,
                _source.PreviewLocalColumn, SurfaceLift * 4f);
            var material = _source.Validation.State ==
                           PlacementValidationState.Valid
                ? _validGhostMaterial
                : _source.Validation.State ==
                  PlacementValidationState.Conditional
                    ? _warningGhostMaterial : _invalidGhostMaterial;
            var ghost = new CountyMeshAccumulator();
            AddGoldenProfileProxy(ghost, presentation, modulePlan, center,
                _source.RotationQuarterTurns,
                placement.FootprintWidthCentimetres / 5000f,
                placement.FootprintLengthCentimetres / 5000f,
                placement.HeightCentimetres / 5000f);
            CreateAccumulatorRenderer("Architectural Building Ghost " +
                presentation.ProfileId, _ghostRoot.transform, ghost,
                material, false);

            var guides = new CountyMeshAccumulator();
            var entrance = placement.EntranceOffsets.Single(item =>
                item.Primary);
            var entranceOffset = RotateGoldenOffset(new Vector3(
                    entrance.EastOffsetCentimetres / 5000f, 0f,
                    entrance.NorthOffsetCentimetres / 5000f),
                _source.RotationQuarterTurns);
            guides.AddBox(center + entranceOffset + Vector3.up * 0.10f,
                new Vector3(0.11f, 0.20f, 0.11f));
            var access = _source.Validation.RoadAccessResult.AccessCells;
            if (access.Count > 0)
                guides.AddRibbon(new[]
                {
                    center + entranceOffset + Vector3.up * 0.04f,
                    CellCenter(access[0].Row, access[0].Column,
                        SurfaceLift * 7f)
                }, 0.07f);
            CreateAccumulatorRenderer("Entrance and Road Access Guide",
                _ghostRoot.transform, guides, material, false);
        }

        private void BuildDrafts()
        {
            ClearChildren(_draftRoot.transform);
            var session = _source.Session;
            if (session == null) return;
            var buildings = new CountyMeshAccumulator();
            var roads = new CountyMeshAccumulator();
            var canals = new CountyMeshAccumulator();
            var walls = new CountyMeshAccumulator();
            foreach (var draft in session.AllDrafts.OrderBy(item =>
                         item.CreatedOrder))
            {
                if (draft is DraftBuildingBlueprint building &&
                    building.CoveredPlanningCells.Count > 0)
                {
                    var centerCell = building.CoveredPlanningCells[
                        building.CoveredPlanningCells.Count / 2];
                    var presentation =
                        CountyBuildingPresentationProfileCatalog.HanLuoyangV2
                            .Resolve(building.FacilityDefinitionId);
                    var placement = _source.Profiles.ProfilesById[
                        building.ProfileId];
                    AddGoldenProfileProxy(buildings, presentation,
                        presentation.Resolve(building.DraftId,
                            building.CreatedOrder),
                        CellCenter(centerCell.Row, centerCell.Column,
                            SurfaceLift * 3f),
                        building.RotationQuarterTurns,
                        building.WidthCentimetres / 5000f,
                        building.LengthCentimetres / 5000f,
                        placement.HeightCentimetres / 5000f);
                }
                else if (draft is DraftRoadGeometry road)
                    AddDraftPath(roads, road.Path, 0.34f,
                        SurfaceLift * 6f);
                else if (draft is DraftCanalGeometry canal)
                    AddDraftPath(canals, canal.Path, 0.24f,
                        SurfaceLift * 6f);
                else if (draft is DraftFortification fortification)
                {
                    foreach (var segment in fortification.Segments)
                    {
                        var center = CellCenter(segment.Cell.Row,
                            segment.Cell.Column, SurfaceLift * 4f);
                        var eastWest = segment.EdgeDirection ==
                                       PlanningCellDirection.North ||
                                       segment.EdgeDirection ==
                                       PlanningCellDirection.South;
                        walls.AddBox(center + Vector3.up * 0.22f,
                            eastWest
                                ? new Vector3(1f, 0.44f, 0.12f)
                                : new Vector3(0.12f, 0.44f, 1f));
                    }
                }
            }
            CreateAccumulatorRenderer("Building Drafts", _draftRoot.transform,
                buildings, _draftWallMaterial, false);
            CreateAccumulatorRenderer("Road Draft Ribbons", _draftRoot.transform,
                roads, _draftRoadMaterial, false);
            CreateAccumulatorRenderer("Canal Draft Ribbons", _draftRoot.transform,
                canals, _draftCanalMaterial, false);
            CreateAccumulatorRenderer("Wall Draft Foundations",
                _draftRoot.transform, walls, _draftWallMaterial, false);
        }

        private static void AddGoldenProfileProxy(
            CountyMeshAccumulator target,
            CountyBuildingPresentationProfile profile,
            CountyBuildingModulePlan modulePlan, Vector3 origin,
            int rotation, float footprintWidth, float footprintDepth,
            float requestedHeight)
        {
            var scaleX = Mathf.Max(0.18f, footprintWidth / 1.36f);
            var scaleZ = Mathf.Max(0.18f, footprintDepth / 1.36f);
            var maximumHeight = Mathf.Max(0.01f,
                modulePlan.Modules.Max(item => item.Height));
            var targetHeight = Mathf.Max(0.28f, requestedHeight);
            if (profile.Importance >=
                CountyBuildingPresentationImportance.Major)
                targetHeight = Mathf.Max(0.36f, requestedHeight);
            var scaleY = targetHeight / maximumHeight;
            var foundationHeight = profile.FoundationFamily ==
                                   CountyBuildingFoundationFamily.CivicTerrace
                ? 0.10f : profile.FoundationFamily ==
                    CountyBuildingFoundationFamily.Formal ? 0.065f : 0.035f;
            var wallHeight = (profile.WallFamily ==
                              CountyBuildingWallFamily.Formal ? 0.24f : 0.18f)
                             * Mathf.Clamp(scaleY, 0.7f, 1.25f);
            var wallThickness = profile.WallFamily ==
                                CountyBuildingWallFamily.TimberFence
                ? 0.035f : 0.055f;
            var gateWidth = profile.GateFamily ==
                            CountyBuildingGateFamily.Gatehouse ? 0.54f :
                profile.GateFamily == CountyBuildingGateFamily.Wide
                    ? 0.42f : 0.28f;
            AddGoldenEnclosure(target, target, origin, rotation, wallHeight,
                wallThickness, gateWidth, profile.GateFamily, scaleX,
                scaleZ);
            foreach (var module in modulePlan.Modules)
            {
                var offset = new Vector3(module.OffsetX * scaleX, 0f,
                    module.OffsetZ * scaleZ);
                var width = module.Width * scaleX;
                var depth = module.Depth * scaleZ;
                var height = module.Height * scaleY;
                switch (module.Kind)
                {
                    case CountyBuildingModuleKind.Hall:
                    case CountyBuildingModuleKind.SideHouse:
                    case CountyBuildingModuleKind.LongWarehouse:
                    case CountyBuildingModuleKind.WorkshopShed:
                    case CountyBuildingModuleKind.Gatehouse:
                        AddGoldenHall(target, target, target, target, origin,
                            offset, width, depth, height, foundationHeight,
                            rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.OpenShed:
                        AddGoldenOpenShed(target, target, origin, offset,
                            width, depth, height, rotation, module.RoofShape);
                        break;
                    case CountyBuildingModuleKind.Tree:
                        target.AddTree(origin + RotateGoldenOffset(offset,
                            rotation), Mathf.Max(0.18f, height));
                        break;
                    default:
                        AddGoldenBox(target, origin,
                            offset + Vector3.up * height * 0.5f,
                            new Vector3(width, height, depth), rotation);
                        break;
                }
            }
        }

        private void AddDraftPath(CountyMeshAccumulator mesh,
            IReadOnlyList<PlanningCellCoord> path, float width, float lift)
        {
            if (path == null || path.Count < 2) return;
            mesh.AddRibbon(path.Select(item => CellCenter(item.Row,
                item.Column, lift)).ToArray(), width);
        }

        private Material CategoryMaterial(string id, Color color)
        {
            if (_categoryMaterials.TryGetValue(id, out var value)) return value;
            value = Material("County " + id, color, 1f);
            _categoryMaterials.Add(id, value);
            return value;
        }

        private GameObject CreateAccumulatorRenderer(string name,
            Transform parent, CountyMeshAccumulator accumulator,
            Material material, bool shadows)
        {
            var mesh = accumulator.CreateMesh(name);
            if (mesh == null) return null;
            OwnMesh(mesh);
            return CreateRenderer(name, parent, mesh, material, shadows);
        }

        private static GameObject CreateRenderer(string name, Transform parent,
            Mesh mesh, Material material, bool shadows)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.isStatic = true;
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = value.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows
                ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = true;
            return value;
        }

        private Mesh OwnMesh(Mesh mesh)
        {
            _ownedMeshes.Add(mesh);
            return mesh;
        }

        private static Vector3 RayPlaneIntersection(Ray ray, float height)
        {
            var denominator = ray.direction.y;
            if (Mathf.Abs(denominator) < 0.0001f) return ray.origin;
            var distance = (height - ray.origin.y) / denominator;
            return ray.GetPoint(Mathf.Max(0f, distance));
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                var filters = child.GetComponentsInChildren<MeshFilter>(true);
                foreach (var filter in filters)
                {
                    var mesh = filter.sharedMesh;
                    if (mesh != null && _ownedMeshes.Remove(mesh))
                        DestroyOwnedObject(mesh);
                }
                DestroyOwnedObject(child.gameObject);
            }
        }

        private static void DestroyOwnedObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }

        private void ReleasePresentation()
        {
            if (_worldRoot != null) DestroyOwnedObject(_worldRoot);
            _worldRoot = null;
            foreach (var mesh in _ownedMeshes) DestroyOwnedObject(mesh);
            _ownedMeshes.Clear();
            _modelFactory?.Dispose();
            _modelFactory = null;
            _modelResolver = null;
            foreach (var material in _ownedMaterials)
                DestroyOwnedObject(material);
            _ownedMaterials.Clear();
            _categoryMaterials.Clear();
            _layers.Clear();
            _roadLodRoots.Clear();
            _selectionRoot = null;
            _fallbackEvidenceRoot = null;
            _goldenBlockRoot = null;
            _citywideBuildingLanguageRoot = null;
            GoldenBlockPlan = null;
            CitywideBuildingLanguagePlan = null;
            CitywideContextFacilityCount = 0;
            CitywideBuildingLanguageModuleCount = 0;
            CitywideBuildingLanguageTriangleCount = 0;
            CitywideBuildingLanguageMaterialCount = 0;
            _built = false;
            Summary = null;
        }

        private void OnDestroy()
        {
            ReleasePresentation();
        }

        private sealed class FacilityBatchGroup
        {
            public FacilityBatchGroup(Material material, int chunkRow,
                int chunkColumn, string materialId)
            {
                Material = material;
                ChunkRow = chunkRow;
                ChunkColumn = chunkColumn;
                MaterialId = materialId;
            }

            public Material Material { get; }
            public int ChunkRow { get; }
            public int ChunkColumn { get; }
            public string MaterialId { get; }
            public List<CombineInstance> Instances { get; } =
                new List<CombineInstance>();
        }

        private sealed class FarAggregateBatch
        {
            public FarAggregateBatch(CountyFarAggregateKind kind, bool rural,
                int chunkRow, int chunkColumn)
            {
                Kind = kind;
                Rural = rural;
                ChunkRow = chunkRow;
                ChunkColumn = chunkColumn;
            }

            public CountyFarAggregateKind Kind { get; }
            public bool Rural { get; }
            public int ChunkRow { get; }
            public int ChunkColumn { get; }
            public CountyMeshAccumulator Bodies { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Roofs { get; } =
                new CountyMeshAccumulator();
        }

        private sealed class CitywideBuildingAccumulators
        {
            public CountyMeshAccumulator DomesticGround { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator WorkGround { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator FormalGround { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Walls { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Bodies { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Timber { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator RoofWarm { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator RoofDark { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator RoofWeathered { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Accents { get; } =
                new CountyMeshAccumulator();
            public CountyMeshAccumulator Vegetation { get; } =
                new CountyMeshAccumulator();

            public CountyMeshAccumulator Ground(
                CountyBuildingGroundTreatment treatment)
            {
                if (treatment == CountyBuildingGroundTreatment.DomesticEarth)
                    return DomesticGround;
                if (treatment == CountyBuildingGroundTreatment.CivicCourt)
                    return FormalGround;
                return WorkGround;
            }

            public CountyMeshAccumulator Roof(int variation)
            {
                if (variation == 0) return RoofWarm;
                return variation == 1 ? RoofDark : RoofWeathered;
            }
        }
    }

    internal sealed class CountyMeshAccumulator
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();

        public void AddHorizontalQuad(Vector3 center, float width, float depth)
        {
            var start = _vertices.Count;
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            _vertices.Add(center + new Vector3(-halfWidth, 0f, -halfDepth));
            _vertices.Add(center + new Vector3(-halfWidth, 0f, halfDepth));
            _vertices.Add(center + new Vector3(halfWidth, 0f, halfDepth));
            _vertices.Add(center + new Vector3(halfWidth, 0f, -halfDepth));
            AddQuadIndices(start, start + 1, start + 2, start + 3);
        }

        public void AddBox(Vector3 center, Vector3 size)
        {
            var half = size * 0.5f;
            var start = _vertices.Count;
            _vertices.Add(center + new Vector3(-half.x, -half.y, -half.z));
            _vertices.Add(center + new Vector3(half.x, -half.y, -half.z));
            _vertices.Add(center + new Vector3(half.x, -half.y, half.z));
            _vertices.Add(center + new Vector3(-half.x, -half.y, half.z));
            _vertices.Add(center + new Vector3(-half.x, half.y, -half.z));
            _vertices.Add(center + new Vector3(half.x, half.y, -half.z));
            _vertices.Add(center + new Vector3(half.x, half.y, half.z));
            _vertices.Add(center + new Vector3(-half.x, half.y, half.z));
            AddQuadIndices(start, start + 3, start + 2, start + 1);
            AddQuadIndices(start + 4, start + 5, start + 6, start + 7);
            AddQuadIndices(start, start + 4, start + 7, start + 3);
            AddQuadIndices(start + 1, start + 2, start + 6, start + 5);
            AddQuadIndices(start + 3, start + 7, start + 6, start + 2);
            AddQuadIndices(start, start + 1, start + 5, start + 4);
        }

        public void AddGabledRoof(Vector3 baseCenter, float width,
            float depth, float roofHeight, int rotationQuarterTurns)
        {
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            var points = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(0f, roofHeight, -halfDepth),
                new Vector3(0f, roofHeight, halfDepth)
            };
            var rotation = Quaternion.Euler(0f,
                (rotationQuarterTurns & 3) * 90f, 0f);
            var start = _vertices.Count;
            foreach (var point in points)
                _vertices.Add(baseCenter + rotation * point);
            // Front and back gables, then the two sloping roof planes.
            AddTriangle(start, start + 4, start + 1);
            AddTriangle(start + 2, start + 3, start + 5);
            AddQuadIndices(start, start + 2, start + 5, start + 4);
            AddQuadIndices(start + 1, start + 4, start + 5, start + 3);
        }

        public void AddHippedRoof(Vector3 baseCenter, float width,
            float depth, float roofHeight, int rotationQuarterTurns)
        {
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            var points = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth * 0.34f, roofHeight, 0f),
                new Vector3(halfWidth * 0.34f, roofHeight, 0f)
            };
            var rotation = Quaternion.Euler(0f,
                (rotationQuarterTurns & 3) * 90f, 0f);
            var start = _vertices.Count;
            foreach (var point in points)
                _vertices.Add(baseCenter + rotation * point);
            AddTriangle(start, start + 4, start + 1);
            AddTriangle(start + 1, start + 4, start + 5);
            AddTriangle(start + 1, start + 5, start + 2);
            AddTriangle(start + 2, start + 5, start + 3);
            AddTriangle(start + 3, start + 5, start + 4);
            AddTriangle(start + 3, start + 4, start);
        }

        public void AddRibbon(IReadOnlyList<Vector3> points, float width)
        {
            if (points == null || points.Count < 2) return;
            for (var index = 0; index < points.Count - 1; index++)
            {
                var first = points[index];
                var second = points[index + 1];
                var direction = second - first;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.000001f) continue;
                var perpendicular = Vector3.Cross(Vector3.up,
                    direction.normalized) * (width * 0.5f);
                var start = _vertices.Count;
                _vertices.Add(first - perpendicular);
                _vertices.Add(first + perpendicular);
                _vertices.Add(second + perpendicular);
                _vertices.Add(second - perpendicular);
                AddQuadIndices(start, start + 1, start + 2, start + 3);
            }
        }

        public void AddDisc(Vector3 center, float radius, int segments)
        {
            if (segments < 3) return;
            var start = _vertices.Count;
            _vertices.Add(center);
            for (var index = 0; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                _vertices.Add(center + new Vector3(Mathf.Cos(angle) * radius,
                    0f, Mathf.Sin(angle) * radius));
            }
            for (var index = 0; index < segments; index++)
            {
                _triangles.Add(start);
                _triangles.Add(start + index + 1);
                _triangles.Add(start + index + 2);
            }
        }

        public void AddTree(Vector3 baseCenter, float height)
        {
            AddBox(baseCenter + Vector3.up * height * 0.19f,
                new Vector3(height * 0.10f, height * 0.38f,
                    height * 0.10f));
            var center = baseCenter + Vector3.up * height * 0.62f;
            var half = height * 0.30f;
            var start = _vertices.Count;
            _vertices.Add(center + Vector3.up * height * 0.38f);
            _vertices.Add(center + new Vector3(-half, -height * 0.28f,
                -half));
            _vertices.Add(center + new Vector3(half, -height * 0.28f,
                -half));
            _vertices.Add(center + new Vector3(half, -height * 0.28f,
                half));
            _vertices.Add(center + new Vector3(-half, -height * 0.28f,
                half));
            _triangles.AddRange(new[]
            {
                start, start + 1, start + 2,
                start, start + 2, start + 3,
                start, start + 3, start + 4,
                start, start + 4, start + 1,
                start + 1, start + 4, start + 3,
                start + 1, start + 3, start + 2
            });
        }

        public Mesh CreateMesh(string name)
        {
            if (_vertices.Count == 0 || _triangles.Count == 0) return null;
            var mesh = new Mesh
            {
                name = name,
                indexFormat = _vertices.Count > ushort.MaxValue
                    ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddQuadIndices(int a, int b, int c, int d)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
            _triangles.Add(a);
            _triangles.Add(c);
            _triangles.Add(d);
        }

        private void AddTriangle(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }
    }
}
