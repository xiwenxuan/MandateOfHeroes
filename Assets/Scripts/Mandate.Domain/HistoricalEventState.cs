using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum HistoricalAnchorStatus : byte
    {
        Dormant,
        Eligible,
        Resolved,
        Prevented,
        Transformed,
        Watching,
        Ready,
        Triggered,
        Resolving,
        CompletedCanonical,
        Variant,
        Delayed,
        Expired
    }

    public enum HistoricalEventOutcomeKind : byte
    {
        Canonical,
        Variant,
        Delayed,
        Transformed,
        Prevented
    }

    public static class HistoricalConditionTypeIds
    {
        public const string WorldDayAtLeast =
            "mandate.historical_condition.world_day_at_least";
        public const string PersonAlive =
            "mandate.historical_condition.person_alive";
        public const string PersonAtLocation =
            "mandate.historical_condition.person_at_location";
        public const string OrganizationExists =
            "mandate.historical_condition.organization_exists";
        public const string FacilityOperational =
            "mandate.historical_condition.facility_operational";
        public const string RouteSecurityAtMost =
            "mandate.historical_condition.route_security_at_most";
        public const string ArmyExists =
            "mandate.historical_condition.army_exists";
        public const string OfficeHolderEquals =
            "mandate.historical_condition.office_holder_equals";
    }

    public static class HistoricalChangeOperationTypeIds
    {
        public const string DestroyFacility =
            "mandate.historical_change.destroy_facility";
        public const string MovePerson =
            "mandate.historical_change.move_person";
        public const string LoseFamilyCenter =
            "mandate.historical_change.lose_family_center";
        public const string RelocateOffice =
            "mandate.historical_change.relocate_office";
        public const string AdjustPublicOrder =
            "mandate.historical_change.adjust_public_order";
        public const string AdjustRouteSecurity =
            "mandate.historical_change.adjust_route_security";
        public const string SetArmyMobilized =
            "mandate.historical_change.set_army_mobilized";
    }

    public enum HistoricalEffectType : byte
    {
        AdjustPublicOrder,
        AdjustGrainPrice,
        SetWarPressure,
        AdjustRouteSecurity,
        SetTaskAvailability,
        SetArmyMobilized
    }

    [Serializable]
    public sealed class HistoricalEffectState
    {
        public HistoricalEffectType Type;
        public string TargetId;
        public int Value;
    }

    [Serializable]
    public sealed class HistoricalEventConditionState
    {
        public string Id;
        public string ConditionTypeId;
        public string TargetId;
        public string StringValue;
        public long NumericValue;
        public bool Negated;
    }

    [Serializable]
    public sealed class HistoricalChangeOperationState
    {
        public string Id;
        public string OperationTypeId;
        public string TargetId;
        public string StringValue;
        public long NumericValue;
    }

    [Serializable]
    public sealed class HistoricalEventOutcomeRuleState
    {
        public string Id;
        public HistoricalEventOutcomeKind Outcome;
        public int Priority;
        public string Summary;
        public List<HistoricalEventConditionState> Preconditions =
            new List<HistoricalEventConditionState>();
        public List<HistoricalChangeOperationState> ChangePackage =
            new List<HistoricalChangeOperationState>();
    }

    [Serializable]
    public sealed class HistoricalEventDefinitionState
    {
        public string Id;
        public string DisplayName;
        public long EarliestDay;
        public long LatestDay;
        public string PrerequisiteEventId;
        public string CanonicalOutcome;
        public bool RequiresStructuredPreconditions;
        public string ChangePackageVersion = "1";
        public List<HistoricalEventOutcomeRuleState> OutcomeRules =
            new List<HistoricalEventOutcomeRuleState>();
        public List<HistoricalEffectState> Effects = new List<HistoricalEffectState>();
    }

    [Serializable]
    public sealed class HistoricalAnchorRuntimeState
    {
        public string Id;
        public string DefinitionId;
        public HistoricalAnchorStatus Status;
        public long ResolvedDay = -1;
        public string ActualOutcome;
        public HistoricalEventOutcomeKind OutcomeKind;
        public string OutcomeRuleId;
        public string AppliedChangePackageVersion;
        public long LastEvaluatedDay = -1;
        public int EvaluationCount;
        public bool AppliedOffscreen;
        public List<string> AppliedChangeOperationIds = new List<string>();
        public List<string> FailedConditionIds = new List<string>();
        public List<string> CausalEventIds = new List<string>();
    }

    public static class HistoricalEventContractRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world.HistoricalEventDefinitions == null ||
                world.HistoricalAnchors == null)
            {
                throw new InvalidOperationException(
                    "Historical event collections cannot be null.");
            }

            var definitionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var definitionIndex = 0;
                 definitionIndex < world.HistoricalEventDefinitions.Count;
                 definitionIndex++)
            {
                var definition = world.HistoricalEventDefinitions[definitionIndex] ??
                    throw new InvalidOperationException(
                        "A historical event definition cannot be null.");
                _ = new StableId(definition.Id);
                if (!definitionIds.Add(definition.Id) ||
                    definition.EarliestDay < 0 ||
                    definition.LatestDay < definition.EarliestDay ||
                    definition.OutcomeRules == null ||
                    definition.Effects == null ||
                    definition.RequiresStructuredPreconditions &&
                    string.IsNullOrWhiteSpace(definition.ChangePackageVersion))
                {
                    throw new InvalidOperationException(
                        $"Invalid historical event definition {definition.Id}.");
                }

                var ruleIds = new HashSet<string>(StringComparer.Ordinal);
                var nestedIds = new HashSet<string>(StringComparer.Ordinal);
                for (var ruleIndex = 0;
                     ruleIndex < definition.OutcomeRules.Count;
                     ruleIndex++)
                {
                    var rule = definition.OutcomeRules[ruleIndex] ??
                        throw new InvalidOperationException(
                            "A historical outcome rule cannot be null.");
                    _ = new StableId(rule.Id);
                    if (!ruleIds.Add(rule.Id) ||
                        !Enum.IsDefined(
                            typeof(HistoricalEventOutcomeKind), rule.Outcome) ||
                        rule.Preconditions == null ||
                        rule.ChangePackage == null)
                    {
                        throw new InvalidOperationException(
                            $"Invalid historical event outcome rule {rule.Id}.");
                    }

                    var hasNonTimeCondition = false;
                    for (var conditionIndex = 0;
                         conditionIndex < rule.Preconditions.Count;
                         conditionIndex++)
                    {
                        var condition = rule.Preconditions[conditionIndex] ??
                            throw new InvalidOperationException(
                                "A historical event condition cannot be null.");
                        _ = new StableId(condition.Id);
                        _ = new StableId(condition.ConditionTypeId);
                        if (!nestedIds.Add(condition.Id))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate historical condition ID {condition.Id}.");
                        }
                        hasNonTimeCondition |= condition.ConditionTypeId !=
                            HistoricalConditionTypeIds.WorldDayAtLeast;
                    }
                    if (definition.RequiresStructuredPreconditions &&
                        !hasNonTimeCondition)
                    {
                        throw new InvalidOperationException(
                            $"Historical outcome rule {rule.Id} cannot be year-only.");
                    }

                    for (var operationIndex = 0;
                         operationIndex < rule.ChangePackage.Count;
                         operationIndex++)
                    {
                        var operation = rule.ChangePackage[operationIndex] ??
                            throw new InvalidOperationException(
                                "A historical change operation cannot be null.");
                        _ = new StableId(operation.Id);
                        _ = new StableId(operation.OperationTypeId);
                        if (!nestedIds.Add(operation.Id))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate historical change operation ID {operation.Id}.");
                        }
                    }
                }
            }

            var anchorIds = new HashSet<string>(StringComparer.Ordinal);
            var anchorDefinitions = new HashSet<string>(StringComparer.Ordinal);
            for (var anchorIndex = 0;
                 anchorIndex < world.HistoricalAnchors.Count;
                 anchorIndex++)
            {
                var anchor = world.HistoricalAnchors[anchorIndex] ??
                    throw new InvalidOperationException(
                        "A historical event runtime state cannot be null.");
                _ = new StableId(anchor.Id);
                _ = new StableId(anchor.DefinitionId);
                if (!anchorIds.Add(anchor.Id) ||
                    !anchorDefinitions.Add(anchor.DefinitionId) ||
                    !definitionIds.Contains(anchor.DefinitionId) ||
                    !Enum.IsDefined(typeof(HistoricalAnchorStatus), anchor.Status) ||
                    !Enum.IsDefined(
                        typeof(HistoricalEventOutcomeKind), anchor.OutcomeKind) ||
                    anchor.LastEvaluatedDay < -1 || anchor.EvaluationCount < 0 ||
                    anchor.AppliedChangeOperationIds == null ||
                    anchor.FailedConditionIds == null ||
                    anchor.CausalEventIds == null)
                {
                    throw new InvalidOperationException(
                        $"Invalid historical event runtime state {anchor.Id}.");
                }
            }
        }
    }
}
