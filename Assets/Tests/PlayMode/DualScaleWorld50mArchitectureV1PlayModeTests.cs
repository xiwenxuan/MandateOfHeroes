using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class DualScaleWorld50mArchitectureV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "DualScaleWorld50mCountySpatialArchitectureV1");

        [UnityTest]
        public IEnumerator ValidationEntry_RendersAllTenEvidenceViewsFromOneWorld()
        {
            var root = new GameObject(
                "Dual-Scale 50m Architecture PlayMode Validation");
            var controller = root.AddComponent<
                DualScaleSpatialArchitectureValidationController>();
            Assert.That(controller.TryInitialize(), Is.True,
                controller.LastError);
            Assert.That(controller.PlanningCellCount, Is.EqualTo(6_400));
            Assert.That(controller.PlanningCellGameObjectCount, Is.Zero);
            var summary = controller.WorldSummaryHash;
            Directory.CreateDirectory(EvidenceRoot);
            yield return null;

            var views = new[]
            {
                (DualScaleValidationEvidenceView.StrategicTiles,
                    "01_dual_scale_strategic_tiles.png"),
                (DualScaleValidationEvidenceView.PlanningCells,
                    "02_planning_cells_50m.png"),
                (DualScaleValidationEvidenceView.FacilityFootprint,
                    "03_facility_physical_footprint.png"),
                (DualScaleValidationEvidenceView.FourPortTopology,
                    "04_cell_four_port_topology.png"),
                (DualScaleValidationEvidenceView.WallEdgeAndGate,
                    "05_wall_edge_and_gate.png"),
                (DualScaleValidationEvidenceView.CountyPortalRoute,
                    "06_county_portal_route.png"),
                (DualScaleValidationEvidenceView.HeightAndLosLow,
                    "07_height_and_los_low.png"),
                (DualScaleValidationEvidenceView.HeightAndLosHigh,
                    "08_height_and_los_high.png"),
                (DualScaleValidationEvidenceView.FacilityGarrisonControl,
                    "09_facility_garrison_control.png"),
                (DualScaleValidationEvidenceView.HotWarmCold,
                    "10_hot_warm_cold_debug.png")
            };
            var total = Stopwatch.StartNew();
            foreach (var item in views)
            {
                controller.ApplyEvidenceView(item.Item1);
                yield return null;
                var path = Path.Combine(EvidenceRoot, item.Item2);
                controller.CaptureEvidence(path, 1280, 720);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(1_000),
                    item.Item2);
                Assert.That(controller.WorldSummaryHash, Is.EqualTo(summary),
                    item.Item2 + " changed the authoritative world");
                if (item.Item1 ==
                    DualScaleValidationEvidenceView.HeightAndLosLow)
                    Assert.That(controller.CurrentLosVisible, Is.False);
                if (item.Item1 ==
                    DualScaleValidationEvidenceView.HeightAndLosHigh)
                    Assert.That(controller.CurrentLosVisible, Is.True);
            }
            total.Stop();

            File.WriteAllText(Path.Combine(EvidenceRoot,
                "performance-unity.json"), "{\n" +
                "  \"schemaVersion\": 1,\n" +
                $"  \"unityVersion\": \"{Application.unityVersion}\",\n" +
                $"  \"operatingSystem\": \"{Escape(SystemInfo.operatingSystem)}\",\n" +
                "  \"planningCellCount\": 6400,\n" +
                "  \"planningCellGameObjects\": 0,\n" +
                $"  \"planningCellRenderObjects\": {controller.RuntimePlanningCellRenderObjectCount},\n" +
                $"  \"chunkCount\": {controller.RuntimeChunkCount},\n" +
                $"  \"tenViewCaptureMilliseconds\": {total.Elapsed.TotalMilliseconds:F3},\n" +
                $"  \"westLoadLevel\": \"{controller.WestCountyLoadHandle.Level}\",\n" +
                $"  \"eastLoadLevel\": \"{controller.EastCountyLoadHandle.Level}\"\n" +
                "}\n");

            Object.Destroy(root);
            yield return null;
        }

        private static string Escape(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
    }
}
