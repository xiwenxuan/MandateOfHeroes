using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum NpcActionType
    {
        Work,
        Trade,
        Visit,
        SeekOffice,
        Enlist,
        Flee
    }

    public sealed class NpcActionCommand
    {
        public StableId ActorId { get; }
        public NpcActionType ActionType { get; }
        public StableId TargetId { get; }
        public string Reason { get; }

        public NpcActionCommand(
            StableId actorId,
            NpcActionType actionType,
            StableId targetId,
            string reason)
        {
            ActorId = actorId;
            ActionType = actionType;
            TargetId = targetId;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class NpcActionPlanner
    {
        public NpcActionCommand Plan(
            WorldState world,
            PersonState person,
            NpcDecision decision)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (decision == null)
            {
                throw new ArgumentNullException(nameof(decision));
            }

            world.Validate();
            var actorId = new StableId(person.Id);
            if (actorId != decision.PersonId)
            {
                throw new InvalidOperationException(
                    "The decision belongs to a different person.");
            }

            var currentLocation = FindLocation(world, person.LocationId);
            switch (decision.SelectedFocus)
            {
                case NpcMonthlyFocus.MaintainLivelihood:
                    return new NpcActionCommand(
                        actorId,
                        NpcActionType.Work,
                        new StableId(currentLocation.Id),
                        "优先解决生计，在当前地点寻找收入和口粮。");

                case NpcMonthlyFocus.CareForFamily:
                case NpcMonthlyFocus.MaintainRelationships:
                    return new NpcActionCommand(
                        actorId,
                        NpcActionType.Visit,
                        FindVisitTarget(world, person),
                        "拜访可接触的亲属或关系人物。");

                case NpcMonthlyFocus.ImproveStatus:
                    return new NpcActionCommand(
                        actorId,
                        NpcActionType.SeekOffice,
                        new StableId(currentLocation.Id),
                        "在所在地寻找官府、军队或组织的仕进机会。");

                case NpcMonthlyFocus.AccumulateWealth:
                    return new NpcActionCommand(
                        actorId,
                        NpcActionType.Trade,
                        new StableId(currentLocation.Id),
                        "利用所在地市场尝试积累财富。");

                case NpcMonthlyFocus.RespondToWar:
                    return PlanWarResponse(world, person, actorId, currentLocation);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static NpcActionCommand PlanWarResponse(
            WorldState world,
            PersonState person,
            StableId actorId,
            LocationState currentLocation)
        {
            if (person.Personality.RiskTolerance >= 5_000 &&
                person.HealthBasisPoints >= 5_000)
            {
                return new NpcActionCommand(
                    actorId,
                    NpcActionType.Enlist,
                    new StableId(currentLocation.Id),
                    "战争压力较高，且人物愿意承担风险，尝试应募或加入地方武装。");
            }

            var refuge = FindSafestReachableLocation(world, currentLocation.Id);
            return new NpcActionCommand(
                actorId,
                NpcActionType.Flee,
                new StableId(refuge.Id),
                $"战争压力较高，人物选择前往治安更高的{refuge.DisplayName}避难。");
        }

        private static StableId FindVisitTarget(WorldState world, PersonState person)
        {
            var candidates = new List<PersonState>();
            for (var i = 0; i < world.People.Count; i++)
            {
                var candidate = world.People[i];
                if (candidate.IsAlive &&
                    candidate.Id != person.Id &&
                    candidate.LocationId == person.LocationId)
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort((left, right) =>
            {
                var leftStrength = RelationshipStrength(world, person.Id, left.Id);
                var rightStrength = RelationshipStrength(world, person.Id, right.Id);
                var strengthComparison = rightStrength.CompareTo(leftStrength);
                return strengthComparison != 0
                    ? strengthComparison
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return candidates.Count == 0
                ? new StableId(person.LocationId)
                : new StableId(candidates[0].Id);
        }

        private static int RelationshipStrength(
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
                    return relationship.Affection +
                        relationship.Trust +
                        relationship.Respect +
                        relationship.Obligation;
                }
            }

            return int.MinValue;
        }

        private static LocationState FindSafestReachableLocation(
            WorldState world,
            string currentLocationId)
        {
            LocationState best = null;
            for (var i = 0; i < world.Locations.Count; i++)
            {
                var candidate = world.Locations[i];
                if (candidate.Id == currentLocationId ||
                    !HasDirectRoute(world, currentLocationId, candidate.Id))
                {
                    continue;
                }

                if (best == null ||
                    candidate.PublicOrderBasisPoints > best.PublicOrderBasisPoints ||
                    candidate.PublicOrderBasisPoints == best.PublicOrderBasisPoints &&
                    string.CompareOrdinal(candidate.Id, best.Id) < 0)
                {
                    best = candidate;
                }
            }

            return best ?? FindLocation(world, currentLocationId);
        }

        private static bool HasDirectRoute(
            WorldState world,
            string originId,
            string destinationId)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                var route = world.Routes[i];
                var forward = route.FromLocationId == originId &&
                    route.ToLocationId == destinationId;
                var backward = route.Bidirectional &&
                    route.ToLocationId == originId &&
                    route.FromLocationId == destinationId;
                if (forward || backward)
                {
                    return true;
                }
            }

            return false;
        }

        private static LocationState FindLocation(WorldState world, string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
        }
    }
}
