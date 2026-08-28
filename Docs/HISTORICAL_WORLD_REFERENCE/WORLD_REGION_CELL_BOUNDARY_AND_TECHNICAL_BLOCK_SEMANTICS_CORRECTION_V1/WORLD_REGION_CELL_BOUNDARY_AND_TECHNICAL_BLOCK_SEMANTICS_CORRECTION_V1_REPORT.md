# WORLD REGION CELL BOUNDARY AND TECHNICAL BLOCK SEMANTICS CORRECTION V1 REPORT

## Outcome

`REGION_CELL_BOUNDARY_CONTRACT = FROZEN`

全国唯一空间事实仍止于 Global Cell。Region 的唯一权威范围是完整 Global Cell 的成员集合；边界由成员
Cell 外边派生。旧 16×16 ID、数量和读取行为全部保留，但语义从 Canonical Terrain/Streaming Chunk
纠正为技术 Spatial / Simulation Aggregation Block。

## Final state

- `GLOBAL_ORIGIN = UNCHANGED`
- `GLOBAL_CELL_GRID = UNCHANGED`
- `GLOBAL_CELL_IDS = UNCHANGED`
- `REGION_MODEL = SET_OF_GLOBAL_CELLS`
- `REGION_BOUNDARY_MODEL = CELL_EDGE_DERIVED`
- `REGION_POLYGON_AUTHORITY = NONE`
- `HENAN_YIN_INCLUDED_CELL_COUNT = 58368`
- `HENAN_YIN_BOUNDARY_EDGE_COUNT = 992`
- `16X16_STATUS = TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK`
- `16X16_BLOCK_COUNT = 28288`
- `TERRAIN_TILE_SIZE = NOT_YET_FROZEN`
- `STREAMING_UNIT_SIZE = NOT_YET_FROZEN`
- `64X64_STATUS = STORAGE_COMPRESSION_ONLY`

## Required answers

1. Global Origin是否变化？否。
2. Global Grid是否变化？否。
3. 7,211,264 Permanent Cell是否变化？否。
4. CellPermanentId是否重新编号？0个。
5. 河南尹是否重新生成Cell？否。
6. 河南尹是否仍包含58,368个Global Cell？是。
7. 河南尹Region Local Origin是否变化？否。
8. Region权威范围由什么决定？IncludedGlobalCellIds。
9. Region Boundary如何产生？成员Cell与非成员Cell之间的外侧公共Cell Edge。
10. Region是否允许切Cell？不允许。
11. Region是否需要第二套权威连续Polygon？不需要。
12. Historical Administrative Boundary是否等于Technical Region？不等于。
13. 16×16当前正式含义？技术Spatial / Simulation Aggregation Block。
14. 16×16是否已经是Terrain Tile？否。
15. 16×16是否已经是Streaming Unit？否。
16. Terrain Tile尺寸是否已确定？否。
17. Streaming Unit尺寸是否已确定？否。
18. 64×64是什么？Storage / Compression Block。
19. Region是否必须按完整16×16 Block划分？否。
20. 相邻Region如何连接？Global Cell Neighbor自然连接。
21. 是否建立新的Region Seam / Border Cell？否。
22. 洛阳400,000 PermanentPerson是否变化？否。
23. 洛阳80,899 Household是否变化？否。
24. 洛阳2,084 Facility是否变化？否。
25. 洛阳5,740 Buildable Cell是否变化？否。
26. Global→河南尹Local→Global误差是多少？0.0m。
27. 本任务是否生成正式Terrain？否。

## Machine evidence

- Membership SHA-256: `28d5c805a558fa7b07d47e40f98d7ab314b03ef39f63615106b569bb0db38728`
- Boundary edges: 992; distinct across-boundary Global Cells: 992.
- Current named adjacent Region count: 0. This means the catalog currently contains only the first production Region;
  it does not invent a Region identity for neighboring Global Cells.
- Derived 16×16 compatibility indices referenced by 河南尹: 228; authority: `DERIVED_TECHNICAL_INDEX`.
- Global/河南尹 Local round trip error: 0.0m.
- Protected 洛阳: 400000 Persons / 80899 Households /
  2084 Facilities / 5740 Buildable Cells.

## Historical decision handling

The preceding report remains a historical record of the original `16×16 Canonical Global Chunk` decision.
Its current status is `SUPERSEDED / SEMANTICALLY_RECLASSIFIED`; no historical file or stable block ID was deleted.

## Final verification

- Full project compile: `PASSED`.
- Complete core regression: `705 / 705 PASSED`, run ID `region-boundary-semantics-24g-20260815`.
- Unity EditMode `Mandate.Tests.GlobalSpatialFoundationV1Tests`: `3 / 3 PASSED`.
- Unity EditMode `Mandate.Tests.WorldMapPipelineTests`: `6 / 6 PASSED`.
- Seven formal workbooks: formula-error scan `0` matches; rendered previews visually inspected.
- PlayMode: not run because this task did not change Presentation or PlayMode runtime behavior.
- Luoyang T4 / Golden smoke: not run because this task did not change T4 or Golden runtime behavior;
  protected Luoyang counts were revalidated directly from the frozen data contract.

## Next gate

Only after this contract is frozen may development enter
`MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1` using 4×4, 8×8 and 16×16 candidates without presuming
Terrain Tile and Streaming Unit use the same size.
