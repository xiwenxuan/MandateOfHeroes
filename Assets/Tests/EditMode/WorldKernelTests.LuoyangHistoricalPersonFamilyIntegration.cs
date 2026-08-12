using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        private static string LuoyangFamilyMetroRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "WorldMap", "Luoyang184MetropolitanInitializationV1");

        private static string LuoyangFamilyHistoricalRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "HistoricalPersons", "Han135260V1");

        [Test]
        public void LuoyangHistoricalFamily_IntegratesWithoutChangingProtectedCounts()
        {
            var world = WorldState.Create(184);
            var result = new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                LuoyangFamilyMetroRoot, LuoyangFamilyHistoricalRoot).Integrate(world);
            Assert.That(result.HistoricalPersonCount, Is.EqualTo(25));
            Assert.That(result.FamilyOrganizationCount, Is.EqualTo(15));
            Assert.That(result.FacilityCount, Is.EqualTo(2084));
            Assert.That(result.AddedPersonCount, Is.Zero);
            Assert.That(result.AddedFacilityCount, Is.Zero);
            Assert.That(world.PopulationStorage.PermanentPersonCount,
                Is.EqualTo(400000));
            Assert.That(world.FamilyCenters.All(item =>
                item.Status == FamilyCenterOperationalStatus.Deferred), Is.True);
        }

        [Test]
        public void LuoyangHistoricalFamily_RoundTripPreservesIdentityAndOwnership()
        {
            var world = WorldState.Create(184);
            new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                LuoyangFamilyMetroRoot, LuoyangFamilyHistoricalRoot).Integrate(world);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            Assert.That(loaded.HistoricalIdentities.Select(item => item.PersonId),
                Is.EquivalentTo(world.HistoricalIdentities.Select(item => item.PersonId)));
            Assert.That(loaded.OrganizationAssets.All(item =>
                item.OwnerId == item.OrganizationId), Is.True);
            Assert.That(loaded.FamilyOrganizationProfiles.Sum(item =>
                item.UnresolvedFacilityClaimIds.Count), Is.EqualTo(32));
        }

        [Test]
        public void LuoyangHistoricalFamily_SameInputsProduceSameSnapshot()
        {
            var left = WorldState.Create(184);
            var right = WorldState.Create(184);
            var bootstrap = new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                LuoyangFamilyMetroRoot, LuoyangFamilyHistoricalRoot);
            bootstrap.Integrate(left);
            bootstrap.Integrate(right);
            left.HistoricalPersonFamilyIntegrations[0]
                .InitializationElapsedMilliseconds = 0;
            right.HistoricalPersonFamilyIntegrations[0]
                .InitializationElapsedMilliseconds = 0;
            Assert.That(WorldSnapshotSerializer.Serialize(left),
                Is.EqualTo(WorldSnapshotSerializer.Serialize(right)));
        }
    }
}
