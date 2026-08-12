using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HouseholdReliefConsumptionResult
    {
        public int ClaimsVisited;
        public int ClaimsFulfilled;
        public int PeopleRecovered;
        public long ConsumedNutritionBasisUnits;
        public long ConsumedPreparedNutritionBasisUnits;
        public long ConsumedPhysicalQuantity;
        public readonly List<string> InventoryTransactionIds =
            new List<string>();
    }

    public sealed class HouseholdReliefConsumptionSystem
    {
        private readonly FoodInventorySystem _foodInventory;
        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public HouseholdReliefConsumptionSystem(
            ProductionContentRegistry content,
            IPersonRepository people = null)
        {
            _foodInventory = new FoodInventorySystem(
                content ?? ProductionContentRegistry.CreateCore());
            _people = people;
        }

        public bool HasConsumableWork(WorldState world, string villageId)
        {
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                return false;
            }
            var village = FindVillage(world, villageId);
            for (var i = 0; i < world.HouseholdReliefConsumptions.Count; i++)
            {
                var consumption = world.HouseholdReliefConsumptions[i];
                if (consumption.VillageId != villageId ||
                    consumption.Status ==
                        HouseholdReliefConsumptionStatus.Fulfilled ||
                    consumption.RemainingNutritionBasisUnits <= 0)
                {
                    continue;
                }
                var pickup = FindPickup(world, consumption.PickupId);
                var storage = FindHouseholdGranary(
                    world, villageId, consumption.FamilyId);
                var eligible = FindServiceableAffectedPeople(
                    world, consumption, village.LocationId);
                if (storage != null && eligible.Count > 0 &&
                    (IsIndividualAllocation(consumption) &&
                        consumption.PreparedNutritionBasisUnits > 0 ||
                     pickup.InventoryTransactionIds.Count > 0 &&
                        HasLinkedFood(
                            world, consumption, pickup, storage.Id)))
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
                !HasConsumableWork(world, villageId))
            {
                throw new InvalidOperationException(
                    "Household relief consumption is not due at the current world time.");
            }
        }

        public HouseholdReliefConsumptionResult Resolve(
            WorldState world,
            string villageId)
        {
            var result = new HouseholdReliefConsumptionResult();
            var village = FindVillage(world, villageId);
            var claims = new List<HouseholdReliefConsumptionState>();
            for (var i = 0; i < world.HouseholdReliefConsumptions.Count; i++)
            {
                var claim = world.HouseholdReliefConsumptions[i];
                if (claim.VillageId == villageId &&
                    claim.Status != HouseholdReliefConsumptionStatus.Fulfilled &&
                    claim.RemainingNutritionBasisUnits > 0)
                {
                    claims.Add(claim);
                }
            }
            claims.Sort(CompareClaims);

            for (var i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                result.ClaimsVisited++;
                var pickup = FindPickup(world, claim.PickupId);
                var storage = FindHouseholdGranary(
                    world, villageId, claim.FamilyId);
                if (claim.CareDeliveryPolicyId ==
                    HouseholdReliefCareDeliveryPolicyIds.AgeHealthDependency)
                {
                    ResolveCareAwareClaim(
                        world, village, claim, pickup, storage, result);
                    continue;
                }
                var eligibleAffected = FindEligibleAffectedPeople(
                    world, claim, village.LocationId);
                var consumer = eligibleAffected.Count == 0
                    ? null
                    : PeopleFor(world).GetRequired(
                        eligibleAffected[0].PersonId);
                if (consumer == null || storage == null)
                {
                    continue;
                }

                long preparedNutritionConsumed = 0;
                if (IsIndividualAllocation(claim) &&
                    claim.PreparedNutritionBasisUnits > 0)
                {
                    preparedNutritionConsumed = AllocateConsumedNutrition(
                        eligibleAffected,
                        claim.PreparedNutritionBasisUnits);
                    claim.PreparedNutritionBasisUnits -=
                        preparedNutritionConsumed;
                    claim.RemainingNutritionBasisUnits =
                        CalculateClaimOutstandingNutrition(claim);
                    claim.LastConsumerPersonId = consumer.Id;
                    claim.LastConsumptionDay = world.AbsoluteDay;
                    claim.Status = claim.RemainingNutritionBasisUnits == 0
                        ? HouseholdReliefConsumptionStatus.Fulfilled
                        : HouseholdReliefConsumptionStatus.PartiallyConsumed;
                    result.ConsumedPreparedNutritionBasisUnits = checked(
                        result.ConsumedPreparedNutritionBasisUnits +
                        preparedNutritionConsumed);
                }

                if (claim.RemainingNutritionBasisUnits == 0)
                {
                    result.PeopleRecovered += RecoverAffectedPeople(
                        world, claim, village.LocationId);
                    result.ClaimsFulfilled++;
                    continue;
                }

                if (pickup.InventoryTransactionIds.Count == 0 ||
                    !HasLinkedFood(world, claim, pickup, storage.Id))
                {
                    if (preparedNutritionConsumed > 0)
                    {
                        result.PeopleRecovered += RecoverAffectedPeople(
                            world, claim, village.LocationId);
                    }
                    continue;
                }

                var requestedNutrition = IsIndividualAllocation(claim)
                    ? CalculateEligibleOutstandingNutrition(
                        eligibleAffected)
                    : claim.RemainingNutritionBasisUnits;
                if (requestedNutrition <= 0)
                {
                    continue;
                }

                var consumed = _foodInventory.ConsumeHouseholdReliefFood(
                    world,
                    claim.FamilyId,
                    storage.Id,
                    consumer.Id,
                    string.Empty,
                    requestedNutrition,
                    pickup.InventoryTransactionIds,
                    claim.Id);
                if (consumed.ConsumedPhysicalQuantity <= 0)
                {
                    continue;
                }

                claim.ConsumedNutritionBasisUnits = checked(
                    claim.ConsumedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits);
                claim.ConsumedPhysicalQuantity = checked(
                    claim.ConsumedPhysicalQuantity +
                    consumed.ConsumedPhysicalQuantity);
                if (IsIndividualAllocation(claim))
                {
                    var allocatedNutrition = AllocateConsumedNutrition(
                        eligibleAffected,
                        consumed.ProvidedNutritionBasisUnits);
                    claim.PreparedNutritionBasisUnits = checked(
                        claim.PreparedNutritionBasisUnits +
                        consumed.ProvidedNutritionBasisUnits -
                        allocatedNutrition);
                    claim.RemainingNutritionBasisUnits =
                        CalculateClaimOutstandingNutrition(claim);
                }
                else
                {
                    claim.RemainingNutritionBasisUnits = Math.Max(
                        0L,
                        claim.RequestedNutritionBasisUnits -
                        claim.ConsumedNutritionBasisUnits);
                }
                claim.LastConsumerPersonId = consumer.Id;
                claim.LastConsumptionDay = world.AbsoluteDay;
                claim.InventoryTransactionIds.Add(
                    consumed.InventoryTransactionId);
                claim.Status = claim.RemainingNutritionBasisUnits == 0
                    ? HouseholdReliefConsumptionStatus.Fulfilled
                    : HouseholdReliefConsumptionStatus.PartiallyConsumed;
                result.PeopleRecovered += RecoverAffectedPeople(
                    world, claim, village.LocationId);
                if (claim.Status == HouseholdReliefConsumptionStatus.Fulfilled)
                {
                    result.ClaimsFulfilled++;
                }
                result.ConsumedNutritionBasisUnits = checked(
                    result.ConsumedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits);
                result.ConsumedPhysicalQuantity = checked(
                    result.ConsumedPhysicalQuantity +
                    consumed.ConsumedPhysicalQuantity);
                result.InventoryTransactionIds.Add(
                    consumed.InventoryTransactionId);
                world.VillageLedgerEntries.Add(new VillageLedgerEntryState
                {
                    Id = $"village_ledger.relief_consumption.{claim.Id}." +
                        $"{claim.InventoryTransactionIds.Count:D4}",
                    Day = world.AbsoluteDay,
                    Type = VillageLedgerEntryType.FoodConsumption,
                    VillageId = village.Id,
                    FamilyId = claim.FamilyId,
                    PersonId = consumer.Id,
                    Quantity = (int)Math.Min(
                        int.MaxValue,
                        consumed.ConsumedPhysicalQuantity),
                    Summary = $"{claim.FamilyId} consumed " +
                        $"{consumed.ConsumedPhysicalQuantity} traced relief food units."
                });
            }
            return result;
        }

        private void ResolveCareAwareClaim(
            WorldState world,
            VillageState village,
            HouseholdReliefConsumptionState claim,
            HouseholdReliefPickupState pickup,
            VillageFacilityState storage,
            HouseholdReliefConsumptionResult result)
        {
            if (storage == null)
            {
                return;
            }

            var longTermNutrition = new LongTermNutritionSystem(
                PeopleFor(world));

            var affectedPeople = FindEligibleAffectedPeople(
                world, claim, village.LocationId);
            var madeProgress = false;
            for (var i = 0; i < affectedPeople.Count; i++)
            {
                var affected = affectedPeople[i];
                var actor = ResolveMealActor(
                    world, claim, affected, village.LocationId);
                if (actor == null)
                {
                    continue;
                }

                var outstanding = Math.Max(
                    0L,
                    affected.AllocatedNutritionBasisUnits -
                    affected.ConsumedNutritionBasisUnits);
                if (outstanding <= 0)
                {
                    continue;
                }

                if (claim.PreparedNutritionBasisUnits > 0)
                {
                    var credited = Math.Min(
                        outstanding, claim.PreparedNutritionBasisUnits);
                    claim.PreparedNutritionBasisUnits -= credited;
                    affected.ConsumedNutritionBasisUnits = checked(
                        affected.ConsumedNutritionBasisUnits + credited);
                    longTermNutrition.CreditReliefNutrition(
                        world,
                        claim,
                        affected,
                        credited,
                        string.Empty);
                    outstanding -= credited;
                    RecordCareDelivery(
                        world,
                        claim,
                        affected,
                        actor.Id,
                        credited,
                        HouseholdReliefCareDeliverySourceIds
                            .PreparedNutrition,
                        string.Empty);
                    result.ConsumedPreparedNutritionBasisUnits = checked(
                        result.ConsumedPreparedNutritionBasisUnits +
                        credited);
                    madeProgress = true;
                    claim.LastConsumerPersonId = affected.PersonId;
                    claim.LastConsumptionDay = world.AbsoluteDay;
                }

                if (outstanding <= 0 ||
                    pickup.InventoryTransactionIds.Count == 0 ||
                    !HasLinkedFood(world, claim, pickup, storage.Id))
                {
                    continue;
                }

                var consumed = _foodInventory.ConsumeHouseholdReliefFood(
                    world,
                    claim.FamilyId,
                    storage.Id,
                    actor.Id,
                    affected.PersonId,
                    outstanding,
                    pickup.InventoryTransactionIds,
                    claim.Id);
                if (consumed.ConsumedPhysicalQuantity <= 0)
                {
                    continue;
                }

                var creditedNutrition = Math.Min(
                    outstanding, consumed.ProvidedNutritionBasisUnits);
                affected.ConsumedNutritionBasisUnits = checked(
                    affected.ConsumedNutritionBasisUnits +
                    creditedNutrition);
                longTermNutrition.CreditReliefNutrition(
                    world,
                    claim,
                    affected,
                    creditedNutrition,
                    consumed.InventoryTransactionId);
                claim.PreparedNutritionBasisUnits = checked(
                    claim.PreparedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits -
                    creditedNutrition);
                claim.ConsumedNutritionBasisUnits = checked(
                    claim.ConsumedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits);
                claim.ConsumedPhysicalQuantity = checked(
                    claim.ConsumedPhysicalQuantity +
                    consumed.ConsumedPhysicalQuantity);
                claim.InventoryTransactionIds.Add(
                    consumed.InventoryTransactionId);
                claim.LastConsumerPersonId = affected.PersonId;
                claim.LastConsumptionDay = world.AbsoluteDay;
                RecordCareDelivery(
                    world,
                    claim,
                    affected,
                    actor.Id,
                    creditedNutrition,
                    HouseholdReliefCareDeliverySourceIds
                        .TracedFoodTransaction,
                    consumed.InventoryTransactionId);
                result.ConsumedNutritionBasisUnits = checked(
                    result.ConsumedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits);
                result.ConsumedPhysicalQuantity = checked(
                    result.ConsumedPhysicalQuantity +
                    consumed.ConsumedPhysicalQuantity);
                result.InventoryTransactionIds.Add(
                    consumed.InventoryTransactionId);
                world.VillageLedgerEntries.Add(new VillageLedgerEntryState
                {
                    Id = $"village_ledger.relief_consumption.{claim.Id}." +
                        $"{claim.InventoryTransactionIds.Count:D4}",
                    Day = world.AbsoluteDay,
                    Type = VillageLedgerEntryType.FoodConsumption,
                    VillageId = village.Id,
                    FamilyId = claim.FamilyId,
                    PersonId = actor.Id,
                    Quantity = (int)Math.Min(
                        int.MaxValue,
                        consumed.ConsumedPhysicalQuantity),
                    Summary = affected.RequiresCaregiverDelivery
                        ? $"{actor.Id} delivered traced relief food to " +
                          $"{affected.PersonId}."
                        : $"{affected.PersonId} consumed traced relief food."
                });
                madeProgress = true;
            }

            claim.RemainingNutritionBasisUnits =
                CalculateClaimOutstandingNutrition(claim);
            claim.Status = claim.RemainingNutritionBasisUnits == 0
                ? HouseholdReliefConsumptionStatus.Fulfilled
                : madeProgress
                    ? HouseholdReliefConsumptionStatus.PartiallyConsumed
                    : claim.Status;
            if (madeProgress)
            {
                result.PeopleRecovered += RecoverAffectedPeople(
                    world, claim, village.LocationId);
                if (claim.Status == HouseholdReliefConsumptionStatus.Fulfilled)
                {
                    result.ClaimsFulfilled++;
                }
            }
        }

        private void RecordCareDelivery(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            HouseholdReliefAffectedPersonState affected,
            string actorPersonId,
            long nutritionBasisUnits,
            string sourceKindId,
            string sourceInventoryTransactionId)
        {
            if (!affected.RequiresCaregiverDelivery ||
                nutritionBasisUnits <= 0)
            {
                return;
            }

            var sequence = 1;
            for (var i = 0;
                 i < world.HouseholdReliefCareDeliveries.Count;
                 i++)
            {
                if (world.HouseholdReliefCareDeliveries[i]
                        .HouseholdReliefConsumptionId == claim.Id)
                {
                    sequence++;
                }
            }
            world.HouseholdReliefCareDeliveries.Add(
                new HouseholdReliefCareDeliveryState
                {
                    Id = $"household_relief.care_delivery.{claim.Id}." +
                        $"{sequence:D4}",
                    HouseholdReliefConsumptionId = claim.Id,
                    RecipientPersonId = affected.PersonId,
                    CaregiverPersonId = actorPersonId,
                    Day = world.AbsoluteDay,
                    NutritionBasisUnits = nutritionBasisUnits,
                    SourceKindId = sourceKindId,
                    SourceInventoryTransactionId =
                        sourceInventoryTransactionId
                });
        }

        public static string ConsumptionId(
            long settlementDay,
            string villageId,
            string familyId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.consumption.{0:D10}.{1}.{2}",
                settlementDay,
                villageId,
                familyId);

        private int RecoverAffectedPeople(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            string villageLocationId)
        {
            var recoveredPeople = 0;
            var people = PeopleFor(world);
            for (var i = 0; i < claim.AffectedPeople.Count; i++)
            {
                var affected = claim.AffectedPeople[i];
                var person = people.GetRequiredForUpdate(affected.PersonId);
                if (!person.IsAlive ||
                    person.LocationId != villageLocationId ||
                    person.LocalDuty == LocalDutyKind.Levy)
                {
                    continue;
                }

                var creditedNutrition = IsIndividualAllocation(claim)
                    ? Math.Min(
                        affected.AllocatedNutritionBasisUnits,
                        affected.ConsumedNutritionBasisUnits)
                    : Math.Min(
                        claim.RequestedNutritionBasisUnits,
                        claim.ConsumedNutritionBasisUnits);
                var recoveryNutrition = IsIndividualAllocation(claim)
                    ? affected.AllocatedNutritionBasisUnits
                    : claim.RequestedNutritionBasisUnits;
                if (recoveryNutrition <= 0)
                {
                    continue;
                }

                var targetHealth = ProportionalBasisPoints(
                    affected.AppliedHealthDamageBasisPoints,
                    creditedNutrition,
                    recoveryNutrition);
                var requestedHealth = Math.Max(
                    0, targetHealth - affected.RecoveredHealthBasisPoints);
                var openingHealth = person.HealthBasisPoints;
                person.HealthBasisPoints = Math.Min(
                    10_000,
                    checked(person.HealthBasisPoints + requestedHealth));
                var actualHealth = person.HealthBasisPoints - openingHealth;
                affected.RecoveredHealthBasisPoints = checked(
                    affected.RecoveredHealthBasisPoints + actualHealth);

                var targetLivelihood = ProportionalBasisPoints(
                    affected.AppliedLivelihoodPressureBasisPoints,
                    creditedNutrition,
                    recoveryNutrition);
                var requestedLivelihood = Math.Max(
                    0,
                    targetLivelihood -
                    affected.RecoveredLivelihoodBasisPoints);
                var openingLivelihood = person.Needs.Livelihood;
                person.Needs.Livelihood = Math.Max(
                    0,
                    person.Needs.Livelihood - requestedLivelihood);
                var actualLivelihood =
                    openingLivelihood - person.Needs.Livelihood;
                affected.RecoveredLivelihoodBasisPoints = checked(
                    affected.RecoveredLivelihoodBasisPoints +
                    actualLivelihood);
                if (actualHealth > 0 || actualLivelihood > 0)
                {
                    recoveredPeople++;
                }
            }
            return recoveredPeople;
        }

        private bool HasLinkedFood(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            HouseholdReliefPickupState pickup,
            string storageFacilityId)
        {
            var sourceIds = new HashSet<string>(
                pickup.InventoryTransactionIds, StringComparer.Ordinal);
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == claim.FamilyId &&
                    batch.StorageFacilityId == storageFacilityId &&
                    batch.Quantity > batch.ReservedQuantity &&
                    sourceIds.Contains(batch.SourceTransactionId))
                {
                    return true;
                }
            }
            return false;
        }

        private static int ProportionalBasisPoints(
            int maximum,
            long numerator,
            long denominator)
        {
            if (maximum <= 0 || numerator <= 0)
            {
                return 0;
            }
            var boundedNumerator = Math.Min(numerator, denominator);
            return (int)Math.Min(
                maximum,
                decimal.Floor(
                    (decimal)maximum * boundedNumerator / denominator));
        }

        private List<HouseholdReliefAffectedPersonState>
            FindEligibleAffectedPeople(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            string villageLocationId)
        {
            var people = PeopleFor(world);
            var candidates =
                new List<HouseholdReliefAffectedPersonState>();
            for (var i = 0; i < claim.AffectedPeople.Count; i++)
            {
                var affected = claim.AffectedPeople[i];
                var person = people.GetRequired(affected.PersonId);
                if (person.IsAlive &&
                    person.LocationId == villageLocationId &&
                    person.LocalDuty != LocalDutyKind.Levy &&
                    (!IsIndividualAllocation(claim) ||
                     affected.ConsumedNutritionBasisUnits <
                        affected.AllocatedNutritionBasisUnits))
                {
                    candidates.Add(affected);
                }
            }
            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.PersonId, right.PersonId));
            return candidates;
        }

        private List<HouseholdReliefAffectedPersonState>
            FindServiceableAffectedPeople(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            string villageLocationId)
        {
            var eligible = FindEligibleAffectedPeople(
                world, claim, villageLocationId);
            if (claim.CareDeliveryPolicyId !=
                HouseholdReliefCareDeliveryPolicyIds.AgeHealthDependency)
            {
                return eligible;
            }

            var serviceable =
                new List<HouseholdReliefAffectedPersonState>();
            for (var i = 0; i < eligible.Count; i++)
            {
                if (ResolveMealActor(
                        world,
                        claim,
                        eligible[i],
                        villageLocationId) != null)
                {
                    serviceable.Add(eligible[i]);
                }
            }
            return serviceable;
        }

        private PersonState ResolveMealActor(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            HouseholdReliefAffectedPersonState affected,
            string villageLocationId)
        {
            var people = PeopleFor(world);
            if (!affected.RequiresCaregiverDelivery)
            {
                return people.GetRequired(affected.PersonId);
            }

            var family = world.Families.Find(item => item.Id == claim.FamilyId) ??
                throw new InvalidOperationException(
                    $"Missing relief family {claim.FamilyId}.");
            var candidateIds = new List<string>(family.MemberIds);
            candidateIds.Sort(StringComparer.Ordinal);
            for (var i = 0; i < candidateIds.Count; i++)
            {
                if (candidateIds[i] == affected.PersonId)
                {
                    continue;
                }
                var candidate = people.GetRequired(candidateIds[i]);
                var ageYears = Math.Max(
                    0L, (world.AbsoluteDay - candidate.BirthDay) / 360L);
                if (candidate.IsAlive &&
                    candidate.LocationId == villageLocationId &&
                    candidate.LocalDuty != LocalDutyKind.Levy &&
                    ageYears >= 15L && ageYears <= 60L)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static bool IsIndividualAllocation(
            HouseholdReliefConsumptionState claim) =>
            claim.AllocationPolicyId == HouseholdReliefAllocationPolicyIds
                .ProportionalIndividualNeed;

        private static long CalculateEligibleOutstandingNutrition(
            IList<HouseholdReliefAffectedPersonState> affectedPeople)
        {
            long result = 0;
            for (var i = 0; i < affectedPeople.Count; i++)
            {
                result = checked(
                    result + Math.Max(
                        0L,
                        affectedPeople[i].AllocatedNutritionBasisUnits -
                        affectedPeople[i].ConsumedNutritionBasisUnits));
            }
            return result;
        }

        private static long CalculateClaimOutstandingNutrition(
            HouseholdReliefConsumptionState claim)
        {
            long result = 0;
            for (var i = 0; i < claim.AffectedPeople.Count; i++)
            {
                result = checked(
                    result + Math.Max(
                        0L,
                        claim.AffectedPeople[i]
                            .AllocatedNutritionBasisUnits -
                        claim.AffectedPeople[i]
                            .ConsumedNutritionBasisUnits));
            }
            return result;
        }

        private static long AllocateConsumedNutrition(
            IList<HouseholdReliefAffectedPersonState> eligibleAffected,
            long consumedNutritionBasisUnits)
        {
            var remaining = consumedNutritionBasisUnits;
            for (var i = 0;
                 i < eligibleAffected.Count && remaining > 0;
                 i++)
            {
                var affected = eligibleAffected[i];
                var outstanding = Math.Max(
                    0L,
                    affected.AllocatedNutritionBasisUnits -
                    affected.ConsumedNutritionBasisUnits);
                var credited = Math.Min(outstanding, remaining);
                affected.ConsumedNutritionBasisUnits = checked(
                    affected.ConsumedNutritionBasisUnits + credited);
                remaining -= credited;
            }
            return consumedNutritionBasisUnits - remaining;
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

        private static int CompareClaims(
            HouseholdReliefConsumptionState left,
            HouseholdReliefConsumptionState right)
        {
            var byDay = left.SettlementDay.CompareTo(right.SettlementDay);
            if (byDay != 0)
            {
                return byDay;
            }
            var byVillage = string.CompareOrdinal(
                left.VillageId, right.VillageId);
            return byVillage != 0
                ? byVillage
                : string.CompareOrdinal(left.FamilyId, right.FamilyId);
        }

        private static VillageState FindVillage(
            WorldState world, string villageId) =>
            world.Villages.Find(item => item.Id == villageId) ??
            throw new InvalidOperationException(
                $"Missing village {villageId}.");

        private static HouseholdReliefPickupState FindPickup(
            WorldState world, string pickupId) =>
            world.HouseholdReliefPickups.Find(item => item.Id == pickupId) ??
            throw new InvalidOperationException(
                $"Missing household relief pickup {pickupId}.");

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string villageId,
            string familyId) => world.VillageFacilities.Find(item =>
                item.VillageId == villageId &&
                item.Kind == VillageFacilityKind.HouseholdGranary &&
                item.OwnerFamilyId == familyId);
    }

    public sealed class HouseholdReliefConsumptionCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.household_relief.consume_delivered_food";
        public const string TransactionKindId =
            "mandate.transaction.household_relief.consume_delivered_food";
        public const string EventTypeId =
            "mandate.event.household_relief.delivered_food_consumed";
        public const string ProjectionHandlerId =
            "mandate.handler.household_relief.consumption_projection";
        public const string IssuerId = "system.household_relief_consumption";
        public const string VillageIdArgumentId = "village_id";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string ExpectedSegmentArgumentId = "expected_segment";

        private readonly HouseholdReliefConsumptionSystem _system;

        public HouseholdReliefConsumptionCommandScheduler(
            HouseholdReliefConsumptionSystem system)
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
                if (!_system.HasConsumableWork(world, village.Id) ||
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
                        58,
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
                "household_relief.consumption_command.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        public static string TransactionId(
            long day, byte segment, string villageId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.consumption_transaction.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        public static string EventId(
            long day, byte segment, string villageId) => string.Format(
                CultureInfo.InvariantCulture,
                "household_relief.consumed.{0:D10}.{1}.{2}",
                day,
                segment,
                villageId);

        private sealed class CommandHandler : IWorldCommandHandler
        {
            private readonly HouseholdReliefConsumptionSystem _system;

            public CommandHandler(HouseholdReliefConsumptionSystem system)
            {
                _system = system;
            }

            public string CommandTypeId =>
                HouseholdReliefConsumptionCommandScheduler.CommandTypeId;

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
                        command,
                        ExpectedSegmentArgumentId,
                        out var segment) ||
                    segment > (byte)DaySegment.Night)
                {
                    throw new InvalidOperationException(
                        "Household relief consumption command arguments are invalid.");
                }
                _ = new StableId(villageId);
                transactions.Add(new Transaction(
                    _system, villageId, day, segment));
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly HouseholdReliefConsumptionSystem _system;
            private readonly string _villageId;
            private readonly long _day;
            private readonly byte _segment;

            public Transaction(
                HouseholdReliefConsumptionSystem system,
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
            public int Priority => 58;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.Validate(world, _villageId, _day, _segment);
                validation.Reserve(
                    "household_relief.consumption." + _villageId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var result = _system.Resolve(world, _villageId);
                if (result.ConsumedPhysicalQuantity <= 0 &&
                    result.ConsumedPreparedNutritionBasisUnits <= 0)
                {
                    throw new InvalidOperationException(
                        "A consumable household relief command made no progress.");
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
                HouseholdReliefConsumptionCommandScheduler.EventTypeId;

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
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value);
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
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value);
        }
    }
}
