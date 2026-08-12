using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum StartingIdentity : byte
    {
        Soldier,
        CountyClerk,
        Merchant,
        Physician,
        Farmer,
        Scholar
    }

    public static class StartingBackgroundIds
    {
        public const string LocalHousehold =
            "starting_background.local_household";
        public const string DisplacedHousehold =
            "starting_background.displaced_household";
        public const string SupportedHousehold =
            "starting_background.supported_household";
    }

    public sealed class NewGameCharacterRequest
    {
        public string DisplayName;
        public int Age = 18;
        public PersonGender Gender = PersonGender.Male;
        public StartingIdentity Identity = StartingIdentity.Soldier;
        public string BackgroundId = StartingBackgroundIds.LocalHousehold;
        public string StartingLocationId = string.Empty;
    }

    public sealed class NewGameSetupService
    {
        public const string CustomPlayerPersonId = "person.player";

        private readonly MerchantHouseholdContentRegistry _merchantContent;

        public NewGameSetupService(
            MerchantHouseholdContentRegistry merchantContent = null)
        {
            _merchantContent = merchantContent ??
                MerchantHouseholdContentRegistry.CreateCore();
        }

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

            ValidateBackground(request.BackgroundId);

            var world = PrototypeWorldFactory.Create184World(masterSeed);
            var person = BuildPlayerPerson(displayName, request);
            if (!string.IsNullOrWhiteSpace(request.StartingLocationId))
            {
                var requestedLocation = RequireLocation(
                    world, request.StartingLocationId);
                ValidateStartingLocation(
                    world, request.Identity, requestedLocation.Id);
                person.LocationId = requestedLocation.Id;
            }
            person.BirthLocationId = person.LocationId;
            ApplyStartingBackground(person, request.BackgroundId);
            CharacterAbilityBootstrap.InitializePerson(
                world.MasterSeed,
                person,
                StartingCharacterBackground(request.Identity));
            new PopulationLedgerSystem().MaterializePerson(
                world,
                person,
                StartingPopulationOccupation(request.Identity));
            var family = new FamilyState
            {
                Id = "family.player_household",
                DisplayName = displayName + "之家",
                HeadPersonId = person.Id,
                LocationId = person.LocationId,
                Wealth = StartingHouseholdWealth(request.Identity),
                MemberIds = { person.Id }
            };
            ApplyFamilyBackground(family, request.BackgroundId);
            world.Families.Add(family);
            person.FamilyId = family.Id;
            EnsureIdentityWorldDefinitions(world, person, request.Identity);
            if (request.Identity == StartingIdentity.Farmer)
            {
                EnsureFarmerHousehold(world, person, family);
            }
            AddStartingMembership(world, person, request.Identity);
            if (request.Identity == StartingIdentity.Soldier)
            {
                EnlistPlayerInPrototypeArmy(world, person);
            }
            world.PlayerPersonId = person.Id;
            if (request.Identity == StartingIdentity.Merchant)
            {
                MerchantHouseholdGameplayService.Initialize(
                    world,
                    person.Id,
                    _merchantContent);
            }
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

        public IReadOnlyList<string> GetLegalStartingLocationIds(
            WorldState world,
            StartingIdentity identity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var result = new List<string>();
            if (identity == StartingIdentity.Soldier)
            {
                var army = world.Armies.Find(
                    item => item.Id == "army.youzhou_reinforcement");
                if (army == null)
                {
                    throw new InvalidOperationException(
                        "幽州援军缺少可供玩家加入的军队编制。");
                }
                result.Add(army.LocationId);
                return result;
            }

            for (var i = 0; i < world.Locations.Count; i++)
            {
                result.Add(world.Locations[i].Id);
            }
            return result;
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
                case StartingIdentity.Farmer:
                    person.LocationId = "location.zhuo";
                    person.Wealth = 120;
                    person.CargoCapacity = 40;
                    person.Provisions = 30;
                    person.Personality.FamilyDuty = 6_500;
                    person.Needs.Livelihood = 5_000;
                    break;
                case StartingIdentity.Scholar:
                    person.LocationId = "location.zhuo";
                    person.Wealth = 600;
                    person.CargoCapacity = 30;
                    person.Personality.Sociability = 5_500;
                    person.Needs.Status = 5_000;
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
                case StartingIdentity.Farmer:
                    return 300;
                case StartingIdentity.Scholar:
                    return 900;
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
                case StartingIdentity.Farmer:
                    return PopulationOccupation.Agriculture;
                case StartingIdentity.Scholar:
                    return PopulationOccupation.Administration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }
        }

        private static CharacterBackgroundKind StartingCharacterBackground(
            StartingIdentity identity)
        {
            switch (identity)
            {
                case StartingIdentity.Soldier:
                    return CharacterBackgroundKind.Soldier;
                case StartingIdentity.CountyClerk:
                    return CharacterBackgroundKind.Official;
                case StartingIdentity.Merchant:
                    return CharacterBackgroundKind.Merchant;
                case StartingIdentity.Physician:
                    return CharacterBackgroundKind.Physician;
                case StartingIdentity.Farmer:
                    return CharacterBackgroundKind.Farmer;
                case StartingIdentity.Scholar:
                    return CharacterBackgroundKind.Scholar;
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
                case StartingIdentity.Farmer:
                    organizationId = "organization.zhuo_farming_households";
                    positionId = "position.zhuo_farmer";
                    break;
                case StartingIdentity.Scholar:
                    organizationId = "organization.zhuo_school";
                    positionId = "position.zhuo_scholar";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }

            world.Memberships.Add(new MembershipState
            {
                Id = "membership." + person.Id + "." + IdentityKey(identity),
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
                case StartingIdentity.Farmer:
                    return "farmer";
                case StartingIdentity.Scholar:
                    return "scholar";
                default:
                    throw new ArgumentOutOfRangeException(nameof(identity));
            }
        }

        private static void ValidateBackground(string backgroundId)
        {
            if (backgroundId != StartingBackgroundIds.LocalHousehold &&
                backgroundId != StartingBackgroundIds.DisplacedHousehold &&
                backgroundId != StartingBackgroundIds.SupportedHousehold)
            {
                throw new ArgumentException(
                    $"不支持的出生背景 {backgroundId}。",
                    nameof(backgroundId));
            }
        }

        private static void ApplyStartingBackground(
            PersonState person,
            string backgroundId)
        {
            if (backgroundId == StartingBackgroundIds.DisplacedHousehold)
            {
                person.Wealth /= 2;
                person.Provisions = Math.Max(5, person.Provisions / 2);
                person.Personality.RiskTolerance = Math.Max(
                    person.Personality.RiskTolerance, 6_000);
            }
            else if (backgroundId == StartingBackgroundIds.SupportedHousehold)
            {
                person.Wealth = checked(person.Wealth + 300);
                person.Provisions = checked(person.Provisions + 10);
                person.Needs.Status = Math.Max(person.Needs.Status, 5_000);
            }
        }

        private static void ApplyFamilyBackground(
            FamilyState family,
            string backgroundId)
        {
            if (backgroundId == StartingBackgroundIds.DisplacedHousehold)
            {
                family.Wealth /= 2;
                family.Debt = 100;
            }
            else if (backgroundId == StartingBackgroundIds.SupportedHousehold)
            {
                family.Wealth = checked(family.Wealth + 600);
            }
        }

        private static LocationState RequireLocation(
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

            throw new ArgumentException(
                $"世界中不存在开局地点 {locationId}。",
                nameof(locationId));
        }

        private static void ValidateStartingLocation(
            WorldState world,
            StartingIdentity identity,
            string locationId)
        {
            if (identity != StartingIdentity.Soldier)
            {
                return;
            }

            var army = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            if (army == null)
            {
                throw new InvalidOperationException(
                    "幽州援军缺少可供玩家加入的军队编制。");
            }
            if (army.LocationId != locationId)
            {
                throw new ArgumentException(
                    "军人开局地点必须是所加入军队当前的真实集结地。",
                    nameof(locationId));
            }
        }

        private static void EnsureIdentityWorldDefinitions(
            WorldState world,
            PersonState person,
            StartingIdentity identity)
        {
            if (identity != StartingIdentity.Farmer &&
                identity != StartingIdentity.Scholar)
            {
                return;
            }

            var farmer = identity == StartingIdentity.Farmer;
            var organizationId = farmer
                ? "organization.zhuo_farming_households"
                : "organization.zhuo_school";
            var positionId = farmer
                ? "position.zhuo_farmer"
                : "position.zhuo_scholar";
            if (!world.Organizations.Exists(item => item.Id == organizationId))
            {
                world.Organizations.Add(new OrganizationState
                {
                    Id = organizationId,
                    DisplayName = farmer ? "涿县农户互助社" : "涿县乡学",
                    Type = farmer
                        ? OrganizationType.Family
                        : OrganizationType.Government,
                    HeadquartersLocationId = person.LocationId,
                    Treasury = farmer ? 500 : 2_000
                });
            }
            if (!world.Positions.Exists(item => item.Id == positionId))
            {
                world.Positions.Add(new PositionState
                {
                    Id = positionId,
                    OrganizationId = organizationId,
                    DisplayName = farmer ? "自耕农" : "游学士人",
                    Rank = 0,
                    Capacity = 100
                });
            }

            var taskId = farmer
                ? "task_definition.player_farm_work"
                : "task_definition.player_school_records";
            if (!world.TaskDefinitions.Exists(item => item.Id == taskId))
            {
                world.TaskDefinitions.Add(new TaskDefinitionState
                {
                    Id = taskId,
                    DisplayName = farmer ? "协助乡里春耕" : "整理乡学文书",
                    Kind = TaskKind.LocalWork,
                    IssuerOrganizationId = organizationId,
                    RequiredPositionId = positionId,
                    OriginLocationId = person.LocationId,
                    RequiredProgress = 3,
                    DurationDays = 10,
                    RewardMoney = farmer ? 80 : 120,
                    RewardProvisions = farmer ? 3 : 1,
                    RequiresMembership = true,
                    IsAvailable = true
                });
            }
        }

        private static void EnsureFarmerHousehold(
            WorldState world,
            PersonState person,
            FamilyState family)
        {
            var suffix = family.Id == "family.player_household"
                ? "player"
                : "zhuo_farm";
            var villageId = "village." + suffix;
            family.LocationId = person.LocationId;
            family.VillageId = villageId;
            family.Grain = Math.Max(family.Grain, 60);
            family.SeedGrain = Math.Max(family.SeedGrain, 24);
            family.FarmlandUnits = Math.Max(family.FarmlandUnits, 6);
            family.FoodSecurityBasisPoints = Math.Max(
                family.FoodSecurityBasisPoints, 7_000);
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var member = FindPerson(world, family.MemberIds[i]);
                member.LocationId = person.LocationId;
                member.FamilyId = family.Id;
                member.VillageOccupation = VillageOccupation.Farmer;
            }

            if (!world.Villages.Exists(item => item.Id == villageId))
            {
                world.Villages.Add(new VillageState
                {
                    Id = villageId,
                    DisplayName = person.DisplayName + "所在里落",
                    LocationId = person.LocationId,
                    ParentLocationId = person.LocationId,
                    HouseholdIds = new List<string> { family.Id },
                    PublicGranaryGrain = 30,
                    LedgerOpeningPublicGrain = 30,
                    LedgerOpeningFamilyGrain = family.Grain,
                    HouseholdCount = 1,
                    LivingResidentCount = family.MemberIds.Count,
                    WorkingResidentCount = family.MemberIds.Count,
                    NextSettlementDay = 30
                });
            }

            var fieldId = "facility." + suffix + ".farmland";
            if (!world.VillageFacilities.Exists(item => item.Id == fieldId))
            {
                world.VillageFacilities.Add(new VillageFacilityState
                {
                    Id = fieldId,
                    VillageId = villageId,
                    Kind = VillageFacilityKind.Farmland,
                    OwnerFamilyId = family.Id,
                    ManagerPersonId = person.Id,
                    Capacity = family.FarmlandUnits,
                    ConditionBasisPoints = 8_000
                });
            }
            var granaryId = "facility." + suffix + ".granary";
            if (!world.VillageFacilities.Exists(item => item.Id == granaryId))
            {
                world.VillageFacilities.Add(new VillageFacilityState
                {
                    Id = granaryId,
                    VillageId = villageId,
                    Kind = VillageFacilityKind.HouseholdGranary,
                    OwnerFamilyId = family.Id,
                    ManagerPersonId = person.Id,
                    Capacity = 2_000,
                    InventoryUnits = family.Grain + family.SeedGrain,
                    ConditionBasisPoints = 8_000
                });
            }
        }

        private static void EnlistPlayerInPrototypeArmy(
            WorldState world,
            PersonState person)
        {
            var army = world.Armies.Find(
                item => item.Id == "army.youzhou_reinforcement");
            if (army == null)
            {
                throw new InvalidOperationException(
                    "幽州援军缺少可供玩家加入的军队编制。");
            }
            var formation = world.MilitaryFormations.Find(
                item => item.ArmyId == army.Id &&
                    item.Kind == MilitaryFormationKind.Unit);
            if (formation == null)
            {
                throw new InvalidOperationException(
                    "幽州援军缺少可供玩家加入的军队编制。");
            }

            new PopulationLedgerSystem().MoveIndependentPerson(
                world,
                person,
                army.LocationId,
                false);
            var service = new MilitaryServiceState
            {
                Id = "military_service.person.player",
                PersonId = person.Id,
                ArmyId = army.Id,
                FormationId = formation.Id,
                Role = MilitaryServiceRole.Soldier,
                Rank = 1,
                Status = MilitaryServiceStatus.Active,
                DisciplineBasisPoints = 5_500,
                LoyaltyBasisPoints = 5_000,
                ServiceExperienceBasisPoints = 1_000,
                EnlistedDay = world.AbsoluteDay,
                LastStatusChangeDay = world.AbsoluteDay
            };
            world.MilitaryServices.Add(service);
            army.MaximumTroops++;
            new MilitaryServiceSystem().SynchronizeArmyCaches(world, army.Id);
            var family = FindFamily(world, person.FamilyId);
            family.LocationId = person.LocationId;
        }

        private static FamilyState FindFamily(WorldState world, string familyId)
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

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }
    }

    public static class PlayerActionIds
    {
        public const string Rest = "player_action.rest";
        public const string AcceptTask = "player_action.task.accept";
        public const string WorkTask = "player_action.task.work";
        public const string AbandonTask = "player_action.task.abandon";
        public const string Construction = "player_action.construction.work";
        public const string FarmStart = "player_action.farm.start";
        public const string FarmComplete = "player_action.farm.complete";
        public const string TradeBuy = "player_action.trade.buy";
        public const string TradeSell = "player_action.trade.sell";
        public const string Study = "player_action.study";
        public const string ArmyAdvance = "player_action.army.advance";
        public const string Battle = "player_action.battle";
        public const string LocalReliefHelp = "player_action.event.local.help";
        public const string LocalReliefDecline = "player_action.event.local.decline";
        public const string HistoricalReport = "player_action.event.history.report";
        public const string HistoricalObserve = "player_action.event.history.observe";
        public const string ClinicCare = "player_action.care.clinic";
        public const string FieldCare = "player_action.care.field";
        public const string HomeRest = "player_action.care.home";
        public const string MerchantUseOwnCapital =
            "player_action.m26p1.capital.own";
        public const string MerchantTakeGuildAdvance =
            "player_action.m26p1.capital.guild";
        public const string MerchantBuyJourneyCargo =
            "player_action.m26p1.cargo.buy";
        public const string MerchantStartJourney =
            "player_action.m26p1.journey.start";
        public const string MerchantEventHelp =
            "player_action.m26p1.event.help";
        public const string MerchantEventGuard =
            "player_action.m26p1.event.guard";
        public const string MerchantEventRefuse =
            "player_action.m26p1.event.refuse";
        public const string MerchantDeliverCargo =
            "player_action.m26p1.cargo.deliver";
        public const string MerchantRepayFamilyDebt =
            "player_action.m26p1.family.repay";
        public const string MerchantInvestCart =
            "player_action.m26p1.family.cart";
    }

    public sealed class PlayerActionOption
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool IsAvailable;
        public string UnavailableReason;
        public string Motivation;
        public string ExpectedOutcome;
        public string Cost;
        public string KnownRisk;
        public string PresentationCue;
        public string UnlockHint;
    }

    public sealed class PlayerActionResult
    {
        public bool Success;
        public string ActionId;
        public string Summary;
        public int DaysAdvanced;
        public long MoneyChange;
        public int ProvisionChange;
        public int HealthChange;
        public string WorldEventId;
        public string ResultId;
        public string PresentationCue;
        public string Detail;
    }

    public sealed class PlayerActionService
    {
        private readonly WorldSimulator _simulator;
        private readonly TaskSystem _tasks = new TaskSystem();
        private readonly ConstructionSystem _construction =
            new ConstructionSystem();
        private readonly TradingSystem _trading = new TradingSystem();
        private readonly EducationSystem _education = new EducationSystem();
        private readonly ArmySystem _armies = new ArmySystem();
        private readonly MerchantHouseholdGameplayService _merchantHousehold;

        public PlayerActionService(
            WorldSimulator simulator,
            MerchantHouseholdContentRegistry merchantContent = null)
        {
            _simulator = simulator ??
                throw new ArgumentNullException(nameof(simulator));
            _merchantHousehold = new MerchantHouseholdGameplayService(
                _simulator,
                merchantContent);
        }

        public MerchantHouseholdGoalView InspectMerchantGoal(
            WorldState world,
            string personId) =>
            _merchantHousehold.Inspect(world, personId);

        public IReadOnlyList<PlayerActionOption> QueryActions(
            WorldState world,
            string personId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var person = FindPerson(world, personId);
            var actions = new List<PlayerActionOption>();
            if (!person.IsAlive)
            {
                actions.Add(Option(
                    PlayerActionIds.Rest,
                    "人物已经去世",
                    "这段人生已经结束。",
                    false,
                    "已故人物不能继续行动。"));
                return actions;
            }

            var journey = FindJourney(world, person.Id);
            var pendingMerchantEvent =
                _merchantHousehold.HasPendingTravelEvent(world, person.Id);
            actions.Add(Option(
                PlayerActionIds.Rest,
                journey == null ? "休息一天" : "旅途中推进一天",
                journey == null
                    ? "消耗一天时间，世界与NPC同步推进。"
                    : "继续当前旅程一天，沿途消耗与世界事件照常结算。",
                !pendingMerchantEvent,
                pendingMerchantEvent
                    ? "请先处理眼前的途中事件。"
                    : string.Empty));

            _merchantHousehold.AddActions(world, person, actions);
            AddTaskActions(world, person, actions, journey == null);
            AddConstructionAction(world, person, actions, journey == null);
            AddIdentityActions(world, person, actions, journey == null);
            AddEventActions(world, person, actions, journey == null);
            AddCareActions(world, person, actions, journey == null);
            return actions;
        }

        public PlayerActionResult Execute(
            WorldState world,
            string personId,
            string actionId)
        {
            var available = QueryActions(world, personId);
            PlayerActionOption selected = null;
            for (var i = 0; i < available.Count; i++)
            {
                if (available[i].Id == actionId)
                {
                    selected = available[i];
                    break;
                }
            }
            if (selected == null)
            {
                return Failure(actionId, "当前人物没有这项行动。");
            }
            if (!selected.IsAvailable)
            {
                return Failure(actionId, selected.UnavailableReason);
            }

            if (_merchantHousehold.Handles(actionId))
            {
                return _merchantHousehold.Execute(world, personId, actionId);
            }

            var person = FindPerson(world, personId);
            var openingDay = world.AbsoluteDay;
            var openingMoney = person.Wealth;
            var openingProvisions = person.Provisions;
            var openingHealth = person.HealthBasisPoints;
            string summary;
            string eventId = string.Empty;
            switch (actionId)
            {
                case PlayerActionIds.Rest:
                    _simulator.AdvanceDays(world, 1);
                    summary = FindJourney(world, person.Id) == null
                        ? "休息了一天，世界继续运转。"
                        : "旅程推进了一天。";
                    break;
                case PlayerActionIds.AcceptTask:
                    summary = AcceptFirstTask(world, person);
                    break;
                case PlayerActionIds.WorkTask:
                    _simulator.AdvanceDays(world, 1);
                    summary = "投入一天完成当前职责，任务与世界同步结算。";
                    break;
                case PlayerActionIds.AbandonTask:
                    summary = _tasks.AbandonActiveTask(
                        world, new StableId(person.Id));
                    break;
                case PlayerActionIds.Construction:
                    summary = WorkConstruction(world, person);
                    _simulator.AdvanceDays(world, 1);
                    break;
                case PlayerActionIds.FarmStart:
                    summary = StartFarmSeason(world, person);
                    _simulator.AdvanceDays(world, 1);
                    break;
                case PlayerActionIds.FarmComplete:
                    summary = CompleteFarmSeason(world, person);
                    break;
                case PlayerActionIds.TradeBuy:
                    summary = _trading.Buy(
                        world,
                        new StableId(person.Id),
                        new StableId("commodity.cloth"),
                        2).Message;
                    _simulator.AdvanceDays(world, 1);
                    break;
                case PlayerActionIds.TradeSell:
                    summary = _trading.Sell(
                        world,
                        new StableId(person.Id),
                        new StableId("commodity.cloth"),
                        Math.Min(
                            2,
                            _trading.GetQuantity(
                                world, person.Id, "commodity.cloth"))).Message;
                    _simulator.AdvanceDays(world, 1);
                    break;
                case PlayerActionIds.Study:
                    summary = StudyOneMonth(world, person);
                    break;
                case PlayerActionIds.ArmyAdvance:
                    summary = AdvanceArmy(world, person);
                    break;
                case PlayerActionIds.Battle:
                    summary = ResolveBattle(world, person);
                    _simulator.AdvanceDays(world, 1);
                    break;
                case PlayerActionIds.LocalReliefHelp:
                    eventId = RecordChoice(
                        world,
                        person,
                        "local_relief_help",
                        "帮助本地缺粮家庭，付出2份口粮，地方治安略有改善。",
                        true,
                        false);
                    summary = "你拿出2份口粮帮助了附近家庭，乡里对你更为信任。";
                    break;
                case PlayerActionIds.LocalReliefDecline:
                    eventId = RecordChoice(
                        world,
                        person,
                        "local_relief_decline",
                        "拒绝参与本地赈济，保留了自己的口粮。",
                        false,
                        false);
                    summary = "你选择保存自家口粮，没有介入这次求助。";
                    break;
                case PlayerActionIds.HistoricalReport:
                    eventId = RecordChoice(
                        world,
                        person,
                        "taiping_report",
                        "将太平道活动传闻上报官府，地方加强了戒备。",
                        false,
                        true);
                    summary = "你将太平道活动传闻上报，地方戒备有所提高。";
                    break;
                case PlayerActionIds.HistoricalObserve:
                    eventId = RecordChoice(
                        world,
                        person,
                        "taiping_observe",
                        "选择继续观察太平道传闻，没有立即惊动官府。",
                        false,
                        false);
                    summary = "你暂未上报，选择继续观察传闻的真伪。";
                    break;
                case PlayerActionIds.ClinicCare:
                    summary = ReceiveClinicCare(world, person);
                    break;
                case PlayerActionIds.FieldCare:
                    var fieldCare = ReceiveFieldCare(world, person);
                    if (!fieldCare.Success)
                    {
                        return Failure(actionId, fieldCare.Message);
                    }
                    summary = fieldCare.Message;
                    break;
                case PlayerActionIds.HomeRest:
                    summary = RecoverAtHome(world, person);
                    break;
                default:
                    return Failure(actionId, "尚未实现这项行动。 ");
            }

            person = FindPerson(world, personId);
            world.Validate();
            return new PlayerActionResult
            {
                Success = true,
                ActionId = actionId,
                Summary = summary,
                DaysAdvanced = checked((int)(world.AbsoluteDay - openingDay)),
                MoneyChange = person.Wealth - openingMoney,
                ProvisionChange = person.Provisions - openingProvisions,
                HealthChange = person.HealthBasisPoints - openingHealth,
                WorldEventId = eventId,
                ResultId = string.IsNullOrEmpty(eventId)
                    ? "result." + actionId + "." + openingDay
                    : eventId,
                PresentationCue = "action.default",
                Detail = summary
            };
        }

        private void AddTaskActions(
            WorldState world,
            PersonState person,
            List<PlayerActionOption> actions,
            bool stationary)
        {
            var active = FindActiveTask(world, person.Id);
            if (active == null)
            {
                var candidate = FindAcceptableTask(world, person);
                actions.Add(Option(
                    PlayerActionIds.AcceptTask,
                    "接受本地任务",
                    candidate == null
                        ? "当前没有符合身份、地点和组织关系的任务。"
                        : "接受“" + candidate.DisplayName + "”。",
                    stationary && candidate != null,
                    stationary
                        ? "当前没有符合身份与地点条件的任务。"
                        : "旅途中不能接受本地任务。"));
                return;
            }

            var activeDefinition = world.TaskDefinitions.Find(item =>
                item.Id == active.DefinitionId);
            if (activeDefinition != null &&
                activeDefinition.Kind == TaskKind.GuidedObjective)
            {
                // Guided objectives expose their own contextual actions. The
                // generic work/abandon buttons would either do nothing or
                // bypass the authored consequence chain.
                return;
            }

            actions.Add(Option(
                PlayerActionIds.WorkTask,
                "推进当前任务",
                "投入一天，由任务系统按地点与任务类型结算进度。",
                stationary,
                "旅途中不能推进本地工作。"));
            actions.Add(Option(
                PlayerActionIds.AbandonTask,
                "放弃当前任务",
                "任务将保留为已放弃记录，不会删除。",
                true,
                string.Empty));
        }

        private void AddConstructionAction(
            WorldState world,
            PersonState person,
            List<PlayerActionOption> actions,
            bool stationary)
        {
            var project = FindLocalProject(world, person.LocationId);
            var location = FindLocation(world, person.LocationId);
            var recommended = ConstructionSystem.RecommendFeature(
                location,
                MapPerspectiveSystem.RecommendForPlayer(world, person.Id));
            var canStart = recommended != LocationFeature.None &&
                (location.Features & recommended) == 0;
            actions.Add(Option(
                PlayerActionIds.Construction,
                project == null ? "发起本地建设" : "参与本地建设",
                project == null
                    ? "发起“" + ConstructionSystem.FeatureName(recommended) +
                      "”建设，并投入一天劳力。"
                    : "为“" + project.DisplayName + "”投入一天劳力与资金。",
                stationary && person.Wealth >= 20 &&
                    (project != null || canStart),
                !stationary
                    ? "旅途中不能参加固定地点建设。"
                    : person.Wealth < 20
                        ? "至少需要20钱购买本次材料。"
                        : "本地暂时没有可发起的建设项目。"));
        }

        private void AddIdentityActions(
            WorldState world,
            PersonState person,
            List<PlayerActionOption> actions,
            bool stationary)
        {
            var family = FindFamily(world, person.FamilyId, false);
            if (family != null && !string.IsNullOrEmpty(family.VillageId))
            {
                var order = FindActiveFarmOrder(world, family.Id);
                actions.Add(Option(
                    order == null
                        ? PlayerActionIds.FarmStart
                        : PlayerActionIds.FarmComplete,
                    order == null ? "安排一季耕作" : "推进至收获结算",
                    order == null
                        ? "投入种子、土地与家庭劳力，建立真实农业工单。"
                        : "推进到收获日，结算产量、损耗与产品批次。",
                    stationary,
                    "旅途中不能管理家田。"));
            }

            if (HasPosition(world, person.Id, "trader"))
            {
                var quantity = _trading.GetQuantity(
                    world, person.Id, "commodity.cloth");
                var buyUnavailableReason = TradeBuyUnavailableReason(
                    world, person, 2);
                actions.Add(Option(
                    PlayerActionIds.TradeBuy,
                    "买入2匹布帛",
                    "从当前市场以实时价格买入并装入人物货物账。",
                    stationary && string.IsNullOrEmpty(buyUnavailableReason),
                    !stationary
                        ? "旅途中不能交易。"
                        : buyUnavailableReason));
                actions.Add(Option(
                    PlayerActionIds.TradeSell,
                    "卖出随身布帛",
                    "按当前市场价格卖出至多2匹布帛。",
                    stationary && HasMarket(world, person.LocationId) &&
                        quantity > 0,
                    quantity <= 0
                        ? "没有可出售的布帛。"
                        : "当前不能进入市场。"));
            }

            if (HasPosition(world, person.Id, "scholar"))
            {
                actions.Add(Option(
                    PlayerActionIds.Study,
                    "研习一个月",
                    "建立或继续经学自学计划，推进30天并结算能力成长。",
                    stationary,
                    "旅途中不能执行整月研习。"));
            }

            var service = FindMilitaryService(world, person.Id);
            if (service != null)
            {
                var army = FindArmy(world, service.ArmyId);
                var enemy = FindArmy(world, "army.yellow_turban_guangzong", false);
                var canBattle = service.Status == MilitaryServiceStatus.Active &&
                    enemy != null && army.LocationId == enemy.LocationId &&
                    army.IsMobilized && enemy.IsMobilized &&
                    FindArmyMarch(world, army.Id) == null;
                actions.Add(Option(
                    PlayerActionIds.ArmyAdvance,
                    "随军开赴前线",
                    "服从军令随军行进；全军人物、补给和世界时间共同推进。",
                    stationary && service.Status == MilitaryServiceStatus.Active &&
                        (army.LocationId == "location.zhongshan" ||
                         army.LocationId == "location.anping"),
                    service.Status != MilitaryServiceStatus.Active
                        ? "当前服役状态不能随军出发。"
                        : "本军当前没有下一段前线路线。"));
                actions.Add(Option(
                    PlayerActionIds.Battle,
                    "参加广宗局部战斗",
                    "由军队主将下令，按真实兵力、装备、士气与补给结算。",
                    canBattle,
                    "敌我军队尚未在广宗同时完成动员与集结。"));
            }
        }

        private static void AddEventActions(
            WorldState world,
            PersonState person,
            List<PlayerActionOption> actions,
            bool stationary)
        {
            if (!HasChoice(world, person.Id, "local_relief"))
            {
                actions.Add(Option(
                    PlayerActionIds.LocalReliefHelp,
                    "地方事件：拿出口粮相助",
                    "附近家庭请求救济；消耗2份口粮并改善地方治安。",
                    stationary && person.Provisions >= 2,
                    person.Provisions < 2
                        ? "至少需要2份口粮。"
                        : "旅途中无法介入本地求助。"));
                actions.Add(Option(
                    PlayerActionIds.LocalReliefDecline,
                    "地方事件：婉拒求助",
                    "保留自己的物资，本事件仍会被世界记录。",
                    stationary,
                    "旅途中无法作出本地选择。"));
            }

            if (world.AbsoluteDay >= 10 &&
                CanWitnessTaipingRumor(person) &&
                !HasChoice(world, person.Id, "taiping"))
            {
                actions.Add(Option(
                    PlayerActionIds.HistoricalReport,
                    "历史传闻：上报太平道活动",
                    "把已获知的活动迹象报告本地官府，提高警戒。",
                    stationary,
                    "旅途中无法向本地官府报告。"));
                actions.Add(Option(
                    PlayerActionIds.HistoricalObserve,
                    "历史传闻：继续观察",
                    "暂不介入，保留对传闻的个人判断。",
                    stationary,
                    "旅途中无法处理这条本地传闻。"));
            }
        }

        private static bool CanWitnessTaipingRumor(PersonState person)
        {
            return person.LocationId == "location.guangzong" ||
                person.LocationId == "location.xiaquyang";
        }

        private static void AddCareActions(
            WorldState world,
            PersonState person,
            List<PlayerActionOption> actions,
            bool stationary)
        {
            if (person.HealthBasisPoints >= 10_000)
            {
                return;
            }

            var location = FindLocation(world, person.LocationId);
            var herbs = FindListing(
                world, person.LocationId, "commodity.herbs");
            actions.Add(Option(
                PlayerActionIds.ClinicCare,
                "就近就医",
                "支付药费并消耗当前市场药材，休养3天。",
                stationary &&
                    (location.Features & LocationFeature.Clinic) != 0 &&
                    herbs != null && herbs.Stock > 0 && person.Wealth >= herbs.Price,
                "需要本地医馆、至少1份药材和足够药费。"));

            var service = FindMilitaryService(world, person.Id);
            var fieldCareUnavailableReason = FieldCareUnavailableReason(
                world, person, service, stationary);
            actions.Add(Option(
                PlayerActionIds.FieldCare,
                "接受随军治疗",
                "由同地军医使用军中药材治疗伤兵。",
                string.IsNullOrEmpty(fieldCareUnavailableReason),
                fieldCareUnavailableReason));

            var family = FindFamily(world, person.FamilyId, false);
            actions.Add(Option(
                PlayerActionIds.HomeRest,
                "回家休养七天",
                "消耗2份个人口粮，恢复部分健康。",
                stationary && family != null &&
                    family.LocationId == person.LocationId &&
                    person.Provisions >= 2,
                "必须回到家庭所在地并准备2份口粮。"));
        }

        private static string FieldCareUnavailableReason(
            WorldState world,
            PersonState person,
            MilitaryServiceState service,
            bool stationary)
        {
            if (!stationary)
            {
                return "旅途中不能接受集中随军治疗。";
            }
            if (service == null ||
                service.Status != MilitaryServiceStatus.Wounded)
            {
                return "只有登记在册的随军伤员可以接受治疗。";
            }

            var army = FindArmy(world, service.ArmyId);
            if (FindArmyMarch(world, army.Id) != null)
            {
                return "军队行军期间不能组织集中治疗。";
            }
            if (FindFieldCarePhysician(world, army, person.Id) == null)
            {
                return "军中当前没有同地且具备治疗能力的医者。";
            }
            if (world.MilitaryMedicalInitialized &&
                !HasAvailableArmyMedicine(world, army))
            {
                return "军中当前没有可用且未预留的药材。";
            }
            return string.Empty;
        }

        private static PersonState FindFieldCarePhysician(
            WorldState world,
            ArmyState army,
            string patientPersonId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                var candidate = world.People[i];
                if (candidate.Id != patientPersonId &&
                    candidate.IsAlive &&
                    candidate.LocationId == army.LocationId &&
                    FindJourney(world, candidate.Id) == null &&
                    Math.Max(
                        candidate.MedicalSkillBasisPoints,
                        candidate.ProfessionalSkills.Medicine) >= 2_500)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static bool HasAvailableArmyMedicine(
            WorldState world,
            ArmyState army)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId ==
                        army.MedicalInventoryContainerId &&
                    batch.OwnerOrganizationId == army.OrganizationId &&
                    batch.ProductDefinitionId == CoreProductionContent
                        .HerbalMedicineMaterialProductId &&
                    batch.Quantity > batch.ReservedQuantity)
                {
                    return true;
                }
            }
            return false;
        }

        private static string TradeBuyUnavailableReason(
            WorldState world,
            PersonState person,
            int quantity)
        {
            if (!HasMarket(world, person.LocationId))
            {
                return "当前地点没有可以交易的市场。";
            }
            var listing = FindListing(
                world, person.LocationId, "commodity.cloth");
            if (listing == null || listing.Stock < quantity)
            {
                return "当前市场的布帛存货不足。";
            }
            var cost = checked((long)listing.Price * quantity);
            if (person.Wealth < cost)
            {
                return "需要" + cost + "钱，当前资金不足。";
            }

            var commodity = world.Commodities.Find(item =>
                item.Id == "commodity.cloth");
            if (commodity == null)
            {
                return "布帛商品定义缺失。";
            }
            var currentWeight = CurrentCargoWeight(world, person.Id);
            var addedWeight = checked((long)commodity.UnitWeight * quantity);
            if (currentWeight + addedWeight > person.CargoCapacity)
            {
                return "随身货物将超过人物载货上限。";
            }
            return string.Empty;
        }

        private static long CurrentCargoWeight(
            WorldState world,
            string personId)
        {
            long total = 0;
            for (var inventoryIndex = 0;
                 inventoryIndex < world.Inventories.Count;
                 inventoryIndex++)
            {
                var inventory = world.Inventories[inventoryIndex];
                if (inventory.OwnerPersonId != personId)
                {
                    continue;
                }
                var commodity = world.Commodities.Find(item =>
                    item.Id == inventory.CommodityId);
                if (commodity != null)
                {
                    total = checked(
                        total + (long)commodity.UnitWeight * inventory.Quantity);
                }
            }
            return total;
        }

        private string AcceptFirstTask(WorldState world, PersonState person)
        {
            var definition = FindAcceptableTask(world, person);
            if (definition == null)
            {
                return "当前没有可接受的任务。";
            }
            return _tasks.TryAccept(
                world,
                new StableId(person.Id),
                new StableId(definition.Id)).Message;
        }

        private string WorkConstruction(WorldState world, PersonState person)
        {
            var project = FindLocalProject(world, person.LocationId);
            if (project == null)
            {
                var location = FindLocation(world, person.LocationId);
                var feature = ConstructionSystem.RecommendFeature(
                    location,
                    MapPerspectiveSystem.RecommendForPlayer(world, person.Id));
                project = _construction.StartProject(
                    world,
                    new StableId(person.Id),
                    new StableId(location.Id),
                    feature);
            }
            var contribution = _construction.Contribute(
                world,
                new StableId(project.Id),
                new StableId(person.Id),
                20,
                30);
            return contribution.Summary + " 本次投入20钱和一天劳力。";
        }

        private string StartFarmSeason(WorldState world, PersonState person)
        {
            var family = FindFamily(world, person.FamilyId, true);
            var village = FindVillage(world, family.VillageId);
            var field = FindVillageFacility(
                world, village.Id, family.Id, VillageFacilityKind.Farmland);
            var granary = FindVillageFacility(
                world,
                village.Id,
                family.Id,
                VillageFacilityKind.HouseholdGranary);
            var landUnits = Math.Min(
                4,
                Math.Min(family.FarmlandUnits, field.Capacity));
            var order = new AgricultureProductionSystem(world.MasterSeed)
                .CreateOrder(
                    world,
                    village.Id,
                    family.Id,
                    field.Id,
                    granary.Id,
                    person.Id,
                    CoreProductionContent.WheatCropId,
                    CoreProductionContent.PrototypeNorthernWheatVarietyId,
                    CoreProductionContent.GrowWheatRecipeId,
                    CoreProductionContent.PrototypeDrylandMethodId,
                    ProductionControlMode.PersonalLabor,
                    landUnits,
                    new[] { person.Id },
                    world.AbsoluteDay + 180);
            return "已建立一季麦作工单，投入" + landUnits + "单位土地与" +
                order.SeedQuantityCommitted + "单位种子。";
        }

        private string CompleteFarmSeason(WorldState world, PersonState person)
        {
            var family = FindFamily(world, person.FamilyId, true);
            var order = FindActiveFarmOrder(world, family.Id);
            if (order == null)
            {
                return "当前没有等待收获的农业工单。";
            }
            var remaining = checked((int)Math.Max(
                0, order.HarvestDay - world.AbsoluteDay));
            if (remaining > 0)
            {
                _simulator.AdvanceDays(world, remaining);
            }
            new AgricultureProductionSystem(world.MasterSeed)
                .ResolveDueOrders(world, order.VillageId);
            if (order.Status == ProductionOrderStatus.Completed &&
                order.StoredQuantity > 0 &&
                !HasInventoryBridge(world, order.Id))
            {
                new ProductInventorySystem()
                    .ConvertCompletedAgricultureHarvestToBatches(world, order.Id);
            }
            return "麦作已经收获：入库" + order.StoredQuantity +
                "，损耗" + order.LostQuantity + "。";
        }

        private string StudyOneMonth(WorldState world, PersonState person)
        {
            EducationPlanState plan = null;
            for (var i = 0; i < world.EducationPlans.Count; i++)
            {
                if (world.EducationPlans[i].StudentPersonId == person.Id &&
                    world.EducationPlans[i].Status == EducationPlanStatus.Active)
                {
                    plan = world.EducationPlans[i];
                    break;
                }
            }
            if (plan == null)
            {
                var discipline = SelectScholarDiscipline(world, person);
                var teacher = FindTeacherFor(world, person, discipline);
                plan = _education.StartPlan(
                    world,
                    new StableId(person.Id),
                    discipline,
                    10,
                    teacher == null ? string.Empty : teacher.Id);
            }
            var before = ProfessionalSkillAccess.Get(
                person.ProfessionalSkills, plan.Discipline);
            _simulator.AdvanceDays(world, 30);
            var after = ProfessionalSkillAccess.Get(
                FindPerson(world, person.Id).ProfessionalSkills,
                plan.Discipline);
            return "完成一个月研习，" + plan.Discipline + "熟练度由" + before +
                "提升至" + after + "。";
        }

        private static ProfessionalDiscipline SelectScholarDiscipline(
            WorldState world,
            PersonState person)
        {
            var disciplines = new[]
            {
                ProfessionalDiscipline.Scholarship,
                ProfessionalDiscipline.Intelligence,
                ProfessionalDiscipline.Administration,
                ProfessionalDiscipline.Negotiation
            };
            for (var i = 0; i < disciplines.Length; i++)
            {
                if (ProfessionalSkillAccess.Get(
                        person.ProfessionalSkills, disciplines[i]) <
                    EducationSystem.SelfStudyLimitBasisPoints ||
                    FindTeacherFor(world, person, disciplines[i]) != null)
                {
                    return disciplines[i];
                }
            }
            return ProfessionalDiscipline.Scholarship;
        }

        private static PersonState FindTeacherFor(
            WorldState world,
            PersonState student,
            ProfessionalDiscipline discipline)
        {
            var studentSkill = ProfessionalSkillAccess.Get(
                student.ProfessionalSkills, discipline);
            PersonState best = null;
            for (var i = 0; i < world.People.Count; i++)
            {
                var candidate = world.People[i];
                if (candidate.Id == student.Id || !candidate.IsAlive ||
                    candidate.LocationId != student.LocationId ||
                    FindJourney(world, candidate.Id) != null)
                {
                    continue;
                }
                var skill = ProfessionalSkillAccess.Get(
                    candidate.ProfessionalSkills, discipline);
                if (skill >= EducationSystem.SelfStudyLimitBasisPoints &&
                    skill > studentSkill &&
                    (best == null || skill < ProfessionalSkillAccess.Get(
                        best.ProfessionalSkills, discipline)))
                {
                    best = candidate;
                }
            }
            return best;
        }

        private string AdvanceArmy(WorldState world, PersonState person)
        {
            var service = FindMilitaryService(world, person.Id);
            var army = FindArmy(world, service.ArmyId);
            string routeId;
            string destinationId;
            if (army.LocationId == "location.zhongshan")
            {
                routeId = "route.zhongshan_anping";
                destinationId = "location.anping";
            }
            else
            {
                routeId = "route.anping_guangzong";
                destinationId = "location.guangzong";
            }
            _armies.StartMarch(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                new StableId(routeId),
                new StableId(destinationId));
            var days = 0;
            while (FindArmyMarch(world, army.Id) != null && days < 30)
            {
                _simulator.AdvanceDays(world, 1);
                days++;
            }
            if (destinationId == "location.guangzong" &&
                world.AbsoluteDay < 30)
            {
                _simulator.AdvanceDays(world, checked((int)(30 - world.AbsoluteDay)));
            }
            return army.DisplayName + "抵达" +
                FindLocation(world, destinationId).DisplayName +
                "，全军人物和补给已同步移动。";
        }

        private static string ResolveBattle(WorldState world, PersonState person)
        {
            var service = FindMilitaryService(world, person.Id);
            var army = FindArmy(world, service.ArmyId);
            var enemy = FindArmy(world, "army.yellow_turban_guangzong");
            return new BattleResolver(world.MasterSeed).Resolve(
                world,
                new StableId(army.CommanderPersonId),
                new StableId(army.Id),
                new StableId(enemy.Id)).Summary;
        }

        private static string RecordChoice(
            WorldState world,
            PersonState person,
            string choiceKey,
            string eventSummary,
            bool consumeProvisions,
            bool improveOrder)
        {
            if (consumeProvisions)
            {
                person.Provisions -= 2;
            }
            var location = FindLocation(world, person.LocationId);
            if (consumeProvisions || improveOrder)
            {
                location.PublicOrderBasisPoints = Math.Min(
                    10_000,
                    location.PublicOrderBasisPoints +
                    (improveOrder ? 150 : 100));
            }
            var id = "life_event.player_choice." + choiceKey + "." + person.Id;
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = id,
                Type = LifeEventType.Recovery,
                Day = world.AbsoluteDay,
                PrimaryPersonId = person.Id,
                FamilyId = person.FamilyId,
                Summary = eventSummary
            });
            return id;
        }

        private void AddRecoveryEvent(
            WorldState world,
            PersonState person,
            string key,
            string summary)
        {
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = "life_event.player_recovery." + key + "." +
                    person.Id + "." + world.AbsoluteDay,
                Type = LifeEventType.Recovery,
                Day = world.AbsoluteDay,
                PrimaryPersonId = person.Id,
                FamilyId = person.FamilyId,
                Summary = summary
            });
        }

        private string ReceiveClinicCare(WorldState world, PersonState person)
        {
            var listing = FindListing(
                world, person.LocationId, "commodity.herbs");
            person.Wealth -= listing.Price;
            listing.Stock--;
            _simulator.AdvanceDays(world, 3);
            person = FindPerson(world, person.Id);
            var recovered = Math.Min(2_500, 10_000 - person.HealthBasisPoints);
            person.HealthBasisPoints += recovered;
            AddRecoveryEvent(
                world, person, "clinic", "在本地医馆用药休养，健康有所恢复。");
            return "就医3天，支付" + listing.Price + "钱并消耗1份市场药材，" +
                "健康恢复" + recovered + "。";
        }

        private MedicalTreatmentResult ReceiveFieldCare(
            WorldState world,
            PersonState person)
        {
            var service = FindMilitaryService(world, person.Id);
            if (service == null)
            {
                return new MedicalTreatmentResult(
                    false, 0, 0, 0, "当前人物没有可以治疗的服役记录。");
            }
            var army = FindArmy(world, service.ArmyId);
            var physician = FindFieldCarePhysician(world, army, person.Id);
            if (physician == null)
            {
                return new MedicalTreatmentResult(
                    false, 0, 0, 0, "军中当前没有具备治疗能力且同地的医者。");
            }
            var treatment = new MedicalSystem(world.MasterSeed)
                .TreatArmyWoundedPerson(
                    world,
                    new StableId(physician.Id),
                    new StableId(army.Id),
                    new StableId(person.Id));
            if (treatment.Success)
            {
                _simulator.AdvanceDays(world, 1);
            }
            return treatment;
        }

        private string RecoverAtHome(WorldState world, PersonState person)
        {
            person.Provisions -= 2;
            _simulator.AdvanceDays(world, 7);
            person = FindPerson(world, person.Id);
            var recovered = Math.Min(1_500, 10_000 - person.HealthBasisPoints);
            person.HealthBasisPoints += recovered;
            AddRecoveryEvent(
                world, person, "home", "回到家中休养七日，健康有所恢复。");
            return "在家休养7天，消耗2份口粮，健康恢复" + recovered + "。";
        }

        private static PlayerActionOption Option(
            string id,
            string name,
            string description,
            bool available,
            string reason)
        {
            return new PlayerActionOption
            {
                Id = id,
                DisplayName = name,
                Description = description,
                IsAvailable = available,
                UnavailableReason = reason ?? string.Empty,
                Motivation = description ?? string.Empty,
                ExpectedOutcome = description ?? string.Empty,
                Cost = "行动会按说明消耗时间或资源。",
                KnownRisk = "结果由当前世界事实与行动规则结算。",
                PresentationCue = "action.default",
                UnlockHint = reason ?? string.Empty
            };
        }

        private static PlayerActionResult Failure(string id, string summary)
        {
            return new PlayerActionResult
            {
                Success = false,
                ActionId = id,
                Summary = summary ?? string.Empty,
                WorldEventId = string.Empty,
                ResultId = string.Empty,
                PresentationCue = string.Empty,
                Detail = "行动未提交，世界状态没有变化。"
            };
        }

        private static TaskDefinitionState FindAcceptableTask(
            WorldState world,
            PersonState person)
        {
            for (var i = 0; i < world.TaskDefinitions.Count; i++)
            {
                var definition = world.TaskDefinitions[i];
                if (!definition.IsAvailable ||
                    definition.OriginLocationId != person.LocationId)
                {
                    continue;
                }
                if (!definition.RequiresMembership ||
                    HasMembership(world, person.Id, definition))
                {
                    return definition;
                }
            }
            return null;
        }

        private static bool HasMembership(
            WorldState world,
            string personId,
            TaskDefinitionState definition)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId == personId &&
                    membership.OrganizationId == definition.IssuerOrganizationId &&
                    (string.IsNullOrEmpty(definition.RequiredPositionId) ||
                     membership.PositionId == definition.RequiredPositionId))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasPosition(
            WorldState world,
            string personId,
            string positionFragment)
        {
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == personId &&
                    world.Memberships[i].PositionId.IndexOf(
                        positionFragment, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasMarket(WorldState world, string locationId) =>
            (FindLocation(world, locationId).Features & LocationFeature.Market) != 0;

        private static bool HasChoice(
            WorldState world,
            string personId,
            string choicePrefix)
        {
            var token = "life_event.player_choice." + choicePrefix;
            for (var i = 0; i < world.LifeEvents.Count; i++)
            {
                if (world.LifeEvents[i].PrimaryPersonId == personId &&
                    world.LifeEvents[i].Id.StartsWith(
                        token, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasInventoryBridge(
            WorldState world,
            string orderId)
        {
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                if (world.InventoryTransactions[i].SourceWorkOrderId == orderId)
                {
                    return true;
                }
            }
            return false;
        }

        private static TaskInstanceState FindActiveTask(
            WorldState world,
            string personId)
        {
            return world.Tasks.Find(item =>
                item.AssigneePersonId == personId &&
                item.Status == TaskStatus.Active);
        }

        private static AgricultureWorkOrderState FindActiveFarmOrder(
            WorldState world,
            string familyId)
        {
            return world.AgricultureWorkOrders.Find(item =>
                item.FamilyId == familyId &&
                item.Status == ProductionOrderStatus.Active);
        }

        private static ConstructionProjectState FindLocalProject(
            WorldState world,
            string locationId)
        {
            return world.ConstructionProjects.Find(item =>
                item.LocationId == locationId && !item.IsCompleted);
        }

        private static JourneyState FindJourney(
            WorldState world,
            string personId)
        {
            return world.Journeys.Find(item => item.PersonId == personId);
        }

        private static ArmyMarchState FindArmyMarch(
            WorldState world,
            string armyId)
        {
            return world.ArmyMarches.Find(item => item.ArmyId == armyId);
        }

        private static MilitaryServiceState FindMilitaryService(
            WorldState world,
            string personId)
        {
            return world.MilitaryServices.Find(item =>
                item.PersonId == personId &&
                item.Status != MilitaryServiceStatus.Dead &&
                item.Status != MilitaryServiceStatus.Retired);
        }

        private static PersonState FindPerson(
            WorldState world,
            string personId)
        {
            var person = world.People.Find(item => item.Id == personId);
            return person ?? throw new InvalidOperationException(
                "Missing person " + personId + ".");
        }

        private static FamilyState FindFamily(
            WorldState world,
            string familyId,
            bool required)
        {
            var family = world.Families.Find(item => item.Id == familyId);
            if (family == null && required)
            {
                throw new InvalidOperationException(
                    "Missing family " + familyId + ".");
            }
            return family;
        }

        private static VillageState FindVillage(
            WorldState world,
            string villageId)
        {
            var village = world.Villages.Find(item => item.Id == villageId);
            return village ?? throw new InvalidOperationException(
                "Missing village " + villageId + ".");
        }

        private static VillageFacilityState FindVillageFacility(
            WorldState world,
            string villageId,
            string familyId,
            VillageFacilityKind kind)
        {
            var facility = world.VillageFacilities.Find(item =>
                item.VillageId == villageId && item.Kind == kind &&
                (kind == VillageFacilityKind.Farmland ||
                 item.OwnerFamilyId == familyId));
            return facility ?? throw new InvalidOperationException(
                "Missing village facility " + kind + ".");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            var location = world.Locations.Find(item => item.Id == locationId);
            return location ?? throw new InvalidOperationException(
                "Missing location " + locationId + ".");
        }

        private static ArmyState FindArmy(
            WorldState world,
            string armyId,
            bool required = true)
        {
            var army = world.Armies.Find(item => item.Id == armyId);
            if (army == null && required)
            {
                throw new InvalidOperationException(
                    "Missing army " + armyId + ".");
            }
            return army;
        }

        private static MarketListingState FindListing(
            WorldState world,
            string locationId,
            string commodityId)
        {
            return world.MarketListings.Find(item =>
                item.LocationId == locationId &&
                item.CommodityId == commodityId);
        }
    }
}
