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
    /// Captures the Golden Block V2 review sequence through PlayableDemo.
    /// The historical V1 comparison image is deliberately preserved rather
    /// than fabricated; phases 02-29 are generated from the current build.
    /// </summary>
    public static class LuoyangGoldenBlockBuildModeV2FinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.LuoyangGoldenBlockBuildModeV2Review.Pending";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private const string Residence =
            "facility.residential.urban_quarter";
        private const string Market = "facility.commercial.market";
        private const string Workshop = "facility.industry.workshop";
        private const string Warehouse = "facility.storage.warehouse";
        private const string Government =
            "facility.government.local_office";

        private static readonly string[] Files =
        {
            "02_golden_block_v2_overview.png",
            "03_residential_compound.png",
            "04_market_compound.png",
            "05_workshop_compound.png",
            "06_granary_compound.png",
            "07_government_compound.png",
            "08_roof_variations.png",
            "09_foundations_and_ground.png",
            "10_walls_and_gatehouses.png",
            "11_market_props.png",
            "12_workshop_props.png",
            "13_granary_loading_area.png",
            "14_residential_life_details.png",
            "15_golden_block_mid.png",
            "16_golden_block_near.png",
            "17_build_mode_grid_off.png",
            "18_build_mode_grid_on.png",
            "19_golden_block_8x8_cells.png",
            "20_cell_hover.png",
            "21_cell_selected.png",
            "22_building_ghost_residential.png",
            "23_building_ghost_government.png",
            "24_ghost_rotation.png",
            "25_multicell_footprint.png",
            "26_entrance_road_access.png",
            "27_invalid_placement.png",
            "28_draft_created.png",
            "29_build_mode_exit_grid_hidden.png"
        };

        private static PlayableLuoyangGameController _game;
        private static LuoyangCountyPlanningPresentationController _planning;
        private static int _attempts;
        private static int _phase;
        private static int _settle;
        private static int _wait;
        private static string _pendingPath;
        private static bool _prepared;

        [InitializeOnLoadMethod]
        private static void Restore()
        {
            var request = Path.Combine(EvidenceRoot(), ".capture-request");
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

        [MenuItem("Mandate/Validation/Capture Luoyang Golden Block Build Mode V2 Evidence And Review")]
        public static void CaptureAndOpenForReview()
        {
            SessionState.SetBool(PendingKey, true);
            Arm();
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
                return;
            }
            _attempts = 0;
            EditorApplication.update -= TryStart;
            EditorApplication.update += TryStart;
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
            if (dashboard.DirectGame == null || !dashboard.DirectGame.IsActive)
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
                Fail(_planning?.LastError ??
                     "Golden Block world-space renderer unavailable.");
                return;
            }
            EditorApplication.update -= TryStart;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            BeginCapture();
        }

        private static void BeginCapture()
        {
            Directory.CreateDirectory(EvidenceRoot());
            foreach (var file in Files)
            {
                var path = Path.Combine(EvidenceRoot(), file);
                if (File.Exists(path)) File.Delete(path);
            }
            _phase = 0;
            _settle = 0;
            _wait = 0;
            _pendingPath = null;
            _prepared = false;
            RequestResolution(1920, 1080);
            EditorApplication.update -= DriveCapture;
            EditorApplication.update += DriveCapture;
        }

        private static void DriveCapture()
        {
            if (!EditorApplication.isPlaying || _planning == null)
            {
                Fail("Play Mode ended during Golden Block V2 capture.");
                return;
            }
            if (Screen.width != 1920 || Screen.height != 1080) return;
            if (!string.IsNullOrWhiteSpace(_pendingPath))
            {
                if (File.Exists(_pendingPath) &&
                    new FileInfo(_pendingPath).Length > 10_000)
                {
                    _pendingPath = null;
                    _phase++;
                    _prepared = false;
                    _settle = 0;
                    if (_phase >= Files.Length) Finish();
                }
                else if (++_wait > 600)
                    Fail("Screenshot was not written: " + _pendingPath);
                return;
            }
            if (!_prepared)
            {
                try
                {
                    PreparePhase(_phase);
                    _planning.WorldSpacePresentation.Synchronize();
                    _prepared = true;
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
            _planning.WorldSpacePresentation.Synchronize();
            _pendingPath = Path.Combine(EvidenceRoot(), Files[_phase]);
            _wait = 0;
            ScreenCapture.CaptureScreenshot(_pendingPath);
        }

        private static void PreparePhase(int phase)
        {
            _planning.WorldSpacePresentation.SetDebugVisible(false);
            _planning.CancelPlanningTool();
            _planning.SetOverlayVisible("roads", true);
            _planning.SetOverlayVisible("rivers", true);
            _planning.SetOverlayVisible("fortifications", true);
            _planning.SetOverlayVisible("planning", true);
            _planning.SetOverlayVisible("grid", false);

            switch (phase)
            {
                case 0:
                case 6:
                case 13:
                    FocusGoldenBlockMid();
                    if (phase == 6)
                        Require(_planning.RotateViewportByGuiDelta(
                                new Vector2(56f, 0f)),
                            "roof silhouette review angle");
                    break;
                case 7:
                    FocusLot(CountyGoldenBlockArchetype.WarehouseCompound,
                        true);
                    break;
                case 8:
                    FocusLot(CountyGoldenBlockArchetype.CivicCourtyard, true);
                    break;
                case 1:
                case 12:
                    FocusLot(CountyGoldenBlockArchetype.ResidenceCourtyard,
                        true);
                    break;
                case 2:
                case 9:
                    FocusLot(CountyGoldenBlockArchetype.MarketFrontage,
                        true);
                    break;
                case 3:
                case 10:
                    FocusLot(CountyGoldenBlockArchetype.WorkshopYard, true);
                    break;
                case 4:
                case 11:
                    FocusLot(CountyGoldenBlockArchetype.WarehouseCompound,
                        true);
                    break;
                case 5:
                    FocusLot(CountyGoldenBlockArchetype.CivicCourtyard, true);
                    break;
                case 14:
                    FocusLot(CountyGoldenBlockArchetype.ResidenceCourtyard,
                        true);
                    break;
                case 15:
                    EnterBuildMode();
                    _planning.SetOverlayVisible("grid", false);
                    break;
                case 16:
                    EnterBuildMode();
                    break;
                case 17:
                    EnterBuildMode();
                    _planning.ZoomViewport(1f,
                        new Vector2(0.5f, 0.5f));
                    _planning.ZoomViewport(1f,
                        new Vector2(0.5f, 0.5f));
                    break;
                case 18:
                    EnterBuildMode();
                    var hoverPlan = _planning.WorldSpacePresentation
                        .GoldenBlockPlan;
                    _planning.SelectCell(hoverPlan.MinimumRow - 4,
                        hoverPlan.MinimumColumn + 1);
                    _planning.SetHoveredPlanningCell(
                        hoverPlan.MinimumRow - 3,
                        hoverPlan.MinimumColumn + 3);
                    break;
                case 19:
                    EnterBuildMode();
                    var selectedPlan = _planning.WorldSpacePresentation
                        .GoldenBlockPlan;
                    _planning.SelectCell(selectedPlan.MinimumRow - 3,
                        selectedPlan.MinimumColumn + 3);
                    _planning.ClearHoveredPlanningCell();
                    break;
                case 20:
                    EnterBuildMode();
                    PrepareValidGhost(Residence);
                    break;
                case 21:
                    EnterBuildMode();
                    PrepareValidGhost(Government);
                    break;
                case 22:
                    EnterBuildMode();
                    PrepareValidGhost(Market);
                    _planning.RotateClockwise();
                    break;
                case 23:
                case 24:
                    EnterBuildMode();
                    PrepareValidGhost(Market);
                    _planning.RotateClockwise();
                    _planning.RotateClockwise();
                    break;
                case 25:
                    EnterBuildMode();
                    Require(_planning.SelectFixture(
                            CountyPlanningFixture.ExistingFacilityCollision),
                        "invalid collision fixture");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 26:
                    EnterBuildMode();
                    PrepareValidGhost(Residence);
                    Require(_planning.CreateDraft() != null,
                        "building Draft");
                    break;
                case 27:
                    _planning.CancelPlanningTool();
                    _planning.SetOverlayVisible("grid", false);
                    FocusGoldenBlockMid();
                    break;
            }
        }

        private static void EnterBuildMode()
        {
            Require(_game.ShowCountyPlanningSubView(),
                "formal county planning route");
            Require(_planning.FocusGoldenBlockBuildMode(),
                "Golden Block Build Mode");
            Require(_planning.SetOverlayVisible("grid", true),
                "formal 50m grid");
            SetToolbarCategory("building");
        }

        private static void FocusLot(CountyGoldenBlockArchetype archetype,
            bool near)
        {
            Require(_game.ShowCountyUrbanAreaView(),
                "formal county urban-area route");
            Require(_planning.FocusGoldenBlockLot(archetype, near),
                archetype + " compound");
        }

        private static void FocusGoldenBlockMid()
        {
            Require(_game.ShowCountyUrbanAreaView(),
                "formal county urban-area route");
            Require(_planning.FocusGoldenBlockPrototype(),
                "Golden Block Mid");
        }

        private static void PrepareValidGhost(string definitionId)
        {
            var profile = _planning.PlayerFacingBuildingProfiles.First(item =>
                string.Equals(item.FacilityDefinitionId, definitionId,
                    StringComparison.Ordinal));
            Require(_planning.SelectProfile(profile.ProfileId),
                definitionId + " profile");
            var golden = _planning.WorldSpacePresentation.GoldenBlockPlan;
            var centerRow = (golden.MinimumRow + golden.MaximumRow) / 2;
            var centerColumn = (golden.MinimumColumn + golden.MaximumColumn) /
                               2;
            for (var radius = 0; radius <= 48; radius++)
            for (var row = centerRow - radius; row <= centerRow + radius;
                 row++)
            for (var column = centerColumn - radius;
                 column <= centerColumn + radius; column++)
            {
                if (row < 0 || row >= _planning.Partition.Rows || column < 0 ||
                    column >= _planning.Partition.Columns) continue;
                if (Math.Max(Math.Abs(row - centerRow),
                        Math.Abs(column - centerColumn)) != radius) continue;
                _planning.SelectCell(row, column);
                if (_planning.Validation == null ||
                    !_planning.Validation.IsValid) continue;
                _planning.SetHoveredPlanningCell(row, column);
                return;
            }
            throw new InvalidOperationException(
                "No valid Golden Block-adjacent placement for " +
                definitionId + ".");
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
            _planning.CancelPlanningTool();
            _planning.SetOverlayVisible("grid", false);
            FocusGoldenBlockMid();
            _planning.WorldSpacePresentation.SetDebugVisible(false);
            _planning.WorldSpacePresentation.Synchronize();
            WriteMetrics();
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= DriveCapture;
            var before = Path.Combine(EvidenceRoot(),
                "01_golden_block_v1_before.png");
            if (!File.Exists(before))
                Debug.LogWarning(
                    "GOLDEN_BLOCK_V2_BASELINE_REQUIRED: add the genuine V1 " +
                    "same-camera image as 01_golden_block_v1_before.png; " +
                    "the capture tool will not fabricate historical evidence.");
            Debug.Log("LUOYANG_GOLDEN_BLOCK_BUILD_MODE_V2_EVIDENCE_READY " +
                      EvidenceRoot());
        }

        private static void WriteMetrics()
        {
            var world = _planning.WorldSpacePresentation;
            var json = JsonUtility.ToJson(new EvidenceMetrics
            {
                profile_count = CountyBuildingPresentationProfileCatalog
                    .HanLuoyangV2.Profiles.Count,
                formal_cell_count =
                    CountyGoldenBlockPresentationPlan.BlockSizeCells *
                    CountyGoldenBlockPresentationPlan.BlockSizeCells,
                presentation_lot_count = world.GoldenBlockPlan.Lots.Count,
                visible_modules = world.GoldenBlockVisibleModuleCount,
                visible_props = world.GoldenBlockPropCount,
                vegetation_instances =
                    world.GoldenBlockVegetationInstanceCount,
                triangles = world.GoldenBlockTriangleCount,
                materials = world.GoldenBlockMaterialCount,
                approximate_fps = Time.smoothDeltaTime > 0f
                    ? 1f / Time.smoothDeltaTime : 0f,
                world_schema = WorldState.CurrentSchemaVersion,
                derived_presentation_only = true,
                generated_screenshot_count = Files.Length,
                historical_before_present = File.Exists(Path.Combine(
                    EvidenceRoot(), "01_golden_block_v1_before.png"))
            }, true);
            File.WriteAllText(Path.Combine(EvidenceRoot(),
                "golden_block_build_mode_v2_metrics.json"), json);
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
            "LuoyangGoldenBlockBuildingArtAnd50mBuildModeV2");

        private static void Require(bool condition, string operation)
        {
            if (!condition) throw new InvalidOperationException(
                "Golden Block V2 evidence setup failed: " + operation + ".");
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryStart;
            EditorApplication.update -= DriveCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError(
                "LUOYANG_GOLDEN_BLOCK_BUILD_MODE_V2_EVIDENCE_FAILED " +
                message);
        }

        [Serializable]
        private sealed class EvidenceMetrics
        {
            public int profile_count;
            public int formal_cell_count;
            public int presentation_lot_count;
            public int visible_modules;
            public int visible_props;
            public int vegetation_instances;
            public int triangles;
            public int materials;
            public float approximate_fps;
            public int world_schema;
            public bool derived_presentation_only;
            public int generated_screenshot_count;
            public bool historical_before_present;
        }
    }
}
