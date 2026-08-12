# 统一世界设计文档目录

> 统一目录：`<repo-root>/Docs`
> 最近整理：2026-08-09

## 1. 用途

本文件集中列出本轮“统一东汉世界、设施、人口、权力、军事、皇室与政治AI”设计所涉及的
原始资料、正式归并结果和项目既有权威文档。文件都已归档在项目 `Docs` 目录；项目 Skill 的
任务路由因运行机制要求继续保留在 `.codex/skills/mandate-unity-development/references`。

需要经常把项目资料上传给网页版ChatGPT时，从
[`GPT_HANDOFF/README.md`](GPT_HANDOFF/README.md)进入轻量对接包；该包只负责沟通摘要，
本目录列出的正式设计和系统总纲仍是权威来源。

## 2. 本轮正式结果

后续跨系统设计首先遵循：

1. [统一世界、设施、权力、皇室与政治AI设计](UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md)
2. [统一东汉世界设计V2合并审计报告](REPORT_UNIFIED_WORLD_DESIGN_V2_MERGE_AUDIT.md)

第一份是正式跨系统设计；第二份记录来源指纹、冲突裁决和仍未冻结的候选，不替代正式规则。

## 3. 本轮归档的四份原始资料

以下文件保持原文，只作为来源记录。它们与当前项目规则冲突时，不得越过正式归并文档和系统总纲：

1. [统一东汉世界核心设计汇总V2](统一东汉世界核心设计汇总_V2.md)
2. [统一世界、设施、人口、权力与军事系统设计记录V1](《群雄志：仕途》统一世界、设施、人口、权力与军事系统设计记录_V1.md)
3. [FACILITY_CATALOG_V1统一设施类型、能力与成长体系](群雄志_仕途_FACILITY_CATALOG_V1_统一设施类型能力与成长体系.docx)
4. [设计增补V1：设施成长、皇室爵位与政治AI](群雄志_仕途_设计增补_V1_设施成长_皇室爵位与政治AI.md)

桌面原件仍保留；项目内副本用于版本管理和后续追溯。

## 4. 已同步的项目权威文档

- [AI项目导览](AI_PROJECT_BRIEF.md)
- [核心玩法](GAME_VISION_AND_GAMEPLAY.md)
- [系统总纲与当前状态](GAME_SYSTEMS_MASTER_AND_STATUS.md)
- [世界模拟、人口经济与地方战争](WORLD_SIMULATION_FOUNDATION.md)
- [数据驱动生产、农业、产业与成长](PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md)
- [人物属性、词条与家族培养](CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md)
- [统一战斗、军团与战争权限](UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md)
- [永久人口、分级模拟与关注演出](TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md)
- [活世界地图、有限认知与全层级委任](TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md)
- [沙盒NPC AI](SANDBOX_NPC_AI.md)

## 5. 后续阅读顺序

```text
根目录 AGENTS.md
→ 项目 mandate-unity-development Skill
→ Skill 的 task-routing.md
→ GAME_VISION_AND_GAMEPLAY.md
→ GAME_SYSTEMS_MASTER_AND_STATUS.md
→ UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md
→ 当前任务对应的领域设计
→ 当前任务书、代码、测试与存档事实
```

窄任务不需要读取全部资料：

- 判断当前状态和开发顺序：读系统总纲；
- Cell、Facility、产权、职位、爵位、皇室、政权或政治AI：读统一世界与权力设计；
- 生产、配方和职业成长：读生产专项，涉及设施成长时再加统一世界与权力设计；
- 战斗和伤亡：读统一战争，涉及军职、Force占格或政权命令时再加统一世界与权力设计；
- 人口冷热、存储和关注：读M12及对应后续人口合同。

## 6. 权威说明

原始资料归档不等于恢复其中已经被裁决的旧规则，尤其不能覆盖：

- HanWorldV1的2000米Cell合同；
- 永久人物不可删除、合并、替代或重随机；
- 开放内容使用稳定命名空间ID和数据定义；
- 存档兼容、确定性、守恒与有限认知规则；
- “已有原型、已有底座、已定方案、待研究”的状态定义。

任何新冲突先记录到合并审计，再修改正式归并文档和系统总纲；不得直接在原始资料副本中
静默改变当前规则。
