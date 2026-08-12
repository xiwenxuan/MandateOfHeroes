using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public static class VillagePrototypeFactory
    {
        public const int MinimumPopulation = 200;
        public const int MaximumPopulation = 500;
        public const int DefaultPopulation = 300;

        private static readonly string[] Surnames =
        {
            "赵", "钱", "孙", "李", "周", "吴", "郑", "王",
            "冯", "陈", "褚", "卫", "蒋", "沈", "韩", "杨",
            "朱", "秦", "尤", "许"
        };

        private static readonly string[] GivenNames =
        {
            "安", "伯", "成", "德", "丰", "广", "和", "敬",
            "良", "宁", "平", "勤", "生", "顺", "文", "兴",
            "义", "勇", "正", "仲"
        };

        public static WorldState Create(
            int population = DefaultPopulation,
            ulong masterSeed = 140_018_001UL)
        {
            if (population < MinimumPopulation || population > MaximumPopulation)
            {
                throw new ArgumentOutOfRangeException(nameof(population));
            }

            var world = WorldState.Create(masterSeed);
            world.Locations.Add(new LocationState
            {
                Id = "location.village_demo_county",
                DisplayName = "安民县",
                Kind = LocationKind.CountySeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Government |
                           LocationFeature.Market |
                           LocationFeature.Farmland,
                StrategicImportance = 2,
                Population = 0,
                GrainPrice = 100,
                PublicOrderBasisPoints = 6_000,
                MapXBasisPoints = 5_000,
                MapYBasisPoints = 5_000
            });
            world.Locations.Add(new LocationState
            {
                Id = "location.village_demo",
                DisplayName = "安禾里",
                Kind = LocationKind.Village,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Farmland |
                           LocationFeature.Workshop |
                           LocationFeature.Clinic,
                StrategicImportance = 1,
                ParentLocationId = "location.village_demo_county",
                Population = population,
                GrainPrice = 100,
                PublicOrderBasisPoints = 6_500,
                MapXBasisPoints = 5_100,
                MapYBasisPoints = 5_100
            });

            var householdCount = Math.Max(40, population / 5);
            var baseSize = population / householdCount;
            var remainder = population % householdCount;
            var random = new NamedRandom(masterSeed);
            var village = new VillageState
            {
                Id = "village.anhe",
                DisplayName = "安禾里",
                LocationId = "location.village_demo",
                ParentLocationId = "location.village_demo_county",
                PublicGranaryGrain = Math.Max(20, population / 10),
                HouseholdCount = householdCount,
                LedgerOpeningPublicGrain = Math.Max(20, population / 10)
            };
            world.Villages.Add(village);

            for (var familyIndex = 0;
                 familyIndex < householdCount;
                 familyIndex++)
            {
                var size = baseSize + (familyIndex < remainder ? 1 : 0);
                AddHousehold(
                    world,
                    village,
                    random,
                    familyIndex,
                    size,
                    familyIndex == householdCount - 1);
            }

            CharacterAbilityBootstrap.InitializeWorld(world);
            ApplyOccupationalSkills(world);
            AddFacilities(world, village);
            AddClinicMedicineStock(world, village);
            HerbalMedicineSupplySystem.InitializePrototype(world, village);
            AddCountyGovernance(world, village);
            VillageLifeSystem.RefreshCaches(world, village);
            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                village.LedgerOpeningFamilyGrain +=
                    FindFamily(world, village.HouseholdIds[i]).Grain;
            }

            world.PlayerPersonId = world.Families[0].HeadPersonId;
            new PopulationLedgerSystem().InitializeFromLocationSummaries(world);
            world.Validate();
            return world;
        }

        private static void AddHousehold(
            WorldState world,
            VillageState village,
            NamedRandom random,
            int familyIndex,
            int size,
            bool isMarginal)
        {
            var familyId = $"family.village_demo.{familyIndex:D3}";
            var familyStableId = new StableId(familyId);
            var surname = Surnames[random.Range(
                "village_generation",
                familyStableId,
                0,
                "surname",
                0,
                Surnames.Length)];
            var family = new FamilyState
            {
                Id = familyId,
                DisplayName = $"{surname}氏家户",
                LocationId = village.LocationId,
                VillageId = village.Id,
                Wealth = isMarginal ? 120 : 1_200 + familyIndex * 5,
                Grain = isMarginal ? 3 : size * 42L + familyIndex % 9,
                SeedGrain = isMarginal ? 2 : size * 8L,
                FarmlandUnits = isMarginal ? 2 : size * 3 + familyIndex % 4,
                ToolConditionBasisPoints = isMarginal ? 3_500 : 8_000,
                FoodSecurityBasisPoints = isMarginal ? 2_000 : 10_000
            };

            for (var memberIndex = 0; memberIndex < size; memberIndex++)
            {
                var person = CreatePerson(
                    world,
                    random,
                    family,
                    village.LocationId,
                    surname,
                    familyIndex,
                    memberIndex);
                world.People.Add(person);
                family.MemberIds.Add(person.Id);
                if (memberIndex == 0)
                {
                    family.HeadPersonId = person.Id;
                }
            }

            if (size >= 2)
            {
                var head = FindPerson(world, family.MemberIds[0]);
                var spouse = FindPerson(world, family.MemberIds[1]);
                head.SpousePersonId = spouse.Id;
                spouse.SpousePersonId = head.Id;
                for (var memberIndex = 2;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var child = FindPerson(world, family.MemberIds[memberIndex]);
                    if ((world.AbsoluteDay - child.BirthDay) / 360 < 30)
                    {
                        child.FatherPersonId = head.Id;
                        child.MotherPersonId = spouse.Id;
                    }
                }
            }

            world.Families.Add(family);
            village.HouseholdIds.Add(family.Id);
        }

        private static PersonState CreatePerson(
            WorldState world,
            NamedRandom random,
            FamilyState family,
            string locationId,
            string surname,
            int familyIndex,
            int memberIndex)
        {
            var id = $"person.village_demo.{familyIndex:D3}.{memberIndex:D2}";
            var stableId = new StableId(id);
            int age;
            PersonGender gender;
            VillageOccupation occupation;
            if (memberIndex == 0)
            {
                age = random.Range(
                    "village_generation", stableId, 0, "head_age", 36, 56);
                gender = PersonGender.Male;
                occupation = OccupationForHead(familyIndex);
            }
            else if (memberIndex == 1)
            {
                age = random.Range(
                    "village_generation", stableId, 0, "spouse_age", 30, 51);
                gender = PersonGender.Female;
                occupation = VillageOccupation.Farmer;
            }
            else if (memberIndex == 4 && familyIndex % 3 == 0)
            {
                age = random.Range(
                    "village_generation", stableId, 0, "elder_age", 58, 76);
                gender = familyIndex % 2 == 0
                    ? PersonGender.Male
                    : PersonGender.Female;
                occupation = VillageOccupation.Dependent;
            }
            else if (memberIndex >= 4)
            {
                age = random.Range(
                    "village_generation", stableId, 0, "young_adult_age", 18, 26);
                gender = (familyIndex + memberIndex) % 2 == 0
                    ? PersonGender.Male
                    : PersonGender.Female;
                occupation = VillageOccupation.Farmer;
            }
            else
            {
                age = random.Range(
                    "village_generation", stableId, 0, "child_age", 4, 18);
                gender = (familyIndex + memberIndex) % 2 == 0
                    ? PersonGender.Male
                    : PersonGender.Female;
                occupation = VillageOccupation.Dependent;
            }

            var givenNameIndex = random.Range(
                "village_generation",
                stableId,
                0,
                "given_name",
                0,
                GivenNames.Length);
            var labor = age < 15 || age > 65
                ? 0
                : Math.Max(3_000, 10_000 - Math.Max(0, age - 45) * 150);
            return new PersonState
            {
                Id = id,
                DisplayName = surname + GivenNames[givenNameIndex],
                LocationId = locationId,
                BirthLocationId = locationId,
                FamilyId = family.Id,
                PopulationOriginLocationId = locationId,
                BirthDay = -age * 360L - random.Range(
                    "village_generation", stableId, 0, "birth_day", 0, 360),
                Gender = gender,
                VillageOccupation = occupation,
                LaborCapacityBasisPoints = labor,
                HealthBasisPoints = random.Range(
                    "village_generation", stableId, 0, "health", 7_500, 10_001),
                Wealth = memberIndex < 2 ? 20 : 0,
                Provisions = 0,
                CargoCapacity = 20,
                NextIndependentEventDay = 30,
                NextIndependentEventReason = "monthly_household_settlement"
            };
        }

        private static VillageOccupation OccupationForHead(int familyIndex)
        {
            switch (familyIndex)
            {
                case 0:
                    return VillageOccupation.Headman;
                case 1:
                    return VillageOccupation.Physician;
                case 2:
                    return VillageOccupation.Artisan;
                case 3:
                    return VillageOccupation.Merchant;
                default:
                    return familyIndex % 12 == 0
                        ? VillageOccupation.Artisan
                        : VillageOccupation.Farmer;
            }
        }

        private static void ApplyOccupationalSkills(WorldState world)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                switch (person.VillageOccupation)
                {
                    case VillageOccupation.Headman:
                        person.ProfessionalSkills.Administration = 7_000;
                        break;
                    case VillageOccupation.Physician:
                        person.MedicalSkillBasisPoints = 7_500;
                        person.ProfessionalSkills.Medicine = 7_500;
                        break;
                    case VillageOccupation.Artisan:
                        person.ProfessionalSkills.Craft = 7_000;
                        break;
                    case VillageOccupation.Merchant:
                        person.ProfessionalSkills.Commerce = 6_500;
                        break;
                    case VillageOccupation.Farmer:
                        person.ProfessionalSkills.Agriculture = 5_500;
                        break;
                }
            }
        }

        private static void AddFacilities(WorldState world, VillageState village)
        {
            var headman = FindByOccupation(world, VillageOccupation.Headman);
            var physician = FindByOccupation(world, VillageOccupation.Physician);
            var artisan = FindByOccupation(world, VillageOccupation.Artisan);
            AddFacility(
                world, village, "farmland", VillageFacilityKind.Farmland,
                headman, 2_000, 8_500, 0);
            AddFacility(
                world, village, "irrigation", VillageFacilityKind.Irrigation,
                headman, 2_000, 8_000, 0);
            AddFacility(
                world, village, "granary", VillageFacilityKind.Granary,
                headman, 5_000, 8_500, village.PublicGranaryGrain);
            AddFacility(
                world, village, "smithy", VillageFacilityKind.Smithy,
                artisan, 40, 8_000, 2_000);
            AddFacility(
                world, village, "clinic", VillageFacilityKind.Clinic,
                physician, 20, 7_500, 600);
            AddFacility(
                world, village, "assembly", VillageFacilityKind.AssemblyHall,
                headman, 500, 8_000, 0);
            for (var familyIndex = 0;
                 familyIndex < village.HouseholdIds.Count;
                 familyIndex++)
            {
                var family = FindFamily(world, village.HouseholdIds[familyIndex]);
                var manager = FindPerson(world, family.HeadPersonId);
                world.VillageFacilities.Add(new VillageFacilityState
                {
                    Id = $"facility.{village.Id}.household_granary.{familyIndex:D3}",
                    VillageId = village.Id,
                    Kind = VillageFacilityKind.HouseholdGranary,
                    OwnerFamilyId = family.Id,
                    ManagerPersonId = manager.Id,
                    Capacity = checked(
                        (int)Math.Min(
                            int.MaxValue,
                            Math.Max(
                                200L,
                                family.Grain + family.SeedGrain +
                                family.FarmlandUnits * 30L))),
                    ConditionBasisPoints = 8_000,
                    InventoryUnits = family.Grain + family.SeedGrain,
                    CapabilityTags = new List<string>
                    {
                        VillageFacilityTags.HouseholdGranary
                    }
                });
            }
        }

        private static void AddCountyGovernance(
            WorldState world,
            VillageState village)
        {
            var administrator = FindFamily(world, village.HouseholdIds[0]);
            var organization = new OrganizationState
            {
                Id = "organization.village_demo_county_government",
                DisplayName = "安民县官府",
                Type = OrganizationType.Government,
                HeadquartersLocationId = village.ParentLocationId,
                LeaderPersonId = administrator.HeadPersonId,
                Treasury = 1_200,
                ReputationBasisPoints = 6_000
            };
            world.Organizations.Add(organization);

            var governance = new CountyGovernanceState
            {
                Id = "county_governance.village_demo",
                CountyLocationId = village.ParentLocationId,
                GovernmentOrganizationId = organization.Id,
                AdministratorFamilyId = administrator.Id,
                AnnualCashTaxRateBasisPoints = 300,
                LocalGrainRetentionBasisPoints = 4_000,
                RegistrationCoverageBasisPoints = 9_000,
                AdministrativeEfficiencyBasisPoints = 8_000,
                GentryInfluenceBasisPoints = 1_800,
                CountyGranaryGrain = 120,
                NextSettlementDay = 30
            };
            world.CountyGovernances.Add(governance);
            village.HouseholdReliefPriorityPolicyId =
                HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability;
            village.HouseholdReliefAuthorizationPolicyId =
                HouseholdReliefAuthorizationPolicyIds.CountyGovernmentLeader;
            village.HouseholdReliefAuthorityOrganizationId = organization.Id;

            var wealthyFamilies = new List<FamilyState>(world.Families);
            wealthyFamilies.Sort((left, right) =>
            {
                var wealth = right.Wealth.CompareTo(left.Wealth);
                return wealth != 0
                    ? wealth
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            var influences = new[] { 800, 600, 400 };
            var compliances = new[] { 7_000, 8_000, 9_000 };
            for (var i = 0; i < Math.Min(3, wealthyFamilies.Count); i++)
            {
                world.CountyGentryHouses.Add(new CountyGentryHouseState
                {
                    Id = $"county_gentry.{governance.Id}.{wealthyFamilies[i].Id}",
                    CountyGovernanceId = governance.Id,
                    FamilyId = wealthyFamilies[i].Id,
                    InfluenceBasisPoints = influences[i],
                    TaxComplianceBasisPoints = compliances[i]
                });
            }

            world.Commodities.Add(new CommodityState
            {
                Id = "commodity.grain",
                DisplayName = "粮食",
                BasePrice = 100,
                UnitWeight = 1
            });
            world.MarketListings.Add(new MarketListingState
            {
                Id = "market.village_demo_county.grain",
                LocationId = village.ParentLocationId,
                CommodityId = "commodity.grain",
                Price = 100,
                EquilibriumPrice = 100,
                Stock = 300,
                TargetStock = 400
            });
        }

        private static void AddClinicMedicineStock(
            WorldState world,
            VillageState village)
        {
            var clinic = world.VillageFacilities.Find(item =>
                item.VillageId == village.Id &&
                item.Kind == VillageFacilityKind.Clinic);
            var physician = FindPerson(world, clinic.ManagerPersonId);
            var quantity = clinic.InventoryUnits;
            var container = new InventoryContainerState
            {
                Id = $"inventory.{village.Id}.clinic",
                KindId = "inventory.village_clinic",
                OwnerFamilyId = physician.FamilyId,
                LocationId = village.LocationId,
                CapacityWeight = Math.Max(1, quantity),
                FoodStorageEnvironmentId =
                    "storage.environment.generic_sheltered",
                FoodStorageProtectionBasisPoints = 4_000
            };
            world.InventoryContainers.Add(container);
            clinic.InventoryUnits = 0;
            new ProductInventorySystem().CreateFamilyContainerOpeningBatch(
                world,
                physician.FamilyId,
                container.Id,
                physician.Id,
                CoreProductionContent.HerbalMedicineMaterialProductId,
                quantity,
                8_000);
        }

        private static void AddFacility(
            WorldState world,
            VillageState village,
            string suffix,
            VillageFacilityKind kind,
            PersonState manager,
            int capacity,
            int condition,
            long inventory)
        {
            world.VillageFacilities.Add(new VillageFacilityState
            {
                Id = $"facility.{village.Id}.{suffix}",
                VillageId = village.Id,
                Kind = kind,
                OwnerFamilyId = manager.FamilyId,
                ManagerPersonId = manager.Id,
                Capacity = capacity,
                ConditionBasisPoints = condition,
                InventoryUnits = inventory,
                CapabilityTags = new List<string>
                {
                    VillageFacilityTags.FromKind(kind)
                }
            });
        }

        private static PersonState FindByOccupation(
            WorldState world,
            VillageOccupation occupation)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].VillageOccupation == occupation)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException(
                $"Village lacks required occupation {occupation}.");
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
    }
}
