using Mandate.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mandate.Editor
{
    public static class DualScaleSpatialArchitectureValidationMenu
    {
        public const string SceneAssetPath =
            "Assets/Scenes/DualScaleSpatialArchitectureValidation.unity";

        [MenuItem("Mandate/Validation/Open Dual-Scale 50m Architecture")]
        public static void OpenForReview()
        {
            BuildSceneAsset();
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorApplication.isPlaying = true;
                };
            }
        }

        public static DualScaleSpatialArchitectureValidationController
            BuildSceneAsset()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "DualScaleSpatialArchitectureValidation";
            var root = new GameObject(
                "Dual-Scale 50m Architecture Validation");
            var controller = root.AddComponent<
                DualScaleSpatialArchitectureValidationController>();
            EditorSceneManager.SaveScene(scene, SceneAssetPath);
            Selection.activeGameObject = root;
            return controller;
        }
    }
}
