using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class EducationSystem
    {
        public const int DaysPerStudyMonth = 30;
        public const int MaximumStudyDaysPerMonth = 20;
        public const int SelfStudyLimitBasisPoints = 3_000;
        public const int ExpertPracticeThresholdBasisPoints = 6_000;
        public const int PhaseSkillCapBasisPoints = 8_000;
        public const int MaximumStudentsPerTeacher = 3;
        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public EducationSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public EducationPlanState StartPlan(
            WorldState world,
            StableId studentId,
            ProfessionalDiscipline discipline,
            int monthlyStudyDays,
            string teacherPersonId = "",
            EducationFundingSource fundingSource =
                EducationFundingSource.Personal,
            string fundingFamilyId = "",
            string practicePositionId = "")
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            if (!Enum.IsDefined(typeof(ProfessionalDiscipline), discipline))
            {
                throw new ArgumentOutOfRangeException(nameof(discipline));
            }

            if (monthlyStudyDays < 1 ||
                monthlyStudyDays > MaximumStudyDaysPerMonth)
            {
                throw new ArgumentOutOfRangeException(nameof(monthlyStudyDays));
            }

            if (!Enum.IsDefined(typeof(EducationFundingSource), fundingSource))
            {
                throw new ArgumentOutOfRangeException(nameof(fundingSource));
            }

            var student = FindPerson(world, studentId.Value);
            if (!student.IsAlive)
            {
                throw new InvalidOperationException("去世人物不能建立学习计划。");
            }

            if (FindActivePlan(world, student.Id) != null)
            {
                throw new InvalidOperationException(
                    "一名人物同时只能执行一个学习计划。");
            }

            var currentSkill = ProfessionalSkillAccess.Get(
                student.ProfessionalSkills, discipline);
            if (currentSkill >= PhaseSkillCapBasisPoints)
            {
                throw new InvalidOperationException(
                    "该专业已经达到当前培养阶段上限。");
            }

            PersonState teacher = null;
            if (!string.IsNullOrEmpty(teacherPersonId))
            {
                teacher = FindPerson(world, teacherPersonId);
                ValidateTeacher(world, student, teacher, discipline);
                if (CountActiveStudents(world, teacher.Id) >=
                    MaximumStudentsPerTeacher)
                {
                    throw new InvalidOperationException("该教师本月已无更多指导名额。");
                }
            }
            else if (currentSkill >= SelfStudyLimitBasisPoints)
            {
                throw new InvalidOperationException(
                    "熟练度达到30后必须由更高水平教师指导。");
            }

            FamilyState fundingFamily = null;
            if (fundingSource == EducationFundingSource.Family)
            {
                fundingFamily = FindFamily(world, fundingFamilyId);
                if (!fundingFamily.MemberIds.Contains(student.Id))
                {
                    throw new InvalidOperationException(
                        "学习费用只能由学生所属家庭支付。");
                }
            }
            else if (!string.IsNullOrEmpty(fundingFamilyId))
            {
                throw new InvalidOperationException(
                    "个人支付计划不能指定家庭资金账户。");
            }

            if (!string.IsNullOrEmpty(practicePositionId))
            {
                ValidatePracticePosition(
                    world, student, practicePositionId, discipline);
            }
            else if (currentSkill >= ExpertPracticeThresholdBasisPoints)
            {
                throw new InvalidOperationException(
                    "熟练度达到60后必须具有匹配的实践职位。");
            }

            var plan = new EducationPlanState
            {
                Id =
                    $"education_plan.{student.Id}.{world.EducationPlans.Count}",
                StudentPersonId = student.Id,
                Discipline = discipline,
                MonthlyStudyDays = monthlyStudyDays,
                TeacherPersonId = teacher == null ? string.Empty : teacher.Id,
                MonthlyFee = teacher == null
                    ? 0
                    : RecommendedMonthlyFee(
                        ProfessionalSkillAccess.Get(
                            teacher.ProfessionalSkills, discipline),
                        monthlyStudyDays),
                FundingSource = fundingSource,
                FundingFamilyId =
                    fundingFamily == null ? string.Empty : fundingFamily.Id,
                PracticePositionId = practicePositionId ?? string.Empty,
                CreatedDay = world.AbsoluteDay,
                LastResolvedDay = -1,
                Status = EducationPlanStatus.Active
            };
            world.EducationPlans.Add(plan);
            world.Validate();
            return plan;
        }

        public void CancelPlan(WorldState world, StableId planId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var plan = FindPlan(world, planId.Value);
            if (plan.Status != EducationPlanStatus.Active &&
                plan.Status != EducationPlanStatus.Suspended)
            {
                throw new InvalidOperationException("该学习计划已经结束。");
            }

            plan.Status = EducationPlanStatus.Cancelled;
            world.Validate();
        }

        public void ResolveDuePlans(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var plans = new List<EducationPlanState>(world.EducationPlans);
            plans.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                if (plan.Status != EducationPlanStatus.Active)
                {
                    continue;
                }

                var dueDay = plan.LastResolvedDay < 0
                    ? plan.CreatedDay + DaysPerStudyMonth
                    : plan.LastResolvedDay + DaysPerStudyMonth;
                if (world.AbsoluteDay < dueDay)
                {
                    continue;
                }

                ResolvePlan(world, plan);
                plan.LastResolvedDay = world.AbsoluteDay;
            }
        }

        public PersonState FindBestTeacher(
            WorldState world,
            string studentPersonId,
            ProfessionalDiscipline discipline)
        {
            var student = FindPerson(world, studentPersonId);
            var studentSkill = ProfessionalSkillAccess.Get(
                student.ProfessionalSkills, discipline);
            PersonState best = null;
            var bestSkill = -1;
            var people = PeopleFor(world).GetKnownPeople();
            for (var i = 0; i < people.Count; i++)
            {
                var candidate = people[i];
                if (candidate.Id == student.Id ||
                    !candidate.IsAlive ||
                    candidate.LocationId != student.LocationId ||
                    IsTraveling(world, candidate.Id) ||
                    CountActiveStudents(world, candidate.Id) >=
                    MaximumStudentsPerTeacher)
                {
                    continue;
                }

                var candidateSkill = ProfessionalSkillAccess.Get(
                    candidate.ProfessionalSkills, discipline);
                if (candidateSkill < SelfStudyLimitBasisPoints ||
                    candidateSkill <= studentSkill)
                {
                    continue;
                }

                if (candidateSkill > bestSkill ||
                    candidateSkill == bestSkill &&
                    best != null &&
                    string.CompareOrdinal(candidate.Id, best.Id) < 0)
                {
                    best = candidate;
                    bestSkill = candidateSkill;
                }
            }

            return best;
        }

        public string FindCompatiblePracticePosition(
            WorldState world,
            string studentPersonId,
            ProfessionalDiscipline discipline)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId != studentPersonId)
                {
                    continue;
                }

                var position = FindPosition(world, membership.PositionId);
                var organization = FindOrganization(
                    world, position.OrganizationId);
                if (IsCompatiblePractice(organization.Type, discipline))
                {
                    return position.Id;
                }
            }

            return string.Empty;
        }

        public static long RecommendedMonthlyFee(
            int teacherSkillBasisPoints,
            int monthlyStudyDays)
        {
            if (teacherSkillBasisPoints < 0 ||
                teacherSkillBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teacherSkillBasisPoints));
            }

            if (monthlyStudyDays < 1 ||
                monthlyStudyDays > MaximumStudyDaysPerMonth)
            {
                throw new ArgumentOutOfRangeException(nameof(monthlyStudyDays));
            }

            return checked(
                monthlyStudyDays * (5L + teacherSkillBasisPoints / 1_000L));
        }

        private void ResolvePlan(
            WorldState world,
            EducationPlanState plan)
        {
            var student = FindPerson(world, plan.StudentPersonId);
            var skillBefore = ProfessionalSkillAccess.Get(
                student.ProfessionalSkills, plan.Discipline);
            if (!student.IsAlive ||
                student.HealthBasisPoints < 2_500 ||
                IsTraveling(world, student.Id))
            {
                AddFailure(
                    world,
                    plan,
                    student,
                    skillBefore,
                    LearningOutcomeKind.StudentUnavailable,
                    "学生死亡、重病或正在旅行，本期无法完成学习。");
                if (!student.IsAlive)
                {
                    plan.Status = EducationPlanStatus.Completed;
                }

                return;
            }

            if (skillBefore >= PhaseSkillCapBasisPoints)
            {
                plan.Status = EducationPlanStatus.Completed;
                AddFailure(
                    world,
                    plan,
                    student,
                    skillBefore,
                    LearningOutcomeKind.SkillCap,
                    "专业能力已经达到当前培养阶段上限。");
                return;
            }

            PersonState teacher = null;
            var teacherFactor = 6_500;
            if (!string.IsNullOrEmpty(plan.TeacherPersonId))
            {
                teacher = FindPerson(world, plan.TeacherPersonId);
                if (!teacher.IsAlive || IsTraveling(world, teacher.Id))
                {
                    plan.Status = EducationPlanStatus.Suspended;
                    AddFailure(
                        world,
                        plan,
                        student,
                        skillBefore,
                        LearningOutcomeKind.TeacherUnavailable,
                        "教师已经死亡或无法授课，计划暂停。");
                    return;
                }

                if (teacher.LocationId != student.LocationId)
                {
                    AddFailure(
                        world,
                        plan,
                        student,
                        skillBefore,
                        LearningOutcomeKind.LocationMismatch,
                        "师生不在同一地点，本期无法授课。");
                    return;
                }

                var teacherSkill = ProfessionalSkillAccess.Get(
                    teacher.ProfessionalSkills, plan.Discipline);
                if (teacherSkill <= skillBefore ||
                    teacherSkill < SelfStudyLimitBasisPoints)
                {
                    plan.Status = EducationPlanStatus.Suspended;
                    AddFailure(
                        world,
                        plan,
                        student,
                        skillBefore,
                        LearningOutcomeKind.TeacherUnavailable,
                        "教师已不能继续指导当前阶段，计划暂停。");
                    return;
                }

                teacherFactor = Clamp(
                    8_500 + (teacherSkill - skillBefore) / 2,
                    8_500,
                    12_000);
            }
            else if (skillBefore >= SelfStudyLimitBasisPoints)
            {
                plan.Status = EducationPlanStatus.Completed;
                AddFailure(
                    world,
                    plan,
                    student,
                    skillBefore,
                    LearningOutcomeKind.SelfStudyLimit,
                    "自修已达到熟练阶段上限，需要寻找教师。");
                return;
            }

            var hasPractice = HasValidPractice(world, plan, student);
            if (skillBefore >= ExpertPracticeThresholdBasisPoints &&
                !hasPractice)
            {
                AddFailure(
                    world,
                    plan,
                    student,
                    skillBefore,
                    LearningOutcomeKind.MissingPractice,
                    "专家阶段必须在匹配职位中持续实践。");
                return;
            }

            if (!CanPay(world, plan, student))
            {
                AddFailure(
                    world,
                    plan,
                    student,
                    skillBefore,
                    LearningOutcomeKind.InsufficientFunds,
                    "本期学习资金不足，未支付费用也未获得成长。");
                return;
            }

            student = PeopleFor(world).GetRequiredForUpdate(student.Id);
            if (teacher != null && plan.MonthlyFee > 0)
            {
                teacher = PeopleFor(world).GetRequiredForUpdate(teacher.Id);
            }

            PayFee(world, plan, student, teacher);
            var aptitude = ProfessionalSkillAccess.CompositeAptitude(
                student.Aptitudes, plan.Discipline);
            var softPotential = ProfessionalSkillAccess.SoftPotential(
                student.Aptitudes, plan.Discipline);
            var facilityFactor = FacilityFactor(
                FindLocation(world, student.LocationId), plan.Discipline);
            var healthFactor = 5_000 + student.HealthBasisPoints / 2;
            var motivationFactor = MotivationFactor(student, plan.Discipline);
            var practiceFactor = hasPractice
                ? 12_000
                : skillBefore >= SelfStudyLimitBasisPoints ? 7_000 : 10_000;
            var diminishingFactor = DiminishingFactor(
                skillBefore, softPotential);
            long gain = plan.MonthlyStudyDays * 25L;
            gain = ApplyFactor(gain, teacherFactor);
            gain = ApplyFactor(gain, facilityFactor);
            gain = ApplyFactor(gain, 7_500 + aptitude / 2);
            gain = ApplyFactor(gain, healthFactor);
            gain = ApplyFactor(gain, motivationFactor);
            gain = ApplyFactor(gain, practiceFactor);
            gain = ApplyFactor(gain, diminishingFactor);
            var skillGain = (int)Math.Max(1, gain);
            var planSkillCap = teacher == null
                ? SelfStudyLimitBasisPoints
                : PhaseSkillCapBasisPoints;
            var skillAfter = Math.Min(planSkillCap, skillBefore + skillGain);
            skillGain = skillAfter - skillBefore;
            ProfessionalSkillAccess.Set(
                student.ProfessionalSkills, plan.Discipline, skillAfter);
            if (plan.Discipline == ProfessionalDiscipline.Medicine)
            {
                student.MedicalSkillBasisPoints = Math.Max(
                    student.MedicalSkillBasisPoints,
                    skillAfter);
            }

            plan.TotalStudyDays = checked(
                plan.TotalStudyDays + plan.MonthlyStudyDays);
            plan.TotalFeesPaid = checked(
                plan.TotalFeesPaid + plan.MonthlyFee);
            plan.TotalSkillGain = checked(plan.TotalSkillGain + skillGain);
            if (skillAfter >= planSkillCap)
            {
                plan.Status = EducationPlanStatus.Completed;
            }

            world.LearningRecords.Add(new LearningRecordState
            {
                Id = RecordId(world, plan),
                EducationPlanId = plan.Id,
                Day = world.AbsoluteDay,
                MonthIndex = world.AbsoluteDay / DaysPerStudyMonth,
                StudentPersonId = student.Id,
                TeacherPersonId = teacher == null ? string.Empty : teacher.Id,
                Discipline = plan.Discipline,
                Outcome = LearningOutcomeKind.Completed,
                StudyDays = plan.MonthlyStudyDays,
                FeePaid = plan.MonthlyFee,
                SkillBefore = skillBefore,
                SkillAfter = skillAfter,
                SkillGain = skillGain,
                CompositeAptitudeBasisPoints = aptitude,
                SoftPotentialBasisPoints = softPotential,
                TeacherFactorBasisPoints = teacherFactor,
                FacilityFactorBasisPoints = facilityFactor,
                HealthFactorBasisPoints = healthFactor,
                MotivationFactorBasisPoints = motivationFactor,
                PracticeFactorBasisPoints = practiceFactor,
                DiminishingFactorBasisPoints = diminishingFactor,
                Summary =
                    $"{student.DisplayName}用{plan.MonthlyStudyDays}日学习" +
                    $"{ProfessionalSkillAccess.DisplayName(plan.Discipline)}，" +
                    $"能力由{skillBefore}提升至{skillAfter}。"
            });
        }

        private static void AddFailure(
            WorldState world,
            EducationPlanState plan,
            PersonState student,
            int currentSkill,
            LearningOutcomeKind outcome,
            string summary)
        {
            world.LearningRecords.Add(new LearningRecordState
            {
                Id = RecordId(world, plan),
                EducationPlanId = plan.Id,
                Day = world.AbsoluteDay,
                MonthIndex = world.AbsoluteDay / DaysPerStudyMonth,
                StudentPersonId = student.Id,
                TeacherPersonId = plan.TeacherPersonId,
                Discipline = plan.Discipline,
                Outcome = outcome,
                SkillBefore = currentSkill,
                SkillAfter = currentSkill,
                Summary = summary
            });
        }

        private static string RecordId(
            WorldState world,
            EducationPlanState plan)
        {
            return
                $"learning_record.{world.AbsoluteDay}.{plan.Id}." +
                $"{world.LearningRecords.Count}";
        }

        private static bool CanPay(
            WorldState world,
            EducationPlanState plan,
            PersonState student)
        {
            if (plan.MonthlyFee <= 0)
            {
                return true;
            }

            if (plan.FundingSource == EducationFundingSource.Personal)
            {
                return student.Wealth >= plan.MonthlyFee;
            }

            return FindFamily(world, plan.FundingFamilyId).Wealth >=
                plan.MonthlyFee;
        }

        private static void PayFee(
            WorldState world,
            EducationPlanState plan,
            PersonState student,
            PersonState teacher)
        {
            if (plan.MonthlyFee <= 0)
            {
                return;
            }

            if (plan.FundingSource == EducationFundingSource.Personal)
            {
                student.Wealth -= plan.MonthlyFee;
            }
            else
            {
                FindFamily(world, plan.FundingFamilyId).Wealth -=
                    plan.MonthlyFee;
            }

            if (teacher != null)
            {
                teacher.Wealth = checked(teacher.Wealth + plan.MonthlyFee);
            }
        }

        private static int FacilityFactor(
            LocationState location,
            ProfessionalDiscipline discipline)
        {
            LocationFeature relevant;
            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                case ProfessionalDiscipline.MartialArts:
                    relevant = LocationFeature.Garrison;
                    break;
                case ProfessionalDiscipline.Administration:
                case ProfessionalDiscipline.Scholarship:
                    relevant = LocationFeature.Government | LocationFeature.Temple;
                    break;
                case ProfessionalDiscipline.Commerce:
                    relevant = LocationFeature.Market;
                    break;
                case ProfessionalDiscipline.Agriculture:
                    relevant = LocationFeature.Farmland;
                    break;
                case ProfessionalDiscipline.Craft:
                    relevant = LocationFeature.Workshop;
                    break;
                case ProfessionalDiscipline.Medicine:
                    relevant = LocationFeature.Clinic;
                    break;
                case ProfessionalDiscipline.Negotiation:
                    relevant = LocationFeature.Government | LocationFeature.Market;
                    break;
                case ProfessionalDiscipline.Intelligence:
                    relevant =
                        LocationFeature.Garrison |
                        LocationFeature.RelayStation |
                        LocationFeature.Government;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(discipline));
            }

            return (location.Features & relevant) != 0 ? 11_500 : 9_000;
        }

        private static int MotivationFactor(
            PersonState student,
            ProfessionalDiscipline discipline)
        {
            var factor = 8_500 + student.Aptitudes.Willpower / 4;
            if (GoalSupports(student.LifeGoal, discipline))
            {
                factor += 1_000;
            }

            return Clamp(factor, 8_500, 12_000);
        }

        private static bool GoalSupports(
            LifeGoalKind goal,
            ProfessionalDiscipline discipline)
        {
            switch (goal)
            {
                case LifeGoalKind.WinMerit:
                case LifeGoalKind.UnifyRealm:
                    return
                        discipline == ProfessionalDiscipline.Military ||
                        discipline == ProfessionalDiscipline.MartialArts;
                case LifeGoalKind.BuildFortune:
                    return discipline == ProfessionalDiscipline.Commerce;
                case LifeGoalKind.HealThePeople:
                    return discipline == ProfessionalDiscipline.Medicine;
                case LifeGoalKind.PassOnCraft:
                    return
                        discipline == ProfessionalDiscipline.Craft ||
                        discipline == ProfessionalDiscipline.Agriculture;
                case LifeGoalKind.RestoreOrder:
                    return
                        discipline == ProfessionalDiscipline.Administration ||
                        discipline == ProfessionalDiscipline.Negotiation;
                case LifeGoalKind.SeekKnowledge:
                    return
                        discipline == ProfessionalDiscipline.Scholarship ||
                        discipline == ProfessionalDiscipline.Intelligence;
                default:
                    return false;
            }
        }

        private static int DiminishingFactor(
            int currentSkill,
            int softPotential)
        {
            if (currentSkill < softPotential)
            {
                return Clamp(
                    10_000 - currentSkill * 3_000 / Math.Max(1, softPotential),
                    7_000,
                    10_000);
            }

            var remaining = Math.Max(1, 10_000 - softPotential);
            return Clamp(
                4_000 -
                (currentSkill - softPotential) * 2_500 / remaining,
                1_000,
                4_000);
        }

        private static long ApplyFactor(long value, int factorBasisPoints)
        {
            return value * factorBasisPoints / 10_000;
        }

        private static bool HasValidPractice(
            WorldState world,
            EducationPlanState plan,
            PersonState student)
        {
            if (string.IsNullOrEmpty(plan.PracticePositionId))
            {
                return false;
            }

            try
            {
                ValidatePracticePosition(
                    world,
                    student,
                    plan.PracticePositionId,
                    plan.Discipline);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static void ValidateTeacher(
            WorldState world,
            PersonState student,
            PersonState teacher,
            ProfessionalDiscipline discipline)
        {
            if (teacher.Id == student.Id)
            {
                throw new InvalidOperationException("人物不能把自己登记为教师。");
            }

            if (!teacher.IsAlive || IsTraveling(world, teacher.Id))
            {
                throw new InvalidOperationException("教师当前无法授课。");
            }

            if (teacher.LocationId != student.LocationId)
            {
                throw new InvalidOperationException("教师必须与学生位于同一地点。");
            }

            var teacherSkill = ProfessionalSkillAccess.Get(
                teacher.ProfessionalSkills, discipline);
            var studentSkill = ProfessionalSkillAccess.Get(
                student.ProfessionalSkills, discipline);
            if (teacherSkill < SelfStudyLimitBasisPoints ||
                teacherSkill <= studentSkill)
            {
                throw new InvalidOperationException(
                    "教师的对应专业必须达到30且高于学生。");
            }
        }

        private static void ValidatePracticePosition(
            WorldState world,
            PersonState student,
            string positionId,
            ProfessionalDiscipline discipline)
        {
            MembershipState membership = null;
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var candidate = world.Memberships[i];
                if (candidate.PersonId == student.Id &&
                    candidate.PositionId == positionId)
                {
                    membership = candidate;
                    break;
                }
            }

            if (membership == null)
            {
                throw new InvalidOperationException(
                    "实践职位必须来自学生当前真实组织成员关系。");
            }

            var position = FindPosition(world, membership.PositionId);
            var organization = FindOrganization(world, position.OrganizationId);
            if (!IsCompatiblePractice(organization.Type, discipline))
            {
                throw new InvalidOperationException(
                    "该组织职位不能为所选专业提供匹配实践。");
            }
        }

        private static bool IsCompatiblePractice(
            OrganizationType organizationType,
            ProfessionalDiscipline discipline)
        {
            switch (discipline)
            {
                case ProfessionalDiscipline.Military:
                case ProfessionalDiscipline.MartialArts:
                    return organizationType == OrganizationType.Military;
                case ProfessionalDiscipline.Administration:
                case ProfessionalDiscipline.Scholarship:
                    return organizationType == OrganizationType.Government;
                case ProfessionalDiscipline.Commerce:
                    return organizationType == OrganizationType.Merchant;
                case ProfessionalDiscipline.Agriculture:
                case ProfessionalDiscipline.Craft:
                    return organizationType == OrganizationType.Family;
                case ProfessionalDiscipline.Medicine:
                    return
                        organizationType == OrganizationType.Government ||
                        organizationType == OrganizationType.Religious;
                case ProfessionalDiscipline.Negotiation:
                    return
                        organizationType == OrganizationType.Government ||
                        organizationType == OrganizationType.Merchant ||
                        organizationType == OrganizationType.Religious;
                case ProfessionalDiscipline.Intelligence:
                    return
                        organizationType == OrganizationType.Intelligence ||
                        organizationType == OrganizationType.Military;
                default:
                    throw new ArgumentOutOfRangeException(nameof(discipline));
            }
        }

        private static int CountActiveStudents(
            WorldState world,
            string teacherPersonId)
        {
            var count = 0;
            for (var i = 0; i < world.EducationPlans.Count; i++)
            {
                var plan = world.EducationPlans[i];
                if (plan.Status == EducationPlanStatus.Active &&
                    plan.TeacherPersonId == teacherPersonId)
                {
                    count++;
                }
            }

            return count;
        }

        private static EducationPlanState FindActivePlan(
            WorldState world,
            string studentPersonId)
        {
            for (var i = 0; i < world.EducationPlans.Count; i++)
            {
                var plan = world.EducationPlans[i];
                if (plan.StudentPersonId == studentPersonId &&
                    (plan.Status == EducationPlanStatus.Active ||
                     plan.Status == EducationPlanStatus.Suspended))
                {
                    return plan;
                }
            }

            return null;
        }

        private static bool IsTraveling(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return true;
                }
            }

            return false;
        }

        private static EducationPlanState FindPlan(
            WorldState world,
            string planId)
        {
            for (var i = 0; i < world.EducationPlans.Count; i++)
            {
                if (world.EducationPlans[i].Id == planId)
                {
                    return world.EducationPlans[i];
                }
            }

            throw new InvalidOperationException($"Missing education plan {planId}.");
        }

        private PersonState FindPerson(
            WorldState world,
            string personId)
        {
            return PeopleFor(world).GetRequired(personId);
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

        private static FamilyState FindFamily(
            WorldState world,
            string familyId)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == familyId)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {familyId}.");
        }

        private static PositionState FindPosition(
            WorldState world,
            string positionId)
        {
            for (var i = 0; i < world.Positions.Count; i++)
            {
                if (world.Positions[i].Id == positionId)
                {
                    return world.Positions[i];
                }
            }

            throw new InvalidOperationException($"Missing position {positionId}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
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
