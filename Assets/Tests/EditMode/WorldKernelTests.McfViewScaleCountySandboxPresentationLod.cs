using System.IO;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void McfPresentationLod_UsesStableHysteresis()
        {
            var lod = new CountyMapPresentationLodController();
            lod.Reset(320f);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Far));
            Assert.That(lod.Update(220f), Is.False);
            Assert.That(lod.Update(190f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Mid));
            Assert.That(lod.Update(64f), Is.False);
            Assert.That(lod.Update(48f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Near));
            Assert.That(lod.Update(80f), Is.True);
            Assert.That(lod.Update(250f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Far));
        }

        [Test]
        public void McfWorldRoadPolicy_IsSparseByDefaultAndCompleteOnDemand()
        {
            Assert.That(StrategicRoadPresentationPolicy.Includes(
                "geo.route.luoyang_changan", 60,
                StrategicRoadPresentationMode.DefaultBackbone), Is.True);
            Assert.That(StrategicRoadPresentationPolicy.Includes(
                "geo.route.hebei_zhongyuan", 120,
                StrategicRoadPresentationMode.DefaultBackbone), Is.True);
            Assert.That(StrategicRoadPresentationPolicy.Includes("R001", 30,
                StrategicRoadPresentationMode.DefaultBackbone), Is.False);
            Assert.That(StrategicRoadPresentationPolicy.Includes("R001", 30,
                StrategicRoadPresentationMode.TransportOverlay), Is.True);
        }

        [Test]
        public void McfCountyPresentationStack_IndexesAuthoritativeLayout()
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var source = new Luoyang50mCountySpatialPrototypeSource(root);
            var stack = new CountyMapPresentationStack(source.LayoutPackage,
                source.Prototype.Partition);
            Assert.That(stack.Roads.Count, Is.EqualTo(
                Luoyang50mCountyLayoutIds.RoadEdgeCount));
            Assert.That(stack.FarFacilities.Count,
                Is.GreaterThan(0).And.LessThan(stack.MidFacilities.Count));
            Assert.That(stack.MidFacilities.Count,
                Is.LessThan(stack.NearFacilities.Count));
            Assert.That(stack.NearFacilities.Count,
                Is.LessThan(source.LayoutPackage.Facilities.Count));
            Assert.That(stack.FarFortificationOutlines.Count,
                Is.GreaterThan(0));
        }
    }
}
