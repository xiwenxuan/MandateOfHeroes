using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    public static class LuoyangP0LandmarkThirdBatchArtBuilder
    {
        public const string RevisionId =
            "luoyang.p0.landmark-third-batch.native-prefab.v1";
        public const string AssetRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Batch3";
        private const string SharedRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Final";
        private const string SecondBatchRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Batch2";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0LandmarkThirdBatchV1/" +
            "luoyang_p0_landmark_third_batch_v1.json";

        public sealed class BuildReport
        {
            public int PrefabCount;
            public int MaterialCount;
            public int MeshCount;
            public int Lod0RendererCount;
            public int Lod1RendererCount;
            public int Lod2RendererCount;
        }

        private sealed class Piece
        {
            public string Name;
            public string MeshId;
            public string MaterialId;
            public Vector3 Position;
            public Vector3 Scale;
            public Vector3 Euler;
        }

        private sealed class ModelRecipe
        {
            public string PrefabName;
            public readonly List<Piece>[] Lods =
            {
                new List<Piece>(), new List<Piece>(), new List<Piece>()
            };
        }

        [MenuItem("Mandate/Luoyang/Build P0 Landmark Third Batch V1")]
        public static void BuildFromMenu()
        {
            var report = BuildAssets();
            Debug.Log("Luoyang P0 landmark third batch built: " +
                      report.PrefabCount + " prefabs, " +
                      report.Lod0RendererCount + "/" +
                      report.Lod1RendererCount + "/" +
                      report.Lod2RendererCount + " LOD renderers; revision " +
                      RevisionId + ".");
        }

        public static BuildReport BuildAssets()
        {
            EnsureFolder(AssetRoot);
            var catalog = LoadCatalog();
            var materials = LoadMaterials();
            var meshes = LoadMeshes();
            var recipes = new Dictionary<string, ModelRecipe>(
                StringComparer.Ordinal)
            {
                [LuoyangHistoricalLandmarkKitIds.Lingtai] = BuildLingtai(),
                [LuoyangHistoricalLandmarkKitIds.Taicang] = BuildTaicang(),
                [LuoyangHistoricalLandmarkKitIds.Arsenal] = BuildArsenal(),
                [LuoyangHistoricalLandmarkKitIds.ZhuolongGarden] =
                    BuildZhuolongGarden()
            };
            var report = new BuildReport
            {
                MaterialCount = materials.Count,
                MeshCount = meshes.Count
            };
            foreach (var profile in catalog.Profiles.OrderBy(
                         item => item.ReviewOrder))
            {
                if (!recipes.TryGetValue(profile.FacilityId, out var recipe))
                    throw new InvalidOperationException(
                        "Missing third-batch recipe for " +
                        profile.FacilityId + ".");
                BuildPrefab(profile, recipe, materials, meshes);
                report.PrefabCount++;
                report.Lod0RendererCount += recipe.Lods[0].Count;
                report.Lod1RendererCount += recipe.Lods[1].Count;
                report.Lod2RendererCount += recipe.Lods[2].Count;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        private static LuoyangP0LandmarkThirdBatchCatalog LoadCatalog()
        {
            var path = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), CatalogPath));
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Third-batch catalog is missing.", path);
            var catalog = JsonUtility.FromJson<
                LuoyangP0LandmarkThirdBatchCatalog>(File.ReadAllText(path));
            if (catalog == null || catalog.Profiles == null ||
                catalog.Profiles.Count !=
                LuoyangP0LandmarkThirdBatchIds.ProfileCount)
                throw new InvalidDataException(
                    "Third-batch catalog must contain exactly four profiles.");
            return catalog;
        }

        private static Dictionary<string, Material> LoadMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            Load(result, Earth, SharedRoot + "/Materials/RammedEarth.mat");
            Load(result, Vermilion, SharedRoot + "/Materials/Vermilion.mat");
            Load(result, Tile, SharedRoot + "/Materials/GreyGreenTile.mat");
            Load(result, Stone, SharedRoot + "/Materials/Stone.mat");
            Load(result, Timber, SharedRoot + "/Materials/Timber.mat");
            Load(result, Bronze, SharedRoot + "/Materials/Bronze.mat");
            Load(result, Water, SecondBatchRoot + "/Materials/Water.mat");
            Load(result, Foliage,
                SecondBatchRoot + "/Materials/Foliage.mat");
            return result;
        }

        private static Dictionary<string, Mesh> LoadMeshes()
        {
            var result = new Dictionary<string, Mesh>(StringComparer.Ordinal);
            Load(result, "box", SharedRoot + "/Meshes/NativeBox.asset");
            Load(result, "post",
                SharedRoot + "/Meshes/NativeOctagonalPost.asset");
            Load(result, "hip_roof",
                SharedRoot + "/Meshes/NativeHanHipRoof.asset");
            Load(result, "disc",
                SecondBatchRoot + "/Meshes/NativeWaterDisc.asset");
            Load(result, "canopy",
                SecondBatchRoot + "/Meshes/NativeTreeCanopy.asset");
            return result;
        }

        private static void Load<T>(IDictionary<string, T> result, string id,
            string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException(
                    "Required third-batch shared asset is missing.", path);
            result.Add(id, asset);
        }

        private static void BuildPrefab(
            LuoyangP0LandmarkThirdBatchProfile profile, ModelRecipe recipe,
            IReadOnlyDictionary<string, Material> materials,
            IReadOnlyDictionary<string, Mesh> meshes)
        {
            var root = new GameObject(recipe.PrefabName);
            try
            {
                foreach (var anchor in profile.Anchors)
                {
                    var value = new GameObject(anchor.AnchorId);
                    value.transform.SetParent(root.transform, false);
                    value.transform.localPosition = new Vector3(anchor.X,
                        anchor.Y, anchor.Z);
                }
                var lods = new LOD[3];
                var transitions = new[] { 0.48f, 0.20f, 0.02f };
                for (var level = 0; level < 3; level++)
                {
                    if (recipe.Lods[level].Count == 0)
                        throw new InvalidOperationException(
                            "Third-batch LOD must not be empty.");
                    var lodRoot = new GameObject("LOD" + level);
                    lodRoot.transform.SetParent(root.transform, false);
                    var renderers = recipe.Lods[level].Select(piece =>
                        BuildPiece(piece, lodRoot.transform, materials, meshes))
                        .ToArray();
                    lods[level] = new LOD(transitions[level], renderers);
                }
                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(lods);
                group.RecalculateBounds();
                var path = "Assets/Resources/" +
                           profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path,
                    out var success);
                if (!success || prefab == null)
                    throw new InvalidOperationException(
                        "Could not save third-batch prefab " + path + ".");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Renderer BuildPiece(Piece piece, Transform parent,
            IReadOnlyDictionary<string, Material> materials,
            IReadOnlyDictionary<string, Mesh> meshes)
        {
            var value = new GameObject(piece.Name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = piece.Position;
            value.transform.localEulerAngles = piece.Euler;
            value.transform.localScale = piece.Scale;
            value.AddComponent<MeshFilter>().sharedMesh = meshes[piece.MeshId];
            var renderer = value.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[piece.MaterialId];
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static ModelRecipe BuildLingtai()
        {
            var value = new ModelRecipe { PrefabName = "Lingtai" };
            Add(value, 0, "tier_1", "box", Stone, V(0, .07f, 0),
                V(.84f, .14f, .84f));
            Add(value, 0, "tier_2", "box", Earth, V(0, .20f, 0),
                V(.66f, .14f, .66f));
            Add(value, 0, "tier_3", "box", Earth, V(0, .33f, 0),
                V(.48f, .14f, .48f));
            Add(value, 0, "tier_4", "box", Vermilion, V(0, .47f, 0),
                V(.31f, .16f, .31f));
            Add(value, 0, "deck", "box", Timber, V(0, .59f, 0),
                V(.44f, .07f, .44f));
            Add(value, 0, "sighting_pole", "post", Bronze,
                V(0, .79f, 0), V(.055f, .35f, .055f));
            Add(value, 0, "sighting_arm", "box", Bronze,
                V(.08f, .91f, 0), V(.20f, .025f, .025f));
            Add(value, 0, "south_stair_1", "box", Stone,
                V(0, .055f, -.46f), V(.26f, .05f, .15f));
            Add(value, 0, "south_stair_2", "box", Stone,
                V(0, .105f, -.38f), V(.23f, .05f, .12f));
            AddCornerPosts(value, 0, .24f, .63f);

            Add(value, 1, "tier_1", "box", Stone, V(0, .07f, 0),
                V(.84f, .14f, .84f));
            Add(value, 1, "tier_2", "box", Earth, V(0, .20f, 0),
                V(.66f, .14f, .66f));
            Add(value, 1, "tier_3", "box", Earth, V(0, .33f, 0),
                V(.48f, .14f, .48f));
            Add(value, 1, "tier_4", "box", Vermilion, V(0, .47f, 0),
                V(.31f, .16f, .31f));
            Add(value, 1, "deck", "box", Timber, V(0, .59f, 0),
                V(.44f, .07f, .44f));
            Add(value, 1, "sighting_pole", "post", Bronze,
                V(0, .79f, 0), V(.055f, .35f, .055f));

            Add(value, 2, "tier_1", "box", Stone, V(0, .07f, 0),
                V(.84f, .14f, .84f));
            Add(value, 2, "tier_3", "box", Earth, V(0, .31f, 0),
                V(.52f, .34f, .52f));
            Add(value, 2, "tier_4", "box", Vermilion, V(0, .50f, 0),
                V(.31f, .16f, .31f));
            Add(value, 2, "sighting_pole", "post", Bronze,
                V(0, .79f, 0), V(.055f, .35f, .055f));
            return value;
        }

        private static ModelRecipe BuildTaicang()
        {
            var value = new ModelRecipe { PrefabName = "Taicang" };
            Add(value, 0, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Granary(value, 0, "nw", -.22f, .20f, true);
            Granary(value, 0, "ne", .22f, .20f, true);
            Granary(value, 0, "sw", -.22f, -.18f, true);
            Granary(value, 0, "se", .22f, -.18f, true);
            Add(value, 0, "south_gate", "box", Vermilion,
                V(0, .14f, -.40f), V(.24f, .20f, .10f));
            Add(value, 0, "south_gate_roof", "hip_roof", Tile,
                V(0, .27f, -.40f), V(.34f, .13f, .20f));

            Add(value, 1, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Granary(value, 1, "nw", -.22f, .20f, false);
            Granary(value, 1, "ne", .22f, .20f, false);
            Granary(value, 1, "sw", -.22f, -.18f, false);
            Granary(value, 1, "se", .22f, -.18f, false);

            Add(value, 2, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Add(value, 2, "north_pair", "box", Earth,
                V(0, .22f, .20f), V(.70f, .34f, .28f));
            Add(value, 2, "north_roof", "hip_roof", Tile,
                V(0, .43f, .20f), V(.78f, .14f, .36f));
            Add(value, 2, "south_pair", "box", Earth,
                V(0, .22f, -.18f), V(.70f, .34f, .28f));
            Add(value, 2, "south_roof", "hip_roof", Tile,
                V(0, .43f, -.18f), V(.78f, .14f, .36f));
            return value;
        }

        private static ModelRecipe BuildArsenal()
        {
            var value = new ModelRecipe { PrefabName = "Arsenal" };
            Add(value, 0, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Hall(value, 0, "north_store", V(0, .20f, .25f),
                V(.66f, .30f, .22f), V(.75f, .14f, .31f));
            Wall(value, 0, "west_wall", V(-.39f, .15f, -.05f),
                V(.09f, .24f, .58f));
            Wall(value, 0, "east_wall", V(.39f, .15f, -.05f),
                V(.09f, .24f, .58f));
            Wall(value, 0, "south_wall_w", V(-.24f, .15f, -.35f),
                V(.30f, .24f, .09f));
            Wall(value, 0, "south_wall_e", V(.24f, .15f, -.35f),
                V(.30f, .24f, .09f));
            Hall(value, 0, "gate_tower", V(0, .22f, -.35f),
                V(.24f, .30f, .15f), V(.34f, .14f, .22f));
            Add(value, 0, "weapon_rack_w", "post", Bronze,
                V(-.18f, .20f, -.11f), V(.055f, .34f, .055f),
                V(0, 0, 15f));
            Add(value, 0, "weapon_rack_e", "post", Bronze,
                V(.18f, .20f, -.11f), V(.055f, .34f, .055f),
                V(0, 0, -15f));
            Add(value, 0, "central_path", "box", Stone,
                V(0, .065f, -.06f), V(.12f, .025f, .52f));

            Add(value, 1, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Hall(value, 1, "north_store", V(0, .20f, .25f),
                V(.66f, .30f, .22f), V(.75f, .14f, .31f));
            Wall(value, 1, "west_wall", V(-.39f, .15f, -.05f),
                V(.09f, .24f, .58f));
            Wall(value, 1, "east_wall", V(.39f, .15f, -.05f),
                V(.09f, .24f, .58f));
            Hall(value, 1, "gate_tower", V(0, .22f, -.35f),
                V(.24f, .30f, .15f), V(.34f, .14f, .22f));

            Add(value, 2, "yard", "box", Earth, V(0, .03f, 0),
                V(.88f, .06f, .84f));
            Add(value, 2, "store_mass", "box", Earth,
                V(0, .20f, .25f), V(.66f, .30f, .22f));
            Add(value, 2, "store_roof", "hip_roof", Tile,
                V(0, .39f, .25f), V(.75f, .14f, .31f));
            Add(value, 2, "gate_mass", "box", Vermilion,
                V(0, .22f, -.35f), V(.24f, .30f, .15f));
            return value;
        }

        private static ModelRecipe BuildZhuolongGarden()
        {
            var value = new ModelRecipe { PrefabName = "ZhuolongGarden" };
            Add(value, 0, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.88f, .05f, .84f));
            Add(value, 0, "pond", "disc", Water,
                V(-.18f, .055f, -.12f), V(.46f, .018f, .36f));
            Hall(value, 0, "pavilion", V(.22f, .19f, .19f),
                V(.24f, .28f, .24f), V(.36f, .16f, .36f),
                V(0, 45f, 0));
            Tree(value, 0, "tree_nw", -.30f, .26f, .28f);
            Tree(value, 0, "tree_e", .31f, -.20f, .25f);
            Tree(value, 0, "tree_s", .03f, -.33f, .23f);
            Add(value, 0, "pond_bridge", "box", Timber,
                V(-.02f, .10f, -.12f), V(.46f, .045f, .10f));
            Add(value, 0, "east_gate", "box", Vermilion,
                V(.41f, .14f, 0), V(.10f, .20f, .24f));
            Add(value, 0, "east_gate_roof", "hip_roof", Tile,
                V(.41f, .27f, 0), V(.19f, .12f, .32f));
            Add(value, 0, "garden_path", "box", Stone,
                V(.18f, .055f, -.03f), V(.38f, .025f, .09f),
                V(0, -25f, 0));

            Add(value, 1, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.88f, .05f, .84f));
            Add(value, 1, "pond", "disc", Water,
                V(-.18f, .055f, -.12f), V(.46f, .018f, .36f));
            Hall(value, 1, "pavilion", V(.22f, .19f, .19f),
                V(.24f, .28f, .24f), V(.36f, .16f, .36f),
                V(0, 45f, 0));
            Tree(value, 1, "tree_nw", -.30f, .26f, .28f);
            Tree(value, 1, "tree_e", .31f, -.20f, .25f);
            Add(value, 1, "pond_bridge", "box", Timber,
                V(-.02f, .10f, -.12f), V(.46f, .045f, .10f));

            Add(value, 2, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.88f, .05f, .84f));
            Add(value, 2, "pavilion_mass", "box", Vermilion,
                V(.22f, .19f, .19f), V(.24f, .28f, .24f),
                V(0, 45f, 0));
            Add(value, 2, "pavilion_roof", "hip_roof", Tile,
                V(.22f, .36f, .19f), V(.36f, .16f, .36f),
                V(0, 45f, 0));
            Add(value, 2, "garden_canopy", "canopy", Foliage,
                V(-.22f, .31f, .12f), V(.36f, .36f, .36f));
            return value;
        }

        private static void Granary(ModelRecipe recipe, int lod, string name,
            float x, float z, bool detailed)
        {
            Add(recipe, lod, name + "_drum", "disc", Earth,
                V(x, .22f, z), V(.28f, .34f, .28f));
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                V(x, .43f, z), V(.34f, .14f, .34f), V(0, 45f, 0));
            if (detailed)
            {
                Add(recipe, lod, name + "_belt", "disc", Timber,
                    V(x, .25f, z), V(.30f, .035f, .30f));
                Add(recipe, lod, name + "_door", "box", Vermilion,
                    V(x, .20f, z - .145f), V(.08f, .16f, .025f));
            }
        }

        private static void Hall(ModelRecipe recipe, int lod, string name,
            Vector3 bodyPosition, Vector3 bodyScale, Vector3 roofScale,
            Vector3? euler = null)
        {
            var rotation = euler ?? Vector3.zero;
            Add(recipe, lod, name + "_body", "box", Vermilion,
                bodyPosition, bodyScale, rotation);
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                bodyPosition + V(0, bodyScale.y * .62f, 0), roofScale,
                rotation);
        }

        private static void Wall(ModelRecipe recipe, int lod, string name,
            Vector3 position, Vector3 scale)
        {
            Add(recipe, lod, name, "box", Earth, position, scale);
            Add(recipe, lod, name + "_coping", "box", Tile,
                position + V(0, scale.y * .55f, 0),
                V(scale.x * 1.05f, .035f, scale.z * 1.05f));
        }

        private static void Tree(ModelRecipe recipe, int lod, string name,
            float x, float z, float height)
        {
            Add(recipe, lod, name + "_trunk", "post", Timber,
                V(x, height * .48f, z), V(.04f, height * .48f, .04f));
            Add(recipe, lod, name + "_canopy", "canopy", Foliage,
                V(x, height + .12f, z), V(.24f, .27f, .24f));
        }

        private static void AddCornerPosts(ModelRecipe recipe, int lod,
            float offset, float y)
        {
            foreach (var x in new[] { -offset, offset })
            foreach (var z in new[] { -offset, offset })
                Add(recipe, lod, "deck_post_" + x + "_" + z, "post",
                    Vermilion, V(x, y, z), V(.025f, .08f, .025f));
        }

        private static void Add(ModelRecipe recipe, int lod, string name,
            string mesh, string material, Vector3 position, Vector3 scale,
            Vector3? euler = null)
        {
            recipe.Lods[lod].Add(new Piece
            {
                Name = name,
                MeshId = mesh,
                MaterialId = material,
                Position = position,
                Scale = scale,
                Euler = euler ?? Vector3.zero
            });
        }

        private static Vector3 V(float x, float y, float z) =>
            new Vector3(x, y, z);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parent))
                throw new InvalidOperationException(
                    "Cannot create asset folder " + path + ".");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private const string Earth = "material.han.p0.rammed_earth";
        private const string Vermilion = "material.han.p0.vermilion";
        private const string Tile = "material.han.p0.grey_green_tile";
        private const string Stone = "material.han.p0.stone";
        private const string Timber = "material.han.p0.timber";
        private const string Bronze = "material.han.p0.bronze";
        private const string Water = "material.han.p0.batch2.water";
        private const string Foliage = "material.han.p0.batch2.foliage";
    }
}
