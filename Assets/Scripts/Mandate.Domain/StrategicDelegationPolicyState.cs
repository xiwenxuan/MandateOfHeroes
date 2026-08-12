using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class StrategicDelegationOrderIds
    {
        public const string ManageAgriculture =
            "order.strategic.manage_agriculture";
        public const string ManageCommerce =
            "order.strategic.manage_commerce";
        public const string TransferFood =
            "order.strategic.transfer_food";
        public const string TransferFunds =
            "order.strategic.transfer_funds";
        public const string RecruitPeople =
            "order.strategic.recruit_people";
        public const string TrainForces =
            "order.strategic.train_forces";
        public const string FormMilitary =
            "order.strategic.form_military";
        public const string TransferMilitary =
            "order.strategic.transfer_military";
        public const string Investigate =
            "order.strategic.investigate";
        public const string BuildInfrastructure =
            "order.strategic.build_infrastructure";
        public const string LaunchCampaign =
            "order.strategic.launch_campaign";
    }

    public static class StrategicDelegationPriorityIds
    {
        public const string Agriculture = "priority.strategic.agriculture";
        public const string Commerce = "priority.strategic.commerce";
        public const string Defense = "priority.strategic.defense";
        public const string Logistics = "priority.strategic.logistics";
        public const string Recruitment = "priority.strategic.recruitment";
        public const string Technology = "priority.strategic.technology";
        public const string Training = "priority.strategic.training";
        public const string Expansion = "priority.strategic.expansion";
    }

    public static class StrategicDelegationPolicyIds
    {
        public const string Balanced =
            "policy.strategic_delegation.balanced";
        public const string CommerceGrowth =
            "policy.strategic_delegation.commerce_growth";
        public const string FrontierDefense =
            "policy.strategic_delegation.frontier_defense";
    }

    [Serializable]
    public sealed class StrategicDelegationPriorityWeightState
    {
        public string PriorityId;
        public int WeightBasisPoints;
    }

    [Serializable]
    public sealed class StrategicDelegationPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool AutoExecute;
        public List<string> AllowedOrderIds = new List<string>();
        public List<StrategicDelegationPriorityWeightState> PriorityWeights =
            new List<StrategicDelegationPriorityWeightState>();

        public void Validate()
        {
            _ = new StableId(Id);
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                throw new InvalidOperationException(
                    "A strategic delegation policy needs a display name.");
            }

            var orderIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < AllowedOrderIds.Count; i++)
            {
                var orderId = AllowedOrderIds[i];
                _ = new StableId(orderId);
                if (!orderIds.Add(orderId))
                {
                    throw new InvalidOperationException(
                        "Strategic delegation order IDs must be unique.");
                }
            }

            var priorityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < PriorityWeights.Count; i++)
            {
                var weight = PriorityWeights[i];
                if (weight == null)
                {
                    throw new InvalidOperationException(
                        "Strategic delegation priority weights cannot be null.");
                }

                _ = new StableId(weight.PriorityId);
                if (!priorityIds.Add(weight.PriorityId))
                {
                    throw new InvalidOperationException(
                        "Strategic delegation priority IDs must be unique.");
                }

                if (weight.WeightBasisPoints < -10_000 ||
                    weight.WeightBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        "Strategic delegation weights must be between -10000 and 10000 basis points.");
                }
            }
        }
    }

    public static class StrategicDelegationPolicyCatalog
    {
        public static List<StrategicDelegationPolicyDefinitionState> CreateCore()
        {
            var policies = new List<StrategicDelegationPolicyDefinitionState>
            {
                CreateBalanced(),
                CreateCommerceGrowth(),
                CreateFrontierDefense()
            };

            for (var i = 0; i < policies.Count; i++)
            {
                policies[i].Validate();
            }

            return policies;
        }

        private static StrategicDelegationPolicyDefinitionState CreateBalanced()
        {
            return Create(
                StrategicDelegationPolicyIds.Balanced,
                "均衡经营",
                "在民生、财政、训练和防务之间保持均衡，并允许有限进攻。",
                new[]
                {
                    StrategicDelegationOrderIds.ManageAgriculture,
                    StrategicDelegationOrderIds.ManageCommerce,
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationOrderIds.TransferFunds,
                    StrategicDelegationOrderIds.RecruitPeople,
                    StrategicDelegationOrderIds.TrainForces,
                    StrategicDelegationOrderIds.FormMilitary,
                    StrategicDelegationOrderIds.TransferMilitary,
                    StrategicDelegationOrderIds.Investigate,
                    StrategicDelegationOrderIds.BuildInfrastructure,
                    StrategicDelegationOrderIds.LaunchCampaign
                },
                Weight(StrategicDelegationPriorityIds.Agriculture, 6_000),
                Weight(StrategicDelegationPriorityIds.Commerce, 6_000),
                Weight(StrategicDelegationPriorityIds.Defense, 6_000),
                Weight(StrategicDelegationPriorityIds.Logistics, 6_000),
                Weight(StrategicDelegationPriorityIds.Recruitment, 5_000),
                Weight(StrategicDelegationPriorityIds.Technology, 5_000),
                Weight(StrategicDelegationPriorityIds.Training, 5_000),
                Weight(StrategicDelegationPriorityIds.Expansion, 3_000));
        }

        private static StrategicDelegationPolicyDefinitionState
            CreateCommerceGrowth()
        {
            return Create(
                StrategicDelegationPolicyIds.CommerceGrowth,
                "商贸振兴",
                "优先恢复市场、财政和粮运，不自动发动对外战役。",
                new[]
                {
                    StrategicDelegationOrderIds.ManageAgriculture,
                    StrategicDelegationOrderIds.ManageCommerce,
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationOrderIds.TransferFunds,
                    StrategicDelegationOrderIds.RecruitPeople,
                    StrategicDelegationOrderIds.Investigate,
                    StrategicDelegationOrderIds.BuildInfrastructure
                },
                Weight(StrategicDelegationPriorityIds.Agriculture, 5_000),
                Weight(StrategicDelegationPriorityIds.Commerce, 9_000),
                Weight(StrategicDelegationPriorityIds.Defense, 3_000),
                Weight(StrategicDelegationPriorityIds.Logistics, 8_000),
                Weight(StrategicDelegationPriorityIds.Recruitment, 4_000),
                Weight(StrategicDelegationPriorityIds.Technology, 6_000),
                Weight(StrategicDelegationPriorityIds.Training, 2_000),
                Weight(StrategicDelegationPriorityIds.Expansion, -5_000));
        }

        private static StrategicDelegationPolicyDefinitionState
            CreateFrontierDefense()
        {
            return Create(
                StrategicDelegationPolicyIds.FrontierDefense,
                "边郡固守",
                "优先补给、整军、训练与防御设施，不自动发动远征。",
                new[]
                {
                    StrategicDelegationOrderIds.ManageAgriculture,
                    StrategicDelegationOrderIds.TransferFood,
                    StrategicDelegationOrderIds.TransferFunds,
                    StrategicDelegationOrderIds.RecruitPeople,
                    StrategicDelegationOrderIds.TrainForces,
                    StrategicDelegationOrderIds.FormMilitary,
                    StrategicDelegationOrderIds.TransferMilitary,
                    StrategicDelegationOrderIds.Investigate,
                    StrategicDelegationOrderIds.BuildInfrastructure
                },
                Weight(StrategicDelegationPriorityIds.Agriculture, 6_000),
                Weight(StrategicDelegationPriorityIds.Commerce, 2_000),
                Weight(StrategicDelegationPriorityIds.Defense, 10_000),
                Weight(StrategicDelegationPriorityIds.Logistics, 9_000),
                Weight(StrategicDelegationPriorityIds.Recruitment, 7_000),
                Weight(StrategicDelegationPriorityIds.Technology, 4_000),
                Weight(StrategicDelegationPriorityIds.Training, 8_000),
                Weight(StrategicDelegationPriorityIds.Expansion, -7_000));
        }

        private static StrategicDelegationPolicyDefinitionState Create(
            string id,
            string displayName,
            string description,
            IEnumerable<string> allowedOrderIds,
            params StrategicDelegationPriorityWeightState[] weights)
        {
            return new StrategicDelegationPolicyDefinitionState
            {
                Id = id,
                DisplayName = displayName,
                Description = description,
                AutoExecute = true,
                AllowedOrderIds = new List<string>(allowedOrderIds),
                PriorityWeights = new List<StrategicDelegationPriorityWeightState>(
                    weights)
            };
        }

        private static StrategicDelegationPriorityWeightState Weight(
            string priorityId,
            int weightBasisPoints)
        {
            return new StrategicDelegationPriorityWeightState
            {
                PriorityId = priorityId,
                WeightBasisPoints = weightBasisPoints
            };
        }
    }
}
