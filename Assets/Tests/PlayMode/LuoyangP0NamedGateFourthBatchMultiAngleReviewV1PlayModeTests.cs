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
        LuoyangP0NamedGateFourthBatchMultiAngleReviewV1PlayModeTests
    {
        private static readonly string[] PieceSlugs =
        {
            "gumen", "jinmen", "kaiyangmen", "maomen"
        };

        private static readonly string[] AngleSlugs =
        {
            "front_oblique", "rear_oblique", "low_oblique"
        };

        private static string EvidenceRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs",
            "HISTORICAL_WORLD_REFERENCE",
            "LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1",
            "Screenshots");

        [UnityTest]
        public IEnumerator FourGates_RenderOverviewAndTwelveClearSafeViews()
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
                .LuoyangP0NamedGateFourthBatchOverview);
            yield return null;
            AssertRuntimeContract(controller);
            var overviewPath = Path.Combine(EvidenceRoot,
                "luoyang_p0_batch4_multi_angle_overview_v1.png");
            controller.CaptureEvidence(overviewPath, 1600, 1000);
            yield return null;

            var previousShadows = QualitySettings.shadows;
            try
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                for (var piece = 0;
                     piece <
                     LuoyangP0NamedGateFourthBatchMultiAngleReviewRig
                         .PieceCount; piece++)
                for (var angle = 0;
                     angle <
                     LuoyangP0NamedGateFourthBatchMultiAngleReviewRig
                         .AngleCount; angle++)
                {
                    var cameraId =
                        LuoyangP0NamedGateFourthBatchMultiAngleReviewRig
                            .GetCameraId(piece, angle);
                    controller
                        .ApplyP0NamedGateFourthBatchMultiAngleReviewCamera(
                            piece, angle);
                    yield return null;
                    AssertRuntimeContract(controller);
                    Assert.That(
                        controller.P0NamedGateFourthBatchReviewPieceIndex,
                        Is.EqualTo(piece));
                    Assert.That(
                        controller.P0NamedGateFourthBatchReviewAngleIndex,
                        Is.EqualTo(angle));
                    Assert.That(
                        controller.ActiveP0NamedGateFourthBatchReviewCameraId,
                        Is.EqualTo(cameraId));
                    AssertCloseupIsClearAndFramed(cameraId);
                    var path = Path.Combine(EvidenceRoot,
                        "luoyang_p0_batch4_" + PieceSlugs[piece] + "_" +
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
                 LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.PieceCount;
                 piece++)
            for (var angle = 0;
                 angle <
                 LuoyangP0NamedGateFourthBatchMultiAngleReviewRig.AngleCount;
                 angle++)
                AssertEvidenceFile(Path.Combine(EvidenceRoot,
                    "luoyang_p0_batch4_" + PieceSlugs[piece] + "_" +
                    AngleSlugs[angle] + "_v1.png"));

            controller.ApplyP0NamedGateFourthBatchMultiAngleReviewCamera(0, 0);
            controller.StepP0NamedGateFourthBatchReviewPiece(-1);
            yield return null;
            Assert.That(controller.P0NamedGateFourthBatchReviewPieceIndex,
                Is.EqualTo(3));
            controller.StepP0NamedGateFourthBatchReviewAngle(-1);
            yield return null;
            Assert.That(controller.P0NamedGateFourthBatchReviewAngleIndex,
                Is.EqualTo(2));

            controller.SetWorldView();
            yield return null;
            Assert.That(controller.LuoyangP0NamedGateFourthBatchPreviewVisible,
                Is.False);
            Assert.That(controller.RuntimeBuildableFacilityModelCount,
                Is.Zero);
            Assert.That(controller.BuildableFacilityPlacements, Is.Empty);
        }

        private static void AssertRuntimeContract(
            HanWorldNaturalMapController controller)
        {
            Assert.That(
                controller.P0NamedGateFourthBatchMultiAngleReviewContractId,
                Is.EqualTo(
                    LuoyangP0NamedGateFourthBatchMultiAngleReviewRig
                        .ContractId));
            Assert.That(controller.ProductionStatus, Is.EqualTo(
                LuoyangP0NamedGateFourthBatchIds.StatusId));
            Assert.That(controller.LuoyangP0NamedGateFourthBatchPreviewVisible,
                Is.True);
            var instances = Object.FindObjectsOfType<
                HanBuildableFacilityModelInstance>();
            Assert.That(instances, Has.Length.EqualTo(4));
            Assert.That(instances.All(item =>
                item.P0NamedGateFourthBatchReady &&
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
