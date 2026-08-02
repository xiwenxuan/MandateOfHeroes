using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class CountyGovernanceSystem
    {
        private const int BasisPoints = 10_000;

        public void ResolveMonthly(WorldState world)
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

                ResolveCounty(world, governances[i]);
            }
        }

        private static void ResolveCounty(
            WorldState world,
            CountyGovernanceState governance)
        {
            var organization = FindOrganization(
                world, governance.GovernmentOrganizationId);
            var administrator = FindFamily(
                world, governance.AdministratorFamilyId);
            var villages = VillagesForCounty(world, governance.CountyLocationId);
            governance.LastMarketPressureBasisPoints =
                CalculateMarketPressure(world, governance.CountyLocationId);

            PayAdministrationStipend(
                world, governance, organization, administrator);
            var relief = DistributeRelief(world, governance, villages);

            var monthInYear =
                (int)(((world.AbsoluteDay / 30) - 1) % 12) + 1;
            if (monthInYear == 10)
            {
                CollectHouseholdCashTax(
                    world, governance, organization, villages);
                RemitVillageGrainTax(world, governance, villages);
            }

            ApplyPublicOrderFeedback(world, governance, villages, relief);
            governance.LastSettlementDay = world.AbsoluteDay;
            governance.NextSettlementDay = world.AbsoluteDay + 30;
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

        private static long DistributeRelief(
            WorldState world,
            CountyGovernanceState governance,
            List<VillageState> villages)
        {
            long total = 0;
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
                var issued = Math.Min(requested, governance.CountyGranaryGrain);
                if (issued <= 0)
                {
                    break;
                }

                governance.CountyGranaryGrain -= issued;
                governance.TotalReliefGrain += issued;
                village.PublicGranaryGrain += issued;
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

        private static void RemitVillageGrainTax(
            WorldState world,
            CountyGovernanceState governance,
            List<VillageState> villages)
        {
            for (var i = 0; i < villages.Count; i++)
            {
                var village = villages[i];
                long collectedToday = 0;
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

                var remitRate = BasisPoints -
                    governance.LocalGrainRetentionBasisPoints;
                var remittance = Math.Min(
                    village.PublicGranaryGrain,
                    collectedToday * remitRate / BasisPoints);
                if (remittance <= 0)
                {
                    continue;
                }

                village.PublicGranaryGrain -= remittance;
                governance.CountyGranaryGrain += remittance;
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
            string countyLocationId)
        {
            for (var i = 0; i < world.MarketListings.Count; i++)
            {
                var listing = world.MarketListings[i];
                if (listing.LocationId == countyLocationId &&
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
}
