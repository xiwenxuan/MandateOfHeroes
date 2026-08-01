using System;

namespace Mandate.Domain
{
    public enum LifeGoalKind : byte
    {
        Unknown,
        PreserveFamily,
        WinMerit,
        BuildFortune,
        HealThePeople,
        PassOnCraft,
        RestoreOrder,
        SeekKnowledge,
        LiveInSeclusion,
        UnifyRealm
    }

    public enum CharacterBackgroundKind : byte
    {
        Commoner,
        Soldier,
        Official,
        Merchant,
        Physician,
        Farmer,
        Warrior,
        Scholar,
        Commander,
        CommanderScholar,
        Diplomat,
        ReligiousLeader
    }

    [Serializable]
    public sealed class CharacterAptitudeState
    {
        public int Constitution;
        public int Strength;
        public int Dexterity;
        public int Perception;
        public int Memory;
        public int Reasoning;
        public int Willpower;
        public int Affinity;
    }

    [Serializable]
    public sealed class ProfessionalSkillState
    {
        public int Military;
        public int MartialArts;
        public int Administration;
        public int Commerce;
        public int Agriculture;
        public int Craft;
        public int Medicine;
        public int Scholarship;
        public int Negotiation;
        public int Intelligence;
    }

    public readonly struct StrategicAttributeSummary
    {
        public int Leadership { get; }
        public int Martial { get; }
        public int Strategy { get; }
        public int Administration { get; }
        public int Charisma { get; }

        public StrategicAttributeSummary(
            int leadership,
            int martial,
            int strategy,
            int administration,
            int charisma)
        {
            Leadership = leadership;
            Martial = martial;
            Strategy = strategy;
            Administration = administration;
            Charisma = charisma;
        }
    }

    public static class StrategicAttributeCalculator
    {
        public static StrategicAttributeSummary Calculate(PersonState person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            var aptitude = person.Aptitudes ??
                throw new InvalidOperationException("Person aptitudes are missing.");
            var skill = person.ProfessionalSkills ??
                throw new InvalidOperationException("Person professional skills are missing.");
            var personality = person.Personality ??
                throw new InvalidOperationException("Person personality is missing.");

            var leadership = Weighted(
                skill.Military, 40,
                aptitude.Willpower, 20,
                aptitude.Reasoning, 10,
                skill.Administration, 10,
                skill.Negotiation, 10,
                aptitude.Affinity, 10);
            var martialBase = Weighted(
                aptitude.Strength, 25,
                aptitude.Dexterity, 20,
                aptitude.Constitution, 20,
                skill.MartialArts, 35);
            var healthFactor = 5_000 + person.HealthBasisPoints / 2;
            var martial = Clamp(martialBase * healthFactor / 10_000);
            var strategy = Weighted(
                aptitude.Reasoning, 25,
                aptitude.Perception, 20,
                aptitude.Memory, 15,
                skill.Military, 10,
                skill.Intelligence, 20,
                skill.Scholarship, 10);
            var administration = Weighted(
                skill.Administration, 35,
                aptitude.Reasoning, 15,
                aptitude.Memory, 10,
                skill.Commerce, 10,
                skill.Agriculture, 10,
                skill.Craft, 5,
                skill.Scholarship, 10,
                aptitude.Willpower, 5);
            var charisma = Weighted(
                aptitude.Affinity, 35,
                skill.Negotiation, 25,
                personality.Sociability, 15,
                personality.Benevolence, 15,
                aptitude.Willpower, 10);

            return new StrategicAttributeSummary(
                Clamp(leadership),
                martial,
                Clamp(strategy),
                Clamp(administration),
                Clamp(charisma));
        }

        private static int Weighted(
            int value1,
            int weight1,
            int value2,
            int weight2,
            int value3,
            int weight3,
            int value4,
            int weight4)
        {
            return (
                value1 * weight1 +
                value2 * weight2 +
                value3 * weight3 +
                value4 * weight4) / 100;
        }

        private static int Weighted(
            int value1,
            int weight1,
            int value2,
            int weight2,
            int value3,
            int weight3,
            int value4,
            int weight4,
            int value5,
            int weight5)
        {
            return (
                value1 * weight1 +
                value2 * weight2 +
                value3 * weight3 +
                value4 * weight4 +
                value5 * weight5) / 100;
        }

        private static int Weighted(
            int value1,
            int weight1,
            int value2,
            int weight2,
            int value3,
            int weight3,
            int value4,
            int weight4,
            int value5,
            int weight5,
            int value6,
            int weight6)
        {
            return (
                value1 * weight1 +
                value2 * weight2 +
                value3 * weight3 +
                value4 * weight4 +
                value5 * weight5 +
                value6 * weight6) / 100;
        }

        private static int Weighted(
            int value1,
            int weight1,
            int value2,
            int weight2,
            int value3,
            int weight3,
            int value4,
            int weight4,
            int value5,
            int weight5,
            int value6,
            int weight6,
            int value7,
            int weight7,
            int value8,
            int weight8)
        {
            return (
                value1 * weight1 +
                value2 * weight2 +
                value3 * weight3 +
                value4 * weight4 +
                value5 * weight5 +
                value6 * weight6 +
                value7 * weight7 +
                value8 * weight8) / 100;
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 10_000 ? 10_000 : value;
        }
    }

    public static class CharacterAbilityBootstrap
    {
        private const string RandomSystemId = "character_ability";

        public static void InitializeWorld(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                InitializePerson(
                    world.MasterSeed,
                    person,
                    InferBackground(person.Id));
            }
        }

        public static bool InitializePerson(
            ulong masterSeed,
            PersonState person,
            CharacterBackgroundKind background)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (person.AbilityProfileInitialized)
            {
                return false;
            }

            var random = new NamedRandom(masterSeed);
            var personId = new StableId(person.Id);
            person.Aptitudes = new CharacterAptitudeState
            {
                Constitution = DrawAptitude(random, personId, "constitution"),
                Strength = DrawAptitude(random, personId, "strength"),
                Dexterity = DrawAptitude(random, personId, "dexterity"),
                Perception = DrawAptitude(random, personId, "perception"),
                Memory = DrawAptitude(random, personId, "memory"),
                Reasoning = DrawAptitude(random, personId, "reasoning"),
                Willpower = DrawAptitude(random, personId, "willpower"),
                Affinity = DrawAptitude(random, personId, "affinity")
            };
            person.ProfessionalSkills = new ProfessionalSkillState
            {
                Military = DrawSkill(random, personId, "military"),
                MartialArts = DrawSkill(random, personId, "martial_arts"),
                Administration = DrawSkill(random, personId, "administration"),
                Commerce = DrawSkill(random, personId, "commerce"),
                Agriculture = DrawSkill(random, personId, "agriculture"),
                Craft = DrawSkill(random, personId, "craft"),
                Medicine = DrawSkill(random, personId, "medicine"),
                Scholarship = DrawSkill(random, personId, "scholarship"),
                Negotiation = DrawSkill(random, personId, "negotiation"),
                Intelligence = DrawSkill(random, personId, "intelligence")
            };

            ApplyBackground(person, background);
            person.ProfessionalSkills.Medicine = Math.Max(
                person.ProfessionalSkills.Medicine,
                person.MedicalSkillBasisPoints);
            ClampAll(person);
            person.LifeGoal = DefaultLifeGoal(background);
            person.AbilityProfileInitialized = true;
            return true;
        }

        public static void InitializeChild(
            WorldState world,
            PersonState child,
            PersonState father,
            PersonState mother)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (father == null)
            {
                throw new ArgumentNullException(nameof(father));
            }

            if (mother == null)
            {
                throw new ArgumentNullException(nameof(mother));
            }

            InitializePerson(
                world.MasterSeed,
                father,
                InferBackground(father.Id));
            InitializePerson(
                world.MasterSeed,
                mother,
                InferBackground(mother.Id));
            if (child.AbilityProfileInitialized)
            {
                return;
            }

            var random = new NamedRandom(world.MasterSeed);
            var childId = new StableId(child.Id);
            child.Aptitudes = new CharacterAptitudeState
            {
                Constitution = Inherit(
                    random, childId, "constitution",
                    father.Aptitudes.Constitution, mother.Aptitudes.Constitution),
                Strength = Inherit(
                    random, childId, "strength",
                    father.Aptitudes.Strength, mother.Aptitudes.Strength),
                Dexterity = Inherit(
                    random, childId, "dexterity",
                    father.Aptitudes.Dexterity, mother.Aptitudes.Dexterity),
                Perception = Inherit(
                    random, childId, "perception",
                    father.Aptitudes.Perception, mother.Aptitudes.Perception),
                Memory = Inherit(
                    random, childId, "memory",
                    father.Aptitudes.Memory, mother.Aptitudes.Memory),
                Reasoning = Inherit(
                    random, childId, "reasoning",
                    father.Aptitudes.Reasoning, mother.Aptitudes.Reasoning),
                Willpower = Inherit(
                    random, childId, "willpower",
                    father.Aptitudes.Willpower, mother.Aptitudes.Willpower),
                Affinity = Inherit(
                    random, childId, "affinity",
                    father.Aptitudes.Affinity, mother.Aptitudes.Affinity)
            };
            SetNoviceProfessionalSkills(child, random, childId);
            child.LifeGoal = LifeGoalKind.Unknown;
            child.AbilityProfileInitialized = true;
        }

        public static CharacterBackgroundKind InferBackground(string personId)
        {
            switch (personId)
            {
                case "person.liu_bei":
                    return CharacterBackgroundKind.Commander;
                case "person.guan_yu":
                case "person.zhang_fei":
                    return CharacterBackgroundKind.Warrior;
                case "person.jian_yong":
                    return CharacterBackgroundKind.Diplomat;
                case "person.zou_jing":
                case "person.guo_dian":
                    return CharacterBackgroundKind.Commander;
                case "person.zhang_shiping":
                case "person.su_shuang":
                    return CharacterBackgroundKind.Merchant;
                case "person.lu_zhi":
                    return CharacterBackgroundKind.CommanderScholar;
                case "person.zhang_jue":
                    return CharacterBackgroundKind.ReligiousLeader;
                case "person.generated.physician_001":
                    return CharacterBackgroundKind.Physician;
                case "person.generated.farmer_001":
                case "person.generated.farmer_002":
                    return CharacterBackgroundKind.Farmer;
                default:
                    return CharacterBackgroundKind.Commoner;
            }
        }

        private static void SetNoviceProfessionalSkills(
            PersonState child,
            NamedRandom random,
            StableId childId)
        {
            child.ProfessionalSkills = new ProfessionalSkillState
            {
                Military = DrawChildSkill(random, childId, "military"),
                MartialArts = DrawChildSkill(random, childId, "martial_arts"),
                Administration = DrawChildSkill(random, childId, "administration"),
                Commerce = DrawChildSkill(random, childId, "commerce"),
                Agriculture = DrawChildSkill(random, childId, "agriculture"),
                Craft = DrawChildSkill(random, childId, "craft"),
                Medicine = DrawChildSkill(random, childId, "medicine"),
                Scholarship = DrawChildSkill(random, childId, "scholarship"),
                Negotiation = DrawChildSkill(random, childId, "negotiation"),
                Intelligence = DrawChildSkill(random, childId, "intelligence")
            };
        }

        private static int DrawAptitude(
            NamedRandom random,
            StableId personId,
            string purpose)
        {
            return random.Range(
                RandomSystemId, personId, 0, "aptitude_" + purpose, 3_200, 6_801);
        }

        private static int DrawSkill(
            NamedRandom random,
            StableId personId,
            string purpose)
        {
            return random.Range(
                RandomSystemId, personId, 0, "skill_" + purpose, 500, 2_501);
        }

        private static int DrawChildSkill(
            NamedRandom random,
            StableId childId,
            string purpose)
        {
            return random.Range(
                RandomSystemId, childId, 0, "child_skill_" + purpose, 0, 301);
        }

        private static int Inherit(
            NamedRandom random,
            StableId childId,
            string purpose,
            int fatherValue,
            int motherValue)
        {
            var familyMean = (fatherValue + motherValue) / 2;
            var variation = random.Range(
                RandomSystemId,
                childId,
                0,
                "inherit_" + purpose,
                -1_200,
                1_201);
            return Clamp(familyMean + variation, 1_500, 9_000);
        }

        private static void ApplyBackground(
            PersonState person,
            CharacterBackgroundKind background)
        {
            var aptitude = person.Aptitudes;
            var skill = person.ProfessionalSkills;
            switch (background)
            {
                case CharacterBackgroundKind.Soldier:
                    aptitude.Constitution += 600;
                    aptitude.Strength += 500;
                    aptitude.Willpower += 500;
                    skill.Military += 1_800;
                    skill.MartialArts += 1_500;
                    break;
                case CharacterBackgroundKind.Official:
                    aptitude.Memory += 500;
                    aptitude.Reasoning += 500;
                    aptitude.Willpower += 300;
                    skill.Administration += 2_400;
                    skill.Scholarship += 1_100;
                    skill.Negotiation += 600;
                    break;
                case CharacterBackgroundKind.Merchant:
                    aptitude.Perception += 500;
                    aptitude.Memory += 300;
                    aptitude.Affinity += 500;
                    skill.Commerce += 2_500;
                    skill.Negotiation += 1_500;
                    skill.Intelligence += 500;
                    break;
                case CharacterBackgroundKind.Physician:
                    aptitude.Perception += 600;
                    aptitude.Memory += 700;
                    aptitude.Reasoning += 500;
                    aptitude.Affinity += 300;
                    skill.Medicine += 3_200;
                    skill.Scholarship += 1_000;
                    break;
                case CharacterBackgroundKind.Farmer:
                    aptitude.Constitution += 600;
                    aptitude.Perception += 300;
                    aptitude.Willpower += 400;
                    skill.Agriculture += 2_700;
                    skill.Craft += 500;
                    break;
                case CharacterBackgroundKind.Warrior:
                    aptitude.Constitution += 1_000;
                    aptitude.Strength += 1_700;
                    aptitude.Dexterity += 1_200;
                    aptitude.Willpower += 600;
                    skill.MartialArts += 4_200;
                    skill.Military += 1_600;
                    break;
                case CharacterBackgroundKind.Scholar:
                    aptitude.Memory += 900;
                    aptitude.Reasoning += 900;
                    aptitude.Affinity += 300;
                    skill.Scholarship += 3_200;
                    skill.Administration += 700;
                    skill.Negotiation += 500;
                    break;
                case CharacterBackgroundKind.Commander:
                    aptitude.Willpower += 1_000;
                    aptitude.Affinity += 700;
                    aptitude.Reasoning += 400;
                    skill.Military += 3_500;
                    skill.MartialArts += 900;
                    skill.Administration += 900;
                    skill.Negotiation += 1_400;
                    skill.Intelligence += 700;
                    break;
                case CharacterBackgroundKind.CommanderScholar:
                    aptitude.Memory += 800;
                    aptitude.Reasoning += 900;
                    aptitude.Willpower += 800;
                    skill.Military += 3_100;
                    skill.Administration += 1_800;
                    skill.Scholarship += 2_800;
                    skill.Negotiation += 900;
                    skill.Intelligence += 900;
                    break;
                case CharacterBackgroundKind.Diplomat:
                    aptitude.Affinity += 1_100;
                    aptitude.Perception += 500;
                    aptitude.Memory += 400;
                    skill.Negotiation += 3_400;
                    skill.Scholarship += 1_100;
                    skill.Intelligence += 700;
                    break;
                case CharacterBackgroundKind.ReligiousLeader:
                    aptitude.Memory += 800;
                    aptitude.Reasoning += 700;
                    aptitude.Willpower += 1_000;
                    aptitude.Affinity += 900;
                    skill.Scholarship += 2_500;
                    skill.Medicine += 2_000;
                    skill.Negotiation += 2_500;
                    skill.Military += 1_700;
                    break;
                case CharacterBackgroundKind.Commoner:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(background), background, "Unknown character background.");
            }
        }

        private static LifeGoalKind DefaultLifeGoal(
            CharacterBackgroundKind background)
        {
            switch (background)
            {
                case CharacterBackgroundKind.Soldier:
                case CharacterBackgroundKind.Warrior:
                case CharacterBackgroundKind.Commander:
                    return LifeGoalKind.WinMerit;
                case CharacterBackgroundKind.Official:
                case CharacterBackgroundKind.CommanderScholar:
                case CharacterBackgroundKind.Diplomat:
                    return LifeGoalKind.RestoreOrder;
                case CharacterBackgroundKind.Merchant:
                    return LifeGoalKind.BuildFortune;
                case CharacterBackgroundKind.Physician:
                case CharacterBackgroundKind.ReligiousLeader:
                    return LifeGoalKind.HealThePeople;
                case CharacterBackgroundKind.Farmer:
                    return LifeGoalKind.PreserveFamily;
                case CharacterBackgroundKind.Scholar:
                    return LifeGoalKind.SeekKnowledge;
                default:
                    return LifeGoalKind.PreserveFamily;
            }
        }

        private static void ClampAll(PersonState person)
        {
            var aptitude = person.Aptitudes;
            aptitude.Constitution = Clamp(aptitude.Constitution, 0, 10_000);
            aptitude.Strength = Clamp(aptitude.Strength, 0, 10_000);
            aptitude.Dexterity = Clamp(aptitude.Dexterity, 0, 10_000);
            aptitude.Perception = Clamp(aptitude.Perception, 0, 10_000);
            aptitude.Memory = Clamp(aptitude.Memory, 0, 10_000);
            aptitude.Reasoning = Clamp(aptitude.Reasoning, 0, 10_000);
            aptitude.Willpower = Clamp(aptitude.Willpower, 0, 10_000);
            aptitude.Affinity = Clamp(aptitude.Affinity, 0, 10_000);

            var skill = person.ProfessionalSkills;
            skill.Military = Clamp(skill.Military, 0, 10_000);
            skill.MartialArts = Clamp(skill.MartialArts, 0, 10_000);
            skill.Administration = Clamp(skill.Administration, 0, 10_000);
            skill.Commerce = Clamp(skill.Commerce, 0, 10_000);
            skill.Agriculture = Clamp(skill.Agriculture, 0, 10_000);
            skill.Craft = Clamp(skill.Craft, 0, 10_000);
            skill.Medicine = Clamp(skill.Medicine, 0, 10_000);
            skill.Scholarship = Clamp(skill.Scholarship, 0, 10_000);
            skill.Negotiation = Clamp(skill.Negotiation, 0, 10_000);
            skill.Intelligence = Clamp(skill.Intelligence, 0, 10_000);
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
