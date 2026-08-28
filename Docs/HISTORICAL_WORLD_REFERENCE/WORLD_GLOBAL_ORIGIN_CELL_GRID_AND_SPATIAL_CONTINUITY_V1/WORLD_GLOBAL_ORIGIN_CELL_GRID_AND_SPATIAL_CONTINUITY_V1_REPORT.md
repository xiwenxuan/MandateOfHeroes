# WORLD GLOBAL ORIGIN CELL GRID AND SPATIAL CONTINUITY V1 REPORT

## Outcome

`GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`

结论：`B_REUSABLE_WITH_NON_ID_MIGRATION`。唯一修正是把旧 64×64 压缩存储块与 16×16 Canonical Global Chunk 分名；不改任何 Cell ID。

## Core answers

1. 唯一 CRS：hanworld.albers.china.v0。
2. Origin：(-3417344.395965772, 6199580.451937504)。
3. 沿用现有体系。
4. CellSize 继续 2000m。
5. 全国 Cell：7211264。
6. 3314×2176 保持。
7. CellPermanentId 全部保留。
8. 未重新编号。
9. Cell Gap=0。
10. Cell Overlap=0。
11. Half-cell Shift=0。
12. Row 向南、Column 向东、均从0开始。
13. 16×16 Canonical Chunk 已冻结。
14. Global Chunk 全国连续。
15. Region 不重新切 Chunk。
16. Region 不生成 Cell。
17. HENAN_YIN_REGION：228 个 Global Chunk、58368 个 Global Cell。
18. Region Origin=(262655.6040342278,3511580.451937504)。
19. Region Local 100% 可逆。
20. Chunk Local 100% 可逆。
21. Visual Local 与 Simulation Cell 分离。
22. 未引入 SubCell。
23. DEM 使用统一 Global Sampling Grid。
24. 相邻 Chunk DEM 共享边误差 0m。
25. 跨 Region 的本轮空间采样连续性通过；最终 Terrain 尚未生产。
26. River 使用 Global 源与同一 Cell Raster。
27. Road RouteId 和 Cell 路径连续。
28. 已定位地点全部有效；未定位地点继续 UNKNOWN，非法映射 0。
29. Model Analysis Point 未升级证据等级。
30. 洛阳 Buildable Cells=5740，保留。
31. 洛阳 Facility=2084，非法 Cell 绑定=0。
32. 发现 64×64 Legacy Storage Block 命名遗留，以转换层隔离。
33. 无 Critical Migration。
34. Floating Origin 不影响世界事实。
35. 旧背景图降级为表现参考。
36. 可以进入河南尹 Terrain 制作。
37. 关中直接引用同一 Global Grid，无需拼图。
38. 成都无需第二套地图。
39. 已形成 Region Spatial Template。
40. ONE WORLD / ONE GLOBAL GRID：是。

## 全国 Global Origin 与母格网实际坐标

- `GLOBAL_CRS_NAME = Han World China-centered Albers Equal Area V0`
- `GLOBAL_CRS_ID = hanworld.albers.china.v0`
- `GLOBAL_ORIGIN_X = -3417344.395965772`
- `GLOBAL_ORIGIN_Y = 6199580.451937504`
- `GLOBAL_ORIGIN_UNIT = meter`
- `GLOBAL_ORIGIN_MEANING = GLOBAL_GRID_NORTHWEST_CORNER`
- `GLOBAL_ORIGIN_CELL_RELATION = Cell(0,0) 左上角 / 西北角，同时也是规则 Grid Envelope 西北角`
- `GLOBAL_ROW_ZERO_DIRECTION = ROW_0_IS_NORTHERNMOST; ROW_INDEX_INCREASES_NORTH_TO_SOUTH`
- `GLOBAL_COLUMN_ZERO_DIRECTION = COLUMN_0_IS_WESTERNMOST; COLUMN_INDEX_INCREASES_WEST_TO_EAST`
- `CELL_SIZE = 2000m`
- `GLOBAL_GRID_COLUMNS = 3314`
- `GLOBAL_GRID_ROWS = 2176`
- `GLOBAL_GRID_FIRST_CELL_ID = cell.hanworld.v0.0`
- `GLOBAL_GRID_FIRST_CELL_ROW = 0`
- `GLOBAL_GRID_FIRST_CELL_COLUMN = 0`
- `GLOBAL_GRID_FIRST_CELL_MIN_X = -3417344.395965772`
- `GLOBAL_GRID_FIRST_CELL_MIN_Y = 6197580.451937504`
- `GLOBAL_GRID_FIRST_CELL_MAX_X = -3415344.395965772`
- `GLOBAL_GRID_FIRST_CELL_MAX_Y = 6199580.451937504`
- `GLOBAL_GRID_FIRST_CELL_CENTER_X = -3416344.395965772`
- `GLOBAL_GRID_FIRST_CELL_CENTER_Y = 6198580.451937504`

Global Origin 不是 Cell(0,0) 左下角。由于行号从北向南增加，它严格对应 Cell(0,0) 的左上角（西北角）。

## 全国规则 Grid Envelope 与 Valid World Extent

- `GLOBAL_GRID_MIN_X = -3417344.395965772`
- `GLOBAL_GRID_MIN_Y = 1847580.451937504`
- `GLOBAL_GRID_MAX_X = 3210655.604034228`
- `GLOBAL_GRID_MAX_Y = 6199580.451937504`
- `GLOBAL_GRID_WIDTH = 6628000.0m = 3314 × 2000m`
- `GLOBAL_GRID_HEIGHT = 4352000.0m = 2176 × 2000m`
- `VALID_WORLD_EXTENT = GLOBAL_GRID_ENVELOPE`
- `VALID_WORLD_MASK = NO_SEPARATE_MASK`

当前每个 Global Cell 都是有效世界 Cell；陆地/水域是语义层，不是删除 Cell 的 Valid Mask。

## 河南尹 Region Local Origin

- `HENAN_YIN_REGION_ID = HENAN_YIN_REGION`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_X = 262655.6040342278`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_Y = 3511580.451937504`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ID = cell.hanworld.v0.4452542`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ROW = 1343`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_COLUMN = 1840`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CORNER = SOUTHWEST_CORNER`
- `HENAN_YIN_REGION_LOCAL_X = 0`
- `HENAN_YIN_REGION_LOCAL_Y = 0`
- `HENAN_YIN_MIN_GLOBAL_ROW = 1152`
- `HENAN_YIN_MAX_GLOBAL_ROW = 1343`
- `HENAN_YIN_MIN_GLOBAL_COLUMN = 1840`
- `HENAN_YIN_MAX_GLOBAL_COLUMN = 2143`
- `HENAN_YIN_BOUNDING_CELL_CAPACITY = 58368`
- `HENAN_YIN_CELL_COUNT = 58368`
- `HENAN_YIN_ADMINISTRATIVE_OVERLAY_CELL_COUNT = 7763`

生产 Region 是规则矩形 Chunk 包络，因此 Bounding Capacity 与 Actual Included Cell Count 都是 58368；行政 Overlay 的 7763 个 Cell 是另一项事实，不能与生产 Region Cell 数混用。

## 洛阳 Canonical Anchor

- `LUOYANG_CANONICAL_PLACE_ID = C027`
- `LUOYANG_GLOBAL_X = 670561.5475446532`
- `LUOYANG_GLOBAL_Y = 3717065.2005044892`
- `LUOYANG_GLOBAL_CELL_ID = cell.hanworld.v0.4114717`
- `LUOYANG_GLOBAL_ROW = 1241`
- `LUOYANG_GLOBAL_COLUMN = 2043`
- `LUOYANG_HENAN_LOCAL_X = 407905.9435104254`
- `LUOYANG_HENAN_LOCAL_Y = 205484.74856698513`
- `LUOYANG_COORDINATE_STATUS = approximate`
- `LUOYANG_CONFIDENCE = medium`

洛阳锚点来自经纬度 (112.45, 34.62) 的投影坐标，是中等置信度近似城市锚点，不表示精确宫城位置，也不建立新的 World Origin。

## 三个固定抽样 Cell

| Sample | CellPermanentId | Row | Column | MinX | MinY | CenterX | CenterY | HenanLocalCenterX | HenanLocalCenterY |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| LUOYANG_URBAN_CANONICAL_ANCHOR_CELL | cell.hanworld.v0.4114717 | 1241 | 2043 | 668655.6040342278 | 3715580.451937504 | 669655.6040342278 | 3716580.451937504 | 407000.0 | 205000.0 |
| LUOYANG_OUTER_SUBURB_CELL | cell.hanworld.v0.4114731 | 1241 | 2057 | 696655.6040342278 | 3715580.451937504 | 697655.6040342278 | 3716580.451937504 | 435000.0 | 205000.0 |
| HENAN_YIN_FAR_OVERLAY_CELL | cell.hanworld.v0.4366390 | 1317 | 1852 | 286655.6040342278 | 3563580.451937504 | 287655.6040342278 | 3564580.451937504 | 25000.0 | 53000.0 |

## 正式公式与机器核验

- `CellCenterX = GlobalOriginX + (GlobalColumn + 0.5) × CellSize`
- `CellCenterY = GlobalOriginY - (GlobalRow + 0.5) × CellSize`
- `RegionLocalX = GlobalX - RegionOriginGlobalX`
- `RegionLocalY = GlobalY - RegionOriginGlobalY`
- `GlobalX = RegionLocalX + RegionOriginGlobalX`
- `GlobalY = RegionLocalY + RegionOriginGlobalY`
- `GLOBAL_GRID_WIDTH_MISMATCH = 0.0m`
- `GLOBAL_GRID_HEIGHT_MISMATCH = 0.0m`
- `SAMPLE_CELL_CENTER_FORMULA_MAX_ERROR = 0.0m`
- `REGION_LOCAL_ROUND_TRIP_MAX_ERROR = 0.0m`


## Machine evidence

- GIS round-trip max error: 2.132e-14 degrees.
- GIS round-trip average error: 1.279e-14 degrees.
- 河南尹行政 Overlay Cell: 7763；生产 Region 是 Chunk 对齐包络。
- River features: 233; river chunk crossings: 4242; breaks: 0.
- Road routes: 18; breaks: 0.
- 洛阳保护事实：400000 Persons / 80899 Households / 2084 Facilities / 5740 Buildable Cells。

下一任务：`HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1`。
