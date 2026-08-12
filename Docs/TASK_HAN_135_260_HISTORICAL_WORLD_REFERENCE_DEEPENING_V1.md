# HAN-135-260-HISTORICAL-WORLD-REFERENCE-DEEPENING-V1 任务书

状态：已完成并通过专用验证（2026-08-10）

## 目标

在既有 `Docs/HISTORICAL_WORLD_REFERENCE` 内，把13州治、105郡国治所候选和77战略城市去重为统一的 Canonical Core Settlement Network，并将核心聚落、重点县、Clan/Branch/Estate Reference、产业资源、交通、军事与13个Scenario深化到可直接进入开发准备审查的程度。

本任务是资料深化，不建立第二套世界，不修改运行时人口、人物、家庭、设施、存档或Unity场景。

## 强制合同

- 一处物理聚落只有一个 `PlacePermanentId`；州治、郡治、县治、都城和战略城市是有有效期的角色。
- 查询使用 `Master → 最新Timeline/Change Event → Scenario Snapshot`，未变化字段继承，不生成126份重复世界。
- 所有结论分为 `HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`。
- 105项治所采用可审计候选，不把县序候选冒充全部完成的治所考证。
- Clan、Branch、Estate、FamilyOrganization互不等同；地产线索不自动物化。
- 洛阳70万供给圈包含40万都市圈，禁止相加；其他城市不得继承洛阳人口比例。
- 不新增或改写P0001—P1202、39 Clan和15 Branch。

## 交付

- 深化索引与覆盖报告：`Docs/HISTORICAL_WORLD_REFERENCE/DEEPENING_V1/`。
- 133个核心聚落Master、105个郡国区域档、250个重点县档、39个Clan档和13个Scenario空间档。
- 9份全国/区域总索引工作簿和8份P0核心城市合并资料工作簿。
- 可复现生成器、工作簿生成器和专用验证器。

## 验收顺序

1. Core Settlement重复与角色去重审计。
2. 13州治、105郡国治所候选、77城覆盖与ID解析。
3. 人物、Clan、Branch、Scenario与Timeline连续性审计。
4. 证据等级、Markdown链接、工作簿结构与公式错误审计。
5. 文档模式验证、`git diff --check`和范围审阅。

## 完成后的开发顺序

暂停扩大历史资料框架，先执行 Development Readiness Review，再从洛阳、许昌、襄阳/江陵、成都、建业、邺、长安的现有资料纵向切片进入实际开发。
