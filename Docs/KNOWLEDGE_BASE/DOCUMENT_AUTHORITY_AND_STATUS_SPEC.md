# Document Authority and Status Specification

## Document Governance

- Purpose：冻结项目文档Authority、Status、替代关系和冲突处理规则。
- Authority：L1 Canonical System Spec / Project Governance
- Covers：REPO_HARD_RULE、L0—L4、CURRENT/CANONICAL/FROZEN/REFERENCE/ARCHIVED/SUPERSEDED状态。
- DoesNotCover：具体游戏Domain设计与运行时实现。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：README_PROJECT_KNOWLEDGE_BASE.md|../GAME_SYSTEMS_MASTER_AND_STATUS.md
- Status：FROZEN

## Authority order

```text
User current instruction
→ Repository Hard Rule (AGENTS.md)
→ L0 Project Constitution
→ matching L1 Canonical System Spec
→ L2 Current System Status
→ L3 Historical / Content / Research Reference
→ L4 Task / Implementation / Acceptance History
```

文件日期不决定权威；L4不会因“更新”自动覆盖L1。无法按既有确认设计裁决的冲突必须进入`MANUAL_REVIEW_REQUIRED`。

## Status

- `CURRENT`：当前有效入口；不必然是最高权威。
- `CANONICAL`：对应Domain当前正式规范。
- `FROZEN`：已确认且不应在普通实现任务中改变。
- `IMPLEMENTED_REFERENCE`：实现/验收证据，只证明报告明确覆盖的范围。
- `RESEARCH_REFERENCE`：研究或参考作品分析，不是实现证据。
- `HISTORICAL_REFERENCE`：历史资料或旧工程上下文。
- `ARCHIVED`：保留追溯，不代表当前顺序。
- `SUPERSEDED`：全部规则已由指定文件替代。
- `PARTIALLY_SUPERSEDED`：必须按章节说明继续使用。
- `DRAFT`、`OPEN`：尚未冻结。
- `INVALID / DO_NOT_USE`：仅用于确证错误或危险文件，不因陈旧滥用。

## Core document boundary header

L0/L1/L2文件顶部必须声明Purpose、Authority、Covers、DoesNotCover、Supersedes、SupersededBy、RelatedCanonicalDocs与Status。
旧Task和Report原则上不改写正文；通过Registry保存状态与替代关系。
