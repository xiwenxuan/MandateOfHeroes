using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Compact adapter for the canonical WorldSignal -> DecisionContext ->
    /// candidate -> policy -> ActionIntent -> validation -> execution pipeline.
    /// It never mutates inventory, money, jobs or Facilities outside an explicit
    /// validated executor branch.
    /// </summary>
    public sealed class Luoyang184IntelligentAgentRuntimeSystem
    {
        public const string SystemId = "mandate.luoyang.184.intelligent_agent.v1";
        private const int AuditLimit = 20_000;

        public void BuildAgents(
            Luoyang184LivingWorldRuntimeState runtime,
            ILuoyang184LivingWorldSource source)
        {
            if (runtime == null || source == null)
                throw new ArgumentNullException("Agent bootstrap input is null.");
            var profileRandom = new NamedRandom(184UL);
            var agentIds = new HashSet<string>(
                runtime.IntelligentAgents.Select(item => item.Id),
                StringComparer.Ordinal);
            for (var i = 0; i < runtime.Households.Count; i++)
            {
                var household = runtime.Households[i];
                Add(runtime, agentIds, profileRandom, household.HouseholdId,
                    source.GetPersonId(household.HeadPersonOrdinal),
                    WorldAgentKind.Household,
                    LuoyangIntelligentAgentRole.Household,
                    WorldDecisionGoalIds.PreserveHousehold, 30, i);
            }
            for (var i = 0; i < runtime.FamilyOrganizations.Count; i++)
            {
                var family = runtime.FamilyOrganizations[i];
                Add(runtime, agentIds, profileRandom, family.Id, family.HeadPersonId,
                    WorldAgentKind.Organization,
                    LuoyangIntelligentAgentRole.FamilyOrganization,
                    WorldDecisionGoalIds.ExpandFamilyOrganization, 30, i);
            }
            for (var i = 0; i < runtime.ExternalSuppliers.Count; i++)
            {
                var supplier = runtime.ExternalSuppliers[i];
                Add(runtime, agentIds, profileRandom, supplier.OrganizationId,
                    supplier.ManagerPersonId, WorldAgentKind.Organization,
                    LuoyangIntelligentAgentRole.Merchant,
                    WorldDecisionGoalIds.BuildMerchantFortune, 14, i);
            }
            Add(runtime, agentIds, profileRandom, "settlement.luoyang.184",
                "person.luoyang.184.settlement_steward",
                WorldAgentKind.Settlement,
                LuoyangIntelligentAgentRole.SettlementDevelopment,
                WorldDecisionGoalIds.DevelopSettlement, 30, 0);
            Add(runtime, agentIds, profileRandom,
                runtime.GovernmentEconomy.OrganizationId,
                "person.luoyang.184.government_steward",
                WorldAgentKind.Government,
                LuoyangIntelligentAgentRole.Government,
                WorldDecisionGoalIds.GovernCounty, 30, 0);
            for (var i = 0; i < runtime.Facilities.Count; i++)
            {
                var facility = runtime.Facilities[i];
                Add(runtime, agentIds, profileRandom,
                    "facility_manager." + facility.FacilityId,
                    facility.OwnerId, WorldAgentKind.Organization,
                    LuoyangIntelligentAgentRole.FacilityManager,
                    WorldDecisionGoalIds.DevelopSettlement, 7, i);
            }
            runtime.IntelligentAgents.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            RebuildSchedule(runtime);
        }

        public void AdvanceDay(Luoyang184LivingWorldRuntimeState runtime)
        {
            var random = new NamedRandom(runtime.MasterSeed);
            var segmentTimer = Stopwatch.StartNew();
            var index = new RuntimeIndex(runtime);
            var sharedSignals = BuildSharedSignals(runtime, index);
            runtime.Performance.DecisionIndexMilliseconds +=
                segmentTimer.ElapsedMilliseconds;
            if (runtime.DecisionScheduleBuckets.Count != 210)
                RebuildSchedule(runtime);
            var bucket = runtime.DecisionScheduleBuckets[
                (int)(runtime.AbsoluteDay % 210)];
            var reschedule = new List<int>(bucket.AgentIndexes.Count);
            foreach (var agentIndex in bucket.AgentIndexes)
            {
                segmentTimer.Restart();
                var agent = runtime.IntelligentAgents[agentIndex];
                if (agent.NextDecisionDay > runtime.AbsoluteDay) continue;
                if (agent.Role == LuoyangIntelligentAgentRole.Household)
                {
                    AdvanceHouseholdAgent(runtime, index, random, agent,
                        sharedSignals);
                    runtime.Performance.HouseholdDecisionMilliseconds +=
                        segmentTimer.ElapsedMilliseconds;
                    reschedule.Add(agentIndex);
                    continue;
                }
                var context = BuildContext(runtime, agent, sharedSignals);
                var candidates = GenerateCandidates(runtime, agent, context);
                var selected = Select(runtime, random, agent, context, candidates);
                var validation = Validate(runtime, index, agent, selected);
                var resultEntityId = string.Empty;
                var executed = validation.CanExecute && Execute(
                    runtime, index, agent, selected, out resultEntityId);
                if (executed) agent.ExecutedActionCount++;
                else if (selected.ActionTypeId != WorldActionTypeIds.NoAction)
                    agent.RejectedActionCount++;
                agent.LastDecisionDay = runtime.AbsoluteDay;
                agent.LastActionTypeId = selected.ActionTypeId;
                agent.DecisionSequence++;
                agent.NextDecisionDay = checked(runtime.AbsoluteDay +
                    Cadence(agent.Role));
                AppendAudit(runtime, agent, context, candidates, selected,
                    validation.ReasonId, executed, resultEntityId);
                if (agent.Role == LuoyangIntelligentAgentRole.FacilityManager)
                    runtime.Performance.FacilityDecisionMilliseconds +=
                        segmentTimer.ElapsedMilliseconds;
                else
                    runtime.Performance.OrganizationDecisionMilliseconds +=
                        segmentTimer.ElapsedMilliseconds;
                reschedule.Add(agentIndex);
            }
            bucket.AgentIndexes.Clear();
            foreach (var agentIndex in reschedule)
            {
                var target = (int)(runtime.IntelligentAgents[agentIndex]
                    .NextDecisionDay % 210);
                runtime.DecisionScheduleBuckets[target].AgentIndexes.Add(
                    agentIndex);
            }
        }

        private static void RebuildSchedule(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.DecisionScheduleBuckets.Clear();
            for (var bucket = 0; bucket < 210; bucket++)
                runtime.DecisionScheduleBuckets.Add(
                    new LuoyangDecisionScheduleBucketState
                    {
                        BucketIndex = bucket
                    });
            for (var index = 0; index < runtime.IntelligentAgents.Count; index++)
                runtime.DecisionScheduleBuckets[(int)(
                    runtime.IntelligentAgents[index].NextDecisionDay % 210)]
                    .AgentIndexes.Add(index);
        }

        private static void AdvanceHouseholdAgent(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            NamedRandom random,
            LuoyangIntelligentAgentRuntimeState agent,
            SharedSignals signals)
        {
            var household = Household(runtime, index, agent);
            var actionType = WorldActionTypeIds.NoAction;
            var executed = false;
            var reason = "no_action";
            var resultId = string.Empty;
            if (household != null && household.FoodReserveMilliunits <
                household.DailyFoodDemandMilliunits * 10)
            {
                actionType = WorldActionTypeIds.CreateMarketBuyOrder;
                var action = new WorldActionIntent
                {
                    Id = "action." + agent.Id + "." + agent.DecisionSequence +
                         ".buy_food",
                    ActionTypeId = actionType,
                    AgentId = agent.SubjectId,
                    AgentKind = agent.AgentKind,
                    LocationId = "location.capital.luoyang"
                };
                var validation = Validate(runtime, index, agent, action);
                reason = validation.ReasonId;
                executed = validation.CanExecute && ExecuteHouseholdPurchase(
                    runtime, index, agent, out resultId);
            }
            if (!executed && household != null &&
                HasUnemployedMember(runtime, household) &&
                random.CheckBasisPoints(SystemId, new StableId(agent.Id),
                    runtime.AbsoluteDay, "seek_work." + agent.DecisionSequence,
                    Math.Min(8_000, signals.EmploymentPressure + 1_000)))
            {
                actionType = WorldActionTypeIds.AssignWorker;
                executed = ExecuteWorkerAssignment(runtime, index, agent,
                    out resultId);
                reason = executed ? "domain_preconditions_met" :
                    "job_unavailable";
            }
            if (executed) agent.ExecutedActionCount++;
            else if (actionType != WorldActionTypeIds.NoAction)
                agent.RejectedActionCount++;
            agent.LastDecisionDay = runtime.AbsoluteDay;
            agent.LastActionTypeId = actionType;
            agent.DecisionSequence++;
            agent.NextDecisionDay = runtime.AbsoluteDay + 30;
            if (executed && runtime.DecisionAudits.Count < AuditLimit)
            {
                runtime.DecisionAudits.Add(new LuoyangDecisionAuditState
                {
                    Id = "decision_audit." + runtime.AbsoluteDay + "." +
                        runtime.DecisionAudits.Count.ToString("D8"),
                    Day = runtime.AbsoluteDay,
                    AgentId = agent.Id,
                    Role = agent.Role,
                    SignalDigest = "food=" + signals.FoodPressure +
                        ";employment=" + signals.EmploymentPressure,
                    CandidateDigest = "buy_food;seek_work;migrate;no_action",
                    SelectedActionTypeId = actionType,
                    ValidationReasonId = reason,
                    Executed = true,
                    ResultEntityId = resultId
                });
            }
        }

        private static void Add(
            Luoyang184LivingWorldRuntimeState runtime,
            HashSet<string> agentIds,
            NamedRandom random,
            string subjectId,
            string representativePersonId,
            WorldAgentKind kind,
            LuoyangIntelligentAgentRole role,
            string goalId,
            int cadence,
            int subjectIndex)
        {
            if (string.IsNullOrWhiteSpace(subjectId)) return;
            var id = "agent." + role.ToString().ToLowerInvariant() + "." + subjectId;
            if (!agentIds.Add(id)) return;
            var stableId = new StableId(id);
            runtime.IntelligentAgents.Add(new LuoyangIntelligentAgentRuntimeState
            {
                Id = id,
                SubjectId = subjectId,
                RepresentativePersonId = representativePersonId ?? string.Empty,
                SubjectIndex = subjectIndex,
                AgentKind = kind,
                Role = role,
                GoalId = goalId,
                RiskPreferenceBasisPoints = random.Range(SystemId, stableId, 0,
                    "personality.risk", 1_000, 9_001),
                DiligenceBasisPoints = random.Range(SystemId, stableId, 0,
                    "personality.diligence", 2_000, 9_501),
                AmbitionBasisPoints = random.Range(SystemId, stableId, 0,
                    "personality.ambition", 1_000, 9_501),
                CompassionBasisPoints = random.Range(SystemId, stableId, 0,
                    "personality.compassion", 1_000, 9_501),
                NextDecisionDay = random.Range(SystemId, stableId, 0,
                    "cadence.offset", 1, cadence + 1)
            });
        }

        private static SharedSignals BuildSharedSignals(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index)
        {
            var demand = runtime.DailyFoodDemandMilliunits;
            var food = runtime.Inventories.Where(item => IsFood(item.ProductId))
                .Sum(item => item.QuantityMilliunits);
            var housing = runtime.Facilities.Sum(item =>
                (long)Math.Max(0, item.ResidentCapacity));
            var activePopulation = runtime.CurrentLocalPopulation;
            return new SharedSignals
            {
                FoodPressure = Shortage(food, demand * 30),
                InventoryPressure = Shortage(food, demand * 14),
                EmploymentPressure = runtime.Workforce.Count == 0 ? 0 :
                    (int)Math.Min(10_000L,
                        (long)index.UnemployedCount * 10_000 /
                        runtime.Workforce.Count),
                HousingPressure = housing <= 0 ? 10_000 :
                    (int)Math.Min(10_000L,
                        (long)activePopulation * 10_000L / housing),
                StoragePressure = StoragePressure(runtime),
                PricePressure = runtime.Markets.Count == 0 ? 0 :
                    runtime.Markets.Max(item => item.CurrentPriceBasisPoints) - 10_000,
                ProfitOpportunity = runtime.Markets.Count == 0 ? 0 :
                    Math.Min(10_000,
                        runtime.Markets.Max(item => item.CurrentPriceBasisPoints))
            };
        }

        private static WorldDecisionContext BuildContext(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            SharedSignals signals)
        {
            var context = new WorldDecisionContext
            {
                AgentId = agent.SubjectId,
                AgentKind = agent.AgentKind,
                LocationId = "location.capital.luoyang",
                AbsoluteDay = runtime.AbsoluteDay,
                DecisionSequence = agent.DecisionSequence
            };
            AddSignal(context, WorldSignalIds.FoodPressure,
                signals.FoodPressure, "real food stock / 30-day demand");
            AddSignal(context, WorldSignalIds.InventoryPressure,
                signals.InventoryPressure, "real inventory / 14-day demand");
            AddSignal(context, WorldSignalIds.EmploymentPressure,
                signals.EmploymentPressure,
                "unemployed permanent people");
            AddSignal(context, WorldSignalIds.HousingPressure,
                signals.HousingPressure,
                "population / active residential capacity proxy");
            AddSignal(context, WorldSignalIds.StoragePressure,
                signals.StoragePressure, "used / physical storage capacity");
            AddSignal(context, WorldSignalIds.PricePressure,
                signals.PricePressure,
                "highest current market price pressure");
            AddSignal(context, WorldSignalIds.ProfitOpportunity,
                signals.ProfitOpportunity,
                "market price and stock opportunity");
            AddSignal(context, WorldSignalIds.LaborAvailability,
                signals.EmploymentPressure,
                "available permanent workforce");
            AddSignal(context, WorldSignalIds.SecurityRisk, 2_000,
                "184 capital baseline security risk");
            return context;
        }

        private static IReadOnlyList<WorldActionIntent> GenerateCandidates(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            WorldDecisionContext context)
        {
            var result = new List<WorldActionIntent>
            {
                Action(agent, context, WorldActionTypeIds.NoAction, "monitor", 500, 0, 0)
            };
            switch (agent.Role)
            {
                case LuoyangIntelligentAgentRole.Household:
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.CreateMarketBuyOrder, "buy_food",
                        Signal(context, WorldSignalIds.FoodPressure) + 2_000,
                        2_000, 500));
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.AssignWorker, "seek_work",
                        Signal(context, WorldSignalIds.EmploymentPressure) + 1_000,
                        500, 300));
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.MigrateHousehold, "consider_migration",
                        Signal(context, WorldSignalIds.FoodPressure), 5_000, 4_000));
                    break;
                case LuoyangIntelligentAgentRole.FamilyOrganization:
                    result.Add(Action(agent, context, WorldActionTypeIds.Invest,
                        "estate_investment", 4_000 + agent.AmbitionBasisPoints / 2,
                        4_000, agent.RiskPreferenceBasisPoints));
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.CreateTransferOrder, "member_support",
                        Signal(context, WorldSignalIds.FoodPressure) +
                        agent.CompassionBasisPoints / 2, 2_000, 500));
                    break;
                case LuoyangIntelligentAgentRole.Merchant:
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.CreateTradeOrder, "restock_market",
                        Signal(context, WorldSignalIds.ProfitOpportunity) +
                        agent.AmbitionBasisPoints / 3, 3_000,
                        10_000 - agent.RiskPreferenceBasisPoints));
                    break;
                case LuoyangIntelligentAgentRole.SettlementDevelopment:
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.BuildFacility, "expand_storage",
                        10_000 + Math.Max(
                            Signal(context, WorldSignalIds.StoragePressure),
                            Signal(context, WorldSignalIds.HousingPressure)) / 2,
                        2_000, 1_000));
                    break;
                case LuoyangIntelligentAgentRole.Government:
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.CreateGovernmentPurchase,
                        "government_food_purchase",
                        Signal(context, WorldSignalIds.FoodPressure) +
                        agent.CompassionBasisPoints / 3, 5_000, 1_000));
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.GovernmentPolicy, "relief_policy",
                        Signal(context, WorldSignalIds.FoodPressure) +
                        agent.CompassionBasisPoints / 2, 4_000, 500));
                    break;
                case LuoyangIntelligentAgentRole.FacilityManager:
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.AssignWorker, "request_worker",
                        Signal(context, WorldSignalIds.EmploymentPressure) + 2_000,
                        1_000, 300));
                    result.Add(Action(agent, context,
                        WorldActionTypeIds.RepairFacility, "maintain_facility",
                        10_000 - FacilityCondition(runtime, agent.SubjectId),
                        2_500, 500));
                    break;
            }
            return result;
        }

        private static WorldActionIntent Select(
            Luoyang184LivingWorldRuntimeState runtime,
            NamedRandom random,
            LuoyangIntelligentAgentRuntimeState agent,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates)
        {
            WorldActionIntent best = candidates[0];
            var bestScore = int.MinValue;
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var score = candidate.ExpectedBenefitBasisPoints -
                            candidate.CostBasisPoints -
                            candidate.RiskBasisPoints *
                            (10_000 - agent.RiskPreferenceBasisPoints) / 10_000;
                score += agent.DiligenceBasisPoints / 20;
                score += random.Range(SystemId, new StableId(agent.Id),
                    runtime.AbsoluteDay,
                    candidate.ActionTypeId + ".sequence." + agent.DecisionSequence,
                    -750, 751, checked((uint)index));
                if (score > bestScore || score == bestScore &&
                    string.CompareOrdinal(candidate.Id, best.Id) < 0)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
            return best;
        }

        private static WorldActionValidationResult Validate(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            WorldActionIntent action)
        {
            if (action.ActionTypeId == WorldActionTypeIds.NoAction)
                return Valid("no_action");
            if (agent.Role == LuoyangIntelligentAgentRole.Household)
            {
                var household = Household(runtime, index, agent);
                if (household == null) return Invalid("missing_household");
                if (action.ActionTypeId == WorldActionTypeIds.CreateMarketBuyOrder &&
                    (household.Wealth <= 0 || !index.HasMarketFood()))
                    return Invalid("household_market_terms_unavailable");
                if (action.ActionTypeId == WorldActionTypeIds.AssignWorker &&
                    !HasUnemployedMember(runtime, household))
                    return Invalid("household_has_no_unemployed_member");
                if (action.ActionTypeId == WorldActionTypeIds.MigrateHousehold)
                    return Deferred("route_and_residence_required");
            }
            if (agent.Role == LuoyangIntelligentAgentRole.FamilyOrganization)
            {
                index.Families.TryGetValue(agent.SubjectId, out var family);
                if (family == null || family.Funds <= 0)
                    return Invalid("family_funds_unavailable");
            }
            if (agent.Role == LuoyangIntelligentAgentRole.Merchant &&
                !index.HasMerchantStock(agent.SubjectId))
                return Invalid("merchant_has_no_real_stock");
            if (agent.Role == LuoyangIntelligentAgentRole.Government &&
                runtime.GovernmentEconomy.Treasury <= 0)
                return Invalid("government_treasury_empty");
            if (agent.Role == LuoyangIntelligentAgentRole.SettlementDevelopment &&
                runtime.Inventories.Where(item =>
                        (item.ProductId == CoreProductionContent.TimberMaterialProductId ||
                         item.ProductId == "product.reference.building_material" ||
                         item.ProductId == "product.material.iron") &&
                        item.QuantityMilliunits >= 10_000)
                    .Select(item => item.ProductId).Distinct(
                        StringComparer.Ordinal).Count() < 2)
                return Deferred("real_construction_material_unavailable");
            if (agent.Role == LuoyangIntelligentAgentRole.FacilityManager &&
                action.ActionTypeId == WorldActionTypeIds.AssignWorker &&
                index.UnemployedCount <= 0)
                return Deferred("no_unemployed_worker_available");
            return Valid("domain_preconditions_met");
        }

        private static bool Execute(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            WorldActionIntent action,
            out string resultEntityId)
        {
            resultEntityId = string.Empty;
            switch (action.ActionTypeId)
            {
                case WorldActionTypeIds.NoAction:
                    return false;
                case WorldActionTypeIds.CreateMarketBuyOrder:
                    return ExecuteHouseholdPurchase(runtime, index, agent, out resultEntityId);
                case WorldActionTypeIds.AssignWorker:
                    return ExecuteWorkerAssignment(runtime, index, agent, out resultEntityId);
                case WorldActionTypeIds.CreateTransferOrder:
                    return ExecuteFamilySupport(runtime, index, agent, out resultEntityId);
                case WorldActionTypeIds.Invest:
                    return ExecuteFamilyInvestment(runtime, index, agent, out resultEntityId);
                case WorldActionTypeIds.CreateTradeOrder:
                    return ExecuteMerchantTrade(runtime, agent,
                        out resultEntityId);
                case WorldActionTypeIds.BuildFacility:
                    return ExecuteCompactExpansion(runtime, index, agent,
                        out resultEntityId);
                case WorldActionTypeIds.CreateGovernmentPurchase:
                    return ExecuteGovernmentPurchase(runtime, agent,
                        out resultEntityId);
                case WorldActionTypeIds.GovernmentPolicy:
                    return ExecuteRelief(runtime, agent, out resultEntityId);
                case WorldActionTypeIds.RepairFacility:
                    return ExecuteMaintenance(runtime, index, agent,
                        out resultEntityId);
                default:
                    return false;
            }
        }

        private static bool ExecuteHouseholdPurchase(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            var household = Household(runtime, index, agent);
            var inventory = index.NextMarketFoodInventory();
            if (household == null || inventory == null) return false;
            var market = index.Market(inventory.ProductId);
            var unitPrice = Math.Max(1L, (market?.BasePrice ?? 1) *
                (market?.CurrentPriceBasisPoints ?? 10_000) / 10_000L);
            var affordable = household.Wealth * 1_000 / unitPrice;
            var desired = Math.Max(1_000L, household.DailyFoodDemandMilliunits * 7 -
                household.FoodReserveMilliunits);
            var quantity = Math.Min(inventory.QuantityMilliunits,
                Math.Min(affordable, desired));
            if (quantity <= 0) return false;
            quantity = new LuoyangFormalEconomySystem().TransferToHousehold(
                runtime, inventory.Id, agent.SubjectIndex,
                inventory.ProductId, quantity,
                InventoryTransactionType.FoodMarketTransferred,
                "market.household." + runtime.AbsoluteDay + "." +
                agent.SubjectIndex + "." + inventory.ProductId);
            if (quantity <= 0) return false;
            var cost = checked((quantity * unitPrice + 999) / 1_000);
            household.Wealth -= cost;
            household.CumulativeMoneySpent += cost;
            if (market != null)
            {
                market.CashBalance += cost;
                market.RecentTradeQuantityMilliunits += quantity;
                market.RecentTradeValue += cost;
                market.TransferredMilliunits += quantity;
            }
            var trade = index.HouseholdBatchTrade(inventory);
            if (trade == null)
            {
                trade = new LuoyangMarketTradeRuntimeState
                {
                    Id = "market_trade.household_batch." +
                        runtime.AbsoluteDay + "." + inventory.ProductId,
                    Day = runtime.AbsoluteDay,
                    ProductId = inventory.ProductId,
                    BuyerId = "household.batch.luoyang.184",
                    SellerId = inventory.OwnerId,
                    SourceInventoryId = inventory.Id,
                    UnitPrice = unitPrice,
                    TradeOrderId = "trade_order.household_batch." +
                        runtime.AbsoluteDay + "." + inventory.ProductId
                };
                runtime.MarketTrades.Add(trade);
                index.RegisterHouseholdBatchTrade(inventory, trade);
            }
            trade.QuantityMilliunits += quantity;
            trade.MoneyTransferred += cost;
            resultId = trade.Id;
            return true;
        }

        private static bool ExecuteWorkerAssignment(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            LuoyangWorkforceAssignmentState person = null;
            if (agent.Role == LuoyangIntelligentAgentRole.Household)
            {
                var household = Household(runtime, index, agent);
                if (household != null)
                    person = Members(runtime, household).FirstOrDefault(item =>
                        item.Status == LuoyangWorkforceStatus.Unemployed);
            }
            else
            {
                person = index.NextUnemployed();
            }
            var facility = index.NextFacilityWithVacancy();
            if (person == null || facility == null) return false;
            person.Status = LuoyangWorkforceStatus.Assigned;
            person.FacilityIndex = checked((uint)facility.FacilityIndex);
            facility.AssignedWorkers++;
            index.MarkEmployed(person);
            resultId = "job." + person.PersonOrdinal + "." + facility.FacilityId;
            return true;
        }

        private static bool ExecuteFamilySupport(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            index.Families.TryGetValue(agent.SubjectId, out var family);
            if (family == null || family.Funds <= 0) return false;
            var household = runtime.Households.Where(item =>
                    item.FamilyOrganizationIndex == family.Index)
                .OrderBy(item => item.FoodSecurityBasisPoints)
                .ThenBy(item => item.HouseholdOrdinal).FirstOrDefault();
            if (household == null) return false;
            var amount = Math.Min(family.Funds, Math.Max(1L,
                household.DailyFoodDemandMilliunits / 1_000));
            family.Funds -= amount;
            family.MemberSupportPaid += amount;
            household.Wealth += amount;
            family.LastStrategyId = "family.support_member";
            resultId = "family_support." + family.Id + "." +
                household.HouseholdId + "." + runtime.AbsoluteDay;
            return true;
        }

        private static bool ExecuteFamilyInvestment(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            index.Families.TryGetValue(agent.SubjectId, out var family);
            if (family == null || family.Funds < 10) return false;
            var amount = Math.Max(10L, family.Funds / 100);
            family.Funds -= amount;
            family.InvestmentPaid += amount;
            family.AssetValue += amount;
            var market = runtime.Markets.OrderBy(item => item.ProductId,
                StringComparer.Ordinal).FirstOrDefault();
            if (market == null)
            {
                family.Funds += amount;
                family.InvestmentPaid -= amount;
                family.AssetValue -= amount;
                return false;
            }
            market.CashBalance += amount;
            market.RecentTradeValue += amount;
            family.LastStrategyId = "family.invest_estate";
            resultId = "family_investment." + family.Id + "." + runtime.AbsoluteDay;
            return true;
        }

        private static bool ExecuteCompactExpansion(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            if (runtime.ConstructionProjects.Exists(item =>
                    !item.Completed && !item.Cancelled &&
                    item.OwnerId == runtime.GovernmentEconomy.OrganizationId))
                return false;
            var property = runtime.CellProperties.Where(item =>
                    item.OwnerId == runtime.GovernmentEconomy.OrganizationId &&
                    item.BuildingRightHolderId == item.OwnerId &&
                    string.IsNullOrEmpty(item.FacilityId) &&
                    !runtime.ConstructionProjects.Exists(project =>
                        !project.Completed && !project.Cancelled &&
                        project.CellId64 == item.CellId64))
                .OrderBy(item => item.CellId64)
                .FirstOrDefault();
            if (property == null) return false;
            try
            {
                const string blueprintId =
                    "blueprint.han.warehouse.general.v1";
                var visual = new LuoyangVisualPresentationSystem();
                var orderCount = runtime.SupplyOrders.Count;
                var arrival = visual.OrderMissingConstructionMaterials(runtime,
                    blueprintId, runtime.GovernmentEconomy.OrganizationId,
                    agent.Id);
                if (arrival > runtime.AbsoluteDay)
                {
                    resultId = runtime.SupplyOrders.Skip(orderCount)
                        .Select(item => item.Id).FirstOrDefault() ?? string.Empty;
                    return !string.IsNullOrEmpty(resultId);
                }
                var project = visual.StartFromBlueprint(runtime, blueprintId,
                    property.CellId64,
                    runtime.GovernmentEconomy.OrganizationId, agent.Id);
                resultId = project.Id;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool ExecuteMerchantTrade(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            var supplier = runtime.ExternalSuppliers.Where(item =>
                    item.OrganizationId == agent.SubjectId &&
                    item.Level != LuoyangSupplierMaterializationLevel
                        .DeferredExternalTrade &&
                    item.InventoryQuantityMilliunits > 0)
                .OrderByDescending(item => item.InventoryQuantityMilliunits)
                .ThenBy(item => item.SupplierId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (supplier == null) return false;
            var destination = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    item.ProductId == supplier.ProductId)
                .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            var market = runtime.Markets.Find(item =>
                item.ProductId == supplier.ProductId);
            if (destination == null || market == null) return false;
            var shipped = Math.Min(supplier.InventoryQuantityMilliunits,
                Math.Min(destination.CapacityMilliunits -
                         destination.QuantityMilliunits, 500_000));
            var unitPrice = Math.Max(1L,
                market.BasePrice * 8L * market.CurrentPriceBasisPoints /
                100_000L);
            shipped = Math.Min(shipped,
                market.CashBalance * 1_000 / unitPrice);
            if (shipped <= 0) return false;
            var cost = checked((shipped * unitPrice + 999) / 1_000);
            var carrier = Math.Min(shipped, supplier.TravelDays * 2_000L);
            var natural = (shipped - carrier) *
                supplier.NaturalLossBasisPoints / 10_000;
            var risk = (shipped - carrier - natural) *
                supplier.RiskLossBasisPoints / 20_000;
            var delivered = shipped - carrier - natural - risk;
            if (delivered <= 0) return false;
            var orderId = "trade_supply_order." + runtime.AbsoluteDay + "." +
                runtime.SupplyOrders.Count.ToString("D6");
            var shipmentId = "trade_shipment." + runtime.AbsoluteDay + "." +
                runtime.Shipments.Count.ToString("D6");
            if (LuoyangFormalEconomySystem.IsFood(supplier.ProductId))
                new LuoyangFormalEconomySystem().DispatchFreight(runtime,
                    supplier.InventoryId, shipmentId, supplier.ProductId,
                    shipped, checked(carrier + natural + risk),
                    supplier.ManagerPersonId);
            else
                supplier.InventoryQuantityMilliunits -= shipped;
            market.CashBalance -= cost;
            supplier.CashBalance += cost;
            supplier.CumulativeSalesRevenue += cost;
            supplier.CumulativeDispatchedMilliunits += shipped;
            runtime.SupplyOrders.Add(new LuoyangSupplyOrderRuntimeState
            {
                Id = orderId,
                RequestedDay = runtime.AbsoluteDay,
                ProductId = supplier.ProductId,
                SupplierId = supplier.SupplierId,
                DestinationInventoryId = destination.Id,
                RequestedQuantityMilliunits = shipped,
                DispatchedQuantityMilliunits = shipped,
                UnitPrice = unitPrice,
                PurchaseCost = cost,
                Status = LuoyangSupplyOrderStatus.InTransit,
                ShipmentId = shipmentId,
                RequestedByAgentId = agent.Id,
                ReasonId = "merchant.price_stock_opportunity"
            });
            runtime.Shipments.Add(new LuoyangShipmentRuntimeState
            {
                Id = shipmentId,
                OrderId = orderId,
                ProductId = supplier.ProductId,
                SupplierId = supplier.SupplierId,
                SourceInventoryId = supplier.InventoryId,
                DestinationInventoryId = destination.Id,
                RouteId = supplier.RouteId,
                CarrierPersonId = supplier.ManagerPersonId,
                DispatchDay = runtime.AbsoluteDay,
                ArrivalDay = runtime.AbsoluteDay + supplier.TravelDays,
                ShippedQuantityMilliunits = shipped,
                CarrierConsumptionMilliunits = carrier,
                NaturalLossMilliunits = natural,
                RiskLossMilliunits = risk,
                DeliveredQuantityMilliunits = delivered,
                PurchaseCost = cost
            });
            resultId = shipmentId;
            return true;
        }

        private static bool ExecuteGovernmentPurchase(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            var marketInventory = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                    IsFood(item.ProductId) && item.QuantityMilliunits > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            if (marketInventory == null) return false;
            var market = runtime.Markets.Find(item =>
                item.ProductId == marketInventory.ProductId);
            var unitPrice = Math.Max(1L, (market?.BasePrice ?? 1) *
                (market?.CurrentPriceBasisPoints ?? 10_000) / 10_000L);
            var quantity = Math.Min(marketInventory.QuantityMilliunits,
                Math.Min(1_000_000L,
                    runtime.GovernmentEconomy.Treasury * 1_000 / unitPrice));
            if (quantity <= 0) return false;
            var cost = checked((quantity * unitPrice + 999) / 1_000);
            var governmentInventory = runtime.Inventories.Find(item =>
                item.OwnerKind == LuoyangInventoryOwnerKind.Government &&
                item.ProductId == marketInventory.ProductId);
            if (governmentInventory == null)
            {
                governmentInventory = new LuoyangInventoryBalanceState
                {
                    Id = "inventory.government.luoyang.184." +
                        marketInventory.ProductId,
                    OwnerKind = LuoyangInventoryOwnerKind.Government,
                    OwnerId = runtime.GovernmentEconomy.OrganizationId,
                    FacilityId = marketInventory.FacilityId,
                    ProductId = marketInventory.ProductId,
                    CapacityMilliunits = marketInventory.CapacityMilliunits
                };
                runtime.Inventories.Add(governmentInventory);
            }
            quantity = Math.Min(quantity, governmentInventory.CapacityMilliunits -
                governmentInventory.QuantityMilliunits);
            if (quantity <= 0) return false;
            cost = checked((quantity * unitPrice + 999) / 1_000);
            quantity = new LuoyangFormalEconomySystem().Transfer(runtime,
                marketInventory.Id, governmentInventory.Id,
                marketInventory.ProductId, quantity,
                InventoryTransactionType.FoodMarketTransferred,
                "market.government." + runtime.AbsoluteDay + "." +
                agent.DecisionSequence);
            if (quantity <= 0) return false;
            cost = checked((quantity * unitPrice + 999) / 1_000);
            runtime.GovernmentEconomy.Treasury -= cost;
            runtime.GovernmentEconomy.PurchaseExpense += cost;
            runtime.GovernmentEconomy.CurrentFoodPolicyId =
                "government.food.procurement";
            if (market != null) market.CashBalance += cost;
            var trade = new LuoyangMarketTradeRuntimeState
            {
                Id = "market_trade.government." + runtime.AbsoluteDay + "." +
                    runtime.MarketTrades.Count.ToString("D6"),
                Day = runtime.AbsoluteDay,
                ProductId = marketInventory.ProductId,
                BuyerId = runtime.GovernmentEconomy.OrganizationId,
                SellerId = marketInventory.OwnerId,
                SourceInventoryId = marketInventory.Id,
                QuantityMilliunits = quantity,
                UnitPrice = unitPrice,
                MoneyTransferred = cost,
                TradeOrderId = "government_purchase." + agent.DecisionSequence
            };
            runtime.MarketTrades.Add(trade);
            resultId = trade.Id;
            return true;
        }

        private static bool ExecuteRelief(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            var inventory = runtime.Inventories.Where(item =>
                    item.OwnerKind == LuoyangInventoryOwnerKind.Government &&
                    IsFood(item.ProductId) && item.QuantityMilliunits > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
            var household = runtime.Households.OrderBy(item =>
                    item.FoodSecurityBasisPoints)
                .ThenBy(item => item.HouseholdOrdinal).FirstOrDefault();
            if (inventory == null || household == null) return false;
            var quantity = Math.Min(inventory.QuantityMilliunits,
                Math.Max(1_000L, household.DailyFoodDemandMilliunits * 3));
            var householdIndex = runtime.Households.IndexOf(household);
            quantity = new LuoyangFormalEconomySystem().TransferToHousehold(
                runtime, inventory.Id, householdIndex, inventory.ProductId,
                quantity,
                InventoryTransactionType.FoodCountyReliefTransferred,
                "relief." + household.HouseholdId + "." +
                runtime.AbsoluteDay);
            if (quantity <= 0) return false;
            household.CumulativeReliefReceivedMilliunits += quantity;
            runtime.GovernmentEconomy.ReliefExpense += quantity / 1_000;
            runtime.GovernmentEconomy.CurrentFoodPolicyId = "government.food.relief";
            resultId = "relief." + household.HouseholdId + "." + runtime.AbsoluteDay;
            return true;
        }

        private static bool ExecuteMaintenance(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent,
            out string resultId)
        {
            resultId = string.Empty;
            var prefix = "facility_manager.";
            var facilityId = agent.SubjectId.StartsWith(prefix,
                StringComparison.Ordinal) ? agent.SubjectId.Substring(prefix.Length) : string.Empty;
            var facility = index.Facility(facilityId);
            if (facility == null || facility.ConditionBasisPoints >= 10_000) return false;
            facility.ConditionBasisPoints = Math.Min(10_000,
                facility.ConditionBasisPoints + 500);
            resultId = "maintenance." + facility.FacilityId + "." + runtime.AbsoluteDay;
            return true;
        }

        private static void AppendAudit(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangIntelligentAgentRuntimeState agent,
            WorldDecisionContext context,
            IReadOnlyList<WorldActionIntent> candidates,
            WorldActionIntent selected,
            string reason,
            bool executed,
            string resultEntityId)
        {
            var important = agent.Role != LuoyangIntelligentAgentRole.Household &&
                            agent.Role != LuoyangIntelligentAgentRole.FacilityManager;
            if (!important && !executed || runtime.DecisionAudits.Count >= AuditLimit)
                return;
            runtime.DecisionAudits.Add(new LuoyangDecisionAuditState
            {
                Id = "decision_audit." + runtime.AbsoluteDay + "." +
                    runtime.DecisionAudits.Count.ToString("D8"),
                Day = runtime.AbsoluteDay,
                AgentId = agent.Id,
                Role = agent.Role,
                SignalDigest = string.Join(";", context.Signals.Select(item =>
                    item.SignalId + "=" + item.ValueBasisPoints)),
                CandidateDigest = string.Join(";", candidates.Select(item =>
                    item.ActionTypeId)),
                SelectedActionTypeId = selected.ActionTypeId,
                ValidationReasonId = reason,
                Executed = executed,
                ResultEntityId = resultEntityId ?? string.Empty
            });
        }

        private static WorldActionIntent Action(
            LuoyangIntelligentAgentRuntimeState agent,
            WorldDecisionContext context,
            string type,
            string suffix,
            int benefit,
            int cost,
            int risk) => new WorldActionIntent
            {
                Id = "action." + agent.Id + "." + agent.DecisionSequence + "." + suffix,
                ActionTypeId = type,
                AgentId = agent.SubjectId,
                AgentKind = agent.AgentKind,
                LocationId = context.LocationId,
                ExpectedBenefitBasisPoints = Math.Max(0, Math.Min(20_000, benefit)),
                CostBasisPoints = Math.Max(0, cost),
                RiskBasisPoints = Math.Max(0, risk)
            };

        private static void AddSignal(WorldDecisionContext context,
            string id, int value, string evidence) => context.Signals.Add(
                new WorldSignalValue
                {
                    SignalId = id,
                    ValueBasisPoints = Math.Max(0, Math.Min(10_000, value)),
                    EvidenceSummary = evidence
                });

        private static int Signal(WorldDecisionContext context, string id) =>
            context.Signals.Find(item => item.SignalId == id)?.ValueBasisPoints ?? 0;

        private static int Shortage(long stock, long target) => target <= 0
            ? 0 : (int)Math.Max(0, Math.Min(10_000,
                (target - stock) * 10_000 / target));

        private static int StoragePressure(Luoyang184LivingWorldRuntimeState runtime)
        {
            var capacity = runtime.Inventories.Sum(item => item.CapacityMilliunits);
            var used = runtime.Inventories.Sum(item => item.QuantityMilliunits);
            return capacity <= 0 ? 0 : (int)Math.Min(10_000,
                used * 10_000 / capacity);
        }

        private static bool IsFood(string productId) =>
            (productId ?? string.Empty).IndexOf("food", StringComparison.Ordinal) >= 0 ||
            (productId ?? string.Empty).IndexOf("grain", StringComparison.Ordinal) >= 0 ||
            (productId ?? string.Empty).IndexOf("ration", StringComparison.Ordinal) >= 0;

        private static bool HasUnemployedMember(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangHouseholdConsumptionState household) =>
            Members(runtime, household).Any(item =>
                item.Status == LuoyangWorkforceStatus.Unemployed);

        private static LuoyangHouseholdConsumptionState Household(
            Luoyang184LivingWorldRuntimeState runtime,
            RuntimeIndex index,
            LuoyangIntelligentAgentRuntimeState agent)
        {
            if (agent.SubjectIndex >= 0 &&
                agent.SubjectIndex < runtime.Households.Count)
                return runtime.Households[agent.SubjectIndex];
            index.Households.TryGetValue(agent.SubjectId, out var household);
            return household;
        }

        private static IEnumerable<LuoyangWorkforceAssignmentState> Members(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangHouseholdConsumptionState household)
        {
            for (var offset = 0; offset < household.MemberCount; offset++)
                yield return runtime.Workforce[checked((int)
                    household.MemberStartOrdinal + offset)];
        }

        private static int FacilityCondition(
            Luoyang184LivingWorldRuntimeState runtime, string subjectId)
        {
            const string prefix = "facility_manager.";
            var id = subjectId.StartsWith(prefix, StringComparison.Ordinal)
                ? subjectId.Substring(prefix.Length) : string.Empty;
            return runtime.Facilities.Find(item => item.FacilityId == id)
                ?.ConditionBasisPoints ?? 10_000;
        }

        private static int Cadence(LuoyangIntelligentAgentRole role) =>
            role == LuoyangIntelligentAgentRole.FacilityManager ? 7 :
            role == LuoyangIntelligentAgentRole.Merchant ? 14 : 30;

        private sealed class SharedSignals
        {
            public int FoodPressure;
            public int InventoryPressure;
            public int EmploymentPressure;
            public int HousingPressure;
            public int StoragePressure;
            public int PricePressure;
            public int ProfitOpportunity;
        }

        private sealed class RuntimeIndex
        {
            public readonly Dictionary<string, LuoyangHouseholdConsumptionState>
                Households;
            public readonly Dictionary<string, LuoyangFamilyOrganizationRuntimeState>
                Families;
            private readonly Dictionary<string, LuoyangMarketRuntimeState> markets;
            private readonly Dictionary<string, LuoyangFacilityProductionRuntimeState>
                facilities;
            private readonly Dictionary<string, bool> merchantStock;
            private readonly Dictionary<string, LuoyangMarketTradeRuntimeState>
                householdBatchTrades;
            private readonly List<LuoyangInventoryBalanceState> marketFood;
            private readonly List<LuoyangWorkforceAssignmentState> unemployed;
            private readonly List<LuoyangFacilityProductionRuntimeState> vacancies;
            private readonly Luoyang184LivingWorldRuntimeState runtime;
            private int unemployedCursor;
            private int vacancyCursor;

            public RuntimeIndex(Luoyang184LivingWorldRuntimeState runtime)
            {
                this.runtime = runtime;
                Households = new Dictionary<string,
                    LuoyangHouseholdConsumptionState>(StringComparer.Ordinal);
                Families = runtime.FamilyOrganizations.ToDictionary(item =>
                    item.Id, StringComparer.Ordinal);
                markets = runtime.Markets.ToDictionary(item => item.ProductId,
                    StringComparer.Ordinal);
                facilities = runtime.Facilities.ToDictionary(item =>
                    item.FacilityId, StringComparer.Ordinal);
                merchantStock = new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (var supplier in runtime.ExternalSuppliers)
                {
                    if (supplier.Level != LuoyangSupplierMaterializationLevel
                            .DeferredExternalTrade &&
                        supplier.InventoryQuantityMilliunits > 0)
                        merchantStock[supplier.OrganizationId] = true;
                }
                marketFood = runtime.Inventories.Where(item =>
                        item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                        IsFood(item.ProductId))
                    .OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
                householdBatchTrades = new Dictionary<string,
                    LuoyangMarketTradeRuntimeState>(StringComparer.Ordinal);
                foreach (var trade in runtime.MarketTrades)
                {
                    if (trade.Day == runtime.AbsoluteDay &&
                        trade.BuyerId == "household.batch.luoyang.184")
                        householdBatchTrades[TradeKey(trade.ProductId,
                            trade.SellerId)] = trade;
                }
                unemployed = runtime.Workforce;
                vacancies = runtime.Facilities.Where(item =>
                        item.AssignedWorkers < item.OptimalWorkers)
                    .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                    .ToList();
                unemployedCursor = Math.Max(0, Math.Min(
                    runtime.UnemployedSearchCursor, unemployed.Count));
                vacancyCursor = Math.Max(0, Math.Min(
                    runtime.FacilityVacancySearchCursor, vacancies.Count));
                runtimeUnemployedCount = runtime.CurrentUnemployedCount;
            }

            public int UnemployedCount => runtimeUnemployedCount;
            private int runtimeUnemployedCount;

            public bool HasMarketFood() =>
                marketFood.Exists(item => item.QuantityMilliunits > 0);

            public bool HasMerchantStock(string organizationId) =>
                merchantStock.TryGetValue(organizationId, out var hasStock) &&
                hasStock;

            public LuoyangInventoryBalanceState NextMarketFoodInventory() =>
                marketFood.Find(item => item.QuantityMilliunits > 0);

            public LuoyangMarketRuntimeState Market(string productId)
            {
                markets.TryGetValue(productId, out var market);
                return market;
            }

            public LuoyangFacilityProductionRuntimeState Facility(string id)
            {
                facilities.TryGetValue(id, out var facility);
                return facility;
            }

            public LuoyangMarketTradeRuntimeState HouseholdBatchTrade(
                LuoyangInventoryBalanceState inventory)
            {
                householdBatchTrades.TryGetValue(TradeKey(inventory.ProductId,
                    inventory.OwnerId), out var trade);
                return trade;
            }

            public void RegisterHouseholdBatchTrade(
                LuoyangInventoryBalanceState inventory,
                LuoyangMarketTradeRuntimeState trade) =>
                householdBatchTrades[TradeKey(inventory.ProductId,
                    inventory.OwnerId)] = trade;

            private static string TradeKey(string productId, string sellerId) =>
                productId + "\u001f" + sellerId;

            public LuoyangWorkforceAssignmentState NextUnemployed()
            {
                if (runtimeUnemployedCount <= 0) return null;
                var checkedCount = 0;
                while (checkedCount < unemployed.Count)
                {
                    if (unemployedCursor >= unemployed.Count) unemployedCursor = 0;
                    var person = unemployed[unemployedCursor++];
                    runtime.UnemployedSearchCursor = unemployedCursor;
                    checkedCount++;
                    if (person.Status == LuoyangWorkforceStatus.Unemployed)
                        return person;
                }
                return null;
            }

            public LuoyangFacilityProductionRuntimeState NextFacilityWithVacancy()
            {
                while (vacancyCursor < vacancies.Count &&
                       vacancies[vacancyCursor].AssignedWorkers >=
                       vacancies[vacancyCursor].OptimalWorkers)
                    vacancyCursor++;
                if (vacancyCursor >= vacancies.Count) vacancyCursor = 0;
                while (vacancyCursor < vacancies.Count &&
                       vacancies[vacancyCursor].AssignedWorkers >=
                       vacancies[vacancyCursor].OptimalWorkers)
                    vacancyCursor++;
                runtime.FacilityVacancySearchCursor = vacancyCursor;
                return vacancyCursor < vacancies.Count
                    ? vacancies[vacancyCursor] : null;
            }

            public void MarkEmployed(LuoyangWorkforceAssignmentState person)
            {
                if (runtimeUnemployedCount > 0) runtimeUnemployedCount--;
                runtime.CurrentUnemployedCount = runtimeUnemployedCount;
            }
        }

        private static WorldActionValidationResult Valid(string reason) =>
            new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Valid,
                ReasonId = reason
            };

        private static WorldActionValidationResult Invalid(string reason) =>
            new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Invalid,
                ReasonId = reason
            };

        private static WorldActionValidationResult Deferred(string reason) =>
            new WorldActionValidationResult
            {
                Status = WorldActionValidationStatus.Deferred,
                ReasonId = reason
            };
    }
}
