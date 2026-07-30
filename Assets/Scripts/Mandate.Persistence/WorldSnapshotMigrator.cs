using System;
using Mandate.Domain;

namespace Mandate.Persistence
{
    public static class WorldSnapshotMigrator
    {
        public static WorldState MigrateToCurrent(WorldState world)
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
    }
}
