using System.Collections;
using System.IO;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class ExplicitStrategicCellMapV1PlayModeTests
    {
        private static string ProjectRoot => Path.GetFullPath(
            Path.Combine(Application.dataPath, ".."));
        private static string EvidenceRoot => Path.Combine(ProjectRoot, "Docs",
            "HISTORICAL_WORLD_REFERENCE", "HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1",
            "Screenshots");
        private static string NationwideEvidenceRoot => Path.Combine(ProjectRoot, "Docs",
            "HISTORICAL_WORLD_REFERENCE", "HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator StrategicCellMap_CapturesHenanYinLuoyangAndMountainReviewImages()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            controller.SetPresentationUiVisible(false);
            Directory.CreateDirectory(EvidenceRoot);
            Directory.CreateDirectory(NationwideEvidenceRoot);

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig.NationwideOverview);
            yield return null;
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.World));
            Assert.That(controller.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.NationwideGuide32));
            Assert.That(controller.StrategicGridStepCells, Is.EqualTo(32));
            Assert.That(controller.StrategicCellCoverageCount,
                Is.EqualTo(GlobalSpatialFoundationV1.CreateCellGrid().CellCount));
            Assert.That(controller.VisibleStrategicCellCount, Is.Zero);
            Assert.That(controller.RuntimeCellOverlayObjectCount, Is.EqualTo(1));
            Assert.That(controller.NationwideStrategicCellContractId,
                Is.EqualTo(ExplicitStrategicCellMapV1.NationwideContractId));
            Assert.That(controller.ProductionStatus,
                Is.EqualTo("HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1_READY_FOR_USER_REVIEW"));
            yield return Capture(controller, NationwideEvidenceRoot,
                "00_NATIONWIDE_STRATEGIC_CELL_LOD.png");

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig.HenanYinOverview);
            yield return null;
            AssertStrategicCellState(controller, 1247, 1992);
            yield return Capture(controller, EvidenceRoot,
                "01_HENAN_YIN_24X24_STRATEGIC_CELLS.png");

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig.LuoyangSelection);
            yield return null;
            AssertStrategicCellState(controller, 1241, 2043);
            yield return Capture(controller, EvidenceRoot,
                "02_LUOYANG_CELL_SELECTION_CLOSE.png");

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig.MountainTerrain);
            yield return null;
            AssertStrategicCellState(controller, 1390, 1710);
            yield return Capture(controller, EvidenceRoot,
                "03_HENAN_MOUNTAIN_TERRAIN_CONFORMING_CELLS.png");

            var nationwideProbe = GlobalSpatialFoundationV1.CreateCellGrid().ToCellId(1720, 910);
            controller.FocusStrategicCell(nationwideProbe);
            yield return null;
            AssertStrategicCellState(controller, 1720, 910);
        }

        private static void AssertStrategicCellState(HanWorldNaturalMapController controller,
            int expectedRow, int expectedColumn)
        {
            var expected = GlobalSpatialFoundationV1.CreateCellGrid()
                .ToCellId(expectedRow, expectedColumn);
            Assert.That(controller.View, Is.EqualTo(HanNaturalMapView.Region));
            Assert.That(controller.CellOverlayVisible, Is.True);
            Assert.That(controller.StrategicCellContractId,
                Is.EqualTo(ExplicitStrategicCellMapV1.ContractId));
            Assert.That(controller.VisibleStrategicCellCount, Is.EqualTo(24 * 24));
            Assert.That(controller.RuntimeCellOverlayObjectCount, Is.EqualTo(2),
                "All 576 visible cells must remain two batched render objects.");
            Assert.That(controller.SelectedCellId, Is.EqualTo(expected));
            Assert.That(controller.TryPickGlobalCell(Vector3.zero, out var picked), Is.True);
            Assert.That(picked, Is.EqualTo(expected));
            Assert.That(controller.ProductionStatus,
                Is.EqualTo("HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1_READY_FOR_USER_REVIEW"));
            Assert.That(controller.StrategicGridLod, Is.EqualTo(StrategicCellGridLod.ExactCell));
            Assert.That(controller.StrategicGridStepCells, Is.EqualTo(1));
            Assert.That(controller.StrategicCellCoverageCount,
                Is.EqualTo(GlobalSpatialFoundationV1.CreateCellGrid().CellCount));
        }

        private static IEnumerator Capture(HanWorldNaturalMapController controller, string root,
            string file)
        {
            var path = Path.Combine(root, file);
            controller.CaptureEvidence(path, 1440, 900);
            yield return null;
            Assert.That(File.Exists(path), Is.True, file);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(8000), file);
        }
    }
}
