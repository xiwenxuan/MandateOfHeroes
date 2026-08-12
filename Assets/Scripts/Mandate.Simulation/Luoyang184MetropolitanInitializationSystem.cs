using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class Luoyang184MetropolitanAgricultureSystem
    {
        public Luoyang184MetropolitanHarvestResult Harvest(
            Luoyang184MetropolitanRuntimeState state,
            Luoyang184MetropolitanAgricultureRecord field,
            int absoluteDay)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (field == null) throw new ArgumentNullException(nameof(field));
            var elapsed = Math.Max(0, absoluteDay - field.PlantedDay);
            var duration = Math.Max(1, field.MaturityDay - field.PlantedDay);
            var maturity = Math.Min(10000, checked(elapsed * 10000 / duration));
            if (maturity < field.EarlyHarvestMinimumBasisPoints)
            {
                return new Luoyang184MetropolitanHarvestResult
                {
                    FieldId = field.FieldId,
                    ProductDefinitionId = field.ProductDefinitionId,
                    MaturityBasisPoints = maturity,
                    RejectedAsTooEarly = true,
                };
            }

            var units = checked(field.FullYieldUnits * maturity / 10000L);
            state.InventoryUnitsByContainer.TryGetValue(field.InventoryContainerId, out var current);
            state.InventoryUnitsByContainer[field.InventoryContainerId] = checked(current + units);
            return new Luoyang184MetropolitanHarvestResult
            {
                FieldId = field.FieldId,
                ProductDefinitionId = field.ProductDefinitionId,
                MaturityBasisPoints = maturity,
                HarvestedUnits = units,
            };
        }
    }

    public sealed class Luoyang184MetropolitanLogisticsSystem
    {
        public Luoyang184MetropolitanLogisticsResult DeliverAll(
            Luoyang184MetropolitanRuntimeState state,
            IReadOnlyList<Luoyang184MetropolitanSupplyChainRecord> chains)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (chains == null) throw new ArgumentNullException(nameof(chains));
            var result = new Luoyang184MetropolitanLogisticsResult();
            foreach (var chain in chains)
            {
                if (chain.ShippedUnits != chain.NaturalLossUnits + chain.RoadLossUnits + chain.DeliveredUnits)
                    throw new InvalidOperationException("Metropolitan logistics conservation failed: " + chain.ChainId);
                result.ChainCount++;
                result.ShippedUnits = checked(result.ShippedUnits + chain.ShippedUnits);
                result.LostUnits = checked(result.LostUnits + chain.NaturalLossUnits + chain.RoadLossUnits);
                result.DeliveredUnits = checked(result.DeliveredUnits + chain.DeliveredUnits);
                result.CarrierConsumptionUnits = checked(result.CarrierConsumptionUnits + chain.CarrierConsumptionUnits);
                state.InventoryUnitsByContainer.TryGetValue(chain.DestinationFacilityId, out var current);
                state.InventoryUnitsByContainer[chain.DestinationFacilityId] = checked(current + chain.DeliveredUnits);
            }
            return result;
        }
    }

    public sealed class Luoyang184MetropolitanEventImpactSystem
    {
        public void Apply(Luoyang184MetropolitanRuntimeState state, Luoyang184MetropolitanEventImpact impact)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (impact == null) throw new ArgumentNullException(nameof(impact));
            if (!state.AppliedEventIds.Add(impact.EventId)) return;
            state.RecruitmentPersons = checked(state.RecruitmentPersons + impact.RecruitmentPersons);
            state.TransportCapacityDelta = checked(state.TransportCapacityDelta + impact.TransportCapacityDelta);
            if (impact.GrainPriceBasisPoints > 0) state.GrainPriceBasisPoints = impact.GrainPriceBasisPoints;
            state.MilitarySupplyUnits = checked(state.MilitarySupplyUnits + impact.MilitarySupplyUnits);
            state.RoadCapacityDelta = checked(state.RoadCapacityDelta + impact.RoadCapacityDelta);
            state.AgriculturalLaborDelta = checked(state.AgriculturalLaborDelta + impact.AgriculturalLaborDelta);
            state.RefugeePressure = checked(state.RefugeePressure + impact.RefugeePressure);
            state.SecurityPressure = checked(state.SecurityPressure + impact.SecurityPressure);
            state.RoadInspectionPressure = checked(state.RoadInspectionPressure + impact.RoadInspectionPressure);
        }
    }

    public sealed class Luoyang184MetropolitanForceTravelSystem
    {
        public ulong AdvanceOneDay(
            Luoyang184MetropolitanForceJourneyState journey,
            Luoyang184MetropolitanRouteRecord route,
            long dailySupplyUnits)
        {
            if (journey == null) throw new ArgumentNullException(nameof(journey));
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (!string.Equals(journey.RouteId, route.RouteId, StringComparison.Ordinal))
                throw new InvalidOperationException("The force journey references another route.");
            if (journey.Arrived) return route.CellIds[route.CellIds.Count - 1];
            if (journey.SupplyUnits < dailySupplyUnits)
                throw new InvalidOperationException("The force cannot advance without its daily supplies.");
            journey.SupplyUnits -= dailySupplyUnits;
            journey.CurrentRouteCellIndex = Math.Min(journey.CurrentRouteCellIndex + 1, route.CellIds.Count - 1);
            journey.Arrived = journey.CurrentRouteCellIndex == route.CellIds.Count - 1;
            return route.CellIds[journey.CurrentRouteCellIndex];
        }
    }
}
