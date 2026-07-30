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

        private WorldState _world;
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
        private readonly Dictionary<string, NpcDecision> _decisions =
            new Dictionary<string, NpcDecision>(StringComparer.Ordinal);

        private Vector2 _scroll;
        private string _snapshot;
        private string _message = "世界尚未初始化。";
        private readonly List<string> _actionLog = new List<string>();
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _normalStyle;

        private void Awake()
        {
            InitializeWorld();
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height - 32));
            GUILayout.Label("群雄志：仕途——世界模拟观察台", _titleStyle);
            GUILayout.Label(
                "这是开发调试界面：用于观察时间、人物、市场、沙盒AI和旅行，并非最终UI。",
                _normalStyle);

            DrawToolbar();
            GUILayout.Space(8);
            GUILayout.Label(_message, _normalStyle);
            GUILayout.Space(8);

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawWorldSummary();
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
            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
            _world = PrototypeWorldFactory.Create184World(DefaultSeed);
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
            for (var i = 0; i < _world.Locations.Count; i++)
            {
                if (_world.Locations[i].Id == locationId)
                {
                    return _world.Locations[i].DisplayName;
                }
            }

            return locationId;
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
