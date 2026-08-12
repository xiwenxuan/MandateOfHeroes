using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mandate.Presentation
{
    public sealed class SimulationDashboard : MonoBehaviour
    {
        private const ulong DefaultSeed = 184_001UL;

        [SerializeField] private bool _playerDemoMode;
        [SerializeField] private bool _showDeveloperTools = true;

        private enum ScreenMode : byte
        {
            MainMenu,
            NewGame,
            Playing
        }

        private enum PlayerPanel : byte
        {
            Map,
            Town,
            Character,
            Actions,
            Tasks,
            World,
            Developer
        }

        private enum MapOverlay : byte
        {
            Terrain,
            PublicOrder,
            GrainPrice,
            War
        }

        private enum MapDetailLevel : byte
        {
            Strategic,
            Regional,
            Local
        }

        private enum MapNavigationMode : byte
        {
            StrategicAtlas,
            CaravanJourney
        }

        private static readonly string[] StartingIdentityLabels =
        {
            "军人",
            "县吏",
            "商人",
            "医者",
            "农户",
            "士人"
        };

        private static readonly string[] StartingBackgroundLabels =
        {
            "本地家户",
            "流离家户",
            "受助家户"
        };

        private MerchantHouseholdContentRegistry _merchantContent;
        private NewGameSetupService _newGameSetupService;
        private WorldState _world;
        private WorldState _selectionPreview;
        private WorldSimulator _simulator;
        private PlayerActionService _playerActionService;
        private NpcDecisionSystem _decisionSystem;
        private NpcActionResolver _actionResolver;
        private readonly NpcActionPlanner _actionPlanner = new NpcActionPlanner();
        private readonly TravelSystem _travelSystem = new TravelSystem();
        private readonly TaskSystem _taskSystem = new TaskSystem();
        private readonly TradingSystem _tradingSystem = new TradingSystem();
        private readonly ArmySystem _armySystem = new ArmySystem();
        private readonly MilitarySupplySystem _militarySupplySystem =
            new MilitarySupplySystem();
        private readonly MilitaryServiceSystem _militaryServiceSystem =
            new MilitaryServiceSystem();
        private readonly MilitaryAuthoritySystem _militaryAuthoritySystem =
            new MilitaryAuthoritySystem();
        private readonly MilitaryEquipmentSystem _militaryEquipmentSystem =
            new MilitaryEquipmentSystem();
        private readonly MilitaryProcurementSystem _militaryProcurementSystem =
            new MilitaryProcurementSystem();
        private readonly ProcessingProductionSystem _processingProductionSystem =
            new ProcessingProductionSystem();
        private readonly UpstreamResourceProductionSystem
            _upstreamResourceProductionSystem =
                new UpstreamResourceProductionSystem();
        private readonly LivestockProductionSystem _livestockProductionSystem =
            new LivestockProductionSystem();
        private BattleResolver _battleResolver;
        private MedicalSystem _medicalSystem;
        private readonly ConstructionSystem _constructionSystem =
            new ConstructionSystem();
        private readonly MerchantTownOperationSystem _townOperationSystem =
            new MerchantTownOperationSystem();
        private readonly PopulationLedgerSystem _populationLedgerSystem =
            new PopulationLedgerSystem();
        private readonly EducationSystem _educationSystem =
            new EducationSystem();
        private readonly Dictionary<string, NpcDecision> _decisions =
            new Dictionary<string, NpcDecision>(StringComparer.Ordinal);
        private readonly PlayerActionPresentationSequence _actionPresentation =
            new PlayerActionPresentationSequence();

        private Vector2 _scroll;
        private string _snapshot;
        private string _message = "世界尚未初始化。";
        private readonly List<string> _actionLog = new List<string>();
        private ScreenMode _screen = ScreenMode.MainMenu;
        private PlayerPanel _playerPanel = PlayerPanel.Map;
        private int _newGameMode;
        private string _customName = "无名";
        private string _customAge = "18";
        private int _customGender;
        private int _customIdentity;
        private int _customBackground;
        private int _customStartingLocation;
        private int _existingPersonIndex;
        private string _existingPersonSearch = string.Empty;
        private int _educationDiscipline;
        private int _educationStudyDays = 10;
        private bool _educationUseTeacher = true;
        private bool _educationUseFamilyFunds;
        private Vector2 _selectionScroll;
        private MapOverlay _mapOverlay;
        private MapPerspective _mapPerspective;
        private MapNavigationMode _mapNavigationMode;
        private float _mapZoom = 1f;
        private Vector2 _mapPan;
        private bool _mapDragging;
        private int _mapDragButton = -1;
        private Vector2 _mapDragStart;
        private Vector2 _mapPanStart;
        private string _selectedLocationId;
        private string _enteredTownLocationId;
        private string _enteredTownFacilityId;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _mapLabelStyle;
        private GUIStyle _mapSealStyle;
        private GUIStyle _townHeroTitleStyle;
        private GUIStyle _townHeroDetailStyle;
        private GUIStyle _townCardTitleStyle;
        private GUIStyle _townCardDetailStyle;
        private ProceduralSilkMapArt _mapArt;
        private Texture2D _townOverviewTexture;

        private void Awake()
        {
            EnsurePlayableSceneObjects();
            _mapArt = new ProceduralSilkMapArt();
            _townOverviewTexture = Resources.Load<Texture2D>(
                TownVisualPresentation.ZhongshanOverviewResourcePath);
            _merchantContent = LoadMerchantHouseholdContent();
            _newGameSetupService = new NewGameSetupService(_merchantContent);
            _selectionPreview = PrototypeWorldFactory.Create184World(DefaultSeed);
            _message = "请选择开始新游戏，创建人物或扮演世界中的现有人物。";
        }

        private static void EnsurePlayableSceneObjects()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void OnDestroy()
        {
            _mapArt?.Dispose();
            _mapArt = null;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (_screen == ScreenMode.MainMenu)
            {
                DrawMainMenu();
                return;
            }

            if (_screen == ScreenMode.NewGame)
            {
                DrawNewGame();
                return;
            }

            DrawPlayerGame();
        }

        private void DrawMainMenu()
        {
            var width = Mathf.Min(620f, Screen.width - 40f);
            var height = Mathf.Min(520f, Screen.height - 40f);
            GUILayout.BeginArea(
                new Rect(
                    (Screen.width - width) * 0.5f,
                    (Screen.height - height) * 0.5f,
                    width,
                    height),
                GUI.skin.box);
            GUILayout.FlexibleSpace();
            GUILayout.Label("群雄志：仕途", _titleStyle);
            GUILayout.Label(
                "从一名普通人或历史人物开始，在东汉末年的同一个世界中生活、从业、旅行并参与历史。",
                _normalStyle);
            GUILayout.Space(24);

            if (GUILayout.Button("开始新游戏", GUILayout.Height(48)))
            {
                _selectionPreview = PrototypeWorldFactory.Create184World(DefaultSeed);
                _screen = ScreenMode.NewGame;
                _message = "请选择自建人物或现有人物。";
            }

            if (GUILayout.Button("商旅—家族体验（推荐）", GUILayout.Height(48)))
            {
                try
                {
                    EnterWorld(_newGameSetupService.CreateCustom184World(
                        new NewGameCharacterRequest
                        {
                            DisplayName = "沈衡",
                            Age = 24,
                            Gender = PersonGender.Male,
                            Identity = StartingIdentity.Merchant,
                            BackgroundId = StartingBackgroundIds.LocalHousehold,
                            StartingLocationId = "location.zhongshan"
                        },
                        DefaultSeed));
                    _playerPanel = PlayerPanel.Actions;
                    _message = "中山家中欠账在即。先查看目标、行情来源与每项行动的代价，再决定怎样筹资本。";
                }
                catch (Exception exception)
                {
                    _message = exception.Message;
                }
            }

            GUI.enabled = _world != null;
            if (GUILayout.Button("继续当前游戏", GUILayout.Height(40)))
            {
                _screen = ScreenMode.Playing;
            }

            GUI.enabled = !string.IsNullOrEmpty(_snapshot);
            if (GUILayout.Button("读取内存存档", GUILayout.Height(40)))
            {
                try
                {
                    EnterWorld(WorldSnapshotSerializer.Deserialize(_snapshot));
                    _message = "已读取内存存档。";
                }
                catch (Exception exception)
                {
                    _message = exception.Message;
                }
            }

            GUI.enabled = true;
            if (_showDeveloperTools &&
                GUILayout.Button("开发者快速进入（刘备）", GUILayout.Height(36)))
            {
                EnterWorld(
                    _newGameSetupService.CreateExisting184World(
                        "person.liu_bei",
                        DefaultSeed));
                _playerPanel = PlayerPanel.Developer;
                _message = "已使用刘备进入开发观察台。";
            }

            GUILayout.Space(18);
            GUILayout.Label(_message, _normalStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawNewGame()
        {
            GUILayout.BeginArea(new Rect(24, 20, Screen.width - 48, Screen.height - 40));
            GUILayout.Label("开始184年新游戏", _titleStyle);
            GUILayout.Label(
                "第一版场景覆盖涿县—中山—广宗。自建身份决定初始地点、资源和组织职位。",
                _normalStyle);
            GUILayout.Space(10);

            _newGameMode = GUILayout.Toolbar(
                _newGameMode,
                new[] { "自建人物", "选择现有人物" },
                GUILayout.Height(36));
            GUILayout.Space(12);

            _selectionScroll = GUILayout.BeginScrollView(_selectionScroll);
            if (_newGameMode == 0)
            {
                DrawCustomCharacterSetup();
            }
            else
            {
                DrawExistingCharacterSetup();
            }

            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("返回主菜单", GUILayout.Height(38)))
            {
                _screen = ScreenMode.MainMenu;
            }

            if (GUILayout.Button("进入世界", GUILayout.Height(38)))
            {
                TryStartSelectedGame();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(_message, _normalStyle);
            GUILayout.EndArea();
        }

        private void DrawCustomCharacterSetup()
        {
            GUILayout.Label("人物姓名", _sectionStyle);
            _customName = GUILayout.TextField(_customName, 16, GUILayout.Height(34));

            GUILayout.Space(12);
            GUILayout.Label("开局年龄（16—70）", _sectionStyle);
            _customAge = GUILayout.TextField(_customAge, 2, GUILayout.Height(34));

            GUILayout.Space(12);
            GUILayout.Label("性别", _sectionStyle);
            _customGender = GUILayout.Toolbar(
                _customGender,
                new[] { "男", "女" },
                GUILayout.Height(34));

            GUILayout.Space(12);
            GUILayout.Label("初始身份", _sectionStyle);
            _customIdentity = GUILayout.SelectionGrid(
                _customIdentity,
                StartingIdentityLabels,
                2,
                GUILayout.Height(84));
            GUILayout.Space(8);
            GUILayout.Label(StartingIdentityDescription(_customIdentity), _normalStyle);

            GUILayout.Space(12);
            GUILayout.Label("出生背景", _sectionStyle);
            _customBackground = GUILayout.Toolbar(
                _customBackground,
                StartingBackgroundLabels,
                GUILayout.Height(34));
            GUILayout.Label(
                _customBackground == 1
                    ? "流离开局资源较少并背负少量家债。"
                    : _customBackground == 2
                        ? "受到宗族或师友接济，开局资源较充足。"
                        : "在本地家户中稳定起步。",
                _normalStyle);

            GUILayout.Space(12);
            GUILayout.Label("开局地点", _sectionStyle);
            var startingLocations = BuildLegalStartingLocations();
            var locationLabels = new string[startingLocations.Count];
            for (var i = 0; i < startingLocations.Count; i++)
            {
                locationLabels[i] = startingLocations[i].DisplayName;
            }
            _customStartingLocation = GUILayout.SelectionGrid(
                Mathf.Clamp(_customStartingLocation, 0, locationLabels.Length - 1),
                locationLabels,
                3,
                GUILayout.Height(70));
        }

        private List<LocationState> BuildLegalStartingLocations()
        {
            var legalIds = _newGameSetupService.GetLegalStartingLocationIds(
                _selectionPreview,
                (StartingIdentity)_customIdentity);
            var result = new List<LocationState>();
            for (var legalIndex = 0; legalIndex < legalIds.Count; legalIndex++)
            {
                for (var locationIndex = 0;
                     locationIndex < _selectionPreview.Locations.Count;
                     locationIndex++)
                {
                    var location = _selectionPreview.Locations[locationIndex];
                    if (location.Id == legalIds[legalIndex])
                    {
                        result.Add(location);
                        break;
                    }
                }
            }
            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "当前身份没有合法的开局地点。");
            }
            return result;
        }

        private void DrawExistingCharacterSetup()
        {
            GUILayout.Label("选择要扮演的人物", _sectionStyle);
            GUILayout.Label("搜索姓名、地点、身份或组织", _normalStyle);
            _existingPersonSearch = GUILayout.TextField(
                _existingPersonSearch, 32, GUILayout.Height(32));
            var candidates = BuildExistingPlayerCandidates();
            var labels = new string[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                var person = candidates[i];
                var historical = person.Id.StartsWith(
                    "person.generated.", StringComparison.Ordinal)
                    ? "[世界人物]"
                    : "[史料人物]";
                labels[i] =
                    $"{historical}{person.DisplayName}　" +
                    $"{FindLocationName(_selectionPreview, person.LocationId)}　" +
                    $"{FindIdentityName(_selectionPreview, person.Id)}";
            }

            if (candidates.Count == 0)
            {
                GUILayout.Label("没有符合搜索条件的在世人物。", _normalStyle);
                _existingPersonIndex = -1;
                return;
            }

            _existingPersonIndex = GUILayout.SelectionGrid(
                Mathf.Clamp(_existingPersonIndex, 0, candidates.Count - 1),
                labels,
                2,
                GUILayout.Height(Mathf.Max(160f, labels.Length * 27f)));
            GUILayout.Space(8);
            GUILayout.Label(
                "现有人物使用同一套人物、身份和世界规则；选择后不会获得额外的玩家专属加成。",
                _normalStyle);
        }

        private List<PersonState> BuildExistingPlayerCandidates()
        {
            var candidates = new List<PersonState>();
            var query = (_existingPersonSearch ?? string.Empty).Trim();
            for (var i = 0; i < _selectionPreview.People.Count; i++)
            {
                var person = _selectionPreview.People[i];
                if (!person.IsAlive)
                {
                    continue;
                }

                if (query.Length == 0 ||
                    ContainsIgnoreCase(person.DisplayName, query) ||
                    ContainsIgnoreCase(
                        FindLocationName(_selectionPreview, person.LocationId),
                        query) ||
                    ContainsIgnoreCase(
                        FindIdentityName(_selectionPreview, person.Id),
                        query) ||
                    PersonOrganizationMatches(person.Id, query))
                {
                    candidates.Add(person);
                }
            }
            return candidates;
        }

        private bool PersonOrganizationMatches(string personId, string query)
        {
            for (var i = 0; i < _selectionPreview.Memberships.Count; i++)
            {
                var membership = _selectionPreview.Memberships[i];
                if (membership.PersonId != personId)
                {
                    continue;
                }
                for (var organizationIndex = 0;
                     organizationIndex < _selectionPreview.Organizations.Count;
                     organizationIndex++)
                {
                    var organization =
                        _selectionPreview.Organizations[organizationIndex];
                    if (organization.Id == membership.OrganizationId &&
                        ContainsIgnoreCase(organization.DisplayName, query))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string SelectedBackgroundId()
        {
            switch (_customBackground)
            {
                case 1:
                    return StartingBackgroundIds.DisplacedHousehold;
                case 2:
                    return StartingBackgroundIds.SupportedHousehold;
                default:
                    return StartingBackgroundIds.LocalHousehold;
            }
        }

        private void TryStartSelectedGame()
        {
            try
            {
                if (_newGameMode == 0)
                {
                    if (!int.TryParse(_customAge, out var age))
                    {
                        throw new ArgumentException("年龄必须是整数。");
                    }

                    var startingLocations = BuildLegalStartingLocations();
                    var request = new NewGameCharacterRequest
                    {
                        DisplayName = _customName,
                        Age = age,
                        Gender = _customGender == 0
                            ? PersonGender.Male
                            : PersonGender.Female,
                        Identity = (StartingIdentity)_customIdentity,
                        BackgroundId = SelectedBackgroundId(),
                        StartingLocationId = startingLocations[
                            Mathf.Clamp(
                                _customStartingLocation,
                                0,
                                startingLocations.Count - 1)].Id
                    };
                    EnterWorld(
                        _newGameSetupService.CreateCustom184World(
                            request,
                            DefaultSeed));
                }
                else
                {
                    var candidates = BuildExistingPlayerCandidates();
                    if (_existingPersonIndex < 0 ||
                        _existingPersonIndex >= candidates.Count)
                    {
                        throw new InvalidOperationException("请选择一名现有人物。");
                    }

                    EnterWorld(
                        _newGameSetupService.CreateExisting184World(
                            candidates[_existingPersonIndex].Id,
                            DefaultSeed));
                }

                _message = $"已进入184年世界，当前扮演{FindPlayer().DisplayName}。";
            }
            catch (Exception exception)
            {
                _message = exception.Message;
            }
        }

        private void EnterWorld(WorldState world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            if (string.IsNullOrEmpty(_world.PlayerPersonId))
            {
                throw new InvalidOperationException("存档没有指定玩家控制人物。");
            }

            RebindServices();
            RefreshMonthlyDecisions();
            _actionLog.Clear();
            _screen = ScreenMode.Playing;
            _playerPanel = PlayerPanel.Map;
            _scroll = Vector2.zero;
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            _selectedLocationId = FindPlayer().LocationId;
            _actionPresentation.ResetActive();
            _mapPerspective = MapPerspectiveSystem.RecommendForPlayer(
                _world,
                _world.PlayerPersonId);
            _mapNavigationMode = _mapPerspective == MapPerspective.Commerce
                ? MapNavigationMode.CaravanJourney
                : MapNavigationMode.StrategicAtlas;
        }

        private void DrawPlayerGame()
        {
            _actionPresentation.Update(Time.realtimeSinceStartup);
            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height - 32));
            DrawPlayerHeader();
            DrawTrackedMerchantGoal();
            DrawActionPresentation();
            GUILayout.Space(8);
            GUILayout.Label(_message, _normalStyle);
            GUILayout.Space(8);

            _scroll = GUILayout.BeginScrollView(_scroll);
            switch (_playerPanel)
            {
                case PlayerPanel.Map:
                    DrawPlayerMap();
                    break;
                case PlayerPanel.Town:
                    DrawPlayerTown();
                    break;
                case PlayerPanel.Character:
                    DrawPlayerCharacter();
                    break;
                case PlayerPanel.Actions:
                    DrawPlayerActions();
                    break;
                case PlayerPanel.Tasks:
                    DrawPlayerTasks();
                    break;
                case PlayerPanel.World:
                    DrawWorldSummary();
                    DrawHistoricalTimeline();
                    DrawLocations();
                    DrawActionLog();
                    break;
                case PlayerPanel.Developer:
                    DrawDeveloperDashboard();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPlayerHeader()
        {
            var player = FindPlayer();
            var journey = FindJourney(player.Id);
            var travelText = journey == null
                ? string.Empty
                : $"　旅途中：前往{FindLocationName(journey.DestinationLocationId)}，" +
                  $"剩余{journey.RemainingKilometers}公里";
            GUILayout.Label("群雄志：仕途——184年涿县至广宗", _titleStyle);
            GUILayout.Label(
                $"扮演：{player.DisplayName}　身份：{FindIdentityName(_world, player.Id)}　" +
                $"所在地：{FindLocationName(player.LocationId)}　" +
                $"第{_world.AbsoluteDay + 1}日{travelText}",
                _normalStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("地图", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Map);
            }

            if (GUILayout.Button("城镇", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Town);
            }

            if (GUILayout.Button("人物", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Character);
            }

            if (GUILayout.Button("行动", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Actions);
            }

            if (GUILayout.Button("任务", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Tasks);
            }

            if (GUILayout.Button("天下", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.World);
            }

            if (_showDeveloperTools &&
                GUILayout.Button("开发观察台", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Developer);
            }

            if (GUILayout.Button("推进一天", GUILayout.Height(32)))
            {
                AdvancePlayerDays(1);
            }

            if (GUILayout.Button("推进三天", GUILayout.Height(32)))
            {
                AdvancePlayerDays(3);
            }

            if (_showDeveloperTools &&
                GUILayout.Button("结算NPC", GUILayout.Height(32)))
            {
                ResolveMonthlyNpcActions();
            }

            if (GUILayout.Button("内存保存", GUILayout.Height(32)))
            {
                _snapshot = WorldSnapshotSerializer.Serialize(_world);
                _message = $"已保存当前世界，共{_snapshot.Length}个字符。";
            }

            GUI.enabled = !string.IsNullOrEmpty(_snapshot);
            if (GUILayout.Button("读取", GUILayout.Height(32)))
            {
                EnterWorld(WorldSnapshotSerializer.Deserialize(_snapshot));
                _message = "已恢复内存存档，玩家身份保持不变。";
            }

            GUI.enabled = true;
            if (GUILayout.Button("主菜单", GUILayout.Height(32)))
            {
                _screen = ScreenMode.MainMenu;
                _message = "当前世界仍保留在内存中，可选择继续当前游戏。";
            }

            GUILayout.EndHorizontal();
        }

        private void SetPlayerPanel(PlayerPanel panel)
        {
            _playerPanel = panel;
            _scroll = Vector2.zero;
        }

        private void DrawTownNavigationCallout(
            PersonState player,
            JourneyState journey)
        {
            var entry = TownNavigationPresentation.Build(
                _world,
                player.Id,
                journey != null,
                _townOperationSystem);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("当前位置与城镇入口", _sectionStyle);
            GUILayout.Label(entry.Guidance, _normalStyle);
            GUI.enabled = entry.CanEnter;
            if (GUILayout.Button(entry.ButtonLabel, GUILayout.Height(42)))
            {
                OpenCurrentTown(FindLocation(entry.LocationId));
            }
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void OpenCurrentTown(LocationState location)
        {
            var player = FindPlayer();
            if (location == null || player.LocationId != location.Id ||
                FindJourney(player.Id) != null)
            {
                _message = "必须先抵达当前地点，才能进入城镇。";
                return;
            }

            _selectedLocationId = location.Id;
            _enteredTownLocationId = location.Id;
            _enteredTownFacilityId = string.Empty;
            _mapZoom = Mathf.Max(_mapZoom, 1.75f);
            _mapPerspective = MapPerspective.Commerce;
            _mapNavigationMode = MapNavigationMode.CaravanJourney;
            SetPlayerPanel(PlayerPanel.Town);
            _message =
                $"已进入{location.DisplayName}城镇；请选择要进入的具体建筑。";
        }

        private void DrawPlayerTown()
        {
            var player = FindPlayer();
            var journey = FindJourney(player.Id);
            GUILayout.Label("当前城镇", _sectionStyle);
            if (journey != null)
            {
                GUILayout.Label(
                    $"正在前往{FindLocationName(journey.DestinationLocationId)}，" +
                    "抵达后才能进入当地建筑。",
                    _normalStyle);
                if (GUILayout.Button("返回地图", GUILayout.Height(36)))
                {
                    SetPlayerPanel(PlayerPanel.Map);
                }
                return;
            }

            var location = FindLocation(player.LocationId);
            var entry = TownNavigationPresentation.Build(
                _world,
                player.Id,
                false,
                _townOperationSystem);
            GUILayout.Label(entry.Guidance, _normalStyle);
            if (entry.VisibleFacilityCount == 0)
            {
                GUILayout.Label(
                    "请返回地图旅行到中山，体验首批商号、仓库、市场和客舍建筑。",
                    _normalStyle);
                if (GUILayout.Button("返回地图", GUILayout.Height(36)))
                {
                    SetPlayerPanel(PlayerPanel.Map);
                }
                return;
            }

            _selectedLocationId = location.Id;
            _enteredTownLocationId = location.Id;
            DrawTownFacilities(location, player);
        }

        private void DrawPlayerMap()
        {
            var player = FindPlayer();
            var journey = FindJourney(player.Id);
            GUILayout.Label("地区地图", _sectionStyle);
            GUILayout.Label(
                "点击地点查看详情；鼠标滚轮缩放，按住鼠标右键或中键拖动地图。",
                _normalStyle);

            DrawTownNavigationCallout(player, journey);

            GUILayout.BeginHorizontal();
            GUILayout.Label("地图模式", GUILayout.Width(70f), GUILayout.Height(30f));
            _mapNavigationMode = (MapNavigationMode)GUILayout.Toolbar(
                (int)_mapNavigationMode,
                new[] { "战略舆图", "商队行旅" },
                GUILayout.Height(30f));
            GUILayout.EndHorizontal();
            GUILayout.Label(
                _mapNavigationMode == MapNavigationMode.StrategicAtlas
                    ? "战略舆图读取城市、道路、军队和地方形势，用于全局判断。"
                    : "商队行旅读取同一世界事实，突出当前车队、载货、口粮、路线与市场机会。",
                _normalStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("身份视角", GUILayout.Width(70f), GUILayout.Height(30f));
            _mapPerspective = (MapPerspective)GUILayout.Toolbar(
                (int)_mapPerspective,
                new[] { "通用", "军务", "政务", "商旅", "医药" },
                GUILayout.Height(30f));
            GUILayout.EndHorizontal();
            GUILayout.Label(
                MapPerspectiveDescription(_mapPerspective),
                _normalStyle);

            GUILayout.BeginHorizontal();
            _mapOverlay = (MapOverlay)GUILayout.Toolbar(
                (int)_mapOverlay,
                new[] { "地形", "治安", "粮价", "战争" },
                GUILayout.Height(32));
            if (GUILayout.Button("重置视角", GUILayout.Width(110), GUILayout.Height(32)))
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }

            GUILayout.EndHorizontal();

            var mapHeight = Mathf.Clamp(Screen.height - 365f, 430f, 650f);
            var mapRect = GUILayoutUtility.GetRect(
                100f,
                mapHeight,
                GUILayout.ExpandWidth(true));
            HandleMapInput(mapRect);
            DrawRegionMap(mapRect, player, journey);

            GUILayout.Space(10);
            if (journey != null)
            {
                GUILayout.Label(
                    $"正在从{FindLocationName(journey.OriginLocationId)}前往" +
                    $"{FindLocationName(journey.DestinationLocationId)}，" +
                    $"剩余{journey.RemainingKilometers}公里。",
                    _normalStyle);
                if (GUILayout.Button("推进一天旅程", GUILayout.Height(38)))
                {
                    AdvancePlayerDays(1);
                }
            }
            else
            {
                DrawSelectedLocationDetails(player);
            }
            if (_mapNavigationMode == MapNavigationMode.CaravanJourney)
            {
                DrawCaravanJourneyStatus(player, journey);
            }
        }

        private void HandleMapInput(Rect mapRect)
        {
            var current = Event.current;
            if (!mapRect.Contains(current.mousePosition))
            {
                if (current.type == EventType.MouseUp)
                {
                    _mapDragging = false;
                    _mapDragButton = -1;
                }

                return;
            }

            if (current.type == EventType.ScrollWheel)
            {
                var oldZoom = _mapZoom;
                var zoomFactor = current.delta.y > 0f ? 0.9f : 1.1f;
                _mapZoom = Mathf.Clamp(oldZoom * zoomFactor, 0.65f, 2.4f);
                var relative =
                    current.mousePosition - mapRect.center - _mapPan;
                _mapPan += relative * (1f - _mapZoom / oldZoom);
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown &&
                (current.button == 1 || current.button == 2))
            {
                _mapDragging = true;
                _mapDragButton = current.button;
                _mapDragStart = current.mousePosition;
                _mapPanStart = _mapPan;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag &&
                _mapDragging &&
                current.button == _mapDragButton)
            {
                _mapPan = _mapPanStart + current.mousePosition - _mapDragStart;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp &&
                _mapDragging &&
                current.button == _mapDragButton)
            {
                _mapDragging = false;
                _mapDragButton = -1;
                current.Use();
            }
        }

        private void DrawRegionMap(
            Rect mapRect,
            PersonState player,
            JourneyState journey)
        {
            GUI.BeginGroup(mapRect);
            var canvas = new Rect(0f, 0f, mapRect.width, mapRect.height);
            DrawMapTerrain(canvas);
            DrawMapRiver(canvas);
            DrawMapRoutes(canvas);
            DrawLocalMapDetails(canvas);
            if (_mapNavigationMode == MapNavigationMode.StrategicAtlas)
            {
                DrawArmyMarkers(canvas);
            }
            DrawLocationNodes(canvas, player);
            DrawPlayerMarker(canvas, player, journey);
            DrawMapLegend(canvas);

            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var tooltipRect =
                    new Rect(canvas.width - 310f, 12f, 296f, 48f);
                DrawMapPanel(tooltipRect, 0.92f);
                GUI.Label(tooltipRect, GUI.tooltip, _mapLabelStyle);
            }

            GUI.EndGroup();
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawMapTerrain(Rect canvas)
        {
            if (_mapArt == null)
            {
                _mapArt = new ProceduralSilkMapArt();
            }

            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(
                canvas,
                _mapArt.SilkTexture,
                new Rect(
                    0f,
                    0f,
                    Mathf.Max(1f, canvas.width / 128f),
                    Mathf.Max(1f, canvas.height / 128f)));

            DrawTerrainBrush(
                canvas,
                1_600,
                3_500,
                430f,
                470f,
                new Color(0.25f, 0.39f, 0.22f, 0.58f));
            DrawTerrainBrush(
                canvas,
                3_400,
                7_300,
                520f,
                260f,
                new Color(0.43f, 0.31f, 0.15f, 0.35f));
            DrawTerrainBrush(
                canvas,
                6_200,
                3_700,
                680f,
                310f,
                new Color(0.53f, 0.43f, 0.22f, 0.28f));
            DrawTerrainBrush(
                canvas,
                8_000,
                7_200,
                460f,
                330f,
                new Color(0.34f, 0.42f, 0.21f, 0.34f));

            DrawMountainStamp(canvas, 900, 1_400, 138f);
            DrawMountainStamp(canvas, 1_500, 2_800, 122f);
            DrawMountainStamp(canvas, 1_200, 6_700, 150f);
            DrawMountainStamp(canvas, 3_000, 8_400, 116f);
            if (CurrentMapDetailLevel() != MapDetailLevel.Strategic)
            {
                DrawMountainStamp(canvas, 7_900, 1_300, 96f);
                DrawMountainStamp(canvas, 8_800, 8_400, 110f);
            }

            DrawMapFrame(canvas);
            GUI.color = previous;
        }

        private void DrawTerrainBrush(
            Rect canvas,
            int xBasisPoints,
            int yBasisPoints,
            float width,
            float height,
            Color color)
        {
            var point = MapPoint(canvas, xBasisPoints, yBasisPoints);
            var rect = new Rect(
                point.x - width * _mapZoom * 0.5f,
                point.y - height * _mapZoom * 0.5f,
                width * _mapZoom,
                height * _mapZoom);
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                rect,
                _mapArt.BrushStampTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = previous;
        }

        private void DrawMountainStamp(
            Rect canvas,
            int xBasisPoints,
            int yBasisPoints,
            float width)
        {
            var point = MapPoint(canvas, xBasisPoints, yBasisPoints);
            var height = width * 0.56f;
            var rect = new Rect(
                point.x - width * _mapZoom * 0.5f,
                point.y - height * _mapZoom * 0.5f,
                width * _mapZoom,
                height * _mapZoom);
            var previous = GUI.color;
            GUI.color = new Color(
                ProceduralSilkMapArt.Ochre.r,
                ProceduralSilkMapArt.Ochre.g,
                ProceduralSilkMapArt.Ochre.b,
                0.68f);
            GUI.DrawTexture(
                rect,
                _mapArt.MountainStampTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = previous;
        }

        private static void DrawMapFrame(Rect canvas)
        {
            var border = ProceduralSilkMapArt.Ink;
            border.a = 0.72f;
            DrawSolidRect(new Rect(0f, 0f, canvas.width, 3f), border);
            DrawSolidRect(
                new Rect(0f, canvas.height - 3f, canvas.width, 3f),
                border);
            DrawSolidRect(new Rect(0f, 0f, 3f, canvas.height), border);
            DrawSolidRect(
                new Rect(canvas.width - 3f, 0f, 3f, canvas.height),
                border);
            border.a = 0.25f;
            DrawSolidRect(new Rect(8f, 8f, canvas.width - 16f, 1f), border);
            DrawSolidRect(
                new Rect(8f, canvas.height - 9f, canvas.width - 16f, 1f),
                border);
        }

        private void DrawMapRiver(Rect canvas)
        {
            var river = new[]
            {
                MapPoint(canvas, 600, 5_000),
                MapPoint(canvas, 2_200, 5_350),
                MapPoint(canvas, 4_000, 5_150),
                MapPoint(canvas, 5_900, 5_700),
                MapPoint(canvas, 7_500, 5_450),
                MapPoint(canvas, 9_500, 6_050)
            };
            for (var i = 0; i < river.Length - 1; i++)
            {
                DrawMapLine(
                    river[i],
                    river[i + 1],
                    9f,
                    new Color(0.12f, 0.18f, 0.16f, 0.34f));
                DrawMapLine(
                    river[i],
                    river[i + 1],
                    6f,
                    new Color(0.20f, 0.43f, 0.55f, 0.88f));
                DrawMapLine(
                    river[i],
                    river[i + 1],
                    1.5f,
                    new Color(0.58f, 0.73f, 0.72f, 0.78f));
            }
        }

        private void DrawMapRoutes(Rect canvas)
        {
            for (var i = 0; i < _world.Routes.Count; i++)
            {
                var route = _world.Routes[i];
                var from = MapPoint(canvas, FindLocation(route.FromLocationId));
                var to = MapPoint(canvas, FindLocation(route.ToLocationId));
                var color = RouteColor(route);
                DrawMapLine(from, to, 5f, new Color(0.18f, 0.11f, 0.06f, 0.46f));
                DrawMapLine(from, to, 2.4f, color);

                if (CurrentMapDetailLevel() != MapDetailLevel.Strategic)
                {
                    var midpoint = (from + to) * 0.5f;
                    var distanceRect =
                        new Rect(midpoint.x - 34f, midpoint.y - 14f, 68f, 19f);
                    DrawMapPanel(distanceRect, 0.78f);
                    GUI.Label(
                        distanceRect,
                        $"{route.DistanceKilometers}公里",
                        _mapLabelStyle);
                }
            }
        }

        private void DrawLocalMapDetails(Rect canvas)
        {
            if (CurrentMapDetailLevel() != MapDetailLevel.Local)
            {
                return;
            }

            for (var i = 0; i < _world.Locations.Count; i++)
            {
                var location = _world.Locations[i];
                var point = MapPoint(canvas, location);
                var visibleFeatures = MapPerspectiveSystem.Inspect(
                    _world,
                    location,
                    _mapPerspective).VisibleFeatures;
                if ((visibleFeatures & LocationFeature.Farmland) != 0)
                {
                    DrawFarmRows(point + new Vector2(-62f, 34f));
                }

                if (location.Terrain == TerrainKind.Forest ||
                    location.Terrain == TerrainKind.Hills ||
                    location.Terrain == TerrainKind.Mountains)
                {
                    DrawGrove(point + new Vector2(58f, -38f));
                }

                if ((visibleFeatures &
                     (LocationFeature.Garrison |
                      LocationFeature.Fortification)) != 0)
                {
                    DrawWatchPost(point + new Vector2(62f, 37f));
                }

                var featureIndex = 0;
                DrawLocalFeatureToken(
                    visibleFeatures,
                    LocationFeature.Market,
                    "市",
                    new Color(0.60f, 0.36f, 0.13f, 1f),
                    point,
                    ref featureIndex);
                DrawLocalFeatureToken(
                    visibleFeatures,
                    LocationFeature.Workshop,
                    "坊",
                    new Color(0.46f, 0.29f, 0.16f, 1f),
                    point,
                    ref featureIndex);
                DrawLocalFeatureToken(
                    visibleFeatures,
                    LocationFeature.Clinic,
                    "医",
                    new Color(0.24f, 0.48f, 0.32f, 1f),
                    point,
                    ref featureIndex);
                DrawLocalFeatureToken(
                    visibleFeatures,
                    LocationFeature.RelayStation,
                    "驿",
                    new Color(0.24f, 0.42f, 0.55f, 1f),
                    point,
                    ref featureIndex);
            }
        }

        private void DrawLocalFeatureToken(
            LocationFeature visibleFeatures,
            LocationFeature feature,
            string label,
            Color color,
            Vector2 point,
            ref int featureIndex)
        {
            if ((visibleFeatures & feature) == 0)
            {
                return;
            }

            var tokenRect = new Rect(
                point.x - 39f + featureIndex * 25f,
                point.y + 56f,
                21f,
                21f);
            DrawSolidRect(
                new Rect(
                    tokenRect.x - 2f,
                    tokenRect.y - 2f,
                    tokenRect.width + 4f,
                    tokenRect.height + 4f),
                ProceduralSilkMapArt.Ink);
            DrawSolidRect(tokenRect, color);
            GUI.Label(tokenRect, label, _mapSealStyle);
            featureIndex++;
        }

        private static void DrawFarmRows(Vector2 center)
        {
            var color = new Color(0.43f, 0.31f, 0.13f, 0.52f);
            for (var row = 0; row < 4; row++)
            {
                var start = center + new Vector2(-17f, row * 5f);
                DrawMapLine(start, start + new Vector2(34f, 0f), 1.2f, color);
            }
        }

        private void DrawGrove(Vector2 center)
        {
            var previous = GUI.color;
            GUI.color = new Color(0.20f, 0.37f, 0.20f, 0.74f);
            for (var i = 0; i < 3; i++)
            {
                var rect = new Rect(
                    center.x - 15f + i * 11f,
                    center.y - 10f + (i % 2) * 6f,
                    20f,
                    20f);
                GUI.DrawTexture(
                    rect,
                    _mapArt.BrushStampTexture,
                    ScaleMode.StretchToFill,
                    true);
            }

            GUI.color = previous;
        }

        private static void DrawWatchPost(Vector2 center)
        {
            var ink = ProceduralSilkMapArt.Ink;
            ink.a = 0.65f;
            DrawSolidRect(
                new Rect(center.x - 1f, center.y - 11f, 2f, 21f),
                ink);
            DrawSolidRect(
                new Rect(center.x - 7f, center.y - 12f, 14f, 4f),
                ProceduralSilkMapArt.Cinnabar);
        }

        private void DrawLocationNodes(Rect canvas, PersonState player)
        {
            for (var i = 0; i < _world.Locations.Count; i++)
            {
                var location = _world.Locations[i];
                var point = MapPoint(canvas, location);
                var detailLevel = CurrentMapDetailLevel();
                var width = detailLevel == MapDetailLevel.Strategic
                    ? 82f
                    : Mathf.Clamp(116f * _mapZoom, 96f, 146f);
                var height = detailLevel == MapDetailLevel.Strategic
                    ? 38f
                    : Mathf.Clamp(48f * _mapZoom, 44f, 62f);
                var isCurrent = location.Id == player.LocationId;
                var isSelected = location.Id == _selectedLocationId;
                var isAdjacent = FindRouteBetween(player.LocationId, location.Id) != null;
                var townFacilityCount = 0;
                for (var facilityIndex = 0;
                     facilityIndex < _world.TownFacilities.Count;
                     facilityIndex++)
                {
                    var facility = _world.TownFacilities[facilityIndex];
                    if (facility.LocationId == location.Id &&
                        facility.IsPubliclyVisible)
                    {
                        townFacilityCount++;
                    }
                }
                var border = isCurrent
                    ? new Color(1f, 0.78f, 0.18f, 1f)
                    : isSelected
                        ? new Color(0.38f, 0.78f, 1f, 1f)
                        : isAdjacent
                            ? new Color(0.82f, 0.88f, 0.62f, 0.9f)
                            : new Color(0.20f, 0.20f, 0.18f, 0.8f);

                var previous = GUI.color;
                var sealSize = Mathf.Clamp(38f * _mapZoom, 32f, 47f);
                var sealRect = new Rect(
                    point.x - sealSize * 0.5f,
                    point.y - sealSize * 0.5f,
                    sealSize,
                    sealSize);
                var locationColor = LocationColor(location);
                if (townFacilityCount > 0 &&
                    detailLevel != MapDetailLevel.Strategic)
                {
                    DrawTownNodeSilhouette(point, sealSize, townFacilityCount);
                }
                GUI.color = border;
                GUI.DrawTexture(
                    new Rect(
                        sealRect.x - 3f,
                        sealRect.y - 3f,
                        sealRect.width + 6f,
                        sealRect.height + 6f),
                    _mapArt.SealTexture,
                    ScaleMode.StretchToFill,
                    true);
                GUI.color = locationColor;
                GUI.DrawTexture(
                    sealRect,
                    _mapArt.SealTexture,
                    ScaleMode.StretchToFill,
                    true);
                GUI.color = previous;
                GUI.Label(sealRect, LocationKindGlyph(location.Kind), _mapSealStyle);

                var labelRect = new Rect(
                    point.x + sealSize * 0.34f,
                    point.y - height * 0.48f,
                    width * 0.78f,
                    height);
                DrawMapPanel(labelRect, 0.87f);
                var label = detailLevel == MapDetailLevel.Strategic
                    ? location.DisplayName
                    : location.DisplayName + "\n" +
                        LocationOverlayLabel(location) +
                        (townFacilityCount > 0
                            ? $" · {townFacilityCount}处建筑"
                            : string.Empty);
                GUI.Label(labelRect, label, _mapLabelStyle);
                var tooltip =
                    $"{location.DisplayName}　{LocationKindName(location.Kind)}　" +
                    $"{TerrainKindName(location.Terrain)}　人口{location.Population}　" +
                    $"治安{location.PublicOrderBasisPoints / 100f:F1}%　" +
                    $"粮价{location.GrainPrice}　" +
                    MapPerspectiveSystem.Inspect(
                        _world,
                        location,
                        _mapPerspective).SecondaryMetric;
                var hitRect = Rect.MinMaxRect(
                    Mathf.Min(sealRect.xMin, labelRect.xMin),
                    Mathf.Min(sealRect.yMin, labelRect.yMin),
                    Mathf.Max(sealRect.xMax, labelRect.xMax),
                    Mathf.Max(sealRect.yMax, labelRect.yMax));
                if (GUI.Button(
                        hitRect,
                        new GUIContent(string.Empty, tooltip),
                        GUIStyle.none))
                {
                    _selectedLocationId = location.Id;
                    _enteredTownLocationId = string.Empty;
                    _enteredTownFacilityId = string.Empty;
                    _message = $"已选择{location.DisplayName}。";
                }

                if (isCurrent && townFacilityCount > 0 &&
                    FindJourney(player.Id) == null)
                {
                    var enterRect = new Rect(
                        labelRect.x,
                        labelRect.yMax + 3f,
                        labelRect.width,
                        22f);
                    if (GUI.Button(enterRect, "进入城镇"))
                    {
                        _selectedLocationId = location.Id;
                        OpenCurrentTown(location);
                    }
                }

                GUI.color = previous;
            }
        }

        private void DrawTownNodeSilhouette(
            Vector2 point,
            float sealSize,
            int facilityCount)
        {
            var shown = Mathf.Clamp(facilityCount, 2, 5);
            var baseY = point.y + sealSize * 0.56f;
            for (var i = 0; i < shown; i++)
            {
                var width = 13f + i % 2 * 4f;
                var height = 8f + i % 3 * 2f;
                var x = point.x - shown * 7f + i * 14f;
                DrawSolidRect(
                    new Rect(x, baseY - height, width, height),
                    new Color(0.20f, 0.16f, 0.11f, 0.88f));
                DrawSolidRect(
                    new Rect(x - 2f, baseY - height - 3f, width + 4f, 3f),
                    ProceduralSilkMapArt.Cinnabar);
            }
        }

        private void DrawArmyMarkers(Rect canvas)
        {
            for (var i = 0; i < _world.Armies.Count; i++)
            {
                var army = _world.Armies[i];
                var point = ArmyMapPoint(canvas, army);
                var hostile =
                    army.OrganizationId == "organization.taiping_yellow_turban";
                var markerPoint = point + new Vector2(0f, 28f + i * 3f);
                var factionColor = hostile
                    ? new Color(0.72f, 0.31f, 0.12f, 1f)
                    : new Color(0.18f, 0.38f, 0.50f, 1f);
                DrawSolidRect(
                    new Rect(markerPoint.x - 29f, markerPoint.y - 13f, 2f, 30f),
                    ProceduralSilkMapArt.Ink);
                DrawSolidRect(
                    new Rect(markerPoint.x - 27f, markerPoint.y - 13f, 56f, 24f),
                    factionColor);
                DrawSolidRect(
                    new Rect(markerPoint.x - 23f, markerPoint.y - 9f, 48f, 16f),
                    new Color(0.82f, 0.72f, 0.50f, 0.92f));
                GUI.Label(
                    new Rect(markerPoint.x - 23f, markerPoint.y - 10f, 48f, 18f),
                    ArmyPerspectiveLabel(army, hostile),
                    _mapLabelStyle);
            }
        }

        private void DrawPlayerMarker(
            Rect canvas,
            PersonState player,
            JourneyState journey)
        {
            Vector2 point;
            if (journey == null)
            {
                point = MapPoint(canvas, FindLocation(player.LocationId));
            }
            else
            {
                var route = FindRoute(journey.RouteId);
                var progress = 1f -
                    Mathf.Clamp01(
                        journey.RemainingKilometers /
                        (float)route.DistanceKilometers);
                point = Vector2.Lerp(
                    MapPoint(canvas, FindLocation(journey.OriginLocationId)),
                    MapPoint(canvas, FindLocation(journey.DestinationLocationId)),
                    progress);
            }

            var marker = new Rect(point.x - 15f, point.y - 47f, 30f, 30f);
            DrawSolidRect(
                new Rect(
                    marker.x - 3f,
                    marker.y - 3f,
                    marker.width + 6f,
                    marker.height + 6f),
                ProceduralSilkMapArt.Ink);
            DrawSolidRect(marker, new Color(0.78f, 0.55f, 0.13f, 1f));
            GUI.Label(marker, "我", _mapSealStyle);
        }

        private void DrawMapLegend(Rect canvas)
        {
            var legendRect = new Rect(
                12f,
                canvas.height - 70f,
                Mathf.Min(540f, canvas.width - 24f),
                56f);
            DrawMapPanel(legendRect, 0.91f);
            GUI.Label(
                legendRect,
                $"{MapDetailLevelName(CurrentMapDetailLevel())}　" +
                $"模式：{MapNavigationModeName(_mapNavigationMode)}　" +
                $"视角：{MapPerspectiveName(_mapPerspective)}　" +
                $"图层：{MapOverlayName(_mapOverlay)}　缩放：{_mapZoom:F1}倍\n" +
                "金色=玩家　石青=汉军　朱砂=黄巾　线色=道路治安",
                _mapLabelStyle);
        }

        private static string MapNavigationModeName(MapNavigationMode mode) =>
            mode == MapNavigationMode.CaravanJourney
                ? "商队行旅"
                : "战略舆图";

        private static void DrawMapPanel(Rect rect, float opacity)
        {
            DrawSolidRect(
                new Rect(
                    rect.x - 2f,
                    rect.y - 2f,
                    rect.width + 4f,
                    rect.height + 4f),
                new Color(
                    ProceduralSilkMapArt.Ink.r,
                    ProceduralSilkMapArt.Ink.g,
                    ProceduralSilkMapArt.Ink.b,
                    Mathf.Clamp01(opacity * 0.62f)));
            DrawSolidRect(
                rect,
                new Color(
                    ProceduralSilkMapArt.SilkLight.r,
                    ProceduralSilkMapArt.SilkLight.g,
                    ProceduralSilkMapArt.SilkLight.b,
                    Mathf.Clamp01(opacity)));
        }

        private MapDetailLevel CurrentMapDetailLevel()
        {
            if (_mapZoom < 0.86f)
            {
                return MapDetailLevel.Strategic;
            }

            return _mapZoom < 1.55f
                ? MapDetailLevel.Regional
                : MapDetailLevel.Local;
        }

        private static string MapDetailLevelName(MapDetailLevel level)
        {
            switch (level)
            {
                case MapDetailLevel.Strategic:
                    return "天下概览";
                case MapDetailLevel.Local:
                    return "县乡近览";
                default:
                    return "州郡舆图";
            }
        }

        private static string MapPerspectiveName(MapPerspective perspective)
        {
            switch (perspective)
            {
                case MapPerspective.Military:
                    return "军务";
                case MapPerspective.Administration:
                    return "政务";
                case MapPerspective.Commerce:
                    return "商旅";
                case MapPerspective.Medicine:
                    return "医药";
                default:
                    return "通用";
            }
        }

        private static string MapPerspectiveDescription(
            MapPerspective perspective)
        {
            switch (perspective)
            {
                case MapPerspective.Military:
                    return "军务视角突出驻军、伤兵、城防、交通节点和战略价值。";
                case MapPerspective.Administration:
                    return "政务视角突出人口、治安、官署、农田和地方交通。";
                case MapPerspective.Commerce:
                    return "商旅视角突出粮价、库存、市场、工坊、驿站和港池。";
                case MapPerspective.Medicine:
                    return "医药视角突出低健康人物、军队伤兵、药价、医馆和补给点。";
                default:
                    return "通用视角显示地貌、地点层级、战略重要度与基础设施。";
            }
        }

        private string ArmyPerspectiveLabel(ArmyState army, bool hostile)
        {
            if (_mapPerspective == MapPerspective.Medicine)
            {
                return $"伤{army.WoundedTroops}";
            }

            if (_mapPerspective == MapPerspective.Military ||
                _mapPerspective == MapPerspective.General)
            {
                return $"{(hostile ? "黄" : "汉")}{army.Troops}";
            }

            return hostile ? "黄巾军" : "汉军";
        }

        private void DrawSelectedLocationDetails(PersonState player)
        {
            var selected = FindLocation(
                string.IsNullOrEmpty(_selectedLocationId)
                    ? player.LocationId
                    : _selectedLocationId);
            GUILayout.Label($"{selected.DisplayName}详情", _sectionStyle);
            GUILayout.Label(
                $"人口：{selected.Population}　粮价：{selected.GrainPrice}　" +
                $"治安：{selected.PublicOrderBasisPoints / 100f:F1}%",
                _normalStyle);
            GUILayout.Label(
                $"层级：{LocationKindName(selected.Kind)}　" +
                $"地貌：{TerrainKindName(selected.Terrain)}　" +
                $"战略重要度：{selected.StrategicImportance}星",
                _normalStyle);
            GUILayout.Label(
                $"设施：{LocationFeaturesName(selected.Features)}",
                _normalStyle);
            var perspectiveInfo = MapPerspectiveSystem.Inspect(
                _world,
                selected,
                _mapPerspective);
            GUILayout.Label(
                $"{MapPerspectiveName(_mapPerspective)}情报：" +
                $"{perspectiveInfo.PrimaryMetric}　{perspectiveInfo.SecondaryMetric}",
                _normalStyle);
            if (_mapPerspective == MapPerspective.Commerce ||
                _mapNavigationMode == MapNavigationMode.CaravanJourney)
            {
                DrawLocalMarketDetails(selected);
            }
            DrawLocationConstruction(selected, player);

            var armyCount = 0;
            for (var i = 0; i < _world.Armies.Count; i++)
            {
                var army = _world.Armies[i];
                if (army.LocationId != selected.Id || FindArmyMarch(army.Id) != null)
                {
                    continue;
                }

                armyCount++;
                GUILayout.Label(
                    $"驻军：{army.DisplayName}　兵力{army.Troops}　" +
                    $"士气{army.MoraleBasisPoints / 100f:F1}%",
                    _normalStyle);
            }

            if (armyCount == 0)
            {
                GUILayout.Label("当前没有驻军。", _normalStyle);
            }

            if (selected.Id == player.LocationId)
            {
                GUILayout.Label("这是你当前所在的地点。", _normalStyle);
                if (GUILayout.Button(
                        "进入城镇（查看并进入真实建筑）",
                        GUILayout.Height(38)))
                {
                    OpenCurrentTown(selected);
                }
                if (_enteredTownLocationId == selected.Id)
                {
                    DrawTownFacilities(selected, player);
                }
                return;
            }

            var route = FindRouteBetween(player.LocationId, selected.Id);
            if (route == null)
            {
                GUILayout.Label("该地点不与当前位置直接相连，需要分段旅行。", _normalStyle);
                return;
            }

            if (GUILayout.Button(
                    $"沿{route.DistanceKilometers}公里道路前往{selected.DisplayName}　" +
                    $"治安{route.SecurityBasisPoints / 100f:F1}%",
                    GUILayout.Height(40)))
            {
                TryStartPlayerJourney(route, selected.Id);
            }
        }

        private void DrawTownFacilities(
            LocationState location,
            PersonState player)
        {
            GUILayout.Space(10);
            GUILayout.Label(location.DisplayName + "城镇建筑", _sectionStyle);
            var town = _townOperationSystem.InspectTown(
                _world,
                player.Id,
                location.Id);
            if (town.Facilities.Count == 0)
            {
                GUILayout.Label(
                    "该地点还没有已建立的可见建筑事实。",
                    _normalStyle);
                return;
            }

            DrawTownOverview(location, town);

            var unplacedCount = 0;
            for (var i = 0; i < town.Facilities.Count; i++)
            {
                if (town.Facilities[i].HasMapPlacement)
                {
                    continue;
                }
                if (unplacedCount == 0)
                {
                    GUILayout.Label("待布置建筑", _sectionStyle);
                }
                DrawTownFacilityCard(town.Facilities[i]);
                GUILayout.Space(7f);
                unplacedCount++;
            }

            if (string.IsNullOrEmpty(_enteredTownFacilityId))
            {
                return;
            }

            TownFacilityView entered;
            try
            {
                entered = _townOperationSystem.EnterFacility(
                    _world,
                    player.Id,
                    _enteredTownFacilityId);
            }
            catch (Exception exception)
            {
                _enteredTownFacilityId = string.Empty;
                _message = exception.Message;
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("当前建筑：" + entered.DisplayName, _sectionStyle);
            DrawTownFacilityContents(location, player, entered);
            if (entered.OperationIds.Contains(
                    TownFacilityOperationIds.PrepareCaravan))
            {
                if (GUILayout.Button(
                        "打开经营准备与人物行动",
                        GUILayout.Height(34)))
                {
                    SetPlayerPanel(PlayerPanel.Actions);
                    _message =
                        "已从城镇建筑进入经营准备；采购、装货和启程会写回同一世界账。";
                }
            }
            if (GUILayout.Button("离开建筑", GUILayout.Height(28)))
            {
                _enteredTownFacilityId = string.Empty;
            }
            GUILayout.EndVertical();
        }

        private void DrawTownOverview(
            LocationState location,
            TownOperationView town)
        {
            var accessibleCount = 0;
            for (var i = 0; i < town.Facilities.Count; i++)
            {
                if (town.Facilities[i].CanEnter)
                {
                    accessibleCount++;
                }
            }

            var panorama = GUILayoutUtility.GetRect(
                320f,
                Mathf.Clamp(Screen.height * 0.48f, 400f, 620f),
                GUILayout.ExpandWidth(true));
            if (_townOverviewTexture != null &&
                !string.IsNullOrEmpty(
                    TownVisualPresentation.OverviewResourcePath(location.Id)))
            {
                GUI.DrawTexture(
                    panorama,
                    _townOverviewTexture,
                    ScaleMode.ScaleAndCrop,
                    true);
            }
            else
            {
                GUI.DrawTextureWithTexCoords(
                    panorama,
                    _mapArt.SilkTexture,
                    new Rect(
                        0f,
                        0f,
                        panorama.width / 128f,
                        panorama.height / 128f));
            }

            DrawSolidRect(
                new Rect(
                    panorama.x,
                    panorama.yMax - 64f,
                    panorama.width,
                    64f),
                new Color(0.08f, 0.06f, 0.04f, 0.78f));
            DrawSolidRect(
                new Rect(panorama.x, panorama.y, panorama.width, 4f),
                ProceduralSilkMapArt.Cinnabar);
            GUI.Label(
                new Rect(
                    panorama.x + 18f,
                    panorama.yMax - 59f,
                    panorama.width - 36f,
                    30f),
                location.DisplayName + "城镇空间近览",
                _townHeroTitleStyle);
            GUI.Label(
                new Rect(
                    panorama.x + 20f,
                    panorama.yMax - 31f,
                    panorama.width - 40f,
                    24f),
                $"在册建筑 {town.Facilities.Count} 处　当前可进入 {accessibleCount} 处　" +
                "点击地图建筑进入",
                _townHeroDetailStyle);
            GUI.Label(
                new Rect(
                    panorama.xMax - 190f,
                    panorama.y + 12f,
                    174f,
                    30f),
                "东汉城镇·原创美术",
                _townHeroDetailStyle);

            for (var i = 0; i < town.Facilities.Count; i++)
            {
                var facility = town.Facilities[i];
                if (!facility.HasMapPlacement)
                {
                    continue;
                }

                var width = Mathf.Clamp(
                    panorama.width *
                        facility.FootprintWidthBasisPoints / 10_000f,
                    96f,
                    162f);
                var height = Mathf.Clamp(
                    panorama.height *
                        facility.FootprintHeightBasisPoints / 10_000f,
                    58f,
                    88f);
                var centerX = panorama.x + panorama.width *
                    facility.MapXBasisPoints / 10_000f;
                var centerY = panorama.y + panorama.height *
                    facility.MapYBasisPoints / 10_000f;
                DrawTownFacilityMarker(
                    new Rect(
                        centerX - width * 0.5f,
                        centerY - height * 0.5f,
                        width,
                        height),
                    facility);
            }
            GUILayout.Space(10f);
        }

        private void DrawTownFacilityMarker(
            Rect marker,
            TownFacilityView facility)
        {
            var visual = TownVisualPresentation.Describe(facility.KindId);
            var tone = TownToneColor(visual.Tone);
            var entered = facility.FacilityId == _enteredTownFacilityId;
            var background = facility.CanEnter
                ? new Color(0.91f, 0.84f, 0.66f, 0.96f)
                : new Color(0.42f, 0.40f, 0.36f, 0.94f);
            if (entered)
            {
                background = new Color(0.98f, 0.85f, 0.48f, 1f);
            }

            DrawSolidRect(marker, ProceduralSilkMapArt.Ink);
            var inner = new Rect(
                marker.x + 2f,
                marker.y + 2f,
                marker.width - 4f,
                marker.height - 4f);
            DrawSolidRect(inner, background);
            DrawSolidRect(
                new Rect(inner.x, inner.y, inner.width, 5f),
                tone);
            var seal = new Rect(
                inner.x + 7f,
                inner.y + 11f,
                Mathf.Min(32f, inner.height - 18f),
                Mathf.Min(32f, inner.height - 18f));
            DrawSolidRect(seal, tone);
            GUI.Label(seal, visual.Seal, _mapSealStyle);
            GUI.Label(
                new Rect(
                    seal.xMax + 6f,
                    inner.y + 9f,
                    inner.xMax - seal.xMax - 10f,
                    22f),
                facility.DisplayName,
                _townCardTitleStyle);
            GUI.Label(
                new Rect(
                    seal.xMax + 6f,
                    inner.y + 31f,
                    inner.xMax - seal.xMax - 10f,
                    19f),
                TownVisualPresentation.DistrictName(facility.DistrictId) +
                (facility.CanEnter ? " · 可进入" : " · 权限受限"),
                _townCardDetailStyle);

            GUI.enabled = facility.CanEnter;
            if (GUI.Button(
                    marker,
                    new GUIContent(
                        string.Empty,
                        facility.CanEnter
                            ? "点击进入" + facility.DisplayName
                            : facility.UnavailableReason),
                    GUIStyle.none))
            {
                TryEnterTownFacility(facility);
            }
            GUI.enabled = true;
        }

        private void DrawTownFacilityCard(TownFacilityView facility)
        {
            var visual = TownVisualPresentation.Describe(facility.KindId);
            var card = GUILayoutUtility.GetRect(
                230f,
                132f,
                GUILayout.ExpandWidth(true));
            var entered = facility.FacilityId == _enteredTownFacilityId;
            var tone = TownToneColor(visual.Tone);
            var background = facility.CanEnter
                ? new Color(0.84f, 0.77f, 0.60f, 0.96f)
                : new Color(0.42f, 0.40f, 0.36f, 0.94f);
            if (entered)
            {
                background = new Color(0.91f, 0.81f, 0.55f, 1f);
            }

            DrawSolidRect(card, ProceduralSilkMapArt.Ink);
            var inner = new Rect(
                card.x + 3f,
                card.y + 3f,
                card.width - 6f,
                card.height - 6f);
            DrawSolidRect(inner, background);
            DrawSolidRect(
                new Rect(inner.x, inner.y, 7f, inner.height),
                tone);

            var seal = new Rect(inner.x + 18f, inner.y + 17f, 58f, 58f);
            DrawSolidRect(
                new Rect(
                    seal.x - 3f,
                    seal.y - 3f,
                    seal.width + 6f,
                    seal.height + 6f),
                ProceduralSilkMapArt.Ink);
            DrawSolidRect(seal, tone);
            GUI.Label(seal, visual.Seal, _mapSealStyle);

            var titleX = seal.xMax + 14f;
            GUI.Label(
                new Rect(
                    titleX,
                    inner.y + 13f,
                    inner.xMax - titleX - 12f,
                    28f),
                facility.DisplayName,
                _townCardTitleStyle);
            GUI.Label(
                new Rect(
                    titleX,
                    inner.y + 42f,
                    inner.xMax - titleX - 12f,
                    22f),
                visual.Category,
                _townCardDetailStyle);
            GUI.Label(
                new Rect(
                    inner.x + 18f,
                    inner.y + 82f,
                    inner.width - 36f,
                    21f),
                "所有者：" + facility.OwnerName +
                (string.IsNullOrEmpty(facility.ManagerName)
                    ? string.Empty
                    : "　负责人：" + facility.ManagerName),
                _townCardDetailStyle);
            GUI.Label(
                new Rect(
                    inner.x + 18f,
                    inner.y + 104f,
                    inner.width - 36f,
                    20f),
                entered
                    ? "● 当前所在建筑"
                    : facility.CanEnter
                        ? "● 开放进入"
                        : "○ " + facility.UnavailableReason,
                _townCardDetailStyle);

            GUI.enabled = facility.CanEnter;
            if (GUI.Button(
                    card,
                    new GUIContent(
                        string.Empty,
                        facility.CanEnter
                            ? "点击进入" + facility.DisplayName
                            : facility.UnavailableReason),
                    GUIStyle.none))
            {
                TryEnterTownFacility(facility);
            }
            GUI.enabled = true;
        }

        private void TryEnterTownFacility(TownFacilityView facility)
        {
            try
            {
                _townOperationSystem.EnterFacility(
                    _world,
                    _world.PlayerPersonId,
                    facility.FacilityId);
                _enteredTownFacilityId = facility.FacilityId;
                _message = "已进入" + facility.DisplayName + "。";
            }
            catch (Exception exception)
            {
                _message = exception.Message;
            }
        }

        private static Color TownToneColor(TownFacilityVisualTone tone)
        {
            switch (tone)
            {
                case TownFacilityVisualTone.Commerce:
                    return new Color(0.66f, 0.35f, 0.13f, 1f);
                case TownFacilityVisualTone.Organization:
                    return new Color(0.60f, 0.18f, 0.12f, 1f);
                case TownFacilityVisualTone.Storage:
                    return new Color(0.43f, 0.31f, 0.15f, 1f);
                case TownFacilityVisualTone.Hospitality:
                    return new Color(0.38f, 0.49f, 0.25f, 1f);
                case TownFacilityVisualTone.Transport:
                    return new Color(0.22f, 0.43f, 0.52f, 1f);
                case TownFacilityVisualTone.Guild:
                    return new Color(0.43f, 0.28f, 0.48f, 1f);
                case TownFacilityVisualTone.Government:
                    return new Color(0.30f, 0.30f, 0.28f, 1f);
                default:
                    return ProceduralSilkMapArt.Ochre;
            }
        }

        private void DrawTownFacilityContents(
            LocationState location,
            PersonState player,
            TownFacilityView facility)
        {
            if (facility.KindId == TownFacilityKindIds.Market)
            {
                DrawLocalMarketDetails(location);
                return;
            }

            if (facility.KindId == TownFacilityKindIds.MerchantHall)
            {
                var branch = _world.MerchantBranches.Find(item =>
                    item.LocationId == location.Id && item.IsHeadquarters);
                var organization = branch == null
                    ? null
                    : _world.Organizations.Find(item =>
                        item.Id == branch.OrganizationId);
                if (organization == null)
                {
                    GUILayout.Label(
                        "此处没有登记中的商号据点。",
                        _normalStyle);
                    return;
                }
                var memberCount = _world.Memberships.FindAll(item =>
                    item.OrganizationId == organization.Id).Count;
                GUILayout.Label(
                    $"{organization.DisplayName}　资金{organization.Treasury}钱　" +
                    $"声望{organization.ReputationBasisPoints / 100f:F1}%　" +
                    $"成员{memberCount}人",
                    _normalStyle);
                GUILayout.Label(
                    "主堂提供商号账本、成员与商旅准备入口；经营结果仍由正式市场、库存和行程系统结算。",
                    _normalStyle);
                return;
            }

            if (facility.KindId == TownFacilityKindIds.Warehouse)
            {
                long quantity = 0;
                long weight = 0;
                var batchCount = 0;
                for (var i = 0; i < _world.ProductBatches.Count; i++)
                {
                    var batch = _world.ProductBatches[i];
                    if (batch.InventoryContainerId !=
                        facility.InventoryContainerId || batch.Quantity <= 0)
                    {
                        continue;
                    }
                    quantity = checked(quantity + batch.Quantity);
                    weight = checked(
                        weight + batch.Quantity * batch.UnitWeight);
                    batchCount++;
                }
                var container = _world.InventoryContainers.Find(item =>
                    item.Id == facility.InventoryContainerId);
                GUILayout.Label(
                    $"正式库存：{batchCount}批、{quantity}单位、{weight}/" +
                    $"{(container == null ? 0 : container.CapacityWeight)}重量",
                    _normalStyle);
                GUILayout.Label(
                    batchCount == 0
                        ? "仓库当前为空；它不会自动生成商品，采购或生产后才会出现具体批次。"
                        : "每批货物保留产品、所有者、来源、数量、品质与存放容器。",
                    _normalStyle);
                return;
            }

            if (facility.KindId == TownFacilityKindIds.Inn)
            {
                var shown = 0;
                GUILayout.Label(
                    "本地可接触人物（招募合同将在后续子任务建立）：",
                    _normalStyle);
                for (var i = 0; i < _world.People.Count && shown < 5; i++)
                {
                    var person = _world.People[i];
                    if (!person.IsAlive || person.Id == player.Id ||
                        person.LocationId != location.Id)
                    {
                        continue;
                    }
                    GUILayout.Label("· " + person.DisplayName, _normalStyle);
                    shown++;
                }
                if (shown == 0)
                {
                    GUILayout.Label(
                        "目前没有可见的本地人物。",
                        _normalStyle);
                }
                return;
            }

            if (facility.KindId == TownFacilityKindIds.VehicleYard)
            {
                GUILayout.Label(
                    $"当前人物载重：{player.CargoCapacity}；" +
                    "载具、牲畜和耐久合同将在后续商队任务中接入。",
                    _normalStyle);
                return;
            }

            if (facility.KindId == TownFacilityKindIds.GuildHall)
            {
                GUILayout.Label(
                    "这里汇集商会、居民与军队的真实需求；可承接委托来自任务系统。",
                    _normalStyle);
                return;
            }

            if (facility.KindId == TownFacilityKindIds.GovernmentOffice)
            {
                GUILayout.Label(
                    "官署用于查看许可、税费与官府任务；具体权限仍由人物职位和组织关系决定。",
                    _normalStyle);
                return;
            }

            GUILayout.Label(
                "该建筑暂时没有可执行的经营操作。",
                _normalStyle);
        }

        private void DrawLocalMarketDetails(LocationState location)
        {
            GUILayout.Space(6);
            GUILayout.Label("当地市场", _sectionStyle);
            var count = 0;
            for (var i = 0; i < _world.MarketListings.Count; i++)
            {
                var listing = _world.MarketListings[i];
                if (listing.LocationId != location.Id)
                {
                    continue;
                }
                var commodity = _world.Commodities.Find(item =>
                    item.Id == listing.CommodityId);
                GUILayout.Label(
                    $"{(commodity == null ? listing.CommodityId : commodity.DisplayName)}　" +
                    $"现价{listing.Price}钱　库存{listing.Stock}　" +
                    $"常态库存{listing.TargetStock}",
                    _normalStyle);
                count++;
            }
            if (count == 0)
            {
                GUILayout.Label("当前没有可公开查询的市场货单。", _normalStyle);
            }
        }

        private void DrawCaravanJourneyStatus(
            PersonState player,
            JourneyState journey)
        {
            GUILayout.Space(10);
            GUILayout.Label("商队行旅账", _sectionStyle);
            long cargoWeight = 0;
            long clothQuantity = 0;
            var batchCount = 0;
            for (var containerIndex = 0;
                 containerIndex < _world.InventoryContainers.Count;
                 containerIndex++)
            {
                var container = _world.InventoryContainers[containerIndex];
                if (container.CarrierPersonId != player.Id)
                {
                    continue;
                }
                for (var batchIndex = 0;
                     batchIndex < _world.ProductBatches.Count;
                     batchIndex++)
                {
                    var batch = _world.ProductBatches[batchIndex];
                    if (batch.InventoryContainerId != container.Id ||
                        batch.Quantity <= 0)
                    {
                        continue;
                    }
                    cargoWeight = checked(
                        cargoWeight + batch.Quantity * batch.UnitWeight);
                    if (batch.ProductDefinitionId ==
                        CoreProductionContent.PlainClothProductId)
                    {
                        clothQuantity = checked(
                            clothQuantity + batch.Quantity);
                    }
                    batchCount++;
                }
            }
            for (var i = 0; i < _world.Inventories.Count; i++)
            {
                var stack = _world.Inventories[i];
                if (stack.OwnerPersonId != player.Id)
                {
                    continue;
                }
                var commodity = _world.Commodities.Find(item =>
                    item.Id == stack.CommodityId);
                if (commodity != null)
                {
                    cargoWeight = checked(
                        cargoWeight + (long)stack.Quantity *
                        commodity.UnitWeight);
                }
            }

            GUILayout.Label(
                $"载货：{cargoWeight}/{player.CargoCapacity}重量　" +
                $"正式批次{batchCount}批　素布{clothQuantity}匹　" +
                $"口粮{player.Provisions}份",
                _normalStyle);
            GUILayout.Label(
                journey == null
                    ? $"位置：{FindLocationName(player.LocationId)}　车队当前停驻"
                    : $"路线：{FindLocationName(journey.OriginLocationId)}→" +
                      $"{FindLocationName(journey.DestinationLocationId)}　" +
                      $"剩余{journey.RemainingKilometers}公里",
                _normalStyle);

            var companion = _world.People.Find(item =>
                item.Id == "person.su_shuang");
            if (companion != null)
            {
                var relationship = _world.Relationships.Find(item =>
                    item.FromPersonId == player.Id &&
                    item.ToPersonId == companion.Id);
                GUILayout.Label(
                    $"同行：{companion.DisplayName}　" +
                    $"信任{(relationship == null ? 0 : relationship.Trust)}　" +
                    $"状态{(FindJourney(companion.Id) == null ? "停驻" : "在途")}",
                    _normalStyle);
            }

            var goal = _playerActionService.InspectMerchantGoal(
                _world, player.Id);
            if (goal.IsAvailable && goal.MarketOpportunity != null)
            {
                GUILayout.Label(
                    $"已知商机：{FindLocationName(goal.MarketOpportunity.OriginLocationId)}" +
                    $"→{FindLocationName(goal.MarketOpportunity.TargetLocationId)}　" +
                    $"口信毛利{goal.MarketOpportunity.ExpectedGrossMargin}钱　" +
                    $"可信度{goal.MarketOpportunity.ReliabilityBasisPoints / 100f:F1}%",
                    _normalStyle);
            }
        }

        private void DrawLocationConstruction(
            LocationState location,
            PersonState player)
        {
            GUILayout.Space(8);
            GUILayout.Label("地方建设", _sectionStyle);
            var projectCount = 0;
            for (var i = 0; i < _world.ConstructionProjects.Count; i++)
            {
                var project = _world.ConstructionProjects[i];
                if (project.LocationId != location.Id)
                {
                    continue;
                }

                projectCount++;
                GUILayout.Label(
                    $"{project.DisplayName}　进度{project.Progress}/" +
                    $"{project.RequiredProgress}　累计投入{project.MoneyInvested}钱" +
                    (project.IsCompleted ? "　已完工" : string.Empty),
                    _normalStyle);
                if (project.IsCompleted)
                {
                    continue;
                }

                var canContribute =
                    player.LocationId == location.Id &&
                    FindJourney(player.Id) == null &&
                    player.Wealth >= 20;
                GUI.enabled = canContribute;
                if (GUILayout.Button(
                        "投入20钱并劳作一日",
                        GUILayout.Height(32)))
                {
                    try
                    {
                        var result = _constructionSystem.Contribute(
                            _world,
                            new StableId(project.Id),
                            new StableId(player.Id),
                            20,
                            20);
                        _simulator.AdvanceDays(_world, 1);
                        RefreshMonthlyDecisions();
                        _message = result.Summary + "世界推进了一日。";
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                    }
                }

                GUI.enabled = true;
                if (!canContribute)
                {
                    GUILayout.Label(
                        player.LocationId != location.Id
                            ? "必须到达当地才能投入建设。"
                            : player.Wealth < 20
                                ? "财富不足20钱。"
                                : "旅途中不能参与建设。",
                        _normalStyle);
                }
            }

            if (projectCount == 0)
            {
                GUILayout.Label("当前没有建设项目。", _normalStyle);
            }

            var suggestion = ConstructionSystem.RecommendFeature(
                location,
                _mapPerspective);
            if (suggestion == LocationFeature.None)
            {
                GUILayout.Label(
                    "当前视角下没有可建议的新设施。",
                    _normalStyle);
                return;
            }

            if (FindConstructionProject(location.Id, suggestion) != null)
            {
                return;
            }

            var canStart =
                player.LocationId == location.Id &&
                FindJourney(player.Id) == null;
            GUI.enabled = canStart;
            if (GUILayout.Button(
                    $"发起{ConstructionSystem.FeatureName(suggestion)}建设　" +
                    $"需要{ConstructionSystem.RequiredProgress(suggestion)}进度",
                    GUILayout.Height(34)))
            {
                try
                {
                    var project = _constructionSystem.StartProject(
                        _world,
                        new StableId(player.Id),
                        new StableId(location.Id),
                        suggestion);
                    _message = $"已发起{project.DisplayName}。";
                }
                catch (Exception exception)
                {
                    _message = exception.Message;
                }
            }

            GUI.enabled = true;
        }

        private ConstructionProjectState FindConstructionProject(
            string locationId,
            LocationFeature feature)
        {
            for (var i = 0; i < _world.ConstructionProjects.Count; i++)
            {
                var project = _world.ConstructionProjects[i];
                if (project.LocationId == locationId &&
                    project.TargetFeature == feature)
                {
                    return project;
                }
            }

            return null;
        }

        private Vector2 ArmyMapPoint(Rect canvas, ArmyState army)
        {
            var march = FindArmyMarch(army.Id);
            if (march == null)
            {
                return MapPoint(canvas, FindLocation(army.LocationId));
            }

            var route = FindRoute(march.RouteId);
            var progress = 1f -
                Mathf.Clamp01(
                    march.RemainingKilometers /
                    (float)route.DistanceKilometers);
            return Vector2.Lerp(
                MapPoint(canvas, FindLocation(march.OriginLocationId)),
                MapPoint(canvas, FindLocation(march.DestinationLocationId)),
                progress);
        }

        private Vector2 MapPoint(Rect canvas, LocationState location)
        {
            return MapPoint(
                canvas,
                location.MapXBasisPoints,
                location.MapYBasisPoints);
        }

        private Vector2 MapPoint(
            Rect canvas,
            int xBasisPoints,
            int yBasisPoints)
        {
            var width = Mathf.Max(100f, canvas.width - 150f) * _mapZoom;
            var height = Mathf.Max(100f, canvas.height - 120f) * _mapZoom;
            return canvas.center + _mapPan + new Vector2(
                (xBasisPoints / 10_000f - 0.5f) * width,
                (yBasisPoints / 10_000f - 0.5f) * height);
        }

        private static void DrawMapLine(
            Vector2 from,
            Vector2 to,
            float width,
            Color color)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }

            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                from);
            GUI.DrawTexture(
                new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width),
                Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private Color RouteColor(RouteState route)
        {
            var security = route.SecurityBasisPoints / 10_000f;
            return Color.Lerp(
                new Color(0.72f, 0.20f, 0.14f, 1f),
                new Color(0.78f, 0.72f, 0.42f, 1f),
                security);
        }

        private Color LocationColor(LocationState location)
        {
            switch (_mapOverlay)
            {
                case MapOverlay.PublicOrder:
                    return Color.Lerp(
                        new Color(0.88f, 0.24f, 0.16f, 1f),
                        new Color(0.32f, 0.78f, 0.30f, 1f),
                        location.PublicOrderBasisPoints / 10_000f);
                case MapOverlay.GrainPrice:
                    return Color.Lerp(
                        new Color(0.32f, 0.72f, 0.30f, 1f),
                        new Color(0.90f, 0.30f, 0.14f, 1f),
                        Mathf.InverseLerp(80f, 180f, location.GrainPrice));
                case MapOverlay.War:
                    return WarLocationColor(location.Id);
                default:
                    return new Color(0.72f, 0.64f, 0.36f, 1f);
            }
        }

        private Color WarLocationColor(string locationId)
        {
            var hasGovernmentArmy = false;
            for (var i = 0; i < _world.Armies.Count; i++)
            {
                var army = _world.Armies[i];
                if (army.LocationId != locationId)
                {
                    continue;
                }

                if (army.OrganizationId == "organization.taiping_yellow_turban")
                {
                    return new Color(0.90f, 0.34f, 0.15f, 1f);
                }

                hasGovernmentArmy = true;
            }

            return hasGovernmentArmy
                ? new Color(0.28f, 0.58f, 0.88f, 1f)
                : new Color(0.52f, 0.52f, 0.46f, 1f);
        }

        private string LocationOverlayLabel(LocationState location)
        {
            switch (_mapOverlay)
            {
                case MapOverlay.PublicOrder:
                    return $"治安{location.PublicOrderBasisPoints / 100f:F0}%";
                case MapOverlay.GrainPrice:
                    return $"粮价{location.GrainPrice}";
                case MapOverlay.War:
                    var armies = 0;
                    for (var i = 0; i < _world.Armies.Count; i++)
                    {
                        if (_world.Armies[i].LocationId == location.Id)
                        {
                            armies++;
                        }
                    }

                    return armies == 0 ? "无驻军" : $"{armies}支军队";
                default:
                    return MapPerspectiveSystem.Inspect(
                        _world,
                        location,
                        _mapPerspective).PrimaryMetric;
            }
        }

        private static string LocationKindGlyph(LocationKind kind)
        {
            switch (kind)
            {
                case LocationKind.RegionalSeat:
                    return "府";
                case LocationKind.Pass:
                    return "关";
                case LocationKind.Port:
                    return "港";
                case LocationKind.MarketTown:
                    return "镇";
                case LocationKind.Village:
                    return "村";
                case LocationKind.Camp:
                    return "营";
                default:
                    return "县";
            }
        }

        private static string LocationKindName(LocationKind kind)
        {
            switch (kind)
            {
                case LocationKind.RegionalSeat:
                    return "州郡治所";
                case LocationKind.Pass:
                    return "关隘";
                case LocationKind.Port:
                    return "港口";
                case LocationKind.MarketTown:
                    return "市镇";
                case LocationKind.Village:
                    return "村庄";
                case LocationKind.Camp:
                    return "营地";
                default:
                    return "县城";
            }
        }

        private static string TerrainKindName(TerrainKind terrain)
        {
            switch (terrain)
            {
                case TerrainKind.Hills:
                    return "丘陵";
                case TerrainKind.Mountains:
                    return "山地";
                case TerrainKind.Riverland:
                    return "河网";
                case TerrainKind.Forest:
                    return "森林";
                case TerrainKind.Marsh:
                    return "湿地";
                default:
                    return "平原";
            }
        }

        private static string LocationFeaturesName(LocationFeature features)
        {
            if (features == LocationFeature.None)
            {
                return "无";
            }

            var result = string.Empty;
            AppendLocationFeature(
                ref result, features, LocationFeature.Government, "官署");
            AppendLocationFeature(
                ref result, features, LocationFeature.Market, "市场");
            AppendLocationFeature(
                ref result, features, LocationFeature.Garrison, "驻军");
            AppendLocationFeature(
                ref result, features, LocationFeature.Farmland, "农田");
            AppendLocationFeature(
                ref result, features, LocationFeature.Workshop, "工坊");
            AppendLocationFeature(
                ref result, features, LocationFeature.Clinic, "医馆");
            AppendLocationFeature(
                ref result, features, LocationFeature.Temple, "寺观");
            AppendLocationFeature(
                ref result, features, LocationFeature.RelayStation, "驿站");
            AppendLocationFeature(
                ref result, features, LocationFeature.Harbor, "港池");
            AppendLocationFeature(
                ref result, features, LocationFeature.Fortification, "城防");
            return result;
        }

        private static void AppendLocationFeature(
            ref string result,
            LocationFeature features,
            LocationFeature expected,
            string label)
        {
            if ((features & expected) == 0)
            {
                return;
            }

            if (result.Length > 0)
            {
                result += "、";
            }

            result += label;
        }

        private static string MapOverlayName(MapOverlay overlay)
        {
            switch (overlay)
            {
                case MapOverlay.PublicOrder:
                    return "治安";
                case MapOverlay.GrainPrice:
                    return "粮价";
                case MapOverlay.War:
                    return "战争";
                default:
                    return "地形";
            }
        }

        private void TryStartPlayerJourney(
            RouteState route,
            string destinationId)
        {
            try
            {
                var player = FindPlayer();
                var mode = FindIdentityName(_world, player.Id) == "行商"
                    ? TravelMode.Caravan
                    : TravelMode.Foot;
                _travelSystem.StartJourney(
                    _world,
                    new StableId(player.Id),
                    new StableId(route.Id),
                    new StableId(destinationId),
                    mode);
                _message =
                    $"{player.DisplayName}已出发前往{FindLocationName(destinationId)}。";
            }
            catch (Exception exception)
            {
                _message = exception.Message;
            }
        }

        private void DrawPlayerCharacter()
        {
            var player = FindPlayer();
            var strategic = StrategicAttributeCalculator.Calculate(player);
            GUILayout.Label("人物", _sectionStyle);
            GUILayout.Label(
                $"姓名：{player.DisplayName}　性别：{GenderName(player.Gender)}　" +
                $"年龄：{Math.Max(0, (_world.AbsoluteDay - player.BirthDay) / 360)}岁",
                _normalStyle);
            GUILayout.Label(
                $"健康：{player.HealthBasisPoints / 100f:F1}%　财富：{player.Wealth}钱　" +
                $"口粮：{player.Provisions}　载货：{CargoSummary(player.Id)}",
                _normalStyle);
            GUILayout.Label(
                $"人生志向：{LifeGoalName(player.LifeGoal)}　" +
                $"身份：{FindIdentityName(_world, player.Id)}",
                _normalStyle);

            GUILayout.Space(12);
            GUILayout.Label("五维综合评价", _sectionStyle);
            GUILayout.Label(
                $"统率 {AbilityValue(strategic.Leadership)}　" +
                $"武勇 {AbilityValue(strategic.Martial)}　" +
                $"智略 {AbilityValue(strategic.Strategy)}　" +
                $"政务 {AbilityValue(strategic.Administration)}　" +
                $"魅力 {AbilityValue(strategic.Charisma)}",
                _normalStyle);
            GUILayout.Label(
                "五维由禀赋、专业能力、性格和当前健康实时派生，不单独存档。",
                _normalStyle);

            GUILayout.Space(12);
            GUILayout.Label("基础禀赋", _sectionStyle);
            GUILayout.Label(
                $"体质 {AbilityValue(player.Aptitudes.Constitution)}　" +
                $"力量 {AbilityValue(player.Aptitudes.Strength)}　" +
                $"灵巧 {AbilityValue(player.Aptitudes.Dexterity)}　" +
                $"感知 {AbilityValue(player.Aptitudes.Perception)}",
                _normalStyle);
            GUILayout.Label(
                $"记忆 {AbilityValue(player.Aptitudes.Memory)}　" +
                $"思辨 {AbilityValue(player.Aptitudes.Reasoning)}　" +
                $"意志 {AbilityValue(player.Aptitudes.Willpower)}　" +
                $"亲和 {AbilityValue(player.Aptitudes.Affinity)}",
                _normalStyle);

            GUILayout.Space(12);
            GUILayout.Label("专业能力", _sectionStyle);
            GUILayout.Label(
                $"军事 {AbilityValue(player.ProfessionalSkills.Military)}　" +
                $"武艺 {AbilityValue(player.ProfessionalSkills.MartialArts)}　" +
                $"政务 {AbilityValue(player.ProfessionalSkills.Administration)}　" +
                $"商业 {AbilityValue(player.ProfessionalSkills.Commerce)}　" +
                $"农业 {AbilityValue(player.ProfessionalSkills.Agriculture)}",
                _normalStyle);
            GUILayout.Label(
                $"工艺 {AbilityValue(player.ProfessionalSkills.Craft)}　" +
                $"医药 {AbilityValue(player.ProfessionalSkills.Medicine)}　" +
                $"学问 {AbilityValue(player.ProfessionalSkills.Scholarship)}　" +
                $"交涉 {AbilityValue(player.ProfessionalSkills.Negotiation)}　" +
                $"情报 {AbilityValue(player.ProfessionalSkills.Intelligence)}",
                _normalStyle);

            GUILayout.Space(12);
            DrawPlayerEducation(player);

            GUILayout.Space(12);
            GUILayout.Label("家庭", _sectionStyle);
            var hasFamily = false;
            for (var i = 0; i < _world.Families.Count; i++)
            {
                var family = _world.Families[i];
                if (!family.MemberIds.Contains(player.Id))
                {
                    continue;
                }

                hasFamily = true;
                GUILayout.Label(
                    $"{family.DisplayName}　家产{family.Wealth}　债务{family.Debt}　" +
                    $"成员{family.MemberIds.Count}人",
                    _normalStyle);
            }

            if (!hasFamily)
            {
                GUILayout.Label("尚未建立独立家户。", _normalStyle);
            }

            GUILayout.Space(12);
            GUILayout.Label("组织与职位", _sectionStyle);
            var hasMembership = false;
            for (var i = 0; i < _world.Memberships.Count; i++)
            {
                var membership = _world.Memberships[i];
                if (membership.PersonId != player.Id)
                {
                    continue;
                }

                hasMembership = true;
                GUILayout.Label(
                    $"{FindOrganization(membership.OrganizationId).DisplayName}　" +
                    $"{FindPositionName(membership.PositionId)}　忠诚" +
                    $"{membership.LoyaltyBasisPoints / 100f:F1}%",
                    _normalStyle);
            }

            if (!hasMembership)
            {
                GUILayout.Label("当前为在野人物。", _normalStyle);
            }
        }

        private void DrawPlayerEducation(PersonState player)
        {
            GUILayout.Label("培养", _sectionStyle);
            EducationPlanState currentPlan = null;
            for (var i = 0; i < _world.EducationPlans.Count; i++)
            {
                var candidate = _world.EducationPlans[i];
                if (candidate.StudentPersonId == player.Id &&
                    (candidate.Status == EducationPlanStatus.Active ||
                     candidate.Status == EducationPlanStatus.Suspended))
                {
                    currentPlan = candidate;
                    break;
                }
            }

            if (currentPlan != null)
            {
                var teacherName = string.IsNullOrEmpty(currentPlan.TeacherPersonId)
                    ? "自修"
                    : FindPerson(currentPlan.TeacherPersonId).DisplayName;
                var nextDay = currentPlan.LastResolvedDay < 0
                    ? currentPlan.CreatedDay + EducationSystem.DaysPerStudyMonth
                    : currentPlan.LastResolvedDay +
                      EducationSystem.DaysPerStudyMonth;
                GUILayout.Label(
                    $"{ProfessionalSkillAccess.DisplayName(currentPlan.Discipline)}　" +
                    $"教师：{teacherName}　每月{currentPlan.MonthlyStudyDays}日　" +
                    $"费用{currentPlan.MonthlyFee}钱　状态：{currentPlan.Status}",
                    _normalStyle);
                GUILayout.Label(
                    $"下次结算：第{nextDay + 1}日　累计学习" +
                    $"{currentPlan.TotalStudyDays}日　累计成长" +
                    $"{AbilityValue(currentPlan.TotalSkillGain)}",
                    _normalStyle);
                if (GUILayout.Button("取消当前学习计划", GUILayout.Height(32)))
                {
                    try
                    {
                        _educationSystem.CancelPlan(
                            _world, new StableId(currentPlan.Id));
                        _message = "已取消当前学习计划。";
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                    }
                }
            }
            else
            {
                _educationDiscipline = GUILayout.SelectionGrid(
                    _educationDiscipline,
                    new[]
                    {
                        "军事", "武艺", "政务", "商业", "农业",
                        "工艺", "医药", "学问", "交涉", "情报"
                    },
                    5,
                    GUILayout.Height(64));
                var discipline = (ProfessionalDiscipline)_educationDiscipline;
                var currentSkill = ProfessionalSkillAccess.Get(
                    player.ProfessionalSkills, discipline);
                var aptitude = ProfessionalSkillAccess.CompositeAptitude(
                    player.Aptitudes, discipline);
                var potential = ProfessionalSkillAccess.SoftPotential(
                    player.Aptitudes, discipline);
                _educationStudyDays = Mathf.RoundToInt(
                    GUILayout.HorizontalSlider(
                        _educationStudyDays,
                        1,
                        EducationSystem.MaximumStudyDaysPerMonth));
                GUILayout.Label(
                    $"每月学习：{_educationStudyDays}日　当前：" +
                    $"{AbilityValue(currentSkill)}　综合资质：" +
                    $"{AbilityValue(aptitude)}　软潜力：" +
                    $"{AbilityValue(potential)}",
                    _normalStyle);

                _educationUseTeacher = GUILayout.Toggle(
                    _educationUseTeacher, "自动寻找同地最佳教师");
                var teacher = _educationUseTeacher
                    ? _educationSystem.FindBestTeacher(
                        _world, player.Id, discipline)
                    : null;
                var practicePosition =
                    _educationSystem.FindCompatiblePracticePosition(
                        _world, player.Id, discipline);
                var family = FindFamilyForPerson(player.Id);
                GUI.enabled = family != null;
                _educationUseFamilyFunds = GUILayout.Toggle(
                    _educationUseFamilyFunds,
                    family == null
                        ? "当前没有可支付学费的家庭"
                        : $"由{family.DisplayName}支付");
                GUI.enabled = true;

                var teacherText = "自修或暂无合格教师";
                if (teacher != null)
                {
                    var teacherSkill = ProfessionalSkillAccess.Get(
                        teacher.ProfessionalSkills, discipline);
                    var monthlyFee = EducationSystem.RecommendedMonthlyFee(
                        teacherSkill, _educationStudyDays);
                    teacherText =
                        $"{teacher.DisplayName}，能力{AbilityValue(teacherSkill)}，" +
                        $"月费{monthlyFee}";
                }

                GUILayout.Label(
                    $"教师：{teacherText}　实践职位：" +
                    $"{(string.IsNullOrEmpty(practicePosition) ? "无" : FindPositionName(practicePosition))}",
                    _normalStyle);

                if (GUILayout.Button("建立学习计划", GUILayout.Height(34)))
                {
                    try
                    {
                        var plan = _educationSystem.StartPlan(
                            _world,
                            new StableId(player.Id),
                            discipline,
                            _educationStudyDays,
                            teacher == null ? string.Empty : teacher.Id,
                            _educationUseFamilyFunds && family != null
                                ? EducationFundingSource.Family
                                : EducationFundingSource.Personal,
                            _educationUseFamilyFunds && family != null
                                ? family.Id
                                : string.Empty,
                            practicePosition);
                        _message =
                            $"已建立{ProfessionalSkillAccess.DisplayName(discipline)}" +
                            $"学习计划，首次结算在第{plan.CreatedDay + 31}日。";
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                    }
                }
            }

            var shownRecords = 0;
            for (var i = _world.LearningRecords.Count - 1;
                 i >= 0 && shownRecords < 3;
                 i--)
            {
                var record = _world.LearningRecords[i];
                if (record.StudentPersonId != player.Id)
                {
                    continue;
                }

                GUILayout.Label(
                    $"第{record.Day + 1}日　{record.Summary}",
                    _normalStyle);
                shownRecords++;
            }
        }

        private static string AbilityValue(int basisPoints)
        {
            return (basisPoints / 100f).ToString("F1");
        }

        private static string LifeGoalName(LifeGoalKind goal)
        {
            switch (goal)
            {
                case LifeGoalKind.PreserveFamily:
                    return "保全家族";
                case LifeGoalKind.WinMerit:
                    return "建功立业";
                case LifeGoalKind.BuildFortune:
                    return "富甲一方";
                case LifeGoalKind.HealThePeople:
                    return "济世救人";
                case LifeGoalKind.PassOnCraft:
                    return "技艺传世";
                case LifeGoalKind.RestoreOrder:
                    return "匡扶秩序";
                case LifeGoalKind.SeekKnowledge:
                    return "求取学问";
                case LifeGoalKind.LiveInSeclusion:
                    return "隐居避世";
                case LifeGoalKind.UnifyRealm:
                    return "统一天下";
                default:
                    return "尚未形成";
            }
        }

        private void DrawPlayerTasks()
        {
            var player = FindPlayer();
            GUILayout.Label("当前人物任务", _sectionStyle);
            var activeTask = false;
            for (var i = 0; i < _world.Tasks.Count; i++)
            {
                var task = _world.Tasks[i];
                if (task.AssigneePersonId != player.Id)
                {
                    continue;
                }

                activeTask = task.Status == TaskStatus.Active || activeTask;
                var definition = FindTaskDefinition(task.DefinitionId);
                GUILayout.Label(
                    $"{definition.DisplayName}　状态：{task.Status}　" +
                    $"进度：{task.Progress}/{definition.RequiredProgress}　" +
                    $"截止：第{task.DeadlineDay + 1}日",
                    _normalStyle);
            }

            if (!activeTask)
            {
                GUILayout.Label("当前没有进行中的任务。", _normalStyle);
            }

            GUILayout.Space(12);
            GUILayout.Label("可以申请的任务", _sectionStyle);
            for (var i = 0; i < _world.TaskDefinitions.Count; i++)
            {
                var definition = _world.TaskDefinitions[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"{definition.DisplayName}　起点：" +
                    $"{FindLocationName(definition.OriginLocationId)}　" +
                    $"期限{definition.DurationDays}天　奖励{definition.RewardMoney}钱",
                    _normalStyle);
                GUI.enabled = definition.IsAvailable && !activeTask;
                if (GUILayout.Button("申请", GUILayout.Width(90)))
                {
                    var result = _taskSystem.TryAccept(
                        _world,
                        new StableId(player.Id),
                        new StableId(definition.Id));
                    _message = result.Message;
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawPlayerActions()
        {
            var player = FindPlayer();
            GUILayout.Label("当前可做的事", _sectionStyle);
            GUILayout.Label(
                "行动来自人物当前地点、身份、组织、资产、任务和健康状态；" +
                "灰色行动会说明缺少的前置条件。",
                _normalStyle);
            GUILayout.Space(8);

            var actions = _playerActionService.QueryActions(
                _world, player.Id);
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(action.DisplayName, _sectionStyle);
                GUILayout.Label(action.Description, _normalStyle);
                if (!string.IsNullOrWhiteSpace(action.Motivation))
                {
                    GUILayout.Label("缘由：" + action.Motivation, _normalStyle);
                }
                if (!string.IsNullOrWhiteSpace(action.ExpectedOutcome))
                {
                    GUILayout.Label("预期：" + action.ExpectedOutcome, _normalStyle);
                }
                if (!string.IsNullOrWhiteSpace(action.Cost))
                {
                    GUILayout.Label("代价：" + action.Cost, _normalStyle);
                }
                if (!string.IsNullOrWhiteSpace(action.KnownRisk))
                {
                    GUILayout.Label("已知风险：" + action.KnownRisk, _normalStyle);
                }
                if (!action.IsAvailable)
                {
                    GUILayout.Label(
                        "暂不可用：" + action.UnavailableReason,
                        _normalStyle);
                }
                GUI.enabled = action.IsAvailable && !_actionPresentation.IsActive;
                if (GUILayout.Button("执行", GUILayout.Height(32)))
                {
                    try
                    {
                        var result = _playerActionService.Execute(
                            _world, player.Id, action.Id);
                        _message = PlayerActionSummary(result);
                        _actionPresentation.Begin(
                            result.ResultId,
                            result.PresentationCue,
                            _message,
                            Time.realtimeSinceStartup);
                        _actionLog.Insert(
                            0,
                            $"第{_world.AbsoluteDay + 1}日　" + _message);
                        RefreshMonthlyDecisions();
                    }
                    catch (Exception exception)
                    {
                        _message = exception.Message;
                    }
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.Space(8);
            GUILayout.Label("最近行动", _sectionStyle);
            if (_actionLog.Count == 0)
            {
                GUILayout.Label("尚未执行玩家行动。", _normalStyle);
            }
            for (var i = 0; i < Math.Min(8, _actionLog.Count); i++)
            {
                GUILayout.Label(_actionLog[i], _normalStyle);
            }
        }

        private static string PlayerActionSummary(PlayerActionResult result)
        {
            if (!result.Success)
            {
                return result.Summary;
            }

            var changes = string.Empty;
            if (result.DaysAdvanced != 0)
            {
                changes += $"　耗时{result.DaysAdvanced}天";
            }
            if (result.MoneyChange != 0)
            {
                changes += $"　钱{Signed(result.MoneyChange)}";
            }
            if (result.ProvisionChange != 0)
            {
                changes += $"　口粮{Signed(result.ProvisionChange)}";
            }
            if (result.HealthChange != 0)
            {
                changes += $"　健康{Signed(result.HealthChange)}";
            }
            return result.Summary + changes;
        }

        private static string Signed(long value) =>
            value > 0 ? "+" + value : value.ToString();

        private void DrawTrackedMerchantGoal()
        {
            if (_playerActionService == null || _world == null)
            {
                return;
            }

            var goal = _playerActionService.InspectMerchantGoal(
                _world, _world.PlayerPersonId);
            if (!goal.IsAvailable)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("当前目标：" + goal.DisplayName, _sectionStyle);
            GUILayout.Label(goal.CurrentObjective, _normalStyle);
            GUILayout.Label("追踪：" + goal.TrackedObjective, _normalStyle);
            GUILayout.Label("家中：" + goal.FamilySituation, _normalStyle);
            if (goal.MarketOpportunity != null)
            {
                var intel = goal.MarketOpportunity;
                GUILayout.Label(
                    $"行情来源：{intel.SourceName}（第{intel.LearnedDay + 1}日，可靠度{intel.ReliabilityBasisPoints / 100f:0}%）",
                    _normalStyle);
                GUILayout.Label(
                    $"估价：中山{intel.ExpectedOriginUnitPrice} / 涿县{intel.ExpectedTargetUnitPrice}；预计毛差{intel.ExpectedGrossMargin}，路程约{intel.EstimatedTravelDays}日，口粮约{intel.EstimatedProvisionCost}。",
                    _normalStyle);
            }
            if (!string.IsNullOrWhiteSpace(goal.LatestImportantResult))
            {
                GUILayout.Label("记忆：" + goal.LatestImportantResult, _normalStyle);
            }
            GUILayout.EndVertical();
        }

        private void DrawActionPresentation()
        {
            if (!_actionPresentation.IsActive)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("行动结果", _sectionStyle);
            GUILayout.Label(_actionPresentation.Summary, _normalStyle);
            GUILayout.HorizontalSlider(
                _actionPresentation.Progress(Time.realtimeSinceStartup),
                0f,
                1f);
            if (GUILayout.Button("跳过表现（结果已经结算）", GUILayout.Height(26)))
            {
                _actionPresentation.Skip();
            }
            GUILayout.EndVertical();
        }

        private void DrawDeveloperDashboard()
        {
            GUILayout.Label("世界模拟观察台", _sectionStyle);
            GUILayout.Label(
                "开发调试界面：完整展示时间、人物、市场、AI、旅行、战争和医疗状态。",
                _normalStyle);
            DrawToolbar();
            DrawWorldSummary();
            DrawPopulationLedger();
            DrawHistoricalTimeline();
            DrawLocations();
            DrawMarkets();
            DrawMerchantTrading();
            DrawMilitarySupply();
            DrawWar();
            DrawMedicalCare();
            DrawPeople();
            DrawFamilies();
            DrawLifeEvents();
            DrawRelationships();
            DrawOrganizations();
            DrawTasks();
            DrawJourneys();
            DrawActionLog();
        }

        private void AdvancePlayerDays(int days)
        {
            _simulator.AdvanceDays(_world, days);
            RefreshMonthlyDecisions();
            _message = $"世界推进了{days}天。";
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重置184年世界", GUILayout.Height(32)))
            {
                InitializeWorld();
            }

            if (GUILayout.Button("推进一天", GUILayout.Height(32)))
            {
                _simulator.AdvanceDays(_world, 1);
                RefreshMonthlyDecisions();
                _message = "世界推进了一天。";
            }

            if (GUILayout.Button("推进一月（30天）", GUILayout.Height(32)))
            {
                _simulator.AdvanceDays(_world, 30);
                RefreshMonthlyDecisions();
                _message = "世界推进了30天，NPC重新选择月度重点。";
            }

            if (GUILayout.Button("结算NPC本月行动", GUILayout.Height(32)))
            {
                ResolveMonthlyNpcActions();
            }

            if (GUILayout.Button("刘备徒步前往中山", GUILayout.Height(32)))
            {
                TryStartLiuBeiJourney();
            }

            if (GUILayout.Button("内存存档", GUILayout.Height(32)))
            {
                _snapshot = WorldSnapshotSerializer.Serialize(_world);
                _message = $"已生成内存快照，共{_snapshot.Length}个字符。";
            }

            GUI.enabled = !string.IsNullOrEmpty(_snapshot);
            if (GUILayout.Button("读取内存存档", GUILayout.Height(32)))
            {
                _world = WorldSnapshotSerializer.Deserialize(_snapshot);
                RebindServices();
                RefreshMonthlyDecisions();
                _message = "已恢复内存快照。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawPopulationLedger()
        {
            GUILayout.Space(10);
            GUILayout.Label("人口总账与守恒审计", _sectionStyle);
            if (!_world.PopulationLedgerInitialized)
            {
                GUILayout.Label("当前世界尚未初始化人口总账。", _normalStyle);
                return;
            }

            var audit = _populationLedgerSystem.Audit(_world);
            GUILayout.Label(
                $"期初人口：{audit.OpeningPopulation}　" +
                $"应有人口：{audit.ExpectedPopulation}　" +
                $"实际人口：{audit.ActualPopulation}　" +
                $"审计：{(audit.IsBalanced ? "平衡" : "不平衡")}",
                _normalStyle);
            GUILayout.Label(
                $"统计人口：{audit.AbstractPopulation}　" +
                $"独立人物：{audit.IndependentPopulation}　" +
                $"出生：{audit.Births}　死亡：{audit.Deaths}　" +
                $"事务：{_world.PopulationTransactions.Count}",
                _normalStyle);

            for (var locationIndex = 0;
                 locationIndex < _world.Locations.Count;
                 locationIndex++)
            {
                var location = _world.Locations[locationIndex];
                var summary = string.Empty;
                for (var occupation = PopulationOccupation.Agriculture;
                     occupation <= PopulationOccupation.Dependent;
                     occupation++)
                {
                    var population = 0;
                    for (var cohortIndex = 0;
                         cohortIndex < _world.PopulationCohorts.Count;
                         cohortIndex++)
                    {
                        var cohort = _world.PopulationCohorts[cohortIndex];
                        if (cohort.LocationId == location.Id &&
                            cohort.Occupation == occupation)
                        {
                            population += cohort.Population;
                        }
                    }

                    if (summary.Length > 0)
                    {
                        summary += "　";
                    }

                    summary +=
                        $"{PopulationLedgerSystem.OccupationName(occupation)}" +
                        $"{population}";
                }

                GUILayout.Label(
                    $"{location.DisplayName}　总人口{location.Population}　{summary}",
                    _normalStyle);
            }

            var migrationCohort = FindPopulationCohort(
                "location.zhuo",
                PopulationOccupation.Agriculture);
            GUI.enabled =
                migrationCohort != null &&
                migrationCohort.Population >= 100;
            if (GUILayout.Button(
                    "调试：100名涿县农业人口迁往广宗",
                    GUILayout.Height(32)))
            {
                try
                {
                    _populationLedgerSystem.TransferCohort(
                        _world,
                        new StableId(migrationCohort.Id),
                        new StableId("location.guangzong"),
                        100);
                    _message = "100名农业人口已经迁往广宗，世界总人口保持不变。";
                }
                catch (Exception exception)
                {
                    _message = exception.Message;
                }
            }

            GUI.enabled = true;
            var first = Math.Max(
                0,
                _world.PopulationTransactions.Count - 8);
            for (var i = _world.PopulationTransactions.Count - 1;
                 i >= first;
                 i--)
            {
                var transaction = _world.PopulationTransactions[i];
                GUILayout.Label(
                    $"第{transaction.Day}日　{transaction.Type}　" +
                    $"{transaction.Summary}",
                    _normalStyle);
            }

            for (var i = 0; i < audit.LocationMismatches.Count; i++)
            {
                GUILayout.Label(
                    "异常：" + audit.LocationMismatches[i],
                    _normalStyle);
            }
        }

        private PopulationCohortState FindPopulationCohort(
            string locationId,
            PopulationOccupation occupation)
        {
            for (var i = 0; i < _world.PopulationCohorts.Count; i++)
            {
                var cohort = _world.PopulationCohorts[i];
                if (cohort.LocationId == locationId &&
                    cohort.Occupation == occupation)
                {
                    return cohort;
                }
            }

            return null;
        }

        private void DrawWorldSummary()
        {
            GUILayout.Label("世界状态", _sectionStyle);
            GUILayout.Label(
                $"模拟日：{_world.AbsoluteDay}　时段：{(DaySegment)_world.Segment}　" +
                $"种子：{_world.MasterSeed}　修订号：{_world.Revision}",
                _normalStyle);
            GUILayout.Label(
                $"地点：{_world.Locations.Count}　道路：{_world.Routes.Count}　" +
                $"人物：{_world.People.Count}　家庭：{_world.Families.Count}",
                _normalStyle);
        }

        private void DrawLocations()
        {
            GUILayout.Space(10);
            GUILayout.Label("地点与市场", _sectionStyle);
            for (var i = 0; i < _world.Locations.Count; i++)
            {
                var location = _world.Locations[i];
                GUILayout.Label(
                    $"{location.DisplayName,-10}　人口 {location.Population,6}　" +
                    $"粮价 {location.GrainPrice,4}　治安 {location.PublicOrderBasisPoints / 100f:F1}%",
                    _normalStyle);
            }
        }

        private void DrawHistoricalTimeline()
        {
            GUILayout.Space(10);
            GUILayout.Label("184年历史时间线", _sectionStyle);
            for (var i = 0; i < _world.HistoricalEventDefinitions.Count; i++)
            {
                var definition = _world.HistoricalEventDefinitions[i];
                var anchor = FindHistoricalAnchor(definition.Id);
                var status = anchor == null
                    ? HistoricalAnchorStatus.Dormant.ToString()
                    : anchor.Status.ToString();
                var result = anchor == null || string.IsNullOrEmpty(anchor.ActualOutcome)
                    ? string.Empty
                    : $"　结果：{anchor.ActualOutcome}";
                GUILayout.Label(
                    $"{definition.DisplayName}　时间窗：{definition.EarliestDay}—" +
                    $"{definition.LatestDay}日　状态：{status}{result}",
                    _normalStyle);
            }
        }

        private void DrawPeople()
        {
            GUILayout.Space(10);
            GUILayout.Label("人物与沙盒AI", _sectionStyle);
            for (var i = 0; i < _world.People.Count; i++)
            {
                var person = _world.People[i];
                var locationName = FindLocationName(person.LocationId);
                var focus = _decisions.TryGetValue(person.Id, out var decision)
                    ? FocusName(decision.SelectedFocus)
                    : "未决定";
                var journey = FindJourney(person.Id);
                var travelText = journey == null
                    ? string.Empty
                    : $"　旅途中→{FindLocationName(journey.DestinationLocationId)} " +
                      $"剩余{journey.RemainingKilometers}公里";

                GUILayout.Label(
                    $"{person.DisplayName,-6}　所在地：{locationName,-8}　" +
                    $"钱：{person.Wealth,4}　口粮：{person.Provisions,2}　" +
                    $"货物：{CargoSummary(person.Id)}　本月重点：{focus}{travelText}",
                    _normalStyle);
            }
        }

        private void DrawMarkets()
        {
            GUILayout.Space(10);
            GUILayout.Label("多商品市场", _sectionStyle);
            for (var locationIndex = 0;
                 locationIndex < _world.Locations.Count;
                 locationIndex++)
            {
                var location = _world.Locations[locationIndex];
                var summary = string.Empty;
                for (var listingIndex = 0;
                     listingIndex < _world.MarketListings.Count;
                     listingIndex++)
                {
                    var listing = _world.MarketListings[listingIndex];
                    if (listing.LocationId != location.Id)
                    {
                        continue;
                    }

                    if (summary.Length > 0)
                    {
                        summary += "　";
                    }

                    summary +=
                        $"{FindCommodityName(listing.CommodityId)} " +
                        $"{listing.Price}钱/存{listing.Stock}";
                }

                GUILayout.Label($"{location.DisplayName}　{summary}", _normalStyle);
            }
        }

        private void DrawMerchantTrading()
        {
            GUILayout.Space(10);
            GUILayout.Label("行商演示：张世平的中山—涿县商路", _sectionStyle);
            var merchant = FindPerson("person.zhang_shiping");
            var journey = FindJourney(merchant.Id);
            var horseQuantity = _tradingSystem.GetQuantity(
                _world, merchant.Id, "commodity.horses");
            GUILayout.Label(
                $"所在地：{FindLocationName(merchant.LocationId)}　钱：{merchant.Wealth}　" +
                $"携带战马：{horseQuantity}匹　" +
                (journey == null ? "当前可交易" : "商队行进中"),
                _normalStyle);

            GUILayout.BeginHorizontal();
            GUI.enabled =
                journey == null &&
                merchant.LocationId == "location.zhongshan";
            if (GUILayout.Button("在中山买入2匹战马", GUILayout.Width(180)))
            {
                var result = _tradingSystem.Buy(
                    _world,
                    new StableId(merchant.Id),
                    new StableId("commodity.horses"),
                    2);
                _message = result.Message;
            }

            if (GUILayout.Button("商队前往涿县", GUILayout.Width(150)))
            {
                _travelSystem.StartJourney(
                    _world,
                    new StableId(merchant.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhuo"),
                    TravelMode.Caravan);
                _message = "张世平商队已从中山出发，约6日到达涿县。";
            }

            GUI.enabled =
                journey == null &&
                merchant.LocationId == "location.zhuo" &&
                horseQuantity > 0;
            if (GUILayout.Button("在涿县卖出全部战马", GUILayout.Width(190)))
            {
                var result = _tradingSystem.Sell(
                    _world,
                    new StableId(merchant.Id),
                    new StableId("commodity.horses"),
                    horseQuantity);
                _message = result.Message;
            }

            GUI.enabled =
                journey == null &&
                merchant.LocationId == "location.zhuo";
            if (GUILayout.Button("商队返回中山", GUILayout.Width(150)))
            {
                _travelSystem.StartJourney(
                    _world,
                    new StableId(merchant.Id),
                    new StableId("route.zhuo_zhongshan"),
                    new StableId("location.zhongshan"),
                    TravelMode.Caravan);
                _message = "张世平商队已返回中山。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawJourneys()
        {
            GUILayout.Space(10);
            GUILayout.Label("当前旅行", _sectionStyle);
            if (_world.Journeys.Count == 0)
            {
                GUILayout.Label("目前没有人物在路上。", _normalStyle);
                return;
            }

            for (var i = 0; i < _world.Journeys.Count; i++)
            {
                var journey = _world.Journeys[i];
                GUILayout.Label(
                    $"{FindPersonName(journey.PersonId)}：{FindLocationName(journey.OriginLocationId)}" +
                    $" → {FindLocationName(journey.DestinationLocationId)}，" +
                    $"剩余{journey.RemainingKilometers}公里，方式：{journey.Mode}",
                    _normalStyle);
            }
        }

        private void DrawFamilies()
        {
            GUILayout.Space(10);
            GUILayout.Label("家庭与传承", _sectionStyle);
            for (var i = 0; i < _world.Families.Count; i++)
            {
                var family = _world.Families[i];
                var livingMembers = 0;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    if (FindPerson(family.MemberIds[memberIndex]).IsAlive)
                    {
                        livingMembers++;
                    }
                }

                GUILayout.Label(
                    $"{family.DisplayName}　家主：{FindPersonName(family.HeadPersonId)}　" +
                    $"在世成员：{livingMembers}/{family.MemberIds.Count}　" +
                    $"家产：{family.Wealth}钱　债务：{family.Debt}钱",
                    _normalStyle);
            }
        }

        private void DrawWar()
        {
            GUILayout.Space(10);
            GUILayout.Label("战争层：军队、行军与野战", _sectionStyle);
            if (_world.MilitaryServiceInitialized)
            {
                GUILayout.Label(
                    $"真实服役：{_world.MilitaryServices.Count}人　" +
                    $"编制：{_world.MilitaryFormations.Count}个　" +
                    $"军令记录：{_world.MilitaryOrders.Count}条",
                    _normalStyle);
            }
            else
            {
                GUILayout.Label(
                    "当前为旧存档抽象兵力模式，尚未建立真实服役账。",
                    _normalStyle);
            }

            for (var i = 0; i < _world.Armies.Count; i++)
            {
                var army = _world.Armies[i];
                var march = FindArmyMarch(army.Id);
                var state = !army.IsMobilized
                    ? "未动员/已溃散"
                    : march == null
                        ? "驻扎"
                        : $"行军至{FindLocationName(march.DestinationLocationId)}，" +
                          $"余{march.RemainingKilometers}公里";
                GUILayout.Label(
                    $"{army.DisplayName}　主将：{FindPersonName(army.CommanderPersonId)}　" +
                    $"所在地：{FindLocationName(army.LocationId)}　兵力：{army.Troops}　" +
                    $"伤兵：{army.WoundedTroops}　" +
                    $"士气：{army.MoraleBasisPoints / 100f:F1}%　" +
                    $"训练：{army.TrainingBasisPoints / 100f:F1}%　" +
                    $"军粮：{army.Provisions}　{state}",
                    _normalStyle);
                if (_world.MilitaryServiceInitialized)
                {
                    var audit = _militaryServiceSystem.AuditArmy(
                        _world, new StableId(army.Id));
                    var playerAuthority = string.IsNullOrEmpty(
                        _world.PlayerPersonId)
                        ? MilitaryAuthorityLevel.None
                        : _militaryAuthoritySystem.GetAuthority(
                            _world,
                            new StableId(_world.PlayerPersonId),
                            new StableId(army.Id));
                    GUILayout.Label(
                        $"　服役审计：可战{audit.Available}、伤{audit.Wounded}、" +
                        $"掉队{audit.Stragglers}、逃亡{audit.Deserters}、" +
                        $"被俘{audit.Captured}、退役{audit.Retired}、死亡{audit.Dead}；" +
                        $"玩家权限：{playerAuthority}",
                        _normalStyle);
                    if (_world.MilitaryEquipmentInitialized)
                    {
                        var readiness =
                            _militaryEquipmentSystem.BuildReadinessReport(
                                _world, army.Id);
                        var equipmentAudit =
                            _militaryEquipmentSystem.AuditArmy(_world, army.Id);
                        GUILayout.Label(
                            $"　军械：战备{readiness.ReadinessBasisPoints / 100f:F1}%　" +
                            $"库存{equipmentAudit.Available}、在用{equipmentAudit.Issued}、" +
                            $"损坏{equipmentAudit.Damaged}　" +
                            $"弓手{TroopCount(readiness, MilitaryEquipmentSystem.ArcherTroopId)}、" +
                            $"矛兵{TroopCount(readiness, MilitaryEquipmentSystem.SpearTroopId)}、" +
                            $"刀盾{TroopCount(readiness, MilitaryEquipmentSystem.SwordShieldTroopId)}、" +
                            $"轻兵{TroopCount(readiness, MilitaryEquipmentSystem.LightInfantryTroopId)}、" +
                            $"徒手{TroopCount(readiness, MilitaryEquipmentSystem.UnarmedTroopId)}　" +
                            $"账实：{(equipmentAudit.IsBalanced ? "平衡" : "异常")}",
                            _normalStyle);
                        GUILayout.Label(
                            $"　主将携行：{EquipmentLoadout(army.CommanderPersonId)}",
                            _normalStyle);
                    }

                    for (var formationIndex = 0;
                         formationIndex < _world.MilitaryFormations.Count;
                         formationIndex++)
                    {
                        var formation = _world.MilitaryFormations[formationIndex];
                        if (formation.ArmyId != army.Id)
                        {
                            continue;
                        }

                        var formationEquipment = _world.MilitaryEquipmentInitialized
                            ? _militaryEquipmentSystem.BuildReadinessReport(
                                _world, army.Id, formation.Id)
                            : null;
                        GUILayout.Label(
                            $"{(string.IsNullOrEmpty(formation.ParentFormationId) ? "　" : "　　↳ ")}" +
                            $"{formation.DisplayName} [{formation.Kind}]　" +
                            $"指挥：{FindPersonName(formation.CommanderPersonId)}　" +
                            $"额定：{formation.AuthorizedStrength}" +
                            (formationEquipment == null
                                ? string.Empty
                                : $"　战备：{formationEquipment.ReadinessBasisPoints / 100f:F1}%"),
                            _normalStyle);
                    }
                }
            }

            var han = FindArmy("army.han_jizhou_vanguard");
            var yellow = FindArmy("army.yellow_turban_guangzong");
            GUILayout.BeginHorizontal();
            GUI.enabled =
                yellow.IsMobilized &&
                han.IsMobilized &&
                han.LocationId == "location.xiaquyang" &&
                FindArmyMarch(han.Id) == null;
            if (GUILayout.Button("官军从下曲阳进军广宗", GUILayout.Width(210)))
            {
                _armySystem.StartMarch(
                    _world,
                    new StableId(han.CommanderPersonId),
                    new StableId(han.Id),
                    new StableId("route.xiaquyang_guangzong"),
                    new StableId("location.guangzong"));
                _message = "冀州官军开始进军广宗，预计8日抵达。";
            }

            GUI.enabled = FindArmyMarch(han.Id) != null;
            if (GUILayout.Button("推进战争8日", GUILayout.Width(140)))
            {
                _simulator.AdvanceDays(_world, 8);
                RefreshMonthlyDecisions();
                _message = "战争时间推进8日。";
            }

            GUI.enabled =
                han.IsMobilized &&
                yellow.IsMobilized &&
                han.LocationId == yellow.LocationId &&
                FindArmyMarch(han.Id) == null &&
                FindArmyMarch(yellow.Id) == null;
            if (GUILayout.Button("结算广宗野战", GUILayout.Width(160)))
            {
                var outcome = _battleResolver.Resolve(
                    _world,
                    new StableId(han.CommanderPersonId),
                    new StableId(han.Id),
                    new StableId(yellow.Id));
                _message = outcome.Summary;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            var first = Math.Max(0, _world.Battles.Count - 5);
            for (var i = _world.Battles.Count - 1; i >= first; i--)
            {
                GUILayout.Label(
                    $"第{_world.Battles[i].Day}日　{_world.Battles[i].Summary}",
                    _normalStyle);
            }

            if (_world.MilitaryEquipmentInitialized &&
                _world.MilitaryEquipmentTransactions.Count > 0)
            {
                GUILayout.Label("近期军械流水", _sectionStyle);
                var firstEquipmentTransaction = Math.Max(
                    0, _world.MilitaryEquipmentTransactions.Count - 5);
                for (var i = _world.MilitaryEquipmentTransactions.Count - 1;
                     i >= firstEquipmentTransaction;
                     i--)
                {
                    var transaction = _world.MilitaryEquipmentTransactions[i];
                    GUILayout.Label(
                        $"第{transaction.Day}日　{transaction.Type}　" +
                        $"{FindEquipmentName(transaction.EquipmentDefinitionId)}×" +
                        $"{transaction.Quantity}　" +
                        $"{transaction.FromArmyId} → {transaction.ToArmyId}",
                        _normalStyle);
                }
            }

            if (_world.MilitaryServiceInitialized &&
                _world.MilitaryOrders.Count > 0)
            {
                GUILayout.Label("近期军令", _sectionStyle);
                var firstOrder = Math.Max(
                    0, _world.MilitaryOrders.Count - 5);
                for (var i = _world.MilitaryOrders.Count - 1;
                     i >= firstOrder;
                     i--)
                {
                    var order = _world.MilitaryOrders[i];
                    GUILayout.Label(
                        $"第{order.Day}日　{FindPersonName(order.IssuerPersonId)}　" +
                        $"{order.Type}　{order.Result}　" +
                        $"权限{order.ActualAuthority}/{order.RequiredAuthority}",
                        _normalStyle);
                }
            }
        }

        private void DrawMedicalCare()
        {
            GUILayout.Space(10);
            GUILayout.Label("医者路线：购药与军中救治", _sectionStyle);
            var physician = FindPerson("person.generated.physician_001");
            var han = FindArmy("army.han_jizhou_vanguard");
            var yellow = FindArmy("army.yellow_turban_guangzong");
            var herbQuantity = _tradingSystem.GetQuantity(
                _world, physician.Id, "commodity.herbs");
            GUILayout.Label(
                $"{physician.DisplayName}　医药：" +
                $"{AbilityValue(physician.ProfessionalSkills.Medicine)}　" +
                $"所在：{FindLocationName(physician.LocationId)}　药材：{herbQuantity}单位　" +
                $"官军伤兵：{han.WoundedTroops}　黄巾伤兵：{yellow.WoundedTroops}",
                _normalStyle);

            GUILayout.BeginHorizontal();
            GUI.enabled =
                FindJourney(physician.Id) == null &&
                physician.LocationId == "location.guangzong";
            if (GUILayout.Button("陈医师购买5单位药材", GUILayout.Width(190)))
            {
                var result = _tradingSystem.Buy(
                    _world,
                    new StableId(physician.Id),
                    new StableId("commodity.herbs"),
                    5);
                _message = result.Message;
            }

            GUI.enabled =
                herbQuantity > 0 &&
                han.WoundedTroops > 0 &&
                physician.LocationId == han.LocationId &&
                FindArmyMarch(han.Id) == null;
            if (GUILayout.Button("救治官军伤兵", GUILayout.Width(150)))
            {
                var result = _medicalSystem.TreatArmyWounded(
                    _world,
                    new StableId(physician.Id),
                    new StableId(han.Id),
                    herbQuantity * MedicalSystem.PatientsPerHerbUnit);
                _message = result.Message;
            }

            GUI.enabled =
                herbQuantity > 0 &&
                yellow.WoundedTroops > 0 &&
                physician.LocationId == yellow.LocationId &&
                FindArmyMarch(yellow.Id) == null;
            if (GUILayout.Button("救治黄巾伤兵", GUILayout.Width(150)))
            {
                var result = _medicalSystem.TreatArmyWounded(
                    _world,
                    new StableId(physician.Id),
                    new StableId(yellow.Id),
                    herbQuantity * MedicalSystem.PatientsPerHerbUnit);
                _message = result.Message;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            var first = Math.Max(0, _world.MedicalTreatments.Count - 5);
            for (var i = _world.MedicalTreatments.Count - 1; i >= first; i--)
            {
                GUILayout.Label(
                    $"第{_world.MedicalTreatments[i].Day}日　" +
                    _world.MedicalTreatments[i].Summary,
                    _normalStyle);
            }
        }

        private void DrawMilitarySupply()
        {
            GUILayout.Space(10);
            GUILayout.Label("军需联动：商人、市场、任务与军队", _sectionStyle);
            var merchant = FindPerson("person.zhang_shiping");
            var army = FindArmy("army.youzhou_reinforcement");
            var organization = FindOrganization(army.OrganizationId);
            var grainQuantity = _tradingSystem.GetQuantity(
                _world, merchant.Id, "commodity.grain");
            GUILayout.Label(
                $"幽州援军军粮：{army.Provisions}　军府资金：{organization.Treasury}钱　" +
                $"张世平携粮：{grainQuantity}单位",
                _normalStyle);

            GUILayout.BeginHorizontal();
            GUI.enabled =
                FindJourney(merchant.Id) == null &&
                merchant.LocationId == "location.zhongshan";
            if (GUILayout.Button("张世平在中山买10单位粮食", GUILayout.Width(220)))
            {
                var result = _tradingSystem.Buy(
                    _world,
                    new StableId(merchant.Id),
                    new StableId("commodity.grain"),
                    10);
                _message = result.Message;
            }

            GUI.enabled =
                grainQuantity > 0 &&
                FindJourney(merchant.Id) == null &&
                merchant.LocationId == army.LocationId;
            if (GUILayout.Button("将携粮售给幽州援军", GUILayout.Width(190)))
            {
                var result = _militarySupplySystem.SellGrainToArmy(
                    _world,
                    new StableId(merchant.Id),
                    new StableId(army.Id),
                    grainQuantity);
                _message = result.Message;
            }

            GUI.enabled =
                FindArmyMarch(army.Id) == null &&
                army.IsMobilized;
            if (GUILayout.Button("援军从当地市场购粮20单位", GUILayout.Width(220)))
            {
                var result = _militarySupplySystem.PurchaseLocalGrain(
                    _world,
                    new StableId(army.Id),
                    20);
                _message = result.Message;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            var equipmentBatch = _world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                CoreProductionContent.LongSpearProductId);
            var workshopSpearBatch = _world.ProductBatches.Find(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.LongSpearProductId &&
                item.InventoryContainerId ==
                    MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId);
            var manufacturingOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.ForgeLongSpearRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            var spearStock = _world.MilitaryArmoryStocks.Find(item =>
                item.ArmyId == army.Id &&
                item.EquipmentDefinitionId ==
                MilitaryEquipmentSystem.LongSpearId);
            GUILayout.Label(
                $"军械采购原型：商号长矛{equipmentBatch?.Quantity ?? 0}件，" +
                $"援军库内可用长矛{spearStock?.AvailableQuantity ?? 0}件",
                _normalStyle);
            GUILayout.Label(
                $"中山军械工坊：长矛成品{workshopSpearBatch?.Quantity ?? 0}件，" +
                $"制造订单{manufacturingOrder?.Status.ToString() ?? "无"}，" +
                $"维修订单{_world.MilitaryEquipmentRepairOrders.Count}笔",
                _normalStyle);
            var ironResource = _world.ResourceBodies.Find(item =>
                item.Id == UpstreamResourceProductionSystem.PrototypeIronBodyId);
            var forestResource = _world.ResourceBodies.Find(item =>
                item.Id == UpstreamResourceProductionSystem.PrototypeForestBodyId);
            var ironExtraction = _world.ResourceExtractionOrders.Find(item =>
                item.ResourceBodyId ==
                    UpstreamResourceProductionSystem.PrototypeIronBodyId &&
                item.Status == ProductionOrderStatus.Active);
            var timberExtraction = _world.ResourceExtractionOrders.Find(item =>
                item.ResourceBodyId ==
                    UpstreamResourceProductionSystem.PrototypeForestBodyId &&
                item.Status == ProductionOrderStatus.Active);
            var charcoalOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.BurnCharcoalRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            var smeltOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.SmeltBloomeryIronRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            GUILayout.Label(
                $"真实上游：铁矿余量{ironResource?.RemainingQuantity ?? 0}" +
                $"（预留{ironResource?.ReservedQuantity ?? 0}），" +
                $"林木余量{forestResource?.RemainingQuantity ?? 0}" +
                $"（预留{forestResource?.ReservedQuantity ?? 0}）",
                _normalStyle);
            GUILayout.Label(
                $"工坊仓：矿石{AvailableProductQuantity(CoreProductionContent.IronOreProductId)}，" +
                $"木料{AvailableProductQuantity(CoreProductionContent.TimberMaterialProductId)}，" +
                $"木炭{AvailableProductQuantity(CoreProductionContent.CharcoalProductId)}，" +
                $"铁料{AvailableProductQuantity(CoreProductionContent.IronMaterialProductId)}",
                _normalStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = ironExtraction == null &&
                FindPerson("person.su_shuang").LocationId ==
                    "location.zhongshan";
            if (GUILayout.Button("采铁矿12单位", GUILayout.Width(150)))
            {
                ironExtraction = _upstreamResourceProductionSystem.CreateOrder(
                    _world,
                    UpstreamResourceProductionSystem.PrototypeIronBodyId,
                    UpstreamResourceProductionSystem.PrototypeIronMineSiteId,
                    "person.su_shuang",
                    new[] { "person.su_shuang" },
                    ProductionControlMode.WorkOrder,
                    12);
                _message = $"采矿订单{ironExtraction.Id}已预留矿体储量。";
            }

            GUI.enabled = timberExtraction == null &&
                FindPerson("person.zhang_shiping").LocationId ==
                    "location.zhongshan";
            if (GUILayout.Button("伐木20单位", GUILayout.Width(150)))
            {
                timberExtraction =
                    _upstreamResourceProductionSystem.CreateOrder(
                        _world,
                        UpstreamResourceProductionSystem.PrototypeForestBodyId,
                        UpstreamResourceProductionSystem.PrototypeLoggingSiteId,
                        "person.zhang_shiping",
                        new[] { "person.zhang_shiping" },
                        ProductionControlMode.WorkOrder,
                        20);
                _message = $"伐木订单{timberExtraction.Id}已预留林木储量。";
            }

            GUI.enabled = ironExtraction != null || timberExtraction != null;
            if (GUILayout.Button("推进采集完成", GUILayout.Width(160)))
            {
                var finishDay = Math.Max(
                    ironExtraction?.FinishDay ?? _world.AbsoluteDay,
                    timberExtraction?.FinishDay ?? _world.AbsoluteDay);
                _simulator.AdvanceDays(
                    _world,
                    Math.Max(1, (int)(finishDay - _world.AbsoluteDay)));
                _message = "已按世界时间推进并结算资源采集。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            var fodderExtraction = _world.ResourceExtractionOrders.Find(item =>
                item.ResourceBodyId == UpstreamResourceProductionSystem
                    .PrototypePastureForageBodyId &&
                item.Status == ProductionOrderStatus.Active);
            var barkExtraction = _world.ResourceExtractionOrders.Find(item =>
                item.ResourceBodyId == UpstreamResourceProductionSystem
                    .PrototypeTanningBarkBodyId &&
                item.Status == ProductionOrderStatus.Active);
            var husbandryOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.BreedSheepRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            var slaughterOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.SlaughterSheepRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            var tanningOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.VegetableTanHideRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            var hornOrder = _world.ProcessingWorkOrders.Find(item =>
                item.RecipeDefinitionId ==
                    CoreProductionContent.FinishHornRecipeId &&
                item.Status == ProductionOrderStatus.Active);
            GUILayout.Label(
                $"畜牧链：活羊{AvailableProductQuantity(CoreProductionContent.LiveSheepProductId)}，" +
                $"牧草{AvailableProductQuantity(CoreProductionContent.PastureFodderProductId)}，" +
                $"生皮{AvailableProductQuantity(CoreProductionContent.RawHideProductId)}，" +
                $"生角{AvailableProductQuantity(CoreProductionContent.RawHornProductId)}，" +
                $"皮革{AvailableProductQuantity(CoreProductionContent.LeatherMaterialProductId)}，" +
                $"角料{AvailableProductQuantity(CoreProductionContent.HornMaterialProductId)}",
                _normalStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = fodderExtraction == null && barkExtraction == null;
            if (GUILayout.Button("采集牧草与鞣料", GUILayout.Width(170)))
            {
                fodderExtraction = _upstreamResourceProductionSystem.CreateOrder(
                    _world,
                    UpstreamResourceProductionSystem.PrototypePastureForageBodyId,
                    UpstreamResourceProductionSystem.PrototypePastureForageSiteId,
                    "person.zhang_shiping",
                    new[] { "person.zhang_shiping" },
                    ProductionControlMode.WorkOrder,
                    20);
                barkExtraction = _upstreamResourceProductionSystem.CreateOrder(
                    _world,
                    UpstreamResourceProductionSystem.PrototypeTanningBarkBodyId,
                    UpstreamResourceProductionSystem.PrototypeBarkHarvestingSiteId,
                    "person.su_shuang",
                    new[] { "person.su_shuang" },
                    ProductionControlMode.WorkOrder,
                    2);
                _message = "已预留草料和鞣料资源，等待采集完成。";
            }

            GUI.enabled = husbandryOrder == null &&
                AvailableProductQuantity(
                    CoreProductionContent.LiveSheepProductId) >= 1 &&
                AvailableProductQuantity(
                    CoreProductionContent.PastureFodderProductId) >= 10;
            if (GUILayout.Button("繁育1批羊", GUILayout.Width(140)))
            {
                husbandryOrder =
                    _livestockProductionSystem.CreateHusbandryOrder(
                        _world,
                        "person.zhang_shiping",
                        ProductionControlMode.TargetInstruction,
                        1);
                _message = $"繁育订单{husbandryOrder.Id}已预留种羊和牧草。";
            }

            GUI.enabled = slaughterOrder == null &&
                AvailableProductQuantity(
                    CoreProductionContent.LiveSheepProductId) >= 2;
            if (GUILayout.Button("屠宰2只羊", GUILayout.Width(140)))
            {
                slaughterOrder =
                    _livestockProductionSystem.CreateSlaughterOrder(
                        _world,
                        "person.su_shuang",
                        ProductionControlMode.WorkOrder,
                        2);
                _message = $"屠宰订单{slaughterOrder.Id}已预留两只羊。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = tanningOrder == null &&
                AvailableProductQuantity(CoreProductionContent.RawHideProductId) >=
                    4 &&
                AvailableProductQuantity(
                    CoreProductionContent.TanningBarkProductId) >= 2;
            if (GUILayout.Button("鞣制2批皮革", GUILayout.Width(160)))
            {
                tanningOrder = _livestockProductionSystem.CreateTanningOrder(
                    _world,
                    "person.zhang_shiping",
                    ProductionControlMode.WorkOrder,
                    2);
                _message = $"制革订单{tanningOrder.Id}已预留生皮与鞣料。";
            }

            GUI.enabled = hornOrder == null &&
                AvailableProductQuantity(CoreProductionContent.RawHornProductId) >=
                    2;
            if (GUILayout.Button("整理1批角料", GUILayout.Width(160)))
            {
                hornOrder =
                    _livestockProductionSystem.CreateHornFinishingOrder(
                        _world,
                        "person.su_shuang",
                        ProductionControlMode.WorkOrder,
                        1);
                _message = $"角料订单{hornOrder.Id}已预留生角。";
            }

            var livestockFinishDay = Math.Max(
                Math.Max(
                    fodderExtraction?.FinishDay ?? _world.AbsoluteDay,
                    barkExtraction?.FinishDay ?? _world.AbsoluteDay),
                Math.Max(
                    Math.Max(
                        husbandryOrder?.FinishDay ?? _world.AbsoluteDay,
                        slaughterOrder?.FinishDay ?? _world.AbsoluteDay),
                    Math.Max(
                        tanningOrder?.FinishDay ?? _world.AbsoluteDay,
                        hornOrder?.FinishDay ?? _world.AbsoluteDay)));
            GUI.enabled = livestockFinishDay > _world.AbsoluteDay;
            if (GUILayout.Button("推进畜牧工单完成", GUILayout.Width(180)))
            {
                _simulator.AdvanceDays(
                    _world,
                    Math.Max(
                        1,
                        (int)(livestockFinishDay - _world.AbsoluteDay)));
                _message = "已按世界时间推进并结算当前畜牧链工单。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = charcoalOrder == null &&
                AvailableProductQuantity(
                    CoreProductionContent.TimberMaterialProductId) >= 12;
            if (GUILayout.Button("烧制6批木炭", GUILayout.Width(160)))
            {
                charcoalOrder =
                    _processingProductionSystem.CreateOrganizationOrder(
                        _world,
                        CoreProductionContent.BurnCharcoalRecipeId,
                        CoreProductionContent.EarthKilnCharcoalMethodId,
                        "organization.zhongshan_merchants",
                        UpstreamResourceProductionSystem
                            .PrototypeCharcoalKilnSiteId,
                        MilitaryEquipmentRepairSystem
                            .PrototypeWorkshopContainerId,
                        "person.zhang_shiping",
                        ProductionControlMode.WorkOrder,
                        6);
                _message = $"烧炭订单{charcoalOrder.Id}已预留木料。";
            }

            GUI.enabled = smeltOrder == null &&
                AvailableProductQuantity(
                    CoreProductionContent.IronOreProductId) >= 12 &&
                AvailableProductQuantity(
                    CoreProductionContent.CharcoalProductId) >= 4;
            if (GUILayout.Button("冶炼4批铁料", GUILayout.Width(160)))
            {
                smeltOrder =
                    _processingProductionSystem.CreateOrganizationOrder(
                        _world,
                        CoreProductionContent.SmeltBloomeryIronRecipeId,
                        CoreProductionContent.BloomerySmeltingMethodId,
                        "organization.zhongshan_merchants",
                        UpstreamResourceProductionSystem.PrototypeBloomerySiteId,
                        MilitaryEquipmentRepairSystem
                            .PrototypeWorkshopContainerId,
                        "person.su_shuang",
                        ProductionControlMode.WorkOrder,
                        4);
                _message = $"冶炼订单{smeltOrder.Id}已预留矿石与木炭。";
            }

            GUI.enabled = charcoalOrder != null || smeltOrder != null;
            if (GUILayout.Button("推进初加工完成", GUILayout.Width(180)))
            {
                var finishDay = Math.Max(
                    charcoalOrder?.FinishDay ?? _world.AbsoluteDay,
                    smeltOrder?.FinishDay ?? _world.AbsoluteDay);
                _simulator.AdvanceDays(
                    _world,
                    Math.Max(1, (int)(finishDay - _world.AbsoluteDay)));
                _message = "已按世界时间推进并结算上游初加工。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = manufacturingOrder == null &&
                FindPerson("person.su_shuang").LocationId ==
                    "location.zhongshan";
            if (GUILayout.Button("工坊制造5件长矛", GUILayout.Width(180)))
            {
                manufacturingOrder =
                    _processingProductionSystem.CreateOrganizationOrder(
                        _world,
                        CoreProductionContent.ForgeLongSpearRecipeId,
                        CoreProductionContent.BlacksmithingMethodId,
                        "organization.zhongshan_merchants",
                        MilitaryEquipmentRepairSystem.PrototypeWorkshopSiteId,
                        MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId,
                        "person.su_shuang",
                        ProductionControlMode.WorkOrder,
                        5);
                _message = $"制造订单{manufacturingOrder.Id}已预留原料。";
            }

            GUI.enabled = manufacturingOrder != null;
            if (GUILayout.Button("推进至制造完成", GUILayout.Width(180)))
            {
                var days = Math.Max(
                    1,
                    (int)(manufacturingOrder.FinishDay -
                        _world.AbsoluteDay));
                _simulator.AdvanceDays(_world, days);
                _message = "已按世界时间推进并结算军械制造。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled =
                _world.MilitaryProcurementOrders.Count == 0 &&
                FindJourney(merchant.Id) == null &&
                FindArmyMarch(army.Id) == null &&
                merchant.LocationId == "location.zhongshan" &&
                army.LocationId == "location.zhongshan";
            if (GUILayout.Button(
                    "采购2件长矛并与援军同赴安平",
                    GUILayout.Width(260)))
            {
                _armySystem.StartMarch(
                    _world,
                    new StableId("person.zou_jing"),
                    new StableId(army.Id),
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping"));
                var order = _militaryProcurementSystem.CreateOrderAndDispatch(
                    _world,
                    new StableId("person.zou_jing"),
                    new StableId(merchant.Id),
                    new StableId(army.Id),
                    new StableId(MilitaryEquipmentSystem.LongSpearId),
                    2,
                    25,
                    new StableId("route.zhongshan_anping"),
                    new StableId("location.anping"));
                _message = $"采购单{order.Id}已发运。";
            }

            GUI.enabled = _world.MilitaryProcurementOrders.Exists(item =>
                item.Status != MilitaryProcurementStatus.Delivered);
            if (GUILayout.Button("推进军械运输18时段", GUILayout.Width(190)))
            {
                _simulator.AdvanceSegments(_world, 18);
                _message = "已推进运输，采购单将按承运人与军队实际位置结算。";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            for (var i = 0; i < _world.MilitaryProcurementOrders.Count; i++)
            {
                var order = _world.MilitaryProcurementOrders[i];
                GUILayout.Label(
                    $"{order.Id}：{order.Status}，{order.Quantity}件，" +
                    $"付款{order.TotalPaid}钱，交付地{order.DestinationLocationId}",
                    _normalStyle);
            }

            var first = Math.Max(0, _world.MilitarySupplies.Count - 5);
            for (var i = _world.MilitarySupplies.Count - 1; i >= first; i--)
            {
                var supply = _world.MilitarySupplies[i];
                GUILayout.Label(
                    $"第{supply.Day}日　{supply.Type}　{supply.Summary}",
                    _normalStyle);
            }
        }

        private void DrawLifeEvents()
        {
            GUILayout.Space(10);
            GUILayout.Label("普通生活与家族世录", _sectionStyle);
            if (_world.LifeEvents.Count == 0)
            {
                GUILayout.Label("推进到第30日后开始产生月度生活事件。", _normalStyle);
                return;
            }

            var first = Math.Max(0, _world.LifeEvents.Count - 12);
            for (var i = _world.LifeEvents.Count - 1; i >= first; i--)
            {
                var lifeEvent = _world.LifeEvents[i];
                GUILayout.Label(
                    $"第{lifeEvent.Day}日　{lifeEvent.Type}　{lifeEvent.Summary}",
                    _normalStyle);
            }
        }

        private void DrawRelationships()
        {
            GUILayout.Space(10);
            GUILayout.Label("人物关系（有方向）", _sectionStyle);
            if (_world.Relationships.Count == 0)
            {
                GUILayout.Label("尚无已记录关系。", _normalStyle);
                return;
            }

            for (var i = 0; i < _world.Relationships.Count; i++)
            {
                var relationship = _world.Relationships[i];
                GUILayout.Label(
                    $"{FindPersonName(relationship.FromPersonId)} → " +
                    $"{FindPersonName(relationship.ToPersonId)}：" +
                    $"好感 {relationship.Affection}　信任 {relationship.Trust}　" +
                    $"敬重 {relationship.Respect}　恩义 {relationship.Obligation}",
                    _normalStyle);
            }
        }

        private void DrawOrganizations()
        {
            GUILayout.Space(10);
            GUILayout.Label("组织、职位与成员", _sectionStyle);
            for (var organizationIndex = 0;
                 organizationIndex < _world.Organizations.Count;
                 organizationIndex++)
            {
                var organization = _world.Organizations[organizationIndex];
                GUILayout.Label(
                    $"{organization.DisplayName}（{organization.Type}）　" +
                    $"驻地：{FindLocationName(organization.HeadquartersLocationId)}",
                    _normalStyle);

                for (var memberIndex = 0;
                     memberIndex < _world.Memberships.Count;
                     memberIndex++)
                {
                    var membership = _world.Memberships[memberIndex];
                    if (membership.OrganizationId != organization.Id)
                    {
                        continue;
                    }

                    GUILayout.Label(
                        $"　{FindPersonName(membership.PersonId)}：" +
                        $"{FindPositionName(membership.PositionId)}　" +
                        $"忠诚 {membership.LoyaltyBasisPoints / 100f:F1}%",
                        _normalStyle);
                }
            }
        }

        private void DrawTasks()
        {
            GUILayout.Space(10);
            GUILayout.Label("任务板（演示人物：刘备）", _sectionStyle);
            for (var i = 0; i < _world.TaskDefinitions.Count; i++)
            {
                var definition = _world.TaskDefinitions[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"{definition.DisplayName}　" +
                    $"{(definition.IsAvailable ? "可接受" : "未解锁")}　" +
                    $"起点：{FindLocationName(definition.OriginLocationId)}　" +
                    $"期限：{definition.DurationDays}天　奖励：{definition.RewardMoney}钱/" +
                    $"{definition.RewardProvisions}口粮",
                    _normalStyle);
                GUI.enabled = definition.IsAvailable;
                if (GUILayout.Button("刘备接受", GUILayout.Width(100)))
                {
                    var result = _taskSystem.TryAccept(
                        _world,
                        new StableId("person.liu_bei"),
                        new StableId(definition.Id));
                    _message = result.Message;
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (_world.Tasks.Count == 0)
            {
                GUILayout.Label("当前没有已接受任务。", _normalStyle);
                return;
            }

            for (var i = 0; i < _world.Tasks.Count; i++)
            {
                var task = _world.Tasks[i];
                var definition = FindTaskDefinition(task.DefinitionId);
                GUILayout.Label(
                    $"{FindPersonName(task.AssigneePersonId)}：{definition.DisplayName}　" +
                    $"状态：{task.Status}　进度：{task.Progress}/{definition.RequiredProgress}　" +
                    $"截止日：{task.DeadlineDay}",
                    _normalStyle);
            }
        }

        private void DrawActionLog()
        {
            GUILayout.Space(10);
            GUILayout.Label("NPC行动日志", _sectionStyle);
            if (_actionLog.Count == 0)
            {
                GUILayout.Label("尚未结算NPC行动。", _normalStyle);
                return;
            }

            for (var i = 0; i < _actionLog.Count; i++)
            {
                GUILayout.Label(_actionLog[i], _normalStyle);
            }
        }

        private void InitializeWorld()
        {
            _world = _newGameSetupService.CreateExisting184World(
                "person.liu_bei",
                DefaultSeed);
            _snapshot = null;
            _actionLog.Clear();
            RebindServices();
            RefreshMonthlyDecisions();
            _message = "184年原型世界已初始化。";
        }

        private void RebindServices()
        {
            _enteredTownLocationId = string.Empty;
            _enteredTownFacilityId = string.Empty;
            _simulator = new WorldSimulator(
                _world.MasterSeed, LoadProductionContent());
            _playerActionService = new PlayerActionService(
                _simulator, _merchantContent);
            _decisionSystem = new NpcDecisionSystem(_world.MasterSeed);
            _actionResolver = new NpcActionResolver(_world.MasterSeed);
            _battleResolver = new BattleResolver(_world.MasterSeed);
            _medicalSystem = new MedicalSystem(_world.MasterSeed);
        }

        private static ProductionContentRegistry LoadProductionContent()
        {
            var asset = Resources.Load<TextAsset>(
                "Content/Core/Production/core-production");
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Core production content resource is missing.");
            }

            return ProductionContentRegistry.FromJson(asset.text);
        }

        private static MerchantHouseholdContentRegistry
            LoadMerchantHouseholdContent()
        {
            var asset = Resources.Load<TextAsset>(
                "Content/Core/Gameplay/merchant-household-p1");
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Merchant-household P1 content resource is missing.");
            }

            return MerchantHouseholdContentRegistry.FromJson(asset.text);
        }

        private void ResolveMonthlyNpcActions()
        {
            _actionLog.Clear();
            var monthIndex = _world.AbsoluteDay / 30;
            for (var i = 0; i < _world.People.Count; i++)
            {
                var person = _world.People[i];
                if (!person.IsAlive || FindJourney(person.Id) != null)
                {
                    continue;
                }

                if (!_decisions.TryGetValue(person.Id, out var decision))
                {
                    decision = _decisionSystem.ChooseMonthlyFocus(person, monthIndex);
                }

                var command = _actionPlanner.Plan(_world, person, decision);
                var outcome = _actionResolver.Resolve(_world, command, monthIndex);
                _actionLog.Add(
                    $"{person.DisplayName}：{ActionName(command.ActionType)}——{outcome.Summary}");
            }

            _message = $"已结算{_actionLog.Count}名NPC的本月行动。";
        }

        private void RefreshMonthlyDecisions()
        {
            _decisions.Clear();
            var monthIndex = _world.AbsoluteDay / 30;
            for (var i = 0; i < _world.People.Count; i++)
            {
                var person = _world.People[i];
                if (person.IsAlive)
                {
                    _decisions[person.Id] =
                        _decisionSystem.ChooseMonthlyFocus(person, monthIndex);
                }
            }
        }

        private void TryStartLiuBeiJourney()
        {
            var person = FindPerson("person.liu_bei");
            if (FindJourney(person.Id) != null)
            {
                _message = "刘备已经在旅途中。";
                return;
            }

            if (person.LocationId != "location.zhuo")
            {
                _message = "刘备当前不在涿县，不能重复执行这条演示路线。";
                return;
            }

            _travelSystem.StartJourney(
                _world,
                new StableId(person.Id),
                new StableId("route.zhuo_zhongshan"),
                new StableId("location.zhongshan"),
                TravelMode.Foot);
            _message = "刘备已从涿县出发，徒步前往中山；预计5天抵达。";
        }

        private PersonState FindPlayer()
        {
            if (_world == null || string.IsNullOrEmpty(_world.PlayerPersonId))
            {
                throw new InvalidOperationException("当前世界没有玩家控制人物。");
            }

            return FindPerson(_world.PlayerPersonId);
        }

        private PersonState FindPerson(string personId)
        {
            for (var i = 0; i < _world.People.Count; i++)
            {
                if (_world.People[i].Id == personId)
                {
                    return _world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private FamilyState FindFamilyForPerson(string personId)
        {
            for (var i = 0; i < _world.Families.Count; i++)
            {
                if (_world.Families[i].MemberIds.Contains(personId))
                {
                    return _world.Families[i];
                }
            }

            return null;
        }

        private JourneyState FindJourney(string personId)
        {
            for (var i = 0; i < _world.Journeys.Count; i++)
            {
                if (_world.Journeys[i].PersonId == personId)
                {
                    return _world.Journeys[i];
                }
            }

            return null;
        }

        private ArmyState FindArmy(string armyId)
        {
            for (var i = 0; i < _world.Armies.Count; i++)
            {
                if (_world.Armies[i].Id == armyId)
                {
                    return _world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }

        private ArmyMarchState FindArmyMarch(string armyId)
        {
            for (var i = 0; i < _world.ArmyMarches.Count; i++)
            {
                if (_world.ArmyMarches[i].ArmyId == armyId)
                {
                    return _world.ArmyMarches[i];
                }
            }

            return null;
        }

        private OrganizationState FindOrganization(string organizationId)
        {
            for (var i = 0; i < _world.Organizations.Count; i++)
            {
                if (_world.Organizations[i].Id == organizationId)
                {
                    return _world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private string FindLocationName(string locationId)
        {
            return FindLocationName(_world, locationId);
        }

        private LocationState FindLocation(string locationId)
        {
            for (var i = 0; i < _world.Locations.Count; i++)
            {
                if (_world.Locations[i].Id == locationId)
                {
                    return _world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
        }

        private RouteState FindRoute(string routeId)
        {
            for (var i = 0; i < _world.Routes.Count; i++)
            {
                if (_world.Routes[i].Id == routeId)
                {
                    return _world.Routes[i];
                }
            }

            throw new InvalidOperationException($"Missing route {routeId}.");
        }

        private RouteState FindRouteBetween(string fromLocationId, string toLocationId)
        {
            if (fromLocationId == toLocationId)
            {
                return null;
            }

            for (var i = 0; i < _world.Routes.Count; i++)
            {
                var route = _world.Routes[i];
                if (route.FromLocationId == fromLocationId &&
                    route.ToLocationId == toLocationId ||
                    route.Bidirectional &&
                    route.ToLocationId == fromLocationId &&
                    route.FromLocationId == toLocationId)
                {
                    return route;
                }
            }

            return null;
        }

        private static string FindLocationName(WorldState world, string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i].DisplayName;
                }
            }

            return locationId;
        }

        private static string FindIdentityName(WorldState world, string personId)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId != personId)
                {
                    continue;
                }

                for (var positionIndex = 0;
                     positionIndex < world.Positions.Count;
                     positionIndex++)
                {
                    if (world.Positions[positionIndex].Id == membership.PositionId)
                    {
                        return world.Positions[positionIndex].DisplayName;
                    }
                }
            }

            return "在野";
        }

        private static string StartingIdentityDescription(int identityIndex)
        {
            switch ((StartingIdentity)identityIndex)
            {
                case StartingIdentity.Soldier:
                    return "从幽州援军当前集结地入伍，拥有士卒职位，可承接军粮和征募任务。";
                case StartingIdentity.CountyClerk:
                    return "从涿县官署担任书佐，可承接户籍、治安和地方政务任务。";
                case StartingIdentity.Merchant:
                    return "从中山商行开始，拥有更多本钱和载货能力，可经营中山—涿县商路。";
                case StartingIdentity.Physician:
                    return "从广宗救济营开始，拥有较高医术，可购买药材并救治战场伤员。";
                case StartingIdentity.Farmer:
                    return "从具体家户与田地开始，可安排农季、投入种子与劳力并收获产品批次。";
                case StartingIdentity.Scholar:
                    return "从乡学开始，可研习、整理文书并通过任务影响地方治理。";
                default:
                    return string.Empty;
            }
        }

        private static string GenderName(PersonGender gender)
        {
            switch (gender)
            {
                case PersonGender.Male:
                    return "男";
                case PersonGender.Female:
                    return "女";
                default:
                    return "未记载";
            }
        }

        private string FindPersonName(string personId)
        {
            for (var i = 0; i < _world.People.Count; i++)
            {
                if (_world.People[i].Id == personId)
                {
                    return _world.People[i].DisplayName;
                }
            }

            return personId;
        }

        private string FindCommodityName(string commodityId)
        {
            for (var i = 0; i < _world.Commodities.Count; i++)
            {
                if (_world.Commodities[i].Id == commodityId)
                {
                    return _world.Commodities[i].DisplayName;
                }
            }

            return commodityId;
        }

        private string FindEquipmentName(string equipmentId)
        {
            for (var i = 0;
                 i < _world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                var definition = _world.MilitaryEquipmentDefinitions[i];
                if (definition.Id == equipmentId)
                {
                    return definition.DisplayName;
                }
            }

            return equipmentId;
        }

        private long AvailableProductQuantity(string productDefinitionId)
        {
            long quantity = 0;
            for (var i = 0; i < _world.ProductBatches.Count; i++)
            {
                var batch = _world.ProductBatches[i];
                if (batch.ProductDefinitionId == productDefinitionId &&
                    batch.InventoryContainerId ==
                        MilitaryEquipmentRepairSystem.PrototypeWorkshopContainerId)
                {
                    quantity = checked(quantity +
                        batch.Quantity - batch.ReservedQuantity);
                }
            }

            return quantity;
        }

        private string EquipmentLoadout(string personId)
        {
            var result = string.Empty;
            for (var i = 0; i < _world.MilitaryEquipmentIssues.Count; i++)
            {
                var issue = _world.MilitaryEquipmentIssues[i];
                if (issue.PersonId != personId)
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += "、";
                }

                result += FindEquipmentName(issue.EquipmentDefinitionId) +
                          "×" + issue.Quantity;
            }

            return result.Length == 0 ? "无" : result;
        }

        private static int TroopCount(
            MilitaryEquipmentReadinessReport report,
            string troopTypeId)
        {
            return report.TroopCounts.TryGetValue(troopTypeId, out var count)
                ? count
                : 0;
        }

        private string CargoSummary(string personId)
        {
            var summary = string.Empty;
            for (var i = 0; i < _world.Inventories.Count; i++)
            {
                var stack = _world.Inventories[i];
                if (stack.OwnerPersonId != personId)
                {
                    continue;
                }

                if (summary.Length > 0)
                {
                    summary += "/";
                }

                summary +=
                    $"{FindCommodityName(stack.CommodityId)}×{stack.Quantity}";
            }

            return summary.Length == 0 ? "无" : summary;
        }

        private string FindPositionName(string positionId)
        {
            for (var i = 0; i < _world.Positions.Count; i++)
            {
                if (_world.Positions[i].Id == positionId)
                {
                    return _world.Positions[i].DisplayName;
                }
            }

            return positionId;
        }

        private TaskDefinitionState FindTaskDefinition(string definitionId)
        {
            for (var i = 0; i < _world.TaskDefinitions.Count; i++)
            {
                if (_world.TaskDefinitions[i].Id == definitionId)
                {
                    return _world.TaskDefinitions[i];
                }
            }

            throw new InvalidOperationException($"Missing task definition {definitionId}.");
        }

        private HistoricalAnchorRuntimeState FindHistoricalAnchor(string definitionId)
        {
            for (var i = 0; i < _world.HistoricalAnchors.Count; i++)
            {
                if (_world.HistoricalAnchors[i].DefinitionId == definitionId)
                {
                    return _world.HistoricalAnchors[i];
                }
            }

            return null;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.35f) }
            };
            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            };
            _normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            _mapLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(4, 4, 2, 2),
                normal = { textColor = ProceduralSilkMapArt.Ink }
            };
            _mapSealStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.59f, 1f) }
            };
            _townHeroTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.86f, 0.63f, 1f) }
            };
            _townHeroDetailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.94f, 0.88f, 0.72f, 1f) }
            };
            _townCardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = ProceduralSilkMapArt.Ink }
            };
            _townCardDetailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                clipping = TextClipping.Clip,
                normal = { textColor = ProceduralSilkMapArt.Ink }
            };
        }

        private static string FocusName(NpcMonthlyFocus focus)
        {
            switch (focus)
            {
                case NpcMonthlyFocus.MaintainLivelihood:
                    return "维持生计";
                case NpcMonthlyFocus.CareForFamily:
                    return "照顾家庭";
                case NpcMonthlyFocus.ImproveStatus:
                    return "提升身份";
                case NpcMonthlyFocus.AccumulateWealth:
                    return "积累财富";
                case NpcMonthlyFocus.MaintainRelationships:
                    return "维护关系";
                case NpcMonthlyFocus.RespondToWar:
                    return "应对战争";
                default:
                    return focus.ToString();
            }
        }

        private static string ActionName(NpcActionType action)
        {
            switch (action)
            {
                case NpcActionType.Work:
                    return "务工";
                case NpcActionType.Trade:
                    return "交易";
                case NpcActionType.Visit:
                    return "拜访";
                case NpcActionType.SeekOffice:
                    return "求仕";
                case NpcActionType.Enlist:
                    return "参军";
                case NpcActionType.Flee:
                    return "避难";
                default:
                    return action.ToString();
            }
        }
    }
}
