# 双尺度统一世界地图与50m县域空间 V1 证据索引

本目录由 `DualScaleWorld50mArchitectureV1Tests`、
`DualScaleWorld50mArchitectureV1UnityTests` 和
`DualScaleWorld50mArchitectureV1PlayModeTests` 生成并校验。画面是架构 Debug
原型，不代表洛阳正式县域、美术或攻城系统已经完成。

## 截图

| 文件 | 验证内容 |
| --- | --- |
| `01_dual_scale_strategic_tiles.png` | 2×2 个 2km StrategicTile 及两县分区 |
| `02_planning_cells_50m.png` | 80×80、共 6400 个 50m PlanningCell；一边 40 倍细分 |
| `03_facility_physical_footprint.png` | Facility 的位置、旋转与跨格 Footprint，不等于 Cell |
| `04_cell_four_port_topology.png` | Cell 北东南西四向连接、道路与阻挡拓扑 |
| `05_wall_edge_and_gate.png` | 墙位于 Cell Edge，Gate 位于同一通行边 |
| `06_county_portal_route.png` | 县内道路、多个 Portal 与同一世界 Route 连续 |
| `07_height_and_los_low.png` | 低位观察点被墙体遮挡，LOS 为红色 |
| `08_height_and_los_high.png` | 攻城高台提高有效高度，LOS 为绿色 |
| `09_facility_garrison_control.png` | Facility、耐久、守军和 Controller 分离 |
| `10_hot_warm_cold_debug.png` | HOT/WARM/COLD 只改变加载与表现层级 |

## 数据

- `performance-core.json`：6400 格紧凑数组、构建、内存、加载层级与 204800 格线性
  理论估算。
- `performance-unity.json`：Unity 版本、逐格 GameObject 计数、渲染对象、Chunk 与十视图
  捕获时间。
- `performance-detailed.json`：连接、墙、Facility 投影、LOS 分位数及 Load/Unload 的
  独立性能探针（最终验收阶段生成）。

洛阳 204800 格数据只属于理论估算，`executed=false`，不能解释为洛阳规模性能通过。
