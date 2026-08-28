using System;
using System.Collections.Generic;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class
        LuoyangP0LandmarkSecondBatchMultiAngleReviewV1Tests
    {
        [Test]
        public void Rig_FreezesFourPiecesThreeAnglesAndFlatReviewCells()
        {
            Assert.That(
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.ContractId,
                Is.EqualTo(
                    "presentation.luoyang.p0-landmark-second-batch.multi-angle-review.v1"));
            Assert.That(
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.PieceCount,
                Is.EqualTo(4));
            Assert.That(
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.AngleCount,
                Is.EqualTo(3));
            Assert.That(LuoyangP0LandmarkSecondBatchPreviewPlan.BoardCenterRow,
                Is.EqualTo(1243));
            Assert.That(
                LuoyangP0LandmarkSecondBatchPreviewPlan.BoardCenterColumn,
                Is.EqualTo(2043));
            var cameraIds = new HashSet<string>(StringComparer.Ordinal);

            for (var piece = 0;
                 piece < LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                     .PieceCount; piece++)
            {
                Assert.That(
                    LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                        .GetPieceLabel(piece), Is.Not.Empty);
                var front = StrategicCellCameraRig.Get(
                    LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                        .GetCameraId(piece, 0));
                var rear = StrategicCellCameraRig.Get(
                    LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                        .GetCameraId(piece, 1));
                var low = StrategicCellCameraRig.Get(
                    LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                        .GetCameraId(piece, 2));
                Assert.That(rear.Row, Is.EqualTo(front.Row));
                Assert.That(rear.Column, Is.EqualTo(front.Column));
                Assert.That(low.Row, Is.EqualTo(front.Row));
                Assert.That(low.Column, Is.EqualTo(front.Column));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(front.Yaw, rear.Yaw)),
                    Is.EqualTo(180f).Within(0.001f));
                Assert.That(front.Pitch, Is.InRange(38f, 44f));
                Assert.That(rear.Pitch, Is.InRange(38f, 44f));
                Assert.That(low.Pitch, Is.InRange(28f, 34f));
                Assert.That(low.Size, Is.LessThanOrEqualTo(1.72f));

                for (var angle = 0;
                     angle < LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                         .AngleCount; angle++)
                {
                    var cameraId =
                        LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                            .GetCameraId(piece, angle);
                    Assert.That(cameraIds.Add(cameraId), Is.True, cameraId);
                    Assert.That(StrategicCellCameraRig
                        .IsLuoyangP0LandmarkSecondBatch(cameraId), Is.True);
                    Assert.That(
                        LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                            .TryGetIndexes(cameraId, out var resolvedPiece,
                                out var resolvedAngle), Is.True);
                    Assert.That(resolvedPiece, Is.EqualTo(piece));
                    Assert.That(resolvedAngle, Is.EqualTo(angle));
                    Assert.That(
                        LuoyangP0LandmarkSecondBatchMultiAngleReviewRig
                            .GetAngleLabel(angle), Is.Not.Empty);
                }
            }

            Assert.That(cameraIds, Has.Count.EqualTo(12));
        }

        [Test]
        public void Rig_RejectsOutOfRangeReviewIndexes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetCameraId(
                    -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LuoyangP0LandmarkSecondBatchMultiAngleReviewRig.GetCameraId(
                    0, 3));
        }
    }
}
