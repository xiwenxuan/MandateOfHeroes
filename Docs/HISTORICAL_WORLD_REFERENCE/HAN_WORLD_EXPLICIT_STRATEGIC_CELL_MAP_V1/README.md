# 汉世界显式战略格地图 V1

本目录是 `HAN-WORLD-EXPLICIT-STRATEGIC-CELL-MAP-V1` 的审查证据入口。

已生成文档：

1. [实施报告](EXPLICIT_STRATEGIC_CELL_MAP_V1_IMPLEMENTATION_REPORT.md)
2. [机器验证汇总](validation_summary.json)
3. [战略格概念预演图（非运行时）](Concept/EXPLICIT_STRATEGIC_CELL_MAP_V1_CONCEPT_PREVIEW_NOT_RUNTIME.png)

当前合同：

- 事实格：既有 2000m `hanworld.square-grid.v1`；
- 审查窗口：河南尹 24×24，共 576 格；
- 表现：贴地格面、格边、悬停与选中；
- 合批：2 个渲染对象；
- 禁止：SubCell、六边形拓扑迁移、存档变化、商业游戏资产复制。

预定截图：

1. `Screenshots/01_HENAN_YIN_24X24_STRATEGIC_CELLS.png`
2. `Screenshots/02_LUOYANG_CELL_SELECTION_CLOSE.png`
3. `Screenshots/03_HENAN_MOUNTAIN_TERRAIN_CONFORMING_CELLS.png`

当前状态：`IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。只有实际生成结果文件并完成人工审图后，才可升级门禁。

后续全国入口：`../HAN_WORLD_NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1/README.md`。全国任务复用这里的 1×1 精确格，不覆盖本目录的河南尹基线。
