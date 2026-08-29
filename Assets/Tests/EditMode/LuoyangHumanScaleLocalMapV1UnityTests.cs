using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
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
                var proxy = proxies.First();
                Assert.That(runtime.TryResolveProxy(
                    proxy.GetComponent<Collider>(), out var target), Is.True);
                Assert.That(target.FacilityId, Is.EqualTo(proxy.FacilityId));
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

        private static LuoyangHumanScaleLocalMapPlanSource Load() =>
            new LuoyangHumanScaleLocalMapPlanSource(Path.Combine(
                Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
                "WorldMap"));
    }
}
