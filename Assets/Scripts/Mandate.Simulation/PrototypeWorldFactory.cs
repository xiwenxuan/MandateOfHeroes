using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class PrototypeWorldFactory
    {
        public static WorldState Create184World(ulong masterSeed = 184_001UL)
        {
            return Create184World(masterSeed, null);
        }

        public static WorldState Create184World(
            ulong masterSeed,
            Func<WorldState, IPersonRepository> personRepositoryFactory)
        {
            var world = WorldState.Create(masterSeed);

            AddLocation(
                world,
                "location.zhuo",
                "涿县",
                20_000,
                100,
                1_600,
                1_300,
                LocationKind.CountySeat,
                TerrainKind.Plains,
                3,
                LocationFeature.Government |
                LocationFeature.Market |
                LocationFeature.Garrison |
                LocationFeature.Farmland |
                LocationFeature.RelayStation);
            AddLocation(
                world,
                "location.zhongshan",
                "中山国节点",
                35_000,
                96,
                3_400,
                2_800,
                LocationKind.RegionalSeat,
                TerrainKind.Plains,
                4,
                LocationFeature.Government |
                LocationFeature.Market |
                LocationFeature.Garrison |
                LocationFeature.Farmland |
                LocationFeature.Workshop);
            AddLocation(
                world,
                "location.anping",
                "安平国节点",
                30_000,
                102,
                5_200,
                4_000,
                LocationKind.RegionalSeat,
                TerrainKind.Riverland,
                4,
                LocationFeature.Government |
                LocationFeature.Market |
                LocationFeature.Garrison |
                LocationFeature.Farmland);
            AddLocation(
                world,
                "location.xiaquyang",
                "下曲阳",
                18_000,
                112,
                6_600,
                3_800,
                LocationKind.CountySeat,
                TerrainKind.Riverland,
                3,
                LocationFeature.Government |
                LocationFeature.Garrison |
                LocationFeature.Farmland);
            AddLocation(
                world,
                "location.guangzong",
                "广宗",
                25_000,
                118,
                7_300,
                6_200,
                LocationKind.CountySeat,
                TerrainKind.Plains,
                4,
                LocationFeature.Market |
                LocationFeature.Garrison |
                LocationFeature.Farmland |
                LocationFeature.Clinic);
            AddLocation(
                world,
                "location.ye",
                "邺县",
                50_000,
                94,
                8_700,
                8_100,
                LocationKind.RegionalSeat,
                TerrainKind.Riverland,
                5,
                LocationFeature.Government |
                LocationFeature.Market |
                LocationFeature.Garrison |
                LocationFeature.Farmland |
                LocationFeature.Workshop |
                LocationFeature.RelayStation |
                LocationFeature.Fortification);

            AddCommodity(world, "commodity.grain", "粮食", 100, 1);
            AddCommodity(
                world,
                "commodity.cloth",
                "布帛",
                180,
                2,
                CoreProductionContent.PlainClothProductId);
            AddCommodity(world, "commodity.salt", "盐", 140, 2);
            AddCommodity(world, "commodity.horses", "战马", 600, 10);
            AddCommodity(world, "commodity.herbs", "药材", 120, 1);
            AddMarketBundle(world, "location.zhuo", 100, 195, 155, 680, 130);
            AddMarketBundle(world, "location.zhongshan", 96, 165, 140, 520, 110);
            AddMarketBundle(world, "location.anping", 102, 180, 150, 590, 115);
            AddMarketBundle(world, "location.xiaquyang", 112, 205, 165, 640, 125);
            AddMarketBundle(world, "location.guangzong", 118, 220, 175, 700, 180);
            AddMarketBundle(world, "location.ye", 94, 170, 135, 610, 100);

            AddRoute(world, "route.zhuo_zhongshan", "location.zhuo", "location.zhongshan", 140);
            AddRoute(world, "route.zhongshan_anping", "location.zhongshan", "location.anping", 90);
            AddRoute(world, "route.anping_xiaquyang", "location.anping", "location.xiaquyang", 80);
            AddRoute(world, "route.xiaquyang_guangzong", "location.xiaquyang", "location.guangzong", 150);
            AddRoute(world, "route.anping_guangzong", "location.anping", "location.guangzong", 170);
            AddRoute(world, "route.guangzong_ye", "location.guangzong", "location.ye", 110);

            AddPerson(world, "person.liu_bei", "刘备", "location.zhuo", -8_400);
            AddPerson(world, "person.guan_yu", "关羽", "location.zhuo", -8_500);
            AddPerson(world, "person.zhang_fei", "张飞", "location.zhuo", -7_500);
            AddPerson(world, "person.jian_yong", "简雍", "location.zhuo", -8_000);
            AddPerson(world, "person.zou_jing", "邹靖", "location.zhuo", -12_000);
            AddPerson(
                world,
                "person.zhang_shiping",
                "张世平",
                "location.zhongshan",
                -12_000,
                2_000,
                cargoCapacity: 120);
            AddPerson(
                world,
                "person.su_shuang",
                "苏双",
                "location.zhongshan",
                -12_000,
                2_000,
                cargoCapacity: 120);
            AddPerson(world, "person.lu_zhi", "卢植", "location.guangzong", -20_000);
            AddPerson(world, "person.zhang_jue", "张角", "location.guangzong", -18_000);
            AddPerson(world, "person.guo_dian", "郭典", "location.xiaquyang", -18_000);
            AddPerson(
                world,
                "person.generated.physician_001",
                "陈医师",
                "location.guangzong",
                -11_000,
                1_000,
                cargoCapacity: 30,
                medicalSkillBasisPoints: 7_500);
            AddPerson(
                world,
                "person.generated.farmer_001",
                "田大",
                "location.zhuo",
                -9_000,
                0,
                PersonGender.Male,
                "person.generated.farmer_002");
            AddPerson(
                world,
                "person.generated.farmer_002",
                "禾娘",
                "location.zhuo",
                -8_300,
                0,
                PersonGender.Female,
                "person.generated.farmer_001");

            world.Families.Add(new FamilyState
            {
                Id = "family.liu_household",
                DisplayName = "刘氏家户",
                HeadPersonId = "person.liu_bei",
                Wealth = 1_000,
                MemberIds = { "person.liu_bei" }
            });
            world.Families.Add(new FamilyState
            {
                Id = "family.zhang_household",
                DisplayName = "张氏家户",
                HeadPersonId = "person.zhang_fei",
                Wealth = 2_000,
                MemberIds = { "person.zhang_fei" }
            });
            world.Families.Add(new FamilyState
            {
                Id = "family.zhuo_farm_household",
                DisplayName = "涿县田户",
                HeadPersonId = "person.generated.farmer_001",
                Wealth = 20,
                MemberIds =
                {
                    "person.generated.farmer_001",
                    "person.generated.farmer_002"
                }
            });
            SynchronizeFamilyReferences(world);

            AddRelationship(
                world, "person.liu_bei", "person.guan_yu", 6_000, 5_500, 5_000);
            AddRelationship(
                world, "person.guan_yu", "person.liu_bei", 6_000, 6_000, 6_000);
            AddRelationship(
                world, "person.liu_bei", "person.zhang_fei", 5_500, 5_000, 4_500);
            AddRelationship(
                world, "person.zhang_fei", "person.liu_bei", 6_000, 5_000, 5_000);

            AddOrganization(
                world,
                "organization.zhuo_county_office",
                "涿县官署",
                OrganizationType.Government,
                "location.zhuo",
                string.Empty);
            AddPosition(
                world,
                "position.zhuo_county_clerk",
                "organization.zhuo_county_office",
                "书佐",
                0,
                20);
            AddOrganization(
                world,
                "organization.youzhou_field_force",
                "幽州官军",
                OrganizationType.Military,
                "location.zhuo",
                "person.zou_jing");
            AddPosition(
                world,
                "position.youzhou_soldier",
                "organization.youzhou_field_force",
                "士卒",
                0,
                1_000);
            AddPosition(
                world,
                "position.youzhou_commander",
                "organization.youzhou_field_force",
                "统兵官",
                10,
                1);
            world.Memberships.Add(new MembershipState
            {
                Id = "membership.person.zou_jing.organization.youzhou_field_force",
                PersonId = "person.zou_jing",
                OrganizationId = "organization.youzhou_field_force",
                PositionId = "position.youzhou_commander",
                JoinedDay = 0,
                LoyaltyBasisPoints = 7_000
            });
            AddOrganization(
                world,
                "organization.zhongshan_merchants",
                "中山商行",
                OrganizationType.Merchant,
                "location.zhongshan",
                "person.zhang_shiping");
            AddOrganization(
                world,
                "organization.guangzong_relief_camp",
                "广宗救济营",
                OrganizationType.Government,
                "location.guangzong",
                string.Empty);
            AddOrganization(
                world,
                "organization.han_jizhou_field_force",
                "冀州讨逆军",
                OrganizationType.Military,
                "location.xiaquyang",
                "person.lu_zhi");
            AddOrganization(
                world,
                "organization.taiping_yellow_turban",
                "太平道黄巾军",
                OrganizationType.Military,
                "location.guangzong",
                "person.zhang_jue");
            AddArmy(
                world,
                "army.han_jizhou_vanguard",
                "冀州官军前锋",
                "organization.han_jizhou_field_force",
                "person.guo_dian",
                "location.xiaquyang",
                5_500,
                7_000,
                6_500,
                6_500,
                30_000,
                true);
            AddArmy(
                world,
                "army.youzhou_reinforcement",
                "幽州援军",
                "organization.youzhou_field_force",
                "person.zou_jing",
                "location.zhongshan",
                3_000,
                5_000,
                6_000,
                5_500,
                8_000,
                true);
            AddArmy(
                world,
                "army.yellow_turban_guangzong",
                "广宗黄巾主力",
                "organization.taiping_yellow_turban",
                "person.zhang_jue",
                "location.guangzong",
                8_000,
                10_000,
                7_500,
                4_000,
                25_000,
                false);
            AddPosition(
                world,
                "position.zhongshan_trader",
                "organization.zhongshan_merchants",
                "行商",
                0,
                20);
            AddPosition(
                world,
                "position.guangzong_physician",
                "organization.guangzong_relief_camp",
                "医者",
                0,
                10);
            AddMembership(
                world,
                "person.zhang_shiping",
                "organization.zhongshan_merchants",
                "position.zhongshan_trader",
                7_000);
            AddMembership(
                world,
                "person.su_shuang",
                "organization.zhongshan_merchants",
                "position.zhongshan_trader",
                7_000);
            AddMembership(
                world,
                "person.generated.physician_001",
                "organization.guangzong_relief_camp",
                "position.guangzong_physician",
                7_500);

            AddTaskDefinition(
                world,
                "task_definition.verify_households",
                "核对涿县户籍",
                TaskKind.LocalWork,
                "organization.zhuo_county_office",
                "position.zhuo_county_clerk",
                "location.zhuo",
                string.Empty,
                3,
                10,
                100,
                2);
            AddTaskDefinition(
                world,
                "task_definition.deliver_military_grain",
                "运送官军粮草至中山",
                TaskKind.TravelDelivery,
                "organization.youzhou_field_force",
                "position.youzhou_soldier",
                "location.zhuo",
                "location.zhongshan",
                1,
                15,
                300,
                5,
                targetArmyId: "army.youzhou_reinforcement",
                armyProvisionReward: 1_000);
            AddTaskDefinition(
                world,
                "task_definition.deliver_horses",
                "运送马匹至涿县",
                TaskKind.TravelDelivery,
                "organization.zhongshan_merchants",
                "position.zhongshan_trader",
                "location.zhongshan",
                "location.zhuo",
                1,
                15,
                500,
                3);
            AddTaskDefinition(
                world,
                "task_definition.recruit_volunteers",
                "征募乡勇",
                TaskKind.LocalWork,
                "organization.youzhou_field_force",
                string.Empty,
                "location.zhuo",
                string.Empty,
                2,
                7,
                150,
                3,
                false,
                false);
            AddTaskDefinition(
                world,
                "task_definition.escort_refugees",
                "护送难民至安平",
                TaskKind.TravelDelivery,
                "organization.guangzong_relief_camp",
                string.Empty,
                "location.guangzong",
                "location.anping",
                1,
                20,
                260,
                4,
                false,
                false);
            AddTaskDefinition(
                world,
                "task_definition.treat_wounded",
                "救治广宗伤兵",
                TaskKind.LocalWork,
                "organization.guangzong_relief_camp",
                string.Empty,
                "location.guangzong",
                string.Empty,
                3,
                10,
                220,
                3,
                false,
                false);
            AddTaskDefinition(
                world,
                "task_definition.investigate_taiping",
                "调查太平道联络",
                TaskKind.LocalWork,
                "organization.zhuo_county_office",
                string.Empty,
                "location.zhuo",
                string.Empty,
                2,
                8,
                180,
                2,
                false,
                false);
            AddTaskDefinition(
                world,
                "task_definition.wartime_grain",
                "战时运粮至涿县",
                TaskKind.TravelDelivery,
                "organization.zhongshan_merchants",
                string.Empty,
                "location.zhongshan",
                "location.zhuo",
                1,
                15,
                360,
                3,
                false,
                false);

            AddHistoricalEvent(
                world,
                "historical_event.yellow_turban_outbreak",
                "黄巾起事",
                30,
                35,
                string.Empty,
                "太平道提前发动，各地进入战乱状态。",
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustPublicOrder,
                    TargetId = "location.guangzong",
                    Value = -2_500
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustGrainPrice,
                    TargetId = "location.guangzong",
                    Value = 30
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustPublicOrder,
                    TargetId = "location.xiaquyang",
                    Value = -1_500
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetWarPressure,
                    TargetId = "person.liu_bei",
                    Value = 8_000
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetWarPressure,
                    TargetId = "person.zou_jing",
                    Value = 9_000
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetTaskAvailability,
                    TargetId = "task_definition.recruit_volunteers",
                    Value = 1
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetTaskAvailability,
                    TargetId = "task_definition.escort_refugees",
                    Value = 1
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetTaskAvailability,
                    TargetId = "task_definition.treat_wounded",
                    Value = 1
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetTaskAvailability,
                    TargetId = "task_definition.investigate_taiping",
                    Value = 1
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetTaskAvailability,
                    TargetId = "task_definition.wartime_grain",
                    Value = 1
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.SetArmyMobilized,
                    TargetId = "army.yellow_turban_guangzong",
                    Value = 1
                });
            AddHistoricalEvent(
                world,
                "historical_event.guangzong_siege",
                "卢植围攻广宗",
                60,
                75,
                "historical_event.yellow_turban_outbreak",
                "官军推进至广宗并建立围城工事。",
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustRouteSecurity,
                    TargetId = "route.anping_guangzong",
                    Value = -2_000
                },
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustGrainPrice,
                    TargetId = "location.guangzong",
                    Value = 20
                });
            AddHistoricalEvent(
                world,
                "historical_event.lu_zhi_recalled",
                "卢植被征还",
                90,
                105,
                "historical_event.guangzong_siege",
                "卢植离开前线，广宗官军指挥发生变化。",
                new HistoricalEffectState
                {
                    Type = HistoricalEffectType.AdjustRouteSecurity,
                    TargetId = "route.xiaquyang_guangzong",
                    Value = -1_000
                });

            CharacterAbilityBootstrap.InitializeWorld(world);
            new PopulationLedgerSystem().InitializeFromLocationSummaries(world);
            var people = personRepositoryFactory == null
                ? null
                : personRepositoryFactory(world);
            if (personRepositoryFactory != null && people == null)
            {
                throw new InvalidOperationException(
                    "Person repository factory returned null.");
            }

            new MilitaryServiceSystem(people).InitializePrototype(world);
            new MilitaryEquipmentSystem(people).InitializePrototype(world);
            new MilitaryProcurementSystem(people).InitializePrototypeSupply(world);
            MilitaryEquipmentRepairSystem.InitializePrototypeWorkshop(world);
            UpstreamResourceProductionSystem.InitializePrototype(world);
            LivestockProductionSystem.InitializePrototype(world);
            MerchantTownOperationSystem.InitializePrototype(world);
            new MedicalSystem(world.MasterSeed, people)
                .InitializePrototypeSupply(world);
            world.Validate();
            return world;
        }

        private static void AddLocation(
            WorldState world,
            string id,
            string displayName,
            int population,
            int grainPrice,
            int mapXBasisPoints,
            int mapYBasisPoints,
            LocationKind kind,
            TerrainKind terrain,
            int strategicImportance,
            LocationFeature features)
        {
            world.Locations.Add(new LocationState
            {
                Id = id,
                DisplayName = displayName,
                Kind = kind,
                Terrain = terrain,
                StrategicImportance = strategicImportance,
                Features = features,
                Population = population,
                GrainPrice = grainPrice,
                PublicOrderBasisPoints = 5_000,
                MapXBasisPoints = mapXBasisPoints,
                MapYBasisPoints = mapYBasisPoints
            });
        }

        private static void AddPerson(
            WorldState world,
            string id,
            string displayName,
            string locationId,
            long birthDay,
            long wealth = 0,
            PersonGender gender = PersonGender.Unknown,
            string spousePersonId = "",
            int cargoCapacity = 50,
            int medicalSkillBasisPoints = 0)
        {
            world.People.Add(new PersonState
            {
                Id = id,
                DisplayName = displayName,
                LocationId = locationId,
                BirthLocationId = locationId,
                BirthDay = birthDay,
                Wealth = wealth,
                Gender = gender,
                SpousePersonId = spousePersonId,
                CargoCapacity = cargoCapacity,
                MedicalSkillBasisPoints = medicalSkillBasisPoints
            });
        }

        private static void SynchronizeFamilyReferences(WorldState world)
        {
            for (var familyIndex = 0;
                 familyIndex < world.Families.Count;
                 familyIndex++)
            {
                var family = world.Families[familyIndex];
                var head = world.People.Find(
                    person => person.Id == family.HeadPersonId);
                family.LocationId = head.LocationId;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    world.People.Find(
                        person => person.Id == family.MemberIds[memberIndex])
                        .FamilyId = family.Id;
                }
            }
        }

        private static void AddCommodity(
            WorldState world,
            string id,
            string displayName,
            int basePrice,
            int unitWeight,
            string productDefinitionId = "")
        {
            world.Commodities.Add(new CommodityState
            {
                Id = id,
                DisplayName = displayName,
                ProductDefinitionId = productDefinitionId,
                BasePrice = basePrice,
                UnitWeight = unitWeight
            });
        }

        private static void AddMarketBundle(
            WorldState world,
            string locationId,
            int grainPrice,
            int clothPrice,
            int saltPrice,
            int horsePrice,
            int herbPrice)
        {
            AddMarketListing(
                world, locationId, "commodity.grain", grainPrice, 600, 600);
            AddMarketListing(
                world, locationId, "commodity.cloth", clothPrice, 220, 220);
            AddMarketListing(
                world, locationId, "commodity.salt", saltPrice, 180, 180);
            AddMarketListing(
                world, locationId, "commodity.horses", horsePrice, 80, 80);
            AddMarketListing(
                world, locationId, "commodity.herbs", herbPrice, 120, 120);
        }

        private static void AddMarketListing(
            WorldState world,
            string locationId,
            string commodityId,
            int price,
            int stock,
            int targetStock)
        {
            world.MarketListings.Add(new MarketListingState
            {
                Id = $"market.{locationId}.{commodityId}",
                LocationId = locationId,
                CommodityId = commodityId,
                Price = price,
                EquilibriumPrice = price,
                Stock = stock,
                TargetStock = targetStock
            });
        }

        private static void AddRoute(
            WorldState world,
            string id,
            string fromLocationId,
            string toLocationId,
            int distanceKilometers)
        {
            world.Routes.Add(new RouteState
            {
                Id = id,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                DistanceKilometers = distanceKilometers,
                Bidirectional = true,
                SecurityBasisPoints = 5_000
            });
        }

        private static void AddRelationship(
            WorldState world,
            string fromPersonId,
            string toPersonId,
            int affection,
            int trust,
            int respect)
        {
            world.Relationships.Add(new RelationshipState
            {
                Id = $"relationship.{fromPersonId}.{toPersonId}",
                FromPersonId = fromPersonId,
                ToPersonId = toPersonId,
                Affection = affection,
                Trust = trust,
                Respect = respect
            });
        }

        private static void AddOrganization(
            WorldState world,
            string id,
            string displayName,
            OrganizationType type,
            string headquartersLocationId,
            string leaderPersonId)
        {
            world.Organizations.Add(new OrganizationState
            {
                Id = id,
                DisplayName = displayName,
                Type = type,
                HeadquartersLocationId = headquartersLocationId,
                LeaderPersonId = leaderPersonId,
                Treasury = 10_000
            });
        }

        private static void AddPosition(
            WorldState world,
            string id,
            string organizationId,
            string displayName,
            int rank,
            int capacity)
        {
            world.Positions.Add(new PositionState
            {
                Id = id,
                OrganizationId = organizationId,
                DisplayName = displayName,
                Rank = rank,
                Capacity = capacity
            });
        }

        private static void AddArmy(
            WorldState world,
            string id,
            string displayName,
            string organizationId,
            string commanderPersonId,
            string locationId,
            int troops,
            int maximumTroops,
            int moraleBasisPoints,
            int trainingBasisPoints,
            int provisions,
            bool isMobilized)
        {
            world.Armies.Add(new ArmyState
            {
                Id = id,
                DisplayName = displayName,
                OrganizationId = organizationId,
                CommanderPersonId = commanderPersonId,
                LocationId = locationId,
                Troops = troops,
                MaximumTroops = maximumTroops,
                MoraleBasisPoints = moraleBasisPoints,
                TrainingBasisPoints = trainingBasisPoints,
                Provisions = provisions,
                IsMobilized = isMobilized
            });
        }

        private static void AddMembership(
            WorldState world,
            string personId,
            string organizationId,
            string positionId,
            int loyaltyBasisPoints)
        {
            world.Memberships.Add(new MembershipState
            {
                Id = $"membership.{personId}.{organizationId}",
                PersonId = personId,
                OrganizationId = organizationId,
                PositionId = positionId,
                JoinedDay = 0,
                LoyaltyBasisPoints = loyaltyBasisPoints
            });
        }

        private static void AddTaskDefinition(
            WorldState world,
            string id,
            string displayName,
            TaskKind kind,
            string issuerOrganizationId,
            string requiredPositionId,
            string originLocationId,
            string targetLocationId,
            int requiredProgress,
            int durationDays,
            long rewardMoney,
            int rewardProvisions,
            bool requiresMembership = true,
            bool isAvailable = true,
            string targetArmyId = "",
            int armyProvisionReward = 0)
        {
            world.TaskDefinitions.Add(new TaskDefinitionState
            {
                Id = id,
                DisplayName = displayName,
                Kind = kind,
                IssuerOrganizationId = issuerOrganizationId,
                RequiredPositionId = requiredPositionId,
                OriginLocationId = originLocationId,
                TargetLocationId = targetLocationId,
                RequiredProgress = requiredProgress,
                DurationDays = durationDays,
                RewardMoney = rewardMoney,
                RewardProvisions = rewardProvisions,
                TargetArmyId = targetArmyId,
                ArmyProvisionReward = armyProvisionReward,
                RequiresMembership = requiresMembership,
                IsAvailable = isAvailable
            });
        }

        private static void AddHistoricalEvent(
            WorldState world,
            string id,
            string displayName,
            long earliestDay,
            long latestDay,
            string prerequisiteEventId,
            string canonicalOutcome,
            params HistoricalEffectState[] effects)
        {
            var definition = new HistoricalEventDefinitionState
            {
                Id = id,
                DisplayName = displayName,
                EarliestDay = earliestDay,
                LatestDay = latestDay,
                PrerequisiteEventId = prerequisiteEventId,
                CanonicalOutcome = canonicalOutcome
            };
            definition.Effects.AddRange(effects);
            world.HistoricalEventDefinitions.Add(definition);
        }
    }
}
