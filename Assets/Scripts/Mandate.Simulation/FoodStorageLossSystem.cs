using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class FoodStorageLossSystem
    {
        public const int AssessmentIntervalDays = 30;
        public const int BaseMonthlyLossBasisPoints = 200;

        private readonly ProductionContentRegistry _content;

        public FoodStorageLossSystem(ProductionContentRegistry content = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public bool IsDue(WorldState world, ProductBatchState batch)
        {
            if (world == null || batch == null || batch.Quantity <= 0 ||
                batch.NextFoodStorageAssessmentDay > world.AbsoluteDay ||
                !_content.TryGetFood(batch.ProductDefinitionId, out _))
            {
                return false;
            }

            if (string.IsNullOrEmpty(batch.InventoryContainerId))
            {
                return !string.IsNullOrEmpty(batch.StorageFacilityId);
            }

            var container = ProductInventorySystem.FindContainer(
                world, batch.InventoryContainerId);
            return string.IsNullOrEmpty(container.CarrierPersonId);
        }

        public FoodStorageLossState Resolve(
            WorldState world,
            string batchId,
            long expectedDay)
        {
            var plan = BuildPlan(world, batchId, expectedDay);
            var batch = plan.Batch;
            var lossId = FoodStorageLossCommandScheduler.LossId(
                batch.Id, expectedDay);
            var transactionId = plan.QuantityLost > 0
                ? FoodStorageLossCommandScheduler.InventoryTransactionId(
                    batch.Id, expectedDay)
                : string.Empty;
            var record = new FoodStorageLossState
            {
                Id = lossId,
                Day = expectedDay,
                BatchId = batch.Id,
                ProductDefinitionId = batch.ProductDefinitionId,
                StorageFacilityId = batch.StorageFacilityId,
                InventoryContainerId = batch.InventoryContainerId,
                StorageEnvironmentId = plan.EnvironmentId,
                StorageProtectionBasisPoints = plan.ProtectionBasisPoints,
                FoodSpoilageSensitivityBasisPoints =
                    plan.Food.SpoilageSensitivityBasisPoints,
                EffectiveLossBasisPoints = plan.EffectiveLossBasisPoints,
                QuantityBefore = batch.Quantity,
                ReservedQuantity = batch.ReservedQuantity,
                QuantityLost = plan.QuantityLost,
                QuantityAfter = checked(batch.Quantity - plan.QuantityLost),
                FreshnessBeforeBasisPoints = batch.FreshnessBasisPoints,
                FreshnessAfterBasisPoints = Math.Max(
                    0,
                    batch.FreshnessBasisPoints -
                    plan.EffectiveLossBasisPoints),
                InventoryTransactionId = transactionId,
                Summary = $"Assessed stored food batch {batch.Id} at " +
                    $"{plan.EnvironmentId}; lost {plan.QuantityLost} units."
            };

            if (plan.QuantityLost > 0)
            {
                var transaction = new InventoryTransactionState
                {
                    Id = transactionId,
                    Day = expectedDay,
                    Type = InventoryTransactionType.FoodStorageNaturalLoss,
                    ActorPersonId = string.Empty,
                    SourceWorkOrderId = string.Empty,
                    SourceMilitaryProcurementId = string.Empty,
                    SourceEquipmentRepairOrderId = string.Empty,
                    SourceResourceExtractionOrderId = string.Empty,
                    SourceMilitaryLogisticsOrderId = string.Empty,
                    SourceVillageId = string.Empty,
                    SourceCountyGovernanceId = string.Empty,
                    SourceFormalMarketOrderId = string.Empty,
                    SourceCivilianFreightId = string.Empty,
                    SourceFoodStorageLossId = lossId,
                    Summary = record.Summary
                };
                transaction.Lines.Add(new InventoryTransactionLineState
                {
                    BatchId = batch.Id,
                    ProductDefinitionId = batch.ProductDefinitionId,
                    OwnerFamilyId = batch.OwnerFamilyId,
                    OwnerOrganizationId = batch.OwnerOrganizationId,
                    StorageFacilityId = batch.StorageFacilityId,
                    InventoryContainerId = batch.InventoryContainerId,
                    UnitId = batch.UnitId,
                    QuantityDelta = -plan.QuantityLost,
                    ReservedQuantityDelta = 0
                });
                world.InventoryTransactions.Add(transaction);
                if (!string.IsNullOrEmpty(batch.StorageFacilityId))
                {
                    var facility = FindFacility(
                        world, batch.StorageFacilityId);
                    facility.InventoryUnits = Math.Max(
                        0,
                        checked(facility.InventoryUnits -
                            plan.QuantityLost * batch.UnitWeight));
                }
                batch.Quantity = record.QuantityAfter;
            }

            batch.FreshnessBasisPoints = record.FreshnessAfterBasisPoints;
            batch.NextFoodStorageAssessmentDay = checked(
                expectedDay + AssessmentIntervalDays);
            world.FoodStorageLosses.Add(record);
            return record;
        }

        public int ResolveAllDue(WorldState world, long expectedDay)
        {
            var batches = CollectDueBatches(world);
            for (var i = 0; i < batches.Count; i++)
            {
                Resolve(world, batches[i].Id, expectedDay);
            }
            return batches.Count;
        }

        public void ValidateAllDue(WorldState world, long expectedDay)
        {
            if (world == null || expectedDay != world.AbsoluteDay)
            {
                throw new InvalidOperationException(
                    "Food storage assessment day drifted from the world clock.");
            }
            var batches = CollectDueBatches(world);
            if (batches.Count == 0)
            {
                throw new InvalidOperationException(
                    "Food storage assessment has no due batches.");
            }
            for (var i = 0; i < batches.Count; i++)
            {
                _ = BuildPlan(world, batches[i].Id, expectedDay);
            }
        }

        public void ValidateDue(
            WorldState world,
            string batchId,
            long expectedDay)
        {
            _ = BuildPlan(world, batchId, expectedDay);
        }

        public List<ProductBatchState> CollectDueBatches(WorldState world)
        {
            var result = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (IsDue(world, world.ProductBatches[i]))
                {
                    result.Add(world.ProductBatches[i]);
                }
            }
            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        internal static int CalculateEffectiveLossBasisPoints(
            int spoilageSensitivityBasisPoints,
            int protectionBasisPoints)
        {
            if (spoilageSensitivityBasisPoints < 0 ||
                protectionBasisPoints < 0 || protectionBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException();
            }
            var exposed = 10_000L - protectionBasisPoints;
            var result = BaseMonthlyLossBasisPoints *
                (long)spoilageSensitivityBasisPoints * exposed /
                100_000_000L;
            return (int)Math.Min(10_000L, result);
        }

        private Plan BuildPlan(
            WorldState world,
            string batchId,
            long expectedDay)
        {
            if (world == null || expectedDay != world.AbsoluteDay)
            {
                throw new InvalidOperationException(
                    "Food storage assessment day drifted from the world clock.");
            }
            var batch = world.ProductBatches.Find(item => item.Id == batchId) ??
                throw new InvalidOperationException(
                    $"Missing stored food batch {batchId}.");
            if (!IsDue(world, batch) || world.FoodStorageLosses.Exists(
                    item => item.Id ==
                        FoodStorageLossCommandScheduler.LossId(
                            batchId, expectedDay)))
            {
                throw new InvalidOperationException(
                    $"Food batch {batchId} is not due for storage loss.");
            }
            var food = _content.GetFood(batch.ProductDefinitionId);
            ResolveEnvironment(
                world,
                batch,
                out var environmentId,
                out var protection);
            var effective = CalculateEffectiveLossBasisPoints(
                food.SpoilageSensitivityBasisPoints,
                protection);
            var available = batch.Quantity - batch.ReservedQuantity;
            var lost = available <= 0 || effective <= 0
                ? 0
                : Math.Max(1L, checked(available * effective / 10_000L));
            return new Plan
            {
                Batch = batch,
                Food = food,
                EnvironmentId = environmentId,
                ProtectionBasisPoints = protection,
                EffectiveLossBasisPoints = effective,
                QuantityLost = Math.Min(available, lost)
            };
        }

        private static void ResolveEnvironment(
            WorldState world,
            ProductBatchState batch,
            out string environmentId,
            out int protectionBasisPoints)
        {
            if (!string.IsNullOrEmpty(batch.StorageFacilityId))
            {
                var facility = FindFacility(world, batch.StorageFacilityId);
                environmentId = facility.FoodStorageEnvironmentId;
                protectionBasisPoints = checked(
                    facility.FoodStorageProtectionBasisPoints *
                    facility.ConditionBasisPoints / 10_000);
                return;
            }
            var container = ProductInventorySystem.FindContainer(
                world, batch.InventoryContainerId);
            environmentId = container.FoodStorageEnvironmentId;
            protectionBasisPoints =
                container.FoodStorageProtectionBasisPoints;
        }

        private static VillageFacilityState FindFacility(
            WorldState world, string id) =>
            world.VillageFacilities.Find(item => item.Id == id) ??
            throw new InvalidOperationException(
                $"Missing food storage facility {id}.");

        private sealed class Plan
        {
            public ProductBatchState Batch;
            public FoodDefinition Food;
            public string EnvironmentId;
            public int ProtectionBasisPoints;
            public int EffectiveLossBasisPoints;
            public long QuantityLost;
        }
    }

    public sealed class FoodStorageLossCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.food_storage.assess_loss";
        public const string IssuerId = "system.food_storage_loss";
        public const string TransactionKindId =
            "mandate.transaction.food_storage.assess_loss";
        public const string EventTypeId =
            "mandate.event.food_storage.loss_assessed";
        public const string ProjectionHandlerId =
            "mandate.handler.food_storage.loss_projection";
        private const string DayArgument = "expected_day";

        private readonly FoodStorageLossSystem _system;

        public FoodStorageLossCommandScheduler(FoodStorageLossSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public int EnsureDueCommands(
            WorldState world,
            WorldCommandRuntime runtime)
        {
            if (_system.CollectDueBatches(world).Count == 0 ||
                world.PersistentWorldCommands.Exists(item =>
                    item.Id == CommandId(world.AbsoluteDay)))
            {
                return 0;
            }
            runtime.Enqueue(world, new WorldCommandEnvelope(
                CommandId(world.AbsoluteDay),
                CommandTypeId,
                IssuerId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                6,
                new Dictionary<string, string>
                {
                    {
                        DayArgument,
                        world.AbsoluteDay.ToString(
                            CultureInfo.InvariantCulture)
                    }
                }));
            return 1;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new Handler(_system);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new ProjectionHandler();

        public static string CommandId(long day) =>
            $"food_storage.loss_command.{day}";
        public static string TransactionId(long day) =>
            $"food_storage.loss_transaction.{day}";
        public static string EventId(long day) =>
            $"food_storage.loss_assessed.{day}";
        public static string LossId(string batchId, long day) =>
            $"food_storage.loss.{day}.{batchId}";
        public static string InventoryTransactionId(string batchId, long day) =>
            $"inventory_transaction.food_storage_loss.{day}.{batchId}";

        private sealed class Handler : IWorldCommandHandler
        {
            private readonly FoodStorageLossSystem _system;
            public Handler(FoodStorageLossSystem system) => _system = system;
            public string CommandTypeId =>
                FoodStorageLossCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 1 ||
                    !command.Arguments.TryGetValue(DayArgument, out var text) ||
                    !long.TryParse(
                        text,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var day))
                {
                    throw new InvalidOperationException(
                        "Food storage loss command arguments are invalid.");
                }
                transactions.Add(new Transaction(_system, command.Id, day));
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly FoodStorageLossSystem _system;
            private readonly string _commandId;
            private readonly long _day;
            public Transaction(
                FoodStorageLossSystem system,
                string commandId,
                long day)
            {
                _system = system;
                _commandId = commandId;
                _day = day;
                Id = TransactionId(day);
            }
            public string Id { get; }
            public string KindId => TransactionKindId;
            public int Priority => 6;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.ValidateAllDue(world, _day);
                validation.Reserve(
                    "food_storage.loss.day." + _day.ToString(
                        CultureInfo.InvariantCulture),
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _system.ResolveAllDue(world, _day);
                events.Add(new WorldRuntimeEvent(
                    EventId(_day),
                    EventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
            }
        }

        private sealed class ProjectionHandler : IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;
            public string EventTypeId =>
                FoodStorageLossCommandScheduler.EventTypeId;
            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
            }
        }
    }
}
