using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCitywideBuildingLanguageV1Tests
    {
        [Test]
        [Timeout(300_000)]
        public void CountyMid_BuildsBatchedFiveFamilyCityWithoutWorldMutation()
        {
            var root = new GameObject("Citywide Building Language Test");
            var cameraObject = new GameObject("Citywide Building Camera");
            try
            {
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    CountySubViewMode.Overview), Is.True,
                    planning.LastError);
                Assert.That(planning.EnsureWorldSpacePresentation(
                    cameraObject.AddComponent<Camera>()), Is.True,
                    planning.LastError);
                var world = planning.WorldSpacePresentation;
                var fingerprint = planning.LayoutFingerprint;
                var formalFacilityCount = planning.FacilityCount;

                Assert.That(world.CitywideBuildingLanguagePlan, Is.Not.Null);
                Assert.That(world.CitywideStyledFacilityCount,
                    Is.EqualTo(1056));
                Assert.That(world.CitywideContextFacilityCount,
                    Is.GreaterThan(850).And.LessThan(1056));
                Assert.That(world.CitywideBuildingLanguageRendererCount,
                    Is.InRange(8,
                        CountyCitywideBuildingLanguagePlan
                            .MaximumSharedRendererCount));
                Assert.That(world.CitywideBuildingLanguageModuleCount,
                    Is.GreaterThan(world.CitywideContextFacilityCount * 3));
                Assert.That(world.CitywideBuildingLanguageTriangleCount,
                    Is.GreaterThan(0).And.LessThan(600_000));
                Assert.That(world.CitywideBuildingLanguageMaterialCount,
                    Is.InRange(8, 12));

                var citywide = world.WorldRoot.Find(
                    "Urban Fabric/Citywide Five-Family Building Language V1");
                Assert.That(citywide, Is.Not.Null);
                Assert.That(citywide.GetComponentsInChildren<Collider>(true),
                    Is.Empty);
                Assert.That(citywide.GetComponentsInChildren<
                    HanBuildableFacilityModelInstance>(true), Is.Empty);

                world.Show(new Rect(0f, 0f, 1280f, 720f));
                Assert.That(planning.FocusGoldenBlockPrototype(), Is.True);
                world.Synchronize();
                Assert.That(planning.PresentationLod,
                    Is.EqualTo(CountyMapPresentationLod.Mid));
                Assert.That(citywide.gameObject.activeSelf, Is.True);
                Assert.That(world.WorldRoot.Find("Urban Fabric/Far Aggregates")
                    .gameObject.activeSelf, Is.False);

                Assert.That(planning.FacilityCount,
                    Is.EqualTo(formalFacilityCount));
                Assert.That(planning.LayoutFingerprint,
                    Is.EqualTo(fingerprint));
                Assert.That(world.CitywideBuildingLanguagePlan
                    .CreatesWorldFacts, Is.False);
                Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
