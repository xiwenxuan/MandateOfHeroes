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
    public static class LuoyangP0LandmarkSecondBatchArtBuilder
    {
        public const string RevisionId =
            "luoyang.p0.landmark-second-batch.native-prefab.v1";
        public const string AssetRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Batch2";
        public const string MaterialRoot = AssetRoot + "/Materials";
        public const string MeshRoot = AssetRoot + "/Meshes";
        private const string SharedRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Final";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0LandmarkSecondBatchV1/" +
            "luoyang_p0_landmark_second_batch_v1.json";

        public sealed class BuildReport
        {
            public int PrefabCount;
            public int SharedMaterialCount;
            public int BatchMaterialCount;
            public int SharedMeshCount;
            public int BatchMeshCount;
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

        [MenuItem("Mandate/Luoyang/Build P0 Landmark Second Batch V1")]
        public static void BuildFromMenu()
        {
            var report = BuildAssets();
            Debug.Log("Luoyang P0 landmark second batch built: " +
                      report.PrefabCount + " prefabs, " +
                      report.Lod0RendererCount + "/" +
                      report.Lod1RendererCount + "/" +
                      report.Lod2RendererCount + " LOD renderers; revision " +
                      RevisionId + ".");
        }

        public static BuildReport BuildAssets()
        {
            EnsureFolder(AssetRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(MeshRoot);
            var catalog = LoadCatalog();
            var materials = LoadAndBuildMaterials();
            var meshes = LoadAndBuildMeshes();
            var recipes = new Dictionary<string, ModelRecipe>(
                StringComparer.Ordinal)
            {
                [LuoyangHistoricalLandmarkKitIds.NorthPalace] =
                    BuildNorthPalace(),
                [LuoyangHistoricalLandmarkKitIds.YonganPalace] =
                    BuildYonganPalace(),
                [LuoyangHistoricalLandmarkKitIds.Taixue] = BuildTaixue(),
                [LuoyangHistoricalLandmarkKitIds.Biyong] = BuildBiyong()
            };
            var report = new BuildReport
            {
                SharedMaterialCount = 6,
                BatchMaterialCount = 2,
                SharedMeshCount = 3,
                BatchMeshCount = 3
            };
            foreach (var profile in catalog.Profiles.OrderBy(
                         item => item.ReviewOrder))
            {
                if (!recipes.TryGetValue(profile.FacilityId, out var recipe))
                    throw new InvalidOperationException(
                        "Missing second-batch recipe for " +
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

        private static LuoyangP0LandmarkSecondBatchCatalog LoadCatalog()
        {
            var path = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), CatalogPath));
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Second-batch catalog is missing.", path);
            var catalog = JsonUtility.FromJson<
                LuoyangP0LandmarkSecondBatchCatalog>(File.ReadAllText(path));
            if (catalog == null || catalog.Profiles == null ||
                catalog.Profiles.Count !=
                LuoyangP0LandmarkSecondBatchIds.ProfileCount)
                throw new InvalidDataException(
                    "Second-batch catalog must contain exactly four profiles.");
            return catalog;
        }

        private static Dictionary<string, Material> LoadAndBuildMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            LoadMaterial(result, Earth, SharedRoot + "/Materials/RammedEarth.mat");
            LoadMaterial(result, Vermilion,
                SharedRoot + "/Materials/Vermilion.mat");
            LoadMaterial(result, Tile,
                SharedRoot + "/Materials/GreyGreenTile.mat");
            LoadMaterial(result, Stone, SharedRoot + "/Materials/Stone.mat");
            LoadMaterial(result, Timber, SharedRoot + "/Materials/Timber.mat");
            LoadMaterial(result, Bronze, SharedRoot + "/Materials/Bronze.mat");
            result.Add(Water, UpsertMaterial("Water",
                new Color(0.16f, 0.32f, 0.34f, 1f), 0.05f, 0.58f));
            result.Add(Foliage, UpsertMaterial("Foliage",
                new Color(0.20f, 0.31f, 0.16f, 1f), 0f, 0.18f));
            return result;
        }

        private static void LoadMaterial(IDictionary<string, Material> result,
            string id, string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                throw new FileNotFoundException(
                    "Shared P0 material is missing.", path);
            result.Add(id, material);
        }

        private static Material UpsertMaterial(string name, Color color,
            float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException(
                    "No supported lit shader is available.");
            var candidate = new Material(shader) { name = name };
            if (candidate.HasProperty("_BaseColor"))
                candidate.SetColor("_BaseColor", color);
            if (candidate.HasProperty("_Color"))
                candidate.SetColor("_Color", color);
            if (candidate.HasProperty("_Metallic"))
                candidate.SetFloat("_Metallic", metallic);
            if (candidate.HasProperty("_Smoothness"))
                candidate.SetFloat("_Smoothness", smoothness);
            var path = MaterialRoot + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(candidate, path);
                return candidate;
            }
            EditorUtility.CopySerialized(candidate, existing);
            Object.DestroyImmediate(candidate);
            return existing;
        }

        private static Dictionary<string, Mesh> LoadAndBuildMeshes()
        {
            var result = new Dictionary<string, Mesh>(StringComparer.Ordinal);
            LoadMesh(result, "box", SharedRoot + "/Meshes/NativeBox.asset");
            LoadMesh(result, "post",
                SharedRoot + "/Meshes/NativeOctagonalPost.asset");
            LoadMesh(result, "hip_roof",
                SharedRoot + "/Meshes/NativeHanHipRoof.asset");
            result.Add("disc", UpsertMesh("NativeWaterDisc",
                ClonePrimitive(PrimitiveType.Cylinder, "NativeWaterDisc")));
            result.Add("canopy", UpsertMesh("NativeTreeCanopy",
                ClonePrimitive(PrimitiveType.Sphere, "NativeTreeCanopy")));
            result.Add("ritual_ring", UpsertMesh("NativeRitualRing",
                CreateRingMesh(32, 0.5f, 0.36f, 0.08f)));
            return result;
        }

        private static void LoadMesh(IDictionary<string, Mesh> result,
            string id, string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
                throw new FileNotFoundException("Shared P0 mesh is missing.",
                    path);
            result.Add(id, mesh);
        }

        private static Mesh ClonePrimitive(PrimitiveType type, string name)
        {
            var value = GameObject.CreatePrimitive(type);
            try
            {
                var mesh = Object.Instantiate(
                    value.GetComponent<MeshFilter>().sharedMesh);
                mesh.name = name;
                return mesh;
            }
            finally
            {
                Object.DestroyImmediate(value);
            }
        }

        private static Mesh CreateRingMesh(int segments, float outerRadius,
            float innerRadius, float height)
        {
            var vertices = new Vector3[segments * 4];
            var triangles = new int[segments * 24];
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var cosine = Mathf.Cos(angle);
                var sine = Mathf.Sin(angle);
                var offset = index * 4;
                vertices[offset] = new Vector3(cosine * outerRadius,
                    height * 0.5f, sine * outerRadius);
                vertices[offset + 1] = new Vector3(cosine * innerRadius,
                    height * 0.5f, sine * innerRadius);
                vertices[offset + 2] = new Vector3(cosine * outerRadius,
                    -height * 0.5f, sine * outerRadius);
                vertices[offset + 3] = new Vector3(cosine * innerRadius,
                    -height * 0.5f, sine * innerRadius);
            }
            for (var index = 0; index < segments; index++)
            {
                var current = index * 4;
                var next = ((index + 1) % segments) * 4;
                var triangle = index * 24;
                AddQuad(triangles, triangle, current, next, next + 1,
                    current + 1);
                AddQuad(triangles, triangle + 6, current + 2, current + 3,
                    next + 3, next + 2);
                AddQuad(triangles, triangle + 12, current, current + 2,
                    next + 2, next);
                AddQuad(triangles, triangle + 18, current + 1, next + 1,
                    next + 3, current + 3);
            }
            var mesh = new Mesh { name = "NativeRitualRing" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(int[] triangles, int offset, int a, int b,
            int c, int d)
        {
            triangles[offset] = a;
            triangles[offset + 1] = b;
            triangles[offset + 2] = c;
            triangles[offset + 3] = a;
            triangles[offset + 4] = c;
            triangles[offset + 5] = d;
        }

        private static Mesh UpsertMesh(string name, Mesh candidate)
        {
            var path = MeshRoot + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(candidate, path);
                return candidate;
            }
            EditorUtility.CopySerialized(candidate, existing);
            Object.DestroyImmediate(candidate);
            return existing;
        }

        private static void BuildPrefab(
            LuoyangP0LandmarkSecondBatchProfile profile, ModelRecipe recipe,
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
                            "Second-batch LOD must not be empty.");
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
                        "Could not save second-batch prefab " + path + ".");
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

        private static ModelRecipe BuildNorthPalace()
        {
            var value = new ModelRecipe { PrefabName = "NorthPalace" };
            Add(value, 0, "platform", "box", Stone, V(0, .06f, .03f),
                V(.90f, .12f, .86f));
            Add(value, 0, "south_stair", "box", Stone, V(0, .07f, -.46f),
                V(.24f, .08f, .20f));
            Hall(value, 0, "main_hall", V(0, .32f, .18f),
                V(.62f, .40f, .30f), V(.74f, .21f, .42f));
            Hall(value, 0, "front_hall", V(0, .21f, -.10f),
                V(.46f, .25f, .22f), V(.58f, .16f, .34f));
            Que(value, 0, "west_que", -.32f, -.28f);
            Que(value, 0, "east_que", .32f, -.28f);
            Add(value, 0, "gate_beam", "box", Timber, V(0, .24f, -.28f),
                V(.44f, .06f, .06f));
            Add(value, 0, "court_paving", "box", Stone,
                V(0, .13f, .02f), V(.50f, .025f, .23f));
            AddPosts(value, 0, "main", .18f, .28f,
                new[] { -.24f, -.08f, .08f, .24f });
            Add(value, 0, "bronze_finial", "post", Bronze,
                V(0, .68f, .18f), V(.035f, .07f, .035f));

            Add(value, 1, "platform", "box", Stone, V(0, .06f, .03f),
                V(.90f, .12f, .86f));
            Hall(value, 1, "main_hall", V(0, .32f, .18f),
                V(.62f, .40f, .30f), V(.74f, .21f, .42f));
            Hall(value, 1, "front_hall", V(0, .21f, -.10f),
                V(.46f, .25f, .22f), V(.58f, .16f, .34f));
            Que(value, 1, "west_que", -.32f, -.28f);
            Que(value, 1, "east_que", .32f, -.28f);
            Add(value, 1, "gate_beam", "box", Timber, V(0, .24f, -.28f),
                V(.44f, .06f, .06f));

            Add(value, 2, "platform", "box", Stone, V(0, .06f, .03f),
                V(.90f, .12f, .86f));
            Add(value, 2, "main_mass", "box", Vermilion,
                V(0, .32f, .18f), V(.62f, .40f, .30f));
            Add(value, 2, "main_roof", "hip_roof", Tile,
                V(0, .55f, .18f), V(.74f, .21f, .42f));
            Add(value, 2, "west_que", "box", Earth,
                V(-.32f, .36f, -.28f), V(.18f, .54f, .18f));
            Add(value, 2, "east_que", "box", Earth,
                V(.32f, .36f, -.28f), V(.18f, .54f, .18f));
            Add(value, 2, "west_que_roof", "hip_roof", Tile,
                V(-.32f, .66f, -.28f), V(.25f, .14f, .25f));
            Add(value, 2, "east_que_roof", "hip_roof", Tile,
                V(.32f, .66f, -.28f), V(.25f, .14f, .25f));
            return value;
        }

        private static ModelRecipe BuildYonganPalace()
        {
            var value = new ModelRecipe { PrefabName = "YonganPalace" };
            Add(value, 0, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.90f, .05f, .86f));
            Add(value, 0, "pond", "disc", Water,
                V(-.25f, .055f, -.22f), V(.30f, .018f, .24f));
            Hall(value, 0, "main_hall", V(-.14f, .22f, .18f),
                V(.48f, .31f, .26f), V(.59f, .17f, .37f));
            Hall(value, 0, "east_pavilion", V(.25f, .17f, -.12f),
                V(.24f, .23f, .22f), V(.35f, .15f, .33f),
                V(0, 25f, 0));
            Add(value, 0, "covered_gallery", "box", Timber,
                V(.08f, .15f, .08f), V(.38f, .08f, .08f),
                V(0, -36f, 0));
            Add(value, 0, "gallery_roof", "box", Tile,
                V(.08f, .21f, .08f), V(.42f, .04f, .13f),
                V(0, -36f, 0));
            Add(value, 0, "garden_path", "box", Stone,
                V(.19f, .055f, -.29f), V(.43f, .025f, .10f),
                V(0, 18f, 0));
            Tree(value, 0, "north_tree", .28f, .22f);
            Tree(value, 0, "west_tree", -.35f, .12f);
            Add(value, 0, "east_gate", "box", Vermilion,
                V(.41f, .14f, -.12f), V(.10f, .20f, .24f));
            Add(value, 0, "east_gate_roof", "hip_roof", Tile,
                V(.41f, .27f, -.12f), V(.19f, .12f, .32f));

            Add(value, 1, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.90f, .05f, .86f));
            Add(value, 1, "pond", "disc", Water,
                V(-.25f, .055f, -.22f), V(.30f, .018f, .24f));
            Hall(value, 1, "main_hall", V(-.14f, .22f, .18f),
                V(.48f, .31f, .26f), V(.59f, .17f, .37f));
            Hall(value, 1, "east_pavilion", V(.25f, .17f, -.12f),
                V(.24f, .23f, .22f), V(.35f, .15f, .33f),
                V(0, 25f, 0));
            Tree(value, 1, "north_tree", .28f, .22f);

            Add(value, 2, "garden_ground", "box", Foliage,
                V(0, .025f, 0), V(.90f, .05f, .86f));
            Add(value, 2, "main_mass", "box", Vermilion,
                V(-.14f, .22f, .18f), V(.48f, .31f, .26f));
            Add(value, 2, "main_roof", "hip_roof", Tile,
                V(-.14f, .40f, .18f), V(.59f, .17f, .37f));
            Add(value, 2, "garden_canopy", "canopy", Foliage,
                V(.28f, .34f, .22f), V(.26f, .30f, .26f));
            return value;
        }

        private static ModelRecipe BuildTaixue()
        {
            var value = new ModelRecipe { PrefabName = "Taixue" };
            Add(value, 0, "campus", "box", Earth,
                V(0, .03f, 0), V(.90f, .06f, .86f));
            Hall(value, 0, "lecture_north", V(0, .19f, .25f),
                V(.70f, .24f, .18f), V(.80f, .15f, .28f));
            Hall(value, 0, "study_west", V(-.27f, .16f, -.07f),
                V(.22f, .20f, .48f), V(.31f, .14f, .58f),
                V(0, 90f, 0));
            Hall(value, 0, "study_east", V(.27f, .16f, -.07f),
                V(.22f, .20f, .48f), V(.31f, .14f, .58f),
                V(0, 90f, 0));
            Add(value, 0, "south_gate", "box", Vermilion,
                V(0, .14f, -.39f), V(.28f, .20f, .12f));
            Add(value, 0, "south_gate_roof", "hip_roof", Tile,
                V(0, .27f, -.39f), V(.38f, .13f, .22f));
            Add(value, 0, "central_path", "box", Stone,
                V(0, .065f, -.10f), V(.12f, .025f, .56f));
            Add(value, 0, "stele", "box", Stone,
                V(0, .24f, -.12f), V(.08f, .38f, .055f));
            Add(value, 0, "stele_base", "box", Stone,
                V(0, .075f, -.12f), V(.16f, .07f, .13f));
            AddPosts(value, 0, "lecture", .25f, .15f,
                new[] { -.27f, -.09f, .09f, .27f });

            Add(value, 1, "campus", "box", Earth,
                V(0, .03f, 0), V(.90f, .06f, .86f));
            Hall(value, 1, "lecture_north", V(0, .19f, .25f),
                V(.70f, .24f, .18f), V(.80f, .15f, .28f));
            Hall(value, 1, "study_west", V(-.27f, .16f, -.07f),
                V(.22f, .20f, .48f), V(.31f, .14f, .58f),
                V(0, 90f, 0));
            Hall(value, 1, "study_east", V(.27f, .16f, -.07f),
                V(.22f, .20f, .48f), V(.31f, .14f, .58f),
                V(0, 90f, 0));
            Add(value, 1, "south_gate", "box", Vermilion,
                V(0, .14f, -.39f), V(.28f, .20f, .12f));
            Add(value, 1, "south_gate_roof", "hip_roof", Tile,
                V(0, .27f, -.39f), V(.38f, .13f, .22f));

            Add(value, 2, "campus", "box", Earth,
                V(0, .03f, 0), V(.90f, .06f, .86f));
            Add(value, 2, "lecture_mass", "box", Vermilion,
                V(0, .19f, .25f), V(.70f, .24f, .18f));
            Add(value, 2, "lecture_roof", "hip_roof", Tile,
                V(0, .33f, .25f), V(.80f, .15f, .28f));
            Add(value, 2, "west_study", "box", Earth,
                V(-.27f, .16f, -.07f), V(.22f, .20f, .48f));
            Add(value, 2, "east_study", "box", Earth,
                V(.27f, .16f, -.07f), V(.22f, .20f, .48f));
            return value;
        }

        private static ModelRecipe BuildBiyong()
        {
            var value = new ModelRecipe { PrefabName = "Biyong" };
            Add(value, 0, "water", "disc", Water,
                V(0, .035f, 0), V(.84f, .025f, .84f));
            Add(value, 0, "ritual_ring", "ritual_ring", Stone,
                V(0, .075f, 0), V(.78f, .18f, .78f));
            Add(value, 0, "central_terrace", "disc", Stone,
                V(0, .105f, 0), V(.42f, .07f, .42f));
            Hall(value, 0, "central_hall", V(0, .29f, 0),
                V(.31f, .28f, .31f), V(.45f, .17f, .45f),
                V(0, 45f, 0));
            Bridge(value, 0, "south_bridge", V(0, .095f, -.33f), 0f);
            Bridge(value, 0, "north_bridge", V(0, .095f, .33f), 0f);
            Bridge(value, 0, "west_bridge", V(-.33f, .095f, 0), 90f);
            Bridge(value, 0, "east_bridge", V(.33f, .095f, 0), 90f);
            AddPosts(value, 0, "central", 0f, .25f,
                new[] { -.12f, .12f });
            Add(value, 0, "roof_finial", "post", Bronze,
                V(0, .58f, 0), V(.035f, .07f, .035f));

            Add(value, 1, "water", "disc", Water,
                V(0, .035f, 0), V(.84f, .025f, .84f));
            Add(value, 1, "ritual_ring", "ritual_ring", Stone,
                V(0, .075f, 0), V(.78f, .18f, .78f));
            Add(value, 1, "central_terrace", "disc", Stone,
                V(0, .105f, 0), V(.42f, .07f, .42f));
            Hall(value, 1, "central_hall", V(0, .29f, 0),
                V(.31f, .28f, .31f), V(.45f, .17f, .45f),
                V(0, 45f, 0));
            Bridge(value, 1, "south_bridge", V(0, .095f, -.33f), 0f);

            Add(value, 2, "ritual_ring", "ritual_ring", Stone,
                V(0, .075f, 0), V(.78f, .18f, .78f));
            Add(value, 2, "central_hall", "box", Vermilion,
                V(0, .29f, 0), V(.31f, .28f, .31f), V(0, 45f, 0));
            Add(value, 2, "central_roof", "hip_roof", Tile,
                V(0, .46f, 0), V(.45f, .17f, .45f), V(0, 45f, 0));
            return value;
        }

        private static void Hall(ModelRecipe recipe, int lod, string name,
            Vector3 bodyPosition, Vector3 bodyScale, Vector3 roofScale,
            Vector3? euler = null)
        {
            var rotation = euler ?? Vector3.zero;
            Add(recipe, lod, name + "_body", "box", Vermilion,
                bodyPosition, bodyScale, rotation);
            var roofPosition = bodyPosition + V(0, bodyScale.y * .58f, 0);
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                roofPosition, roofScale, rotation);
            if (lod == 0)
                RoofDetail(recipe, lod, name, roofPosition, roofScale,
                    rotation);
        }

        private static void Que(ModelRecipe recipe, int lod, string name,
            float x, float z)
        {
            Add(recipe, lod, name + "_base", "box", Stone,
                V(x, .10f, z), V(.24f, .10f, .24f));
            Add(recipe, lod, name + "_body", "box", Earth,
                V(x, .36f, z), V(.18f, .54f, .18f));
            Add(recipe, lod, name + "_belt", "box", Vermilion,
                V(x, .48f, z), V(.21f, .05f, .21f));
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                V(x, .66f, z), V(.25f, .14f, .25f));
            if (lod == 0)
                RoofDetail(recipe, lod, name, V(x, .66f, z),
                    V(.25f, .14f, .25f), Vector3.zero);
        }

        private static void Tree(ModelRecipe recipe, int lod, string name,
            float x, float z)
        {
            Add(recipe, lod, name + "_trunk", "post", Timber,
                V(x, .20f, z), V(.04f, .20f, .04f));
            Add(recipe, lod, name + "_canopy", "canopy", Foliage,
                V(x, .39f, z), V(.25f, .27f, .25f));
        }

        private static void Bridge(ModelRecipe recipe, int lod, string name,
            Vector3 position, float yaw)
        {
            Add(recipe, lod, name + "_deck", "box", Timber, position,
                V(.14f, .04f, .34f), V(0, yaw, 0));
            Add(recipe, lod, name + "_stone_cap", "box", Stone,
                position + V(0, -.035f, 0), V(.18f, .04f, .36f),
                V(0, yaw, 0));
        }

        private static void RoofDetail(ModelRecipe recipe, int lod,
            string name, Vector3 position, Vector3 scale, Vector3 rotation)
        {
            Add(recipe, lod, name + "_eave", "box", Timber,
                position + V(0, -.012f, 0),
                V(scale.x * 1.03f, .025f, scale.z * 1.03f), rotation);
            Add(recipe, lod, name + "_ridge", "box", Bronze,
                position + V(0, scale.y * .52f, 0),
                V(scale.x * .58f, .025f, .028f), rotation);
        }

        private static void AddPosts(ModelRecipe recipe, int lod,
            string name, float z, float y, IEnumerable<float> xs)
        {
            foreach (var x in xs)
                Add(recipe, lod, name + "_post_" + x.ToString("0.000"),
                    "post", Vermilion, V(x, y, z),
                    V(.032f, .12f, .032f));
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
