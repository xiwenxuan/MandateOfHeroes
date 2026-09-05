using Mandate.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mandate.Editor
{
    public static class MapViewRoutingInkWorldFinalReviewMenu
    {
        private const string PendingKey =
            "Mandate.MapViewRoutingInkWorldFinalReview.Pending";
        private const string ScenePath = "Assets/Scenes/PlayableDemo.unity";
        private static int _attempts;

        [InitializeOnLoadMethod]
        private static void RestorePendingReview()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            Arm();
            if (EditorApplication.isPlaying)
                EditorApplication.update += TryEnterInkWorld;
        }

        [MenuItem("Mandate/Validation/Open Map Routing Ink World Review")]
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
                EditorApplication.update -= TryEnterInkWorld;
                EditorApplication.update += TryEnterInkWorld;
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
            EditorApplication.update -= TryEnterInkWorld;
            EditorApplication.update += TryEnterInkWorld;
        }

        private static void TryEnterInkWorld()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= TryEnterInkWorld;
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
            if (!game.ShowWorldView() || !game.SetWorldMapInkStyle(true))
            {
                if (_attempts < 900) return;
                Fail("The formal ink world review route failed.");
                return;
            }
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryEnterInkWorld;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            var gameViewType = typeof(EditorWindow).Assembly.GetType(
                "UnityEditor.GameView");
            if (gameViewType != null)
            {
                var gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameView.Focus();
            }
            Debug.Log("MAP_VIEW_ROUTING_INK_WORLD_FINAL_REVIEW_READY");
        }

        private static void Fail(string message)
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= TryEnterInkWorld;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.LogError(message);
        }
    }
}
