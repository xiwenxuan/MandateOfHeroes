using System;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class LuoyangPlayerCommandTypeIds
    {
        public const string SeekWork = "player.seek_work";
        public const string Study = "player.study";
        public const string BuyProperty = "player.buy_property";
        public const string ExpandIndustry = "player.expand_industry";
        public const string BuildIndustry = "player.build_industry";
        public const string Trade = "player.trade";
        public const string AcceptOffice = "player.accept_office";
        public const string Enlist = "player.enlist";
    }

    /// <summary>Player-facing commands over the same executors used by AI.</summary>
    public sealed class Luoyang184PlayerCommandSystem
    {
        public LuoyangPlayerCommandRuntimeState Execute(
            Luoyang184LivingWorldRuntimeState runtime,
            uint personOrdinal,
            string commandTypeId,
            string targetId = null)
        {
            if (runtime == null || personOrdinal >= runtime.Workforce.Count)
                throw new ArgumentOutOfRangeException(nameof(personOrdinal));
            var command = new LuoyangPlayerCommandRuntimeState
            {
                Id = "player_command." + runtime.AbsoluteDay + "." +
                     runtime.PlayerCommands.Count.ToString("D6"),
                Day = runtime.AbsoluteDay,
                PersonOrdinal = personOrdinal,
                CommandTypeId = commandTypeId,
                TargetId = targetId ?? string.Empty,
                StatusId = "rejected"
            };
            try
            {
                switch (commandTypeId)
                {
                    case LuoyangPlayerCommandTypeIds.SeekWork:
                        command.ResultId = SeekWork(runtime, personOrdinal);
                        break;
                    case LuoyangPlayerCommandTypeIds.Study:
                        command.ResultId = Study(runtime, personOrdinal);
                        break;
                    case LuoyangPlayerCommandTypeIds.BuyProperty:
                        command.ResultId = BuyProperty(runtime, personOrdinal,
                            targetId);
                        break;
                    case LuoyangPlayerCommandTypeIds.ExpandIndustry:
                        command.ResultId = ExpandIndustry(runtime, personOrdinal,
                            targetId);
                        break;
                    case LuoyangPlayerCommandTypeIds.BuildIndustry:
                        command.ResultId = BuildIndustry(runtime, personOrdinal,
                            targetId);
                        break;
                    case LuoyangPlayerCommandTypeIds.Trade:
                        command.ResultId = Trade(runtime, personOrdinal);
                        break;
                    case LuoyangPlayerCommandTypeIds.AcceptOffice:
                        command.ResultId = AcceptOffice(runtime, personOrdinal);
                        break;
                    case LuoyangPlayerCommandTypeIds.Enlist:
                        command.ResultId = Enlist(runtime, personOrdinal);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported player command.");
                }
                command.StatusId = "completed";
            }
            catch (InvalidOperationException exception)
            {
                command.ResultId = exception.Message;
            }
            runtime.PlayerCommands.Add(command);
            return command;
        }

        private static string SeekWork(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal)
        {
            var person = runtime.Workforce[(int)ordinal];
            if (person.Status != LuoyangWorkforceStatus.Unemployed)
                throw new InvalidOperationException("人物当前不是失业状态。");
            var facility = runtime.Facilities.Where(item =>
                    item.AssignedWorkers < item.OptimalWorkers &&
                    item.ConditionBasisPoints > 0)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).FirstOrDefault();
            if (facility == null) throw new InvalidOperationException("当前没有空缺岗位。");
            person.Status = LuoyangWorkforceStatus.Assigned;
            person.FacilityIndex = (uint)facility.FacilityIndex;
            person.SocialRoleId = "role.artisan";
            person.CurrentActivityId = "activity.work";
            facility.AssignedWorkers++;
            runtime.CurrentUnemployedCount--;
            return facility.FacilityId;
        }

        private static string Study(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal)
        {
            var development = runtime.PersonDevelopment.Find(item =>
                item.PersonOrdinal == ordinal);
            if (development == null)
            {
                var person = runtime.Workforce[(int)ordinal];
                var household = runtime.Households[(int)person.HouseholdOrdinal];
                development = new LuoyangPersonDevelopmentRuntimeState
                {
                    PersonOrdinal = ordinal,
                    SocialRoleId = person.SocialRoleId,
                    CurrentActivityId = "activity.study",
                    ResidenceFacilityId = household.ResidenceFacilityIndex <
                        runtime.Facilities.Count
                        ? runtime.Facilities[(int)household.ResidenceFacilityIndex]
                            .FacilityId : string.Empty
                };
                var book = runtime.Inventories.Find(item =>
                    item.ProductId == "product.book.classics" &&
                    item.QuantityMilliunits > 0);
                if (book == null) throw new InvalidOperationException("没有可借阅的书籍。");
                development.BookInventoryIds.Add(book.Id);
                runtime.PersonDevelopment.Add(development);
            }
            development.StudyMinutes += 600;
            development.KnowledgeBasisPoints = Math.Min(10_000,
                development.KnowledgeBasisPoints + 10);
            return "study_session." + ordinal + "." + runtime.AbsoluteDay;
        }

        private static string BuyProperty(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal, string targetId)
        {
            var person = runtime.Workforce[(int)ordinal];
            var household = runtime.Households[(int)person.HouseholdOrdinal];
            var property = string.IsNullOrWhiteSpace(targetId)
                ? runtime.CellProperties.Where(item => item.OwnerId ==
                        runtime.GovernmentEconomy.OrganizationId)
                    .OrderBy(item => string.IsNullOrEmpty(item.FacilityId) ? 0 : 1)
                    .ThenBy(item => item.CellId64).FirstOrDefault()
                : runtime.CellProperties.Find(item =>
                    item.CellId64.ToString() == targetId);
            if (property == null) throw new InvalidOperationException("没有可购买地产。");
            return new Luoyang184PropertyConstructionRuntimeSystem().Transfer(
                runtime, property.CellId64, property.OwnerId,
                household.HouseholdId, 10, "person." + ordinal).Id;
        }

        private static string ExpandIndustry(
            Luoyang184LivingWorldRuntimeState runtime, uint ordinal,
            string targetId)
        {
            var person = runtime.Workforce[(int)ordinal];
            var household = runtime.Households[(int)person.HouseholdOrdinal];
            var facility = runtime.Facilities.Where(item =>
                    (string.IsNullOrEmpty(targetId) || item.FacilityId == targetId) &&
                    item.OwnerId == household.HouseholdId)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (facility == null)
                throw new InvalidOperationException("人物家庭没有可扩建产业。");
            return new Luoyang184PropertyConstructionRuntimeSystem().Start(
                runtime, LuoyangCompactConstructionKind.Expansion,
                facility.CellId64, facility.FacilityId, facility.DefinitionId,
                facility.OwnerId, "player.person." + ordinal, 30, 10).Id;
        }

        private static string BuildIndustry(
            Luoyang184LivingWorldRuntimeState runtime, uint ordinal,
            string targetId)
        {
            var person = runtime.Workforce[(int)ordinal];
            var household = runtime.Households[(int)person.HouseholdOrdinal];
            var property = runtime.CellProperties.Where(item =>
                    item.OwnerId == household.HouseholdId &&
                    string.IsNullOrEmpty(item.FacilityId) &&
                    (string.IsNullOrEmpty(targetId) ||
                     item.CellId64.ToString() == targetId))
                .OrderBy(item => item.CellId64).FirstOrDefault();
            if (property == null)
                throw new InvalidOperationException("人物家庭没有已购买的空地。");
            return new Luoyang184PropertyConstructionRuntimeSystem().Start(
                runtime, LuoyangCompactConstructionKind.NewBuild,
                property.CellId64, string.Empty,
                "facility.workshop.general", household.HouseholdId,
                "player.person." + ordinal, 30, 10).Id;
        }

        private static string Trade(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal)
        {
            var person = runtime.Workforce[(int)ordinal];
            var household = runtime.Households[(int)person.HouseholdOrdinal];
            var inventory = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    item.QuantityMilliunits > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            if (inventory == null || household.Wealth <= 0)
                throw new InvalidOperationException("市场库存或资金不足。");
            var quantity = Math.Min(inventory.QuantityMilliunits, 1_000L);
            var cost = Math.Min(household.Wealth, 1L);
            inventory.QuantityMilliunits -= quantity;
            household.Wealth -= cost;
            var market = runtime.Markets.FirstOrDefault(item =>
                item.ProductId == inventory.ProductId);
            if (market == null)
                throw new InvalidOperationException(
                    "The market inventory has no owning market account.");
            market.CashBalance += cost;
            market.RecentTradeQuantityMilliunits += quantity;
            market.RecentTradeValue += cost;
            household.FoodReserveMilliunits += quantity;
            var trade = new LuoyangMarketTradeRuntimeState
            {
                Id = "market_trade.player." + runtime.AbsoluteDay + "." +
                     runtime.MarketTrades.Count.ToString("D6"),
                Day = runtime.AbsoluteDay,
                ProductId = inventory.ProductId,
                BuyerId = household.HouseholdId,
                SellerId = inventory.OwnerId,
                SourceInventoryId = inventory.Id,
                QuantityMilliunits = quantity,
                UnitPrice = 1,
                MoneyTransferred = cost,
                TradeOrderId = "trade_order.player." + ordinal
            };
            runtime.MarketTrades.Add(trade);
            return trade.Id;
        }

        private static string AcceptOffice(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal)
        {
            var office = runtime.Offices.FirstOrDefault(item =>
                item.HolderPersonOrdinal >= runtime.Workforce.Count);
            if (office == null)
                throw new InvalidOperationException("当前没有空缺合法职位。");
            office.HolderPersonOrdinal = ordinal;
            var person = runtime.Workforce[(int)ordinal];
            person.Status = LuoyangWorkforceStatus.Official;
            person.SocialRoleId = "role.official";
            person.CurrentActivityId = "activity.government_work";
            return office.Id;
        }

        private static string Enlist(Luoyang184LivingWorldRuntimeState runtime,
            uint ordinal)
        {
            var person = runtime.Workforce[(int)ordinal];
            if (person.Status == LuoyangWorkforceStatus.MilitaryDuty)
                throw new InvalidOperationException("人物已经在军役中。");
            if (person.Status == LuoyangWorkforceStatus.Assigned &&
                person.FacilityIndex < runtime.Facilities.Count)
            {
                var former = runtime.Facilities[(int)person.FacilityIndex];
                former.AssignedWorkers = Math.Max(0,
                    former.AssignedWorkers - 1);
            }
            person.Status = LuoyangWorkforceStatus.MilitaryDuty;
            person.SocialRoleId = "role.soldier";
            person.CurrentActivityId = "activity.military_service";
            runtime.Forces[0].PermanentPersonCount++;
            return runtime.Forces[0].Id;
        }
    }
}
