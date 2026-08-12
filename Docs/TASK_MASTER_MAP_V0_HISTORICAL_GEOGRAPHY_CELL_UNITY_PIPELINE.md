# MASTER-MAP-V0 历史地理母版、Cell 精度实验与 Unity 地图流水线

## 目标与状态

本任务把合法开放的物理地理源、项目已有的 `admin.han140.*`、`geo.region.*`、
`C001-C077`、`L###`、`location.*` 和 `R001-R012` 接入同一套可复现地图母版，比较
方形 Cell 精度，生成全国 V0 Cell Grid，并由 Unity 按 Chunk 读取和显示。

截至 2026-08-08，本任务的代码、数据包、验证场景和定向测试已经完成。它建立的是静态著作地图
底座，不改变现有主游戏模拟、存档版本或动态 Owner/Facility/Force 世界事实。

## 不变量

- 一个方格就是一个正式 Cell，采用 N、NE、E、SE、S、SW、W、NW 八邻接。
- CellId 使用可逆的 64 位行优先算法：`row * columns + column`。
- Chunk 只是加载、压缩和表现单位，绝不是第二种空间单位。
- Unity 不为全国 Cell 创建 GameObject 或 MonoBehaviour。
- 经纬度著作坐标、Albers 米制处理坐标和 Unity 表现坐标严格分离。
- 未解决坐标保持 null；行政自动几何全部标记 `synthetic_proxy` 和 `historical_claim=false`。
- Natural Earth 与开放 DEM 是现代物理参考，不宣称复原汉代河道、海岸线或行政边界。

## 交付入口

- 母版：`MapData/HanWorld_Master_V0/HanWorld_Master_V0.gpkg`
- 母版清单：`MapData/HanWorld_Master_V0/HanWorld_Master_V0_manifest.json`
- Cell 比较：`MapData/HanWorld_CellGrid_V0/reports/CELL_SCALE_COMPARISON_REPORT.md`
- Unity 数据包：`Assets/StreamingAssets/WorldMap/HanWorldV0/`
- Unity 验证场景：`Assets/Scenes/MapValidation.unity`
- 最终验收：`MapData/HanWorld_CellGrid_V0/reports/MAP_MASTER_FINAL_ACCEPTANCE.md`

## 重新生成

安装 QGIS LTR 后，在项目根目录执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\MapPipeline\Build-HanWorldMap.ps1
```

每个外部阶段独立受 300 秒硬超时保护。大型原始缓存位于 Git 忽略目录；提交的是裁切母版、
Unity 运行包、来源清单、哈希、比较图和生成代码。
