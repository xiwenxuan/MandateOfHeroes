# Project Knowledge Base

## Document Governance

- Purpose：作为Codex和开发人员寻找项目Source of Truth的第一入口。
- Authority：L2 Current System Status / Knowledge Index
- Covers：Canonical Domain Map、文档Registry、决策/开放问题、冲突、实现/研究缺口和城市Manifest。
- DoesNotCover：替代各Domain的L1正文。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md|../GAME_SYSTEMS_MASTER_AND_STATUS.md
- Status：CURRENT

## Start here

1. Repository规则：`../../AGENTS.md`与项目Skill。
2. 项目愿景：`../GAME_VISION_AND_GAMEPLAY.md`。
3. 文档权威：`DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md`。
4. Domain入口：`REGISTRY/PROJECT_CANONICAL_DOMAIN_MAP.xlsx`。
5. 当前完成度：`../GAME_SYSTEMS_MASTER_AND_STATUS.md`。
6. 历史/内容资料：`../HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md`。
7. 旧Task/Report：只通过`REGISTRY/PROJECT_DOCUMENT_REGISTRY.xlsx`检索，不直接当Canonical。

## Fast routes

| Question | Read |
|---|---|
| 游戏愿景 | `../GAME_VISION_AND_GAMEPLAY.md` |
| 世界、地图、人口、经济 | `../WORLD_SIMULATION_FOUNDATION.md` + Master Status |
| 人物、能力、成长 | `../CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md` |
| Family | `../FAMILY_ORGANIZATION_REFERENCE_V1/README.md` → Family Spatial Consolidation |
| Facility、产权、职位、政治 | `../UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` |
| 生产、建设、农业、科研 | `../PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md` |
| 军事与战争 | `../UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` |
| Scenario | `../HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md` |
| 存档和确定性 | `../DETERMINISTIC_SIMULATION_AND_SAVE.md` |
| 洛阳或其他P0开发 | `DEVELOPMENT_MANIFESTS/` |
| 城市做细、资料包或升档 | `../HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md` |

## End of consolidation

本知识库完成后暂停扩大资料治理。184洛阳 `DEVELOPMENT READINESS REVIEW` 与其有界的 `HISTORICAL PERSON / FAMILY INTEGRATION V1` 已于2026-08-11完成。V69已把25名历史人物、15个既有FamilyOrganization、Office与Deferred FamilyCenter合同接入同一40万永久人物世界，且0新增Person、0新增Facility。正式证据入口为`../HISTORICAL_WORLD_REFERENCE/LUOYANG_184_HISTORICAL_PERSON_FAMILY_INTEGRATION_V1/`；下一候选是可写派生检查点与洛阳生活经济闭环，未获新任务不得自动启动。

## Historical administrative geography route（ADMINISTRATIVE-SEAT-CANONICAL-PLACE-V1）

行政区、治所、物理Place、战略显示名、Scenario Snapshot和重大历史世界状态的统一入口为：

`../HISTORICAL_WORLD_REFERENCE/ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1/README.md`

使用时先读取`REGISTRY/PROJECT_CANONICAL_DOMAIN_MAP.xlsx`的`HistoricalWorldGeography`行，再查工作簿。Reference只说明历史初始化和开发候选，不表示Runtime Place、Seat或HistoricalChangePackage已经实现。下一资料审查为`DEVELOPMENT-PLACE-ROSTER-AND-REFERENCE-READINESS-V1`；它与当前223年正式剧本的产品优先级可以交叉服务，但不能擅自改写全局开发顺序。

## 开发地点Roster与资料准备度（DEVELOPMENT-PLACE-ROSTER-V1）

正式入口：`../HISTORICAL_WORLD_REFERENCE/DEVELOPMENT_PLACE_ROSTER_V1/README.md`。

该入口冻结72个专项开发地点、D0—D5制作深度、逐Place历史状态计划、资料准备度、阻塞项和开发Wave。D级不是行政等级或物理类型；77战略显示名、133核心聚落和105治所都不自动成为Roster。16个D4/D5地点使用`DEVELOPMENT_MANIFESTS/`中的独立Manifest，八份既有P0 Manifest已原位升级，没有生成第二套。

Wave 0固定为洛阳D5、虎牢D4与函谷D3组成的`LUOYANG_HULAO`开发工作包；三者仍是独立Place/参考。门禁审查允许洛阳Core先进入Wave 0A，虎牢、函谷因Cell与分期Facility范围未关闭而延后到Wave 0B；冻结Wave本身未被改写。

## City Development Pack与升档入口

正式入口：`../HISTORICAL_WORLD_REFERENCE/CITY_DEVELOPMENT_PACKS/README_CORE_CITY_DEVELOPMENT_PACKS.md`。

首批10城已经建立可审计资料包，并通过稳定ID引用人物、Clan、Scenario、人口和Facility母库。以后要求把任何地点做细时，先建立或升级Pack，再评审是否改变D级或进入Runtime；Pack完成不自动升档、不自动生成世界对象。72项Roster可扩展，D0/D1地点也允许按协议申请升级。184洛阳门禁证据入口为`../HISTORICAL_WORLD_REFERENCE/LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1/`。

## Development Place 完整参考包（FDRP V1）

地点开发的当前资料入口已迁移到 `../HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`。这里保存 72 个 T1—T4 地点的统一完整参考包、当前主表、事件依赖地点登记与升级协议。旧 D2—D5 Roster 和首批 10 城 Pack 仍可用于追溯，但不再是现行术语的权威入口。

查询时必须同时检查开发档位、参考包完整度和运行时实现状态；三者不能互相推断。参考包中的 `MODELED` 是玩法/系统补全，`UNKNOWN` 与 `NO_EVIDENCE` 不得被转成“没有”，也不得自动物化为世界实体。

## 全国1182县生产、资源、产业与供应参考 V1

全国县域经济开发参考入口为：

`../HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE/README.md`

对应任务书为
`../TASK_HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1.md`。该包覆盖1182县、
13个历史切片和40份审计工作簿，但只具有开发参考权限。县级总量不得替代Cell资源、具体Facility、
Worker、Recipe、Inventory和Transport；1114个未解析县的分析点不得作为历史县治或古道证据。

## 洛阳184生活经济闭环 V1

400,000 Person、80,899 Household、2,084 Facility的当前可运行生活经济证据入口为：

`../HISTORICAL_WORLD_REFERENCE/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1/`

实现任务书为`../TASK_LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1.md`。V70摘要、派生检查点、工作/生产/库存/作物/消费/短缺合同均不能覆盖受保护初始化事实。365日结果已选择`SUPPLY_REGION_DEPENDENCY`路线；下一候选是洛阳外围供应区与农业腹地物化，不得用魔法进口或把计划中的额外人口直接叠加到40万都市基线。

## 智能人口驱动世界与条件历史事件 V1

任务书：`../TASK_WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1.md`

正式报告、11份字段/架构工作簿、洛阳189—190事件原型、Simulation Arena、存档与性能报告位于：
`../HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_POPULATION_DRIVEN_SIMULATION_AND_HISTORICAL_EVENT_CONTRACT_V1/`

查询该领域时先读总报告，再按需要读取Signal/Action/Policy/Seed/Order-Shipment/LOD/HistoricalEvent合同。Reference不等于Runtime；AI只能提议动作；重大事件不能只按年份触发；V71保存决策与事件幂等状态。

## 智能决策 Policy 与 Simulation Arena V1

任务书：`../TASK_WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1.md`

总报告、10份决策/训练/Arena工作簿、模型资产、4,000次运行、独立决策/事件Trace及性能/存档/Neural/下一阶段报告位于：
`../HISTORICAL_WORLD_REFERENCE/WORLD_INTELLIGENT_DECISION_POLICY_AND_SIMULATION_ARENA_V1/`

查询顺序为总报告→性能/确定性/Neural报告→工作簿→`ARENA/`和`MODEL/`原始证据。V72只保存Profile、Goal、Model与有界DecisionMemory；Utility为可解释主基线，Merchant Neural仅为候选实验，Runtime Online Training继续禁止。当前Arena没有证明成熟Facility、产业、贸易网络、政府财政或400K全HOT性能，下一候选是`WORLD-HOT-WARM-COLD-PERMANENT-PERSON-SIMULATION-V1`。
