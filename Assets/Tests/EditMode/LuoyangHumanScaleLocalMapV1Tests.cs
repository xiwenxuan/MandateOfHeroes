using System;
using System.Collections.Generic;
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
        private static readonly Lazy<LuoyangHumanScaleFixture>
            SharedLuoyangHumanScale =
                new Lazy<LuoyangHumanScaleFixture>(
                    CreateLuoyangHumanScaleFixture);

        [Test]
        public void LuoyangLocalMapGenerationTests_CoversFormalFacilitiesAndCells()
        {
            var fixture = SharedLuoyangHumanScale.Value;
            var plan = fixture.Plan;

            Assert.That(plan.CreatesSimulationSubCells, Is.False);
            Assert.That(plan.LocalSpaces.Count, Is.EqualTo(5_980));
            Assert.That(plan.LocalSpaces.Select(item => item.ParentCellId64)
                .Distinct().Count(), Is.EqualTo(5_980));
            Assert.That(plan.FacilityCapabilities.Count, Is.EqualTo(2_084));
            Assert.That(plan.Footprints.Count, Is.EqualTo(2_084));
            Assert.That(plan.FacilityCapabilities.Select(item =>
                    item.CapabilityId).Distinct(),
                Is.EquivalentTo(FacilitySpatialCapabilityIds.All));

            var expectedAccessCount = plan.FacilityCapabilities.Sum(item =>
                item.RequiresAccess
                    ? item.CapabilityId == FacilitySpatialCapabilityIds.Gate ||
                      item.CapabilityId ==
                      FacilitySpatialCapabilityIds.Bridge ? 2 : 1
                    : 0);
            Assert.That(plan.Entrances.Count,
                Is.EqualTo(expectedAccessCount));
        }

        [Test]
        public void LuoyangLocalMapDeterminismTests_SameInputsProduceSameHash()
        {
            var fixture = SharedLuoyangHumanScale.Value;
            var second = LuoyangHumanScaleLocalMapRules.CreatePlan(
                fixture.Performance, fixture.Composition,
                fixture.StrategicRoads);

            Assert.That(second.AssetHash, Is.EqualTo(fixture.Plan.AssetHash));
            Assert.That(second.Nodes.Select(item => item.Id),
                Is.EqualTo(fixture.Plan.Nodes.Select(item => item.Id)));
            Assert.That(second.Edges.Select(item => item.Id),
                Is.EqualTo(fixture.Plan.Edges.Select(item => item.Id)));
        }

        [Test]
        public void LuoyangLocalMap_SpatialReferenceTests_AllReferencesAndCoordinatesAreValid()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            foreach (var capability in plan.FacilityCapabilities)
            {
                Assert.That(plan.LocalSpacesById.ContainsKey(
                    capability.LocalSpaceId), Is.True, capability.FacilityId);
                Assert.That(plan.FootprintsByFacilityId.ContainsKey(
                    capability.FacilityId), Is.True, capability.FacilityId);
                if (capability.RequiresAccess)
                    Assert.That(plan.AccessPointsByFacilityId.ContainsKey(
                        capability.FacilityId), Is.True,
                        capability.FacilityId);
            }

            foreach (var point in plan.Edges.SelectMany(item => item.Geometry))
            {
                Assert.That(double.IsNaN(point.LocalEastMetres) ||
                            double.IsInfinity(point.LocalEastMetres), Is.False);
                Assert.That(double.IsNaN(point.LocalNorthMetres) ||
                            double.IsInfinity(point.LocalNorthMetres), Is.False);
                Assert.That(point.LocalEastMetres, Is.InRange(0d, 2_000d));
                Assert.That(point.LocalNorthMetres, Is.InRange(0d, 2_000d));
            }
        }

        [Test]
        public void LuoyangLocalMap_CellTransitionTests_PreserveContinuousConnectedPath()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            Assert.That(plan.Transitions, Is.Not.Empty);
            foreach (var transition in plan.Transitions)
            {
                Assert.That(plan.EdgesById.ContainsKey(transition.EdgeId),
                    Is.True, transition.Id);
                Assert.That(transition.ConnectedPathId,
                    Is.EqualTo(transition.EdgeId));
                Assert.That(transition.FromCellId64,
                    Is.Not.EqualTo(transition.ToCellId64));
                Assert.That(transition.SourceGlobalEastingMetres,
                    Is.EqualTo(transition.TargetGlobalEastingMetres)
                        .Within(0.001d));
                Assert.That(transition.SourceGlobalNorthingMetres,
                    Is.EqualTo(transition.TargetGlobalNorthingMetres)
                        .Within(0.001d));
                Assert.That(transition.TraversalConditionId, Is.Not.Empty);
            }
        }

        [Test]
        public void LuoyangLocalMap_ExistingTownSpatialCompatibilityTests_UsesOneProjectionContract()
        {
            var world = PrototypeWorldFactory.Create184World();
            var town = world.TownFacilities.Single(item =>
                item.Id == "town_facility.zhongshan.market");
            var townProjection = SettlementSpatialCompatibility.Project(town);

            var plan = SharedLuoyangHumanScale.Value.Plan;
            var capability = plan.FacilityCapabilities[0];
            var localProjection = SettlementSpatialCompatibility.Project(
                capability,
                plan.FootprintsByFacilityId[capability.FacilityId]);

            Assert.That(townProjection.FacilityId, Is.EqualTo(town.Id));
            Assert.That(townProjection.CoordinateSystemId,
                Is.EqualTo(SettlementSpatialCoordinateSystemIds
                    .NormalizedTownBasisPoints));
            Assert.That(townProjection.CoordinateExtentUnits,
                Is.EqualTo(10_000));
            Assert.That(localProjection.FacilityId,
                Is.EqualTo(capability.FacilityId));
            Assert.That(localProjection.ParentCellId64,
                Is.EqualTo(capability.CellId64));
            Assert.That(localProjection.CoordinateSystemId,
                Is.EqualTo(SettlementSpatialCoordinateSystemIds
                    .StrategicCellLocalCentimetres));
            Assert.That(localProjection.CoordinateExtentUnits,
                Is.EqualTo(200_000));
        }

        [Test]
        public void LuoyangLocalMap_FacilitySpatialCapabilityTests_AuditsAll2084()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            var accessRequired = plan.FacilityCapabilities.Count(item =>
                item.RequiresAccess);
            var blockingRequired = plan.FacilityCapabilities.Count(item =>
                item.HasBlockingGeometry);

            Assert.That(plan.FacilityCapabilities.Count, Is.EqualTo(2_084));
            Assert.That(plan.FacilityCapabilities.Count(item =>
                    item.CellId64 != 0 &&
                    plan.LocalSpacesByCellId.ContainsKey(item.CellId64)),
                Is.EqualTo(2_084));
            Assert.That(plan.FacilityCapabilities.Count(item =>
                    FacilitySpatialCapabilityIds.All.Contains(
                        item.CapabilityId)), Is.EqualTo(2_084));
            Assert.That(plan.FacilityCapabilities.Count(item =>
                    !item.RequiresAccess ||
                    plan.AccessPointsByFacilityId.TryGetValue(item.FacilityId,
                        out var access) && access.Count > 0),
                Is.EqualTo(2_084));
            Assert.That(accessRequired, Is.GreaterThan(0));
            Assert.That(blockingRequired, Is.GreaterThan(0));
            Assert.That(plan.Footprints.Count(item =>
                    item.HalfExtentEastMetres > 0d &&
                    item.HalfExtentNorthMetres > 0d), Is.EqualTo(2_084));
        }

        [Test]
        public void LuoyangLocalMap_DynamicWorldStateTests_ReadsFacilityRoadAndPassageFacts()
        {
            var fixture = CreateLuoyangHumanScaleMovementFixture();
            var routePlanner = new LuoyangHumanScaleLocalRoutePlanner(
                fixture.Map.Plan);
            Assert.That(routePlanner.TryFindRoute(fixture.World,
                    fixture.InitialFacilityId, fixture.TargetFacilityId,
                    out var route, out var failure), Is.True, failure);
            Assert.That(route, Is.Not.Null);

            var target = fixture.World.Facilities.Single(item => item.Id ==
                fixture.TargetFacilityId);
            target.LifecycleStatus = FacilityLifecycleStatus.Disabled;
            Assert.That(routePlanner.TryFindRoute(fixture.World,
                    fixture.InitialFacilityId, fixture.TargetFacilityId,
                    out _, out failure), Is.False);
            Assert.That(failure,
                Is.EqualTo("local-route.failure.facility-inaccessible.v1"));
            target.LifecycleStatus = FacilityLifecycleStatus.Operational;

            var passageEdge = fixture.Map.Plan.Edges.First(item =>
                item.TraversalConditionId ==
                LocalTraversalConditionIds.FormalPassageAvailable);
            Assert.That(LuoyangHumanScaleWorldTraversalRules
                .CanTraverseLocalEdge(fixture.World, passageEdge), Is.True);
            var passage = fixture.World.LuoyangPassageTraversals.Single(item =>
                item.FacilityId == passageEdge.PassageFacilityId);
            passage.TraversalStatusId =
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId;
            Assert.That(LuoyangHumanScaleWorldTraversalRules
                .CanTraverseLocalEdge(fixture.World, passageEdge), Is.False);
            passage.TraversalStatusId =
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId;
            Assert.That(LuoyangHumanScaleWorldTraversalRules
                .CanTraverseLocalEdge(fixture.World, passageEdge), Is.True);

            var roadEdge = fixture.Map.Plan.Edges.First(item =>
                item.TraversalConditionId ==
                LocalTraversalConditionIds.FormalRoadOpen);
            var road = fixture.World.LuoyangRoadOperationalSegments.Single(
                item => item.EdgeId == roadEdge.SourceStrategicEdgeId);
            Assert.That(LuoyangHumanScaleWorldTraversalRules
                .CanTraverseLocalEdge(fixture.World, roadEdge), Is.True);
            road.StatusId = LuoyangFormalPlayerMovementIds.BlockedRoadStatusId;
            Assert.That(LuoyangHumanScaleWorldTraversalRules
                .CanTraverseLocalEdge(fixture.World, roadEdge), Is.False);
        }

        [Test]
        public void LuoyangLocalMap_PersonLocalMovementTests_UsesFormalCommandAndSettlement()
        {
            var fixture = CreateLuoyangHumanScaleMovementFixture();
            var person = new PlayerSession(fixture.World).ControlledPerson;
            var initialTime = fixture.World.AbsoluteDay * 4L +
                fixture.World.Segment;
            var initialStamina = person.StaminaBasisPoints;
            var initialFood = person.Provisions;

            Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                    fixture.TargetFacilityId, out var movement,
                    out var localRoute, out var failure), Is.True, failure);
            Assert.That(movement.IsLocalHumanScale, Is.True);
            Assert.That(localRoute.Edges.Count, Is.GreaterThan(1));
            Assert.That(movement.RequestCommandId,
                Does.StartWith("luoyang.player-movement.request."));
            Assert.That(fixture.World.PersistentWorldCommands.Any(item =>
                    item.Id == movement.RequestCommandId && item.CommandTypeId ==
                    LuoyangFormalPlayerMovementIds.MoveCommandTypeId), Is.True);

            fixture.Service.Complete(fixture.World, movement.Id);
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Completed));
            Assert.That(person.CurrentFacilityId,
                Is.EqualTo(fixture.TargetFacilityId));
            Assert.That(person.LocationPrecisionId, Is.EqualTo(
                LuoyangHumanScaleLocalMapIds
                    .FacilityEntranceLocationTypeId));
            Assert.That(person.CurrentLocalSpaceId, Is.Not.Empty);
            Assert.That(person.StaminaBasisPoints,
                Is.EqualTo(initialStamina -
                    movement.ExpectedStaminaCostBasisPoints));
            Assert.That(person.Provisions,
                Is.EqualTo(initialFood - movement.ExpectedFoodCost));
            Assert.That(fixture.World.AbsoluteDay * 4L +
                fixture.World.Segment, Is.GreaterThan(initialTime));
            fixture.World.Validate();
        }

        [Test]
        public void LuoyangLocalMap_LocalLocationSaveLoadTests_RestoresMidMovementAnchor()
        {
            var fixture = CreateLuoyangHumanScaleMovementFixture();
            Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                    fixture.TargetFacilityId, out var movement,
                    out _, out var failure), Is.True, failure);
            fixture.Service.AdvanceNextSegment(fixture.World, movement.Id);
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Active));
            var before = new PlayerSession(fixture.World).ControlledPerson;
            var expectedAnchor = before.CurrentLocalAnchorId;
            var expectedEast = before.CurrentLocalEastCentimetres;
            var expectedNorth = before.CurrentLocalNorthCentimetres;

            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(fixture.World));
            var restored = new PlayerSession(loaded).ControlledPerson;
            Assert.That(restored.CurrentLocalAnchorId,
                Is.EqualTo(expectedAnchor));
            Assert.That(restored.CurrentLocalEastCentimetres,
                Is.EqualTo(expectedEast));
            Assert.That(restored.CurrentLocalNorthCentimetres,
                Is.EqualTo(expectedNorth));
            Assert.That(loaded.LuoyangFormalPlayerMovements.Single(item =>
                    item.Id == movement.Id).CurrentSegmentIndex,
                Is.EqualTo(movement.CurrentSegmentIndex));
        }

        [Test]
        public void LuoyangLocalMap_V76MigrationTests_PreservesStrategicLocation()
        {
            var world = WorldState.Create(184);
            world.People.Add(new PersonState
            {
                Id = "person.local-map.v76",
                DisplayName = "V76 人物",
                LocationId = "location.local-map.v76",
                BirthLocationId = "location.local-map.v76",
                CurrentCellId64 = 123,
                CurrentFacilityId = "facility.local-map.v76",
                LocationPrecisionId = null,
                CurrentLocalSpaceId = null,
                CurrentLocalAnchorId = null
            });
            world.SchemaVersion = 76;

            var migrated = WorldSnapshotMigrator.MigrateToCurrent(world);
            var person = migrated.People.Single();
            Assert.That(migrated.SchemaVersion, Is.EqualTo(77));
            Assert.That(person.CurrentCellId64, Is.EqualTo(123));
            Assert.That(person.CurrentFacilityId,
                Is.EqualTo("facility.local-map.v76"));
            Assert.That(person.LocationPrecisionId, Is.EqualTo(
                LuoyangHumanScaleLocalMapIds.StrategicLocationTypeId));
            Assert.That(person.CurrentLocalSpaceId, Is.Empty);
        }

        [Test]
        public void LuoyangLocalMap_StrategicLocalCoordinateTests_RoundTripsExactly()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            var service = new LuoyangStrategicLocalCoordinateService(plan);
            var space = plan.LocalSpaces[plan.LocalSpaces.Count / 2];
            var local = new LuoyangLocalCoordinate(space.Id, 321.25d,
                1_654.75d, 8d);
            var world = service.LocalToStrategic(local);
            var roundTrip = service.StrategicToLocal(space.ParentCellId64,
                world, local.ElevationMetres);
            Assert.That(roundTrip, Is.EqualTo(local));

            var unityScale = plan.WorldScale;
            var origin = new GlobalProjectedCoordinate(
                world.EastingMetres - 100d,
                world.NorthingMetres - 200d);
            var unity = unityScale.WorldToUnity(world, 8d, origin);
            var restoredWorld = unityScale.UnityToWorld(unity, origin);
            Assert.That(restoredWorld.EastingMetres,
                Is.EqualTo(world.EastingMetres).Within(0.0001d));
            Assert.That(restoredWorld.NorthingMetres,
                Is.EqualTo(world.NorthingMetres).Within(0.0001d));
        }

        [Test]
        public void LuoyangLocalMap_LocalTargetTests_ResolvesFacilityGateBridgeRoadAndGround()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            var resolver = new LuoyangLocalTargetResolver(plan);
            foreach (var expected in new[]
                     {
                         Tuple.Create(FacilitySpatialCapabilityIds.Building,
                             LuoyangLocalTargetKindIds.Facility),
                         Tuple.Create(FacilitySpatialCapabilityIds.Gate,
                             LuoyangLocalTargetKindIds.Gate),
                         Tuple.Create(FacilitySpatialCapabilityIds.Bridge,
                             LuoyangLocalTargetKindIds.Bridge)
                     })
            {
                var capability = plan.FacilityCapabilities.First(item =>
                    item.CapabilityId == expected.Item1 &&
                    item.RequiresAccess);
                var target = resolver.ResolveFacility(
                    capability.FacilityId);
                Assert.That(target.IsValid, Is.True, capability.FacilityId);
                Assert.That(target.KindId, Is.EqualTo(expected.Item2));
                Assert.That(target.LocalNodeId, Is.Not.Empty);
            }
            var access = plan.Entrances.First();
            var ground = resolver.ResolveGround(access.CellId64,
                access.EastMetres, access.NorthMetres);
            Assert.That(ground.IsValid, Is.True,
                ground.FailureReasonId);
            Assert.That(ground.KindId,
                Is.EqualTo(LuoyangLocalTargetKindIds.Ground));
            var road = resolver.ResolveRoad(plan.Edges.First().Id);
            Assert.That(road.IsValid, Is.True, road.FailureReasonId);
            Assert.That(road.KindId,
                Is.EqualTo(LuoyangLocalTargetKindIds.Road));
        }

        [Test]
        public void LuoyangLocalMap_StreamingRangeTests_LoadsUnloadsAndReturnsDeterministically()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            var session = new LuoyangHumanScaleStreamingSession(plan);
            var center = plan.LocalSpaces.First(item =>
                item.GridColumn > LuoyangHumanScaleLocalMapIds.MapMinColumn &&
                item.GridColumn < LuoyangHumanScaleLocalMapIds.MapMaxColumn &&
                item.GridRow > LuoyangHumanScaleLocalMapIds.MapMinRow &&
                item.GridRow < LuoyangHumanScaleLocalMapIds.MapMaxRow);
            var first = session.MoveWindow(center.ParentCellId64);
            Assert.That(first.ResidentCellIds.Count, Is.EqualTo(9));
            Assert.That(first.LoadedCellIds.Count, Is.EqualTo(9));
            Assert.That(first.ResidentFacilityCount, Is.GreaterThanOrEqualTo(0));
            var nextSpace = plan.LocalSpaces.Single(item =>
                item.GridColumn == center.GridColumn + 1 &&
                item.GridRow == center.GridRow);
            var second = session.MoveWindow(nextSpace.ParentCellId64);
            Assert.That(second.LoadedCellIds.Count, Is.EqualTo(3));
            Assert.That(second.UnloadedCellIds.Count, Is.EqualTo(3));
            var returned = session.MoveWindow(center.ParentCellId64);
            Assert.That(returned.ResidentCellIds,
                Is.EqualTo(first.ResidentCellIds));
            Assert.That(returned.MapAssetHash, Is.EqualTo(plan.AssetHash));
        }

        [Test]
        public void LuoyangLocalMap_StreamingWorldFactIsolationTests_DoesNotMutateWorld()
        {
            var fixture = CreateLuoyangHumanScaleMovementFixture();
            var before = WorldSnapshotSerializer.Serialize(fixture.World);
            var session = new LuoyangHumanScaleStreamingSession(
                fixture.Map.Plan);
            foreach (var cell in fixture.Map.Plan.LocalSpaces.Take(5))
                session.MoveWindow(cell.ParentCellId64);
            var after = WorldSnapshotSerializer.Serialize(fixture.World);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void LuoyangLocalMap_LocalMovementReplayTests_ThreeRunsMatch()
        {
            var hashes = new string[3];
            for (var run = 0; run < hashes.Length; run++)
            {
                var fixture = CreateLuoyangHumanScaleMovementFixture();
                Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                        fixture.TargetFacilityId, out var movement, out _,
                        out var failure), Is.True, failure);
                fixture.Service.Complete(fixture.World, movement.Id);
                hashes[run] = SnapshotHash(fixture.World);
            }
            Assert.That(hashes[1], Is.EqualTo(hashes[0]));
            Assert.That(hashes[2], Is.EqualTo(hashes[0]));
        }

        [Test]
        public void LuoyangLocalMap_ExistingV76MovementGraphTests_UpgradesWithoutReplacingWorldFacts()
        {
            var strategic = CreateFormalMovementFixture();
            var map = SharedLuoyangHumanScale.Value;
            foreach (var capability in map.Plan.FacilityCapabilities)
                if (!strategic.World.Facilities.Any(item => item.Id ==
                        capability.FacilityId))
                    strategic.World.Facilities.Add(new FacilityState
                    {
                        Id = capability.FacilityId,
                        DisplayName = capability.FacilityId,
                        DefinitionId = capability.FacilityDefinitionId,
                        CellId64 = capability.CellId64,
                        SettlementId = new PlayerSession(strategic.World)
                            .ControlledPerson.LocationId,
                        LifecycleStatus =
                            FacilityLifecycleStatus.Operational,
                        ConditionBasisPoints = 10_000
                    });
            var originalFacilityCount = strategic.World.Facilities.Count;
            var originalRoadHash = string.Join("|", strategic.World
                .LuoyangRoadOperationalSegments.OrderBy(item => item.EdgeId)
                .Select(item => item.EdgeId + ":" + item.StatusId));
            var runtime = new WorldCommandRuntime();
            var system = new LuoyangFormalPlayerMovementSystem(
                map.StrategicRoads, null, map.Plan);
            system.RegisterHandlers(runtime);

            Assert.That(system.EnsureInitialized(strategic.World, runtime,
                new PlayerSession(strategic.World).ControlledPerson
                    .CurrentFacilityId), Is.True);
            runtime.ProcessDue(strategic.World);
            runtime.DispatchPublishedEvents(strategic.World);

            Assert.That(strategic.World.LuoyangLocalNavigationLocations.Count,
                Is.EqualTo(2_084));
            Assert.That(strategic.World.Facilities.Count,
                Is.EqualTo(originalFacilityCount));
            Assert.That(string.Join("|", strategic.World
                    .LuoyangRoadOperationalSegments
                    .OrderBy(item => item.EdgeId)
                    .Select(item => item.EdgeId + ":" + item.StatusId)),
                Is.EqualTo(originalRoadHash));
            Assert.That(new PlayerSession(strategic.World).ControlledPerson
                .CurrentLocalSpaceId, Is.Not.Empty);
            strategic.World.Validate();
        }

        [Test]
        public void LuoyangLocalMap_GatePassageTests_WaitsAcrossSaveAndResumesAfterReopen()
        {
            var map = SharedLuoyangHumanScale.Value;
            var selected = FindRepresentativeRoute(map.Plan, edge =>
                edge.TraversalConditionId ==
                    LocalTraversalConditionIds.FormalPassageAvailable &&
                map.Plan.FacilityCapabilitiesByFacilityId[
                    edge.PassageFacilityId].CapabilityId ==
                    FacilitySpatialCapabilityIds.Gate);
            var fixture = CreateLuoyangHumanScaleMovementFixture(
                selected.Item1, selected.Item2);
            Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                    selected.Item2, out var movement, out var route,
                    out var failure), Is.True, failure);
            var passageIndex = route.Edges.Select((edge, index) =>
                    new { edge, index }).First(item =>
                    item.edge.TraversalConditionId ==
                    LocalTraversalConditionIds.FormalPassageAvailable &&
                    fixture.Map.Plan.FacilityCapabilitiesByFacilityId[
                        item.edge.PassageFacilityId].CapabilityId ==
                    FacilitySpatialCapabilityIds.Gate).index;
            while (movement.CurrentSegmentIndex < passageIndex)
                fixture.Service.AdvanceNextSegment(fixture.World,
                    movement.Id);
            var passageFacilityId = movement.Segments[passageIndex]
                .FormalWorldObjectId;
            TransitionPassage(fixture.World, fixture.Runtime,
                fixture.Map.StrategicRoads, passageFacilityId,
                LuoyangRoadConnectorPassageTraversalIds.ClosedStatusId,
                "passage.reason.local-map-gate-wait.v1");
            var saved = WorldSnapshotSerializer.Serialize(fixture.World);
            var loaded = WorldSnapshotSerializer.Deserialize(saved);
            var loadedMovement = loaded.LuoyangFormalPlayerMovements.Single(
                item => item.Id == movement.Id);
            Assert.That(loadedMovement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Active));
            var resumed = CreateResumedLocalMovementService(loaded, map,
                passageFacilityId,
                LuoyangRoadConnectorPassageTraversalIds.OpenStatusId);
            resumed.Complete(loaded, loadedMovement.Id);
            Assert.That(loadedMovement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Completed));
            Assert.That(new PlayerSession(loaded).ControlledPerson
                .CurrentFacilityId, Is.EqualTo(selected.Item2));
        }

        [Test]
        public void LuoyangLocalMap_BridgePassageTests_DestroyedWorldBridgeInterruptsFormalMovement()
        {
            var map = SharedLuoyangHumanScale.Value;
            var selected = FindRepresentativeRoute(map.Plan, edge =>
                edge.TraversalConditionId ==
                    LocalTraversalConditionIds.FormalPassageAvailable &&
                map.Plan.FacilityCapabilitiesByFacilityId[
                    edge.PassageFacilityId].CapabilityId ==
                    FacilitySpatialCapabilityIds.Bridge);
            var fixture = CreateLuoyangHumanScaleMovementFixture(
                selected.Item1, selected.Item2);
            Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                    selected.Item2, out var movement, out var route,
                    out var failure), Is.True, failure);
            var passageIndex = route.Edges.Select((edge, index) =>
                    new { edge, index }).First(item =>
                    item.edge.TraversalConditionId ==
                    LocalTraversalConditionIds.FormalPassageAvailable &&
                    fixture.Map.Plan.FacilityCapabilitiesByFacilityId[
                        item.edge.PassageFacilityId].CapabilityId ==
                    FacilitySpatialCapabilityIds.Bridge).index;
            while (movement.CurrentSegmentIndex < passageIndex)
                fixture.Service.AdvanceNextSegment(fixture.World,
                    movement.Id);
            var passageFacilityId = movement.Segments[passageIndex]
                .FormalWorldObjectId;
            TransitionPassage(fixture.World, fixture.Runtime,
                fixture.Map.StrategicRoads, passageFacilityId,
                LuoyangRoadConnectorPassageTraversalIds.DestroyedStatusId,
                "passage.reason.local-map-bridge-destroyed.v1");
            fixture.Service.AdvanceNextSegment(fixture.World, movement.Id);
            Assert.That(movement.Status,
                Is.EqualTo(LuoyangFormalMovementStatus.Interrupted));
            Assert.That(movement.FailureReasonId, Is.EqualTo(
                LuoyangFormalPlayerMovementIds.InvalidRouteReasonId));
        }

        [Test]
        public void LuoyangLocalMap_CrossCellMovementTests_SavesAfterContinuousTransition()
        {
            var map = SharedLuoyangHumanScale.Value;
            var selected = FindRepresentativeRoute(map.Plan,
                edge => edge.CrossesStrategicCellBoundary);
            var fixture = CreateLuoyangHumanScaleMovementFixture(
                selected.Item1, selected.Item2);
            Assert.That(fixture.Service.TryRequestLocal(fixture.World,
                    selected.Item2, out var movement, out var route,
                    out var failure), Is.True, failure);
            var transitionIndex = route.Edges.Select((edge, index) =>
                    new { edge, index }).First(item =>
                    item.edge.CrossesStrategicCellBoundary).index;
            while (movement.CurrentSegmentIndex <= transitionIndex)
                fixture.Service.AdvanceNextSegment(fixture.World,
                    movement.Id);
            var person = new PlayerSession(fixture.World).ControlledPerson;
            Assert.That(person.CurrentCellId64,
                Is.EqualTo(movement.Segments[transitionIndex].ToCellId64));
            var loaded = WorldSnapshotSerializer.Deserialize(
                WorldSnapshotSerializer.Serialize(fixture.World));
            Assert.That(new PlayerSession(loaded).ControlledPerson
                .CurrentCellId64, Is.EqualTo(person.CurrentCellId64));
            Assert.That(new PlayerSession(loaded).ControlledPerson
                .CurrentLocalAnchorId,
                Is.EqualTo(person.CurrentLocalAnchorId));
        }

        private static LuoyangHumanScaleFixture
            CreateLuoyangHumanScaleFixture()
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
                root, coverage.CombinedCatalog, landmarks, performance)
                .Catalog;
            var review = new LuoyangFinalAssetReviewManifestSource(root,
                production, landmarks, gates, fabric, infrastructure, defense,
                resources, civic, performance).Plan;
            var composition = LuoyangWholeCityCompositionRules.CreatePlan(
                performance, review);
            var interaction = LuoyangFacilityInteractionNavigationRules
                .CreatePlan(performance, composition);
            var strategicRoads = LuoyangRoadConnectorPassageTraversalRules
                .CreatePlan(interaction);
            return new LuoyangHumanScaleFixture(performance, composition,
                strategicRoads,
                LuoyangHumanScaleLocalMapRules.CreatePlan(performance,
                    composition, strategicRoads));
        }

        private static LuoyangHumanScaleMovementFixture
            CreateLuoyangHumanScaleMovementFixture(string requestedInitial =
                null, string requestedTarget = null)
        {
            var map = SharedLuoyangHumanScale.Value;
            var planner = new LuoyangHumanScaleLocalRoutePlanner(map.Plan);
            var candidates = map.Plan.FacilityCapabilities.Where(item =>
                    item.RequiresAccess &&
                    item.CapabilityId != FacilitySpatialCapabilityIds.Gate &&
                    item.CapabilityId != FacilitySpatialCapabilityIds.Bridge)
                .OrderBy(item => item.CellId64)
                .ThenBy(item => item.FacilityId,
                    StringComparer.Ordinal).ToArray();
            string initial = null;
            string target = null;
            LuoyangHumanScaleLocalRoute selected = null;
            var startCandidate = requestedInitial == null
                ? candidates[0]
                : map.Plan.FacilityCapabilitiesByFacilityId[
                    requestedInitial];
            var startSpace = map.Plan.LocalSpacesByCellId[
                startCandidate.CellId64];
            var orderedTargets = requestedTarget == null
                ? candidates.Skip(1).OrderBy(item =>
                         Math.Abs(map.Plan.LocalSpacesByCellId[item.CellId64]
                                      .GridColumn - startSpace.GridColumn) +
                         Math.Abs(map.Plan.LocalSpacesByCellId[item.CellId64]
                                      .GridRow - startSpace.GridRow))
                     .ThenBy(item => item.FacilityId,
                         StringComparer.Ordinal).Take(300)
                : new[]
                {
                    map.Plan.FacilityCapabilitiesByFacilityId[requestedTarget]
                };
            foreach (var candidate in orderedTargets)
            {
                if (planner.TryFindRoute(startCandidate.FacilityId,
                        candidate.FacilityId, _ => true, _ => true,
                    out var route, out _) && route.Edges.Count > 1 &&
                    (requestedTarget != null ||
                     route.WeightedDistanceCentimetres < 1_000_000))
                {
                    initial = startCandidate.FacilityId;
                    target = candidate.FacilityId;
                    selected = route;
                }
                if (selected != null) break;
            }
            if (selected == null)
                throw new InvalidOperationException(
                    "No same-cell local movement fixture route exists.");

            var world = WorldState.Create(184);
            world.Locations.Add(new LocationState
            {
                Id = LuoyangHumanScaleLocalMapIds.SettlementLocationId,
                DisplayName = "洛阳",
                Kind = LocationKind.RegionalSeat,
                Terrain = TerrainKind.Plains,
                Features = LocationFeature.Government |
                    LocationFeature.Market,
                Population = 1
            });
            world.People.Add(new PersonState
            {
                Id = "person.m26.local-map-player",
                DisplayName = "M26 洛阳近景玩家",
                LocationId = LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId,
                BirthLocationId = LuoyangHumanScaleLocalMapIds
                    .SettlementLocationId,
                StaminaBasisPoints = 10_000,
                Provisions = 100
            });
            world.PlayerPersonId = "person.m26.local-map-player";
            foreach (var capability in map.Plan.FacilityCapabilities)
                world.Facilities.Add(new FacilityState
                {
                    Id = capability.FacilityId,
                    DisplayName = capability.FacilityId,
                    DefinitionId = capability.FacilityDefinitionId,
                    CellId64 = capability.CellId64,
                    SettlementId = LuoyangHumanScaleLocalMapIds
                        .SettlementLocationId,
                    LifecycleStatus = FacilityLifecycleStatus.Operational,
                    ConditionBasisPoints = 10_000
                });
            world.PopulationStorage.SynchronizeInlineCounts(world.People);
            var runtime = new WorldCommandRuntime();
            var passageSystem = new LuoyangPassageWorldCommandSystem(
                map.StrategicRoads);
            passageSystem.RegisterHandlers(runtime);
            passageSystem.EnsureInitialized(world, runtime);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var movementSystem = new LuoyangFormalPlayerMovementSystem(
                map.StrategicRoads, null, map.Plan);
            movementSystem.RegisterHandlers(runtime);
            movementSystem.EnsureInitialized(world, runtime, initial);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
            var simulator = new WorldSimulator(world.MasterSeed, null,
                new WorldStatePersonRepository(world), runtime);
            var service = new LuoyangFormalPlayerMovementService(
                movementSystem, runtime, simulator);
            world.Validate();
            return new LuoyangHumanScaleMovementFixture(map, world, runtime,
                movementSystem, service, initial, target);
        }

        private static Tuple<string, string>
            FindRepresentativeRoute(LuoyangHumanScaleLocalMapPlan plan,
                Func<LuoyangLocalNavEdge, bool> predicate)
        {
            var planner = new LuoyangHumanScaleLocalRoutePlanner(plan);
            var accessByNode = plan.Entrances.GroupBy(item =>
                    item.AccessNodeId, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.First(),
                    StringComparer.Ordinal);
            var adjacency = plan.Nodes.ToDictionary(item => item.Id,
                _ => new List<Tuple<string, LuoyangLocalNavEdge>>(),
                StringComparer.Ordinal);
            foreach (var edge in plan.Edges)
            {
                adjacency[edge.FromNodeId].Add(Tuple.Create(edge.ToNodeId,
                    edge));
                adjacency[edge.ToNodeId].Add(Tuple.Create(edge.FromNodeId,
                    edge));
            }
            foreach (var selectedEdge in plan.Edges.Where(predicate)
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                var from = FindNearestAccess(selectedEdge.FromNodeId,
                    selectedEdge.Id, selectedEdge.PassageFacilityId,
                    adjacency, accessByNode, plan);
                var to = FindNearestAccess(selectedEdge.ToNodeId,
                    selectedEdge.Id, selectedEdge.PassageFacilityId,
                    adjacency, accessByNode, plan);
                if (from == null || to == null || from == to) continue;
                if (planner.TryFindRoute(from, to, _ => true, _ => true,
                        out var route, out _) && route.Edges.Any(predicate))
                    return Tuple.Create(from, to);
            }
            throw new InvalidOperationException(
                "No representative local route satisfies the predicate.");
        }

        private static string FindNearestAccess(string startNodeId,
            string excludedEdgeId, string excludedFacilityId,
            IReadOnlyDictionary<string,
                List<Tuple<string, LuoyangLocalNavEdge>>> adjacency,
            IReadOnlyDictionary<string, LuoyangFacilityLocalEntrance>
                accessByNode,
            LuoyangHumanScaleLocalMapPlan plan)
        {
            var queue = new Queue<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal)
                { startNodeId };
            queue.Enqueue(startNodeId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (accessByNode.TryGetValue(current, out var access) &&
                    access.FacilityId != excludedFacilityId &&
                    plan.FacilityCapabilitiesByFacilityId[access.FacilityId]
                        .CapabilityId != FacilitySpatialCapabilityIds.Gate &&
                    plan.FacilityCapabilitiesByFacilityId[access.FacilityId]
                        .CapabilityId != FacilitySpatialCapabilityIds.Bridge)
                    return access.FacilityId;
                foreach (var next in adjacency[current].OrderBy(item =>
                             item.Item2.Id, StringComparer.Ordinal))
                    if (next.Item2.Id != excludedEdgeId &&
                        seen.Add(next.Item1))
                    {
                        queue.Enqueue(next.Item1);
                    }
            }
            return null;
        }

        private static LuoyangFormalPlayerMovementService
            CreateResumedLocalMovementService(WorldState world,
                LuoyangHumanScaleFixture map,
                string transitionFacilityId = null,
                string transitionStatusId = null)
        {
            var runtime = new WorldCommandRuntime();
            if (!string.IsNullOrEmpty(transitionFacilityId))
                TransitionPassage(world, runtime, map.StrategicRoads,
                    transitionFacilityId, transitionStatusId,
                    "passage.reason.local-map-resume.v1");
            var movementSystem = new LuoyangFormalPlayerMovementSystem(
                map.StrategicRoads, null, map.Plan);
            movementSystem.RegisterHandlers(runtime);
            var simulator = new WorldSimulator(world.MasterSeed, null,
                new WorldStatePersonRepository(world), runtime);
            return new LuoyangFormalPlayerMovementService(movementSystem,
                runtime, simulator);
        }

        private static void TransitionPassage(WorldState world,
            WorldCommandRuntime runtime,
            LuoyangRoadTraversalRefinementPlan roads, string facilityId,
            string statusId, string reasonId)
        {
            var passageSystem = new LuoyangPassageWorldCommandSystem(roads);
            passageSystem.RegisterHandlers(runtime);
            Assert.That(passageSystem.EnqueueTransition(world, runtime,
                facilityId, statusId, reasonId,
                "person.m26.local-map-player"), Is.True);
            runtime.ProcessDue(world);
            runtime.DispatchPublishedEvents(world);
        }

        private static string SnapshotHash(WorldState world)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                WorldSnapshotSerializer.Serialize(world));
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(item =>
                item.ToString("x2",
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        private sealed class LuoyangHumanScaleFixture
        {
            public LuoyangHumanScaleFixture(
                LuoyangBuildingPerformancePlan performance,
                LuoyangWholeCityCompositionPlan composition,
                LuoyangRoadTraversalRefinementPlan strategicRoads,
                LuoyangHumanScaleLocalMapPlan plan)
            {
                Performance = performance;
                Composition = composition;
                StrategicRoads = strategicRoads;
                Plan = plan;
            }

            public LuoyangBuildingPerformancePlan Performance { get; }
            public LuoyangWholeCityCompositionPlan Composition { get; }
            public LuoyangRoadTraversalRefinementPlan StrategicRoads { get; }
            public LuoyangHumanScaleLocalMapPlan Plan { get; }
        }

        private sealed class LuoyangHumanScaleMovementFixture
        {
            public LuoyangHumanScaleMovementFixture(
                LuoyangHumanScaleFixture map, WorldState world,
                WorldCommandRuntime runtime,
                LuoyangFormalPlayerMovementSystem system,
                LuoyangFormalPlayerMovementService service,
                string initialFacilityId, string targetFacilityId)
            {
                Map = map;
                World = world;
                Runtime = runtime;
                System = system;
                Service = service;
                InitialFacilityId = initialFacilityId;
                TargetFacilityId = targetFacilityId;
            }
            public LuoyangHumanScaleFixture Map { get; }
            public WorldState World { get; }
            public WorldCommandRuntime Runtime { get; }
            public LuoyangFormalPlayerMovementSystem System { get; }
            public LuoyangFormalPlayerMovementService Service { get; }
            public string InitialFacilityId { get; }
            public string TargetFacilityId { get; }
        }
    }
}
