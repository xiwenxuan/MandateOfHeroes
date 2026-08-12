using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class KnownMarketOpportunityView
    {
        public string SourceName;
        public long LearnedDay;
        public int ReliabilityBasisPoints;
        public string OriginLocationId;
        public string TargetLocationId;
        public string CommodityId;
        public int ExpectedOriginUnitPrice;
        public int ExpectedTargetUnitPrice;
        public int ExpectedGrossMargin;
        public int EstimatedTravelDays;
        public int EstimatedProvisionCost;
    }

    public sealed class MerchantHouseholdGoalView
    {
        public bool IsAvailable;
        public string GoalId;
        public string DisplayName;
        public string CurrentObjective;
        public string TrackedObjective;
        public string FamilySituation;
        public string LatestImportantResult;
        public int Phase;
        public TaskStatus Status;
        public long DeadlineDay;
        public KnownMarketOpportunityView MarketOpportunity;
    }

    public sealed class MerchantHouseholdGameplayService
    {
        public const string PrimaryTaskDefinitionId =
            "task_definition.m26p1.household_trade_recovery";
        private const string MemoryPrefix = "life_event.m26p1.";

        private readonly WorldSimulator _simulator;
        private readonly TradingSystem _trading = new TradingSystem();
        private readonly TravelSystem _travel = new TravelSystem();
        private readonly MerchantHouseholdContentRegistry _content;

        public MerchantHouseholdGameplayService(
            WorldSimulator simulator,
            MerchantHouseholdContentRegistry content = null)
        {
            _simulator = simulator;
            _content = content ?? MerchantHouseholdContentRegistry.CreateCore();
        }

        public static void Initialize(
            WorldState world,
            string personId,
            MerchantHouseholdContentRegistry content = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            var registry = content ?? MerchantHouseholdContentRegistry.CreateCore();
            var definition = registry.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var person = FindPerson(world, personId);
            if (!person.IsAlive ||
                person.LocationId != definition.OriginLocationId ||
                !HasTraderPosition(world, person.Id) ||
                string.IsNullOrEmpty(person.FamilyId) ||
                FindTask(world, person.Id) != null)
            {
                return;
            }

            var family = FindFamily(world, person.FamilyId);
            family.Debt = checked(family.Debt + definition.InitialFamilyDebt);
            EnsureTaskDefinition(world, definition);
            world.Tasks.Add(new TaskInstanceState
            {
                Id = "task.m26p1.household_trade_recovery." + person.Id,
                DefinitionId = PrimaryTaskDefinitionId,
                AssigneePersonId = person.Id,
                Status = TaskStatus.Active,
                AcceptedDay = world.AbsoluteDay,
                DeadlineDay = checked(world.AbsoluteDay + definition.DurationDays),
                Progress = 0
            });
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = MemoryPrefix + "family_pressure." + person.Id,
                Type = LifeEventType.HouseholdDebt,
                Day = world.AbsoluteDay,
                PrimaryPersonId = person.Id,
                FamilyId = family.Id,
                Summary = family.DisplayName + "尚欠" + family.Debt +
                    "钱，家中希望用一次稳妥商旅缓解债务。"
            });
        }

        public bool Handles(string actionId) =>
            actionId != null &&
            actionId.StartsWith("player_action.m26p1.",
                StringComparison.Ordinal);

        public bool HasPendingTravelEvent(WorldState world, string personId)
        {
            var task = FindTask(world, personId);
            if (task == null || task.Status != TaskStatus.Active ||
                task.Progress != 2)
            {
                return false;
            }
            var journey = FindJourney(world, personId);
            if (journey == null)
            {
                return false;
            }
            var goal = Goal();
            var travelEvent = _content.GetTravelEvent(goal.TravelEventId);
            return journey.RemainingKilometers <=
                travelEvent.TriggerRemainingKilometers;
        }

        public MerchantHouseholdGoalView Inspect(
            WorldState world,
            string personId)
        {
            var task = FindTask(world, personId);
            if (task == null)
            {
                return new MerchantHouseholdGoalView();
            }
            var goal = Goal();
            var person = FindPerson(world, personId);
            var family = FindFamily(world, person.FamilyId);
            var view = new MerchantHouseholdGoalView
            {
                IsAvailable = true,
                GoalId = goal.Id,
                DisplayName = goal.DisplayName,
                CurrentObjective = CurrentObjective(world, person, task),
                TrackedObjective = TrackedObjective(world, person, task),
                FamilySituation = family.DisplayName + "：家产" + family.Wealth +
                    "钱，债务" + family.Debt + "钱。",
                LatestImportantResult = LatestMemory(world, person.Id),
                Phase = task.Progress,
                Status = task.Status,
                DeadlineDay = task.DeadlineDay,
                MarketOpportunity = BuildOpportunity(task, goal)
            };
            return view;
        }

        public void AddActions(
            WorldState world,
            PersonState person,
            IList<PlayerActionOption> actions)
        {
            var task = FindTask(world, person.Id);
            if (task == null || task.Status != TaskStatus.Active)
            {
                return;
            }
            var goal = Goal();
            var journey = FindJourney(world, person.Id);
            switch (task.Progress)
            {
                case 0:
                    var family = FindFamily(world, person.FamilyId);
                    actions.Add(P1Option(
                        PlayerActionIds.MerchantUseOwnCapital,
                        "给家中留钱，自备商旅本钱",
                        "家人担心这次远行耗尽家底。",
                        "给家中留下" + goal.PersonalReserveContribution +
                            "钱后，以个人余资经营。",
                        "个人钱财转入家庭账；不新增债务。",
                        "若行情变化，余资可能不足以补货。",
                        "交谈",
                        person.Wealth >= goal.PersonalReserveContribution,
                        "还需要" + goal.PersonalReserveContribution +
                            "钱，或可改向商行申请垫款。"));
                    var guild = FindOrganization(world, goal.IssuerOrganizationId);
                    actions.Add(P1Option(
                        PlayerActionIds.MerchantTakeGuildAdvance,
                        "向中山商行申请垫款",
                        "家中债务压着日常开支，需要更充足的周转钱。",
                        "商行先垫" + goal.GuildAdvanceMoney +
                            "钱，家中增加" + goal.GuildAdvanceDebt + "钱债务。",
                        "商行金库减少，个人现金增加，家庭形成真实债务。",
                        "即使跑商亏损，新增债务仍需偿还。",
                        "交谈",
                        guild.Treasury >= goal.GuildAdvanceMoney,
                        "商行当前没有足够周转金；可改用自有本钱。"));
                    break;
                case 1:
                    var buyReason = BuyUnavailableReason(world, person, goal);
                    actions.Add(P1Option(
                        PlayerActionIds.MerchantBuyJourneyCargo,
                        "按计划采购6匹中山布帛",
                        "涿县布价的可信口信提供了一次跨地机会。",
                        "按中山实时价格买足" + goal.CargoQuantity + "匹布帛。",
                        "消耗个人钱财和载重，市场库存与价格立即变化。",
                        "到达时价格和需求可能已经改变。",
                        "搬运/交易",
                        string.IsNullOrEmpty(buyReason),
                        buyReason));
                    break;
                case 2:
                    if (journey == null)
                    {
                        var travelReason = TravelUnavailableReason(
                            world, person, goal);
                        actions.Add(P1Option(
                            PlayerActionIds.MerchantStartJourney,
                            "启程前往涿县",
                            "货物已经备妥，家中在等待这笔经营结果。",
                            "沿已知道路行商约" + EstimatedTravelDays(goal) +
                                "天，途中世界继续运行。",
                            "每日消耗口粮；同行者和货物共同承担道路风险。",
                            "途中可能遇到需要取舍的地方事件。",
                            "行走",
                            string.IsNullOrEmpty(travelReason),
                            travelReason));
                    }
                    else if (HasPendingTravelEvent(world, person.Id))
                    {
                        AddTravelEventActions(world, person, actions);
                    }
                    break;
                case 3:
                    if (journey == null)
                    {
                        var deliverReason = DeliveryUnavailableReason(
                            world, person, goal);
                        actions.Add(P1Option(
                            PlayerActionIds.MerchantDeliverCargo,
                            "在涿县交付并出售布帛",
                            "家中能否缓解债务，取决于这批货的实际成交。",
                            "按涿县当前价格出售现有货物并领取委托报酬。",
                            "货物进入涿县市场；商行金库支付真实佣金。",
                            "逾期会降低佣金，货损会造成部分完成。",
                            "搬运/交易",
                            string.IsNullOrEmpty(deliverReason),
                            deliverReason));
                    }
                    break;
                case 4:
                    AddLongTermActions(world, person, actions);
                    break;
            }
        }

        public PlayerActionResult Execute(
            WorldState world,
            string personId,
            string actionId)
        {
            var person = FindPerson(world, personId);
            var task = FindTask(world, personId);
            if (task == null || task.Status != TaskStatus.Active)
            {
                return Failure(actionId, "当前没有这条商旅—家庭目标。 ");
            }
            var allowed = new List<PlayerActionOption>();
            AddActions(world, person, allowed);
            var option = FindOption(allowed, actionId);
            if (option == null || !option.IsAvailable)
            {
                return Failure(
                    actionId,
                    option == null
                        ? "这项选择不属于当前目标阶段。"
                        : option.UnavailableReason);
            }

            var openingDay = world.AbsoluteDay;
            var openingMoney = person.Wealth;
            var openingProvisions = person.Provisions;
            var openingHealth = person.HealthBasisPoints;
            PlayerActionResult result;
            switch (actionId)
            {
                case PlayerActionIds.MerchantUseOwnCapital:
                    result = CommitOwnCapital(world, person, task);
                    break;
                case PlayerActionIds.MerchantTakeGuildAdvance:
                    result = TakeGuildAdvance(world, person, task);
                    break;
                case PlayerActionIds.MerchantBuyJourneyCargo:
                    result = BuyCargo(world, person, task);
                    break;
                case PlayerActionIds.MerchantStartJourney:
                    result = StartJourney(world, person, task);
                    break;
                case PlayerActionIds.MerchantEventHelp:
                case PlayerActionIds.MerchantEventGuard:
                case PlayerActionIds.MerchantEventRefuse:
                    result = ResolveTravelEvent(world, person, task, actionId);
                    break;
                case PlayerActionIds.MerchantDeliverCargo:
                    result = DeliverCargo(world, person, task);
                    break;
                case PlayerActionIds.MerchantRepayFamilyDebt:
                    result = RepayFamilyDebt(world, person, task);
                    break;
                case PlayerActionIds.MerchantInvestCart:
                    result = InvestInCart(world, person, task);
                    break;
                default:
                    return Failure(actionId, "未知的商旅—家庭行动。 ");
            }

            person = FindPerson(world, personId);
            result.DaysAdvanced = checked((int)(world.AbsoluteDay - openingDay));
            result.MoneyChange = person.Wealth - openingMoney;
            result.ProvisionChange = person.Provisions - openingProvisions;
            result.HealthChange = person.HealthBasisPoints - openingHealth;
            world.Validate();
            return result;
        }

        private PlayerActionResult CommitOwnCapital(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var family = FindFamily(world, person.FamilyId);
            person.Wealth -= goal.PersonalReserveContribution;
            family.Wealth = checked(
                family.Wealth + goal.PersonalReserveContribution);
            task.Progress = 1;
            var memory = RecordMemory(
                world, person, "own_capital", LifeEventType.HouseholdDebt,
                "你给家中留下" + goal.PersonalReserveContribution +
                "钱，决定不用新增借债完成这次经营。 ");
            return Success(
                PlayerActionIds.MerchantUseOwnCapital,
                memory,
                "家中收下备用钱，你以自己的余资承担这次商旅。",
                "交谈");
        }

        private PlayerActionResult TakeGuildAdvance(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var guild = FindOrganization(world, goal.IssuerOrganizationId);
            var family = FindFamily(world, person.FamilyId);
            guild.Treasury -= goal.GuildAdvanceMoney;
            person.Wealth = checked(person.Wealth + goal.GuildAdvanceMoney);
            family.Debt = checked(family.Debt + goal.GuildAdvanceDebt);
            task.Progress = 1;
            var memory = RecordMemory(
                world, person, "guild_advance", LifeEventType.HouseholdDebt,
                "中山商行垫付" + goal.GuildAdvanceMoney +
                "钱，家中新增" + goal.GuildAdvanceDebt + "钱债务。 ");
            AdjustTrust(world, person.Id, goal.CompanionPersonId, 150);
            return Success(
                PlayerActionIds.MerchantTakeGuildAdvance,
                memory,
                "苏双替你在商行作保，周转钱增加，但家债也更重。",
                "交谈");
        }

        private PlayerActionResult BuyCargo(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var existing = _trading.GetQuantity(
                world, person.Id, goal.CommodityId);
            var needed = Math.Max(0, goal.CargoQuantity - existing);
            if (needed > 0)
            {
                var trade = _trading.Buy(
                    world,
                    new StableId(person.Id),
                    new StableId(goal.CommodityId),
                    needed);
                if (!trade.Success)
                {
                    return Failure(PlayerActionIds.MerchantBuyJourneyCargo,
                        trade.Message);
                }
            }
            task.Progress = 2;
            var memory = RecordMemory(
                world, person, "cargo_bought", LifeEventType.Migration,
                "你按中山当日实价备齐" + goal.CargoQuantity +
                "匹布帛，市场库存与价格已经变化。 ");
            return Success(
                PlayerActionIds.MerchantBuyJourneyCargo,
                memory,
                "布帛已经点数装车；涿县价格仍只是此前获得的口信。",
                "搬运/交易");
        }

        private PlayerActionResult StartJourney(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var journey = _travel.StartJourney(
                world,
                new StableId(person.Id),
                new StableId(goal.RouteId),
                new StableId(goal.TargetLocationId),
                TravelMode.Caravan);
            var companion = FindPerson(world, goal.CompanionPersonId, false);
            if (companion != null && companion.IsAlive &&
                companion.LocationId == person.LocationId &&
                FindJourney(world, companion.Id) == null)
            {
                _travel.StartJourney(
                    world,
                    new StableId(companion.Id),
                    new StableId(goal.RouteId),
                    new StableId(goal.TargetLocationId),
                    TravelMode.Caravan);
            }
            var memory = RecordMemory(
                world, person, "departed", LifeEventType.Migration,
                "你沿已知的中山—涿县道路启程，路程" +
                journey.RemainingKilometers + "公里。 ");
            return Success(
                PlayerActionIds.MerchantStartJourney,
                memory,
                "车队离开中山；此后每天都会消耗口粮，市场和家中也继续变化。",
                "行走");
        }

        private PlayerActionResult ResolveTravelEvent(
            WorldState world,
            PersonState person,
            TaskInstanceState task,
            string actionId)
        {
            var goal = Goal();
            var travelEvent = _content.GetTravelEvent(goal.TravelEventId);
            var random = new NamedRandom(world.MasterSeed);
            string key;
            string summary;
            if (actionId == PlayerActionIds.MerchantEventHelp)
            {
                person.Provisions -= travelEvent.HelpProvisionCost;
                AdjustTrust(world, person.Id, goal.CompanionPersonId,
                    travelEvent.HelpTrustGain);
                var lost = random.Range(
                    "m26p1_travel_event",
                    new StableId(person.Id),
                    task.AcceptedDay,
                    travelEvent.Id + ".help",
                    0,
                    10_000) < travelEvent.HelpCargoLossChanceBasisPoints;
                if (lost)
                {
                    _trading.LoseCargo(
                        world, person.Id, goal.CommodityId, 1);
                }
                key = "event_help";
                summary = lost
                    ? "你与苏双停车相助，救下困在路边的人家；搬运中有1匹布帛受损。"
                    : "你与苏双停车相助，保住了对方的车货，也没有损失自己的布帛。";
            }
            else if (actionId == PlayerActionIds.MerchantEventGuard)
            {
                person.Provisions -= travelEvent.GuardProvisionCost;
                AdjustTrust(world, person.Id, goal.CompanionPersonId,
                    travelEvent.GuardTrustChange);
                var injured = random.Range(
                    "m26p1_travel_event",
                    new StableId(person.Id),
                    task.AcceptedDay,
                    travelEvent.Id + ".guard",
                    0,
                    10_000) < travelEvent.GuardInjuryChanceBasisPoints;
                if (injured)
                {
                    person.HealthBasisPoints = Math.Max(
                        0, person.HealthBasisPoints - travelEvent.GuardHealthLoss);
                }
                key = "event_guard";
                summary = injured
                    ? "你留下看守车货，驱赶窥伺者时受了轻伤，布帛得以保全。"
                    : "你留下看守车货，避开了可疑人群，布帛得以保全。";
            }
            else
            {
                person.Provisions -= travelEvent.RefuseProvisionCost;
                AdjustTrust(world, person.Id, goal.CompanionPersonId,
                    travelEvent.RefuseTrustChange);
                key = "event_refuse";
                summary = "你拒绝停车介入，车队继续赶路；苏双对你的取舍有所保留。";
            }
            task.Progress = 3;
            _simulator.AdvanceDays(world, 1);
            var memory = RecordMemory(
                world, person, key, LifeEventType.Migration, summary);
            return Success(actionId, memory, summary, "交谈");
        }

        private PlayerActionResult DeliverCargo(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var available = _trading.GetQuantity(
                world, person.Id, goal.CommodityId);
            var quantity = Math.Min(goal.CargoQuantity, available);
            var trade = _trading.Sell(
                world,
                new StableId(person.Id),
                new StableId(goal.CommodityId),
                quantity);
            if (!trade.Success)
            {
                return Failure(PlayerActionIds.MerchantDeliverCargo,
                    trade.Message);
            }
            var guild = FindOrganization(world, goal.IssuerOrganizationId);
            var commission = world.AbsoluteDay <= task.DeadlineDay
                ? goal.DeliveryCommission
                : goal.LateCommission;
            commission = checked((int)(
                (long)commission * quantity / goal.CargoQuantity));
            guild.Treasury -= commission;
            person.Wealth = checked(person.Wealth + commission);
            task.Progress = 4;
            var partial = quantity < goal.CargoQuantity;
            var memory = RecordMemory(
                world, person, partial ? "partial_delivery" : "delivery",
                LifeEventType.Migration,
                trade.Message + " 商行按实际交付另付" + commission +
                "钱；" + (partial ? "本次为部分完成。" : "本次如数完成。"));
            return Success(
                PlayerActionIds.MerchantDeliverCargo,
                memory,
                trade.Message + " 商行佣金" + commission +
                "钱已经从组织金库支付。",
                "搬运/交易");
        }

        private PlayerActionResult RepayFamilyDebt(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var family = FindFamily(world, person.FamilyId);
            var guild = FindOrganization(world, goal.IssuerOrganizationId);
            var payment = Math.Min(goal.DebtRepayment, checked((int)family.Debt));
            person.Wealth -= payment;
            family.Debt -= payment;
            guild.Treasury = checked(guild.Treasury + payment);
            CompletePrimaryAndCreateFollowup(
                world, person, task, goal.DebtFollowupDefinitionId,
                "补足家中越冬储备", TaskKind.GuidedObjective,
                goal.TargetLocationId, string.Empty);
            var memory = RecordMemory(
                world, person, "repay_debt", LifeEventType.HouseholdDebt,
                "你向中山商行归还" + payment + "钱，家中债务降至" +
                family.Debt + "钱。 ");
            return Success(
                PlayerActionIds.MerchantRepayFamilyDebt,
                memory,
                "家债已经按实减少；下一目标是补足家庭长期储备。",
                "交谈");
        }

        private PlayerActionResult InvestInCart(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var goal = Goal();
            var guild = FindOrganization(world, goal.IssuerOrganizationId);
            person.Wealth -= goal.CartInvestmentCost;
            guild.Treasury = checked(guild.Treasury + goal.CartInvestmentCost);
            person.CargoCapacity = checked(
                person.CargoCapacity + goal.CartCapacityGain);
            var caravanContainer = world.InventoryContainers.Find(item =>
                item.CarrierPersonId == person.Id &&
                item.KindId == "inventory_container.merchant_caravan");
            if (caravanContainer != null)
            {
                caravanContainer.CapacityWeight = person.CargoCapacity;
            }
            CompletePrimaryAndCreateFollowup(
                world, person, task, goal.CartFollowupDefinitionId,
                "经营中山—涿县固定商路", TaskKind.GuidedObjective,
                goal.TargetLocationId, goal.OriginLocationId);
            var memory = RecordMemory(
                world, person, "invest_cart", LifeEventType.Migration,
                "你向商行购置并整修一辆货车，载货上限增加" +
                goal.CartCapacityGain + "；原有家债暂未减少。 ");
            return Success(
                PlayerActionIds.MerchantInvestCart,
                memory,
                "新货车已经记入人物运输能力；下一目标是建立固定往返经营。",
                "工作");
        }

        private void AddTravelEventActions(
            WorldState world,
            PersonState person,
            IList<PlayerActionOption> actions)
        {
            var travelEvent = _content.GetTravelEvent(Goal().TravelEventId);
            actions.Add(P1Option(
                PlayerActionIds.MerchantEventHelp,
                "停车帮忙抬车修轴",
                travelEvent.SpeakerRole + "请你决定是否援手。",
                "耽搁一天并帮助受困人家，苏双会记住你的选择。",
                "消耗" + travelEvent.HelpProvisionCost +
                    "份口粮；搬动货物可能造成少量货损。",
                "已知道路并不安稳，停车会增加暴露时间。",
                "交谈",
                person.Provisions >= travelEvent.HelpProvisionCost,
                "口粮不足，无法停车照料两支车队。"));
            actions.Add(P1Option(
                PlayerActionIds.MerchantEventGuard,
                "留下守货，让苏双去帮忙",
                "你需要在善意和对自家货物的责任之间取舍。",
                "保全布帛并让同行者代为援手。",
                "消耗" + travelEvent.GuardProvisionCost +
                    "份口粮；可能在驱赶窥伺者时受伤。",
                "货物更安全，但苏双会据此判断你的为人。",
                "停留",
                person.Provisions >= travelEvent.GuardProvisionCost,
                "至少需要1份口粮才能在此停留。"));
            actions.Add(P1Option(
                PlayerActionIds.MerchantEventRefuse,
                "拒绝介入，继续赶路",
                "你可以拒绝这项与委托无关的麻烦。",
                "不停车搬货，按原计划继续行程。",
                "消耗1份途中口粮；与苏双的信任下降。",
                "受困人家不会获得你的帮助，后续关系机会也会改变。",
                "行走",
                person.Provisions >= travelEvent.RefuseProvisionCost,
                "已经没有继续赶路所需的口粮。"));
        }

        private void AddLongTermActions(
            WorldState world,
            PersonState person,
            IList<PlayerActionOption> actions)
        {
            var goal = Goal();
            var family = FindFamily(world, person.FamilyId);
            var payment = Math.Min(goal.DebtRepayment, checked((int)family.Debt));
            actions.Add(P1Option(
                PlayerActionIds.MerchantRepayFamilyDebt,
                "优先偿还家债",
                "家中希望先把日后的固定负担降下来。",
                "偿还至多" + payment + "钱，立即降低具体家庭债务。",
                "个人现金转入商行金库；本次不扩大载货能力。",
                "现金余量减少，但家庭抗风险能力提高。",
                "交谈",
                payment > 0 && person.Wealth >= payment,
                payment <= 0
                    ? "家中已经没有这笔债务。"
                    : "还需要" + payment + "钱才能执行。"));
            actions.Add(P1Option(
                PlayerActionIds.MerchantInvestCart,
                "购置货车扩大经营",
                "你也可以把收益投入下一次经营，而不是立刻还债。",
                "支付" + goal.CartInvestmentCost + "钱，载货上限增加" +
                    goal.CartCapacityGain + "。",
                "钱款转入商行金库；家庭原有债务继续存在。",
                "经营能力提高，但家庭负担没有立即下降。",
                "工作",
                person.Wealth >= goal.CartInvestmentCost,
                "还需要" + goal.CartInvestmentCost + "钱。"));
        }

        private static void CompletePrimaryAndCreateFollowup(
            WorldState world,
            PersonState person,
            TaskInstanceState task,
            string definitionId,
            string displayName,
            TaskKind kind,
            string originLocationId,
            string targetLocationId)
        {
            task.Progress = 5;
            task.Status = TaskStatus.Completed;
            task.RewardClaimed = true;
            if (!world.TaskDefinitions.Exists(item => item.Id == definitionId))
            {
                world.TaskDefinitions.Add(new TaskDefinitionState
                {
                    Id = definitionId,
                    DisplayName = displayName,
                    Kind = kind,
                    IssuerOrganizationId = "organization.zhongshan_merchants",
                    OriginLocationId = originLocationId,
                    TargetLocationId = targetLocationId,
                    RequiredProgress = 3,
                    DurationDays = 30,
                    RequiresMembership = false,
                    IsAvailable = false
                });
            }
            world.Tasks.Add(new TaskInstanceState
            {
                Id = "task.m26p1.followup." + person.Id + "." +
                    world.Tasks.Count,
                DefinitionId = definitionId,
                AssigneePersonId = person.Id,
                Status = TaskStatus.Active,
                AcceptedDay = world.AbsoluteDay,
                DeadlineDay = checked(world.AbsoluteDay + 30),
                Progress = 0
            });
        }

        private static void EnsureTaskDefinition(
            WorldState world,
            MerchantHouseholdGoalDefinition goal)
        {
            if (world.TaskDefinitions.Exists(
                    item => item.Id == PrimaryTaskDefinitionId))
            {
                return;
            }
            world.TaskDefinitions.Add(new TaskDefinitionState
            {
                Id = PrimaryTaskDefinitionId,
                DisplayName = goal.DisplayName,
                Kind = TaskKind.GuidedObjective,
                IssuerOrganizationId = goal.IssuerOrganizationId,
                RequiredPositionId = "position.zhongshan_trader",
                OriginLocationId = goal.OriginLocationId,
                TargetLocationId = goal.TargetLocationId,
                RequiredProgress = 5,
                DurationDays = goal.DurationDays,
                RequiresMembership = true,
                IsAvailable = false
            });
        }

        private string BuyUnavailableReason(
            WorldState world,
            PersonState person,
            MerchantHouseholdGoalDefinition goal)
        {
            if (person.LocationId != goal.OriginLocationId)
            {
                return "需要先回到中山市场。";
            }
            var existing = _trading.GetQuantity(
                world, person.Id, goal.CommodityId);
            var needed = Math.Max(0, goal.CargoQuantity - existing);
            if (needed == 0)
            {
                return string.Empty;
            }
            var listing = FindListing(
                world, person.LocationId, goal.CommodityId);
            if (listing.Stock < needed)
            {
                return "中山市场只剩" + listing.Stock + "匹，尚不足备货。";
            }
            var cost = checked((long)listing.Price * needed);
            if (person.Wealth < cost)
            {
                return "按当前实价需要" + cost + "钱。";
            }
            var commodity = FindCommodity(world, goal.CommodityId);
            if (CurrentCargoWeight(world, person.Id) +
                    (long)commodity.UnitWeight * needed > person.CargoCapacity)
            {
                return "现有载货能力不足。";
            }
            return string.Empty;
        }

        private string TravelUnavailableReason(
            WorldState world,
            PersonState person,
            MerchantHouseholdGoalDefinition goal)
        {
            if (person.LocationId != goal.OriginLocationId)
            {
                return "当前不在计划的中山起点。";
            }
            if (_trading.GetQuantity(world, person.Id, goal.CommodityId) <
                goal.CargoQuantity)
            {
                return "尚未备齐计划中的布帛。";
            }
            var provisions = EstimatedTravelDays(goal) + 1;
            if (person.Provisions < provisions)
            {
                return "预计至少需要" + provisions + "份口粮。";
            }
            return string.Empty;
        }

        private string DeliveryUnavailableReason(
            WorldState world,
            PersonState person,
            MerchantHouseholdGoalDefinition goal)
        {
            if (person.LocationId != goal.TargetLocationId)
            {
                return "需要先抵达涿县。";
            }
            if (_trading.GetQuantity(world, person.Id, goal.CommodityId) <= 0)
            {
                return "已经没有可以交付的布帛；可继续经营筹措替代货物。";
            }
            var commission = world.AbsoluteDay <= FindTask(world, person.Id).DeadlineDay
                ? goal.DeliveryCommission
                : goal.LateCommission;
            if (FindOrganization(world, goal.IssuerOrganizationId).Treasury <
                commission)
            {
                return "商行金库暂时无法支付佣金，交付不会被吞没。";
            }
            return string.Empty;
        }

        private KnownMarketOpportunityView BuildOpportunity(
            TaskInstanceState task,
            MerchantHouseholdGoalDefinition goal)
        {
            return new KnownMarketOpportunityView
            {
                SourceName = goal.IntelligenceSourceName,
                LearnedDay = task.AcceptedDay,
                ReliabilityBasisPoints =
                    goal.IntelligenceReliabilityBasisPoints,
                OriginLocationId = goal.OriginLocationId,
                TargetLocationId = goal.TargetLocationId,
                CommodityId = goal.CommodityId,
                ExpectedOriginUnitPrice = goal.ExpectedOriginUnitPrice,
                ExpectedTargetUnitPrice = goal.ExpectedTargetUnitPrice,
                ExpectedGrossMargin = checked(
                    (goal.ExpectedTargetUnitPrice -
                     goal.ExpectedOriginUnitPrice) * goal.CargoQuantity),
                EstimatedTravelDays = EstimatedTravelDays(goal),
                EstimatedProvisionCost = EstimatedTravelDays(goal) + 1
            };
        }

        private static int EstimatedTravelDays(
            MerchantHouseholdGoalDefinition goal) =>
            (140 + TravelSystem.KilometersPerSegment(TravelMode.Caravan) * 4 - 1) /
            (TravelSystem.KilometersPerSegment(TravelMode.Caravan) * 4);

        private string CurrentObjective(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            if (task.Status == TaskStatus.Completed)
            {
                return "首轮商旅已经完成，新的家庭目标已经生成。";
            }
            switch (task.Progress)
            {
                case 0:
                    return "决定用自有本钱，还是接受商行垫款。";
                case 1:
                    return "在中山按实时价格备齐6匹布帛。";
                case 2:
                    return FindJourney(world, person.Id) == null
                        ? "备足口粮并沿已知道路前往涿县。"
                        : HasPendingTravelEvent(world, person.Id)
                            ? "处理路旁折轴车事件后再继续赶路。"
                            : "继续商旅行程，留意途中变化。";
                case 3:
                    return FindJourney(world, person.Id) == null
                        ? "按涿县当前实价交付现有布帛。"
                        : "继续前往涿县，市场和家庭仍在变化。";
                case 4:
                    return "在偿还家债和购置货车之间作出长期选择。";
                default:
                    return "查看新生成的家庭目标。";
            }
        }

        private static string TrackedObjective(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            var followup = world.Tasks.Find(item =>
                item.AssigneePersonId == person.Id &&
                item.Id.StartsWith("task.m26p1.followup.",
                    StringComparison.Ordinal) &&
                item.Status == TaskStatus.Active);
            if (followup == null)
            {
                return "完成本轮经营后，再决定家庭的长期方向。";
            }
            var definition = world.TaskDefinitions.Find(item =>
                item.Id == followup.DefinitionId);
            return definition == null
                ? "后续目标内容缺失：" + followup.DefinitionId
                : definition.DisplayName;
        }

        private static string LatestMemory(WorldState world, string personId)
        {
            for (var i = world.LifeEvents.Count - 1; i >= 0; i--)
            {
                var item = world.LifeEvents[i];
                if (item.PrimaryPersonId == personId &&
                    item.Id.StartsWith(MemoryPrefix,
                        StringComparison.Ordinal))
                {
                    return item.Summary;
                }
            }
            return "尚无重要结果。";
        }

        private static string RecordMemory(
            WorldState world,
            PersonState person,
            string key,
            LifeEventType type,
            string summary)
        {
            var id = MemoryPrefix + key + "." + person.Id;
            if (world.LifeEvents.Exists(item => item.Id == id))
            {
                throw new InvalidOperationException(
                    "The merchant-household result was already committed.");
            }
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = id,
                Type = type,
                Day = world.AbsoluteDay,
                PrimaryPersonId = person.Id,
                SecondaryPersonId = GoalCompanion(world, person.Id),
                FamilyId = person.FamilyId,
                Summary = summary
            });
            return id;
        }

        private static string GoalCompanion(WorldState world, string personId) =>
            world.People.Exists(item => item.Id == "person.su_shuang")
                ? "person.su_shuang"
                : string.Empty;

        private static void AdjustTrust(
            WorldState world,
            string fromPersonId,
            string toPersonId,
            int delta)
        {
            var relationship = world.Relationships.Find(item =>
                item.FromPersonId == fromPersonId &&
                item.ToPersonId == toPersonId);
            if (relationship == null)
            {
                relationship = new RelationshipState
                {
                    Id = "relationship." + fromPersonId + "." + toPersonId,
                    FromPersonId = fromPersonId,
                    ToPersonId = toPersonId
                };
                world.Relationships.Add(relationship);
            }
            relationship.Trust = Math.Max(
                -10_000, Math.Min(10_000, relationship.Trust + delta));
            relationship.LastInteractionDay = world.AbsoluteDay;
        }

        private MerchantHouseholdGoalDefinition Goal() =>
            _content.GetGoal(MerchantHouseholdContentIds.FirstGoal);

        private static PlayerActionOption P1Option(
            string id,
            string name,
            string motivation,
            string expected,
            string cost,
            string risk,
            string cue,
            bool available,
            string unlockHint)
        {
            return new PlayerActionOption
            {
                Id = id,
                DisplayName = name,
                Description = expected,
                Motivation = motivation,
                ExpectedOutcome = expected,
                Cost = cost,
                KnownRisk = risk,
                PresentationCue = cue,
                IsAvailable = available,
                UnavailableReason = available ? string.Empty : unlockHint,
                UnlockHint = unlockHint ?? string.Empty
            };
        }

        private static PlayerActionResult Success(
            string actionId,
            string resultId,
            string summary,
            string cue)
        {
            return new PlayerActionResult
            {
                Success = true,
                ActionId = actionId,
                ResultId = resultId,
                WorldEventId = resultId,
                Summary = summary,
                PresentationCue = cue,
                Detail = "权威结果已写入人物人生事件、任务阶段及相关世界账。"
            };
        }

        private static PlayerActionResult Failure(string actionId, string summary) =>
            new PlayerActionResult
            {
                Success = false,
                ActionId = actionId,
                Summary = summary ?? string.Empty,
                Detail = "行动未提交，世界时间与资源均未变化。"
            };

        private static PlayerActionOption FindOption(
            IList<PlayerActionOption> options,
            string actionId)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Id == actionId)
                {
                    return options[i];
                }
            }
            return null;
        }

        private static TaskInstanceState FindTask(
            WorldState world,
            string personId) =>
            world.Tasks.Find(item =>
                item.AssigneePersonId == personId &&
                item.DefinitionId == PrimaryTaskDefinitionId);

        private static JourneyState FindJourney(
            WorldState world,
            string personId) =>
            world.Journeys.Find(item => item.PersonId == personId);

        private static bool HasTraderPosition(
            WorldState world,
            string personId) =>
            world.Memberships.Exists(item =>
                item.PersonId == personId &&
                item.PositionId == "position.zhongshan_trader");

        private static PersonState FindPerson(
            WorldState world,
            string personId,
            bool required = true)
        {
            var person = world.People.Find(item => item.Id == personId);
            if (person == null && required)
            {
                throw new InvalidOperationException(
                    "Missing merchant-household person " + personId + ".");
            }
            return person;
        }

        private static FamilyState FindFamily(WorldState world, string familyId) =>
            world.Families.Find(item => item.Id == familyId) ??
            throw new InvalidOperationException(
                "Missing merchant-household family " + familyId + ".");

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId) =>
            world.Organizations.Find(item => item.Id == organizationId) ??
            throw new InvalidOperationException(
                "Missing merchant organization " + organizationId + ".");

        private static MarketListingState FindListing(
            WorldState world,
            string locationId,
            string commodityId) =>
            world.MarketListings.Find(item =>
                item.LocationId == locationId &&
                item.CommodityId == commodityId) ??
            throw new InvalidOperationException(
                "Missing market listing " + locationId + "/" + commodityId + ".");

        private static CommodityState FindCommodity(
            WorldState world,
            string commodityId) =>
            world.Commodities.Find(item => item.Id == commodityId) ??
            throw new InvalidOperationException(
                "Missing commodity " + commodityId + ".");

        private long CurrentCargoWeight(WorldState world, string personId) =>
            _trading.GetCargoWeight(world, personId);
    }
}
