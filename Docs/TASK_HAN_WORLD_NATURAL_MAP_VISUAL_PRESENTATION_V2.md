# HAN-WORLD-NATURAL-MAP-VISUAL-PRESENTATION-V2 任务书

## 1. 目标

在不改变全国唯一 Global Origin、3314×2176 Global Cell、2000m Cell、CellPermanentId、河南尹 Region 成员和 8×8 Terrain Tile 正式决定的前提下，把 V1 技术底图升级为可从 Unity Game View 验收的全国—区域连续自然地图。

最终状态采用：

`HAN_WORLD_NATURAL_MAP_VISUAL_PRESENTATION_V2_PLAYABLE_WITH_ART_LIMITS`

原因不是工程未贯通，而是树冠、河岸、材质细节与远景抗锯齿仍属于程序化原型美术，不能宣称最终商业美术完成。

## 2. 实施范围

- 同一 DEM 派生 WORLD 与 REGION 地形，不建立第二套地理事实。
- WORLD 使用 8 Cell 采样步长的单一连续网格；REGION 使用 1 Cell（2000m）步长的单一连续显示网格。
- 3×3 个正式 8×8 Terrain Tile 继续驻留并提供碰撞/流式单元，但不重复绘制同一表面，避免矩形覆盖和裂缝。
- Surface 使用稳定命名空间 ID、主次材质混合、全局坐标连续噪声和坡向光照。
- 河流使用平滑中心线、按等级和纵向变化的宽度、独立河岸/水体带并贴合地形。
- 森林使用连续密度场、确定性抖动和合并网格；不逐树建立 GameObject。
- Cell Grid 为独立 Debug 层，正式画面默认关闭。
- 固定七个验收相机和 WORLD→河南尹连续缩放证据。
- 输出 14 张 Game View 截图、12 份工作簿、正式报告、视觉报告和机器摘要。

## 3. 禁止事项

- 不修改 Global Origin、Global Cell Grid、Cell 编号或河南尹成员。
- 不建立 SubCell 或第二套 Terrain 地理事实。
- 不以背景图、行政色块、Cell 色块冒充自然地貌。
- 不伪造洛水几何；无可靠兼容来源时保持 `NOT_PROVEN_SOURCE_GAP`。
- 不自动提交或推送。

## 4. 验收门

| 门 | 要求 | 结果 |
|---|---|---|
| 工程 | 全工程编译 | PASS |
| 核心 | 不可变指纹分组完整回归 | PASS，709/709 |
| 地图 EditMode | Global Spatial、V1 Basemap、V2 Presentation | PASS，12/12 |
| PlayMode | 完整套件与截图生成 | PASS，16/16 |
| 视觉 | 全国、河南尹、地形、河流、森林、Surface、Tile、Grid、Background、Transition | PLAYABLE_WITH_ART_LIMITS |
| 工作簿 | 12 份可打开、已检查并渲染 | PASS |
| 人工门禁 | 用户确认自然地图方向 | PENDING_USER_APPROVAL |

全量 807 项 EditMode 单进程曾在 300 秒超时；24 组尝试中第 6 组把六年长跑与 33 项组合后超时。该六年用例单独运行 1/1 通过，本任务要求的地图 EditMode 12/12 通过。此环境限制不被写成测试失败或全量通过。

## 5. 完成条件

代码、数据、截图、工作簿、报告和验证证据齐备后停止，不进入河南尹下一阶段高细节建设；先交由用户检查核心截图并确认方向。
