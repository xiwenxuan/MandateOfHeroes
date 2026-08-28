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
        public void LuoyangWholeCityComposition_Covers2084FacilitiesWithoutSubCells()
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
                defense, resources, finalCivic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);

            Assert.That(composition.Anchors.Count, Is.EqualTo(2084));
            Assert.That(composition.Anchors.Select(item => item.CellId64)
                .Distinct().Count(), Is.EqualTo(2084));
            Assert.That(composition.Anchors.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(54));
            Assert.That(composition.FacilityCountByDistrict.Count,
                Is.EqualTo(6));
            Assert.That(composition.CreatesSimulationSubCells, Is.False);
            Assert.That(LuoyangWholeCityCompositionRules
                .SelectDensestResidentAnchors(composition, performance).Count,
                Is.EqualTo(549));
        }
    }
}
