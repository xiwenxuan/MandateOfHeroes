# 历史人物与剧本输入资料整合审计 V1

## 1. 审计范围

本审计对照2026-08-10提供的五份资料与项目现有Docs、StreamingAssets、洛阳初始化包及
`HAN-135-260-HISTORICAL-PERSON-CLAN-MASTER-V1`任务要求。

## 2. 逐项结论

| 输入资料 | 既有整理程度 | 本轮处置 |
| --- | --- | --- |
| `01_140-264历史人物与时间轴母库_V5.xlsx` | 已被184洛阳包登记为来源，25名洛阳人物局部物化；全国1200人物、599候选、486原始关系及五类Timeline尚无正式全国运行时母库 | 作为不可重编号的Existing Historical Dataset导入、审计和升级，不重复另建PersonId |
| `02_历史剧本切片总索引_140-264.md` | 13个年份已进入人口快照与部分任务书，但完整主剧本治理规则未统一 | 合并到`HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md` |
| `03_历史剧本_ScenarioSnapshot_数据规范.md` | 洛阳184原型局部使用其结构，尚未形成全项目Snapshot边界文档 | 合并到统一规范；保留Clan/FamilyOrganization/Household分离规则 |
| `04_HistoricalTimePoint_StartPoint_FateDecision_规范.md` | 事件系统存在原型，TimePoint/StartPoint/FateDecision的正式区别未进入文档路由 | 合并到统一规范 |
| `05_184黄巾起义世界切片_V1.xlsx` | 已被洛阳184初始化包登记并部分物化；24人核心切片、设施与事件内容已有对应数据 | 不复制第二套184世界；作为PersonId、位置、官职与事件兼容回归基线 |

## 3. 冲突与处理

- 输入资料将“家族组织候选”用于研究聚类；本项目当前硬规则要求Clan、Branch、
  FamilyOrganization、Household分离。本轮只清洗HistoricalClan和Branch，不把599候选自动物化为家族组织。
- `05`中存在`FamilyOrganizationState`研究页；它只作为已有洛阳实例的兼容参考，不授权本任务新建、
  扩张或重写FamilyOrganization资产。
- 输入标题覆盖140—264，而人物—宗族母库任务覆盖135—260。人物生命区间与135—260有交集即可收录；
  263—264只作为260主剧本的后续TimePoint，不改变本任务的核心年度边界。
- 输入关系使用显示名；正式数据必须PersonId化。不能唯一解析的同名或缺失人物保留为未解决审计，
  不按姓名猜测合并。

## 4. 权威落点

- 历史剧本治理：`HISTORICAL_SCENARIOS_TIMELINE_AND_FATE_DECISIONS.md`；
- 历史人物—宗族执行与状态：`TASK_HAN_135_260_HISTORICAL_PERSON_CLAN_MASTER_V1.md`；
- 历史人口：`TASK_HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1.md`；
- 永久人物与冷热规则：`TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`；
- 运行时数据：`Assets/StreamingAssets/HistoricalPersons/Han135260V1/`。

外部输入仍保留为来源证据，但后续开发应从上述项目内文档和运行时数据进入，避免AI或开发者再次
从桌面文件建立第二套相互冲突的数据体系。
