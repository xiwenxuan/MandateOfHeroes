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
                    case 36:
                        MigrateVersionThirtySixToThirtySeven(world, content);
                        break;
                    case 37:
                        MigrateVersionThirtySevenToThirtyEight(world);
                        break;
                    case 38:
                        MigrateVersionThirtyEightToThirtyNine(world);
                        break;
                    case 39:
                        MigrateVersionThirtyNineToForty(world);
                        break;
                    case 40:
                        MigrateVersionFortyToFortyOne(world);
                        break;
                    case 41:
                        MigrateVersionFortyOneToFortyTwo(world);
                        break;
                    case 42:
                        MigrateVersionFortyTwoToFortyThree(world);
                        break;
                    case 43:
                        MigrateVersionFortyThreeToFortyFour(world, content);
                        break;
                    case 44:
                        MigrateVersionFortyFourToFortyFive(world, content);
                        break;
                    case 45:
                        MigrateVersionFortyFiveToFortySix(world);
                        break;
                    case 46:
                        MigrateVersionFortySixToFortySeven(world);
                        break;
                    case 47:
                        MigrateVersionFortySevenToFortyEight(world);
                        break;
                    case 48:
                        MigrateVersionFortyEightToFortyNine(world);
                        break;
                    case 49:
                        MigrateVersionFortyNineToFifty(world);
                        break;
                    case 50:
                        MigrateVersionFiftyToFiftyOne(world);
                        break;
                    case 51:
                        MigrateVersionFiftyOneToFiftyTwo(world);
                        break;
                    case 52:
                        MigrateVersionFiftyTwoToFiftyThree(world);
                        break;
                    case 53:
                        MigrateVersionFiftyThreeToFiftyFour(world);
                        break;
                    case 54:
                        MigrateVersionFiftyFourToFiftyFive(world);
                        break;
                    case 55:
                        MigrateVersionFiftyFiveToFiftySix(world);
                        break;
                    case 56:
                        MigrateVersionFiftySixToFiftySeven(world);
                        break;
                    case 57:
                        MigrateVersionFiftySevenToFiftyEight(world);
                        break;
                    case 58:
                        MigrateVersionFiftyEightToFiftyNine(world);
                        break;
                    case 59:
                        MigrateVersionFiftyNineToSixty(world);
                        break;
                    case 60:
                        MigrateVersionSixtyToSixtyOne(world);
                        break;
                    case 61:
                        MigrateVersionSixtyOneToSixtyTwo(world);
                        break;
                    case 62:
                        MigrateVersionSixtyTwoToSixtyThree(world);
                        break;
                    case 63:
                        MigrateVersionSixtyThreeToSixtyFour(world);
                        break;
                    case 64:
                        MigrateVersionSixtyFourToSixtyFive(world, content);
                        break;
                    case 65:
                        MigrateVersionSixtyFiveToSixtySix(world);
                        break;
                    case 66:
                        MigrateVersionSixtySixToSixtySeven(world);
                        break;
                    case 67:
                        MigrateVersionSixtySevenToSixtyEight(world);
                        break;
                    case 68:
                        MigrateVersionSixtyEightToSixtyNine(world);
                        break;
                    case 69:
                        MigrateVersionSixtyNineToSeventy(world);
                        break;
                    case 70:
                        MigrateVersionSeventyToSeventyOne(world);
                        break;
                    case 71:
                        MigrateVersionSeventyOneToSeventyTwo(world);
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

        private static void MigrateVersionThirtySixToThirtySeven(
            WorldState world,
            ProductionContentRegistry content)
        {
            world.FoodStorageLosses = new List<FoodStorageLossState>();
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                facility.FoodStorageEnvironmentId =
                    "storage.environment.household_granary";
                facility.FoodStorageProtectionBasisPoints = 2_500;
            }
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                var container = world.InventoryContainers[i];
                if (container.KindId == "inventory.village_public_granary")
                {
                    container.FoodStorageEnvironmentId =
                        "storage.environment.village_public_granary";
                    container.FoodStorageProtectionBasisPoints = 3_500;
                }
                else if (container.KindId == "inventory.county_granary")
                {
                    container.FoodStorageEnvironmentId =
                        "storage.environment.county_granary";
                    container.FoodStorageProtectionBasisPoints = 4_500;
                }
                else
                {
                    container.FoodStorageEnvironmentId =
                        "storage.environment.generic_sheltered";
                    container.FoodStorageProtectionBasisPoints = 2_000;
                }
            }
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (content.TryGetFood(
                        world.ProductBatches[i].ProductDefinitionId,
                        out _))
                {
                    world.ProductBatches[i].NextFoodStorageAssessmentDay =
                        checked(world.AbsoluteDay + 30);
                }
            }
            world.SchemaVersion = 37;
        }

        private static void MigrateVersionThirtySevenToThirtyEight(
            WorldState world)
        {
            world.HouseholdReliefPickups =
                new List<HouseholdReliefPickupState>();
            world.SchemaVersion = 38;
        }

        private static void MigrateVersionThirtyEightToThirtyNine(
            WorldState world)
        {
            world.HouseholdReliefConsumptions =
                new List<HouseholdReliefConsumptionState>();
            world.SchemaVersion = 39;
        }

        private static void MigrateVersionThirtyNineToForty(
            WorldState world)
        {
            for (var i = 0;
                 i < world.HouseholdReliefConsumptions.Count;
                 i++)
            {
                var consumption = world.HouseholdReliefConsumptions[i];
                consumption.AllocationPolicyId =
                    HouseholdReliefAllocationPolicyIds
                        .LegacyHouseholdShared;
                consumption.PreparedNutritionBasisUnits = -1;
                for (var affectedIndex = 0;
                     affectedIndex < consumption.AffectedPeople.Count;
                     affectedIndex++)
                {
                    var affected = consumption.AffectedPeople[affectedIndex];
                    affected.RequiredNutritionBasisUnits = -1;
                    affected.AllocatedNutritionBasisUnits = -1;
                    affected.ConsumedNutritionBasisUnits = -1;
                }
            }
            world.SchemaVersion = 40;
        }

        private static void MigrateVersionFortyToFortyOne(
            WorldState world)
        {
            for (var villageIndex = 0;
                 villageIndex < world.Villages.Count;
                 villageIndex++)
            {
                var village = world.Villages[villageIndex];
                village.HouseholdReliefPriorityPolicyId =
                    HouseholdReliefPriorityPolicyIds.NeedSeverityVulnerability;
                var governance = world.CountyGovernances.Find(item =>
                    item.CountyLocationId == village.ParentLocationId);
                if (governance == null)
                {
                    village.HouseholdReliefAuthorizationPolicyId =
                        HouseholdReliefAuthorizationPolicyIds.EmergencySystem;
                    village.HouseholdReliefAuthorityOrganizationId = string.Empty;
                }
                else
                {
                    village.HouseholdReliefAuthorizationPolicyId =
                        HouseholdReliefAuthorizationPolicyIds
                            .CountyGovernmentLeader;
                    village.HouseholdReliefAuthorityOrganizationId =
                        governance.GovernmentOrganizationId;
                }
            }

            for (var pickupIndex = 0;
                 pickupIndex < world.HouseholdReliefPickups.Count;
                 pickupIndex++)
            {
                var pickup = world.HouseholdReliefPickups[pickupIndex];
                pickup.PriorityPolicyId = HouseholdReliefPriorityPolicyIds
                    .LegacySettlementFamilyOrder;
                pickup.AuthorizationPolicyId =
                    HouseholdReliefAuthorizationPolicyIds.LegacySystem;
                pickup.AuthorizingOrganizationId = string.Empty;
                pickup.AuthorizingPersonId = string.Empty;
                pickup.AuthorizedDay = -1;
                pickup.ShortfallSeverityBasisPoints = -1;
                pickup.VulnerableAffectedPersonCount = -1;
                pickup.AffectedPersonCountAtAuthorization = -1;
            }
            world.SchemaVersion = 41;
        }

        private static void MigrateVersionFortyOneToFortyTwo(
            WorldState world)
        {
            world.HouseholdReliefCareDeliveries =
                new List<HouseholdReliefCareDeliveryState>();
            for (var claimIndex = 0;
                 claimIndex < world.HouseholdReliefConsumptions.Count;
                 claimIndex++)
            {
                var claim = world.HouseholdReliefConsumptions[claimIndex];
                claim.CareDeliveryPolicyId =
                    HouseholdReliefCareDeliveryPolicyIds.LegacySelfService;
                for (var affectedIndex = 0;
                     affectedIndex < claim.AffectedPeople.Count;
                     affectedIndex++)
                {
                    claim.AffectedPeople[affectedIndex]
                        .RequiresCaregiverDelivery = false;
                }
            }
            for (var transactionIndex = 0;
                 transactionIndex < world.InventoryTransactions.Count;
                 transactionIndex++)
            {
                world.InventoryTransactions[transactionIndex]
                    .HouseholdReliefRecipientPersonId = string.Empty;
            }
            world.SchemaVersion = 42;
        }

        private static void MigrateVersionFortyTwoToFortyThree(
            WorldState world)
        {
            world.PersonNutritionProfiles =
                new List<PersonNutritionProfileState>();
            world.PersonNutritionLedgerEntries =
                new List<PersonNutritionLedgerEntryState>();
            world.NutritionConditionEpisodes =
                new List<NutritionConditionEpisodeState>();
            world.SchemaVersion = 43;
        }

        private static void MigrateVersionFortyThreeToFortyFour(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            world.CivilianMedicalCases =
                new List<CivilianMedicalCaseState>();
            world.CivilianMedicalTreatments =
                new List<CivilianMedicalTreatmentState>();
            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 44;
        }

        private static void MigrateVersionFortyFourToFortyFive(
            WorldState world,
            ProductionContentRegistry productionContent)
        {
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                var facility = world.VillageFacilities[i];
                facility.CapabilityTags ??= new List<string>();
                var canonical = VillageFacilityTags.FromKind(facility.Kind);
                if (!facility.CapabilityTags.Contains(canonical))
                {
                    facility.CapabilityTags.Add(canonical);
                }
                facility.CapabilityTags.Sort(StringComparer.Ordinal);
            }

            for (var i = 0; i < world.ResourceExtractionOrders.Count; i++)
            {
                var order = world.ResourceExtractionOrders[i];
                order.OwnerFamilyId ??= string.Empty;
                order.StorageFacilityId ??= string.Empty;
                order.OwnerOrganizationId ??= string.Empty;
                order.ProductionSiteId ??= string.Empty;
                order.InventoryContainerId ??= string.Empty;
            }

            world.ProductionContentManifest = productionContent.CreateManifest();
            world.SchemaVersion = 45;
        }

        private static void MigrateVersionFortyFiveToFortySix(
            WorldState world)
        {
            world.CivilianMedicalPrescriptions =
                new List<CivilianMedicalPrescriptionState>();
            world.CivilianMedicalServices =
                new List<CivilianMedicalServiceState>();
            world.CivilianMedicalServiceContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0; i < world.CivilianMedicalCases.Count; i++)
            {
                var medicalCase = world.CivilianMedicalCases[i];
                medicalCase.PrescriptionId = string.Empty;
                medicalCase.Status = CivilianMedicalCaseStatus.Active;
                medicalCase.ClosedDay = -1;
                medicalCase.ClosureReasonId = string.Empty;
            }
            for (var i = 0; i < world.CivilianMedicalTreatments.Count; i++)
            {
                var treatment = world.CivilianMedicalTreatments[i];
                treatment.PrescriptionId = string.Empty;
                treatment.MedicalServiceId = string.Empty;
            }
            world.SchemaVersion = 46;
        }

        private static void MigrateVersionFortySixToFortySeven(
            WorldState world)
        {
            world.MilitaryMedicalCases =
                new List<MilitaryMedicalCaseState>();
            world.MilitaryMedicalServices =
                new List<MilitaryMedicalServiceState>();
            world.MilitaryMedicalInitialized = false;
            world.MilitaryMedicalContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0; i < world.Armies.Count; i++)
            {
                world.Armies[i].MedicalInventoryContainerId = string.Empty;
            }
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i].SourceMilitaryMedicalServiceId =
                    string.Empty;
            }
            world.SchemaVersion = 47;
        }

        private static void MigrateVersionFortySevenToFortyEight(
            WorldState world)
        {
            for (var i = 0; i < world.MilitaryLogisticsOrders.Count; i++)
            {
                world.MilitaryLogisticsOrders[i].DeliveryPolicyId =
                    MilitaryLogisticsDeliveryPolicyIds.ArmyProvisions;
                world.MilitaryLogisticsOrders[i]
                    .TargetInventoryContainerId = string.Empty;
            }

            world.SchemaVersion = 48;
        }

        private static void MigrateVersionFortyEightToFortyNine(
            WorldState world)
        {
            world.MilitaryMedicalEvacuations =
                new List<MilitaryMedicalEvacuationState>();
            world.SchemaVersion = 49;
        }

        private static void MigrateVersionFortyNineToFifty(
            WorldState world)
        {
            world.MilitaryRearMedicalSites =
                new List<MilitaryRearMedicalSiteState>();
            world.MilitaryRearMedicalAdmissions =
                new List<MilitaryRearMedicalAdmissionState>();
            world.MilitaryRearMedicalTreatments =
                new List<MilitaryRearMedicalTreatmentState>();

            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                evacuation.RearMedicalSiteId = string.Empty;
                evacuation.RearMedicalAdmissionId = string.Empty;
                evacuation.ReturnRouteId = string.Empty;
                evacuation.ReturnDestinationLocationId = string.Empty;
                evacuation.PatientReturnJourneyId = string.Empty;
                evacuation.ReturnStartedDay = -1;
                evacuation.CompletedDay = -1;
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    evacuation.TeamMembers[memberIndex].ReturnJourneyId =
                        string.Empty;
                }
            }
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i]
                    .SourceMilitaryRearMedicalTreatmentId = string.Empty;
            }
            world.SchemaVersion = 50;
        }

        private static void MigrateVersionFiftyToFiftyOne(
            WorldState world)
        {
            world.MilitaryFieldHospitalConstructionProjects =
                new List<MilitaryFieldHospitalConstructionProjectState>();
            world.MilitaryFieldHospitalConstructionWork =
                new List<MilitaryFieldHospitalConstructionWorkState>();
            world.MilitaryFieldHospitalMaintenance =
                new List<MilitaryFieldHospitalMaintenanceState>();
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                var site = world.MilitaryRearMedicalSites[i];
                site.SourceConstructionProjectId = string.Empty;
                site.SupportInventoryContainerId = string.Empty;
                site.MaintenancePolicyId = string.Empty;
                site.LastMaintenanceDay = -1;
                site.NextMaintenanceDay = -1;
            }
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                var admission = world.MilitaryRearMedicalAdmissions[i];
                admission.RequiredTreatmentStages = 1;
                admission.CompletedTreatmentStages =
                    string.IsNullOrEmpty(admission.TreatmentId) ? 0 : 1;
                admission.TreatmentIds = new List<string>();
                if (!string.IsNullOrEmpty(admission.TreatmentId))
                {
                    admission.TreatmentIds.Add(admission.TreatmentId);
                }
            }
            for (var i = 0;
                 i < world.MilitaryRearMedicalTreatments.Count;
                 i++)
            {
                world.MilitaryRearMedicalTreatments[i].StageIndex = 0;
                world.MilitaryRearMedicalTreatments[i].RequiredStageCount = 1;
            }
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i]
                    .SourceMilitaryFieldHospitalConstructionProjectId =
                        string.Empty;
                world.InventoryTransactions[i]
                    .SourceMilitaryFieldHospitalMaintenanceId = string.Empty;
            }
            world.SchemaVersion = 51;
        }

        private static void MigrateVersionFiftyOneToFiftyTwo(
            WorldState world)
        {
            world.MilitaryInjuryEpisodes =
                new List<MilitaryInjuryEpisodeState>();
            world.MilitaryInjuryProfiles =
                MilitaryInjuryProfileCatalog.CreateCore();
            world.MilitaryInjuryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                var admission = world.MilitaryRearMedicalAdmissions[i];
                admission.InjuryEpisodeId = string.Empty;
                admission.TreatmentPlanProtocolIds = new List<string>();
                MilitaryRearMedicalSiteState site = null;
                for (var siteIndex = 0;
                     siteIndex < world.MilitaryRearMedicalSites.Count;
                     siteIndex++)
                {
                    if (world.MilitaryRearMedicalSites[siteIndex].Id ==
                        admission.RearMedicalSiteId)
                    {
                        site = world.MilitaryRearMedicalSites[siteIndex];
                        break;
                    }
                }
                if (site == null)
                {
                    throw new InvalidOperationException(
                        $"Missing rear medical site {admission.RearMedicalSiteId} " +
                        "during V51-to-V52 migration.");
                }
                if (site.KindId ==
                    MilitaryRearMedicalSiteKindIds.FieldHospital)
                {
                    admission.TreatmentPlanProtocolIds.Add(
                        MilitaryRearMedicalTreatmentProtocolIds
                            .FieldStabilization);
                    admission.TreatmentPlanProtocolIds.Add(
                        MilitaryRearMedicalTreatmentProtocolIds.FieldRecovery);
                }
                else
                {
                    admission.TreatmentPlanProtocolIds.Add(
                        MilitaryRearMedicalTreatmentProtocolIds
                            .InpatientHerbalRecovery);
                }
                if (admission.TreatmentPlanProtocolIds.Count !=
                    admission.RequiredTreatmentStages)
                {
                    throw new InvalidOperationException(
                        $"Rear medical admission {admission.Id} has an invalid " +
                        "V51 treatment-stage count.");
                }
            }
            world.SchemaVersion = 52;
        }

        private static void MigrateVersionFiftyTwoToFiftyThree(
            WorldState world)
        {
            world.MilitarySurgicalProcedures =
                MilitarySurgicalProcedureCatalog.CreateCore();
            world.MilitarySurgeryContractActivationDay = checked(
                world.AbsoluteDay + 1);
            var coreProfiles = MilitaryInjuryProfileCatalog.CreateCore();
            for (var i = 0; i < world.MilitaryInjuryProfiles.Count; i++)
            {
                var profile = world.MilitaryInjuryProfiles[i];
                profile.SurgicalProcedureId = string.Empty;
                for (var coreIndex = 0;
                     coreIndex < coreProfiles.Count;
                     coreIndex++)
                {
                    if (coreProfiles[coreIndex].Id == profile.Id)
                    {
                        profile.SurgicalProcedureId =
                            coreProfiles[coreIndex].SurgicalProcedureId;
                        break;
                    }
                }
            }
            for (var i = 0; i < world.People.Count; i++)
            {
                world.People[i].PermanentLaborCapacityPenaltyBasisPoints = 0;
            }
            for (var i = 0;
                 i < world.MilitaryMedicalEvacuations.Count;
                 i++)
            {
                world.MilitaryMedicalEvacuations[i].PatientReturnPolicyId =
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .ReturnWithTeam;
            }
            for (var i = 0; i < world.MilitaryInjuryEpisodes.Count; i++)
            {
                var injury = world.MilitaryInjuryEpisodes[i];
                injury.SurgicalProcedureId = string.Empty;
                injury.SurgeryTreatmentId = string.Empty;
                injury.SurgeryCompletedDay = -1;
                injury.PermanentOutcomeId = string.Empty;
                injury.LaborCapacityBeforeBasisPoints = -1;
                injury.LaborCapacityAfterBasisPoints = -1;
                injury.PermanentLaborCapacityPenaltyBasisPoints = 0;
                injury.RequiresMedicalRetirement = false;
            }
            world.SchemaVersion = 53;
        }

        private static void MigrateVersionFiftyThreeToFiftyFour(
            WorldState world)
        {
            world.MilitaryMedicalTransfers =
                new List<MilitaryMedicalTransferState>();
            world.MilitaryMedicalTransferContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryMedicalEvacuations.Count;
                 i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                evacuation.CurrentCareLocationId =
                    evacuation.DestinationLocationId;
            }
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                var admission = world.MilitaryRearMedicalAdmissions[i];
                admission.MedicalTransferId = string.Empty;
                admission.TreatmentPlanOriginSiteKindId = string.Empty;
                for (var siteIndex = 0;
                     siteIndex < world.MilitaryRearMedicalSites.Count;
                     siteIndex++)
                {
                    if (world.MilitaryRearMedicalSites[siteIndex].Id ==
                        admission.RearMedicalSiteId)
                    {
                        admission.TreatmentPlanOriginSiteKindId =
                            world.MilitaryRearMedicalSites[siteIndex].KindId;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(
                    admission.TreatmentPlanOriginSiteKindId))
                {
                    throw new InvalidOperationException(
                        $"Missing rear medical site {admission.RearMedicalSiteId} " +
                        "during V53-to-V54 migration.");
                }
            }
            for (var i = 0; i < world.InventoryTransactions.Count; i++)
            {
                world.InventoryTransactions[i].SourceMilitaryMedicalTransferId =
                    string.Empty;
            }
            world.SchemaVersion = 54;
        }

        private static void MigrateVersionFiftyFourToFiftyFive(
            WorldState world)
        {
            world.MilitaryWoundDeathPolicies =
                MilitaryWoundDeathPolicyCatalog.CreateCore();
            world.MilitaryWoundDeaths = new List<MilitaryWoundDeathState>();
            world.MilitaryFamilyInheritances =
                new List<MilitaryFamilyInheritanceState>();
            world.MilitarySurvivorCompensations =
                new List<MilitarySurvivorCompensationState>();
            world.MilitaryWoundDeathContractActivationDay = checked(
                world.AbsoluteDay + 1);
            world.SchemaVersion = 55;
        }

        private static void MigrateVersionFiftyFiveToFiftySix(
            WorldState world)
        {
            world.MilitaryMedicalDeathResponsibilities =
                new List<MilitaryMedicalDeathResponsibilityState>();
            world.MilitaryMedicalDeathResponsibilityContractActivationDay =
                checked(world.AbsoluteDay + 1);
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                var death = world.MilitaryWoundDeaths[i];
                death.DeathContextId = MilitaryWoundDeathContextIds
                    .PostReturnMedicalRetirement;
                death.MedicalResponsibilityId = string.Empty;
            }
            world.SchemaVersion = 56;
        }

        private static void MigrateVersionFiftySixToFiftySeven(
            WorldState world)
        {
            world.MilitaryInpatientDeteriorationPolicies =
                MilitaryInpatientDeteriorationPolicyCatalog.CreateCore();
            world.MilitaryInpatientDeathClosures =
                new List<MilitaryInpatientDeathClosureState>();
            world.MilitaryInpatientDeathContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                world.MilitaryRearMedicalAdmissions[i]
                    .InpatientDeathClosureId = string.Empty;
            }
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = world.MilitaryMedicalTransfers[i];
                transfer.ReleasedReservedMedicineUnits = 0;
                transfer.ReservationReleaseInventoryTransactionId =
                    string.Empty;
            }
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                world.MilitaryWoundDeaths[i].InpatientDeathClosureId =
                    string.Empty;
            }
            world.SchemaVersion = 57;
        }

        private static void MigrateVersionFiftySevenToFiftyEight(
            WorldState world)
        {
            world.MilitaryMedicalTransferDeathClosures =
                new List<MilitaryMedicalTransferDeathClosureState>();
            world.MilitaryMedicalTransferDeathContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                world.MilitaryRearMedicalAdmissions[i]
                    .MedicalTransferDeathClosureId = string.Empty;
            }
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                world.MilitaryMedicalTransfers[i].DeathClosureId =
                    string.Empty;
            }
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                world.MilitaryWoundDeaths[i]
                    .MedicalTransferDeathClosureId = string.Empty;
            }
            world.SchemaVersion = 58;
        }

        private static void MigrateVersionFiftyEightToFiftyNine(
            WorldState world)
        {
            var corePolicies = MilitaryWoundDeathPolicyCatalog.CreateCore();
            for (var coreIndex = 0;
                 coreIndex < corePolicies.Count;
                 coreIndex++)
            {
                var core = corePolicies[coreIndex];
                var exists = false;
                for (var existingIndex = 0;
                     existingIndex < world.MilitaryWoundDeathPolicies.Count;
                     existingIndex++)
                {
                    if (world.MilitaryWoundDeathPolicies[existingIndex].Id ==
                        core.Id)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    world.MilitaryWoundDeathPolicies.Add(core);
                }
            }
            world.MilitaryOriginalEvacuationDeteriorationPolicies =
                MilitaryOriginalEvacuationDeteriorationPolicyCatalog
                    .CreateCore();
            world.MilitaryOriginalEvacuationDeathClosures =
                new List<MilitaryOriginalEvacuationDeathClosureState>();
            world.MilitaryOriginalEvacuationDeathContractActivationDay =
                checked(world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryMedicalEvacuations.Count;
                 i++)
            {
                world.MilitaryMedicalEvacuations[i]
                    .OriginalEvacuationDeathClosureId = string.Empty;
            }
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                world.MilitaryWoundDeaths[i]
                    .OriginalEvacuationDeathClosureId = string.Empty;
            }
            for (var i = 0;
                 i < world.MilitaryMedicalDeathResponsibilities.Count;
                 i++)
            {
                world.MilitaryMedicalDeathResponsibilities[i].SourceArmyId =
                    string.Empty;
            }
            world.SchemaVersion = 59;
        }

        private static void MigrateVersionFiftyNineToSixty(
            WorldState world)
        {
            var corePolicies = MilitaryWoundDeathPolicyCatalog.CreateCore();
            for (var coreIndex = 0;
                 coreIndex < corePolicies.Count;
                 coreIndex++)
            {
                var core = corePolicies[coreIndex];
                var exists = false;
                for (var existingIndex = 0;
                     existingIndex < world.MilitaryWoundDeathPolicies.Count;
                     existingIndex++)
                {
                    if (world.MilitaryWoundDeathPolicies[existingIndex].Id ==
                        core.Id)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    world.MilitaryWoundDeathPolicies.Add(core);
                }
            }
            world.MilitaryPatientReturnDeteriorationPolicies =
                MilitaryPatientReturnDeteriorationPolicyCatalog.CreateCore();
            world.MilitaryPatientReturnDeathClosures =
                new List<MilitaryPatientReturnDeathClosureState>();
            world.MilitaryPatientReturnDeathContractActivationDay = checked(
                world.AbsoluteDay + 1);
            for (var i = 0;
                 i < world.MilitaryMedicalEvacuations.Count;
                 i++)
            {
                world.MilitaryMedicalEvacuations[i]
                    .PatientReturnDeathClosureId = string.Empty;
            }
            for (var i = 0;
                 i < world.MilitaryRearMedicalAdmissions.Count;
                 i++)
            {
                world.MilitaryRearMedicalAdmissions[i]
                    .PatientReturnDeathClosureId = string.Empty;
            }
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                world.MilitaryWoundDeaths[i]
                    .PatientReturnDeathClosureId = string.Empty;
            }
            world.SchemaVersion = 60;
        }

        private static void MigrateVersionSixtyToSixtyOne(
            WorldState world)
        {
            var woundDeathPolicies = MilitaryWoundDeathPolicyCatalog.CreateCore();
            for (var coreIndex = 0;
                 coreIndex < woundDeathPolicies.Count;
                 coreIndex++)
            {
                var core = woundDeathPolicies[coreIndex];
                if (!world.MilitaryWoundDeathPolicies.Exists(
                    item => item.Id == core.Id))
                {
                    world.MilitaryWoundDeathPolicies.Add(core);
                }
            }

            var deteriorationPolicies =
                MilitaryPatientReturnDeteriorationPolicyCatalog.CreateCore();
            for (var coreIndex = 0;
                 coreIndex < deteriorationPolicies.Count;
                 coreIndex++)
            {
                var core = deteriorationPolicies[coreIndex];
                if (!world.MilitaryPatientReturnDeteriorationPolicies.Exists(
                    item => item.Id == core.Id))
                {
                    world.MilitaryPatientReturnDeteriorationPolicies.Add(core);
                }
            }

            for (var i = 0;
                 i < world.MilitaryPatientReturnDeathClosures.Count;
                 i++)
            {
                var closure = world.MilitaryPatientReturnDeathClosures[i];
                closure.PatientJourneyCompletedBeforeDeath = false;
                closure.TeamJourneySnapshotsAtDeath = new List<
                    MilitaryPatientReturnTeamJourneySnapshotState>();
            }
            world.MilitaryPatientArrivalWaitingTeamDeathContractActivationDay =
                checked(world.AbsoluteDay + 1);
            world.SchemaVersion = 61;
        }

        private static void MigrateVersionSixtyOneToSixtyTwo(
            WorldState world)
        {
            var corePolicies = MilitaryReturnTeamDeathPolicyCatalog.CreateCore();
            for (var coreIndex = 0;
                 coreIndex < corePolicies.Count;
                 coreIndex++)
            {
                var core = corePolicies[coreIndex];
                if (!world.MilitaryReturnTeamDeathPolicies.Exists(
                    item => item.Id == core.Id))
                {
                    world.MilitaryReturnTeamDeathPolicies.Add(core);
                }
            }

            world.MilitaryReturnTeamDeaths =
                new List<MilitaryReturnTeamDeathState>();
            for (var evacuationIndex = 0;
                 evacuationIndex < world.MilitaryMedicalEvacuations.Count;
                 evacuationIndex++)
            {
                var evacuation =
                    world.MilitaryMedicalEvacuations[evacuationIndex];
                if (evacuation.TeamMembers == null)
                {
                    continue;
                }
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    evacuation.TeamMembers[memberIndex].ReturnDeathId =
                        string.Empty;
                }
            }
            for (var i = 0;
                 i < world.MilitaryFamilyInheritances.Count;
                 i++)
            {
                world.MilitaryFamilyInheritances[i].ReturnTeamDeathId =
                    string.Empty;
            }
            for (var i = 0;
                 i < world.MilitarySurvivorCompensations.Count;
                 i++)
            {
                world.MilitarySurvivorCompensations[i].ReturnTeamDeathId =
                    string.Empty;
            }
            world.MilitaryReturnTeamDeathContractActivationDay = checked(
                world.AbsoluteDay + 1);
            world.SchemaVersion = 62;
        }

        private static void MigrateVersionSixtyTwoToSixtyThree(
            WorldState world)
        {
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                world.MilitaryMedicalTransfers[i]
                    .CompletedTreatmentStagesAtDispatch = 0;
            }
            world.MilitaryPostTreatmentTransferContractActivationDay =
                checked(world.AbsoluteDay + 1);
            world.SchemaVersion = 63;
        }

        private static void MigrateVersionSixtyThreeToSixtyFour(
            WorldState world)
        {
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = world.MilitaryMedicalTransfers[i];
                transfer.SequenceIndex = 0;
                transfer.PreviousMedicalTransferId = string.Empty;
                transfer.NextMedicalTransferId = string.Empty;
            }
            world.MilitaryRepeatedMedicalTransferContractActivationDay =
                checked(world.AbsoluteDay + 1);
            world.SchemaVersion = 64;
        }

        private static void MigrateVersionSixtyFourToSixtyFive(
            WorldState world,
            ProductionContentRegistry content)
        {
            for (var i = 0; i < world.Commodities.Count; i++)
            {
                var commodity = world.Commodities[i];
                commodity.ProductDefinitionId = commodity.Id ==
                    "commodity.cloth"
                    ? CoreProductionContent.PlainClothProductId
                    : string.Empty;
            }
            world.ProductionContentManifest = content.CreateManifest();
            world.SchemaVersion = 65;
        }

        private static void MigrateVersionSixtyFiveToSixtySix(
            WorldState world)
        {
            world.StrategicDelegationMandates =
                new List<StrategicDelegationMandateState>();
            world.StrategicDelegationCommandProposals =
                new List<StrategicDelegationCommandProposalState>();
            world.SchemaVersion = 66;
        }

        private static void MigrateVersionSixtySixToSixtySeven(
            WorldState world)
        {
            world.TownFacilities = new List<TownFacilityState>();
            world.MerchantBranches = new List<MerchantBranchState>();
            world.SchemaVersion = 67;
        }

        private static void MigrateVersionSixtySevenToSixtyEight(
            WorldState world)
        {
            world.TownFacilities ??= new List<TownFacilityState>();
            for (var i = 0; i < world.TownFacilities.Count; i++)
            {
                var facility = world.TownFacilities[i];
                if (facility != null && !facility.HasMapPlacement)
                {
                    CoreTownFacilityLayout.TryApplyZhongshan(facility);
                }
            }

            world.SchemaVersion = 68;
        }

        private static void MigrateVersionSixtyEightToSixtyNine(
            WorldState world)
        {
            world.CanonicalPlaceCrosswalks ??=
                new List<CanonicalPlaceCrosswalkState>();
            world.HistoricalIdentities ??= new List<HistoricalIdentityState>();
            world.PersonLineages ??= new List<PersonLineageState>();
            world.FamilyOrganizationProfiles ??=
                new List<FamilyOrganizationProfileState>();
            world.FamilyOrganizationMembers ??=
                new List<FamilyOrganizationMemberState>();
            world.FamilyCenters ??= new List<FamilyCenterState>();
            world.OrganizationAssets ??= new List<OrganizationAssetState>();
            world.CivilMilitaryOfficeDefinitions ??=
                new List<CivilMilitaryOfficeDefinitionState>();
            world.CivilMilitaryOfficeAssignments ??=
                new List<CivilMilitaryOfficeAssignmentState>();
            world.PersonPrimaryActivities ??=
                new List<PersonPrimaryActivityState>();
            world.HistoricalPersonFamilyIntegrations ??=
                new List<HistoricalPersonFamilyIntegrationState>();
            world.FacilityDefinitions ??= new List<FacilityDefinitionState>();
            world.Facilities ??= new List<FacilityState>();
            world.SchemaVersion = 69;
        }

        private static void MigrateVersionSixtyNineToSeventy(
            WorldState world)
        {
            world.LuoyangLivingWorlds ??=
                new List<Luoyang184LivingWorldState>();
            world.SchemaVersion = 70;
        }

        private static void MigrateVersionSeventyToSeventyOne(
            WorldState world)
        {
            world.WorldDecisionAgents ??=
                new List<WorldDecisionAgentState>();
            world.WorldSimulationLodStates ??=
                new List<WorldSimulationLodState>();
            world.SchemaVersion = 71;
        }

        private static void MigrateVersionSeventyOneToSeventyTwo(
            WorldState world)
        {
            world.WorldDecisionAgents ??=
                new List<WorldDecisionAgentState>();
            for (var i = 0; i < world.WorldDecisionAgents.Count; i++)
            {
                var state = world.WorldDecisionAgents[i];
                if (state == null)
                {
                    continue;
                }
                state.ModelId = string.IsNullOrWhiteSpace(state.ModelId)
                    ? "none"
                    : state.ModelId;
                state.PolicyProfileId ??= string.Empty;
                state.PrimaryGoalId ??= string.Empty;
                if (state.PrimaryGoalWeightBasisPoints < 0 ||
                    state.PrimaryGoalWeightBasisPoints > 10_000)
                {
                    state.PrimaryGoalWeightBasisPoints = 5_000;
                }
                state.Memory ??= new List<WorldDecisionMemoryEntryState>();
            }
            world.SchemaVersion = 72;
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
