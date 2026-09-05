using System.Collections;
using System.Globalization;
using System.IO;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class Luoyang50mCountySpatialPrototypeV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "Luoyang50mCountySpatialPrototypeV1");

        [UnityTest]
        public IEnumerator FullScalePrototypeRendersFourEvidenceViewsWithOneMapObject()
        {
            var root = new GameObject("Luoyang 50m Prototype PlayMode Test");
            var controller = root.AddComponent<
                Luoyang50mCountySpatialPrototypeController>();
            Assert.That(controller.TryInitialize(), Is.True,
                controller.LastError);
            Assert.That(controller.Prototype.Partition.PlanningCellCount,
                Is.EqualTo(204800));
            Assert.That(controller.Prototype.Facilities.Count,
                Is.EqualTo(2084));
            Assert.That(controller.PlanningCellGameObjectCount, Is.Zero);
            Assert.That(controller.PlanningCellRenderObjectCount,
                Is.EqualTo(1));
            Assert.That(controller.MapTexture.width, Is.EqualTo(640));
            Assert.That(controller.MapTexture.height, Is.EqualTo(320));
            Directory.CreateDirectory(EvidenceRoot);
            yield return null;

            var views = new[]
            {
                (Luoyang50mPrototypeView.TerrainWaterRoad,
                    "01_terrain_water_road.png"),
                (Luoyang50mPrototypeView.FacilityDistricts,
                    "02_facility_districts.png"),
                (Luoyang50mPrototypeView.MigrationPrecision,
                    "03_source_spatial_precision.png"),
                (Luoyang50mPrototypeView.LayoutClosure,
                    "04_layout_network_closure.png")
            };
            foreach (var item in views)
            {
                controller.SetView(item.Item1);
                yield return null;
                var path = Path.Combine(EvidenceRoot, item.Item2);
                controller.CaptureEvidence(path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(1_000),
                    item.Item2);
            }

            var prototype = controller.Prototype;
            var benchmark = controller.Benchmark;
            File.WriteAllText(Path.Combine(EvidenceRoot,
                "performance-unity.json"), "{\n" +
                "  \"schemaVersion\": 1,\n" +
                $"  \"unityVersion\": \"{Application.unityVersion}\",\n" +
                $"  \"planningCellCount\": {prototype.Partition.PlanningCellCount},\n" +
                $"  \"chunkCount\": {prototype.Partition.ChunkCount},\n" +
                $"  \"packedArrayBytes\": {prototype.Partition.PackedArrayBytes},\n" +
                $"  \"facilityCount\": {prototype.Facilities.Count},\n" +
                $"  \"roadFacilityCount\": {prototype.RoadFacilityCount},\n" +
                $"  \"fortificationFacilityCount\": {prototype.FortificationFacilityCount},\n" +
                $"  \"sourceRoadStrategicCellCount\": {prototype.SourceRoadStrategicCellCount},\n" +
                $"  \"sourceWaterStrategicCellCount\": {prototype.SourceWaterStrategicCellCount},\n" +
                $"  \"facilityDerivedWaterPlanningCellCount\": {prototype.FacilityDerivedWaterPlanningCellCount},\n" +
                $"  \"sourceAnchorPreservedCount\": {prototype.SourceAnchorPreservedCount},\n" +
                $"  \"reconstructedPlacementCount\": {prototype.ReconstructedPlacementCount},\n" +
                $"  \"buildMilliseconds\": {Number(prototype.BuildMilliseconds)},\n" +
                $"  \"managedAllocationBytes\": {prototype.ManagedAllocationBytes},\n" +
                $"  \"hotP50Milliseconds\": {Number(benchmark.HotP50Milliseconds)},\n" +
                $"  \"hotP95Milliseconds\": {Number(benchmark.HotP95Milliseconds)},\n" +
                $"  \"warmP50Milliseconds\": {Number(benchmark.WarmP50Milliseconds)},\n" +
                $"  \"warmP95Milliseconds\": {Number(benchmark.WarmP95Milliseconds)},\n" +
                $"  \"coldP50Milliseconds\": {Number(benchmark.ColdP50Milliseconds)},\n" +
                $"  \"coldP95Milliseconds\": {Number(benchmark.ColdP95Milliseconds)},\n" +
                "  \"planningCellGameObjects\": 0,\n" +
                "  \"planningCellRenderObjects\": 1,\n" +
                $"  \"deterministicHash\": \"{prototype.DeterministicHash}\"\n" +
                "}\n");

            Object.Destroy(root);
            yield return null;
        }

        private static string Number(double value) => value.ToString(
            "F3", CultureInfo.InvariantCulture);
    }
}
