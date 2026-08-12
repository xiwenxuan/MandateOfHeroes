using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum WorldAgentKind : byte
    {
        Person,
        Household,
        Organization,
        Government,
        Force,
        Settlement
    }

    public enum WorldActionValidationStatus : byte
    {
        Valid,
        Invalid,
        Deferred,
        PartiallyExecutable
    }

    public enum WorldSimulationLodTier : byte
    {
        Hot,
        Warm,
        Cold
    }

    public static class WorldSignalIds
    {
        public const string PopulationPressure = "mandate.signal.population_pressure";
        public const string FoodPressure = "mandate.signal.food_pressure";
        public const string HousingPressure = "mandate.signal.housing_pressure";
        public const string EmploymentPressure = "mandate.signal.employment_pressure";
        public const string InventoryPressure = "mandate.signal.inventory_pressure";
        public const string PricePressure = "mandate.signal.price_pressure";
        public const string LaborAvailability = "mandate.signal.labor_availability";
        public const string LandAvailability = "mandate.signal.land_availability";
        public const string WaterAvailability = "mandate.signal.water_availability";
        public const string TransportCapacity = "mandate.signal.transport_capacity";
        public const string RouteRisk = "mandate.signal.route_risk";
        public const string SecurityRisk = "mandate.signal.security_risk";
        public const string WarPressure = "mandate.signal.war_pressure";
        public const string GovernmentPressure = "mandate.signal.government_pressure";
        public const string ProfitOpportunity = "mandate.signal.profit_opportunity";
        public const string MigrationPressure = "mandate.signal.migration_pressure";
        public const string StoragePressure = "mandate.signal.storage_pressure";
        public const string ResourceOpportunity = "mandate.signal.resource_opportunity";
    }

    public static class WorldActionTypeIds
    {
        public const string NoAction = "mandate.action.no_action";
        public const string Observe = "mandate.action.observe";
        public const string AcquireLand = "mandate.action.acquire_land";
        public const string ReclaimLand = "mandate.action.reclaim_land";
        public const string BuildFacility = "mandate.action.build_facility";
        public const string AssignWorker = "mandate.action.assign_worker";
        public const string ChangeProduction = "mandate.action.change_production";
        public const string CreateTradeOrder = "mandate.action.create_trade_order";
        public const string CreateMarketBuyOrder = "mandate.action.create_market_buy_order";
        public const string CreateTransferOrder = "mandate.action.create_transfer_order";
        public const string CreateGovernmentPurchase = "mandate.action.create_government_purchase";
        public const string CreateMilitarySupplyOrder = "mandate.action.create_military_supply_order";
        public const string CreateShipment = "mandate.action.create_shipment";
        public const string MigrateHousehold = "mandate.action.migrate_household";
        public const string MovePerson = "mandate.action.move_person";
        public const string Invest = "mandate.action.invest";
        public const string RepairFacility = "mandate.action.repair_facility";
        public const string AbandonFacility = "mandate.action.abandon_facility";
        public const string EstablishFamilyCenter = "mandate.action.establish_family_center";
        public const string GovernmentPolicy = "mandate.action.government_policy";
        public const string MilitaryAction = "mandate.action.military_action";
    }

    public static class DecisionPolicyIds
    {
        public const string Rule = "mandate.policy.rule.v1";
        public const string Utility = "mandate.policy.utility.v1";
        public const string NeuralAdapter = "mandate.policy.neural_adapter.v1";
        public const string HistoricalConstraint = "mandate.policy.historical_constraint.v1";
        public const string RandomizedWrapper = "mandate.policy.randomized_wrapper.v1";

        public const string RuleBaseline = Rule;
        public const string UtilityV1 = Utility;
        public const string NeuralScoringV1 = NeuralAdapter;
        public const string RandomizedUtilityV1 =
            "mandate.policy.randomized_utility.v1";
    }

    public static class WorldDecisionGoalIds
    {
        public const string PreserveHousehold =
            "mandate.goal.preserve_household";
        public const string ExpandFamilyOrganization =
            "mandate.goal.expand_family_organization";
        public const string BuildMerchantFortune =
            "mandate.goal.build_merchant_fortune";
        public const string DevelopSettlement =
            "mandate.goal.develop_settlement";
        public const string GovernCounty =
            "mandate.goal.govern_county";
    }

    public static class WorldAgentPolicyProfileIds
    {
        public const string Household = "mandate.policy_profile.household.v1";
        public const string FamilyOrganization =
            "mandate.policy_profile.family_organization.v1";
        public const string Merchant = "mandate.policy_profile.merchant.v1";
        public const string Settlement = "mandate.policy_profile.settlement.v1";
        public const string CountyGovernment =
            "mandate.policy_profile.county_government.v1";
    }

    [Serializable]
    public sealed class WorldSignalValue
    {
        public string SignalId;
        public int ValueBasisPoints;
        public string EvidenceSummary;
    }

    [Serializable]
    public sealed class WorldDecisionContext
    {
        public string AgentId;
        public WorldAgentKind AgentKind;
        public string LocationId;
        public long AbsoluteDay;
        public long DecisionSequence;
        public List<WorldSignalValue> Signals = new List<WorldSignalValue>();
    }

    [Serializable]
    public sealed class WorldActionArgument
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public sealed class WorldActionIntent
    {
        public string Id;
        public string ActionTypeId;
        public string AgentId;
        public WorldAgentKind AgentKind;
        public string LocationId;
        public int BaseUtilityBasisPoints;
        public int ExpectedBenefitBasisPoints;
        public int CostBasisPoints;
        public int RiskBasisPoints;
        public List<WorldActionArgument> Arguments =
            new List<WorldActionArgument>();
    }

    [Serializable]
    public sealed class WorldDecisionMemoryEntryState
    {
        public string Id;
        public long Day;
        public string ActionId;
        public string ActionTypeId;
        public string ValidationReasonId;
        public bool Executed;
        public int ShortOutcomeBasisPoints;
        public int LongOutcomeBasisPoints;
    }

    [Serializable]
    public sealed class WorldActionValidationResult
    {
        public WorldActionValidationStatus Status;
        public string ReasonId;
        public string Explanation;
        public long ExecutableQuantity;

        public bool CanExecute =>
            Status == WorldActionValidationStatus.Valid ||
            Status == WorldActionValidationStatus.PartiallyExecutable;
    }

    [Serializable]
    public sealed class WorldDecisionAgentState
    {
        public string Id;
        public string AgentId;
        public WorldAgentKind AgentKind;
        public string PolicyId = DecisionPolicyIds.Rule;
        public string PolicyVersion = "1";
        public string ModelVersion = "none";
        public string ModelId = "none";
        public string PolicyProfileId = string.Empty;
        public string PrimaryGoalId = string.Empty;
        public int PrimaryGoalWeightBasisPoints = 5_000;
        public long DecisionSequence;
        public long LastDecisionDay = -1;
        public string LastActionId;
        public List<WorldDecisionMemoryEntryState> Memory =
            new List<WorldDecisionMemoryEntryState>();
    }

    [Serializable]
    public sealed class NeuralPolicyModelDefinition
    {
        public string ModelId;
        public string ModelVersion;
        public string FeatureSchemaVersion;
        public string ActionSchemaVersion;
        public string DatasetVersion;
        public string ConfigHash;
        public string WeightHash;
        public List<string> FeatureIds = new List<string>();
        public List<double> FeatureMinimums = new List<double>();
        public List<double> FeatureMaximums = new List<double>();
        public int HiddenSize;
        public List<double> HiddenWeights = new List<double>();
        public List<double> HiddenBiases = new List<double>();
        public List<double> OutputWeights = new List<double>();
        public double OutputBias;
    }

    [Serializable]
    public sealed class WorldSimulationLodState
    {
        public string Id;
        public string TargetKindId;
        public string TargetId;
        public WorldSimulationLodTier Tier;
        public long LastEvaluatedDay = -1;
        public long NextEvaluationDay;
    }

    public static class LivingWorldRuntimeRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world.WorldDecisionAgents == null ||
                world.WorldSimulationLodStates == null)
            {
                throw new InvalidOperationException(
                    "Living-world runtime collections cannot be null.");
            }

            var decisionIds = new HashSet<string>(StringComparer.Ordinal);
            var agentKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.WorldDecisionAgents.Count; i++)
            {
                var state = world.WorldDecisionAgents[i] ??
                    throw new InvalidOperationException(
                        "A world decision-agent state cannot be null.");
                _ = new StableId(state.Id);
                _ = new StableId(state.AgentId);
                _ = new StableId(state.PolicyId);
                if (!decisionIds.Add(state.Id) ||
                    !agentKeys.Add(state.AgentKind + "\n" + state.AgentId) ||
                    !Enum.IsDefined(typeof(WorldAgentKind), state.AgentKind) ||
                    state.DecisionSequence < 0 ||
                    state.LastDecisionDay < -1 ||
                    string.IsNullOrWhiteSpace(state.PolicyVersion) ||
                    string.IsNullOrWhiteSpace(state.ModelVersion) ||
                    string.IsNullOrWhiteSpace(state.ModelId) ||
                    state.PrimaryGoalWeightBasisPoints < 0 ||
                    state.PrimaryGoalWeightBasisPoints > 10_000 ||
                    state.Memory == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid world decision-agent state {state.Id}.");
                }


                if (!string.IsNullOrEmpty(state.PolicyProfileId))
                {
                    _ = new StableId(state.PolicyProfileId);
                }
                if (!string.IsNullOrEmpty(state.PrimaryGoalId))
                {
                    _ = new StableId(state.PrimaryGoalId);
                }
                var memoryIds = new HashSet<string>(StringComparer.Ordinal);
                for (var memoryIndex = 0;
                     memoryIndex < state.Memory.Count;
                     memoryIndex++)
                {
                    var memory = state.Memory[memoryIndex] ??
                        throw new InvalidOperationException(
                            $"Decision memory cannot be null for {state.Id}.");
                    _ = new StableId(memory.Id);
                    if (!memoryIds.Add(memory.Id) || memory.Day < 0 ||
                        string.IsNullOrWhiteSpace(memory.ActionId) ||
                        string.IsNullOrWhiteSpace(memory.ActionTypeId) ||
                        memory.ShortOutcomeBasisPoints < -10_000 ||
                        memory.ShortOutcomeBasisPoints > 10_000 ||
                        memory.LongOutcomeBasisPoints < -10_000 ||
                        memory.LongOutcomeBasisPoints > 10_000)
                    {
                        throw new InvalidOperationException(
                            $"Invalid decision memory {memory.Id} for {state.Id}.");
                    }
                }
            }

            var lodIds = new HashSet<string>(StringComparer.Ordinal);
            var targetKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.WorldSimulationLodStates.Count; i++)
            {
                var state = world.WorldSimulationLodStates[i] ??
                    throw new InvalidOperationException(
                        "A simulation LOD state cannot be null.");
                _ = new StableId(state.Id);
                _ = new StableId(state.TargetKindId);
                _ = new StableId(state.TargetId);
                if (!lodIds.Add(state.Id) ||
                    !targetKeys.Add(state.TargetKindId + "\n" + state.TargetId) ||
                    !Enum.IsDefined(typeof(WorldSimulationLodTier), state.Tier) ||
                    state.LastEvaluatedDay < -1 || state.NextEvaluationDay < 0)
                {
                    throw new InvalidOperationException(
                        $"Invalid simulation LOD state {state.Id}.");
                }
            }
        }


        public static void ValidateNeuralModel(
            NeuralPolicyModelDefinition model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            _ = new StableId(model.ModelId);
            if (string.IsNullOrWhiteSpace(model.ModelVersion) ||
                string.IsNullOrWhiteSpace(model.FeatureSchemaVersion) ||
                string.IsNullOrWhiteSpace(model.ActionSchemaVersion) ||
                string.IsNullOrWhiteSpace(model.DatasetVersion) ||
                string.IsNullOrWhiteSpace(model.ConfigHash) ||
                string.IsNullOrWhiteSpace(model.WeightHash) ||
                model.HiddenSize <= 0 || model.FeatureIds == null ||
                model.FeatureMinimums == null ||
                model.FeatureMaximums == null ||
                model.HiddenWeights == null || model.HiddenBiases == null ||
                model.OutputWeights == null || model.FeatureIds.Count == 0 ||
                model.FeatureMinimums.Count != model.FeatureIds.Count ||
                model.FeatureMaximums.Count != model.FeatureIds.Count ||
                model.HiddenWeights.Count !=
                    model.FeatureIds.Count * model.HiddenSize ||
                model.HiddenBiases.Count != model.HiddenSize ||
                model.OutputWeights.Count != model.HiddenSize)
            {
                throw new InvalidOperationException(
                    "Neural policy model contract is invalid.");
            }
            var featureIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < model.FeatureIds.Count; i++)
            {
                _ = new StableId(model.FeatureIds[i]);
                if (!featureIds.Add(model.FeatureIds[i]) ||
                    !IsFinite(model.FeatureMinimums[i]) ||
                    !IsFinite(model.FeatureMaximums[i]) ||
                    model.FeatureMaximums[i] <= model.FeatureMinimums[i])
                {
                    throw new InvalidOperationException(
                        $"Invalid neural feature {model.FeatureIds[i]}.");
                }
            }
            for (var i = 0; i < model.HiddenWeights.Count; i++)
            {
                if (!IsFinite(model.HiddenWeights[i]))
                {
                    throw new InvalidOperationException(
                        "Neural hidden weight is not finite.");
                }
            }
            for (var i = 0; i < model.HiddenBiases.Count; i++)
            {
                if (!IsFinite(model.HiddenBiases[i]) ||
                    !IsFinite(model.OutputWeights[i]))
                {
                    throw new InvalidOperationException(
                        "Neural model parameter is not finite.");
                }
            }
            if (!IsFinite(model.OutputBias))
            {
                throw new InvalidOperationException(
                    "Neural output bias is not finite.");
            }
        }

        private static bool IsFinite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
