using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class Luoyang184UrbanHistoricalEventSystem
    {
        public Luoyang184ScenarioEventDefinition ApplyNext(
            Luoyang184UrbanScenarioState state,
            IReadOnlyList<Luoyang184ScenarioEventDefinition> definitions)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var next = definitions.OrderBy(item => item.Order)
                .FirstOrDefault(item => !state.AppliedEventIds.Contains(item.EventId));
            if (next == null)
            {
                return null;
            }

            Apply(state, next);
            return next;
        }

        public void Apply(Luoyang184UrbanScenarioState state, Luoyang184ScenarioEventDefinition definition)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (state.AppliedEventIds.Contains(definition.EventId))
            {
                return;
            }

            ValidateReferences(state, definition);
            foreach (var action in definition.Actions)
            {
                ApplyAction(state, action);
            }
            state.AppliedEventIds.Add(definition.EventId);
        }

        private static void ValidateReferences(Luoyang184UrbanScenarioState state, Luoyang184ScenarioEventDefinition definition)
        {
            foreach (var action in definition.Actions)
            {
                if (!string.IsNullOrEmpty(action.PersonId) && !state.HistoricalPeople.ContainsKey(action.PersonId))
                {
                    throw new InvalidOperationException("Unknown historical PersonId: " + action.PersonId);
                }
                if (!string.IsNullOrEmpty(action.ForceId) && !state.Forces.ContainsKey(action.ForceId))
                {
                    throw new InvalidOperationException("Unknown ForceId: " + action.ForceId);
                }
                if (!string.IsNullOrEmpty(action.ScopeForceId) && !state.Forces.ContainsKey(action.ScopeForceId))
                {
                    throw new InvalidOperationException("Unknown scoped ForceId: " + action.ScopeForceId);
                }
            }
        }

        private static void ApplyAction(Luoyang184UrbanScenarioState state, Luoyang184ScenarioActionDefinition action)
        {
            switch (action.TypeId)
            {
                case "person.set_activity":
                    state.HistoricalPeople[action.PersonId].CurrentActivityId = action.Value;
                    break;
                case "person.set_location":
                    state.HistoricalPeople[action.PersonId].CurrentLocationId = action.Value;
                    break;
                case "force.activate":
                    state.Forces[action.ForceId].Status = "Active";
                    break;
                case "force.deploy":
                    state.Forces[action.ForceId].Status = "Deployed";
                    break;
                case "person.pause_work":
                    state.PausedWorkForceIds.Add(action.ScopeForceId);
                    break;
                case "city.add_military_supply_pressure":
                    state.MilitarySupplyPressure = checked(state.MilitarySupplyPressure + action.NumericValue);
                    break;
                case "city.add_transport_pressure":
                    state.TransportPressure = checked(state.TransportPressure + action.NumericValue);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Luoyang 184 event action: " + action.TypeId);
            }
        }
    }

    public sealed class Luoyang184UrbanPopulationAuditTickResult
    {
        public int PersonCount { get; set; }
        public int HousedCount { get; set; }
        public int AssignedWorkCount { get; set; }
        public int ActiveWorkCount { get; set; }
        public int HouseholdCount { get; set; }
        public int HouseholdMemberCount { get; set; }
        public long DeterministicChecksum { get; set; }
        public double ElapsedMilliseconds { get; set; }
    }

    public sealed class Luoyang184UrbanPopulationAuditTickSystem
    {
        public Luoyang184UrbanPopulationAuditTickResult RunDaily(
            ILuoyang184UrbanPopulationSource source,
            Luoyang184UrbanScenarioState scenario,
            int chunkSize = 4096)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            var timer = Stopwatch.StartNew();
            var result = new Luoyang184UrbanPopulationAuditTickResult();
            for (var start = 0; start < source.Manifest.PersonCount; start += chunkSize)
            {
                var count = Math.Min(chunkSize, source.Manifest.PersonCount - start);
                foreach (var person in source.ReadPersons(start, count))
                {
                    result.PersonCount++;
                    if (person.ResidenceStatusIndex != 0) result.HousedCount++;
                    if (person.WorkFacilityIndex != uint.MaxValue)
                    {
                        result.AssignedWorkCount++;
                        if (!scenario.IsWorkPaused(person)) result.ActiveWorkCount++;
                    }
                    result.DeterministicChecksum = unchecked(
                        result.DeterministicChecksum * 31L + person.Ordinal + person.HealthBasisPoints + (long)person.CurrentCellId64);
                }
            }
            timer.Stop();
            result.ElapsedMilliseconds = timer.Elapsed.TotalMilliseconds;
            return result;
        }

        public Luoyang184UrbanPopulationAuditTickResult RunMonthly(
            ILuoyang184UrbanPopulationSource source,
            int chunkSize = 4096)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            var timer = Stopwatch.StartNew();
            var result = new Luoyang184UrbanPopulationAuditTickResult();
            for (var start = 0; start < source.Manifest.HouseholdCount; start += chunkSize)
            {
                var count = Math.Min(chunkSize, source.Manifest.HouseholdCount - start);
                foreach (var household in source.ReadHouseholds(start, count))
                {
                    result.HouseholdCount++;
                    result.HouseholdMemberCount += household.MemberCount;
                    result.DeterministicChecksum = unchecked(
                        result.DeterministicChecksum * 31L + household.Ordinal + household.HeadOrdinal + household.Wealth);
                }
            }
            timer.Stop();
            result.ElapsedMilliseconds = timer.Elapsed.TotalMilliseconds;
            return result;
        }
    }
}
