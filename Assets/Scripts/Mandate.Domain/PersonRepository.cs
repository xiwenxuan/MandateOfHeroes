using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public interface IPersonRepository
    {
        IReadOnlyList<PersonState> GetKnownPeople();

        PersonState GetRequired(string personId);

        PersonState GetRequiredForUpdate(string personId);

        bool TryGet(string personId, out PersonState person);

        void Add(PersonState person);

        IReadOnlyList<string> GetAddedPersonIds();

        IReadOnlyList<string> GetChangedPersonIds();

        void AcceptAddedPeople(IEnumerable<string> personIds);

        void AcceptChanges(IEnumerable<string> personIds);
    }

    public sealed class WorldStatePersonRepository : IPersonRepository
    {
        private readonly WorldState world;
        private readonly Dictionary<string, PersonState> people =
            new Dictionary<string, PersonState>(StringComparer.Ordinal);
        private readonly HashSet<string> changedPersonIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> addedPersonIds =
            new HashSet<string>(StringComparer.Ordinal);

        public WorldStatePersonRepository(WorldState world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            IndexWorldPeople(false);
        }

        public IReadOnlyList<PersonState> GetKnownPeople()
        {
            IndexWorldPeople(true);
            var result = new List<PersonState>(people.Values);
            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
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
            if (!addedPersonIds.Contains(person.Id))
            {
                changedPersonIds.Add(person.Id);
            }

            return person;
        }

        public bool TryGet(string personId, out PersonState person)
        {
            _ = new StableId(personId);
            if (people.TryGetValue(personId, out person))
            {
                return true;
            }

            IndexWorldPeople(true);
            if (people.TryGetValue(personId, out person))
            {
                return true;
            }

            person = null;
            return false;
        }

        public void Add(PersonState person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            _ = new StableId(person.Id);
            if (people.ContainsKey(person.Id))
            {
                throw new InvalidOperationException(
                    $"Person {person.Id} already exists.");
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i] == null || world.People[i].Id == person.Id)
                {
                    throw new InvalidOperationException(
                        $"Person {person.Id} already exists or the world is invalid.");
                }
            }

            world.People.Add(person);
            people.Add(person.Id, person);
            addedPersonIds.Add(person.Id);
        }

        public IReadOnlyList<string> GetAddedPersonIds()
        {
            var result = new List<string>(addedPersonIds);
            result.Sort(StringComparer.Ordinal);
            return result;
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

        public void AcceptAddedPeople(IEnumerable<string> personIds)
        {
            if (personIds == null)
            {
                throw new ArgumentNullException(nameof(personIds));
            }

            foreach (var personId in personIds)
            {
                _ = new StableId(personId);
                addedPersonIds.Remove(personId);
            }
        }

        private void IndexWorldPeople(bool trackAdditions)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person == null)
                {
                    throw new InvalidOperationException(
                        "World contains a null person.");
                }

                _ = new StableId(person.Id);
                if (people.TryGetValue(person.Id, out var existing))
                {
                    if (!ReferenceEquals(existing, person))
                    {
                        throw new InvalidOperationException(
                            $"World contains duplicate person {person.Id}.");
                    }

                    continue;
                }

                people.Add(person.Id, person);
                if (trackAdditions)
                {
                    addedPersonIds.Add(person.Id);
                }
            }
        }
    }
}
