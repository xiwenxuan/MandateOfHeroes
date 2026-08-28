using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Mandate.Presentation
{
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

        private LuoyangHumanScaleStreamingRuntime(
            LuoyangHumanScaleLocalMapPlan plan,
            Func<double, double, Vector3> positionResolver,
            Transform parent)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _positionResolver = positionResolver ??
                throw new ArgumentNullException(nameof(positionResolver));
            _session = new LuoyangHumanScaleStreamingSession(plan);
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

        public static LuoyangHumanScaleStreamingRuntime Build(
            LuoyangHumanScaleLocalMapPlan plan,
            Func<double, double, Vector3> positionResolver,
            ulong centerCellId64, Transform parent = null)
        {
            var runtime = new LuoyangHumanScaleStreamingRuntime(plan,
                positionResolver, parent);
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
            Destroy(Root);
            Destroy(_terrainMaterial);
            Destroy(_roadMaterial);
            Destroy(_blockingMaterial);
            Destroy(_accessMaterial);
        }

        private GameObject BuildCell(ulong cellId64)
        {
            var space = _plan.LocalSpacesByCellId[cellId64];
            var root = new GameObject("LOCAL_CELL_" + cellId64);
            root.transform.SetParent(Root.transform, false);
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

        private void BuildRoads(Transform parent, ulong cellId64)
        {
            var edges = _plan.Edges.Where(item => item.Geometry.Any(point =>
                point.CellId64 == cellId64)).ToArray();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            foreach (var edge in edges)
            {
                for (var index = 1; index < edge.Geometry.Count; index++)
                {
                    var first = edge.Geometry[index - 1];
                    var second = edge.Geometry[index];
                    if (first.CellId64 != cellId64 &&
                        second.CellId64 != cellId64) continue;
                    AddRibbon(vertices, triangles,
                        _positionResolver(first.GlobalEastingMetres,
                            first.GlobalNorthingMetres),
                        _positionResolver(second.GlobalEastingMetres,
                            second.GlobalNorthingMetres),
                        Math.Max(0.08f, edge.WidthCentimetres / 100f /
                            _plan.WorldScale.WorldMetresPerUnityUnit));
                }
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
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "LOCAL_FOOTPRINT_" + footprint.FacilityId;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = _positionResolver(
                space.OriginEastingMetres + footprint.CenterEastMetres,
                space.OriginNorthingMetres + footprint.CenterNorthMetres) +
                Vector3.up * 0.04f;
            gameObject.transform.rotation = Quaternion.Euler(0f,
                footprint.RotationMilliDegrees / 1000f, 0f);
            gameObject.transform.localScale = new Vector3(
                (float)(footprint.HalfExtentEastMetres * 2d /
                    _plan.WorldScale.WorldMetresPerUnityUnit), 0.08f,
                (float)(footprint.HalfExtentNorthMetres * 2d /
                    _plan.WorldScale.WorldMetresPerUnityUnit));
            gameObject.GetComponent<MeshRenderer>().sharedMaterial =
                _blockingMaterial;
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
                gameObject.AddComponent<LuoyangLocalTargetProxy>().Initialize(
                    kindId, footprint.FacilityId, access.AccessNodeId,
                    footprint.CellId64);
            }
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
                .Where(item => item != null && item.name.StartsWith(
                    "LOCAL_ROADS_", StringComparison.Ordinal))
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
