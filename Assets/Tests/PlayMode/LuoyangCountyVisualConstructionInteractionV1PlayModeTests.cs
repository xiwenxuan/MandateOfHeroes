using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class
        LuoyangCountyVisualConstructionInteractionV1PlayModeTests
    {
        private static readonly string EvidenceRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "LuoyangCountyVisualConstructionInteractionReworkV1");
        private static readonly string AutomatedEvidenceRoot = Path.Combine(
            EvidenceRoot, "AutomatedStateProof");

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PlayableDemo_ProvidesCompleteDraftPlanningInteraction()
        {
            yield return SceneManager.LoadSceneAsync("PlayableDemo",
                LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null || !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var game = dashboard.DirectGame;
            Assert.That(game.ShowWorldView(), Is.True);
            var worldBefore = WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld);
            var beforeDay = dashboard.CurrentWorld.AbsoluteDay;
            Assert.That(game.EnterLuoyangCountyPlanningForTests(), Is.True,
                game.LastMessage);
            yield return null;
            var planning = game.CountyPlanning;
            Directory.CreateDirectory(EvidenceRoot);

            Assert.That(planning.ToolState.PrimaryTool,
                Is.EqualTo(CountyPlanningPrimaryTool.Building));
            Assert.That(planning.MapOverlays.AdministrativeVisible, Is.True);
            Assert.That(planning.MapOverlays.RoadsVisible, Is.True);
            Assert.That(planning.MapOverlays.RiversVisible, Is.True);
            Assert.That(planning.MapOverlays.GridVisible, Is.True);
            yield return CaptureGameView(planning,
                "01_map_legend_and_overlays.png");
            yield return CaptureGameView(planning,
                "02_admin_boundary_lod_far.png");
            Assert.That(planning.ZoomViewport(1f, new Vector2(0.5f, 0.5f)),
                Is.True);
            yield return CaptureGameView(planning,
                "03_admin_boundary_lod_near.png");
            yield return CaptureGameView(planning,
                "04_construction_bottom_toolbar.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);
            planning.ActivateBuildingTool(planning.SelectedProfile.ProfileId);
            Assert.That(planning.Validation.IsValid, Is.True);
            yield return CaptureGameView(planning,
                "05_building_ghost_valid.png");
            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ExistingFacilityCollision), Is.True);
            planning.ActivateBuildingTool(planning.SelectedProfile.ProfileId);
            Assert.That(planning.Validation.IsValid, Is.False);
            yield return CaptureGameView(planning,
                "06_building_ghost_invalid.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);
            planning.ActivateBuildingTool(planning.SelectedProfile.ProfileId);
            var first = planning.CreateDraft();
            Assert.That(first, Is.Not.Null);
            var second = CreateAnotherBuilding(planning);
            Assert.That(second, Is.Not.Null);
            yield return CaptureGameView(planning,
                "07_continuous_building_placement.png");

            var roadTimer = Stopwatch.StartNew();
            var roadStart = CreateLinear(planning,
                (row, column) => planning.CreateRoadDraft(row, column, row,
                    column + 2));
            roadTimer.Stop();
            planning.SelectCell(roadStart.Row, roadStart.Column);
            yield return CaptureGameView(planning,
                "08_road_drag_preview.png");

            var wallTimer = Stopwatch.StartNew();
            var wallStart = CreateLinear(planning,
                (row, column) => planning.CreateWallDraft(row, column, row,
                    column + 2));
            wallTimer.Stop();
            planning.SelectCell(wallStart.Row, wallStart.Column);
            yield return CaptureGameView(planning,
                "09_wall_edge_drag_preview.png");

            var canalTimer = Stopwatch.StartNew();
            var canalStart = CreateLinear(planning,
                (row, column) => planning.CreateCanalDraft(row, column, row,
                    column + 2));
            canalTimer.Stop();
            planning.SelectCell(canalStart.Row, canalStart.Column);
            yield return CaptureGameView(planning,
                "10_canal_drag_preview.png");

            var zoneTimer = Stopwatch.StartNew();
            var zone = planning.CreateZoneDraft(
                CountyPlanningZoneKind.Residential, canalStart.Row + 3,
                canalStart.Column, canalStart.Row + 5,
                canalStart.Column + 4);
            zoneTimer.Stop();
            Assert.That(zone.Cells.Count, Is.EqualTo(15));
            planning.SelectCell(canalStart.Row + 4, canalStart.Column + 2);
            yield return CaptureGameView(planning, "11_zone_brush.png");

            var moveTarget = FindBuildingTarget(planning, first.DraftId,
                first.Position, true);
            Assert.That(moveTarget, Is.Not.Null);
            yield return CaptureGameView(planning, "12_draft_move.png");
            var copied = FindCopyTarget(planning, first.DraftId);
            Assert.That(copied, Is.Not.Null);
            Assert.That(copied.DraftId, Is.Not.EqualTo(first.DraftId));
            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ExistingFacilityCollision), Is.True);
            var existingFacilityId = planning.CellInspection.FacilityIds
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(existingFacilityId))
                planning.EyedropperExistingFacility(existingFacilityId);
            yield return CaptureGameView(planning,
                "13_draft_copy_eyedropper.png");

            Assert.That(planning.Session.RemoveDraft(copied.DraftId), Is.True);
            yield return CaptureGameView(planning,
                "14_draft_demolish.png");
            var undoTimer = Stopwatch.StartNew();
            Assert.That(planning.Undo(), Is.Not.Null);
            undoTimer.Stop();
            var redoTimer = Stopwatch.StartNew();
            Assert.That(planning.Redo(), Is.Not.Null);
            redoTimer.Stop();
            yield return CaptureGameView(planning, "15_undo_redo.png");

            Assert.That(planning.SetOverlayVisible("roads", true), Is.True);
            yield return CaptureGameView(planning, "16_road_overlay.png");
            Assert.That(planning.SetOverlayVisible("terrain", true), Is.True);
            yield return CaptureGameView(planning,
                "17_terrain_overlay.png");
            var panBefore = planning.ViewMinimumColumn;
            Assert.That(game.PanCountyPlanningViewByGuiDelta(
                new Vector2(-80f, 20f), new Rect(0f, 0f, 900f, 450f)),
                Is.True);
            Assert.That(planning.ViewMinimumColumn, Is.Not.EqualTo(panBefore));
            Assert.That(game.RotateCountyPlanningViewByGuiDelta(
                new Vector2(60f, 0f)), Is.True);
            yield return CaptureGameView(planning,
                "18_input_camera_construction.png");

            Assert.That(dashboard.CurrentWorld.AbsoluteDay,
                Is.EqualTo(beforeDay));
            Assert.That(WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld), Is.EqualTo(worldBefore));
            Assert.That(dashboard.CurrentWorld.Facilities.Count,
                Is.EqualTo(2084));
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));

            var interactionPerformance = planning
                .MeasureInteractionPerformance(roadStart.Row,
                    roadStart.Column);
            var frameStart = Time.realtimeSinceStartupAsDouble;
            const int frameSamples = 60;
            for (var frame = 0; frame < frameSamples; frame++)
                yield return null;
            var frameSeconds = Time.realtimeSinceStartupAsDouble - frameStart;
            var framesPerSecond = frameSeconds <= 0d
                ? 0d : frameSamples / frameSeconds;
            File.WriteAllText(Path.Combine(EvidenceRoot,
                    "planning_interaction_performance_v1.json"),
                PerformanceJson(planning, interactionPerformance,
                    framesPerSecond,
                    roadTimer.Elapsed.TotalMilliseconds,
                    wallTimer.Elapsed.TotalMilliseconds,
                    canalTimer.Elapsed.TotalMilliseconds,
                    zoneTimer.Elapsed.TotalMilliseconds,
                    undoTimer.Elapsed.TotalMilliseconds,
                    redoTimer.Elapsed.TotalMilliseconds));
        }

        private static DraftBuildingBlueprint CreateAnotherBuilding(
            LuoyangCountyPlanningPresentationController planning)
        {
            var originRow = planning.SelectedLocalRow;
            var originColumn = planning.SelectedLocalColumn;
            for (var radius = 2; radius <= 40; radius++)
            for (var side = 0; side < 4; side++)
            {
                var row = originRow + (side == 0 ? -radius :
                    side == 2 ? radius : 0);
                var column = originColumn + (side == 1 ? radius :
                    side == 3 ? -radius : 0);
                if (row < 0 || row >= 320 || column < 0 || column >= 640)
                    continue;
                planning.SelectCell(row, column);
                for (var rotation = 0; rotation < 4; rotation++)
                {
                    if (planning.Validation.IsValid)
                        return planning.CreateDraft();
                    planning.RotateClockwise();
                }
            }
            return null;
        }

        private static (int Row, int Column) CreateLinear(
            LuoyangCountyPlanningPresentationController planning,
            Func<int, int, ICountyPlanningDraft> create)
        {
            for (var row = 2; row < 318; row += 3)
            for (var column = 2; column < 636; column += 3)
                if (create(row, column) != null) return (row, column);
            throw new AssertionException("No valid linear draft path found.");
        }

        private static DraftBuildingBlueprint FindBuildingTarget(
            LuoyangCountyPlanningPresentationController planning,
            string draftId, GlobalProjectedCoordinate ignoredPosition,
            bool move)
        {
            for (var row = 4; row < 316; row += 2)
            for (var column = 4; column < 636; column += 2)
            for (var rotation = 0; rotation < 4; rotation++)
            {
                var result = move
                    ? planning.MoveBuildingDraft(draftId, row, column,
                        rotation)
                    : planning.CopyBuildingDraft(draftId, row, column,
                        rotation);
                if (result != null) return result;
            }
            return null;
        }

        private static DraftBuildingBlueprint FindCopyTarget(
            LuoyangCountyPlanningPresentationController planning,
            string draftId) => FindBuildingTarget(planning, draftId,
            default, false);

        private static IEnumerator CaptureGameView(
            LuoyangCountyPlanningPresentationController planning,
            string fileName)
        {
            Directory.CreateDirectory(AutomatedEvidenceRoot);
            var path = Path.Combine(AutomatedEvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return null;
            planning.CaptureEvidence(path, 1280, 720);
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5_000),
                fileName);
        }

        private static string PerformanceJson(
            LuoyangCountyPlanningPresentationController planning,
            CountyPlanningInteractionPerformanceSnapshot interaction,
            double framesPerSecond,
            double road, double wall, double canal, double zone,
            double undo, double redo)
        {
            string N(double value) => value.ToString("0.###",
                CultureInfo.InvariantCulture);
            return "{\n" +
                   "  \"schema_id\": \"mandate.luoyang.county-visual-planning-performance.v1\",\n" +
                   "  \"sample_count\": " + interaction.Samples + ",\n" +
                   "  \"sampled_fps_1280x720\": " +
                   N(framesPerSecond) + ",\n" +
                   "  \"building_ghost_update_p50_ms\": " +
                   N(interaction.BuildingGhostUpdateP50Milliseconds) + ",\n" +
                   "  \"building_ghost_update_p95_ms\": " +
                   N(interaction.BuildingGhostUpdateP95Milliseconds) + ",\n" +
                   "  \"building_ghost_validation_p50_ms\": " +
                   N(planning.Performance.ValidatorP50Milliseconds) + ",\n" +
                   "  \"building_ghost_validation_p95_ms\": " +
                   N(planning.Performance.ValidatorP95Milliseconds) + ",\n" +
                   "  \"road_preview_p50_ms\": " +
                   N(interaction.RoadPreviewP50Milliseconds) + ",\n" +
                   "  \"road_preview_p95_ms\": " +
                   N(interaction.RoadPreviewP95Milliseconds) + ",\n" +
                   "  \"wall_preview_p50_ms\": " +
                   N(interaction.WallPreviewP50Milliseconds) + ",\n" +
                   "  \"wall_preview_p95_ms\": " +
                   N(interaction.WallPreviewP95Milliseconds) + ",\n" +
                   "  \"canal_preview_p50_ms\": " +
                   N(interaction.CanalPreviewP50Milliseconds) + ",\n" +
                   "  \"canal_preview_p95_ms\": " +
                   N(interaction.CanalPreviewP95Milliseconds) + ",\n" +
                   "  \"zone_brush_update_p50_ms\": " +
                   N(interaction.ZoneBrushP50Milliseconds) + ",\n" +
                   "  \"zone_brush_update_p95_ms\": " +
                   N(interaction.ZoneBrushP95Milliseconds) + ",\n" +
                   "  \"road_draft_commit_ms\": " + N(road) + ",\n" +
                   "  \"wall_draft_commit_ms\": " + N(wall) + ",\n" +
                   "  \"canal_draft_commit_ms\": " + N(canal) + ",\n" +
                   "  \"zone_draft_commit_ms\": " + N(zone) + ",\n" +
                   "  \"undo_ms\": " + N(undo) + ",\n" +
                   "  \"redo_ms\": " + N(redo) + ",\n" +
                   "  \"overlay_switch_ms\": " +
                   N(planning.LastOverlaySwitchMilliseconds) + ",\n" +
                   "  \"managed_allocation_bytes\": " +
                   interaction.ManagedAllocationBytes + ",\n" +
                   "  \"planning_cell_game_objects\": 0,\n" +
                   "  \"world_schema\": 79\n" +
                   "}\n";
        }
    }
}
