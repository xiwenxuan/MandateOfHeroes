# Document Governance Report V1

## Document Governance

- Purpose：报告全项目文档登记、Authority/Status、冲突、缺口与后续读取方式。
- Authority：L2 Current System Status / Governance Report
- Covers：本轮扫描与分类结果。
- DoesNotCover：具体Domain规则或运行时完成证明。
- Supersedes：无
- SupersededBy：无
- RelatedCanonicalDocs：README_PROJECT_KNOWLEDGE_BASE.md|REGISTRY/PROJECT_DOCUMENT_REGISTRY.xlsx
- Status：CURRENT

## 文档治理问题验收答复

1. 登记长期文档/表格：**967**。
2. L0：**1**。
3. L1：**16**。
4. L2：**17**。
5. L3：**738**。
6. L4：**194**；另有REPO_HARD_RULE **1**。
7. CURRENT：**35**。
8. ARCHIVED：**2**。
9. SUPERSEDED：**0**。
10. PARTIALLY_SUPERSEDED：**1**。
11. 缺少单一Canonical Spec的Domain：Market|Logistics|UI。
12. 未裁决的多个L1冲突：0；旧规则冲突通过Preferred L1与Conflict Register表达，无法裁决者保留MANUAL_REVIEW_REQUIRED。
13. 最易误导的旧文档：Docs/DATA_AND_CONTENT_FOUNDATION.md|Docs/DEVELOPMENT_PLAN.md|Docs/PREPRODUCTION_BACKLOG.md，以及所有被脱离L1/L2上下文单独读取的旧Task。
14. 只需Header/Registry即可继续保留的主要是早期Task、Report、Benchmark和Reference Analysis；不重写历史正文。
15. 最小修订对象：Game Vision、Master Status、Data Foundation、World/Character/Production/Combat/AI/Save/Facility/Family等核心入口的职责边界与Cross Reference。
16. Document Conflict：**12**。
17. Implementation Gap：**12**。
18. Research Gap：**12**。
19. Family读取顺序：Game Vision → Family关系规范 → FamilyCenter规则 → Master Status → Family Spatial → 相关L4。
20. 洛阳读取顺序：Repository规则 → Domain L1 → Master → `LUOYANG_184_DEVELOPMENT_REFERENCE_MANIFEST`。
21. 其他城市：选择对应P0 Manifest，再读P0 Master、Family Spatial和相关Scenario，不重新搜旧Task拼规则。
22. 可以从`README_PROJECT_KNOWLEDGE_BASE.md`找到主要Source of Truth；Document Registry提供完整路径、状态和权威等级。

## 审计边界

扫描发现**0**条既有Markdown内部链接/编码问题，其中核心L0/L1/L2为**0**条。新Knowledge Base链接必须在验收时为零错误；历史L3/L4问题保留在`link_audit.json`，不得靠批量移动文件掩盖。

本轮为Documentation / Reference Only：没有修改Unity运行时代码、Scene、Prefab、Save Schema或Domain Model。
