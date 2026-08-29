# 洛阳紧凑食品投影合同 V1

## 可缓存字段

- `LuoyangInventoryBalanceState.QuantityMilliunits`
- `LuoyangExternalSupplierRuntimeState.InventoryQuantityMilliunits`
- `LuoyangHouseholdConsumptionState.FoodReserveMilliunits`
- `LuoyangMarketRuntimeState.SupplyMilliunits`、Demand/Shortage/Price 摘要
- `LuoyangShipmentRuntimeState` 的计划量、状态与表现进度（其 cargo 实物在正式移动容器）
- DaySnapshot、ShortageResponse、AI signal 和只读 Supply Projection

## 单向规则

```text
Domain Operation
→ Formal ProductBatch / InventoryTransaction commit
→ Formal Revision + 1
→ Incremental Projection refresh 或完整 rebuild
```

禁止把投影数量反写为正式库存。唯一例外是明确版本迁移/新场景 bootstrap；该过程必须在
`IsPhysicalAuthority` 启用前执行，并产生正式化事务。

## 修订与摘要

- `Revision`：正式物理账修订。
- `ProjectionRevision`：最后一次完整/增量投影来源修订。
- `ProjectionHash`：按稳定 inventory、supplier、household 顺序对投影数量计算的 SHA-256。
- `AuthorityHash`：按稳定 container、batch、transaction/claim 顺序计算的 SHA-256。

运行时检查点要求 `ProjectionRevision == Revision`。重建时先保存 AuthorityHash，清空所有可重建食品
投影，再从正式账重建；AuthorityHash 必须保持不变，ProjectionHash 必须与重建前一致。

## Drift 失败证据

发现差异必须记录：

- First Drift Day
- Projection kind（inventory/supplier/household）
- Stable source ID
- Product ID（适用时）
- Formal quantity
- Compact quantity
- Difference

Drift 不得自动通过 Compact→Formal 修补；只能由正式事实覆盖/重建投影。
