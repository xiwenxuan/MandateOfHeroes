# 江陵 Development Reference Manifest

## Document Governance

- Purpose：为江陵后续开发提供唯一资料入口。
- Authority：L2 Current Development Input Manifest
- Covers：江陵的Canonical、历史、人口、人物、宗族、设施、交通、军事与实现输入。
- DoesNotCover：新的历史结论或运行时实现。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md
- Status：CURRENT

| Field | Reference |
|---|---|
| TargetPlace | 江陵 |
| TargetYear / Scenario | Scenario-selected |
| CanonicalSystemDocs | `Docs/GAME_VISION_AND_GAMEPLAY.md` → `Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01...` → `02...` → `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_江陵_place_han140_jingzhou_nan_jiangling/00_Master.md` + `Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/` |
| PopulationDataset | `Assets/StreamingAssets/HistoricalPopulation/Han135260V1/`与`Docs/HISTORICAL_POPULATION_135_260.md` |
| PersonDataset | `Assets/StreamingAssets/HistoricalPersons/Han135260V1/persons.json` |
| ClanDataset | `clans.json`、`branches.json`、`clan_presence.json` |
| FacilityReference | `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| TransportReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/10_135-260重点交通节点开发参考.xlsx` |
| MilitaryReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/11_135-260重要军事空间与战役开发参考.xlsx` |
| ExistingImplementation | Historical reference only; no formal city runtime initialization. |
| KnownConflicts | 见A10与Knowledge Base的Document Conflict Register；Reference不得冒充实现。 |
| KnownResearchGaps | 精确住宅、Estate边界、族产、Branch迁入、Facility位置和Center证据。 |
| KnownImplementationGaps | FamilyOrganization/FamilyCenter正式运行时、资产权限、通信、存档迁移和UI。 |
| DoNotUseDocs | 旧Task/Report、参考游戏分析和Benchmark不得单独作为当前Canonical Spec。 |
| RecommendedReadingOrder | AGENTS → Game Vision → Domain L1 → Master Status → 本Manifest → P0 Master → Family Spatial → 相关Task/Report。 |
