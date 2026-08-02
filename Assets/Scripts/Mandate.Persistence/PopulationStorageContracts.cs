using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Persistence
{
    [Serializable]
    public sealed class PermanentPersonCoreRecord
    {
        public string PersonId;
        public string DisplayName;
        public string CurrentLocationId;
        public string BirthLocationId;
        public string FamilyId;
        public long BirthDay;
        public bool IsAlive;
        public int HealthBasisPoints;
        public PersonGender Gender;
        public string FatherPersonId;
        public string MotherPersonId;
        public string SpousePersonId;
        public bool CountsTowardPopulation;
        public string PopulationOriginLocationId;
        public VillageOccupation VillageOccupation;
        public int LaborCapacityBasisPoints;
        public long NextIndependentEventDay;
        public string NextIndependentEventReason;
        public LocalDutyKind LocalDuty;
        public long LocalDutyUntilDay;

        public static PermanentPersonCoreRecord FromPerson(PersonState person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            return new PermanentPersonCoreRecord
            {
                PersonId = person.Id,
                DisplayName = person.DisplayName,
                CurrentLocationId = person.LocationId,
                BirthLocationId = person.BirthLocationId,
                FamilyId = person.FamilyId,
                BirthDay = person.BirthDay,
                IsAlive = person.IsAlive,
                HealthBasisPoints = person.HealthBasisPoints,
                Gender = person.Gender,
                FatherPersonId = person.FatherPersonId,
                MotherPersonId = person.MotherPersonId,
                SpousePersonId = person.SpousePersonId,
                CountsTowardPopulation = person.CountsTowardPopulation,
                PopulationOriginLocationId = person.PopulationOriginLocationId,
                VillageOccupation = person.VillageOccupation,
                LaborCapacityBasisPoints = person.LaborCapacityBasisPoints,
                NextIndependentEventDay = person.NextIndependentEventDay,
                NextIndependentEventReason = person.NextIndependentEventReason,
                LocalDuty = person.LocalDuty,
                LocalDutyUntilDay = person.LocalDutyUntilDay
            };
        }

        public bool Matches(PersonState person)
        {
            return person != null &&
                   string.Equals(PersonId, person.Id, StringComparison.Ordinal) &&
                   string.Equals(DisplayName, person.DisplayName, StringComparison.Ordinal) &&
                   string.Equals(CurrentLocationId, person.LocationId, StringComparison.Ordinal) &&
                   string.Equals(BirthLocationId, person.BirthLocationId, StringComparison.Ordinal) &&
                   string.Equals(FamilyId, person.FamilyId, StringComparison.Ordinal) &&
                   BirthDay == person.BirthDay &&
                   IsAlive == person.IsAlive &&
                   HealthBasisPoints == person.HealthBasisPoints &&
                   Gender == person.Gender &&
                   string.Equals(FatherPersonId, person.FatherPersonId, StringComparison.Ordinal) &&
                   string.Equals(MotherPersonId, person.MotherPersonId, StringComparison.Ordinal) &&
                   string.Equals(SpousePersonId, person.SpousePersonId, StringComparison.Ordinal) &&
                   CountsTowardPopulation == person.CountsTowardPopulation &&
                   string.Equals(
                       PopulationOriginLocationId,
                       person.PopulationOriginLocationId,
                       StringComparison.Ordinal) &&
                   VillageOccupation == person.VillageOccupation &&
                   LaborCapacityBasisPoints == person.LaborCapacityBasisPoints &&
                   NextIndependentEventDay == person.NextIndependentEventDay &&
                   string.Equals(
                       NextIndependentEventReason,
                       person.NextIndependentEventReason,
                       StringComparison.Ordinal) &&
                   LocalDuty == person.LocalDuty &&
                   LocalDutyUntilDay == person.LocalDutyUntilDay;
        }
    }

    [Serializable]
    public sealed class PersonDetailExtensionRecord
    {
        public const int CurrentExtensionVersion = 1;

        public int ExtensionVersion = CurrentExtensionVersion;
        public long StorageRevision;
        public PersonState Person;
    }

    [Serializable]
    public sealed class PopulationCheckpoint
    {
        public string PackageId;
        public int PartitionCount;
        public long StorageRevision;
        public List<PermanentPersonCoreRecord> People =
            new List<PermanentPersonCoreRecord>();
        public List<PersonDetailExtensionRecord> DetailExtensions =
            new List<PersonDetailExtensionRecord>();

        public static PopulationCheckpoint FromInlineWorld(
            WorldState world,
            string packageId,
            int partitionCount,
            long storageRevision)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var checkpoint = new PopulationCheckpoint
            {
                PackageId = packageId,
                PartitionCount = partitionCount,
                StorageRevision = storageRevision
            };
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                checkpoint.People.Add(PermanentPersonCoreRecord.FromPerson(person));
                checkpoint.DetailExtensions.Add(new PersonDetailExtensionRecord
                {
                    StorageRevision = storageRevision,
                    Person = person
                });
            }

            return checkpoint;
        }
    }

    [Serializable]
    public sealed class PopulationPartitionManifestEntry
    {
        public int PartitionIndex;
        public int PersonCount;
        public int LivingPersonCount;
        public int DetailExtensionCount;
        public string CoreRelativePath;
        public long CoreLength;
        public string CoreSha256;
        public string DetailRelativePath;
        public long DetailLength;
        public string DetailSha256;
    }

    [Serializable]
    public sealed class PopulationPackageManifest
    {
        public const int CurrentFormatVersion = 1;

        public int FormatVersion = CurrentFormatVersion;
        public string PackageId;
        public int PartitionCount;
        public long StorageRevision;
        public long PermanentPersonCount;
        public long LivingPersonCount;
        public long DetailExtensionCount;
        public List<PopulationPartitionManifestEntry> Partitions =
            new List<PopulationPartitionManifestEntry>();
        public string ManifestSha256;

        public PopulationStorageState ToDomainState()
        {
            return new PopulationStorageState
            {
                Mode = PopulationStorageMode.PartitionedPackage,
                PackageId = PackageId,
                PartitionCount = PartitionCount,
                PermanentPersonCount = PermanentPersonCount,
                LivingPersonCount = LivingPersonCount,
                DetailExtensionCount = DetailExtensionCount,
                StorageRevision = StorageRevision,
                ManifestSha256 = ManifestSha256
            };
        }
    }

    public interface IPermanentPopulationStore
    {
        PopulationPackageManifest CommitCheckpoint(PopulationCheckpoint checkpoint);

        PopulationPackageManifest OpenCurrent();

        bool TryReadCore(string personId, out PermanentPersonCoreRecord person);

        bool TryReadDetail(string personId, out PersonState person);

        IReadOnlyList<PermanentPersonCoreRecord> LoadCorePartition(
            int partitionIndex);
    }
}
