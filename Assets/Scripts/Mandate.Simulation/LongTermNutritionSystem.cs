using System;
using System.Collections.Generic;
using System.Globalization;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class LongTermNutritionSystem
    {
        private readonly IPersonRepository _people;

        public LongTermNutritionSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public void RecordMonthlySettlement(
            WorldState world,
            long settlementDay,
            IList<FormalHouseholdFoodPersonSettlementResult> settlements)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (settlements == null)
                throw new ArgumentNullException(nameof(settlements));
            if (settlementDay != world.AbsoluteDay ||
                settlementDay <= 0 || settlementDay % 30 != 0)
            {
                throw new InvalidOperationException(
                    "Long-term nutrition settlement is not due.");
            }

            var ordered = new List<FormalHouseholdFoodPersonSettlementResult>(
                settlements);
            ordered.Sort((left, right) => string.CompareOrdinal(
                left.PersonId, right.PersonId));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < ordered.Count; i++)
            {
                var settlement = ordered[i];
                if (settlement == null ||
                    !seen.Add(settlement.PersonId) ||
                    settlement.RequiredNutritionBasisUnits <= 0 ||
                    settlement.MissingNutritionBasisUnits < 0 ||
                    settlement.MissingNutritionBasisUnits >
                        settlement.RequiredNutritionBasisUnits)
                {
                    throw new InvalidOperationException(
                        "Invalid person nutrition settlement.");
                }

                var profile = world.PersonNutritionProfiles.Find(item =>
                    item.PersonId == settlement.PersonId);
                if (settlement.MissingNutritionBasisUnits > 0)
                {
                    profile = profile ?? CreateProfile(
                        world, settlement.PersonId, settlementDay,
                        settlement.RequiredNutritionBasisUnits);
                    RecordDeficit(
                        world, profile, settlementDay,
                        settlement.RequiredNutritionBasisUnits,
                        settlement.MissingNutritionBasisUnits);
                }
                else if (profile != null &&
                    (profile.NutritionDebtBasisUnits > 0 ||
                     !string.IsNullOrEmpty(
                         profile.ActiveConditionEpisodeId)))
                {
                    RecordRecovery(
                        world, profile, settlementDay,
                        settlement.RequiredNutritionBasisUnits);
                }
            }
        }

        public long CreditReliefNutrition(
            WorldState world,
            HouseholdReliefConsumptionState claim,
            HouseholdReliefAffectedPersonState affected,
            long nutritionBasisUnits,
            string sourceInventoryTransactionId)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            if (affected == null)
                throw new ArgumentNullException(nameof(affected));
            if (nutritionBasisUnits <= 0)
                return 0;

            var profile = world.PersonNutritionProfiles.Find(item =>
                item.PersonId == affected.PersonId);
            if (profile == null || profile.NutritionDebtBasisUnits <= 0)
            {
                return 0;
            }

            var credited = Math.Min(
                nutritionBasisUnits, profile.NutritionDebtBasisUnits);
            var openingDebt = profile.NutritionDebtBasisUnits;
            var openingRisk = profile.DiseaseRiskBasisPoints;
            var openingDeficit = profile.ConsecutiveDeficitMonths;
            var openingAdequate = profile.ConsecutiveAdequateMonths;
            profile.NutritionDebtBasisUnits -= credited;
            if (profile.NutritionDebtBasisUnits == 0)
                profile.ConsecutiveDeficitMonths = 0;
            profile.DiseaseRiskBasisPoints = LongTermNutritionRules
                .CalculateDiseaseRiskBasisPoints(
                    profile.NutritionDebtBasisUnits,
                    profile.ReferenceMonthlyNutritionBasisUnits,
                    profile.ConsecutiveDeficitMonths);
            profile.LastUpdatedDay = world.AbsoluteDay;

            var episode = FindActiveEpisode(world, profile);
            if (episode != null)
            {
                episode.LastEvaluatedDay = world.AbsoluteDay;
                episode.PeakDiseaseRiskBasisPoints = Math.Max(
                    episode.PeakDiseaseRiskBasisPoints,
                    profile.DiseaseRiskBasisPoints);
            }

            var entryId = string.Format(
                CultureInfo.InvariantCulture,
                "nutrition.ledger.relief.{0}.{1}.{2:D20}",
                claim.Id,
                affected.PersonId,
                affected.ConsumedNutritionBasisUnits);
            EnsureLedgerIdAvailable(world, entryId);
            world.PersonNutritionLedgerEntries.Add(
                new PersonNutritionLedgerEntryState
                {
                    Id = entryId,
                    PersonId = affected.PersonId,
                    PolicyId = profile.PolicyId,
                    Kind = NutritionLedgerEntryKind.ReliefNutritionCredit,
                    Day = world.AbsoluteDay,
                    ReferenceMonthlyNutritionBasisUnits =
                        profile.ReferenceMonthlyNutritionBasisUnits,
                    NutritionBasisUnits = credited,
                    OpeningNutritionDebtBasisUnits = openingDebt,
                    ClosingNutritionDebtBasisUnits =
                        profile.NutritionDebtBasisUnits,
                    OpeningDiseaseRiskBasisPoints = openingRisk,
                    ClosingDiseaseRiskBasisPoints =
                        profile.DiseaseRiskBasisPoints,
                    OpeningConsecutiveDeficitMonths = openingDeficit,
                    ClosingConsecutiveDeficitMonths =
                        profile.ConsecutiveDeficitMonths,
                    OpeningConsecutiveAdequateMonths = openingAdequate,
                    ClosingConsecutiveAdequateMonths =
                        profile.ConsecutiveAdequateMonths,
                    ConditionEpisodeId = episode?.Id ?? string.Empty,
                    SourceHouseholdReliefConsumptionId = claim.Id,
                    SourceInventoryTransactionId =
                        sourceInventoryTransactionId ?? string.Empty
                });
            return credited;
        }

        private void RecordDeficit(
            WorldState world,
            PersonNutritionProfileState profile,
            long day,
            long requiredNutrition,
            long missingNutrition)
        {
            var entryId = MonthlyEntryId(day, profile.PersonId);
            EnsureLedgerIdAvailable(world, entryId);
            var openingDebt = profile.NutritionDebtBasisUnits;
            var openingRisk = profile.DiseaseRiskBasisPoints;
            var openingDeficit = profile.ConsecutiveDeficitMonths;
            var openingAdequate = profile.ConsecutiveAdequateMonths;

            profile.ReferenceMonthlyNutritionBasisUnits = requiredNutrition;
            profile.NutritionDebtBasisUnits = checked(
                profile.NutritionDebtBasisUnits + missingNutrition);
            profile.ConsecutiveDeficitMonths++;
            profile.ConsecutiveAdequateMonths = 0;
            profile.DiseaseRiskBasisPoints = LongTermNutritionRules
                .CalculateDiseaseRiskBasisPoints(
                    profile.NutritionDebtBasisUnits,
                    requiredNutrition,
                    profile.ConsecutiveDeficitMonths);
            profile.LastUpdatedDay = day;

            var episode = FindActiveEpisode(world, profile);
            if (episode == null &&
                profile.ConsecutiveDeficitMonths >=
                    LongTermNutritionRules.IllnessDeficitMonthThreshold &&
                profile.DiseaseRiskBasisPoints >=
                    LongTermNutritionRules
                        .IllnessRiskThresholdBasisPoints)
            {
                episode = new NutritionConditionEpisodeState
                {
                    Id = string.Format(
                        CultureInfo.InvariantCulture,
                        "nutrition.condition_episode.{0}.{1:D10}",
                        profile.PersonId,
                        day),
                    PersonId = profile.PersonId,
                    PolicyId = profile.PolicyId,
                    ConditionId =
                        NutritionConditionIds.MalnutritionIllness,
                    StartDay = day,
                    LastEvaluatedDay = day,
                    PeakDiseaseRiskBasisPoints =
                        profile.DiseaseRiskBasisPoints
                };
                if (world.NutritionConditionEpisodes.Exists(item =>
                        item.Id == episode.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate nutrition condition episode {episode.Id}.");
                }
                world.NutritionConditionEpisodes.Add(episode);
                profile.ActiveConditionEpisodeId = episode.Id;
            }

            var healthDelta = 0;
            if (episode != null)
            {
                var person = PeopleFor(world).GetRequiredForUpdate(
                    profile.PersonId);
                var requestedDamage = LongTermNutritionRules
                    .CalculateIllnessHealthDamageBasisPoints(
                        profile.DiseaseRiskBasisPoints);
                var appliedDamage = Math.Min(
                    person.HealthBasisPoints, requestedDamage);
                person.HealthBasisPoints -= appliedDamage;
                healthDelta = -appliedDamage;
                episode.AppliedHealthDamageBasisPoints = checked(
                    episode.AppliedHealthDamageBasisPoints + appliedDamage);
                episode.PeakDiseaseRiskBasisPoints = Math.Max(
                    episode.PeakDiseaseRiskBasisPoints,
                    profile.DiseaseRiskBasisPoints);
                episode.LastEvaluatedDay = day;
            }

            world.PersonNutritionLedgerEntries.Add(
                BuildEntry(
                    entryId,
                    profile,
                    NutritionLedgerEntryKind.MonthlyDeficit,
                    day,
                    missingNutrition,
                    openingDebt,
                    openingRisk,
                    openingDeficit,
                    openingAdequate,
                    healthDelta,
                    episode?.Id ?? string.Empty));
        }

        private void RecordRecovery(
            WorldState world,
            PersonNutritionProfileState profile,
            long day,
            long requiredNutrition)
        {
            var entryId = MonthlyEntryId(day, profile.PersonId);
            EnsureLedgerIdAvailable(world, entryId);
            var openingDebt = profile.NutritionDebtBasisUnits;
            var openingRisk = profile.DiseaseRiskBasisPoints;
            var openingDeficit = profile.ConsecutiveDeficitMonths;
            var openingAdequate = profile.ConsecutiveAdequateMonths;
            var recoveredNutrition = Math.Min(
                openingDebt, Math.Max(1L, requiredNutrition / 2L));

            profile.ReferenceMonthlyNutritionBasisUnits = requiredNutrition;
            profile.NutritionDebtBasisUnits -= recoveredNutrition;
            profile.ConsecutiveDeficitMonths = 0;
            profile.ConsecutiveAdequateMonths++;
            profile.DiseaseRiskBasisPoints = LongTermNutritionRules
                .CalculateDiseaseRiskBasisPoints(
                    profile.NutritionDebtBasisUnits,
                    requiredNutrition,
                    profile.ConsecutiveDeficitMonths);
            profile.LastUpdatedDay = day;

            var episode = FindActiveEpisode(world, profile);
            var healthDelta = 0;
            if (episode != null)
            {
                var remainingDamage = Math.Max(
                    0,
                    episode.AppliedHealthDamageBasisPoints -
                    episode.RecoveredHealthBasisPoints);
                var person = PeopleFor(world).GetRequiredForUpdate(
                    profile.PersonId);
                healthDelta = Math.Min(
                    LongTermNutritionRules.MonthlyHealthRecoveryBasisPoints,
                    Math.Min(remainingDamage,
                        10_000 - person.HealthBasisPoints));
                person.HealthBasisPoints += healthDelta;
                episode.RecoveredHealthBasisPoints = checked(
                    episode.RecoveredHealthBasisPoints + healthDelta);
                episode.PeakDiseaseRiskBasisPoints = Math.Max(
                    episode.PeakDiseaseRiskBasisPoints,
                    profile.DiseaseRiskBasisPoints);
                episode.LastEvaluatedDay = day;
                if (profile.NutritionDebtBasisUnits == 0 &&
                    profile.ConsecutiveAdequateMonths >=
                        LongTermNutritionRules
                            .ResolutionAdequateMonthThreshold)
                {
                    episode.EndDay = day;
                    profile.ActiveConditionEpisodeId = string.Empty;
                }
            }

            world.PersonNutritionLedgerEntries.Add(
                BuildEntry(
                    entryId,
                    profile,
                    NutritionLedgerEntryKind.MonthlyRecovery,
                    day,
                    recoveredNutrition,
                    openingDebt,
                    openingRisk,
                    openingDeficit,
                    openingAdequate,
                    healthDelta,
                    episode?.Id ?? string.Empty));
        }

        private static PersonNutritionLedgerEntryState BuildEntry(
            string id,
            PersonNutritionProfileState profile,
            NutritionLedgerEntryKind kind,
            long day,
            long nutrition,
            long openingDebt,
            int openingRisk,
            int openingDeficit,
            int openingAdequate,
            int healthDelta,
            string episodeId)
        {
            return new PersonNutritionLedgerEntryState
            {
                Id = id,
                PersonId = profile.PersonId,
                PolicyId = profile.PolicyId,
                Kind = kind,
                Day = day,
                ReferenceMonthlyNutritionBasisUnits =
                    profile.ReferenceMonthlyNutritionBasisUnits,
                NutritionBasisUnits = nutrition,
                OpeningNutritionDebtBasisUnits = openingDebt,
                ClosingNutritionDebtBasisUnits =
                    profile.NutritionDebtBasisUnits,
                OpeningDiseaseRiskBasisPoints = openingRisk,
                ClosingDiseaseRiskBasisPoints =
                    profile.DiseaseRiskBasisPoints,
                OpeningConsecutiveDeficitMonths = openingDeficit,
                ClosingConsecutiveDeficitMonths =
                    profile.ConsecutiveDeficitMonths,
                OpeningConsecutiveAdequateMonths = openingAdequate,
                ClosingConsecutiveAdequateMonths =
                    profile.ConsecutiveAdequateMonths,
                HealthBasisPointsDelta = healthDelta,
                ConditionEpisodeId = episodeId,
                SourceHouseholdReliefConsumptionId = string.Empty,
                SourceInventoryTransactionId = string.Empty
            };
        }

        private static PersonNutritionProfileState CreateProfile(
            WorldState world,
            string personId,
            long day,
            long referenceNutrition)
        {
            var profile = new PersonNutritionProfileState
            {
                Id = "nutrition.profile." + personId,
                PersonId = personId,
                PolicyId = NutritionPolicyIds
                    .LongitudinalHouseholdNutrition,
                FirstObservedDay = day,
                LastUpdatedDay = day,
                ReferenceMonthlyNutritionBasisUnits = referenceNutrition,
                ActiveConditionEpisodeId = string.Empty
            };
            world.PersonNutritionProfiles.Add(profile);
            return profile;
        }

        private static NutritionConditionEpisodeState FindActiveEpisode(
            WorldState world,
            PersonNutritionProfileState profile)
        {
            if (string.IsNullOrEmpty(profile.ActiveConditionEpisodeId))
                return null;
            return world.NutritionConditionEpisodes.Find(item =>
                item.Id == profile.ActiveConditionEpisodeId) ??
                throw new InvalidOperationException(
                    $"Missing nutrition episode {profile.ActiveConditionEpisodeId}.");
        }

        private static string MonthlyEntryId(long day, string personId) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "nutrition.ledger.monthly.{0:D10}.{1}",
                day,
                personId);

        private static void EnsureLedgerIdAvailable(
            WorldState world, string id)
        {
            if (world.PersonNutritionLedgerEntries.Exists(item =>
                    item.Id == id))
            {
                throw new InvalidOperationException(
                    $"Duplicate nutrition ledger entry {id}.");
            }
        }

        private IPersonRepository PeopleFor(WorldState world) =>
            _people ?? new WorldStatePersonRepository(world);
    }
}
