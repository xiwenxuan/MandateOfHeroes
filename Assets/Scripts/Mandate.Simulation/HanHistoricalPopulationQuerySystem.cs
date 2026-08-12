using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class HanHistoricalPopulationQuerySystem
    {
        private readonly IHanNationalPopulationSnapshotSource source;

        public HanHistoricalPopulationQuerySystem(IHanNationalPopulationSnapshotSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public HanNationalPopulationRecord NationalPopulation(int year)
        {
            return source.LoadPopulationSnapshot(year).National;
        }

        public IReadOnlyList<HanProvincePopulationRecord> ProvincePopulation(int year)
        {
            return source.LoadPopulationSnapshot(year).Provinces;
        }

        public IReadOnlyList<HanRegionPopulationRecord> CommanderyEquivalentPopulation(int year)
        {
            return source.LoadPopulationSnapshot(year).Regions;
        }

        public IReadOnlyList<HanCountyPopulationRecord> CountyPopulation(int year)
        {
            return source.LoadPopulationSnapshot(year).Counties;
        }

        public IReadOnlyList<HanMajorCityPopulationRecord> MajorCityPopulation(int year)
        {
            return source.LoadPopulationSnapshot(year).MajorCities;
        }

        public HanPopulationYearSnapshot LoadScenarioPopulation(string scenarioId)
        {
            return source.LoadScenarioSnapshot(scenarioId);
        }

        public HanRegionPopulationRecord FindRegion(int year, string regionPermanentId)
        {
            if (string.IsNullOrWhiteSpace(regionPermanentId)) throw new ArgumentException("A region ID is required.", nameof(regionPermanentId));
            return source.LoadPopulationSnapshot(year).Regions.Single(row => string.Equals(row.RegionPermanentId, regionPermanentId, StringComparison.Ordinal));
        }

        public HanCountyPopulationRecord FindCounty(int year, string countyPermanentId)
        {
            if (string.IsNullOrWhiteSpace(countyPermanentId)) throw new ArgumentException("A county ID is required.", nameof(countyPermanentId));
            return source.LoadPopulationSnapshot(year).Counties.Single(row => string.Equals(row.CountyPermanentId, countyPermanentId, StringComparison.Ordinal));
        }
    }
}
