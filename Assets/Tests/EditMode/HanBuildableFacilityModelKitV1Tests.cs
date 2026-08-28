using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Presentation;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class HanBuildableFacilityModelKitV1Tests
    {
        private static string CatalogRoot => Path.Combine(Application.dataPath,
            "StreamingAssets", "WorldMap", "HanBuildableFacilityModelKitV1");

        [Test]
        public void Catalog_FreezesExactlyTheAcceptedSevenBuildableModels()
        {
            var catalog = new HanBuildableFacilityModelCatalogSource(CatalogRoot).Catalog;

            Assert.That(catalog.SchemaId,
                Is.EqualTo(HanBuildableFacilityModelCatalogRules.SchemaId));
            Assert.That(catalog.Models, Has.Count.EqualTo(7));
            Assert.That(catalog.Models.Select(value => value.ModelId),
                Is.EquivalentTo(HanBuildableFacilityModelIds.AllModelIds));
            Assert.That(catalog.Models.Select(value => value.AssetId).Distinct().Count(),
                Is.EqualTo(7));
            Assert.That(catalog.Models.All(value => value.StrategicFootprintRatio <= 0.90f),
                Is.True);
            Assert.That(catalog.Models.Any(value =>
                value.ModelId.Contains("nangong")), Is.False,
                "Nangong is historical-only and must not enter the buildable model kit.");
        }

        [Test]
        public void PreviewPlan_UsesSevenDistinctFrozenGlobalCellsWithoutSubCells()
        {
            var grid = GlobalSpatialFoundationV1.CreateCellGrid();
            var plan = HanBuildableFacilityPreviewPlan.Create(grid, 1241, 2043);

            Assert.That(plan, Has.Count.EqualTo(7));
            Assert.That(plan.Select(value => value.CellId.Value).Distinct().Count(),
                Is.EqualTo(7));
            Assert.That(plan.Select(value => value.ModelId),
                Is.EquivalentTo(HanBuildableFacilityModelIds.AllModelIds));
            Assert.That(plan.All(value => grid.TryDecode(value.CellId,
                out _, out _)), Is.True);
            Assert.That(ExplicitStrategicCellMapV1.CreatesSimulationSubCells, Is.False);
        }

        [Test]
        public void Factory_CreatesEveryModelFromSharedPaletteWithoutPhysicsColliders()
        {
            var catalog = new HanBuildableFacilityModelCatalogSource(CatalogRoot).Catalog;
            var parent = new GameObject("Han Buildable Facility Model Test Root");
            var factory = new HanBuildableFacilityModelFactory(catalog);
            try
            {
                Assert.That(factory.ModelCount, Is.EqualTo(7));
                Assert.That(factory.MaterialCount, Is.EqualTo(catalog.Materials.Count));
                foreach (var model in catalog.Models)
                {
                    var instance = factory.Create(model.ModelId, parent.transform,
                        "test." + model.ModelId, 100, true);
                    Assert.That(instance.AssetId, Is.EqualTo(model.AssetId));
                    Assert.That(instance.PreviewOnly, Is.True);
                    Assert.That(instance.GetComponentsInChildren<Renderer>(),
                        Has.Length.EqualTo(model.Modules.Count));
                    Assert.That(instance.GetComponentsInChildren<Collider>(), Is.Empty);
                    AssertWithinStrategicFootprint(instance,
                        model.StrategicFootprintRatio + 0.08f);
                }
                var uniqueMaterials = parent.GetComponentsInChildren<Renderer>()
                    .Select(value => value.sharedMaterial).Distinct().Count();
                Assert.That(uniqueMaterials, Is.LessThanOrEqualTo(factory.MaterialCount));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                factory.Dispose();
            }
        }

        [Test]
        public void VisualProfiles_BindTheSevenAcceptedRuntimeAssetIds()
        {
            var system = new LuoyangVisualPresentationSystem();
            var expected = new Dictionary<string, string>
            {
                ["facility.residential.urban_quarter"] =
                    HanBuildableFacilityModelIds.ResidenceAsset,
                ["facility.storage.warehouse"] =
                    HanBuildableFacilityModelIds.WarehouseAsset,
                ["facility.industry.workshop"] =
                    HanBuildableFacilityModelIds.WorkshopAsset,
                ["facility.commercial.market"] =
                    HanBuildableFacilityModelIds.MarketAsset,
                ["military_rear_medical_site.field_hospital"] =
                    HanBuildableFacilityModelIds.FieldHospitalAsset,
                ["facility.fortification.city_wall"] =
                    HanBuildableFacilityModelIds.CityWallAsset,
                ["facility.fortification.city_gate"] =
                    HanBuildableFacilityModelIds.CityGateAsset
            };

            foreach (var pair in expected)
                Assert.That(system.ResolveProfile(pair.Key).MainAssetId,
                    Is.EqualTo(pair.Value), pair.Key);
            var medical = system.ResolveProfile(
                "military_rear_medical_site.field_hospital");
            Assert.That(medical.Availability.HasFlag(BuildAvailability.Military), Is.True);
            Assert.That(medical.Availability.HasFlag(BuildAvailability.Player), Is.True);
        }

        private static void AssertWithinStrategicFootprint(
            HanBuildableFacilityModelInstance instance, float maximumSize)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(maximumSize),
                instance.ModelId + " X footprint");
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(maximumSize),
                instance.ModelId + " Z footprint");
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.001f),
                instance.ModelId + " ground plane");
        }
    }
}
