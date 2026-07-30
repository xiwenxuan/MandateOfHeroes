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

        public static string Serialize(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            return JsonConvert.SerializeObject(world, Settings);
        }

        public static WorldState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Snapshot JSON cannot be empty.", nameof(json));
            }

            var world = JsonConvert.DeserializeObject<WorldState>(json, Settings)
                ?? throw new InvalidOperationException("Snapshot did not contain a world.");
            world.Validate();
            return world;
        }
    }
}
