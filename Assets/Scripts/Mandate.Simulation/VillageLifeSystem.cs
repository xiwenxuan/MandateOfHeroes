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

    public sealed class VillageLifeSystem
    {
        private const int DaysPerYear = 360;
        private readonly NamedRandom _random;
        private readonly PopulationLedgerSystem _population =
            new PopulationLedgerSystem();
        private readonly AgricultureProductionSystem _agricultureProduction;

        public VillageLifeSystem(
            ulong masterSeed,
            ProductionContentRegistry productionContent = null)
        {
            _random = new NamedRandom(masterSeed);
            _agricultureProduction =
                new AgricultureProductionSystem(masterSeed, productionContent);
        }

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

            var villages = new List<VillageState>(world.Villages);
            villages.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < villages.Count; i++)
            {
                ResolveVillage(world, villages[i]);
            }
        }

        public static void RefreshAllCaches(WorldState world)
        {
            for (var i = 0; i < world.Villages.Count; i++)
            {
                RefreshCaches(world, world.Villages[i]);
            }
        }

        public static void RefreshCaches(WorldState world, VillageState village)
        {
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
                    var person = FindPerson(world, family.MemberIds[memberIndex]);
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
            var village = FindVillage(world, villageId);
            var audit = new VillageLifeAudit
            {
                Households = village.HouseholdIds.Count,
                PublicGranaryGrain = village.PublicGranaryGrain
            };
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
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
                audit.FamilyGrain += family.Grain;
                audit.HouseholdMembers += family.MemberIds.Count;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = FindPerson(world, family.MemberIds[memberIndex]);
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
                            $"grain={family.Grain}|food={family.FoodSecurityBasisPoints}|" +
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

        private void ResolveVillage(WorldState world, VillageState village)
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

            ResolveFood(world, village, families);
            ResolveTools(world, village, families);
            ResolveMedicalCare(world, village);

            if (monthInYear == 9)
            {
                _agricultureProduction.ResolveDueOrders(world, village.Id);
            }

            if (monthInYear == 10)
            {
                ResolveTax(world, village, families);
            }

            if (monthInYear == 12)
            {
                ResolveMigration(world, village, families);
                ResolveMarriages(world, village);
            }

            for (var i = 0; i < families.Count; i++)
            {
                for (var memberIndex = 0;
                     memberIndex < families[i].MemberIds.Count;
                     memberIndex++)
                {
                    var person = FindPerson(world, families[i].MemberIds[memberIndex]);
                    if (person.IsAlive)
                    {
                        person.NextIndependentEventDay = world.AbsoluteDay + 30;
                        person.NextIndependentEventReason =
                            "monthly_household_settlement";
                    }
                }
            }

            village.LastSettlementDay = world.AbsoluteDay;
            village.NextSettlementDay = world.AbsoluteDay + 30;
            RefreshCaches(world, village);
        }

        private static void ReleaseCompletedDuties(
            WorldState world,
            VillageState village)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person.LocationId == village.LocationId &&
                    person.LocalDuty != LocalDutyKind.None &&
                    person.LocalDutyUntilDay <= world.AbsoluteDay)
                {
                    person.LocalDuty = LocalDutyKind.None;
                    person.LocalDutyUntilDay = -1;
                }
            }
        }

        private static void UpdateLaborProfiles(
            WorldState world,
            VillageState village)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (!person.IsAlive || person.LocationId != village.LocationId)
                {
                    continue;
                }

                var age = Math.Max(
                    0, (world.AbsoluteDay - person.BirthDay) / DaysPerYear);
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
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var required = 0;
                var residents = new List<PersonState>();
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    var person = FindPerson(world, family.MemberIds[memberIndex]);
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
                    residents[residentIndex].HealthBasisPoints = Math.Max(
                        0, residents[residentIndex].HealthBasisPoints - damage);
                    residents[residentIndex].Needs.Livelihood = Math.Min(
                        10_000, residents[residentIndex].Needs.Livelihood + 1_000);
                }
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

        private static void ResolveHarvest(
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

        private static void ResolveTax(
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
                var paid = Math.Min(due, family.Grain);
                family.Grain -= paid;
                family.TaxArrearsGrain = due - paid;
                village.PublicGranaryGrain += paid;
                village.TaxGrainCollected += paid;
                AddLedger(
                    world, village, VillageLedgerEntryType.TaxPayment,
                    family.Id, string.Empty, -paid, paid,
                    (int)Math.Min(int.MaxValue, paid),
                    $"{family.DisplayName}缴纳税粮{paid}，欠税{family.TaxArrearsGrain}。");
            }
        }

        private static void ResolveTools(
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

        private static void ResolveMedicalCare(
            WorldState world,
            VillageState village)
        {
            var clinic = FindFacility(world, village.Id, VillageFacilityKind.Clinic);
            if (!FacilityOperational(world, village, VillageFacilityKind.Clinic) ||
                clinic.InventoryUnits <= 0)
            {
                return;
            }

            var patients = new List<PersonState>();
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                if (person.IsAlive && person.LocationId == village.LocationId &&
                    person.HealthBasisPoints < 9_500)
                {
                    patients.Add(person);
                }
            }

            patients.Sort((left, right) =>
            {
                var health = left.HealthBasisPoints.CompareTo(right.HealthBasisPoints);
                return health != 0
                    ? health
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            var treated = Math.Min(clinic.Capacity, patients.Count);
            treated = (int)Math.Min(treated, clinic.InventoryUnits);
            for (var i = 0; i < treated; i++)
            {
                var patient = patients[i];
                var recovery = Math.Min(200, 10_000 - patient.HealthBasisPoints);
                patient.HealthBasisPoints += recovery;
                clinic.InventoryUnits--;
                AddLedger(
                    world, village, VillageLedgerEntryType.MedicalCare,
                    patient.FamilyId, patient.Id, 0, 0, recovery,
                    $"{patient.DisplayName}接受村中医护，恢复{recovery}健康。");
            }
        }

        private static void ResolveCorvee(
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
                var person = candidates[i];
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
                    var candidate = FindPerson(
                        world, family.MemberIds[memberIndex]);
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

        private static void ResolveMarriages(WorldState world, VillageState village)
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

        private static List<PersonState> EligibleMarriageCandidates(
            WorldState world,
            VillageState village,
            PersonGender gender)
        {
            var result = new List<PersonState>();
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
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

        private static PersonState SelectWorker(
            WorldState world,
            VillageState village,
            FamilyState family,
            bool includeSpecialists)
        {
            PersonState selected = null;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = FindPerson(world, family.MemberIds[i]);
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

        private static int HouseholdLabor(
            WorldState world,
            VillageState village,
            FamilyState family)
        {
            var total = 0;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var person = FindPerson(world, family.MemberIds[i]);
                if (person.IsAlive && person.LocationId == village.LocationId &&
                    person.LocalDuty == LocalDutyKind.None)
                {
                    total += person.LaborCapacityBasisPoints;
                }
            }

            return Math.Min(10_000, total / Math.Max(1, family.FarmlandUnits / 4));
        }

        private static bool FacilityOperational(
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

            var manager = FindPerson(world, facility.ManagerPersonId);
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

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
        }
    }
}
