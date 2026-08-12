using System;
using Mandate.Domain;
using Mandate.Simulation;

namespace Mandate.Presentation
{
    public sealed class TownNavigationPresentationState
    {
        public string LocationId;
        public string LocationName;
        public int VisibleFacilityCount;
        public bool CanEnter;
        public string ButtonLabel;
        public string Guidance;
    }

    public static class TownNavigationPresentation
    {
        public static TownNavigationPresentationState Build(
            WorldState world,
            string personId,
            bool isTraveling,
            MerchantTownOperationSystem system = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            var person = world.People.Find(item => item.Id == personId) ??
                throw new InvalidOperationException(
                    $"Missing person {personId}.");
            var location = world.Locations.Find(item =>
                item.Id == person.LocationId) ??
                throw new InvalidOperationException(
                    $"Missing location {person.LocationId}.");
            var town = (system ?? new MerchantTownOperationSystem())
                .InspectTown(world, person.Id, location.Id);
            var canEnter = !isTraveling && town.CanEnterTown &&
                town.Facilities.Count > 0;
            return new TownNavigationPresentationState
            {
                LocationId = location.Id,
                LocationName = location.DisplayName,
                VisibleFacilityCount = town.Facilities.Count,
                CanEnter = canEnter,
                ButtonLabel = town.Facilities.Count > 0
                    ? $"进入{location.DisplayName}城镇（{town.Facilities.Count}处建筑）"
                    : $"查看{location.DisplayName}聚落",
                Guidance = isTraveling
                    ? "人物正在旅途中，抵达后才能进入当地建筑。"
                    : town.Facilities.Count > 0
                        ? $"你现在位于{location.DisplayName}，可以直接进入城镇，不需要在地图下方寻找入口。"
                        : $"{location.DisplayName}尚未建立可进入建筑；中山已有首批商号经营场所。"
            };
        }
    }

    public enum TownFacilityVisualTone : byte
    {
        Commerce,
        Organization,
        Storage,
        Hospitality,
        Transport,
        Guild,
        Government,
        General
    }

    public sealed class TownFacilityVisualDescriptor
    {
        public string Seal;
        public string Category;
        public TownFacilityVisualTone Tone;
    }

    public static class TownVisualPresentation
    {
        public const string ZhongshanLocationId = "location.zhongshan";
        public const string ZhongshanOverviewResourcePath =
            "Art/Towns/zhongshan-town-overview-v1";

        public static string OverviewResourcePath(string locationId) =>
            locationId == ZhongshanLocationId
                ? ZhongshanOverviewResourcePath
                : string.Empty;

        public static TownFacilityVisualDescriptor Describe(string kindId)
        {
            switch (kindId)
            {
                case TownFacilityKindIds.Market:
                    return Create("市", "市井交易", TownFacilityVisualTone.Commerce);
                case TownFacilityKindIds.MerchantHall:
                    return Create("堂", "商号经营", TownFacilityVisualTone.Organization);
                case TownFacilityKindIds.Warehouse:
                    return Create("仓", "货物仓储", TownFacilityVisualTone.Storage);
                case TownFacilityKindIds.Inn:
                    return Create("舍", "食宿往来", TownFacilityVisualTone.Hospitality);
                case TownFacilityKindIds.VehicleYard:
                    return Create("车", "车马运输", TownFacilityVisualTone.Transport);
                case TownFacilityKindIds.GuildHall:
                    return Create("会", "行会委托", TownFacilityVisualTone.Guild);
                case TownFacilityKindIds.GovernmentOffice:
                    return Create("官", "官府告示", TownFacilityVisualTone.Government);
                default:
                    return Create("屋", "城镇建筑", TownFacilityVisualTone.General);
            }
        }

        public static string DistrictName(string districtId)
        {
            switch (districtId)
            {
                case TownDistrictIds.ZhongshanMarket:
                    return "市廛";
                case TownDistrictIds.ZhongshanMerchant:
                    return "商坊";
                case TownDistrictIds.ZhongshanTravel:
                    return "旅舍车马区";
                case TownDistrictIds.ZhongshanCivic:
                    return "官署区";
                default:
                    return "待定街区";
            }
        }

        private static TownFacilityVisualDescriptor Create(
            string seal,
            string category,
            TownFacilityVisualTone tone) =>
            new TownFacilityVisualDescriptor
            {
                Seal = seal,
                Category = category,
                Tone = tone
            };
    }
}
