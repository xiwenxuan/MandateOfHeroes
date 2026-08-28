# 洛阳资源与农业设施生产化 V1 证据说明

本目录保存任务`LUOYANG-RESOURCE-AND-AGRICULTURE-PRODUCTION-V1`的审图证据。

历史/比较资料：

- [中国国家博物馆：中国古代基本陈列·秦汉](https://www.chnmuseum.cn/portals/0/web/zt/gudai/detail4.html)
- [中国国家博物馆：石田塘](https://www.chnmuseum.cn/zp/zpml/kgfjp/202110/t20211028_251927.shtml)
- [中国国家博物馆：收获渔猎画像砖](https://www.chnmuseum.cn/zp/zpml/kgfjp/202110/t20211028_251929.shtml)
- [中国国家博物馆：冶铁画像石](https://www.chnmuseum.cn/zp/zpml/kgfjp/202208/t20220829_257114.shtml)
- 项目任务`Docs/TASK_M23_P2_UPSTREAM_RESOURCE_EXTRACTION_AND_PRIMARY_PROCESSING.md`

边界：26个Cell来自当前开局权威数据，但全部为`GameplayReconstruction + Approximate +
GeneratedForTest + C`，没有逐项史料来源。截图只能证明程序化视觉、正式Cell覆盖、LOD与清理行为，
不能证明洛阳考古位置、资源储量、生产结算或灌溉模拟。

预期截图：

1. `01_ALL_26_RESOURCE_AGRICULTURE_ACTUAL_CELLS.png`
2. `02_FORESTRY_MINE_QUARRY_PRODUCTION_LINE.png`
3. `03_SOUTHERN_QUARRY_TERRACES.png`
4. `04_SIX_BUNDED_RICE_FIELDS.png`

验收结果（2026-08-27）：相关核心1/1、目标EditMode 3/3、图形PlayMode 1/1、全城批处理
EditMode 3/3与图形PlayMode 1/1通过。四张截图均为1600×1000实际Game View；当前最密549设施
窗口为1,673个LOD2源模块、95个Renderer、18,148个顶点、27.412ms构建，预算通过。
