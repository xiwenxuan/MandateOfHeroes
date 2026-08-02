using System;

namespace Mandate.Domain
{
    public enum PopulationStorageMode : byte
    {
        InlineSnapshot,
        PartitionedPackage
    }

    [Serializable]
    public sealed class PopulationStorageState
    {
        public const int CurrentContractVersion = 1;

        public int ContractVersion = CurrentContractVersion;
        public PopulationStorageMode Mode = PopulationStorageMode.InlineSnapshot;
        public string PackageId = string.Empty;
        public int PartitionCount;
        public long PermanentPersonCount;
        public long LivingPersonCount;
        public long DetailExtensionCount;
        public long StorageRevision;
        public string ManifestSha256 = string.Empty;

        public static PopulationStorageState CreateInline(
            System.Collections.Generic.IReadOnlyList<PersonState> people)
        {
            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            var state = new PopulationStorageState();
            state.SynchronizeInlineCounts(people);
            return state;
        }

        public void SynchronizeInlineCounts(
            System.Collections.Generic.IReadOnlyList<PersonState> people)
        {
            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            Mode = PopulationStorageMode.InlineSnapshot;
            PackageId = string.Empty;
            PartitionCount = 0;
            PermanentPersonCount = people.Count;
            LivingPersonCount = 0;
            for (var i = 0; i < people.Count; i++)
            {
                if (people[i] != null && people[i].IsAlive)
                {
                    LivingPersonCount++;
                }
            }

            DetailExtensionCount = people.Count;
            StorageRevision = 0;
            ManifestSha256 = string.Empty;
        }

        public void Validate(int materializedPersonCount)
        {
            if (ContractVersion != CurrentContractVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported population storage contract {ContractVersion}.");
            }

            if (!Enum.IsDefined(typeof(PopulationStorageMode), Mode) ||
                materializedPersonCount < 0 ||
                PermanentPersonCount < 0 ||
                LivingPersonCount < 0 ||
                DetailExtensionCount < 0 ||
                StorageRevision < 0 ||
                LivingPersonCount > PermanentPersonCount ||
                DetailExtensionCount > PermanentPersonCount)
            {
                throw new InvalidOperationException(
                    "Invalid population storage metadata.");
            }

            if (Mode == PopulationStorageMode.InlineSnapshot)
            {
                if (PartitionCount != 0 ||
                    !string.IsNullOrEmpty(PackageId) ||
                    !string.IsNullOrEmpty(ManifestSha256))
                {
                    throw new InvalidOperationException(
                        "Inline population storage cannot reference an external package.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(PackageId) ||
                PartitionCount <= 0 ||
                !IsSha256(ManifestSha256))
            {
                throw new InvalidOperationException(
                    "Invalid partitioned population package metadata.");
            }
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F')))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public enum PopulationOccupation : byte
    {
        Agriculture,
        Artisan,
        Merchant,
        Administration,
        Medical,
        Dependent
    }

    public enum PopulationTransactionType : byte
    {
        Birth,
        Death,
        Migration,
        Instantiation,
        Reaggregation
    }

    [Serializable]
    public sealed class PopulationCohortState
    {
        public string Id;
        public string LocationId;
        public string OriginLocationId;
        public PopulationOccupation Occupation;
        public int Population;
        public int Households;
        public int WorkingAgePopulation;
        public long CollectiveWealth;
        public int AverageHealthBasisPoints = 10_000;
        public int SatisfactionBasisPoints = 5_000;
        public int MigrationPressureBasisPoints;
        public ulong StableSeed;
    }

    [Serializable]
    public sealed class PopulationTransactionState
    {
        public string Id;
        public long Day;
        public PopulationTransactionType Type;
        public int Quantity;
        public string FromLocationId;
        public string ToLocationId;
        public string FromCohortId;
        public string ToCohortId;
        public string PersonId;
        public string Summary;
    }

    public static class PopulationLedgerBootstrap
    {
        private static readonly PopulationOccupation[] Occupations =
        {
            PopulationOccupation.Agriculture,
            PopulationOccupation.Artisan,
            PopulationOccupation.Merchant,
            PopulationOccupation.Administration,
            PopulationOccupation.Medical,
            PopulationOccupation.Dependent
        };

        private static readonly int[] DistributionBasisPoints =
        {
            5_500,
            1_000,
            500,
            200,
            100,
            2_700
        };

        public static void Initialize(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.PopulationLedgerInitialized)
            {
                return;
            }

            if (world.PopulationCohorts.Count != 0 ||
                world.PopulationTransactions.Count != 0)
            {
                throw new InvalidOperationException(
                    "Population ledger contains data before initialization.");
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (string.IsNullOrEmpty(person.PopulationOriginLocationId))
                {
                    person.PopulationOriginLocationId = person.LocationId;
                }
            }

            long openingPopulation = 0;
            for (var locationIndex = 0;
                 locationIndex < world.Locations.Count;
                 locationIndex++)
            {
                var location = world.Locations[locationIndex];
                var independentPopulation = CountIndependentPopulation(
                    world,
                    location.Id);
                if (independentPopulation > location.Population)
                {
                    throw new InvalidOperationException(
                        $"{location.Id} has more independent people than its " +
                        "population summary.");
                }

                var abstractPopulation =
                    location.Population - independentPopulation;
                var allocated = 0;
                for (var occupationIndex = 0;
                     occupationIndex < Occupations.Length;
                     occupationIndex++)
                {
                    var population = occupationIndex == Occupations.Length - 1
                        ? abstractPopulation - allocated
                        : abstractPopulation *
                          DistributionBasisPoints[occupationIndex] /
                          10_000;
                    allocated += population;
                    if (population <= 0)
                    {
                        continue;
                    }

                    var occupation = Occupations[occupationIndex];
                    world.PopulationCohorts.Add(new PopulationCohortState
                    {
                        Id =
                            $"population.{location.Id}." +
                            OccupationKey(occupation),
                        LocationId = location.Id,
                        OriginLocationId = location.Id,
                        Occupation = occupation,
                        Population = population,
                        Households = Math.Max(1, population / 5),
                        WorkingAgePopulation =
                            occupation == PopulationOccupation.Dependent
                                ? population / 4
                                : population * 3 / 5,
                        CollectiveWealth = population * 20L,
                        AverageHealthBasisPoints = 8_500,
                        SatisfactionBasisPoints = 5_000,
                        MigrationPressureBasisPoints = 0,
                        StableSeed = StableSeed(
                            world.MasterSeed,
                            location.Id,
                            occupation)
                    });
                }

                openingPopulation += location.Population;
            }

            world.PopulationOpeningTotal = openingPopulation;
            world.PopulationLedgerInitialized = true;
        }

        private static int CountIndependentPopulation(
            WorldState world,
            string locationId)
        {
            var count = 0;
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person.CountsTowardPopulation &&
                    person.IsAlive &&
                    person.LocationId == locationId)
                {
                    count++;
                }
            }

            return count;
        }

        private static string OccupationKey(PopulationOccupation occupation)
        {
            switch (occupation)
            {
                case PopulationOccupation.Agriculture:
                    return "agriculture";
                case PopulationOccupation.Artisan:
                    return "artisan";
                case PopulationOccupation.Merchant:
                    return "merchant";
                case PopulationOccupation.Administration:
                    return "administration";
                case PopulationOccupation.Medical:
                    return "medical";
                default:
                    return "dependent";
            }
        }

        private static ulong StableSeed(
            ulong masterSeed,
            string locationId,
            PopulationOccupation occupation)
        {
            unchecked
            {
                var hash = 1469598103934665603UL ^ masterSeed;
                for (var i = 0; i < locationId.Length; i++)
                {
                    hash ^= locationId[i];
                    hash *= 1099511628211UL;
                }

                hash ^= (byte)occupation;
                hash *= 1099511628211UL;
                return hash == 0 ? 1UL : hash;
            }
        }
    }
}
