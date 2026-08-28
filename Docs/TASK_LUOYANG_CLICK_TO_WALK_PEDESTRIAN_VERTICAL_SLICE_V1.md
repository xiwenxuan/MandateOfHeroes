# 洛阳可点击道路步行与动态门桥阻断竖切片 V1 任务书

任务 ID：`LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-08-28

## 一、任务目标

承接洛阳 379 节点、402 边精化道路图和 20 项门桥人物阻断表现，建立一条可直接操作的城内步行竖切片：

1. 为道路、玩法重建连接、城门和桥梁冻结米制可通行宽度与人物净空合同；
2. 从既有门桥状态感知路径生成确定性步行计划、距离、预计时间和会车侧移；
3. 在 CITY 当前驻留窗口生成一名可见人物代理、目标标记和路线带；
4. 右键地图或显式选择道路节点后，人物沿道路节点移动；左键建筑选择保持不变；
5. 门桥在移动途中关闭或毁坏时立即取消受影响路线，非 Trigger 阻断体保留最终碰撞安全门；
6. 切回 WORLD 时完整清理人物、路线和目标对象；
7. 不创建、复制或重随机 PermanentPerson，不把逐帧位置写入世界账或存档。

## 二、权威输入与边界

- `Docs/TASK_LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1.md`；
- `Docs/TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`；
- `Docs/TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md`；
- `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`；
- `Docs/TASK_M26_P0_PLAYABLE_DEMO_MAIN_LOOP_INTEGRATION.md`。

步行代理只属于玩家关注范围的 Presentation 演出。调用方可传入真实 `PersonId`，审图默认使用稳定的
`presentation-person.luoyang.walk-review.v1`；两者都不能据此新增人物事实。人物的正式地点、旅行、时间和
体力变化仍须由共享世界命令完成，本 V1 不以逐帧地图坐标替代它们。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---:|
| 普通道路可行宽度 | 18m |
| 玩法重建连接可行宽度 | 12m |
| 城门通道可行宽度 | 12m |
| 桥梁通道可行宽度 | 8m |
| 人物净空半径 | 0.45m |
| 基准步速 | 1.35m/s |
| 会车侧移 | 由稳定角色 ID 决定左右侧，且不超过通道宽度 18% 或 1.2m |
| 点击吸附 | 只吸附到当前驻留窗口中的正式道路/门桥节点 |
| 门桥状态 | 继续只读既有 `LuoyangPassageTraversalSession` / V75 投影 |
| 存档 | 不新增字段、不升级 Schema、不保存逐帧路线或坐标 |
| 人物事实 | 不创建 Person；默认审图 ID 明确为 Presentation-only |

现有地图以 2km Global Cell 为基础。运行时保留上述米制规则，但人物模型、路线带和侧移在审图时使用
最小可读尺寸，不得将画面中的放大比例解释为人物身高或道路宽度的 1:1 考古测绘。

## 四、实施方案

1. 在 Domain 中新增只读步行计划：稳定角色、起终节点、逐段 Profile、宽度、距离、预计时间、侧移和
   失败原因；路径继续调用既有门桥状态感知 Dijkstra。
2. 逐段宽度由边与目标节点类型确定：普通道路、玩法重建连接、城门和桥梁分别使用冻结 Profile；
   受损门桥继续继承 1,800‰ 代价。
3. Presentation 在既有交互根下创建一名低多边形人物、非 Trigger CapsuleCollider、目标标记和路线 Mesh；
   同一驻留窗口内只保留一名受关注代理。
4. 人物按确定性路线与稳定侧移移动；移动采用加速审图时钟，不改变 Domain 预计时长。物理查询忽略
   选择 Trigger，并对启用的门桥阻断体保留碰撞停止保护。
5. 路线建立后若门桥状态刷新使剩余路径非法，立即停止并报告稳定阻断原因；重新开放后需重新下达
   移动目标，避免 Presentation 自行伪造玩家命令。
6. 控制器公开角色、路线、当前节点、目标节点、状态、预计距离/时间、门桥接近点与测试步进入口。

## 五、验收标准

1. 相同角色、起点、终点与门桥状态生成完全相同的节点序列、侧移、距离和预计时长。
2. 道路、玩法重建连接、城门和桥梁宽度 Profile 值有效，人物净空小于半通道宽度。
3. 开放和受损门桥可生成路线，受损预计成本更高；关闭和毁坏的起终门桥明确拒绝。
4. CITY 运行时存在一名人物代理、非 Trigger CapsuleCollider、目标标记与非空路线 Mesh。
5. 显式目标和右键落点均只吸附到当前驻留道路节点；左键 Facility 选择不受影响。
6. 移动中的必要门桥关闭后，路线在同一刷新周期取消，人物不会穿过启用的阻断体。
7. WORLD 切换后人物、路线、目标和既有交互根全部清理。
8. 全工程编译、定向核心、目标 EditMode、图形 PlayMode、截图、`git diff --check` 与范围审阅分别留证。

## 六、明确不在范围

- 不实现全城 Unity NavMesh 烘焙、室内导航、楼梯、船只或车辆；
- 不实现多人物人群模拟、排队、ORCA/RVO、动态拥堵或逐人 AI 调度；
- 不让点击步行直接消耗世界时间、体力、口粮或改变正式 Person Location；
- 不制作最终角色 FBX、骨骼、步行动画、服装、音效、脚印或对话；
- 不改变门桥守军、战损、维修、建设、产权、库存或存档；
- 不提交、不推送、不关闭用户程序。

## 七、执行清单

- [x] 冻结任务书、人物事实边界与米制道路宽度。
- [x] 实现确定性步行计划与门桥状态映射。
- [x] 实现人物、路线、点击落点、移动与碰撞安全停止。
- [x] 接入控制器查询、刷新和 WORLD 清理。
- [x] 增加核心、EditMode 与图形 PlayMode 验收。
- [x] 更新证据、系统总纲与任务路由。
- [x] 完成分层验证并回填证据。

## 八、实施结果与证据

### 8.1 已实现

- 新增纯 C# 步行计划，保存稳定角色引用、道路节点序列、逐段宽度 Profile、米制距离、受损加权距离、
  预计时长与稳定左右侧移；失败计划具有明确稳定原因；
- 开放/受损门桥继续可进入，受损进入代价保持 1,800‰；关闭/毁坏的起终门桥明确拒绝；
- CITY 既有交互根下只建立一名受关注人物、非 Trigger CapsuleCollider、亮黄色路线和洋红目标；
- 人物由正式道路节点位置和路线驱动，表现时钟加速但不改 Domain 的 1.35m/s 预计时长；
- 右键射线忽略选择 Trigger 并吸附最近驻留道路节点，显式节点 API 供正式输入层和测试复用；
- 门桥状态刷新会检查剩余路线，必要门桥关闭或毁坏时立即停步；非 Trigger 门桥 Collider 保留物理保护；
- 控制器公开角色、当前/目标节点、状态、停止原因、路线节点、距离、时长和门桥两侧接近道路；
- WORLD 生命周期销毁角色、路线、目标和共享运行时资源；没有修改最终 54 项 Prefab/FBX。

### 8.2 验证记录

| 门禁 | 结果 | 证据 |
|---|---|---|
| 最终全工程编译 | 通过 | `tmp/skill-verification/compile-20260828-191544-399.out.log` |
| 合并定向核心 | 7/7 通过 | `tmp/skill-verification/core-tests-20260828-191632-717.out.log` |
| 最终目标 EditMode | 1/1 通过 | `tmp/unity-validation/unity-EditMode-20260828-191151-809.summary.json` 与非空 NUnit XML |
| 最终目标图形 PlayMode | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-191653-930.summary.json` 与非空 NUnit XML |
| 上一门桥状态化图形回归 | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-190801-840.summary.json` |
| 建筑选择/道路图形回归 | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-190909-977.summary.json` |

首次在受限工作区启动目标 EditMode 时，Unity 在 45 秒内没有创建启动日志，安全脚本只终止本次 PID；
按项目规则在沙箱外使用相同安全命令重跑后通过。这是宿主沙箱启动边界，不是代码、项目锁、筛选、
许可证或测试失败。

图形验收已人工检查：北宫南门、蓝衣人物、青色基础道路、亮黄色当前步行路线、洋红目标和绿色开放
门叶可同时辨认。自动测试还覆盖动态闭门停止、受损重新通行、Collider 属性和 WORLD 清理。证据入口为
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1/`。

以上是定向验收，不替代完整核心、EditMode 或 PlayMode 分组回归。最终 `git diff --check` 与范围审阅
通过；本轮未修改 Save Schema、程序集、Unity 版本或最终资产批准状态。

## 九、后续候选

1. 将点击步行绑定 M26 正式玩家 Person、行动命令、时间/体力/口粮和 Location 到达回写；
2. 加入第二名以上关注人物的队列、会车、拥堵和确定性 RVO/ORCA；
3. 建设城内近景地形、实际路口/巷道和可分块 NavMesh；
4. 制作角色 FBX、骨骼步行动画、服装层级、脚步音效与交互动作。
