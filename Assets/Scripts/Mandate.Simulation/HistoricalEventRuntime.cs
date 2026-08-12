using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HistoricalConditionEvaluation
    {
        public bool Satisfied;
        public List<string> FailedConditionIds = new List<string>();
    }

    public static class HistoricalEventPreconditionEvaluator
    {
        public static HistoricalConditionEvaluation Evaluate(
            WorldState world,
            IReadOnlyList<HistoricalEventConditionState> conditions)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var result = new HistoricalConditionEvaluation { Satisfied = true };
            if (conditions == null || conditions.Count == 0)
            {
                result.Satisfied = false;
                result.FailedConditionIds.Add("missing_structured_preconditions");
                return result;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i] ??
                    throw new InvalidOperationException(
                        "A historical event condition cannot be null.");
                _ = new StableId(condition.Id);
                _ = new StableId(condition.ConditionTypeId);
                var satisfied = EvaluateOne(world, condition);
                if (condition.Negated)
                {
                    satisfied = !satisfied;
                }
                if (!satisfied)
                {
                    result.Satisfied = false;
                    result.FailedConditionIds.Add(condition.Id);
                }
            }
            return result;
        }

        private static bool EvaluateOne(
            WorldState world,
            HistoricalEventConditionState condition)
        {
            switch (condition.ConditionTypeId)
            {
                case HistoricalConditionTypeIds.WorldDayAtLeast:
                    return world.AbsoluteDay >= condition.NumericValue;
                case HistoricalConditionTypeIds.PersonAlive:
                {
                    var person = world.People.Find(item =>
                        item.Id == condition.TargetId);
                    return person != null && person.IsAlive;
                }
                case HistoricalConditionTypeIds.PersonAtLocation:
                {
                    var person = world.People.Find(item =>
                        item.Id == condition.TargetId);
                    return person != null && person.IsAlive &&
                        person.LocationId == condition.StringValue;
                }
                case HistoricalConditionTypeIds.OrganizationExists:
                    return world.Organizations.Exists(item =>
                        item.Id == condition.TargetId);
                case HistoricalConditionTypeIds.FacilityOperational:
                {
                    var facility = world.Facilities.Find(item =>
                        item.Id == condition.TargetId);
                    return facility != null &&
                        facility.LifecycleStatus ==
                            FacilityLifecycleStatus.Operational;
                }
                case HistoricalConditionTypeIds.RouteSecurityAtMost:
                {
                    var route = world.Routes.Find(item =>
                        item.Id == condition.TargetId);
                    return route != null &&
                        route.SecurityBasisPoints <= condition.NumericValue;
                }
                case HistoricalConditionTypeIds.ArmyExists:
                    return world.Armies.Exists(item =>
                        item.Id == condition.TargetId && item.Troops > 0);
                case HistoricalConditionTypeIds.OfficeHolderEquals:
                    return world.CivilMilitaryOfficeAssignments.Exists(item =>
                        item.OfficeDefinitionId == condition.TargetId &&
                        item.IsActive &&
                        item.HolderPersonId == condition.StringValue);
                default:
                    throw new InvalidOperationException(
                        $"Unknown historical condition type {condition.ConditionTypeId}.");
            }
        }
    }

    public static class HistoricalChangePackageExecutor
    {
        public static void Apply(
            WorldState world,
            string version,
            IReadOnlyList<HistoricalChangeOperationState> operations,
            HistoricalAnchorRuntimeState anchor)
        {
            if (world == null || anchor == null)
            {
                throw new ArgumentNullException(
                    world == null ? nameof(world) : nameof(anchor));
            }
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new InvalidOperationException(
                    "A historical change package requires a version.");
            }

            anchor.AppliedChangeOperationIds ??= new List<string>();
            if (operations != null)
            {
                for (var i = 0; i < operations.Count; i++)
                {
                    var operation = operations[i] ??
                        throw new InvalidOperationException(
                            "A historical change operation cannot be null.");
                    _ = new StableId(operation.Id);
                    _ = new StableId(operation.OperationTypeId);
                    if (anchor.AppliedChangeOperationIds.Contains(operation.Id))
                    {
                        continue;
                    }
                    ApplyOne(world, operation);
                    anchor.AppliedChangeOperationIds.Add(operation.Id);
                }
            }
            anchor.AppliedChangePackageVersion = version;
        }

        private static void ApplyOne(
            WorldState world,
            HistoricalChangeOperationState operation)
        {
            switch (operation.OperationTypeId)
            {
                case HistoricalChangeOperationTypeIds.DestroyFacility:
                {
                    var facility = world.Facilities.Find(item =>
                        item.Id == operation.TargetId);
                    if (facility != null)
                    {
                        facility.LifecycleStatus =
                            FacilityLifecycleStatus.Destroyed;
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.MovePerson:
                {
                    var person = world.People.Find(item =>
                        item.Id == operation.TargetId);
                    if (person != null && person.IsAlive &&
                        world.Locations.Exists(item =>
                            item.Id == operation.StringValue))
                    {
                        person.LocationId = operation.StringValue;
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.LoseFamilyCenter:
                {
                    var center = world.FamilyCenters.Find(item =>
                        item.Id == operation.TargetId);
                    if (center != null)
                    {
                        center.Status = FamilyCenterOperationalStatus.Lost;
                        center.Designation = FamilyCenterDesignation.None;
                        center.ReadinessReason = "historical_change_package";
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.RelocateOffice:
                {
                    var assignment = world.CivilMilitaryOfficeAssignments.Find(
                        item => item.Id == operation.TargetId);
                    if (assignment != null &&
                        (string.IsNullOrEmpty(operation.StringValue) ||
                         world.Facilities.Exists(item =>
                             item.Id == operation.StringValue)))
                    {
                        assignment.WorkplaceFacilityId = operation.StringValue;
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.AdjustPublicOrder:
                {
                    var location = world.Locations.Find(item =>
                        item.Id == operation.TargetId);
                    if (location != null)
                    {
                        location.PublicOrderBasisPoints = Clamp(
                            checked(location.PublicOrderBasisPoints +
                                (int)operation.NumericValue));
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.AdjustRouteSecurity:
                {
                    var route = world.Routes.Find(item =>
                        item.Id == operation.TargetId);
                    if (route != null)
                    {
                        route.SecurityBasisPoints = Clamp(
                            checked(route.SecurityBasisPoints +
                                (int)operation.NumericValue));
                    }
                    return;
                }
                case HistoricalChangeOperationTypeIds.SetArmyMobilized:
                {
                    var army = world.Armies.Find(item =>
                        item.Id == operation.TargetId);
                    if (army != null)
                    {
                        army.IsMobilized = operation.NumericValue != 0 &&
                            army.Troops > 0;
                    }
                    return;
                }
                default:
                    throw new InvalidOperationException(
                        $"Unknown historical change operation {operation.OperationTypeId}.");
            }
        }

        private static int Clamp(int value) =>
            Math.Max(0, Math.Min(10_000, value));
    }
}
