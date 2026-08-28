# 洛阳道路断点细化与城门/桥梁动态通行 V1 任务书

任务 ID：`LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1`

状态：已完成目标实现与目标验收，待用户审图

日期：2026-08-28

## 一、任务目标

在不修改上一阶段 379 节点/382 边基础图历史合同的前提下，新增一层可审计的精化通行图：

1. 将 28 条匿名 `Provisional` 道路断点边升级为带稳定 ID、来源边、格级折线路径、证据等级和
   空间精度、优先最少穿越非通行Facility格的四邻路径和穿越计数的玩法重建连接；
2. 将 18 座城门/宫门/军门与 2 座桥由单侧道路叶节点升级为各有两条道路接近边的通行节点；
3. 在纯 C# Domain 中建立 20 项门桥会话态，支持开放、关闭、受损、毁坏和单调时间变更；
4. 让确定性寻路读取门桥状态，关闭或毁坏的门桥不可进入，受损门桥提高通行代价；
5. 在 CITY 视图以青色显示严格道路、橙色显示玩法重建连接、红色显示封闭门桥、橙黄色显示
   受损门桥，并保持 549 个独立选择触发器和 WORLD 清理合同。

## 二、权威输入与兼容关系

- `Docs/TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md`；
- `Docs/TASK_LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1.md`；
- `Docs/TASK_M16_LIVING_WORLD_MAP_INFORMATION_AND_DELEGATION_DESIGN.md`；
- `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`；
- 2,084 项洛阳 Facility 与全城构图锚点。

上一阶段 382 边基础图保留不变，旧测试继续验证其 334 条严格道路边、28 条临时边和 20 条单侧
接入边。本任务以只读方式消费该基础图并产生 402 边精化层，不用新口径改写旧任务的完成记录。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---:|
| 导航节点 | 379 |
| 严格四邻接道路边 | 334 |
| 身份化玩法重建连接 | 28 |
| 门桥节点 | 20 |
| 门桥双侧接近边 | 40 |
| 精化通行边合计 | 402 |
| 玩法重建证据标签 | `historical_evidence.gameplay_reconstruction` |
| 空间精度 | `cell` |
| 创建 Simulation SubCell | 否 |
| 修改 Save Schema | 否 |
| 跨读档持久化 | 否，本阶段为明确的 Domain 会话态 |

稳定状态 ID：

- `passage.traversal.open.v1`；
- `passage.traversal.closed.v1`；
- `passage.traversal.damaged.v1`；
- `passage.traversal.destroyed.v1`。

普通内容使用稳定命名空间 ID；本任务没有新增存档字段、版本或迁移。门桥状态已经从 UI 提升到
Domain 规则层，但在正式接入 `WorldState`、命令/事件账和迁移前，不能宣称读档后保持，也不能作为
完整攻城世界事实使用。

## 四、实施方案

1. `LuoyangRoadTraversalRefinementPlan` 包装旧图，不原地改变旧边。
2. 每条旧临时边生成一个 `LuoyangModeledRoadConnector`，记录稳定 Connector ID、来源边、两端
   Facility、逐序号格坐标、固定北东南西扩展顺序的“最少阻挡数→最少步数→Cell ID”路径、实际
   穿越非导航Facility格计数、玩法重建证据和“非史实精确”标记。当前2km抽象设施格可能封闭道路
   连通片，因此穿越计数必须显式审计，不能静默声称完全避障。
3. 每个门桥按曼哈顿距离、方向对置等级和 Facility ID 稳定排序选择两个不同道路接近点。
4. `LuoyangPassageTraversalSession` 保存 20 项 Domain 记录；变更要求稳定原因 ID、非负时间且不得
   回退时间，同状态重放不增加 Revision。
5. 精化寻路使用稳定 Dijkstra；关闭/毁坏节点不可进入，受损节点使用 1.8 倍代价。
6. Presentation 只读取精化图和 Domain 状态；它不拥有通行事实。切换地图视图销毁 Mesh、Collider
   和选择状态，但初始化后的 Domain 会话在控制器生命周期内保留。

## 五、验收标准

1. 28 个 Connector ID 唯一，每条路径首尾匹配原节点、相邻 Waypoint 恰好相差一个四邻格。
2. 精化图恰好 402 边且没有 `Provisional=true`；20 个门桥各有恰好两条接近边。
3. 相同输入重复生成 Connector、边、路径和同分裁决结果一致。
4. 状态变更拒绝未知状态、负数/回退时间和空原因；关闭门桥的路径为空，重新开放后路径恢复。
5. CITY 保持 549 个独立 Trigger Collider，显示非空青色道路、橙色连接和关闭门红色标记。
6. 截图为 1600×1000、文件非空并通过像素方差断言；Null Graphics 图片不能通过。
7. WORLD 切换后运行时交互根和代理清零。
8. 全工程编译、定向核心、目标 EditMode、目标图形 PlayMode、相关图形回归和
   `git diff --check` 分别记录。

## 六、明确不在范围

- 不把 28 条玩法重建折线描述为汉代道路史料或考古精确路线；
- 不制作人物/车马尺度 NavMesh、局部避障、队伍宽度、实体阻挡或动画门扇；
- 不实现守军、权限、钥匙、围城、冲车、桥梁载重、洪水、维修材料或施工工单；
- 不升级 `WorldState`、Save Schema、快照和迁移；关闭状态在本阶段不跨读档；
- 不改变 Facility、Global Cell、产权、人口、库存、财政、建设权限和历史初始化数据。

## 七、执行清单

- [x] 建立 28 条身份化道路连接和逐格折线路径。
- [x] 建立 402 边精化图和 20 个门桥双侧接近合同。
- [x] 建立 Domain 门桥会话态、单调变更和确定性状态感知寻路。
- [x] 接入 CITY 橙色连接、关闭/受损门桥标记和控制器 API。
- [x] 保持旧 382 边合同、549 Trigger 和 WORLD 清理兼容。
- [x] 新增核心、EditMode、图形 PlayMode 与截图门禁。
- [x] 建立证据目录并同步总纲、地图计划和任务路由。
- [x] 相关图形回归和最终统一验证回填。

## 八、当前验证记录

| 阶段 | 结果 |
|---|---|
| 中间全工程编译 | 通过；`tmp/skill-verification/compile-20260828-095717-592.out.log` |
| 新增核心合同 | 1/1 通过；`tmp/skill-verification/core-tests-20260828-095729-783.out.log` |
| 目标 EditMode | 3/3 通过；最终结果见`tmp/unity-validation/unity-EditMode-20260828-102637-648.summary.json` |
| 目标图形 PlayMode | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-102420-857.summary.json` |
| 上一阶段选择/道路图形回归 | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-101237-466.summary.json` |
| 全城构图图形回归 | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-101354-901.summary.json` |
| 截图像素方差 | 通过；目标 PlayMode 内置断言 |
| 最终统一全工程编译 | 通过；`tmp/skill-verification/compile-20260828-102620-772.out.log` |
| 最终统一核心合同 | 1/1 通过；`tmp/skill-verification/core-tests-20260828-102634-930.out.log` |
| 最终统一 EditMode | 3/3 通过；`tmp/unity-validation/unity-EditMode-20260828-102637-648.summary.json` |
| 最终 `git diff --check` | 通过；统一验证结果 `RESULT compile=passed core-tests=passed unity-tests=passed diff-check=passed` |

最初在沙箱内启动的 Unity 无项目烟测和目标测试均在创建日志前被安全脚本终止，不计为代码或测试
失败。许可证客户端在此前授权范围内重启后正常，但决定性差异是 Unity GUI/批处理必须在沙箱外运行；
获得受控执行权限后目标 EditMode 和图形 PlayMode 均通过。

以上为本任务定向验收，不替代全量核心、EditMode或PlayMode回归。编译阶段仅出现既有测试数据载体
字段未显式赋值的 `CS0649` 警告；本任务新增程序集无编译错误。

## 九、下一步

本任务关闭“匿名断点边”和“门桥只能静态常通”的战略图缺口。下一阶段应将门桥状态正式接入
`WorldState + Command + DomainEvent + Snapshot/Migration`，并补守军/权限/损坏/维修原因链；其后再
建设城外道路、桥渡与外围供应运输节点。人物尺度局部 NavMesh 必须等待近景地形与实际通道宽度合同，
不能直接由当前 2km Cell 折线烘焙。

后续兼容记录（2026-08-28）：`LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1` 已按上述下一步
建立V74 `WorldState + Command + DomainEvent/Outbox + Snapshot/Migration`合同；本任务的会话态声明仍
作为V1历史边界保留，不回写成当时已经持久化。守军/权限/围城、桥梁载重/洪水/维修和动画仍未完成。
