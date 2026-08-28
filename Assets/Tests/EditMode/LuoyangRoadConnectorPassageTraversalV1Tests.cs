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
    public sealed class LuoyangRoadConnectorPassageTraversalV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void RefinedGraph_AuthorsEveryGapAndGivesEveryPassageTwoApproaches()
        {
            var loaded = Load();
            var plan = loaded.Refinement;

            Assert.That(plan.ContractId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.ContractId));
            Assert.That(plan.StatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.StatusId));
            Assert.That(plan.ModeledConnectors.Count, Is.EqualTo(28));
            Assert.That(plan.NavigationEdges.Count, Is.EqualTo(402));
            Assert.That(plan.NavigationEdges.Count(item => item.Provisional),
                Is.Zero);
            Assert.That(plan.NavigationEdges.Count(item => string.Equals(
                item.EdgeProfileId,
                LuoyangRoadConnectorPassageTraversalIds
                    .PassageApproachEdgeProfileId,
                StringComparison.Ordinal)), Is.EqualTo(40));
            Assert.That(plan.PassageFacilityIds.Count, Is.EqualTo(20));
            Assert.That(plan.ChangesSaveSchema, Is.False);
            Assert.That(plan.PersistsAcrossSave, Is.False);

            var nodeById = plan.NavigationNodes.ToDictionary(item => item.NodeId,
                StringComparer.Ordinal);
            var navigationCells = new HashSet<string>(plan.NavigationNodes
                .Select(item => item.GridRow + ":" + item.GridColumn),
                StringComparer.Ordinal);
            var occupiedNonNavigationCells = new HashSet<string>(
                plan.BasePlan.SelectionProxies.Where(item =>
                        !navigationCells.Contains(item.GridRow + ":" +
                                                  item.GridColumn))
                    .Select(item => item.GridRow + ":" + item.GridColumn),
                StringComparer.Ordinal);
            foreach (var connector in plan.ModeledConnectors)
            {
                Assert.That(connector.ClaimsHistoricalExactness, Is.False);
                Assert.That(connector.EvidenceClassId, Is.EqualTo(
                    LuoyangRoadConnectorPassageTraversalIds.EvidenceClassId));
                Assert.That(connector.SpatialPrecisionId, Is.EqualTo("cell"));
                Assert.That(connector.RouteAuthoringProfileId, Is.EqualTo(
                    LuoyangRoadConnectorPassageTraversalIds
                        .RouteAuthoringProfileId));
                Assert.That(connector.Waypoints.Count, Is.GreaterThan(1));
                Assert.That(connector.OccupiedNonNavigationCellCrossingCount,
                    Is.EqualTo(connector.Waypoints.Count(point =>
                        occupiedNonNavigationCells.Contains(point.GridRow +
                            ":" + point.GridColumn))));
                var from = nodeById[connector.FromNodeId];
                var to = nodeById[connector.ToNodeId];
                Assert.That(connector.Waypoints.First().GridRow,
                    Is.EqualTo(from.GridRow));
                Assert.That(connector.Waypoints.First().GridColumn,
                    Is.EqualTo(from.GridColumn));
                Assert.That(connector.Waypoints.Last().GridRow,
                    Is.EqualTo(to.GridRow));
                Assert.That(connector.Waypoints.Last().GridColumn,
                    Is.EqualTo(to.GridColumn));
                for (var index = 1; index < connector.Waypoints.Count; index++)
                {
                    var previous = connector.Waypoints[index - 1];
                    var current = connector.Waypoints[index];
                    Assert.That(Math.Abs(previous.GridRow - current.GridRow) +
                        Math.Abs(previous.GridColumn - current.GridColumn),
                        Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void PassageSession_UsesStableStatusesAndMonotonicTransitions()
        {
            var plan = Load().Refinement;
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);
            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;

            Assert.That(session.Records.Count, Is.EqualTo(20));
            Assert.That(session.Records.All(item => item.CanTraverse &&
                item.Revision == 0), Is.True);
            Assert.That(session.ChangesSaveSchema, Is.False);
            Assert.That(session.PersistsAcrossSave, Is.False);
            Assert.That(session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                12, "passage.reason.test-close.v1"), Is.True);
            Assert.That(session.Get(gateId).CanTraverse, Is.False);
            Assert.That(session.Get(gateId).Revision, Is.EqualTo(1));
            Assert.That(session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                12, "passage.reason.test-close-repeat.v1"), Is.False);
            Assert.Throws<InvalidOperationException>(() => session.SetStatus(
                gateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                11, "passage.reason.invalid-backwards.v1"));
            Assert.Throws<ArgumentException>(() => session.SetStatus(gateId,
                "passage.traversal.unknown", 13,
                "passage.reason.invalid-status.v1"));
            Assert.That(session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                13, "passage.reason.test-damage.v1"), Is.True);
            Assert.That(session.Get(gateId).CanTraverse, Is.True);
            Assert.That(session.Get(gateId).TraversalCostPermille,
                Is.EqualTo(1800));
            var bridgeId = session.Records.First(item => string.Equals(
                item.FacilityDefinitionId, "facility.public.bridge",
                StringComparison.Ordinal)).FacilityId;
            Assert.That(session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                13, "passage.reason.test-bridge-damage.v1"), Is.True);
            Assert.That(session.Get(bridgeId).CanTraverse, Is.True);
            Assert.That(session.Get(bridgeId).TraversalCostPermille,
                Is.EqualTo(1800));
            Assert.That(session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                14, "passage.reason.test-bridge-destroyed.v1"), Is.True);
            Assert.That(session.Get(bridgeId).CanTraverse, Is.False);
        }

        [Test]
        public void RefinedPath_IsDeterministicAndRespectsBlockedPassages()
        {
            var firstPlan = Load().Refinement;
            var secondPlan = Load().Refinement;
            Assert.That(secondPlan.ModeledConnectors.Select(Identity).ToArray(),
                Is.EqualTo(firstPlan.ModeledConnectors.Select(Identity)
                    .ToArray()));
            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            var gateNode = firstPlan.NavigationNodesByFacilityId[gateId];
            var approach = firstPlan.NavigationEdges.First(item =>
                string.Equals(item.EdgeProfileId,
                    LuoyangRoadConnectorPassageTraversalIds
                        .PassageApproachEdgeProfileId,
                    StringComparison.Ordinal) &&
                (item.FromNodeId == gateNode.NodeId ||
                 item.ToNodeId == gateNode.NodeId));
            var otherNodeId = approach.FromNodeId == gateNode.NodeId
                ? approach.ToNodeId : approach.FromNodeId;
            var roadId = firstPlan.NavigationNodes.First(item =>
                item.NodeId == otherNodeId).FacilityId;
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(firstPlan);
            var openPath = LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(firstPlan, session, gateId, roadId);
            Assert.That(openPath.Count, Is.GreaterThanOrEqualTo(2));
            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                30, "passage.reason.test-destroyed.v1");
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(firstPlan, session, gateId, roadId), Is.Empty);
            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                31, "passage.reason.test-reopened.v1");
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(firstPlan, session, gateId, roadId),
                Is.EqualTo(openPath));
        }

        [Test]
        public void PersistedPassageWorld_InitializesTransitionsAndRoundTripsCurrentSchema()
        {
            var plan = Load().Refinement;
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);

            Assert.That(system.EnsureInitialized(world, runtime), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(world.LuoyangPassageTraversals, Has.Count.EqualTo(20));
            Assert.That(world.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));

            const string gateId =
                LuoyangGateIdentityKitIds.NorthPalaceSouthGate;
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                "passage.reason.editmode-damage.v1",
                "person.editmode-issuer"), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(world));
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(plan, loaded);
            Assert.That(session.Records.Count, Is.EqualTo(20));
            Assert.That(session.PersistsAcrossSave, Is.True);
            Assert.That(session.Get(gateId).Revision, Is.EqualTo(1));
            Assert.That(session.Get(gateId).TraversalCostPermille,
                Is.EqualTo(1800));
            Assert.That(loaded.PersistentWorldCommands, Has.Count.EqualTo(2));
            Assert.That(loaded.WorldCommandBatchResults, Has.Count.EqualTo(2));
            Assert.That(loaded.WorldEventOutbox, Has.Count.EqualTo(2));
        }

        [Test]
        public void PersistedPassageWorld_V73MigrationDoesNotInventPassages()
        {
            var world = WorldState.Create(184);
            world.SchemaVersion = 73;
            world.LuoyangPassageTraversals = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangPassageTraversals, Is.Empty);
            migrated.Validate();
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
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            return new LoadedPlans(
                LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                    interaction));
        }

        private static string Identity(LuoyangModeledRoadConnector item) =>
            string.Join("|", item.ConnectorId, item.SourceProvisionalEdgeId,
                item.RefinedEdgeId, string.Join(",", item.Waypoints.Select(
                    point => point.Sequence + ":" + point.GridRow + ":" +
                             point.GridColumn)));

        private sealed class LoadedPlans
        {
            public LoadedPlans(LuoyangRoadTraversalRefinementPlan refinement)
            {
                Refinement = refinement;
            }

            public LuoyangRoadTraversalRefinementPlan Refinement { get; }
        }
    }
}
