using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Presentation;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    public static class LuoyangRemainingFinalAssetFbxExporter
    {
        public const string RevisionId =
            "luoyang.remaining-38.fbx-source.v1";
        public const string SourceRoot =
            "Assets/ArtSource/Han/Luoyang/FinalRemaining";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangRemainingFinalAssetsV1/" +
            "luoyang_remaining_final_assets_v1.json";

        public sealed class PieceReport
        {
            public int ReviewOrder;
            public string AssetVariantId;
            public string PrefabPath;
            public string FbxPath;
            public long FbxLength;
        }

        public sealed class ExportReport
        {
            public string Revision;
            public readonly List<PieceReport> Pieces =
                new List<PieceReport>();
        }

        [MenuItem("Mandate/Luoyang/Export Remaining 38 Final FBX V1")]
        public static void ExportFromMenu()
        {
            var report = ExportAll();
            Debug.Log("Luoyang remaining final FBX exported: " +
                      report.Pieces.Count + " files; revision " +
                      report.Revision + ".");
        }

        public static ExportReport ExportAll()
        {
            var catalog = LoadCatalog();
            ValidateCatalog(catalog);
            Directory.CreateDirectory(ToAbsolutePath(SourceRoot));
            var report = new ExportReport { Revision = RevisionId };
            foreach (var profile in catalog.Profiles.OrderBy(
                         item => item.ReviewOrder))
            {
                var prefabPath = "Assets/Resources/" +
                                 profile.ArtistPrefabResourcePath + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) ==
                    null)
                    throw new FileNotFoundException(
                        "Remaining final-asset prefab is missing.",
                        prefabPath);
                var fbxPath = NormalizeAndValidatePath(
                    profile.ArtistFbxTargetPath);
                var absoluteFbxPath = ToAbsolutePath(fbxPath);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                Mesh markerMesh = null;
                Material markerMaterial = null;
                try
                {
                    ValidatePrefab(root, profile);
                    foreach (var group in root.GetComponentsInChildren<
                                 LODGroup>(true))
                        Object.DestroyImmediate(group);
                    AddAnchorMarkers(root, profile, out markerMesh,
                        out markerMaterial);
                    var exported = ModelExporter.ExportObject(absoluteFbxPath,
                        root);
                    if (string.IsNullOrWhiteSpace(exported) ||
                        !File.Exists(absoluteFbxPath))
                        throw new InvalidOperationException(
                            "Unity FBX Exporter did not create " + fbxPath +
                            ".");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    if (markerMesh != null)
                        Object.DestroyImmediate(markerMesh);
                    if (markerMaterial != null)
                        Object.DestroyImmediate(markerMaterial);
                }

                var length = new FileInfo(absoluteFbxPath).Length;
                if (length < 1024)
                    throw new InvalidDataException(
                        "Remaining final-asset FBX is unexpectedly small: " +
                        fbxPath + ".");
                report.Pieces.Add(new PieceReport
                {
                    ReviewOrder = profile.ReviewOrder,
                    AssetVariantId = profile.AssetVariantId,
                    PrefabPath = prefabPath,
                    FbxPath = fbxPath,
                    FbxLength = length
                });
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var piece in report.Pieces)
            {
                var importer = AssetImporter.GetAtPath(piece.FbxPath) as
                    ModelImporter;
                if (importer == null)
                    throw new InvalidDataException(
                        "Unity could not import " + piece.FbxPath + ".");
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.preserveHierarchy = true;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (report.Pieces.Count !=
                LuoyangRemainingFinalAssetIds.ProfileCount)
                throw new InvalidOperationException(
                    "Remaining final-asset FBX exporter did not create 38 sources.");
            return report;
        }

        private static void ValidatePrefab(GameObject root,
            LuoyangRemainingFinalAssetProfile profile)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidDataException(
                    "Remaining final prefab contains a Collider: " +
                    profile.AssetVariantId + ".");
            var metadata = root.GetComponent<
                LuoyangFinalAssetPrefabMetadata>();
            var group = root.GetComponent<LODGroup>();
            if (metadata == null || group == null ||
                metadata.ReviewOrder != profile.ReviewOrder ||
                !string.Equals(metadata.AssetVariantId,
                    profile.AssetVariantId, StringComparison.Ordinal) ||
                !string.Equals(metadata.SourceProfileId,
                    profile.SourceProfileId, StringComparison.Ordinal) ||
                metadata.StableAnchorIds == null ||
                metadata.StableAnchorIds.Length == 0)
                throw new InvalidDataException(
                    "Remaining final prefab metadata is invalid: " +
                    profile.AssetVariantId + ".");
            var lods = group.GetLODs();
            if (lods.Length != 3 || lods.Any(lod => lod.renderers == null ||
                    lod.renderers.Length == 0 || lod.renderers.Any(renderer =>
                        renderer == null || renderer.sharedMaterial == null)))
                throw new InvalidDataException(
                    "Remaining final prefab must have three populated LODs: " +
                    profile.AssetVariantId + ".");
            var names = new HashSet<string>(root
                .GetComponentsInChildren<Transform>(true)
                .Select(item => item.name), StringComparer.Ordinal);
            if (metadata.StableAnchorIds.Any(anchor =>
                    !names.Contains(anchor)))
                throw new InvalidDataException(
                    "Remaining final prefab is missing a stable anchor: " +
                    profile.AssetVariantId + ".");
        }

        private static void AddAnchorMarkers(GameObject root,
            LuoyangRemainingFinalAssetProfile profile, out Mesh mesh,
            out Material material)
        {
            var metadata = root.GetComponent<
                LuoyangFinalAssetPrefabMetadata>();
            mesh = new Mesh { name = "__FINAL38_ANCHOR_MARKER_MESH" };
            mesh.vertices = new[]
            {
                Vector3.zero,
                new Vector3(0.001f, 0f, 0f),
                new Vector3(0f, 0.001f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var shader = Shader.Find("Standard") ??
                         Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException(
                    "No shader is available for the FBX anchor marker.");
            material = new Material(shader)
            {
                name = "__FINAL38_ANCHOR_MARKER"
            };
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var anchorId in metadata.StableAnchorIds)
            {
                var transform = transforms.SingleOrDefault(item =>
                    string.Equals(item.name, anchorId,
                        StringComparison.Ordinal));
                if (transform == null)
                    throw new InvalidDataException(
                        "Remaining final prefab anchor is missing: " +
                        anchorId + " for " + profile.AssetVariantId + ".");
                transform.gameObject.AddComponent<MeshFilter>().sharedMesh =
                    mesh;
                transform.gameObject.AddComponent<MeshRenderer>()
                    .sharedMaterial = material;
            }
        }

        private static LuoyangRemainingFinalAssetCatalog LoadCatalog()
        {
            var path = ToAbsolutePath(CatalogPath);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Remaining final-asset catalog is missing.", path);
            return JsonUtility.FromJson<LuoyangRemainingFinalAssetCatalog>(
                       File.ReadAllText(path)) ??
                   throw new InvalidDataException(
                       "Remaining final-asset catalog could not be parsed.");
        }

        private static void ValidateCatalog(
            LuoyangRemainingFinalAssetCatalog catalog)
        {
            if (catalog.ProfileCount !=
                    LuoyangRemainingFinalAssetIds.ProfileCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangRemainingFinalAssetIds.ProfileCount ||
                !string.Equals(catalog.StatusId,
                    LuoyangRemainingFinalAssetIds.StatusId,
                    StringComparison.Ordinal) ||
                catalog.Profiles.Any(item => !item.ArtistPrefabPresent ||
                    !item.FinalArtApproved))
                throw new InvalidDataException(
                    "Remaining final-asset FBX export requires 38 preaccepted source prefabs.");
        }

        private static string NormalizeAndValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException("FBX path is required.");
            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(SourceRoot + "/",
                    StringComparison.Ordinal) ||
                !normalized.EndsWith(".fbx",
                    StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("../"))
                throw new InvalidDataException(
                    "Remaining final-asset FBX must remain under " +
                    SourceRoot + ".");
            var fullPath = ToAbsolutePath(normalized);
            var fullRoot = ToAbsolutePath(SourceRoot) +
                           Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Remaining final-asset FBX path escaped its source root.");
            return normalized;
        }

        private static string ToAbsolutePath(string projectPath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
