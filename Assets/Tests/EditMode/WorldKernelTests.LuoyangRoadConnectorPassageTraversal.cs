using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void LuoyangPassageWorldState_CommandRoundTripPreservesStateAndAudit()
        {
            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);

            Assert.That(system.EnsureInitialized(world, runtime), Is.True);
            var initialization = runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(initialization.ProcessedCommands, Is.EqualTo(1));
            Assert.That(initialization.CommittedTransactions, Is.EqualTo(1));
            Assert.That(initialization.PublishedEvents, Is.EqualTo(1));
            Assert.That(world.LuoyangPassageTraversals, Has.Count.EqualTo(20));
            Assert.That(world.PersistentWorldCommands.Single(item =>
                    item.Id == LuoyangPassageTraversalWorldContractIds
                        .InitializationCommandId).Status,
                Is.EqualTo(PersistentWorldCommandStatus.Completed));

            var gateId = plan.PassageFacilityIds.First();
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-persisted-close.v1",
                "person.core-test-issuer"), Is.True);
            var transition = runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(transition.ProcessedCommands, Is.EqualTo(1));
            var current = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == gateId);
            Assert.That(current.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId));
            Assert.That(current.Revision, Is.EqualTo(1));
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-repeat.v1",
                "person.core-test-issuer"), Is.False);

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(loaded.WorldEventOutbox, Has.Count.EqualTo(2));
            Assert.That(loaded.WorldEventOutbox.All(item =>
                item.DispatchStatus == WorldEventDispatchStatus.Dispatched),
                Is.True);
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(plan, loaded);
            Assert.That(session.PersistsAcrossSave, Is.True);
            Assert.That(session.ChangesSaveSchema, Is.True);
            Assert.That(session.IsWorldStateProjection, Is.True);
            Assert.That(session.Get(gateId).CanTraverse, Is.False);
            Assert.Throws<System.InvalidOperationException>(() =>
                session.SetStatus(gateId,
                    LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                    1, "passage.reason.invalid-direct-write.v1"));
        }

        [Test]
        public void LuoyangPassageWorldState_V73MigrationIsEmptyAndInvalidVersionsReject()
        {
            var legacy = WorldState.Create(184);
            legacy.SchemaVersion = 73;
            legacy.LuoyangPassageTraversals = null;
            var migrated = WorldSnapshotMigrator.MigrateToCurrent(legacy);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangPassageTraversals, Is.Empty);
            Assert.That(migrated.PersistentWorldCommands, Is.Empty);
            Assert.That(migrated.WorldEventOutbox, Is.Empty);
            migrated.Validate();

            var zero = WorldState.Create(184);
            zero.SchemaVersion = 0;
            Assert.Throws<System.InvalidOperationException>(() =>
                WorldSnapshotMigrator.MigrateToCurrent(zero));
            var future = WorldState.Create(184);
            future.SchemaVersion = WorldState.CurrentSchemaVersion + 1;
            Assert.Throws<System.InvalidOperationException>(() =>
                WorldSnapshotMigrator.MigrateToCurrent(future));
        }

        [Test]
        public void LuoyangPassageWorldState_RejectsTamperAndConflictingBatch()
        {
            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);
            system.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            var gateId = plan.PassageFacilityIds.First();

            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.core-conflict-close.v1",
                "person.core-test-issuer"), Is.True);
            Assert.That(system.EnqueueTransition(world, runtime, gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                "passage.reason.core-conflict-damage.v1",
                "person.core-test-issuer"), Is.True);
            Assert.Throws<System.InvalidOperationException>(() =>
                runtime.ProcessDue(world));
            var unchanged = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == gateId);
            Assert.That(unchanged.Revision, Is.Zero);
            Assert.That(unchanged.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            Assert.That(world.WorldCommandBatchResults.Last().Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            world.Validate();

            unchanged.LastReasonId = "passage.reason.tampered.v1";
            Assert.Throws<System.InvalidOperationException>(() =>
                world.Validate());
        }

        [Test]
        public void LuoyangPassageOperations_GuardDamageRepairAndReopenAreAuditable()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;

            Assert.That(system.EnqueueGuardAssignment(world, runtime,
                fixture.FacilityId, fixture.GuardArmyId,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var control = world.LuoyangPassageOperationalControls.Single();
            Assert.That(control.GuardPersonIds,
                Is.EqualTo(new[] { fixture.GuardCommanderPersonId }));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(10_000));
            Assert.Throws<System.InvalidOperationException>(() =>
                system.EnqueueTransition(world, runtime, fixture.FacilityId,
                    LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                    "passage.reason.presentation-direct-close.v1",
                    "person.presentation.map"));

            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.guard-close.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.guard-open.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.That(system.EnqueueBattleDamage(world, runtime,
                fixture.FacilityId, fixture.BattleId, 4_000,
                "passage.reason.test-battle-damage.v1",
                fixture.AttackerCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var passage = world.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == fixture.FacilityId);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(6_000));
            Assert.That(world.Facilities.Single(item =>
                item.Id == fixture.FacilityId).ConditionBasisPoints,
                Is.EqualTo(6_000));
            Assert.That(world.LuoyangPassageDamageRecords, Has.Count.EqualTo(1));

            Assert.That(system.EnqueueStartRepair(world, runtime,
                fixture.FacilityId, fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId, fixture.InventoryContainerId),
                Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var repair = world.LuoyangPassageRepairOrders.Single();
            var project = world.FacilityConstructionProjects.Single(item =>
                item.Id == repair.FacilityConstructionProjectId);
            Assert.That(project.Kind,
                Is.EqualTo(FacilityConstructionProjectKind.Repair));
            Assert.That(project.Materials.Sum(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.TimberMaterialProductId
                        ? item.ReservedQuantity : 0),
                Is.EqualTo(LuoyangPassageOperationsContractIds
                    .GateRequiredTimberUnits));
            Assert.That(project.Materials.Sum(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.IronMaterialProductId
                        ? item.ReservedQuantity : 0),
                Is.EqualTo(LuoyangPassageOperationsContractIds
                    .GateRequiredIronUnits));
            Assert.That(world.InventoryTransactions.Count(item =>
                    item.SourceFacilityConstructionProjectId == project.Id &&
                    item.Type == InventoryTransactionType
                        .FacilityConstructionMaterialReserved),
                Is.EqualTo(1));

            system.ContributeRepairLabor(world, repair.Id,
                fixture.GuardCommanderPersonId, 480);
            world.AbsoluteDay = 1;
            system.ContributeRepairLabor(world, repair.Id,
                fixture.GuardCommanderPersonId, 480);
            world.AbsoluteDay = project.EarliestCompletionDay;
            Assert.That(system.EnqueueCompleteRepair(world, runtime, repair.Id,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.That(repair.Status,
                Is.EqualTo(LuoyangPassageRepairStatus.Completed));
            Assert.That(project.Status,
                Is.EqualTo(FacilityConstructionStatus.Completed));
            Assert.That(control.CurrentConditionBasisPoints, Is.EqualTo(10_000));
            Assert.That(control.ActiveRepairOrderId, Is.Empty);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId));
            Assert.That(world.ProductBatches.Single(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.TimberMaterialProductId).Quantity,
                Is.EqualTo(12));
            Assert.That(world.ProductBatches.Single(item =>
                    item.ProductDefinitionId ==
                        CoreProductionContent.IronMaterialProductId).Quantity,
                Is.EqualTo(3));
            Assert.That(world.InventoryTransactions.Count(item =>
                    item.SourceFacilityConstructionProjectId == project.Id &&
                    item.Type == InventoryTransactionType
                        .FacilityConstructionMaterialConsumed),
                Is.EqualTo(1));

            Assert.That(system.EnqueueTransition(world, runtime,
                fixture.FacilityId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId,
                "passage.reason.guard-reopen-after-repair.v1",
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            Assert.That(passage.TraversalStatusId, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId));
            world.Validate();

            var json = WorldSnapshotSerializer.Serialize(world);
            var loaded = WorldSnapshotSerializer.Deserialize(json);
            Assert.That(WorldSnapshotSerializer.Serialize(loaded),
                Is.EqualTo(json));
            Assert.That(loaded.LuoyangPassageOperationalControls,
                Has.Count.EqualTo(1));
            Assert.That(loaded.LuoyangPassageDamageRecords,
                Has.Count.EqualTo(1));
            Assert.That(loaded.LuoyangPassageRepairOrders,
                Has.Count.EqualTo(1));
            loaded.LuoyangPassageRepairOrders.Single().CompletionEventId =
                "luoyang.passage.event.tampered";
            Assert.Throws<System.InvalidOperationException>(() =>
                loaded.Validate());
        }

        [Test]
        public void LuoyangPassageOperations_RejectsFalseAuthorityAndMaterialShortage()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;
            Assert.That(system.EnqueueGuardAssignment(world, runtime,
                fixture.FacilityId, fixture.GuardArmyId,
                fixture.GuardCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            Assert.Throws<System.InvalidOperationException>(() =>
                system.EnqueueBattleDamage(world, runtime,
                    fixture.FacilityId, fixture.BattleId, 1_000,
                    "passage.reason.false-attacker.v1",
                    fixture.GuardCommanderPersonId));
            Assert.That(system.EnqueueBattleDamage(world, runtime,
                fixture.FacilityId, fixture.BattleId, 4_000,
                "passage.reason.real-attacker.v1",
                fixture.AttackerCommanderPersonId), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);

            var timber = world.ProductBatches.Single(item =>
                item.ProductDefinitionId ==
                    CoreProductionContent.TimberMaterialProductId);
            var opening = world.InventoryTransactions.Single(item =>
                item.Id == timber.SourceTransactionId).Lines.Single();
            timber.Quantity = 7;
            opening.QuantityDelta = 7;
            world.Validate();
            Assert.That(system.EnqueueStartRepair(world, runtime,
                fixture.FacilityId, fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId, fixture.InventoryContainerId),
                Is.True);
            Assert.Throws<System.InvalidOperationException>(() =>
                runtime.ProcessDue(world));
            Assert.That(world.LuoyangPassageRepairOrders, Is.Empty);
            Assert.That(world.FacilityConstructionProjects, Is.Empty);
            Assert.That(world.ProductBatches.All(item =>
                item.ReservedQuantity == 0), Is.True);
            Assert.That(world.WorldCommandBatchResults.Last().Outcome,
                Is.EqualTo(WorldCommandBatchOutcome.Rejected));
            world.Validate();
        }

        [Test]
        public void LuoyangPassageOperations_V74MigrationIsEmptyAndNormalizesInventoryProvenance()
        {
            var legacy = WorldState.Create(184);
            legacy.SchemaVersion = 74;
            legacy.LuoyangPassageOperationalControls = null;
            legacy.LuoyangPassageDamageRecords = null;
            legacy.LuoyangPassageRepairOrders = null;
            legacy.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = "inventory_transaction.v74.provenance-probe",
                SourceFacilityConstructionProjectId = null
            });

            var migrated = WorldSnapshotMigrator.MigrateToCurrent(legacy);
            Assert.That(migrated.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(migrated.LuoyangPassageOperationalControls, Is.Empty);
            Assert.That(migrated.LuoyangPassageDamageRecords, Is.Empty);
            Assert.That(migrated.LuoyangPassageRepairOrders, Is.Empty);
            Assert.That(migrated.InventoryTransactions.Single()
                .SourceFacilityConstructionProjectId, Is.Empty);
            migrated.InventoryTransactions.Clear();
            migrated.Validate();
        }

        [Test]
        public void LuoyangRoadConnectorPassageTraversal_AuthorsAndBlocksPassages()
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
            var civic = new LuoyangFinalCivicRitualMedicalProductionKitSource(
                root, coverage.CombinedCatalog, landmarks, performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, civic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            var plan = LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                interaction);
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);

            Assert.That(plan.ModeledConnectors.Count, Is.EqualTo(28));
            Assert.That(plan.NavigationEdges.Count, Is.EqualTo(402));
            Assert.That(session.Records.Count, Is.EqualTo(20));
            var gate = plan.PassageFacilityIds.First();
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(plan, session, gate, gate).Count,
                Is.EqualTo(1));
            session.SetStatus(gate,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                1, "passage.reason.core-test.v1");
            Assert.That(LuoyangRoadConnectorPassageTraversalRules
                .FindFacilityPath(plan, session, gate, gate), Is.Empty);
        }

        [Test]
        public void LuoyangPassagePedestrianPresentation_ProjectsDeterministicBlockingWithoutPersistence()
        {
            var plan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);
            var gateId = plan.PassageFacilityIds.First(item =>
                !string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var bridgeId = plan.PassageFacilityIds.First(item =>
                string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));

            var opening = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            Assert.That(opening.States.Count, Is.EqualTo(20));
            Assert.That(opening.ChangesSaveSchema, Is.False);
            Assert.That(opening.PersistsAcrossSave, Is.False);
            Assert.That(opening.IsWorldStateProjection, Is.False);
            Assert.That(opening.States.All(item =>
                !item.BlocksPedestrianTraversal &&
                item.VisualStateId ==
                    LuoyangPassagePedestrianPresentationIds.OpenVisualStateId),
                Is.True);

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                1, "passage.reason.pedestrian-closed.v1");
            session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId,
                1, "passage.reason.pedestrian-damaged.v1");
            var first = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            var second = LuoyangPassagePedestrianPresentationRules.CreatePlan(
                plan, session);
            Assert.That(first.Get(gateId).BlocksPedestrianTraversal, Is.True);
            Assert.That(first.Get(gateId).VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.ClosedVisualStateId));
            Assert.That(first.Get(bridgeId).BlocksPedestrianTraversal,
                Is.False);
            Assert.That(first.Get(bridgeId).ConditionBasisPoints,
                Is.EqualTo(5_000));
            Assert.That(second.States.Select(item => string.Join("|",
                    item.FacilityId, item.TraversalStatusId,
                    item.VisualStateId, item.BlocksPedestrianTraversal,
                    item.ConditionBasisPoints, item.PassageRevision)).ToArray(),
                Is.EqualTo(first.States.Select(item => string.Join("|",
                    item.FacilityId, item.TraversalStatusId,
                    item.VisualStateId, item.BlocksPedestrianTraversal,
                    item.ConditionBasisPoints, item.PassageRevision)).ToArray()));

            session.SetStatus(bridgeId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                2, "passage.reason.pedestrian-destroyed.v1");
            var destroyed = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(plan, session).Get(bridgeId);
            Assert.That(destroyed.BlocksPedestrianTraversal, Is.True);
            Assert.That(destroyed.ConditionBasisPoints, Is.Zero);
            Assert.That(destroyed.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds.DestroyedVisualStateId));
        }

        [Test]
        public void LuoyangClickToWalkPedestrian_UsesStableWidthsCostsAndDynamicPassageRules()
        {
            var plan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateInitialSession(plan);
            var gateId = plan.PassageFacilityIds.First(item =>
                !string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var bridgeId = plan.PassageFacilityIds.First(item =>
                string.Equals(plan.NavigationNodesByFacilityId[item]
                        .FacilityDefinitionId, "facility.public.bridge",
                    System.StringComparison.Ordinal));
            var gateNode = plan.NavigationNodesByFacilityId[gateId];
            var bridgeNode = plan.NavigationNodesByFacilityId[bridgeId];
            var nodeById = plan.NavigationNodes.ToDictionary(item =>
                item.NodeId, System.StringComparer.Ordinal);
            var gateRoadIds = plan.NavigationEdges.Where(item =>
                    item.EdgeProfileId ==
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId &&
                    (item.FromNodeId == gateNode.NodeId ||
                     item.ToNodeId == gateNode.NodeId))
                .Select(item => item.FromNodeId == gateNode.NodeId
                    ? nodeById[item.ToNodeId].FacilityId
                    : nodeById[item.FromNodeId].FacilityId)
                .OrderBy(item => item, System.StringComparer.Ordinal).ToArray();
            var gateRoadId = gateRoadIds[0];
            var bridgeRoadId = plan.NavigationEdges.Where(item =>
                    item.EdgeProfileId ==
                        LuoyangRoadConnectorPassageTraversalIds
                            .PassageApproachEdgeProfileId &&
                    (item.FromNodeId == bridgeNode.NodeId ||
                     item.ToNodeId == bridgeNode.NodeId))
                .Select(item => item.FromNodeId == bridgeNode.NodeId
                    ? nodeById[item.ToNodeId].FacilityId
                    : nodeById[item.FromNodeId].FacilityId).First();

            const string actorId = "person.luoyang.walking-core-test";
            var open = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            var repeated = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(open.ContractId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.ContractId));
            Assert.That(open.StatusId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.StatusId));
            Assert.That(open.CanWalk, Is.True);
            Assert.That(open.CreatesPermanentPerson, Is.False);
            Assert.That(open.ChangesSaveSchema, Is.False);
            Assert.That(open.PersistsAcrossSave, Is.False);
            Assert.That(open.FacilityIds, Is.EqualTo(repeated.FacilityIds));
            Assert.That(open.Segments.Select(item => string.Join("|",
                    item.EdgeId, item.WidthProfileId, item.WidthMetres,
                    item.LateralOffsetMetres)).ToArray(),
                Is.EqualTo(repeated.Segments.Select(item => string.Join("|",
                    item.EdgeId, item.WidthProfileId, item.WidthMetres,
                    item.LateralOffsetMetres)).ToArray()));
            Assert.That(open.Segments.Single().WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.GateWidthProfileId));
            Assert.That(open.Segments.Single().WidthMetres, Is.EqualTo(12f));
            Assert.That(open.Segments.Single().UsesPassage, Is.True);
            var crossing = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadIds[0], gateRoadIds[1]);
            Assert.That(crossing.CanWalk, Is.True);
            Assert.That(crossing.FacilityIds, Does.Contain(gateId));

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.DamagedStatusId, 1,
                "passage.reason.walking-core-damaged.v1");
            var damaged = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(damaged.CanWalk, Is.True);
            Assert.That(damaged.UsesDamagedPassage, Is.True);
            Assert.That(damaged.WeightedDistanceMetres,
                Is.GreaterThan(open.WeightedDistanceMetres));
            Assert.That(damaged.EstimatedDurationSeconds,
                Is.GreaterThan(open.EstimatedDurationSeconds));

            session.SetStatus(gateId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId, 2,
                "passage.reason.walking-core-closed.v1");
            var blocked = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, gateRoadId, gateId);
            Assert.That(blocked.CanWalk, Is.False);
            Assert.That(blocked.FailureReasonId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.BlockedPassageReasonId));

            var bridge = LuoyangClickToWalkPedestrianRules.CreatePlan(plan,
                session, actorId, bridgeRoadId, bridgeId);
            Assert.That(bridge.CanWalk, Is.True);
            Assert.That(bridge.Segments.Single().WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds.BridgeWidthProfileId));
            Assert.That(bridge.Segments.Single().WidthMetres, Is.EqualTo(8f));

            var connectorWalk = plan.ModeledConnectors.Select(connector =>
                LuoyangClickToWalkPedestrianRules.CreatePlan(plan, session,
                    actorId, nodeById[connector.FromNodeId].FacilityId,
                    nodeById[connector.ToNodeId].FacilityId)).First(item =>
                item.CanWalk && item.UsesModeledConnector);
            Assert.That(connectorWalk.Segments.First(item =>
                    item.UsesModeledConnector).WidthProfileId, Is.EqualTo(
                LuoyangClickToWalkPedestrianIds
                    .ModeledConnectorWidthProfileId));
            Assert.That(connectorWalk.Segments.First(item =>
                    item.UsesModeledConnector).WidthMetres, Is.EqualTo(12f));
        }

        [Test]
        public void LuoyangPassagePedestrianPresentation_UsesV75IntegrityAndActiveRepairReadOnly()
        {
            var fixture = CreateLuoyangPassageOperationsFixture();
            var world = fixture.World;
            var runtime = fixture.Runtime;
            var system = fixture.System;
            system.EnqueueGuardAssignment(world, runtime, fixture.FacilityId,
                fixture.GuardArmyId, fixture.GuardCommanderPersonId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            system.EnqueueBattleDamage(world, runtime, fixture.FacilityId,
                fixture.BattleId, 4_000,
                "passage.reason.pedestrian-projection-damage.v1",
                fixture.AttackerCommanderPersonId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            system.EnqueueStartRepair(world, runtime, fixture.FacilityId,
                fixture.GuardCommanderPersonId,
                fixture.GuardCommanderPersonId,
                fixture.InventoryContainerId);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var serializedBefore = WorldSnapshotSerializer.Serialize(world);
            var passagePlan = BuildLuoyangPassagePlan();
            var session = LuoyangRoadConnectorPassageTraversalRules
                .CreateSessionFromWorldState(passagePlan, world);

            var presentation = LuoyangPassagePedestrianPresentationRules
                .CreatePlan(passagePlan, session, world);
            var state = presentation.Get(fixture.FacilityId);
            Assert.That(presentation.IsWorldStateProjection, Is.True);
            Assert.That(state.ConditionBasisPoints, Is.EqualTo(6_000));
            Assert.That(state.IntegrityRevision, Is.EqualTo(1));
            Assert.That(state.IsRepairing, Is.True);
            Assert.That(state.BlocksPedestrianTraversal, Is.False);
            Assert.That(state.VisualStateId, Is.EqualTo(
                LuoyangPassagePedestrianPresentationIds
                    .RepairingVisualStateId));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(serializedBefore));
        }

        private static LuoyangRoadTraversalRefinementPlan
            BuildLuoyangPassagePlan()
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
            var civic = new LuoyangFinalCivicRitualMedicalProductionKitSource(
                root, coverage.CombinedCatalog, landmarks, performance).Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, civic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            return LuoyangRoadConnectorPassageTraversalRules.CreatePlan(
                interaction);
        }

        private static LuoyangPassageOperationsFixture
            CreateLuoyangPassageOperationsFixture()
        {
            const string locationId = "location.luoyang.passage_test";
            const string guardOrganizationId =
                "organization.luoyang.passage_guard";
            const string attackerOrganizationId =
                "organization.luoyang.passage_attacker";
            const string guardCommanderPersonId =
                "person.luoyang.passage_guard_commander";
            const string attackerCommanderPersonId =
                "person.luoyang.passage_attacker_commander";
            const string guardArmyId = "army.luoyang.passage_guard";
            const string attackerArmyId = "army.luoyang.passage_attacker";
            const string inventoryContainerId =
                "inventory_container.luoyang.passage_repair";
            const string battleId = "battle.luoyang.passage_test";
            const ulong cellId64 = 900001;

            var plan = BuildLuoyangPassagePlan();
            var world = WorldState.Create(184);
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangPassageWorldCommandSystem(plan);
            system.RegisterHandlers(runtime);
            system.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var passage = world.LuoyangPassageTraversals.First(item =>
                !string.Equals(item.FacilityDefinitionId,
                    "facility.public.bridge", System.StringComparison.Ordinal));

            world.Locations.Add(new LocationState
            {
                Id = locationId,
                DisplayName = "洛阳关隘测试区",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Garrison |
                    LocationFeature.Fortification,
                Population = 2
            });
            world.People.Add(new PersonState
            {
                Id = guardCommanderPersonId,
                DisplayName = "守关校尉",
                LocationId = locationId,
                BirthLocationId = locationId
            });
            world.People.Add(new PersonState
            {
                Id = attackerCommanderPersonId,
                DisplayName = "攻方主将",
                LocationId = locationId,
                BirthLocationId = locationId
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = guardOrganizationId,
                DisplayName = "洛阳守关组织",
                Type = OrganizationType.Military,
                HeadquartersLocationId = locationId,
                LeaderPersonId = guardCommanderPersonId,
                Treasury = 1_000
            });
            world.Organizations.Add(new OrganizationState
            {
                Id = attackerOrganizationId,
                DisplayName = "洛阳攻方组织",
                Type = OrganizationType.Military,
                HeadquartersLocationId = locationId,
                LeaderPersonId = attackerCommanderPersonId,
                Treasury = 1_000
            });
            world.Armies.Add(new ArmyState
            {
                Id = guardArmyId,
                DisplayName = "洛阳守关军",
                OrganizationId = guardOrganizationId,
                CommanderPersonId = guardCommanderPersonId,
                LocationId = locationId,
                Troops = 1,
                MaximumTroops = 1,
                Provisions = 100
            });
            world.Armies.Add(new ArmyState
            {
                Id = attackerArmyId,
                DisplayName = "洛阳攻方军",
                OrganizationId = attackerOrganizationId,
                CommanderPersonId = attackerCommanderPersonId,
                LocationId = locationId,
                Troops = 1,
                MaximumTroops = 1,
                Provisions = 100
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = "formation.luoyang.passage_guard.root",
                ArmyId = guardArmyId,
                ParentFormationId = string.Empty,
                DisplayName = "洛阳守关军本阵",
                Kind = MilitaryFormationKind.Army,
                CommanderPersonId = guardCommanderPersonId,
                AuthorizedStrength = 1,
                DisplayOrder = 0
            });
            world.MilitaryFormations.Add(new MilitaryFormationState
            {
                Id = "formation.luoyang.passage_attacker.root",
                ArmyId = attackerArmyId,
                ParentFormationId = string.Empty,
                DisplayName = "洛阳攻方军本阵",
                Kind = MilitaryFormationKind.Army,
                CommanderPersonId = attackerCommanderPersonId,
                AuthorizedStrength = 1,
                DisplayOrder = 0
            });
            world.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = "military_service.luoyang.passage_guard_commander",
                PersonId = guardCommanderPersonId,
                ArmyId = guardArmyId,
                FormationId = "formation.luoyang.passage_guard.root",
                Role = MilitaryServiceRole.Commander,
                Rank = 10,
                Status = MilitaryServiceStatus.Active,
                EnlistedDay = 0,
                LastStatusChangeDay = 0
            });
            world.MilitaryServices.Add(new MilitaryServiceState
            {
                Id = "military_service.luoyang.passage_attacker_commander",
                PersonId = attackerCommanderPersonId,
                ArmyId = attackerArmyId,
                FormationId = "formation.luoyang.passage_attacker.root",
                Role = MilitaryServiceRole.Commander,
                Rank = 10,
                Status = MilitaryServiceStatus.Active,
                EnlistedDay = 0,
                LastStatusChangeDay = 0
            });
            world.MilitaryServiceInitialized = true;
            new PropertyConstructionSystem().GrantOpeningProperty(world,
                cellId64, locationId, guardOrganizationId,
                guardOrganizationId);
            world.FacilityDefinitions.Add(new FacilityDefinitionState
            {
                Id = passage.FacilityDefinitionId,
                DisplayName = "洛阳关隘测试定义",
                CategoryId = "facility.category.fortification"
            });
            world.Facilities.Add(new FacilityState
            {
                Id = passage.FacilityId,
                DisplayName = "洛阳关隘测试设施",
                DefinitionId = passage.FacilityDefinitionId,
                CellId64 = cellId64,
                OwnerId = guardOrganizationId,
                ControllerId = guardOrganizationId,
                AdministrativeControllerId = guardOrganizationId,
                SettlementId = locationId,
                HistoricalConfidence =
                    HistoricalConfidenceLevel.GameplayReconstruction,
                SpatialPrecision = HistoricalSpatialPrecision.Confirmed,
                SourceNote = "Deterministic passage operations fixture."
            });
            world.InventoryContainers.Add(new InventoryContainerState
            {
                Id = inventoryContainerId,
                KindId = "inventory_container.military_construction_store",
                OwnerOrganizationId = guardOrganizationId,
                LocationId = locationId,
                CapacityWeight = 1_000
            });
            AddPassageRepairOpeningBatch(world,
                "product_batch.luoyang.passage_repair.timber",
                CoreProductionContent.TimberMaterialProductId, 20,
                guardOrganizationId, inventoryContainerId, locationId,
                guardCommanderPersonId);
            AddPassageRepairOpeningBatch(world,
                "product_batch.luoyang.passage_repair.iron",
                CoreProductionContent.IronMaterialProductId, 5,
                guardOrganizationId, inventoryContainerId, locationId,
                guardCommanderPersonId);
            world.Battles.Add(new BattleRecordState
            {
                Id = battleId,
                Day = 0,
                LocationId = locationId,
                AttackerArmyId = attackerArmyId,
                DefenderArmyId = guardArmyId,
                AttackerInitialTroops = 1,
                DefenderInitialTroops = 1,
                AttackerCasualties = 0,
                DefenderCasualties = 0,
                AttackerWounded = 0,
                DefenderWounded = 0,
                AttackerEquipmentReadinessBasisPoints = 10_000,
                DefenderEquipmentReadinessBasisPoints = 10_000,
                Result = BattleResultType.Stalemate,
                WinnerArmyId = string.Empty,
                Summary = "关隘战损权威测试战斗。"
            });
            world.Validate();
            return new LuoyangPassageOperationsFixture(world, runtime, system,
                passage.FacilityId, guardArmyId, guardCommanderPersonId,
                attackerCommanderPersonId, inventoryContainerId, battleId);
        }

        private static void AddPassageRepairOpeningBatch(
            WorldState world,
            string batchId,
            string productDefinitionId,
            long quantity,
            string ownerOrganizationId,
            string inventoryContainerId,
            string locationId,
            string actorPersonId)
        {
            var product = ProductionContentRegistry.CreateCore().GetProduct(
                productDefinitionId);
            var transactionId = "inventory_transaction." + batchId +
                ".opening";
            var batch = new ProductBatchState
            {
                Id = batchId,
                ProductDefinitionId = product.Id,
                OwnerOrganizationId = ownerOrganizationId,
                InventoryContainerId = inventoryContainerId,
                OriginLocationId = locationId,
                SourceTransactionId = transactionId,
                UnitId = product.UnitId,
                UnitWeight = product.BaseWeight,
                ProducedDay = world.AbsoluteDay,
                Quantity = quantity,
                QualityBasisPoints = 8_500,
                FreshnessBasisPoints = 9_500,
                QualityDimensions = ProductQualityRules.CreateUniform(
                    product, 8_500)
            };
            world.ProductBatches.Add(batch);
            world.InventoryTransactions.Add(new InventoryTransactionState
            {
                Id = transactionId,
                Day = world.AbsoluteDay,
                Type = InventoryTransactionType.OpeningBalance,
                ActorPersonId = actorPersonId,
                Summary = "Passage repair test opening balance.",
                Lines =
                {
                    new InventoryTransactionLineState
                    {
                        BatchId = batch.Id,
                        ProductDefinitionId = batch.ProductDefinitionId,
                        OwnerOrganizationId = batch.OwnerOrganizationId,
                        InventoryContainerId = batch.InventoryContainerId,
                        UnitId = batch.UnitId,
                        QuantityDelta = quantity
                    }
                }
            });
        }

        private sealed class LuoyangPassageOperationsFixture
        {
            public LuoyangPassageOperationsFixture(
                WorldState world,
                WorldCommandRuntime runtime,
                LuoyangPassageWorldCommandSystem system,
                string facilityId,
                string guardArmyId,
                string guardCommanderPersonId,
                string attackerCommanderPersonId,
                string inventoryContainerId,
                string battleId)
            {
                World = world;
                Runtime = runtime;
                System = system;
                FacilityId = facilityId;
                GuardArmyId = guardArmyId;
                GuardCommanderPersonId = guardCommanderPersonId;
                AttackerCommanderPersonId = attackerCommanderPersonId;
                InventoryContainerId = inventoryContainerId;
                BattleId = battleId;
            }

            public WorldState World { get; }
            public WorldCommandRuntime Runtime { get; }
            public LuoyangPassageWorldCommandSystem System { get; }
            public string FacilityId { get; }
            public string GuardArmyId { get; }
            public string GuardCommanderPersonId { get; }
            public string AttackerCommanderPersonId { get; }
            public string InventoryContainerId { get; }
            public string BattleId { get; }
        }
    }
}
