# FamilyCenter设计规则 V1

## Document Governance

- Purpose：冻结Primary/Local/Remote/Disabled FamilyCenter成立、范围、迁移与动作规则。
- Authority：L1 CANONICAL SYSTEM SPEC。
- Covers：FamilyCenter规则与候选判定。
- DoesNotCover：ACTIVE_CENTER历史结论或运行时实现证明。
- Supersedes：单总部和Branch即Local Center等早期简化。
- SupersededBy：无。
- RelatedCanonicalDocs：`01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md`、`../UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`。
- Status：FROZEN。

## 1. 成立条件

FamilyCenter不是“加成建筑”，而是某个真实Facility在特定FamilyOrganization下获得的管理指定。以下条件必须同时成立：

1. 真实Facility存在且未被摧毁；
2. Facility具备数据驱动能力 `FamilyManagement`；
3. Facility由该FamilyOrganization合法所有或控制；
4. 组织正式指定它为`PrimaryFamilyCenter`或`LocalFamilyCenter`；
5. 指派真实Person担任管理者并能实际履职。

一个FamilyOrganization最多一个Primary，可以有多个Local；同一ManagementArea内最多一个中心，Primary已覆盖本地时不得再建Local。不要使用`BranchFamilyCenter`，避免与谱系Branch混淆。

## 2. 类型、能力与承载设施

采用能力模型，不把中心锁死为单一BaseType。可以新增标准内容定义`facility.family_hall`作为常见候选，但宅第、庄园、商馆、坞堡等只有显式具备`FamilyManagement`能力并满足全部成立条件时才能承载中心。祠堂/宗庙只提供礼仪能力；住宅只提供居住能力；田地、仓库和工坊也不会自动组成中心。

## 3. 管理范围

中心绑定明确`ManagementAreaId`，可指向Settlement、UrbanArea、County、EstateComplex或其他已定义区域，不使用任意圆形半径。资产必须逐项分配到中心；处在几何范围内不等于受其管理。

状态冻结为：

- `NONE`：组织在当地没有中心，也没有远程管理关系；
- `REMOTE`：由其他中心有限监督，只能传递少量命令和报告；
- `LOCAL`：本地中心可以执行动作矩阵允许的地方组织行为；
- `PRIMARY`：组织根账、最高职位和跨区决策所在地，同时承担本地中心职责；
- `DISABLED`：设施失效、失去控制或管理者缺位导致中心停用。

## 4. 人员、通信与失效

中心指定可在管理者缺位时保留，但立即进入`DISABLED/UNSTAFFED`，除紧急保全和等待任命外不得执行正式管理。远程命令必须等待道路、信使、旅行和信息更新；不得跨城即时共享账簿、库存、军情或职位。

设施被毁、夺取或失去控制时，中心失效，但人物、家户、土地、库存、债务和其他资产各自按真实状态保留。远地资产若仍有人员、材料和运营条件可继续日常运作，但无法凭空获得新的组织预算、建设和任命。

## 5. 迁移、升格、撤销与分立

- 迁移Primary：新Facility先满足条件，完成档案、账簿、职位和必要库存交接后指定；旧Primary必须明确降为Local、撤销或废弃。
- Local升Primary：是同一中心的指定变化，不复制资产。
- 撤销Local：仅撤销管理资格，不删除当地成员或资产。
- FamilyOrganization分立：成员、中心、债务和每项组织资产必须明确分配；个人资产仍归Person，禁止平均复制或按姓氏自动切割。

## 6. 二十项开放问题冻结表

| # | 问题 | 状态 | V1结论 |
|---:|---|---|---|
| 1 | 中心采用能力还是BaseType | FROZEN | 采用`FamilyManagement`能力模型。 |
| 2 | 是否提供标准FamilyHall | FROZEN | 提供数据驱动标准候选，但不是唯一承载类型。 |
| 3 | 是否必须有管理者 | FROZEN | 必须有真实Person；缺位时指定保留但中心停用。 |
| 4 | 是否允许远程监督 | FROZEN | 允许极弱REMOTE，受通信、距离和人员约束。 |
| 5 | Local可做什么 | FROZEN | 仅做动作矩阵中的本地资产、预算、人员和设施管理。 |
| 6 | Primary专属什么 | FROZEN | 根账、最高职位、跨区大宗调拨、建撤Local、迁移和分立。 |
| 7 | 每区域几个中心 | FROZEN | 同一组织在同一ManagementArea最多一个。 |
| 8 | 中心范围如何定义 | FROZEN | 用显式ManagementAreaId和资产分配，不用半径。 |
| 9 | 庄园能否承载 | FROZEN | 可，但必须有真实Facility及完整能力/产权/人员条件。 |
| 10 | 住宅能否承载 | FROZEN | 条件同上；居住能力本身不足。 |
| 11 | 中心摧毁后资产怎样 | FROZEN | 资产独立保留或按真实事件损毁/转移，不随中心删除。 |
| 12 | 无管理者怎样 | FROZEN | `DISABLED/UNSTAFFED`，暂停正式管理。 |
| 13 | Primary怎样迁移 | FROZEN | 先建新中心并交接，再改变唯一Primary指定。 |
| 14 | Local能否升Primary | FROZEN | 可以；旧Primary须明确降格/撤销/废弃。 |
| 15 | 分家怎样处理 | FROZEN | 成员、中心、债务、组织资产逐项分配，禁止复制。 |
| 16 | Clan怎样生成剧本组织 | FROZEN | 只通过InitializationReference候选与证据审核，不自动生成。 |
| 17 | 一个Clan能否多个组织 | FROZEN | 可以，尤其不同Branch或政治集团。 |
| 18 | 住宅何时成为候选 | FROZEN | 具备长期组织管理证据、合法控制和可承载Facility时。 |
| 19 | 只有外地官员在京怎样 | FROZEN | 仅`MemberPresence`，不建立Branch、组织或中心。 |
| 20 | 洛阳184哪些组织应有中心 | OPEN_WITH_RECOMMENDATION | 当前7个均无真实Facility，不正式指定；先修成员边界，再研究皇室特殊中心、何氏及杨/袁地方候选。 |

## 7. 本轮禁止项

本轮不实现全国FamilyOrganization、Household、普通Clan资产、庄园或FamilyCenter Facility；不修改洛阳7组织运行时数据；不实现通信系统。相关表格是开发参考，不是运行时事实。
