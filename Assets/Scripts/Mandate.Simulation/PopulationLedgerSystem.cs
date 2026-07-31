using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class PopulationAuditResult
    {
        public long OpeningPopulation;
        public long ExpectedPopulation;
        public long ActualPopulation;
        public long AbstractPopulation;
        public int IndependentPopulation;
        public int Births;
        public int Deaths;
        public readonly List<string> LocationMismatches =
            new List<string>();

        public bool IsBalanced =>
            ExpectedPopulation == ActualPopulation &&
            LocationMismatches.Count == 0;
    }

    public sealed class PopulationLedgerSystem
    {
        public void InitializeFromLocationSummaries(WorldState world)
        {
            PopulationLedgerBootstrap.Initialize(world);
            world.Validate();
        }

        public void MaterializePerson(
            WorldState world,
            PersonState person,
            PopulationOccupation occupation)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            MaterializePeople(
                world,
                new[] { person },
                occupation);
        }

        public void MaterializePeople(
            WorldState world,
            IList<PersonState> people,
            PopulationOccupation occupation)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            world.Validate();
            if (!world.PopulationLedgerInitialized)
            {
                throw new InvalidOperationException(
                    "Population ledger has not been initialized.");
            }

            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < world.People.Count; i++)
            {
                existingIds.Add(world.People[i].Id);
            }

            for (var personIndex = 0;
                 personIndex < people.Count;
                 personIndex++)
            {
                var person = people[personIndex] ??
                    throw new InvalidOperationException(
                        "A materialized person cannot be null.");
                _ = new StableId(person.Id);
                if (!existingIds.Add(person.Id))
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} already exists.");
                }

                var cohort = FindAvailableCohort(
                    world,
                    person.LocationId,
                    occupation);
                cohort.Population--;
                RefreshCohortDemographics(cohort);
                person.CountsTowardPopulation = true;
                person.PopulationOriginLocationId = cohort.OriginLocationId;
                world.People.Add(person);
                AddTransaction(
                    world,
                    PopulationTransactionType.Instantiation,
                    1,
                    person.LocationId,
                    person.LocationId,
                    cohort.Id,
                    string.Empty,
                    person.Id,
                    $"{person.DisplayName}从{OccupationName(occupation)}人口中实例化。");
            }

            world.Validate();
        }

        public void RecordBirth(WorldState world, PersonState child)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (!world.PopulationLedgerInitialized)
            {
                return;
            }

            child.CountsTowardPopulation = true;
            child.PopulationOriginLocationId = child.LocationId;
            var location = FindLocation(world, child.LocationId);
            location.Population = checked(location.Population + 1);
            AddTransaction(
                world,
                PopulationTransactionType.Birth,
                1,
                string.Empty,
                child.LocationId,
                string.Empty,
                string.Empty,
                child.Id,
                $"{child.DisplayName}出生。");
            world.Validate();
        }

        public void RecordDeath(WorldState world, PersonState person)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            RecordDeaths(world, new[] { person }, true);
        }

        public void RecordDeaths(
            WorldState world,
            IList<PersonState> people,
            bool validateInitialState)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            if (validateInitialState)
            {
                world.Validate();
            }

            var affectedArmies = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i] ??
                    throw new InvalidOperationException(
                        "A deceased person cannot be null.");
                if (!person.IsAlive)
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} is already deceased.");
                }

                for (var serviceIndex = 0;
                     serviceIndex < world.MilitaryServices.Count;
                     serviceIndex++)
                {
                    var service = world.MilitaryServices[serviceIndex];
                    if (service.PersonId != person.Id)
                    {
                        continue;
                    }

                    service.Status = MilitaryServiceStatus.Dead;
                    service.LastStatusChangeDay = world.AbsoluteDay;
                    affectedArmies.Add(service.ArmyId);
                }

                person.IsAlive = false;
                if (world.PopulationLedgerInitialized &&
                    person.CountsTowardPopulation)
                {
                    var location = FindLocation(world, person.LocationId);
                    location.Population = checked(location.Population - 1);
                    AddTransaction(
                        world,
                        PopulationTransactionType.Death,
                        1,
                        person.LocationId,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        person.Id,
                        $"{person.DisplayName}死亡。");
                }
            }

            var militaryService = new MilitaryServiceSystem();
            foreach (var armyId in affectedArmies)
            {
                militaryService.SynchronizeArmyCaches(world, armyId);
            }

            world.Validate();
        }

        public void MoveIndependentPerson(
            WorldState world,
            PersonState person,
            string destinationLocationId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            world.Validate();
            var originLocationId = person.LocationId;
            if (originLocationId == destinationLocationId)
            {
                return;
            }

            var origin = FindLocation(world, originLocationId);
            var destination = FindLocation(world, destinationLocationId);
            if (world.PopulationLedgerInitialized &&
                person.CountsTowardPopulation &&
                person.IsAlive)
            {
                origin.Population = checked(origin.Population - 1);
                destination.Population = checked(destination.Population + 1);
            }

            person.LocationId = destinationLocationId;
            if (world.PopulationLedgerInitialized &&
                person.CountsTowardPopulation &&
                person.IsAlive)
            {
                AddTransaction(
                    world,
                    PopulationTransactionType.Migration,
                    1,
                    originLocationId,
                    destinationLocationId,
                    string.Empty,
                    string.Empty,
                    person.Id,
                    $"{person.DisplayName}从{origin.DisplayName}迁往" +
                    $"{destination.DisplayName}。");
            }

            world.Validate();
        }

        public void MovePeople(
            WorldState world,
            IList<PersonState> people,
            string destinationLocationId,
            bool validateInitialState)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            if (validateInitialState)
            {
                world.Validate();
            }

            var destination = FindLocation(world, destinationLocationId);
            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i] ??
                    throw new InvalidOperationException(
                        "A moving person cannot be null.");
                var originLocationId = person.LocationId;
                if (originLocationId == destinationLocationId)
                {
                    continue;
                }

                var origin = FindLocation(world, originLocationId);
                if (world.PopulationLedgerInitialized &&
                    person.CountsTowardPopulation &&
                    person.IsAlive)
                {
                    origin.Population = checked(origin.Population - 1);
                    destination.Population =
                        checked(destination.Population + 1);
                }

                person.LocationId = destinationLocationId;
                if (world.PopulationLedgerInitialized &&
                    person.CountsTowardPopulation &&
                    person.IsAlive)
                {
                    AddTransaction(
                        world,
                        PopulationTransactionType.Migration,
                        1,
                        originLocationId,
                        destinationLocationId,
                        string.Empty,
                        string.Empty,
                        person.Id,
                        $"{person.DisplayName}随军由{origin.DisplayName}" +
                        $"迁往{destination.DisplayName}。");
                }
            }

            world.Validate();
        }

        public void TransferCohort(
            WorldState world,
            StableId cohortId,
            StableId destinationLocationId,
            int quantity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            var source = FindCohort(world, cohortId.Value);
            if (source.LocationId == destinationLocationId.Value)
            {
                throw new InvalidOperationException(
                    "Population cohort is already at the destination.");
            }

            if (source.Population < quantity)
            {
                throw new InvalidOperationException(
                    "Population cohort does not contain enough people.");
            }

            var destination = FindOrCreateDestinationCohort(
                world,
                source,
                destinationLocationId.Value);
            var originLocation = FindLocation(world, source.LocationId);
            var destinationLocation = FindLocation(
                world,
                destinationLocationId.Value);
            source.Population -= quantity;
            destination.Population = checked(destination.Population + quantity);
            RefreshCohortDemographics(source);
            RefreshCohortDemographics(destination);
            originLocation.Population -= quantity;
            destinationLocation.Population =
                checked(destinationLocation.Population + quantity);
            AddTransaction(
                world,
                PopulationTransactionType.Migration,
                quantity,
                originLocation.Id,
                destinationLocation.Id,
                source.Id,
                destination.Id,
                string.Empty,
                $"{quantity}名{OccupationName(source.Occupation)}从" +
                $"{originLocation.DisplayName}迁往" +
                $"{destinationLocation.DisplayName}。");
            world.Validate();
        }

        public PopulationAuditResult Audit(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var result = new PopulationAuditResult
            {
                OpeningPopulation = world.PopulationOpeningTotal,
                ExpectedPopulation = world.PopulationOpeningTotal
            };
            var byLocation = new Dictionary<string, long>(
                StringComparer.Ordinal);
            for (var i = 0; i < world.Locations.Count; i++)
            {
                byLocation.Add(world.Locations[i].Id, 0);
            }

            for (var i = 0; i < world.PopulationCohorts.Count; i++)
            {
                var cohort = world.PopulationCohorts[i];
                result.AbstractPopulation += cohort.Population;
                result.ActualPopulation += cohort.Population;
                byLocation[cohort.LocationId] += cohort.Population;
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (!person.CountsTowardPopulation || !person.IsAlive)
                {
                    continue;
                }

                result.IndependentPopulation++;
                result.ActualPopulation++;
                byLocation[person.LocationId]++;
            }

            for (var i = 0; i < world.PopulationTransactions.Count; i++)
            {
                var transaction = world.PopulationTransactions[i];
                if (transaction.Type == PopulationTransactionType.Birth)
                {
                    result.Births += transaction.Quantity;
                    result.ExpectedPopulation += transaction.Quantity;
                }
                else if (transaction.Type == PopulationTransactionType.Death)
                {
                    result.Deaths += transaction.Quantity;
                    result.ExpectedPopulation -= transaction.Quantity;
                }
            }

            for (var i = 0; i < world.Locations.Count; i++)
            {
                var location = world.Locations[i];
                if (byLocation[location.Id] != location.Population)
                {
                    result.LocationMismatches.Add(
                        $"{location.DisplayName}：汇总{location.Population}，" +
                        $"账本{byLocation[location.Id]}");
                }
            }

            return result;
        }

        public static string OccupationName(
            PopulationOccupation occupation)
        {
            switch (occupation)
            {
                case PopulationOccupation.Agriculture:
                    return "农业";
                case PopulationOccupation.Artisan:
                    return "工匠";
                case PopulationOccupation.Merchant:
                    return "商贸";
                case PopulationOccupation.Administration:
                    return "行政";
                case PopulationOccupation.Medical:
                    return "医药";
                default:
                    return "依附人口";
            }
        }

        private static void AddTransaction(
            WorldState world,
            PopulationTransactionType type,
            int quantity,
            string fromLocationId,
            string toLocationId,
            string fromCohortId,
            string toCohortId,
            string personId,
            string summary)
        {
            world.PopulationTransactions.Add(new PopulationTransactionState
            {
                Id =
                    $"population_transaction.{world.PopulationTransactions.Count}." +
                    type.ToString().ToLowerInvariant(),
                Day = world.AbsoluteDay,
                Type = type,
                Quantity = quantity,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                FromCohortId = fromCohortId,
                ToCohortId = toCohortId,
                PersonId = personId,
                Summary = summary
            });
        }

        private static PopulationCohortState FindAvailableCohort(
            WorldState world,
            string locationId,
            PopulationOccupation occupation)
        {
            for (var i = 0; i < world.PopulationCohorts.Count; i++)
            {
                var cohort = world.PopulationCohorts[i];
                if (cohort.LocationId == locationId &&
                    cohort.Occupation == occupation &&
                    cohort.Population > 0)
                {
                    return cohort;
                }
            }

            throw new InvalidOperationException(
                $"{locationId}没有可实例化的{OccupationName(occupation)}人口。");
        }

        private static PopulationCohortState FindCohort(
            WorldState world,
            string cohortId)
        {
            for (var i = 0; i < world.PopulationCohorts.Count; i++)
            {
                if (world.PopulationCohorts[i].Id == cohortId)
                {
                    return world.PopulationCohorts[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing population cohort {cohortId}.");
        }

        private static PopulationCohortState FindOrCreateDestinationCohort(
            WorldState world,
            PopulationCohortState source,
            string destinationLocationId)
        {
            for (var i = 0; i < world.PopulationCohorts.Count; i++)
            {
                var cohort = world.PopulationCohorts[i];
                if (cohort.LocationId == destinationLocationId &&
                    cohort.OriginLocationId == source.OriginLocationId &&
                    cohort.Occupation == source.Occupation)
                {
                    return cohort;
                }
            }

            var created = new PopulationCohortState
            {
                Id =
                    $"{source.Id}.at.{destinationLocationId}." +
                    $"{world.PopulationCohorts.Count}",
                LocationId = destinationLocationId,
                OriginLocationId = source.OriginLocationId,
                Occupation = source.Occupation,
                Population = 0,
                Households = 0,
                WorkingAgePopulation = 0,
                CollectiveWealth = 0,
                AverageHealthBasisPoints =
                    source.AverageHealthBasisPoints,
                SatisfactionBasisPoints =
                    source.SatisfactionBasisPoints,
                MigrationPressureBasisPoints =
                    source.MigrationPressureBasisPoints,
                StableSeed = source.StableSeed ^
                             (ulong)world.PopulationCohorts.Count + 1UL
            };
            world.PopulationCohorts.Add(created);
            return created;
        }

        private static void RefreshCohortDemographics(
            PopulationCohortState cohort)
        {
            cohort.Households = cohort.Population == 0
                ? 0
                : Math.Max(1, cohort.Population / 5);
            cohort.WorkingAgePopulation =
                cohort.Occupation == PopulationOccupation.Dependent
                    ? cohort.Population / 4
                    : cohort.Population * 3 / 5;
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
        }
    }
}
