using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed partial class WorldKernelTests
    {
        [Test]
        public void MapViewRoutingCountyRenameAndInkPrototype_RoutesMcfAndCountySubviews()
        {
            var state = new LuoyangPlayableViewState();
            state.ShowWorld();
            Assert.That(state.Mode, Is.EqualTo(
                LuoyangPlayableViewMode.World));
            state.ShowCounty(Luoyang50mCountySpatialPrototypeIds.CountyId);
            Assert.That(state.Mode, Is.EqualTo(
                LuoyangPlayableViewMode.County));
            Assert.That(state.CountySubView, Is.EqualTo(
                CountySubViewMode.Overview));
            state.SetCountySubView(CountySubViewMode.UrbanArea);
            state.SetCountySubView(CountySubViewMode.Planning);
            state.SetCountySubView(CountySubViewMode.Overview);
            Assert.That(state.ObservedCountyId, Is.EqualTo(
                Luoyang50mCountySpatialPrototypeIds.CountyId));
            state.ShowPlayer();
            Assert.That(state.Mode, Is.EqualTo(
                LuoyangPlayableViewMode.Person));

            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('M',
                out var world), Is.True);
            Assert.That(world, Is.EqualTo(
                LuoyangPlayableViewCommand.ShowWorld));
            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('C',
                out var county), Is.True);
            Assert.That(county, Is.EqualTo(
                LuoyangPlayableViewCommand.ShowCounty));
            Assert.That(LuoyangPlayableViewCommandBindings.TryResolve('F',
                out var person), Is.True);
            Assert.That(person, Is.EqualTo(
                LuoyangPlayableViewCommand.ShowPerson));
        }

        [Test]
        public void MapViewRoutingCountyRenameAndInkPrototype_SubviewsDoNotMutateWorld()
        {
            var source = new LuoyangHumanScaleLocalMapPlanSource(
                DirectLuoyangWorldMapRoot());
            var world = PlayableLuoyangWorldFactory.Create(
                source.Plan, source.Performance, 184_001UL);
            var before = WorldSnapshotSerializer.Serialize(world);
            var facilityIds = world.Facilities.Select(item => item.Id)
                .OrderBy(value => value).ToArray();
            var player = new PlayerSession(world).ControlledPerson;
            var playerFacility = player.CurrentFacilityId;
            var playerCell = player.CurrentCellId64;
            var view = new LuoyangPlayableViewState();

            view.ShowWorld();
            view.ShowCounty("admin.han140.youzhou.zhuo.zhuo");
            view.SetCountySubView(CountySubViewMode.UrbanArea);
            view.SetCountySubView(CountySubViewMode.Planning);
            view.ShowPlayer();

            Assert.That(new PlayerSession(world).ControlledPerson
                .CurrentFacilityId, Is.EqualTo(playerFacility));
            Assert.That(new PlayerSession(world).ControlledPerson
                .CurrentCellId64, Is.EqualTo(playerCell));
            Assert.That(world.Facilities.Select(item => item.Id)
                .OrderBy(value => value), Is.EqualTo(facilityIds));
            Assert.That(WorldSnapshotSerializer.Serialize(world),
                Is.EqualTo(before));
        }

        [Test]
        public void MapViewRoutingCountyRenameAndInkPrototype_DeprecatedCityMeansCountyUrbanArea()
        {
            var view = new LuoyangPlayableViewState();
#pragma warning disable CS0618
            view.ShowCity(PlayableLuoyangWorldContractIds.MarketFacilityId);
#pragma warning restore CS0618
            Assert.That(view.Mode, Is.EqualTo(
                LuoyangPlayableViewMode.County));
            Assert.That(view.CountySubView, Is.EqualTo(
                CountySubViewMode.UrbanArea));
            Assert.That(view.ObservedCountyId, Is.EqualTo(
                Luoyang50mCountySpatialPrototypeIds.CountyId));
        }

    }

    public sealed class MapViewRoutingCountyRenameAndInkPrototypeV1UnityTests
    {
        [Test]
        public void InkPrototype_IsPresentationOnlyProfile()
        {
            var current = HanWorldArtProfileCatalog.Get(
                HanWorldArtStyle.ChineseSemiRealistic);
            var ink = HanWorldArtProfileCatalog.Get(
                HanWorldArtStyle.InkLandscapePrototype);
            Assert.That(ink.ProfileId, Is.EqualTo(
                HanWorldArtProfileCatalog.InkPrototypeId));
            Assert.That(ink.InkStrength, Is.GreaterThan(0f));
            Assert.That(ink.PaperTextureStrength, Is.GreaterThan(0f));
            Assert.That(ink.RiverTint, Is.Not.EqualTo(current.RiverTint));
            Assert.That(ink.RoadTint, Is.Not.EqualTo(current.RoadTint));
            Assert.That(ink.VisualIntent, Does.Contain("gazetteer"));
        }

        [Test]
        public void HanStrategicDiorama_IsColouredPresentationOnlyProfile()
        {
            var profile = HanWorldArtProfileCatalog.Get(
                HanWorldArtStyle.HanStrategicDiorama);
            Assert.That(profile.ProfileId, Is.EqualTo(
                HanWorldArtProfileCatalog.StrategicDioramaId));
            Assert.That(profile.DioramaStrength, Is.GreaterThan(0.8f));
            Assert.That(profile.DioramaLightBands, Is.InRange(3f, 8f));
            Assert.That(profile.DioramaEdgeStrength, Is.GreaterThan(0f));
            Assert.That(profile.Saturation, Is.GreaterThan(1f));
            Assert.That(profile.InkStrength, Is.Zero);
            Assert.That(profile.PaperTextureStrength, Is.Zero);
            Assert.That(profile.VisualIntent, Does.Contain("clean-room"));
        }

        [Test]
        public void HanStrategicDioramaCameraRig_FreezesTiltedWorldLods()
        {
            var world = HanStrategicDioramaCameraRig.Get(
                HanStrategicDioramaCameraRig.World);
            var middle = HanStrategicDioramaCameraRig.Get(
                HanStrategicDioramaCameraRig.WorldMid);
            var near = HanStrategicDioramaCameraRig.Get(
                HanStrategicDioramaCameraRig.WorldNear);
            Assert.That(world.Size, Is.GreaterThan(middle.Size));
            Assert.That(middle.Size, Is.GreaterThan(near.Size));
            Assert.That(world.Pitch, Is.InRange(50f, 62f));
            Assert.That(middle.Pitch, Is.InRange(50f, 62f));
            Assert.That(near.Pitch, Is.InRange(48f, 58f));
            Assert.That(world.Yaw, Is.Zero,
                "The full map remains horizontally composed while pitch provides relief.");
        }

        [Test]
        public void CountySubviews_UseDistinctOverviewUrbanAndPlanningScales()
        {
            var root = new GameObject("County Subview Scale Test");
            try
            {
                var planning = root.AddComponent<
                    LuoyangCountyPlanningPresentationController>();
                Assert.That(planning.Begin(
                    Luoyang50mCountySpatialPrototypeIds.CountyId,
                    CountySubViewMode.Overview), Is.True,
                    planning.LastError);
                Assert.That(planning.ViewRows, Is.EqualTo(320f));
                Assert.That(planning.ViewColumns, Is.EqualTo(640f));

                Assert.That(planning.SetPresentationMode(
                    CountySubViewMode.UrbanArea), Is.True);
                Assert.That(planning.ViewRows, Is.EqualTo(160f));
                Assert.That(planning.ViewColumns, Is.EqualTo(320f));

                Assert.That(planning.SetPresentationMode(
                    CountySubViewMode.Planning), Is.True);
                Assert.That(planning.ViewRows, Is.EqualTo(24f));
                Assert.That(planning.ViewColumns, Is.EqualTo(48f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
