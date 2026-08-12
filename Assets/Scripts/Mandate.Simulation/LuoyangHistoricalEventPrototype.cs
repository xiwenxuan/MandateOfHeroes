using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class Luoyang189190PrototypeBindings
    {
        public string EmperorPersonId;
        public string LuoyangLocationId;
        public string AlternateLocationId;
        public string GovernmentOrganizationId;
        public string PalaceFacilityId;
        public string DestroyedFacilityId;
        public string FamilyCenterId;
        public string OfficeAssignmentId;
        public string RelocatedOfficeFacilityId;
        public string RouteId;
        public string ArmyId;
        public long EarliestDay;
        public long LatestDay;
    }

    public static class Luoyang189190HistoricalEventPrototype
    {
        public const string DefinitionId =
            "mandate.event.luoyang_189_190.contract_prototype";

        public static HistoricalEventDefinitionState Create(
            Luoyang189190PrototypeBindings bindings)
        {
            if (bindings == null ||
                string.IsNullOrWhiteSpace(bindings.EmperorPersonId) ||
                string.IsNullOrWhiteSpace(bindings.LuoyangLocationId) ||
                string.IsNullOrWhiteSpace(bindings.GovernmentOrganizationId) ||
                string.IsNullOrWhiteSpace(bindings.PalaceFacilityId) ||
                bindings.EarliestDay < 0 ||
                bindings.LatestDay < bindings.EarliestDay)
            {
                throw new InvalidOperationException(
                    "Luoyang 189/190 prototype bindings are incomplete.");
            }

            var definition = new HistoricalEventDefinitionState
            {
                Id = DefinitionId,
                DisplayName = "Luoyang 189/190 conditional event contract prototype",
                EarliestDay = bindings.EarliestDay,
                LatestDay = bindings.LatestDay,
                CanonicalOutcome = "canonical",
                RequiresStructuredPreconditions = true,
                ChangePackageVersion = "luoyang_189_190.prototype.v1"
            };

            definition.OutcomeRules.Add(CreateCanonical(bindings));
            definition.OutcomeRules.Add(CreateTransformed(bindings));
            definition.OutcomeRules.Add(CreateVariant(bindings));
            definition.OutcomeRules.Add(CreatePrevented(bindings));
            return definition;
        }

        private static HistoricalEventOutcomeRuleState CreateCanonical(
            Luoyang189190PrototypeBindings bindings)
        {
            var rule = NewRule(
                "canonical", HistoricalEventOutcomeKind.Canonical, 400,
                "The canonical structural shock occurs because its real actors, government and palace remain in place.");
            AddTimeAndOrganization(rule, bindings);
            rule.Preconditions.Add(Condition(rule,
                "emperor_alive", HistoricalConditionTypeIds.PersonAlive,
                bindings.EmperorPersonId));
            rule.Preconditions.Add(Condition(rule,
                "emperor_in_luoyang", HistoricalConditionTypeIds.PersonAtLocation,
                bindings.EmperorPersonId, bindings.LuoyangLocationId));
            rule.Preconditions.Add(Condition(rule,
                "palace_operational", HistoricalConditionTypeIds.FacilityOperational,
                bindings.PalaceFacilityId));
            AddCommonChanges(rule, bindings, true);
            return rule;
        }

        private static HistoricalEventOutcomeRuleState CreateTransformed(
            Luoyang189190PrototypeBindings bindings)
        {
            var rule = NewRule(
                "transformed", HistoricalEventOutcomeKind.Transformed, 300,
                "The political shock is transformed because the emperor is alive but no longer in Luoyang.");
            AddTimeAndOrganization(rule, bindings);
            rule.Preconditions.Add(Condition(rule,
                "emperor_alive", HistoricalConditionTypeIds.PersonAlive,
                bindings.EmperorPersonId));
            rule.Preconditions.Add(Condition(rule,
                "emperor_not_in_luoyang", HistoricalConditionTypeIds.PersonAtLocation,
                bindings.EmperorPersonId, bindings.LuoyangLocationId, true));
            AddCommonChanges(rule, bindings, false);
            return rule;
        }

        private static HistoricalEventOutcomeRuleState CreateVariant(
            Luoyang189190PrototypeBindings bindings)
        {
            var rule = NewRule(
                "variant", HistoricalEventOutcomeKind.Variant, 200,
                "A variant political crisis occurs because the canonical emperor actor is no longer alive.");
            AddTimeAndOrganization(rule, bindings);
            rule.Preconditions.Add(Condition(rule,
                "emperor_not_alive", HistoricalConditionTypeIds.PersonAlive,
                bindings.EmperorPersonId, null, true));
            AddCommonChanges(rule, bindings, false);
            return rule;
        }

        private static HistoricalEventOutcomeRuleState CreatePrevented(
            Luoyang189190PrototypeBindings bindings)
        {
            var rule = NewRule(
                "prevented", HistoricalEventOutcomeKind.Prevented, 100,
                "The canonical Luoyang shock is prevented because its palace complex is already unavailable.");
            AddTimeAndOrganization(rule, bindings);
            rule.Preconditions.Add(Condition(rule,
                "palace_not_operational", HistoricalConditionTypeIds.FacilityOperational,
                bindings.PalaceFacilityId, null, true));
            return rule;
        }

        private static HistoricalEventOutcomeRuleState NewRule(
            string suffix,
            HistoricalEventOutcomeKind outcome,
            int priority,
            string summary) =>
            new HistoricalEventOutcomeRuleState
            {
                Id = DefinitionId + ".outcome." + suffix,
                Outcome = outcome,
                Priority = priority,
                Summary = summary
            };

        private static void AddTimeAndOrganization(
            HistoricalEventOutcomeRuleState rule,
            Luoyang189190PrototypeBindings bindings)
        {
            rule.Preconditions.Add(new HistoricalEventConditionState
            {
                Id = rule.Id + ".condition.time",
                ConditionTypeId = HistoricalConditionTypeIds.WorldDayAtLeast,
                NumericValue = bindings.EarliestDay
            });
            rule.Preconditions.Add(Condition(rule,
                "government_exists",
                HistoricalConditionTypeIds.OrganizationExists,
                bindings.GovernmentOrganizationId));
        }

        private static HistoricalEventConditionState Condition(
            HistoricalEventOutcomeRuleState rule,
            string suffix,
            string type,
            string target,
            string value = null,
            bool negated = false) =>
            new HistoricalEventConditionState
            {
                Id = rule.Id + ".condition." + suffix,
                ConditionTypeId = type,
                TargetId = target,
                StringValue = value ?? string.Empty,
                Negated = negated
            };

        private static void AddCommonChanges(
            HistoricalEventOutcomeRuleState rule,
            Luoyang189190PrototypeBindings bindings,
            bool canonical)
        {
            if (!string.IsNullOrEmpty(bindings.DestroyedFacilityId))
            {
                rule.ChangePackage.Add(Operation(
                    rule, "destroy_facility",
                    HistoricalChangeOperationTypeIds.DestroyFacility,
                    bindings.DestroyedFacilityId));
            }
            if (!canonical &&
                !string.IsNullOrEmpty(bindings.AlternateLocationId))
            {
                rule.ChangePackage.Add(Operation(
                    rule, "move_emperor",
                    HistoricalChangeOperationTypeIds.MovePerson,
                    bindings.EmperorPersonId,
                    bindings.AlternateLocationId));
            }
            if (!string.IsNullOrEmpty(bindings.FamilyCenterId))
            {
                rule.ChangePackage.Add(Operation(
                    rule, "lose_family_center",
                    HistoricalChangeOperationTypeIds.LoseFamilyCenter,
                    bindings.FamilyCenterId));
            }
            if (!string.IsNullOrEmpty(bindings.OfficeAssignmentId))
            {
                rule.ChangePackage.Add(Operation(
                    rule, "relocate_office",
                    HistoricalChangeOperationTypeIds.RelocateOffice,
                    bindings.OfficeAssignmentId,
                    bindings.RelocatedOfficeFacilityId ?? string.Empty));
            }
            if (!string.IsNullOrEmpty(bindings.RouteId))
            {
                var route = Operation(
                    rule, "route_security",
                    HistoricalChangeOperationTypeIds.AdjustRouteSecurity,
                    bindings.RouteId);
                route.NumericValue = canonical ? -2_000 : -750;
                rule.ChangePackage.Add(route);
            }
            if (!string.IsNullOrEmpty(bindings.ArmyId))
            {
                var army = Operation(
                    rule, "mobilize_army",
                    HistoricalChangeOperationTypeIds.SetArmyMobilized,
                    bindings.ArmyId);
                army.NumericValue = 1;
                rule.ChangePackage.Add(army);
            }
        }

        private static HistoricalChangeOperationState Operation(
            HistoricalEventOutcomeRuleState rule,
            string suffix,
            string type,
            string target,
            string value = null) =>
            new HistoricalChangeOperationState
            {
                Id = rule.Id + ".change." + suffix,
                OperationTypeId = type,
                TargetId = target,
                StringValue = value ?? string.Empty
            };
    }
}
