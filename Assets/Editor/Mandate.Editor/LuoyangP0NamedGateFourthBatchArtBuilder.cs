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
    public static class LuoyangP0NamedGateFourthBatchArtBuilder
    {
        public const string RevisionId =
            "luoyang.p0.named-gate-fourth-batch.native-prefab.v1";
        public const string AssetRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Batch4";
        private const string SharedRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Final";
        private const string SecondBatchRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Batch2";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0NamedGateFourthBatchV1/" +
            "luoyang_p0_named_gate_fourth_batch_v1.json";

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

        [MenuItem("Mandate/Luoyang/Build P0 Named Gate Fourth Batch V1")]
        public static void BuildFromMenu()
        {
            var report = BuildAssets();
            Debug.Log("Luoyang P0 named-gate fourth batch built: " +
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
                [LuoyangGateIdentityKitIds.Gumen] = BuildGumen(),
                [LuoyangGateIdentityKitIds.Jinmen] = BuildJinmen(),
                [LuoyangGateIdentityKitIds.Kaiyangmen] = BuildKaiyangmen(),
                [LuoyangGateIdentityKitIds.Maomen] = BuildMaomen()
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
                        "Missing named-gate fourth-batch recipe for " +
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

        private static LuoyangP0NamedGateFourthBatchCatalog LoadCatalog()
        {
            var path = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), CatalogPath));
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Named-gate fourth-batch catalog is missing.", path);
            var catalog = JsonUtility.FromJson<
                LuoyangP0NamedGateFourthBatchCatalog>(File.ReadAllText(path));
            if (catalog == null || catalog.Profiles == null ||
                catalog.Profiles.Count !=
                LuoyangP0NamedGateFourthBatchIds.ProfileCount)
                throw new InvalidDataException(
                    "Named-gate fourth-batch catalog must contain exactly four profiles.");
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
            Load(result, "road",
                SharedRoot + "/Meshes/NativeRoadCrown.asset");
            return result;
        }

        private static void Load<T>(IDictionary<string, T> result, string id,
            string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException(
                    "Required named-gate shared asset is missing.", path);
            result.Add(id, asset);
        }

        private static void BuildPrefab(
            LuoyangP0NamedGateFourthBatchProfile profile, ModelRecipe recipe,
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
                            "Named-gate fourth-batch LOD must not be empty.");
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
                        "Could not save named-gate fourth-batch prefab " +
                        path + ".");
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

        private static ModelRecipe BuildGumen()
        {
            var value = new ModelRecipe { PrefabName = "Gumen" };
            AddRoad(value, 0, Stone, .14f);
            GateWalls(value, 0, .33f, .19f, .20f, .32f, .22f);
            GatePiers(value, 0, Timber, .15f, .24f, .12f, .42f, .20f);
            GateHouse(value, 0, .50f, .42f, .17f, .26f,
                .63f, .58f, .15f, .36f);
            Add(value, 0, "ridge", "box", Bronze, V(0, .75f, 0),
                V(.36f, .035f, .035f));
            Add(value, 0, "watch_post_w", "post", Vermilion,
                V(-.16f, .68f, 0), V(.03f, .12f, .03f));
            Add(value, 0, "watch_post_e", "post", Vermilion,
                V(.16f, .68f, 0), V(.03f, .12f, .03f));

            AddRoad(value, 1, Stone, .14f);
            GateWalls(value, 1, .33f, .19f, .20f, .32f, .22f);
            GateHouse(value, 1, .50f, .42f, .17f, .26f,
                .63f, .58f, .15f, .36f);

            GateWalls(value, 2, .33f, .20f, .20f, .34f, .22f, false);
            Add(value, 2, "gate_roof", "hip_roof", Tile,
                V(0, .59f, 0), V(.58f, .22f, .36f));
            return value;
        }

        private static ModelRecipe BuildJinmen()
        {
            var value = new ModelRecipe { PrefabName = "Jinmen" };
            Add(value, 0, "causeway", "road", Stone, V(0, .035f, -.08f),
                V(.24f, .07f, .78f));
            Add(value, 0, "water_w", "box", Water, V(-.28f, .025f, -.16f),
                V(.22f, .018f, .52f));
            Add(value, 0, "water_e", "box", Water, V(.28f, .025f, -.16f),
                V(.22f, .018f, .52f));
            GateWalls(value, 0, .33f, .18f, .20f, .30f, .24f, true, .04f);
            GatePiers(value, 0, Vermilion, .15f, .24f, .12f, .42f,
                .22f, .04f);
            GateHouse(value, 0, .49f, .44f, .15f, .27f,
                .61f, .60f, .13f, .38f, .04f);
            Add(value, 0, "jin_marker", "post", Stone,
                V(-.28f, .18f, -.25f), V(.08f, .16f, .08f));
            Add(value, 0, "marker_cap", "hip_roof", Tile,
                V(-.28f, .31f, -.25f), V(.15f, .08f, .15f));

            Add(value, 1, "causeway", "road", Stone, V(0, .035f, -.08f),
                V(.24f, .07f, .78f));
            GateWalls(value, 1, .33f, .18f, .20f, .30f, .24f, true, .04f);
            GateHouse(value, 1, .49f, .44f, .15f, .27f,
                .61f, .60f, .13f, .38f, .04f);
            Add(value, 1, "jin_marker", "post", Stone,
                V(-.28f, .18f, -.25f), V(.08f, .16f, .08f));

            GateWalls(value, 2, .33f, .19f, .20f, .32f, .24f, false, .04f);
            Add(value, 2, "gate_roof", "hip_roof", Tile,
                V(0, .58f, .04f), V(.60f, .20f, .38f));
            return value;
        }

        private static ModelRecipe BuildKaiyangmen()
        {
            var value = new ModelRecipe { PrefabName = "Kaiyangmen" };
            AddRoad(value, 0, Stone, .18f);
            GateWalls(value, 0, .34f, .21f, .18f, .36f, .24f, true, .06f);
            GatePiers(value, 0, Vermilion, .15f, .28f, .13f, .50f,
                .22f, .06f);
            GateHouse(value, 0, .58f, .48f, .20f, .30f,
                .73f, .64f, .16f, .40f, .06f);
            Que(value, 0, "west_que", -.34f, -.18f);
            Que(value, 0, "east_que", .34f, -.18f);
            Add(value, 0, "ceremonial_ridge", "box", Bronze,
                V(0, .86f, .06f), V(.38f, .035f, .035f));

            AddRoad(value, 1, Stone, .18f);
            GateWalls(value, 1, .34f, .21f, .18f, .36f, .24f, true, .06f);
            GateHouse(value, 1, .58f, .48f, .20f, .30f,
                .73f, .64f, .16f, .40f, .06f);
            Add(value, 1, "west_que", "box", Vermilion,
                V(-.34f, .43f, -.18f), V(.14f, .28f, .16f));
            Add(value, 1, "east_que", "box", Vermilion,
                V(.34f, .43f, -.18f), V(.14f, .28f, .16f));

            GateWalls(value, 2, .34f, .23f, .18f, .40f, .24f, false, .06f);
            Add(value, 2, "gate_roof", "hip_roof", Tile,
                V(0, .69f, .06f), V(.64f, .24f, .40f));
            return value;
        }

        private static ModelRecipe BuildMaomen()
        {
            var value = new ModelRecipe { PrefabName = "Maomen" };
            AddRoad(value, 0, Stone, .13f);
            GateWalls(value, 0, .32f, .22f, .22f, .38f, .24f);
            GatePiers(value, 0, Timber, .14f, .24f, .11f, .42f, .20f);
            GateHouse(value, 0, .49f, .38f, .15f, .25f,
                .61f, .52f, .13f, .34f);
            Add(value, 0, "guard_mast", "post", Timber,
                V(.31f, .44f, -.16f), V(.05f, .22f, .05f));
            Add(value, 0, "guard_cap", "box", Bronze,
                V(.31f, .61f, -.16f), V(.09f, .04f, .09f));

            AddRoad(value, 1, Stone, .13f);
            GateWalls(value, 1, .32f, .22f, .22f, .38f, .24f);
            GateHouse(value, 1, .49f, .38f, .15f, .25f,
                .61f, .52f, .13f, .34f);

            GateWalls(value, 2, .32f, .22f, .22f, .38f, .24f, false);
            Add(value, 2, "gate_roof", "hip_roof", Tile,
                V(0, .58f, 0), V(.52f, .20f, .34f));
            return value;
        }

        private static void AddRoad(ModelRecipe recipe, int lod,
            string material, float width)
        {
            Add(recipe, lod, "passage_road", "road", material,
                V(0, .025f, 0), V(width, .05f, .86f));
        }

        private static void GateWalls(ModelRecipe recipe, int lod,
            float x, float y, float width, float height, float depth,
            bool includeCoping = true, float z = 0f)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var name = side < 0f ? "west_wall" : "east_wall";
                Add(recipe, lod, name, "box", Earth,
                    V(side * x, y, z), V(width, height, depth));
                if (includeCoping)
                    Add(recipe, lod, name + "_coping", "box", Tile,
                        V(side * x, y + height * .55f, z),
                        V(width * 1.06f, .035f, depth * 1.08f));
            }
        }

        private static void GatePiers(ModelRecipe recipe, int lod,
            string material, float x, float y, float width, float height,
            float depth, float z = 0f)
        {
            Add(recipe, lod, "west_pier", "box", material,
                V(-x, y, z), V(width, height, depth));
            Add(recipe, lod, "east_pier", "box", material,
                V(x, y, z), V(width, height, depth));
        }

        private static void GateHouse(ModelRecipe recipe, int lod,
            float y, float width, float height, float depth,
            float roofY, float roofWidth, float roofHeight, float roofDepth,
            float z = 0f)
        {
            Add(recipe, lod, "gate_house", "box", Vermilion,
                V(0, y, z), V(width, height, depth));
            Add(recipe, lod, "gate_roof", "hip_roof", Tile,
                V(0, roofY, z), V(roofWidth, roofHeight, roofDepth));
        }

        private static void Que(ModelRecipe recipe, int lod, string name,
            float x, float z)
        {
            Add(recipe, lod, name, "box", Vermilion,
                V(x, .43f, z), V(.14f, .28f, .16f));
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                V(x, .61f, z), V(.22f, .10f, .24f));
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
    }
}
