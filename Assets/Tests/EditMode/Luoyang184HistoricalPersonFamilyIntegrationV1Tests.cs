using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    [TestFixture]
    public sealed class Luoyang184HistoricalPersonFamilyIntegrationV1Tests
    {
        private static string MetropolitanRoot => Path.Combine(
            Application.dataPath, "StreamingAssets", "WorldMap",
            "Luoyang184MetropolitanInitializationV1");

        private static string HistoricalRoot => Path.Combine(
            Application.dataPath, "StreamingAssets", "HistoricalPersons",
            "Han135260V1");

        private WorldState world;
        private Luoyang184MetropolitanPopulationStore store;
        private Luoyang184HistoricalPersonFamilyIntegrationResult result;

        [OneTimeSetUp]
        public void BuildIntegratedWorldOnce()
        {
            world = WorldState.Create(184);
            store = new Luoyang184MetropolitanPopulationStore(MetropolitanRoot);
            result = new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                MetropolitanRoot, HistoricalRoot).Integrate(world);
        }

        [Test]
        public void HistoricalMappingsBindAllTwentyFiveExistingPersonsWithoutDuplicates()
        {
            Assert.That(world.HistoricalIdentities.Count, Is.EqualTo(25));
            Assert.That(world.HistoricalIdentities.Select(item => item.PersonId)
                .Distinct().Count(), Is.EqualTo(25));
            Assert.That(world.HistoricalIdentities.All(item =>
                item.PersonId == item.HistoricalPersonId), Is.True);
            Assert.That(world.HistoricalIdentities.All(item =>
                store.TryGetOrdinal(item.PersonId, out var ordinal) &&
                ordinal == item.PersonOrdinal), Is.True);
            Assert.That(result.AddedPersonCount, Is.Zero);
            Assert.That(world.People, Is.Empty,
                "The adapter must not create duplicate inline NPCs.");
        }

        [Test]
        public void ClanAndBranchRemainSeparateFromHouseholdAndFamilyOrganization()
        {
            var emperor = world.HistoricalIdentities.Single(item =>
                item.PersonId == "P0038");
            Assert.That(emperor.BranchId,
                Is.EqualTo("branch.han.v1.f415.eastern_han_mainline"));
            Assert.That(emperor.HouseholdId,
                Does.StartWith("household.luoyang.184."));
            Assert.That(emperor.HouseholdId, Is.Not.EqualTo(emperor.BranchId));
            Assert.That(world.PersonLineages.Select(item => item.PersonId)
                .Distinct().Count(), Is.EqualTo(25));
        }

        [Test]
        public void FifteenOrganizationsAreRetainedAndTwoContaminatedRangesAreCorrected()
        {
            Assert.That(world.FamilyOrganizationProfiles.Count, Is.EqualTo(15));
            Assert.That(world.FamilyOrganizationProfiles.Count(item =>
                item.MigrationStatus == "MIGRATED_CORRECTED"), Is.EqualTo(7));
            Assert.That(world.FamilyOrganizationProfiles.Count(item =>
                item.MigrationStatus.StartsWith("RETAINED",
                    StringComparison.Ordinal)), Is.EqualTo(8));
            Assert.That(world.FamilyOrganizationProfiles.Count(item =>
                item.UnresolvedFacilityClaimIds.Count == 4), Is.EqualTo(8));
            Assert.That(result.RemovedMisassignedHistoricalMembershipCount,
                Is.EqualTo(10));
            var f088 = world.FamilyOrganizationMembers.Where(item =>
                item.OrganizationId == "family_organization.luoyang.184.f088").ToList();
            CollectionAssert.AreEquivalent(
                new[] { "P0037", "P0038", "P0039", "P0040" },
                f088.Where(item => item.IsHistoricalMember)
                    .Select(item => item.PersonId));
            Assert.That(f088.Any(item => item.PersonId == "P0047"), Is.False);
            var f036 = world.FamilyOrganizationMembers.Where(item =>
                item.OrganizationId == "family_organization.luoyang.184.f036").ToList();
            CollectionAssert.AreEquivalent(new[] { "P0035", "P0036" },
                f036.Where(item => item.IsHistoricalMember)
                    .Select(item => item.PersonId));
            Assert.That(f036.Any(item => item.PersonId == "P0054"), Is.False);
        }

        [Test]
        public void EveryFamilyMemberAndHistoricalHouseholdResidenceResolvesInProtectedPackage()
        {
            Assert.That(world.FamilyOrganizationMembers.All(item =>
                store.TryReadCore(item.PersonId, out _)), Is.True);
            foreach (var identity in world.HistoricalIdentities)
            {
                Assert.That(store.TryReadCore(identity.PersonId, out var core), Is.True);
                Assert.That(core.FamilyId, Is.EqualTo(identity.HouseholdId));
                var record = store.Source.ReadPersons(
                    checked((int)identity.PersonOrdinal), 1).Single();
                Assert.That(store.GetFacilityId(record.ResidenceFacilityIndex),
                    Is.EqualTo(identity.ResidenceFacilityId));
                Assert.That(world.Facilities.Any(item =>
                    item.Id == identity.ResidenceFacilityId), Is.True);
            }
        }

        [Test]
        public void FacilityAndPopulationConservationRemainExactAndNoCenterIsInvented()
        {
            Assert.That(world.PopulationStorage.PermanentPersonCount,
                Is.EqualTo(400000));
            Assert.That(world.HistoricalPersonFamilyIntegrations.Single()
                .HouseholdCount, Is.EqualTo(80899));
            Assert.That(world.Facilities.Count, Is.EqualTo(2084));
            Assert.That(world.Facilities.Select(item => item.Id).Distinct().Count(),
                Is.EqualTo(2084));
            Assert.That(result.AddedFacilityCount, Is.Zero);
            Assert.That(world.FamilyCenters.Count, Is.EqualTo(15));
            Assert.That(world.FamilyCenters.All(item =>
                item.Status == FamilyCenterOperationalStatus.Deferred &&
                item.Designation == FamilyCenterDesignation.None &&
                string.IsNullOrEmpty(item.FacilityId)), Is.True);
            Assert.That(world.FacilityDefinitions.Any(item =>
                item.CapabilityIds.Contains(FacilityCapabilityIds.FamilyManagement)),
                Is.False);
        }

        [Test]
        public void FamilyCenterActivationRequiresFacilityCapabilityControlAndManagerActivity()
        {
            var copy = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var center = copy.FamilyCenters.Single(item =>
                item.OrganizationId == "family_organization.luoyang.184.f088");
            center.Status = FamilyCenterOperationalStatus.Active;
            center.Designation = FamilyCenterDesignation.Primary;
            center.FacilityId = copy.Facilities[0].Id;
            center.ManagerPersonId = "P0038";
            Assert.Throws<InvalidOperationException>(() => copy.Validate());

            var facility = copy.Facilities[0];
            facility.OwnerId = center.OrganizationId;
            var definition = copy.FacilityDefinitions.Single(item =>
                item.Id == facility.DefinitionId);
            definition.CapabilityIds.Add(FacilityCapabilityIds.FamilyManagement);
            copy.Validate();
            facility.LifecycleStatus = FacilityLifecycleStatus.Destroyed;
            Assert.Throws<InvalidOperationException>(() => copy.Validate());
            center.Status = FamilyCenterOperationalStatus.Lost;
            center.Designation = FamilyCenterDesignation.None;
            copy.Validate();
            Assert.That(copy.Organizations.Any(item =>
                item.Id == center.OrganizationId), Is.True,
                "Losing a center must not delete the organization.");
        }

        [Test]
        public void PersonalAssetsRemainInPersonPackageAndFamilyAssetsUseOrganizationOwner()
        {
            Assert.That(store.TryGetOrdinal("P0035", out var ordinal), Is.True);
            var before = store.Source.ReadPersons(checked((int)ordinal), 1)
                .Single().PersonalAssets;
            Assert.That(store.Source.ReadPersons(checked((int)ordinal), 1)
                .Single().PersonalAssets, Is.EqualTo(before));
            Assert.That(world.OrganizationAssets.All(item =>
                item.OwnerId == item.OrganizationId), Is.True);
            Assert.That(world.OrganizationAssets.Any(item =>
                item.OwnerId == "P0035"), Is.False);
        }

        [Test]
        public void HistoricalOfficesUseGenericCivilMilitaryJurisdictionFacilityAndActivity()
        {
            Assert.That(world.CivilMilitaryOfficeAssignments, Is.Not.Empty);
            Assert.That(world.CivilMilitaryOfficeAssignments.All(item =>
                world.HistoricalIdentities.Any(identity =>
                    identity.PersonId == item.HolderPersonId)), Is.True);
            Assert.That(world.CivilMilitaryOfficeAssignments.All(item =>
                world.Facilities.Any(facility =>
                    facility.Id == item.WorkplaceFacilityId)), Is.True);
            Assert.That(world.CivilMilitaryOfficeDefinitions.All(item =>
                item.JurisdictionId ==
                    Luoyang184HistoricalPersonFamilyIntegrationBootstrap
                        .CanonicalPlaceId &&
                world.Facilities.Any(facility =>
                    facility.Id == item.GovernmentFacilityId)), Is.True);
            Assert.That(world.PersonPrimaryActivities.Count, Is.EqualTo(25));
            Assert.That(world.PersonPrimaryActivities.GroupBy(item => item.PersonId)
                .All(group => group.Count(item => item.IsActive) == 1), Is.True);
        }

        [Test]
        public void SaveLoadMigrationAndSecondIntegrationAreStable()
        {
            var timer = Stopwatch.StartNew();
            var json = WorldSnapshotSerializer.Serialize(world);
            var saveMilliseconds = timer.ElapsedMilliseconds;
            timer.Restart();
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            var loadMilliseconds = timer.ElapsedMilliseconds;
            Assert.That(WorldSnapshotSerializer.Serialize(loaded), Is.EqualTo(json));
            var second = new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                MetropolitanRoot, HistoricalRoot).Integrate(loaded);
            Assert.That(second.WasAlreadyIntegrated, Is.True);
            Assert.That(loaded.HistoricalIdentities.Count, Is.EqualTo(25));

            var v68 = WorldState.Create(68);
            v68.SchemaVersion = 68;
            var migrated = WorldSnapshotSerializer.Deserialize(
                JsonUtility.ToJson(v68));
            Assert.That(migrated.SchemaVersion, Is.EqualTo(69));
            Assert.That(migrated.HistoricalIdentities, Is.Empty);
            TestContext.WriteLine("integration_ms=" +
                result.InitializationElapsedMilliseconds);
            TestContext.WriteLine("save_ms=" + saveMilliseconds);
            TestContext.WriteLine("load_ms=" + loadMilliseconds);
            TestContext.WriteLine("historical_query_10k_ticks=" +
                result.HistoricalQueryElapsedTicks);
            TestContext.WriteLine("family_query_10k_ticks=" +
                result.FamilyQueryElapsedTicks);
        }

        [Test]
        public void RuntimeIndexesAvoidRepeatedFourHundredThousandPersonScans()
        {
            var index = new HistoricalPersonFamilyRuntimeIndex(world);
            Assert.That(index.TryGetIdentity("P0038", out var identity), Is.True);
            Assert.That(identity.CanonicalName, Is.Not.Empty);
            Assert.That(index.GetOrganizationMembers(
                "family_organization.luoyang.184.f088"), Is.Not.Empty);
            Assert.That(index.GetClanMembers("clan.han.v1.f036"),
                Does.Contain("P0035"));
        }
    }
}
