using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mandate.Presentation
{
    public sealed class LuoyangFacilitySelectionProxyInstance : MonoBehaviour
    {
        public string ProxyId { get; private set; }
        public string FacilityId { get; private set; }
        public ulong CellId64 { get; private set; }
        public string CollisionProfileId { get; private set; }

        public void Initialize(LuoyangFacilitySelectionProxy proxy)
        {
            if (proxy == null) throw new ArgumentNullException(nameof(proxy));
            ProxyId = proxy.ProxyId;
            FacilityId = proxy.FacilityId;
            CellId64 = proxy.CellId64;
            CollisionProfileId = proxy.CollisionProfileId;
        }
    }

    public sealed class LuoyangFacilityInteractionNavigationRuntime : IDisposable
    {
        public const string RootName =
            "Luoyang Facility Selection Collision Navigation V1";
        public const string NavigationOverlayName =
            "Luoyang Authored Road Navigation Overlay V1";
        public const string ModeledConnectorOverlayName =
            "Luoyang Modeled Road Connector Overlay V1";
        public const string BlockedPassageOverlayName =
            "Luoyang Blocked Passage Overlay V1";
        public const string DamagedPassageOverlayName =
            "Luoyang Damaged Passage Overlay V1";
        public const string SelectionHighlightName =
            "Luoyang Selected Facility Highlight V1";

        private readonly Dictionary<string,
            LuoyangFacilitySelectionProxyInstance> _instancesByFacilityId;
        private readonly Material _navigationMaterial;
        private readonly Material _modeledConnectorMaterial;
        private readonly Material _blockedPassageMaterial;
        private readonly Material _damagedPassageMaterial;
        private readonly Material _selectionMaterial;
        private readonly Mesh _navigationMesh;
        private readonly Mesh _modeledConnectorMesh;
        private readonly Mesh _blockedPassageMesh;
        private readonly Mesh _damagedPassageMesh;
        private readonly Mesh _selectionMesh;
        private readonly MeshRenderer _blockedPassageRenderer;
        private readonly MeshRenderer _damagedPassageRenderer;
        private readonly MeshRenderer _selectionRenderer;

        private LuoyangFacilityInteractionNavigationRuntime(GameObject root,
            Dictionary<string, LuoyangFacilitySelectionProxyInstance>
                instancesByFacilityId,
            Material navigationMaterial, Material modeledConnectorMaterial,
            Material blockedPassageMaterial, Material damagedPassageMaterial,
            Material selectionMaterial, Mesh navigationMesh,
            Mesh modeledConnectorMesh, Mesh blockedPassageMesh,
            Mesh damagedPassageMesh, Mesh selectionMesh,
            MeshRenderer blockedPassageRenderer,
            MeshRenderer damagedPassageRenderer,
            MeshRenderer selectionRenderer, int residentNavigationEdgeCount,
            int residentModeledConnectorEdgeCount)
        {
            Root = root;
            _instancesByFacilityId = instancesByFacilityId;
            _navigationMaterial = navigationMaterial;
            _modeledConnectorMaterial = modeledConnectorMaterial;
            _blockedPassageMaterial = blockedPassageMaterial;
            _damagedPassageMaterial = damagedPassageMaterial;
            _selectionMaterial = selectionMaterial;
            _navigationMesh = navigationMesh;
            _modeledConnectorMesh = modeledConnectorMesh;
            _blockedPassageMesh = blockedPassageMesh;
            _damagedPassageMesh = damagedPassageMesh;
            _selectionMesh = selectionMesh;
            _blockedPassageRenderer = blockedPassageRenderer;
            _damagedPassageRenderer = damagedPassageRenderer;
            _selectionRenderer = selectionRenderer;
            ResidentNavigationEdgeCount = residentNavigationEdgeCount;
            ResidentModeledConnectorEdgeCount = residentModeledConnectorEdgeCount;
        }

        public GameObject Root { get; }
        public int ResidentProxyCount => _instancesByFacilityId.Count;
        public int ResidentNavigationEdgeCount { get; }
        public int ResidentModeledConnectorEdgeCount { get; }
        public int ResidentPassageMarkerCount { get; private set; }

        public static LuoyangFacilityInteractionNavigationRuntime Build(
            LuoyangFacilityInteractionNavigationPlan plan,
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession,
            IReadOnlyList<LuoyangBuildingPerformanceFacility> residents,
            Func<LuoyangBuildingPerformanceFacility, Vector3> positionResolver,
            Func<LuoyangBuildingPerformanceFacility, float> rotationResolver,
            Func<int, int, Vector3> cellPositionResolver,
            float horizontalMetresPerUnit, float verticalMetresPerUnit)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (refinementPlan == null)
                throw new ArgumentNullException(nameof(refinementPlan));
            if (passageSession == null)
                throw new ArgumentNullException(nameof(passageSession));
            if (residents == null) throw new ArgumentNullException(nameof(residents));
            if (positionResolver == null)
                throw new ArgumentNullException(nameof(positionResolver));
            if (rotationResolver == null)
                throw new ArgumentNullException(nameof(rotationResolver));
            if (cellPositionResolver == null)
                throw new ArgumentNullException(nameof(cellPositionResolver));
            if (horizontalMetresPerUnit <= 0f || verticalMetresPerUnit <= 0f)
                throw new ArgumentOutOfRangeException(nameof(horizontalMetresPerUnit));

            var root = new GameObject(RootName);
            var instances = new Dictionary<string,
                LuoyangFacilitySelectionProxyInstance>(StringComparer.Ordinal);
            foreach (var resident in residents.OrderBy(item => item.CellId64)
                         .ThenBy(item => item.FacilityId,
                             StringComparer.Ordinal))
            {
                var proxy = plan.SelectionProxiesByFacilityId[
                    resident.FacilityId];
                var gameObject = new GameObject("LUOYANG_SELECTION_PROXY_" +
                                                resident.FacilityId);
                gameObject.transform.SetParent(root.transform, false);
                gameObject.transform.position = positionResolver(resident);
                gameObject.transform.rotation = Quaternion.Euler(0f,
                    rotationResolver(resident), 0f);
                var instance = gameObject.AddComponent<
                    LuoyangFacilitySelectionProxyInstance>();
                instance.Initialize(proxy);
                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0f,
                    proxy.HeightMetres / verticalMetresPerUnit * 0.5f, 0f);
                collider.size = new Vector3(
                    proxy.HalfExtentEastMetres * 2f / horizontalMetresPerUnit,
                    proxy.HeightMetres / verticalMetresPerUnit,
                    proxy.HalfExtentNorthMetres * 2f /
                    horizontalMetresPerUnit);
                instances.Add(proxy.FacilityId, instance);
            }

            var navigationMaterial = CreateMaterial(new Color(0.12f, 0.72f,
                0.92f, 0.78f));
            var modeledConnectorMaterial = CreateMaterial(new Color(1f, 0.48f,
                0.08f, 0.88f));
            var blockedPassageMaterial = CreateMaterial(new Color(0.96f, 0.08f,
                0.08f, 0.98f));
            var damagedPassageMaterial = CreateMaterial(new Color(1f, 0.68f,
                0.08f, 0.96f));
            var selectionMaterial = CreateMaterial(new Color(1f, 0.74f,
                0.08f, 0.96f));
            var nodeById = refinementPlan.NavigationNodes.ToDictionary(
                item => item.NodeId, StringComparer.Ordinal);
            var visibleEdges = refinementPlan.NavigationEdges.Where(edge =>
                    instances.ContainsKey(nodeById[edge.FromNodeId].FacilityId) &&
                    instances.ContainsKey(nodeById[edge.ToNodeId].FacilityId))
                .ToArray();
            var visibleModeledEdges = visibleEdges.Where(edge => string.Equals(
                    edge.EdgeProfileId,
                    LuoyangRoadConnectorPassageTraversalIds
                        .ModeledConnectorEdgeProfileId,
                    StringComparison.Ordinal)).ToArray();
            var navigationMesh = BuildNavigationMesh(visibleEdges.Where(edge =>
                    !string.Equals(edge.EdgeProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .ModeledConnectorEdgeProfileId,
                        StringComparison.Ordinal)).ToArray(), nodeById,
                instances);
            var overlay = new GameObject(NavigationOverlayName);
            overlay.transform.SetParent(root.transform, false);
            overlay.AddComponent<MeshFilter>().sharedMesh = navigationMesh;
            var overlayRenderer = overlay.AddComponent<MeshRenderer>();
            overlayRenderer.sharedMaterial = navigationMaterial;
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;

            var modeledConnectorMesh = BuildModeledConnectorMesh(
                visibleModeledEdges, refinementPlan.ModeledConnectorsByEdgeId,
                nodeById, instances, cellPositionResolver);
            var modeledOverlay = new GameObject(ModeledConnectorOverlayName);
            modeledOverlay.transform.SetParent(root.transform, false);
            modeledOverlay.AddComponent<MeshFilter>().sharedMesh =
                modeledConnectorMesh;
            var modeledRenderer = modeledOverlay.AddComponent<MeshRenderer>();
            modeledRenderer.sharedMaterial = modeledConnectorMaterial;
            modeledRenderer.shadowCastingMode = ShadowCastingMode.Off;
            modeledRenderer.receiveShadows = false;

            var blockedPassageMesh = new Mesh
                { name = BlockedPassageOverlayName };
            var blockedPassage = new GameObject(BlockedPassageOverlayName);
            blockedPassage.transform.SetParent(root.transform, false);
            blockedPassage.AddComponent<MeshFilter>().sharedMesh =
                blockedPassageMesh;
            var blockedPassageRenderer =
                blockedPassage.AddComponent<MeshRenderer>();
            blockedPassageRenderer.sharedMaterial = blockedPassageMaterial;
            blockedPassageRenderer.shadowCastingMode = ShadowCastingMode.Off;
            blockedPassageRenderer.receiveShadows = false;

            var damagedPassageMesh = new Mesh
                { name = DamagedPassageOverlayName };
            var damagedPassage = new GameObject(DamagedPassageOverlayName);
            damagedPassage.transform.SetParent(root.transform, false);
            damagedPassage.AddComponent<MeshFilter>().sharedMesh =
                damagedPassageMesh;
            var damagedPassageRenderer =
                damagedPassage.AddComponent<MeshRenderer>();
            damagedPassageRenderer.sharedMaterial = damagedPassageMaterial;
            damagedPassageRenderer.shadowCastingMode = ShadowCastingMode.Off;
            damagedPassageRenderer.receiveShadows = false;

            var selectionMesh = new Mesh { name = SelectionHighlightName };
            var selection = new GameObject(SelectionHighlightName);
            selection.transform.SetParent(root.transform, false);
            selection.AddComponent<MeshFilter>().sharedMesh = selectionMesh;
            var selectionRenderer = selection.AddComponent<MeshRenderer>();
            selectionRenderer.sharedMaterial = selectionMaterial;
            selectionRenderer.shadowCastingMode = ShadowCastingMode.Off;
            selectionRenderer.receiveShadows = false;
            selectionRenderer.enabled = false;

            var runtime = new LuoyangFacilityInteractionNavigationRuntime(root,
                instances, navigationMaterial, modeledConnectorMaterial,
                blockedPassageMaterial, damagedPassageMaterial,
                selectionMaterial, navigationMesh, modeledConnectorMesh,
                blockedPassageMesh, damagedPassageMesh, selectionMesh,
                blockedPassageRenderer, damagedPassageRenderer,
                selectionRenderer, visibleEdges.Length,
                visibleModeledEdges.Length);
            runtime.RefreshTraversalState(passageSession);
            return runtime;
        }

        public void RefreshTraversalState(
            LuoyangPassageTraversalSession passageSession)
        {
            if (passageSession == null) throw new ArgumentNullException(
                nameof(passageSession));
            var blockedVertices = new List<Vector3>();
            var blockedTriangles = new List<int>();
            var damagedVertices = new List<Vector3>();
            var damagedTriangles = new List<int>();
            var count = 0;
            foreach (var record in passageSession.Records)
            {
                if (string.Equals(record.TraversalStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                        StringComparison.Ordinal) ||
                    !_instancesByFacilityId.TryGetValue(record.FacilityId,
                        out var instance)) continue;
                var center = instance.transform.position + Vector3.up * 0.095f;
                var blocked = !record.CanTraverse;
                var vertices = blocked ? blockedVertices : damagedVertices;
                var triangles = blocked ? blockedTriangles : damagedTriangles;
                AddRibbon(vertices, triangles,
                    center + new Vector3(-0.18f, 0f, -0.18f),
                    center + new Vector3(0.18f, 0f, 0.18f), 0.045f);
                AddRibbon(vertices, triangles,
                    center + new Vector3(-0.18f, 0f, 0.18f),
                    center + new Vector3(0.18f, 0f, -0.18f), 0.045f);
                count++;
            }
            SetMesh(_blockedPassageMesh, blockedVertices, blockedTriangles);
            SetMesh(_damagedPassageMesh, damagedVertices, damagedTriangles);
            _blockedPassageRenderer.enabled = blockedVertices.Count > 0;
            _damagedPassageRenderer.enabled = damagedVertices.Count > 0;
            ResidentPassageMarkerCount = count;
        }

        public bool TrySelect(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId) ||
                !_instancesByFacilityId.TryGetValue(facilityId,
                    out var instance))
            {
                ClearSelection();
                return false;
            }
            var collider = instance.GetComponent<BoxCollider>();
            var bounds = collider.bounds;
            var center = new Vector3(bounds.center.x, bounds.min.y + 0.075f,
                bounds.center.z);
            var halfX = Math.Max(0.12f, bounds.extents.x);
            var halfZ = Math.Max(0.12f, bounds.extents.z);
            var corners = new[]
            {
                center + new Vector3(-halfX, 0f, -halfZ),
                center + new Vector3(halfX, 0f, -halfZ),
                center + new Vector3(halfX, 0f, halfZ),
                center + new Vector3(-halfX, 0f, halfZ)
            };
            var vertices = new List<Vector3>(16);
            var triangles = new List<int>(24);
            for (var index = 0; index < 4; index++)
                AddRibbon(vertices, triangles, corners[index],
                    corners[(index + 1) % 4], 0.045f);
            _selectionMesh.Clear();
            _selectionMesh.SetVertices(vertices);
            _selectionMesh.SetTriangles(triangles, 0);
            _selectionMesh.RecalculateBounds();
            _selectionRenderer.enabled = true;
            return true;
        }

        public void ClearSelection()
        {
            _selectionMesh.Clear();
            _selectionRenderer.enabled = false;
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
            if (_navigationMesh != null)
                UnityEngine.Object.DestroyImmediate(_navigationMesh);
            if (_modeledConnectorMesh != null)
                UnityEngine.Object.DestroyImmediate(_modeledConnectorMesh);
            if (_blockedPassageMesh != null)
                UnityEngine.Object.DestroyImmediate(_blockedPassageMesh);
            if (_damagedPassageMesh != null)
                UnityEngine.Object.DestroyImmediate(_damagedPassageMesh);
            if (_selectionMesh != null)
                UnityEngine.Object.DestroyImmediate(_selectionMesh);
            if (_navigationMaterial != null)
                UnityEngine.Object.DestroyImmediate(_navigationMaterial);
            if (_modeledConnectorMaterial != null)
                UnityEngine.Object.DestroyImmediate(_modeledConnectorMaterial);
            if (_blockedPassageMaterial != null)
                UnityEngine.Object.DestroyImmediate(_blockedPassageMaterial);
            if (_damagedPassageMaterial != null)
                UnityEngine.Object.DestroyImmediate(_damagedPassageMaterial);
            if (_selectionMaterial != null)
                UnityEngine.Object.DestroyImmediate(_selectionMaterial);
        }

        private static Mesh BuildNavigationMesh(
            IReadOnlyList<LuoyangRoadNavigationEdge> edges,
            IReadOnlyDictionary<string, LuoyangRoadNavigationNode> nodeById,
            IReadOnlyDictionary<string, LuoyangFacilitySelectionProxyInstance>
                instances)
        {
            var vertices = new List<Vector3>(edges.Count * 4);
            var triangles = new List<int>(edges.Count * 6);
            foreach (var edge in edges)
            {
                var fromId = nodeById[edge.FromNodeId].FacilityId;
                var toId = nodeById[edge.ToNodeId].FacilityId;
                var from = instances[fromId].transform.position +
                           Vector3.up * 0.06f;
                var to = instances[toId].transform.position +
                         Vector3.up * 0.06f;
                AddRibbon(vertices, triangles, from, to,
                    edge.Provisional ? 0.018f : 0.028f);
            }
            var mesh = new Mesh { name = NavigationOverlayName };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildModeledConnectorMesh(
            IReadOnlyList<LuoyangRoadNavigationEdge> edges,
            IReadOnlyDictionary<string, LuoyangModeledRoadConnector>
                connectorsByEdgeId,
            IReadOnlyDictionary<string, LuoyangRoadNavigationNode> nodeById,
            IReadOnlyDictionary<string, LuoyangFacilitySelectionProxyInstance>
                instances,
            Func<int, int, Vector3> cellPositionResolver)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            foreach (var edge in edges)
            {
                var connector = connectorsByEdgeId[edge.EdgeId];
                var points = connector.Waypoints.Select(point =>
                    cellPositionResolver(point.GridRow, point.GridColumn) +
                    Vector3.up * 0.075f).ToArray();
                points[0] = instances[nodeById[edge.FromNodeId].FacilityId]
                    .transform.position + Vector3.up * 0.075f;
                points[points.Length - 1] =
                    instances[nodeById[edge.ToNodeId].FacilityId]
                        .transform.position + Vector3.up * 0.075f;
                for (var index = 0; index < points.Length - 1; index++)
                    AddRibbon(vertices, triangles, points[index],
                        points[index + 1], 0.022f);
            }
            var mesh = new Mesh { name = ModeledConnectorOverlayName };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetMesh(Mesh mesh, List<Vector3> vertices,
            List<int> triangles)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private static void AddRibbon(ICollection<Vector3> vertices,
            ICollection<int> triangles, Vector3 from, Vector3 to,
            float halfWidth)
        {
            var delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.000001f) return;
            var perpendicular = new Vector3(-delta.z, 0f, delta.x).normalized *
                                halfWidth;
            var start = vertices.Count;
            vertices.Add(from - perpendicular);
            vertices.Add(from + perpendicular);
            vertices.Add(to + perpendicular);
            vertices.Add(to - perpendicular);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException(
                    "No shader is available for the Luoyang interaction overlay.");
            var material = new Material(shader)
            {
                name = "Luoyang Interaction Overlay Runtime Material",
                color = color
            };
            return material;
        }
    }
}
