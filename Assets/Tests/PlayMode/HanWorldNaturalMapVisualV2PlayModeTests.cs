using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class HanWorldNaturalMapVisualV2PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE", "HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2");

        [UnityTest]
        public IEnumerator VisualV2_WorldRegionTransitionPreservesOneWorldAndCleanLayers()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldNaturalBasemap", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            Assert.That(controller.ProductionStatus,
                Is.EqualTo("HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY"));
            Assert.That(controller.UsesLegacyBackground, Is.False);
            Assert.That(controller.UsesAdministrativeOverlay, Is.True);
            Assert.That(controller.CellOverlayVisible, Is.False);
            Assert.That(controller.RuntimeTerrainObjectCount, Is.EqualTo(1));
            Assert.That(controller.IndexedTerrainTileCount, Is.EqualTo(112880));

            controller.SetHenanYinTransition(1f);
            yield return null;
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.RuntimeTerrainObjectCount, Is.EqualTo(10));
            Assert.That(controller.RuntimeRiverMeshCount, Is.EqualTo(1));
            Assert.That(controller.RuntimeVegetationObjectCount, Is.LessThanOrEqualTo(1));
            Assert.That(controller.TryPickGlobalCell(Vector3.zero, out var picked), Is.True);
            Assert.That(picked, Is.EqualTo(GlobalSpatialFoundationV1.CreateCellGrid().ToCellId(1247, 1992)));
        }

        [UnityTest]
        public IEnumerator VisualV2_CapturesFourteenGoldenCandidatesAndPerformanceEvidence()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldNaturalBasemap", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.SetCellOverlayVisible(false);
            var screenshots = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshots);

            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.WorldFull);
            yield return Capture(controller, screenshots, "01_WORLD_FULL_CLEAN.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.WorldNorthChina);
            yield return Capture(controller, screenshots, "02_WORLD_MOUNTAIN_PLAIN_READABILITY.png");
            controller.SetHenanYinTransition(0.56f);
            yield return Capture(controller, screenshots, "03_WORLD_MAJOR_RIVER_CLEAN.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.HenanRegion);
            yield return Capture(controller, screenshots, "04_HENAN_YIN_REGION_CLEAN.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.HenanMountain);
            yield return Capture(controller, screenshots, "05_HENAN_YIN_TERRAIN_RELIEF.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.HenanRiver);
            yield return Capture(controller, screenshots, "06_RIVER_CLOSE_PRESENTATION.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.HenanForest);
            yield return Capture(controller, screenshots, "07_FOREST_CLOSE_PRESENTATION.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.HenanMountain);
            yield return Capture(controller, screenshots, "08_SURFACE_BLEND_CLOSE.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.TileSeam);
            yield return Capture(controller, screenshots, "09_TILE_BOUNDARY_STRESS_TEST.png");
            controller.SetCellOverlayVisible(false);
            yield return Capture(controller, screenshots, "10_GRID_OFF_CLEAN.png");
            controller.ApplyCameraPreset(VisualAcceptanceCameraRig.WorldFull);
            yield return Capture(controller, screenshots, "11_BACKGROUND_OFF_WORLD.png");
            controller.SetHenanYinTransition(0f);
            yield return Capture(controller, screenshots, "12_WORLD_TO_REGION_START.png");
            controller.SetHenanYinTransition(0.62f);
            yield return Capture(controller, screenshots, "13_WORLD_TO_REGION_MID.png");
            controller.SetHenanYinTransition(1f);
            yield return Capture(controller, screenshots, "14_WORLD_TO_REGION_FINAL.png");

            var frameMs = Time.deltaTime * 1000f;
            var region = controller.GetPerformanceSnapshot(frameMs);
            controller.SetWorldView();
            yield return null;
            var world = controller.GetPerformanceSnapshot(Time.deltaTime * 1000f);
            File.WriteAllText(Path.Combine(EvidenceRoot, "natural_map_performance_v2.json"),
                BuildPerformanceJson(world, region), Encoding.UTF8);
            Assert.That(world.ResidentTerrainMeshes, Is.EqualTo(1));
            Assert.That(region.ResidentTerrainMeshes, Is.EqualTo(10));
            Assert.That(world.TerrainMeshBytes, Is.GreaterThan(0));
            Assert.That(region.WorldRegionTransitionMilliseconds, Is.GreaterThan(0d));
        }

        private static IEnumerator Capture(HanWorldNaturalMapController controller, string root, string file)
        {
            yield return null;
            var path = Path.Combine(root, file);
            controller.CaptureEvidence(path);
            Assert.That(File.Exists(path), Is.True, file);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5000), file);
        }

        private static string BuildPerformanceJson(NaturalMapPerformanceSnapshot world,
            NaturalMapPerformanceSnapshot region)
        {
            string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
            return "{\n" +
                   "  \"schema\": \"hanworld.natural-map-performance.v2\",\n" +
                   "  \"world_mode_frame_time_ms\": " + Number(world.ObservedFrameMilliseconds) + ",\n" +
                   "  \"region_mode_frame_time_ms\": " + Number(region.ObservedFrameMilliseconds) + ",\n" +
                   "  \"world_terrain_generation_ms\": " + Number(world.TerrainGenerationMilliseconds) + ",\n" +
                   "  \"region_terrain_generation_ms\": " + Number(region.TerrainGenerationMilliseconds) + ",\n" +
                   "  \"world_terrain_resident_count\": " + world.ResidentTerrainMeshes + ",\n" +
                   "  \"region_terrain_resident_count\": " + region.ResidentTerrainMeshes + ",\n" +
                   "  \"world_terrain_memory_bytes\": " + world.TerrainMeshBytes + ",\n" +
                   "  \"region_terrain_memory_bytes\": " + region.TerrainMeshBytes + ",\n" +
                   "  \"vegetation_draw_batches\": " + region.VegetationDrawBatches + ",\n" +
                   "  \"river_mesh_count\": " + region.RiverMeshCount + ",\n" +
                   "  \"managed_gc_delta_bytes\": " + region.ManagedGcDeltaBytes + ",\n" +
                   "  \"world_region_transition_ms\": " + Number(region.WorldRegionTransitionMilliseconds) + "\n" +
                   "}\n";
        }
    }
}
