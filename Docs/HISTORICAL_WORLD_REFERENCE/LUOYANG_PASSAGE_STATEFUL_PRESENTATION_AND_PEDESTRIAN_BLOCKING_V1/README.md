# 洛阳门桥状态化表现与人物尺度通行阻断 V1 证据

任务：`LUOYANG_PASSAGE_STATEFUL_PRESENTATION_AND_PEDESTRIAN_BLOCKING_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 证据范围

本目录证明 V75 的 20 项门桥正式世界事实已经具有只读人物通行投影和 Unity 状态化表现：

- `open`：开放构件，人物阻断关闭；
- `closed`：闭门构件，人物阻断启用；
- `damaged`：受损构件，人物阻断关闭，既有路径代价保持 1,800‰；
- `destroyed`：瓦砾构件，人物阻断启用；
- 活动维修：在权威通行状态之上增加脚手表现，不自行改写通行事实。

运行时对象独立于 54 项最终美术 Prefab，切换 WORLD 后与选择代理、道路覆盖层一起销毁。本目录不证明完整 NavMesh、角色动画、室内行走、门扇骨骼动画或考古复原。

## 截图

- `Screenshots/01_CLOSED_GATE_PEDESTRIAN_BLOCKER.png`：北宫南门关闭近景，可见门楼、两侧道路接近线、黄色选择框、红色地面关闭标记和红色闭门构件。

上一阶段的全城构图截图由同一最终图形回归更新：

- `../LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1/Screenshots/01_DENSE_CITY_MODELED_CONNECTORS_AND_CLOSED_GATE.png`。

## 自动验收

| 门禁 | 结果 | 记录 |
|---|---|---|
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260828-180617-435.out.log` |
| 定向核心 | 6/6 | `tmp/skill-verification/core-tests-20260828-180707-802.out.log` |
| EditMode | 1/1 | `tmp/unity-validation/unity-EditMode-20260828-180734-351.summary.json` |
| 图形 PlayMode | 1/1 | `tmp/unity-validation/unity-PlayMode-20260828-180810-561.summary.json` |
| V75 正式世界绑定 PlayMode | 1/1 | `tmp/unity-validation/unity-PlayMode-20260828-180123-230.summary.json` |
| 交互导航图形回归 | 1/1 | `tmp/unity-validation/unity-PlayMode-20260828-180937-383.summary.json` |

图形测试同时断言：非 Trigger Collider、开放/关闭/损坏/毁坏动态启停、稳定状态 ID、截图非空白和 WORLD 清理。
