# Development Place Roster V1

本目录冻结项目第一次可执行的重点地点开发路线图。

## 核心边界

- `DevelopmentDepth`是制作深度，不是历史行政等级、人口等级或物理类型。
- `DevelopmentPriority / Wave`是项目开发顺序，不是世界层级。
- 77个战略显示名、133个Core Settlement、105个治所和1182县都不自动等于Roster。
- 世界事实仍由`CanonicalPlace + Cell + Facility + Population + Organization`组成。
- `geo.site.*`沿用既有非城市稳定参考；进入运行时前仍需完成CanonicalPlace/Cell范围评审。
- 本轮只冻结资料与开发范围，不实现新Place、Facility、HistoricalChangePackage或存档升级。
- 72项Roster不是永久白名单；D0/D1地点可先建立Development Pack，再申请调整制作深度。
- `Pack Ready`只表示资料合同可供后续任务消费，不自动改变`DevelopmentDepth`，也不表示Runtime已经实现。

## 入口

- `01_DEVELOPMENT_PLACE_ROSTER.xlsx`：正式Roster。
- `02_DEVELOPMENT_PLACE_HISTORICAL_STATE_PLAN.xlsx`：逐Place历史状态支持计划。
- `03_DEVELOPMENT_PLACE_REFERENCE_READINESS_MATRIX.xlsx`：资料/数据/设计/实现准备度。
- `04_DEVELOPMENT_PLACE_BLOCKER_REGISTER.xlsx`：阻塞分类。
- `05_DEVELOPMENT_REGION_SLICE_CANDIDATES.xlsx`：区域开发工作包候选。
- `06_DEVELOPMENT_WAVE_PLAN_V1.xlsx`：开发波次。
- `07_D4_D5_PLACE_MASTER.xlsx`：深度开发名册。
- `08_D2_D3_ACCESSIBLE_PLACE_MASTER.xlsx`：可访问及地区中心名册。
- `09_NON_URBAN_STRATEGIC_PLACE_MASTER.xlsx`：非城市重要地点与暂缓项。
- `10_DEVELOPMENT_PLACE_REFERENCE_GAP_PRIORITY.xlsx`：只影响开发的资料缺口。
- `DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1_REPORT.md`：验收结论。
- [`../CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md`](../CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md)：首批10城开发包与通用升档协议。

首批10城Development Pack完成后，下一阶段固定为`LUOYANG-184-DEVELOPMENT-READINESS-REVIEW-V1`；没有新的明确计划时不自动扩充第二批城市。

## FDRP V1 兼容说明

本目录自 2026-08-11 起作为历史名册与准备度证据保留，不回写、不删除。当前术语与 72 地点完整参考包请使用 [`../PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`](../PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md)：`D2→T1`、`D3→T2`、`D4→T3`、`D5→T4`，Wave 原样保留。D0/D1 不再属于特殊 Development Place 档位，名册外没有 T0。
