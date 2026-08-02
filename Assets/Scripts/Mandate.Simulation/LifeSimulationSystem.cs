using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LifeSimulationSystem
    {
        private const int DaysPerYear = 360;
        private readonly NamedRandom _random;
        private readonly IPersonRepository _personRepository;
        private readonly PopulationLedgerSystem _populationLedgerSystem;

        public LifeSimulationSystem(
            ulong masterSeed,
            IPersonRepository personRepository = null)
        {
            _random = new NamedRandom(masterSeed);
            _personRepository = personRepository;
            _populationLedgerSystem =
                new PopulationLedgerSystem(personRepository);
        }

        public void ResolveMonthly(WorldState world)
        {
            if (world.AbsoluteDay == 0 || world.AbsoluteDay % 30 != 0)
            {
                return;
            }

            var monthIndex = world.AbsoluteDay / 30;
            var people = _personRepository ??
                new WorldStatePersonRepository(world);
            ResolveDeathsAndSuccession(world, people);
            ResolveHouseholdFinances(world, people, monthIndex);
            ResolveHealth(world, people, monthIndex);
            ResolveBirths(world, people, monthIndex);
            ResolveDeathsAndSuccession(world, people);
        }

        private void ResolveHouseholdFinances(
            WorldState world,
            IPersonRepository people,
            long monthIndex)
        {
            var families = new List<FamilyState>(world.Families);
            families.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var livingMembers = CountLivingMembers(people, family);
                var upkeep = livingMembers * 15L;
                if (family.Wealth >= upkeep)
                {
                    family.Wealth -= upkeep;
                    continue;
                }

                var shortfall = upkeep - family.Wealth;
                family.Wealth = 0;
                family.Debt = checked(family.Debt + shortfall);
                AddEvent(
                    world,
                    LifeEventType.HouseholdDebt,
                    family.HeadPersonId,
                    string.Empty,
                    family.Id,
                    $"家庭月度开支不足，新增债务{shortfall}钱。",
                    monthIndex);

                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(family.MemberIds[memberIndex]);
                    if (person.IsAlive)
                    {
                        person = people.GetRequiredForUpdate(person.Id);
                        person.Needs.Livelihood = Math.Min(
                            10_000,
                            person.Needs.Livelihood + 500);
                    }
                }
            }
        }

        private void ResolveHealth(
            WorldState world,
            IPersonRepository people,
            long monthIndex)
        {
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                var person = knownPeople[i];
                if (!person.IsAlive)
                {
                    continue;
                }

                var location = FindLocation(world, person.LocationId);
                var ageYears = Math.Max(0, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
                var agePressure = ageYears >= 50 ? (int)Math.Min(2_000, (ageYears - 49) * 50) : 0;
                var disorderPressure = (10_000 - location.PublicOrderBasisPoints) / 20;
                var illnessChance = Math.Min(3_000, 100 + agePressure + disorderPressure);
                var personId = new StableId(person.Id);
                if (_random.CheckBasisPoints(
                        "life",
                        personId,
                        monthIndex,
                        "illness",
                        illnessChance))
                {
                    person = people.GetRequiredForUpdate(person.Id);
                    var damage = _random.Range(
                        "life", personId, monthIndex, "illness_damage", 300, 1_001);
                    person.HealthBasisPoints = Math.Max(0, person.HealthBasisPoints - damage);
                    AddEvent(
                        world,
                        LifeEventType.Illness,
                        person.Id,
                        string.Empty,
                        FindFamilyId(world, person.Id),
                        $"{person.DisplayName}患病，健康下降{damage}。",
                        monthIndex);
                }
                else if (person.HealthBasisPoints < 10_000)
                {
                    person = people.GetRequiredForUpdate(person.Id);
                    var recovery = Math.Min(250, 10_000 - person.HealthBasisPoints);
                    person.HealthBasisPoints += recovery;
                    AddEvent(
                        world,
                        LifeEventType.Recovery,
                        person.Id,
                        string.Empty,
                        FindFamilyId(world, person.Id),
                        $"{person.DisplayName}休养恢复{recovery}健康。",
                        monthIndex);
                }
            }
        }

        private void ResolveBirths(
            WorldState world,
            IPersonRepository people,
            long monthIndex)
        {
            var mothers = people.GetKnownPeople();
            for (var i = 0; i < mothers.Count; i++)
            {
                var mother = mothers[i];
                if (!mother.IsAlive ||
                    mother.Gender != PersonGender.Female ||
                    string.IsNullOrEmpty(mother.SpousePersonId))
                {
                    continue;
                }

                var ageYears = (world.AbsoluteDay - mother.BirthDay) / DaysPerYear;
                if (ageYears < 18 || ageYears > 42 ||
                    mother.LastChildbirthDay >= 0 &&
                    world.AbsoluteDay - mother.LastChildbirthDay < 300)
                {
                    continue;
                }

                var father = people.GetRequired(mother.SpousePersonId);
                if (!father.IsAlive || father.LocationId != mother.LocationId)
                {
                    continue;
                }

                var motherId = new StableId(mother.Id);
                if (!_random.CheckBasisPoints(
                        "life",
                        motherId,
                        monthIndex,
                        "childbirth",
                        450))
                {
                    continue;
                }

                var family = FindFamily(world, mother.Id);
                if (family == null)
                {
                    continue;
                }

                var childIndex = CountChildren(people, mother.Id) + 1;
                var childId = $"person.generated.child.{mother.Id}.{childIndex}";
                var child = new PersonState
                {
                    Id = childId,
                    DisplayName = $"新生儿{childIndex}",
                    LocationId = mother.LocationId,
                    BirthLocationId = mother.LocationId,
                    FamilyId = family.Id,
                    BirthDay = world.AbsoluteDay,
                    Gender = _random.CheckBasisPoints(
                        "life", motherId, monthIndex, "child_gender", 5_000)
                        ? PersonGender.Female
                        : PersonGender.Male,
                    FatherPersonId = father.Id,
                    MotherPersonId = mother.Id,
                    Provisions = 0,
                    VillageOccupation = VillageOccupation.Dependent,
                    LaborCapacityBasisPoints = 0,
                    NextIndependentEventDay = world.AbsoluteDay + 30,
                    NextIndependentEventReason =
                        "monthly_household_settlement"
                };
                CharacterAbilityBootstrap.InitializeChild(
                    world,
                    child,
                    people.GetRequiredForUpdate(father.Id),
                    people.GetRequiredForUpdate(mother.Id));
                people.Add(child);
                family.MemberIds.Add(child.Id);
                _populationLedgerSystem.RecordBirth(world, child);
                people.GetRequiredForUpdate(mother.Id).LastChildbirthDay =
                    world.AbsoluteDay;
                AddEvent(
                    world,
                    LifeEventType.Birth,
                    child.Id,
                    mother.Id,
                    family.Id,
                    $"{mother.DisplayName}与{father.DisplayName}的孩子出生。",
                    monthIndex);
            }
        }

        private void ResolveDeathsAndSuccession(
            WorldState world,
            IPersonRepository people)
        {
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                var person = knownPeople[i];
                if (person.IsAlive && person.HealthBasisPoints <= 0)
                {
                    person = people.GetRequiredForUpdate(person.Id);
                    _populationLedgerSystem.RecordDeath(world, person);
                    AddEvent(
                        world,
                        LifeEventType.Death,
                        person.Id,
                        string.Empty,
                        FindFamilyId(world, person.Id),
                        $"{person.DisplayName}去世。",
                        world.AbsoluteDay / 30);
                }
            }

            for (var familyIndex = 0; familyIndex < world.Families.Count; familyIndex++)
            {
                var family = world.Families[familyIndex];
                var head = people.GetRequired(family.HeadPersonId);
                if (head.IsAlive)
                {
                    continue;
                }

                PersonState successor = null;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var candidate = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (!candidate.IsAlive)
                    {
                        continue;
                    }

                    if (successor == null ||
                        candidate.BirthDay < successor.BirthDay ||
                        candidate.BirthDay == successor.BirthDay &&
                        string.CompareOrdinal(candidate.Id, successor.Id) < 0)
                    {
                        successor = candidate;
                    }
                }

                if (successor == null)
                {
                    continue;
                }

                var formerHead = family.HeadPersonId;
                family.HeadPersonId = successor.Id;
                AddEvent(
                    world,
                    LifeEventType.Succession,
                    successor.Id,
                    formerHead,
                    family.Id,
                    $"{successor.DisplayName}继任{family.DisplayName}家主。",
                    world.AbsoluteDay / 30);
            }
        }

        private static void AddEvent(
            WorldState world,
            LifeEventType type,
            string primaryPersonId,
            string secondaryPersonId,
            string familyId,
            string summary,
            long sequence)
        {
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id =
                    $"life_event.{world.AbsoluteDay}.{type.ToString().ToLowerInvariant()}." +
                    $"{primaryPersonId}.{sequence}",
                Type = type,
                Day = world.AbsoluteDay,
                PrimaryPersonId = primaryPersonId,
                SecondaryPersonId = secondaryPersonId,
                FamilyId = familyId,
                Summary = summary
            });
        }

        private static int CountLivingMembers(
            IPersonRepository people,
            FamilyState family)
        {
            var count = 0;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                if (people.GetRequired(family.MemberIds[i]).IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountChildren(
            IPersonRepository people,
            string motherId)
        {
            var count = 0;
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                if (knownPeople[i].MotherPersonId == motherId)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FindFamilyId(WorldState world, string personId)
        {
            var family = FindFamily(world, personId);
            return family == null ? string.Empty : family.Id;
        }

        private static FamilyState FindFamily(WorldState world, string personId)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].MemberIds.Contains(personId))
                {
                    return world.Families[i];
                }
            }

            return null;
        }

        private static LocationState FindLocation(WorldState world, string locationId)
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
