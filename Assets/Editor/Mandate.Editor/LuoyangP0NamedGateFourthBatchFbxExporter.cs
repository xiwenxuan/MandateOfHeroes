using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Editor
{
    public static class LuoyangP0NamedGateFourthBatchFbxExporter
    {
        public const string RevisionId =
            "luoyang.p0.named-gate-fourth-batch.fbx-source.v1";
        public const string ExporterPackageId = "com.unity.formats.fbx";
        public const string ExporterPackageVersion = "4.2.1";
        public const string SourceRoot =
            "Assets/ArtSource/Han/Luoyang/P0Batch4";
        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0NamedGateFourthBatchV1/" +
            "luoyang_p0_named_gate_fourth_batch_v1.json";

        public sealed class PieceReport
        {
            public string FacilityId;
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

        [MenuItem("Mandate/Luoyang/Export P0 Named Gate Fourth Batch FBX V1")]
        public static void ExportFromMenu()
        {
            var report = ExportAll();
            Debug.Log("Luoyang P0 named-gate fourth-batch FBX exported: " +
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
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                    throw new FileNotFoundException(
                        "Named-gate fourth-batch prefab is missing.",
                        prefabPath);
                var fbxPath = NormalizeAndValidatePath(
                    profile.ArtistFbxTargetPath);
                var absoluteFbxPath = ToAbsolutePath(fbxPath);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                Mesh markerMesh = null;
                Material markerMaterial = null;
                try
                {
                    var group = root.GetComponent<LODGroup>();
                    if (group == null || group.GetLODs().Length != 3 ||
                        group.GetLODs().Any(item =>
                            item.renderers == null ||
                            item.renderers.Length == 0))
                        throw new InvalidDataException(
                            "Named-gate fourth-batch prefab must have three populated LODs: " +
                            prefabPath + ".");
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
                        "Named-gate fourth-batch FBX is unexpectedly small: " +
                        fbxPath + ".");
                report.Pieces.Add(new PieceReport
                {
                    FacilityId = profile.FacilityId,
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
            return report;
        }

        private static void AddAnchorMarkers(GameObject root,
            LuoyangP0NamedGateFourthBatchProfile profile, out Mesh mesh,
            out Material material)
        {
            mesh = new Mesh { name = "__P0_BATCH4_ANCHOR_MARKER_MESH" };
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
                name = "__P0_BATCH4_ANCHOR_MARKER"
            };
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var anchor in profile.Anchors)
            {
                var transform = transforms.SingleOrDefault(item =>
                    string.Equals(item.name, anchor.AnchorId,
                        StringComparison.Ordinal));
                if (transform == null)
                    throw new InvalidDataException(
                        "Named-gate fourth-batch prefab anchor is missing: " +
                        anchor.AnchorId + ".");
                transform.gameObject.AddComponent<MeshFilter>().sharedMesh =
                    mesh;
                transform.gameObject.AddComponent<MeshRenderer>()
                    .sharedMaterial = material;
            }
        }

        private static LuoyangP0NamedGateFourthBatchCatalog LoadCatalog()
        {
            var path = ToAbsolutePath(CatalogPath);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Named-gate fourth-batch catalog is missing.", path);
            return JsonUtility.FromJson<
                       LuoyangP0NamedGateFourthBatchCatalog>(
                       File.ReadAllText(path)) ??
                   throw new InvalidDataException(
                       "Named-gate fourth-batch catalog could not be parsed.");
        }

        private static void ValidateCatalog(
            LuoyangP0NamedGateFourthBatchCatalog catalog)
        {
            if (catalog.ProfileCount !=
                    LuoyangP0NamedGateFourthBatchIds.ProfileCount ||
                catalog.Profiles == null || catalog.Profiles.Count !=
                    LuoyangP0NamedGateFourthBatchIds.ProfileCount ||
                !string.Equals(catalog.ReviewDecisionStatusId,
                    LuoyangP0NamedGateFourthBatchIds.ReviewDecisionStatusId,
                    StringComparison.Ordinal) ||
                catalog.Profiles.Any(item =>
                    !item.ArtistPrefabPresent || !item.FinalArtApproved))
                throw new InvalidDataException(
                    "Named-gate fourth-batch FBX export requires four accepted source prefabs.");
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
                    "Named-gate fourth-batch FBX must remain under " +
                    SourceRoot + ".");
            var fullPath = ToAbsolutePath(normalized);
            var fullRoot = ToAbsolutePath(SourceRoot) +
                           Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "Named-gate fourth-batch FBX path escaped its source root.");
            return normalized;
        }

        private static string ToAbsolutePath(string projectPath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
