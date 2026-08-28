using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public sealed class LuoyangRemainingFinalAssetV1Tests
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
            public LuoyangRemainingFinalAssetSource Remaining;
        }

        [Serializable]
        private sealed class SourceManifest
        {
            public int profile_count;
            public int covered_facility_count;
            public int source_file_count;
            public int fbx_source_count;
            public int fbx_missing_count;
            public bool all_sources_unity_reimport_validated;
            public bool final_art_approved;
            public SourceManifestFile[] source_files;
            public SourceManifestPiece[] pieces;
        }

        [Serializable]
        private sealed class SourceManifestFile
        {
            public string path;
            public long length;
            public string sha256;
        }

        [Serializable]
        private sealed class SourceManifestPiece
        {
            public long prefab_length;
            public long fbx_length;
            public int lod_count;
            public bool final_art_approved;
        }

        [Test]
        public void Catalog_FreezesAll38RemainingAcceptedSlots()
        {
            var fixture = Load();
            Assert.That(fixture.Remaining.Catalog.StatusId, Is.EqualTo(
                LuoyangRemainingFinalAssetIds.StatusId));
            Assert.That(fixture.Remaining.Catalog.Profiles,
                Has.Count.EqualTo(38));
            Assert.That(fixture.Remaining.Catalog.Profiles.Sum(item =>
                item.FacilityUsageCount), Is.EqualTo(2068));
            Assert.That(fixture.Remaining.Catalog.Profiles.All(item =>
                item.ArtistPrefabPresent && item.FinalArtApproved), Is.True);
        }

        [Test]
        public void BuildAssets_Creates38NativeThreeLodPrefabs()
        {
            var report = LuoyangRemainingFinalAssetArtBuilder.BuildAssets();
            Assert.That(report.PrefabCount, Is.EqualTo(38));
            Assert.That(report.MaterialCount, Is.GreaterThanOrEqualTo(6));
            Assert.That(report.MeshCount, Is.GreaterThanOrEqualTo(8));
            Assert.That(report.Lod0RendererCount,
                Is.GreaterThan(report.Lod1RendererCount));
            Assert.That(report.Lod1RendererCount,
                Is.GreaterThanOrEqualTo(report.Lod2RendererCount));
        }

        [Test]
        public void Prefabs_AfterProjectReloadExpose38NativeThreeLodAssets()
        {
            foreach (var profile in Load().Remaining.Catalog.Profiles)
                ValidatePrefab(profile);
        }

        [Test]
        public void ExportAll_Creates38ReimportableFbxSources()
        {
            var report = LuoyangRemainingFinalAssetFbxExporter.ExportAll();
            Assert.That(report.Pieces, Has.Count.EqualTo(38));
            foreach (var profile in Load().Remaining.Catalog.Profiles)
            {
                var piece = report.Pieces.Single(item =>
                    item.AssetVariantId == profile.AssetVariantId);
                Assert.That(piece.FbxLength, Is.GreaterThan(1024));
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.FbxPath);
                Assert.That(imported, Is.Not.Null, piece.FbxPath);
                var names = imported.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name).ToArray();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    piece.PrefabPath);
                var metadata = prefab.GetComponent<
                    LuoyangFinalAssetPrefabMetadata>();
                foreach (var anchor in metadata.StableAnchorIds)
                    Assert.That(names, Does.Contain(anchor.Replace('.', '_')),
                        piece.FbxPath);
                Assert.That(imported.GetComponentsInChildren<Collider>(true),
                    Is.Empty, piece.FbxPath);
            }
        }

        [Test]
        public void SourceManifest_Freezes240FilesAnd38ValidatedFbxSources()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "ArtSource", "Han", "Luoyang",
                "FinalRemaining",
                "luoyang_remaining_38_final_asset_source_manifest_v1.json");
            Assert.That(File.Exists(path), Is.True, path);
            var manifest = JsonUtility.FromJson<SourceManifest>(
                File.ReadAllText(path));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.profile_count, Is.EqualTo(38));
            Assert.That(manifest.covered_facility_count, Is.EqualTo(2068));
            Assert.That(manifest.source_file_count, Is.EqualTo(240));
            Assert.That(manifest.fbx_source_count, Is.EqualTo(38));
            Assert.That(manifest.fbx_missing_count, Is.Zero);
            Assert.That(manifest.final_art_approved, Is.True);
            Assert.That(manifest.all_sources_unity_reimport_validated,
                Is.True);
            var sourceFiles = manifest.source_files;
            Assert.That(sourceFiles, Is.Not.Null);
            Assert.That(sourceFiles.Length, Is.EqualTo(240));
            foreach (var file in sourceFiles)
                ValidateHash(file);
            var pieces = manifest.pieces;
            Assert.That(pieces, Is.Not.Null);
            Assert.That(pieces.Length, Is.EqualTo(38));
            Assert.That(pieces.All(piece => piece.lod_count == 3 &&
                piece.final_art_approved && piece.prefab_length > 1024 &&
                piece.fbx_length > 1024), Is.True);
        }

        [Test]
        public void Runtime_RealPrefabsApproveAll38AndFallbackApprovesNone()
        {
            var fixture = Load();
            ValidateRuntime(fixture, null, true);
            ValidateRuntime(fixture, _ => null, false);
        }

        private static void ValidateRuntime(Fixture fixture,
            Func<string, GameObject> loader, bool expectedApproved)
        {
            var root = new GameObject("Remaining Final Asset Runtime Tests");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic, null, loader, null, null, null,
                fixture.Remaining.Plan);
            try
            {
                foreach (var profile in fixture.Remaining.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId,
                        root.transform, profile.RepresentativeFacilityId,
                        profile.RepresentativeCellId64, true);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(profile.AssetVariantId));
                    Assert.That(instance.FinalAssetRuntimeReady, Is.True);
                    Assert.That(instance.FinalAssetArtistPrefabLoaded,
                        Is.EqualTo(expectedApproved));
                    Assert.That(instance.FinalAssetProceduralFallbackActive,
                        Is.EqualTo(!expectedApproved));
                    Assert.That(instance.FinalAssetApproved,
                        Is.EqualTo(expectedApproved));
                    Assert.That(instance.GetComponentsInChildren<LODGroup>(
                        true), Is.Not.Empty);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        private static void ValidatePrefab(
            LuoyangRemainingFinalAssetProfile profile)
        {
            var path = "Assets/Resources/" +
                       profile.ArtistPrefabResourcePath + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),
                Is.Empty, path);
            var metadata = prefab.GetComponent<
                LuoyangFinalAssetPrefabMetadata>();
            Assert.That(metadata, Is.Not.Null, path);
            Assert.That(metadata.AssetVariantId,
                Is.EqualTo(profile.AssetVariantId));
            Assert.That(metadata.StableAnchorIds, Is.Not.Empty);
            var group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null, path);
            var lods = group.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(3), path);
            Assert.That(lods.All(lod => lod.renderers != null &&
                lod.renderers.Length > 0 && lod.renderers.All(renderer =>
                    renderer != null && renderer.sharedMaterial != null)),
                Is.True, path);
            Assert.That(lods[0].renderers.Length,
                Is.GreaterThanOrEqualTo(lods[1].renderers.Length), path);
            Assert.That(lods[1].renderers.Length,
                Is.GreaterThanOrEqualTo(lods[2].renderers.Length), path);
            Assert.That(lods[0].renderers.Length,
                Is.GreaterThan(lods[2].renderers.Length), path);
            var names = prefab.GetComponentsInChildren<Transform>(true)
                .Select(item => item.name).ToArray();
            foreach (var anchor in metadata.StableAnchorIds)
                Assert.That(names, Does.Contain(anchor), path);
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
            value.Remaining = new LuoyangRemainingFinalAssetSource(
                WorldMapRoot, value.Review.Catalog);
            return value;
        }

        private static void ValidateHash(SourceManifestFile record)
        {
            var relative = record.path;
            var path = Path.Combine(Directory.GetCurrentDirectory(),
                relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(path), Is.True, relative);
            Assert.That(new FileInfo(path).Length,
                Is.EqualTo(record.length), relative);
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var builder = new StringBuilder(64);
                foreach (var value in sha.ComputeHash(stream))
                    builder.Append(value.ToString("x2"));
                Assert.That(builder.ToString(),
                    Is.EqualTo(record.sha256), relative);
            }
        }
    }
}
