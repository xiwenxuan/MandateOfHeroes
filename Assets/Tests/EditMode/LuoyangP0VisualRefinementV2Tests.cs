using System.Collections.Generic;
using System.Linq;
using Mandate.Editor;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0VisualRefinementV2Tests
    {
        [Test]
        public void RefinedPrefabs_KeepDistinctDetailsAndStrictLodReduction()
        {
            var report = LuoyangP0NativePrefabArtBuilder.BuildAssets();
            Debug.Log("Luoyang P0 V2 build report: prefabs=" +
                      report.PrefabCount + ", materials=" +
                      report.MaterialCount + ", meshes=" +
                      report.MeshCount + ", lod-renderers=" +
                      report.Lod0RendererCount + "/" +
                      report.Lod1RendererCount + "/" +
                      report.Lod2RendererCount + ".");
            Assert.That(LuoyangP0NativePrefabArtBuilder.RevisionId,
                Is.EqualTo("luoyang.p0.native-prefab.visual-refinement.v2"));
            Assert.That(report.Lod0RendererCount, Is.GreaterThanOrEqualTo(120));
            Assert.That(report.Lod1RendererCount,
                Is.LessThan(report.Lod0RendererCount));
            Assert.That(report.Lod2RendererCount,
                Is.LessThan(report.Lod1RendererCount));

            var expectedDetails = new Dictionary<string, string[]>
            {
                {
                    "SouthPalace", new[]
                    {
                        "central_court_paving", "court_gate_lintel",
                        "front_hall_ridge", "rear_hall_ridge"
                    }
                },
                {
                    "Mingtang", new[]
                    {
                        "lower_canopy", "upper_drum", "upper_roof_ridge",
                        "south_axial_path"
                    }
                },
                {
                    "Guangyangmen", new[]
                    {
                        "gate_leaf_west", "barbican_crossbeam",
                        "barbican_tower_west", "gatehouse_ridge"
                    }
                },
                {
                    "NorthPalaceSouthGate", new[]
                    {
                        "west_que_ridge", "east_que_ridge",
                        "ceremonial_stair", "west_banner_post"
                    }
                }
            };

            foreach (var entry in expectedDetails)
            {
                var path = LuoyangP0NativePrefabArtBuilder.AssetRoot + "/" +
                           entry.Key + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                var names = prefab.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                foreach (var detail in entry.Value)
                    Assert.That(names, Does.Contain(detail), path);
                var lods = prefab.GetComponent<LODGroup>().GetLODs();
                Assert.That(lods[0].renderers.Length,
                    Is.GreaterThan(lods[1].renderers.Length), path);
                Assert.That(lods[1].renderers.Length,
                    Is.GreaterThan(lods[2].renderers.Length), path);
            }
        }

        [Test]
        public void CloseupCameras_UseTightObliqueReviewFraming()
        {
            var ids = new[]
            {
                StrategicCellCameraRig.LuoyangP0SouthPalaceCloseup,
                StrategicCellCameraRig.LuoyangP0MingtangCloseup,
                StrategicCellCameraRig.LuoyangP0GuangyangmenCloseup,
                StrategicCellCameraRig.LuoyangP0NorthPalaceGateCloseup
            };
            var cameras = ids.Select(StrategicCellCameraRig.Get).ToArray();
            Assert.That(cameras.All(item => item.Size <= 1.45f), Is.True);
            Assert.That(cameras.All(item => item.Pitch >= 38f &&
                item.Pitch <= 44f), Is.True);
            Assert.That(cameras.Select(item => item.Yaw).Distinct().Count(),
                Is.GreaterThanOrEqualTo(3));
        }
    }
}
