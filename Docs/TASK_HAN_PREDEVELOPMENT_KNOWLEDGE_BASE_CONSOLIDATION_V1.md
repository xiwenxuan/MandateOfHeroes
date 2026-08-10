# HAN-PREDEVELOPMENT-KNOWLEDGE-BASE-CONSOLIDATION-V1 任务书

## Document Governance

- Purpose：在恢复大规模开发前，收口家族空间参考与全项目Canonical知识库。
- Authority：L4 Task / Execution Contract；不得覆盖L0/L1规则。
- Covers：133核心聚落、250重点县、Clan/Branch空间时间线、13剧本切片、文档Registry、Domain Map、决策/冲突/缺口与P0城市Manifest。
- DoesNotCover：全国运行时FamilyOrganization/FamilyCenter物化、人口重建、地图重做、存档升级或大型Simulation开发。
- Supersedes：无；整合既有资料而不删除历史。
- SupersededBy：无。
- RelatedCanonicalDocs：`GAME_VISION_AND_GAMEPLAY.md`、`GAME_SYSTEMS_MASTER_AND_STATUS.md`、`FAMILY_ORGANIZATION_REFERENCE_V1/README.md`。
- Status：IMPLEMENTED_REFERENCE（资料与治理交付已完成；仅证明本任务明确覆盖范围）。

## 1. 目标

本任务由两个不可分割的Workstream组成：

1. `HAN FAMILY SPATIAL REFERENCE CONSOLIDATION`：把洛阳验证过的Clan、Branch、Person、Residence、Estate、FamilyAsset、FamilyOrganization Candidate与Primary/Local FamilyCenter Candidate方法推广到133核心聚落、250重点县、重要宗族及13剧本。
2. `PROJECT DOCUMENT GOVERNANCE AND CANONICALIZATION`：为项目长期资料建立Authority Level、Status、Canonical Domain Map、替代关系、重大决策、开放问题、文档冲突、实现缺口、研究缺口和统一Codex阅读协议。

## 2. 不可破坏边界

- FamilyCenter属于FamilyOrganization，不属于Clan、Branch或Household。
- FamilyCenter必须依赖真实Facility、`FamilyManagement`能力、合法产权/控制、正式指定和真实管理者。
- Member、Residence、Estate或Asset Presence均不等于Center；历史Reference不得写成Active Center。
- 既有PersonId、ClanId、BranchId、CountyPermanentId、CoreSettlementId和Population Dataset保持不变。
- 不批量生成Permanent Person、Household、FamilyOrganization、FamilyCenter、Estate、资产或城市Facility。
- 不修改运行时代码、Scene、Prefab、Domain Model或Save Schema。
- 不自动提交或推送。

## 3. 正式交付

- `Docs/HISTORICAL_WORLD_REFERENCE/FAMILY_SPATIAL_CONSOLIDATION_V1/`：A01—A11。
- `Docs/KNOWLEDGE_BASE/`：B01—B11及8个P0城市Development Manifest。
- `outputs/HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1/`：机器可读工作数据、渲染预览、检查与验证报告。

## 4. 验收

必须通过Family Spatial ID/层级/候选/时间线审计、Document Registry路径/分类/替代链/链接/编码审计、全部工作簿公式错误与渲染检查，以及项目DocumentationOnly验证。没有修改运行时，因此编译、核心测试和Unity测试均不适用。

## 5. 实施结果（2026-08-11）

- A01—A11已生成：133个核心聚落、250个重点县、39个Canonical Clan、15个Canonical Branch与13个正式Scenario全部进入可查询框架；无证据地点保持`UNKNOWN`。
- A04/A05使用Master + Change Record + Inherited State，不逐年复制；A06形成570条稀疏Scenario快照，其中39×13=507个Clan基础组合完整存在。
- A07严格分离Residence、Estate与Asset；运行时`F088/F571/F572`不伪造为Canonical Clan，保留为`UnresolvedRuntimeClanId`及迁移冲突。
- A08的52条候选均为`REFERENCE_ONLY_DO_NOT_INSTANTIATE`；A09的18条候选均未指定Active Center，且未伪造Existing Facility。
- B01—B11已生成：登记967份长期文档/表格、33个Domain、21项冻结决策、11项开放决策、12项文档冲突、12项实现缺口、12项研究缺口，并建立8个P0城市Development Manifest。
- 16份核心入口已补充统一Document Governance Header；README、系统总纲与Skill任务路由已接入Knowledge Base。
- 17份工作簿均具有说明/数据页，公式错误扫描为0；34张渲染预览已逐页视觉检查。
- 专用验证共40项检查全部通过；Markdown内部链接/编码问题为0，916份登记Markdown均可按UTF-8读取。
- 下一阶段不是继续扩大资料框架，而是先执行184洛阳Development Readiness Review；通过后再建立`LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1`。

机器验收结果见`../outputs/HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1/validation_summary.json`。本任务没有修改Unity运行时代码、Scene、Prefab、Domain Model或Save Schema，因此编译、核心测试和Unity测试不适用。
