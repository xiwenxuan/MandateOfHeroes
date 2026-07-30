using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class TaskAcceptResult
    {
        public bool Success { get; }
        public TaskInstanceState Task { get; }
        public string Message { get; }

        public TaskAcceptResult(bool success, TaskInstanceState task, string message)
        {
            Success = success;
            Task = task;
            Message = message ?? string.Empty;
        }
    }

    public sealed class TaskSystem
    {
        private readonly MilitarySupplySystem _militarySupplySystem =
            new MilitarySupplySystem();

        public TaskAcceptResult TryAccept(
            WorldState world,
            StableId personId,
            StableId definitionId)
        {
            world.Validate();
            var person = FindPerson(world, personId.Value);
            var definition = FindDefinition(world, definitionId.Value);
            if (!person.IsAlive)
            {
                return new TaskAcceptResult(false, null, "死亡人物不能接受任务。");
            }

            if (!definition.IsAvailable)
            {
                return new TaskAcceptResult(false, null, "任务尚未解锁。");
            }

            if (person.LocationId != definition.OriginLocationId)
            {
                return new TaskAcceptResult(false, null, "人物不在任务起点。");
            }

            if (FindActiveTask(world, person.Id) != null)
            {
                return new TaskAcceptResult(false, null, "人物已有进行中的任务。");
            }

            if (definition.RequiresMembership &&
                !HasRequiredMembership(world, person.Id, definition))
            {
                return new TaskAcceptResult(false, null, "人物不具备所需组织或职位。");
            }

            var task = new TaskInstanceState
            {
                Id = $"task.{definition.Id}.{person.Id}.{world.Revision}",
                DefinitionId = definition.Id,
                AssigneePersonId = person.Id,
                Status = TaskStatus.Active,
                AcceptedDay = world.AbsoluteDay,
                DeadlineDay = checked(world.AbsoluteDay + definition.DurationDays),
                Progress = 0
            };
            world.Tasks.Add(task);
            world.Validate();
            return new TaskAcceptResult(true, task, $"接受任务：{definition.DisplayName}。");
        }

        public void ResolveDailyProgress(WorldState world)
        {
            for (var i = 0; i < world.Tasks.Count; i++)
            {
                var task = world.Tasks[i];
                if (task.Status != TaskStatus.Active)
                {
                    continue;
                }

                var definition = FindDefinition(world, task.DefinitionId);
                var person = FindPerson(world, task.AssigneePersonId);
                if (!person.IsAlive || world.AbsoluteDay > task.DeadlineDay)
                {
                    task.Status = TaskStatus.Failed;
                    continue;
                }

                if (definition.Kind == TaskKind.LocalWork &&
                    person.LocationId == definition.OriginLocationId &&
                    !IsTraveling(world, person.Id))
                {
                    task.Progress++;
                }
                else if (definition.Kind == TaskKind.TravelDelivery &&
                    person.LocationId == definition.TargetLocationId &&
                    !IsTraveling(world, person.Id))
                {
                    task.Progress = definition.RequiredProgress;
                }

                if (task.Progress >= definition.RequiredProgress)
                {
                    task.Status = TaskStatus.Completed;
                    GrantReward(world, person, task, definition);
                }
            }
        }

        private void GrantReward(
            WorldState world,
            PersonState person,
            TaskInstanceState task,
            TaskDefinitionState definition)
        {
            if (task.RewardClaimed)
            {
                return;
            }

            person.Wealth = checked(person.Wealth + definition.RewardMoney);
            person.Provisions = checked(person.Provisions + definition.RewardProvisions);
            _militarySupplySystem.ApplyTaskDelivery(world, task, definition);
            task.RewardClaimed = true;
        }

        private static bool HasRequiredMembership(
            WorldState world,
            string personId,
            TaskDefinitionState definition)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId == personId &&
                    membership.OrganizationId == definition.IssuerOrganizationId &&
                    (string.IsNullOrEmpty(definition.RequiredPositionId) ||
                     membership.PositionId == definition.RequiredPositionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTraveling(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return true;
                }
            }

            return false;
        }

        private static TaskInstanceState FindActiveTask(WorldState world, string personId)
        {
            for (var i = 0; i < world.Tasks.Count; i++)
            {
                if (world.Tasks[i].AssigneePersonId == personId &&
                    world.Tasks[i].Status == TaskStatus.Active)
                {
                    return world.Tasks[i];
                }
            }

            return null;
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

        private static TaskDefinitionState FindDefinition(
            WorldState world,
            string definitionId)
        {
            for (var i = 0; i < world.TaskDefinitions.Count; i++)
            {
                if (world.TaskDefinitions[i].Id == definitionId)
                {
                    return world.TaskDefinitions[i];
                }
            }

            throw new InvalidOperationException($"Missing task definition {definitionId}.");
        }
    }
}
