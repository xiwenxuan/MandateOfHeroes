using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class CivilianMedicalDiagnosisResult
    {
        public bool Success { get; internal set; }
        public string MedicalCaseId { get; internal set; } = string.Empty;
        public string Message { get; internal set; } = string.Empty;
    }

    public sealed class CivilianMedicalTreatmentResult
    {
        public bool Success { get; internal set; }
        public string MedicalCaseId { get; internal set; } = string.Empty;
        public string TreatmentId { get; internal set; } = string.Empty;
        public string PrescriptionId { get; internal set; } = string.Empty;
        public string MedicalServiceId { get; internal set; } = string.Empty;
        public string InventoryTransactionId { get; internal set; } = string.Empty;
        public int MedicineUnitsConsumed { get; internal set; }
        public int RecoveredHealthBasisPoints { get; internal set; }
        public int WorkMinutes { get; internal set; }
        public long FeePaid { get; internal set; }
        public int PhysicianMedicalSkillGainBasisPoints { get; internal set; }
        public bool CaseClosed { get; internal set; }
        public string Message { get; internal set; } = string.Empty;
    }

    public sealed class CivilianMedicalSystem
    {
        private readonly ProductionContentRegistry _content;
        private readonly IPersonRepository _people;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public CivilianMedicalSystem(
            ProductionContentRegistry content = null,
            IPersonRepository people = null)
        {
            _content = content ?? ProductionContentRegistry.CreateCore();
            _people = people;
        }

        public CivilianMedicalDiagnosisResult DiagnoseNutritionCondition(
            WorldState world,
            string nutritionConditionEpisodeId,
            string physicianPersonId,
            string authorizingPersonId)
        {
            RequireWorld(world);
            world.Validate();
            var episode = FindEpisode(world, nutritionConditionEpisodeId);
            var existing = FindCaseForEpisode(world, episode.Id);
            if (existing != null)
            {
                EnsurePrescription(world, existing);
                world.Validate();
                return new CivilianMedicalDiagnosisResult
                {
                    Success = true,
                    MedicalCaseId = existing.Id,
                    Message = "The nutrition condition already has a medical case."
                };
            }
            if (episode.EndDay != -1)
            {
                return DiagnosisFailure(
                    "A resolved nutrition condition cannot receive a new diagnosis.");
            }

            var people = PeopleFor(world);
            var patient = people.GetRequired(episode.PersonId);
            var physician = people.GetRequired(physicianPersonId);
            var authorizer = people.GetRequired(authorizingPersonId);
            if (!CanPhysicianTreat(world, physician, patient, out var skill,
                    out var physicianFailure))
            {
                return DiagnosisFailure(physicianFailure);
            }
            if (!TryAuthorize(
                    world.AbsoluteDay,
                    patient,
                    authorizer,
                    out var authorizationPolicyId,
                    out var authorizationFailure))
            {
                return DiagnosisFailure(authorizationFailure);
            }

            var medicalCase = new CivilianMedicalCaseState
            {
                Id = $"civilian_medical_case.{world.AbsoluteDay}." +
                     $"{world.CivilianMedicalCases.Count:D6}",
                PatientPersonId = patient.Id,
                PatientFamilyIdAtDiagnosis = patient.FamilyId,
                NutritionConditionEpisodeId = episode.Id,
                DiagnosisId = CivilianMedicalDiagnosisIds.MalnutritionIllness,
                TreatmentProtocolId = CivilianMedicalTreatmentProtocolIds
                    .SupportiveHerbalCare,
                DiagnosedDay = world.AbsoluteDay,
                DiagnosingPhysicianPersonId = physician.Id,
                PhysicianMedicalSkillBasisPointsAtDiagnosis = skill,
                AuthorizingPersonId = authorizer.Id,
                AuthorizingFamilyIdAtDiagnosis = authorizer.FamilyId,
                AuthorizationPolicyId = authorizationPolicyId
            };
            world.CivilianMedicalCases.Add(medicalCase);
            var prescription = EnsurePrescription(world, medicalCase);
            world.Validate();
            return new CivilianMedicalDiagnosisResult
            {
                Success = true,
                MedicalCaseId = medicalCase.Id,
                Message = $"Diagnosed {patient.DisplayName} and issued " +
                    $"prescription {prescription.Id}."
            };
        }

        public CivilianMedicalTreatmentResult TreatNutritionCondition(
            WorldState world,
            string medicalCaseId,
            string physicianPersonId,
            string authorizingPersonId,
            string clinicFacilityId = null)
        {
            RequireWorld(world);
            world.Validate();
            var medicalCase = FindCase(world, medicalCaseId);
            var episode = FindEpisode(
                world, medicalCase.NutritionConditionEpisodeId);
            var profile = FindNutritionProfile(
                world, medicalCase.PatientPersonId);
            var people = PeopleFor(world);
            var patient = people.GetRequired(medicalCase.PatientPersonId);
            var physician = people.GetRequired(physicianPersonId);
            var authorizer = people.GetRequired(authorizingPersonId);
            if (medicalCase.Status == CivilianMedicalCaseStatus.Closed)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "A closed civilian medical case cannot be treated.");
            }
            if (world.AbsoluteDay <
                world.CivilianMedicalServiceContractActivationDay)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "Formal civilian medical service begins on the next day.");
            }
            if (!patient.IsAlive)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds.PatientDied);
                world.Validate();
                return TreatmentFailure(
                    medicalCase.Id,
                    "The patient has died and the case is closed.");
            }
            if (episode.EndDay != -1)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds
                        .NutritionConditionResolved);
                world.Validate();
                return TreatmentFailure(
                    medicalCase.Id,
                    "The nutrition condition has resolved and the case is closed.");
            }
            if (medicalCase.LastTreatmentDay == world.AbsoluteDay)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "The case has already been treated today.");
            }
            if (physician.Id != medicalCase.DiagnosingPhysicianPersonId ||
                authorizer.Id != medicalCase.AuthorizingPersonId)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "This prototype requires the diagnosed physician and authorizer.");
            }
            if (!CanPhysicianTreat(world, physician, patient, out var skill,
                    out var physicianFailure))
            {
                return TreatmentFailure(medicalCase.Id, physicianFailure);
            }
            if (!TryAuthorize(
                    world.AbsoluteDay,
                    patient,
                    authorizer,
                    out var authorizationPolicyId,
                    out var authorizationFailure) ||
                authorizationPolicyId != medicalCase.AuthorizationPolicyId)
            {
                return TreatmentFailure(
                    medicalCase.Id, authorizationFailure);
            }

            var prescription = EnsurePrescription(world, medicalCase);
            world.Validate();

            if (!TryResolveVenue(
                    world,
                    physician,
                    clinicFacilityId,
                    out var resolvedClinicFacilityId,
                    out var venuePolicyId,
                    out var venueFailure))
            {
                return TreatmentFailure(medicalCase.Id, venueFailure);
            }
            if (PhysicianWorkMinutesOnDay(
                    world,
                    physician.Id,
                    world.AbsoluteDay) +
                CivilianMedicalRules.TreatmentWorkMinutes >
                CivilianMedicalRules.MaximumDailyPhysicianWorkMinutes)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "The physician has no treatment work time remaining today.");
            }

            var recoverableDamage = episode.AppliedHealthDamageBasisPoints -
                episode.RecoveredHealthBasisPoints -
                medicalCase.TotalRecoveredHealthBasisPoints;
            recoverableDamage = Math.Min(
                recoverableDamage, 10_000 - patient.HealthBasisPoints);
            if (recoverableDamage <= 0)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds.InjuryRecovered);
                world.Validate();
                return TreatmentFailure(
                    medicalCase.Id,
                    "The nutrition injury is recovered and the case is closed.");
            }

            var medicineBatch = FindMedicineBatch(world, physician);
            if (medicineBatch == null)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "No unreserved formal herbal medicine batch is available.");
            }

            var payerFamily = FindFamily(world, patient.FamilyId);
            var payeeFamily = FindFamily(world, physician.FamilyId);
            var sameFamily = payerFamily.Id == payeeFamily.Id;
            var consultationFee = sameFamily
                ? 0
                : CivilianMedicalRules.BaseConsultationFee + skill / 500L;
            var medicineFee = sameFamily
                ? 0
                : CivilianMedicalRules.MedicineServiceFee;
            var totalFee = checked(consultationFee + medicineFee);
            if (payerFamily.Wealth < totalFee)
            {
                return TreatmentFailure(
                    medicalCase.Id,
                    "The patient household cannot pay the formal treatment fee.");
            }

            var recovery = Math.Min(
                recoverableDamage,
                Math.Min(
                    600,
                    100 + skill / 20 +
                    medicineBatch.QualityBasisPoints / 50));
            var treatmentId = $"civilian_medical_treatment." +
                $"{world.AbsoluteDay}.{world.CivilianMedicalTreatments.Count:D6}";
            var serviceId = $"civilian_medical_service." +
                $"{world.AbsoluteDay}.{world.CivilianMedicalServices.Count:D6}";
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.MedicalTreatmentConsumed,
                physician.Id,
                string.Empty,
                0,
                0,
                -medicineBatch.UnitWeight *
                    CivilianMedicalRules.MedicineUnitsPerTreatment,
                $"Consumed formal herbal medicine for {medicalCase.Id}.");
            transaction.SourceCivilianMedicalTreatmentId = treatmentId;
            transaction.Lines.Add(ProductInventorySystem.Line(
                medicineBatch,
                -CivilianMedicalRules.MedicineUnitsPerTreatment,
                0));

            var openingHealth = patient.HealthBasisPoints;
            var openingDebt = profile.NutritionDebtBasisUnits;
            var openingRisk = profile.DiseaseRiskBasisPoints;
            var payerWealthBefore = payerFamily.Wealth;
            var payeeWealthBefore = payeeFamily.Wealth;
            var physicianSkillBefore = skill;
            var physicianSkillGain = Math.Min(
                10_000 - physicianSkillBefore,
                Math.Max(1, 5 + recovery / 100));
            var writablePatient = people.GetRequiredForUpdate(patient.Id);
            writablePatient.HealthBasisPoints = checked(
                writablePatient.HealthBasisPoints + recovery);
            var writablePhysician = people.GetRequiredForUpdate(physician.Id);
            var physicianSkillAfter = checked(
                physicianSkillBefore + physicianSkillGain);
            writablePhysician.ProfessionalSkills ??=
                new ProfessionalSkillState();
            writablePhysician.ProfessionalSkills.Medicine = Math.Max(
                writablePhysician.ProfessionalSkills.Medicine,
                physicianSkillAfter);
            writablePhysician.MedicalSkillBasisPoints = Math.Max(
                writablePhysician.MedicalSkillBasisPoints,
                physicianSkillAfter);
            medicineBatch.Quantity = checked(
                medicineBatch.Quantity -
                CivilianMedicalRules.MedicineUnitsPerTreatment);
            if (!sameFamily)
            {
                payerFamily.Wealth = checked(payerFamily.Wealth - totalFee);
                payeeFamily.Wealth = checked(payeeFamily.Wealth + totalFee);
            }
            var treatment = new CivilianMedicalTreatmentState
            {
                Id = treatmentId,
                Day = world.AbsoluteDay,
                MedicalCaseId = medicalCase.Id,
                PatientPersonId = patient.Id,
                PhysicianPersonId = physician.Id,
                PhysicianMedicalSkillBasisPoints = skill,
                AuthorizingPersonId = authorizer.Id,
                AuthorizationPolicyId = authorizationPolicyId,
                MedicineProductDefinitionId = medicineBatch.ProductDefinitionId,
                SourceMedicineBatchId = medicineBatch.Id,
                InventoryTransactionId = transaction.Id,
                PrescriptionId = prescription.Id,
                MedicalServiceId = serviceId,
                MedicineUnitsConsumed =
                    CivilianMedicalRules.MedicineUnitsPerTreatment,
                OpeningHealthBasisPoints = openingHealth,
                ClosingHealthBasisPoints = writablePatient.HealthBasisPoints,
                RecoveredHealthBasisPoints = recovery,
                OpeningNutritionDebtBasisUnits = openingDebt,
                ClosingNutritionDebtBasisUnits = profile.NutritionDebtBasisUnits,
                OpeningDiseaseRiskBasisPoints = openingRisk,
                ClosingDiseaseRiskBasisPoints = profile.DiseaseRiskBasisPoints
            };
            var service = new CivilianMedicalServiceState
            {
                Id = serviceId,
                Day = world.AbsoluteDay,
                MedicalCaseId = medicalCase.Id,
                PrescriptionId = prescription.Id,
                TreatmentId = treatment.Id,
                PatientPersonId = patient.Id,
                PhysicianPersonId = physician.Id,
                AuthorizingPersonId = authorizer.Id,
                ClinicFacilityId = resolvedClinicFacilityId,
                VenuePolicyId = venuePolicyId,
                WorkMinutes = CivilianMedicalRules.TreatmentWorkMinutes,
                ConsultationFee = consultationFee,
                MedicineFee = medicineFee,
                TotalFee = totalFee,
                PayerFamilyId = payerFamily.Id,
                PayeeFamilyId = payeeFamily.Id,
                PaymentPolicyId = sameFamily
                    ? CivilianMedicalPaymentPolicyIds.SameHouseholdCare
                    : CivilianMedicalPaymentPolicyIds.HouseholdDirect,
                PayerFamilyWealthBefore = payerWealthBefore,
                PayerFamilyWealthAfter = payerFamily.Wealth,
                PayeeFamilyWealthBefore = payeeWealthBefore,
                PayeeFamilyWealthAfter = payeeFamily.Wealth,
                PhysicianMedicalSkillBeforeBasisPoints = physicianSkillBefore,
                PhysicianMedicalSkillAfterBasisPoints = physicianSkillAfter,
                PhysicianMedicalSkillGainBasisPoints = physicianSkillGain
            };
            medicalCase.LastTreatmentDay = world.AbsoluteDay;
            medicalCase.TotalMedicineUnitsConsumed = checked(
                medicalCase.TotalMedicineUnitsConsumed +
                treatment.MedicineUnitsConsumed);
            medicalCase.TotalRecoveredHealthBasisPoints = checked(
                medicalCase.TotalRecoveredHealthBasisPoints + recovery);
            world.InventoryTransactions.Add(transaction);
            world.CivilianMedicalTreatments.Add(treatment);
            world.CivilianMedicalServices.Add(service);
            TryCloseCase(world, medicalCase, episode, writablePatient);
            world.Validate();
            return new CivilianMedicalTreatmentResult
            {
                Success = true,
                MedicalCaseId = medicalCase.Id,
                TreatmentId = treatment.Id,
                PrescriptionId = prescription.Id,
                MedicalServiceId = service.Id,
                InventoryTransactionId = transaction.Id,
                MedicineUnitsConsumed = treatment.MedicineUnitsConsumed,
                RecoveredHealthBasisPoints = recovery,
                WorkMinutes = service.WorkMinutes,
                FeePaid = service.TotalFee,
                PhysicianMedicalSkillGainBasisPoints = physicianSkillGain,
                CaseClosed = medicalCase.Status ==
                    CivilianMedicalCaseStatus.Closed,
                Message = $"Treated {patient.DisplayName} with formal herbal medicine."
            };
        }

        public CivilianMedicalCaseState FindCaseForEpisode(
            WorldState world,
            string episodeId)
        {
            for (var i = 0; i < world.CivilianMedicalCases.Count; i++)
            {
                if (world.CivilianMedicalCases[i].NutritionConditionEpisodeId ==
                    episodeId)
                {
                    return world.CivilianMedicalCases[i];
                }
            }
            return null;
        }

        public int ReconcileCasesForResidents(
            WorldState world,
            HashSet<string> residentPersonIds)
        {
            RequireWorld(world);
            if (residentPersonIds == null)
            {
                throw new ArgumentNullException(nameof(residentPersonIds));
            }
            world.Validate();
            var closed = 0;
            var people = PeopleFor(world);
            for (var i = 0; i < world.CivilianMedicalCases.Count; i++)
            {
                var medicalCase = world.CivilianMedicalCases[i];
                if (medicalCase.Status != CivilianMedicalCaseStatus.Active ||
                    !residentPersonIds.Contains(medicalCase.PatientPersonId))
                {
                    continue;
                }
                var episode = FindEpisode(
                    world, medicalCase.NutritionConditionEpisodeId);
                var patient = people.GetRequired(medicalCase.PatientPersonId);
                if (TryCloseCase(world, medicalCase, episode, patient))
                {
                    closed++;
                }
            }
            world.Validate();
            return closed;
        }

        private CivilianMedicalPrescriptionState EnsurePrescription(
            WorldState world,
            CivilianMedicalCaseState medicalCase)
        {
            if (!string.IsNullOrEmpty(medicalCase.PrescriptionId))
            {
                for (var i = 0;
                     i < world.CivilianMedicalPrescriptions.Count;
                     i++)
                {
                    if (world.CivilianMedicalPrescriptions[i].Id ==
                        medicalCase.PrescriptionId)
                    {
                        return world.CivilianMedicalPrescriptions[i];
                    }
                }
                throw new InvalidOperationException(
                    $"Missing prescription {medicalCase.PrescriptionId}.");
            }
            if (medicalCase.Status != CivilianMedicalCaseStatus.Active)
            {
                throw new InvalidOperationException(
                    "A closed medical case cannot receive a prescription.");
            }

            var prescription = new CivilianMedicalPrescriptionState
            {
                Id = $"civilian_medical_prescription.{world.AbsoluteDay}." +
                    $"{world.CivilianMedicalPrescriptions.Count:D6}",
                MedicalCaseId = medicalCase.Id,
                PatientPersonId = medicalCase.PatientPersonId,
                PrescribingPhysicianPersonId =
                    medicalCase.DiagnosingPhysicianPersonId,
                IssuedDay = world.AbsoluteDay,
                PrescriptionProtocolId =
                    CivilianMedicalPrescriptionProtocolIds
                        .SupportiveHerbalMaterial,
                Items = new List<CivilianMedicalPrescriptionItemState>
                {
                    new CivilianMedicalPrescriptionItemState
                    {
                        ProductDefinitionId = CoreProductionContent
                            .HerbalMedicineMaterialProductId,
                        UnitsPerTreatment =
                            CivilianMedicalRules.MedicineUnitsPerTreatment,
                        AdministrationRouteId =
                            CivilianMedicalAdministrationRouteIds
                                .OralPreparedHerbalMedicine
                    }
                }
            };
            medicalCase.PrescriptionId = prescription.Id;
            world.CivilianMedicalPrescriptions.Add(prescription);
            return prescription;
        }

        private static bool TryResolveVenue(
            WorldState world,
            PersonState physician,
            string clinicFacilityId,
            out string resolvedClinicFacilityId,
            out string venuePolicyId,
            out string failure)
        {
            resolvedClinicFacilityId = string.Empty;
            venuePolicyId = CivilianMedicalVenuePolicyIds.HomeVisit;
            failure = string.Empty;
            if (string.IsNullOrEmpty(clinicFacilityId))
            {
                return true;
            }

            VillageFacilityState clinic = null;
            for (var i = 0; i < world.VillageFacilities.Count; i++)
            {
                if (world.VillageFacilities[i].Id == clinicFacilityId)
                {
                    clinic = world.VillageFacilities[i];
                    break;
                }
            }
            VillageState village = null;
            if (clinic != null)
            {
                for (var i = 0; i < world.Villages.Count; i++)
                {
                    if (world.Villages[i].Id == clinic.VillageId)
                    {
                        village = world.Villages[i];
                        break;
                    }
                }
            }
            if (clinic == null || village == null ||
                clinic.Kind != VillageFacilityKind.Clinic ||
                clinic.Capacity <= 0 ||
                clinic.ConditionBasisPoints <= 0 ||
                village.LocationId != physician.LocationId)
            {
                failure = "The selected village clinic is not operational locally.";
                return false;
            }

            resolvedClinicFacilityId = clinic.Id;
            venuePolicyId = CivilianMedicalVenuePolicyIds.VillageClinic;
            return true;
        }

        private static int PhysicianWorkMinutesOnDay(
            WorldState world,
            string physicianPersonId,
            long day)
        {
            var workMinutes = 0;
            for (var i = 0; i < world.CivilianMedicalServices.Count; i++)
            {
                var service = world.CivilianMedicalServices[i];
                if (service.PhysicianPersonId == physicianPersonId &&
                    service.Day == day)
                {
                    workMinutes = checked(
                        workMinutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryMedicalServices.Count; i++)
            {
                var service = world.MilitaryMedicalServices[i];
                if (service.PhysicianPersonId == physicianPersonId &&
                    service.Day == day)
                {
                    workMinutes = checked(
                        workMinutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryRearMedicalTreatments.Count; i++)
            {
                var treatment = world.MilitaryRearMedicalTreatments[i];
                if (treatment.PhysicianPersonId == physicianPersonId &&
                    treatment.Day == day)
                {
                    workMinutes = checked(
                        workMinutes + treatment.WorkMinutes);
                }
            }
            return workMinutes;
        }

        private static bool TryCloseCase(
            WorldState world,
            CivilianMedicalCaseState medicalCase,
            NutritionConditionEpisodeState episode,
            PersonState patient)
        {
            if (medicalCase.Status == CivilianMedicalCaseStatus.Closed)
            {
                return false;
            }
            if (!patient.IsAlive)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds.PatientDied);
                return true;
            }
            if (episode.EndDay != -1)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds
                        .NutritionConditionResolved);
                return true;
            }
            if (episode.AppliedHealthDamageBasisPoints -
                    episode.RecoveredHealthBasisPoints -
                    medicalCase.TotalRecoveredHealthBasisPoints <= 0)
            {
                CloseCase(
                    world,
                    medicalCase,
                    CivilianMedicalCaseClosureReasonIds.InjuryRecovered);
                return true;
            }
            return false;
        }

        private static void CloseCase(
            WorldState world,
            CivilianMedicalCaseState medicalCase,
            string closureReasonId)
        {
            medicalCase.Status = CivilianMedicalCaseStatus.Closed;
            medicalCase.ClosedDay = world.AbsoluteDay;
            medicalCase.ClosureReasonId = closureReasonId;
            if (string.IsNullOrEmpty(medicalCase.PrescriptionId))
            {
                return;
            }
            for (var i = 0;
                 i < world.CivilianMedicalPrescriptions.Count;
                 i++)
            {
                var prescription = world.CivilianMedicalPrescriptions[i];
                if (prescription.Id == medicalCase.PrescriptionId)
                {
                    prescription.IsActive = false;
                    prescription.ClosedDay = world.AbsoluteDay;
                    return;
                }
            }
            throw new InvalidOperationException(
                $"Missing prescription {medicalCase.PrescriptionId}.");
        }

        private ProductBatchState FindMedicineBatch(
            WorldState world,
            PersonState physician)
        {
            _content.ValidateManifest(world.ProductionContentManifest);
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.OwnerFamilyId == physician.FamilyId &&
                    batch.ProductDefinitionId ==
                        CoreProductionContent.HerbalMedicineMaterialProductId &&
                    batch.Quantity - batch.ReservedQuantity >=
                        CivilianMedicalRules.MedicineUnitsPerTreatment &&
                    BatchLocationId(world, batch) == physician.LocationId)
                {
                    candidates.Add(batch);
                }
            }
            candidates.Sort((left, right) =>
            {
                var day = left.ProducedDay.CompareTo(right.ProducedDay);
                return day != 0
                    ? day
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return candidates.Count == 0 ? null : candidates[0];
        }

        private static string BatchLocationId(
            WorldState world,
            ProductBatchState batch)
        {
            if (!string.IsNullOrEmpty(batch.InventoryContainerId))
            {
                return ProductInventorySystem.FindContainer(
                    world, batch.InventoryContainerId).LocationId;
            }
            var storage = ProductInventorySystem.FindFacility(
                world, batch.StorageFacilityId);
            return ProductInventorySystem.FindVillage(
                world, storage.VillageId).LocationId;
        }

        private static bool CanPhysicianTreat(
            WorldState world,
            PersonState physician,
            PersonState patient,
            out int skill,
            out string failure)
        {
            skill = Math.Max(
                physician.MedicalSkillBasisPoints,
                physician.ProfessionalSkills?.Medicine ?? 0);
            if (!physician.IsAlive || !patient.IsAlive ||
                physician.VillageOccupation != VillageOccupation.Physician ||
                skill < CivilianMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                failure = "The selected person lacks civilian physician authority.";
                return false;
            }
            if (physician.LocationId != patient.LocationId ||
                IsTraveling(world, physician.Id) ||
                physician.LocalDuty != LocalDutyKind.None)
            {
                failure = "The physician is not locally available.";
                return false;
            }
            failure = string.Empty;
            return true;
        }

        private static bool TryAuthorize(
            long day,
            PersonState patient,
            PersonState authorizer,
            out string policyId,
            out string failure)
        {
            policyId = string.Empty;
            failure = string.Empty;
            if (!authorizer.IsAlive ||
                authorizer.LocationId != patient.LocationId ||
                AgeYears(authorizer, day) < CivilianMedicalRules.AdultAgeYears)
            {
                failure = "The authorizer is not an available adult.";
                return false;
            }
            if (AgeYears(patient, day) >= CivilianMedicalRules.AdultAgeYears)
            {
                if (authorizer.Id != patient.Id)
                {
                    failure = "An independent adult patient must authorize this treatment.";
                    return false;
                }
                policyId = CivilianMedicalAuthorizationPolicyIds.PatientSelf;
                return true;
            }
            if (authorizer.Id == patient.Id ||
                authorizer.FamilyId != patient.FamilyId)
            {
                failure = "A minor requires an adult from the same household.";
                return false;
            }
            policyId = CivilianMedicalAuthorizationPolicyIds
                .HouseholdAdultCaregiver;
            return true;
        }

        private static long AgeYears(PersonState person, long day)
        {
            return Math.Max(0, (day - person.BirthDay) / 360);
        }

        private static bool IsTraveling(WorldState world, string personId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].PersonId == personId)
                {
                    return true;
                }
            }
            return false;
        }

        private static NutritionConditionEpisodeState FindEpisode(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.NutritionConditionEpisodes.Count; i++)
            {
                if (world.NutritionConditionEpisodes[i].Id == id)
                {
                    return world.NutritionConditionEpisodes[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing nutrition condition episode {id}.");
        }

        private static PersonNutritionProfileState FindNutritionProfile(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.PersonNutritionProfiles.Count; i++)
            {
                if (world.PersonNutritionProfiles[i].PersonId == personId)
                {
                    return world.PersonNutritionProfiles[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing nutrition profile for {personId}.");
        }

        private static CivilianMedicalCaseState FindCase(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.CivilianMedicalCases.Count; i++)
            {
                if (world.CivilianMedicalCases[i].Id == id)
                {
                    return world.CivilianMedicalCases[i];
                }
            }
            throw new InvalidOperationException($"Missing medical case {id}.");
        }

        private static FamilyState FindFamily(WorldState world, string id)
        {
            for (var i = 0; i < world.Families.Count; i++)
            {
                if (world.Families[i].Id == id)
                {
                    return world.Families[i];
                }
            }
            throw new InvalidOperationException($"Missing family {id}.");
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

        private static void RequireWorld(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
        }

        private static CivilianMedicalDiagnosisResult DiagnosisFailure(
            string message)
        {
            return new CivilianMedicalDiagnosisResult
            {
                Success = false,
                Message = message
            };
        }

        private static CivilianMedicalTreatmentResult TreatmentFailure(
            string medicalCaseId,
            string message)
        {
            return new CivilianMedicalTreatmentResult
            {
                Success = false,
                MedicalCaseId = medicalCaseId,
                Message = message
            };
        }
    }
}
