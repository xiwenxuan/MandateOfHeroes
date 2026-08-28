# HAN-WORLD-STYLE-D-STRATEGIC-LANDSCAPE-VISUAL-REFINEMENT-AND-ZHONGHUA-SOURCE-RECOVERY-V2

## 任务定位

本任务在不改变统一世界空间事实的前提下，将 Style D 从 V1 候选推进为可供用户审图的战略山河 V2 原型，并按有限重试流程恢复《中华三国志》候选源码研究。源码无法取得时必须保留阻断证据，禁止把 API 元数据写成已经本地确认。

## 冻结边界

- 唯一 CRS：`hanworld.albers.china.v0`。
- Global Origin：`(-3417344.395965772, 6199580.451937504)`。
- 正式母格网：`3314 × 2176`，共 `7,211,264` 个永久 Global Cell。
- 正式 Cell 边长：`2000m`；Terrain Tile：`8 × 8 Cell`。
- `Global Cell resolution != visual terrain resolution`。
- REGION、CITY 和近景增加的顶点只属于 Presentation，不得建立 SubCell、重编号 Cell 或回写世界事实。
- 不复制外部代码、贴图、地图、模型或字体；许可证未确认前不得集成候选项目。

## 实施范围

1. Style D V1 差距审计。
2. 河流自适应采样、限幅 Miter/Bevel Join、统一河岸采样与地形贴合。
3. 森林 WORLD / REGION / CITY 三层 LOD。
4. REGION 2倍、CITY 4倍、近景 8倍的表现层地形细化。
5. 山系、谷地、平原和战略可读性调色。
6. 固定相机、15张核心 Game View 截图、性能证据和自动回归。
7. 中华三国志候选仓库有限克隆重试、网络诊断和许可证状态审计。
8. 15份工作簿、4份正式报告、Canonical 文档、Knowledge Base 与 Registry 同步。

## 停止条件

- 本轮状态只允许到 `STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW`。
- 用户审图前不进行全国 Style D 生产化、不做河南尹全量高精生产、不建设洛阳城墙/宫城/建筑资产。
- 任何视觉缺陷必须记为 `PARTIAL / FAIL / NOT_PROVEN`，不得用机器测试替代人工视觉结论。
- 外部工具硬超时 300 秒；不得无限重试或后台遗留进程。

## 验收口径

- 全工程编译、核心回归、相关 EditMode、Style D PlayMode、`git diff --check` 分别报告。
- 15张核心截图名称固定；V1截图不得重渲染美化。
- 工作簿必须可打开、已渲染检查且无公式错误。
- 源码获取状态必须同时报告本地文件、网络、许可证和是否复制外部资产。

## 当前结论

实现、证据与文档已进入用户审图门禁；河流源线段端点接缝、汇流网格、城市近景低频块状感和连续 LOD morph 仍为 `PARTIAL`。外部候选源码因 GitHub 443 网络阻断未取得，许可证仍未解决。

## 后续兼容说明（2026-08-26）

用户随后接受 `TASK_HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1.md` 的显式战术格方向。因此，本任务中“干净自然画面默认不显示格线”的历史门禁仍适用于旧 V2 截图，但不再禁止后续战术格视图显示既有 Global Cell。新任务仍保持 2000m 方格、八邻接和无 SubCell，不回写或改写本任务证据。
