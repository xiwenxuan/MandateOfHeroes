using System;
using System.Linq;
using Mandate.Domain;
using Mandate.Simulation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void CountyVisualPlanning_ToolAndOverlayStateAreExclusiveAndPresentationOnly()
        {
            var world = WorldState.Create(184001UL);
            var revision = world.Revision;
            var time = world.AbsoluteDay;
            var tools = new PlanningToolState();
            var overlays = new PlanningMapOverlayState();

            tools.Activate(CountyPlanningPrimaryTool.Building,
                "placement.luoyang.residence.general.v1");
            tools.Activate(CountyPlanningPrimaryTool.Road);
            tools.BeginDrag(new PlanningCellCoord(10, 20));
            overlays.SetAdministrativeVisible(false);
            overlays.SetRoadsVisible(false);
            overlays.SetRiversVisible(false);
            overlays.SetGridVisible(false);
            overlays.SetTerrainAnalysisVisible(true);

            Assert.That(tools.PrimaryTool,
                Is.EqualTo(CountyPlanningPrimaryTool.Road));
            Assert.That(tools.SelectedProfileId, Is.Empty);
            Assert.That(tools.IsDragging, Is.True);
            tools.CancelCurrentAction();
            Assert.That(tools.IsDragging, Is.False);
            Assert.That(tools.PrimaryTool,
                Is.EqualTo(CountyPlanningPrimaryTool.Road));
            tools.CancelCurrentAction();
            Assert.That(tools.PrimaryTool,
                Is.EqualTo(CountyPlanningPrimaryTool.None));
            Assert.That(overlays.Version, Is.EqualTo(5));
            Assert.That(world.Revision, Is.EqualTo(revision));
            Assert.That(world.AbsoluteDay, Is.EqualTo(time));
        }

        [Test]
        public void CountyVisualPlanning_InputContractSeparatesCancelAndCameraRotation()
        {
            Assert.That(PlanningInputContract.ResolveMouseIntent(1, false,
                    false), Is.EqualTo(PlanningInputIntent.CancelTool));
            Assert.That(PlanningInputContract.ResolveMouseIntent(1, true,
                    true), Is.EqualTo(PlanningInputIntent.RotateCamera));
            Assert.That(PlanningInputContract.ResolveMouseIntent(2, false,
                    true), Is.EqualTo(PlanningInputIntent.PanCamera));
            Assert.That(PlanningInputContract.ResolveMouseIntent(0, false,
                    false), Is.EqualTo(
                PlanningInputIntent.PrimaryToolAction));
        }

        [Test]
        public void CountyVisualPlanning_LinearAndZoneDraftsAreDeterministicAndUndoable()
        {
            var context = CreatePlanningContext();
            var service = new CountyPlanningDraftGeometryService(
                context.Source.Prototype, context.Source.LayoutPackage,
                context.Validator);
            var partitionHash = context.Source.Prototype.Partition
                .ComputeSpatialHash();
            var session = new CountyPlanningSession(context.Validator.CountyId);
            var roadPath = FindValidLinearPath(context, service,
                service.ValidateRoad);
            var wallPath = FindValidLinearPath(context, service,
                path => service.ValidateWall(service.BuildWallSegments(path)));
            var canalPath = FindValidLinearPath(context, service,
                service.ValidateCanal);
            var roadValidation = service.ValidateRoad(roadPath);
            var wallSegments = service.BuildWallSegments(wallPath);
            var wallValidation = service.ValidateWall(wallSegments);
            var canalValidation = service.ValidateCanal(canalPath);

            session.CreateRoadDraft(roadPath, roadValidation);
            session.CreateFortificationDraft(wallSegments, wallValidation);
            session.CreateCanalDraft(canalPath, canalValidation);
            var local = context.Source.Prototype.Partition;
            session.CreateZoneDraft(CountyPlanningZoneKind.Residential,
                service.BuildRectangle(20, 20, 22, 24));
            var hash = session.ComputeDeterministicHash();

            Assert.That(session.RoadDrafts.Single().Path,
                Is.EqualTo(roadPath));
            Assert.That(session.FortificationDrafts.Single().Segments.All(
                value => value.EdgeDirection == PlanningCellDirection.North ||
                         value.EdgeDirection == PlanningCellDirection.West),
                Is.True);
            Assert.That(session.CanalDrafts.Count, Is.EqualTo(1));
            Assert.That(session.ZoneDrafts.Single().Cells.Count,
                Is.EqualTo(15));
            Assert.That(session.Undo(), Is.Not.Null);
            Assert.That(session.ZoneDrafts, Is.Empty);
            Assert.That(session.Redo(), Is.Not.Null);
            Assert.That(session.ComputeDeterministicHash(), Is.EqualTo(hash));
            Assert.That(local.ComputeSpatialHash(), Is.EqualTo(partitionHash));
        }

        [Test]
        public void CountyVisualPlanning_MoveCopyEyedropperDataAndDeleteStayDraftOnly()
        {
            var context = CreatePlanningContext();
            var residence = Residence(context);
            var firstPlacement = FindConnected(context, residence);
            var session = new CountyPlanningSession(context.Validator.CountyId);
            var firstFootprint = context.Validator.CreateFootprint(residence,
                firstPlacement.Row, firstPlacement.Column,
                firstPlacement.Rotation);
            var first = session.CreateDraft(residence, firstFootprint,
                context.Validator.Validate(firstFootprint, session));
            var secondPlacement = FindAnotherConnected(context, residence,
                session, firstPlacement.Row, firstPlacement.Column,
                first.DraftId);
            var secondFootprint = context.Validator.CreateFootprint(residence,
                secondPlacement.Row, secondPlacement.Column,
                secondPlacement.Rotation);
            var moved = session.MoveBuildingDraft(first.DraftId, residence,
                secondFootprint, context.Validator.Validate(secondFootprint,
                    session, first.DraftId));
            var thirdPlacement = FindAnotherConnected(context, residence,
                session, secondPlacement.Row, secondPlacement.Column,
                string.Empty);
            var thirdFootprint = context.Validator.CreateFootprint(residence,
                thirdPlacement.Row, thirdPlacement.Column,
                thirdPlacement.Rotation);
            var copied = session.CopyBuildingDraft(residence, thirdFootprint,
                context.Validator.Validate(thirdFootprint, session));

            Assert.That(moved.DraftId, Is.EqualTo(first.DraftId));
            Assert.That(copied.DraftId, Is.Not.EqualTo(first.DraftId));
            Assert.That(copied.FacilityDefinitionId,
                Is.EqualTo(first.FacilityDefinitionId));
            Assert.That(copied.ProvenanceId,
                Is.EqualTo(LuoyangCountyPlanningIds.DraftProvenanceId));
            Assert.That(session.RemoveDraft(copied.DraftId), Is.True);
            Assert.That(session.Drafts.Count, Is.EqualTo(1));
            Assert.That(session.Undo(), Is.SameAs(copied));
            Assert.That(session.Drafts.Count, Is.EqualTo(2));
        }

        private static PlanningCellCoord[] FindValidLinearPath(
            PlanningContext context,
            CountyPlanningDraftGeometryService service,
            Func<System.Collections.Generic.IReadOnlyList<PlanningCellCoord>,
                PlanningDraftValidation> validate)
        {
            var partition = context.Source.Prototype.Partition;
            for (var row = 2; row < partition.Rows - 2; row += 3)
            for (var column = 2; column < partition.Columns - 4; column += 3)
            {
                var path = service.BuildStablePath(row, column, row,
                    column + 2).ToArray();
                if (validate(path).IsValid) return path;
            }
            throw new AssertionException("No valid linear planning path found.");
        }

        private static (int Row, int Column, int Rotation)
            FindAnotherConnected(PlanningContext context,
                FacilityPlacementProfile profile,
                CountyPlanningSession session, int excludedRow,
                int excludedColumn, string ignoredDraftId)
        {
            var partition = context.Source.Prototype.Partition;
            for (var row = 4; row < partition.Rows - 4; row++)
            for (var column = 4; column < partition.Columns - 4; column++)
            {
                if (row == excludedRow && column == excludedColumn) continue;
                for (var rotation = 0; rotation < 4; rotation++)
                {
                    var footprint = context.Validator.CreateFootprint(profile,
                        row, column, rotation);
                    var result = context.Validator.Validate(footprint,
                        session, ignoredDraftId);
                    if (result.State == PlacementValidationState.Valid)
                        return (row, column, rotation);
                }
            }
            throw new AssertionException("No second valid placement found.");
        }
    }
}
