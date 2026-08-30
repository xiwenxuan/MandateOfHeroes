using System.Collections;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mandate.Tests.PlayMode
{
    public sealed class LuoyangGoldenSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenSlice_DefaultsToPlayableViewAndHidesGrid()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            Assert.That(controller.UsesPlayablePresentation, Is.True);
            Assert.That(controller.NormalModeHidesCellGrid, Is.True);
            Assert.That(controller.GoldenSlice.FacilityAnchors, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator GoldenSlice_BuildModeUsesRealBlueprintAndRuntimeProject()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            controller.SetBuildMode(true);
            Assert.That(controller.BuildModeEnabled, Is.True);
            Assert.That(controller.NormalModeHidesCellGrid, Is.False);
            Assert.That(controller.SelectBuildBlueprint(
                "blueprint.han.residence.general.v1"), Is.True);
            Assert.That(controller.PrepareOwnedBuildCell(), Is.True);
            var before = controller.LivingRuntime.ConstructionProjects.Count;
            Assert.That(controller.ConfirmBlueprintConstruction(), Is.True,
                controller.PlayableMessage);
            Assert.That(controller.LivingRuntime.ConstructionProjects.Count,
                Is.EqualTo(before + 1));
        }

        [UnityTest]
        public IEnumerator PlayerSupplyCardTests_OrdinaryViewReadsFormalAuthorityWithoutMutation()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            var before = Luoyang184LivingWorldCheckpointStore
                .ComputeDeterministicStateSha256(controller.LivingRuntime);
            controller.RefreshSupplyCard();
            Assert.That(controller.SupplyCard, Is.Not.Null);
            Assert.That(controller.SupplyCard.IsLimitedKnowledge, Is.True);
            Assert.That(controller.SupplyCard.CityFoodStockMilliunits,
                Is.GreaterThan(0));
            Assert.That(controller.SupplyCard.DailyDemandMilliunits,
                Is.GreaterThan(0));
            Assert.That(Luoyang184LivingWorldCheckpointStore
                    .ComputeDeterministicStateSha256(
                        controller.LivingRuntime), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator PlayerMerchantFormalInterventionTests_OrdinaryActionCreatesFormalMobileCargo()
        {
            yield return SceneManager.LoadSceneAsync("LuoyangWorldValidation");
            yield return null;
            var controller = Object.FindObjectOfType<
                LuoyangWorldValidationController>();
            Assert.That(controller.IsReady, Is.True, controller.LastError);
            Assert.That(controller.RegisterSelectedPlayerMerchantCarrier(),
                Is.True, controller.PlayableMessage);
            var before = controller.LivingRuntime.Shipments.Count;
            Assert.That(controller.DispatchSelectedPlayerMerchantSupply(),
                Is.True, controller.PlayableMessage);
            Assert.That(controller.LivingRuntime.Shipments.Count,
                Is.EqualTo(before + 1));
            var shipment = controller.LivingRuntime.Shipments[
                controller.LivingRuntime.Shipments.Count - 1];
            Assert.That(shipment.PlayerDirected, Is.True);
            Assert.That(shipment.RemainingCargoQuantityMilliunits,
                Is.GreaterThan(0));
            Assert.That(controller.LivingRuntime.FormalEconomy
                .InventoryContainers, Has.Some.Matches<Mandate.Domain
                .InventoryContainerState>(item => item.Id ==
                    Mandate.Simulation.LuoyangFormalEconomySystem
                        .FreightContainerId(shipment.Id)));
        }
    }
}
