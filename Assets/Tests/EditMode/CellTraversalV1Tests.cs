using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void CellTraversalV1_TopologyTests_EnforcesTerminalStraightCornerTAndCross()
        {
            var terminal = Profile(1, CellInternalTopology.Terminal, false,
                MovementCapabilityIds.Foot,
                CellTraversalDirection.North,
                CellTraversalDirection.South);
            var straight = Profile(2, CellInternalTopology.Straight, true,
                MovementCapabilityIds.Foot,
                CellTraversalDirection.North,
                CellTraversalDirection.South);
            var corner = Profile(3, CellInternalTopology.Corner, true,
                MovementCapabilityIds.Foot,
                CellTraversalDirection.North,
                CellTraversalDirection.East);
            var tee = Profile(4, CellInternalTopology.TIntersection, true,
                MovementCapabilityIds.Foot,
                CellTraversalDirection.North,
                CellTraversalDirection.East,
                CellTraversalDirection.West);
            var cross = Profile(5, CellInternalTopology.Cross, true,
                MovementCapabilityIds.Foot,
                CellTraversalDirections.All.ToArray());

            Assert.That(terminal.AllowsInternal(
                CellTraversalDirection.North,
                CellTraversalDirection.South), Is.False);
            Assert.That(straight.AllowsInternal(
                CellTraversalDirection.North,
                CellTraversalDirection.South), Is.True);
            Assert.That(straight.AllowsInternal(
                CellTraversalDirection.North,
                CellTraversalDirection.East), Is.False);
            Assert.That(corner.AllowsInternal(CellTraversalDirection.North,
                CellTraversalDirection.East), Is.True);
            Assert.That(tee.AllowsInternal(CellTraversalDirection.North,
                CellTraversalDirection.West), Is.True);
            Assert.That(cross.AllowsInternal(CellTraversalDirection.South,
                CellTraversalDirection.East), Is.True);
        }

        [Test]
        public void CellTraversalV1_PlannerTests_UsesCardinalPortsAndRejectsDiagonalOnlyContact()
        {
            var grid = Grid();
            var origin = Id(grid, 2, 1);
            var diagonal = Id(grid, 1, 2);
            var planner = Planner(grid,
                Profile(origin, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()),
                Profile(diagonal, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()));

            Assert.That(planner.TryFindRoute(origin, diagonal,
                    MovementCapabilityIds.Foot, null, out _, out var failure),
                Is.False);
            Assert.That(failure,
                Is.EqualTo("cell-route.failure.unreachable.v1"));
        }

        [Test]
        public void CellTraversalV1_FacilityEntryTests_AllowsDestinationButNeverUsesBuildingAsShortcut()
        {
            var grid = Grid();
            var west = Id(grid, 2, 1);
            var building = Id(grid, 2, 2);
            var east = Id(grid, 2, 3);
            var buildingProfile = Profile(building,
                CellInternalTopology.Terminal, false,
                MovementCapabilityIds.Foot,
                CellTraversalDirection.East,
                CellTraversalDirection.West);
            buildingProfile.FacilityId = "facility.test.multi-entrance";
            buildingProfile.AccessRequirementId =
                FacilityAccessRequirementIds.Optional;
            var planner = Planner(grid,
                Profile(west, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()),
                buildingProfile,
                Profile(east, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()));

            Assert.That(planner.TryFindRoute(west, building,
                    MovementCapabilityIds.Foot, null, out var destination,
                    out var failure), Is.True, failure);
            Assert.That(destination.TargetCellId64, Is.EqualTo(building));
            Assert.That(planner.TryFindRoute(west, east,
                    MovementCapabilityIds.Foot, null, out _, out failure),
                Is.False);
        }

        [Test]
        public void CellTraversalV1_PassageStateTests_GateAndBridgeCloseAndReopenWithoutChangingTopology()
        {
            foreach (var formalId in new[]
                     {
                         "facility.test.gate", "facility.test.bridge"
                     })
            {
                var grid = Grid();
                var west = Id(grid, 2, 1);
                var passage = Id(grid, 2, 2);
                var east = Id(grid, 2, 3);
                var passageProfile = Profile(passage,
                    CellInternalTopology.Straight, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirection.East,
                    CellTraversalDirection.West);
                passageProfile.FacilityId = formalId;
                passageProfile.FacilityCapabilityId = formalId.EndsWith(
                    "gate", StringComparison.Ordinal)
                    ? FacilitySpatialCapabilityIds.Gate
                    : FacilitySpatialCapabilityIds.Bridge;
                foreach (var port in passageProfile.Ports.Where(item =>
                             item.Enabled))
                {
                    port.TraversalConditionId =
                        CellTraversalIds.FormalPassageConditionId;
                    port.FormalWorldObjectId = formalId;
                }
                var planner = Planner(grid,
                    Profile(west, CellInternalTopology.OpenArea, true,
                        MovementCapabilityIds.Foot,
                        CellTraversalDirections.All.ToArray()),
                    passageProfile,
                    Profile(east, CellInternalTopology.OpenArea, true,
                        MovementCapabilityIds.Foot,
                        CellTraversalDirections.All.ToArray()));
                var open = true;
                bool Available(CellTraversalPort port) =>
                    port.TraversalConditionId !=
                        CellTraversalIds.FormalPassageConditionId || open;

                Assert.That(planner.TryFindRoute(west, east,
                        MovementCapabilityIds.Foot, Available, out var route,
                        out var failure), Is.True, failure);
                Assert.That(route.Segments.Any(item =>
                    item.TraversalConditionId ==
                    CellTraversalIds.FormalPassageConditionId), Is.True);
                open = false;
                Assert.That(planner.TryFindRoute(west, east,
                        MovementCapabilityIds.Foot, Available, out _,
                        out failure), Is.False);
                open = true;
                Assert.That(planner.TryFindRoute(west, east,
                        MovementCapabilityIds.Foot, Available, out _,
                        out failure), Is.True, failure);
            }
        }

        [Test]
        public void CellTraversalV1_MovementCapabilityTests_AllowsForestFootAndRejectsForestCart()
        {
            var grid = Grid();
            var west = Id(grid, 2, 1);
            var forest = Id(grid, 2, 2);
            var east = Id(grid, 2, 3);
            var planner = Planner(grid,
                Profile(west, CellInternalTopology.OpenArea, true,
                    new[] { MovementCapabilityIds.Foot,
                        MovementCapabilityIds.Cart },
                    CellTraversalDirections.All.ToArray()),
                Profile(forest, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()),
                Profile(east, CellInternalTopology.OpenArea, true,
                    new[] { MovementCapabilityIds.Foot,
                        MovementCapabilityIds.Cart },
                    CellTraversalDirections.All.ToArray()));

            Assert.That(planner.TryFindRoute(west, east,
                    MovementCapabilityIds.Foot, null, out _, out var failure),
                Is.True, failure);
            Assert.That(planner.TryFindRoute(west, east,
                    MovementCapabilityIds.Cart, null, out _, out failure),
                Is.False);
        }

        [Test]
        public void CellTraversalV1_TraversalCostTests_RoadIsPreferredButOffRoadRemainsFallback()
        {
            var grid = Grid();
            var origin = Id(grid, 2, 1);
            var target = Id(grid, 2, 3);
            var forest = Profile(Id(grid, 2, 2),
                CellInternalTopology.OpenArea, true,
                MovementCapabilityIds.Foot,
                CellTraversalDirections.All.ToArray());
            forest.TraversalDistanceCentimetres = 18_000;
            forest.TraversalCostPermilleByCapability[
                MovementCapabilityIds.Foot] = 1_500;
            var profiles = new List<CellTraversalProfile>
            {
                Profile(origin, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray()),
                forest,
                Profile(target, CellInternalTopology.OpenArea, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray())
            };
            foreach (var column in new[] { 1, 2, 3 })
            {
                var road = Profile(Id(grid, 1, column),
                    CellInternalTopology.Cross, true,
                    MovementCapabilityIds.Foot,
                    CellTraversalDirections.All.ToArray());
                road.TraversalDistanceCentimetres = 1_000;
                foreach (var port in road.Ports.Where(item => item.Enabled))
                {
                    port.TraversalConditionId =
                        CellTraversalIds.FormalRoadConditionId;
                    port.FormalWorldObjectId = "road.test.open";
                }
                profiles.Add(road);
            }
            var planner = Planner(grid, profiles.ToArray());
            Assert.That(planner.TryFindRoute(origin, target,
                    MovementCapabilityIds.Foot, _ => true, out var preferred,
                    out var failure), Is.True, failure);
            Assert.That(preferred.Segments.Any(item => item.FromCellId64 ==
                Id(grid, 1, 2)), Is.True);
            Assert.That(planner.TryFindRoute(origin, target,
                    MovementCapabilityIds.Foot,
                    port => port.TraversalConditionId !=
                        CellTraversalIds.FormalRoadConditionId,
                    out var fallback, out failure), Is.True, failure);
            Assert.That(fallback.Segments.Any(item => item.FromCellId64 ==
                forest.CellId64), Is.True);
        }

        [Test]
        public void CellTraversalV1_LuoyangAuditTests_CoversAllCellsFacilitiesAndAccessRules()
        {
            var plan = SharedLuoyangHumanScale.Value.Plan;
            var traversal = plan.CellTraversal;

            Assert.That(traversal, Is.Not.Null);
            Assert.That(traversal.Profiles.Count, Is.EqualTo(5_980));
            Assert.That(traversal.AssetHash.Length, Is.EqualTo(64));
            Assert.That(traversal.Profiles.Count(item =>
                !string.IsNullOrEmpty(item.FacilityId)), Is.EqualTo(2_084));
            Assert.That(traversal.Profiles.All(item =>
                item.Ports.Count == 4), Is.True);
            Assert.That(traversal.Profiles.Count(item =>
                    item.FacilityCapabilityId ==
                    FacilitySpatialCapabilityIds.Road), Is.EqualTo(359));
            Assert.That(traversal.Profiles.Count(item =>
                    item.FacilityCapabilityId ==
                    FacilitySpatialCapabilityIds.Gate), Is.EqualTo(18));
            Assert.That(traversal.Profiles.Count(item =>
                    item.FacilityCapabilityId ==
                    FacilitySpatialCapabilityIds.Bridge), Is.EqualTo(2));
            Assert.That(traversal.Profiles.Count(item =>
                    item.AccessRequirementId ==
                    FacilityAccessRequirementIds.RoadRequired),
                Is.GreaterThan(0));
            Assert.That(traversal.Profiles.Where(item =>
                    item.AccessRequirementId ==
                    FacilityAccessRequirementIds.RoadRequired)
                .All(item => item.Ports.Any(port => port.Enabled)), Is.True);
        }

        [Test]
        public void CellTraversalV1_PerformanceTests_BuildsCompactNationScaleCompatibleData()
        {
            var fixture = SharedLuoyangHumanScale.Value;
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(true);
            var timer = Stopwatch.StartNew();
            var plan = LuoyangCellTraversalRules.CreatePlan(fixture.Plan,
                fixture.StrategicRoads);
            timer.Stop();
            var memoryDelta = GC.GetTotalMemory(false) - memoryBefore;

            Assert.That(plan.Profiles.Count, Is.EqualTo(5_980));
            Assert.That(timer.ElapsedMilliseconds, Is.LessThan(5_000));
            Assert.That(memoryDelta, Is.LessThan(256L * 1024L * 1024L));
            Console.WriteLine(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "CELL_TRAVERSAL_PERF profiles={0} build_ms={1} " +
                "managed_delta={2} game_objects=0",
                plan.Profiles.Count, timer.ElapsedMilliseconds,
                memoryDelta));
        }

        private static CellTraversalPlanner Planner(CellGridIndex grid,
            params CellTraversalProfile[] profiles) =>
            new CellTraversalPlanner(new CellTraversalPlan(profiles,
                new string('a', 64)), grid);

        private static CellGridIndex Grid() => new CellGridIndex(6, 6, 0d,
            12_000d, 2_000d, "cell-traversal.test-grid.v1");

        private static ulong Id(CellGridIndex grid, int row, int column) =>
            grid.ToCellId(row, column).Value;

        private static CellTraversalProfile Profile(ulong cellId64,
            CellInternalTopology topology, bool passThrough,
            string movementCapabilityId,
            params CellTraversalDirection[] enabled) => Profile(cellId64,
            topology, passThrough, new[] { movementCapabilityId }, enabled);

        private static CellTraversalProfile Profile(ulong cellId64,
            CellInternalTopology topology, bool passThrough,
            IReadOnlyList<string> movementCapabilityIds,
            params CellTraversalDirection[] enabled)
        {
            var profile = new CellTraversalProfile
            {
                CellId64 = cellId64,
                TerrainCapabilityId = "terrain.test.open.v1",
                PassThroughAllowed = passThrough,
                InternalTopology = topology,
                TraversalDistanceCentimetres = 12_000
            };
            foreach (var capabilityId in movementCapabilityIds)
                profile.TraversalCostPermilleByCapability[capabilityId] =
                    1_000;
            foreach (var direction in CellTraversalDirections.All)
            {
                var open = enabled.Contains(direction);
                profile.Ports.Add(new CellTraversalPort
                {
                    Direction = direction,
                    Enabled = open,
                    AllowsEntry = open,
                    AllowsExit = open,
                    RoleId = open
                        ? CellTraversalPortRoleIds.TerrainBoundary
                        : CellTraversalPortRoleIds.Blocked,
                    MovementCapabilityIds = open
                        ? movementCapabilityIds.ToList()
                        : new List<string>(),
                    AdditionalDistanceCentimetres = open ? 100 : 0,
                    WidthCentimetres = open ? 400 : 0,
                    CapacityClass = open ? 2 : 0
                });
            }
            return profile;
        }
    }
}
