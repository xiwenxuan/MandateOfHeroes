# 夏口 Development Reference Manifest

## Document Governance

- Purpose：为夏口后续开发提供唯一资料入口。
- Authority：L2 Current Development Input Manifest
- Covers：CanonicalPlace、历史状态、资料准备度、阻塞项与建议开发范围。
- DoesNotCover：新的历史事实、运行时Place/Facility实现或存档升级。
- RelatedCanonicalDocs：../README_PROJECT_KNOWLEDGE_BASE.md
- Status：CURRENT

| Field | Reference |
|---|---|
| TargetPlace | 夏口 |
| HistoricalReferenceDocs | `Docs/HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md` |
| ExistingImplementation | Reference only; no formal runtime initialization. |
| DoNotInfer | D级不是历史城市等级；Reference不是Implementation。 |

## Development Place Roster V1

| Field | Frozen value |
|---|---|
| CanonicalPlaceId | `geo.site.xiakou` |
| DevelopmentDepth | D4 |
| DevelopmentPriority | P1 |
| RecommendedWave | WAVE_2 |
| HistoricalStatePlan | 184:S1/H1, 208:S4/H4, 219:S2/H2 |
| SupportedScenarios / TimePoints | 184, 208, 219 |
| ReferenceReadiness | RESEARCH_REQUIRED |
| Blockers | DPB-013 |
| RecommendedDevelopmentScope | Naval|Trade|Logistics|HistoricalEvent |
| RuntimeBoundary | 这是开发目标与资料入口，不表示运行时已经实现。 |

<!-- FDRP-V1:BEGIN -->
## 当前完整参考包合同（FDRP V1）

- DevelopmentTier：`T3`（旧 `D4` 仅作历史映射）
- ReferencePackCompleteness：`RESEARCH_BLOCKED`
- RuntimeImplementationStatus：`NOT_STARTED`
- Wave：`WAVE_2`（未改变）
- Pack：`Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/PACKS/GEO_SITE_XIAKOU`

以上三个状态相互独立；完整包不会自动升档或物化运行时实体。
<!-- FDRP-V1:END -->
