# 洛阳 70 万人食品经济权威矩阵（修复前冻结）

审计基线：`4343237b5f15e8da1ab1e137f1bc73fa95e0cd77`；工作区同时保留上一联合任务的未提交成果。

分类定义：

- `DOUBLE_AUTHORITY`：同一洛阳业务概念在 70 万人紧凑路径和独立正式 `WorldState` 路径中各有可写物理事实；目前不是同一次调用双写，但两边均可被解释为真实库存。
- `COMPACT_ONLY`：70 万人实际长期运行只写紧凑事实。
- `FORMAL_ONLY`：只有正式批次路径拥有该事实，70 万人路径没有等价物。
- `FORMAL_WITH_CACHE`：目标状态；正式批次/事务唯一可写，紧凑字段仅为带修订与摘要的可重建投影。
- `UNKNOWN`：尚未识别写入者或读取者。本矩阵审计后为 **0**。

## 矩阵

| 世界事实 | 当前写入者 | 当前读取者 | 当前持久化 | 当前权威/分类 | 正式目标权威 | 迁移 | 可派生/缓存 | 冲突风险 |
|---|---|---|---|---|---|---|---|---|
| Food Opening Stock | `Luoyang184LivingWorldSystem.BuildOpeningInventories` 写 `LuoyangInventoryBalanceState`；正式世界由 `FoodStockFormalizationSystem`/`ProductInventorySystem` 建批次 | 紧凑生产、市场、供给、审计；正式食品系统 | 洛阳 checkpoint v6；WorldState V78 | `DOUBLE_AUTHORITY` | `ProductBatchState` + `OpeningBalance`/正式化事务 | 必须 | 可按 inventory/product 投影 | 高：开局粮可能被解释两次 |
| Seed | 紧凑 `BuildCrops`/`Harvest`/`TrySowNextCycle` 写 seed inventory；正式农业仍有正式批次与兼容种粮合同 | 两套农业路径 | 两套存档 | `DOUBLE_AUTHORITY`，且 70 万人仍 `COMPACT_ONLY` | 本任务食品范围内留种来源写正式批次；播种扣正式批次或明确兼容边界 | 必须 | 仅摘要可缓存 | 高 |
| Harvest Output | 紧凑 `Luoyang184LivingWorldSystem.Harvest` 增 `QuantityMilliunits`；正式 `AgricultureProductionSystem`/`ProductInventorySystem` 建 `FoodHarvested` 批次事务 | 紧凑库存/市场；正式库存/市场 | 两套存档 | `DOUBLE_AUTHORITY` | 正式批次/事务唯一 Source | 必须 | Harvest summary 可派生 | 极高 |
| Household Food | `AllocateOpeningHouseholdFood`、智能体买粮/赈济、玩家买粮、税粮和消费直接改 `FoodReserveMilliunits`；正式世界改家庭粮仓批次 | 紧凑消费/AI/税粮；正式消费/市场/救济 | checkpoint v6 与 WorldState V78 | `DOUBLE_AUTHORITY` | 聚合正式 household inventory/batch；紧凑 reserve 为投影 | 必须 | 可缓存，必须可重建 | 极高 |
| Village Food | 紧凑运行没有独立稳定 village batch，公共粮由普通 inventory/政府仓表达；正式世界有村公共粮仓容器 | T4 税粮/治理；正式公共粮命令 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 village public inventory | 必须 | 可派生 | 高 |
| County Food | 紧凑 `GovernmentEconomy.GranaryInventoryId` 指向普通 inventory；正式世界有 county granary container | 紧凑政府采购/赈济/税粮；正式公共粮/采购 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 county public inventory | 必须 | 可派生 | 高 |
| Public Granary | `Luoyang184T4IntegratedRuntimeSystem` 和智能体政府动作直接改 compact inventory；正式 M25-P3/P11/P12+ 改正式批次 | 紧凑治理、短缺；正式治理/采购/救济 | 两套存档 | `DOUBLE_AUTHORITY` | 正式公共容器批次与成对转移事务 | 必须 | public summary 可缓存 | 极高 |
| Market Sellable Stock | 紧凑 `LuoyangInventoryOwnerKind.Market` 数量；正式卖单只预留家庭正式批次 | 紧凑 AI/玩家/价格；`FormalCountyMarketSystem` | 两套存档 | `DOUBLE_AUTHORITY` | 正式未预留 batch/正式 sell reservation | 必须 | 价格/可售汇总可缓存 | 极高：可能出现 2X |
| Reserved Stock | 紧凑市场没有批次预留；正式 `FormalMarketBatchReservationState`/`ReservedQuantity` | 正式市场/货运 | WorldState V78 | `FORMAL_ONLY` | 保持正式 | 否 | 可缓存 | 低；紧凑市场必须禁用物理来源 |
| Freight Stock | 紧凑 supplier scalar + `LuoyangShipmentRuntimeState` 数量；正式 `CivilianFreightSystem` 把 batch 转入 mobile container | 两套到货、价格和投影 | checkpoint v6 与 WorldState V78 | `DOUBLE_AUTHORITY` | 正式 batch/mobile container/freight ledger | 必须 | shipment view 可缓存 | 极高 |
| Food Consumption | `SettleHouseholdConsumption` 直接扣 reserve；T4 军粮另扣 compact inventory；正式 `FoodInventorySystem` 建 `FoodConsumed` 负事务 | 紧凑 shortfall/person reconciliation；正式营养/shortfall | 两套存档 | `DOUBLE_AUTHORITY` | 正式负事务唯一 Sink | 必须 | actual/shortfall 可缓存 | 极高：双扣风险 |
| Food Shortfall | 紧凑 `CumulativeFoodShortageMilliunits`；正式家庭月结 shortfall/event | AI、治理、报告 | 两套存档 | `DOUBLE_AUTHORITY`（派生事实重复计算） | `formal demand - formal actual consumed` | 必须 | 是，只读投影 | 高：可能判两次 |
| Storage Loss | 紧凑 flow 的 opening/processing loss 与显式 sink；正式 `FoodStorageNaturalLoss` | 两套守恒审计 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 loss transaction | 必须 | 可汇总 | 高 |
| Transport Loss | 紧凑 shipment carrier/natural/risk loss；正式 civilian freight natural loss transaction | 两套货运与守恒 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 freight loss transaction | 必须 | 可汇总 | 极高 |
| Tax Transfer | `Luoyang184T4IntegratedRuntimeSystem.ResolveAnnualInKindTax` 直接扣 reserve/inventory 并增政府仓；正式 M25-P3/P11 成对批次转移 | 紧凑财政/治理；正式公共粮 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 batch transfer，净物理变化 0 | 必须 | 税粮摘要可缓存 | 极高 |
| Relief Transfer | 智能体政府赈济直接减政府 inventory、增 reserve；正式县仓→村仓→家庭批次链 | 两套家庭安全与治理 | 两套存档 | `DOUBLE_AUTHORITY` | 正式 transfer transaction，净物理变化 0 | 必须 | 可汇总 | 极高 |
| Compact Food Balance | `Inventories.QuantityMilliunits`、supplier scalar、shipment scalar、household reserve 均可写 | 紧凑全部经济与旧审计 | checkpoint v6 | `COMPACT_ONLY`（70 万人当前运行） | `FORMAL_WITH_CACHE` | 必须 | 是；需 revision/hash/rebuild | 极高 |
| Formal Batch Balance | 正式 ProductInventory/FoodInventory/Market/Freight/Public systems | 正式所有经济系统和 Auditor | WorldState V78 | `FORMAL_ONLY`（目前仅独立正式 fixture） | 唯一物理权威 | 需接管 70 万人路径 | 可生成紧凑投影 | 当前未覆盖 70 万人 |

## 13 个必答结论

1. 70 万人 Harvest 当前只写 compact；独立正式世界 Harvest 写 ProductBatch；整体是平行双权威。
2. 同一次 70 万人 Harvest 当前不会写 formal batch，所以不是函数内双写；问题是结果无法和正式市场/物流联合解释。
3. 70 万人 Household Consumption 只扣 `FoodReserveMilliunits`；独立正式世界只扣 ProductBatch。
4. 同一次消费当前不会双扣，但两套 Shortfall 可分别成立。
5. 70 万人 Market 读取 compact market inventory；正式市场只读正式 batch reservation。
6. 70 万人 Freight 从 supplier scalar 装货；正式 freight 从已预留 ProductBatch 装入移动容器。
7. 70 万人 Shortfall 读取 compact reserve；正式 shortfall 读取正式实际营养消费。
8. 70 万人 Public Granary 是普通 compact inventory；正式世界是组织 inventory container/batch。
9. 保存时存在两种不同文件合同：checkpoint v6 保存一套紧凑粮，WorldState V78 保存一套正式粮；当前没有相互覆盖，因为它们是两个世界，而这正是联合验收失败根因。
10. Load 后各自恢复各自事实，没有单向 Formal→Projection 验证。
11. `LuoyangCitySupplyProjection` 本身只读；`Luoyang184LivingWorldRuntimeState` 的同名汇总不是只读投影，而是当前 70 万人物理权威。
12. 70 万人仍停留在 compact 的路径：Opening、supplier production、harvest/seed、processing、market buy/sell/government buy、shipment/loss/arrival、household reserve/consumption/shortfall、tax、relief、部分军粮。
13. 修复目标：正式批次/事务唯一可写；compact inventories/household reserve/market/shipment 只保留由正式账重建的查询和表现摘要。

## Writer/Reader 完整性结论

已识别食品物理写入文件：

- `Luoyang184LivingWorldSystem.cs`
- `Luoyang184IntelligentAgentRuntimeSystem.cs`
- `Luoyang184PlayerCommandSystem.cs`
- `Luoyang184T4IntegratedRuntimeSystem.cs`
- `LuoyangVisualPresentationSystem.cs`（蓝图采购入口会建立紧凑供给单/运单）
- 正式 `AgricultureProductionSystem.cs`、`FoodInventorySystem.cs`、`FoodStockFormalizationSystem.cs`、`FormalCountyMarketSystem.cs`、`CivilianFreightSystem.cs` 及正式公共粮/救济系统

已识别持久化边界：`Luoyang184LivingWorldCheckpointStore` 与 `WorldSnapshotSerializer`。审计未留下未分类路径：`UNKNOWN = 0`。
