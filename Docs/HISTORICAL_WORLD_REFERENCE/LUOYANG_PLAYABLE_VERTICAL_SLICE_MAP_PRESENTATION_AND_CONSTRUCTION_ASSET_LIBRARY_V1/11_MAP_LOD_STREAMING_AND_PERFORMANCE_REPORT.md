# 洛阳地图 LOD、Streaming 与性能报告

- 世界事实仍是40万PermanentPerson、80,899 Household、2,084开局Facility和正式Cell；表现层不复制这些集合。
- LOD0 WORLD、LOD1 REGION、LOD2 CITY、LOD3 CLOSE只改变Facility/Actor/Shipment/Crop表现预算，不改变Runtime ID。
- CITY默认48个真实Actor，CLOSE默认96个；绝不创建40万个GameObject。
- Golden Slice默认最多72个Facility Anchor、6个Shipment Representation和12个Crop批次标记。
- 连续River/Road使用带RuntimeBindingId的表现Spline；Spline点不参与Simulation寻路、产权或空间权威。
- Golden Slice背景为可替换的原创表现资源；全国GIS绝对坐标和CellId不变。
- 正式全国Chunk Terrain Mesh、Addressable/Chunk卸载及Floating Origin实装留待全洛阳扩展阶段。本轮接口和预算已经冻结，但不得声称全国Streaming完成。
