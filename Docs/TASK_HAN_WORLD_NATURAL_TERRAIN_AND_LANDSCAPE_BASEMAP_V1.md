# HAN-WORLD-NATURAL-TERRAIN-AND-LANDSCAPE-BASEMAP-V1 任务书

## 任务目标

在冻结的 `HanWorldV1` 全国空间基础上，建立不依赖旧背景图、可由 Unity 直接渲染的连续自然世界底图。该底图必须复用唯一 Global Cell 网格、真实 DEM、许可兼容的河流资料，并为后续河南尹高精地图和洛阳城市建设提供共同地表。

## 冻结边界

- CRS：`hanworld.albers.china.v0`。
- Global Origin：`(-3417344.395965772, 6199580.451937504)`，含义为全国母格网西北角。
- Global Cell：3314×2176、2km、7,211,264 个，行向南递增、列向东递增，永久 ID 不变。
- Region 仍为 `Set<GlobalCellId>`；不得建立 Region Cell、切割 Cell 或第二套世界坐标。
- 16×16 只表示模拟聚合；64×64 只表示现有二进制压缩。
- 史实、现代自然资料、合理建模和表现增强必须分层标记。

## 实施范围

1. 审计全国 DEM、河流、湖泊、许可证、分辨率、NoData 和历史适用边界。
2. 使用真实 DEM 对 4×4、8×8、16×16 Terrain Tile，在平原、山地、河流和洛阳样区执行 3×3/5×5 驻留实测。
3. 冻结 8×8 Global Cell（16km）Terrain Tile；24×24 Cell Streaming Unit 仅为可替换的 V1 暂定值。
4. 实现 `NaturalSurfaceClassifier`、`TerrainTileIndex`、`HanWorldTerrainGenerator`、`TerrainCellBinding`、`GlobalRiverVisualGenerator` 和 `GlobalVegetationGenerator`。
5. 建立 `HanWorldNaturalBasemap` 场景，支持 WORLD/REGION 视角、洛阳与河南尹定位、浮动原点、Cell 拾取和调试网格。
6. 生成 12 份正式工作簿、机器摘要、视觉报告、10 张 Unity 截图和可再生成脚本。
7. 执行编译、核心测试、EditMode、PlayMode、差异检查、凭据与本机路径扫描。

## 验收停止条件

- 任何 Global Cell ID、原点、行列方向或 Region 成员事实改变，立即失败。
- Tile 之间存在非零共享边高程误差，立即失败。
- 旧背景图关闭后地图消失，立即失败。
- 用蓝色 Cell 代替全部河流、一个树一个 GameObject、一个 Cell 一个 GameObject，立即失败。
- 无来源的河流不得为通过验收而虚构；必须登记 `NOT_PROVEN_SOURCE_GAP`。
- 无 XML 或机器摘要不得声称 Unity 测试通过。
- 禁止自动提交和推送。

## 当前执行结论

实现和专项 Unity 验收已完成；完整结论见同目录正式报告与 `validation_summary.json`。全量核心回归已按项目规定分为 12 组执行并最终聚合通过 709/709，本任务状态为 `HAN_WORLD_NATURAL_BASEMAP_V1_COMPLETE`。
