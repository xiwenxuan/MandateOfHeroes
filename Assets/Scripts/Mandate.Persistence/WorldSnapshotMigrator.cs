using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public static class WorldSnapshotMigrator
    {
        public static WorldState MigrateToCurrent(
            WorldState world,
            ProductionContentRegistry productionContent = null)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.SchemaVersion <= 0 ||
                world.SchemaVersion > WorldState.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported world schema {world.SchemaVersion}.");
            }

            var content = productionContent ??
                          ProductionContentRegistry.CreateCore();

            while (world.SchemaVersion < WorldState.CurrentSchemaVersion)
            {
                switch (world.SchemaVersion)
                {
                    case 1:
                        MigrateVersionOneToTwo(world);
                        break;
                    case 2:
                        MigrateVersionTwoToThree(world);
                        break;
                    case 3:
                        MigrateVersionThreeToFour(world);
                        break;
                    case 4:
                        MigrateVersionFourToFive(world);
                        break;
                    case 5:
                        MigrateVersionFiveToSix(world);
                        break;
                    case 6:
                        MigrateVersionSixToSeven(world);
                        break;
                    case 7:
                        MigrateVersionSevenToEight(world);
                        break;
                    case 8:
                        MigrateVersionEightToNine(world);
                        break;
                    case 9:
                        MigrateVersionNineToTen(world, content);
                        break;
                    case 10:
                        MigrateVersionTenToEleven(world);
                        break;
                    case 11:
                        MigrateVersionElevenToTwelve(world);
                        break;
                    case 12:
                        MigrateVersionTwelveToThirteen(world);
                        break;
                    case 13:
                        MigrateVersionThirteenToFourteen(world, content);
                        break;
                    case 14:
                        MigrateVersionFourteenToFifteen(world, content);
                        break;
                    case 15:
                        MigrateVersionFifteenToSixteen(world, content);
                        break;
                    case 16:
                        MigrateVersionSixteenToSeventeen(world, content);
                        break;
                    case 17:
                        MigrateVersionSeventeenToEighteen(world, content);
                        break;
                    case 18:
                        MigrateVersionEighteenToNineteen(world);
                        break;
                    case 19:
                        MigrateVersionNineteenToTwenty(world);
                        break;
                    case 20:
                        MigrateVersionTwentyToTwentyOne(world);
                        break;
                    case 21:
                        MigrateVersionTwentyOneToTwentyTwo(world);
                        break;
                    case 22:
                        MigrateVersionTwentyTwoToTwentyThree(world);
                        break;
                    case 23:
                        MigrateVersionTwentyThreeToTwentyFour(world);
                        break;
                    case 24:
                        MigrateVersionTwentyFourToTwentyFive(world);
                        break;
                    case 25:
                        MigrateVersionTwentyFiveToTwentySix(world);
                        break;
                    case 26:
                        MigrateVersionTwentySixToTwentySeven(world);
                        break;
                    case 27:
                        MigrateVersionTwentySevenToTwentyEight(world);
                        break;
                    case 28:
                        MigrateVersionTwentyEightToTwentyNine(world);
                        break;
                    case 29:
                        MigrateVersionTwentyNineToThirty(world);
                        break;
                    case 30:
                        MigrateVersionThirtyToThirtyOne(world);
                        break;
                    case 31:
                        MigrateVersionThirtyOneToThirtyTwo(world);
                        break;
                    case 32:
                        MigrateVersionThirtyTwoToThirtyThree(world);
                        break;
                    case 33:
                        MigrateVersionThirtyThreeToThirtyFour(world);
                        break;
                    case 34:
                        MigrateVersionThirtyFourToThirtyFive(world);
                        break;
                    case 35:
                        MigrateVersionThirtyFiveToThirtySix(world);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"No migration path from schema {world.SchemaVersion}.");
                }
            }

            return world;
        }

        private static void MigrateVersionOneToTwo(WorldState world)
        {
            PopulationLedgerBootstrap.Initialize(world);
            world.SchemaVersion = 2;
        }

        private static void MigrateVersionTwoToThree(WorldState world)
        {
            CharacterAbilityBootstrap.InitializeWorld(world);
            world.SchemaVersion = 3;
        }

        private static void MigrateVersionThreeToFour(WorldState world)
        {
            world.EducationPlans ??= new System.Collections.Generic.List<
                EducationPlanState>();
            world.LearningRecords ??= new System.Collections.Generic.List<
                LearningRecordState>();
            world.SchemaVersion = 4;
        }

        private static void MigrateVersionFourToFive(WorldState world)
        {
            world.MilitaryFormations ??= new System.Collections.Generic.List<
                MilitaryFormationState>();
            world.MilitaryServices ??= new System.Collections.Generic.List<
                MilitaryServiceState>();
            world.MilitaryOrders ??= new System.Collections.Generic.List<
                MilitaryOrderState>();
            world.MilitaryServiceInitialized = false;
            world.SchemaVersion = 5;
        }

        private static void MigrateVersionFiveToSix(WorldState world)
        {
            world.Villages ??= new System.Collections.Generic.List<
                VillageState>();
            world.VillageFacilities ??= new System.Collections.Generic.List<
                VillageFacilityState>();
            world.VillageLedgerEntries ??= new System.Collections.Generic.List<
                VillageLedgerEntryState>();

            for (var personIndex = 0;
                 personIndex < world.People.Count;
                 personIndex++)
            {
                var person = world.People[personIndex];
                if (string.IsNullOrEmpty(person.BirthLocationId))
                {
                    person.BirthLocationId = person.PopulationOriginLocationId;
                    if (string.IsNullOrEmpty(person.BirthLocationId))
                    {
                        person.BirthLocationId = person.LocationId;
                    }
                }

                person.FamilyId = string.Empty;
                person.NextIndependentEventDay = -1;
                person.NextIndependentEventReason = string.Empty;
                person.LocalDuty = LocalDutyKind.None;
                person.LocalDutyUntilDay = -1;
            }

            for (var familyIndex = 0;
                 familyIndex < world.Families.Count;
                 familyIndex++)
            {
                var family = world.Families[familyIndex];
                var head = FindPerson(world, family.HeadPersonId);
                family.LocationId = head.LocationId;
                family.VillageId = string.Empty;
                for (var memberIndex = 0;
                     memberIndex < family.MemberIds.Count;
                     memberIndex++)
                {
                    FindPerson(world, family.MemberIds[memberIndex]).FamilyId =
                        family.Id;
                }
            }

            world.SchemaVersion = 6;
        }

        private static void MigrateVersionSixToSeven(WorldState world)
        {
            world.PopulationStorage =
                PopulationStorageState.CreateInline(world.People);
            world.SchemaVersion = 7;
        }

        private static void MigrateVersionSevenToEight(WorldState world)
        {
            world.AgricultureWorkOrders ??=
                new System.Collections.Generic.List<AgricultureWorkOrderState>();
            world.ProductionLedgerEntries ??=
                new System.Collections.Generic.List<ProductionLedgerEntryState>();
            world.ProductionContentManifest =
                ProductionContentRegistry.CreateCore().CreateManifest();
            world.SchemaVersion = 8;
        }

        private static void MigrateVersionEightToNine(WorldState world)
        {
            world.ResearchProjects ??=
                new System.Collections.Generic.List<ResearchProjectState>();
            world.TechnologyApplications ??=
                new System.Collections.Generic.List<TechnologyApplicationState>();
            world.ResearchLedgerEntries ??=
                new System.Collections.Generic.List<ResearchLedgerEntryState>();
            for (var i = 0; i < world.People.Count; i++)
            {
                var person = world.People[i];
                person.SkillMasteries ??=
                    new System.Collections.Generic.List<SkillMasteryState>();
                person.KnowledgeMasteries ??=
                    new System.Collections.Generic.List<KnowledgeMasteryState>();
                person.TechnologyMasteries ??=
                    new System.Collections.Generic.List<TechnologyMasteryState>();
            }

            world.ProductionContentManifest =
                ProductionContentRegistry.CreateCore().CreateManifest();
            world.SchemaVersion = 9;
        }

        private static void MigrateVersionNineToTen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductBatches ??=
                new System.Collections.Generic.List<ProductBatchState>();
            world.InventoryTransactions ??=
                new System.Collections.Generic.List<InventoryTransactionState>();
            world.ProcessingWorkOrders ??=
                new System.Collections.Generic.List<ProcessingWorkOrderState>();
            world.ProductionContentManifest =
                productionContent.CreateManifest();
            world.SchemaVersion = 10;
        }

        private static void MigrateVersionTenToEleven(WorldState world)
        {
            world.AttentionFocuses ??=
                new System.Collections.Generic.List<AttentionFocusState>();
            world.AttentionLedgerEntries ??=
                new System.Collections.Generic.List<AttentionLedgerEntryState>();
            world.SchemaVersion = 11;
        }

        private static void MigrateVersionElevenToTwelve(WorldState world)
        {
            world.CountyGovernances ??=
                new System.Collections.Generic.List<CountyGovernanceState>();
            world.CountyGentryHouses ??=
                new System.Collections.Generic.List<CountyGentryHouseState>();
            world.CountyHouseholdTaxes ??=
                new System.Collections.Generic.List<CountyHouseholdTaxState>();
            world.CountyFiscalLedgerEntries ??=
                new System.Collections.Generic.List<CountyFiscalLedgerEntryState>();
            world.SchemaVersion = 12;
        }

        private static void MigrateVersionTwelveToThirteen(WorldState world)
        {
            world.MilitaryEquipmentInitialized = false;
            world.MilitaryEquipmentDefinitions =
                new System.Collections.Generic.List<
                    MilitaryEquipmentDefinitionState>();
            world.MilitaryArmoryStocks =
                new System.Collections.Generic.List<MilitaryArmoryStockState>();
            world.MilitaryEquipmentIssues =
                new System.Collections.Generic.List<MilitaryEquipmentIssueState>();
            world.MilitaryEquipmentTransactions =
                new System.Collections.Generic.List<
                    MilitaryEquipmentTransactionState>();
            world.SchemaVersion = 13;
        }

        private static void MigrateVersionThirteenToFourteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.InventoryContainers =
                new System.Collections.Generic.List<InventoryContainerState>();
            world.MilitaryProcurementOrders =
                new System.Collections.Generic.List<
                    MilitaryProcurementOrderState>();
            world.MilitaryProcurementLedgerEntries =
                new System.Collections.Generic.List<
                    MilitaryProcurementLedgerEntryState>();

            world.ProductBatches.RemoveAll(batch =>
                !string.IsNullOrEmpty(batch.OwnerOrganizationId) ||
                !string.IsNullOrEmpty(batch.InventoryContainerId));
            world.InventoryTransactions.RemoveAll(transaction =>
                !string.IsNullOrEmpty(
                    transaction.SourceMilitaryProcurementId) ||
                transaction.Type == InventoryTransactionType.OpeningBalance ||
                transaction.Type ==
                    InventoryTransactionType.MilitaryProcurementDispatched);

            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                var definition = world.MilitaryEquipmentDefinitions[i];
                definition.ProductDefinitionId =
                    ProductForEquipment(definition.Id);
            }

            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 14;
        }

        private static string ProductForEquipment(string equipmentId)
        {
            switch (equipmentId)
            {
                case "equipment.han_ring_sword":
                    return CoreProductionContent.RingSwordProductId;
                case "equipment.wooden_shield":
                    return CoreProductionContent.WoodenShieldProductId;
                case "equipment.long_spear":
                    return CoreProductionContent.LongSpearProductId;
                case "equipment.horn_bow":
                    return CoreProductionContent.HornBowProductId;
                case "equipment.arrow_bundle":
                    return CoreProductionContent.ArrowBundleProductId;
                case "equipment.lamellar_armor":
                    return CoreProductionContent.LamellarArmorProductId;
                default:
                    return "product.equipment." +
                           equipmentId.Substring(equipmentId.IndexOf('.') + 1);
            }
        }

        private static void MigrateVersionFourteenToFifteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductionSites =
                new System.Collections.Generic.List<ProductionSiteState>();
            world.MilitaryEquipmentRepairOrders =
                new System.Collections.Generic.List<
                    MilitaryEquipmentRepairOrderState>();
            for (var i = 0;
                 i < world.MilitaryEquipmentDefinitions.Count;
                 i++)
            {
                ConfigureRepair(world.MilitaryEquipmentDefinitions[i]);
            }

            for (var i = 0; i < world.MilitaryArmoryStocks.Count; i++)
            {
                world.MilitaryArmoryStocks[i].ReservedDamagedQuantity = 0;
            }

            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 15;
        }

        private static void ConfigureRepair(
            MilitaryEquipmentDefinitionState definition)
        {
            switch (definition.Id)
            {
                case "equipment.han_ring_sword":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        1,
                        3,
                        CoreProductionContent.BlacksmithFacilityTag);
                    break;
                case "equipment.wooden_shield":
                    SetRepair(
                        definition,
                        CoreProductionContent.TimberMaterialProductId,
                        2,
                        2,
                        CoreProductionContent.WoodworkingFacilityTag);
                    break;
                case "equipment.long_spear":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        1,
                        2,
                        CoreProductionContent.BlacksmithFacilityTag);
                    break;
                case "equipment.horn_bow":
                    SetRepair(
                        definition,
                        CoreProductionContent.HornMaterialProductId,
                        1,
                        3,
                        CoreProductionContent.BowmakingFacilityTag);
                    break;
                case "equipment.arrow_bundle":
                    SetRepair(
                        definition,
                        CoreProductionContent.TimberMaterialProductId,
                        1,
                        1,
                        CoreProductionContent.WoodworkingFacilityTag);
                    break;
                case "equipment.lamellar_armor":
                    SetRepair(
                        definition,
                        CoreProductionContent.IronMaterialProductId,
                        2,
                        5,
                        CoreProductionContent.ArmoringFacilityTag);
                    break;
                default:
                    return;
            }
        }

        private static void MigrateVersionFifteenToSixteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ResourceBodies =
                new System.Collections.Generic.List<ResourceBodyState>();
            world.ResourceExtractionOrders =
                new System.Collections.Generic.List<
                    ResourceExtractionOrderState>();
            world.ResourceExtractionLedgerEntries =
                new System.Collections.Generic.List<
                    ResourceExtractionLedgerEntryState>();
            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 16;
        }

        private static void MigrateVersionSixteenToSeventeen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 17;
        }

        private static void MigrateVersionSeventeenToEighteen(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.ProductionPracticeLedgerEntries =
                new System.Collections.Generic.List<
                    ProductionPracticeLedgerEntryState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                var product = productionContent.GetProduct(
                    batch.ProductDefinitionId);
                batch.QualityDimensions = ProductQualityRules.CreateUniform(
                    product, batch.QualityBasisPoints);
            }

            for (var i = 0; i < world.ProcessingWorkOrders.Count; i++)
            {
                var order = world.ProcessingWorkOrders[i];
                var method = productionContent.GetMethod(
                    order.MethodDefinitionId);
                order.PracticeTrackingEnabled = false;
                order.PracticeSkillDefinitionId =
                    method.PracticeSkillDefinitionId;
                order.ManagerSkillBasisPointsAtStart = 0;
                order.PracticeGainBasisPoints = 0;
                order.OutputQualityBasisPoints = 0;
            }

            world.ProductionContentManifest =
                productionContent.CreateManifest();
            world.SchemaVersion = 18;
        }

        private static void MigrateVersionEighteenToNineteen(
            WorldState world)
        {
            world.MilitaryLogisticsOrders =
                new System.Collections.Generic.List<
                    MilitaryLogisticsOrderState>();
            world.MilitaryLogisticsLedgerEntries =
                new System.Collections.Generic.List<
                    MilitaryLogisticsLedgerEntryState>();
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i]
                    .SourceMilitaryLogisticsOrderId = string.Empty;
            }

            for (var i = 0; i < world.MilitarySupplies.Count; i++)
            {
                world.MilitarySupplies[i].SourceLogisticsOrderId =
                    string.Empty;
            }

            world.SchemaVersion = 19;
        }

        private static void MigrateVersionNineteenToTwenty(
            WorldState world)
        {
            world.MilitaryLogisticsLegs =
                new System.Collections.Generic.List<
                    MilitaryLogisticsLegState>();
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                var order = world.MilitaryLogisticsOrders[i];
                order.FinalDestinationLocationId =
                    order.DestinationLocationId;
                order.CurrentLegSequence = 0;
                order.PlannedLegCount = 0;
                order.AutoDeliverAtFinal = true;
            }

            world.SchemaVersion = 20;
        }

        private static void MigrateVersionTwentyToTwentyOne(
            WorldState world)
        {
            world.MilitaryLogisticsEscorts =
                new System.Collections.Generic.List<
                    MilitaryLogisticsEscortState>();
            world.MilitaryLogisticsIncidents =
                new System.Collections.Generic.List<
                    MilitaryLogisticsIncidentState>();
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                world.MilitaryLogisticsOrders[i].HostileLossQuantity = 0;
            }

            for (var i = 0; i < world.MilitaryLogisticsLegs.Count; i++)
            {
                var leg = world.MilitaryLogisticsLegs[i];
                leg.HostileLossQuantity = 0;
                leg.RiskPolicyId = MilitaryLogisticsRiskPolicyIds.None;
                leg.ThreatOrganizationId = string.Empty;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsLedgerEntries.Count;
                 i++)
            {
                world.MilitaryLogisticsLedgerEntries[i]
                    .CargoHostileLossDelta = 0;
            }

            world.SchemaVersion = 21;
        }

        private static void MigrateVersionTwentyOneToTwentyTwo(
            WorldState world)
        {
            world.MilitaryLogisticsClashes =
                new System.Collections.Generic.List<
                    MilitaryLogisticsClashState>();
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                world.MilitaryLogisticsOrders[i].RecoveredCargoQuantity = 0;
            }

            for (var i = 0; i < world.MilitaryLogisticsLegs.Count; i++)
            {
                world.MilitaryLogisticsLegs[i].RecoveredCargoQuantity = 0;
            }

            for (var i = 0; i < world.MilitaryLogisticsIncidents.Count; i++)
            {
                world.MilitaryLogisticsIncidents[i]
                    .RecoveredCargoQuantity = 0;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsLedgerEntries.Count;
                 i++)
            {
                world.MilitaryLogisticsLedgerEntries[i]
                    .CargoRecoveredDelta = 0;
            }

            world.SchemaVersion = 22;
        }

        private static void MigrateVersionTwentyTwoToTwentyThree(
            WorldState world)
        {
            world.MilitaryLogisticsDelegationGoals =
                new System.Collections.Generic.List<
                    MilitaryLogisticsDelegationGoalState>();
            world.MilitaryLogisticsDelegationOffers =
                new System.Collections.Generic.List<
                    MilitaryLogisticsDelegationOfferState>();
            world.MilitaryLogisticsDelegationReports =
                new System.Collections.Generic.List<
                    MilitaryLogisticsDelegationReportState>();
            world.SchemaVersion = 23;
        }

        private static void MigrateVersionTwentyThreeToTwentyFour(
            WorldState world)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = world.MilitaryLogisticsDelegationGoals[i];
                var scheduledDay = goal.LastEvaluatedDay >= 0
                    ? checked(goal.LastEvaluatedDay +
                        goal.ReportIntervalDays)
                    : checked(goal.CreatedDay + goal.ReportIntervalDays);
                goal.NextEvaluationDay = Math.Min(
                    goal.DeadlineDay, scheduledDay);
                goal.FulfilledDay = -1;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                var offer = world.MilitaryLogisticsDelegationOffers[i];
                var goal = FindDelegationGoal(world, offer.GoalId);
                offer.ValidUntilDay = goal.DeadlineDay;
                offer.ClosedDay = offer.Status ==
                    MilitaryLogisticsDelegationOfferStatus.Withdrawn
                        ? offer.SubmittedDay
                        : -1;
            }

            world.SchemaVersion = 24;
        }

        private static MilitaryLogisticsDelegationGoalState
            FindDelegationGoal(WorldState world, string goalId)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                if (world.MilitaryLogisticsDelegationGoals[i].Id == goalId)
                {
                    return world.MilitaryLogisticsDelegationGoals[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics delegation goal {goalId} " +
                "during world migration.");
        }

        private static void MigrateVersionTwentyFourToTwentyFive(
            WorldState world)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = world.MilitaryLogisticsDelegationGoals[i];
                goal.ParentGoalId = string.Empty;
                goal.DelegationDepth = 0;
                goal.AssigneePersonId = goal.IssuerPersonId;
                goal.DelegatedByPersonId = string.Empty;
                goal.AssigneeAuthorityAtDelegation =
                    MilitaryAuthorityLevel.Army;
                goal.ChildGoalIds = new System.Collections.Generic.List<string>();
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationReports.Count;
                 i++)
            {
                world.MilitaryLogisticsDelegationReports[i].RelatedGoalId =
                    string.Empty;
            }

            world.SchemaVersion = 25;
        }

        private static void MigrateVersionTwentyFiveToTwentySix(
            WorldState world)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = world.MilitaryLogisticsDelegationGoals[i];
                goal.UnassignedCargoQuantity = 0;
                goal.AvailableBudgetReserve = 0;
                goal.CancelledDay = -1;
                goal.CancelledByPersonId = string.Empty;
                goal.CancellationReasonId = string.Empty;
                goal.ReplacesGoalId = string.Empty;
                goal.ReplacementGoalIds =
                    new System.Collections.Generic.List<string>();
                if (goal.Status ==
                    MilitaryLogisticsDelegationStatus.Cancelled)
                {
                    goal.CancelledDay = goal.CreatedDay;
                    goal.CancelledByPersonId = goal.IssuerPersonId;
                    goal.CancellationReasonId =
                        MilitaryLogisticsCancellationReasonIds
                            .MigratedUnspecified;
                }
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var parent = world.MilitaryLogisticsDelegationGoals[i];
                if (parent.ChildGoalIds.Count == 0)
                {
                    continue;
                }

                long assignedQuantity = 0;
                long assignedBudget = 0;
                for (var childIndex = 0;
                     childIndex < parent.ChildGoalIds.Count;
                     childIndex++)
                {
                    var child = FindDelegationGoal(
                        world, parent.ChildGoalIds[childIndex]);
                    if (child.Status ==
                        MilitaryLogisticsDelegationStatus.Cancelled)
                    {
                        continue;
                    }
                    assignedQuantity = checked(
                        assignedQuantity + child.RequestedCargoQuantity);
                    assignedBudget = checked(
                        assignedBudget + child.BudgetLimit);
                }

                parent.UnassignedCargoQuantity = checked(
                    parent.RequestedCargoQuantity -
                    (int)assignedQuantity);
                parent.AvailableBudgetReserve = checked(
                    parent.BudgetLimit - assignedBudget);
                if (parent.UnassignedCargoQuantity > 0 &&
                    parent.Status ==
                        MilitaryLogisticsDelegationStatus.Delegated)
                {
                    parent.Status =
                        MilitaryLogisticsDelegationStatus.NeedsAttention;
                }
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                var offer = world.MilitaryLogisticsDelegationOffers[i];
                var goal = FindDelegationGoal(world, offer.GoalId);
                if (goal.Status ==
                        MilitaryLogisticsDelegationStatus.Cancelled &&
                    offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Active)
                {
                    offer.Status =
                        MilitaryLogisticsDelegationOfferStatus.GoalCancelled;
                    offer.ClosedDay = Math.Max(
                        offer.SubmittedDay, goal.CancelledDay);
                }
            }

            world.SchemaVersion = 26;
        }

        private static void MigrateVersionTwentySixToTwentySeven(
            WorldState world)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = world.MilitaryLogisticsDelegationGoals[i];
                goal.FulfillmentPolicyId =
                    MilitaryLogisticsDelegationFulfillmentPolicyIds
                        .FullReceiptRequired;
                goal.ReceivedCargoQuantity = 0;
                goal.OutstandingCargoQuantity =
                    goal.RequestedCargoQuantity;
                goal.CompletedLogisticsOrderIds =
                    new System.Collections.Generic.List<string>();

                if (goal.Status !=
                    MilitaryLogisticsDelegationStatus.Fulfilled)
                {
                    continue;
                }

                goal.FulfillmentPolicyId =
                    MilitaryLogisticsDelegationFulfillmentPolicyIds
                        .LegacyOrderCompletion;
                goal.OutstandingCargoQuantity = 0;
                if (goal.ChildGoalIds.Count != 0 ||
                    string.IsNullOrEmpty(goal.LogisticsOrderId))
                {
                    continue;
                }

                var order = FindLogisticsOrder(
                    world, goal.LogisticsOrderId);
                goal.ReceivedCargoQuantity =
                    order.DeliveredCargoQuantity;
                goal.CompletedLogisticsOrderIds.Add(order.Id);
                if (!string.IsNullOrEmpty(goal.SelectedOfferId))
                {
                    var offer = FindDelegationOffer(
                        world, goal.SelectedOfferId);
                    offer.Status =
                        MilitaryLogisticsDelegationOfferStatus.Completed;
                    offer.ClosedDay = Math.Max(
                        offer.SubmittedDay, order.DeliveredDay);
                    offer.LogisticsOrderId = order.Id;
                }
                goal.SelectedOfferId = string.Empty;
                goal.LogisticsOrderId = string.Empty;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                var offer = world.MilitaryLogisticsDelegationOffers[i];
                if (offer.Status ==
                    MilitaryLogisticsDelegationOfferStatus.Completed)
                {
                    continue;
                }

                offer.LogisticsOrderId = string.Empty;
                var goal = FindDelegationGoal(world, offer.GoalId);
                if (offer.Status ==
                        MilitaryLogisticsDelegationOfferStatus.Selected &&
                    goal.Status ==
                        MilitaryLogisticsDelegationStatus.Dispatched)
                {
                    offer.LogisticsOrderId = goal.LogisticsOrderId;
                }
            }

            for (var depth =
                     MilitaryLogisticsDelegationContract
                         .MaximumDelegationDepth;
                 depth >= 0;
                 depth--)
            {
                for (var i = 0;
                     i < world.MilitaryLogisticsDelegationGoals.Count;
                     i++)
                {
                    var parent =
                        world.MilitaryLogisticsDelegationGoals[i];
                    if (parent.DelegationDepth != depth ||
                        parent.ChildGoalIds.Count == 0 ||
                        parent.Status !=
                            MilitaryLogisticsDelegationStatus.Fulfilled)
                    {
                        continue;
                    }

                    var received = 0;
                    for (var childIndex = 0;
                         childIndex < parent.ChildGoalIds.Count;
                         childIndex++)
                    {
                        var child = FindDelegationGoal(
                            world, parent.ChildGoalIds[childIndex]);
                        if (child.Status !=
                            MilitaryLogisticsDelegationStatus.Cancelled)
                        {
                            received = checked(
                                received + child.ReceivedCargoQuantity);
                        }
                    }
                    parent.ReceivedCargoQuantity = received;
                }
            }

            world.SchemaVersion = 27;
        }

        private static void MigrateVersionTwentySevenToTwentyEight(
            WorldState world)
        {
            world.MilitaryLogisticsLiabilitySettlements =
                new System.Collections.Generic.List<
                    MilitaryLogisticsLiabilitySettlementState>();
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationGoals.Count;
                 i++)
            {
                var goal = world.MilitaryLogisticsDelegationGoals[i];
                goal.ReplacementProcurementPolicyId =
                    MilitaryLogisticsReplacementProcurementPolicyIds
                        .LegacyUnrestricted;
                goal.AuthorizedReplacementQuantity = 0;
                goal.ConsumedReplacementAuthorizationQuantity = 0;
                goal.LastReplacementAuthorizedDay = -1;
                goal.LastReplacementAuthorizedByPersonId = string.Empty;
                goal.LastReplacementAuthorizationReasonId = string.Empty;
                goal.CompensationReceived = 0;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                world.MilitaryLogisticsDelegationOffers[i]
                    .LiabilityPolicyId =
                    MilitaryLogisticsLiabilityPolicyIds
                        .LegacyNoRetroactiveSettlement;
            }

            for (var i = 0;
                 i < world.MilitaryLogisticsOrders.Count;
                 i++)
            {
                world.MilitaryLogisticsOrders[i].LiabilityPolicyId =
                    MilitaryLogisticsLiabilityPolicyIds
                        .LegacyNoRetroactiveSettlement;
            }

            world.SchemaVersion = 28;
        }

        private static void MigrateVersionTwentyEightToTwentyNine(
            WorldState world)
        {
            world.FoodInventoryAuthorityMode =
                FoodInventoryAuthorityMode.LegacyScalar;
            for (var i = 0; i < world.Villages.Count; i++)
            {
                world.Villages[i].PublicGranaryInventoryContainerId =
                    string.Empty;
            }

            for (var i = 0; i < world.CountyGovernances.Count; i++)
            {
                world.CountyGovernances[i].GranaryInventoryContainerId =
                    string.Empty;
            }

            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                var transaction = world.InventoryTransactions[i];
                transaction.SourceVillageId ??= string.Empty;
                transaction.SourceCountyGovernanceId ??= string.Empty;
            }

            world.SchemaVersion = 29;
        }

        private static void MigrateVersionTwentyNineToThirty(
            WorldState world)
        {
            world.FormalMarketOrders = new System.Collections.Generic.List<
                FormalMarketOrderState>();
            world.FormalMarketTrades = new System.Collections.Generic.List<
                FormalMarketTradeState>();
            world.FormalMarketPrices = new System.Collections.Generic.List<
                FormalMarketPriceState>();
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i].SourceFormalMarketOrderId ??=
                    string.Empty;
            }
            world.SchemaVersion = 30;
        }

        private static void MigrateVersionThirtyToThirtyOne(
            WorldState world)
        {
            world.CivilianFreights = new System.Collections.Generic.List<
                CivilianFreightState>();
            world.CivilianFreightLedgerEntries =
                new System.Collections.Generic.List<
                    CivilianFreightLedgerEntryState>();
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i].SourceCivilianFreightId ??=
                    string.Empty;
            }
            for (var i = 0; i < world.FormalMarketTrades.Count; i++)
            {
                var trade = world.FormalMarketTrades[i];
                trade.DestinationCountyGovernanceId =
                    trade.CountyGovernanceId;
                trade.SellerProceeds = trade.MoneyTransferred;
                trade.CivilianFreightId = string.Empty;
            }
            world.SchemaVersion = 31;
        }

        private static void MigrateVersionThirtyOneToThirtyTwo(
            WorldState world)
        {
            world.CivilianFreightDemands =
                new System.Collections.Generic.List<
                    CivilianFreightDemandState>();
            world.CivilianCarrierRegistrations =
                new System.Collections.Generic.List<
                    CivilianCarrierRegistrationState>();
            world.CivilianCarrierOffers =
                new System.Collections.Generic.List<
                    CivilianCarrierOfferState>();
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                freight.DemandId ??= string.Empty;
                freight.CarrierOfferId ??= string.Empty;
                freight.PlannedRouteIds =
                    new System.Collections.Generic.List<string>
                    {
                        freight.RouteId
                    };
                freight.CurrentRouteIndex = 0;
            }
            world.SchemaVersion = 32;
        }

        private static void MigrateVersionThirtyTwoToThirtyThree(
            WorldState world)
        {
            world.PersistentWorldCommands =
                new List<PersistentWorldCommandState>();
            world.WorldCommandBatchResults =
                new List<WorldCommandBatchResultState>();
            world.WorldEventOutbox = new List<WorldEventOutboxState>();
            world.SchemaVersion = 33;
        }

        private static void MigrateVersionThirtyThreeToThirtyFour(
            WorldState world)
        {
            world.PublicReliefProcurementTrades =
                new List<PublicReliefProcurementTradeState>();
            world.SchemaVersion = 34;
        }

        private static void MigrateVersionThirtyFourToThirtyFive(
            WorldState world)
        {
            world.PublicReliefProcurementTrades ??=
                new List<PublicReliefProcurementTradeState>();
            for (var i = 0;
                 i < world.PublicReliefProcurementTrades.Count;
                 i++)
            {
                var trade = world.PublicReliefProcurementTrades[i];
                trade.SourceCountyGovernanceId ??=
                    trade.CountyGovernanceId;
                trade.CivilianFreightId ??= string.Empty;
                trade.FreightFee = Math.Max(0, trade.FreightFee);
            }
            world.CivilianFreights ??= new List<CivilianFreightState>();
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                var freight = world.CivilianFreights[i];
                freight.BuyerOrganizationId ??= string.Empty;
                freight.DestinationInventoryContainerId ??= string.Empty;
                freight.PublicReliefProcurementTradeId ??= string.Empty;
                freight.SourcePublicReliefEventId ??= string.Empty;
                freight.SourcePublicReliefCommandId ??= string.Empty;
            }
            world.SchemaVersion = 35;
        }

        private static void MigrateVersionThirtyFiveToThirtySix(
            WorldState world)
        {
            world.PublicReliefRecoveries =
                new List<PublicReliefRecoveryState>();
            world.CivilianFreights ??= new List<CivilianFreightState>();
            for (var i = 0; i < world.CivilianFreights.Count; i++)
            {
                world.CivilianFreights[i].PublicReliefRecoveryId ??=
                    string.Empty;
            }
            world.PublicReliefProcurementTrades ??=
                new List<PublicReliefProcurementTradeState>();
            for (var i = 0;
                 i < world.PublicReliefProcurementTrades.Count;
                 i++)
            {
                world.PublicReliefProcurementTrades[i]
                    .PublicReliefRecoveryId ??= string.Empty;
            }
            world.SchemaVersion = 36;
        }

        private static MilitaryLogisticsOrderState FindLogisticsOrder(
            WorldState world,
            string orderId)
        {
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                if (world.MilitaryLogisticsOrders[i].Id == orderId)
                {
                    return world.MilitaryLogisticsOrders[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics order {orderId} during world migration.");
        }

        private static MilitaryLogisticsDelegationOfferState
            FindDelegationOffer(WorldState world, string offerId)
        {
            for (var i = 0;
                 i < world.MilitaryLogisticsDelegationOffers.Count;
                 i++)
            {
                if (world.MilitaryLogisticsDelegationOffers[i].Id == offerId)
                {
                    return world.MilitaryLogisticsDelegationOffers[i];
                }
            }

            throw new InvalidOperationException(
                $"Missing military logistics delegation offer {offerId} " +
                "during world migration.");
        }

        private static void SetRepair(
            MilitaryEquipmentDefinitionState definition,
            string productId,
            int quantity,
            int durationDays,
            string facilityTag)
        {
            definition.RepairMaterialProductDefinitionId = productId;
            definition.RepairMaterialQuantityPerUnit = quantity;
            definition.RepairDurationDays = durationDays;
            definition.RepairFacilityTag = facilityTag;
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

            throw new InvalidOperationException(
                $"Missing person {personId} during world migration.");
        }
    }
}
