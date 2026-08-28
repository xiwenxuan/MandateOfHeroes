using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using NUnit.Framework;

namespace Mandate.Tests
{
    public sealed class ZhonghuaSanguozhiFusionStyleV1Tests
    {
        [Test]
        public void StyleD_ProfileAndCamerasAreStableAndPresentationOnly()
        {
            var profile = HanWorldArtProfileCatalog.Get(HanWorldArtStyle.ZhonghuaSanguozhiFusion);
            Assert.That(profile.ProfileId, Is.EqualTo(HanWorldArtProfileCatalog.StyleDId));
            Assert.That(profile.FusionStrength, Is.InRange(0.01f, 1f));
            Assert.That(profile.ForestCanopyScale, Is.GreaterThan(0f));
            foreach (var id in new[]
            {
                ZhonghuaFusionCameraRig.World, ZhonghuaFusionCameraRig.Region,
                ZhonghuaFusionCameraRig.Mountain, ZhonghuaFusionCameraRig.River,
                ZhonghuaFusionCameraRig.Forest, ZhonghuaFusionCameraRig.Plain,
                ZhonghuaFusionCameraRig.WorldToRegionMid,
                ZhonghuaFusionCameraRig.CityDistancePreview
            })
            {
                var camera = ZhonghuaFusionCameraRig.Get(id);
                Assert.That(camera.Id, Is.EqualTo(id));
                Assert.That(camera.Row, Is.InRange(0, GlobalSpatialFoundationV1.Rows - 1));
                Assert.That(camera.Column, Is.InRange(0, GlobalSpatialFoundationV1.Columns - 1));
            }
            Assert.That(typeof(HanWorldArtProfile).GetFields().Select(value => value.Name),
                Has.None.Contains("Elevation"));
        }

        [Test]
        public void FeatureAnalyzer_DerivesBoundedFeaturesWithoutMutatingSource()
        {
            var vertices = new[]
            {
                V(0, 2, 200), V(1, 2, 260), V(2, 2, 220),
                V(0, 1, 180), V(1, 1, 820, NaturalSurfaceIds.Forest), V(2, 1, 190),
                V(0, 0, 120), V(1, 0, 70, NaturalSurfaceIds.River), V(2, 0, 110)
            };
            var originalElevations = vertices.Select(value => value.SourceElevationMetres).ToArray();
            var data = new NaturalTerrainMeshData
            {
                Vertices = vertices,
                Triangles = new[] { 0, 3, 1, 1, 3, 4, 1, 4, 2, 2, 4, 5, 3, 6, 4, 4, 6, 7, 4, 7, 5, 5, 7, 8 },
                Tile = new TerrainTileDefinition(new TerrainTileId(0, 0), 0, 1, 0, 1, 0, 0, 2, 2)
            };

            var features = ZhonghuaFusionTerrainFeatureAnalyzer.Analyze(data);

            Assert.That(features.Primary, Has.Count.EqualTo(vertices.Length));
            Assert.That(features.Secondary, Has.Count.EqualTo(vertices.Length));
            Assert.That(features.Primary.SelectMany(Vals).All(value => value >= 0f && value <= 1f), Is.True);
            Assert.That(features.Secondary.SelectMany(Vals).All(value => value >= 0f && value <= 1f), Is.True);
            Assert.That(features.MountainVertices, Is.GreaterThan(0));
            Assert.That(features.ForestVertices, Is.GreaterThan(0));
            Assert.That(features.RiverValleyVertices, Is.GreaterThan(0));
            Assert.That(vertices.Select(value => value.SourceElevationMetres), Is.EqualTo(originalElevations));
        }

        private static NaturalTerrainVertex V(double x, double y, double elevation,
            string surface = NaturalSurfaceIds.Grassland) => new NaturalTerrainVertex(x * 2000d,
            y * 2000d, elevation, elevation, new NaturalSurfaceBlend(surface, surface,
                NaturalLandformIds.Plain, 0d));

        private static float[] Vals(UnityEngine.Vector4 value) =>
            new[] { value.x, value.y, value.z, value.w };
    }
}
