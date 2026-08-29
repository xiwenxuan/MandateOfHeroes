# 洛阳人物尺度 Cell 四向通行、正式移动与近景表现 V1：验收报告

## 1. 交付身份

```text
Task: TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1
Branch: codex/m23-p4-quality-artisan-growth
Baseline: a641382
Unity Version: 2022.3.62f3c1
Save Version: V77 (unchanged)
Formal Acceptance: ACCEPTED
```

## 2. 交付结果

- 5,980 个正式 Cell 全部取得固定四向端口与内部拓扑；
- 2,084 个 Facility 全部取得人物移动能力、Access 规则和目的地入口；
- 359 个 Road、18 个 Gate-type、2 个 Bridge 从正式数据派生，没有硬编码为长期常量；
- `CellTraversalPlanner` 只接受四向、互为反向且能力兼容的端口；
- 建筑可作为目标进入，但不会成为穿越捷径；
- 正式道路、门桥和设施状态在路段执行前重验；
- `MovePersonCommand`、世界时间、体力、口粮、位置、存档和重放仍使用同一本世界账；
- Unity 从 CellRoute 展开人物尺度路线，LocalNav 不再是跨 Cell 正式移动权威；
- Save Schema 保持 V77，旧 V77 路段兼容，不虚构迁移事实。

## 3. 验证结果

| 验证 | 结果 | 正式证据 |
|---|---|---|
| 全工程编译 | PASS | `scripts/verify-project.ps1` 编译阶段通过 |
| CellTraversal 专项核心 | 8/8 PASS | 四向、拓扑、建筑入口、门桥、能力、道路偏好、数据审计、性能 |
| 洛阳局部移动专项核心 | 17/17 PASS | 正式移动、跨 Cell、门桥、存档和重放 |
| 完整核心回归 | 774/774 PASS | `tmp/core-test-groups/luoyang-cell-traversal-v1-20260829/aggregate.json` |
| 源指纹 | PASS | `374F2AE5A158AA50B2347BE23D19D6DFE96DA43F0FA6ED62BE07BEAA43BEBF4D` |
| Unity EditMode | 3/3 PASS | `tmp/unity-validation/unity-EditMode-20260829-104723-335.xml` |
| Unity PlayMode | 1/1 PASS | `tmp/unity-validation/unity-PlayMode-20260829-104753-536.xml` |
| 性能 | PASS | 5,980 Profile：60 ms、GameObject 0；Unity 3×3：加载92 ms、更新装入0 ms、卸载1 ms、19对象/9 Mesh/9 Collider |
| `git diff --check` | PASS | 无空白错误 |
| Introduced Regression | 0 | 编译、核心和 Unity 均无新增失败 |

两个历史多年确定性慢测按已有明确分类使用 900 秒专属上限并通过；其余核心与 Unity 测试继续使用
300 秒门禁。完整核心回归使用 12 个固定指纹分组执行并聚合，不以单个无限时长进程绕过门禁。

## 4. 数据与访问审计

```text
Cell Profile: 5980 / 5980
Facility Profile: 2084 / 2084
Road: 359
Gate-type: 18
Bridge: 2
RoadRequired: 18
Invalid Port/Profile: 0
```

没有道路正面的 37 个商业仓库、10 个仓储仓库、28 个公共官仓和 1 个仓储官仓保持
`Optional`；已有道路正面的 7 个仓储仓库、4 个公共官仓和 7 个坞堡使用 `RoadRequired`。
详细审计见
[`Evidence/LuoyangCellTraversalV1/existing-spatial-audit.md`](Evidence/LuoyangCellTraversalV1/existing-spatial-audit.md)。

## 5. 最终结论

```text
ACCEPTED
```

固定下一步为食品库存守恒差额 RCA 与修复；完成后再进入洛阳外围供应区与城市物流 V1。本任务不
继续扩充第二套局部图、建筑内部导航或全国人物级寻路。
