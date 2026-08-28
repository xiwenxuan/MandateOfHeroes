using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0FbxSourceExportV1Tests
    {
        [Test]
        public void ExportAll_CreatesFourReadableSourcesMatchingAcceptedPrefabs()
        {
            var report = LuoyangP0FbxSourceExporter.ExportAll();
            Assert.That(report.Revision, Is.EqualTo(
                LuoyangP0FbxSourceExporter.RevisionId));
            Assert.That(report.PackageId, Is.EqualTo(
                "com.unity.formats.fbx"));
            Assert.That(report.PackageVersion, Is.EqualTo("4.2.1"));
            Assert.That(report.Pieces, Has.Count.EqualTo(4));
            Assert.That(report.Pieces.Select(item => item.FacilityId),
                Is.EquivalentTo(
                    LuoyangP0FinalAssetVerticalSliceIds.FacilityIds));

            var catalog = LoadCatalog();
            Assert.That(catalog.FinalArtApprovalStatusId, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .FinalArtApprovalStatusId));
            Assert.That(catalog.FbxSourceToolchainId, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.FbxSourceToolchainId));
            foreach (var profile in catalog.Profiles)
            {
                Assert.That(profile.FinalArtApproved, Is.True,
                    profile.CandidateId);
                var piece = report.Pieces.Single(item =>
                    item.FacilityId == profile.FacilityId);
                Assert.That(piece.FbxPath,
                    Is.EqualTo(profile.ArtistFbxTargetPath));
                Assert.That(piece.FbxLength, Is.GreaterThan(1024));
                Assert.That(File.Exists(ToAbsolutePath(piece.FbxPath)),
                    Is.True, piece.FbxPath);
                Assert.That(AssetImporter.GetAtPath(piece.FbxPath),
                    Is.TypeOf<ModelImporter>(), piece.FbxPath);

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.PrefabPath);
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.FbxPath);
                Assert.That(source, Is.Not.Null, piece.PrefabPath);
                Assert.That(imported, Is.Not.Null, piece.FbxPath);
                ValidateHierarchy(profile, source, imported, piece.FbxPath);
            }
        }

        private static void ValidateHierarchy(
            LuoyangP0FinalAssetProfile profile, GameObject source,
            GameObject imported, string path)
        {
            var sourceGroup = source.GetComponent<LODGroup>();
            var importedGroup = imported.GetComponent<LODGroup>();
            Assert.That(sourceGroup, Is.Not.Null, path);
            var sourceLods = sourceGroup.GetLODs();
            Assert.That(sourceLods, Has.Length.EqualTo(3), path);
            var importedRendererGroups = importedGroup == null
                ? FindNamedLodRendererGroups(imported, path)
                : importedGroup.GetLODs().Select(lod => lod.renderers)
                    .ToArray();
            Assert.That(importedRendererGroups, Has.Length.EqualTo(3), path);
            for (var index = 0; index < 3; index++)
            {
                Assert.That(importedRendererGroups[index].Length,
                    Is.EqualTo(sourceLods[index].renderers.Length),
                    path + " LOD" + index);
                Assert.That(importedRendererGroups[index].All(renderer =>
                        renderer != null && renderer.sharedMaterial != null),
                    Is.True, path + " LOD" + index);
            }

            Assert.That(imported.GetComponentsInChildren<Collider>(true),
                Is.Empty, path);
            var sourceTransforms = source
                .GetComponentsInChildren<Transform>(true);
            var importedTransforms = imported
                .GetComponentsInChildren<Transform>(true);
            foreach (var anchor in profile.Anchors)
            {
                var sourceAnchor = sourceTransforms.SingleOrDefault(item =>
                    string.Equals(item.name, anchor.AnchorId,
                        StringComparison.Ordinal));
                var importedAnchor = importedTransforms.SingleOrDefault(
                    item => string.Equals(item.name,
                        ToFbxCompatibleName(anchor.AnchorId),
                        StringComparison.Ordinal));
                Assert.That(sourceAnchor, Is.Not.Null, path);
                Assert.That(importedAnchor, Is.Not.Null, path);
                AssertVector(importedAnchor.localPosition,
                    sourceAnchor.localPosition, 0.0001f,
                    path + " " + anchor.AnchorId);
            }

            var sourceInstance = Object.Instantiate(source);
            var importedInstance = Object.Instantiate(imported);
            try
            {
                AssertVector(CalculateLodBounds(importedInstance).size,
                    CalculateLodBounds(sourceInstance).size, 0.001f,
                    path + " bounds");
            }
            finally
            {
                Object.DestroyImmediate(sourceInstance);
                Object.DestroyImmediate(importedInstance);
            }
        }

        private static Renderer[][] FindNamedLodRendererGroups(
            GameObject imported, string path)
        {
            var transforms = imported.GetComponentsInChildren<Transform>(true);
            var result = new Renderer[3][];
            for (var index = 0; index < result.Length; index++)
            {
                var name = "LOD" + index;
                var lodRoot = transforms.SingleOrDefault(item =>
                    string.Equals(item.name, name, StringComparison.Ordinal));
                Assert.That(lodRoot, Is.Not.Null,
                    path + " named " + name + " hierarchy");
                result[index] = lodRoot
                    .GetComponentsInChildren<Renderer>(true);
            }
            return result;
        }

        private static Bounds CalculateLodBounds(GameObject root)
        {
            var group = root.GetComponent<LODGroup>();
            var renderers = group == null
                ? FindNamedLodRendererGroups(root, root.name)
                    .SelectMany(item => item).ToArray()
                : group.GetLODs().SelectMany(item => item.renderers).ToArray();
            Assert.That(renderers, Is.Not.Empty, root.name);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void AssertVector(Vector3 actual, Vector3 expected,
            float tolerance, string message)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance),
                message + " x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance),
                message + " y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(tolerance),
                message + " z");
        }

        private static string ToFbxCompatibleName(string value)
        {
            return value.Replace('.', '_');
        }

        private static LuoyangP0FinalAssetVerticalSliceCatalog LoadCatalog()
        {
            var path = ToAbsolutePath(
                "Assets/StreamingAssets/WorldMap/" +
                "LuoyangP0FinalAssetVerticalSliceV1/" +
                "luoyang_p0_final_asset_vertical_slice_v1.json");
            return JsonUtility.FromJson<
                LuoyangP0FinalAssetVerticalSliceCatalog>(
                File.ReadAllText(path));
        }

        private static string ToAbsolutePath(string projectPath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
