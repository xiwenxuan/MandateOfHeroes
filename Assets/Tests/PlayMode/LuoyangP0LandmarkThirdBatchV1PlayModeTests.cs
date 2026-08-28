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
    public sealed class LuoyangP0LandmarkThirdBatchV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourApprovedPrefabs_RenderFiveViewsAndCleanUp()
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
            controller.ApplyStrategicCellCamera(StrategicCellCameraRig
                .LuoyangP0LandmarkThirdBatchOverview);
            yield return null;

            Assert.That(controller.LuoyangP0LandmarkThirdBatchPreviewVisible,
                Is.True);
            Assert.That(controller.P0LandmarkThirdBatchProfileCount,
                Is.EqualTo(4));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(4));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.StatusId));
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances.Length, Is.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0LandmarkThirdBatchReady &&
                item.P0FinalAssetVerticalSliceReady &&
                item.P0FinalAssetArtistPrefabLoaded &&
                !item.P0FinalAssetProceduralFallbackActive &&
                item.P0FinalAssetFinalArtApproved), Is.True);
            Assert.That(instances.All(item => item
                    .GetComponentInChildren<LODGroup>(true)?.GetLODs().Length ==
                3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);

            Directory.CreateDirectory(EvidenceRoot);
            var cameras = new[]
            {
                (StrategicCellCameraRig.LuoyangP0LandmarkThirdBatchOverview,
                    "luoyang_p0_landmark_batch3_overview_v1.png"),
                (StrategicCellCameraRig.LuoyangP0LingtaiCloseup,
                    "luoyang_p0_lingtai_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0TaicangCloseup,
                    "luoyang_p0_taicang_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0ArsenalCloseup,
                    "luoyang_p0_arsenal_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0ZhuolongGardenCloseup,
                    "luoyang_p0_zhuolong_garden_candidate_v1.png")
            };
            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                foreach (var camera in cameras)
                {
                    controller.ApplyStrategicCellCamera(camera.Item1);
                    yield return null;
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
            Assert.That(controller.LuoyangP0LandmarkThirdBatchPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
        }
    }
}
