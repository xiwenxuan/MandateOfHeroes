using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class RelationshipSystem
    {
        private readonly NamedRandom _random;
        private readonly IPersonRepository _people;

        public RelationshipSystem(
            ulong masterSeed,
            IPersonRepository people = null)
        {
            _random = new NamedRandom(masterSeed);
            _people = people;
        }

        public int ResolveVisit(
            WorldState world,
            StableId actorId,
            StableId targetId,
            long monthIndex)
        {
            var people = _people ?? new WorldStatePersonRepository(world);
            var actor = people.GetRequired(actorId.Value);
            var target = people.GetRequired(targetId.Value);
            if (!actor.IsAlive || !target.IsAlive)
            {
                throw new InvalidOperationException("A visit requires two living people.");
            }

            if (actor.LocationId != target.LocationId)
            {
                throw new InvalidOperationException("A visit requires both people at one location.");
            }

            var baseGain = 80 +
                actor.Personality.Sociability / 200 +
                actor.Personality.Benevolence / 250;
            var variation = _random.Range(
                "relationship",
                actorId,
                monthIndex,
                "visit_" + targetId.Value,
                0,
                61);
            var gain = baseGain + variation;

            var outgoing = GetOrCreate(world, actorId.Value, targetId.Value);
            outgoing.Affection = ClampRelationship(outgoing.Affection + gain);
            outgoing.Trust = ClampRelationship(outgoing.Trust + gain / 2);
            outgoing.Respect = ClampRelationship(outgoing.Respect + gain / 4);
            outgoing.LastInteractionDay = world.AbsoluteDay;

            var response = GetOrCreate(world, targetId.Value, actorId.Value);
            var responseGain = Math.Max(
                20,
                gain / 2 + target.Personality.Sociability / 400);
            response.Affection = ClampRelationship(response.Affection + responseGain);
            response.Trust = ClampRelationship(response.Trust + responseGain / 3);
            response.LastInteractionDay = world.AbsoluteDay;

            actor = people.GetRequiredForUpdate(actor.Id);
            actor.Needs.Relationships = Math.Max(
                0,
                actor.Needs.Relationships - 1_000);
            return gain;
        }

        private static RelationshipState GetOrCreate(
            WorldState world,
            string fromPersonId,
            string toPersonId)
        {
            for (var i = 0; i < world.Relationships.Count; i++)
            {
                var relationship = world.Relationships[i];
                if (relationship.FromPersonId == fromPersonId &&
                    relationship.ToPersonId == toPersonId)
                {
                    return relationship;
                }
            }

            var created = new RelationshipState
            {
                Id = $"relationship.{fromPersonId}.{toPersonId}",
                FromPersonId = fromPersonId,
                ToPersonId = toPersonId
            };
            world.Relationships.Add(created);
            return created;
        }

        private static int ClampRelationship(int value)
        {
            if (value < -10_000)
            {
                return -10_000;
            }

            return value > 10_000 ? 10_000 : value;
        }
    }
}
