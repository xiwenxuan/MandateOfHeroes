# GLOBAL SPATIAL FOUNDATION CONTRACT V1

Status: `GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`

## Canonical chain

`Global CRS → Global Origin → Global Cell Grid → Global Cell → Region Membership`

- CRS: `hanworld.albers.china.v0`; `+proj=aea +lat_1=25 +lat_2=47 +lat_0=0 +lon_0=105 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs +type=crs`.
- Origin: `(-3417344.395965772, 6199580.451937504)`, immutable northwest / upper-left projected boundary.
- Cell: 3314 × 2176, 2000 m, 0-based row-major ID `row * 3314 + column`. IDs 0..7211263 remain unchanged.
- Global Cell Grid is the only authoritative world spatial partition.
- Region is a set of complete Global Cells. `IncludedGlobalCellIds` is its authority; bounds and polygons are derived query/visualization aids.
- Region boundary is derived from member-Cell outer edges. It may be stepped, never cuts a Cell, and never creates seam/border/transition Cells.
- Technical Region is not an AdministrativeRegion; historical administrative polygons remain independent overlays.
- The existing 16 × 16 IDs and 208 × 136 = 28288 groupings remain stable, but their previous `Canonical Global Chunk` meaning is `SUPERSEDED / SEMANTICALLY_RECLASSIFIED`.
- Current 16 × 16 meaning is `SIMULATION_AGGREGATION_BLOCK_16`: a technical spatial/simulation aggregation index, not a world fact, Terrain Tile, or Streaming Unit.
- Terrain Tile and Streaming Unit sizes are both `NOT_YET_FROZEN` and require the next Unity benchmark. Region membership need not align to complete 16 × 16 blocks.
- The old 64 × 64 package block is `STORAGE_COMPRESSION_ONLY`, never a Terrain or Streaming unit.
- Region, aggregation-block, and Unity local coordinates are reversible offsets. They never create Cells or stable identities.
- Terrain Tile, Streaming Unit, Simulation Aggregation Block, and Storage Block may use different sizes; every technical partition must preserve Global Cell IDs.
- Visual anchors may place art inside a Cell but are not SubCells and cannot own land, forces or facilities.
- DEM is sampled in global coordinates. Adjacent producers request the same boundary coordinates.
- Rivers, roads and places retain global IDs and global coordinates; visual segments retain their canonical parent ID.
- Administrative boundaries are overlays and never alter Cell geometry.
- Floating Origin affects Unity X/Z only and has zero world-fact effect.

## Region Cell Boundary and technical-block semantics

- `REGION_CELL_BOUNDARY_CONTRACT = FROZEN`
- `REGION_MODEL = SET_OF_GLOBAL_CELLS`
- `REGION_AUTHORITY = INCLUDED_GLOBAL_CELL_IDS`
- `REGION_BOUNDARY_MODEL = CELL_EDGE_DERIVED`
- `REGION_POLYGON_AUTHORITY = NONE`
- `REGION_CUT_CELL = 0`
- `TECHNICAL_REGION_EQUALS_ADMINISTRATIVE_REGION = FALSE`
- `16X16_STATUS = TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK`
- `16X16_BLOCK_COUNT = 28288`
- `16X16_IS_WORLD_FACT = FALSE`
- `16X16_TERRAIN_TILE_FROZEN = FALSE`
- `16X16_STREAMING_UNIT_FROZEN = FALSE`
- `TERRAIN_TILE_SIZE = NOT_YET_FROZEN`
- `STREAMING_UNIT_SIZE = NOT_YET_FROZEN`
- `64X64_STATUS = STORAGE_COMPRESSION_ONLY`

`IncludedGlobalChunkIds` is retained only as a `DERIVED_TECHNICAL_INDEX`. It cannot define Region
membership. Region visual polygons are `DERIVED_FROM_CELL_MEMBERSHIP` or `REFERENCE_ONLY` and never flow
back into Cell membership. Rivers, roads, Places, and Facilities preserve their single global identity across
Region boundaries.

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


## Forbidden

No regional grid, Cell renumbering, Region Cell cutting, authoritative Region polygon, seam/border Cell,
Facility SubCell, moving geospatial origin, administrative Cell geometry, unbenchmarked Terrain/Streaming size,
or background image as world fact.

## Style D V2 表现分辨率增补（2026-08-16）

- `GLOBAL_CELL_RESOLUTION_IS_VISUAL_TERRAIN_RESOLUTION = FALSE`。
- Global Cell仍为2000m、`3314 × 2176`、共`7,211,264`个永久空间身份；Global Origin与Row/Column方向完全不变。
- WORLD、REGION、CITY、CLOSE_PREVIEW可分别使用1×、2×、4×、8×表现顶点密度。
- 新增顶点只允许插值权威高程与稳定Surface，并叠加全局坐标确定性的视觉微起伏；不得形成SubCell、产权单位、寻路事实、资源事实或存档身份。
- 相邻Tile共享边必须由同一全局坐标公式生成；Floating Origin只改变Unity局部坐标。
- 正式实现合同：`presentation.han-world.visual-terrain-detail.v2`。
