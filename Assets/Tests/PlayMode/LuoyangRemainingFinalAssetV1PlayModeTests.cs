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
    public sealed class LuoyangRemainingFinalAssetV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator All54FinalSlots_LoadApprovedPrefabsAndCleanUp()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            yield return null;
            yield return null;
            Assert.That(controller.RemainingFinalAssetProfileCount,
                Is.EqualTo(38));
            Assert.That(controller.LuoyangRemainingFinalAssetStatus,
                Is.EqualTo(LuoyangRemainingFinalAssetIds.StatusId));

            controller.SetLuoyangFinalAssetReviewPreviewVisible(true);
            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.LuoyangFinalAssetReviewAll);
            yield return null;
            yield return null;
            var instances = Object.FindObjectsOfType<
                    HanBuildableFacilityModelInstance>()
                .Where(item => item.PreviewOnly && item.FinalAssetReviewReady)
                .ToArray();
            Assert.That(instances, Has.Length.EqualTo(54));
            Assert.That(instances.All(item => item.FinalAssetRuntimeReady),
                Is.True);
            Assert.That(instances.All(item =>
                item.FinalAssetArtistPrefabLoaded), Is.True);
            Assert.That(instances.All(item =>
                !item.FinalAssetProceduralFallbackActive), Is.True);
            Assert.That(instances.All(item => item.FinalAssetApproved),
                Is.True);
            Assert.That(instances.Count(item =>
                    item.FinalAssetReviewOrder >= 15 &&
                    item.FinalAssetReviewOrder != 22), Is.EqualTo(38));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "luoyang_all_54_final_assets_activated_v1.png");
            controller.CaptureEvidence(path, 1600, 1000);
            yield return null;
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000),
                path);

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangFinalAssetReviewPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
        }
    }
}
