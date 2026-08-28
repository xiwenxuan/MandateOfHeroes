# 洛阳门桥状态化表现与人物尺度通行阻断 V1 任务书

任务 ID：`LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-08-28

## 一、任务目标

承接 V75 的 20 项洛阳门桥正式世界事实，关闭“世界账已经关闭、毁坏或维修，但地图中的人物尺度物理通行和可见状态没有同步”的表现缺口：

1. 从既有门桥状态、完整度和维修工单生成只读、确定性的通行表现投影；
2. 对 `closed` 与 `destroyed` 门桥启用非 Trigger 人物通行阻断，对 `open` 与 `damaged` 保持可通行；
3. 为开放、关闭、损坏、毁坏和维修中提供轻量、可替换的三维状态构件；
4. 门桥状态或 V75 世界投影刷新时，阻断、构件和已有导航状态标记在同一帧同步；
5. WORLD/CITY 切换时完整创建与清理运行时对象，不向最终建筑 Prefab 写入 Collider；
6. 不改变 V75 存档、Facility、Global Cell、资产批准状态或权威道路图。

## 二、权威输入与范围边界

- `Docs/TASK_LUOYANG_PASSAGE_GUARD_DAMAGE_AND_REAL_REPAIR_V1.md`；
- `Docs/TASK_LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1.md`；
- `Docs/TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`；
- `Docs/TASK_LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1.md`；
- `Docs/TASK_LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1.md`；
- `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`。

V75 的 `LuoyangPassageTraversalWorldState`、守军控制、战损流水和维修工单仍是唯一权威事实。Presentation 只读投影不得反向创建、修正或替代世界事实。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---|
| 门桥集合 | 继续复用 V75 的 20 项稳定 Facility ID |
| 投影来源 | `LuoyangPassageTraversalSession`；绑定世界时附加读取 V75 控制与维修集合 |
| 阻断状态 | `closed`、`destroyed` |
| 可通行状态 | `open`、`damaged`；损坏代价继续由既有路径规则保持 1,800‰ |
| 维修表现 | 只在存在 `InProgress` 维修工单时显示；不自行改变通行状态 |
| 物理代理 | CITY 当前驻留窗口内每项门桥最多一个非 Trigger `BoxCollider`；启停而非反复创建 |
| 状态构件 | 开门叶、闭门叶、受损残片、毁坏瓦砾、维修脚手五类低多边形可替换代理 |
| 朝向 | 从既有两侧道路接近边确定人物穿行轴，阻断面与穿行轴垂直 |
| 最终资产 | 不改 54 项 Prefab、FBX、LOD、锚点、材质或 `FinalArtApproved` |
| 存档 | 不升级 V75；不新增序列化字段；相同输入产生相同投影 |

状态构件是战略地图/人物通行调试级表现，不是东汉门扇结构、桥梁构件或施工工艺的考古复原。

## 四、实施方案

1. 在纯 C# Domain 中新增门桥人物通行投影合同，逐项保存稳定 Facility、定义、状态、完整度、门桥 Revision、完整度 Revision、维修中与阻断布尔值。
2. 投影校验 20 项集合、稳定顺序、状态映射、完整度范围和世界维修引用；无绑定世界的审图模式只从会话态确定基本表现。
3. 扩展 `LuoyangFacilityInteractionNavigationRuntime`：
   - 在当前 549 Facility 驻留窗口中为可见门桥建立独立运行时实例；
   - 复用一个低多边形网格和有限材质，不给最终 Prefab 添加 Collider；
   - 状态刷新时只调整启用、局部变换、材质和阻断 Collider。
4. 地图控制器公开当前驻留门桥数、活动阻断数、损坏/毁坏表现数、维修脚手数和单门桥只读状态；绑定、解绑、命令刷新与视图切换统一走同一刷新入口。
5. 核心测试覆盖状态映射、确定性、V75 完整度/维修投影和无存档变化；Unity 测试覆盖 Collider Trigger 属性、动态启停、可视构件、CITY/WORLD 清理与正式世界投影。

## 五、验收标准

1. 相同道路计划、会话和 V75 世界生成完全相同的 20 项稳定投影。
2. `closed`、`destroyed` 的人物阻断为真；`open`、`damaged` 为假，且与既有路径 `CanTraverse` 完全一致。
3. 绑定 V75 世界后，投影采用守军控制中的真实完整度与完整度 Revision；活动维修工单只产生维修表现，不伪造状态转换。
4. CITY 当前驻留窗口中的门桥各只有一个物理阻断代理，`BoxCollider.isTrigger=false`，状态切换只启停代理。
5. 状态化构件和已有地面覆盖层同时刷新；关闭、损坏、毁坏和维修状态可由运行时指标与组件回读。
6. 切换 WORLD 后，选择代理、门桥物理代理和状态构件全部清理。
7. 全工程编译、定向核心、目标 EditMode、相关图形 PlayMode、`git diff --check` 与范围审阅分别留证。

## 六、明确不在范围

- 不改变守军、战斗、维修材料、劳动、资金或存档规则；
- 不实现逐人物排队、钥匙、宵禁、通行证、拥堵或守军盘查；
- 不烘焙完整城内 NavMesh，不实现角色动画、局部避障、寻路代理或室内行走；
- 不实现门扇、吊桥、瓦砾和施工的骨骼动画、音效、粒子或最终美术；
- 不实现完整攻城、攻城器械、桥梁载重、洪水、船撞或逐构件损坏；
- 不提交、不推送、不关闭用户程序。

## 七、执行清单

- [x] 冻结任务书、来源、状态映射和非目标边界。
- [x] 实现纯 C# 人物通行与状态表现投影。
- [x] 实现 Unity 运行时阻断代理和状态构件。
- [x] 接入地图控制器刷新、查询与清理。
- [x] 增加核心、EditMode 与相关 PlayMode 测试。
- [x] 更新系统总纲与任务路由。
- [x] 完成编译、核心、Unity、diff 与范围验收并回填证据。

## 八、实施结果与证据

### 8.1 已实现

- 新增 20 项稳定门桥的人物通行/状态表现只读投影；相同计划、会话与世界产生相同稳定顺序和状态值；
- 无绑定世界时从既有会话态推导基本完整度，绑定 V75 世界时读取真实控制完整度、完整度 Revision 和活动维修工单；
- `closed`、`destroyed` 启用非 Trigger `BoxCollider`，`open`、`damaged` 关闭阻断体，和既有 `CanTraverse` 保持同一规则；
- CITY 驻留窗口为可见门桥各建立一个可复用运行时实例，状态切换只更新组件，不反复创建物理代理；
- 开放、关闭、损坏、毁坏和维修中使用共享低多边形网格与有限材质形成可替换构件；
- 地图控制器公开驻留门桥、活动阻断、损坏、毁坏、维修脚手计数和单门桥状态查询；
- 绑定、解绑、命令刷新、会话重置与 WORLD 清理均接入同一刷新生命周期；
- 修正 V75 合同中仍遗留的旧 `UNITY_STARTUP_BLOCKED` 状态常量，使其与已经取得的受控 Unity 通过证据一致。

### 8.2 验证结果

| 门禁 | 结果 | 证据 |
|---|---|---|
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260828-180617-435.out.log` |
| 定向核心 | 6/6 通过 | `tmp/skill-verification/core-tests-20260828-180707-802.out.log` |
| 目标 EditMode | 1/1 通过 | `tmp/unity-validation/unity-EditMode-20260828-180734-351.summary.json` 与非空 NUnit XML |
| 目标图形 PlayMode | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-180810-561.summary.json` 与非空 NUnit XML |
| 正式世界绑定 PlayMode | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-180123-230.summary.json` 与非空 NUnit XML |
| 上一交互导航图形回归 | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-180937-383.summary.json` 与非空 NUnit XML |
| `git diff --check` | 通过 | 最终范围验收 |

首次在受限工作区内启动目标 EditMode 时，Unity 在 45 秒内没有创建启动日志，安全脚本只终止本次
PID；按项目测试规则在工作区外使用相同安全命令重跑后产生有效日志/XML并通过。此事实继续归类为
宿主沙箱启动边界，不是代码、项目锁、筛选或许可证失败。

图形验收生成全城视图和门桥近景。近景可同时辨认最终门楼、两侧道路接近线、选择框、地面关闭
标记和红色闭门构件；自动门禁同时验证阻断 Collider、四状态切换与 WORLD 清理。证据入口为
`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1/`。

以上均为本任务定向验收，不替代完整核心、EditMode 或 PlayMode 分组回归。

## 九、后续候选

本任务完成后，再单独选择以下一项，不自动扩大：

1. 人物角色、道路宽度、局部避障与可点击移动的完整城内步行竖切片；
2. 门扇、吊桥、瓦砾和施工的最终动画/音效/特效；
3. 守军换防、失控、占领与组织控制权变更；
4. 完整围城与攻城器械结算。
