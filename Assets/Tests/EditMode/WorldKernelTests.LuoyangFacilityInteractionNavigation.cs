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
        public void LuoyangFacilityInteractionNavigation_CoversCityAndConnectsPassages()
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
                worldMapRoot, coverage.CombinedCatalog, gates, performance)
                .Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, performance).Catalog;
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    worldMapRoot, coverage.CombinedCatalog, landmarks,
                    performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(worldMapRoot,
                production, landmarks, gates, fabric, infrastructure,
                defense, resources, finalCivic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var plan = LuoyangFacilityInteractionNavigationRules.CreatePlan(
                performance, composition);

            Assert.That(plan.SelectionProxies.Count, Is.EqualTo(2084));
            Assert.That(plan.NavigationNodes.Count, Is.EqualTo(379));
            Assert.That(plan.NavigationEdges.Count, Is.EqualTo(382));
            Assert.That(plan.NavigationEdges.Count(item => item.Provisional),
                Is.EqualTo(28));
            var path = LuoyangFacilityInteractionNavigationRules
                .FindFacilityPath(plan, plan.NavigationNodes.First().FacilityId,
                    plan.NavigationNodes.Last().FacilityId);
            Assert.That(path.Count, Is.GreaterThan(1));
        }
    }
}
