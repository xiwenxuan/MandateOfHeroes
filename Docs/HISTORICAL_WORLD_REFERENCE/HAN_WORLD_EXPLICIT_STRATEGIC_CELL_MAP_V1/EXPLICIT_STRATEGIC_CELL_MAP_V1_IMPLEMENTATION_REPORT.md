# 显式战略格地图 V1 实施报告

## 结论

河南尹显式战略格 V1 已完成代码与测试建设，并通过离线几何执行检查和四文件 Roslyn 语法检查。由于本机缺少项目指定的 Unity 2022.3.62f3c1，且现有 Unity 6 没有有效 Editor 许可证，本轮不能生成或宣称新的 Game View 截图，也不能把 Unity 测试写成通过。

当前门禁：`IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。

## 已实现

- 新增 `presentation.han-world.explicit-strategic-cell-map.v1` 表现合同。
- 直接使用既有 2000m `hanworld.square-grid.v1` 和稳定 `WorldMapCellId`。
- 河南尹 24×24、576 格贴地格面与格边。
- 普通、悬停、选中三类格面颜色，以及悬停/选中强化轮廓。
- 格面与格边保持两个合批渲染对象，不为每格创建 GameObject。
- 鼠标射线悬停与左键选中，结果只保存在 Presentation。
- 河南尹总览、洛阳选中近景、河南山地三组固定审查相机。
- 新增透明顶点色战略格 Shader。

## 保持不变

- Global Origin、3314×2176、7,211,264 个 Cell、2000m Cell 边长。
- 八邻接、Region 成员关系、地形绑定、确定性世界事实与存档合同。
- 不建立 SubCell，不迁移六边形，不复制商业游戏资产、画面或代码。

## 验证结果

| 阶段 | 结果 | 证据 |
|---|---|---|
| 离线战略格几何执行检查 | PASS | `RESULT passed=11 failed=0` |
| Roslyn C# 语法检查 | PASS | `RESULT parsed=4 errors=0` |
| 全工程编译 | BLOCKED | 本机没有项目要求的 Unity 2022.3.62f3c1 与受支持的 VS/MSBuild/.NET 4.7.1 targeting pack |
| 核心测试 | NOT RUN | 全工程编译环境未建立；本轮不以离线几何检查替代核心测试 |
| 目标 EditMode | BLOCKED | `Tools/Run-UnityTestsSafe.ps1` 找不到固定 Unity 2022 路径 |
| 隔离副本 Unity 6 PlayMode 兼容烟测 | BLOCKED | Package Manager IPC 在非沙箱恢复，但 Editor 无有效许可证，退出码 198、无 XML |
| 目标 Unity 2022 PlayMode | NOT RUN | 正确 Editor 未安装，三张预定 Game View 截图未生成 |
| `git diff --check` | PASS | 2026-08-26 执行；仅报告用户既有 Knowledge Base 文件的换行提示，无 whitespace error |

Unity 6 隔离烟测汇总位于：

`C:/Users/89733/.codex/visualizations/2026/08/26/01a03d16-463c-7942-8b10-e06e0ecb606e/unity6-strategic-cell-review/validation-escalated/unity-PlayMode-20260826-170826-517.summary.json`

## 视觉说明

本轮额外基于既有 Style D Game View 制作了一张 AI 概念预演图，用于确认“深褐方格 + 金色选中 + 青色悬停”的方向。该图保存在 `Concept/EXPLICIT_STRATEGIC_CELL_MAP_V1_CONCEPT_PREVIEW_NOT_RUNTIME.png`；它不是 Unity 输出、不是运行时证据、不会登记为 Golden，也不进入 `Screenshots/` 运行时证据目录。

## 解除阻塞后的固定命令

在安装并激活 Unity 2022.3.62f3c1 后，依次运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityTestsSafe.ps1 -Mode EditModeTests -TestFilter Mandate.Tests.ExplicitStrategicCellMapV1Tests -TimeoutSeconds 300
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityTestsSafe.ps1 -Mode PlayModeTests -TestFilter Mandate.Tests.ExplicitStrategicCellMapV1PlayModeTests -TimeoutSeconds 300 -UseGraphics
```

只有得到非空 XML、三张截图存在且人工审图通过后，才可升级为 `HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1_READY_FOR_USER_REVIEW`。
