using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class LuoyangFacilityInteractionNavigationV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Plan_CoversEveryFacilityWithASeparateSelectionTrigger()
        {
            var loaded = Load();
            var plan = loaded.Interaction;

            Assert.That(plan.ContractId, Is.EqualTo(
                LuoyangFacilityInteractionNavigationIds.ContractId));
            Assert.That(plan.StatusId, Is.EqualTo(
                LuoyangFacilityInteractionNavigationIds.StatusId));
            Assert.That(plan.SelectionProxies.Count, Is.EqualTo(2084));
            Assert.That(plan.SelectionProxiesByFacilityId.Count,
                Is.EqualTo(2084));
            Assert.That(plan.SelectionProxies.Select(item => item.ProxyId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2084));
            Assert.That(plan.SelectionProxies.All(item => item.IsSelectable &&
                item.IsTrigger && item.HalfExtentEastMetres > 0f &&
                item.HalfExtentEastMetres < 1000f &&
                item.HalfExtentNorthMetres > 0f &&
                item.HalfExtentNorthMetres < 1000f), Is.True);
            Assert.That(plan.CreatesSimulationSubCells, Is.False);
            Assert.That(plan.ChangesSaveSchema, Is.False);
            Assert.That(LuoyangWholeCityCompositionRules
                .SelectDensestResidentAnchors(loaded.Composition,
                    loaded.Performance).Count, Is.EqualTo(549));
        }

        [Test]
        public void RoadGraph_PreservesAuthoredAdjacencyAndLabelsGapConnectors()
        {
            var plan = Load().Interaction;

            Assert.That(plan.NavigationNodes.Count, Is.EqualTo(379));
            Assert.That(plan.NavigationNodes.Count(item => string.Equals(
                item.FacilityDefinitionId, "facility.public.road",
                StringComparison.Ordinal)), Is.EqualTo(359));
            Assert.That(plan.NavigationEdges.Count, Is.EqualTo(382));
            Assert.That(plan.NavigationEdges.Count(item => string.Equals(
                item.EdgeProfileId,
                LuoyangFacilityInteractionNavigationIds.StrictRoadEdgeProfileId,
                StringComparison.Ordinal)), Is.EqualTo(334));
            Assert.That(plan.RoadComponentCountBeforeConnectors, Is.EqualTo(29));
            Assert.That(plan.NavigationEdges.Count(item => item.Provisional),
                Is.EqualTo(28));
            Assert.That(plan.NavigationEdges.Count(item => string.Equals(
                item.EdgeProfileId,
                LuoyangFacilityInteractionNavigationIds
                    .PassageConnectorEdgeProfileId,
                StringComparison.Ordinal)), Is.EqualTo(20));
            Assert.That(plan.NavigationEdges.All(item =>
                plan.NavigationNodes.Any(node => node.NodeId == item.FromNodeId) &&
                plan.NavigationNodes.Any(node => node.NodeId == item.ToNodeId)),
                Is.True);
        }

        [Test]
        public void NavigationAndProxyGeneration_AreDeterministic()
        {
            var first = Load().Interaction;
            var second = Load().Interaction;
            Assert.That(second.SelectionProxies.Select(ProxyIdentity).ToArray(),
                Is.EqualTo(first.SelectionProxies.Select(ProxyIdentity).ToArray()));
            Assert.That(second.NavigationEdges.Select(EdgeIdentity).ToArray(),
                Is.EqualTo(first.NavigationEdges.Select(EdgeIdentity).ToArray()));

            var from = first.NavigationNodes.First().FacilityId;
            var to = first.NavigationNodes.Last().FacilityId;
            var firstPath = LuoyangFacilityInteractionNavigationRules
                .FindFacilityPath(first, from, to);
            var secondPath = LuoyangFacilityInteractionNavigationRules
                .FindFacilityPath(second, from, to);
            Assert.That(firstPath.Count, Is.GreaterThan(1));
            Assert.That(firstPath.First(), Is.EqualTo(from));
            Assert.That(firstPath.Last(), Is.EqualTo(to));
            Assert.That(secondPath, Is.EqualTo(firstPath));
            Assert.That(LuoyangFacilityInteractionNavigationRules
                .FindFacilityPath(first, "missing", to), Is.Empty);
        }

        private static LoadedPlans Load()
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
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            return new LoadedPlans(performance, composition,
                LuoyangFacilityInteractionNavigationRules.CreatePlan(
                    performance, composition));
        }

        private static string ProxyIdentity(LuoyangFacilitySelectionProxy item) =>
            string.Join("|", item.ProxyId, item.FacilityId, item.CellId64,
                item.CenterLocalEastMetres, item.CenterLocalNorthMetres,
                item.HalfExtentEastMetres, item.HalfExtentNorthMetres,
                item.HeightMetres, item.CollisionProfileId);

        private static string EdgeIdentity(LuoyangRoadNavigationEdge item) =>
            string.Join("|", item.EdgeId, item.FromNodeId, item.ToNodeId,
                item.EdgeProfileId, item.TraversalCostMetres,
                item.Provisional);

        private sealed class LoadedPlans
        {
            public LoadedPlans(LuoyangBuildingPerformancePlan performance,
                LuoyangWholeCityCompositionPlan composition,
                LuoyangFacilityInteractionNavigationPlan interaction)
            {
                Performance = performance;
                Composition = composition;
                Interaction = interaction;
            }

            public LuoyangBuildingPerformancePlan Performance { get; }
            public LuoyangWholeCityCompositionPlan Composition { get; }
            public LuoyangFacilityInteractionNavigationPlan Interaction { get; }
        }
    }
}
