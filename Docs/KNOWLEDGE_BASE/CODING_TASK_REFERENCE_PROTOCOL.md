# Coding Task Reference Protocol

## Document Governance

- Purpose：规定未来Codex/开发人员开始任务时的最小权威读取顺序。
- Authority：L1 Canonical System Spec / Development Protocol
- Covers：任务开工阅读、Source of Truth声明、冲突升级和交付分类。
- DoesNotCover：具体Domain设计。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：DOCUMENT_AUTHORITY_AND_STATUS_SPEC.md|README_PROJECT_KNOWLEDGE_BASE.md
- Status：FROZEN

## Required sequence

1. 读取`AGENTS.md`与项目Skill。
2. 读取L0 `GAME_VISION_AND_GAMEPLAY.md`。
3. 在`PROJECT_CANONICAL_DOMAIN_MAP.xlsx`选择任务Domain的L1。
4. 读取L2 `GAME_SYSTEMS_MASTER_AND_STATUS.md`确认实现状态与当前顺序。
5. 读取相关L3历史/内容/研究资料；涉及城市先读对应Development Manifest。
6. 最后读取直接相关L4 Task/Report，不能反向覆盖L1。

## Every new task must declare

- `CANONICAL REFERENCES`
- `CURRENT STATE REFERENCES`
- `HISTORICAL REFERENCES`
- `IMPLEMENTATION HISTORY REFERENCES`

若旧Task与Canonical冲突，停止并记录冲突；若Canonical规则明确但代码未实现，登记Implementation Gap；史料不足登记Research Gap。禁止把三者混在一起。
