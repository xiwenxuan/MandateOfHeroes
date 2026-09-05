using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mandate.Presentation
{
    [Serializable]
    public sealed class PlayableLuoyangViewPerformanceSnapshot
    {
        public string ViewMode;
        public double FramesPerSecond;
        public double EnterMilliseconds;
        public int ActiveGameObjectCount;
        public long ManagedGcDeltaBytes;
    }

    public sealed class PlayableLuoyangGameController : MonoBehaviour
    {
        private const float TopHudHeight = 104f;
        private const float BottomHudHeight = 64f;
        private const float BuildingPanelWidth = 360f;
        private const float LocalCameraDistance = 30f;
        private const float LocalCameraOrthographicSize = 8f;
        private const float DirectActorVisualScale = 1f;
        private const float DirectMovementPlaybackSpeed = 24f;
        private SimulationDashboard _dashboard;
        private HanWorldNaturalMapController _map;
        private LuoyangCountyPlanningPresentationController _countyPlanning;
        private WorldState _world;
        private WorldCommandRuntime _commandRuntime;
        private bool _active;
        private bool _paused;
        private readonly LuoyangPlayableViewState _viewState =
            new LuoyangPlayableViewState();
        private bool _followPlayer = true;
        private bool _cameraPoseInitialized;
        private bool _middleDragActive;
        private bool _rightDragActive;
        private bool _runtimeReferenceFailureLogged;
        private Vector2 _lastDragMousePosition;
        private Vector3 _cameraFocus;
        private float _cameraYawDegrees = 35f;
        private float _cameraPitchDegrees = 52f;
        private string _planningToolbarCategory = "building";
        private double _lastViewEnterMilliseconds;
        private long _lastViewManagedGcDeltaBytes;
        private string _message =
            "左键选择建筑；中键拖动平移，右键拖动旋转。";
        private string _lastMovementState = string.Empty;
        private GUIStyle _hudTitleStyle;
        private GUIStyle _hudStyle;
        private GUIStyle _hudSmallStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _pauseTitleStyle;
        private GUIStyle _panelTitleStyle;
        private GUIStyle _panelBodyStyle;
        private GUIStyle _panelHintStyle;
        private GUIStyle _objectiveStyle;
        private GUIStyle _playerMarkerStyle;

        public bool IsActive => _active;
        public bool IsPaused => _paused;
        public bool IsStrategicMapVisible => _viewState.Mode ==
                                             LuoyangPlayableViewMode.World;
        public bool IsCountyViewVisible => _viewState.Mode ==
                                           LuoyangPlayableViewMode.County;
        public bool IsPersonViewVisible => _viewState.Mode ==
                                           LuoyangPlayableViewMode.Person;
        public bool IsCountyDetailPresentationVisible =>
            IsCountyViewVisible && _countyPlanning != null &&
            _countyPlanning.IsActive;
        public bool IsCountyPlanningVisible =>
            IsCountyDetailPresentationVisible &&
            _viewState.CountySubView == CountySubViewMode.Planning;
        public LuoyangPlayableViewMode ViewMode => _viewState.Mode;
        public string ViewFocusFacilityId => _viewState.FocusFacilityId;
        public string ObservedCountyId => _viewState.ObservedCountyId;
        public CountySubViewMode CountySubView => _viewState.CountySubView;
        public bool IsFollowingPlayer => _followPlayer;
        public WorldState BoundWorld => _world;
        public HanWorldNaturalMapController NaturalMap => _map;
        public string LastMessage => _message;
        public Vector3 CameraFocus => _cameraFocus;
        public float CameraYawDegrees => _cameraYawDegrees;
        public float CameraPitchDegrees => _cameraPitchDegrees;
        public FacilityState SelectedFacility => FindFacility(
            _map?.SelectedLuoyangFacilityId);
        public string CurrentObjectiveText => BuildCurrentObjectiveText();
        public LuoyangCountyPlanningPresentationController CountyPlanning =>
            _countyPlanning;
        public bool IsInkWorldStyle => _map != null &&
            _map.ActiveArtStyle == HanWorldArtStyle.InkLandscapePrototype;
        public bool IsStrategicDioramaWorldStyle => _map != null &&
            _map.ActiveArtStyle == HanWorldArtStyle.HanStrategicDiorama;

        public bool SetWorldMapStrategicDioramaStyle(bool enabled)
        {
            if (!_active || !TryEnsureMapRuntimeReady(
                    "World map strategic diorama style")) return false;
            var beforeRevision = _world?.Revision ?? 0;
            _map.SetArtStyle(enabled
                ? HanWorldArtStyle.HanStrategicDiorama
                : HanWorldArtStyle.ChineseSemiRealistic);
            if (_world != null && _world.Revision != beforeRevision)
                throw new InvalidOperationException(
                    "A presentation style changed WorldState.");
            _message = enabled
                ? "已切换汉末彩色立体战略沙盘：分层山川、城邑微缩、道路水系与上下文格网。"
                : "已切换自然地形对照；世界状态与地图权威数据未改变。";
            return true;
        }

        public bool SetWorldMapInkStyle(bool ink)
        {
            if (!_active || !TryEnsureMapRuntimeReady(
                    "World map style")) return false;
            var beforeRevision = _world?.Revision ?? 0;
            _map.SetArtStyle(ink
                ? HanWorldArtStyle.InkLandscapePrototype
                : HanWorldArtStyle.ChineseSemiRealistic);
            if (_world != null && _world.Revision != beforeRevision)
                throw new InvalidOperationException(
                    "A presentation style changed WorldState.");
            _message = ink
                ? "已切换水墨原型：绢本、地形墨染、青墨河流、赭色道路与三级边界。"
                : "已恢复当前地图样式；世界状态与地图权威数据未改变。";
            return true;
        }

        public PlayableLuoyangViewPerformanceSnapshot
            GetViewPerformanceSnapshot()
        {
            var delta = Time.unscaledDeltaTime;
            return new PlayableLuoyangViewPerformanceSnapshot
            {
                ViewMode = ViewMode.ToString().ToUpperInvariant(),
                FramesPerSecond = delta <= 0f ? 0d : 1d / delta,
                EnterMilliseconds = _lastViewEnterMilliseconds,
                ActiveGameObjectCount = UnityEngine.Object
                    .FindObjectsOfType<GameObject>()
                    .Count(item => item.activeInHierarchy),
                ManagedGcDeltaBytes = _lastViewManagedGcDeltaBytes
            };
        }

        public bool PrepareMap()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _map = GetComponent<HanWorldNaturalMapController>() ??
                gameObject.AddComponent<HanWorldNaturalMapController>();
            _map.enabled = true;
            _map.SetPresentationUiVisible(false);
            _map.ExternalPlayerHudVisible = true;
            _map.ExternalPlayerHudPointerGuard = IsPointerOverPlayerUi;
            if (!_map.HasRuntimeReferences)
            {
                var camera = Camera.main;
                if (camera == null)
                    throw new InvalidOperationException(
                        "PlayableDemo 缺少 Main Camera。");
                if (!_map.IsReady) _map.SetPresentationCamera(camera);
                if (!_map.TryEnsureRuntimeReferences("PlayableDemo.PrepareMap"))
                    return false;
            }
            _runtimeReferenceFailureLogged = false;
            return _map.LuoyangHumanScaleLocalMapPlan != null;
        }

        private bool TryEnsureMapRuntimeReady(string context)
        {
            _map = _map ?? GetComponent<HanWorldNaturalMapController>();
            if (_map != null && _map.TryEnsureRuntimeReferences(context))
            {
                _runtimeReferenceFailureLogged = false;
                if (_world != null && !_map.LuoyangPassageWorldBound)
                {
                    _commandRuntime = _commandRuntime ??
                        new WorldCommandRuntime();
                    _map.BindLuoyangPassageWorld(_world, _commandRuntime);
                    _map.SetLuoyangPedestrianPlaybackSpeed(
                        DirectMovementPlaybackSpeed);
                }
                return true;
            }
            _message = "地图运行时引用不可用，本次视角切换已取消；请重新进入 Play Mode。";
            if (!_runtimeReferenceFailureLogged)
            {
                Debug.LogError("PlayableDemo map route failed safely: " +
                               context + ". " + (_map?.LastError ??
                                   "Map controller is missing."));
                _runtimeReferenceFailureLogged = true;
            }
            return false;
        }

        public bool Begin(WorldState world, SimulationDashboard dashboard)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (dashboard == null)
                throw new ArgumentNullException(nameof(dashboard));
            if (!PrepareMap()) return false;

            _dashboard = dashboard;
            _world = world;
            _commandRuntime = new WorldCommandRuntime();
            if (_map.LuoyangPassageWorldBound)
                _map.UnbindLuoyangPassageWorld();
            _viewState.ShowPlayer();
            if (!_map.ShowPlayableLuoyangPersonNearfield()) return false;
            _map.BindLuoyangPassageWorld(_world, _commandRuntime);
            _map.SetLuoyangPedestrianPlaybackSpeed(
                DirectMovementPlaybackSpeed);
            _active = true;
            _paused = false;
            _followPlayer = true;
            _lastMovementState = _map.LuoyangPedestrianMovementStateId ??
                string.Empty;
            _message =
                "已进入洛阳。先从住处前往市场完成交易，再到市曹柜台接受差事。";
            var actor = _map.GetLuoyangClickWalkPedestrian();
            actor.transform.localScale = Vector3.one *
                                         DirectActorVisualScale;
            InitializeLocalCamera(true);
            _map.SelectLuoyangFacility(
                PlayableLuoyangWorldContractIds.MarketFacilityId);
            return true;
        }

        public void Suspend()
        {
            _active = false;
            _paused = false;
            _viewState.ShowPlayer();
            if (_map != null)
            {
                _map.SetHumanScaleLocalPresentationVisible(false);
                _map.ExternalPlayerHudVisible = false;
                _map.ExternalPlayerHudPointerGuard = null;
                if (_map.LuoyangPassageWorldBound)
                    _map.UnbindLuoyangPassageWorld();
                _map.enabled = false;
            }
            gameObject.SetActive(false);
        }

        public bool ToggleStrategicMap()
        {
            if (!_active || !TryEnsureMapRuntimeReady("ToggleStrategicMap"))
                return false;
            if (IsStrategicMapVisible)
            {
                ReturnToLuoyangView();
                return true;
            }
            ShowWorldView();
            return true;
        }

        public bool ShowWorldView()
        {
            if (!_active || !TryEnsureMapRuntimeReady("M 天下")) return false;
            EndCountyPlanningPresentation();
            var before = GC.GetTotalMemory(false);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            _viewState.ShowWorld();
            _followPlayer = false;
            _map.HidePlayableLuoyangPresentation();
            _map.SetStrategicMapPresentationVisible(true);
            _map.SetArtStyle(HanWorldArtStyle.HanStrategicDiorama,
                refreshPresentation: false);
            _map.SetWorldView();
            _map.SetCellOverlayVisible(false);
            _map.SetTransportOverlayVisible(false);
            _map.SetRoadOverlayVisible(true);
            _map.SetRiverOverlayVisible(true);
            _map.SetAdministrativeOverlayVisible(true);
            _map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.Province);
            var player = Player();
            var playerCountyId = ResolvePlayerCounty();
            if (!string.IsNullOrWhiteSpace(playerCountyId))
                _map.FocusWorldNearCounty(playerCountyId);
            else if (player.CurrentCellId64 != 0UL)
                _map.FocusWorldNearCell(player.CurrentCellId64);
            RecordViewTransition(timer, before);
            _message =
                "天下统一地图；滚轮缩放行政层级，左键选县，中键平移，右键旋转。";
            return true;
        }

        public bool ShowCountyView()
        {
            if (!_active || !TryEnsureMapRuntimeReady("C 县域")) return false;
            var countyId = ResolveCountyViewTarget();
            if (string.IsNullOrWhiteSpace(countyId))
            {
                _message = "未能从当前选择或人物位置解析县域。";
                return false;
            }
            EndCountyPlanningPresentation();
            var before = GC.GetTotalMemory(false);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var focusId = _map.SelectedLuoyangFacilityId;
            _viewState.ShowCounty(countyId, CountySubViewMode.Overview,
                focusId);
            _followPlayer = false;
            _map.HidePlayableLuoyangPresentation();
            _map.SetCellOverlayVisible(false);
            if (!_map.EnterCountyPlanning(countyId))
            {
                _message = "该县当前没有可读取的县域空间。";
                return false;
            }
            // The county world-space presentation reuses the strategic map
            // camera with a local 50 m coordinate system. Keeping the 2 km
            // administrative fill alive here draws that global mesh across
            // the local terrain and can completely cover the sandbox at Mid
            // and Near LOD. Administrative facts remain available in the
            // view state; only their world-map presentation is hidden.
            _map.SetAdministrativeOverlayVisible(false);
            _map.SetStrategicMapPresentationVisible(false);
            if (!EnsureCountyDetailPresentation(countyId,
                    CountySubViewMode.Overview))
            {
                _map.ExitCountyPlanning();
                _map.SetStrategicMapPresentationVisible(true);
                _map.SetAdministrativeOverlayVisible(true);
                _message = string.IsNullOrWhiteSpace(
                    _countyPlanning.LastError)
                    ? "县域详细表现初始化失败。"
                    : "县域详细表现初始化失败，详情见 Console。";
                return false;
            }
            RecordViewTransition(timer, before);
            _message = CountyDisplayName(countyId) +
                "｜县域总览；城区与建设是同一 50m 空间的子视图。";
            return true;
        }

        [Obsolete("City is a County UrbanArea subview. Use ShowCountyView.")]
        public bool ShowCityView()
        {
            return ShowCountyView() &&
                   ShowCountySubView(CountySubViewMode.UrbanArea);
        }

        public bool ShowCountySubView(CountySubViewMode subView)
        {
            if (!_active || !IsCountyViewVisible ||
                string.IsNullOrWhiteSpace(ObservedCountyId)) return false;
            if (!EnsureCountyDetailPresentation(ObservedCountyId, subView))
            {
                if (subView != CountySubViewMode.Overview)
                {
                    _message = "该县尚未安装可用的 50m 县域详细包。";
                    return false;
                }
            }
            _viewState.SetCountySubView(subView);
            _message = CountyDisplayName(ObservedCountyId) + "｜县域｜" +
                       CountySubViewLabel(subView) +
                       "；只改变镜头与表现，不改变世界事实。";
            return true;
        }

        public bool ShowCountyUrbanAreaView()
        {
            return ShowCountySubView(CountySubViewMode.UrbanArea);
        }

        public bool ShowCountyPlanningSubView()
        {
            return ShowCountySubView(CountySubViewMode.Planning);
        }

        private bool EnsureCountyDetailPresentation(string countyId,
            CountySubViewMode subView)
        {
            _countyPlanning = GetComponent<
                LuoyangCountyPlanningPresentationController>() ??
                gameObject.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
            var ready = _countyPlanning.IsActive &&
                string.Equals(_countyPlanning.CountyId, countyId,
                    StringComparison.Ordinal)
                ? _countyPlanning.SetPresentationMode(subView)
                : _countyPlanning.Begin(countyId, subView);
            if (!ready) return false;
            return _countyPlanning.EnsureWorldSpacePresentation(
                _map.PresentationCamera);
        }

        private string ResolveCountyViewTarget()
        {
            var selection = _map?.AdministrativeSelection;
            if (selection != null && selection.Level ==
                    AdministrativeRegionLevel.County)
                return selection.RegionId;
            return ResolvePlayerCounty();
        }

        private string ResolvePlayerCounty()
        {
            var player = Player();
            if (_map.TryResolveCountyIdForLocation(player.LocationId,
                    out var locationCountyId))
                return locationCountyId;
            return player.CurrentCellId64 != 0UL &&
                   _map.TryResolveCountyId(player.CurrentCellId64,
                       out var countyId)
                ? countyId
                : string.Empty;
        }

        private string CountyDisplayName(string countyId)
        {
            return _map != null &&
                   _map.TryGetAdministrativeRegionDisplayName(countyId,
                       out var displayName)
                ? displayName
                : countyId;
        }

        private static string CountySubViewLabel(CountySubViewMode mode)
        {
            switch (mode)
            {
                case CountySubViewMode.UrbanArea: return "城区";
                case CountySubViewMode.Planning: return "建设";
                default: return "总览";
            }
        }

        private static string CountyLodLabel(CountyMapPresentationLod lod)
        {
            switch (lod)
            {
                case CountyMapPresentationLod.Near:
                    return "建设近景 Near";
                case CountyMapPresentationLod.Mid:
                    return "城区中景 Mid";
                default:
                    return "县域全览 Far";
            }
        }

        public bool ShowPersonView()
        {
            if (!_active || !TryEnsureMapRuntimeReady("F 人物")) return false;
            EndCountyPlanningPresentation();
            var before = GC.GetTotalMemory(false);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            _viewState.ShowPlayer();
            _followPlayer = true;
            if (!_map.ShowPlayableLuoyangPersonNearfield()) return false;
            var actor = _map.GetLuoyangClickWalkPedestrian();
            actor.transform.localScale = Vector3.one * DirectActorVisualScale;
            InitializeLocalCamera(true);
            RecordViewTransition(timer, before);
            _message = "已返回玩家真实位置；视角切换没有移动人物。";
            return true;
        }

        public bool EnterSelectedFacilityNearfield()
        {
            if (!_active || _map == null || !IsPersonViewVisible) return false;
            var facilityId = _map.SelectedLuoyangFacilityId;
            if (string.IsNullOrWhiteSpace(facilityId))
            {
                _message = "请先在人物近景选择一座建筑。";
                return false;
            }
            var before = GC.GetTotalMemory(false);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            _viewState.ObserveFacility(facilityId);
            _followPlayer = false;
            if (!_map.ShowPlayableLuoyangPersonNearfield(facilityId))
                return false;
            _map.SelectLuoyangFacility(facilityId);
            var actor = _map.GetLuoyangClickWalkPedestrian();
            actor.transform.localScale = Vector3.one * DirectActorVisualScale;
            InitializeNearfieldObservationCamera(facilityId);
            RecordViewTransition(timer, before);
            _message = $"正在观察{FindFacilityName(facilityId)}近景；人物位置未改变，按 F 回到人物。";
            return true;
        }

        private void RecordViewTransition(System.Diagnostics.Stopwatch timer,
            long managedBytesBefore)
        {
            timer.Stop();
            _lastViewEnterMilliseconds = timer.Elapsed.TotalMilliseconds;
            _lastViewManagedGcDeltaBytes = Math.Max(0L,
                GC.GetTotalMemory(false) - managedBytesBefore);
        }

        public bool TravelFromPlayerToSelectedFacility()
        {
            if (!_active || _map == null || !IsPersonViewVisible) return false;
            var facilityId = _map.SelectedLuoyangFacilityId;
            if (string.IsNullOrWhiteSpace(facilityId)) return false;
            return TryMoveToSelectedFacility();
        }

        public void ReturnToLuoyangView()
        {
            ShowPersonView();
        }

        public bool TryMoveToSelectedFacility()
        {
            if (!_active || _paused || !IsPersonViewVisible || _map == null)
                return false;
            var selectedFacilityId = _map.SelectedLuoyangFacilityId;
            if (string.IsNullOrWhiteSpace(selectedFacilityId))
            {
                _message = "请先左键选择一座建筑。";
                return false;
            }
            if (string.Equals(_map.LuoyangPedestrianCurrentFacilityId,
                    selectedFacilityId, StringComparison.Ordinal))
            {
                _message = $"你已在{FindFacilityName(selectedFacilityId)}。";
                return true;
            }
            if (!_map.SetLuoyangPedestrianDestination(selectedFacilityId))
            {
                _message = BuildMovementFailureMessage();
                return false;
            }
            _followPlayer = true;
            _message = $"正在前往{FindFacilityName(selectedFacilityId)}。";
            return true;
        }

        public bool TryMoveToObjectiveFacility()
        {
            var targetId = CurrentObjectiveFacilityId();
            if (string.IsNullOrWhiteSpace(targetId))
            {
                _message = "首条洛阳生活目标已经完成，可以自由访问建筑。";
                return false;
            }
            if (!_map.SelectLuoyangFacility(targetId))
            {
                _message = "目标建筑当前不在可见范围，请先用中键平移镜头。";
                return false;
            }
            return TryMoveToSelectedFacility();
        }

        public bool ExecuteSelectedBuildingAction(string actionId)
        {
            if (!_active || _paused || !IsPersonViewVisible ||
                _map == null || _dashboard == null) return false;
            if (_map.LuoyangPedestrianIsWalking)
            {
                _message = "请先等待人物到达建筑。";
                return false;
            }
            var selectedId = _map.SelectedLuoyangFacilityId;
            if (string.IsNullOrWhiteSpace(selectedId) ||
                !string.Equals(selectedId,
                    _map.LuoyangPedestrianCurrentFacilityId,
                    StringComparison.Ordinal))
            {
                _message = "人物到达所选建筑后才能执行这里的行动。";
                return false;
            }
            var facility = FindFacility(selectedId);
            var option = ContextActions(facility).FirstOrDefault(item =>
                string.Equals(item.Id, actionId, StringComparison.Ordinal));
            if (option == null)
            {
                _message = "这座建筑不提供该项行动。";
                return false;
            }
            if (!option.IsAvailable)
            {
                _message = string.IsNullOrWhiteSpace(option.UnavailableReason)
                    ? "当前条件不足。" : option.UnavailableReason;
                return false;
            }
            var result = _dashboard.ExecuteCurrentPlayerAction(actionId);
            _message = result.Success
                ? result.Summary
                : string.IsNullOrWhiteSpace(result.Detail)
                    ? result.Summary : result.Detail;
            return result.Success;
        }

        public bool RestOneDay()
        {
            if (!_active || _paused || _world == null ||
                _map.LuoyangPedestrianIsWalking)
            {
                _message = "移动途中不能休息。";
                return false;
            }
            var result = _dashboard.ExecuteCurrentPlayerAction(
                PlayerActionIds.Rest);
            _message = result.Success
                ? result.Summary
                : string.IsNullOrWhiteSpace(result.Detail)
                    ? result.Summary : result.Detail;
            return result.Success;
        }

        public bool SaveToMemory()
        {
            if (!_active || _dashboard == null) return false;
            var saved = _dashboard.SaveCurrentWorldToMemory();
            _message = saved
                ? "当前世界已保存到内存存档。"
                : "保存失败。";
            return saved;
        }

        private void Update()
        {
            if (!_active || _world == null || _map == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!_paused);
                return;
            }
            if (_paused) return;
            if (!ViewHotkeysBlockedByTextInput())
            {
                if (Input.GetKeyDown(KeyCode.M)) ShowWorldView();
                if (Input.GetKeyDown(KeyCode.C)) ShowCountyView();
                if (Input.GetKeyDown(KeyCode.F)) ShowPersonView();
                if (IsCountyViewVisible && Input.GetKeyDown(KeyCode.Home))
                    ShowCountySubView(CountySubViewMode.Overview);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsPersonViewVisible) TryMoveToSelectedFacility();
            }
            if (IsCountyPlanningVisible && Input.GetKeyDown(KeyCode.R))
                _countyPlanning.RotateClockwise();
            else if (IsPersonViewVisible && Input.GetKeyDown(KeyCode.R))
                RestOneDay();
            if (IsCountyPlanningVisible && Input.GetKeyDown(KeyCode.Q))
                CreatePlanningDraft();
            if (IsCountyPlanningVisible && Input.GetKeyDown(KeyCode.Tab))
                _countyPlanning.RotateClockwise();
            if (IsCountyPlanningVisible &&
                (Input.GetKeyDown(KeyCode.Z) &&
                 (Input.GetKey(KeyCode.LeftControl) ||
                  Input.GetKey(KeyCode.RightControl)) ||
                 Input.GetKeyDown(KeyCode.Z)))
                UndoPlanningDraft();
            if (IsCountyPlanningVisible &&
                (Input.GetKeyDown(KeyCode.Y) &&
                 (Input.GetKey(KeyCode.LeftControl) ||
                  Input.GetKey(KeyCode.RightControl)) ||
                 Input.GetKeyDown(KeyCode.X)))
                RedoPlanningDraft();
            if (IsCountyPlanningVisible &&
                Input.GetKeyDown(KeyCode.Delete) &&
                _countyPlanning.DeleteSelectedDrafts())
                _message = "已删除所选规划草案；正式世界没有变化。";
            if (Input.GetKeyDown(KeyCode.S)) SaveToMemory();

            UpdateCameraControls();
            RefreshMovementMessage();
        }

        private void UpdateCameraControls()
        {
            if (IsCountyDetailPresentationVisible)
            {
                UpdateCountyPlanningMapControls();
                return;
            }
            var camera = _map.PresentationCamera;
            if (camera == null) return;
            UpdateMouseCameraControls(camera);
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                _followPlayer = false;
                var right = Vector3.ProjectOnPlane(
                    camera.transform.right, Vector3.up).normalized;
                var forward = Vector3.ProjectOnPlane(
                    camera.transform.up, Vector3.up).normalized;
                var speed = Mathf.Max(0.8f,
                    camera.orthographicSize * 0.75f);
                var motion = (right * horizontal + forward * vertical) *
                             (speed * Time.unscaledDeltaTime);
                camera.transform.position += motion;
                _cameraFocus += motion;
            }

            if (!IsPointerOverPlayerUi(Input.mousePosition))
            {
                var scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    if (IsStrategicMapVisible)
                    {
                        _map.AdjustAdministrativeZoom(-scroll,
                            new Vector2(
                                Input.mousePosition.x / Screen.width,
                                Input.mousePosition.y / Screen.height));
                    }
                    else
                    {
                        const float minimum = 1.6f;
                        const float maximum = 160f;
                        camera.orthographicSize = Mathf.Clamp(
                            camera.orthographicSize *
                            Mathf.Pow(0.88f, scroll), minimum, maximum);
                    }
                }
            }

            if (_followPlayer && IsPersonViewVisible) FocusPlayerNow();
        }

        private void UpdateCountyPlanningMapControls()
        {
            var screenMouse = (Vector2)Input.mousePosition;
            var guiMouse = new Vector2(screenMouse.x,
                Screen.height - screenMouse.y);
            var mapRect = CountyViewMapRect();
            if (mapRect.Contains(guiMouse))
            {
                var scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    var anchor = new Vector2(
                        (guiMouse.x - mapRect.x) / mapRect.width,
                        (guiMouse.y - mapRect.y) / mapRect.height);
                    if (_countyPlanning.ZoomViewport(scroll, anchor))
                        _message = "县域 50m 空间已缩放；中键平移，Alt+右键旋转。";
                }
            }
            if (Input.GetMouseButtonDown(2) && mapRect.Contains(guiMouse))
            {
                _middleDragActive = true;
                _lastDragMousePosition = screenMouse;
            }
            if (Input.GetMouseButtonDown(1) &&
                (Input.GetKey(KeyCode.LeftAlt) ||
                 Input.GetKey(KeyCode.RightAlt)) &&
                mapRect.Contains(guiMouse))
            {
                _rightDragActive = true;
                _lastDragMousePosition = screenMouse;
            }

            if (_middleDragActive && Input.GetMouseButton(2))
            {
                var screenDelta = screenMouse - _lastDragMousePosition;
                var guiDelta = new Vector2(screenDelta.x, -screenDelta.y);
                if (PanCountyPlanningViewByGuiDelta(guiDelta, mapRect))
                    _message =
                        "县域视图已平移；Alt+右键旋转镜头，R旋转建筑。";
                _lastDragMousePosition = screenMouse;
            }
            if (_rightDragActive && Input.GetMouseButton(1))
            {
                var screenDelta = screenMouse - _lastDragMousePosition;
                var guiDelta = new Vector2(screenDelta.x, -screenDelta.y);
                if (RotateCountyPlanningViewByGuiDelta(guiDelta))
                    _message = "县域视图朝向 " +
                        Mathf.RoundToInt(_countyPlanning
                            .ViewRotationDegrees) +
                        "°；R用于旋转建筑。";
                _lastDragMousePosition = screenMouse;
            }
            if (Input.GetMouseButtonUp(2)) _middleDragActive = false;
            if (Input.GetMouseButtonUp(1)) _rightDragActive = false;
        }

        public bool PanCountyPlanningViewByGuiDelta(Vector2 guiDelta,
            Rect mapRect)
        {
            return IsCountyDetailPresentationVisible &&
                   _countyPlanning.PanViewportByGuiDelta(guiDelta, mapRect);
        }

        public bool RotateCountyPlanningViewByGuiDelta(Vector2 guiDelta)
        {
            return IsCountyDetailPresentationVisible &&
                   _countyPlanning.RotateViewportByGuiDelta(guiDelta);
        }

        private void UpdateMouseCameraControls(Camera camera)
        {
            var mouse = (Vector2)Input.mousePosition;
            if (Input.GetMouseButtonDown(2) &&
                !IsPointerOverPlayerUi(mouse))
            {
                _middleDragActive = true;
                _lastDragMousePosition = mouse;
                _followPlayer = false;
            }
            if (Input.GetMouseButtonDown(1) &&
                !IsPointerOverPlayerUi(mouse))
            {
                _rightDragActive = true;
                _lastDragMousePosition = mouse;
                _followPlayer = false;
            }

            if (_middleDragActive && Input.GetMouseButton(2))
            {
                var delta = mouse - _lastDragMousePosition;
                PanCameraByScreenDelta(delta, Screen.height);
                _lastDragMousePosition = mouse;
            }
            if (_rightDragActive && Input.GetMouseButton(1))
            {
                var delta = mouse - _lastDragMousePosition;
                RotateCameraByScreenDelta(delta);
                _lastDragMousePosition = mouse;
            }
            if (Input.GetMouseButtonUp(2)) _middleDragActive = false;
            if (Input.GetMouseButtonUp(1)) _rightDragActive = false;
        }

        public bool PanCameraByScreenDelta(Vector2 screenDelta,
            float viewportHeight)
        {
            var camera = _map?.PresentationCamera;
            if (camera == null || viewportHeight <= 0f) return false;
            if (IsStrategicMapVisible)
            {
                _map.PanAdministrativeMap(new Vector2(
                    screenDelta.x / Mathf.Max(1f, Screen.width),
                    -screenDelta.y / viewportHeight));
                _followPlayer = false;
                return true;
            }
            var unitsPerPixel = camera.orthographic
                ? camera.orthographicSize * 2f / viewportHeight
                : LocalCameraDistance / viewportHeight;
            var right = Vector3.ProjectOnPlane(camera.transform.right,
                Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(camera.transform.up,
                Vector3.up).normalized;
            var motion = (-right * screenDelta.x -
                          forward * screenDelta.y) * unitsPerPixel;
            camera.transform.position += motion;
            _cameraFocus += motion;
            _followPlayer = false;
            return true;
        }

        public bool RotateCameraByScreenDelta(Vector2 screenDelta)
        {
            if (_map?.PresentationCamera == null) return false;
            if (IsStrategicMapVisible)
            {
                _map.RotateAdministrativeMap(screenDelta.x * 0.22f);
                _followPlayer = false;
                return true;
            }
            _cameraYawDegrees += screenDelta.x * 0.22f;
            _cameraPitchDegrees = Mathf.Clamp(
                _cameraPitchDegrees - screenDelta.y * 0.18f, 28f, 78f);
            ApplyLocalCameraPose();
            _followPlayer = false;
            return true;
        }

        private bool IsPointerOverPlayerUi(Vector2 pointer)
        {
            if (pointer.y >= Screen.height - TopHudHeight ||
                pointer.y <= BottomHudHeight) return true;
            if (IsStrategicMapVisible)
            {
                var strategicGuiPointer = new Vector2(pointer.x,
                    Screen.height - pointer.y);
                if (IsCountyPlanningVisible)
                    return CountyPlanningLeftPanel().Contains(
                               strategicGuiPointer) ||
                           CountyPlanningRightPanel().Contains(
                               strategicGuiPointer) ||
                           CountyPlanningBottomToolbarRect().Contains(
                               strategicGuiPointer);
                return new Rect(16f, TopHudHeight + 12f, 438f, 224f)
                           .Contains(strategicGuiPointer) ||
                       new Rect(Screen.width - 376f,
                               TopHudHeight + 12f, 360f, 202f)
                           .Contains(strategicGuiPointer);
            }
            if (IsCountyViewVisible)
            {
                var countyGuiPointer = new Vector2(pointer.x,
                    Screen.height - pointer.y);
                if (CountySubViewNavigationRect().Contains(countyGuiPointer))
                    return true;
                if (IsCountyPlanningVisible)
                    return CountyPlanningLeftPanel().Contains(
                               countyGuiPointer) ||
                           CountyPlanningRightPanel().Contains(
                               countyGuiPointer) ||
                           CountyPlanningBottomToolbarRect().Contains(
                               countyGuiPointer);
                return CountyOverviewDetailsPanel().Contains(
                    countyGuiPointer);
            }
            if (pointer.x >= Screen.width - BuildingPanelWidth - 16f)
                return true;
            if (!IsPersonViewVisible) return false;
            var objectiveWidth = Mathf.Min(470f,
                Mathf.Max(320f, Screen.width - BuildingPanelWidth - 56f));
            var guiPointer = new Vector2(pointer.x,
                Screen.height - pointer.y);
            return new Rect(16f, TopHudHeight + 12f,
                objectiveWidth, 92f).Contains(guiPointer);
        }

        private static bool ViewHotkeysBlockedByTextInput()
        {
            var selected = EventSystem.current == null
                ? null : EventSystem.current.currentSelectedGameObject;
            return selected != null && selected.GetComponent<InputField>() !=
                null;
        }

        private void InitializeLocalCamera(bool resetView)
        {
            var camera = _map?.PresentationCamera;
            if (camera == null || !IsPersonViewVisible) return;
            if (resetView || !_cameraPoseInitialized)
            {
                camera.orthographic = true;
                camera.orthographicSize = LocalCameraOrthographicSize;
                _cameraYawDegrees = 35f;
                _cameraPitchDegrees = 52f;
                _cameraPoseInitialized = true;
            }
            FocusPlayerNow();
        }

        private void InitializeNearfieldObservationCamera(string facilityId)
        {
            var camera = _map?.PresentationCamera;
            if (camera == null || !_map.TryGetLuoyangFacilityLocalWorldPosition(
                    facilityId, out var focus)) return;
            camera.orthographic = true;
            camera.orthographicSize = LocalCameraOrthographicSize;
            _cameraYawDegrees = 35f;
            _cameraPitchDegrees = 52f;
            _cameraFocus = focus + Vector3.up * 0.10f;
            _cameraPoseInitialized = true;
            ApplyLocalCameraPose();
        }

        private void ApplyLocalCameraPose()
        {
            var camera = _map?.PresentationCamera;
            if (camera == null || IsStrategicMapVisible) return;
            camera.transform.rotation = Quaternion.Euler(
                _cameraPitchDegrees, _cameraYawDegrees, 0f);
            camera.transform.position = _cameraFocus -
                camera.transform.rotation * Vector3.forward *
                Mathf.Max(LocalCameraDistance,
                    camera.orthographicSize * 1.45f);
        }

        private void FocusPlayerNow()
        {
            if (_map == null || !IsPersonViewVisible) return;
            LuoyangClickWalkPedestrianInstance actor;
            try
            {
                actor = _map.GetLuoyangClickWalkPedestrian();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            var camera = _map.PresentationCamera;
            if (camera == null || actor == null) return;
            var actorFocus = actor.transform.position + Vector3.up * 0.09f;
            _cameraFocus = !_map.LuoyangPedestrianIsWalking &&
                           _map.TryGetLuoyangFacilityLocalWorldPosition(
                               _map.LuoyangPedestrianCurrentFacilityId,
                               out var facilityFocus)
                ? Vector3.Lerp(actorFocus,
                    facilityFocus + Vector3.up * 0.09f, 0.42f)
                : actorFocus;
            ApplyLocalCameraPose();
        }

        private void RefreshMovementMessage()
        {
            if (!IsPersonViewVisible) return;
            var movementState = _map.LuoyangPedestrianMovementStateId ??
                string.Empty;
            if (string.Equals(movementState, _lastMovementState,
                    StringComparison.Ordinal)) return;
            _lastMovementState = movementState;
            if (_map.LuoyangPedestrianIsWalking)
            {
                var targetName = FindFacilityName(
                    _map.LuoyangPedestrianTargetFacilityId);
                _message = $"正在前往{targetName}。";
                return;
            }
            if (!string.IsNullOrWhiteSpace(
                    _map.LuoyangPedestrianLastStopReasonId))
            {
                _message = BuildMovementFailureMessage();
                return;
            }
            if (!string.IsNullOrWhiteSpace(
                    _map.LuoyangPedestrianCurrentFacilityId))
            {
                var currentName = FindFacilityName(
                    _map.LuoyangPedestrianCurrentFacilityId);
                _message = $"已到达{currentName}。";
            }
        }

        private string BuildMovementFailureMessage()
        {
            var reason = _map?.LuoyangPedestrianLastStopReasonId;
            return string.IsNullOrWhiteSpace(reason)
                ? "当前没有可通行路线。"
                : $"无法到达：{reason}";
        }

        private string FindFacilityName(string facilityId)
        {
            return FindFacility(facilityId)?.DisplayName ??
                   (string.IsNullOrWhiteSpace(facilityId)
                       ? "目标建筑" : facilityId);
        }

        private FacilityState FindFacility(string facilityId)
        {
            if (_world == null || string.IsNullOrWhiteSpace(facilityId))
                return null;
            return _world.Facilities.FirstOrDefault(item => string.Equals(
                item.Id, facilityId, StringComparison.Ordinal));
        }

        public IReadOnlyList<PlayerActionOption> GetSelectedBuildingActions()
        {
            return ContextActions(SelectedFacility);
        }

        private IReadOnlyList<PlayerActionOption> ContextActions(
            FacilityState facility)
        {
            if (facility == null || _dashboard == null)
                return Array.Empty<PlayerActionOption>();
            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            var definitionId = facility.DefinitionId ?? string.Empty;
            if (definitionId.IndexOf("market", StringComparison.Ordinal) >= 0)
            {
                actionIds.Add(PlayerActionIds.TradeBuy);
                actionIds.Add(PlayerActionIds.TradeSell);
                actionIds.Add(PlayerActionIds.AcceptTask);
                actionIds.Add(PlayerActionIds.WorkTask);
                actionIds.Add(PlayerActionIds.AbandonTask);
                actionIds.Add(PlayerActionIds.LocalReliefHelp);
                actionIds.Add(PlayerActionIds.LocalReliefDecline);
            }
            else if (definitionId.IndexOf("office",
                         StringComparison.Ordinal) >= 0 ||
                     definitionId.IndexOf("court_hall",
                         StringComparison.Ordinal) >= 0)
            {
                actionIds.Add(PlayerActionIds.AcceptTask);
                actionIds.Add(PlayerActionIds.WorkTask);
                actionIds.Add(PlayerActionIds.AbandonTask);
                actionIds.Add(PlayerActionIds.Construction);
                actionIds.Add(PlayerActionIds.LocalReliefHelp);
                actionIds.Add(PlayerActionIds.LocalReliefDecline);
            }
            else if (definitionId.IndexOf("clinic",
                         StringComparison.Ordinal) >= 0)
            {
                actionIds.Add(PlayerActionIds.ClinicCare);
                actionIds.Add(PlayerActionIds.HomeRest);
            }
            else if (definitionId.IndexOf("school",
                         StringComparison.Ordinal) >= 0 ||
                     definitionId.IndexOf("academy",
                         StringComparison.Ordinal) >= 0)
                actionIds.Add(PlayerActionIds.Study);
            else if (definitionId.IndexOf("agriculture",
                         StringComparison.Ordinal) >= 0)
            {
                actionIds.Add(PlayerActionIds.FarmStart);
                actionIds.Add(PlayerActionIds.FarmComplete);
            }
            else if (definitionId.IndexOf("workshop",
                         StringComparison.Ordinal) >= 0)
                actionIds.Add(PlayerActionIds.Construction);
            else if (definitionId.IndexOf("inn",
                         StringComparison.Ordinal) >= 0 ||
                     definitionId.IndexOf("residence",
                         StringComparison.Ordinal) >= 0)
                actionIds.Add(PlayerActionIds.Rest);

            if (actionIds.Count == 0) actionIds.Add(PlayerActionIds.Rest);
            return _dashboard.QueryCurrentPlayerActions().Where(item =>
                    actionIds.Contains(item.Id))
                .ToArray();
        }

        private string CurrentObjectiveFacilityId()
        {
            if (_world == null) return string.Empty;
            if (!_world.TradeRecords.Any(item => string.Equals(item.PersonId,
                    _world.PlayerPersonId, StringComparison.Ordinal)))
                return PlayableLuoyangWorldContractIds.MarketFacilityId;
            var task = _world.Tasks.FirstOrDefault(item => string.Equals(
                item.DefinitionId,
                PlayableLuoyangWorldContractIds.LocalTaskDefinitionId,
                StringComparison.Ordinal));
            return task == null || task.Status == TaskStatus.Active
                ? PlayableLuoyangWorldContractIds.OfficeFacilityId
                : string.Empty;
        }

        private string BuildCurrentObjectiveText()
        {
            if (_world == null) return "正在准备洛阳生活目标……";
            if (!_world.TradeRecords.Any(item => string.Equals(item.PersonId,
                    _world.PlayerPersonId, StringComparison.Ordinal)))
                return "第一步：从住处前往市场，买入或卖出一次布帛。";
            var task = _world.Tasks.FirstOrDefault(item => string.Equals(
                item.DefinitionId,
                PlayableLuoyangWorldContractIds.LocalTaskDefinitionId,
                StringComparison.Ordinal));
            if (task == null)
                return "第二步：在市场市曹柜台接受“核验商籍”差事。";
            if (task.Status == TaskStatus.Active)
                return $"当前差事：核验商籍 {task.Progress}/3；在市场市曹继续投入一天。";
            return task.Status == TaskStatus.Completed
                ? "洛阳生活已起步：完成交易和市曹差事，可自由经营与探索。"
                : "差事已经结束；可继续访问市集、客舍和其他建筑。";
        }

        private static string FacilityTypeLabel(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) return "设施";
            if (definitionId.Contains("market")) return "市集";
            if (definitionId.Contains("office") ||
                definitionId.Contains("court_hall")) return "官署";
            if (definitionId.Contains("road")) return "道路";
            if (definitionId.Contains("gate")) return "城门／宫门";
            if (definitionId.Contains("bridge")) return "桥梁";
            if (definitionId.Contains("residence")) return "住宅";
            if (definitionId.Contains("warehouse") ||
                definitionId.Contains("granary")) return "仓储";
            if (definitionId.Contains("workshop")) return "工坊";
            if (definitionId.Contains("clinic")) return "医馆";
            if (definitionId.Contains("school") ||
                definitionId.Contains("academy")) return "学舍";
            if (definitionId.Contains("agriculture")) return "农业设施";
            if (definitionId.Contains("military")) return "军用设施";
            if (definitionId.Contains("ritual")) return "礼制建筑";
            return "城市设施";
        }

        private static string ConfidenceLabel(
            HistoricalConfidenceLevel confidence)
        {
            return confidence == HistoricalConfidenceLevel.HistoricalAnchor
                ? "史料锚点"
                : confidence ==
                  HistoricalConfidenceLevel.HistoricalReconstruction
                    ? "历史复原"
                    : "玩法重建";
        }

        private static string PrecisionLabel(
            HistoricalSpatialPrecision precision)
        {
            return precision == HistoricalSpatialPrecision.Confirmed
                ? "确认位置"
                : precision == HistoricalSpatialPrecision.Probable
                    ? "推定位置"
                    : "近似位置";
        }

        private PersonState Player()
        {
            return new PlayerSession(_world).ControlledPerson;
        }

        private void OnGUI()
        {
            if (!_active || _world == null) return;
            EnsureStyles();
            var player = Player();
            GUI.Box(new Rect(0f, 0f, Screen.width, TopHudHeight),
                GUIContent.none);
            GUI.Label(new Rect(18f, 10f, 300f, 31f),
                IsStrategicMapVisible
                    ? "天下｜" + (IsStrategicDioramaWorldStyle
                        ? "彩色立体战略沙盘"
                        : IsInkWorldStyle
                            ? "水墨开发对照" : "自然地形对照")
                    : IsCountyViewVisible
                        ? CountyDisplayName(ObservedCountyId) +
                          "｜县域｜" + CountySubViewLabel(CountySubView)
                        : "洛阳｜人物近景",
                _hudTitleStyle);
            var displayYear = 184 + _world.AbsoluteDay / 360;
            var currentFacilityName = FindFacilityName(
                player.CurrentFacilityId);
            GUI.Label(new Rect(20f, 45f, 700f, 25f),
                $"{player.DisplayName}　{displayYear}年 第{_world.AbsoluteDay + 1}日　" +
                $"钱财 {player.Wealth}　口粮 {player.Provisions}　" +
                $"体力 {player.StaminaBasisPoints / 100f:0}%",
                _hudStyle);
            GUI.Label(new Rect(20f, 72f,
                    Mathf.Max(200f, Screen.width - 680f), 23f),
                IsStrategicMapVisible
                    ? "全国正式 Cell、行政边界、河流与道路（战略层）"
                    : IsCountyViewVisible
                        ? "50m 县域空间 · 城区与建设共享 Facility / Road / Water"
                        : $"当前：{currentFacilityName}　" +
                          $"状态：{MovementStatusLabel()}",
                _hudSmallStyle);

            var buttonX = Mathf.Max(320f, Screen.width - 560f);
            if (GUI.Button(new Rect(buttonX, 15f, 70f, 34f), "M 天下"))
                ShowWorldView();
            if (GUI.Button(new Rect(buttonX + 74f, 15f, 70f, 34f),
                    "C 县域")) ShowCountyView();
            if (GUI.Button(new Rect(buttonX + 148f, 15f, 70f, 34f),
                    "F 人物")) ShowPersonView();
            GUI.enabled = IsPersonViewVisible;
            if (GUI.Button(new Rect(buttonX + 222f, 15f, 78f, 34f),
                    "E 前往")) TryMoveToSelectedFacility();
            if (GUI.Button(new Rect(buttonX + 304f, 15f, 86f, 34f),
                    "R 休息一天")) RestOneDay();
            GUI.enabled = true;
            if (GUI.Button(new Rect(buttonX + 394f, 15f, 68f, 34f),
                    "S 保存")) SaveToMemory();
            if (GUI.Button(new Rect(buttonX + 466f, 15f, 76f, 34f),
                    "Esc 菜单")) SetPaused(true);

            if (IsPersonViewVisible)
            {
                DrawObjectiveCard();
                DrawBuildingPanel();
                DrawPlayerWorldMarker();
            }
            else if (IsCountyViewVisible) DrawCountyViewOverlay();
            else
            {
                DrawStrategicAdministrativeOverlay();
            }

            GUI.Box(new Rect(16f, Screen.height - 58f,
                Screen.width - 32f, 42f), GUIContent.none);
            GUI.Label(new Rect(28f, Screen.height - 50f,
                Screen.width - 56f, 28f),
                IsPersonViewVisible
                    ? _message +
                      "　｜　左键选建筑 · 中键平移 · 右键旋转 · 滚轮缩放"
                    : IsCountyPlanningVisible
                        ? _message +
                          "　｜　左键规划 · R旋转 · 右键取消 · 中键平移 · Alt+右键旋转 · 滚轮缩放"
                    : _message +
                      "　｜　中键平移 · 右键旋转 · 滚轮缩放",
                _messageStyle);
            if (_paused) DrawPauseMenu();
        }

        private void DrawStrategicAdministrativeOverlay()
        {
            if (_map == null || !_map.IsReady) return;
            var controls = new Rect(16f, TopHudHeight + 12f, 438f, 224f);
            var details = new Rect(Screen.width - 376f,
                TopHudHeight + 12f, 360f, 202f);
            GUI.Box(controls, GUIContent.none);
            var viewState = _map.AdministrativeMapViewState;
            var planning = viewState != null && viewState.ViewMode ==
                AdministrativeMapViewMode.CountyPlanning;
            var selection = _map.AdministrativeSelection;
            var title = planning && selection != null
                ? selection.DisplayName + "｜县域规划"
                : "州—郡国—县行政图";
            GUI.Label(new Rect(controls.x + 12f, controls.y + 8f,
                    controls.width - 24f, 28f), title, _panelTitleStyle);

            var buttonY = controls.y + 42f;
            if (!planning)
            {
                if (GUI.Button(new Rect(controls.x + 12f, buttonY,
                        76f, 30f), "州界"))
                    _map.SetAdministrativeLabelLevel(
                        AdministrativeMapLabelLevel.Province);
                if (GUI.Button(new Rect(controls.x + 94f, buttonY,
                        76f, 30f), "郡国界"))
                    _map.SetAdministrativeLabelLevel(
                        AdministrativeMapLabelLevel.CommanderyEquivalent);
                if (GUI.Button(new Rect(controls.x + 176f, buttonY,
                        76f, 30f), "县界"))
                    _map.SetAdministrativeLabelLevel(
                        AdministrativeMapLabelLevel.County);
                GUI.enabled = selection != null && selection.Level ==
                    AdministrativeRegionLevel.County;
                if (GUI.Button(new Rect(controls.x + 260f, buttonY,
                        164f, 30f), "进入县域规划"))
                {
                    EnterCountyPlanning(selection.RegionId);
                }
                GUI.enabled = true;
            }
            else if (GUI.Button(new Rect(controls.x + 260f, buttonY,
                         164f, 30f), "退出县域规划"))
            {
                _map.ExitCountyPlanning();
                _message = "已退出县域规划，返回统一世界地图。";
            }
            var priorColor = GUI.color;
            if (!IsStrategicDioramaWorldStyle)
                GUI.color = new Color(1f, 0.76f, 0.28f);
            if (GUI.Button(new Rect(controls.x + 12f,
                    controls.y + 78f, 128f, 28f), "自然地形"))
                SetWorldMapStrategicDioramaStyle(false);
            GUI.color = priorColor;
            if (IsStrategicDioramaWorldStyle)
                GUI.color = new Color(0.96f, 0.62f, 0.16f);
            if (GUI.Button(new Rect(controls.x + 146f,
                    controls.y + 78f, 128f, 28f), "战略沙盘"))
                SetWorldMapStrategicDioramaStyle(true);
            GUI.color = priorColor;
            if (DrawOverlayToggleButton(new Rect(controls.x + 280f,
                        controls.y + 78f, 144f, 28f), "交通详图",
                    _map.TransportOverlayVisible))
                _map.SetTransportOverlayVisible(
                    !_map.TransportOverlayVisible);
            var overlayY = controls.y + 112f;
            if (DrawOverlayToggleButton(new Rect(controls.x + 12f,
                        overlayY, 92f, 26f), "行政",
                    _map.AdministrativeOverlayVisible))
                _map.SetAdministrativeOverlayVisible(
                    !_map.AdministrativeOverlayVisible);
            if (DrawOverlayToggleButton(new Rect(controls.x + 110f,
                        overlayY, 92f, 26f), "道路",
                    _map.RoadOverlayVisible))
                _map.SetRoadOverlayVisible(!_map.RoadOverlayVisible);
            if (DrawOverlayToggleButton(new Rect(controls.x + 208f,
                        overlayY, 92f, 26f), "河流",
                    _map.RiverOverlayVisible))
                _map.SetRiverOverlayVisible(!_map.RiverOverlayVisible);
            if (DrawOverlayToggleButton(new Rect(controls.x + 306f,
                        overlayY, 118f, 26f), "战略格网",
                    _map.CellOverlayVisible))
                _map.SetCellOverlayVisible(!_map.CellOverlayVisible);

            GUI.Label(new Rect(controls.x + 12f, controls.y + 144f,
                    controls.width - 24f, 34f),
                "图例　行政：淡褐示意线　道路：赭橙官路\n" +
                "　　　河流：蓝色水系　格网：2公里战略格；交通详图显示全部路线",
                _panelHintStyle);
            GUI.Label(new Rect(controls.x + 12f, controls.y + 184f,
                    controls.width - 24f, 26f),
                "左键选区 · 滚轮缩放 · 中键平移 · 右键旋转",
                _panelHintStyle);

            DrawStrategicSettlementMarkers(controls, details);
            DrawStrategicAdministrativeLabels(controls, details);
            DrawStrategicAdministrativeDetails(details);
            HandleStrategicAdministrativeSelection(controls, details);
        }

        private void DrawStrategicSettlementMarkers(Rect controls,
            Rect details)
        {
            if (_map == null) return;
            if (IsStrategicDioramaWorldStyle) return;
            var showNames = _map.AdministrativeMapViewState.LabelLevel !=
                AdministrativeMapLabelLevel.Province;
            foreach (var marker in _map.GetVisibleSettlementMarkers())
            {
                if (!_map.TryGetSettlementMarkerViewport(marker,
                        out var viewport)) continue;
                var center = new Vector2(viewport.x * Screen.width,
                    Screen.height - viewport.y * Screen.height);
                var seal = new Rect(center.x - 9f, center.y - 9f, 18f, 18f);
                if (controls.Overlaps(seal) || details.Overlaps(seal))
                    continue;
                var previous = GUI.color;
                GUI.color = IsInkWorldStyle
                    ? new Color(0.62f, 0.15f, 0.10f, 0.96f)
                    : new Color(0.93f, 0.64f, 0.18f, 0.96f);
                GUI.DrawTexture(seal, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(seal, new GUIContent("邑",
                    marker.LocationId + "\n" + marker.RegionId),
                    _playerMarkerStyle);
                GUI.color = previous;
                if (!showNames) continue;
                GUI.Label(new Rect(seal.xMax + 3f, seal.y - 2f,
                        92f, 22f), marker.DisplayName,
                    _panelHintStyle);
            }
        }

        private void DrawCountyViewOverlay()
        {
            var navigation = CountySubViewNavigationRect();
            GUI.Box(navigation, GUIContent.none);
            GUI.Label(new Rect(navigation.x + 12f, navigation.y + 8f,
                174f, 28f), "县域子视图", _panelTitleStyle);
            var buttonX = navigation.x + 188f;
            DrawCountySubViewButton(new Rect(buttonX, navigation.y + 7f,
                    100f, 32f), CountySubViewMode.Overview, "县域总览");
            DrawCountySubViewButton(new Rect(buttonX + 106f,
                    navigation.y + 7f, 100f, 32f),
                CountySubViewMode.UrbanArea,
                string.Equals(ObservedCountyId,
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    StringComparison.Ordinal)
                    ? "洛阳城区" : "主要城区");
            DrawCountySubViewButton(new Rect(buttonX + 212f,
                    navigation.y + 7f, 100f, 32f),
                CountySubViewMode.Planning, "建设规划");
            if (GUI.Button(new Rect(buttonX + 318f,
                    navigation.y + 7f, 100f, 32f), "样板街区"))
            {
                if (ShowCountySubView(CountySubViewMode.UrbanArea) &&
                    _countyPlanning.FocusGoldenBlockPrototype())
                {
                    _message = "已聚焦洛阳黄金街区：五类模块化院落、巷道与环境肌理均为只读表现。";
                }
            }
            var debug = _countyPlanning?.WorldSpacePresentation?.DebugVisible ??
                        false;
            if (GUI.Button(new Rect(navigation.xMax - 102f,
                    navigation.y + 7f, 90f, 32f),
                    debug ? "关闭调试" : "开发调试"))
            {
                _countyPlanning.SetWorldSpaceDebugVisible(!debug);
                _message = debug
                    ? "已关闭全部县域调试几何。"
                    : "已开启县域调试几何；普通玩家默认关闭。";
            }
            GUI.Label(new Rect(buttonX + 424f, navigation.y + 10f,
                    Mathf.Max(120f, navigation.xMax - 112f -
                                      (buttonX + 424f)), 26f),
                "同一 50m 布局 · " + CountyLodLabel(
                    _countyPlanning?.PresentationLod ??
                    CountyMapPresentationLod.Far), _panelHintStyle);

            if (!IsCountyDetailPresentationVisible)
            {
                var rect = new Rect(20f, navigation.yMax + 12f,
                    Screen.width - 40f,
                    Screen.height - navigation.yMax - BottomHudHeight - 28f);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(new Rect(rect.x + 18f, rect.y + 16f,
                    rect.width - 36f, 80f),
                    "该县可通过正式行政边界观察；50m 县域详细包尚未安装。",
                    _panelBodyStyle);
                return;
            }
            if (IsCountyPlanningVisible)
            {
                DrawCountyPlanningTools();
                return;
            }

            var map = CountyViewMapRect();
            _countyPlanning.DrawMap(map);
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 && map.Contains(
                    Event.current.mousePosition) &&
                _countyPlanning.SelectCellFromMap(map,
                    Event.current.mousePosition))
            {
                var selected = _countyPlanning.SelectedObservedFacility;
                _message = selected == null
                    ? "已选择县域地表 Cell；附近没有正式 Facility。"
                    : "已选择建筑：" + selected.DisplayName + "。";
                Event.current.Use();
            }
            var details = CountyOverviewDetailsPanel();
            GUI.Box(details, GUIContent.none);
            GUI.Label(new Rect(details.x + 14f, details.y + 12f,
                details.width - 28f, 28f),
                CountySubView == CountySubViewMode.UrbanArea
                    ? "主要城区聚焦" : "县域空间总览",
                _panelTitleStyle);
            var body = CountySubView == CountySubViewMode.UrbanArea
                ? "世界空间斜俯视聚焦洛阳建成区；建筑、城墙、城门、道路与水渠仍读取同一" +
                  " 50m 权威布局。普通建筑按住宅、市场、工坊、仓廪、官署五族合批表现，" +
                  "具名地标继续使用正式模型；城区候选凸包只在开发调试中显示。"
                : "世界空间县域沙盘：分块地形、河流水面、道路路带、墙门、农田、村落与" +
                  "建筑群共同表达 320×640 的 50m 空间；远景建筑轮廓同样区分五类用途，" +
                  "默认不显示黄线、黄点云或全县格网。";
            GUI.Label(new Rect(details.x + 14f, details.y + 50f,
                details.width - 28f, 132f), body, _panelBodyStyle);
            GUI.Label(new Rect(details.x + 14f, details.y + 190f,
                    details.width - 28f, 96f),
                $"CountyId\n{ObservedCountyId}\n\nFacility：" +
                _countyPlanning.FacilityCount +
                "\n当前可见：" +
                (_countyPlanning.PresentationSnapshot?.VisibleFacilities ??
                 0) + "\n道路段：" +
                (_countyPlanning.PresentationSnapshot?.VisibleRoadSegments ??
                 0) + "\nUrbanArea：" + _countyPlanning.UrbanAreaId +
                SelectedCountyFacilityDetails(),
                _panelHintStyle);
        }

        private string SelectedCountyFacilityDetails()
        {
            var facility = _countyPlanning?.SelectedObservedFacility;
            if (facility == null) return string.Empty;
            return "\n\n选中：" + facility.DisplayName +
                   "\n类型：" + FacilityTypeLabel(facility.DefinitionId) +
                   "\n占地：" +
                   (facility.WidthCentimetres / 100f).ToString("0.#") +
                   "×" + (facility.DepthCentimetres / 100f).ToString("0.#") +
                   "m\n位置口径：" + facility.SourceSpatialPrecisionId;
        }

        private void DrawCountySubViewButton(Rect rect,
            CountySubViewMode mode, string label)
        {
            var previous = GUI.color;
            if (CountySubView == mode)
                GUI.color = new Color(1f, 0.75f, 0.26f);
            if (GUI.Button(rect, label)) ShowCountySubView(mode);
            GUI.color = previous;
        }

        private static bool DrawOverlayToggleButton(Rect rect, string label,
            bool enabled)
        {
            var previous = GUI.color;
            GUI.color = enabled
                ? new Color(0.96f, 0.72f, 0.28f)
                : new Color(0.58f, 0.58f, 0.52f);
            var clicked = GUI.Button(rect, label + (enabled ? " ✓" : ""));
            GUI.color = previous;
            return clicked;
        }

        public bool EnterCountyPlanning(string countyId)
        {
            if (!_active || !TryEnsureMapRuntimeReady(
                    "County Planning") ||
                string.IsNullOrWhiteSpace(countyId)) return false;
            if (IsCountyViewVisible && string.Equals(ObservedCountyId,
                    countyId, StringComparison.Ordinal))
                return ShowCountySubView(CountySubViewMode.Planning);
            EndCountyPlanningPresentation();
            _viewState.ShowCounty(countyId, CountySubViewMode.Planning);
            _followPlayer = false;
            _map.HidePlayableLuoyangPresentation();
            _map.SetCellOverlayVisible(false);
            if (!_map.EnterCountyPlanning(countyId))
            {
                _message = "该县当前不能进入县域规划。";
                return false;
            }
            _map.SetAdministrativeOverlayVisible(false);
            _map.SetStrategicMapPresentationVisible(false);
            if (!EnsureCountyDetailPresentation(countyId,
                    CountySubViewMode.Planning))
            {
                _map.ExitCountyPlanning();
                _map.SetStrategicMapPresentationVisible(true);
                _map.SetAdministrativeOverlayVisible(true);
                _message = string.IsNullOrWhiteSpace(
                    _countyPlanning.LastError)
                    ? "县域规划工具初始化失败。"
                    : "县域规划工具初始化失败，详情见 Console。";
                return false;
            }
            _message = "已进入洛阳｜县域规划；当前只生成草案，人物、时间、" +
                       "钱粮、设施和道路均未改变。";
            return true;
        }

        public bool EnterLuoyangCountyPlanningForTests() =>
            EnterCountyPlanning(
                Luoyang50mCountySpatialPrototypeIds.CountyId);

        public bool CreatePlanningDraft()
        {
            if (!IsCountyPlanningVisible) return false;
            var draft = _countyPlanning.CreateDraft();
            if (draft == null)
            {
                _message = _countyPlanning.Validation?.PrimaryReason ??
                           "当前位置不能生成草案。";
                return false;
            }
            _message = "已创建第 " + _countyPlanning.Drafts.Count +
                       " 个建设草案；尚未形成正式 Facility。";
            return true;
        }

        public bool UndoPlanningDraft()
        {
            if (!IsCountyPlanningVisible ||
                _countyPlanning.Undo() == null) return false;
            _message = "已撤销上一个建设草案；正式世界没有变化。";
            return true;
        }

        public bool RedoPlanningDraft()
        {
            if (!IsCountyPlanningVisible ||
                _countyPlanning.Redo() == null) return false;
            _message = "已重做建设草案；仍未进入正式建设结算。";
            return true;
        }

        private void EndCountyPlanningPresentation()
        {
            if (_countyPlanning != null) _countyPlanning.End();
            var state = _map?.AdministrativeMapViewState;
            if (state != null && state.ViewMode ==
                    AdministrativeMapViewMode.CountyPlanning)
                _map.ExitCountyPlanning();
        }

        private static Rect CountyPlanningLeftPanel()
        {
            var right = CountyPlanningRightPanel();
            return new Rect(16f, TopHudHeight + 64f,
                Mathf.Max(240f, right.x - 28f), 56f);
        }

        private static Rect CountyPlanningRightPanel() =>
            new Rect(Screen.width - 316f, TopHudHeight + 64f, 300f,
                Mathf.Max(260f, CountyPlanningBottomToolbarRect().y -
                    TopHudHeight - 76f));

        private static Rect CountyPlanningBottomToolbarRect()
        {
            var width = Mathf.Max(240f, Screen.width - 344f);
            return new Rect(16f,
                Mathf.Max(420f, Screen.height - BottomHudHeight - 146f),
                width, 134f);
        }

        private static Rect CountySubViewNavigationRect() =>
            new Rect(16f, TopHudHeight + 12f, Screen.width - 32f, 44f);

        private static Rect CountyOverviewDetailsPanel() =>
            new Rect(Screen.width - 356f, TopHudHeight + 64f, 340f,
                Mathf.Max(320f, Screen.height - TopHudHeight -
                    BottomHudHeight - 76f));

        private Rect CountyViewMapRect()
        {
            if (IsCountyPlanningVisible) return CountyPlanningMapRect();
            if (Screen.width > 0 && Screen.height > 0)
            {
                var details = CountyOverviewDetailsPanel();
                return new Rect(16f, TopHudHeight + 64f,
                    Mathf.Max(240f, details.x - 28f),
                    Mathf.Max(240f, Screen.height - TopHudHeight -
                        BottomHudHeight - 76f));
            }
            return new Rect(328f, TopHudHeight + 64f, 584f, 292f);
        }

        private static Rect CountyPlanningMapRect()
        {
            var legend = CountyPlanningLeftPanel();
            var right = CountyPlanningRightPanel();
            var toolbar = CountyPlanningBottomToolbarRect();
            var y = legend.yMax + 8f;
            return new Rect(16f, y, Mathf.Max(240f, right.x - 28f),
                Mathf.Max(180f, toolbar.y - y - 8f));
        }

        private void DrawCountyPlanningTools()
        {
            var legend = CountyPlanningLeftPanel();
            var right = CountyPlanningRightPanel();
            var map = CountyPlanningMapRect();
            var toolbar = CountyPlanningBottomToolbarRect();
            GUI.Box(legend, GUIContent.none);
            GUI.Box(right, GUIContent.none);
            DrawPlanningLegendAndOverlayControls(legend);

            _countyPlanning.DrawMap(map);
            DrawPlanningBottomToolbar(toolbar);
            DrawCountyPlanningDetails(right);
            var current = Event.current;
            var mapMessage = _countyPlanning.HandleMapGuiEvent(map, current);
            if (!string.IsNullOrWhiteSpace(mapMessage)) _message = mapMessage;
        }

        private void DrawPlanningLegendAndOverlayControls(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 10f, rect.y + 5f, 82f, 24f),
                "地图图例", _panelTitleStyle);
            var overlays = _countyPlanning.MapOverlays;
            var x = rect.x + 92f;
            if (DrawOverlayToggleButton(new Rect(x, rect.y + 5f, 74f, 24f),
                    "行政", overlays.AdministrativeVisible))
                _countyPlanning.SetOverlayVisible("administrative",
                    !overlays.AdministrativeVisible);
            if (DrawOverlayToggleButton(new Rect(x + 78f, rect.y + 5f,
                        74f, 24f), "道路", overlays.RoadsVisible))
                _countyPlanning.SetOverlayVisible("roads",
                    !overlays.RoadsVisible);
            if (DrawOverlayToggleButton(new Rect(x + 156f, rect.y + 5f,
                        74f, 24f), "河流", overlays.RiversVisible))
                _countyPlanning.SetOverlayVisible("rivers",
                    !overlays.RiversVisible);
            if (DrawOverlayToggleButton(new Rect(x + 234f, rect.y + 5f,
                        74f, 24f), "格网", overlays.GridVisible))
                _countyPlanning.SetOverlayVisible("grid",
                    !overlays.GridVisible);
            if (DrawOverlayToggleButton(new Rect(x + 312f, rect.y + 5f,
                        74f, 24f), "城防",
                    overlays.FortificationsVisible))
                _countyPlanning.SetOverlayVisible("fortifications",
                    !overlays.FortificationsVisible);
            if (DrawOverlayToggleButton(new Rect(x + 390f, rect.y + 5f,
                        74f, 24f), "规划",
                    overlays.PlanningVisible))
                _countyPlanning.SetOverlayVisible("planning",
                    !overlays.PlanningVisible);
            if (DrawOverlayToggleButton(new Rect(x + 468f, rect.y + 5f,
                        88f, 24f), "地形分析",
                    overlays.TerrainAnalysisVisible))
                _countyPlanning.SetOverlayVisible("terrain",
                    !overlays.TerrainAnalysisVisible);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 31f,
                    rect.width - 20f, 20f),
                "淡褐=行政　土褐=既有道路　蓝=河渠　深褐=城防　细格=近景50米规划格　青=草案",
                _panelHintStyle);
        }

        private void DrawPlanningBottomToolbar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none);
            var categories = new[]
            {
                new[] { "road", "道路" }, new[] { "building", "建筑" },
                new[] { "zone", "区域" }, new[] { "defense", "城防" },
                new[] { "water", "水利" }, new[] { "tools", "工具" }
            };
            var categoryWidth = Mathf.Min(92f,
                (rect.width - 24f) / categories.Length);
            for (var index = 0; index < categories.Length; index++)
            {
                var previous = GUI.color;
                if (_planningToolbarCategory == categories[index][0])
                    GUI.color = new Color(1f, 0.74f, 0.25f);
                if (GUI.Button(new Rect(rect.x + 10f +
                            index * categoryWidth, rect.y + 7f,
                        categoryWidth - 4f, 27f), categories[index][1]))
                    _planningToolbarCategory = categories[index][0];
                GUI.color = previous;
            }
            GUI.Label(new Rect(rect.xMax - 310f, rect.y + 10f, 298f, 22f),
                "当前工具：" + PlanningToolLabel(
                    _countyPlanning.ToolState.PrimaryTool) +
                "　规划阶段", _panelHintStyle);

            var y = rect.y + 40f;
            if (_planningToolbarCategory == "building")
                DrawPlanningBuildingCards(rect, y);
            else if (_planningToolbarCategory == "road")
                DrawPlanningToolButton(new Rect(rect.x + 12f, y, 150f, 42f),
                    "官路蓝图\n拖拽铺设", CountyPlanningPrimaryTool.Road);
            else if (_planningToolbarCategory == "defense")
                DrawPlanningToolButton(new Rect(rect.x + 12f, y, 150f, 42f),
                    "城墙蓝图\n沿Cell边拖拽",
                    CountyPlanningPrimaryTool.Wall);
            else if (_planningToolbarCategory == "water")
                DrawPlanningToolButton(new Rect(rect.x + 12f, y, 150f, 42f),
                    "水渠蓝图\n读取坡度与水源",
                    CountyPlanningPrimaryTool.Canal);
            else if (_planningToolbarCategory == "zone")
                DrawPlanningZoneButtons(rect, y);
            else DrawPlanningUtilityButtons(rect, y);

            GUI.Label(new Rect(rect.x + 12f, rect.yMax - 31f,
                    rect.width - 24f, 22f),
                "左键放置/拖拽　R旋转建筑　右键取消　中键平移　Alt+右键旋转镜头　滚轮缩放",
                _panelHintStyle);
        }

        private void DrawPlanningBuildingCards(Rect rect, float y)
        {
            var profiles = _countyPlanning.PlayerFacingBuildingProfiles;
            var width = Mathf.Min(172f,
                (rect.width - 24f) / Mathf.Max(1, profiles.Count));
            for (var index = 0; index < profiles.Count; index++)
            {
                var profile = profiles[index];
                var selected = _countyPlanning.ToolState.PrimaryTool ==
                               CountyPlanningPrimaryTool.Building &&
                               ReferenceEquals(profile,
                                   _countyPlanning.SelectedProfile);
                var previous = GUI.color;
                if (selected) GUI.color = new Color(0.98f, 0.76f, 0.29f);
                var label = profile.DisplayName + "｜" +
                    PlacementCategoryLabel(profile.PlacementCategoryId) +
                    "\n" + profile.FootprintWidthCentimetres / 100f +
                    "×" + profile.FootprintLengthCentimetres / 100f +
                    "m · " + PlacementPurpose(profile.PlacementCategoryId) +
                    " · " + (profile.RoadAccessRequirement ==
                              FacilityRoadAccessRequirement.Required
                        ? "需道路" : "道路可选");
                if (GUI.Button(new Rect(rect.x + 12f + index * width, y,
                        width - 5f, 48f), label))
                {
                    _countyPlanning.SelectProfile(profile.ProfileId);
                    _message = "已选择" + profile.DisplayName +
                               "；建筑Ghost将跟随鼠标，左键可连续放置。";
                }
                GUI.color = previous;
            }
        }

        private void DrawPlanningZoneButtons(Rect rect, float y)
        {
            var values = new[]
            {
                CountyPlanningZoneKind.Residential,
                CountyPlanningZoneKind.Production,
                CountyPlanningZoneKind.Storage,
                CountyPlanningZoneKind.Agriculture
            };
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (GUI.Button(new Rect(rect.x + 12f + index * 134f, y,
                        128f, 42f), ZoneLabel(value) + "\n拖拽涂刷"))
                {
                    _countyPlanning.ActivateZoneTool(value);
                    _message = "区域只是规划草案，不改变正式LandUse。";
                }
            }
        }

        private void DrawPlanningUtilityButtons(Rect rect, float y)
        {
            var tools = new[]
            {
                CountyPlanningPrimaryTool.Select,
                CountyPlanningPrimaryTool.MoveDraft,
                CountyPlanningPrimaryTool.CopyDraft,
                CountyPlanningPrimaryTool.Eyedropper,
                CountyPlanningPrimaryTool.DemolishDraft
            };
            for (var index = 0; index < tools.Length; index++)
                DrawPlanningToolButton(new Rect(rect.x + 12f + index * 112f,
                    y, 106f, 38f), PlanningToolLabel(tools[index]),
                    tools[index]);
            GUI.enabled = _countyPlanning.SelectedDraftIds.Count > 0;
            if (GUI.Button(new Rect(rect.x + 576f, y, 112f, 38f),
                    "删除所选") && _countyPlanning.DeleteSelectedDrafts())
                _message = "已批量删除所选草案；正式设施未改变。";
            GUI.enabled = _countyPlanning.Session.UndoCount > 0;
            if (GUI.Button(new Rect(rect.x + 694f, y, 80f, 38f),
                    "Ctrl+Z\n撤销")) UndoPlanningDraft();
            GUI.enabled = _countyPlanning.Session.RedoCount > 0;
            if (GUI.Button(new Rect(rect.x + 780f, y, 80f, 38f),
                    "Ctrl+Y\n重做")) RedoPlanningDraft();
            GUI.enabled = true;
        }

        private void DrawPlanningToolButton(Rect rect, string label,
            CountyPlanningPrimaryTool tool)
        {
            var previous = GUI.color;
            if (_countyPlanning.ToolState.PrimaryTool == tool)
                GUI.color = new Color(0.98f, 0.76f, 0.29f);
            if (GUI.Button(rect, label))
            {
                _countyPlanning.ActivateTool(tool);
                _message = "已切换" + PlanningToolLabel(tool) + "。";
            }
            GUI.color = previous;
        }

        private static string PlanningToolLabel(CountyPlanningPrimaryTool tool)
        {
            switch (tool)
            {
                case CountyPlanningPrimaryTool.Building: return "建筑";
                case CountyPlanningPrimaryTool.Road: return "道路";
                case CountyPlanningPrimaryTool.Wall: return "城墙";
                case CountyPlanningPrimaryTool.Canal: return "水渠";
                case CountyPlanningPrimaryTool.Zone: return "区域涂刷";
                case CountyPlanningPrimaryTool.Select: return "框选";
                case CountyPlanningPrimaryTool.MoveDraft: return "移动草案";
                case CountyPlanningPrimaryTool.CopyDraft: return "复制草案";
                case CountyPlanningPrimaryTool.Eyedropper: return "吸管";
                case CountyPlanningPrimaryTool.DemolishDraft: return "删除草案";
                default: return "观察";
            }
        }

        private static string ZoneLabel(CountyPlanningZoneKind kind)
        {
            switch (kind)
            {
                case CountyPlanningZoneKind.Production: return "生产规划区";
                case CountyPlanningZoneKind.Storage: return "仓储规划区";
                case CountyPlanningZoneKind.Agriculture: return "农业规划区";
                default: return "住宅规划区";
            }
        }

        private static string PlacementCategoryLabel(string categoryId) =>
            categoryId.Contains("residential") ? "住宅" :
            categoryId.Contains("storage") ? "仓储" :
            categoryId.Contains("industry") ? "生产" :
            categoryId.Contains("commercial") ? "商业" :
            categoryId.Contains("military") ? "军事" : "公共";

        private static string PlacementPurpose(string categoryId) =>
            categoryId.Contains("residential") ? "居住" :
            categoryId.Contains("storage") ? "存放物资" :
            categoryId.Contains("industry") ? "生产加工" :
            categoryId.Contains("commercial") ? "市场交易" :
            categoryId.Contains("military") ? "警戒防务" : "公共服务";

        private void DrawPlanningFixtureButton(Rect panel, ref float y,
            CountyPlanningFixture fixture, string label)
        {
            if (y > panel.yMax - 76f) return;
            if (GUI.Button(new Rect(panel.x + 12f, y,
                    panel.width - 24f, 25f), label))
            {
                _countyPlanning.SelectFixture(fixture);
                _message = "已切换验收样例：" + label + "。";
            }
            y += 27f;
        }

        private void DrawCountyPlanningDetails(Rect panel)
        {
            var validation = _countyPlanning.Validation;
            var cell = _countyPlanning.CellInspection;
            var observed = _countyPlanning.SelectedObservedFacility;
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f,
                panel.width - 24f, 28f), "落位检查",
                _panelTitleStyle);
            var state = validation.State == PlacementValidationState.Valid
                ? "可生成草案"
                : validation.State == PlacementValidationState.Conditional
                    ? "条件性草案"
                    : "不可落位";
            var road = RoadAccessLabel(validation.RoadAccessResult.Status);
            var details = _countyPlanning.SelectedProfile.DisplayName +
                "　朝向 " + (_countyPlanning.RotationQuarterTurns * 90) +
                "°\n状态：" + state + "　道路：" + road +
                "\n实体占地：" +
                _countyPlanning.CurrentFootprint.WidthMetres.ToString("0.#") +
                "m × " + _countyPlanning.CurrentFootprint.LengthMetres
                    .ToString("0.#") + "m" +
                "　覆盖 " + validation.CoveredCells.Count + " 格\n" +
                (cell == null
                    ? "中心 Cell：县域外"
                    : "中心 Cell：(" + cell.LocalRow + ", " +
                      cell.LocalColumn + ")　高程 " +
                      (cell.ElevationDecimetres / 10f).ToString("0.0") +
                      "m\n地形：" + cell.Terrain + "　坡度：" +
                      cell.SlopeBasis + "　用地：" + cell.LandUse +
                      "\n水体：" + (cell.WaterState > 0 ? "有" : "无") +
                      "　四向通行：" + string.Join(" / ",
                          cell.FourPorts.Select(PortLabel))) +
                (observed == null
                    ? "\n现有对象：无（当前仅检查地表与草案）"
                    : "\n现有对象：" + observed.DisplayName + "｜" +
                      PlacementCategoryLabel(observed.CategoryId) +
                      "\n正式定义：" + observed.DefinitionId +
                      "\n占地：" +
                      (observed.WidthCentimetres / 100f).ToString("0.#") +
                      "×" +
                      (observed.DepthCentimetres / 100f).ToString("0.#") +
                      "m（只读）") +
                "\n全部草案：" + _countyPlanning.Session.AllDrafts.Count +
                "（不进入存档和正式结算）";
            GUI.Label(new Rect(panel.x + 14f, panel.y + 42f,
                panel.width - 28f, 205f), details, _panelBodyStyle);

            GUI.Label(new Rect(panel.x + 12f, panel.y + 254f,
                panel.width - 24f, 26f), validation.State ==
                PlacementValidationState.Invalid ? "阻挡原因" : "提示",
                _panelTitleStyle);
            var issues = validation.BlockingReasons.Count > 0
                ? validation.BlockingReasons
                : validation.Warnings;
            var issueText = issues.Count == 0 ? "无" : string.Join("\n",
                issues.Take(6).Select(value => "• " + value.Message));
            var showPerformance = _countyPlanning.Performance != null &&
                                  panel.height > 510f;
            var issueHeight = showPerformance
                ? Mathf.Max(54f, panel.height - 442f)
                : Mathf.Max(54f, panel.height - 326f);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 284f,
                panel.width - 28f, issueHeight), issueText, _panelBodyStyle);

            var performance = _countyPlanning.Performance;
            if (showPerformance)
                GUI.Label(new Rect(panel.x + 14f, panel.yMax - 104f,
                    panel.width - 28f, 62f),
                    "运行指标（64样本）\n选格 P50/P95：" +
                    performance.CellPickP50Milliseconds.ToString("0.000") +
                    "/" + performance.CellPickP95Milliseconds
                        .ToString("0.000") + " ms\n校验 P50/P95：" +
                    performance.ValidatorP50Milliseconds.ToString("0.000") +
                    "/" + performance.ValidatorP95Milliseconds
                        .ToString("0.000") + " ms",
                    _panelHintStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.yMax - 36f,
                panel.width - 28f, 24f),
                "规划层不扣钱粮、不推进日期、不新增 Facility。",
                _panelHintStyle);
        }

        private static string RoadAccessLabel(
            FacilityRoadAccessStatus status)
        {
            switch (status)
            {
                case FacilityRoadAccessStatus.Connected: return "已接通";
                case FacilityRoadAccessStatus.TooFar: return "距离过远";
                case FacilityRoadAccessStatus.Blocked: return "路径受阻";
                case FacilityRoadAccessStatus.WrongSide: return "入口背向";
                case FacilityRoadAccessStatus.NoRoad: return "附近无路";
                default: return "不要求";
            }
        }

        private static string PortLabel(PlanningCellConnectionKind kind)
        {
            switch (kind)
            {
                case PlanningCellConnectionKind.OpenByRoad: return "路";
                case PlanningCellConnectionKind.OpenByBridge: return "桥";
                case PlanningCellConnectionKind.OpenByGate: return "门";
                case PlanningCellConnectionKind.BlockedByWall: return "墙";
                case PlanningCellConnectionKind.BlockedByWater: return "水";
                case PlanningCellConnectionKind.OutsidePartition: return "界";
                default: return kind == PlanningCellConnectionKind.Open
                    ? "通" : "阻";
            }
        }

        private void DrawStrategicAdministrativeLabels(Rect controls,
            Rect details)
        {
            var drawn = 0;
            var occupied = new List<Rect>();
            foreach (var label in _map.GetVisibleAdministrativeLabels())
            {
                if (drawn >= 80 ||
                    !_map.TryGetAdministrativeLabelViewport(label,
                        out var viewport)) continue;
                var width = label.Level == AdministrativeRegionLevel.Province
                    ? 84f : 68f;
                var rect = new Rect(
                    viewport.x * Screen.width - width * 0.5f,
                    (1f - viewport.y) * Screen.height - 12f,
                    width, 24f);
                if (rect.Overlaps(controls) || rect.Overlaps(details) ||
                    rect.y < TopHudHeight ||
                    rect.yMax > Screen.height - BottomHudHeight) continue;
                var overlap = false;
                foreach (var existing in occupied)
                    if (existing.Overlaps(rect))
                    {
                        overlap = true;
                        break;
                    }
                if (overlap && !label.Selected) continue;
                var previousColor = GUI.color;
                GUI.color = label.Selected
                    ? new Color(1f, 0.68f, 0.18f, 0.98f)
                    : label.Level == AdministrativeRegionLevel.Province
                        ? new Color(0.62f, 0.30f, 0.20f, 0.94f)
                        : new Color(0.24f, 0.22f, 0.17f, 0.88f);
                GUI.Box(rect, GUIContent.none);
                GUI.color = previousColor;
                GUI.Label(rect, label.DisplayName, _playerMarkerStyle);
                occupied.Add(rect);
                drawn++;
            }
        }

        private void DrawStrategicAdministrativeDetails(Rect rect)
        {
            GUI.Box(rect, GUIContent.none);
            var selected = _map.AdministrativeSelection;
            GUI.Label(new Rect(rect.x + 12f, rect.y + 9f,
                    rect.width - 24f, 28f), "行政区信息", _panelTitleStyle);
            if (selected == null)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 48f,
                        rect.width - 28f, rect.height - 60f),
                    "点击有效地图 Cell 读取行政区。放大到县级后可进入县域规划。",
                    _panelBodyStyle);
                return;
            }
            var hierarchy = selected.Level == AdministrativeRegionLevel.County
                ? selected.ParentProvinceName + " / " +
                  selected.ParentCommanderyName + " / " +
                  selected.DisplayName
                : selected.DisplayName;
            var settlements = selected.PublicMajorSettlements.Count == 0
                ? "无公开聚落标签"
                : string.Join("、", selected.PublicMajorSettlements);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 43f,
                    rect.width - 28f, rect.height - 52f),
                hierarchy + "\n" +
                "等级：" + AdministrativeLevelLabel(selected.Level) +
                "　类型：" + selected.RegionType +
                "　Cell：" + selected.CellCount + "\n" +
                "边界：" + selected.GeometryStatus +
                "　暂定：" + (selected.Provisional ? "是" : "否") + "\n" +
                "公开道路格：" + selected.PublicRoadCellCount +
                "　主要聚落：" + settlements + "\n" +
                selected.ActualControllerSummary,
                _panelBodyStyle);
        }

        private void HandleStrategicAdministrativeSelection(Rect controls,
            Rect details)
        {
            var current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 ||
                controls.Contains(current.mousePosition) ||
                details.Contains(current.mousePosition) ||
                current.mousePosition.y < TopHudHeight ||
                current.mousePosition.y > Screen.height - BottomHudHeight)
                return;
            var viewport = new Vector2(
                current.mousePosition.x / Screen.width,
                1f - current.mousePosition.y / Screen.height);
            if (_map.TrySelectAdministrativeRegion(viewport))
            {
                var selected = _map.AdministrativeSelection;
                _message = selected == null
                    ? "已读取行政区。"
                    : "已选择：" + selected.DisplayName + "（" +
                      AdministrativeLevelLabel(selected.Level) + "）";
            }
            else
            {
                _message = "该位置不属于当前行政边界数据覆盖范围。";
            }
            current.Use();
        }

        private static string AdministrativeLevelLabel(
            AdministrativeRegionLevel level)
        {
            switch (level)
            {
                case AdministrativeRegionLevel.Province: return "州";
                case AdministrativeRegionLevel.CommanderyEquivalent:
                    return "郡国等价区";
                case AdministrativeRegionLevel.County: return "县";
                default: return level.ToString();
            }
        }

        private void DrawCityLandmarkLabels()
        {
            var projection = _map?.LuoyangCityViewProjection;
            var camera = _map?.PresentationCamera;
            if (projection == null || camera == null) return;
            var overview = camera.orthographicSize >= 42f;
            var occupied = new List<Rect>();
            var landmarks = projection.Facilities.Where(item =>
                         string.Equals(item.DisplayName, "南宫",
                             StringComparison.Ordinal) ||
                         string.Equals(item.DisplayName, "北宫",
                             StringComparison.Ordinal) ||
                         (!overview && string.Equals(item.DisplayName, "太仓",
                             StringComparison.Ordinal) ||
                         !overview && string.Equals(item.FacilityId,
                             "facility.instance.luoyang.184.arsenal",
                             StringComparison.Ordinal) ||
                         string.Equals(item.FacilityId,
                             PlayableLuoyangWorldContractIds.MarketFacilityId,
                             StringComparison.Ordinal) ||
                         !overview && string.Equals(item.FacilityDefinitionId,
                             "facility.fortification.city_gate",
                             StringComparison.Ordinal)))
                .OrderBy(item => CityLandmarkPriority(item, overview))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal);
            foreach (var facility in landmarks)
            {
                if (!_map.TryGetLuoyangCityFacilityWorldPosition(
                        facility.FacilityId, out var position)) continue;
                var screen = camera.WorldToScreenPoint(position +
                                                       Vector3.up * 0.35f);
                if (screen.z <= 0f || screen.x < 0f ||
                    screen.x > Screen.width - BuildingPanelWidth - 20f ||
                    screen.y < BottomHudHeight ||
                    screen.y > Screen.height - TopHudHeight) continue;
                var rect = new Rect(screen.x - 42f,
                    Screen.height - screen.y - 15f, 84f, 26f);
                if (occupied.Any(item => item.Overlaps(rect))) continue;
                occupied.Add(rect);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(rect, facility.DisplayName, _playerMarkerStyle);
            }
        }

        private static int CityLandmarkPriority(
            LuoyangCityFacilityProjection facility, bool overview)
        {
            if (string.Equals(facility.DisplayName, "南宫",
                    StringComparison.Ordinal)) return 0;
            if (string.Equals(facility.DisplayName, "北宫",
                    StringComparison.Ordinal)) return 1;
            if (string.Equals(facility.FacilityId,
                    PlayableLuoyangWorldContractIds.MarketFacilityId,
                    StringComparison.Ordinal)) return 2;
            if (overview) return 99;
            if (string.Equals(facility.DisplayName, "太仓",
                    StringComparison.Ordinal)) return 3;
            if (string.Equals(facility.FacilityId,
                    "facility.instance.luoyang.184.arsenal",
                    StringComparison.Ordinal)) return 4;
            return 10;
        }

        [Obsolete("City focus is now the County UrbanArea subview.")]
        public bool FocusSelectedCityFacility()
        {
            return IsCountyViewVisible &&
                   ShowCountySubView(CountySubViewMode.UrbanArea);
        }

        private void DrawObjectiveCard()
        {
            var width = Mathf.Min(470f,
                Mathf.Max(320f, Screen.width - BuildingPanelWidth - 56f));
            var rect = new Rect(16f, TopHudHeight + 12f, width, 92f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f,
                rect.width - 28f, 23f), "当前玩法目标", _panelTitleStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 33f,
                rect.width - 132f, 50f), BuildCurrentObjectiveText(),
                _objectiveStyle);
            var hasTarget = !string.IsNullOrWhiteSpace(
                CurrentObjectiveFacilityId());
            GUI.enabled = hasTarget && !_map.LuoyangPedestrianIsWalking;
            if (GUI.Button(new Rect(rect.x + rect.width - 112f,
                    rect.y + 41f, 98f, 34f), "前往目标"))
                TryMoveToObjectiveFacility();
            GUI.enabled = true;
        }

        private void DrawPlayerWorldMarker()
        {
            var camera = _map?.PresentationCamera;
            if (camera == null) return;
            LuoyangClickWalkPedestrianInstance actor;
            try
            {
                actor = _map.GetLuoyangClickWalkPedestrian();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            var screen = camera.WorldToScreenPoint(
                actor.transform.position + Vector3.up * 0.48f);
            if (screen.z <= 0f) return;
            var width = _map.LuoyangPedestrianIsWalking ? 112f : 66f;
            var x = Mathf.Clamp(screen.x - width * 0.5f, 4f,
                Screen.width - BuildingPanelWidth - width - 24f);
            var y = Mathf.Clamp(Screen.height - screen.y - 34f,
                TopHudHeight + 6f, Screen.height - BottomHudHeight - 36f);
            var rect = new Rect(x, y, width, 30f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, _map.LuoyangPedestrianIsWalking
                ? "◆ 我 · 行走中" : "◆ 我", _playerMarkerStyle);
        }

        private void DrawBuildingPanel()
        {
            var height = Mathf.Max(310f,
                Screen.height - TopHudHeight - BottomHudHeight - 24f);
            var rect = new Rect(Screen.width - BuildingPanelWidth - 16f,
                TopHudHeight + 12f, BuildingPanelWidth, height);
            GUI.Box(rect, GUIContent.none);
            var facility = SelectedFacility;
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f,
                rect.width - 32f, 27f), "建筑信息", _panelTitleStyle);
            if (facility == null)
            {
                GUI.Label(new Rect(rect.x + 16f, rect.y + 52f,
                    rect.width - 32f, 100f),
                    "左键点击地图上的建筑，即可查看名称、类型、通行条件和可执行玩法。",
                    _panelBodyStyle);
                return;
            }

            var y = rect.y + 47f;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 31f),
                facility.DisplayName, _panelTitleStyle);
            y += 35f;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 23f),
                $"类型：{FacilityTypeLabel(facility.DefinitionId)}",
                _panelBodyStyle);
            y += 24f;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 23f),
                $"状态：{(facility.LifecycleStatus == FacilityLifecycleStatus.Operational ? "正常运作" : facility.LifecycleStatus.ToString())}　" +
                $"完好度：{facility.ConditionBasisPoints / 100f:0}%",
                _panelBodyStyle);
            y += 24f;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 23f),
                $"依据：{ConfidenceLabel(facility.HistoricalConfidence)} · " +
                PrecisionLabel(facility.SpatialPrecision), _panelBodyStyle);
            y += 24f;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 23f),
                $"所有者：{facility.OwnerId}　控制者：{facility.ControllerId}",
                _panelBodyStyle);
            y += 24f;
            if (_map.TryGetLuoyangFacilitySpatialCapability(facility.Id,
                    out var capability))
            {
                GUI.Label(new Rect(rect.x + 16f, y,
                        rect.width - 32f, 23f),
                    $"通行：{(capability.RequiresAccess ? "须从正式入口进入" : "可直接抵达")}　" +
                    $"Cell {facility.CellId64}", _panelBodyStyle);
                y += 25f;
                var profile = LuoyangNearfieldVisualProfileResolver.Resolve(
                    _map.LuoyangHumanScaleLocalMapPlan, facility.Id);
                GUI.Label(new Rect(rect.x + 16f, y,
                        rect.width - 32f, 23f),
                    $"近景接口：{profile.ProfileId}", _panelHintStyle);
                y += 25f;
            }

            var atFacility = string.Equals(facility.Id,
                Player().CurrentFacilityId,
                StringComparison.Ordinal) &&
                !_map.LuoyangPedestrianIsWalking;
            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 24f),
                atFacility ? "人物位置：已到达" :
                _map.LuoyangPedestrianIsWalking && string.Equals(facility.Id,
                    _map.LuoyangPedestrianTargetFacilityId,
                    StringComparison.Ordinal)
                    ? "人物位置：正在前往" : "人物位置：尚未到达",
                atFacility ? _panelBodyStyle : _panelHintStyle);
            y += 31f;
            GUI.enabled = !atFacility && !_map.LuoyangPedestrianIsWalking;
            if (GUI.Button(new Rect(rect.x + 16f, y,
                    rect.width - 32f, 38f), "前往此处"))
                TryMoveToSelectedFacility();
            GUI.enabled = true;
            y += 49f;

            GUI.Label(new Rect(rect.x + 16f, y, rect.width - 32f, 25f),
                "此处可做", _panelTitleStyle);
            y += 30f;
            if (!atFacility)
            {
                GUI.Label(new Rect(rect.x + 16f, y,
                    rect.width - 32f, 50f),
                    "先点击“前往此处”。人物到达后，这里的正式行动才会开放。",
                    _panelHintStyle);
                return;
            }

            var actions = ContextActions(facility);
            if (actions.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 16f, y,
                    rect.width - 32f, 44f),
                    "当前身份在这里没有可执行行动。", _panelHintStyle);
                return;
            }
            foreach (var action in actions.Take(5))
            {
                GUI.enabled = action.IsAvailable;
                if (GUI.Button(new Rect(rect.x + 16f, y,
                        rect.width - 32f, 34f), action.DisplayName))
                    ExecuteSelectedBuildingAction(action.Id);
                GUI.enabled = true;
                y += 38f;
                if (!action.IsAvailable &&
                    !string.IsNullOrWhiteSpace(action.UnavailableReason))
                {
                    GUI.Label(new Rect(rect.x + 20f, y,
                            rect.width - 40f, 35f),
                        action.UnavailableReason, _panelHintStyle);
                    y += 36f;
                }
                if (y > rect.yMax - 42f) break;
            }
        }

        private string MovementStatusLabel()
        {
            if (_map.LuoyangPedestrianIsWalking) return "行走中";
            if (!string.IsNullOrWhiteSpace(
                    _map.LuoyangPedestrianLastStopReasonId)) return "受阻";
            return "可行动";
        }

        private void DrawPauseMenu()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height),
                GUIContent.none);
            var width = 360f;
            var height = 292f;
            var rect = new Rect((Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 30f, rect.y + 22f,
                width - 60f, 42f), "游戏暂停", _pauseTitleStyle);
            if (GUI.Button(new Rect(rect.x + 45f, rect.y + 82f,
                    width - 90f, 42f), "继续游戏"))
                SetPaused(false);
            if (GUI.Button(new Rect(rect.x + 45f, rect.y + 134f,
                    width - 90f, 42f), "保存到内存存档"))
                SaveToMemory();
            if (GUI.Button(new Rect(rect.x + 45f, rect.y + 186f,
                    width - 90f, 42f), "返回主菜单"))
                _dashboard.ReturnToMainMenuFromDirectGame();
            if (GUI.Button(new Rect(rect.x + 45f, rect.y + 238f,
                    width - 90f, 34f), "取消"))
                SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            if (_map != null) _map.enabled = !paused;
        }

        private void EnsureStyles()
        {
            if (_hudTitleStyle != null && _hudStyle != null &&
                _hudSmallStyle != null && _messageStyle != null &&
                _pauseTitleStyle != null && _panelTitleStyle != null &&
                _panelBodyStyle != null && _panelHintStyle != null &&
                _objectiveStyle != null && _playerMarkerStyle != null) return;
            _hudTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.76f, 0.31f) }
            };
            _hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            _hudSmallStyle = new GUIStyle(_hudStyle)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.82f, 0.84f, 0.79f) }
            };
            _messageStyle = new GUIStyle(_hudStyle)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.96f, 0.88f, 0.67f) }
            };
            _pauseTitleStyle = new GUIStyle(_hudTitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28
            };
            _panelTitleStyle = new GUIStyle(_hudTitleStyle)
            {
                fontSize = 17,
                wordWrap = true
            };
            _panelBodyStyle = new GUIStyle(_hudStyle)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.91f, 0.91f, 0.87f) }
            };
            _panelHintStyle = new GUIStyle(_panelBodyStyle)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.72f, 0.75f, 0.69f) }
            };
            _objectiveStyle = new GUIStyle(_panelBodyStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.86f, 0.58f) }
            };
            _playerMarkerStyle = new GUIStyle(_hudStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.78f, 0.22f) }
            };
        }
    }
}
