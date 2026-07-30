using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class ArmySystem
    {
        private const int KilometersPerSegment = 5;

        public ArmyMarchState StartMarch(
            WorldState world,
            StableId armyId,
            StableId routeId,
            StableId destinationId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var army = FindArmy(world, armyId.Value);
            if (!army.IsMobilized || army.Troops <= 0)
            {
                throw new InvalidOperationException(
                    "Only a mobilized army with troops can march.");
            }

            if (FindMarch(world, army.Id) != null)
            {
                throw new InvalidOperationException($"{army.Id} is already marching.");
            }

            var route = FindRoute(world, routeId.Value);
            var forward =
                route.FromLocationId == army.LocationId &&
                route.ToLocationId == destinationId.Value;
            var backward =
                route.Bidirectional &&
                route.ToLocationId == army.LocationId &&
                route.FromLocationId == destinationId.Value;
            if (!forward && !backward)
            {
                throw new InvalidOperationException(
                    $"Route {route.Id} does not connect the army to its destination.");
            }

            var march = new ArmyMarchState
            {
                Id = $"army_march.{army.Id}.{world.Revision}",
                ArmyId = army.Id,
                RouteId = route.Id,
                OriginLocationId = army.LocationId,
                DestinationLocationId = destinationId.Value,
                RemainingKilometers = route.DistanceKilometers,
                StartedDay = world.AbsoluteDay
            };
            world.ArmyMarches.Add(march);
            world.Validate();
            return march;
        }

        public void AdvanceMarchesOneSegment(WorldState world)
        {
            for (var i = world.ArmyMarches.Count - 1; i >= 0; i--)
            {
                var march = world.ArmyMarches[i];
                march.RemainingKilometers -= KilometersPerSegment;
                if (march.RemainingKilometers > 0)
                {
                    continue;
                }

                var army = FindArmy(world, march.ArmyId);
                army.LocationId = march.DestinationLocationId;
                world.ArmyMarches.RemoveAt(i);
            }
        }

        public void ConsumeDailyMarchSupplies(WorldState world)
        {
            for (var i = world.ArmyMarches.Count - 1; i >= 0; i--)
            {
                var army = FindArmy(world, world.ArmyMarches[i].ArmyId);
                var required = Math.Max(1, army.Troops / 100);
                if (army.Provisions >= required)
                {
                    army.Provisions -= required;
                    continue;
                }

                army.Provisions = 0;
                army.MoraleBasisPoints = Math.Max(
                    0, army.MoraleBasisPoints - 150);
                var deserters = Math.Max(1, army.Troops / 500);
                army.Troops = Math.Max(0, army.Troops - deserters);
                if (army.Troops == 0)
                {
                    army.IsMobilized = false;
                    world.ArmyMarches.RemoveAt(i);
                }
            }
        }

        private static ArmyState FindArmy(WorldState world, string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }

        private static ArmyMarchState FindMarch(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return world.ArmyMarches[i];
                }
            }

            return null;
        }

        private static RouteState FindRoute(WorldState world, string routeId)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == routeId)
                {
                    return world.Routes[i];
                }
            }

            throw new InvalidOperationException($"Missing route {routeId}.");
        }
    }
}
