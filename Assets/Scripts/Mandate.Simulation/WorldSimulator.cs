using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum WorldSystemPhase : byte
    {
        SegmentCommand = 0,
        SegmentRuntimeEvent = 1,
        SegmentMovement = 2,
        SegmentArrival = 3,
        DailyCommand = 4,
        DailyTransit = 5,
        DailyHistoricalEvent = 6,
        DailySimulation = 7,
        DailyProjection = 8,
        DailyRuntimeEvent = 9
    }

    public enum WorldSystemCadence : byte
    {
        EverySegment = 0,
        NewDay = 1
    }

    public sealed class WorldSystemExecutionContext
    {
        private readonly Dictionary<string, object> _scratch =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public WorldSystemExecutionContext(WorldState world, bool enteredNewDay)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            EnteredNewDay = enteredNewDay;
        }

        public WorldState World { get; }

        public bool EnteredNewDay { get; }

        public void SetScratch<T>(string key, T value)
        {
            _ = new StableId(key);
            _scratch[key] = value;
        }

        public T GetScratch<T>(string key)
        {
            _ = new StableId(key);
            if (!_scratch.TryGetValue(key, out var value) || !(value is T typed))
            {
                throw new InvalidOperationException(
                    $"World execution scratch value '{key}' is unavailable.");
            }

            return typed;
        }
    }

    public sealed class WorldScheduledSystem
    {
        public WorldScheduledSystem(
            string id,
            WorldSystemPhase phase,
            WorldSystemCadence cadence,
            int order,
            Action<WorldSystemExecutionContext> execute)
        {
            if (!Enum.IsDefined(typeof(WorldSystemPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (!Enum.IsDefined(typeof(WorldSystemCadence), cadence))
            {
                throw new ArgumentOutOfRangeException(nameof(cadence));
            }

            Id = new StableId(id).Value;
            Phase = phase;
            Cadence = cadence;
            Order = order;
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string Id { get; }

        public WorldSystemPhase Phase { get; }

        public WorldSystemCadence Cadence { get; }

        public int Order { get; }

        internal Action<WorldSystemExecutionContext> Execute { get; }
    }

    public sealed class WorldSystemScheduler
    {
        private readonly List<WorldScheduledSystem> _systems =
            new List<WorldScheduledSystem>();
        private readonly List<string> _lastExecutionTrace =
            new List<string>();

        public IReadOnlyList<string> LastExecutionTrace => _lastExecutionTrace;

        public void Register(WorldScheduledSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            for (var i = 0; i < _systems.Count; i++)
            {
                if (string.Equals(
                        _systems[i].Id,
                        system.Id,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"World system '{system.Id}' is already registered.");
                }
            }

            _systems.Add(system);
        }

        public void BeginTrace()
        {
            _lastExecutionTrace.Clear();
        }

        public void ExecutePhase(
            WorldSystemPhase phase,
            WorldSystemExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var due = new List<WorldScheduledSystem>();
            for (var i = 0; i < _systems.Count; i++)
            {
                var system = _systems[i];
                if (system.Phase != phase ||
                    system.Cadence == WorldSystemCadence.NewDay &&
                    !context.EnteredNewDay)
                {
                    continue;
                }

                due.Add(system);
            }

            due.Sort(CompareSystems);
            for (var i = 0; i < due.Count; i++)
            {
                due[i].Execute(context);
                _lastExecutionTrace.Add(due[i].Id);
            }
        }

        private static int CompareSystems(
            WorldScheduledSystem left,
            WorldScheduledSystem right)
        {
            var byOrder = left.Order.CompareTo(right.Order);
            return byOrder != 0
                ? byOrder
                : string.CompareOrdinal(left.Id, right.Id);
        }
    }

    public sealed class WorldCommandEnvelope
    {
        public WorldCommandEnvelope(
            string id,
            string commandTypeId,
            string issuerId,
            long dueDay,
            DaySegment dueSegment,
            int priority,
            IReadOnlyDictionary<string, string> arguments = null)
        {
            if (dueDay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dueDay));
            }

            _ = new WorldTime(dueDay, dueSegment);
            Id = new StableId(id).Value;
            CommandTypeId = new StableId(commandTypeId).Value;
            IssuerId = new StableId(issuerId).Value;
            DueDay = dueDay;
            DueSegment = dueSegment;
            Priority = priority;
            var argumentSnapshot =
                new Dictionary<string, string>(StringComparer.Ordinal);
            if (arguments != null)
            {
                foreach (var pair in arguments)
                {
                    var key = new StableId(pair.Key).Value;
                    if (pair.Value == null)
                    {
                        throw new ArgumentException(
                            $"Command argument '{key}' cannot be null.",
                            nameof(arguments));
                    }

                    argumentSnapshot.Add(key, pair.Value);
                }
            }

            Arguments = argumentSnapshot;
        }

        public string Id { get; }

        public string CommandTypeId { get; }

        public string IssuerId { get; }

        public long DueDay { get; }

        public DaySegment DueSegment { get; }

        public int Priority { get; }

        public IReadOnlyDictionary<string, string> Arguments { get; }
    }

    public interface IWorldCommandHandler
    {
        string CommandTypeId { get; }

        void Plan(
            WorldCommandEnvelope command,
            WorldTransactionBuffer transactions);
    }

    public interface IWorldTransaction
    {
        string Id { get; }

        string KindId { get; }

        int Priority { get; }

        void Validate(
            WorldState world,
            WorldTransactionValidationContext validation);

        void Apply(WorldState world, WorldEventBuffer events);
    }

    public sealed class WorldTransactionValidationContext
    {
        private readonly Dictionary<string, long> _reservedAmounts =
            new Dictionary<string, long>(StringComparer.Ordinal);

        public long Reserve(
            string resourceId,
            long requestedAmount,
            long availableAmount,
            string transactionId)
        {
            resourceId = new StableId(resourceId).Value;
            transactionId = new StableId(transactionId).Value;
            if (requestedAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedAmount));
            }

            if (availableAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableAmount));
            }

            _reservedAmounts.TryGetValue(resourceId, out var alreadyReserved);
            var nextReserved = checked(alreadyReserved + requestedAmount);
            if (nextReserved > availableAmount)
            {
                throw new InvalidOperationException(
                    $"Transaction '{transactionId}' cannot reserve " +
                    $"{requestedAmount} from '{resourceId}'; " +
                    $"{alreadyReserved} of {availableAmount} is already reserved.");
            }

            _reservedAmounts[resourceId] = nextReserved;
            return nextReserved;
        }

        public long GetReserved(string resourceId)
        {
            resourceId = new StableId(resourceId).Value;
            return _reservedAmounts.TryGetValue(resourceId, out var amount)
                ? amount
                : 0;
        }
    }

    public sealed class WorldRuntimeEvent
    {
        public WorldRuntimeEvent(
            string id,
            string eventTypeId,
            string sourceTransactionId,
            long day,
            DaySegment segment)
        {
            if (day < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(day));
            }

            _ = new WorldTime(day, segment);
            Id = new StableId(id).Value;
            EventTypeId = new StableId(eventTypeId).Value;
            SourceTransactionId = new StableId(sourceTransactionId).Value;
            Day = day;
            Segment = segment;
        }

        public string Id { get; }

        public string EventTypeId { get; }

        public string SourceTransactionId { get; }

        public long Day { get; }

        public DaySegment Segment { get; }
    }

    public interface IWorldRuntimeEventHandler
    {
        string HandlerId { get; }

        string EventTypeId { get; }

        void Handle(
            WorldRuntimeEvent worldEvent,
            WorldCommandRuntime commandRuntime);
    }

    public sealed class WorldEventBuffer
    {
        private readonly List<WorldRuntimeEvent> _pending =
            new List<WorldRuntimeEvent>();
        private readonly List<WorldRuntimeEvent> _published =
            new List<WorldRuntimeEvent>();

        public IReadOnlyList<WorldRuntimeEvent> Published => _published;

        public void Add(WorldRuntimeEvent worldEvent)
        {
            if (worldEvent == null)
            {
                throw new ArgumentNullException(nameof(worldEvent));
            }

            for (var i = 0; i < _pending.Count; i++)
            {
                if (string.Equals(
                        _pending[i].Id,
                        worldEvent.Id,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"World event '{worldEvent.Id}' is already pending.");
                }
            }

            _pending.Add(worldEvent);
        }

        internal void Publish()
        {
            _pending.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            _published.Clear();
            _published.AddRange(_pending);
            _pending.Clear();
        }

        internal void Clear()
        {
            _pending.Clear();
            _published.Clear();
        }
    }

    public sealed class WorldTransactionBuffer
    {
        private readonly List<IWorldTransaction> _transactions =
            new List<IWorldTransaction>();

        public int Count => _transactions.Count;

        public void Add(IWorldTransaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            _ = new StableId(transaction.Id);
            _ = new StableId(transaction.KindId);
            for (var i = 0; i < _transactions.Count; i++)
            {
                if (string.Equals(
                        _transactions[i].Id,
                        transaction.Id,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"World transaction '{transaction.Id}' is already pending.");
                }
            }

            _transactions.Add(transaction);
        }

        internal void Commit(WorldState world, WorldEventBuffer events)
        {
            Validate(world);
            Apply(world, events);
        }

        internal void Validate(WorldState world)
        {
            _transactions.Sort(CompareTransactions);
            var validation = new WorldTransactionValidationContext();
            for (var i = 0; i < _transactions.Count; i++)
            {
                _transactions[i].Validate(world, validation);
            }
        }

        internal void Apply(WorldState world, WorldEventBuffer events)
        {
            for (var i = 0; i < _transactions.Count; i++)
            {
                _transactions[i].Apply(world, events);
            }

            _transactions.Clear();
        }

        internal List<WorldTransactionExecutionState> CreateExecutionSnapshot()
        {
            _transactions.Sort(CompareTransactions);
            var snapshot = new List<WorldTransactionExecutionState>(
                _transactions.Count);
            for (var i = 0; i < _transactions.Count; i++)
            {
                snapshot.Add(new WorldTransactionExecutionState
                {
                    TransactionId = _transactions[i].Id,
                    TransactionKindId = _transactions[i].KindId,
                    Priority = _transactions[i].Priority
                });
            }

            return snapshot;
        }

        internal void Clear()
        {
            _transactions.Clear();
        }

        private static int CompareTransactions(
            IWorldTransaction left,
            IWorldTransaction right)
        {
            var byPriority = left.Priority.CompareTo(right.Priority);
            return byPriority != 0
                ? byPriority
                : string.CompareOrdinal(left.Id, right.Id);
        }
    }

    public sealed class WorldCommandExecutionReport
    {
        public WorldCommandExecutionReport(
            int processedCommands,
            int committedTransactions,
            int publishedEvents)
        {
            ProcessedCommands = processedCommands;
            CommittedTransactions = committedTransactions;
            PublishedEvents = publishedEvents;
        }

        public int ProcessedCommands { get; }

        public int CommittedTransactions { get; }

        public int PublishedEvents { get; }
    }

    public sealed class WorldCommandRuntime
    {
        private readonly Dictionary<string, IWorldCommandHandler> _handlers =
            new Dictionary<string, IWorldCommandHandler>(StringComparer.Ordinal);
        private readonly List<IWorldRuntimeEventHandler> _eventHandlers =
            new List<IWorldRuntimeEventHandler>();
        private readonly WorldTransactionBuffer _transactions =
            new WorldTransactionBuffer();
        private readonly WorldEventBuffer _events = new WorldEventBuffer();
        private WorldState _boundWorld;

        public int PendingCommandCount
        {
            get
            {
                if (_boundWorld == null)
                {
                    return 0;
                }

                var count = 0;
                for (var i = 0;
                     i < _boundWorld.PersistentWorldCommands.Count;
                     i++)
                {
                    if (_boundWorld.PersistentWorldCommands[i].Status ==
                        PersistentWorldCommandStatus.Pending)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IReadOnlyList<WorldRuntimeEvent> PublishedEvents =>
            _events.Published;

        public WorldCommandExecutionReport LastReport { get; private set; } =
            new WorldCommandExecutionReport(0, 0, 0);

        public void RegisterHandler(IWorldCommandHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var commandTypeId = new StableId(handler.CommandTypeId).Value;
            if (_handlers.ContainsKey(commandTypeId))
            {
                throw new InvalidOperationException(
                    $"Command handler '{commandTypeId}' is already registered.");
            }

            _handlers.Add(commandTypeId, handler);
        }

        public bool HasHandler(string commandTypeId)
        {
            commandTypeId = new StableId(commandTypeId).Value;
            return _handlers.ContainsKey(commandTypeId);
        }

        public void RegisterEventHandler(IWorldRuntimeEventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _ = new StableId(handler.HandlerId);
            _ = new StableId(handler.EventTypeId);
            for (var i = 0; i < _eventHandlers.Count; i++)
            {
                if (string.Equals(
                        _eventHandlers[i].HandlerId,
                        handler.HandlerId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Event handler '{handler.HandlerId}' is already registered.");
                }
            }

            _eventHandlers.Add(handler);
        }

        public bool HasEventHandler(string handlerId)
        {
            handlerId = new StableId(handlerId).Value;
            for (var i = 0; i < _eventHandlers.Count; i++)
            {
                if (string.Equals(
                        _eventHandlers[i].HandlerId,
                        handlerId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void Enqueue(WorldCommandEnvelope command)
        {
            if (_boundWorld == null)
            {
                throw new InvalidOperationException(
                    "Persistent command enqueue requires a bound world. " +
                    "Call Enqueue(world, command) or ProcessDue(world) first.");
            }

            Enqueue(_boundWorld, command);
        }

        public void Enqueue(WorldState world, WorldCommandEnvelope command)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            world.Validate();
            for (var i = 0; i < world.PersistentWorldCommands.Count; i++)
            {
                if (string.Equals(
                        world.PersistentWorldCommands[i].Id,
                        command.Id,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"World command '{command.Id}' is already queued.");
                }
            }

            var arguments = new List<WorldCommandArgumentState>(
                command.Arguments.Count);
            foreach (var pair in command.Arguments)
            {
                arguments.Add(new WorldCommandArgumentState
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }
            arguments.Sort((left, right) =>
                string.CompareOrdinal(left.Key, right.Key));
            world.PersistentWorldCommands.Add(new PersistentWorldCommandState
            {
                Id = command.Id,
                CommandTypeId = command.CommandTypeId,
                IssuerId = command.IssuerId,
                CreatedDay = world.AbsoluteDay,
                CreatedSegment = world.Segment,
                DueDay = command.DueDay,
                DueSegment = (byte)command.DueSegment,
                Priority = command.Priority,
                Status = PersistentWorldCommandStatus.Pending,
                AttemptCount = 0,
                LastAttemptResultId = string.Empty,
                CompletedDay = -1,
                CompletionResultId = string.Empty,
                Arguments = arguments
            });
            world.PersistentWorldCommands.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            _boundWorld = world;
            world.Validate();
        }

        public WorldCommandExecutionReport ProcessDue(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            _boundWorld = world;
            var dueStates = new List<PersistentWorldCommandState>();
            for (var i = 0; i < world.PersistentWorldCommands.Count; i++)
            {
                var command = world.PersistentWorldCommands[i];
                if (command.Status == PersistentWorldCommandStatus.Pending &&
                    IsDue(command, world))
                {
                    dueStates.Add(command);
                }
            }

            dueStates.Sort(CompareCommands);
            _transactions.Clear();
            _events.Clear();
            if (dueStates.Count == 0)
            {
                LastReport = new WorldCommandExecutionReport(0, 0, 0);
                return LastReport;
            }

            var due = new List<WorldCommandEnvelope>(dueStates.Count);
            for (var i = 0; i < dueStates.Count; i++)
            {
                due.Add(ToEnvelope(dueStates[i]));
            }

            List<WorldTransactionExecutionState> transactionSnapshot = null;
            try
            {
                for (var i = 0; i < due.Count; i++)
                {
                    if (!_handlers.TryGetValue(
                            due[i].CommandTypeId,
                            out var handler))
                    {
                        throw new InvalidOperationException(
                            $"No handler is registered for command " +
                            $"'{due[i].CommandTypeId}'.");
                    }

                    handler.Plan(due[i], _transactions);
                }

                transactionSnapshot = _transactions.CreateExecutionSnapshot();
                _transactions.Validate(world);
            }
            catch (Exception exception)
            {
                if (transactionSnapshot == null)
                {
                    transactionSnapshot = _transactions.CreateExecutionSnapshot();
                }
                _transactions.Clear();
                _events.Clear();
                var failureResult = CreateBatchResult(
                    world,
                    dueStates,
                    transactionSnapshot,
                    WorldCommandBatchOutcome.Rejected,
                    exception.Message.StartsWith(
                        "No handler is registered for command ",
                        StringComparison.Ordinal)
                        ? "mandate.command_failure.missing_handler"
                        : "mandate.command_failure.batch_rejected");
                for (var i = 0; i < dueStates.Count; i++)
                {
                    dueStates[i].AttemptCount++;
                    dueStates[i].LastAttemptResultId = failureResult.Id;
                }
                world.WorldCommandBatchResults.Add(failureResult);
                world.Validate();
                throw;
            }

            var transactionCount = _transactions.Count;
            _transactions.Apply(world, _events);
            _events.Publish();
            var result = CreateBatchResult(
                world,
                dueStates,
                transactionSnapshot,
                WorldCommandBatchOutcome.Succeeded,
                string.Empty);
            var publishedEventIds = new HashSet<string>(
                StringComparer.Ordinal);
            for (var i = 0; i < world.WorldEventOutbox.Count; i++)
            {
                publishedEventIds.Add(world.WorldEventOutbox[i].Id);
            }
            for (var i = 0; i < _events.Published.Count; i++)
            {
                var worldEvent = _events.Published[i];
                if (!publishedEventIds.Add(worldEvent.Id))
                {
                    throw new InvalidOperationException(
                        $"World event '{worldEvent.Id}' already exists in the outbox.");
                }

                world.WorldEventOutbox.Add(new WorldEventOutboxState
                {
                    Id = worldEvent.Id,
                    EventTypeId = worldEvent.EventTypeId,
                    SourceTransactionId = worldEvent.SourceTransactionId,
                    Day = worldEvent.Day,
                    Segment = (byte)worldEvent.Segment,
                    DispatchStatus = WorldEventDispatchStatus.Pending,
                    DispatchedDay = -1,
                    DeliveredHandlerIds = new List<string>()
                });
                result.PublishedEventIds.Add(worldEvent.Id);
            }
            result.PublishedEventIds.Sort(StringComparer.Ordinal);
            for (var i = 0; i < dueStates.Count; i++)
            {
                CompleteAttempt(dueStates[i], result, world);
            }
            world.WorldCommandBatchResults.Add(result);
            world.Validate();

            LastReport = new WorldCommandExecutionReport(
                due.Count,
                transactionCount,
                _events.Published.Count);
            return LastReport;
        }

        public void DispatchPublishedEvents()
        {
            if (_boundWorld == null)
            {
                throw new InvalidOperationException(
                    "Persistent event dispatch requires a bound world.");
            }

            DispatchPublishedEvents(_boundWorld);
        }

        public void DispatchPublishedEvents(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            _boundWorld = world;
            var handlers = new List<IWorldRuntimeEventHandler>(_eventHandlers);
            handlers.Sort((left, right) =>
                string.CompareOrdinal(left.HandlerId, right.HandlerId));
            var outbox = new List<WorldEventOutboxState>();
            for (var i = 0; i < world.WorldEventOutbox.Count; i++)
            {
                if (world.WorldEventOutbox[i].DispatchStatus ==
                    WorldEventDispatchStatus.Pending)
                {
                    outbox.Add(world.WorldEventOutbox[i]);
                }
            }
            outbox.Sort(CompareOutboxEvents);
            for (var eventIndex = 0; eventIndex < outbox.Count; eventIndex++)
            {
                var entry = outbox[eventIndex];
                var worldEvent = new WorldRuntimeEvent(
                    entry.Id,
                    entry.EventTypeId,
                    entry.SourceTransactionId,
                    entry.Day,
                    (DaySegment)entry.Segment);
                var matchingHandlers = 0;
                for (var handlerIndex = 0;
                     handlerIndex < handlers.Count;
                     handlerIndex++)
                {
                    if (string.Equals(
                            handlers[handlerIndex].EventTypeId,
                            worldEvent.EventTypeId,
                            StringComparison.Ordinal))
                    {
                        matchingHandlers++;
                        if (entry.DeliveredHandlerIds.Contains(
                                handlers[handlerIndex].HandlerId))
                        {
                            continue;
                        }

                        handlers[handlerIndex].Handle(worldEvent, this);
                        entry.DeliveredHandlerIds.Add(
                            handlers[handlerIndex].HandlerId);
                        entry.DeliveredHandlerIds.Sort(StringComparer.Ordinal);
                    }
                }

                if (matchingHandlers > 0)
                {
                    entry.DispatchStatus = WorldEventDispatchStatus.Dispatched;
                    entry.DispatchedDay = world.AbsoluteDay;
                    entry.DispatchedSegment = world.Segment;
                }
            }

            world.Validate();
        }

        private static bool IsDue(
            PersistentWorldCommandState command,
            WorldState world)
        {
            return command.DueDay < world.AbsoluteDay ||
                command.DueDay == world.AbsoluteDay &&
                command.DueSegment <= world.Segment;
        }

        private static int CompareCommands(
            PersistentWorldCommandState left,
            PersistentWorldCommandState right)
        {
            var byDay = left.DueDay.CompareTo(right.DueDay);
            if (byDay != 0)
            {
                return byDay;
            }

            var bySegment = left.DueSegment.CompareTo(right.DueSegment);
            if (bySegment != 0)
            {
                return bySegment;
            }

            var byPriority = left.Priority.CompareTo(right.Priority);
            return byPriority != 0
                ? byPriority
                : string.CompareOrdinal(left.Id, right.Id);
        }

        private static WorldCommandEnvelope ToEnvelope(
            PersistentWorldCommandState command)
        {
            var arguments = new Dictionary<string, string>(
                StringComparer.Ordinal);
            for (var i = 0; i < command.Arguments.Count; i++)
            {
                arguments.Add(
                    command.Arguments[i].Key,
                    command.Arguments[i].Value);
            }

            return new WorldCommandEnvelope(
                command.Id,
                command.CommandTypeId,
                command.IssuerId,
                command.DueDay,
                (DaySegment)command.DueSegment,
                command.Priority,
                arguments);
        }

        private static WorldCommandBatchResultState CreateBatchResult(
            WorldState world,
            List<PersistentWorldCommandState> commands,
            List<WorldTransactionExecutionState> transactions,
            WorldCommandBatchOutcome outcome,
            string failureCode)
        {
            var commandIds = new List<string>(commands.Count);
            for (var i = 0; i < commands.Count; i++)
            {
                commandIds.Add(commands[i].Id);
            }
            commandIds.Sort(StringComparer.Ordinal);
            var resultId = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "world.command.result.{0:D10}.{1:D2}.{2:D8}",
                world.AbsoluteDay,
                world.Segment,
                world.WorldCommandBatchResults.Count);
            return new WorldCommandBatchResultState
            {
                Id = resultId,
                Outcome = outcome,
                Day = world.AbsoluteDay,
                Segment = world.Segment,
                FailureCode = failureCode,
                CommandIds = commandIds,
                Transactions = transactions ??
                    new List<WorldTransactionExecutionState>(),
                PublishedEventIds = new List<string>()
            };
        }

        private static void CompleteAttempt(
            PersistentWorldCommandState command,
            WorldCommandBatchResultState result,
            WorldState world)
        {
            command.AttemptCount++;
            command.LastAttemptResultId = result.Id;
            command.Status = PersistentWorldCommandStatus.Completed;
            command.CompletedDay = world.AbsoluteDay;
            command.CompletedSegment = world.Segment;
            command.CompletionResultId = result.Id;
        }

        private static int CompareOutboxEvents(
            WorldEventOutboxState left,
            WorldEventOutboxState right)
        {
            var byDay = left.Day.CompareTo(right.Day);
            if (byDay != 0)
            {
                return byDay;
            }

            var bySegment = left.Segment.CompareTo(right.Segment);
            return bySegment != 0
                ? bySegment
                : string.CompareOrdinal(left.Id, right.Id);
        }
    }

    public sealed class LuoyangPassageWorldCommandSystem
    {
        private const int CommandPriority = 24;
        private readonly LuoyangRoadTraversalRefinementPlan _plan;
        private readonly List<PassageDefinition> _passages =
            new List<PassageDefinition>();
        private readonly Dictionary<string, PassageDefinition> _passagesById =
            new Dictionary<string, PassageDefinition>(StringComparer.Ordinal);

        public LuoyangPassageWorldCommandSystem(
            LuoyangRoadTraversalRefinementPlan plan)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            LuoyangRoadConnectorPassageTraversalRules.Validate(plan);
            for (var i = 0; i < plan.PassageFacilityIds.Count; i++)
            {
                var facilityId = plan.PassageFacilityIds[i];
                var definition = new PassageDefinition(
                    facilityId,
                    plan.NavigationNodesByFacilityId[facilityId]
                        .FacilityDefinitionId);
                _passages.Add(definition);
                _passagesById.Add(definition.FacilityId, definition);
            }
            _passages.Sort((left, right) => string.CompareOrdinal(
                left.FacilityId, right.FacilityId));
            if (_passages.Count !=
                LuoyangPassageTraversalWorldContractIds.PassageCount)
                throw new InvalidOperationException(
                    "The Luoyang passage command contract requires exactly " +
                    LuoyangPassageTraversalWorldContractIds.PassageCount +
                    " passages.");
        }

        public void RegisterHandlers(WorldCommandRuntime runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (!runtime.HasHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationCommandTypeId))
                runtime.RegisterHandler(new InitializationCommandHandler(this));
            if (!runtime.HasHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionCommandTypeId))
                runtime.RegisterHandler(new TransitionCommandHandler(this));
            if (!runtime.HasHandler(
                    LuoyangPassageOperationsContractIds
                        .GuardAssignmentCommandTypeId))
                runtime.RegisterHandler(new GuardAssignmentCommandHandler(this));
            if (!runtime.HasHandler(
                    LuoyangPassageOperationsContractIds
                        .RepairStartCommandTypeId))
                runtime.RegisterHandler(new RepairStartCommandHandler(this));
            if (!runtime.HasEventHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationProjectionHandlerId))
                runtime.RegisterEventHandler(new ProjectionHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationProjectionHandlerId,
                    LuoyangPassageTraversalWorldContractIds
                        .InitializedEventTypeId));
            if (!runtime.HasEventHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionProjectionHandlerId))
                runtime.RegisterEventHandler(new ProjectionHandler(
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionProjectionHandlerId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionedEventTypeId));
            if (!runtime.HasEventHandler(
                    LuoyangPassageOperationsContractIds
                        .GuardProjectionHandlerId))
                runtime.RegisterEventHandler(new ProjectionHandler(
                    LuoyangPassageOperationsContractIds
                        .GuardProjectionHandlerId,
                    LuoyangPassageOperationsContractIds
                        .GuardAssignedEventTypeId));
            if (!runtime.HasEventHandler(
                    LuoyangPassageOperationsContractIds
                        .RepairProjectionHandlerId))
                runtime.RegisterEventHandler(new ProjectionHandler(
                    LuoyangPassageOperationsContractIds
                        .RepairProjectionHandlerId,
                    LuoyangPassageOperationsContractIds
                        .RepairStartedEventTypeId));
        }

        public bool EnsureInitialized(
            WorldState world,
            WorldCommandRuntime runtime)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            world.Validate();
            ValidatePersistedPlan(world, allowUninitialized: true);
            if (world.LuoyangPassageTraversals.Count != 0)
                return false;
            for (var i = 0; i < world.PersistentWorldCommands.Count; i++)
            {
                if (string.Equals(world.PersistentWorldCommands[i].Id,
                        LuoyangPassageTraversalWorldContractIds
                            .InitializationCommandId,
                        StringComparison.Ordinal))
                    return false;
            }

            var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    LuoyangPassageTraversalWorldContractIds.ContractArgumentId,
                    LuoyangPassageTraversalWorldContractIds.ContractId
                },
                {
                    LuoyangPassageTraversalWorldContractIds
                        .PassageCountArgumentId,
                    _passages.Count.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                }
            };
            for (var index = 0; index < _passages.Count; index++)
            {
                arguments.Add(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationFacilityArgumentId(index),
                    _passages[index].FacilityId);
                arguments.Add(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationDefinitionArgumentId(index),
                    _passages[index].FacilityDefinitionId);
            }

            runtime.Enqueue(world, new WorldCommandEnvelope(
                LuoyangPassageTraversalWorldContractIds.InitializationCommandId,
                LuoyangPassageTraversalWorldContractIds
                    .InitializationCommandTypeId,
                LuoyangPassageTraversalWorldContractIds.InitializationIssuerId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                arguments));
            return true;
        }

        public bool EnqueueTransition(
            WorldState world,
            WorldCommandRuntime runtime,
            string facilityId,
            string targetStatusId,
            string reasonId,
            string issuerId,
            string commandId = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            facilityId = new StableId(facilityId).Value;
            targetStatusId = new StableId(targetStatusId).Value;
            reasonId = new StableId(reasonId).Value;
            issuerId = new StableId(issuerId).Value;
            if (!IsSupportedStatus(targetStatusId))
                throw new ArgumentException(
                    "Unknown Luoyang passage traversal status.",
                    nameof(targetStatusId));
            world.Validate();
            ValidatePersistedPlan(world, allowUninitialized: false);
            var current = FindState(world, facilityId);
            if (string.Equals(current.TraversalStatusId, targetStatusId,
                    StringComparison.Ordinal))
                return false;
            var nextRevision = checked(current.Revision + 1);
            commandId = string.IsNullOrWhiteSpace(commandId)
                ? CreateTransitionCommandId(facilityId, nextRevision,
                    targetStatusId, reasonId)
                : new StableId(commandId).Value;
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    LuoyangPassageTraversalWorldContractIds.FacilityIdArgumentId,
                    current.FacilityId
                },
                {
                    LuoyangPassageTraversalWorldContractIds
                        .FacilityDefinitionIdArgumentId,
                    current.FacilityDefinitionId
                },
                {
                    LuoyangPassageTraversalWorldContractIds
                        .ExpectedRevisionArgumentId,
                    current.Revision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                },
                {
                    LuoyangPassageTraversalWorldContractIds.TargetStatusArgumentId,
                    targetStatusId
                },
                {
                    LuoyangPassageTraversalWorldContractIds.ReasonArgumentId,
                    reasonId
                }
            };
            var control = TryFindControl(world, facilityId);
            if (control != null)
            {
                if (!string.Equals(targetStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                        StringComparison.Ordinal) &&
                    !string.Equals(targetStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                        StringComparison.Ordinal) ||
                    control.CurrentConditionBasisPoints != 10_000 ||
                    !string.IsNullOrEmpty(control.ActiveRepairOrderId))
                    throw new InvalidOperationException(
                        "An operational passage may be normally opened or " +
                        "closed only while intact and not under repair.");
                var authority = ResolveOperationalAuthority(
                    world, control, issuerId);
                arguments.Add(
                    LuoyangPassageOperationsContractIds.CauseArgumentId,
                    LuoyangPassageOperationsContractIds.GuardOperationCauseId);
                arguments.Add(
                    LuoyangPassageOperationsContractIds
                        .AuthorityBasisArgumentId,
                    authority);
            }
            runtime.Enqueue(world, new WorldCommandEnvelope(
                commandId,
                LuoyangPassageTraversalWorldContractIds.TransitionCommandTypeId,
                issuerId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                arguments));
            return true;
        }

        public bool EnqueueGuardAssignment(
            WorldState world,
            WorldCommandRuntime runtime,
            string facilityId,
            string guardArmyId,
            string authorizingPersonId,
            int initialConditionBasisPoints = 10_000)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            facilityId = new StableId(facilityId).Value;
            guardArmyId = new StableId(guardArmyId).Value;
            authorizingPersonId = new StableId(authorizingPersonId).Value;
            world.Validate();
            ValidatePersistedPlan(world, allowUninitialized: false);
            if (TryFindControl(world, facilityId) != null)
                return false;
            var passage = FindState(world, facilityId);
            var facility = FindFacility(world, facilityId);
            var army = FindArmy(world, guardArmyId);
            var controllerId = ResolveControllerOrganizationId(
                world, facility);
            var authority = ResolveGuardAssignmentAuthority(world, facility,
                army, authorizingPersonId, controllerId);
            ValidateInitialCondition(passage.TraversalStatusId,
                initialConditionBasisPoints);
            var commandId = LuoyangPassageOperationsContractIds
                .GuardCommandId(facilityId);
            runtime.Enqueue(world, new WorldCommandEnvelope(
                commandId,
                LuoyangPassageOperationsContractIds
                    .GuardAssignmentCommandTypeId,
                authorizingPersonId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    {
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityIdArgumentId,
                        facilityId
                    },
                    {
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        passage.FacilityDefinitionId
                    },
                    {
                        LuoyangPassageOperationsContractIds
                            .GuardArmyIdArgumentId,
                        guardArmyId
                    },
                    {
                        LuoyangPassageOperationsContractIds
                            .InitialConditionArgumentId,
                        initialConditionBasisPoints.ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                    },
                    {
                        LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        authority
                    }
                }));
            return true;
        }

        public bool EnqueueBattleDamage(
            WorldState world,
            WorldCommandRuntime runtime,
            string facilityId,
            string battleRecordId,
            int damageBasisPoints,
            string reasonId,
            string attackingCommanderPersonId,
            string commandId = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            facilityId = new StableId(facilityId).Value;
            battleRecordId = new StableId(battleRecordId).Value;
            reasonId = new StableId(reasonId).Value;
            attackingCommanderPersonId =
                new StableId(attackingCommanderPersonId).Value;
            if (damageBasisPoints <= 0 || damageBasisPoints > 10_000)
                throw new ArgumentOutOfRangeException(nameof(damageBasisPoints));
            world.Validate();
            var control = FindControl(world, facilityId);
            if (control.CurrentConditionBasisPoints <= 0 ||
                !string.IsNullOrEmpty(control.ActiveRepairOrderId))
                throw new InvalidOperationException(
                    "A destroyed or actively repaired passage cannot receive " +
                    "this damage command.");
            var battle = FindBattle(world, battleRecordId);
            var attacker = FindArmy(world, battle.AttackerArmyId);
            ResolveAttackingAuthority(world, attacker,
                attackingCommanderPersonId);
            var passage = FindState(world, facilityId);
            var conditionAfter = Math.Max(0,
                control.CurrentConditionBasisPoints - damageBasisPoints);
            var targetStatusId = conditionAfter == 0
                ? LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId
                : LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId;
            var nextRevision = checked(passage.Revision + 1);
            commandId = string.IsNullOrWhiteSpace(commandId)
                ? CreateTransitionCommandId(facilityId, nextRevision,
                    targetStatusId, reasonId) + "." + battleRecordId
                : new StableId(commandId).Value;
            runtime.Enqueue(world, new WorldCommandEnvelope(
                commandId,
                LuoyangPassageTraversalWorldContractIds.TransitionCommandTypeId,
                attackingCommanderPersonId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { LuoyangPassageTraversalWorldContractIds.FacilityIdArgumentId,
                        passage.FacilityId },
                    { LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        passage.FacilityDefinitionId },
                    { LuoyangPassageTraversalWorldContractIds
                            .ExpectedRevisionArgumentId,
                        passage.Revision.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) },
                    { LuoyangPassageTraversalWorldContractIds.TargetStatusArgumentId,
                        targetStatusId },
                    { LuoyangPassageTraversalWorldContractIds.ReasonArgumentId,
                        reasonId },
                    { LuoyangPassageOperationsContractIds.CauseArgumentId,
                        LuoyangPassageOperationsContractIds.BattleDamageCauseId },
                    { LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        LuoyangPassageOperationsContractIds
                            .AttackingArmyCommanderAuthorityId },
                    { LuoyangPassageOperationsContractIds.BattleRecordIdArgumentId,
                        battle.Id },
                    { LuoyangPassageOperationsContractIds.AttackerArmyIdArgumentId,
                        attacker.Id },
                    { LuoyangPassageOperationsContractIds
                            .DamageBasisPointsArgumentId,
                        damageBasisPoints.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) }
                }));
            return true;
        }

        public bool EnqueueStartRepair(
            WorldState world,
            WorldCommandRuntime runtime,
            string facilityId,
            string authorizingPersonId,
            string managerPersonId,
            string materialInventoryContainerId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            facilityId = new StableId(facilityId).Value;
            authorizingPersonId = new StableId(authorizingPersonId).Value;
            managerPersonId = new StableId(managerPersonId).Value;
            materialInventoryContainerId =
                new StableId(materialInventoryContainerId).Value;
            world.Validate();
            var control = FindControl(world, facilityId);
            if (control.CurrentConditionBasisPoints >= 10_000 ||
                string.IsNullOrEmpty(control.LastDamageRecordId) ||
                !string.IsNullOrEmpty(control.ActiveRepairOrderId))
                throw new InvalidOperationException(
                    "The passage does not have repairable audited damage.");
            var authority = ResolveOperationalAuthority(
                world, control, authorizingPersonId);
            var passage = FindState(world, facilityId);
            var orderId = LuoyangPassageOperationsContractIds.RepairOrderId(
                facilityId, control.IntegrityRevision);
            if (world.LuoyangPassageRepairOrders.Any(item => item != null &&
                    string.Equals(item.Id, orderId, StringComparison.Ordinal)))
                return false;
            runtime.Enqueue(world, new WorldCommandEnvelope(
                LuoyangPassageOperationsContractIds.RepairStartCommandId(
                    facilityId, control.IntegrityRevision),
                LuoyangPassageOperationsContractIds.RepairStartCommandTypeId,
                authorizingPersonId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { LuoyangPassageTraversalWorldContractIds.FacilityIdArgumentId,
                        passage.FacilityId },
                    { LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        passage.FacilityDefinitionId },
                    { LuoyangPassageOperationsContractIds
                            .ExpectedIntegrityRevisionArgumentId,
                        control.IntegrityRevision.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) },
                    { LuoyangPassageOperationsContractIds.RepairOrderIdArgumentId,
                        orderId },
                    { LuoyangPassageOperationsContractIds.ManagerPersonIdArgumentId,
                        managerPersonId },
                    { LuoyangPassageOperationsContractIds
                            .InventoryContainerIdArgumentId,
                        materialInventoryContainerId },
                    { LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        authority }
                }));
            return true;
        }

        public FacilityConstructionLaborState ContributeRepairLabor(
            WorldState world,
            string repairOrderId,
            string workerPersonId,
            int laborMinutes)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            repairOrderId = new StableId(repairOrderId).Value;
            workerPersonId = new StableId(workerPersonId).Value;
            world.Validate();
            var order = FindRepairOrder(world, repairOrderId);
            if (order.Status != LuoyangPassageRepairStatus.InProgress)
                throw new InvalidOperationException(
                    "The Luoyang passage repair is already complete.");
            var control = FindControl(world, order.FacilityId);
            if (!IsEligibleRepairWorker(world, control, workerPersonId))
                throw new InvalidOperationException(
                    "The repair worker is not part of the controlling " +
                    "organization or guard army.");
            return new PropertyConstructionSystem().ContributeLabor(world,
                order.FacilityConstructionProjectId, workerPersonId,
                laborMinutes);
        }

        public bool EnqueueCompleteRepair(
            WorldState world,
            WorldCommandRuntime runtime,
            string repairOrderId,
            string authorizingPersonId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            repairOrderId = new StableId(repairOrderId).Value;
            authorizingPersonId = new StableId(authorizingPersonId).Value;
            world.Validate();
            var order = FindRepairOrder(world, repairOrderId);
            if (order.Status == LuoyangPassageRepairStatus.Completed)
                return false;
            var control = FindControl(world, order.FacilityId);
            var authority = ResolveOperationalAuthority(
                world, control, authorizingPersonId);
            var project = FindConstructionProject(
                world, order.FacilityConstructionProjectId);
            if (project.CompletedLaborMinutes < project.RequiredLaborMinutes ||
                world.AbsoluteDay < project.EarliestCompletionDay)
                throw new InvalidOperationException(
                    "The passage repair lacks labor or elapsed world time.");
            var passage = FindState(world, order.FacilityId);
            var targetStatusId =
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId;
            var nextRevision = checked(passage.Revision + 1);
            var commandId = CreateTransitionCommandId(passage.FacilityId,
                nextRevision, targetStatusId,
                LuoyangPassageOperationsContractIds.RepairCompletionReasonId) +
                "." + order.Id;
            runtime.Enqueue(world, new WorldCommandEnvelope(
                commandId,
                LuoyangPassageTraversalWorldContractIds.TransitionCommandTypeId,
                authorizingPersonId,
                world.AbsoluteDay,
                (DaySegment)world.Segment,
                CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { LuoyangPassageTraversalWorldContractIds.FacilityIdArgumentId,
                        passage.FacilityId },
                    { LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        passage.FacilityDefinitionId },
                    { LuoyangPassageTraversalWorldContractIds
                            .ExpectedRevisionArgumentId,
                        passage.Revision.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) },
                    { LuoyangPassageTraversalWorldContractIds.TargetStatusArgumentId,
                        targetStatusId },
                    { LuoyangPassageTraversalWorldContractIds.ReasonArgumentId,
                        LuoyangPassageOperationsContractIds
                            .RepairCompletionReasonId },
                    { LuoyangPassageOperationsContractIds.CauseArgumentId,
                        LuoyangPassageOperationsContractIds
                            .RepairCompletionCauseId },
                    { LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        authority },
                    { LuoyangPassageOperationsContractIds.RepairOrderIdArgumentId,
                        order.Id }
                }));
            return true;
        }

        public void ValidatePersistedPlan(
            WorldState world,
            bool allowUninitialized = false)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (world.LuoyangPassageTraversals.Count == 0 && allowUninitialized)
                return;
            if (world.LuoyangPassageTraversals.Count != _passages.Count)
                throw new InvalidOperationException(
                    "The persisted Luoyang passage set does not match the " +
                    "authored navigation plan.");
            for (var index = 0; index < _passages.Count; index++)
            {
                var state = world.LuoyangPassageTraversals[index];
                var authored = _passages[index];
                if (!string.Equals(state.FacilityId, authored.FacilityId,
                        StringComparison.Ordinal) ||
                    !string.Equals(state.FacilityDefinitionId,
                        authored.FacilityDefinitionId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Persisted Luoyang passage content drifted at " +
                        authored.FacilityId + ".");
            }
        }

        public static string CreateTransitionCommandId(
            string facilityId,
            long revision,
            string targetStatusId,
            string reasonId)
        {
            if (revision <= 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.passage.transition.command.{0}.revision.{1:D8}.{2}.{3}",
                new StableId(facilityId).Value,
                revision,
                new StableId(targetStatusId).Value,
                new StableId(reasonId).Value);
        }

        private static LuoyangPassageTraversalWorldState FindState(
            WorldState world,
            string facilityId)
        {
            for (var i = 0; i < world.LuoyangPassageTraversals.Count; i++)
            {
                if (string.Equals(
                        world.LuoyangPassageTraversals[i].FacilityId,
                        facilityId,
                        StringComparison.Ordinal))
                    return world.LuoyangPassageTraversals[i];
            }
            throw new KeyNotFoundException(
                "Unknown persisted Luoyang passage Facility ID: " + facilityId);
        }

        private static bool IsSupportedStatus(string statusId)
        {
            for (var i = 0;
                 i < LuoyangRoadConnectorPassageTraversalIds.StatusIds.Count;
                 i++)
            {
                if (string.Equals(
                        LuoyangRoadConnectorPassageTraversalIds.StatusIds[i],
                        statusId,
                        StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private sealed class InitializationCommandHandler :
            IWorldCommandHandler
        {
            private readonly LuoyangPassageWorldCommandSystem _owner;

            public InitializationCommandHandler(
                LuoyangPassageWorldCommandSystem owner)
            {
                _owner = owner;
            }

            public string CommandTypeId =>
                LuoyangPassageTraversalWorldContractIds
                    .InitializationCommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                var expectedCount = 2 + _owner._passages.Count * 2;
                if (command.Arguments.Count != expectedCount ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .ContractArgumentId,
                        out var contractId) ||
                    !string.Equals(contractId,
                        LuoyangPassageTraversalWorldContractIds.ContractId,
                        StringComparison.Ordinal) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .PassageCountArgumentId,
                        out var countText) ||
                    !int.TryParse(countText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var count) || count != _owner._passages.Count)
                    throw new InvalidOperationException(
                        "Luoyang passage initialization command arguments are invalid.");
                for (var index = 0;
                     index < _owner._passages.Count;
                     index++)
                {
                    var expected = _owner._passages[index];
                    if (!command.Arguments.TryGetValue(
                            LuoyangPassageTraversalWorldContractIds
                                .InitializationFacilityArgumentId(index),
                            out var facilityId) ||
                        !command.Arguments.TryGetValue(
                            LuoyangPassageTraversalWorldContractIds
                                .InitializationDefinitionArgumentId(index),
                            out var definitionId) ||
                        !string.Equals(facilityId, expected.FacilityId,
                            StringComparison.Ordinal) ||
                        !string.Equals(definitionId,
                            expected.FacilityDefinitionId,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Luoyang passage initialization content drifted.");
                }
                transactions.Add(new InitializationTransaction(
                    command.Id, _owner._passages));
            }
        }

        private sealed class InitializationTransaction : IWorldTransaction
        {
            private readonly string _commandId;
            private readonly List<PassageDefinition> _passages;

            public InitializationTransaction(
                string commandId,
                IReadOnlyList<PassageDefinition> passages)
            {
                _commandId = commandId;
                _passages = new List<PassageDefinition>(passages);
            }

            public string Id =>
                LuoyangPassageTraversalWorldContractIds
                    .InitializationTransactionId;

            public string KindId =>
                LuoyangPassageTraversalWorldContractIds
                    .InitializationTransactionKindId;

            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                if (world.LuoyangPassageTraversals.Count != 0)
                    throw new InvalidOperationException(
                        "Luoyang passage world state is already initialized.");
                validation.Reserve(
                    "luoyang.passage.initialization",
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                for (var index = 0; index < _passages.Count; index++)
                {
                    var passage = _passages[index];
                    world.LuoyangPassageTraversals.Add(
                        new LuoyangPassageTraversalWorldState
                        {
                            Id = LuoyangPassageTraversalWorldContractIds
                                .StateId(passage.FacilityId),
                            FacilityId = passage.FacilityId,
                            FacilityDefinitionId =
                                passage.FacilityDefinitionId,
                            TraversalStatusId =
                                LuoyangRoadConnectorPassageTraversalIds
                                    .OpenStatusId,
                            Revision = 0,
                            LastChangedDay = world.AbsoluteDay,
                            LastChangedSegment = world.Segment,
                            LastReasonId =
                                LuoyangRoadConnectorPassageTraversalIds
                                    .InitialReasonId,
                            LastCommandId = _commandId,
                            LastEventId =
                                LuoyangPassageTraversalWorldContractIds
                                    .InitializationEventId
                        });
                }
                events.Add(new WorldRuntimeEvent(
                    LuoyangPassageTraversalWorldContractIds
                        .InitializationEventId,
                    LuoyangPassageTraversalWorldContractIds
                        .InitializedEventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
            }
        }

        private sealed class TransitionCommandHandler : IWorldCommandHandler
        {
            private readonly LuoyangPassageWorldCommandSystem _owner;

            public TransitionCommandHandler(
                LuoyangPassageWorldCommandSystem owner)
            {
                _owner = owner;
            }

            public string CommandTypeId =>
                LuoyangPassageTraversalWorldContractIds.TransitionCommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count < 5 ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityIdArgumentId,
                        out var facilityId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        out var definitionId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .ExpectedRevisionArgumentId,
                        out var revisionText) ||
                    !long.TryParse(revisionText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var expectedRevision) || expectedRevision < 0 ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .TargetStatusArgumentId,
                        out var targetStatusId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds.ReasonArgumentId,
                        out var reasonId) ||
                    !_owner._passagesById.TryGetValue(facilityId,
                        out var authored) ||
                    !string.Equals(authored.FacilityDefinitionId, definitionId,
                        StringComparison.Ordinal) ||
                    !IsSupportedStatus(targetStatusId))
                    throw new InvalidOperationException(
                        "Luoyang passage transition command arguments are invalid.");
                _ = new StableId(reasonId);
                if (command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds.CauseArgumentId,
                        out var causeId))
                {
                    if (!command.Arguments.TryGetValue(
                            LuoyangPassageOperationsContractIds
                                .AuthorityBasisArgumentId,
                            out var authorityBasisId))
                        throw new InvalidOperationException(
                            "Operational transition authority is missing.");
                    if (string.Equals(causeId,
                            LuoyangPassageOperationsContractIds
                                .GuardOperationCauseId,
                            StringComparison.Ordinal))
                    {
                        if (command.Arguments.Count != 7)
                            throw new InvalidOperationException(
                                "Guarded transition arguments are invalid.");
                        transactions.Add(new GuardedTransitionTransaction(
                            command.Id, command.IssuerId, authored,
                            expectedRevision, targetStatusId, reasonId,
                            authorityBasisId));
                        return;
                    }
                    if (string.Equals(causeId,
                            LuoyangPassageOperationsContractIds
                                .BattleDamageCauseId,
                            StringComparison.Ordinal))
                    {
                        if (command.Arguments.Count != 10 ||
                            !command.Arguments.TryGetValue(
                                LuoyangPassageOperationsContractIds
                                    .BattleRecordIdArgumentId,
                                out var battleRecordId) ||
                            !command.Arguments.TryGetValue(
                                LuoyangPassageOperationsContractIds
                                    .AttackerArmyIdArgumentId,
                                out var attackerArmyId) ||
                            !command.Arguments.TryGetValue(
                                LuoyangPassageOperationsContractIds
                                    .DamageBasisPointsArgumentId,
                                out var damageText) ||
                            !int.TryParse(damageText,
                                System.Globalization.NumberStyles.None,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var damageBasisPoints))
                            throw new InvalidOperationException(
                                "Battle-damage transition arguments are invalid.");
                        transactions.Add(new BattleDamageTransitionTransaction(
                            command.Id, command.IssuerId, authored,
                            expectedRevision, targetStatusId, reasonId,
                            authorityBasisId, battleRecordId, attackerArmyId,
                            damageBasisPoints));
                        return;
                    }
                    if (string.Equals(causeId,
                            LuoyangPassageOperationsContractIds
                                .RepairCompletionCauseId,
                            StringComparison.Ordinal))
                    {
                        if (command.Arguments.Count != 8 ||
                            !command.Arguments.TryGetValue(
                                LuoyangPassageOperationsContractIds
                                    .RepairOrderIdArgumentId,
                                out var repairOrderId))
                            throw new InvalidOperationException(
                                "Repair-completion transition arguments are invalid.");
                        transactions.Add(new RepairCompletionTransitionTransaction(
                            command.Id, command.IssuerId, authored,
                            expectedRevision, targetStatusId, reasonId,
                            authorityBasisId, repairOrderId));
                        return;
                    }
                    throw new InvalidOperationException(
                        "Unknown operational passage transition cause.");
                }
                if (command.Arguments.Count != 5)
                    throw new InvalidOperationException(
                        "Legacy transition arguments are invalid.");
                transactions.Add(new TransitionTransaction(
                    command.Id,
                    authored,
                    expectedRevision,
                    targetStatusId,
                    reasonId));
            }
        }

        private sealed class TransitionTransaction : IWorldTransaction
        {
            private readonly string _commandId;
            private readonly PassageDefinition _passage;
            private readonly long _expectedRevision;
            private readonly string _targetStatusId;
            private readonly string _reasonId;

            public TransitionTransaction(
                string commandId,
                PassageDefinition passage,
                long expectedRevision,
                string targetStatusId,
                string reasonId)
            {
                _commandId = commandId;
                _passage = passage;
                _expectedRevision = expectedRevision;
                _targetStatusId = targetStatusId;
                _reasonId = reasonId;
                Id = LuoyangPassageTraversalWorldContractIds
                    .TransitionTransactionId(
                        passage.FacilityId,
                        checked(expectedRevision + 1),
                        commandId);
            }

            public string Id { get; }

            public string KindId =>
                LuoyangPassageTraversalWorldContractIds
                    .TransitionTransactionKindId;

            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var current = FindState(world, _passage.FacilityId);
                if (!string.Equals(current.FacilityDefinitionId,
                        _passage.FacilityDefinitionId,
                        StringComparison.Ordinal) ||
                    current.Revision != _expectedRevision ||
                    string.Equals(current.TraversalStatusId, _targetStatusId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Luoyang passage transition precondition drifted for " +
                        _passage.FacilityId + ".");
                if (TryFindControl(world, _passage.FacilityId) != null)
                    throw new InvalidOperationException(
                        "A controlled Luoyang passage rejects legacy " +
                        "uncausaled transitions.");
                validation.Reserve(
                    "luoyang.passage.transition." + _passage.FacilityId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var current = FindState(world, _passage.FacilityId);
                var nextRevision = checked(_expectedRevision + 1);
                var eventId = LuoyangPassageTraversalWorldContractIds
                    .TransitionEventId(_passage.FacilityId, nextRevision);
                current.TraversalStatusId = _targetStatusId;
                current.Revision = nextRevision;
                current.LastChangedDay = world.AbsoluteDay;
                current.LastChangedSegment = world.Segment;
                current.LastReasonId = _reasonId;
                current.LastCommandId = _commandId;
                current.LastEventId = eventId;
                events.Add(new WorldRuntimeEvent(
                    eventId,
                    LuoyangPassageTraversalWorldContractIds
                        .TransitionedEventTypeId,
                    Id,
                    world.AbsoluteDay,
                        (DaySegment)world.Segment));
            }
        }

        private sealed class GuardedTransitionTransaction : IWorldTransaction
        {
            private readonly string _commandId;
            private readonly string _issuerPersonId;
            private readonly PassageDefinition _passage;
            private readonly long _expectedRevision;
            private readonly string _targetStatusId;
            private readonly string _reasonId;
            private readonly string _authorityBasisId;

            public GuardedTransitionTransaction(
                string commandId,
                string issuerPersonId,
                PassageDefinition passage,
                long expectedRevision,
                string targetStatusId,
                string reasonId,
                string authorityBasisId)
            {
                _commandId = commandId;
                _issuerPersonId = issuerPersonId;
                _passage = passage;
                _expectedRevision = expectedRevision;
                _targetStatusId = targetStatusId;
                _reasonId = reasonId;
                _authorityBasisId = authorityBasisId;
                Id = LuoyangPassageTraversalWorldContractIds
                    .TransitionTransactionId(passage.FacilityId,
                        checked(expectedRevision + 1), commandId);
            }

            public string Id { get; }
            public string KindId => LuoyangPassageTraversalWorldContractIds
                .TransitionTransactionKindId;
            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var current = FindState(world, _passage.FacilityId);
                var control = FindControl(world, _passage.FacilityId);
                var actualAuthority = ResolveOperationalAuthority(
                    world, control, _issuerPersonId);
                if (current.Revision != _expectedRevision ||
                    string.Equals(current.TraversalStatusId, _targetStatusId,
                        StringComparison.Ordinal) ||
                    control.CurrentConditionBasisPoints != 10_000 ||
                    !string.IsNullOrEmpty(control.ActiveRepairOrderId) ||
                    !string.Equals(actualAuthority, _authorityBasisId,
                        StringComparison.Ordinal) ||
                    !string.Equals(_targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                         StringComparison.Ordinal) &&
                    !string.Equals(_targetStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                         StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Guarded passage transition preconditions drifted.");
                ReservePassage(validation, _passage.FacilityId, Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                ApplyPassageTransition(world, events, _passage.FacilityId,
                    _expectedRevision, _targetStatusId, _reasonId,
                    _commandId, Id);
            }
        }

        private sealed class BattleDamageTransitionTransaction :
            IWorldTransaction
        {
            private readonly string _commandId;
            private readonly string _issuerPersonId;
            private readonly PassageDefinition _passage;
            private readonly long _expectedRevision;
            private readonly string _targetStatusId;
            private readonly string _reasonId;
            private readonly string _authorityBasisId;
            private readonly string _battleRecordId;
            private readonly string _attackerArmyId;
            private readonly int _damageBasisPoints;

            public BattleDamageTransitionTransaction(
                string commandId,
                string issuerPersonId,
                PassageDefinition passage,
                long expectedRevision,
                string targetStatusId,
                string reasonId,
                string authorityBasisId,
                string battleRecordId,
                string attackerArmyId,
                int damageBasisPoints)
            {
                _commandId = commandId;
                _issuerPersonId = issuerPersonId;
                _passage = passage;
                _expectedRevision = expectedRevision;
                _targetStatusId = targetStatusId;
                _reasonId = reasonId;
                _authorityBasisId = authorityBasisId;
                _battleRecordId = battleRecordId;
                _attackerArmyId = attackerArmyId;
                _damageBasisPoints = damageBasisPoints;
                Id = LuoyangPassageTraversalWorldContractIds
                    .TransitionTransactionId(passage.FacilityId,
                        checked(expectedRevision + 1), commandId);
            }

            public string Id { get; }
            public string KindId => LuoyangPassageTraversalWorldContractIds
                .TransitionTransactionKindId;
            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var passage = FindState(world, _passage.FacilityId);
                var control = FindControl(world, _passage.FacilityId);
                var facility = FindFacility(world, _passage.FacilityId);
                var battle = FindBattle(world, _battleRecordId);
                var attacker = FindArmy(world, _attackerArmyId);
                ResolveAttackingAuthority(world, attacker, _issuerPersonId);
                var conditionAfter = Math.Max(0,
                    control.CurrentConditionBasisPoints - _damageBasisPoints);
                var expectedStatus = conditionAfter == 0
                    ? LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId
                    : LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId;
                if (passage.Revision != _expectedRevision ||
                    control.CurrentConditionBasisPoints <= 0 ||
                    !string.IsNullOrEmpty(control.ActiveRepairOrderId) ||
                    _damageBasisPoints <= 0 || _damageBasisPoints > 10_000 ||
                    !string.Equals(_authorityBasisId,
                        LuoyangPassageOperationsContractIds
                            .AttackingArmyCommanderAuthorityId,
                        StringComparison.Ordinal) ||
                    !string.Equals(expectedStatus, _targetStatusId,
                        StringComparison.Ordinal) ||
                    !string.Equals(battle.AttackerArmyId, attacker.Id,
                        StringComparison.Ordinal) ||
                    !string.Equals(battle.DefenderArmyId, control.GuardArmyId,
                        StringComparison.Ordinal) ||
                    !string.Equals(battle.LocationId, facility.SettlementId,
                        StringComparison.Ordinal) ||
                    battle.Day > world.AbsoluteDay ||
                    string.Equals(attacker.OrganizationId,
                        control.ControllerOrganizationId,
                        StringComparison.Ordinal) ||
                    world.LuoyangPassageDamageRecords.Any(item => item != null &&
                        string.Equals(item.FacilityId, _passage.FacilityId,
                            StringComparison.Ordinal) &&
                        string.Equals(item.BattleRecordId, battle.Id,
                            StringComparison.Ordinal)))
                    throw new InvalidOperationException(
                        "Battle damage is not backed by a valid hostile battle.");
                ReservePassage(validation, _passage.FacilityId, Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var passage = FindState(world, _passage.FacilityId);
                var control = FindControl(world, _passage.FacilityId);
                var facility = FindFacility(world, _passage.FacilityId);
                var before = control.CurrentConditionBasisPoints;
                var after = Math.Max(0, before - _damageBasisPoints);
                var integrityRevision = checked(control.IntegrityRevision + 1);
                var passageRevision = checked(_expectedRevision + 1);
                var eventId = LuoyangPassageTraversalWorldContractIds
                    .TransitionEventId(_passage.FacilityId, passageRevision);
                var damage = new LuoyangPassageDamageRecordState
                {
                    Id = LuoyangPassageOperationsContractIds.DamageRecordId(
                        _passage.FacilityId, integrityRevision),
                    FacilityId = _passage.FacilityId,
                    BattleRecordId = _battleRecordId,
                    AttackerArmyId = _attackerArmyId,
                    AttackerCommanderPersonId = _issuerPersonId,
                    AuthorityBasisId = _authorityBasisId,
                    DamageBasisPoints = _damageBasisPoints,
                    ConditionBeforeBasisPoints = before,
                    ConditionAfterBasisPoints = after,
                    IntegrityRevision = integrityRevision,
                    PassageRevisionBefore = _expectedRevision,
                    PassageRevisionAfter = passageRevision,
                    Day = world.AbsoluteDay,
                    Segment = world.Segment,
                    CommandId = _commandId,
                    EventId = eventId
                };
                control.CurrentConditionBasisPoints = after;
                control.IntegrityRevision = integrityRevision;
                control.LastDamageRecordId = damage.Id;
                facility.ConditionBasisPoints = after;
                facility.LifecycleStatus = after == 0
                    ? FacilityLifecycleStatus.Destroyed
                    : FacilityLifecycleStatus.Operational;
                world.LuoyangPassageDamageRecords.Add(damage);
                ApplyPassageTransition(world, events, passage.FacilityId,
                    _expectedRevision, _targetStatusId, _reasonId,
                    _commandId, Id);
            }
        }

        private sealed class RepairCompletionTransitionTransaction :
            IWorldTransaction
        {
            private readonly string _commandId;
            private readonly string _issuerPersonId;
            private readonly PassageDefinition _passage;
            private readonly long _expectedRevision;
            private readonly string _targetStatusId;
            private readonly string _reasonId;
            private readonly string _authorityBasisId;
            private readonly string _repairOrderId;

            public RepairCompletionTransitionTransaction(
                string commandId,
                string issuerPersonId,
                PassageDefinition passage,
                long expectedRevision,
                string targetStatusId,
                string reasonId,
                string authorityBasisId,
                string repairOrderId)
            {
                _commandId = commandId;
                _issuerPersonId = issuerPersonId;
                _passage = passage;
                _expectedRevision = expectedRevision;
                _targetStatusId = targetStatusId;
                _reasonId = reasonId;
                _authorityBasisId = authorityBasisId;
                _repairOrderId = repairOrderId;
                Id = LuoyangPassageTraversalWorldContractIds
                    .TransitionTransactionId(passage.FacilityId,
                        checked(expectedRevision + 1), commandId);
            }

            public string Id { get; }
            public string KindId => LuoyangPassageTraversalWorldContractIds
                .TransitionTransactionKindId;
            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var passage = FindState(world, _passage.FacilityId);
                var control = FindControl(world, _passage.FacilityId);
                var order = FindRepairOrder(world, _repairOrderId);
                var project = FindConstructionProject(
                    world, order.FacilityConstructionProjectId);
                var actualAuthority = ResolveOperationalAuthority(
                    world, control, _issuerPersonId);
                if (passage.Revision != _expectedRevision ||
                    order.Status != LuoyangPassageRepairStatus.InProgress ||
                    !string.Equals(order.FacilityId, _passage.FacilityId,
                        StringComparison.Ordinal) ||
                    !string.Equals(control.ActiveRepairOrderId, order.Id,
                        StringComparison.Ordinal) ||
                    control.IntegrityRevision !=
                        order.SourceIntegrityRevision ||
                    project.CompletedLaborMinutes < project.RequiredLaborMinutes ||
                    world.AbsoluteDay < project.EarliestCompletionDay ||
                    !string.Equals(actualAuthority, _authorityBasisId,
                        StringComparison.Ordinal) ||
                    !string.Equals(_targetStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                        StringComparison.Ordinal) ||
                    !string.Equals(_reasonId,
                        LuoyangPassageOperationsContractIds
                            .RepairCompletionReasonId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Passage repair completion preconditions drifted.");
                ReservePassage(validation, _passage.FacilityId, Id);
                validation.Reserve("facility.construction.project." + project.Id,
                    1, 1, Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var control = FindControl(world, _passage.FacilityId);
                var order = FindRepairOrder(world, _repairOrderId);
                var facility = new PropertyConstructionSystem()
                    .TryCompleteDeferredValidation(world,
                        order.FacilityConstructionProjectId) ??
                    throw new InvalidOperationException(
                        "The audited passage repair did not complete.");
                var eventId = LuoyangPassageTraversalWorldContractIds
                    .TransitionEventId(_passage.FacilityId,
                        checked(_expectedRevision + 1));
                control.CurrentConditionBasisPoints = 10_000;
                control.IntegrityRevision = checked(
                    control.IntegrityRevision + 1);
                control.ActiveRepairOrderId = string.Empty;
                facility.ConditionBasisPoints = 10_000;
                facility.LifecycleStatus = FacilityLifecycleStatus.Operational;
                order.Status = LuoyangPassageRepairStatus.Completed;
                order.CompletedDay = world.AbsoluteDay;
                order.CompletionCommandId = _commandId;
                order.CompletionEventId = eventId;
                ApplyPassageTransition(world, events, _passage.FacilityId,
                    _expectedRevision, _targetStatusId, _reasonId,
                    _commandId, Id);
            }
        }

        private sealed class GuardAssignmentCommandHandler :
            IWorldCommandHandler
        {
            private readonly LuoyangPassageWorldCommandSystem _owner;

            public GuardAssignmentCommandHandler(
                LuoyangPassageWorldCommandSystem owner)
            {
                _owner = owner;
            }

            public string CommandTypeId => LuoyangPassageOperationsContractIds
                .GuardAssignmentCommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 5 ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityIdArgumentId,
                        out var facilityId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        out var definitionId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .GuardArmyIdArgumentId,
                        out var guardArmyId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .InitialConditionArgumentId,
                        out var conditionText) ||
                    !int.TryParse(conditionText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var initialCondition) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        out var authorityBasisId) ||
                    !_owner._passagesById.TryGetValue(facilityId,
                        out var passage) ||
                    !string.Equals(passage.FacilityDefinitionId, definitionId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Luoyang passage guard command arguments are invalid.");
                transactions.Add(new GuardAssignmentTransaction(
                    command.Id, command.IssuerId, passage, guardArmyId,
                    initialCondition, authorityBasisId));
            }
        }

        private sealed class GuardAssignmentTransaction : IWorldTransaction
        {
            private readonly string _commandId;
            private readonly string _authorizingPersonId;
            private readonly PassageDefinition _passage;
            private readonly string _guardArmyId;
            private readonly int _initialCondition;
            private readonly string _authorityBasisId;

            public GuardAssignmentTransaction(
                string commandId,
                string authorizingPersonId,
                PassageDefinition passage,
                string guardArmyId,
                int initialCondition,
                string authorityBasisId)
            {
                _commandId = commandId;
                _authorizingPersonId = authorizingPersonId;
                _passage = passage;
                _guardArmyId = guardArmyId;
                _initialCondition = initialCondition;
                _authorityBasisId = authorityBasisId;
                Id = LuoyangPassageOperationsContractIds.GuardTransactionId(
                    passage.FacilityId);
            }

            public string Id { get; }
            public string KindId => LuoyangPassageOperationsContractIds
                .GuardAssignmentTransactionKindId;
            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var passage = FindState(world, _passage.FacilityId);
                var facility = FindFacility(world, _passage.FacilityId);
                var army = FindArmy(world, _guardArmyId);
                var controllerId = ResolveControllerOrganizationId(
                    world, facility);
                var authority = ResolveGuardAssignmentAuthority(world,
                    facility, army, _authorizingPersonId, controllerId);
                ValidateInitialCondition(passage.TraversalStatusId,
                    _initialCondition);
                var guardPeople = CollectGuardPeople(
                    world, army, facility.SettlementId);
                if (TryFindControl(world, _passage.FacilityId) != null ||
                    !string.Equals(authority, _authorityBasisId,
                        StringComparison.Ordinal) || guardPeople.Count == 0)
                    throw new InvalidOperationException(
                        "Luoyang passage guard assignment preconditions drifted.");
                ReservePassage(validation, _passage.FacilityId, Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var passage = FindState(world, _passage.FacilityId);
                var facility = FindFacility(world, _passage.FacilityId);
                var army = FindArmy(world, _guardArmyId);
                var controllerId = ResolveControllerOrganizationId(
                    world, facility);
                var eventId = LuoyangPassageOperationsContractIds.GuardEventId(
                    _passage.FacilityId);
                world.LuoyangPassageOperationalControls.Add(
                    new LuoyangPassageOperationalControlState
                    {
                        Id = LuoyangPassageOperationsContractIds.ControlId(
                            _passage.FacilityId),
                        FacilityId = _passage.FacilityId,
                        ControllerOrganizationId = controllerId,
                        GuardArmyId = army.Id,
                        GuardCommanderPersonId = army.CommanderPersonId,
                        GuardPersonIds = CollectGuardPeople(
                            world, army, facility.SettlementId),
                        AuthorizedByPersonId = _authorizingPersonId,
                        AuthorityBasisId = _authorityBasisId,
                        ActivatedPassageRevision = passage.Revision,
                        InitialTraversalStatusId = passage.TraversalStatusId,
                        InitialConditionBasisPoints = _initialCondition,
                        CurrentConditionBasisPoints = _initialCondition,
                        IntegrityRevision = 0,
                        LastDamageRecordId = string.Empty,
                        ActiveRepairOrderId = string.Empty,
                        AssignedDay = world.AbsoluteDay,
                        AssignedSegment = world.Segment,
                        AssignmentCommandId = _commandId,
                        AssignmentEventId = eventId
                    });
                facility.ConditionBasisPoints = _initialCondition;
                facility.LifecycleStatus = _initialCondition == 0
                    ? FacilityLifecycleStatus.Destroyed
                    : FacilityLifecycleStatus.Operational;
                events.Add(new WorldRuntimeEvent(eventId,
                    LuoyangPassageOperationsContractIds.GuardAssignedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }

        private sealed class RepairStartCommandHandler : IWorldCommandHandler
        {
            private readonly LuoyangPassageWorldCommandSystem _owner;

            public RepairStartCommandHandler(
                LuoyangPassageWorldCommandSystem owner)
            {
                _owner = owner;
            }

            public string CommandTypeId => LuoyangPassageOperationsContractIds
                .RepairStartCommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 7 ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityIdArgumentId,
                        out var facilityId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageTraversalWorldContractIds
                            .FacilityDefinitionIdArgumentId,
                        out var definitionId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .ExpectedIntegrityRevisionArgumentId,
                        out var integrityText) ||
                    !long.TryParse(integrityText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var expectedIntegrityRevision) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .RepairOrderIdArgumentId,
                        out var repairOrderId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .ManagerPersonIdArgumentId,
                        out var managerPersonId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .InventoryContainerIdArgumentId,
                        out var containerId) ||
                    !command.Arguments.TryGetValue(
                        LuoyangPassageOperationsContractIds
                            .AuthorityBasisArgumentId,
                        out var authorityBasisId) ||
                    !_owner._passagesById.TryGetValue(facilityId,
                        out var passage) ||
                    !string.Equals(passage.FacilityDefinitionId, definitionId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Luoyang passage repair-start arguments are invalid.");
                transactions.Add(new RepairStartTransaction(
                    command.Id, command.IssuerId, passage,
                    expectedIntegrityRevision, repairOrderId, managerPersonId,
                    containerId, authorityBasisId));
            }
        }

        private sealed class RepairStartTransaction : IWorldTransaction
        {
            private readonly string _commandId;
            private readonly string _authorizingPersonId;
            private readonly PassageDefinition _passage;
            private readonly long _expectedIntegrityRevision;
            private readonly string _repairOrderId;
            private readonly string _managerPersonId;
            private readonly string _containerId;
            private readonly string _authorityBasisId;

            public RepairStartTransaction(
                string commandId,
                string authorizingPersonId,
                PassageDefinition passage,
                long expectedIntegrityRevision,
                string repairOrderId,
                string managerPersonId,
                string containerId,
                string authorityBasisId)
            {
                _commandId = commandId;
                _authorizingPersonId = authorizingPersonId;
                _passage = passage;
                _expectedIntegrityRevision = expectedIntegrityRevision;
                _repairOrderId = repairOrderId;
                _managerPersonId = managerPersonId;
                _containerId = containerId;
                _authorityBasisId = authorityBasisId;
                Id = LuoyangPassageOperationsContractIds
                    .RepairStartTransactionId(repairOrderId);
            }

            public string Id { get; }
            public string KindId => LuoyangPassageOperationsContractIds
                .RepairStartTransactionKindId;
            public int Priority => CommandPriority;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                var control = FindControl(world, _passage.FacilityId);
                var facility = FindFacility(world, _passage.FacilityId);
                var container = FindInventoryContainer(world, _containerId);
                var actualAuthority = ResolveOperationalAuthority(world,
                    control, _authorizingPersonId);
                var profile = RepairProfileFor(_passage.FacilityDefinitionId);
                var requirements = RepairRequirements(profile);
                if (control.IntegrityRevision !=
                        _expectedIntegrityRevision ||
                    control.CurrentConditionBasisPoints >= 10_000 ||
                    string.IsNullOrEmpty(control.LastDamageRecordId) ||
                    !string.IsNullOrEmpty(control.ActiveRepairOrderId) ||
                    world.LuoyangPassageRepairOrders.Any(item => item != null &&
                        string.Equals(item.Id, _repairOrderId,
                            StringComparison.Ordinal)) ||
                    !string.Equals(_repairOrderId,
                        LuoyangPassageOperationsContractIds.RepairOrderId(
                            _passage.FacilityId,
                            _expectedIntegrityRevision),
                        StringComparison.Ordinal) ||
                    !string.Equals(actualAuthority, _authorityBasisId,
                        StringComparison.Ordinal) ||
                    !string.Equals(facility.OwnerId,
                        control.ControllerOrganizationId,
                        StringComparison.Ordinal) ||
                    !string.Equals(container.OwnerOrganizationId,
                        control.ControllerOrganizationId,
                        StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(container.OwnerFamilyId) ||
                    !string.IsNullOrEmpty(container.CarrierPersonId) ||
                    !string.Equals(container.LocationId, facility.SettlementId,
                        StringComparison.Ordinal) ||
                    !IsEligibleRepairWorker(world, control, _managerPersonId) ||
                    !HasUnreservedMaterials(world, container.Id, requirements))
                    throw new InvalidOperationException(
                        "Luoyang passage repair-start preconditions drifted.");
                ReservePassage(validation, _passage.FacilityId, Id);
                validation.Reserve("inventory.container." + container.Id,
                    requirements.Values.Sum(),
                    requirements.Values.Sum(), Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var control = FindControl(world, _passage.FacilityId);
                var profile = RepairProfileFor(_passage.FacilityDefinitionId);
                var requirements = RepairRequirements(profile);
                var bridge = string.Equals(profile,
                    LuoyangPassageOperationsContractIds.BridgeRepairProfileId,
                    StringComparison.Ordinal);
                var project = new PropertyConstructionSystem()
                    .StartFacilityWork(world, _passage.FacilityId,
                        FacilityConstructionProjectKind.Repair,
                        _managerPersonId, _containerId, requirements,
                        bridge
                            ? LuoyangPassageOperationsContractIds
                                .BridgeRequiredLaborMinutes
                            : LuoyangPassageOperationsContractIds
                                .GateRequiredLaborMinutes,
                        bridge
                            ? LuoyangPassageOperationsContractIds
                                .BridgeMinimumDays
                            : LuoyangPassageOperationsContractIds.GateMinimumDays,
                        LuoyangPassageOperationsContractIds.RequiredMoney);
                var eventId = LuoyangPassageOperationsContractIds
                    .RepairStartEventId(_repairOrderId);
                world.LuoyangPassageRepairOrders.Add(
                    new LuoyangPassageRepairOrderState
                    {
                        Id = _repairOrderId,
                        FacilityId = _passage.FacilityId,
                        ProfileId = profile,
                        ControllerOrganizationId =
                            control.ControllerOrganizationId,
                        AuthorizingPersonId = _authorizingPersonId,
                        AuthorityBasisId = _authorityBasisId,
                        ManagerPersonId = _managerPersonId,
                        MaterialInventoryContainerId = _containerId,
                        FacilityConstructionProjectId = project.Id,
                        SourceDamageRecordId = control.LastDamageRecordId,
                        SourceIntegrityRevision = control.IntegrityRevision,
                        SourcePassageRevision = FindState(
                            world, _passage.FacilityId).Revision,
                        StartedDay = world.AbsoluteDay,
                        Status = LuoyangPassageRepairStatus.InProgress,
                        StartCommandId = _commandId,
                        StartEventId = eventId,
                        CompletionCommandId = string.Empty,
                        CompletionEventId = string.Empty
                    });
                control.ActiveRepairOrderId = _repairOrderId;
                events.Add(new WorldRuntimeEvent(eventId,
                    LuoyangPassageOperationsContractIds.RepairStartedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }

        private static void ReservePassage(
            WorldTransactionValidationContext validation,
            string facilityId,
            string transactionId)
        {
            validation.Reserve("luoyang.passage.transition." + facilityId,
                1, 1, transactionId);
        }

        private static void ApplyPassageTransition(
            WorldState world,
            WorldEventBuffer events,
            string facilityId,
            long expectedRevision,
            string targetStatusId,
            string reasonId,
            string commandId,
            string transactionId)
        {
            var current = FindState(world, facilityId);
            var nextRevision = checked(expectedRevision + 1);
            var eventId = LuoyangPassageTraversalWorldContractIds
                .TransitionEventId(facilityId, nextRevision);
            current.TraversalStatusId = targetStatusId;
            current.Revision = nextRevision;
            current.LastChangedDay = world.AbsoluteDay;
            current.LastChangedSegment = world.Segment;
            current.LastReasonId = reasonId;
            current.LastCommandId = commandId;
            current.LastEventId = eventId;
            events.Add(new WorldRuntimeEvent(eventId,
                LuoyangPassageTraversalWorldContractIds.TransitionedEventTypeId,
                transactionId, world.AbsoluteDay,
                (DaySegment)world.Segment));
        }

        private static LuoyangPassageOperationalControlState TryFindControl(
            WorldState world,
            string facilityId)
        {
            for (var i = 0;
                 i < world.LuoyangPassageOperationalControls.Count;
                 i++)
            {
                var control = world.LuoyangPassageOperationalControls[i];
                if (control != null && string.Equals(control.FacilityId,
                        facilityId, StringComparison.Ordinal))
                    return control;
            }
            return null;
        }

        private static LuoyangPassageOperationalControlState FindControl(
            WorldState world,
            string facilityId) => TryFindControl(world, facilityId) ??
                throw new InvalidOperationException(
                    "Luoyang passage has no operational guard control.");

        private static FacilityState FindFacility(
            WorldState world,
            string facilityId) => world.Facilities.Find(item => item != null &&
                string.Equals(item.Id, facilityId, StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage Facility is missing.");

        private static ArmyState FindArmy(WorldState world, string armyId) =>
            world.Armies.Find(item => item != null && string.Equals(item.Id,
                armyId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException(
                "Luoyang passage guard or attacking army is missing.");

        private static BattleRecordState FindBattle(
            WorldState world,
            string battleId) => world.Battles.Find(item => item != null &&
                string.Equals(item.Id, battleId, StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage damage battle is missing.");

        private static LuoyangPassageRepairOrderState FindRepairOrder(
            WorldState world,
            string orderId) => world.LuoyangPassageRepairOrders.Find(item =>
                item != null && string.Equals(item.Id, orderId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage repair order is missing.");

        private static FacilityConstructionProjectState
            FindConstructionProject(WorldState world, string projectId) =>
                world.FacilityConstructionProjects.Find(item => item != null &&
                    string.Equals(item.Id, projectId,
                        StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage Facility repair project is missing.");

        private static InventoryContainerState FindInventoryContainer(
            WorldState world,
            string containerId) => world.InventoryContainers.Find(item =>
                item != null && string.Equals(item.Id, containerId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Luoyang passage repair inventory container is missing.");

        private static string ResolveControllerOrganizationId(
            WorldState world,
            FacilityState facility)
        {
            if (!string.IsNullOrWhiteSpace(facility.ControllerId) &&
                world.Organizations.Exists(item => item != null &&
                    string.Equals(item.Id, facility.ControllerId,
                        StringComparison.Ordinal)))
                return facility.ControllerId;
            if (!string.IsNullOrWhiteSpace(facility.OwnerId) &&
                world.Organizations.Exists(item => item != null &&
                    string.Equals(item.Id, facility.OwnerId,
                        StringComparison.Ordinal)))
                return facility.OwnerId;
            throw new InvalidOperationException(
                "Passage control must resolve to an existing organization.");
        }

        private static string ResolveGuardAssignmentAuthority(
            WorldState world,
            FacilityState facility,
            ArmyState army,
            string authorizingPersonId,
            string controllerOrganizationId)
        {
            if (!string.Equals(army.OrganizationId,
                    controllerOrganizationId, StringComparison.Ordinal) ||
                !string.Equals(army.LocationId, facility.SettlementId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Guard army must belong to the controller and be co-located.");
            var organization = world.Organizations.Find(item => item != null &&
                string.Equals(item.Id, controllerOrganizationId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Passage controller organization is missing.");
            RequireLivingPerson(world, authorizingPersonId);
            if (string.Equals(organization.LeaderPersonId,
                    authorizingPersonId, StringComparison.Ordinal))
                return LuoyangPassageOperationsContractIds
                    .OrganizationLeaderAuthorityId;
            if (string.Equals(army.CommanderPersonId,
                    authorizingPersonId, StringComparison.Ordinal) &&
                new MilitaryAuthoritySystem().GetAuthority(world,
                    new StableId(authorizingPersonId), new StableId(army.Id)) >=
                MilitaryAuthorityLevel.Army)
                return LuoyangPassageOperationsContractIds
                    .GuardArmyCommanderAuthorityId;
            throw new InvalidOperationException(
                "Guard assignment requires controller-leader or army authority.");
        }

        private static string ResolveOperationalAuthority(
            WorldState world,
            LuoyangPassageOperationalControlState control,
            string authorizingPersonId)
        {
            var organization = world.Organizations.Find(item => item != null &&
                string.Equals(item.Id, control.ControllerOrganizationId,
                    StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    "Passage controller organization is missing.");
            RequireLivingPerson(world, authorizingPersonId);
            if (string.Equals(organization.LeaderPersonId,
                    authorizingPersonId, StringComparison.Ordinal))
                return LuoyangPassageOperationsContractIds
                    .OrganizationLeaderAuthorityId;
            var army = FindArmy(world, control.GuardArmyId);
            if (string.Equals(army.CommanderPersonId,
                    authorizingPersonId, StringComparison.Ordinal) &&
                new MilitaryAuthoritySystem().GetAuthority(world,
                    new StableId(authorizingPersonId), new StableId(army.Id)) >=
                MilitaryAuthorityLevel.Army)
                return LuoyangPassageOperationsContractIds
                    .GuardArmyCommanderAuthorityId;
            throw new InvalidOperationException(
                "Passage operation requires current controller or guard command.");
        }

        private static void ResolveAttackingAuthority(
            WorldState world,
            ArmyState attacker,
            string attackingCommanderPersonId)
        {
            RequireLivingPerson(world, attackingCommanderPersonId);
            if (!string.Equals(attacker.CommanderPersonId,
                    attackingCommanderPersonId, StringComparison.Ordinal) ||
                new MilitaryAuthoritySystem().GetAuthority(world,
                    new StableId(attackingCommanderPersonId),
                    new StableId(attacker.Id)) < MilitaryAuthorityLevel.Army)
                throw new InvalidOperationException(
                    "Passage damage must be confirmed by the attacking army commander.");
        }

        private static void RequireLivingPerson(
            WorldState world,
            string personId)
        {
            if (!world.People.Exists(item => item != null &&
                    string.Equals(item.Id, personId,
                        StringComparison.Ordinal) && item.IsAlive))
                throw new InvalidOperationException(
                    "Passage authority requires a living permanent Person.");
        }

        private static List<string> CollectGuardPeople(
            WorldState world,
            ArmyState army,
            string locationId)
        {
            var result = new List<string>();
            if (world.MilitaryServiceInitialized)
            {
                foreach (var service in world.MilitaryServices)
                {
                    if (service != null &&
                        string.Equals(service.ArmyId, army.Id,
                            StringComparison.Ordinal) &&
                        (service.Status == MilitaryServiceStatus.Active ||
                         service.Status == MilitaryServiceStatus.Mustering) &&
                        world.People.Exists(person => person != null &&
                            string.Equals(person.Id, service.PersonId,
                                StringComparison.Ordinal) && person.IsAlive &&
                            string.Equals(person.LocationId, locationId,
                                StringComparison.Ordinal)))
                        result.Add(service.PersonId);
                }
            }
            else if (world.People.Exists(person => person != null &&
                         string.Equals(person.Id, army.CommanderPersonId,
                             StringComparison.Ordinal) && person.IsAlive &&
                         string.Equals(person.LocationId, locationId,
                             StringComparison.Ordinal)))
                result.Add(army.CommanderPersonId);
            result.Sort(StringComparer.Ordinal);
            for (var i = result.Count - 1; i > 0; i--)
            {
                if (string.Equals(result[i], result[i - 1],
                        StringComparison.Ordinal))
                    result.RemoveAt(i);
            }
            if (!result.Contains(army.CommanderPersonId,
                    StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "Guard army commander must be an active co-located Person.");
            return result;
        }

        private static bool IsEligibleRepairWorker(
            WorldState world,
            LuoyangPassageOperationalControlState control,
            string personId)
        {
            var facility = FindFacility(world, control.FacilityId);
            var person = world.People.Find(item => item != null &&
                string.Equals(item.Id, personId, StringComparison.Ordinal));
            if (person == null || !person.IsAlive ||
                !string.Equals(person.LocationId, facility.SettlementId,
                    StringComparison.Ordinal) ||
                world.Journeys.Exists(item => item != null &&
                    string.Equals(item.PersonId, personId,
                        StringComparison.Ordinal)))
                return false;
            var organization = world.Organizations.Find(item => item != null &&
                string.Equals(item.Id, control.ControllerOrganizationId,
                    StringComparison.Ordinal));
            if (organization != null && string.Equals(
                    organization.LeaderPersonId, personId,
                    StringComparison.Ordinal) ||
                world.Memberships.Exists(item => item != null &&
                    string.Equals(item.PersonId, personId,
                        StringComparison.Ordinal) &&
                    string.Equals(item.OrganizationId,
                        control.ControllerOrganizationId,
                        StringComparison.Ordinal)))
                return true;
            return world.MilitaryServices.Exists(service => service != null &&
                string.Equals(service.PersonId, personId,
                    StringComparison.Ordinal) &&
                string.Equals(service.ArmyId, control.GuardArmyId,
                    StringComparison.Ordinal) &&
                (service.Status == MilitaryServiceStatus.Active ||
                 service.Status == MilitaryServiceStatus.Mustering));
        }

        private static void ValidateInitialCondition(
            string traversalStatusId,
            int condition)
        {
            var valid = condition == 10_000 &&
                    (string.Equals(traversalStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                         StringComparison.Ordinal) ||
                     string.Equals(traversalStatusId,
                         LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                         StringComparison.Ordinal)) ||
                condition > 0 && condition < 10_000 &&
                    string.Equals(traversalStatusId,
                        LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                        StringComparison.Ordinal) ||
                condition == 0 && string.Equals(traversalStatusId,
                    LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                    StringComparison.Ordinal);
            if (!valid)
                throw new InvalidOperationException(
                    "Explicit passage integrity assessment does not match status.");
        }

        private static string RepairProfileFor(string facilityDefinitionId) =>
            string.Equals(facilityDefinitionId, "facility.public.bridge",
                StringComparison.Ordinal)
                ? LuoyangPassageOperationsContractIds.BridgeRepairProfileId
                : LuoyangPassageOperationsContractIds.GateRepairProfileId;

        private static Dictionary<string, long> RepairRequirements(
            string profileId) => new Dictionary<string, long>(
                StringComparer.Ordinal)
            {
                {
                    CoreProductionContent.TimberMaterialProductId,
                    string.Equals(profileId,
                        LuoyangPassageOperationsContractIds.BridgeRepairProfileId,
                        StringComparison.Ordinal)
                        ? LuoyangPassageOperationsContractIds
                            .BridgeRequiredTimberUnits
                        : LuoyangPassageOperationsContractIds
                            .GateRequiredTimberUnits
                },
                {
                    CoreProductionContent.IronMaterialProductId,
                    LuoyangPassageOperationsContractIds.GateRequiredIronUnits
                }
            };

        private static bool HasUnreservedMaterials(
            WorldState world,
            string containerId,
            IReadOnlyDictionary<string, long> requirements)
        {
            foreach (var requirement in requirements)
            {
                long available = 0;
                foreach (var batch in world.ProductBatches)
                {
                    if (batch != null && string.Equals(
                            batch.InventoryContainerId, containerId,
                            StringComparison.Ordinal) && string.Equals(
                            batch.ProductDefinitionId, requirement.Key,
                            StringComparison.Ordinal))
                        available = checked(available + batch.Quantity -
                            batch.ReservedQuantity);
                }
                if (available < requirement.Value) return false;
            }
            return true;
        }

        private sealed class ProjectionHandler : IWorldRuntimeEventHandler
        {
            public ProjectionHandler(string handlerId, string eventTypeId)
            {
                HandlerId = handlerId;
                EventTypeId = eventTypeId;
            }

            public string HandlerId { get; }

            public string EventTypeId { get; }

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // WorldState already owns the committed fact. Presentation
                // rebuilds its read-only session projection after dispatch.
            }
        }

        private sealed class PassageDefinition
        {
            public PassageDefinition(
                string facilityId,
                string facilityDefinitionId)
            {
                FacilityId = new StableId(facilityId).Value;
                FacilityDefinitionId =
                    new StableId(facilityDefinitionId).Value;
            }

            public string FacilityId { get; }

            public string FacilityDefinitionId { get; }
        }
    }

    public sealed class LuoyangFormalPlayerMovementSystem
    {
        private const int CommandPriority = 18;
        private const string PersonArgumentId = "person_id";
        private const string InitialFacilityArgumentId = "initial_facility_id";
        private const string OriginFacilityArgumentId = "origin_facility_id";
        private const string TargetFacilityArgumentId = "target_facility_id";
        private const string MovementArgumentId = "movement_id";
        private const string SegmentIndexArgumentId = "segment_index";
        private const string EdgeArgumentId = "edge_id";
        private const string ExpectedRevisionArgumentId = "expected_revision";
        private const string TargetStatusArgumentId = "target_status_id";
        private const string ReasonArgumentId = "reason_id";
        private const string LocalMapVersionArgumentId =
            "local_map_version_id";
        private const string LocalMapHashArgumentId = "local_map_hash";

        private readonly LuoyangRoadTraversalRefinementPlan _plan;
        private readonly LuoyangMovementCostCalculator _costCalculator;
        private readonly Dictionary<string, LuoyangRoadNavigationEdge>
            _edgesById;
        private readonly Dictionary<string, LuoyangRoadNavigationNode>
            _nodesByFacilityId;
        private readonly HashSet<string> _passageFacilityIds;
        private readonly LuoyangHumanScaleLocalMapPlan _localMap;
        private readonly LuoyangHumanScaleLocalRoutePlanner
            _localRoutePlanner;

        public LuoyangFormalPlayerMovementSystem(
            LuoyangRoadTraversalRefinementPlan plan,
            LuoyangMovementCostCalculator costCalculator = null,
            LuoyangHumanScaleLocalMapPlan localMap = null)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            LuoyangRoadConnectorPassageTraversalRules.Validate(plan);
            _costCalculator = costCalculator ??
                new LuoyangMovementCostCalculator();
            _edgesById = plan.NavigationEdges.ToDictionary(item =>
                item.EdgeId, StringComparer.Ordinal);
            _nodesByFacilityId = plan.NavigationNodes.ToDictionary(item =>
                item.FacilityId, StringComparer.Ordinal);
            _passageFacilityIds = new HashSet<string>(
                plan.PassageFacilityIds, StringComparer.Ordinal);
            _localMap = localMap;
            _localRoutePlanner = localMap == null
                ? null
                : new LuoyangHumanScaleLocalRoutePlanner(localMap);
        }

        public void RegisterHandlers(WorldCommandRuntime runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            Register(runtime, LuoyangFormalPlayerMovementIds
                .InitializeCommandTypeId, new InitializeHandler(this));
            Register(runtime, LuoyangFormalPlayerMovementIds.MoveCommandTypeId,
                new MoveHandler(this));
            Register(runtime, LuoyangFormalPlayerMovementIds
                .AdvanceSegmentCommandTypeId, new AdvanceHandler(this));
            Register(runtime, LuoyangFormalPlayerMovementIds
                .RoadTransitionCommandTypeId, new RoadTransitionHandler(this));
            Register(runtime, LuoyangFormalPlayerMovementIds
                .UpgradeLocalLocationsCommandTypeId,
                new UpgradeLocalLocationsHandler(this));
        }

        public bool UsesHumanScaleLocalMap => _localMap != null;

        public bool EnsureInitialized(WorldState world,
            WorldCommandRuntime runtime, string initialFacilityId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            RegisterHandlers(runtime);
            var session = new PlayerSession(world);
            var person = session.ControlledPerson;
            if (world.LuoyangLocalNavigationLocations.Count != 0 ||
                world.LuoyangRoadOperationalSegments.Count != 0)
            {
                if (_localMap != null &&
                    HasStrategicLocationInitialization(world))
                {
                    var upgradeCommandId = "luoyang.player-movement." +
                        "upgrade-local-locations." + _localMap.MapVersionId;
                    runtime.Enqueue(world, new WorldCommandEnvelope(
                        upgradeCommandId,
                        LuoyangFormalPlayerMovementIds
                            .UpgradeLocalLocationsCommandTypeId,
                        LuoyangFormalPlayerMovementIds.SystemIssuerId,
                        world.AbsoluteDay, (DaySegment)world.Segment,
                        CommandPriority,
                        new Dictionary<string, string>(
                            StringComparer.Ordinal)
                        {
                            [LocalMapVersionArgumentId] =
                                _localMap.MapVersionId,
                            [LocalMapHashArgumentId] = _localMap.AssetHash
                        }));
                    return true;
                }
                ValidateInitializedPlan(world);
                return false;
            }
            if (_localMap == null &&
                    !_nodesByFacilityId.ContainsKey(initialFacilityId) ||
                _localMap != null &&
                    !_localMap.NavigationNodesByFacilityId.ContainsKey(
                        initialFacilityId))
                throw new InvalidOperationException(
                    "The initial player Facility is not a navigation node.");
            var commandId = "luoyang.player-movement.initialize." + person.Id;
            runtime.Enqueue(world, new WorldCommandEnvelope(commandId,
                LuoyangFormalPlayerMovementIds.InitializeCommandTypeId,
                LuoyangFormalPlayerMovementIds.SystemIssuerId,
                world.AbsoluteDay, (DaySegment)world.Segment, CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [PersonArgumentId] = person.Id,
                    [InitialFacilityArgumentId] = initialFacilityId
                }));
            return true;
        }

        private bool HasStrategicLocationInitialization(WorldState world)
        {
            if (world.LuoyangLocalNavigationLocations.Count !=
                    _plan.NavigationNodes.Count ||
                world.LuoyangRoadOperationalSegments.Count !=
                    _plan.NavigationEdges.Count)
                return false;
            var locations = world.LuoyangLocalNavigationLocations.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            return _plan.NavigationNodes.All(item =>
                locations.TryGetValue(item.FacilityId, out var state) &&
                state.CellId64 == item.CellId64 &&
                state.FacilityDefinitionId == item.FacilityDefinitionId);
        }

        public bool TryCreatePlan(WorldState world, string targetFacilityId,
            out LuoyangPedestrianWalkPlan plan, out string failureReasonId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.Validate();
            ValidateInitializedPlan(world);
            var session = new PlayerSession(world);
            var person = session.ControlledPerson;
            if (!session.CanAct)
            {
                plan = null;
                failureReasonId = "movement.rejection.person-cannot-act.v1";
                return false;
            }
            if (world.Journeys.Any(item => item != null &&
                    item.PersonId == person.Id))
            {
                plan = null;
                failureReasonId = "movement.rejection.person-traveling.v1";
                return false;
            }
            var passageSession = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(_plan, world);
            var roads = world.LuoyangRoadOperationalSegments.ToDictionary(
                item => item.EdgeId, StringComparer.Ordinal);
            plan = LuoyangClickToWalkPedestrianRules.CreatePlan(_plan,
                passageSession, person.Id, person.CurrentFacilityId,
                targetFacilityId, edgeId => roads.TryGetValue(edgeId,
                    out var road) && road.CanTraverse);
            if (!plan.CanWalk || plan.Segments.Count == 0)
            {
                failureReasonId = plan.FailureReasonId.Length == 0
                    ? "movement.rejection.same-location.v1"
                    : plan.FailureReasonId;
                return false;
            }
            var snapshot = BuildMovementState(world, person, plan,
                "movement.preview");
            if (person.StaminaBasisPoints <
                snapshot.ExpectedStaminaCostBasisPoints)
            {
                failureReasonId = LuoyangFormalPlayerMovementIds
                    .InsufficientStaminaReasonId;
                return false;
            }
            if (person.Provisions < snapshot.ExpectedFoodCost)
            {
                failureReasonId = LuoyangFormalPlayerMovementIds
                    .InsufficientFoodReasonId;
                return false;
            }
            failureReasonId = string.Empty;
            return true;
        }

        public bool TryCreateLocalPlan(WorldState world,
            string targetFacilityId,
            out LuoyangHumanScaleLocalRoute plan,
            out string failureReasonId)
        {
            if (_localRoutePlanner == null)
                throw new InvalidOperationException(
                    "This movement system has no human-scale map.");
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.Validate();
            ValidateInitializedPlan(world);
            var session = new PlayerSession(world);
            var person = session.ControlledPerson;
            if (!session.CanAct)
            {
                plan = null;
                failureReasonId = "movement.rejection.person-cannot-act.v1";
                return false;
            }
            if (world.Journeys.Any(item => item != null &&
                    item.PersonId == person.Id))
            {
                plan = null;
                failureReasonId = "movement.rejection.person-traveling.v1";
                return false;
            }
            if (!_localRoutePlanner.TryFindRoute(world,
                    person.CurrentFacilityId, targetFacilityId,
                    out plan, out failureReasonId))
                return false;
            var snapshot = BuildLocalMovementState(world, person, plan,
                "movement.preview");
            if (person.StaminaBasisPoints <
                snapshot.ExpectedStaminaCostBasisPoints)
            {
                failureReasonId = LuoyangFormalPlayerMovementIds
                    .InsufficientStaminaReasonId;
                plan = null;
                return false;
            }
            if (person.Provisions < snapshot.ExpectedFoodCost)
            {
                failureReasonId = LuoyangFormalPlayerMovementIds
                    .InsufficientFoodReasonId;
                plan = null;
                return false;
            }
            failureReasonId = string.Empty;
            return true;
        }

        public string EnqueueMove(WorldState world, WorldCommandRuntime runtime,
            string targetFacilityId, string issuerId = null)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            RegisterHandlers(runtime);
            var person = new PlayerSession(world).ControlledPerson;
            var sequence = world.PersistentWorldCommands.Count(item =>
                item != null && item.CommandTypeId ==
                LuoyangFormalPlayerMovementIds.MoveCommandTypeId);
            var commandId = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.player-movement.request.{0}.{1:D8}", person.Id,
                sequence);
            runtime.Enqueue(world, new WorldCommandEnvelope(commandId,
                LuoyangFormalPlayerMovementIds.MoveCommandTypeId,
                issuerId ?? LuoyangFormalPlayerMovementIds
                    .PresentationIssuerId, world.AbsoluteDay,
                (DaySegment)world.Segment, CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [PersonArgumentId] = person.Id,
                    [OriginFacilityArgumentId] = person.CurrentFacilityId,
                    [TargetFacilityArgumentId] = targetFacilityId
                }));
            return commandId;
        }

        public string EnqueueAdvanceSegment(WorldState world,
            WorldCommandRuntime runtime, LuoyangFormalPlayerMovementState movement)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (movement == null) throw new ArgumentNullException(nameof(movement));
            RegisterHandlers(runtime);
            var commandId = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.player-movement.advance.{0}.{1:D4}", movement.Id,
                movement.CurrentSegmentIndex);
            runtime.Enqueue(world, new WorldCommandEnvelope(commandId,
                LuoyangFormalPlayerMovementIds.AdvanceSegmentCommandTypeId,
                LuoyangFormalPlayerMovementIds.SystemIssuerId,
                world.AbsoluteDay, (DaySegment)world.Segment, CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [MovementArgumentId] = movement.Id,
                    [SegmentIndexArgumentId] = movement.CurrentSegmentIndex
                        .ToString(System.Globalization.CultureInfo.InvariantCulture)
                }));
            return commandId;
        }

        public string EnqueueRoadTransition(WorldState world,
            WorldCommandRuntime runtime, string edgeId, string targetStatusId,
            string reasonId)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (!LuoyangFormalPlayerMovementRules.IsRoadStatus(targetStatusId))
                throw new ArgumentException("Unknown road status.",
                    nameof(targetStatusId));
            var road = world.LuoyangRoadOperationalSegments.Single(item =>
                item.EdgeId == edgeId);
            var commandId = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "luoyang.road-transition.{0}.{1:D8}", edgeId,
                checked(road.Revision + 1));
            RegisterHandlers(runtime);
            runtime.Enqueue(world, new WorldCommandEnvelope(commandId,
                LuoyangFormalPlayerMovementIds.RoadTransitionCommandTypeId,
                LuoyangFormalPlayerMovementIds.SystemIssuerId,
                world.AbsoluteDay, (DaySegment)world.Segment, CommandPriority,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [EdgeArgumentId] = edgeId,
                    [ExpectedRevisionArgumentId] = road.Revision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    [TargetStatusArgumentId] = targetStatusId,
                    [ReasonArgumentId] = new StableId(reasonId).Value
                }));
            return commandId;
        }

        public bool CanTraverseNextSegment(WorldState world,
            LuoyangFormalPlayerMovementState movement)
        {
            if (movement == null ||
                movement.Status != LuoyangFormalMovementStatus.Active ||
                movement.CurrentSegmentIndex >= movement.Segments.Count)
                return false;
            return CanTraverse(world,
                movement.Segments[movement.CurrentSegmentIndex]);
        }

        public void ValidateInitializedPlan(WorldState world)
        {
            var expectedLocationCount = _localMap == null
                ? _plan.NavigationNodes.Count
                : _localMap.FacilityCapabilities.Count;
            if (world.LuoyangLocalNavigationLocations.Count !=
                    expectedLocationCount ||
                world.LuoyangRoadOperationalSegments.Count !=
                    _plan.NavigationEdges.Count)
                throw new InvalidOperationException(
                    "The persisted Luoyang movement graph is incomplete.");
            var local = world.LuoyangLocalNavigationLocations.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            if (_localMap == null)
            {
                foreach (var node in _plan.NavigationNodes)
                    if (!local.TryGetValue(node.FacilityId,
                            out var persisted) ||
                        persisted.CellId64 != node.CellId64 ||
                        persisted.FacilityDefinitionId !=
                            node.FacilityDefinitionId)
                        throw new InvalidOperationException(
                            "The persisted Luoyang local-location graph drifted.");
            }
            else
            {
                foreach (var capability in _localMap.FacilityCapabilities)
                    if (!local.TryGetValue(capability.FacilityId,
                            out var persisted) ||
                        persisted.CellId64 != capability.CellId64 ||
                        persisted.FacilityDefinitionId !=
                            capability.FacilityDefinitionId)
                        throw new InvalidOperationException(
                            "The persisted human-scale location graph drifted.");
            }
            var roads = world.LuoyangRoadOperationalSegments.ToDictionary(
                item => item.EdgeId, StringComparer.Ordinal);
            foreach (var edge in _plan.NavigationEdges)
                if (!roads.TryGetValue(edge.EdgeId, out var persisted) ||
                    !_nodesByFacilityId.TryGetValue(
                        Node(edge.FromNodeId).FacilityId, out _) ||
                    persisted.FromFacilityId != Node(edge.FromNodeId).FacilityId ||
                    persisted.ToFacilityId != Node(edge.ToNodeId).FacilityId)
                    throw new InvalidOperationException(
                        "The persisted Luoyang road graph drifted.");
        }

        private LuoyangFormalPlayerMovementState BuildMovementState(
            WorldState world, PersonState person,
            LuoyangPedestrianWalkPlan plan, string commandId)
        {
            var movement = new LuoyangFormalPlayerMovementState
            {
                Id = "luoyang.player-movement." + person.Id + "." +
                     world.LuoyangFormalPlayerMovements.Count.ToString("D8",
                         System.Globalization.CultureInfo.InvariantCulture),
                RequestCommandId = commandId,
                PersonId = person.Id,
                PolicyId = _costCalculator.Policy.Id,
                MovementModeId = LuoyangFormalPlayerMovementIds
                    .FootMovementModeId,
                OriginSettlementLocationId = person.LocationId,
                OriginFacilityId = plan.StartFacilityId,
                OriginCellId64 = _nodesByFacilityId[plan.StartFacilityId]
                    .CellId64,
                TargetFacilityId = plan.TargetFacilityId,
                TargetCellId64 = _nodesByFacilityId[plan.TargetFacilityId]
                    .CellId64,
                IssuedDay = world.AbsoluteDay,
                IssuedSegment = world.Segment,
                Status = LuoyangFormalMovementStatus.Active,
                FailureReasonId = string.Empty,
                LastProgressCommandId = string.Empty,
                StartedEventId = string.Empty,
                CompletionEventId = string.Empty
            };
            var elapsed = 0;
            var chargedFood = 0;
            foreach (var source in plan.Segments)
            {
                var cost = _costCalculator.CalculateSegment(
                    source.DistanceMetres, source.WeightedDistanceMetres);
                elapsed = checked(elapsed + cost.DurationMinutes);
                var totalFood = _costCalculator.CalculateFoodCost(elapsed);
                var passageId = _passageFacilityIds.Contains(
                        source.FromFacilityId)
                    ? source.FromFacilityId
                    : _passageFacilityIds.Contains(source.ToFacilityId)
                        ? source.ToFacilityId
                        : string.Empty;
                movement.Segments.Add(
                    new LuoyangFormalMovementSegmentState
                    {
                        Sequence = source.Sequence,
                        EdgeId = source.EdgeId,
                        FromFacilityId = source.FromFacilityId,
                        ToFacilityId = source.ToFacilityId,
                        PassageFacilityId = passageId,
                        DistanceMetres = cost.DistanceMetres,
                        WeightedDistanceMetres = cost
                            .WeightedDistanceMetres,
                        DurationMinutes = cost.DurationMinutes,
                        StaminaCostBasisPoints = cost
                            .StaminaCostBasisPoints,
                        FoodCost = totalFood - chargedFood
                    });
                chargedFood = totalFood;
            }
            movement.ExpectedDurationMinutes = elapsed;
            movement.ExpectedWorldSegments = _costCalculator
                .CalculateWorldSegments(elapsed);
            movement.ExpectedStaminaCostBasisPoints = movement.Segments.Sum(
                item => item.StaminaCostBasisPoints);
            movement.ExpectedFoodCost = movement.Segments.Sum(item =>
                item.FoodCost);
            return movement;
        }

        private LuoyangFormalPlayerMovementState BuildLocalMovementState(
            WorldState world, PersonState person,
            LuoyangHumanScaleLocalRoute plan, string commandId)
        {
            var totalCost = _costCalculator.CalculateSegment(
                plan.DistanceCentimetres / 100d,
                plan.WeightedDistanceCentimetres / 100d);
            var movement = new LuoyangFormalPlayerMovementState
            {
                Id = "luoyang.player-movement." + person.Id + "." +
                     world.LuoyangFormalPlayerMovements.Count.ToString("D8",
                         System.Globalization.CultureInfo.InvariantCulture),
                RequestCommandId = commandId,
                PersonId = person.Id,
                PolicyId = _costCalculator.Policy.Id,
                MovementModeId = LuoyangFormalPlayerMovementIds
                    .FootMovementModeId,
                OriginSettlementLocationId = person.LocationId,
                OriginFacilityId = plan.StartFacilityId,
                OriginCellId64 = _localMap.NavigationNodesByFacilityId[
                    plan.StartFacilityId].CellId64,
                TargetFacilityId = plan.TargetFacilityId,
                TargetCellId64 = _localMap.NavigationNodesByFacilityId[
                    plan.TargetFacilityId].CellId64,
                IssuedDay = world.AbsoluteDay,
                IssuedSegment = world.Segment,
                Status = LuoyangFormalMovementStatus.Active,
                FailureReasonId = string.Empty,
                LastProgressCommandId = string.Empty,
                StartedEventId = string.Empty,
                CompletionEventId = string.Empty,
                IsLocalHumanScale = true,
                LocalMapVersionId = _localMap.MapVersionId,
                LocalMapAssetHash = _localMap.AssetHash,
                ExpectedDurationMinutes = totalCost.DurationMinutes,
                ExpectedWorldSegments = _costCalculator
                    .CalculateWorldSegments(totalCost.DurationMinutes),
                ExpectedStaminaCostBasisPoints = totalCost
                    .StaminaCostBasisPoints,
                ExpectedFoodCost = _costCalculator.CalculateFoodCost(
                    totalCost.DurationMinutes)
            };
            long accumulatedWeight = 0;
            var allocatedDuration = 0;
            var allocatedStamina = 0;
            var allocatedFood = 0;
            for (var index = 0; index < plan.Edges.Count; index++)
            {
                var edge = plan.Edges[index];
                var fromPoint = edge.Geometry.First();
                var toPoint = edge.Geometry.Last();
                accumulatedWeight = checked(accumulatedWeight +
                    (long)edge.DistanceCentimetres *
                    edge.TraversalCostPermille / 1000L);
                var durationAtEnd = Allocate(totalCost.DurationMinutes,
                    accumulatedWeight, plan.WeightedDistanceCentimetres);
                var staminaAtEnd = Allocate(
                    totalCost.StaminaCostBasisPoints, accumulatedWeight,
                    plan.WeightedDistanceCentimetres);
                var foodAtEnd = _costCalculator.CalculateFoodCost(
                    durationAtEnd);
                movement.Segments.Add(
                    new LuoyangFormalMovementSegmentState
                    {
                        Sequence = index,
                        EdgeId = edge.Id,
                        FromFacilityId = index == 0
                            ? plan.StartFacilityId : string.Empty,
                        ToFacilityId = index + 1 == plan.Edges.Count
                            ? plan.TargetFacilityId : string.Empty,
                        PassageFacilityId = edge.PassageFacilityId,
                        DistanceMetres = Math.Max(1,
                            (edge.DistanceCentimetres + 99) / 100),
                        WeightedDistanceMetres = Math.Max(1, checked((int)
                            Math.Ceiling((double)edge.DistanceCentimetres *
                                edge.TraversalCostPermille / 100_000d))),
                        DurationMinutes = durationAtEnd - allocatedDuration,
                        StaminaCostBasisPoints = staminaAtEnd -
                            allocatedStamina,
                        FoodCost = foodAtEnd - allocatedFood,
                        IsLocalHumanScale = true,
                        FormalWorldObjectId = edge.FormalWorldObjectId,
                        TraversalConditionId = edge.TraversalConditionId,
                        FromLocalNodeId = plan.NodeIds[index],
                        ToLocalNodeId = plan.NodeIds[index + 1],
                        FromLocalSpaceId = fromPoint.LocalSpaceId,
                        ToLocalSpaceId = toPoint.LocalSpaceId,
                        FromCellId64 = fromPoint.CellId64,
                        ToCellId64 = toPoint.CellId64,
                        FromEastCentimetres = Centimetres(
                            fromPoint.LocalEastMetres),
                        FromNorthCentimetres = Centimetres(
                            fromPoint.LocalNorthMetres),
                        FromElevationCentimetres = Centimetres(
                            fromPoint.ElevationMetres),
                        ToEastCentimetres = Centimetres(
                            toPoint.LocalEastMetres),
                        ToNorthCentimetres = Centimetres(
                            toPoint.LocalNorthMetres),
                        ToElevationCentimetres = Centimetres(
                            toPoint.ElevationMetres)
                    });
                allocatedDuration = durationAtEnd;
                allocatedStamina = staminaAtEnd;
                allocatedFood = foodAtEnd;
            }
            return movement;
        }

        private static int Allocate(int total, long accumulated,
            long denominator) => denominator <= 0 ? total : checked((int)
            Math.Min(total, (total * accumulated + denominator - 1L) /
                denominator));

        private static int Centimetres(double metres) => checked(
            (int)Math.Round(metres * 100d,
                MidpointRounding.AwayFromZero));

        private bool CanTraverse(WorldState world,
            LuoyangFormalMovementSegmentState segment)
        {
            if (segment.IsLocalHumanScale)
            {
                if (segment.TraversalConditionId.StartsWith(
                        "cell-traversal.condition.",
                        StringComparison.Ordinal))
                    return LuoyangCellTraversalRules.CanTraverseSegment(world,
                        new CellRouteSegment(segment.EdgeId,
                            segment.FromCellId64, segment.ToCellId64,
                            segment.TraversalConditionId,
                            segment.FormalWorldObjectId));
                if (_localMap == null || !_localMap.EdgesById.TryGetValue(
                        segment.EdgeId, out var localEdge) ||
                    !string.Equals(localEdge.TraversalConditionId,
                        segment.TraversalConditionId,
                        StringComparison.Ordinal) ||
                    !string.Equals(localEdge.FormalWorldObjectId,
                        segment.FormalWorldObjectId,
                        StringComparison.Ordinal))
                    return false;
                return LuoyangHumanScaleWorldTraversalRules
                    .CanTraverseLocalEdge(world, localEdge);
            }
            var road = world.LuoyangRoadOperationalSegments.FirstOrDefault(
                item => item.EdgeId == segment.EdgeId);
            if (road == null || !road.CanTraverse) return false;
            if (string.IsNullOrEmpty(segment.PassageFacilityId)) return true;
            var passage = world.LuoyangPassageTraversals.FirstOrDefault(item =>
                item.FacilityId == segment.PassageFacilityId);
            return passage != null && (passage.TraversalStatusId ==
                    LuoyangRoadConnectorPassageTraversalIds.OpenStatusId ||
                passage.TraversalStatusId ==
                    LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId);
        }

        private LuoyangRoadNavigationNode Node(string nodeId) =>
            _plan.NavigationNodes.Single(item => item.NodeId == nodeId);

        private static void Register(WorldCommandRuntime runtime,
            string commandTypeId, IWorldCommandHandler handler)
        {
            if (!runtime.HasHandler(commandTypeId))
                runtime.RegisterHandler(handler);
        }

        private static string Argument(WorldCommandEnvelope command,
            string key)
        {
            if (!command.Arguments.TryGetValue(key, out var value) ||
                string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Movement command '{command.Id}' lacks '{key}'.");
            return value;
        }

        private sealed class InitializeHandler : IWorldCommandHandler
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            public InitializeHandler(LuoyangFormalPlayerMovementSystem owner) =>
                _owner = owner;
            public string CommandTypeId => LuoyangFormalPlayerMovementIds
                .InitializeCommandTypeId;
            public void Plan(WorldCommandEnvelope command,
                WorldTransactionBuffer transactions) => transactions.Add(
                new InitializeTransaction(_owner, command));
        }

        private sealed class MoveHandler : IWorldCommandHandler
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            public MoveHandler(LuoyangFormalPlayerMovementSystem owner) =>
                _owner = owner;
            public string CommandTypeId => LuoyangFormalPlayerMovementIds
                .MoveCommandTypeId;
            public void Plan(WorldCommandEnvelope command,
                WorldTransactionBuffer transactions) => transactions.Add(
                new MoveTransaction(_owner, command));
        }

        private sealed class AdvanceHandler : IWorldCommandHandler
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            public AdvanceHandler(LuoyangFormalPlayerMovementSystem owner) =>
                _owner = owner;
            public string CommandTypeId => LuoyangFormalPlayerMovementIds
                .AdvanceSegmentCommandTypeId;
            public void Plan(WorldCommandEnvelope command,
                WorldTransactionBuffer transactions) => transactions.Add(
                new AdvanceTransaction(_owner, command));
        }

        private sealed class RoadTransitionHandler : IWorldCommandHandler
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            public RoadTransitionHandler(
                LuoyangFormalPlayerMovementSystem owner) => _owner = owner;
            public string CommandTypeId => LuoyangFormalPlayerMovementIds
                .RoadTransitionCommandTypeId;
            public void Plan(WorldCommandEnvelope command,
                WorldTransactionBuffer transactions) => transactions.Add(
                new RoadTransitionTransaction(_owner, command));
        }

        private sealed class UpgradeLocalLocationsHandler :
            IWorldCommandHandler
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            public UpgradeLocalLocationsHandler(
                LuoyangFormalPlayerMovementSystem owner) => _owner = owner;
            public string CommandTypeId => LuoyangFormalPlayerMovementIds
                .UpgradeLocalLocationsCommandTypeId;
            public void Plan(WorldCommandEnvelope command,
                WorldTransactionBuffer transactions) => transactions.Add(
                new UpgradeLocalLocationsTransaction(_owner, command));
        }

        private sealed class UpgradeLocalLocationsTransaction :
            IWorldTransaction
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            private readonly WorldCommandEnvelope _command;
            public UpgradeLocalLocationsTransaction(
                LuoyangFormalPlayerMovementSystem owner,
                WorldCommandEnvelope command)
            {
                _owner = owner;
                _command = command;
            }
            public string Id =>
                "luoyang.player-movement.transaction.upgrade-local-locations";
            public string KindId =>
                "mandate.transaction.person-movement.upgrade-local-locations.v1";
            public int Priority => CommandPriority;
            public void Validate(WorldState world,
                WorldTransactionValidationContext validation)
            {
                if (_owner._localMap == null ||
                    !_owner.HasStrategicLocationInitialization(world) ||
                    Argument(_command, LocalMapVersionArgumentId) !=
                        _owner._localMap.MapVersionId ||
                    Argument(_command, LocalMapHashArgumentId) !=
                        _owner._localMap.AssetHash ||
                    world.LuoyangFormalPlayerMovements.Any(item =>
                        item != null && item.Status ==
                        LuoyangFormalMovementStatus.Active))
                    throw new InvalidOperationException(
                        "The human-scale location upgrade is stale or unsafe.");
            }
            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var person = new PlayerSession(world).ControlledPerson;
                world.LuoyangLocalNavigationLocations.Clear();
                foreach (var capability in _owner._localMap
                             .FacilityCapabilities.OrderBy(
                                 item => item.FacilityId,
                                 StringComparer.Ordinal))
                {
                    var space = _owner._localMap.LocalSpacesByCellId[
                        capability.CellId64];
                    world.LuoyangLocalNavigationLocations.Add(
                        new LuoyangLocalNavigationLocationState
                        {
                            Id = "luoyang.local-location." +
                                 capability.FacilityId,
                            FacilityId = capability.FacilityId,
                            FacilityDefinitionId =
                                capability.FacilityDefinitionId,
                            SettlementLocationId = person.LocationId,
                            CellId64 = capability.CellId64,
                            GridColumn = space.GridColumn,
                            GridRow = space.GridRow
                        });
                }
                if (!string.IsNullOrEmpty(person.CurrentFacilityId) &&
                    _owner._localMap.NavigationNodesByFacilityId.TryGetValue(
                        person.CurrentFacilityId, out var node))
                {
                    person.CurrentCellId64 = node.CellId64;
                    person.CurrentLocalSpaceId = node.LocalSpaceId;
                    person.CurrentLocalAnchorId = node.Id;
                    person.CurrentLocalEastCentimetres = Centimetres(
                        node.LocalEastMetres);
                    person.CurrentLocalNorthCentimetres = Centimetres(
                        node.LocalNorthMetres);
                    person.CurrentLocalElevationCentimetres = Centimetres(
                        node.ElevationMetres);
                    person.LocationPrecisionId = _owner._localMap
                        .EntrancesByFacilityId.ContainsKey(
                            person.CurrentFacilityId)
                        ? LuoyangHumanScaleLocalMapIds
                            .FacilityEntranceLocationTypeId
                        : LuoyangHumanScaleLocalMapIds.GroundLocationTypeId;
                }
                events.Add(new WorldRuntimeEvent(
                    "luoyang.player-movement.event.upgraded-local-locations." +
                    _owner._localMap.MapVersionId,
                    "mandate.event.person-movement." +
                    "upgraded-local-locations.v1", Id,
                    world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }

        private sealed class InitializeTransaction : IWorldTransaction
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            private readonly WorldCommandEnvelope _command;
            public InitializeTransaction(
                LuoyangFormalPlayerMovementSystem owner,
                WorldCommandEnvelope command)
            {
                _owner = owner;
                _command = command;
            }
            public string Id => "luoyang.player-movement.transaction.initialize";
            public string KindId =>
                "mandate.transaction.luoyang-player-movement.initialize.v1";
            public int Priority => CommandPriority;
            public void Validate(WorldState world,
                WorldTransactionValidationContext validation)
            {
                if (world.LuoyangLocalNavigationLocations.Count != 0 ||
                    world.LuoyangRoadOperationalSegments.Count != 0)
                    throw new InvalidOperationException(
                        "The Luoyang movement graph is already initialized.");
                var personId = Argument(_command, PersonArgumentId);
                var session = new PlayerSession(world);
                var initialFacilityId = Argument(_command,
                    InitialFacilityArgumentId);
                var initialExists = _owner._localMap == null
                    ? _owner._nodesByFacilityId.ContainsKey(initialFacilityId)
                    : _owner._localMap.NavigationNodesByFacilityId.ContainsKey(
                        initialFacilityId);
                if (session.ControlledPersonId != personId || !initialExists)
                    throw new InvalidOperationException(
                        "The movement initialization Person or Facility is invalid.");
                if (world.LuoyangPassageTraversals.Count !=
                    LuoyangPassageTraversalWorldContractIds.PassageCount)
                    throw new InvalidOperationException(
                        "Formal movement requires persisted passage facts.");
            }
            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var person = new PlayerSession(world).ControlledPerson;
                var initialFacilityId = Argument(_command,
                    InitialFacilityArgumentId);
                if (_owner._localMap == null)
                {
                    foreach (var node in _owner._plan.NavigationNodes.OrderBy(
                                 item => item.FacilityId,
                                 StringComparer.Ordinal))
                        world.LuoyangLocalNavigationLocations.Add(
                            new LuoyangLocalNavigationLocationState
                            {
                                Id = "luoyang.local-location." +
                                     node.FacilityId,
                                FacilityId = node.FacilityId,
                                FacilityDefinitionId =
                                    node.FacilityDefinitionId,
                                SettlementLocationId = person.LocationId,
                                CellId64 = node.CellId64,
                                GridColumn = node.GridColumn,
                                GridRow = node.GridRow
                            });
                }
                else
                {
                    foreach (var capability in _owner._localMap
                                 .FacilityCapabilities.OrderBy(
                                     item => item.FacilityId,
                                     StringComparer.Ordinal))
                    {
                        var space = _owner._localMap.LocalSpacesByCellId[
                            capability.CellId64];
                        world.LuoyangLocalNavigationLocations.Add(
                            new LuoyangLocalNavigationLocationState
                            {
                                Id = "luoyang.local-location." +
                                     capability.FacilityId,
                                FacilityId = capability.FacilityId,
                                FacilityDefinitionId =
                                    capability.FacilityDefinitionId,
                                SettlementLocationId = person.LocationId,
                                CellId64 = capability.CellId64,
                                GridColumn = space.GridColumn,
                                GridRow = space.GridRow
                            });
                    }
                }
                foreach (var edge in _owner._plan.NavigationEdges.OrderBy(
                             item => item.EdgeId, StringComparer.Ordinal))
                    world.LuoyangRoadOperationalSegments.Add(
                        new LuoyangRoadOperationalSegmentState
                        {
                            Id = "luoyang.road-state." + edge.EdgeId,
                            EdgeId = edge.EdgeId,
                            FromFacilityId = _owner.Node(edge.FromNodeId)
                                .FacilityId,
                            ToFacilityId = _owner.Node(edge.ToNodeId)
                                .FacilityId,
                            StatusId = LuoyangFormalPlayerMovementIds
                                .OpenRoadStatusId,
                            Revision = 0,
                            LastChangedDay = world.AbsoluteDay,
                            LastChangedSegment = world.Segment,
                            LastReasonId = "road.reason.initialized.v1",
                            LastCommandId = _command.Id,
                            LastEventId = "luoyang.player-movement.event.initialized"
                        });
                if (_owner._localMap == null)
                {
                    var initial = _owner._nodesByFacilityId[
                        initialFacilityId];
                    person.CurrentFacilityId = initial.FacilityId;
                    person.CurrentCellId64 = initial.CellId64;
                    person.LocationPrecisionId =
                        LuoyangHumanScaleLocalMapIds.StrategicLocationTypeId;
                    person.CurrentLocalSpaceId = string.Empty;
                    person.CurrentLocalAnchorId = string.Empty;
                }
                else
                {
                    var entrance = _owner._localMap
                        .NavigationNodesByFacilityId[initialFacilityId];
                    person.CurrentFacilityId = initialFacilityId;
                    person.CurrentCellId64 = entrance.CellId64;
                    person.LocationPrecisionId = _owner._localMap
                        .EntrancesByFacilityId.ContainsKey(initialFacilityId)
                            ? LuoyangHumanScaleLocalMapIds
                                .FacilityEntranceLocationTypeId
                            : LuoyangHumanScaleLocalMapIds
                                .GroundLocationTypeId;
                    person.CurrentLocalSpaceId = entrance.LocalSpaceId;
                    person.CurrentLocalAnchorId = entrance.Id;
                    person.CurrentLocalEastCentimetres = Centimetres(
                        entrance.LocalEastMetres);
                    person.CurrentLocalNorthCentimetres = Centimetres(
                        entrance.LocalNorthMetres);
                    person.CurrentLocalElevationCentimetres = Centimetres(
                        entrance.ElevationMetres);
                }
                events.Add(new WorldRuntimeEvent(
                    "luoyang.player-movement.event.initialized",
                    LuoyangFormalPlayerMovementIds.InitializedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }

            private static int Centimetres(double metres) => checked(
                (int)Math.Round(metres * 100d,
                    MidpointRounding.AwayFromZero));
        }

        private sealed class MoveTransaction : IWorldTransaction
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            private readonly WorldCommandEnvelope _command;
            private LuoyangFormalPlayerMovementState _movement;
            public MoveTransaction(LuoyangFormalPlayerMovementSystem owner,
                WorldCommandEnvelope command)
            {
                _owner = owner;
                _command = command;
            }
            public string Id => "luoyang.player-movement.transaction.start." +
                                _command.Id;
            public string KindId =>
                "mandate.transaction.person-movement.start.v1";
            public int Priority => CommandPriority;
            public void Validate(WorldState world,
                WorldTransactionValidationContext validation)
            {
                _owner.ValidateInitializedPlan(world);
                var session = new PlayerSession(world);
                var person = session.ControlledPerson;
                if (Argument(_command, PersonArgumentId) != person.Id ||
                    !session.CanAct)
                    throw new InvalidOperationException(
                        "The controlled Person cannot start this movement.");
                var origin = Argument(_command, OriginFacilityArgumentId);
                var target = Argument(_command, TargetFacilityArgumentId);
                if (origin != person.CurrentFacilityId)
                    throw new InvalidOperationException(
                        "Movement origin does not match the Person location.");
                if (_owner._localMap == null)
                {
                    if (!_owner.TryCreatePlan(world, target, out var plan,
                            out var failure))
                        throw new InvalidOperationException(failure);
                    _movement = _owner.BuildMovementState(world, person, plan,
                        _command.Id);
                }
                else
                {
                    if (!_owner.TryCreateLocalPlan(world, target,
                            out var plan, out var failure))
                        throw new InvalidOperationException(failure);
                    _movement = _owner.BuildLocalMovementState(world, person,
                        plan, _command.Id);
                }
            }
            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _movement.StartedEventId =
                    "luoyang.player-movement.event.started." + _movement.Id;
                world.LuoyangFormalPlayerMovements.Add(_movement);
                events.Add(new WorldRuntimeEvent(_movement.StartedEventId,
                    LuoyangFormalPlayerMovementIds.MovementStartedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }

        private sealed class AdvanceTransaction : IWorldTransaction
        {
            private readonly LuoyangFormalPlayerMovementSystem _owner;
            private readonly WorldCommandEnvelope _command;
            private LuoyangFormalPlayerMovementState _movement;
            private PersonState _person;
            private LuoyangFormalMovementSegmentState _segment;
            private string _interruptionReason;
            public AdvanceTransaction(
                LuoyangFormalPlayerMovementSystem owner,
                WorldCommandEnvelope command)
            {
                _owner = owner;
                _command = command;
            }
            public string Id => "luoyang.player-movement.transaction.advance." +
                                _command.Id;
            public string KindId =>
                "mandate.transaction.person-movement.advance-segment.v1";
            public int Priority => CommandPriority;
            public void Validate(WorldState world,
                WorldTransactionValidationContext validation)
            {
                var movementId = Argument(_command, MovementArgumentId);
                _movement = world.LuoyangFormalPlayerMovements.SingleOrDefault(
                    item => item.Id == movementId) ??
                    throw new InvalidOperationException(
                        "The movement action does not exist.");
                var expected = int.Parse(Argument(_command,
                    SegmentIndexArgumentId),
                    System.Globalization.CultureInfo.InvariantCulture);
                if (_movement.Status != LuoyangFormalMovementStatus.Active ||
                    _movement.CurrentSegmentIndex != expected ||
                    expected >= _movement.Segments.Count)
                    throw new InvalidOperationException(
                        "The movement segment boundary is stale.");
                _person = world.People.Single(item => item.Id ==
                    _movement.PersonId);
                _segment = _movement.Segments[expected];
                if (!_owner.CanTraverse(world, _segment))
                    _interruptionReason = LuoyangFormalPlayerMovementIds
                        .InvalidRouteReasonId;
                else if (_person.StaminaBasisPoints <
                         _segment.StaminaCostBasisPoints)
                    _interruptionReason = LuoyangFormalPlayerMovementIds
                        .InsufficientStaminaReasonId;
                else if (_person.Provisions < _segment.FoodCost)
                    _interruptionReason = LuoyangFormalPlayerMovementIds
                        .InsufficientFoodReasonId;
            }
            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _movement.LastProgressCommandId = _command.Id;
                if (!string.IsNullOrEmpty(_interruptionReason))
                {
                    _movement.Status = LuoyangFormalMovementStatus.Interrupted;
                    _movement.FailureReasonId = _interruptionReason;
                    _movement.CompletedDay = world.AbsoluteDay;
                    _movement.CompletedSegment = world.Segment;
                    _movement.CompletionEventId =
                        "luoyang.player-movement.event.interrupted." +
                        _movement.Id;
                    events.Add(new WorldRuntimeEvent(
                        "luoyang.player-movement.event.route-invalidated." +
                        _movement.Id,
                        LuoyangFormalPlayerMovementIds
                            .RouteInvalidatedEventTypeId, Id,
                        world.AbsoluteDay, (DaySegment)world.Segment));
                    events.Add(new WorldRuntimeEvent(
                        _movement.CompletionEventId,
                        LuoyangFormalPlayerMovementIds
                            .MovementInterruptedEventTypeId, Id,
                        world.AbsoluteDay, (DaySegment)world.Segment));
                    return;
                }

                _person.StaminaBasisPoints -=
                    _segment.StaminaCostBasisPoints;
                _person.Provisions -= _segment.FoodCost;
                _movement.ConsumedStaminaBasisPoints = checked(
                    _movement.ConsumedStaminaBasisPoints +
                    _segment.StaminaCostBasisPoints);
                _movement.ConsumedFood = checked(_movement.ConsumedFood +
                    _segment.FoodCost);
                _movement.ElapsedDurationMinutes = checked(
                    _movement.ElapsedDurationMinutes +
                    _segment.DurationMinutes);
                var accumulated = checked(_movement.UnsettledDurationMinutes +
                    _segment.DurationMinutes);
                _movement.UnsettledDurationMinutes = accumulated %
                    LuoyangFormalPlayerMovementIds.WorldSegmentMinutes;
                _movement.CurrentSegmentIndex++;
                if (_segment.IsLocalHumanScale)
                {
                    _person.CurrentCellId64 = _segment.ToCellId64;
                    _person.CurrentLocalSpaceId =
                        _segment.ToLocalSpaceId;
                    _person.CurrentLocalAnchorId =
                        _segment.ToLocalNodeId;
                    _person.CurrentLocalEastCentimetres =
                        _segment.ToEastCentimetres;
                    _person.CurrentLocalNorthCentimetres =
                        _segment.ToNorthCentimetres;
                    _person.CurrentLocalElevationCentimetres =
                        _segment.ToElevationCentimetres;
                    var finalLocalSegment =
                        _movement.CurrentSegmentIndex ==
                        _movement.Segments.Count;
                    _person.CurrentFacilityId = finalLocalSegment
                        ? _movement.TargetFacilityId
                        : string.Empty;
                    _person.LocationPrecisionId = finalLocalSegment
                        ? LuoyangHumanScaleLocalMapIds
                            .FacilityEntranceLocationTypeId
                        : LuoyangHumanScaleLocalMapIds
                            .GroundLocationTypeId;
                }
                else
                {
                    var local = world.LuoyangLocalNavigationLocations.Single(
                        item => item.FacilityId == _segment.ToFacilityId);
                    _person.CurrentFacilityId = local.FacilityId;
                    _person.CurrentCellId64 = local.CellId64;
                    _person.LocationPrecisionId =
                        LuoyangHumanScaleLocalMapIds.StrategicLocationTypeId;
                }
                var suffix = _movement.CurrentSegmentIndex.ToString("D4",
                    System.Globalization.CultureInfo.InvariantCulture);
                events.Add(new WorldRuntimeEvent(
                    "luoyang.player-movement.event.location." + _movement.Id +
                    "." + suffix,
                    LuoyangFormalPlayerMovementIds.LocationChangedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
                if (_movement.CurrentSegmentIndex < _movement.Segments.Count)
                {
                    events.Add(new WorldRuntimeEvent(
                        "luoyang.player-movement.event.progressed." +
                        _movement.Id + "." + suffix,
                        LuoyangFormalPlayerMovementIds
                            .MovementProgressedEventTypeId, Id,
                        world.AbsoluteDay, (DaySegment)world.Segment));
                    return;
                }
                _movement.UnsettledDurationMinutes = 0;
                _movement.Status = LuoyangFormalMovementStatus.Completed;
                _movement.CompletedDay = world.AbsoluteDay;
                _movement.CompletedSegment = world.Segment;
                _movement.CompletionEventId =
                    "luoyang.player-movement.event.completed." + _movement.Id;
                events.Add(new WorldRuntimeEvent(
                    _movement.CompletionEventId,
                    LuoyangFormalPlayerMovementIds.MovementCompletedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }

        private sealed class RoadTransitionTransaction : IWorldTransaction
        {
            private readonly WorldCommandEnvelope _command;
            private LuoyangRoadOperationalSegmentState _road;
            private string _targetStatus;
            public RoadTransitionTransaction(
                LuoyangFormalPlayerMovementSystem owner,
                WorldCommandEnvelope command)
            {
                _command = command;
            }
            public string Id => "luoyang.road-transition.transaction." +
                                _command.Id;
            public string KindId =>
                "mandate.transaction.luoyang-road-segment.transition.v1";
            public int Priority => CommandPriority;
            public void Validate(WorldState world,
                WorldTransactionValidationContext validation)
            {
                var edgeId = Argument(_command, EdgeArgumentId);
                _road = world.LuoyangRoadOperationalSegments.SingleOrDefault(
                    item => item.EdgeId == edgeId) ??
                    throw new InvalidOperationException(
                        "The road segment does not exist.");
                var revision = long.Parse(Argument(_command,
                    ExpectedRevisionArgumentId),
                    System.Globalization.CultureInfo.InvariantCulture);
                _targetStatus = Argument(_command, TargetStatusArgumentId);
                if (_road.Revision != revision ||
                    !LuoyangFormalPlayerMovementRules.IsRoadStatus(
                        _targetStatus))
                    throw new InvalidOperationException(
                        "The road transition is stale or invalid.");
            }
            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _road.StatusId = _targetStatus;
                _road.Revision++;
                _road.LastChangedDay = world.AbsoluteDay;
                _road.LastChangedSegment = world.Segment;
                _road.LastReasonId = Argument(_command, ReasonArgumentId);
                _road.LastCommandId = _command.Id;
                _road.LastEventId = "luoyang.road-transition.event." +
                                    _command.Id;
                events.Add(new WorldRuntimeEvent(_road.LastEventId,
                    LuoyangFormalPlayerMovementIds.RoadTransitionedEventTypeId,
                    Id, world.AbsoluteDay, (DaySegment)world.Segment));
            }
        }
    }

    public sealed class LuoyangFormalPlayerMovementService
    {
        private readonly LuoyangFormalPlayerMovementSystem _movementSystem;
        private readonly WorldCommandRuntime _commandRuntime;
        private readonly WorldSimulator _simulator;

        public LuoyangFormalPlayerMovementService(
            LuoyangFormalPlayerMovementSystem movementSystem,
            WorldCommandRuntime commandRuntime,
            WorldSimulator simulator)
        {
            _movementSystem = movementSystem ?? throw new ArgumentNullException(
                nameof(movementSystem));
            _commandRuntime = commandRuntime ?? throw new ArgumentNullException(
                nameof(commandRuntime));
            _simulator = simulator ?? throw new ArgumentNullException(
                nameof(simulator));
            _movementSystem.RegisterHandlers(_commandRuntime);
        }

        public bool TryRequest(WorldState world, string targetFacilityId,
            out LuoyangFormalPlayerMovementState movement,
            out LuoyangPedestrianWalkPlan presentationPlan,
            out string failureReasonId)
        {
            if (!_movementSystem.TryCreatePlan(world, targetFacilityId,
                    out presentationPlan, out failureReasonId))
            {
                movement = null;
                return false;
            }
            var commandId = _movementSystem.EnqueueMove(world,
                _commandRuntime, targetFacilityId);
            _commandRuntime.ProcessDue(world);
            _commandRuntime.DispatchPublishedEvents(world);
            movement = world.LuoyangFormalPlayerMovements.Single(item =>
                item.RequestCommandId == commandId);
            return true;
        }

        public bool TryRequestLocal(WorldState world, string targetFacilityId,
            out LuoyangFormalPlayerMovementState movement,
            out LuoyangHumanScaleLocalRoute presentationPlan,
            out string failureReasonId)
        {
            if (!_movementSystem.TryCreateLocalPlan(world, targetFacilityId,
                    out presentationPlan, out failureReasonId))
            {
                movement = null;
                return false;
            }
            var commandId = _movementSystem.EnqueueMove(world,
                _commandRuntime, targetFacilityId);
            _commandRuntime.ProcessDue(world);
            _commandRuntime.DispatchPublishedEvents(world);
            movement = world.LuoyangFormalPlayerMovements.Single(item =>
                item.RequestCommandId == commandId);
            return true;
        }

        public LuoyangFormalPlayerMovementState AdvanceNextSegment(
            WorldState world, string movementId)
        {
            var movement = world.LuoyangFormalPlayerMovements.Single(item =>
                item.Id == movementId);
            if (movement.Status != LuoyangFormalMovementStatus.Active)
                return movement;
            var segment = movement.Segments[movement.CurrentSegmentIndex];
            if (_movementSystem.CanTraverseNextSegment(world, movement))
            {
                var accumulated = checked(movement.UnsettledDurationMinutes +
                    segment.DurationMinutes);
                var worldSegments = accumulated /
                    LuoyangFormalPlayerMovementIds.WorldSegmentMinutes;
                var final = movement.CurrentSegmentIndex + 1 ==
                    movement.Segments.Count;
                if (final && accumulated %
                        LuoyangFormalPlayerMovementIds.WorldSegmentMinutes != 0)
                    worldSegments++;
                if (worldSegments > 0)
                    _simulator.AdvanceSegments(world, worldSegments);
            }
            _movementSystem.EnqueueAdvanceSegment(world, _commandRuntime,
                movement);
            _commandRuntime.ProcessDue(world);
            _commandRuntime.DispatchPublishedEvents(world);
            return movement;
        }

        public LuoyangFormalPlayerMovementState Complete(WorldState world,
            string movementId)
        {
            var movement = world.LuoyangFormalPlayerMovements.Single(item =>
                item.Id == movementId);
            while (movement.Status == LuoyangFormalMovementStatus.Active)
                AdvanceNextSegment(world, movementId);
            return movement;
        }
    }

    public sealed class WorldSimulator
    {
        private readonly NamedRandom _random;
        private readonly IPersonRepository _personRepository;
        private readonly TravelSystem _travelSystem;
        private readonly TaskSystem _taskSystem;
        private readonly HistoricalEventSystem _historicalEventSystem =
            new HistoricalEventSystem();
        private readonly LifeSimulationSystem _lifeSimulationSystem;
        private readonly MarketSimulationSystem _marketSimulationSystem;
        private readonly FormalCountyMarketSystem _formalCountyMarketSystem;
        private readonly FormalMarketDailyCommandScheduler
            _formalMarketCommandScheduler;
        private readonly CivilianFreightSystem _civilianFreightSystem;
        private readonly CivilianFreightPlanningCommandScheduler
            _civilianFreightPlanningCommandScheduler;
        private readonly ArmySystem _armySystem;
        private readonly EducationSystem _educationSystem;
        private readonly VillageLifeSystem _villageLifeSystem;
        private readonly FormalHouseholdFoodMonthlyCommandScheduler
            _formalHouseholdFoodMonthlyCommandScheduler;
        private readonly ResearchSystem _researchSystem;
        private readonly ProcessingProductionSystem _processingSystem;
        private readonly UpstreamResourceProductionSystem
            _upstreamResourceSystem;
        private readonly HerbalMedicineSupplySystem _herbalMedicineSupplySystem;
        private readonly MilitaryFieldHospitalSystem
            _militaryFieldHospitalSystem;
        private readonly MilitaryProcurementSystem _militaryProcurementSystem;
        private readonly MilitaryLogisticsSystem _militaryLogisticsSystem;
        private readonly MilitaryLogisticsDelegationSystem
            _militaryLogisticsDelegationSystem;
        private readonly MilitaryEquipmentRepairSystem _militaryRepairSystem =
            new MilitaryEquipmentRepairSystem();
        private readonly CountyGovernanceSystem _countyGovernanceSystem;
        private readonly FormalPublicFoodMonthlyCommandScheduler
            _formalPublicFoodMonthlyCommandScheduler;
        private readonly PublicReliefProcurementCommandScheduler
            _publicReliefProcurementCommandScheduler;
        private readonly PublicReliefExternalProcurementCommandScheduler
            _publicReliefExternalProcurementCommandScheduler;
        private readonly PublicReliefArrivalRecoveryCommandScheduler
            _publicReliefArrivalRecoveryCommandScheduler;
        private readonly HouseholdReliefPickupCommandScheduler
            _householdReliefPickupCommandScheduler;
        private readonly HouseholdReliefConsumptionCommandScheduler
            _householdReliefConsumptionCommandScheduler;
        private readonly FoodStorageLossCommandScheduler
            _foodStorageLossCommandScheduler;
        private readonly WorldSystemScheduler _scheduler =
            new WorldSystemScheduler();
        private readonly WorldCommandRuntime _commandRuntime;

        public WorldSimulator(
            ulong masterSeed,
            ProductionContentRegistry productionContent = null,
            IPersonRepository personRepository = null,
            WorldCommandRuntime commandRuntime = null,
            CellTraversalPlan cellTraversalPlan = null,
            IStrategicCellRouteProvider strategicCellRouteProvider = null)
        {
            _random = new NamedRandom(masterSeed);
            _personRepository = personRepository;
            _armySystem = new ArmySystem(personRepository);
            _travelSystem = new TravelSystem(personRepository);
            _taskSystem = new TaskSystem(personRepository);
            _lifeSimulationSystem = new LifeSimulationSystem(
                masterSeed, personRepository);
            _educationSystem = new EducationSystem(personRepository);
            _marketSimulationSystem = new MarketSimulationSystem(masterSeed);
            _formalCountyMarketSystem = new FormalCountyMarketSystem(
                productionContent ?? ProductionContentRegistry.CreateCore());
            _civilianFreightSystem = new CivilianFreightSystem(
                masterSeed,
                productionContent ?? ProductionContentRegistry.CreateCore(),
                cellTraversalPlan,
                strategicCellRouteProvider);
            _villageLifeSystem = new VillageLifeSystem(
                masterSeed, productionContent, personRepository);
            _formalHouseholdFoodMonthlyCommandScheduler =
                new FormalHouseholdFoodMonthlyCommandScheduler(
                    _villageLifeSystem);
            _countyGovernanceSystem = new CountyGovernanceSystem(
                productionContent);
            _formalPublicFoodMonthlyCommandScheduler =
                new FormalPublicFoodMonthlyCommandScheduler(
                    _countyGovernanceSystem,
                    _villageLifeSystem);
            _publicReliefProcurementCommandScheduler =
                new PublicReliefProcurementCommandScheduler(
                    new PublicReliefProcurementSystem(productionContent));
            _publicReliefExternalProcurementCommandScheduler =
                new PublicReliefExternalProcurementCommandScheduler(
                    new PublicReliefExternalProcurementSystem(
                        masterSeed,
                        productionContent ??
                            ProductionContentRegistry.CreateCore()));
            _publicReliefArrivalRecoveryCommandScheduler =
                new PublicReliefArrivalRecoveryCommandScheduler(
                    new PublicReliefArrivalRecoverySystem(
                        masterSeed,
                        productionContent ??
                            ProductionContentRegistry.CreateCore()));
            _householdReliefPickupCommandScheduler =
                new HouseholdReliefPickupCommandScheduler(
                    new HouseholdReliefPickupSystem(
                        productionContent ??
                            ProductionContentRegistry.CreateCore(),
                        personRepository));
            _householdReliefConsumptionCommandScheduler =
                new HouseholdReliefConsumptionCommandScheduler(
                    new HouseholdReliefConsumptionSystem(
                        productionContent ??
                            ProductionContentRegistry.CreateCore(),
                        personRepository));
            _foodStorageLossCommandScheduler =
                new FoodStorageLossCommandScheduler(
                    new FoodStorageLossSystem(
                        productionContent ??
                            ProductionContentRegistry.CreateCore()));
            _researchSystem = new ResearchSystem(productionContent);
            _processingSystem = new ProcessingProductionSystem(
                productionContent, personRepository);
            _upstreamResourceSystem =
                new UpstreamResourceProductionSystem(
                    productionContent, personRepository);
            _herbalMedicineSupplySystem = new HerbalMedicineSupplySystem(
                productionContent, personRepository);
            _militaryFieldHospitalSystem = new MilitaryFieldHospitalSystem(
                personRepository,
                productionContent ?? ProductionContentRegistry.CreateCore());
            _militaryProcurementSystem =
                new MilitaryProcurementSystem(personRepository);
            _militaryLogisticsSystem = new MilitaryLogisticsSystem(
                productionContent, personRepository);
            _militaryLogisticsDelegationSystem =
                new MilitaryLogisticsDelegationSystem(
                    productionContent, personRepository);
            _commandRuntime = commandRuntime ?? new WorldCommandRuntime();
            _formalMarketCommandScheduler =
                new FormalMarketDailyCommandScheduler(
                    _formalCountyMarketSystem);
            _civilianFreightPlanningCommandScheduler =
                new CivilianFreightPlanningCommandScheduler(
                    _civilianFreightSystem);
            if (!_commandRuntime.HasHandler(
                    FormalMarketDailyCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _formalMarketCommandScheduler.CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    FormalMarketDailyCommandScheduler.ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _formalMarketCommandScheduler.CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    CivilianFreightPlanningCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _civilianFreightPlanningCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    CivilianFreightPlanningCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _civilianFreightPlanningCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    FormalHouseholdFoodMonthlyCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _formalHouseholdFoodMonthlyCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    FormalHouseholdFoodMonthlyCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _formalHouseholdFoodMonthlyCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    FormalPublicFoodMonthlyCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _formalPublicFoodMonthlyCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    FormalPublicFoodMonthlyCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _formalPublicFoodMonthlyCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    PublicReliefProcurementCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _publicReliefProcurementCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    PublicReliefProcurementCommandScheduler.TriggerHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _publicReliefProcurementCommandScheduler
                        .CreateTriggerHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    PublicReliefProcurementCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _publicReliefProcurementCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    PublicReliefExternalProcurementCommandScheduler
                        .CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _publicReliefExternalProcurementCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    PublicReliefExternalProcurementCommandScheduler
                        .TriggerHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _publicReliefExternalProcurementCommandScheduler
                        .CreateTriggerHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    PublicReliefExternalProcurementCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _publicReliefExternalProcurementCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    PublicReliefArrivalRecoveryCommandScheduler
                        .CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _publicReliefArrivalRecoveryCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    PublicReliefArrivalRecoveryCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _publicReliefArrivalRecoveryCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    HouseholdReliefPickupCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _householdReliefPickupCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    HouseholdReliefPickupCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _householdReliefPickupCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    HouseholdReliefConsumptionCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _householdReliefConsumptionCommandScheduler
                        .CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    HouseholdReliefConsumptionCommandScheduler
                        .ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _householdReliefConsumptionCommandScheduler
                        .CreateProjectionHandler());
            }
            if (!_commandRuntime.HasHandler(
                    FoodStorageLossCommandScheduler.CommandTypeId))
            {
                _commandRuntime.RegisterHandler(
                    _foodStorageLossCommandScheduler.CreateCommandHandler());
            }
            if (!_commandRuntime.HasEventHandler(
                    FoodStorageLossCommandScheduler.ProjectionHandlerId))
            {
                _commandRuntime.RegisterEventHandler(
                    _foodStorageLossCommandScheduler
                        .CreateProjectionHandler());
            }
            RegisterScheduledSystems();
        }

        public WorldSystemScheduler Scheduler => _scheduler;

        public WorldCommandRuntime CommandRuntime => _commandRuntime;

        public bool CanBuildStrategicCellRoute(WorldState world,
            string assetRouteId, string formalWorldRouteId,
            ulong originCellId64, ulong targetCellId64,
            string movementCapabilityId) =>
            _civilianFreightSystem.CanBuildStrategicRoute(
                world,
                assetRouteId,
                formalWorldRouteId,
                originCellId64,
                targetCellId64,
                movementCapabilityId);

        public CivilianFreightState DispatchMerchantOwnedFreight(
            WorldState world, MerchantOwnedFreightDispatchRequest request) =>
            _civilianFreightSystem.DispatchMerchantOwnedCargo(world, request);

        public bool RecordMerchantOwnedFreightCargoLoss(WorldState world,
            string carrierPersonId, string commodityId, long quantity) =>
            _civilianFreightSystem.RecordMerchantOwnedCargoLoss(
                world, carrierPersonId, commodityId, quantity);

        public TradeResult SettleMerchantOwnedFreightCargoSale(
            WorldState world, string carrierPersonId, string commodityId,
            long quantity) =>
            _civilianFreightSystem.SettleMerchantOwnedCargoSale(
                world, carrierPersonId, commodityId, quantity);

        public void AdvanceDays(WorldState world, int days)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (days < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }

            if (world.MasterSeed == 0)
            {
                throw new InvalidOperationException("A world must have a non-zero master seed.");
            }

            AdvanceSegments(world, checked(days * 4));
        }

        public void AdvanceSegments(WorldState world, int segments)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (segments < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }

            _scheduler.BeginTrace();
            for (var i = 0; i < segments; i++)
            {
                world.Validate();
                var segmentContext =
                    new WorldSystemExecutionContext(world, false);
                _scheduler.ExecutePhase(
                    WorldSystemPhase.SegmentCommand,
                    segmentContext);
                _scheduler.ExecutePhase(
                    WorldSystemPhase.SegmentRuntimeEvent,
                    segmentContext);
                _scheduler.ExecutePhase(
                    WorldSystemPhase.SegmentMovement,
                    segmentContext);
                _scheduler.ExecutePhase(
                    WorldSystemPhase.SegmentArrival,
                    segmentContext);
                var enteredNewDay = world.AdvanceOneSegment();
                if (enteredNewDay)
                {
                    var dailyContext =
                        new WorldSystemExecutionContext(world, true);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailyCommand,
                        dailyContext);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailyTransit,
                        dailyContext);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailyHistoricalEvent,
                        dailyContext);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailySimulation,
                        dailyContext);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailyProjection,
                        dailyContext);
                    _scheduler.ExecutePhase(
                        WorldSystemPhase.DailyRuntimeEvent,
                        dailyContext);
                }
            }
        }

        private void RegisterScheduledSystems()
        {
            Register(
                "mandate.runtime.segment.command",
                WorldSystemPhase.SegmentCommand,
                WorldSystemCadence.EverySegment,
                10,
                context => _commandRuntime.ProcessDue(context.World));
            Register(
                "mandate.runtime.segment.runtime_events",
                WorldSystemPhase.SegmentRuntimeEvent,
                WorldSystemCadence.EverySegment,
                10,
                context => _commandRuntime.DispatchPublishedEvents(
                    context.World));
            Register(
                "mandate.runtime.segment.travel",
                WorldSystemPhase.SegmentMovement,
                WorldSystemCadence.EverySegment,
                10,
                context => _travelSystem.AdvanceJourneysOneSegment(context.World));
            Register(
                "mandate.runtime.segment.army_march",
                WorldSystemPhase.SegmentMovement,
                WorldSystemCadence.EverySegment,
                20,
                context => _armySystem.AdvanceMarchesOneSegment(context.World));
            Register(
                "mandate.runtime.segment.procurement_arrival",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                10,
                context => _militaryProcurementSystem.ResolveArrivals(context.World));
            Register(
                "mandate.runtime.segment.logistics_arrival",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                20,
                context => _militaryLogisticsSystem.ResolveArrivals(context.World));
            Register(
                "mandate.runtime.segment.civilian_freight_arrival",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                30,
                context => _civilianFreightSystem.ResolveArrivals(context.World));
            Register(
                "mandate.runtime.segment.public_relief_arrival_recovery",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                40,
                context =>
                {
                    _publicReliefArrivalRecoveryCommandScheduler
                        .EnsureDueCommands(
                            context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.segment.household_relief_pickup",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                50,
                context =>
                {
                    _householdReliefPickupCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.segment.household_relief_consumption",
                WorldSystemPhase.SegmentArrival,
                WorldSystemCadence.EverySegment,
                60,
                context =>
                {
                    _householdReliefConsumptionCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.command",
                WorldSystemPhase.DailyCommand,
                WorldSystemCadence.NewDay,
                10,
                context => _commandRuntime.ProcessDue(context.World));
            Register(
                "mandate.runtime.daily.civilian_freight_planning",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                5,
                context =>
                {
                    _civilianFreightPlanningCommandScheduler.EnsureDueCommand(
                        context.World,
                        _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.logistics_transit",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                10,
                context => context.SetScratch(
                    "mandate.runtime.provisioned_carriers",
                    _militaryLogisticsSystem.ResolveDailyTransit(context.World)));
            Register(
                "mandate.runtime.daily.travel_provisions",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                20,
                context => _travelSystem.ConsumeDailyTravelProvisions(
                    context.World,
                    context.GetScratch<ISet<string>>(
                        "mandate.runtime.provisioned_carriers")));
            Register(
                "mandate.runtime.daily.civilian_freight_transit",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                25,
                context =>
                    _civilianFreightSystem.ResolveDailyTransit(context.World));
            Register(
                "mandate.runtime.daily.army_supplies",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                30,
                context => _armySystem.ConsumeDailyMarchSupplies(context.World));
            Register(
                "mandate.runtime.daily.logistics_delegation",
                WorldSystemPhase.DailyTransit,
                WorldSystemCadence.NewDay,
                40,
                context =>
                    _militaryLogisticsDelegationSystem.ProcessDue(context.World));
            Register(
                "mandate.runtime.daily.historical_events",
                WorldSystemPhase.DailyHistoricalEvent,
                WorldSystemCadence.NewDay,
                10,
                context =>
                    _historicalEventSystem.ResolveEligibleEvents(context.World));
            Register(
                "mandate.runtime.daily.tasks",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                10,
                context => _taskSystem.ResolveDailyProgress(context.World));
            Register(
                "mandate.runtime.daily.research",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                20,
                context => _researchSystem.ResolveDailyProjects(context.World));
            Register(
                "mandate.runtime.daily.upstream_resource",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                30,
                context => _upstreamResourceSystem.ResolveDueOrders(context.World));
            Register(
                "mandate.runtime.daily.processing",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                40,
                context => _processingSystem.ResolveDueOrders(context.World));
            Register(
                "mandate.runtime.daily.herbal_medicine_supply",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                45,
                context =>
                    _herbalMedicineSupplySystem.ResolveDaily(context.World));
            Register(
                "mandate.runtime.daily.field_hospital_maintenance",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                47,
                context =>
                    _militaryFieldHospitalSystem.AssessMaintenanceDue(
                        context.World));
            Register(
                "mandate.runtime.daily.military_repair",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                50,
                context => _militaryRepairSystem.ResolveDueOrders(context.World));
            Register(
                "mandate.runtime.daily.formal_household_food_monthly",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                55,
                context =>
                {
                    _formalHouseholdFoodMonthlyCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.food_storage_loss",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                57,
                context =>
                {
                    if (context.World.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches)
                    {
                        _foodStorageLossCommandScheduler.EnsureDueCommands(
                            context.World,
                            _commandRuntime);
                        _commandRuntime.ProcessDue(context.World);
                    }
                });
            Register(
                "mandate.runtime.daily.household_relief_pickup",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                56,
                context =>
                {
                    _householdReliefPickupCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.household_relief_consumption",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                58,
                context =>
                {
                    _householdReliefConsumptionCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.village_life",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                60,
                context =>
                {
                    if (context.World.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches)
                    {
                        _villageLifeSystem
                            .ResolveMonthlyAfterFormalFoodAndPublicFoodCommands(
                                context.World);
                    }
                    else
                    {
                        _villageLifeSystem.ResolveMonthly(context.World);
                    }
                });
            Register(
                "mandate.runtime.daily.life",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay,
                70,
                context => _lifeSimulationSystem.ResolveMonthly(context.World));
            Register(
                "mandate.runtime.daily.village_cache",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                10,
                context => VillageLifeSystem.RefreshAllCaches(
                    context.World,
                    _personRepository));
            Register(
                "mandate.runtime.daily.formal_public_food_monthly",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                15,
                context =>
                {
                    _formalPublicFoodMonthlyCommandScheduler
                        .EnsureDueCommands(context.World, _commandRuntime);
                    _commandRuntime.ProcessDue(context.World);
                });
            Register(
                "mandate.runtime.daily.county_governance",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                20,
                context =>
                {
                    if (context.World.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches)
                    {
                        _countyGovernanceSystem
                            .ResolveMonthlyAfterFormalPublicFoodCommands(
                                context.World);
                    }
                    else
                    {
                        _countyGovernanceSystem.ResolveMonthly(context.World);
                    }
                });
            Register(
                "mandate.runtime.daily.education",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                30,
                context => _educationSystem.ResolveDuePlans(context.World));
            Register(
                "mandate.runtime.daily.market_prices",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                40,
                context =>
                {
                    if (context.World.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches)
                    {
                        _formalMarketCommandScheduler.EnsureDueCommand(
                            context.World,
                            _commandRuntime);
                        _commandRuntime.ProcessDue(context.World);
                    }
                    else
                    {
                        _marketSimulationSystem.ResolveDailyPrices(context.World);
                    }
                });
            Register(
                "mandate.runtime.daily.public_order",
                WorldSystemPhase.DailyProjection,
                WorldSystemCadence.NewDay,
                50,
                context => ResolvePublicOrder(context.World));
            Register(
                "mandate.runtime.daily.runtime_events",
                WorldSystemPhase.DailyRuntimeEvent,
                WorldSystemCadence.NewDay,
                10,
                context => _commandRuntime.DispatchPublishedEvents(
                    context.World));
        }

        private void Register(
            string id,
            WorldSystemPhase phase,
            WorldSystemCadence cadence,
            int order,
            Action<WorldSystemExecutionContext> execute)
        {
            _scheduler.Register(new WorldScheduledSystem(
                id,
                phase,
                cadence,
                order,
                execute));
        }

        private void ResolvePublicOrder(WorldState world)
        {
            var locations = new List<LocationState>(world.Locations);
            locations.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < locations.Count; i++)
            {
                UpdatePublicOrder(locations[i], world.AbsoluteDay);
            }
        }

        private void UpdatePublicOrder(LocationState location, long resolvingDay)
        {
            var locationId = new StableId(location.Id);
            if (!_random.CheckBasisPoints(
                    "public_order",
                    locationId,
                    resolvingDay,
                    "daily_change",
                    500))
            {
                return;
            }

            var change = _random.Range(
                "public_order",
                locationId,
                resolvingDay,
                "daily_direction",
                -20,
                21);
            location.PublicOrderBasisPoints =
                Clamp(location.PublicOrderBasisPoints + change, 0, 10_000);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
