using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class Luoyang50mCountySpatialPrototypeV1UnityTests
    {
        [Test]
        public void FormalAssembliesBuildFullScalePrototypeInsideUnity()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            var prototype = new Luoyang50mCountySpatialPrototypeSource(root)
                .Prototype;

            Assert.That(prototype.Partition.PlanningCellCount,
                Is.EqualTo(Luoyang50mCountySpatialPrototypeIds
                    .PlanningCellCount));
            Assert.That(prototype.Partition.FacilityPlacements.Count,
                Is.EqualTo(Luoyang50mCountySpatialPrototypeIds
                    .FacilityCount));
            Assert.That(prototype.Partition.ChunkCount,
                Is.EqualTo(Luoyang50mCountySpatialPrototypeIds.ChunkCount));
        }

        [Test]
        public void FullScalePrototypeCreatesNoCellGameObjects()
        {
            var before = UnityEngine.Object.FindObjectsOfType<
                UnityEngine.GameObject>().Length;
            var root = Path.Combine(Directory.GetCurrentDirectory(), "Assets",
                "StreamingAssets", "WorldMap");
            _ = new Luoyang50mCountySpatialPrototypeSource(root).Prototype;
            var after = UnityEngine.Object.FindObjectsOfType<
                UnityEngine.GameObject>().Length;

            Assert.That(after, Is.EqualTo(before));
        }
    }
}
