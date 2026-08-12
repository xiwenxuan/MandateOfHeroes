using System;
using System.Collections.Generic;
using System.Linq;

namespace Mandate.Domain
{
    public enum HistoricalConfidenceLevel : byte
    {
        HistoricalAnchor,
        HistoricalReconstruction,
        GameplayReconstruction
    }

    public enum HistoricalSpatialPrecision : byte
    {
        Confirmed,
        Probable,
        Approximate
    }

    public static class FacilityPopulationTypeIds
    {
        public const string Civilian = "population.civilian";
        public const string ActiveMilitary = "population.active_military";
        public const string TemporaryGuest = "population.temporary_guest";
    }

    public enum FacilityLifecycleStatus : byte
    {
        Operational,
        Disabled,
        Destroyed,
        Abandoned
    }

    public enum FacilityPersonAssignmentAuthority : byte
    {
        InlineLists,
        ExternalPermanentPopulationPackage
    }

    [Serializable]
    public sealed class FacilityDefinitionState
    {
        public string Id;
        public string DisplayName;
        public string CategoryId;
        public int ResidentialCapacityPersons;
        public int MinimumWorkersForNormalOperation;
        public int WorkerCapacity;
        public List<string> AllowedResidentTypeIds = new List<string>();
        public List<string> PurposeIds = new List<string>();
        public List<string> CapabilityIds = new List<string>();
        public List<string> FutureHookIds = new List<string>();
    }

    [Serializable]
    public sealed class FacilityPersonFact
    {
        public string PersonId;
        public string PopulationTypeId;
        public string ProfessionId;
        public bool IsAlive = true;
        public bool IsActiveMilitary;
        public string CurrentCellId;
        public Dictionary<string, int> SkillsByDefinitionId = new Dictionary<string, int>(StringComparer.Ordinal);
        public HashSet<string> TraitIds = new HashSet<string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class FacilityState
    {
        public string Id;
        public string DisplayName;
        public string DefinitionId;
        public ulong CellId64;
        public string OwnerId;
        public string ControllerId;
        public string AdministrativeControllerId;
        public string SettlementId;
        public HistoricalConfidenceLevel HistoricalConfidence;
        public HistoricalSpatialPrecision SpatialPrecision;
        public string SourceNote;
        public FacilityLifecycleStatus LifecycleStatus =
            FacilityLifecycleStatus.Operational;
        public FacilityPersonAssignmentAuthority PersonAssignmentAuthority =
            FacilityPersonAssignmentAuthority.InlineLists;
        public int ResidentPersonCount;
        public int WorkerPersonCount;
        public int StudentPersonCount;
        public long StorageCapacity;
        public List<string> ResidentPersonIds = new List<string>();
        public List<string> WorkerPersonIds = new List<string>();
        public List<string> ServicePersonIds = new List<string>();

        public bool HasNormalProduction(FacilityDefinitionState definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!string.Equals(definition.Id, DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Facility definition mismatch.");
            return WorkerPersonIds.Count >= definition.MinimumWorkersForNormalOperation;
        }
    }

    public static class FacilityHousingRules
    {
        public static bool CanHouse(FacilityDefinitionState definition, FacilityPersonFact person)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (!person.IsAlive || definition.ResidentialCapacityPersons <= 0) return false;
            if (definition.AllowedResidentTypeIds.Contains(FacilityPopulationTypeIds.ActiveMilitary) &&
                !person.IsActiveMilitary) return false;
            return definition.AllowedResidentTypeIds.Contains(person.PopulationTypeId) ||
                   person.IsActiveMilitary && definition.AllowedResidentTypeIds.Contains(FacilityPopulationTypeIds.ActiveMilitary);
        }

        public static bool TryAssign(FacilityDefinitionState definition, FacilityState facility,
            FacilityPersonFact person, out string reason)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (!string.Equals(definition?.Id, facility.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Facility definition mismatch.");
            if (!CanHouse(definition, person))
            {
                reason = "resident_not_eligible";
                return false;
            }
            if (facility.ResidentPersonIds.Contains(person.PersonId))
            {
                reason = "already_resident";
                return false;
            }
            if (facility.ResidentPersonIds.Count >= definition.ResidentialCapacityPersons)
            {
                reason = "person_capacity_full";
                return false;
            }
            facility.ResidentPersonIds.Add(person.PersonId);
            reason = null;
            return true;
        }

        public static bool TryRemove(FacilityState facility, string personId, out string reason)
        {
            if (facility == null) throw new ArgumentNullException(nameof(facility));
            if (string.IsNullOrWhiteSpace(personId)) throw new ArgumentException("Person ID is required.", nameof(personId));
            if (!facility.ResidentPersonIds.Remove(personId))
            {
                reason = "person_not_resident";
                return false;
            }
            reason = null;
            return true;
        }
    }

    [Serializable]
    public sealed class FacilityJobDefinitionState
    {
        public string Id;
        public string FacilityDefinitionId;
        public string ProfessionId;
        public string PrimarySkillId;
        public int MinimumSkillBasisPoints;
        public int Capacity;
        public bool RequiresSameCell;
        public List<string> RequiredTraitIds = new List<string>();
        public List<string> ForbiddenTraitIds = new List<string>();
    }

    [Serializable]
    public sealed class JobFitResult
    {
        public bool Eligible;
        public int FitBasisPoints;
        public List<string> Reasons = new List<string>();
    }

    public static class FacilityJobRules
    {
        public static JobFitResult Evaluate(FacilityJobDefinitionState job, FacilityPersonFact person,
            string facilityCellId)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (person == null) throw new ArgumentNullException(nameof(person));
            var result = new JobFitResult { Eligible = true, FitBasisPoints = 3_000 };
            if (!person.IsAlive)
            {
                result.Eligible = false;
                result.Reasons.Add("person_not_alive");
            }
            if (!string.IsNullOrEmpty(job.ProfessionId) &&
                !string.Equals(job.ProfessionId, person.ProfessionId, StringComparison.Ordinal))
            {
                result.Eligible = false;
                result.Reasons.Add("profession_mismatch");
            }
            if (job.RequiresSameCell && !string.Equals(person.CurrentCellId, facilityCellId, StringComparison.Ordinal))
            {
                result.Eligible = false;
                result.Reasons.Add("person_not_present");
            }
            var skill = 0;
            if (!string.IsNullOrEmpty(job.PrimarySkillId))
                person.SkillsByDefinitionId.TryGetValue(job.PrimarySkillId, out skill);
            if (skill < job.MinimumSkillBasisPoints)
            {
                result.Eligible = false;
                result.Reasons.Add("skill_below_minimum");
            }
            foreach (var trait in job.RequiredTraitIds)
                if (!person.TraitIds.Contains(trait))
                {
                    result.Eligible = false;
                    result.Reasons.Add("missing_trait:" + trait);
                }
            foreach (var trait in job.ForbiddenTraitIds)
                if (person.TraitIds.Contains(trait))
                {
                    result.Eligible = false;
                    result.Reasons.Add("forbidden_trait:" + trait);
                }
            result.FitBasisPoints = Math.Max(0, Math.Min(10_000,
                result.FitBasisPoints + skill / 2 + (result.Eligible ? 1_500 : 0)));
            return result;
        }
    }

    [Serializable]
    public sealed class LocalDevelopmentPressureState
    {
        public int TotalPersons;
        public int HousedPersons;
        public int EffectiveWorkers;
        public int FilledJobs;
        public int AvailableResidentialPersonSlots;
        public int VacantJobSlots;
        public int SkillShortageSlots;
        public int FoodDaysBasisPoints;
        public int SecurityBasisPoints;

        public int UnhousedPersons => Math.Max(0, TotalPersons - HousedPersons);
        public int UnemployedWorkers => Math.Max(0, EffectiveWorkers - FilledJobs);
        public bool NeedsHousing => UnhousedPersons > 0 || AvailableResidentialPersonSlots < Math.Max(1, TotalPersons / 20);
        public bool NeedsJobs => UnemployedWorkers > 0 && VacantJobSlots == 0;
        public bool NeedsTraining => VacantJobSlots > 0 && SkillShortageSlots > 0;
    }

    public enum BlueprintOrientation : byte
    {
        North,
        East,
        South,
        West
    }

    public enum BlueprintConstructionStage : byte
    {
        Survey,
        Foundation,
        Structure,
        Services,
        Commissioning
    }

    [Serializable]
    public sealed class BlueprintCellDefinition
    {
        public int RelativeX;
        public int RelativeY;
        public string FacilityDefinitionId;
        public BlueprintConstructionStage Stage;
        public int BuildOrder;
        public List<string> RequiredRoadConnectionIds = new List<string>();
        public List<string> ModuleIds = new List<string>();
        public Dictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class FacilityBlueprintDefinition
    {
        public string Id;
        public string DisplayName;
        public BlueprintOrientation Orientation;
        public List<BlueprintCellDefinition> Cells = new List<BlueprintCellDefinition>();
        public Dictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class BlueprintPlacementCellFact
    {
        public int X;
        public int Y;
        public ulong CellId64;
        public bool Exists;
        public bool Developable;
        public string OwnerId;
        public string FacilityId;
        public HashSet<string> RoadConnectionIds = new HashSet<string>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class BlueprintPlacementResult
    {
        public bool IsValid;
        public List<string> Errors = new List<string>();
        public List<ulong> ReservedCellIds = new List<ulong>();
    }

    public static class FacilityBlueprintRules
    {
        public static BlueprintPlacementResult Validate(FacilityBlueprintDefinition blueprint, int originX,
            int originY, string actingOwnerId, Func<int, int, BlueprintPlacementCellFact> cellLookup)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));
            if (cellLookup == null) throw new ArgumentNullException(nameof(cellLookup));
            var result = new BlueprintPlacementResult();
            var relativeCells = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in blueprint.Cells.OrderBy(item => item.BuildOrder))
            {
                var rotated = Rotate(entry.RelativeX, entry.RelativeY, blueprint.Orientation);
                var relativeKey = rotated.Item1 + ":" + rotated.Item2;
                if (!relativeCells.Add(relativeKey))
                {
                    result.Errors.Add("duplicate_blueprint_cell:" + relativeKey);
                    continue;
                }
                var cell = cellLookup(originX + rotated.Item1, originY + rotated.Item2);
                if (cell == null || !cell.Exists)
                {
                    result.Errors.Add("cell_missing:" + relativeKey);
                    continue;
                }
                if (!cell.Developable) result.Errors.Add("cell_not_developable:" + cell.CellId64);
                if (!string.Equals(cell.OwnerId, actingOwnerId, StringComparison.Ordinal))
                    result.Errors.Add("owner_mismatch:" + cell.CellId64);
                if (!string.IsNullOrEmpty(cell.FacilityId)) result.Errors.Add("cell_occupied:" + cell.CellId64);
                foreach (var road in entry.RequiredRoadConnectionIds)
                    if (!cell.RoadConnectionIds.Contains(road))
                        result.Errors.Add("road_connection_missing:" + cell.CellId64 + ":" + road);
                result.ReservedCellIds.Add(cell.CellId64);
            }
            result.IsValid = result.Errors.Count == 0 && blueprint.Cells.Count > 0;
            return result;
        }

        private static Tuple<int, int> Rotate(int x, int y, BlueprintOrientation orientation)
        {
            switch (orientation)
            {
                case BlueprintOrientation.East: return Tuple.Create(-y, x);
                case BlueprintOrientation.South: return Tuple.Create(-x, -y);
                case BlueprintOrientation.West: return Tuple.Create(y, -x);
                default: return Tuple.Create(x, y);
            }
        }
    }

    public enum WallState : byte
    {
        Intact,
        Damaged,
        Breached,
        Destroyed
    }

    public enum GateOpenState : byte
    {
        Closed,
        Open,
        Destroyed
    }

    public enum MoatState : byte
    {
        Dry,
        Flooded,
        Filled,
        Bridged
    }

    [Serializable]
    public sealed class WallFacilityState
    {
        public string FacilityId;
        public string NetworkId;
        public ulong CellId64;
        public int HeightCentimetres;
        public int ThicknessCentimetres;
        public string MaterialId;
        public int MaximumDurability;
        public int CurrentDurability;
        public List<string> DefenderPersonIds = new List<string>();
        public WallState State;

        public bool BlocksForce => State == WallState.Intact || State == WallState.Damaged;

        public void ApplyDamage(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            CurrentDurability = Math.Max(0, CurrentDurability - damage);
            if (CurrentDurability == 0) State = WallState.Breached;
            else if (CurrentDurability < MaximumDurability) State = WallState.Damaged;
        }
    }

    [Serializable]
    public sealed class GateFacilityState
    {
        public string FacilityId;
        public string NetworkId;
        public ulong CellId64;
        public string OwnerId;
        public string ControllerId;
        public int MaximumDurability;
        public int CurrentDurability;
        public int PassageCapacityPerHour;
        public List<string> DefenderPersonIds = new List<string>();
        public GateOpenState OpenState;

        public bool CanPass(string forceControllerId) => OpenState == GateOpenState.Destroyed ||
            OpenState == GateOpenState.Open && string.Equals(ControllerId, forceControllerId, StringComparison.Ordinal);

        public void SetOpenState(string actingControllerId, GateOpenState state)
        {
            if (!string.Equals(ControllerId, actingControllerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Only the gate controller may change its state.");
            OpenState = state;
        }
    }

    [Serializable]
    public sealed class MoatFeatureState
    {
        public string Id;
        public ulong CellId64;
        public MoatState State;
        public int WidthCentimetres;
        public int DepthCentimetres;

        public bool BlocksOrdinaryMovement => State == MoatState.Flooded;
    }

    [Serializable]
    public sealed class FortificationNetworkState
    {
        public string Id;
        public string DisplayName;
        public string ParentNetworkId;
        public List<WallFacilityState> Walls = new List<WallFacilityState>();
        public List<GateFacilityState> Gates = new List<GateFacilityState>();
        public List<MoatFeatureState> Moats = new List<MoatFeatureState>();
    }

    public static class SiegePassabilityRules
    {
        public static bool CanCrossWall(WallFacilityState wall, int ladderEffectiveHeightCentimetres)
        {
            if (wall == null) throw new ArgumentNullException(nameof(wall));
            if (!wall.BlocksForce) return true;
            return ladderEffectiveHeightCentimetres >= wall.HeightCentimetres;
        }

        public static bool CanCrossMoat(MoatFeatureState moat, bool hasPreparedBridge)
        {
            if (moat == null) throw new ArgumentNullException(nameof(moat));
            return !moat.BlocksOrdinaryMovement || hasPreparedBridge || moat.State == MoatState.Bridged;
        }
    }
}
