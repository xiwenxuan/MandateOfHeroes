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
    public static class LuoyangCountyWorldSpaceFinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.LuoyangCountyWorldSpaceReview.Pending";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private static readonly string[] Files =
        {
            "01_legacy_county_debug_map.png",
            "02_county_worldspace_far.png",
            "03_county_worldspace_mid.png",
            "04_county_worldspace_near.png",
            "05_county_terrain_relief.png",
            "06_county_river_worldspace.png",
            "07_county_canal_worldspace.png",
            "08_county_road_main.png",
            "09_county_road_junction.png",
            "10_county_urban_far.png",
            "11_county_facility_mid.png",
            "12_county_facility_near.png",
            "13_county_facility_fallback.png",
            "14_county_wall_far.png",
            "15_county_wall_gate_mid.png",
            "16_county_agriculture.png",
            "17_county_village.png",
            "18_county_vegetation.png",
            "19_county_planning_grid_local.png",
            "20_county_building_ghost.png",
            "21_county_road_draft.png",
            "22_county_wall_draft.png",
            "23_county_canal_draft.png",
            "24_county_debug_overlay.png"
        };

        private static PlayableLuoyangGameController _game;
        private static LuoyangCountyPlanningPresentationController _planning;
        private static int _attempts;
        private static int _phase;
        private static int _settle;
        private static int _wait;
        private static string _pendingPath;
        private static (int Row, int Column) _roadStart;
        private static (int Row, int Column) _wallStart;
        private static (int Row, int Column) _canalStart;

        [InitializeOnLoadMethod]
        private static void Restore()
        {
            var request = Path.Combine(Directory.GetCurrentDirectory(),
                "Docs", "Evidence",
                "LuoyangCountyWorldSpaceSandboxPresentationV1",
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

        [MenuItem("Mandate/Validation/Capture Luoyang County World-Space Evidence And Review")]
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
                    Fail("Could not start the formal Luoyang game.");
                    return;
                }
            }
            _game = dashboard.DirectGame;
            if (!_game.ShowCountyView())
            {
                if (++_attempts < 900) return;
                Fail("Could not enter the formal Luoyang county view.");
                return;
            }
            _planning = _game.CountyPlanning;
            if (_planning?.WorldSpacePresentation == null ||
                !_planning.WorldSpacePresentation.IsBuilt)
            {
                if (++_attempts < 900) return;
                Fail(_planning?.LastError ??
                     "World-space county renderer did not initialize.");
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
            _settle = 8;
            _wait = 0;
            _pendingPath = null;
            EditorApplication.update -= DriveCapture;
            EditorApplication.update += DriveCapture;
        }

        private static void DriveCapture()
        {
            if (!EditorApplication.isPlaying || _planning == null)
            {
                Fail("Play Mode ended during world-space evidence capture.");
                return;
            }
            if (_phase == 0)
            {
                CopyLegacyImage();
                _phase++;
                _settle = 6;
                return;
            }
            if (_settle-- > 0) return;
            try
            {
                PreparePhase(_phase);
                _planning.WorldSpacePresentation.Synchronize();
                CapturePresentationCamera(Path.Combine(EvidenceRoot(),
                    Files[_phase]));
                _phase++;
                _settle = 2;
                if (_phase >= Files.Length) Finish();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        private static void CapturePresentationCamera(string path)
        {
            var camera = _game.NaturalMap.PresentationCamera;
            if (camera == null) throw new InvalidOperationException(
                "Formal presentation camera is unavailable.");
            var priorTarget = camera.targetTexture;
            var priorRect = camera.rect;
            var priorActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f),
                    0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = priorTarget;
                camera.rect = priorRect;
                RenderTexture.active = priorActive;
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
            if (!File.Exists(path) || new FileInfo(path).Length <= 10_000)
                throw new IOException("Camera evidence was not written: " +
                                      path);
        }

        private static void PreparePhase(int phase)
        {
            var world = _planning.WorldSpacePresentation;
            world.SetFallbackEvidenceVisible(false);
            world.SetDebugVisible(false);
            SetBaseOverlays();
            switch (phase)
            {
                case 1:
                    Require(_game.ShowCountyView(), "county Far");
                    break;
                case 2:
                    Require(_game.ShowCountyUrbanAreaView(), "county Mid");
                    break;
                case 3:
                    Require(_game.ShowCountyPlanningSubView(), "county Near");
                    break;
                case 4:
                    Require(_game.ShowCountyUrbanAreaView(), "terrain relief");
                    FocusTerrain(PlanningTerrainClass.Hill);
                    break;
                case 5:
                    Require(_game.ShowCountyUrbanAreaView(), "river");
                    FocusFirst((row, column) =>
                        _planning.Partition.WaterState(row, column) > 0);
                    break;
                case 6:
                    Require(_game.ShowCountyUrbanAreaView(), "canal");
                    var canal = _planning.LayoutPackage.CanalEdges.First();
                    _planning.SelectCell(canal.FromLocalRow,
                        canal.FromLocalColumn);
                    break;
                case 7:
                    Require(_game.ShowCountyUrbanAreaView(), "main road");
                    var road = _planning.PresentationStack.Roads.First(item =>
                        item.PresentationClass <=
                        CountyRoadPresentationClass.CountyMainR1);
                    _planning.SelectCell(road.Edge.FromLocalRow,
                        road.Edge.FromLocalColumn);
                    break;
                case 8:
                    Require(_game.ShowCountyPlanningSubView(), "junction");
                    FocusRoadJunction();
                    break;
                case 9:
                    Require(_game.ShowCountyView(), "urban Far");
                    break;
                case 10:
                    Require(_game.ShowCountyUrbanAreaView(), "facility Mid");
                    FocusUrbanFacility();
                    break;
                case 11:
                    Require(_game.ShowCountyPlanningSubView(),
                        "facility Near");
                    FocusUrbanFacility();
                    break;
                case 12:
                    Require(_game.ShowCountyPlanningSubView(), "fallback");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence),
                        "fallback evidence focus");
                    world.SetFallbackEvidenceVisible(true);
                    break;
                case 13:
                    Require(_game.ShowCountyView(), "wall Far");
                    break;
                case 14:
                    Require(_game.ShowCountyUrbanAreaView(), "wall gate Mid");
                    var gate = _planning.LayoutPackage.Fortifications
                        .First(item => item.IsGate);
                    _planning.SelectCell(gate.LocalRow, gate.LocalColumn);
                    break;
                case 15:
                    Require(_game.ShowCountyPlanningSubView(), "agriculture");
                    FocusFacility(item => IsAgriculture(item));
                    break;
                case 16:
                    Require(_game.ShowCountyUrbanAreaView(), "village");
                    FocusVillageFacility();
                    break;
                case 17:
                    Require(_game.ShowCountyUrbanAreaView(), "vegetation");
                    FocusTerrain(PlanningTerrainClass.Forest);
                    break;
                case 18:
                    Require(_game.ShowCountyPlanningSubView(), "local grid");
                    _planning.SetOverlayVisible("grid", true);
                    break;
                case 19:
                    Require(_game.ShowCountyPlanningSubView(), "ghost");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence), "valid ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 20:
                    Require(_game.ShowCountyPlanningSubView(), "road draft");
                    _roadStart = FindLinear(CountyPlanningPrimaryTool.Road);
                    Require(_planning.CreateRoadDraft(_roadStart.Row,
                        _roadStart.Column, _roadStart.Row,
                        _roadStart.Column + 2) != null, "road draft");
                    break;
                case 21:
                    Require(_game.ShowCountyPlanningSubView(), "wall draft");
                    _wallStart = FindLinear(CountyPlanningPrimaryTool.Wall);
                    Require(_planning.CreateWallDraft(_wallStart.Row,
                        _wallStart.Column, _wallStart.Row,
                        _wallStart.Column + 2) != null, "wall draft");
                    break;
                case 22:
                    Require(_game.ShowCountyPlanningSubView(), "canal draft");
                    _canalStart = FindLinear(CountyPlanningPrimaryTool.Canal);
                    Require(_planning.CreateCanalDraft(_canalStart.Row,
                        _canalStart.Column, _canalStart.Row,
                        _canalStart.Column + 2) != null, "canal draft");
                    break;
                case 23:
                    Require(_game.ShowCountyUrbanAreaView(), "debug overlay");
                    world.SetDebugVisible(true);
                    break;
            }
        }

        private static void SetBaseOverlays()
        {
            _planning.SetOverlayVisible("roads", true);
            _planning.SetOverlayVisible("rivers", true);
            _planning.SetOverlayVisible("fortifications", true);
            _planning.SetOverlayVisible("planning", true);
            _planning.SetOverlayVisible("grid", true);
        }

        private static void FocusUrbanFacility()
        {
            var area = _planning.LayoutPackage.UrbanAreaCandidate;
            FocusFacility(item => item.LocalRow >= area.MinimumRow &&
                                  item.LocalRow <= area.MaximumRow &&
                                  item.LocalColumn >= area.MinimumColumn &&
                                  item.LocalColumn <= area.MaximumColumn);
        }

        private static void FocusVillageFacility()
        {
            var area = _planning.LayoutPackage.UrbanAreaCandidate;
            FocusFacility(item => item.LocalRow < area.MinimumRow ||
                                  item.LocalRow > area.MaximumRow ||
                                  item.LocalColumn < area.MinimumColumn ||
                                  item.LocalColumn > area.MaximumColumn);
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

        private static bool IsAgriculture(Luoyang50mLayoutFacility item) =>
            string.Equals(item.CategoryId, "agriculture",
                StringComparison.Ordinal) ||
            item.DefinitionId.IndexOf("agriculture",
                StringComparison.Ordinal) >= 0;

        private static void FocusTerrain(PlanningTerrainClass terrain)
        {
            FocusFirst((row, column) =>
                _planning.Partition.Terrain(row, column) == terrain);
        }

        private static void FocusFirst(Func<int, int, bool> predicate)
        {
            for (var row = 0; row < _planning.Partition.Rows; row += 2)
            for (var column = 0; column < _planning.Partition.Columns;
                 column += 2)
                if (predicate(row, column))
                {
                    _planning.SelectCell(row, column);
                    return;
                }
            throw new InvalidOperationException(
                "No county presentation focus cell matched.");
        }

        private static void FocusRoadJunction()
        {
            var degrees = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var edge in _planning.LayoutPackage.RoadEdges)
            {
                degrees[edge.FromNodeId] = degrees.TryGetValue(edge.FromNodeId,
                    out var first) ? first + 1 : 1;
                degrees[edge.ToNodeId] = degrees.TryGetValue(edge.ToNodeId,
                    out var second) ? second + 1 : 1;
            }
            var node = _planning.LayoutPackage.RoadNodes.First(item =>
                degrees.TryGetValue(item.NodeId, out var degree) &&
                degree >= 3);
            _planning.SelectCell(node.LocalRow, node.LocalColumn);
        }

        private static (int Row, int Column) FindLinear(
            CountyPlanningPrimaryTool tool)
        {
            for (var row = 2; row < 318; row += 3)
            for (var column = 2; column < 636; column += 3)
                if (_planning.PreviewDraftToolIsValid(tool, row, column,
                        row, column + 2))
                    return (row, column);
            throw new InvalidOperationException(
                "No valid county draft path for " + tool + ".");
        }

        private static void CopyLegacyImage()
        {
            var source = Path.Combine(Directory.GetCurrentDirectory(),
                "Docs", "Evidence",
                "LuoyangCountyVisualConstructionInteractionReworkV1",
                "01_map_legend_and_overlays.png");
            if (!File.Exists(source)) throw new FileNotFoundException(
                "Preserved legacy county image is missing.", source);
            File.Copy(source, Path.Combine(EvidenceRoot(), Files[0]), true);
        }

        private static void Finish()
        {
            _planning.WorldSpacePresentation.SetFallbackEvidenceVisible(false);
            _planning.WorldSpacePresentation.SetDebugVisible(false);
            SetBaseOverlays();
            Require(_game.ShowCountyUrbanAreaView(),
                "final county Mid review handoff");
            var area = _planning.LayoutPackage.UrbanAreaCandidate;
            _planning.SelectCell((area.MinimumRow + area.MaximumRow) / 2,
                (area.MinimumColumn + area.MaximumColumn) / 2);
            _planning.WorldSpacePresentation.Synchronize();
            WriteMetrics();
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= DriveCapture;
            Debug.Log("LUOYANG_COUNTY_WORLDSPACE_EVIDENCE_READY " +
                      EvidenceRoot());
        }

        private static void WriteMetrics()
        {
            var world = _planning.WorldSpacePresentation;
            var summary = world.Summary;
            var json = JsonUtility.ToJson(new EvidenceMetrics
            {
                presentation_version = summary.PresentationVersion,
                cache_key = summary.CacheKey,
                deterministic_signature = summary.DeterministicSignature
                    .ToString(),
                terrain_chunks = summary.TerrainChunkCount,
                terrain_vertices = summary.TerrainVertexCount,
                facilities = summary.FacilityCount,
                model_resolved_facilities =
                    summary.ModelResolvedFacilityCount,
                roads = summary.RoadSegmentCount,
                road_junctions = summary.RoadJunctionCount,
                canals = summary.CanalSegmentCount,
                fortifications = summary.FortificationSegmentCount,
                gates = summary.GateCount,
                renderers = world.RendererCount,
                detailed_facility_objects = world.DetailedFacilityObjectCount,
                planning_cell_game_objects =
                    summary.PlanningCellGameObjectCount,
                cold_build_milliseconds = world.LastBuildMilliseconds,
                warm_enter_milliseconds = world.LastWarmEnterMilliseconds,
                approximate_fps = Time.smoothDeltaTime > 0f
                    ? 1f / Time.smoothDeltaTime : 0f,
                urban_candidate_hull_default_visible =
                    summary.UrbanCandidateHullVisibleByDefault,
                derived_presentation_only = summary.IsDerivedPresentationOnly
            }, true);
            File.WriteAllText(Path.Combine(EvidenceRoot(),
                "county_worldspace_metrics.json"), json);
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
            "LuoyangCountyWorldSpaceSandboxPresentationV1");

        private static void Require(bool condition, string operation)
        {
            if (!condition) throw new InvalidOperationException(
                "County world-space evidence setup failed: " +
                operation + ".");
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryStart;
            EditorApplication.update -= DriveCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError("LUOYANG_COUNTY_WORLDSPACE_EVIDENCE_FAILED " +
                           message);
        }

        [Serializable]
        private sealed class EvidenceMetrics
        {
            public string presentation_version;
            public string cache_key;
            public string deterministic_signature;
            public int terrain_chunks;
            public int terrain_vertices;
            public int facilities;
            public int model_resolved_facilities;
            public int roads;
            public int road_junctions;
            public int canals;
            public int fortifications;
            public int gates;
            public int renderers;
            public int detailed_facility_objects;
            public int planning_cell_game_objects;
            public double cold_build_milliseconds;
            public double warm_enter_milliseconds;
            public float approximate_fps;
            public bool urban_candidate_hull_default_visible;
            public bool derived_presentation_only;
        }
    }
}
