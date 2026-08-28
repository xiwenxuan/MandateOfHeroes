# 汉世界全国战略格 LOD V1

本目录是 `HAN-WORLD-NATIONWIDE-STRATEGIC-CELL-GRID-LOD-V1` 的实现与验证入口。

交付文件：

1. [实施报告](NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1_IMPLEMENTATION_REPORT.md)
2. [机器验证汇总](validation_summary.json)
3. [全国总览概念预演图（非运行时）](Concept/NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1_CONCEPT_NOT_RUNTIME.png)

核心口径：

- 全国事实覆盖：7,211,264 个既有 2000m Global Cell；
- 世界总览：32×32 Cell 视觉步长，约 64km，引导格一个合批对象；
- 地区视图：1×1 精确 Cell，24×24 可见窗口，两个合批对象；
- 32×32 不是新的世界、行政、地形或模拟聚合语义；
- 不改 Cell ID、八邻接、Global Origin 或存档。

预定运行时截图：`Screenshots/00_NATIONWIDE_STRATEGIC_CELL_LOD.png`。

当前状态：`NATIONWIDE_GRID_IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。
