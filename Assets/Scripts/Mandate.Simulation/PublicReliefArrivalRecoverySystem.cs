using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class PublicReliefArrivalRecoverySystem
    {
        private readonly FoodInventorySystem _foodInventory;
        private readonly PublicReliefExternalProcurementSystem
            _externalProcurement;

        public PublicReliefArrivalRecoverySystem(
            ulong masterSeed,
            ProductionContentRegistry content)
        {
            content ??= ProductionContentRegistry.CreateCore();
            _foodInventory = new FoodInventorySystem(content);
            _externalProcurement =
                new PublicReliefExternalProcurementSystem(
                    masterSeed, content);
        }

        public void Validate(
            WorldState world,
            string freightId,
            long expectedDay,
            byte expectedSegment)
        {
            world.Validate();
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                world.AbsoluteDay != expectedDay ||
                world.Segment != expectedSegment)
            {
                throw new InvalidOperationException(
                    "Public relief arrival recovery is not due at the current world time.");
            }
            var freight = FindFreight(world, freightId);
            if (freight.Status != CivilianFreightStatus.Completed ||
                string.IsNullOrEmpty(freight.BuyerOrganizationId) ||
                string.IsNullOrEmpty(freight.PublicReliefProcurementTradeId) ||
                freight.CompletedDay > world.AbsoluteDay ||
                HasReport(world, freight.Id))
            {
                throw new InvalidOperationException(
                    "Public relief freight is not eligible for arrival recovery.");
            }
            var governance = FindGovernance(
                world, freight.DestinationCountyGovernanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var leader = ProductInventorySystem.FindPerson(
                world, government.LeaderPersonId);
            if (freight.BuyerOrganizationId != government.Id ||
                freight.DestinationInventoryContainerId !=
                    governance.GranaryInventoryContainerId ||
                !leader.IsAlive)
            {
                throw new InvalidOperationException(
                    "Public relief recovery requires the receiving county authority.");
            }
            if (freight.IsSupplementalPublicReliefFreight)
            {
                var recovery = FindRecovery(
                    world, freight.PublicReliefRecoveryId);
                if (recovery.CountyGovernanceId != governance.Id ||
                    recovery.SupplementalFreightId != freight.Id ||
                    recovery.SupplementalAttemptCount != 1)
                {
                    throw new InvalidOperationException(
                        "Supplemental public relief freight lacks its recovery contract.");
                }
            }
            else
            {
                ValidateInitialSource(world, freight, governance.Id);
            }
        }

        public void Resolve(
            WorldState world,
            string freightId,
            string sourceCommandId)
        {
            var freight = FindFreight(world, freightId);
            var governance = FindGovernance(
                world, freight.DestinationCountyGovernanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var recovery = freight.IsSupplementalPublicReliefFreight
                ? FindRecovery(world, freight.PublicReliefRecoveryId)
                : CreateRecovery(world, freight, governance.Id);
            var report = new PublicReliefFreightRecoveryReportState
            {
                Id = $"{recovery.Id}.freight_report.{recovery.FreightReports.Count:D4}",
                CivilianFreightId = freight.Id,
                PublicReliefProcurementTradeId =
                    freight.PublicReliefProcurementTradeId,
                IsSupplemental = freight.IsSupplementalPublicReliefFreight,
                DispatchedQuantity = freight.DispatchedQuantity,
                NaturalLossQuantity = freight.NaturalLossQuantity,
                DeliveredQuantity = freight.DeliveredQuantity,
                DispatchedDay = freight.DispatchedDay,
                ArrivedDay = freight.ArrivedDay,
                CompletedDay = freight.CompletedDay,
                ReconciledDay = world.AbsoluteDay,
                TransitDays = Math.Max(
                    0, freight.CompletedDay - freight.DispatchedDay),
                ReceiptWaitingDays = freight.ArrivedDay < 0
                    ? 0
                    : Math.Max(0, freight.CompletedDay - freight.ArrivedDay),
                ExceptionCode = BuildExceptionCode(freight)
            };
            var availableForRecovery = Math.Min(
                freight.DeliveredQuantity, recovery.RemainingQuantity);
            for (var i = 0;
                 i < recovery.VillageRecoveries.Count &&
                 availableForRecovery > 0;
                 i++)
            {
                var villageRecovery = recovery.VillageRecoveries[i];
                if (villageRecovery.RemainingQuantity <= 0)
                {
                    continue;
                }
                var village = FindVillage(world, villageRecovery.VillageId);
                var requested = Math.Min(
                    villageRecovery.RemainingQuantity,
                    availableForRecovery);
                var transfer = _foodInventory.TransferContainerToContainer(
                    world,
                    governance.GranaryInventoryContainerId,
                    village.PublicGranaryInventoryContainerId,
                    government.LeaderPersonId,
                    requested,
                    InventoryTransactionType.FoodCountyReliefTransferred,
                    village.Id,
                    governance.Id);
                var distributed = transfer.TransferredPhysicalQuantity;
                if (distributed <= 0)
                {
                    continue;
                }
                villageRecovery.RecoveredQuantity = checked(
                    villageRecovery.RecoveredQuantity + distributed);
                villageRecovery.RemainingQuantity = checked(
                    villageRecovery.RemainingQuantity - distributed);
                villageRecovery.InventoryTransactionIds.Add(
                    transfer.InventoryTransactionId);
                report.RecoveryDistributedQuantity = checked(
                    report.RecoveryDistributedQuantity + distributed);
                availableForRecovery = checked(
                    availableForRecovery - distributed);
                governance.TotalReliefGrain = checked(
                    governance.TotalReliefGrain + distributed);
                AddReliefLedger(
                    world, governance.Id, village.Id, distributed,
                    "Cross-county relief arrival was issued from the county granary.");
            }

            recovery.FreightReports.Add(report);
            recovery.TotalDispatchedQuantity = checked(
                recovery.TotalDispatchedQuantity +
                report.DispatchedQuantity);
            recovery.TotalNaturalLossQuantity = checked(
                recovery.TotalNaturalLossQuantity +
                report.NaturalLossQuantity);
            recovery.TotalDeliveredQuantity = checked(
                recovery.TotalDeliveredQuantity + report.DeliveredQuantity);
            recovery.TotalRecoveredQuantity = checked(
                recovery.TotalRecoveredQuantity +
                report.RecoveryDistributedQuantity);
            recovery.RemainingQuantity = checked(
                recovery.ExternalShortfallQuantity -
                recovery.TotalRecoveredQuantity);
            recovery.LastRecoveryDay = world.AbsoluteDay;

            if (recovery.RemainingQuantity == 0)
            {
                recovery.Status = PublicReliefRecoveryStatus.Fulfilled;
                return;
            }

            var arrivedButUndistributed = Math.Max(
                0,
                recovery.TotalDeliveredQuantity -
                recovery.TotalRecoveredQuantity);
            var sourcingShortfall = Math.Max(
                0, recovery.RemainingQuantity - arrivedButUndistributed);
            if (freight.IsSupplementalPublicReliefFreight ||
                recovery.SupplementalAttemptCount > 0)
            {
                recovery.Status = arrivedButUndistributed > 0
                    ? PublicReliefRecoveryStatus.DistributionBlocked
                    : PublicReliefRecoveryStatus.Exhausted;
                return;
            }
            if (sourcingShortfall <= 0)
            {
                recovery.Status =
                    PublicReliefRecoveryStatus.DistributionBlocked;
                return;
            }

            recovery.SupplementalAttemptCount = 1;
            recovery.SupplementalRequestedQuantity = sourcingShortfall;
            var limits = ReadRemainingBudgets(
                world, freight, recovery.SourceExternalSourcingEventId);
            if (limits.GoodsBudget <= 0 || limits.FreightBudget <= 0)
            {
                recovery.Status = PublicReliefRecoveryStatus.Exhausted;
                return;
            }
            var supplemental = _externalProcurement.ResolveSupplemental(
                world,
                governance.Id,
                recovery.SourceExternalSourcingEventId,
                sourceCommandId,
                recovery.Id,
                sourcingShortfall,
                limits.GoodsBudget,
                limits.FreightBudget,
                limits.MaximumUnitPrice);
            recovery.SupplementalFreightId =
                supplemental.CivilianFreightId;
            recovery.Status = string.IsNullOrEmpty(
                    supplemental.CivilianFreightId)
                ? PublicReliefRecoveryStatus.Exhausted
                : PublicReliefRecoveryStatus.SupplementalInTransit;
        }

        private static PublicReliefRecoveryState CreateRecovery(
            WorldState world,
            CivilianFreightState freight,
            string governanceId)
        {
            var externalEvent = FindEvent(
                world, freight.SourcePublicReliefEventId);
            var shortfallDay = checked(externalEvent.Day - 1);
            var localProcurementCommand = FindCommand(
                world,
                PublicReliefProcurementCommandScheduler.CommandId(
                    shortfallDay, governanceId));
            var shortfallEventId = ReadArgument(
                localProcurementCommand,
                PublicReliefProcurementCommandScheduler
                    .SourceEventIdArgumentId);
            var externalShortfall = SumFiscal(
                world,
                governanceId,
                externalEvent.Day,
                CountyFiscalEntryType.GrainProcurementUnfilled);
            var localPurchased = SumPublicReliefTrades(
                world, governanceId, shortfallEventId, true);
            var villageShortfalls = new List<CountyFiscalLedgerEntryState>();
            for (var i = 0; i < world.CountyFiscalLedgerEntries.Count; i++)
            {
                var entry = world.CountyFiscalLedgerEntries[i];
                if (entry.Day == shortfallDay &&
                    entry.CountyGovernanceId == governanceId &&
                    entry.Type == CountyFiscalEntryType.GrainReliefShortfall &&
                    entry.Amount > 0)
                {
                    villageShortfalls.Add(entry);
                }
            }
            villageShortfalls.Sort((left, right) =>
                string.CompareOrdinal(left.VillageId, right.VillageId));
            var recovery = new PublicReliefRecoveryState
            {
                Id = $"public_relief.recovery.{externalEvent.Day:D10}.{governanceId}",
                Status = PublicReliefRecoveryStatus.Exhausted,
                CountyGovernanceId = governanceId,
                SourceShortfallEventId = shortfallEventId,
                SourceExternalSourcingEventId = externalEvent.Id,
                SourceShortfallDay = shortfallDay,
                ExternalShortfallQuantity = externalShortfall,
                RemainingQuantity = externalShortfall,
                SupplementalFreightId = string.Empty,
                LastRecoveryDay = world.AbsoluteDay
            };
            var skip = localPurchased;
            var remaining = externalShortfall;
            for (var i = 0; i < villageShortfalls.Count && remaining > 0; i++)
            {
                var available = villageShortfalls[i].Amount;
                var skipped = Math.Min(skip, available);
                skip -= skipped;
                available -= skipped;
                var required = Math.Min(available, remaining);
                if (required <= 0)
                {
                    continue;
                }
                recovery.VillageRecoveries.Add(
                    new PublicReliefVillageRecoveryState
                    {
                        VillageId = villageShortfalls[i].VillageId,
                        RequiredQuantity = required,
                        RemainingQuantity = required
                    });
                remaining -= required;
            }
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    "External relief shortfall cannot be assigned to its source villages.");
            }
            world.PublicReliefRecoveries.Add(recovery);
            return recovery;
        }

        private static void ValidateInitialSource(
            WorldState world,
            CivilianFreightState freight,
            string governanceId)
        {
            if (!string.IsNullOrEmpty(freight.PublicReliefRecoveryId) ||
                freight.IsSupplementalPublicReliefFreight)
            {
                throw new InvalidOperationException(
                    "Initial public relief freight has supplemental linkage.");
            }
            var source = FindEvent(
                world, freight.SourcePublicReliefEventId);
            if (source.EventTypeId !=
                    PublicReliefProcurementContractIds
                        .ExternalSourcingRequiredEventTypeId ||
                source.Day <= 0 ||
                source.SourceTransactionId !=
                    PublicReliefProcurementCommandScheduler.TransactionId(
                        source.Day, governanceId) ||
                SumFiscal(
                    world,
                    governanceId,
                    source.Day,
                    CountyFiscalEntryType.GrainProcurementUnfilled) <= 0)
            {
                throw new InvalidOperationException(
                    "Initial public relief freight has an invalid shortfall source.");
            }
        }

        private static RemainingBudgets ReadRemainingBudgets(
            WorldState world,
            CivilianFreightState freight,
            string sourceEventId)
        {
            var command = FindCommand(
                world, freight.SourcePublicReliefCommandId);
            var maximumGoods = ReadPositiveArgument(
                command,
                PublicReliefExternalProcurementCommandScheduler
                    .MaximumGoodsBudgetArgumentId);
            var maximumFreight = ReadPositiveArgument(
                command,
                PublicReliefExternalProcurementCommandScheduler
                    .MaximumFreightBudgetArgumentId);
            var maximumUnitPrice = ReadPositiveArgument(
                command,
                PublicReliefExternalProcurementCommandScheduler
                    .MaximumUnitPriceArgumentId);
            long spentGoods = 0;
            long spentFreight = 0;
            for (var i = 0;
                 i < world.PublicReliefProcurementTrades.Count;
                 i++)
            {
                var trade = world.PublicReliefProcurementTrades[i];
                if (trade.SourceShortfallEventId == sourceEventId &&
                    !string.IsNullOrEmpty(trade.CivilianFreightId))
                {
                    spentGoods = checked(
                        spentGoods + trade.MoneyTransferred);
                    spentFreight = checked(
                        spentFreight + trade.FreightFee);
                }
            }
            return new RemainingBudgets
            {
                GoodsBudget = Math.Max(0, maximumGoods - spentGoods),
                FreightBudget = Math.Max(
                    0, maximumFreight - spentFreight),
                MaximumUnitPrice = maximumUnitPrice
            };
        }

        private static long ReadPositiveArgument(
            PersistentWorldCommandState command,
            string key)
        {
            for (var i = 0; i < command.Arguments.Count; i++)
            {
                if (command.Arguments[i].Key == key &&
                    long.TryParse(
                        command.Arguments[i].Value,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var value) && value > 0)
                {
                    return value;
                }
            }
            throw new InvalidOperationException(
                $"Public relief source command lacks argument {key}.");
        }

        private static string ReadArgument(
            PersistentWorldCommandState command,
            string key)
        {
            for (var i = 0; i < command.Arguments.Count; i++)
            {
                if (command.Arguments[i].Key == key &&
                    !string.IsNullOrEmpty(command.Arguments[i].Value))
                {
                    return command.Arguments[i].Value;
                }
            }
            throw new InvalidOperationException(
                $"Public relief source command lacks argument {key}.");
        }

        private static string BuildExceptionCode(
            CivilianFreightState freight)
        {
            var result = new List<string>();
            if (freight.NaturalLossQuantity > 0)
            {
                result.Add("natural_loss");
            }
            if (freight.DeliveredQuantity < freight.DispatchedQuantity)
            {
                result.Add("actual_arrival_shortfall");
            }
            if (freight.ArrivedDay >= 0 &&
                freight.CompletedDay > freight.ArrivedDay)
            {
                result.Add("receipt_capacity_wait");
            }
            return result.Count == 0
                ? string.Empty
                : string.Join(";", result);
        }

        private static void AddReliefLedger(
            WorldState world,
            string governanceId,
            string villageId,
            long quantity,
            string summary)
        {
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = $"county_fiscal.{world.AbsoluteDay}.recovery.{world.CountyFiscalLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = CountyFiscalEntryType.GrainRelief,
                    CountyGovernanceId = governanceId,
                    FamilyId = string.Empty,
                    VillageId = villageId,
                    VillageGrainDelta = quantity,
                    CountyGrainDelta = -quantity,
                    Amount = quantity,
                    Summary = summary
                });
        }

        private static long SumFiscal(
            WorldState world,
            string governanceId,
            long day,
            CountyFiscalEntryType type)
        {
            long result = 0;
            for (var i = 0; i < world.CountyFiscalLedgerEntries.Count; i++)
            {
                var entry = world.CountyFiscalLedgerEntries[i];
                if (entry.CountyGovernanceId == governanceId &&
                    entry.Day == day && entry.Type == type)
                {
                    result = checked(result + entry.Amount);
                }
            }
            return result;
        }

        private static long SumPublicReliefTrades(
            WorldState world,
            string governanceId,
            string sourceEventId,
            bool localOnly)
        {
            long result = 0;
            for (var i = 0;
                 i < world.PublicReliefProcurementTrades.Count;
                 i++)
            {
                var trade = world.PublicReliefProcurementTrades[i];
                if (trade.CountyGovernanceId == governanceId &&
                    trade.SourceShortfallEventId == sourceEventId &&
                    (!localOnly ||
                     string.IsNullOrEmpty(trade.CivilianFreightId)))
                {
                    result = checked(result + trade.Quantity);
                }
            }
            return result;
        }

        private static bool HasReport(WorldState world, string freightId)
        {
            for (var i = 0; i < world.PublicReliefRecoveries.Count; i++)
            {
                for (var reportIndex = 0;
                     reportIndex <
                        world.PublicReliefRecoveries[i].FreightReports.Count;
                     reportIndex++)
                {
                    if (world.PublicReliefRecoveries[i]
                            .FreightReports[reportIndex]
                            .CivilianFreightId == freightId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static CivilianFreightState FindFreight(
            WorldState world, string id) =>
            world.CivilianFreights.Find(item => item.Id == id) ??
            throw new InvalidOperationException(
                $"Missing civilian freight {id}.");

        private static PublicReliefRecoveryState FindRecovery(
            WorldState world, string id) =>
            world.PublicReliefRecoveries.Find(item => item.Id == id) ??
            throw new InvalidOperationException(
                $"Missing public relief recovery {id}.");

        private static CountyGovernanceState FindGovernance(
            WorldState world, string id) =>
            world.CountyGovernances.Find(item => item.Id == id) ??
            throw new InvalidOperationException(
                $"Missing county governance {id}.");

        private static OrganizationState FindOrganization(
            WorldState world, string id) =>
            world.Organizations.Find(item => item.Id == id) ??
            throw new InvalidOperationException(
                $"Missing organization {id}.");

        private static VillageState FindVillage(
            WorldState world, string id) =>
            world.Villages.Find(item => item.Id == id) ??
            throw new InvalidOperationException($"Missing village {id}.");

        private static WorldEventOutboxState FindEvent(
            WorldState world, string id) =>
            world.WorldEventOutbox.Find(item => item.Id == id) ??
            throw new InvalidOperationException($"Missing event {id}.");

        private static PersistentWorldCommandState FindCommand(
            WorldState world, string id) =>
            world.PersistentWorldCommands.Find(item => item.Id == id) ??
            throw new InvalidOperationException($"Missing command {id}.");

        private sealed class RemainingBudgets
        {
            public long GoodsBudget;
            public long FreightBudget;
            public long MaximumUnitPrice;
        }
    }

    public sealed class PublicReliefArrivalRecoveryCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.public_relief.recover_arrival";
        public const string IssuerId =
            "system.public_relief_arrival_recovery";
        public const string FreightIdArgumentId = "civilian_freight_id";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string ExpectedSegmentArgumentId = "expected_segment";
        public const string TransactionKindId =
            "mandate.transaction.public_relief.recover_arrival";
        public const string EventTypeId =
            "mandate.event.public_relief.arrival_recovery_resolved";
        public const string ProjectionHandlerId =
            "mandate.handler.public_relief.arrival_recovery_projection";

        private readonly PublicReliefArrivalRecoverySystem _system;

        public PublicReliefArrivalRecoveryCommandScheduler(
            PublicReliefArrivalRecoverySystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public int EnsureDueCommands(
            WorldState world,
            WorldCommandRuntime runtime)
        {
            var candidates = new List<CivilianFreightState>();
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                if (freight.Status == CivilianFreightStatus.Completed &&
                    !string.IsNullOrEmpty(freight.BuyerOrganizationId) &&
                    !string.IsNullOrEmpty(
                        freight.PublicReliefProcurementTradeId) &&
                    !HasCommandOrReport(world, freight.Id))
                {
                    candidates.Add(freight);
                }
            }
            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < candidates.Count; i++)
            {
                var freight = candidates[i];
                runtime.Enqueue(
                    world,
                    new WorldCommandEnvelope(
                        CommandId(freight.Id),
                        CommandTypeId,
                        IssuerId,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment,
                        7,
                        new Dictionary<string, string>
                        {
                            { FreightIdArgumentId, freight.Id },
                            {
                                ExpectedDayArgumentId,
                                Invariant(world.AbsoluteDay)
                            },
                            {
                                ExpectedSegmentArgumentId,
                                Invariant(world.Segment)
                            }
                        }));
            }
            return candidates.Count;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new CommandHandler(_system);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new ProjectionHandler();

        public static string CommandId(string freightId) =>
            $"public_relief.arrival_recovery_command.{freightId}";

        public static string TransactionId(string freightId) =>
            $"public_relief.arrival_recovery_transaction.{freightId}";

        public static string EventId(string freightId) =>
            $"public_relief.arrival_recovery_resolved.{freightId}";

        private static bool HasCommandOrReport(
            WorldState world, string freightId)
        {
            var commandId = CommandId(freightId);
            if (world.PersistentWorldCommands.Exists(item =>
                    item.Id == commandId))
            {
                return true;
            }
            for (var i = 0; i < world.PublicReliefRecoveries.Count; i++)
            {
                if (world.PublicReliefRecoveries[i].FreightReports.Exists(
                        item => item.CivilianFreightId == freightId))
                {
                    return true;
                }
            }
            return false;
        }

        private sealed class CommandHandler : IWorldCommandHandler
        {
            private readonly PublicReliefArrivalRecoverySystem _system;

            public CommandHandler(PublicReliefArrivalRecoverySystem system)
            {
                _system = system;
            }

            public string CommandTypeId =>
                PublicReliefArrivalRecoveryCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 3 ||
                    !TryId(command, FreightIdArgumentId, out var freightId) ||
                    !TryNonNegative(
                        command, ExpectedDayArgumentId, out var day) ||
                    !TryByte(
                        command, ExpectedSegmentArgumentId, out var segment) ||
                    segment > (byte)DaySegment.Night)
                {
                    throw new InvalidOperationException(
                        "Public relief arrival recovery arguments are invalid.");
                }
                transactions.Add(new Transaction(
                    _system, command.Id, freightId, day, segment));
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly PublicReliefArrivalRecoverySystem _system;
            private readonly string _commandId;
            private readonly string _freightId;
            private readonly long _day;
            private readonly byte _segment;

            public Transaction(
                PublicReliefArrivalRecoverySystem system,
                string commandId,
                string freightId,
                long day,
                byte segment)
            {
                _system = system;
                _commandId = commandId;
                _freightId = freightId;
                _day = day;
                _segment = segment;
                Id = TransactionId(freightId);
            }

            public string Id { get; }
            public string KindId => TransactionKindId;
            public int Priority => 7;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.Validate(world, _freightId, _day, _segment);
                validation.Reserve(
                    "public_relief.arrival_recovery." + _freightId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _system.Resolve(world, _freightId, _commandId);
                events.Add(new WorldRuntimeEvent(
                    EventId(_freightId),
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
                PublicReliefArrivalRecoveryCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
            }
        }

        private static string Invariant(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static bool TryId(
            WorldCommandEnvelope command,
            string key,
            out string value)
        {
            if (!command.Arguments.TryGetValue(key, out value) ||
                string.IsNullOrEmpty(value))
            {
                return false;
            }
            _ = new StableId(value);
            return true;
        }

        private static bool TryNonNegative(
            WorldCommandEnvelope command,
            string key,
            out long value)
        {
            value = 0;
            return command.Arguments.TryGetValue(key, out var text) &&
                long.TryParse(
                    text,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out value) && value >= 0;
        }

        private static bool TryByte(
            WorldCommandEnvelope command,
            string key,
            out byte value)
        {
            value = 0;
            return command.Arguments.TryGetValue(key, out var text) &&
                byte.TryParse(
                    text,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out value);
        }
    }
}
