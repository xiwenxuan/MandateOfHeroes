using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class Luoyang184UrbanInitializationPlayModeTests
    {
        [UnityTest]
        public IEnumerator FormalPackageLoadsAChunkWithoutCreatingOneGameObjectPerPerson()
        {
            var root = Path.Combine(
                Application.dataPath, "StreamingAssets", "WorldMap", "Luoyang184UrbanInitializationV1");
            var objectsBefore = Object.FindObjectsOfType<GameObject>().Length;
            var reader = new Luoyang184UrbanInitializationReader(root);
            var chunk = reader.ReadPersons(0, 4096).ToArray();
            yield return null;
            var objectsAfter = Object.FindObjectsOfType<GameObject>().Length;

            Assert.That(reader.Manifest.PersonCount, Is.EqualTo(270000));
            Assert.That(chunk.Length, Is.EqualTo(4096));
            Assert.That(chunk[0].Ordinal, Is.Zero);
            Assert.That(chunk[4095].Ordinal, Is.EqualTo(4095));
            Assert.That(objectsAfter, Is.EqualTo(objectsBefore),
                "Loading permanent population data must not create one GameObject per Person.");
        }
    }
}
