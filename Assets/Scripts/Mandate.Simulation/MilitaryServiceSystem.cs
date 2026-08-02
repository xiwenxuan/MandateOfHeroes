using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryServiceAudit
    {
        public int Mustering;
        public int Active;
        public int Wounded;
        public int Stragglers;
        public int Deserters;
        public int Captured;
        public int Retired;
        public int Dead;

        public int Available => Mustering + Active;
        public int Total =>
            Mustering + Active + Wounded + Stragglers +
            Deserters + Captured + Retired + Dead;
    }

    public sealed class MilitaryServiceSystem
    {
        public const int PrototypeStrengthPerArmy = 80;
        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public MilitaryServiceSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public void InitializePrototype(
            WorldState world,
            int strengthPerArmy = PrototypeStrengthPerArmy)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (!world.PopulationLedgerInitialized)
            {
                throw new InvalidOperationException(
                    "Population ledger must be initialized first.");
            }

            if (world.MilitaryServiceInitialized)
            {
                return;
            }

            if (strengthPerArmy < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(strengthPerArmy));
            }

            var armies = new List<ArmyState>(world.Armies);
            armies.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var people = PeopleFor(world);
            var population = new PopulationLedgerSystem(people);
            var recruitsByArmy = new List<List<PersonState>>();
            for (var armyIndex = 0; armyIndex < armies.Count; armyIndex++)
            {
                var army = armies[armyIndex];
                var commander = people.GetRequired(army.CommanderPersonId);
                if (commander.LocationId != army.LocationId)
                {
                    population.MoveIndependentPerson(
                        world, commander, army.LocationId);
                }

                var recruits = new List<PersonState>();
                for (var index = 1; index < strengthPerArmy; index++)
                {
                    var role = RoleForIndex(index);
                    var person = CreateRecruit(
                        world, army, index, role);
                    recruits.Add(person);
                }

                population.MaterializePeople(
                    world, recruits, PopulationOccupation.Administration);
                recruitsByArmy.Add(recruits);
            }

            for (var armyIndex = 0; armyIndex < armies.Count; armyIndex++)
            {
                AddArmyStructure(
                    world,
                    armies[armyIndex],
                    recruitsByArmy[armyIndex],
                    strengthPerArmy);
            }

            world.MilitaryServiceInitialized = true;
            for (var i = 0; i < armies.Count; i++)
            {
                armies[i].MaximumTroops = strengthPerArmy;
                SynchronizeArmyCaches(world, armies[i].Id);
            }

            world.Validate();
        }

        public MilitaryServiceAudit AuditArmy(
            WorldState world,
            StableId armyId)
        {
            var audit = new MilitaryServiceAudit();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId != armyId.Value)
                {
                    continue;
                }

                switch (service.Status)
                {
                    case MilitaryServiceStatus.Mustering:
                        audit.Mustering++;
                        break;
                    case MilitaryServiceStatus.Active:
                        audit.Active++;
                        break;
                    case MilitaryServiceStatus.Wounded:
                        audit.Wounded++;
                        break;
                    case MilitaryServiceStatus.Straggler:
                        audit.Stragglers++;
                        break;
                    case MilitaryServiceStatus.Deserter:
                        audit.Deserters++;
                        break;
                    case MilitaryServiceStatus.Captured:
                        audit.Captured++;
                        break;
                    case MilitaryServiceStatus.Retired:
                        audit.Retired++;
                        break;
                    case MilitaryServiceStatus.Dead:
                        audit.Dead++;
                        break;
                }
            }

            return audit;
        }

        public void SynchronizeArmyCaches(
            WorldState world,
            string armyId)
        {
            if (!world.MilitaryServiceInitialized &&
                world.MilitaryServices.Count == 0)
            {
                return;
            }

            var army = FindArmy(world, armyId);
            var audit = AuditArmy(world, new StableId(armyId));
            army.Troops = audit.Available;
            army.WoundedTroops = audit.Wounded;
            if (army.Troops == 0)
            {
                army.IsMobilized = false;
            }
        }

        public List<MilitaryServiceState> ApplyCasualties(
            WorldState world,
            StableId armyId,
            int casualties,
            int wounded,
            long sequence,
            bool deferEquipmentResolution = false)
        {
            if (casualties < 0 ||
                wounded < 0 ||
                wounded > casualties)
            {
                throw new ArgumentOutOfRangeException(nameof(casualties));
            }

            if (!world.MilitaryServiceInitialized)
            {
                var abstractArmy = FindArmy(world, armyId.Value);
                abstractArmy.Troops -= casualties;
                abstractArmy.WoundedTroops = checked(
                    abstractArmy.WoundedTroops + wounded);
                return new List<MilitaryServiceState>();
            }

            var available = SelectServices(
                world, armyId.Value, MilitaryServiceStatus.Active,
                casualties, sequence, "casualty");
            if (available.Count < casualties)
            {
                var mustering = SelectServices(
                    world, armyId.Value, MilitaryServiceStatus.Mustering,
                    casualties - available.Count, sequence, "casualty_mustering");
                available.AddRange(mustering);
            }

            if (available.Count != casualties)
            {
                throw new InvalidOperationException(
                    $"Army {armyId.Value} lacks service members for casualties.");
            }

            var people = PeopleFor(world);
            for (var i = 0; i < available.Count; i++)
            {
                var service = available[i];
                service.Status = i < wounded
                    ? MilitaryServiceStatus.Wounded
                    : MilitaryServiceStatus.Dead;
                service.LastStatusChangeDay = world.AbsoluteDay;
                var person = people.GetRequired(service.PersonId);
                if (service.Status == MilitaryServiceStatus.Wounded)
                {
                    var woundedHealth = Math.Min(
                        person.HealthBasisPoints, 4_000);
                    if (woundedHealth != person.HealthBasisPoints)
                    {
                        people.GetRequiredForUpdate(service.PersonId)
                            .HealthBasisPoints = woundedHealth;
                    }
                }
            }

            SynchronizeArmyCaches(world, armyId.Value);
            var ledger = new PopulationLedgerSystem(people);
            var deceased = new List<PersonState>();
            for (var i = wounded; i < available.Count; i++)
            {
                deceased.Add(people.GetRequired(available[i].PersonId));
            }

            if (deceased.Count > 0)
            {
                ledger.RecordDeaths(world, deceased, false);
            }

            if (world.MilitaryEquipmentInitialized &&
                !deferEquipmentResolution)
            {
                new MilitaryEquipmentSystem(_people)
                    .ResolveCasualtiesWithoutBattle(
                        world, available, sequence);
            }

            if (!deferEquipmentResolution)
            {
                world.Validate();
            }

            return available;
        }

        public int ApplyDesertion(
            WorldState world,
            StableId armyId,
            int requested,
            long sequence)
        {
            if (requested <= 0)
            {
                return 0;
            }

            if (!world.MilitaryServiceInitialized)
            {
                var army = FindArmy(world, armyId.Value);
                var applied = Math.Min(requested, army.Troops);
                army.Troops -= applied;
                return applied;
            }

            var selected = SelectServices(
                world, armyId.Value, MilitaryServiceStatus.Active,
                requested, sequence, "desertion");
            for (var i = 0; i < selected.Count; i++)
            {
                selected[i].Status = MilitaryServiceStatus.Deserter;
                selected[i].LastStatusChangeDay = world.AbsoluteDay;
            }

            if (world.MilitaryEquipmentInitialized)
            {
                new MilitaryEquipmentSystem(_people).ResolveDesertionLoss(
                    world, selected, sequence);
            }

            SynchronizeArmyCaches(world, armyId.Value);
            world.Validate();
            return selected.Count;
        }

        public int RecoverWounded(
            WorldState world,
            StableId armyId,
            int requested,
            long sequence,
            IPersonRepository people = null)
        {
            if (!world.MilitaryServiceInitialized)
            {
                var army = FindArmy(world, armyId.Value);
                var recovered = Math.Min(requested, army.WoundedTroops);
                army.WoundedTroops -= recovered;
                army.Troops = checked(army.Troops + recovered);
                return recovered;
            }

            var selected = SelectServices(
                world, armyId.Value, MilitaryServiceStatus.Wounded,
                requested, sequence, "recovery");
            people = people ?? PeopleFor(world);
            for (var i = 0; i < selected.Count; i++)
            {
                var service = selected[i];
                service.Status = MilitaryServiceStatus.Active;
                service.LastStatusChangeDay = world.AbsoluteDay;
                var person = people.GetRequired(service.PersonId);
                var recoveredHealth = Math.Max(
                    person.HealthBasisPoints, 6_000);
                if (recoveredHealth != person.HealthBasisPoints)
                {
                    people.GetRequiredForUpdate(service.PersonId)
                        .HealthBasisPoints = recoveredHealth;
                }
            }

            SynchronizeArmyCaches(world, armyId.Value);
            world.Validate();
            return selected.Count;
        }

        private static void AddArmyStructure(
            WorldState world,
            ArmyState army,
            List<PersonState> recruits,
            int strength)
        {
            var rootId = $"formation.{army.Id}.root";
            var leftId = $"formation.{army.Id}.left";
            var rightId = $"formation.{army.Id}.right";
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = rootId,
                ArmyId = army.Id,
                DisplayName = army.DisplayName,
                Kind = MilitaryFormationKind.Army,
                CommanderPersonId = army.CommanderPersonId,
                AuthorizedStrength = strength,
                DisplayOrder = 0
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = leftId,
                ArmyId = army.Id,
                ParentFormationId = rootId,
                DisplayName = army.DisplayName + "左队",
                Kind = MilitaryFormationKind.Unit,
                CommanderPersonId = recruits[0].Id,
                AuthorizedStrength = strength / 2,
                DisplayOrder = 1
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = rightId,
                ArmyId = army.Id,
                ParentFormationId = rootId,
                DisplayName = army.DisplayName + "右队",
                Kind = MilitaryFormationKind.Unit,
                CommanderPersonId = recruits[1].Id,
                AuthorizedStrength = strength - strength / 2,
                DisplayOrder = 2
            });

            AddService(
                world, army, army.CommanderPersonId, rootId,
                MilitaryServiceRole.Commander, 3, 8_000, 8_000);
            for (var i = 0; i < recruits.Count; i++)
            {
                var index = i + 1;
                AddService(
                    world,
                    army,
                    recruits[i].Id,
                    index % 2 == 1 ? leftId : rightId,
                    RoleForIndex(index),
                    index <= 2 ? 2 : 1,
                    4_800 + index % 9 * 250,
                    5_000 + index % 7 * 300);
            }
        }

        private static void AddService(
            WorldState world,
            ArmyState army,
            string personId,
            string formationId,
            MilitaryServiceRole role,
            int rank,
            int discipline,
            int loyalty)
        {
            world.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = $"military_service.{personId}",
                PersonId = personId,
                ArmyId = army.Id,
                FormationId = formationId,
                Role = role,
                Rank = rank,
                Status = MilitaryServiceStatus.Active,
                DisciplineBasisPoints = Math.Min(10_000, discipline),
                LoyaltyBasisPoints = Math.Min(10_000, loyalty),
                ServiceExperienceBasisPoints =
                    role == MilitaryServiceRole.Commander ? 7_000 :
                    role == MilitaryServiceRole.Officer ? 4_500 : 1_500,
                EnlistedDay = world.AbsoluteDay,
                LastStatusChangeDay = world.AbsoluteDay
            });
        }

        private static PersonState CreateRecruit(
            WorldState world,
            ArmyState army,
            int index,
            MilitaryServiceRole role)
        {
            var person = new PersonState
            {
                Id = $"person.military.{army.Id}.{index:000}",
                DisplayName = army.DisplayName + "军士" + index.ToString("000"),
                LocationId = army.LocationId,
                BirthDay =
                    world.AbsoluteDay - (18 + index % 23) * 360L - index % 360,
                Gender = PersonGender.Male,
                HealthBasisPoints = 8_000 + index % 17 * 100,
                Wealth = 10 + index % 13,
                Provisions = 8,
                CargoCapacity = 30,
                CountsTowardPopulation = true
            };
            CharacterAbilityBootstrap.InitializePerson(
                world.MasterSeed,
                person,
                BackgroundForRole(role));
            return person;
        }

        private static CharacterBackgroundKind BackgroundForRole(
            MilitaryServiceRole role)
        {
            switch (role)
            {
                case MilitaryServiceRole.Officer:
                    return CharacterBackgroundKind.Commander;
                case MilitaryServiceRole.Medic:
                    return CharacterBackgroundKind.Physician;
                case MilitaryServiceRole.Quartermaster:
                    return CharacterBackgroundKind.Official;
                default:
                    return CharacterBackgroundKind.Soldier;
            }
        }

        private static MilitaryServiceRole RoleForIndex(int index)
        {
            if (index <= 2)
            {
                return MilitaryServiceRole.Officer;
            }

            if (index == 3)
            {
                return MilitaryServiceRole.Medic;
            }

            if (index == 4)
            {
                return MilitaryServiceRole.Quartermaster;
            }

            return index == 5
                ? MilitaryServiceRole.Messenger
                : MilitaryServiceRole.Soldier;
        }

        private static List<MilitaryServiceState> SelectServices(
            WorldState world,
            string armyId,
            MilitaryServiceStatus status,
            int maximum,
            long sequence,
            string purpose)
        {
            var selected = new List<MilitaryServiceState>();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == armyId && service.Status == status)
                {
                    selected.Add(service);
                }
            }

            var random = new NamedRandom(world.MasterSeed);
            selected.Sort((left, right) =>
            {
                var leftScore = random.Range(
                    "military_service",
                    new StableId(left.PersonId),
                    sequence,
                    purpose,
                    0,
                    int.MaxValue);
                var rightScore = random.Range(
                    "military_service",
                    new StableId(right.PersonId),
                    sequence,
                    purpose,
                    0,
                    int.MaxValue);
                var scoreComparison = leftScore.CompareTo(rightScore);
                return scoreComparison != 0
                    ? scoreComparison
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            if (selected.Count > maximum)
            {
                selected.RemoveRange(maximum, selected.Count - maximum);
            }

            return selected;
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            if (_people != null)
            {
                return _people;
            }

            if (!ReferenceEquals(_fallbackWorld, world))
            {
                _fallbackWorld = world;
                _fallbackPeople = new WorldStatePersonRepository(world);
            }

            return _fallbackPeople;
        }

        private static ArmyState FindArmy(
            WorldState world,
            string armyId)
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
    }
}
