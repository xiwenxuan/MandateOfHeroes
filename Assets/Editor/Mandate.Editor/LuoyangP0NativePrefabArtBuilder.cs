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
    public static class LuoyangP0NativePrefabArtBuilder
    {
        public const string RevisionId =
            "luoyang.p0.native-prefab.visual-refinement.v2";
        public const string AssetRoot =
            "Assets/Resources/Art/Han/Luoyang/P0Final";
        public const string MaterialRoot = AssetRoot + "/Materials";
        public const string MeshRoot = AssetRoot + "/Meshes";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0FinalAssetVerticalSliceV1/" +
            "luoyang_p0_final_asset_vertical_slice_v1.json";

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

        [MenuItem("Mandate/Luoyang/Build P0 Native Prefab Art V2")]
        public static void BuildFromMenu()
        {
            var report = BuildAssets();
            Debug.Log("Luoyang P0 native prefab art built: " +
                      report.PrefabCount + " prefabs, " +
                      report.MaterialCount + " materials, " +
                      report.MeshCount + " meshes, " +
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

            var absoluteCatalogPath = Path.Combine(
                Directory.GetCurrentDirectory(), CatalogPath);
            if (!File.Exists(absoluteCatalogPath))
                throw new FileNotFoundException(
                    "Luoyang P0 catalog is missing.", absoluteCatalogPath);
            var catalog = JsonUtility.FromJson<
                LuoyangP0FinalAssetVerticalSliceCatalog>(
                File.ReadAllText(absoluteCatalogPath));
            if (catalog == null || catalog.Profiles == null ||
                catalog.Profiles.Count != 4)
                throw new InvalidDataException(
                    "Luoyang P0 catalog must contain exactly four profiles.");

            var materials = BuildMaterials(catalog.Materials);
            var meshes = BuildMeshes();
            var recipes = new Dictionary<string, ModelRecipe>(
                StringComparer.Ordinal)
            {
                { LuoyangHistoricalLandmarkKitIds.SouthPalace,
                    BuildSouthPalace() },
                { LuoyangHistoricalLandmarkKitIds.Mingtang,
                    BuildMingtang() },
                { LuoyangGateIdentityKitIds.Guangyangmen,
                    BuildGuangyangmen() },
                { LuoyangGateIdentityKitIds.NorthPalaceSouthGate,
                    BuildNorthPalaceSouthGate() }
            };

            var report = new BuildReport
            {
                MaterialCount = materials.Count,
                MeshCount = meshes.Count
            };
            foreach (var profile in catalog.Profiles.OrderBy(
                         item => item.FacilityId, StringComparer.Ordinal))
            {
                if (!recipes.TryGetValue(profile.FacilityId, out var recipe))
                    throw new InvalidOperationException(
                        "No native art recipe for " + profile.FacilityId + ".");
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

        private static Dictionary<string, Material> BuildMaterials(
            IEnumerable<HanBuildableFacilityMaterialDefinition> definitions)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException(
                    "No supported lit shader is available.");
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                var fileName = ToPascalCase(definition.MaterialId.Split('.').Last());
                var path = MaterialRoot + "/" + fileName + ".mat";
                var candidate = new Material(shader)
                {
                    name = fileName
                };
                var color = new Color(definition.Red, definition.Green,
                    definition.Blue, definition.Alpha);
                if (candidate.HasProperty("_BaseColor"))
                    candidate.SetColor("_BaseColor", color);
                if (candidate.HasProperty("_Color"))
                    candidate.SetColor("_Color", color);
                if (candidate.HasProperty("_Metallic"))
                    candidate.SetFloat("_Metallic", definition.Metallic);
                if (candidate.HasProperty("_Smoothness"))
                    candidate.SetFloat("_Smoothness", definition.Smoothness);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    AssetDatabase.CreateAsset(candidate, path);
                    material = candidate;
                }
                else
                {
                    EditorUtility.CopySerialized(candidate, material);
                    Object.DestroyImmediate(candidate);
                }
                result.Add(definition.MaterialId, material);
            }
            return result;
        }

        private static Dictionary<string, Mesh> BuildMeshes()
        {
            return new Dictionary<string, Mesh>(StringComparer.Ordinal)
            {
                { "box", UpsertMesh("NativeBox", ClonePrimitive(
                    PrimitiveType.Cube, "NativeBox")) },
                { "post", UpsertMesh("NativeOctagonalPost", ClonePrimitive(
                    PrimitiveType.Cylinder, "NativeOctagonalPost")) },
                { "hip_roof", UpsertMesh("NativeHanHipRoof",
                    CreateHipRoofMesh()) },
                { "road_crown", UpsertMesh("NativeRoadCrown",
                    CreateRoadCrownMesh()) }
            };
        }

        private static Mesh ClonePrimitive(PrimitiveType type, string meshName)
        {
            var value = GameObject.CreatePrimitive(type);
            try
            {
                var mesh = Object.Instantiate(
                    value.GetComponent<MeshFilter>().sharedMesh);
                mesh.name = meshName;
                return mesh;
            }
            finally
            {
                Object.DestroyImmediate(value);
            }
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

        private static Mesh CreateHipRoofMesh()
        {
            var mesh = new Mesh { name = "NativeHanHipRoof" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(-0.28f, 0.5f, 0f),
                new Vector3(0.28f, 0.5f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 1, 5, 0, 5, 4,
                1, 2, 5,
                2, 3, 4, 2, 4, 5,
                3, 0, 4,
                0, 3, 2, 0, 2, 1
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRoadCrownMesh()
        {
            var mesh = new Mesh { name = "NativeRoadCrown" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0f, 0.18f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0f, 0.18f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, -0.08f, -0.5f),
                new Vector3(0.5f, -0.08f, -0.5f),
                new Vector3(-0.5f, -0.08f, 0.5f),
                new Vector3(0.5f, -0.08f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2,
                6, 8, 3, 6, 3, 0, 2, 5, 9, 2, 9, 7,
                6, 0, 2, 6, 2, 7, 3, 8, 9, 3, 9, 5,
                6, 7, 9, 6, 9, 8
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildPrefab(LuoyangP0FinalAssetProfile profile,
            ModelRecipe recipe, IReadOnlyDictionary<string, Material> materials,
            IReadOnlyDictionary<string, Mesh> meshes)
        {
            var root = new GameObject(recipe.PrefabName);
            try
            {
                foreach (var anchor in profile.Anchors)
                {
                    var anchorObject = new GameObject(anchor.AnchorId);
                    anchorObject.transform.SetParent(root.transform, false);
                    anchorObject.transform.localPosition = new Vector3(anchor.X,
                        anchor.Y, anchor.Z);
                }

                var lods = new LOD[3];
                var transitions = new[] { 0.48f, 0.20f, 0.02f };
                for (var level = 0; level < 3; level++)
                {
                    var lodRoot = new GameObject("LOD" + level);
                    lodRoot.transform.SetParent(root.transform, false);
                    var renderers = new List<Renderer>();
                    foreach (var piece in recipe.Lods[level])
                        renderers.Add(BuildPiece(piece, lodRoot.transform,
                            materials, meshes));
                    lods[level] = new LOD(transitions[level],
                        renderers.ToArray());
                }
                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(lods);
                group.RecalculateBounds();

                var prefabPath = "Assets/Resources/" +
                                 profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath,
                    out var success);
                if (!success || prefab == null)
                    throw new InvalidOperationException(
                        "Could not save native prefab " + prefabPath + ".");
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
            if (!materials.TryGetValue(piece.MaterialId, out var material))
                throw new InvalidOperationException(
                    "Unknown P0 material " + piece.MaterialId + ".");
            if (!meshes.TryGetValue(piece.MeshId, out var mesh))
                throw new InvalidOperationException(
                    "Unknown native mesh " + piece.MeshId + ".");
            var value = new GameObject(piece.Name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = piece.Position;
            value.transform.localEulerAngles = piece.Euler;
            value.transform.localScale = piece.Scale;
            value.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = value.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static ModelRecipe BuildSouthPalace()
        {
            var recipe = new ModelRecipe { PrefabName = "SouthPalace" };
            Add(recipe, 0, "terrace", "box", Stone, V(0, .035f, 0),
                V(.92f, .07f, .88f));
            Add(recipe, 0, "stair", "box", Stone, V(0, .065f, -.48f),
                V(.26f, .08f, .18f));
            Hall(recipe, 0, "front_hall", V(0, .18f, -.22f),
                V(.58f, .22f, .24f), V(.70f, .14f, .36f));
            Hall(recipe, 0, "rear_hall", V(0, .23f, .24f),
                V(.66f, .32f, .27f), V(.78f, .16f, .39f));
            Add(recipe, 0, "west_gallery", "box", Timber,
                V(-.39f, .15f, .01f), V(.06f, .20f, .58f));
            Add(recipe, 0, "east_gallery", "box", Timber,
                V(.39f, .15f, .01f), V(.06f, .20f, .58f));
            Add(recipe, 0, "west_gallery_roof", "box", Tile,
                V(-.39f, .27f, .01f), V(.12f, .045f, .64f));
            Add(recipe, 0, "east_gallery_roof", "box", Tile,
                V(.39f, .27f, .01f), V(.12f, .045f, .64f));
            Add(recipe, 0, "central_court_paving", "box", Stone,
                V(0, .082f, .015f), V(.54f, .025f, .22f));
            Add(recipe, 0, "court_gate_pier_west", "box", Vermilion,
                V(-.11f, .16f, -.44f), V(.075f, .19f, .075f));
            Add(recipe, 0, "court_gate_pier_east", "box", Vermilion,
                V(.11f, .16f, -.44f), V(.075f, .19f, .075f));
            Add(recipe, 0, "court_gate_lintel", "box", Timber,
                V(0, .265f, -.44f), V(.31f, .055f, .075f));
            Add(recipe, 0, "court_gate_roof", "hip_roof", Tile,
                V(0, .295f, -.44f), V(.38f, .11f, .18f));
            AddRoofDetails(recipe, 0, "court_gate", V(0, .295f, -.44f),
                V(.38f, .11f, .18f));
            AddPosts(recipe, 0, "front", -.35f, .13f,
                new[] { -.24f, -.08f, .08f, .24f });
            AddPosts(recipe, 0, "rear", .075f, .19f,
                new[] { -.28f, -.095f, .095f, .28f });
            Add(recipe, 0, "bronze_finial", "post", Bronze,
                V(0, .54f, .24f), V(.035f, .07f, .035f));

            Add(recipe, 1, "terrace", "box", Stone, V(0, .035f, 0),
                V(.92f, .07f, .88f));
            Hall(recipe, 1, "front_hall", V(0, .18f, -.22f),
                V(.58f, .22f, .24f), V(.70f, .14f, .36f));
            Hall(recipe, 1, "rear_hall", V(0, .23f, .24f),
                V(.66f, .32f, .27f), V(.78f, .16f, .39f));
            Add(recipe, 1, "west_gallery", "box", Timber,
                V(-.39f, .15f, .01f), V(.08f, .20f, .60f));
            Add(recipe, 1, "east_gallery", "box", Timber,
                V(.39f, .15f, .01f), V(.08f, .20f, .60f));
            Add(recipe, 1, "finial", "post", Bronze,
                V(0, .54f, .24f), V(.04f, .07f, .04f));

            Add(recipe, 2, "terrace", "box", Stone, V(0, .035f, 0),
                V(.92f, .07f, .88f));
            Add(recipe, 2, "front_mass", "box", Earth,
                V(0, .18f, -.22f), V(.58f, .22f, .24f));
            Add(recipe, 2, "front_roof", "hip_roof", Tile,
                V(0, .29f, -.22f), V(.70f, .20f, .36f));
            Add(recipe, 2, "rear_mass", "box", Vermilion,
                V(0, .23f, .24f), V(.66f, .32f, .27f));
            Add(recipe, 2, "rear_roof", "hip_roof", Tile,
                V(0, .41f, .24f), V(.78f, .22f, .39f));
            return recipe;
        }

        private static ModelRecipe BuildMingtang()
        {
            var recipe = new ModelRecipe { PrefabName = "Mingtang" };
            Add(recipe, 0, "altar_lower", "box", Stone,
                V(0, .04f, 0), V(.88f, .08f, .88f));
            Add(recipe, 0, "altar_middle", "box", Earth,
                V(0, .115f, 0), V(.68f, .09f, .68f));
            Add(recipe, 0, "altar_upper", "box", Vermilion,
                V(0, .19f, 0), V(.50f, .08f, .50f));
            Add(recipe, 0, "hall", "box", Vermilion,
                V(0, .30f, 0), V(.37f, .18f, .37f));
            Add(recipe, 0, "lower_canopy", "hip_roof", Tile,
                V(0, .39f, 0), V(.54f, .15f, .54f));
            AddRoofDetails(recipe, 0, "lower_canopy", V(0, .39f, 0),
                V(.54f, .15f, .54f));
            Add(recipe, 0, "upper_drum", "box", Vermilion,
                V(0, .46f, 0), V(.24f, .12f, .24f));
            Add(recipe, 0, "upper_roof", "hip_roof", Tile,
                V(0, .52f, 0), V(.40f, .15f, .40f));
            AddRoofDetails(recipe, 0, "upper_roof", V(0, .52f, 0),
                V(.40f, .15f, .40f));
            Add(recipe, 0, "south_stair", "box", Stone,
                V(0, .085f, -.43f), V(.24f, .07f, .20f));
            Add(recipe, 0, "north_stair", "box", Stone,
                V(0, .085f, .43f), V(.24f, .07f, .20f));
            Add(recipe, 0, "west_stair", "box", Stone,
                V(-.43f, .085f, 0), V(.20f, .07f, .24f));
            Add(recipe, 0, "east_stair", "box", Stone,
                V(.43f, .085f, 0), V(.20f, .07f, .24f));
            Add(recipe, 0, "south_axial_path", "box", Stone,
                V(0, .045f, -.58f), V(.18f, .025f, .32f));
            AddCornerPosts(recipe, 0, .17f, .29f, .17f);
            Add(recipe, 0, "roof_finial", "post", Bronze,
                V(0, .66f, 0), V(.04f, .06f, .04f));
            AddRails(recipe, 0);

            Add(recipe, 1, "altar_lower", "box", Stone,
                V(0, .04f, 0), V(.88f, .08f, .88f));
            Add(recipe, 1, "altar_middle", "box", Earth,
                V(0, .115f, 0), V(.68f, .09f, .68f));
            Add(recipe, 1, "altar_upper", "box", Vermilion,
                V(0, .19f, 0), V(.50f, .08f, .50f));
            Add(recipe, 1, "hall", "box", Vermilion,
                V(0, .31f, 0), V(.37f, .20f, .37f));
            Add(recipe, 1, "roof", "hip_roof", Tile,
                V(0, .43f, 0), V(.52f, .22f, .52f));
            Add(recipe, 1, "south_stair", "box", Stone,
                V(0, .085f, -.43f), V(.24f, .07f, .20f));
            Add(recipe, 1, "finial", "post", Bronze,
                V(0, .57f, 0), V(.04f, .08f, .04f));

            Add(recipe, 2, "altar", "box", Stone,
                V(0, .085f, 0), V(.82f, .17f, .82f));
            Add(recipe, 2, "hall", "box", Vermilion,
                V(0, .29f, 0), V(.38f, .24f, .38f));
            Add(recipe, 2, "roof", "hip_roof", Tile,
                V(0, .43f, 0), V(.52f, .22f, .52f));
            Add(recipe, 2, "finial", "post", Bronze,
                V(0, .57f, 0), V(.04f, .08f, .04f));
            return recipe;
        }

        private static ModelRecipe BuildGuangyangmen()
        {
            var recipe = new ModelRecipe { PrefabName = "Guangyangmen" };
            GateRoad(recipe, 0);
            Add(recipe, 0, "west_wall", "box", Earth,
                V(-.38f, .15f, .06f), V(.20f, .26f, .72f));
            Add(recipe, 0, "east_wall", "box", Earth,
                V(.38f, .15f, .06f), V(.20f, .26f, .72f));
            Add(recipe, 0, "west_coping", "box", Tile,
                V(-.38f, .295f, .06f), V(.24f, .05f, .76f));
            Add(recipe, 0, "east_coping", "box", Tile,
                V(.38f, .295f, .06f), V(.24f, .05f, .76f));
            Gatehouse(recipe, 0, false);
            Add(recipe, 0, "barbican_west", "box", Earth,
                V(-.22f, .11f, -.31f), V(.28f, .18f, .22f));
            Add(recipe, 0, "barbican_east", "box", Earth,
                V(.22f, .11f, -.31f), V(.28f, .18f, .22f));
            Add(recipe, 0, "barbican_west_roof", "box", Tile,
                V(-.22f, .215f, -.31f), V(.31f, .035f, .25f));
            Add(recipe, 0, "barbican_east_roof", "box", Tile,
                V(.22f, .215f, -.31f), V(.31f, .035f, .25f));
            Add(recipe, 0, "barbican_tower_west", "box", Earth,
                V(-.35f, .25f, -.31f), V(.16f, .24f, .16f));
            Add(recipe, 0, "barbican_tower_east", "box", Earth,
                V(.35f, .25f, -.31f), V(.16f, .24f, .16f));
            Add(recipe, 0, "barbican_tower_west_roof", "hip_roof", Tile,
                V(-.35f, .38f, -.31f), V(.23f, .14f, .23f));
            Add(recipe, 0, "barbican_tower_east_roof", "hip_roof", Tile,
                V(.35f, .38f, -.31f), V(.23f, .14f, .23f));
            AddRoofDetails(recipe, 0, "barbican_tower_west",
                V(-.35f, .38f, -.31f), V(.23f, .14f, .23f));
            AddRoofDetails(recipe, 0, "barbican_tower_east",
                V(.35f, .38f, -.31f), V(.23f, .14f, .23f));
            Add(recipe, 0, "gate_leaf_west", "box", Vermilion,
                V(-.055f, .19f, -.151f), V(.085f, .22f, .025f));
            Add(recipe, 0, "gate_leaf_east", "box", Vermilion,
                V(.055f, .19f, -.151f), V(.085f, .22f, .025f));
            Add(recipe, 0, "barbican_crossbeam", "box", Timber,
                V(0, .25f, -.42f), V(.50f, .05f, .05f));
            AddGatePosts(recipe, 0);
            Add(recipe, 0, "bronze_marker", "post", Bronze,
                V(0, .53f, .04f), V(.03f, .07f, .03f));

            GateRoad(recipe, 1);
            Add(recipe, 1, "west_wall", "box", Earth,
                V(-.38f, .15f, .06f), V(.20f, .26f, .72f));
            Add(recipe, 1, "east_wall", "box", Earth,
                V(.38f, .15f, .06f), V(.20f, .26f, .72f));
            Gatehouse(recipe, 1, false);
            Add(recipe, 1, "barbican_west", "box", Earth,
                V(-.22f, .11f, -.31f), V(.28f, .18f, .22f));
            Add(recipe, 1, "barbican_east", "box", Earth,
                V(.22f, .11f, -.31f), V(.28f, .18f, .22f));

            GateRoad(recipe, 2);
            Add(recipe, 2, "west_wall", "box", Earth,
                V(-.38f, .15f, .06f), V(.20f, .26f, .72f));
            Add(recipe, 2, "east_wall", "box", Earth,
                V(.38f, .15f, .06f), V(.20f, .26f, .72f));
            Add(recipe, 2, "gate_mass", "box", Vermilion,
                V(0, .24f, .04f), V(.42f, .30f, .28f));
            Add(recipe, 2, "gate_roof", "hip_roof", Tile,
                V(0, .41f, .04f), V(.58f, .22f, .43f));
            return recipe;
        }

        private static ModelRecipe BuildNorthPalaceSouthGate()
        {
            var recipe = new ModelRecipe
                { PrefabName = "NorthPalaceSouthGate" };
            GateRoad(recipe, 0);
            Gatehouse(recipe, 0, true);
            QueTower(recipe, 0, "west_que", -.39f);
            QueTower(recipe, 0, "east_que", .39f);
            AddGatePosts(recipe, 0);
            Add(recipe, 0, "west_screen", "box", Earth,
                V(-.40f, .13f, .25f), V(.18f, .20f, .28f));
            Add(recipe, 0, "east_screen", "box", Earth,
                V(.40f, .13f, .25f), V(.18f, .20f, .28f));
            Add(recipe, 0, "gate_leaf_west", "box", Vermilion,
                V(-.060f, .20f, -.151f), V(.09f, .24f, .025f));
            Add(recipe, 0, "gate_leaf_east", "box", Vermilion,
                V(.060f, .20f, -.151f), V(.09f, .24f, .025f));
            Add(recipe, 0, "ceremonial_stair", "box", Stone,
                V(0, .055f, -.30f), V(.24f, .07f, .22f));
            Add(recipe, 0, "west_banner_post", "post", Vermilion,
                V(-.24f, .31f, -.24f), V(.025f, .20f, .025f));
            Add(recipe, 0, "east_banner_post", "post", Vermilion,
                V(.24f, .31f, -.24f), V(.025f, .20f, .025f));
            Add(recipe, 0, "west_banner_cap", "post", Bronze,
                V(-.24f, .53f, -.24f), V(.04f, .035f, .04f));
            Add(recipe, 0, "east_banner_cap", "post", Bronze,
                V(.24f, .53f, -.24f), V(.04f, .035f, .04f));
            Add(recipe, 0, "bronze_finial", "post", Bronze,
                V(0, .55f, 0), V(.035f, .08f, .035f));

            GateRoad(recipe, 1);
            Gatehouse(recipe, 1, true);
            QueTower(recipe, 1, "west_que", -.39f);
            QueTower(recipe, 1, "east_que", .39f);

            GateRoad(recipe, 2);
            Add(recipe, 2, "gate_mass", "box", Vermilion,
                V(0, .24f, 0), V(.46f, .34f, .29f));
            Add(recipe, 2, "gate_roof", "hip_roof", Tile,
                V(0, .43f, 0), V(.62f, .23f, .44f));
            Add(recipe, 2, "west_que", "box", Earth,
                V(-.39f, .25f, -.03f), V(.22f, .44f, .24f));
            Add(recipe, 2, "east_que", "box", Earth,
                V(.39f, .25f, -.03f), V(.22f, .44f, .24f));
            Add(recipe, 2, "west_que_roof", "hip_roof", Tile,
                V(-.39f, .50f, -.03f), V(.29f, .18f, .34f));
            Add(recipe, 2, "east_que_roof", "hip_roof", Tile,
                V(.39f, .50f, -.03f), V(.29f, .18f, .34f));
            return recipe;
        }

        private static void Hall(ModelRecipe recipe, int lod, string name,
            Vector3 bodyPosition, Vector3 bodyScale, Vector3 roofScale)
        {
            Add(recipe, lod, name + "_body", "box", Vermilion,
                bodyPosition, bodyScale);
            var roofPosition = bodyPosition + V(0, bodyScale.y * .58f, 0);
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                roofPosition, roofScale);
            if (lod == 0)
                AddRoofDetails(recipe, lod, name, roofPosition, roofScale);
        }

        private static void GateRoad(ModelRecipe recipe, int lod)
        {
            Add(recipe, lod, "road", "road_crown", Stone,
                V(0, .025f, 0), V(.22f, .10f, .92f));
        }

        private static void Gatehouse(ModelRecipe recipe, int lod,
            bool larger)
        {
            var offset = larger ? .14f : .13f;
            var height = larger ? .34f : .30f;
            Add(recipe, lod, "gate_pier_west", "box", Vermilion,
                V(-offset, .23f, 0), V(.15f, height, .28f));
            Add(recipe, lod, "gate_pier_east", "box", Vermilion,
                V(offset, .23f, 0), V(.15f, height, .28f));
            Add(recipe, lod, "gate_lintel", "box", Timber,
                V(0, .34f, -.15f), V(.40f, .07f, .07f));
            var roofPosition = V(0, larger ? .43f : .41f, 0);
            var roofScale = V(larger ? .62f : .58f, .23f, .43f);
            Add(recipe, lod, "gate_roof", "hip_roof", Tile,
                roofPosition, roofScale);
            if (lod == 0)
                AddRoofDetails(recipe, lod, "gatehouse", roofPosition,
                    roofScale);
        }

        private static void QueTower(ModelRecipe recipe, int lod, string name,
            float x)
        {
            Add(recipe, lod, name + "_base", "box", Stone,
                V(x, .06f, -.03f), V(.26f, .10f, .29f));
            Add(recipe, lod, name + "_body", "box", Earth,
                V(x, .27f, -.03f), V(.21f, .36f, .23f));
            Add(recipe, lod, name + "_belt", "box", Vermilion,
                V(x, .37f, -.03f), V(.23f, .055f, .25f));
            Add(recipe, lod, name + "_roof", "hip_roof", Tile,
                V(x, .49f, -.03f), V(.30f, .19f, .35f));
            if (lod == 0)
                AddRoofDetails(recipe, lod, name, V(x, .49f, -.03f),
                    V(.30f, .19f, .35f));
        }

        private static void AddRoofDetails(ModelRecipe recipe, int lod,
            string prefix, Vector3 roofPosition, Vector3 roofScale)
        {
            Add(recipe, lod, prefix + "_eave_belt", "box", Timber,
                roofPosition + V(0, -.012f, 0),
                V(roofScale.x * 1.03f, .025f, roofScale.z * 1.03f));
            var ridgeY = roofPosition.y + roofScale.y * .52f;
            Add(recipe, lod, prefix + "_ridge", "box", Bronze,
                V(roofPosition.x, ridgeY, roofPosition.z),
                V(roofScale.x * .58f, .025f, .028f));
            var ridgeOffset = roofScale.x * .31f;
            Add(recipe, lod, prefix + "_ridge_cap_west", "post", Bronze,
                V(roofPosition.x - ridgeOffset, ridgeY + .015f,
                    roofPosition.z), V(.026f, .035f, .026f));
            Add(recipe, lod, prefix + "_ridge_cap_east", "post", Bronze,
                V(roofPosition.x + ridgeOffset, ridgeY + .015f,
                    roofPosition.z), V(.026f, .035f, .026f));
        }

        private static void AddPosts(ModelRecipe recipe, int lod,
            string prefix, float z, float y, IEnumerable<float> xs)
        {
            foreach (var x in xs)
                Add(recipe, lod, prefix + "_post_" + x.ToString("0.000"),
                    "post", Vermilion, V(x, y, z),
                    V(.032f, .12f, .032f));
        }

        private static void AddCornerPosts(ModelRecipe recipe, int lod,
            float x, float y, float z)
        {
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
                Add(recipe, lod, "corner_post_" + sx + "_" + sz, "post",
                    Vermilion, V(sx * x, y, sz * z),
                    V(.035f, .12f, .035f));
        }

        private static void AddRails(ModelRecipe recipe, int lod)
        {
            Add(recipe, lod, "rail_south", "box", Timber,
                V(0, .255f, -.23f), V(.44f, .035f, .035f));
            Add(recipe, lod, "rail_north", "box", Timber,
                V(0, .255f, .23f), V(.44f, .035f, .035f));
            Add(recipe, lod, "rail_west", "box", Timber,
                V(-.23f, .255f, 0), V(.035f, .035f, .44f));
            Add(recipe, lod, "rail_east", "box", Timber,
                V(.23f, .255f, 0), V(.035f, .035f, .44f));
        }

        private static void AddGatePosts(ModelRecipe recipe, int lod)
        {
            Add(recipe, lod, "gate_post_west", "post", Timber,
                V(-.10f, .22f, -.16f), V(.035f, .15f, .035f));
            Add(recipe, lod, "gate_post_east", "post", Timber,
                V(.10f, .22f, -.16f), V(.035f, .15f, .035f));
        }

        private static void Add(ModelRecipe recipe, int lod, string name,
            string mesh, string material, Vector3 position, Vector3 scale)
        {
            recipe.Lods[lod].Add(new Piece
            {
                Name = name,
                MeshId = mesh,
                MaterialId = material,
                Position = position,
                Scale = scale,
                Euler = Vector3.zero
            });
        }

        private static Vector3 V(float x, float y, float z) =>
            new Vector3(x, y, z);

        private static string ToPascalCase(string value)
        {
            return string.Concat(value.Split(new[] { '_', '-', ' ' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(item => char.ToUpperInvariant(item[0]) +
                                item.Substring(1)));
        }

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
    }
}
