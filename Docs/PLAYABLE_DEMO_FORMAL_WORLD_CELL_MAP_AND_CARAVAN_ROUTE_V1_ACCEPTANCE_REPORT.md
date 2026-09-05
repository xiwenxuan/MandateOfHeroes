# PlayableDemo 正式全国格子地图替换与商旅路线可视化 V1 验收报告

## 1. 结论

当前状态：`IMPLEMENTED / AUTOMATED UNITY RE-RUN BLOCKED BY OPEN EDITOR`。

旧版 `ProceduralSilkMapArt` 绢帛示意图已经退出普通玩家“地图”页的执行路径。该页现在由
`HanWorldV1` 全国自然地图、全国 32×32 Cell 引导 LOD、区域精确 2km Cell 和正式
`R003 / CellRoute / CivilianFreight` 路线投影驱动。

本任务没有修改世界结算、路线规划、库存、人口或存档结构，也不改变 M26 独立人工盲玩仍为
`NOT ACCEPTED / BLOCKED` 的既有结论。

## 2. 已交付

- `PlayableWorldMapProjectionSystem`：启程前读取正式 R003 计划，启程后读取同一 Freight 路线与进度。
- `PlayableFormalWorldMapController`：用独立 Camera 和 RenderTexture 把正式地图嵌入玩家页。
- 全国概览、跟随玩家/商队、显示/隐藏格子三项表现操作。
- 起点、终点、当前 Cell、已走路线、剩余路线和正式剩余距离展示。
- 玩家页首动态显示日期、身份、当前位置和旅途，不再硬编码固定标题。
- 嵌入式自然地图关闭自动 `Update/OnGUI`，避免玩家操作其他界面时误触地图 hover/选格。
- Core 与 PlayMode 回归用例。

## 3. 验收门禁

| 门禁 | 结果 | 证据 |
|---|---|---|
| 全工程编译 | PASS | `verify-project.ps1`，2026-09-01 12:25 |
| 计划路线读取 R003 | PASS | `PlayableFormalWorldMap_PlannedRouteUsesR003WithoutMutation` |
| 启程后读取 Freight 进度 | PASS | `PlayableFormalWorldMap_DepartureReadsFreightProgress` |
| 投影不改变 World Snapshot | PASS | 上述两项测试均做序列化前后相等断言 |
| Core 目标测试 | PASS 2/2 | `tmp/skill-verification/core-tests-20260901-122909-446.out.log` |
| 完整 Core 单进程尝试 | BLOCKED / 300s | 长期经济/人口回归超过普通硬上限；验证脚本清理子进程时另报 `taskkill Access denied`，没有结果文件，不计为 PASS |
| `git diff --check` | PASS | 同一验证运行 |
| 受控 Unity EditMode | BLOCKED / 120 | Unity 编辑器 PID 21736 正在打开项目 |
| 受控 Unity PlayMode | BLOCKED / 120 | Unity 编辑器 PID 21736 正在打开项目；安全入口未启动第二个 Unity，未关闭用户程序 |
| 独立人工盲玩 | NOT RUN | 属于 M26 既有最终门禁，不由本任务替代 |

Unity 门禁结果文件：

- `tmp/unity-validation/playable-formal-world-map-v1/unity-PlayMode-20260901-122625-327.summary.json`
- `tmp/unity-validation/playable-formal-world-map-v1/unity-PlayMode-20260901-122625-327.log`
- `tmp/unity-validation/playable-formal-world-map-v1-editmode/unity-EditMode-20260901-123540-248.summary.json`

## 4. 剩余复验

用户保存当前 Unity 工作后关闭该编辑器，再通过 `Tools/Run-UnityTestsSafe.ps1` 复跑
`Mandate.Tests.PlayMode.M26MerchantProductReadinessPlayModeTests.OrdinaryPlayerRoute_StartsPreviewsBuysAndDeparts`。
只有结果文件明确为 PASS 后，本任务才可改为最终 `ACCEPTED`。
