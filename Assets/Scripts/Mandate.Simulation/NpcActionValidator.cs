using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public readonly struct CommandValidation
    {
        public bool IsValid { get; }
        public string Error { get; }

        private CommandValidation(bool isValid, string error)
        {
            IsValid = isValid;
            Error = error ?? string.Empty;
        }

        public static CommandValidation Valid() => new CommandValidation(true, string.Empty);

        public static CommandValidation Invalid(string error) =>
            new CommandValidation(false, error);
    }

    public sealed class NpcActionValidator
    {
        public CommandValidation Validate(WorldState world, NpcActionCommand command)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            PersonState actor = null;
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == command.ActorId.Value)
                {
                    actor = world.People[i];
                    break;
                }
            }

            if (actor == null)
            {
                return CommandValidation.Invalid("行动者不存在。");
            }

            if (!actor.IsAlive)
            {
                return CommandValidation.Invalid("死亡人物不能行动。");
            }

            switch (command.ActionType)
            {
                case NpcActionType.Work:
                case NpcActionType.Trade:
                case NpcActionType.SeekOffice:
                case NpcActionType.Enlist:
                    return command.TargetId.Value == actor.LocationId
                        ? CommandValidation.Valid()
                        : CommandValidation.Invalid("人物不在目标地点。");

                case NpcActionType.Visit:
                    return TargetPersonOrCurrentLocationExists(world, actor, command.TargetId)
                        ? CommandValidation.Valid()
                        : CommandValidation.Invalid("拜访目标不可接触。");

                case NpcActionType.Flee:
                    return LocationExists(world, command.TargetId.Value) &&
                        HasDirectRoute(world, actor.LocationId, command.TargetId.Value)
                        ? CommandValidation.Valid()
                        : CommandValidation.Invalid("避难地点不存在或没有直达道路。");

                default:
                    return CommandValidation.Invalid("未知行动类型。");
            }
        }

        private static bool TargetPersonOrCurrentLocationExists(
            WorldState world,
            PersonState actor,
            StableId targetId)
        {
            if (targetId.Value == actor.LocationId)
            {
                return true;
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var target = world.People[i];
                if (target.Id == targetId.Value &&
                    target.IsAlive &&
                    target.LocationId == actor.LocationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LocationExists(WorldState world, string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDirectRoute(
            WorldState world,
            string originId,
            string destinationId)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                var route = world.Routes[i];
                if (route.FromLocationId == originId &&
                    route.ToLocationId == destinationId ||
                    route.Bidirectional &&
                    route.ToLocationId == originId &&
                    route.FromLocationId == destinationId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
