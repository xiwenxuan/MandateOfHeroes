using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0NativePrefabArtDeliveryV1Tests
    {
        [Test]
        public void BuildAssets_CreatesFourReplaceableThreeLodPrefabs()
        {
            var report = LuoyangP0NativePrefabArtBuilder.BuildAssets();
            Assert.That(report.PrefabCount, Is.EqualTo(4));
            Assert.That(report.MaterialCount, Is.EqualTo(6));
            Assert.That(report.MeshCount, Is.EqualTo(4));
            Assert.That(report.Lod0RendererCount, Is.GreaterThanOrEqualTo(120));
            Assert.That(report.Lod1RendererCount,
                Is.LessThan(report.Lod0RendererCount));
            Assert.That(report.Lod2RendererCount,
                Is.LessThan(report.Lod1RendererCount));

            var catalogPath = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap",
                "LuoyangP0FinalAssetVerticalSliceV1",
                "luoyang_p0_final_asset_vertical_slice_v1.json");
            var catalog = JsonUtility.FromJson<
                LuoyangP0FinalAssetVerticalSliceCatalog>(
                File.ReadAllText(catalogPath));
            foreach (var profile in catalog.Profiles)
            {
                var path = "Assets/Resources/" +
                           profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(profile.ArtistPrefabPresent, Is.True,
                    profile.CandidateId);
                Assert.That(profile.FinalArtApproved, Is.True,
                    profile.CandidateId);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true),
                    Is.Empty, path);
                var group = prefab.GetComponent<LODGroup>();
                Assert.That(group, Is.Not.Null, path);
                var lods = group.GetLODs();
                Assert.That(lods, Has.Length.EqualTo(3), path);
                Assert.That(lods.All(lod => lod.renderers != null &&
                    lod.renderers.Length > 0 && lod.renderers.All(renderer =>
                        renderer != null && renderer.sharedMaterial != null)),
                    Is.True, path);
                Assert.That(lods[0].renderers.Length,
                    Is.GreaterThanOrEqualTo(24), path);
                var names = prefab.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                foreach (var anchor in profile.Anchors)
                    Assert.That(names, Does.Contain(anchor.AnchorId), path);
            }
        }
    }
}
