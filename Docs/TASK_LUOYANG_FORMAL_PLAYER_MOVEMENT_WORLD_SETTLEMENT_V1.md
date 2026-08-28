# TASK：洛阳正式玩家人物移动与世界结算 V1

## 1. 任务定位

本任务把 `TASK_LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1.md` 中只读、会话态的演示人物步行，
升级为正式玩家 `Person` 的可校验、可结算、可存档、可确定性重放的世界行动。现有演示人物仍只用于
未绑定正式世界的审图与表现测试；它不再代表玩家世界事实。

当前合同 ID 为
`mandate.luoyang.formal-player-movement-world-settlement.v1`，存档版本由 V75 升级到 V76。

## 2. 范围与边界

### 2.1 本轮实现

- 使用现有 `WorldState.PlayerPersonId` 作为唯一受控人物持久身份，并以 `PlayerSession` 提供
  `ControlledPersonId`、`ControlledPerson` 与 `CanAct` 查询，不增加第二份玩家人物记录。
- 在正式 `PersonState` 上保存体力、洛阳本地 Cell 和 Facility；Settlement 继续复用既有
  `LocationId`，口粮继续复用既有 `Provisions`。
- 把 379 个正式导航位置、402 条道路边的运行状态和进行中的移动行动写入 V76 世界快照。
- 点击经目标解析后生成持久命令，由 Simulation 校验、寻路、计费、推进既有世界时间并提交位置。
- 路线读取现有 V75 门桥事实和 V76 道路状态，不维护独立的表现层通行布尔值。
- 支持在 Segment 边界保存、读档和继续；中途状态变化在下一 Segment 提交前重新校验。
- Unity 只在世界结算成功后播放已提交路线，未绑定正式世界时保留旧演示行为。

### 2.2 明确不实现

- 不增加或修改 54 个洛阳建筑资产，不建立第二套 Person、Location、WorldTime 或 Inventory。
- 不做全城人物尺度 NavMesh、室内寻路、NPC 群体/RVO、正式角色模型和动画。
- 不做高分辨率洛阳 DEM、外围供应区、完整攻城、逐帧移动存档或每帧动态重算。
- V1 路线失败或下一 Segment 失效时中断，不自动执行新的替代路线命令。

## 3. 权威数据模型

### 3.1 玩家与人物

`WorldState.PlayerPersonId` 仍是存档中的唯一玩家控制引用。`PlayerSession` 是只读领域包装，解析已有
`PersonState`，并拒绝人物不存在、已死亡、非 Active 或已有正式 Journey 的行动。

人物本地位置由以下事实共同表达：

```text
PersonState.LocationId          Settlement
PersonState.CurrentCellId64     Global Cell
PersonState.CurrentFacilityId   Local navigation / Facility anchor
```

`Transform.position` 不是世界位置事实。

### 3.2 路线与行动

V76 新增：

- `LuoyangLocalNavigationLocationState`：Facility、Cell、Settlement 与道路网格坐标的稳定映射。
- `LuoyangRoadOperationalSegmentState`：402 条边的 `open/blocked/destroyed` 状态、Revision 与命令事件来源。
- `LuoyangFormalPlayerMovementState`：请求命令、人物、起终点、固定路线快照、分段成本、当前 Segment、
  未结算分钟、累计消耗及完成/中断状态。

路线快照保存稳定 `EdgeId`、起终 Facility、门桥 Facility、整数距离、加权距离、时长、体力和口粮成本，
不保存 Unity `Vector3[]` 作为世界事实。

### 3.3 迁移

V75→V76 只初始化空的本地导航、道路状态和移动集合；旧人物获得合法默认体力和空的本地位置引用。
迁移不会从旧演示 GameObject、逐帧坐标或会话路线推断正式世界事实。正式洛阳导航事实由显式初始化命令
创建。

## 4. 命令、事件与数据流

### 4.1 持久命令

```text
mandate.command.luoyang-player-movement.initialize.v1
mandate.command.luoyang-player-movement.request.v1
mandate.command.luoyang-player-movement.advance-segment.v1
mandate.command.luoyang-road-segment.transition.v1
```

请求命令不信任 UI 成本；Domain/Simulation 使用当前人物、当前起点、当前道路与门桥事实重新规划并计费。

### 4.2 正式事件

```text
mandate.event.person-movement.started.v1
mandate.event.person-movement.progressed.v1
mandate.event.person-movement.completed.v1
mandate.event.person-location.changed.v1
mandate.event.person-movement.interrupted.v1
mandate.event.person-movement.route-invalidated.v1
mandate.event.luoyang-road-segment.transitioned.v1
```

### 4.3 执行链

```text
Mouse Click
→ World Target Resolver
→ persistent movement request command
→ formal Person/origin/target/resource validation
→ passage- and road-aware route snapshot
→ data-driven movement cost
→ existing WorldSimulator segment advancement
→ persistent segment command and state/events
→ Person time/stamina/food/location facts
→ Unity playback of the committed route
```

Presentation 不直接写 `PersonState`、世界时间、门桥或道路状态。

## 5. 成本与时间合同

默认策略 ID 为 `movement.policy.luoyang-pedestrian-world-settlement.v1`：

```text
步行速度              80 m / world minute
体力                  ceil(weighted metres / 20) basis points
进食周期              360 world minutes
口粮                  floor(total duration / 360)
正式世界 Segment      360 world minutes
```

每条边先向上取整为整数米，再应用既有道路权重和可选负重修正；所有除法均使用明确的整数向上取整或
向下取整。移动服务只通过现有 `WorldSimulator.AdvanceSegments` 推进生产、人口、设施、事件、经济和 AI
共享的世界时间。不足一个世界 Segment 的累计移动时间记录在行动的 `UnsettledDurationMinutes` 中，
完成行动时推进剩余正式 Segment，不建立第二套时钟。

体力不足以完成整次行动时拒绝命令。预计时长不足一个进食周期时口粮成本为 0；跨越进食周期且
`PersonState.Provisions` 不足时拒绝命令。

## 6. 动态通行与中断

- 普通道路只有 `road.segment.status.open.v1` 可通行；`blocked` 和 `destroyed` 不可通行。
- 城门、桥梁直接读取 V75 `LuoyangPassageTraversalState`；关闭、损毁或当前维修规则导致不可通行时，
  正式路线不能通过。
- 每个 Segment 提交前重新读取道路与门桥事实。若下一 Segment 已失效，行动在当前边界进入
  `Interrupted`，保留已消耗时间、体力、口粮和已到达位置，并发出 RouteInvalidated 与
  MovementInterrupted。
- 道路修复用带 ExpectedRevision 的正式 transition 命令恢复为 Open；门桥修复继续复用 V75 维修事实。

## 7. 存档、读档与确定性

V76 保存正式玩家引用、人物位置/体力/口粮、世界时间、门桥状态、道路状态和行动分段进度。V1 的安全
存档点是 Segment 边界；读档后服务从 `CurrentSegmentIndex` 继续，不重复扣费或丢失行动。

正式计算不得读取 `Time.deltaTime`、`Time.time`、`DateTime.Now`、无种子的随机数或帧计数。相同 V76
初始快照和命令序列必须得到相同的 Location、时间、体力、口粮、道路/门桥状态和世界状态哈希。

## 8. 验收矩阵

自动验收至少覆盖：

- 正式 PlayerSession 与请求命令落账；人物缺失、不可行动、起点陈旧、目标非法/不可达拒绝。
- 固定距离的时间、体力、口粮计算；资源不足拒绝。
- 普通道路阻断/损毁/修复；城门关闭；桥梁损毁；下一 Segment 失效中断。
- 时间、体力、口粮、Cell、Facility 与事件原子提交。
- Segment 边界保存、读档和继续；V75→V76 不虚构本地事实。
- 同一初始状态与命令序列运行三次，最终状态哈希完全相同。
- Unity 2022.3.62f3c1 ProjectLoad、目标 EditMode、正式玩家生成、点击、世界先结算再播放及读档一致性。

最终结果以 `LUOYANG_FORMAL_PLAYER_MOVEMENT_V1_ACCEPTANCE_REPORT.md` 为准。只有全部门禁通过才能写
`ACCEPTED`。

## 9. 后续顺序

本任务通过后，下一任务固定为“洛阳人物尺度近景地图与局部导航 V1”；随后才是洛阳外围供应区物化，
再随后是正式人物美术、动画和城市表现完善。不得把本 V1 自动扩大为 NPC 群体移动或全国人物寻路。
