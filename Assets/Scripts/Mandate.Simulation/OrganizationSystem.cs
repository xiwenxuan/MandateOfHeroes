using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class OrganizationJoinResult
    {
        public bool Success { get; }
        public MembershipState Membership { get; }
        public string Message { get; }

        public OrganizationJoinResult(
            bool success,
            MembershipState membership,
            string message)
        {
            Success = success;
            Membership = membership;
            Message = message ?? string.Empty;
        }
    }

    public sealed class OrganizationSystem
    {
        public OrganizationJoinResult TryJoinAtCurrentLocation(
            WorldState world,
            StableId personId,
            OrganizationType organizationType)
        {
            var person = FindPerson(world, personId.Value);
            if (!person.IsAlive)
            {
                return new OrganizationJoinResult(false, null, "死亡人物不能加入组织。");
            }

            if (IsAlreadyMemberOfType(world, person.Id, organizationType))
            {
                return new OrganizationJoinResult(
                    false, null, "人物已经属于此类组织。");
            }

            var organizations = new List<OrganizationState>();
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                var organization = world.Organizations[i];
                if (organization.Type == organizationType &&
                    organization.HeadquartersLocationId == person.LocationId)
                {
                    organizations.Add(organization);
                }
            }

            organizations.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < organizations.Count; i++)
            {
                var position = FindEntryPositionWithCapacity(world, organizations[i].Id);
                if (position == null)
                {
                    continue;
                }

                var membership = new MembershipState
                {
                    Id = $"membership.{person.Id}.{organizations[i].Id}",
                    PersonId = person.Id,
                    OrganizationId = organizations[i].Id,
                    PositionId = position.Id,
                    JoinedDay = world.AbsoluteDay,
                    LoyaltyBasisPoints = 5_000
                };
                world.Memberships.Add(membership);
                world.Validate();
                return new OrganizationJoinResult(
                    true,
                    membership,
                    $"加入{organizations[i].DisplayName}，担任{position.DisplayName}。");
            }

            return new OrganizationJoinResult(
                false, null, "所在地没有对应组织，或入门职位已经满员。");
        }

        private static PositionState FindEntryPositionWithCapacity(
            WorldState world,
            string organizationId)
        {
            var positions = new List<PositionState>();
            for (var i = 0; i < world.Positions.Count; i++)
            {
                if (world.Positions[i].OrganizationId == organizationId)
                {
                    positions.Add(world.Positions[i]);
                }
            }

            positions.Sort((left, right) =>
            {
                var rank = left.Rank.CompareTo(right.Rank);
                return rank != 0 ? rank : string.CompareOrdinal(left.Id, right.Id);
            });

            for (var i = 0; i < positions.Count; i++)
            {
                var occupied = 0;
                for (var memberIndex = 0; memberIndex < world.Memberships.Count; memberIndex++)
                {
                    if (world.Memberships[memberIndex].PositionId == positions[i].Id)
                    {
                        occupied++;
                    }
                }

                if (occupied < positions[i].Capacity)
                {
                    return positions[i];
                }
            }

            return null;
        }

        private static bool IsAlreadyMemberOfType(
            WorldState world,
            string personId,
            OrganizationType organizationType)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId != personId)
                {
                    continue;
                }

                for (var organizationIndex = 0;
                     organizationIndex < world.Organizations.Count;
                     organizationIndex++)
                {
                    var organization = world.Organizations[organizationIndex];
                    if (organization.Id == membership.OrganizationId &&
                        organization.Type == organizationType)
                    {
                        return true;
                    }
                }
            }

            return false;
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
    }
}
