using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class TownFacilityKindIds
    {
        public const string Residence = "town.facility.residence";
        public const string Market = "town.facility.market";
        public const string MerchantHall = "town.facility.merchant_hall";
        public const string Warehouse = "town.facility.warehouse";
        public const string Inn = "town.facility.inn";
        public const string VehicleYard = "town.facility.vehicle_yard";
        public const string GuildHall = "town.facility.guild_hall";
        public const string GovernmentOffice = "town.facility.government_office";
    }

    public static class TownFacilityAccessPolicyIds
    {
        public const string Public = "town.access.public";
        public const string OrganizationMembers =
            "town.access.organization_members";
        public const string ManagerOnly = "town.access.manager_only";
    }

    public static class TownFacilityOperationIds
    {
        public const string InspectMarket = "town.operation.inspect_market";
        public const string InspectCompany = "town.operation.inspect_company";
        public const string InspectLedger = "town.operation.inspect_ledger";
        public const string InspectInventory =
            "town.operation.inspect_inventory";
        public const string PrepareCaravan = "town.operation.prepare_caravan";
        public const string InspectRecruits = "town.operation.inspect_recruits";
        public const string Rest = "town.operation.rest";
        public const string InspectTransport =
            "town.operation.inspect_transport";
        public const string InspectCommissions =
            "town.operation.inspect_commissions";
        public const string InspectGovernmentNotices =
            "town.operation.inspect_government_notices";
    }

    public static class TownDistrictIds
    {
        public const string ZhongshanMarket =
            "town.district.zhongshan.market";
        public const string ZhongshanMerchant =
            "town.district.zhongshan.merchant";
        public const string ZhongshanTravel =
            "town.district.zhongshan.travel";
        public const string ZhongshanCivic =
            "town.district.zhongshan.civic";
    }

    [Serializable]
    public sealed class TownFacilityState
    {
        public string Id;
        public string KindId;
        public string DisplayName;
        public string LocationId;
        public string OwnerOrganizationId;
        public string OwnerFamilyId;
        public string ManagerPersonId;
        public string InventoryContainerId;
        public string AccessPolicyId = TownFacilityAccessPolicyIds.Public;
        public bool IsPubliclyVisible = true;
        public bool IsOperational = true;
        public bool HasMapPlacement;
        public string DistrictId;
        public int MapXBasisPoints;
        public int MapYBasisPoints;
        public int FootprintWidthBasisPoints;
        public int FootprintHeightBasisPoints;
    }

    public static class CoreTownFacilityLayout
    {
        public static bool TryApplyZhongshan(TownFacilityState facility)
        {
            if (facility == null ||
                facility.LocationId != "location.zhongshan")
            {
                return false;
            }

            switch (facility.Id)
            {
                case "town_facility.zhongshan.market":
                    Apply(facility, TownDistrictIds.ZhongshanMarket,
                        2_600, 5_600, 2_000, 1_650);
                    return true;
                case "town_facility.zhongshan.merchant_hall":
                    Apply(facility, TownDistrictIds.ZhongshanMerchant,
                        4_650, 4_850, 1_500, 1_250);
                    return true;
                case "town_facility.zhongshan.warehouse":
                    Apply(facility, TownDistrictIds.ZhongshanMerchant,
                        6_250, 5_750, 1_500, 1_150);
                    return true;
                case "town_facility.zhongshan.inn":
                    Apply(facility, TownDistrictIds.ZhongshanTravel,
                        2_050, 7_650, 1_400, 1_100);
                    return true;
                case "town_facility.zhongshan.vehicle_yard":
                    Apply(facility, TownDistrictIds.ZhongshanTravel,
                        7_750, 7_350, 1_700, 1_250);
                    return true;
                case "town_facility.zhongshan.guild_hall":
                    Apply(facility, TownDistrictIds.ZhongshanMarket,
                        4_100, 7_300, 1_450, 1_100);
                    return true;
                case "town_facility.zhongshan.government_office":
                    Apply(facility, TownDistrictIds.ZhongshanCivic,
                        6_900, 3_300, 1_650, 1_300);
                    return true;
                default:
                    return false;
            }
        }

        private static void Apply(
            TownFacilityState facility,
            string districtId,
            int xBasisPoints,
            int yBasisPoints,
            int widthBasisPoints,
            int heightBasisPoints)
        {
            facility.HasMapPlacement = true;
            facility.DistrictId = districtId;
            facility.MapXBasisPoints = xBasisPoints;
            facility.MapYBasisPoints = yBasisPoints;
            facility.FootprintWidthBasisPoints = widthBasisPoints;
            facility.FootprintHeightBasisPoints = heightBasisPoints;
        }
    }

    [Serializable]
    public sealed class MerchantBranchState
    {
        public string Id;
        public string OrganizationId;
        public string DisplayName;
        public string LocationId;
        public string ManagerPersonId;
        public string InventoryContainerId;
        public bool IsHeadquarters;
        public List<string> FacilityIds = new List<string>();
    }
}
