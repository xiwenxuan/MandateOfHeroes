using System;
using System.Collections.Generic;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using UnityEngine;

namespace Mandate.Presentation
{
    public sealed class SimulationDashboard : MonoBehaviour
    {
        private const ulong DefaultSeed = 184_001UL;

        private enum ScreenMode : byte
        {
            MainMenu,
            NewGame,
            Playing
        }

        private enum PlayerPanel : byte
        {
            Map,
            Character,
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

        private static readonly string[] StartingIdentityLabels =
        {
            "军人",
            "县吏",
            "商人",
            "医者"
        };

        private readonly NewGameSetupService _newGameSetupService =
            new NewGameSetupService();
        private WorldState _world;
        private WorldState _selectionPreview;
        private WorldSimulator _simulator;
        private NpcDecisionSystem _decisionSystem;
        private NpcActionResolver _actionResolver;
        private readonly NpcActionPlanner _actionPlanner = new NpcActionPlanner();
        private readonly TravelSystem _travelSystem = new TravelSystem();
        private readonly TaskSystem _taskSystem = new TaskSystem();
        private readonly TradingSystem _tradingSystem = new TradingSystem();
        private readonly ArmySystem _armySystem = new ArmySystem();
        private readonly MilitarySupplySystem _militarySupplySystem =
            new MilitarySupplySystem();
        private BattleResolver _battleResolver;
        private MedicalSystem _medicalSystem;
        private readonly ConstructionSystem _constructionSystem =
            new ConstructionSystem();
        private readonly PopulationLedgerSystem _populationLedgerSystem =
            new PopulationLedgerSystem();
        private readonly Dictionary<string, NpcDecision> _decisions =
            new Dictionary<string, NpcDecision>(StringComparer.Ordinal);

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
        private int _existingPersonIndex;
        private Vector2 _selectionScroll;
        private MapOverlay _mapOverlay;
        private MapPerspective _mapPerspective;
        private float _mapZoom = 1f;
        private Vector2 _mapPan;
        private bool _mapDragging;
        private int _mapDragButton = -1;
        private Vector2 _mapDragStart;
        private Vector2 _mapPanStart;
        private string _selectedLocationId;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _mapLabelStyle;
        private GUIStyle _mapSealStyle;
        private ProceduralSilkMapArt _mapArt;

        private void Awake()
        {
            _mapArt = new ProceduralSilkMapArt();
            _selectionPreview = PrototypeWorldFactory.Create184World(DefaultSeed);
            _message = "请选择开始新游戏，创建人物或扮演世界中的现有人物。";
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
            if (GUILayout.Button("开发者快速进入（刘备）", GUILayout.Height(36)))
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
        }

        private void DrawExistingCharacterSetup()
        {
            GUILayout.Label("选择要扮演的人物", _sectionStyle);
            var labels = new string[_selectionPreview.People.Count];
            for (var i = 0; i < _selectionPreview.People.Count; i++)
            {
                var person = _selectionPreview.People[i];
                labels[i] =
                    $"{person.DisplayName}　{FindLocationName(_selectionPreview, person.LocationId)}　" +
                    $"{FindIdentityName(_selectionPreview, person.Id)}";
            }

            _existingPersonIndex = GUILayout.SelectionGrid(
                _existingPersonIndex,
                labels,
                2,
                GUILayout.Height(Mathf.Max(160f, labels.Length * 27f)));
            GUILayout.Space(8);
            GUILayout.Label(
                "现有人物使用同一套人物、身份和世界规则；选择后不会获得额外的玩家专属加成。",
                _normalStyle);
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

                    var request = new NewGameCharacterRequest
                    {
                        DisplayName = _customName,
                        Age = age,
                        Gender = _customGender == 0
                            ? PersonGender.Male
                            : PersonGender.Female,
                        Identity = (StartingIdentity)_customIdentity
                    };
                    EnterWorld(
                        _newGameSetupService.CreateCustom184World(
                            request,
                            DefaultSeed));
                }
                else
                {
                    if (_existingPersonIndex < 0 ||
                        _existingPersonIndex >= _selectionPreview.People.Count)
                    {
                        throw new InvalidOperationException("请选择一名现有人物。");
                    }

                    EnterWorld(
                        _newGameSetupService.CreateExisting184World(
                            _selectionPreview.People[_existingPersonIndex].Id,
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
            _mapPerspective = MapPerspectiveSystem.RecommendForPlayer(
                _world,
                _world.PlayerPersonId);
        }

        private void DrawPlayerGame()
        {
            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height - 32));
            DrawPlayerHeader();
            GUILayout.Space(8);
            GUILayout.Label(_message, _normalStyle);
            GUILayout.Space(8);

            _scroll = GUILayout.BeginScrollView(_scroll);
            switch (_playerPanel)
            {
                case PlayerPanel.Map:
                    DrawPlayerMap();
                    break;
                case PlayerPanel.Character:
                    DrawPlayerCharacter();
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

            if (GUILayout.Button("人物", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Character);
            }

            if (GUILayout.Button("任务", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Tasks);
            }

            if (GUILayout.Button("天下", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.World);
            }

            if (GUILayout.Button("开发观察台", GUILayout.Height(32)))
            {
                SetPlayerPanel(PlayerPanel.Developer);
            }

            if (GUILayout.Button("推进一天", GUILayout.Height(32)))
            {
                AdvancePlayerDays(1);
            }

            if (GUILayout.Button("结算NPC", GUILayout.Height(32)))
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

        private void DrawPlayerMap()
        {
            var player = FindPlayer();
            var journey = FindJourney(player.Id);
            GUILayout.Label("地区地图", _sectionStyle);
            GUILayout.Label(
                "点击地点查看详情；鼠标滚轮缩放，按住鼠标右键或中键拖动地图。",
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
            DrawArmyMarkers(canvas);
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
                    : location.DisplayName + "\n" + LocationOverlayLabel(location);
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
                    _message = $"已选择{location.DisplayName}。";
                }

                GUI.color = previous;
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
                $"视角：{MapPerspectiveName(_mapPerspective)}　" +
                $"图层：{MapOverlayName(_mapOverlay)}　缩放：{_mapZoom:F1}倍\n" +
                "金色=玩家　石青=汉军　朱砂=黄巾　线色=道路治安",
                _mapLabelStyle);
        }

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
                $"医术：{player.MedicalSkillBasisPoints / 100f:F1}%　" +
                $"身份：{FindIdentityName(_world, player.Id)}",
                _normalStyle);

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
                $"{physician.DisplayName}　医术：{physician.MedicalSkillBasisPoints / 100f:F1}%　" +
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
            _simulator = new WorldSimulator(_world.MasterSeed);
            _decisionSystem = new NpcDecisionSystem(_world.MasterSeed);
            _actionResolver = new NpcActionResolver(_world.MasterSeed);
            _battleResolver = new BattleResolver(_world.MasterSeed);
            _medicalSystem = new MedicalSystem(_world.MasterSeed);
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
                    return "从涿县加入幽州官军，拥有士卒职位，可承接军粮和征募任务。";
                case StartingIdentity.CountyClerk:
                    return "从涿县官署担任书佐，可承接户籍、治安和地方政务任务。";
                case StartingIdentity.Merchant:
                    return "从中山商行开始，拥有更多本钱和载货能力，可经营中山—涿县商路。";
                case StartingIdentity.Physician:
                    return "从广宗救济营开始，拥有较高医术，可购买药材并救治战场伤员。";
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
