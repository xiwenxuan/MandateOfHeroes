using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HouseholdReliefPickupResult
    {
        public int RequestsVisited;
        public int RequestsFulfilled;
        public long DeliveredNutritionBasisUnits;
        public long DeliveredPhysicalQuantity;
        public readonly List<string> InventoryTransactionIds =
            new List<string>();
    }

    public sealed class HouseholdReliefPickupSystem
    {
        private readonly FoodInventorySystem _foodInventory;
        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public HouseholdReliefPickupSystem(
            ProductionContentRegistry content,
            IPersonRepository people = null)
        {
            _foodInventory = new FoodInventorySystem(
                content ?? ProductionContentRegistry.CreateCore());
            _people = people;
        }

        public static void RecordMonthlyShortfalls(
            WorldState world,
            string sourceEventId,
            long settlementDay,
            string villageId,
            IList<FormalHouseholdFoodShortfallResult> shortfalls)
        {
            if (world == null || string.IsNullOrEmpty(sourceEventId) ||
                string.IsNullOrEmpty(villageId) || settlementDay <= 0 ||
                shortfalls == null)
            {
                throw new InvalidOperationException(
                    "Household relief shortfall facts are invalid.");
            }

            var village = FindVillage(world, villageId);
            ResolveAuthorizationSnapshot(
                world,
                village,
                out var authorizationPolicyId,
                out var authorizingOrganizationId,
                out var authorizingPersonId);

            for (var i = 0; i < shortfalls.Count; i++)
            {
                var shortfall = shortfalls[i] ??
                    throw new InvalidOperationException(
                        "A household food shortfall cannot be null.");
                if (string.IsNullOrEmpty(shortfall.FamilyId) ||
                    shortfall.RequiredNutritionBasisUnits <= 0 ||
                    shortfall.ProvidedNutritionBasisUnits < 0 ||
                    shortfall.MissingNutritionBasisUnits <= 0 ||
                    shortfall.RequiredNutritionBasisUnits -
                        shortfall.ProvidedNutritionBasisUnits !=
                        shortfall.MissingNutritionBasisUnits ||
                    shortfall.AffectedPeople.Count == 0)
                {
                    throw new InvalidOperationException(
                        "A household food shortfall is inconsistent.");
                }

                var id = PickupId(
                    settlementDay, villageId, shortfall.FamilyId);
                if (world.HouseholdReliefPickups.Exists(item =>
                        item.Id == id))
                {
                    throw new InvalidOperationException(
                        $"Household relief pickup {id} already exists.");
                }

                var vulnerableAffectedPersonCount = 0;
                for (var personIndex = 0;
                     personIndex < shortfall.AffectedPeople.Count;
                     personIndex++)
                {
                    if (shortfall.AffectedPeople[personIndex]
                            .RequiredNutritionBasisUnits == 20_000)
                    {
                        vulnerableAffectedPersonCount++;
                    }
                }

                var pickup =
                    new HouseholdReliefPickupState
                    {
                        Id = id,
                        Status = HouseholdReliefPickupStatus.Waiting,
                        SourceShortfallEventId = sourceEventId,
                        VillageId = villageId,
                        FamilyId = shortfall.FamilyId,
                        PriorityPolicyId =
                            village.HouseholdReliefPriorityPolicyId,
                        AuthorizationPolicyId = authorizationPolicyId,
                        AuthorizingOrganizationId = authorizingOrganizationId,
                        AuthorizingPersonId = authorizingPersonId,
                        AuthorizedDay = settlementDay,
                        ShortfallSeverityBasisPoints = checked((int)Math.Max(
                            1L,
                            Math.Min(
                                10_000L,
                                shortfall.MissingNutritionBasisUnits * 10_000L /
                                shortfall.RequiredNutritionBasisUnits))),
                        VulnerableAffectedPersonCount =
                            vulnerableAffectedPersonCount,
                        AffectedPersonCountAtAuthorization =
                            shortfall.AffectedPeople.Count,
                        SettlementDay = settlementDay,
                        RequestedNutritionBasisUnits =
                            shortfall.MissingNutritionBasisUnits,
                        RemainingNutritionBasisUnits =
                            shortfall.MissingNutritionBasisUnits,
                        LastCollectorPersonId = string.Empty,
                        LastPickupDay = -1
                    };
                world.HouseholdReliefPickups.Add(pickup);

                var consumption = new HouseholdReliefConsumptionState
                {
                    Id = HouseholdReliefConsumptionSystem.ConsumptionId(
                        settlementDay, villageId, shortfall.FamilyId),
                    Status = HouseholdReliefConsumptionStatus.Waiting,
                    PickupId = pickup.Id,
                    SourceShortfallEventId = sourceEventId,
                    VillageId = villageId,
                    FamilyId = shortfall.FamilyId,
                    AllocationPolicyId = HouseholdReliefAllocationPolicyIds
                        .ProportionalIndividualNeed,
                    CareDeliveryPolicyId =
                        HouseholdReliefCareDeliveryPolicyIds
                            .AgeHealthDependency,
                    SettlementDay = settlementDay,
                    RequestedNutritionBasisUnits =
                        shortfall.MissingNutritionBasisUnits,
                    RemainingNutritionBasisUnits =
                        shortfall.MissingNutritionBasisUnits,
                    LastConsumerPersonId = string.Empty,
                    LastConsumptionDay = -1
                };
                for (var personIndex = 0;
                     personIndex < shortfall.AffectedPeople.Count;
                     personIndex++)
                {
                    var affected = shortfall.AffectedPeople[personIndex];
                    if (affected == null ||
                        string.IsNullOrEmpty(affected.PersonId) ||
                        affected.AppliedHealthDamageBasisPoints < 0 ||
                        affected.AppliedLivelihoodPressureBasisPoints < 0 ||
                        affected.RequiredNutritionBasisUnits <= 0 ||
                        affected.AllocatedNutritionBasisUnits < 0)
                    {
                        throw new InvalidOperationException(
                            "A household food affected person is invalid.");
                    }
                    consumption.AffectedPeople.Add(
                        new HouseholdReliefAffectedPersonState
                        {
                            PersonId = affected.PersonId,
                            RequiresCaregiverDelivery =
                                affected.RequiresCaregiverDelivery,
                            RequiredNutritionBasisUnits =
                                affected.RequiredNutritionBasisUnits,
                            AllocatedNutritionBasisUnits =
                                affected.AllocatedNutritionBasisUnits,
                            AppliedHealthDamageBasisPoints = affected
                                .AppliedHealthDamageBasisPoints,
                            AppliedLivelihoodPressureBasisPoints = affected
                                .AppliedLivelihoodPressureBasisPoints
                        });
                }
                consumption.AffectedPeople.Sort((left, right) =>
                    string.CompareOrdinal(left.PersonId, right.PersonId));
                long allocatedNutrition = 0;
                for (var personIndex = 0;
                     personIndex < consumption.AffectedPeople.Count;
                     personIndex++)
                {
                    allocatedNutrition = checked(
                        allocatedNutrition + consumption
                            .AffectedPeople[personIndex]
                            .AllocatedNutritionBasisUnits);
                }
                if (allocatedNutrition !=
                    consumption.RequestedNutritionBasisUnits)
                {
                    throw new InvalidOperationException(
                        "Household relief person allocations do not close.");
                }
                world.HouseholdReliefConsumptions.Add(consumption);
            }
        }

        public bool HasDeliverableWork(WorldState world, string villageId)
        {
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                return false;
            }
            var village = FindVillage(world, villageId);
            if (_foodInventory.SummarizeContainer(
                    world, village.PublicGranaryInventoryContainerId)
                .PhysicalQuantity <= 0)
            {
                return false;
            }

            for (var i = 0; i < world.HouseholdReliefPickups.Count; i++)
            {
                var request = world.HouseholdReliefPickups[i];
                if (request.VillageId == villageId &&
                    request.Status != HouseholdReliefPickupStatus.Fulfilled &&
                    request.RemainingNutritionBasisUnits > 0 &&
                    FindCollector(world, request.FamilyId, village.LocationId) !=
                        null &&
                    HasHouseholdCapacity(
                        world, villageId, request.FamilyId))
                {
                    return true;
                }
            }
            return false;
        }

        public void Validate(
            WorldState world,
            string villageId,
            long expectedDay,
            byte expectedSegment)
        {
            world.Validate();
            if (world.AbsoluteDay != expectedDay ||
                world.Segment != expectedSegment ||
                !HasDeliverableWork(world, villageId))
            {
                throw new InvalidOperationException(
                    "Household relief pickup is not due at the current world time.");
            }
        }

        public HouseholdReliefPickupResult Resolve(
            WorldState world,
            string villageId)
        {
            var result = new HouseholdReliefPickupResult();
            var village = FindVillage(world, villageId);
            var requests = new List<HouseholdReliefPickupState>();
            for (var i = 0; i < world.HouseholdReliefPickups.Count; i++)
            {
                var request = world.HouseholdReliefPickups[i];
                if (request.VillageId == villageId &&
                    request.Status != HouseholdReliefPickupStatus.Fulfilled &&
                    request.RemainingNutritionBasisUnits > 0)
                {
                    requests.Add(request);
                }
            }
            requests.Sort(CompareRequests);

            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                result.RequestsVisited++;
                var collector = FindCollector(
                    world, request.FamilyId, village.LocationId);
                var storage = FindHouseholdGranary(
                    world, villageId, request.FamilyId);
                if (collector == null || storage == null ||
                    storage.InventoryUnits >= storage.Capacity)
                {
                    continue;
                }

                var transfer = _foodInventory
                    .TransferContainerToFamilyByNutrition(
                        world,
                        village.PublicGranaryInventoryContainerId,
                        request.FamilyId,
                        storage.Id,
                        collector.Id,
                        request.RemainingNutritionBasisUnits,
                        InventoryTransactionType
                            .FoodVillageReliefTransferred,
                        village.Id);
                if (transfer.TransferredPhysicalQuantity <= 0)
                {
                    break;
                }

                request.DeliveredNutritionBasisUnits = checked(
                    request.DeliveredNutritionBasisUnits +
                    transfer.TransferredNutritionBasisUnits);
                request.DeliveredPhysicalQuantity = checked(
                    request.DeliveredPhysicalQuantity +
                    transfer.TransferredPhysicalQuantity);
                request.RemainingNutritionBasisUnits = Math.Max(
                    0L,
                    request.RequestedNutritionBasisUnits -
                    request.DeliveredNutritionBasisUnits);
                request.LastCollectorPersonId = collector.Id;
                request.LastPickupDay = world.AbsoluteDay;
                request.InventoryTransactionIds.Add(
                    transfer.InventoryTransactionId);
                request.Status = request.RemainingNutritionBasisUnits == 0
                    ? HouseholdReliefPickupStatus.Fulfilled
                    : HouseholdReliefPickupStatus.PartiallyDelivered;
                if (request.Status == HouseholdReliefPickupStatus.Fulfilled)
                {
                    result.RequestsFulfilled++;
                }
                result.DeliveredNutritionBasisUnits = checked(
                    result.DeliveredNutritionBasisUnits +
                    transfer.TransferredNutritionBasisUnits);
                result.DeliveredPhysicalQuantity = checked(
                    result.DeliveredPhysicalQuantity +
                    transfer.TransferredPhysicalQuantity);
                result.InventoryTransactionIds.Add(
                    transfer.InventoryTransactionId);
                world.VillageLedgerEntries.Add(new VillageLedgerEntryState
                {
                    Id = $"village_ledger.relief_pickup.{request.Id}." +
                        $"{request.InventoryTransactionIds.Count:D4}",
                    Day = world.AbsoluteDay,
                    Type = VillageLedgerEntryType.GrainRelief,
                    VillageId = village.Id,
                    FamilyId = request.FamilyId,
                    PersonId = collector.Id,
                    Quantity = (int)Math.Min(
                        int.MaxValue,
                        transfer.TransferredPhysicalQuantity),
                    Summary = $"{request.FamilyId} collected " +
                        $"{transfer.TransferredPhysicalQuantity} food units " +
                        "from the village granary."
                });
            }
            return result;
        }

        public static string PickupId(
            long settlementDay,
            string villageId,
            string familyId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.pickup.{0:D10}.{1}.{2}",
                settlementDay,
                villageId,
                familyId);

        private static int CompareRequests(
            HouseholdReliefPickupState left,
            HouseholdReliefPickupState right)
        {
            var byDay = left.SettlementDay.CompareTo(right.SettlementDay);
            if (byDay != 0)
            {
                return byDay;
            }
            var byVillage = string.CompareOrdinal(
                left.VillageId, right.VillageId);
            if (byVillage != 0)
            {
                return byVillage;
            }
            if (left.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability &&
                right.PriorityPolicyId ==
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability)
            {
                var bySeverity = right.ShortfallSeverityBasisPoints.CompareTo(
                    left.ShortfallSeverityBasisPoints);
                if (bySeverity != 0)
                {
                    return bySeverity;
                }
                var byVulnerability =
                    right.VulnerableAffectedPersonCount.CompareTo(
                        left.VulnerableAffectedPersonCount);
                if (byVulnerability != 0)
                {
                    return byVulnerability;
                }
                var byAffected =
                    right.AffectedPersonCountAtAuthorization.CompareTo(
                        left.AffectedPersonCountAtAuthorization);
                if (byAffected != 0)
                {
                    return byAffected;
                }
            }
            return string.CompareOrdinal(left.FamilyId, right.FamilyId);
        }

        private static void ResolveAuthorizationSnapshot(
            WorldState world,
            VillageState village,
            out string authorizationPolicyId,
            out string authorizingOrganizationId,
            out string authorizingPersonId)
        {
            authorizationPolicyId =
                village.HouseholdReliefAuthorizationPolicyId;
            if (authorizationPolicyId ==
                HouseholdReliefAuthorizationPolicyIds.EmergencySystem)
            {
                authorizingOrganizationId = string.Empty;
                authorizingPersonId = string.Empty;
                return;
            }
            if (authorizationPolicyId !=
                HouseholdReliefAuthorizationPolicyIds.CountyGovernmentLeader)
            {
                throw new InvalidOperationException(
                    $"Village {village.Id} has an unsupported relief authority policy.");
            }

            var organization = world.Organizations.Find(item =>
                item.Id == village.HouseholdReliefAuthorityOrganizationId);
            if (organization == null ||
                organization.Type != OrganizationType.Government ||
                string.IsNullOrEmpty(organization.LeaderPersonId))
            {
                throw new InvalidOperationException(
                    $"Village {village.Id} has no valid relief authority.");
            }
            authorizingOrganizationId = organization.Id;
            authorizingPersonId = organization.LeaderPersonId;
        }

        private bool HasHouseholdCapacity(
            WorldState world,
            string villageId,
            string familyId)
        {
            var storage = FindHouseholdGranary(world, villageId, familyId);
            return storage != null && storage.InventoryUnits < storage.Capacity;
        }

        private PersonState FindCollector(
            WorldState world,
            string familyId,
            string villageLocationId)
        {
            var family = FindFamily(world, familyId);
            var people = PeopleFor(world);
            var candidates = new List<PersonState>();
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = people.GetRequired(family.MemberIds[i]);
                if (person.IsAlive && person.LocationId == villageLocationId &&
                    person.LocalDuty != LocalDutyKind.Levy)
                {
                    candidates.Add(person);
                }
            }
            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Id == family.HeadPersonId)
                {
                    return candidates[i];
                }
            }
            return candidates.Count == 0 ? null : candidates[0];
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            if (_people != null)
            {
                return _people;
            }
            if (!ReferenceEquals(_fallbackWorld, world))
            {
                _fallbackWorld = world;
                _fallbackPeople = new WorldStatePersonRepository(world);
            }
            return _fallbackPeople;
        }

        private static VillageState FindVillage(
            WorldState world, string villageId) =>
            world.Villages.Find(item => item.Id == villageId) ??
            throw new InvalidOperationException(
                $"Missing village {villageId}.");

        private static FamilyState FindFamily(
            WorldState world, string familyId) =>
            world.Families.Find(item => item.Id == familyId) ??
            throw new InvalidOperationException(
                $"Missing family {familyId}.");

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string villageId,
            string familyId) => world.VillageFacilities.Find(item =>
                item.VillageId == villageId &&
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == familyId);
    }

    public sealed class HouseholdReliefPickupCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.household_relief.resolve_pickups";
        public const string TransactionKindId =
            "mandate.transaction.household_relief.resolve_pickups";
        public const string EventTypeId =
            "mandate.event.household_relief.pickups_resolved";
        public const string ProjectionHandlerId =
            "mandate.handler.household_relief.pickup_projection";
        public const string IssuerId = "system.household_relief_pickup";
        public const string VillageIdArgumentId = "village_id";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string ExpectedSegmentArgumentId = "expected_segment";

        private readonly HouseholdReliefPickupSystem _system;

        public HouseholdReliefPickupCommandScheduler(
            HouseholdReliefPickupSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public int EnsureDueCommands(
            WorldState world,
            WorldCommandRuntime runtime)
        {
            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var created = 0;
            for (var i = 0; i < villages.Count; i++)
            {
                var village = villages[i];
                var commandId = CommandId(
                    world.AbsoluteDay, world.Segment, village.Id);
                if (!_system.HasDeliverableWork(world, village.Id) ||
                    world.PersistentWorldCommands.Exists(item =>
                        item.Id == commandId))
                {
                    continue;
                }
                runtime.Enqueue(
                    world,
                    new WorldCommandEnvelope(
                        commandId,
                        CommandTypeId,
                        IssuerId,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment,
                        56,
                        new Dictionary<string, string>
                        {
                            { VillageIdArgumentId, village.Id },
                            {
                                ExpectedDayArgumentId,
                                Invariant(world.AbsoluteDay)
                            },
                            {
                                ExpectedSegmentArgumentId,
                                Invariant(world.Segment)
                            }
                        }));
                created++;
            }
            return created;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new CommandHandler(_system);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new ProjectionHandler();

        public static string CommandId(
            long day, byte segment, string villageId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.pickup_command.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        public static string TransactionId(
            long day, byte segment, string villageId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.pickup_transaction.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        public static string EventId(
            long day, byte segment, string villageId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.pickup_resolved.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        private sealed class CommandHandler : IWorldCommandHandler
        {
            private readonly HouseholdReliefPickupSystem _system;

            public CommandHandler(HouseholdReliefPickupSystem system)
            {
                _system = system;
            }

            public string CommandTypeId =>
                HouseholdReliefPickupCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 3 ||
                    !command.Arguments.TryGetValue(
                        VillageIdArgumentId, out var villageId) ||
                    string.IsNullOrEmpty(villageId) ||
                    !TryLong(command, ExpectedDayArgumentId, out var day) ||
                    !TryByte(
                        command, ExpectedSegmentArgumentId, out var segment) ||
                    segment > (byte)DaySegment.Night)
                {
                    throw new InvalidOperationException(
                        "Household relief pickup command arguments are invalid.");
                }
                _ = new StableId(villageId);
                transactions.Add(new Transaction(
                    _system, villageId, day, segment));
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly HouseholdReliefPickupSystem _system;
            private readonly string _villageId;
            private readonly long _day;
            private readonly byte _segment;

            public Transaction(
                HouseholdReliefPickupSystem system,
                string villageId,
                long day,
                byte segment)
            {
                _system = system;
                _villageId = villageId;
                _day = day;
                _segment = segment;
                Id = TransactionId(day, segment, villageId);
            }

            public string Id { get; }
            public string KindId => TransactionKindId;
            public int Priority => 56;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.Validate(world, _villageId, _day, _segment);
                validation.Reserve(
                    "household_relief.pickup." + _villageId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var result = _system.Resolve(world, _villageId);
                if (result.DeliveredPhysicalQuantity <= 0)
                {
                    throw new InvalidOperationException(
                        "A deliverable household relief command made no progress.");
                }
                events.Add(new WorldRuntimeEvent(
                    EventId(_day, _segment, _villageId),
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
                HouseholdReliefPickupCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
            }
        }

        private static string Invariant(long value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static bool TryLong(
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
