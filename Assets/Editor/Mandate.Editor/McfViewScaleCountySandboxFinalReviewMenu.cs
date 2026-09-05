using System;
using System.IO;
using System.Reflection;
using Mandate.Domain;
using Mandate.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    public static class McfViewScaleCountySandboxFinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.McfViewScaleCountySandboxReview.Pending";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private static readonly string[] Files =
        {
            "01_world_strategic_default.png",
            "02_world_strategic_transport_overlay.png",
            "03_county_before_lod_rework.png",
            "04_county_far_after.png",
            "05_county_mid_after.png",
            "06_county_near_after.png",
            "07_county_facility_aggregate.png",
            "08_county_facility_detail.png",
            "09_county_road_lod_far.png",
            "10_county_road_lod_mid.png",
            "11_county_fortification_far.png",
            "12_county_fortification_near.png",
            "13_county_grid_planning_only.png",
            "14_map_legend.png",
            "15_map_overlay_controls.png",
            "16_person_view_no_grid.png"
        };

        private static PlayableLuoyangGameController _game;
        private static int _attempts;
        private static int _phase;
        private static int _settle;
        private static int _wait;
        private static string _pendingPath;
        private static int _requestedWidth;
        private static int _requestedHeight;

        [InitializeOnLoadMethod]
        private static void Restore()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (EditorApplication.isPlaying)
                EditorApplication.update += TryStart;
        }

        [MenuItem("Mandate/Validation/Capture MCF County LOD Evidence And Review")]
        public static void CaptureAndOpenForReview()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(ScenePath,
                    OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
            else
            {
                _attempts = 0;
                EditorApplication.update -= TryStart;
                EditorApplication.update += TryStart;
            }
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
                    Fail("Could not start the formal Luoyang game.");
                    return;
                }
            }
            _game = dashboard.DirectGame;
            EditorApplication.update -= TryStart;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            BeginCapture();
        }

        private static void BeginCapture()
        {
            var root = EvidenceRoot();
            Directory.CreateDirectory(root);
            foreach (var file in Files)
            {
                var path = Path.Combine(root, file);
                if (File.Exists(path)) File.Delete(path);
            }
            _phase = 0;
            _settle = 6;
            _wait = 0;
            _pendingPath = null;
            _requestedWidth = 0;
            _requestedHeight = 0;
            EditorApplication.update -= DriveCapture;
            EditorApplication.update += DriveCapture;
        }

        private static void DriveCapture()
        {
            if (!EditorApplication.isPlaying || _game == null)
            {
                Fail("Play Mode ended during M/C/F evidence capture.");
                return;
            }
            var width = _phase < 4 ? 1280 : 1920;
            var height = _phase < 4 ? 720 : 1080;
            if (_requestedWidth != width || _requestedHeight != height)
            {
                RequestResolution(width, height);
                _requestedWidth = width;
                _requestedHeight = height;
                _settle = 8;
                return;
            }
            if (Screen.width != width || Screen.height != height) return;
            if (_phase == 2 && string.IsNullOrWhiteSpace(_pendingPath))
            {
                CopyBeforeImage();
                _phase++;
                _settle = 5;
                return;
            }
            if (!string.IsNullOrWhiteSpace(_pendingPath))
            {
                if (File.Exists(_pendingPath) &&
                    new FileInfo(_pendingPath).Length > 10_000)
                {
                    _pendingPath = null;
                    _phase++;
                    _settle = 5;
                    if (_phase >= Files.Length)
                    {
                        Finish();
                        return;
                    }
                }
                else if (++_wait > 600)
                    Fail("Screenshot was not written: " + _pendingPath);
                return;
            }
            if (_settle-- > 0) return;
            try
            {
                PreparePhase(_phase);
                _pendingPath = Path.Combine(EvidenceRoot(), Files[_phase]);
                _wait = 0;
                ScreenCapture.CaptureScreenshot(_pendingPath);
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        private static void PreparePhase(int phase)
        {
            switch (phase)
            {
                case 0:
                    Require(_game.ShowWorldView(), "M world default");
                    _game.NaturalMap.SetTransportOverlayVisible(false);
                    break;
                case 1:
                    _game.NaturalMap.SetTransportOverlayVisible(true);
                    break;
                case 3:
                case 8:
                case 10:
                    Require(_game.ShowCountyView(), "C county far");
                    break;
                case 4:
                case 6:
                case 9:
                    Require(_game.ShowCountyUrbanAreaView(),
                        "C county mid");
                    break;
                case 5:
                case 7:
                case 11:
                case 12:
                case 13:
                case 14:
                    Require(_game.ShowCountyPlanningSubView(),
                        "C county near");
                    _game.CountyPlanning.SetOverlayVisible("grid", true);
                    _game.CountyPlanning.SetOverlayVisible(
                        "fortifications", true);
                    _game.CountyPlanning.SetOverlayVisible("planning", true);
                    break;
                case 15:
                    Require(_game.ShowPersonView(), "F person");
                    break;
            }
        }

        private static void CopyBeforeImage()
        {
            var source = Path.Combine(Directory.GetCurrentDirectory(),
                "Docs", "Evidence",
                "LuoyangCountyVisualConstructionInteractionReworkV1",
                "01_map_legend_and_overlays.png");
            if (!File.Exists(source))
                throw new FileNotFoundException(
                    "The preserved county pre-LOD evidence is missing.",
                    source);
            File.Copy(source, Path.Combine(EvidenceRoot(), Files[2]), true);
        }

        private static void Finish()
        {
            Require(_game.ShowCountyView(),
                "final C county overview handoff");
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= DriveCapture;
            Debug.Log("MCF_COUNTY_SANDBOX_PRESENTATION_LOD_EVIDENCE_READY " +
                      EvidenceRoot());
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
            "McfViewScaleCountySandboxPresentationLodV1");

        private static void Require(bool condition, string operation)
        {
            if (!condition) throw new InvalidOperationException(
                "MCF evidence setup failed: " + operation + ".");
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryStart;
            EditorApplication.update -= DriveCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError("MCF_COUNTY_SANDBOX_PRESENTATION_LOD_FAILED " +
                           message);
        }
    }
}
