using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class PublicReliefExternalProcurementResult
    {
        public string CountyGovernanceId;
        public long RequestedQuantity;
        public long DispatchedQuantity;
        public long GoodsMoneySpent;
        public long FreightFeeEscrowed;
        public string CivilianFreightId;
    }

    public sealed class PublicReliefExternalProcurementSystem
    {
        private readonly ProductionContentRegistry _content;
        private readonly CivilianFreightSystem _freight;

        public PublicReliefExternalProcurementSystem(
            ulong masterSeed,
            ProductionContentRegistry content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _freight = new CivilianFreightSystem(masterSeed, content);
        }

        public void Validate(
            WorldState world,
            string governanceId,
            string sourceEventId,
            long expectedDay,
            long maximumQuantity,
            long maximumGoodsBudget,
            long maximumFreightBudget,
            long maximumUnitPrice)
        {
            world.Validate();
            _content.ValidateWorldReferences(world);
            var governance = FindGovernance(world, governanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var leader = ProductInventorySystem.FindPerson(
                world, government.LeaderPersonId);
            var sourceEvent = FindEvent(world, sourceEventId);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                expectedDay != world.AbsoluteDay || expectedDay <= 0 ||
                maximumQuantity <= 0 || maximumGoodsBudget <= 0 ||
                maximumFreightBudget <= 0 || maximumUnitPrice <= 0 ||
                government.Type != OrganizationType.Government ||
                !leader.IsAlive ||
                string.IsNullOrEmpty(governance.GranaryInventoryContainerId) ||
                sourceEvent.EventTypeId !=
                    PublicReliefProcurementContractIds
                        .ExternalSourcingRequiredEventTypeId ||
                sourceEvent.Day != expectedDay - 1 ||
                sourceEvent.SourceTransactionId !=
                    PublicReliefProcurementCommandScheduler.TransactionId(
                        sourceEvent.Day, governanceId) ||
                LocalUnfilledOnDay(
                    world, governanceId, sourceEvent.Day) <= 0)
            {
                throw new InvalidOperationException(
                    "External public relief procurement command is invalid.");
            }
        }

        public PublicReliefExternalProcurementResult Resolve(
            WorldState world,
            string governanceId,
            string sourceEventId,
            string sourceCommandId,
            long maximumQuantity,
            long maximumGoodsBudget,
            long maximumFreightBudget,
            long maximumUnitPrice)
        {
            var requestedQuantity = Math.Min(
                maximumQuantity,
                LocalUnfilledOnDay(
                    world, governanceId, world.AbsoluteDay - 1));
            return ResolveRequested(
                world,
                governanceId,
                sourceEventId,
                sourceCommandId,
                requestedQuantity,
                maximumGoodsBudget,
                maximumFreightBudget,
                maximumUnitPrice,
                string.Empty,
                false);
        }

        public PublicReliefExternalProcurementResult ResolveSupplemental(
            WorldState world,
            string governanceId,
            string sourceEventId,
            string sourceCommandId,
            string recoveryId,
            long requestedQuantity,
            long maximumGoodsBudget,
            long maximumFreightBudget,
            long maximumUnitPrice)
        {
            if (requestedQuantity <= 0 || maximumGoodsBudget <= 0 ||
                maximumFreightBudget <= 0 || maximumUnitPrice <= 0 ||
                string.IsNullOrEmpty(recoveryId))
            {
                throw new InvalidOperationException(
                    "Supplemental relief procurement limits are invalid.");
            }
            _ = new StableId(recoveryId);
            return ResolveRequested(
                world,
                governanceId,
                sourceEventId,
                sourceCommandId,
                requestedQuantity,
                maximumGoodsBudget,
                maximumFreightBudget,
                maximumUnitPrice,
                recoveryId,
                true);
        }

        private PublicReliefExternalProcurementResult ResolveRequested(
            WorldState world,
            string governanceId,
            string sourceEventId,
            string sourceCommandId,
            long requestedQuantity,
            long maximumGoodsBudget,
            long maximumFreightBudget,
            long maximumUnitPrice,
            string recoveryId,
            bool supplemental)
        {
            var governance = FindGovernance(world, governanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var result = new PublicReliefExternalProcurementResult
            {
                CountyGovernanceId = governanceId,
                RequestedQuantity = requestedQuantity,
                CivilianFreightId = string.Empty
            };
            var candidate = FindBestCandidate(
                world,
                governance,
                result.RequestedQuantity,
                Math.Min(maximumGoodsBudget, government.Treasury),
                maximumFreightBudget,
                maximumUnitPrice);
            if (candidate == null)
            {
                AddFiscalLedger(
                    world, governance.Id,
                    CountyFiscalEntryType.GrainExternalProcurementUnfilled,
                    string.Empty, 0, 0, result.RequestedQuantity,
                    "No known-route external seller and civilian carrier could fulfill the relief request.");
                return result;
            }

            var freight = _freight.DispatchPublicRelief(
                world,
                new PublicReliefFreightDispatchRequest
                {
                    DestinationCountyGovernanceId = governance.Id,
                    BuyerOrganizationId = government.Id,
                    DestinationInventoryContainerId =
                        governance.GranaryInventoryContainerId,
                    SourcePublicReliefEventId = sourceEventId,
                    SourcePublicReliefCommandId = sourceCommandId,
                    PublicReliefRecoveryId = recoveryId,
                    IsSupplemental = supplemental,
                    SellOrderId = candidate.Order.Id,
                    CarrierPersonId = candidate.Registration.CarrierPersonId,
                    TransportInventoryContainerId = candidate.Registration
                        .TransportInventoryContainerId,
                    RouteIds = candidate.RouteIds,
                    Quantity = candidate.Quantity,
                    FreightFee = candidate.FreightFee
                });
            result.DispatchedQuantity = candidate.Quantity;
            result.GoodsMoneySpent = checked(
                candidate.Quantity * candidate.Order.UnitPrice);
            result.FreightFeeEscrowed = candidate.FreightFee;
            result.CivilianFreightId = freight.Id;
            AddFiscalLedger(
                world, governance.Id,
                CountyFiscalEntryType.GrainExternalProcurement,
                candidate.Order.OwnerFamilyId,
                result.GoodsMoneySpent,
                -result.GoodsMoneySpent,
                result.DispatchedQuantity,
                "County government purchased external relief food from a household seller.");
            if (result.FreightFeeEscrowed > 0)
            {
                AddFiscalLedger(
                    world, governance.Id,
                    CountyFiscalEntryType.GrainExternalFreightEscrow,
                    string.Empty,
                    0,
                    -result.FreightFeeEscrowed,
                    result.FreightFeeEscrowed,
                    "County government placed the civilian freight fee in shipment escrow.");
            }
            var unfilled = checked(
                result.RequestedQuantity - result.DispatchedQuantity);
            if (unfilled > 0)
            {
                AddFiscalLedger(
                    world, governance.Id,
                    CountyFiscalEntryType.GrainExternalProcurementUnfilled,
                    string.Empty, 0, 0, unfilled,
                    "External relief procurement remained partially unfilled.");
            }
            return result;
        }

        private Candidate FindBestCandidate(
            WorldState world,
            CountyGovernanceState destination,
            long requestedQuantity,
            long goodsBudget,
            long freightBudget,
            long maximumUnitPrice)
        {
            Candidate best = null;
            var orders = new List<FormalMarketOrderState>();
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                var order = world.FormalMarketOrders[i];
                if (order.Side == FormalMarketOrderSide.Sell &&
                    order.Status == FormalMarketOrderStatus.Active &&
                    order.ExpiryDay >= world.AbsoluteDay &&
                    order.CountyGovernanceId != destination.Id &&
                    order.RemainingQuantity > 0 &&
                    order.UnitPrice > 0 &&
                    order.UnitPrice <= maximumUnitPrice &&
                    _content.TryGetFood(order.ProductDefinitionId, out _))
                {
                    orders.Add(order);
                }
            }
            orders.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var registrations = new List<CivilianCarrierRegistrationState>();
            for (var i = 0; i < world.CivilianCarrierRegistrations.Count; i++)
            {
                if (world.CivilianCarrierRegistrations[i].Active)
                {
                    registrations.Add(world.CivilianCarrierRegistrations[i]);
                }
            }
            registrations.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));

            for (var orderIndex = 0; orderIndex < orders.Count; orderIndex++)
            {
                var order = orders[orderIndex];
                var seller = ProductInventorySystem.FindFamily(
                    world, order.OwnerFamilyId);
                for (var registrationIndex = 0;
                     registrationIndex < registrations.Count;
                     registrationIndex++)
                {
                    var registration = registrations[registrationIndex];
                    var carrier = ProductInventorySystem.FindPerson(
                        world, registration.CarrierPersonId);
                    var container = ProductInventorySystem.FindContainer(
                        world, registration.TransportInventoryContainerId);
                    if (!carrier.IsAlive || HasJourney(world, carrier.Id) ||
                        carrier.LocationId != seller.LocationId ||
                        container.LocationId != seller.LocationId)
                    {
                        continue;
                    }
                    if (!_freight.TryPlanKnownRoute(
                            world, registration, seller.LocationId,
                            destination.CountyLocationId,
                            out var routeIds, out var distance,
                            out var security))
                    {
                        continue;
                    }
                    var quantity = Math.Min(
                        requestedQuantity,
                        Math.Min(
                            order.RemainingQuantity,
                            Math.Min(
                                goodsBudget / order.UnitPrice,
                                _freight.CalculateAvailableQuantityCapacity(
                                    world, registration,
                                    order.ProductDefinitionId))));
                    while (quantity > 0)
                    {
                        var fee = _freight.CalculateRegisteredFreightFee(
                            registration, quantity, distance);
                        var goodsMoney = checked(quantity * order.UnitPrice);
                        if (fee <= freightBudget &&
                            checked(goodsMoney + fee) <=
                                FindOrganization(
                                    world,
                                    destination.GovernmentOrganizationId)
                                    .Treasury)
                        {
                            var candidate = new Candidate
                            {
                                Order = order,
                                Registration = registration,
                                RouteIds = routeIds,
                                Distance = distance,
                                Security = security,
                                Quantity = quantity,
                                FreightFee = fee
                            };
                            if (best == null || Compare(candidate, best) < 0)
                            {
                                best = candidate;
                            }
                            break;
                        }
                        quantity--;
                    }
                }
            }
            return best;
        }

        private static int Compare(Candidate left, Candidate right)
        {
            var quantity = right.Quantity.CompareTo(left.Quantity);
            if (quantity != 0) return quantity;
            var total = checked(left.Quantity * left.Order.UnitPrice +
                left.FreightFee).CompareTo(
                checked(right.Quantity * right.Order.UnitPrice +
                    right.FreightFee));
            if (total != 0) return total;
            var security = right.Security.CompareTo(left.Security);
            if (security != 0) return security;
            var distance = left.Distance.CompareTo(right.Distance);
            if (distance != 0) return distance;
            var order = string.CompareOrdinal(left.Order.Id, right.Order.Id);
            return order != 0
                ? order
                : string.CompareOrdinal(
                    left.Registration.Id, right.Registration.Id);
        }

        private static bool HasJourney(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId) return true;
            }
            return false;
        }

        private static long LocalUnfilledOnDay(
            WorldState world, string governanceId, long day)
        {
            long total = 0;
            for (var i = 0; i < world.CountyFiscalLedgerEntries.Count; i++)
            {
                var entry = world.CountyFiscalLedgerEntries[i];
                if (entry.Day == day &&
                    entry.CountyGovernanceId == governanceId &&
                    entry.Type == CountyFiscalEntryType.GrainProcurementUnfilled)
                {
                    total = checked(total + entry.Amount);
                }
            }
            return total;
        }

        private static void AddFiscalLedger(
            WorldState world,
            string governanceId,
            CountyFiscalEntryType type,
            string familyId,
            long familyMoneyDelta,
            long governmentMoneyDelta,
            long amount,
            string summary)
        {
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = $"county_fiscal.{world.AbsoluteDay}." +
                        $"{world.CountyFiscalLedgerEntries.Count:D6}",
                    Day = world.AbsoluteDay,
                    Type = type,
                    CountyGovernanceId = governanceId,
                    FamilyId = familyId,
                    VillageId = string.Empty,
                    FamilyMoneyDelta = familyMoneyDelta,
                    GovernmentMoneyDelta = governmentMoneyDelta,
                    Amount = amount,
                    Summary = summary
                });
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world, string id)
        {
            var result = world.CountyGovernances.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing county governance {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world, string id)
        {
            var result = world.Organizations.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing organization {id}.");
        }

        private static WorldEventOutboxState FindEvent(
            WorldState world, string id)
        {
            var result = world.WorldEventOutbox.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing world event {id}.");
        }

        private sealed class Candidate
        {
            public FormalMarketOrderState Order;
            public CivilianCarrierRegistrationState Registration;
            public List<string> RouteIds;
            public int Distance;
            public int Security;
            public long Quantity;
            public long FreightFee;
        }
    }

    public sealed class PublicReliefExternalProcurementCommandScheduler
    {
        public const string CommandTypeId =
            PublicReliefProcurementContractIds.ExternalProcurementCommandTypeId;
        public const string IssuerId =
            "system.public_relief_external_procurement";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string GovernanceIdArgumentId = "county_governance_id";
        public const string SourceEventIdArgumentId = "source_event_id";
        public const string MaximumQuantityArgumentId = "maximum_quantity";
        public const string MaximumGoodsBudgetArgumentId =
            "maximum_goods_budget";
        public const string MaximumFreightBudgetArgumentId =
            "maximum_freight_budget";
        public const string MaximumUnitPriceArgumentId = "maximum_unit_price";
        public const string TransactionKindId =
            "mandate.transaction.public_relief.procure_external_shortfall";
        public const string EventTypeId =
            "mandate.event.public_relief.external_procurement_dispatched";
        public const string TriggerHandlerId =
            "mandate.handler.public_relief.external_sourcing_trigger";
        public const string ProjectionHandlerId =
            "mandate.handler.public_relief.external_procurement_projection";

        private readonly PublicReliefExternalProcurementSystem _system;

        public PublicReliefExternalProcurementCommandScheduler(
            PublicReliefExternalProcurementSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new CommandHandler(_system);
        public IWorldRuntimeEventHandler CreateTriggerHandler() =>
            new TriggerHandler();
        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new ProjectionHandler();

        public static string CommandId(long sourceDay, string governanceId) =>
            $"public_relief.external_procurement_command.{sourceDay:D10}.{governanceId}";
        public static string TransactionId(long day, string governanceId) =>
            $"public_relief.external_procurement_transaction.{day:D10}.{governanceId}";
        public static string EventId(long day, string governanceId) =>
            $"public_relief.external_procurement_dispatched.{day:D10}.{governanceId}";

        private sealed class TriggerHandler : IWorldRuntimeEventHandler
        {
            public string HandlerId => TriggerHandlerId;
            public string EventTypeId =>
                PublicReliefProcurementContractIds
                    .ExternalSourcingRequiredEventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                const string marker = "public_relief.procurement_transaction.";
                if (!worldEvent.SourceTransactionId.StartsWith(
                        marker, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "External sourcing event has an invalid source transaction.");
                }
                var separator = worldEvent.SourceTransactionId.IndexOf(
                    '.', marker.Length + 10);
                if (separator < 0)
                {
                    throw new InvalidOperationException(
                        "External sourcing event lacks a county governance ID.");
                }
                var governanceId = worldEvent.SourceTransactionId.Substring(
                    separator + 1);
                _ = new StableId(governanceId);
                var expectedDay = checked(worldEvent.Day + 1);
                commandRuntime.Enqueue(new WorldCommandEnvelope(
                    CommandId(worldEvent.Day, governanceId),
                    CommandTypeId,
                    IssuerId,
                    expectedDay,
                    DaySegment.Dawn,
                    6,
                    new Dictionary<string, string>
                    {
                        { ExpectedDayArgumentId, Invariant(expectedDay) },
                        { GovernanceIdArgumentId, governanceId },
                        { SourceEventIdArgumentId, worldEvent.Id },
                        { MaximumQuantityArgumentId, "10000" },
                        { MaximumGoodsBudgetArgumentId, "100000" },
                        { MaximumFreightBudgetArgumentId, "25000" },
                        { MaximumUnitPriceArgumentId, "100" }
                    }));
            }
        }

        private sealed class CommandHandler : IWorldCommandHandler
        {
            private readonly PublicReliefExternalProcurementSystem _system;
            public CommandHandler(PublicReliefExternalProcurementSystem system) =>
                _system = system;
            public string CommandTypeId =>
                PublicReliefExternalProcurementCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 7 ||
                    !TryPositive(command, ExpectedDayArgumentId, out var day) ||
                    !TryId(command, GovernanceIdArgumentId, out var governance) ||
                    !TryId(command, SourceEventIdArgumentId, out var sourceEvent) ||
                    !TryPositive(command, MaximumQuantityArgumentId, out var quantity) ||
                    !TryPositive(command, MaximumGoodsBudgetArgumentId, out var goodsBudget) ||
                    !TryPositive(command, MaximumFreightBudgetArgumentId, out var freightBudget) ||
                    !TryPositive(command, MaximumUnitPriceArgumentId, out var unitPrice))
                {
                    throw new InvalidOperationException(
                        "External public relief command arguments are invalid.");
                }
                transactions.Add(new Transaction(
                    _system, command.Id, day, governance, sourceEvent,
                    quantity, goodsBudget, freightBudget, unitPrice));
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly PublicReliefExternalProcurementSystem _system;
            private readonly string _commandId;
            private readonly long _day;
            private readonly string _governance;
            private readonly string _sourceEvent;
            private readonly long _quantity;
            private readonly long _goodsBudget;
            private readonly long _freightBudget;
            private readonly long _unitPrice;

            public Transaction(
                PublicReliefExternalProcurementSystem system,
                string commandId,
                long day,
                string governance,
                string sourceEvent,
                long quantity,
                long goodsBudget,
                long freightBudget,
                long unitPrice)
            {
                _system = system;
                _commandId = commandId;
                _day = day;
                _governance = governance;
                _sourceEvent = sourceEvent;
                _quantity = quantity;
                _goodsBudget = goodsBudget;
                _freightBudget = freightBudget;
                _unitPrice = unitPrice;
                Id = TransactionId(day, governance);
            }

            public string Id { get; }
            public string KindId => TransactionKindId;
            public int Priority => 6;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.Validate(
                    world, _governance, _sourceEvent, _day, _quantity,
                    _goodsBudget, _freightBudget, _unitPrice);
                validation.Reserve(
                    "public_relief.external." + _sourceEvent,
                    1, 1, Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                _system.Resolve(
                    world, _governance, _sourceEvent, _commandId,
                    _quantity, _goodsBudget, _freightBudget, _unitPrice);
                events.Add(new WorldRuntimeEvent(
                    EventId(_day, _governance),
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
                PublicReliefExternalProcurementCommandScheduler.EventTypeId;
            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
            }
        }

        private static string Invariant(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static bool TryPositive(
            WorldCommandEnvelope command, string key, out long value)
        {
            value = 0;
            return command.Arguments.TryGetValue(key, out var text) &&
                long.TryParse(
                    text, NumberStyles.None, CultureInfo.InvariantCulture,
                    out value) && value > 0;
        }

        private static bool TryId(
            WorldCommandEnvelope command, string key, out string value)
        {
            if (!command.Arguments.TryGetValue(key, out value) ||
                string.IsNullOrEmpty(value)) return false;
            _ = new StableId(value);
            return true;
        }
    }
}
