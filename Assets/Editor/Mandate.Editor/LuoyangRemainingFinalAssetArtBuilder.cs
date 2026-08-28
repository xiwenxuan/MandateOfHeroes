using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    public static class LuoyangRemainingFinalAssetArtBuilder
    {
        public const string RevisionId =
            "luoyang.remaining-38.unity-native-prefabs.v1";
        public const string ResourceRoot =
            "Assets/Resources/Art/Han/Luoyang/FinalRemaining";
        private const string MaterialsRoot = ResourceRoot + "/Materials";
        private const string MeshesRoot = ResourceRoot + "/Meshes";

        public sealed class BuildReport
        {
            public string Revision;
            public int PrefabCount;
            public int MaterialCount;
            public int MeshCount;
            public int Lod0RendererCount;
            public int Lod1RendererCount;
            public int Lod2RendererCount;
        }

        private sealed class Fixture
        {
            public LuoyangFacilityModelCoverageSource Coverage;
            public LuoyangProductionBuildingKitCatalog Production;
            public LuoyangHistoricalLandmarkKitCatalog Landmarks;
            public LuoyangGateIdentityKitCatalog Gates;
            public LuoyangMediumFrequencyUrbanFabricKitCatalog Fabric;
            public LuoyangInfrastructureProductionKitCatalog Infrastructure;
            public LuoyangLowFrequencyDefenseProductionKitCatalog Defense;
            public LuoyangResourceAgricultureProductionKitCatalog Resources;
            public LuoyangFinalCivicRitualMedicalProductionKitCatalog
                FinalCivic;
            public LuoyangFinalAssetReviewManifestSource Review;
            public LuoyangRemainingFinalAssetSource Remaining;
        }

        [MenuItem("Mandate/Luoyang/Build Remaining 38 Final Prefabs V1")]
        public static void BuildFromMenu()
        {
            var report = BuildAssets();
            Debug.Log("Luoyang remaining final prefabs built: " +
                      report.PrefabCount + " prefabs, " +
                      report.MaterialCount + " materials, " +
                      report.MeshCount + " meshes; revision " +
                      report.Revision + ".");
        }

        public static BuildReport BuildAssets()
        {
            EnsureFolder(ResourceRoot);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(MeshesRoot);
            var fixture = LoadFixture();
            var report = new BuildReport { Revision = RevisionId };
            var persistedMaterials = new Dictionary<Material, Material>();
            var persistedMeshes = new Dictionary<Mesh, Mesh>();
            var materialIndex = 0;
            var meshIndex = 0;
            var container = new GameObject(
                "Luoyang Remaining Final Asset Build Root");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic);
            try
            {
                foreach (var profile in fixture.Remaining.Catalog.Profiles
                             .OrderBy(item => item.ReviewOrder))
                {
                    var instance = factory.Create(profile.ModelId,
                        container.transform, profile.RepresentativeFacilityId,
                        profile.RepresentativeCellId64, true);
                    if (!string.Equals(instance.AssetId,
                            profile.AssetVariantId, StringComparison.Ordinal))
                        throw new InvalidDataException(
                            "Runtime source asset does not match remaining slot R" +
                            profile.ReviewOrder + ".");
                    var prefabRoot = instance.gameObject;
                    PreparePrefabRoot(instance, profile);
                    PersistRendererAssets(prefabRoot,
                        persistedMaterials, persistedMeshes,
                        ref materialIndex, ref meshIndex);
                    ValidatePrefabRoot(prefabRoot, profile, report);
                    var prefabPath = "Assets/Resources/" +
                                     profile.ArtistPrefabResourcePath +
                                     ".prefab";
                    var prefab = PrefabUtility.SaveAsPrefabAsset(
                        prefabRoot, prefabPath);
                    if (prefab == null)
                        throw new InvalidOperationException(
                            "Unity did not save remaining final prefab " +
                            prefabPath + ".");
                    AssetDatabase.ImportAsset(prefabPath,
                        ImportAssetOptions.ForceSynchronousImport |
                        ImportAssetOptions.ForceUpdate);
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) ==
                        null)
                        throw new InvalidOperationException(
                            "Unity did not synchronously import remaining final prefab " +
                            prefabPath + ".");
                    report.PrefabCount++;
                    Object.DestroyImmediate(prefabRoot);
                }
            }
            finally
            {
                factory.Dispose();
                Object.DestroyImmediate(container);
            }

            report.MaterialCount = persistedMaterials.Count;
            report.MeshCount = persistedMeshes.Count;
            if (report.PrefabCount !=
                LuoyangRemainingFinalAssetIds.ProfileCount)
                throw new InvalidOperationException(
                    "Remaining final-asset builder did not create 38 prefabs.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        private static void PreparePrefabRoot(
            HanBuildableFacilityModelInstance instance,
            LuoyangRemainingFinalAssetProfile profile)
        {
            var anchors = new[]
                {
                    instance.PlacementAnchorId, instance.EntranceAnchorId,
                    instance.OuterPassageAnchorId,
                    instance.InnerPassageAnchorId
                }
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal).ToArray();
            var names = new HashSet<string>(instance
                .GetComponentsInChildren<Transform>(true)
                .Select(item => item.name), StringComparer.Ordinal);
            if (anchors.Length == 0 || anchors.Any(item => !names.Contains(item)))
                throw new InvalidDataException(
                    "Remaining final-asset source has missing stable anchors: " +
                    profile.AssetVariantId + ".");
            var root = instance.gameObject;
            root.name = "R" + profile.ReviewOrder.ToString("D2") + "." +
                        profile.AssetVariantId;
            var metadata = root.AddComponent<
                LuoyangFinalAssetPrefabMetadata>();
            metadata.AssetVariantId = profile.AssetVariantId;
            metadata.SourceProfileId = profile.SourceProfileId;
            metadata.ReviewOrder = profile.ReviewOrder;
            metadata.StableAnchorIds = anchors;
            Object.DestroyImmediate(instance);
        }

        private static void PersistRendererAssets(GameObject root,
            IDictionary<Material, Material> materials,
            IDictionary<Mesh, Mesh> meshes, ref int materialIndex,
            ref int meshIndex)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(
                         true))
            {
                var source = filter.sharedMesh;
                if (source == null)
                    throw new InvalidDataException(
                        "Remaining final prefab contains a missing mesh.");
                if (!meshes.TryGetValue(source, out var persisted))
                {
                    var path = MeshesRoot + "/M" +
                               meshIndex.ToString("D2") + "_" +
                               SafeName(source.name) + ".asset";
                    persisted = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (persisted == null)
                    {
                        persisted = Object.Instantiate(source);
                        persisted.name = Path.GetFileNameWithoutExtension(path);
                        AssetDatabase.CreateAsset(persisted, path);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(source, persisted);
                        persisted.name = Path.GetFileNameWithoutExtension(path);
                        EditorUtility.SetDirty(persisted);
                    }
                    meshes.Add(source, persisted);
                    meshIndex++;
                }
                filter.sharedMesh = persisted;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(
                         true))
            {
                var values = renderer.sharedMaterials;
                for (var index = 0; index < values.Length; index++)
                {
                    var source = values[index];
                    if (source == null)
                        throw new InvalidDataException(
                            "Remaining final prefab contains a missing material.");
                    if (!materials.TryGetValue(source, out var persisted))
                    {
                        var path = MaterialsRoot + "/MAT" +
                                   materialIndex.ToString("D2") + "_" +
                                   SafeName(source.name) + ".mat";
                        persisted = AssetDatabase.LoadAssetAtPath<Material>(
                            path);
                        if (persisted == null)
                        {
                            persisted = Object.Instantiate(source);
                            persisted.name =
                                Path.GetFileNameWithoutExtension(path);
                            AssetDatabase.CreateAsset(persisted, path);
                        }
                        else
                        {
                            EditorUtility.CopySerialized(source, persisted);
                            persisted.name =
                                Path.GetFileNameWithoutExtension(path);
                            EditorUtility.SetDirty(persisted);
                        }
                        materials.Add(source, persisted);
                        materialIndex++;
                    }
                    values[index] = persisted;
                }
                renderer.sharedMaterials = values;
            }
        }

        private static void ValidatePrefabRoot(GameObject root,
            LuoyangRemainingFinalAssetProfile profile, BuildReport report)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidDataException(
                    "Remaining final prefab contains a Collider: " +
                    profile.AssetVariantId + ".");
            var group = root.GetComponent<LODGroup>();
            if (group == null)
                throw new InvalidDataException(
                    "Remaining final prefab has no root LODGroup: " +
                    profile.AssetVariantId + ".");
            var lods = group.GetLODs();
            if (lods.Length != 3 || lods.Any(lod => lod.renderers == null ||
                    lod.renderers.Length == 0 || lod.renderers.Any(renderer =>
                        renderer == null || renderer.sharedMaterial == null)) ||
                lods[0].renderers.Length < lods[1].renderers.Length ||
                lods[1].renderers.Length < lods[2].renderers.Length ||
                lods[0].renderers.Length <= lods[2].renderers.Length)
                throw new InvalidDataException(
                    "Remaining final prefab must have three populated decreasing LODs: " +
                    profile.AssetVariantId + ".");
            report.Lod0RendererCount += lods[0].renderers.Length;
            report.Lod1RendererCount += lods[1].renderers.Length;
            report.Lod2RendererCount += lods[2].renderers.Length;
        }

        private static Fixture LoadFixture()
        {
            var worldMapRoot = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "StreamingAssets", "WorldMap");
            var value = new Fixture
            {
                Coverage = new LuoyangFacilityModelCoverageSource(worldMapRoot)
            };
            value.Production = new LuoyangProductionBuildingKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            value.Landmarks = new LuoyangHistoricalLandmarkKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            value.Gates = new LuoyangGateIdentityKitSource(worldMapRoot,
                value.Coverage.CombinedCatalog).Catalog;
            value.Fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(
                worldMapRoot, value.Coverage.Bindings,
                value.Coverage.CombinedCatalog).Plan;
            value.Infrastructure = new LuoyangInfrastructureProductionKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog,
                performance).Catalog;
            value.Defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog, value.Gates,
                performance).Catalog;
            value.Resources = new LuoyangResourceAgricultureProductionKitSource(
                worldMapRoot, value.Coverage.CombinedCatalog,
                performance).Catalog;
            value.FinalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    worldMapRoot, value.Coverage.CombinedCatalog,
                    value.Landmarks, performance).Catalog;
            value.Review = new LuoyangFinalAssetReviewManifestSource(
                worldMapRoot, value.Production, value.Landmarks, value.Gates,
                value.Fabric, value.Infrastructure, value.Defense,
                value.Resources, value.FinalCivic, performance);
            value.Remaining = new LuoyangRemainingFinalAssetSource(
                worldMapRoot, value.Review.Catalog);
            return value;
        }

        private static string SafeName(string value)
        {
            var chars = (value ?? "unnamed").Select(character =>
                    char.IsLetterOrDigit(character) || character == '_'
                        ? character : '_')
                .ToArray();
            return new string(chars);
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
    }
}
