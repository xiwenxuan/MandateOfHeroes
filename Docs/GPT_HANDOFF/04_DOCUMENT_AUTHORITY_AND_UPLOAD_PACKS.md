# 文档权威顺序与GPT上传组合

> 本文帮助用户决定“这次要给GPT上传哪些文件”。

## 1. 仓库内的正式权威顺序

```text
用户当轮明确指令
→ 根目录 AGENTS.md
→ mandate-unity-development/SKILL.md
→ Skill的task-routing.md
→ GAME_SYSTEMS_MASTER_AND_STATUS.md
→ 当前任务对应的正式领域设计
→ 当前任务书
→ 现有代码、测试和存档兼容事实
```

`AI_PROJECT_BRIEF.md`和本目录只负责快速导览，不能替代上述权威。

## 2. 每次都建议上传

网页版GPT首次接触项目时上传本目录六份文件。若上下文受限，最少上传：

1. `01_PROJECT_BRIEF_FOR_GPT.md`；
2. `02_NON_NEGOTIABLE_RULES.md`；
3. `03_CURRENT_STATUS_AND_PRIORITIES.md`；
4. `06_DECISION_AND_CHANGE_RETURN_TEMPLATE.md`。

## 3. 按问题追加的资料

### 3.1 总体玩法、身份和多代人生

- `../GAME_VISION_AND_GAMEPLAY.md`
- `../GAME_SYSTEMS_MASTER_AND_STATUS.md`

### 3.2 统一世界、Facility、产权、职位、皇室和政治AI

- `../UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`
- `../REPORT_UNIFIED_WORLD_DESIGN_V2_MERGE_AUDIT.md`（需要追溯冲突时）

原始四份讨论资料通常不必再次上传；只有需要核对原话或候选细节时，才从
`../UNIFIED_WORLD_DESIGN_DOCUMENT_INDEX.md`选择。

### 3.3 地图、城镇、有限认知和地图美术

- `../WORLD_SIMULATION_FOUNDATION.md`
- `../TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md`
- `../MAP_ART_RESOURCE_PLAN.md`
- `../TASK_MASTER_MAP_V1_LUOYANG_POPULATION_FACILITY_CELL_CAPACITY.md`
- 当前城镇或地图任务书，例如M26-P5B。

### 3.4 生产、农业、设施经营和科研

- `../PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`
- `../GAME_SYSTEMS_MASTER_AND_STATUS.md`
- 涉及Facility成长时再加统一世界与权力设计。

### 3.5 人物属性、技能、教育与家族培养

- `../CHARACTER_ATTRIBUTES_TRAITS_AND_GROWTH.md`
- 涉及永久人物冷热和规模时加 `../TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`。

### 3.6 人口、家庭、冷热存储和超大规模

- `../TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`
- 当前具体M15/M20/M21/M24任务书和报告；
- 不要一次上传全部人口历史任务，只选择这次问题直接相关的证据。

### 3.7 战斗、军团、战争和军需

- `../UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md`
- 涉及军职、Force占格、政权或政治AI时加统一世界与权力设计；
- 涉及具体物流时再加对应M23任务书。

### 3.8 沙盒NPC与组织委任

- `../SANDBOX_NPC_AI.md`
- `../TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md`
- 涉及政治AI时加统一世界与权力设计；
- 涉及永久人口调度时加M12。

### 3.9 要求GPT写可执行任务书

除本对接包外，还要上传：

- 当前领域权威设计；
- 系统总纲中对应状态和全局顺序；
- 上一个直接相关任务书及完成报告；
- 如果要改代码，提供相关源码、测试、存档版本和当前错误/验证结果。

GPT只能形成任务书候选，Codex执行前必须重新检查本地工作区和最新代码。

## 4. 不建议上传的组合

- 不要一次上传几百份历史任务书；
- 不要只上传一份旧任务书而不上传当前总纲；
- 不要把原始讨论资料当作已经裁决的最终规则；
- 不要只给截图而不给可搜索的文本和当前版本；
- 不要让GPT依据参考游戏介绍直接生成可复制的商业内容。

## 5. 版本说明建议

每次上传时在消息开头附上：

```text
资料快照日期：<日期>
项目分支/提交（如知道）：<值>
本次问题：<问题>
本次需要：讨论 / 方案 / 决策对比 / 任务书 / 文档合并
已经接受的前置决定：<列出>
禁止改变的边界：<列出>
```

讨论结束后让GPT按 `06_DECISION_AND_CHANGE_RETURN_TEMPLATE.md`输出，方便Codex识别哪些是建议、
哪些已由用户接受、哪些需要修改文档或代码。
