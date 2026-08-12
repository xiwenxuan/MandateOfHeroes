using System.Collections;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class MapValidationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MapValidation_LoadsChunkMapAndExposesRealSelectedCell()
        {
            yield return SceneManager.LoadSceneAsync("MapValidation", LoadSceneMode.Single);
            yield return null;

            var controller = Object.FindObjectOfType<MapValidationController>();
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            Assert.That(controller.SelectedCell.Id.Value, Is.GreaterThan(0));
            Assert.That(controller.PositionedCityCount, Is.EqualTo(72));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MapValidation"));
        }
    }
}
