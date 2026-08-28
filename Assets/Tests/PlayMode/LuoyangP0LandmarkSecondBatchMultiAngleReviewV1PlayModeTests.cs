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
    public sealed class
        LuoyangP0LandmarkSecondBatchMultiAngleReviewV1PlayModeTests
    {
        private static readonly string[] PieceSlugs =
        {
            "north_palace", "yongan_palace", "taixue", "biyong"
        };

        private static readonly string[] AngleSlugs =
        {
            "front_oblique", "rear_oblique", "low_oblique"
        };

        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourPieces_RenderOverviewAndTwelveClearSafeViews()
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
            Directory.CreateDirectory(EvidenceRoot);

            controller.ApplyStrategicCellCamera(StrategicCellCameraRig
                .LuoyangP0LandmarkSecondBatchOverview);
            yield return null;
            AssertRuntimeContract(controller);
            var overviewPath = Path.Combine(EvidenceRoot,
                "luoyang_p0_batch2_multi_angle_overview_v1.png");
            controller.CaptureEvidence(overviewPath, 1600, 1000);
            yield return null;

            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                for (var piece = 0;
                     piece <
                     LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.PieceCount;
                     piece++)
                for (var angle = 0;
                     angle <
                     LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.AngleCount;
                     angle++)
                {
                    var cameraId =
                        LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                            .GetCameraId(piece, angle);
                    controller
                        .ApplyP0LandmarkSecondBatchMultiAngleReviewCamera(
                            piece, angle);
                    yield return null;
                    AssertRuntimeContract(controller);
                    Assert.That(
                        controller.P0LandmarkSecondBatchReviewPieceIndex,
                        Is.EqualTo(piece));
                    Assert.That(
                        controller.P0LandmarkSecondBatchReviewAngleIndex,
                        Is.EqualTo(angle));
                    Assert.That(
                        controller.ActiveP0LandmarkSecondBatchReviewCameraId,
                        Is.EqualTo(cameraId));
                    AssertCloseupIsClearAndFramed(cameraId);
                    var path = Path.Combine(EvidenceRoot,
                        "luoyang_p0_batch2_" + PieceSlugs[piece] + "_" +
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
                 piece <
                 LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.PieceCount;
                 piece++)
            for (var angle = 0;
                 angle <
                 LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.AngleCount;
                 angle++)
                AssertEvidenceFile(Path.Combine(EvidenceRoot,
                    "luoyang_p0_batch2_" + PieceSlugs[piece] + "_" +
                    AngleSlugs[angle] + "_v1.png"));

            controller.ApplyP0LandmarkSecondBatchMultiAngleReviewCamera(0, 0);
            controller.StepP0LandmarkSecondBatchReviewPiece(-1);
            yield return null;
            Assert.That(controller.P0LandmarkSecondBatchReviewPieceIndex,
                Is.EqualTo(3));
            controller.StepP0LandmarkSecondBatchReviewAngle(-1);
            yield return null;
            Assert.That(controller.P0LandmarkSecondBatchReviewAngleIndex,
                Is.EqualTo(2));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangP0LandmarkSecondBatchPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }

        private static void AssertRuntimeContract(
            HanWorldNaturalMapController controller)
        {
            Assert.That(
                controller.P0LandmarkSecondBatchMultiAngleReviewContractId,
                Is.EqualTo(
                    LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                        .ContractId));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.StatusId));
            Assert.That(controller.LuoyangP0LandmarkSecondBatchPreviewVisible,
                Is.True);
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0LandmarkSecondBatchReady &&
                item.P0FinalAssetArtistPrefabLoaded &&
                !item.P0FinalAssetProceduralFallbackActive &&
                item.P0FinalAssetFinalArtApproved), Is.True);
        }

        private static void AssertCloseupIsClearAndFramed(string cameraId)
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

            var direction = bounds.center - reviewCamera.transform.position;
            var distance = direction.magnitude;
            if (Physics.Raycast(reviewCamera.transform.position,
                    direction.normalized, out var hit,
                    Mathf.Max(0f, distance - 0.02f)))
                Assert.Fail(cameraId + " center sightline is occluded by " +
                            hit.transform.name);
        }

        private static void AssertEvidenceFile(string path)
        {
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(12000), path);
        }
    }
}
