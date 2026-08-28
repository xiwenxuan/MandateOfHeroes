using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class HanBuildableFacilityModelIds
    {
        public const string Residence =
            "model.han.buildable.residence.general.v1";
        public const string Warehouse =
            "model.han.buildable.warehouse.general.v1";
        public const string Workshop =
            "model.han.buildable.workshop.general.v1";
        public const string Market =
            "model.han.buildable.market.general.v1";
        public const string FieldHospital =
            "model.han.buildable.field_hospital.timber_leather.v1";
        public const string CityWall =
            "model.han.buildable.city_wall.segment.v1";
        public const string CityGate =
            "model.han.buildable.city_gate.segment.v1";

        public const string ResidenceAsset = "HAN_RES_COMMON_SMALL_A";
        public const string WarehouseAsset = "HAN_STORAGE_A";
        public const string WorkshopAsset = "HAN_PRODUCTION_A";
        public const string MarketAsset = "HAN_MARKET_GENERAL_A";
        public const string FieldHospitalAsset = "HAN_FIELD_HOSPITAL_A";
        public const string CityWallAsset = "HAN_WALL_A";
        public const string CityGateAsset = "HAN_GATE_A";

        public static readonly IReadOnlyList<string> AllModelIds = new[]
        {
            Residence,
            Warehouse,
            Workshop,
            Market,
            FieldHospital,
            CityWall,
            CityGate
        };
    }

    [Serializable]
    public sealed class HanBuildableFacilityModelCatalog
    {
        public string SchemaId;
        public string RegionalStyleId;
        public List<HanBuildableFacilityMaterialDefinition> Materials =
            new List<HanBuildableFacilityMaterialDefinition>();
        public List<HanBuildableFacilityModelDefinition> Models =
            new List<HanBuildableFacilityModelDefinition>();
    }

    [Serializable]
    public sealed class HanBuildableFacilityMaterialDefinition
    {
        public string MaterialId;
        public float Red;
        public float Green;
        public float Blue;
        public float Alpha = 1f;
        public float Metallic;
        public float Smoothness;
    }

    [Serializable]
    public sealed class HanBuildableFacilityModelDefinition
    {
        public string ModelId;
        public string AssetId;
        public string DisplayName;
        public string FacilityDefinitionId;
        public string VisualProfileId;
        public string SourceBuildContractId;
        public string ModularKitId;
        public float StrategicFootprintRatio;
        public List<string> AvailabilityIds = new List<string>();
        public List<HanBuildableFacilityModuleDefinition> Modules =
            new List<HanBuildableFacilityModuleDefinition>();
    }

    [Serializable]
    public sealed class HanBuildableFacilityModuleDefinition
    {
        public string ModuleId;
        public string PrimitiveId;
        public string MaterialId;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;
    }

    public static class HanBuildableFacilityModelCatalogRules
    {
        public const string SchemaId =
            "mandate.han-buildable-facility-model-catalog.v1";
        public const string ModularKitId = "HAN_BUILDING_MODULAR_KIT_V1";

        public static void Validate(HanBuildableFacilityModelCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!string.Equals(catalog.SchemaId, SchemaId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(catalog.RegionalStyleId) ||
                catalog.Materials == null || catalog.Materials.Count == 0 ||
                catalog.Models == null || catalog.Models.Count == 0)
                throw new InvalidOperationException(
                    "Invalid Han buildable Facility model catalog header.");

            var materialIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var material in catalog.Materials)
            {
                if (material == null || string.IsNullOrWhiteSpace(material.MaterialId) ||
                    !materialIds.Add(material.MaterialId) ||
                    !Unit(material.Red) || !Unit(material.Green) ||
                    !Unit(material.Blue) || !Unit(material.Alpha) ||
                    !Unit(material.Metallic) || !Unit(material.Smoothness))
                    throw new InvalidOperationException(
                        "Invalid Han buildable Facility material definition.");
            }

            var modelIds = new HashSet<string>(StringComparer.Ordinal);
            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in catalog.Models)
            {
                if (model == null || string.IsNullOrWhiteSpace(model.ModelId) ||
                    !modelIds.Add(model.ModelId) ||
                    string.IsNullOrWhiteSpace(model.AssetId) ||
                    !assetIds.Add(model.AssetId) ||
                    string.IsNullOrWhiteSpace(model.DisplayName) ||
                    string.IsNullOrWhiteSpace(model.FacilityDefinitionId) ||
                    string.IsNullOrWhiteSpace(model.VisualProfileId) ||
                    string.IsNullOrWhiteSpace(model.SourceBuildContractId) ||
                    !string.Equals(model.ModularKitId, ModularKitId,
                        StringComparison.Ordinal) ||
                    !Finite(model.StrategicFootprintRatio) ||
                    model.StrategicFootprintRatio <= 0f ||
                    model.StrategicFootprintRatio > 0.90f ||
                    model.AvailabilityIds == null ||
                    model.AvailabilityIds.Count == 0 ||
                    model.Modules == null || model.Modules.Count == 0 ||
                    model.Modules.Count > 128)
                    throw new InvalidOperationException(
                        "Invalid Han buildable Facility model definition.");

                var moduleIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var module in model.Modules)
                {
                    if (module == null ||
                        string.IsNullOrWhiteSpace(module.ModuleId) ||
                        !moduleIds.Add(module.ModuleId) ||
                        (module.PrimitiveId != "cube" &&
                         module.PrimitiveId != "cylinder") ||
                        !materialIds.Contains(module.MaterialId) ||
                        !Finite(module.PositionX) || !Finite(module.PositionY) ||
                        !Finite(module.PositionZ) || !Finite(module.RotationX) ||
                        !Finite(module.RotationY) || !Finite(module.RotationZ) ||
                        !Finite(module.ScaleX) || !Finite(module.ScaleY) ||
                        !Finite(module.ScaleZ) || module.ScaleX <= 0f ||
                        module.ScaleY <= 0f || module.ScaleZ <= 0f ||
                        module.ScaleX > 1f || module.ScaleY > 1f ||
                        module.ScaleZ > 1f || module.PositionY < 0f)
                        throw new InvalidOperationException(
                            "Invalid Han buildable Facility model module.");
                    var halfFootprint = model.StrategicFootprintRatio * 0.5f;
                    if (Math.Abs(module.PositionX) + module.ScaleX * 0.5f >
                            halfFootprint + 0.0001f ||
                        Math.Abs(module.PositionZ) + module.ScaleZ * 0.5f >
                            halfFootprint + 0.0001f)
                        throw new InvalidOperationException(
                            "Han buildable Facility module exceeds its single-Cell footprint.");
                }
            }
        }

        private static bool Unit(float value) => Finite(value) &&
            value >= 0f && value <= 1f;

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
