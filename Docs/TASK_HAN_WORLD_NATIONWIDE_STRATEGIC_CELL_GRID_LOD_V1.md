# HAN-WORLD-NATIONWIDE-STRATEGIC-CELL-GRID-LOD-V1

## 任务定位

用户于 2026-08-26 在确认河南尹显式战略格方案后，进一步明确要求全国格子化。本任务把同一套 2000m Global Cell 表现合同推广到全国，但使用视距 LOD 控制总览密度：世界总览显示 32×32 Cell 的视觉引导格，进入任意地区后显示逐个 1×1 正式 Cell。

## 冻结事实

- 权威格网仍为 `hanworld.square-grid.v1`，3314×2176，共 7,211,264 个稳定 Cell。
- 每个真实 Cell 仍为 2000m 方格，并继续使用既有八邻接、Global Origin、Region 成员关系和存档合同。
- 32×32 只表示世界总览的显示步长，约 64km；不是 Chunk、Region、行政区、模拟聚合单元或新世界身份。
- 缩放和进入地区只改变信息密度与表现，不改变同一 Cell 的事实、邻接或 ID。

## 实施范围

1. WORLD 模式全国 LOD 引导格：32×32 Cell 步长、14,316 个逻辑边段、57,264 个顶点、一个合批渲染对象。
2. REGION/CITY 模式精确格：24×24 可见窗口、576 个真实 Cell、格面与格边两个合批对象。
3. `FocusStrategicCell(WorldMapCellId)`：任何合法全国 Cell 均可成为地区焦点并切换到精确格。
4. 世界总览固定相机与 `strategic-cell.nationwide.overview` 审查入口。
5. 全国覆盖计数、当前显示 LOD、显示步长和渲染对象数量的可测试运行时状态。
6. 全国总览与地区精确格的 EditMode/PlayMode 自动化合同。

## 不在范围内

- 不同时实例化或绘制 7,211,264 个格面，不创建数百万 GameObject。
- 不把 LOD 引导格用于寻路、行政、经济、AI 调度或存档。
- 不改变道路移动代价、攻击范围、建设规则、ZOC 或战争系统。
- 不授权最终 Golden、美术资产量产或商业游戏资产复刻。

## 验收口径

- 世界总览报告全国覆盖 7,211,264 Cell，但不创建七百万项表现列表。
- 世界总览只使用一个引导格渲染对象，且顶点数保持在 16 位索引上限内。
- 任意合法 `WorldMapCellId` 可进入 1×1 精确格模式，中心拾取仍返回同一 ID。
- 切换 LOD 不改变 Domain、Simulation 或 Persistence。
- 编译、核心测试、目标 EditMode、目标 PlayMode、截图和 `git diff --check` 分别报告。

## 当前状态

全国 LOD 与任意 Cell 精确进入代码、测试和静态执行检查已建立；当前为 `NATIONWIDE_GRID_IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。本机缺少项目指定 Unity 2022.3.62f3c1，现有 Unity 6 无有效 Editor 许可证，因此不能声称 Unity 编译、测试或全国 Game View 截图通过。
