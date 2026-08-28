using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mandate.Tests
{
    public sealed class LuoyangFacilityModelCoverageV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void CoverageCatalog_ComposesSevenAcceptedAndTwentyNineSupplementalModels()
        {
            var source = new LuoyangFacilityModelCoverageSource(WorldMapRoot);

            Assert.That(source.SupplementalCatalog.Models, Has.Count.EqualTo(29));
            Assert.That(source.CombinedCatalog.Models, Has.Count.EqualTo(36));
            Assert.That(source.CombinedCatalog.Models.Select(item => item.ModelId),
                Is.EquivalentTo(LuoyangFacilityModelCoverageIds.AllModelIds));
            Assert.That(source.CombinedCatalog.Models.Select(item => item.AssetId)
                .Distinct().Count(), Is.EqualTo(36));
            Assert.That(source.Bindings.DefinitionBindings, Has.Count.GreaterThanOrEqualTo(61));
            Assert.That(source.Bindings.FacilityOverrides, Has.Count.EqualTo(1));
        }

        [Test]
        public void BindingCatalog_CoversEveryOpeningFacilityWithoutSubstringFallback()
        {
            var source = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var resolver = new LuoyangFacilityModelBindingResolver(source.Bindings,
                source.CombinedCatalog);
            var definitions = new HashSet<string>(System.StringComparer.Ordinal);
            var instanceCount = 0;
            foreach (var relative in new[]
                     {
                         Path.Combine("Luoyang184UrbanInitializationV1", "facilities.json"),
                         Path.Combine("Luoyang184MetropolitanInitializationV1", "facilities.json")
                     })
            {
                var json = File.ReadAllText(Path.Combine(WorldMapRoot, relative));
                var matches = Regex.Matches(json,
                    "\\\"definition_id\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
                instanceCount += matches.Count;
                foreach (Match match in matches) definitions.Add(match.Groups[1].Value);
            }

            Assert.That(instanceCount, Is.EqualTo(2084));
            Assert.That(definitions, Has.Count.EqualTo(61));
            Assert.That(definitions.All(resolver.CoversDefinition), Is.True,
                "All 2,084 opening Facilities must resolve through explicit stable IDs.");
            Assert.That(resolver.ResolveModelId("facility.agriculture.millet_field"),
                Is.EqualTo(LuoyangFacilityModelCoverageIds.DryField));
            Assert.That(resolver.ResolveModelId("facility.education.academy"),
                Is.EqualTo(LuoyangFacilityModelCoverageIds.ImperialAcademy));
            Assert.That(resolver.ResolveModelId("facility.historical.urban_ward"),
                Is.EqualTo(HanBuildableFacilityModelIds.Residence));
            Assert.That(resolver.ResolveModelId("facility.military.fortified_manor"),
                Is.EqualTo(LuoyangFacilityModelCoverageIds.FortifiedManor));
            Assert.That(resolver.ResolveModelId("facility.storage.warehouse",
                    "facility.instance.luoyang.184.arsenal"),
                Is.EqualTo(LuoyangFacilityModelCoverageIds.Arsenal));
        }

        [Test]
        public void VisualSystem_UsesExplicitModelAssetsForKnownRegressionCases()
        {
            var source = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var system = new LuoyangVisualPresentationSystem(source.Bindings,
                source.CombinedCatalog);

            Assert.That(system.ResolveProfile("facility.agriculture.millet_field")
                .MainAssetId, Is.EqualTo(LuoyangFacilityModelCoverageIds.DryFieldAsset));
            Assert.That(system.ResolveProfile("facility.education.academy")
                .MainAssetId,
                Is.EqualTo(LuoyangFacilityModelCoverageIds.ImperialAcademyAsset));
            Assert.That(system.ResolveProfile("facility.historical.urban_ward")
                .MainAssetId, Is.EqualTo(HanBuildableFacilityModelIds.ResidenceAsset));
            Assert.That(system.ResolveProfile("facility.government.court_hall")
                .MainAssetId,
                Is.EqualTo(LuoyangFacilityModelCoverageIds.PalaceComplexAsset));
            Assert.That(system.ResolveProfile("facility.storage.warehouse",
                    "facility.instance.luoyang.184.arsenal").MainAssetId,
                Is.EqualTo(LuoyangFacilityModelCoverageIds.ArsenalAsset));
        }

        [Test]
        public void CompositeFactory_CreatesAllModelsAndCoveragePlanUsesDistinctCells()
        {
            var source = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var root = new GameObject("Luoyang Facility Coverage Test Root");
            var factory = new HanBuildableFacilityModelFactory(source.CombinedCatalog);
            try
            {
                foreach (var model in source.CombinedCatalog.Models)
                {
                    var instance = factory.Create(model.ModelId, root.transform,
                        "test." + model.ModelId, 100, true);
                    Assert.That(instance.GetComponentsInChildren<Renderer>(),
                        Has.Length.EqualTo(model.Modules.Count), model.ModelId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(), Is.Empty,
                        model.ModelId);
                }
                Assert.That(factory.ModelCount, Is.EqualTo(36));

                var grid = GlobalSpatialFoundationV1.CreateCellGrid();
                var plan = LuoyangFacilityModelCoveragePreviewPlan.Create(grid,
                    1241, 2043);
                Assert.That(plan, Has.Count.EqualTo(36));
                Assert.That(plan.Select(item => item.CellId.Value).Distinct().Count(),
                    Is.EqualTo(36));
                Assert.That(plan.Select(item => item.ModelId),
                    Is.EquivalentTo(LuoyangFacilityModelCoverageIds.AllModelIds));
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void CoverageCamera_IsASeparateCloseReviewEntry()
        {
            var preset = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangFacilityCoverageReview);
            Assert.That(preset.Id,
                Is.EqualTo(StrategicCellCameraRig.LuoyangFacilityCoverageReview));
            Assert.That(preset.IsWorldView, Is.False);
            Assert.That(preset.DetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
            Assert.That(preset.Size, Is.InRange(9f, 13f));
        }
    }

    public sealed class LuoyangProductionBuildingKitV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Catalog_FreezesTenHighFrequencyProfilesAndEightySixPercentCoverage()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;

            Assert.That(production.SchemaId,
                Is.EqualTo(LuoyangProductionBuildingKitIds.SchemaId));
            Assert.That(production.KitId,
                Is.EqualTo(LuoyangProductionBuildingKitIds.KitId));
            Assert.That(production.Profiles, Has.Count.EqualTo(10));
            Assert.That(production.Profiles.Select(item => item.ModelId),
                Is.EqualTo(LuoyangProductionBuildingKitIds.HighFrequencyModelIds));
            Assert.That(production.Profiles.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(1800));
            Assert.That((double)production.CoveredOpeningFacilityCount /
                        production.OpeningFacilityCount,
                Is.EqualTo(1800d / 2084d).Within(0.000001d));
            Assert.That(production.Profiles.All(item =>
                item.Lod1ModuleIds.Count > item.Lod2ModuleIds.Count), Is.True);
            Assert.That(production.Profiles.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(10));
        }

        [Test]
        public void Factory_BuildsReusableCustomMeshesAnchorsAndThreeLods()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var root = new GameObject("Luoyang Production Building Kit Test Root");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production);
            try
            {
                Assert.That(factory.ProductionProfileCount, Is.EqualTo(10));
                foreach (var profile in production.Profiles)
                {
                    var instance = factory.Create(profile.ModelId, root.transform,
                        "production-test." + profile.ModelId, 100, true);
                    Assert.That(instance.ProductionReady, Is.True, profile.ModelId);
                    Assert.That(instance.ProductionProfileId,
                        Is.EqualTo(profile.ProfileId), profile.ModelId);
                    Assert.That(instance.ProductionAssetVariantId,
                        Is.EqualTo(profile.AssetVariantId), profile.ModelId);
                    Assert.That(instance.transform.Find(profile.PlacementAnchorId),
                        Is.Not.Null, profile.ModelId);
                    Assert.That(instance.transform.Find(profile.EntranceAnchorId),
                        Is.Not.Null, profile.ModelId);
                    var lod = instance.GetComponent<LODGroup>();
                    Assert.That(lod, Is.Not.Null, profile.ModelId);
                    Assert.That(lod.GetLODs(), Has.Length.EqualTo(3),
                        profile.ModelId);
                    var customMeshes = instance
                        .GetComponentsInChildren<MeshFilter>(true)
                        .Where(item => item.sharedMesh != null &&
                            item.sharedMesh.name.StartsWith("HAN_PRODUCTION_",
                                System.StringComparison.Ordinal))
                        .Select(item => item.sharedMesh).ToArray();
                    Assert.That(customMeshes, Is.Not.Empty, profile.ModelId);
                    Assert.That(customMeshes.Distinct().Count(),
                        Is.LessThan(customMeshes.Length),
                        profile.ModelId + " should reuse custom meshes across LODs.");
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.ModelId);
                }

                Assert.That(factory.ProductionMeshCount, Is.EqualTo(8));
                var fallback = factory.Create(HanBuildableFacilityModelIds.Market,
                    root.transform, "production-test.market", 101, true);
                Assert.That(fallback.ProductionReady, Is.False);
                Assert.That(fallback.GetComponent<LODGroup>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }
    }

    public sealed class LuoyangHistoricalLandmarkDistinctSilhouettesV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Catalog_UsesTenAuthoritativeFacilitiesCellsAndRestrictedAvailability()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();

            Assert.That(landmarks.SchemaId,
                Is.EqualTo(LuoyangHistoricalLandmarkKitIds.SchemaId));
            Assert.That(landmarks.KitId,
                Is.EqualTo(LuoyangHistoricalLandmarkKitIds.KitId));
            Assert.That(landmarks.Profiles, Has.Count.EqualTo(10));
            Assert.That(landmarks.Profiles.Select(item => item.FacilityId),
                Is.EqualTo(LuoyangHistoricalLandmarkKitIds.FacilityIds));
            Assert.That(landmarks.Profiles.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(10));
            Assert.That(landmarks.Profiles.Select(item => item.SilhouetteId)
                .Distinct().Count(), Is.EqualTo(10));
            Assert.That(landmarks.Profiles.All(item =>
                grid.ToCellId(item.GridY, item.GridX).Value == item.CellId64),
                Is.True);
            Assert.That(landmarks.Profiles.All(item =>
                item.AvailabilityIds.Contains("HistoricalInit") &&
                !item.AvailabilityIds.Contains("Player") &&
                !item.AvailabilityIds.Contains("Ai") &&
                item.SourceIds.Count > 0 &&
                !string.IsNullOrWhiteSpace(item.HistoricalBasis)), Is.True);
        }

        [Test]
        public void Factory_BuildsTenDistinctFacilityLevelSilhouettesWithThreeLods()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var root = new GameObject("Luoyang Historical Landmark Kit Test Root");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks);
            try
            {
                var signatures = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (var profile in landmarks.Profiles)
                {
                    var instance = factory.Create(profile.BaseModelId, root.transform,
                        profile.FacilityId, profile.CellId64, true);
                    Assert.That(instance.HistoricalLandmarkReady, Is.True,
                        profile.FacilityId);
                    Assert.That(instance.HistoricalLandmarkProfileId,
                        Is.EqualTo(profile.ProfileId), profile.FacilityId);
                    Assert.That(instance.HistoricalLandmarkSilhouetteId,
                        Is.EqualTo(profile.SilhouetteId), profile.FacilityId);
                    Assert.That(instance.AssetId, Is.EqualTo(profile.AssetVariantId),
                        profile.FacilityId);
                    Assert.That(instance.transform.Find(profile.PlacementAnchorId),
                        Is.Not.Null, profile.FacilityId);
                    Assert.That(instance.transform.Find(profile.EntranceAnchorId),
                        Is.Not.Null, profile.FacilityId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), profile.FacilityId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.FacilityId);
                    var lod0 = instance.transform.Find("LOD0.landmark");
                    Assert.That(lod0, Is.Not.Null, profile.FacilityId);
                    var signature = string.Join("|", lod0.Cast<Transform>()
                        .Select(item => item.name + ":" +
                            item.localPosition.ToString("F3") + ":" +
                            item.localScale.ToString("F3")));
                    Assert.That(signatures.Add(signature), Is.True,
                        profile.FacilityId + " must have an independent silhouette.");
                }

                Assert.That(factory.HistoricalLandmarkProfileCount, Is.EqualTo(10));
                Assert.That(signatures, Has.Count.EqualTo(10));
                Assert.That(factory.ProductionMeshCount, Is.GreaterThanOrEqualTo(6));
                var fallback = factory.Create(
                    LuoyangFacilityModelCoverageIds.PalaceComplex, root.transform,
                    "facility.instance.unrelated", 1, true);
                Assert.That(fallback.HistoricalLandmarkReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void PreviewPlan_UsesHistoricalCellsAndDedicatedReviewCamera()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var plan = LuoyangHistoricalLandmarkPreviewPlan.Create(grid, landmarks);

            Assert.That(plan, Has.Count.EqualTo(10));
            Assert.That(plan.Select(item => item.RuntimeBindingId),
                Is.EqualTo(LuoyangHistoricalLandmarkKitIds.FacilityIds));
            Assert.That(plan.Select(item => item.CellId.Value),
                Is.EqualTo(landmarks.Profiles.Select(item => item.CellId64)));
            Assert.That(plan.Select(item => item.CellId.Value).Distinct().Count(),
                Is.EqualTo(10));

            var preset = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangHistoricalLandmarkReview);
            Assert.That(preset.Row, Is.EqualTo(1246));
            Assert.That(preset.Column, Is.EqualTo(2043));
            Assert.That(preset.Size, Is.InRange(12f, 14f));
            Assert.That(preset.DetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
        }
    }

    public sealed class LuoyangGateIdentityV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Catalog_UsesExactlyTwelveCityAndTwoPalaceGateFacilities()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();

            Assert.That(gates.SchemaId,
                Is.EqualTo(LuoyangGateIdentityKitIds.SchemaId));
            Assert.That(gates.KitId, Is.EqualTo(LuoyangGateIdentityKitIds.KitId));
            Assert.That(gates.Profiles, Has.Count.EqualTo(14));
            Assert.That(gates.Profiles.Select(item => item.FacilityId),
                Is.EqualTo(LuoyangGateIdentityKitIds.FacilityIds));
            Assert.That(gates.Profiles.Count(item => item.GateClassId ==
                LuoyangGateIdentityKitIds.CityGateClassId), Is.EqualTo(12));
            Assert.That(gates.Profiles.Count(item => item.GateClassId ==
                LuoyangGateIdentityKitIds.PalaceGateClassId), Is.EqualTo(2));
            Assert.That(gates.Profiles.All(item =>
                grid.ToCellId(item.GridY, item.GridX).Value == item.CellId64 &&
                item.AvailabilityIds.Contains("HistoricalInit") &&
                !item.AvailabilityIds.Contains("Player") &&
                !item.AvailabilityIds.Contains("Ai")), Is.True);
            Assert.That(gates.Profiles.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(14));
            Assert.That(gates.Profiles.Select(item => item.SilhouetteId)
                .Distinct().Count(), Is.EqualTo(14));
            Assert.That(gates.Profiles.Any(item => item.FacilityId.Contains(
                ".recommended.")), Is.False);
        }

        [Test]
        public void Factory_BuildsFourteenDistinctGateIdentitiesWithPassageAnchors()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var root = new GameObject("Luoyang Gate Identity Kit Test Root");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates);
            try
            {
                var signatures = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (var profile in gates.Profiles)
                {
                    var instance = factory.Create(profile.BaseModelId, root.transform,
                        profile.FacilityId, profile.CellId64, true);
                    Assert.That(instance.GateIdentityReady, Is.True,
                        profile.FacilityId);
                    Assert.That(instance.GateIdentityProfileId,
                        Is.EqualTo(profile.ProfileId), profile.FacilityId);
                    Assert.That(instance.GateIdentitySilhouetteId,
                        Is.EqualTo(profile.SilhouetteId), profile.FacilityId);
                    Assert.That(instance.VisualFacing,
                        Is.EqualTo(profile.VisualFacing), profile.FacilityId);
                    Assert.That(instance.AssetId, Is.EqualTo(profile.AssetVariantId),
                        profile.FacilityId);
                    Assert.That(instance.transform.Find(profile.PlacementAnchorId),
                        Is.Not.Null, profile.FacilityId);
                    Assert.That(instance.transform.Find(
                        profile.OuterPassageAnchorId), Is.Not.Null,
                        profile.FacilityId);
                    Assert.That(instance.transform.Find(
                        profile.InnerPassageAnchorId), Is.Not.Null,
                        profile.FacilityId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), profile.FacilityId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.FacilityId);
                    var lod0 = instance.transform.Find("LOD0.gate-identity");
                    Assert.That(lod0, Is.Not.Null, profile.FacilityId);
                    var signature = string.Join("|", lod0.Cast<Transform>()
                        .Select(item => item.name + ":" +
                            item.localPosition.ToString("F3") + ":" +
                            item.localScale.ToString("F3")));
                    Assert.That(signatures.Add(signature), Is.True,
                        profile.FacilityId + " must have an independent silhouette.");
                }

                Assert.That(factory.GateIdentityProfileCount, Is.EqualTo(14));
                Assert.That(signatures, Has.Count.EqualTo(14));
                var fallback = factory.Create(HanBuildableFacilityModelIds.CityGate,
                    root.transform, "facility.instance.unrelated", 1, true);
                Assert.That(fallback.GateIdentityReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void PreviewPlan_UsesAuthoritativeCellsDirectionsAndReviewCamera()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var plan = LuoyangGateIdentityPreviewPlan.Create(grid, gates);

            Assert.That(plan, Has.Count.EqualTo(14));
            Assert.That(plan.Select(item => item.RuntimeBindingId),
                Is.EqualTo(LuoyangGateIdentityKitIds.FacilityIds));
            Assert.That(plan.Select(item => item.CellId.Value),
                Is.EqualTo(gates.Profiles.Select(item => item.CellId64)));
            Assert.That(plan.Select(item => item.CellId.Value).Distinct().Count(),
                Is.EqualTo(14));
            Assert.That(plan.Zip(gates.Profiles, (placement, profile) =>
                placement.RotationDegrees ==
                LuoyangGateIdentityKitIds.RotationForFacing(profile.VisualFacing))
                .All(value => value), Is.True);

            var preset = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangGateIdentityReview);
            Assert.That(preset.Row, Is.EqualTo(1241));
            Assert.That(preset.Column, Is.EqualTo(2043));
            Assert.That(preset.Size, Is.InRange(13f, 14f));
            Assert.That(preset.DetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
        }
    }

    public sealed class LuoyangMediumFrequencyUrbanFabricV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Catalog_FreezesFiveActualOpeningFacilityGroupsAndNinetyFourPercentCoverage()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;

            Assert.That(fabric.SchemaId,
                Is.EqualTo(LuoyangMediumFrequencyUrbanFabricKitIds.SchemaId));
            Assert.That(fabric.KitId,
                Is.EqualTo(LuoyangMediumFrequencyUrbanFabricKitIds.KitId));
            Assert.That(fabric.Profiles, Has.Count.EqualTo(5));
            Assert.That(fabric.Profiles.Select(item => item.ModelId),
                Is.EqualTo(LuoyangMediumFrequencyUrbanFabricKitIds.ModelIds));
            Assert.That(fabric.Profiles.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(158));
            Assert.That(fabric.ProducedOpeningFacilityCount, Is.EqualTo(1958));
            Assert.That((double)fabric.ProducedOpeningFacilityCount /
                        fabric.OpeningFacilityCount,
                Is.EqualTo(1958d / 2084d).Within(0.000001d));
            foreach (var profile in fabric.Profiles)
            {
                Assert.That(profile.OpeningUsageCount, Is.EqualTo(
                    LuoyangMediumFrequencyUrbanFabricKitIds
                        .OpeningUsageCounts[profile.ModelId]), profile.ModelId);
                Assert.That(profile.FacilityDefinitionIds, Is.EquivalentTo(
                    LuoyangMediumFrequencyUrbanFabricKitIds
                        .FacilityDefinitionIds[profile.ModelId]), profile.ModelId);
                Assert.That(profile.Lod2ModuleIds,
                    Is.SubsetOf(profile.Lod1ModuleIds), profile.ModelId);
            }
        }

        [Test]
        public void Factory_BuildsFiveDistinctCellBoundedModelsWithAnchorsAndThreeLods()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var root = new GameObject("Luoyang Urban Fabric Kit Test Root");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric);
            try
            {
                var signatures = new HashSet<string>(
                    System.StringComparer.Ordinal);
                foreach (var profile in fabric.Profiles)
                {
                    var instance = factory.Create(profile.ModelId, root.transform,
                        "urban-fabric-test." + profile.ModelId, 100, true);
                    Assert.That(instance.MediumFrequencyUrbanFabricReady, Is.True,
                        profile.ModelId);
                    Assert.That(instance.UrbanFabricProfileId,
                        Is.EqualTo(profile.ProfileId), profile.ModelId);
                    Assert.That(instance.AssetId, Is.EqualTo(profile.AssetVariantId),
                        profile.ModelId);
                    Assert.That(instance.UrbanFabricRoleId,
                        Is.EqualTo(profile.FabricRoleId), profile.ModelId);
                    Assert.That(instance.transform.Find(profile.PlacementAnchorId),
                        Is.Not.Null, profile.ModelId);
                    Assert.That(instance.transform.Find(profile.EntranceAnchorId),
                        Is.Not.Null, profile.ModelId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), profile.ModelId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.ModelId);
                    var lod0 = instance.transform.Find("LOD0.urban-fabric");
                    Assert.That(lod0, Is.Not.Null, profile.ModelId);
                    var signature = string.Join("|", lod0.Cast<Transform>()
                        .Select(item => item.name + ":" +
                            item.localPosition.ToString("F3") + ":" +
                            item.localScale.ToString("F3")));
                    Assert.That(signatures.Add(signature), Is.True,
                        profile.ModelId + " must have an independent silhouette.");
                }

                Assert.That(factory.MediumFrequencyUrbanFabricProfileCount,
                    Is.EqualTo(5));
                Assert.That(signatures, Has.Count.EqualTo(5));
                var fallback = factory.Create(HanBuildableFacilityModelIds.Residence,
                    root.transform, "facility.instance.unrelated", 101, true);
                Assert.That(fallback.MediumFrequencyUrbanFabricReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void PreviewPlan_UsesFifteenUniqueCellsFiveTypesAndDedicatedCamera()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var plan = LuoyangMediumFrequencyUrbanFabricPreviewPlan.Create(grid,
                1246, 2043);

            Assert.That(plan, Has.Count.EqualTo(15));
            Assert.That(plan.Select(item => item.CellId.Value).Distinct().Count(),
                Is.EqualTo(15));
            Assert.That(plan.GroupBy(item => item.ModelId).ToDictionary(
                    group => group.Key, group => group.Count()),
                Is.EquivalentTo(LuoyangMediumFrequencyUrbanFabricKitIds.ModelIds
                    .ToDictionary(modelId => modelId, _ => 3)));
            Assert.That(plan.Select(item => item.RotationDegrees).Distinct(),
                Is.EquivalentTo(new[] { 0f, 90f, 180f, 270f }));

            var preset = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangMediumFrequencyUrbanFabricReview);
            Assert.That(preset.Row, Is.EqualTo(1246));
            Assert.That(preset.Column, Is.EqualTo(2043));
            Assert.That(preset.Size, Is.EqualTo(6.6f).Within(0.001f));
            Assert.That(preset.DetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.ClosePreview));
        }
    }

    public sealed class LuoyangBuildingWholeCityPerformanceAndBatchingV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        [Test]
        public void Plan_UsesActualFacilitiesUniqueCellsAndFrozenDenseWindow()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var source = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            var plan = source.Plan;
            var window = LuoyangBuildingPerformanceRules
                .SelectDensestResidentWindow(plan);

            Assert.That(source.Budget.SchemaId,
                Is.EqualTo(LuoyangBuildingPerformanceBudgetIds.SchemaId));
            Assert.That(plan.Facilities.Count, Is.EqualTo(2084));
            Assert.That(plan.Facilities.Select(item => item.FacilityId)
                .Distinct().Count(), Is.EqualTo(2084));
            Assert.That(plan.Facilities.Select(item => item.CellId64)
                .Distinct().Count(), Is.EqualTo(2084));
            Assert.That(plan.SpatialBatches.Count, Is.EqualTo(64));
            Assert.That(plan.ResidentWindowCount, Is.EqualTo(11));
            Assert.That(window.FirstColumn, Is.EqualTo(2040));
            Assert.That(window.FirstRow, Is.EqualTo(1224));
            Assert.That(window.Facilities.Count, Is.EqualTo(549));
            Assert.That(window.SpatialBatches.Count, Is.EqualTo(9));
            Assert.That(window.Facilities.All(item =>
                item.GridColumn >= 2040 && item.GridColumn < 2064 &&
                item.GridRow >= 1224 && item.GridRow < 1248), Is.True);
        }

        [Test]
        public void Renderer_CombinesDenseWindowByEightCellBatchAndMaterialWithinBudget()
        {
            var coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var source = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, source.Plan);
            var defense =
                new LuoyangLowFrequencyDefenseProductionKitSource(WorldMapRoot,
                    coverage.CombinedCatalog, gates, source.Plan);
            var resourceAgriculture =
                new LuoyangResourceAgricultureProductionKitSource(WorldMapRoot,
                    coverage.CombinedCatalog, source.Plan);
            var finalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    WorldMapRoot, coverage.CombinedCatalog, landmarks,
                    source.Plan);
            var window = LuoyangBuildingPerformanceRules
                .SelectDensestResidentWindow(source.Plan);
            var root = new GameObject("Luoyang Building Batch Test Root");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric,
                infrastructure.Catalog, defense.Catalog,
                resourceAgriculture.Catalog, finalCivic.Catalog);
            var renderer = new LuoyangBuildingPerformanceBatchRenderer();
            try
            {
                var metrics = renderer.Build(root.transform, source.Plan, window,
                    factory,
                    item => new Vector3(item.GridColumn - 2051, 0f,
                        item.GridRow - 1235),
                    item => defense.Plan.Facilities.FirstOrDefault(
                                    value => value.FacilityId == item.FacilityId)
                                ?.RotationDegrees ??
                            infrastructure.Plan.Facilities.FirstOrDefault(
                                    value => value.FacilityId == item.FacilityId)
                                ?.RotationDegrees ?? item.RotationDegrees);

                Assert.That(metrics.WithinBudget, Is.True);
                Assert.That(metrics.FullCityFacilityCount, Is.EqualTo(2084));
                Assert.That(metrics.FullCitySpatialBatchCount, Is.EqualTo(64));
                Assert.That(metrics.ResidentFacilityCount, Is.EqualTo(549));
                Assert.That(metrics.ResidentSpatialBatchCount, Is.EqualTo(9));
                Assert.That(metrics.BuildingRendererBatchCount,
                    Is.LessThanOrEqualTo(200));
                Assert.That(metrics.CombinedMeshCount,
                    Is.EqualTo(metrics.BuildingRendererBatchCount));
                Assert.That(metrics.CombinedVertexCount,
                    Is.LessThanOrEqualTo(250000));
                Assert.That(metrics.RendererReductionRatio,
                    Is.GreaterThanOrEqualTo(0.85d));
                Assert.That(metrics.AllowsSpatialOcclusion, Is.True);
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true),
                    Has.Length.EqualTo(metrics.BuildingRendererBatchCount));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<MeshFilter>(true)
                    .All(item => item.sharedMesh != null), Is.True);
            }
            finally
            {
                renderer.Dispose();
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void Camera_UsesDensestWindowWithoutChangingStreamingSemantics()
        {
            var preset = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangBuildingPerformanceReview);
            Assert.That(preset.Row, Is.EqualTo(1235));
            Assert.That(preset.Column, Is.EqualTo(2051));
            Assert.That(preset.Size, Is.InRange(14f, 15f));
            Assert.That(preset.DetailLevel,
                Is.EqualTo(VisualTerrainDetailLevel.City));
            Assert.That(LuoyangBuildingPerformanceBudgetIds.SpatialBatchEdgeCells,
                Is.EqualTo(8));
            Assert.That(LuoyangBuildingPerformanceBudgetIds.ResidentWindowEdgeCells,
                Is.EqualTo(24));
        }
    }

    public sealed class LuoyangCanalWellBridgeInfrastructureProductionV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        private static LuoyangInfrastructureProductionKitSource Load(
            out LuoyangFacilityModelCoverageSource coverage,
            out LuoyangBuildingPerformancePlanSource performance)
        {
            coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            performance = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            return new LuoyangInfrastructureProductionKitSource(WorldMapRoot,
                coverage.CombinedCatalog, performance.Plan);
        }

        [Test]
        public void Catalog_PreservesThreeBaseModelsPermissionsAnd1995Coverage()
        {
            var source = Load(out var coverage, out _);
            Assert.That(source.Catalog.SchemaId, Is.EqualTo(
                LuoyangInfrastructureProductionKitIds.SchemaId));
            Assert.That(source.Catalog.Profiles.Count, Is.EqualTo(3));
            Assert.That(source.Catalog.Profiles.Select(item => item.ModelId),
                Is.EquivalentTo(
                    LuoyangInfrastructureProductionKitIds.ModelIds));
            Assert.That(source.Catalog.Profiles.Sum(item =>
                item.OpeningUsageCount), Is.EqualTo(37));
            Assert.That(source.Catalog.ProducedOpeningFacilityCount,
                Is.EqualTo(1995));

            foreach (var profile in source.Catalog.Profiles)
            {
                var model = coverage.CombinedCatalog.Models.Single(item =>
                    item.ModelId == profile.ModelId);
                Assert.That(profile.AvailabilityIds,
                    Is.EquivalentTo(model.AvailabilityIds), profile.ModelId);
                Assert.That(profile.Lod2ModuleIds.All(
                    profile.Lod1ModuleIds.Contains), Is.True, profile.ModelId);
                Assert.That(profile.Anchors.Select(item => item.AnchorId)
                    .Distinct().Count(), Is.EqualTo(profile.Anchors.Count),
                    profile.ModelId);
            }
        }

        [Test]
        public void Factory_CreatesDistinctThreeTierInfrastructureAndBatchLod2()
        {
            var infrastructure = Load(out var coverage, out _);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var root = new GameObject("Luoyang Infrastructure Factory Tests");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric,
                infrastructure.Catalog);
            try
            {
                var signatures = new HashSet<string>(StringComparer.Ordinal);
                foreach (var profile in infrastructure.Catalog.Profiles)
                {
                    var facility = infrastructure.Plan.Facilities.First(item =>
                        item.ModelId == profile.ModelId);
                    var instance = factory.Create(profile.ModelId, root.transform,
                        facility.FacilityId, facility.CellId64, true);
                    Assert.That(instance.InfrastructureProductionReady, Is.True,
                        profile.ModelId);
                    Assert.That(instance.InfrastructureProfileId,
                        Is.EqualTo(profile.ProfileId), profile.ModelId);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(profile.AssetVariantId), profile.ModelId);
                    Assert.That(instance.InfrastructureRoleId,
                        Is.EqualTo(profile.InfrastructureRoleId), profile.ModelId);
                    Assert.That(instance.transform.Find(profile.PlacementAnchorId),
                        Is.Not.Null, profile.ModelId);
                    foreach (var anchor in profile.Anchors)
                        Assert.That(instance.transform.Find(anchor.AnchorId),
                            Is.Not.Null, anchor.AnchorId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), profile.ModelId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.ModelId);
                    var lod0 = instance.transform.Find("LOD0.infrastructure");
                    Assert.That(lod0, Is.Not.Null, profile.ModelId);
                    var signature = string.Join("|", lod0.Cast<Transform>()
                        .Select(item => item.name + ":" +
                            item.localPosition.ToString("F3") + ":" +
                            item.localScale.ToString("F3")));
                    Assert.That(signatures.Add(signature), Is.True,
                        profile.ModelId + " must have an independent silhouette.");
                    var batchModules = factory.GetWorldBatchModules(profile.ModelId,
                        facility.FacilityId);
                    Assert.That(batchModules.Count,
                        Is.EqualTo(profile.Lod2ModuleIds.Count), profile.ModelId);
                    Assert.That(batchModules.Select(item => item.ModuleId),
                        Is.EquivalentTo(profile.Lod2ModuleIds), profile.ModelId);
                }
                Assert.That(factory.InfrastructureProductionProfileCount,
                    Is.EqualTo(3));
                Assert.That(signatures.Count, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ActualPlan_Freezes37CellsTwoWaterwaysAndThreeReviewCameras()
        {
            var infrastructure = Load(out _, out _);
            var plan = infrastructure.Plan;
            var placements = LuoyangInfrastructureProductionPreviewPlan.Create(
                GlobalSpatialFoundationV1.CreateCellGrid(), plan);

            Assert.That(plan.Facilities.Count, Is.EqualTo(37));
            Assert.That(placements.Count, Is.EqualTo(37));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(37));
            Assert.That(plan.WaterwayComponentCount, Is.EqualTo(2));
            Assert.That(plan.WaterwayEndpointCount, Is.EqualTo(4));
            Assert.That(plan.WaterwayStraightCount, Is.EqualTo(17));
            Assert.That(plan.Facilities.Count(item => item.TopologyId ==
                LuoyangInfrastructureProductionKitIds.TopologyIsolated),
                Is.EqualTo(16));
            Assert.That(plan.Facilities.Where(item => item.ConnectionMask != 0)
                .All(item => item.RotationDegrees == 0f), Is.True);

            var overview = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangInfrastructureOverview);
            var canal = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangInfrastructureCanalCorridor);
            var bridge = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangInfrastructureBridgeChain);
            Assert.That(overview.Row, Is.EqualTo(1236));
            Assert.That(overview.Column, Is.EqualTo(2048));
            Assert.That(canal.Row, Is.EqualTo(1227));
            Assert.That(canal.Column, Is.EqualTo(2037));
            Assert.That(bridge.Row, Is.EqualTo(1254));
            Assert.That(bridge.Column, Is.EqualTo(2054));
            Assert.That(StrategicCellCameraRig.IsLuoyangInfrastructureReview(
                overview.Id), Is.True);
            Assert.That(StrategicCellCameraRig.IsLuoyangInfrastructureReview(
                canal.Id), Is.True);
            Assert.That(StrategicCellCameraRig.IsLuoyangInfrastructureReview(
                bridge.Id), Is.True);
        }
    }

    public sealed class LuoyangLowFrequencyDefenseProductionV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        private static LuoyangLowFrequencyDefenseProductionKitSource Load(
            out LuoyangFacilityModelCoverageSource coverage,
            out LuoyangGateIdentityKitCatalog gates,
            out LuoyangBuildingPerformancePlanSource performance)
        {
            coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            performance = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            return new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, gates,
                performance.Plan);
        }

        [Test]
        public void Catalog_Reuses14GateIdentitiesAndAddsThreeProceduralProfiles()
        {
            var source = Load(out var coverage, out var gates, out _);
            var catalog = source.Catalog;

            Assert.That(catalog.SchemaId, Is.EqualTo(
                LuoyangLowFrequencyDefenseProductionKitIds.SchemaId));
            Assert.That(catalog.Profiles.Count, Is.EqualTo(5));
            Assert.That(catalog.Profiles.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(28));
            Assert.That(catalog.ProducedOpeningFacilityCount, Is.EqualTo(2023));
            var reuse = catalog.Profiles.Where(item => item.ProductionModeId ==
                LuoyangLowFrequencyDefenseProductionKitIds.IdentityReuseModeId)
                .ToArray();
            var procedural = catalog.Profiles.Where(item =>
                    item.ProductionModeId ==
                    LuoyangLowFrequencyDefenseProductionKitIds.ProceduralModeId)
                .ToArray();
            Assert.That(reuse, Has.Length.EqualTo(2));
            Assert.That(reuse.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(14));
            Assert.That(reuse.All(item => item.Modules.Count == 0 &&
                item.Lod1ModuleIds.Count == 0 &&
                item.Lod2ModuleIds.Count == 0), Is.True);
            Assert.That(reuse.SelectMany(item => item.FacilityIds),
                Is.EquivalentTo(gates.Profiles.Select(item => item.FacilityId)));
            Assert.That(procedural, Has.Length.EqualTo(3));
            Assert.That(procedural.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(14));
            var models = coverage.CombinedCatalog.Models.ToDictionary(
                item => item.ModelId, StringComparer.Ordinal);
            Assert.That(procedural.All(item =>
                new HashSet<string>(item.AvailabilityIds,
                    StringComparer.Ordinal).SetEquals(
                    models[item.ModelId].AvailabilityIds)), Is.True);
            Assert.That(procedural.All(item =>
                item.Lod2ModuleIds.All(item.Lod1ModuleIds.Contains)), Is.True);
        }

        [Test]
        public void Factory_PreservesGateIdentitiesAndBuildsThreeDefenseLodFamilies()
        {
            var defense = Load(out var coverage, out var gates,
                out var performance);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance.Plan);
            var root = new GameObject("Luoyang Defense Factory Tests");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric,
                infrastructure.Catalog, defense.Catalog);
            try
            {
                foreach (var facility in defense.Plan.Facilities)
                {
                    var profile = defense.Catalog.Profiles.Single(item =>
                        item.FacilityIds.Contains(facility.FacilityId));
                    var instance = factory.Create(facility.ModelId, root.transform,
                        facility.FacilityId, facility.CellId64, true);
                    Assert.That(instance.LowFrequencyDefenseProductionReady,
                        Is.True, facility.FacilityId);
                    Assert.That(instance.LowFrequencyDefenseProfileId,
                        Is.EqualTo(profile.ProfileId), facility.FacilityId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), facility.FacilityId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, facility.FacilityId);
                    var reuse = profile.ProductionModeId ==
                        LuoyangLowFrequencyDefenseProductionKitIds
                            .IdentityReuseModeId;
                    Assert.That(instance.GateIdentityReady, Is.EqualTo(reuse),
                        facility.FacilityId);
                    if (reuse)
                    {
                        Assert.That(instance.AssetId,
                            Is.Not.EqualTo(profile.AssetVariantId));
                        Assert.That(instance.LowFrequencyDefenseAssetVariantId,
                            Is.EqualTo(instance.GateIdentityAssetVariantId));
                    }
                    else
                        Assert.That(instance.AssetId,
                            Is.EqualTo(profile.AssetVariantId));
                }

                Assert.That(factory.LowFrequencyDefenseProductionProfileCount,
                    Is.EqualTo(5));
                var batchSignatures = defense.Catalog.Profiles.Where(item =>
                        item.ProductionModeId ==
                        LuoyangLowFrequencyDefenseProductionKitIds
                            .ProceduralModeId)
                    .Select(item => string.Join("|", factory.GetWorldBatchModules(
                        item.ModelId, item.FacilityIds[0]).Select(module =>
                        module.ModuleId))).ToArray();
                Assert.That(batchSignatures.Distinct().Count(), Is.EqualTo(3));

                var buildableGenericGate = factory.Create(
                    HanBuildableFacilityModelIds.CityGate, root.transform,
                    "facility.instance.future.generic_military_gate", 1UL, false);
                Assert.That(buildableGenericGate.GateIdentityReady, Is.False);
                Assert.That(buildableGenericGate
                    .LowFrequencyDefenseProductionReady, Is.True);
                Assert.That(buildableGenericGate.LowFrequencyDefenseRoleId,
                    Is.EqualTo("defense.role.generic_military_gate"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ActualPlan_Freezes28CellsDirectionsAndThreeReviewCameras()
        {
            var defense = Load(out _, out _, out _);
            var plan = defense.Plan;
            var placements =
                LuoyangLowFrequencyDefenseProductionPreviewPlan.Create(
                    GlobalSpatialFoundationV1.CreateCellGrid(), plan);

            Assert.That(plan.Facilities.Count, Is.EqualTo(28));
            Assert.That(placements.Count, Is.EqualTo(28));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(28));
            Assert.That(plan.IdentityReuseCount, Is.EqualTo(14));
            Assert.That(plan.ProceduralCount, Is.EqualTo(14));
            Assert.That(plan.Facilities.Where(item => item.FacilityDefinitionId ==
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .MilitaryGateDefinition)
                .All(item => item.RotationDegrees == 0f &&
                    item.VisualFacing == "south" && item.DirectionBasisId ==
                    LuoyangLowFrequencyDefenseProductionKitIds
                        .GenericGateDefaultFacingPolicyId), Is.True);

            var overview = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangLowFrequencyDefenseOverview);
            var manors = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangDefenseManorGateLine);
            var beacons = StrategicCellCameraRig.Get(
                StrategicCellCameraRig.LuoyangDefenseNorthernBeaconPair);
            Assert.That(overview.Row, Is.EqualTo(1233));
            Assert.That(overview.Column, Is.EqualTo(2045));
            Assert.That(manors.Row, Is.EqualTo(1223));
            Assert.That(manors.Column, Is.EqualTo(2033));
            Assert.That(beacons.Row, Is.EqualTo(1216));
            Assert.That(beacons.Column, Is.EqualTo(2064));
            Assert.That(new[] { overview.Id, manors.Id, beacons.Id }.All(
                StrategicCellCameraRig.IsLuoyangLowFrequencyDefenseReview),
                Is.True);
        }
    }

    public sealed class LuoyangResourceAndAgricultureProductionV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        private static LuoyangResourceAgricultureProductionKitSource Load(
            out LuoyangFacilityModelCoverageSource coverage,
            out LuoyangBuildingPerformancePlanSource performance)
        {
            coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            performance = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            return new LuoyangResourceAgricultureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance.Plan);
        }

        [Test]
        public void Catalog_FreezesFourProfilesPermissionsAnd2049Coverage()
        {
            var source = Load(out var coverage, out _);
            var catalog = source.Catalog;
            Assert.That(catalog.SchemaId, Is.EqualTo(
                LuoyangResourceAgricultureProductionKitIds.SchemaId));
            Assert.That(catalog.Profiles.Count, Is.EqualTo(4));
            Assert.That(catalog.Profiles.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(26));
            Assert.That(catalog.ProducedOpeningFacilityCount, Is.EqualTo(2049));
            Assert.That(catalog.Profiles.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(4));
            var models = coverage.CombinedCatalog.Models.ToDictionary(
                item => item.ModelId, StringComparer.Ordinal);
            foreach (var profile in catalog.Profiles)
            {
                Assert.That(profile.AvailabilityIds,
                    Is.EquivalentTo(models[profile.ModelId].AvailabilityIds),
                    profile.ProfileId);
                Assert.That(profile.Lod2ModuleIds.All(
                    profile.Lod1ModuleIds.Contains), Is.True,
                    profile.ProfileId);
                Assert.That(profile.EvidenceBasisId, Is.EqualTo(
                    LuoyangResourceAgricultureProductionKitIds.EvidenceBasisId));
            }
            var shared = catalog.Profiles.Where(item => item.ModelId ==
                LuoyangFacilityModelCoverageIds.MineQuarry).ToArray();
            Assert.That(shared, Has.Length.EqualTo(2));
            Assert.That(shared.Select(item => item.FacilityDefinitionId),
                Is.EquivalentTo(new[]
                {
                    LuoyangResourceAgricultureProductionKitIds.MineDefinition,
                    LuoyangResourceAgricultureProductionKitIds.QuarryDefinition
                }));
            Assert.That(shared.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Factory_BuildsFourLodFamiliesAndNeverGuessesSharedModel()
        {
            var resource = Load(out var coverage, out var performance);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance.Plan);
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, gates, performance.Plan);
            var root = new GameObject("Luoyang Resource Factory Tests");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric,
                infrastructure.Catalog, defense.Catalog, resource.Catalog);
            try
            {
                var signatures = new HashSet<string>(StringComparer.Ordinal);
                foreach (var profile in resource.Catalog.Profiles)
                {
                    var instance = factory.Create(profile.ModelId, root.transform,
                        profile.FacilityDefinitionId, 1UL, true);
                    Assert.That(instance.ResourceAgricultureProductionReady,
                        Is.True, profile.ProfileId);
                    Assert.That(instance.ResourceAgricultureProfileId,
                        Is.EqualTo(profile.ProfileId), profile.ProfileId);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(profile.AssetVariantId), profile.ProfileId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), profile.ProfileId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, profile.ProfileId);
                    var lod0 = instance.transform.Find(
                        "LOD0.resource-agriculture");
                    Assert.That(lod0, Is.Not.Null, profile.ProfileId);
                    var signature = string.Join("|", lod0.Cast<Transform>()
                        .Select(item => item.name + ":" +
                            item.localPosition.ToString("F3") + ":" +
                            item.localScale.ToString("F3")));
                    Assert.That(signatures.Add(signature), Is.True,
                        profile.ProfileId);
                    var batch = factory.GetWorldBatchModules(profile.ModelId,
                        profile.FacilityDefinitionId);
                    Assert.That(batch.Select(item => item.ModuleId),
                        Is.EquivalentTo(profile.Lod2ModuleIds));
                }
                Assert.That(factory.ResourceAgricultureProductionProfileCount,
                    Is.EqualTo(4));
                Assert.That(signatures.Count, Is.EqualTo(4));

                var unknown = factory.Create(
                    LuoyangFacilityModelCoverageIds.MineQuarry, root.transform,
                    "facility.instance.future.unknown_extract", 2UL, false);
                Assert.That(unknown.ResourceAgricultureProductionReady,
                    Is.False);
                Assert.That(unknown.AssetId,
                    Is.EqualTo(LuoyangFacilityModelCoverageIds.MineQuarryAsset));
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ActualPlan_Freezes26CellsAndFourReviewCameras()
        {
            var resource = Load(out _, out _);
            var plan = resource.Plan;
            var placements =
                LuoyangResourceAgricultureProductionPreviewPlan.Create(
                    GlobalSpatialFoundationV1.CreateCellGrid(), plan);
            Assert.That(plan.Facilities.Count, Is.EqualTo(26));
            Assert.That(placements.Count, Is.EqualTo(26));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(26));
            Assert.That(plan.Facilities.Min(item => item.GridColumn),
                Is.EqualTo(2030));
            Assert.That(plan.Facilities.Max(item => item.GridColumn),
                Is.EqualTo(2060));
            Assert.That(plan.Facilities.Min(item => item.GridRow),
                Is.EqualTo(1228));
            Assert.That(plan.Facilities.Max(item => item.GridRow),
                Is.EqualTo(1256));

            var presets = new[]
            {
                StrategicCellCameraRig.Get(StrategicCellCameraRig
                    .LuoyangResourceAgricultureOverview),
                StrategicCellCameraRig.Get(StrategicCellCameraRig
                    .LuoyangResourceExtractionLine),
                StrategicCellCameraRig.Get(StrategicCellCameraRig
                    .LuoyangSouthernQuarryTerraces),
                StrategicCellCameraRig.Get(StrategicCellCameraRig
                    .LuoyangRicePaddyBand)
            };
            Assert.That(presets.Select(item => item.Id).All(
                StrategicCellCameraRig.IsLuoyangResourceAgricultureReview),
                Is.True);
            Assert.That(presets.Select(item => item.Row),
                Is.EqualTo(new[] { 1244, 1253, 1228, 1256 }));
            Assert.That(presets.Select(item => item.Column),
                Is.EqualTo(new[] { 2045, 2047, 2031, 2054 }));
        }
    }

    public sealed class LuoyangFinalCivicRitualMedicalProductionClosureV1Tests
    {
        private static string WorldMapRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap");

        private static LuoyangFinalCivicRitualMedicalProductionKitSource Load(
            out LuoyangFacilityModelCoverageSource coverage,
            out LuoyangHistoricalLandmarkKitCatalog landmarks,
            out LuoyangBuildingPerformancePlanSource performance)
        {
            coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            landmarks = new LuoyangHistoricalLandmarkKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            performance = new LuoyangBuildingPerformancePlanSource(WorldMapRoot,
                coverage.Bindings, coverage.CombinedCatalog);
            return new LuoyangFinalCivicRitualMedicalProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, landmarks,
                performance.Plan);
        }

        [Test]
        public void Catalog_FreezesTwelveProfilesTenLandmarksAndFiveProceduralAssets()
        {
            var source = Load(out var coverage, out var landmarks, out _);
            var catalog = source.Catalog;
            Assert.That(catalog.SchemaId, Is.EqualTo(
                LuoyangFinalCivicRitualMedicalProductionKitIds.SchemaId));
            Assert.That(catalog.Profiles.Count, Is.EqualTo(12));
            Assert.That(catalog.Profiles.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(35));
            Assert.That(catalog.ProducedOpeningFacilityCount, Is.EqualTo(2084));
            Assert.That(catalog.Profiles.Where(item => item.ProductionModeId ==
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId).Sum(item => item.OpeningUsageCount),
                Is.EqualTo(10));
            var procedural = catalog.Profiles.Where(item =>
                item.ProductionModeId ==
                LuoyangFinalCivicRitualMedicalProductionKitIds.ProceduralModeId)
                .ToArray();
            Assert.That(procedural, Has.Length.EqualTo(5));
            Assert.That(procedural.Sum(item => item.OpeningUsageCount),
                Is.EqualTo(25));
            Assert.That(procedural.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(5));

            var models = coverage.CombinedCatalog.Models.ToDictionary(
                item => item.ModelId, StringComparer.Ordinal);
            var landmarksById = landmarks.Profiles.ToDictionary(
                item => item.FacilityId, StringComparer.Ordinal);
            foreach (var profile in catalog.Profiles)
            {
                if (profile.ProductionModeId ==
                    LuoyangFinalCivicRitualMedicalProductionKitIds.ProceduralModeId)
                {
                    Assert.That(profile.AvailabilityIds,
                        Is.EquivalentTo(models[profile.ModelId].AvailabilityIds),
                        profile.ProfileId);
                    Assert.That(profile.Lod2ModuleIds.All(
                        profile.Lod1ModuleIds.Contains), Is.True,
                        profile.ProfileId);
                }
                else
                    foreach (var id in profile.FacilityIds)
                    {
                        Assert.That(profile.AvailabilityIds,
                            Is.EquivalentTo(landmarksById[id].AvailabilityIds),
                            id);
                        Assert.That(profile.ModelId,
                            Is.EqualTo(landmarksById[id].BaseModelId), id);
                    }
            }

            var ritual = catalog.Profiles.Where(item => item.ModelId ==
                LuoyangFacilityModelCoverageIds.RitualHall).ToArray();
            Assert.That(ritual, Has.Length.EqualTo(2));
            Assert.That(ritual.Select(item => item.ProductionModeId),
                Is.EquivalentTo(new[]
                {
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .IdentityReuseModeId,
                    LuoyangFinalCivicRitualMedicalProductionKitIds
                        .ProceduralModeId
                }));
            var publicSpace = procedural.Where(item => item.ModelId ==
                LuoyangFacilityModelCoverageIds.Plaza).ToArray();
            Assert.That(publicSpace, Has.Length.EqualTo(2));
            Assert.That(publicSpace.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Factory_ReusesTenLandmarksBuildsFiveLodFamiliesAndKeepsSharedModelsAmbiguous()
        {
            var finalCivic = Load(out var coverage, out var landmarks,
                out var performance);
            var production = new LuoyangProductionBuildingKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                coverage.CombinedCatalog).Catalog;
            var fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, coverage.CombinedCatalog).Catalog;
            var infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance.Plan);
            var defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, gates, performance.Plan);
            var resource = new LuoyangResourceAgricultureProductionKitSource(
                WorldMapRoot, coverage.CombinedCatalog, performance.Plan);
            var root = new GameObject("Luoyang Final Civic Factory Tests");
            var factory = new HanBuildableFacilityModelFactory(
                coverage.CombinedCatalog, production, landmarks, gates, fabric,
                infrastructure.Catalog, defense.Catalog, resource.Catalog,
                finalCivic.Catalog);
            try
            {
                foreach (var facility in finalCivic.Plan.Facilities)
                {
                    var instance = factory.Create(facility.ModelId,
                        root.transform, facility.FacilityId,
                        facility.CellId64, true);
                    Assert.That(instance.FinalCivicProductionReady, Is.True,
                        facility.FacilityId);
                    Assert.That(instance.FinalCivicProfileId,
                        Is.EqualTo(facility.ProfileId), facility.FacilityId);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(facility.AssetVariantId), facility.FacilityId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), facility.FacilityId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, facility.FacilityId);
                    if (facility.ProductionModeId ==
                        LuoyangFinalCivicRitualMedicalProductionKitIds
                            .IdentityReuseModeId)
                        Assert.That(instance.HistoricalLandmarkReady, Is.True,
                            facility.FacilityId);
                    else
                        Assert.That(instance.transform.Find("LOD0.final-civic"),
                            Is.Not.Null, facility.FacilityId);

                    var batch = factory.GetWorldBatchModules(facility.ModelId,
                        facility.FacilityId);
                    Assert.That(batch, Is.Not.Empty, facility.FacilityId);
                }
                Assert.That(factory.FinalCivicProductionProfileCount,
                    Is.EqualTo(12));

                var unknownPlaza = factory.Create(
                    LuoyangFacilityModelCoverageIds.Plaza, root.transform,
                    "facility.instance.future.unknown_public_space", 2UL, false);
                Assert.That(unknownPlaza.FinalCivicProductionReady, Is.False);
                Assert.That(unknownPlaza.AssetId,
                    Is.EqualTo(LuoyangFacilityModelCoverageIds.PlazaAsset));
                var unknownRitual = factory.Create(
                    LuoyangFacilityModelCoverageIds.RitualHall, root.transform,
                    "facility.instance.future.unknown_ritual", 3UL, false);
                Assert.That(unknownRitual.FinalCivicProductionReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ActualPlan_Freezes35CellsFullCoverageAndFourReviewCameras()
        {
            var source = Load(out _, out _, out _);
            var plan = source.Plan;
            var placements =
                LuoyangFinalCivicRitualMedicalProductionPreviewPlan.Create(
                    GlobalSpatialFoundationV1.CreateCellGrid(), plan);
            Assert.That(plan.Facilities.Count, Is.EqualTo(35));
            Assert.That(placements.Count, Is.EqualTo(35));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(35));
            Assert.That(plan.IdentityReuseCount, Is.EqualTo(10));
            Assert.That(plan.ProceduralCount, Is.EqualTo(25));
            Assert.That(plan.Facilities.Min(item => item.GridColumn),
                Is.EqualTo(2024));
            Assert.That(plan.Facilities.Max(item => item.GridColumn),
                Is.EqualTo(2064));
            Assert.That(plan.Facilities.Min(item => item.GridRow),
                Is.EqualTo(1210));
            Assert.That(plan.Facilities.Max(item => item.GridRow),
                Is.EqualTo(1264));

            var presets = new[]
            {
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangFinalCivicOverview),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangClinicLine),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangRitualHallLine),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangPublicCivicCluster)
            };
            Assert.That(presets.Select(item => item.Id).All(
                StrategicCellCameraRig.IsLuoyangFinalCivicReview), Is.True);
            Assert.That(presets.Select(item => item.Row),
                Is.EqualTo(new[] { 1237, 1255, 1237, 1241 }));
            Assert.That(presets.Select(item => item.Column),
                Is.EqualTo(new[] { 2044, 2050, 2044, 2041 }));
        }
    }

    public sealed class LuoyangWholeCityFinalAssetReviewV1Tests
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
            public LuoyangBuildingPerformancePlan Performance;
            public LuoyangInfrastructureProductionKitCatalog Infrastructure;
            public LuoyangLowFrequencyDefenseProductionKitCatalog Defense;
            public LuoyangResourceAgricultureProductionKitCatalog Resources;
            public LuoyangFinalCivicRitualMedicalProductionKitCatalog FinalCivic;
            public LuoyangFinalAssetReviewManifestSource Review;
        }

        private static Fixture Load()
        {
            var result = new Fixture();
            result.Coverage = new LuoyangFacilityModelCoverageSource(WorldMapRoot);
            result.Production = new LuoyangProductionBuildingKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog).Catalog;
            result.Landmarks = new LuoyangHistoricalLandmarkKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog).Catalog;
            result.Gates = new LuoyangGateIdentityKitSource(WorldMapRoot,
                result.Coverage.CombinedCatalog).Catalog;
            result.Fabric = new LuoyangMediumFrequencyUrbanFabricKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog).Catalog;
            result.Performance = new LuoyangBuildingPerformancePlanSource(
                WorldMapRoot, result.Coverage.Bindings,
                result.Coverage.CombinedCatalog).Plan;
            result.Infrastructure = new LuoyangInfrastructureProductionKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog,
                result.Performance).Catalog;
            result.Defense = new LuoyangLowFrequencyDefenseProductionKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog, result.Gates,
                result.Performance).Catalog;
            result.Resources = new LuoyangResourceAgricultureProductionKitSource(
                WorldMapRoot, result.Coverage.CombinedCatalog,
                result.Performance).Catalog;
            result.FinalCivic =
                new LuoyangFinalCivicRitualMedicalProductionKitSource(
                    WorldMapRoot, result.Coverage.CombinedCatalog,
                    result.Landmarks, result.Performance).Catalog;
            result.Review = new LuoyangFinalAssetReviewManifestSource(WorldMapRoot,
                result.Production, result.Landmarks, result.Gates,
                result.Fabric, result.Infrastructure, result.Defense,
                result.Resources, result.FinalCivic, result.Performance);
            return result;
        }

        [Test]
        public void Manifest_FreezesFiftyFourReplacementSlotsAndAll2084Facilities()
        {
            var fixture = Load();
            var catalog = fixture.Review.Catalog;
            Assert.That(catalog.SchemaId,
                Is.EqualTo(LuoyangFinalAssetReviewIds.SchemaId));
            Assert.That(catalog.ManifestId,
                Is.EqualTo(LuoyangFinalAssetReviewIds.ManifestId));
            Assert.That(catalog.AuditGroups, Has.Count.EqualTo(9));
            Assert.That(catalog.Items, Has.Count.EqualTo(54));
            Assert.That(catalog.Items.Select(item => item.AssetVariantId)
                .Distinct().Count(), Is.EqualTo(54));
            Assert.That(catalog.Items.Sum(item => item.FacilityUsageCount),
                Is.EqualTo(2084));
            Assert.That(fixture.Review.Plan.FacilityAssetVariants,
                Has.Count.EqualTo(2084));
            Assert.That(fixture.Review.Plan.FacilityAssetVariants.Values
                .Distinct().Count(), Is.EqualTo(54));
            Assert.That(catalog.Items.Any(item =>
                item.AssetVariantId.StartsWith("REUSE_",
                    StringComparison.Ordinal)), Is.False,
                "Reuse placeholders are not actual runtime replacement slots.");
            Assert.That(catalog.AuditGroups.All(item =>
                item.MaterialReadinessScore == 1), Is.True);
            Assert.That(catalog.Items.Count(item => item.PriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP0), Is.EqualTo(24));
            Assert.That(catalog.Items.Count(item => item.PriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP1), Is.EqualTo(10));
            Assert.That(catalog.Items.Count(item => item.PriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP2), Is.EqualTo(14));
            Assert.That(catalog.Items.Count(item => item.PriorityId ==
                    LuoyangFinalAssetReviewIds.PriorityP3), Is.EqualTo(6));
        }

        [Test]
        public void Factory_ResolvesEveryRepresentativeToItsStableReplacementSlot()
        {
            var fixture = Load();
            var root = new GameObject("Luoyang Final Asset Review Factory Tests");
            var factory = new HanBuildableFacilityModelFactory(
                fixture.Coverage.CombinedCatalog, fixture.Production,
                fixture.Landmarks, fixture.Gates, fixture.Fabric,
                fixture.Infrastructure, fixture.Defense, fixture.Resources,
                fixture.FinalCivic);
            try
            {
                foreach (var item in fixture.Review.Catalog.Items)
                {
                    var instance = factory.Create(item.ModelId, root.transform,
                        item.RepresentativeFacilityId,
                        item.RepresentativeCellId64, true);
                    Assert.That(instance.AssetId,
                        Is.EqualTo(item.ReplacementSlotId), item.ItemId);
                    Assert.That(instance.ModelId, Is.EqualTo(item.ModelId),
                        item.ItemId);
                    Assert.That(instance.GetComponent<LODGroup>()?.GetLODs(),
                        Has.Length.EqualTo(3), item.ItemId);
                    Assert.That(instance.GetComponentsInChildren<Collider>(true),
                        Is.Empty, item.ItemId);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                factory.Dispose();
            }
        }

        [Test]
        public void ReviewBoard_Uses54PreviewCellsAndFourFixedPriorityCameras()
        {
            var fixture = Load();
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var placements = LuoyangFinalAssetReviewPreviewPlan.Create(grid,
                fixture.Review.Plan,
                LuoyangFinalAssetReviewPreviewPlan.BoardCenterRow,
                LuoyangFinalAssetReviewPreviewPlan.BoardCenterColumn);
            Assert.That(placements, Has.Count.EqualTo(54));
            Assert.That(placements.Select(item => item.CellId.Value)
                .Distinct().Count(), Is.EqualTo(54));
            Assert.That(placements.Select(item => item.RuntimeBindingId)
                .Distinct().Count(), Is.EqualTo(54));

            var presets = new[]
            {
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewAll),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP0),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP1),
                StrategicCellCameraRig.Get(
                    StrategicCellCameraRig.LuoyangFinalAssetReviewP2P3)
            };
            Assert.That(presets.Select(item => item.Id).All(
                StrategicCellCameraRig.IsLuoyangFinalAssetReview), Is.True);
            Assert.That(presets.Select(item => item.Row),
                Is.EqualTo(new[] { 1243, 1237, 1243, 1249 }));
            Assert.That(presets.Select(item => item.Column),
                Is.All.EqualTo(2043));
        }
    }
}
