using System.Collections;
using System.Linq;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangP0FbxFinalActivationV1PlayModeTests
    {
        [UnityTest]
        public IEnumerator FourApprovedPrefabs_LoadWithoutFallbackAndCleanUp()
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
                .LuoyangP0FinalAssetVerticalSliceOverview);
            yield return null;

            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1"));
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0FinalAssetVerticalSliceReady &&
                item.P0FinalAssetArtistPrefabLoaded &&
                !item.P0FinalAssetProceduralFallbackActive &&
                item.P0FinalAssetFinalArtApproved), Is.True);
            Assert.That(instances.All(item => item
                    .GetComponentInChildren<LODGroup>(true)?.GetLODs().Length ==
                3), Is.True);
            Assert.That(instances.SelectMany(item =>
                item.GetComponentsInChildren<Collider>(true)), Is.Empty);

            controller.SetWorldView();
            yield return null;
            Assert.That(controller
                .LuoyangP0FinalAssetVerticalSlicePreviewVisible, Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
        }
    }
}
