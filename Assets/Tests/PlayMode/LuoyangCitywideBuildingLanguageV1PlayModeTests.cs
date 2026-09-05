using System.Collections;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangCitywideBuildingLanguageV1PlayModeTests
    {
        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PlayableDemo_CountyMidUsesCitywideBuildingLanguage()
        {
            yield return SceneManager.LoadSceneAsync("PlayableDemo",
                LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var controller = dashboard.DirectGame;
            Assert.That(controller.ShowCountyView(), Is.True,
                controller.LastMessage);
            yield return null;
            var planning = controller.CountyPlanning;
            Assert.That(planning, Is.Not.Null);
            Assert.That(planning.FocusGoldenBlockPrototype(), Is.True);
            planning.WorldSpacePresentation.Synchronize();
            yield return null;

            var world = planning.WorldSpacePresentation;
            Assert.That(world.CitywideStyledFacilityCount, Is.EqualTo(1056));
            Assert.That(world.CitywideContextFacilityCount,
                Is.GreaterThan(850));
            Assert.That(world.CitywideBuildingLanguageRendererCount,
                Is.LessThanOrEqualTo(
                    CountyCitywideBuildingLanguagePlan
                        .MaximumSharedRendererCount));
            Assert.That(planning.PresentationLod,
                Is.EqualTo(CountyMapPresentationLod.Mid));
            Assert.That(planning.FacilityCount, Is.EqualTo(2084));
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
        }
    }
}
