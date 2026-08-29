# 70万人经济权威统一、正式库存接管与正常供应标定 V1

## 结论

**ACCEPTED**

70万人洛阳食品经济已经收敛到单一物理权威：`ProductBatchState + InventoryTransactionState`。
开局库存、收获、家庭消费、税粮、市场、军粮采购、民运、损耗和短缺均从同一正式批次账生成；
紧凑家庭与库存字段只保留为单向投影、缓存和索引，不能独立创造、销毁或出售食品。

30日正常基线不再出现技术性普遍缺粮；1年基线中农业、市场、民运和家庭消费持续运行，正式食品
差额为0。任务书 Acceptance Gate A—K 全部通过。Unity唯一环境探针仍在生成启动日志前阻塞，按
任务书第13、96—98节作为环境缺口记录，不替代业务验收。

## 版本与范围

| 项目 | 值 |
|---|---|
| Task | 70万人经济权威统一、正式库存接管与正常供应标定 V1 |
| Branch | `codex/m23-p4-quality-artisan-growth` |
| Baseline Commit | `4343237b5f15e8da1ab1e137f1bc73fa95e0cd77` |
| Final Commit | 本报告随验收提交交付；精确提交号以该提交及最终回复为准 |
| World Save Version | V78 |
| Luoyang Derived Checkpoint | v7（v6顺序迁移） |
| World Rules Version | 1 |
| Formal Economy Contract | v1 |
| Core Production Content | `content.core.production` 11.1.0 |
| Han Food Content | `content.scenario.han_food_extension` 2.1.0 |
| Unity | 2022.3.62f3c1 |
| Population | 700,000 |
| Households | 142,980 |

## 权威架构

Authority Matrix 已覆盖开局库存、种子、收获、家庭/公共/市场/在途食品、消费、短缺、仓储与运输
损耗、税粮、赈济及紧凑汇总，`UNKNOWN = 0`。修复后的合同为：

```text
正式 Domain Operation
→ ProductBatch / InventoryTransaction 物理事实
→ 市场、税粮、公共仓、民运和家庭消费
→ FormalEconomy Revision
→ Compact Projection / Cache / Index
```

双写路径修复前，长期洛阳紧凑库存与正式小型垂直链可以各自生产/消费；修复后所有食品写入集中到
`LuoyangFormalEconomySystem`，旧字段只在正式权威激活前用于一次性v6→v7迁移或非食品边界。
Formal→Compact 可以增量刷新或确定性重建；Compact→Formal 的普通运行反向写入为0。

批次按产品、容器、来源窗口及影响玩法的质量事实合并；没有为70万人逐人、逐餐创建重量级批次。

## 正常供应基线

| 指标 | 30日 | 1年（365日） |
|---|---:|---:|
| Opening Food | 133,187,537,700 | 133,187,537,700 |
| Demand | 19,026,791,100 | 231,492,625,050 |
| Local Agriculture | 0 | 129,170,460,000 |
| External Production | 6,660,000,000 | 43,937,365,000 |
| Total Production | 6,660,000,000 | 173,107,825,000 |
| Consumption | 19,026,791,100 | 228,870,983,496 |
| Shortage | 0 | 2,621,641,554 |
| Shortfall Households | 0（0%） | 76,256（累计） |
| Closing Food | 120,819,691,306 | 56,456,320,382 |
| Closing Days of Supply | 190 | 89 |
| Food Difference | 0 | 0 |
| Market Volume | 662,302,839 | 214,712,943,317 |
| Freight Dispatched | 2,000,000 | 26,000,000 |
| Freight Delivered | 1,964,706 | 25,048,643 |
| Transport Loss | 35,294 | 458,822 |
| Tax Transfer | 1,211,016,797 | 7,838,058,500 |
| Batch Count | 79 | 6,184 |
| Transaction Count | 142 | 7,222 |
| Simulation Runtime | 4,677 ms | 55,511 ms |
| Peak Managed Memory | 198,983,296 bytes | 220,594,960 bytes |

30日依靠版本化210日开局储备和真实外部供应跨越未收获窗口，没有把作物加速到30日内成熟；135条
农业记录使用110日确定性错峰，旧30日调度下限405仍保持。1年消费满足率为98.86%，存在可解释的
局部/累计缺粮，但不是技术性全户崩溃。

标定严格按 Authority→Accounting→Demand→Opening Stock→Agriculture→Storage→Transport→Market
顺序进行。Authority-only控制组和市场接入失败候选均保留在 `calibration-candidates.json`，没有只保留
成功参数。

## 一致性、存档和性能

- Projection：Day 0/1/7/15/30/365最大差额0；重建前后正式权威哈希不变。
- Save/Load：洛阳派生Checkpoint v6顺序形式化为v7；v7保存正式权威并重建投影，陈旧投影不能覆盖批次。
- Replay：相同Seed三次正式权威哈希和投影哈希3/3一致；性能遥测不参与世界事实哈希。
- Performance：30日独立有界探针8,893 ms；批次79、事务142、投影重建209 ms，均低于预声明上限；
  1年批次6,184、事务7,222，无OOM或灾难性热点，结论 `ACCEPTABLE FOR V1`。

## Acceptance Gate

| Gate | 结果 | 证据摘要 |
|---|---|---|
| A Authority Audit | PASS | 所有食品物理Writer/Reader已分类，UNKNOWN=0 |
| B Formal Physical Authority | PASS | 收获、消费、市场、民运、公共仓均写正式批次/事务 |
| C Compact Layer | PASS | 仅Projection/Cache/Index，可重建且不能独立改实物 |
| D No Double Write | PASS | Harvest/Consumption/Tax/Market/Freight双写计数均0 |
| E 70万人正常30日 | PASS | 完成、差额0、短缺户0、无批次/库存损坏或需求风暴 |
| F 正常1年 | PASS | 农业/市场/民运/消费持续，差额0，无技术性饥荒崩溃 |
| G Projection | PASS | 各检查点差额0 |
| H Save/Load | PASS | 连续与续跑权威/投影一致，v6→v7迁移明确 |
| I Replay | PASS | 3/3一致 |
| J Performance | PASS / ACCEPTABLE FOR V1 | 30日与1年时间/内存已记录，增长有界 |
| K Existing Regression | PASS | 上一22项、AI 5项、编译和完整836项均通过；新增回归0 |

## 验证

| 阶段 | 结果 |
|---|---|
| Full Compile | PASS；仅NuGet漏洞源不可达警告，不影响编译产物 |
| Authority Families | 14/14 PASS |
| Previous Integrated Scenarios | 22/22 PASS |
| Affected AI | 5/5 PASS |
| Full Core | 836/836 PASS，冻结指纹 `3FC748…50BCD` |
| Unity EditMode Probe | BLOCKED/125；60秒无启动日志，只终止本次PID 61672，未重试 |
| `git diff --check` | PASS |
| Introduced Regression | 0 |

## 边界与下一步

本任务只关闭70万人双经济Authority与正常供应Baseline。上一份
`LUOYANG_SUPPLY_MARKET_HOUSEHOLD_INTEGRATED_STRESS_AND_PLAYABILITY_V1_ACCEPTANCE_REPORT.md` 仍保持
`NOT ACCEPTED`；不得由本结论静默改写。

下一任务固定回到“洛阳城市供给—市场—家庭消费联合压力与可玩性验收 V1”做 Final Remediation /
Re-acceptance：重跑Gate、Road、Production、Carrier、Storage、政府采购/赈济、Demand Storm、玩家
Supply Card、玩家商旅介入、Save/Load、Replay与Unity证据，不再重建经济底层。

## Evidence Index

权威矩阵、Writer/Reader与双写审计、Bridge/Projection合同、需求/库存/农业/运力/市场审计、全部标定
候选、30日/1年基线、存档/重放/性能及Unity环境探针统一位于：
`Docs/Evidence/LuoyangIntegratedEconomyV1/`。
