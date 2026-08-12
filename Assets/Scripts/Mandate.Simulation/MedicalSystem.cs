using System;
using System.Collections.Generic;
using Mandate.Domain;

namespace Mandate.Simulation
{
    public sealed class MedicalTreatmentResult
    {
        public bool Success { get; }
        public int PatientsTreated { get; }
        public int RecoveredTroops { get; }
        public int HerbsConsumed { get; }
        public string Message { get; }

        public MedicalTreatmentResult(
            bool success,
            int patientsTreated,
            int recoveredTroops,
            int herbsConsumed,
            string message)
        {
            Success = success;
            PatientsTreated = patientsTreated;
            RecoveredTroops = recoveredTroops;
            HerbsConsumed = herbsConsumed;
            Message = message ?? string.Empty;
        }
    }

    public sealed class MedicalSystem
    {
        public const int PatientsPerHerbUnit = 5;
        private readonly NamedRandom _random;
        private readonly IPersonRepository _people;
        private readonly ProductionContentRegistry _content;
        private WorldState _fallbackWorld;
        private IPersonRepository _fallbackPeople;

        public MedicalSystem(
            ulong masterSeed,
            IPersonRepository people = null,
            ProductionContentRegistry content = null)
        {
            _random = new NamedRandom(masterSeed);
            _people = people;
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public void InitializePrototypeSupply(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            if (world.MilitaryMedicalInitialized)
            {
                return;
            }
            if (!world.MilitaryServiceInitialized)
            {
                throw new InvalidOperationException(
                    "Military service must be initialized before military medicine.");
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var product = _content.GetProduct(
                CoreProductionContent.HerbalMedicineMaterialProductId);
            var armies = new List<ArmyState>(world.Armies);
            armies.Sort((left, right) =>
                string.CompareOrdinal(left.Id, right.Id));
            for (var i = 0; i < armies.Count; i++)
            {
                var army = armies[i];
                var container = new InventoryContainerState
                {
                    Id = $"inventory_container.military_medical.{army.Id}",
                    KindId = "inventory_container.military_medical_store",
                    OwnerOrganizationId = army.OrganizationId,
                    LocationId = army.LocationId,
                    CapacityWeight = MilitaryMedicalRules
                        .PrototypeMedicalContainerCapacityWeight
                };
                world.InventoryContainers.Add(container);
                army.MedicalInventoryContainerId = container.Id;

                var transaction = ProductInventorySystem.NewTransaction(
                    world,
                    InventoryTransactionType.OpeningBalance,
                    army.CommanderPersonId,
                    string.Empty,
                    0,
                    0,
                    checked(
                        MilitaryMedicalRules.PrototypeOpeningMedicineQuantity *
                        product.BaseWeight),
                    $"Created opening military medical stock for {army.Id}.");
                var batch = ProductInventorySystem.NewOrganizationBatch(
                    world,
                    product,
                    container,
                    transaction.Id,
                    string.Empty,
                    MilitaryMedicalRules.PrototypeOpeningMedicineQuantity,
                    8_000);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, batch.Quantity, 0));
                world.ProductBatches.Add(batch);
                world.InventoryTransactions.Add(transaction);
            }
            world.MilitaryInjuryProfiles =
                MilitaryInjuryProfileCatalog.CreateCore();
            world.MilitarySurgicalProcedures =
                MilitarySurgicalProcedureCatalog.CreateCore();
            world.MilitaryWoundDeathPolicies =
                MilitaryWoundDeathPolicyCatalog.CreateCore();
            world.MilitaryInpatientDeteriorationPolicies =
                MilitaryInpatientDeteriorationPolicyCatalog.CreateCore();
            world.MilitaryOriginalEvacuationDeteriorationPolicies =
                MilitaryOriginalEvacuationDeteriorationPolicyCatalog
                    .CreateCore();
            world.MilitaryPatientReturnDeteriorationPolicies =
                MilitaryPatientReturnDeteriorationPolicyCatalog.CreateCore();
            world.MilitaryReturnTeamDeathPolicies =
                MilitaryReturnTeamDeathPolicyCatalog.CreateCore();
            world.MilitaryMedicalInitialized = true;
            world.Validate();
        }

        public MedicalTreatmentResult TreatArmyWounded(
            WorldState world,
            StableId physicianId,
            StableId armyId,
            int requestedPatients)
        {
            return TreatArmyWoundedInternal(
                world,
                physicianId,
                armyId,
                requestedPatients,
                string.Empty);
        }

        public MedicalTreatmentResult TreatArmyWoundedPerson(
            WorldState world,
            StableId physicianId,
            StableId armyId,
            StableId patientPersonId)
        {
            return TreatArmyWoundedInternal(
                world,
                physicianId,
                armyId,
                1,
                patientPersonId.Value);
        }

        private MedicalTreatmentResult TreatArmyWoundedInternal(
            WorldState world,
            StableId physicianId,
            StableId armyId,
            int requestedPatients,
            string prioritizedPatientPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (requestedPatients <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedPatients));
            }

            world.Validate();
            var physician = FindPerson(world, physicianId.Value);
            var army = FindArmy(world, armyId.Value);
            var medicalSkill = EffectiveMedicalSkill(physician);
            if (!physician.IsAlive ||
                medicalSkill < MilitaryMedicalRules
                    .MinimumPhysicianSkillBasisPoints)
            {
                return Failure(
                    "The person lacks the medical skill required for army care.");
            }
            if (physician.LocationId != army.LocationId)
            {
                return Failure("The physician and wounded are not co-located.");
            }
            if (IsPersonTraveling(world, physician.Id) ||
                IsArmyMarching(world, army.Id))
            {
                return Failure(
                    "Concentrated care cannot be performed during travel or marching.");
            }
            if (army.WoundedTroops <= 0)
            {
                return Failure("The army has no wounded people awaiting care.");
            }
            if (!world.MilitaryServiceInitialized)
            {
                if (!string.IsNullOrEmpty(prioritizedPatientPersonId))
                {
                    return Failure(
                        "Specific-person care requires formal military service records.");
                }
                return TreatLegacyAbstractArmy(
                    world, physician, army, requestedPatients, medicalSkill);
            }
            if (!world.MilitaryMedicalInitialized ||
                string.IsNullOrEmpty(army.MedicalInventoryContainerId))
            {
                return Failure(
                    "The army has no initialized formal medical inventory.");
            }

            var authorizer = FindPerson(world, army.CommanderPersonId);
            if (!authorizer.IsAlive ||
                !HasActiveCommanderService(world, authorizer.Id, army.Id))
            {
                return Failure(
                    "The army has no active commander able to authorize care.");
            }
            var authorizationPolicyId = ResolveAuthorizationPolicy(
                world, physician.Id, army.Id);
            var dailyWork = PhysicianWorkMinutesOnDay(
                world, physician.Id, world.AbsoluteDay);
            var workSlots = Math.Max(
                0,
                (MilitaryMedicalRules.MaximumDailyPhysicianWorkMinutes -
                 dailyWork) / MilitaryMedicalRules.TreatmentWorkMinutes);
            if (workSlots == 0)
            {
                return Failure(
                    "The physician has no treatment work time remaining today.");
            }

            var people = PeopleFor(world);
            var wounded = FindWoundedServices(world, people, army);
            if (!string.IsNullOrEmpty(prioritizedPatientPersonId))
            {
                var targetIndex = wounded.FindIndex(item =>
                    item.PersonId == prioritizedPatientPersonId);
                if (targetIndex < 0)
                {
                    return Failure(
                        "The requested person is not an eligible wounded member of this army.");
                }
                var target = wounded[targetIndex];
                wounded.RemoveAt(targetIndex);
                wounded.Insert(0, target);
            }
            var medicine = FindArmyMedicineBatches(world, army);
            long medicineUnits = 0;
            for (var i = 0; i < medicine.Count; i++)
            {
                medicineUnits = checked(
                    medicineUnits + medicine[i].Quantity -
                    medicine[i].ReservedQuantity);
            }
            var patients = Math.Min(
                requestedPatients,
                Math.Min(
                    wounded.Count,
                    Math.Min(
                        workSlots,
                        checked((int)Math.Min(int.MaxValue, medicineUnits)))));
            if (patients <= 0)
            {
                return Failure(
                    "No unreserved formal army herbal medicine is available.");
            }

            var serviceSystem = new MilitaryServiceSystem(people);
            var medicineIndex = 0;
            for (var patientIndex = 0;
                 patientIndex < patients;
                 patientIndex++)
            {
                while (medicine[medicineIndex].Quantity -
                       medicine[medicineIndex].ReservedQuantity <= 0)
                {
                    medicineIndex++;
                }
                var batch = medicine[medicineIndex];
                var militaryService = wounded[patientIndex];
                var patient = people.GetRequired(militaryService.PersonId);
                var openingHealth = patient.HealthBasisPoints;
                var skillBefore = EffectiveMedicalSkill(physician);
                var skillGain = Math.Min(
                    10_000 - skillBefore,
                    Math.Max(
                        1,
                        4 +
                        (MilitaryMedicalRules.ReturnToDutyHealthBasisPoints -
                         Math.Min(
                             MilitaryMedicalRules.ReturnToDutyHealthBasisPoints,
                             openingHealth)) / 500));
                var caseId = $"military_medical_case." +
                    $"{world.AbsoluteDay}.{world.MilitaryMedicalCases.Count:D6}";
                var medicalServiceId = $"military_medical_service." +
                    $"{world.AbsoluteDay}.{world.MilitaryMedicalServices.Count:D6}";
                var transaction = ProductInventorySystem.NewTransaction(
                    world,
                    InventoryTransactionType.MilitaryMedicalTreatmentConsumed,
                    physician.Id,
                    string.Empty,
                    0,
                    0,
                    -checked(
                        batch.UnitWeight *
                        MilitaryMedicalRules.MedicineUnitsPerTreatment),
                    $"Consumed army medicine for {caseId}.");
                transaction.SourceMilitaryMedicalServiceId = medicalServiceId;
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch,
                    -MilitaryMedicalRules.MedicineUnitsPerTreatment,
                    0));

                serviceSystem.RecoverWoundedServiceWithoutValidation(
                    world, militaryService, people);
                var writablePhysician = people.GetRequiredForUpdate(physician.Id);
                var skillAfter = checked(skillBefore + skillGain);
                writablePhysician.ProfessionalSkills ??=
                    new ProfessionalSkillState();
                writablePhysician.ProfessionalSkills.Medicine = Math.Max(
                    writablePhysician.ProfessionalSkills.Medicine,
                    skillAfter);
                writablePhysician.MedicalSkillBasisPoints = Math.Max(
                    writablePhysician.MedicalSkillBasisPoints,
                    skillAfter);
                batch.Quantity = checked(
                    batch.Quantity -
                    MilitaryMedicalRules.MedicineUnitsPerTreatment);
                var closingHealth = people.GetRequired(patient.Id)
                    .HealthBasisPoints;

                var medicalCase = new MilitaryMedicalCaseState
                {
                    Id = caseId,
                    ArmyId = army.Id,
                    MilitaryServiceId = militaryService.Id,
                    PatientPersonId = patient.Id,
                    PhysicianPersonId = physician.Id,
                    AuthorizingPersonId = authorizer.Id,
                    AuthorizationPolicyId = authorizationPolicyId,
                    TriageId = TriageId(openingHealth),
                    TreatmentProtocolId = MilitaryMedicalTreatmentProtocolIds
                        .FieldHerbalCare,
                    DiagnosedDay = world.AbsoluteDay,
                    Status = MilitaryMedicalCaseStatus.Closed,
                    ClosedDay = world.AbsoluteDay,
                    ClosureReasonId = MilitaryMedicalCaseClosureReasonIds
                        .ReturnedToDuty,
                    MilitaryMedicalServiceId = medicalServiceId
                };
                var service = new MilitaryMedicalServiceState
                {
                    Id = medicalServiceId,
                    Day = world.AbsoluteDay,
                    MedicalCaseId = caseId,
                    ArmyId = army.Id,
                    MilitaryServiceId = militaryService.Id,
                    PatientPersonId = patient.Id,
                    PhysicianPersonId = physician.Id,
                    AuthorizingPersonId = authorizer.Id,
                    AuthorizationPolicyId = authorizationPolicyId,
                    VenuePolicyId = MilitaryMedicalVenuePolicyIds.ArmyFieldUnit,
                    WorkMinutes = MilitaryMedicalRules.TreatmentWorkMinutes,
                    MedicineProductDefinitionId = batch.ProductDefinitionId,
                    SourceMedicineBatchId = batch.Id,
                    InventoryTransactionId = transaction.Id,
                    MedicineUnitsConsumed =
                        MilitaryMedicalRules.MedicineUnitsPerTreatment,
                    OpeningHealthBasisPoints = openingHealth,
                    ClosingHealthBasisPoints = closingHealth,
                    RecoveredHealthBasisPoints = closingHealth - openingHealth,
                    OpeningMilitaryStatus = MilitaryServiceStatus.Wounded,
                    ClosingMilitaryStatus = MilitaryServiceStatus.Active,
                    PhysicianMedicalSkillBeforeBasisPoints = skillBefore,
                    PhysicianMedicalSkillAfterBasisPoints = skillAfter,
                    PhysicianMedicalSkillGainBasisPoints = skillGain
                };
                world.InventoryTransactions.Add(transaction);
                world.MilitaryMedicalCases.Add(medicalCase);
                world.MilitaryMedicalServices.Add(service);
            }

            var sequence = world.MedicalTreatments.Count;
            var record = new MedicalTreatmentRecordState
            {
                Id = $"medical_treatment.{world.AbsoluteDay}." +
                    $"{physician.Id}.{army.Id}.{sequence}",
                Day = world.AbsoluteDay,
                PhysicianPersonId = physician.Id,
                ArmyId = army.Id,
                PatientsTreated = patients,
                RecoveredTroops = patients,
                HerbsConsumed = patients,
                Summary = $"{physician.DisplayName} treated {army.DisplayName}: " +
                    $"{patients} wounded returned to duty."
            };
            world.MedicalTreatments.Add(record);
            world.Validate();
            return new MedicalTreatmentResult(
                true,
                patients,
                patients,
                patients,
                record.Summary);
        }

        private MedicalTreatmentResult TreatLegacyAbstractArmy(
            WorldState world,
            PersonState physician,
            ArmyState army,
            int requestedPatients,
            int medicalSkill)
        {
            var herbs = FindInventory(world, physician.Id, "commodity.herbs");
            if (herbs == null || herbs.Quantity <= 0)
            {
                return Failure("The physician carries no legacy herbs.");
            }
            var patients = Math.Min(
                requestedPatients,
                Math.Min(
                    army.WoundedTroops,
                    herbs.Quantity * PatientsPerHerbUnit));
            var herbsConsumed =
                (patients + PatientsPerHerbUnit - 1) / PatientsPerHerbUnit;
            var sequence = world.MedicalTreatments.Count;
            var variation = _random.Range(
                "medical",
                new StableId(physician.Id),
                sequence,
                "army_treatment",
                -500,
                501);
            var recoveryRate = Clamp(
                3_500 + medicalSkill / 2 + variation,
                2_500,
                9_500);
            var recovered = Math.Max(1, patients * recoveryRate / 10_000);
            herbs.Quantity -= herbsConsumed;
            if (herbs.Quantity == 0)
            {
                world.Inventories.Remove(herbs);
            }
            recovered = new MilitaryServiceSystem().RecoverWounded(
                world,
                new StableId(army.Id),
                recovered,
                sequence,
                PeopleFor(world));
            var record = new MedicalTreatmentRecordState
            {
                Id = $"medical_treatment.{world.AbsoluteDay}." +
                    $"{physician.Id}.{army.Id}.{sequence}",
                Day = world.AbsoluteDay,
                PhysicianPersonId = physician.Id,
                ArmyId = army.Id,
                PatientsTreated = patients,
                RecoveredTroops = recovered,
                HerbsConsumed = herbsConsumed,
                Summary = $"Legacy treatment restored {recovered} abstract wounded."
            };
            world.MedicalTreatments.Add(record);
            world.Validate();
            return new MedicalTreatmentResult(
                true, patients, recovered, herbsConsumed, record.Summary);
        }

        private List<ProductBatchState> FindArmyMedicineBatches(
            WorldState world,
            ArmyState army)
        {
            _content.ValidateManifest(world.ProductionContentManifest);
            var result = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId ==
                        army.MedicalInventoryContainerId &&
                    batch.OwnerOrganizationId == army.OrganizationId &&
                    batch.ProductDefinitionId == CoreProductionContent
                        .HerbalMedicineMaterialProductId &&
                    batch.Quantity > batch.ReservedQuantity)
                {
                    result.Add(batch);
                }
            }
            result.Sort((left, right) =>
            {
                var day = left.ProducedDay.CompareTo(right.ProducedDay);
                return day != 0
                    ? day
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static List<MilitaryServiceState> FindWoundedServices(
            WorldState world,
            IPersonRepository people,
            ArmyState army)
        {
            var result = new List<MilitaryServiceState>();
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.ArmyId == army.Id &&
                    service.Status == MilitaryServiceStatus.Wounded &&
                    !MilitaryMedicalEvacuationSystem.IsServiceInEvacuation(
                        world, service.Id) &&
                    people.GetRequired(service.PersonId).LocationId ==
                        army.LocationId)
                {
                    result.Add(service);
                }
            }
            result.Sort((left, right) =>
            {
                var person = string.CompareOrdinal(
                    left.PersonId, right.PersonId);
                return person != 0
                    ? person
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            return result;
        }

        private static string ResolveAuthorizationPolicy(
            WorldState world,
            string physicianId,
            string armyId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == physicianId &&
                    service.ArmyId == armyId &&
                    service.Role == MilitaryServiceRole.Medic &&
                    service.Status == MilitaryServiceStatus.Active)
                {
                    return MilitaryMedicalAuthorizationPolicyIds.InternalMedic;
                }
            }
            return MilitaryMedicalAuthorizationPolicyIds
                .CommanderAuthorizedPractitioner;
        }

        private static bool HasActiveCommanderService(
            WorldState world,
            string personId,
            string armyId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == personId &&
                    service.ArmyId == armyId &&
                    service.Role == MilitaryServiceRole.Commander &&
                    service.Status == MilitaryServiceStatus.Active)
                {
                    return true;
                }
            }
            return false;
        }

        private static int PhysicianWorkMinutesOnDay(
            WorldState world,
            string physicianId,
            long day)
        {
            var minutes = 0;
            for (var i = 0; i < world.CivilianMedicalServices.Count; i++)
            {
                var service = world.CivilianMedicalServices[i];
                if (service.PhysicianPersonId == physicianId &&
                    service.Day == day)
                {
                    minutes = checked(minutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryMedicalServices.Count; i++)
            {
                var service = world.MilitaryMedicalServices[i];
                if (service.PhysicianPersonId == physicianId &&
                    service.Day == day)
                {
                    minutes = checked(minutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryRearMedicalTreatments.Count; i++)
            {
                var treatment = world.MilitaryRearMedicalTreatments[i];
                if (treatment.PhysicianPersonId == physicianId &&
                    treatment.Day == day)
                {
                    minutes = checked(minutes + treatment.WorkMinutes);
                }
            }
            return minutes;
        }

        private static string TriageId(int healthBasisPoints)
        {
            if (healthBasisPoints <= 2_500)
            {
                return MilitaryMedicalTriageIds.Critical;
            }
            return healthBasisPoints <= 4_000
                ? MilitaryMedicalTriageIds.Severe
                : MilitaryMedicalTriageIds.Moderate;
        }

        private static bool IsPersonTraveling(WorldState world, string personId)
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

        private static bool IsArmyMarching(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return true;
                }
            }
            return false;
        }

        private static InventoryStackState FindInventory(
            WorldState world,
            string personId,
            string commodityId)
        {
            for (var i = 0; i < world.Inventories.Count; i++)
            {
                var inventory = world.Inventories[i];
                if (inventory.OwnerPersonId == personId &&
                    inventory.CommodityId == commodityId)
                {
                    return inventory;
                }
            }
            return null;
        }

        private PersonState FindPerson(WorldState world, string personId)
        {
            return PeopleFor(world).GetRequired(personId);
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

        private static ArmyState FindArmy(WorldState world, string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }
            throw new InvalidOperationException($"Missing army {armyId}.");
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }
            return value > maximum ? maximum : value;
        }

        private static int EffectiveMedicalSkill(PersonState person)
        {
            var professionalSkill = person.ProfessionalSkills == null
                ? 0
                : person.ProfessionalSkills.Medicine;
            return Math.Max(person.MedicalSkillBasisPoints, professionalSkill);
        }

        private static MedicalTreatmentResult Failure(string message)
        {
            return new MedicalTreatmentResult(false, 0, 0, 0, message);
        }
    }

    public sealed class MilitaryMedicalEvacuationSystem
    {
        private readonly IPersonRepository _people;

        public MilitaryMedicalEvacuationSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public MilitaryMedicalEvacuationState Dispatch(
            WorldState world,
            StableId authorizingPersonId,
            StableId patientMilitaryServiceId,
            IReadOnlyList<StableId> teamMilitaryServiceIds,
            StableId routeId,
            StableId destinationLocationId,
            StableId designatedReceivingPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (teamMilitaryServiceIds == null ||
                teamMilitaryServiceIds.Count <
                    MilitaryMedicalRules.MinimumEvacuationTeamMembers ||
                teamMilitaryServiceIds.Count >
                    MilitaryMedicalRules.MaximumEvacuationTeamMembers)
            {
                throw new InvalidOperationException(
                    "A medical evacuation requires two to eight team members.");
            }

            world.Validate();
            if (!world.MilitaryServiceInitialized ||
                !world.MilitaryMedicalInitialized)
            {
                throw new InvalidOperationException(
                    "Formal military service and medicine must be initialized.");
            }

            var people = _people ?? new WorldStatePersonRepository(world);
            var patientService = FindService(
                world, patientMilitaryServiceId.Value);
            var army = FindArmy(world, patientService.ArmyId);
            var patient = people.GetRequired(patientService.PersonId);
            if (patientService.Status != MilitaryServiceStatus.Wounded ||
                !patient.IsAlive ||
                patient.LocationId != army.LocationId ||
                IsPersonTraveling(world, patient.Id) ||
                IsServiceInEvacuation(world, patientService.Id))
            {
                throw new InvalidOperationException(
                    "The patient is not an eligible co-located wounded service member.");
            }
            if (IsArmyMarching(world, army.Id))
            {
                throw new InvalidOperationException(
                    "An army already marching cannot dispatch a new evacuation.");
            }

            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, new StableId(army.Id));
            if (authority < MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "Army authority is required to dispatch a medical evacuation.");
            }

            var route = FindRoute(world, routeId.Value);
            ValidateRoute(
                route, army.LocationId, destinationLocationId.Value);
            var receiver = people.GetRequired(
                designatedReceivingPersonId.Value);
            var receiverSkill = EffectiveMedicalSkill(receiver);
            if (!receiver.IsAlive ||
                receiver.LocationId != destinationLocationId.Value ||
                IsPersonTraveling(world, receiver.Id) ||
                receiverSkill < MilitaryMedicalRules
                    .MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The designated receiver is not an available qualified practitioner at the destination.");
            }

            var memberServices = new List<MilitaryServiceState>();
            var memberPeople = new List<PersonState>();
            var uniqueServices = new HashSet<string>(StringComparer.Ordinal);
            var uniquePeople = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < teamMilitaryServiceIds.Count; i++)
            {
                var service = FindService(
                    world, teamMilitaryServiceIds[i].Value);
                var person = people.GetRequired(service.PersonId);
                if (!uniqueServices.Add(service.Id) ||
                    !uniquePeople.Add(person.Id) ||
                    service.ArmyId != army.Id ||
                    service.Status != MilitaryServiceStatus.Active ||
                    !person.IsAlive ||
                    person.LocationId != army.LocationId ||
                    person.Id == patient.Id ||
                    person.Id == receiver.Id ||
                    IsPersonTraveling(world, person.Id) ||
                    IsServiceInEvacuation(world, service.Id) ||
                    IsFormationCommander(world, person.Id))
                {
                    throw new InvalidOperationException(
                        "Every evacuation team member must be a unique eligible active non-commanding member of the source army.");
                }
                memberServices.Add(service);
                memberPeople.Add(person);
            }

            var travel = new TravelSystem(people);
            var evacuation = new MilitaryMedicalEvacuationState
            {
                Id = $"military_medical_evacuation.{world.AbsoluteDay}." +
                    $"{world.MilitaryMedicalEvacuations.Count:D6}",
                CreatedDay = world.AbsoluteDay,
                SourceArmyId = army.Id,
                PatientMilitaryServiceId = patientService.Id,
                PatientPersonId = patient.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                TransportPolicyId = MilitaryMedicalEvacuationTransportPolicyIds
                    .StretcherTeamFoot,
                ReceptionPolicyId = MilitaryMedicalEvacuationReceptionPolicyIds
                    .DesignatedPractitionerHandoff,
                PatientReturnPolicyId =
                    MilitaryMedicalEvacuationPatientReturnPolicyIds
                        .ReturnWithTeam,
                OriginLocationId = army.LocationId,
                DestinationLocationId = destinationLocationId.Value,
                CurrentCareLocationId = destinationLocationId.Value,
                RouteId = route.Id,
                DesignatedReceivingPersonId = receiver.Id
            };
            evacuation.PatientJourneyId = travel.StartJourneyWithoutValidation(
                world,
                new StableId(patient.Id),
                new StableId(route.Id),
                destinationLocationId,
                TravelMode.Foot).Id;
            for (var i = 0; i < memberServices.Count; i++)
            {
                var journey = travel.StartJourneyWithoutValidation(
                    world,
                    new StableId(memberPeople[i].Id),
                    new StableId(route.Id),
                    destinationLocationId,
                    TravelMode.Foot);
                memberServices[i].Status =
                    MilitaryServiceStatus.MedicalEvacuationDuty;
                memberServices[i].LastStatusChangeDay = world.AbsoluteDay;
                evacuation.TeamMembers.Add(
                    new MilitaryMedicalEvacuationTeamMemberState
                    {
                        PersonId = memberPeople[i].Id,
                        MilitaryServiceId = memberServices[i].Id,
                        RoleId = MilitaryMedicalEvacuationTeamRoleIds
                            .StretcherBearer,
                        JourneyId = journey.Id,
                        ReturnJourneyId = string.Empty,
                        ReturnDeathId = string.Empty
                    });
            }
            world.MilitaryMedicalEvacuations.Add(evacuation);
            new MilitaryServiceSystem(people).SynchronizeArmyCaches(
                world, army.Id);
            world.Validate();
            return evacuation;
        }

        public void Receive(
            WorldState world,
            StableId evacuationId,
            StableId receivingPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var evacuation = FindEvacuation(world, evacuationId.Value);
            if (evacuation.Status !=
                MilitaryMedicalEvacuationStatus.AwaitingReception ||
                evacuation.DesignatedReceivingPersonId !=
                    receivingPersonId.Value)
            {
                throw new InvalidOperationException(
                    "The evacuation is not awaiting this designated receiver.");
            }
            var people = _people ?? new WorldStatePersonRepository(world);
            var receiver = people.GetRequired(receivingPersonId.Value);
            var skill = EffectiveMedicalSkill(receiver);
            if (!receiver.IsAlive ||
                receiver.LocationId != evacuation.DestinationLocationId ||
                IsPersonTraveling(world, receiver.Id) ||
                skill < MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The designated practitioner is not available to receive the patient.");
            }

            evacuation.Status = MilitaryMedicalEvacuationStatus.Received;
            evacuation.ReceivingPersonId = receiver.Id;
            evacuation.ReceivedDay = world.AbsoluteDay;
            evacuation.ReceivingMedicalSkillBasisPoints = skill;
            world.Validate();
        }

        internal static void ResolveArrivalsWithoutValidation(WorldState world)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                if (evacuation.Status !=
                        MilitaryMedicalEvacuationStatus.InTransit &&
                    evacuation.Status !=
                        MilitaryMedicalEvacuationStatus.DeceasedInTransit ||
                    FindJourney(world, evacuation.PatientJourneyId) != null)
                {
                    continue;
                }
                var allArrived = true;
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    if (FindJourney(
                            world,
                            evacuation.TeamMembers[memberIndex].JourneyId) != null)
                    {
                        allArrived = false;
                        break;
                    }
                }
                if (allArrived)
                {
                    evacuation.Status = evacuation.Status ==
                            MilitaryMedicalEvacuationStatus.DeceasedInTransit
                        ? MilitaryMedicalEvacuationStatus.ReadyForReturn
                        : MilitaryMedicalEvacuationStatus.AwaitingReception;
                    evacuation.ArrivedDay = world.AbsoluteDay;
                }
            }
            MilitaryMedicalTransferSystem.ResolveArrivalsWithoutValidation(
                world);
            MilitaryRearMedicalSystem.ResolveReturnsWithoutValidation(world);
        }

        public static bool IsServiceInEvacuation(
            WorldState world,
            string militaryServiceId)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                if (evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.Completed)
                {
                    continue;
                }
                if (evacuation.PatientMilitaryServiceId == militaryServiceId)
                {
                    return true;
                }
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    if (evacuation.TeamMembers[memberIndex].MilitaryServiceId ==
                        militaryServiceId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsPersonInEvacuation(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                if (evacuation.Status ==
                    MilitaryMedicalEvacuationStatus.Completed)
                {
                    continue;
                }
                if (evacuation.PatientPersonId == personId)
                {
                    return true;
                }
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    if (evacuation.TeamMembers[memberIndex].PersonId == personId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static MilitaryMedicalEvacuationState FindEvacuation(
            WorldState world,
            string evacuationId)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                if (world.MilitaryMedicalEvacuations[i].Id == evacuationId)
                {
                    return world.MilitaryMedicalEvacuations[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military medical evacuation {evacuationId}.");
        }

        private static MilitaryServiceState FindService(
            WorldState world,
            string serviceId)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                if (world.MilitaryServices[i].Id == serviceId)
                {
                    return world.MilitaryServices[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military service {serviceId}.");
        }

        private static ArmyState FindArmy(WorldState world, string armyId)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == armyId)
                {
                    return world.Armies[i];
                }
            }
            throw new InvalidOperationException($"Missing army {armyId}.");
        }

        private static RouteState FindRoute(WorldState world, string routeId)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == routeId)
                {
                    return world.Routes[i];
                }
            }
            throw new InvalidOperationException($"Missing route {routeId}.");
        }

        private static JourneyState FindJourney(WorldState world, string journeyId)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].Id == journeyId)
                {
                    return world.Journeys[i];
                }
            }
            return null;
        }

        private static bool IsPersonTraveling(WorldState world, string personId)
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

        private static bool IsArmyMarching(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsFormationCommander(
            WorldState world,
            string personId)
        {
            for (var i = 0; i < world.MilitaryFormations.Count; i++)
            {
                if (world.MilitaryFormations[i].CommanderPersonId == personId)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ValidateRoute(
            RouteState route,
            string originLocationId,
            string destinationLocationId)
        {
            var forward = route.FromLocationId == originLocationId &&
                route.ToLocationId == destinationLocationId;
            var backward = route.Bidirectional &&
                route.ToLocationId == originLocationId &&
                route.FromLocationId == destinationLocationId;
            if (!forward && !backward)
            {
                throw new InvalidOperationException(
                    $"Route {route.Id} does not connect the evacuation endpoints.");
            }
        }

        private static int EffectiveMedicalSkill(PersonState person)
        {
            return Math.Max(
                person.MedicalSkillBasisPoints,
                person.ProfessionalSkills == null
                    ? 0
                    : person.ProfessionalSkills.Medicine);
        }
    }

    public sealed class MilitaryRearMedicalSystem
    {
        private const string StoreKindId =
            "inventory_container.military_rear_medical_store";
        private readonly IPersonRepository _people;
        private readonly ProductionContentRegistry _content;

        public MilitaryRearMedicalSystem(
            IPersonRepository people = null,
            ProductionContentRegistry content = null)
        {
            _people = people;
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public MilitaryRearMedicalSiteState RegisterExistingClinic(
            WorldState world,
            StableId locationId,
            StableId ownerOrganizationId,
            StableId managerPersonId,
            int bedCapacity,
            int openingMedicineUnits)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            if (bedCapacity <= 0 || openingMedicineUnits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bedCapacity));
            }
            world.Validate();
            if (!world.MilitaryMedicalInitialized)
            {
                throw new InvalidOperationException(
                    "Military medicine must be initialized first.");
            }

            var location = FindLocation(world, locationId.Value);
            var organization = FindOrganization(
                world, ownerOrganizationId.Value);
            var people = PeopleFor(world);
            var manager = people.GetRequired(managerPersonId.Value);
            if ((location.Features & LocationFeature.Clinic) == 0)
            {
                throw new InvalidOperationException(
                    "A rear medical site requires an existing clinic location.");
            }
            if (!manager.IsAlive || manager.LocationId != location.Id ||
                !CanManageOrganization(world, manager.Id, organization))
            {
                throw new InvalidOperationException(
                    "The registering manager must be an available organization member at the clinic.");
            }

            var siteId = $"military_rear_medical_site.{organization.Id}." +
                location.Id;
            var containerId =
                $"inventory_container.military_rear_medical.{organization.Id}." +
                location.Id;
            if (FindSiteOrNull(world, siteId) != null ||
                FindContainerOrNull(world, containerId) != null)
            {
                throw new InvalidOperationException(
                    "This organization clinic is already registered.");
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var product = _content.GetProduct(
                CoreProductionContent.HerbalMedicineMaterialProductId);
            var container = new InventoryContainerState
            {
                Id = containerId,
                KindId = StoreKindId,
                OwnerOrganizationId = organization.Id,
                LocationId = location.Id,
                CapacityWeight = Math.Max(
                    MilitaryMedicalRules.PrototypeMedicalContainerCapacityWeight,
                    checked((long)Math.Max(1, openingMedicineUnits) *
                        product.BaseWeight))
            };
            var site = new MilitaryRearMedicalSiteState
            {
                Id = siteId,
                KindId = MilitaryRearMedicalSiteKindIds.ExistingClinic,
                LocationId = location.Id,
                OwnerOrganizationId = organization.Id,
                MedicineInventoryContainerId = container.Id,
                BedCapacity = bedCapacity,
                RegisteredDay = world.AbsoluteDay
            };

            world.InventoryContainers.Add(container);
            world.MilitaryRearMedicalSites.Add(site);
            if (openingMedicineUnits > 0)
            {
                var transaction = ProductInventorySystem.NewTransaction(
                    world,
                    InventoryTransactionType.OpeningBalance,
                    manager.Id,
                    string.Empty,
                    0,
                    0,
                    checked((long)openingMedicineUnits * product.BaseWeight),
                    $"Registered opening rear-clinic medicine for {site.Id}.");
                var batch = ProductInventorySystem.NewOrganizationBatch(
                    world,
                    product,
                    container,
                    transaction.Id,
                    string.Empty,
                    openingMedicineUnits,
                    8_000);
                transaction.Lines.Add(ProductInventorySystem.Line(
                    batch, batch.Quantity, 0));
                world.ProductBatches.Add(batch);
                world.InventoryTransactions.Add(transaction);
            }
            world.Validate();
            return site;
        }

        public MilitaryRearMedicalAdmissionState Admit(
            WorldState world,
            StableId evacuationId,
            StableId rearMedicalSiteId,
            StableId physicianPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var evacuation = FindEvacuation(world, evacuationId.Value);
            var site = FindSite(world, rearMedicalSiteId.Value);
            var people = PeopleFor(world);
            var physician = people.GetRequired(physicianPersonId.Value);
            var patient = people.GetRequired(evacuation.PatientPersonId);
            if (evacuation.Status != MilitaryMedicalEvacuationStatus.Received ||
                evacuation.DestinationLocationId != site.LocationId ||
                evacuation.ReceivingPersonId != physician.Id ||
                !site.IsOperational)
            {
                throw new InvalidOperationException(
                    "The received evacuation cannot be admitted at this site.");
            }
            if (!physician.IsAlive || physician.LocationId != site.LocationId ||
                IsPersonTraveling(world, physician.Id) ||
                EffectiveMedicalSkill(physician) <
                    MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The receiving practitioner is unavailable or unqualified.");
            }
            if (OccupiedBeds(world, site.Id) >= site.BedCapacity)
            {
                throw new InvalidOperationException(
                    "The rear medical site has no available bed.");
            }

            var admissionId =
                $"military_rear_medical_admission.{world.AbsoluteDay}." +
                $"{world.MilitaryRearMedicalAdmissions.Count:D6}";
            var treatmentPlan = BuildTreatmentPlan(site);
            var injury = AssessInjury(
                world, evacuation, patient, admissionId);
            if (!string.IsNullOrEmpty(injury.SurgicalProcedureId))
            {
                treatmentPlan.Insert(
                    treatmentPlan.Count - 1,
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery);
            }
            if (injury.InfectionStatus == MilitaryInfectionStatus.Active)
            {
                treatmentPlan.Insert(
                    treatmentPlan.Count - 1,
                    MilitaryRearMedicalTreatmentProtocolIds.InfectionControl);
            }
            var admission = new MilitaryRearMedicalAdmissionState
            {
                Id = admissionId,
                EvacuationId = evacuation.Id,
                RearMedicalSiteId = site.Id,
                PatientPersonId = evacuation.PatientPersonId,
                PatientMilitaryServiceId =
                    evacuation.PatientMilitaryServiceId,
                PhysicianPersonId = physician.Id,
                AdmittedDay = world.AbsoluteDay,
                RequiredTreatmentStages = treatmentPlan.Count,
                TreatmentPlanProtocolIds = treatmentPlan,
                TreatmentPlanOriginSiteKindId = site.KindId,
                InjuryEpisodeId = injury.Id
            };
            world.MilitaryInjuryEpisodes.Add(injury);
            world.MilitaryRearMedicalAdmissions.Add(admission);
            evacuation.RearMedicalSiteId = site.Id;
            evacuation.RearMedicalAdmissionId = admission.Id;
            evacuation.Status = MilitaryMedicalEvacuationStatus.Admitted;
            world.Validate();
            return admission;
        }

        public MilitaryRearMedicalTreatmentState TreatInpatient(
            WorldState world,
            StableId admissionId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var admission = FindAdmission(world, admissionId.Value);
            var evacuation = FindEvacuation(world, admission.EvacuationId);
            var site = FindSite(world, admission.RearMedicalSiteId);
            var people = PeopleFor(world);
            var physician = people.GetRequired(admission.PhysicianPersonId);
            var patient = people.GetRequired(admission.PatientPersonId);
            if (admission.Status !=
                    MilitaryRearMedicalAdmissionStatus.InTreatment ||
                evacuation.Status != MilitaryMedicalEvacuationStatus.Admitted ||
                !site.IsOperational || !physician.IsAlive || !patient.IsAlive ||
                physician.LocationId != site.LocationId ||
                patient.LocationId != site.LocationId ||
                IsPersonTraveling(world, physician.Id))
            {
                throw new InvalidOperationException(
                    "The inpatient treatment conditions are not satisfied.");
            }
            var skillBefore = EffectiveMedicalSkill(physician);
            if (skillBefore <
                MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The assigned practitioner is not qualified.");
            }
            var stageIndex = admission.CompletedTreatmentStages;
            if (stageIndex < 0 ||
                stageIndex >= admission.RequiredTreatmentStages ||
                admission.TreatmentPlanProtocolIds == null ||
                admission.TreatmentPlanProtocolIds.Count !=
                    admission.RequiredTreatmentStages)
            {
                throw new InvalidOperationException(
                    "The inpatient treatment stages are already complete.");
            }
            var treatmentProtocolId =
                admission.TreatmentPlanProtocolIds[stageIndex];
            var surgery = treatmentProtocolId ==
                MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery;
            MilitaryInjuryEpisodeState surgeryEpisode = null;
            MilitarySurgicalProcedureDefinitionState surgeryProcedure = null;
            if (surgery)
            {
                surgeryEpisode = FindInjuryEpisode(
                    world, admission.InjuryEpisodeId);
                if (string.IsNullOrEmpty(surgeryEpisode.SurgicalProcedureId) ||
                    !string.IsNullOrEmpty(surgeryEpisode.SurgeryTreatmentId))
                {
                    throw new InvalidOperationException(
                        "Trauma surgery requires one unresolved surgical injury.");
                }
                surgeryProcedure = FindSurgicalProcedure(
                    world, surgeryEpisode.SurgicalProcedureId);
                if (skillBefore <
                    surgeryProcedure.MinimumPhysicianSkillBasisPoints)
                {
                    throw new InvalidOperationException(
                        "The assigned practitioner is not qualified for surgery.");
                }
            }
            var infectionControl = treatmentProtocolId ==
                MilitaryRearMedicalTreatmentProtocolIds.InfectionControl;
            MilitaryInjuryEpisodeState infectionEpisode = null;
            if (infectionControl)
            {
                infectionEpisode = FindInjuryEpisode(
                    world, admission.InjuryEpisodeId);
                if (infectionEpisode.InfectionStatus !=
                    MilitaryInfectionStatus.Active)
                {
                    throw new InvalidOperationException(
                        "Infection control requires an active infection episode.");
                }
            }
            var stabilization = treatmentProtocolId ==
                MilitaryRearMedicalTreatmentProtocolIds.FieldStabilization;
            var workMinutes = surgery
                ? surgeryProcedure.WorkMinutes
                : infectionControl
                ? MilitaryMedicalRules.InfectionControlWorkMinutes
                : stabilization
                    ? MilitaryMedicalRules.FieldStabilizationWorkMinutes
                    : MilitaryMedicalRules.RearTreatmentWorkMinutes;
            var targetHealth = surgery
                ? surgeryProcedure.TargetHealthBasisPoints
                : infectionControl
                ? MilitaryMedicalRules.InfectionControlHealthBasisPoints
                : stabilization
                    ? MilitaryMedicalRules.FieldStabilizationHealthBasisPoints
                    : MilitaryMedicalRules.ReturnToDutyHealthBasisPoints;
            var medicineUnits = surgery
                ? surgeryProcedure.MedicineUnits
                : infectionControl
                ? MilitaryMedicalRules.InfectionControlMedicineUnits
                : MilitaryMedicalRules.MedicineUnitsPerTreatment;
            var permanentImpairment = surgery &&
                surgeryEpisode.SeverityBasisPoints >=
                    surgeryProcedure
                        .PermanentImpairmentSeverityBasisPoints;
            var permanentPenalty = permanentImpairment
                ? surgeryProcedure
                    .PermanentImpairmentLaborPenaltyBasisPoints
                : 0;
            if (surgery && checked(
                    patient.PermanentLaborCapacityPenaltyBasisPoints +
                    permanentPenalty) > 10_000)
            {
                throw new InvalidOperationException(
                    "The permanent labor penalty exceeds the supported range.");
            }
            if (PhysicianWorkMinutesOnDay(
                    world, physician.Id, world.AbsoluteDay) +
                workMinutes >
                    MilitaryMedicalRules.MaximumDailyPhysicianWorkMinutes)
            {
                throw new InvalidOperationException(
                    "The practitioner has insufficient work time today.");
            }
            var batch = FindMedicineBatch(world, site, medicineUnits);
            var transfer = FindMedicalTransfer(
                world, admission.MedicalTransferId);
            var consumesTransferReservation = transfer != null &&
                transfer.Status == MilitaryMedicalTransferStatus.Completed;
            if (consumesTransferReservation)
            {
                if (transfer.DestinationRearMedicalSiteId != site.Id ||
                    transfer.ConsumedReservedMedicineUnits + medicineUnits >
                        transfer.ReservedMedicineUnits)
                {
                    throw new InvalidOperationException(
                        "The transferred patient's medicine reservation does not cover this treatment.");
                }
                batch = FindProductBatch(
                    world, transfer.ReservedMedicineBatchId);
                if (batch.InventoryContainerId !=
                        site.MedicineInventoryContainerId ||
                    batch.ReservedQuantity < medicineUnits)
                {
                    throw new InvalidOperationException(
                        "The transferred patient's reserved medicine is unavailable.");
                }
            }
            if (batch == null)
            {
                throw new InvalidOperationException(
                    "The rear medical site has no unreserved herbal medicine.");
            }

            var openingHealth = patient.HealthBasisPoints;
            var closingHealth = Math.Max(
                openingHealth,
                targetHealth);
            var skillGain = Math.Min(
                10_000 - skillBefore,
                Math.Max(
                    1,
                    4 +
                    (targetHealth -
                     Math.Min(
                         targetHealth,
                         openingHealth)) / 500));
            var skillAfter = checked(skillBefore + skillGain);
            var treatment = new MilitaryRearMedicalTreatmentState
            {
                Id = $"military_rear_medical_treatment.{world.AbsoluteDay}." +
                    $"{world.MilitaryRearMedicalTreatments.Count:D6}",
                Day = world.AbsoluteDay,
                AdmissionId = admission.Id,
                EvacuationId = evacuation.Id,
                RearMedicalSiteId = site.Id,
                PatientPersonId = patient.Id,
                PatientMilitaryServiceId =
                    admission.PatientMilitaryServiceId,
                PhysicianPersonId = physician.Id,
                TreatmentProtocolId = treatmentProtocolId,
                MedicineProductDefinitionId = batch.ProductDefinitionId,
                SourceMedicineBatchId = batch.Id,
                MedicineUnitsConsumed = medicineUnits,
                WorkMinutes = workMinutes,
                OpeningHealthBasisPoints = openingHealth,
                ClosingHealthBasisPoints = closingHealth,
                RecoveredHealthBasisPoints = closingHealth - openingHealth,
                PhysicianMedicalSkillBeforeBasisPoints = skillBefore,
                PhysicianMedicalSkillAfterBasisPoints = skillAfter,
                PhysicianMedicalSkillGainBasisPoints = skillGain,
                StageIndex = stageIndex,
                RequiredStageCount = admission.RequiredTreatmentStages
            };
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.MilitaryRearMedicalTreatmentConsumed,
                physician.Id,
                string.Empty,
                0,
                0,
                -checked(batch.UnitWeight * medicineUnits),
                $"Consumed rear-clinic medicine for {treatment.Id}.");
            transaction.SourceMilitaryRearMedicalTreatmentId = treatment.Id;
            transaction.Lines.Add(ProductInventorySystem.Line(
                batch,
                -medicineUnits,
                consumesTransferReservation ? -medicineUnits : 0));
            treatment.InventoryTransactionId = transaction.Id;

            var writablePatient = people.GetRequiredForUpdate(patient.Id);
            writablePatient.HealthBasisPoints = closingHealth;
            if (surgery)
            {
                surgeryEpisode.LaborCapacityBeforeBasisPoints =
                    writablePatient.LaborCapacityBasisPoints;
                writablePatient.LaborCapacityBasisPoints = Math.Max(
                    0,
                    writablePatient.LaborCapacityBasisPoints -
                        permanentPenalty);
                writablePatient.PermanentLaborCapacityPenaltyBasisPoints =
                    checked(
                        writablePatient
                            .PermanentLaborCapacityPenaltyBasisPoints +
                        permanentPenalty);
                surgeryEpisode.LaborCapacityAfterBasisPoints =
                    writablePatient.LaborCapacityBasisPoints;
                surgeryEpisode.PermanentLaborCapacityPenaltyBasisPoints =
                    permanentPenalty;
                surgeryEpisode.PermanentOutcomeId = permanentImpairment
                    ? MilitaryInjuryOutcomeIds.PermanentMobilityImpairment
                    : MilitaryInjuryOutcomeIds.NoPermanentImpairment;
                surgeryEpisode.RequiresMedicalRetirement =
                    permanentImpairment;
                surgeryEpisode.SurgeryTreatmentId = treatment.Id;
                surgeryEpisode.SurgeryCompletedDay = world.AbsoluteDay;
            }
            var writablePhysician = people.GetRequiredForUpdate(physician.Id);
            writablePhysician.ProfessionalSkills ??=
                new ProfessionalSkillState();
            writablePhysician.ProfessionalSkills.Medicine = Math.Max(
                writablePhysician.ProfessionalSkills.Medicine, skillAfter);
            writablePhysician.MedicalSkillBasisPoints = Math.Max(
                writablePhysician.MedicalSkillBasisPoints, skillAfter);
            batch.Quantity = checked(
                batch.Quantity - medicineUnits);
            if (consumesTransferReservation)
            {
                batch.ReservedQuantity = checked(
                    batch.ReservedQuantity - medicineUnits);
                transfer.ConsumedReservedMedicineUnits = checked(
                    transfer.ConsumedReservedMedicineUnits + medicineUnits);
            }
            world.InventoryTransactions.Add(transaction);
            world.MilitaryRearMedicalTreatments.Add(treatment);
            if (infectionControl)
            {
                infectionEpisode.InfectionStatus =
                    MilitaryInfectionStatus.Controlled;
                infectionEpisode.InfectionControlTreatmentId = treatment.Id;
                infectionEpisode.InfectionControlledDay = world.AbsoluteDay;
            }
            admission.TreatmentIds ??= new List<string>();
            admission.TreatmentIds.Add(treatment.Id);
            admission.TreatmentId = treatment.Id;
            admission.CompletedTreatmentStages++;
            if (admission.CompletedTreatmentStages ==
                admission.RequiredTreatmentStages)
            {
                admission.Status =
                    MilitaryRearMedicalAdmissionStatus.ReadyForReturn;
                admission.ReadyForReturnDay = world.AbsoluteDay;
                var finalInjury = FindInjuryEpisode(
                    world, admission.InjuryEpisodeId);
                admission.DischargePolicyId =
                    finalInjury.RequiresMedicalRetirement
                        ? MilitaryRearMedicalDischargePolicyIds
                            .MedicalRetirementAtCareSite
                        : MilitaryRearMedicalDischargePolicyIds
                            .ReturnToSourceArmy;
                evacuation.PatientReturnPolicyId =
                    finalInjury.RequiresMedicalRetirement
                        ? MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .RemainAtCareSiteForMedicalRetirement
                        : MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .ReturnWithTeam;
                evacuation.Status =
                    MilitaryMedicalEvacuationStatus.ReadyForReturn;
            }
            world.Validate();
            return treatment;
        }

        public void StartReturn(
            WorldState world,
            StableId evacuationId,
            StableId routeId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var evacuation = FindEvacuation(world, evacuationId.Value);
            var originalEvacuationDeath = !string.IsNullOrEmpty(
                evacuation.OriginalEvacuationDeathClosureId);
            var admission = originalEvacuationDeath
                ? null
                : FindAdmission(world, evacuation.RearMedicalAdmissionId);
            var returnOriginLocationId = originalEvacuationDeath
                ? evacuation.DestinationLocationId
                : FindSite(world, evacuation.RearMedicalSiteId).LocationId;
            var army = FindArmy(world, evacuation.SourceArmyId);
            var route = FindRoute(world, routeId.Value);
            var dischargedAfterInpatientDeath = admission != null &&
                admission.Status ==
                    MilitaryRearMedicalAdmissionStatus.Discharged &&
                (!string.IsNullOrEmpty(admission.InpatientDeathClosureId) ||
                 !string.IsNullOrEmpty(
                     admission.MedicalTransferDeathClosureId));
            if (evacuation.Status !=
                    MilitaryMedicalEvacuationStatus.ReadyForReturn ||
                !originalEvacuationDeath &&
                    admission.Status !=
                        MilitaryRearMedicalAdmissionStatus.ReadyForReturn &&
                    !dischargedAfterInpatientDeath ||
                IsArmyMarching(world, army.Id))
            {
                throw new InvalidOperationException(
                    "The treated evacuation cannot begin its return.");
            }
            ValidateRoute(route, returnOriginLocationId, army.LocationId);
            var people = PeopleFor(world);
            var patient = people.GetRequired(evacuation.PatientPersonId);
            var teamPeople = new List<PersonState>();
            var patientReturns = evacuation.PatientReturnPolicyId ==
                MilitaryMedicalEvacuationPatientReturnPolicyIds.ReturnWithTeam;
            var patientRemainsAfterDeath =
                evacuation.PatientReturnPolicyId ==
                MilitaryMedicalEvacuationPatientReturnPolicyIds
                    .RemainAtCareSiteAfterDeath;
            if (patientReturns &&
                !EligibleReturnPerson(
                    world, patient, returnOriginLocationId) ||
                patientRemainsAfterDeath &&
                (patient.IsAlive ||
                 patient.LocationId != returnOriginLocationId ||
                 IsPersonTraveling(world, patient.Id)) ||
                !patientReturns && !patientRemainsAfterDeath &&
                (!patient.IsAlive ||
                 patient.LocationId != returnOriginLocationId ||
                 IsPersonTraveling(world, patient.Id)))
            {
                throw new InvalidOperationException(
                    "The patient is not available for the return journey.");
            }
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = people.GetRequired(
                    evacuation.TeamMembers[i].PersonId);
                if (!EligibleReturnPerson(
                    world, member, returnOriginLocationId))
                {
                    throw new InvalidOperationException(
                        "Every evacuation team member must be available to return.");
                }
                teamPeople.Add(member);
            }

            var travel = new TravelSystem(people);
            evacuation.PatientReturnJourneyId = patientReturns
                ? travel.StartJourneyWithoutValidation(
                    world,
                    new StableId(patient.Id),
                    new StableId(route.Id),
                    new StableId(army.LocationId),
                    TravelMode.Foot).Id
                : string.Empty;
            for (var i = 0; i < teamPeople.Count; i++)
            {
                evacuation.TeamMembers[i].ReturnJourneyId =
                    travel.StartJourneyWithoutValidation(
                        world,
                        new StableId(teamPeople[i].Id),
                        new StableId(route.Id),
                        new StableId(army.LocationId),
                        TravelMode.Foot).Id;
            }
            evacuation.ReturnRouteId = route.Id;
            evacuation.ReturnDestinationLocationId = army.LocationId;
            evacuation.ReturnStartedDay = world.AbsoluteDay;
            evacuation.Status =
                MilitaryMedicalEvacuationStatus.ReturningToArmy;
            if (admission != null && admission.Status ==
                MilitaryRearMedicalAdmissionStatus.ReadyForReturn)
            {
                admission.Status =
                    MilitaryRearMedicalAdmissionStatus.Discharged;
                admission.DischargedDay = world.AbsoluteDay;
            }
            world.Validate();
        }

        internal static void ResolveReturnsWithoutValidation(WorldState world)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                if (evacuation.Status !=
                        MilitaryMedicalEvacuationStatus.ReturningToArmy &&
                    evacuation.Status != MilitaryMedicalEvacuationStatus
                        .PatientDeceasedReturningToArmy &&
                    evacuation.Status != MilitaryMedicalEvacuationStatus
                        .PatientDeceasedAwaitingTeamRejoin)
                {
                    continue;
                }
                var patientReturnJourney = FindJourney(
                    world, evacuation.PatientReturnJourneyId);
                var allArrived = true;
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    var member = evacuation.TeamMembers[memberIndex];
                    var returnJourney = FindJourney(
                        world, member.ReturnJourneyId);
                    if (!string.IsNullOrEmpty(member.ReturnDeathId))
                    {
                        var returnDeath = FindReturnTeamDeath(
                            world, member.ReturnDeathId);
                        if (returnJourney == null &&
                            returnDeath.CorpseArrivedDay < 0)
                        {
                            returnDeath.CorpseArrivedDay = world.AbsoluteDay;
                        }
                    }
                    if (returnJourney != null)
                    {
                        allArrived = false;
                    }
                }
                if (patientReturnJourney != null || !allArrived)
                {
                    continue;
                }

                var army = FindArmy(world, evacuation.SourceArmyId);
                if (army.LocationId != evacuation.ReturnDestinationLocationId)
                {
                    throw new InvalidOperationException(
                        "The source army moved away from its returning medical party.");
                }
                var patientService = FindService(
                    world, evacuation.PatientMilitaryServiceId);
                patientService.Status =
                    evacuation.PatientReturnPolicyId ==
                        MilitaryMedicalEvacuationPatientReturnPolicyIds
                            .RemainAtCareSiteAfterDeath
                        || evacuation.PatientReturnPolicyId ==
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .ReturnCorpseWithTeam
                        || evacuation.PatientReturnPolicyId ==
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .CorpseAtArmyAwaitingTeamRejoin
                        ? MilitaryServiceStatus.Dead
                        : evacuation.PatientReturnPolicyId ==
                            MilitaryMedicalEvacuationPatientReturnPolicyIds
                                .RemainAtCareSiteForMedicalRetirement
                            ? MilitaryServiceStatus.Retired
                            : MilitaryServiceStatus.Active;
                patientService.LastStatusChangeDay = world.AbsoluteDay;
                for (var memberIndex = 0;
                     memberIndex < evacuation.TeamMembers.Count;
                     memberIndex++)
                {
                    var memberService = FindService(
                        world,
                        evacuation.TeamMembers[memberIndex]
                            .MilitaryServiceId);
                    memberService.Status = string.IsNullOrEmpty(
                            evacuation.TeamMembers[memberIndex].ReturnDeathId)
                        ? MilitaryServiceStatus.Active
                        : MilitaryServiceStatus.Dead;
                    memberService.LastStatusChangeDay = world.AbsoluteDay;
                }
                if (string.IsNullOrEmpty(
                    evacuation.OriginalEvacuationDeathClosureId))
                {
                    var admission = FindAdmission(
                        world, evacuation.RearMedicalAdmissionId);
                    admission.Status =
                        MilitaryRearMedicalAdmissionStatus.Completed;
                    admission.CompletedDay = world.AbsoluteDay;
                }
                evacuation.Status = MilitaryMedicalEvacuationStatus.Completed;
                evacuation.CompletedDay = world.AbsoluteDay;
                new MilitaryServiceSystem().SynchronizeArmyCaches(
                    world, army.Id);
            }
        }

        public static bool HasReturningEvacuationForArmy(
            WorldState world,
            string armyId)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                var evacuation = world.MilitaryMedicalEvacuations[i];
                if (evacuation.SourceArmyId == armyId &&
                    (evacuation.Status ==
                         MilitaryMedicalEvacuationStatus.ReturningToArmy ||
                     evacuation.Status == MilitaryMedicalEvacuationStatus
                         .PatientDeceasedReturningToArmy ||
                     evacuation.Status == MilitaryMedicalEvacuationStatus
                         .PatientDeceasedAwaitingTeamRejoin))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<string> BuildTreatmentPlan(
            MilitaryRearMedicalSiteState site)
        {
            var plan = new List<string>();
            if (site.KindId == MilitaryRearMedicalSiteKindIds.FieldHospital)
            {
                plan.Add(MilitaryRearMedicalTreatmentProtocolIds
                    .FieldStabilization);
                plan.Add(MilitaryRearMedicalTreatmentProtocolIds.FieldRecovery);
            }
            else
            {
                plan.Add(MilitaryRearMedicalTreatmentProtocolIds
                    .InpatientHerbalRecovery);
            }
            return plan;
        }

        private static MilitaryInjuryEpisodeState AssessInjury(
            WorldState world,
            MilitaryMedicalEvacuationState evacuation,
            PersonState patient,
            string admissionId)
        {
            var health = Math.Max(0, Math.Min(10_000,
                patient.HealthBasisPoints));
            var severity = 10_000 - health;
            var transitDays = checked((int)Math.Max(
                0, evacuation.ReceivedDay - evacuation.CreatedDay));
            var contamination = Math.Min(
                10_000,
                checked(severity / 2 + transitDays * 400));
            var profile = MilitaryInjuryProfileCatalog.Select(
                world.MilitaryInjuryProfiles, health);
            MilitarySurgicalProcedureDefinitionState procedure = null;
            if (!string.IsNullOrEmpty(profile.SurgicalProcedureId))
            {
                procedure = FindSurgicalProcedure(
                    world, profile.SurgicalProcedureId);
            }
            var infection = contamination >=
                MilitaryMedicalRules.InfectionRiskThresholdBasisPoints;
            return new MilitaryInjuryEpisodeState
            {
                Id = $"military_injury_episode.{world.AbsoluteDay}." +
                    $"{world.MilitaryInjuryEpisodes.Count:D6}",
                EvacuationId = evacuation.Id,
                AdmissionId = admissionId,
                PatientPersonId = patient.Id,
                PatientMilitaryServiceId =
                    evacuation.PatientMilitaryServiceId,
                InjuryProfileId = profile.Id,
                AssessedDay = world.AbsoluteDay,
                AdmissionHealthBasisPoints = health,
                SeverityBasisPoints = severity,
                TransitDays = transitDays,
                ContaminationBasisPoints = contamination,
                InfectionRiskBasisPoints = contamination,
                InfectionStatus = infection
                    ? MilitaryInfectionStatus.Active
                    : MilitaryInfectionStatus.AtRisk,
                SurgicalProcedureId = world.AbsoluteDay >=
                    world.MilitarySurgeryContractActivationDay &&
                    procedure != null &&
                    severity >= procedure.MinimumSeverityBasisPoints
                        ? procedure.Id
                        : string.Empty
            };
        }

        private static MilitarySurgicalProcedureDefinitionState
            FindSurgicalProcedure(WorldState world, string id)
        {
            for (var i = 0; i < world.MilitarySurgicalProcedures.Count; i++)
            {
                if (world.MilitarySurgicalProcedures[i].Id == id)
                {
                    return world.MilitarySurgicalProcedures[i];
                }
            }
            throw new InvalidOperationException(
                $"Military surgical procedure {id} is missing.");
        }

        private static MilitaryInjuryEpisodeState FindInjuryEpisode(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryInjuryEpisodes.Count; i++)
            {
                if (world.MilitaryInjuryEpisodes[i].Id == id)
                {
                    return world.MilitaryInjuryEpisodes[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military injury episode {id}.");
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            return _people ?? new WorldStatePersonRepository(world);
        }

        private static int OccupiedBeds(WorldState world, string siteId)
        {
            var count = 0;
            for (var i = 0; i < world.MilitaryRearMedicalAdmissions.Count; i++)
            {
                var admission = world.MilitaryRearMedicalAdmissions[i];
                var activeTransfer = FindMedicalTransfer(
                    world, admission.MedicalTransferId);
                if (admission.RearMedicalSiteId == siteId &&
                    (activeTransfer == null ||
                     activeTransfer.Status ==
                        MilitaryMedicalTransferStatus.Completed) &&
                    (admission.Status ==
                         MilitaryRearMedicalAdmissionStatus.InTreatment ||
                     admission.Status ==
                         MilitaryRearMedicalAdmissionStatus.ReadyForReturn))
                {
                    count++;
                }
            }
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = world.MilitaryMedicalTransfers[i];
                if (transfer.DestinationRearMedicalSiteId == siteId &&
                    (transfer.Status ==
                         MilitaryMedicalTransferStatus.InTransit ||
                     transfer.Status ==
                         MilitaryMedicalTransferStatus.AwaitingReception))
                {
                    count++;
                }
            }
            return count;
        }

        private static MilitaryMedicalTransferState FindMedicalTransfer(
            WorldState world,
            string transferId)
        {
            if (string.IsNullOrEmpty(transferId))
            {
                return null;
            }
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                if (world.MilitaryMedicalTransfers[i].Id == transferId)
                {
                    return world.MilitaryMedicalTransfers[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military medical transfer {transferId}.");
        }

        private static ProductBatchState FindProductBatch(
            WorldState world,
            string batchId)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == batchId)
                {
                    return world.ProductBatches[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing product batch {batchId}.");
        }

        private ProductBatchState FindMedicineBatch(
            WorldState world,
            MilitaryRearMedicalSiteState site,
            int requiredUnits)
        {
            _content.ValidateManifest(world.ProductionContentManifest);
            ProductBatchState selected = null;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId !=
                        site.MedicineInventoryContainerId ||
                    batch.OwnerOrganizationId != site.OwnerOrganizationId ||
                    batch.ProductDefinitionId != CoreProductionContent
                        .HerbalMedicineMaterialProductId ||
                    batch.Quantity - batch.ReservedQuantity < requiredUnits)
                {
                    continue;
                }
                if (selected == null || batch.ProducedDay < selected.ProducedDay ||
                    batch.ProducedDay == selected.ProducedDay &&
                    string.CompareOrdinal(batch.Id, selected.Id) < 0)
                {
                    selected = batch;
                }
            }
            return selected;
        }

        private static int PhysicianWorkMinutesOnDay(
            WorldState world,
            string physicianId,
            long day)
        {
            var minutes = 0;
            for (var i = 0; i < world.CivilianMedicalServices.Count; i++)
            {
                var service = world.CivilianMedicalServices[i];
                if (service.PhysicianPersonId == physicianId &&
                    service.Day == day)
                {
                    minutes = checked(minutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryMedicalServices.Count; i++)
            {
                var service = world.MilitaryMedicalServices[i];
                if (service.PhysicianPersonId == physicianId &&
                    service.Day == day)
                {
                    minutes = checked(minutes + service.WorkMinutes);
                }
            }
            for (var i = 0; i < world.MilitaryRearMedicalTreatments.Count; i++)
            {
                var treatment = world.MilitaryRearMedicalTreatments[i];
                if (treatment.PhysicianPersonId == physicianId &&
                    treatment.Day == day)
                {
                    minutes = checked(minutes + treatment.WorkMinutes);
                }
            }
            return minutes;
        }

        private static bool CanManageOrganization(
            WorldState world,
            string personId,
            OrganizationState organization)
        {
            if (organization.LeaderPersonId == personId)
            {
                return true;
            }
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == personId &&
                    world.Memberships[i].OrganizationId == organization.Id)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool EligibleReturnPerson(
            WorldState world,
            PersonState person,
            string locationId)
        {
            return person.IsAlive && person.LocationId == locationId &&
                !IsPersonTraveling(world, person.Id);
        }

        private static int EffectiveMedicalSkill(PersonState person)
        {
            return Math.Max(
                person.MedicalSkillBasisPoints,
                person.ProfessionalSkills == null
                    ? 0
                    : person.ProfessionalSkills.Medicine);
        }

        private static bool IsPersonTraveling(WorldState world, string personId)
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

        private static bool IsArmyMarching(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ValidateRoute(
            RouteState route,
            string originLocationId,
            string destinationLocationId)
        {
            var forward = route.FromLocationId == originLocationId &&
                route.ToLocationId == destinationLocationId;
            var backward = route.Bidirectional &&
                route.ToLocationId == originLocationId &&
                route.FromLocationId == destinationLocationId;
            if (!forward && !backward)
            {
                throw new InvalidOperationException(
                    "The route does not connect the rear site and source army.");
            }
        }

        private static MilitaryMedicalEvacuationState FindEvacuation(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                if (world.MilitaryMedicalEvacuations[i].Id == id)
                {
                    return world.MilitaryMedicalEvacuations[i];
                }
            }
            throw new InvalidOperationException($"Missing evacuation {id}.");
        }

        private static MilitaryRearMedicalSiteState FindSite(
            WorldState world,
            string id)
        {
            return FindSiteOrNull(world, id) ??
                throw new InvalidOperationException(
                    $"Missing rear medical site {id}.");
        }

        private static MilitaryRearMedicalSiteState FindSiteOrNull(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                if (world.MilitaryRearMedicalSites[i].Id == id)
                {
                    return world.MilitaryRearMedicalSites[i];
                }
            }
            return null;
        }

        private static MilitaryRearMedicalAdmissionState FindAdmission(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryRearMedicalAdmissions.Count; i++)
            {
                if (world.MilitaryRearMedicalAdmissions[i].Id == id)
                {
                    return world.MilitaryRearMedicalAdmissions[i];
                }
            }
            throw new InvalidOperationException($"Missing admission {id}.");
        }

        private static MilitaryServiceState FindService(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                if (world.MilitaryServices[i].Id == id)
                {
                    return world.MilitaryServices[i];
                }
            }
            throw new InvalidOperationException($"Missing service {id}.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == id)
                {
                    return world.Armies[i];
                }
            }
            throw new InvalidOperationException($"Missing army {id}.");
        }

        private static RouteState FindRoute(WorldState world, string id)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == id)
                {
                    return world.Routes[i];
                }
            }
            throw new InvalidOperationException($"Missing route {id}.");
        }

        private static JourneyState FindJourney(WorldState world, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].Id == id)
                {
                    return world.Journeys[i];
                }
            }
            return null;
        }

        private static MilitaryReturnTeamDeathState FindReturnTeamDeath(
            WorldState world, string id)
        {
            var result = world.MilitaryReturnTeamDeaths.Find(
                item => item.Id == id);
            return result ?? throw new InvalidOperationException(
                $"Missing military return-team death {id}.");
        }

        private static LocationState FindLocation(WorldState world, string id)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == id)
                {
                    return world.Locations[i];
                }
            }
            throw new InvalidOperationException($"Missing location {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == id)
                {
                    return world.Organizations[i];
                }
            }
            throw new InvalidOperationException($"Missing organization {id}.");
        }

        private static InventoryContainerState FindContainerOrNull(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return world.InventoryContainers[i];
                }
            }
            return null;
        }
    }

    public sealed class MilitaryMedicalTransferSystem
    {
        private readonly IPersonRepository _people;

        public MilitaryMedicalTransferSystem(IPersonRepository people = null)
        {
            _people = people;
        }

        public MilitaryMedicalTransferState Dispatch(
            WorldState world,
            StableId authorizingPersonId,
            StableId admissionId,
            StableId destinationRearMedicalSiteId,
            StableId routeId,
            StableId designatedReceivingPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var admission = FindAdmission(world, admissionId.Value);
            var evacuation = FindEvacuation(world, admission.EvacuationId);
            var sourceSite = FindSite(world, admission.RearMedicalSiteId);
            var destinationSite = FindSite(
                world, destinationRearMedicalSiteId.Value);
            var army = FindArmy(world, evacuation.SourceArmyId);
            var route = FindRoute(world, routeId.Value);
            var people = _people ?? new WorldStatePersonRepository(world);
            var patient = people.GetRequired(admission.PatientPersonId);
            var receiver = people.GetRequired(
                designatedReceivingPersonId.Value);
            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, new StableId(army.Id));
            var previousTransfer = string.IsNullOrEmpty(
                    admission.MedicalTransferId)
                ? null
                : FindTransfer(world, admission.MedicalTransferId);
            var repeatedTransfer = previousTransfer != null;

            if (world.AbsoluteDay <
                    world.MilitaryMedicalTransferContractActivationDay ||
                admission.Status !=
                    MilitaryRearMedicalAdmissionStatus.InTreatment ||
                admission.CompletedTreatmentStages < 0 ||
                admission.CompletedTreatmentStages >=
                    admission.RequiredTreatmentStages ||
                admission.CompletedTreatmentStages > 0 &&
                    world.AbsoluteDay < world
                        .MilitaryPostTreatmentTransferContractActivationDay ||
                repeatedTransfer &&
                    (world.AbsoluteDay < world
                         .MilitaryRepeatedMedicalTransferContractActivationDay ||
                     previousTransfer.Status !=
                         MilitaryMedicalTransferStatus.Completed ||
                     !string.IsNullOrEmpty(
                         previousTransfer.NextMedicalTransferId) ||
                     previousTransfer.SequenceIndex + 1 >=
                         MilitaryMedicalRules
                             .MaximumMedicalTransfersPerAdmission) ||
                evacuation.Status != MilitaryMedicalEvacuationStatus.Admitted ||
                evacuation.CurrentCareLocationId != sourceSite.LocationId ||
                sourceSite.Id == destinationSite.Id ||
                sourceSite.OwnerOrganizationId !=
                    destinationSite.OwnerOrganizationId ||
                !sourceSite.IsOperational || !destinationSite.IsOperational ||
                authority < MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "The admission is not eligible for this medical transfer.");
            }
            ValidateRoute(route, sourceSite.LocationId, destinationSite.LocationId);
            if (!patient.IsAlive || patient.LocationId != sourceSite.LocationId ||
                IsPersonTraveling(world, patient.Id))
            {
                throw new InvalidOperationException(
                    "The patient is unavailable at the source medical site.");
            }
            if (!receiver.IsAlive ||
                receiver.LocationId != destinationSite.LocationId ||
                IsPersonTraveling(world, receiver.Id) ||
                EffectiveMedicalSkill(receiver) <
                    MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The designated receiving practitioner is unavailable or unqualified.");
            }
            if (OccupiedBeds(world, destinationSite.Id) >=
                destinationSite.BedCapacity)
            {
                throw new InvalidOperationException(
                    "The destination medical site has no reservable bed.");
            }

            var teamPeople = new List<PersonState>();
            for (var i = 0; i < evacuation.TeamMembers.Count; i++)
            {
                var member = people.GetRequired(
                    evacuation.TeamMembers[i].PersonId);
                if (!member.IsAlive ||
                    member.LocationId != sourceSite.LocationId ||
                    IsPersonTraveling(world, member.Id))
                {
                    throw new InvalidOperationException(
                        "Every evacuation team member must be available at the source site.");
                }
                teamPeople.Add(member);
            }

            var requiredMedicineUnits = RequiredRemainingMedicineUnits(
                world, admission);
            var reservedBatch = FindMedicineBatch(
                world, destinationSite, requiredMedicineUnits);
            if (reservedBatch == null)
            {
                throw new InvalidOperationException(
                    "The destination medical site lacks reservable medicine for the frozen treatment plan.");
            }

            ProductBatchState previousReservedBatch = null;
            InventoryTransactionState previousReleaseTransaction = null;
            var previousReleaseUnits = 0;
            if (repeatedTransfer)
            {
                previousReservedBatch = FindProductBatch(
                    world, previousTransfer.ReservedMedicineBatchId);
                previousReleaseUnits = checked(
                    previousTransfer.ReservedMedicineUnits -
                    previousTransfer.ConsumedReservedMedicineUnits -
                    previousTransfer.ReleasedReservedMedicineUnits);
                if (previousReleaseUnits <= 0 ||
                    previousReleaseUnits != requiredMedicineUnits ||
                    previousReservedBatch.ReservedQuantity <
                        previousReleaseUnits ||
                    !string.IsNullOrEmpty(previousTransfer
                        .ReservationReleaseInventoryTransactionId))
                {
                    throw new InvalidOperationException(
                        "The previous medical-transfer reservation cannot be closed for onward transfer.");
                }
                previousReleaseTransaction =
                    ProductInventorySystem.NewTransaction(
                        world,
                        InventoryTransactionType
                            .MilitaryMedicalTransferMedicineReleased,
                        admission.PhysicianPersonId,
                        string.Empty,
                        0,
                        0,
                        0,
                        $"Released medicine for onward transfer from {previousTransfer.Id}.");
                previousReleaseTransaction.SourceMilitaryMedicalTransferId =
                    previousTransfer.Id;
                previousReleaseTransaction.Lines.Add(
                    ProductInventorySystem.Line(
                        previousReservedBatch, 0, -previousReleaseUnits));
            }

            var transfer = new MilitaryMedicalTransferState
            {
                Id = $"military_medical_transfer.{world.AbsoluteDay}." +
                    $"{world.MilitaryMedicalTransfers.Count:D6}",
                SequenceIndex = repeatedTransfer
                    ? previousTransfer.SequenceIndex + 1
                    : 0,
                PreviousMedicalTransferId = repeatedTransfer
                    ? previousTransfer.Id
                    : string.Empty,
                NextMedicalTransferId = string.Empty,
                CreatedDay = world.AbsoluteDay,
                EvacuationId = evacuation.Id,
                AdmissionId = admission.Id,
                SourceRearMedicalSiteId = sourceSite.Id,
                DestinationRearMedicalSiteId = destinationSite.Id,
                SourcePhysicianPersonId = admission.PhysicianPersonId,
                DesignatedReceivingPersonId = receiver.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                RouteId = route.Id,
                ReservedMedicineBatchId = reservedBatch.Id,
                ReservedMedicineUnits = requiredMedicineUnits,
                CompletedTreatmentStagesAtDispatch =
                    admission.CompletedTreatmentStages
            };
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType.MilitaryMedicalTransferMedicineReserved,
                receiver.Id,
                string.Empty,
                0,
                0,
                0,
                $"Reserved destination medicine for {transfer.Id}.");
            transaction.SourceMilitaryMedicalTransferId = transfer.Id;
            if (repeatedTransfer)
            {
                previousReleaseTransaction.Id =
                    $"inventory_transaction.{world.AbsoluteDay}." +
                    $"{world.InventoryTransactions.Count:D6}";
                transaction.Id =
                    $"inventory_transaction.{world.AbsoluteDay}." +
                    $"{world.InventoryTransactions.Count + 1:D6}";
            }
            transaction.Lines.Add(ProductInventorySystem.Line(
                reservedBatch, 0, requiredMedicineUnits));
            transfer.ReservationInventoryTransactionId = transaction.Id;

            var travel = new TravelSystem(people);
            transfer.PatientJourneyId = travel.StartJourneyWithoutValidation(
                world,
                new StableId(patient.Id),
                new StableId(route.Id),
                new StableId(destinationSite.LocationId),
                TravelMode.Foot).Id;
            for (var i = 0; i < teamPeople.Count; i++)
            {
                var journey = travel.StartJourneyWithoutValidation(
                    world,
                    new StableId(teamPeople[i].Id),
                    new StableId(route.Id),
                    new StableId(destinationSite.LocationId),
                    TravelMode.Foot);
                transfer.TeamMembers.Add(
                    new MilitaryMedicalTransferTeamMemberState
                    {
                        PersonId = teamPeople[i].Id,
                        MilitaryServiceId =
                            evacuation.TeamMembers[i].MilitaryServiceId,
                        JourneyId = journey.Id
                    });
            }
            reservedBatch.ReservedQuantity = checked(
                reservedBatch.ReservedQuantity + requiredMedicineUnits);
            if (repeatedTransfer)
            {
                previousReservedBatch.ReservedQuantity = checked(
                    previousReservedBatch.ReservedQuantity -
                    previousReleaseUnits);
                previousTransfer.ReleasedReservedMedicineUnits = checked(
                    previousTransfer.ReleasedReservedMedicineUnits +
                    previousReleaseUnits);
                previousTransfer.ReservationReleaseInventoryTransactionId =
                    previousReleaseTransaction.Id;
                previousTransfer.NextMedicalTransferId = transfer.Id;
                world.InventoryTransactions.Add(previousReleaseTransaction);
            }
            admission.MedicalTransferId = transfer.Id;
            world.InventoryTransactions.Add(transaction);
            world.MilitaryMedicalTransfers.Add(transfer);
            world.Validate();
            return transfer;
        }

        public void Receive(
            WorldState world,
            StableId transferId,
            StableId receivingPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var transfer = FindTransfer(world, transferId.Value);
            var admission = FindAdmission(world, transfer.AdmissionId);
            var evacuation = FindEvacuation(world, transfer.EvacuationId);
            var destinationSite = FindSite(
                world, transfer.DestinationRearMedicalSiteId);
            var people = _people ?? new WorldStatePersonRepository(world);
            var receiver = people.GetRequired(receivingPersonId.Value);
            if (transfer.Status !=
                    MilitaryMedicalTransferStatus.AwaitingReception ||
                transfer.DesignatedReceivingPersonId != receiver.Id ||
                !destinationSite.IsOperational || !receiver.IsAlive ||
                receiver.LocationId != destinationSite.LocationId ||
                IsPersonTraveling(world, receiver.Id) ||
                EffectiveMedicalSkill(receiver) <
                    MilitaryMedicalRules.MinimumPhysicianSkillBasisPoints)
            {
                throw new InvalidOperationException(
                    "The transfer is not awaiting this available practitioner.");
            }

            transfer.Status = MilitaryMedicalTransferStatus.Completed;
            transfer.ReceivingPersonId = receiver.Id;
            transfer.ReceivingMedicalSkillBasisPoints =
                EffectiveMedicalSkill(receiver);
            transfer.ReceivedDay = world.AbsoluteDay;
            transfer.ResponsibilityTransferredDay = world.AbsoluteDay;
            admission.RearMedicalSiteId = destinationSite.Id;
            admission.PhysicianPersonId = receiver.Id;
            evacuation.RearMedicalSiteId = destinationSite.Id;
            evacuation.CurrentCareLocationId = destinationSite.LocationId;
            world.Validate();
        }

        internal static void ResolveArrivalsWithoutValidation(WorldState world)
        {
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = world.MilitaryMedicalTransfers[i];
                if ((transfer.Status !=
                         MilitaryMedicalTransferStatus.InTransit &&
                     transfer.Status !=
                         MilitaryMedicalTransferStatus.DeceasedInTransit) ||
                    FindJourney(world, transfer.PatientJourneyId) != null)
                {
                    continue;
                }
                var arrived = true;
                for (var memberIndex = 0;
                     memberIndex < transfer.TeamMembers.Count;
                     memberIndex++)
                {
                    if (FindJourney(
                            world,
                            transfer.TeamMembers[memberIndex].JourneyId) != null)
                    {
                        arrived = false;
                        break;
                    }
                }
                if (arrived)
                {
                    transfer.ArrivedDay = world.AbsoluteDay;
                    if (transfer.Status ==
                        MilitaryMedicalTransferStatus.DeceasedInTransit)
                    {
                        var admission = FindAdmission(
                            world, transfer.AdmissionId);
                        var evacuation = FindEvacuation(
                            world, transfer.EvacuationId);
                        var destination = FindSite(
                            world, transfer.DestinationRearMedicalSiteId);
                        transfer.Status = MilitaryMedicalTransferStatus
                            .ClosedAfterPatientDeath;
                        admission.RearMedicalSiteId = destination.Id;
                        evacuation.RearMedicalSiteId = destination.Id;
                        evacuation.CurrentCareLocationId =
                            destination.LocationId;
                        evacuation.Status = MilitaryMedicalEvacuationStatus
                            .ReadyForReturn;
                    }
                    else
                    {
                        transfer.Status = MilitaryMedicalTransferStatus
                            .AwaitingReception;
                    }
                }
            }
        }

        private static int RequiredRemainingMedicineUnits(
            WorldState world,
            MilitaryRearMedicalAdmissionState admission)
        {
            var total = 0;
            for (var i = admission.CompletedTreatmentStages;
                 i < admission.TreatmentPlanProtocolIds.Count;
                 i++)
            {
                var protocol = admission.TreatmentPlanProtocolIds[i];
                if (protocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.TraumaSurgery)
                {
                    var injury = FindInjury(world, admission.InjuryEpisodeId);
                    var procedure = FindProcedure(
                        world, injury.SurgicalProcedureId);
                    total = checked(total + procedure.MedicineUnits);
                }
                else if (protocol ==
                    MilitaryRearMedicalTreatmentProtocolIds.InfectionControl)
                {
                    total = checked(total +
                        MilitaryMedicalRules.InfectionControlMedicineUnits);
                }
                else
                {
                    total = checked(total +
                        MilitaryMedicalRules.MedicineUnitsPerTreatment);
                }
            }
            return total;
        }

        private static int OccupiedBeds(WorldState world, string siteId)
        {
            var count = 0;
            for (var i = 0; i < world.MilitaryRearMedicalAdmissions.Count; i++)
            {
                var admission = world.MilitaryRearMedicalAdmissions[i];
                var transfer = string.IsNullOrEmpty(admission.MedicalTransferId)
                    ? null
                    : FindTransfer(world, admission.MedicalTransferId);
                if (admission.RearMedicalSiteId == siteId &&
                    (transfer == null || transfer.Status ==
                        MilitaryMedicalTransferStatus.Completed) &&
                    (admission.Status ==
                         MilitaryRearMedicalAdmissionStatus.InTreatment ||
                     admission.Status ==
                         MilitaryRearMedicalAdmissionStatus.ReadyForReturn))
                {
                    count++;
                }
            }
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                var transfer = world.MilitaryMedicalTransfers[i];
                if (transfer.DestinationRearMedicalSiteId == siteId &&
                    (transfer.Status ==
                         MilitaryMedicalTransferStatus.InTransit ||
                     transfer.Status ==
                         MilitaryMedicalTransferStatus.AwaitingReception))
                {
                    count++;
                }
            }
            return count;
        }

        private static ProductBatchState FindMedicineBatch(
            WorldState world,
            MilitaryRearMedicalSiteState site,
            int requiredUnits)
        {
            ProductBatchState selected = null;
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId !=
                        site.MedicineInventoryContainerId ||
                    batch.OwnerOrganizationId != site.OwnerOrganizationId ||
                    batch.ProductDefinitionId != CoreProductionContent
                        .HerbalMedicineMaterialProductId ||
                    batch.Quantity - batch.ReservedQuantity < requiredUnits)
                {
                    continue;
                }
                if (selected == null || batch.ProducedDay < selected.ProducedDay ||
                    batch.ProducedDay == selected.ProducedDay &&
                    string.CompareOrdinal(batch.Id, selected.Id) < 0)
                {
                    selected = batch;
                }
            }
            return selected;
        }

        private static ProductBatchState FindProductBatch(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                if (world.ProductBatches[i].Id == id)
                {
                    return world.ProductBatches[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing product batch {id}.");
        }

        private static MilitaryMedicalTransferState FindTransfer(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryMedicalTransfers.Count; i++)
            {
                if (world.MilitaryMedicalTransfers[i].Id == id)
                {
                    return world.MilitaryMedicalTransfers[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military medical transfer {id}.");
        }

        private static MilitaryRearMedicalAdmissionState FindAdmission(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryRearMedicalAdmissions.Count; i++)
            {
                if (world.MilitaryRearMedicalAdmissions[i].Id == id)
                {
                    return world.MilitaryRearMedicalAdmissions[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military rear medical admission {id}.");
        }

        private static MilitaryMedicalEvacuationState FindEvacuation(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryMedicalEvacuations.Count; i++)
            {
                if (world.MilitaryMedicalEvacuations[i].Id == id)
                {
                    return world.MilitaryMedicalEvacuations[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military medical evacuation {id}.");
        }

        private static MilitaryRearMedicalSiteState FindSite(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                if (world.MilitaryRearMedicalSites[i].Id == id)
                {
                    return world.MilitaryRearMedicalSites[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military rear medical site {id}.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == id)
                {
                    return world.Armies[i];
                }
            }
            throw new InvalidOperationException($"Missing army {id}.");
        }

        private static RouteState FindRoute(WorldState world, string id)
        {
            for (var i = 0; i < world.Routes.Count; i++)
            {
                if (world.Routes[i].Id == id)
                {
                    return world.Routes[i];
                }
            }
            throw new InvalidOperationException($"Missing route {id}.");
        }

        private static JourneyState FindJourney(WorldState world, string id)
        {
            for (var i = 0; i < world.Journeys.Count; i++)
            {
                if (world.Journeys[i].Id == id)
                {
                    return world.Journeys[i];
                }
            }
            return null;
        }

        private static MilitaryInjuryEpisodeState FindInjury(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryInjuryEpisodes.Count; i++)
            {
                if (world.MilitaryInjuryEpisodes[i].Id == id)
                {
                    return world.MilitaryInjuryEpisodes[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military injury episode {id}.");
        }

        private static MilitarySurgicalProcedureDefinitionState FindProcedure(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitarySurgicalProcedures.Count; i++)
            {
                if (world.MilitarySurgicalProcedures[i].Id == id)
                {
                    return world.MilitarySurgicalProcedures[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing military surgical procedure {id}.");
        }

        private static bool IsPersonTraveling(WorldState world, string personId)
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

        private static void ValidateRoute(
            RouteState route,
            string originLocationId,
            string destinationLocationId)
        {
            var forward = route.FromLocationId == originLocationId &&
                route.ToLocationId == destinationLocationId;
            var backward = route.Bidirectional &&
                route.ToLocationId == originLocationId &&
                route.FromLocationId == destinationLocationId;
            if (!forward && !backward)
            {
                throw new InvalidOperationException(
                    $"Route {route.Id} does not connect the transfer endpoints.");
            }
        }

        private static int EffectiveMedicalSkill(PersonState person)
        {
            return Math.Max(
                person.MedicalSkillBasisPoints,
                person.ProfessionalSkills == null
                    ? 0
                    : person.ProfessionalSkills.Medicine);
        }
    }

    public sealed class MilitaryFieldHospitalSystem
    {
        private const string MedicalStoreKindId =
            "inventory_container.military_rear_medical_store";
        private readonly IPersonRepository _people;
        private readonly ProductionContentRegistry _content;

        public MilitaryFieldHospitalSystem(
            IPersonRepository people = null,
            ProductionContentRegistry content = null)
        {
            _people = people;
            _content = content ?? ProductionContentRegistry.CreateCore();
        }

        public MilitaryFieldHospitalConstructionProjectState StartProject(
            WorldState world,
            StableId authorizingPersonId,
            StableId sourceArmyId,
            StableId locationId,
            StableId managerPersonId,
            StableId materialInventoryContainerId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            if (!world.MilitaryMedicalInitialized)
            {
                throw new InvalidOperationException(
                    "Military medicine must be initialized first.");
            }
            var people = PeopleFor(world);
            var army = FindArmy(world, sourceArmyId.Value);
            var location = FindLocation(world, locationId.Value);
            var manager = people.GetRequired(managerPersonId.Value);
            var organization = FindOrganization(world, army.OrganizationId);
            var container = FindContainer(
                world, materialInventoryContainerId.Value);
            var authority = new MilitaryAuthoritySystem().GetAuthority(
                world, authorizingPersonId, sourceArmyId);
            if (authority < MilitaryAuthorityLevel.Army)
            {
                throw new InvalidOperationException(
                    "Army authority is required to build a field hospital.");
            }
            if (army.LocationId != location.Id ||
                IsArmyMarching(world, army.Id) ||
                !EligibleProjectWorker(
                    world, people, manager, army, location.Id) ||
                container.OwnerOrganizationId != organization.Id ||
                !string.IsNullOrEmpty(container.OwnerFamilyId) ||
                !string.IsNullOrEmpty(container.CarrierPersonId) ||
                container.LocationId != location.Id)
            {
                throw new InvalidOperationException(
                    "The army, manager and organization material store must be co-located.");
            }
            if (organization.Treasury <
                MilitaryMedicalRules.FieldHospitalRequiredMoney)
            {
                throw new InvalidOperationException(
                    "The owning organization lacks construction funds.");
            }
            for (var i = 0;
                 i < world.MilitaryFieldHospitalConstructionProjects.Count;
                 i++)
            {
                var existing =
                    world.MilitaryFieldHospitalConstructionProjects[i];
                if (existing.OwnerOrganizationId == organization.Id &&
                    existing.LocationId == location.Id)
                {
                    throw new InvalidOperationException(
                        "This organization already has a field-hospital project here.");
                }
            }
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                var existing = world.MilitaryRearMedicalSites[i];
                if (existing.KindId ==
                        MilitaryRearMedicalSiteKindIds.FieldHospital &&
                    existing.OwnerOrganizationId == organization.Id &&
                    existing.LocationId == location.Id)
                {
                    throw new InvalidOperationException(
                        "This organization already operates a field hospital here.");
                }
            }

            _content.ValidateManifest(world.ProductionContentManifest);
            var timberPlan = PlanConsumption(
                world,
                container,
                CoreProductionContent.TimberMaterialProductId,
                MilitaryMedicalRules.FieldHospitalRequiredTimberUnits);
            var leatherPlan = PlanConsumption(
                world,
                container,
                CoreProductionContent.LeatherMaterialProductId,
                MilitaryMedicalRules.FieldHospitalRequiredLeatherUnits);
            var project = new MilitaryFieldHospitalConstructionProjectState
            {
                Id = $"military_field_hospital_project.{world.AbsoluteDay}." +
                    $"{world.MilitaryFieldHospitalConstructionProjects.Count:D6}",
                ProfileId = MilitaryFieldHospitalConstructionProfileIds
                    .TimberLeatherCamp,
                SourceArmyId = army.Id,
                LocationId = location.Id,
                OwnerOrganizationId = organization.Id,
                AuthorizingPersonId = authorizingPersonId.Value,
                AuthorizingAuthority = authority,
                ManagerPersonId = manager.Id,
                MaterialInventoryContainerId = container.Id,
                RequiredTimberUnits =
                    MilitaryMedicalRules.FieldHospitalRequiredTimberUnits,
                RequiredLeatherUnits =
                    MilitaryMedicalRules.FieldHospitalRequiredLeatherUnits,
                RequiredMoney =
                    MilitaryMedicalRules.FieldHospitalRequiredMoney,
                RequiredLaborDays =
                    MilitaryMedicalRules.FieldHospitalRequiredLaborDays,
                OwnerTreasuryBefore = organization.Treasury,
                OwnerTreasuryAfter = checked(
                    organization.Treasury -
                    MilitaryMedicalRules.FieldHospitalRequiredMoney),
                StartedDay = world.AbsoluteDay
            };
            var totalWeight = checked(
                ConsumptionWeight(timberPlan) +
                ConsumptionWeight(leatherPlan));
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType
                    .MilitaryFieldHospitalConstructionConsumed,
                manager.Id,
                string.Empty,
                0,
                0,
                -totalWeight,
                $"Consumed materials for {project.Id}.");
            transaction.SourceMilitaryFieldHospitalConstructionProjectId =
                project.Id;
            AddConsumptionLines(transaction, timberPlan);
            AddConsumptionLines(transaction, leatherPlan);
            project.InventoryTransactionId = transaction.Id;

            organization.Treasury = project.OwnerTreasuryAfter;
            ApplyConsumption(timberPlan);
            ApplyConsumption(leatherPlan);
            world.InventoryTransactions.Add(transaction);
            world.MilitaryFieldHospitalConstructionProjects.Add(project);
            world.Validate();
            return project;
        }

        public MilitaryFieldHospitalConstructionWorkState WorkOneDay(
            WorldState world,
            StableId projectId,
            StableId workerPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var project = FindProject(world, projectId.Value);
            if (project.Status !=
                MilitaryFieldHospitalConstructionStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "The field-hospital project is already complete.");
            }
            var people = PeopleFor(world);
            var worker = people.GetRequired(workerPersonId.Value);
            var army = FindArmy(world, project.SourceArmyId);
            if (IsArmyMarching(world, army.Id) ||
                !EligibleProjectWorker(
                    world, people, worker, army, project.LocationId))
            {
                throw new InvalidOperationException(
                    "The worker is unavailable for field-hospital construction.");
            }
            for (var i = 0;
                 i < world.MilitaryFieldHospitalConstructionWork.Count;
                 i++)
            {
                var existing = world.MilitaryFieldHospitalConstructionWork[i];
                if (existing.ProjectId == project.Id &&
                    existing.WorkerPersonId == worker.Id &&
                    existing.Day == world.AbsoluteDay)
                {
                    throw new InvalidOperationException(
                        "A worker can contribute only once to this project per day.");
                }
            }

            var work = new MilitaryFieldHospitalConstructionWorkState
            {
                Id = $"military_field_hospital_work.{world.AbsoluteDay}." +
                    $"{world.MilitaryFieldHospitalConstructionWork.Count:D6}",
                ProjectId = project.Id,
                Day = world.AbsoluteDay,
                WorkerPersonId = worker.Id,
                LaborDays = 1
            };
            world.MilitaryFieldHospitalConstructionWork.Add(work);
            project.CompletedLaborDays++;
            if (project.CompletedLaborDays == project.RequiredLaborDays)
            {
                CompleteProject(world, project);
            }
            world.Validate();
            return work;
        }

        public void AssessMaintenanceDue(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                var site = world.MilitaryRearMedicalSites[i];
                if (site.KindId ==
                        MilitaryRearMedicalSiteKindIds.FieldHospital &&
                    world.AbsoluteDay > site.NextMaintenanceDay)
                {
                    site.IsOperational = false;
                }
            }
        }

        public MilitaryFieldHospitalMaintenanceState Maintain(
            WorldState world,
            StableId rearMedicalSiteId,
            StableId managerPersonId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            world.Validate();
            var site = FindSite(world, rearMedicalSiteId.Value);
            if (site.KindId != MilitaryRearMedicalSiteKindIds.FieldHospital ||
                world.AbsoluteDay < site.NextMaintenanceDay)
            {
                throw new InvalidOperationException(
                    "This field hospital is not due for maintenance.");
            }
            var project = FindProject(
                world, site.SourceConstructionProjectId);
            var army = FindArmy(world, project.SourceArmyId);
            var people = PeopleFor(world);
            var manager = people.GetRequired(managerPersonId.Value);
            var organization = FindOrganization(
                world, site.OwnerOrganizationId);
            var container = FindContainer(
                world, site.SupportInventoryContainerId);
            if (!EligibleProjectWorker(
                    world, people, manager, army, site.LocationId) ||
                organization.Treasury <
                    MilitaryMedicalRules.FieldHospitalMaintenanceMoney)
            {
                throw new InvalidOperationException(
                    "The field hospital lacks an eligible manager or maintenance funds.");
            }
            _content.ValidateManifest(world.ProductionContentManifest);
            var timberPlan = PlanConsumption(
                world,
                container,
                CoreProductionContent.TimberMaterialProductId,
                MilitaryMedicalRules.FieldHospitalMaintenanceTimberUnits);
            var maintenance = new MilitaryFieldHospitalMaintenanceState
            {
                Id = $"military_field_hospital_maintenance." +
                    $"{world.AbsoluteDay}." +
                    $"{world.MilitaryFieldHospitalMaintenance.Count:D6}",
                RearMedicalSiteId = site.Id,
                Day = world.AbsoluteDay,
                ManagerPersonId = manager.Id,
                SourceTimberBatchId = timberPlan[0].Batch.Id,
                TimberUnitsConsumed =
                    MilitaryMedicalRules.FieldHospitalMaintenanceTimberUnits,
                MoneyPaid = MilitaryMedicalRules.FieldHospitalMaintenanceMoney,
                OwnerTreasuryBefore = organization.Treasury,
                OwnerTreasuryAfter = checked(
                    organization.Treasury -
                    MilitaryMedicalRules.FieldHospitalMaintenanceMoney),
                PreviousNextMaintenanceDay = site.NextMaintenanceDay,
                NewNextMaintenanceDay = checked(
                    world.AbsoluteDay +
                    MilitaryMedicalRules.FieldHospitalMaintenanceIntervalDays)
            };
            var transaction = ProductInventorySystem.NewTransaction(
                world,
                InventoryTransactionType
                    .MilitaryFieldHospitalMaintenanceConsumed,
                manager.Id,
                string.Empty,
                0,
                0,
                -ConsumptionWeight(timberPlan),
                $"Maintained field hospital {site.Id}.");
            transaction.SourceMilitaryFieldHospitalMaintenanceId =
                maintenance.Id;
            AddConsumptionLines(transaction, timberPlan);
            maintenance.InventoryTransactionId = transaction.Id;

            organization.Treasury = maintenance.OwnerTreasuryAfter;
            ApplyConsumption(timberPlan);
            site.LastMaintenanceDay = world.AbsoluteDay;
            site.NextMaintenanceDay = maintenance.NewNextMaintenanceDay;
            site.IsOperational = true;
            world.InventoryTransactions.Add(transaction);
            world.MilitaryFieldHospitalMaintenance.Add(maintenance);
            world.Validate();
            return maintenance;
        }

        private void CompleteProject(
            WorldState world,
            MilitaryFieldHospitalConstructionProjectState project)
        {
            var container = new InventoryContainerState
            {
                Id = $"inventory_container.military_field_hospital." +
                    project.Id,
                KindId = MedicalStoreKindId,
                OwnerOrganizationId = project.OwnerOrganizationId,
                LocationId = project.LocationId,
                CapacityWeight = MilitaryMedicalRules
                    .PrototypeMedicalContainerCapacityWeight
            };
            var site = new MilitaryRearMedicalSiteState
            {
                Id = $"military_rear_medical_site.field_hospital." +
                    project.Id,
                KindId = MilitaryRearMedicalSiteKindIds.FieldHospital,
                LocationId = project.LocationId,
                OwnerOrganizationId = project.OwnerOrganizationId,
                MedicineInventoryContainerId = container.Id,
                BedCapacity = MilitaryMedicalRules.FieldHospitalBedCapacity,
                RegisteredDay = world.AbsoluteDay,
                SourceConstructionProjectId = project.Id,
                SupportInventoryContainerId =
                    project.MaterialInventoryContainerId,
                MaintenancePolicyId =
                    MilitaryFieldHospitalMaintenancePolicyIds
                        .TenDayTimberUpkeep,
                LastMaintenanceDay = world.AbsoluteDay,
                NextMaintenanceDay = checked(
                    world.AbsoluteDay +
                    MilitaryMedicalRules.FieldHospitalMaintenanceIntervalDays),
                IsOperational = true
            };
            world.InventoryContainers.Add(container);
            world.MilitaryRearMedicalSites.Add(site);
            project.Status =
                MilitaryFieldHospitalConstructionStatus.Completed;
            project.CompletedDay = world.AbsoluteDay;
            project.RearMedicalSiteId = site.Id;
        }

        private sealed class BatchConsumption
        {
            public ProductBatchState Batch;
            public long Quantity;
        }

        private List<BatchConsumption> PlanConsumption(
            WorldState world,
            InventoryContainerState container,
            string productDefinitionId,
            long requiredQuantity)
        {
            _ = _content.GetProduct(productDefinitionId);
            var candidates = new List<ProductBatchState>();
            for (var i = 0; i < world.ProductBatches.Count; i++)
            {
                var batch = world.ProductBatches[i];
                if (batch.InventoryContainerId == container.Id &&
                    batch.OwnerOrganizationId ==
                        container.OwnerOrganizationId &&
                    batch.ProductDefinitionId == productDefinitionId &&
                    batch.Quantity > batch.ReservedQuantity)
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
            var remaining = requiredQuantity;
            var result = new List<BatchConsumption>();
            for (var i = 0; i < candidates.Count && remaining > 0; i++)
            {
                var available = candidates[i].Quantity -
                    candidates[i].ReservedQuantity;
                var quantity = Math.Min(available, remaining);
                result.Add(new BatchConsumption
                {
                    Batch = candidates[i],
                    Quantity = quantity
                });
                remaining -= quantity;
            }
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient unreserved {productDefinitionId}.");
            }
            return result;
        }

        private static long ConsumptionWeight(
            List<BatchConsumption> plan)
        {
            long weight = 0;
            for (var i = 0; i < plan.Count; i++)
            {
                weight = checked(
                    weight + plan[i].Quantity * plan[i].Batch.UnitWeight);
            }
            return weight;
        }

        private static void AddConsumptionLines(
            InventoryTransactionState transaction,
            List<BatchConsumption> plan)
        {
            for (var i = 0; i < plan.Count; i++)
            {
                transaction.Lines.Add(ProductInventorySystem.Line(
                    plan[i].Batch, -plan[i].Quantity, 0));
            }
        }

        private static void ApplyConsumption(List<BatchConsumption> plan)
        {
            for (var i = 0; i < plan.Count; i++)
            {
                plan[i].Batch.Quantity = checked(
                    plan[i].Batch.Quantity - plan[i].Quantity);
            }
        }

        private IPersonRepository PeopleFor(WorldState world)
        {
            return _people ?? new WorldStatePersonRepository(world);
        }

        private static bool EligibleProjectWorker(
            WorldState world,
            IPersonRepository people,
            PersonState person,
            ArmyState army,
            string locationId)
        {
            if (!person.IsAlive || person.LocationId != locationId ||
                IsPersonTraveling(world, person.Id))
            {
                return false;
            }
            for (var i = 0; i < world.MilitaryServices.Count; i++)
            {
                var service = world.MilitaryServices[i];
                if (service.PersonId == person.Id &&
                    service.ArmyId == army.Id &&
                    (service.Status == MilitaryServiceStatus.Active ||
                     service.Status == MilitaryServiceStatus.Mustering))
                {
                    _ = people.GetRequired(service.PersonId);
                    return true;
                }
            }
            for (var i = 0; i < world.Memberships.Count; i++)
            {
                if (world.Memberships[i].PersonId == person.Id &&
                    world.Memberships[i].OrganizationId ==
                        army.OrganizationId)
                {
                    return true;
                }
            }
            return person.Id == army.CommanderPersonId;
        }

        private static bool IsPersonTraveling(WorldState world, string personId)
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

        private static bool IsArmyMarching(WorldState world, string armyId)
        {
            for (var i = 0; i < world.ArmyMarches.Count; i++)
            {
                if (world.ArmyMarches[i].ArmyId == armyId)
                {
                    return true;
                }
            }
            return false;
        }

        private static MilitaryFieldHospitalConstructionProjectState
            FindProject(WorldState world, string id)
        {
            for (var i = 0;
                 i < world.MilitaryFieldHospitalConstructionProjects.Count;
                 i++)
            {
                if (world.MilitaryFieldHospitalConstructionProjects[i].Id == id)
                {
                    return world.MilitaryFieldHospitalConstructionProjects[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing field-hospital project {id}.");
        }

        private static MilitaryRearMedicalSiteState FindSite(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.MilitaryRearMedicalSites.Count; i++)
            {
                if (world.MilitaryRearMedicalSites[i].Id == id)
                {
                    return world.MilitaryRearMedicalSites[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing rear medical site {id}.");
        }

        private static ArmyState FindArmy(WorldState world, string id)
        {
            for (var i = 0; i < world.Armies.Count; i++)
            {
                if (world.Armies[i].Id == id)
                {
                    return world.Armies[i];
                }
            }
            throw new InvalidOperationException($"Missing army {id}.");
        }

        private static LocationState FindLocation(WorldState world, string id)
        {
            for (var i = 0; i < world.Locations.Count; i++)
            {
                if (world.Locations[i].Id == id)
                {
                    return world.Locations[i];
                }
            }
            throw new InvalidOperationException($"Missing location {id}.");
        }

        private static OrganizationState FindOrganization(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.Organizations.Count; i++)
            {
                if (world.Organizations[i].Id == id)
                {
                    return world.Organizations[i];
                }
            }
            throw new InvalidOperationException($"Missing organization {id}.");
        }

        private static InventoryContainerState FindContainer(
            WorldState world,
            string id)
        {
            for (var i = 0; i < world.InventoryContainers.Count; i++)
            {
                if (world.InventoryContainers[i].Id == id)
                {
                    return world.InventoryContainers[i];
                }
            }
            throw new InvalidOperationException(
                $"Missing inventory container {id}.");
        }
    }
}
