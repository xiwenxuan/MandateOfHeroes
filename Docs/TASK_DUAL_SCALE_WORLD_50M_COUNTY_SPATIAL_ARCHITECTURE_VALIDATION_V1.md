# 任务书：双尺度统一世界地图、50m县域空间与流式分区架构决策验证 V1

## 1. 任务定位

本任务是地图空间基础架构的 P0 决策验证，不是洛阳正式县域制作。目标是在不复制
世界事实、不升级正式存档的前提下，用可运行的小规模原型判断以下组合能否进入
下一阶段：

```text
StrategicTile2Km
+ PlanningCell50m
+ CountySpatialPartition
+ Facility真实物理空间
+ 四向通行 / Route Portal / Wall Edge
+ 高度与LOS
+ Facility战争状态
```

正式玩家世界继续使用同一份 `WorldState`。Person、Household、Facility、Inventory、
ProductBatch、Army、Organization、Route、时间、权属、工单和损伤不得因视角或加载
等级而复制。

## 2. 范围

### 2.1 必须完成

- 建立 2×2 个 2km StrategicTile 的隔离验证场景。
- 在相同全局坐标基准下建立 80×80、共 6400 个 50m PlanningCell。
- 验证 `1 StrategicTile2Km = 40×40 PlanningCell50m` 的无歧义映射。
- PlanningCell 用紧凑数组和 Chunk 表达；禁止逐格 GameObject/MonoBehaviour。
- Cell 具有北、东、南、西四个相邻连接；四口不是建筑入口。
- Facility 使用同一正式 FacilityId，并独立保存位置、旋转、Footprint、高度、碰撞、
  Entrance 与覆盖格索引；Facility 不等于 Cell。
- Person 在 CountyLocal、InsideFacility、StrategicTransit、ArmyAttached 中互斥。
- Army 在 Strategic 与 CountyMaterialized 中互斥，不复制士兵、装备和库存。
- Route 在战略层与县域层保持同一身份，并支持每县多个稳定 CountyPortal。
- 墙体放在 Cell Edge；Gate、Closed Gate、Breach 直接改变同一通行拓扑。
- 使用 `GroundElevation + StructureHeight + CombatPositionHeightOffset` 计算有效高度，
  以真实遮挡查询验证低位与高台 LOS 差异。
- Facility 的结构耐久、Disabled/Destroyed、守军、Controller、Owner 分离；无人防守
  或投降时允许完整占领，耐久归零不自动转移控制权。
- HOT/WARM/COLD 只改变缓存、查询与表现精度，不推进时间、不重随机、不改变世界结果。
- 输出 Core、Unity EditMode/PlayMode、性能与十张指定截图证据。

### 2.2 明确不做

- 洛阳约 204800 格正式生成及现有 Facility 批量迁移。
- 1182 县 50m 数据批量生成。
- 正式建设 UI、人物完整局部寻路、建筑室内、完整军事 AI 或攻城战。
- 正式存档版本升级、旧存档精确位置随机补全。
- 洛阳最终道路、城墙、人物移动和美术制作。

## 3. 架构合同

### 3.1 坐标与空间

```text
GlobalWorldPosition
↔ StrategicTileCoord
↔ PlanningCellCoord
```

所有坐标基于统一全局原点和固定尺寸；县域不得自建无法与全国格对应的任意原点。
县域是 `WorldState` 的详细空间分区，而不是 `CountyWorldState`。

### 3.2 Cell、Facility 与建筑内部

PlanningCell 负责规划、地形、高程、土地、四向拓扑与空间索引。Facility 是独立物理
对象，可小于一格、占一格或跨多格。人物到达真实 Entrance 后切换为
InsideFacility 并打开相应玩法/管理界面；本任务不建立通用室内空间。

### 3.3 流式分区与调度

- COLD：保留永久事实、到期计划和轻量空间基础，不保留 Unity 表现。
- WARM：加载 Header、主要道路、Portal、主要 Facility 与边界邻域。
- HOT：加载详细 Cell、道路、Footprint、局部查询、Render Chunk 和调试表现。

世界时间只由正式 Scheduler 推进。进入或退出县域只能 Load、Project、Cache、Render，
不能 Catch-up、Advance、Reroll、Regenerate 或创建世界事实。未加载县域继续通过既有
到期事件和世界系统结算。

### 3.4 道路、防御与战争

同一 `WorldRoute` 可以同时提供战略摘要、县域详细线段和多个 CountyPortal。行政县界
不是物理墙。Fortification 统一表达城墙、木栅、土垒、营寨和庄园围墙；墙在线性 Edge，
箭塔、瞭望台和攻城高台仍是 Facility。

## 4. 技术原型验收

### 4.1 Core

至少覆盖：双尺度往返、Tile 边界连续、四向镜像、单格/跨格 Footprint、Entrance、
人物/军队空间互斥、多个 Portal、同 Route 身份、HOT/WARM/COLD 同结果、Load 不修改
世界、Wall/Gate/Breach、高度与 LOS、耐久/守军/控制/所有权、Disabled/Destroyed。

### 4.2 Unity

EditMode 验证 Chunk、映射、Footprint、墙边投影、高度、LOS、Debug Overlay 和零逐格
GameObject。PlayMode 使用正式程序集展示 2×2 StrategicTile、80×80 PlanningCell、
道路、县界、Portal、墙、门、箭塔、普通 Facility、人物、高台和高程，并支持战略/县域
视图及各调试层切换。

### 4.3 视觉证据

必须生成：

1. `01_dual_scale_strategic_tiles.png`
2. `02_planning_cells_50m.png`
3. `03_facility_physical_footprint.png`
4. `04_cell_four_port_topology.png`
5. `05_wall_edge_and_gate.png`
6. `06_county_portal_route.png`
7. `07_height_and_los_low.png`
8. `08_height_and_los_high.png`
9. `09_facility_garrison_control.png`
10. `10_hot_warm_cold_debug.png`

### 4.4 性能

6400 格实测至少记录 Cell 数、内存、Chunk、Build/Load/Unload、Facility 投影、连接与墙
拓扑、LOS P50/P95 和 GC。洛阳 512km²、约 204800 格只做理论估算，不能标记为通过。

## 5. 存档迁移设计边界

本任务保持正式 Schema V79。后续迁移必须区分：可由统一坐标、既有 Cell、道路和确定性
规则推导的事实，与史料不足的 Unknown/Provisional。旧 Facility Footprint、精确位置、
人物 CountyLocal 位置、Army 局部位置、Portal 几何和详细土地权不得随机补全冒充历史事实。

## 6. 决策门

- Decision A：双尺度、50m 和流式结构通过，进入“洛阳50m县域空间原型与Facility迁移
  验证 V1”。
- Decision B：双尺度成立，但 50m 需与 25m/100m 等候选对比后再迁移。
- Decision C：事实唯一性、确定性、流式成本或兼容性失败，保留现有正式结构。

最终选择只能写入实施报告，并以代码、自动测试、Unity 原型、性能数据和用户现场查看为
依据。自动验收完成后状态只能是：

`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`

在用户明确验收前不得标记 `ACCEPTED`。
