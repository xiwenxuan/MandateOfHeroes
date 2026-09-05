using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class LuoyangHumanScaleLocalMapV1UnityTests
    {
        [Test]
        public void LocalMapLoadAndStreaming_CreatesNineCellPresentationWindow()
        {
            var source = Load();
            var center = source.Plan.LocalSpaces.First(item =>
                item.GridColumn > LuoyangHumanScaleLocalMapIds.MapMinColumn &&
                item.GridColumn < LuoyangHumanScaleLocalMapIds.MapMaxColumn &&
                item.GridRow > LuoyangHumanScaleLocalMapIds.MapMinRow &&
                item.GridRow < LuoyangHumanScaleLocalMapIds.MapMaxRow);
            var origin = new GlobalProjectedCoordinate(
                center.OriginEastingMetres + center.WidthMetres * 0.5d,
                center.OriginNorthingMetres + center.HeightMetres * 0.5d);
            Vector3 Resolve(double east, double north)
            {
                var value = source.Plan.WorldScale.WorldToUnity(
                    new GlobalProjectedCoordinate(east, north), 0d, origin);
                return new Vector3((float)value.XMetres, 0f,
                    (float)value.ZMetres);
            }

            var memoryBefore = GC.GetTotalMemory(true);
            var timer = Stopwatch.StartNew();
            var runtime = LuoyangHumanScaleStreamingRuntime.Build(source.Plan,
                Resolve, center.ParentCellId64);
            timer.Stop();
            try
            {
                Assert.That(runtime.ResidentCellCount, Is.EqualTo(9));
                Assert.That(runtime.ResidentGameObjectCount,
                    Is.GreaterThan(9));
                Assert.That(runtime.ResidentMeshCount, Is.GreaterThan(0));
                Assert.That(runtime.ResidentColliderCount,
                    Is.GreaterThanOrEqualTo(9));
                Assert.That(runtime.DebugCellGroundVisible, Is.True);
                Assert.That(runtime.Root.GetComponentsInChildren<Transform>()
                    .Any(item => item.name.StartsWith("LOCAL_TERRAIN_",
                        StringComparison.Ordinal)), Is.True);
                Assert.That(runtime.MapAssetHash,
                    Is.EqualTo(source.Plan.AssetHash));
                var next = source.Plan.LocalSpaces.Single(item =>
                    item.GridColumn == center.GridColumn + 1 &&
                    item.GridRow == center.GridRow);
                var update = runtime.MoveWindow(next.ParentCellId64);
                Assert.That(update.LoadedCellIds.Count, Is.EqualTo(3));
                Assert.That(update.UnloadedCellIds.Count, Is.EqualTo(3));
                Assert.That(runtime.ResidentCellCount, Is.EqualTo(9));
                UnityEngine.Debug.Log(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "LOCAL_MAP_PERF load_ms={0} update_load_ms={1} " +
                    "update_unload_ms={2} objects={3} meshes={4} " +
                    "colliders={5} managed_delta={6}", timer.ElapsedMilliseconds,
                    runtime.LastLoadMilliseconds,
                    runtime.LastUnloadMilliseconds,
                    runtime.ResidentGameObjectCount,
                    runtime.ResidentMeshCount,
                    runtime.ResidentColliderCount,
                    GC.GetTotalMemory(false) - memoryBefore));
            }
            finally
            {
                runtime.Dispose();
            }
            Assert.That(GameObject.Find(
                LuoyangHumanScaleStreamingRuntime.RootName), Is.Null);
        }

        [Test]
        public void LocalMapTargets_CreateFacilityGateAndBridgeClickProxies()
        {
            var source = Load();
            var access = source.Plan.Entrances.First(item =>
                source.Plan.FacilityCapabilitiesByFacilityId[item.FacilityId]
                    .CapabilityId == FacilitySpatialCapabilityIds.Gate);
            var space = source.Plan.LocalSpacesByCellId[access.CellId64];
            var origin = new GlobalProjectedCoordinate(
                space.OriginEastingMetres + 1_000d,
                space.OriginNorthingMetres + 1_000d);
            Vector3 Resolve(double east, double north)
            {
                var value = source.Plan.WorldScale.WorldToUnity(
                    new GlobalProjectedCoordinate(east, north), 0d, origin);
                return new Vector3((float)value.XMetres, 0f,
                    (float)value.ZMetres);
            }
            var runtime = LuoyangHumanScaleStreamingRuntime.Build(source.Plan,
                Resolve, access.CellId64);
            try
            {
                var proxies = runtime.Root.GetComponentsInChildren<
                    LuoyangLocalTargetProxy>(true);
                Assert.That(proxies, Is.Not.Empty);
                Assert.That(proxies.Any(item => item.FacilityId ==
                    access.FacilityId), Is.True);
                var proxy = proxies.First(item =>
                    item.GetComponentInChildren<Collider>(true) != null);
                var collider = proxy.GetComponentInChildren<Collider>(true);
                Assert.That(runtime.TryResolveProxy(collider, out var target),
                    Is.True);
                Assert.That(target.FacilityId, Is.EqualTo(proxy.FacilityId));
            }
            finally
            {
                runtime.Dispose();
            }
        }

        [Test]
        public void PlayerNearfield_BuildsCompactStreetContextWithoutDebugCells()
        {
            var source = Load();
            const string focusFacilityId =
                PlayableLuoyangWorldContractIds.MarketFacilityId;
            var footprint = source.Plan.FootprintsByFacilityId[
                focusFacilityId];
            var space = source.Plan.LocalSpacesByCellId[footprint.CellId64];
            var origin = new GlobalProjectedCoordinate(
                space.OriginEastingMetres + footprint.CenterEastMetres,
                space.OriginNorthingMetres + footprint.CenterNorthMetres);
            Vector3 Resolve(double east, double north)
            {
                var value = source.Plan.WorldScale.WorldToUnity(
                    new GlobalProjectedCoordinate(east, north), 0d, origin);
                return new Vector3((float)value.XMetres, 0f,
                    (float)value.ZMetres);
            }

            var runtime = LuoyangHumanScaleStreamingRuntime.Build(source.Plan,
                Resolve, footprint.CellId64, null, false,
                LuoyangNearfieldVisualOptions.PlayerDefault(),
                focusFacilityId);
            try
            {
                Assert.That(runtime.DebugCellGroundVisible, Is.False);
                Assert.That(runtime.NearfieldContextFacilityCount,
                    Is.EqualTo(9));
                Assert.That(runtime.NearfieldContextStableSummary,
                    Is.Not.EqualTo(0UL));
                Assert.That(GameObject.Find(
                    "LUOYANG_NEARFIELD_URBAN_CONTEXT_V1"), Is.Not.Null);
                Assert.That(runtime.Root.GetComponentsInChildren<Transform>()
                    .Count(item => item.name.StartsWith(
                        "NEARFIELD_CONTEXT_FACILITY_",
                        StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(8));
                Assert.That(runtime.Root.GetComponentsInChildren<Transform>()
                    .Any(item => item.name ==
                        "NEARFIELD_STREET_EAST_WEST"), Is.True);
                Assert.That(runtime.Root.GetComponentsInChildren<Transform>()
                    .Any(item => item.name.StartsWith("LOCAL_TERRAIN_",
                        StringComparison.Ordinal)), Is.False);
            }
            finally
            {
                runtime.Dispose();
            }
        }

        [Test]
        public void CellTraversalPresentation_ExpandsFormalRouteThroughCardinalPortAnchors()
        {
            var source = Load();
            var planner = new LuoyangHumanScaleLocalRoutePlanner(source.Plan);
            var facilities = source.Plan.FacilityCapabilities.Where(item =>
                    item.RequiresAccess && item.CapabilityId !=
                    FacilitySpatialCapabilityIds.Gate && item.CapabilityId !=
                    FacilitySpatialCapabilityIds.Bridge)
                .OrderBy(item => item.CellId64).Take(300).ToArray();
            LuoyangHumanScaleLocalRoute route = null;
            for (var index = 1; index < facilities.Length && route == null;
                 index++)
                planner.TryFindRoute(facilities[0].FacilityId,
                    facilities[index].FacilityId, _ => true, _ => true,
                    out route, out _);

            Assert.That(route, Is.Not.Null);
            Assert.That(route.CellRoute, Is.Not.Null);
            Assert.That(route.Points.Count,
                Is.EqualTo(route.Edges.Count + 1));
            Assert.That(route.Edges.Any(item =>
                item.CrossesStrategicCellBoundary), Is.True);
            foreach (var edge in route.Edges.Where(item =>
                         item.CrossesStrategicCellBoundary))
            {
                Assert.That(edge.Geometry.Count, Is.EqualTo(2));
                Assert.That(edge.Geometry[0].GlobalEastingMetres,
                    Is.EqualTo(edge.Geometry[1].GlobalEastingMetres)
                        .Within(0.001d));
                Assert.That(edge.Geometry[0].GlobalNorthingMetres,
                    Is.EqualTo(edge.Geometry[1].GlobalNorthingMetres)
                        .Within(0.001d));
            }
            var visitedCells = route.Points.Select(item => item.CellId64)
                .Distinct().Count();
            Assert.That(route.DistanceCentimetres,
                Is.LessThan((long)visitedCells * 200_000L));
        }

        [Test]
        public void SupplyFreightPresentation_ShowsCarrierWaitingAndArrivalReadOnly()
        {
            var world = WorldState.Create(184);
            var freight = new CivilianFreightState
            {
                Id = "civilian_freight.presentation.v1",
                CarrierPersonId = "person.presentation.carrier",
                ProductDefinitionId =
                    CoreProductionContent.WheatGrainProductId,
                Status = CivilianFreightStatus.InTransit,
                UsesCellRoute = true,
                CellRouteMovementCapabilityId =
                    MovementCapabilityIds.Cart,
                CellRouteCurrentCellId64 = 4_114_717,
                RemainingCargoQuantity = 80,
                CellRouteWaiting = true,
                CellRouteWaitingOnFormalWorldObjectId =
                    "facility.instance.luoyang.184.gate.gumen",
                CellRouteRevision = 2
            };
            world.CivilianFreights.Add(freight);
            var originalQuantity = freight.RemainingCargoQuantity;
            var originalCell = freight.CellRouteCurrentCellId64;
            var runtime = LuoyangSupplyFreightPresentationRuntime.Build(
                world,
                cellId => new Vector3(cellId == originalCell ? 2f : 0f,
                    0f, 3f));
            try
            {
                Assert.That(runtime.LoadedMarkerCount, Is.EqualTo(1));
                var marker = runtime.Markers[freight.Id];
                Assert.That(marker.CarrierPersonId,
                    Is.EqualTo(freight.CarrierPersonId));
                Assert.That(marker.PresentationStateId, Is.EqualTo(
                    LuoyangSupplyFreightPresentationIds
                        .WaitingAtPassageStateId));
                Assert.That(marker.WaitingOnFormalWorldObjectId,
                    Is.EqualTo(freight
                        .CellRouteWaitingOnFormalWorldObjectId));
                Assert.That(marker.transform.position.x, Is.EqualTo(2f));
                Assert.That(freight.RemainingCargoQuantity,
                    Is.EqualTo(originalQuantity));
                Assert.That(freight.CellRouteCurrentCellId64,
                    Is.EqualTo(originalCell));

                freight.Status = CivilianFreightStatus.Completed;
                freight.CellRouteWaiting = false;
                freight.CellRouteWaitingOnFormalWorldObjectId = string.Empty;
                runtime.Refresh(world);
                Assert.That(marker.PresentationStateId, Is.EqualTo(
                    LuoyangSupplyFreightPresentationIds.ArrivedStateId));
                Assert.That(marker.RemainingCargoQuantity,
                    Is.EqualTo(originalQuantity));
            }
            finally
            {
                runtime.Dispose();
            }
            Assert.That(GameObject.Find(
                LuoyangSupplyFreightPresentationIds.RootName), Is.Null);
        }

        private static LuoyangHumanScaleLocalMapPlanSource Load() =>
            new LuoyangHumanScaleLocalMapPlanSource(Path.Combine(
                Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
                "WorldMap"));
    }
}
