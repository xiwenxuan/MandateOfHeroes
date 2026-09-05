using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mandate.Domain;
using Mandate.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    /// <summary>
    /// Captures the V2 county sandbox through the formal PlayableDemo route.
    /// The sequence leaves the editor in a clean Far county view for manual
    /// review and never changes the authoritative WorldState.
    /// </summary>
    public static class LuoyangCountyStrategicSandboxV2FinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.LuoyangCountyStrategicSandboxV2Review.Pending";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private static readonly string[] Files =
        {
            "01_v1_far_before.png",
            "02_v2_far_terrain_fixed.png",
            "03_v2_far_final.png",
            "04_v2_mid.png",
            "05_v2_near.png",
            "06_v2_terrain_hills.png",
            "07_v2_river.png",
            "08_v2_major_road.png",
            "09_v2_urban_aggregate.png",
            "10_v2_landmarks.png",
            "11_v2_wall_gate.png",
            "12_v2_village.png",
            "13_v2_farmland.png",
            "14_v2_vegetation.png",
            "15_v2_mid_building_clusters.png",
            "16_v2_near_facility.png",
            "17_v2_planning_entry.png",
            "18_v2_construction_toolbar.png",
            "19_v2_building_ghost_valid.png",
            "20_v2_building_ghost_invalid.png",
            "21_v2_local_grid.png",
            "22_v2_road_draft.png",
            "23_v2_wall_draft.png",
            "24_v2_canal_draft.png",
            "25_v2_facility_info_panel.png",
            "26_v2_debug_overlay.png",
            "27_v2_debug_off.png"
        };

        private static PlayableLuoyangGameController _game;
        private static LuoyangCountyPlanningPresentationController _planning;
        private static int _attempts;
        private static int _phase;
        private static int _settle;
        private static int _wait;
        private static string _pendingPath;
        private static int _width;
        private static int _height;
        private static bool _phasePrepared;

        [InitializeOnLoadMethod]
        private static void Restore()
        {
            var request = Path.Combine(Directory.GetCurrentDirectory(),
                "Docs", "Evidence",
                "LuoyangCountyStrategicSandboxVisualAndConstructionInteractionV2",
                ".capture-request");
            if (File.Exists(request))
            {
                File.Delete(request);
                EditorApplication.delayCall += CaptureAndOpenForReview;
                return;
            }
            if (!SessionState.GetBool(PendingKey, false)) return;
            Arm();
            if (EditorApplication.isPlaying)
                EditorApplication.update += TryStart;
        }

        [MenuItem("Mandate/Validation/Capture Luoyang County Strategic Sandbox V2 Evidence And Review")]
        public static void CaptureAndOpenForReview()
        {
            SessionState.SetBool(PendingKey, true);
            Arm();
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
            else
            {
                _attempts = 0;
                EditorApplication.update -= TryStart;
                EditorApplication.update += TryStart;
            }
        }

        private static void Arm()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            _attempts = 0;
            EditorApplication.update -= TryStart;
            EditorApplication.update += TryStart;
        }

        private static void TryStart()
        {
            if (!EditorApplication.isPlaying) return;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            if (dashboard == null)
            {
                if (++_attempts < 900) return;
                Fail("PlayableDemo did not create SimulationDashboard.");
                return;
            }
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
            {
                if (!dashboard.StartRecommendedLuoyangExperience())
                {
                    if (++_attempts < 900) return;
                    Fail("Could not start formal Luoyang experience.");
                    return;
                }
            }
            _game = dashboard.DirectGame;
            if (!_game.ShowCountyView())
            {
                if (++_attempts < 900) return;
                Fail("Could not enter formal county route.");
                return;
            }
            _planning = _game.CountyPlanning;
            if (_planning?.WorldSpacePresentation == null ||
                !_planning.WorldSpacePresentation.IsBuilt)
            {
                if (++_attempts < 900) return;
                Fail(_planning?.LastError ?? "County V2 renderer unavailable.");
                return;
            }
            EditorApplication.update -= TryStart;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            BeginCapture();
        }

        private static void BeginCapture()
        {
            Directory.CreateDirectory(EvidenceRoot());
            for (var index = 1; index < Files.Length; index++)
            {
                var path = Path.Combine(EvidenceRoot(), Files[index]);
                if (File.Exists(path)) File.Delete(path);
            }
            _phase = 1;
            _settle = 8;
            _wait = 0;
            _pendingPath = null;
            _width = 0;
            _height = 0;
            _phasePrepared = false;
            EditorApplication.update -= DriveCapture;
            EditorApplication.update += DriveCapture;
        }

        private static void DriveCapture()
        {
            if (!EditorApplication.isPlaying || _planning == null)
            {
                Fail("Play Mode ended during V2 evidence capture.");
                return;
            }
            var width = _phase == Files.Length - 1 ? 1280 : 1920;
            var height = _phase == Files.Length - 1 ? 720 : 1080;
            if (_width != width || _height != height)
            {
                RequestResolution(width, height);
                _width = width;
                _height = height;
                _phasePrepared = false;
                _settle = 0;
                return;
            }
            if (Screen.width != width || Screen.height != height) return;
            if (!string.IsNullOrWhiteSpace(_pendingPath))
            {
                if (File.Exists(_pendingPath) &&
                    new FileInfo(_pendingPath).Length > 10_000)
                {
                    _pendingPath = null;
                    _phase++;
                    _phasePrepared = false;
                    _settle = 0;
                    if (_phase >= Files.Length) Finish();
                }
                else if (++_wait > 600)
                    Fail("Screenshot was not written: " + _pendingPath);
                return;
            }
            if (!_phasePrepared)
            {
                try
                {
                    PreparePhase(_phase);
                    _planning.WorldSpacePresentation.Synchronize();
                    _phasePrepared = true;
                    _settle = 8;
                }
                catch (Exception exception)
                {
                    Fail("phase=" + _phase + " file=" + Files[_phase] +
                         " " + exception);
                }
                return;
            }
            if (_settle-- > 0) return;
            try
            {
                _planning.WorldSpacePresentation.Synchronize();
                _pendingPath = Path.Combine(EvidenceRoot(), Files[_phase]);
                _wait = 0;
                ScreenCapture.CaptureScreenshot(_pendingPath);
            }
            catch (Exception exception)
            {
                Fail("phase=" + _phase + " file=" + Files[_phase] +
                     " " + exception);
            }
        }

        private static void PreparePhase(int phase)
        {
            var world = _planning.WorldSpacePresentation;
            world.SetDebugVisible(false);
            SetOverlays(true, true, true, false, true);
            switch (phase)
            {
                case 1:
                case 2:
                case 8:
                case 9:
                    Require(_game.ShowCountyView(), "Far county");
                    break;
                case 3:
                    Require(_game.ShowCountyUrbanAreaView(), "Mid county");
                    break;
                case 4:
                    Require(_game.ShowCountyPlanningSubView(), "Near county");
                    break;
                case 5:
                    Require(_game.ShowCountyUrbanAreaView(), "hill relief");
                    FocusTerrainOrRelief(PlanningTerrainClass.Hill);
                    break;
                case 6:
                    Require(_game.ShowCountyUrbanAreaView(), "river");
                    FocusFirst((row, column) =>
                        _planning.Partition.WaterState(row, column) > 0);
                    break;
                case 7:
                    Require(_game.ShowCountyUrbanAreaView(), "major road");
                    var road = _planning.PresentationStack.Roads.First(item =>
                        item.PresentationClass <=
                        CountyRoadPresentationClass.CountyMainR1);
                    _planning.SelectCell(road.Edge.FromLocalRow,
                        road.Edge.FromLocalColumn);
                    break;
                case 10:
                    Require(_game.ShowCountyUrbanAreaView(), "wall gate");
                    var gate = _planning.LayoutPackage.Fortifications.First(
                        item => item.IsGate);
                    _planning.SelectCell(gate.LocalRow, gate.LocalColumn);
                    break;
                case 11:
                    Require(_game.ShowCountyUrbanAreaView(), "village");
                    FocusFacility(item => string.Equals(item.DefinitionId,
                        "facility.residential.rural_hamlet",
                        StringComparison.Ordinal));
                    break;
                case 12:
                    Require(_game.ShowCountyPlanningSubView(), "farmland");
                    FocusFacility(CountyWorldSpacePresentationPlan
                        .IsAgriculturalFacility);
                    break;
                case 13:
                    Require(_game.ShowCountyUrbanAreaView(), "vegetation");
                    FocusVegetation();
                    break;
                case 14:
                    Require(_game.ShowCountyUrbanAreaView(),
                        "Mid building clusters");
                    FocusFacility(item => InsideUrban(item) &&
                        !CountyWorldSpacePresentationPlan
                            .IsSpecializedInfrastructure(item.DefinitionId));
                    break;
                case 15:
                case 24:
                    Require(_game.ShowCountyPlanningSubView(),
                        "Near facility");
                    FocusFacility(item => InsideUrban(item) &&
                        !CountyWorldSpacePresentationPlan
                            .IsSpecializedInfrastructure(item.DefinitionId));
                    break;
                case 16:
                    Require(_game.ShowCountyPlanningSubView(),
                        "planning entry");
                    break;
                case 17:
                    Require(_game.ShowCountyPlanningSubView(),
                        "construction toolbar");
                    SetToolbarCategory("building");
                    break;
                case 18:
                    Require(_game.ShowCountyPlanningSubView(), "valid ghost");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence), "valid ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    SetToolbarCategory("building");
                    break;
                case 19:
                    Require(_game.ShowCountyPlanningSubView(),
                        "invalid ghost");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ExistingFacilityCollision),
                        "invalid ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 20:
                    Require(_game.ShowCountyPlanningSubView(), "local grid");
                    _planning.SetOverlayVisible("grid", true);
                    break;
                case 21:
                    Require(_game.ShowCountyPlanningSubView(), "road draft");
                    CreateLinearDraft(CountyPlanningPrimaryTool.Road);
                    SetToolbarCategory("road");
                    break;
                case 22:
                    Require(_game.ShowCountyPlanningSubView(), "wall draft");
                    CreateLinearDraft(CountyPlanningPrimaryTool.Wall);
                    SetToolbarCategory("defense");
                    break;
                case 23:
                    Require(_game.ShowCountyPlanningSubView(), "canal draft");
                    CreateLinearDraft(CountyPlanningPrimaryTool.Canal);
                    SetToolbarCategory("water");
                    break;
                case 25:
                    Require(_game.ShowCountyUrbanAreaView(), "debug overlay");
                    world.SetDebugVisible(true);
                    break;
                case 26:
                    Require(_game.ShowCountyView(), "clean Far handoff");
                    world.SetDebugVisible(false);
                    break;
            }
        }

        private static void SetOverlays(bool roads, bool rivers,
            bool walls, bool grid, bool planning)
        {
            _planning.SetOverlayVisible("roads", roads);
            _planning.SetOverlayVisible("rivers", rivers);
            _planning.SetOverlayVisible("fortifications", walls);
            _planning.SetOverlayVisible("grid", grid);
            _planning.SetOverlayVisible("planning", planning);
        }

        private static bool InsideUrban(Luoyang50mLayoutFacility facility)
        {
            return _planning.IsInsideUrbanPresentation(facility.LocalRow,
                facility.LocalColumn, 12);
        }

        private static void FocusFacility(
            Func<Luoyang50mLayoutFacility, bool> predicate)
        {
            var facility = _planning.LayoutPackage.Facilities
                .Where(predicate)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal)
                .First();
            _planning.SelectCell(facility.LocalRow, facility.LocalColumn);
        }

        private static void FocusFirst(Func<int, int, bool> predicate)
        {
            for (var row = 0; row < _planning.Partition.Rows; row++)
            for (var column = 0; column < _planning.Partition.Columns;
                 column++)
                if (predicate(row, column))
                {
                    _planning.SelectCell(row, column);
                    return;
                }
            throw new InvalidOperationException("No focus cell matched.");
        }

        private static void FocusTerrainOrRelief(
            PlanningTerrainClass preferredTerrain)
        {
            var partition = _planning.Partition;
            for (var row = 0; row < partition.Rows; row += 2)
            for (var column = 0; column < partition.Columns; column += 2)
                if (partition.Terrain(row, column) == preferredTerrain)
                {
                    _planning.SelectCell(row, column);
                    return;
                }

            var bestRow = 0;
            var bestColumn = 0;
            var bestScore = long.MinValue;
            var centerRow = partition.Rows / 2;
            var centerColumn = partition.Columns / 2;
            for (var row = 4; row < partition.Rows - 4; row += 2)
            for (var column = 4; column < partition.Columns - 4; column += 2)
            {
                var elevation = partition.GroundElevationDecimetres(row,
                    column);
                var relief = Math.Max(
                    Math.Max(Math.Abs(elevation -
                                      partition.GroundElevationDecimetres(
                                          row - 4, column)),
                        Math.Abs(elevation -
                                 partition.GroundElevationDecimetres(
                                     row + 4, column))),
                    Math.Max(Math.Abs(elevation -
                                      partition.GroundElevationDecimetres(
                                          row, column - 4)),
                        Math.Abs(elevation -
                                 partition.GroundElevationDecimetres(
                                     row, column + 4))));
                var centerDistance = Math.Abs(row - centerRow) +
                                     Math.Abs(column - centerColumn);
                var score = ((long)relief << 32) +
                            ((long)elevation << 16) - centerDistance;
                if (score <= bestScore) continue;
                bestScore = score;
                bestRow = row;
                bestColumn = column;
            }
            _planning.SelectCell(bestRow, bestColumn);
        }

        private static void FocusVegetation()
        {
            var partition = _planning.Partition;
            for (var row = 0; row < partition.Rows; row += 6)
            for (var column = 0; column < partition.Columns; column += 6)
            {
                if (CountyWorldSpacePresentationPlan.StableModulo(row,
                        column, 3) == 0) continue;
                if (partition.Terrain(row, column) ==
                    PlanningTerrainClass.Forest ||
                    IsWaterside(row, column))
                {
                    _planning.SelectCell(row, column);
                    return;
                }
            }
            throw new InvalidOperationException(
                "No rendered vegetation focus cell matched.");
        }

        private static bool IsWaterside(int row, int column)
        {
            var partition = _planning.Partition;
            if (partition.WaterState(row, column) > 0) return false;
            var use = partition.LandUse(row, column);
            if (use == PlanningLandUseClass.Agriculture ||
                use == PlanningLandUseClass.Residential ||
                use == PlanningLandUseClass.Industry ||
                use == PlanningLandUseClass.Government ||
                use == PlanningLandUseClass.Military) return false;
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                var candidateRow = row + dr;
                var candidateColumn = column + dc;
                if (candidateRow < 0 || candidateRow >= partition.Rows ||
                    candidateColumn < 0 ||
                    candidateColumn >= partition.Columns) continue;
                if (partition.WaterState(candidateRow, candidateColumn) > 0)
                    return true;
            }
            return false;
        }

        private static void CreateLinearDraft(CountyPlanningPrimaryTool tool)
        {
            for (var row = 2; row < _planning.Partition.Rows - 2; row += 3)
            for (var column = 2;
                 column < _planning.Partition.Columns - 4; column += 3)
            {
                if (!_planning.PreviewDraftToolIsValid(tool, row, column,
                        row, column + 2)) continue;
                object draft = tool == CountyPlanningPrimaryTool.Road
                    ? _planning.CreateRoadDraft(row, column, row, column + 2)
                    : tool == CountyPlanningPrimaryTool.Wall
                        ? _planning.CreateWallDraft(row, column, row,
                            column + 2)
                        : _planning.CreateCanalDraft(row, column, row,
                            column + 2);
                Require(draft != null, tool + " draft");
                _planning.SelectCell(row, column + 1);
                return;
            }
            throw new InvalidOperationException("No valid " + tool +
                                                " draft path found.");
        }

        private static void SetToolbarCategory(string category)
        {
            var field = typeof(PlayableLuoyangGameController).GetField(
                "_planningToolbarCategory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(_game, category);
        }

        private static void Finish()
        {
            _planning.WorldSpacePresentation.SetDebugVisible(false);
            Require(_game.ShowCountyView(), "final clean Far handoff");
            _planning.WorldSpacePresentation.Synchronize();
            WriteMetrics();
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= DriveCapture;
            Debug.Log("LUOYANG_COUNTY_STRATEGIC_SANDBOX_V2_EVIDENCE_READY " +
                      EvidenceRoot());
        }

        private static void WriteMetrics()
        {
            var world = _planning.WorldSpacePresentation;
            var summary = world.Summary;
            var json = JsonUtility.ToJson(new EvidenceMetrics
            {
                presentation_version = summary.PresentationVersion,
                layout_fingerprint = _planning.LayoutFingerprint,
                deterministic_signature =
                    summary.DeterministicSignature.ToString(),
                terrain_chunks = summary.TerrainChunkCount,
                facilities = summary.FacilityCount,
                far_landmarks = summary.FarLandmarkCount,
                far_ordinary_suppressed =
                    summary.FarSuppressedOrdinaryFacilityCount,
                far_aggregates = summary.FarAggregateCount,
                far_aggregate_renderers = world.FarAggregateRendererCount,
                far_detail_objects =
                    world.FarOrdinaryFacilityDetailObjectCount,
                renderers = world.RendererCount,
                cold_build_milliseconds = world.LastBuildMilliseconds,
                warm_enter_milliseconds = world.LastWarmEnterMilliseconds,
                approximate_fps = Time.smoothDeltaTime > 0f
                    ? 1f / Time.smoothDeltaTime : 0f,
                world_schema = WorldState.CurrentSchemaVersion,
                derived_presentation_only = summary.IsDerivedPresentationOnly
            }, true);
            File.WriteAllText(Path.Combine(EvidenceRoot(),
                "county_strategic_sandbox_v2_metrics.json"), json);
        }

        private static void RequestResolution(int width, int height)
        {
            var method = typeof(LuoyangCountyPlanningFinalReviewMenu)
                .GetMethod("RequestGameViewResolution",
                    BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(
                "Game View resolution helper is unavailable.");
            method.Invoke(null, new object[] { width, height });
        }

        private static string EvidenceRoot() => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "LuoyangCountyStrategicSandboxVisualAndConstructionInteractionV2");

        private static void Require(bool condition, string operation)
        {
            if (!condition) throw new InvalidOperationException(
                "V2 evidence setup failed: " + operation + ".");
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryStart;
            EditorApplication.update -= DriveCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError(
                "LUOYANG_COUNTY_STRATEGIC_SANDBOX_V2_EVIDENCE_FAILED " +
                message);
        }

        [Serializable]
        private sealed class EvidenceMetrics
        {
            public string presentation_version;
            public string layout_fingerprint;
            public string deterministic_signature;
            public int terrain_chunks;
            public int facilities;
            public int far_landmarks;
            public int far_ordinary_suppressed;
            public int far_aggregates;
            public int far_aggregate_renderers;
            public int far_detail_objects;
            public int renderers;
            public double cold_build_milliseconds;
            public double warm_enter_milliseconds;
            public float approximate_fps;
            public int world_schema;
            public bool derived_presentation_only;
        }
    }
}
