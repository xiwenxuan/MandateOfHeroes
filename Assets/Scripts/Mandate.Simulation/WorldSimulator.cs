using System;
using System.Collections.Generic;
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
        private readonly WorldSystemScheduler _scheduler =
            new WorldSystemScheduler();
        private readonly WorldCommandRuntime _commandRuntime;

        public WorldSimulator(
            ulong masterSeed,
            ProductionContentRegistry productionContent = null,
            IPersonRepository personRepository = null,
            WorldCommandRuntime commandRuntime = null)
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
                productionContent ?? ProductionContentRegistry.CreateCore());
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
            _researchSystem = new ResearchSystem(productionContent);
            _processingSystem = new ProcessingProductionSystem(
                productionContent, personRepository);
            _upstreamResourceSystem =
                new UpstreamResourceProductionSystem(
                    productionContent, personRepository);
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
            RegisterScheduledSystems();
        }

        public WorldSystemScheduler Scheduler => _scheduler;

        public WorldCommandRuntime CommandRuntime => _commandRuntime;

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
