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
    public sealed class LuoyangP0LandmarkThirdBatchV1Tests
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
            public LuoyangP0LandmarkThirdBatchSource Batch;
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
                LuoyangP0LandmarkThirdBatchIds.StatusId));
            Assert.That(fixture.Batch.Catalog.Profiles.Select(item =>
                item.ReviewOrder), Is.EqualTo(new[] { 6, 7, 8, 9 }));
            Assert.That(fixture.Batch.Plan.ProfilesByFacilityId.Keys,
                Is.EquivalentTo(LuoyangP0LandmarkThirdBatchIds.FacilityIds));
            Assert.That(fixture.Batch.Catalog.ReviewDecisionStatusId,
                Is.EqualTo(LuoyangP0LandmarkThirdBatchIds
                    .ReviewDecisionStatusId));
            Assert.That(fixture.Batch.Catalog.UserReviewDecisionRecordId,
                Is.EqualTo(LuoyangP0LandmarkThirdBatchIds
                    .UserReviewDecisionRecordId));
            Assert.That(fixture.Batch.Catalog.UserReviewDecisionDate,
                Is.EqualTo(LuoyangP0LandmarkThirdBatchIds
                    .UserReviewDecisionDate));
            foreach (var profile in fixture.Batch.Catalog.Profiles)
            {
                Assert.That(profile.ArtistPrefabPresent, Is.True,
                    profile.CandidateId);
                Assert.That(profile.FinalArtApproved, Is.True,
                    profile.CandidateId);
                var review = fixture.Review.Catalog.Items.Single(item =>
                    item.RepresentativeFacilityId == profile.FacilityId);
                Assert.That(review.ReviewOrder, Is.EqualTo(profile.ReviewOrder));
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
        public void BuildAssets_CreatesFourStrictThreeLodPrefabs()
        {
            var report = LuoyangP0LandmarkThirdBatchArtBuilder.BuildAssets();
            Assert.That(report.PrefabCount, Is.EqualTo(4));
            Assert.That(report.MaterialCount, Is.EqualTo(8));
            Assert.That(report.MeshCount, Is.EqualTo(5));
            Assert.That(report.Lod0RendererCount,
                Is.GreaterThan(report.Lod1RendererCount));
            Assert.That(report.Lod1RendererCount,
                Is.GreaterThan(report.Lod2RendererCount));
            foreach (var profile in Load().Batch.Catalog.Profiles)
                ValidatePrefab(profile);
        }

        [Test]
        public void ExportAll_CreatesFourReimportableFbxSources()
        {
            LuoyangP0LandmarkThirdBatchArtBuilder.BuildAssets();
            var report = LuoyangP0LandmarkThirdBatchFbxExporter.ExportAll();
            Assert.That(report.Revision, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchFbxExporter.RevisionId));
            Assert.That(report.Pieces.Count, Is.EqualTo(4));
            Assert.That(report.Pieces.Select(item => item.FacilityId),
                Is.EquivalentTo(LuoyangP0LandmarkThirdBatchIds.FacilityIds));
            var profiles = Load().Batch.Catalog.Profiles;
            foreach (var profile in profiles)
            {
                var piece = report.Pieces.Single(item =>
                    item.FacilityId == profile.FacilityId);
                Assert.That(piece.FbxLength, Is.GreaterThan(1024));
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.FbxPath);
                Assert.That(imported, Is.Not.Null, piece.FbxPath);
                var prefabPath = "Assets/Resources/" +
                                 profile.ArtistPrefabResourcePath + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                for (var level = 0; level < 3; level++)
                {
                    var lodName = "LOD" + level;
                    var prefabLod = Find(prefab.transform, lodName);
                    var importedLod = Find(imported.transform, lodName);
                    Assert.That(prefabLod, Is.Not.Null, prefabPath);
                    Assert.That(importedLod, Is.Not.Null, piece.FbxPath);
                    Assert.That(importedLod.GetComponentsInChildren<Renderer>(
                            true).Length,
                        Is.EqualTo(prefabLod.GetComponentsInChildren<Renderer>(
                            true).Length), piece.FbxPath);
                }
                var importedNames = imported.GetComponentsInChildren<Transform>(
                    true).Select(item => item.name).ToArray();
                foreach (var anchor in profile.Anchors)
                    Assert.That(importedNames,
                        Does.Contain(anchor.AnchorId.Replace('.', '_')),
                        piece.FbxPath);
                Assert.That(imported.GetComponentsInChildren<Collider>(true),
                    Is.Empty, piece.FbxPath);
            }
        }

        [Test]
        public void Runtime_LoadsRealApprovedPrefabsWithoutFallback()
        {
            var fixture = Load();
            var root = new GameObject("P0 Landmark Batch 3 Runtime Tests");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic, null, null, null, fixture.Batch.Plan);
            try
            {
                foreach (var profile in fixture.Batch.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId,
                        root.transform, profile.FacilityId, profile.CellId64,
                        true);
                    Assert.That(instance.P0LandmarkThirdBatchReady, Is.True);
                    Assert.That(instance.P0LandmarkSecondBatchReady, Is.False);
                    Assert.That(instance.P0FinalAssetArtistPrefabLoaded,
                        Is.True);
                    Assert.That(instance.P0FinalAssetProceduralFallbackActive,
                        Is.False);
                    Assert.That(instance.P0FinalAssetFinalArtApproved,
                        Is.True);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void Runtime_ApprovedCatalogKeepsProceduralFallbackUnapproved()
        {
            var fixture = Load();
            var root = new GameObject("P0 Landmark Batch 3 Fallback Tests");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic, null, _ => null, null,
                fixture.Batch.Plan);
            try
            {
                foreach (var profile in fixture.Batch.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId,
                        root.transform, profile.FacilityId, profile.CellId64,
                        true);
                    Assert.That(instance.P0LandmarkThirdBatchReady, Is.True);
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
        public void SourceManifest_FreezesFourAcceptedValidatedFbxSources()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var manifestPath = Path.Combine(projectRoot, "Assets",
                "ArtSource", "Han", "Luoyang", "P0Batch3",
                "luoyang_p0_landmark_third_batch_source_manifest_v1.json");
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var manifest = JsonUtility.FromJson<SourceManifest>(
                File.ReadAllText(manifestPath));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.schema_version, Is.EqualTo(1));
            Assert.That(manifest.contract_id, Is.EqualTo(
                "art_source.luoyang.p0-landmark-third-batch.user-accepted-fbx-source-validated-final-activation.v1"));
            Assert.That(manifest.task_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.FinalActivationTaskId));
            Assert.That(manifest.status_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.StatusId));
            Assert.That(manifest.selection_policy_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.SelectionPolicyId));
            Assert.That(manifest.review_decision_status_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.ReviewDecisionStatusId));
            Assert.That(manifest.user_review_decision_record_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.UserReviewDecisionRecordId));
            Assert.That(manifest.user_review_decision_date, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.UserReviewDecisionDate));
            Assert.That(manifest.final_art_approval_status_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.FinalArtApprovalStatusId));
            Assert.That(manifest.source_archive_status_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.SourceArchiveStatusId));
            Assert.That(manifest.source_license_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.SourceLicenseId));
            Assert.That(manifest.fbx_source_toolchain_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.FbxSourceToolchainId));
            Assert.That(manifest.fbx_toolchain_license_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.FbxToolchainLicenseId));
            Assert.That(manifest.fbx_anchor_name_mapping_id, Is.EqualTo(
                LuoyangP0LandmarkThirdBatchIds.FbxAnchorNameMappingId));
            Assert.That(manifest.user_review_decision,
                Is.EqualTo("ACCEPTED_ALL_FOUR"));
            Assert.That(manifest.source_file_count, Is.EqualTo(60));
            Assert.That(manifest.toolchain_file_count, Is.EqualTo(2));
            Assert.That(manifest.fbx_source_count, Is.EqualTo(4));
            Assert.That(manifest.fbx_missing_count, Is.Zero);
            Assert.That(manifest.user_review_ready, Is.True);
            Assert.That(manifest.final_art_activation_ready, Is.True);
            Assert.That(manifest.final_art_approved, Is.True);

            ValidateFiles(projectRoot, manifest.source_files, 60);
            ValidateFiles(projectRoot, manifest.toolchain_files, 2);
            Assert.That(manifest.pieces, Has.Length.EqualTo(4));
            Assert.That(manifest.pieces.Select(item => item.review_order),
                Is.EqualTo(new[] { 6, 7, 8, 9 }));
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
                Assert.That(piece.final_art_approved, Is.True);
                Assert.That(piece.anchor_mappings, Has.Length.EqualTo(
                    profile.Anchors.Count));
                foreach (var anchor in profile.Anchors)
                {
                    var mapping = piece.anchor_mappings.Single(item =>
                        item.stable_anchor_id == anchor.AnchorId);
                    Assert.That(mapping.fbx_node_name,
                        Is.EqualTo(anchor.AnchorId.Replace('.', '_')));
                }
                var fbxPath = Path.Combine(projectRoot,
                    piece.fbx_source_path.Replace('/',
                        Path.DirectorySeparatorChar));
                Assert.That(Sha256(fbxPath), Is.EqualTo(piece.fbx_sha256));
                Assert.That(AssetImporter.GetAtPath(piece.fbx_source_path),
                    Is.TypeOf<ModelImporter>());
            }
        }

        private static void ValidatePrefab(
            LuoyangP0LandmarkThirdBatchProfile profile)
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
            Assert.That(lods[0].renderers.Length,
                Is.GreaterThan(lods[1].renderers.Length), path);
            Assert.That(lods[1].renderers.Length,
                Is.GreaterThan(lods[2].renderers.Length), path);
            var names = prefab.GetComponentsInChildren<Transform>(true)
                .Select(item => item.name).ToArray();
            foreach (var anchor in profile.Anchors)
                Assert.That(names, Does.Contain(anchor.AnchorId), path);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var index = 0; index < root.childCount; index++)
            {
                var value = Find(root.GetChild(index), name);
                if (value != null) return value;
            }
            return null;
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
            value.Batch = new LuoyangP0LandmarkThirdBatchSource(WorldMapRoot,
                value.Coverage.CombinedCatalog, value.Landmarks,
                value.Review.Catalog);
            return value;
        }
    }
}
