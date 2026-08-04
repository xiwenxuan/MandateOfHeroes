using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class MilitaryLogisticsDelegationContract
    {
        public const int MaximumDelegationDepth = 2;
        public const int MaximumDirectSubgoals = 8;
    }

    public static class MilitarySupplyAcquisitionMethodIds
    {
        public const string CommercialPurchase =
            "military_supply.acquisition.commercial_purchase";
        public const string InternalDepotTransfer =
            "military_supply.acquisition.internal_depot_transfer";
        public const string CompensatedRequisition =
            "military_supply.acquisition.compensated_requisition";
        public const string ForcedRequisition =
            "military_supply.acquisition.forced_requisition";
        public const string Plunder =
            "military_supply.acquisition.plunder";
    }

    public static class MilitaryCargoConsumptionPolicyIds
    {
        public const string Prohibited =
            "military_supply.cargo_consumption.prohibited";
        public const string EmergencyAuthorized =
            "military_supply.cargo_consumption.emergency_authorized";
    }

    public static class MilitaryLogisticsRiskPolicyIds
    {
        public const string None = "military_logistics.risk.none";
        public const string Standard = "military_logistics.risk.standard";
    }

    public static class MilitaryLogisticsIncidentTypeIds
    {
        public const string BanditAttack =
            "military_logistics.incident.bandit_attack";
    }

    public static class MilitaryLogisticsIncidentOutcomeIds
    {
        public const string Avoided =
            "military_logistics.incident_outcome.avoided";
        public const string Repelled =
            "military_logistics.incident_outcome.repelled";
        public const string CargoSeized =
            "military_logistics.incident_outcome.cargo_seized";
    }

    public static class MilitaryLogisticsClashTypeIds
    {
        public const string InitialDefense =
            "military_logistics.clash.initial_defense";
        public const string RecoveryAttempt =
            "military_logistics.clash.recovery_attempt";
    }

    public static class MilitaryLogisticsClashOutcomeIds
    {
        public const string DefendersHeld =
            "military_logistics.clash_outcome.defenders_held";
        public const string AttackersSeizedCargo =
            "military_logistics.clash_outcome.attackers_seized_cargo";
        public const string CargoRecovered =
            "military_logistics.clash_outcome.cargo_recovered";
        public const string RecoveryFailed =
            "military_logistics.clash_outcome.recovery_failed";
    }

    public static class MilitaryLogisticsDelegationCarrierPreferenceIds
    {
        public const string LowestCost =
            "military_logistics.delegation.carrier_preference.lowest_cost";
        public const string SafestRoute =
            "military_logistics.delegation.carrier_preference.safest_route";
        public const string OwnOrganizationFirst =
            "military_logistics.delegation.carrier_preference.own_organization_first";
    }

    public static class MilitaryLogisticsCancellationReasonIds
    {
        public const string AssigneeUnavailable =
            "military_logistics.delegation.cancellation.assignee_unavailable";
        public const string DeadlineExpired =
            "military_logistics.delegation.cancellation.deadline_expired";
        public const string NoViableOffer =
            "military_logistics.delegation.cancellation.no_viable_offer";
        public const string SuperiorReassignment =
            "military_logistics.delegation.cancellation.superior_reassignment";
        public const string MigratedUnspecified =
            "military_logistics.delegation.cancellation.migrated_unspecified";
    }

    public static class MilitaryLogisticsDelegationFulfillmentPolicyIds
    {
        public const string FullReceiptRequired =
            "military_logistics.delegation.fulfillment.full_receipt_required";
        public const string LegacyOrderCompletion =
            "military_logistics.delegation.fulfillment.legacy_order_completion";
    }

    public static class MilitaryLogisticsLiabilityPolicyIds
    {
        public const string BuyerRetainsRisk =
            "military_logistics.liability.buyer_retains_risk";
        public const string LossBearerCompensates =
            "military_logistics.liability.loss_bearer_compensates";
        public const string LegacyNoRetroactiveSettlement =
            "military_logistics.liability.legacy_no_retroactive_settlement";
    }

    public static class MilitaryLogisticsReplacementProcurementPolicyIds
    {
        public const string WaitForCustodyResolution =
            "military_logistics.replacement.wait_for_custody_resolution";
        public const string ExplicitAuthorization =
            "military_logistics.replacement.explicit_authorization";
        public const string LegacyUnrestricted =
            "military_logistics.replacement.legacy_unrestricted";
    }

    public static class MilitaryLogisticsDelegationReportTypeIds
    {
        public const string GoalCreated =
            "military_logistics.delegation.report.goal_created";
        public const string OfferSubmitted =
            "military_logistics.delegation.report.offer_submitted";
        public const string Dispatched =
            "military_logistics.delegation.report.dispatched";
        public const string OfferWithdrawn =
            "military_logistics.delegation.report.offer_withdrawn";
        public const string OfferExpired =
            "military_logistics.delegation.report.offer_expired";
        public const string Progress =
            "military_logistics.delegation.report.progress";
        public const string Fulfilled =
            "military_logistics.delegation.report.fulfilled";
        public const string SubgoalCreated =
            "military_logistics.delegation.report.subgoal_created";
        public const string DelegatedProgress =
            "military_logistics.delegation.report.delegated_progress";
        public const string GoalCancelled =
            "military_logistics.delegation.report.goal_cancelled";
        public const string OfferClosedByCancellation =
            "military_logistics.delegation.report.offer_closed_by_cancellation";
        public const string AllocationRecovered =
            "military_logistics.delegation.report.allocation_recovered";
        public const string ReplacementGoalCreated =
            "military_logistics.delegation.report.replacement_goal_created";
        public const string SubgoalReassigned =
            "military_logistics.delegation.report.subgoal_reassigned";
        public const string AttemptCompleted =
            "military_logistics.delegation.report.attempt_completed";
        public const string SupplementalDispatched =
            "military_logistics.delegation.report.supplemental_dispatched";
        public const string NoOffer =
            "military_logistics.delegation.exception.no_offer";
        public const string OfferInvalidated =
            "military_logistics.delegation.exception.offer_invalidated";
        public const string BudgetExceeded =
            "military_logistics.delegation.exception.budget_exceeded";
        public const string AuthorityLost =
            "military_logistics.delegation.exception.authority_lost";
        public const string DeadlineExpired =
            "military_logistics.delegation.exception.deadline_expired";
        public const string DispatchRejected =
            "military_logistics.delegation.exception.dispatch_rejected";
        public const string AssigneeUnavailable =
            "military_logistics.delegation.exception.assignee_unavailable";
        public const string ChildException =
            "military_logistics.delegation.exception.child_exception";
        public const string AllocationGap =
            "military_logistics.delegation.exception.allocation_gap";
        public const string DeliveryShortfall =
            "military_logistics.delegation.exception.delivery_shortfall";
        public const string LiabilitySettled =
            "military_logistics.delegation.report.liability_settled";
        public const string LiabilityArrears =
            "military_logistics.delegation.exception.liability_arrears";
        public const string ReplacementAuthorizationRequired =
            "military_logistics.delegation.exception.replacement_authorization_required";
        public const string LiabilityPayment =
            "military_logistics.delegation.report.liability_payment";
        public const string ReplacementAuthorized =
            "military_logistics.delegation.report.replacement_authorized";
    }

    public enum MilitaryLogisticsDelegationStatus : byte
    {
        Pending,
        NeedsAttention,
        Dispatched,
        Cancelled,
        Fulfilled,
        Expired,
        Delegated
    }

    public enum MilitaryLogisticsDelegationOfferStatus : byte
    {
        Active,
        Selected,
        Withdrawn,
        Expired,
        GoalCancelled,
        Completed
    }

    public enum MilitaryLogisticsStatus : byte
    {
        InTransit,
        AwaitingArmy,
        Delivered,
        AwaitingHandoff
    }

    public enum MilitaryLogisticsLedgerType : byte
    {
        Dispatch,
        ConvoyProvisionConsumed,
        NaturalLoss,
        Delivery,
        Handoff,
        HostileCargoLoss,
        HostileCargoRecovered
    }

    public enum MilitaryLogisticsLiabilitySettlementStatus : byte
    {
        Settled,
        InArrears
    }

    public enum MilitaryLogisticsLegStatus : byte
    {
        Planned,
        InTransit,
        AwaitingHandoff,
        AwaitingReceipt,
        Completed
    }

    public enum MilitaryLogisticsEscortStatus : byte
    {
        Planned,
        InTransit,
        Arrived
    }

    [Serializable]
    public sealed class MilitaryLogisticsDelegationGoalState
    {
        public string Id;
        public long CreatedDay;
        public long DeadlineDay;
        public int ReportIntervalDays = 1;
        public long LastEvaluatedDay = -1;
        public long NextEvaluationDay;
        public long FulfilledDay = -1;
        public string ParentGoalId = string.Empty;
        public int DelegationDepth;
        public string AssigneePersonId;
        public string DelegatedByPersonId = string.Empty;
        public MilitaryAuthorityLevel AssigneeAuthorityAtDelegation;
        public List<string> ChildGoalIds = new List<string>();
        public int UnassignedCargoQuantity;
        public long AvailableBudgetReserve;
        public long CancelledDay = -1;
        public string CancelledByPersonId = string.Empty;
        public string CancellationReasonId = string.Empty;
        public string ReplacesGoalId = string.Empty;
        public List<string> ReplacementGoalIds = new List<string>();
        public string FulfillmentPolicyId =
            MilitaryLogisticsDelegationFulfillmentPolicyIds
                .FullReceiptRequired;
        public int ReceivedCargoQuantity;
        public int OutstandingCargoQuantity;
        public List<string> CompletedLogisticsOrderIds = new List<string>();
        public string ReplacementProcurementPolicyId =
            MilitaryLogisticsReplacementProcurementPolicyIds
                .WaitForCustodyResolution;
        public int AuthorizedReplacementQuantity;
        public int ConsumedReplacementAuthorizationQuantity;
        public long LastReplacementAuthorizedDay = -1;
        public string LastReplacementAuthorizedByPersonId = string.Empty;
        public string LastReplacementAuthorizationReasonId = string.Empty;
        public long CompensationReceived;
        public string IssuerPersonId;
        public string TargetArmyId;
        public string DestinationLocationId;
        public string ProductDefinitionId;
        public int RequestedCargoQuantity;
        public long MaximumUnitPrice;
        public long BudgetLimit;
        public string CarrierPreferenceId;
        public string CargoConsumptionPolicyId =
            MilitaryCargoConsumptionPolicyIds.Prohibited;
        public string RiskPolicyId = MilitaryLogisticsRiskPolicyIds.None;
        public string ThreatOrganizationId = string.Empty;
        public MilitaryLogisticsDelegationStatus Status;
        public string SelectedOfferId = string.Empty;
        public string LogisticsOrderId = string.Empty;
        public long CommittedCost;
    }

    [Serializable]
    public sealed class MilitaryLogisticsDelegationOfferState
    {
        public string Id;
        public long SubmittedDay;
        public long ValidUntilDay;
        public long ClosedDay = -1;
        public string GoalId;
        public string CarrierPersonId;
        public string CarrierOrganizationId;
        public string SourceCargoBatchId;
        public string SourceProvisionBatchId;
        public string TransportInventoryContainerId;
        public string OriginLocationId;
        public string RouteId;
        public string AcquisitionMethodId;
        public string LossBearerOrganizationId;
        public string LiabilityPolicyId =
            MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk;
        public int AvailableCargoQuantity;
        public int ConvoyProvisionQuantity;
        public int DailyConvoyProvisionUse = 1;
        public long UnitPrice;
        public MilitaryLogisticsDelegationOfferStatus Status;
        public string LogisticsOrderId = string.Empty;
    }

    [Serializable]
    public sealed class MilitaryLogisticsDelegationReportState
    {
        public string Id;
        public long Day;
        public string GoalId;
        public string ActorPersonId;
        public string TypeId;
        public bool IsException;
        public string RelatedOfferId = string.Empty;
        public string RelatedGoalId = string.Empty;
        public string LogisticsOrderId = string.Empty;
        public string Summary;
    }

    [Serializable]
    public sealed class MilitaryLogisticsOrderState
    {
        public string Id;
        public long CreatedDay;
        public long DeliveredDay = -1;
        public string AcquisitionMethodId;
        public string CargoConsumptionPolicyId;
        public string BuyerOrganizationId;
        public string SourceOrganizationId;
        public string CarrierOrganizationId;
        public string LossBearerOrganizationId;
        public string LiabilityPolicyId =
            MilitaryLogisticsLiabilityPolicyIds.BuyerRetainsRisk;
        public string IssuerPersonId;
        public string CarrierPersonId;
        public string TargetArmyId;
        public string CargoProductDefinitionId;
        public string SourceCargoBatchId;
        public string SourceProvisionBatchId;
        public string SourceInventoryContainerId;
        public string TransportInventoryContainerId;
        public string RouteId;
        public string JourneyId;
        public string ArmyMarchId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string FinalDestinationLocationId;
        public int CurrentLegSequence;
        public int PlannedLegCount;
        public bool AutoDeliverAtFinal = true;
        public int DispatchedCargoQuantity;
        public int RemainingCargoQuantity;
        public int DeliveredCargoQuantity;
        public int NaturalLossQuantity;
        public int HostileLossQuantity;
        public int RecoveredCargoQuantity;
        public int CargoConsumedAsProvisionsQuantity;
        public int ConvoyProvisionsLoaded;
        public int ConvoyProvisionsRemaining;
        public int ConvoyProvisionsConsumed;
        public int DailyConvoyProvisionUse;
        public int DailyNaturalLossBasisPoints;
        public long NaturalLossRemainderBasisPoints;
        public int CargoUnitWeightAtDispatch;
        public int ConvoyProvisionUnitWeightAtDispatch;
        public int CargoQualityBasisPointsAtDispatch;
        public int CargoFreshnessBasisPointsAtDispatch;
        public List<ProductQualityDimensionState>
            CargoQualityDimensionsAtDispatch =
                new List<ProductQualityDimensionState>();
        public long UnitPrice;
        public long TotalPaid;
        public int OriginPublicOrderDelta;
        public MilitaryLogisticsStatus Status;
    }

    [Serializable]
    public sealed class MilitaryLogisticsLiabilitySettlementState
    {
        public string Id;
        public long CreatedDay;
        public long LastPaymentDay = -1;
        public string GoalId;
        public string LogisticsOrderId;
        public string LiabilityPolicyId;
        public string PayerOrganizationId;
        public string PayeeOrganizationId;
        public int NaturalLossQuantity;
        public int HostileLossQuantity;
        public long UnitValue;
        public long AmountDue;
        public long AmountPaid;
        public long OutstandingAmount;
        public MilitaryLogisticsLiabilitySettlementStatus Status;
    }

    [Serializable]
    public sealed class MilitaryLogisticsLegState
    {
        public string Id;
        public string LogisticsOrderId;
        public int Sequence;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string RouteId;
        public string CarrierPersonId;
        public string CarrierOrganizationId;
        public string TransportInventoryContainerId;
        public string ProvisionBatchId;
        public int PlannedProvisionQuantity;
        public int LoadedProvisionQuantity;
        public int ConsumedProvisionQuantity;
        public int NaturalLossQuantity;
        public int HostileLossQuantity;
        public int RecoveredCargoQuantity;
        public int CargoReceivedQuantity;
        public int CargoTransferredQuantity;
        public int DailyProvisionUse;
        public string RiskPolicyId;
        public string ThreatOrganizationId;
        public string JourneyId;
        public long StartedDay = -1;
        public long CompletedDay = -1;
        public MilitaryLogisticsLegStatus Status;
    }

    [Serializable]
    public sealed class MilitaryLogisticsEscortState
    {
        public string Id;
        public string LogisticsOrderId;
        public string LogisticsLegId;
        public int LegSequence;
        public string PersonId;
        public string JourneyId;
        public int EscortPowerAtDeparture;
        public long StartedDay = -1;
        public long ArrivedDay = -1;
        public MilitaryLogisticsEscortStatus Status;
    }

    [Serializable]
    public sealed class MilitaryLogisticsIncidentState
    {
        public string Id;
        public long Day;
        public string LogisticsOrderId;
        public string LogisticsLegId;
        public string RouteId;
        public string IncidentTypeId;
        public string OutcomeId;
        public string ThreatOrganizationId;
        public int AttackChanceBasisPoints;
        public int AttackRollBasisPoints;
        public int EscortPower;
        public int ThreatPower;
        public int SeizedCargoQuantity;
        public int RecoveredCargoQuantity;
        public string Summary;
    }

    [Serializable]
    public sealed class MilitaryLogisticsInjuryState
    {
        public string PersonId;
        public string MilitaryServiceId;
        public int HealthBeforeBasisPoints;
        public int HealthAfterBasisPoints;
    }

    [Serializable]
    public sealed class MilitaryLogisticsClashState
    {
        public string Id;
        public long Day;
        public string LogisticsOrderId;
        public string LogisticsLegId;
        public string IncidentId;
        public string TypeId;
        public string OutcomeId;
        public string IssuerPersonId;
        public string DefenderOrganizationId;
        public List<string> DefenderPersonIds = new List<string>();
        public int DefenderPower;
        public int ThreatPower;
        public int CargoRecoveredQuantity;
        public List<MilitaryLogisticsInjuryState> Injuries =
            new List<MilitaryLogisticsInjuryState>();
        public string Summary;
    }

    [Serializable]
    public sealed class MilitaryLogisticsLedgerEntryState
    {
        public string Id;
        public long Day;
        public MilitaryLogisticsLedgerType Type;
        public string LogisticsOrderId;
        public string ActorPersonId;
        public int CargoDispatchedDelta;
        public int CargoRemainingDelta;
        public int CargoDeliveredDelta;
        public int CargoNaturalLossDelta;
        public int CargoHostileLossDelta;
        public int CargoRecoveredDelta;
        public int CargoConsumedAsProvisionsDelta;
        public int ConvoyProvisionsLoadedDelta;
        public int ConvoyProvisionsRemainingDelta;
        public int ConvoyProvisionsConsumedDelta;
        public int ArmyProvisionsDelta;
        public long BuyerMoneyDelta;
        public long SourceMoneyDelta;
        public int OriginPublicOrderDelta;
        public string Summary;
    }
}
