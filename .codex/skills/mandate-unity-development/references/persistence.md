# Persistence and determinism

Read `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md` before changing persistence or deterministic behavior. Also read `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md` when work affects permanent people, households, attention tiers, event scheduling, or partitioned storage.

## Current sources of truth

- `WorldState.CurrentSchemaVersion` defines the current world schema.
- `WorldSnapshotSerializer` owns snapshot serialization and deserialization.
- `WorldSnapshotMigrator` owns sequential migration to the current schema.
- `NamedRandom` owns deterministic named random streams.
- `StableId` owns stable entity identity.

Verify these symbols in source because versions and fields can change.

## Change rules

1. Treat save data as an external compatibility contract.
2. Increment the schema version for incompatible persisted-state changes.
3. Add a sequential migration from the immediately previous version.
4. Initialize new collections and fields explicitly during migration.
5. Validate invariants after migration and deserialization.
6. Keep runtime-only Unity objects out of snapshots.
7. Do not derive persistent identity from list positions, runtime instance IDs, current time, or unstable hashes.
8. Route random decisions through named deterministic streams with stable entity IDs and explicit time/action coordinates.
9. Preserve deterministic iteration order when collection order affects outcomes.
10. Treat every person's stable identity and life history as permanent state from birth; a summary or cache may optimize access but may not replace, merge, rerandomize, or erase that identity.
11. When partitioning world data, preserve cross-partition references and provide a deterministic reconstruction path.

## Required tests

- serialize and deserialize the current version;
- migrate each newly supported previous version;
- reject zero, negative, future, or unsupported versions as appropriate;
- load missing optional collections safely when compatibility requires it;
- produce identical results from the same seed and starting state;
- preserve identity and cross-references after round trip;
- preserve permanent people across attention-tier changes and partition load/unload cycles.
