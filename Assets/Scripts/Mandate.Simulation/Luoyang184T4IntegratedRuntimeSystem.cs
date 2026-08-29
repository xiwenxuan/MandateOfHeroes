using System;
using System.Collections.Generic;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    /// <summary>
    /// Low-frequency cross-system orchestration for the Luoyang T4 compact
    /// runtime. Permanent people, households, facilities and inventories remain
    /// the authoritative facts; these records only point at those facts.
    /// </summary>
    public sealed class Luoyang184T4IntegratedRuntimeSystem
    {
        public const int DaysPerYear = 360;
        public const long Year189Day = 5 * DaysPerYear;
        public const long Year190Day = 6 * DaysPerYear;
        private const long MarketTradingLotReserveMilliunits = 1_000;
        private static readonly LuoyangFormalEconomySystem FormalEconomy =
            new LuoyangFormalEconomySystem();

        public void Initialize(Luoyang184LivingWorldRuntimeState runtime)
        {
            BuildSocialRoles(runtime);
            BuildFamilyAssets(runtime);
            BuildPersonalDevelopment(runtime);
            BuildOffices(runtime);
            BuildGovernmentGranary(runtime);
            BuildMilitary(runtime);
            BuildEvents(runtime);
        }

        public void AdvanceDay(Luoyang184LivingWorldRuntimeState runtime)
        {
            ResolveRelocationTravel(runtime);
            if (runtime.AbsoluteDay % 30 == 0)
            {
                SettleFamilyEconomy(runtime);
                SettlePersonalLife(runtime);
                SettleGovernment(runtime);
                SettleMilitary(runtime);
                CaptureSocialPressure(runtime);
            }
            ResolveHistoricalEvents(runtime);
        }

        private static void ResolveRelocationTravel(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var person in runtime.Workforce.Where(item =>
                         item.TransitArrivalDay >= 0 &&
                         item.TransitArrivalDay <= runtime.AbsoluteDay))
            {
                person.CurrentLocationId = person.TransitDestinationId;
                person.TransitDestinationId = string.Empty;
                person.TransitArrivalDay = -1;
            }
            foreach (var inventory in runtime.Inventories.Where(item =>
                         item.TransitArrivalDay >= 0 &&
                         item.TransitArrivalDay <= runtime.AbsoluteDay))
            {
                inventory.CurrentLocationId = inventory.TransitDestinationId;
                inventory.TransitDestinationId = string.Empty;
                inventory.TransitArrivalDay = -1;
            }
            foreach (var force in runtime.Forces.Where(item =>
                         item.TransitArrivalDay >= 0 &&
                         item.TransitArrivalDay <= runtime.AbsoluteDay))
            {
                force.CurrentLocationId = force.TransitDestinationId;
                force.TransitDestinationId = string.Empty;
                force.TransitArrivalDay = -1;
            }
            if (runtime.GovernmentEconomy.CurrentLocationId ==
                    "route.luoyang_changan.in_transit" &&
                runtime.Offices.All(item =>
                    runtime.Workforce[(int)item.HolderPersonOrdinal]
                        .TransitArrivalDay < 0))
                runtime.GovernmentEconomy.CurrentLocationId =
                    "location.capital.changan";
            foreach (var development in runtime.PersonDevelopment)
            {
                var person = runtime.Workforce[(int)development.PersonOrdinal];
                development.CurrentLocationId = person.CurrentLocationId;
            }
        }

        private static void BuildSocialRoles(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var person in runtime.Workforce)
            {
                person.SocialRoleId = Role(runtime, person);
                person.CurrentActivityId = person.Status == LuoyangWorkforceStatus.Assigned
                    ? "activity.work" : person.Status == LuoyangWorkforceStatus.Official
                        ? "activity.government_work" : person.Status ==
                          LuoyangWorkforceStatus.MilitaryDuty
                            ? "activity.military_service" : person.Status ==
                              LuoyangWorkforceStatus.Student
                                ? "activity.study" : "activity.household_life";
            }
        }

        private static string Role(Luoyang184LivingWorldRuntimeState runtime,
            LuoyangWorkforceAssignmentState person)
        {
            if (person.Status == LuoyangWorkforceStatus.Official) return "role.official";
            if (person.Status == LuoyangWorkforceStatus.MilitaryDuty) return "role.soldier";
            if (person.Status == LuoyangWorkforceStatus.Student) return "role.student";
            if (person.Status == LuoyangWorkforceStatus.FamilyManagement) return "role.family_manager";
            if (person.Status == LuoyangWorkforceStatus.Unemployed) return "role.unemployed";
            if (person.Status != LuoyangWorkforceStatus.Assigned) return "role.household_dependent";
            var facility = person.FacilityIndex < runtime.Facilities.Count
                ? runtime.Facilities[(int)person.FacilityIndex] : null;
            var id = facility?.DefinitionId ?? string.Empty;
            if (id.IndexOf("field", StringComparison.OrdinalIgnoreCase) >= 0)
                return "role.farmer";
            if (id.IndexOf("market", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0)
                return "role.merchant";
            if (id.IndexOf("medical", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("clinic", StringComparison.OrdinalIgnoreCase) >= 0)
                return "role.physician";
            if (id.IndexOf("school", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("academy", StringComparison.OrdinalIgnoreCase) >= 0)
                return "role.scholar";
            if (id.IndexOf("transport", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("stable", StringComparison.OrdinalIgnoreCase) >= 0)
                return "role.transporter";
            return "role.artisan";
        }

        private static void BuildFamilyAssets(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var family in runtime.FamilyOrganizations)
            {
                foreach (var property in runtime.CellProperties.Where(item =>
                             item.OwnerId == family.Id))
                    AddFamilyAsset(runtime, family.Id, "asset.cell",
                        property.CellId64.ToString(), property.OwnerId,
                        property.BuildingRightHolderId);
                foreach (var facility in runtime.Facilities.Where(item =>
                             item.OwnerId == family.Id))
                    AddFamilyAsset(runtime, family.Id, "asset.facility",
                        facility.FacilityId, facility.OwnerId, facility.OwnerId);
                foreach (var inventory in runtime.Inventories.Where(item =>
                             item.OwnerId == family.Id))
                    AddFamilyAsset(runtime, family.Id, "asset.inventory",
                        inventory.Id, inventory.OwnerId, inventory.OwnerId);
            }
        }

        private static void AddFamilyAsset(Luoyang184LivingWorldRuntimeState runtime,
            string familyId, string kind, string assetId, string owner, string controller)
        {
            var id = "family_asset." + familyId + "." + kind + "." + assetId;
            if (runtime.FamilyAssets.Exists(item => item.Id == id)) return;
            runtime.FamilyAssets.Add(new LuoyangFamilyAssetRuntimeState
            {
                Id = id, FamilyOrganizationId = familyId, AssetKindId = kind,
                AssetId = assetId, OwnerId = owner, ControllerId = controller,
                AcquiredDay = runtime.AbsoluteDay
            });
        }

        private static void BuildPersonalDevelopment(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var candidates = runtime.Workforce.Where(item =>
                    item.Status == LuoyangWorkforceStatus.Student ||
                    item.Status == LuoyangWorkforceStatus.Official ||
                    item.Status == LuoyangWorkforceStatus.FamilyManagement)
                .OrderBy(item => item.PersonOrdinal).Take(256).ToList();
            foreach (var person in candidates)
            {
                var household = runtime.Households[(int)person.HouseholdOrdinal];
                var residence = household.ResidenceFacilityIndex < runtime.Facilities.Count
                    ? runtime.Facilities[(int)household.ResidenceFacilityIndex].FacilityId
                    : string.Empty;
                runtime.PersonDevelopment.Add(new LuoyangPersonDevelopmentRuntimeState
                {
                    PersonOrdinal = person.PersonOrdinal,
                    CurrentActivityId = person.CurrentActivityId,
                    ResidenceFacilityId = residence,
                    SocialRoleId = person.SocialRoleId
                });
            }
            var educationFacility = runtime.Facilities.OrderBy(item =>
                    item.DefinitionId.IndexOf("education", StringComparison.OrdinalIgnoreCase) >= 0
                        ? 0 : 1).ThenBy(item => item.FacilityId,
                    StringComparer.Ordinal).First();
            var books = runtime.Inventories.Find(item =>
                item.ProductId == "product.book.classics");
            if (books == null)
            {
                books = new LuoyangInventoryBalanceState
                {
                    Id = "inventory.luoyang.184.imperial_library.classics",
                    OwnerKind = LuoyangInventoryOwnerKind.Government,
                    OwnerId = runtime.GovernmentEconomy.OrganizationId,
                    FacilityId = educationFacility.FacilityId,
                    ProductId = "product.book.classics",
                    CapacityMilliunits = 1_000_000,
                    QuantityMilliunits = 100_000
                };
                runtime.Inventories.Add(books);
            }
            foreach (var person in runtime.PersonDevelopment.Take(32))
                person.BookInventoryIds.Add(books.Id);
        }

        private static void BuildOffices(Luoyang184LivingWorldRuntimeState runtime)
        {
            var officials = runtime.Workforce.Where(item =>
                    item.Status == LuoyangWorkforceStatus.Official)
                .OrderBy(item => item.PersonOrdinal).ToList();
            var government = runtime.Facilities.Where(item =>
                    item.OwnerId == runtime.GovernmentEconomy.OrganizationId)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).ToList();
            var officeKinds = new[]
            {
                "office.central_government", "office.henan_yin",
                "office.county_administration", "office.imperial_court",
                "office.military_command"
            };
            for (var i = 0; i < officeKinds.Length && i < officials.Count; i++)
                runtime.Offices.Add(new LuoyangOfficeRuntimeState
                {
                    Id = officeKinds[i] + ".luoyang.184",
                    OfficeKindId = officeKinds[i],
                    JurisdictionId = i == 0 || i == 3
                        ? "jurisdiction.han.central" : "jurisdiction.luoyang",
                    AuthorityId = i == 4 ? "authority.military" :
                        "authority.government",
                    GovernmentFacilityId = government.Count == 0 ? string.Empty :
                        government[i % government.Count].FacilityId,
                    HolderPersonOrdinal = officials[i].PersonOrdinal,
                    CurrentActivityId = "activity.government_work"
                });
        }

        private static void BuildMilitary(Luoyang184LivingWorldRuntimeState runtime)
        {
            var soldiers = runtime.Workforce.Count(item =>
                item.Status == LuoyangWorkforceStatus.MilitaryDuty);
            var militaryFacilities = runtime.Facilities.Where(item =>
                    item.DefinitionId.IndexOf("military", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.DefinitionId.IndexOf("barracks", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.DefinitionId.IndexOf("arsenal", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(item => item.FacilityId, StringComparer.Ordinal).ToList();
            var force = new LuoyangMilitaryForceRuntimeState
            {
                Id = "force.han.luoyang.garrison.184",
                OrganizationId = "organization.military.han.luoyang",
                BarracksFacilityId = militaryFacilities.FirstOrDefault()?.FacilityId ?? string.Empty,
                ArsenalFacilityId = militaryFacilities.Skip(1).FirstOrDefault()?.FacilityId ?? string.Empty,
                PermanentPersonCount = soldiers,
                DefenseBasisPoints = 7_000
            };
            var productIds = new[] { "product.food.dry_ration", "product.feed.horse",
                "product.weapon.general", "product.armor.general", "product.weapon.arrow",
                "product.textile.plain_cloth", "product.reference.tools",
                "product.transport.pack_animal" };
            foreach (var product in productIds)
            {
                var inventory = new LuoyangInventoryBalanceState
                {
                    Id = "inventory.military.luoyang.184." + product,
                    OwnerKind = LuoyangInventoryOwnerKind.Military,
                    OwnerId = force.OrganizationId,
                    FacilityId = string.IsNullOrEmpty(force.ArsenalFacilityId)
                        ? force.BarracksFacilityId : force.ArsenalFacilityId,
                    ProductId = product,
                    CapacityMilliunits = 100_000_000
                };
                runtime.Inventories.Add(inventory);
                force.InventoryIds.Add(inventory.Id);
            }
            runtime.Forces.Add(force);
        }

        private static void BuildGovernmentGranary(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            if (!string.IsNullOrEmpty(runtime.GovernmentEconomy.GranaryInventoryId))
                return;
            var facility = runtime.Facilities.Where(item =>
                    item.OwnerId == runtime.GovernmentEconomy.OrganizationId &&
                    runtime.Inventories.Any(inventory =>
                        inventory.FacilityId == item.FacilityId &&
                        inventory.CapacityMilliunits > 0))
                .OrderByDescending(item => runtime.Inventories.Where(inventory =>
                    inventory.FacilityId == item.FacilityId).Sum(inventory =>
                    inventory.CapacityMilliunits))
                .ThenBy(item => item.FacilityId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (facility == null) return;
            var granary = new LuoyangInventoryBalanceState
            {
                Id = "inventory.government.luoyang.184.grain_tax",
                OwnerKind = LuoyangInventoryOwnerKind.Government,
                OwnerId = runtime.GovernmentEconomy.OrganizationId,
                FacilityId = facility.FacilityId,
                ProductId = "product.reference.food_equivalent",
                CapacityMilliunits = Math.Max(1_000_000L,
                    runtime.Inventories.Where(item =>
                        item.FacilityId == facility.FacilityId).Sum(item =>
                        item.CapacityMilliunits))
            };
            runtime.Inventories.Add(granary);
            runtime.GovernmentEconomy.GranaryInventoryId = granary.Id;
        }

        private static void BuildEvents(Luoyang184LivingWorldRuntimeState runtime)
        {
            runtime.HistoricalEvents.Add(new LuoyangHistoricalEventRuntimeState
            {
                Id = "event_runtime.luoyang.189.palace_crisis",
                DefinitionId = "historical_event.han.189.palace_crisis",
                StatusId = "watching", EarliestDay = Year189Day
            });
            runtime.HistoricalEvents.Add(new LuoyangHistoricalEventRuntimeState
            {
                Id = "event_runtime.luoyang.190.relocation_and_destruction",
                DefinitionId = "historical_event.han.190.luoyang_relocation",
                StatusId = "watching", EarliestDay = Year190Day
            });
        }

        private static void SettleFamilyEconomy(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var family in runtime.FamilyOrganizations)
            {
                family.AssetValue = Math.Max(family.AssetValue,
                    runtime.FamilyAssets.Count(item =>
                        item.FamilyOrganizationId == family.Id) * 100L);
            }
        }

        private static void SettlePersonalLife(Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var person in runtime.PersonDevelopment)
            {
                person.StudyMinutes += 600;
                person.TrainingMinutes += 120;
                person.KnowledgeBasisPoints = Math.Min(10_000,
                    person.KnowledgeBasisPoints + 5);
                person.SkillBasisPoints = Math.Min(10_000,
                    person.SkillBasisPoints + 2);
                if (person.StudyMinutes >= 3_600 &&
                    !person.KnownRecipeIds.Contains("recipe.knowledge.basic"))
                    person.KnownRecipeIds.Add("recipe.knowledge.basic");
            }
        }

        private static void SettleGovernment(Luoyang184LivingWorldRuntimeState runtime)
        {
            long moneyTax = 0;
            var taxDay = runtime.AbsoluteDay;
            foreach (var household in runtime.Households)
            {
                var due = Math.Min(household.Wealth, Math.Max(0, household.Wealth / 100));
                if (due <= 0) continue;
                household.Wealth -= due;
                household.CumulativeMoneyTaxPaid += due;
                moneyTax += due;
            }
            if (moneyTax > 0)
                runtime.Taxes.Add(new LuoyangTaxRuntimeState
                {
                    Id = "tax.money.monthly_batch." + taxDay,
                    Day = taxDay,
                    TaxKindId = "tax.money.household.monthly_batch",
                    PayerId = "households.luoyang.184",
                    GovernmentId = runtime.GovernmentEconomy.OrganizationId,
                    MoneyPaid = moneyTax
                });
            runtime.GovernmentEconomy.Treasury += moneyTax;
            runtime.GovernmentEconomy.TaxRevenue += moneyTax;
            CollectInKindTax(runtime, taxDay);
            var salaries = runtime.Offices.Count * 100L;
            salaries = Math.Min(salaries, runtime.GovernmentEconomy.Treasury);
            runtime.GovernmentEconomy.Treasury -= salaries;
            var perOffice = runtime.Offices.Count == 0 ? 0 :
                salaries / runtime.Offices.Count;
            var remainder = runtime.Offices.Count == 0 ? 0 :
                salaries % runtime.Offices.Count;
            for (var index = 0; index < runtime.Offices.Count; index++)
            {
                var office = runtime.Offices[index];
                var paid = perOffice + (index < remainder ? 1 : 0);
                office.SalaryExpense += paid;
                var person = runtime.Workforce[(int)office.HolderPersonOrdinal];
                runtime.Households[(int)person.HouseholdOrdinal].Wealth += paid;
            }
        }

        private static void CollectInKindTax(
            Luoyang184LivingWorldRuntimeState runtime, long taxDay)
        {
            var granary = runtime.Inventories.Find(item =>
                item.Id == runtime.GovernmentEconomy.GranaryInventoryId);
            if (granary == null) return;
            long total = FormalEconomy.CollectHouseholdTax(runtime, granary.Id,
                Math.Max(0L, granary.CapacityMilliunits -
                             granary.QuantityMilliunits),
                "government.tax.inkind.household_to_granary." + taxDay);
            if (granary.QuantityMilliunits < granary.CapacityMilliunits)
            {
                foreach (var source in runtime.Inventories.Where(item =>
                             item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                             (item.ProductId.IndexOf("food", StringComparison.Ordinal) >= 0 ||
                              item.ProductId.IndexOf("grain", StringComparison.Ordinal) >= 0) &&
                             item.QuantityMilliunits > 0)
                         .OrderBy(item => item.Id, StringComparer.Ordinal))
                {
                    var due = Math.Max(0L,
                        (source.QuantityMilliunits + 99) / 100);
                    var paid = Math.Min(due,
                        granary.CapacityMilliunits - granary.QuantityMilliunits);
                    if (paid <= 0) continue;
                    var transferred = FormalEconomy.Transfer(runtime, source.Id,
                        granary.Id, source.ProductId, paid,
                        InventoryTransactionType.FoodTaxTransferred,
                        "government.tax.inkind.market_to_granary." + taxDay +
                        "." + source.Id);
                    total += transferred;
                    if (granary.QuantityMilliunits >= granary.CapacityMilliunits)
                        break;
                }
            }
            if (total <= 0) return;
            runtime.Taxes.Add(new LuoyangTaxRuntimeState
            {
                Id = "tax.inkind.monthly_batch." + taxDay,
                Day = taxDay,
                TaxKindId = "tax.inkind.household.grain.monthly_batch",
                PayerId = "households_and_markets.luoyang.184",
                GovernmentId = runtime.GovernmentEconomy.OrganizationId,
                ProductId = granary.ProductId,
                ProductQuantityMilliunits = total,
                DestinationInventoryId = granary.Id
            });
            runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
            {
                Id = "flow.tax.inkind." + taxDay,
                Day = taxDay,
                OperationId = "government.tax.inkind.household_to_granary",
                ProductId = granary.ProductId,
                SourceInventoryId = "household.compact_reserves",
                DestinationInventoryId = granary.Id,
                QuantityMilliunits = total
            });
        }

        public void ProcureMilitaryFood(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            ProcureMilitaryFood(runtime, true);
        }

        private void ProcureMilitaryFood(
            Luoyang184LivingWorldRuntimeState runtime,
            bool dailyCadence)
        {
            foreach (var force in runtime.Forces)
            {
                var food = runtime.Inventories.Find(item =>
                    force.InventoryIds.Contains(item.Id) &&
                    item.ProductId == "product.food.dry_ration");
                var demand = force.PermanentPersonCount * 30L;
                var foodAvailable = food == null ? 0 :
                    LuoyangFormalEconomySystem.GetAvailableQuantity(runtime,
                        food.Id);
                var needed = Math.Max(0, demand - foodAvailable);
                if (needed > 0 && food != null)
                {
                    var market = runtime.Inventories.Where(item =>
                            item.OwnerKind == LuoyangInventoryOwnerKind.Market &&
                            (item.ProductId.IndexOf("food", StringComparison.Ordinal) >= 0 ||
                             item.ProductId.IndexOf("grain", StringComparison.Ordinal) >= 0) &&
                            item.QuantityMilliunits >
                            MarketTradingLotReserveMilliunits)
                        .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (market != null)
                    {
                        var purchasable = Math.Max(0,
                            LuoyangFormalEconomySystem.GetAvailableQuantity(
                                runtime, market.Id, market.ProductId) -
                            MarketTradingLotReserveMilliunits);
                        var moved = Math.Min(needed, purchasable);
                        if (dailyCadence)
                        {
                            // Daily procurement smooths government demand without
                            // letting the garrison pre-empt an entire month's
                            // market stock before households and player trades can
                            // settle.  The month-end settlement below may buy the
                            // remaining formal requirement before consumption.
                            var dailyDemand = checked((demand + 29) / 30);
                            moved = Math.Min(moved, dailyDemand);
                        }
                        var marketState = runtime.Markets.Find(item =>
                            item.ProductId == market.ProductId);
                        var unitPrice = Math.Max(1L,
                            (marketState?.BasePrice ?? 1) *
                            (marketState?.CurrentPriceBasisPoints ?? 10_000) /
                            10_000L);
                        moved = Math.Min(moved,
                            runtime.GovernmentEconomy.Treasury * 1_000 / unitPrice);
                        moved = FormalEconomy.Transfer(runtime, market.Id,
                            food.Id, market.ProductId, moved,
                            InventoryTransactionType.FoodMarketTransferred,
                            "military.procurement.local_batch." +
                            runtime.AbsoluteDay + "." + force.Id);
                        if (moved <= 0) continue;
                        var cost = checked((moved * unitPrice + 999) / 1_000);
                        runtime.GovernmentEconomy.Treasury -= cost;
                        runtime.GovernmentEconomy.PurchaseExpense += cost;
                        if (marketState != null) marketState.CashBalance += cost;
                        runtime.InventoryFlows.Add(new LuoyangInventoryFlowState
                        {
                            Id = "flow.military.procurement." + runtime.AbsoluteDay,
                            Day = runtime.AbsoluteDay,
                            OperationId = "military.procurement.local_batch",
                            ProductId = market.ProductId,
                            SourceInventoryId = market.Id,
                            DestinationInventoryId = food.Id,
                            QuantityMilliunits = moved
                        });
                    }
                }
            }
        }

        private void SettleMilitary(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            ProcureMilitaryFood(runtime, false);
            foreach (var force in runtime.Forces)
            {
                var food = runtime.Inventories.Find(item =>
                    force.InventoryIds.Contains(item.Id) &&
                    item.ProductId == "product.food.dry_ration");
                var demand = force.PermanentPersonCount * 30L;
                var consumed = food == null ? 0 :
                    FormalEconomy.ConsumeInventory(runtime, food.Id, null,
                        demand, InventoryTransactionType.FoodConsumed,
                        "military.food_consumption." + runtime.AbsoluteDay +
                        "." + force.Id);
                force.FoodConsumedMilliunits += consumed;
                force.DefenseBasisPoints = consumed < demand ?
                    Math.Max(0, force.DefenseBasisPoints - 100) :
                    Math.Min(10_000, force.DefenseBasisPoints + 10);
            }
        }

        private static void CaptureSocialPressure(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            var food = runtime.DailyFoodDemandMilliunits <= 0 ? 0 :
                (int)Math.Min(10_000, runtime.Households.Sum(item =>
                    item.CumulativeFoodShortageMilliunits) * 10_000 /
                    Math.Max(1L, runtime.Households.Sum(item =>
                        item.CumulativeFoodDemandMilliunits)));
            var unemployment = runtime.Workforce.Count == 0 ? 0 :
                runtime.CurrentUnemployedCount * 10_000 / runtime.Workforce.Count;
            var war = runtime.AbsoluteDay >= Year189Day ? 3_000 : 500;
            var displacement = runtime.HistoricalEvents.Exists(item =>
                item.DefinitionId.Contains("relocation") && item.ResolvedDay >= 0) ? 8_000 : 0;
            var composite = (food * 4 + unemployment * 2 + war * 3 + displacement) / 10;
            runtime.SocialPressureHistory.Add(new LuoyangSocialPressureRuntimeState
            {
                Day = runtime.AbsoluteDay, FoodShortageBasisPoints = food,
                UnemploymentBasisPoints = unemployment, WarBasisPoints = war,
                DisplacementBasisPoints = displacement,
                CompositeBasisPoints = composite,
                PublicOrderStatusId = composite >= 7_500 ? "public_order.critical" :
                    composite >= 4_000 ? "public_order.unstable" : "public_order.stable"
            });
        }

        private static void ResolveHistoricalEvents(
            Luoyang184LivingWorldRuntimeState runtime)
        {
            foreach (var historicalEvent in runtime.HistoricalEvents.Where(item =>
                         item.ResolvedDay < 0 && runtime.AbsoluteDay >= item.EarliestDay)
                     .OrderBy(item => item.EarliestDay))
            {
                var pressure = runtime.SocialPressureHistory.LastOrDefault()
                    ?.CompositeBasisPoints ?? 0;
                if (historicalEvent.DefinitionId.Contains("189"))
                {
                    historicalEvent.OutcomeId = pressure >= 7_500 ? "transformed" :
                        pressure >= 4_000 ? "variant" : "canonical";
                    runtime.GovernmentEconomy.CurrentDevelopmentPolicyId =
                        "government.policy.palace_crisis_response";
                    historicalEvent.AppliedChangeIds.Add("government.policy_changed");
                }
                else
                {
                    var prevented = runtime.Forces.Any(item =>
                        item.DefenseBasisPoints >= 9_500 && !item.GatesClosed);
                    historicalEvent.OutcomeId = prevented ? "prevented" :
                        pressure < 3_000 ? "delayed" : "canonical";
                    if (historicalEvent.OutcomeId == "delayed" &&
                        runtime.AbsoluteDay < historicalEvent.EarliestDay + 180)
                        continue;
                    if (!prevented)
                    {
                        foreach (var facility in runtime.Facilities.Where(item =>
                                     item.OwnerId == runtime.GovernmentEconomy.OrganizationId)
                                 .OrderBy(item => item.FacilityId,
                                     StringComparer.Ordinal).Take(10))
                        {
                            facility.ConditionBasisPoints = Math.Min(
                                facility.ConditionBasisPoints, 3_000);
                            facility.Status = LuoyangProductionRuntimeStatus.Maintenance;
                        }
                        foreach (var household in runtime.Households.Take(1_000))
                            household.ResidenceFacilityIndex = uint.MaxValue;
                        StartRelocation(runtime, historicalEvent);
                        historicalEvent.AppliedChangeIds.Add("facility.damage.real");
                        historicalEvent.AppliedChangeIds.Add("household.displacement.real");
                    }
                }
                historicalEvent.StatusId = historicalEvent.OutcomeId;
                historicalEvent.ResolvedDay = runtime.AbsoluteDay;
                historicalEvent.AppliedOffscreen = true;
            }
        }

        private static void StartRelocation(
            Luoyang184LivingWorldRuntimeState runtime,
            LuoyangHistoricalEventRuntimeState historicalEvent)
        {
            const string destination = "location.capital.changan";
            var arrivalDay = runtime.AbsoluteDay + 30;
            runtime.GovernmentEconomy.CurrentLocationId =
                "route.luoyang_changan.in_transit";
            var movedPeople = new HashSet<uint>();
            foreach (var office in runtime.Offices)
            {
                var person = runtime.Workforce[(int)office.HolderPersonOrdinal];
                movedPeople.Add(person.PersonOrdinal);
                person.TransitDestinationId = destination;
                person.TransitArrivalDay = arrivalDay;
                person.CurrentActivityId = "activity.government_relocation";
                var development = runtime.PersonDevelopment.FirstOrDefault(item =>
                    item.PersonOrdinal == person.PersonOrdinal);
                if (development != null)
                {
                    development.CurrentActivityId =
                        "activity.government_relocation";
                    development.CurrentLocationId =
                        "route.luoyang_changan.in_transit";
                }
            }
            var militaryPeople = runtime.Workforce.Where(item =>
                    item.Status == LuoyangWorkforceStatus.MilitaryDuty)
                .OrderBy(item => item.PersonOrdinal).Take(runtime.Forces.Sum(item =>
                    item.PermanentPersonCount));
            foreach (var person in militaryPeople)
            {
                movedPeople.Add(person.PersonOrdinal);
                person.TransitDestinationId = destination;
                person.TransitArrivalDay = arrivalDay;
                person.CurrentActivityId = "activity.military_relocation";
            }
            runtime.CurrentLocalPopulation = Math.Max(0,
                runtime.CurrentLocalPopulation - movedPeople.Count);
            foreach (var force in runtime.Forces)
            {
                force.TransitDestinationId = destination;
                force.TransitArrivalDay = arrivalDay;
            }
            foreach (var inventory in runtime.Inventories.Where(item =>
                         item.OwnerKind == LuoyangInventoryOwnerKind.Government ||
                         item.OwnerKind == LuoyangInventoryOwnerKind.Military))
            {
                inventory.TransitDestinationId = destination;
                inventory.TransitArrivalDay = arrivalDay;
            }
            historicalEvent.AppliedChangeIds.Add("government.relocation.travel");
            historicalEvent.AppliedChangeIds.Add("office_holders.relocation.travel");
            historicalEvent.AppliedChangeIds.Add("soldiers.relocation.travel");
            historicalEvent.AppliedChangeIds.Add("government_inventory.relocation.travel");
            historicalEvent.AppliedChangeIds.Add("military_force.relocation.travel");
        }
    }
}
