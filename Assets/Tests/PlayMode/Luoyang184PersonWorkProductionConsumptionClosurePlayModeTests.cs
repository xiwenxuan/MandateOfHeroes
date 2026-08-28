using System.Collections;
using Mandate.Domain;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class Luoyang184PersonWorkProductionConsumptionClosurePlayModeTests
    {
        [UnityTest]
        public IEnumerator LivingWorldDashboardUsesCompactRecordsAndExposesDebugSelectors()
        {
            var before = Object.FindObjectsOfType<GameObject>().Length;
            var load = SceneManager.LoadSceneAsync("LuoyangWorldValidation",
                LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True,
                controller.LastError);
            Assert.That(controller.LivingRuntime.Workforce.Count,
                Is.EqualTo(400_000));
            Assert.That(controller.LivingRuntime.Households.Count,
                Is.EqualTo(80_899));
            Assert.That(controller.LivingRuntime.Facilities.Count,
                Is.EqualTo(2_084));
            Assert.That(controller.SelectLivingPerson(399_999), Is.True);
            Assert.That(controller.SelectLivingHousehold(80_898), Is.True);
            Assert.That(controller.SelectLivingFacility(2_083), Is.True);
            Assert.That(controller.LivingDebugText, Is.Not.Empty);
            Assert.That(controller.ExecutePlayerCommand(
                LuoyangPlayerCommandTypeIds.Study), Is.True);
            Assert.That(controller.LivingRuntime.PlayerCommands,
                Has.Some.Matches<LuoyangPlayerCommandRuntimeState>(item =>
                    item.CommandTypeId == LuoyangPlayerCommandTypeIds.Study &&
                    item.StatusId == "completed"));
            Assert.That(Object.FindObjectsOfType<GameObject>().Length - before,
                Is.LessThan(32), "Permanent Persons must not become GameObjects.");
        }
    }
}
