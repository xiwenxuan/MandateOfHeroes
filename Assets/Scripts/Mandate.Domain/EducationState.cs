using System;

namespace Mandate.Domain
{
    public enum ProfessionalDiscipline : byte
    {
        Military,
        MartialArts,
        Administration,
        Commerce,
        Agriculture,
        Craft,
        Medicine,
        Scholarship,
        Negotiation,
        Intelligence
    }

    public enum EducationFundingSource : byte
    {
        Personal,
        Family
    }

    public enum EducationPlanStatus : byte
    {
        Active,
        Suspended,
        Cancelled,
        Completed
    }

    public enum LearningOutcomeKind : byte
    {
        Completed,
        StudentUnavailable,
        TeacherUnavailable,
        LocationMismatch,
        InsufficientFunds,
        SelfStudyLimit,
        MissingPractice,
        SkillCap
    }

    [Serializable]
    public sealed class EducationPlanState
    {
        public string Id;
        public string StudentPersonId;
        public ProfessionalDiscipline Discipline;
        public int MonthlyStudyDays = 10;
        public string TeacherPersonId;
        public long MonthlyFee;
        public EducationFundingSource FundingSource;
        public string FundingFamilyId;
        public string PracticePositionId;
        public long CreatedDay;
        public long LastResolvedDay = -1;
        public int TotalStudyDays;
        public long TotalFeesPaid;
        public int TotalSkillGain;
        public EducationPlanStatus Status = EducationPlanStatus.Active;
    }

    [Serializable]
    public sealed class LearningRecordState
    {
        public string Id;
        public string EducationPlanId;
        public long Day;
        public long MonthIndex;
        public string StudentPersonId;
        public string TeacherPersonId;
        public ProfessionalDiscipline Discipline;
        public LearningOutcomeKind Outcome;
        public int StudyDays;
        public long FeePaid;
        public int SkillBefore;
        public int SkillAfter;
        public int SkillGain;
        public int CompositeAptitudeBasisPoints;
        public int SoftPotentialBasisPoints;
        public int TeacherFactorBasisPoints;
        public int FacilityFactorBasisPoints;
        public int HealthFactorBasisPoints;
        public int MotivationFactorBasisPoints;
        public int PracticeFactorBasisPoints;
        public int DiminishingFactorBasisPoints;
        public string Summary;
    }

    public static class ProfessionalSkillAccess
    {
        public static int Get(
            ProfessionalSkillState skills,
            ProfessionalDiscipline discipline)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                    return skills.Military;
                case ProfessionalDiscipline.MartialArts:
                    return skills.MartialArts;
                case ProfessionalDiscipline.Administration:
                    return skills.Administration;
                case ProfessionalDiscipline.Commerce:
                    return skills.Commerce;
                case ProfessionalDiscipline.Agriculture:
                    return skills.Agriculture;
                case ProfessionalDiscipline.Craft:
                    return skills.Craft;
                case ProfessionalDiscipline.Medicine:
                    return skills.Medicine;
                case ProfessionalDiscipline.Scholarship:
                    return skills.Scholarship;
                case ProfessionalDiscipline.Negotiation:
                    return skills.Negotiation;
                case ProfessionalDiscipline.Intelligence:
                    return skills.Intelligence;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(discipline), discipline, "Unknown discipline.");
            }
        }

        public static void Set(
            ProfessionalSkillState skills,
            ProfessionalDiscipline discipline,
            int value)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (value < 0 || value > 10_000)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                    skills.Military = value;
                    break;
                case ProfessionalDiscipline.MartialArts:
                    skills.MartialArts = value;
                    break;
                case ProfessionalDiscipline.Administration:
                    skills.Administration = value;
                    break;
                case ProfessionalDiscipline.Commerce:
                    skills.Commerce = value;
                    break;
                case ProfessionalDiscipline.Agriculture:
                    skills.Agriculture = value;
                    break;
                case ProfessionalDiscipline.Craft:
                    skills.Craft = value;
                    break;
                case ProfessionalDiscipline.Medicine:
                    skills.Medicine = value;
                    break;
                case ProfessionalDiscipline.Scholarship:
                    skills.Scholarship = value;
                    break;
                case ProfessionalDiscipline.Negotiation:
                    skills.Negotiation = value;
                    break;
                case ProfessionalDiscipline.Intelligence:
                    skills.Intelligence = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(discipline), discipline, "Unknown discipline.");
            }
        }

        public static int CompositeAptitude(
            CharacterAptitudeState aptitude,
            ProfessionalDiscipline discipline)
        {
            if (aptitude == null)
            {
                throw new ArgumentNullException(nameof(aptitude));
            }

            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                    return Average(
                        aptitude.Reasoning,
                        aptitude.Willpower,
                        aptitude.Perception);
                case ProfessionalDiscipline.MartialArts:
                    return Average(
                        aptitude.Strength,
                        aptitude.Dexterity,
                        aptitude.Constitution);
                case ProfessionalDiscipline.Administration:
                    return Average(
                        aptitude.Reasoning,
                        aptitude.Memory,
                        aptitude.Willpower);
                case ProfessionalDiscipline.Commerce:
                    return Average(
                        aptitude.Perception,
                        aptitude.Memory,
                        aptitude.Affinity);
                case ProfessionalDiscipline.Agriculture:
                    return Average(
                        aptitude.Constitution,
                        aptitude.Perception,
                        aptitude.Willpower);
                case ProfessionalDiscipline.Craft:
                    return Average(
                        aptitude.Dexterity,
                        aptitude.Perception,
                        aptitude.Reasoning);
                case ProfessionalDiscipline.Medicine:
                    return Average(
                        aptitude.Perception,
                        aptitude.Memory,
                        aptitude.Reasoning);
                case ProfessionalDiscipline.Scholarship:
                    return Average(
                        aptitude.Memory,
                        aptitude.Reasoning,
                        aptitude.Willpower);
                case ProfessionalDiscipline.Negotiation:
                    return Average(
                        aptitude.Affinity,
                        aptitude.Perception,
                        aptitude.Willpower);
                case ProfessionalDiscipline.Intelligence:
                    return Average(
                        aptitude.Perception,
                        aptitude.Reasoning,
                        aptitude.Memory);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(discipline), discipline, "Unknown discipline.");
            }
        }

        public static int SoftPotential(
            CharacterAptitudeState aptitude,
            ProfessionalDiscipline discipline)
        {
            return Math.Min(
                10_000,
                2_500 + CompositeAptitude(aptitude, discipline) * 3 / 4);
        }

        public static string DisplayName(ProfessionalDiscipline discipline)
        {
            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                    return "军事";
                case ProfessionalDiscipline.MartialArts:
                    return "武艺";
                case ProfessionalDiscipline.Administration:
                    return "政务";
                case ProfessionalDiscipline.Commerce:
                    return "商业";
                case ProfessionalDiscipline.Agriculture:
                    return "农业";
                case ProfessionalDiscipline.Craft:
                    return "工艺";
                case ProfessionalDiscipline.Medicine:
                    return "医药";
                case ProfessionalDiscipline.Scholarship:
                    return "学问";
                case ProfessionalDiscipline.Negotiation:
                    return "交涉";
                case ProfessionalDiscipline.Intelligence:
                    return "情报";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(discipline), discipline, "Unknown discipline.");
            }
        }

        private static int Average(int first, int second, int third)
        {
            return (first + second + third) / 3;
        }
    }
}
