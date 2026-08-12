using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HanHistoricalPersonClanQuerySystem
    {
        private readonly IHanHistoricalPersonClanSource source;

        public HanHistoricalPersonClanQuerySystem(IHanHistoricalPersonClanSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public HanHistoricalPerson GetPerson(string personId) { return source.GetPerson(personId); }
        public HanHistoricalClan GetClan(string clanId) { return source.GetClan(clanId); }
        public HanHistoricalBranch GetBranch(string branchId) { return source.GetBranch(branchId); }

        public IReadOnlyList<HanHistoricalPerson> GetParents(string personId)
        {
            RequirePerson(personId);
            var parentIds = source.GetKinship()
                .Where(item => item.PersonBId == personId && IsParentRelation(item.RelationType))
                .Select(item => item.PersonAId).Distinct(StringComparer.Ordinal);
            return parentIds.Select(source.GetPerson).ToList();
        }

        public IReadOnlyList<HanHistoricalPerson> GetChildren(string personId)
        {
            RequirePerson(personId);
            var childIds = source.GetKinship()
                .Where(item => item.PersonAId == personId && IsParentRelation(item.RelationType))
                .Select(item => item.PersonBId).Distinct(StringComparer.Ordinal);
            return childIds.Select(source.GetPerson).ToList();
        }

        public IReadOnlyList<HanHistoricalPerson> GetSpouses(string personId)
        {
            RequirePerson(personId);
            var ids = source.GetMarriages().Where(item => item.PersonAId == personId || item.PersonBId == personId)
                .Select(item => item.PersonAId == personId ? item.PersonBId : item.PersonAId)
                .Distinct(StringComparer.Ordinal);
            return ids.Select(source.GetPerson).ToList();
        }

        public IReadOnlyList<HanHistoricalPerson> GetSiblings(string personId)
        {
            RequirePerson(personId);
            var explicitIds = source.GetKinship().Where(item => item.RelationType == "Sibling" && (item.PersonAId == personId || item.PersonBId == personId))
                .Select(item => item.PersonAId == personId ? item.PersonBId : item.PersonAId);
            var parentIds = new HashSet<string>(GetParents(personId).Select(item => item.PersonId), StringComparer.Ordinal);
            var derivedIds = parentIds.SelectMany(parentId => GetChildren(parentId)).Where(item => item.PersonId != personId).Select(item => item.PersonId);
            return explicitIds.Concat(derivedIds).Distinct(StringComparer.Ordinal).Select(source.GetPerson).ToList();
        }

        public IReadOnlyList<HanHistoricalPerson> GetAncestors(string personId, int depth)
        {
            return Traverse(personId, depth, GetParents);
        }

        public IReadOnlyList<HanHistoricalPerson> GetDescendants(string personId, int depth)
        {
            return Traverse(personId, depth, GetChildren);
        }

        public HanHistoricalLocationRecord GetPersonLocation(string personId, int year)
        {
            RequireYear(year);
            RequirePerson(personId);
            var matches = source.GetLocations().Where(item => item.PersonId == personId && item.ContainsYear(year)).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        public IReadOnlyList<HanHistoricalCivilOfficeRecord> GetPersonOffice(string personId, int year)
        {
            RequireYear(year);
            RequirePerson(personId);
            return source.GetCivilOffices().Where(item => item.PersonId == personId && item.ContainsYear(year)).ToList();
        }

        public IReadOnlyList<HanHistoricalMilitaryOfficeRecord> GetPersonMilitaryOffice(string personId, int year)
        {
            RequireYear(year);
            RequirePerson(personId);
            return source.GetMilitaryOffices().Where(item => item.PersonId == personId && item.ContainsYear(year)).ToList();
        }

        public IReadOnlyList<HanHistoricalTitleRecord> GetPersonTitle(string personId, int year)
        {
            RequireYear(year);
            RequirePerson(personId);
            return source.GetTitles().Where(item => item.PersonId == personId && item.ContainsYear(year)).ToList();
        }

        public IReadOnlyList<HanHistoricalAllegianceRecord> GetPersonAllegiance(string personId, int year)
        {
            RequireYear(year);
            RequirePerson(personId);
            return source.GetAllegiances().Where(item => item.PersonId == personId && item.ContainsYear(year)).ToList();
        }

        public IReadOnlyList<HanHistoricalPerson> GetClanMembers(string clanId, int year)
        {
            RequireYear(year);
            source.GetClan(clanId);
            var living = new HashSet<string>(source.LoadHistoricalSnapshot(year).Persons.Select(item => item.PersonId), StringComparer.Ordinal);
            return source.GetPeople().Where(item => item.ClanId == clanId && living.Contains(item.PersonId)).ToList();
        }

        public IReadOnlyList<HanHistoricalBranch> GetClanBranches(string clanId)
        {
            source.GetClan(clanId);
            return source.GetBranches().Where(item => item.ClanId == clanId).ToList();
        }

        public IReadOnlyList<HanHistoricalClanPresenceRecord> GetClanPresence(string clanId, int year)
        {
            RequireYear(year);
            source.GetClan(clanId);
            return source.GetClanPresence().Where(item => item.ClanId == clanId && item.ContainsYear(year)).ToList();
        }

        public HanHistoricalScenarioSnapshot LoadHistoricalPersonSnapshot(int year) { RequireYear(year); return source.LoadHistoricalSnapshot(year); }
        public HanHistoricalScenarioSnapshot LoadHistoricalClanSnapshot(int year) { RequireYear(year); return source.LoadHistoricalSnapshot(year); }
        public HanHistoricalScenarioSnapshot LoadScenarioSnapshot(string scenarioId) { return source.LoadScenarioSnapshot(scenarioId); }

        private IReadOnlyList<HanHistoricalPerson> Traverse(string personId, int depth, Func<string, IReadOnlyList<HanHistoricalPerson>> step)
        {
            RequirePerson(personId);
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
            var visited = new HashSet<string>(StringComparer.Ordinal) { personId };
            var frontier = new List<string> { personId };
            var result = new List<HanHistoricalPerson>();
            for (var level = 0; level < depth && frontier.Count > 0; level++)
            {
                var next = new List<string>();
                foreach (var current in frontier)
                {
                    foreach (var person in step(current))
                    {
                        if (!visited.Add(person.PersonId)) continue;
                        result.Add(person);
                        next.Add(person.PersonId);
                    }
                }
                frontier = next;
            }
            return result;
        }

        private HanHistoricalPerson RequirePerson(string personId)
        {
            if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("A PersonId is required.", nameof(personId));
            return source.GetPerson(personId);
        }

        private static bool IsParentRelation(string type)
        {
            return type == "BiologicalFather" || type == "BiologicalMother" || type == "AdoptiveFather"
                || type == "AdoptiveMother" || type == "BiologicalParentUnspecified";
        }

        private static void RequireYear(int year)
        {
            if (year < 135 || year > 260) throw new ArgumentOutOfRangeException(nameof(year));
        }
    }
}
