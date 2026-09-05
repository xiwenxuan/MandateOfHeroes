using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Mandate.Domain
{
    [Serializable]
    public sealed class MerchantHouseholdGoalDefinition
    {
        public string Id;
        public string DisplayName;
        public string OriginLocationId;
        public string TargetLocationId;
        public string RouteId;
        public string CellRouteAssetRouteId;
        public ulong CellRouteOriginCellId64;
        public ulong CellRouteTargetCellId64;
        public string CellRouteMovementCapabilityId;
        public string CommodityId;
        public int CargoQuantity;
        public int ExpectedOriginUnitPrice;
        public int ExpectedTargetUnitPrice;
        public int IntelligenceReliabilityBasisPoints;
        public string IntelligenceSourceName;
        public string IssuerOrganizationId;
        public string CompanionPersonId;
        public int InitialFamilyDebt;
        public int PersonalReserveContribution;
        public int GuildAdvanceMoney;
        public int GuildAdvanceDebt;
        public int DeliveryCommission;
        public int LateCommission;
        public int DebtRepayment;
        public int CartInvestmentCost;
        public int CartCapacityGain;
        public int DurationDays;
        public string TravelEventId;
        public string DebtFollowupDefinitionId;
        public string CartFollowupDefinitionId;
    }

    [Serializable]
    public sealed class MerchantHouseholdTravelEventDefinition
    {
        public string Id;
        public string DisplayName;
        public string SpeakerRole;
        public int TriggerRemainingKilometers;
        public int HelpProvisionCost;
        public int GuardProvisionCost;
        public int RefuseProvisionCost;
        public int HelpTrustGain;
        public int GuardTrustChange;
        public int RefuseTrustChange;
        public int HelpCargoLossChanceBasisPoints;
        public int GuardInjuryChanceBasisPoints;
        public int GuardHealthLoss;
    }

    [Serializable]
    public sealed class MerchantHouseholdContentPackage
    {
        public string PackageId;
        public string Version;
        public List<MerchantHouseholdGoalDefinition> Goals =
            new List<MerchantHouseholdGoalDefinition>();
        public List<MerchantHouseholdTravelEventDefinition> TravelEvents =
            new List<MerchantHouseholdTravelEventDefinition>();
    }

    public sealed class MerchantHouseholdContentRegistry
    {
        private readonly Dictionary<string, MerchantHouseholdGoalDefinition>
            _goals = new Dictionary<string, MerchantHouseholdGoalDefinition>(
                StringComparer.Ordinal);
        private readonly Dictionary<string, MerchantHouseholdTravelEventDefinition>
            _events =
                new Dictionary<string, MerchantHouseholdTravelEventDefinition>(
                    StringComparer.Ordinal);

        public static MerchantHouseholdContentRegistry FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Merchant-household content JSON is empty.",
                    nameof(json));
            }
            var package = JsonConvert.DeserializeObject<
                MerchantHouseholdContentPackage>(json);
            return new MerchantHouseholdContentRegistry(package);
        }

        public static MerchantHouseholdContentRegistry CreateCore() =>
            new MerchantHouseholdContentRegistry(CreateCorePackage());

        public MerchantHouseholdContentRegistry(
            MerchantHouseholdContentPackage package)
        {
            if (package == null ||
                string.IsNullOrWhiteSpace(package.PackageId) ||
                string.IsNullOrWhiteSpace(package.Version))
            {
                throw new InvalidOperationException(
                    "Merchant-household package identity is missing.");
            }
            AddUnique(package.Goals, _goals, item => item.Id, "goal");
            AddUnique(package.TravelEvents, _events, item => item.Id, "event");
            Validate();
        }

        public MerchantHouseholdGoalDefinition GetGoal(string id)
        {
            if (!_goals.TryGetValue(id ?? string.Empty, out var value))
            {
                throw new InvalidOperationException(
                    $"Missing merchant-household goal content {id}.");
            }
            return value;
        }

        public MerchantHouseholdTravelEventDefinition GetTravelEvent(string id)
        {
            if (!_events.TryGetValue(id ?? string.Empty, out var value))
            {
                throw new InvalidOperationException(
                    $"Missing merchant-household travel event content {id}.");
            }
            return value;
        }

        private void Validate()
        {
            foreach (var pair in _goals)
            {
                var item = pair.Value;
                if (string.IsNullOrWhiteSpace(item.DisplayName) ||
                    string.IsNullOrWhiteSpace(item.OriginLocationId) ||
                    string.IsNullOrWhiteSpace(item.TargetLocationId) ||
                    string.IsNullOrWhiteSpace(item.RouteId) ||
                    string.IsNullOrWhiteSpace(item.CellRouteAssetRouteId) ||
                    item.CellRouteOriginCellId64 == 0 ||
                    item.CellRouteTargetCellId64 == 0 ||
                    item.CellRouteOriginCellId64 ==
                        item.CellRouteTargetCellId64 ||
                    !MovementCapabilityIds.All.Contains(
                        item.CellRouteMovementCapabilityId) ||
                    string.IsNullOrWhiteSpace(item.CommodityId) ||
                    string.IsNullOrWhiteSpace(item.IssuerOrganizationId) ||
                    string.IsNullOrWhiteSpace(item.TravelEventId) ||
                    item.CargoQuantity <= 0 ||
                    item.ExpectedOriginUnitPrice <= 0 ||
                    item.ExpectedTargetUnitPrice <= 0 ||
                    item.IntelligenceReliabilityBasisPoints < 0 ||
                    item.IntelligenceReliabilityBasisPoints > 10_000 ||
                    item.DurationDays <= 0 ||
                    !_events.ContainsKey(item.TravelEventId))
                {
                    throw new InvalidOperationException(
                        $"Merchant-household goal {item.Id} is invalid.");
                }
            }
            foreach (var pair in _events)
            {
                var item = pair.Value;
                if (string.IsNullOrWhiteSpace(item.DisplayName) ||
                    item.TriggerRemainingKilometers <= 0 ||
                    item.HelpProvisionCost < 0 ||
                    item.GuardProvisionCost < 0 ||
                    item.RefuseProvisionCost < 0 ||
                    item.HelpCargoLossChanceBasisPoints < 0 ||
                    item.HelpCargoLossChanceBasisPoints > 10_000 ||
                    item.GuardInjuryChanceBasisPoints < 0 ||
                    item.GuardInjuryChanceBasisPoints > 10_000 ||
                    item.GuardHealthLoss < 0)
                {
                    throw new InvalidOperationException(
                        $"Merchant-household event {item.Id} is invalid.");
                }
            }
        }

        private static void AddUnique<T>(
            IEnumerable<T> source,
            IDictionary<string, T> destination,
            Func<T, string> id,
            string kind)
        {
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Merchant-household {kind} collection is missing.");
            }
            foreach (var item in source)
            {
                var key = item == null ? string.Empty : id(item);
                if (string.IsNullOrWhiteSpace(key) ||
                    destination.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate or missing merchant-household {kind} ID {key}.");
                }
                destination.Add(key, item);
            }
        }

        private static MerchantHouseholdContentPackage CreateCorePackage()
        {
            var package = new MerchantHouseholdContentPackage
            {
                PackageId = "mandate.gameplay.merchant_household.p1",
                Version = "1.0.0"
            };
            package.TravelEvents.Add(new MerchantHouseholdTravelEventDefinition
            {
                Id = "event.m26p1.roadside_broken_cart",
                DisplayName = "滹沱河道旁的折轴车",
                SpeakerRole = "同行商人苏双",
                TriggerRemainingKilometers = 116,
                HelpProvisionCost = 2,
                GuardProvisionCost = 1,
                RefuseProvisionCost = 1,
                HelpTrustGain = 500,
                GuardTrustChange = 100,
                RefuseTrustChange = -300,
                HelpCargoLossChanceBasisPoints = 2_500,
                GuardInjuryChanceBasisPoints = 2_000,
                GuardHealthLoss = 800
            });
            package.Goals.Add(new MerchantHouseholdGoalDefinition
            {
                Id = "goal.m26p1.zhongshan_zhuo_household_recovery",
                DisplayName = "把中山布帛送到涿县，缓解家中债务",
                OriginLocationId = "location.zhongshan",
                TargetLocationId = "location.zhuo",
                RouteId = "route.zhuo_zhongshan",
                CellRouteAssetRouteId = "R003",
                CellRouteOriginCellId64 = 3_352_589,
                CellRouteTargetCellId64 = 3_160_413,
                CellRouteMovementCapabilityId =
                    MovementCapabilityIds.PackAnimal,
                CommodityId = "commodity.cloth",
                CargoQuantity = 6,
                ExpectedOriginUnitPrice = 165,
                ExpectedTargetUnitPrice = 195,
                IntelligenceReliabilityBasisPoints = 7_500,
                IntelligenceSourceName = "中山商行昨日行脚口信",
                IssuerOrganizationId = "organization.zhongshan_merchants",
                CompanionPersonId = "person.su_shuang",
                InitialFamilyDebt = 600,
                PersonalReserveContribution = 200,
                GuildAdvanceMoney = 400,
                GuildAdvanceDebt = 440,
                DeliveryCommission = 300,
                LateCommission = 150,
                DebtRepayment = 600,
                CartInvestmentCost = 500,
                CartCapacityGain = 40,
                DurationDays = 20,
                TravelEventId = "event.m26p1.roadside_broken_cart",
                DebtFollowupDefinitionId =
                    "task_definition.m26p1.rebuild_household_reserve",
                CartFollowupDefinitionId =
                    "task_definition.m26p1.expand_trade_route"
            });
            return package;
        }
    }

    public static class MerchantHouseholdContentIds
    {
        public const string FirstGoal =
            "goal.m26p1.zhongshan_zhuo_household_recovery";
    }
}
