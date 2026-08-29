using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public enum FoodConservationTransactionClass : byte
    {
        Unknown,
        Source,
        Sink,
        InternalTransfer,
        ReservationOnly,
        Transformation,
        OwnerChange,
        CompatibilityMirror
    }

    [Serializable]
    public sealed class FormalFoodProductLedgerState
    {
        public string ProductDefinitionId;
        public long TransactionQuantityDelta;
        public long ClosingPhysicalQuantity;
        public long Difference;
        public int BatchCount;
        public int TransactionLineCount;
    }

    [Serializable]
    public sealed class FormalFoodInventoryLedgerState
    {
        public string InventoryContainerId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public long ClosingPhysicalQuantity;
        public int BatchCount;
    }

    [Serializable]
    public sealed class FormalFoodBatchTraceState
    {
        public string BatchId;
        public string ProductDefinitionId;
        public string InventoryContainerId;
        public string OwnerFamilyId;
        public string OwnerOrganizationId;
        public string SourceWorkOrderId;
        public string SourceTransactionId;
        public long ClosingQuantity;
        public long ReservedQuantity;
        public long TransactionQuantityDelta;
        public long Difference;
        public List<string> TransactionIds = new List<string>();
    }

    [Serializable]
    public sealed class FormalFoodTransactionAuditState
    {
        public string TransactionId;
        public long Day;
        public InventoryTransactionType TransactionType;
        public FoodConservationTransactionClass Classification;
        public long PhysicalQuantityDelta;
        public long ReservedQuantityDelta;
        public int FoodLineCount;
        public string SourceId;
    }

    [Serializable]
    public sealed class FormalFoodConservationAuditState
    {
        public const string SchemaId =
            "mandate.formal-food-conservation-audit.v1";

        public string Schema = SchemaId;
        public long WorldDay;
        public FoodInventoryAuthorityMode AuthorityMode;
        public long TransactionPhysicalQuantity;
        public long ClosingPhysicalQuantity;
        public long Difference;
        public int UnknownPhysicalDeltaCount;
        public int InternalTransferImbalanceCount;
        public int ReservationPhysicalDeltaCount;
        public int DuplicateTransactionIdCount;
        public int DuplicateBatchIdCount;
        public int NegativeBatchCount;
        public int InvalidReservedQuantityCount;
        public int MissingBatchReferenceCount;
        public bool Balanced;
        public List<FormalFoodProductLedgerState> Products =
            new List<FormalFoodProductLedgerState>();
        public List<FormalFoodInventoryLedgerState> Inventories =
            new List<FormalFoodInventoryLedgerState>();
        public List<FormalFoodBatchTraceState> Batches =
            new List<FormalFoodBatchTraceState>();
        public List<FormalFoodTransactionAuditState> Transactions =
            new List<FormalFoodTransactionAuditState>();
    }

    /// <summary>
    /// Replays the existing formal inventory journal without mutating the
    /// world. The content registry, rather than an enum or ID allow-list,
    /// defines which ProductDefinitions are food.
    /// </summary>
    public sealed class FormalFoodConservationAuditor
    {
        public FormalFoodConservationAuditState Audit(
            WorldState world,
            ProductionContentRegistry content)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (content == null) throw new ArgumentNullException(nameof(content));

            var result = new FormalFoodConservationAuditState
            {
                WorldDay = world.AbsoluteDay,
                AuthorityMode = world.FoodInventoryAuthorityMode
            };
            var foodIds = new HashSet<string>(content.GetFoodsInStableOrder()
                .Select(item => item.ProductDefinitionId), StringComparer.Ordinal);
            var productLedgers = foodIds.OrderBy(item => item,
                    StringComparer.Ordinal)
                .ToDictionary(item => item,
                    item => new FormalFoodProductLedgerState
                    {
                        ProductDefinitionId = item
                    }, StringComparer.Ordinal);

            var batches = world.ProductBatches ?? new List<ProductBatchState>();
            var transactions = world.InventoryTransactions ??
                               new List<InventoryTransactionState>();
            result.DuplicateBatchIdCount = batches.Where(item => item != null)
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .Count(group => string.IsNullOrWhiteSpace(group.Key) ||
                                group.Count() > 1);
            result.DuplicateTransactionIdCount = transactions
                .Where(item => item != null)
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .Count(group => string.IsNullOrWhiteSpace(group.Key) ||
                                group.Count() > 1);

            var batchById = batches.Where(item => item != null &&
                                                  !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.Ordinal);
            var batchTraces = new Dictionary<string, FormalFoodBatchTraceState>(
                StringComparer.Ordinal);
            foreach (var batch in batches.Where(item => item != null &&
                         foodIds.Contains(item.ProductDefinitionId))
                     .OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                if (batch.Quantity < 0) result.NegativeBatchCount++;
                if (batch.ReservedQuantity < 0 ||
                    batch.ReservedQuantity > batch.Quantity)
                    result.InvalidReservedQuantityCount++;
                var ledger = productLedgers[batch.ProductDefinitionId];
                ledger.ClosingPhysicalQuantity = checked(
                    ledger.ClosingPhysicalQuantity + batch.Quantity);
                ledger.BatchCount++;
                if (batchTraces.ContainsKey(batch.Id)) continue;
                batchTraces.Add(batch.Id, new FormalFoodBatchTraceState
                {
                    BatchId = batch.Id,
                    ProductDefinitionId = batch.ProductDefinitionId,
                    InventoryContainerId = batch.InventoryContainerId,
                    OwnerFamilyId = batch.OwnerFamilyId,
                    OwnerOrganizationId = batch.OwnerOrganizationId,
                    SourceWorkOrderId = batch.SourceWorkOrderId,
                    SourceTransactionId = batch.SourceTransactionId,
                    ClosingQuantity = batch.Quantity,
                    ReservedQuantity = batch.ReservedQuantity
                });
            }

            foreach (var transaction in transactions.Where(item => item != null)
                         .OrderBy(item => item.Day)
                         .ThenBy(item => item.Id, StringComparer.Ordinal))
            {
                long physicalDelta = 0;
                long reservedDelta = 0;
                var foodLineCount = 0;
                foreach (var line in (transaction.Lines ??
                                      new List<InventoryTransactionLineState>())
                             .Where(item => item != null))
                {
                    if (!foodIds.Contains(line.ProductDefinitionId)) continue;
                    foodLineCount++;
                    physicalDelta = checked(physicalDelta + line.QuantityDelta);
                    reservedDelta = checked(reservedDelta +
                                            line.ReservedQuantityDelta);
                    var product = productLedgers[line.ProductDefinitionId];
                    product.TransactionQuantityDelta = checked(
                        product.TransactionQuantityDelta + line.QuantityDelta);
                    product.TransactionLineCount++;
                    if (string.IsNullOrWhiteSpace(line.BatchId) ||
                        !batchById.TryGetValue(line.BatchId, out var batch))
                    {
                        result.MissingBatchReferenceCount++;
                        continue;
                    }
                    if (batch.ProductDefinitionId != line.ProductDefinitionId)
                        result.MissingBatchReferenceCount++;
                    if (batchTraces.TryGetValue(line.BatchId, out var trace))
                    {
                        trace.TransactionQuantityDelta = checked(
                            trace.TransactionQuantityDelta + line.QuantityDelta);
                        trace.TransactionIds.Add(transaction.Id);
                    }
                }
                if (foodLineCount == 0) continue;

                var classification = Classify(transaction.Type);
                if (classification == FoodConservationTransactionClass.Unknown &&
                    physicalDelta != 0)
                    result.UnknownPhysicalDeltaCount++;
                if ((classification ==
                     FoodConservationTransactionClass.InternalTransfer ||
                     classification == FoodConservationTransactionClass.OwnerChange) &&
                    physicalDelta != 0)
                    result.InternalTransferImbalanceCount++;
                if (classification ==
                        FoodConservationTransactionClass.ReservationOnly &&
                    physicalDelta != 0)
                    result.ReservationPhysicalDeltaCount++;
                result.Transactions.Add(new FormalFoodTransactionAuditState
                {
                    TransactionId = transaction.Id,
                    Day = transaction.Day,
                    TransactionType = transaction.Type,
                    Classification = classification,
                    PhysicalQuantityDelta = physicalDelta,
                    ReservedQuantityDelta = reservedDelta,
                    FoodLineCount = foodLineCount,
                    SourceId = SourceId(transaction)
                });
            }

            foreach (var product in productLedgers.Values.OrderBy(item =>
                         item.ProductDefinitionId, StringComparer.Ordinal))
            {
                product.Difference = checked(product.TransactionQuantityDelta -
                                             product.ClosingPhysicalQuantity);
                result.Products.Add(product);
            }
            foreach (var trace in batchTraces.Values.OrderBy(item => item.BatchId,
                         StringComparer.Ordinal))
            {
                trace.TransactionIds = trace.TransactionIds.Distinct()
                    .OrderBy(item => item, StringComparer.Ordinal).ToList();
                trace.Difference = checked(trace.TransactionQuantityDelta -
                                           trace.ClosingQuantity);
                result.Batches.Add(trace);
            }
            result.Inventories = batches.Where(item => item != null &&
                                              foodIds.Contains(item.ProductDefinitionId))
                .GroupBy(item => new
                {
                    item.InventoryContainerId,
                    item.OwnerFamilyId,
                    item.OwnerOrganizationId
                })
                .Select(group => new FormalFoodInventoryLedgerState
                {
                    InventoryContainerId = group.Key.InventoryContainerId,
                    OwnerFamilyId = group.Key.OwnerFamilyId,
                    OwnerOrganizationId = group.Key.OwnerOrganizationId,
                    ClosingPhysicalQuantity = group.Sum(item => item.Quantity),
                    BatchCount = group.Count()
                })
                .OrderBy(item => item.InventoryContainerId,
                    StringComparer.Ordinal)
                .ThenBy(item => item.OwnerFamilyId, StringComparer.Ordinal)
                .ThenBy(item => item.OwnerOrganizationId, StringComparer.Ordinal)
                .ToList();
            result.TransactionPhysicalQuantity = result.Products.Sum(item =>
                item.TransactionQuantityDelta);
            result.ClosingPhysicalQuantity = result.Products.Sum(item =>
                item.ClosingPhysicalQuantity);
            result.Difference = checked(result.TransactionPhysicalQuantity -
                                        result.ClosingPhysicalQuantity);
            result.Balanced = result.AuthorityMode ==
                                  FoodInventoryAuthorityMode.FormalProductBatches &&
                              result.Difference == 0 &&
                              result.Products.All(item => item.Difference == 0) &&
                              result.Batches.All(item => item.Difference == 0) &&
                              result.UnknownPhysicalDeltaCount == 0 &&
                              result.InternalTransferImbalanceCount == 0 &&
                              result.ReservationPhysicalDeltaCount == 0 &&
                              result.DuplicateTransactionIdCount == 0 &&
                              result.DuplicateBatchIdCount == 0 &&
                              result.NegativeBatchCount == 0 &&
                              result.InvalidReservedQuantityCount == 0 &&
                              result.MissingBatchReferenceCount == 0;
            return result;
        }

        public static FoodConservationTransactionClass Classify(
            InventoryTransactionType type)
        {
            switch (type)
            {
                case InventoryTransactionType.OpeningBalance:
                case InventoryTransactionType.ResourceExtractionSettled:
                case InventoryTransactionType.FoodHarvested:
                    return FoodConservationTransactionClass.Source;
                case InventoryTransactionType.FoodConsumed:
                case InventoryTransactionType.CivilianFreightNaturalLoss:
                case InventoryTransactionType.FoodStorageNaturalLoss:
                case InventoryTransactionType.MedicalTreatmentConsumed:
                case InventoryTransactionType.MilitaryMedicalTreatmentConsumed:
                case InventoryTransactionType.MilitaryRearMedicalTreatmentConsumed:
                case InventoryTransactionType.MilitaryFieldHospitalConstructionConsumed:
                case InventoryTransactionType.MilitaryFieldHospitalMaintenanceConsumed:
                case InventoryTransactionType.MerchantCargoDamaged:
                case InventoryTransactionType.FacilityConstructionMaterialConsumed:
                case InventoryTransactionType.EquipmentRepairSettled:
                    return FoodConservationTransactionClass.Sink;
                case InventoryTransactionType.Reserved:
                case InventoryTransactionType.ReservationReleased:
                case InventoryTransactionType.EquipmentRepairReserved:
                case InventoryTransactionType.MilitaryLogisticsHandoffReserved:
                case InventoryTransactionType.FoodMarketReserved:
                case InventoryTransactionType.FoodMarketReservationReleased:
                case InventoryTransactionType.MilitaryMedicalTransferMedicineReserved:
                case InventoryTransactionType.MilitaryMedicalTransferMedicineReleased:
                case InventoryTransactionType.FacilityConstructionMaterialReserved:
                case InventoryTransactionType.FacilityConstructionMaterialReleased:
                    return FoodConservationTransactionClass.ReservationOnly;
                case InventoryTransactionType.RecipeSettled:
                    return FoodConservationTransactionClass.Transformation;
                case InventoryTransactionType.LegacyBalanceConverted:
                case InventoryTransactionType.LegacyFoodStockFormalized:
                    return FoodConservationTransactionClass.CompatibilityMirror;
                case InventoryTransactionType.MilitaryProcurementDispatched:
                case InventoryTransactionType.MilitaryLogisticsDispatched:
                case InventoryTransactionType.MilitaryLogisticsHandoffLoaded:
                case InventoryTransactionType.FoodTaxTransferred:
                case InventoryTransactionType.FoodVillageReliefTransferred:
                case InventoryTransactionType.FoodCountyReliefTransferred:
                case InventoryTransactionType.FoodTaxRemitted:
                case InventoryTransactionType.FoodMarketTransferred:
                case InventoryTransactionType.CivilianFreightDispatched:
                case InventoryTransactionType.CivilianFreightDelivered:
                case InventoryTransactionType.FoodPublicReliefProcurementTransferred:
                case InventoryTransactionType.MilitaryLogisticsDelivered:
                case InventoryTransactionType.MerchantMarketPurchased:
                case InventoryTransactionType.MerchantMarketSold:
                    return FoodConservationTransactionClass.InternalTransfer;
                default:
                    return FoodConservationTransactionClass.Unknown;
            }
        }

        private static string SourceId(InventoryTransactionState transaction)
        {
            return FirstNonEmpty(
                transaction.SourceWorkOrderId,
                transaction.SourceFormalMarketOrderId,
                transaction.SourceCivilianFreightId,
                transaction.SourceFoodStorageLossId,
                transaction.SourceHouseholdReliefConsumptionId,
                transaction.SourceVillageId,
                transaction.SourceCountyGovernanceId,
                transaction.SourceMilitaryProcurementId,
                transaction.SourceMilitaryLogisticsOrderId,
                transaction.SourceResourceExtractionOrderId,
                transaction.SourceFacilityConstructionProjectId);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (var i = 0; i < values.Length; i++)
                if (!string.IsNullOrWhiteSpace(values[i])) return values[i];
            return string.Empty;
        }
    }

    [Serializable]
    public sealed class LuoyangFoodFlowAuditState
    {
        public string FlowId;
        public long Day;
        public string SimulationPhase;
        public string OperationId;
        public string ProductId;
        public string SourceInventoryId;
        public string DestinationInventoryId;
        public long QuantityMilliunits;
        public long ExplicitPhysicalLossMilliunits;
        public FoodConservationTransactionClass Classification;
    }

    [Serializable]
    public sealed class LuoyangFoodProductLedgerState
    {
        public string ProductId;
        public long SourceMilliunits;
        public long ClosingInventoryMilliunits;
        public long ClosingCompatibilityReserveMilliunits;
        public long ConsumedMilliunits;
        public long ProcessingLossMilliunits;
        public int FlowCount;
        public bool CompatibilityAggregate;
    }

    [Serializable]
    public sealed class LuoyangFoodConservationAuditState
    {
        public const string SchemaId =
            "mandate.luoyang-living-food-conservation-audit.v1";

        public string Schema = SchemaId;
        public long WorldDay;
        public long SourceMilliunits;
        public long HouseholdConsumedMilliunits;
        public long MilitaryConsumedMilliunits;
        public long ProcessingLossMilliunits;
        public long ClosingInventoryMilliunits;
        public long ClosingHouseholdReserveMilliunits;
        public long DifferenceMilliunits;
        public long LegacyExcludedSourceMilliunits;
        public long LegacyExcludedClosingMilliunits;
        public long LegacyBoundaryDifferenceMilliunits;
        public int UnknownPhysicalDeltaCount;
        public bool Balanced;
        public List<LuoyangFoodProductLedgerState> Products =
            new List<LuoyangFoodProductLedgerState>();
        public List<LuoyangFoodFlowAuditState> Transactions =
            new List<LuoyangFoodFlowAuditState>();
    }

    /// <summary>
    /// Read-only closure for the V70 derived Luoyang living-world checkpoint.
    /// The compact household reserve is the explicit compatibility food product;
    /// it is physical stock, not a second mirror of a formal ProductBatch.
    /// </summary>
    public sealed class LuoyangFoodConservationAuditor
    {
        public const string CompatibilityFoodProductId =
            "product.reference.food_equivalent";
        public const string CompactHouseholdInventoryId =
            "household.compact_reserves";

        public LuoyangFoodConservationAuditState Audit(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (runtime.FormalEconomy != null &&
                runtime.FormalEconomy.IsPhysicalAuthority)
            {
                var formal = AuditFormalAuthority(runtime);
                // Preserve the old compact-boundary RCA signal as diagnostic
                // evidence only.  It must not become a second authority or
                // affect the balanced result of the formal ledger.
                formal.LegacyBoundaryDifferenceMilliunits =
                    AuditLegacyProjection(runtime)
                        .LegacyBoundaryDifferenceMilliunits;
                return formal;
            }
            return AuditLegacyProjection(runtime);
        }

        private static LuoyangFoodConservationAuditState AuditLegacyProjection(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var result = new LuoyangFoodConservationAuditState
            {
                WorldDay = runtime.AbsoluteDay
            };
            var products = new SortedDictionary<string,
                LuoyangFoodProductLedgerState>(StringComparer.Ordinal);
            foreach (var flow in (runtime.InventoryFlows ??
                         new List<LuoyangInventoryFlowState>())
                     .Where(item => item != null &&
                                    IsPhysicalFoodProduct(item.ProductId))
                     .OrderBy(item => item.Day)
                     .ThenBy(item => item.Id, StringComparer.Ordinal))
            {
                var classification = Classify(flow.OperationId);
                if (classification == FoodConservationTransactionClass.Unknown &&
                    flow.QuantityMilliunits != 0)
                    result.UnknownPhysicalDeltaCount++;
                result.Transactions.Add(new LuoyangFoodFlowAuditState
                {
                    FlowId = flow.Id,
                    Day = flow.Day,
                    SimulationPhase = Phase(flow.OperationId),
                    OperationId = flow.OperationId,
                    ProductId = flow.ProductId,
                    SourceInventoryId = flow.SourceInventoryId,
                    DestinationInventoryId = flow.DestinationInventoryId,
                    QuantityMilliunits = flow.QuantityMilliunits,
                    ExplicitPhysicalLossMilliunits =
                        PhysicalLoss(flow.OperationId, flow.LossMilliunits),
                    Classification = classification
                });
                var product = Product(products, flow.ProductId);
                product.FlowCount++;
                if (classification == FoodConservationTransactionClass.Source)
                {
                    product.SourceMilliunits = checked(product.SourceMilliunits +
                                                       flow.QuantityMilliunits);
                    result.SourceMilliunits = checked(result.SourceMilliunits +
                                                      flow.QuantityMilliunits);
                    if (flow.ProductId == CompatibilityFoodProductId)
                        result.LegacyExcludedSourceMilliunits = checked(
                            result.LegacyExcludedSourceMilliunits +
                            flow.QuantityMilliunits);
                }
                if (flow.OperationId == "production.recipe_settlement")
                {
                    product.ProcessingLossMilliunits = checked(
                        product.ProcessingLossMilliunits + flow.LossMilliunits);
                    result.ProcessingLossMilliunits = checked(
                        result.ProcessingLossMilliunits + flow.LossMilliunits);
                }
            }

            foreach (var inventory in (runtime.Inventories ??
                         new List<LuoyangInventoryBalanceState>())
                     .Where(item => item != null &&
                                    IsPhysicalFoodProduct(item.ProductId)))
            {
                result.ClosingInventoryMilliunits = checked(
                    result.ClosingInventoryMilliunits +
                    inventory.QuantityMilliunits);
                Product(products, inventory.ProductId)
                    .ClosingInventoryMilliunits = checked(
                    Product(products, inventory.ProductId)
                        .ClosingInventoryMilliunits + inventory.QuantityMilliunits);
                if (inventory.ProductId == CompatibilityFoodProductId)
                    result.LegacyExcludedClosingMilliunits = checked(
                        result.LegacyExcludedClosingMilliunits +
                        inventory.QuantityMilliunits);
            }
            result.ClosingHouseholdReserveMilliunits =
                (runtime.Households ?? new List<LuoyangHouseholdConsumptionState>())
                .Where(item => item != null).Sum(item => item.FoodReserveMilliunits);
            var compatibility = Product(products, CompatibilityFoodProductId);
            compatibility.CompatibilityAggregate = true;
            compatibility.ClosingCompatibilityReserveMilliunits =
                result.ClosingHouseholdReserveMilliunits;
            result.HouseholdConsumedMilliunits = runtime.Households
                .Where(item => item != null)
                .Sum(item => item.CumulativeFoodConsumedMilliunits);
            compatibility.ConsumedMilliunits =
                result.HouseholdConsumedMilliunits;
            result.MilitaryConsumedMilliunits = (runtime.Forces ??
                    new List<LuoyangMilitaryForceRuntimeState>())
                .Where(item => item != null).Sum(item => item.FoodConsumedMilliunits);
            Product(products, CoreProductionContent.DryRationProductId)
                .ConsumedMilliunits = result.MilitaryConsumedMilliunits;

            var closing = checked(result.ClosingInventoryMilliunits +
                                  result.ClosingHouseholdReserveMilliunits);
            var sinks = checked(result.HouseholdConsumedMilliunits +
                                result.MilitaryConsumedMilliunits +
                                result.ProcessingLossMilliunits);
            result.DifferenceMilliunits = checked(result.SourceMilliunits -
                                                  sinks - closing);
            var legacySource = checked(result.SourceMilliunits -
                                       result.LegacyExcludedSourceMilliunits);
            var legacyClosing = checked(result.ClosingInventoryMilliunits -
                                        result.LegacyExcludedClosingMilliunits);
            result.LegacyBoundaryDifferenceMilliunits = checked(legacySource -
                sinks - legacyClosing);
            result.Products = products.Values.ToList();
            result.Balanced = result.DifferenceMilliunits == 0 &&
                              result.UnknownPhysicalDeltaCount == 0;
            return result;
        }

        private static LuoyangFoodConservationAuditState AuditFormalAuthority(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var formal = runtime.FormalEconomy;
            long transactionDelta = 0;
            long source = 0;
            long sink = 0;
            foreach (var transaction in formal.InventoryTransactions.Where(
                         item => item != null))
            {
                var net = transaction.Lines.Where(item => item != null &&
                        LuoyangFormalEconomyContract.IsFood(
                            item.ProductDefinitionId))
                    .Sum(item => item.QuantityDelta);
                transactionDelta = checked(transactionDelta + net);
                if (net > 0) source = checked(source + net);
                else sink = checked(sink - net);
            }
            var household = formal.ProductBatches.Where(item => item != null &&
                    item.InventoryContainerId ==
                    LuoyangFormalEconomyContract.HouseholdContainerId &&
                    LuoyangFormalEconomyContract.IsFood(
                        item.ProductDefinitionId))
                .Sum(item => item.Quantity);
            var closing = formal.ProductBatches.Where(item => item != null &&
                    LuoyangFormalEconomyContract.IsFood(
                        item.ProductDefinitionId))
                .Sum(item => item.Quantity);
            var householdConsumed = runtime.Households.Where(item =>
                    item != null)
                .Sum(item => item.CumulativeFoodConsumedMilliunits);
            var militaryConsumed = runtime.Forces.Where(item => item != null)
                .Sum(item => item.FoodConsumedMilliunits);
            var result = new LuoyangFoodConservationAuditState
            {
                WorldDay = runtime.AbsoluteDay,
                SourceMilliunits = source,
                HouseholdConsumedMilliunits = householdConsumed,
                MilitaryConsumedMilliunits = militaryConsumed,
                ProcessingLossMilliunits = Math.Max(0,
                    sink - householdConsumed - militaryConsumed),
                ClosingInventoryMilliunits = closing - household,
                ClosingHouseholdReserveMilliunits = household,
                DifferenceMilliunits = checked(transactionDelta - closing),
                UnknownPhysicalDeltaCount = checked((int)Math.Min(
                    int.MaxValue, formal.CompactPhysicalMutationCount))
            };
            result.LegacyBoundaryDifferenceMilliunits =
                result.DifferenceMilliunits;
            result.Balanced = result.DifferenceMilliunits == 0 &&
                              result.UnknownPhysicalDeltaCount == 0;
            result.Products.Add(new LuoyangFoodProductLedgerState
            {
                ProductId = CompatibilityFoodProductId,
                CompatibilityAggregate = true,
                SourceMilliunits = result.SourceMilliunits,
                ConsumedMilliunits = checked(householdConsumed +
                                             militaryConsumed),
                ProcessingLossMilliunits = result.ProcessingLossMilliunits,
                ClosingInventoryMilliunits =
                    result.ClosingInventoryMilliunits,
                ClosingCompatibilityReserveMilliunits = household
            });
            return result;
        }

        public static bool IsPhysicalFoodProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return false;
            if (productId == CompatibilityFoodProductId) return true;
            if (productId.StartsWith("product.food.",
                    StringComparison.Ordinal)) return true;
            return productId == CoreProductionContent.WheatGrainProductId ||
                   productId == CoreProductionContent.WheatFlourProductId ||
                   productId == CoreProductionContent.DryRationProductId;
        }

        public static FoodConservationTransactionClass Classify(
            string operationId)
        {
            switch (operationId)
            {
                case "scenario.opening.delivered_stock":
                case "supply.shipment_delivered":
                case "production.crop_harvest":
                    return FoodConservationTransactionClass.Source;
                case "household.food_reserve_consumed":
                case "military.food_consumed":
                    return FoodConservationTransactionClass.Sink;
                case "production.recipe_settlement":
                    return FoodConservationTransactionClass.Transformation;
                case "scenario.opening.household_food_allocation":
                case "market.household_food_purchase":
                case "government.relief.household_delivery":
                case "government.tax.inkind.household_to_granary":
                case "government.tax.inkind.market_to_granary":
                case "military.procurement.local_batch":
                    return FoodConservationTransactionClass.InternalTransfer;
                default:
                    return FoodConservationTransactionClass.Unknown;
            }
        }

        private static string Phase(string operationId)
        {
            if (operationId == "supply.shipment_delivered")
                return "external_supply";
            if (operationId == "production.crop_harvest" ||
                operationId == "production.recipe_settlement")
                return "production";
            if (operationId == "market.household_food_purchase")
                return "intelligent_agents";
            if (operationId == "household.food_reserve_consumed")
                return "household_consumption";
            if (operationId != null && operationId.StartsWith(
                    "government.", StringComparison.Ordinal))
                return "integrated_government";
            if (operationId != null && operationId.StartsWith(
                    "military.", StringComparison.Ordinal))
                return "integrated_military";
            return "initialization";
        }

        private static long PhysicalLoss(string operationId, long loss)
        {
            if (loss <= 0) return 0;
            return operationId == "production.recipe_settlement" ? loss : 0;
        }

        private static LuoyangFoodProductLedgerState Product(
            IDictionary<string, LuoyangFoodProductLedgerState> products,
            string productId)
        {
            if (!products.TryGetValue(productId, out var result))
            {
                result = new LuoyangFoodProductLedgerState
                {
                    ProductId = productId,
                    CompatibilityAggregate =
                        productId == CompatibilityFoodProductId
                };
                products.Add(productId, result);
            }
            return result;
        }
    }
}
