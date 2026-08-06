using Mandate.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mandate.Tests
{
    public sealed class DashboardSceneTests
    {
        [Test]
        public void SimulationDashboardScene_LoadsWithDashboardComponent()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/SimulationDashboard.unity",
                OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);

            SimulationDashboard dashboard = null;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                dashboard = roots[i].GetComponentInChildren<SimulationDashboard>(true);
                if (dashboard != null)
                {
                    break;
                }
            }

            Assert.That(dashboard, Is.Not.Null);
        }

        [Test]
        public void PlayableDemoScene_HasCameraAndPlayerModeDashboard()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/PlayableDemo.unity",
                OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            SimulationDashboard dashboard = null;
            Camera camera = null;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                dashboard ??= roots[i]
                    .GetComponentInChildren<SimulationDashboard>(true);
                camera ??= roots[i].GetComponentInChildren<Camera>(true);
            }

            Assert.That(dashboard, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.CompareTag("MainCamera"), Is.True);
            var serialized = new SerializedObject(dashboard);
            Assert.That(serialized.FindProperty("_playerDemoMode").boolValue,
                Is.True);
            Assert.That(serialized.FindProperty("_showDeveloperTools").boolValue,
                Is.False);
            Assert.That(EditorBuildSettings.scenes[0].path,
                Is.EqualTo("Assets/Scenes/PlayableDemo.unity"));
        }
    }
}
