using System;
using System.IO;
using System.Linq;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        private static string RuntimeRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets", "HistoricalPopulation", "Han135260V1");

        [Test]
        public void ManifestProtectsScopeAnchorsAndPermanentPersonBoundary()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            Assert.That(reader.Manifest.YearStart, Is.EqualTo(135));
            Assert.That(reader.Manifest.YearEnd, Is.EqualTo(260));
            Assert.That(reader.Manifest.YearCount, Is.EqualTo(126));
            Assert.That(reader.Manifest.ProvinceCount, Is.EqualTo(13));
            Assert.That(reader.Manifest.RegionCount, Is.EqualTo(105));
            Assert.That(reader.Manifest.CountyCount, Is.EqualTo(1182));
            Assert.That(reader.Manifest.CountyYearRecordCount, Is.EqualTo(148932));
            Assert.That(reader.Manifest.NationalAnchor140Registered, Is.EqualTo(49150220));
            Assert.That(reader.Manifest.NationalAnchor157Registered, Is.EqualTo(56486856));
            Assert.That(reader.Manifest.PermanentPersonsGenerated, Is.Zero);
        }

        [Test]
        public void EveryYearLoadsAndConservesNationalProvinceRegionCountyAndMigration()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            long previousActualEnd = -1;
            long previousRegisteredEnd = -1;
            for (var year = 135; year <= 260; year++)
            {
                var snapshot = reader.LoadPopulationSnapshot(year);
                Assert.That(snapshot.Provinces.Count, Is.EqualTo(13), year.ToString());
                Assert.That(snapshot.Regions.Count, Is.EqualTo(105), year.ToString());
                Assert.That(snapshot.Counties.Count, Is.EqualTo(1182), year.ToString());
                Assert.That(snapshot.Provinces.Sum(row => row.ModeledActualPopulation), Is.EqualTo(snapshot.National.ModeledActualPopulationStart), year.ToString());
                Assert.That(snapshot.Regions.Sum(row => row.ModeledActualPopulation), Is.EqualTo(snapshot.National.ModeledActualPopulationStart), year.ToString());
                Assert.That(snapshot.Counties.Sum(row => row.ModeledActualPopulation), Is.EqualTo(snapshot.National.ModeledActualPopulationStart), year.ToString());
                Assert.That(snapshot.Regions.Sum(row => row.NetMigration), Is.Zero, year.ToString());
                Assert.That(snapshot.Conservation.Status, Is.EqualTo("PASS"), year.ToString());
                if (previousActualEnd >= 0)
                {
                    Assert.That(snapshot.National.ModeledActualPopulationStart, Is.EqualTo(previousActualEnd), year.ToString());
                    Assert.That(snapshot.National.RegisteredPopulationStart, Is.EqualTo(previousRegisteredEnd), year.ToString());
                }
                previousActualEnd = snapshot.National.ModeledActualPopulationEnd;
                previousRegisteredEnd = snapshot.National.RegisteredPopulationEnd;
            }
        }

        [Test]
        public void BackcastConvergesToProtectedOneFortyAnchorWithoutDiscontinuity()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            var previous = reader.LoadPopulationSnapshot(135);
            for (var year = 136; year <= 140; year++)
            {
                var current = reader.LoadPopulationSnapshot(year);
                Assert.That(previous.National.ModeledActualPopulationEnd, Is.EqualTo(current.National.ModeledActualPopulationStart));
                Assert.That(previous.National.RegisteredPopulationEnd, Is.EqualTo(current.National.RegisteredPopulationStart));
                previous = current;
            }
            Assert.That(previous.National.RegisteredPopulationStart, Is.EqualTo(49150220));
            Assert.That(previous.National.HistoricalAnchors, Is.EqualTo("140_HOU_HAN_SHU"));
        }

        [Test]
        public void OneEightyFourStartAndEndKeepYellowTurbanImpactInTheYear()
        {
            var snapshot = new HanNationalPopulationDatasetReader(RuntimeRoot).LoadPopulationSnapshot(184);
            Assert.That(snapshot.SnapshotMoment, Is.EqualTo("YEAR_START"));
            Assert.That(snapshot.National.ModeledActualPopulationStart, Is.EqualTo(53500000));
            Assert.That(snapshot.National.ModeledActualPopulationEnd, Is.EqualTo(51500000));
            Assert.That(snapshot.National.WarDeaths, Is.GreaterThan(0));
            Assert.That(snapshot.Regions.SelectMany(row => row.ActiveEventIds), Does.Contain("pop.event.war.184.yellow_turban"));
        }

        [Test]
        public void ThirteenScenarioSnapshotsAreDirectTimelineReferences()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            var scenarios = new[] { 140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260 };
            for (var index = 0; index < scenarios.Length; index++)
            {
                var scenarioId = "S" + (index + 1).ToString("D2") + "_" + scenarios[index];
                var scenario = reader.LoadScenarioSnapshot(scenarioId);
                var annual = reader.LoadPopulationSnapshot(scenarios[index]);
                Assert.That(scenario, Is.SameAs(annual), scenarioId);
                Assert.That(scenario.National.ModeledActualPopulationStart, Is.EqualTo(annual.National.ModeledActualPopulationStart), scenarioId);
            }
        }

        [Test]
        public void CountyAllocationUsesAllPermanentIdsAndIsNotAverageDivision()
        {
            var snapshot = new HanNationalPopulationDatasetReader(RuntimeRoot).LoadPopulationSnapshot(184);
            Assert.That(snapshot.Counties.Select(row => row.CountyPermanentId).Distinct().Count(), Is.EqualTo(1182));
            Assert.That(snapshot.Counties.Select(row => row.CountyWeight).Distinct().Count(), Is.GreaterThan(1000));
            var henan = snapshot.Counties.Where(row => row.ParentRegionPermanentId == "admin.han140.sili.henan").ToArray();
            Assert.That(henan.Select(row => row.ModeledActualPopulation).Distinct().Count(), Is.GreaterThan(10));
            Assert.That(henan.Single(row => row.CountyPermanentId == "admin.han140.sili.henan.luoyang").ModeledActualPopulation,
                Is.GreaterThan(henan.Min(row => row.ModeledActualPopulation)));
        }

        [Test]
        public void QuerySystemReturnsNationalRegionCountyAndScenarioFromOneSource()
        {
            var system = new HanHistoricalPopulationQuerySystem(new HanNationalPopulationDatasetReader(RuntimeRoot));
            Assert.That(system.NationalPopulation(260).ModeledActualPopulationStart, Is.EqualTo(32000000));
            Assert.That(system.ProvincePopulation(184).Count, Is.EqualTo(13));
            Assert.That(system.CommanderyEquivalentPopulation(184).Count, Is.EqualTo(105));
            Assert.That(system.CountyPopulation(184).Count, Is.EqualTo(1182));
            Assert.That(system.FindRegion(184, "admin.han140.sili.henan").HistoricalName, Is.EqualTo("河南尹"));
            Assert.That(system.FindCounty(184, "admin.han140.sili.henan.luoyang").HistoricalCountyName, Is.EqualTo("雒阳"));
            Assert.That(system.LoadScenarioPopulation("S02_184").Year, Is.EqualTo(184));
        }

        [Test]
        public void ReaderRejectsYearsOutsideFormalTimeline()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.LoadPopulationSnapshot(134));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.LoadPopulationSnapshot(261));
        }

        [Test]
        public void RuntimePackageHashesAreIntact()
        {
            var reader = new HanNationalPopulationDatasetReader(RuntimeRoot);
            Assert.That(reader.ValidatePackageFiles(), Is.Empty);
        }
    }
}
