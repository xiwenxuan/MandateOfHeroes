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
    public static class LuoyangCountyPlanningFinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.LuoyangCountyPlanningFinalReview.Pending";
        private const string CaptureKey =
            "Mandate.LuoyangCountyPlanningFinalReview.Capture";
        private const string ResolutionRetryKey =
            "Mandate.LuoyangCountyPlanningFinalReview.ResolutionRetry";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private static readonly string[] EvidenceFiles =
        {
            "01_map_legend_and_overlays.png",
            "02_admin_boundary_lod_far.png",
            "03_admin_boundary_lod_near.png",
            "04_construction_bottom_toolbar.png",
            "05_building_ghost_valid.png",
            "06_building_ghost_invalid.png",
            "07_continuous_building_placement.png",
            "08_road_drag_preview.png",
            "09_wall_edge_drag_preview.png",
            "10_canal_drag_preview.png",
            "11_zone_brush.png",
            "12_draft_move.png",
            "13_draft_copy_eyedropper.png",
            "14_draft_demolish.png",
            "15_undo_redo.png",
            "16_road_overlay.png",
            "17_terrain_overlay.png",
            "18_input_camera_construction.png",
            "19_recommended_1920x1080_layout.png"
        };
        private static int _attempts;
        private static int _capturePhase;
        private static int _settleFrames;
        private static int _screenshotWaitFrames;
        private static int _resolutionWaitFrames;
        private static int _requestedWidth;
        private static int _requestedHeight;
        private static string _pendingScreenshot;
        private static PlayableLuoyangGameController _game;
        private static LuoyangCountyPlanningPresentationController _planning;
        private static DraftBuildingBlueprint _firstBuilding;
        private static DraftBuildingBlueprint _copiedBuilding;
        private static (int Row, int Column) _roadStart;
        private static (int Row, int Column) _wallStart;
        private static (int Row, int Column) _canalStart;

        [InitializeOnLoadMethod]
        private static void RestorePendingReview()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                if (ShouldRetryCommandLineEvidence())
                    EditorApplication.delayCall +=
                        CaptureEvidenceAndOpenForReview;
                return;
            }
            Arm();
            if (EditorApplication.isPlaying)
                EditorApplication.update += TryEnterPlanning;
        }

        [MenuItem("Mandate/Validation/Open Luoyang County Planning Review")]
        public static void OpenForReview()
        {
            SessionState.SetBool(PendingKey, true);
            Arm();
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(ScenePath,
                    OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
            else
            {
                _attempts = 0;
                EditorApplication.update -= TryEnterPlanning;
                EditorApplication.update += TryEnterPlanning;
            }
        }

        [MenuItem("Mandate/Validation/Capture Luoyang Planning Evidence And Review")]
        public static void CaptureEvidenceAndOpenForReview()
        {
            SessionState.SetBool(CaptureKey, true);
            OpenForReview();
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
            EditorApplication.update -= TryEnterPlanning;
            EditorApplication.update += TryEnterPlanning;
        }

        private static void TryEnterPlanning()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= TryEnterPlanning;
                return;
            }
            _attempts++;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            if (dashboard == null)
            {
                if (_attempts < 900) return;
                Fail("PlayableDemo did not create SimulationDashboard.");
                return;
            }
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
            {
                if (!dashboard.StartRecommendedLuoyangExperience())
                {
                    if (_attempts < 900) return;
                    Fail("PlayableDemo could not start the Luoyang game.");
                    return;
                }
            }
            var game = dashboard.DirectGame;
            if (!game.ShowWorldView() ||
                !game.EnterLuoyangCountyPlanningForTests())
            {
                if (_attempts < 900) return;
                Fail("Formal Luoyang county planning route failed.");
                return;
            }
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryEnterPlanning;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (SessionState.GetBool(CaptureKey, false))
                BeginEvidenceCapture(game);
            Debug.Log("LUOYANG_COUNTY_PLANNING_FINAL_REVIEW_READY");
        }

        private static void BeginEvidenceCapture(
            PlayableLuoyangGameController game)
        {
            _game = game;
            _planning = game.CountyPlanning;
            _capturePhase = 0;
            _settleFrames = 4;
            _screenshotWaitFrames = 0;
            _resolutionWaitFrames = 0;
            _requestedWidth = 0;
            _requestedHeight = 0;
            _pendingScreenshot = null;
            _firstBuilding = null;
            _copiedBuilding = null;
            var root = EvidenceRoot();
            Directory.CreateDirectory(root);
            foreach (var file in EvidenceFiles)
            {
                var path = Path.Combine(root, file);
                if (File.Exists(path)) File.Delete(path);
            }
            RequestGameViewResolution(1280, 720);
            EditorApplication.update -= DriveEvidenceCapture;
            EditorApplication.update += DriveEvidenceCapture;
        }

        private static void DriveEvidenceCapture()
        {
            if (!EditorApplication.isPlaying || _planning == null)
            {
                CaptureFail("Play Mode ended during evidence capture.");
                return;
            }
            var targetWidth = _capturePhase < 18 ? 1280 : 1920;
            var targetHeight = _capturePhase < 18 ? 720 : 1080;
            if (_requestedWidth != targetWidth ||
                _requestedHeight != targetHeight)
                RequestGameViewResolution(targetWidth, targetHeight);
            if (Screen.width != targetWidth || Screen.height != targetHeight)
            {
                if (++_resolutionWaitFrames > 600)
                    CaptureFail("Game View resolution did not become " +
                                targetWidth + "x" + targetHeight +
                                "; actual=" + Screen.width + "x" +
                                Screen.height + ".");
                return;
            }
            if (!string.IsNullOrWhiteSpace(_pendingScreenshot))
            {
                _screenshotWaitFrames++;
                if (File.Exists(_pendingScreenshot) &&
                    new FileInfo(_pendingScreenshot).Length > 10_000)
                {
                    _pendingScreenshot = null;
                    _capturePhase++;
                    _settleFrames = 4;
                    if (_capturePhase >= EvidenceFiles.Length)
                    {
                        SessionState.SetBool(CaptureKey, false);
                        EditorApplication.update -= DriveEvidenceCapture;
                        Debug.Log(
                            "LUOYANG_COUNTY_VISUAL_CONSTRUCTION_EVIDENCE_READY " +
                            EvidenceRoot());
                    }
                }
                else if (_screenshotWaitFrames > 600)
                    CaptureFail("Game View screenshot was not written: " +
                                _pendingScreenshot);
                return;
            }
            if (_settleFrames-- > 0) return;
            try
            {
                PrepareEvidencePhase(_capturePhase);
                _pendingScreenshot = Path.Combine(EvidenceRoot(),
                    EvidenceFiles[_capturePhase]);
                _screenshotWaitFrames = 0;
                ScreenCapture.CaptureScreenshot(_pendingScreenshot);
            }
            catch (Exception exception)
            {
                CaptureFail(exception.ToString());
            }
        }

        private static void PrepareEvidencePhase(int phase)
        {
            switch (phase)
            {
                case 0:
                    SetBaseOverlays();
                    break;
                case 1:
                    SetBaseOverlays();
                    break;
                case 2:
                    Require(_planning.ZoomViewport(1f,
                        new Vector2(0.5f, 0.5f)), "near zoom");
                    break;
                case 3:
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 4:
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence),
                        "valid building ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 5:
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ExistingFacilityCollision),
                        "invalid building ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
                case 6:
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence),
                        "continuous building start");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    _firstBuilding = _planning.CreateDraft();
                    Require(_firstBuilding != null, "first building draft");
                    Require(CreateAnotherBuilding() != null,
                        "second building draft");
                    break;
                case 7:
                    _roadStart = FindLinearPreview(
                        CountyPlanningPrimaryTool.Road);
                    break;
                case 8:
                    Require(_planning.CreateRoadDraft(_roadStart.Row,
                        _roadStart.Column, _roadStart.Row,
                        _roadStart.Column + 2) != null, "road commit");
                    _wallStart = FindLinearPreview(
                        CountyPlanningPrimaryTool.Wall);
                    break;
                case 9:
                    Require(_planning.CreateWallDraft(_wallStart.Row,
                        _wallStart.Column, _wallStart.Row,
                        _wallStart.Column + 2) != null, "wall commit");
                    _canalStart = FindLinearPreview(
                        CountyPlanningPrimaryTool.Canal);
                    break;
                case 10:
                    Require(_planning.CreateCanalDraft(_canalStart.Row,
                        _canalStart.Column, _canalStart.Row,
                        _canalStart.Column + 2) != null, "canal commit");
                    _planning.PreviewDraftTool(
                        CountyPlanningPrimaryTool.Zone,
                        _canalStart.Row + 3, _canalStart.Column,
                        _canalStart.Row + 5, _canalStart.Column + 4,
                        CountyPlanningZoneKind.Residential);
                    break;
                case 11:
                    _planning.CreateZoneDraft(
                        CountyPlanningZoneKind.Residential,
                        _canalStart.Row + 3, _canalStart.Column,
                        _canalStart.Row + 5, _canalStart.Column + 4);
                    Require(FindBuildingTarget(_firstBuilding.DraftId,
                        true) != null, "move building draft");
                    break;
                case 12:
                    _copiedBuilding = FindBuildingTarget(
                        _firstBuilding.DraftId, false);
                    Require(_copiedBuilding != null, "copy building draft");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ExistingFacilityCollision),
                        "eyedropper fixture");
                    var facilityId = _planning
                        .FirstExistingFacilityIdAtSelection();
                    if (!string.IsNullOrWhiteSpace(facilityId))
                        _planning.EyedropperExistingFacility(facilityId);
                    break;
                case 13:
                    Require(_planning.Session.RemoveDraft(
                        _copiedBuilding.DraftId), "delete copied draft");
                    break;
                case 14:
                    Require(_planning.Undo() != null, "undo");
                    Require(_planning.Redo() != null, "redo");
                    break;
                case 15:
                    _planning.SetOverlayVisible("roads", true);
                    _planning.SetOverlayVisible("terrain", false);
                    break;
                case 16:
                    _planning.SetOverlayVisible("terrain", true);
                    break;
                case 17:
                    Require(_game.PanCountyPlanningViewByGuiDelta(
                        new Vector2(-80f, 20f),
                        new Rect(0f, 0f, 900f, 450f)), "camera pan");
                    Require(_game.RotateCountyPlanningViewByGuiDelta(
                        new Vector2(60f, 0f)), "camera rotation");
                    break;
                case 18:
                    SetBaseOverlays();
                    if (Mathf.Abs(_planning.ViewRotationDegrees) > 0.001f)
                        Require(_planning.RotateViewportByGuiDelta(
                                new Vector2(-_planning.ViewRotationDegrees /
                                            0.32f, 0f)),
                            "recommended layout north-up view");
                    Require(_planning.SelectFixture(
                        CountyPlanningFixture.ValidResidence),
                        "recommended layout building ghost");
                    _planning.ActivateBuildingTool(
                        _planning.SelectedProfile.ProfileId);
                    break;
            }
        }

        private static bool ShouldRetryCommandLineEvidence()
        {
            if (SessionState.GetBool(ResolutionRetryKey, false)) return false;
            var arguments = Environment.GetCommandLineArgs();
            var requested = false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], "-executeMethod",
                        StringComparison.OrdinalIgnoreCase) ||
                    index + 1 >= arguments.Length) continue;
                requested = string.Equals(arguments[index + 1],
                    typeof(LuoyangCountyPlanningFinalReviewMenu).FullName +
                    ".CaptureEvidenceAndOpenForReview",
                    StringComparison.Ordinal);
                break;
            }
            if (!requested || EvidenceHasRequiredResolutions()) return false;
            SessionState.SetBool(ResolutionRetryKey, true);
            return true;
        }

        private static bool EvidenceHasRequiredResolutions()
        {
            return PngHasDimensions(Path.Combine(EvidenceRoot(),
                       EvidenceFiles[0]), 1280, 720) &&
                   PngHasDimensions(Path.Combine(EvidenceRoot(),
                       EvidenceFiles[EvidenceFiles.Length - 1]), 1920, 1080);
        }

        private static bool PngHasDimensions(string path, int width, int height)
        {
            if (!File.Exists(path)) return false;
            using (var stream = File.OpenRead(path))
            {
                var header = new byte[24];
                if (stream.Read(header, 0, header.Length) != header.Length)
                    return false;
                var actualWidth = (header[16] << 24) | (header[17] << 16) |
                                  (header[18] << 8) | header[19];
                var actualHeight = (header[20] << 24) | (header[21] << 16) |
                                   (header[22] << 8) | header[23];
                return actualWidth == width && actualHeight == height;
            }
        }

        private static void RequestGameViewResolution(int width, int height)
        {
            _requestedWidth = width;
            _requestedHeight = height;
            _resolutionWaitFrames = 0;
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            var sizeModeType = editorAssembly.GetType(
                "UnityEditor.GameViewSizeType");
            Require(sizesType != null && gameViewType != null &&
                    sizeType != null && sizeModeType != null,
                "Unity Game View reflection types");

            const BindingFlags all = BindingFlags.Public |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Instance |
                                     BindingFlags.Static;
            var instanceProperty = sizesType.GetProperty("instance", all);
            if (instanceProperty == null)
            {
                var singletonType = typeof(ScriptableSingleton<>)
                    .MakeGenericType(sizesType);
                instanceProperty = singletonType.GetProperty("instance", all);
            }
            Require(instanceProperty != null,
                "Unity Game View sizes singleton");
            var sizes = instanceProperty.GetValue(null, null);
            var getGroup = sizesType.GetMethod("GetGroup", all);
            Require(sizes != null && getGroup != null,
                "Unity Game View size group");
            var groupParameter = getGroup.GetParameters()[0].ParameterType;
            var standalone = Enum.Parse(groupParameter, "Standalone");
            var group = getGroup.Invoke(sizes, new[] { standalone });
            Require(group != null, "Unity standalone Game View size group");

            var groupType = group.GetType();
            var getTotalCount = groupType.GetMethod("GetTotalCount", all);
            var getSize = groupType.GetMethod("GetGameViewSize", all);
            var addCustomSize = groupType.GetMethod("AddCustomSize", all);
            Require(getTotalCount != null && getSize != null &&
                    addCustomSize != null, "Unity Game View size methods");
            var total = (int)getTotalCount.Invoke(group, null);
            var selectedIndex = -1;
            for (var index = 0; index < total; index++)
            {
                var candidate = getSize.Invoke(group, new object[] { index });
                if (ReadIntMember(candidate, "width") == width &&
                    ReadIntMember(candidate, "height") == height)
                {
                    selectedIndex = index;
                    break;
                }
            }

            if (selectedIndex < 0)
            {
                var fixedResolution = Enum.Parse(sizeModeType,
                    "FixedResolution");
                var constructor = sizeType.GetConstructor(all, null,
                    new[]
                    {
                        sizeModeType, typeof(int), typeof(int), typeof(string)
                    }, null);
                Require(constructor != null,
                    "Unity fixed Game View size constructor");
                var custom = constructor.Invoke(new[]
                {
                    fixedResolution, (object)width, height,
                    "Mandate " + width + "x" + height
                });
                addCustomSize.Invoke(group, new[] { custom });
                selectedIndex = (int)getTotalCount.Invoke(group, null) - 1;
            }

            var gameView = EditorWindow.GetWindow(gameViewType);
            var selectedSize = gameViewType.GetProperty(
                "selectedSizeIndex", all);
            Require(gameView != null && selectedSize != null,
                "Unity Game View selected size");
            selectedSize.SetValue(gameView, selectedIndex, null);
            gameView.Focus();
            gameView.Repaint();
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        private static int ReadIntMember(object target, string memberName)
        {
            if (target == null) return -1;
            const BindingFlags all = BindingFlags.Public |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Instance;
            var type = target.GetType();
            var property = type.GetProperty(memberName, all);
            if (property != null)
                return Convert.ToInt32(property.GetValue(target, null));
            var field = type.GetField(memberName, all);
            return field == null
                ? -1
                : Convert.ToInt32(field.GetValue(target));
        }

        private static void SetBaseOverlays()
        {
            _planning.SetOverlayVisible("administrative", true);
            _planning.SetOverlayVisible("roads", true);
            _planning.SetOverlayVisible("rivers", true);
            _planning.SetOverlayVisible("grid", true);
            _planning.SetOverlayVisible("terrain", false);
        }

        private static DraftBuildingBlueprint CreateAnotherBuilding()
        {
            var originRow = _planning.SelectedLocalRow;
            var originColumn = _planning.SelectedLocalColumn;
            for (var radius = 2; radius <= 40; radius++)
            for (var side = 0; side < 4; side++)
            {
                var row = originRow + (side == 0 ? -radius :
                    side == 2 ? radius : 0);
                var column = originColumn + (side == 1 ? radius :
                    side == 3 ? -radius : 0);
                if (row < 0 || row >= 320 || column < 0 || column >= 640)
                    continue;
                _planning.SelectCell(row, column);
                for (var rotation = 0; rotation < 4; rotation++)
                {
                    if (_planning.IsCurrentBuildingPlacementValid)
                        return _planning.CreateDraft();
                    _planning.RotateClockwise();
                }
            }
            return null;
        }

        private static (int Row, int Column) FindLinearPreview(
            CountyPlanningPrimaryTool tool)
        {
            for (var row = 2; row < 318; row += 3)
            for (var column = 2; column < 636; column += 3)
            {
                if (_planning.PreviewDraftToolIsValid(tool, row,
                        column, row, column + 2))
                    return (row, column);
            }
            throw new InvalidOperationException(
                "No valid preview path found for " + tool + ".");
        }

        private static DraftBuildingBlueprint FindBuildingTarget(
            string draftId, bool move)
        {
            for (var row = 4; row < 316; row += 2)
            for (var column = 4; column < 636; column += 2)
            for (var rotation = 0; rotation < 4; rotation++)
            {
                var result = move
                    ? _planning.MoveBuildingDraft(draftId, row, column,
                        rotation)
                    : _planning.CopyBuildingDraft(draftId, row, column,
                        rotation);
                if (result != null) return result;
            }
            return null;
        }

        private static string EvidenceRoot() => Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "LuoyangCountyVisualConstructionInteractionReworkV1");

        private static void Require(bool condition, string operation)
        {
            if (!condition) throw new InvalidOperationException(
                "Evidence setup failed: " + operation + ".");
        }

        private static void CaptureFail(string message)
        {
            SessionState.SetBool(CaptureKey, false);
            EditorApplication.update -= DriveEvidenceCapture;
            Debug.LogError("LUOYANG_COUNTY_VISUAL_CONSTRUCTION_EVIDENCE_FAILED " +
                           message);
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(CaptureKey, false);
            EditorApplication.update -= TryEnterPlanning;
            EditorApplication.update -= DriveEvidenceCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError(message);
        }
    }
}
