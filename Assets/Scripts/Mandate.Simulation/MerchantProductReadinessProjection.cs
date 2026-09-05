using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MerchantPurchasePreviewView
    {
        public string ProductName;
        public int PlannedQuantity;
        public int CurrentUnitPrice;
        public int AvailableMarketStock;
        public long TotalCost;
        public long CashBefore;
        public long CashAfter;
        public long CurrentCargoWeight;
        public long AddedCargoWeight;
        public long CargoWeightAfter;
        public long CargoCapacity;
        public bool IsAlreadyPurchased;
        public bool CanPurchase;
        public string Blocker;
        public string RecoveryHint;
        public string OwnershipSummary;
    }

    public sealed class MerchantJourneyPreviewView
    {
        public string CarrierPersonName;
        public string CarrierName;
        public string CompanionName;
        public string OriginName;
        public string DestinationName;
        public int RouteDistanceKilometers;
        public int RouteSecurityBasisPoints;
        public int EstimatedTravelDays;
        public int RequiredProvisions;
        public int AvailableProvisions;
        public long CurrentCargoWeight;
        public long CargoCapacity;
        public bool IsInTransit;
        public int RemainingKilometers;
        public bool CanDepart;
        public string RoadStatus;
        public string KnownRisk;
        public string Blocker;
        public string RecoveryHint;
    }

    public sealed class MerchantSettlementView
    {
        public bool HasPurchase;
        public bool HasSale;
        public int PurchasedQuantity;
        public int SoldQuantity;
        public int SaleQuantity;
        public int RemainingQuantity;
        public int LostQuantity;
        public long PurchaseCost;
        public int CurrentSaleUnitPrice;
        public long ExpectedSaleRevenue;
        public long ExpectedCommission;
        public long ExpectedNetResult;
        public long ActualSaleRevenue;
        public long ActualCommission;
        public long ActualNetResult;
        public int TravelDays;
        public int ActualProvisionsUsed;
        public int DestinationMarketStock;
        public bool CanSell;
        public string Blocker;
        public string RecoveryHint;
        public string WorldImpactSummary;
        public string NextStep;
    }

    public sealed class MerchantOrganizationMemberView
    {
        public string PersonName;
        public string PositionName;
        public bool IsPlayer;
    }

    public sealed class MerchantOrganizationView
    {
        public string OrganizationName;
        public long Treasury;
        public int ReputationBasisPoints;
        public string HeadquartersName;
        public string ManagerName;
        public string PlayerPositionName;
        public long WarehouseQuantity;
        public long WarehouseWeight;
        public long WarehouseCapacity;
        public string WarehouseSummary;
        public string LongTermGoal;
        public List<MerchantOrganizationMemberView> Members =
            new List<MerchantOrganizationMemberView>();
    }

    public sealed class MerchantProductReadinessView
    {
        public string PlayerName;
        public string CurrentLocationName;
        public long PlayerCash;
        public int PlayerProvisions;
        public string PressureSummary;
        public string RecommendedNextStep;
        public MerchantPurchasePreviewView Purchase;
        public MerchantJourneyPreviewView Journey;
        public MerchantSettlementView Settlement;
        public MerchantOrganizationView Organization;
    }

    /// <summary>
    /// Rebuildable, read-only player projection over the existing merchant,
    /// market, inventory, journey, family and organization authorities.
    /// </summary>
    public static class MerchantProductReadinessProjectionSystem
    {
        public static MerchantProductReadinessView Build(
            WorldState world,
            string personId,
            MerchantHouseholdContentRegistry content)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var task = world.Tasks.Find(item =>
                item.AssigneePersonId == personId &&
                item.DefinitionId ==
                    MerchantHouseholdGameplayService.PrimaryTaskDefinitionId);
            if (task == null)
            {
                return null;
            }

            var goal = content.GetGoal(MerchantHouseholdContentIds.FirstGoal);
            var person = RequirePerson(world, personId);
            var family = world.Families.Find(item =>
                item.Id == person.FamilyId);
            var commodity = world.Commodities.Find(item =>
                item.Id == goal.CommodityId);
            var origin = RequireLocation(world, goal.OriginLocationId);
            var target = RequireLocation(world, goal.TargetLocationId);
            var originListing = RequireListing(
                world, goal.OriginLocationId, goal.CommodityId);
            var targetListing = RequireListing(
                world, goal.TargetLocationId, goal.CommodityId);
            var route = world.Routes.Find(item => item.Id == goal.RouteId);
            var journey = world.Journeys.Find(item =>
                item.PersonId == person.Id);
            var trading = new TradingSystem();
            var carriedQuantity = trading.GetQuantity(
                world, person.Id, goal.CommodityId);
            var cargoWeight = trading.GetCargoWeight(world, person.Id);
            var needed = Math.Max(0, goal.CargoQuantity - carriedQuantity);
            var addedWeight = commodity == null
                ? 0L
                : checked((long)commodity.UnitWeight * needed);
            var purchaseCost = checked((long)originListing.Price * needed);
            var purchaseBlocker = PurchaseBlocker(
                person, goal, originListing, needed, purchaseCost,
                cargoWeight, addedWeight);
            var purchaseRecords = FindTradeRecords(
                world, person.Id, goal.CommodityId, task.AcceptedDay, true);
            var saleRecords = FindTradeRecords(
                world, person.Id, goal.CommodityId, task.AcceptedDay, false);
            var purchasedQuantity = SumQuantity(purchaseRecords);
            var soldQuantity = SumQuantity(saleRecords);
            var actualPurchaseCost = -SumMoney(purchaseRecords);
            var actualSaleRevenue = SumMoney(saleRecords);
            var lostQuantity = Math.Max(
                0, purchasedQuantity - soldQuantity - carriedQuantity);
            var commission = Commission(
                goal, task, world.AbsoluteDay, carriedQuantity);
            var actualCommission = saleRecords.Count == 0
                ? 0L
                : Commission(goal, task, saleRecords[
                    saleRecords.Count - 1].Day, soldQuantity);
            var salePrice = person.LocationId == goal.TargetLocationId
                ? targetListing.Price
                : goal.ExpectedTargetUnitPrice;
            var expectedRevenue = checked((long)salePrice * carriedQuantity);
            var saleBlocker = SaleBlocker(
                person, goal, carriedQuantity,
                FindOrganization(world, goal.IssuerOrganizationId),
                commission);
            var travelDays = TravelDays(world, person.Id, task.AcceptedDay);
            var actualProvisionsUsed = checked(
                travelDays + EventProvisionCost(
                    world, person.Id,
                    content.GetTravelEvent(goal.TravelEventId)));

            return new MerchantProductReadinessView
            {
                PlayerName = person.DisplayName,
                CurrentLocationName = RequireLocation(
                    world, person.LocationId).DisplayName,
                PlayerCash = person.Wealth,
                PlayerProvisions = person.Provisions,
                PressureSummary = family == null
                    ? "以这次经营维持生计。"
                    : family.DisplayName + "现有家产" + family.Wealth +
                      "钱、债务" + family.Debt + "钱。",
                RecommendedNextStep = RecommendedNextStep(
                    world, person, task),
                Purchase = new MerchantPurchasePreviewView
                {
                    ProductName = commodity == null
                        ? "计划货物"
                        : commodity.DisplayName,
                    PlannedQuantity = needed,
                    CurrentUnitPrice = originListing.Price,
                    AvailableMarketStock = originListing.Stock,
                    TotalCost = purchaseCost,
                    CashBefore = person.Wealth,
                    CashAfter = person.Wealth - purchaseCost,
                    CurrentCargoWeight = cargoWeight,
                    AddedCargoWeight = addedWeight,
                    CargoWeightAfter = cargoWeight + addedWeight,
                    CargoCapacity = person.CargoCapacity,
                    IsAlreadyPurchased = purchaseRecords.Count > 0,
                    CanPurchase = string.IsNullOrEmpty(purchaseBlocker),
                    Blocker = purchaseBlocker,
                    RecoveryHint = PurchaseRecovery(purchaseBlocker),
                    OwnershipSummary = carriedQuantity <= 0
                        ? "成交后货物归人物家庭所有，并装入随行商队货舱。"
                        : "随行商队货舱现有" + carriedQuantity +
                          "匹布帛，归人物家庭持有。"
                },
                Journey = BuildJourney(
                    world, person, goal, route, journey,
                    cargoWeight, carriedQuantity),
                Settlement = new MerchantSettlementView
                {
                    HasPurchase = purchaseRecords.Count > 0,
                    HasSale = saleRecords.Count > 0,
                    PurchasedQuantity = purchasedQuantity,
                    SoldQuantity = soldQuantity,
                    SaleQuantity = carriedQuantity,
                    RemainingQuantity = carriedQuantity,
                    LostQuantity = lostQuantity,
                    PurchaseCost = actualPurchaseCost,
                    CurrentSaleUnitPrice = salePrice,
                    ExpectedSaleRevenue = expectedRevenue,
                    ExpectedCommission = commission,
                    ExpectedNetResult = checked(
                        expectedRevenue + commission - actualPurchaseCost),
                    ActualSaleRevenue = actualSaleRevenue,
                    ActualCommission = actualCommission,
                    ActualNetResult = checked(
                        actualSaleRevenue + actualCommission -
                        actualPurchaseCost),
                    TravelDays = travelDays,
                    ActualProvisionsUsed = actualProvisionsUsed,
                    DestinationMarketStock = targetListing.Stock,
                    CanSell = string.IsNullOrEmpty(saleBlocker),
                    Blocker = saleBlocker,
                    RecoveryHint = SaleRecovery(saleBlocker),
                    WorldImpactSummary = WorldImpact(
                        target.DisplayName, targetListing, saleRecords,
                        soldQuantity, actualCommission, family),
                    NextStep = RecommendedNextStep(world, person, task)
                },
                Organization = BuildOrganization(
                    world, person, goal, task)
            };
        }

        private static MerchantJourneyPreviewView BuildJourney(
            WorldState world,
            PersonState person,
            MerchantHouseholdGoalDefinition goal,
            RouteState route,
            JourneyState journey,
            long cargoWeight,
            int carriedQuantity)
        {
            var requiredProvisions = EstimatedTravelDays(route) + 1;
            var blocker = JourneyBlocker(
                person, goal, route, carriedQuantity,
                requiredProvisions);
            var companion = world.People.Find(item =>
                item.Id == goal.CompanionPersonId);
            var container = world.InventoryContainers.Find(item =>
                item.CarrierPersonId == person.Id &&
                item.KindId == "inventory_container.merchant_caravan");
            var freight = world.CivilianFreights.Find(item =>
                item.CarrierPersonId == person.Id &&
                item.PurposeId ==
                    CivilianFreightPurposeIds.MerchantOwnerCarriage &&
                item.Status != CivilianFreightStatus.Completed);
            var roadStatus = route == null
                ? "尚未掌握可用道路"
                : freight != null && freight.UsesCellRoute
                    ? freight.CellRouteWaiting
                        ? "全国格路受阻，车队正在原格等待道路恢复"
                        : "全国格路通行中，剩余约" +
                          (freight.CellRouteRemainingWeightedCentimetres +
                           99_999L) / 100_000L + "公里"
                    : "已知道路可通行，治安约" +
                      route.SecurityBasisPoints / 100 + "%";
            return new MerchantJourneyPreviewView
            {
                CarrierPersonName = person.DisplayName,
                CarrierName = container == null
                    ? "随行商队货舱（购货时登记）"
                    : "随行商队货舱",
                CompanionName = companion == null
                    ? "本次没有同行者"
                    : companion.DisplayName,
                OriginName = RequireLocation(
                    world, goal.OriginLocationId).DisplayName,
                DestinationName = RequireLocation(
                    world, goal.TargetLocationId).DisplayName,
                RouteDistanceKilometers = route == null
                    ? 0
                    : route.DistanceKilometers,
                RouteSecurityBasisPoints = route == null
                    ? 0
                    : route.SecurityBasisPoints,
                EstimatedTravelDays = EstimatedTravelDays(route),
                RequiredProvisions = requiredProvisions,
                AvailableProvisions = person.Provisions,
                CurrentCargoWeight = cargoWeight,
                CargoCapacity = person.CargoCapacity,
                IsInTransit = journey != null,
                RemainingKilometers = journey == null
                    ? 0
                    : journey.RemainingKilometers,
                CanDepart = journey == null && string.IsNullOrEmpty(blocker),
                RoadStatus = roadStatus,
                KnownRisk = "途中可能遇到道路事件、货损或伤病；" +
                    "具体结果尚不可知。",
                Blocker = blocker,
                RecoveryHint = JourneyRecovery(blocker)
            };
        }

        private static MerchantOrganizationView BuildOrganization(
            WorldState world,
            PersonState player,
            MerchantHouseholdGoalDefinition goal,
            TaskInstanceState task)
        {
            var organization = FindOrganization(
                world, goal.IssuerOrganizationId);
            var branch = world.MerchantBranches.Find(item =>
                item.OrganizationId == organization.Id &&
                item.IsHeadquarters);
            var warehouse = branch == null
                ? null
                : world.InventoryContainers.Find(item =>
                    item.Id == branch.InventoryContainerId);
            var view = new MerchantOrganizationView
            {
                OrganizationName = organization.DisplayName,
                Treasury = organization.Treasury,
                ReputationBasisPoints = organization.ReputationBasisPoints,
                HeadquartersName = RequireLocation(
                    world, organization.HeadquartersLocationId).DisplayName,
                ManagerName = branch == null
                    ? string.Empty
                    : PersonName(world, branch.ManagerPersonId),
                WarehouseCapacity = warehouse == null
                    ? 0L
                    : warehouse.CapacityWeight,
                LongTermGoal = task.Status == TaskStatus.Completed
                    ? FindFollowupGoal(world, player.Id)
                    : "完成首轮中山—涿县经营，再决定偿债或扩充货车。"
            };
            if (warehouse != null)
            {
                for (var i = 0; i < world.ProductBatches.Count; i++)
                {
                    var batch = world.ProductBatches[i];
                    if (batch.InventoryContainerId != warehouse.Id ||
                        batch.Quantity <= 0)
                    {
                        continue;
                    }
                    view.WarehouseQuantity = checked(
                        view.WarehouseQuantity + batch.Quantity);
                    view.WarehouseWeight = checked(
                        view.WarehouseWeight +
                        batch.Quantity * batch.UnitWeight);
                }
            }
            view.WarehouseSummary = view.WarehouseQuantity == 0
                ? "商号仓库目前为空；随身货物不会自动转入仓库。"
                : "商号仓库现有" + view.WarehouseQuantity +
                  "单位货物，载重" + view.WarehouseWeight + "/" +
                  view.WarehouseCapacity + "。";

            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.OrganizationId != organization.Id)
                {
                    continue;
                }
                var position = world.Positions.Find(item =>
                    item.Id == membership.PositionId);
                var person = world.People.Find(item =>
                    item.Id == membership.PersonId);
                if (person == null)
                {
                    continue;
                }
                var member = new MerchantOrganizationMemberView
                {
                    PersonName = person.DisplayName,
                    PositionName = position == null
                        ? "商号成员"
                        : position.DisplayName,
                    IsPlayer = person.Id == player.Id
                };
                view.Members.Add(member);
                if (member.IsPlayer)
                {
                    view.PlayerPositionName = member.PositionName;
                }
            }
            view.Members.Sort((left, right) =>
            {
                if (left.IsPlayer != right.IsPlayer)
                {
                    return left.IsPlayer ? -1 : 1;
                }
                return string.CompareOrdinal(
                    left.PersonName, right.PersonName);
            });
            return view;
        }

        private static string PurchaseBlocker(
            PersonState person,
            MerchantHouseholdGoalDefinition goal,
            MarketListingState listing,
            int needed,
            long cost,
            long cargoWeight,
            long addedWeight)
        {
            if (person.LocationId != goal.OriginLocationId)
            {
                return "当前不在中山集市，无法按这份计划采购。";
            }
            if (needed == 0)
            {
                return string.Empty;
            }
            if (listing.Stock < needed)
            {
                return "中山集市现货不足，无法装满计划中的6匹布帛。";
            }
            if (person.Wealth < cost)
            {
                return "现有现金不足以支付这批布帛。";
            }
            if (cargoWeight + addedWeight > person.CargoCapacity)
            {
                return "现有承运空间不足，装车后会超载。";
            }
            return string.Empty;
        }

        private static string PurchaseRecovery(string blocker)
        {
            if (string.IsNullOrEmpty(blocker)) return string.Empty;
            if (blocker.IndexOf("不在中山", StringComparison.Ordinal) >= 0)
                return "先返回中山，再进入当地市场。";
            if (blocker.IndexOf("现货不足", StringComparison.Ordinal) >= 0)
                return "等待市场补货后重新查看；失败不会扣钱或生成货物。";
            if (blocker.IndexOf("现金不足", StringComparison.Ordinal) >= 0)
                return "改用可用的筹资选择，或读取购货前存档调整决定。";
            return "先腾出随身载重；失败不会扣钱或推进时间。";
        }

        private static string JourneyBlocker(
            PersonState person,
            MerchantHouseholdGoalDefinition goal,
            RouteState route,
            int carriedQuantity,
            int requiredProvisions)
        {
            if (route == null)
            {
                return "尚未掌握连接中山与涿县的可用道路。";
            }
            if (person.LocationId != goal.OriginLocationId)
            {
                return "当前不在计划的中山起点。";
            }
            if (carriedQuantity < goal.CargoQuantity)
            {
                return "尚未备齐计划中的6匹布帛。";
            }
            if (person.Provisions < requiredProvisions)
            {
                return "口粮不足，无法保证完成预计行程。";
            }
            return string.Empty;
        }

        private static string JourneyRecovery(string blocker)
        {
            if (string.IsNullOrEmpty(blocker)) return string.Empty;
            if (blocker.IndexOf("道路", StringComparison.Ordinal) >= 0)
                return "先等待或取得新的合法路线；本次不会强行启程。";
            if (blocker.IndexOf("起点", StringComparison.Ordinal) >= 0)
                return "先返回中山。";
            if (blocker.IndexOf("布帛", StringComparison.Ordinal) >= 0)
                return "先完成采购并确认货物已经装入商队货舱。";
            return "先补足口粮；本次不会推进时间或移动人物。";
        }

        private static string SaleBlocker(
            PersonState person,
            MerchantHouseholdGoalDefinition goal,
            int quantity,
            OrganizationState organization,
            long commission)
        {
            if (person.LocationId != goal.TargetLocationId)
            {
                return "尚未抵达涿县，不能在目的市场交付。";
            }
            if (quantity <= 0)
            {
                return "商队已经没有可以交付的布帛。";
            }
            if (organization.Treasury < commission)
            {
                return "商行目前付不出本次佣金，货物不会被提前收走。";
            }
            return string.Empty;
        }

        private static string SaleRecovery(string blocker)
        {
            if (string.IsNullOrEmpty(blocker)) return string.Empty;
            if (blocker.IndexOf("抵达", StringComparison.Ordinal) >= 0)
                return "继续推进旅程，抵达后再查看当地实时价格。";
            if (blocker.IndexOf("没有", StringComparison.Ordinal) >= 0)
                return "查看途中货损记录；没有货物时不会生成重复销售。";
            return "等待商行恢复周转后重试；失败不会减少货物。";
        }

        private static string WorldImpact(
            string targetName,
            MarketListingState listing,
            List<TradeRecordState> sales,
            int soldQuantity,
            long commission,
            FamilyState family)
        {
            if (sales.Count == 0)
            {
                return "尚未成交；市场、家庭与商号资金没有被预览改动。";
            }
            var last = sales[sales.Count - 1];
            return targetName + "市场新增" + soldQuantity +
                "匹布帛供给；成交价" + last.UnitPrice +
                "钱，成交后的公开市价为" + listing.Price +
                "钱。商行实际支付佣金" + commission + "钱；" +
                (family == null
                    ? "家庭账保持现有状态。"
                    : "家庭债务现为" + family.Debt + "钱。");
        }

        private static string RecommendedNextStep(
            WorldState world,
            PersonState person,
            TaskInstanceState task)
        {
            if (task.Status == TaskStatus.Completed)
            {
                return FindFollowupGoal(world, person.Id);
            }
            switch (task.Progress)
            {
                case 0: return "前往行动页，选择自有本钱或商行垫款。";
                case 1: return "核对采购总价和载重后，买入6匹布帛。";
                case 2:
                    return world.Journeys.Exists(item =>
                            item.PersonId == person.Id)
                        ? "推进旅程；遇到途中事件时先作出选择。"
                        : "核对同行、口粮、路线和风险后启程。";
                case 3:
                    return world.Journeys.Exists(item =>
                            item.PersonId == person.Id)
                        ? "继续行程直至抵达涿县。"
                        : "查看涿县当前价格与预计盈亏，再交付布帛。";
                case 4: return "选择优先偿还家债，或购置货车扩大经营。";
                default: return "查看已生成的长期家庭目标。";
            }
        }

        private static string FindFollowupGoal(WorldState world,
            string personId)
        {
            var followup = world.Tasks.Find(item =>
                item.AssigneePersonId == personId &&
                item.Id.StartsWith("task.m26p1.followup.",
                    StringComparison.Ordinal) &&
                item.Status == TaskStatus.Active);
            if (followup == null)
            {
                return "首轮经营已经完成，等待新的家庭经营安排。";
            }
            var definition = world.TaskDefinitions.Find(item =>
                item.Id == followup.DefinitionId);
            return definition == null
                ? "新的家庭经营目标正在整理中。"
                : definition.DisplayName;
        }

        private static List<TradeRecordState> FindTradeRecords(
            WorldState world,
            string personId,
            string commodityId,
            long acceptedDay,
            bool purchase)
        {
            var result = world.TradeRecords.FindAll(item =>
                item.PersonId == personId &&
                item.CommodityId == commodityId &&
                item.Day >= acceptedDay &&
                item.IsPurchase == purchase);
            result.Sort((left, right) =>
            {
                var day = left.Day.CompareTo(right.Day);
                return day != 0
                    ? day
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static int SumQuantity(List<TradeRecordState> records)
        {
            var total = 0;
            for (var i = 0; i < records.Count; i++)
            {
                total = checked(total + records[i].Quantity);
            }
            return total;
        }

        private static long SumMoney(List<TradeRecordState> records)
        {
            long total = 0;
            for (var i = 0; i < records.Count; i++)
            {
                total = checked(total + records[i].MoneyChange);
            }
            return total;
        }

        private static int TravelDays(
            WorldState world, string personId, long acceptedDay)
        {
            var departed = world.LifeEvents.Find(item =>
                item.PrimaryPersonId == personId &&
                item.Id.StartsWith("life_event.m26p1.departed.",
                    StringComparison.Ordinal));
            if (departed == null || departed.Day < acceptedDay)
            {
                return 0;
            }
            var delivered = world.LifeEvents.Find(item =>
                item.PrimaryPersonId == personId &&
                (item.Id.StartsWith("life_event.m26p1.delivery.",
                     StringComparison.Ordinal) ||
                 item.Id.StartsWith("life_event.m26p1.partial_delivery.",
                     StringComparison.Ordinal)));
            var endDay = delivered == null
                ? world.AbsoluteDay
                : delivered.Day;
            return checked((int)Math.Max(0L, endDay - departed.Day));
        }

        private static int EventProvisionCost(
            WorldState world,
            string personId,
            MerchantHouseholdTravelEventDefinition travelEvent)
        {
            if (world.LifeEvents.Exists(item =>
                    item.PrimaryPersonId == personId &&
                    item.Id.StartsWith("life_event.m26p1.event_help.",
                        StringComparison.Ordinal)))
                return travelEvent.HelpProvisionCost;
            if (world.LifeEvents.Exists(item =>
                    item.PrimaryPersonId == personId &&
                    item.Id.StartsWith("life_event.m26p1.event_guard.",
                        StringComparison.Ordinal)))
                return travelEvent.GuardProvisionCost;
            if (world.LifeEvents.Exists(item =>
                    item.PrimaryPersonId == personId &&
                    item.Id.StartsWith("life_event.m26p1.event_refuse.",
                        StringComparison.Ordinal)))
                return travelEvent.RefuseProvisionCost;
            return 0;
        }

        private static long Commission(
            MerchantHouseholdGoalDefinition goal,
            TaskInstanceState task,
            long day,
            int quantity)
        {
            var full = day <= task.DeadlineDay
                ? goal.DeliveryCommission
                : goal.LateCommission;
            return checked((long)full * quantity / goal.CargoQuantity);
        }

        private static int EstimatedTravelDays(RouteState route)
        {
            var distance = route == null ? 140 : route.DistanceKilometers;
            var daily = checked(
                TravelSystem.KilometersPerSegment(TravelMode.Caravan) * 4);
            return checked((distance + daily - 1) / daily);
        }

        private static PersonState RequirePerson(
            WorldState world, string personId) =>
            world.People.Find(item => item.Id == personId) ??
            throw new InvalidOperationException(
                "The merchant player Person is missing.");

        private static LocationState RequireLocation(
            WorldState world, string locationId) =>
            world.Locations.Find(item => item.Id == locationId) ??
            throw new InvalidOperationException(
                "The merchant location is missing.");

        private static MarketListingState RequireListing(
            WorldState world, string locationId, string commodityId) =>
            world.MarketListings.Find(item =>
                item.LocationId == locationId &&
                item.CommodityId == commodityId) ??
            throw new InvalidOperationException(
                "The merchant market listing is missing.");

        private static OrganizationState FindOrganization(
            WorldState world, string organizationId) =>
            world.Organizations.Find(item => item.Id == organizationId) ??
            throw new InvalidOperationException(
                "The merchant organization is missing.");

        private static string PersonName(WorldState world, string personId)
        {
            var person = world.People.Find(item => item.Id == personId);
            return person == null ? string.Empty : person.DisplayName;
        }
    }
}
