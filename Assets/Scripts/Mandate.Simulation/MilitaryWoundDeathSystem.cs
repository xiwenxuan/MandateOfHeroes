using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MilitaryWoundDeathSystem
    {
        private readonly IPersonRepository _people;

        public MilitaryWoundDeathSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public MilitaryWoundDeathState ResolvePostTreatmentDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds.PostReturnMedicalRetirement,
                string.Empty);
        }

        public MilitaryWoundDeathState ResolveReadyForReturnDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds.ReadyForReturnAtCareSite,
                string.Empty);
        }

        public MilitaryWoundDeathState ResolveInTreatmentDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId,
            StableId deteriorationPolicyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds.InTreatmentAtCareSite,
                deteriorationPolicyId.Value);
        }

        public MilitaryWoundDeathState ResolveMedicalTransferDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId,
            StableId deteriorationPolicyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds.DuringCrossFacilityTransfer,
                deteriorationPolicyId.Value);
        }

        public MilitaryWoundDeathState ResolvePatientReturnJourneyDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId,
            StableId deteriorationPolicyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds.DuringPatientReturnJourney,
                deteriorationPolicyId.Value);
        }

        public MilitaryWoundDeathState ResolvePatientArrivalWaitingTeamDeath(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId,
            StableId deteriorationPolicyId)
        {
            return Resolve(
                world,
                admissionId,
                authorizingPersonId,
                policyId,
                MilitaryWoundDeathContextIds
                    .AwaitingReturnTeamRejoinAtArmy,
                deteriorationPolicyId.Value);
        }

        public MilitaryWoundDeathState ResolveOriginalEvacuationDeath(
            WorldState world,
            StableId evacuationId,
            StableId authorizingPersonId,
            StableId policyId,
            StableId deteriorationPolicyId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var evacuation = FindEvacuation(world, evacuationId.Value);
            var service = FindService(
                world, evacuation.PatientMilitaryServiceId);
            var army = FindArmy(world, service.ArmyId);
            var organization = FindOrganization(world, army.OrganizationId);
            var policy = FindPolicy(world, policyId.Value);
            var deteriorationPolicy =
                FindOriginalEvacuationDeteriorationPolicy(
                    world, deteriorationPolicyId.Value);
            var people = PeopleFor(world);
            var patient = people.GetRequired(evacuation.PatientPersonId);
            var family = FindFamily(world, patient.FamilyId);
            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, new StableId(army.Id));
            var occurredInTransit = evacuation.Status ==
                MilitaryMedicalEvacuationStatus.InTransit;
            var patientJourney = FindJourney(
                world, evacuation.PatientJourneyId);
            var closingHealth = Math.Max(
                0,
                patient.HealthBasisPoints -
                    deteriorationPolicy.HealthLossBasisPoints);
            var derivedSeverity = checked(
                10_000 - patient.HealthBasisPoints);
            if (world.AbsoluteDay <
                    world.MilitaryOriginalEvacuationDeathContractActivationDay ||
                evacuation.Status !=
                    MilitaryMedicalEvacuationStatus.InTransit &&
                    evacuation.Status !=
                        MilitaryMedicalEvacuationStatus.AwaitingReception ||
                service.Status != MilitaryServiceStatus.Wounded ||
                !patient.IsAlive ||
                !string.IsNullOrEmpty(
                    evacuation.OriginalEvacuationDeathClosureId) ||
                !string.IsNullOrEmpty(evacuation.ReceivingPersonId) ||
                evacuation.ReceivedDay != -1 ||
                evacuation.ReceivingMedicalSkillBasisPoints != 0 ||
                !string.IsNullOrEmpty(evacuation.RearMedicalSiteId) ||
                !string.IsNullOrEmpty(evacuation.RearMedicalAdmissionId) ||
                occurredInTransit &&
                    (patientJourney == null ||
                     patientJourney.RemainingKilometers <= 0) ||
                !occurredInTransit && patientJourney != null ||
                patient.HealthBasisPoints < 0 ||
                patient.HealthBasisPoints >
                    deteriorationPolicy.MaximumOpeningHealthBasisPoints ||
                closingHealth >
                    deteriorationPolicy.MaximumClosingHealthBasisPoints ||
                closingHealth >
                    policy.MaximumPostTreatmentHealthBasisPoints ||
                derivedSeverity < policy.MinimumSeverityBasisPoints ||
                world.AbsoluteDay < checked(
                    evacuation.CreatedDay +
                    deteriorationPolicy.MinimumDaysAfterDispatch) ||
                authority < MilitaryAuthorityLevel.Army ||
                !family.MemberIds.Contains(patient.Id) ||
                HasWoundDeath(world, string.Empty, patient.Id))
            {
                throw new InvalidOperationException(
                    "The patient is not eligible for original-evacuation death.");
            }

            var formerHead = family.HeadPersonId;
            var headChanged = formerHead == patient.Id;
            var successor = headChanged
                ? SelectSuccessor(world, family, patient.Id, people)
                : people.GetRequired(formerHead);
            if (successor == null || !successor.IsAlive)
            {
                throw new InvalidOperationException(
                    "The family has no living successor for this death.");
            }

            var compensationAmount = checked(
                policy.BaseCompensationMoney +
                policy.CompensationPerRankMoney * service.Rank);
            if (compensationAmount < 0 ||
                organization.Treasury < compensationAmount ||
                patient.Wealth < 0)
            {
                throw new InvalidOperationException(
                    "The organization cannot fund the survivor compensation.");
            }

            var familyAfterInheritance = checked(
                family.Wealth + patient.Wealth);
            var familyAfterCompensation = checked(
                familyAfterInheritance + compensationAmount);
            var organizationAfter = checked(
                organization.Treasury - compensationAmount);
            var index = world.MilitaryWoundDeaths.Count;
            var deathId = $"military_wound_death.{world.AbsoluteDay}." +
                $"{index:D6}";
            var inheritanceId = $"military_family_inheritance." +
                $"{world.AbsoluteDay}.{index:D6}";
            var compensationId = $"military_survivor_compensation." +
                $"{world.AbsoluteDay}.{index:D6}";
            var responsibilityId =
                $"military_medical_death_responsibility." +
                $"{world.AbsoluteDay}.{index:D6}";
            var closureId =
                $"military_original_evacuation_death_closure." +
                $"{world.AbsoluteDay}.{index:D6}";
            var deathLifeEventId = $"life_event.{deathId}.death";
            var successionLifeEventId = headChanged
                ? $"life_event.{deathId}.succession"
                : string.Empty;
            var inheritance = new MilitaryFamilyInheritanceState
            {
                Id = inheritanceId,
                Day = world.AbsoluteDay,
                WoundDeathId = deathId,
                FamilyId = family.Id,
                DeceasedPersonId = patient.Id,
                FormerHeadPersonId = formerHead,
                SuccessorPersonId = successor.Id,
                HeadChanged = headChanged,
                DeceasedWealthBefore = patient.Wealth,
                DeceasedWealthAfter = 0,
                FamilyWealthBefore = family.Wealth,
                FamilyWealthAfter = familyAfterInheritance
            };
            var compensation = new MilitarySurvivorCompensationState
            {
                Id = compensationId,
                Day = world.AbsoluteDay,
                WoundDeathId = deathId,
                PolicyId = policy.Id,
                ArmyId = army.Id,
                OrganizationId = organization.Id,
                FamilyId = family.Id,
                DeceasedPersonId = patient.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                MilitaryRankAtDeath = service.Rank,
                Amount = compensationAmount,
                OrganizationTreasuryBefore = organization.Treasury,
                OrganizationTreasuryAfter = organizationAfter,
                FamilyWealthBefore = familyAfterInheritance,
                FamilyWealthAfter = familyAfterCompensation
            };
            var death = new MilitaryWoundDeathState
            {
                Id = deathId,
                Day = world.AbsoluteDay,
                PolicyId = policy.Id,
                DeathContextId =
                    MilitaryWoundDeathContextIds.DuringOriginalEvacuation,
                InjuryEpisodeId = string.Empty,
                AdmissionId = string.Empty,
                EvacuationId = evacuation.Id,
                PatientPersonId = patient.Id,
                PatientMilitaryServiceId = service.Id,
                ArmyId = army.Id,
                OrganizationId = organization.Id,
                DeathLocationId = occurredInTransit
                    ? string.Empty
                    : evacuation.DestinationLocationId,
                SeverityBasisPoints = derivedSeverity,
                HealthAtDeathBasisPoints = closingHealth,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                FamilyId = family.Id,
                FamilyInheritanceId = inheritance.Id,
                SurvivorCompensationId = compensation.Id,
                DeathLifeEventId = deathLifeEventId,
                SuccessionLifeEventId = successionLifeEventId,
                MedicalResponsibilityId = responsibilityId,
                InpatientDeathClosureId = string.Empty,
                MedicalTransferDeathClosureId = string.Empty,
                OriginalEvacuationDeathClosureId = closureId
            };
            var responsibility = new MilitaryMedicalDeathResponsibilityState
            {
                Id = responsibilityId,
                Day = world.AbsoluteDay,
                WoundDeathId = death.Id,
                DeathContextId = death.DeathContextId,
                ResponsibilityPolicyId =
                    MilitaryMedicalDeathResponsibilityPolicyIds
                        .SourceArmyUntilRearHandoff,
                AdmissionId = string.Empty,
                EvacuationId = evacuation.Id,
                InjuryEpisodeId = string.Empty,
                PatientPersonId = patient.Id,
                RearMedicalSiteId = string.Empty,
                CareOrganizationId = organization.Id,
                SourceArmyId = army.Id,
                ResponsiblePhysicianPersonId = string.Empty,
                ResponsiblePhysicianMedicalSkillBasisPoints = 0,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority
            };
            var closure = new MilitaryOriginalEvacuationDeathClosureState
            {
                Id = closureId,
                Day = world.AbsoluteDay,
                WoundDeathId = death.Id,
                DeteriorationPolicyId = deteriorationPolicy.Id,
                EvacuationId = evacuation.Id,
                PatientPersonId = patient.Id,
                PatientMilitaryServiceId = service.Id,
                SourceArmyId = army.Id,
                SourceOrganizationId = organization.Id,
                EvacuationAuthorizingPersonId =
                    evacuation.AuthorizingPersonId,
                EvacuationAuthorizingAuthority =
                    evacuation.AuthorizingAuthority,
                DeathAuthorizingPersonId = authorizingPersonId.Value,
                DeathAuthorizingAuthority = authority,
                OriginLocationId = evacuation.OriginLocationId,
                DestinationLocationId = evacuation.DestinationLocationId,
                DesignatedReceivingPersonId =
                    evacuation.DesignatedReceivingPersonId,
                RouteId = evacuation.RouteId,
                OccurredInTransit = occurredInTransit,
                RemainingKilometersAtDeath = occurredInTransit
                    ? patientJourney.RemainingKilometers
                    : 0,
                OpeningHealthBasisPoints = patient.HealthBasisPoints,
                HealthLossBasisPoints =
                    deteriorationPolicy.HealthLossBasisPoints,
                ClosingHealthBasisPoints = closingHealth,
                DerivedSeverityBasisPoints = derivedSeverity
            };

            var writablePatient = people.GetRequiredForUpdate(patient.Id);
            writablePatient.HealthBasisPoints = closingHealth;
            writablePatient.Wealth = 0;
            family.Wealth = familyAfterCompensation;
            if (headChanged)
            {
                family.HeadPersonId = successor.Id;
            }
            organization.Treasury = organizationAfter;
            evacuation.OriginalEvacuationDeathClosureId = closure.Id;
            evacuation.PatientReturnPolicyId =
                MilitaryMedicalEvacuationPatientReturnPolicyIds
                    .RemainAtCareSiteAfterDeath;
            evacuation.Status = occurredInTransit
                ? MilitaryMedicalEvacuationStatus.DeceasedInTransit
                : MilitaryMedicalEvacuationStatus.ReadyForReturn;
            world.MilitaryMedicalDeathResponsibilities.Add(responsibility);
            world.MilitaryOriginalEvacuationDeathClosures.Add(closure);
            world.MilitaryFamilyInheritances.Add(inheritance);
            world.MilitarySurvivorCompensations.Add(compensation);
            world.MilitaryWoundDeaths.Add(death);
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = deathLifeEventId,
                Type = LifeEventType.Death,
                Day = world.AbsoluteDay,
                PrimaryPersonId = patient.Id,
                SecondaryPersonId = string.Empty,
                FamilyId = family.Id,
                Summary = $"{patient.DisplayName}在战场后送途中因重伤恶化去世。"
            });
            if (headChanged)
            {
                world.LifeEvents.Add(new LifeEventRecordState
                {
                    Id = successionLifeEventId,
                    Type = LifeEventType.Succession,
                    Day = world.AbsoluteDay,
                    PrimaryPersonId = successor.Id,
                    SecondaryPersonId = patient.Id,
                    FamilyId = family.Id,
                    Summary = $"{successor.DisplayName}继任{family.DisplayName}家主。"
                });
            }

            new PopulationLedgerSystem(people).RecordDeaths(
                world, new[] { writablePatient }, false);
            return death;
        }

        private MilitaryWoundDeathState Resolve(
            WorldState world,
            StableId admissionId,
            StableId authorizingPersonId,
            StableId policyId,
            string deathContextId,
            string deteriorationPolicyId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            world.Validate();
            var admission = FindAdmission(world, admissionId.Value);
            var evacuation = FindEvacuation(world, admission.EvacuationId);
            var injury = FindInjury(world, admission.InjuryEpisodeId);
            var service = FindService(
                world, admission.PatientMilitaryServiceId);
            var army = FindArmy(world, service.ArmyId);
            var organization = FindOrganization(
                world, army.OrganizationId);
            var policy = FindPolicy(world, policyId.Value);
            var people = PeopleFor(world);
            var patient = people.GetRequired(admission.PatientPersonId);
            var family = FindFamily(world, patient.FamilyId);
            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, new StableId(army.Id));
            var atCareSiteBeforeReturn = deathContextId ==
                MilitaryWoundDeathContextIds.ReadyForReturnAtCareSite;
            var postReturn = deathContextId ==
                MilitaryWoundDeathContextIds.PostReturnMedicalRetirement;
            var inTreatment = deathContextId ==
                MilitaryWoundDeathContextIds.InTreatmentAtCareSite;
            var duringTransfer = deathContextId ==
                MilitaryWoundDeathContextIds.DuringCrossFacilityTransfer;
            var duringPatientReturn = deathContextId ==
                    MilitaryWoundDeathContextIds.DuringPatientReturnJourney ||
                deathContextId == MilitaryWoundDeathContextIds
                    .AwaitingReturnTeamRejoinAtArmy;
            var awaitingReturnTeam = deathContextId ==
                MilitaryWoundDeathContextIds
                    .AwaitingReturnTeamRejoinAtArmy;
            var careSiteDeath = atCareSiteBeforeReturn || inTreatment;
            var deteriorationPolicy = inTreatment || duringTransfer
                ? FindDeteriorationPolicy(world, deteriorationPolicyId)
                : null;
            var returnDeteriorationPolicy = duringPatientReturn
                ? FindPatientReturnDeteriorationPolicy(
                    world, deteriorationPolicyId)
                : null;
            var medicalTransfer = inTreatment || duringTransfer
                ? FindMedicalTransfer(world, admission.MedicalTransferId)
                : null;
            MilitaryRearMedicalSiteState responsibilitySite = null;
            PersonState responsiblePhysician = null;
            if (careSiteDeath)
            {
                responsibilitySite = FindRearMedicalSite(
                    world, admission.RearMedicalSiteId);
                responsiblePhysician = people.GetRequired(
                    admission.PhysicianPersonId);
            }
            else if (duringTransfer && medicalTransfer != null)
            {
                responsibilitySite = FindRearMedicalSite(
                    world, medicalTransfer.SourceRearMedicalSiteId);
                responsiblePhysician = people.GetRequired(
                    medicalTransfer.SourcePhysicianPersonId);
            }
            else if (duringPatientReturn)
            {
                responsibilitySite = FindRearMedicalSite(
                    world, admission.RearMedicalSiteId);
                responsiblePhysician = people.GetRequired(
                    admission.PhysicianPersonId);
            }
            var validPhase = atCareSiteBeforeReturn
                ? admission.Status ==
                      MilitaryRearMedicalAdmissionStatus.ReadyForReturn &&
                  evacuation.Status ==
                      MilitaryMedicalEvacuationStatus.ReadyForReturn &&
                  service.Status == MilitaryServiceStatus.Wounded
                : inTreatment
                    ? admission.Status ==
                          MilitaryRearMedicalAdmissionStatus.InTreatment &&
                      evacuation.Status ==
                          MilitaryMedicalEvacuationStatus.Admitted &&
                      service.Status == MilitaryServiceStatus.Wounded &&
                      admission.CompletedTreatmentStages <
                          admission.RequiredTreatmentStages &&
                      (medicalTransfer == null ||
                       medicalTransfer.Status ==
                          MilitaryMedicalTransferStatus.Completed)
                    : duringTransfer
                        ? admission.Status ==
                              MilitaryRearMedicalAdmissionStatus.InTreatment &&
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus.Admitted &&
                          service.Status == MilitaryServiceStatus.Wounded &&
                          medicalTransfer != null &&
                          admission.CompletedTreatmentStages ==
                              medicalTransfer
                                  .CompletedTreatmentStagesAtDispatch &&
                          (medicalTransfer.Status ==
                               MilitaryMedicalTransferStatus.InTransit ||
                           medicalTransfer.Status ==
                               MilitaryMedicalTransferStatus
                                   .AwaitingReception)
                    : duringPatientReturn
                        ? admission.Status ==
                              MilitaryRearMedicalAdmissionStatus.Discharged &&
                          evacuation.Status ==
                              MilitaryMedicalEvacuationStatus
                                  .ReturningToArmy &&
                          service.Status == MilitaryServiceStatus.Wounded &&
                           evacuation.PatientReturnPolicyId ==
                               MilitaryMedicalEvacuationPatientReturnPolicyIds
                                   .ReturnWithTeam &&
                           (awaitingReturnTeam
                               ? FindJourney(
                                     world,
                                     evacuation.PatientReturnJourneyId) == null &&
                                 patient.LocationId ==
                                     evacuation.ReturnDestinationLocationId &&
                                 HasOutstandingReturnTeamJourney(
                                     world, evacuation)
                               : FindJourney(
                                     world,
                                     evacuation.PatientReturnJourneyId) != null)
                    : postReturn &&
                      admission.Status ==
                          MilitaryRearMedicalAdmissionStatus.Completed &&
                      evacuation.Status ==
                          MilitaryMedicalEvacuationStatus.Completed &&
                      service.Status == MilitaryServiceStatus.Retired;
            var waitingPeriodStartDay = atCareSiteBeforeReturn
                ? admission.ReadyForReturnDay
                : inTreatment
                    ? admission.AdmittedDay
                    : duringTransfer
                        ? admission.AdmittedDay
                    : duringPatientReturn
                        ? evacuation.ReturnStartedDay
                    : admission.CompletedDay;
            var minimumWaitingDays = duringPatientReturn
                ? returnDeteriorationPolicy.MinimumDaysAfterReturnStart
                : inTreatment || duringTransfer
                    ? deteriorationPolicy.MinimumDaysAfterAdmission
                    : policy.MinimumDaysAfterCareCompletion;
            var closingHealth = duringPatientReturn
                ? Math.Max(
                    0,
                    patient.HealthBasisPoints -
                        returnDeteriorationPolicy.HealthLossBasisPoints)
                : inTreatment || duringTransfer
                    ? Math.Max(
                        0,
                        patient.HealthBasisPoints -
                        deteriorationPolicy.HealthLossBasisPoints)
                    : patient.HealthBasisPoints;

            if (world.AbsoluteDay <
                    world.MilitaryWoundDeathContractActivationDay ||
                careSiteDeath && world.AbsoluteDay <
                    world.MilitaryMedicalDeathResponsibilityContractActivationDay ||
                inTreatment && world.AbsoluteDay <
                    world.MilitaryInpatientDeathContractActivationDay ||
                duringTransfer && world.AbsoluteDay <
                    world.MilitaryMedicalTransferDeathContractActivationDay ||
                duringPatientReturn && world.AbsoluteDay <
                    (awaitingReturnTeam
                        ? world
                            .MilitaryPatientArrivalWaitingTeamDeathContractActivationDay
                        : world.MilitaryPatientReturnDeathContractActivationDay) ||
                !validPhase ||
                careSiteDeath &&
                    (!responsibilitySite.IsOperational ||
                     responsibilitySite.LocationId != patient.LocationId ||
                     responsibilitySite.OwnerOrganizationId !=
                        organization.Id ||
                     !responsiblePhysician.IsAlive ||
                     responsiblePhysician.LocationId != patient.LocationId) ||
                duringTransfer &&
                    (responsibilitySite.OwnerOrganizationId !=
                         organization.Id ||
                     !responsiblePhysician.IsAlive ||
                     responsiblePhysician.LocationId !=
                         responsibilitySite.LocationId ||
                     medicalTransfer.Status ==
                         MilitaryMedicalTransferStatus.InTransit &&
                         patient.LocationId !=
                             responsibilitySite.LocationId ||
                     medicalTransfer.Status ==
                         MilitaryMedicalTransferStatus.AwaitingReception &&
                         patient.LocationId != FindRearMedicalSite(
                             world,
                             medicalTransfer.DestinationRearMedicalSiteId)
                             .LocationId) ||
                duringPatientReturn &&
                    (string.IsNullOrEmpty(
                         evacuation.PatientReturnJourneyId) ||
                     (awaitingReturnTeam
                         ? FindJourney(
                               world,
                               evacuation.PatientReturnJourneyId) != null ||
                           patient.LocationId !=
                               evacuation.ReturnDestinationLocationId ||
                           !HasOutstandingReturnTeamJourney(
                               world, evacuation)
                         : FindJourney(
                               world,
                               evacuation.PatientReturnJourneyId) == null ||
                           FindJourney(
                               world,
                               evacuation.PatientReturnJourneyId)
                               .RemainingKilometers <= 0)) ||
                !inTreatment && !duringTransfer && !duringPatientReturn &&
                    !injury.RequiresMedicalRetirement ||
                !patient.IsAlive ||
                !duringTransfer && !duringPatientReturn &&
                    patient.LocationId != evacuation.CurrentCareLocationId ||
                injury.SeverityBasisPoints < policy.MinimumSeverityBasisPoints ||
                (inTreatment || duringTransfer) &&
                    injury.SeverityBasisPoints <
                        deteriorationPolicy.MinimumSeverityBasisPoints ||
                duringPatientReturn &&
                    injury.SeverityBasisPoints <
                        returnDeteriorationPolicy
                            .MinimumSeverityBasisPoints ||
                closingHealth >
                    policy.MaximumPostTreatmentHealthBasisPoints ||
                (inTreatment || duringTransfer) && closingHealth >
                    deteriorationPolicy.MaximumClosingHealthBasisPoints ||
                duringPatientReturn && closingHealth >
                    returnDeteriorationPolicy
                        .MaximumClosingHealthBasisPoints ||
                world.AbsoluteDay < checked(
                    waitingPeriodStartDay +
                    minimumWaitingDays) ||
                authority < MilitaryAuthorityLevel.Army ||
                !family.MemberIds.Contains(patient.Id) ||
                HasWoundDeath(world, injury.Id, patient.Id))
            {
                throw new InvalidOperationException(
                    "The patient is not eligible for this wound-death " +
                    $"context: context={deathContextId}, phase={validPhase}, " +
                    $"day={world.AbsoluteDay}, start={waitingPeriodStartDay}, " +
                    $"minimum={minimumWaitingDays}, health=" +
                    $"{patient.HealthBasisPoints}->{closingHealth}, severity=" +
                    $"{injury.SeverityBasisPoints}, journey=" +
                    $"{FindJourney(world, evacuation.PatientReturnJourneyId)?.RemainingKilometers}.");
            }

            var formerHead = family.HeadPersonId;
            var headChanged = formerHead == patient.Id;
            var successor = headChanged
                ? SelectSuccessor(world, family, patient.Id, people)
                : people.GetRequired(formerHead);
            if (successor == null || !successor.IsAlive)
            {
                throw new InvalidOperationException(
                    "The family has no living successor for this death.");
            }

            var compensationAmount = checked(
                policy.BaseCompensationMoney +
                policy.CompensationPerRankMoney * service.Rank);
            if (compensationAmount < 0 ||
                organization.Treasury < compensationAmount ||
                patient.Wealth < 0)
            {
                throw new InvalidOperationException(
                    "The organization cannot fund the survivor compensation.");
            }

            ProductBatchState reservedMedicineBatch = null;
            var releasedReservedMedicineUnits = 0;
            var reservedMedicineUnitsBeforeRelease = 0;
            if ((inTreatment || duringTransfer) && medicalTransfer != null)
            {
                releasedReservedMedicineUnits = checked(
                    medicalTransfer.ReservedMedicineUnits -
                    medicalTransfer.ConsumedReservedMedicineUnits -
                    medicalTransfer.ReleasedReservedMedicineUnits);
                reservedMedicineBatch = FindProductBatch(
                    world, medicalTransfer.ReservedMedicineBatchId);
                reservedMedicineUnitsBeforeRelease = checked(
                    (int)reservedMedicineBatch.ReservedQuantity);
                if (releasedReservedMedicineUnits < 0 ||
                    reservedMedicineBatch.ReservedQuantity <
                        releasedReservedMedicineUnits)
                {
                    throw new InvalidOperationException(
                        "The transferred patient's unused medicine reservation is inconsistent.");
                }
            }

            var familyAfterInheritance = checked(
                family.Wealth + patient.Wealth);
            var familyAfterCompensation = checked(
                familyAfterInheritance + compensationAmount);
            var organizationAfter = checked(
                organization.Treasury - compensationAmount);
            var index = world.MilitaryWoundDeaths.Count;
            var deathId = $"military_wound_death.{world.AbsoluteDay}." +
                $"{index:D6}";
            var inheritanceId = $"military_family_inheritance." +
                $"{world.AbsoluteDay}.{index:D6}";
            var compensationId = $"military_survivor_compensation." +
                $"{world.AbsoluteDay}.{index:D6}";
            var deathLifeEventId = $"life_event.{deathId}.death";
            var successionLifeEventId = headChanged
                ? $"life_event.{deathId}.succession"
                : string.Empty;
            var responsibilityId = atCareSiteBeforeReturn
                || inTreatment || duringTransfer || duringPatientReturn
                ? $"military_medical_death_responsibility." +
                  $"{world.AbsoluteDay}.{index:D6}"
                : string.Empty;
            var inpatientClosureId = inTreatment
                ? $"military_inpatient_death_closure." +
                  $"{world.AbsoluteDay}.{index:D6}"
                : string.Empty;
            var transferDeathClosureId = duringTransfer
                ? $"military_medical_transfer_death_closure." +
                  $"{world.AbsoluteDay}.{index:D6}"
                : string.Empty;
            var patientReturnDeathClosureId = duringPatientReturn
                ? $"military_patient_return_death_closure." +
                  $"{world.AbsoluteDay}.{index:D6}"
                : string.Empty;
            InventoryTransactionState reservationReleaseTransaction = null;
            if (releasedReservedMedicineUnits > 0)
            {
                reservationReleaseTransaction =
                    ProductInventorySystem.NewTransaction(
                        world,
                        InventoryTransactionType
                            .MilitaryMedicalTransferMedicineReleased,
                        responsiblePhysician.Id,
                        string.Empty,
                        0,
                        0,
                        0,
                        $"Released unused inpatient medicine for {deathId}.");
                reservationReleaseTransaction.SourceMilitaryMedicalTransferId =
                    medicalTransfer.Id;
                reservationReleaseTransaction.Lines.Add(
                    ProductInventorySystem.Line(
                        reservedMedicineBatch,
                        0,
                        -releasedReservedMedicineUnits));
            }

            var inheritance = new MilitaryFamilyInheritanceState
            {
                Id = inheritanceId,
                Day = world.AbsoluteDay,
                WoundDeathId = deathId,
                FamilyId = family.Id,
                DeceasedPersonId = patient.Id,
                FormerHeadPersonId = formerHead,
                SuccessorPersonId = successor.Id,
                HeadChanged = headChanged,
                DeceasedWealthBefore = patient.Wealth,
                DeceasedWealthAfter = 0,
                FamilyWealthBefore = family.Wealth,
                FamilyWealthAfter = familyAfterInheritance
            };
            var compensation = new MilitarySurvivorCompensationState
            {
                Id = compensationId,
                Day = world.AbsoluteDay,
                WoundDeathId = deathId,
                PolicyId = policy.Id,
                ArmyId = army.Id,
                OrganizationId = organization.Id,
                FamilyId = family.Id,
                DeceasedPersonId = patient.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                MilitaryRankAtDeath = service.Rank,
                Amount = compensationAmount,
                OrganizationTreasuryBefore = organization.Treasury,
                OrganizationTreasuryAfter = organizationAfter,
                FamilyWealthBefore = familyAfterInheritance,
                FamilyWealthAfter = familyAfterCompensation
            };
            var death = new MilitaryWoundDeathState
            {
                Id = deathId,
                Day = world.AbsoluteDay,
                PolicyId = policy.Id,
                DeathContextId = deathContextId,
                InjuryEpisodeId = injury.Id,
                AdmissionId = admission.Id,
                EvacuationId = evacuation.Id,
                PatientPersonId = patient.Id,
                PatientMilitaryServiceId = service.Id,
                ArmyId = army.Id,
                OrganizationId = organization.Id,
                DeathLocationId = duringPatientReturn && !awaitingReturnTeam ||
                    duringTransfer && medicalTransfer.Status ==
                        MilitaryMedicalTransferStatus.InTransit
                        ? string.Empty
                        : patient.LocationId,
                SeverityBasisPoints = injury.SeverityBasisPoints,
                HealthAtDeathBasisPoints = closingHealth,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                FamilyId = family.Id,
                FamilyInheritanceId = inheritance.Id,
                SurvivorCompensationId = compensation.Id,
                DeathLifeEventId = deathLifeEventId,
                SuccessionLifeEventId = successionLifeEventId,
                MedicalResponsibilityId = responsibilityId,
                InpatientDeathClosureId = inpatientClosureId,
                MedicalTransferDeathClosureId = transferDeathClosureId,
                OriginalEvacuationDeathClosureId = string.Empty,
                PatientReturnDeathClosureId = patientReturnDeathClosureId
            };
            MilitaryMedicalDeathResponsibilityState responsibility = null;
            if (careSiteDeath || duringTransfer || duringPatientReturn)
            {
                responsibility = new MilitaryMedicalDeathResponsibilityState
                {
                    Id = responsibilityId,
                    Day = world.AbsoluteDay,
                    WoundDeathId = death.Id,
                    DeathContextId = deathContextId,
                    ResponsibilityPolicyId =
                        duringTransfer
                            ? MilitaryMedicalDeathResponsibilityPolicyIds
                                .SourceCareUntilTransferHandoff
                            : duringPatientReturn
                                ? MilitaryMedicalDeathResponsibilityPolicyIds
                                    .LastCareTeamDuringAuthorizedReturn
                            : MilitaryMedicalDeathResponsibilityPolicyIds
                                .CurrentCareTeamDocumented,
                    AdmissionId = admission.Id,
                    EvacuationId = evacuation.Id,
                    InjuryEpisodeId = injury.Id,
                    PatientPersonId = patient.Id,
                    RearMedicalSiteId = responsibilitySite.Id,
                    CareOrganizationId =
                        responsibilitySite.OwnerOrganizationId,
                    SourceArmyId = string.Empty,
                    ResponsiblePhysicianPersonId = responsiblePhysician.Id,
                    ResponsiblePhysicianMedicalSkillBasisPoints = Math.Max(
                        responsiblePhysician.MedicalSkillBasisPoints,
                        responsiblePhysician.ProfessionalSkills?.Medicine ?? 0),
                    AuthorizingPersonId = authorizingPersonId.Value,
                    AuthorizingAuthority = authority
                };
            }
            MilitaryInpatientDeathClosureState inpatientClosure = null;
            if (inTreatment)
            {
                inpatientClosure = new MilitaryInpatientDeathClosureState
                {
                    Id = inpatientClosureId,
                    Day = world.AbsoluteDay,
                    WoundDeathId = death.Id,
                    DeteriorationPolicyId = deteriorationPolicy.Id,
                    AdmissionId = admission.Id,
                    EvacuationId = evacuation.Id,
                    InjuryEpisodeId = injury.Id,
                    PatientPersonId = patient.Id,
                    RearMedicalSiteId = responsibilitySite.Id,
                    PhysicianPersonId = responsiblePhysician.Id,
                    CompletedTreatmentStagesAtDeath =
                        admission.CompletedTreatmentStages,
                    RequiredTreatmentStagesAtDeath =
                        admission.RequiredTreatmentStages,
                    NextTreatmentProtocolId =
                        admission.TreatmentPlanProtocolIds[
                            admission.CompletedTreatmentStages],
                    OpeningHealthBasisPoints = patient.HealthBasisPoints,
                    HealthLossBasisPoints =
                        deteriorationPolicy.HealthLossBasisPoints,
                    ClosingHealthBasisPoints = closingHealth,
                    MedicalTransferId = medicalTransfer?.Id ?? string.Empty,
                    ReservedMedicineBatchId =
                        reservedMedicineBatch?.Id ?? string.Empty,
                    ReservedMedicineUnitsBeforeRelease =
                        reservedMedicineUnitsBeforeRelease,
                    ReleasedReservedMedicineUnits =
                        releasedReservedMedicineUnits,
                    ReservedMedicineUnitsAfterRelease = checked(
                        reservedMedicineUnitsBeforeRelease -
                        releasedReservedMedicineUnits),
                    ReservationReleaseInventoryTransactionId =
                        reservationReleaseTransaction?.Id ?? string.Empty
                };
            }
            MilitaryMedicalTransferDeathClosureState transferDeathClosure =
                null;
            if (duringTransfer)
            {
                var patientJourney = FindJourney(
                    world, medicalTransfer.PatientJourneyId);
                var occurredInTransit = medicalTransfer.Status ==
                    MilitaryMedicalTransferStatus.InTransit;
                if (occurredInTransit && patientJourney == null)
                {
                    throw new InvalidOperationException(
                        "An in-transit medical death requires the patient journey.");
                }
                transferDeathClosure =
                    new MilitaryMedicalTransferDeathClosureState
                    {
                        Id = transferDeathClosureId,
                        Day = world.AbsoluteDay,
                        WoundDeathId = death.Id,
                        DeteriorationPolicyId = deteriorationPolicy.Id,
                        MedicalTransferId = medicalTransfer.Id,
                        AdmissionId = admission.Id,
                        EvacuationId = evacuation.Id,
                        InjuryEpisodeId = injury.Id,
                        PatientPersonId = patient.Id,
                        SourceRearMedicalSiteId =
                            medicalTransfer.SourceRearMedicalSiteId,
                        DestinationRearMedicalSiteId =
                            medicalTransfer.DestinationRearMedicalSiteId,
                        SourcePhysicianPersonId =
                            medicalTransfer.SourcePhysicianPersonId,
                        DesignatedReceivingPersonId =
                            medicalTransfer.DesignatedReceivingPersonId,
                        AuthorizingPersonId = authorizingPersonId.Value,
                        AuthorizingAuthority = authority,
                        RouteId = medicalTransfer.RouteId,
                        OccurredInTransit = occurredInTransit,
                        RemainingKilometersAtDeath = occurredInTransit
                            ? patientJourney.RemainingKilometers
                            : 0,
                        OpeningHealthBasisPoints = patient.HealthBasisPoints,
                        HealthLossBasisPoints =
                            deteriorationPolicy.HealthLossBasisPoints,
                        ClosingHealthBasisPoints = closingHealth,
                        ReservedMedicineBatchId = reservedMedicineBatch.Id,
                        ReservedMedicineUnitsBeforeRelease =
                            reservedMedicineUnitsBeforeRelease,
                        ReleasedReservedMedicineUnits =
                            releasedReservedMedicineUnits,
                        ReservedMedicineUnitsAfterRelease = checked(
                            reservedMedicineUnitsBeforeRelease -
                            releasedReservedMedicineUnits),
                        ReservationReleaseInventoryTransactionId =
                            reservationReleaseTransaction?.Id ?? string.Empty
                    };
            }
            MilitaryPatientReturnDeathClosureState patientReturnDeathClosure =
                null;
            if (duringPatientReturn)
            {
                var patientReturnJourney = FindJourney(
                    world, evacuation.PatientReturnJourneyId);
                patientReturnDeathClosure =
                    new MilitaryPatientReturnDeathClosureState
                    {
                        Id = patientReturnDeathClosureId,
                        Day = world.AbsoluteDay,
                        WoundDeathId = death.Id,
                        DeteriorationPolicyId =
                            returnDeteriorationPolicy.Id,
                        AdmissionId = admission.Id,
                        EvacuationId = evacuation.Id,
                        InjuryEpisodeId = injury.Id,
                        PatientPersonId = patient.Id,
                        PatientMilitaryServiceId = service.Id,
                        SourceArmyId = army.Id,
                        SourceRearMedicalSiteId = responsibilitySite.Id,
                        SourcePhysicianPersonId = responsiblePhysician.Id,
                        ReturnRouteId = evacuation.ReturnRouteId,
                        ReturnOriginLocationId =
                            evacuation.CurrentCareLocationId,
                        ReturnDestinationLocationId =
                            evacuation.ReturnDestinationLocationId,
                        PatientReturnJourneyId =
                            evacuation.PatientReturnJourneyId,
                        ReturnStartedDay = evacuation.ReturnStartedDay,
                        RemainingKilometersAtDeath = awaitingReturnTeam
                            ? 0
                            : patientReturnJourney.RemainingKilometers,
                        OpeningHealthBasisPoints = patient.HealthBasisPoints,
                        HealthLossBasisPoints =
                            returnDeteriorationPolicy.HealthLossBasisPoints,
                        ClosingHealthBasisPoints = closingHealth,
                        PatientJourneyCompletedBeforeDeath =
                            awaitingReturnTeam,
                        TeamJourneySnapshotsAtDeath = awaitingReturnTeam
                            ? BuildReturnTeamJourneySnapshots(
                                world, evacuation)
                            : new List<
                                MilitaryPatientReturnTeamJourneySnapshotState>()
                    };
            }

            var writablePatient = people.GetRequiredForUpdate(patient.Id);
            writablePatient.HealthBasisPoints = closingHealth;
            writablePatient.Wealth = 0;
            family.Wealth = familyAfterCompensation;
            if (headChanged)
            {
                family.HeadPersonId = successor.Id;
            }
            organization.Treasury = organizationAfter;
            if (careSiteDeath || duringTransfer)
            {
                admission.DischargePolicyId =
                    duringTransfer
                        ? MilitaryRearMedicalDischargePolicyIds
                            .DeathDuringMedicalTransfer
                        : MilitaryRearMedicalDischargePolicyIds.DeathAtCareSite;
                evacuation.PatientReturnPolicyId =
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .RemainAtCareSiteAfterDeath;
                world.MilitaryMedicalDeathResponsibilities.Add(
                    responsibility);
            }
            if (duringPatientReturn)
            {
                admission.PatientReturnDeathClosureId =
                    patientReturnDeathClosure.Id;
                evacuation.PatientReturnDeathClosureId =
                    patientReturnDeathClosure.Id;
                evacuation.PatientReturnPolicyId =
                    awaitingReturnTeam
                        ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .CorpseAtArmyAwaitingTeamRejoin
                        : MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .ReturnCorpseWithTeam;
                evacuation.Status = awaitingReturnTeam
                    ? MilitaryMedicalEvacuationStatus
                        .PatientDeceasedAwaitingTeamRejoin
                    : MilitaryMedicalEvacuationStatus
                        .PatientDeceasedReturningToArmy;
                world.MilitaryMedicalDeathResponsibilities.Add(
                    responsibility);
                world.MilitaryPatientReturnDeathClosures.Add(
                    patientReturnDeathClosure);
            }
            if (inTreatment)
            {
                admission.InpatientDeathClosureId = inpatientClosure.Id;
                admission.Status =
                    MilitaryRearMedicalAdmissionStatus.Discharged;
                admission.ReadyForReturnDay = world.AbsoluteDay;
                admission.DischargedDay = world.AbsoluteDay;
                evacuation.Status =
                    MilitaryMedicalEvacuationStatus.ReadyForReturn;
                if (reservationReleaseTransaction != null)
                {
                    reservedMedicineBatch.ReservedQuantity = checked(
                        reservedMedicineBatch.ReservedQuantity -
                        releasedReservedMedicineUnits);
                    medicalTransfer.ReleasedReservedMedicineUnits = checked(
                        medicalTransfer.ReleasedReservedMedicineUnits +
                        releasedReservedMedicineUnits);
                    medicalTransfer.ReservationReleaseInventoryTransactionId =
                        reservationReleaseTransaction.Id;
                    world.InventoryTransactions.Add(
                        reservationReleaseTransaction);
                }
                world.MilitaryInpatientDeathClosures.Add(inpatientClosure);
            }
            if (duringTransfer)
            {
                admission.MedicalTransferDeathClosureId =
                    transferDeathClosure.Id;
                admission.Status =
                    MilitaryRearMedicalAdmissionStatus.Discharged;
                admission.ReadyForReturnDay = world.AbsoluteDay;
                admission.DischargedDay = world.AbsoluteDay;
                medicalTransfer.DeathClosureId = transferDeathClosure.Id;
                medicalTransfer.Status = transferDeathClosure.OccurredInTransit
                    ? MilitaryMedicalTransferStatus.DeceasedInTransit
                    : MilitaryMedicalTransferStatus.ClosedAfterPatientDeath;
                if (!transferDeathClosure.OccurredInTransit)
                {
                    var destination = FindRearMedicalSite(
                        world,
                        medicalTransfer.DestinationRearMedicalSiteId);
                    admission.RearMedicalSiteId = destination.Id;
                    evacuation.RearMedicalSiteId = destination.Id;
                    evacuation.CurrentCareLocationId = destination.LocationId;
                    evacuation.Status =
                        MilitaryMedicalEvacuationStatus.ReadyForReturn;
                }
                reservedMedicineBatch.ReservedQuantity = checked(
                    reservedMedicineBatch.ReservedQuantity -
                    releasedReservedMedicineUnits);
                medicalTransfer.ReleasedReservedMedicineUnits = checked(
                    medicalTransfer.ReleasedReservedMedicineUnits +
                    releasedReservedMedicineUnits);
                medicalTransfer.ReservationReleaseInventoryTransactionId =
                    reservationReleaseTransaction.Id;
                world.InventoryTransactions.Add(
                    reservationReleaseTransaction);
                world.MilitaryMedicalTransferDeathClosures.Add(
                    transferDeathClosure);
            }
            world.MilitaryFamilyInheritances.Add(inheritance);
            world.MilitarySurvivorCompensations.Add(compensation);
            world.MilitaryWoundDeaths.Add(death);
            world.LifeEvents.Add(new LifeEventRecordState
            {
                Id = deathLifeEventId,
                Type = LifeEventType.Death,
                Day = world.AbsoluteDay,
                PrimaryPersonId = patient.Id,
                SecondaryPersonId = string.Empty,
                FamilyId = family.Id,
                Summary = $"{patient.DisplayName}因战伤并发症去世。"
            });
            if (headChanged)
            {
                world.LifeEvents.Add(new LifeEventRecordState
                {
                    Id = successionLifeEventId,
                    Type = LifeEventType.Succession,
                    Day = world.AbsoluteDay,
                    PrimaryPersonId = successor.Id,
                    SecondaryPersonId = patient.Id,
                    FamilyId = family.Id,
                    Summary = $"{successor.DisplayName}继任{family.DisplayName}家主。"
                });
            }

            new PopulationLedgerSystem(people).RecordDeaths(
                world, new[] { writablePatient }, false);
            return death;
        }

        private static PersonState SelectSuccessor(
            WorldState world,
            FamilyState family,
            string deceasedPersonId,
            IPersonRepository people)
        {
            PersonState selected = null;
            for (var i = 0; i < family.MemberIds.Count; i++)
            {
                var candidateId = family.MemberIds[i];
                if (candidateId == deceasedPersonId)
                {
                    continue;
                }
                var candidate = people.GetRequired(candidateId);
                if (!candidate.IsAlive)
                {
                    continue;
                }
                if (selected == null ||
                    candidate.BirthDay < selected.BirthDay ||
                    candidate.BirthDay == selected.BirthDay &&
                    string.CompareOrdinal(candidate.Id, selected.Id) < 0)
                {
                    selected = candidate;
                }
            }
            return selected;
        }

        private static bool HasWoundDeath(
            WorldState world,
            string injuryEpisodeId,
            string patientPersonId)
        {
            for (var i = 0; i < world.MilitaryWoundDeaths.Count; i++)
            {
                var death = world.MilitaryWoundDeaths[i];
                if (death.InjuryEpisodeId == injuryEpisodeId ||
                    death.PatientPersonId == patientPersonId)
                {
                    return true;
                }
            }
            return false;
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            return _people ?? new WorldStatePersonRepository(world);
        }

        private static MilitaryRearMedicalAdmissionState FindAdmission(
            WorldState world, string id)
        {
            var result = world.MilitaryRearMedicalAdmissions.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing rear medical admission {id}.");
        }

        private static MilitaryMedicalEvacuationState FindEvacuation(
            WorldState world, string id)
        {
            var result = world.MilitaryMedicalEvacuations.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing medical evacuation {id}.");
        }

        private static MilitaryInjuryEpisodeState FindInjury(
            WorldState world, string id)
        {
            var result = world.MilitaryInjuryEpisodes.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military injury episode {id}.");
        }

        private static MilitaryRearMedicalSiteState FindRearMedicalSite(
            WorldState world, string id)
        {
            var result = world.MilitaryRearMedicalSites.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing rear medical site {id}.");
        }

        private static MilitaryServiceState FindService(
            WorldState world, string id)
        {
            var result = world.MilitaryServices.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military service {id}.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            var result = world.Armies.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing army {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world, string id)
        {
            var result = world.Organizations.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing organization {id}.");
        }

        private static FamilyState FindFamily(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException(
                    "Post-treatment death requires a permanent family.");
            }
            var result = world.Families.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing family {id}.");
        }

        private static MilitaryWoundDeathPolicyDefinitionState FindPolicy(
            WorldState world, string id)
        {
            var result = world.MilitaryWoundDeathPolicies.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing wound-death policy {id}.");
        }

        private static MilitaryInpatientDeteriorationPolicyDefinitionState
            FindDeteriorationPolicy(WorldState world, string id)
        {
            var result = world.MilitaryInpatientDeteriorationPolicies.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing inpatient deterioration policy {id}.");
        }

        private static
            MilitaryOriginalEvacuationDeteriorationPolicyDefinitionState
            FindOriginalEvacuationDeteriorationPolicy(
                WorldState world, string id)
        {
            var result = world
                .MilitaryOriginalEvacuationDeteriorationPolicies.Find(
                    item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing original-evacuation deterioration policy {id}.");
        }

        private static
            MilitaryPatientReturnDeteriorationPolicyDefinitionState
            FindPatientReturnDeteriorationPolicy(
                WorldState world, string id)
        {
            var result = world.MilitaryPatientReturnDeteriorationPolicies.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing patient-return deterioration policy {id}.");
        }

        private static bool HasOutstandingReturnTeamJourney(
            WorldState world,
            MilitaryMedicalEvacuationState evacuation)
        {
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var journey = FindJourney(
                    world, evacuation.TeamMembers[i].ReturnJourneyId);
                if (journey != null && journey.RemainingKilometers > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static List<MilitaryPatientReturnTeamJourneySnapshotState>
            BuildReturnTeamJourneySnapshots(
                WorldState world,
                MilitaryMedicalEvacuationState evacuation)
        {
            var snapshots = new List<
                MilitaryPatientReturnTeamJourneySnapshotState>(
                    evacuation.TeamMembers.Count);
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = evacuation.TeamMembers[i];
                var journey = FindJourney(world, member.ReturnJourneyId);
                snapshots.Add(
                    new MilitaryPatientReturnTeamJourneySnapshotState
                    {
                        PersonId = member.PersonId,
                        MilitaryServiceId = member.MilitaryServiceId,
                        ReturnJourneyId = member.ReturnJourneyId,
                        RemainingKilometersAtDeath =
                            journey?.RemainingKilometers ?? 0
                    });
            }
            return snapshots;
        }

        private static MilitaryMedicalTransferState FindMedicalTransfer(
            WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            var result = world.MilitaryMedicalTransfers.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing medical transfer {id}.");
        }

        private static ProductBatchState FindProductBatch(
            WorldState world, string id)
        {
            var result = world.ProductBatches.Find(item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing product batch {id}.");
        }

        private static JourneyState FindJourney(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return world.Journeys.Find(item => item.Id == id);
        }
    }
}
