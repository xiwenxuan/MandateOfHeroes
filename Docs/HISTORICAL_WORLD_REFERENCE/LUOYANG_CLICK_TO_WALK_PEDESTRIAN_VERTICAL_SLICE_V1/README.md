# 洛阳可点击道路步行与动态门桥阻断竖切片 V1 证据

任务：`LUOYANG_CLICK_TO_WALK_PEDESTRIAN_VERTICAL_SLICE_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 已证明范围

- 步行计划只读复用洛阳 379 节点、402 边精化图与 20 项门桥状态；
- 普通道路、玩法重建连接、城门和桥梁分别使用 18m、12m、12m、8m 可通行宽度；
- 稳定角色 ID 决定同一侧移方向，相同输入得到相同路线、距离与预计时长；
- 右键落点与显式目标只吸附当前 CITY 驻留窗口的正式道路/门桥节点；
- 一名关注范围人物代理具有非 Trigger CapsuleCollider、可见低多边形身体、亮黄色当前路线和洋红目标；
- 移动中的必要门桥关闭或毁坏后，同一刷新周期取消路线并报告稳定阻断原因；
- WORLD 切换清理人物、路线、目标、选择代理和门桥表现根；
- 不创建 PermanentPerson，不升级 V75，不保存逐帧坐标、路线或显示状态。

## 截图

- `Screenshots/01_OPEN_GATE_CLICK_WALK_ROUTE_AND_ACTOR.png`：北宫南门开放近景。蓝衣人物位于门洞，
  亮黄色带为当前点击步行路线，青色带为既有道路图，洋红块为目标标记，绿色构件为开放门叶。

人物、路线宽度和侧移具有战略地图最小可读放大；Domain 中的道路宽度、净空、速度、距离和预计时间
仍保存米制合同。本图不是人物身高、门洞或道路宽度的 1:1 考古测绘。

## 自动验收

| 门禁 | 结果 | 证据 |
|---|---|---|
| 最终全工程编译 | 通过 | `tmp/skill-verification/compile-20260828-191544-399.out.log` |
| 合并定向核心 | 7/7 通过 | `tmp/skill-verification/core-tests-20260828-191632-717.out.log` |
| 最终目标 EditMode | 1/1 通过 | `tmp/unity-validation/unity-EditMode-20260828-191151-809.summary.json` |
| 最终目标图形 PlayMode | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-191653-930.summary.json` |
| 上一门桥状态化图形回归 | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-190801-840.summary.json` |
| 建筑选择/道路图形回归 | 1/1 通过 | `tmp/unity-validation/unity-PlayMode-20260828-190909-977.summary.json` |

首次受限工作区内启动目标 EditMode 时，Unity 在 45 秒内没有生成启动日志；安全脚本只终止本次 PID。
使用完全相同的安全脚本在沙箱外重跑后产生有效日志与 NUnit XML 并通过，继续归类为宿主沙箱启动边界。

以上是当前任务的定向证据，不替代完整核心、EditMode 或 PlayMode 分组回归。最终 `git diff --check`
与范围审阅通过，详细边界记录在任务书中。
