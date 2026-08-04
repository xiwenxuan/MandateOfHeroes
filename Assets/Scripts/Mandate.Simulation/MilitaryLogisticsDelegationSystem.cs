using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryLogisticsDelegationGoalRequest
    {
        public StableId IssuerPersonId;
        public StableId TargetArmyId;
        public StableId DestinationLocationId;
        public string ProductDefinitionId;
        public int RequestedCargoQuantity;
        public long MaximumUnitPrice;
        public long BudgetLimit;
        public long DeadlineDay;
        public int ReportIntervalDays = 1;
        public string CarrierPreferenceId =
            MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost;
        public string CargoConsumptionPolicyId =
            MilitaryCargoConsumptionPolicyIds.Prohibited;
        public string RiskPolicyId = MilitaryLogisticsRiskPolicyIds.None;
        public string ThreatOrganizationId = string.Empty;
    }

    public sealed class MilitaryLogisticsDelegationOfferRequest
    {
        public StableId CarrierPersonId;
        public StableId SourceCargoBatchId;
        public string SourceProvisionBatchId = string.Empty;
        public StableId RouteId;
        public string AcquisitionMethodId =
            MilitarySupplyAcquisitionMethodIds.CommercialPurchase;
        public string CarrierOrganizationId;
        public string LossBearerOrganizationId;
        public int CargoQuantity;
        public int ConvoyProvisionQuantity;
        public int DailyConvoyProvisionUse = 1;
        public long UnitPrice;
        public long ValidUntilDay;
    }

    public sealed class MilitaryLogisticsSubgoalRequest
    {
        public StableId AssigneePersonId;
        public int RequestedCargoQuantity;
        public long MaximumUnitPrice;
        public long BudgetLimit;
        public long DeadlineDay;
        public int ReportIntervalDays = 1;
        public string CarrierPreferenceId =
            MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost;
    }

    public sealed class MilitaryLogisticsDelegationSystem
    {
        public const int MaximumDelegationDepth =
            MilitaryLogisticsDelegationContract.MaximumDelegationDepth;
        public const int MaximumDirectSubgoals =
            MilitaryLogisticsDelegationContract.MaximumDirectSubgoals;

        private readonly IPersonRepository _people;
        private readonly ProductionContentRegistry _content;
        private readonly MilitaryLogisticsSystem _logistics;
        private readonly MilitaryAuthoritySystem _authority =
            new MilitaryAuthoritySystem();

        public MilitaryLogisticsDelegationSystem(
            ProductionContentRegistry content = null,
            IPersonRepository people = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _people = people;
            _logistics = new MilitaryLogisticsSystem(_content, people);
        }

        public MilitaryLogisticsDelegationGoalState CreateGoal(
            WorldState world,
            MilitaryLogisticsDelegationGoalRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            world.Validate();
            if (request.RequestedCargoQuantity <= 0 ||
                request.MaximumUnitPrice < 0 || request.BudgetLimit < 0 ||
                request.DeadlineDay < world.AbsoluteDay ||
                request.ReportIntervalDays <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request), "The delegated logistics target is invalid.");
            }

            ValidatePreference(request.CarrierPreferenceId);
            ValidateRisk(
                world, request.RiskPolicyId, request.ThreatOrganizationId);
            _ = new StableId(request.CargoConsumptionPolicyId);
            var product = _content.GetProduct(request.ProductDefinitionId);
            if (!product.CategoryTags.Contains("product.food") ||
                !product.CategoryTags.Contains("product.military_supply"))
            {
                throw new InvalidOperationException(
                    "A military logistics goal requires military food supplies.");
            }

            _ = FindLocation(world, request.DestinationLocationId.Value);
            _ = FindArmy(world, request.TargetArmyId.Value);
            if (_authority.GetAuthority(
                    world,
                    request.IssuerPersonId,
                    request.TargetArmyId) < MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The issuer lacks army logistics authority.");
            }

            var goal = new MilitaryLogisticsDelegationGoalState
            {
                Id = $"military_logistics_delegation_goal." +
                     $"{world.AbsoluteDay}." +
                     $"{world.MilitaryLogisticsDelegationGoals.Count}",
                CreatedDay = world.AbsoluteDay,
                DeadlineDay = request.DeadlineDay,
                ReportIntervalDays = request.ReportIntervalDays,
                NextEvaluationDay = Math.Min(
                    request.DeadlineDay,
                    checked(world.AbsoluteDay + request.ReportIntervalDays)),
                IssuerPersonId = request.IssuerPersonId.Value,
                AssigneePersonId = request.IssuerPersonId.Value,
                AssigneeAuthorityAtDelegation = MilitaryAuthorityLevel.Army,
                TargetArmyId = request.TargetArmyId.Value,
                DestinationLocationId = request.DestinationLocationId.Value,
                ProductDefinitionId = product.Id,
                RequestedCargoQuantity = request.RequestedCargoQuantity,
                OutstandingCargoQuantity = request.RequestedCargoQuantity,
                MaximumUnitPrice = request.MaximumUnitPrice,
                BudgetLimit = request.BudgetLimit,
                CarrierPreferenceId = request.CarrierPreferenceId,
                CargoConsumptionPolicyId =
                    request.CargoConsumptionPolicyId,
                RiskPolicyId = request.RiskPolicyId,
                ThreatOrganizationId = request.ThreatOrganizationId ??
                    string.Empty,
                Status = MilitaryLogisticsDelegationStatus.Pending
            };
            world.MilitaryLogisticsDelegationGoals.Add(goal);
            AddReport(
                world,
                goal,
                goal.IssuerPersonId,
                MilitaryLogisticsDelegationReportTypeIds.GoalCreated,
                false,
                string.Empty,
                string.Empty,
                $"{goal.IssuerPersonId}下达军需目标{goal.Id}。",
                false);
            world.Validate();
            return goal;
        }

        public MilitaryLogisticsDelegationOfferState SubmitOffer(
            WorldState world,
            string goalId,
            MilitaryLogisticsDelegationOfferRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            world.Validate();
            var goal = FindGoal(world, goalId);
            if (goal.Status == MilitaryLogisticsDelegationStatus.Dispatched ||
                goal.Status == MilitaryLogisticsDelegationStatus.Cancelled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Fulfilled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Expired ||
                goal.Status == MilitaryLogisticsDelegationStatus.Delegated ||
                goal.ChildGoalIds.Count != 0 ||
                world.AbsoluteDay > goal.DeadlineDay ||
                goal.OutstandingCargoQuantity <= 0 ||
                RequiresReplacementAuthorization(world, goal) ||
                request.CargoQuantity < goal.OutstandingCargoQuantity ||
                request.ConvoyProvisionQuantity < 0 ||
                request.DailyConvoyProvisionUse <= 0 ||
                request.UnitPrice < 0)
            {
                throw new InvalidOperationException(
                    "The logistics delegation cannot accept this offer.");
            }

            var validUntilDay = request.ValidUntilDay <= 0
                ? goal.DeadlineDay
                : request.ValidUntilDay;
            if (validUntilDay < world.AbsoluteDay ||
                validUntilDay > goal.DeadlineDay)
            {
                throw new InvalidOperationException(
                    "The offer validity must end between submission and the goal deadline.");
            }

            ValidateOfferMethod(request.AcquisitionMethodId);
            var carrier = PeopleFor(world).GetRequired(
                request.CarrierPersonId.Value);
            var transportContainer = FindContainerByCarrier(
                world, carrier.Id);
            if (!carrier.IsAlive ||
                carrier.LocationId != transportContainer.LocationId ||
                transportContainer.OwnerOrganizationId !=
                    request.CarrierOrganizationId ||
                !HasMembership(
                    world, carrier.Id, request.CarrierOrganizationId))
            {
                throw new InvalidOperationException(
                    "The proposed carrier and transport custody are invalid.");
            }

            var cargoBatch = FindBatch(
                world, request.SourceCargoBatchId.Value);
            var cargoContainer = FindContainer(
                world, cargoBatch.InventoryContainerId);
            if (cargoBatch.ProductDefinitionId != goal.ProductDefinitionId ||
                cargoBatch.Quantity - cargoBatch.ReservedQuantity <
                    request.CargoQuantity ||
                cargoContainer.LocationId != carrier.LocationId)
            {
                throw new InvalidOperationException(
                    "The offered cargo is unavailable or not co-located.");
            }

            ValidateProvisionOffer(
                world,
                request,
                cargoBatch,
                carrier.LocationId);
            var army = FindArmy(world, goal.TargetArmyId);
            if (request.LossBearerOrganizationId != army.OrganizationId &&
                request.LossBearerOrganizationId !=
                    request.CarrierOrganizationId)
            {
                throw new InvalidOperationException(
                    "Only the buyer or carrier may bear delegated freight loss.");
            }
            var sourceOrganization = FindOrganization(
                world, cargoBatch.OwnerOrganizationId);
            _ = FindOrganization(world, request.CarrierOrganizationId);
            _ = FindOrganization(world, request.LossBearerOrganizationId);
            ValidateOfferOwnershipAndPrice(
                request.AcquisitionMethodId,
                army.OrganizationId,
                sourceOrganization.Id,
                request.UnitPrice);
            var route = FindRoute(world, request.RouteId.Value);
            if (!RouteConnects(
                    route, carrier.LocationId, goal.DestinationLocationId))
            {
                throw new InvalidOperationException(
                    "The offered route does not reach the delegated destination.");
            }

            var offer = new MilitaryLogisticsDelegationOfferState
            {
                Id = $"military_logistics_delegation_offer." +
                     $"{world.AbsoluteDay}." +
                     $"{world.MilitaryLogisticsDelegationOffers.Count}",
                SubmittedDay = world.AbsoluteDay,
                ValidUntilDay = validUntilDay,
                GoalId = goal.Id,
                CarrierPersonId = carrier.Id,
                CarrierOrganizationId = request.CarrierOrganizationId,
                SourceCargoBatchId = cargoBatch.Id,
                SourceProvisionBatchId = request.SourceProvisionBatchId ??
                    string.Empty,
                TransportInventoryContainerId = transportContainer.Id,
                OriginLocationId = carrier.LocationId,
                RouteId = route.Id,
                AcquisitionMethodId = request.AcquisitionMethodId,
                LossBearerOrganizationId = request.LossBearerOrganizationId,
                LiabilityPolicyId = request.LossBearerOrganizationId ==
                    army.OrganizationId
                        ? MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk
                        : MilitaryLogisticsLiabilityPolicyIds
                            .LossBearerCompensates,
                AvailableCargoQuantity = request.CargoQuantity,
                ConvoyProvisionQuantity = request.ConvoyProvisionQuantity,
                DailyConvoyProvisionUse = request.DailyConvoyProvisionUse,
                UnitPrice = request.UnitPrice,
                Status = MilitaryLogisticsDelegationOfferStatus.Active
            };
            world.MilitaryLogisticsDelegationOffers.Add(offer);
            var needsPromptRetry = goal.Status ==
                MilitaryLogisticsDelegationStatus.NeedsAttention;
            goal.Status = MilitaryLogisticsDelegationStatus.Pending;
            if (needsPromptRetry)
            {
                goal.NextEvaluationDay = Math.Min(
                    goal.NextEvaluationDay,
                    Math.Min(
                        goal.DeadlineDay,
                        checked(world.AbsoluteDay + 1)));
            }
            AddReport(
                world,
                goal,
                carrier.Id,
                MilitaryLogisticsDelegationReportTypeIds.OfferSubmitted,
                false,
                offer.Id,
                string.Empty,
                $"{carrier.Id}为{goal.Id}提交承运报价{offer.Id}。",
                false);
            world.Validate();
            return offer;
        }

        public List<MilitaryLogisticsDelegationGoalState> DelegateGoal(
            WorldState world,
            string parentGoalId,
            StableId delegatorPersonId,
            IList<MilitaryLogisticsSubgoalRequest> requests)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (requests == null || requests.Count == 0 ||
                requests.Count > MaximumDirectSubgoals)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requests),
                    $"A delegation split requires 1-{MaximumDirectSubgoals} subgoals.");
            }

            world.Validate();
            var parent = FindGoal(world, parentGoalId);
            if ((parent.Status != MilitaryLogisticsDelegationStatus.Pending &&
                 parent.Status !=
                    MilitaryLogisticsDelegationStatus.NeedsAttention) ||
                parent.DelegationDepth >= MaximumDelegationDepth ||
                parent.AssigneePersonId != delegatorPersonId.Value ||
                parent.ChildGoalIds.Count != 0 ||
                !string.IsNullOrEmpty(parent.SelectedOfferId) ||
                !string.IsNullOrEmpty(parent.LogisticsOrderId) ||
                HasOffer(world, parent.Id))
            {
                throw new InvalidOperationException(
                    "The parent logistics goal cannot be delegated.");
            }

            if (_authority.GetAuthority(
                    world,
                    new StableId(parent.IssuerPersonId),
                    new StableId(parent.TargetArmyId)) <
                MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The root issuer no longer has army logistics authority.");
            }

            var delegator = PeopleFor(world).GetRequired(
                delegatorPersonId.Value);
            var delegatorAuthority = _authority.GetAuthority(
                world,
                delegatorPersonId,
                new StableId(parent.TargetArmyId));
            if (!delegator.IsAlive ||
                delegatorAuthority <
                    parent.AssigneeAuthorityAtDelegation ||
                delegatorAuthority <= MilitaryAuthorityLevel.Self)
            {
                throw new InvalidOperationException(
                    "The current assignee cannot delegate this goal further.");
            }

            var sortedRequests = new List<MilitaryLogisticsSubgoalRequest>();
            for (var i = 0; i < requests.Count; i++)
            {
                if (requests[i] == null)
                {
                    throw new ArgumentException(
                        "A subgoal request cannot be null.", nameof(requests));
                }
                sortedRequests.Add(requests[i]);
            }
            sortedRequests.Sort((left, right) => string.CompareOrdinal(
                left.AssigneePersonId.Value,
                right.AssigneePersonId.Value));

            var validated = new List<ValidatedSubgoal>();
            var assigneeIds = new HashSet<string>(StringComparer.Ordinal);
            long totalQuantity = 0;
            long totalBudget = 0;
            for (var i = 0; i < sortedRequests.Count; i++)
            {
                var request = sortedRequests[i];
                ValidatePreference(request.CarrierPreferenceId);
                if (!assigneeIds.Add(request.AssigneePersonId.Value) ||
                    request.RequestedCargoQuantity <= 0 ||
                    request.MaximumUnitPrice < 0 ||
                    request.MaximumUnitPrice > parent.MaximumUnitPrice ||
                    request.BudgetLimit < 0 ||
                    request.DeadlineDay < world.AbsoluteDay ||
                    request.DeadlineDay > parent.DeadlineDay ||
                    request.ReportIntervalDays <= 0)
                {
                    throw new InvalidOperationException(
                        "A delegated child target exceeds its parent constraints.");
                }

                var assignee = PeopleFor(world).GetRequired(
                    request.AssigneePersonId.Value);
                var assigneeAuthority = _authority.GetAuthority(
                    world,
                    request.AssigneePersonId,
                    new StableId(parent.TargetArmyId));
                if (!assignee.IsAlive ||
                    assigneeAuthority == MilitaryAuthorityLevel.None ||
                    assigneeAuthority >= delegatorAuthority)
                {
                    throw new InvalidOperationException(
                        "A subgoal assignee must be an available lower-authority service member.");
                }

                totalQuantity = checked(
                    totalQuantity + request.RequestedCargoQuantity);
                totalBudget = checked(totalBudget + request.BudgetLimit);
                validated.Add(new ValidatedSubgoal
                {
                    Request = request,
                    AssigneeAuthority = assigneeAuthority
                });
            }

            if (totalQuantity != parent.RequestedCargoQuantity ||
                totalBudget > parent.BudgetLimit)
            {
                throw new InvalidOperationException(
                    "Subgoal quantity and budget allocations violate the parent target.");
            }

            var children = new List<MilitaryLogisticsDelegationGoalState>();
            parent.Status = MilitaryLogisticsDelegationStatus.Delegated;
            parent.UnassignedCargoQuantity = 0;
            parent.AvailableBudgetReserve = checked(
                parent.BudgetLimit - totalBudget);
            parent.NextEvaluationDay = Math.Min(
                parent.DeadlineDay,
                checked(world.AbsoluteDay + parent.ReportIntervalDays));
            for (var i = 0; i < validated.Count; i++)
            {
                var item = validated[i];
                var request = item.Request;
                var child = new MilitaryLogisticsDelegationGoalState
                {
                    Id = $"military_logistics_delegation_goal." +
                         $"{world.AbsoluteDay}." +
                         $"{world.MilitaryLogisticsDelegationGoals.Count}",
                    CreatedDay = world.AbsoluteDay,
                    DeadlineDay = request.DeadlineDay,
                    ReportIntervalDays = request.ReportIntervalDays,
                    NextEvaluationDay = Math.Min(
                        request.DeadlineDay,
                        checked(world.AbsoluteDay +
                            request.ReportIntervalDays)),
                    ParentGoalId = parent.Id,
                    DelegationDepth = parent.DelegationDepth + 1,
                    IssuerPersonId = parent.IssuerPersonId,
                    AssigneePersonId = request.AssigneePersonId.Value,
                    DelegatedByPersonId = delegatorPersonId.Value,
                    AssigneeAuthorityAtDelegation =
                        item.AssigneeAuthority,
                    TargetArmyId = parent.TargetArmyId,
                    DestinationLocationId = parent.DestinationLocationId,
                    ProductDefinitionId = parent.ProductDefinitionId,
                    RequestedCargoQuantity = request.RequestedCargoQuantity,
                    OutstandingCargoQuantity = request.RequestedCargoQuantity,
                    MaximumUnitPrice = request.MaximumUnitPrice,
                    BudgetLimit = request.BudgetLimit,
                    CarrierPreferenceId = request.CarrierPreferenceId,
                    CargoConsumptionPolicyId =
                        parent.CargoConsumptionPolicyId,
                    RiskPolicyId = parent.RiskPolicyId,
                    ThreatOrganizationId = parent.ThreatOrganizationId,
                    Status = MilitaryLogisticsDelegationStatus.Pending
                };
                world.MilitaryLogisticsDelegationGoals.Add(child);
                parent.ChildGoalIds.Add(child.Id);
                children.Add(child);
            }

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                AddReport(
                    world,
                    child,
                    delegatorPersonId.Value,
                    MilitaryLogisticsDelegationReportTypeIds.GoalCreated,
                    false,
                    string.Empty,
                    string.Empty,
                    $"{delegatorPersonId.Value}将{parent.Id}拆为子目标" +
                    $"{child.Id}并委任{child.AssigneePersonId}。",
                    false,
                    parent.Id);
                AddReport(
                    world,
                    parent,
                    delegatorPersonId.Value,
                    MilitaryLogisticsDelegationReportTypeIds.SubgoalCreated,
                    false,
                    string.Empty,
                    string.Empty,
                    $"父目标{parent.Id}创建子目标{child.Id}，数量" +
                    $"{child.RequestedCargoQuantity}，预算" +
                    $"{child.BudgetLimit}。",
                    false,
                    child.Id);
            }

            world.Validate();
            return children;
        }

        public void CancelUncommittedSubgoal(
            WorldState world,
            string parentGoalId,
            string childGoalId,
            StableId actorPersonId,
            string cancellationReasonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _ = new StableId(cancellationReasonId);
            world.Validate();
            var parent = FindGoal(world, parentGoalId);
            var child = FindGoal(world, childGoalId);
            ValidateParentAssigneeAuthority(world, parent, actorPersonId);
            if (child.ParentGoalId != parent.Id ||
                child.ChildGoalIds.Count != 0 ||
                child.Status != MilitaryLogisticsDelegationStatus.Pending &&
                child.Status !=
                    MilitaryLogisticsDelegationStatus.NeedsAttention &&
                child.Status != MilitaryLogisticsDelegationStatus.Expired ||
                !string.IsNullOrEmpty(child.SelectedOfferId) ||
                !string.IsNullOrEmpty(child.LogisticsOrderId) ||
                child.CommittedCost != 0 ||
                child.ReceivedCargoQuantity != 0 ||
                child.CompletedLogisticsOrderIds.Count != 0 ||
                parent.Status != MilitaryLogisticsDelegationStatus.Delegated &&
                parent.Status !=
                    MilitaryLogisticsDelegationStatus.NeedsAttention)
            {
                throw new InvalidOperationException(
                    "Only an uncommitted direct leaf subgoal may be cancelled.");
            }

            var activeOffers = FindActiveOffers(world, child.Id);
            child.Status = MilitaryLogisticsDelegationStatus.Cancelled;
            child.CancelledDay = world.AbsoluteDay;
            child.CancelledByPersonId = actorPersonId.Value;
            child.CancellationReasonId = cancellationReasonId;
            parent.UnassignedCargoQuantity = checked(
                parent.UnassignedCargoQuantity +
                child.RequestedCargoQuantity);
            parent.AvailableBudgetReserve = checked(
                parent.AvailableBudgetReserve + child.BudgetLimit);
            parent.Status = MilitaryLogisticsDelegationStatus.NeedsAttention;
            parent.NextEvaluationDay = Math.Min(
                parent.DeadlineDay,
                checked(world.AbsoluteDay + parent.ReportIntervalDays));

            for (var i = 0; i < activeOffers.Count; i++)
            {
                var offer = activeOffers[i];
                offer.Status =
                    MilitaryLogisticsDelegationOfferStatus.GoalCancelled;
                offer.ClosedDay = world.AbsoluteDay;
                AddReport(
                    world,
                    child,
                    actorPersonId.Value,
                    MilitaryLogisticsDelegationReportTypeIds
                        .OfferClosedByCancellation,
                    false,
                    offer.Id,
                    string.Empty,
                    $"报价{offer.Id}因子目标{child.Id}取消而关闭。",
                    false);
            }

            AddReport(
                world,
                child,
                actorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds.GoalCancelled,
                false,
                string.Empty,
                string.Empty,
                $"子目标{child.Id}因{cancellationReasonId}取消。",
                false,
                parent.Id);
            AddReport(
                world,
                parent,
                actorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds.AllocationRecovered,
                false,
                string.Empty,
                string.Empty,
                $"父目标{parent.Id}从{child.Id}回收数量" +
                $"{child.RequestedCargoQuantity}和预算{child.BudgetLimit}。",
                false,
                child.Id);
            world.Validate();
        }

        public List<MilitaryLogisticsDelegationGoalState>
            ReassignCancelledSubgoal(
                WorldState world,
                string parentGoalId,
                string cancelledGoalId,
                StableId delegatorPersonId,
                IList<MilitaryLogisticsSubgoalRequest> requests)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (requests == null || requests.Count == 0 ||
                requests.Count > MaximumDirectSubgoals)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requests),
                    $"A reassignment requires 1-{MaximumDirectSubgoals} replacement goals.");
            }

            world.Validate();
            var parent = FindGoal(world, parentGoalId);
            var cancelled = FindGoal(world, cancelledGoalId);
            var delegatorAuthority = ValidateParentAssigneeAuthority(
                world, parent, delegatorPersonId);
            if (parent.Status !=
                    MilitaryLogisticsDelegationStatus.NeedsAttention ||
                cancelled.ParentGoalId != parent.Id ||
                cancelled.Status !=
                    MilitaryLogisticsDelegationStatus.Cancelled ||
                cancelled.ChildGoalIds.Count != 0 ||
                cancelled.ReplacementGoalIds.Count != 0 ||
                parent.UnassignedCargoQuantity <
                    cancelled.RequestedCargoQuantity ||
                parent.AvailableBudgetReserve < cancelled.BudgetLimit)
            {
                throw new InvalidOperationException(
                    "The cancelled allocation is not available for reassignment.");
            }

            var sortedRequests = new List<MilitaryLogisticsSubgoalRequest>();
            for (var i = 0; i < requests.Count; i++)
            {
                if (requests[i] == null)
                {
                    throw new ArgumentException(
                        "A replacement request cannot be null.",
                        nameof(requests));
                }
                sortedRequests.Add(requests[i]);
            }
            sortedRequests.Sort((left, right) => string.CompareOrdinal(
                left.AssigneePersonId.Value,
                right.AssigneePersonId.Value));

            var assigneeIds = new HashSet<string>(StringComparer.Ordinal);
            var activeChildCount = 0;
            var existingChildren = FindChildren(world, parent);
            for (var i = 0; i < existingChildren.Count; i++)
            {
                if (existingChildren[i].Status ==
                    MilitaryLogisticsDelegationStatus.Cancelled)
                {
                    continue;
                }
                activeChildCount++;
                assigneeIds.Add(existingChildren[i].AssigneePersonId);
            }
            if (activeChildCount + sortedRequests.Count >
                MaximumDirectSubgoals)
            {
                throw new InvalidOperationException(
                    "The active direct subgoal limit would be exceeded.");
            }

            var validated = new List<ValidatedSubgoal>();
            long totalQuantity = 0;
            long totalBudget = 0;
            for (var i = 0; i < sortedRequests.Count; i++)
            {
                var request = sortedRequests[i];
                ValidatePreference(request.CarrierPreferenceId);
                if (!assigneeIds.Add(request.AssigneePersonId.Value) ||
                    request.RequestedCargoQuantity <= 0 ||
                    request.MaximumUnitPrice < 0 ||
                    request.MaximumUnitPrice > parent.MaximumUnitPrice ||
                    request.BudgetLimit < 0 ||
                    request.DeadlineDay < world.AbsoluteDay ||
                    request.DeadlineDay > parent.DeadlineDay ||
                    request.ReportIntervalDays <= 0)
                {
                    throw new InvalidOperationException(
                        "A replacement target exceeds its parent constraints.");
                }

                var assignee = PeopleFor(world).GetRequired(
                    request.AssigneePersonId.Value);
                var assigneeAuthority = _authority.GetAuthority(
                    world,
                    request.AssigneePersonId,
                    new StableId(parent.TargetArmyId));
                if (!assignee.IsAlive ||
                    assigneeAuthority == MilitaryAuthorityLevel.None ||
                    assigneeAuthority >= delegatorAuthority)
                {
                    throw new InvalidOperationException(
                        "A replacement assignee must be an available lower-authority service member.");
                }

                totalQuantity = checked(
                    totalQuantity + request.RequestedCargoQuantity);
                totalBudget = checked(totalBudget + request.BudgetLimit);
                validated.Add(new ValidatedSubgoal
                {
                    Request = request,
                    AssigneeAuthority = assigneeAuthority
                });
            }

            if (totalQuantity != cancelled.RequestedCargoQuantity ||
                totalBudget > cancelled.BudgetLimit ||
                totalBudget > parent.AvailableBudgetReserve)
            {
                throw new InvalidOperationException(
                    "Replacement quantity or budget exceeds the recovered allocation.");
            }

            var replacements = new List<
                MilitaryLogisticsDelegationGoalState>();
            for (var i = 0; i < validated.Count; i++)
            {
                var item = validated[i];
                var request = item.Request;
                var replacement = new MilitaryLogisticsDelegationGoalState
                {
                    Id = $"military_logistics_delegation_goal." +
                         $"{world.AbsoluteDay}." +
                         $"{world.MilitaryLogisticsDelegationGoals.Count}",
                    CreatedDay = world.AbsoluteDay,
                    DeadlineDay = request.DeadlineDay,
                    ReportIntervalDays = request.ReportIntervalDays,
                    NextEvaluationDay = Math.Min(
                        request.DeadlineDay,
                        checked(world.AbsoluteDay +
                            request.ReportIntervalDays)),
                    ParentGoalId = parent.Id,
                    DelegationDepth = parent.DelegationDepth + 1,
                    IssuerPersonId = parent.IssuerPersonId,
                    AssigneePersonId = request.AssigneePersonId.Value,
                    DelegatedByPersonId = delegatorPersonId.Value,
                    AssigneeAuthorityAtDelegation = item.AssigneeAuthority,
                    ReplacesGoalId = cancelled.Id,
                    TargetArmyId = parent.TargetArmyId,
                    DestinationLocationId = parent.DestinationLocationId,
                    ProductDefinitionId = parent.ProductDefinitionId,
                    RequestedCargoQuantity = request.RequestedCargoQuantity,
                    OutstandingCargoQuantity = request.RequestedCargoQuantity,
                    MaximumUnitPrice = request.MaximumUnitPrice,
                    BudgetLimit = request.BudgetLimit,
                    CarrierPreferenceId = request.CarrierPreferenceId,
                    CargoConsumptionPolicyId =
                        parent.CargoConsumptionPolicyId,
                    RiskPolicyId = parent.RiskPolicyId,
                    ThreatOrganizationId = parent.ThreatOrganizationId,
                    Status = MilitaryLogisticsDelegationStatus.Pending
                };
                world.MilitaryLogisticsDelegationGoals.Add(replacement);
                parent.ChildGoalIds.Add(replacement.Id);
                cancelled.ReplacementGoalIds.Add(replacement.Id);
                replacements.Add(replacement);
            }

            parent.UnassignedCargoQuantity = checked(
                parent.UnassignedCargoQuantity -
                cancelled.RequestedCargoQuantity);
            parent.AvailableBudgetReserve = checked(
                parent.AvailableBudgetReserve - totalBudget);
            parent.Status = parent.UnassignedCargoQuantity == 0
                ? MilitaryLogisticsDelegationStatus.Delegated
                : MilitaryLogisticsDelegationStatus.NeedsAttention;
            parent.NextEvaluationDay = Math.Min(
                parent.DeadlineDay,
                checked(world.AbsoluteDay + parent.ReportIntervalDays));

            AddReport(
                world,
                cancelled,
                delegatorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds.SubgoalReassigned,
                false,
                string.Empty,
                string.Empty,
                $"取消目标{cancelled.Id}已由{replacements.Count}个新目标替代。",
                false,
                parent.Id);
            for (var i = 0; i < replacements.Count; i++)
            {
                var replacement = replacements[i];
                AddReport(
                    world,
                    replacement,
                    delegatorPersonId.Value,
                    MilitaryLogisticsDelegationReportTypeIds
                        .ReplacementGoalCreated,
                    false,
                    string.Empty,
                    string.Empty,
                    $"新目标{replacement.Id}替代{cancelled.Id}，数量" +
                    $"{replacement.RequestedCargoQuantity}，预算" +
                    $"{replacement.BudgetLimit}。",
                    false,
                    parent.Id);
                AddReport(
                    world,
                    parent,
                    delegatorPersonId.Value,
                    MilitaryLogisticsDelegationReportTypeIds
                        .SubgoalReassigned,
                    false,
                    string.Empty,
                    string.Empty,
                    $"父目标{parent.Id}以{replacement.Id}替代" +
                    $"{cancelled.Id}的回收份额。",
                    false,
                    replacement.Id);
            }

            world.Validate();
            return replacements;
        }

        public void WithdrawOffer(
            WorldState world,
            string goalId,
            string offerId,
            StableId actorPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var goal = FindGoal(world, goalId);
            var offer = FindOffer(world, offerId);
            if (offer.GoalId != goal.Id ||
                offer.CarrierPersonId != actorPersonId.Value ||
                offer.Status != MilitaryLogisticsDelegationOfferStatus.Active ||
                goal.Status == MilitaryLogisticsDelegationStatus.Dispatched ||
                goal.Status == MilitaryLogisticsDelegationStatus.Cancelled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Fulfilled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Expired ||
                goal.Status == MilitaryLogisticsDelegationStatus.Delegated)
            {
                throw new InvalidOperationException(
                    "Only the carrier may withdraw an open active offer.");
            }

            offer.Status = MilitaryLogisticsDelegationOfferStatus.Withdrawn;
            offer.ClosedDay = world.AbsoluteDay;
            AddReport(
                world,
                goal,
                actorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds.OfferWithdrawn,
                false,
                offer.Id,
                string.Empty,
                $"{actorPersonId.Value}撤回承运报价{offer.Id}。",
                false);
            world.Validate();
        }

        public void ProcessDue(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            ExpireOffers(world);
            var goals = new List<MilitaryLogisticsDelegationGoalState>(
                world.MilitaryLogisticsDelegationGoals);
            goals.Sort((left, right) =>
            {
                var dayOrder = left.NextEvaluationDay.CompareTo(
                    right.NextEvaluationDay);
                return dayOrder != 0
                    ? dayOrder
                    : string.CompareOrdinal(left.Id, right.Id);
            });

            for (var i = 0; i < goals.Count; i++)
            {
                var goal = goals[i];
                if (goal.ChildGoalIds.Count != 0 &&
                    (goal.Status ==
                         MilitaryLogisticsDelegationStatus.Delegated ||
                     goal.Status ==
                         MilitaryLogisticsDelegationStatus.NeedsAttention))
                {
                    continue;
                }

                if (goal.Status == MilitaryLogisticsDelegationStatus.Dispatched)
                {
                    ProcessDispatchedGoal(world, goal);
                    continue;
                }

                if ((goal.Status == MilitaryLogisticsDelegationStatus.Pending ||
                     goal.Status ==
                        MilitaryLogisticsDelegationStatus.NeedsAttention) &&
                    goal.NextEvaluationDay <= world.AbsoluteDay)
                {
                    EvaluateAndDispatch(world, goal.Id);
                }
            }

            var aggregateGoals = goals.FindAll(item =>
                item.ChildGoalIds.Count != 0);
            aggregateGoals.Sort((left, right) =>
            {
                var depthOrder = right.DelegationDepth.CompareTo(
                    left.DelegationDepth);
                return depthOrder != 0
                    ? depthOrder
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            for (var i = 0; i < aggregateGoals.Count; i++)
            {
                RefreshParentReceipt(world, aggregateGoals[i]);
            }

            var delegatedGoals = goals.FindAll(item =>
                item.ChildGoalIds.Count != 0 &&
                (item.Status ==
                     MilitaryLogisticsDelegationStatus.Delegated ||
                 item.Status ==
                     MilitaryLogisticsDelegationStatus.NeedsAttention));
            delegatedGoals.Sort((left, right) =>
            {
                var depthOrder = right.DelegationDepth.CompareTo(
                    left.DelegationDepth);
                return depthOrder != 0
                    ? depthOrder
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            for (var i = 0; i < delegatedGoals.Count; i++)
            {
                ProcessDelegatedGoal(world, delegatedGoals[i]);
            }

            world.Validate();
        }

        public void AuthorizeReplacementProcurement(
            WorldState world,
            string goalId,
            StableId actorPersonId,
            string reasonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _ = new StableId(reasonId);
            world.Validate();
            var goal = FindGoal(world, goalId);
            var availableAuthorization = checked(
                goal.AuthorizedReplacementQuantity -
                goal.ConsumedReplacementAuthorizationQuantity);
            if (goal.IssuerPersonId != actorPersonId.Value ||
                goal.ChildGoalIds.Count != 0 ||
                goal.Status !=
                    MilitaryLogisticsDelegationStatus.NeedsAttention ||
                !string.IsNullOrEmpty(goal.LogisticsOrderId) ||
                goal.OutstandingCargoQuantity <= 0 ||
                UnresolvedHostileCustody(world, goal) <= 0 ||
                goal.ReplacementProcurementPolicyId ==
                    MilitaryLogisticsReplacementProcurementPolicyIds
                        .LegacyUnrestricted ||
                availableAuthorization >= goal.OutstandingCargoQuantity ||
                _authority.GetAuthority(
                    world,
                    actorPersonId,
                    new StableId(goal.TargetArmyId)) <
                    MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The replacement procurement authorization is invalid.");
            }

            goal.AuthorizedReplacementQuantity = checked(
                goal.AuthorizedReplacementQuantity +
                goal.OutstandingCargoQuantity - availableAuthorization);
            goal.ReplacementProcurementPolicyId =
                MilitaryLogisticsReplacementProcurementPolicyIds
                    .ExplicitAuthorization;
            goal.LastReplacementAuthorizedDay = world.AbsoluteDay;
            goal.LastReplacementAuthorizedByPersonId = actorPersonId.Value;
            goal.LastReplacementAuthorizationReasonId = reasonId;
            AddReport(
                world,
                goal,
                actorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds
                    .ReplacementAuthorized,
                false,
                string.Empty,
                string.Empty,
                $"Authorized replacement procurement for " +
                $"{goal.OutstandingCargoQuantity} units while hostile " +
                "cargo custody remains unresolved.",
                true);
            world.Validate();
        }

        public long CollectOutstandingLiability(
            WorldState world,
            string settlementId,
            StableId actorPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var settlement = FindLiabilitySettlement(world, settlementId);
            var goal = FindGoal(world, settlement.GoalId);
            if (goal.IssuerPersonId != actorPersonId.Value ||
                settlement.Status !=
                    MilitaryLogisticsLiabilitySettlementStatus.InArrears ||
                _authority.GetAuthority(
                    world,
                    actorPersonId,
                    new StableId(goal.TargetArmyId)) <
                    MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The liability collection request is invalid.");
            }

            var payer = FindOrganization(
                world, settlement.PayerOrganizationId);
            var payee = FindOrganization(
                world, settlement.PayeeOrganizationId);
            var paid = Math.Min(
                payer.Treasury, settlement.OutstandingAmount);
            if (paid <= 0)
            {
                return 0;
            }

            payer.Treasury = checked(payer.Treasury - paid);
            payee.Treasury = checked(payee.Treasury + paid);
            settlement.AmountPaid = checked(
                settlement.AmountPaid + paid);
            settlement.OutstandingAmount = checked(
                settlement.AmountDue - settlement.AmountPaid);
            settlement.LastPaymentDay = world.AbsoluteDay;
            settlement.Status = settlement.OutstandingAmount == 0
                ? MilitaryLogisticsLiabilitySettlementStatus.Settled
                : MilitaryLogisticsLiabilitySettlementStatus.InArrears;
            goal.CompensationReceived = checked(
                goal.CompensationReceived + paid);
            AddReport(
                world,
                goal,
                actorPersonId.Value,
                MilitaryLogisticsDelegationReportTypeIds.LiabilityPayment,
                false,
                string.Empty,
                settlement.LogisticsOrderId,
                $"Collected {paid} from liability settlement " +
                $"{settlement.Id}; outstanding " +
                $"{settlement.OutstandingAmount}.",
                true);
            world.Validate();
            return paid;
        }

        public MilitaryLogisticsOrderState EvaluateAndDispatch(
            WorldState world,
            string goalId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var goal = FindGoal(world, goalId);
            if (goal.Status == MilitaryLogisticsDelegationStatus.Dispatched ||
                goal.Status == MilitaryLogisticsDelegationStatus.Cancelled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Fulfilled ||
                goal.Status == MilitaryLogisticsDelegationStatus.Expired ||
                goal.Status == MilitaryLogisticsDelegationStatus.Delegated ||
                goal.ChildGoalIds.Count != 0)
            {
                throw new InvalidOperationException(
                    "The logistics delegation is no longer open.");
            }

            if (RequiresReplacementAuthorization(world, goal))
            {
                return ReportException(
                    world,
                    goal,
                    MilitaryLogisticsDelegationReportTypeIds
                        .ReplacementAuthorizationRequired,
                    "Replacement procurement requires explicit authorization while seized cargo remains in hostile custody.",
                    string.Empty);
            }

            goal.LastEvaluatedDay = world.AbsoluteDay;
            if (world.AbsoluteDay > goal.DeadlineDay)
            {
                goal.Status = MilitaryLogisticsDelegationStatus.Expired;
                AddReport(
                    world,
                    goal,
                    goal.IssuerPersonId,
                    MilitaryLogisticsDelegationReportTypeIds.DeadlineExpired,
                    true,
                    string.Empty,
                    string.Empty,
                    "军需目标已经超过期限。",
                    true);
                world.Validate();
                return null;
            }

            if (_authority.GetAuthority(
                    world,
                    new StableId(goal.IssuerPersonId),
                    new StableId(goal.TargetArmyId)) <
                MilitaryAuthorityLevel.Army)
            {
                return ReportException(
                    world,
                    goal,
                    MilitaryLogisticsDelegationReportTypeIds.AuthorityLost,
                    "签发人已经失去目标军队的军级权限。",
                    string.Empty);
            }

            var assignee = PeopleFor(world).GetRequired(
                goal.AssigneePersonId);
            if (!assignee.IsAlive ||
                _authority.GetAuthority(
                    world,
                    new StableId(goal.AssigneePersonId),
                    new StableId(goal.TargetArmyId)) <
                goal.AssigneeAuthorityAtDelegation)
            {
                return ReportException(
                    world,
                    goal,
                    MilitaryLogisticsDelegationReportTypeIds
                        .AssigneeUnavailable,
                    "受任人已经死亡、离队或失去委任所需军职权限。",
                    string.Empty);
            }

            var activeOffers = FindActiveOffers(world, goal.Id);
            if (activeOffers.Count == 0)
            {
                return ReportException(
                    world,
                    goal,
                    MilitaryLogisticsDelegationReportTypeIds.NoOffer,
                    "军需目标尚无可评估的承运报价。",
                    string.Empty);
            }

            var candidates = new List<OfferCandidate>();
            var budgetRejected = false;
            var lastInvalidOfferId = string.Empty;
            for (var i = 0; i < activeOffers.Count; i++)
            {
                if (TryCreateCandidate(
                        world,
                        goal,
                        activeOffers[i],
                        out var candidate,
                        out var rejectedForBudget))
                {
                    candidates.Add(candidate);
                }
                else
                {
                    budgetRejected |= rejectedForBudget;
                    if (!rejectedForBudget)
                    {
                        lastInvalidOfferId = activeOffers[i].Id;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return ReportException(
                    world,
                    goal,
                    budgetRejected
                        ? MilitaryLogisticsDelegationReportTypeIds.BudgetExceeded
                        : MilitaryLogisticsDelegationReportTypeIds
                            .OfferInvalidated,
                    budgetRejected
                        ? "现有承运报价超过单价、预算或当前可用军费。"
                        : "现有承运报价的人物、库存、口粮或路线已经失效。",
                    lastInvalidOfferId);
            }

            candidates.Sort((left, right) =>
                CompareCandidates(goal, left, right));
            var selected = candidates[0];
            try
            {
                var order = _logistics.Dispatch(
                    world,
                    BuildDispatchRequest(goal, selected.Offer));
                ConsumeReplacementAuthorization(world, goal);
                selected.Offer.Status =
                    MilitaryLogisticsDelegationOfferStatus.Selected;
                selected.Offer.LogisticsOrderId = order.Id;
                goal.Status = MilitaryLogisticsDelegationStatus.Dispatched;
                goal.SelectedOfferId = selected.Offer.Id;
                goal.LogisticsOrderId = order.Id;
                goal.CommittedCost = checked(
                    goal.CommittedCost + order.TotalPaid);
                goal.NextEvaluationDay = checked(
                    world.AbsoluteDay + goal.ReportIntervalDays);
                AddReport(
                    world,
                    goal,
                    goal.IssuerPersonId,
                    goal.CompletedLogisticsOrderIds.Count == 0
                        ? MilitaryLogisticsDelegationReportTypeIds.Dispatched
                        : MilitaryLogisticsDelegationReportTypeIds
                            .SupplementalDispatched,
                    false,
                    selected.Offer.Id,
                    order.Id,
                    $"军需目标{goal.Id}采用报价{selected.Offer.Id}并生成" +
                    $"货运单{order.Id}。",
                    false);
                world.Validate();
                return order;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException ||
                exception is ArgumentOutOfRangeException)
            {
                return ReportException(
                    world,
                    goal,
                    MilitaryLogisticsDelegationReportTypeIds.DispatchRejected,
                    $"领域物流规则拒绝报价{selected.Offer.Id}：" +
                    exception.Message,
                    selected.Offer.Id);
            }
        }

        private static void ExpireOffers(WorldState world)
        {
            var offers = new List<MilitaryLogisticsDelegationOfferState>(
                world.MilitaryLogisticsDelegationOffers);
            offers.Sort((left, right) =>
            {
                var dayOrder = left.ValidUntilDay.CompareTo(
                    right.ValidUntilDay);
                return dayOrder != 0
                    ? dayOrder
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            for (var i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                if (offer.Status !=
                        MilitaryLogisticsDelegationOfferStatus.Active ||
                    offer.ValidUntilDay >= world.AbsoluteDay)
                {
                    continue;
                }

                var goal = FindGoal(world, offer.GoalId);
                offer.Status = MilitaryLogisticsDelegationOfferStatus.Expired;
                offer.ClosedDay = world.AbsoluteDay;
                AddReport(
                    world,
                    goal,
                    offer.CarrierPersonId,
                    MilitaryLogisticsDelegationReportTypeIds.OfferExpired,
                    false,
                    offer.Id,
                    string.Empty,
                    $"承运报价{offer.Id}超过有效期，已退出候选。",
                    false);
                if ((goal.Status ==
                         MilitaryLogisticsDelegationStatus.Pending ||
                     goal.Status ==
                         MilitaryLogisticsDelegationStatus.NeedsAttention) &&
                    FindActiveOffers(world, goal.Id).Count == 0)
                {
                    goal.Status =
                        MilitaryLogisticsDelegationStatus.NeedsAttention;
                }
            }
        }

        private static void ProcessDispatchedGoal(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal)
        {
            var order = FindLogisticsOrder(world, goal.LogisticsOrderId);
            if (order.Status == MilitaryLogisticsStatus.Delivered)
            {
                SettleLiability(world, goal, order);
                var completedOfferId = goal.SelectedOfferId;
                var completedOffer = FindOffer(world, completedOfferId);
                completedOffer.Status =
                    MilitaryLogisticsDelegationOfferStatus.Completed;
                completedOffer.ClosedDay = Math.Max(
                    completedOffer.SubmittedDay, order.DeliveredDay);
                if (!goal.CompletedLogisticsOrderIds.Contains(order.Id))
                {
                    goal.CompletedLogisticsOrderIds.Add(order.Id);
                    goal.ReceivedCargoQuantity = checked(
                        goal.ReceivedCargoQuantity +
                        order.DeliveredCargoQuantity);
                    goal.OutstandingCargoQuantity = checked(
                        goal.RequestedCargoQuantity -
                        goal.ReceivedCargoQuantity);
                }

                goal.SelectedOfferId = string.Empty;
                goal.LogisticsOrderId = string.Empty;
                AddReport(
                    world,
                    goal,
                    goal.IssuerPersonId,
                    MilitaryLogisticsDelegationReportTypeIds.AttemptCompleted,
                    false,
                    completedOfferId,
                    order.Id,
                    $"军需目标{goal.Id}已完成：交付" +
                    $"{order.DeliveredCargoQuantity}，自然损耗" +
                    $"{order.NaturalLossQuantity}，敌对损失" +
                    $"{order.HostileLossQuantity}，夺回" +
                    $"{order.RecoveredCargoQuantity}。",
                    false);

                if (goal.OutstandingCargoQuantity == 0)
                {
                    goal.Status =
                        MilitaryLogisticsDelegationStatus.Fulfilled;
                    goal.FulfilledDay = world.AbsoluteDay;
                    AddReport(
                        world,
                        goal,
                        goal.IssuerPersonId,
                        MilitaryLogisticsDelegationReportTypeIds.Fulfilled,
                        false,
                        completedOfferId,
                        order.Id,
                        $"Goal {goal.Id} received the full requested cargo " +
                        $"quantity {goal.ReceivedCargoQuantity}.",
                        true);
                    return;
                }

                if (world.AbsoluteDay > goal.DeadlineDay)
                {
                    goal.Status = MilitaryLogisticsDelegationStatus.Expired;
                    AddReport(
                        world,
                        goal,
                        goal.IssuerPersonId,
                        MilitaryLogisticsDelegationReportTypeIds.DeadlineExpired,
                        true,
                        completedOfferId,
                        order.Id,
                        $"Goal {goal.Id} expired with outstanding cargo " +
                        $"quantity {goal.OutstandingCargoQuantity}.",
                        true);
                    return;
                }

                goal.Status =
                    MilitaryLogisticsDelegationStatus.NeedsAttention;
                goal.NextEvaluationDay = Math.Min(
                    goal.DeadlineDay,
                    checked(world.AbsoluteDay + goal.ReportIntervalDays));
                AddReport(
                    world,
                    goal,
                    goal.IssuerPersonId,
                    MilitaryLogisticsDelegationReportTypeIds.DeliveryShortfall,
                    true,
                    completedOfferId,
                    order.Id,
                    $"Goal {goal.Id} has received " +
                    $"{goal.ReceivedCargoQuantity} cargo and still requires " +
                    $"{goal.OutstandingCargoQuantity}.",
                    true);
                return;
            }

            if (goal.NextEvaluationDay > world.AbsoluteDay)
            {
                return;
            }

            AddReport(
                world,
                goal,
                goal.IssuerPersonId,
                MilitaryLogisticsDelegationReportTypeIds.Progress,
                false,
                goal.SelectedOfferId,
                order.Id,
                $"军需目标{goal.Id}在途：剩余" +
                $"{order.RemainingCargoQuantity}，已交付" +
                $"{order.DeliveredCargoQuantity}，自然损耗" +
                $"{order.NaturalLossQuantity}，敌对损失" +
                $"{order.HostileLossQuantity}，夺回" +
                $"{order.RecoveredCargoQuantity}。",
                false);
            goal.NextEvaluationDay = checked(
                world.AbsoluteDay + goal.ReportIntervalDays);
        }

        private static void ProcessDelegatedGoal(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal)
        {
            var children = RefreshParentReceipt(world, goal);

            if (world.AbsoluteDay > goal.DeadlineDay)
            {
                goal.Status = MilitaryLogisticsDelegationStatus.Expired;
                AddReport(
                    world,
                    goal,
                    goal.AssigneePersonId,
                    MilitaryLogisticsDelegationReportTypeIds.DeadlineExpired,
                    true,
                    string.Empty,
                    string.Empty,
                    "父级军需目标在全部子目标完成前超过期限。",
                    true);
                return;
            }

            var fulfilledCount = 0;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Status ==
                    MilitaryLogisticsDelegationStatus.Fulfilled)
                {
                    fulfilledCount++;
                }
            }

            if (goal.UnassignedCargoQuantity == 0 &&
                fulfilledCount == children.Count &&
                goal.OutstandingCargoQuantity == 0)
            {
                goal.Status = MilitaryLogisticsDelegationStatus.Fulfilled;
                goal.FulfilledDay = world.AbsoluteDay;
                AddReport(
                    world,
                    goal,
                    goal.AssigneePersonId,
                    MilitaryLogisticsDelegationReportTypeIds.Fulfilled,
                    false,
                    string.Empty,
                    string.Empty,
                    $"父级军需目标{goal.Id}的{children.Count}个子目标" +
                    $"全部完成，累计实际交付{goal.ReceivedCargoQuantity}。",
                    true);
                return;
            }

            if (goal.NextEvaluationDay > world.AbsoluteDay)
            {
                return;
            }

            var exceptionCount = 0;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Status !=
                        MilitaryLogisticsDelegationStatus.NeedsAttention &&
                    child.Status !=
                        MilitaryLogisticsDelegationStatus.Expired)
                {
                    continue;
                }

                exceptionCount++;
                AddReport(
                    world,
                    goal,
                    goal.AssigneePersonId,
                    MilitaryLogisticsDelegationReportTypeIds.ChildException,
                    true,
                    string.Empty,
                    string.Empty,
                    $"子目标{child.Id}当前状态为{child.Status}。",
                    true,
                    child.Id);
            }

            if (goal.UnassignedCargoQuantity > 0)
            {
                exceptionCount++;
                AddReport(
                    world,
                    goal,
                    goal.AssigneePersonId,
                    MilitaryLogisticsDelegationReportTypeIds.AllocationGap,
                    true,
                    string.Empty,
                    string.Empty,
                    $"父级军需目标{goal.Id}仍有未分配数量" +
                    $"{goal.UnassignedCargoQuantity}、预算储备" +
                    $"{goal.AvailableBudgetReserve}。",
                    true);
            }

            AddReport(
                world,
                goal,
                goal.AssigneePersonId,
                MilitaryLogisticsDelegationReportTypeIds.DelegatedProgress,
                false,
                string.Empty,
                string.Empty,
                $"父级军需目标{goal.Id}进度：完成{fulfilledCount}/" +
                $"{children.Count}，未分配{goal.UnassignedCargoQuantity}，" +
                $"异常{exceptionCount}。",
                false);
            goal.NextEvaluationDay = Math.Min(
                checked(world.AbsoluteDay + goal.ReportIntervalDays),
                checked(goal.DeadlineDay + 1));
        }

        private static List<MilitaryLogisticsDelegationGoalState>
            RefreshParentReceipt(
                WorldState world,
                MilitaryLogisticsDelegationGoalState goal)
        {
            var children = FindActiveChildren(world, goal);
            long receivedQuantity = 0;
            for (var i = 0; i < children.Count; i++)
            {
                receivedQuantity = checked(
                    receivedQuantity + children[i].ReceivedCargoQuantity);
            }
            goal.ReceivedCargoQuantity = checked((int)receivedQuantity);
            if (goal.FulfillmentPolicyId ==
                MilitaryLogisticsDelegationFulfillmentPolicyIds
                    .FullReceiptRequired)
            {
                goal.OutstandingCargoQuantity = checked(
                    goal.RequestedCargoQuantity -
                    goal.ReceivedCargoQuantity);
            }
            return children;
        }

        private static void SettleLiability(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal,
            MilitaryLogisticsOrderState order)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsLiabilitySettlements.Count;
                 i++)
            {
                if (world.MilitaryLogisticsLiabilitySettlements[i]
                        .LogisticsOrderId == order.Id)
                {
                    return;
                }
            }

            var compensableLoss = checked(
                order.NaturalLossQuantity + order.HostileLossQuantity);
            var amountDue = 0L;
            if (order.LiabilityPolicyId ==
                    MilitaryLogisticsLiabilityPolicyIds
                        .LossBearerCompensates &&
                order.LossBearerOrganizationId !=
                    order.BuyerOrganizationId &&
                order.UnitPrice > 0)
            {
                amountDue = Math.Min(
                    order.TotalPaid,
                    checked(order.UnitPrice * compensableLoss));
            }

            var payer = FindOrganization(
                world, order.LossBearerOrganizationId);
            var payee = FindOrganization(
                world, order.BuyerOrganizationId);
            var amountPaid = amountDue == 0
                ? 0
                : Math.Min(payer.Treasury, amountDue);
            if (amountPaid > 0)
            {
                payer.Treasury = checked(payer.Treasury - amountPaid);
                payee.Treasury = checked(payee.Treasury + amountPaid);
                goal.CompensationReceived = checked(
                    goal.CompensationReceived + amountPaid);
            }

            var outstanding = checked(amountDue - amountPaid);
            var settlement = new MilitaryLogisticsLiabilitySettlementState
            {
                Id = $"military_logistics_liability.{order.Id}",
                CreatedDay = world.AbsoluteDay,
                LastPaymentDay = amountPaid > 0
                    ? world.AbsoluteDay
                    : -1,
                GoalId = goal.Id,
                LogisticsOrderId = order.Id,
                LiabilityPolicyId = order.LiabilityPolicyId,
                PayerOrganizationId = order.LossBearerOrganizationId,
                PayeeOrganizationId = order.BuyerOrganizationId,
                NaturalLossQuantity = order.NaturalLossQuantity,
                HostileLossQuantity = order.HostileLossQuantity,
                UnitValue = order.UnitPrice,
                AmountDue = amountDue,
                AmountPaid = amountPaid,
                OutstandingAmount = outstanding,
                Status = outstanding == 0
                    ? MilitaryLogisticsLiabilitySettlementStatus.Settled
                    : MilitaryLogisticsLiabilitySettlementStatus.InArrears
            };
            world.MilitaryLogisticsLiabilitySettlements.Add(settlement);
            AddReport(
                world,
                goal,
                goal.IssuerPersonId,
                outstanding == 0
                    ? MilitaryLogisticsDelegationReportTypeIds
                        .LiabilitySettled
                    : MilitaryLogisticsDelegationReportTypeIds
                        .LiabilityArrears,
                outstanding != 0,
                goal.SelectedOfferId,
                order.Id,
                $"Liability settlement {settlement.Id}: due {amountDue}, " +
                $"paid {amountPaid}, outstanding {outstanding}.",
                true);
        }

        private static bool RequiresReplacementAuthorization(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal)
        {
            if (goal.ReplacementProcurementPolicyId ==
                MilitaryLogisticsReplacementProcurementPolicyIds
                    .LegacyUnrestricted)
            {
                return false;
            }

            if (UnresolvedHostileCustody(world, goal) <= 0)
            {
                return false;
            }

            var available = checked(
                goal.AuthorizedReplacementQuantity -
                goal.ConsumedReplacementAuthorizationQuantity);
            return goal.ReplacementProcurementPolicyId !=
                    MilitaryLogisticsReplacementProcurementPolicyIds
                        .ExplicitAuthorization ||
                available < goal.OutstandingCargoQuantity;
        }

        private static void ConsumeReplacementAuthorization(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal)
        {
            if (goal.ReplacementProcurementPolicyId !=
                    MilitaryLogisticsReplacementProcurementPolicyIds
                        .ExplicitAuthorization ||
                UnresolvedHostileCustody(world, goal) <= 0)
            {
                return;
            }

            goal.ConsumedReplacementAuthorizationQuantity = checked(
                goal.ConsumedReplacementAuthorizationQuantity +
                goal.OutstandingCargoQuantity);
        }

        private static int UnresolvedHostileCustody(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal)
        {
            var completedIds = new HashSet<string>(
                goal.CompletedLogisticsOrderIds, StringComparer.Ordinal);
            var custody = 0;
            for (var i = 0;
                 i < world.MilitaryLogisticsIncidents.Count;
                 i++)
            {
                var incident = world.MilitaryLogisticsIncidents[i];
                if (completedIds.Contains(incident.LogisticsOrderId))
                {
                    custody = checked(
                        custody + incident.SeizedCargoQuantity -
                        incident.RecoveredCargoQuantity);
                }
            }
            return custody;
        }

        private MilitaryLogisticsOrderState ReportException(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal,
            string reportTypeId,
            string summary,
            string relatedOfferId)
        {
            goal.Status = MilitaryLogisticsDelegationStatus.NeedsAttention;
            goal.NextEvaluationDay = Math.Min(
                checked(world.AbsoluteDay + goal.ReportIntervalDays),
                checked(goal.DeadlineDay + 1));
            AddReport(
                world,
                goal,
                goal.IssuerPersonId,
                reportTypeId,
                true,
                relatedOfferId,
                string.Empty,
                summary,
                true);
            world.Validate();
            return null;
        }

        private bool TryCreateCandidate(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal,
            MilitaryLogisticsDelegationOfferState offer,
            out OfferCandidate candidate,
            out bool rejectedForBudget)
        {
            candidate = null;
            rejectedForBudget = false;
            try
            {
                var carrier = PeopleFor(world).GetRequired(
                    offer.CarrierPersonId);
                var transportContainer = FindContainerByCarrier(
                    world, carrier.Id);
                var cargoBatch = FindBatch(world, offer.SourceCargoBatchId);
                var cargoContainer = FindContainer(
                    world, cargoBatch.InventoryContainerId);
                var route = FindRoute(world, offer.RouteId);
                if (!carrier.IsAlive ||
                    transportContainer.Id !=
                        offer.TransportInventoryContainerId ||
                    carrier.LocationId != offer.OriginLocationId ||
                    transportContainer.LocationId != offer.OriginLocationId ||
                    transportContainer.OwnerOrganizationId !=
                        offer.CarrierOrganizationId ||
                    !HasMembership(
                        world, carrier.Id, offer.CarrierOrganizationId) ||
                    cargoBatch.ProductDefinitionId !=
                        goal.ProductDefinitionId ||
                    cargoBatch.Quantity - cargoBatch.ReservedQuantity <
                        goal.OutstandingCargoQuantity ||
                    cargoContainer.LocationId != offer.OriginLocationId ||
                    !RouteConnects(
                        route,
                        offer.OriginLocationId,
                        goal.DestinationLocationId))
                {
                    return false;
                }

                ValidateCurrentProvision(
                    world, goal, offer, cargoBatch);
                var army = FindArmy(world, goal.TargetArmyId);
                ValidateOfferOwnershipAndPrice(
                    offer.AcquisitionMethodId,
                    army.OrganizationId,
                    cargoBatch.OwnerOrganizationId,
                    offer.UnitPrice);
                var totalCost = checked(
                    offer.UnitPrice * goal.OutstandingCargoQuantity);
                var cumulativeCost = checked(
                    goal.CommittedCost - goal.CompensationReceived +
                    totalCost);
                var buyer = FindOrganization(world, army.OrganizationId);
                if (offer.UnitPrice > goal.MaximumUnitPrice ||
                    cumulativeCost > goal.BudgetLimit ||
                    totalCost > buyer.Treasury)
                {
                    rejectedForBudget = true;
                    return false;
                }

                candidate = new OfferCandidate
                {
                    Offer = offer,
                    TotalCost = totalCost,
                    RouteSecurityBasisPoints = route.SecurityBasisPoints,
                    UsesBuyerCarrier =
                        offer.CarrierOrganizationId == army.OrganizationId
                };
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (OverflowException)
            {
                rejectedForBudget = true;
                return false;
            }
        }

        private static int CompareCandidates(
            MilitaryLogisticsDelegationGoalState goal,
            OfferCandidate left,
            OfferCandidate right)
        {
            int comparison;
            if (goal.CarrierPreferenceId ==
                MilitaryLogisticsDelegationCarrierPreferenceIds
                    .OwnOrganizationFirst)
            {
                comparison = right.UsesBuyerCarrier.CompareTo(
                    left.UsesBuyerCarrier);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            if (goal.CarrierPreferenceId ==
                MilitaryLogisticsDelegationCarrierPreferenceIds.SafestRoute)
            {
                comparison = right.RouteSecurityBasisPoints.CompareTo(
                    left.RouteSecurityBasisPoints);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            comparison = left.TotalCost.CompareTo(right.TotalCost);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = right.RouteSecurityBasisPoints.CompareTo(
                left.RouteSecurityBasisPoints);
            return comparison != 0
                ? comparison
                : string.CompareOrdinal(left.Offer.Id, right.Offer.Id);
        }

        private static MilitaryLogisticsDispatchRequest BuildDispatchRequest(
            MilitaryLogisticsDelegationGoalState goal,
            MilitaryLogisticsDelegationOfferState offer)
        {
            return new MilitaryLogisticsDispatchRequest
            {
                IssuerPersonId = new StableId(goal.IssuerPersonId),
                CarrierPersonId = new StableId(offer.CarrierPersonId),
                TargetArmyId = new StableId(goal.TargetArmyId),
                SourceCargoBatchId = new StableId(
                    offer.SourceCargoBatchId),
                SourceProvisionBatchId = offer.SourceProvisionBatchId,
                RouteId = new StableId(offer.RouteId),
                DestinationLocationId = new StableId(
                    goal.DestinationLocationId),
                AcquisitionMethodId = offer.AcquisitionMethodId,
                CarrierOrganizationId = offer.CarrierOrganizationId,
                LossBearerOrganizationId = offer.LossBearerOrganizationId,
                LiabilityPolicyId = offer.LiabilityPolicyId,
                CargoConsumptionPolicyId = goal.CargoConsumptionPolicyId,
                CargoQuantity = goal.OutstandingCargoQuantity,
                ConvoyProvisionQuantity = offer.ConvoyProvisionQuantity,
                DailyConvoyProvisionUse = offer.DailyConvoyProvisionUse,
                UnitPrice = offer.UnitPrice,
                RiskPolicyId = goal.RiskPolicyId,
                ThreatOrganizationId = goal.ThreatOrganizationId
            };
        }

        private static void ValidatePreference(string preferenceId)
        {
            _ = new StableId(preferenceId);
            if (preferenceId !=
                    MilitaryLogisticsDelegationCarrierPreferenceIds.LowestCost &&
                preferenceId !=
                    MilitaryLogisticsDelegationCarrierPreferenceIds.SafestRoute &&
                preferenceId !=
                    MilitaryLogisticsDelegationCarrierPreferenceIds
                        .OwnOrganizationFirst)
            {
                throw new InvalidOperationException(
                    $"Unsupported carrier preference {preferenceId}.");
            }
        }

        private MilitaryAuthorityLevel ValidateParentAssigneeAuthority(
            WorldState world,
            MilitaryLogisticsDelegationGoalState parent,
            StableId actorPersonId)
        {
            if (parent.AssigneePersonId != actorPersonId.Value ||
                _authority.GetAuthority(
                    world,
                    new StableId(parent.IssuerPersonId),
                    new StableId(parent.TargetArmyId)) <
                    MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The actor lacks authority over this delegated allocation.");
            }

            var actor = PeopleFor(world).GetRequired(actorPersonId.Value);
            var authority = _authority.GetAuthority(
                world,
                actorPersonId,
                new StableId(parent.TargetArmyId));
            if (!actor.IsAlive ||
                authority < parent.AssigneeAuthorityAtDelegation ||
                authority <= MilitaryAuthorityLevel.Self)
            {
                throw new InvalidOperationException(
                    "The parent assignee is unavailable or lacks delegation authority.");
            }

            return authority;
        }

        private static void ValidateRisk(
            WorldState world,
            string riskPolicyId,
            string threatOrganizationId)
        {
            _ = new StableId(riskPolicyId);
            if (riskPolicyId == MilitaryLogisticsRiskPolicyIds.None &&
                string.IsNullOrEmpty(threatOrganizationId))
            {
                return;
            }

            if (riskPolicyId != MilitaryLogisticsRiskPolicyIds.Standard ||
                string.IsNullOrWhiteSpace(threatOrganizationId))
            {
                throw new InvalidOperationException(
                    "The delegated logistics risk contract is unsupported.");
            }

            _ = FindOrganization(world, threatOrganizationId);
        }

        private static void ValidateOfferMethod(string acquisitionMethodId)
        {
            _ = new StableId(acquisitionMethodId);
            if (acquisitionMethodId !=
                    MilitarySupplyAcquisitionMethodIds.CommercialPurchase &&
                acquisitionMethodId !=
                    MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer)
            {
                throw new InvalidOperationException(
                    "Carrier offers only support purchase or internal transfer.");
            }
        }

        private static void ValidateOfferOwnershipAndPrice(
            string acquisitionMethodId,
            string buyerOrganizationId,
            string sourceOrganizationId,
            long unitPrice)
        {
            if (acquisitionMethodId ==
                MilitarySupplyAcquisitionMethodIds.CommercialPurchase)
            {
                if (buyerOrganizationId == sourceOrganizationId ||
                    unitPrice <= 0)
                {
                    throw new InvalidOperationException(
                        "Commercial offers need an external source and positive price.");
                }
                return;
            }

            if (acquisitionMethodId !=
                    MilitarySupplyAcquisitionMethodIds.InternalDepotTransfer ||
                buyerOrganizationId != sourceOrganizationId ||
                unitPrice != 0)
            {
                throw new InvalidOperationException(
                    "Internal offers require common ownership and zero price.");
            }
        }

        private static void ValidateProvisionOffer(
            WorldState world,
            MilitaryLogisticsDelegationOfferRequest request,
            ProductBatchState cargoBatch,
            string originLocationId)
        {
            if (request.ConvoyProvisionQuantity == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(request.SourceProvisionBatchId))
            {
                throw new InvalidOperationException(
                    "A convoy provision batch is required.");
            }

            var provisionBatch = FindBatch(
                world, request.SourceProvisionBatchId);
            var required = provisionBatch.Id == cargoBatch.Id
                ? checked((long)request.CargoQuantity +
                    request.ConvoyProvisionQuantity)
                : request.ConvoyProvisionQuantity;
            if (provisionBatch.Quantity - provisionBatch.ReservedQuantity <
                    required ||
                provisionBatch.OwnerOrganizationId !=
                    request.CarrierOrganizationId ||
                FindContainer(world, provisionBatch.InventoryContainerId)
                    .LocationId != originLocationId)
            {
                throw new InvalidOperationException(
                    "The proposed carrier lacks valid self-provisions.");
            }
        }

        private static void ValidateCurrentProvision(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal,
            MilitaryLogisticsDelegationOfferState offer,
            ProductBatchState cargoBatch)
        {
            if (offer.ConvoyProvisionQuantity == 0)
            {
                return;
            }

            var provisionBatch = FindBatch(
                world, offer.SourceProvisionBatchId);
            var required = provisionBatch.Id == cargoBatch.Id
                ? checked((long)goal.RequestedCargoQuantity +
                    offer.ConvoyProvisionQuantity)
                : offer.ConvoyProvisionQuantity;
            if (provisionBatch.Quantity - provisionBatch.ReservedQuantity <
                    required ||
                provisionBatch.OwnerOrganizationId !=
                    offer.CarrierOrganizationId ||
                FindContainer(world, provisionBatch.InventoryContainerId)
                    .LocationId != offer.OriginLocationId)
            {
                throw new InvalidOperationException(
                    "The carrier self-provisions are no longer available.");
            }
        }

        private static void AddReport(
            WorldState world,
            MilitaryLogisticsDelegationGoalState goal,
            string actorPersonId,
            string typeId,
            bool isException,
            string relatedOfferId,
            string logisticsOrderId,
            string summary,
            bool deduplicate,
            string relatedGoalId = "")
        {
            if (deduplicate)
            {
                for (var i = 0;
                     i < world.MilitaryLogisticsDelegationReports.Count;
                     i++)
                {
                    var existing =
                        world.MilitaryLogisticsDelegationReports[i];
                    if (existing.GoalId == goal.Id &&
                        existing.Day == world.AbsoluteDay &&
                        existing.TypeId == typeId &&
                        existing.RelatedOfferId == relatedOfferId &&
                        existing.RelatedGoalId == relatedGoalId)
                    {
                        return;
                    }
                }
            }

            world.MilitaryLogisticsDelegationReports.Add(
                new MilitaryLogisticsDelegationReportState
                {
                    Id = $"military_logistics_delegation_report." +
                         $"{world.AbsoluteDay}." +
                         $"{world.MilitaryLogisticsDelegationReports.Count}",
                    Day = world.AbsoluteDay,
                    GoalId = goal.Id,
                    ActorPersonId = actorPersonId,
                    TypeId = typeId,
                    IsException = isException,
                    RelatedOfferId = relatedOfferId ?? string.Empty,
                    RelatedGoalId = relatedGoalId ?? string.Empty,
                    LogisticsOrderId = logisticsOrderId ?? string.Empty,
                    Summary = summary
                });
        }

        private static List<MilitaryLogisticsDelegationOfferState>
            FindActiveOffers(WorldState world, string goalId)
        {
            var result = new List<MilitaryLogisticsDelegationOfferState>();
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                var offer = world.MilitaryLogisticsDelegationOffers[i];
                if (offer.GoalId == goalId &&
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Active)
                {
                    result.Add(offer);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static bool HasOffer(WorldState world, string goalId)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                if (world.MilitaryLogisticsDelegationOffers[i].GoalId == goalId)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<MilitaryLogisticsDelegationGoalState>
            FindChildren(
                WorldState world,
                MilitaryLogisticsDelegationGoalState parent)
        {
            var children = new List<MilitaryLogisticsDelegationGoalState>();
            for (var i = 0; i < parent.ChildGoalIds.Count; i++)
            {
                children.Add(FindGoal(world, parent.ChildGoalIds[i]));
            }
            children.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return children;
        }

        private static List<MilitaryLogisticsDelegationGoalState>
            FindActiveChildren(
                WorldState world,
                MilitaryLogisticsDelegationGoalState parent)
        {
            return FindChildren(world, parent).FindAll(item =>
                item.Status != MilitaryLogisticsDelegationStatus.Cancelled);
        }

        private IPersonRepository PeopleFor(WorldState world) =>
            _people ?? new WorldStatePersonRepository(world);

        private static MilitaryLogisticsDelegationGoalState FindGoal(
            WorldState world,
            string goalId)
        {
            _ = new StableId(goalId);
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                if (world.MilitaryLogisticsDelegationGoals[i].Id == goalId)
                {
                    return world.MilitaryLogisticsDelegationGoals[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics delegation goal {goalId}.");
        }

        private static MilitaryLogisticsLiabilitySettlementState
            FindLiabilitySettlement(WorldState world, string settlementId)
        {
            _ = new StableId(settlementId);
            for (var i = 0;
                 i < world.MilitaryLogisticsLiabilitySettlements.Count;
                 i++)
            {
                if (world.MilitaryLogisticsLiabilitySettlements[i].Id ==
                    settlementId)
                {
                    return world.MilitaryLogisticsLiabilitySettlements[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics liability settlement " +
                $"{settlementId}.");
        }

        private static MilitaryLogisticsDelegationOfferState FindOffer(
            WorldState world,
            string offerId)
        {
            _ = new StableId(offerId);
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                if (world.MilitaryLogisticsDelegationOffers[i].Id == offerId)
                {
                    return world.MilitaryLogisticsDelegationOffers[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics delegation offer {offerId}.");
        }

        private static MilitaryLogisticsOrderState FindLogisticsOrder(
            WorldState world,
            string orderId)
        {
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                if (world.MilitaryLogisticsOrders[i].Id == orderId)
                {
                    return world.MilitaryLogisticsOrders[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics order {orderId}.");
        }

        private static ProductBatchState FindBatch(WorldState world, string id)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == id)
                {
                    return world.ProductBatches[i];
                }
            }

            throw new InvalidOperationException($"Missing product batch {id}.");
        }

        private static InventoryContainerState FindContainer(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return world.InventoryContainers[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing inventory container {id}.");
        }

        private static InventoryContainerState FindContainerByCarrier(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].CarrierPersonId == personId)
                {
                    return world.InventoryContainers[i];
                }
            }

            throw new InvalidOperationException(
                $"Carrier {personId} has no inventory container.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == id)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == id)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {id}.");
        }

        private static LocationState FindLocation(WorldState world, string id)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == id)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing location {id}.");
        }

        private static RouteState FindRoute(WorldState world, string id)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == id)
                {
                    return world.Routes[i];
                }
            }

            throw new InvalidOperationException($"Missing route {id}.");
        }

        private static bool HasMembership(
            WorldState world,
            string personId,
            string organizationId)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == personId &&
                    world.Memberships[i].OrganizationId == organizationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RouteConnects(
            RouteState route,
            string origin,
            string destination)
        {
            return route.FromLocationId == origin &&
                   route.ToLocationId == destination ||
                   route.Bidirectional &&
                   route.ToLocationId == origin &&
                   route.FromLocationId == destination;
        }

        private sealed class OfferCandidate
        {
            public MilitaryLogisticsDelegationOfferState Offer;
            public long TotalCost;
            public int RouteSecurityBasisPoints;
            public bool UsesBuyerCarrier;
        }

        private sealed class ValidatedSubgoal
        {
            public MilitaryLogisticsSubgoalRequest Request;
            public MilitaryAuthorityLevel AssigneeAuthority;
        }
    }
}
