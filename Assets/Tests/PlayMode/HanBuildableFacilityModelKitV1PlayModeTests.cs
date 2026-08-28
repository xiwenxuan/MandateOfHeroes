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
    public sealed class HanBuildableFacilityModelKitV1PlayModeTests
    {
        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1", "Screenshots");

        [UnityTest]
        public IEnumerator FirstBatch_PlacesSevenModelsOnRealStrategicCellsAndCapturesEvidence()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);

            controller.ApplyStrategicCellCamera(
                StrategicCellCameraRig.BuildableFacilityReview);
            yield return null;

            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.VisualDetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
            Assert.That(controller.CellOverlayVisible, Is.True);
            Assert.That(controller.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.ExactCell));
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.True);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.EqualTo(7));
            Assert.That(controller.BuildableFacilityPlacements, Has.Count.EqualTo(7));
            Assert.That(controller.BuildableFacilityPlacements
                .Select(value => value.CellId.Value).Distinct().Count(), Is.EqualTo(7));
            Assert.That(controller.BuildableFacilityPlacements
                .Select(value => value.ModelId),
                Is.EquivalentTo(HanBuildableFacilityModelIds.AllModelIds));
            Assert.That(Object.FindObjectsOfType<HanBuildableFacilityModelInstance>(),
                Has.Length.EqualTo(7));
            Assert.That(controller.RuntimeBuildableFacilityRendererCount,
                Is.GreaterThan(60));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1_READY_FOR_USER_REVIEW"));

            Directory.CreateDirectory(EvidenceRoot);
            var path = Path.Combine(EvidenceRoot,
                "01_FIRST_BATCH_SEVEN_MODELS_ON_STRATEGIC_CELLS.png");
            controller.CaptureEvidence(path, 1440, 900);
            yield return null;
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(8000));

            controller.SetBuildableFacilityPreviewVisible(false);
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var direct = new HanBuildableFacilityModelPlacement(
                HanBuildableFacilityModelIds.Residence,
                grid.ToCellId(1245, 2043), "facility.runtime.test.residence", 90f);
            var instance = controller.PlaceBuildableFacilityModel(direct);
            Assert.That(instance.PreviewOnly, Is.False);
            Assert.That(instance.RuntimeBindingId,
                Is.EqualTo("facility.runtime.test.residence"));
            Assert.That(instance.CellId64, Is.EqualTo(direct.CellId.Value));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.BuildableFacilityPreviewVisible, Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
        }
    }
}
