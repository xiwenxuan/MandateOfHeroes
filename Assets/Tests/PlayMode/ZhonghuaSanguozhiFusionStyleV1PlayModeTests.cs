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
    public sealed class ZhonghuaSanguozhiFusionStyleV1PlayModeTests
    {
        private static string EvidenceRoot
        {
            get
            {
                var configured = Environment.GetEnvironmentVariable("MANDATE_STYLE_D_EVIDENCE_ROOT");
                return string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
                        "HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1", "outputs", "local")
                    : configured;
            }
        }

        [UnityTest]
        public IEnumerator StyleD_CapturesRequiredSameCameraComparisonsAndMacroFeatures()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab", LoadSceneMode.Single);
            yield return null;
            var controller = UnityEngine.Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.SetCellOverlayVisible(false);
            var screenshots = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshots);
            var snapshots = new List<NaturalMapPerformanceSnapshot>();

            yield return Capture(controller, HanWorldArtStyle.ChineseSemiRealistic,
                ZhonghuaFusionCameraRig.World, "01_CURRENT_WORLD_REFERENCE.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.World, "02_STYLE_D_WORLD.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ChineseSemiRealistic,
                ZhonghuaFusionCameraRig.Region, "03_CURRENT_REGION_REFERENCE.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.Region, "04_STYLE_D_REGION.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.Mountain, "05_STYLE_D_MOUNTAIN.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.River, "06_STYLE_D_RIVER.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.Forest, "07_STYLE_D_FOREST.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.Plain, "08_STYLE_D_PLAIN.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.WorldToRegionMid, "09_STYLE_D_WORLD_TO_REGION_MID.png", screenshots, snapshots);
            yield return Capture(controller, HanWorldArtStyle.ZhonghuaSanguozhiFusion,
                ZhonghuaFusionCameraRig.CityDistancePreview, "10_STYLE_D_CITY_DISTANCE_PREVIEW.png", screenshots, snapshots);

            Assert.That(controller.ProductionStatus,
                Is.EqualTo("STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW"));
            Assert.That(controller.LastFusionFeatureVertexCount, Is.GreaterThan(0));
            Assert.That(controller.RuntimeVegetationObjectCount, Is.GreaterThan(0),
                "Style D V2 city distance must add a batched individual-tree presentation layer.");
            Assert.That(File.ReadAllBytes(Path.Combine(screenshots, "01_CURRENT_WORLD_REFERENCE.png")),
                Is.Not.EqualTo(File.ReadAllBytes(Path.Combine(screenshots, "02_STYLE_D_WORLD.png"))));
            Assert.That(File.ReadAllBytes(Path.Combine(screenshots, "03_CURRENT_REGION_REFERENCE.png")),
                Is.Not.EqualTo(File.ReadAllBytes(Path.Combine(screenshots, "04_STYLE_D_REGION.png"))));
            File.WriteAllText(Path.Combine(EvidenceRoot, "style_d_performance.json"),
                BuildPerformanceJson(snapshots), Encoding.UTF8);
        }

        private static IEnumerator Capture(HanWorldNaturalMapController controller,
            HanWorldArtStyle style, string cameraId, string fileName, string screenshots,
            ICollection<NaturalMapPerformanceSnapshot> snapshots)
        {
            controller.SetArtStyle(style);
            controller.ApplyZhonghuaFusionCamera(cameraId);
            yield return null;
            yield return null;
            var path = Path.Combine(screenshots, fileName);
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
            text.AppendLine("  \"schema\": \"hanworld.style-d-performance.v1\",");
            text.AppendLine("  \"gpu_timing_note\": \"0 means unavailable in controlled batch Game View capture\",");
            text.AppendLine("  \"samples\": [");
            var index = 0;
            foreach (var value in values)
            {
                text.Append("    {\"profile_id\": \"").Append(value.ArtProfileId)
                    .Append("\", \"view\": \"").Append(value.Mode)
                    .Append("\", \"frame_ms\": ").Append(N(value.ObservedFrameMilliseconds))
                    .Append(", \"terrain_generation_ms\": ").Append(N(value.TerrainGenerationMilliseconds))
                    .Append(", \"draw_calls\": ").Append(value.DrawCalls)
                    .Append(", \"terrain_mesh_bytes\": ").Append(value.TerrainMeshBytes)
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
