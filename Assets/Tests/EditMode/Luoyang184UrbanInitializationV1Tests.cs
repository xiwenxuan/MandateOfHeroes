using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class Luoyang184UrbanInitializationV1Tests
    {
        private static string RuntimeRoot => Path.Combine(
            Application.dataPath, "StreamingAssets", "WorldMap", "Luoyang184UrbanInitializationV1");

        [Test]
        public void ManifestFreezesAcceptedPopulationHierarchyAndIntegrityHashes()
        {
            var reader = new Luoyang184UrbanInitializationReader(RuntimeRoot);
            Assert.That(reader.Manifest.PersonCount, Is.EqualTo(270000));
            Assert.That(reader.Manifest.WalledCityPopulation, Is.EqualTo(200000));
            Assert.That(reader.Manifest.UrbanAreaPopulation, Is.EqualTo(270000));
            Assert.That(reader.Manifest.MetropolitanPlanPopulation, Is.EqualTo(400000));
            Assert.That(reader.Manifest.SupplyRegionPlanPopulation, Is.EqualTo(700000));
            Assert.That(reader.Manifest.PopulationProfileId,
                Is.EqualTo("population_profile.luoyang.184.urban_recommended"));
            Assert.That(reader.ValidatePackageFiles(), Is.Empty);
        }

        [Test]
        public void AllPersonsArePermanentUniqueHousedAndReferenceAValidHousehold()
        {
            var reader = new Luoyang184UrbanInitializationReader(RuntimeRoot);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var historical = 0;
            var generated = 0;
            var familyMembers = new Dictionary<ushort, int>();
            var expectedOrdinal = 0u;
            foreach (var person in reader.ReadPersons(0, reader.Manifest.PersonCount))
            {
                Assert.That(person.Ordinal, Is.EqualTo(expectedOrdinal));
                Assert.That(ids.Add(reader.GetPersonId(person.Ordinal)), Is.True, "Permanent PersonId must be unique.");
                Assert.That(person.HouseholdOrdinal, Is.LessThan((uint)reader.Manifest.HouseholdCount));
                Assert.That(person.ResidenceFacilityIndex, Is.Not.EqualTo(uint.MaxValue));
                Assert.That(person.ResidenceStatusIndex, Is.Not.Zero);
                Assert.That(person.HealthBasisPoints, Is.InRange(0, 10000));
                if (person.DataOriginIndex == 0) historical++;
                else if (person.DataOriginIndex == 2) generated++;
                else Assert.Fail("Formal population contains an engineering/stress origin at ordinal " + person.Ordinal);
                if (person.FamilyOrganizationIndex != ushort.MaxValue)
                {
                    familyMembers.TryGetValue(person.FamilyOrganizationIndex, out var current);
                    familyMembers[person.FamilyOrganizationIndex] = current + 1;
                }
                expectedOrdinal++;
            }

            Assert.That(expectedOrdinal, Is.EqualTo(270000));
            Assert.That(historical, Is.EqualTo(reader.Manifest.HistoricalPersonCount));
            Assert.That(generated + historical, Is.EqualTo(270000));
            Assert.That(familyMembers.OrderBy(item => item.Key).Select(item => item.Value),
                Is.EqualTo(new[] { 20, 250, 300, 350, 250, 100, 130 }));
        }

        [Test]
        public void HouseholdsCoverPopulationExactlyAndKeepFamilySeparate()
        {
            var reader = new Luoyang184UrbanInitializationReader(RuntimeRoot);
            var expectedOrdinal = 0u;
            var expectedMemberStart = 0u;
            var memberTotal = 0;
            foreach (var household in reader.ReadHouseholds(0, reader.Manifest.HouseholdCount))
            {
                Assert.That(household.Ordinal, Is.EqualTo(expectedOrdinal));
                Assert.That(household.MemberStartOrdinal, Is.EqualTo(expectedMemberStart));
                Assert.That(household.MemberCount, Is.GreaterThan(0));
                Assert.That(household.HeadOrdinal,
                    Is.InRange(household.MemberStartOrdinal, household.MemberStartOrdinal + household.MemberCount - 1u));
                Assert.That(household.ResidenceFacilityIndex, Is.Not.EqualTo(uint.MaxValue));
                expectedMemberStart += household.MemberCount;
                memberTotal += household.MemberCount;
                expectedOrdinal++;
            }

            Assert.That(expectedOrdinal, Is.EqualTo(reader.Manifest.HouseholdCount));
            Assert.That(memberTotal, Is.EqualTo(reader.Manifest.PersonCount));
            Assert.That(reader.Manifest.FamilyOrganizationCount, Is.EqualTo(7));
        }

        [Test]
        public void HistoricalEventsMutatePersonForceWorkAndLogisticsState()
        {
            var reader = new Luoyang184UrbanInitializationReader(RuntimeRoot);
            var state = reader.BuildScenarioState();
            var system = new Luoyang184UrbanHistoricalEventSystem();
            Assert.That(state.HistoricalPeople.Count, Is.EqualTo(25));
            Assert.That(state.Forces.Values.Sum(item => item.MemberCount), Is.EqualTo(34000));
            Assert.That(reader.Events.Count, Is.EqualTo(10));

            for (var index = 0; index < reader.Events.Count; index++)
            {
                Assert.That(system.ApplyNext(state, reader.Events), Is.Not.Null);
            }

            Assert.That(system.ApplyNext(state, reader.Events), Is.Null);
            Assert.That(state.AppliedEventIds.Count, Is.EqualTo(10));
            Assert.That(state.HistoricalPeople["P0054"].CurrentActivityId, Is.EqualTo("activity.detained"));
            Assert.That(state.HistoricalPeople["P0931"].CurrentLocationId, Is.EqualTo("cell.route.luoyang_julu"));
            Assert.That(state.HistoricalPeople["P0931"].CurrentActivityId, Is.EqualTo("activity.military_inspection"));
            Assert.That(state.Forces["force.han.luzhi_north"].Status, Is.EqualTo("Deployed"));
            Assert.That(state.PausedWorkForceIds, Does.Contain("force.han.luzhi_north"));
            Assert.That(state.MilitarySupplyPressure, Is.EqualTo(1200));
            Assert.That(state.TransportPressure, Is.EqualTo(600));
        }

        [Test]
        public void ChunkedDailyAndMonthlyAuditTicksCoverFormalPopulationWithoutActors()
        {
            var reader = new Luoyang184UrbanInitializationReader(RuntimeRoot);
            var state = reader.BuildScenarioState();
            var eventSystem = new Luoyang184UrbanHistoricalEventSystem();
            foreach (var definition in reader.Events)
            {
                eventSystem.Apply(state, definition);
            }

            var tickSystem = new Luoyang184UrbanPopulationAuditTickSystem();
            var daily = tickSystem.RunDaily(reader, state, 4096);
            var monthly = tickSystem.RunMonthly(reader, 4096);
            Assert.That(daily.PersonCount, Is.EqualTo(270000));
            Assert.That(daily.HousedCount, Is.EqualTo(270000));
            Assert.That(daily.AssignedWorkCount, Is.EqualTo(177962));
            Assert.That(daily.ActiveWorkCount, Is.LessThan(daily.AssignedWorkCount));
            Assert.That(monthly.HouseholdCount, Is.EqualTo(53992));
            Assert.That(monthly.HouseholdMemberCount, Is.EqualTo(270000));
            Assert.That(daily.DeterministicChecksum, Is.Not.Zero);
            Assert.That(monthly.DeterministicChecksum, Is.Not.Zero);
            TestContext.WriteLine("Daily audit tick ms=" + daily.ElapsedMilliseconds.ToString("F3"));
            TestContext.WriteLine("Monthly audit tick ms=" + monthly.ElapsedMilliseconds.ToString("F3"));
        }
    }
}
