using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public enum FacilityConstructionStatus : byte
    {
        Planned,
        InProgress,
        Completed,
        Cancelled
    }

    public enum FacilityConstructionProjectKind : byte
    {
        NewBuild,
        Repair,
        Expansion
    }

    public enum CellPropertyTransferKind : byte
    {
        Purchase,
        Sale,
        Gift,
        AdministrativeGrant
    }

    [Serializable]
    public sealed class WorldCellPropertyState
    {
        public string Id;
        public ulong CellId64;
        public string LocationId;
        public string OwnerId;
        public string AdministrativeControllerId;
        public long AcquiredDay;
        public long LastTransferDay;
        public long LastTransferPrice;
        public int Revision;
    }

    [Serializable]
    public sealed class CellPropertyTransferState
    {
        public string Id;
        public ulong CellId64;
        public string LocationId;
        public string FromOwnerId;
        public string ToOwnerId;
        public CellPropertyTransferKind Kind;
        public long Price;
        public long Day;
        public string AuthorizingPersonId;
    }

    [Serializable]
    public sealed class FacilityConstructionMaterialState
    {
        public string BatchId;
        public string ProductDefinitionId;
        public long ReservedQuantity;
        public long ConsumedQuantity;
    }

    [Serializable]
    public sealed class FacilityConstructionProjectState
    {
        public string Id;
        public string LocationId;
        public ulong CellId64;
        public string FacilityDefinitionId;
        public FacilityConstructionProjectKind Kind;
        public string TargetFacilityId;
        public string OwnerId;
        public string SponsorPersonId;
        public string MaterialInventoryContainerId;
        public long StartedDay;
        public long EarliestCompletionDay;
        public long CompletedDay = -1;
        public int RequiredLaborMinutes;
        public int CompletedLaborMinutes;
        public long MoneyCost;
        public FacilityConstructionStatus Status;
        public string ResultFacilityId;
        public List<FacilityConstructionMaterialState> Materials =
            new List<FacilityConstructionMaterialState>();
    }

    [Serializable]
    public sealed class FacilityConstructionLaborState
    {
        public string Id;
        public string ProjectId;
        public string WorkerPersonId;
        public long Day;
        public int LaborMinutes;
    }

    [Serializable]
    public sealed class HouseholdMigrationState
    {
        public string Id;
        public string FamilyId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string RouteId;
        public long StartedDay;
        public long CompletedDay = -1;
        public bool IsCompleted;
        public List<string> JourneyIds = new List<string>();
    }

    public static class PropertyConstructionRules
    {
        public static void ValidateWorld(WorldState world)
        {
            if (world.CellProperties == null ||
                world.CellPropertyTransfers == null ||
                world.FacilityConstructionProjects == null ||
                world.FacilityConstructionLabor == null ||
                world.HouseholdMigrations == null)
            {
                throw new InvalidOperationException(
                    "Property and construction collections cannot be null.");
            }

            var cells = new HashSet<ulong>();
            var propertyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in world.CellProperties)
            {
                if (property == null || string.IsNullOrWhiteSpace(property.Id) ||
                    property.CellId64 == 0 || !propertyIds.Add(property.Id) ||
                    !cells.Add(property.CellId64) ||
                    string.IsNullOrWhiteSpace(property.LocationId) ||
                    string.IsNullOrWhiteSpace(property.OwnerId) ||
                    property.AcquiredDay < 0 || property.LastTransferDay < 0 ||
                    property.LastTransferPrice < 0 || property.Revision < 0)
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate Cell property state.");
                }
            }

            var transferIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transfer in world.CellPropertyTransfers)
            {
                if (transfer == null || string.IsNullOrWhiteSpace(transfer.Id) ||
                    !transferIds.Add(transfer.Id) || transfer.CellId64 == 0 ||
                    string.IsNullOrWhiteSpace(transfer.LocationId) ||
                    string.IsNullOrWhiteSpace(transfer.ToOwnerId) ||
                    transfer.Price < 0 || transfer.Day < 0 ||
                    transfer.Day > world.AbsoluteDay ||
                    !Enum.IsDefined(typeof(CellPropertyTransferKind), transfer.Kind))
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate Cell property transfer.");
                }
            }

            var projectIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var project in world.FacilityConstructionProjects)
            {
                if (project == null || string.IsNullOrWhiteSpace(project.Id) ||
                    !projectIds.Add(project.Id) || project.CellId64 == 0 ||
                    string.IsNullOrWhiteSpace(project.LocationId) ||
                    string.IsNullOrWhiteSpace(project.FacilityDefinitionId) ||
                    !Enum.IsDefined(typeof(FacilityConstructionProjectKind),
                        project.Kind) ||
                    string.IsNullOrWhiteSpace(project.OwnerId) ||
                    string.IsNullOrWhiteSpace(project.SponsorPersonId) ||
                    project.StartedDay < 0 ||
                    project.EarliestCompletionDay < project.StartedDay ||
                    project.RequiredLaborMinutes <= 0 ||
                    project.CompletedLaborMinutes < 0 ||
                    project.CompletedLaborMinutes > project.RequiredLaborMinutes ||
                    project.MoneyCost < 0 ||
                    project.Materials == null || project.Materials.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Invalid Facility construction project.");
                }
                if (project.Kind != FacilityConstructionProjectKind.NewBuild &&
                    string.IsNullOrWhiteSpace(project.TargetFacilityId))
                {
                    throw new InvalidOperationException(
                        "Repair or expansion lacks a target Facility.");
                }
                if (project.Kind != FacilityConstructionProjectKind.NewBuild &&
                    !world.Facilities.Exists(item =>
                        item.Id == project.TargetFacilityId))
                {
                    throw new InvalidOperationException(
                        "Facility work references a missing target.");
                }
                if (project.Status == FacilityConstructionStatus.Completed &&
                    (project.CompletedDay < project.EarliestCompletionDay ||
                     string.IsNullOrWhiteSpace(project.ResultFacilityId)))
                {
                    throw new InvalidOperationException(
                        "Completed construction lacks time or Facility evidence.");
                }
            }

            foreach (var facility in world.Facilities)
            {
                if (facility == null || facility.ConditionBasisPoints < 0 ||
                    facility.ConditionBasisPoints > 10_000 ||
                    facility.RuntimeExpansionLevel < 0)
                {
                    throw new InvalidOperationException(
                        "Invalid Facility lifecycle state.");
                }
            }
        }
    }
}
