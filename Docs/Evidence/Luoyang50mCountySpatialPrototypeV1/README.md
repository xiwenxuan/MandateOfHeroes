# 洛阳50m县域真实规模原型 V1 证据索引

## 自动生成证据

- `01_terrain_water_road.png`：HanWorldV1地形、正式2km道路展开、正式沟渠Facility派生候选水网、2,084个候选Facility点和4个Portal。
- `02_facility_districts.png`：既有WholeCityComposition六分区颜色。
- `03_source_spatial_precision.png`：绿色=源Cell精度，黄色=Probable，橙色=Approximate；颜色描述旧资料精度，不批准候选50m位置。
- `04_layout_network_closure.png`：布局包道路、水渠、城防、Portal与六区凸包审阅图；待Unity启动门恢复后由PlayMode自动生成。
- `performance-unity.json`：真实204,800格构建、装载和单Renderer表现测量。

## 解释边界

上述图是容量与迁移候选图，不是最终洛阳美术或历史精确城市地图。选定8×16个
HanWorldV1父级战略Cell中，正式`water.bin`水格数为0；蓝色局部水网只由19个既有
`facility.public.canal`设施的相邻关系确定性派生，并独立计数。2座Bridge和16口Well
仍作为正式Facility迁移，但本任务没有凭空制造黄河、洛水或历史河道。

旧设施范围为92×65个2km Cell，候选县域只有16×8个2km父Tile。因此只有1项候选位置
恰好保留原父Tile，另2,083项均标记为`gameplay-reconstruction.provisional`。旧ID、
Definition、Model、CellId64和空间精度完整保留，候选位置没有写回正式数据或存档。
