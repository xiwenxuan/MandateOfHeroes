using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class McfViewScaleCountySandboxPresentationLodV1Tests
    {
        [Test]
        public void MainViewContract_ContainsOnlyWorldCountyAndPerson()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                LuoyangPlayableViewMode.World,
                LuoyangPlayableViewMode.County,
                LuoyangPlayableViewMode.Person
            }, (LuoyangPlayableViewMode[])System.Enum.GetValues(
                typeof(LuoyangPlayableViewMode)));
            CollectionAssert.AreEquivalent(new[]
            {
                CountySubViewMode.Overview,
                CountySubViewMode.UrbanArea,
                CountySubViewMode.Planning
            }, (CountySubViewMode[])System.Enum.GetValues(
                typeof(CountySubViewMode)));
        }

        [Test]
        public void LodController_UsesFarMidNearHysteresis()
        {
            var lod = new CountyMapPresentationLodController();
            lod.Reset(320f);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Far));
            Assert.That(lod.Update(220f), Is.False,
                "Far must not chatter at the Mid exit threshold.");
            Assert.That(lod.Update(190f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Mid));
            Assert.That(lod.Update(64f), Is.False,
                "Mid must not chatter at the Near exit threshold.");
            Assert.That(lod.Update(48f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Near));
            Assert.That(lod.Update(64f), Is.False);
            Assert.That(lod.Update(80f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Mid));
            Assert.That(lod.Update(250f), Is.True);
            Assert.That(lod.Current, Is.EqualTo(
                CountyMapPresentationLod.Far));
        }

        [Test]
        public void StrategicRoadPolicy_DefaultIsSparseAndTransportIsComplete()
        {
            Assert.That(StrategicRoadPresentationPolicy.Includes(
                "geo.route.luoyang_changan", 60,
                StrategicRoadPresentationMode.DefaultBackbone), Is.True);
            Assert.That(StrategicRoadPresentationPolicy.Includes(
                "geo.route.hanzhong_jiange_chengdu", 120,
                StrategicRoadPresentationMode.DefaultBackbone), Is.True);
            Assert.That(StrategicRoadPresentationPolicy.Includes("R001", 30,
                StrategicRoadPresentationMode.DefaultBackbone), Is.False);
            Assert.That(StrategicRoadPresentationPolicy.Includes("R001", 30,
                StrategicRoadPresentationMode.TransportOverlay), Is.True);
        }

        [Test]
        public void CountySandbox_ChangesPresentationOnlyAndSuppressesPointCloud()
        {
            var root = new GameObject("MCF County LOD Test");
            try
            {
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    CountySubViewMode.Overview), Is.True,
                    planning.LastError);
                var schema = WorldState.CurrentSchemaVersion;
                var fingerprint = planning.LayoutFingerprint;

                Assert.That(planning.PresentationLod, Is.EqualTo(
                    CountyMapPresentationLod.Far));
                Assert.That(planning.PresentationSnapshot.VisibleFacilities,
                    Is.GreaterThan(0).And.LessThan(planning.FacilityCount));
                Assert.That(planning.PresentationSnapshot.VisibleGridRows,
                    Is.Zero);
                Assert.That(planning.PlanningCellGameObjectCount, Is.Zero);

                Assert.That(planning.SetPresentationMode(
                    CountySubViewMode.UrbanArea), Is.True);
                Assert.That(planning.PresentationLod, Is.EqualTo(
                    CountyMapPresentationLod.Mid));
                Assert.That(planning.PresentationSnapshot.VisibleGridRows,
                    Is.Zero);

                Assert.That(planning.SetPresentationMode(
                    CountySubViewMode.Planning), Is.True);
                Assert.That(planning.PresentationLod, Is.EqualTo(
                    CountyMapPresentationLod.Near));
                Assert.That(planning.ShouldShowPlanningGrid, Is.True);
                Assert.That(planning.PresentationSnapshot.VisibleGridRows,
                    Is.GreaterThan(0));

                Assert.That(planning.SetOverlayVisible("fortifications",
                    false), Is.True);
                Assert.That(planning.PresentationSnapshot
                    .VisibleFortificationSegments, Is.Zero);
                Assert.That(planning.SetOverlayVisible("planning", false),
                    Is.True);
                Assert.That(planning.MapOverlays.PlanningVisible, Is.False);
                Assert.That(planning.LayoutFingerprint,
                    Is.EqualTo(fingerprint));
                Assert.That(WorldState.CurrentSchemaVersion,
                    Is.EqualTo(schema).And.EqualTo(79));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
