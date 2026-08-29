# 洛阳城市供给—市场—家庭消费联合压力与可玩性验收 V1

## 结论

**NOT ACCEPTED**

本轮已经完成编码前能力审计、现有正式链复用、只读供给投影扩展、正式市场报价桥、
玩家货运薄适配和22项核心场景验证。结果证明两个系统各自能运行，但没有证明它们是
同一个经济世界：70万人长期运行仍由 `Luoyang184LivingWorldRuntimeState` 的紧凑库存、
市场、供给单和运输记录结算；正式 `WorldState` 经济则由 `ProductBatchState`、
`FormalMarketOrderState`、`CivilianFreightState`、正式家庭消费和公共粮命令结算。

不能把“70万人紧凑运行通过”和“小型正式垂直切片通过”并排后写成联合通过。
此外，无中断的70万人第30日结果为142,980/142,980户全部缺粮，累计缺口
19,023,923,801 milliunits；普通玩家供给卡和真实Unity玩家介入也尚未建立。

依据任务书第124节，任一适用 Gate 未通过时最终结果只能是 `NOT ACCEPTED`。

## 版本与范围

| 项目 | 值 |
|---|---|
| Task | 洛阳城市供给—市场—家庭消费联合压力与可玩性验收 V1 |
| Branch | `codex/m23-p4-quality-artisan-growth` |
| Baseline Commit | `4343237b5f15e8da1ab1e137f1bc73fa95e0cd77` |
| Task Commit | 未创建；最终 Gate 未全部通过 |
| Push | 未执行；最终 Gate 未全部通过 |
| Save Version | V78 |
| World Rules Version | 1 |
| Core Production Content | `content.core.production` 11.1.0 |
| Han Food Content | `content.scenario.han_food_extension` 2.1.0 |
| Unity | 2022.3.62f3c1 |
| Luoyang Living Population | 700,000 |
| Households | 142,980 |
| Facilities | 2,779（前置正式报告） |
| Outer Settlements | 33（前置正式报告） |
| Agriculture Records | 135（前置正式报告） |

## 本轮实现

1. 扩展既有只读 `LuoyangCitySupplyProjection`，增加库存所有权分类、在途/阻断/延误量、
   家庭与人物缺粮、主要食品价格、采购、承运、活动货运和来源计数。投影不写库存、价格、
   货运或家庭事实。
2. 在既有 `FormalCountyMarketSystem` 上增加只读 `FormalMarketQuote`。它读取正式批次、
   活动订单、近期成交、家庭缺口和在途阻断；价格权威仍是正式成交写入的
   `FormalMarketPriceState`，测试没有直接修改价格。
3. 移除家庭、商人和政府候选动作中的硬编码价格100，改为读取正式报价；动作仍经既有
   动作校验、命令和结算入口。
4. 增加 `LuoyangPlayerSupplyInterventionService` 薄适配，只允许当前玩家Person作为承运人，
   并调用既有 `CivilianFreightSystem.Dispatch`；它不拥有库存、价格、路线或收货状态。
5. 增加22项核心测试，覆盖投影、价格、Gate关闭/恢复、道路改道、早收减产、承运不足、
   仓容、政府采购、赈济、家庭差异、需求风暴、玩家货运、食品/现金守恒、存读、3/3重放、
   一年小型正式世界和拆分性能探针。

这些修改只闭合了正式小型链的局部能力，没有安全完成70万人紧凑经济向统一正式经济的迁移。

## 关键证据

### 70万人正常30日

| 指标 | 结果 |
|---|---:|
| Persons | 700,000 |
| Households | 142,980 |
| Closing Food Stock | 13,721 milliunits |
| Cumulative Food Consumed | 2,867,299 milliunits |
| Cumulative Food Shortage | 19,023,923,801 milliunits |
| Shortfall Households | 142,980 |
| Compact Conservation Difference | 0 |

守恒为0只证明账相等，不证明正常供应可玩。所有家庭缺粮且缺少任务书要求的逐日联合价格、
生产、消费、市场、采购、货运和 DaysOfSupply 序列，因此30日 Normal Supply 不通过。

### 小型正式链

| 场景 | 证据 |
|---|---|
| Price | 正常97，压力99，正式成交99，恢复98 |
| Gate | 广阳门阻断1单/12单位；重开后送达12、损耗0、送达账1条 |
| Road | 驮畜正式改道成功，Route Revision 1，货物未复制 |
| Production | 80%早收产量1，完全成熟产量2，产出批次有工单来源 |
| Carrier | 需求1、报价0、运单0，无补偿承运人 |
| Storage | 发运1000、送达994、自然损耗6、恢复仓容后剩余0 |
| Government | 采购5、资金10、内部现金净额0；赈济送达2并消费2 |
| Household | 39个领取家庭、4种需求值、195名受影响人物 |
| Player service | 正式承运20、单价2、运费100、现金净额0、食品差额0 |
| Save/Load | 在途20→20、活动运单1→1、价格摘要一致 |
| Replay | 3/3字节一致 |
| Formal one year | 200人夹具推进360日后207人，食品差额0 |

上述结果是正式系统可复用的正证据，但测试夹具不是70万人洛阳经济，不可扩大解释。

### 拆分性能探针

| 指标 | 结果 |
|---|---:|
| Compact 700k init + one day | 2,671 ms |
| Compact initialization | 2,207 ms |
| Compact one day | 211 ms |
| Compact peak managed memory | 194,331,072 bytes |
| Small formal projection ×100 | 6 ms |
| Unified 700k formal economy | 未测/不存在 |
| Unity Frame / GC | 无本轮正式玩家场景证据 |

这些数字证明两个独立侧面各自有界，不证明联合运行性能。

## Acceptance Gate

| Gate | 结果 | 说明 |
|---|---|---|
| A 系统复用 | **FAIL** | 新增部分均为既有系统扩展或薄适配，但实际70万人食品、市场与运输仍是紧凑运行时的平行权威。 |
| B Normal Supply | **FAIL** | 30日所有142,980户缺粮；无逐日联合序列；一年仅为207人正式夹具。 |
| C Market Feedback | **FAIL** | 小型正式报价97→99→98可解释，但未接入实际70万人市场时间序列。 |
| D Gate Shock | **FAIL** | 正式Gate/CellRoute小型链通过，未影响实际洛阳库存、价格和家庭缺口；多Gate网络选择未证实。 |
| E Production Shock | **FAIL** | 正式早收减产通过，未传导到实际城市联合经济。 |
| F Carrier Shortage | **FAIL** | 正式无幻影运力通过，未验证实际700k运力积压和恢复。 |
| G Storage Bottleneck | **FAIL** | 正式仓满等待和幂等收货通过，未验证实际洛阳仓网。 |
| H Government | **FAIL** | 正式预算/卖方批次/赈济通过，未接入实际70万人公共供给。 |
| I Households | **FAIL** | 正式夹具保留家庭差异；实际正常基线所有家庭缺粮且使用另一库存权威。 |
| J Player Impact | **FAIL** | 服务级玩家承运通过，但无普通玩家UI，且未改变实际70万人世界。 |
| K Conservation | **FAIL** | 两套账各自差额0；没有一个联合权威账可审计。 |
| L Save / Load | **FAIL** | 小型正式投影往返通过；未做任务要求的联合连续运行=恢复运行全状态比较。 |
| M Replay | **FAIL** | 小型正式切片3/3；未做实际联合世界3/3。 |
| N Performance | **FAIL** | 拆分性能有界；缺统一700k正式经济、年度分项、GC与Unity Frame证据。 |
| O Player Visibility | **FAIL** | 现有开发观察台和旧聚合界面不能冒充读取正式投影的普通玩家供给卡。 |

## 验证结果

| 阶段 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | `compile-20260829-194050-517.out.log` |
| 正式功能核心测试 | 20/20 PASS | `core-tests-20260829-193616-199.out.log` |
| 规模/性能核心测试 | 2/2 PASS（仅证明拆分事实） | `core-tests-20260829-194056-635.out.log` |
| 受影响既有AI回归 | 5/5 PASS | 家庭、商人、政府采购精确筛选 |
| Unity EditMode | BLOCKED（环境门禁） | PID 50168 在45秒内未创建启动日志；安全脚本只终止本次进程。`unity-EditMode-20260829-194529-727.summary.json` |
| Unity EngineSmoke | BLOCKED（环境门禁） | 无项目启动同样无日志，证明阻塞发生在项目和测试框架之前。`unity-EngineSmoke-20260829-194701-596.summary.json` |
| Unity PlayMode | NOT RUN | EngineSmoke 已复现相同环境阻塞，不重复启动。 |
| `git diff --check` | PASS（阶段性） | 验证脚本输出 |

核心测试绿色表示局部实现没有违反已断言的不变量，不改变上表联合 Acceptance 失败结论。

## 阻塞项与下一任务

下一任务必须是“洛阳70万人紧凑经济向正式世界经济的权威统一/迁移设计”，而不是继续增加
第二套汇总或UI。至少需要先确定并实现：

1. `Luoyang184LivingWorldRuntimeState` 的库存、家庭口粮、市场交易、供给单与运输记录如何映射或
   迁移到正式批次、容器、订单、运单和家庭消费合同；不得保留两个可写真相。
2. 70万人永久Person/Household的索引式访问、到期调度和活动集合，避免把全部Person内联进
   `WorldState` 或每帧扫描。
3. V78存档是否需要升版、分区检查点如何顺序迁移、旧紧凑存档如何保留原引用并往返验证。
4. 先修复正常供应标定：所有家庭第30日缺粮不是可接受默认体验；修复必须来自生产、库存、
   运力、路线、市场和消费参数，不得直接补城市粮食标量。
5. 统一权威闭合后再接普通玩家 Supply Card 和商旅交互，随后重跑本报告全部 Gate。

## Evidence Index

- `Docs/Evidence/LuoyangIntegratedEconomyV1/existing-capability-audit.md`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/baseline-30d.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/baseline-1y.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/gate-shock.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/road-shock.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/production-shock.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/carrier-shortage.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/storage-bottleneck.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/government-relief.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/household-distribution.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/market-price-series.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/demand-storm-audit.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/player-intervention.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/food-conservation.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/save-load.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/replay.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/performance.json`
- `Docs/Evidence/LuoyangIntegratedEconomyV1/screenshots/README.md`

本轮不创建提交、不推送，因为任务书只授权在全部适用 Gate 通过后执行这些外部状态变更。
