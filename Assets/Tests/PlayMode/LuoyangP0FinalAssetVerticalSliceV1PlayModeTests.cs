using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangP0FinalAssetVerticalSliceV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_FOUR_PIECE_VISUAL_REFINEMENT_AND_REVIEW_READABILITY_V2",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourRefinedNativePrefabs_RenderFiveV2ReviewViewsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            controller.ApplyStrategicCellCamera(StrategicCellCameraRig
                .LuoyangP0FinalAssetVerticalSliceOverview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.LuoyangP0FinalAssetVerticalSlicePreviewVisible,
                Is.True);
            Assert.That(controller.P0FinalAssetVerticalSliceProfileCount,
                Is.EqualTo(4));
            Assert.That(controller.BuildableFacilityPlacements,
                Has.Count.EqualTo(4));
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0FinalAssetVerticalSliceReady), Is.True);
            Assert.That(instances.All(item =>
                item.P0FinalAssetArtistPrefabLoaded &&
                !item.P0FinalAssetProceduralFallbackActive &&
                item.P0FinalAssetFinalArtApproved), Is.True);
            Assert.That(instances.All(item => item.AssetId ==
                item.P0FinalAssetReplacementSlotId), Is.True);
            Assert.That(instances.All(item =>
                item.GetComponentInChildren<LODGroup>(true)?.GetLODs().Length ==
                3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1"));

            Directory.CreateDirectory(EvidenceRoot);
            var cameras = new[]
            {
                (StrategicCellCameraRig.LuoyangP0FinalAssetVerticalSliceOverview,
                    "luoyang_p0_refined_native_prefab_overview_v2.png"),
                (StrategicCellCameraRig.LuoyangP0SouthPalaceCloseup,
                    "luoyang_p0_south_palace_refined_v2.png"),
                (StrategicCellCameraRig.LuoyangP0MingtangCloseup,
                    "luoyang_p0_mingtang_refined_v2.png"),
                (StrategicCellCameraRig.LuoyangP0GuangyangmenCloseup,
                    "luoyang_p0_guangyangmen_refined_v2.png"),
                (StrategicCellCameraRig.LuoyangP0NorthPalaceGateCloseup,
                    "luoyang_p0_north_palace_south_gate_refined_v2.png")
            };
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                foreach (var camera in cameras)
                {
                    controller.ApplyStrategicCellCamera(camera.Item1);
                    yield return null;
                    Assert.That(controller
                        .LuoyangP0FinalAssetVerticalSlicePreviewVisible, Is.True);
                    if (camera.Item1 != StrategicCellCameraRig
                            .LuoyangP0FinalAssetVerticalSliceOverview)
                        AssertCloseupIsFramed(camera.Item1);
                    controller.CaptureEvidence(Path.Combine(EvidenceRoot,
                        camera.Item2), 1600, 1000);
                    yield return null;
                }
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }
            foreach (var camera in cameras)
            {
                var path = Path.Combine(EvidenceRoot, camera.Item2);
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                    path);
            }

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangP0FinalAssetVerticalSlicePreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }

        private static void AssertCloseupIsFramed(string cameraId)
        {
            var preset = StrategicCellCameraRig.Get(cameraId);
            var cellId = GlobalSpatialFoundationV1.CreateCellGrid().ToCellId(
                preset.Row, preset.Column).Value;
            var instance = Object.FindObjectsOfType<
                    HanBuildableFacilityModelInstance>()
                .Single(item => item.CellId64 == cellId);
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, cameraId);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            var reviewCamera = Camera.main;
            Assert.That(reviewCamera, Is.Not.Null, cameraId);
            var center = reviewCamera.WorldToViewportPoint(bounds.center);
            Assert.That(center.z, Is.GreaterThan(0f), cameraId);
            Assert.That(center.x, Is.InRange(0.48f, 0.52f), cameraId);
            Assert.That(center.y, Is.InRange(0.48f, 0.52f), cameraId);

            var minimum = bounds.min;
            var maximum = bounds.max;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? minimum.x : maximum.x,
                    (corner & 2) == 0 ? minimum.y : maximum.y,
                    (corner & 4) == 0 ? minimum.z : maximum.z);
                var viewport = reviewCamera.WorldToViewportPoint(point);
                Assert.That(viewport.x, Is.InRange(0.02f, 0.98f), cameraId);
                Assert.That(viewport.y, Is.InRange(0.02f, 0.98f), cameraId);
            }
        }
    }
}
