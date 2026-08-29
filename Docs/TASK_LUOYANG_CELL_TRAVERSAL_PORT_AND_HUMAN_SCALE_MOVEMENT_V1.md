# TASK：洛阳人物尺度 Cell 四向通行、正式移动与近景表现 V1

## 1. 目标与结论

本任务把洛阳人物尺度正式移动权威收敛为：

```text
Global Cell
→ North / East / South / West Traversal Port
→ Cell Traversal Capability
→ Cell Route
→ MovePersonCommand / World Settlement
→ Unity Presentation Path
```

`LocalSpace`、入口、占地、道路中心线、门桥几何和 Streaming Chunk 继续作为派生表现资料，
但不再决定跨 Cell 正式路线。一个战略格仍是一个 Cell，一个 Cell 最多一个正式 Facility；道路、
巷道、城门和桥梁也是占 Cell 的 Facility，不在建筑 Cell 内建立第二套正式道路世界。

## 2. 范围

### 2.1 实现范围

- 为每个 Cell 固定建立 North、East、South、West 四个潜在端口；
- 定义 Terminal、Straight、Corner、T、Cross、OpenArea、Custom 内部拓扑；
- 端口记录启用、出入、角色、宽度、容量、移动能力、动态条件和正式世界对象；
- 支持 Foot、Horse、Cart、PackAnimal、Military 等稳定数据 ID；
- 定义 `FacilityAccessRequirement`：None、Optional、RoadRequired、VehicleRoadRequired；
- 建立确定性的 `CellTraversalPlanner` 与 `CellRoute`；
- 路线成本使用人物尺度 Traversal Metric，不使用 `CellCount × 2000m` 或 Unity
  `Vector3.Distance` 作为世界结算距离；
- 将同一 `MovePersonCommand`、人物位置、世界时间、体力、口粮、存档和重放接入
  `CellRoute`；
- Unity 只展开已批准的 CellRoute 端口锚点和近景几何，不成为世界权威。

### 2.2 不在本任务范围

- 不创建 SubCell 或第二套 Person、Facility、Road、Gate、Bridge；
- 不修改 5,980 个 Cell、2,084 个 Facility、359 个 Road、18 个 Gate-type 和 2 个
  Bridge 的正式身份；
- 不凭空补道路，不借建筑多入口形成穿楼捷径；
- 不升级 V77 存档结构，不重写旧存档位置；
- 不扩建洛阳外围供应区，不处理食品库存守恒差额 RCA。

## 3. 权威与架构

- `Mandate.Domain`：端口、拓扑、能力、成本、CellRoute 和确定性规划；
- `Mandate.Simulation`：在每个路段结算前复核正式道路、门、桥和 Facility 状态，并结算人物
  时间、体力、口粮与位置；
- `Mandate.Persistence`：沿用 V77 已存在的路段正式对象、条件、Cell 和厘米坐标字段；
- `Mandate.Presentation`：将 CellRoute 展开为道路/门桥锚点和可见路线，Streaming 只装卸表现；
- 旧 LocalNav 图只保留为 V77 旧命令兼容和表现几何来源，不再选择跨 Cell 正式路线。

## 4. 数据策略

洛阳计划必须从正式地图与 Facility 清单派生：

```text
CellTraversalProfile: 5,980
Facility-bound Profile: 2,084
Road: 359
Gate-type: 18
Bridge: 2
四端口/Profile: 4
```

所有现有 Facility 都取得通行配置。已有明确道路正面的仓储/官仓/坞堡使用
`RoadRequired`；历史数据中没有道路正面的同类设施保持 `Optional`，避免为了满足规则伪造道路。
建筑可作为目的地进入，但默认不可作为跨越该 Cell 的捷径。森林等非道路地表可允许步行，车辆则
只能使用其能力允许的端口。

## 5. 存档与确定性

- Save Schema 保持 V77；
- 新生成的移动命令使用 CellRoute 作为正式路线来源；
- 已保存的旧 V77 局部路段继续按原字段和条件恢复；
- Gate/Bridge/Road 状态不复制进静态拓扑，执行前从正式世界状态重新验证；
- 相同地图数据、世界状态、人物能力和指令必须产生相同 CellRoute 与最终世界 Hash。

## 6. 验收门禁

必须依次完成：

1. 全工程编译；
2. CellTraversal 专项核心测试；
3. 既有洛阳局部移动核心测试；
4. 固定指纹完整核心回归；
5. 受控 Unity EditMode 和图形 PlayMode；
6. `git diff --check` 与差异审阅；
7. 更新总纲、存档合同、任务路由、证据与验收报告；
8. 提交并推送。

正式验收结果见
[`LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTANCE_REPORT.md`](LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1_ACCEPTANCE_REPORT.md)。

## 7. 后续固定顺序

本任务达到 ACCEPTED 后：

1. 食品库存守恒差额 RCA 与修复；
2. 洛阳外围供应区与城市物流 V1。

不得继续在本任务内扩充建筑内部导航、全国人物级寻路或第二套局部世界。
