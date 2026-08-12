# Living World Runtime Contract V1 执行报告

## 结论

项目已从“历史资料与多个原型并存”进入第一版统一 Living World 运行合同。V71 不建立第二个世界：Reference 只负责开局、校准、历史惯性和调试；运行权威仍是既有 Person、Household、Organization、Cell、Facility、Inventory、Market、Office、Force、Route、Order、Shipment 与事件状态。

新增的 `WorldSignal/DecisionContext` 从真实世界事实重算；Rule、Utility、Historical Constraint、Neural Adapter 和稳定随机包装策略只对行动打分；`WorldActionValidator` 统一返回 VALID、INVALID、DEFERRED 或 PARTIALLY_EXECUTABLE，任何策略都不能跳过库存、人物、路线、Cell、产权和领域命令。随机策略装饰器在扰动后再次校验候选，不能把非法动作恢复为可执行动作。

旧 `HistoricalEventSystem` 已原位升级，没有复制事件引擎。结构化事件禁止“年份即触发”，支持 Canonical、Variant、Delayed、Transformed、Prevented；ChangePackage 记录逐操作应用 ID，可离屏改变真实 Facility、Person、FamilyCenter、Office、Route、Army 等事实，保存后不会重复执行。

## 实现边界

- 复用 V33 持久命令—事务—出站箱，不复制命令总线。
- 复用正式市场、民用货运和军需运输；县级 127 条供应关系与 4,471 条走廊只作 Reference/候选图。
- World Seed 继续使用 `WorldState.MasterSeed`；V71 新增每主体决策序号、PolicyVersion、ModelVersion 与 LOD 调度状态。
- Signal 可以缓存，但其权威来源始终可重算；本轮没有把 Signal 保存成第二套经济账。
- 洛阳189/190是合同原型，不是完整史实内容包；绑定到现有真实对象，不重新生成洛阳。

## 40项核心问题回答

| # | 回答 | 证据/边界 |
|---:|---|---|
| 1 | 是 | Reference 与 Runtime Driver 已写入代码、总纲和审计表。 |
| 2 | 是 | Snapshot 只初始化起点。 |
| 3 | 是 | 连续运行无未来 Snapshot 自动导入入口。 |
| 4 | 是 | Population 进入 Food/Housing/Employment 等 Signal。 |
| 5 | 是 | 活人劳动能力重算 LaborAvailability。 |
| 6 | 是 | 人口下降降低劳力，允许需求与聚落收缩；完整收缩平衡仍待专项。 |
| 7 | 是 | Policy 只返回 `WorldActionIntent`。 |
| 8 | 是 | `WorldActionValidator` 与既有领域命令/事务最终执行。 |
| 9 | 是 | `RuleDecisionPolicy`。 |
| 10 | 是 | `UtilityDecisionPolicy` 为 V1 主智能策略基础。 |
| 11 | 是 | `NeuralDecisionPolicyAdapter` 只接受特征并输出分数。 |
| 12 | 不能 | NN候选仍必须通过统一验证。 |
| 13 | 是 | 使用非零 `MasterSeed`，并由 `WorldSeedService`寻址。 |
| 14 | 是 | 不同 Seed 的稳定扰动轨迹产生差异。 |
| 15 | 是 | 固定 Seed/版本/序号可复现。 |
| 16 | 是 | V1 不含在线训练。 |
| 17 | 否 | 不存在由 Reference 自动发货的运行入口。 |
| 18 | 是 | 历史供应关系仅用于初始化、候选路线和校准。 |
| 19 | 是 | 复用 FormalMarket、PersistentWorldCommand、MilitaryLogistics 等正式订单。 |
| 20 | 是 | 复用 CivilianFreight/MilitaryLogistics 的真实 Shipment。 |
| 21 | 是 | 起运来自真实预留 ProductBatch。 |
| 22 | 是 | 政府采购/调拨继续检查国库、库存和承运。 |
| 23 | 是 | 军需继续检查真实批次、载具、人员和路线。 |
| 24 | 是 | V71 保存 Hot/Warm/Cold LOD 状态。 |
| 25 | 是 | Cold 只降低频率；永久人物、设施、库存不变。 |
| 26 | 是 | 既有 Anchor 已升级为条件式运行状态机。 |
| 27 | 不会 | 结构化 Rule 必须至少有一个非时间条件。 |
| 28 | 是 | Canonical。 |
| 29 | 是 | Variant。 |
| 30 | 是 | Delayed。 |
| 31 | 是 | Transformed。 |
| 32 | 是 | Prevented。 |
| 33 | 是 | 事件不依赖玩家或表现对象在场。 |
| 34 | 是 | 原型实际改变 Facility/Person/FamilyCenter/Office/Route/Army。库存变化仍必须走既有库存事务。 |
| 35 | 是 | 事件完成后不锁定后续 AI。 |
| 36 | 是 | 已完成合同级原型，未宣称完整189/190内容。 |
| 37 | 是 | Arena 保存场景、Seed、策略集、时长、指标和轨迹。 |
| 38 | 是 | 可替换 Rule/Utility/Neural Adapter；本轮不训练模型。 |
| 39 | 是 | 不重建洛阳，受保护 400K/80,899/2,084 合同保持。 |
| 40 | Hot/Warm/Cold专项 | Policy与Arena基础已足够进入全量永久人物调度深化。 |

## 交付与验证摘要

- Schema：V70→V71 顺序迁移。
- 新增针对性核心测试：44项，当前44/44通过；含V70→V71迁移、100/1000/1182代理、1000事件Watcher和1000订单/运输候选批次。
- 全工程编译：通过。
- Unity EditMode：本任务目标2/2、补充ChangePackage/批次5/5及V70→V71迁移1/1通过。
- Unity PlayMode：Living World事件/Arena 2/2通过；洛阳场景Smoke 1/1通过。
- 洛阳40万生产消费闭环EditMode：15/15通过。
- 首次沙盒内Unity启动曾因120秒无启动日志被安全终止；同一安全脚本在获准的沙盒外执行后全部通过，无残留Unity进程。
- 验证证据见 `validation_summary.json`；本轮没有宣称运行全仓库所有历史核心测试。
- 未提交、未推送。
