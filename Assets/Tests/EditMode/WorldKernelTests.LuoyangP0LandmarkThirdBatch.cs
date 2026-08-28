using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangP0LandmarkThirdBatch_FreezesFourAcceptedActivatedSlots()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            var coverage = new LuoyangFacilityModelCoverageSource(root);
            var production = new LuoyangProductionBuildingKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(root,
                coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(root,
                coverage.Bindings, coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                root, coverage.CombinedCatalog, gates, performance).Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                root, coverage.CombinedCatalog, performance).Catalog;
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(root,
                    coverage.CombinedCatalog, landmarks, performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, finalCivic, performance).Catalog;
            var batch = new LuoyangP0LandmarkThirdBatchSource(root,
                coverage.CombinedCatalog, landmarks, review);

            Assert.That(batch.Plan.ProfilesByFacilityId.Keys,
                Is.EquivalentTo(LuoyangP0LandmarkThirdBatchIds.FacilityIds));
            Assert.That(batch.Catalog.Profiles.Select(item =>
                item.ReviewOrder), Is.EqualTo(new[] { 6, 7, 8, 9 }));
            Assert.That(batch.Catalog.Profiles.All(item =>
                item.ArtistPrefabPresent && item.FinalArtApproved), Is.True);
            Assert.That(batch.Catalog.UserReviewDecisionRecordId, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.UserReviewDecisionRecordId));
        }
    }
}
