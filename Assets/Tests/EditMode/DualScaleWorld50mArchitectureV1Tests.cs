using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void DualScale50m_Contract_TwoKilometresExactlyContainsFortyByFortyCells()
        {
            Assert.That(DualScaleCountySpatialContractV1.StrategicTileSizeMetres,
                Is.EqualTo(2_000));
            Assert.That(DualScaleCountySpatialContractV1.PlanningCellSizeMetres,
                Is.EqualTo(50));
            Assert.That(DualScaleCountySpatialContractV1.PlanningCellsPerStrategicTile,
                Is.EqualTo(1_600));
        }

        [Test]
        public void DualScale50m_CoordinateProjection_StrategicMinimumAndLastPlanningCellRoundTrip()
        {
            var projection = new DualScaleCoordinateProjection();
            var tile = new StrategicTileCoord(1240, 2042);
            var minimum = projection.StrategicTileMinimum(tile);
            Assert.That(projection.ToStrategicTile(minimum), Is.EqualTo(tile));
            Assert.That(projection.ToStrategicTile(new PlanningCellCoord(
                minimum.Row + 39, minimum.Column + 39)), Is.EqualTo(tile));
        }

        [Test]
        public void DualScale50m_CoordinateProjection_AdjacentStrategicTilesHaveNoGapOrOverlap()
        {
            var projection = new DualScaleCoordinateProjection();
            var left = projection.StrategicTileMinimum(
                new StrategicTileCoord(1240, 2042));
            var right = projection.StrategicTileMinimum(
                new StrategicTileCoord(1240, 2043));
            Assert.That(right.Column - left.Column, Is.EqualTo(40));
            var leftEdge = projection.PlanningCellCenter(
                new PlanningCellCoord(left.Row, left.Column + 39));
            var rightEdge = projection.PlanningCellCenter(right);
            Assert.That(rightEdge.EastingMetres - leftEdge.EastingMetres,
                Is.EqualTo(50d).Within(0.000001d));
        }

        [Test]
        public void DualScale50m_CoordinateProjection_GlobalCoordinateRoundTripsBothScales()
        {
            var scenario = Scenario();
            var cell = scenario.WestCounty.ToGlobalCell(23, 17);
            var point = scenario.Projection.PlanningCellCenter(cell);
            Assert.That(scenario.Projection.ToPlanningCell(point), Is.EqualTo(cell));
            Assert.That(scenario.Projection.ToStrategicTile(point),
                Is.EqualTo(scenario.Projection.ToStrategicTile(cell)));
        }

        [Test]
        public void DualScale50m_Fixture_IsTwoByTwoStrategicTilesAndExactlySixThousandFourHundredCells()
        {
            var scenario = Scenario();
            Assert.That(scenario.PlanningCellCount, Is.EqualTo(6_400));
            var partitions = new[] { scenario.WestCounty, scenario.EastCounty };
            var tiles = partitions.SelectMany(partition =>
                    Enumerable.Range(0, partition.Rows).SelectMany(row =>
                        Enumerable.Range(0, partition.Columns).Select(column =>
                            scenario.Projection.ToStrategicTile(
                                partition.ToGlobalCell(row, column)))))
                .Distinct().ToArray();
            Assert.That(tiles.Length, Is.EqualTo(4));
        }

        [Test]
        public void DualScale50m_FourWayConnection_SetBetweenAlwaysMirrorsTheNeighborEdge()
        {
            var grid = new PlanningCellConnectionGrid(4, 4);
            grid.SetBetween(1, 1, PlanningCellDirection.East,
                PlanningCellConnectionKind.OpenByRoad);
            Assert.That(grid.Get(1, 1, PlanningCellDirection.East),
                Is.EqualTo(PlanningCellConnectionKind.OpenByRoad));
            Assert.That(grid.Get(1, 2, PlanningCellDirection.West),
                Is.EqualTo(PlanningCellConnectionKind.OpenByRoad));
        }

        [Test]
        public void DualScale50m_FourWayConnection_OutsideEdgeIsNotPassable()
        {
            var grid = new PlanningCellConnectionGrid(2, 2);
            Assert.That(grid.Get(0, 0, PlanningCellDirection.North),
                Is.EqualTo(PlanningCellConnectionKind.OutsidePartition));
            Assert.That(grid.IsPassable(0, 0, PlanningCellDirection.North),
                Is.False);
        }

        [Test]
        public void DualScale50m_Facility_IsPhysicalPlacementAndNotThePlanningCell()
        {
            var scenario = Scenario();
            var placement = scenario.Placement(
                DualScaleSpatialValidationScenarioFactory.HouseFacilityId);
            Assert.That(placement.FacilityId,
                Is.EqualTo(scenario.Facility(placement.FacilityId).Id));
            Assert.That(placement.CollisionProfileId, Is.Not.Empty);
            Assert.That(placement.Entrances, Has.Count.EqualTo(1));
            Assert.That(placement.ResolveCoveredPlanningCells(
                scenario.Projection), Has.Count.EqualTo(1));
        }

        [Test]
        public void DualScale50m_Facility_LargeRotatedFootprintOccupiesMultiplePlanningCells()
        {
            var scenario = Scenario();
            var placement = scenario.Placement(
                DualScaleSpatialValidationScenarioFactory.StorehouseFacilityId);
            Assert.That(placement.RotationQuarterTurns, Is.EqualTo(1));
            Assert.That(placement.ResolveCoveredPlanningCells(
                scenario.Projection).Count, Is.GreaterThan(1));
        }

        [Test]
        public void DualScale50m_Facility_EntranceIsAStablePhysicalAnchor()
        {
            var scenario = Scenario();
            var placement = scenario.Placement(
                DualScaleSpatialValidationScenarioFactory.HouseFacilityId);
            var entrance = placement.Entrances.Single();
            Assert.That(placement.Entrance(entrance.Id), Is.SameAs(entrance));
            Assert.That(scenario.Projection.ToPlanningCell(entrance.Position),
                Is.Not.Null);
        }

        [Test]
        public void DualScale50m_Person_EnterAndExitFacilityKeepsSameAuthoritativePerson()
        {
            var scenario = Scenario();
            var person = scenario.World.People.Single();
            var placement = scenario.Placement(
                DualScaleSpatialValidationScenarioFactory.HouseFacilityId);
            var service = new PersonSpatialTransitionServiceV1();
            var inside = service.EnterFacility(person, scenario.PersonSpatial,
                placement, placement.Entrances[0].Id);
            Assert.That(inside.PersonId, Is.EqualTo(person.Id));
            Assert.That(person.CurrentFacilityId, Is.EqualTo(placement.FacilityId));
            var outside = service.ExitFacility(person, inside, placement,
                placement.Entrances[0].Id);
            Assert.That(outside.PersonId, Is.EqualTo(person.Id));
            Assert.That(person.CurrentFacilityId, Is.Empty);
        }

        [Test]
        public void DualScale50m_Person_CannotEnterFacilityBeforeReachingEntrance()
        {
            var scenario = Scenario();
            var person = scenario.World.People.Single();
            var placement = scenario.Placement(
                DualScaleSpatialValidationScenarioFactory.HouseFacilityId);
            var wrongPosition = PersonSpatialStateV1.CountyLocal(person.Id,
                scenario.WestCounty.CountyId,
                scenario.Projection.PlanningCellCenter(
                    scenario.WestCounty.ToGlobalCell(0, 0)));
            Assert.Throws<InvalidOperationException>(() =>
                new PersonSpatialTransitionServiceV1().EnterFacility(person,
                    wrongPosition, placement, placement.Entrances[0].Id));
        }

        [Test]
        public void DualScale50m_Person_AllFourSpatialModesCarryExactlyOneAuthority()
        {
            var point = new GlobalProjectedCoordinate(1d, 2d);
            var values = new[]
            {
                PersonSpatialStateV1.CountyLocal("person.a", "county.a", point),
                PersonSpatialStateV1.InsideFacility("person.a", "facility.a"),
                PersonSpatialStateV1.StrategicTransit("person.a", "route.a",
                    "segment.a", 5000),
                PersonSpatialStateV1.ArmyAttached("person.a", "army.a")
            };
            Assert.That(values.Select(value => value.Mode).Distinct().Count(),
                Is.EqualTo(4));
        }

        [Test]
        public void DualScale50m_Army_MaterializeAndReturnKeepsOneArmyIdentity()
        {
            var scenario = Scenario();
            var army = scenario.World.Armies.Single();
            var portal = scenario.Route.Portals.First();
            var service = new ArmySpatialTransitionServiceV1();
            var local = service.Materialize(army, scenario.ArmySpatial,
                portal, scenario.Projection);
            Assert.That(local.Mode, Is.EqualTo(ArmySpatialModeV1.CountyMaterialized));
            Assert.That(local.ArmyId, Is.EqualTo(army.Id));
            var strategic = service.ReturnToStrategic(army, local, portal);
            Assert.That(strategic.Mode, Is.EqualTo(ArmySpatialModeV1.Strategic));
            Assert.That(strategic.ArmyId, Is.EqualTo(army.Id));
        }

        [Test]
        public void DualScale50m_CountyPortal_PersonUsesSameRouteToLeaveAndArrive()
        {
            var scenario = Scenario();
            var person = scenario.World.People.Single();
            var source = scenario.Route.Portals.Single(portal =>
                portal.CountyId == scenario.WestCounty.CountyId);
            var destination = scenario.Route.Portals.Single(portal =>
                portal.CountyId == scenario.EastCounty.CountyId);
            var atPortal = PersonSpatialStateV1.CountyLocal(person.Id,
                source.CountyId,
                scenario.Projection.PlanningCellCenter(source.Cell));
            var service = new PersonSpatialTransitionServiceV1();
            var transit = service.BeginStrategicTransit(person, atPortal,
                source, "segment.validation.v1", scenario.Projection);
            var arrived = service.ArriveFromStrategicTransit(person, transit,
                destination, scenario.Projection);
            Assert.That(transit.RouteId, Is.EqualTo(scenario.World.Routes.Single().Id));
            Assert.That(arrived.CountyId, Is.EqualTo(destination.CountyId));
            Assert.That(arrived.PersonId, Is.EqualTo(person.Id));
        }

        [Test]
        public void DualScale50m_CountyPortal_MultiplePortalsForOneCountyAreAllowed()
        {
            var scenario = Scenario();
            var existing = scenario.Route.Portals.Single(portal =>
                portal.CountyId == scenario.WestCounty.CountyId);
            scenario.WestCounty.AddPortal(new CountyPortalSpatialState(
                "portal.validation.second.v1", existing.RouteId,
                existing.CountyId, existing.NeighborCountyId,
                scenario.WestCounty.ToGlobalCell(0, 20),
                scenario.Projection.ToStrategicTile(
                    scenario.WestCounty.ToGlobalCell(0, 20)),
                "portal.passage.mountain.v1"));
            Assert.That(scenario.WestCounty.Portals.Count, Is.EqualTo(2));
        }

        [Test]
        public void DualScale50m_Streaming_HotWarmColdOnlyChangesCacheResidency()
        {
            var scenario = Scenario();
            var before = DualScaleWorldSummaryV1.Create(scenario.World);
            var spatialBefore = scenario.WestCounty.ComputeSpatialHash();
            var loader = new CountySpatialLoadCoordinator(scenario.Projection);
            var hot = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Hot);
            var warm = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Warm);
            var cold = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Cold);
            Assert.That(hot.ResidentPlanningCellCount, Is.EqualTo(3_200));
            Assert.That(warm.ResidentPlanningCellCount, Is.Zero);
            Assert.That(cold.ResidentPortalCount, Is.Zero);
            Assert.That(scenario.WestCounty.ComputeSpatialHash(),
                Is.EqualTo(spatialBefore));
            Assert.That(DualScaleWorldSummaryV1.Create(scenario.World),
                Is.EqualTo(before));
        }

        [Test]
        public void DualScale50m_Streaming_DifferentResidencyProducesIdenticalDailyWorldResult()
        {
            var cold = Scenario();
            var warm = Scenario();
            var hot = Scenario();
            new CountySpatialLoadCoordinator(cold.Projection).SetLevel(
                cold.WestCounty, CountySpatialLoadLevel.Cold);
            new CountySpatialLoadCoordinator(warm.Projection).SetLevel(
                warm.WestCounty, CountySpatialLoadLevel.Warm);
            new CountySpatialLoadCoordinator(hot.Projection).SetLevel(
                hot.WestCounty, CountySpatialLoadLevel.Hot);
            AdvanceOneDay(cold.World);
            AdvanceOneDay(warm.World);
            AdvanceOneDay(hot.World);
            var expected = DualScaleWorldSummaryV1.Create(cold.World);
            Assert.That(DualScaleWorldSummaryV1.Create(warm.World),
                Is.EqualTo(expected));
            Assert.That(DualScaleWorldSummaryV1.Create(hot.World),
                Is.EqualTo(expected));
        }

        [Test]
        public void DualScale50m_Wall_IsStoredOnCellEdgeAndDoesNotConsumeEitherCell()
        {
            var scenario = Scenario();
            var wall = scenario.WestCounty.Fortifications.Values.First(item =>
                !item.IsGate);
            Assert.That(wall.Edge.First, Is.Not.EqualTo(wall.Edge.Second));
            Assert.That(scenario.WestCounty.PlanningCellCount, Is.EqualTo(3_200));
            Assert.That(wall.PassageKind,
                Is.EqualTo(PlanningCellConnectionKind.BlockedByWall));
        }

        [Test]
        public void DualScale50m_Gate_ClosedAndOpenChangeOnlyPassageState()
        {
            var scenario = Scenario();
            var gate = scenario.WestCounty.Fortifications[
                "fortification.validation.gate.v1"];
            var durability = gate.Durability;
            gate.SetGateState(GatePassageStateV1.Open);
            Assert.That(gate.PassageKind,
                Is.EqualTo(PlanningCellConnectionKind.OpenByGate));
            gate.SetGateState(GatePassageStateV1.Closed);
            Assert.That(gate.PassageKind,
                Is.EqualTo(PlanningCellConnectionKind.BlockedByClosedGate));
            Assert.That(gate.Durability, Is.EqualTo(durability));
        }

        [Test]
        public void DualScale50m_Fortification_ZeroDurabilityCreatesPassableBreach()
        {
            var scenario = Scenario();
            var gate = scenario.WestCounty.Fortifications[
                "fortification.validation.gate.v1"];
            gate.ApplyDamage(gate.MaximumDurability);
            Assert.That(gate.Durability, Is.Zero);
            Assert.That(gate.GateState, Is.EqualTo(GatePassageStateV1.Breached));
            Assert.That(gate.PassageKind,
                Is.EqualTo(PlanningCellConnectionKind.OpenThroughBreach));
        }

        [Test]
        public void DualScale50m_HeightAndLos_LowObserverBlockedButHighPlatformVisible()
        {
            var query = new SpatialLineOfSightQueryV1();
            var low = new EffectiveElevationSample(
                new GlobalProjectedCoordinate(0d, 0d), 1_000, 0, 100);
            var high = new EffectiveElevationSample(
                new GlobalProjectedCoordinate(0d, 0d), 1_000, 2_000, 100);
            var target = new EffectiveElevationSample(
                new GlobalProjectedCoordinate(10d, 0d), 1_000, 0, 100);
            var wall = new SpatialOccluderV1("wall.test", 4d, 6d, -1d, 1d,
                1_200);
            Assert.That(query.HasLineOfSight(low, target, new[] { wall },
                out var blocker), Is.False);
            Assert.That(blocker, Is.EqualTo("wall.test"));
            Assert.That(query.HasLineOfSight(high, target, new[] { wall },
                out _), Is.True);
        }

        [Test]
        public void DualScale50m_HeightAndLos_TallerObstacleBlocksHighPlatform()
        {
            var query = new SpatialLineOfSightQueryV1();
            var observer = new EffectiveElevationSample(
                new GlobalProjectedCoordinate(0d, 0d), 1_000, 2_000, 100);
            var target = new EffectiveElevationSample(
                new GlobalProjectedCoordinate(10d, 0d), 1_000, 0, 100);
            var tower = new SpatialOccluderV1("tower.test", 4d, 6d, -1d,
                1d, 4_000);
            Assert.That(query.HasLineOfSight(observer, target,
                new[] { tower }, out _), Is.False);
        }

        [Test]
        public void DualScale50m_FacilityCombat_DamageAndGarrisonAreIndependent()
        {
            var facility = Scenario().Facility(
                DualScaleSpatialValidationScenarioFactory.ArrowTowerFacilityId);
            var defense = new FacilityDefenseStateV1(facility.Id, 20);
            FacilityConflictRulesV1.ApplyStructuralDamage(facility, 2_000);
            Assert.That(facility.ConditionBasisPoints, Is.EqualTo(8_000));
            Assert.That(defense.GarrisonCount, Is.EqualTo(20));
            defense.ApplyLoss(5);
            Assert.That(facility.ConditionBasisPoints, Is.EqualTo(8_000));
            Assert.That(defense.GarrisonCount, Is.EqualTo(15));
        }

        [Test]
        public void DualScale50m_FacilityCombat_CanOccupyIntactFacilityAfterGarrisonCleared()
        {
            var facility = Scenario().Facility(
                DualScaleSpatialValidationScenarioFactory.ArrowTowerFacilityId);
            var owner = facility.OwnerId;
            var defense = new FacilityDefenseStateV1(facility.Id, 10);
            FacilityConflictRulesV1.ApplyStructuralDamage(facility, 2_000);
            defense.ApplyLoss(10);
            Assert.That(FacilityConflictRulesV1.TryOccupy(facility, defense,
                "organization.validation.attacker.v1"), Is.True);
            Assert.That(facility.ConditionBasisPoints, Is.EqualTo(8_000));
            Assert.That(facility.OwnerId, Is.EqualTo(owner));
            Assert.That(facility.ControllerId,
                Is.EqualTo("organization.validation.attacker.v1"));
        }

        [Test]
        public void DualScale50m_FacilityCombat_SurrenderAllowsOccupationWithoutDestruction()
        {
            var facility = Scenario().Facility(
                DualScaleSpatialValidationScenarioFactory.WatchtowerFacilityId);
            var defense = new FacilityDefenseStateV1(facility.Id, 30);
            defense.Surrender();
            Assert.That(FacilityConflictRulesV1.TryOccupy(facility, defense,
                "organization.validation.attacker.v1"), Is.True);
            Assert.That(facility.ConditionBasisPoints, Is.EqualTo(10_000));
        }

        [Test]
        public void DualScale50m_FacilityCombat_DisabledCanRepairButDestroyedCannotOrdinarilyRepair()
        {
            var disabled = Scenario().Facility(
                DualScaleSpatialValidationScenarioFactory.WatchtowerFacilityId);
            FacilityConflictRulesV1.ApplyStructuralDamage(disabled, 10_000);
            Assert.That(disabled.LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Disabled));
            Assert.That(FacilityConflictRulesV1.TryRepair(disabled, 2_000),
                Is.True);
            var destroyed = Scenario().Facility(
                DualScaleSpatialValidationScenarioFactory.SiegePlatformFacilityId);
            FacilityConflictRulesV1.Destroy(destroyed);
            Assert.That(FacilityConflictRulesV1.TryRepair(destroyed, 2_000),
                Is.False);
            Assert.That(destroyed.LifecycleStatus,
                Is.EqualTo(FacilityLifecycleStatus.Destroyed));
        }

        [Test]
        public void DualScale50m_Persistence_PrototypeDoesNotUpgradeFormalWorldSchema()
        {
            var scenario = Scenario();
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
            Assert.That(scenario.World.SchemaVersion,
                Is.EqualTo(WorldState.CurrentSchemaVersion));
            Assert.That(DualScaleCountySpatialContractV1.PlanningGridSchemaVersion,
                Does.Contain("candidate"));
        }

        [Test]
        public void DualScale50m_Performance_SixThousandFourHundredCellFixtureMeetsArchitectureBudgetAndWritesEvidence()
        {
            ForceCollection();
            var allocationBefore = GC.GetTotalMemory(true);
            var timer = Stopwatch.StartNew();
            var scenario = Scenario();
            timer.Stop();
            var allocationBytes = Math.Max(0,
                GC.GetTotalMemory(true) - allocationBefore);
            var loader = new CountySpatialLoadCoordinator(scenario.Projection);
            var hot = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Hot);
            var warm = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Warm);
            var cold = loader.SetLevel(scenario.WestCounty,
                CountySpatialLoadLevel.Cold);
            var packedBytes = scenario.WestCounty.PackedArrayBytes +
                              scenario.EastCounty.PackedArrayBytes;
            WritePerformanceEvidence(scenario, timer.Elapsed.TotalMilliseconds,
                allocationBytes, packedBytes, hot, warm, cold);
            Assert.That(scenario.PlanningCellCount, Is.EqualTo(6_400));
            Assert.That(timer.Elapsed.TotalMilliseconds, Is.LessThan(2_000d));
            Assert.That(allocationBytes, Is.LessThan(64L * 1024L * 1024L));
            Assert.That(packedBytes, Is.LessThan(2 * 1024 * 1024));
            Assert.That(hot.BuildMilliseconds, Is.LessThan(500d));
        }

        private static DualScaleSpatialValidationScenario Scenario() =>
            DualScaleSpatialValidationScenarioFactory.Create();

        private static void AdvanceOneDay(WorldState world)
        {
            var scheduler = new WorldSystemScheduler();
            scheduler.Register(new WorldScheduledSystem(
                "system.validation.dual-scale-daily.v1",
                WorldSystemPhase.DailySimulation,
                WorldSystemCadence.NewDay, 0, context =>
                {
                    context.World.AbsoluteDay++;
                    context.World.Inventories.Single().Quantity += 3;
                    context.World.People.Single().Wealth += 2;
                    context.World.Facilities.Single(facility => facility.Id ==
                        DualScaleSpatialValidationScenarioFactory.HouseFacilityId)
                        .WorkerPersonCount = 1;
                }));
            scheduler.ExecutePhase(WorldSystemPhase.DailySimulation,
                new WorldSystemExecutionContext(world, true));
        }

        private static void ForceCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void WritePerformanceEvidence(
            DualScaleSpatialValidationScenario scenario,
            double buildMilliseconds, long allocationBytes, int packedBytes,
            CountySpatialCacheHandle hot, CountySpatialCacheHandle warm,
            CountySpatialCacheHandle cold)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(),
                "Docs", "Evidence",
                "DualScaleWorld50mCountySpatialArchitectureV1");
            Directory.CreateDirectory(directory);
            const int theoreticalLuoyangCells = 204_800;
            var bytesPerCell = packedBytes / (double)scenario.PlanningCellCount;
            var estimatedBytes = (long)Math.Ceiling(bytesPerCell *
                                                    theoreticalLuoyangCells);
            var json = "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"fixturePlanningCellCount\": 6400,\n" +
                $"  \"fixtureChunkCount\": {scenario.WestCounty.ChunkCount + scenario.EastCounty.ChunkCount},\n" +
                $"  \"buildMilliseconds\": {buildMilliseconds:F3},\n" +
                $"  \"managedAllocationBytes\": {allocationBytes},\n" +
                $"  \"packedAuthoritativeArrayBytes\": {packedBytes},\n" +
                $"  \"packedBytesPerCell\": {bytesPerCell:F3},\n" +
                "  \"planningCellGameObjects\": 0,\n" +
                $"  \"hotBuildMilliseconds\": {hot.BuildMilliseconds:F3},\n" +
                $"  \"hotManagedAllocationBytes\": {hot.ManagedAllocationBytes},\n" +
                $"  \"warmBuildMilliseconds\": {warm.BuildMilliseconds:F3},\n" +
                $"  \"warmManagedAllocationBytes\": {warm.ManagedAllocationBytes},\n" +
                $"  \"coldBuildMilliseconds\": {cold.BuildMilliseconds:F3},\n" +
                $"  \"coldManagedAllocationBytes\": {cold.ManagedAllocationBytes},\n" +
                "  \"theoreticalLuoyangPlanningCellCount\": 204800,\n" +
                $"  \"theoreticalPackedArrayBytesEstimate\": {estimatedBytes},\n" +
                "  \"theoreticalEstimateWasExecuted\": false,\n" +
                "  \"note\": \"204800 is a linear packed-array estimate, not a passed runtime benchmark.\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(directory,
                "performance-core.json"), json);
        }
    }
}
