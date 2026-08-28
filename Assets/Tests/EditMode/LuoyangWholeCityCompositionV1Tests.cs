using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class LuoyangWholeCityCompositionV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Plan_CoversAllFacilitiesFinalAssetsAndSixDistricts()
        {
            var plan = Load();

            Assert.That(plan.ContractId, Is.EqualTo(
                LuoyangWholeCityCompositionIds.ContractId));
            Assert.That(plan.StatusId, Is.EqualTo(
                LuoyangWholeCityCompositionIds.StatusId));
            Assert.That(plan.CreatesSimulationSubCells, Is.False);
            Assert.That(plan.Anchors.Count, Is.EqualTo(2084));
            Assert.That(plan.AnchorsByFacilityId.Count, Is.EqualTo(2084));
            Assert.That(plan.Anchors.Select(item => item.CellId64).Distinct()
                .Count(), Is.EqualTo(2084));
            Assert.That(plan.Anchors.Select(item => item.AssetVariantId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(54));
            Assert.That(plan.FacilityCountByDistrict.Count, Is.EqualTo(6));
            Assert.That(LuoyangWholeCityCompositionIds.DistrictIds.All(id =>
                plan.FacilityCountByDistrict.TryGetValue(id, out var count) &&
                count > 0), Is.True);
            Assert.That(plan.FacilityCountByDistrict.Values.Sum(),
                Is.EqualTo(2084));
        }

        [Test]
        public void Anchors_AreDeterministicRoadFacingAndCellLocalOnly()
        {
            var first = Load();
            var second = Load();
            var firstRows = first.Anchors.Select(Identity).ToArray();
            var secondRows = second.Anchors.Select(Identity).ToArray();

            Assert.That(secondRows, Is.EqualTo(firstRows));
            Assert.That(first.Anchors.All(item =>
                item.TerrainGroundingRequired), Is.True);
            Assert.That(first.Anchors.Where(item => item.CorridorAligned)
                .All(item => Math.Abs(item.VisualLocalEastMetres) < 0.001f &&
                             Math.Abs(item.VisualLocalNorthMetres) < 0.001f),
                Is.True);
            Assert.That(first.Anchors.Where(item => !item.CorridorAligned)
                .All(item => Math.Abs(item.VisualLocalEastMetres) ==
                                 LuoyangWholeCityCompositionIds
                                     .FrontageOffsetMetres ||
                             Math.Abs(item.VisualLocalNorthMetres) ==
                                 LuoyangWholeCityCompositionIds
                                     .FrontageOffsetMetres), Is.True);
            Assert.That(first.Anchors.All(item =>
                Math.Abs(item.VisualLocalEastMetres) <=
                    LuoyangWholeCityCompositionIds.MaximumLocalOffsetMetres &&
                Math.Abs(item.VisualLocalNorthMetres) <=
                    LuoyangWholeCityCompositionIds.MaximumLocalOffsetMetres &&
                item.RotationDegrees % 90f == 0f), Is.True);
        }

        [Test]
        public void DenseWindow_GroundsExactly549ComposedFacilities()
        {
            var loaded = LoadWithPerformance();
            var anchors = LuoyangWholeCityCompositionRules
                .SelectDensestResidentAnchors(loaded.Composition,
                    loaded.Performance);

            Assert.That(anchors.Count, Is.EqualTo(549));
            Assert.That(anchors.Select(item => item.FacilityId).Distinct()
                .Count(), Is.EqualTo(549));
            Assert.That(anchors.All(item =>
                item.GridColumn >= 2040 && item.GridColumn < 2064 &&
                item.GridRow >= 1224 && item.GridRow < 1248 &&
                item.TerrainGroundingRequired), Is.True);
            Assert.That(anchors.Select(item => item.DistrictId).Distinct()
                .Count(), Is.GreaterThanOrEqualTo(4));
        }

        private static LuoyangWholeCityCompositionPlan Load() =>
            LoadWithPerformance().Composition;

        private static LoadedPlans LoadWithPerformance()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(
                WorldMapRoot, coverage.Bindings, coverage.CombinedCatalog).Plan;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance).Catalog;
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, gates, performance)
                .Catalog;
            var resources = new LuoyangResourceAgricultureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance).Catalog;
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    WorldMapRoot, coverage.CombinedCatalog, landmarks,
                    performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(WorldMapRoot,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, finalCivic, performance).Plan;
            return new LoadedPlans(performance,
                LuoyangWholeCityCompositionRules.CreatePlan(performance,
                    review));
        }

        private static string Identity(
            LuoyangWholeCityCompositionAnchor item) => string.Join("|",
            item.FacilityId, item.CellId64, item.AssetVariantId,
            item.DistrictId, item.SurfaceProfileId, item.ConnectionProfileId,
            item.VisualLocalEastMetres, item.VisualLocalNorthMetres,
            item.RotationDegrees, item.Scale);

        private sealed class LoadedPlans
        {
            public LoadedPlans(LuoyangBuildingPerformancePlan performance,
                LuoyangWholeCityCompositionPlan composition)
            {
                Performance = performance;
                Composition = composition;
            }

            public LuoyangBuildingPerformancePlan Performance { get; }
            public LuoyangWholeCityCompositionPlan Composition { get; }
        }
    }
}
