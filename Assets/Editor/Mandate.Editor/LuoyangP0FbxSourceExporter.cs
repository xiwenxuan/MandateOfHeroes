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
    public static class LuoyangP0FbxSourceExporter
    {
        public const string RevisionId =
            "luoyang.p0.accepted-fbx-source-export.v1";
        public const string ExporterPackageId = "com.unity.formats.fbx";
        public const string ExporterPackageVersion = "4.2.1";
        public const string SourceRoot =
            "Assets/ArtSource/Han/Luoyang/P0Final";

        private const string CatalogPath =
            "Assets/StreamingAssets/WorldMap/" +
            "LuoyangP0FinalAssetVerticalSliceV1/" +
            "luoyang_p0_final_asset_vertical_slice_v1.json";

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
            public string PackageId;
            public string PackageVersion;
            public readonly List<PieceReport> Pieces =
                new List<PieceReport>();
        }

        [MenuItem("Mandate/Luoyang/Export P0 Accepted FBX Sources V1")]
        public static void ExportFromMenu()
        {
            var report = ExportAll();
            Debug.Log("Luoyang P0 accepted FBX sources exported: " +
                      report.Pieces.Count + " files via " +
                      report.PackageId + "@" + report.PackageVersion +
                      "; revision " + report.Revision + ".");
        }

        public static ExportReport ExportAll()
        {
            var catalog = LoadCatalog();
            ValidateCatalogGate(catalog);
            Directory.CreateDirectory(ToAbsolutePath(SourceRoot));

            var report = new ExportReport
            {
                Revision = RevisionId,
                PackageId = ExporterPackageId,
                PackageVersion = ExporterPackageVersion
            };
            foreach (var profile in catalog.Profiles.OrderBy(
                         item => item.FacilityId, StringComparer.Ordinal))
            {
                var prefabPath = "Assets/Resources/" +
                                 profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath);
                if (prefab == null)
                    throw new FileNotFoundException(
                        "Accepted P0 prefab is missing.", prefabPath);

                var fbxPath = NormalizeAndValidateFbxPath(
                    profile.ArtistFbxTargetPath);
                var absoluteFbxPath = ToAbsolutePath(fbxPath);
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                Mesh anchorMarkerMesh = null;
                Material anchorMarkerMaterial = null;
                try
                {
                    var lodGroup = prefabRoot.GetComponent<LODGroup>();
                    if (lodGroup == null || lodGroup.GetLODs().Length != 3)
                        throw new InvalidDataException(
                            "Accepted P0 prefab must have exactly three LODs: " +
                            prefabPath + ".");
                    // The package's LOD visitor exports only renderers assigned
                    // to the LODGroup and omits independent empty anchor nodes.
                    // The temporary prefab-content copy retains the explicit
                    // LOD0/LOD1/LOD2 hierarchy, so removing only this Unity
                    // component makes both the LOD geometry and anchors part of
                    // the editable FBX without changing the source prefab.
                    Object.DestroyImmediate(lodGroup);
                    AddExportOnlyAnchorMarkers(prefabRoot, profile,
                        out anchorMarkerMesh, out anchorMarkerMaterial);
                    var exportedPath = ModelExporter.ExportObject(
                        absoluteFbxPath, prefabRoot);
                    if (string.IsNullOrWhiteSpace(exportedPath) ||
                        !File.Exists(absoluteFbxPath))
                        throw new InvalidOperationException(
                            "Unity FBX Exporter did not create " + fbxPath +
                            ".");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    if (anchorMarkerMesh != null)
                        Object.DestroyImmediate(anchorMarkerMesh);
                    if (anchorMarkerMaterial != null)
                        Object.DestroyImmediate(anchorMarkerMaterial);
                }

                var length = new FileInfo(absoluteFbxPath).Length;
                if (length < 1024)
                    throw new InvalidDataException(
                        "Exported FBX is unexpectedly small: " + fbxPath +
                        " (" + length + " bytes)." );
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
                        "Unity could not import exported FBX: " +
                        piece.FbxPath + ".");
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

        private static void AddExportOnlyAnchorMarkers(GameObject root,
            LuoyangP0FinalAssetProfile profile, out Mesh markerMesh,
            out Material markerMaterial)
        {
            markerMesh = new Mesh { name = "__P0_ANCHOR_MARKER_MESH" };
            markerMesh.vertices = new[]
            {
                Vector3.zero,
                new Vector3(0.001f, 0f, 0f),
                new Vector3(0f, 0.001f, 0f)
            };
            markerMesh.triangles = new[] { 0, 1, 2 };
            markerMesh.RecalculateNormals();
            markerMesh.RecalculateBounds();
            var shader = Shader.Find("Standard") ??
                         Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException(
                    "No shader is available for the FBX anchor marker.");
            markerMaterial = new Material(shader)
            {
                name = "__P0_ANCHOR_MARKER"
            };

            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var anchor in profile.Anchors)
            {
                var anchorTransform = transforms.SingleOrDefault(item =>
                    string.Equals(item.name, anchor.AnchorId,
                        StringComparison.Ordinal));
                if (anchorTransform == null)
                    throw new InvalidDataException(
                        "Accepted P0 prefab anchor is missing: " +
                        anchor.AnchorId + ".");
                anchorTransform.gameObject.AddComponent<MeshFilter>()
                    .sharedMesh = markerMesh;
                anchorTransform.gameObject.AddComponent<MeshRenderer>()
                    .sharedMaterial = markerMaterial;
            }
        }

        private static LuoyangP0FinalAssetVerticalSliceCatalog LoadCatalog()
        {
            var absoluteCatalogPath = ToAbsolutePath(CatalogPath);
            if (!File.Exists(absoluteCatalogPath))
                throw new FileNotFoundException(
                    "Luoyang P0 catalog is missing.", absoluteCatalogPath);
            var catalog = JsonUtility.FromJson<
                LuoyangP0FinalAssetVerticalSliceCatalog>(
                File.ReadAllText(absoluteCatalogPath));
            if (catalog == null)
                throw new InvalidDataException(
                    "Luoyang P0 catalog could not be parsed.");
            return catalog;
        }

        private static void ValidateCatalogGate(
            LuoyangP0FinalAssetVerticalSliceCatalog catalog)
        {
            if (catalog.ProfileCount != 4 || catalog.Profiles == null ||
                catalog.Profiles.Count != 4)
                throw new InvalidDataException(
                    "Luoyang P0 FBX export requires exactly four profiles.");
            if (!string.Equals(catalog.UserReviewDecisionStatusId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .UserReviewDecisionStatusId,
                    StringComparison.Ordinal) ||
                !string.Equals(catalog.UserReviewDecisionRecordId,
                    LuoyangP0FinalAssetVerticalSliceIds
                        .UserReviewDecisionRecordId,
                    StringComparison.Ordinal))
                throw new InvalidDataException(
                    "Luoyang P0 FBX export requires the recorded user " +
                    "acceptance decision.");
            if (catalog.Profiles.Any(profile =>
                    !profile.ArtistPrefabPresent))
                throw new InvalidDataException(
                    "All accepted P0 prefabs must exist before FBX export.");
        }

        private static string NormalizeAndValidateFbxPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException(
                    "P0 FBX target path is missing.");
            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(SourceRoot + "/",
                    StringComparison.Ordinal) ||
                !normalized.EndsWith(".fbx",
                    StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("../"))
                throw new InvalidDataException(
                    "P0 FBX target must remain under " + SourceRoot +
                    ": " + path + ".");
            var fullPath = ToAbsolutePath(normalized);
            var fullRoot = ToAbsolutePath(SourceRoot) +
                           Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "P0 FBX target escaped its source root: " + path + ".");
            return normalized;
        }

        private static string ToAbsolutePath(string projectPath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
