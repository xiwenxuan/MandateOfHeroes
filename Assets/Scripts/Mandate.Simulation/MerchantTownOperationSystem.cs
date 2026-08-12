using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class TownFacilityView
    {
        public string FacilityId;
        public string KindId;
        public string DisplayName;
        public string OwnerName;
        public string ManagerName;
        public string InventoryContainerId;
        public bool HasMapPlacement;
        public string DistrictId;
        public int MapXBasisPoints;
        public int MapYBasisPoints;
        public int FootprintWidthBasisPoints;
        public int FootprintHeightBasisPoints;
        public bool CanEnter;
        public string UnavailableReason;
        public List<string> OperationIds = new List<string>();
    }

    public sealed class TownOperationView
    {
        public string LocationId;
        public string DisplayName;
        public bool CanEnterTown;
        public string UnavailableReason;
        public List<TownFacilityView> Facilities =
            new List<TownFacilityView>();
    }

    public sealed class MerchantTownOperationSystem
    {
        public const string ZhongshanOrganizationId =
            "organization.zhongshan_merchants";
        public const string ZhongshanBranchId =
            "merchant_branch.zhongshan_merchants.headquarters";
        public const string ZhongshanWarehouseContainerId =
            "inventory_container.merchant_branch.zhongshan_merchants";

        public TownOperationView InspectTown(
            WorldState world,
            string personId,
            string locationId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var person = FindPerson(world, personId);
            var location = FindLocation(world, locationId);
            var view = new TownOperationView
            {
                LocationId = location.Id,
                DisplayName = location.DisplayName,
                CanEnterTown = person.IsAlive &&
                    person.LocationId == location.Id,
                UnavailableReason = person.LocationId == location.Id
                    ? person.IsAlive ? string.Empty : "人物已经死亡。"
                    : "必须先抵达该地点，才能进入城镇建筑。"
            };

            for (var i = 0; i < world.TownFacilities.Count; i++)
            {
                var facility = world.TownFacilities[i];
                if (facility.LocationId != location.Id ||
                    !facility.IsPubliclyVisible)
                {
                    continue;
                }

                view.Facilities.Add(BuildFacilityView(
                    world,
                    person,
                    facility,
                    view.CanEnterTown));
            }
            view.Facilities.Sort(CompareFacilityPlacement);
            return view;
        }

        public TownFacilityView EnterFacility(
            WorldState world,
            string personId,
            string facilityId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var person = FindPerson(world, personId);
            var facility = world.TownFacilities.Find(item =>
                item.Id == facilityId) ??
                throw new InvalidOperationException(
                    $"Missing town facility {facilityId}.");
            if (!person.IsAlive || person.LocationId != facility.LocationId)
            {
                throw new InvalidOperationException(
                    "人物必须活着并身处该城镇，才能进入建筑。");
            }

            var view = BuildFacilityView(world, person, facility, true);
            if (!view.CanEnter)
            {
                throw new InvalidOperationException(view.UnavailableReason);
            }
            return view;
        }

        public static void InitializePrototype(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (world.MerchantBranches.Exists(item =>
                    item.Id == ZhongshanBranchId))
            {
                return;
            }

            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = ZhongshanWarehouseContainerId,
                KindId = "inventory_container.merchant_branch_warehouse",
                OwnerOrganizationId = ZhongshanOrganizationId,
                LocationId = "location.zhongshan",
                CapacityWeight = 2_000,
                FoodStorageEnvironmentId =
                    "storage.environment.generic_sheltered",
                FoodStorageProtectionBasisPoints = 3_000
            });

            AddFacility(
                world,
                "town_facility.zhongshan.market",
                TownFacilityKindIds.Market,
                "中山集市",
                string.Empty,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);
            AddFacility(
                world,
                "town_facility.zhongshan.merchant_hall",
                TownFacilityKindIds.MerchantHall,
                "中山商行主堂",
                ZhongshanOrganizationId,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);
            AddFacility(
                world,
                "town_facility.zhongshan.warehouse",
                TownFacilityKindIds.Warehouse,
                "中山商行仓库",
                ZhongshanOrganizationId,
                ZhongshanWarehouseContainerId,
                TownFacilityAccessPolicyIds.OrganizationMembers);
            AddFacility(
                world,
                "town_facility.zhongshan.inn",
                TownFacilityKindIds.Inn,
                "中山客舍",
                string.Empty,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);
            AddFacility(
                world,
                "town_facility.zhongshan.vehicle_yard",
                TownFacilityKindIds.VehicleYard,
                "中山车马场",
                ZhongshanOrganizationId,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);
            AddFacility(
                world,
                "town_facility.zhongshan.guild_hall",
                TownFacilityKindIds.GuildHall,
                "中山行会馆",
                string.Empty,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);
            AddFacility(
                world,
                "town_facility.zhongshan.government_office",
                TownFacilityKindIds.GovernmentOffice,
                "中山国官署",
                string.Empty,
                string.Empty,
                TownFacilityAccessPolicyIds.Public);

            for (var i = 0; i < world.TownFacilities.Count; i++)
            {
                CoreTownFacilityLayout.TryApplyZhongshan(
                    world.TownFacilities[i]);
            }

            world.MerchantBranches.Add(new MerchantBranchState
            {
                Id = ZhongshanBranchId,
                OrganizationId = ZhongshanOrganizationId,
                DisplayName = "中山商行总号",
                LocationId = "location.zhongshan",
                ManagerPersonId = "person.zhang_shiping",
                InventoryContainerId = ZhongshanWarehouseContainerId,
                IsHeadquarters = true,
                FacilityIds = new List<string>
                {
                    "town_facility.zhongshan.merchant_hall",
                    "town_facility.zhongshan.warehouse",
                    "town_facility.zhongshan.vehicle_yard"
                }
            });
        }

        private static void AddFacility(
            WorldState world,
            string id,
            string kindId,
            string displayName,
            string ownerOrganizationId,
            string inventoryContainerId,
            string accessPolicyId)
        {
            world.TownFacilities.Add(new TownFacilityState
            {
                Id = id,
                KindId = kindId,
                DisplayName = displayName,
                LocationId = "location.zhongshan",
                OwnerOrganizationId = ownerOrganizationId,
                ManagerPersonId = ownerOrganizationId == ZhongshanOrganizationId
                    ? "person.zhang_shiping"
                    : string.Empty,
                InventoryContainerId = inventoryContainerId,
                AccessPolicyId = accessPolicyId,
                IsPubliclyVisible = true,
                IsOperational = true
            });
        }

        private static TownFacilityView BuildFacilityView(
            WorldState world,
            PersonState person,
            TownFacilityState facility,
            bool isInTown)
        {
            var canEnter = isInTown && facility.IsOperational &&
                HasAccess(world, person.Id, facility);
            return new TownFacilityView
            {
                FacilityId = facility.Id,
                KindId = facility.KindId,
                DisplayName = facility.DisplayName,
                OwnerName = FindOwnerName(world, facility),
                ManagerName = FindPersonName(world, facility.ManagerPersonId),
                InventoryContainerId = facility.InventoryContainerId,
                HasMapPlacement = facility.HasMapPlacement,
                DistrictId = facility.DistrictId,
                MapXBasisPoints = facility.MapXBasisPoints,
                MapYBasisPoints = facility.MapYBasisPoints,
                FootprintWidthBasisPoints =
                    facility.FootprintWidthBasisPoints,
                FootprintHeightBasisPoints =
                    facility.FootprintHeightBasisPoints,
                CanEnter = canEnter,
                UnavailableReason = canEnter
                    ? string.Empty
                    : !isInTown
                        ? "尚未抵达该城镇。"
                        : !facility.IsOperational
                            ? "该建筑当前没有运营。"
                            : "当前人物没有进入权限。",
                OperationIds = OperationsFor(facility.KindId)
            };
        }

        private static int CompareFacilityPlacement(
            TownFacilityView left,
            TownFacilityView right)
        {
            if (left.HasMapPlacement != right.HasMapPlacement)
            {
                return left.HasMapPlacement ? -1 : 1;
            }

            var comparison = left.MapYBasisPoints.CompareTo(
                right.MapYBasisPoints);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.MapXBasisPoints.CompareTo(
                right.MapXBasisPoints);
            return comparison != 0
                ? comparison
                : string.CompareOrdinal(left.FacilityId, right.FacilityId);
        }

        private static bool HasAccess(
            WorldState world,
            string personId,
            TownFacilityState facility)
        {
            if (facility.AccessPolicyId == TownFacilityAccessPolicyIds.Public)
            {
                return true;
            }
            if (facility.AccessPolicyId ==
                TownFacilityAccessPolicyIds.OrganizationMembers)
            {
                return HasMembership(
                    world, personId, facility.OwnerOrganizationId);
            }
            if (facility.AccessPolicyId ==
                TownFacilityAccessPolicyIds.ManagerOnly)
            {
                return personId == facility.ManagerPersonId;
            }
            return false;
        }

        private static List<string> OperationsFor(string kindId)
        {
            if (kindId == TownFacilityKindIds.Market)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectMarket,
                    TownFacilityOperationIds.PrepareCaravan
                };
            }
            if (kindId == TownFacilityKindIds.MerchantHall)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectCompany,
                    TownFacilityOperationIds.InspectLedger,
                    TownFacilityOperationIds.PrepareCaravan
                };
            }
            if (kindId == TownFacilityKindIds.Warehouse)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectInventory,
                    TownFacilityOperationIds.PrepareCaravan
                };
            }
            if (kindId == TownFacilityKindIds.Inn)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectRecruits,
                    TownFacilityOperationIds.Rest
                };
            }
            if (kindId == TownFacilityKindIds.VehicleYard)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectTransport,
                    TownFacilityOperationIds.PrepareCaravan
                };
            }
            if (kindId == TownFacilityKindIds.GuildHall)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectCommissions
                };
            }
            if (kindId == TownFacilityKindIds.GovernmentOffice)
            {
                return new List<string>
                {
                    TownFacilityOperationIds.InspectGovernmentNotices
                };
            }
            return new List<string>();
        }

        private static bool HasMembership(
            WorldState world,
            string personId,
            string organizationId) =>
            !string.IsNullOrEmpty(organizationId) &&
            world.Memberships.Exists(item =>
                item.PersonId == personId &&
                item.OrganizationId == organizationId);

        private static PersonState FindPerson(WorldState world, string personId) =>
            world.People.Find(item => item.Id == personId) ??
            throw new InvalidOperationException($"Missing person {personId}.");

        private static LocationState FindLocation(
            WorldState world,
            string locationId) =>
            world.Locations.Find(item => item.Id == locationId) ??
            throw new InvalidOperationException($"Missing location {locationId}.");

        private static string FindPersonName(
            WorldState world,
            string personId)
        {
            if (string.IsNullOrEmpty(personId))
            {
                return string.Empty;
            }
            var person = world.People.Find(item => item.Id == personId);
            return person == null ? personId : person.DisplayName;
        }

        private static string FindOwnerName(
            WorldState world,
            TownFacilityState facility)
        {
            if (!string.IsNullOrEmpty(facility.OwnerOrganizationId))
            {
                var organization = world.Organizations.Find(item =>
                    item.Id == facility.OwnerOrganizationId);
                return organization == null
                    ? facility.OwnerOrganizationId
                    : organization.DisplayName;
            }
            if (!string.IsNullOrEmpty(facility.OwnerFamilyId))
            {
                var family = world.Families.Find(item =>
                    item.Id == facility.OwnerFamilyId);
                return family == null
                    ? facility.OwnerFamilyId
                    : family.DisplayName;
            }
            return "公共场所";
        }
    }
}
