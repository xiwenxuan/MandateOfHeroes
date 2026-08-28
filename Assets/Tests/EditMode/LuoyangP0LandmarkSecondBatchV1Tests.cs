using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mandate.Domain;
using Mandate.Editor;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0LandmarkSecondBatchV1Tests
    {
        private static string WorldMapRoot => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "StreamingAssets",
            "WorldMap");

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
            public LuoyangP0LandmarkSecondBatchSource Batch;
        }

        [Serializable]
        private sealed class SourceFileRecord
        {
            public string path;
            public long length;
            public string sha256;
        }

        [Serializable]
        private sealed class AnchorMappingRecord
        {
            public string stable_anchor_id;
            public string fbx_node_name;
        }

        [Serializable]
        private sealed class PieceRecord
        {
            public int review_order;
            public string facility_id;
            public string user_review_decision;
            public string fbx_source_path;
            public long fbx_length;
            public string fbx_sha256;
            public string fbx_source_status;
            public string anchor_name_mapping_id;
            public AnchorMappingRecord[] anchor_mappings;
            public bool final_art_approved;
        }

        [Serializable]
        private sealed class SourceManifest
        {
            public int schema_version;
            public string contract_id;
            public string task_id;
            public string status_id;
            public string selection_policy_id;
            public string review_decision_status_id;
            public string user_review_decision_record_id;
            public string user_review_decision_date;
            public string final_art_approval_status_id;
            public string source_archive_status_id;
            public string source_license_id;
            public string fbx_source_toolchain_id;
            public string fbx_toolchain_license_id;
            public string fbx_anchor_name_mapping_id;
            public string user_review_decision;
            public int source_file_count;
            public int toolchain_file_count;
            public int fbx_source_count;
            public int fbx_missing_count;
            public bool user_review_ready;
            public bool final_art_activation_ready;
            public bool final_art_approved;
            public SourceFileRecord[] toolchain_files;
            public SourceFileRecord[] source_files;
            public PieceRecord[] pieces;
        }

        [Test]
        public void Catalog_RecordsAllFourAcceptedAndFinalActivated()
        {
            var fixture = Load();
            Assert.That(fixture.Batch.Catalog.StatusId, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.StatusId));
            Assert.That(fixture.Batch.Catalog.Profiles.Count, Is.EqualTo(4));
            Assert.That(fixture.Batch.Catalog.Profiles.Select(item =>
                    item.ReviewOrder),
                Is.EqualTo(new[] { 1, 2, 3, 5 }));
            Assert.That(fixture.Batch.Plan.ProfilesByFacilityId.Keys,
                Is.EquivalentTo(
                    LuoyangP0LandmarkSecondBatchIds.FacilityIds));
            Assert.That(fixture.Batch.Catalog.ReviewDecisionStatusId,
                Is.EqualTo(LuoyangP0LandmarkSecondBatchIds
                    .ReviewDecisionStatusId));
            Assert.That(fixture.Batch.Catalog.UserReviewDecisionRecordId,
                Is.EqualTo(LuoyangP0LandmarkSecondBatchIds
                    .UserReviewDecisionRecordId));
            Assert.That(fixture.Batch.Catalog.UserReviewDecisionDate,
                Is.EqualTo(LuoyangP0LandmarkSecondBatchIds
                    .UserReviewDecisionDate));
            foreach (var profile in fixture.Batch.Catalog.Profiles)
            {
                Assert.That(profile.ArtistPrefabPresent, Is.True,
                    profile.CandidateId);
                Assert.That(profile.FinalArtApproved, Is.True,
                    profile.CandidateId);
                Assert.That(profile.ReplacementSlotId,
                    Is.EqualTo(profile.AssetVariantId));
                var review = fixture.Review.Catalog.Items.Single(item =>
                    item.RepresentativeFacilityId == profile.FacilityId);
                Assert.That(review.ReviewOrder,
                    Is.EqualTo(profile.ReviewOrder));
                Assert.That(review.PriorityId,
                    Is.EqualTo(LuoyangFinalAssetReviewIds.PriorityP0));
                Assert.That(review.ModelId, Is.EqualTo(profile.ModelId));
                Assert.That(review.ReplacementSlotId,
                    Is.EqualTo(profile.ReplacementSlotId));
                Assert.That(review.RepresentativeCellId64,
                    Is.EqualTo(profile.CellId64));
            }
        }

        [Test]
        public void Runtime_ApprovedCatalogKeepsProceduralFallbackUnapproved()
        {
            var fixture = Load();
            var root = new GameObject("P0 Landmark Batch 2 Fallback Tests");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic, null, _ => null, fixture.Batch.Plan);
            try
            {
                foreach (var profile in fixture.Batch.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId,
                        root.transform, profile.FacilityId, profile.CellId64,
                        true);
                    Assert.That(instance.P0LandmarkSecondBatchReady, Is.True);
                    Assert.That(instance.P0FinalAssetArtistPrefabLoaded,
                        Is.False);
                    Assert.That(instance.P0FinalAssetProceduralFallbackActive,
                        Is.True);
                    Assert.That(instance.P0FinalAssetFinalArtApproved,
                        Is.False);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void BuildAssets_CreatesFourThreeLodSourcePrefabs()
        {
            var report = LuoyangP0LandmarkSecondBatchArtBuilder.BuildAssets();
            Assert.That(report.PrefabCount, Is.EqualTo(4));
            Assert.That(report.SharedMaterialCount, Is.EqualTo(6));
            Assert.That(report.BatchMaterialCount, Is.EqualTo(2));
            Assert.That(report.SharedMeshCount, Is.EqualTo(3));
            Assert.That(report.BatchMeshCount, Is.EqualTo(3));
            Assert.That(report.Lod0RendererCount,
                Is.GreaterThan(report.Lod1RendererCount));
            Assert.That(report.Lod1RendererCount,
                Is.GreaterThan(report.Lod2RendererCount));
            var catalog = Load().Batch.Catalog;
            foreach (var profile in catalog.Profiles)
            {
                var path = "Assets/Resources/" +
                           profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true),
                    Is.Empty, path);
                var group = prefab.GetComponent<LODGroup>();
                Assert.That(group, Is.Not.Null, path);
                var lods = group.GetLODs();
                Assert.That(lods.Length, Is.EqualTo(3), path);
                Assert.That(lods.All(lod => lod.renderers != null &&
                    lod.renderers.Length > 0 && lod.renderers.All(renderer =>
                        renderer != null && renderer.sharedMaterial != null)),
                    Is.True, path);
                var names = prefab.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                foreach (var anchor in profile.Anchors)
                    Assert.That(names, Does.Contain(anchor.AnchorId), path);
            }
        }

        [Test]
        public void ExportAll_CreatesFourReimportableFbxSourcesMatchingPrefabs()
        {
            LuoyangP0LandmarkSecondBatchArtBuilder.BuildAssets();
            var report = LuoyangP0LandmarkSecondBatchFbxExporter.ExportAll();
            Assert.That(report.Revision, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchFbxExporter.RevisionId));
            Assert.That(report.Pieces.Count, Is.EqualTo(4));
            Assert.That(report.Pieces.Select(item => item.FacilityId),
                Is.EquivalentTo(
                    LuoyangP0LandmarkSecondBatchIds.FacilityIds));
            var catalog = Load().Batch.Catalog;
            foreach (var profile in catalog.Profiles)
            {
                var piece = report.Pieces.Single(item =>
                    item.FacilityId == profile.FacilityId);
                Assert.That(piece.FbxLength, Is.GreaterThan(1024));
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

        [Test]
        public void SourceManifest_FreezesFourAcceptedValidatedFbxSources()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var manifestPath = Path.Combine(projectRoot, "Assets",
                "ArtSource", "Han", "Luoyang", "P0Batch2",
                "luoyang_p0_landmark_second_batch_source_manifest_v1.json");
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var manifest = JsonUtility.FromJson<SourceManifest>(
                File.ReadAllText(manifestPath));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.schema_version, Is.EqualTo(1));
            Assert.That(manifest.contract_id, Is.EqualTo(
                "art_source.luoyang.p0-landmark-second-batch.user-accepted-fbx-source-validated-final-activation.v1"));
            Assert.That(manifest.task_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.FinalActivationTaskId));
            Assert.That(manifest.status_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.StatusId));
            Assert.That(manifest.selection_policy_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.SelectionPolicyId));
            Assert.That(manifest.review_decision_status_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.ReviewDecisionStatusId));
            Assert.That(manifest.user_review_decision_record_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.UserReviewDecisionRecordId));
            Assert.That(manifest.user_review_decision_date, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.UserReviewDecisionDate));
            Assert.That(manifest.final_art_approval_status_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.FinalArtApprovalStatusId));
            Assert.That(manifest.source_archive_status_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.SourceArchiveStatusId));
            Assert.That(manifest.source_license_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.SourceLicenseId));
            Assert.That(manifest.fbx_source_toolchain_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.FbxSourceToolchainId));
            Assert.That(manifest.fbx_toolchain_license_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.FbxToolchainLicenseId));
            Assert.That(manifest.fbx_anchor_name_mapping_id, Is.EqualTo(
                LuoyangP0LandmarkSecondBatchIds.FbxAnchorNameMappingId));
            Assert.That(manifest.user_review_decision,
                Is.EqualTo("ACCEPTED_ALL_FOUR"));
            Assert.That(manifest.source_file_count, Is.EqualTo(54));
            Assert.That(manifest.toolchain_file_count, Is.EqualTo(2));
            Assert.That(manifest.fbx_source_count, Is.EqualTo(4));
            Assert.That(manifest.fbx_missing_count, Is.Zero);
            Assert.That(manifest.user_review_ready, Is.True);
            Assert.That(manifest.final_art_activation_ready, Is.True);
            Assert.That(manifest.final_art_approved, Is.True);

            ValidateFiles(projectRoot, manifest.source_files, 54);
            ValidateFiles(projectRoot, manifest.toolchain_files, 2);
            Assert.That(manifest.pieces, Has.Length.EqualTo(4));
            Assert.That(manifest.pieces.Select(item => item.review_order),
                Is.EqualTo(new[] { 1, 2, 3, 5 }));
            Assert.That(manifest.pieces.Select(item => item.facility_id),
                Is.EquivalentTo(
                    LuoyangP0LandmarkSecondBatchIds.FacilityIds));

            var catalog = Load().Batch.Catalog;
            foreach (var profile in catalog.Profiles)
            {
                var piece = manifest.pieces.Single(item =>
                    item.facility_id == profile.FacilityId);
                Assert.That(piece.user_review_decision,
                    Is.EqualTo("ACCEPTED"), piece.facility_id);
                Assert.That(piece.fbx_source_path,
                    Is.EqualTo(profile.ArtistFbxTargetPath));
                Assert.That(piece.fbx_length, Is.GreaterThan(1024));
                Assert.That(piece.fbx_source_status,
                    Is.EqualTo("PRESENT_UNITY_REIMPORT_VALIDATED"));
                Assert.That(piece.anchor_name_mapping_id, Is.EqualTo(
                    LuoyangP0LandmarkSecondBatchIds
                        .FbxAnchorNameMappingId));
                Assert.That(piece.final_art_approved, Is.True);
                var fbxPath = Path.Combine(projectRoot,
                    piece.fbx_source_path.Replace('/',
                        Path.DirectorySeparatorChar));
                Assert.That(Sha256(fbxPath), Is.EqualTo(piece.fbx_sha256));
                Assert.That(AssetImporter.GetAtPath(piece.fbx_source_path),
                    Is.TypeOf<ModelImporter>());
                var prefabPath = "Assets/Resources/" +
                                 profile.ArtistPrefabResourcePath + ".prefab";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath);
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.fbx_source_path);
                Assert.That(source, Is.Not.Null, prefabPath);
                Assert.That(imported, Is.Not.Null, piece.fbx_source_path);
                ValidateHierarchy(profile, source, imported,
                    piece.fbx_source_path);
                Assert.That(piece.anchor_mappings, Has.Length.EqualTo(
                    profile.Anchors.Count));
                foreach (var anchor in profile.Anchors)
                {
                    var mapping = piece.anchor_mappings.Single(item =>
                        item.stable_anchor_id == anchor.AnchorId);
                    Assert.That(mapping.fbx_node_name,
                        Is.EqualTo(anchor.AnchorId.Replace('.', '_')));
                }
            }
        }

        private static void ValidateHierarchy(
            LuoyangP0LandmarkSecondBatchProfile profile, GameObject source,
            GameObject imported, string path)
        {
            var sourceLods = source.GetComponent<LODGroup>().GetLODs();
            var importedGroups = FindNamedLods(imported, path);
            Assert.That(sourceLods.Length, Is.EqualTo(3), path);
            Assert.That(importedGroups.Length, Is.EqualTo(3), path);
            for (var index = 0; index < 3; index++)
            {
                Assert.That(importedGroups[index].Length,
                    Is.EqualTo(sourceLods[index].renderers.Length),
                    path + " LOD" + index);
                Assert.That(importedGroups[index].All(renderer =>
                        renderer != null && renderer.sharedMaterial != null),
                    Is.True, path + " LOD" + index);
            }
            Assert.That(imported.GetComponentsInChildren<Collider>(true),
                Is.Empty, path);
            var sourceTransforms = source.GetComponentsInChildren<Transform>(
                true);
            var importedTransforms = imported
                .GetComponentsInChildren<Transform>(true);
            foreach (var anchor in profile.Anchors)
            {
                var sourceAnchor = sourceTransforms.Single(item =>
                    item.name == anchor.AnchorId);
                var importedAnchor = importedTransforms.SingleOrDefault(item =>
                    item.name == anchor.AnchorId.Replace('.', '_'));
                Assert.That(importedAnchor, Is.Not.Null,
                    path + " " + anchor.AnchorId);
                AssertVector(importedAnchor.localPosition,
                    sourceAnchor.localPosition, 0.0001f,
                    path + " " + anchor.AnchorId);
            }

            var sourceInstance = Object.Instantiate(source);
            var importedInstance = Object.Instantiate(imported);
            try
            {
                AssertVector(CalculateBounds(importedInstance).size,
                    CalculateBounds(sourceInstance).size, 0.001f,
                    path + " bounds");
            }
            finally
            {
                Object.DestroyImmediate(sourceInstance);
                Object.DestroyImmediate(importedInstance);
            }
        }

        private static Renderer[][] FindNamedLods(GameObject root, string path)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var result = new Renderer[3][];
            for (var index = 0; index < result.Length; index++)
            {
                var lod = transforms.SingleOrDefault(item =>
                    item.name == "LOD" + index);
                Assert.That(lod, Is.Not.Null, path + " LOD" + index);
                result[index] = lod.GetComponentsInChildren<Renderer>(true);
            }
            return result;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var group = root.GetComponent<LODGroup>();
            var renderers = group == null
                ? FindNamedLods(root, root.name).SelectMany(item => item)
                    .ToArray()
                : group.GetLODs().SelectMany(item => item.renderers).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
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

        private static void ValidateFiles(string projectRoot,
            IReadOnlyCollection<SourceFileRecord> files, int expectedCount)
        {
            Assert.That(files.Count, Is.EqualTo(expectedCount));
            Assert.That(files.Select(item => item.path)
                .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(expectedCount));
            foreach (var file in files)
            {
                var path = Path.Combine(projectRoot,
                    file.path.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(path), Is.True, file.path);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(file.length),
                    file.path);
                Assert.That(Sha256(path), Is.EqualTo(file.sha256), file.path);
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2")));
        }

        private static Fixture Load()
        {
            var value = new Fixture
            {
                Coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot)
            };
            value.Production = new LuoyangProductionBuildingKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            value.Landmarks = new LuoyangHistoricalLandmarkKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            value.Gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                value.Coverage.CombinedCatalog).Catalog;
            value.Fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog).Catalog;
            var performance = new LuoyangBuildingPerformancePlanSource(
                WorldMapRoot, value.Coverage.Bindings,
                value.Coverage.CombinedCatalog).Plan;
            value.Infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog,
                performance).Catalog;
            value.Defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog, value.Gates,
                performance).Catalog;
            value.Resources = new LuoyangResourceAgricultureProductionKitSource(
                WorldMapRoot, value.Coverage.CombinedCatalog,
                performance).Catalog;
            value.FinalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    WorldMapRoot, value.Coverage.CombinedCatalog,
                    value.Landmarks, performance).Catalog;
            value.Review = new LuoyangFinalAssetReviewManifestSource(
                WorldMapRoot, value.Production, value.Landmarks, value.Gates,
                value.Fabric, value.Infrastructure, value.Defense,
                value.Resources, value.FinalCivic, performance);
            value.Batch = new LuoyangP0LandmarkSecondBatchSource(WorldMapRoot,
                value.Coverage.CombinedCatalog, value.Landmarks,
                value.Review.Catalog);
            return value;
        }
    }
}
