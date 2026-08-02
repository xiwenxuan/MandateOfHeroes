using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LocalRelationshipNetworkSnapshot
    {
        public string CenterPersonId;
        public readonly List<string> PersonIds = new List<string>();
        public readonly List<string> FamilyIds = new List<string>();
        public readonly List<string> VillageIds = new List<string>();
        public readonly List<string> OrganizationIds = new List<string>();
        public readonly List<string> ExplicitRelationshipIds =
            new List<string>();
    }

    public sealed class AttentionResidencyPlan
    {
        public string ObserverPersonId;
        public int MaximumHotPeople;
        public readonly List<string> HotPersonIds = new List<string>();
    }

    public sealed class AttentionSystem
    {
        public const string ManualReasonId = "attention.reason.manual";

        public AttentionFocusState SetReason(
            WorldState world,
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId,
            string reasonId,
            AttentionLevel level)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            ValidateIdentity(world, observerPersonId, targetKind, targetId, reasonId);
            if (!Enum.IsDefined(typeof(AttentionLevel), level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            var existing = FindFocus(
                world, observerPersonId, targetKind, targetId, reasonId);
            if (level == AttentionLevel.None)
            {
                if (existing != null)
                {
                    RemoveExisting(world, existing);
                }

                return null;
            }

            if (existing != null)
            {
                if (existing.Level == level)
                {
                    return existing;
                }

                var previous = existing.Level;
                existing.Level = level;
                existing.LastChangedDay = world.AbsoluteDay;
                AddLedger(
                    world,
                    existing,
                    AttentionLedgerChangeKind.Updated,
                    previous,
                    level);
                return existing;
            }

            var created = new AttentionFocusState
            {
                Id = BuildFocusId(
                    observerPersonId, targetKind, targetId, reasonId),
                ObserverPersonId = observerPersonId,
                TargetKind = targetKind,
                TargetId = targetId,
                Level = level,
                ReasonId = reasonId,
                CreatedDay = world.AbsoluteDay,
                LastChangedDay = world.AbsoluteDay
            };
            world.AttentionFocuses.Add(created);
            AddLedger(
                world,
                created,
                AttentionLedgerChangeKind.Added,
                AttentionLevel.None,
                level);
            return created;
        }

        public void ClearReason(
            WorldState world,
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId,
            string reasonId)
        {
            SetReason(
                world,
                observerPersonId,
                targetKind,
                targetId,
                reasonId,
                AttentionLevel.None);
        }

        public AttentionLevel GetEffectiveLevel(
            WorldState world,
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId)
        {
            var result = AttentionLevel.None;
            for (var i = 0; i < world.AttentionFocuses.Count; i++)
            {
                var focus = world.AttentionFocuses[i];
                if (focus.ObserverPersonId == observerPersonId &&
                    focus.TargetKind == targetKind &&
                    focus.TargetId == targetId &&
                    focus.Level > result)
                {
                    result = focus.Level;
                }
            }

            return result;
        }

        public LocalRelationshipNetworkSnapshot BuildLocalRelationshipNetwork(
            WorldState world,
            string centerPersonId,
            int maximumPeople = 128)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (maximumPeople <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPeople));
            }

            var center = FindPerson(world, centerPersonId);
            var snapshot = new LocalRelationshipNetworkSnapshot
            {
                CenterPersonId = center.Id
            };
            var people = new HashSet<string>(StringComparer.Ordinal);
            AddPerson(snapshot, people, center.Id, maximumPeople);

            var family = FindFamily(world, center.FamilyId);
            if (family != null)
            {
                snapshot.FamilyIds.Add(family.Id);
                AddSortedPeople(
                    snapshot, people, family.MemberIds, maximumPeople);
                if (!string.IsNullOrEmpty(family.VillageId))
                {
                    snapshot.VillageIds.Add(family.VillageId);
                }
            }

            var directPeople = new List<string>();
            for (var i = 0; i < world.Relationships.Count; i++)
            {
                var relationship = world.Relationships[i];
                if (relationship.FromPersonId == center.Id)
                {
                    directPeople.Add(relationship.ToPersonId);
                }
                else if (relationship.ToPersonId == center.Id)
                {
                    directPeople.Add(relationship.FromPersonId);
                }
            }

            AddSortedPeople(snapshot, people, directPeople, maximumPeople);

            var organizationIds = OrganizationsForPerson(world, center.Id);
            for (var i = 0; i < organizationIds.Count; i++)
            {
                snapshot.OrganizationIds.Add(organizationIds[i]);
                AddSortedPeople(
                    snapshot,
                    people,
                    MembersForOrganization(world, organizationIds[i]),
                    maximumPeople);
            }

            if (family != null && !string.IsNullOrEmpty(family.VillageId))
            {
                var village = FindVillage(world, family.VillageId);
                if (village != null)
                {
                    var villagePeople = new List<string>();
                    for (var i = 0; i < village.HouseholdIds.Count; i++)
                    {
                        var household = FindFamily(world, village.HouseholdIds[i]);
                        if (household != null)
                        {
                            villagePeople.AddRange(household.MemberIds);
                        }
                    }

                    AddSortedPeople(
                        snapshot, people, villagePeople, maximumPeople);
                }
            }

            var relationshipIds = new List<string>();
            for (var i = 0; i < world.Relationships.Count; i++)
            {
                var relationship = world.Relationships[i];
                if (people.Contains(relationship.FromPersonId) &&
                    people.Contains(relationship.ToPersonId))
                {
                    relationshipIds.Add(relationship.Id);
                }
            }

            relationshipIds.Sort(StringComparer.Ordinal);
            snapshot.ExplicitRelationshipIds.AddRange(relationshipIds);
            return snapshot;
        }

        public AttentionResidencyPlan BuildResidencyPlan(
            WorldState world,
            string observerPersonId,
            int maximumHotPeople = 256)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (maximumHotPeople <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHotPeople));
            }

            _ = FindPerson(world, observerPersonId);
            var plan = new AttentionResidencyPlan
            {
                ObserverPersonId = observerPersonId,
                MaximumHotPeople = maximumHotPeople
            };
            var hot = new HashSet<string>(StringComparer.Ordinal);
            AddHot(plan, hot, observerPersonId, maximumHotPeople);

            var effective = EffectiveFocuses(world, observerPersonId);
            for (var i = 0; i < effective.Count; i++)
            {
                AddPrimaryTargetPeople(
                    world, plan, hot, effective[i], maximumHotPeople);
            }

            for (var i = 0; i < effective.Count; i++)
            {
                if (effective[i].Level == AttentionLevel.Deep)
                {
                    AddDeepTargetPeople(
                        world, plan, hot, effective[i], maximumHotPeople);
                }
            }

            return plan;
        }

        private void AddDeepTargetPeople(
            WorldState world,
            AttentionResidencyPlan plan,
            HashSet<string> hot,
            EffectiveFocus focus,
            int maximumHotPeople)
        {
            switch (focus.TargetKind)
            {
                case AttentionTargetKind.Person:
                    var network = BuildLocalRelationshipNetwork(
                        world, focus.TargetId, maximumHotPeople);
                    AddSortedHot(
                        plan, hot, network.PersonIds, maximumHotPeople);
                    break;
                case AttentionTargetKind.Family:
                    var family = FindFamily(world, focus.TargetId);
                    AddSortedHot(
                        plan, hot, family.MemberIds, maximumHotPeople);
                    break;
                case AttentionTargetKind.Village:
                    var village = FindVillage(world, focus.TargetId);
                    var villagePeople = new List<string>();
                    for (var i = 0; i < village.HouseholdIds.Count; i++)
                    {
                        var household = FindFamily(world, village.HouseholdIds[i]);
                        villagePeople.AddRange(household.MemberIds);
                    }

                    AddSortedHot(
                        plan, hot, villagePeople, maximumHotPeople);
                    break;
                case AttentionTargetKind.Organization:
                    AddSortedHot(
                        plan,
                        hot,
                        MembersForOrganization(world, focus.TargetId),
                        maximumHotPeople);
                    break;
            }
        }

        private static void AddPrimaryTargetPeople(
            WorldState world,
            AttentionResidencyPlan plan,
            HashSet<string> hot,
            EffectiveFocus focus,
            int maximumHotPeople)
        {
            switch (focus.TargetKind)
            {
                case AttentionTargetKind.Person:
                    AddHot(plan, hot, focus.TargetId, maximumHotPeople);
                    break;
                case AttentionTargetKind.Family:
                    AddHot(
                        plan,
                        hot,
                        FindFamily(world, focus.TargetId).HeadPersonId,
                        maximumHotPeople);
                    break;
                case AttentionTargetKind.Village:
                    var village = FindVillage(world, focus.TargetId);
                    var primary = new List<string>();
                    for (var i = 0; i < village.HouseholdIds.Count; i++)
                    {
                        primary.Add(
                            FindFamily(world, village.HouseholdIds[i]).HeadPersonId);
                    }

                    for (var i = 0; i < world.VillageFacilities.Count; i++)
                    {
                        var facility = world.VillageFacilities[i];
                        if (facility.VillageId == village.Id &&
                            !string.IsNullOrEmpty(facility.ManagerPersonId))
                        {
                            primary.Add(facility.ManagerPersonId);
                        }
                    }

                    AddSortedHot(plan, hot, primary, maximumHotPeople);
                    break;
                case AttentionTargetKind.Facility:
                    var targetFacility = FindFacility(world, focus.TargetId);
                    if (!string.IsNullOrEmpty(targetFacility.ManagerPersonId))
                    {
                        AddHot(
                            plan,
                            hot,
                            targetFacility.ManagerPersonId,
                            maximumHotPeople);
                    }

                    break;
                case AttentionTargetKind.Organization:
                    var organization = FindOrganization(world, focus.TargetId);
                    if (!string.IsNullOrEmpty(organization.LeaderPersonId))
                    {
                        AddHot(
                            plan,
                            hot,
                            organization.LeaderPersonId,
                            maximumHotPeople);
                    }

                    break;
            }
        }

        private static List<EffectiveFocus> EffectiveFocuses(
            WorldState world,
            string observerPersonId)
        {
            var focuses = new List<AttentionFocusState>();
            for (var i = 0; i < world.AttentionFocuses.Count; i++)
            {
                if (world.AttentionFocuses[i].ObserverPersonId == observerPersonId)
                {
                    focuses.Add(world.AttentionFocuses[i]);
                }
            }

            focuses.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            var byTarget = new Dictionary<string, EffectiveFocus>(
                StringComparer.Ordinal);
            for (var i = 0; i < focuses.Count; i++)
            {
                var focus = focuses[i];
                var key = ((byte)focus.TargetKind).ToString() + "|" + focus.TargetId;
                if (!byTarget.TryGetValue(key, out var current))
                {
                    byTarget.Add(key, new EffectiveFocus
                    {
                        Key = key,
                        TargetKind = focus.TargetKind,
                        TargetId = focus.TargetId,
                        Level = focus.Level
                    });
                }
                else if (focus.Level > current.Level)
                {
                    current.Level = focus.Level;
                }
            }

            var result = new List<EffectiveFocus>(byTarget.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static void ValidateIdentity(
            WorldState world,
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId,
            string reasonId)
        {
            _ = FindPerson(world, observerPersonId);
            _ = new StableId(targetId);
            _ = new StableId(reasonId);
            if (!Enum.IsDefined(typeof(AttentionTargetKind), targetKind))
            {
                throw new ArgumentOutOfRangeException(nameof(targetKind));
            }

            switch (targetKind)
            {
                case AttentionTargetKind.Person:
                    _ = FindPerson(world, targetId);
                    break;
                case AttentionTargetKind.Family:
                    _ = FindFamily(world, targetId);
                    break;
                case AttentionTargetKind.Village:
                    _ = FindVillage(world, targetId);
                    break;
                case AttentionTargetKind.Facility:
                    _ = FindFacility(world, targetId);
                    break;
                case AttentionTargetKind.Organization:
                    _ = FindOrganization(world, targetId);
                    break;
            }
        }

        private static AttentionFocusState FindFocus(
            WorldState world,
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId,
            string reasonId)
        {
            for (var i = 0; i < world.AttentionFocuses.Count; i++)
            {
                var focus = world.AttentionFocuses[i];
                if (focus.ObserverPersonId == observerPersonId &&
                    focus.TargetKind == targetKind &&
                    focus.TargetId == targetId &&
                    focus.ReasonId == reasonId)
                {
                    return focus;
                }
            }

            return null;
        }

        private static void RemoveExisting(
            WorldState world,
            AttentionFocusState existing)
        {
            world.AttentionFocuses.Remove(existing);
            AddLedger(
                world,
                existing,
                AttentionLedgerChangeKind.Removed,
                existing.Level,
                AttentionLevel.None);
        }

        private static void AddLedger(
            WorldState world,
            AttentionFocusState focus,
            AttentionLedgerChangeKind changeKind,
            AttentionLevel previousLevel,
            AttentionLevel newLevel)
        {
            world.AttentionLedgerEntries.Add(new AttentionLedgerEntryState
            {
                Id = $"attention_ledger.{world.AbsoluteDay}.{world.AttentionLedgerEntries.Count:D8}",
                Day = world.AbsoluteDay,
                ObserverPersonId = focus.ObserverPersonId,
                TargetKind = focus.TargetKind,
                TargetId = focus.TargetId,
                ReasonId = focus.ReasonId,
                ChangeKind = changeKind,
                PreviousLevel = previousLevel,
                NewLevel = newLevel
            });
        }

        private static string BuildFocusId(
            string observerPersonId,
            AttentionTargetKind targetKind,
            string targetId,
            string reasonId) =>
            $"attention_focus.{observerPersonId}.{targetKind.ToString().ToLowerInvariant()}." +
            $"{targetId}.{reasonId}";

        private static void AddSortedPeople(
            LocalRelationshipNetworkSnapshot snapshot,
            HashSet<string> people,
            IEnumerable<string> candidates,
            int maximumPeople)
        {
            var sorted = SortedDistinct(candidates);
            for (var i = 0; i < sorted.Count; i++)
            {
                AddPerson(snapshot, people, sorted[i], maximumPeople);
            }
        }

        private static void AddSortedHot(
            AttentionResidencyPlan plan,
            HashSet<string> hot,
            IEnumerable<string> candidates,
            int maximumHotPeople)
        {
            var sorted = SortedDistinct(candidates);
            for (var i = 0; i < sorted.Count; i++)
            {
                AddHot(plan, hot, sorted[i], maximumHotPeople);
            }
        }

        private static List<string> SortedDistinct(IEnumerable<string> values)
        {
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    unique.Add(value);
                }
            }

            var result = new List<string>(unique);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static void AddPerson(
            LocalRelationshipNetworkSnapshot snapshot,
            HashSet<string> people,
            string personId,
            int maximumPeople)
        {
            if (snapshot.PersonIds.Count < maximumPeople && people.Add(personId))
            {
                snapshot.PersonIds.Add(personId);
            }
        }

        private static void AddHot(
            AttentionResidencyPlan plan,
            HashSet<string> hot,
            string personId,
            int maximumHotPeople)
        {
            if (plan.HotPersonIds.Count < maximumHotPeople && hot.Add(personId))
            {
                plan.HotPersonIds.Add(personId);
            }
        }

        private static List<string> OrganizationsForPerson(
            WorldState world,
            string personId)
        {
            var result = new List<string>();
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == personId)
                {
                    result.Add(world.Memberships[i].OrganizationId);
                }
            }

            return SortedDistinct(result);
        }

        private static List<string> MembersForOrganization(
            WorldState world,
            string organizationId)
        {
            var result = new List<string>();
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].OrganizationId == organizationId)
                {
                    result.Add(world.Memberships[i].PersonId);
                }
            }

            return SortedDistinct(result);
        }

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }

        private static FamilyState FindFamily(WorldState world, string familyId)
        {
            if (string.IsNullOrEmpty(familyId))
            {
                return null;
            }

            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == familyId)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {familyId}.");
        }

        private static VillageState FindVillage(WorldState world, string villageId)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == villageId)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {villageId}.");
        }

        private static VillageFacilityState FindFacility(
            WorldState world,
            string facilityId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                if (world.VillageFacilities[i].Id == facilityId)
                {
                    return world.VillageFacilities[i];
                }
            }

            throw new InvalidOperationException($"Missing facility {facilityId}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private sealed class EffectiveFocus
        {
            public string Key;
            public AttentionTargetKind TargetKind;
            public string TargetId;
            public AttentionLevel Level;
        }
    }
}
