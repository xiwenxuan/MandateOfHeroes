# 洛阳外围供应区、正式物流与城市供给闭环 V1：验收报告

## 1. 交付身份

```text
Task: TASK_LUOYANG_OUTER_SUPPLY_FORMAL_LOGISTICS_AND_CITY_SUPPLY_LOOP_V1
Branch: codex/m23-p4-quality-artisan-growth
Baseline HEAD: ab65b72f0f3442ad48331ac6c3b062780c5cd8ca
Commit: 本任务提交见 Git 历史
Unity Version: 2022.3.62f3c1
Save Version: V78
World Rules Version: 1
Content Version: content.core.production 11.0.0
Formal Acceptance: NOT ACCEPTED
```

本轮已经实现并验证正式供应物流垂直切片，但人口物化、正式长期世界接线和 Unity 门禁没有全部闭合，
因此不能依据局部核心测试写成 `ACCEPTED`。

## 2. 正式实现

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

## 3. 数据与内容审计

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

## 4. 验证矩阵

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

## 5. 未通过 Gate

1. **外围世界 Gate B 未闭合**：400,000 实际人物距离 700,000 包含式目标仍差 300,000，现有外围
   设施住宅容量也不足以承载该差额；清单目标不等于世界事实。
2. **正式长期闭环未闭合**：本轮垂直切片证明底座可行，但受保护紧凑包的 135 条农业记录、城市
   400,000 人需求与正式 V78 世界还没有全部进入同一长期调度闭环。
3. **内容桥未闭合**：豆、黍、粟的旧包 ID 尚缺正式内容定义。
4. **Unity Gate K 未闭合**：当前宿主两次无法启动测试框架，未取得 EditMode、PlayMode、截图、
   Loaded Objects、Frame Time、GC 或 Streaming 图形证据。
5. **性能 Gate L 仅部分证实**：没有新增每帧 700,000 Person 或全国 Cell 扫描，但完整 Unity 指标
   因环境阻塞不可判为 PASS。

## 6. 结论

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
