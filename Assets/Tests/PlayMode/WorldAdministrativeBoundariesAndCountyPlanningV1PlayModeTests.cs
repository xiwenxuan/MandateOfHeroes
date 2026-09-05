using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class
        WorldAdministrativeBoundariesAndCountyPlanningV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "WorldAdministrativeBoundariesAndCountyPlanningV1");

        [UnityTest]
        public IEnumerator PlayableDemo_UsesOneWorldForAdministrativeLodAndCountyPlanning()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            var directGame = dashboard.DirectGame;
            Assert.That(directGame, Is.Not.Null);
            Assert.That(directGame.IsActive, Is.True);
            Assert.That(directGame.ShowWorldView(), Is.True);
            yield return null;

            var map = directGame.NaturalMap;
            var world = dashboard.CurrentWorld;
            Assert.That(map, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(directGame.BoundWorld, Is.SameAs(world));
            Assert.That(map.IsReady, Is.True, map.LastError);
            map.SetAdministrativeOverlayVisible(true);
            Assert.That(map.AdministrativeOverlayVisible, Is.True);
            Assert.That(map.GetVisibleAdministrativeLabels().Count,
                Is.GreaterThan(0));
            Assert.That(map.AdministrativeBoundaryTopology, Is.Not.Null);
            Assert.That(map.AdministrativeBoundaryTopology.MappedCellCount,
                Is.EqualTo(4_647_051));
            Assert.That(map.AdministrativeBoundaryTopology.SegmentCount,
                Is.EqualTo(105_116));
            var before = WorldSnapshotSerializer.Serialize(world);
            Directory.CreateDirectory(EvidenceRoot);

            yield return CaptureGameView(
                "01_world_province_boundaries.png", 1280, 720);

            map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.CommanderyEquivalent);
            Assert.That(map.GetVisibleAdministrativeLabels().Count,
                Is.GreaterThan(0));
            yield return null;
            yield return CaptureGameView(
                "02_world_commandery_boundaries.png", 1280, 720);

            map.SetAdministrativeLabelLevel(
                AdministrativeMapLabelLevel.County);
            Assert.That(map.GetVisibleAdministrativeLabels().Count,
                Is.GreaterThan(0));
            yield return null;
            yield return CaptureGameView(
                "03_world_county_boundaries.png", 1280, 720);

            Assert.That(map.SelectAdministrativeRegion(
                "admin.han140.youzhou.zhuo.zhuo"), Is.True);
            Assert.That(map.AdministrativeSelection.Level,
                Is.EqualTo(AdministrativeRegionLevel.County));
            Assert.That(map.EnterCountyPlanning(
                map.AdministrativeSelection.RegionId), Is.True);
            yield return null;
            yield return CaptureGameView(
                "04_county_selected.png", 1280, 720);
            map.ExitCountyPlanning();

            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            Assert.That(map.SelectAdministrativeRegion(
                "admin.han140.sili.henan.luoyang"), Is.True);
            var enter = Stopwatch.StartNew();
            Assert.That(map.EnterCountyPlanning(
                map.AdministrativeSelection.RegionId), Is.True);
            enter.Stop();
            Assert.That(map.AdministrativeMapViewState.ViewMode,
                Is.EqualTo(AdministrativeMapViewMode.CountyPlanning));
            Assert.That(map.AdministrativeMapViewState.PlanningCountyId,
                Is.EqualTo("admin.han140.sili.henan.luoyang"));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
            yield return null;
            yield return CaptureGameView(
                "05_county_planning_overview.png", 1920, 1080);

            for (var step = 0; step < 8; step++)
                Assert.That(map.AdjustAdministrativeZoom(-1f,
                    new Vector2(0.5f, 0.5f)), Is.True);
            map.PanAdministrativeMap(new Vector2(0.02f, -0.01f));
            map.RotateAdministrativeMap(8f);
            Assert.That(map.AdministrativeMapViewState.ViewMode,
                Is.EqualTo(AdministrativeMapViewMode.CountyPlanning),
                "连续缩放不得自动加载另一套城市地图");
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));

            map.ExitCountyPlanning();
            Assert.That(map.SelectAdministrativeRegion(
                "admin.han140.jizhou.julu.guangzong"), Is.True);
            Assert.That(map.EnterCountyPlanning(
                map.AdministrativeSelection.RegionId), Is.True);
            yield return null;
            yield return CaptureGameView(
                "06_county_planning_neighbor_context.png", 1920, 1080);
            Assert.That(map.AdministrativeSelection.DisplayName,
                Is.EqualTo("广宗"));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));

            var fps = Time.unscaledDeltaTime > 0f
                ? 1f / Time.unscaledDeltaTime : 0f;
            File.WriteAllText(Path.Combine(EvidenceRoot,
                    "administrative_boundary_performance_v1.json"),
                PerformanceJson(map, enter.Elapsed.TotalMilliseconds, fps));
            Assert.That(map.AdministrativeBoundaryBuildMilliseconds,
                Is.GreaterThan(0d));
            Assert.That(map.AdministrativeRenderObjectCount,
                Is.GreaterThan(0));
            Assert.That(directGame.PanCameraByScreenDelta(
                new Vector2(12f, -8f), 720f), Is.True);
            Assert.That(directGame.RotateCameraByScreenDelta(
                new Vector2(8f, 0f)), Is.True);
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        private static IEnumerator CaptureGameView(string file,
            int width, int height)
        {
            // WaitForEndOfFrame is not resumed reliably by the Editor while
            // running with -batchmode. One ordinary player-loop frame is
            // sufficient because the formal Main Camera performs the render
            // and graphics readback explicitly below.
            yield return null;
            var target = new RenderTexture(width, height, 24,
                RenderTextureFormat.ARGB32);
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null, "PlayableDemo Main Camera");
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24,
                false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false);
            camera.targetTexture = previousTarget;
            var path = Path.Combine(EvidenceRoot, file);
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            var visibleSamples = 0;
            var pixels = image.GetPixels32();
            for (var index = 0; index < pixels.Length; index += 997)
            {
                var pixel = pixels[index];
                if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8)
                    visibleSamples++;
            }
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Assert.That(File.Exists(path), Is.True, file);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5000), file);
            Assert.That(visibleSamples, Is.GreaterThan(32),
                file + " must contain rendered scene pixels.");
        }

        private static string PerformanceJson(
            HanWorldNaturalMapController map,
            double planningEntryMilliseconds, float fps)
        {
            string Number(double value) => value.ToString("0.###",
                CultureInfo.InvariantCulture);
            var topology = map.AdministrativeBoundaryTopology;
            return "{\n" +
                   "  \"schema\": \"mandate.administrative-boundary-performance.v1\",\n" +
                   "  \"world_map_observed_fps\": " + Number(fps) + ",\n" +
                   "  \"boundary_build_ms\": " +
                   Number(map.AdministrativeBoundaryBuildMilliseconds) + ",\n" +
                   "  \"county_planning_entry_ms\": " +
                   Number(planningEntryMilliseconds) + ",\n" +
                   "  \"boundary_cache_bytes\": " +
                   map.AdministrativeBoundaryCacheBytes + ",\n" +
                   "  \"boundary_render_build_ms\": " +
                   Number(map.AdministrativeRenderBuildMilliseconds) + ",\n" +
                   "  \"boundary_render_gc_delta_bytes\": " +
                   map.AdministrativeRenderGcDeltaBytes + ",\n" +
                   "  \"boundary_render_objects\": " +
                   map.AdministrativeRenderObjectCount + ",\n" +
                   "  \"boundary_render_chunks\": " +
                   map.AdministrativeRenderedChunkCount + ",\n" +
                   "  \"rendered_boundary_segments\": " +
                   map.AdministrativeRenderedSegmentCount + ",\n" +
                   "  \"formal_unique_boundary_segments\": " +
                   topology.SegmentCount + "\n" +
                   "}\n";
        }
    }
}
