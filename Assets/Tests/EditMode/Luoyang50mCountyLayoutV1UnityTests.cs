using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class Luoyang50mCountyLayoutV1UnityTests
    {
        [Test]
        public void VersionedLayoutPackageLoadsInsideUnityWithoutSceneObjects()
        {
            var before = UnityEngine.Object.FindObjectsOfType<
                UnityEngine.GameObject>().Length;
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountyLayoutPackageSource(root);
            var after = UnityEngine.Object.FindObjectsOfType<
                UnityEngine.GameObject>().Length;

            Assert.That(source.Package.PackageId,
                Is.EqualTo(Luoyang50mCountyLayoutIds.PackageId));
            Assert.That(source.Package.Facilities.Count, Is.EqualTo(2084));
            Assert.That(source.Package.RoadEdges.Count, Is.EqualTo(334));
            Assert.That(source.Package.CanalEdges.Count, Is.EqualTo(17));
            Assert.That(after, Is.EqualTo(before));
        }
    }
}
