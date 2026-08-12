using System;

namespace Mandate.Domain
{
    public static class NutritionPolicyIds
    {
        public const string LongitudinalHouseholdNutrition =
            "mandate.nutrition.longitudinal_household.v1";
    }

    public static class NutritionConditionIds
    {
        public const string MalnutritionIllness =
            "mandate.condition.malnutrition_illness.v1";
    }

    public enum NutritionLedgerEntryKind : byte
    {
        MonthlyDeficit,
        MonthlyRecovery,
        ReliefNutritionCredit
    }

    [Serializable]
    public sealed class PersonNutritionProfileState
    {
        public string Id;
        public string PersonId;
        public string PolicyId;
        public long FirstObservedDay;
        public long LastUpdatedDay;
        public long ReferenceMonthlyNutritionBasisUnits;
        public long NutritionDebtBasisUnits;
        public int DiseaseRiskBasisPoints;
        public int ConsecutiveDeficitMonths;
        public int ConsecutiveAdequateMonths;
        public string ActiveConditionEpisodeId;
    }

    [Serializable]
    public sealed class PersonNutritionLedgerEntryState
    {
        public string Id;
        public string PersonId;
        public string PolicyId;
        public NutritionLedgerEntryKind Kind;
        public long Day;
        public long ReferenceMonthlyNutritionBasisUnits;
        public long NutritionBasisUnits;
        public long OpeningNutritionDebtBasisUnits;
        public long ClosingNutritionDebtBasisUnits;
        public int OpeningDiseaseRiskBasisPoints;
        public int ClosingDiseaseRiskBasisPoints;
        public int OpeningConsecutiveDeficitMonths;
        public int ClosingConsecutiveDeficitMonths;
        public int OpeningConsecutiveAdequateMonths;
        public int ClosingConsecutiveAdequateMonths;
        public int HealthBasisPointsDelta;
        public string ConditionEpisodeId;
        public string SourceHouseholdReliefConsumptionId;
        public string SourceInventoryTransactionId;
    }

    [Serializable]
    public sealed class NutritionConditionEpisodeState
    {
        public string Id;
        public string PersonId;
        public string PolicyId;
        public string ConditionId;
        public long StartDay;
        public long LastEvaluatedDay;
        public long EndDay = -1;
        public int PeakDiseaseRiskBasisPoints;
        public int AppliedHealthDamageBasisPoints;
        public int RecoveredHealthBasisPoints;
    }

    public static class LongTermNutritionRules
    {
        public const int IllnessRiskThresholdBasisPoints = 5_000;
        public const int IllnessDeficitMonthThreshold = 2;
        public const int ResolutionAdequateMonthThreshold = 2;
        public const int MonthlyHealthRecoveryBasisPoints = 200;

        public static int CalculateDiseaseRiskBasisPoints(
            long nutritionDebtBasisUnits,
            long referenceMonthlyNutritionBasisUnits,
            int consecutiveDeficitMonths)
        {
            if (nutritionDebtBasisUnits <= 0 ||
                referenceMonthlyNutritionBasisUnits <= 0)
            {
                return 0;
            }

            var debtRisk = nutritionDebtBasisUnits * 7_000L /
                Math.Max(1L, referenceMonthlyNutritionBasisUnits * 2L);
            var streakRisk = Math.Max(0, consecutiveDeficitMonths - 1) *
                1_500L;
            return (int)Math.Min(10_000L, debtRisk + streakRisk);
        }

        public static int CalculateIllnessHealthDamageBasisPoints(
            int diseaseRiskBasisPoints)
        {
            return Math.Max(100, diseaseRiskBasisPoints / 20);
        }
    }
}
