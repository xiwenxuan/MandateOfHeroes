using System.Collections;
using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class Luoyang184HistoricalPersonFamilyIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator IntegrationCreatesNoPersonOrFacilityGameObjects()
        {
            var before = Object.FindObjectsOfType<GameObject>().Length;
            var metro = Path.Combine(Application.dataPath, "StreamingAssets",
                "WorldMap", "Luoyang184MetropolitanInitializationV1");
            var historical = Path.Combine(Application.dataPath, "StreamingAssets",
                "HistoricalPersons", "Han135260V1");
            var world = WorldState.Create(184);
            var result = new Luoyang184HistoricalPersonFamilyIntegrationBootstrap(
                metro, historical).Integrate(world);
            yield return null;
            Assert.That(result.HistoricalPersonCount, Is.EqualTo(25));
            Assert.That(result.FacilityCount, Is.EqualTo(2084));
            Assert.That(Object.FindObjectsOfType<GameObject>().Length, Is.EqualTo(before));
        }
    }
}
