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
        public void LuoyangRemainingFinalAssets_FreezesAndPreacceptsAll38Slots()
        {
            var worldMapRoot = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var coverage = new LuoyangFacilityModelCoverageSource(worldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(worldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(
                worldMapRoot, coverage.Bindings,
                coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, gates,
                performance).Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, performance).Catalog;
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    worldMapRoot, coverage.CombinedCatalog, landmarks,
                    performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(worldMapRoot,
                production, landmarks, gates, fabric, infrastructure,
                defense, resources, finalCivic, performance).Catalog;
            var remaining = new LuoyangRemainingFinalAssetSource(worldMapRoot,
                review);

            Assert.That(remaining.Catalog.Profiles, Has.Count.EqualTo(38));
            Assert.That(remaining.Plan.ProfilesByAssetVariantId,
                Has.Count.EqualTo(38));
            Assert.That(remaining.Catalog.Profiles.Sum(item =>
                item.FacilityUsageCount), Is.EqualTo(2068));
            Assert.That(remaining.Catalog.Profiles.Select(item =>
                    item.ReviewOrder), Is.EqualTo(
                    LuoyangRemainingFinalAssetIds.RemainingReviewOrders));
            Assert.That(remaining.Catalog.Profiles.All(item =>
                item.ArtistPrefabPresent && item.FinalArtApproved), Is.True);
            Assert.That(remaining.Catalog.UserDecisionRecordId, Is.EqualTo(
                LuoyangRemainingFinalAssetIds.UserDecisionRecordId));
            Assert.That(remaining.Catalog.UserDecisionId, Is.EqualTo(
                LuoyangRemainingFinalAssetIds.UserDecisionId));
            Assert.That(remaining.Catalog.Profiles.Count(item =>
                    item.PriorityId == LuoyangFinalAssetReviewIds.PriorityP0),
                Is.EqualTo(8));
            Assert.That(remaining.Catalog.Profiles.Count(item =>
                    item.PriorityId == LuoyangFinalAssetReviewIds.PriorityP1),
                Is.EqualTo(10));
            Assert.That(remaining.Catalog.Profiles.Count(item =>
                    item.PriorityId == LuoyangFinalAssetReviewIds.PriorityP2),
                Is.EqualTo(14));
            Assert.That(remaining.Catalog.Profiles.Count(item =>
                    item.PriorityId == LuoyangFinalAssetReviewIds.PriorityP3),
                Is.EqualTo(6));
        }
    }
}
