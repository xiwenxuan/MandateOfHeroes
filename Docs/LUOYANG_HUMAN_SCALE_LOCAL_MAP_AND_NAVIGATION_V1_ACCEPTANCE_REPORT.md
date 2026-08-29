# 洛阳人物尺度近景地图与局部导航 V1：验收报告

## 1. 交付身份

```text
Task: TASK_LUOYANG_HUMAN_SCALE_LOCAL_MAP_AND_NAVIGATION_V1
Branch: codex/m23-p4-quality-artisan-growth
Implementation Commit: 1b05faf7815ddf200fe1df3bde4b0fe2da899c91
Unity Version: 2022.3.62f3c1
Save Version: V77
Map Version: luoyang.local-map.master.v1
Formal Acceptance: ACCEPTED (presentation/local-detail scope)
```

## 2. 范围收敛说明

本任务交付的 Strategic/Local 坐标、5,980 个 LocalSpace、2,084 个 Facility Anchor/Footprint、
入口、道路/门桥几何、3×3 Streaming 和 V77 局部位置合同均保留并通过 Unity 复验。

后续
[`TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1.md`](TASK_LUOYANG_CELL_TRAVERSAL_PORT_AND_HUMAN_SCALE_MOVEMENT_V1.md)
已将跨 Cell 正式路线权威收敛到 `Cell + 四向 Traversal Port + CellRoute`。因此本报告对 LocalNav
的 ACCEPTED 只表示近景几何、入口、Streaming 和旧 V77 路段兼容，不再表示 LocalNav 图拥有正式
跨 Cell 路线选择权。

## 3. 稳定规模

```text
Facility Count: 2084
Spatial Anchor Count: 2084
LocalSpace Count: 5980
Legacy/Presentation Navigation Node Count: 1959
Legacy/Presentation Navigation Edge Count: 1976
Cell Transition Count: 4920
Gate-type Facility Count: 18
Bridge Count: 2
Map SHA-256: 894004b3bd1b09acba46c753e09efdd0d6b91303b2ab1489e44857c1da8f2b18
```

## 4. 最终验收门禁

| Gate | 结果 | 说明 |
|---|---|---|
| 空间架构继承 | PASS | 复用 V68/M26-P5B；LocalSpace 非 SubCell；无第二套世界实体 |
| Facility / Anchor | PASS | 2,084/2,084 Facility 取得稳定 Anchor、Footprint 与入口 |
| 门桥与正式状态 | PASS | 18 个 Gate-type、2 个 Bridge 全映射并读取正式世界状态 |
| 正式人物移动 | PASS | 同一 Person、MovePersonCommand、时间、体力、口粮、位置与存档 |
| Streaming | PASS | 3×3 装卸不改变 Person、Facility、Inventory 或世界 Hash |
| Unity EditMode | 3/3 PASS | CellRoute 端口锚点展开、Streaming 和非固定 2km 成本 |
| Unity PlayMode | 1/1 PASS | 图形近景表现与 Streaming 冒烟通过 |
| 完整核心回归 | 774/774 PASS | 固定指纹 12 组聚合，失败 0 |
| 性能边界 | PASS | 3×3加载92 ms、更新装入0 ms、卸载1 ms、19对象/9 Mesh/9 Collider；Cell Profile构建60 ms、GameObject 0 |
| 差异检查 | PASS | `git diff --check` 无错误 |

Unity 环境启动问题现已解除。正式结果文件：

- `tmp/unity-validation/unity-EditMode-20260829-104723-335.xml`
- `tmp/unity-validation/unity-PlayMode-20260829-104753-536.xml`

## 5. 最终结论

```text
ACCEPTED
```

旧报告中的 `blocked/125` 是历史环境门禁，不再是当前状态。LocalSpace 与 LocalNav 有价值的数据继续
服务近景表现和旧存档兼容；当前正式移动权威及后续改动入口以 Cell Traversal 任务为准。
