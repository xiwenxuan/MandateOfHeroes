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
        private static string HistoricalPersonClanRuntimeRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets", "HistoricalPersons", "Han135260V1");

        [Test]
        public void HistoricalPersonClanManifestPreservesExistingScopeAndSeparation()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.That(reader.Manifest.YearStart, Is.EqualTo(135));
            Assert.That(reader.Manifest.YearEnd, Is.EqualTo(260));
            Assert.That(reader.Manifest.PersonCount, Is.EqualTo(1202));
            Assert.That(reader.Manifest.ClanCount, Is.EqualTo(39));
            Assert.That(reader.Manifest.BranchCount, Is.EqualTo(15));
            Assert.That(reader.Manifest.ScenarioCount, Is.EqualTo(13));
            Assert.That(reader.Manifest.FamilyOrganizationCount, Is.Zero);
            Assert.That(reader.Manifest.HouseholdCount, Is.Zero);
        }

        [Test]
        public void ExistingPersonIdsRemainUniqueFromP0001ThroughP1202()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.That(reader.GetPeople().Select(item => item.PersonId).Distinct().Count(), Is.EqualTo(1202));
            Assert.That(reader.GetPerson("P0001").CanonicalName, Is.EqualTo("刘志"));
            Assert.That(reader.GetPerson("P1202").CanonicalName, Is.EqualTo("杜友"));
        }

        [Test]
        public void SameDisplayNameNeverCausesAutomaticIdentityMerge()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.That(reader.GetPerson("P0182").CanonicalName, Is.EqualTo("孙夫人"));
            Assert.That(reader.GetPerson("P0239").CanonicalName, Is.EqualTo("孙夫人"));
            Assert.That(reader.GetPerson("P0182"), Is.Not.SameAs(reader.GetPerson("P0239")));
        }

        [Test]
        public void KinshipQueriesUsePersonIdsAndPreserveAdoptionSeparately()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            var query = new HanHistoricalPersonClanQuerySystem(reader);
            Assert.That(query.GetParents("P0377").Select(item => item.CanonicalName), Does.Contain("杨彪"));
            Assert.That(reader.GetKinship().Any(item => item.RelationType == "AdoptiveFather"), Is.True);
            Assert.That(reader.GetKinship().Any(item => item.PersonAId == item.PersonBId), Is.False);
        }

        [Test]
        public void MarriageDoesNotRewriteBirthClan()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            foreach (var marriage in reader.GetMarriages())
            {
                var a = reader.GetPerson(marriage.PersonAId);
                var b = reader.GetPerson(marriage.PersonBId);
                Assert.That(a.ClanId, Is.EqualTo(a.BirthClanId), marriage.MarriageId);
                Assert.That(b.ClanId, Is.EqualTo(b.BirthClanId), marriage.MarriageId);
            }
        }

        [Test]
        public void NativePlaceNeverBecomesHistoricalCurrentLocationByFallback()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.That(reader.GetLocations().All(item => item.ModelFallbackLocation == null), Is.True);
            var query = new HanHistoricalPersonClanQuerySystem(reader);
            Assert.That(query.GetPersonLocation("P0001", 140), Is.Null);
        }

        [Test]
        public void ThirteenFormalScenarioSnapshotsComeFromOneMasterTimeline()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            var ids = new[]
            {
                "scenario.han.140.peace", "scenario.han.184.yellow_turban", "scenario.han.189.luoyang_coup",
                "scenario.han.194.warlords", "scenario.han.200.guandu_eve", "scenario.han.207.longzhong",
                "scenario.han.214.yizhou_settled", "scenario.han.219.hanzhong_king", "scenario.han.223.baidicheng",
                "scenario.han.227.northern_expedition", "scenario.han.234.wuzhang", "scenario.han.249.gaopingling",
                "scenario.han.260.endgame"
            };
            foreach (var id in ids)
            {
                var snapshot = reader.LoadScenarioSnapshot(id);
                Assert.That(snapshot.SourceTimelineVersion, Is.EqualTo("han135260-person-clan-v1"), id);
                Assert.That(snapshot.Persons, Is.Not.Empty, id);
                Assert.That(snapshot.Clans.Count, Is.EqualTo(39), id);
            }
        }

        [Test]
        public void NonScenarioHistoricalTimePointIsDerivedWithoutSecondPersonTable()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            var snapshot = reader.LoadHistoricalSnapshot(190);
            Assert.That(snapshot.ScenarioId, Is.EqualTo("historical-time-point.190"));
            Assert.That(snapshot.Persons.Select(item => item.PersonId).Distinct().Count(), Is.EqualTo(snapshot.Persons.Count));
            Assert.That(snapshot.Clans.Count, Is.EqualTo(reader.GetClans().Count));
        }

        [Test]
        public void LuoyangHistoricalOverlayStillResolvesAllTwentyFivePeople()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            var luoyangPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets", "WorldMap", "Luoyang184UrbanInitializationV1", "historical_persons.json");
            var text = File.ReadAllText(luoyangPath);
            Assert.That(reader.GetPeople().Count(item => text.Contains("\"person_id\": \"" + item.PersonId + "\"")), Is.GreaterThanOrEqualTo(25));
            Assert.That(reader.LoadScenarioSnapshot("scenario.han.184.yellow_turban").Year, Is.EqualTo(184));
        }

        [Test]
        public void ClanQueriesReturnBranchesMembersPresenceAndMarriageNetwork()
        {
            var query = new HanHistoricalPersonClanQuerySystem(new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot));
            const string clanId = "clan.han.v1.f120";
            Assert.That(query.GetClan(clanId).CanonicalClanName, Is.EqualTo("琅琊诸葛氏"));
            Assert.That(query.GetClanBranches(clanId).Count, Is.EqualTo(3));
            Assert.That(query.GetClanMembers(clanId, 219), Is.Not.Empty);
            Assert.That(query.GetClanPresence(clanId, 219), Is.Not.Empty);
        }

        [Test]
        public void HistoricalPersonClanRuntimePackageHashesAreIntact()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.That(reader.ValidatePackageFiles(), Is.Empty);
        }

        [Test]
        public void HistoricalSnapshotRejectsYearsOutsideFormalResearchWindow()
        {
            var reader = new HanHistoricalPersonClanDatasetReader(HistoricalPersonClanRuntimeRoot);
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.LoadHistoricalSnapshot(134));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.LoadHistoricalSnapshot(261));
        }
    }
}
