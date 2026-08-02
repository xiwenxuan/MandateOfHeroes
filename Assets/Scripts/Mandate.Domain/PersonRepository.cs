using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public interface IPersonRepository
    {
        PersonState GetRequired(string personId);

        PersonState GetRequiredForUpdate(string personId);

        bool TryGet(string personId, out PersonState person);

        IReadOnlyList<string> GetChangedPersonIds();

        void AcceptChanges(IEnumerable<string> personIds);
    }

    public sealed class WorldStatePersonRepository : IPersonRepository
    {
        private readonly WorldState world;
        private readonly Dictionary<string, PersonState> people =
            new Dictionary<string, PersonState>(StringComparer.Ordinal);
        private readonly HashSet<string> changedPersonIds =
            new HashSet<string>(StringComparer.Ordinal);

        public WorldStatePersonRepository(WorldState world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person == null || people.ContainsKey(person.Id))
                {
                    throw new InvalidOperationException(
                        "World contains a null or duplicate person.");
                }

                people.Add(person.Id, person);
            }
        }

        public PersonState GetRequired(string personId)
        {
            if (TryGet(personId, out var person))
            {
                return person;
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        public PersonState GetRequiredForUpdate(string personId)
        {
            var person = GetRequired(personId);
            changedPersonIds.Add(person.Id);
            return person;
        }

        public bool TryGet(string personId, out PersonState person)
        {
            _ = new StableId(personId);
            if (people.TryGetValue(personId, out person))
            {
                return true;
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var candidate = world.People[i];
                if (candidate.Id == personId)
                {
                    people.Add(candidate.Id, candidate);
                    person = candidate;
                    return true;
                }
            }

            person = null;
            return false;
        }

        public IReadOnlyList<string> GetChangedPersonIds()
        {
            var result = new List<string>(changedPersonIds);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public void AcceptChanges(IEnumerable<string> personIds)
        {
            if (personIds == null)
            {
                throw new ArgumentNullException(nameof(personIds));
            }

            foreach (var personId in personIds)
            {
                _ = new StableId(personId);
                changedPersonIds.Remove(personId);
            }
        }
    }
}
