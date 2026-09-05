using Mandate.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mandate.Editor
{
    public static class Luoyang50mCountySpatialPrototypeMenu
    {
        public const string SceneAssetPath =
            "Assets/Scenes/Luoyang50mCountySpatialPrototype.unity";

        [MenuItem("Mandate/Validation/Open Luoyang 50m County Prototype")]
        public static void OpenForReview()
        {
            BuildSceneAsset();
            if (!Application.isBatchMode)
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorApplication.isPlaying = true;
                };
        }

        public static Luoyang50mCountySpatialPrototypeController
            BuildSceneAsset()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "Luoyang50mCountySpatialPrototype";
            var root = new GameObject("Luoyang 50m County Spatial Prototype");
            var controller = root.AddComponent<
                Luoyang50mCountySpatialPrototypeController>();
            EditorSceneManager.SaveScene(scene, SceneAssetPath);
            Selection.activeGameObject = root;
            return controller;
        }
    }
}
