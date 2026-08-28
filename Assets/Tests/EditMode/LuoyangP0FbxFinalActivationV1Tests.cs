using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mandate.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0FbxFinalActivationV1Tests
    {
        [Serializable]
        private sealed class FileRecord
        {
            public string path;
            public long length;
            public string sha256;
        }

        [Serializable]
        private sealed class AnchorMapping
        {
            public string stable_anchor_id;
            public string fbx_node_name;
        }

        [Serializable]
        private sealed class PieceRecord
        {
            public string facility_id;
            public string fbx_source_path;
            public long fbx_length;
            public string fbx_sha256;
            public string fbx_source_status;
            public string anchor_name_mapping_id;
            public AnchorMapping[] anchor_mappings;
            public bool final_art_approved;
        }

        [Serializable]
        private sealed class FinalManifest
        {
            public int schema_version;
            public string contract_id;
            public string task_id;
            public string user_review_decision_status_id;
            public string source_archive_status_id;
            public string final_art_approval_status_id;
            public string source_license_id;
            public string fbx_source_toolchain_id;
            public string fbx_toolchain_license_id;
            public string fbx_anchor_name_mapping_id;
            public string user_review_decision;
            public int final_source_file_count;
            public int toolchain_file_count;
            public int fbx_source_count;
            public int fbx_missing_count;
            public bool final_art_activation_ready;
            public bool final_art_approved;
            public FileRecord[] toolchain_files;
            public FileRecord[] final_source_files;
            public PieceRecord[] pieces;
        }

        [Test]
        public void Manifest_FreezesFourValidatedFbxSourcesAndFinalApproval()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var manifestPath = Path.Combine(projectRoot, "Assets",
                "ArtSource", "Han", "Luoyang", "P0Final",
                "luoyang_p0_final_source_archive_manifest_v1.json");
            var catalogPath = Path.Combine(projectRoot, "Assets",
                "StreamingAssets", "WorldMap",
                "LuoyangP0FinalAssetVerticalSliceV1",
                "luoyang_p0_final_asset_vertical_slice_v1.json");
            var manifest = JsonUtility.FromJson<FinalManifest>(
                File.ReadAllText(manifestPath));
            var catalog = JsonUtility.FromJson<
                LuoyangP0FinalAssetVerticalSliceCatalog>(
                File.ReadAllText(catalogPath));

            Assert.That(manifest.schema_version, Is.EqualTo(1));
            Assert.That(manifest.contract_id, Is.EqualTo(
                "art_source.luoyang.p0-four-piece.fbx-source-freeze-and-final-activation.v1"));
            Assert.That(manifest.task_id, Is.EqualTo(
                "LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1"));
            Assert.That(manifest.user_review_decision_status_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .UserReviewDecisionStatusId));
            Assert.That(manifest.source_archive_status_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.SourceArchiveStatusId));
            Assert.That(manifest.final_art_approval_status_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .FinalArtApprovalStatusId));
            Assert.That(manifest.source_license_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.SourceLicenseId));
            Assert.That(manifest.fbx_source_toolchain_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.FbxSourceToolchainId));
            Assert.That(manifest.fbx_toolchain_license_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .FbxToolchainLicenseId));
            Assert.That(manifest.fbx_anchor_name_mapping_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.FbxAnchorNameMappingId));
            Assert.That(manifest.user_review_decision,
                Is.EqualTo("ACCEPTED_ALL_FOUR"));
            Assert.That(manifest.final_source_file_count, Is.EqualTo(42));
            Assert.That(manifest.toolchain_file_count, Is.EqualTo(2));
            Assert.That(manifest.fbx_source_count, Is.EqualTo(4));
            Assert.That(manifest.fbx_missing_count, Is.Zero);
            Assert.That(manifest.final_art_activation_ready, Is.True);
            Assert.That(manifest.final_art_approved, Is.True);

            ValidateFiles(projectRoot, manifest.final_source_files, 42);
            ValidateFiles(projectRoot, manifest.toolchain_files, 2);
            Assert.That(manifest.pieces, Has.Length.EqualTo(4));
            Assert.That(manifest.pieces.Select(item => item.facility_id),
                Is.EquivalentTo(
                    LuoyangP0FinalAssetVerticalSliceIds.FacilityIds));

            foreach (var profile in catalog.Profiles)
            {
                Assert.That(profile.CandidateStatusId, Is.EqualTo(
                    LuoyangP0FinalAssetVerticalSliceIds.CandidateStatusId));
                Assert.That(profile.ArtistPrefabPresent, Is.True);
                Assert.That(profile.FinalArtApproved, Is.True);
                var piece = manifest.pieces.Single(item =>
                    item.facility_id == profile.FacilityId);
                Assert.That(piece.fbx_source_path,
                    Is.EqualTo(profile.ArtistFbxTargetPath));
                Assert.That(piece.fbx_length, Is.GreaterThan(1024));
                Assert.That(piece.fbx_source_status,
                    Is.EqualTo("PRESENT_UNITY_REIMPORT_VALIDATED"));
                Assert.That(piece.anchor_name_mapping_id, Is.EqualTo(
                    LuoyangP0FinalAssetVerticalSliceIds
                        .FbxAnchorNameMappingId));
                Assert.That(piece.final_art_approved, Is.True);
                var fbxPath = Path.Combine(projectRoot,
                    piece.fbx_source_path.Replace('/',
                        Path.DirectorySeparatorChar));
                Assert.That(Sha256(fbxPath), Is.EqualTo(piece.fbx_sha256));
                Assert.That(AssetImporter.GetAtPath(piece.fbx_source_path),
                    Is.TypeOf<ModelImporter>());
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

        private static void ValidateFiles(string projectRoot,
            IReadOnlyCollection<FileRecord> files, int expectedCount)
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
    }
}
