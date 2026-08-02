using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class WorldSimulator
    {
        private readonly NamedRandom _random;
        private readonly TravelSystem _travelSystem = new TravelSystem();
        private readonly TaskSystem _taskSystem = new TaskSystem();
        private readonly HistoricalEventSystem _historicalEventSystem =
            new HistoricalEventSystem();
        private readonly LifeSimulationSystem _lifeSimulationSystem;
        private readonly MarketSimulationSystem _marketSimulationSystem;
        private readonly ArmySystem _armySystem = new ArmySystem();
        private readonly EducationSystem _educationSystem = new EducationSystem();
        private readonly VillageLifeSystem _villageLifeSystem;
        private readonly ResearchSystem _researchSystem;

        public WorldSimulator(
            ulong masterSeed,
            ProductionContentRegistry productionContent = null)
        {
            _random = new NamedRandom(masterSeed);
            _lifeSimulationSystem = new LifeSimulationSystem(masterSeed);
            _marketSimulationSystem = new MarketSimulationSystem(masterSeed);
            _villageLifeSystem = new VillageLifeSystem(
                masterSeed, productionContent);
            _researchSystem = new ResearchSystem(productionContent);
        }

        public void AdvanceDays(WorldState world, int days)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (days < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }

            if (world.MasterSeed == 0)
            {
                throw new InvalidOperationException("A world must have a non-zero master seed.");
            }

            AdvanceSegments(world, checked(days * 4));
        }

        public void AdvanceSegments(WorldState world, int segments)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (segments < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }

            for (var i = 0; i < segments; i++)
            {
                world.Validate();
                _travelSystem.AdvanceJourneysOneSegment(world);
                _armySystem.AdvanceMarchesOneSegment(world);
                var enteredNewDay = world.AdvanceOneSegment();
                if (enteredNewDay)
                {
                    ResolveDailySystems(world);
                }
            }
        }

        private void ResolveDailySystems(WorldState world)
        {
            _travelSystem.ConsumeDailyTravelProvisions(world);
            _armySystem.ConsumeDailyMarchSupplies(world);
            _historicalEventSystem.ResolveEligibleEvents(world);
            _taskSystem.ResolveDailyProgress(world);
            _researchSystem.ResolveDailyProjects(world);
            _villageLifeSystem.ResolveMonthly(world);
            _lifeSimulationSystem.ResolveMonthly(world);
            VillageLifeSystem.RefreshAllCaches(world);
            _educationSystem.ResolveDuePlans(world);
            _marketSimulationSystem.ResolveDailyPrices(world);
            var locations = new List<LocationState>(world.Locations);
            locations.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < locations.Count; i++)
            {
                UpdatePublicOrder(locations[i], world.AbsoluteDay);
            }
        }

        private void UpdatePublicOrder(LocationState location, long resolvingDay)
        {
            var locationId = new StableId(location.Id);
            if (!_random.CheckBasisPoints(
                    "public_order",
                    locationId,
                    resolvingDay,
                    "daily_change",
                    500))
            {
                return;
            }

            var change = _random.Range(
                "public_order",
                locationId,
                resolvingDay,
                "daily_direction",
                -20,
                21);
            location.PublicOrderBasisPoints =
                Clamp(location.PublicOrderBasisPoints + change, 0, 10_000);
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
