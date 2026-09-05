using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class PlayableDirectLuoyangGameV1PlayModeTests
    {
        [Serializable]
        private sealed class ThreeLevelPerformanceEvidence
        {
            public string UnityVersion;
            public string OperatingSystem;
            public string Processor;
            public int ProcessorCount;
            public int ScreenWidth;
            public int ScreenHeight;
            public List<PlayableLuoyangViewPerformanceSnapshot> Views =
                new List<PlayableLuoyangViewPerformanceSnapshot>();
        }

        private static readonly string ThreeLevelEvidenceRoot = Path.Combine(
            Directory.GetCurrentDirectory(), "Docs", "Evidence",
            "LuoyangP01ThreeLevelViewV1");

        [UnityTest]
        public IEnumerator PlayableDemo_EntersDirectLuoyangAndSwitchesMap()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;

            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null ||
                !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;

            var game = dashboard.DirectGame;
            Assert.That(game, Is.Not.Null);
            Assert.That(game.IsActive, Is.True);
            Assert.That(game.BoundWorld, Is.SameAs(dashboard.CurrentWorld));
            Assert.That(game.BoundWorld.PlayerPersonId,
                Is.EqualTo(PlayableLuoyangWorldContractIds.PlayerPersonId));
            Assert.That(game.BoundWorld.Facilities.Count, Is.EqualTo(2_084));
            Assert.That(game.NaturalMap.IsReady, Is.True,
                game.NaturalMap.LastError);
            Assert.That(game.NaturalMap.LuoyangBuildingPerformancePreviewVisible,
                Is.True);
            Assert.That(game.NaturalMap.LuoyangPassageWorldBound, Is.True);
            Assert.That(game.NaturalMap.HumanScaleLocalPresentationVisible,
                Is.True);
            Assert.That(game.NaturalMap
                .RuntimeLuoyangHumanScaleResidentCellCount, Is.EqualTo(9));
            Assert.That(game.NaturalMap.GetLuoyangClickWalkPedestrian(),
                Is.Not.Null);
            Assert.That(game.NaturalMap.GetLuoyangClickWalkPedestrian()
                .transform.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(game.NaturalMap
                .LuoyangPedestrianPlaybackSpeedUnitsPerSecond,
                Is.EqualTo(24f).Within(0.001f));
            Assert.That(game.NaturalMap.PresentationCamera.orthographicSize,
                Is.LessThanOrEqualTo(8f));
            Assert.That(new PlayerSession(game.BoundWorld)
                .ControlledPerson.CurrentFacilityId, Is.Not.Empty);
            Assert.That(game.ViewMode,
                Is.EqualTo(LuoyangPlayableViewMode.Person));
            Assert.That(game.NaturalMap.LuoyangDebugCellPresentationVisible,
                Is.False);
            Assert.That(game.NaturalMap
                .RuntimeLuoyangNearfieldContextFacilityCount, Is.EqualTo(9));
            Assert.That(game.NaturalMap.LuoyangCityViewProjection.FacilityCount,
                Is.EqualTo(2_084));

            var camera = game.NaturalMap.PresentationCamera;
            var openingCameraPosition = camera.transform.position;
            Assert.That(game.PanCameraByScreenDelta(
                new Vector2(40f, 20f), 720f), Is.True);
            Assert.That(camera.transform.position,
                Is.Not.EqualTo(openingCameraPosition));
            var openingYaw = game.CameraYawDegrees;
            Assert.That(game.RotateCameraByScreenDelta(
                new Vector2(20f, -10f)), Is.True);
            Assert.That(game.CameraYawDegrees, Is.GreaterThan(openingYaw));

            Assert.That(game.NaturalMap.SelectLuoyangFacility(
                PlayableLuoyangWorldContractIds.MarketFacilityId), Is.True);
            Assert.That(game.SelectedFacility, Is.Not.Null);
            Assert.That(game.SelectedFacility.DisplayName,
                Is.EqualTo("市场"));
            if (!string.Equals(game.NaturalMap
                    .LuoyangPedestrianCurrentFacilityId,
                    PlayableLuoyangWorldContractIds.MarketFacilityId,
                    System.StringComparison.Ordinal))
            {
                Assert.That(game.TryMoveToSelectedFacility(), Is.True,
                    game.LastMessage);
                for (var step = 0; step < 1000 && game.NaturalMap
                         .LuoyangPedestrianIsWalking; step++)
                    game.NaturalMap.StepLuoyangPedestrian(1f);
            }
            Assert.That(game.NaturalMap.LuoyangPedestrianCurrentFacilityId,
                Is.EqualTo(PlayableLuoyangWorldContractIds.MarketFacilityId));
            Assert.That(game.GetSelectedBuildingActions().Any(item =>
                    item.Id == PlayerActionIds.TradeBuy && item.IsAvailable),
                Is.True);
            Assert.That(game.ExecuteSelectedBuildingAction(
                PlayerActionIds.TradeBuy), Is.True, game.LastMessage);
            Assert.That(game.BoundWorld.TradeRecords.Count,
                Is.EqualTo(1));
            Assert.That(game.GetSelectedBuildingActions().Any(item =>
                    item.Id == PlayerActionIds.AcceptTask && item.IsAvailable),
                Is.True);
            Assert.That(game.ExecuteSelectedBuildingAction(
                PlayerActionIds.AcceptTask), Is.True, game.LastMessage);
            Assert.That(game.BoundWorld.Tasks.Single().DefinitionId,
                Is.EqualTo(PlayableLuoyangWorldContractIds
                    .LocalTaskDefinitionId));

            var worldBeforeViews = WorldSnapshotSerializer.Serialize(
                game.BoundWorld);
            var playerBeforeViews = new PlayerSession(game.BoundWorld)
                .ControlledPerson;
            var playerFacilityBeforeViews = playerBeforeViews.CurrentFacilityId;
            var playerCellBeforeViews = playerBeforeViews.CurrentCellId64;
            Assert.That(game.ShowCountyView(), Is.True);
            Assert.That(game.ViewMode,
                Is.EqualTo(LuoyangPlayableViewMode.County));
            Assert.That(game.CountySubView,
                Is.EqualTo(CountySubViewMode.Overview));
            Assert.That(game.CountyPlanning.FacilityCount,
                Is.EqualTo(2_084));
            Assert.That(game.ShowCountySubView(
                CountySubViewMode.UrbanArea), Is.True);
            Assert.That(game.ShowCountySubView(
                CountySubViewMode.Planning), Is.True);
            Assert.That(game.ShowCountySubView(
                CountySubViewMode.Overview), Is.True);
            Assert.That(game.ShowPersonView(), Is.True);
            const string observedGate =
                "facility.instance.luoyang.184.gate.guangyangmen";
            Assert.That(game.NaturalMap.SelectLuoyangFacility(observedGate),
                Is.True);
            Assert.That(game.EnterSelectedFacilityNearfield(), Is.True);
            Assert.That(game.ViewMode,
                Is.EqualTo(LuoyangPlayableViewMode.Person));
            Assert.That(game.ViewFocusFacilityId, Is.EqualTo(observedGate));
            Assert.That(new PlayerSession(game.BoundWorld).ControlledPerson
                .CurrentFacilityId, Is.EqualTo(playerFacilityBeforeViews));
            Assert.That(new PlayerSession(game.BoundWorld).ControlledPerson
                .CurrentCellId64, Is.EqualTo(playerCellBeforeViews));
            Assert.That(game.NaturalMap.LuoyangNearfieldFocusFacilityId,
                Is.EqualTo(observedGate));
            Assert.That(game.NaturalMap.LuoyangDebugCellPresentationVisible,
                Is.False);
            Assert.That(game.NaturalMap
                .RuntimeLuoyangNearfieldContextFacilityCount, Is.EqualTo(9));
            Assert.That(GameObject.Find(
                "LUOYANG_NEARFIELD_SEAMLESS_GROUND_V1"), Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<Transform>()
                .Any(item => item.name.StartsWith("LOCAL_TERRAIN_",
                    System.StringComparison.Ordinal)), Is.False);
            Assert.That(game.ShowPersonView(), Is.True);
            Assert.That(game.ViewFocusFacilityId, Is.Null);
            Assert.That(WorldSnapshotSerializer.Serialize(game.BoundWorld),
                Is.EqualTo(worldBeforeViews));

            Assert.That(game.ToggleStrategicMap(), Is.True);
            Assert.That(game.IsStrategicMapVisible, Is.True);
            Assert.That(game.NaturalMap.HumanScaleLocalPresentationVisible,
                Is.False);
            Assert.That(game.NaturalMap.View,
                Is.EqualTo(HanNaturalMapView.World));
            Assert.That(game.NaturalMap.StrategicGridLod,
                Is.EqualTo(StrategicCellGridLod.NationwideGuide32));

            Assert.That(game.ShowCountyView(), Is.True);
            Assert.That(game.IsCountyViewVisible, Is.True);
            Assert.That(game.ShowPersonView(), Is.True);
            Assert.That(game.IsPersonViewVisible, Is.True);
            Assert.That(game.NaturalMap.LuoyangBuildingPerformancePreviewVisible,
                Is.True);
            Assert.That(game.NaturalMap.LuoyangPassageWorldBound, Is.True);
            Assert.That(game.NaturalMap.HumanScaleLocalPresentationVisible,
                Is.True);

            var openingDay = game.BoundWorld.AbsoluteDay;
            Assert.That(game.RestOneDay(), Is.True);
            Assert.That(game.BoundWorld.AbsoluteDay,
                Is.EqualTo(openingDay + 1));
        }

        [UnityTest]
        [Timeout(300_000)]
        public IEnumerator ThreeLevelView_CapturesRequiredRealGameViews()
        {
            yield return SceneManager.LoadSceneAsync(
                "PlayableDemo", LoadSceneMode.Single);
            yield return null;
            var dashboard = Object.FindObjectOfType<SimulationDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            if (dashboard.DirectGame == null || !dashboard.DirectGame.IsActive)
                Assert.That(dashboard.StartRecommendedLuoyangExperience(),
                    Is.True);
            yield return null;
            var game = dashboard.DirectGame;
            Assert.That(game, Is.Not.Null);
            Directory.CreateDirectory(ThreeLevelEvidenceRoot);
            foreach (var staleName in new[]
                     {
                         "01_world_view_luoyang.png",
                         "02_luoyang_city_overview.png",
                         "03_luoyang_city_mid_zoom.png",
                         "04_luoyang_facility_selection.png",
                         "05_luoyang_person_nearfield.png",
                         "06_luoyang_nearfield_urban_scale.png",
                         "performance-baseline.json"
                     })
            {
                var stalePath = Path.Combine(ThreeLevelEvidenceRoot,
                    staleName);
                if (File.Exists(stalePath)) File.Delete(stalePath);
            }
            var performance = new ThreeLevelPerformanceEvidence
            {
                UnityVersion = Application.unityVersion,
                OperatingSystem = SystemInfo.operatingSystem,
                Processor = SystemInfo.processorType,
                ProcessorCount = SystemInfo.processorCount,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height
            };

            Assert.That(game.ShowWorldView(), Is.True);
            yield return SampleViewPerformance(game, performance);
            yield return CaptureGameView(game,
                "01_world_view_luoyang.png");

            Assert.That(game.ShowCountyView(), Is.True);
            yield return SampleViewPerformance(game, performance);
            yield return CaptureGameView(game,
                "02_luoyang_city_overview.png");

            Assert.That(game.ShowCountySubView(
                CountySubViewMode.UrbanArea), Is.True);
            yield return CaptureGameView(game,
                "03_luoyang_city_mid_zoom.png");

            Assert.That(game.ShowCountySubView(
                CountySubViewMode.Planning), Is.True);
            yield return CaptureGameView(game,
                "04_luoyang_facility_selection.png");

            Assert.That(game.ShowPersonView(), Is.True);
            yield return SampleViewPerformance(game, performance);
            yield return CaptureGameView(game,
                "05_luoyang_person_nearfield.png");

            var playerFacilityId = new PlayerSession(game.BoundWorld)
                .ControlledPerson.CurrentFacilityId;
            Assert.That(game.NaturalMap.SelectLuoyangFacility(
                playerFacilityId), Is.True);
            Assert.That(game.EnterSelectedFacilityNearfield(), Is.True);
            Assert.That(game.NaturalMap
                .RuntimeLuoyangNearfieldContextFacilityCount, Is.EqualTo(9));
            game.NaturalMap.PresentationCamera.orthographicSize = 11f;
            yield return CaptureGameView(game,
                "06_luoyang_nearfield_urban_scale.png");
            File.WriteAllText(Path.Combine(ThreeLevelEvidenceRoot,
                    "performance-baseline.json"),
                JsonUtility.ToJson(performance, true));
        }

        private static IEnumerator CaptureGameView(
            PlayableLuoyangGameController game, string fileName)
        {
            var path = Path.Combine(ThreeLevelEvidenceRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return null;
            yield return null;
            if (Application.isBatchMode)
            {
                game.NaturalMap.CaptureEvidence(path, 1280, 720);
                Assert.That(new FileInfo(path).Length,
                    Is.GreaterThan(10_000), fileName);
                yield break;
            }
            ScreenCapture.CaptureScreenshot(path);
            for (var frame = 0; frame < 180 &&
                 (!File.Exists(path) || new FileInfo(path).Length <= 10_000);
                 frame++)
                yield return null;
            Assert.That(File.Exists(path), Is.True, path);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(10_000),
                fileName);
        }

        private static IEnumerator SampleViewPerformance(
            PlayableLuoyangGameController game,
            ThreeLevelPerformanceEvidence evidence)
        {
            const int sampleFrames = 60;
            var elapsed = 0f;
            for (var frame = 0; frame < sampleFrames; frame++)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            var snapshot = game.GetViewPerformanceSnapshot();
            snapshot.FramesPerSecond = elapsed <= 0f
                ? 0d : sampleFrames / elapsed;
            evidence.Views.Add(snapshot);
        }
    }
}
