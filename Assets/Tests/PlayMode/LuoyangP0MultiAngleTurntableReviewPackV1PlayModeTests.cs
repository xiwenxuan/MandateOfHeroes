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
    public sealed class LuoyangP0MultiAngleTurntableReviewPackV1PlayModeTests
    {
        private static readonly string[] PieceSlugs =
        {
            "south_palace", "mingtang", "guangyangmen",
            "north_palace_south_gate"
        };

        private static readonly string[] AngleSlugs =
        {
            "front_oblique", "rear_oblique", "low_oblique"
        };

        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourPieces_RenderOverviewAndTwelveSafeFrameViews()
        {
            yield return SceneManager.LoadSceneAsync("HanWorldArtDirectionLab",
                LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindObjectOfType<
                HanWorldNaturalMapController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            controller.SetPresentationUiVisible(false);
            Directory.CreateDirectory(EvidenceRoot);

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig
                .LuoyangP0FinalAssetVerticalSliceOverview);
            yield return null;
            AssertP0RuntimeContract(controller);
            var overviewPath = Path.Combine(EvidenceRoot,
                "luoyang_p0_multi_angle_overview_v1.png");
            controller.CaptureEvidence(overviewPath, 1600, 1000);
            yield return null;

            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                for (var piece = 0;
                     piece < LuoyangP0MultiAngleReviewRig.PieceCount; piece++)
                for (var angle = 0;
                     angle < LuoyangP0MultiAngleReviewRig.AngleCount; angle++)
                {
                    var cameraId = LuoyangP0MultiAngleReviewRig.GetCameraId(
                        piece, angle);
                    controller.ApplyP0MultiAngleReviewCamera(piece, angle);
                    yield return null;
                    AssertP0RuntimeContract(controller);
                    Assert.That(controller.P0ReviewPieceIndex,
                        Is.EqualTo(piece));
                    Assert.That(controller.P0ReviewAngleIndex,
                        Is.EqualTo(angle));
                    Assert.That(controller.ActiveP0ReviewCameraId,
                        Is.EqualTo(cameraId));
                    AssertCloseupIsFramed(cameraId);
                    var path = Path.Combine(EvidenceRoot,
                        "luoyang_p0_" + PieceSlugs[piece] + "_" +
                        AngleSlugs[angle] + "_v1.png");
                    controller.CaptureEvidence(path, 1600, 1000);
                    yield return null;
                }
            }
            finally
            {
                QualitySettings.shadows = previousShadows;
            }

            AssertEvidenceFile(overviewPath);
            for (var piece = 0;
                 piece < LuoyangP0MultiAngleReviewRig.PieceCount; piece++)
            for (var angle = 0;
                 angle < LuoyangP0MultiAngleReviewRig.AngleCount; angle++)
                AssertEvidenceFile(Path.Combine(EvidenceRoot,
                    "luoyang_p0_" + PieceSlugs[piece] + "_" +
                    AngleSlugs[angle] + "_v1.png"));

            controller.ApplyP0MultiAngleReviewCamera(0, 0);
            controller.StepP0ReviewPiece(-1);
            yield return null;
            Assert.That(controller.P0ReviewPieceIndex, Is.EqualTo(3));
            controller.StepP0ReviewAngle(-1);
            yield return null;
            Assert.That(controller.P0ReviewAngleIndex, Is.EqualTo(2));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangP0FinalAssetVerticalSlicePreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount, Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }

        private static void AssertP0RuntimeContract(
            HanWorldNaturalMapController controller)
        {
            Assert.That(controller.P0MultiAngleReviewContractId, Is.EqualTo(
                LuoyangP0MultiAngleReviewRig.ContractId));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                "LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1"));
            Assert.That(controller.LuoyangP0FinalAssetVerticalSlicePreviewVisible,
                Is.True);
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0FinalAssetArtistPrefabLoaded &&
                !item.P0FinalAssetProceduralFallbackActive &&
                item.P0FinalAssetFinalArtApproved), Is.True);
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

        private static void AssertEvidenceFile(string path)
        {
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000), path);
        }
    }
}
