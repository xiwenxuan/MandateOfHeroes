using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangGoldenBlockPresentationV1Tests
    {
        [Test]
        [Timeout(300_000)]
        public void GoldenBlock_BuildsBatchedFiveFamilyStreetSceneAndFocuses()
        {
            var root = new GameObject("Golden Block EditMode Test");
            var cameraObject = new GameObject("Golden Block Camera");
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
                var golden = world.GoldenBlockPlan;

                Assert.That(golden, Is.Not.Null);
                Assert.That(golden.Lots.Count, Is.EqualTo(16));
                Assert.That(world.GoldenBlockRendererCount,
                    Is.GreaterThanOrEqualTo(8).And.LessThanOrEqualTo(12));
                Assert.That(world.WorldRoot.Find(
                    "Urban Fabric/Luoyang Golden Block V2"), Is.Not.Null);
                Assert.That(planning.FocusGoldenBlockPrototype(), Is.True);
                Assert.That(planning.PresentationMode,
                    Is.EqualTo(CountySubViewMode.UrbanArea));
                Assert.That(planning.PresentationLod,
                    Is.EqualTo(CountyMapPresentationLod.Mid));
                Assert.That(planning.SelectedLocalRow,
                    Is.InRange(golden.MinimumRow, golden.MaximumRow));
                Assert.That(planning.SelectedLocalColumn,
                    Is.InRange(golden.MinimumColumn, golden.MaximumColumn));
                Assert.That(planning.FacilityCount, Is.EqualTo(2084));
                Assert.That(world.GoldenBlockVisibleModuleCount,
                    Is.GreaterThan(64));
                Assert.That(world.GoldenBlockPropCount,
                    Is.GreaterThan(0));
                Assert.That(world.GoldenBlockVegetationInstanceCount,
                    Is.GreaterThan(0));
                Assert.That(world.GoldenBlockTriangleCount,
                    Is.GreaterThan(0));
                Assert.That(world.GoldenBlockMaterialCount,
                    Is.InRange(8, 12));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        [Timeout(300_000)]
        public void GoldenBlockV2_BuildModeUsesFormalGridProfileGhostAndDraft()
        {
            var root = new GameObject("Golden Block V2 Build Test");
            var cameraObject = new GameObject("Golden Block V2 Camera");
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
                var formalFacilityCount = planning.FacilityCount;
                var fingerprint = planning.LayoutFingerprint;

                Assert.That(planning.FocusGoldenBlockBuildMode(), Is.True);
                Assert.That(planning.PresentationMode,
                    Is.EqualTo(CountySubViewMode.Planning));
                Assert.That(planning.PresentationLod,
                    Is.EqualTo(CountyMapPresentationLod.Near));
                Assert.That(planning.SetOverlayVisible("grid", true),
                    Is.True);
                world.Show(new Rect(0f, 0f, 1280f, 720f));
                world.Synchronize();

                Assert.That(planning.ShouldShowPlanningGrid, Is.True);
                Assert.That(world.PlanningGridGameObjectCount,
                    Is.InRange(4, 5));
                Assert.That(world.CurrentGhostPresentationProfileId,
                    Is.EqualTo("presentation.building.han.residence.v2"));
                Assert.That(planning.Validation.CoveredCells, Is.Not.Empty);
                Assert.That(planning.CurrentFootprint.WidthMetres,
                    Is.LessThan(50d));

                var market = planning.PlayerFacingBuildingProfiles.Single(
                    item => item.FacilityDefinitionId ==
                            "facility.commercial.market");
                planning.SelectProfile(market.ProfileId);
                world.Synchronize();
                Assert.That(planning.CurrentFootprint.WidthMetres,
                    Is.GreaterThan(50d));
                Assert.That(planning.Validation.CoveredCells.Count,
                    Is.GreaterThan(1));
                Assert.That(world.CurrentGhostPresentationProfileId,
                    Is.EqualTo("presentation.building.han.market.v2"));

                var beforeDirection = planning.CurrentFootprint.Entrances
                    .Single(item => item.Primary).OutwardDirection;
                planning.RotateClockwise();
                world.Synchronize();
                Assert.That(planning.CurrentFootprint.Entrances.Single(
                        item => item.Primary).OutwardDirection,
                    Is.Not.EqualTo(beforeDirection));

                Assert.That(planning.FacilityCount,
                    Is.EqualTo(formalFacilityCount));
                Assert.That(planning.LayoutFingerprint,
                    Is.EqualTo(fingerprint));
                Assert.That(planning.Session.AllDrafts, Is.Empty);

                planning.SetPresentationMode(CountySubViewMode.UrbanArea);
                world.Synchronize();
                Assert.That(planning.ShouldShowPlanningGrid, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
