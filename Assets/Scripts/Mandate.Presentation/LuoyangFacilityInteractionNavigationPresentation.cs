using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
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

    public sealed class LuoyangPassagePedestrianPresentationInstance :
        MonoBehaviour
    {
        private BoxCollider _navigationBlocker;
        private Transform _leftLeaf;
        private Transform _rightLeaf;
        private Transform _rubble;
        private Transform _scaffold;
        private MeshRenderer _leftLeafRenderer;
        private MeshRenderer _rightLeafRenderer;
        private MeshRenderer _rubbleRenderer;
        private MeshRenderer _scaffoldRenderer;
        private Material _openMaterial;
        private Material _closedMaterial;
        private Material _damagedMaterial;
        private Material _destroyedMaterial;
        private Material _repairingMaterial;
        private float _width;
        private float _height;
        private float _depth;

        public string FacilityId { get; private set; }
        public string TraversalStatusId { get; private set; }
        public string VisualStateId { get; private set; }
        public bool BlocksPedestrianTraversal { get; private set; }
        public bool IsRepairing { get; private set; }
        public int ConditionBasisPoints { get; private set; }
        public long PassageRevision { get; private set; }
        public long IntegrityRevision { get; private set; }
        public BoxCollider NavigationBlocker => _navigationBlocker;

        public void Initialize(LuoyangPassagePedestrianState state,
            Mesh unitCubeMesh, float width, float height, float depth,
            Material openMaterial, Material closedMaterial,
            Material damagedMaterial, Material destroyedMaterial,
            Material repairingMaterial)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (unitCubeMesh == null)
                throw new ArgumentNullException(nameof(unitCubeMesh));
            if (width <= 0f || height <= 0f || depth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            _width = width;
            _height = height;
            _depth = depth;
            _openMaterial = openMaterial ?? throw new ArgumentNullException(
                nameof(openMaterial));
            _closedMaterial = closedMaterial ?? throw new ArgumentNullException(
                nameof(closedMaterial));
            _damagedMaterial = damagedMaterial ??
                throw new ArgumentNullException(nameof(damagedMaterial));
            _destroyedMaterial = destroyedMaterial ??
                throw new ArgumentNullException(nameof(destroyedMaterial));
            _repairingMaterial = repairingMaterial ??
                throw new ArgumentNullException(nameof(repairingMaterial));

            _navigationBlocker = gameObject.AddComponent<BoxCollider>();
            _navigationBlocker.isTrigger = false;
            _navigationBlocker.center = new Vector3(0f, height * 0.5f, 0f);
            _navigationBlocker.size = new Vector3(width, height, depth);
            _leftLeaf = CreatePart("STATE_LEAF_LEFT", unitCubeMesh,
                out _leftLeafRenderer);
            _rightLeaf = CreatePart("STATE_LEAF_RIGHT", unitCubeMesh,
                out _rightLeafRenderer);
            _rubble = CreatePart("STATE_RUBBLE", unitCubeMesh,
                out _rubbleRenderer);
            _scaffold = CreatePart("STATE_REPAIR_SCAFFOLD", unitCubeMesh,
                out _scaffoldRenderer);
            Apply(state);
        }

        public void Apply(LuoyangPassagePedestrianState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!string.IsNullOrWhiteSpace(FacilityId) &&
                !string.Equals(FacilityId, state.FacilityId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A passage presentation instance cannot change Facility ID.");
            FacilityId = state.FacilityId;
            TraversalStatusId = state.TraversalStatusId;
            VisualStateId = state.VisualStateId;
            BlocksPedestrianTraversal = state.BlocksPedestrianTraversal;
            IsRepairing = state.IsRepairing;
            ConditionBasisPoints = state.ConditionBasisPoints;
            PassageRevision = state.PassageRevision;
            IntegrityRevision = state.IntegrityRevision;
            _navigationBlocker.enabled = state.BlocksPedestrianTraversal;

            SetPartActive(_leftLeaf, false);
            SetPartActive(_rightLeaf, false);
            SetPartActive(_rubble, false);
            SetPartActive(_scaffold, false);
            if (string.Equals(state.TraversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                    StringComparison.Ordinal))
            {
                ConfigureLeaf(_leftLeaf, _leftLeafRenderer, -1f, -58f,
                    _openMaterial);
                ConfigureLeaf(_rightLeaf, _rightLeafRenderer, 1f, 58f,
                    _openMaterial);
            }
            else if (string.Equals(state.TraversalStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                         StringComparison.Ordinal))
            {
                ConfigureLeaf(_leftLeaf, _leftLeafRenderer, -1f, 0f,
                    _closedMaterial);
                ConfigureLeaf(_rightLeaf, _rightLeafRenderer, 1f, 0f,
                    _closedMaterial);
            }
            else if (string.Equals(state.TraversalStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                         StringComparison.Ordinal))
            {
                ConfigureLeaf(_leftLeaf, _leftLeafRenderer, -0.55f, -24f,
                    _damagedMaterial);
                ConfigureRubble(_damagedMaterial, 0.48f, 0.22f, 18f);
            }
            else if (string.Equals(state.TraversalStatusId,
                         LuoyangRoadConnectorPassageTraversalIds
                             .DestroyedStatusId,
                         StringComparison.Ordinal))
            {
                ConfigureRubble(_destroyedMaterial, 0.92f, 0.38f, -11f);
            }
            else
            {
                throw new InvalidOperationException(
                    "Unknown Luoyang passage presentation status: " +
                    state.TraversalStatusId);
            }

            if (state.IsRepairing) ConfigureScaffold();
        }

        private Transform CreatePart(string name, Mesh mesh,
            out MeshRenderer meshRenderer)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return part.transform;
        }

        private void ConfigureLeaf(Transform leaf, MeshRenderer renderer,
            float side, float yawDegrees, Material material)
        {
            SetPartActive(leaf, true);
            leaf.localPosition = new Vector3(side * _width * 0.24f,
                _height * 0.5f, 0f);
            leaf.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            leaf.localScale = new Vector3(_width * 0.48f, _height,
                Math.Max(0.018f, _depth * 0.42f));
            renderer.sharedMaterial = material;
        }

        private void ConfigureRubble(Material material, float widthScale,
            float heightScale, float yawDegrees)
        {
            SetPartActive(_rubble, true);
            _rubble.localPosition = new Vector3(0f,
                _height * heightScale * 0.5f, 0f);
            _rubble.localRotation = Quaternion.Euler(0f, yawDegrees, 7f);
            _rubble.localScale = new Vector3(_width * widthScale,
                Math.Max(0.035f, _height * heightScale), _depth * 1.75f);
            _rubbleRenderer.sharedMaterial = material;
        }

        private void ConfigureScaffold()
        {
            SetPartActive(_scaffold, true);
            _scaffold.localPosition = new Vector3(0f, _height * 0.88f, 0f);
            _scaffold.localRotation = Quaternion.identity;
            _scaffold.localScale = new Vector3(_width * 1.12f,
                Math.Max(0.018f, _height * 0.08f), _depth * 1.35f);
            _scaffoldRenderer.sharedMaterial = _repairingMaterial;
        }

        private static void SetPartActive(Transform part, bool active)
        {
            if (part != null) part.gameObject.SetActive(active);
        }
    }

    public sealed class LuoyangClickWalkPedestrianInstance : MonoBehaviour
    {
        private const float BodyHeight = 0.14f;
        private const float BodyRadius = 0.025f;
        private IReadOnlyList<Vector3> _routePoints = Array.Empty<Vector3>();
        private LuoyangPedestrianWalkPlan _routePlan;
        private LuoyangHumanScaleLocalRoute _localRoutePlan;
        private int _nextPointIndex;

        public string ActorId { get; private set; }
        public string CurrentFacilityId { get; private set; }
        public string TargetFacilityId { get; private set; }
        public string MovementStateId { get; private set; }
        public string LastStopReasonId { get; private set; }
        public bool IsWalking => string.Equals(MovementStateId,
            LuoyangClickToWalkPedestrianIds.WalkingStateId,
            StringComparison.Ordinal);
        public int RoutePointCount => _routePoints.Count;
        public CapsuleCollider CollisionProxy { get; private set; }
        public IReadOnlyList<string> RouteFacilityIds =>
            _routePlan?.FacilityIds ?? (_localRoutePlan == null
                ? Array.Empty<string>()
                : new[]
                {
                    _localRoutePlan.StartFacilityId,
                    _localRoutePlan.TargetFacilityId
                });

        public void Initialize(string actorId, string facilityId,
            Vector3 position, Mesh unitCubeMesh, Material bodyMaterial,
            Material skinMaterial)
        {
            if (unitCubeMesh == null)
                throw new ArgumentNullException(nameof(unitCubeMesh));
            ActorId = new StableId(actorId).Value;
            CurrentFacilityId = new StableId(facilityId).Value;
            TargetFacilityId = string.Empty;
            LastStopReasonId = string.Empty;
            MovementStateId = LuoyangClickToWalkPedestrianIds.ReadyStateId;
            transform.position = position;
            CollisionProxy = gameObject.AddComponent<CapsuleCollider>();
            CollisionProxy.isTrigger = false;
            CollisionProxy.radius = BodyRadius;
            CollisionProxy.height = BodyHeight;
            CollisionProxy.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.interpolation = RigidbodyInterpolation.None;

            CreateBodyPart("BODY", unitCubeMesh, bodyMaterial,
                new Vector3(0f, 0.087f, 0f),
                new Vector3(0.052f, 0.078f, 0.032f));
            CreateBodyPart("HEAD", unitCubeMesh, skinMaterial,
                new Vector3(0f, 0.145f, 0f),
                new Vector3(0.040f, 0.040f, 0.040f));
            CreateBodyPart("ARM_LEFT", unitCubeMesh, bodyMaterial,
                new Vector3(-0.037f, 0.086f, 0f),
                new Vector3(0.018f, 0.070f, 0.018f));
            CreateBodyPart("ARM_RIGHT", unitCubeMesh, bodyMaterial,
                new Vector3(0.037f, 0.086f, 0f),
                new Vector3(0.018f, 0.070f, 0.018f));
            CreateBodyPart("LEG_LEFT", unitCubeMesh, bodyMaterial,
                new Vector3(-0.014f, 0.027f, 0f),
                new Vector3(0.020f, 0.054f, 0.022f));
            CreateBodyPart("LEG_RIGHT", unitCubeMesh, bodyMaterial,
                new Vector3(0.014f, 0.027f, 0f),
                new Vector3(0.020f, 0.054f, 0.022f));
        }

        public void PlaceAt(string facilityId, Vector3 position)
        {
            CurrentFacilityId = new StableId(facilityId).Value;
            TargetFacilityId = string.Empty;
            LastStopReasonId = string.Empty;
            MovementStateId = LuoyangClickToWalkPedestrianIds.ReadyStateId;
            _routePlan = null;
            _localRoutePlan = null;
            _routePoints = Array.Empty<Vector3>();
            _nextPointIndex = 0;
            transform.position = position;
        }

        public void BindActor(string actorId)
        {
            if (IsWalking)
                throw new InvalidOperationException(
                    "A walking presentation actor cannot be rebound.");
            ActorId = new StableId(actorId).Value;
        }

        public void BeginRoute(LuoyangPedestrianWalkPlan plan,
            IReadOnlyList<Vector3> routePoints)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (!plan.CanWalk || routePoints == null ||
                routePoints.Count != plan.FacilityIds.Count)
                throw new InvalidOperationException(
                    "A runtime pedestrian route requires a valid Domain plan.");
            _routePlan = plan;
            _localRoutePlan = null;
            _routePoints = routePoints;
            CurrentFacilityId = plan.StartFacilityId;
            TargetFacilityId = plan.TargetFacilityId;
            LastStopReasonId = string.Empty;
            transform.position = routePoints[0];
            _nextPointIndex = routePoints.Count > 1 ? 1 : 0;
            MovementStateId = routePoints.Count > 1
                ? LuoyangClickToWalkPedestrianIds.WalkingStateId
                : LuoyangClickToWalkPedestrianIds.ArrivedStateId;
        }

        public void BeginLocalRoute(LuoyangHumanScaleLocalRoute plan,
            IReadOnlyList<Vector3> routePoints)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (routePoints == null || routePoints.Count < 2)
                throw new InvalidOperationException(
                    "A local pedestrian route requires Domain route points.");
            _routePlan = null;
            _localRoutePlan = plan;
            _routePoints = routePoints;
            CurrentFacilityId = plan.StartFacilityId;
            TargetFacilityId = plan.TargetFacilityId;
            LastStopReasonId = string.Empty;
            transform.position = routePoints[0];
            _nextPointIndex = 1;
            MovementStateId =
                LuoyangClickToWalkPedestrianIds.WalkingStateId;
        }

        public void Stop(string reasonId, bool blocked)
        {
            LastStopReasonId = new StableId(reasonId).Value;
            MovementStateId = blocked
                ? LuoyangClickToWalkPedestrianIds.BlockedStateId
                : LuoyangClickToWalkPedestrianIds.CancelledStateId;
        }

        public bool Step(float deltaSeconds, float speedUnitsPerSecond)
        {
            if (!IsWalking || deltaSeconds <= 0f ||
                speedUnitsPerSecond <= 0f) return false;
            var target = _routePoints[_nextPointIndex];
            var difference = target - transform.position;
            var distance = difference.magnitude;
            var stepDistance = speedUnitsPerSecond * deltaSeconds;
            if (distance > 0.00001f)
            {
                var direction = difference / distance;
                var probeDistance = Math.Min(distance, stepDistance);
                var hits = Physics.SphereCastAll(
                    transform.position + Vector3.up * (BodyHeight * 0.5f),
                    BodyRadius, direction, probeDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                if (hits.Any(hit => hit.collider != CollisionProxy &&
                    hit.collider.enabled &&
                    hit.collider.GetComponent<
                        LuoyangPassagePedestrianPresentationInstance>() != null))
                {
                    Stop(LuoyangClickToWalkPedestrianIds.DynamicBlockerReasonId,
                        true);
                    return false;
                }
                transform.position = Vector3.MoveTowards(transform.position,
                    target, stepDistance);
                var horizontal = new Vector3(direction.x, 0f, direction.z);
                if (horizontal.sqrMagnitude > 0.00001f)
                    transform.rotation = Quaternion.LookRotation(horizontal,
                        Vector3.up);
            }
            if ((transform.position - target).sqrMagnitude > 0.000001f)
                return true;
            if (_localRoutePlan == null)
                CurrentFacilityId = _routePlan.FacilityIds[_nextPointIndex];
            _nextPointIndex++;
            if (_nextPointIndex < _routePoints.Count) return true;
            transform.position = _routePoints[_routePoints.Count - 1];
            CurrentFacilityId = _localRoutePlan == null
                ? _routePlan.TargetFacilityId
                : _localRoutePlan.TargetFacilityId;
            MovementStateId = LuoyangClickToWalkPedestrianIds.ArrivedStateId;
            return true;
        }

        public IReadOnlyList<string> RemainingFacilityIds()
        {
            if (_localRoutePlan != null && IsWalking)
                return new[]
                {
                    CurrentFacilityId,
                    _localRoutePlan.TargetFacilityId
                };
            if (_routePlan == null || !IsWalking) return Array.Empty<string>();
            return _routePlan.FacilityIds.Skip(Math.Max(0,
                _nextPointIndex - 1)).ToArray();
        }

        private void CreateBodyPart(string name, Mesh mesh, Material material,
            Vector3 localPosition, Vector3 localScale)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
        public const string PassagePedestrianPresentationRootName =
            "Luoyang Passage Stateful Presentation Pedestrian Blocking V1";
        public const string ClickWalkPedestrianRootName =
            "Luoyang Click To Walk Pedestrian Vertical Slice V1";
        public const string ClickWalkRouteName =
            "Luoyang Click To Walk Route V1";
        public const string ClickWalkTargetName =
            "Luoyang Click To Walk Target V1";

        private readonly Dictionary<string,
            LuoyangFacilitySelectionProxyInstance> _instancesByFacilityId;
        private readonly Dictionary<string,
            LuoyangPassagePedestrianPresentationInstance>
            _passagePresentationInstancesByFacilityId;
        private readonly LuoyangRoadTraversalRefinementPlan _refinementPlan;
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
        private readonly Material _passageOpenMaterial;
        private readonly Material _passageClosedMaterial;
        private readonly Material _passageDamagedMaterial;
        private readonly Material _passageDestroyedMaterial;
        private readonly Material _passageRepairingMaterial;
        private readonly Mesh _passageStateCubeMesh;
        private LuoyangPassageTraversalSession _passageSession;
        private Dictionary<string, Vector3> _pedestrianNodePositions;
        private LuoyangClickWalkPedestrianInstance _pedestrianInstance;
        private Mesh _pedestrianRouteMesh;
        private MeshRenderer _pedestrianRouteRenderer;
        private GameObject _pedestrianTarget;
        private Material _pedestrianBodyMaterial;
        private Material _pedestrianSkinMaterial;
        private Material _pedestrianRouteMaterial;
        private Material _pedestrianTargetMaterial;
        private float _horizontalMetresPerUnit;
        private LuoyangPedestrianWalkPlan _lastPedestrianWalkPlan;
        private LuoyangHumanScaleLocalRoute _lastHumanScaleRoute;
        private WorldState _formalMovementWorld;
        private LuoyangFormalPlayerMovementService _formalMovementService;
        private LuoyangHumanScaleLocalMapPlan _humanScaleLocalMap;
        private Func<double, double, Vector3> _localWorldPositionResolver;

        private LuoyangFacilityInteractionNavigationRuntime(GameObject root,
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            Dictionary<string, LuoyangFacilitySelectionProxyInstance>
                instancesByFacilityId,
            Dictionary<string,
                LuoyangPassagePedestrianPresentationInstance>
                passagePresentationInstancesByFacilityId,
            Material navigationMaterial, Material modeledConnectorMaterial,
            Material blockedPassageMaterial, Material damagedPassageMaterial,
            Material selectionMaterial, Material passageOpenMaterial,
            Material passageClosedMaterial, Material passageDamagedMaterial,
            Material passageDestroyedMaterial,
            Material passageRepairingMaterial, Mesh passageStateCubeMesh,
            Mesh navigationMesh,
            Mesh modeledConnectorMesh, Mesh blockedPassageMesh,
            Mesh damagedPassageMesh, Mesh selectionMesh,
            MeshRenderer blockedPassageRenderer,
            MeshRenderer damagedPassageRenderer,
            MeshRenderer selectionRenderer, int residentNavigationEdgeCount,
            int residentModeledConnectorEdgeCount)
        {
            Root = root;
            _refinementPlan = refinementPlan ?? throw new ArgumentNullException(
                nameof(refinementPlan));
            _instancesByFacilityId = instancesByFacilityId;
            _passagePresentationInstancesByFacilityId =
                passagePresentationInstancesByFacilityId;
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
            _passageOpenMaterial = passageOpenMaterial;
            _passageClosedMaterial = passageClosedMaterial;
            _passageDamagedMaterial = passageDamagedMaterial;
            _passageDestroyedMaterial = passageDestroyedMaterial;
            _passageRepairingMaterial = passageRepairingMaterial;
            _passageStateCubeMesh = passageStateCubeMesh;
            ResidentNavigationEdgeCount = residentNavigationEdgeCount;
            ResidentModeledConnectorEdgeCount = residentModeledConnectorEdgeCount;
        }

        public GameObject Root { get; }
        public int ResidentProxyCount => _instancesByFacilityId.Count;
        public int ResidentNavigationEdgeCount { get; }
        public int ResidentModeledConnectorEdgeCount { get; }
        public int ResidentPassageMarkerCount { get; private set; }
        public int ResidentPassagePresentationCount =>
            _passagePresentationInstancesByFacilityId.Count;
        public int ActivePedestrianBlockerCount { get; private set; }
        public int DamagedPassagePresentationCount { get; private set; }
        public int DestroyedPassagePresentationCount { get; private set; }
        public int ActiveRepairScaffoldCount { get; private set; }
        public string PedestrianActorId => _pedestrianInstance?.ActorId;
        public string PedestrianCurrentFacilityId =>
            _pedestrianInstance?.CurrentFacilityId;
        public string PedestrianTargetFacilityId =>
            _pedestrianInstance?.TargetFacilityId;
        public string PedestrianMovementStateId =>
            _pedestrianInstance?.MovementStateId;
        public string PedestrianLastStopReasonId =>
            _pedestrianInstance?.LastStopReasonId;
        public bool PedestrianIsWalking =>
            _pedestrianInstance != null && _pedestrianInstance.IsWalking;
        public int PedestrianRouteNodeCount =>
            _pedestrianInstance?.RouteFacilityIds.Count ?? 0;
        public float PedestrianRouteDistanceMetres =>
            _lastHumanScaleRoute != null
                ? (float)(_lastHumanScaleRoute.DistanceCentimetres / 100d)
                : _lastPedestrianWalkPlan?.TotalDistanceMetres ?? 0f;
        public float PedestrianEstimatedDurationSeconds =>
            _lastPedestrianWalkPlan?.EstimatedDurationSeconds ?? 0f;
        public LuoyangClickWalkPedestrianInstance PedestrianInstance =>
            _pedestrianInstance;
        public IReadOnlyList<string> PedestrianRouteFacilityIds =>
            _pedestrianInstance?.RouteFacilityIds ?? Array.Empty<string>();
        public bool UsesFormalPlayerMovement =>
            _formalMovementWorld != null && _formalMovementService != null;
        public bool UsesHumanScaleFormalMovement =>
            UsesFormalPlayerMovement && _humanScaleLocalMap != null &&
            _localWorldPositionResolver != null;

        public static LuoyangFacilityInteractionNavigationRuntime Build(
            LuoyangFacilityInteractionNavigationPlan plan,
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession,
            WorldState passageWorld,
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
            var passageOpenMaterial = CreateMaterial(new Color(0.20f, 0.48f,
                0.28f, 0.96f));
            var passageClosedMaterial = CreateMaterial(new Color(0.42f, 0.22f,
                0.10f, 0.98f));
            var passageDamagedMaterial = CreateMaterial(new Color(0.95f,
                0.56f, 0.08f, 0.98f));
            var passageDestroyedMaterial = CreateMaterial(new Color(0.42f,
                0.08f, 0.06f, 0.98f));
            var passageRepairingMaterial = CreateMaterial(new Color(0.10f,
                0.82f, 0.92f, 0.98f));
            var passageStateCubeMesh = CreateUnitCubeMesh();
            var nodeById = refinementPlan.NavigationNodes.ToDictionary(
                item => item.NodeId, StringComparer.Ordinal);
            var passagePresentationRoot = new GameObject(
                PassagePedestrianPresentationRootName);
            passagePresentationRoot.transform.SetParent(root.transform, false);
            var passagePresentationInstances = new Dictionary<string,
                LuoyangPassagePedestrianPresentationInstance>(
                StringComparer.Ordinal);
            var pedestrianPlan = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(refinementPlan, passageSession, passageWorld);
            foreach (var state in pedestrianPlan.States)
            {
                if (!instances.TryGetValue(state.FacilityId,
                        out var selectionProxy)) continue;
                var selectionCollider = selectionProxy.GetComponent<
                    BoxCollider>();
                var bounds = selectionCollider.bounds;
                var width = Mathf.Clamp(
                    Mathf.Max(bounds.size.x, bounds.size.z) * 0.62f,
                    0.16f, 0.52f);
                var height = Mathf.Clamp(bounds.size.y * 0.72f,
                    0.12f, 0.42f);
                var depth = Mathf.Clamp(
                    Mathf.Min(bounds.size.x, bounds.size.z) * 0.24f,
                    0.04f, 0.12f);
                var presentationObject = new GameObject(
                    "LUOYANG_PASSAGE_PEDESTRIAN_" + state.FacilityId);
                presentationObject.transform.SetParent(
                    passagePresentationRoot.transform, false);
                presentationObject.transform.position = new Vector3(
                    bounds.center.x, bounds.min.y + 0.012f, bounds.center.z);
                presentationObject.transform.rotation = Quaternion.Euler(0f,
                    ResolvePassageYawDegrees(refinementPlan,
                        state.FacilityId), 0f);
                var presentationInstance = presentationObject.AddComponent<
                    LuoyangPassagePedestrianPresentationInstance>();
                presentationInstance.Initialize(state, passageStateCubeMesh,
                    width, height, depth, passageOpenMaterial,
                    passageClosedMaterial, passageDamagedMaterial,
                    passageDestroyedMaterial, passageRepairingMaterial);
                passagePresentationInstances.Add(state.FacilityId,
                    presentationInstance);
            }
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
                refinementPlan, instances, passagePresentationInstances,
                navigationMaterial, modeledConnectorMaterial,
                blockedPassageMaterial, damagedPassageMaterial,
                selectionMaterial, passageOpenMaterial,
                passageClosedMaterial, passageDamagedMaterial,
                passageDestroyedMaterial, passageRepairingMaterial,
                passageStateCubeMesh, navigationMesh, modeledConnectorMesh,
                blockedPassageMesh, damagedPassageMesh, selectionMesh,
                blockedPassageRenderer, damagedPassageRenderer,
                selectionRenderer, visibleEdges.Length,
                visibleModeledEdges.Length);
            runtime.InitializeClickWalkPedestrian(passageSession, instances,
                horizontalMetresPerUnit);
            runtime.RefreshTraversalState(passageSession, passageWorld);
            return runtime;
        }

        private void InitializeClickWalkPedestrian(
            LuoyangPassageTraversalSession passageSession,
            IReadOnlyDictionary<string,
                LuoyangFacilitySelectionProxyInstance> instances,
            float horizontalMetresPerUnit)
        {
            _passageSession = passageSession ?? throw new ArgumentNullException(
                nameof(passageSession));
            _horizontalMetresPerUnit = horizontalMetresPerUnit;
            _pedestrianNodePositions = _refinementPlan.NavigationNodes
                .Where(item => instances.ContainsKey(item.FacilityId))
                .ToDictionary(item => item.FacilityId,
                    item => instances[item.FacilityId].transform.position +
                            Vector3.up * 0.025f, StringComparer.Ordinal);
            var initial = _refinementPlan.NavigationNodes.Where(item =>
                    string.Equals(item.FacilityDefinitionId,
                        "facility.public.road", StringComparison.Ordinal) &&
                    _pedestrianNodePositions.ContainsKey(item.FacilityId))
                .OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (initial == null) return;

            var root = new GameObject(ClickWalkPedestrianRootName);
            root.transform.SetParent(Root.transform, false);
            _pedestrianBodyMaterial = CreateMaterial(new Color(0.12f, 0.26f,
                0.62f, 1f));
            _pedestrianSkinMaterial = CreateMaterial(new Color(0.94f, 0.72f,
                0.50f, 1f));
            _pedestrianRouteMaterial = CreateMaterial(new Color(1f, 0.92f,
                0.10f, 0.98f));
            _pedestrianTargetMaterial = CreateMaterial(new Color(1f, 0.20f,
                0.76f, 0.98f));
            var actor = new GameObject("PEDESTRIAN_ACTOR_" +
                                       LuoyangClickToWalkPedestrianIds
                                           .PreviewActorId);
            actor.transform.SetParent(root.transform, false);
            _pedestrianInstance = actor.AddComponent<
                LuoyangClickWalkPedestrianInstance>();
            _pedestrianInstance.Initialize(
                LuoyangClickToWalkPedestrianIds.PreviewActorId,
                initial.FacilityId,
                _pedestrianNodePositions[initial.FacilityId],
                _passageStateCubeMesh, _pedestrianBodyMaterial,
                _pedestrianSkinMaterial);

            _pedestrianRouteMesh = new Mesh { name = ClickWalkRouteName };
            var route = new GameObject(ClickWalkRouteName);
            route.transform.SetParent(root.transform, false);
            route.AddComponent<MeshFilter>().sharedMesh =
                _pedestrianRouteMesh;
            _pedestrianRouteRenderer = route.AddComponent<MeshRenderer>();
            _pedestrianRouteRenderer.sharedMaterial =
                _pedestrianRouteMaterial;
            _pedestrianRouteRenderer.shadowCastingMode =
                ShadowCastingMode.Off;
            _pedestrianRouteRenderer.receiveShadows = false;
            _pedestrianRouteRenderer.enabled = false;

            _pedestrianTarget = new GameObject(ClickWalkTargetName);
            _pedestrianTarget.transform.SetParent(root.transform, false);
            _pedestrianTarget.AddComponent<MeshFilter>().sharedMesh =
                _passageStateCubeMesh;
            var targetRenderer = _pedestrianTarget.AddComponent<MeshRenderer>();
            targetRenderer.sharedMaterial = _pedestrianTargetMaterial;
            targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            targetRenderer.receiveShadows = false;
            _pedestrianTarget.transform.localScale = new Vector3(0.08f,
                0.012f, 0.08f);
            _pedestrianTarget.SetActive(false);
        }

        public bool TryPlacePedestrianAtFacility(string facilityId,
            string actorId = null)
        {
            if (_pedestrianInstance == null ||
                string.IsNullOrWhiteSpace(facilityId) ||
                !_pedestrianNodePositions.TryGetValue(facilityId,
                    out var position)) return false;
            if (UsesFormalPlayerMovement)
            {
                var controlled = new PlayerSession(_formalMovementWorld)
                    .ControlledPerson;
                if (!string.Equals(controlled.CurrentFacilityId, facilityId,
                        StringComparison.Ordinal) ||
                    !string.IsNullOrWhiteSpace(actorId) && !string.Equals(
                        actorId, controlled.Id, StringComparison.Ordinal))
                    return false;
            }
            if (!string.IsNullOrWhiteSpace(actorId) && !string.Equals(
                    actorId, _pedestrianInstance.ActorId,
                    StringComparison.Ordinal))
            {
                if (_lastPedestrianWalkPlan != null ||
                    _pedestrianInstance.IsWalking) return false;
                _pedestrianInstance.BindActor(actorId);
            }
            _pedestrianInstance.PlaceAt(facilityId, position);
            _lastPedestrianWalkPlan = null;
            _lastHumanScaleRoute = null;
            ClearPedestrianRouteVisual();
            return true;
        }

        public bool TrySetPedestrianDestination(string facilityId)
        {
            if (_pedestrianInstance == null || _passageSession == null ||
                string.IsNullOrWhiteSpace(facilityId)) return false;
            if (UsesFormalPlayerMovement)
                return TrySetFormalPedestrianDestination(facilityId);
            var plan = LuoyangClickToWalkPedestrianRules.CreatePlan(
                _refinementPlan, _passageSession,
                _pedestrianInstance.ActorId,
                _pedestrianInstance.CurrentFacilityId, facilityId);
            _lastPedestrianWalkPlan = plan;
            if (!plan.CanWalk)
            {
                _pedestrianInstance.Stop(plan.FailureReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            if (plan.FacilityIds.Any(item =>
                    !_pedestrianNodePositions.ContainsKey(item)))
            {
                _pedestrianInstance.Stop(
                    LuoyangClickToWalkPedestrianIds
                        .OutsideResidentWindowReasonId, false);
                ClearPedestrianRouteVisual();
                return false;
            }
            var routePoints = BuildPedestrianRoutePoints(plan);
            _pedestrianInstance.BeginRoute(plan, routePoints);
            SetPedestrianRouteVisual(routePoints);
            _pedestrianTarget.transform.position = routePoints[
                routePoints.Count - 1] + Vector3.up * 0.006f;
            _pedestrianTarget.SetActive(true);
            return true;
        }

        public bool TrySetPedestrianDestination(Vector3 worldPosition)
        {
            if (_pedestrianInstance == null ||
                _pedestrianNodePositions.Count == 0) return false;
            var target = _pedestrianNodePositions
                .Select(item => new
                {
                    item.Key,
                    Distance = HorizontalDistanceSquared(item.Value,
                        worldPosition)
                })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Key, StringComparer.Ordinal)
                .First();
            if (target.Distance > 0.85f * 0.85f) return false;
            return TrySetPedestrianDestination(target.Key);
        }

        public bool StepPedestrian(float deltaSeconds)
        {
            if (_pedestrianInstance == null) return false;
            const float reviewSpeedUnitsPerSecond = 0.32f;
            return _pedestrianInstance.Step(deltaSeconds,
                reviewSpeedUnitsPerSecond);
        }

        public void BindFormalMovement(WorldState world,
            LuoyangFormalPlayerMovementService movementService,
            LuoyangHumanScaleLocalMapPlan humanScaleLocalMap = null,
            Func<double, double, Vector3> localWorldPositionResolver = null)
        {
            _formalMovementWorld = world ?? throw new ArgumentNullException(
                nameof(world));
            _formalMovementService = movementService ??
                throw new ArgumentNullException(nameof(movementService));
            if ((humanScaleLocalMap == null) !=
                (localWorldPositionResolver == null))
                throw new InvalidOperationException(
                    "Human-scale map and coordinate resolver must bind together.");
            _humanScaleLocalMap = humanScaleLocalMap;
            _localWorldPositionResolver = localWorldPositionResolver;
            if (_pedestrianInstance == null) return;
            var controlled = new PlayerSession(world).ControlledPerson;
            Vector3 position;
            if (humanScaleLocalMap != null)
            {
                if (!humanScaleLocalMap.NavigationNodesByFacilityId
                        .TryGetValue(controlled.CurrentFacilityId,
                            out var localNode))
                    throw new InvalidOperationException(
                        "The controlled Person has no local navigation anchor.");
                var space = humanScaleLocalMap.LocalSpacesByCellId[
                    localNode.CellId64];
                position = localWorldPositionResolver(
                    space.OriginEastingMetres + localNode.LocalEastMetres,
                    space.OriginNorthingMetres +
                    localNode.LocalNorthMetres);
            }
            else if (!_pedestrianNodePositions.TryGetValue(
                         controlled.CurrentFacilityId, out position))
                throw new InvalidOperationException(
                    "The controlled Person is outside the active Luoyang " +
                    "resident presentation window.");
            if (_pedestrianInstance.IsWalking)
                throw new InvalidOperationException(
                    "Formal movement cannot bind while preview playback runs.");
            _pedestrianInstance.BindActor(controlled.Id);
            _pedestrianInstance.PlaceAt(controlled.CurrentFacilityId, position);
            _lastPedestrianWalkPlan = null;
            _lastHumanScaleRoute = null;
            ClearPedestrianRouteVisual();
        }

        public void UnbindFormalMovement()
        {
            _formalMovementWorld = null;
            _formalMovementService = null;
            _humanScaleLocalMap = null;
            _localWorldPositionResolver = null;
            _lastHumanScaleRoute = null;
        }

        private bool TrySetFormalPedestrianDestination(string facilityId)
        {
            if (_pedestrianInstance.IsWalking) return false;
            if (UsesHumanScaleFormalMovement)
                return TrySetHumanScaleFormalPedestrianDestination(
                    facilityId);
            if (!_formalMovementService.TryRequest(_formalMovementWorld,
                    facilityId, out var movement, out var plan,
                    out var failureReasonId))
            {
                _pedestrianInstance.Stop(string.IsNullOrWhiteSpace(
                        failureReasonId)
                        ? LuoyangClickToWalkPedestrianIds.NoRouteReasonId
                        : failureReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            _formalMovementService.Complete(_formalMovementWorld, movement.Id);
            if (movement.Status != LuoyangFormalMovementStatus.Completed)
            {
                var controlled = new PlayerSession(_formalMovementWorld)
                    .ControlledPerson;
                if (_pedestrianNodePositions.TryGetValue(
                        controlled.CurrentFacilityId, out var position))
                    _pedestrianInstance.PlaceAt(
                        controlled.CurrentFacilityId, position);
                _pedestrianInstance.Stop(movement.FailureReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            _lastPedestrianWalkPlan = plan;
            if (plan.FacilityIds.Any(item =>
                    !_pedestrianNodePositions.ContainsKey(item)))
            {
                _pedestrianInstance.PlaceAt(plan.TargetFacilityId,
                    _pedestrianNodePositions[plan.TargetFacilityId]);
                _pedestrianInstance.Stop(
                    LuoyangClickToWalkPedestrianIds
                        .OutsideResidentWindowReasonId, false);
                ClearPedestrianRouteVisual();
                return false;
            }
            var routePoints = BuildPedestrianRoutePoints(plan);
            _pedestrianInstance.BeginRoute(plan, routePoints);
            SetPedestrianRouteVisual(routePoints);
            _pedestrianTarget.transform.position = routePoints[
                routePoints.Count - 1] + Vector3.up * 0.006f;
            _pedestrianTarget.SetActive(true);
            return true;
        }

        private bool TrySetHumanScaleFormalPedestrianDestination(
            string facilityId)
        {
            if (!_formalMovementService.TryRequestLocal(_formalMovementWorld,
                    facilityId, out var movement, out var plan,
                    out var failureReasonId))
            {
                _pedestrianInstance.Stop(string.IsNullOrWhiteSpace(
                        failureReasonId)
                        ? LuoyangClickToWalkPedestrianIds.NoRouteReasonId
                        : failureReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            _formalMovementService.Complete(_formalMovementWorld, movement.Id);
            if (movement.Status != LuoyangFormalMovementStatus.Completed)
            {
                _pedestrianInstance.Stop(movement.FailureReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            var routePoints = plan.Points.Select(item =>
                _localWorldPositionResolver(item.GlobalEastingMetres,
                    item.GlobalNorthingMetres)).ToArray();
            if (routePoints.Length < 2)
            {
                _pedestrianInstance.Stop(
                    LuoyangClickToWalkPedestrianIds.NoRouteReasonId, true);
                ClearPedestrianRouteVisual();
                return false;
            }
            _lastPedestrianWalkPlan = null;
            _lastHumanScaleRoute = plan;
            _pedestrianInstance.BeginLocalRoute(plan, routePoints);
            SetPedestrianRouteVisual(routePoints);
            _pedestrianTarget.transform.position = routePoints[
                routePoints.Length - 1] + Vector3.up * 0.006f;
            _pedestrianTarget.SetActive(true);
            return true;
        }

        private IReadOnlyList<Vector3> BuildPedestrianRoutePoints(
            LuoyangPedestrianWalkPlan plan)
        {
            var result = plan.FacilityIds.Select(item =>
                _pedestrianNodePositions[item]).ToArray();
            for (var index = 0; index < plan.Segments.Count; index++)
            {
                var from = _pedestrianNodePositions[
                    plan.Segments[index].FromFacilityId];
                var to = _pedestrianNodePositions[
                    plan.Segments[index].ToFacilityId];
                var direction = new Vector3(to.x - from.x, 0f,
                    to.z - from.z).normalized;
                var perpendicular = new Vector3(-direction.z, 0f,
                    direction.x);
                var actualOffset = plan.Segments[index].LateralOffsetMetres /
                                   _horizontalMetresPerUnit;
                var visibleOffset = Mathf.Sign(actualOffset) * Mathf.Max(
                    Mathf.Abs(actualOffset), 0.012f);
                if (index == 0) result[0] += perpendicular * visibleOffset;
                result[index + 1] += perpendicular * visibleOffset;
            }
            return result;
        }

        private void SetPedestrianRouteVisual(
            IReadOnlyList<Vector3> routePoints)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var index = 1; index < routePoints.Count; index++)
                AddRibbon(vertices, triangles,
                    routePoints[index - 1] + Vector3.up * 0.055f,
                    routePoints[index] + Vector3.up * 0.055f, 0.040f);
            SetMesh(_pedestrianRouteMesh, vertices, triangles);
            _pedestrianRouteRenderer.enabled = vertices.Count > 0;
        }

        private void ClearPedestrianRouteVisual()
        {
            if (_pedestrianRouteMesh != null) _pedestrianRouteMesh.Clear();
            if (_pedestrianRouteRenderer != null)
                _pedestrianRouteRenderer.enabled = false;
            if (_pedestrianTarget != null) _pedestrianTarget.SetActive(false);
        }

        private static float HorizontalDistanceSquared(Vector3 first,
            Vector3 second)
        {
            var x = first.x - second.x;
            var z = first.z - second.z;
            return x * x + z * z;
        }

        public void RefreshTraversalState(
            LuoyangPassageTraversalSession passageSession,
            WorldState passageWorld = null)
        {
            if (passageSession == null) throw new ArgumentNullException(
                nameof(passageSession));
            _passageSession = passageSession;
            var pedestrianPlan = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(_refinementPlan, passageSession, passageWorld);
            foreach (var state in pedestrianPlan.States)
                if (_passagePresentationInstancesByFacilityId.TryGetValue(
                        state.FacilityId, out var presentationInstance))
                    presentationInstance.Apply(state);
            ActivePedestrianBlockerCount =
                _passagePresentationInstancesByFacilityId.Values.Count(item =>
                    item.BlocksPedestrianTraversal);
            DamagedPassagePresentationCount =
                _passagePresentationInstancesByFacilityId.Values.Count(item =>
                    string.Equals(item.TraversalStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                        StringComparison.Ordinal));
            DestroyedPassagePresentationCount =
                _passagePresentationInstancesByFacilityId.Values.Count(item =>
                    string.Equals(item.TraversalStatusId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .DestroyedStatusId,
                        StringComparison.Ordinal));
            ActiveRepairScaffoldCount =
                _passagePresentationInstancesByFacilityId.Values.Count(item =>
                    item.IsRepairing);
            if (!UsesFormalPlayerMovement && _pedestrianInstance != null &&
                _pedestrianInstance.IsWalking &&
                _pedestrianInstance.RemainingFacilityIds().Any(item =>
                    passageSession.TryGet(item, out var passage) &&
                    !passage.CanTraverse))
            {
                _pedestrianInstance.Stop(
                    LuoyangClickToWalkPedestrianIds.BlockedPassageReasonId,
                    true);
                ClearPedestrianRouteVisual();
            }
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

        public LuoyangPassagePedestrianPresentationInstance
            GetPassagePresentation(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId) ||
                !_passagePresentationInstancesByFacilityId.TryGetValue(
                    facilityId, out var instance))
                throw new KeyNotFoundException(
                    "The Luoyang passage is not resident in the current " +
                    "presentation window: " + facilityId);
            return instance;
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
            if (_passageStateCubeMesh != null)
                UnityEngine.Object.DestroyImmediate(_passageStateCubeMesh);
            if (_pedestrianRouteMesh != null)
                UnityEngine.Object.DestroyImmediate(_pedestrianRouteMesh);
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
            if (_passageOpenMaterial != null)
                UnityEngine.Object.DestroyImmediate(_passageOpenMaterial);
            if (_passageClosedMaterial != null)
                UnityEngine.Object.DestroyImmediate(_passageClosedMaterial);
            if (_passageDamagedMaterial != null)
                UnityEngine.Object.DestroyImmediate(_passageDamagedMaterial);
            if (_passageDestroyedMaterial != null)
                UnityEngine.Object.DestroyImmediate(_passageDestroyedMaterial);
            if (_passageRepairingMaterial != null)
                UnityEngine.Object.DestroyImmediate(_passageRepairingMaterial);
            if (_pedestrianBodyMaterial != null)
                UnityEngine.Object.DestroyImmediate(_pedestrianBodyMaterial);
            if (_pedestrianSkinMaterial != null)
                UnityEngine.Object.DestroyImmediate(_pedestrianSkinMaterial);
            if (_pedestrianRouteMaterial != null)
                UnityEngine.Object.DestroyImmediate(_pedestrianRouteMaterial);
            if (_pedestrianTargetMaterial != null)
                UnityEngine.Object.DestroyImmediate(_pedestrianTargetMaterial);
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

        private static float ResolvePassageYawDegrees(
            LuoyangRoadTraversalRefinementPlan plan, string facilityId)
        {
            var passage = plan.NavigationNodesByFacilityId[facilityId];
            var approachNodes = plan.NavigationEdges.Where(edge =>
                    string.Equals(edge.EdgeProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId,
                        StringComparison.Ordinal) &&
                    (string.Equals(edge.FromNodeId, passage.NodeId,
                         StringComparison.Ordinal) ||
                     string.Equals(edge.ToNodeId, passage.NodeId,
                         StringComparison.Ordinal)))
                .Select(edge => edge.FromNodeId == passage.NodeId
                    ? edge.ToNodeId : edge.FromNodeId)
                .Select(nodeId => plan.NavigationNodes.First(node =>
                    string.Equals(node.NodeId, nodeId,
                        StringComparison.Ordinal)))
                .OrderBy(node => node.NodeId, StringComparer.Ordinal).ToArray();
            var rowDelta = approachNodes.Length >= 2
                ? approachNodes[1].GridRow - approachNodes[0].GridRow
                : approachNodes.Length == 1
                    ? approachNodes[0].GridRow - passage.GridRow
                    : 1;
            var columnDelta = approachNodes.Length >= 2
                ? approachNodes[1].GridColumn - approachNodes[0].GridColumn
                : approachNodes.Length == 1
                    ? approachNodes[0].GridColumn - passage.GridColumn
                    : 0;
            if (rowDelta == 0 && columnDelta == 0) rowDelta = 1;
            return Mathf.Atan2(columnDelta, rowDelta) * Mathf.Rad2Deg;
        }

        private static Mesh CreateUnitCubeMesh()
        {
            var mesh = new Mesh
            {
                name = "Luoyang Passage Stateful Presentation Unit Cube V1"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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

    public static class LuoyangSupplyFreightPresentationIds
    {
        public const string RootName =
            "LUOYANG_OUTER_SUPPLY_FREIGHT_PRESENTATION_V1";
        public const string InTransitStateId =
            "presentation.freight.in-transit.v1";
        public const string WaitingAtPassageStateId =
            "presentation.freight.waiting-at-passage.v1";
        public const string ArrivedStateId =
            "presentation.freight.arrived.v1";
    }

    /// <summary>
    /// Read-only Unity marker for one persisted CivilianFreight. The marker
    /// never changes inventory, route progress, ownership, or arrival state.
    /// </summary>
    public sealed class LuoyangSupplyFreightMarker : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _properties =
            new MaterialPropertyBlock();

        public string FreightId { get; private set; }
        public string CarrierPersonId { get; private set; }
        public string ProductDefinitionId { get; private set; }
        public string PresentationStateId { get; private set; }
        public string WaitingOnFormalWorldObjectId { get; private set; }
        public ulong CurrentCellId64 { get; private set; }
        public long RemainingCargoQuantity { get; private set; }
        public int RouteRevision { get; private set; }

        public void Initialize(MeshRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(
                nameof(renderer));
        }

        public void Apply(CivilianFreightState freight, Vector3 position)
        {
            if (freight == null) throw new ArgumentNullException(
                nameof(freight));
            if (_renderer == null)
                throw new InvalidOperationException(
                    "Initialize the freight marker before applying state.");
            FreightId = freight.Id;
            CarrierPersonId = freight.CarrierPersonId;
            ProductDefinitionId = freight.ProductDefinitionId;
            CurrentCellId64 = freight.CellRouteCurrentCellId64;
            RemainingCargoQuantity = freight.RemainingCargoQuantity;
            RouteRevision = freight.CellRouteRevision;
            WaitingOnFormalWorldObjectId =
                freight.CellRouteWaitingOnFormalWorldObjectId;
            PresentationStateId = freight.Status ==
                    CivilianFreightStatus.Completed
                ? LuoyangSupplyFreightPresentationIds.ArrivedStateId
                : freight.CellRouteWaiting
                    ? LuoyangSupplyFreightPresentationIds
                        .WaitingAtPassageStateId
                    : LuoyangSupplyFreightPresentationIds.InTransitStateId;
            transform.position = position + Vector3.up * 0.11f;
            transform.localScale = string.Equals(
                    freight.CellRouteMovementCapabilityId,
                    MovementCapabilityIds.Cart,
                    StringComparison.Ordinal)
                ? new Vector3(0.18f, 0.09f, 0.11f)
                : new Vector3(0.10f, 0.13f, 0.08f);
            var color = freight.Status == CivilianFreightStatus.Completed
                ? new Color(0.30f, 0.82f, 0.42f, 1f)
                : freight.CellRouteWaiting
                    ? new Color(1f, 0.46f, 0.08f, 1f)
                    : new Color(0.16f, 0.72f, 0.86f, 1f);
            _properties.Clear();
            _properties.SetColor("_Color", color);
            _properties.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_properties);
        }
    }

    public sealed class LuoyangSupplyFreightPresentationRuntime : IDisposable
    {
        private readonly Dictionary<string, LuoyangSupplyFreightMarker>
            _markers = new Dictionary<string, LuoyangSupplyFreightMarker>(
                StringComparer.Ordinal);
        private readonly Func<ulong, Vector3> _cellPositionResolver;
        private readonly Material _sharedMaterial;

        private LuoyangSupplyFreightPresentationRuntime(
            Func<ulong, Vector3> cellPositionResolver)
        {
            _cellPositionResolver = cellPositionResolver ??
                throw new ArgumentNullException(nameof(cellPositionResolver));
            Root = new GameObject(
                LuoyangSupplyFreightPresentationIds.RootName);
            _sharedMaterial = CreateSupplyFreightMaterial();
        }

        public GameObject Root { get; }
        public int LoadedMarkerCount => _markers.Count;
        public IReadOnlyDictionary<string, LuoyangSupplyFreightMarker>
            Markers => _markers;

        public static LuoyangSupplyFreightPresentationRuntime Build(
            WorldState world, Func<ulong, Vector3> cellPositionResolver)
        {
            var runtime = new LuoyangSupplyFreightPresentationRuntime(
                cellPositionResolver);
            runtime.Refresh(world);
            return runtime;
        }

        public void Refresh(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var visible = world.CivilianFreights.Where(item =>
                    item != null && item.UsesCellRoute &&
                    item.CellRouteCurrentCellId64 != 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
            var ids = new HashSet<string>(visible.Select(item => item.Id),
                StringComparer.Ordinal);
            foreach (var obsolete in _markers.Keys.Where(item =>
                         !ids.Contains(item)).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(
                    _markers[obsolete].gameObject);
                _markers.Remove(obsolete);
            }
            foreach (var freight in visible)
            {
                if (!_markers.TryGetValue(freight.Id, out var marker))
                {
                    var markerObject = GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                    markerObject.name = "FREIGHT_" + freight.Id;
                    markerObject.transform.SetParent(Root.transform, false);
                    var renderer = markerObject.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = _sharedMaterial;
                    marker = markerObject.AddComponent<
                        LuoyangSupplyFreightMarker>();
                    marker.Initialize(renderer);
                    _markers.Add(freight.Id, marker);
                }
                marker.Apply(freight,
                    _cellPositionResolver(
                        freight.CellRouteCurrentCellId64));
            }
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
            if (_sharedMaterial != null)
                UnityEngine.Object.DestroyImmediate(_sharedMaterial);
            _markers.Clear();
        }

        private static Material CreateSupplyFreightMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException(
                    "No shader is available for supply freight markers.");
            return new Material(shader)
            {
                name = "Luoyang Supply Freight Marker Material V1",
                color = Color.white
            };
        }
    }
}
