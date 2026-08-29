# 洛阳城市供给—市场—家庭消费联合能力审计 V1

审计基线：`4343237`；分支：`codex/m23-p4-quality-artisan-growth`；日期：2026-08-29。

本审计在任何联合经济实现之前完成。分类含义：`REUSE` 直接复用既有正式能力，
`VALIDATE` 能力已存在但需要本任务给出联合证据，`EXTEND` 只扩展既有只读投影或表现，
`FIX` 需要在既有权威上闭合缺口，`NEW` 仅允许新增不存在且不会成为世界权威的测试、报告或表现入口。

## 结论摘要

- `WorldState` 侧已经具备正式 `ProductBatch`、库存容器、市场订单与批次预留、交易结算、
  民用货运、CellRoute、家庭月度消费、公共采购、赈济领取与消费、持久命令和守恒审计。
- 洛阳70万人长期运行当前由 `Luoyang184LivingWorldRuntimeState` 的紧凑人物、家户、库存、
  市场、供给单和运输记录驱动。它使用同一永久人口包，但食品库存与交易没有复用
  `WorldState.ProductBatches`、`FormalMarketOrders` 和 `CivilianFreights`。
- 因而，既有成果分别证明了“70万人紧凑长期运行”和“正式批次市场物流垂直切片”，
  但尚未证明二者在同一权威库存与同一正式结算链上联合运行。这是本任务首要 `FIX`，
  不能用两个结果并排或用 `LuoyangLivingWorld` 汇总值替代联合验收。
- 现有 `LuoyangCitySupplyProjection` 是正确方向的只读投影，但字段不足，且读取
  `WorldState.People`、`ProductBatches`、`Families`、`Freight` 和 `MarketOrders` 的全量集合；
  它只能按世界日/显式刷新，不能在 Unity 每帧重建。
- 普通玩家目前能看到旧的 `LocationState.GrainPrice` 和紧凑生活世界聚合供应值，也能看到
  正式货运标记；尚无同时读取正式 Supply Projection、正式产品价格和正式阻断原因的供给卡。

## 18项必答审计

| # | 问题 | 当前事实 | 分类 |
|---:|---|---|---|
| 1 | 正式市场库存来源 | `FormalCountyMarketSystem.CreateSellOrder` 只从卖方家庭、存储设施和产品匹配的 `ProductBatchState` 取未预留数量；订单本身不产生库存。 | REUSE / VALIDATE |
| 2 | 价格公式读取事实 | 初始均衡价读取县治 `LocationState.GrainPrice` 与 `FoodDefinition.MarketValueBasisPoints`；成交价来自正式卖单价格，`LastTradeUnitPrice` 只在正式私人/公共成交时更新。当前没有库存、未满足需求、在途或交通阻断直接参与的统一报价公式。 | FIX / VALIDATE |
| 3 | Household Food Demand | 正式家户月结按家庭成员年龄生成需求：儿童和老人每月2单位、其他存活成员每月3单位，并以食品营养值结算；紧凑洛阳运行时另以永久人物记录汇总每日 milliunits。 | REUSE；跨运行时统一为 FIX |
| 4 | Public Granary 来源 | 县官仓接收正式税粮、家庭到官仓批次转移、本地公共采购和跨县公共赈济货运；发放时从官仓容器转入村公共粮仓。 | REUSE / VALIDATE |
| 5 | Order 预留 Batch | 卖单按品质、到期和稳定顺序挑选批次，增加 `ReservedQuantity` 并保存 `FormalMarketBatchReservationState`；取消、过期或成交均释放/扣减预留。 | REUSE / VALIDATE |
| 6 | AI 采购/运输需求 | `LivingWorldDecisionPolicyV2` 经动作校验器创建正式买卖单或政府采购命令；`CivilianFreightPlanningCommandScheduler` 从跨县买卖单生成 Demand/Offer 并派运。 | REUSE / VALIDATE |
| 7 | Public Procurement 结算 | 以已提交的公共粮短缺事件授权，校验政府、预算、卖单和批次；货物转入官仓，政府减资、卖方增资，写采购交易、财政账和市场价。跨县不足再走外部采购与正式民运。 | REUSE / VALIDATE |
| 8 | Household Shortfall | 家庭正式食品批次的营养供给不足月度需求时生成家庭/人物短缺结果、事务和事件；不从城市平均供应推断。 | REUSE / VALIDATE |
| 9 | Relief 到 Household | 县官仓转村公共粮仓；短缺事件形成领取需求，`HouseholdReliefPickupSystem` 按严重度与脆弱性授权并转入家庭粮仓，`HouseholdReliefConsumptionSystem` 再按领取声明结算人物营养恢复。 | REUSE / VALIDATE |
| 10 | Freight 连接 CellRoute | `CivilianFreightSystem.Dispatch` 在正式跨县市场路线腿上建立持久 CellRoute 快照；`TravelSystem` 读取 Road/Gate/Bridge 通行状态，等待或重算，到货后只卸载一次。 | REUSE / VALIDATE |
| 11 | 抽象城市粮食总量权威 | `LuoyangCitySupplyProjection` 不是权威；但 `Luoyang184LivingWorldRuntimeState` 的 `Inventories`、家庭 `FoodReserveMilliunits` 和 `LuoyangLivingWorld` 摘要目前是70万人长期运行食品事实，未接到统一 `ProductBatch` 权威。 | FIX |
| 12 | 绕过 ProductBatch | 统一农业、正式家庭、市场、采购、救济和民运不绕过；紧凑洛阳长期运行的生产、市场、供给单、运输和家庭消费绕过 `WorldState.ProductBatches`。旧 `LegacyScalar` 分支仍保留兼容但 V78 正式世界要求批次模式。 | FIX |
| 13 | Hot Path 全量扫描 | 统一市场/货运规划扫描活动订单、需求和运单；家户月结扫描到期村庄家户；紧凑运行每日扫描市场、库存和全部家户。现有 Supply Projection 还会全扫人物/批次/家户/运单/订单。70万人投影及紧凑日结需要性能证据，投影不得每帧运行。 | FIX / VALIDATE |
| 14 | 事件/到期调度 | 正式市场、货运规划、家户消费、公共粮、采购、外部采购、到货恢复、赈济领取与消费均有持久命令、事务和事件入口；135农业记录有到期索引。 | REUSE / VALIDATE |
| 15 | 玩家可见正式事实 | Unity 已有正式 CellRoute 货运标记；`SimulationDashboard` 可显示旧粮价和市场列表；`LuoyangWorldValidationController` 可显示紧凑运行供应、订单、运输和家户数据。 | EXTEND |
| 16 | 仍读旧聚合的界面 | `SimulationDashboard` 的粮价主要读取 `LocationState.GrainPrice`；`LuoyangWorldValidationController` 读取 `Luoyang184LivingWorldRuntimeState` 的食品库存、市场、供给单、运输与短缺聚合，而非 `LuoyangCitySupplyProjection`。 | FIX / EXTEND |
| 17 | 已足够、只需验证 | 正式批次预留与转移、私人/公共资金双边结算、CellRoute 门路桥阻断、仓满等待、到货幂等、家庭差异化短缺、公共采购与赈济、食品守恒、V78存档和3/3重放底座。 | VALIDATE |
| 18 | 最小 Integration Bridge | 需要一个不新增权威的正式联合场景入口；扩展现有只读 Supply Projection 的分类、价格、短缺人物、采购/承运/来源和解释字段；新增只读玩家供给卡；并明确解决或拒绝紧凑70万人库存与统一 ProductBatch 双权威。 | FIX / EXTEND / NEW（仅测试与表现） |

## 现有能力分类

### REUSE

- `AgricultureProductionSystem` / `ProductInventorySystem`
- `FormalCountyMarketSystem`
- `CivilianFreightSystem` / `TravelSystem` / CellRoute / Passage
- `FormalHouseholdFoodMonthlyCommandScheduler`
- `FormalPublicFoodMonthlyCommandScheduler`
- 本地与外部 `PublicReliefProcurement`、到货恢复、家户领取与消费
- `FormalFoodConservationAuditor`
- `WorldSnapshotSerializer`、持久命令和确定性世界时间
- `LuoyangSupplyCatchmentSelection` 与现有 `LuoyangCitySupplyProjection` 基础

### VALIDATE

- 正常30日与1年
- Gate/Road/Production/Carrier/Storage 冲击与恢复
- 私人市场和公共采购同时运行
- 家户差异、需求风暴、现金与食品守恒
- Save/Load、3/3 Replay、性能和 Unity 受控验证

### EXTEND

- 现有 `LuoyangCitySupplyProjection` 增加库存来源分类、短缺人物、价格、采购、承运、来源和解释。
- 只读玩家供给卡按显式世界日刷新并显示正式来源；不得自行改库存、价格、运单或家庭。

### FIX

- 闭合或明确阻断70万人紧凑运行时与统一正式 ProductBatch/Market/Freight/Household 权威的分离。
- 价格反馈必须来自正式订单、成交和可解释供需事实；不得由测试直接改 `LastTradeUnitPrice`。
- 投影与日常规划的全量扫描需要限频、活动集合/到期集合或性能证据。
- 玩家界面不得用旧聚合值冒充正式联合供给状态。

### NEW（受限）

- 联合压力场景测试入口、机器可读证据输出和验收报告。
- 最小只读普通玩家 Supply Card。
- 不新增任何食品、需求、市场、货运或消费 Authority。

## 开工判定

本任务可以继续，但不能把现有两套各自通过的证据直接合并为 `ACCEPTED`。实现顺序必须先扩展
只读投影和玩家入口，再用同一正式库存链建立联合场景；若70万人紧凑运行时无法在本任务内安全闭合，
最终必须保留 `NOT ACCEPTED`，并把具体架构缺口、迁移范围和已通过的独立 Gate 分开报告。
