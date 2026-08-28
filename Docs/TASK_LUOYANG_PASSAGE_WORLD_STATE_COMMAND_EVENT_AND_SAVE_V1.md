# 洛阳门桥 WorldState、命令事件与存档 V1 任务书

任务 ID：`LUOYANG_PASSAGE_WORLD_STATE_COMMAND_EVENT_AND_SAVE_V1`

状态：目标实现与目标验收通过，待用户复核

日期：2026-08-28

## 一、任务目标

把上一阶段 20 项城门、宫门、军门和桥梁的 Domain 会话态提升为正式世界事实：

1. 将门桥当前通行状态、Revision、变更时间和稳定原因保存到 `WorldState`；
2. 复用 M25-P7 持久命令、批次结果、事务摘要和事件出站箱，不建立第二套执行账；
3. 通过一个显式历史初始化命令原子建立全部 20 项开放状态；
4. 通过逐门桥转换命令提交开放、关闭、受损和毁坏，拒绝过期 Revision 与同批冲突；
5. 将正式世界模式从 V73 升至 V74，提供 V73→V74 顺序空迁移；
6. 让地图控制器在绑定正式 `WorldState` 后只读取世界投影，并通过命令改变状态。

## 二、权威输入与兼容关系

- `Docs/TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`；
- `Docs/TASK_M25_P7_PERSISTENT_COMMAND_RESULTS_AND_EVENT_OUTBOX.md`；
- `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`；
- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- 当前 379 节点、402 边精化通行图与 20 项门桥清单。

上一阶段的 402 边图、28 条玩法重建连接和会话预览模式保持兼容。V74 新合同只提升门桥状态
权威与恢复能力，不改道路几何、Facility、Global Cell、所有权或建设事实。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---:|
| 旧正式世界模式 | V73 |
| 新正式世界模式 | V74 |
| 持久门桥记录 | 20 |
| 历史初始化命令 | 1，原子建立 20 项 |
| 初始化默认状态 | `passage.traversal.open.v1` |
| 状态转换并发条件 | `Facility ID + expected_revision` |
| 事务冲突资源 | 每个门桥稳定 ID |
| 事件恢复语义 | M25-P7 Outbox，至少一次分发边界 |
| V73→V74 迁移 | 只初始化空集合，不倒推旧会话状态 |

每项 `LuoyangPassageTraversalWorldState` 保存：稳定状态 ID、Facility/Definition ID、状态 ID、
Revision、最后变更日/时段、原因 ID、命令 ID和事件 ID。状态历史由保留的完成命令、批次结果、
事务摘要和事件出站箱闭合，不依赖 Unity 对象或列表位置。

## 四、实施方案

1. `WorldState.CurrentSchemaVersion` 升至 74，新增 `LuoyangPassageTraversals`。
2. `LuoyangPassageTraversalWorldRules` 校验空或完整 20 项、稳定排序、状态/时间/Revision、初始化
   快照、连续转换 Revision，以及当前记录到命令和事件的交叉引用。
3. `LuoyangPassageWorldCommandSystem` 注册初始化/转换处理器与投影事件处理器：
   - 初始化命令冻结 20 项 Facility/Definition 参数，并在一项事务内原子写入；
   - 转换命令冻结 Facility、Definition、expected revision、目标状态和原因；
   - 同门桥同批事务使用共享预约，冲突时整批拒绝且不修改世界；
   - 完成命令重放不再次提交，出站事件按处理器 ID 幂等确认。
4. 地图控制器增加显式 `BindLuoyangPassageWorld`。绑定后会话只是只读投影，状态更新必须走
   正式命令；未绑定的审图/预览仍使用旧会话态，不伪装成存档事实。
5. V73→V74 迁移只建立空集合。旧存档不会因加载而自动生成 20 项开放门桥；正式洛阳初始化
   必须显式提交初始化命令。

## 五、验收标准

1. 初始化命令一次性建立恰好 20 项，完成命令、事务结果和初始化事件引用闭合。
2. 转换命令使指定门桥 Revision 单调加一，并保存目标状态、原因、命令和事件。
3. 相同已完成命令不重复应用；同门桥同 Revision 的同批冲突在任何世界写入前拒绝。
4. V74 快照往返后状态、Revision、命令、结果、事件和分发确认保持一致且重序列化一致。
5. V73 迁移到 V74 后集合为空，零、未来版本继续拒绝。
6. 篡改最后原因、状态/命令/事件引用或不连续 Revision 时 `WorldState.Validate` 拒绝。
7. 从 V74 建立的地图会话是只读投影；关闭/毁坏门桥继续影响既有确定性寻路。
8. 全工程编译、定向核心、目标 EditMode、受影响 PlayMode、`git diff --check` 和范围审阅分别记录。

## 六、明确不在范围

- 不实现守军、职位权限、钥匙、夜禁、围城、冲车或历史战役规则；
- 不实现桥梁载重、洪水冲毁、材料消耗、维修工单或施工时长；
- 不实现门扇、吊桥和损毁动画，也不生成角色尺度 NavMesh；
- 不自动把任意旧预览会话状态写入正式存档；
- 不建设城外道路、外围桥渡和供应运输节点；
- 不提交、推送或关闭用户程序。

## 七、执行清单

- [x] 冻结 V74 门桥世界状态与命令/事件合同。
- [x] 实现初始化和转换事务、冲突预约与稳定事件 ID。
- [x] 实现 `WorldState` 不变量及 V73→V74 空迁移。
- [x] 实现地图控制器正式世界绑定与只读投影。
- [x] 增加当前往返、迁移、非法版本、篡改和冲突回归。
- [x] 完成最终 Unity 与相关图形回归。
- [x] 回填最终验证证据与完成状态。

## 八、当前验证记录

| 阶段 | 结果 |
|---|---|
| 中间全工程编译 | 通过；`tmp/skill-verification/compile-20260828-104506-428.out.log` |
| 中间新增核心合同 | 3/3 通过；`tmp/skill-verification/core-tests-20260828-104536-657.out.log` |
| 最终全工程编译 | 通过；`tmp/skill-verification/compile-20260828-110026-614.out.log` |
| 最终新增核心合同 | 3/3 通过；`tmp/skill-verification/core-tests-20260828-110040-784.out.log` |
| 最终目标 EditMode | 5/5 通过；`tmp/unity-validation/unity-EditMode-20260828-110043-456.summary.json` |
| 正式世界绑定图形 PlayMode | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-105457-733.summary.json` |
| 上一门桥图形回归 | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-105545-965.summary.json` |
| 全城构图图形回归 | 1/1 通过；`tmp/unity-validation/unity-PlayMode-20260828-105656-696.summary.json` |
| 最终 `git diff --check` | 通过；统一结果 `RESULT compile=passed core-tests=passed unity-tests=passed diff-check=passed` |

第一次目标 EditMode 运行得到4/5，唯一失败来自旧NUnit对`IReadOnlyList`使用`Has.Count`的断言兼容问题；
改为直接断言`Records.Count`后，最终5/5通过。该次失败没有暴露产品逻辑写入或迁移缺陷。

以上为本任务定向验收，不替代完整核心、EditMode或PlayMode回归。编译只保留工作区既有测试数据
载体的`CS0649`警告；本任务没有编译错误。没有提交或推送。

## 九、下一步

V74 完成后，下一项应建立门桥“可否改变状态”的世界原因链：守军与职位权限、攻城/破坏来源、
损坏程度、维修授权和真实材料/劳动工单。该任务必须复用现有组织、军队、建设和库存事实，不能
把状态按钮直接当成攻城系统。其后再接城外道路、桥渡和外围供应运输节点。
