using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HistoricalEventSystem
    {
        public List<HistoricalAnchorRuntimeState> ResolveEligibleEvents(WorldState world)
        {
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
                if (anchor.Status == HistoricalAnchorStatus.Resolved ||
                    anchor.Status == HistoricalAnchorStatus.Prevented ||
                    anchor.Status == HistoricalAnchorStatus.Transformed)
                {
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
