using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum StartingIdentity : byte
    {
        Soldier,
        CountyClerk,
        Merchant,
        Physician
    }

    public sealed class NewGameCharacterRequest
    {
        public string DisplayName;
        public int Age = 18;
        public PersonGender Gender = PersonGender.Male;
        public StartingIdentity Identity = StartingIdentity.Soldier;
    }

    public sealed class NewGameSetupService
    {
        public const string CustomPlayerPersonId = "person.player";

        public WorldState CreateCustom184World(
            NewGameCharacterRequest request,
            ulong masterSeed = 184_001UL)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var displayName = (request.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0 || displayName.Length > 16)
            {
                throw new ArgumentException(
                    "人物姓名必须为1至16个字符。",
                    nameof(request));
            }

            if (request.Age < 16 || request.Age > 70)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    "开局年龄必须在16至70岁之间。");
            }

            if (request.Gender == PersonGender.Unknown)
            {
                throw new ArgumentException("自建人物必须选择性别。", nameof(request));
            }

            var world = PrototypeWorldFactory.Create184World(masterSeed);
            var person = BuildPlayerPerson(displayName, request);
            new PopulationLedgerSystem().MaterializePerson(
                world,
                person,
                StartingPopulationOccupation(request.Identity));
            world.Families.Add(new FamilyState
            {
                Id = "family.player_household",
                DisplayName = displayName + "之家",
                HeadPersonId = person.Id,
                Wealth = StartingHouseholdWealth(request.Identity),
                MemberIds = { person.Id }
            });
            AddStartingMembership(world, person, request.Identity);
            world.PlayerPersonId = person.Id;
            world.Validate();
            return world;
        }

        public WorldState CreateExisting184World(
            string personId,
            ulong masterSeed = 184_001UL)
        {
            if (string.IsNullOrWhiteSpace(personId))
            {
                throw new ArgumentException(
                    "必须选择一名现有人物。",
                    nameof(personId));
            }

            var world = PrototypeWorldFactory.Create184World(masterSeed);
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id != personId)
                {
                    continue;
                }

                world.PlayerPersonId = personId;
                world.Validate();
                return world;
            }

            throw new InvalidOperationException($"世界中不存在人物 {personId}。");
        }

        private static PersonState BuildPlayerPerson(
            string displayName,
            NewGameCharacterRequest request)
        {
            var person = new PersonState
            {
                Id = CustomPlayerPersonId,
                DisplayName = displayName,
                BirthDay = checked(-request.Age * 360L),
                Gender = request.Gender,
                IsAlive = true,
                HealthBasisPoints = 10_000,
                Provisions = 20
            };

            switch (request.Identity)
            {
                case StartingIdentity.Soldier:
                    person.LocationId = "location.zhuo";
                    person.Wealth = 200;
                    person.CargoCapacity = 30;
                    person.Personality.RiskTolerance = 6_000;
                    person.Needs.Status = 4_000;
                    break;
                case StartingIdentity.CountyClerk:
                    person.LocationId = "location.zhuo";
                    person.Wealth = 500;
                    person.CargoCapacity = 30;
                    person.Personality.Sociability = 6_000;
                    person.Needs.Status = 4_500;
                    break;
                case StartingIdentity.Merchant:
                    person.LocationId = "location.zhongshan";
                    person.Wealth = 2_000;
                    person.CargoCapacity = 120;
                    person.Personality.RiskTolerance = 5_500;
                    person.Needs.Wealth = 4_500;
                    break;
                case StartingIdentity.Physician:
                    person.LocationId = "location.guangzong";
                    person.Wealth = 1_000;
                    person.CargoCapacity = 30;
                    person.MedicalSkillBasisPoints = 6_500;
                    person.Personality.Benevolence = 7_000;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(request.Identity),
                        request.Identity,
                        "不支持的开局身份。");
            }

            return person;
        }

        private static long StartingHouseholdWealth(StartingIdentity identity)
        {
            switch (identity)
            {
                case StartingIdentity.Soldier:
                    return 300;
                case StartingIdentity.CountyClerk:
                    return 800;
                case StartingIdentity.Merchant:
                    return 3_000;
                case StartingIdentity.Physician:
                    return 1_200;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }
        }

        private static PopulationOccupation StartingPopulationOccupation(
            StartingIdentity identity)
        {
            switch (identity)
            {
                case StartingIdentity.Soldier:
                case StartingIdentity.CountyClerk:
                    return PopulationOccupation.Administration;
                case StartingIdentity.Merchant:
                    return PopulationOccupation.Merchant;
                case StartingIdentity.Physician:
                    return PopulationOccupation.Medical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }
        }

        private static void AddStartingMembership(
            WorldState world,
            PersonState person,
            StartingIdentity identity)
        {
            string organizationId;
            string positionId;
            switch (identity)
            {
                case StartingIdentity.Soldier:
                    organizationId = "organization.youzhou_field_force";
                    positionId = "position.youzhou_soldier";
                    break;
                case StartingIdentity.CountyClerk:
                    organizationId = "organization.zhuo_county_office";
                    positionId = "position.zhuo_county_clerk";
                    break;
                case StartingIdentity.Merchant:
                    organizationId = "organization.zhongshan_merchants";
                    positionId = "position.zhongshan_trader";
                    break;
                case StartingIdentity.Physician:
                    organizationId = "organization.guangzong_relief_camp";
                    positionId = "position.guangzong_physician";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }

            world.Memberships.Add(new MembershipState
            {
                Id = "membership.person.player." + IdentityKey(identity),
                PersonId = person.Id,
                OrganizationId = organizationId,
                PositionId = positionId,
                JoinedDay = world.AbsoluteDay,
                LoyaltyBasisPoints = 5_000
            });
        }

        private static string IdentityKey(StartingIdentity identity)
        {
            switch (identity)
            {
                case StartingIdentity.Soldier:
                    return "soldier";
                case StartingIdentity.CountyClerk:
                    return "county_clerk";
                case StartingIdentity.Merchant:
                    return "merchant";
                case StartingIdentity.Physician:
                    return "physician";
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }
        }
    }
}
