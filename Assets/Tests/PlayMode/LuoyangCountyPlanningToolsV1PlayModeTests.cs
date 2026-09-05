using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCountyPlanningToolsV1PlayModeTests
    {
        private static readonly string EvidenceRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "LuoyangCountyPlanningToolsV1");

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PlayableDemo_UsesFormalPlanningRouteAndCapturesAcceptance()
        {
            yield return SceneManager.LoadSceneAsync("PlayableDemo",
                LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;

            var game = dashboard.DirectGame;
            Assert.That(game, Is.Not.Null);
            Assert.That(game.ShowWorldView(), Is.True);
            var worldBefore = WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld);
            var partitionObjectsBefore = Object.FindObjectsOfType<GameObject>()
                .Length;
            var gcBefore = GC.GetTotalMemory(false);
            var enter = Stopwatch.StartNew();
            Assert.That(game.EnterLuoyangCountyPlanningForTests(), Is.True,
                game.LastMessage);
            enter.Stop();
            yield return null;

            var planning = game.CountyPlanning;
            Assert.That(game.IsCountyPlanningVisible, Is.True);
            Assert.That(planning, Is.Not.Null);
            Assert.That(planning.IsActive, Is.True, planning.LastError);
            Assert.That(planning.PlanningCellGameObjectCount, Is.EqualTo(0));
            Assert.That(planning.CountyMapRenderObjectCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<GameObject>().Length,
                Is.LessThan(partitionObjectsBefore + 200),
                "规划入口不得按 204,800 个 Cell 实例化对象；少量既有地图" +
                "分块可在下一帧完成创建。");
            Assert.That(planning.Validator.IndexedFacilityCount,
                Is.EqualTo(2084));
            Assert.That(planning.Validation.State,
                Is.EqualTo(PlacementValidationState.Valid));
            Assert.That(WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld), Is.EqualTo(worldBefore));

            Assert.That(planning.SelectCell(160, 320), Is.True);
            var viewRowBeforePan = planning.ViewMinimumRow;
            var viewColumnBeforePan = planning.ViewMinimumColumn;
            var planningMapRect = new Rect(0f, 0f, 960f, 480f);
            Assert.That(game.PanCountyPlanningViewByGuiDelta(
                new Vector2(-96f, 48f), planningMapRect), Is.True);
            Assert.That(planning.ViewMinimumRow,
                Is.LessThan(viewRowBeforePan));
            Assert.That(planning.ViewMinimumColumn,
                Is.GreaterThan(viewColumnBeforePan));
            var buildingRotation = planning.RotationQuarterTurns;
            Assert.That(game.RotateCountyPlanningViewByGuiDelta(
                new Vector2(96f, 0f)), Is.True);
            Assert.That(planning.ViewRotationDegrees,
                Is.GreaterThan(0f));
            Assert.That(planning.RotationQuarterTurns,
                Is.EqualTo(buildingRotation),
                "右键地图旋转不得替代建筑自身的 Tab 旋转。");
            Assert.That(game.RotateCountyPlanningViewByGuiDelta(
                new Vector2(-96f, 0f)), Is.True);
            Assert.That(planning.ViewRotationDegrees,
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);

            Directory.CreateDirectory(EvidenceRoot);
            yield return Capture(planning, "01_luoyang_planning_mode.png");
            yield return Capture(planning, "02_cell_selection.png");
            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);
            yield return Capture(planning,
                "03_residential_valid_preview.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.LargeFacility), Is.True);
            Assert.That(planning.Validation.CoveredCells.Count,
                Is.GreaterThan(1));
            yield return Capture(planning,
                "04_large_facility_multicell_preview.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ExistingFacilityCollision), Is.True);
            Assert.That(planning.Validation.BlockingReasons.Any(value =>
                value.Code == PlacementReasonIds
                    .ExistingFacilityCollision), Is.True);
            yield return Capture(planning,
                "05_existing_facility_collision.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.WaterBlocking), Is.True);
            Assert.That(planning.Validation.BlockingReasons.Any(value =>
                value.Code == PlacementReasonIds.WaterOverlap), Is.True);
            yield return Capture(planning, "06_water_blocking.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.FortificationBlocking), Is.True);
            Assert.That(planning.Validation.BlockingReasons.Any(value =>
                value.Code == PlacementReasonIds.FortificationOverlap),
                Is.True);
            yield return Capture(planning,
                "07_fortification_blocking.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);
            Assert.That(planning.Validation.RoadAccessResult.Status,
                Is.EqualTo(FacilityRoadAccessStatus.Connected));
            yield return Capture(planning, "08_road_access_valid.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.RoadAccessInvalid), Is.True);
            Assert.That(planning.Validation.RoadAccessResult.Status,
                Is.Not.EqualTo(FacilityRoadAccessStatus.Connected));
            yield return Capture(planning, "09_road_access_invalid.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.ValidResidence), Is.True);
            var draftTimer = Stopwatch.StartNew();
            Assert.That(game.CreatePlanningDraft(), Is.True,
                game.LastMessage);
            Assert.That(CreateAnotherDraft(planning), Is.True);
            draftTimer.Stop();
            Assert.That(planning.Drafts.Count, Is.EqualTo(2));
            Assert.That(dashboard.CurrentWorld.Facilities.Count,
                Is.EqualTo(2084));
            yield return Capture(planning, "10_draft_blueprints.png");

            var undoTimer = Stopwatch.StartNew();
            Assert.That(game.UndoPlanningDraft(), Is.True);
            undoTimer.Stop();
            Assert.That(planning.Drafts.Count, Is.EqualTo(1));
            yield return Capture(planning, "11_undo_blueprint.png");

            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.BeaconNearWall), Is.True);
            Assert.That(planning.SelectedProfile.FacilityDefinitionId,
                Is.EqualTo("facility.military.beacon"));
            yield return Capture(planning,
                "12_arrow_tower_preview_near_wall.png");

            Assert.That(WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld), Is.EqualTo(worldBefore));
            var performance = planning.Performance;
            File.WriteAllText(Path.Combine(EvidenceRoot,
                    "planning_performance_v1.json"),
                PerformanceJson(enter.Elapsed.TotalMilliseconds,
                    draftTimer.Elapsed.TotalMilliseconds,
                    undoTimer.Elapsed.TotalMilliseconds,
                    Math.Max(0L, GC.GetTotalMemory(false) - gcBefore),
                    planning, performance));
        }

        private static bool CreateAnotherDraft(
            LuoyangCountyPlanningPresentationController planning)
        {
            var originRow = planning.SelectedLocalRow;
            var originColumn = planning.SelectedLocalColumn;
            for (var radius = 1; radius <= 32; radius++)
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
                    if (planning.Validation.IsValid &&
                        planning.Validation.RoadAccessResult.Status ==
                        FacilityRoadAccessStatus.Connected)
                        return planning.CreateDraft() != null;
                    planning.RotateClockwise();
                }
            }
            return false;
        }

        private static IEnumerator Capture(
            LuoyangCountyPlanningPresentationController planning,
            string fileName)
        {
            var path = Path.Combine(EvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return null;
            yield return null;
            planning.CaptureEvidence(path, 1280, 720);
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(5_000),
                fileName);
        }

        private static string PerformanceJson(double enterMilliseconds,
            double draftMilliseconds, double undoMilliseconds,
            long allocationBytes,
            LuoyangCountyPlanningPresentationController planning,
            CountyPlanningPerformanceSnapshot performance)
        {
            string Number(double value) => value.ToString("0.###",
                CultureInfo.InvariantCulture);
            return "{\n" +
                   "  \"schema_id\": \"mandate.luoyang.county-planning-performance.v1\",\n" +
                   "  \"entry_ms\": " + Number(enterMilliseconds) + ",\n" +
                   "  \"cell_pick_p50_ms\": " +
                   Number(performance.CellPickP50Milliseconds) + ",\n" +
                   "  \"cell_pick_p95_ms\": " +
                   Number(performance.CellPickP95Milliseconds) + ",\n" +
                   "  \"validator_p50_ms\": " +
                   Number(performance.ValidatorP50Milliseconds) + ",\n" +
                   "  \"validator_p95_ms\": " +
                   Number(performance.ValidatorP95Milliseconds) + ",\n" +
                   "  \"draft_two_operations_ms\": " +
                   Number(draftMilliseconds) + ",\n" +
                   "  \"undo_ms\": " + Number(undoMilliseconds) + ",\n" +
                   "  \"managed_gc_delta_bytes\": " + allocationBytes + ",\n" +
                   "  \"indexed_facilities\": " +
                   planning.Validator.IndexedFacilityCount + ",\n" +
                   "  \"last_facility_candidates\": " +
                   planning.Validator.LastFacilityCandidateCount + ",\n" +
                   "  \"last_road_candidates\": " +
                   planning.Validator.LastRoadCandidateCount + ",\n" +
                   "  \"planning_cell_game_objects\": 0,\n" +
                   "  \"county_map_render_objects\": 1\n" +
                   "}\n";
        }
    }
}
