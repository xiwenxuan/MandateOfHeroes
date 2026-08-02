using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum InventoryTransactionType : byte
    {
        LegacyBalanceConverted,
        Reserved,
        ReservationReleased,
        RecipeSettled,
        OpeningBalance,
        MilitaryProcurementDispatched,
        EquipmentRepairReserved,
        EquipmentRepairSettled,
        ResourceExtractionSettled
    }

    [Serializable]
    public sealed class InventoryContainerState
    {
        public string Id;
        public string KindId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string CarrierPersonId;
        public string LocationId;
        public long CapacityWeight;
    }

    [Serializable]
    public sealed class ProductionSiteState
    {
        public string Id;
        public string KindId;
        public string OwnerOrganizationId;
        public string LocationId;
        public string ManagerPersonId;
        public string InventoryContainerId;
        public int ConcurrentOrderCapacity = 1;
        public int ConditionBasisPoints = 10_000;
        public List<string> FacilityTags = new List<string>();
    }

    [Serializable]
    public sealed class ProductQualityDimensionState
    {
        public string QualityDimensionId;
        public int ValueBasisPoints;
    }

    [Serializable]
    public sealed class ProductBatchState
    {
        public string Id;
        public string ProductDefinitionId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string StorageFacilityId;
        public string InventoryContainerId;
        public string OriginLocationId;
        public string SourceWorkOrderId;
        public string SourceTransactionId;
        public string CropVarietyDefinitionId;
        public string UnitId;
        public int UnitWeight = 1;
        public long ProducedDay;
        public long Quantity;
        public long ReservedQuantity;
        public int QualityBasisPoints = 10_000;
        public int FreshnessBasisPoints = 10_000;
        public int SeedVigorBasisPoints;
        public int SeedPurityBasisPoints;
        public List<ProductQualityDimensionState> QualityDimensions =
            new List<ProductQualityDimensionState>();
    }

    [Serializable]
    public sealed class InventoryTransactionLineState
    {
        public string BatchId;
        public string ProductDefinitionId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string StorageFacilityId;
        public string InventoryContainerId;
        public string UnitId;
        public long QuantityDelta;
        public long ReservedQuantityDelta;
    }

    [Serializable]
    public sealed class InventoryTransactionState
    {
        public string Id;
        public long Day;
        public InventoryTransactionType Type;
        public string ActorPersonId;
        public string SourceWorkOrderId;
        public string SourceMilitaryProcurementId;
        public string SourceEquipmentRepairOrderId;
        public string SourceResourceExtractionOrderId;
        public long LegacyFamilyGrainDelta;
        public long LegacyFamilySeedGrainDelta;
        public long FacilityInventoryDelta;
        public string Summary;
        public List<InventoryTransactionLineState> Lines =
            new List<InventoryTransactionLineState>();
    }

    [Serializable]
    public sealed class BatchReservationState
    {
        public string BatchId;
        public long Quantity;
    }

    [Serializable]
    public sealed class ProcessingWorkOrderState
    {
        public string Id;
        public string RecipeDefinitionId;
        public string MethodDefinitionId;
        public string OwnerFamilyId;
        public string StorageFacilityId;
        public string OwnerOrganizationId;
        public string ProductionSiteId;
        public string InventoryContainerId;
        public string ManagerPersonId;
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long FinishDay;
        public long SettledDay = -1;
        public int RunCount;
        public bool PracticeTrackingEnabled;
        public string PracticeSkillDefinitionId;
        public int ManagerSkillBasisPointsAtStart;
        public int PracticeGainBasisPoints;
        public int OutputQualityBasisPoints;
        public List<BatchReservationState> InputReservations =
            new List<BatchReservationState>();
        public List<string> OutputBatchIds = new List<string>();
    }

    [Serializable]
    public sealed class ProductionPracticeLedgerEntryState
    {
        public string Id;
        public long Day;
        public string ProcessingWorkOrderId;
        public string PersonId;
        public string SkillDefinitionId;
        public int MasteryBeforeBasisPoints;
        public int MasteryAfterBasisPoints;
        public int GainBasisPoints;
        public int OutputQualityBasisPoints;
        public string Summary;
    }

    public static class ProductQualityRules
    {
        public static List<ProductQualityDimensionState> CreateUniform(
            ProductDefinition product,
            int valueBasisPoints)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (valueBasisPoints < 0 || valueBasisPoints > 10_000 ||
                product.QualityDimensionIds == null ||
                product.QualityDimensionIds.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valueBasisPoints));
            }

            var result = new List<ProductQualityDimensionState>(
                product.QualityDimensionIds.Count);
            for (var i = 0; i < product.QualityDimensionIds.Count; i++)
            {
                result.Add(new ProductQualityDimensionState
                {
                    QualityDimensionId = product.QualityDimensionIds[i],
                    ValueBasisPoints = valueBasisPoints
                });
            }

            return result;
        }

        public static int CalculateSummary(
            IList<ProductQualityDimensionState> dimensions)
        {
            if (dimensions == null || dimensions.Count == 0)
            {
                throw new InvalidOperationException(
                    "A product batch must have quality dimensions.");
            }

            long total = 0;
            for (var i = 0; i < dimensions.Count; i++)
            {
                var dimension = dimensions[i] ??
                    throw new InvalidOperationException(
                        "A product quality dimension cannot be null.");
                if (dimension.ValueBasisPoints < 0 ||
                    dimension.ValueBasisPoints > 10_000)
                {
                    throw new InvalidOperationException(
                        "A product quality value is outside basis-point bounds.");
                }

                total += dimension.ValueBasisPoints;
            }

            return (int)(total / dimensions.Count);
        }

        public static bool MatchesDefinition(
            ProductBatchState batch,
            ProductDefinition product)
        {
            if (batch == null || product == null ||
                batch.QualityDimensions == null ||
                product.QualityDimensionIds == null ||
                batch.QualityDimensions.Count !=
                    product.QualityDimensionIds.Count)
            {
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < batch.QualityDimensions.Count; i++)
            {
                var dimension = batch.QualityDimensions[i];
                if (dimension == null ||
                    dimension.QualityDimensionId !=
                        product.QualityDimensionIds[i] ||
                    dimension.ValueBasisPoints < 0 ||
                    dimension.ValueBasisPoints > 10_000 ||
                    !ids.Add(dimension.QualityDimensionId))
                {
                    return false;
                }
            }

            return batch.QualityBasisPoints ==
                CalculateSummary(batch.QualityDimensions);
        }
    }

    public enum ResourceExtractionLedgerEntryType : byte
    {
        Reserved,
        Settled
    }

    [Serializable]
    public sealed class ResourceBodyState
    {
        public string Id;
        public string ResourceKindId;
        public string OutputProductDefinitionId;
        public string LocationId;
        public string Provenance;
        public string GenerationRuleVersion;
        public string RequiredFacilityTag;
        public long InitialQuantity;
        public long RemainingQuantity;
        public long ReservedQuantity;
        public int QualityBasisPoints = 10_000;
        public int ExtractionDifficultyBasisPoints = 10_000;
    }

    [Serializable]
    public sealed class ResourceExtractionOrderState
    {
        public string Id;
        public string ResourceBodyId;
        public string OwnerOrganizationId;
        public string ProductionSiteId;
        public string InventoryContainerId;
        public string ManagerPersonId;
        public List<string> WorkerPersonIds = new List<string>();
        public ProductionControlMode ControlMode;
        public ProductionOrderStatus Status;
        public long CreatedDay;
        public long FinishDay;
        public long SettledDay = -1;
        public long RequestedQuantity;
        public long ExtractedQuantity;
        public string OutputBatchId;
    }

    [Serializable]
    public sealed class ResourceExtractionLedgerEntryState
    {
        public string Id;
        public long Day;
        public ResourceExtractionLedgerEntryType Type;
        public string ResourceBodyId;
        public string ResourceExtractionOrderId;
        public string ActorPersonId;
        public long RemainingQuantityDelta;
        public long ReservedQuantityDelta;
        public string OutputBatchId;
        public long OutputQuantity;
        public string Summary;
    }
}
