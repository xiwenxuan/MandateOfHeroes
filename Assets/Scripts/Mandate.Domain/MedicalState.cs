using System;
using System.Collections.Generic;

namespace Mandate.Domain
{
    public static class CivilianMedicalDiagnosisIds
    {
        public const string MalnutritionIllness =
            "diagnosis.nutrition.malnutrition_illness";
    }

    public static class CivilianMedicalTreatmentProtocolIds
    {
        public const string SupportiveHerbalCare =
            "treatment.nutrition.supportive_herbal_care";
    }

    public static class CivilianMedicalPrescriptionProtocolIds
    {
        public const string SupportiveHerbalMaterial =
            "prescription.nutrition.supportive_herbal_material";
    }

    public static class CivilianMedicalAdministrationRouteIds
    {
        public const string OralPreparedHerbalMedicine =
            "medical_administration.oral.prepared_herbal_medicine";
    }

    public static class CivilianMedicalVenuePolicyIds
    {
        public const string VillageClinic = "medical_venue.village_clinic";
        public const string HomeVisit = "medical_venue.home_visit";
    }

    public static class CivilianMedicalPaymentPolicyIds
    {
        public const string HouseholdDirect =
            "medical_payment.household_direct";
        public const string SameHouseholdCare =
            "medical_payment.same_household_care";
    }

    public static class CivilianMedicalCaseClosureReasonIds
    {
        public const string InjuryRecovered =
            "medical_case_closure.injury_recovered";
        public const string NutritionConditionResolved =
            "medical_case_closure.nutrition_condition_resolved";
        public const string PatientDied =
            "medical_case_closure.patient_died";
    }

    public static class CivilianMedicalAuthorizationPolicyIds
    {
        public const string PatientSelf = "medical_authorization.patient_self";
        public const string HouseholdAdultCaregiver =
            "medical_authorization.household_adult_caregiver";
    }

    public static class CivilianMedicalRules
    {
        public const int AdultAgeYears = 15;
        public const int MinimumPhysicianSkillBasisPoints = 2_500;
        public const int MedicineUnitsPerTreatment = 1;
        public const int TreatmentWorkMinutes = 120;
        public const int MaximumDailyPhysicianWorkMinutes = 480;
        public const long BaseConsultationFee = 20;
        public const long MedicineServiceFee = 5;

        public static long RecommendedTreatmentFee(
            int physicianMedicalSkillBasisPoints)
        {
            if (physicianMedicalSkillBasisPoints < 0 ||
                physicianMedicalSkillBasisPoints > 10_000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicianMedicalSkillBasisPoints));
            }

            return checked(
                BaseConsultationFee +
                physicianMedicalSkillBasisPoints / 500L +
                MedicineServiceFee);
        }
    }

    public enum CivilianMedicalCaseStatus : byte
    {
        Active,
        Closed
    }

    [Serializable]
    public sealed class CivilianMedicalCaseState
    {
        public string Id;
        public string PatientPersonId;
        public string PatientFamilyIdAtDiagnosis;
        public string NutritionConditionEpisodeId;
        public string DiagnosisId;
        public string TreatmentProtocolId;
        public long DiagnosedDay;
        public string DiagnosingPhysicianPersonId;
        public int PhysicianMedicalSkillBasisPointsAtDiagnosis;
        public string AuthorizingPersonId;
        public string AuthorizingFamilyIdAtDiagnosis;
        public string AuthorizationPolicyId;
        public string PrescriptionId;
        public long LastTreatmentDay = -1;
        public int TotalMedicineUnitsConsumed;
        public int TotalRecoveredHealthBasisPoints;
        public CivilianMedicalCaseStatus Status =
            CivilianMedicalCaseStatus.Active;
        public long ClosedDay = -1;
        public string ClosureReasonId;
    }

    [Serializable]
    public sealed class CivilianMedicalPrescriptionItemState
    {
        public string ProductDefinitionId;
        public int UnitsPerTreatment;
        public string AdministrationRouteId;
    }

    [Serializable]
    public sealed class CivilianMedicalPrescriptionState
    {
        public string Id;
        public string MedicalCaseId;
        public string PatientPersonId;
        public string PrescribingPhysicianPersonId;
        public long IssuedDay;
        public string PrescriptionProtocolId;
        public bool IsActive = true;
        public long ClosedDay = -1;
        public List<CivilianMedicalPrescriptionItemState> Items =
            new List<CivilianMedicalPrescriptionItemState>();
    }

    [Serializable]
    public sealed class CivilianMedicalTreatmentState
    {
        public string Id;
        public long Day;
        public string MedicalCaseId;
        public string PatientPersonId;
        public string PhysicianPersonId;
        public int PhysicianMedicalSkillBasisPoints;
        public string AuthorizingPersonId;
        public string AuthorizationPolicyId;
        public string MedicineProductDefinitionId;
        public string SourceMedicineBatchId;
        public string InventoryTransactionId;
        public string PrescriptionId;
        public string MedicalServiceId;
        public int MedicineUnitsConsumed;
        public int OpeningHealthBasisPoints;
        public int ClosingHealthBasisPoints;
        public int RecoveredHealthBasisPoints;
        public long OpeningNutritionDebtBasisUnits;
        public long ClosingNutritionDebtBasisUnits;
        public int OpeningDiseaseRiskBasisPoints;
        public int ClosingDiseaseRiskBasisPoints;
    }

    [Serializable]
    public sealed class CivilianMedicalServiceState
    {
        public string Id;
        public long Day;
        public string MedicalCaseId;
        public string PrescriptionId;
        public string TreatmentId;
        public string PatientPersonId;
        public string PhysicianPersonId;
        public string AuthorizingPersonId;
        public string ClinicFacilityId;
        public string VenuePolicyId;
        public int WorkMinutes;
        public long ConsultationFee;
        public long MedicineFee;
        public long TotalFee;
        public string PayerFamilyId;
        public string PayeeFamilyId;
        public string PaymentPolicyId;
        public long PayerFamilyWealthBefore;
        public long PayerFamilyWealthAfter;
        public long PayeeFamilyWealthBefore;
        public long PayeeFamilyWealthAfter;
        public int PhysicianMedicalSkillBeforeBasisPoints;
        public int PhysicianMedicalSkillAfterBasisPoints;
        public int PhysicianMedicalSkillGainBasisPoints;
    }

    public static class MilitaryMedicalTriageIds
    {
        public const string Critical = "military_triage.critical";
        public const string Severe = "military_triage.severe";
        public const string Moderate = "military_triage.moderate";
    }

    public static class MilitaryMedicalTreatmentProtocolIds
    {
        public const string FieldHerbalCare =
            "military_treatment.field_herbal_care";
    }

    public static class MilitaryMedicalVenuePolicyIds
    {
        public const string ArmyFieldUnit =
            "military_medical_venue.army_field_unit";
        public const string RearClinic =
            "military_medical_venue.rear_clinic";
    }

    public static class MilitaryMedicalAuthorizationPolicyIds
    {
        public const string InternalMedic =
            "military_medical_authorization.internal_medic";
        public const string CommanderAuthorizedPractitioner =
            "military_medical_authorization.commander_authorized_practitioner";
    }

    public static class MilitaryMedicalCaseClosureReasonIds
    {
        public const string ReturnedToDuty =
            "military_medical_case_closure.returned_to_duty";
    }

    public static class MilitaryMedicalRules
    {
        public const int MinimumPhysicianSkillBasisPoints = 2_500;
        public const int MedicineUnitsPerTreatment = 1;
        public const int TreatmentWorkMinutes = 60;
        public const int MaximumDailyPhysicianWorkMinutes = 480;
        public const int ReturnToDutyHealthBasisPoints = 6_000;
        public const int PrototypeOpeningMedicineQuantity = 20;
        public const long PrototypeMedicalContainerCapacityWeight = 200;
        public const int MinimumEvacuationTeamMembers = 2;
        public const int MaximumEvacuationTeamMembers = 8;
        public const int MaximumMedicalTransfersPerAdmission = 4;
        public const int RearTreatmentWorkMinutes = 120;
        public const int FieldStabilizationWorkMinutes = 60;
        public const int FieldStabilizationHealthBasisPoints = 5_000;
        public const int FieldHospitalRequiredTimberUnits = 20;
        public const int FieldHospitalRequiredLeatherUnits = 5;
        public const int FieldHospitalRequiredMoney = 500;
        public const int FieldHospitalRequiredLaborDays = 3;
        public const int FieldHospitalBedCapacity = 4;
        public const int FieldHospitalMaintenanceIntervalDays = 10;
        public const int FieldHospitalMaintenanceMoney = 100;
        public const int FieldHospitalMaintenanceTimberUnits = 1;
        public const int InfectionRiskThresholdBasisPoints = 6_000;
        public const int InfectionControlWorkMinutes = 180;
        public const int InfectionControlMedicineUnits = 2;
        public const int InfectionControlHealthBasisPoints = 5_000;
        public const int TraumaSurgeryWorkMinutes = 240;
        public const int TraumaSurgeryMedicineUnits = 3;
        public const int TraumaSurgeryHealthBasisPoints = 5_000;
        public const int TraumaSurgeryMinimumSkillBasisPoints = 5_000;
        public const int PermanentImpairmentSeverityBasisPoints = 8_000;
        public const int PermanentImpairmentLaborPenaltyBasisPoints = 3_000;
    }

    public static class MilitaryRearMedicalSiteKindIds
    {
        public const string ExistingClinic =
            "military_rear_medical_site.existing_clinic";
        public const string FieldHospital =
            "military_rear_medical_site.field_hospital";
    }

    public static class MilitaryRearMedicalTreatmentProtocolIds
    {
        public const string InpatientHerbalRecovery =
            "military_rear_treatment.inpatient_herbal_recovery";
        public const string FieldStabilization =
            "military_rear_treatment.field_stabilization";
        public const string FieldRecovery =
            "military_rear_treatment.field_recovery";
        public const string InfectionControl =
            "military_rear_treatment.infection_control";
        public const string TraumaSurgery =
            "military_rear_treatment.trauma_surgery";
    }

    public static class MilitarySurgicalProcedureIds
    {
        public const string TraumaDebridementAndReduction =
            "military_surgical_procedure.trauma_debridement_and_reduction";
    }

    public static class MilitaryInjuryOutcomeIds
    {
        public const string NoPermanentImpairment =
            "military_injury_outcome.no_permanent_impairment";
        public const string PermanentMobilityImpairment =
            "military_injury_outcome.permanent_mobility_impairment";
    }

    public static class MilitaryWoundDeathPolicyIds
    {
        public const string SeverePostTreatmentComplication =
            "military_wound_death_policy.severe_post_treatment_complication";
        public const string SevereOriginalEvacuationComplication =
            "military_wound_death_policy.severe_original_evacuation_complication";
        public const string SevereReturnJourneyComplication =
            "military_wound_death_policy.severe_return_journey_complication";
        public const string SevereAwaitingTeamRejoinComplication =
            "military_wound_death_policy.severe_awaiting_team_rejoin_complication";
    }

    public static class MilitaryWoundDeathContextIds
    {
        public const string PostReturnMedicalRetirement =
            "military_wound_death_context.post_return_medical_retirement";
        public const string ReadyForReturnAtCareSite =
            "military_wound_death_context.ready_for_return_at_care_site";
        public const string InTreatmentAtCareSite =
            "military_wound_death_context.in_treatment_at_care_site";
        public const string DuringCrossFacilityTransfer =
            "military_wound_death_context.during_cross_facility_transfer";
        public const string DuringOriginalEvacuation =
            "military_wound_death_context.during_original_evacuation";
        public const string DuringPatientReturnJourney =
            "military_wound_death_context.during_patient_return_journey";
        public const string AwaitingReturnTeamRejoinAtArmy =
            "military_wound_death_context.awaiting_return_team_rejoin_at_army";
    }

    public static class MilitaryInpatientDeteriorationPolicyIds
    {
        public const string SevereWoundComplication =
            "military_inpatient_deterioration_policy.severe_wound_complication";
    }

    public static class MilitaryOriginalEvacuationDeteriorationPolicyIds
    {
        public const string SevereUntreatedTransitComplication =
            "military_original_evacuation_deterioration.severe_untreated_transit_complication";
    }

    public static class MilitaryPatientReturnDeteriorationPolicyIds
    {
        public const string SevereTravelRelapse =
            "military_patient_return_deterioration.severe_travel_relapse";
        public const string SeverePostJourneyRelapse =
            "military_patient_return_deterioration.severe_post_journey_relapse";
    }

    public static class MilitaryReturnTeamDeathPolicyIds
    {
        public const string ReturnJourneyFatality =
            "military_return_team_death_policy.return_journey_fatality";
    }

    public static class MilitaryReturnTeamCorpsePolicyIds
    {
        public const string ContinueExistingJourneyToSourceArmy =
            "military_return_team_corpse.continue_existing_journey_to_source_army";
    }

    [Serializable]
    public sealed class MilitaryReturnTeamDeathPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumDaysAfterReturnStart;
        public int HealthLossBasisPoints;
        public int MaximumClosingHealthBasisPoints;
        public long BaseCompensationMoney;
        public long CompensationPerRankMoney;
    }

    public static class MilitaryReturnTeamDeathPolicyCatalog
    {
        public static List<MilitaryReturnTeamDeathPolicyDefinitionState>
            CreateCore()
        {
            return new List<MilitaryReturnTeamDeathPolicyDefinitionState>
            {
                new MilitaryReturnTeamDeathPolicyDefinitionState
                {
                    Id = MilitaryReturnTeamDeathPolicyIds
                        .ReturnJourneyFatality,
                    DisplayName = "Return-team journey fatality",
                    MinimumDaysAfterReturnStart = 1,
                    HealthLossBasisPoints = 10_000,
                    MaximumClosingHealthBasisPoints = 0,
                    BaseCompensationMoney = 200,
                    CompensationPerRankMoney = 25
                }
            };
        }
    }

    [Serializable]
    public sealed class
        MilitaryPatientReturnDeteriorationPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumSeverityBasisPoints;
        public int MinimumDaysAfterReturnStart;
        public int HealthLossBasisPoints;
        public int MaximumClosingHealthBasisPoints;
    }

    public static class MilitaryPatientReturnDeteriorationPolicyCatalog
    {
        public static List<
            MilitaryPatientReturnDeteriorationPolicyDefinitionState>
            CreateCore()
        {
            return new List<
                MilitaryPatientReturnDeteriorationPolicyDefinitionState>
            {
                new MilitaryPatientReturnDeteriorationPolicyDefinitionState
                {
                    Id = MilitaryPatientReturnDeteriorationPolicyIds
                        .SevereTravelRelapse,
                    DisplayName = "重伤返军途中复发",
                    MinimumSeverityBasisPoints = 5_000,
                    MinimumDaysAfterReturnStart = 1,
                    HealthLossBasisPoints = 5_000,
                    MaximumClosingHealthBasisPoints = 1_500
                },
                new MilitaryPatientReturnDeteriorationPolicyDefinitionState
                {
                    Id = MilitaryPatientReturnDeteriorationPolicyIds
                        .SeverePostJourneyRelapse,
                    DisplayName = "重伤抵军后等待队员期间复发",
                    MinimumSeverityBasisPoints = 5_000,
                    MinimumDaysAfterReturnStart = 1,
                    HealthLossBasisPoints = 5_000,
                    MaximumClosingHealthBasisPoints = 1_500
                }
            };
        }
    }

    [Serializable]
    public sealed class
        MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumDaysAfterDispatch;
        public int MaximumOpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int MaximumClosingHealthBasisPoints;
    }

    public static class MilitaryOriginalEvacuationDeteriorationPolicyCatalog
    {
        public static List<
            MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>
            CreateCore()
        {
            return new List<
                MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState>
            {
                new MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState
                {
                    Id = MilitaryOriginalEvacuationDeteriorationPolicyIds
                        .SevereUntreatedTransitComplication,
                    DisplayName = "重伤首次后送途中恶化",
                    MinimumDaysAfterDispatch = 1,
                    MaximumOpeningHealthBasisPoints = 2_500,
                    HealthLossBasisPoints = 1_000,
                    MaximumClosingHealthBasisPoints = 1_500
                }
            };
        }
    }

    [Serializable]
    public sealed class MilitaryInpatientDeteriorationPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumSeverityBasisPoints;
        public int MinimumDaysAfterAdmission;
        public int HealthLossBasisPoints;
        public int MaximumClosingHealthBasisPoints;
    }

    public static class MilitaryInpatientDeteriorationPolicyCatalog
    {
        public static List<MilitaryInpatientDeteriorationPolicyDefinitionState>
            CreateCore()
        {
            return new List<
                MilitaryInpatientDeteriorationPolicyDefinitionState>
            {
                new MilitaryInpatientDeteriorationPolicyDefinitionState
                {
                    Id = MilitaryInpatientDeteriorationPolicyIds
                        .SevereWoundComplication,
                    DisplayName = "重伤住院并发恶化",
                    MinimumSeverityBasisPoints = 8_000,
                    MinimumDaysAfterAdmission = 1,
                    HealthLossBasisPoints = 1_000,
                    MaximumClosingHealthBasisPoints = 5_000
                }
            };
        }
    }

    public static class MilitaryMedicalDeathResponsibilityPolicyIds
    {
        public const string CurrentCareTeamDocumented =
            "military_medical_death_responsibility.current_care_team_documented";
        public const string SourceCareUntilTransferHandoff =
            "military_medical_death_responsibility.source_care_until_transfer_handoff";
        public const string SourceArmyUntilRearHandoff =
            "military_medical_death_responsibility.source_army_until_rear_handoff";
        public const string LastCareTeamDuringAuthorizedReturn =
            "military_medical_death_responsibility.last_care_team_during_authorized_return";
    }

    [Serializable]
    public sealed class MilitaryWoundDeathPolicyDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumSeverityBasisPoints;
        public int MaximumPostTreatmentHealthBasisPoints;
        public int MinimumDaysAfterCareCompletion;
        public long BaseCompensationMoney;
        public long CompensationPerRankMoney;
    }

    public static class MilitaryWoundDeathPolicyCatalog
    {
        public static List<MilitaryWoundDeathPolicyDefinitionState> CreateCore()
        {
            return new List<MilitaryWoundDeathPolicyDefinitionState>
            {
                new MilitaryWoundDeathPolicyDefinitionState
                {
                    Id = MilitaryWoundDeathPolicyIds
                        .SeverePostTreatmentComplication,
                    DisplayName = "重伤治疗后并发症",
                    MinimumSeverityBasisPoints = 8_000,
                    MaximumPostTreatmentHealthBasisPoints = 6_000,
                    MinimumDaysAfterCareCompletion = 1,
                    BaseCompensationMoney = 200,
                    CompensationPerRankMoney = 25
                },
                new MilitaryWoundDeathPolicyDefinitionState
                {
                    Id = MilitaryWoundDeathPolicyIds
                        .SevereAwaitingTeamRejoinComplication,
                    DisplayName = "重伤抵军后等待队员期间并发症",
                    MinimumSeverityBasisPoints = 5_000,
                    MaximumPostTreatmentHealthBasisPoints = 1_500,
                    MinimumDaysAfterCareCompletion = 1,
                    BaseCompensationMoney = 200,
                    CompensationPerRankMoney = 25
                },
                new MilitaryWoundDeathPolicyDefinitionState
                {
                    Id = MilitaryWoundDeathPolicyIds
                        .SevereOriginalEvacuationComplication,
                    DisplayName = "重伤首次后送并发症",
                    MinimumSeverityBasisPoints = 8_000,
                    MaximumPostTreatmentHealthBasisPoints = 2_000,
                    MinimumDaysAfterCareCompletion = 1,
                    BaseCompensationMoney = 200,
                    CompensationPerRankMoney = 25
                },
                new MilitaryWoundDeathPolicyDefinitionState
                {
                    Id = MilitaryWoundDeathPolicyIds
                        .SevereReturnJourneyComplication,
                    DisplayName = "重伤返军途中并发症",
                    MinimumSeverityBasisPoints = 5_000,
                    MaximumPostTreatmentHealthBasisPoints = 1_500,
                    MinimumDaysAfterCareCompletion = 1,
                    BaseCompensationMoney = 200,
                    CompensationPerRankMoney = 25
                }
            };
        }
    }

    [Serializable]
    public sealed class MilitarySurgicalProcedureDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumSeverityBasisPoints;
        public int MinimumPhysicianSkillBasisPoints;
        public int WorkMinutes;
        public int MedicineUnits;
        public int TargetHealthBasisPoints;
        public int PermanentImpairmentSeverityBasisPoints;
        public int PermanentImpairmentLaborPenaltyBasisPoints;
    }

    public static class MilitarySurgicalProcedureCatalog
    {
        public static List<MilitarySurgicalProcedureDefinitionState> CreateCore()
        {
            return new List<MilitarySurgicalProcedureDefinitionState>
            {
                new MilitarySurgicalProcedureDefinitionState
                {
                    Id = MilitarySurgicalProcedureIds
                        .TraumaDebridementAndReduction,
                    DisplayName = "创伤清创复位",
                    MinimumSeverityBasisPoints = 5_500,
                    MinimumPhysicianSkillBasisPoints =
                        MilitaryMedicalRules
                            .TraumaSurgeryMinimumSkillBasisPoints,
                    WorkMinutes = MilitaryMedicalRules
                        .TraumaSurgeryWorkMinutes,
                    MedicineUnits = MilitaryMedicalRules
                        .TraumaSurgeryMedicineUnits,
                    TargetHealthBasisPoints = MilitaryMedicalRules
                        .TraumaSurgeryHealthBasisPoints,
                    PermanentImpairmentSeverityBasisPoints =
                        MilitaryMedicalRules
                            .PermanentImpairmentSeverityBasisPoints,
                    PermanentImpairmentLaborPenaltyBasisPoints =
                        MilitaryMedicalRules
                            .PermanentImpairmentLaborPenaltyBasisPoints
                }
            };
        }
    }

    public static class MilitaryInjuryProfileIds
    {
        public const string SoftTissue =
            "military_injury_profile.soft_tissue";
        public const string Fracture =
            "military_injury_profile.fracture";
        public const string Penetrating =
            "military_injury_profile.penetrating";
    }

    [Serializable]
    public sealed class MilitaryInjuryProfileDefinitionState
    {
        public string Id;
        public string DisplayName;
        public int MinimumAdmissionHealthBasisPoints;
        public int MaximumAdmissionHealthBasisPoints;
        public int SelectionPriority;
        public string SurgicalProcedureId = string.Empty;
    }

    public static class MilitaryInjuryProfileCatalog
    {
        public static List<MilitaryInjuryProfileDefinitionState> CreateCore()
        {
            return new List<MilitaryInjuryProfileDefinitionState>
            {
                new MilitaryInjuryProfileDefinitionState
                {
                    Id = MilitaryInjuryProfileIds.Penetrating,
                    DisplayName = "穿透伤",
                    MinimumAdmissionHealthBasisPoints = 0,
                    MaximumAdmissionHealthBasisPoints = 2_500,
                    SelectionPriority = 100,
                    SurgicalProcedureId = MilitarySurgicalProcedureIds
                        .TraumaDebridementAndReduction
                },
                new MilitaryInjuryProfileDefinitionState
                {
                    Id = MilitaryInjuryProfileIds.Fracture,
                    DisplayName = "骨折伤",
                    MinimumAdmissionHealthBasisPoints = 2_501,
                    MaximumAdmissionHealthBasisPoints = 4_500,
                    SelectionPriority = 100,
                    SurgicalProcedureId = MilitarySurgicalProcedureIds
                        .TraumaDebridementAndReduction
                },
                new MilitaryInjuryProfileDefinitionState
                {
                    Id = MilitaryInjuryProfileIds.SoftTissue,
                    DisplayName = "软组织伤",
                    MinimumAdmissionHealthBasisPoints = 4_501,
                    MaximumAdmissionHealthBasisPoints = 10_000,
                    SelectionPriority = 100,
                    SurgicalProcedureId = string.Empty
                }
            };
        }

        public static MilitaryInjuryProfileDefinitionState Select(
            List<MilitaryInjuryProfileDefinitionState> definitions,
            int admissionHealthBasisPoints)
        {
            MilitaryInjuryProfileDefinitionState selected = null;
            if (definitions != null)
            {
                for (var i = 0; i < definitions.Count; i++)
                {
                    var candidate = definitions[i];
                    if (candidate == null ||
                        admissionHealthBasisPoints <
                            candidate.MinimumAdmissionHealthBasisPoints ||
                        admissionHealthBasisPoints >
                            candidate.MaximumAdmissionHealthBasisPoints)
                    {
                        continue;
                    }
                    if (selected == null ||
                        candidate.SelectionPriority >
                            selected.SelectionPriority ||
                        candidate.SelectionPriority ==
                            selected.SelectionPriority &&
                        string.CompareOrdinal(candidate.Id, selected.Id) < 0)
                    {
                        selected = candidate;
                    }
                }
            }
            if (selected == null)
            {
                throw new InvalidOperationException(
                    $"No injury profile covers admission health " +
                    $"{admissionHealthBasisPoints}.");
            }
            return selected;
        }
    }

    public static class MilitaryFieldHospitalConstructionProfileIds
    {
        public const string TimberLeatherCamp =
            "military_field_hospital_profile.timber_leather_camp";
    }

    public static class MilitaryFieldHospitalMaintenancePolicyIds
    {
        public const string TenDayTimberUpkeep =
            "military_field_hospital_maintenance.ten_day_timber_upkeep";
    }

    public static class MilitaryRearMedicalDischargePolicyIds
    {
        public const string ReturnToSourceArmy =
            "military_rear_discharge.return_to_source_army";
        public const string MedicalRetirementAtCareSite =
            "military_rear_discharge.medical_retirement_at_care_site";
        public const string DeathAtCareSite =
            "military_rear_discharge.death_at_care_site";
        public const string DeathDuringMedicalTransfer =
            "military_rear_discharge.death_during_medical_transfer";
    }

    public static class MilitaryMedicalEvacuationPatientReturnPolicyIds
    {
        public const string ReturnWithTeam =
            "military_medical_patient_return.return_with_team";
        public const string RemainAtCareSiteForMedicalRetirement =
            "military_medical_patient_return.remain_for_medical_retirement";
        public const string RemainAtCareSiteAfterDeath =
            "military_medical_patient_return.remain_after_death";
        public const string ReturnCorpseWithTeam =
            "military_medical_patient_return.return_corpse_with_team";
        public const string CorpseAtArmyAwaitingTeamRejoin =
            "military_medical_patient_return.corpse_at_army_awaiting_team_rejoin";
    }

    public static class MilitaryMedicalEvacuationTransportPolicyIds
    {
        public const string StretcherTeamFoot =
            "military_medical_evacuation_transport.stretcher_team_foot";
    }

    public static class MilitaryMedicalEvacuationTeamRoleIds
    {
        public const string StretcherBearer =
            "military_medical_evacuation_role.stretcher_bearer";
    }

    public static class MilitaryMedicalEvacuationReceptionPolicyIds
    {
        public const string DesignatedPractitionerHandoff =
            "military_medical_evacuation_reception.designated_practitioner_handoff";
    }

    public enum MilitaryMedicalEvacuationStatus : byte
    {
        InTransit,
        AwaitingReception,
        Received,
        Admitted,
        ReadyForReturn,
        ReturningToArmy,
        Completed,
        DeceasedInTransit,
        PatientDeceasedReturningToArmy,
        PatientDeceasedAwaitingTeamRejoin
    }

    [Serializable]
    public sealed class MilitaryMedicalEvacuationTeamMemberState
    {
        public string PersonId;
        public string MilitaryServiceId;
        public string RoleId;
        public string JourneyId;
        public string ReturnJourneyId;
        public string ReturnDeathId = string.Empty;
    }

    [Serializable]
    public sealed class MilitaryMedicalEvacuationState
    {
        public string Id;
        public long CreatedDay;
        public string SourceArmyId;
        public string PatientMilitaryServiceId;
        public string PatientPersonId;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string TransportPolicyId;
        public string ReceptionPolicyId;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string RouteId;
        public string PatientJourneyId;
        public List<MilitaryMedicalEvacuationTeamMemberState> TeamMembers =
            new List<MilitaryMedicalEvacuationTeamMemberState>();
        public string DesignatedReceivingPersonId;
        public MilitaryMedicalEvacuationStatus Status =
            MilitaryMedicalEvacuationStatus.InTransit;
        public long ArrivedDay = -1;
        public string ReceivingPersonId;
        public long ReceivedDay = -1;
        public int ReceivingMedicalSkillBasisPoints;
        public string RearMedicalSiteId;
        public string RearMedicalAdmissionId;
        public string CurrentCareLocationId;
        public string ReturnRouteId;
        public string ReturnDestinationLocationId;
        public string PatientReturnJourneyId;
        public string PatientReturnPolicyId;
        public long ReturnStartedDay = -1;
        public long CompletedDay = -1;
        public string OriginalEvacuationDeathClosureId;
        public string PatientReturnDeathClosureId;
    }

    public enum MilitaryRearMedicalAdmissionStatus : byte
    {
        InTreatment,
        ReadyForReturn,
        Discharged,
        Completed
    }

    [Serializable]
    public sealed class MilitaryRearMedicalSiteState
    {
        public string Id;
        public string KindId;
        public string LocationId;
        public string OwnerOrganizationId;
        public string MedicineInventoryContainerId;
        public int BedCapacity;
        public long RegisteredDay;
        public bool IsOperational = true;
        public string SourceConstructionProjectId;
        public string SupportInventoryContainerId;
        public string MaintenancePolicyId;
        public long LastMaintenanceDay = -1;
        public long NextMaintenanceDay = -1;
    }

    [Serializable]
    public sealed class MilitaryRearMedicalAdmissionState
    {
        public string Id;
        public string EvacuationId;
        public string RearMedicalSiteId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string PhysicianPersonId;
        public long AdmittedDay;
        public MilitaryRearMedicalAdmissionStatus Status =
            MilitaryRearMedicalAdmissionStatus.InTreatment;
        public string TreatmentId;
        public int RequiredTreatmentStages = 1;
        public int CompletedTreatmentStages;
        public List<string> TreatmentIds = new List<string>();
        public List<string> TreatmentPlanProtocolIds = new List<string>();
        public string TreatmentPlanOriginSiteKindId;
        public string InjuryEpisodeId;
        public string MedicalTransferId;
        public string InpatientDeathClosureId;
        public string MedicalTransferDeathClosureId;
        public string PatientReturnDeathClosureId;
        public long ReadyForReturnDay = -1;
        public string DischargePolicyId;
        public long DischargedDay = -1;
        public long CompletedDay = -1;
    }

    public enum MilitaryMedicalTransferStatus : byte
    {
        InTransit,
        AwaitingReception,
        Completed,
        DeceasedInTransit,
        ClosedAfterPatientDeath
    }

    [Serializable]
    public sealed class MilitaryMedicalTransferTeamMemberState
    {
        public string PersonId;
        public string MilitaryServiceId;
        public string JourneyId;
    }

    [Serializable]
    public sealed class MilitaryMedicalTransferState
    {
        public string Id;
        public int SequenceIndex;
        public string PreviousMedicalTransferId;
        public string NextMedicalTransferId;
        public long CreatedDay;
        public string EvacuationId;
        public string AdmissionId;
        public string SourceRearMedicalSiteId;
        public string DestinationRearMedicalSiteId;
        public string SourcePhysicianPersonId;
        public string DesignatedReceivingPersonId;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string RouteId;
        public string PatientJourneyId;
        public List<MilitaryMedicalTransferTeamMemberState> TeamMembers =
            new List<MilitaryMedicalTransferTeamMemberState>();
        public string ReservedMedicineBatchId;
        public int ReservedMedicineUnits;
        public int ConsumedReservedMedicineUnits;
        public int ReleasedReservedMedicineUnits;
        public int CompletedTreatmentStagesAtDispatch;
        public string ReservationInventoryTransactionId;
        public string ReservationReleaseInventoryTransactionId;
        public string DeathClosureId;
        public MilitaryMedicalTransferStatus Status =
            MilitaryMedicalTransferStatus.InTransit;
        public long ArrivedDay = -1;
        public string ReceivingPersonId;
        public int ReceivingMedicalSkillBasisPoints;
        public long ReceivedDay = -1;
        public long ResponsibilityTransferredDay = -1;
    }

    [Serializable]
    public sealed class MilitaryRearMedicalTreatmentState
    {
        public string Id;
        public long Day;
        public string AdmissionId;
        public string EvacuationId;
        public string RearMedicalSiteId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string PhysicianPersonId;
        public string TreatmentProtocolId;
        public string MedicineProductDefinitionId;
        public string SourceMedicineBatchId;
        public string InventoryTransactionId;
        public int MedicineUnitsConsumed;
        public int WorkMinutes;
        public int OpeningHealthBasisPoints;
        public int ClosingHealthBasisPoints;
        public int RecoveredHealthBasisPoints;
        public int PhysicianMedicalSkillBeforeBasisPoints;
        public int PhysicianMedicalSkillAfterBasisPoints;
        public int PhysicianMedicalSkillGainBasisPoints;
        public int StageIndex;
        public int RequiredStageCount = 1;
    }

    public enum MilitaryInfectionStatus : byte
    {
        AtRisk,
        Active,
        Controlled
    }

    [Serializable]
    public sealed class MilitaryInjuryEpisodeState
    {
        public string Id;
        public string EvacuationId;
        public string AdmissionId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string InjuryProfileId;
        public long AssessedDay;
        public int AdmissionHealthBasisPoints;
        public int SeverityBasisPoints;
        public int TransitDays;
        public int ContaminationBasisPoints;
        public int InfectionRiskBasisPoints;
        public MilitaryInfectionStatus InfectionStatus;
        public string InfectionControlTreatmentId;
        public long InfectionControlledDay = -1;
        public string SurgicalProcedureId;
        public string SurgeryTreatmentId = string.Empty;
        public long SurgeryCompletedDay = -1;
        public string PermanentOutcomeId = string.Empty;
        public int LaborCapacityBeforeBasisPoints = -1;
        public int LaborCapacityAfterBasisPoints = -1;
        public int PermanentLaborCapacityPenaltyBasisPoints;
        public bool RequiresMedicalRetirement;
    }

    [Serializable]
    public sealed class MilitaryWoundDeathState
    {
        public string Id;
        public long Day;
        public string PolicyId;
        public string DeathContextId;
        public string InjuryEpisodeId;
        public string AdmissionId;
        public string EvacuationId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string ArmyId;
        public string OrganizationId;
        public string DeathLocationId;
        public int SeverityBasisPoints;
        public int HealthAtDeathBasisPoints;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string FamilyId;
        public string FamilyInheritanceId;
        public string SurvivorCompensationId;
        public string DeathLifeEventId;
        public string SuccessionLifeEventId;
        public string MedicalResponsibilityId;
        public string InpatientDeathClosureId;
        public string MedicalTransferDeathClosureId;
        public string OriginalEvacuationDeathClosureId;
        public string PatientReturnDeathClosureId;
    }

    [Serializable]
    public sealed class MilitaryMedicalDeathResponsibilityState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string DeathContextId;
        public string ResponsibilityPolicyId;
        public string AdmissionId;
        public string EvacuationId;
        public string InjuryEpisodeId;
        public string PatientPersonId;
        public string RearMedicalSiteId;
        public string CareOrganizationId;
        public string SourceArmyId;
        public string ResponsiblePhysicianPersonId;
        public int ResponsiblePhysicianMedicalSkillBasisPoints;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
    }

    [Serializable]
    public sealed class MilitaryInpatientDeathClosureState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string DeteriorationPolicyId;
        public string AdmissionId;
        public string EvacuationId;
        public string InjuryEpisodeId;
        public string PatientPersonId;
        public string RearMedicalSiteId;
        public string PhysicianPersonId;
        public int CompletedTreatmentStagesAtDeath;
        public int RequiredTreatmentStagesAtDeath;
        public string NextTreatmentProtocolId;
        public int OpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int ClosingHealthBasisPoints;
        public string MedicalTransferId;
        public string ReservedMedicineBatchId;
        public int ReservedMedicineUnitsBeforeRelease;
        public int ReleasedReservedMedicineUnits;
        public int ReservedMedicineUnitsAfterRelease;
        public string ReservationReleaseInventoryTransactionId;
    }

    [Serializable]
    public sealed class MilitaryMedicalTransferDeathClosureState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string DeteriorationPolicyId;
        public string MedicalTransferId;
        public string AdmissionId;
        public string EvacuationId;
        public string InjuryEpisodeId;
        public string PatientPersonId;
        public string SourceRearMedicalSiteId;
        public string DestinationRearMedicalSiteId;
        public string SourcePhysicianPersonId;
        public string DesignatedReceivingPersonId;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string RouteId;
        public bool OccurredInTransit;
        public int RemainingKilometersAtDeath;
        public int OpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int ClosingHealthBasisPoints;
        public string ReservedMedicineBatchId;
        public int ReservedMedicineUnitsBeforeRelease;
        public int ReleasedReservedMedicineUnits;
        public int ReservedMedicineUnitsAfterRelease;
        public string ReservationReleaseInventoryTransactionId;
    }

    [Serializable]
    public sealed class MilitaryOriginalEvacuationDeathClosureState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string DeteriorationPolicyId;
        public string EvacuationId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string SourceArmyId;
        public string SourceOrganizationId;
        public string EvacuationAuthorizingPersonId;
        public MilitaryAuthorityLevel EvacuationAuthorizingAuthority;
        public string DeathAuthorizingPersonId;
        public MilitaryAuthorityLevel DeathAuthorizingAuthority;
        public string OriginLocationId;
        public string DestinationLocationId;
        public string DesignatedReceivingPersonId;
        public string RouteId;
        public bool OccurredInTransit;
        public int RemainingKilometersAtDeath;
        public int OpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int ClosingHealthBasisPoints;
        public int DerivedSeverityBasisPoints;
    }

    [Serializable]
    public sealed class MilitaryPatientReturnDeathClosureState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string DeteriorationPolicyId;
        public string AdmissionId;
        public string EvacuationId;
        public string InjuryEpisodeId;
        public string PatientPersonId;
        public string PatientMilitaryServiceId;
        public string SourceArmyId;
        public string SourceRearMedicalSiteId;
        public string SourcePhysicianPersonId;
        public string ReturnRouteId;
        public string ReturnOriginLocationId;
        public string ReturnDestinationLocationId;
        public string PatientReturnJourneyId;
        public long ReturnStartedDay;
        public int RemainingKilometersAtDeath;
        public int OpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int ClosingHealthBasisPoints;
        public bool PatientJourneyCompletedBeforeDeath;
        public List<MilitaryPatientReturnTeamJourneySnapshotState>
            TeamJourneySnapshotsAtDeath =
                new List<MilitaryPatientReturnTeamJourneySnapshotState>();
    }

    [Serializable]
    public sealed class MilitaryPatientReturnTeamJourneySnapshotState
    {
        public string PersonId;
        public string MilitaryServiceId;
        public string ReturnJourneyId;
        public int RemainingKilometersAtDeath;
    }

    [Serializable]
    public sealed class MilitaryReturnTeamDeathState
    {
        public string Id;
        public long Day;
        public string PolicyId;
        public string CorpsePolicyId;
        public string EvacuationId;
        public string PersonId;
        public string MilitaryServiceId;
        public string SourceArmyId;
        public string OrganizationId;
        public string ReturnJourneyId;
        public string ReturnRouteId;
        public string ReturnOriginLocationId;
        public string ReturnDestinationLocationId;
        public long ReturnStartedDay;
        public int RemainingKilometersAtDeath;
        public int OpeningHealthBasisPoints;
        public int HealthLossBasisPoints;
        public int ClosingHealthBasisPoints;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string FamilyId;
        public string FamilyInheritanceId;
        public string SurvivorCompensationId;
        public string DeathLifeEventId;
        public string SuccessionLifeEventId;
        public long CorpseArrivedDay = -1;
    }

    [Serializable]
    public sealed class MilitaryFamilyInheritanceState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string ReturnTeamDeathId = string.Empty;
        public string FamilyId;
        public string DeceasedPersonId;
        public string FormerHeadPersonId;
        public string SuccessorPersonId;
        public bool HeadChanged;
        public long DeceasedWealthBefore;
        public long DeceasedWealthAfter;
        public long FamilyWealthBefore;
        public long FamilyWealthAfter;
    }

    [Serializable]
    public sealed class MilitarySurvivorCompensationState
    {
        public string Id;
        public long Day;
        public string WoundDeathId;
        public string ReturnTeamDeathId = string.Empty;
        public string PolicyId;
        public string ArmyId;
        public string OrganizationId;
        public string FamilyId;
        public string DeceasedPersonId;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public int MilitaryRankAtDeath;
        public long Amount;
        public long OrganizationTreasuryBefore;
        public long OrganizationTreasuryAfter;
        public long FamilyWealthBefore;
        public long FamilyWealthAfter;
    }

    public enum MilitaryFieldHospitalConstructionStatus : byte
    {
        InProgress,
        Completed
    }

    [Serializable]
    public sealed class MilitaryFieldHospitalConstructionProjectState
    {
        public string Id;
        public string ProfileId;
        public string SourceArmyId;
        public string LocationId;
        public string OwnerOrganizationId;
        public string AuthorizingPersonId;
        public MilitaryAuthorityLevel AuthorizingAuthority;
        public string ManagerPersonId;
        public string MaterialInventoryContainerId;
        public int RequiredTimberUnits;
        public int RequiredLeatherUnits;
        public long RequiredMoney;
        public int RequiredLaborDays;
        public int CompletedLaborDays;
        public string InventoryTransactionId;
        public long OwnerTreasuryBefore;
        public long OwnerTreasuryAfter;
        public long StartedDay;
        public MilitaryFieldHospitalConstructionStatus Status =
            MilitaryFieldHospitalConstructionStatus.InProgress;
        public long CompletedDay = -1;
        public string RearMedicalSiteId;
    }

    [Serializable]
    public sealed class MilitaryFieldHospitalConstructionWorkState
    {
        public string Id;
        public string ProjectId;
        public long Day;
        public string WorkerPersonId;
        public int LaborDays;
    }

    [Serializable]
    public sealed class MilitaryFieldHospitalMaintenanceState
    {
        public string Id;
        public string RearMedicalSiteId;
        public long Day;
        public string ManagerPersonId;
        public string SourceTimberBatchId;
        public string InventoryTransactionId;
        public int TimberUnitsConsumed;
        public long MoneyPaid;
        public long OwnerTreasuryBefore;
        public long OwnerTreasuryAfter;
        public long PreviousNextMaintenanceDay;
        public long NewNextMaintenanceDay;
    }

    public enum MilitaryMedicalCaseStatus : byte
    {
        Active,
        Closed
    }

    [Serializable]
    public sealed class MilitaryMedicalCaseState
    {
        public string Id;
        public string ArmyId;
        public string MilitaryServiceId;
        public string PatientPersonId;
        public string PhysicianPersonId;
        public string AuthorizingPersonId;
        public string AuthorizationPolicyId;
        public string TriageId;
        public string TreatmentProtocolId;
        public long DiagnosedDay;
        public MilitaryMedicalCaseStatus Status;
        public long ClosedDay = -1;
        public string ClosureReasonId;
        public string MilitaryMedicalServiceId;
    }

    [Serializable]
    public sealed class MilitaryMedicalServiceState
    {
        public string Id;
        public long Day;
        public string MedicalCaseId;
        public string ArmyId;
        public string MilitaryServiceId;
        public string PatientPersonId;
        public string PhysicianPersonId;
        public string AuthorizingPersonId;
        public string AuthorizationPolicyId;
        public string VenuePolicyId;
        public int WorkMinutes;
        public string MedicineProductDefinitionId;
        public string SourceMedicineBatchId;
        public string InventoryTransactionId;
        public int MedicineUnitsConsumed;
        public int OpeningHealthBasisPoints;
        public int ClosingHealthBasisPoints;
        public int RecoveredHealthBasisPoints;
        public MilitaryServiceStatus OpeningMilitaryStatus;
        public MilitaryServiceStatus ClosingMilitaryStatus;
        public int PhysicianMedicalSkillBeforeBasisPoints;
        public int PhysicianMedicalSkillAfterBasisPoints;
        public int PhysicianMedicalSkillGainBasisPoints;
    }

    [Serializable]
    public sealed class MedicalTreatmentRecordState
    {
        public string Id;
        public long Day;
        public string PhysicianPersonId;
        public string ArmyId;
        public int PatientsTreated;
        public int RecoveredTroops;
        public int HerbsConsumed;
        public string Summary;
    }
}
