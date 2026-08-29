# 洛阳 70 万人正式库存桥设计 V1

## 决策

70 万人洛阳派生运行时不再把 `LuoyangInventoryBalanceState.QuantityMilliunits`、
`LuoyangExternalSupplierRuntimeState.InventoryQuantityMilliunits` 或
`LuoyangHouseholdConsumptionState.FoodReserveMilliunits` 当作食品物理权威。唯一物理权威为同一
checkpoint 内的稀疏正式库存侧车：

```text
InventoryContainerState
+ ProductBatchState
+ InventoryTransactionState
```

该侧车不是第三套库存：旧紧凑数量在 bootstrap/v6→v7 明确转换后立即降为投影；运行时业务入口只能
先提交正式批次事务，再刷新受影响投影。不存在 Compact→Formal 的日常同步。

## 大规模粒度

- 原有设施/市场/政府/外围供应库存：每个稳定 inventory/product/source window 一个可合并批次。
- Household：使用一个洛阳家户聚合正式容器，并保存按稳定 Household 顺序的非物理分配 claim；
  claim 总和必须等于该容器食品批次总和。它是任务书允许的 `Household Aggregate Batch`，避免
  142,980 户 × 每日 × 每产品事务爆炸。
- Harvest：按 field/cycle/source work order 建批次，可在不丢来源的前提下按同一来源窗口合并。
- Consumption：每日/结算窗口生成一笔批量 `FoodConsumed` 事务；逐户需求、实吃和缺口仍保留，
  物理扣减只发生一次。
- Freight：每票 shipment 对应一个正式移动容器；起运从正式来源容器转入，显式损耗写负事务，
  到货从移动容器转入正式目的容器。
- Market/Tax/Relief：均为正式容器之间的内部转移；紧凑交易、税、赈济记录只是结果摘要。

## 守恒

```text
Opening + Harvest + External Production
= Consumption + Storage/Transport/Processing Loss + Closing Formal Batches
```

Market、Tax、Relief、Freight 装卸的全局净物理变化必须为 0。Projection 的物理影响恒为 0。

## 存档

- `WorldState` 继续 V78，本任务不把 70 万永久人物内联进 `WorldState`。
- 洛阳派生 checkpoint 由 v6 顺序升级到 v7。
- v6→v7 把当时的 compact closing stock 作为一次显式兼容正式化：设施/供应商/家户/在途分别建
  正式容器与 opening transaction；旧字段随后仅为 projection。
- v7 Load 以正式批次为准校验并重建 projection；旧 projection 不能覆盖正式批次。

## 不变量

1. `IsPhysicalAuthority == true` 后，食品 direct compact physical mutation 计数必须为 0。
2. 每个 batch 只能引用一个正式 container，数量与预留合法。
3. 每笔内部转移逐产品净变化为 0。
4. 家户 claims 总量等于 household aggregate batch 总量。
5. ProjectionRevision 不得大于 Formal Revision；重建后必须相等且摘要一致。
6. 删除/篡改投影后重建不改变正式 batch、transaction 或 authority hash。
7. Batch 和 transaction ID 稳定且不重复。

## 已知边界

紧凑市场/供给单/Shipment 仍承担有界计划、AI 输入和表现索引；它们不能提供实物。正式实物来源、
移动容器和所有增减由本桥负责。把全部 70 万 Household、Person 与完整 M25 市场命令对象内联进
`WorldState` 属于后续 HOT/WARM/COLD/分区正式世界迁移，不是本任务的性能可接受方案。
