using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MarketSimulationSystem
    {
        private readonly NamedRandom _random;

        public MarketSimulationSystem(ulong masterSeed)
        {
            _random = new NamedRandom(masterSeed);
        }

        public void ResolveDailyPrices(WorldState world)
        {
            var listings = new List<MarketListingState>(world.MarketListings);
            listings.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < listings.Count; i++)
            {
                var listing = listings[i];
                var scarcityAdjustment =
                    (long)(listing.TargetStock - listing.Stock) *
                    listing.EquilibriumPrice /
                    listing.TargetStock /
                    2;
                var targetPrice = checked(
                    listing.EquilibriumPrice + (int)scarcityAdjustment);
                var trend = (targetPrice - listing.Price) / 8;
                var noise = _random.Range(
                    "commodity_market",
                    new StableId(listing.Id),
                    world.AbsoluteDay,
                    "daily_price_noise",
                    -2,
                    3);
                listing.Price = Clamp(
                    listing.Price + trend + noise,
                    Math.Max(1, listing.EquilibriumPrice / 4),
                    checked(listing.EquilibriumPrice * 4));
                SyncLegacyGrainPrice(world, listing);
            }
        }

        private static void SyncLegacyGrainPrice(
            WorldState world,
            MarketListingState listing)
        {
            if (listing.CommodityId != "commodity.grain")
            {
                return;
            }

            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == listing.LocationId)
                {
                    world.Locations[i].GrainPrice = listing.Price;
                    return;
                }
            }
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
