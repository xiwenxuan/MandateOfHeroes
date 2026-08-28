using System.Collections;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangGoldenSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenSlice_DefaultsToPlayableViewAndHidesGrid()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            Assert.That(controller.UsesPlayablePresentation, Is.True);
            Assert.That(controller.NormalModeHidesCellGrid, Is.True);
            Assert.That(controller.GoldenSlice.FacilityAnchors, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator GoldenSlice_BuildModeUsesRealBlueprintAndRuntimeProject()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            controller.SetBuildMode(true);
            Assert.That(controller.BuildModeEnabled, Is.True);
            Assert.That(controller.NormalModeHidesCellGrid, Is.False);
            Assert.That(controller.SelectBuildBlueprint(
                "blueprint.han.residence.general.v1"), Is.True);
            Assert.That(controller.PrepareOwnedBuildCell(), Is.True);
            var before = controller.LivingRuntime.ConstructionProjects.Count;
            Assert.That(controller.ConfirmBlueprintConstruction(), Is.True,
                controller.PlayableMessage);
            Assert.That(controller.LivingRuntime.ConstructionProjects.Count,
                Is.EqualTo(before + 1));
        }
    }
}
