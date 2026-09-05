using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCountyPlanningToolsV1UnityTests
    {
        [Test]
        public void Presentation_UsesOneTextureAndNoPerCellGameObjects()
        {
            var root = new GameObject("Luoyang County Planning Test");
            try
            {
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();

                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId), Is.True,
                    planning.LastError);
                Assert.That(planning.IsActive, Is.True);
                Assert.That(planning.Profiles.Profiles.Count, Is.EqualTo(6));
                Assert.That(planning.PlayerFacingBuildingProfiles.Count,
                    Is.EqualTo(5));
                Assert.That(planning.MapTexture.width, Is.EqualTo(640));
                Assert.That(planning.MapTexture.height, Is.EqualTo(320));
                Assert.That(planning.PlanningCellGameObjectCount,
                    Is.EqualTo(0));
                Assert.That(planning.CountyMapRenderObjectCount,
                    Is.EqualTo(1));
                Assert.That(planning.Validation.State,
                    Is.EqualTo(PlacementValidationState.Valid));
                Assert.That(planning.CellInspection.CountyId, Is.EqualTo(
                    Luoyang50mCountySpatialPrototypeIds.CountyId));

                Assert.That(planning.SelectFixture(
                    CountyPlanningFixture.LargeFacility), Is.True);
                Assert.That(planning.CurrentFootprint.WidthMetres,
                    Is.GreaterThan(50d));
                Assert.That(planning.Validation.CoveredCells.Count,
                    Is.GreaterThan(1));
                Assert.That(planning.SelectFixture(
                    CountyPlanningFixture.ExistingFacilityCollision),
                    Is.True);
                Assert.That(planning.Validation.BlockingReasons.Any(value =>
                    value.Code == PlacementReasonIds
                        .ExistingFacilityCollision), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
