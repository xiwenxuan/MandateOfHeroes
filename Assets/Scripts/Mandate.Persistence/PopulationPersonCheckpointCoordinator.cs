using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public sealed class PersonCheckpointCommitResult
    {
        public PopulationPackageManifest Manifest;
        public readonly List<string> CommittedPersonIds = new List<string>();
        public int RewrittenPartitionCount;
    }

    public sealed class PopulationPersonCheckpointCoordinator
    {
        private readonly IPermanentPopulationStore store;
        private readonly PopulationResidencySession residency;

        public PopulationPersonCheckpointCoordinator(
            IPermanentPopulationStore store,
            PopulationResidencySession residency = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.residency = residency;
        }

        public PersonCheckpointCommitResult CommitChangedPeople(
            WorldState world,
            IPersonRepository people,
            long storageRevision)
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
            var changedIds = people.GetChangedPersonIds();
            if (changedIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "No changed people are available for checkpointing.");
            }

            var previous = store.OpenCurrent();
            var checkpoint = new PopulationIncrementalCheckpoint
            {
                StorageRevision = storageRevision
            };
            for (var i = 0; i < changedIds.Count; i++)
            {
                checkpoint.ChangedPeople.Add(
                    people.GetRequired(changedIds[i]));
            }

            var manifest = store.CommitIncrementalCheckpoint(checkpoint);
            var rewrittenPartitions = 0;
            for (var i = 0; i < manifest.Partitions.Count; i++)
            {
                if (!string.Equals(
                        manifest.Partitions[i].CoreRelativePath,
                        previous.Partitions[i].CoreRelativePath,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        manifest.Partitions[i].DetailRelativePath,
                        previous.Partitions[i].DetailRelativePath,
                        StringComparison.Ordinal))
                {
                    rewrittenPartitions++;
                }
            }

            world.PopulationStorage = manifest.ToDomainState();
            residency?.RefreshCommitted(changedIds);
            people.AcceptChanges(changedIds);
            var result = new PersonCheckpointCommitResult
            {
                Manifest = manifest,
                RewrittenPartitionCount = rewrittenPartitions
            };
            result.CommittedPersonIds.AddRange(changedIds);
            return result;
        }
    }
}
