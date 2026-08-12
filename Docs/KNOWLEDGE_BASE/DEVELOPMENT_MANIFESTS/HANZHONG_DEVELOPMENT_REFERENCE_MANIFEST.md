# 汉中 Development Reference Manifest

## Document Governance

- Purpose：为汉中后续开发提供唯一资料入口。
- Authority：L2 Current Development Input Manifest
- Covers：CanonicalPlace、历史状态、资料准备度、阻塞项与建议开发范围。
- DoesNotCover：新的历史事实、运行时Place/Facility实现或存档升级。
- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md
- Status：CURRENT

| Field | Reference |
|---|---|
| TargetPlace | 汉中 |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md` |
| ExistingImplementation | Reference only; no formal runtime initialization. |
| DoNotInfer | D级不是历史城市等级；Reference不是Implementation。 |

## Development Place Roster V1

| Field | Frozen value |
|---|---|
| CanonicalPlaceId | `place.han140.yizhou.hanzhong.nanzheng` |
| DevelopmentDepth | D4 |
| DevelopmentPriority | P1 |
| RecommendedWave | WAVE_2 |
| HistoricalStatePlan | 184:S2/H2, 214:S3/H3, 219:S4/H4, 227:S3/H3 |
| SupportedScenarios / TimePoints | 184, 214, 219, 227 |
| ReferenceReadiness | MOSTLY_READY |
| Blockers | DPB-005 |
| RecommendedDevelopmentScope | Urban|Agriculture|Military|Pass|Logistics|HistoricalEvent |
| RuntimeBoundary | 这是开发目标与资料入口，不表示运行时已经实现。 |

## City Development Pack V1

| Field | Reference |
|---|---|
| CityDevelopmentPack | `Docs/HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/HANZHONG_CANONICAL_PLACE/` |
| PackStatus | READY_WITH_MODELED_GAPS |
| ReferenceReadiness | MOSTLY_READY |
| HistoricalStatePlan | `07_CORE_CITY_HISTORICAL_STATE_AND_CHANGEPOINT_PLAN.xlsx` |
| HinterlandReference | `05_CORE_CITY_HINTERLAND_AND_SETTLEMENT_NETWORK.xlsx` |
| PopulationLayerReference | `06_CORE_CITY_POPULATION_LAYER_REFERENCE.xlsx` |
| FacilityReference | `HANZHONG_CANONICAL_PLACE/CITY_DEVELOPMENT_DATA.xlsx#05_FACILITIES` |
| PersonCoverage | 10 stable PersonId city-slice records |
| FamilyCoverage | 4 Clan/Branch slice records; no automatic FamilyCenter |
| DepthUpgradeRecommendation | NONE_THIS_TASK |
| RuntimeBoundary | Pack complete does not mean runtime implemented and does not change DevelopmentDepth. |

<!-- FDRP-V1:BEGIN -->
## 当前完整参考包合同（FDRP V1）

- DevelopmentTier：`T3`（旧 `D4` 仅作历史映射）
- ReferencePackCompleteness：`FULL_READY_WITH_MODELED_GAPS`
- RuntimeImplementationStatus：`NOT_STARTED`
- Wave：`WAVE_2`（未改变）
- Pack：`Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/PACKS/PLACE_HAN140_YIZHOU_HANZHONG_NANZHENG`

以上三个状态相互独立；完整包不会自动升档或物化运行时实体。
<!-- FDRP-V1:END -->
