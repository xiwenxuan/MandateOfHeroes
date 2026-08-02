using System;
using Mandate.Domain;
using Newtonsoft.Json;

namespace Mandate.Persistence
{
    public static class WorldSnapshotSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include
        };

        public static string Serialize(
            WorldState world,
            ProductionContentRegistry productionContent = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.PopulationStorage != null &&
                world.PopulationStorage.Mode ==
                PopulationStorageMode.InlineSnapshot)
            {
                world.PopulationStorage.SynchronizeInlineCounts(world.People);
            }
            else if (world.PopulationStorage != null &&
                     world.People.Count >
                     world.PopulationStorage.PermanentPersonCount)
            {
                throw new InvalidOperationException(
                    "Partitioned population contains uncommitted permanent people.");
            }

            (productionContent ?? ProductionContentRegistry.CreateCore())
                .ValidateWorldReferences(world);
            world.Validate();
            return JsonConvert.SerializeObject(world, Settings);
        }

        public static WorldState Deserialize(
            string json,
            ProductionContentRegistry productionContent = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Snapshot JSON cannot be empty.", nameof(json));
            }

            var world = JsonConvert.DeserializeObject<WorldState>(json, Settings)
                ?? throw new InvalidOperationException("Snapshot did not contain a world.");
            world = WorldSnapshotMigrator.MigrateToCurrent(world);
            (productionContent ?? ProductionContentRegistry.CreateCore())
                .ValidateWorldReferences(world);
            world.Validate();
            return world;
        }
    }
}
