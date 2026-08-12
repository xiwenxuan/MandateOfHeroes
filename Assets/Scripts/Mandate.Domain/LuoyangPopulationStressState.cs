using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public enum StressSimulationTier : byte
    {
        PersistentData,
        LowFrequency,
        MediumFrequency,
        HighFrequencyActor
    }

    public enum StressConstructionStatus : byte
    {
        Planned,
        Approved,
        UnderConstruction,
        Completed,
        Cancelled,
        Failed
    }

    [Serializable]
    public sealed class LuoyangStressPersonState
    {
        public string PersonId;
        public string HouseholdId;
        public int Age;
        public string SexId;
        public int HealthBasisPoints = 10_000;
        public string CurrentActivityId;
        public string ResidenceFacilityId;
        public string WorkFacilityId;
        public string ProfessionId;
        public string PrimarySkillId;
        public int PrimarySkillBasisPoints;
        public bool IsAlive = true;
        public bool IsActiveMilitary;
        public bool IsLaborEligible;
        public ulong CurrentCellId64;
        public ulong OriginCellId64;
        public string AdministrativeRelationId;
        public int DailyConsumptionBasisPoints = 10_000;
        public long NextScheduledUpdateDay;
        public StressSimulationTier SimulationTier;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PersonId) ||
                string.IsNullOrWhiteSpace(HouseholdId) ||
                Age < 0 || Age > 130 ||
                string.IsNullOrWhiteSpace(SexId) ||
                HealthBasisPoints < 0 || HealthBasisPoints > 10_000 ||
                string.IsNullOrWhiteSpace(CurrentActivityId) ||
                string.IsNullOrWhiteSpace(ProfessionId) ||
                string.IsNullOrWhiteSpace(AdministrativeRelationId) ||
                DailyConsumptionBasisPoints < 0 ||
                NextScheduledUpdateDay < 0 ||
                !Enum.IsDefined(typeof(StressSimulationTier), SimulationTier))
            {
                throw new InvalidOperationException("Invalid permanent Luoyang stress Person record.");
            }

            if (IsActiveMilitary &&
                !string.Equals(ProfessionId, "profession.military", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Active military Person must use the military profession.");
            }
        }
    }

    [Serializable]
    public sealed class LuoyangStressPressureState
    {
        public int HousingPressureBasisPoints;
        public int EmploymentPressureBasisPoints;
        public int LaborShortagePressureBasisPoints;
        public int SkillShortagePressureBasisPoints;
        public int FoodPressureBasisPoints;
        public int StoragePressureBasisPoints;
        public int MarketPressureBasisPoints;
        public int InfrastructurePressureBasisPoints;
        public int MilitaryPressureBasisPoints;
        public int EducationPressureBasisPoints;
        public int LandPressureBasisPoints;
        public int TreasuryPressureBasisPoints;

        public int Read(string pressureId)
        {
            switch (pressureId)
            {
                case "pressure.housing": return HousingPressureBasisPoints;
                case "pressure.employment": return EmploymentPressureBasisPoints;
                case "pressure.labor_shortage": return LaborShortagePressureBasisPoints;
                case "pressure.skill_shortage": return SkillShortagePressureBasisPoints;
                case "pressure.food": return FoodPressureBasisPoints;
                case "pressure.storage": return StoragePressureBasisPoints;
                case "pressure.market": return MarketPressureBasisPoints;
                case "pressure.infrastructure": return InfrastructurePressureBasisPoints;
                case "pressure.military": return MilitaryPressureBasisPoints;
                case "pressure.education": return EducationPressureBasisPoints;
                case "pressure.land": return LandPressureBasisPoints;
                case "pressure.treasury": return TreasuryPressureBasisPoints;
                default: throw new ArgumentOutOfRangeException(nameof(pressureId), pressureId, "Unknown pressure ID.");
            }
        }

        public void Validate()
        {
            var values = new[]
            {
                HousingPressureBasisPoints, EmploymentPressureBasisPoints,
                LaborShortagePressureBasisPoints, SkillShortagePressureBasisPoints,
                FoodPressureBasisPoints, StoragePressureBasisPoints,
                MarketPressureBasisPoints, InfrastructurePressureBasisPoints,
                MilitaryPressureBasisPoints, EducationPressureBasisPoints,
                LandPressureBasisPoints, TreasuryPressureBasisPoints
            };
            if (values.Any(value => value < 0 || value > 10_000))
                throw new InvalidOperationException("Luoyang stress pressure must be within 0..10000.");
        }
    }

    [Serializable]
    public sealed class LuoyangStressConstructionCandidateDefinition
    {
        public string Id;
        public string FacilityDefinitionId;
        public string CategoryId;
        public string PrimaryPressureId;
        public int MinimumPressureBasisPoints;
        public int PressureWeightBasisPoints = 10_000;
        public int TreasuryCost;
        public int MaterialCost;
        public int ConstructionWorkerDays;
        public int CellCount = 1;
        public int JobCapacityAdded;
        public int MinimumWorkersForOperation;
        public bool HistoricalProtected;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) ||
                string.IsNullOrWhiteSpace(FacilityDefinitionId) ||
                string.IsNullOrWhiteSpace(CategoryId) ||
                string.IsNullOrWhiteSpace(PrimaryPressureId) ||
                MinimumPressureBasisPoints < 0 || MinimumPressureBasisPoints > 10_000 ||
                PressureWeightBasisPoints < 0 ||
                TreasuryCost < 0 || MaterialCost < 0 || ConstructionWorkerDays < 0 ||
                CellCount <= 0 || JobCapacityAdded < 0 || MinimumWorkersForOperation < 0)
            {
                throw new InvalidOperationException("Invalid data-driven stress construction candidate.");
            }
        }
    }

    [Serializable]
    public sealed class LuoyangStressConstructionContext
    {
        public int AvailableTreasury;
        public int AvailableMaterials;
        public int AvailableConstructionWorkers;
        public int AvailableDevelopableCells;
    }

    [Serializable]
    public sealed class LuoyangStressConstructionDecision
    {
        public string CandidateDefinitionId;
        public string FacilityDefinitionId;
        public string PressureSourceId;
        public int PressureBasisPoints;
        public long Score;
        public bool IsFeasible;
        public List<string> Reasons = new List<string>();
    }

    public static class LuoyangStressConstructionRules
    {
        public static IReadOnlyList<LuoyangStressConstructionDecision> Rank(
            LuoyangStressPressureState pressure,
            LuoyangStressConstructionContext context,
            IEnumerable<LuoyangStressConstructionCandidateDefinition> definitions,
            int maximumResults = 3)
        {
            if (pressure == null) throw new ArgumentNullException(nameof(pressure));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (maximumResults <= 0) throw new ArgumentOutOfRangeException(nameof(maximumResults));
            pressure.Validate();

            var decisions = new List<LuoyangStressConstructionDecision>();
            foreach (var definition in definitions)
            {
                definition.Validate();
                var value = pressure.Read(definition.PrimaryPressureId);
                var decision = new LuoyangStressConstructionDecision
                {
                    CandidateDefinitionId = definition.Id,
                    FacilityDefinitionId = definition.FacilityDefinitionId,
                    PressureSourceId = definition.PrimaryPressureId,
                    PressureBasisPoints = value,
                    Score = (long)value * definition.PressureWeightBasisPoints,
                    IsFeasible = true
                };

                if (definition.HistoricalProtected)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("historical_definition_not_a_stress_expansion_candidate");
                }
                if (value < definition.MinimumPressureBasisPoints)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("pressure_below_threshold");
                }
                if (context.AvailableTreasury < definition.TreasuryCost)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("treasury_insufficient");
                }
                if (context.AvailableMaterials < definition.MaterialCost)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("materials_insufficient");
                }
                if (context.AvailableConstructionWorkers <= 0 && definition.ConstructionWorkerDays > 0)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("construction_labor_unavailable");
                }
                if (context.AvailableDevelopableCells < definition.CellCount)
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("developable_land_unavailable");
                }
                if (pressure.LaborShortagePressureBasisPoints >= 7_500 &&
                    definition.JobCapacityAdded > definition.MinimumWorkersForOperation * 4 &&
                    definition.PrimaryPressureId != "pressure.food" &&
                    definition.PrimaryPressureId != "pressure.housing")
                {
                    decision.IsFeasible = false;
                    decision.Reasons.Add("existing_labor_shortage_blocks_job_expansion");
                }
                if (pressure.TreasuryPressureBasisPoints >= 8_000 && definition.TreasuryCost > 0)
                {
                    decision.Score /= 4;
                    decision.Reasons.Add("treasury_pressure_penalty");
                }

                decisions.Add(decision);
            }

            return decisions
                .OrderByDescending(item => item.IsFeasible)
                .ThenByDescending(item => item.Score)
                .ThenBy(item => item.CandidateDefinitionId, StringComparer.Ordinal)
                .Take(maximumResults)
                .ToArray();
        }
    }

    [Serializable]
    public sealed class LuoyangStressConstructionProjectState
    {
        public string Id;
        public string CandidateDefinitionId;
        public string FacilityId;
        public ulong CellId64;
        public string OwnerId;
        public string ControllerId;
        public string PressureSourceId;
        public int CreatedDay;
        public int ApprovedDay = -1;
        public int StartedDay = -1;
        public int CompletedDay = -1;
        public StressConstructionStatus Status;

        public void Approve(int day)
        {
            Require(StressConstructionStatus.Planned);
            if (day < CreatedDay) throw new ArgumentOutOfRangeException(nameof(day));
            ApprovedDay = day;
            Status = StressConstructionStatus.Approved;
        }

        public void Start(int day)
        {
            Require(StressConstructionStatus.Approved);
            if (day < ApprovedDay) throw new ArgumentOutOfRangeException(nameof(day));
            StartedDay = day;
            Status = StressConstructionStatus.UnderConstruction;
        }

        public void Complete(int day)
        {
            Require(StressConstructionStatus.UnderConstruction);
            if (day <= StartedDay) throw new ArgumentOutOfRangeException(nameof(day));
            CompletedDay = day;
            Status = StressConstructionStatus.Completed;
        }

        public void Cancel()
        {
            if (Status == StressConstructionStatus.Completed || Status == StressConstructionStatus.Failed)
                throw new InvalidOperationException("Completed or failed construction cannot be cancelled.");
            Status = StressConstructionStatus.Cancelled;
        }

        private void Require(StressConstructionStatus expected)
        {
            if (Status != expected)
                throw new InvalidOperationException($"Construction transition requires {expected}, found {Status}.");
        }
    }

    public sealed class LuoyangStressWorkerIndex
    {
        private readonly Dictionary<string, List<LuoyangStressPersonState>> _availableByProfession =
            new Dictionary<string, List<LuoyangStressPersonState>>(StringComparer.Ordinal);

        public int Count { get; private set; }

        public void Add(LuoyangStressPersonState person)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            person.Validate();
            if (!person.IsAlive || !person.IsLaborEligible || !string.IsNullOrEmpty(person.WorkFacilityId)) return;
            if (!_availableByProfession.TryGetValue(person.ProfessionId, out var people))
            {
                people = new List<LuoyangStressPersonState>();
                _availableByProfession.Add(person.ProfessionId, people);
            }
            people.Add(person);
            Count++;
        }

        public IReadOnlyList<LuoyangStressPersonState> FindByProfession(string professionId, int maximum)
        {
            if (string.IsNullOrWhiteSpace(professionId)) throw new ArgumentException("Profession is required.", nameof(professionId));
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            return _availableByProfession.TryGetValue(professionId, out var people)
                ? people.OrderByDescending(person => person.PrimarySkillBasisPoints)
                    .ThenBy(person => person.PersonId, StringComparer.Ordinal)
                    .Take(maximum).ToArray()
                : Array.Empty<LuoyangStressPersonState>();
        }
    }

    public sealed class LuoyangStressResidenceIndex
    {
        private readonly Dictionary<string, Queue<FacilityState>> _availableByResidentType =
            new Dictionary<string, Queue<FacilityState>>(StringComparer.Ordinal);
        private readonly Dictionary<string, FacilityDefinitionState> _definitionByFacility =
            new Dictionary<string, FacilityDefinitionState>(StringComparer.Ordinal);

        public void Add(FacilityDefinitionState definition, FacilityState facility)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (!string.Equals(definition.Id, facility.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Residence facility definition mismatch.");
            _definitionByFacility.Add(facility.Id, definition);
            foreach (var residentType in definition.AllowedResidentTypeIds.Distinct(StringComparer.Ordinal))
            {
                if (!_availableByResidentType.TryGetValue(residentType, out var queue))
                {
                    queue = new Queue<FacilityState>();
                    _availableByResidentType.Add(residentType, queue);
                }
                if (facility.ResidentPersonIds.Count < definition.ResidentialCapacityPersons) queue.Enqueue(facility);
            }
        }

        public bool TryAssign(FacilityPersonFact person, out FacilityState assigned)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            var residentType = person.IsActiveMilitary
                ? FacilityPopulationTypeIds.ActiveMilitary
                : person.PopulationTypeId;
            if (!_availableByResidentType.TryGetValue(residentType, out var queue))
            {
                assigned = null;
                return false;
            }

            while (queue.Count > 0)
            {
                var facility = queue.Peek();
                var definition = _definitionByFacility[facility.Id];
                if (!FacilityHousingRules.TryAssign(definition, facility, person, out _))
                {
                    queue.Dequeue();
                    continue;
                }
                assigned = facility;
                if (facility.ResidentPersonIds.Count >= definition.ResidentialCapacityPersons) queue.Dequeue();
                return true;
            }

            assigned = null;
            return false;
        }
    }

    public static class LuoyangStressPopulationRules
    {
        public static void ValidateCounts(int permanentPersons, int housedPersons, int unhousedPersons)
        {
            if (permanentPersons < 0 || housedPersons < 0 || unhousedPersons < 0 ||
                housedPersons + unhousedPersons != permanentPersons)
            {
                throw new InvalidOperationException("Housed plus unhoused must equal permanent Person count.");
            }
        }

        public static void ValidateUniqueAssignments(IEnumerable<LuoyangStressPersonState> people)
        {
            if (people == null) throw new ArgumentNullException(nameof(people));
            var personIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var person in people)
            {
                person.Validate();
                if (!personIds.Add(person.PersonId))
                    throw new InvalidOperationException("Duplicate permanent Person ID: " + person.PersonId);
            }
        }
    }
}
