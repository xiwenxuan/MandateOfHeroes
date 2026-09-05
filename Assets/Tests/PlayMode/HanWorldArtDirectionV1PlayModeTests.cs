using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class HanWorldArtDirectionV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE", "HAN_WORLD_NATURAL_MAP_ART_DIRECTION_AND_RENDERING_V1");

        [UnityTest]
        public IEnumerator ArtDirection_ProfilesSwitchWithoutChangingAuthoritativeWorld()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab", LoadSceneMode.Single);
            yield return null;
            var controller = UnityEngine.Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            controller.ApplyArtSampleCamera(ArtDirectionSample.CentralPlain, HanNaturalMapView.Region);
            Assert.That(controller.TryPickGlobalCell(Vector3.zero, out var before), Is.True);
            var origin = controller.FloatingOrigin;
            foreach (var style in Styles)
            {
                controller.SetArtStyle(style);
                controller.ApplyArtSampleCamera(ArtDirectionSample.CentralPlain, HanNaturalMapView.Region);
                Assert.That(controller.TryPickGlobalCell(Vector3.zero, out var after), Is.True);
                Assert.That(after, Is.EqualTo(before));
                Assert.That(controller.FloatingOrigin.EastingMetres, Is.EqualTo(origin.EastingMetres));
                Assert.That(controller.FloatingOrigin.NorthingMetres, Is.EqualTo(origin.NorthingMetres));
                Assert.That(controller.IndexedTerrainTileCount, Is.EqualTo(112880));
                Assert.That(controller.CellOverlayVisible, Is.False);
                Assert.That(controller.UsesLegacyBackground, Is.False);
            }
            Assert.That(controller.ProductionStatus,
                Is.EqualTo("HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY"));
        }

        [UnityTest]
        public IEnumerator ArtDirection_CapturesEighteenGameViewsThreeComparisonsAndPerformance()
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

            foreach (var style in Styles)
            {
                controller.SetArtStyle(style);
                foreach (var sample in Samples)
                foreach (var view in Views)
                {
                    controller.ApplyArtSampleCamera(sample, view);
                    yield return null;
                    yield return null;
                    var file = StyleCode(style) + "_" + SampleCode(sample) + "_" +
                               view.ToString().ToUpperInvariant() + ".png";
                    controller.CaptureEvidence(Path.Combine(screenshots, file), 1280, 720);
                    var info = new FileInfo(Path.Combine(screenshots, file));
                    Assert.That(info.Exists, Is.True, file);
                    Assert.That(info.Length, Is.GreaterThan(5000), file);
                    AssertVisualContent(Path.Combine(screenshots, file));
                    snapshots.Add(controller.GetPerformanceSnapshot(Time.deltaTime * 1000f));
                }
            }

            for (var index = 0; index < Samples.Length; index++)
            {
                var sampleCode = SampleCode(Samples[index]);
                var output = Path.Combine(screenshots,
                    (index + 1).ToString("00") + "_" + sampleCode + "_STYLE_COMPARISON.png");
                BuildComparison(output,
                    Path.Combine(screenshots, "STYLE_A_" + sampleCode + "_REGION.png"),
                    Path.Combine(screenshots, "STYLE_B_" + sampleCode + "_REGION.png"),
                    Path.Combine(screenshots, "STYLE_C_" + sampleCode + "_REGION.png"));
                Assert.That(new FileInfo(output).Length, Is.GreaterThan(12000), output);
            }

            File.WriteAllText(Path.Combine(EvidenceRoot, "style_performance_comparison.json"),
                BuildPerformanceJson(snapshots), Encoding.UTF8);
            Assert.That(snapshots, Has.Count.EqualTo(18));
            Assert.That(snapshots.TrueForAll(value => value.MaterialCount == 4), Is.True);
            Assert.That(snapshots.TrueForAll(value => value.ShaderVariantCount == 1), Is.True);
            Assert.That(File.ReadAllBytes(Path.Combine(screenshots, "STYLE_A_SAMPLE_A_REGION.png")),
                Is.Not.EqualTo(File.ReadAllBytes(Path.Combine(screenshots, "STYLE_B_SAMPLE_A_REGION.png"))));
            Assert.That(File.ReadAllBytes(Path.Combine(screenshots, "STYLE_B_SAMPLE_A_REGION.png")),
                Is.Not.EqualTo(File.ReadAllBytes(Path.Combine(screenshots, "STYLE_C_SAMPLE_A_REGION.png"))));
        }

        private static readonly HanWorldArtStyle[] Styles =
        {
            HanWorldArtStyle.RealisticNatural,
            HanWorldArtStyle.ChineseSemiRealistic,
            HanWorldArtStyle.StrategicSandbox
        };

        private static readonly ArtDirectionSample[] Samples =
        {
            ArtDirectionSample.CentralPlain,
            ArtDirectionSample.MountainRiver,
            ArtDirectionSample.ForestHills
        };

        private static readonly HanNaturalMapView[] Views =
            { HanNaturalMapView.World, HanNaturalMapView.Region };

        private static string StyleCode(HanWorldArtStyle style) => style == HanWorldArtStyle.RealisticNatural
            ? "STYLE_A" : style == HanWorldArtStyle.ChineseSemiRealistic ? "STYLE_B" : "STYLE_C";

        private static string SampleCode(ArtDirectionSample sample) => sample == ArtDirectionSample.CentralPlain
            ? "SAMPLE_A" : sample == ArtDirectionSample.MountainRiver ? "SAMPLE_B" : "SAMPLE_C";

        private static void BuildComparison(string output, params string[] inputs)
        {
            const int width = 1280;
            const int height = 720;
            var canvas = new Texture2D(width * inputs.Length, height, TextureFormat.RGB24, false);
            canvas.SetPixels32(new Color32[width * inputs.Length * height]);
            for (var index = 0; index < inputs.Length; index++)
            {
                var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                Assert.That(image.LoadImage(File.ReadAllBytes(inputs[index])), Is.True, inputs[index]);
                canvas.SetPixels32(index * width, 0, width, height, image.GetPixels32());
                UnityEngine.Object.DestroyImmediate(image);
            }
            canvas.Apply();
            File.WriteAllBytes(output, canvas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(canvas);
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
            Assert.That(max - min, Is.GreaterThan(18), "Screenshot is a flat background: " + path);
            Assert.That(distinct.Count, Is.GreaterThan(24), "Screenshot lacks rendered terrain detail: " + path);
        }

        private static string BuildPerformanceJson(IReadOnlyList<NaturalMapPerformanceSnapshot> values)
        {
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"hanworld.art-direction-performance.v1\",");
            text.AppendLine("  \"gpu_timing_note\": \"0 means unavailable in headless/batch Game View capture\",");
            text.AppendLine("  \"samples\": [");
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                text.Append("    {\"profile_id\": \"").Append(value.ArtProfileId)
                    .Append("\", \"view\": \"").Append(value.Mode)
                    .Append("\", \"cpu_frame_ms\": ").Append(N(value.CpuFrameMilliseconds))
                    .Append(", \"gpu_frame_ms\": ").Append(N(value.GpuFrameMilliseconds))
                    .Append(", \"observed_frame_ms\": ").Append(N(value.ObservedFrameMilliseconds))
                    .Append(", \"draw_calls\": ").Append(value.DrawCalls)
                    .Append(", \"material_count\": ").Append(value.MaterialCount)
                    .Append(", \"shader_variants\": ").Append(value.ShaderVariantCount)
                    .Append(", \"terrain_mesh_bytes\": ").Append(value.TerrainMeshBytes)
                    .Append(", \"vegetation_batches\": ").Append(value.VegetationDrawBatches)
                    .Append(", \"river_meshes\": ").Append(value.RiverMeshCount).Append('}');
                text.AppendLine(index + 1 == values.Count ? string.Empty : ",");
            }
            text.AppendLine("  ]");
            text.AppendLine("}");
            return text.ToString();
        }

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
