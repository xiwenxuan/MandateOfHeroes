using System;
using System.Collections.Generic;
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
            if (MilitaryMedicalEvacuationSystem.IsPersonInEvacuation(
                    world, personId.Value))
            {
                throw new InvalidOperationException(
                    "A person assigned to medical evacuation can only move through that evacuation workflow.");
            }
            var journey = StartJourneyWithoutValidation(
                world, personId, routeId, destinationId, mode);
            world.Validate();
            return journey;
        }

        internal JourneyState StartJourneyWithoutValidation(
            WorldState world,
            StableId personId,
            StableId routeId,
            StableId destinationId,
            TravelMode mode)
        {
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
            return journey;
        }

        public void AdvanceJourneysOneSegment(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var people = PeopleFor(world);
            var completedAnyJourney = false;
            for (var i = world.Journeys.Count - 1; i >= 0; i--)
            {
                var journey = world.Journeys[i];
                journey.RemainingKilometers -= KilometersPerSegment(journey.Mode);
                if (journey.RemainingKilometers > 0)
                {
                    continue;
                }

                var person = people.GetRequiredForUpdate(journey.PersonId);
                for (var orderIndex = 0;
                     orderIndex < world.MilitaryProcurementOrders.Count;
                     orderIndex++)
                {
                    var order = world.MilitaryProcurementOrders[orderIndex];
                    if (order.JourneyId == journey.Id &&
                        order.Status ==
                        MilitaryProcurementStatus.InTransit)
                    {
                        order.Status =
                            MilitaryProcurementStatus.AwaitingArmy;
                    }
                }
                for (var orderIndex = 0;
                     orderIndex < world.MilitaryLogisticsOrders.Count;
                     orderIndex++)
                {
                    var order = world.MilitaryLogisticsOrders[orderIndex];
                    if (order.JourneyId == journey.Id &&
                        order.Status == MilitaryLogisticsStatus.InTransit)
                    {
                        var hasHandoff = order.PlannedLegCount > 0 &&
                            order.CurrentLegSequence <
                                order.PlannedLegCount - 1;
                        order.Status = hasHandoff
                            ? MilitaryLogisticsStatus.AwaitingHandoff
                            : MilitaryLogisticsStatus.AwaitingArmy;
                        for (var legIndex = 0;
                             legIndex < world.MilitaryLogisticsLegs.Count;
                             legIndex++)
                        {
                            var leg = world.MilitaryLogisticsLegs[legIndex];
                            if (leg.LogisticsOrderId == order.Id &&
                                leg.Sequence == order.CurrentLegSequence)
                            {
                                leg.Status = hasHandoff
                                    ? MilitaryLogisticsLegStatus
                                        .AwaitingHandoff
                                    : MilitaryLogisticsLegStatus
                                        .AwaitingReceipt;
                                break;
                            }
                        }
                    }
                }
                for (var escortIndex = 0;
                     escortIndex < world.MilitaryLogisticsEscorts.Count;
                     escortIndex++)
                {
                    var escort = world.MilitaryLogisticsEscorts[escortIndex];
                    if (escort.JourneyId == journey.Id &&
                        escort.Status ==
                            MilitaryLogisticsEscortStatus.InTransit)
                    {
                        escort.Status =
                            MilitaryLogisticsEscortStatus.Arrived;
                        escort.ArrivedDay = world.AbsoluteDay;
                    }
                }
                _populationLedgerSystem.MoveIndependentPerson(
                    world,
                    person,
                    journey.DestinationLocationId,
                    false);
                for (var containerIndex = 0;
                     containerIndex < world.InventoryContainers.Count;
                     containerIndex++)
                {
                    var container = world.InventoryContainers[containerIndex];
                    if (container.CarrierPersonId == person.Id)
                    {
                        container.LocationId = journey.DestinationLocationId;
                    }
                }
                for (var freightIndex = 0;
                     freightIndex < world.CivilianFreights.Count;
                     freightIndex++)
                {
                    var freight = world.CivilianFreights[freightIndex];
                    if (freight.JourneyId == journey.Id &&
                        freight.Status == CivilianFreightStatus.InTransit)
                    {
                        var hasNextLeg = freight.PlannedRouteIds != null &&
                            freight.CurrentRouteIndex + 1 <
                                freight.PlannedRouteIds.Count;
                        freight.Status = hasNextLeg
                            ? CivilianFreightStatus.AwaitingNextLeg
                            : CivilianFreightStatus.AwaitingReceipt;
                        if (!hasNextLeg)
                        {
                            freight.ArrivedDay = world.AbsoluteDay;
                        }
                    }
                }
                world.Journeys.RemoveAt(i);
                completedAnyJourney = true;
            }
            if (completedAnyJourney)
            {
                MilitaryMedicalEvacuationSystem.ResolveArrivalsWithoutValidation(
                    world);
                world.Validate();
            }
        }

        public void ConsumeDailyTravelProvisions(
            WorldState world,
            ISet<string> externallyProvisionedPeople = null)
        {
            var people = PeopleFor(world);
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                var person = people.GetRequired(
                    world.Journeys[i].PersonId);
                if (!person.IsAlive)
                {
                    continue;
                }
                person = people.GetRequiredForUpdate(person.Id);
                if (externallyProvisionedPeople != null &&
                    externallyProvisionedPeople.Contains(person.Id))
                {
                    continue;
                }

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
