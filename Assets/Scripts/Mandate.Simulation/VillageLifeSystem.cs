using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public enum VillageAttentionLevel : byte
    {
        None,
        Normal,
        Deep
    }

    public sealed class VillageAttentionReport
    {
        public string VillageId;
        public VillageAttentionLevel AttentionLevel;
        public int PermanentPeople;
        public int LivingResidents;
        public int Households;
        public int WorkingResidents;
        public long FamilyGrain;
        public long PublicGranaryGrain;
        public int FoodSecurityBasisPoints;
        public readonly List<string> HouseholdDetails = new List<string>();
        public readonly List<string> RecentEvents = new List<string>();
    }

    public sealed class VillageLifeAudit
    {
        public int PermanentPeople;
        public int LivingResidents;
        public int HouseholdMembers;
        public int Households;
        public long FamilyGrain;
        public long PublicGranaryGrain;
        public int InvalidFamilyReferences;
        public int AbstractPopulation;

        public bool IsValid =>
            InvalidFamilyReferences == 0 &&
            FamilyGrain >= 0 &&
            PublicGranaryGrain >= 0;
    }

    public sealed class FormalHouseholdFoodShortfallResult
    {
        public string FamilyId;
        public long RequiredNutritionBasisUnits;
        public long ProvidedNutritionBasisUnits;
        public long MissingNutritionBasisUnits;
        public readonly List<FormalHouseholdFoodAffectedPersonResult>
            AffectedPeople =
                new List<FormalHouseholdFoodAffectedPersonResult>();
    }

    public sealed class FormalHouseholdFoodAffectedPersonResult
    {
        public string PersonId;
        public bool RequiresCaregiverDelivery;
        public long RequiredNutritionBasisUnits;
        public long AllocatedNutritionBasisUnits;
        public int AppliedHealthDamageBasisPoints;
        public int AppliedLivelihoodPressureBasisPoints;
    }

    public sealed class FormalHouseholdFoodPersonSettlementResult
    {
        public string PersonId;
        public long RequiredNutritionBasisUnits;
        public long MissingNutritionBasisUnits;
    }

    public sealed class FormalHouseholdFoodSettlementResult
    {
        public string VillageId;
        public int HouseholdsProcessed;
        public long RequiredNutritionBasisUnits;
        public long ProvidedNutritionBasisUnits;
        public long ConsumedPhysicalQuantity;
        public readonly List<string> ShortfallFamilyIds = new List<string>();
        public readonly List<FormalHouseholdFoodShortfallResult> Shortfalls =
            new List<FormalHouseholdFoodShortfallResult>();
        public readonly List<FormalHouseholdFoodPersonSettlementResult>
            PersonSettlements =
                new List<FormalHouseholdFoodPersonSettlementResult>();

        public bool HasShortfall => ShortfallFamilyIds.Count > 0;
    }

    public sealed class VillageLifeSystem
    {
        private const int DaysPerYear = 360;
        private readonly NamedRandom _random;
        private readonly IPersonRepository _people;
        private readonly PopulationLedgerSystem _population;
        private readonly AgricultureProductionSystem _agricultureProduction;
        private readonly ProductionContentRegistry _productionContent;
        private readonly FoodInventorySystem _foodInventory;
        private readonly CivilianMedicalSystem _civilianMedical;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public VillageLifeSystem(
            ulong masterSeed,
            ProductionContentRegistry productionContent = null,
            IPersonRepository people = null)
        {
            _random = new NamedRandom(masterSeed);
            _people = people;
            _population = new PopulationLedgerSystem(people);
            _productionContent = productionContent ??
                ProductionContentRegistry.CreateCore();
            _foodInventory = new FoodInventorySystem(_productionContent);
            _civilianMedical = new CivilianMedicalSystem(
                _productionContent, people);
            _agricultureProduction =
                new AgricultureProductionSystem(
                    masterSeed, _productionContent, people);
        }

        public void ResolveMonthly(WorldState world)
        {
            ResolveMonthlyCore(world, true, true);
        }

        public void ResolveMonthlyAfterFormalFoodCommands(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (world.FoodInventoryAuthorityMode !=
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                throw new InvalidOperationException(
                    "Only formal food worlds can skip committed food settlement.");
            }
            ResolveMonthlyCore(world, false, true);
        }

        public void ResolveMonthlyAfterFormalFoodAndPublicFoodCommands(
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
                    "Only formal food worlds can skip committed food and tax settlement.");
            }
            ResolveMonthlyCore(world, false, false);
        }

        public FormalHouseholdFoodSettlementResult ResolveFormalFoodMonthly(
            WorldState world,
            string villageId,
            long expectedDay)
        {
            ValidateFormalFoodMonthly(world, villageId, expectedDay);
            var village = FindVillage(world, villageId);
            return ResolveFormalFood(
                world,
                village,
                FamiliesForVillage(world, village));
        }

        public void ValidateFormalFoodMonthly(
            WorldState world,
            string villageId,
            long expectedDay)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            _productionContent.ValidateWorldReferences(world);
            var village = FindVillage(world, villageId);
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                expectedDay != world.AbsoluteDay ||
                expectedDay <= 0 ||
                expectedDay % 30 != 0 ||
                village.LastSettlementDay == expectedDay ||
                village.HouseholdIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Formal household food monthly settlement is not due.");
            }
        }

        public long ResolveFormalTaxMonthly(
            WorldState world,
            string villageId,
            long expectedDay)
        {
            ValidateFormalTaxMonthly(world, villageId, expectedDay);
            var village = FindVillage(world, villageId);
            var before = village.TaxGrainCollected;
            ResolveTax(world, village, FamiliesForVillage(world, village));
            return checked(village.TaxGrainCollected - before);
        }

        public void ValidateFormalTaxMonthly(
            WorldState world,
            string villageId,
            long expectedDay)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            _productionContent.ValidateWorldReferences(world);
            var village = FindVillage(world, villageId);
            var monthInYear =
                (int)(((expectedDay / 30) - 1) % 12) + 1;
            if (world.FoodInventoryAuthorityMode !=
                    FoodInventoryAuthorityMode.FormalProductBatches ||
                expectedDay != world.AbsoluteDay ||
                expectedDay <= 0 ||
                expectedDay % 30 != 0 ||
                monthInYear != 10 ||
                village.LastSettlementDay != expectedDay ||
                village.HouseholdIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Formal household grain tax settlement is not due.");
            }
        }

        private void ResolveMonthlyCore(
            WorldState world,
            bool resolveFood,
            bool resolveTax)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.AbsoluteDay == 0 || world.AbsoluteDay % 30 != 0)
            {
                return;
            }

            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < villages.Count; i++)
            {
                ResolveVillage(world, villages[i], resolveFood, resolveTax);
            }
        }

        public static void RefreshAllCaches(
            WorldState world,
            IPersonRepository people = null)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                RefreshCaches(world, world.Villages[i], people);
            }
        }

        public static void RefreshCaches(
            WorldState world,
            VillageState village,
            IPersonRepository people = null)
        {
            people = people ?? new WorldStatePersonRepository(world);
            var living = 0;
            var working = 0;
            long foodSecurityTotal = 0;
            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                var family = FindFamily(world, village.HouseholdIds[i]);
                foodSecurityTotal += family.FoodSecurityBasisPoints;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (!person.IsAlive || person.LocationId != village.LocationId)
                    {
                        continue;
                    }

                    living++;
                    if (person.LaborCapacityBasisPoints > 0 &&
                        person.LocalDuty == LocalDutyKind.None)
                    {
                        working++;
                    }
                }
            }

            village.LivingResidentCount = living;
            village.WorkingResidentCount = working;
            village.HouseholdCount = village.HouseholdIds.Count;
            village.FoodSecurityBasisPoints = village.HouseholdIds.Count == 0
                ? 10_000
                : (int)(foodSecurityTotal / village.HouseholdIds.Count);
            var granary = FindFacility(
                world, village.Id, VillageFacilityKind.Granary);
            if (granary != null)
            {
                granary.InventoryUnits = village.PublicGranaryGrain;
            }

            for (var familyIndex = 0;
                 familyIndex < village.HouseholdIds.Count;
                 familyIndex++)
            {
                var family = FindFamily(
                    world, village.HouseholdIds[familyIndex]);
                var householdGranary = FindHouseholdGranary(
                    world, village.Id, family.Id);
                if (householdGranary != null)
                {
                    householdGranary.InventoryUnits = ProductInventorySystem
                        .CalculatePhysicalInventoryUnits(
                            world, householdGranary.Id, family.Id);
                }
            }
        }

        public VillageLifeAudit Audit(WorldState world, string villageId)
        {
            var people = PeopleFor(world);
            var village = FindVillage(world, villageId);
            var audit = new VillageLifeAudit
            {
                Households = village.HouseholdIds.Count,
                PublicGranaryGrain = world.FoodInventoryAuthorityMode ==
                    FoodInventoryAuthorityMode.FormalProductBatches
                    ? _foodInventory.SummarizeContainer(
                        world,
                        village.PublicGranaryInventoryContainerId)
                        .PhysicalQuantity
                    : village.PublicGranaryGrain
            };
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                var person = knownPeople[i];
                if (person.BirthLocationId == village.LocationId)
                {
                    audit.PermanentPeople++;
                }

                if (person.IsAlive && person.LocationId == village.LocationId)
                {
                    audit.LivingResidents++;
                }
            }

            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                var family = FindFamily(world, village.HouseholdIds[i]);
                audit.FamilyGrain += FamilyFoodQuantity(
                    world, village, family);
                audit.HouseholdMembers += family.MemberIds.Count;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (person.FamilyId != family.Id)
                    {
                        audit.InvalidFamilyReferences++;
                    }
                }
            }

            for (var i = 0; i < world.PopulationCohorts.Count; i++)
            {
                if (world.PopulationCohorts[i].LocationId == village.LocationId)
                {
                    audit.AbstractPopulation += world.PopulationCohorts[i].Population;
                }
            }

            return audit;
        }

        public VillageAttentionReport BuildAttentionReport(
            WorldState world,
            string villageId,
            VillageAttentionLevel attentionLevel)
        {
            if (!Enum.IsDefined(typeof(VillageAttentionLevel), attentionLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(attentionLevel));
            }

            var village = FindVillage(world, villageId);
            var audit = Audit(world, villageId);
            var report = new VillageAttentionReport
            {
                VillageId = village.Id,
                AttentionLevel = attentionLevel,
                PermanentPeople = audit.PermanentPeople,
                LivingResidents = audit.LivingResidents,
                Households = audit.Households,
                WorkingResidents = village.WorkingResidentCount,
                FamilyGrain = audit.FamilyGrain,
                PublicGranaryGrain = audit.PublicGranaryGrain,
                FoodSecurityBasisPoints = village.FoodSecurityBasisPoints
            };

            if (attentionLevel >= VillageAttentionLevel.Normal)
            {
                var families = FamiliesForVillage(world, village);
                for (var i = 0; i < families.Count; i++)
                {
                    var family = families[i];
                    if (family.FoodSecurityBasisPoints < 8_000 ||
                        family.TaxArrearsGrain > 0 ||
                        attentionLevel == VillageAttentionLevel.Deep)
                    {
                        report.HouseholdDetails.Add(
                            $"{family.Id}|members={family.MemberIds.Count}|" +
                            $"grain={FamilyFoodQuantity(world, village, family)}|" +
                            $"food={family.FoodSecurityBasisPoints}|" +
                            $"tax_arrears={family.TaxArrearsGrain}");
                    }
                }

                var start = Math.Max(0, world.VillageLedgerEntries.Count -
                    (attentionLevel == VillageAttentionLevel.Deep ? 50 : 10));
                for (var i = start; i < world.VillageLedgerEntries.Count; i++)
                {
                    var entry = world.VillageLedgerEntries[i];
                    if (entry.VillageId == village.Id)
                    {
                        report.RecentEvents.Add(entry.Summary);
                    }
                }
            }

            return report;
        }

        private void ResolveVillage(
            WorldState world,
            VillageState village,
            bool resolveFood,
            bool resolveTax)
        {
            var monthInYear = (int)(((world.AbsoluteDay / 30) - 1) % 12) + 1;
            ReleaseCompletedDuties(world, village);
            UpdateLaborProfiles(world, village);
            var families = FamiliesForVillage(world, village);
            if (monthInYear == 2)
            {
                ResolveCorvee(world, village, families);
            }

            if (monthInYear == 3)
            {
                _agricultureProduction.CreateDelegatedSeasonOrders(
                    world, village, world.AbsoluteDay + 180);
            }

            if (monthInYear == 4)
            {
                ResolveLevy(world, village, families);
            }

            if (resolveFood)
            {
                ResolveFood(world, village, families);
            }
            ResolveTools(world, village, families);
            ResolveMedicalCare(world, village, families);

            if (monthInYear == 9)
            {
                _agricultureProduction.ResolveDueOrders(world, village.Id);
            }

            if (resolveTax && monthInYear == 10)
            {
                ResolveTax(world, village, families);
            }

            if (monthInYear == 12)
            {
                ResolveMigration(world, village, families);
                ResolveMarriages(world, village);
            }

            var people = PeopleFor(world);
            for (var i = 0; i < families.Count; i++)
            {
                for (var memberIndex = 0;
                     memberIndex < families[i].MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        families[i].MemberIds[memberIndex]);
                    if (person.IsAlive)
                    {
                        person = people.GetRequiredForUpdate(person.Id);
                        person.NextIndependentEventDay = world.AbsoluteDay + 30;
                        person.NextIndependentEventReason =
                            "monthly_household_settlement";
                    }
                }
            }

            village.LastSettlementDay = world.AbsoluteDay;
            village.NextSettlementDay = world.AbsoluteDay + 30;
            RefreshCaches(world, village, _people);
        }

        private void ReleaseCompletedDuties(
            WorldState world,
            VillageState village)
        {
            var people = PeopleFor(world);
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                var person = knownPeople[i];
                if (person.LocationId == village.LocationId &&
                    person.LocalDuty != LocalDutyKind.None &&
                    person.LocalDutyUntilDay <= world.AbsoluteDay)
                {
                    person = people.GetRequiredForUpdate(person.Id);
                    person.LocalDuty = LocalDutyKind.None;
                    person.LocalDutyUntilDay = -1;
                }
            }
        }

        private void UpdateLaborProfiles(
            WorldState world,
            VillageState village)
        {
            var people = PeopleFor(world);
            var knownPeople = people.GetKnownPeople();
            for (var i = 0; i < knownPeople.Count; i++)
            {
                var person = knownPeople[i];
                if (!person.IsAlive || person.LocationId != village.LocationId)
                {
                    continue;
                }

                var age = Math.Max(
                    0, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
                person = people.GetRequiredForUpdate(person.Id);
                if (age < 15 || age > 65)
                {
                    person.LaborCapacityBasisPoints = 0;
                }
                else
                {
                    var ageFactor = age <= 45
                        ? 10_000
                        : Math.Max(4_000, 10_000 - (int)(age - 45) * 200);
                    person.LaborCapacityBasisPoints =
                        ageFactor * person.HealthBasisPoints / 10_000;
                    if (person.VillageOccupation == VillageOccupation.Dependent)
                    {
                        person.VillageOccupation = VillageOccupation.Farmer;
                    }
                }
            }
        }

        private void ResolveFood(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            if (world.FoodInventoryAuthorityMode ==
                FoodInventoryAuthorityMode.FormalProductBatches)
            {
                ResolveFormalFood(world, village, families);
                return;
            }

            var people = PeopleFor(world);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var required = 0;
                var residents = new List<PersonState>();
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (!person.IsAlive || person.LocationId != village.LocationId ||
                        person.LocalDuty == LocalDutyKind.Levy)
                    {
                        continue;
                    }

                    residents.Add(person);
                    var age = Math.Max(
                        0, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
                    required += age < 15 || age > 60 ? 2 : 3;
                }

                var shortfall = Math.Max(0L, required - family.Grain);
                var relief = Math.Min(shortfall, village.PublicGranaryGrain);
                if (relief > 0)
                {
                    family.Grain += relief;
                    village.PublicGranaryGrain -= relief;
                    AddLedger(
                        world, village, VillageLedgerEntryType.GrainRelief,
                        family.Id, string.Empty, relief, -relief, (int)relief,
                        $"{family.DisplayName}从公共粮仓获得{relief}粮食救济。");
                }

                var consumed = Math.Min((long)required, family.Grain);
                family.Grain -= consumed;
                family.LastConsumptionGrain = consumed;
                family.FoodSecurityBasisPoints = required == 0
                    ? 10_000
                    : (int)(consumed * 10_000 / required);
                AddLedger(
                    world, village, VillageLedgerEntryType.FoodConsumption,
                    family.Id, string.Empty, -consumed, 0, (int)consumed,
                    $"{family.DisplayName}本月消费粮食{consumed}/{required}。");

                if (consumed >= required)
                {
                    continue;
                }

                var damage = Math.Max(
                    100, (required - (int)consumed) * 1_000 / Math.Max(1, required));
                for (var residentIndex = 0;
                     residentIndex < residents.Count;
                     residentIndex++)
                {
                    var resident = people.GetRequiredForUpdate(
                        residents[residentIndex].Id);
                    resident.HealthBasisPoints = Math.Max(
                        0, resident.HealthBasisPoints - damage);
                    resident.Needs.Livelihood = Math.Min(
                        10_000, resident.Needs.Livelihood + 1_000);
                }
            }
        }

        private FormalHouseholdFoodSettlementResult ResolveFormalFood(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var result = new FormalHouseholdFoodSettlementResult
            {
                VillageId = village.Id
            };
            var people = PeopleFor(world);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                result.HouseholdsProcessed++;
                var required = 0;
                var residents = new List<PersonState>();
                var residentNutritionNeeds = new List<long>();
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(
                        family.MemberIds[memberIndex]);
                    if (!person.IsAlive ||
                        person.LocationId != village.LocationId ||
                        person.LocalDuty == LocalDutyKind.Levy)
                    {
                        continue;
                    }

                    residents.Add(person);
                    var age = Math.Max(
                        0, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
                    var requiredUnits = age < 15 || age > 60 ? 2 : 3;
                    required += requiredUnits;
                    residentNutritionNeeds.Add(
                        checked((long)requiredUnits * 10_000L));
                }

                if (required == 0)
                {
                    family.LastConsumptionGrain = 0;
                    family.FoodSecurityBasisPoints = 10_000;
                    continue;
                }

                var storage = FindHouseholdGranary(
                    world, village.Id, family.Id) ??
                    throw new InvalidOperationException(
                        $"Family {family.Id} has no household granary.");
                var requiredNutrition = checked((long)required * 10_000L);
                result.RequiredNutritionBasisUnits = checked(
                    result.RequiredNutritionBasisUnits + requiredNutrition);
                var opening = _foodInventory.SummarizeFamilyGranary(
                    world, family.Id, storage.Id);
                var shortfall = Math.Max(
                    0L, requiredNutrition - opening.NutritionBasisUnits);
                if (shortfall > 0)
                {
                    var relief = _foodInventory
                        .TransferContainerToFamilyByNutrition(
                            world,
                            village.PublicGranaryInventoryContainerId,
                            family.Id,
                            storage.Id,
                            residents[0].Id,
                            shortfall,
                            InventoryTransactionType
                                .FoodVillageReliefTransferred,
                            village.Id);
                    if (relief.TransferredPhysicalQuantity > 0)
                    {
                        AddLedger(
                            world,
                            village,
                            VillageLedgerEntryType.GrainRelief,
                            family.Id,
                            residents[0].Id,
                            0,
                            0,
                            (int)Math.Min(
                                int.MaxValue,
                                relief.TransferredPhysicalQuantity),
                            $"{family.DisplayName}从公共粮仓获得" +
                            $"{relief.TransferredPhysicalQuantity}单位食品救济。");
                    }
                }

                var consumed = _foodInventory.ConsumeFamilyFood(
                    world,
                    family.Id,
                    storage.Id,
                    residents[0].Id,
                    requiredNutrition);
                result.ProvidedNutritionBasisUnits = checked(
                    result.ProvidedNutritionBasisUnits +
                    consumed.ProvidedNutritionBasisUnits);
                result.ConsumedPhysicalQuantity = checked(
                    result.ConsumedPhysicalQuantity +
                    consumed.ConsumedPhysicalQuantity);
                family.LastConsumptionGrain =
                    consumed.ConsumedPhysicalQuantity;
                family.FoodSecurityBasisPoints = (int)Math.Min(
                    10_000L,
                    consumed.ProvidedNutritionBasisUnits * 10_000L /
                    requiredNutrition);
                AddLedger(
                    world,
                    village,
                    VillageLedgerEntryType.FoodConsumption,
                    family.Id,
                    residents[0].Id,
                    0,
                    0,
                    (int)Math.Min(
                        int.MaxValue,
                        consumed.ConsumedPhysicalQuantity),
                    $"{family.DisplayName}本月获得营养" +
                    $"{consumed.ProvidedNutritionBasisUnits}/" +
                    $"{requiredNutrition}。");

                if (consumed.Fulfilled)
                {
                    for (var residentIndex = 0;
                         residentIndex < residents.Count;
                         residentIndex++)
                    {
                        result.PersonSettlements.Add(
                            new FormalHouseholdFoodPersonSettlementResult
                            {
                                PersonId = residents[residentIndex].Id,
                                RequiredNutritionBasisUnits =
                                    residentNutritionNeeds[residentIndex]
                            });
                    }
                    continue;
                }

                var missingNutrition = Math.Max(
                    0L,
                    requiredNutrition -
                    consumed.ProvidedNutritionBasisUnits);
                result.ShortfallFamilyIds.Add(family.Id);
                var shortfallResult =
                    new FormalHouseholdFoodShortfallResult
                {
                    FamilyId = family.Id,
                    RequiredNutritionBasisUnits = requiredNutrition,
                    ProvidedNutritionBasisUnits =
                        consumed.ProvidedNutritionBasisUnits,
                    MissingNutritionBasisUnits = missingNutrition
                };
                result.Shortfalls.Add(shortfallResult);
                var damage = (int)Math.Max(
                    100L,
                    missingNutrition * 1_000L /
                    Math.Max(1L, requiredNutrition));
                for (var residentIndex = 0;
                     residentIndex < residents.Count;
                     residentIndex++)
                {
                    var resident = people.GetRequiredForUpdate(
                        residents[residentIndex].Id);
                    var openingHealth = resident.HealthBasisPoints;
                    var openingLivelihood = resident.Needs.Livelihood;
                    resident.HealthBasisPoints = Math.Max(
                        0, resident.HealthBasisPoints - damage);
                    resident.Needs.Livelihood = Math.Min(
                        10_000, resident.Needs.Livelihood + 1_000);
                    shortfallResult.AffectedPeople.Add(
                        new FormalHouseholdFoodAffectedPersonResult
                        {
                            PersonId = resident.Id,
                            RequiresCaregiverDelivery =
                                RequiresCaregiverDelivery(world, resident),
                            RequiredNutritionBasisUnits =
                                residentNutritionNeeds[residentIndex],
                            AppliedHealthDamageBasisPoints =
                                openingHealth - resident.HealthBasisPoints,
                            AppliedLivelihoodPressureBasisPoints =
                                resident.Needs.Livelihood - openingLivelihood
                        });
                }
                AllocateFormalShortfallNutrition(
                    shortfallResult,
                    missingNutrition,
                    requiredNutrition);
                for (var affectedIndex = 0;
                     affectedIndex < shortfallResult.AffectedPeople.Count;
                     affectedIndex++)
                {
                    var affected = shortfallResult.AffectedPeople[affectedIndex];
                    result.PersonSettlements.Add(
                        new FormalHouseholdFoodPersonSettlementResult
                        {
                            PersonId = affected.PersonId,
                            RequiredNutritionBasisUnits =
                                affected.RequiredNutritionBasisUnits,
                            MissingNutritionBasisUnits =
                                affected.AllocatedNutritionBasisUnits
                        });
                }
            }

            result.ShortfallFamilyIds.Sort(StringComparer.Ordinal);
            new LongTermNutritionSystem(people).RecordMonthlySettlement(
                world, world.AbsoluteDay, result.PersonSettlements);
            return result;
        }

        private static bool RequiresCaregiverDelivery(
            WorldState world,
            PersonState person)
        {
            var ageYears = Math.Max(
                0L, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
            return ageYears < 15L || ageYears > 60L ||
                person.HealthBasisPoints < 5_000;
        }

        private static void AllocateFormalShortfallNutrition(
            FormalHouseholdFoodShortfallResult shortfall,
            long missingNutritionBasisUnits,
            long requiredNutritionBasisUnits)
        {
            var ordered = new List<FormalHouseholdFoodAffectedPersonResult>(
                shortfall.AffectedPeople);
            ordered.Sort((left, right) =>
                string.CompareOrdinal(left.PersonId, right.PersonId));
            long allocated = 0;
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].AllocatedNutritionBasisUnits = (long)decimal.Floor(
                    (decimal)missingNutritionBasisUnits *
                    ordered[i].RequiredNutritionBasisUnits /
                    requiredNutritionBasisUnits);
                allocated = checked(
                    allocated + ordered[i].AllocatedNutritionBasisUnits);
            }
            var remainder = checked(
                missingNutritionBasisUnits - allocated);
            for (var i = 0; remainder > 0; i++)
            {
                ordered[i % ordered.Count].AllocatedNutritionBasisUnits++;
                remainder--;
            }
        }

        private static void ResolvePlanting(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var seedNeeded = Math.Max(1, family.FarmlandUnits * 2);
                var plantedSeed = Math.Min(family.SeedGrain, seedNeeded);
                family.SeedGrain -= plantedSeed;
                family.PlantedSeedGrain = plantedSeed;
                family.CultivatedLandUnits =
                    family.FarmlandUnits * (int)plantedSeed / seedNeeded;
                AddLedger(
                    world, village, VillageLedgerEntryType.Planting,
                    family.Id, string.Empty, 0, 0, (int)plantedSeed,
                    $"{family.DisplayName}播种{plantedSeed}单位种粮，" +
                    $"耕种{family.CultivatedLandUnits}单位土地。");
            }
        }

        private void ResolveHarvest(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var irrigation = FindFacility(
                world, village.Id, VillageFacilityKind.Irrigation);
            var irrigationFactor = irrigation == null
                ? 7_000
                : irrigation.ConditionBasisPoints;
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var labor = HouseholdLabor(world, village, family);
                var factor = Math.Min(
                    irrigationFactor,
                    Math.Min(family.ToolConditionBasisPoints,
                        Math.Max(3_000, labor)));
                var harvest = family.CultivatedLandUnits * 15L * factor / 10_000;
                family.Grain += harvest;
                family.LastHarvestGrain = harvest;
                family.SeedGrain += Math.Min(
                    harvest / 8, family.FarmlandUnits * 2L);
                family.CultivatedLandUnits = 0;
                family.PlantedSeedGrain = 0;
                AddLedger(
                    world, village, VillageLedgerEntryType.Harvest,
                    family.Id, string.Empty, harvest, 0, (int)harvest,
                    $"{family.DisplayName}收获粮食{harvest}。");
            }
        }

        private void ResolveTax(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var headmanAvailable = FacilityOperational(
                world, village, VillageFacilityKind.AssemblyHall);
            var rateBasisPoints = headmanAvailable ? 1_000 : 700;
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var due = family.LastHarvestGrain * rateBasisPoints / 10_000 +
                          family.TaxArrearsGrain;
                AddLedger(
                    world, village, VillageLedgerEntryType.TaxAssessment,
                    family.Id, string.Empty, 0, 0, (int)Math.Min(int.MaxValue, due),
                    $"{family.DisplayName}本年应纳税粮{due}。");
                long paid;
                if (world.FoodInventoryAuthorityMode ==
                    FoodInventoryAuthorityMode.FormalProductBatches)
                {
                    var storage = FindHouseholdGranary(
                        world, village.Id, family.Id) ??
                        throw new InvalidOperationException(
                            $"Family {family.Id} has no household granary.");
                    var available = _foodInventory.SummarizeFamilyGranary(
                        world, family.Id, storage.Id).PhysicalQuantity;
                    var requested = Math.Min(due, available);
                    paid = requested <= 0
                        ? 0
                        : _foodInventory.TransferFamilyToContainer(
                            world,
                            family.Id,
                            storage.Id,
                            village.PublicGranaryInventoryContainerId,
                            family.HeadPersonId,
                            requested,
                            InventoryTransactionType.FoodTaxTransferred,
                            village.Id).TransferredPhysicalQuantity;
                }
                else
                {
                    paid = Math.Min(due, family.Grain);
                    family.Grain -= paid;
                    village.PublicGranaryGrain += paid;
                }
                family.TaxArrearsGrain = due - paid;
                village.TaxGrainCollected += paid;
                AddLedger(
                    world, village, VillageLedgerEntryType.TaxPayment,
                    family.Id, string.Empty,
                    world.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches
                        ? 0
                        : -paid,
                    world.FoodInventoryAuthorityMode ==
                        FoodInventoryAuthorityMode.FormalProductBatches
                        ? 0
                        : paid,
                    (int)Math.Min(int.MaxValue, paid),
                    $"{family.DisplayName}缴纳税粮{paid}，欠税{family.TaxArrearsGrain}。");
            }
        }

        private long FamilyFoodQuantity(
            WorldState world,
            VillageState village,
            FamilyState family)
        {
            if (world.FoodInventoryAuthorityMode ==
                FoodInventoryAuthorityMode.LegacyScalar)
            {
                return family.Grain;
            }

            var storage = FindHouseholdGranary(
                world, village.Id, family.Id);
            return storage == null
                ? 0
                : _foodInventory.SummarizeFamilyGranary(
                    world, family.Id, storage.Id).PhysicalQuantity;
        }

        private void ResolveTools(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var smithy = FindFacility(world, village.Id, VillageFacilityKind.Smithy);
            var operational = FacilityOperational(
                world, village, VillageFacilityKind.Smithy);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var wear = Math.Max(10, HouseholdLabor(world, village, family) / 200);
                family.ToolConditionBasisPoints = Math.Max(
                    0, family.ToolConditionBasisPoints - wear);
                AddLedger(
                    world, village, VillageLedgerEntryType.ToolWear,
                    family.Id, string.Empty, 0, 0, wear,
                    $"{family.DisplayName}农具损耗{wear}。");

                if (!operational || smithy.InventoryUnits <= 0 ||
                    family.ToolConditionBasisPoints >= 9_000)
                {
                    continue;
                }

                var repair = Math.Min(80, 9_000 - family.ToolConditionBasisPoints);
                family.ToolConditionBasisPoints += repair;
                smithy.InventoryUnits--;
                AddLedger(
                    world, village, VillageLedgerEntryType.ToolRepair,
                    family.Id, smithy.ManagerPersonId, 0, 0, repair,
                    $"铁匠为{family.DisplayName}修复农具{repair}。");
            }
        }

        private void ResolveMedicalCare(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var clinic = FindFacility(world, village.Id, VillageFacilityKind.Clinic);
            if (!FacilityOperational(world, village, VillageFacilityKind.Clinic) ||
                clinic.Capacity <= 0)
            {
                return;
            }

            var people = PeopleFor(world);
            var residentIds = new HashSet<string>(StringComparer.Ordinal);
            var physicians = new List<PersonState>();
            for (var familyIndex = 0; familyIndex < families.Count; familyIndex++)
            {
                var family = families[familyIndex];
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = people.GetRequired(family.MemberIds[memberIndex]);
                    residentIds.Add(person.Id);
                    if (person.IsAlive &&
                        person.LocationId == village.LocationId &&
                        person.VillageOccupation == VillageOccupation.Physician &&
                        person.LocalDuty == LocalDutyKind.None &&
                        Math.Max(
                            person.MedicalSkillBasisPoints,
                            person.ProfessionalSkills?.Medicine ?? 0) >=
                            CivilianMedicalRules
                                .MinimumPhysicianSkillBasisPoints)
                    {
                        physicians.Add(person);
                    }
                }
            }
            _civilianMedical.ReconcileCasesForResidents(world, residentIds);
            if (physicians.Count == 0)
            {
                return;
            }
            physicians.Sort((left, right) =>
            {
                var leftSkill = Math.Max(
                    left.MedicalSkillBasisPoints,
                    left.ProfessionalSkills?.Medicine ?? 0);
                var rightSkill = Math.Max(
                    right.MedicalSkillBasisPoints,
                    right.ProfessionalSkills?.Medicine ?? 0);
                var skill = rightSkill.CompareTo(leftSkill);
                return skill != 0
                    ? skill
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            var physician = physicians[0];

            var episodes = new List<NutritionConditionEpisodeState>();
            for (var i = 0; i < world.NutritionConditionEpisodes.Count; i++)
            {
                var episode = world.NutritionConditionEpisodes[i];
                var existingCase = _civilianMedical.FindCaseForEpisode(
                    world, episode.Id);
                if (episode.EndDay == -1 &&
                    residentIds.Contains(episode.PersonId) &&
                    (existingCase == null ||
                     existingCase.Status == CivilianMedicalCaseStatus.Active))
                {
                    episodes.Add(episode);
                }
            }
            episodes.Sort((left, right) =>
            {
                var risk = right.PeakDiseaseRiskBasisPoints.CompareTo(
                    left.PeakDiseaseRiskBasisPoints);
                if (risk != 0)
                {
                    return risk;
                }
                var day = left.StartDay.CompareTo(right.StartDay);
                return day != 0
                    ? day
                    : string.CompareOrdinal(left.Id, right.Id);
            });

            var appointments = Math.Min(clinic.Capacity, episodes.Count);
            for (var i = 0; i < appointments; i++)
            {
                var patient = people.GetRequired(episodes[i].PersonId);
                var authorizer = SelectMedicalAuthorizer(
                    world, patient, people);
                if (authorizer == null)
                {
                    continue;
                }
                var diagnosis = _civilianMedical.DiagnoseNutritionCondition(
                    world, episodes[i].Id, physician.Id, authorizer.Id);
                if (!diagnosis.Success)
                {
                    continue;
                }
                var treatment = _civilianMedical.TreatNutritionCondition(
                    world,
                    diagnosis.MedicalCaseId,
                    physician.Id,
                    authorizer.Id,
                    clinic.Id);
                if (treatment.Success)
                {
                    AddLedger(
                        world, village, VillageLedgerEntryType.MedicalCare,
                        patient.FamilyId, patient.Id, 0, 0,
                        treatment.RecoveredHealthBasisPoints,
                        $"{patient.DisplayName}接受正式营养性疾病诊疗，恢复" +
                        $"{treatment.RecoveredHealthBasisPoints}健康。");
                }
            }
        }

        private static PersonState SelectMedicalAuthorizer(
            WorldState world,
            PersonState patient,
            IPersonRepository people)
        {
            if ((world.AbsoluteDay - patient.BirthDay) / DaysPerYear >=
                CivilianMedicalRules.AdultAgeYears)
            {
                return patient;
            }
            FamilyState family = null;
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == patient.FamilyId)
                {
                    family = world.Families[i];
                    break;
                }
            }
            if (family == null)
            {
                return null;
            }
            PersonState selected = null;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var candidate = people.GetRequired(family.MemberIds[i]);
                if (!candidate.IsAlive ||
                    candidate.LocationId != patient.LocationId ||
                    (world.AbsoluteDay - candidate.BirthDay) / DaysPerYear <
                        CivilianMedicalRules.AdultAgeYears)
                {
                    continue;
                }
                if (selected == null ||
                    string.CompareOrdinal(candidate.Id, selected.Id) < 0)
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        private void ResolveCorvee(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var headmanAvailable = FacilityOperational(
                world, village, VillageFacilityKind.AssemblyHall);
            for (var i = 0; i < families.Count; i++)
            {
                if (!headmanAvailable && i % 2 == 1)
                {
                    continue;
                }

                var worker = SelectWorker(world, village, families[i], true);
                if (worker == null)
                {
                    continue;
                }

                worker = PeopleFor(world).GetRequiredForUpdate(worker.Id);
                worker.LocalDuty = LocalDutyKind.Corvee;
                worker.LocalDutyUntilDay = world.AbsoluteDay + 10;
                families[i].CorveeDaysThisYear += 10;
                village.CorveeDaysCompleted += 10;
                AddLedger(
                    world, village, VillageLedgerEntryType.Corvee,
                    families[i].Id, worker.Id, 0, 0, 10,
                    $"{worker.DisplayName}承担10日村中劳役。");
            }
        }

        private void ResolveLevy(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            var candidates = new List<PersonState>();
            for (var i = 0; i < families.Count; i++)
            {
                var worker = SelectWorker(world, village, families[i], false);
                if (worker != null &&
                    worker.VillageOccupation == VillageOccupation.Farmer)
                {
                    candidates.Add(worker);
                }
            }

            candidates.Sort((left, right) =>
            {
                var leftRoll = _random.NextUInt64(
                    "village_levy", new StableId(left.Id), world.AbsoluteDay,
                    "selection");
                var rightRoll = _random.NextUInt64(
                    "village_levy", new StableId(right.Id), world.AbsoluteDay,
                    "selection");
                var result = leftRoll.CompareTo(rightRoll);
                return result != 0
                    ? result
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            var quota = Math.Min(candidates.Count,
                Math.Max(1, village.LivingResidentCount / 100));
            for (var i = 0; i < quota; i++)
            {
                var person = PeopleFor(world).GetRequiredForUpdate(
                    candidates[i].Id);
                person.LocalDuty = LocalDutyKind.Levy;
                person.LocalDutyUntilDay = world.AbsoluteDay + 90;
                village.LevyPersonDays += 90;
                AddLedger(
                    world, village, VillageLedgerEntryType.Levy,
                    person.FamilyId, person.Id, 0, 0, 90,
                    $"{person.DisplayName}被临时征发90日，家庭失去一名劳动力。");
            }
        }

        private void ResolveMigration(
            WorldState world,
            VillageState village,
            List<FamilyState> families)
        {
            if (string.IsNullOrEmpty(village.ParentLocationId))
            {
                return;
            }

            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                if (family.FoodSecurityBasisPoints >= 7_000 &&
                    family.TaxArrearsGrain == 0 &&
                    family.Debt < 500)
                {
                    continue;
                }

                PersonState migrant = null;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var candidate = PeopleFor(world).GetRequired(
                        family.MemberIds[memberIndex]);
                    if (candidate.Id != family.HeadPersonId &&
                        candidate.IsAlive &&
                        candidate.LocationId == village.LocationId &&
                        candidate.LocalDuty == LocalDutyKind.None &&
                        candidate.LaborCapacityBasisPoints > 0 &&
                        (migrant == null ||
                         candidate.LaborCapacityBasisPoints >
                         migrant.LaborCapacityBasisPoints))
                    {
                        migrant = candidate;
                    }
                }

                if (migrant == null)
                {
                    continue;
                }

                _population.MoveIndependentPerson(
                    world, migrant, village.ParentLocationId);
                AddLedger(
                    world, village, VillageLedgerEntryType.Migration,
                    family.Id, migrant.Id, 0, 0, 1,
                    $"{migrant.DisplayName}因家庭生计困难离开{village.DisplayName}。");
                AddLifeEvent(
                    world, LifeEventType.Migration, migrant.Id, string.Empty,
                    family.Id, $"{migrant.DisplayName}迁往县城谋生。");
            }
        }

        private void ResolveMarriages(WorldState world, VillageState village)
        {
            var men = EligibleMarriageCandidates(world, village, PersonGender.Male);
            var women = EligibleMarriageCandidates(world, village, PersonGender.Female);
            var pairCount = Math.Min(men.Count, women.Count);
            for (var i = 0; i < pairCount; i++)
            {
                var man = men[i];
                PersonState woman = null;
                var womanIndex = -1;
                for (var candidateIndex = 0;
                     candidateIndex < women.Count;
                     candidateIndex++)
                {
                    if (women[candidateIndex].FamilyId != man.FamilyId)
                    {
                        woman = women[candidateIndex];
                        womanIndex = candidateIndex;
                        break;
                    }
                }

                if (woman == null)
                {
                    break;
                }

                women.RemoveAt(womanIndex);
                var originFamily = FindFamily(world, woman.FamilyId);
                var destinationFamily = FindFamily(world, man.FamilyId);
                if (woman.Id == originFamily.HeadPersonId)
                {
                    continue;
                }

                originFamily.MemberIds.Remove(woman.Id);
                destinationFamily.MemberIds.Add(woman.Id);
                var people = PeopleFor(world);
                man = people.GetRequiredForUpdate(man.Id);
                woman = people.GetRequiredForUpdate(woman.Id);
                woman.FamilyId = destinationFamily.Id;
                man.SpousePersonId = woman.Id;
                woman.SpousePersonId = man.Id;
                AddLedger(
                    world, village, VillageLedgerEntryType.Marriage,
                    destinationFamily.Id, woman.Id, 0, 0, 1,
                    $"{man.DisplayName}与{woman.DisplayName}成婚，" +
                    $"{woman.DisplayName}迁入{destinationFamily.DisplayName}。");
                AddLifeEvent(
                    world, LifeEventType.Marriage, man.Id, woman.Id,
                    destinationFamily.Id,
                    $"{man.DisplayName}与{woman.DisplayName}成婚。");
            }
        }

        private List<PersonState> EligibleMarriageCandidates(
            WorldState world,
            VillageState village,
            PersonGender gender)
        {
            var result = new List<PersonState>();
            var people = PeopleFor(world).GetKnownPeople();
            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];
                var age = (world.AbsoluteDay - person.BirthDay) / DaysPerYear;
                if (person.IsAlive && person.LocationId == village.LocationId &&
                    person.Gender == gender &&
                    string.IsNullOrEmpty(person.SpousePersonId) &&
                    !string.IsNullOrEmpty(person.FamilyId) &&
                    age >= 18 && age <= 30)
                {
                    result.Add(person);
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private PersonState SelectWorker(
            WorldState world,
            VillageState village,
            FamilyState family,
            bool includeSpecialists)
        {
            PersonState selected = null;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = PeopleFor(world).GetRequired(
                    family.MemberIds[i]);
                if (!person.IsAlive || person.LocationId != village.LocationId ||
                    person.LocalDuty != LocalDutyKind.None ||
                    person.LaborCapacityBasisPoints <= 0 ||
                    !includeSpecialists &&
                    person.VillageOccupation != VillageOccupation.Farmer)
                {
                    continue;
                }

                if (selected == null ||
                    person.LaborCapacityBasisPoints >
                    selected.LaborCapacityBasisPoints ||
                    person.LaborCapacityBasisPoints ==
                    selected.LaborCapacityBasisPoints &&
                    string.CompareOrdinal(person.Id, selected.Id) < 0)
                {
                    selected = person;
                }
            }

            return selected;
        }

        private int HouseholdLabor(
            WorldState world,
            VillageState village,
            FamilyState family)
        {
            var total = 0;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = PeopleFor(world).GetRequired(
                    family.MemberIds[i]);
                if (person.IsAlive && person.LocationId == village.LocationId &&
                    person.LocalDuty == LocalDutyKind.None)
                {
                    total += person.LaborCapacityBasisPoints;
                }
            }

            return Math.Min(10_000, total / Math.Max(1, family.FarmlandUnits / 4));
        }

        private bool FacilityOperational(
            WorldState world,
            VillageState village,
            VillageFacilityKind kind)
        {
            var facility = FindFacility(world, village.Id, kind);
            if (facility == null || facility.ConditionBasisPoints <= 0 ||
                string.IsNullOrEmpty(facility.ManagerPersonId))
            {
                return false;
            }

            var manager = PeopleFor(world).GetRequired(
                facility.ManagerPersonId);
            return manager.IsAlive && manager.HealthBasisPoints > 0 &&
                   manager.LocationId == village.LocationId;
        }

        private static void AddLedger(
            WorldState world,
            VillageState village,
            VillageLedgerEntryType type,
            string familyId,
            string personId,
            long familyGrainDelta,
            long publicGrainDelta,
            int quantity,
            string summary)
        {
            world.VillageLedgerEntries.Add(new VillageLedgerEntryState
            {
                Id = $"village_ledger.{world.AbsoluteDay}.{world.VillageLedgerEntries.Count:D6}",
                Day = world.AbsoluteDay,
                Type = type,
                VillageId = village.Id,
                FamilyId = familyId,
                PersonId = personId,
                FamilyGrainDelta = familyGrainDelta,
                PublicGrainDelta = publicGrainDelta,
                Quantity = quantity,
                Summary = summary
            });
        }

        private static void AddLifeEvent(
            WorldState world,
            LifeEventType type,
            string primaryPersonId,
            string secondaryPersonId,
            string familyId,
            string summary)
        {
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = $"life_event.{world.AbsoluteDay}.{type.ToString().ToLowerInvariant()}." +
                     $"{world.LifeEvents.Count:D6}",
                Type = type,
                Day = world.AbsoluteDay,
                PrimaryPersonId = primaryPersonId,
                SecondaryPersonId = secondaryPersonId,
                FamilyId = familyId,
                Summary = summary
            });
        }

        private static List<FamilyState> FamiliesForVillage(
            WorldState world,
            VillageState village)
        {
            var result = new List<FamilyState>();
            for (var i = 0; i < village.HouseholdIds.Count; i++)
            {
                result.Add(FindFamily(world, village.HouseholdIds[i]));
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static VillageState FindVillage(WorldState world, string villageId)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                if (world.Villages[i].Id == villageId)
                {
                    return world.Villages[i];
                }
            }

            throw new InvalidOperationException($"Missing village {villageId}.");
        }

        private static VillageFacilityState FindFacility(
            WorldState world,
            string villageId,
            VillageFacilityKind kind)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == villageId && facility.Kind == kind)
                {
                    return facility;
                }
            }

            return null;
        }

        private static VillageFacilityState FindHouseholdGranary(
            WorldState world,
            string villageId,
            string familyId)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                if (facility.VillageId == villageId &&
                    facility.Kind == VillageFacilityKind.HouseholdGranary &&
                    facility.OwnerFamilyId == familyId)
                {
                    return facility;
                }
            }

            return null;
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

        private IPersonRepository PeopleFor(WorldState world)
        {
            if (_people != null)
            {
                return _people;
            }

            if (!ReferenceEquals(_fallbackWorld, world))
            {
                _fallbackWorld = world;
                _fallbackPeople = new WorldStatePersonRepository(world);
            }

            return _fallbackPeople;
        }
    }

    public sealed class FormalHouseholdFoodMonthlyCommandScheduler
    {
        public const string CommandTypeId =
            "mandate.command.formal_food.resolve_household_monthly";
        public const string IssuerId = "system.formal_household_food";
        public const string ExpectedDayArgumentId = "expected_day";
        public const string VillageIdArgumentId = "village_id";
        public const string TransactionKindId =
            "mandate.transaction.formal_food.resolve_household_monthly";
        public const string ShortfallEventTypeId =
            "mandate.event.formal_food.household_shortfall_detected";
        public const string ProjectionHandlerId =
            "mandate.handler.formal_food.shortfall_projection";

        private readonly VillageLifeSystem _villageLife;

        public FormalHouseholdFoodMonthlyCommandScheduler(
            VillageLifeSystem villageLife)
        {
            _villageLife = villageLife ?? throw new ArgumentNullException(
                nameof(villageLife));
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

            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            var created = 0;
            for (var villageIndex = 0;
                 villageIndex < villages.Count;
                 villageIndex++)
            {
                var village = villages[villageIndex];
                if (village.HouseholdIds.Count == 0 ||
                    village.LastSettlementDay == world.AbsoluteDay)
                {
                    continue;
                }
                var commandId = MonthlyCommandId(
                    world.AbsoluteDay,
                    village.Id);
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
                        55,
                        new Dictionary<string, string>
                        {
                            {
                                ExpectedDayArgumentId,
                                Invariant(world.AbsoluteDay)
                            },
                            { VillageIdArgumentId, village.Id }
                        }));
                created++;
            }
            return created;
        }

        public IWorldCommandHandler CreateCommandHandler() =>
            new FormalHouseholdFoodMonthlyCommandHandler(_villageLife);

        public IWorldRuntimeEventHandler CreateProjectionHandler() =>
            new FormalHouseholdFoodShortfallProjectionHandler();

        public static string MonthlyCommandId(long day, string villageId) =>
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_food.monthly_command.{0:D10}.{1}",
                day,
                villageId);

        public static string MonthlyTransactionId(
            long day,
            string villageId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_food.monthly_transaction.{0:D10}.{1}",
                day,
                villageId);

        public static string MonthlyShortfallEventId(
            long day,
            string villageId) => string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "formal_food.household_shortfall.{0:D10}.{1}",
                day,
                villageId);

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

        private sealed class FormalHouseholdFoodMonthlyCommandHandler :
            IWorldCommandHandler
        {
            private readonly VillageLifeSystem _villageLife;

            public FormalHouseholdFoodMonthlyCommandHandler(
                VillageLifeSystem villageLife)
            {
                _villageLife = villageLife;
            }

            public string CommandTypeId =>
                FormalHouseholdFoodMonthlyCommandScheduler.CommandTypeId;

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
                        VillageIdArgumentId,
                        out var villageId) ||
                    string.IsNullOrEmpty(villageId))
                {
                    throw new InvalidOperationException(
                        "Formal household food monthly command arguments are invalid.");
                }
                _ = new StableId(villageId);
                transactions.Add(
                    new FormalHouseholdFoodMonthlyTransaction(
                        _villageLife,
                        expectedDay,
                        villageId));
            }
        }

        private sealed class FormalHouseholdFoodMonthlyTransaction :
            IWorldTransaction
        {
            private readonly VillageLifeSystem _villageLife;
            private readonly long _expectedDay;
            private readonly string _villageId;

            public FormalHouseholdFoodMonthlyTransaction(
                VillageLifeSystem villageLife,
                long expectedDay,
                string villageId)
            {
                _villageLife = villageLife;
                _expectedDay = expectedDay;
                _villageId = villageId;
                Id = MonthlyTransactionId(expectedDay, villageId);
            }

            public string Id { get; }

            public string KindId => TransactionKindId;

            public int Priority => 55;

            public void Validate(
                WorldState world,
                WorldTransactionValidationContext validation)
            {
                _villageLife.ValidateFormalFoodMonthly(
                    world,
                    _villageId,
                    _expectedDay);
                validation.Reserve(
                    "formal_food.household_monthly." +
                        Invariant(_expectedDay) + "." + _villageId,
                    1,
                    1,
                    Id);
            }

            public void Apply(WorldState world, WorldEventBuffer events)
            {
                var result = _villageLife.ResolveFormalFoodMonthly(
                    world,
                    _villageId,
                    _expectedDay);
                if (result.HasShortfall)
                {
                    var shortfallEventId = MonthlyShortfallEventId(
                        _expectedDay, _villageId);
                    HouseholdReliefPickupSystem.RecordMonthlyShortfalls(
                        world,
                        shortfallEventId,
                        _expectedDay,
                        _villageId,
                        result.Shortfalls);
                    events.Add(new WorldRuntimeEvent(
                        shortfallEventId,
                        ShortfallEventTypeId,
                        Id,
                        world.AbsoluteDay,
                        (DaySegment)world.Segment));
                }
            }
        }

        private sealed class FormalHouseholdFoodShortfallProjectionHandler :
            IWorldRuntimeEventHandler
        {
            public string HandlerId => ProjectionHandlerId;

            public string EventTypeId => ShortfallEventTypeId;

            public void Handle(
                WorldRuntimeEvent worldEvent,
                WorldCommandRuntime commandRuntime)
            {
                // The committed transaction owns inventory and household
                // consequences. This handler only acknowledges the event.
            }
        }
    }
}
