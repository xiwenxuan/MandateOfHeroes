# 东汉全国自然地图视觉表现 V2 正式报告

## 结论

最终状态：`HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_PLAYABLE_WITH_ART_LIMITS`。

V2 已把 V1 的技术验证画面升级为同一权威世界上的全国—河南尹连续自然地表。全国视角可以辨认海陆、青藏高地、主要山系、平原与水系；区域视角可以辨认山谷、河道、河岸、森林密度和地表变化。Cell Grid、行政色块和旧背景图均不参与正式画面。

## 权威空间不变量

- CRS：`hanworld.albers.china.v0`。
- Global Origin：`(-3417344.395965772, 6199580.451937504)`，含义为 `GLOBAL_GRID_NORTHWEST_CORNER`。
- Global Grid：3314 列×2176 行，共 7,211,264 个 2000m Cell。
- 行方向：north_to_south；列方向：west_to_east。
- Terrain Tile：8×8 Global Cell（16km），正式冻结。
- 24×24 Cell 只仍是临时流式参数，不是世界事实。
- 河南尹 Region：58,368 个既有 Global Cell；没有新建、切割或重编号 Cell。

本轮还修正了 `GlobalSpatialFoundationV1/global_spatial_foundation.json` 中两个未同步的旧占位字段，使其与已冻结领域合同一致：Terrain Tile 为 8×8；Streaming Unit 为 provisional 24×24。没有修改任何坐标或 ID。

## 实现

### 地形与 LOD

- `WorldTerrainLodController` 从同一 `HanWorldV1/elevation.bin` 生成 WORLD 和 REGION。
- WORLD 为 8 Cell 采样的单一连续网格，避免加载全部高精地形常驻。
- REGION 为 1 Cell 采样的连续 2km 网格。
- 3×3 正式 Terrain Tile 仍驻留并提供碰撞、Cell 精度和未来流式边界，但其重复表面不绘制。此方案消除了中景与近景重叠导致的矩形色块和拓扑裂缝。
- 自定义 Shader 关闭背面剔除，因为权威 Grid 行方向使原始三角绕序与 Unity 默认正面相反；表现层不为修画面而改写权威 Cell 拓扑。

### Surface、河流与森林

- `TerrainSurfaceBlendController` 依据开放 Surface ID 混合主次地表，并以 Global projected coordinate 产生连续变化，跨 Tile 不重新随机。
- `GlobalRiverVisualGenerator` 对正式许可 Polyline 做两轮 Chaikin 平滑，按河流等级、流程位置和稳定相位调宽，生成河岸—水体—河岸连续带。
- `GlobalForestDensitySampler` 从 Cell 密度双线性采样，并叠加全局连续噪声；`GlobalVegetationGenerator` 使用确定性抖动并合并为单网格批次。
- 洛水仍为 `NOT_PROVEN_SOURCE_GAP`，没有根据文字记载伪造 Polyline。

### 相机、格网与背景

- 七个固定相机覆盖全国、华北、河南尹、山地、河流、森林和 Tile 接缝。
- `SetHenanYinTransition` 提供 WORLD→REGION 的确定性缩放切换证据。
- Cell Grid 只由 `SetCellOverlayVisible(true)` 显式开启，默认关闭。
- `UsesLegacyBackground=false`、`UsesAdministrativeOverlay=false`；截图 11 证明自然地表不依赖背景图。

## 验证结果

- 全工程编译：PASS。
- 完整核心回归：PASS，12 组 709/709；证据 `tmp/core-test-groups/han-natural-visual-v2-final-20260816/aggregate.json`。
- 地图定向 EditMode：PASS，12/12；证据 `tmp/unity-validation/unity-EditMode-20260816-053018-838.summary.json`。
- 六年长跑单项：PASS，1/1；证据 `tmp/unity-validation/unity-EditMode-20260816-052640-350.summary.json`。
- 完整 PlayMode：PASS，16/16；证据 `tmp/unity-validation/unity-PlayMode-20260816-053114-354.summary.json`。
- V2 截图专项 PlayMode：最后一次包含于完整 PlayMode，并生成 14 张 Game View PNG。
- 工作簿：PASS，12/12，结构检查、公式错误扫描、PNG 渲染完成。
- `git diff --check`、凭据、本机路径和进程残留检查：见 `validation_summary.json`。

## 已知美术与性能限制

- 树冠为七边程序化锥体，密度已自然化但树种、形态、季相和风动仍未完成。
- 河流中心线和平滑已改善，局部河岸仍有带状/像素阶梯感，缺少洲滩、支汊、水面法线和桥渡表现。
- 2km DEM 在河南尹近景仍可观察到低多边形起伏；下一阶段可补充只读高分辨率视觉采样，但不得改变 Global Cell。
- 全国远景的色彩分级、抗锯齿、雾化和海岸细节仍需美术调色。
- batchmode 报告的 333.333ms frame delta 不代表真实玩家帧率，标记为 `NOT_PROVEN_BATCHMODE_TIMING`；世界生成约 1.526s、区域生成约 0.151s、WORLD→REGION 切换约 0.345s 仅作为当前机器上的受控证据。

## 人工验收门禁

14 张截图目前均为 `CANDIDATE_PENDING_USER_APPROVAL`，不是已批准 Golden。用户确认“自然地图方向正确”前，不进入河南尹高精视觉、洛阳建筑或下一 Region 阶段。
