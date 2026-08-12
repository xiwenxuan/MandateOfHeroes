# 洛阳 Development Reference Manifest

## Document Governance

- Purpose：为洛阳后续开发提供唯一资料入口。
- Authority：L2 Current Development Input Manifest
- Covers：洛阳的Canonical、历史、人口、人物、宗族、设施、交通、军事与实现输入。
- DoesNotCover：新的历史结论或运行时实现。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md
- Status：CURRENT

| Field | Reference |
|---|---|
| TargetPlace | 洛阳 |
| TargetYear / Scenario | 184 / scenario.han.184.yellow_turban |
| CanonicalSystemDocs | `Docs/GAME_VISION_AND_GAMEPLAY.md` → `Docs/FAMILY_ORGANIZATION_REFERENCE_V1/01...` → `02...` → `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/04_CORE_SETTLEMENTS/P0_洛阳_place_han140_sili_henan_luoyang/00_Master.md` + `Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/` |
| PopulationDataset | `Assets/StreamingAssets/HistoricalPopulation/Han135260V1/`与`Docs/HISTORICAL_POPULATION_135_260.md` |
| PersonDataset | `Assets/StreamingAssets/HistoricalPersons/Han135260V1/persons.json` |
| ClanDataset | `clans.json`、`branches.json`、`clan_presence.json` |
| FacilityReference | `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`|Docs/TASK_LUOYANG_184_HISTORICAL_V1.md|Docs/TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md|Docs/TASK_LUOYANG_184_METROPOLITAN_INITIALIZATION_V1.md |
| TransportReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/10_135-260重点交通节点开发参考.xlsx` |
| MilitaryReference | `Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/11_135-260重要军事空间与战役开发参考.xlsx` |
| ExistingImplementation | Formal 270,000 urban + 400,000 metropolitan packages exist; V69已接入25名历史人物、15个既有FamilyOrganization、2,084 Facility投影、Office/Activity及15个Deferred FamilyCenter。 |
| KnownConflicts | 见A10与Knowledge Base的Document Conflict Register；Reference不得冒充实现。 |
| KnownResearchGaps | 精确住宅、Estate边界、族产、Branch迁入、Facility位置和Center证据。 |
| KnownImplementationGaps | 可写派生人口检查点、40万人生活经济闭环、32条都市圈Facility未决主张、有效FamilyCenter条件、通信和正式玩家UI。 |
| DoNotUseDocs | 旧Task/Report、参考游戏分析和Benchmark不得单独作为当前Canonical Spec。 |
| RecommendedReadingOrder | AGENTS → Game Vision → Domain L1 → Master Status → 本Manifest → P0 Master → Family Spatial → 相关Task/Report。 |

## Development Place Roster V1

| Field | Frozen value |
|---|---|
| CanonicalPlaceId | `place.han140.sili.henan.luoyang` |
| DevelopmentDepth | D5 |
| DevelopmentPriority | P0 |
| RecommendedWave | WAVE_0 |
| HistoricalStatePlan | 140:S2/H2, 184:S4/H4, 189:S4/H4, 190:S4/H4, 194:S3/H3, 249:S3/H3 |
| SupportedScenarios / TimePoints | 140, 184, 189, 190, 194, 249 |
| ReferenceReadiness | READY_FOR_IMPLEMENTATION |
| Blockers | DPB-001|DPB-002 |
| RecommendedDevelopmentScope | Urban|Administrative|Family|Trade|Military|HistoricalEvent|Political|Education|Clan|Estate |
| RuntimeBoundary | 这是开发目标与资料入口，不表示运行时已经实现。 |

## City Development Pack V1

| Field | Reference |
|---|---|
| CityDevelopmentPack | `Docs/HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/LUOYANG/` |
| PackStatus | DEVELOPMENT_READY |
| ReferenceReadiness | READY_FOR_IMPLEMENTATION |
| HistoricalStatePlan | `07_CORE_CITY_HISTORICAL_STATE_AND_CHANGEPOINT_PLAN.xlsx` |
| HinterlandReference | `05_CORE_CITY_HINTERLAND_AND_SETTLEMENT_NETWORK.xlsx` |
| PopulationLayerReference | `06_CORE_CITY_POPULATION_LAYER_REFERENCE.xlsx` |
| FacilityReference | `LUOYANG/CITY_DEVELOPMENT_DATA.xlsx#05_FACILITIES` |
| PersonCoverage | 10 stable PersonId city-slice records |
| FamilyCoverage | 2 Clan/Branch slice records; no automatic FamilyCenter |
| DepthUpgradeRecommendation | NONE_THIS_TASK |
| RuntimeBoundary | Pack complete does not mean runtime implemented and does not change DevelopmentDepth. |

<!-- FDRP-V1:BEGIN -->
## 当前完整参考包合同（FDRP V1）

- DevelopmentTier：`T4`（旧 `D5` 仅作历史映射）
- ReferencePackCompleteness：`FULL_READY`
- RuntimeImplementationStatus：`PARTIAL`
- Wave：`WAVE_0`（未改变）
- Pack：`Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/PACKS/PLACE_HAN140_SILI_HENAN_LUOYANG`

以上三个状态相互独立；完整包不会自动升档或物化运行时实体。
<!-- FDRP-V1:END -->

## Development Readiness Review V1（2026-08-11）

| Field | Frozen value |
|---|---|
| Review | `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md` |
| GateA | `GO_WITH_BLOCKERS` |
| GateB | `GO_WITH_DEFERRED_PLACES` |
| FormalOpeningPopulation | `400000`（都市圈包含式唯一人口来源） |
| CoreIntegrity | 400000 Person、80899 Household、2084 Facility 的身份、引用、Cell与容量不变量通过 |
| RequiredBlockers | 主世界投影；25历史Person幂等绑定；7旧组织迁移；FamilyCenter持久合同；旧Facility内联人物列表去权威化 |
| DeferredPlaces | `geo.site.hulao`、`geo.site.hangu` |
| NextTask | `LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1` |
| OutOfScope | 虎牢/函谷物化、700K物化、全国人物/家族、通用Facility重构、190玩法、UI/美术/场景 |

门禁只允许进入上述有界集成任务，不表示洛阳已经接入主世界，也不修改冻结的 Wave 0 组成。

## Historical Person / Family Integration V1（2026-08-11）

| Field | Current value |
|---|---|
| Task | `Docs/TASK_LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1.md` |
| Evidence | `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/` |
| RuntimeSchema | `V69` |
| PermanentFacts | `400000 Person / 80899 Household / 2084 Facility`，全部沿用受保护包 |
| HistoricalBinding | `25/25 exact P-ID; added=0; duplicate=0` |
| FamilyOrganization | `15 retained; 7 migrated/corrected; 8 retained with 32 unresolved Facility claims` |
| FamilyCenter | `15 Deferred; Active Primary=0; Active Local=0` |
| Office | `8 Civil/Military assignments with canonical jurisdiction and existing Facility` |
| DeferredPlaces | `geo.site.hulao`、`geo.site.hangu`保持Deferred |
| NextCandidate | 可写派生检查点与`Residence→Work→Production→Consumption→Market→Supply`闭环；未自动启动 |
