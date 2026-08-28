using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class
        LuoyangP0UserAcceptanceAndSourceArchiveReadinessV1Tests
    {
        [Serializable]
        private sealed class SourceFileRecord
        {
            public string path;
            public long length;
            public string sha256;
        }

        [Serializable]
        private sealed class PieceRecord
        {
            public string facility_id;
            public string user_review_decision;
            public string independent_fbx_target_path;
            public bool independent_fbx_exists;
            public string independent_fbx_status;
            public bool final_art_approved;
        }

        [Serializable]
        private sealed class ArchiveManifest
        {
            public int schema_version;
            public string contract_id;
            public string user_review_decision_status_id;
            public string user_review_decision_record_id;
            public string user_review_decision_date;
            public string source_archive_status_id;
            public string user_review_decision;
            public int unity_native_source_file_count;
            public int independent_fbx_target_count;
            public int independent_fbx_missing_count;
            public bool final_art_activation_ready;
            public bool final_art_approved;
            public SourceFileRecord[] unity_native_source_files;
            public PieceRecord[] pieces;
        }

        [Test]
        public void Manifest_PreservesSupersededReadinessSnapshot()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var manifestPath = Path.Combine(projectRoot, "Assets",
                "ArtSource", "Han", "Luoyang", "P0Final",
                "luoyang_p0_source_archive_manifest_v1.json");
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            var manifest = JsonUtility.FromJson<ArchiveManifest>(
                File.ReadAllText(manifestPath));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.schema_version, Is.EqualTo(1));
            Assert.That(manifest.contract_id, Is.EqualTo(
                "art_source.luoyang.p0-four-piece.user-acceptance-and-source-readiness.v1"));
            Assert.That(manifest.user_review_decision_status_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .UserReviewDecisionStatusId));
            Assert.That(manifest.user_review_decision_record_id, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds
                    .UserReviewDecisionRecordId));
            Assert.That(manifest.user_review_decision_date, Is.EqualTo(
                LuoyangP0FinalAssetVerticalSliceIds.UserReviewDecisionDate));
            Assert.That(manifest.source_archive_status_id, Is.EqualTo(
                "source_archive.unity_native_complete.independent_dcc_fbx_missing.v1"));
            Assert.That(manifest.user_review_decision,
                Is.EqualTo("ACCEPTED_ALL_FOUR"));
            Assert.That(manifest.unity_native_source_file_count,
                Is.EqualTo(32));
            Assert.That(manifest.independent_fbx_target_count, Is.EqualTo(4));
            Assert.That(manifest.independent_fbx_missing_count, Is.EqualTo(4));
            Assert.That(manifest.final_art_activation_ready, Is.False);
            Assert.That(manifest.final_art_approved, Is.False);

            Assert.That(manifest.unity_native_source_files,
                Has.Length.EqualTo(32));
            Assert.That(manifest.unity_native_source_files.Select(item =>
                item.path).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(32));
            foreach (var source in manifest.unity_native_source_files)
            {
                Assert.That(source.path, Is.Not.Empty);
                Assert.That(source.length, Is.GreaterThan(0), source.path);
                Assert.That(source.sha256, Has.Length.EqualTo(64), source.path);
            }

            Assert.That(manifest.pieces, Has.Length.EqualTo(4));
            Assert.That(manifest.pieces.Select(item => item.facility_id),
                Is.EquivalentTo(LuoyangP0FinalAssetVerticalSliceIds.FacilityIds));
            foreach (var piece in manifest.pieces)
            {
                Assert.That(piece.user_review_decision,
                    Is.EqualTo("ACCEPTED"), piece.facility_id);
                Assert.That(piece.independent_fbx_exists, Is.False,
                    piece.facility_id);
                Assert.That(piece.independent_fbx_status, Is.EqualTo(
                    "MISSING_REQUIRED_FOR_FINAL_ART_ACTIVATION"),
                    piece.facility_id);
                Assert.That(piece.final_art_approved, Is.False,
                    piece.facility_id);
            }
        }
    }
}
