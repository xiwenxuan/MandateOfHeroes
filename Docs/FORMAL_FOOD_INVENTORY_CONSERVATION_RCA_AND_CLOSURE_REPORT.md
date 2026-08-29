# 正式食品库存守恒差额 RCA、修复与长期生活闭环 V1

## 1. 交付身份

```text
Task: TASK_FORMAL_FOOD_INVENTORY_CONSERVATION_RCA_AND_CLOSURE_V1
Branch: codex/m23-p4-quality-artisan-growth
Baseline HEAD: a87a306de4da6b4f76f841cb3d98487d692c5b1a
Unity Version: 2022.3.62f3c1
World Save Version: V77 (unchanged)
Luoyang Derived Checkpoint Version: v6 (unchanged)
Code/Core Closure: PASS
Formal Acceptance: ACCEPTED
```

食品差额的 RCA、代码修复、核心回归和适用 Unity 目标回归均已闭合。本轮结果全部来自新执行，
没有使用历史 XML 代替。

## 2. Initial Symptom 与三次精确复现

原场景为洛阳 184 年、Seed `184`、400,000 Person、80,899 Household、2,084 Facility 的
365 日生活证据：

| 项目 | 原值（milliunits） |
|---|---:|
| Imported | 13,283,375 |
| Harvested | 461,890,000 |
| Consumed | 26,008,292 |
| Processing Loss | 16,170,000 |
| Closing | 445,720,000 |
| Left | 475,173,375 |
| Right | 487,898,292 |
| Signed Difference (`Left - Right`) | **-12,724,917** |
| Absolute Difference | **12,724,917** |

在不修改生产代码的情况下，用原测试入口启动三个独立 Core 进程，三次均得到同一失败：

```text
Expected: 487898292
But was: 475173375
```

冻结证据位于 `tmp/food-conservation/reproduction-core-run-1..3/`，机器摘要为
`tmp/food-conservation/baseline-summary.json`。

## 3. Food Conservation Boundary

### 3.1 正式世界

正式世界的食品库存权威是 `FoodInventoryAuthorityMode.FormalProductBatches`。食品集合来自运行时
`ProductionContentRegistry.GetFoodsInStableOrder()`，按 `ProductDefinitionId` 和产品 canonical
physical quantity 分账；不硬编码“六种食品”。

`ProductBatch.Quantity` 是物理存量，`ReservedQuantity` 只是同一批次的可用性约束。市场、税粮、
救济、货运、领取、Owner 变化属于内部转移；消费、仓损、运损和明确加工损失才是 Sink。

### 3.2 原差额所在的洛阳 V70 派生运行时

原失败不是正式 `WorldState.ProductBatches` 的差额，而是洛阳紧凑派生检查点的证据边界。该边界的
物理库存由下列事实组成：

- `LuoyangInventoryBalanceState` 中的食品库存；
- `LuoyangHouseholdConsumptionState.FoodReserveMilliunits` 家户紧凑储备；
- `product.reference.food_equivalent` 兼容食品实物；
- 开局/外部供应/收获 Source，家庭与军队实际消费、加工明确损失 Sink。

该兼容运行时没有为原症状创建正式 `ProductBatch` 或 `InventoryTransaction`，因此不能虚构一个
BatchId 或 TransactionId；主要证据是已有 `LuoyangInventoryFlowState`。

## 4. First Divergence

第一次差异不是 Day 12，也不是最终 Day 365，而是 **Day 0 / initialization**：

| 维度 | 值 |
|---|---|
| Flow | `flow.scenario.opening.household_food.5` |
| Operation | `scenario.opening.household_food_allocation` |
| Product | `product.food.millet_grain` |
| Source Inventory | `inventory.luoyang.184.facility.instance.luoyang.184.taicang.product.food.millet_grain` |
| Destination | `household.compact_reserves` |
| Quantity | 970,000 milliunits |
| Expected World Delta | 0（内部转移） |
| 旧审计 Difference | +970,000 |
| Formal Batch / InventoryTransaction | N/A（V70 紧凑流） |

运行时正确地把 970,000 从太仓库存转入家户紧凑储备；旧测试 Closing 只统计
`runtime.Inventories`，没有统计 `FoodReserveMilliunits`，所以物资仍在世界里，旧审计却认为它消失。

Day 12 出现第二个边界错误：`product.reference.food_equivalent` 首次进入外部供应/家户市场路径，
旧私有食品谓词没有把该兼容产品算作食品。到 Day 365：

```text
遗漏兼容食品 Source       12,735,268
遗漏兼容食品 Closing         -7,101
遗漏家户紧凑 Closing          -3,250
净遗漏                       12,724,917
```

因此最终 `Left - Right = -12,724,917`。这是审计 **少计 Source 与 Closing** 的净结果，不是世界
库存被多扣、少扣、多加或少加。

## 5. Root Cause

根因是两个证据测试各自维护了已经过期的私有食品 ID allow-list，并且 Closing 边界只读取库存表：

- `Luoyang184LivingWorldEvidenceTests` 的旧 `IsClosureFood`；
- `Luoyang184PersonWorkProductionConsumptionClosureV1Tests` 的旧 `IsFood`；
- 两者都遗漏 `product.reference.food_equivalent`；
- 两者都遗漏 `Household.FoodReserveMilliunits`。

负责的生产路径没有重复消费、重复供应或补偿事务。首次失败回归在修复前先错误地预期 Day 12，实际
返回 Day 0，从而把定位前移到开局家户分配；随后才修改审计边界。

既有测试未更早抓住问题的原因：

1. 365 日证据刷新由环境变量显式开启，普通运行只检查旧证据文件存在；
2. `ResourceConservationTests` 位于独立 Unity NUnit fixture，不在纯 Core Runner 的发现列表；
3. 正式食品测试审计的是 `WorldState + ProductBatch + InventoryTransaction`，而原症状位于 V70
   洛阳紧凑兼容证据；
4. 旧证据没有共同的只读审计器，正式与兼容边界各自维护，发生漂移时没有 Unknown/Boundary
   回归直接失败。

## 6. Fix

新增只读 `FormalFoodConservationAuditor` 与 `LuoyangFoodConservationAuditor`：

- 不修改 World、库存、Batch、Transaction、Command 或 WorldTime；
- 正式食品集合来自 Content Registry；
- 输出 World、Product、Owner/Inventory、Batch、Transaction Type、Day；
- 洛阳紧凑流额外输出 Simulation Phase 与旧边界 Difference；
- 全部现有 `InventoryTransactionType` 被归为 Source、Sink、Internal Transfer、Reservation Only、
  Transformation、Owner Change 或 Compatibility Mirror；
- 未分类且改变物理数量的记录产生 `UNKNOWN_PHYSICAL_DELTA` 并令审计失败；
- 检查重复 TransactionId/BatchId、负批次、非法 ReservedQuantity、缺失 Batch 引用、内部转移
  非零净量和 Reservation 物理变化。

365 日证据改为复用唯一审计器，并把家户紧凑储备与兼容食品纳入同一个物理边界。没有修改消费率、
产量、腐败率、救济量、运输损失、人口需求或最终库存，也没有新增 BalanceFix、补偿事务、影子库存。

为区分“文件完整性”和“确定性世界状态”，洛阳派生检查点清单增加
`deterministic_state_sha256`。原 `checkpoint_sha256` 继续覆盖包含性能遥测的 gzip 文件；新摘要仅排除
`Luoyang184LivingWorldRuntimeState.Performance`，其他人物、库存、流、市场、政府、军队和命令状态
全部覆盖。测试证明只改 Performance 时状态摘要不变，改 1 milliunit 库存时摘要必变。

## 7. Formal Auditor Evidence 与性能

正式批次开局世界审计：

```text
Food Products: 6（运行时注册结果）
Inventories: 42
ProductBatches: 246
Food InventoryTransactions: 42
World Difference: 0
Unknown Physical Delta: 0
Internal Transfer Imbalance: 0
Reservation Physical Delta: 0
Duplicate Batch / Transaction: 0 / 0
Negative / Invalid Reserved / Missing Batch Reference: 0 / 0 / 0
Audit Runtime: 8 ms
Managed Memory Delta: 0 bytes（本次测量）
Machine-readable Output: 154,049 bytes
```

审计前后完整 World Snapshot 字符串相同，证明 Auditor 为只读。机器证据：

- `tmp/food-conservation/product-ledger.json`
- `tmp/food-conservation/inventory-ledger.json`
- `tmp/food-conservation/batch-trace.json`
- `tmp/food-conservation/transaction-classification.json`
- `tmp/food-conservation/auditor-performance.json`

## 8. Regression、长期与 Replay Evidence

### 8.1 守恒结果

修复后 365 日：

| 项目 | 数值（milliunits） |
|---|---:|
| Imported | 26,018,643 |
| Harvested | 461,890,000 |
| Consumed | 26,008,292 |
| Processing Loss | 16,170,000 |
| Closing Inventory | 445,727,101 |
| Closing Household Reserve | 3,250 |
| Left | 487,908,643 |
| Right | 487,908,643 |
| Difference | **0** |
| Legacy Boundary Difference | -12,724,917 |
| Unknown Physical Delta | 0 |

保留 `Legacy Boundary Difference` 是为了证明原症状仍可解释，而不是用新数字覆盖历史失败。

独立中间检查点 Day `0/1/7/30/90/180/365` 均为 Difference `0`、Unknown `0`。30 日正式世界
连续运行与 Day 15 Save/Load 后继续到 Day 30 的完整 Snapshot 相同。一年正式食品世界两次独立运行
均通过逐产品、逐批次和事务重放审计。

### 8.2 三次 Replay

洛阳 365 日三次独立进程均得到：

```text
Left / Right: 487,908,643 / 487,908,643
Difference: 0
Unknown: 0
Authoritative State SHA-256:
251c0637170147554a09b58ce448c1adcb16a599003a6b182d9beb576e0067ba
```

三个原始 gzip SHA 不同，因为其中保存运行时性能遥测；它们只作为文件完整性摘要。正式审计 JSON
另做三次独立生成，四类摘要均 3/3 一致：

```text
Product Ledger: d50435445cee188f9e028d2852f92a9fd440d427762d1ea22f2bb2f17d3b5905
Inventory Ledger: 0e801ead2851fcb299044e99ae558825b79067b602f856f6a5a61deeb8e19f1a
Batch Trace: 0d16d14e87a828562bc291a9ee50244e7b6f73b4236abbda6f863e1d9fd84623
Transaction Classification: 6128647e2885b7485a9ffc75080f2a6ec40c294679a4b23cb97ad0228179c116
```

### 8.3 Full Core

固定源码指纹分 12 组完整执行：

```text
Total: 781
Passed: 781
Failed: 0
Slow classification:
- FoodRuntime_FormalWorldIsDeterministicForOneYear: 900 s gate, PASS
- Simulation_SaveResumeMatchesContinuousRun: 900 s gate, PASS
All other tests: 300 s gate
Aggregate: tmp/core-test-groups/food-conservation-final-20260829/aggregate.json
```

完整回归包含 Opening、Harvest、Consumption、正式市场预留/部分成交/取消、税粮、县村赈济、家户
领取与实际进食、营养分配、照护、仓损、民运装卸/运损/到货/容量、分家继承、持久命令幂等和
Save Migration 等既有专项。

## 9. Migration Impact

- `WorldSnapshotSerializer` 仍为 V77；没有新增持久世界字段或迁移；
- 洛阳派生 runtime `FormatVersion` 仍为 6；checkpoint gzip 内容合同不变；
- manifest 仅增加向后兼容的 `deterministic_state_sha256`；
- 不改旧存档库存，不重建历史 Transaction，不制造假食品；
- 旧存档事实保持原样，新审计只读解释当前事实。

## 10. Acceptance Gates

| Gate | 结果 | 证据 |
|---|---|---|
| A 原始问题复现 | PASS | 3/3 同一 `-12,724,917`，首次差异 Day 0 |
| B RCA | PASS | 根因、代码路径、账本路径与遗漏代数全部明确 |
| C Auditor | PASS | World/Product/Inventory/Batch/Transaction；Unknown=0 |
| D 内部转移 | PASS | 市场、税粮、赈济、Freight、Pickup、Owner 变化核心回归通过 |
| E 合法 Sink | PASS | 消费、仓损、运损、加工损失均有正式事务且无重复 Sink |
| F 兼容层 | PASS | 正式权威为 ProductBatch；V70 紧凑边界单独明确，无双写/双计 |
| G Save/Load | PASS | 30 日食品续跑及完整慢测连续/续跑一致 |
| H Replay | PASS | 洛阳状态摘要 3/3；正式 Product/Inventory/Batch/Transaction 摘要 3/3 |
| I 长期生活 | PASS | Day 0/1/7/30/90/180/365 差额均 0，最终负库存/未知流为 0 |
| J 回归 | PASS | Compile PASS、Core 781/781、适用 Unity EditMode 1/1 |

## 11. Unity Validation

本轮只通过 `Tools/Run-UnityTestsSafe.ps1` 启动锁定版本。无图形 EngineSmoke 在120秒内未创建
日志，安全脚本只终止本任务拥有的PID；随后保留图形初始化的批处理EngineSmoke在22.246秒通过，
证明许可、编辑器与测试框架可用，并定位阻塞只发生于当前机器的`-nographics`启动路径。

```text
EngineSmoke: PASS
Summary: tmp/food-conservation/unity-final-graphics/
         unity-EngineSmoke-20260829-125859-473.summary.json
```

一次无筛选完整EditMode运行已启动测试并导入项目资源，但在继续执行全项目无关fixture时触及300秒
硬上限；该结果记录为`blocked/124`，不冒充PASS，也不扩大上限。由于本任务没有Presentation代码
变更，适用回归是Core Runner无法发现的独立食品fixture；它随后生成完整XML并通过：

```text
Filter: Mandate.Tests.Luoyang184PersonWorkProductionConsumptionClosureV1Tests.ResourceConservationTests
Unity EditMode: 1/1 PASS
Duration: 36.297 s
Result: tmp/food-conservation/unity-targeted/
        unity-EditMode-20260829-130502-328.xml
```

PlayMode不适用：本任务未改Presentation、Scene、Prefab或玩家交互。

## 12. Final Conservation Result

核心问题的答案是：原差额绝对值为 **12,724,917 milliunits**；第一次在 **Day 0 / initialization**
出现，涉及 `product.food.millet_grain` 从太仓转入 `household.compact_reserves` 的开局内部流。原症状
没有正式 Batch 或 InventoryTransaction；Day 12 又出现 `product.reference.food_equivalent` 兼容食品
遗漏。实际世界物资没有错误增减，错误是旧证据边界少计 Source 与 Closing。

修复统一了只读审计边界，不改变任何业务数量；Save/Load、365 日长期、三次状态重放、完整核心与
适用Unity目标回归均严格闭合。最终正式结论为：

```text
ACCEPTED
```

下一正式任务可以进入“洛阳外围供应区与城市物流V1”，但必须复用本任务关闭后的正式生产、批次
库存、市场、民运、消费和守恒体系，不得建立第二套Cargo、Inventory或Route权威。
