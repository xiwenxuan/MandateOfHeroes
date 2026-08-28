using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    [Flags]
    public enum BuildAvailability : ushort
    {
        None = 0,
        Player = 1 << 0,
        Ai = 1 << 1,
        Family = 1 << 2,
        Government = 1 << 3,
        Military = 1 << 4,
        HistoricalInit = 1 << 5,
        Event = 1 << 6
    }

    public enum FacilityVisualImportance : byte { A, B, C }
    public enum FacilityRuntimeVisualState : byte
    {
        Active, Idle, Working, WaitingInput, Damaged, Destroyed,
        Abandoned, UnderConstruction
    }
    public enum ConstructionVisualStage : byte
    {
        Ghost, SitePreparation, Foundation, Frame, Structure, Finishing,
        Complete, Damaged, Ruin
    }
    public enum MapVisualLod : byte { World, Region, City, Close }
    public enum CropVisualStage : byte
    {
        Sown, Seedling, Growing, Harvestable80, Mature, Harvested, Fallow
    }

    [Serializable]
    public sealed class BuildMaterialRequirement
    {
        public string ProductId;
        public long QuantityMilliunits;
    }

    [Serializable]
    public sealed class BuildBlueprintDefinition
    {
        public string BlueprintId;
        public string FacilityDefinitionId;
        public string VisualProfileId;
        public List<string> AllowedTerrain = new List<string>();
        public string AllowedCellConditionId;
        public string AuthorityRequirementId;
        public string OwnershipRequirementId;
        public List<BuildMaterialRequirement> RequiredMaterials =
            new List<BuildMaterialRequirement>();
        public long RequiredMoney;
        public int RequiredWorkers;
        public int ConstructionDays;
        public List<ConstructionVisualStage> ConstructionStages =
            new List<ConstructionVisualStage>();
        public BuildAvailability Availability;
        public string RegionalStyleId;
        public string HistoricalRestrictionId;
    }

    [Serializable]
    public sealed class FacilityVisualProfile
    {
        public string VisualProfileId;
        public string FacilityTypeId;
        public string RegionalStyleId;
        public string ScaleProfileId;
        public string MainAssetId;
        public string ModularKitId;
        public string DecorationSetId;
        public string WallSetId;
        public string RoofSetId;
        public string GateSetId;
        public string PropSetId;
        public string VegetationSetId;
        public string DamageVisualId;
        public string RuinVisualId;
        public string LodProfileId;
        public int CrowdAnchorCount;
        public int WorkerAnchorCount;
        public int VehicleAnchorCount;
        public int ProductionEffectAnchorCount;
        public FacilityVisualImportance Importance;
        public bool ReusableConstructionAsset;
        public BuildAvailability Availability;
    }

    [Serializable]
    public sealed class FacilityVisualAnchor
    {
        public string FacilityId;
        public ulong CellId64;
        public string VisualProfileId;
        public float LocalX;
        public float LocalY;
        public float LocalZ;
        public float RotationDegrees;
        public float Scale = 1f;
        public string VisualFootprintProfileId;
        public string EntranceAnchorId;
        public string RoadConnectionAnchorId;
    }

    [Serializable]
    public sealed class PersonVisualRepresentation
    {
        public uint PersonOrdinal;
        public string RuntimePersonId;
        public string FacilityId;
        public float LocalX;
        public float LocalY;
        public int Priority;
        public bool HistoricalPerson;
    }

    [Serializable]
    public sealed class ShipmentVisualRepresentation
    {
        public string ShipmentId;
        public string RouteId;
        public string ProductId;
        public long CargoMilliunits;
        public float Progress01;
        public int RepresentativeVehicleCount;
    }

    [Serializable]
    public sealed class RuntimeVisualSpline
    {
        public string VisualSplineId;
        public string RuntimeBindingId;
        public string KindId;
        public float Width;
        public List<VisualSplinePoint> Points = new List<VisualSplinePoint>();
    }

    [Serializable]
    public sealed class VisualSplinePoint
    {
        public float X;
        public float Y;
    }

    public static class LuoyangVisualPresentationRules
    {
        public static ConstructionVisualStage ResolveConstructionStage(
            LuoyangCompactConstructionProjectState project, long absoluteDay)
        {
            if (project == null) return ConstructionVisualStage.Ghost;
            if (project.Cancelled) return ConstructionVisualStage.Ruin;
            if (project.Completed) return ConstructionVisualStage.Complete;
            var duration = Math.Max(1, project.CompletionDay - project.StartedDay);
            var progress = Math.Max(0, Math.Min(duration,
                absoluteDay - project.StartedDay)) / (double)duration;
            if (progress < .15) return ConstructionVisualStage.SitePreparation;
            if (progress < .35) return ConstructionVisualStage.Foundation;
            if (progress < .60) return ConstructionVisualStage.Frame;
            if (progress < .85) return ConstructionVisualStage.Structure;
            return ConstructionVisualStage.Finishing;
        }

        public static CropVisualStage ResolveCropStage(LuoyangCropRuntimeState crop)
        {
            if (crop == null) return CropVisualStage.Fallow;
            if (crop.Phase == LuoyangCropPhase.Fallow) return CropVisualStage.Fallow;
            if (crop.Phase == LuoyangCropPhase.Harvested) return CropVisualStage.Harvested;
            if (crop.MaturityBasisPoints >= 10_000) return CropVisualStage.Mature;
            if (crop.MaturityBasisPoints >= crop.EarlyHarvestMinimumBasisPoints)
                return CropVisualStage.Harvestable80;
            if (crop.MaturityBasisPoints >= 3_000) return CropVisualStage.Growing;
            if (crop.MaturityBasisPoints >= 800) return CropVisualStage.Seedling;
            return CropVisualStage.Sown;
        }

        public static FacilityRuntimeVisualState ResolveFacilityState(
            LuoyangFacilityProductionRuntimeState facility,
            LuoyangCompactConstructionProjectState project = null)
        {
            if (project != null && !project.Completed && !project.Cancelled)
                return FacilityRuntimeVisualState.UnderConstruction;
            if (facility == null || facility.ConditionBasisPoints <= 0)
                return FacilityRuntimeVisualState.Abandoned;
            if (facility.ConditionBasisPoints < 2_500)
                return FacilityRuntimeVisualState.Destroyed;
            if (facility.ConditionBasisPoints < 7_000)
                return FacilityRuntimeVisualState.Damaged;
            if (facility.Status == LuoyangProductionRuntimeStatus.InProgress ||
                facility.Status == LuoyangProductionRuntimeStatus.Ready)
                return FacilityRuntimeVisualState.Working;
            if (facility.Status == LuoyangProductionRuntimeStatus.WaitingInput)
                return FacilityRuntimeVisualState.WaitingInput;
            if (facility.Status == LuoyangProductionRuntimeStatus.Idle)
                return FacilityRuntimeVisualState.Idle;
            return FacilityRuntimeVisualState.Active;
        }
    }
}
