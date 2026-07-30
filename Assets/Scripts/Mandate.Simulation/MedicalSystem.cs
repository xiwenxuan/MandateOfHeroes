using System;
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

        public MedicalSystem(ulong masterSeed)
        {
            _random = new NamedRandom(masterSeed);
        }

        public MedicalTreatmentResult TreatArmyWounded(
            WorldState world,
            StableId physicianId,
            StableId armyId,
            int requestedPatients)
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
            if (!physician.IsAlive || medicalSkill < 2_500)
            {
                return Failure("该人物没有可用于军中救治的医术。");
            }

            if (physician.LocationId != army.LocationId)
            {
                return Failure("医者与伤兵不在同一地点。");
            }

            if (IsPersonTraveling(world, physician.Id) ||
                IsArmyMarching(world, army.Id))
            {
                return Failure("行军或旅行途中不能组织集中救治。");
            }

            if (army.WoundedTroops <= 0)
            {
                return Failure("该军队目前没有待救治伤兵。");
            }

            var herbs = FindInventory(
                world, physician.Id, "commodity.herbs");
            if (herbs == null || herbs.Quantity <= 0)
            {
                return Failure("医者没有携带药材。");
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

            army.WoundedTroops -= recovered;
            army.Troops = checked(army.Troops + recovered);
            if (army.Troops > army.MaximumTroops)
            {
                throw new InvalidOperationException(
                    "Treatment would exceed the army maximum.");
            }

            var record = new MedicalTreatmentRecordState
            {
                Id =
                    $"medical_treatment.{world.AbsoluteDay}.{physician.Id}." +
                    $"{army.Id}.{sequence}",
                Day = world.AbsoluteDay,
                PhysicianPersonId = physician.Id,
                ArmyId = army.Id,
                PatientsTreated = patients,
                RecoveredTroops = recovered,
                HerbsConsumed = herbsConsumed,
                Summary =
                    $"{physician.DisplayName}救治{army.DisplayName}伤兵{patients}人，" +
                    $"{recovered}人恢复战斗能力。"
            };
            world.MedicalTreatments.Add(record);
            world.Validate();
            return new MedicalTreatmentResult(
                true,
                patients,
                recovered,
                herbsConsumed,
                record.Summary);
        }

        private static bool IsPersonTraveling(
            WorldState world,
            string personId)
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

        private static PersonState FindPerson(WorldState world, string personId)
        {
            for (var i = 0; i < world.People.Count; i++)
            {
                if (world.People[i].Id == personId)
                {
                    return world.People[i];
                }
            }

            throw new InvalidOperationException($"Missing person {personId}.");
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
}
