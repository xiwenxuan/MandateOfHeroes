# FamilyOrganization、Clan、Branch、Household与FamilyCenter关系规范 V1

## Document Governance

- Purpose：冻结Person、Household、HistoricalClan、LineageBranch、FamilyOrganization与FamilyCenter关系。
- Authority：L1 CANONICAL SYSTEM SPEC。
- Covers：家族实体、产权与初始化边界。
- DoesNotCover：历史Presence事实或运行时FamilyCenter实现。
- Supersedes：早期粗粒度Family/branches/members/properties模型。
- SupersededBy：无。
- RelatedCanonicalDocs：`02_FamilyCenter设计规则_V1.md`、`../KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`。
- Status：FROZEN。

## 1. 冻结结论

本规范冻结五类实体，不允许按中文日常用语混用：

| 实体 | 含义 | 可否直接拥有组织资产 | 可否自动产生FamilyCenter |
|---|---|---:|---:|
| `Person` | 永久人物、私人权利与私人资产主体 | 仅私人资产 | 否 |
| `Household` | 共同居住、消费、照护和日常财产的生活家户 | 家户共同资产 | 否 |
| `Clan` | 历史宗族、姓族与谱系认同的长期历史实体 | 否，除非另建组织产权记录 | 否 |
| `Branch` | Clan内部谱系分支；不是管理机构 | 否 | 否 |
| `FamilyOrganization` | 拥有族产、职位、账簿、产业、档案或私军的组织主体 | 是 | 否，仍需真实Facility与指定 |
| `FamilyCenter` | FamilyOrganization指定的正式管理中心状态 | 它不是独立所有者 | 不适用 |

最高原则：家族成员可以在没有FamilyCenter的城市正常居住、任官、经商、买地和发展；FamilyCenter限制的是FamilyOrganization在当地的正式组织管理能力，而不是族人的存在与发展能力。

## 2. 分离规则

1. 一个Clan可以没有FamilyOrganization，也可以在不同年代形成多个FamilyOrganization。
2. 一个Branch可以跨多个家户；一个家户也可包含姻亲、仆役或不同Clan成员。
3. 同姓、同Clan、本籍相同、同城任官、共同住宅或拥有土地，都不能单独证明FamilyOrganization存在。
4. 成员加入组织不自动把私人资产变成族产；组织资产也不因家主死亡进入私人遗产。
5. `FamilyCenter`属于FamilyOrganization，绝不属于Clan或Branch。
6. 历史空间必须分层记录：`ClanPresence`、`BranchPresence`、`MemberPresence`、`ResidenceEvidence`、`EstateEvidence`、`FamilyAssetEvidence`、`FamilyCenterEvidence`。
7. 史料人物在洛阳只证明人物在洛阳；没有住宅证据时不得分配精确Facility或Cell。

## 3. 初始化合同

`FamilyOrganizationInitializationReference`只是剧本候选桥梁：

```text
Scenario + Clan + optional Branch
    -> candidate FamilyOrganization boundary
    -> evidence review
    -> members/assets/authority/facility/manager materialization
```

它不得执行“39个Clan生成39个FamilyOrganization”，也不得把同Clan的竞争政治集团静默合并。真正物化至少需要：明确成员边界、组织资产、合法权力来源、可追溯账簿，以及（若要建立中心）真实Facility、`FamilyManagement`能力、组织产权/控制、管理者Person与正式指定。

## 4. 证据等级

- `HISTORICAL`：史料直接支持该层事实。
- `RECONSTRUCTED`：多条史料共同支持的保守复原。
- `MODELED`：为玩法或数据完整性建立的项目模型。
- `UNKNOWN`：不能确定，禁止静默补全。

FamilyCenter专用等级：`HISTORICAL_CENTER_EVIDENCE`、`RECONSTRUCTED_CENTER_CANDIDATE`、`MODELED_CENTER_CANDIDATE`、`UNKNOWN`。庄园、宅第或祠堂证据不得自动升级为中心证据。

## 5. 存档与迁移

新增普通宗族、Branch、设施类型或中心候选必须使用稳定命名空间ID和数据定义。旧存档里的成员、家户、资产与中心状态必须顺序迁移；缺失ID保留原引用并报告，禁止重新随机、合并或删除永久人物。
