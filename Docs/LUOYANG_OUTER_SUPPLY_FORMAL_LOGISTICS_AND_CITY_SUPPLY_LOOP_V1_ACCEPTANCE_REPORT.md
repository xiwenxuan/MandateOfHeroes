# 洛阳外围供应区、正式物流与城市供给闭环 V1：验收报告

## 1. 交付身份

```text
Task: TASK_LUOYANG_OUTER_SUPPLY_FORMAL_LOGISTICS_AND_CITY_SUPPLY_LOOP_V1
Branch: codex/m23-p4-quality-artisan-growth
First Attempt Baseline: ab65b72f0f3442ad48331ac6c3b062780c5cd8ca
Remediation Baseline: 748a15b27df50c84be0354ad4b1bca7986f8f873
Remediation Final Commit: 本报告提交见 Git 历史
Unity Version: 2022.3.62f3c1
Save Version: V78
World Rules Version: 1
Content Version: content.core.production 11.1.0 / content.scenario.han_food_extension 2.1.0
Formal Acceptance: ACCEPTED
```

本报告保留首次验收的 `NOT ACCEPTED` 事实，并在第7节以后记录 remediation 与最终收口。
最终 A—L Gate 已全部通过，正式结论更新为 `ACCEPTED`。

## 2. First Attempt：正式实现（历史）

- V78 在既有 `CivilianFreightState` 上保存 CellRoute 计划摘要、移动能力、路段、进度、等待原因与
  正式门桥对象；同一 `JourneyState`、真实 Person 和 Mobile Container 继续承担移动与货物权威。
- `TravelSystem` 每个世界 Segment 按路线成本推进，并在跨越前读取 Road、Gate、Bridge 当前状态；
  阻断时等待或按能力重算，开放后从保存进度继续。
- 统一 `AgricultureProductionSystem` 增加版本化 8,000 basis-points 早收门槛。80% 成熟产量为
  满产的 70%，80% 到 100% 间线性恢复至 100%；规则快照随工单进入存档。
- 木材不走食品特例：真实 ResourceBody 经既有上游采集工单生成正式木料批次，再走同一市场、库存、
  民运和 CellRoute。
- `LuoyangSupplyCatchment` 只是对受保护洛阳包的只读选择；`LuoyangCitySupplyProjection` 只汇总真实
  批次、订单、在途货运、家庭需求与短缺，不建立城市库存或需求权威。
- Unity 新增只读货运占位标记，等待/在途/到货只反映世界 Freight 状态，不移动批次或结算库存。
- V77→V78 顺序迁移只初始化农业规则快照和空 CellRoute 字段，不虚构历史运输或收获。

## 3. First Attempt：数据与内容审计（历史）

| 项目 | 当前事实 |
|---|---:|
| Luoyang Population | 400,000 |
| Outer Supply Population | 130,000 |
| Inclusive Population Target | 700,000 |
| Unmaterialized Population Gap | **300,000** |
| Supply Catchment Cell Count | 869 |
| Settlement Count | 33 |
| Farm/Agriculture Unit Count | 135 |
| Storage Count | 22 |
| Selected Facility Count | 854 |
| Selected Road Facility Count | 267 |
| Whole Luoyang Gate-type Facility Count | 18 |
| Whole Luoyang Bridge Count | 2 |
| Source Food Product IDs | 4 |
| Formal Food Bridge Completed | 1（小麦） |
| Unresolved Source Food IDs | 3（豆、黍、粟） |
| Wood Product Count | 1 |

关键 Cell/Facility/Settlement/Owner/Hash 引用错误为 0；所有 869 个选择 Cell 均存在于同一 5,980 Cell
通行计划。`product.food.wheat_grain` 通过带稳定迁移 ID 的显式映射连接
`product.wheat_grain`；其他缺失内容 ID 保留并报告。

## 4. First Attempt：验证矩阵（历史）

| 验收项 | 结果 | 证据/说明 |
|---|---|---|
| Full Compile | PASS | MSBuild 全工程通过 |
| Normal Supply Test | PASS | 食品 Harvest→Batch→Market→Freight→Gate→Storage→Consumption |
| 80% Harvest | PASS | `<80%`拒绝，`80%/中间/100%`确定性产量 |
| Food Vertical Slice | PASS | 同一正式世界账，Difference 0 |
| Wood Vertical Slice | PASS | ResourceBody→Batch→同一 Freight/CellRoute/Gate |
| Gate Interruption | PASS | 等待、城市短缺、重开、恢复，只到货一次 |
| Bridge / Road Block | PASS | 桥关闭等待；驮运合法越野，车辆拒绝非法越野 |
| Destination Full | PASS | 等待、Save/Load、扩容后一次性完成 |
| Origin Insufficient | PASS | 原子拒绝，快照不变 |
| Carrier Unavailable | PASS | 原子拒绝，快照不变 |
| Food Conservation | PASS | 全链 Difference 0 |
| Wood Conservation | PASS | 资源余量与产品批次总量守恒 |
| Save / Load | PASS | 在途、Gate 等待、Destination 等待、V78 往返 |
| Replay | PASS | Gate 中断场景 3/3 完整快照一致 |
| Core Regression | PASS | 固定源码指纹，12组 793/793，失败0 |
| Unity EditMode | BLOCKED | 启动前 blocked/125，无日志/XML |
| Unity PlayMode | BLOCKED | 同一 Unity 启动环境门禁，未伪报 PASS |
| Performance | PARTIAL | 查询+通行初始化 2,503 ms；Unity 图形指标未取得 |
| Introduced Regression | Core 0；Unity 未知 | 不以环境阻塞冒充代码失败或成功 |

## 5. First Attempt：未通过 Gate（历史）

1. **外围世界 Gate B 未闭合**：400,000 实际人物距离 700,000 包含式目标仍差 300,000，现有外围
   设施住宅容量也不足以承载该差额；清单目标不等于世界事实。
2. **正式长期闭环未闭合**：本轮垂直切片证明底座可行，但受保护紧凑包的 135 条农业记录、城市
   400,000 人需求与正式 V78 世界还没有全部进入同一长期调度闭环。
3. **内容桥未闭合**：豆、黍、粟的旧包 ID 尚缺正式内容定义。
4. **Unity Gate K 未闭合**：当前宿主两次无法启动测试框架，未取得 EditMode、PlayMode、截图、
   Loaded Objects、Frame Time、GC 或 Streaming 图形证据。
5. **性能 Gate L 仅部分证实**：没有新增每帧 700,000 Person 或全国 Cell 扫描，但完整 Unity 指标
   因环境阻塞不可判为 PASS。

## 6. First Attempt 结论（历史）

```text
NOT ACCEPTED
```

本提交可以作为正式物流集成底座继续开发，不能作为“洛阳约 70 万人口完整外围供给世界已经建成”的
证明。下一步应先物化缺失 300,000 人及家庭/设施承载、完成旧内容 ID 正式定义和长期世界接线，并在
可启动 Unity 的环境补齐 EditMode/PlayMode 与图形性能证据；全部 Gate 通过后再进入城市供给—市场—
家庭消费联合压力与可玩性验收 V1。

详细能力分类见
[`Evidence/LuoyangOuterSupplyV1/existing-capability-audit.md`](Evidence/LuoyangOuterSupplyV1/existing-capability-audit.md)，
验证摘要见
[`Evidence/LuoyangOuterSupplyV1/verification-summary.md`](Evidence/LuoyangOuterSupplyV1/verification-summary.md)。

## 7. Remediation 实现与审计

- 由正式基线重新计算 `700,000 - 400,000 = 300,000`，生成确定性的增量紧凑人口包；
  不是修改汇总计数。新增300,000个永久Person、62,081户、695个住宅Facility，分布到全部33个外围
  Settlement；每个新增Facility占唯一Cell。
- 正式世界总人口为700,000、总Household为142,980、总Facility为2,779。外围人口为430,000、
  外围Household为88,988；外围住宅容量451,487，承载430,000名外围居民，容量差额为+21,487。
- 新增人口中劳动年龄人物217,802名，初始为可进入正式劳动体系的未分配劳动力；没有强制全部进入农业，
  也没有生成Person GameObject或NavMeshAgent。
- 三个旧食品稳定ID直接获得正式Product和Food Definition：`product.food.bean`（豆）、
  `product.food.broomcorn_grain`（黍）、`product.food.millet_grain`（粟）。三者保持原ID、无别名、
  无静默改指，`OpeningShareBasisPoints=0`，因此加载定义不会生成库存。
- 135条外围农业记录进入持久化到期索引：每条作物保存`NextDueDay`与`ScheduleRevision`，运行时只分派
  到期项，不在每Tick扫描全部农田；80%早收与产量惩罚继续复用统一农业规则。
- 正式住宅容量和Settlement关系进入生活世界运行时；住房压力读取`ResidentCapacity`，不再把最佳工人数
  当作住宅容量。就业、住房压力乘法使用64位中间值，消除70万人规模下的整数溢出。
- Unity货运标记把`MaterialPropertyBlock`创建移入`Awake`，避免MonoBehaviour构造阶段调用Unity API。

人口增量包重复生成两次，受保护的6个输出文件逐字节一致；`persons.bin`、`households.bin`及JSON文件
均由manifest SHA-256校验。详细事实见本报告同目录的remediation证据集。

## 8. Final Acceptance Matrix

| Gate | 最终结果 | 本轮正式证据 |
|---|---|---|
| A 上一成果不回归 | PASS | V78、80%早收、Food/Wood Freight、CellRoute、Gate等待/恢复、仓满、原点不足、Carrier不可用、Road Block、守恒均在799项完整核心内通过 |
| B 人口目标 | PASS | 700,000/700,000，Gap 0；142,980户；33外围Settlement；2,779 Facility；无孤儿引用或重复Cell |
| C 三食品定义 | PASS | 豆/黍/粟 3/3 正式解析；无静默映射、无重复ID、定义加载不增库存 |
| D 135条农业 | PASS | 135/135调度；Invalid/Missing Definition/Missing Cell/Missing Facility/Duplicate Schedule均为0 |
| E 长期农业 | PASS | 30日与1年通过；135个农场均有长期产出；Save/Load与3/3 Replay通过 |
| F 扩人口食品链 | PASS | 日需求634,226,370 milliunits；Food生产、仓储、运输、Gate、消费和短缺行为通过；Difference 0 |
| G 木材回归 | PASS | Wood ResourceBody→Batch→Freight→CellRoute→Gate→Inventory通过 |
| H Save/Load | PASS | V78人口/家庭/容量、农业到期索引、Gate等待、Freight与Inventory往返通过 |
| I Replay | PASS | 农业状态哈希3/3一致；Gate中断完整快照3/3一致 |
| J Core | PASS | 最终源码指纹`C584F7855DD8A8B5B8F39DBC6E88F113A19471BDED49AE413E92C773BEB11C79`；799/799，失败0 |
| K Unity | PASS | Project Load Smoke通过；8项适用EditMode通过；3项有图形PlayMode通过 |
| L Performance | ACCEPTABLE FOR V1 | 70万人Unity初始化5,919ms；新增2个GameObject；分配增量1,817,461 bytes；20帧平均6.698ms；30日推进6,266ms；一年66,595ms |

## 9. Final Metrics

```text
Branch: codex/m23-p4-quality-artisan-growth
Remediation Baseline Commit: 748a15b27df50c84be0354ad4b1bca7986f8f873
Remediation Final Commit: 本报告提交见 Git 历史
Save Version: V78
World Rules Version: 1
Content Version: core 11.1.0 / Han Food 2.1.0
Unity Version: 2022.3.62f3c1
Target Inclusive Population: 700000
Actual Inclusive Population: 700000
Added Permanent Persons: 300000
Household Count: 142980
Added Households: 62081
Settlement Count: 33 outer
Facility Count: 2779 world / 1549 selected outer
Residence Capacity: 451487 outer / 430000 assigned
Legacy Food IDs: product.food.bean; product.food.broomcorn_grain; product.food.millet_grain
Agriculture Record Count: 135
Agriculture Scheduled Count: 135
30-Day Agriculture: PASS; 405 dispatches; Difference 0
1-Year Agriculture: PASS; 4499 dispatches; 135 harvested farms; Difference 0
Food Production / Consumption / Conservation: PASS / PASS / Difference 0
Gate Interruption: PASS
Wood Regression: PASS
Save Load: PASS
Replay: PASS 3/3
Core: PASS 799/799
Unity Project Load: PASS
Unity EditMode: PASS
Unity PlayMode: PASS
Performance: ACCEPTABLE FOR V1
Introduced Regression: 0
Final Result: ACCEPTED
```

## 10. 最终结论

```text
ACCEPTED
```

首次验收的四类缺口已经逐项关闭：人口与家庭是真实永久事实，旧食品ID进入正式内容注册表，135条农业
记录进入V78长期到期调度，Unity EditMode/PlayMode及图形性能均取得本轮结果。可以按总纲进入下一正式
任务“洛阳城市供给—市场—家庭消费联合压力与可玩性验收 V1”；本结论不表示下一任务已经实现。
