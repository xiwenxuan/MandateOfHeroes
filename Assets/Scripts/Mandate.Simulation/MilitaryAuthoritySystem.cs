using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryAuthoritySystem
    {
        public MilitaryAuthorityLevel GetAuthority(
            WorldState world,
            StableId issuerPersonId,
            StableId armyId,
            string formationId = "")
        {
            if (!world.MilitaryServiceInitialized)
            {
                var army = FindArmy(world, armyId.Value);
                return army.CommanderPersonId == issuerPersonId.Value
                    ? MilitaryAuthorityLevel.Army
                    : MilitaryAuthorityLevel.None;
            }

            var service = FindService(
                world, issuerPersonId.Value, armyId.Value);
            if (service == null ||
                service.Status != MilitaryServiceStatus.Active &&
                service.Status != MilitaryServiceStatus.Mustering)
            {
                return MilitaryAuthorityLevel.None;
            }

            var armyState = FindArmy(world, armyId.Value);
            if (service.Role == MilitaryServiceRole.Commander &&
                armyState.CommanderPersonId == issuerPersonId.Value)
            {
                return MilitaryAuthorityLevel.Army;
            }

            var formation = FindFormation(world, service.FormationId);
            if (formation.CommanderPersonId == issuerPersonId.Value)
            {
                if (string.IsNullOrEmpty(formationId) ||
                    formation.Id == formationId)
                {
                    return MilitaryAuthorityLevel.Formation;
                }
            }

            return MilitaryAuthorityLevel.Self;
        }

        public MilitaryOrderState IssueOrder(
            WorldState world,
            StableId issuerPersonId,
            StableId armyId,
            MilitaryOrderType type,
            MilitaryAuthorityLevel requiredAuthority,
            string formationId = "",
            string targetLocationId = "",
            string targetArmyId = "")
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var actual = GetAuthority(
                world, issuerPersonId, armyId, formationId);
            var authorized = actual >= requiredAuthority;
            var order = new MilitaryOrderState
            {
                Id =
                    $"military_order.{world.AbsoluteDay}." +
                    $"{world.MilitaryOrders.Count}.{type.ToString().ToLowerInvariant()}",
                Day = world.AbsoluteDay,
                IssuerPersonId = issuerPersonId.Value,
                ArmyId = armyId.Value,
                FormationId = formationId ?? string.Empty,
                Type = type,
                RequiredAuthority = requiredAuthority,
                ActualAuthority = actual,
                Result = authorized
                    ? MilitaryOrderResult.Authorized
                    : MilitaryOrderResult.Rejected,
                TargetLocationId = targetLocationId ?? string.Empty,
                TargetArmyId = targetArmyId ?? string.Empty,
                Summary = authorized
                    ? $"{issuerPersonId.Value}获准对{armyId.Value}下达{type}命令。"
                    : $"{issuerPersonId.Value}无权对{armyId.Value}下达{type}命令。"
            };
            if (world.MilitaryServiceInitialized)
            {
                world.MilitaryOrders.Add(order);
            }
            world.Validate();
            return order;
        }

        private static MilitaryServiceState FindService(
            WorldState world,
            string personId,
            string armyId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == personId &&
                    service.ArmyId == armyId)
                {
                    return service;
                }
            }

            return null;
        }

        private static MilitaryFormationState FindFormation(
            WorldState world,
            string formationId)
        {
            for (var i = 0; i < world.MilitaryFormations.Count; i++)
            {
                if (world.MilitaryFormations[i].Id == formationId)
                {
                    return world.MilitaryFormations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing formation {formationId}.");
        }

        private static ArmyState FindArmy(
            WorldState world,
            string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }

            throw new InvalidOperationException($"Missing army {armyId}.");
        }
    }
}
