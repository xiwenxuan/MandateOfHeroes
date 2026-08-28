# 洛阳正式玩家人物移动与世界结算 V1 验收报告

## 1. 环境与冻结点

- 验收日期：2026-08-28
- 分支：`codex/m23-p4-quality-artisan-growth`
- Unity：`2022.3.62f3c1 (1623fc0bbb97)`
- 正式实现提交：`07e24fdeda6e`（`feat: add formal Luoyang player movement settlement`）
- 测试与分类超时工具提交：`2b5f82b01224`（`test: cover Luoyang movement and classify slow regressions`）
- 验收依据：本报告、任务书、冻结完整核心回归、受控 Unity 结果和证据目录。

## 2. 已实现范围

- V76 正式玩家人物引用、本地 Cell/Facility、体力、口粮、道路状态和移动分段进度。
- 持久移动命令、事件、通行状态感知路线、时间/体力/口粮成本和世界时间结算。
- 城门、桥梁、道路损毁/修复对规划与下一 Segment 的阻断和中断。
- Segment 边界保存、读档、继续和 V75→V76 顺序迁移。
- Unity 正式绑定、点击请求、世界先结算再播放已提交路线。
- 相同 V76 快照和命令序列的确定性重放。

## 3. 未实现与明确越界项

- 未实现全城 NavMesh、室内导航、NPC 群体移动、RVO/拥堵和全国人物寻路。
- 未制作最终人物 FBX、动作、建筑资产或外围供应区。
- 未把逐帧坐标、表现路线或 Unity 对象写回核心世界事实。
- 未在本任务修改可选生活证据刷新暴露的既有食品守恒差额。

## 4. 测试结果

| 门禁 | 结果 | 证据 |
| --- | ---: | --- |
| 全工程编译 | PASS | `tmp/skill-verification` 编译汇总 |
| 新增目标核心测试 | 11/11 PASS | `core-tests-20260828-214418-806.out.log` |
| 冻结完整核心分组回归 | 747/747 PASS | `tmp/core-test-groups/luoyang-movement-v1-final/aggregate.json` |
| Unity ProjectLoad | PASS | `unity-ProjectLoadSmoke-20260828-222312-445.summary.json` |
| Unity EditMode 目标测试 | 11/11 PASS | `unity-EditMode-20260828-222343-406.summary.json` |
| Unity 图形 PlayMode | 4/4 PASS | `unity-PlayMode-20260828-222519-263.summary.json` |
| 三次确定性重放 | PASS | 三次哈希完全相同 |
| `git diff --check` | PASS | 无空白错误 |

冻结完整核心回归的源码指纹为
`CF640144C8062024947FBCF3501E65973B07EBAD4A3479E6EE92D7B03699DBBB`。

## 5. 超时分类

测试上限不是整体放宽。只有以下两个精确命名的多年确定性用例归为 `slow-determinism`，使用 900 秒上限：

- `FoodRuntime_FormalWorldIsDeterministicForOneYear`：PASS，约 503 秒。
- `Simulation_SaveResumeMatchesContinuousRun`：PASS，约 502 秒。

其余 745 个核心测试均归为 `regular` 并保留 300 秒上限；Unity 门禁同样保留 300 秒上限。分类名、测试名
与实际生效的上限均写入分组结果，禁止根据一次超时把任意测试自动升级为慢测试。

## 6. 回归与独立诊断

- 必需门禁中的既有失败：0。
- 本次改动引入失败：0。
- 冻结完整核心结果：747 通过、0 失败。
- 一次显式开启的可选生活证据刷新在标准门禁之外暴露食品守恒差额：左侧 `475173375`，右侧
  `487898292`，差额 `12724917`。该问题不由本次人物移动引入，生成物位于忽略目录 `outputs/`，未提交，
  并留待对应经济/证据任务单独诊断。

## 7. 确定性重放

三次相同初始状态和命令序列均得到：

`10d273927467e01e167e1aa97e9e0ec99b12cc738c8c53de81ea6cc0792e1b27`

正式 Location、世界时间、体力、口粮、道路/门桥事实和最终世界状态一致。

## 8. 后续固定顺序

下一任务固定为“洛阳人物尺度近景地图与局部导航 V1”。本验收不授权自动扩充人物移动、建筑资产、
NPC 群体寻路或外围供应区。

## Final Decision

ACCEPTED
