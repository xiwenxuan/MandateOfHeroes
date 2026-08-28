using System;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public sealed class LuoyangHumanScaleLocalMapPlanSource
    {
        public LuoyangHumanScaleLocalMapPlanSource(string worldMapRoot)
        {
            if (string.IsNullOrWhiteSpace(worldMapRoot))
                throw new ArgumentException("World map root is required.",
                    nameof(worldMapRoot));
            var coverage = new LuoyangFacilityModelCoverageSource(
                worldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(worldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                worldMapRoot, coverage.CombinedCatalog).Catalog;
            Performance = new LuoyangBuildingPerformancePlanSource(
                worldMapRoot, coverage.Bindings,
                coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, Performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, gates,
                Performance).Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                worldMapRoot, coverage.CombinedCatalog, Performance).Catalog;
            var civic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    worldMapRoot, coverage.CombinedCatalog, landmarks,
                    Performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(
                worldMapRoot, production, landmarks, gates, fabric,
                infrastructure, defense, resources, civic,
                Performance).Plan;
            Composition = LuoyangWholeCityCompositionRules.CreatePlan(
                Performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(Performance, Composition);
            StrategicRoads = LuoyangRoadConnectorPassageTraversalRules
                .CreatePlan(interaction);
            Plan = LuoyangHumanScaleLocalMapRules.CreatePlan(Performance,
                Composition, StrategicRoads);
        }

        public LuoyangBuildingPerformancePlan Performance { get; }
        public LuoyangWholeCityCompositionPlan Composition { get; }
        public LuoyangRoadTraversalRefinementPlan StrategicRoads { get; }
        public LuoyangHumanScaleLocalMapPlan Plan { get; }
    }
}
