using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class TravelSystem
    {
        private readonly PopulationLedgerSystem _populationLedgerSystem;
        private readonly IPersonRepository _people;

        public TravelSystem(IPersonRepository people = null)
        {
            _people = people;
            _populationLedgerSystem = new PopulationLedgerSystem(people);
        }

        public JourneyState StartJourney(
            WorldState world,
            StableId personId,
            StableId routeId,
            StableId destinationId,
            TravelMode mode)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var people = PeopleFor(world);
            var person = people.GetRequired(personId.Value);
            if (!person.IsAlive)
            {
                throw new InvalidOperationException("A deceased person cannot travel.");
            }

            if (FindJourneyByPerson(world, person.Id) != null)
            {
                throw new InvalidOperationException($"{person.Id} is already traveling.");
            }

            var route = FindRoute(world, routeId.Value);
            var forward = route.FromLocationId == person.LocationId &&
                route.ToLocationId == destinationId.Value;
            var backward = route.Bidirectional &&
                route.ToLocationId == person.LocationId &&
                route.FromLocationId == destinationId.Value;
            if (!forward && !backward)
            {
                throw new InvalidOperationException(
                    $"Route {route.Id} does not connect {person.LocationId} to {destinationId}.");
            }

            var journey = new JourneyState
            {
                Id = $"journey.{person.Id}.{world.Revision}",
                PersonId = person.Id,
                RouteId = route.Id,
                OriginLocationId = person.LocationId,
                DestinationLocationId = destinationId.Value,
                Mode = mode,
                RemainingKilometers = route.DistanceKilometers,
                StartedDay = world.AbsoluteDay,
                StartedSegment = world.Segment
            };
            world.Journeys.Add(journey);
            world.Validate();
            return journey;
        }

        public void AdvanceJourneysOneSegment(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var people = PeopleFor(world);
            for (var i = world.Journeys.Count - 1; i >= 0; i--)
            {
                var journey = world.Journeys[i];
                journey.RemainingKilometers -= KilometersPerSegment(journey.Mode);
                if (journey.RemainingKilometers > 0)
                {
                    continue;
                }

                var person = people.GetRequiredForUpdate(journey.PersonId);
                world.Journeys.RemoveAt(i);
                _populationLedgerSystem.MoveIndependentPerson(
                    world,
                    person,
                    journey.DestinationLocationId);
            }
        }

        public void ConsumeDailyTravelProvisions(WorldState world)
        {
            var people = PeopleFor(world);
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                var person = people.GetRequiredForUpdate(
                    world.Journeys[i].PersonId);
                if (person.Provisions > 0)
                {
                    person.Provisions--;
                }
                else
                {
                    person.Needs.Livelihood = Math.Min(
                        10_000,
                        person.Needs.Livelihood + 1_000);
                    person.HealthBasisPoints = Math.Max(
                        0,
                        person.HealthBasisPoints - 100);
                }
            }
        }

        public static int KilometersPerSegment(TravelMode mode)
        {
            switch (mode)
            {
                case TravelMode.Foot:
                    return 7;
                case TravelMode.Mounted:
                    return 16;
                case TravelMode.Caravan:
                    return 6;
                case TravelMode.MilitaryUnit:
                    return 5;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private IPersonRepository PeopleFor(WorldState world) =>
            _people ?? new WorldStatePersonRepository(world);

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

        private static JourneyState FindJourneyByPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return world.Journeys[i];
                }
            }

            return null;
        }
    }
}
