using Mandate.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
    }
}
