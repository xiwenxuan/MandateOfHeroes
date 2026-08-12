using System.Collections;
using System.IO;
using System.Linq;
using Mandate.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class Luoyang184MetropolitanInitializationPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompositePackageCrossesUrbanBoundaryWithoutCreatingPopulationGameObjects()
        {
            var root = Path.Combine(Application.dataPath, "StreamingAssets", "WorldMap",
                "Luoyang184MetropolitanInitializationV1");
            var before = Object.FindObjectsOfType<GameObject>().Length;
            var reader = new Luoyang184MetropolitanInitializationReader(root);
            var chunk = reader.ReadPersons(269500, 4096).ToArray();
            yield return null;
            var after = Object.FindObjectsOfType<GameObject>().Length;
            Assert.That(reader.Manifest.PersonCount, Is.EqualTo(400000));
            Assert.That(chunk.Length, Is.EqualTo(4096));
            Assert.That(chunk.First().Ordinal, Is.EqualTo(269500));
            Assert.That(chunk.Last().Ordinal, Is.EqualTo(273595));
            Assert.That(after, Is.EqualTo(before));
        }
    }
}
