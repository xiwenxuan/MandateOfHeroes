using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class StyleDStrategicLandscapeV2PlayModeTests
    {
        private static string EvidenceRoot
        {
            get
            {
                var configured = Environment.GetEnvironmentVariable("MANDATE_STYLE_D_V2_EVIDENCE_ROOT");
                return string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
                        "HAN_WORLD_STYLE_D_STRATEGIC_LANDSCAPE_VISUAL_REFINEMENT_V2", "outputs", "local")
                    : configured;
            }
        }

        [UnityTest]
        public IEnumerator StyleDV2_CapturesFrozenV1V2ComparisonAndLandscapeLods()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab", LoadSceneMode.Single);
            yield return null;
            var controller = UnityEngine.Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            controller.SetArtStyle(HanWorldArtStyle.ZhonghuaSanguozhiFusion);
            controller.SetPresentationUiVisible(false);
            controller.SetCellOverlayVisible(false);
            var screenshots = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshots);
            CopyV1("01_CURRENT_WORLD_REFERENCE.png", "01_STYLE_D_V1_WORLD.png", screenshots);
            CopyV1("03_CURRENT_REGION_REFERENCE.png", "03_STYLE_D_V1_REGION.png", screenshots);
            var snapshots = new List<NaturalMapPerformanceSnapshot>();

            yield return Capture(controller, ZhonghuaFusionCameraRig.World,
                "02_STYLE_D_V2_WORLD.png", screenshots, snapshots);
            yield return Capture(controller, ZhonghuaFusionCameraRig.Region,
                "04_STYLE_D_V2_REGION.png", screenshots, snapshots);
            yield return Capture(controller, ZhonghuaFusionCameraRig.CityDistance,
                "05_STYLE_D_V2_CITY_DISTANCE.png", screenshots, snapshots);
            Assert.That(controller.VisualDetailLevel, Is.EqualTo(VisualTerrainDetailLevel.City));
            Assert.That(controller.LastVisualTerrainVertexCount, Is.GreaterThan(30000));
            yield return Capture(controller, ZhonghuaFusionCameraRig.Mountain,
                "06_STYLE_D_V2_MOUNTAIN.png", screenshots, snapshots);
            yield return Capture(controller, ZhonghuaFusionCameraRig.RiverGentle,
                "07_STYLE_D_V2_RIVER_GENTLE.png", screenshots, snapshots);
            yield return Capture(controller, ZhonghuaFusionCameraRig.RiverSharpBend,
                "08_STYLE_D_V2_RIVER_SHARP_BEND.png", screenshots, snapshots);
            Assert.That(controller.LastRiverDiagnostics.InvalidTriangleCount, Is.Zero);
            Assert.That(controller.LastRiverDiagnostics.NaNVertexCount, Is.Zero);
            Assert.That(controller.LastRiverDiagnostics.ExtremeMiterCount, Is.Zero);
            yield return Capture(controller, ZhonghuaFusionCameraRig.ForestWorld,
                "09_STYLE_D_V2_FOREST_WORLD.png", screenshots, snapshots);
            Assert.That(controller.RuntimeVegetationObjectCount, Is.Zero);
            yield return Capture(controller, ZhonghuaFusionCameraRig.ForestRegion,
                "10_STYLE_D_V2_FOREST_REGION.png", screenshots, snapshots);
            Assert.That(controller.RuntimeVegetationObjectCount, Is.EqualTo(1));
            yield return Capture(controller, ZhonghuaFusionCameraRig.ForestCity,
                "11_STYLE_D_V2_FOREST_CITY.png", screenshots, snapshots);
            Assert.That(controller.RuntimeVegetationObjectCount, Is.EqualTo(1));
            yield return Capture(controller, ZhonghuaFusionCameraRig.Plain,
                "12_STYLE_D_V2_PLAIN.png", screenshots, snapshots);
            yield return Capture(controller, ZhonghuaFusionCameraRig.TerrainDetail,
                "13_STYLE_D_V2_TERRAIN_DETAIL.png", screenshots, snapshots);
            Assert.That(controller.VisualDetailLevel, Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
            Assert.That(controller.LastVisualTerrainVertexCount, Is.GreaterThan(100000));
            yield return Capture(controller, ZhonghuaFusionCameraRig.WorldToCityMid,
                "14_STYLE_D_V2_WORLD_TO_CITY_MID.png", screenshots, snapshots);
            controller.SetCellOverlayVisible(false);
            yield return Capture(controller, ZhonghuaFusionCameraRig.GridOff,
                "15_STYLE_D_V2_BACKGROUND_GRID_OFF.png", screenshots, snapshots);

            var riverDiagnostics = Path.Combine(EvidenceRoot, "RiverDiagnostics");
            Directory.CreateDirectory(riverDiagnostics);
            yield return CaptureDiagnostic(controller, ZhonghuaFusionCameraRig.RiverStraight,
                "RIVER_STRAIGHT.png", riverDiagnostics);
            yield return CaptureDiagnostic(controller, ZhonghuaFusionCameraRig.RiverGentle,
                "RIVER_GENTLE_BEND.png", riverDiagnostics);
            yield return CaptureDiagnostic(controller, ZhonghuaFusionCameraRig.RiverSharpBend,
                "RIVER_SHARP_BEND.png", riverDiagnostics);
            yield return CaptureDiagnostic(controller, ZhonghuaFusionCameraRig.RiverConfluence,
                "RIVER_CONFLUENCE.png", riverDiagnostics);
            yield return CaptureDiagnostic(controller, ZhonghuaFusionCameraRig.RiverBankClose,
                "RIVER_BANK_CLOSE.png", riverDiagnostics);

            controller.SetStyleDWorldToCityTransition(0f);
            Assert.That(controller.VisualDetailLevel, Is.EqualTo(VisualTerrainDetailLevel.World));
            controller.SetStyleDWorldToCityTransition(0.70f);
            Assert.That(controller.VisualDetailLevel, Is.EqualTo(VisualTerrainDetailLevel.Region));
            controller.SetStyleDWorldToCityTransition(1f);
            Assert.That(controller.VisualDetailLevel, Is.EqualTo(VisualTerrainDetailLevel.City));

            Assert.That(controller.CellOverlayVisible, Is.False);
            Assert.That(controller.UsesLegacyBackground, Is.False);
            Assert.That(controller.ProductionStatus,
                Is.EqualTo("STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW"));
            Assert.That(Directory.GetFiles(screenshots, "*.png"), Has.Length.EqualTo(15));
            File.WriteAllText(Path.Combine(EvidenceRoot, "style_d_v2_performance.json"),
                BuildPerformanceJson(snapshots), Encoding.UTF8);
        }

        private static IEnumerator CaptureDiagnostic(HanWorldNaturalMapController controller, string cameraId,
            string fileName, string directory)
        {
            controller.ApplyZhonghuaFusionCamera(cameraId);
            yield return null;
            yield return null;
            var path = Path.Combine(directory, fileName);
            controller.CaptureEvidence(path, 1280, 720);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5000), fileName);
            AssertVisualContent(path);
        }

        private static void CopyV1(string sourceName, string targetName, string targetDirectory)
        {
            var source = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
                "HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1", "outputs",
                "20260816-1732-style-d", "Screenshots", sourceName);
            Assert.That(File.Exists(source), Is.True, "Missing frozen Style D V1 evidence: " + source);
            var target = Path.Combine(targetDirectory, targetName);
            File.Copy(source, target, true);
            Assert.That(new FileInfo(target).Length, Is.GreaterThan(5000));
        }

        private static IEnumerator Capture(HanWorldNaturalMapController controller, string cameraId,
            string fileName, string directory, ICollection<NaturalMapPerformanceSnapshot> snapshots)
        {
            controller.ApplyZhonghuaFusionCamera(cameraId);
            yield return null;
            yield return null;
            var path = Path.Combine(directory, fileName);
            controller.CaptureEvidence(path, 1280, 720);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5000), fileName);
            AssertVisualContent(path);
            snapshots.Add(controller.GetPerformanceSnapshot(Time.deltaTime * 1000f));
        }

        private static void AssertVisualContent(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            var pixels = image.GetPixels32();
            var min = 255;
            var max = 0;
            var distinct = new HashSet<int>();
            var step = Math.Max(1, pixels.Length / 4096);
            for (var index = 0; index < pixels.Length; index += step)
            {
                var pixel = pixels[index];
                var luminance = (pixel.r * 299 + pixel.g * 587 + pixel.b * 114) / 1000;
                min = Math.Min(min, luminance);
                max = Math.Max(max, luminance);
                distinct.Add((pixel.r / 8 << 10) | (pixel.g / 8 << 5) | pixel.b / 8);
            }
            UnityEngine.Object.DestroyImmediate(image);
            Assert.That(max - min, Is.GreaterThan(18), "Flat screenshot: " + path);
            Assert.That(distinct.Count, Is.GreaterThan(24), "Insufficient visual detail: " + path);
        }

        private static string BuildPerformanceJson(IReadOnlyCollection<NaturalMapPerformanceSnapshot> values)
        {
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"hanworld.style-d-strategic-landscape-performance.v2\",");
            text.AppendLine("  \"cell_size_metres\": 2000,");
            text.AppendLine("  \"creates_simulation_subcells\": false,");
            text.AppendLine("  \"gpu_timing_note\": \"0 means unavailable in controlled batch capture\",");
            text.AppendLine("  \"samples\": [");
            var index = 0;
            foreach (var value in values)
            {
                text.Append("    {\"view\": \"").Append(value.Mode)
                    .Append("\", \"detail\": \"").Append(value.VisualDetailLevel)
                    .Append("\", \"frame_ms\": ").Append(N(value.ObservedFrameMilliseconds))
                    .Append(", \"terrain_generation_ms\": ").Append(N(value.TerrainGenerationMilliseconds))
                    .Append(", \"terrain_vertices\": ").Append(value.VisualTerrainVertices)
                    .Append(", \"terrain_mesh_bytes\": ").Append(value.TerrainMeshBytes)
                    .Append(", \"river_adaptive_samples\": ").Append(value.RiverAdaptiveSamples)
                    .Append(", \"river_bevel_fallbacks\": ").Append(value.RiverBevelFallbacks)
                    .Append(", \"draw_calls\": ").Append(value.DrawCalls)
                    .Append(", \"vegetation_batches\": ").Append(value.VegetationDrawBatches).Append('}');
                text.AppendLine(++index == values.Count ? string.Empty : ",");
            }
            text.AppendLine("  ]");
            text.AppendLine("}");
            return text.ToString();
        }

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
