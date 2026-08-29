using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mandate.Domain;
using Mandate.Persistence;
using Mandate.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mandate.Tests
{
    public sealed class Luoyang184MetropolitanInitializationV1Tests
    {
        private static string RuntimeRoot => Path.Combine(
            Application.dataPath, "StreamingAssets", "WorldMap", "Luoyang184MetropolitanInitializationV1");

        [Test]
        public void CompositeManifestProtectsUrbanBaseAndReachesExactlyFourHundredThousand()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            Assert.That(reader.MetropolitanManifest.BasePersonCount, Is.EqualTo(270000));
            Assert.That(reader.MetropolitanManifest.AddedPersonCount, Is.EqualTo(130000));
            Assert.That(reader.Manifest.PersonCount, Is.EqualTo(400000));
            Assert.That(reader.Manifest.WalledCityPopulation, Is.EqualTo(200000));
            Assert.That(reader.Manifest.UrbanAreaPopulation, Is.EqualTo(270000));
            Assert.That(reader.Manifest.MetropolitanPlanPopulation, Is.EqualTo(400000));
            Assert.That(reader.Manifest.SupplyRegionPlanPopulation, Is.EqualTo(700000));
            Assert.That(reader.ValidatePackageFiles(), Is.Empty);
        }

        [Test]
        public void CompositeReaderPreservesEveryBaseBoundaryAndAppendsStableIds()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var baseReader = reader.BaseReader;
            var baseFirst = baseReader.ReadPersons(0, 1).Single();
            var baseLast = baseReader.ReadPersons(269999, 1).Single();
            var compositeFirst = reader.ReadPersons(0, 1).Single();
            var boundary = reader.ReadPersons(269999, 3).ToArray();
            Assert.That(compositeFirst.Ordinal, Is.EqualTo(baseFirst.Ordinal));
            Assert.That(compositeFirst.HouseholdOrdinal, Is.EqualTo(baseFirst.HouseholdOrdinal));
            Assert.That(boundary[0].Ordinal, Is.EqualTo(baseLast.Ordinal));
            Assert.That(boundary[0].CurrentCellId64, Is.EqualTo(baseLast.CurrentCellId64));
            Assert.That(boundary[1].Ordinal, Is.EqualTo(270000));
            Assert.That(boundary[2].Ordinal, Is.EqualTo(270001));
            Assert.That(reader.GetPersonId(269999), Is.EqualTo(baseReader.GetPersonId(269999)));
            Assert.That(reader.GetPersonId(270000), Is.EqualTo("person.luoyang.184.metropolitan.270001"));
            Assert.That(reader.GetPersonId(399999), Is.EqualTo("person.luoyang.184.metropolitan.400000"));
        }

        [Test]
        public void NewPeopleAndHouseholdsArePermanentHousedRelatedAndCapacityBound()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var residents = new Dictionary<uint, int>();
            var workers = new Dictionary<uint, int>();
            var expected = 270000u;
            var assigned = 0;
            foreach (var person in reader.ReadPersons(270000, 130000))
            {
                Assert.That(person.Ordinal, Is.EqualTo(expected));
                Assert.That(person.HouseholdOrdinal, Is.InRange(53992u, (uint)reader.Manifest.HouseholdCount - 1u));
                Assert.That(person.ResidenceFacilityIndex, Is.InRange(1230u, (uint)reader.Manifest.FacilityCount - 1u));
                Assert.That(person.ResidenceStatusIndex, Is.Not.Zero);
                Assert.That(person.DataOriginIndex, Is.EqualTo(2));
                AssertRelation(person.FatherOrdinal);
                AssertRelation(person.MotherOrdinal);
                AssertRelation(person.SpouseOrdinal);
                residents[person.ResidenceFacilityIndex] = residents.TryGetValue(person.ResidenceFacilityIndex, out var rc) ? rc + 1 : 1;
                if (person.WorkFacilityIndex != uint.MaxValue)
                {
                    assigned++;
                    workers[person.WorkFacilityIndex] = workers.TryGetValue(person.WorkFacilityIndex, out var wc) ? wc + 1 : 1;
                }
                expected++;
            }
            Assert.That(expected, Is.EqualTo(400000));
            Assert.That(assigned, Is.EqualTo(72000));
            foreach (var facility in reader.Facilities)
            {
                residents.TryGetValue((uint)facility.GlobalFacilityIndex, out var residentCount);
                workers.TryGetValue((uint)facility.GlobalFacilityIndex, out var workerCount);
                Assert.That(residentCount, Is.EqualTo(facility.CurrentResidents));
                Assert.That(workerCount, Is.EqualTo(facility.CurrentWorkers));
                Assert.That(residentCount, Is.LessThanOrEqualTo(facility.ResidentialCapacity));
                Assert.That(workerCount, Is.LessThanOrEqualTo(facility.WorkerCapacity));
            }
        }

        [Test]
        public void NewHouseholdsCoverOnlyTheAppendRangeWithoutGaps()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var expectedHousehold = 53992u;
            var expectedPerson = 270000u;
            foreach (var household in reader.ReadHouseholds(53992, reader.MetropolitanManifest.AddedHouseholdCount))
            {
                Assert.That(household.Ordinal, Is.EqualTo(expectedHousehold));
                Assert.That(household.MemberStartOrdinal, Is.EqualTo(expectedPerson));
                Assert.That(household.MemberCount, Is.GreaterThan(0));
                Assert.That(household.HeadOrdinal,
                    Is.InRange(household.MemberStartOrdinal, household.MemberStartOrdinal + household.MemberCount - 1u));
                expectedPerson += household.MemberCount;
                expectedHousehold++;
            }
            Assert.That(expectedPerson, Is.EqualTo(400000));
            Assert.That(expectedHousehold, Is.EqualTo(reader.Manifest.HouseholdCount));
        }

        [Test]
        public void FacilitiesHaveUniqueCellsOwnersAndSeparateAdministration()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            Assert.That(reader.Facilities.Count, Is.EqualTo(reader.MetropolitanManifest.AddedFacilityCount));
            Assert.That(reader.Facilities.Select(item => item.CellId64).Distinct().Count(), Is.EqualTo(reader.Facilities.Count));
            Assert.That(reader.Facilities.Select(item => item.FacilityId).Distinct().Count(), Is.EqualTo(reader.Facilities.Count));
            Assert.That(reader.Facilities.All(item => !string.IsNullOrEmpty(item.OwnerId)), Is.True);
            Assert.That(reader.Facilities.All(item => !string.IsNullOrEmpty(item.AdministrativeControllerId)), Is.True);
            Assert.That(reader.Facilities.Where(item => item.DefinitionId.Contains("field"))
                .All(item => item.CategoryId == "resource_agriculture"), Is.True);
        }

        [Test]
        public void EverySettlementHasARealGateRouteAndForceConsumesSupplyWhileMoving()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            Assert.That(reader.Routes.Count, Is.EqualTo(33));
            Assert.That(reader.Routes.Select(item => item.SettlementId).Distinct().Count(), Is.EqualTo(33));
            Assert.That(reader.Routes.All(item => item.CellIds.Count >= 2 && item.DistanceMetres > 0), Is.True);
            var route = reader.Routes.OrderByDescending(item => item.CellIds.Count).First();
            var journey = new Luoyang184MetropolitanForceJourneyState
            {
                ForceId = "force.han.luzhi_north", RouteId = route.RouteId, SupplyUnits = 10000,
            };
            var system = new Luoyang184MetropolitanForceTravelSystem();
            var before = journey.SupplyUnits;
            var cell = system.AdvanceOneDay(journey, route, 120);
            Assert.That(cell, Is.EqualTo(route.CellIds[1]));
            Assert.That(journey.SupplyUnits, Is.EqualTo(before - 120));
        }

        [Test]
        public void AgricultureRejectsPrematureHarvestAndWritesMatureInventory()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var field = reader.Agriculture.First();
            var state = new Luoyang184MetropolitanRuntimeState();
            var system = new Luoyang184MetropolitanAgricultureSystem();
            var premature = system.Harvest(state, field, field.PlantedDay + 1);
            Assert.That(premature.RejectedAsTooEarly, Is.True);
            Assert.That(state.InventoryUnitsByContainer, Is.Empty);
            var mature = system.Harvest(state, field, field.MaturityDay);
            Assert.That(mature.RejectedAsTooEarly, Is.False);
            Assert.That(mature.HarvestedUnits, Is.EqualTo(field.FullYieldUnits));
            Assert.That(state.InventoryUnitsByContainer[field.InventoryContainerId], Is.EqualTo(field.FullYieldUnits));
        }

        [Test]
        public void LogisticsUsesRealCarriersLossesAndUrbanDestinationInventory()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var state = new Luoyang184MetropolitanRuntimeState();
            var result = new Luoyang184MetropolitanLogisticsSystem().DeliverAll(state, reader.SupplyChains);
            Assert.That(result.ChainCount, Is.EqualTo(5));
            Assert.That(reader.SupplyChains.Select(item => item.ProductDefinitionId).Distinct().Count(), Is.EqualTo(5));
            Assert.That(reader.SupplyChains.All(item => item.CarrierPersonOrdinal >= 270000 && item.CarrierPersonOrdinal < 400000), Is.True);
            Assert.That(result.ShippedUnits, Is.EqualTo(result.LostUnits + result.DeliveredUnits));
            Assert.That(state.InventoryUnitsByContainer.Values.Sum(), Is.EqualTo(result.DeliveredUnits));
        }

        [Test]
        public void HistoricalEventsCanChangeSuburbanRecruitmentTransportLaborAndRefugees()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var state = new Luoyang184MetropolitanRuntimeState();
            var system = new Luoyang184MetropolitanEventImpactSystem();
            foreach (var impact in reader.EventImpacts) system.Apply(state, impact);
            foreach (var impact in reader.EventImpacts) system.Apply(state, impact);
            Assert.That(state.AppliedEventIds.Count, Is.EqualTo(4));
            Assert.That(state.RecruitmentPersons, Is.EqualTo(1200));
            Assert.That(state.TransportCapacityDelta, Is.EqualTo(-300));
            Assert.That(state.AgriculturalLaborDelta, Is.EqualTo(-240));
            Assert.That(state.RefugeePressure, Is.EqualTo(120));
            Assert.That(state.SecurityPressure, Is.EqualTo(120));
        }

        [Test]
        public void ChunkedCompositeDailyAndMonthlyTicksCoverFourHundredThousand()
        {
            var reader = new Luoyang184MetropolitanInitializationReader(RuntimeRoot);
            var state = reader.BaseReader.BuildScenarioState();
            var system = new Luoyang184UrbanPopulationAuditTickSystem();
            var timer = Stopwatch.StartNew();
            var daily = system.RunDaily(reader, state, 8192);
            var monthly = system.RunMonthly(reader, 8192);
            timer.Stop();
            Assert.That(daily.PersonCount, Is.EqualTo(400000));
            Assert.That(daily.HousedCount, Is.EqualTo(400000));
            Assert.That(daily.AssignedWorkCount, Is.EqualTo(249962));
            Assert.That(monthly.HouseholdCount, Is.EqualTo(reader.Manifest.HouseholdCount));
            Assert.That(monthly.HouseholdMemberCount, Is.EqualTo(400000));
            TestContext.WriteLine("Composite 400K daily ms=" + daily.ElapsedMilliseconds.ToString("F3"));
            TestContext.WriteLine("Composite households monthly ms=" + monthly.ElapsedMilliseconds.ToString("F3"));
            TestContext.WriteLine("Combined wall ms=" + timer.Elapsed.TotalMilliseconds.ToString("F3"));
        }

        [Test]
        public void LuoyangOuterSupplyCatchmentTests_ProjectionIsReferentiallyValidAndClosesPopulationGap()
        {
            var worldMapRoot = Path.Combine(Application.dataPath,
                "StreamingAssets", "WorldMap");
            var timer = Stopwatch.StartNew();
            var reader = new LuoyangOuterSupplyCatchmentV1Reader(
                Path.Combine(worldMapRoot,
                    "LuoyangOuterSupplyCatchmentV1"));
            var audit = reader.Audit();
            timer.Stop();
            Assert.That(audit.CriticalReferencesPassed, Is.True,
                string.Join(",", audit.CriticalReferenceErrors));
            Assert.That(reader.Manifest.IsProjectionOnly, Is.True);
            Assert.That(reader.Manifest.AdministrativeEffect,
                Is.EqualTo("none"));
            Assert.That(audit.CellCount, Is.EqualTo(1564));
            Assert.That(audit.FacilityCount, Is.EqualTo(1549));
            Assert.That(audit.SettlementCount, Is.EqualTo(33));
            Assert.That(audit.AgricultureUnitCount, Is.EqualTo(135));
            Assert.That(audit.StorageFacilityCount, Is.EqualTo(22));
            Assert.That(audit.RoadFacilityCount, Is.EqualTo(267));
            Assert.That(audit.MaterializedOuterPopulation,
                Is.EqualTo(430_000));
            Assert.That(audit.MaterializedOuterHouseholds,
                Is.EqualTo(88_988));
            Assert.That(audit.MaterializedWorldPopulation,
                Is.EqualTo(700_000));
            Assert.That(audit.InclusivePopulationTarget,
                Is.EqualTo(700_000));
            Assert.That(audit.UnmaterializedPopulationGap,
                Is.Zero);
            Assert.That(audit.PopulationTargetMaterialized, Is.True);
            Assert.That(reader.Definition.FoodProductDefinitionIds,
                Does.Contain(CoreProductionContent.WheatGrainProductId));
            Assert.That(reader.Definition.WoodProductDefinitionIds,
                Does.Contain(CoreProductionContent.TimberMaterialProductId));
            Assert.That(timer.ElapsedMilliseconds, Is.LessThan(3_000));
            TestContext.WriteLine(
                "OUTER_SUPPLY_CATCHMENT init_ms=" +
                timer.ElapsedMilliseconds + " cells=" + audit.CellCount +
                " facilities=" + audit.FacilityCount +
                " settlements=" + audit.SettlementCount +
                " materialized_outer_population=" +
                audit.MaterializedOuterPopulation +
                " population_gap=" + audit.UnmaterializedPopulationGap);
        }

        private static void AssertRelation(int ordinal)
        {
            Assert.That(ordinal == -1 || ordinal >= 270000 && ordinal < 400000, Is.True);
        }
    }
}
