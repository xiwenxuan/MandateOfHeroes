using System.Collections;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangGoldenBlockBuildModeV2PlayModeTests
    {
        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator PlayableDemo_GoldenBlockV2BuildModeIsPresentationOnly()
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

            var game = dashboard.DirectGame;
            var worldBefore = WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld);
            Assert.That(game.ShowCountyView(), Is.True, game.LastMessage);
            var planning = game.CountyPlanning;
            Assert.That(planning, Is.Not.Null);
            Assert.That(planning.FocusGoldenBlockPrototype(), Is.True);
            planning.WorldSpacePresentation.Synchronize();
            Assert.That(planning.PresentationMode,
                Is.EqualTo(CountySubViewMode.UrbanArea));
            Assert.That(planning.WorldSpacePresentation
                .GoldenBlockVisibleModuleCount, Is.GreaterThan(64));
            Assert.That(planning.WorldSpacePresentation
                .GoldenBlockMaterialCount, Is.InRange(8, 12));
            Assert.That(planning.PlayerFacingBuildingProfiles.Count,
                Is.EqualTo(5));

            Assert.That(planning.FocusGoldenBlockBuildMode(), Is.True);
            Assert.That(planning.SetOverlayVisible("grid", true), Is.True);
            planning.WorldSpacePresentation.Show(
                new Rect(0f, 0f, 1280f, 720f));
            Assert.That(planning.PresentationMode,
                Is.EqualTo(CountySubViewMode.Planning));
            Assert.That(planning.PlanningCellGameObjectCount, Is.Zero);
            Assert.That(planning.SelectFixture(
                CountyPlanningFixture.LargeFacility), Is.True);
            planning.ActivateBuildingTool(
                planning.SelectedProfile.ProfileId);
            planning.WorldSpacePresentation.Synchronize();
            Assert.That(planning.Validation.CoveredCells.Count,
                Is.GreaterThan(1));
            Assert.That(planning.WorldSpacePresentation
                .CurrentGhostPresentationProfileId, Is.Not.Empty);
            Assert.That(planning.CreateDraft(), Is.Not.Null);
            Assert.That(planning.Drafts.Count, Is.EqualTo(1));
            Assert.That(planning.Undo(), Is.Not.Null);
            Assert.That(planning.Drafts, Is.Empty);
            Assert.That(planning.Redo(), Is.Not.Null);
            Assert.That(planning.Drafts.Count, Is.EqualTo(1));
            Assert.That(WorldSnapshotSerializer.Serialize(
                dashboard.CurrentWorld), Is.EqualTo(worldBefore));

            planning.CancelPlanningTool();
            planning.SetOverlayVisible("grid", false);
            Assert.That(planning.FocusGoldenBlockPrototype(), Is.True);
            yield return null;
            Assert.That(planning.MapOverlays.GridVisible, Is.False);
            Assert.That(WorldState.CurrentSchemaVersion, Is.EqualTo(79));
        }
    }
}
