# Content and data

Use [task-routing.md](task-routing.md) to select the directly relevant design before adding content. Common sources include:

- `Docs/DATA_AND_CONTENT_FOUNDATION.md`
- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` for production, construction, agriculture, industry, or technology data
- `Docs/LEGAL_AND_ASSETS.md`
- `Docs/HISTORICAL_CITY_LIST.md`
- `Docs/HISTORICAL_CHARACTERS_FIRST_50.md`
- `Docs/HISTORICAL_EVENTS_182_190.md`
- `Docs/HISTORICAL_POPULATION_135_260.md`

## Rules

1. Reuse established IDs, naming conventions, schemas, and validation.
2. Keep static authoring data separate from mutable runtime and save state.
3. Treat ScriptableObject fields, prefab references, and scene references as serialized contracts.
4. When renaming or moving serialized fields, preserve compatibility with an appropriate Unity migration mechanism.
5. Add validation for duplicate IDs, missing references, invalid ranges, and impossible relationships.
6. Keep historical facts, original game expression, and third-party source material distinguishable.
7. Record license, source, author, and modification status for every external asset.
8. Do not copy proprietary game UI, maps, text, data, audio, or art.
9. Population-scale source data may guide generation and validation, but it must not silently overwrite established permanent identities or household histories.

## Unity acceptance

Changes to ScriptableObjects, scenes, prefabs, `.asmdef` files, serialization, or editor integration require a controlled Unity test unless the environment is blocked. Compilation alone is not sufficient.
