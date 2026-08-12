using System.Collections;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests
{
    public sealed class LuoyangWorldValidationPlayModeTests
    {
        [UnityTest]
        public IEnumerator LuoyangWorldValidationSceneInitializesSelectsHistoricalFacilityAndDemonstratesFortification()
        {
            var load = SceneManager.LoadSceneAsync("LuoyangWorldValidation", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
            var controller = Object.FindObjectOfType<LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null, "Validation scene must contain its controller.");
            Assert.That(controller.IsReady || controller.TryInitialize(), Is.True, controller.LastError);
            Assert.That(controller.Prototype.PopulationProfile.TotalPersons, Is.EqualTo(20_542));
            Assert.That(controller.Prototype.ScenarioYear, Is.EqualTo(184));
            Assert.That(controller.SelectFacility("facility.instance.luoyang.184.taixue"), Is.True);
            Assert.That(controller.SelectedCell.FacilityName, Is.EqualTo("太学"));
            controller.SetOverlay(LuoyangMapOverlay.Fortifications);
            Assert.That(controller.Overlay, Is.EqualTo(LuoyangMapOverlay.Fortifications));
            Assert.That(controller.DemonstrateGateState("facility.instance.luoyang.184.gate.pingchengmen", "Open"), Is.True);
            Assert.That(controller.SelectFacility("facility.instance.luoyang.184.gate.pingchengmen"), Is.True);
            Assert.That(controller.SelectedCell.GateState, Is.EqualTo("Open"));
            controller.LocateLuoyangHulaoCorridor();
            controller.Zoom(0.75f);
            Assert.That(controller.CellsPerPixel, Is.GreaterThan(0f));
            Assert.That(controller.SelectStressProfile("Profile_250000_Stress"), Is.True);
            Assert.That(controller.StressProfile.PersonCount, Is.EqualTo(250_000));
            Assert.That(controller.StressMode.SimulationDays, Is.EqualTo(365));
            Assert.That(controller.MaximumVisualActorCount, Is.LessThanOrEqualTo(256));
            controller.SetConstructionMode(false);
            Assert.That(controller.StressMode.FacilitiesAdded, Is.Zero);
            controller.SetConstructionMode(true);
            Assert.That(controller.StressMode.FacilitiesAdded, Is.GreaterThan(0));
            Assert.That(controller.SelectStressProfile("Profile_500000_Stress"), Is.True);
            Assert.That(controller.StressProfile.Lod.PermanentPersonCount, Is.EqualTo(500_000));
            Assert.That(controller.StressProfile.Lod.HighFrequencyActorCount, Is.LessThan(500_000));
            yield return null;
        }
    }
}
