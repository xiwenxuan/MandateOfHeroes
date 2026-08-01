using System;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum MapPerspective : byte
    {
        General,
        Military,
        Administration,
        Commerce,
        Medicine
    }

    public sealed class LocationPerspectiveInfo
    {
        public string PrimaryMetric;
        public string SecondaryMetric;
        public int EmphasisBasisPoints;
        public LocationFeature VisibleFeatures;
    }

    public static class MapPerspectiveSystem
    {
        public static MapPerspective RecommendForPlayer(
            WorldState world,
            string personId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            for (var i = 0; i < world.Memberships.Count; i++)
            {
                var membership = world.Memberships[i];
                if (membership.PersonId != personId)
                {
                    continue;
                }

                if (membership.PositionId.IndexOf(
                        "physician",
                        StringComparison.Ordinal) >= 0)
                {
                    return MapPerspective.Medicine;
                }

                if (membership.PositionId.IndexOf(
                        "trader",
                        StringComparison.Ordinal) >= 0)
                {
                    return MapPerspective.Commerce;
                }

                var organization = FindOrganization(
                    world,
                    membership.OrganizationId);
                if (organization == null)
                {
                    continue;
                }

                switch (organization.Type)
                {
                    case OrganizationType.Military:
                        return MapPerspective.Military;
                    case OrganizationType.Merchant:
                        return MapPerspective.Commerce;
                    case OrganizationType.Government:
                        return MapPerspective.Administration;
                }
            }

            return MapPerspective.General;
        }

        public static LocationPerspectiveInfo Inspect(
            WorldState world,
            LocationState location,
            MapPerspective perspective)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            switch (perspective)
            {
                case MapPerspective.Military:
                    return InspectMilitary(world, location);
                case MapPerspective.Administration:
                    return InspectAdministration(location);
                case MapPerspective.Commerce:
                    return InspectCommerce(world, location);
                case MapPerspective.Medicine:
                    return InspectMedicine(world, location);
                default:
                    return InspectGeneral(location);
            }
        }

        private static LocationPerspectiveInfo InspectGeneral(
            LocationState location)
        {
            return new LocationPerspectiveInfo
            {
                PrimaryMetric = TerrainName(location.Terrain),
                SecondaryMetric =
                    $"{LocationKindName(location.Kind)}·{location.StrategicImportance}星",
                EmphasisBasisPoints = location.StrategicImportance * 2_000,
                VisibleFeatures = location.Features
            };
        }

        private static LocationPerspectiveInfo InspectMilitary(
            WorldState world,
            LocationState location)
        {
            var troops = 0;
            var wounded = 0;
            for (var i = 0; i < world.Armies.Count; i++)
            {
                var army = world.Armies[i];
                if (army.LocationId != location.Id ||
                    IsArmyMarching(world, army.Id))
                {
                    continue;
                }

                troops += army.Troops;
                wounded += army.WoundedTroops;
            }

            var hasDefense =
                (location.Features &
                 (LocationFeature.Garrison |
                  LocationFeature.Fortification)) != 0;
            return new LocationPerspectiveInfo
            {
                PrimaryMetric = $"兵{troops}",
                SecondaryMetric =
                    $"伤{wounded}·{(hasDefense ? "有城防" : "无城防")}",
                EmphasisBasisPoints = ClampBasisPoints(
                    location.StrategicImportance * 1_500 + troops / 2),
                VisibleFeatures =
                    location.Features &
                    (LocationFeature.Garrison |
                     LocationFeature.Fortification |
                     LocationFeature.RelayStation |
                     LocationFeature.Harbor)
            };
        }

        private static LocationPerspectiveInfo InspectAdministration(
            LocationState location)
        {
            var hasGovernment =
                (location.Features & LocationFeature.Government) != 0;
            return new LocationPerspectiveInfo
            {
                PrimaryMetric = $"民{location.Population / 1_000}千",
                SecondaryMetric =
                    $"治安{location.PublicOrderBasisPoints / 100f:F0}%·" +
                    (hasGovernment ? "有官署" : "无官署"),
                EmphasisBasisPoints = ClampBasisPoints(
                    location.Population / 8 +
                    location.PublicOrderBasisPoints / 2),
                VisibleFeatures =
                    location.Features &
                    (LocationFeature.Government |
                     LocationFeature.Farmland |
                     LocationFeature.Market |
                     LocationFeature.RelayStation)
            };
        }

        private static LocationPerspectiveInfo InspectCommerce(
            WorldState world,
            LocationState location)
        {
            var stock = 0;
            var listingCount = 0;
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId != location.Id)
                {
                    continue;
                }

                stock += listing.Stock;
                listingCount++;
            }

            var hasMarket =
                (location.Features & LocationFeature.Market) != 0;
            return new LocationPerspectiveInfo
            {
                PrimaryMetric = $"粮{location.GrainPrice}钱",
                SecondaryMetric =
                    $"{(hasMarket ? "有市" : "无市")}·货{stock}",
                EmphasisBasisPoints = ClampBasisPoints(
                    listingCount * 1_200 +
                    stock / 2 +
                    ((location.Features & LocationFeature.Workshop) != 0
                        ? 1_500
                        : 0)),
                VisibleFeatures =
                    location.Features &
                    (LocationFeature.Market |
                     LocationFeature.Workshop |
                     LocationFeature.RelayStation |
                     LocationFeature.Harbor)
            };
        }

        private static LocationPerspectiveInfo InspectMedicine(
            WorldState world,
            LocationState location)
        {
            var unhealthyPeople = 0;
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person.LocationId == location.Id &&
                    person.IsAlive &&
                    person.HealthBasisPoints < 9_000)
                {
                    unhealthyPeople++;
                }
            }

            var wounded = 0;
            for (var i = 0; i < world.Armies.Count; i++)
            {
                var army = world.Armies[i];
                if (army.LocationId == location.Id &&
                    !IsArmyMarching(world, army.Id))
                {
                    wounded += army.WoundedTroops;
                }
            }

            var herbPrice = FindCommodityPrice(
                world,
                location.Id,
                "commodity.herbs");
            var hasClinic =
                (location.Features & LocationFeature.Clinic) != 0;
            return new LocationPerspectiveInfo
            {
                PrimaryMetric = $"伤病{unhealthyPeople + wounded}",
                SecondaryMetric =
                    $"药{herbPrice}钱·{(hasClinic ? "有医馆" : "无医馆")}",
                EmphasisBasisPoints = ClampBasisPoints(
                    unhealthyPeople * 1_000 +
                    wounded * 2 +
                    (hasClinic ? 1_000 : 0)),
                VisibleFeatures =
                    location.Features &
                    (LocationFeature.Clinic |
                     LocationFeature.Market |
                     LocationFeature.RelayStation |
                     LocationFeature.Temple)
            };
        }

        private static bool IsArmyMarching(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindCommodityPrice(
            WorldState world,
            string locationId,
            string commodityId)
        {
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == locationId &&
                    listing.CommodityId == commodityId)
                {
                    return listing.Price;
                }
            }

            return 0;
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

            return null;
        }

        private static int ClampBasisPoints(int value)
        {
            return Math.Max(0, Math.Min(10_000, value));
        }

        private static string LocationKindName(LocationKind kind)
        {
            return kind == LocationKind.RegionalSeat ? "治所" :
                kind == LocationKind.Pass ? "关隘" :
                kind == LocationKind.Port ? "港口" :
                kind == LocationKind.MarketTown ? "市镇" :
                kind == LocationKind.Village ? "村庄" :
                kind == LocationKind.Camp ? "营地" :
                "县城";
        }

        private static string TerrainName(TerrainKind terrain)
        {
            return terrain == TerrainKind.Hills ? "丘陵" :
                terrain == TerrainKind.Mountains ? "山地" :
                terrain == TerrainKind.Riverland ? "河网" :
                terrain == TerrainKind.Forest ? "森林" :
                terrain == TerrainKind.Marsh ? "湿地" :
                "平原";
        }
    }
}
