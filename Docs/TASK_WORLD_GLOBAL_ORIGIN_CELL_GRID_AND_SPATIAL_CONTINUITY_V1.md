# WORLD-GLOBAL-ORIGIN-CELL-GRID-AND-SPATIAL-CONTINUITY-V1

状态：`COMPLETED / GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`

目标、禁止事项与验收口径来自用户正式总任务书及“四十六-A 空间起点数值增补”。

## 执行结论

- 全国现有 3314 × 2176、2000m、7,211,264 Cell 格网继续作为唯一世界格网。
- Global Origin 明确为 `(-3417344.395965772, 6199580.451937504)`，含义是规则母格网和 Cell(0,0) 的西北/左上角。
- 河南尹 Local Origin 明确为 `(262655.6040342278, 3511580.451937504)`，含义是生产 Region 的西南角；Local(0,0) 严格等于该点。
- 洛阳 Canonical Anchor、三个固定抽样 Cell、Cell/Region 公式及往返误差均写入正式合同、报告、机器摘要和第 15 号工作簿。
- 现有 Cell ID 不迁移、不重排、不重新随机；最终分类为 `B_REUSABLE_WITH_NON_ID_MIGRATION`。

## 正式交付

- 15 份空间母版与验收工作簿、Canonical 合同、验收报告、`SPATIAL_ORIGIN_SUMMARY.md` 与机器结果：`Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/`
- 运行时合同与河南尹切片：`Assets/StreamingAssets/WorldMap/GlobalSpatialFoundationV1/`
- 领域、模拟、持久化与测试代码：`Assets/Scripts/`、`Assets/Tests/`
- 可重复生成工具：`MapPipeline/scripts/build_global_spatial_foundation_v1.py` 及配套工作簿、Registry 更新器。

下一阶段入口：`HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1`。
