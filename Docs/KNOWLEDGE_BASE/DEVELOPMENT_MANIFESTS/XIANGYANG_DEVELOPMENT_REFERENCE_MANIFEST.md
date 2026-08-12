# 襄阳 Development Reference Manifest

## Document Governance

- Purpose：为襄阳后续开发提供唯一资料入口。
- Authority：L2 Current Development Input Manifest
- Covers：襄阳的Canonical、历史、人口、人物、宗族、设施、交通、军事与实现输入。
- DoesNotCover：新的历史结论或运行时实现。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md
- Status：CURRENT

| Field | Reference |
|---|---|
| TargetPlace | 襄阳 |
| TargetYear / Scenario | Scenario-selected |
| CanonicalSystemDocs | `Docs/GAME_VISION_AND_GAMEPLAY.md` → `Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01...` → `02...` → `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_襄阳_place_han140_jingzhou_nan_xiangyang/00_Master.md` + `Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/` |
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

## Development Place Roster V1

| Field | Frozen value |
|---|---|
| CanonicalPlaceId | `place.han140.jingzhou.nan.xiangyang` |
| DevelopmentDepth | D4 |
| DevelopmentPriority | P1 |
| RecommendedWave | WAVE_1 |
| HistoricalStatePlan | 184:S2/H2, 194:S3/H3, 208:S4/H4, 219:S4/H4 |
| SupportedScenarios / TimePoints | 184, 194, 208, 219 |
| ReferenceReadiness | MOSTLY_READY |
| Blockers | DPB-010 |
| RecommendedDevelopmentScope | Urban|Administrative|Trade|Military|Naval|HistoricalEvent |
| RuntimeBoundary | 这是开发目标与资料入口，不表示运行时已经实现。 |

## City Development Pack V1

| Field | Reference |
|---|---|
| CityDevelopmentPack | `Docs/HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/XIANGYANG/` |
| PackStatus | READY_WITH_MODELED_GAPS |
| ReferenceReadiness | MOSTLY_READY |
| HistoricalStatePlan | `07_CORE_CITY_HISTORICAL_STATE_AND_CHANGEPOINT_PLAN.xlsx` |
| HinterlandReference | `05_CORE_CITY_HINTERLAND_AND_SETTLEMENT_NETWORK.xlsx` |
| PopulationLayerReference | `06_CORE_CITY_POPULATION_LAYER_REFERENCE.xlsx` |
| FacilityReference | `XIANGYANG/CITY_DEVELOPMENT_DATA.xlsx#05_FACILITIES` |
| PersonCoverage | 10 stable PersonId city-slice records |
| FamilyCoverage | 3 Clan/Branch slice records; no automatic FamilyCenter |
| DepthUpgradeRecommendation | NONE_THIS_TASK |
| RuntimeBoundary | Pack complete does not mean runtime implemented and does not change DevelopmentDepth. |

<!-- FDRP-V1:BEGIN -->
## 当前完整参考包合同（FDRP V1）

- DevelopmentTier：`T3`（旧 `D4` 仅作历史映射）
- ReferencePackCompleteness：`FULL_READY_WITH_MODELED_GAPS`
- RuntimeImplementationStatus：`NOT_STARTED`
- Wave：`WAVE_1`（未改变）
- Pack：`Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/PACKS/PLACE_HAN140_JINGZHOU_NAN_XIANGYANG`

以上三个状态相互独立；完整包不会自动升档或物化运行时实体。
<!-- FDRP-V1:END -->
