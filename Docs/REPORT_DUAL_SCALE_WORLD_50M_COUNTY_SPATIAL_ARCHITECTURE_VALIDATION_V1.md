# 双尺度统一世界地图、50m县域空间与流式分区架构决策验证 V1 实施报告

## 1. 当前结论与决策

本轮已经用正式 `Mandate.Domain`、`Mandate.Simulation` 和 `Mandate.Presentation`
程序集建立隔离的 2×2 StrategicTile / 80×80 PlanningCell 技术原型。原型证明：

```text
2km战略尺度
+ 50m县域详细空间
+ 单一WorldState
+ 县域流式缓存
```

可以在不复制 Person、Facility、Inventory、Army 和 Route，不推进世界时间、不升级
正式存档的前提下共同工作。

最终决策：**Decision A（通过）**。

批准下一阶段进入“洛阳50m县域空间原型与Facility迁移验证 V1”，但本结论只批准
架构方向，不表示洛阳约204800格、2084项Facility迁移、正式建设工具或攻城系统通过。

当前工程状态：

`DECISION_A_CONFIRMED_AND_P1_AUTHORIZED_2026_09_03`

用户已指示继续进入P1；这确认架构决策并授权下一阶段，不把未单独完成的P0视觉现场复核
伪写为另一项验收事实。

## 2. 开工快照

- HEAD：`940c4381da4cbb893c0882fd28e68914397af897`
- 分支：`codex/m23-p4-quality-artisan-growth`
- Unity：`2022.3.62f3c1 (1623fc0bbb97)`
- 正式存档：World Schema V79；本任务没有升级或新增迁移
- 正式全国栅格：`hanworld.square-grid.v1`，2km Cell 合同保持不变
- 开工时工作区已有大量用户修改和前序任务交付；本轮没有还原、提交或推送

## 3. 现有地图架构审计

现有正式世界以 2km Global Cell 承担全国坐标、行政归属、战略路线、人物和军队的
战略位置。它适合天下/州郡县阅读与跨县移动，但不能直接表达普通院落、街道、门墙、
建筑入口或局部战争。

`WORLD_SIMULATION_FOUNDATION.md` 的既有“不得产生 SubCell 世界账”仍然有效：本轮
PlanningCell 是候选的县域空间分区和索引，不是第二份 Person/Facility/Inventory 账，
也尚未写入 V79。正式迁移必须等 P1 真实洛阳规模验证与 P3 兼容设计通过。

## 4. StrategicTile 与 PlanningCell

`StrategicTileCoord` 继续映射现有 2km `CellGridIndex`；`PlanningCellCoord` 使用相同
全局原点、50m 候选格网和候选 Grid Schema 标识。固定关系为：

```text
1 StrategicTile2Km = 40×40 PlanningCell50m = 1600 cells
```

`DualScaleCoordinateProjection` 提供 Global Position、StrategicTile、PlanningCell
以及稳定 CellId 的确定性往返，并校验两个格网原点和倍率严格对齐。相邻战略 Tile
边界无重叠、无空洞。

原型使用两个 80×40 CountySpatialPartition 拼成 80×80，总计6400格和2×2战略 Tile。
每个分区使用 `ushort/byte` 紧凑数组、四向连接字节数组和16格 Chunk；运行时不为
每个 PlanningCell 创建 GameObject 或 MonoBehaviour。

## 5. Cell 四口

`PlanningCellConnectionGrid` 每格保存 North/East/South/West 四个逻辑连接。`SetBetween`
同时写入邻格相反方向，边界外明确为 `OutsidePartition`。通行状态包含普通开放、道路、
桥、门、缺口，以及地形、水、墙、关闭城门和临时阻挡。

四口只描述相邻 Cell 的通路，不表示建筑有四扇门，也不维护第二套视觉通行事实。

## 6. Facility 真实物理空间

`FacilitySpatialPlacement` 直接引用正式 FacilityId 和 CountyId，保存全局中心、厘米级
宽深、90度旋转、高度、碰撞 Profile、真实 Entrance 和覆盖 PlanningCell 投影。原型
包含小于一格的民居、旋转跨格仓库、箭塔、瞭望台和攻城高台。

Facility 始终是 `WorldState.Facilities` 中同一对象；Placement 是空间侧车，不生成
LocalFacility。建筑进入必须先到真实 Entrance，再由空间转换服务把同一 PersonId 从
CountyLocal 切换为 InsideFacility。退出回到入口位置；本轮没有室内空间。

## 7. Person 与 Army 空间唯一性

`PersonSpatialStateV1` 将 CountyLocal、InsideFacility、StrategicTransit、ArmyAttached
建模为互斥联合状态，每种模式只允许自身所需引用，非法组合在构造时拒绝。进入建筑、
离开建筑、开始战略运输和经 Portal 抵达均保持同一正式 PersonState/PersonId。

`ArmySpatialStateV1` 同样强制 Strategic XOR CountyMaterialized。Materialize 只增加
详细位置和指挥表现，不复制 Army、士兵、装备、军粮或伤亡；返回战略层仍是同一 ArmyId。

## 8. 流式加载与 WorldScheduler 边界

`CountySpatialLoadCoordinator` 构建可丢弃的 CacheHandle：

- COLD：零 PlanningCell/Chunk/Portal 驻留，只保留正式世界与空间基础事实；
- WARM：Portal 与主要高建筑摘要；
- HOT：完整 PlanningCell、Chunk 和 Facility Footprint 索引。

每次 SetLevel 前后比较分区确定性 Hash；加载只 Project/Cache，不调用世界时间推进或
随机机制。Core 测试以相同 Seed、WorldState、命令和一天推进比较 HOT/WARM/COLD 的
世界摘要，结果一致。COLD 县的生产、家庭、市场、运输和战争仍由既有正式 Scheduler
及 Due 机制运行；相机、选择和加载等级不是模拟触发器。

## 9. Route 与 CountyPortal

`WorldRouteSpatialStateV1` 保留一个正式 RouteId，战略摘要、两个县域局部路段和 Portal
全部引用该 ID。`CountyPortalSpatialState` 使用稳定 PortalId、两侧 County、边界
PlanningCell、StrategicTile 与通行定义；数据结构和测试允许同一县拥有多个 Portal。

人物通过 Portal 离开时进入同 Route 的 StrategicTransit，抵达另一 Portal 后回到
CountyLocal。Portal 不是瞬间传送，行政县界也不是物理墙。

## 10. Fortification、Gate 与 Breach

`PlanningCellEdge` 将共享边规范化，墙不会占掉任一 Cell。`FortificationSegmentSpatialState`
统一表达墙段与城门，保存稳定 ID、Definition、尺寸、耐久、Owner、Controller、守军和
Gate 状态。墙、关闭门、开放门和破口直接投影为同一 Cell 连接状态；耐久归零形成
`OpenThroughBreach`，不存在独立 VisualPassable。

## 11. Height 与 LOS

有效高度采用：

```text
GroundElevation
+ StructureHeight
+ CombatPositionHeightOffset
```

`SpatialLineOfSightQueryV1` 以全局线段和遮挡体包围盒求交，再比较交点射线高度与遮挡顶高。
同一墙体下，低位观察者视线被阻挡；站上真实攻城高台后能看到墙后有结构高度的目标。高台
改变的是空间高度和 LOS，不是抽象攻击力 Buff。

## 12. Facility 战争状态

Facility 的正式 `ConditionBasisPoints/LifecycleStatus` 与 `FacilityDefenseStateV1` 的
守军分离。结构伤害不会自动杀死守军，守军损失不会损坏结构。守军为零或投降后，可以
完整占领仍完好的 Facility；占领只改 Controller，Owner 不自动转移。

耐久归零先进入 Disabled，不自动转移 Controller；Disabled 可按正式条件维修。Destroyed
是单独终态，普通维修路径拒绝，必须由未来重建/废墟规则处理。破坏、压制守军、招降和
绕过/封锁因此可以成为不同战术意图。

## 13. 性能证据

6400格原型实测：

| 指标 | 结果 |
| --- | ---: |
| PlanningCellCount | 6400 |
| 紧凑权威数组 | 76800 bytes（12 bytes/cell） |
| Core完整场景托管分配 | 123400 bytes（一次实测） |
| 隔离探针完整场景托管分配 | 96336 bytes（一次实测） |
| ChunkCount | 30（两个80×40分区，16格Chunk） |
| Core场景 BuildTime | 23.340 ms（含首次运行/JIT影响的一次实测） |
| Core HOT内部构建 | 0.456 ms / 72 bytes GC |
| Core WARM内部构建 | 0.277 ms / 24 bytes GC |
| COLD内部构建 | 0.000 ms |
| Connection Build P50 / P95 | 0.107 / 0.1176 ms |
| Wall Topology Build P50 / P95 | 0.0145 / 0.0211 ms |
| Facility Footprint P50 / P95 | 0.0016 / 0.0030 ms |
| HOT Load P50 / P95 | 0.1605 / 0.2550 ms |
| WARM Load P50 / P95 | 0.1526 / 0.1696 ms |
| HOT→COLD Unload P50 / P95 | 0.1492 / 0.1581 ms |
| LOS Query P50 / P95 | 0.175 / 0.394 μs |
| PlanningCell GameObject | 0 |
| Unity PlanningCell Render Object | 2 |
| Unity十视图捕获 | 226.063 ms |

详细的端到端 Load/Unload、Facility 投影、连接、墙和 LOS P50/P95 见
`Docs/Evidence/DualScaleWorld50mCountySpatialArchitectureV1/performance-detailed.json`。
Core 与 Unity 原始数据分别见 `performance-core.json` 和 `performance-unity.json`。

### 13.1 洛阳理论估算

按512km²、50m格估算为204800格。只按12 bytes/cell线性外推，基础紧凑数组约
2457600 bytes；320×640格、16格 Chunk 理论为800个 Chunk。隔离探针线性外推
HOT Load P50约5.136 ms、Facility投影P50约0.0512 ms，但二者只按格数比例估算，
未纳入真实2084项Facility、地形、道路、水系、索引和Unity渲染，因此均标记
`executed=false`。

这些数字是 P1 容量规划，不是204800格运行通过。P1必须以真实洛阳形状、2084项
Facility、道路、水系、墙、Portal、空间索引和 Unity Renderer 重新测量。

## 14. 存档迁移草案与风险

本轮不迁移。后续建议：

| 旧事实 | 允许的后续处理 | 不允许 |
| --- | --- | --- |
| Facility | 保留原 FacilityId；Definition 有确定 Footprint 时投影，否则 Unknown/Provisional | 随机造历史占地 |
| Facility 位置 | 现有全局锚点可直接用；只有2km Cell时先保留粗粒度父 Tile | 随机挑 1600 子格 |
| Person Location | 保留 PersonId/Location；经明确 Portal/Entrance 或玩家命令后进入 CountyLocal | 观察县域时重随机位置 |
| Army | 保留同一 ArmyId 与战略 Cell/Route；经边界 Portal 才物化 | 战略和县域各复制一军 |
| Road | 保留 RouteId；有几何时求县界交点生成稳定 Portal | 无几何时伪造史实路线 |
| 2km土地权 | 先保存父 Tile 权益/精度；经勘测、内容或合法命令细分 | 自动把整 Tile 权属灌入所有子格 |

统一格网换算、父 Tile 归属、已有精确锚点和显式 Definition 尺寸可以确定性推导；历史
道路几何、入口、院落边界、局部人物位置和详细土地权若资料不足，必须保留
Unknown/Provisional 并显式补录。P1仍可使用非持久 Feature Flag；只有 P3 才设计顺序迁移、
往返测试和兼容合同。

## 15. 自动测试与视觉证据

| 阶段 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260902-192936-551.out.log` |
| 新增 Core | 29/29 通过 | `tmp/skill-verification/core-tests-20260902-193024-707.out.log` |
| Core 全量 | 928/928 通过 | `tmp/core-test-groups/dualscale50m-v1-20260902/aggregate.json` |
| Unity EditMode | 7/7 通过 | `tmp/unity-validation/unity-EditMode-20260902-193144-128.summary.json` |
| Unity PlayMode | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260902-193209-249.summary.json` |
| PlayableDemo smoke | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260902-203138-350.summary.json` |
| task-scope diff check | 通过 | 全仓检查仍被任务外4个P0Final FBX `.meta` 尾随空格阻塞 |

PlayMode 使用真实 Main Camera 生成十张指定截图，并断言每个视图切换前后正式世界摘要
不变。所有截图已经人工检查，不是空 BackBuffer；低位 LOS 为红色阻挡，高台 LOS 为绿色
可见，网格、Footprint、墙边、门、Portal、道路、人物和加载层级均可辨认。证据索引见：

`Docs/Evidence/DualScaleWorld50mCountySpatialArchitectureV1/README.md`

## 16. 已知限制与下一阶段门禁

1. 当前 50m Grid Schema 明确标记为 candidate，未进入 V79 持久合同。
2. 性能只实测6400格；204800格及800 Chunk均为理论估算。
3. 原型只含5个 Facility、17段防御工程和一条 Route，不代表真实洛阳数据迁移完成。
4. HOT/WARM/COLD 当前是受控缓存合同，不是最终 Addressables/Scene Streaming 实现。
5. LOS 是最小矩形遮挡验证，不是完整弹道、射界或攻城 AI。
6. 没有正式建设 UI、室内、人物完整局部导航或攻城玩法。
7. 全仓 `git diff --check` 的4个任务外 FBX `.meta` 尾随空格属于既存工作区修改，本轮
   没有擅自改写；本任务文件范围检查通过。

Decision A 后严格进入 P1：以 Feature Flag 构建真实洛阳约204800格非持久原型，加载
现有 Facility、UrbanArea、FortifiedBoundary、道路、水系和 Portal，重新做内存、Chunk、
空间索引和 Unity 图形性能验证。P1通过前不做正式存档迁移；P3通过前不冻结永久50m协议。

## 17. 用户现场验收入口

- 场景：`Assets/Scenes/DualScaleSpatialArchitectureValidation.unity`
- 菜单：`Mandate/Validation/Open Dual-Scale 50m Architecture`
- 当前现场状态：Unity 2022.3.62f3c1 已进入 Play Mode，Game View 已选中并停留在县域详细视图
- 默认画面：50m格网、道路、县界、Portal、Wall Edge、Gate、普通Facility、箭塔、攻城高台与LOS
- 可操作项：战略/县域切换、格网、四口、墙门、低位/高台LOS、加载等级循环和Facility选择

该人工验收实例不会由自动清理脚本关闭。用户明确确认前，本任务仍保持
`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`，不得写成`ACCEPTED`。
