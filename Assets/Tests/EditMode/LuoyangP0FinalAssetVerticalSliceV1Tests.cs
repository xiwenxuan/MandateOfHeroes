using System;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests.EditMode
{
    public sealed class LuoyangP0FinalAssetVerticalSliceV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

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
            public LuoyangFinalCivicRitualMedicalProductionKitCatalog FinalCivic;
            public LuoyangFinalAssetReviewManifestSource Review;
            public LuoyangP0FinalAssetVerticalSliceSource P0;
        }

        private static Fixture Load()
        {
            var value = new Fixture();
            value.Coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            value.Production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                value.Coverage.CombinedCatalog).Catalog;
            value.Landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                value.Coverage.CombinedCatalog).Catalog;
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
            value.Review = new LuoyangFinalAssetReviewManifestSource(WorldMapRoot,
                value.Production, value.Landmarks, value.Gates, value.Fabric,
                value.Infrastructure, value.Defense, value.Resources,
                value.FinalCivic, performance);
            value.P0 = new LuoyangP0FinalAssetVerticalSliceSource(WorldMapRoot,
                value.Coverage.CombinedCatalog, value.Landmarks, value.Gates,
                value.Review.Catalog);
            return value;
        }

        private static HanBuildableFacilityModelFactory CreateFactory(
            Fixture fixture, Func<string, GameObject> loader = null) =>
            new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic, fixture.P0.Plan, loader);

        [Test]
        public void Catalog_FreezesExactlyFourExistingReplacementSlots()
        {
            var fixture = Load();
            Assert.That(fixture.P0.Catalog.Profiles, Has.Count.EqualTo(4));
            Assert.That(fixture.P0.Catalog.Materials, Has.Count.EqualTo(6));
            Assert.That(fixture.P0.Catalog.UserReviewDecisionStatusId,
                Is.EqualTo(LuoyangP0FinalAssetVerticalSliceIds
                    .UserReviewDecisionStatusId));
            Assert.That(fixture.P0.Catalog.UserReviewDecisionRecordId,
                Is.EqualTo(LuoyangP0FinalAssetVerticalSliceIds
                    .UserReviewDecisionRecordId));
            Assert.That(fixture.P0.Catalog.SourceArchiveStatusId,
                Is.EqualTo(LuoyangP0FinalAssetVerticalSliceIds
                    .SourceArchiveStatusId));
            Assert.That(fixture.P0.Plan.ProfilesByFacilityId.Keys,
                Is.EquivalentTo(LuoyangP0FinalAssetVerticalSliceIds.FacilityIds));
            foreach (var profile in fixture.P0.Catalog.Profiles)
            {
                Assert.That(profile.ReplacementSlotId,
                    Is.EqualTo(profile.AssetVariantId), profile.CandidateId);
                Assert.That(profile.Modules.Count, Is.GreaterThanOrEqualTo(8));
                Assert.That(profile.Lod2ModuleIds,
                    Is.SubsetOf(profile.Lod1ModuleIds));
                Assert.That(profile.ArtistPrefabPresent, Is.True);
                Assert.That(profile.FinalArtApproved, Is.True);
                var review = fixture.Review.Catalog.Items.Single(item =>
                    item.RepresentativeFacilityId == profile.FacilityId);
                Assert.That(profile.ModelId, Is.EqualTo(review.ModelId));
                Assert.That(profile.ReplacementSlotId,
                    Is.EqualTo(review.ReplacementSlotId));
                Assert.That(profile.CellId64,
                    Is.EqualTo(review.RepresentativeCellId64));
            }
        }

        [Test]
        public void Runtime_FallsBackToOriginalThreeLodCandidatesWithoutColliders()
        {
            var fixture = Load();
            var root = new GameObject("P0 Final Asset Candidate Tests");
            var factory = CreateFactory(fixture, _ => null);
            try
            {
                foreach (var profile in fixture.P0.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId,
                        root.transform, profile.FacilityId, profile.CellId64,
                        true);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(profile.ReplacementSlotId));
                    Assert.That(instance.P0FinalAssetVerticalSliceReady, Is.True);
                    Assert.That(instance.P0FinalAssetArtistPrefabLoaded, Is.False);
                    Assert.That(instance.P0FinalAssetProceduralFallbackActive,
                        Is.True);
                    Assert.That(instance.P0FinalAssetFinalArtApproved, Is.False);
                    Assert.That(instance.GetComponent<LODGroup>().GetLODs(),
                        Has.Length.EqualTo(3));
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty);
                    Assert.That(factory.GetWorldBatchModules(profile.ModelId,
                            profile.FacilityId).Select(item => item.ModuleId),
                        Is.EquivalentTo(profile.Lod2ModuleIds));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ArtistPrefabContract_LoadsAValidHotSwapWithoutChangingIdentity()
        {
            var fixture = Load();
            var profile = fixture.P0.Catalog.Profiles[0];
            var root = new GameObject("P0 Artist Prefab Contract Tests");
            var prefab = CreateArtistPrefab(profile, out var material);
            var factory = CreateFactory(fixture, path => path ==
                profile.ArtistPrefabResourcePath ? prefab : null);
            try
            {
                var instance = factory.Create(profile.ModelId, root.transform,
                    profile.FacilityId, profile.CellId64, true);
                Assert.That(instance.AssetId,
                    Is.EqualTo(profile.ReplacementSlotId));
                Assert.That(instance.RuntimeBindingId,
                    Is.EqualTo(profile.FacilityId));
                Assert.That(instance.P0FinalAssetArtistPrefabLoaded, Is.True);
                Assert.That(instance.P0FinalAssetProceduralFallbackActive,
                    Is.False);
                Assert.That(instance.P0FinalAssetFinalArtApproved, Is.True);
                Assert.That(instance.GetComponentsInChildren<LODGroup>(true),
                    Has.Length.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(material);
                factory.Dispose();
            }
        }

        [Test]
        public void ReviewBoard_UsesFourCellsAndFiveFixedCameras()
        {
            var fixture = Load();
            var placements = LuoyangP0FinalAssetVerticalSlicePreviewPlan.Create(
                GlobalSpatialFoundationV1.CreateCellGrid(), fixture.P0.Plan);
            Assert.That(placements, Has.Count.EqualTo(4));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(4));
            Assert.That(placements.Select(item => item.RuntimeBindingId),
                Is.EqualTo(LuoyangP0FinalAssetVerticalSliceIds.FacilityIds));
            var cameraIds = new[]
            {
                StrategicCellCameraRig.LuoyangP0FinalAssetVerticalSliceOverview,
                StrategicCellCameraRig.LuoyangP0SouthPalaceCloseup,
                StrategicCellCameraRig.LuoyangP0MingtangCloseup,
                StrategicCellCameraRig.LuoyangP0GuangyangmenCloseup,
                StrategicCellCameraRig.LuoyangP0NorthPalaceGateCloseup
            };
            Assert.That(cameraIds.All(
                StrategicCellCameraRig.IsLuoyangP0FinalAssetVerticalSlice),
                Is.True);
            Assert.That(cameraIds.Select(StrategicCellCameraRig.Get)
                .All(item => !item.IsWorldView), Is.True);
            var closeups = cameraIds.Skip(1).Select(StrategicCellCameraRig.Get)
                .ToArray();
            Assert.That(closeups.All(item => item.Size <= 1.45f), Is.True);
            Assert.That(closeups.All(item => item.Pitch >= 38f &&
                item.Pitch <= 44f), Is.True);
        }

        private static GameObject CreateArtistPrefab(
            LuoyangP0FinalAssetProfile profile, out Material material)
        {
            var value = new GameObject("Valid P0 Artist Prefab");
            foreach (var anchor in profile.Anchors)
            {
                var child = new GameObject(anchor.AnchorId);
                child.transform.SetParent(value.transform, false);
                child.transform.localPosition = new Vector3(anchor.X, anchor.Y,
                    anchor.Z);
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard") ?? Shader.Find("Diffuse") ??
                         Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "P0_TEST_ARTIST_MATERIAL" };
            var renderers = new Renderer[3];
            for (var index = 0; index < renderers.Length; index++)
            {
                var lod = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lod.name = "LOD" + index;
                lod.transform.SetParent(value.transform, false);
                UnityEngine.Object.DestroyImmediate(lod.GetComponent<Collider>());
                renderers[index] = lod.GetComponent<Renderer>();
                renderers[index].sharedMaterial = material;
            }
            var group = value.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.18f, new[] { renderers[0] }),
                new LOD(0.065f, new[] { renderers[1] }),
                new LOD(0.010f, new[] { renderers[2] })
            });
            group.RecalculateBounds();
            return value;
        }
    }
}
