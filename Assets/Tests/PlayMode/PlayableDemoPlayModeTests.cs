using System.Collections;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class PlayableDemoPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayableDemo_StartsWithCameraAndDashboard()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;

            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(
                Object.FindObjectOfType<SimulationDashboard>(),
                Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo("PlayableDemo"));
        }
    }
}
