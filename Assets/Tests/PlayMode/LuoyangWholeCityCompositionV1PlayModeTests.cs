using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class LuoyangWholeCityCompositionV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1");

        [UnityTest]
        public IEnumerator DenseCityWindow_UsesComposedFinalAssetsAndTerrainGrounding()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(),
                Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            yield return null;

            Assert.That(controller.LuoyangWholeCityCompositionStatus,
                Is.EqualTo(LuoyangWholeCityCompositionIds.StatusId));
            Assert.That(controller.ProductionStatus,
                Is.EqualTo(LuoyangRoadConnectorPassageTraversalIds.StatusId));
            Assert.That(controller.WholeCityCompositionFacilityAnchorCount,
                Is.EqualTo(2084));
            Assert.That(controller.WholeCityCompositionDistrictCount,
                Is.EqualTo(6));
            Assert.That(controller.WholeCityCompositionAssetVariantCount,
                Is.EqualTo(54));
            Assert.That(controller.WholeCityCompositionDenseResidentAnchorCount,
                Is.EqualTo(549));
            Assert.That(controller.WholeCityCompositionCreatesSimulationSubCells,
                Is.False);
            Assert.That(controller.LuoyangBuildingBatchMetrics, Is.Not.Null);
            Assert.That(controller.LuoyangBuildingBatchMetrics.WithinBudget,
                Is.True);
            Assert.That(controller.LuoyangBuildingBatchMetrics
                .ResidentFacilityCount, Is.EqualTo(549));
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.EqualTo(controller.LuoyangBuildingBatchMetrics
                    .BuildingRendererBatchCount));
            Assert.That(Object.FindObjectsOfType<Collider>()
                .Where(item => item.gameObject.name.StartsWith(
                    "HAN_LUOYANG_BATCH_")), Is.Empty);
            Assert.That(Object.FindObjectsOfType<MeshFilter>()
                .Where(item => item.gameObject.name.StartsWith(
                    "HAN_LUOYANG_BATCH_"))
                .All(item => item.sharedMesh != null &&
                             item.sharedMesh.bounds.size.sqrMagnitude > 0f),
                Is.True);

            Directory.CreateDirectory(EvidenceRoot);
            var screenshotRoot = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshotRoot);
            var screenshotPath = Path.Combine(screenshotRoot,
                "01_DENSE_549_COMPOSED_FINAL_ASSET_TERRAIN_WINDOW.png");
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                controller.CaptureEvidence(screenshotPath, 1600, 1000);
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            yield return null;
            Assert.That(File.Exists(screenshotPath), Is.True);
            Assert.That(new FileInfo(screenshotPath).Length,
                Is.GreaterThan(12000));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.Zero);
            Assert.That(controller.LuoyangBuildingBatchMetrics, Is.Null);
        }
    }
}
