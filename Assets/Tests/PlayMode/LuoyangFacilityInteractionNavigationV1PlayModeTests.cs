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
    public sealed class LuoyangFacilityInteractionNavigationV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1");

        [UnityTest]
        public IEnumerator DenseCityWindow_BuildsSelectableTriggersAndRoadOverlay()
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
            yield return null;

            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangRoadConnectorPassageTraversalIds.StatusId));
            Assert.That(controller.LuoyangFacilityInteractionNavigationStatus,
                Is.EqualTo(LuoyangFacilityInteractionNavigationIds.StatusId));
            Assert.That(controller.LuoyangFacilitySelectionProxyPlanCount,
                Is.EqualTo(2084));
            Assert.That(controller.RuntimeLuoyangFacilitySelectionProxyCount,
                Is.EqualTo(549));
            Assert.That(controller.LuoyangRoadNavigationNodeCount,
                Is.EqualTo(379));
            Assert.That(controller.LuoyangRoadNavigationEdgeCount,
                Is.EqualTo(382));
            Assert.That(controller.LuoyangRoadComponentCountBeforeConnectors,
                Is.EqualTo(29));
            Assert.That(controller.RuntimeLuoyangRoadNavigationEdgeCount,
                Is.GreaterThan(0));

            var proxies = Object.FindObjectsOfType<
                LuoyangFacilitySelectionProxyInstance>();
            Assert.That(proxies.Length, Is.EqualTo(549));
            Assert.That(proxies.Select(item => item.FacilityId).Distinct()
                .Count(), Is.EqualTo(549));
            Assert.That(proxies.All(item =>
            {
                var collider = item.GetComponent<BoxCollider>();
                return collider != null && collider.isTrigger &&
                       collider.size.x > 0f && collider.size.y > 0f &&
                       collider.size.z > 0f;
            }), Is.True);
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .NavigationOverlayName), Is.Not.Null);

            var target = proxies.OrderBy(item => item.FacilityId).First();
            var targetCollider = target.GetComponent<BoxCollider>();
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var ray = new Ray(camera.transform.position,
                (targetCollider.bounds.center - camera.transform.position)
                .normalized);
            Assert.That(controller.TrySelectLuoyangFacility(ray), Is.True);
            Assert.That(controller.SelectedLuoyangFacilityId,
                Is.EqualTo(target.FacilityId));
            var highlight = GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime
                    .SelectionHighlightName);
            Assert.That(highlight, Is.Not.Null);
            Assert.That(highlight.GetComponent<MeshRenderer>().enabled,
                Is.True);
            yield return null;
            yield return null;

            Directory.CreateDirectory(EvidenceRoot);
            var screenshotRoot = Path.Combine(EvidenceRoot, "Screenshots");
            Directory.CreateDirectory(screenshotRoot);
            var screenshotPath = Path.Combine(screenshotRoot,
                "01_DENSE_CITY_SELECTION_AND_ROAD_NAVIGATION_OVERLAY.png");
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
            AssertScreenshotHasVisualVariance(screenshotPath);

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.RuntimeLuoyangFacilitySelectionProxyCount,
                Is.Zero);
            Assert.That(controller.SelectedLuoyangFacilityId, Is.Null);
            Assert.That(GameObject.Find(
                LuoyangFacilityInteractionNavigationRuntime.RootName), Is.Null);
        }

        private static void AssertScreenshotHasVisualVariance(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.That(texture.LoadImage(bytes), Is.True);
                var pixels = texture.GetPixels32();
                var sampledColors = pixels.Where((_, index) => index % 997 == 0)
                    .Select(item => item.r + ":" + item.g + ":" + item.b)
                    .Distinct().Count();
                Assert.That(sampledColors, Is.GreaterThan(32),
                    "Evidence screenshot is visually blank or nearly uniform.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
