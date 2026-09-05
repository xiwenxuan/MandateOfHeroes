using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
    public sealed class LuoyangNearfieldClusterHook : MonoBehaviour
    {
        public string FacilityId { get; private set; }
        public string VisualProfileId { get; private set; }
        public string ClusterHookId { get; private set; }

        public void Initialize(LuoyangNearfieldVisualProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            FacilityId = profile.FacilityId;
            VisualProfileId = profile.ProfileId;
            ClusterHookId = profile.ClusterHookId;
        }
    }

    public sealed class LuoyangNearfieldVisualOptions
    {
        public bool ShowDebugCellGround { get; set; } = true;
        public bool ShowDebugFacilityFootprints { get; set; } = true;
        public bool ShowUrbanPlaceholders { get; set; }

        public static LuoyangNearfieldVisualOptions PlayerDefault() =>
            new LuoyangNearfieldVisualOptions
            {
                ShowDebugCellGround = false,
                ShowDebugFacilityFootprints = false,
                ShowUrbanPlaceholders = true
            };
    }

    public sealed class LuoyangLocalTargetProxy : MonoBehaviour
    {
        public string KindId { get; private set; }
        public string FacilityId { get; private set; }
        public string LocalNodeId { get; private set; }
        public ulong CellId64 { get; private set; }

        public void Initialize(string kindId, string facilityId,
            string localNodeId, ulong cellId64)
        {
            KindId = new StableId(kindId).Value;
            FacilityId = new StableId(facilityId).Value;
            LocalNodeId = new StableId(localNodeId).Value;
            CellId64 = cellId64;
        }
    }

    public sealed class LuoyangHumanScaleStreamingRuntime : IDisposable
    {
        public const string RootName =
            "Luoyang Human Scale Streaming V1";
        private readonly LuoyangHumanScaleLocalMapPlan _plan;
        private readonly LuoyangHumanScaleStreamingSession _session;
        private readonly Func<double, double, Vector3> _positionResolver;
        private readonly Dictionary<ulong, GameObject> _rootsByCell =
            new Dictionary<ulong, GameObject>();
        private readonly Material _terrainMaterial;
        private readonly Material _roadMaterial;
        private readonly Material _blockingMaterial;
        private readonly Material _accessMaterial;
        private readonly Material _nearfieldGroundMaterial;
        private readonly Material _roofMaterial;
        private readonly Material _wallMaterial;
        private readonly Material[] _buildingMaterials;
        private readonly bool _showFacilityFootprints;
        private readonly LuoyangNearfieldVisualOptions _visualOptions;
        private readonly string _focusFacilityId;
        private GameObject _nearfieldUrbanContextRoot;
        private LuoyangNearfieldUrbanContextProjection
            _nearfieldUrbanContextProjection;

        private LuoyangHumanScaleStreamingRuntime(
            LuoyangHumanScaleLocalMapPlan plan,
            Func<double, double, Vector3> positionResolver,
            Transform parent, bool showFacilityFootprints,
            LuoyangNearfieldVisualOptions visualOptions,
            string focusFacilityId)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _positionResolver = positionResolver ??
                throw new ArgumentNullException(nameof(positionResolver));
            _session = new LuoyangHumanScaleStreamingSession(plan);
            _showFacilityFootprints = showFacilityFootprints;
            _focusFacilityId = string.IsNullOrWhiteSpace(focusFacilityId)
                ? null : new StableId(focusFacilityId).Value;
            _visualOptions = visualOptions ??
                new LuoyangNearfieldVisualOptions
                {
                    ShowDebugCellGround = true,
                    ShowDebugFacilityFootprints = showFacilityFootprints,
                    ShowUrbanPlaceholders = false
                };
            Root = new GameObject(RootName);
            if (parent != null) Root.transform.SetParent(parent, false);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard") ??
                         Shader.Find("Sprites/Default");
            _terrainMaterial = Material(shader,
                new Color(0.31f, 0.38f, 0.22f, 1f));
            _roadMaterial = Material(shader,
                new Color(0.46f, 0.38f, 0.27f, 1f));
            _blockingMaterial = Material(shader,
                new Color(0.48f, 0.20f, 0.13f, 0.70f));
            _accessMaterial = Material(shader,
                new Color(0.82f, 0.68f, 0.20f, 1f));
            _nearfieldGroundMaterial = Material(shader,
                new Color(0.34f, 0.36f, 0.22f, 1f));
            _roofMaterial = Material(shader,
                new Color(0.22f, 0.12f, 0.08f, 1f));
            _wallMaterial = Material(shader,
                new Color(0.56f, 0.43f, 0.28f, 1f));
            _buildingMaterials = new[]
            {
                Material(shader, new Color(0.62f, 0.50f, 0.31f, 1f)),
                Material(shader, new Color(0.55f, 0.43f, 0.28f, 1f)),
                Material(shader, new Color(0.68f, 0.57f, 0.37f, 1f)),
                Material(shader, new Color(0.48f, 0.39f, 0.25f, 1f))
            };
            if (!_visualOptions.ShowDebugCellGround)
                BuildSeamlessNearfieldGround();
        }

        public GameObject Root { get; }
        public int ResidentCellCount => _rootsByCell.Count;
        public int ResidentGameObjectCount => Root == null ? 0 :
            Root.GetComponentsInChildren<Transform>(true).Length;
        public int ResidentMeshCount => Root == null ? 0 :
            Root.GetComponentsInChildren<MeshFilter>(true).Length;
        public int ResidentColliderCount => Root == null ? 0 :
            Root.GetComponentsInChildren<Collider>(true).Length;
        public long LastLoadMilliseconds { get; private set; }
        public long LastUnloadMilliseconds { get; private set; }
        public long LastManagedMemoryBytes { get; private set; }
        public string MapAssetHash => _session.MapAssetHash;
        public bool DebugCellGroundVisible =>
            _visualOptions.ShowDebugCellGround;
        public int NearfieldContextFacilityCount =>
            _nearfieldUrbanContextProjection?.Facilities.Count ?? 0;
        public ulong NearfieldContextStableSummary =>
            _nearfieldUrbanContextProjection?.StableSummary ?? 0UL;

        public static LuoyangHumanScaleStreamingRuntime Build(
            LuoyangHumanScaleLocalMapPlan plan,
            Func<double, double, Vector3> positionResolver,
            ulong centerCellId64, Transform parent = null,
            bool showFacilityFootprints = true,
            LuoyangNearfieldVisualOptions visualOptions = null,
            string focusFacilityId = null)
        {
            var runtime = new LuoyangHumanScaleStreamingRuntime(plan,
                positionResolver, parent, showFacilityFootprints,
                visualOptions, focusFacilityId);
            runtime.MoveWindow(centerCellId64);
            return runtime;
        }

        public LuoyangLocalStreamingUpdate MoveWindow(ulong centerCellId64)
        {
            var update = _session.MoveWindow(centerCellId64);
            var timer = Stopwatch.StartNew();
            foreach (var cellId in update.UnloadedCellIds)
            {
                if (!_rootsByCell.TryGetValue(cellId, out var root)) continue;
                _rootsByCell.Remove(cellId);
                DestroyCell(root);
            }
            timer.Stop();
            LastUnloadMilliseconds = timer.ElapsedMilliseconds;
            timer.Restart();
            foreach (var cellId in update.LoadedCellIds)
                _rootsByCell.Add(cellId, BuildCell(cellId));
            RebuildNearfieldUrbanContext(centerCellId64);
            timer.Stop();
            LastLoadMilliseconds = timer.ElapsedMilliseconds;
            LastManagedMemoryBytes = GC.GetTotalMemory(false);
            return update;
        }

        public bool TryResolveProxy(Collider collider,
            out LuoyangResolvedLocalTarget target)
        {
            target = null;
            var proxy = collider == null ? null :
                collider.GetComponentInParent<LuoyangLocalTargetProxy>();
            if (proxy == null) return false;
            target = new LuoyangLocalTargetResolver(_plan).ResolveFacility(
                proxy.FacilityId);
            return target.IsValid;
        }

        public void Dispose()
        {
            foreach (var root in _rootsByCell.Values) DestroyCell(root);
            _rootsByCell.Clear();
            DestroyCell(_nearfieldUrbanContextRoot);
            _nearfieldUrbanContextRoot = null;
            _nearfieldUrbanContextProjection = null;
            Destroy(Root);
            Destroy(_terrainMaterial);
            Destroy(_roadMaterial);
            Destroy(_blockingMaterial);
            Destroy(_accessMaterial);
            Destroy(_nearfieldGroundMaterial);
            Destroy(_roofMaterial);
            Destroy(_wallMaterial);
            foreach (var material in _buildingMaterials) Destroy(material);
        }

        private GameObject BuildCell(ulong cellId64)
        {
            var space = _plan.LocalSpacesByCellId[cellId64];
            var root = new GameObject("LOCAL_CELL_" + cellId64);
            root.transform.SetParent(Root.transform, false);
            if (_visualOptions.ShowDebugCellGround)
                BuildTerrain(root.transform, space);
            BuildRoads(root.transform, cellId64);
            foreach (var footprint in _plan.Footprints.Where(item =>
                         item.CellId64 == cellId64 && item.BlocksPedestrian))
                BuildFootprint(root.transform, space, footprint);
            foreach (var access in _plan.Entrances.Where(item =>
                         item.CellId64 == cellId64))
                BuildAccess(root.transform, space, access);
            return root;
        }

        private void RebuildNearfieldUrbanContext(ulong centerCellId64)
        {
            DestroyCell(_nearfieldUrbanContextRoot);
            _nearfieldUrbanContextRoot = null;
            _nearfieldUrbanContextProjection = null;
            if (!_visualOptions.ShowUrbanPlaceholders) return;
            var focusFacilityId = _focusFacilityId;
            if (string.IsNullOrWhiteSpace(focusFacilityId))
                focusFacilityId = _plan.Footprints
                    .Where(item => item.CellId64 == centerCellId64)
                    .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                    .Select(item => item.FacilityId).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(focusFacilityId) ||
                !_plan.FootprintsByFacilityId.TryGetValue(focusFacilityId,
                    out var focusFootprint) ||
                !_plan.LocalSpacesByCellId.TryGetValue(
                    focusFootprint.CellId64, out var focusSpace)) return;

            _nearfieldUrbanContextProjection =
                LuoyangNearfieldUrbanContextProjection.Create(_plan,
                    focusFacilityId);
            _nearfieldUrbanContextRoot = new GameObject(
                "LUOYANG_NEARFIELD_URBAN_CONTEXT_V1");
            _nearfieldUrbanContextRoot.transform.SetParent(Root.transform,
                false);
            var focusPosition = _positionResolver(
                focusSpace.OriginEastingMetres +
                focusFootprint.CenterEastMetres,
                focusSpace.OriginNorthingMetres +
                focusFootprint.CenterNorthMetres);
            BuildNearfieldStreetContext(
                _nearfieldUrbanContextRoot.transform, focusPosition);
            foreach (var projected in _nearfieldUrbanContextProjection
                         .Facilities)
            {
                var footprint = _plan.FootprintsByFacilityId[
                    projected.FacilityId];
                if (projected.IsFocusFacility &&
                    footprint.BlocksPedestrian) continue;
                BuildNearfieldContextFacility(
                    _nearfieldUrbanContextRoot.transform, focusPosition,
                    projected, footprint);
            }
        }

        private void BuildNearfieldStreetContext(Transform parent,
            Vector3 focusPosition)
        {
            var root = new GameObject("NEARFIELD_STREET_CONTEXT");
            root.transform.SetParent(parent, false);
            root.transform.position = focusPosition;
            AddBox(root.transform, "NEARFIELD_STREET_EAST_WEST",
                new Vector3(0f, 0.005f, 4.1f),
                new Vector3(30f, 0.01f, 2.1f), _roadMaterial, false);
            AddBox(root.transform, "NEARFIELD_STREET_NORTH_SOUTH",
                new Vector3(4.25f, 0.006f, 0f),
                new Vector3(2.1f, 0.012f, 28f), _roadMaterial, false);
        }

        private void BuildNearfieldContextFacility(Transform parent,
            Vector3 focusPosition,
            LuoyangNearfieldContextFacilityProjection projected,
            LuoyangFacilityLocalFootprint footprint)
        {
            var profile = LuoyangNearfieldVisualProfileResolver.Resolve(_plan,
                projected.FacilityId);
            if (!profile.HasStructuralPlaceholder) return;
            var root = new GameObject("NEARFIELD_CONTEXT_FACILITY_" +
                                      projected.FacilityId);
            root.transform.SetParent(parent, false);
            root.transform.position = focusPosition + new Vector3(
                (float)projected.VisualEastUnityUnits, 0f,
                (float)projected.VisualNorthUnityUnits);
            root.transform.rotation = Quaternion.Euler(0f,
                projected.RotationMilliDegrees / 1000f, 0f);
            root.AddComponent<LuoyangNearfieldClusterHook>()
                .Initialize(profile);
            BuildStructuralPlaceholder(root.transform, footprint, profile,
                false);
        }

        private void BuildTerrain(Transform parent,
            LuoyangHumanScaleLocalSpace space)
        {
            var first = _positionResolver(space.OriginEastingMetres,
                space.OriginNorthingMetres);
            var opposite = _positionResolver(
                space.OriginEastingMetres + space.WidthMetres,
                space.OriginNorthingMetres + space.HeightMetres);
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "LOCAL_TERRAIN_" + space.ParentCellId64;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = new Vector3(
                (first.x + opposite.x) * 0.5f, Math.Min(first.y, opposite.y),
                (first.z + opposite.z) * 0.5f);
            gameObject.transform.localScale = new Vector3(
                Math.Abs(opposite.x - first.x), 0.01f,
                Math.Abs(opposite.z - first.z));
            gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                _terrainMaterial;
        }

        private void BuildSeamlessNearfieldGround()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "LUOYANG_NEARFIELD_SEAMLESS_GROUND_V1";
            gameObject.transform.SetParent(Root.transform, false);
            gameObject.transform.position = new Vector3(0f, -0.015f, 0f);
            gameObject.transform.localScale = new Vector3(24_000f, 0.02f,
                24_000f);
            gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                _nearfieldGroundMaterial;
        }

        private void BuildRoads(Transform parent, ulong cellId64)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            if (_plan.CellTraversal == null ||
                !_plan.CellTraversal.ProfilesByCellId.TryGetValue(cellId64,
                    out var profile)) return;
            var isRoadLike = string.Equals(profile.FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Road,
                    StringComparison.Ordinal) ||
                string.Equals(profile.FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal) ||
                string.Equals(profile.FacilityCapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal);
            if (!isRoadLike) return;
            var space = _plan.LocalSpacesByCellId[cellId64];
            var center = _positionResolver(
                space.OriginEastingMetres + space.WidthMetres * 0.5d,
                space.OriginNorthingMetres + space.HeightMetres * 0.5d);
            foreach (var port in profile.Ports.Where(item => item.Enabled &&
                         (!string.Equals(profile.FacilityCapabilityId,
                              FacilitySpatialCapabilityIds.Road,
                              StringComparison.Ordinal) ||
                          string.Equals(item.RoleId,
                              CellTraversalPortRoleIds.RoadConnection,
                              StringComparison.Ordinal))))
            {
                var east = CellTraversalDirections.EastCentimetres(
                    port.Direction) / 100d;
                var north = CellTraversalDirections.NorthCentimetres(
                    port.Direction) / 100d;
                AddRibbon(vertices, triangles, center, _positionResolver(
                        space.OriginEastingMetres + east,
                        space.OriginNorthingMetres + north),
                    Math.Max(0.08f, port.WidthCentimetres / 100f /
                        _plan.WorldScale.WorldMetresPerUnityUnit));
            }
            if (vertices.Count == 0) return;
            var mesh = new Mesh { name = "LOCAL_ROADS_" + cellId64 };
            if (vertices.Count > 65_535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var gameObject = new GameObject(mesh.name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial =
                _roadMaterial;
            gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildFootprint(Transform parent,
            LuoyangHumanScaleLocalSpace space,
            LuoyangFacilityLocalFootprint footprint)
        {
            var profile = LuoyangNearfieldVisualProfileResolver.Resolve(_plan,
                footprint.FacilityId);
            var root = new GameObject("NEARFIELD_FACILITY_" +
                                      footprint.FacilityId);
            root.transform.SetParent(parent, false);
            root.transform.position = _positionResolver(
                space.OriginEastingMetres + footprint.CenterEastMetres,
                space.OriginNorthingMetres + footprint.CenterNorthMetres);
            root.transform.rotation = Quaternion.Euler(0f,
                footprint.RotationMilliDegrees / 1000f, 0f);
            root.AddComponent<LuoyangNearfieldClusterHook>().Initialize(profile);
            var capability = _plan.FacilityCapabilitiesByFacilityId[
                footprint.FacilityId];
            if (capability.RequiresAccess &&
                _plan.EntrancesByFacilityId.TryGetValue(
                    footprint.FacilityId, out var access))
            {
                var kindId = string.Equals(capability.CapabilityId,
                        FacilitySpatialCapabilityIds.Gate,
                        StringComparison.Ordinal)
                    ? LuoyangLocalTargetKindIds.Gate
                    : string.Equals(capability.CapabilityId,
                        FacilitySpatialCapabilityIds.Bridge,
                        StringComparison.Ordinal)
                        ? LuoyangLocalTargetKindIds.Bridge
                        : LuoyangLocalTargetKindIds.Facility;
                root.AddComponent<LuoyangLocalTargetProxy>().Initialize(
                    kindId, footprint.FacilityId, access.AccessNodeId,
                    footprint.CellId64);
            }

            var width = (float)(footprint.HalfExtentEastMetres * 2d /
                _plan.WorldScale.WorldMetresPerUnityUnit);
            var depth = (float)(footprint.HalfExtentNorthMetres * 2d /
                _plan.WorldScale.WorldMetresPerUnityUnit);
            if (_visualOptions.ShowDebugFacilityFootprints ||
                _showFacilityFootprints)
                AddBox(root.transform, "DEBUG_FOOTPRINT", Vector3.up * 0.04f,
                    new Vector3(width, 0.08f, depth), _blockingMaterial);
            if (!_visualOptions.ShowUrbanPlaceholders ||
                !profile.HasStructuralPlaceholder) return;

            BuildStructuralPlaceholder(root.transform, footprint, profile,
                true);
        }

        private void BuildStructuralPlaceholder(Transform root,
            LuoyangFacilityLocalFootprint footprint,
            LuoyangNearfieldVisualProfile profile, bool collidersEnabled)
        {
            var width = (float)(footprint.HalfExtentEastMetres * 2d /
                _plan.WorldScale.WorldMetresPerUnityUnit);
            var depth = (float)(footprint.HalfExtentNorthMetres * 2d /
                _plan.WorldScale.WorldMetresPerUnityUnit);

            var height = Mathf.Max(0.28f,
                profile.HeightCentimetres / 100f /
                _plan.WorldScale.WorldMetresPerUnityUnit);
            var isWall = string.Equals(profile.CapabilityId,
                FacilitySpatialCapabilityIds.Wall,
                StringComparison.Ordinal);
            var isGate = string.Equals(profile.CapabilityId,
                FacilitySpatialCapabilityIds.Gate,
                StringComparison.Ordinal);
            var isBridge = string.Equals(profile.CapabilityId,
                FacilitySpatialCapabilityIds.Bridge,
                StringComparison.Ordinal);
            var wallLike = isWall || isGate || isBridge;
            var bodyWidth = isWall
                ? Mathf.Clamp(width, 5f, 16f)
                : isGate ? Mathf.Clamp(width, 4f, 8f)
                : isBridge ? Mathf.Clamp(width, 2.5f, 5f)
                : Mathf.Clamp(width * 0.24f, 1.4f, 3.8f);
            var bodyDepth = isWall
                ? Mathf.Clamp(depth, 0.7f, 1.5f)
                : isGate ? Mathf.Clamp(depth, 1.5f, 3f)
                : isBridge ? Mathf.Clamp(depth, 5f, 14f)
                : Mathf.Clamp(depth * 0.22f, 1.1f, 2.4f);
            var material = wallLike ? _wallMaterial : _buildingMaterials[
                profile.StableVariantIndex % _buildingMaterials.Length];
            var body = AddBox(root.transform, "BUILDING_BODY",
                Vector3.up * (height * 0.5f),
                new Vector3(bodyWidth, height, bodyDepth), material,
                collidersEnabled);
            AddBox(body.transform, "BUILDING_ROOF",
                new Vector3(0f, 0.55f, 0f),
                new Vector3(1.08f, 0.10f / Mathf.Max(height, 0.01f), 1.10f),
                _roofMaterial, collidersEnabled);
            if (!wallLike)
            {
                var annexHeight = height * 0.62f;
                AddBox(root.transform, "BUILDING_ANNEX",
                    new Vector3(bodyWidth * 0.72f, annexHeight * 0.5f,
                        -bodyDepth * 0.54f),
                    new Vector3(Mathf.Clamp(bodyWidth * 0.42f, 0.8f, 1.7f),
                        annexHeight,
                        Mathf.Clamp(bodyDepth * 0.44f, 0.65f, 1.25f)),
                    material, collidersEnabled);
                BuildCourtyardPlaceholder(root.transform, bodyWidth,
                    bodyDepth, collidersEnabled);
            }
        }

        private void BuildCourtyardPlaceholder(Transform parent,
            float bodyWidth, float bodyDepth, bool collidersEnabled)
        {
            var yardWidth = Mathf.Clamp(bodyWidth + 2.4f, 3.8f, 7.2f);
            var yardDepth = Mathf.Clamp(bodyDepth + 2.2f, 3.4f, 6.2f);
            const float wallHeight = 0.16f;
            const float wallThickness = 0.12f;
            var sideLength = Mathf.Max(0.8f, yardDepth - wallThickness * 2f);
            AddBox(parent, "COURTYARD_WALL_LEFT",
                new Vector3(-yardWidth * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, sideLength),
                _wallMaterial, collidersEnabled);
            AddBox(parent, "COURTYARD_WALL_RIGHT",
                new Vector3(yardWidth * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, sideLength),
                _wallMaterial, collidersEnabled);
            AddBox(parent, "COURTYARD_WALL_REAR",
                new Vector3(0f, wallHeight * 0.5f, yardDepth * 0.5f),
                new Vector3(yardWidth, wallHeight, wallThickness),
                _wallMaterial, collidersEnabled);
            var frontSegment = Mathf.Max(0.7f, (yardWidth - 1.1f) * 0.5f);
            var frontX = (frontSegment + 1.1f) * 0.5f;
            AddBox(parent, "COURTYARD_WALL_FRONT_LEFT",
                new Vector3(-frontX, wallHeight * 0.5f,
                    -yardDepth * 0.5f),
                new Vector3(frontSegment, wallHeight, wallThickness),
                _wallMaterial, collidersEnabled);
            AddBox(parent, "COURTYARD_WALL_FRONT_RIGHT",
                new Vector3(frontX, wallHeight * 0.5f,
                    -yardDepth * 0.5f),
                new Vector3(frontSegment, wallHeight, wallThickness),
                _wallMaterial, collidersEnabled);
        }

        private void BuildAccess(Transform parent,
            LuoyangHumanScaleLocalSpace space,
            LuoyangFacilityLocalEntrance access)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = "LOCAL_ACCESS_" + access.Id;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = _positionResolver(
                space.OriginEastingMetres + access.EastMetres,
                space.OriginNorthingMetres + access.NorthMetres) +
                Vector3.up * 0.08f;
            gameObject.transform.localScale = Vector3.one * 0.12f;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                _accessMaterial;
            var capability = _plan.FacilityCapabilitiesByFacilityId[
                access.FacilityId];
            var kindId = string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Gate,
                    StringComparison.Ordinal)
                ? LuoyangLocalTargetKindIds.Gate
                : string.Equals(capability.CapabilityId,
                    FacilitySpatialCapabilityIds.Bridge,
                    StringComparison.Ordinal)
                    ? LuoyangLocalTargetKindIds.Bridge
                    : LuoyangLocalTargetKindIds.Facility;
            gameObject.AddComponent<LuoyangLocalTargetProxy>().Initialize(
                kindId, access.FacilityId, access.AccessNodeId,
                access.CellId64);
            BuildAccessLane(parent, space, access);
        }

        private void BuildAccessLane(Transform parent,
            LuoyangHumanScaleLocalSpace space,
            LuoyangFacilityLocalEntrance access)
        {
            var start = _positionResolver(
                space.OriginEastingMetres + access.EastMetres,
                space.OriginNorthingMetres + access.NorthMetres);
            var center = _positionResolver(
                space.OriginEastingMetres + space.WidthMetres * 0.5d,
                space.OriginNorthingMetres + space.HeightMetres * 0.5d);
            var direction = center - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            var end = start + direction.normalized * Mathf.Min(3.5f,
                direction.magnitude);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddRibbon(vertices, triangles, start, end, 0.32f);
            var mesh = new Mesh { name = "LOCAL_ACCESS_LANE_" + access.Id };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var value = new GameObject(mesh.name);
            value.transform.SetParent(parent, false);
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            value.AddComponent<MeshRenderer>().sharedMaterial = _roadMaterial;
            value.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static GameObject AddBox(Transform parent, string name,
            Vector3 localPosition, Vector3 localScale, Material material,
            bool colliderEnabled = true)
        {
            var value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            value.transform.localScale = localScale;
            value.GetComponent<MeshRenderer>().sharedMaterial = material;
            var collider = value.GetComponent<Collider>();
            if (collider != null) collider.enabled = colliderEnabled;
            return value;
        }

        private static void AddRibbon(ICollection<Vector3> vertices,
            ICollection<int> triangles, Vector3 first, Vector3 second,
            float width)
        {
            var direction = new Vector3(second.x - first.x, 0f,
                second.z - first.z);
            if (direction.sqrMagnitude < 0.000001f) return;
            direction.Normalize();
            var side = new Vector3(-direction.z, 0f, direction.x) *
                       (width * 0.5f);
            var start = vertices.Count;
            vertices.Add(first - side + Vector3.up * 0.015f);
            vertices.Add(first + side + Vector3.up * 0.015f);
            vertices.Add(second - side + Vector3.up * 0.015f);
            vertices.Add(second + side + Vector3.up * 0.015f);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Material Material(Shader shader, Color color) =>
            new Material(shader) { color = color };

        private static void DestroyCell(GameObject root)
        {
            if (root == null) return;
            var generatedMeshes = root.GetComponentsInChildren<MeshFilter>(
                    true)
                .Select(item => item.sharedMesh)
                .Where(item => item != null &&
                    (item.name.StartsWith("LOCAL_ROADS_",
                         StringComparison.Ordinal) ||
                     item.name.StartsWith("LOCAL_ACCESS_LANE_",
                         StringComparison.Ordinal)))
                .Distinct()
                .ToArray();
            Destroy(root);
            foreach (var mesh in generatedMeshes) Destroy(mesh);
        }

        private static void Destroy(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
