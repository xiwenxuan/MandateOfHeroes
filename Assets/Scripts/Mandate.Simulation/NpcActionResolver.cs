using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum NpcActionResolutionStatus
    {
        Completed,
        StartedJourney,
        Deferred,
        Rejected
    }

    public sealed class NpcActionOutcome
    {
        public StableId ActorId { get; }
        public NpcActionType ActionType { get; }
        public NpcActionResolutionStatus Status { get; }
        public long WealthChange { get; }
        public int ProvisionsChange { get; }
        public string Summary { get; }

        public NpcActionOutcome(
            StableId actorId,
            NpcActionType actionType,
            NpcActionResolutionStatus status,
            long wealthChange,
            int provisionsChange,
            string summary)
        {
            ActorId = actorId;
            ActionType = actionType;
            Status = status;
            WealthChange = wealthChange;
            ProvisionsChange = provisionsChange;
            Summary = summary ?? string.Empty;
        }
    }

    public sealed class NpcActionResolver
    {
        private readonly NamedRandom _random;
        private readonly NpcActionValidator _validator = new NpcActionValidator();
        private readonly TravelSystem _travelSystem;
        private readonly RelationshipSystem _relationshipSystem;
        private readonly OrganizationSystem _organizationSystem =
            new OrganizationSystem();

        public NpcActionResolver(
            ulong masterSeed,
            IPersonRepository people = null)
        {
            _random = new NamedRandom(masterSeed);
            _travelSystem = new TravelSystem(people);
            _relationshipSystem = new RelationshipSystem(masterSeed, people);
        }

        public NpcActionOutcome Resolve(
            WorldState world,
            NpcActionCommand command,
            long monthIndex)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (monthIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(monthIndex));
            }

            var validation = _validator.Validate(world, command);
            if (!validation.IsValid)
            {
                return new NpcActionOutcome(
                    command.ActorId,
                    command.ActionType,
                    NpcActionResolutionStatus.Rejected,
                    0,
                    0,
                    validation.Error);
            }

            var actor = FindPerson(world, command.ActorId.Value);
            switch (command.ActionType)
            {
                case NpcActionType.Work:
                    return ResolveWork(actor, command, monthIndex);
                case NpcActionType.Trade:
                    return ResolveTrade(world, actor, command, monthIndex);
                case NpcActionType.Flee:
                    return ResolveFlee(world, actor, command);
                case NpcActionType.Visit:
                    return ResolveVisit(world, command, monthIndex);
                case NpcActionType.SeekOffice:
                    return ResolveJoinOrganization(
                        world, command, OrganizationType.Government);
                case NpcActionType.Enlist:
                    return ResolveJoinOrganization(
                        world, command, OrganizationType.Military);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private NpcActionOutcome ResolveWork(
            PersonState actor,
            NpcActionCommand command,
            long monthIndex)
        {
            var income = _random.Range(
                "npc_action", command.ActorId, monthIndex, "work_income", 30, 81);
            var provisions = _random.Range(
                "npc_action", command.ActorId, monthIndex, "work_provisions", 3, 8);

            actor.Wealth = checked(actor.Wealth + income);
            actor.Provisions = checked(actor.Provisions + provisions);
            actor.Needs.Livelihood = Math.Max(0, actor.Needs.Livelihood - 1_500);
            return new NpcActionOutcome(
                command.ActorId,
                command.ActionType,
                NpcActionResolutionStatus.Completed,
                income,
                provisions,
                $"务工完成，获得{income}钱与{provisions}份口粮。");
        }

        private NpcActionOutcome ResolveTrade(
            WorldState world,
            PersonState actor,
            NpcActionCommand command,
            long monthIndex)
        {
            if (actor.Wealth < 20)
            {
                return new NpcActionOutcome(
                    command.ActorId,
                    command.ActionType,
                    NpcActionResolutionStatus.Rejected,
                    0,
                    0,
                    "本金不足20钱，无法进行本月交易。");
            }

            var location = FindLocation(world, actor.LocationId);
            var volatility = Math.Max(10, location.GrainPrice / 3);
            var profit = _random.Range(
                "npc_action",
                command.ActorId,
                monthIndex,
                "trade_profit",
                -volatility,
                volatility + 1);
            profit = (int)Math.Max(-actor.Wealth, profit);
            actor.Wealth += profit;
            actor.Needs.Wealth = Math.Max(0, actor.Needs.Wealth - 750);

            var text = profit >= 0
                ? $"交易完成，获利{profit}钱。"
                : $"交易完成，亏损{-profit}钱。";
            return new NpcActionOutcome(
                command.ActorId,
                command.ActionType,
                NpcActionResolutionStatus.Completed,
                profit,
                0,
                text);
        }

        private NpcActionOutcome ResolveFlee(
            WorldState world,
            PersonState actor,
            NpcActionCommand command)
        {
            var route = FindDirectRoute(world, actor.LocationId, command.TargetId.Value);
            _travelSystem.StartJourney(
                world,
                command.ActorId,
                new StableId(route.Id),
                command.TargetId,
                TravelMode.Foot);
            return new NpcActionOutcome(
                command.ActorId,
                command.ActionType,
                NpcActionResolutionStatus.StartedJourney,
                0,
                0,
                $"开始沿{route.Id}前往{command.TargetId.Value}避难。");
        }

        private NpcActionOutcome ResolveVisit(
            WorldState world,
            NpcActionCommand command,
            long monthIndex)
        {
            if (FindPersonOrNull(world, command.TargetId.Value) == null)
            {
                return new NpcActionOutcome(
                    command.ActorId,
                    command.ActionType,
                    NpcActionResolutionStatus.Deferred,
                    0,
                    0,
                    "所在地没有可拜访人物，本月改为独处休整。");
            }

            var affectionGain = _relationshipSystem.ResolveVisit(
                world,
                command.ActorId,
                command.TargetId,
                monthIndex);
            var target = FindPerson(world, command.TargetId.Value);
            return new NpcActionOutcome(
                command.ActorId,
                command.ActionType,
                NpcActionResolutionStatus.Completed,
                0,
                0,
                $"拜访{target.DisplayName}，好感增加{affectionGain}。");
        }

        private NpcActionOutcome ResolveJoinOrganization(
            WorldState world,
            NpcActionCommand command,
            OrganizationType organizationType)
        {
            var result = _organizationSystem.TryJoinAtCurrentLocation(
                world,
                command.ActorId,
                organizationType);
            if (!result.Success)
            {
                return new NpcActionOutcome(
                    command.ActorId,
                    command.ActionType,
                    NpcActionResolutionStatus.Deferred,
                    0,
                    0,
                    result.Message);
            }

            var actor = FindPerson(world, command.ActorId.Value);
            if (organizationType == OrganizationType.Government)
            {
                actor.Needs.Status = Math.Max(0, actor.Needs.Status - 1_500);
            }
            else
            {
                actor.Needs.WarPressure = Math.Max(0, actor.Needs.WarPressure - 1_000);
            }

            return new NpcActionOutcome(
                command.ActorId,
                command.ActionType,
                NpcActionResolutionStatus.Completed,
                0,
                0,
                result.Message);
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

        private static PersonState FindPersonOrNull(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            return null;
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

        private static RouteState FindDirectRoute(
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
                    return route;
                }
            }

            throw new InvalidOperationException(
                $"No direct route from {originId} to {destinationId}.");
        }
    }
}
