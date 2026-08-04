using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class FormalPublicFoodSettlementResult
    {
        public string CountyGovernanceId;
        public int VillagesProcessed;
        public long HouseholdTaxTransferred;
        public long VillageTaxRemitted;
        public long RequestedReliefQuantity;
        public long CountyReliefTransferred;
        public long ReliefShortfallQuantity;
    }

    public sealed class CountyGovernanceSystem
    {
        private const int BasisPoints = 10_000;
        private readonly ProductionContentRegistry _productionContent;
        private readonly FoodInventorySystem _foodInventory;

        public CountyGovernanceSystem(
            ProductionContentRegistry productionContent = null)
        {
            _productionContent = productionContent ??
                ProductionContentRegistry.CreateCore();
            _foodInventory = new FoodInventorySystem(
                _productionContent);
        }

        public void ResolveMonthly(WorldState world)
        {
            ResolveMonthlyCore(world, true);
        }

        public void ResolveMonthlyAfterFormalPublicFoodCommands(
            WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                throw new InvalidOperationException(
                    "Only formal food worlds can skip committed public food settlement.");
            }
            ResolveMonthlyCore(world, false);
        }

        private void ResolveMonthlyCore(
            WorldState world,
            bool resolvePublicFood)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.AbsoluteDay == 0 || world.AbsoluteDay % 30 != 0)
            {
                return;
            }

            var governances = new List<CountyGovernanceState>(
                world.CountyGovernances);
            governances.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < governances.Count; i++)
            {
                if (governances[i].LastSettlementDay == world.AbsoluteDay ||
                    governances[i].NextSettlementDay > world.AbsoluteDay)
                {
                    continue;
                }

                ResolveCounty(world, governances[i], resolvePublicFood);
            }
        }

        public bool HasFormalPublicFoodWork(
            WorldState world,
            string governanceId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                world.AbsoluteDay <= 0 ||
                world.AbsoluteDay % 30 != 0)
            {
                return false;
            }

            var governance = FindGovernance(world, governanceId);
            if (governance.LastSettlementDay == world.AbsoluteDay ||
                governance.NextSettlementDay > world.AbsoluteDay)
            {
                return false;
            }

            var villages = VillagesForCounty(
                world, governance.CountyLocationId);
            if (villages.Count == 0)
            {
                return false;
            }

            var monthInYear =
                (int)(((world.AbsoluteDay / 30) - 1) % 12) + 1;
            if (monthInYear == 10)
            {
                for (var i = 0; i < villages.Count; i++)
                {
                    if (villages[i].HouseholdIds.Count > 0)
                    {
                        return true;
                    }
                }
            }

            var marketPressure = CalculateMarketPressure(world, governance);
            for (var i = 0; i < villages.Count; i++)
            {
                if (villages[i].FoodSecurityBasisPoints < 8_000 ||
                    marketPressure > 11_000)
                {
                    return true;
                }
            }
            return false;
        }

        public void ValidateFormalPublicFoodMonthly(
            WorldState world,
            VillageLifeSystem villageLife,
            string governanceId,
            long expectedDay)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (villageLife == null)
            {
                throw new ArgumentNullException(nameof(villageLife));
            }
            world.Validate();
            _productionContent.ValidateWorldReferences(world);
            var governance = FindGovernance(world, governanceId);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                expectedDay != world.AbsoluteDay ||
                expectedDay <= 0 ||
                expectedDay % 30 != 0 ||
                governance.LastSettlementDay == expectedDay ||
                governance.NextSettlementDay > expectedDay ||
                !HasFormalPublicFoodWork(world, governanceId))
            {
                throw new InvalidOperationException(
                    "Formal public food monthly settlement is not due.");
            }

            var villages = VillagesForCounty(
                world, governance.CountyLocationId);
            var monthInYear =
                (int)(((expectedDay / 30) - 1) % 12) + 1;
            for (var i = 0; i < villages.Count; i++)
            {
                if (villages[i].LastSettlementDay != expectedDay)
                {
                    throw new InvalidOperationException(
                        $"Village {villages[i].Id} has not completed its monthly settlement.");
                }
                if (monthInYear == 10 && villages[i].HouseholdIds.Count > 0)
                {
                    villageLife.ValidateFormalTaxMonthly(
                        world, villages[i].Id, expectedDay);
                }
            }
        }

        public FormalPublicFoodSettlementResult ResolveFormalPublicFoodMonthly(
            WorldState world,
            VillageLifeSystem villageLife,
            string governanceId,
            long expectedDay)
        {
            ValidateFormalPublicFoodMonthly(
                world, villageLife, governanceId, expectedDay);
            var governance = FindGovernance(world, governanceId);
            var villages = VillagesForCounty(
                world, governance.CountyLocationId);
            var result = new FormalPublicFoodSettlementResult
            {
                CountyGovernanceId = governance.Id,
                VillagesProcessed = villages.Count
            };
            var monthInYear =
                (int)(((expectedDay / 30) - 1) % 12) + 1;
            if (monthInYear == 10)
            {
                for (var i = 0; i < villages.Count; i++)
                {
                    if (villages[i].HouseholdIds.Count > 0)
                    {
                        result.HouseholdTaxTransferred = checked(
                            result.HouseholdTaxTransferred +
                            villageLife.ResolveFormalTaxMonthly(
                                world, villages[i].Id, expectedDay));
                    }
                }

                var remittedBefore = governance.TotalGrainTaxReceived;
                RemitVillageGrainTax(world, governance, villages);
                result.VillageTaxRemitted = checked(
                    governance.TotalGrainTaxReceived - remittedBefore);
            }

            governance.LastMarketPressureBasisPoints =
                CalculateMarketPressure(world, governance);
            var reliefBefore = governance.TotalReliefGrain;
            DistributeRelief(
                world,
                governance,
                villages,
                out var requestedRelief,
                out var reliefShortfall);
            result.RequestedReliefQuantity = requestedRelief;
            result.CountyReliefTransferred = checked(
                governance.TotalReliefGrain - reliefBefore);
            result.ReliefShortfallQuantity = reliefShortfall;
            return result;
        }

        private void ResolveCounty(
            WorldState world,
            CountyGovernanceState governance,
            bool resolvePublicFood)
        {
            var organization = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var administrator = FindFamily(
                world, governance.AdministratorFamilyId);
            var villages = VillagesForCounty(world, governance.CountyLocationId);
            governance.LastMarketPressureBasisPoints =
                CalculateMarketPressure(world, governance);

            PayAdministrationStipend(
                world, governance, organization, administrator);
            var relief = resolvePublicFood
                ? DistributeRelief(
                    world,
                    governance,
                    villages,
                    out _,
                    out _)
                : ReliefIssuedToday(world, governance.Id);

            var monthInYear =
                (int)(((world.AbsoluteDay / 30) - 1) % 12) + 1;
            if (monthInYear == 10)
            {
                CollectHouseholdCashTax(
                    world, governance, organization, villages);
                if (resolvePublicFood)
                {
                    RemitVillageGrainTax(world, governance, villages);
                }
            }

            ApplyPublicOrderFeedback(world, governance, villages, relief);
            governance.LastSettlementDay = world.AbsoluteDay;
            governance.NextSettlementDay = world.AbsoluteDay + 30;
        }

        private static long ReliefIssuedToday(
            WorldState world,
            string governanceId)
        {
            long total = 0;
            for (var i = 0; i < world.CountyFiscalLedgerEntries.Count; i++)
            {
                var entry = world.CountyFiscalLedgerEntries[i];
                if (entry.Day == world.AbsoluteDay &&
                    entry.CountyGovernanceId == governanceId &&
                    entry.Type == CountyFiscalEntryType.GrainRelief)
                {
                    total = checked(total + entry.Amount);
                }
            }
            return total;
        }

        private static void PayAdministrationStipend(
            WorldState world,
            CountyGovernanceState governance,
            OrganizationState organization,
            FamilyState administrator)
        {
            var stipend = Math.Min(30L, organization.Treasury);
            if (stipend <= 0)
            {
                return;
            }

            organization.Treasury -= stipend;
            administrator.Wealth += stipend;
            governance.TotalAdministrationPaid += stipend;
            AddLedger(
                world,
                governance,
                CountyFiscalEntryType.AdministrationStipend,
                administrator.Id,
                string.Empty,
                stipend,
                -stipend,
                0,
                0,
                stipend,
                "County administration stipend paid.");
        }

        private long DistributeRelief(
            WorldState world,
            CountyGovernanceState governance,
            List<VillageState> villages,
            out long requestedTotal,
            out long shortfallTotal)
        {
            long total = 0;
            requestedTotal = 0;
            shortfallTotal = 0;
            for (var i = 0; i < villages.Count; i++)
            {
                var village = villages[i];
                var foodGap = Math.Max(
                    0, 8_000 - village.FoodSecurityBasisPoints);
                var marketGap = Math.Max(
                    0, governance.LastMarketPressureBasisPoints - 11_000);
                if (foodGap == 0 && marketGap == 0)
                {
                    continue;
                }

                var requested = Math.Max(
                    1L,
                    (long)Math.Max(1, village.LivingResidentCount) *
                    (foodGap + marketGap) / 20_000);
                requestedTotal = checked(requestedTotal + requested);
                long issued;
                if (world.FoodInventoryAuthorityMode ==
                    FoodInventoryAuthorityMode.FormalProductBatches)
                {
                    var available = _foodInventory.SummarizeContainer(
                        world,
                        governance.GranaryInventoryContainerId)
                        .PhysicalQuantity;
                    var requestedFromStock = Math.Min(requested, available);
                    issued = requestedFromStock <= 0
                        ? 0
                        : _foodInventory.TransferContainerToContainer(
                            world,
                            governance.GranaryInventoryContainerId,
                            village.PublicGranaryInventoryContainerId,
                            FindOrganization(
                                world,
                                governance.GovernmentOrganizationId)
                                .LeaderPersonId,
                            requestedFromStock,
                            InventoryTransactionType
                                .FoodCountyReliefTransferred,
                            village.Id,
                            governance.Id).TransferredPhysicalQuantity;
                }
                else
                {
                    issued = Math.Min(
                        requested, governance.CountyGranaryGrain);
                    governance.CountyGranaryGrain -= issued;
                    village.PublicGranaryGrain += issued;
                }
                var shortfall = checked(requested - issued);
                if (shortfall > 0)
                {
                    shortfallTotal = checked(shortfallTotal + shortfall);
                    AddLedger(
                        world,
                        governance,
                        CountyFiscalEntryType.GrainReliefShortfall,
                        string.Empty,
                        village.Id,
                        0,
                        0,
                        0,
                        0,
                        shortfall,
                        "County relief demand remained unmet after drawing the county granary.");
                }
                if (issued <= 0)
                {
                    continue;
                }

                governance.TotalReliefGrain += issued;
                total += issued;
                AddLedger(
                    world,
                    governance,
                    CountyFiscalEntryType.GrainRelief,
                    string.Empty,
                    village.Id,
                    0,
                    0,
                    issued,
                    -issued,
                    issued,
                    "County granary relief issued to village.");
            }

            return total;
        }

        private static void CollectHouseholdCashTax(
            WorldState world,
            CountyGovernanceState governance,
            OrganizationState organization,
            List<VillageState> villages)
        {
            var families = FamiliesForVillages(world, villages);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var taxable = Math.Max(0L, family.Wealth - 100L);
                var assessed = taxable *
                    governance.AnnualCashTaxRateBasisPoints / BasisPoints;
                assessed = assessed *
                    governance.RegistrationCoverageBasisPoints / BasisPoints;
                assessed = assessed *
                    governance.AdministrativeEfficiencyBasisPoints / BasisPoints;

                var gentry = FindGentry(world, governance.Id, family.Id);
                if (gentry != null)
                {
                    var compliant = assessed *
                        gentry.TaxComplianceBasisPoints / BasisPoints;
                    gentry.TotalAssessmentReductionMoney +=
                        assessed - compliant;
                    assessed = compliant;
                }

                var tax = FindOrCreateTaxAccount(
                    world, governance.Id, family.Id);
                tax.AssessedMoney += assessed;
                tax.LastAssessmentDay = world.AbsoluteDay;
                AddLedger(
                    world,
                    governance,
                    CountyFiscalEntryType.HouseholdAssessment,
                    family.Id,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    assessed,
                    "Annual household cash tax assessed.");

                var payable = assessed + tax.ArrearsMoney;
                var paid = Math.Min(payable, Math.Max(0L, family.Wealth - 100L));
                tax.PaidMoney += paid;
                tax.ArrearsMoney = payable - paid;
                if (paid <= 0)
                {
                    continue;
                }

                family.Wealth -= paid;
                organization.Treasury += paid;
                governance.TotalMoneyTaxCollected += paid;
                AddLedger(
                    world,
                    governance,
                    CountyFiscalEntryType.HouseholdPayment,
                    family.Id,
                    string.Empty,
                    -paid,
                    paid,
                    0,
                    0,
                    paid,
                    "Household cash tax paid to county government.");
            }
        }

        private void RemitVillageGrainTax(
            WorldState world,
            CountyGovernanceState governance,
            List<VillageState> villages)
        {
            for (var i = 0; i < villages.Count; i++)
            {
                var village = villages[i];
                long collectedToday = 0;
                if (world.FoodInventoryAuthorityMode ==
                    FoodInventoryAuthorityMode.FormalProductBatches)
                {
                    for (var transactionIndex = 0;
                         transactionIndex < world.InventoryTransactions.Count;
                         transactionIndex++)
                    {
                        var transaction =
                            world.InventoryTransactions[transactionIndex];
                        if (transaction.Day != world.AbsoluteDay ||
                            transaction.Type != InventoryTransactionType
                                .FoodTaxTransferred ||
                            transaction.SourceVillageId != village.Id)
                        {
                            continue;
                        }

                        for (var lineIndex = 0;
                             lineIndex < transaction.Lines.Count;
                             lineIndex++)
                        {
                            var line = transaction.Lines[lineIndex];
                            if (line.InventoryContainerId ==
                                    village
                                        .PublicGranaryInventoryContainerId &&
                                line.QuantityDelta > 0)
                            {
                                collectedToday = checked(
                                    collectedToday + line.QuantityDelta);
                            }
                        }
                    }
                }
                else
                {
                    for (var entryIndex = 0;
                         entryIndex < world.VillageLedgerEntries.Count;
                         entryIndex++)
                    {
                        var entry = world.VillageLedgerEntries[entryIndex];
                        if (entry.Day == world.AbsoluteDay &&
                            entry.VillageId == village.Id &&
                            entry.Type == VillageLedgerEntryType.TaxPayment)
                        {
                            collectedToday += entry.PublicGrainDelta;
                        }
                    }
                }

                var remitRate = BasisPoints -
                    governance.LocalGrainRetentionBasisPoints;
                var requested = collectedToday * remitRate / BasisPoints;
                long remittance;
                if (world.FoodInventoryAuthorityMode ==
                    FoodInventoryAuthorityMode.FormalProductBatches)
                {
                    var available = _foodInventory.SummarizeContainer(
                        world,
                        village.PublicGranaryInventoryContainerId)
                        .PhysicalQuantity;
                    requested = Math.Min(requested, available);
                    remittance = requested <= 0
                        ? 0
                        : _foodInventory.TransferContainerToContainer(
                            world,
                            village.PublicGranaryInventoryContainerId,
                            governance.GranaryInventoryContainerId,
                            FindOrganization(
                                world,
                                governance.GovernmentOrganizationId)
                                .LeaderPersonId,
                            requested,
                            InventoryTransactionType.FoodTaxRemitted,
                            village.Id,
                            governance.Id).TransferredPhysicalQuantity;
                }
                else
                {
                    remittance = Math.Min(
                        village.PublicGranaryGrain, requested);
                    village.PublicGranaryGrain -= remittance;
                    governance.CountyGranaryGrain += remittance;
                }
                if (remittance <= 0)
                {
                    continue;
                }

                governance.TotalGrainTaxReceived += remittance;
                AddLedger(
                    world,
                    governance,
                    CountyFiscalEntryType.GrainRemittance,
                    string.Empty,
                    village.Id,
                    0,
                    0,
                    -remittance,
                    remittance,
                    remittance,
                    "Village remitted its county share of collected grain tax.");
            }
        }

        private static void ApplyPublicOrderFeedback(
            WorldState world,
            CountyGovernanceState governance,
            List<VillageState> villages,
            long relief)
        {
            var foodTotal = 0L;
            for (var i = 0; i < villages.Count; i++)
            {
                foodTotal += villages[i].FoodSecurityBasisPoints;
            }

            var food = villages.Count == 0
                ? BasisPoints
                : (int)(foodTotal / villages.Count);
            var change =
                (governance.AdministrativeEfficiencyBasisPoints - 5_000) / 200 +
                (food - 8_000) / 200 -
                Math.Max(0, governance.LastMarketPressureBasisPoints - 10_000) / 200 +
                (relief > 0 ? 20 : 0) -
                governance.GentryInfluenceBasisPoints / 500;
            change = Clamp(change, -100, 100);
            governance.LastPublicOrderChange = change;
            var county = FindLocation(world, governance.CountyLocationId);
            county.PublicOrderBasisPoints = Clamp(
                county.PublicOrderBasisPoints + change, 0, BasisPoints);
            for (var i = 0; i < villages.Count; i++)
            {
                var villageLocation = FindLocation(world, villages[i].LocationId);
                villageLocation.PublicOrderBasisPoints = Clamp(
                    villageLocation.PublicOrderBasisPoints + change / 2,
                    0,
                    BasisPoints);
            }
        }

        private static int CalculateMarketPressure(
            WorldState world,
            CountyGovernanceState governance)
        {
            if (world.FoodInventoryAuthorityMode ==
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                return Clamp(
                    FormalCountyMarketSystem.CalculateMarketPressureBasisPoints(
                        world, governance.Id),
                    0,
                    20_000);
            }

            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == governance.CountyLocationId &&
                    listing.CommodityId == "commodity.grain")
                {
                    return Clamp(
                        checked((int)((long)listing.Price * BasisPoints /
                            Math.Max(1, listing.EquilibriumPrice))),
                        0,
                        20_000);
                }
            }

            return BasisPoints;
        }

        private static List<VillageState> VillagesForCounty(
            WorldState world,
            string countyLocationId)
        {
            var result = new List<VillageState>();
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].ParentLocationId == countyLocationId)
                {
                    result.Add(world.Villages[i]);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static List<FamilyState> FamiliesForVillages(
            WorldState world,
            List<VillageState> villages)
        {
            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < villages.Count; i++)
            {
                for (var familyIndex = 0;
                     familyIndex < villages[i].HouseholdIds.Count;
                     familyIndex++)
                {
                    familyIds.Add(villages[i].HouseholdIds[familyIndex]);
                }
            }

            var result = new List<FamilyState>();
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (familyIds.Contains(world.Families[i].Id))
                {
                    result.Add(world.Families[i]);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static CountyGentryHouseState FindGentry(
            WorldState world,
            string governanceId,
            string familyId)
        {
            for (var i = 0; i < world.CountyGentryHouses.Count; i++)
            {
                var gentry = world.CountyGentryHouses[i];
                if (gentry.CountyGovernanceId == governanceId &&
                    gentry.FamilyId == familyId)
                {
                    return gentry;
                }
            }

            return null;
        }

        private static CountyHouseholdTaxState FindOrCreateTaxAccount(
            WorldState world,
            string governanceId,
            string familyId)
        {
            for (var i = 0; i < world.CountyHouseholdTaxes.Count; i++)
            {
                var account = world.CountyHouseholdTaxes[i];
                if (account.CountyGovernanceId == governanceId &&
                    account.FamilyId == familyId)
                {
                    return account;
                }
            }

            var created = new CountyHouseholdTaxState
            {
                Id = $"county_tax.{governanceId}.{familyId}",
                CountyGovernanceId = governanceId,
                FamilyId = familyId
            };
            world.CountyHouseholdTaxes.Add(created);
            return created;
        }

        private static void AddLedger(
            WorldState world,
            CountyGovernanceState governance,
            CountyFiscalEntryType type,
            string familyId,
            string villageId,
            long familyMoneyDelta,
            long governmentMoneyDelta,
            long villageGrainDelta,
            long countyGrainDelta,
            long amount,
            string summary)
        {
            var subject = !string.IsNullOrEmpty(familyId)
                ? familyId
                : villageId;
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = $"county_fiscal.{governance.Id}.{world.AbsoluteDay}." +
                         $"{type.ToString().ToLowerInvariant()}.{subject}",
                    Day = world.AbsoluteDay,
                    Type = type,
                    CountyGovernanceId = governance.Id,
                    FamilyId = familyId,
                    VillageId = villageId,
                    FamilyMoneyDelta = familyMoneyDelta,
                    GovernmentMoneyDelta = governmentMoneyDelta,
                    VillageGrainDelta = villageGrainDelta,
                    CountyGrainDelta = countyGrainDelta,
                    Amount = amount,
                    Summary = summary
                });
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world,
            string governanceId)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].Id == governanceId)
                {
                    return world.CountyGovernances[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing county governance {governanceId}.");
        }

        private static FamilyState FindFamily(WorldState world, string familyId)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == familyId)
                {
                    return world.Families[i];
                }
            }

            throw new InvalidOperationException($"Missing family {familyId}.");
        }

        private static LocationState FindLocation(
            WorldState world,
            string locationId)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == locationId)
                {
                    return world.Locations[i];
                }
            }

            throw new InvalidOperationException($"Missing location {locationId}.");
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }

    public sealed class FormalPublicFoodMonthlyCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.formal_public_food.resolve_monthly";
        public const string IssuerId = "system.formal_public_food";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string GovernanceIdArgumentId = "county_governance_id";
        public const string TransactionKindId =
            "mandate.transaction.formal_public_food.resolve_monthly";
        public const string EventTypeId =
            "mandate.event.formal_public_food.monthly_resolved";
        public const string ReliefShortfallEventTypeId =
            PublicReliefProcurementContractIds.ShortfallEventTypeId;
        public const string ProjectionHandlerId =
            "mandate.handler.formal_public_food.monthly_projection";

        private readonly CountyGovernanceSystem _countyGovernance;
        private readonly VillageLifeSystem _villageLife;

        public FormalPublicFoodMonthlyCommandScheduler(
            CountyGovernanceSystem countyGovernance,
            VillageLifeSystem villageLife)
        {
            _countyGovernance = countyGovernance ??
                throw new ArgumentNullException(nameof(countyGovernance));
            _villageLife = villageLife ??
                throw new ArgumentNullException(nameof(villageLife));
        }

        public int EnsureDueCommands(
            WorldState world,
            WorldCommandRuntime commandRuntime)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (commandRuntime == null)
            {
                throw new ArgumentNullException(nameof(commandRuntime));
            }
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                world.AbsoluteDay <= 0 ||
                world.AbsoluteDay % 30 != 0)
            {
                return 0;
            }

            var governances = new List<CountyGovernanceState>(
                world.CountyGovernances);
            governances.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var created = 0;
            for (var i = 0; i < governances.Count; i++)
            {
                var governance = governances[i];
                if (!_countyGovernance.HasFormalPublicFoodWork(
                        world, governance.Id))
                {
                    continue;
                }

                var commandId = MonthlyCommandId(
                    world.AbsoluteDay, governance.Id);
                if (HasCommand(world, commandId))
                {
                    continue;
                }

                commandRuntime.Enqueue(
                    world,
                    new WorldCommandEnvelope(
                        commandId,
                        CommandTypeId,
                        IssuerId,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment,
                        15,
                        new Dictionary<string, string>
                        {
                            {
                                ExpectedDayArgumentId,
                                Invariant(world.AbsoluteDay)
                            },
                            { GovernanceIdArgumentId, governance.Id }
                        }));
                created++;
            }
            return created;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new FormalPublicFoodMonthlyCommandHandler(
                _countyGovernance, _villageLife);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new FormalPublicFoodMonthlyProjectionHandler();

        public static string MonthlyCommandId(
            long day,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_public_food.monthly_command.{0:D10}.{1}",
                day,
                governanceId);

        public static string MonthlyTransactionId(
            long day,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_public_food.monthly_transaction.{0:D10}.{1}",
                day,
                governanceId);

        public static string MonthlyEventId(
            long day,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_public_food.monthly_resolved.{0:D10}.{1}",
                day,
                governanceId);

        public static string ReliefShortfallEventId(
            long day,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_public_food.county_relief_shortfall.{0:D10}.{1}",
                day,
                governanceId);

        private static string Invariant(long value) => value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        private static bool HasCommand(WorldState world, string commandId)
        {
            for (var i = 0; i < world.PersistentWorldCommands.Count; i++)
            {
                if (world.PersistentWorldCommands[i].Id == commandId)
                {
                    return true;
                }
            }
            return false;
        }

        private sealed class FormalPublicFoodMonthlyCommandHandler :
            IWorldCommandHandler
        {
            private readonly CountyGovernanceSystem _countyGovernance;
            private readonly VillageLifeSystem _villageLife;

            public FormalPublicFoodMonthlyCommandHandler(
                CountyGovernanceSystem countyGovernance,
                VillageLifeSystem villageLife)
            {
                _countyGovernance = countyGovernance;
                _villageLife = villageLife;
            }

            public string CommandTypeId =>
                FormalPublicFoodMonthlyCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 2 ||
                    !command.Arguments.TryGetValue(
                        ExpectedDayArgumentId,
                        out var expectedDayText) ||
                    !long.TryParse(
                        expectedDayText,
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var expectedDay) ||
                    expectedDay <= 0 ||
                    !command.Arguments.TryGetValue(
                        GovernanceIdArgumentId,
                        out var governanceId) ||
                    string.IsNullOrEmpty(governanceId))
                {
                    throw new InvalidOperationException(
                        "Formal public food monthly command arguments are invalid.");
                }
                _ = new StableId(governanceId);
                transactions.Add(new FormalPublicFoodMonthlyTransaction(
                    _countyGovernance,
                    _villageLife,
                    expectedDay,
                    governanceId));
            }
        }

        private sealed class FormalPublicFoodMonthlyTransaction :
            IWorldTransaction
        {
            private readonly CountyGovernanceSystem _countyGovernance;
            private readonly VillageLifeSystem _villageLife;
            private readonly long _expectedDay;
            private readonly string _governanceId;

            public FormalPublicFoodMonthlyTransaction(
                CountyGovernanceSystem countyGovernance,
                VillageLifeSystem villageLife,
                long expectedDay,
                string governanceId)
            {
                _countyGovernance = countyGovernance;
                _villageLife = villageLife;
                _expectedDay = expectedDay;
                _governanceId = governanceId;
                Id = MonthlyTransactionId(expectedDay, governanceId);
            }

            public string Id { get; }

            public string KindId => TransactionKindId;

            public int Priority => 15;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _countyGovernance.ValidateFormalPublicFoodMonthly(
                    world,
                    _villageLife,
                    _governanceId,
                    _expectedDay);
                validation.Reserve(
                    "formal_public_food.monthly." +
                        Invariant(_expectedDay) + "." + _governanceId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var result = _countyGovernance.ResolveFormalPublicFoodMonthly(
                    world,
                    _villageLife,
                    _governanceId,
                    _expectedDay);
                events.Add(new WorldRuntimeEvent(
                    MonthlyEventId(_expectedDay, _governanceId),
                    EventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
                if (result.ReliefShortfallQuantity > 0)
                {
                    events.Add(new WorldRuntimeEvent(
                        ReliefShortfallEventId(
                            _expectedDay, _governanceId),
                        ReliefShortfallEventTypeId,
                        Id,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment));
                }
            }
        }

        private sealed class FormalPublicFoodMonthlyProjectionHandler :
            IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;

            public string EventTypeId =>
                FormalPublicFoodMonthlyCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // The committed transaction owns inventory and fiscal writes.
                // This handler only acknowledges the projection boundary.
            }
        }
    }

    public sealed class PublicReliefProcurementResult
    {
        public string CountyGovernanceId;
        public long RequestedQuantity;
        public long PurchasedQuantity;
        public long MoneySpent;
        public long UnfilledQuantity;
    }

    public sealed class PublicReliefProcurementSystem
    {
        private readonly ProductionContentRegistry _productionContent;
        private readonly FoodInventorySystem _foodInventory;

        public PublicReliefProcurementSystem(
            ProductionContentRegistry productionContent = null)
        {
            _productionContent = productionContent ??
                ProductionContentRegistry.CreateCore();
            _foodInventory = new FoodInventorySystem(_productionContent);
        }

        public void Validate(
            WorldState world,
            string governanceId,
            string sourceShortfallEventId,
            long expectedDay,
            long maximumQuantity,
            long maximumBudget,
            long maximumUnitPrice)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            _productionContent.ValidateWorldReferences(world);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                expectedDay != world.AbsoluteDay ||
                expectedDay <= 0 ||
                maximumQuantity <= 0 ||
                maximumBudget <= 0 ||
                maximumUnitPrice <= 0)
            {
                throw new InvalidOperationException(
                    "Public relief procurement command is not valid for the current world time.");
            }

            var governance = FindGovernance(world, governanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var leader = FindPerson(world, government.LeaderPersonId);
            if (government.Type != OrganizationType.Government ||
                !leader.IsAlive ||
                string.IsNullOrEmpty(governance.GranaryInventoryContainerId))
            {
                throw new InvalidOperationException(
                    "Public relief procurement requires a living county government authority and a county granary.");
            }

            var sourceEvent = FindOutboxEvent(
                world, sourceShortfallEventId);
            if (sourceEvent.EventTypeId !=
                    FormalPublicFoodMonthlyCommandScheduler
                        .ReliefShortfallEventTypeId ||
                sourceEvent.Day != checked(expectedDay - 1) ||
                sourceEvent.SourceTransactionId !=
                    FormalPublicFoodMonthlyCommandScheduler
                        .MonthlyTransactionId(sourceEvent.Day, governanceId) ||
                ReliefShortfallOnDay(
                    world, governanceId, sourceEvent.Day) <= 0)
            {
                throw new InvalidOperationException(
                    "Public relief procurement lacks a matching committed county shortfall event.");
            }
        }

        public PublicReliefProcurementResult Resolve(
            WorldState world,
            string governanceId,
            string sourceShortfallEventId,
            string sourceCommandId,
            long maximumQuantity,
            long maximumBudget,
            long maximumUnitPrice)
        {
            var governance = FindGovernance(world, governanceId);
            var government = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var sourceEvent = FindOutboxEvent(
                world, sourceShortfallEventId);
            var result = new PublicReliefProcurementResult
            {
                CountyGovernanceId = governanceId,
                RequestedQuantity = Math.Min(
                    maximumQuantity,
                    ReliefShortfallOnDay(
                        world, governanceId, sourceEvent.Day))
            };
            var remaining = result.RequestedQuantity;
            var remainingBudget = Math.Min(maximumBudget, government.Treasury);
            var sellOrders = EligibleSellOrders(
                world, governanceId, maximumUnitPrice);
            for (var i = 0;
                 i < sellOrders.Count && remaining > 0 && remainingBudget > 0;
                 i++)
            {
                var sell = sellOrders[i];
                var affordable = remainingBudget / sell.UnitPrice;
                var requested = Math.Min(
                    remaining,
                    Math.Min(sell.RemainingQuantity, affordable));
                if (requested <= 0)
                {
                    continue;
                }

                var transfer = _foodInventory
                    .TransferReservedFamilyToCountyGranary(
                        world,
                        sell.OwnerFamilyId,
                        sell.StorageFacilityId,
                        governance.GranaryInventoryContainerId,
                        government.LeaderPersonId,
                        sell.BatchReservations,
                        requested,
                        sell.Id,
                        governance.Id);
                var quantity = transfer.TransferredPhysicalQuantity;
                if (quantity <= 0)
                {
                    continue;
                }

                var money = checked(quantity * sell.UnitPrice);
                var seller = FindFamily(world, sell.OwnerFamilyId);
                government.Treasury = checked(government.Treasury - money);
                seller.Wealth = checked(seller.Wealth + money);
                sell.RemainingQuantity = checked(
                    sell.RemainingQuantity - quantity);
                sell.FilledQuantity = checked(
                    sell.FilledQuantity + quantity);
                sell.SettledMoney = checked(sell.SettledMoney + money);
                if (sell.RemainingQuantity == 0)
                {
                    sell.Status = FormalMarketOrderStatus.Filled;
                    sell.ClosedDay = world.AbsoluteDay;
                    sell.CloseReason = "filled_by_public_relief_procurement";
                }

                var trade = new PublicReliefProcurementTradeState
                {
                    Id = $"public_relief_procurement_trade.{world.AbsoluteDay}." +
                         $"{world.PublicReliefProcurementTrades.Count:D6}",
                    Day = world.AbsoluteDay,
                    CountyGovernanceId = governance.Id,
                    SourceCountyGovernanceId = governance.Id,
                    BuyerOrganizationId = government.Id,
                    DestinationInventoryContainerId =
                        governance.GranaryInventoryContainerId,
                    SourceShortfallEventId = sourceShortfallEventId,
                    SourceCommandId = sourceCommandId,
                    SellOrderId = sell.Id,
                    SellerFamilyId = seller.Id,
                    ProductDefinitionId = sell.ProductDefinitionId,
                    Quantity = quantity,
                    UnitPrice = sell.UnitPrice,
                    MoneyTransferred = money,
                    InventoryTransactionId =
                        transfer.InventoryTransactionId,
                    CivilianFreightId = string.Empty,
                    FreightFee = 0,
                    PublicReliefRecoveryId = string.Empty,
                    IsSupplementalPublicReliefProcurement = false
                };
                world.PublicReliefProcurementTrades.Add(trade);
                UpdateMarketPrice(world, trade);
                AddProcurementLedger(
                    world,
                    governance,
                    trade,
                    CountyFiscalEntryType.GrainProcurement,
                    seller.Id,
                    money,
                    -money,
                    money,
                    "County government purchased reserved household food for relief stock.");
                result.PurchasedQuantity = checked(
                    result.PurchasedQuantity + quantity);
                result.MoneySpent = checked(result.MoneySpent + money);
                remaining = checked(remaining - quantity);
                remainingBudget = checked(remainingBudget - money);
            }

            result.UnfilledQuantity = remaining;
            if (remaining > 0)
            {
                AddProcurementLedger(
                    world,
                    governance,
                    null,
                    CountyFiscalEntryType.GrainProcurementUnfilled,
                    string.Empty,
                    0,
                    0,
                    remaining,
                    "Authorized county relief procurement remained unfilled.");
            }
            return result;
        }

        private List<FormalMarketOrderState> EligibleSellOrders(
            WorldState world,
            string governanceId,
            long maximumUnitPrice)
        {
            var result = new List<FormalMarketOrderState>();
            for (var i = 0; i < world.FormalMarketOrders.Count; i++)
            {
                var order = world.FormalMarketOrders[i];
                if (order.CountyGovernanceId == governanceId &&
                    order.Side == FormalMarketOrderSide.Sell &&
                    order.Status == FormalMarketOrderStatus.Active &&
                    order.ExpiryDay >= world.AbsoluteDay &&
                    order.RemainingQuantity > 0 &&
                    order.UnitPrice <= maximumUnitPrice &&
                    _productionContent.TryGetFood(
                        order.ProductDefinitionId, out _))
                {
                    result.Add(order);
                }
            }
            result.Sort((left, right) =>
            {
                var byPrice = left.UnitPrice.CompareTo(right.UnitPrice);
                if (byPrice != 0)
                {
                    return byPrice;
                }
                var byDay = left.CreatedDay.CompareTo(right.CreatedDay);
                if (byDay != 0)
                {
                    return byDay;
                }
                var byProduct = string.CompareOrdinal(
                    left.ProductDefinitionId,
                    right.ProductDefinitionId);
                return byProduct != 0
                    ? byProduct
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static long ReliefShortfallOnDay(
            WorldState world,
            string governanceId,
            long day)
        {
            long result = 0;
            for (var i = 0;
                 i < world.CountyFiscalLedgerEntries.Count;
                 i++)
            {
                var entry = world.CountyFiscalLedgerEntries[i];
                if (entry.Day == day &&
                    entry.CountyGovernanceId == governanceId &&
                    entry.Type == CountyFiscalEntryType.GrainReliefShortfall)
                {
                    result = checked(result + entry.Amount);
                }
            }
            return result;
        }

        private void UpdateMarketPrice(
            WorldState world,
            PublicReliefProcurementTradeState trade)
        {
            FormalMarketPriceState price = null;
            for (var i = 0; i < world.FormalMarketPrices.Count; i++)
            {
                var candidate = world.FormalMarketPrices[i];
                if (candidate.CountyGovernanceId ==
                        trade.CountyGovernanceId &&
                    candidate.ProductDefinitionId ==
                        trade.ProductDefinitionId)
                {
                    price = candidate;
                    break;
                }
            }
            if (price == null)
            {
                throw new InvalidOperationException(
                    "A public relief seller must already have a formal market price record.");
            }
            price.LastTradeUnitPrice = trade.UnitPrice;
            price.LastTradeDay = trade.Day;
            price.CumulativeTradedQuantity = checked(
                price.CumulativeTradedQuantity + trade.Quantity);
            price.CumulativeTurnover = checked(
                price.CumulativeTurnover + trade.MoneyTransferred);
        }

        private static void AddProcurementLedger(
            WorldState world,
            CountyGovernanceState governance,
            PublicReliefProcurementTradeState trade,
            CountyFiscalEntryType type,
            string familyId,
            long familyMoneyDelta,
            long governmentMoneyDelta,
            long amount,
            string summary)
        {
            var suffix = trade == null
                ? $"unfilled.{world.CountyFiscalLedgerEntries.Count:D6}"
                : trade.Id;
            world.CountyFiscalLedgerEntries.Add(
                new CountyFiscalLedgerEntryState
                {
                    Id = $"county_fiscal.{governance.Id}." +
                         $"{world.AbsoluteDay}.procurement.{suffix}",
                    Day = world.AbsoluteDay,
                    Type = type,
                    CountyGovernanceId = governance.Id,
                    FamilyId = familyId,
                    VillageId = string.Empty,
                    FamilyMoneyDelta = familyMoneyDelta,
                    GovernmentMoneyDelta = governmentMoneyDelta,
                    VillageGrainDelta = 0,
                    CountyGrainDelta = 0,
                    Amount = amount,
                    Summary = summary
                });
        }

        private static WorldEventOutboxState FindOutboxEvent(
            WorldState world,
            string eventId)
        {
            for (var i = 0; i < world.WorldEventOutbox.Count; i++)
            {
                if (world.WorldEventOutbox[i].Id == eventId)
                {
                    return world.WorldEventOutbox[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing public relief procurement source event {eventId}.");
        }

        private static CountyGovernanceState FindGovernance(
            WorldState world,
            string governanceId)
        {
            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                if (world.CountyGovernances[i].Id == governanceId)
                {
                    return world.CountyGovernances[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing county governance {governanceId}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string organizationId)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == organizationId)
                {
                    return world.Organizations[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing organization {organizationId}.");
        }

        private static PersonState FindPerson(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing person {personId}.");
        }

        private static FamilyState FindFamily(
            WorldState world,
            string familyId)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == familyId)
                {
                    return world.Families[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing family {familyId}.");
        }
    }

    public sealed class PublicReliefProcurementCommandScheduler
    {
        public const string CommandTypeId =
            PublicReliefProcurementContractIds.CommandTypeId;
        public const string IssuerId = "system.public_relief_procurement";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string GovernanceIdArgumentId = "county_governance_id";
        public const string SourceEventIdArgumentId =
            "source_shortfall_event_id";
        public const string MaximumQuantityArgumentId = "maximum_quantity";
        public const string MaximumBudgetArgumentId = "maximum_budget";
        public const string MaximumUnitPriceArgumentId = "maximum_unit_price";
        public const string TransactionKindId =
            "mandate.transaction.public_relief.procure_shortfall";
        public const string EventTypeId =
            "mandate.event.public_relief.procurement_resolved";
        public const string ExternalSourcingRequiredEventTypeId =
            PublicReliefProcurementContractIds
                .ExternalSourcingRequiredEventTypeId;
        public const string TriggerHandlerId =
            "mandate.handler.public_relief.shortfall_trigger";
        public const string ProjectionHandlerId =
            "mandate.handler.public_relief.procurement_projection";

        private const long DefaultMaximumQuantity = 10_000;
        private const long DefaultMaximumBudget = 100_000;
        private const long DefaultMaximumUnitPrice = 100;
        private readonly PublicReliefProcurementSystem _system;

        public PublicReliefProcurementCommandScheduler(
            PublicReliefProcurementSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new CommandHandler(_system);

        public IWorldRuntimeEventHandler CreateTriggerHandler() =>
            new TriggerHandler();

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new ProjectionHandler();

        public static string CommandId(
            long sourceDay,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "public_relief.procurement_command.{0:D10}.{1}",
                sourceDay,
                governanceId);

        public static string TransactionId(
            long expectedDay,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "public_relief.procurement_transaction.{0:D10}.{1}",
                expectedDay,
                governanceId);

        public static string EventId(
            long expectedDay,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "public_relief.procurement_resolved.{0:D10}.{1}",
                expectedDay,
                governanceId);

        public static string ExternalSourcingRequiredEventId(
            long expectedDay,
            string governanceId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "public_relief.external_sourcing_required.{0:D10}.{1}",
                expectedDay,
                governanceId);

        private static string Invariant(long value) => value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        private sealed class TriggerHandler : IWorldRuntimeEventHandler
        {
            public string HandlerId => TriggerHandlerId;

            public string EventTypeId =>
                FormalPublicFoodMonthlyCommandScheduler
                    .ReliefShortfallEventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                var prefix = FormalPublicFoodMonthlyCommandScheduler
                    .MonthlyTransactionId(worldEvent.Day, string.Empty);
                if (!worldEvent.SourceTransactionId.StartsWith(
                        prefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Public relief shortfall event has an invalid source transaction.");
                }
                var governanceId = worldEvent.SourceTransactionId.Substring(
                    prefix.Length);
                _ = new StableId(governanceId);
                var expectedDay = checked(worldEvent.Day + 1);
                commandRuntime.Enqueue(new WorldCommandEnvelope(
                    CommandId(worldEvent.Day, governanceId),
                    CommandTypeId,
                    IssuerId,
                    expectedDay,
                    DaySegment.Dawn,
                    5,
                    new Dictionary<string, string>
                    {
                        { ExpectedDayArgumentId, Invariant(expectedDay) },
                        { GovernanceIdArgumentId, governanceId },
                        { SourceEventIdArgumentId, worldEvent.Id },
                        {
                            MaximumQuantityArgumentId,
                            Invariant(DefaultMaximumQuantity)
                        },
                        {
                            MaximumBudgetArgumentId,
                            Invariant(DefaultMaximumBudget)
                        },
                        {
                            MaximumUnitPriceArgumentId,
                            Invariant(DefaultMaximumUnitPrice)
                        }
                    }));
            }
        }

        private sealed class CommandHandler : IWorldCommandHandler
        {
            private readonly PublicReliefProcurementSystem _system;

            public CommandHandler(PublicReliefProcurementSystem system)
            {
                _system = system;
            }

            public string CommandTypeId =>
                PublicReliefProcurementCommandScheduler.CommandTypeId;

            public void Plan(
                WorldCommandEnvelope command,
                WorldTransactionBuffer transactions)
            {
                if (command.Arguments.Count != 6 ||
                    !TryPositiveLong(
                        command, ExpectedDayArgumentId, out var expectedDay) ||
                    !TryStableId(
                        command, GovernanceIdArgumentId, out var governanceId) ||
                    !TryStableId(
                        command, SourceEventIdArgumentId, out var sourceEventId) ||
                    !TryPositiveLong(
                        command, MaximumQuantityArgumentId, out var maximumQuantity) ||
                    !TryPositiveLong(
                        command, MaximumBudgetArgumentId, out var maximumBudget) ||
                    !TryPositiveLong(
                        command, MaximumUnitPriceArgumentId, out var maximumUnitPrice))
                {
                    throw new InvalidOperationException(
                        "Public relief procurement command arguments are invalid.");
                }
                transactions.Add(new Transaction(
                    _system,
                    command.Id,
                    expectedDay,
                    governanceId,
                    sourceEventId,
                    maximumQuantity,
                    maximumBudget,
                    maximumUnitPrice));
            }

            private static bool TryPositiveLong(
                WorldCommandEnvelope command,
                string key,
                out long value)
            {
                value = 0;
                return command.Arguments.TryGetValue(key, out var text) &&
                    long.TryParse(
                    text,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value) && value > 0;
            }

            private static bool TryStableId(
                WorldCommandEnvelope command,
                string key,
                out string value)
            {
                if (!command.Arguments.TryGetValue(key, out value) ||
                    string.IsNullOrEmpty(value))
                {
                    return false;
                }
                _ = new StableId(value);
                return true;
            }
        }

        private sealed class Transaction : IWorldTransaction
        {
            private readonly PublicReliefProcurementSystem _system;
            private readonly string _sourceCommandId;
            private readonly long _expectedDay;
            private readonly string _governanceId;
            private readonly string _sourceEventId;
            private readonly long _maximumQuantity;
            private readonly long _maximumBudget;
            private readonly long _maximumUnitPrice;

            public Transaction(
                PublicReliefProcurementSystem system,
                string sourceCommandId,
                long expectedDay,
                string governanceId,
                string sourceEventId,
                long maximumQuantity,
                long maximumBudget,
                long maximumUnitPrice)
            {
                _system = system;
                _sourceCommandId = sourceCommandId;
                _expectedDay = expectedDay;
                _governanceId = governanceId;
                _sourceEventId = sourceEventId;
                _maximumQuantity = maximumQuantity;
                _maximumBudget = maximumBudget;
                _maximumUnitPrice = maximumUnitPrice;
                Id = TransactionId(expectedDay, governanceId);
            }

            public string Id { get; }

            public string KindId => TransactionKindId;

            public int Priority => 5;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _system.Validate(
                    world,
                    _governanceId,
                    _sourceEventId,
                    _expectedDay,
                    _maximumQuantity,
                    _maximumBudget,
                    _maximumUnitPrice);
                validation.Reserve(
                    "public_relief.procurement." + _sourceEventId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var result = _system.Resolve(
                    world,
                    _governanceId,
                    _sourceEventId,
                    _sourceCommandId,
                    _maximumQuantity,
                    _maximumBudget,
                    _maximumUnitPrice);
                events.Add(new WorldRuntimeEvent(
                    EventId(_expectedDay, _governanceId),
                    EventTypeId,
                    Id,
                    world.AbsoluteDay,
                    (DaySegment)world.Segment));
                if (result.UnfilledQuantity > 0)
                {
                    events.Add(new WorldRuntimeEvent(
                        ExternalSourcingRequiredEventId(
                            _expectedDay, _governanceId),
                        ExternalSourcingRequiredEventTypeId,
                        Id,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment));
                }
            }
        }

        private sealed class ProjectionHandler : IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;

            public string EventTypeId =>
                PublicReliefProcurementCommandScheduler.EventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // The committed transaction owns inventory and fiscal writes.
            }
        }
    }
}
