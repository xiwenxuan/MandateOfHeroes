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
    public sealed class LuoyangP0NamedGateFourthBatchV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourApprovedGatePrefabs_RenderFiveViewsAndCleanUp()
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
                .LuoyangP0NamedGateFourthBatchOverview);
            yield return null;

            Assert.That(controller.LuoyangP0NamedGateFourthBatchPreviewVisible,
                Is.True);
            Assert.That(controller.P0NamedGateFourthBatchProfileCount,
                Is.EqualTo(4));
            Assert.That(controller.BuildableFacilityPlacements.Count,
                Is.EqualTo(4));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangP0NamedGateFourthBatchIds.StatusId));
            Assert.That(controller.BuildableFacilityPlacements.Select(item =>
                    item.RotationDegrees),
                Is.EqualTo(new[] { 180f, 0f, 0f, 270f }));
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances.Length, Is.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0NamedGateFourthBatchReady &&
                item.GateIdentityReady &&
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
                (StrategicCellCameraRig.LuoyangP0NamedGateFourthBatchOverview,
                    "luoyang_p0_named_gate_batch4_overview_v1.png"),
                (StrategicCellCameraRig.LuoyangP0GumenCloseup,
                    "luoyang_p0_gumen_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0JinmenCloseup,
                    "luoyang_p0_jinmen_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0KaiyangmenCloseup,
                    "luoyang_p0_kaiyangmen_candidate_v1.png"),
                (StrategicCellCameraRig.LuoyangP0MaomenCloseup,
                    "luoyang_p0_maomen_candidate_v1.png")
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
            Assert.That(controller.LuoyangP0NamedGateFourthBatchPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
        }
    }
}
