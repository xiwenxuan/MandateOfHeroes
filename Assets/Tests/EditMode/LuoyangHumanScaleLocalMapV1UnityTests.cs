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

        private static LuoyangHumanScaleLocalMapPlanSource Load() =>
            new LuoyangHumanScaleLocalMapPlanSource(Path.Combine(
                Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
                "WorldMap"));
    }
}
