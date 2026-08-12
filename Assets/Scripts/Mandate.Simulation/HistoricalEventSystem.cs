using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HistoricalEventSystem
    {
        public List<HistoricalAnchorRuntimeState> ResolveEligibleEvents(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var resolved = new List<HistoricalAnchorRuntimeState>();
            var definitions = new List<HistoricalEventDefinitionState>(
                world.HistoricalEventDefinitions);
            definitions.Sort((left, right) =>
            {
                var day = left.EarliestDay.CompareTo(right.EarliestDay);
                return day != 0 ? day : string.CompareOrdinal(left.Id, right.Id);
            });

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var anchor = FindOrCreateAnchor(world, definition.Id);
                if (IsTerminal(anchor.Status))
                {
                    continue;
                }

                anchor.LastEvaluatedDay = world.AbsoluteDay;
                anchor.EvaluationCount = checked(anchor.EvaluationCount + 1);

                if (definition.RequiresStructuredPreconditions ||
                    definition.OutcomeRules.Count > 0)
                {
                    ResolveStructured(world, definition, anchor, resolved);
                    continue;
                }

                if (world.AbsoluteDay > definition.LatestDay)
                {
                    anchor.Status = HistoricalAnchorStatus.Prevented;
                    anchor.ActualOutcome = "时间窗结束，条件未满足。";
                    anchor.ResolvedDay = world.AbsoluteDay;
                    continue;
                }

                if (world.AbsoluteDay < definition.EarliestDay ||
                    !PrerequisiteResolved(world, definition.PrerequisiteEventId))
                {
                    continue;
                }

                anchor.Status = HistoricalAnchorStatus.Eligible;
                ApplyEffects(world, definition);
                anchor.Status = HistoricalAnchorStatus.Resolved;
                anchor.ResolvedDay = world.AbsoluteDay;
                anchor.ActualOutcome = definition.CanonicalOutcome;
                if (!string.IsNullOrEmpty(definition.PrerequisiteEventId))
                {
                    anchor.CausalEventIds.Add(definition.PrerequisiteEventId);
                }

                resolved.Add(anchor);
            }

            return resolved;
        }

        private static void ResolveStructured(
            WorldState world,
            HistoricalEventDefinitionState definition,
            HistoricalAnchorRuntimeState anchor,
            List<HistoricalAnchorRuntimeState> resolved)
        {
            if (world.AbsoluteDay < definition.EarliestDay)
            {
                anchor.Status = HistoricalAnchorStatus.Watching;
                return;
            }

            if (!PrerequisiteResolved(world, definition.PrerequisiteEventId))
            {
                anchor.Status = HistoricalAnchorStatus.Delayed;
                anchor.OutcomeKind = HistoricalEventOutcomeKind.Delayed;
                anchor.ActualOutcome = "prerequisite_not_resolved";
                return;
            }

            var rules = new List<HistoricalEventOutcomeRuleState>(
                definition.OutcomeRules);
            rules.Sort((left, right) =>
            {
                var byPriority = right.Priority.CompareTo(left.Priority);
                return byPriority != 0
                    ? byPriority
                    : string.CompareOrdinal(left.Id, right.Id);
            });

            HistoricalEventOutcomeRuleState selected = null;
            var failed = new List<string>();
            for (var ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                var rule = rules[ruleIndex];
                if (!HasNonTimeCondition(rule))
                {
                    failed.Add(rule.Id + ":year_only_rule_forbidden");
                    continue;
                }

                var evaluation = HistoricalEventPreconditionEvaluator.Evaluate(
                    world, rule.Preconditions);
                if (evaluation.Satisfied)
                {
                    selected = rule;
                    break;
                }

                failed.AddRange(evaluation.FailedConditionIds);
            }

            anchor.FailedConditionIds = failed;
            if (selected == null)
            {
                if (world.AbsoluteDay <= definition.LatestDay)
                {
                    anchor.Status = HistoricalAnchorStatus.Delayed;
                    anchor.OutcomeKind = HistoricalEventOutcomeKind.Delayed;
                    anchor.ActualOutcome = "conditions_not_yet_satisfied";
                }
                else
                {
                    anchor.Status = HistoricalAnchorStatus.Expired;
                    anchor.OutcomeKind = HistoricalEventOutcomeKind.Prevented;
                    anchor.ActualOutcome = "event_window_expired";
                    anchor.ResolvedDay = world.AbsoluteDay;
                    resolved.Add(anchor);
                }
                return;
            }

            if (selected.Outcome == HistoricalEventOutcomeKind.Delayed)
            {
                anchor.Status = HistoricalAnchorStatus.Delayed;
                anchor.OutcomeKind = HistoricalEventOutcomeKind.Delayed;
                anchor.OutcomeRuleId = selected.Id;
                anchor.ActualOutcome = selected.Summary;
                return;
            }

            anchor.Status = HistoricalAnchorStatus.Ready;
            anchor.Status = HistoricalAnchorStatus.Triggered;
            anchor.Status = HistoricalAnchorStatus.Resolving;
            HistoricalChangePackageExecutor.Apply(
                world,
                definition.ChangePackageVersion,
                selected.ChangePackage,
                anchor);
            anchor.Status = ToTerminalStatus(selected.Outcome);
            anchor.OutcomeKind = selected.Outcome;
            anchor.OutcomeRuleId = selected.Id;
            anchor.ActualOutcome = selected.Summary;
            anchor.ResolvedDay = world.AbsoluteDay;
            anchor.AppliedOffscreen = IsOffscreen(world, selected);
            if (!string.IsNullOrEmpty(definition.PrerequisiteEventId) &&
                !anchor.CausalEventIds.Contains(definition.PrerequisiteEventId))
            {
                anchor.CausalEventIds.Add(definition.PrerequisiteEventId);
            }
            resolved.Add(anchor);
        }

        private static bool HasNonTimeCondition(
            HistoricalEventOutcomeRuleState rule)
        {
            if (rule == null || rule.Preconditions == null)
            {
                return false;
            }

            for (var i = 0; i < rule.Preconditions.Count; i++)
            {
                if (rule.Preconditions[i].ConditionTypeId !=
                    HistoricalConditionTypeIds.WorldDayAtLeast)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsTerminal(HistoricalAnchorStatus status) =>
            status == HistoricalAnchorStatus.Resolved ||
            status == HistoricalAnchorStatus.Prevented ||
            status == HistoricalAnchorStatus.Transformed ||
            status == HistoricalAnchorStatus.CompletedCanonical ||
            status == HistoricalAnchorStatus.Variant ||
            status == HistoricalAnchorStatus.Expired;

        private static HistoricalAnchorStatus ToTerminalStatus(
            HistoricalEventOutcomeKind outcome)
        {
            switch (outcome)
            {
                case HistoricalEventOutcomeKind.Canonical:
                    return HistoricalAnchorStatus.CompletedCanonical;
                case HistoricalEventOutcomeKind.Variant:
                    return HistoricalAnchorStatus.Variant;
                case HistoricalEventOutcomeKind.Transformed:
                    return HistoricalAnchorStatus.Transformed;
                case HistoricalEventOutcomeKind.Prevented:
                    return HistoricalAnchorStatus.Prevented;
                default:
                    throw new InvalidOperationException(
                        "A delayed outcome cannot be terminal.");
            }
        }

        private static bool IsOffscreen(
            WorldState world,
            HistoricalEventOutcomeRuleState rule)
        {
            if (string.IsNullOrEmpty(world.PlayerPersonId))
            {
                return true;
            }

            var player = world.People.Find(item => item.Id == world.PlayerPersonId);
            if (player == null)
            {
                return true;
            }

            for (var i = 0; i < rule.Preconditions.Count; i++)
            {
                var condition = rule.Preconditions[i];
                if (condition.ConditionTypeId ==
                        HistoricalConditionTypeIds.PersonAtLocation &&
                    condition.StringValue == player.LocationId)
                {
                    return false;
                }
            }
            return true;
        }

        private static void ApplyEffects(
            WorldState world,
            HistoricalEventDefinitionState definition)
        {
            for (var i = 0; i < definition.Effects.Count; i++)
            {
                var effect = definition.Effects[i];
                switch (effect.Type)
                {
                    case HistoricalEffectType.AdjustPublicOrder:
                    {
                        var location = FindLocation(world, effect.TargetId);
                        location.PublicOrderBasisPoints = Clamp(
                            location.PublicOrderBasisPoints + effect.Value,
                            0,
                            10_000);
                        break;
                    }
                    case HistoricalEffectType.AdjustGrainPrice:
                    {
                        var location = FindLocation(world, effect.TargetId);
                        location.GrainPrice = Math.Max(1, location.GrainPrice + effect.Value);
                        var listing = FindMarketListing(
                            world, effect.TargetId, "commodity.grain");
                        if (listing != null)
                        {
                            listing.Price = location.GrainPrice;
                        }
                        break;
                    }
                    case HistoricalEffectType.SetWarPressure:
                    {
                        var person = FindPerson(world, effect.TargetId);
                        person.Needs.WarPressure = Clamp(effect.Value, 0, 10_000);
                        break;
                    }
                    case HistoricalEffectType.AdjustRouteSecurity:
                    {
                        var route = FindRoute(world, effect.TargetId);
                        route.SecurityBasisPoints = Clamp(
                            route.SecurityBasisPoints + effect.Value,
                            0,
                            10_000);
                        break;
                    }
                    case HistoricalEffectType.SetTaskAvailability:
                    {
                        var task = FindTaskDefinition(world, effect.TargetId);
                        task.IsAvailable = effect.Value != 0;
                        break;
                    }
                    case HistoricalEffectType.SetArmyMobilized:
                    {
                        var army = FindArmy(world, effect.TargetId);
                        army.IsMobilized = effect.Value != 0 && army.Troops > 0;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static bool PrerequisiteResolved(WorldState world, string prerequisiteId)
        {
            if (string.IsNullOrEmpty(prerequisiteId))
            {
                return true;
            }

            for (var i = 0; i < world.HistoricalAnchors.Count; i++)
            {
                var anchor = world.HistoricalAnchors[i];
                if (anchor.DefinitionId == prerequisiteId &&
                    anchor.Status == HistoricalAnchorStatus.Resolved)
                {
                    return true;
                }
            }

            return false;
        }

        private static HistoricalAnchorRuntimeState FindOrCreateAnchor(
            WorldState world,
            string definitionId)
        {
            for (var i = 0; i < world.HistoricalAnchors.Count; i++)
            {
                if (world.HistoricalAnchors[i].DefinitionId == definitionId)
                {
                    return world.HistoricalAnchors[i];
                }
            }

            var anchor = new HistoricalAnchorRuntimeState
            {
                Id = "historical_anchor." + definitionId,
                DefinitionId = definitionId,
                Status = HistoricalAnchorStatus.Dormant
            };
            world.HistoricalAnchors.Add(anchor);
            return anchor;
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

            throw new InvalidOperationException($"Missing location {id}.");
        }

        private static PersonState FindPerson(WorldState world, string id)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == id)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {id}.");
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

        private static TaskDefinitionState FindTaskDefinition(WorldState world, string id)
        {
            for (var i = 0; i < world.TaskDefinitions.Count; i++)
            {
                if (world.TaskDefinitions[i].Id == id)
                {
                    return world.TaskDefinitions[i];
                }
            }

            throw new InvalidOperationException($"Missing task definition {id}.");
        }

        private static MarketListingState FindMarketListing(
            WorldState world,
            string locationId,
            string commodityId)
        {
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == locationId &&
                    listing.CommodityId == commodityId)
                {
                    return listing;
                }
            }

            return null;
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

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
