# 洛阳人物尺度近景地图与局部导航 V1：验收报告

## 1. 交付身份

```text
Task: TASK_LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1
Branch: codex/m23-p4-quality-artisan-growth
Baseline: e0ab8740d33763d5bb88fd1414a2e224ed8200c3
Implementation Commit: 1b05faf7815ddf200fe1df3bde4b0fe2da899c91
Unity Version: 2022.3.62f3c1
Save Version: V77
Map Version: luoyang.local-map.master.v1
Formal Acceptance: NOT ACCEPTED
```

## 2. 规模与稳定结果

```text
Facility Count: 2084
Spatial Anchor Count: 2084
LocalSpace Count: 5980
Navigation Node Count: 1959
Navigation Edge Count: 1976
Cell Transition Count: 4920
Gate-type Facility Count: 18
Major Historical Gate Count: 12
Bridge Count: 2
Map SHA-256: 894004b3bd1b09acba46c753e09efdd0d6b91303b2ab1489e44857c1da8f2b18
```

## 3. Formal Acceptance Gate

| Gate | 结果 | 说明 |
|---|---|---|
| A 空间架构继承 | PASS | 复用 V68/M26-P5B 空间事实；LocalSpace 非 SubCell；无第二套 Person/Facility/Road/Gate/Bridge 权威 |
| B 编译与核心回归 | PASS | Full Compile PASS；Targeted 19/19；Core 766/766；Introduced Regression 0；diff-check PASS |
| C 全量 Facility | PASS | 2,084 Anchor/Cell/Capability/Footprint 全有效；1,560/1,560 必需 Access 有效；Critical Invalid 0 |
| D Local Navigation | PASS（Core） | Road、Intersection、Facility Access、Gate/Bridge Node、Cross Cell 与非法通行拒绝均有 Domain 测试 |
| E 门桥 | PASS（Core） | 18 Gate-type、2 Bridge 全映射；开闭/重开、损毁中断读取正式状态 |
| F 正式人物移动 | PASS（Core） | 同一 M26 Person、同一 MovePersonCommand；时间、体力、口粮、位置和 Cross Cell 均进入世界账 |
| G Unity | BLOCKED | EditMode/PlayMode 未进入测试框架；Road/Facility Click、碰撞与近景场景无本轮 Unity 执行证据 |
| H Streaming | PARTIAL | Domain 3×3 范围、世界事实隔离和五 Cell 往返代码已覆盖；Unity 实际 Load/Unload 测试未运行 |
| I Save / Load | PASS（Core） | 局部位置、移动中、跨 Cell、Gate Waiting、Bridge 中断和 V76→V77 迁移通过 |
| J Determinism | PASS（Core） | 3/3 Replay，位置、时间、口粮、体力、路线与世界 Hash 一致 |
| K 性能 | NOT ACCEPTED | Domain 生成/路径数据已取得；Unity Streaming、对象数、帧时间、Nav Cost 未取得 |

## 4. 验证结果

```text
Full Compile: PASS
Core Regression: 766 / 766 PASS
Targeted Tests: 19 / 19 PASS
Unity EditMode: BLOCKED / NOT RUN (startup log gate, code 125)
Unity PlayMode: BLOCKED / NOT RUN (startup log gate, code 125)
Facility Audit: 2084 / 2084 PASS
Road Connectivity: PASS (Core)
Gate: 18 / 18 mapped; dynamic Core tests PASS
Bridge: 2 / 2 mapped; dynamic Core tests PASS
Cross Cell: PASS (Core)
Streaming: Domain PASS; Unity NOT RUN
Save Load: PASS (Core)
Replay: 3 / 3 PASS
Performance: Domain measured; Unity evidence unavailable
Introduced Regression: 0 in completed 766-test Core suite
```

两个获用户明确授权的历史慢测使用专属 900 秒限制并分别通过；其余核心用例继续使用 300 秒
限制。完整回归通过 12 个固定指纹分组逐项运行并聚合，避免以单一长驻进程绕过防卡死规则。

## 5. 性能记录

正式数据与采样方法见
[`Evidence/LuoyangLocalMapV1/README.md`](Evidence/LuoyangLocalMapV1/README.md)。当前 Domain 完整地图
生成 660.2841 ms，50 条路径样本 50/50 成功，中位 1.2130 ms、P95 2.6585 ms、最大
9.6555 ms。3×3 Unity Streaming 的对象、Mesh、Collider、加载/卸载、帧时间、GC 和 Nav Cost
测试已经编写，但 Unity 未生成启动日志，不能提供伪造或替代数据。

## 6. 未通过原因与收口条件

本任务的代码和核心回归阶段已经完成，但 Formal Gate G、H 的 Unity 部分和 Gate K 尚缺本轮
Unity 2022.3.62f3c1 实际证据。EditMode 与 PlayMode 各自启动前均确认没有用户 Unity/Hub 进程；
安全入口创建任务进程后，45 秒内日志保持不存在/空文件，随后只终止本任务 PID。没有关闭用户
程序，也没有得到测试失败 XML。

重新达到 ACCEPTED 至少需要：

1. 排除 Unity 无启动日志的本机环境问题；
2. 取得新增 EditMode/PlayMode 的正式 XML 与日志；
3. 在 Unity 中验证点击、Player Spawn、碰撞、门桥、跨 Cell、五 Cell Streaming 往返与恢复；
4. 记录 Streaming Load/Unload、GameObject、Mesh、Collider、Managed/Working Set、GC、Frame Time 和 Nav Cost；
5. 更新本报告后重新执行最终验证。

## 7. 最终结论

```text
NOT ACCEPTED
```

允许提交和推送当前“实现完成、核心验证通过、Unity 环境门禁未解除”的有效阶段成果，但不得把它
描述为本任务正式验收完成。食品库存守恒 RCA 只有在本任务后续达到 ACCEPTED 后才进入正式顺序。
