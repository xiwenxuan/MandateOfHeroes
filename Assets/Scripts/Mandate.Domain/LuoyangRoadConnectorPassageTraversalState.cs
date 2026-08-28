using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public static class LuoyangRoadConnectorPassageTraversalIds
    {
        public const string ContractId =
            "mandate.luoyang.authored-road-connectors-dynamic-passage.v1";
        public const string TaskId =
            "LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1";
        public const string StatusId =
            "LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string ModeledConnectorEdgeProfileId =
            "navigation.edge.modeled-road-gap-connector.v1";
        public const string PassageApproachEdgeProfileId =
            "navigation.edge.gate-or-bridge-two-sided-approach.v1";
        public const string EvidenceClassId =
            "historical_evidence.gameplay_reconstruction";
        public const string SpatialPrecisionId = "cell";
        public const string RouteAuthoringProfileId =
            "navigation.connector.route.minimum-obstruction-cardinal.v1";
        public const string OpenStatusId = "passage.traversal.open.v1";
        public const string ClosedStatusId = "passage.traversal.closed.v1";
        public const string DamagedStatusId = "passage.traversal.damaged.v1";
        public const string DestroyedStatusId = "passage.traversal.destroyed.v1";
        public const string InitialReasonId =
            "passage.traversal.reason.historical-initialization.v1";

        public const int ModeledConnectorCount = 28;
        public const int PassageCount = 20;
        public const int PassageApproachEdgeCount = 40;
        public const int RefinedNavigationEdgeCount = 402;
        public const bool ChangesSaveSchema = false;
        public const bool PersistsAcrossSave = false;

        public static readonly IReadOnlyList<string> StatusIds = new[]
        {
            OpenStatusId,
            ClosedStatusId,
            DamagedStatusId,
            DestroyedStatusId
        };
    }

    public static class LuoyangPassageTraversalWorldContractIds
    {
        public const string ContractId =
            "mandate.luoyang.passage-world-state-command-event.v1";
        public const string TaskId =
            "LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1";
        public const string StatusId =
            "LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string InitializationCommandTypeId =
            "mandate.command.luoyang.passage.initialize";
        public const string TransitionCommandTypeId =
            "mandate.command.luoyang.passage.transition";
        public const string InitializationIssuerId =
            "system.luoyang.historical_initialization";
        public const string PresentationBridgeIssuerId =
            "system.luoyang.map_controller";
        public const string InitializationCommandId =
            "luoyang.passage.initialization.command.184.v1";
        public const string InitializationTransactionId =
            "luoyang.passage.initialization.transaction.184.v1";
        public const string InitializationEventId =
            "luoyang.passage.initialized.event.184.v1";
        public const string InitializationTransactionKindId =
            "mandate.transaction.luoyang.passage.initialize";
        public const string TransitionTransactionKindId =
            "mandate.transaction.luoyang.passage.transition";
        public const string InitializedEventTypeId =
            "mandate.event.luoyang.passage.initialized";
        public const string TransitionedEventTypeId =
            "mandate.event.luoyang.passage.transitioned";
        public const string InitializationProjectionHandlerId =
            "mandate.handler.luoyang.passage.initialization_projection";
        public const string TransitionProjectionHandlerId =
            "mandate.handler.luoyang.passage.transition_projection";
        public const string ContractArgumentId = "contract_id";
        public const string PassageCountArgumentId = "passage_count";
        public const string FacilityIdArgumentId = "facility_id";
        public const string FacilityDefinitionIdArgumentId =
            "facility_definition_id";
        public const string ExpectedRevisionArgumentId = "expected_revision";
        public const string TargetStatusArgumentId = "target_status_id";
        public const string ReasonArgumentId = "reason_id";

        public const int WorldSchemaVersion = 74;
        public const int PassageCount = 20;
        public const bool ChangesSaveSchema = true;
        public const bool PersistsAcrossSave = true;

        public static string InitializationFacilityArgumentId(int index) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "passage_{0:D2}_facility_id", index);

        public static string InitializationDefinitionArgumentId(int index) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "passage_{0:D2}_definition_id", index);

        public static string StateId(string facilityId) =>
            "luoyang.passage.state." + new StableId(facilityId).Value;

        public static string TransitionTransactionId(
            string facilityId,
            long revision,
            string commandId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.passage.transition.transaction.{0}.revision.{1:D8}.{2}",
                new StableId(facilityId).Value,
                revision,
                new StableId(commandId).Value);

        public static string TransitionEventId(
            string facilityId,
            long revision) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.passage.transitioned.event.{0}.revision.{1:D8}",
                new StableId(facilityId).Value,
                revision);
    }

    public static class LuoyangPassageOperationsContractIds
    {
        public const string ContractId =
            "mandate.luoyang.passage-guard-damage-real-repair.v1";
        public const string TaskId =
            "LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1";
        public const string StatusId =
            "LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";

        public const string GuardAssignmentCommandTypeId =
            "mandate.command.luoyang.passage.assign_guard";
        public const string RepairStartCommandTypeId =
            "mandate.command.luoyang.passage.start_repair";
        public const string GuardAssignmentTransactionKindId =
            "mandate.transaction.luoyang.passage.assign_guard";
        public const string RepairStartTransactionKindId =
            "mandate.transaction.luoyang.passage.start_repair";
        public const string GuardAssignedEventTypeId =
            "mandate.event.luoyang.passage.guard_assigned";
        public const string RepairStartedEventTypeId =
            "mandate.event.luoyang.passage.repair_started";
        public const string GuardProjectionHandlerId =
            "mandate.handler.luoyang.passage.guard_projection";
        public const string RepairProjectionHandlerId =
            "mandate.handler.luoyang.passage.repair_projection";

        public const string LegacyTransitionCauseId =
            "passage.transition.cause.v74_compatibility";
        public const string GuardOperationCauseId =
            "passage.transition.cause.guard_operation";
        public const string BattleDamageCauseId =
            "passage.transition.cause.battle_damage";
        public const string RepairCompletionCauseId =
            "passage.transition.cause.repair_completion";
        public const string OrganizationLeaderAuthorityId =
            "passage.authority.controller_organization_leader";
        public const string GuardArmyCommanderAuthorityId =
            "passage.authority.guard_army_commander";
        public const string AttackingArmyCommanderAuthorityId =
            "passage.authority.attacking_army_commander";
        public const string RepairCompletionReasonId =
            "passage.traversal.reason.repair-completed-awaiting-guard-opening.v1";
        public const string GateRepairProfileId =
            "passage.repair.profile.han_gate_timber_iron.v1";
        public const string BridgeRepairProfileId =
            "passage.repair.profile.han_bridge_timber_iron.v1";

        public const string CauseArgumentId = "transition_cause_id";
        public const string AuthorityBasisArgumentId = "authority_basis_id";
        public const string GuardArmyIdArgumentId = "guard_army_id";
        public const string ManagerPersonIdArgumentId = "manager_person_id";
        public const string InventoryContainerIdArgumentId =
            "inventory_container_id";
        public const string InitialConditionArgumentId =
            "initial_condition_basis_points";
        public const string BattleRecordIdArgumentId = "battle_record_id";
        public const string AttackerArmyIdArgumentId = "attacker_army_id";
        public const string DamageBasisPointsArgumentId =
            "damage_basis_points";
        public const string RepairOrderIdArgumentId = "repair_order_id";
        public const string ExpectedIntegrityRevisionArgumentId =
            "expected_integrity_revision";

        public const int WorldSchemaVersion = 75;
        public const int GateRequiredTimberUnits = 8;
        public const int GateRequiredIronUnits = 2;
        public const int GateRequiredLaborMinutes = 960;
        public const int GateMinimumDays = 2;
        public const int BridgeRequiredTimberUnits = 12;
        public const int BridgeRequiredIronUnits = 2;
        public const int BridgeRequiredLaborMinutes = 1_440;
        public const int BridgeMinimumDays = 3;
        public const int RequiredMoney = 100;

        public static string ControlId(string facilityId) =>
            "luoyang.passage.control." + new StableId(facilityId).Value;

        public static string GuardCommandId(string facilityId) =>
            "luoyang.passage.guard.command." + new StableId(facilityId).Value;

        public static string GuardTransactionId(string facilityId) =>
            "luoyang.passage.guard.transaction." +
            new StableId(facilityId).Value;

        public static string GuardEventId(string facilityId) =>
            "luoyang.passage.guard-assigned.event." +
            new StableId(facilityId).Value;

        public static string DamageRecordId(string facilityId, long revision) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.passage.damage.{0}.integrity.{1:D8}",
                new StableId(facilityId).Value, revision);

        public static string RepairOrderId(string facilityId, long revision) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.passage.repair.{0}.integrity.{1:D8}",
                new StableId(facilityId).Value, revision);

        public static string RepairStartCommandId(
            string facilityId,
            long integrityRevision) => RepairOrderId(
                facilityId, integrityRevision) + ".start.command";

        public static string RepairStartTransactionId(
            string repairOrderId) =>
            new StableId(repairOrderId).Value + ".start.transaction";

        public static string RepairStartEventId(string repairOrderId) =>
            new StableId(repairOrderId).Value + ".started.event";
    }

    public static class LuoyangPassagePedestrianPresentationIds
    {
        public const string ContractId =
            "mandate.luoyang.passage-stateful-presentation-pedestrian-blocking.v1";
        public const string TaskId =
            "LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1";
        public const string StatusId =
            "LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string OpenVisualStateId =
            "passage.presentation.open.v1";
        public const string ClosedVisualStateId =
            "passage.presentation.closed.v1";
        public const string DamagedVisualStateId =
            "passage.presentation.damaged.v1";
        public const string DestroyedVisualStateId =
            "passage.presentation.destroyed.v1";
        public const string RepairingVisualStateId =
            "passage.presentation.repairing.v1";

        public const int PassageCount = 20;
        public const bool ChangesSaveSchema = false;
        public const bool PersistsAcrossSave = false;
    }

    public sealed class LuoyangPassagePedestrianState
    {
        public string FacilityId { get; internal set; }
        public string FacilityDefinitionId { get; internal set; }
        public string TraversalStatusId { get; internal set; }
        public string VisualStateId { get; internal set; }
        public bool BlocksPedestrianTraversal { get; internal set; }
        public bool IsRepairing { get; internal set; }
        public bool IsBridge { get; internal set; }
        public int ConditionBasisPoints { get; internal set; }
        public long PassageRevision { get; internal set; }
        public long IntegrityRevision { get; internal set; }
    }

    public sealed class LuoyangPassagePedestrianPresentationPlan
    {
        private readonly IReadOnlyDictionary<string,
            LuoyangPassagePedestrianState> _statesByFacilityId;

        internal LuoyangPassagePedestrianPresentationPlan(
            IReadOnlyList<LuoyangPassagePedestrianState> states,
            bool isWorldStateProjection)
        {
            States = states ?? throw new ArgumentNullException(nameof(states));
            IsWorldStateProjection = isWorldStateProjection;
            _statesByFacilityId = states.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
        }

        public string ContractId =>
            LuoyangPassagePedestrianPresentationIds.ContractId;
        public string TaskId => LuoyangPassagePedestrianPresentationIds.TaskId;
        public string StatusId =>
            LuoyangPassagePedestrianPresentationIds.StatusId;
        public bool ChangesSaveSchema =>
            LuoyangPassagePedestrianPresentationIds.ChangesSaveSchema;
        public bool PersistsAcrossSave =>
            LuoyangPassagePedestrianPresentationIds.PersistsAcrossSave;
        public bool IsWorldStateProjection { get; }
        public IReadOnlyList<LuoyangPassagePedestrianState> States { get; }

        public LuoyangPassagePedestrianState Get(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId) ||
                !_statesByFacilityId.TryGetValue(facilityId, out var state))
                throw new KeyNotFoundException(
                    "Unknown Luoyang pedestrian-passage Facility ID: " +
                    facilityId);
            return state;
        }
    }

    public static class LuoyangPassagePedestrianPresentationRules
    {
        private const string BridgeDefinitionId = "facility.public.bridge";

        public static LuoyangPassagePedestrianPresentationPlan CreatePlan(
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession,
            WorldState world = null)
        {
            if (refinementPlan == null)
                throw new ArgumentNullException(nameof(refinementPlan));
            if (passageSession == null)
                throw new ArgumentNullException(nameof(passageSession));
            LuoyangRoadConnectorPassageTraversalRules.Validate(refinementPlan);
            if (world != null)
            {
                if (!passageSession.IsWorldStateProjection)
                    throw new InvalidOperationException(
                        "A persisted Luoyang pedestrian projection requires " +
                        "the read-only WorldState passage session.");
                LuoyangPassageTraversalWorldRules.ValidateWorld(world);
                LuoyangPassageOperationalRules.ValidateWorld(world);
            }

            var controls = world == null
                ? new Dictionary<string,
                    LuoyangPassageOperationalControlState>(StringComparer.Ordinal)
                : world.LuoyangPassageOperationalControls.ToDictionary(
                    item => item.FacilityId, StringComparer.Ordinal);
            var activeRepairFacilityIds = world == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(world.LuoyangPassageRepairOrders.Where(
                        item => item.Status ==
                            LuoyangPassageRepairStatus.InProgress)
                    .Select(item => item.FacilityId), StringComparer.Ordinal);
            var states = passageSession.Records.OrderBy(
                    item => item.FacilityId, StringComparer.Ordinal)
                .Select(record =>
                {
                    controls.TryGetValue(record.FacilityId, out var control);
                    var isRepairing = activeRepairFacilityIds.Contains(
                        record.FacilityId);
                    return new LuoyangPassagePedestrianState
                    {
                        FacilityId = record.FacilityId,
                        FacilityDefinitionId = record.FacilityDefinitionId,
                        TraversalStatusId = record.TraversalStatusId,
                        VisualStateId = isRepairing
                            ? LuoyangPassagePedestrianPresentationIds
                                .RepairingVisualStateId
                            : ResolveVisualStateId(record.TraversalStatusId),
                        BlocksPedestrianTraversal = !record.CanTraverse,
                        IsRepairing = isRepairing,
                        IsBridge = string.Equals(record.FacilityDefinitionId,
                            BridgeDefinitionId, StringComparison.Ordinal),
                        ConditionBasisPoints = control == null
                            ? InferConditionBasisPoints(
                                record.TraversalStatusId)
                            : control.CurrentConditionBasisPoints,
                        PassageRevision = record.Revision,
                        IntegrityRevision = control?.IntegrityRevision ?? 0
                    };
                }).ToArray();
            var plan = new LuoyangPassagePedestrianPresentationPlan(states,
                world != null);
            Validate(plan, refinementPlan, passageSession);
            return plan;
        }

        public static void Validate(
            LuoyangPassagePedestrianPresentationPlan plan,
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (refinementPlan == null)
                throw new ArgumentNullException(nameof(refinementPlan));
            if (passageSession == null)
                throw new ArgumentNullException(nameof(passageSession));
            if (plan.ChangesSaveSchema || plan.PersistsAcrossSave ||
                plan.States.Count !=
                    LuoyangPassagePedestrianPresentationIds.PassageCount ||
                plan.States.Select(item => item.FacilityId).Distinct(
                    StringComparer.Ordinal).Count() != plan.States.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang pedestrian-passage projection totals.");
            var passageIds = new HashSet<string>(
                refinementPlan.PassageFacilityIds, StringComparer.Ordinal);
            foreach (var state in plan.States)
            {
                if (state == null || !passageIds.Contains(state.FacilityId) ||
                    state.ConditionBasisPoints < 0 ||
                    state.ConditionBasisPoints > 10_000 ||
                    state.PassageRevision < 0 || state.IntegrityRevision < 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang pedestrian-passage projection state.");
                var record = passageSession.Get(state.FacilityId);
                if (!string.Equals(state.FacilityDefinitionId,
                        record.FacilityDefinitionId, StringComparison.Ordinal) ||
                    !string.Equals(state.TraversalStatusId,
                        record.TraversalStatusId, StringComparison.Ordinal) ||
                    state.BlocksPedestrianTraversal == record.CanTraverse ||
                    state.PassageRevision != record.Revision ||
                    state.IsRepairing != string.Equals(state.VisualStateId,
                        LuoyangPassagePedestrianPresentationIds
                            .RepairingVisualStateId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Luoyang pedestrian-passage projection drifted from " +
                        "the authoritative traversal session.");
            }
        }

        private static string ResolveVisualStateId(string traversalStatusId)
        {
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                    StringComparison.Ordinal))
                return LuoyangPassagePedestrianPresentationIds
                    .OpenVisualStateId;
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                    StringComparison.Ordinal))
                return LuoyangPassagePedestrianPresentationIds
                    .ClosedVisualStateId;
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                    StringComparison.Ordinal))
                return LuoyangPassagePedestrianPresentationIds
                    .DamagedVisualStateId;
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                    StringComparison.Ordinal))
                return LuoyangPassagePedestrianPresentationIds
                    .DestroyedVisualStateId;
            throw new InvalidOperationException(
                "Unknown Luoyang passage traversal status projection: " +
                traversalStatusId);
        }

        private static int InferConditionBasisPoints(string traversalStatusId)
        {
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                    StringComparison.Ordinal)) return 5_000;
            if (string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                    StringComparison.Ordinal)) return 0;
            return 10_000;
        }
    }

    public static class LuoyangClickToWalkPedestrianIds
    {
        public const string ContractId =
            "presentation.luoyang.click-to-walk-pedestrian.v1";
        public const string TaskId =
            "LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1";
        public const string StatusId =
            "LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW";
        public const string PreviewActorId =
            "presentation-person.luoyang.walk-review.v1";
        public const string RoadWidthProfileId =
            "pedestrian.corridor.luoyang-road-18m.v1";
        public const string ModeledConnectorWidthProfileId =
            "pedestrian.corridor.luoyang-modeled-connector-12m.v1";
        public const string GateWidthProfileId =
            "pedestrian.corridor.luoyang-gate-12m.v1";
        public const string BridgeWidthProfileId =
            "pedestrian.corridor.luoyang-bridge-8m.v1";
        public const string ReadyStateId = "pedestrian.walk.ready.v1";
        public const string WalkingStateId = "pedestrian.walk.walking.v1";
        public const string ArrivedStateId = "pedestrian.walk.arrived.v1";
        public const string BlockedStateId = "pedestrian.walk.blocked.v1";
        public const string CancelledStateId = "pedestrian.walk.cancelled.v1";
        public const string UnknownNodeReasonId =
            "pedestrian.walk.failure.unknown-node.v1";
        public const string BlockedPassageReasonId =
            "pedestrian.walk.failure.blocked-passage.v1";
        public const string NoRouteReasonId =
            "pedestrian.walk.failure.no-route.v1";
        public const string OutsideResidentWindowReasonId =
            "pedestrian.walk.failure.outside-resident-window.v1";
        public const string DynamicBlockerReasonId =
            "pedestrian.walk.failure.dynamic-blocker.v1";

        public const float RoadWidthMetres = 18f;
        public const float ModeledConnectorWidthMetres = 12f;
        public const float GateWidthMetres = 12f;
        public const float BridgeWidthMetres = 8f;
        public const float PersonClearanceRadiusMetres = 0.45f;
        public const float WalkingSpeedMetresPerSecond = 1.35f;
        public const bool ChangesSaveSchema = false;
        public const bool PersistsAcrossSave = false;
        public const bool CreatesPermanentPerson = false;
    }

    public sealed class LuoyangPedestrianWalkSegment
    {
        public int Sequence { get; internal set; }
        public string EdgeId { get; internal set; }
        public string FromFacilityId { get; internal set; }
        public string ToFacilityId { get; internal set; }
        public string WidthProfileId { get; internal set; }
        public float WidthMetres { get; internal set; }
        public float DistanceMetres { get; internal set; }
        public float WeightedDistanceMetres { get; internal set; }
        public float LateralOffsetMetres { get; internal set; }
        public bool UsesModeledConnector { get; internal set; }
        public bool UsesPassage { get; internal set; }
        public bool UsesDamagedPassage { get; internal set; }
    }

    public sealed class LuoyangPedestrianWalkPlan
    {
        internal LuoyangPedestrianWalkPlan(string actorId,
            string startFacilityId, string targetFacilityId,
            IReadOnlyList<string> facilityIds,
            IReadOnlyList<LuoyangPedestrianWalkSegment> segments,
            string failureReasonId)
        {
            ActorId = actorId;
            StartFacilityId = startFacilityId;
            TargetFacilityId = targetFacilityId;
            FacilityIds = facilityIds ?? throw new ArgumentNullException(
                nameof(facilityIds));
            Segments = segments ?? throw new ArgumentNullException(
                nameof(segments));
            FailureReasonId = failureReasonId ?? string.Empty;
            TotalDistanceMetres = segments.Sum(item => item.DistanceMetres);
            WeightedDistanceMetres = segments.Sum(item =>
                item.WeightedDistanceMetres);
            EstimatedDurationSeconds = WeightedDistanceMetres /
                LuoyangClickToWalkPedestrianIds.WalkingSpeedMetresPerSecond;
        }

        public string ContractId => LuoyangClickToWalkPedestrianIds.ContractId;
        public string TaskId => LuoyangClickToWalkPedestrianIds.TaskId;
        public string StatusId => LuoyangClickToWalkPedestrianIds.StatusId;
        public bool ChangesSaveSchema =>
            LuoyangClickToWalkPedestrianIds.ChangesSaveSchema;
        public bool PersistsAcrossSave =>
            LuoyangClickToWalkPedestrianIds.PersistsAcrossSave;
        public bool CreatesPermanentPerson =>
            LuoyangClickToWalkPedestrianIds.CreatesPermanentPerson;
        public string ActorId { get; }
        public string StartFacilityId { get; }
        public string TargetFacilityId { get; }
        public IReadOnlyList<string> FacilityIds { get; }
        public IReadOnlyList<LuoyangPedestrianWalkSegment> Segments { get; }
        public string FailureReasonId { get; }
        public bool CanWalk => FacilityIds.Count > 0 &&
                               string.IsNullOrEmpty(FailureReasonId);
        public float TotalDistanceMetres { get; }
        public float WeightedDistanceMetres { get; }
        public float EstimatedDurationSeconds { get; }
        public bool UsesModeledConnector => Segments.Any(item =>
            item.UsesModeledConnector);
        public bool UsesPassage => Segments.Any(item => item.UsesPassage);
        public bool UsesDamagedPassage => Segments.Any(item =>
            item.UsesDamagedPassage);
    }

    public static class LuoyangClickToWalkPedestrianRules
    {
        private const string RoadDefinitionId = "facility.public.road";
        private const string BridgeDefinitionId = "facility.public.bridge";

        public static LuoyangPedestrianWalkPlan CreatePlan(
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession,
            string actorId, string startFacilityId, string targetFacilityId)
        {
            return CreatePlan(refinementPlan, passageSession, actorId,
                startFacilityId, targetFacilityId, null);
        }

        public static LuoyangPedestrianWalkPlan CreatePlan(
            LuoyangRoadTraversalRefinementPlan refinementPlan,
            LuoyangPassageTraversalSession passageSession,
            string actorId, string startFacilityId, string targetFacilityId,
            Func<string, bool> canTraverseEdge)
        {
            if (refinementPlan == null)
                throw new ArgumentNullException(nameof(refinementPlan));
            if (passageSession == null)
                throw new ArgumentNullException(nameof(passageSession));
            actorId = new StableId(actorId).Value;
            if (string.IsNullOrWhiteSpace(startFacilityId) ||
                string.IsNullOrWhiteSpace(targetFacilityId) ||
                !refinementPlan.NavigationNodesByFacilityId.ContainsKey(
                    startFacilityId) ||
                !refinementPlan.NavigationNodesByFacilityId.ContainsKey(
                    targetFacilityId))
                return Failed(actorId, startFacilityId, targetFacilityId,
                    LuoyangClickToWalkPedestrianIds.UnknownNodeReasonId);

            var facilityPath = LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(refinementPlan, passageSession,
                    startFacilityId, targetFacilityId, canTraverseEdge);
            if (facilityPath.Count == 0)
            {
                var blocked = IsBlockedPassage(passageSession,
                                  startFacilityId) ||
                              IsBlockedPassage(passageSession,
                                  targetFacilityId);
                return Failed(actorId, startFacilityId, targetFacilityId,
                    blocked
                        ? LuoyangClickToWalkPedestrianIds
                            .BlockedPassageReasonId
                        : LuoyangClickToWalkPedestrianIds.NoRouteReasonId);
            }

            var nodesByFacility = refinementPlan.NavigationNodesByFacilityId;
            var segments = new List<LuoyangPedestrianWalkSegment>(
                Math.Max(0, facilityPath.Count - 1));
            var laneSign = StableLaneSign(actorId);
            for (var index = 1; index < facilityPath.Count; index++)
            {
                var from = nodesByFacility[facilityPath[index - 1]];
                var to = nodesByFacility[facilityPath[index]];
                var edge = refinementPlan.NavigationEdges.Single(item =>
                    item.FromNodeId == from.NodeId &&
                    item.ToNodeId == to.NodeId ||
                    item.FromNodeId == to.NodeId &&
                    item.ToNodeId == from.NodeId);
                ResolveWidth(edge, from, to, out var profileId,
                    out var widthMetres, out var usesPassage);
                var damaged = usesPassage &&
                              (IsDamagedPassage(passageSession,
                                   from.FacilityId) ||
                               IsDamagedPassage(passageSession,
                                   to.FacilityId));
                var multiplier = passageSession.TryGet(to.FacilityId,
                    out var destinationPassage)
                    ? destinationPassage.TraversalCostPermille / 1000f
                    : 1f;
                segments.Add(new LuoyangPedestrianWalkSegment
                {
                    Sequence = segments.Count,
                    EdgeId = edge.EdgeId,
                    FromFacilityId = from.FacilityId,
                    ToFacilityId = to.FacilityId,
                    WidthProfileId = profileId,
                    WidthMetres = widthMetres,
                    DistanceMetres = edge.TraversalCostMetres,
                    WeightedDistanceMetres = edge.TraversalCostMetres *
                                             multiplier,
                    LateralOffsetMetres = laneSign * Math.Min(1.2f,
                        widthMetres * 0.18f),
                    UsesModeledConnector = string.Equals(edge.EdgeProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .ModeledConnectorEdgeProfileId,
                        StringComparison.Ordinal),
                    UsesPassage = usesPassage,
                    UsesDamagedPassage = damaged
                });
            }

            var plan = new LuoyangPedestrianWalkPlan(actorId,
                startFacilityId, targetFacilityId, facilityPath.ToArray(),
                segments.ToArray(), string.Empty);
            Validate(plan);
            return plan;
        }

        public static void Validate(LuoyangPedestrianWalkPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.ChangesSaveSchema || plan.PersistsAcrossSave ||
                plan.CreatesPermanentPerson ||
                string.IsNullOrWhiteSpace(plan.ActorId))
                throw new InvalidOperationException(
                    "Invalid Luoyang click-to-walk persistence boundary.");
            if (!plan.CanWalk)
            {
                if (plan.FacilityIds.Count != 0 || plan.Segments.Count != 0 ||
                    string.IsNullOrWhiteSpace(plan.FailureReasonId))
                    throw new InvalidOperationException(
                        "Invalid failed Luoyang pedestrian walk plan.");
                return;
            }
            if (plan.FacilityIds.Count != plan.Segments.Count + 1 ||
                !string.Equals(plan.FacilityIds[0], plan.StartFacilityId,
                    StringComparison.Ordinal) ||
                !string.Equals(plan.FacilityIds[plan.FacilityIds.Count - 1],
                    plan.TargetFacilityId, StringComparison.Ordinal) ||
                plan.Segments.Any(item => item == null ||
                    item.Sequence < 0 ||
                    string.IsNullOrWhiteSpace(item.EdgeId) ||
                    string.IsNullOrWhiteSpace(item.WidthProfileId) ||
                    item.WidthMetres <=
                        LuoyangClickToWalkPedestrianIds
                            .PersonClearanceRadiusMetres * 2f ||
                    item.DistanceMetres <= 0f ||
                    item.WeightedDistanceMetres <= 0f ||
                    Math.Abs(item.LateralOffsetMetres) +
                        LuoyangClickToWalkPedestrianIds
                            .PersonClearanceRadiusMetres >=
                        item.WidthMetres * 0.5f) ||
                plan.Segments.Select(item => item.Sequence).Distinct().Count() !=
                    plan.Segments.Count || plan.TotalDistanceMetres < 0f ||
                plan.WeightedDistanceMetres < plan.TotalDistanceMetres ||
                plan.EstimatedDurationSeconds < 0f)
                throw new InvalidOperationException(
                    "Invalid Luoyang pedestrian walk route geometry.");
        }

        private static LuoyangPedestrianWalkPlan Failed(string actorId,
            string startFacilityId, string targetFacilityId, string reasonId)
        {
            var plan = new LuoyangPedestrianWalkPlan(actorId,
                startFacilityId ?? string.Empty, targetFacilityId ?? string.Empty,
                Array.Empty<string>(),
                Array.Empty<LuoyangPedestrianWalkSegment>(), reasonId);
            Validate(plan);
            return plan;
        }

        private static void ResolveWidth(LuoyangRoadNavigationEdge edge,
            LuoyangRoadNavigationNode from, LuoyangRoadNavigationNode to,
            out string profileId, out float widthMetres,
            out bool usesPassage)
        {
            var passage = !string.Equals(from.FacilityDefinitionId,
                    RoadDefinitionId, StringComparison.Ordinal)
                ? from
                : !string.Equals(to.FacilityDefinitionId, RoadDefinitionId,
                    StringComparison.Ordinal)
                    ? to
                    : null;
            usesPassage = passage != null;
            if (passage != null && string.Equals(
                    passage.FacilityDefinitionId, BridgeDefinitionId,
                    StringComparison.Ordinal))
            {
                profileId = LuoyangClickToWalkPedestrianIds
                    .BridgeWidthProfileId;
                widthMetres = LuoyangClickToWalkPedestrianIds
                    .BridgeWidthMetres;
                return;
            }
            if (passage != null)
            {
                profileId = LuoyangClickToWalkPedestrianIds.GateWidthProfileId;
                widthMetres = LuoyangClickToWalkPedestrianIds.GateWidthMetres;
                return;
            }
            if (string.Equals(edge.EdgeProfileId,
                    LuoyangRoadConnectorPassageTraversalIds
                        .ModeledConnectorEdgeProfileId,
                    StringComparison.Ordinal))
            {
                profileId = LuoyangClickToWalkPedestrianIds
                    .ModeledConnectorWidthProfileId;
                widthMetres = LuoyangClickToWalkPedestrianIds
                    .ModeledConnectorWidthMetres;
                return;
            }
            profileId = LuoyangClickToWalkPedestrianIds.RoadWidthProfileId;
            widthMetres = LuoyangClickToWalkPedestrianIds.RoadWidthMetres;
        }

        private static bool IsBlockedPassage(
            LuoyangPassageTraversalSession session, string facilityId) =>
            session.TryGet(facilityId, out var passage) &&
            !passage.CanTraverse;

        private static bool IsDamagedPassage(
            LuoyangPassageTraversalSession session, string facilityId) =>
            session.TryGet(facilityId, out var passage) && string.Equals(
                passage.TraversalStatusId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                StringComparison.Ordinal);

        private static float StableLaneSign(string actorId)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in actorId)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (hash & 1) == 0 ? -1f : 1f;
            }
        }
    }

    public static class LuoyangFormalPlayerMovementIds
    {
        public const string ContractId =
            "mandate.luoyang.formal-player-movement-world-settlement.v1";
        public const string PolicyId =
            "movement.policy.luoyang-pedestrian-world-settlement.v1";
        public const string InitializeCommandTypeId =
            "mandate.command.luoyang-player-movement.initialize.v1";
        public const string MoveCommandTypeId =
            "mandate.command.luoyang-player-movement.request.v1";
        public const string AdvanceSegmentCommandTypeId =
            "mandate.command.luoyang-player-movement.advance-segment.v1";
        public const string RoadTransitionCommandTypeId =
            "mandate.command.luoyang-road-segment.transition.v1";
        public const string SystemIssuerId =
            "mandate.issuer.luoyang-player-movement-system.v1";
        public const string PresentationIssuerId =
            "mandate.issuer.luoyang-player-input.v1";
        public const string InitializedEventTypeId =
            "mandate.event.luoyang-player-movement.initialized.v1";
        public const string MovementStartedEventTypeId =
            "mandate.event.person-movement.started.v1";
        public const string MovementProgressedEventTypeId =
            "mandate.event.person-movement.progressed.v1";
        public const string MovementCompletedEventTypeId =
            "mandate.event.person-movement.completed.v1";
        public const string LocationChangedEventTypeId =
            "mandate.event.person-location.changed.v1";
        public const string MovementInterruptedEventTypeId =
            "mandate.event.person-movement.interrupted.v1";
        public const string RouteInvalidatedEventTypeId =
            "mandate.event.person-movement.route-invalidated.v1";
        public const string RoadTransitionedEventTypeId =
            "mandate.event.luoyang-road-segment.transitioned.v1";
        public const string OpenRoadStatusId =
            "road.segment.status.open.v1";
        public const string BlockedRoadStatusId =
            "road.segment.status.blocked.v1";
        public const string DestroyedRoadStatusId =
            "road.segment.status.destroyed.v1";
        public const string FootMovementModeId = "movement.mode.foot.v1";
        public const string InvalidRouteReasonId =
            "movement.interruption.route-invalidated.v1";
        public const string InsufficientStaminaReasonId =
            "movement.rejection.insufficient-stamina.v1";
        public const string InsufficientFoodReasonId =
            "movement.rejection.insufficient-food.v1";
        public const int WorldSegmentMinutes = 360;
    }

    public enum LuoyangFormalMovementStatus : byte
    {
        Active,
        Completed,
        Interrupted
    }

    [Serializable]
    public sealed class LuoyangLocalNavigationLocationState
    {
        public string Id;
        public string FacilityId;
        public string FacilityDefinitionId;
        public string SettlementLocationId;
        public ulong CellId64;
        public int GridColumn;
        public int GridRow;
    }

    [Serializable]
    public sealed class LuoyangRoadOperationalSegmentState
    {
        public string Id;
        public string EdgeId;
        public string FromFacilityId;
        public string ToFacilityId;
        public string StatusId;
        public long Revision;
        public long LastChangedDay;
        public byte LastChangedSegment;
        public string LastReasonId;
        public string LastCommandId;
        public string LastEventId;

        public bool CanTraverse => string.Equals(StatusId,
            LuoyangFormalPlayerMovementIds.OpenRoadStatusId,
            StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class LuoyangFormalMovementSegmentState
    {
        public int Sequence;
        public string EdgeId;
        public string FromFacilityId;
        public string ToFacilityId;
        public string PassageFacilityId;
        public int DistanceMetres;
        public int WeightedDistanceMetres;
        public int DurationMinutes;
        public int StaminaCostBasisPoints;
        public int FoodCost;
    }

    [Serializable]
    public sealed class LuoyangFormalPlayerMovementState
    {
        public string Id;
        public string RequestCommandId;
        public string PersonId;
        public string PolicyId;
        public string MovementModeId;
        public string OriginSettlementLocationId;
        public string OriginFacilityId;
        public ulong OriginCellId64;
        public string TargetFacilityId;
        public ulong TargetCellId64;
        public long IssuedDay;
        public byte IssuedSegment;
        public int ExpectedDurationMinutes;
        public int ExpectedWorldSegments;
        public int ExpectedStaminaCostBasisPoints;
        public int ExpectedFoodCost;
        public int CurrentSegmentIndex;
        public int ElapsedDurationMinutes;
        public int UnsettledDurationMinutes;
        public int ConsumedStaminaBasisPoints;
        public int ConsumedFood;
        public LuoyangFormalMovementStatus Status;
        public string FailureReasonId;
        public long CompletedDay = -1;
        public byte CompletedSegment;
        public string LastProgressCommandId;
        public string StartedEventId;
        public string CompletionEventId;
        public List<LuoyangFormalMovementSegmentState> Segments =
            new List<LuoyangFormalMovementSegmentState>();
    }

    public sealed class LuoyangMovementCostPolicy
    {
        public LuoyangMovementCostPolicy(
            int walkingMetresPerMinute = 80,
            int metresPerStaminaBasisPoint = 20,
            int feedingIntervalMinutes = 360)
        {
            if (walkingMetresPerMinute <= 0 ||
                metresPerStaminaBasisPoint <= 0 ||
                feedingIntervalMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(
                    walkingMetresPerMinute));
            WalkingMetresPerMinute = walkingMetresPerMinute;
            MetresPerStaminaBasisPoint = metresPerStaminaBasisPoint;
            FeedingIntervalMinutes = feedingIntervalMinutes;
        }

        public string Id => LuoyangFormalPlayerMovementIds.PolicyId;
        public int WalkingMetresPerMinute { get; }
        public int MetresPerStaminaBasisPoint { get; }
        public int FeedingIntervalMinutes { get; }
    }

    public sealed class LuoyangMovementSegmentCost
    {
        public int DistanceMetres { get; internal set; }
        public int WeightedDistanceMetres { get; internal set; }
        public int DurationMinutes { get; internal set; }
        public int StaminaCostBasisPoints { get; internal set; }
    }

    public sealed class LuoyangMovementCostCalculator
    {
        private readonly LuoyangMovementCostPolicy _policy;

        public LuoyangMovementCostCalculator(
            LuoyangMovementCostPolicy policy = null)
        {
            _policy = policy ?? new LuoyangMovementCostPolicy();
        }

        public LuoyangMovementCostPolicy Policy => _policy;

        public LuoyangMovementSegmentCost CalculateSegment(
            double distanceMetres,
            double weightedDistanceMetres,
            int loadBasisPoints = 0)
        {
            if (distanceMetres <= 0d || weightedDistanceMetres <= 0d ||
                weightedDistanceMetres + 0.0001d < distanceMetres ||
                loadBasisPoints < 0 || loadBasisPoints > 10_000)
                throw new ArgumentOutOfRangeException(nameof(distanceMetres));
            var distance = Math.Max(1, checked((int)Math.Ceiling(
                distanceMetres)));
            var weighted = Math.Max(distance, checked((int)Math.Ceiling(
                weightedDistanceMetres)));
            var loadAdjusted = checked(weighted +
                weighted * loadBasisPoints / 20_000);
            return new LuoyangMovementSegmentCost
            {
                DistanceMetres = distance,
                WeightedDistanceMetres = loadAdjusted,
                DurationMinutes = CeilingDivide(loadAdjusted,
                    _policy.WalkingMetresPerMinute),
                StaminaCostBasisPoints = CeilingDivide(loadAdjusted,
                    _policy.MetresPerStaminaBasisPoint)
            };
        }

        public int CalculateFoodCost(int durationMinutes)
        {
            if (durationMinutes < 0)
                throw new ArgumentOutOfRangeException(nameof(durationMinutes));
            return durationMinutes / _policy.FeedingIntervalMinutes;
        }

        public int CalculateWorldSegments(int durationMinutes)
        {
            if (durationMinutes < 0)
                throw new ArgumentOutOfRangeException(nameof(durationMinutes));
            return durationMinutes == 0 ? 0 : CeilingDivide(durationMinutes,
                LuoyangFormalPlayerMovementIds.WorldSegmentMinutes);
        }

        private static int CeilingDivide(int value, int divisor) => checked(
            (value + divisor - 1) / divisor);
    }

    public static class LuoyangFormalPlayerMovementRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.LuoyangLocalNavigationLocations == null ||
                world.LuoyangRoadOperationalSegments == null ||
                world.LuoyangFormalPlayerMovements == null)
                throw new InvalidOperationException(
                    "Luoyang formal movement collections cannot be null.");

            var locations = new Dictionary<string,
                LuoyangLocalNavigationLocationState>(StringComparer.Ordinal);
            foreach (var location in world.LuoyangLocalNavigationLocations)
            {
                if (location == null)
                    throw new InvalidOperationException(
                        "A Luoyang local navigation location cannot be null.");
                _ = new StableId(location.Id);
                _ = new StableId(location.FacilityId);
                _ = new StableId(location.FacilityDefinitionId);
                _ = new StableId(location.SettlementLocationId);
                if (location.CellId64 == 0 ||
                    !locations.TryAdd(location.FacilityId, location))
                    throw new InvalidOperationException(
                        "Invalid or duplicate Luoyang local location.");
            }

            var roads = new Dictionary<string,
                LuoyangRoadOperationalSegmentState>(StringComparer.Ordinal);
            foreach (var road in world.LuoyangRoadOperationalSegments)
            {
                if (road == null || !IsRoadStatus(road.StatusId) ||
                    road.Revision < 0 || road.LastChangedDay < 0 ||
                    road.LastChangedDay > world.AbsoluteDay ||
                    !locations.ContainsKey(road.FromFacilityId) ||
                    !locations.ContainsKey(road.ToFacilityId))
                    throw new InvalidOperationException(
                        "Invalid Luoyang road operational segment.");
                _ = new StableId(road.Id);
                _ = new StableId(road.EdgeId);
                if (!roads.TryAdd(road.EdgeId, road))
                    throw new InvalidOperationException(
                        "Duplicate Luoyang road operational edge.");
            }

            foreach (var person in world.People)
            {
                if (person == null || string.IsNullOrEmpty(
                        person.CurrentFacilityId)) continue;
                if (!locations.TryGetValue(person.CurrentFacilityId,
                        out var local) || local.CellId64 !=
                    person.CurrentCellId64 || !string.Equals(
                        local.SettlementLocationId, person.LocationId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Person {person.Id} has an invalid local location.");
            }

            var activePeople = new HashSet<string>(StringComparer.Ordinal);
            foreach (var movement in world.LuoyangFormalPlayerMovements)
            {
                ValidateMovement(world, movement, locations, roads);
                if (movement.Status == LuoyangFormalMovementStatus.Active &&
                    !activePeople.Add(movement.PersonId))
                    throw new InvalidOperationException(
                        "A Person cannot have two active Luoyang movements.");
            }
        }

        public static bool IsRoadStatus(string statusId) => string.Equals(
                statusId, LuoyangFormalPlayerMovementIds.OpenRoadStatusId,
                StringComparison.Ordinal) || string.Equals(statusId,
                LuoyangFormalPlayerMovementIds.BlockedRoadStatusId,
                StringComparison.Ordinal) || string.Equals(statusId,
                LuoyangFormalPlayerMovementIds.DestroyedRoadStatusId,
                StringComparison.Ordinal);

        private static void ValidateMovement(WorldState world,
            LuoyangFormalPlayerMovementState movement,
            IReadOnlyDictionary<string, LuoyangLocalNavigationLocationState>
                locations,
            IReadOnlyDictionary<string, LuoyangRoadOperationalSegmentState>
                roads)
        {
            if (movement == null || !Enum.IsDefined(
                    typeof(LuoyangFormalMovementStatus), movement.Status) ||
                !string.Equals(movement.PolicyId,
                    LuoyangFormalPlayerMovementIds.PolicyId,
                    StringComparison.Ordinal) ||
                !string.Equals(movement.MovementModeId,
                    LuoyangFormalPlayerMovementIds.FootMovementModeId,
                    StringComparison.Ordinal) || movement.Segments == null ||
                movement.Segments.Count == 0 ||
                movement.CurrentSegmentIndex < 0 ||
                movement.CurrentSegmentIndex > movement.Segments.Count ||
                movement.ExpectedDurationMinutes <= 0 ||
                movement.ExpectedWorldSegments <= 0 ||
                movement.ExpectedStaminaCostBasisPoints <= 0 ||
                movement.ExpectedFoodCost < 0 ||
                movement.ElapsedDurationMinutes < 0 ||
                movement.UnsettledDurationMinutes < 0 ||
                movement.UnsettledDurationMinutes >=
                    LuoyangFormalPlayerMovementIds.WorldSegmentMinutes ||
                movement.ConsumedStaminaBasisPoints < 0 ||
                movement.ConsumedFood < 0 ||
                !locations.ContainsKey(movement.OriginFacilityId) ||
                !locations.ContainsKey(movement.TargetFacilityId) ||
                !world.People.Any(item => item != null && string.Equals(
                    item.Id, movement.PersonId, StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Invalid Luoyang formal player movement.");
            _ = new StableId(movement.Id);
            _ = new StableId(movement.RequestCommandId);
            _ = new StableId(movement.PersonId);
            var duration = 0;
            var stamina = 0;
            var food = 0;
            var expectedFrom = movement.OriginFacilityId;
            for (var i = 0; i < movement.Segments.Count; i++)
            {
                var segment = movement.Segments[i];
                if (segment == null || segment.Sequence != i ||
                    !roads.ContainsKey(segment.EdgeId) ||
                    !string.Equals(segment.FromFacilityId, expectedFrom,
                        StringComparison.Ordinal) ||
                    !locations.ContainsKey(segment.ToFacilityId) ||
                    segment.DistanceMetres <= 0 ||
                    segment.WeightedDistanceMetres < segment.DistanceMetres ||
                    segment.DurationMinutes <= 0 ||
                    segment.StaminaCostBasisPoints <= 0 ||
                    segment.FoodCost < 0)
                    throw new InvalidOperationException(
                        "Invalid Luoyang movement route snapshot.");
                duration = checked(duration + segment.DurationMinutes);
                stamina = checked(stamina +
                    segment.StaminaCostBasisPoints);
                food = checked(food + segment.FoodCost);
                expectedFrom = segment.ToFacilityId;
            }
            if (!string.Equals(expectedFrom, movement.TargetFacilityId,
                    StringComparison.Ordinal) ||
                duration != movement.ExpectedDurationMinutes ||
                stamina != movement.ExpectedStaminaCostBasisPoints ||
                food != movement.ExpectedFoodCost ||
                movement.ConsumedStaminaBasisPoints > stamina ||
                movement.ConsumedFood > food ||
                movement.ElapsedDurationMinutes > duration)
                throw new InvalidOperationException(
                    "Luoyang movement totals do not match its route snapshot.");
            if (movement.Status == LuoyangFormalMovementStatus.Active)
            {
                if (movement.CurrentSegmentIndex >= movement.Segments.Count ||
                    movement.CompletedDay != -1 ||
                    !string.IsNullOrEmpty(movement.CompletionEventId) ||
                    !string.IsNullOrEmpty(movement.FailureReasonId))
                    throw new InvalidOperationException(
                        "Invalid active Luoyang movement lifecycle.");
            }
            else if (movement.CompletedDay < movement.IssuedDay ||
                     string.IsNullOrEmpty(movement.CompletionEventId) ||
                     movement.Status == LuoyangFormalMovementStatus.Completed &&
                     movement.CurrentSegmentIndex != movement.Segments.Count ||
                     movement.Status == LuoyangFormalMovementStatus.Interrupted &&
                     string.IsNullOrEmpty(movement.FailureReasonId))
                throw new InvalidOperationException(
                    "Invalid terminal Luoyang movement lifecycle.");
        }
    }

    [Serializable]
    public sealed class LuoyangPassageTraversalWorldState
    {
        public string Id;
        public string FacilityId;
        public string FacilityDefinitionId;
        public string TraversalStatusId;
        public long Revision;
        public long LastChangedDay;
        public byte LastChangedSegment;
        public string LastReasonId;
        public string LastCommandId;
        public string LastEventId;
    }

    [Serializable]
    public sealed class LuoyangPassageOperationalControlState
    {
        public string Id;
        public string FacilityId;
        public string ControllerOrganizationId;
        public string GuardArmyId;
        public string GuardCommanderPersonId;
        public List<string> GuardPersonIds = new List<string>();
        public string AuthorizedByPersonId;
        public string AuthorityBasisId;
        public long ActivatedPassageRevision;
        public string InitialTraversalStatusId;
        public int InitialConditionBasisPoints;
        public int CurrentConditionBasisPoints;
        public long IntegrityRevision;
        public string LastDamageRecordId;
        public string ActiveRepairOrderId;
        public long AssignedDay;
        public byte AssignedSegment;
        public string AssignmentCommandId;
        public string AssignmentEventId;
    }

    [Serializable]
    public sealed class LuoyangPassageDamageRecordState
    {
        public string Id;
        public string FacilityId;
        public string BattleRecordId;
        public string AttackerArmyId;
        public string AttackerCommanderPersonId;
        public string AuthorityBasisId;
        public int DamageBasisPoints;
        public int ConditionBeforeBasisPoints;
        public int ConditionAfterBasisPoints;
        public long IntegrityRevision;
        public long PassageRevisionBefore;
        public long PassageRevisionAfter;
        public long Day;
        public byte Segment;
        public string CommandId;
        public string EventId;
    }

    public enum LuoyangPassageRepairStatus : byte
    {
        InProgress,
        Completed
    }

    [Serializable]
    public sealed class LuoyangPassageRepairOrderState
    {
        public string Id;
        public string FacilityId;
        public string ProfileId;
        public string ControllerOrganizationId;
        public string AuthorizingPersonId;
        public string AuthorityBasisId;
        public string ManagerPersonId;
        public string MaterialInventoryContainerId;
        public string FacilityConstructionProjectId;
        public string SourceDamageRecordId;
        public long SourceIntegrityRevision;
        public long SourcePassageRevision;
        public long StartedDay;
        public long CompletedDay = -1;
        public LuoyangPassageRepairStatus Status;
        public string StartCommandId;
        public string StartEventId;
        public string CompletionCommandId;
        public string CompletionEventId;
    }

    [Serializable]
    public sealed class LuoyangRoadConnectorWaypoint
    {
        public int Sequence;
        public int GridColumn;
        public int GridRow;
    }

    [Serializable]
    public sealed class LuoyangModeledRoadConnector
    {
        public string ConnectorId;
        public string SourceProvisionalEdgeId;
        public string RefinedEdgeId;
        public string FromNodeId;
        public string ToNodeId;
        public string FromFacilityId;
        public string ToFacilityId;
        public string EvidenceClassId;
        public string SpatialPrecisionId;
        public string RouteAuthoringProfileId;
        public int OccupiedNonNavigationCellCrossingCount;
        public bool ClaimsHistoricalExactness;
        public IReadOnlyList<LuoyangRoadConnectorWaypoint> Waypoints;
    }

    [Serializable]
    public sealed class LuoyangPassageTraversalRecord
    {
        public string FacilityId { get; internal set; }
        public string FacilityDefinitionId { get; internal set; }
        public string TraversalStatusId { get; internal set; }
        public long Revision { get; internal set; }
        public long LastChangedTick { get; internal set; }
        public string LastReasonId { get; internal set; }

        public bool CanTraverse => string.Equals(TraversalStatusId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                StringComparison.Ordinal) ||
            string.Equals(TraversalStatusId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                StringComparison.Ordinal);

        public int TraversalCostPermille => string.Equals(TraversalStatusId,
            LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
            StringComparison.Ordinal) ? 1800 : CanTraverse ? 1000 : int.MaxValue;
    }

    public sealed class LuoyangPassageTraversalSession
    {
        private readonly Dictionary<string, LuoyangPassageTraversalRecord>
            _recordsByFacilityId;
        private readonly bool _changesSaveSchema;
        private readonly bool _persistsAcrossSave;
        private readonly bool _isWorldStateProjection;

        internal LuoyangPassageTraversalSession(
            IEnumerable<LuoyangPassageTraversalRecord> records,
            bool changesSaveSchema = false,
            bool persistsAcrossSave = false,
            bool isWorldStateProjection = false)
        {
            _recordsByFacilityId = records.ToDictionary(item => item.FacilityId,
                StringComparer.Ordinal);
            _changesSaveSchema = changesSaveSchema;
            _persistsAcrossSave = persistsAcrossSave;
            _isWorldStateProjection = isWorldStateProjection;
        }

        public string ContractId =>
            LuoyangRoadConnectorPassageTraversalIds.ContractId;
        public bool ChangesSaveSchema => _changesSaveSchema;
        public bool PersistsAcrossSave => _persistsAcrossSave;
        public bool IsWorldStateProjection => _isWorldStateProjection;
        public IReadOnlyList<LuoyangPassageTraversalRecord> Records =>
            _recordsByFacilityId.Values.OrderBy(item => item.FacilityId,
                StringComparer.Ordinal).ToArray();

        public LuoyangPassageTraversalRecord Get(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId) ||
                !_recordsByFacilityId.TryGetValue(facilityId, out var record))
                throw new KeyNotFoundException(
                    "Unknown Luoyang passage Facility ID: " + facilityId);
            return record;
        }

        public bool TryGet(string facilityId,
            out LuoyangPassageTraversalRecord record) =>
            _recordsByFacilityId.TryGetValue(facilityId, out record);

        public bool SetStatus(string facilityId, string statusId,
            long absoluteTick, string reasonId)
        {
            if (_isWorldStateProjection)
                throw new InvalidOperationException(
                    "A persisted Luoyang passage projection is read-only; " +
                    "change it through a world command.");
            if (!LuoyangRoadConnectorPassageTraversalIds.StatusIds.Contains(
                    statusId, StringComparer.Ordinal))
                throw new ArgumentException(
                    "Unknown Luoyang passage traversal status.", nameof(statusId));
            if (absoluteTick < 0)
                throw new ArgumentOutOfRangeException(nameof(absoluteTick));
            if (string.IsNullOrWhiteSpace(reasonId))
                throw new ArgumentException(
                    "A stable passage transition reason ID is required.",
                    nameof(reasonId));
            var current = Get(facilityId);
            if (absoluteTick < current.LastChangedTick)
                throw new InvalidOperationException(
                    "Passage state cannot move backwards in deterministic time.");
            if (string.Equals(current.TraversalStatusId, statusId,
                    StringComparison.Ordinal)) return false;
            current.TraversalStatusId = statusId;
            current.Revision++;
            current.LastChangedTick = absoluteTick;
            current.LastReasonId = reasonId;
            return true;
        }
    }

    public sealed class LuoyangRoadTraversalRefinementPlan
    {
        public LuoyangRoadTraversalRefinementPlan(
            LuoyangFacilityInteractionNavigationPlan basePlan,
            IReadOnlyList<LuoyangModeledRoadConnector> modeledConnectors,
            IReadOnlyList<LuoyangRoadNavigationEdge> navigationEdges,
            IReadOnlyList<string> passageFacilityIds)
        {
            BasePlan = basePlan ?? throw new ArgumentNullException(nameof(basePlan));
            ModeledConnectors = modeledConnectors ?? throw new ArgumentNullException(
                nameof(modeledConnectors));
            NavigationEdges = navigationEdges ?? throw new ArgumentNullException(
                nameof(navigationEdges));
            PassageFacilityIds = passageFacilityIds ?? throw new ArgumentNullException(
                nameof(passageFacilityIds));
            ModeledConnectorsByEdgeId = modeledConnectors.ToDictionary(
                item => item.RefinedEdgeId, StringComparer.Ordinal);
        }

        public string ContractId =>
            LuoyangRoadConnectorPassageTraversalIds.ContractId;
        public string TaskId => LuoyangRoadConnectorPassageTraversalIds.TaskId;
        public string StatusId =>
            LuoyangRoadConnectorPassageTraversalIds.StatusId;
        public bool ChangesSaveSchema =>
            LuoyangRoadConnectorPassageTraversalIds.ChangesSaveSchema;
        public bool PersistsAcrossSave =>
            LuoyangRoadConnectorPassageTraversalIds.PersistsAcrossSave;
        public LuoyangFacilityInteractionNavigationPlan BasePlan { get; }
        public IReadOnlyList<LuoyangRoadNavigationNode> NavigationNodes =>
            BasePlan.NavigationNodes;
        public IReadOnlyDictionary<string, LuoyangRoadNavigationNode>
            NavigationNodesByFacilityId => BasePlan.NavigationNodesByFacilityId;
        public IReadOnlyList<LuoyangModeledRoadConnector> ModeledConnectors { get; }
        public IReadOnlyDictionary<string, LuoyangModeledRoadConnector>
            ModeledConnectorsByEdgeId { get; }
        public IReadOnlyList<LuoyangRoadNavigationEdge> NavigationEdges { get; }
        public IReadOnlyList<string> PassageFacilityIds { get; }
    }

    public static class LuoyangRoadConnectorPassageTraversalRules
    {
        private const string RoadDefinitionId = "facility.public.road";

        public static LuoyangRoadTraversalRefinementPlan CreatePlan(
            LuoyangFacilityInteractionNavigationPlan basePlan)
        {
            if (basePlan == null) throw new ArgumentNullException(nameof(basePlan));
            var nodeById = basePlan.NavigationNodes.ToDictionary(
                item => item.NodeId, StringComparer.Ordinal);
            var roads = basePlan.NavigationNodes.Where(IsRoad)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            var passages = basePlan.NavigationNodes.Where(item => !IsRoad(item))
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).ToArray();
            var navigationCells = new HashSet<long>(basePlan.NavigationNodes
                .Select(item => CellKey(item.GridRow, item.GridColumn)));
            var blockedCells = new HashSet<long>(basePlan.SelectionProxies
                .Where(item => !navigationCells.Contains(CellKey(item.GridRow,
                    item.GridColumn)))
                .Select(item => CellKey(item.GridRow, item.GridColumn)));
            const int routeSearchPaddingCells = 2;
            var minRow = Math.Max(0, basePlan.SelectionProxies.Min(item =>
                item.GridRow) - routeSearchPaddingCells);
            var maxRow = basePlan.SelectionProxies.Max(item => item.GridRow) +
                         routeSearchPaddingCells;
            var minColumn = Math.Max(0, basePlan.SelectionProxies.Min(item =>
                item.GridColumn) - routeSearchPaddingCells);
            var maxColumn = basePlan.SelectionProxies.Max(item =>
                item.GridColumn) + routeSearchPaddingCells;
            var edges = basePlan.NavigationEdges.Where(item => string.Equals(
                    item.EdgeProfileId,
                    LuoyangFacilityInteractionNavigationIds.StrictRoadEdgeProfileId,
                    StringComparison.Ordinal))
                .Select(CloneEdge).ToList();
            var connectors = new List<LuoyangModeledRoadConnector>();
            foreach (var source in basePlan.NavigationEdges.Where(item =>
                         item.Provisional).OrderBy(item => item.EdgeId,
                         StringComparer.Ordinal))
            {
                var first = nodeById[source.FromNodeId];
                var second = nodeById[source.ToNodeId];
                var connectorId = "road-connector.modeled." +
                                  first.FacilityId + ".to." + second.FacilityId;
                var waypoints = BuildMinimumObstructionRoute(first, second,
                    blockedCells, minRow, maxRow, minColumn, maxColumn);
                var refined = CreateEdge(first, second,
                    LuoyangRoadConnectorPassageTraversalIds
                        .ModeledConnectorEdgeProfileId,
                    (waypoints.Count - 1) *
                    LuoyangFacilityInteractionNavigationIds.CellSizeMetres);
                connectors.Add(new LuoyangModeledRoadConnector
                {
                    ConnectorId = connectorId,
                    SourceProvisionalEdgeId = source.EdgeId,
                    RefinedEdgeId = refined.EdgeId,
                    FromNodeId = refined.FromNodeId,
                    ToNodeId = refined.ToNodeId,
                    FromFacilityId = nodeById[refined.FromNodeId].FacilityId,
                    ToFacilityId = nodeById[refined.ToNodeId].FacilityId,
                    EvidenceClassId = LuoyangRoadConnectorPassageTraversalIds
                        .EvidenceClassId,
                    SpatialPrecisionId = LuoyangRoadConnectorPassageTraversalIds
                        .SpatialPrecisionId,
                    RouteAuthoringProfileId =
                        LuoyangRoadConnectorPassageTraversalIds
                            .RouteAuthoringProfileId,
                    OccupiedNonNavigationCellCrossingCount = waypoints.Count(
                        point => blockedCells.Contains(CellKey(point.GridRow,
                            point.GridColumn))),
                    ClaimsHistoricalExactness = false,
                    Waypoints = waypoints
                });
                edges.Add(refined);
            }

            foreach (var passage in passages)
            {
                foreach (var road in SelectTwoApproachRoads(passage, roads))
                    edges.Add(CreateEdge(passage, road,
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId,
                        EuclideanDistance(passage, road) *
                        LuoyangFacilityInteractionNavigationIds.CellSizeMetres));
            }

            var plan = new LuoyangRoadTraversalRefinementPlan(basePlan,
                connectors.OrderBy(item => item.ConnectorId,
                    StringComparer.Ordinal).ToArray(),
                edges.OrderBy(item => item.EdgeId, StringComparer.Ordinal).ToArray(),
                passages.Select(item => item.FacilityId).ToArray());
            Validate(plan);
            return plan;
        }

        public static LuoyangPassageTraversalSession CreateInitialSession(
            LuoyangRoadTraversalRefinementPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var records = plan.PassageFacilityIds.Select(facilityId =>
            {
                var node = plan.NavigationNodesByFacilityId[facilityId];
                return new LuoyangPassageTraversalRecord
                {
                    FacilityId = facilityId,
                    FacilityDefinitionId = node.FacilityDefinitionId,
                    TraversalStatusId =
                        LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                    Revision = 0,
                    LastChangedTick = 0,
                    LastReasonId = LuoyangRoadConnectorPassageTraversalIds
                        .InitialReasonId
                };
            });
            return new LuoyangPassageTraversalSession(records);
        }

        public static LuoyangPassageTraversalSession CreateSessionFromWorldState(
            LuoyangRoadTraversalRefinementPlan plan,
            WorldState world)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            Validate(plan);
            LuoyangPassageTraversalWorldRules.ValidateWorld(world);
            if (world.LuoyangPassageTraversals.Count !=
                LuoyangPassageTraversalWorldContractIds.PassageCount)
                throw new InvalidOperationException(
                    "The persisted Luoyang passage set is not initialized.");

            var persistedByFacility = world.LuoyangPassageTraversals
                .ToDictionary(item => item.FacilityId, StringComparer.Ordinal);
            var records = new List<LuoyangPassageTraversalRecord>(
                plan.PassageFacilityIds.Count);
            foreach (var facilityId in plan.PassageFacilityIds.OrderBy(
                         item => item, StringComparer.Ordinal))
            {
                if (!persistedByFacility.TryGetValue(facilityId,
                        out var persisted))
                    throw new InvalidOperationException(
                        "Persisted Luoyang passage is missing Facility " +
                        facilityId + ".");
                var definitionId = plan.NavigationNodesByFacilityId[facilityId]
                    .FacilityDefinitionId;
                if (!string.Equals(persisted.FacilityDefinitionId,
                        definitionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Persisted Luoyang passage definition drifted for " +
                        facilityId + ".");
                records.Add(new LuoyangPassageTraversalRecord
                {
                    FacilityId = persisted.FacilityId,
                    FacilityDefinitionId = persisted.FacilityDefinitionId,
                    TraversalStatusId = persisted.TraversalStatusId,
                    Revision = persisted.Revision,
                    LastChangedTick = checked(persisted.LastChangedDay * 4L +
                                              persisted.LastChangedSegment),
                    LastReasonId = persisted.LastReasonId
                });
            }

            return new LuoyangPassageTraversalSession(records,
                LuoyangPassageTraversalWorldContractIds.ChangesSaveSchema,
                LuoyangPassageTraversalWorldContractIds.PersistsAcrossSave,
                true);
        }

        public static IReadOnlyList<string> FindFacilityPath(
            LuoyangRoadTraversalRefinementPlan plan,
            LuoyangPassageTraversalSession session,
            string fromFacilityId, string toFacilityId)
        {
            return FindFacilityPath(plan, session, fromFacilityId,
                toFacilityId, null);
        }

        public static IReadOnlyList<string> FindFacilityPath(
            LuoyangRoadTraversalRefinementPlan plan,
            LuoyangPassageTraversalSession session,
            string fromFacilityId, string toFacilityId,
            Func<string, bool> canTraverseEdge)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (!plan.NavigationNodesByFacilityId.TryGetValue(fromFacilityId,
                    out var from) ||
                !plan.NavigationNodesByFacilityId.TryGetValue(toFacilityId,
                    out var to)) return Array.Empty<string>();
            if (!CanEnter(session, from.FacilityId) ||
                !CanEnter(session, to.FacilityId)) return Array.Empty<string>();
            if (string.Equals(from.NodeId, to.NodeId, StringComparison.Ordinal))
                return new[] { fromFacilityId };

            var adjacency = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                _ => new List<WeightedNeighbor>(), StringComparer.Ordinal);
            var nodeById = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            foreach (var edge in plan.NavigationEdges)
            {
                if (canTraverseEdge != null &&
                    !canTraverseEdge(edge.EdgeId)) continue;
                var edgeCost = Math.Max(1d, edge.TraversalCostMetres);
                adjacency[edge.FromNodeId].Add(new WeightedNeighbor(
                    edge.ToNodeId, edgeCost));
                adjacency[edge.ToNodeId].Add(new WeightedNeighbor(
                    edge.FromNodeId, edgeCost));
            }
            foreach (var neighbors in adjacency.Values)
                neighbors.Sort((a, b) => string.CompareOrdinal(a.NodeId,
                    b.NodeId));

            var distance = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                _ => double.PositiveInfinity, StringComparer.Ordinal);
            var previous = new Dictionary<string, string>(StringComparer.Ordinal);
            var unvisited = new HashSet<string>(distance.Keys,
                StringComparer.Ordinal);
            distance[from.NodeId] = 0d;
            while (unvisited.Count > 0)
            {
                var current = unvisited.OrderBy(item => distance[item])
                    .ThenBy(item => item, StringComparer.Ordinal).First();
                if (double.IsPositiveInfinity(distance[current])) break;
                unvisited.Remove(current);
                if (string.Equals(current, to.NodeId, StringComparison.Ordinal))
                    break;
                foreach (var neighbor in adjacency[current])
                {
                    if (!unvisited.Contains(neighbor.NodeId)) continue;
                    var destination = nodeById[neighbor.NodeId];
                    if (!CanEnter(session, destination.FacilityId)) continue;
                    var candidate = distance[current] + neighbor.CostMetres *
                        TraversalMultiplier(session, destination.FacilityId);
                    if (candidate > distance[neighbor.NodeId] + 0.0001d)
                        continue;
                    if (Math.Abs(candidate - distance[neighbor.NodeId]) <=
                            0.0001d && previous.TryGetValue(neighbor.NodeId,
                            out var oldPrevious) &&
                        string.CompareOrdinal(current, oldPrevious) >= 0)
                        continue;
                    distance[neighbor.NodeId] = candidate;
                    previous[neighbor.NodeId] = current;
                }
            }
            if (!previous.ContainsKey(to.NodeId)) return Array.Empty<string>();
            var reversed = new List<string>();
            for (var cursor = to.NodeId;; cursor = previous[cursor])
            {
                reversed.Add(nodeById[cursor].FacilityId);
                if (string.Equals(cursor, from.NodeId, StringComparison.Ordinal))
                    break;
            }
            reversed.Reverse();
            return reversed;
        }

        public static void Validate(LuoyangRoadTraversalRefinementPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.ChangesSaveSchema || plan.PersistsAcrossSave ||
                plan.ModeledConnectors.Count !=
                    LuoyangRoadConnectorPassageTraversalIds.ModeledConnectorCount ||
                plan.PassageFacilityIds.Count !=
                    LuoyangRoadConnectorPassageTraversalIds.PassageCount ||
                plan.NavigationEdges.Count !=
                    LuoyangRoadConnectorPassageTraversalIds
                        .RefinedNavigationEdgeCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang refined road traversal totals.");
            var nodes = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            if (plan.NavigationEdges.Any(edge => edge == null || edge.Provisional ||
                    !nodes.ContainsKey(edge.FromNodeId) ||
                    !nodes.ContainsKey(edge.ToNodeId) ||
                    edge.TraversalCostMetres <= 0f) ||
                plan.NavigationEdges.Select(item => item.EdgeId).Distinct(
                    StringComparer.Ordinal).Count() != plan.NavigationEdges.Count)
                throw new InvalidOperationException(
                    "Invalid Luoyang refined navigation edge.");
            if (plan.NavigationEdges.Count(item => string.Equals(
                    item.EdgeProfileId,
                    LuoyangRoadConnectorPassageTraversalIds
                        .ModeledConnectorEdgeProfileId,
                    StringComparison.Ordinal)) !=
                LuoyangRoadConnectorPassageTraversalIds.ModeledConnectorCount ||
                plan.NavigationEdges.Count(item => string.Equals(
                    item.EdgeProfileId,
                    LuoyangRoadConnectorPassageTraversalIds
                        .PassageApproachEdgeProfileId,
                    StringComparison.Ordinal)) !=
                LuoyangRoadConnectorPassageTraversalIds
                    .PassageApproachEdgeCount)
                throw new InvalidOperationException(
                    "Invalid Luoyang refined navigation edge profiles.");
            foreach (var passageId in plan.PassageFacilityIds)
            {
                var nodeId = plan.NavigationNodesByFacilityId[passageId].NodeId;
                if (plan.NavigationEdges.Count(item => string.Equals(
                        item.EdgeProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId,
                        StringComparison.Ordinal) &&
                    (string.Equals(item.FromNodeId, nodeId,
                         StringComparison.Ordinal) ||
                     string.Equals(item.ToNodeId, nodeId,
                         StringComparison.Ordinal))) != 2)
                    throw new InvalidOperationException(
                        "Every Luoyang passage requires two road approaches: " +
                        passageId);
            }
            if (plan.ModeledConnectors.Any(item => item == null ||
                    string.IsNullOrWhiteSpace(item.ConnectorId) ||
                    item.ClaimsHistoricalExactness ||
                    !string.Equals(item.EvidenceClassId,
                        LuoyangRoadConnectorPassageTraversalIds.EvidenceClassId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.SpatialPrecisionId,
                        LuoyangRoadConnectorPassageTraversalIds.SpatialPrecisionId,
                        StringComparison.Ordinal) ||
                    !string.Equals(item.RouteAuthoringProfileId,
                        LuoyangRoadConnectorPassageTraversalIds
                            .RouteAuthoringProfileId,
                        StringComparison.Ordinal) ||
                    item.Waypoints == null || item.Waypoints.Count < 2 ||
                    item.Waypoints.Select((point, index) => point.Sequence == index)
                        .Any(valid => !valid)) ||
                plan.ModeledConnectors.Select(item => item.ConnectorId).Distinct(
                    StringComparer.Ordinal).Count() != plan.ModeledConnectors.Count)
                throw new InvalidOperationException(
                    "Invalid authored Luoyang road connector provenance.");
        }

        private static bool IsRoad(LuoyangRoadNavigationNode item) =>
            string.Equals(item.FacilityDefinitionId, RoadDefinitionId,
                StringComparison.Ordinal);

        private static IReadOnlyList<LuoyangRoadNavigationNode>
            SelectTwoApproachRoads(LuoyangRoadNavigationNode passage,
                IReadOnlyList<LuoyangRoadNavigationNode> roads)
        {
            var first = roads.OrderBy(item => ManhattanDistance(passage, item))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).First();
            var firstRow = first.GridRow - passage.GridRow;
            var firstColumn = first.GridColumn - passage.GridColumn;
            var second = roads.Where(item => !ReferenceEquals(item, first))
                .OrderBy(item => OppositionRank(firstRow, firstColumn,
                    item.GridRow - passage.GridRow,
                    item.GridColumn - passage.GridColumn))
                .ThenBy(item => ManhattanDistance(passage, item))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal).First();
            return new[] { first, second };
        }

        private static int OppositionRank(int firstRow, int firstColumn,
            int secondRow, int secondColumn)
        {
            var dot = firstRow * secondRow + firstColumn * secondColumn;
            return dot < 0 ? 0 : dot == 0 ? 1 : 2;
        }

        private static IReadOnlyList<LuoyangRoadConnectorWaypoint>
            BuildMinimumObstructionRoute(LuoyangRoadNavigationNode from,
                LuoyangRoadNavigationNode to,
                IReadOnlyCollection<long> blockedCells, int minRow, int maxRow,
                int minColumn, int maxColumn)
        {
            var start = CellKey(from.GridRow, from.GridColumn);
            var target = CellKey(to.GridRow, to.GridColumn);
            var queue = new SortedSet<RouteCandidate>();
            var previous = new Dictionary<long, long>();
            var scoreByCell = new Dictionary<long, RouteScore>();
            queue.Add(new RouteCandidate(start, 0, 0));
            previous[start] = start;
            scoreByCell[start] = new RouteScore(0, 0);
            var offsets = new[]
            {
                new[] { -1, 0 },
                new[] { 0, 1 },
                new[] { 1, 0 },
                new[] { 0, -1 }
            };
            while (queue.Count > 0)
            {
                var candidate = queue.Min;
                queue.Remove(candidate);
                if (!scoreByCell.TryGetValue(candidate.CellKey, out var best) ||
                    best.ObstructionCount != candidate.ObstructionCount ||
                    best.StepCount != candidate.StepCount) continue;
                var current = candidate.CellKey;
                if (current == target) break;
                var currentRow = CellRow(current);
                var currentColumn = CellColumn(current);
                foreach (var offset in offsets)
                {
                    var row = currentRow + offset[0];
                    var column = currentColumn + offset[1];
                    if (row < minRow || row > maxRow || column < minColumn ||
                        column > maxColumn) continue;
                    var next = CellKey(row, column);
                    var nextObstructions = candidate.ObstructionCount +
                        (blockedCells.Contains(next) && next != target ? 1 : 0);
                    var nextSteps = candidate.StepCount + 1;
                    if (scoreByCell.TryGetValue(next, out var oldScore) &&
                        (oldScore.ObstructionCount < nextObstructions ||
                         oldScore.ObstructionCount == nextObstructions &&
                         oldScore.StepCount <= nextSteps)) continue;
                    previous[next] = current;
                    scoreByCell[next] = new RouteScore(nextObstructions,
                        nextSteps);
                    queue.Add(new RouteCandidate(next, nextObstructions,
                        nextSteps));
                }
            }
            if (!previous.ContainsKey(target))
                throw new InvalidOperationException(
                    "No bounded minimum-obstruction cell route exists for Luoyang connector " +
                    from.FacilityId + " -> " + to.FacilityId + ".");
            var reversed = new List<long>();
            for (var cursor = target;; cursor = previous[cursor])
            {
                reversed.Add(cursor);
                if (cursor == start) break;
            }
            reversed.Reverse();
            var result = new List<LuoyangRoadConnectorWaypoint>(reversed.Count);
            foreach (var cell in reversed)
                AddWaypoint(result, CellRow(cell), CellColumn(cell));
            return result;
        }

        private static void AddWaypoint(
            ICollection<LuoyangRoadConnectorWaypoint> result,
            int row, int column) => result.Add(new LuoyangRoadConnectorWaypoint
        {
            Sequence = result.Count,
            GridRow = row,
            GridColumn = column
        });

        private static LuoyangRoadNavigationEdge CloneEdge(
            LuoyangRoadNavigationEdge source) => new LuoyangRoadNavigationEdge
        {
            EdgeId = source.EdgeId,
            FromNodeId = source.FromNodeId,
            ToNodeId = source.ToNodeId,
            EdgeProfileId = source.EdgeProfileId,
            TraversalCostMetres = source.TraversalCostMetres,
            Provisional = false
        };

        private static LuoyangRoadNavigationEdge CreateEdge(
            LuoyangRoadNavigationNode first, LuoyangRoadNavigationNode second,
            string profileId, float costMetres)
        {
            var from = string.CompareOrdinal(first.NodeId, second.NodeId) <= 0
                ? first : second;
            var to = ReferenceEquals(from, first) ? second : first;
            return new LuoyangRoadNavigationEdge
            {
                EdgeId = "navigation-edge." + profileId + "." +
                         from.FacilityId + ".to." + to.FacilityId,
                FromNodeId = from.NodeId,
                ToNodeId = to.NodeId,
                EdgeProfileId = profileId,
                TraversalCostMetres = Math.Max(1f, costMetres),
                Provisional = false
            };
        }

        private static int ManhattanDistance(LuoyangRoadNavigationNode first,
            LuoyangRoadNavigationNode second) =>
            Math.Abs(first.GridRow - second.GridRow) +
            Math.Abs(first.GridColumn - second.GridColumn);

        private static float EuclideanDistance(
            LuoyangRoadNavigationNode first, LuoyangRoadNavigationNode second)
        {
            var row = first.GridRow - second.GridRow;
            var column = first.GridColumn - second.GridColumn;
            return (float)Math.Sqrt(row * row + column * column);
        }

        private static long CellKey(int row, int column) =>
            ((long)row << 32) ^ (uint)column;

        private static int CellRow(long cellKey) => (int)(cellKey >> 32);

        private static int CellColumn(long cellKey) => (int)(uint)cellKey;

        private static bool CanEnter(LuoyangPassageTraversalSession session,
            string facilityId) => !session.TryGet(facilityId, out var record) ||
                                  record.CanTraverse;

        private static double TraversalMultiplier(
            LuoyangPassageTraversalSession session, string facilityId) =>
            session.TryGet(facilityId, out var record)
                ? record.TraversalCostPermille / 1000d : 1d;

        private sealed class WeightedNeighbor
        {
            public WeightedNeighbor(string nodeId, double costMetres)
            {
                NodeId = nodeId;
                CostMetres = costMetres;
            }

            public string NodeId { get; }
            public double CostMetres { get; }
        }

        private sealed class RouteScore
        {
            public RouteScore(int obstructionCount, int stepCount)
            {
                ObstructionCount = obstructionCount;
                StepCount = stepCount;
            }

            public int ObstructionCount { get; }
            public int StepCount { get; }
        }

        private sealed class RouteCandidate : IComparable<RouteCandidate>
        {
            public RouteCandidate(long cellKey, int obstructionCount,
                int stepCount)
            {
                CellKey = cellKey;
                ObstructionCount = obstructionCount;
                StepCount = stepCount;
            }

            public long CellKey { get; }
            public int ObstructionCount { get; }
            public int StepCount { get; }

            public int CompareTo(RouteCandidate other)
            {
                var obstruction = ObstructionCount.CompareTo(
                    other.ObstructionCount);
                if (obstruction != 0) return obstruction;
                var step = StepCount.CompareTo(other.StepCount);
                return step != 0 ? step : CellKey.CompareTo(other.CellKey);
            }
        }
    }

    public static class LuoyangPassageTraversalWorldRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.LuoyangPassageTraversals == null)
                throw new InvalidOperationException(
                    "Luoyang passage world state cannot be null.");
            if (world.LuoyangPassageTraversals.Count != 0 &&
                world.LuoyangPassageTraversals.Count !=
                LuoyangPassageTraversalWorldContractIds.PassageCount)
                throw new InvalidOperationException(
                    "A persisted Luoyang passage set must be empty or contain " +
                    LuoyangPassageTraversalWorldContractIds.PassageCount +
                    " records.");

            var initializationCommands = world.PersistentWorldCommands.Where(
                    item => item != null && string.Equals(item.CommandTypeId,
                        LuoyangPassageTraversalWorldContractIds
                            .InitializationCommandTypeId,
                        StringComparison.Ordinal))
                .ToArray();
            var completedInitializationCommands = initializationCommands.Where(
                    item => item.Status == PersistentWorldCommandStatus.Completed)
                .ToArray();
            if (world.LuoyangPassageTraversals.Count == 0)
            {
                if (completedInitializationCommands.Length != 0 ||
                    world.PersistentWorldCommands.Any(item => item != null &&
                        item.Status == PersistentWorldCommandStatus.Completed &&
                        string.Equals(item.CommandTypeId,
                            LuoyangPassageTraversalWorldContractIds
                                .TransitionCommandTypeId,
                            StringComparison.Ordinal)) ||
                    world.WorldEventOutbox.Any(item => item != null &&
                        (string.Equals(item.EventTypeId,
                             LuoyangPassageTraversalWorldContractIds
                                 .InitializedEventTypeId,
                             StringComparison.Ordinal) ||
                         string.Equals(item.EventTypeId,
                             LuoyangPassageTraversalWorldContractIds
                                 .TransitionedEventTypeId,
                             StringComparison.Ordinal))))
                    throw new InvalidOperationException(
                        "Completed Luoyang passage execution requires the " +
                        "persisted passage set.");
                return;
            }
            if (completedInitializationCommands.Length != 1)
                throw new InvalidOperationException(
                    "The persisted Luoyang passage set requires exactly one " +
                    "completed initialization command.");

            var initialization = completedInitializationCommands[0];
            if (!string.Equals(initialization.Id,
                    LuoyangPassageTraversalWorldContractIds.InitializationCommandId,
                    StringComparison.Ordinal) ||
                !string.Equals(initialization.IssuerId,
                    LuoyangPassageTraversalWorldContractIds.InitializationIssuerId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The Luoyang passage initialization command identity is invalid.");
            var initializationArguments = ArgumentsById(initialization);
            if (initializationArguments.Count != 2 +
                    LuoyangPassageTraversalWorldContractIds.PassageCount * 2 ||
                !TryGet(initializationArguments,
                    LuoyangPassageTraversalWorldContractIds.ContractArgumentId,
                    out var contractId) ||
                !string.Equals(contractId,
                    LuoyangPassageTraversalWorldContractIds.ContractId,
                    StringComparison.Ordinal) ||
                !TryGet(initializationArguments,
                    LuoyangPassageTraversalWorldContractIds.PassageCountArgumentId,
                    out var countText) ||
                !int.TryParse(countText,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var count) ||
                count != LuoyangPassageTraversalWorldContractIds.PassageCount)
                throw new InvalidOperationException(
                    "The Luoyang passage initialization command arguments are invalid.");

            var initializationEvent = world.WorldEventOutbox.FirstOrDefault(
                item => item != null && string.Equals(item.Id,
                    LuoyangPassageTraversalWorldContractIds.InitializationEventId,
                    StringComparison.Ordinal));
            if (initializationEvent == null || !string.Equals(
                    initializationEvent.EventTypeId,
                    LuoyangPassageTraversalWorldContractIds.InitializedEventTypeId,
                    StringComparison.Ordinal) ||
                !string.Equals(initializationEvent.SourceTransactionId,
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationTransactionId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The Luoyang passage initialization event is missing.");

            string previousFacilityId = null;
            for (var index = 0;
                 index < world.LuoyangPassageTraversals.Count;
                 index++)
            {
                var state = world.LuoyangPassageTraversals[index] ??
                    throw new InvalidOperationException(
                        "A Luoyang passage world state cannot be null.");
                _ = new StableId(state.Id);
                _ = new StableId(state.FacilityId);
                _ = new StableId(state.FacilityDefinitionId);
                _ = new StableId(state.TraversalStatusId);
                _ = new StableId(state.LastReasonId);
                _ = new StableId(state.LastCommandId);
                _ = new StableId(state.LastEventId);
                if (!string.Equals(state.Id,
                        LuoyangPassageTraversalWorldContractIds.StateId(
                            state.FacilityId), StringComparison.Ordinal) ||
                    previousFacilityId != null && string.CompareOrdinal(
                        previousFacilityId, state.FacilityId) >= 0 ||
                    !LuoyangRoadConnectorPassageTraversalIds.StatusIds.Contains(
                        state.TraversalStatusId, StringComparer.Ordinal) ||
                    state.Revision < 0 || state.LastChangedDay < 0 ||
                    state.LastChangedDay > world.AbsoluteDay ||
                    state.LastChangedSegment > (byte)DaySegment.Night)
                    throw new InvalidOperationException(
                        "Invalid persisted Luoyang passage state " + state.Id +
                        ".");
                previousFacilityId = state.FacilityId;

                if (!TryGet(initializationArguments,
                        LuoyangPassageTraversalWorldContractIds
                            .InitializationFacilityArgumentId(index),
                        out var initializedFacilityId) ||
                    !TryGet(initializationArguments,
                        LuoyangPassageTraversalWorldContractIds
                            .InitializationDefinitionArgumentId(index),
                        out var initializedDefinitionId) ||
                    !string.Equals(initializedFacilityId, state.FacilityId,
                        StringComparison.Ordinal) ||
                    !string.Equals(initializedDefinitionId,
                        state.FacilityDefinitionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The Luoyang passage initialization snapshot does not " +
                        "match persisted state " + state.Id + ".");

                ValidateTransitionHistory(world, state, initialization,
                    initializationEvent);
            }
        }

        private static void ValidateTransitionHistory(
            WorldState world,
            LuoyangPassageTraversalWorldState state,
            PersistentWorldCommandState initialization,
            WorldEventOutboxState initializationEvent)
        {
            var transitions = world.PersistentWorldCommands.Where(item =>
                    item != null && item.Status ==
                        PersistentWorldCommandStatus.Completed &&
                    string.Equals(item.CommandTypeId,
                        LuoyangPassageTraversalWorldContractIds
                            .TransitionCommandTypeId,
                        StringComparison.Ordinal) &&
                    CommandTargetsFacility(item, state.FacilityId))
                .OrderBy(item => ParseExpectedRevision(item))
                .ToArray();
            if (transitions.Length != state.Revision)
                throw new InvalidOperationException(
                    "Luoyang passage transition history does not match revision " +
                    state.Id + ".");

            if (state.Revision == 0)
            {
                if (!string.Equals(state.TraversalStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                        StringComparison.Ordinal) ||
                    !string.Equals(state.LastReasonId,
                        LuoyangRoadConnectorPassageTraversalIds.InitialReasonId,
                        StringComparison.Ordinal) ||
                    !string.Equals(state.LastCommandId, initialization.Id,
                        StringComparison.Ordinal) ||
                    !string.Equals(state.LastEventId, initializationEvent.Id,
                        StringComparison.Ordinal) ||
                    state.LastChangedDay != initializationEvent.Day ||
                    state.LastChangedSegment != initializationEvent.Segment)
                    throw new InvalidOperationException(
                        "Invalid initial Luoyang passage provenance " + state.Id +
                        ".");
                return;
            }

            for (var index = 0; index < transitions.Length; index++)
            {
                var command = transitions[index];
                var expectedRevision = ParseExpectedRevision(command);
                if (expectedRevision != index)
                    throw new InvalidOperationException(
                        "Luoyang passage transition revisions are not contiguous " +
                        state.Id + ".");
                var arguments = ArgumentsById(command);
                if (arguments.Count < 5 || !TryGet(arguments,
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        out var definitionId) ||
                    !string.Equals(definitionId, state.FacilityDefinitionId,
                        StringComparison.Ordinal) ||
                    !TryGet(arguments,
                        LuoyangPassageTraversalWorldContractIds
                            .TargetStatusArgumentId,
                        out var statusId) ||
                    !LuoyangRoadConnectorPassageTraversalIds.StatusIds.Contains(
                        statusId, StringComparer.Ordinal) ||
                    !TryGet(arguments,
                        LuoyangPassageTraversalWorldContractIds.ReasonArgumentId,
                        out var reasonId))
                    throw new InvalidOperationException(
                        "Invalid Luoyang passage transition command " +
                        command.Id + ".");
                _ = new StableId(reasonId);
                ValidateTransitionCause(world, state, command, arguments,
                    expectedRevision, statusId);

                var revision = expectedRevision + 1;
                var eventId = LuoyangPassageTraversalWorldContractIds
                    .TransitionEventId(state.FacilityId, revision);
                var transitionEvent = world.WorldEventOutbox.FirstOrDefault(
                    item => item != null && string.Equals(item.Id, eventId,
                        StringComparison.Ordinal));
                if (transitionEvent == null || !string.Equals(
                        transitionEvent.EventTypeId,
                        LuoyangPassageTraversalWorldContractIds
                            .TransitionedEventTypeId,
                        StringComparison.Ordinal) ||
                    !string.Equals(transitionEvent.SourceTransactionId,
                        LuoyangPassageTraversalWorldContractIds
                            .TransitionTransactionId(state.FacilityId,
                                revision, command.Id),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Missing Luoyang passage transition event " + eventId +
                        ".");

                if (revision == state.Revision &&
                    (!string.Equals(state.TraversalStatusId, statusId,
                         StringComparison.Ordinal) ||
                     !string.Equals(state.LastReasonId, reasonId,
                         StringComparison.Ordinal) ||
                     !string.Equals(state.LastCommandId, command.Id,
                         StringComparison.Ordinal) ||
                     !string.Equals(state.LastEventId, eventId,
                         StringComparison.Ordinal) ||
                     state.LastChangedDay != transitionEvent.Day ||
                     state.LastChangedSegment != transitionEvent.Segment))
                    throw new InvalidOperationException(
                        "The current Luoyang passage state does not match its " +
                        "latest transition " + state.Id + ".");
            }
        }

        private static void ValidateTransitionCause(
            WorldState world,
            LuoyangPassageTraversalWorldState state,
            PersistentWorldCommandState command,
            IReadOnlyDictionary<string, string> arguments,
            long expectedRevision,
            string targetStatusId)
        {
            var control = world.LuoyangPassageOperationalControls.FirstOrDefault(
                item => item != null && string.Equals(item.FacilityId,
                    state.FacilityId, StringComparison.Ordinal));
            if (!TryGet(arguments,
                    LuoyangPassageOperationsContractIds.CauseArgumentId,
                    out var causeId))
            {
                if (arguments.Count != 5 || control != null &&
                    expectedRevision >= control.ActivatedPassageRevision)
                    throw new InvalidOperationException(
                        "An operational Luoyang passage transition requires " +
                        "an audited cause.");
                return;
            }

            _ = new StableId(causeId);
            if (!TryGet(arguments,
                    LuoyangPassageOperationsContractIds
                        .AuthorityBasisArgumentId,
                    out var authorityBasisId))
                throw new InvalidOperationException(
                    "An operational Luoyang passage transition lacks an " +
                    "authority snapshot.");
            _ = new StableId(authorityBasisId);
            _ = new StableId(command.IssuerId);

            if (string.Equals(causeId,
                    LuoyangPassageOperationsContractIds.GuardOperationCauseId,
                    StringComparison.Ordinal))
            {
                if (arguments.Count != 7 || control == null ||
                    !string.Equals(authorityBasisId,
                         LuoyangPassageOperationsContractIds
                             .OrganizationLeaderAuthorityId,
                         StringComparison.Ordinal) &&
                    !string.Equals(authorityBasisId,
                         LuoyangPassageOperationsContractIds
                             .GuardArmyCommanderAuthorityId,
                         StringComparison.Ordinal) ||
                    !string.Equals(targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                         StringComparison.Ordinal) &&
                    !string.Equals(targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                         StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid guarded Luoyang passage transition cause.");
                return;
            }

            if (string.Equals(causeId,
                    LuoyangPassageOperationsContractIds.BattleDamageCauseId,
                    StringComparison.Ordinal))
            {
                if (arguments.Count != 10 || control == null ||
                    !string.Equals(authorityBasisId,
                         LuoyangPassageOperationsContractIds
                             .AttackingArmyCommanderAuthorityId,
                         StringComparison.Ordinal) ||
                    !TryGet(arguments,
                        LuoyangPassageOperationsContractIds
                            .BattleRecordIdArgumentId,
                        out var battleRecordId) ||
                    !TryGet(arguments,
                        LuoyangPassageOperationsContractIds
                            .AttackerArmyIdArgumentId,
                        out var attackerArmyId) ||
                    !TryGet(arguments,
                        LuoyangPassageOperationsContractIds
                            .DamageBasisPointsArgumentId,
                        out var damageText) ||
                    !int.TryParse(damageText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var damage) || damage <= 0 || damage > 10_000 ||
                    !string.Equals(targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                         StringComparison.Ordinal) &&
                    !string.Equals(targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                         StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid battle-damage Luoyang passage transition cause.");
                _ = new StableId(battleRecordId);
                _ = new StableId(attackerArmyId);
                return;
            }

            if (string.Equals(causeId,
                    LuoyangPassageOperationsContractIds.RepairCompletionCauseId,
                    StringComparison.Ordinal))
            {
                if (arguments.Count != 8 || control == null ||
                    !TryGet(arguments,
                        LuoyangPassageOperationsContractIds
                            .RepairOrderIdArgumentId,
                        out var repairOrderId) ||
                    !string.Equals(targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                         StringComparison.Ordinal) ||
                    !string.Equals(authorityBasisId,
                         LuoyangPassageOperationsContractIds
                             .OrganizationLeaderAuthorityId,
                         StringComparison.Ordinal) &&
                    !string.Equals(authorityBasisId,
                         LuoyangPassageOperationsContractIds
                             .GuardArmyCommanderAuthorityId,
                         StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid repair-completion Luoyang passage transition cause.");
                _ = new StableId(repairOrderId);
                return;
            }

            throw new InvalidOperationException(
                "Unknown Luoyang passage transition cause " + causeId + ".");
        }

        private static bool CommandTargetsFacility(
            PersistentWorldCommandState command,
            string facilityId)
        {
            var arguments = ArgumentsById(command);
            return TryGet(arguments,
                       LuoyangPassageTraversalWorldContractIds.FacilityIdArgumentId,
                       out var target) &&
                   string.Equals(target, facilityId, StringComparison.Ordinal);
        }

        private static long ParseExpectedRevision(
            PersistentWorldCommandState command)
        {
            var arguments = ArgumentsById(command);
            if (!TryGet(arguments,
                    LuoyangPassageTraversalWorldContractIds
                        .ExpectedRevisionArgumentId,
                    out var text) ||
                !long.TryParse(text,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var revision) || revision < 0)
                throw new InvalidOperationException(
                    "Invalid expected revision on Luoyang passage command " +
                    command.Id + ".");
            return revision;
        }

        private static Dictionary<string, string> ArgumentsById(
            PersistentWorldCommandState command)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (command.Arguments == null)
                throw new InvalidOperationException(
                    "Luoyang passage command arguments cannot be null.");
            foreach (var argument in command.Arguments)
            {
                if (argument == null || argument.Value == null)
                    throw new InvalidOperationException(
                        "Luoyang passage command contains an invalid argument.");
                result.Add(argument.Key, argument.Value);
            }
            return result;
        }

        private static bool TryGet(
            IReadOnlyDictionary<string, string> arguments,
            string key,
            out string value) => arguments.TryGetValue(key, out value);
    }

    public static class LuoyangPassageOperationalRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.LuoyangPassageOperationalControls == null ||
                world.LuoyangPassageDamageRecords == null ||
                world.LuoyangPassageRepairOrders == null)
                throw new InvalidOperationException(
                    "Luoyang passage operational collections cannot be null.");

            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var control in world.LuoyangPassageOperationalControls)
            {
                if (control == null ||
                    !facilityIds.Add(new StableId(control.FacilityId).Value) ||
                    !string.Equals(control.Id,
                        LuoyangPassageOperationsContractIds.ControlId(
                            control.FacilityId), StringComparison.Ordinal) ||
                    control.GuardPersonIds == null ||
                    control.GuardPersonIds.Count == 0 ||
                    control.ActivatedPassageRevision < 0 ||
                    control.InitialConditionBasisPoints < 0 ||
                    control.InitialConditionBasisPoints > 10_000 ||
                    control.CurrentConditionBasisPoints < 0 ||
                    control.CurrentConditionBasisPoints > 10_000 ||
                    control.IntegrityRevision < 0 || control.AssignedDay < 0 ||
                    control.AssignedDay > world.AbsoluteDay ||
                    control.AssignedSegment > (byte)DaySegment.Night)
                    throw new InvalidOperationException(
                        "Invalid Luoyang passage operational control.");

                _ = new StableId(control.ControllerOrganizationId);
                _ = new StableId(control.GuardArmyId);
                _ = new StableId(control.GuardCommanderPersonId);
                _ = new StableId(control.AuthorizedByPersonId);
                _ = new StableId(control.AuthorityBasisId);
                var passage = FindPassage(world, control.FacilityId);
                var facility = FindFacility(world, control.FacilityId);
                var organization = FindOrganization(
                    world, control.ControllerOrganizationId);
                var army = FindArmy(world, control.GuardArmyId);
                if (passage.Revision < control.ActivatedPassageRevision ||
                    !string.Equals(facility.ControllerId, organization.Id,
                        StringComparison.Ordinal) &&
                    !string.Equals(facility.OwnerId, organization.Id,
                        StringComparison.Ordinal) ||
                    !string.Equals(army.OrganizationId, organization.Id,
                        StringComparison.Ordinal) ||
                    !string.Equals(army.CommanderPersonId,
                        control.GuardCommanderPersonId,
                        StringComparison.Ordinal) ||
                    facility.ConditionBasisPoints !=
                        control.CurrentConditionBasisPoints ||
                    !IsInitialStatusCompatible(
                        control.InitialTraversalStatusId,
                        control.InitialConditionBasisPoints) ||
                    !IsCurrentStatusCompatible(passage.TraversalStatusId,
                        control.CurrentConditionBasisPoints) ||
                    control.CurrentConditionBasisPoints == 0 &&
                        facility.LifecycleStatus !=
                            FacilityLifecycleStatus.Destroyed ||
                    control.CurrentConditionBasisPoints > 0 &&
                        facility.LifecycleStatus ==
                            FacilityLifecycleStatus.Destroyed)
                    throw new InvalidOperationException(
                        "Luoyang passage control drifted from its Facility, " +
                        "army or traversal state.");

                ValidateSortedGuardPeople(world, control);
                ValidateAuthorityBasis(control.AuthorityBasisId);
                ValidateCompletedCommandAndEvent(world,
                    control.AssignmentCommandId,
                    LuoyangPassageOperationsContractIds
                        .GuardAssignmentCommandTypeId,
                    control.AssignmentEventId,
                    LuoyangPassageOperationsContractIds.GuardAssignedEventTypeId,
                    LuoyangPassageOperationsContractIds.GuardTransactionId(
                        control.FacilityId));
                ValidateIntegrityChain(world, control);
            }

            foreach (var damage in world.LuoyangPassageDamageRecords)
            {
                if (damage == null || damage.DamageBasisPoints <= 0 ||
                    damage.DamageBasisPoints > 10_000 ||
                    damage.ConditionBeforeBasisPoints <= 0 ||
                    damage.ConditionBeforeBasisPoints > 10_000 ||
                    damage.ConditionAfterBasisPoints < 0 ||
                    damage.ConditionAfterBasisPoints >=
                        damage.ConditionBeforeBasisPoints ||
                    damage.ConditionAfterBasisPoints != Math.Max(0,
                        damage.ConditionBeforeBasisPoints -
                        damage.DamageBasisPoints) ||
                    damage.IntegrityRevision <= 0 ||
                    damage.PassageRevisionAfter !=
                        damage.PassageRevisionBefore + 1 ||
                    damage.Day < 0 || damage.Day > world.AbsoluteDay ||
                    damage.Segment > (byte)DaySegment.Night ||
                    !string.Equals(damage.Id,
                        LuoyangPassageOperationsContractIds.DamageRecordId(
                            damage.FacilityId, damage.IntegrityRevision),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Invalid Luoyang passage damage record.");
                _ = FindPassage(world, damage.FacilityId);
                var battle = world.Battles.FirstOrDefault(item => item != null &&
                    string.Equals(item.Id, damage.BattleRecordId,
                        StringComparison.Ordinal)) ??
                    throw new InvalidOperationException(
                        "Luoyang passage damage references a missing battle.");
                var attacker = FindArmy(world, damage.AttackerArmyId);
                var control = FindControl(world, damage.FacilityId);
                var facility = FindFacility(world, damage.FacilityId);
                if (!string.Equals(battle.AttackerArmyId, attacker.Id,
                        StringComparison.Ordinal) ||
                    !string.Equals(battle.DefenderArmyId,
                        control.GuardArmyId, StringComparison.Ordinal) ||
                    !string.Equals(battle.LocationId, facility.SettlementId,
                        StringComparison.Ordinal) ||
                    damage.Day < battle.Day ||
                    string.Equals(attacker.OrganizationId,
                        control.ControllerOrganizationId,
                        StringComparison.Ordinal) ||
                    !string.Equals(attacker.CommanderPersonId,
                        damage.AttackerCommanderPersonId,
                        StringComparison.Ordinal) ||
                    !string.Equals(damage.AuthorityBasisId,
                        LuoyangPassageOperationsContractIds
                            .AttackingArmyCommanderAuthorityId,
                        StringComparison.Ordinal) ||
                    world.LuoyangPassageDamageRecords.Count(item =>
                        item != null && string.Equals(item.FacilityId,
                            damage.FacilityId, StringComparison.Ordinal) &&
                        string.Equals(item.BattleRecordId,
                            damage.BattleRecordId,
                            StringComparison.Ordinal)) != 1)
                    throw new InvalidOperationException(
                        "Luoyang passage damage battle authority drifted.");
                ValidateCompletedCommandAndEvent(world, damage.CommandId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionCommandTypeId,
                    damage.EventId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionedEventTypeId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionTransactionId(damage.FacilityId,
                            damage.PassageRevisionAfter, damage.CommandId));
            }

            foreach (var order in world.LuoyangPassageRepairOrders)
                ValidateRepairOrder(world, order);
        }

        private static void ValidateIntegrityChain(
            WorldState world,
            LuoyangPassageOperationalControlState control)
        {
            var operations = new List<IntegrityOperation>();
            foreach (var damage in world.LuoyangPassageDamageRecords)
            {
                if (damage != null && string.Equals(damage.FacilityId,
                        control.FacilityId, StringComparison.Ordinal))
                    operations.Add(new IntegrityOperation(
                        damage.IntegrityRevision,
                        damage.ConditionBeforeBasisPoints,
                        damage.ConditionAfterBasisPoints,
                        damage.Id));
            }
            foreach (var repair in world.LuoyangPassageRepairOrders)
            {
                if (repair != null && repair.Status ==
                        LuoyangPassageRepairStatus.Completed &&
                    string.Equals(repair.FacilityId, control.FacilityId,
                        StringComparison.Ordinal))
                    operations.Add(new IntegrityOperation(
                        repair.SourceIntegrityRevision + 1,
                        FindDamage(world, repair.SourceDamageRecordId)
                            .ConditionAfterBasisPoints,
                        10_000,
                        repair.Id));
            }
            operations.Sort((left, right) => left.Revision.CompareTo(
                right.Revision));
            var condition = control.InitialConditionBasisPoints;
            long revision = 0;
            foreach (var operation in operations)
            {
                if (operation.Revision != revision + 1 ||
                    operation.Before != condition)
                    throw new InvalidOperationException(
                        "Luoyang passage integrity history is not contiguous.");
                revision = operation.Revision;
                condition = operation.After;
            }
            if (revision != control.IntegrityRevision ||
                condition != control.CurrentConditionBasisPoints)
                throw new InvalidOperationException(
                    "Luoyang passage integrity summary does not match history.");
            var lastDamage = operations.LastOrDefault(item =>
                item.Id.StartsWith("luoyang.passage.damage.",
                    StringComparison.Ordinal));
            var expectedLastDamageId = lastDamage?.Id ?? string.Empty;
            if (!string.Equals(control.LastDamageRecordId ?? string.Empty,
                    expectedLastDamageId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang passage last-damage reference is invalid.");
        }

        private static void ValidateRepairOrder(
            WorldState world,
            LuoyangPassageRepairOrderState order)
        {
            if (order == null || !Enum.IsDefined(
                    typeof(LuoyangPassageRepairStatus), order.Status) ||
                order.SourceIntegrityRevision <= 0 ||
                order.SourcePassageRevision < 0 || order.StartedDay < 0 ||
                order.StartedDay > world.AbsoluteDay)
                throw new InvalidOperationException(
                    "Invalid Luoyang passage repair order.");
            var control = FindControl(world, order.FacilityId);
            var project = world.FacilityConstructionProjects.FirstOrDefault(
                item => item != null && string.Equals(item.Id,
                    order.FacilityConstructionProjectId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage repair lacks its Facility project.");
            var sourceDamage = FindDamage(world, order.SourceDamageRecordId);
            if (!string.Equals(order.Id,
                    LuoyangPassageOperationsContractIds.RepairOrderId(
                        order.FacilityId, order.SourceIntegrityRevision),
                    StringComparison.Ordinal) ||
                !string.Equals(order.ControllerOrganizationId,
                    control.ControllerOrganizationId,
                    StringComparison.Ordinal) ||
                sourceDamage.IntegrityRevision !=
                    order.SourceIntegrityRevision ||
                !string.Equals(sourceDamage.FacilityId, order.FacilityId,
                    StringComparison.Ordinal) ||
                project.Kind != FacilityConstructionProjectKind.Repair ||
                !string.Equals(project.TargetFacilityId, order.FacilityId,
                    StringComparison.Ordinal) ||
                !string.Equals(project.SponsorPersonId, order.ManagerPersonId,
                    StringComparison.Ordinal) ||
                !string.Equals(project.MaterialInventoryContainerId,
                    order.MaterialInventoryContainerId,
                    StringComparison.Ordinal) ||
                project.MoneyCost !=
                    LuoyangPassageOperationsContractIds.RequiredMoney)
                throw new InvalidOperationException(
                    "Luoyang passage repair drifted from its source or project.");
            ValidateRepairProfile(project, order.ProfileId);
            ValidateConstructionAudit(world, project, order);
            ValidateAuthorityBasis(order.AuthorityBasisId);
            ValidateCompletedCommandAndEvent(world, order.StartCommandId,
                LuoyangPassageOperationsContractIds.RepairStartCommandTypeId,
                order.StartEventId,
                LuoyangPassageOperationsContractIds.RepairStartedEventTypeId,
                LuoyangPassageOperationsContractIds.RepairStartTransactionId(
                    order.Id));
            if (order.Status == LuoyangPassageRepairStatus.InProgress)
            {
                if (project.Status == FacilityConstructionStatus.Completed ||
                    !string.Equals(control.ActiveRepairOrderId, order.Id,
                        StringComparison.Ordinal) ||
                    order.CompletedDay != -1 ||
                    !string.IsNullOrEmpty(order.CompletionCommandId) ||
                    !string.IsNullOrEmpty(order.CompletionEventId))
                    throw new InvalidOperationException(
                        "Active Luoyang passage repair lifecycle is invalid.");
            }
            else if (project.Status != FacilityConstructionStatus.Completed ||
                     order.CompletedDay < order.StartedDay ||
                     order.CompletedDay != project.CompletedDay ||
                     !string.IsNullOrEmpty(control.ActiveRepairOrderId) ||
                     string.IsNullOrEmpty(order.CompletionCommandId) ||
                     string.IsNullOrEmpty(order.CompletionEventId))
                throw new InvalidOperationException(
                    "Completed Luoyang passage repair lacks completion evidence.");
            else
                ValidateCompletedCommandAndEvent(world,
                    order.CompletionCommandId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionCommandTypeId,
                    order.CompletionEventId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionedEventTypeId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionTransactionId(order.FacilityId,
                            checked(order.SourcePassageRevision + 1),
                            order.CompletionCommandId));
        }

        private static void ValidateRepairProfile(
            FacilityConstructionProjectState project,
            string profileId)
        {
            var bridge = string.Equals(profileId,
                LuoyangPassageOperationsContractIds.BridgeRepairProfileId,
                StringComparison.Ordinal);
            if (!bridge && !string.Equals(profileId,
                    LuoyangPassageOperationsContractIds.GateRepairProfileId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Unknown Luoyang passage repair profile.");
            var timber = project.Materials.Where(item => item != null &&
                    string.Equals(item.ProductDefinitionId,
                        CoreProductionContent.TimberMaterialProductId,
                        StringComparison.Ordinal))
                .Sum(item => item.ReservedQuantity);
            var iron = project.Materials.Where(item => item != null &&
                    string.Equals(item.ProductDefinitionId,
                        CoreProductionContent.IronMaterialProductId,
                        StringComparison.Ordinal))
                .Sum(item => item.ReservedQuantity);
            var requiredTimber = bridge
                ? LuoyangPassageOperationsContractIds.BridgeRequiredTimberUnits
                : LuoyangPassageOperationsContractIds.GateRequiredTimberUnits;
            var requiredLabor = bridge
                ? LuoyangPassageOperationsContractIds.BridgeRequiredLaborMinutes
                : LuoyangPassageOperationsContractIds.GateRequiredLaborMinutes;
            var requiredDays = bridge
                ? LuoyangPassageOperationsContractIds.BridgeMinimumDays
                : LuoyangPassageOperationsContractIds.GateMinimumDays;
            if (timber != requiredTimber || iron !=
                    LuoyangPassageOperationsContractIds.GateRequiredIronUnits ||
                project.Materials.Any(item => item == null ||
                    !string.Equals(item.ProductDefinitionId,
                         CoreProductionContent.TimberMaterialProductId,
                         StringComparison.Ordinal) &&
                    !string.Equals(item.ProductDefinitionId,
                         CoreProductionContent.IronMaterialProductId,
                         StringComparison.Ordinal)) ||
                project.RequiredLaborMinutes != requiredLabor ||
                project.EarliestCompletionDay !=
                    project.StartedDay + requiredDays)
                throw new InvalidOperationException(
                    "Luoyang passage repair profile quantities drifted.");
        }

        private static void ValidateConstructionAudit(
            WorldState world,
            FacilityConstructionProjectState project,
            LuoyangPassageRepairOrderState order)
        {
            var transactions = world.InventoryTransactions.Where(item =>
                    item != null && string.Equals(
                        item.SourceFacilityConstructionProjectId,
                        project.Id, StringComparison.Ordinal))
                .ToArray();
            if (transactions.Count(item => item.Type ==
                    InventoryTransactionType
                        .FacilityConstructionMaterialReserved) != 1 ||
                transactions.Count(item => item.Type ==
                    InventoryTransactionType
                        .FacilityConstructionMaterialConsumed) !=
                    (order.Status == LuoyangPassageRepairStatus.Completed
                        ? 1 : 0))
                throw new InvalidOperationException(
                    "Luoyang passage repair inventory audit is incomplete.");
            var labor = world.FacilityConstructionLabor.Where(item =>
                    item != null && string.Equals(item.ProjectId, project.Id,
                        StringComparison.Ordinal)).ToArray();
            if (labor.Sum(item => item.LaborMinutes) !=
                    project.CompletedLaborMinutes ||
                labor.GroupBy(item => item.WorkerPersonId + "|" + item.Day,
                        StringComparer.Ordinal).Any(group => group.Count() != 1) ||
                labor.Any(item => !world.People.Any(person => person != null &&
                    string.Equals(person.Id, item.WorkerPersonId,
                        StringComparison.Ordinal))))
                throw new InvalidOperationException(
                    "Luoyang passage repair labor audit is invalid.");
        }

        private static bool IsCurrentStatusCompatible(
            string statusId,
            int condition)
        {
            if (condition == 0)
                return string.Equals(statusId,
                    LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                    StringComparison.Ordinal);
            if (condition < 10_000)
                return string.Equals(statusId,
                    LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                    StringComparison.Ordinal);
            return string.Equals(statusId,
                       LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                       StringComparison.Ordinal) ||
                   string.Equals(statusId,
                       LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                       StringComparison.Ordinal);
        }

        private static bool IsInitialStatusCompatible(
            string statusId,
            int condition) => IsCurrentStatusCompatible(statusId, condition);

        private static void ValidateSortedGuardPeople(
            WorldState world,
            LuoyangPassageOperationalControlState control)
        {
            string previous = null;
            foreach (var personId in control.GuardPersonIds)
            {
                _ = new StableId(personId);
                if (previous != null && string.CompareOrdinal(previous,
                        personId) >= 0 ||
                    !world.People.Any(person => person != null &&
                        string.Equals(person.Id, personId,
                            StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        "Luoyang passage guard Person list is invalid.");
                previous = personId;
            }
            if (!control.GuardPersonIds.Contains(
                    control.GuardCommanderPersonId, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang passage commander is absent from the guard list.");
        }

        private static void ValidateAuthorityBasis(string authorityBasisId)
        {
            if (!string.Equals(authorityBasisId,
                    LuoyangPassageOperationsContractIds
                        .OrganizationLeaderAuthorityId,
                    StringComparison.Ordinal) &&
                !string.Equals(authorityBasisId,
                    LuoyangPassageOperationsContractIds
                        .GuardArmyCommanderAuthorityId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Unsupported Luoyang passage authority basis.");
        }

        private static void ValidateCompletedCommandAndEvent(
            WorldState world,
            string commandId,
            string commandTypeId,
            string eventId,
            string eventTypeId,
            string transactionId)
        {
            var command = world.PersistentWorldCommands.FirstOrDefault(item =>
                item != null && string.Equals(item.Id, commandId,
                    StringComparison.Ordinal));
            var worldEvent = world.WorldEventOutbox.FirstOrDefault(item =>
                item != null && string.Equals(item.Id, eventId,
                    StringComparison.Ordinal));
            if (command == null || command.Status !=
                    PersistentWorldCommandStatus.Completed ||
                !string.Equals(command.CommandTypeId, commandTypeId,
                    StringComparison.Ordinal) || worldEvent == null ||
                !string.Equals(worldEvent.EventTypeId, eventTypeId,
                    StringComparison.Ordinal) ||
                !string.Equals(worldEvent.SourceTransactionId, transactionId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Luoyang passage command/event provenance is incomplete.");
        }

        private static LuoyangPassageTraversalWorldState FindPassage(
            WorldState world,
            string facilityId) => world.LuoyangPassageTraversals.FirstOrDefault(
                item => item != null && string.Equals(item.FacilityId,
                    facilityId, StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Missing Luoyang passage traversal state.");

        private static LuoyangPassageOperationalControlState FindControl(
            WorldState world,
            string facilityId) =>
            world.LuoyangPassageOperationalControls.FirstOrDefault(item =>
                item != null && string.Equals(item.FacilityId, facilityId,
                    StringComparison.Ordinal)) ??
            throw new InvalidOperationException(
                "Missing Luoyang passage operational control.");

        private static FacilityState FindFacility(
            WorldState world,
            string facilityId) => world.Facilities.FirstOrDefault(item =>
                item != null && string.Equals(item.Id, facilityId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Missing Luoyang passage Facility.");

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId) => world.Organizations.FirstOrDefault(item =>
                item != null && string.Equals(item.Id, organizationId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Missing Luoyang passage controller organization.");

        private static ArmyState FindArmy(WorldState world, string armyId) =>
            world.Armies.FirstOrDefault(item => item != null && string.Equals(
                item.Id, armyId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException(
                "Missing Luoyang passage guard or attacker army.");

        private static LuoyangPassageDamageRecordState FindDamage(
            WorldState world,
            string damageId) => world.LuoyangPassageDamageRecords
                .FirstOrDefault(item => item != null && string.Equals(item.Id,
                    damageId, StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Missing Luoyang passage damage record.");

        private sealed class IntegrityOperation
        {
            public IntegrityOperation(
                long revision,
                int before,
                int after,
                string id)
            {
                Revision = revision;
                Before = before;
                After = after;
                Id = id;
            }

            public long Revision { get; }
            public int Before { get; }
            public int After { get; }
            public string Id { get; }
        }
    }
}
