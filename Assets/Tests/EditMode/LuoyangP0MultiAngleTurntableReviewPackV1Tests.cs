using System;
using System.Collections.Generic;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0MultiAngleTurntableReviewPackV1Tests
    {
        [Test]
        public void Rig_FreezesFourPiecesThreeAnglesAndDistinctStableCameras()
        {
            Assert.That(LuoyangP0MultiAngleReviewRig.ContractId, Is.EqualTo(
                "presentation.luoyang.p0-four-piece.multi-angle-review.v1"));
            Assert.That(LuoyangP0MultiAngleReviewRig.PieceCount, Is.EqualTo(4));
            Assert.That(LuoyangP0MultiAngleReviewRig.AngleCount, Is.EqualTo(3));
            var cameraIds = new HashSet<string>(StringComparer.Ordinal);

            for (var piece = 0;
                 piece < LuoyangP0MultiAngleReviewRig.PieceCount; piece++)
            {
                Assert.That(LuoyangP0MultiAngleReviewRig.GetPieceLabel(piece),
                    Is.Not.Empty);
                var front = StrategicCellCameraRig.Get(
                    LuoyangP0MultiAngleReviewRig.GetCameraId(piece, 0));
                var rear = StrategicCellCameraRig.Get(
                    LuoyangP0MultiAngleReviewRig.GetCameraId(piece, 1));
                var low = StrategicCellCameraRig.Get(
                    LuoyangP0MultiAngleReviewRig.GetCameraId(piece, 2));
                Assert.That(rear.Row, Is.EqualTo(front.Row));
                Assert.That(rear.Column, Is.EqualTo(front.Column));
                Assert.That(low.Row, Is.EqualTo(front.Row));
                Assert.That(low.Column, Is.EqualTo(front.Column));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(front.Yaw, rear.Yaw)),
                    Is.EqualTo(180f).Within(0.001f));
                Assert.That(front.Pitch, Is.InRange(38f, 44f));
                Assert.That(rear.Pitch, Is.InRange(38f, 44f));
                Assert.That(low.Pitch, Is.InRange(28f, 34f));
                Assert.That(low.Size, Is.LessThanOrEqualTo(1.65f));

                for (var angle = 0;
                     angle < LuoyangP0MultiAngleReviewRig.AngleCount; angle++)
                {
                    var cameraId = LuoyangP0MultiAngleReviewRig.GetCameraId(
                        piece, angle);
                    Assert.That(cameraIds.Add(cameraId), Is.True, cameraId);
                    Assert.That(StrategicCellCameraRig
                        .IsLuoyangP0FinalAssetVerticalSlice(cameraId), Is.True);
                    Assert.That(LuoyangP0MultiAngleReviewRig.TryGetIndexes(
                        cameraId, out var resolvedPiece, out var resolvedAngle),
                        Is.True);
                    Assert.That(resolvedPiece, Is.EqualTo(piece));
                    Assert.That(resolvedAngle, Is.EqualTo(angle));
                    Assert.That(LuoyangP0MultiAngleReviewRig.GetAngleLabel(angle),
                        Is.Not.Empty);
                }
            }

            Assert.That(cameraIds, Has.Count.EqualTo(12));
        }

        [Test]
        public void Rig_RejectsOutOfRangeReviewIndexes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LuoyangP0MultiAngleReviewRig.GetCameraId(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LuoyangP0MultiAngleReviewRig.GetCameraId(0, 3));
        }
    }
}
