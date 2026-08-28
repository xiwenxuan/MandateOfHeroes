using System.Collections;
using System.IO;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangGoldenSliceVisualEvidencePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenSlice_CapturesRequiredVisualEvidence()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            var root = Path.Combine(Directory.GetCurrentDirectory(), "outputs",
                "luoyang-playable-v1", "screenshots");
            Directory.CreateDirectory(root);

            controller.SetVisualLod(MapVisualLod.World);
            yield return Capture(controller, root, "01_luoyang_regional_view.png");
            controller.SetVisualLod(MapVisualLod.City);
            yield return Capture(controller, root, "02_luoyang_full_city_view.png");
            controller.SetVisualLod(MapVisualLod.Close);
            yield return Capture(controller, root, "03_golden_slice_clean.png");

            var anchors = controller.GoldenSlice.FacilityAnchors;
            yield return SelectAndCapture(controller, root, anchors,
                "gate", "04_city_gate.png");
            yield return SelectAndCapture(controller, root, anchors,
                "market", "05_market.png");
            yield return SelectAndCapture(controller, root, anchors,
                "residence", "06_residence.png");
            yield return SelectAndCapture(controller, root, anchors,
                "production", "07_workshop.png");
            yield return Capture(controller, root, "08_crop_stages.png");
            yield return Capture(controller, root, "09_shipment_arrival.png");

            controller.SetBuildMode(true);
            controller.SelectBuildBlueprint(
                "blueprint.han.residence.general.v1");
            Assert.That(controller.PrepareOwnedBuildCell(), Is.True);
            yield return Capture(controller, root, "10_construction_ghost.png");
            Assert.That(controller.ConfirmBlueprintConstruction(), Is.True,
                controller.PlayableMessage);
            yield return Capture(controller, root, "11_construction_active.png");
            controller.AdvancePlayableDays(30);
            yield return Capture(controller, root, "12_facility_complete.png");

            controller.SetBuildMode(false);
            var damaged = controller.LivingRuntime.Facilities[0];
            damaged.ConditionBasisPoints = 1_000;
            controller.RefreshGoldenSlice();
            controller.SelectVisualFacility(damaged.FacilityId);
            yield return Capture(controller, root, "13_facility_damaged_ruin.png");
        }

        private static IEnumerator SelectAndCapture(
            LuoyangWorldValidationController controller, string root,
            System.Collections.Generic.IEnumerable<FacilityVisualAnchor> anchors,
            string token, string file)
        {
            foreach (var anchor in anchors)
                if (anchor.VisualProfileId.Contains(token))
                {
                    controller.SelectVisualFacility(anchor.FacilityId);
                    break;
                }
            yield return Capture(controller, root, file);
        }

        private static IEnumerator Capture(
            LuoyangWorldValidationController controller, string root,
            string file)
        {
            yield return null;
            var path = Path.Combine(root, file);
            controller.CaptureCleanPlayableEvidence(path);
            Assert.That(File.Exists(path), Is.True,
                "Unity did not write visual evidence " + file);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(0), file);
        }
    }
}
