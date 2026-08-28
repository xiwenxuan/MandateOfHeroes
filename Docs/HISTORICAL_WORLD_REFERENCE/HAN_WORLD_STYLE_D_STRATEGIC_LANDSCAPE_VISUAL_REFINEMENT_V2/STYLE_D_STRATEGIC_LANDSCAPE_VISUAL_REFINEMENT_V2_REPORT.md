# Style D 战略山河视觉细化 V2 实施报告

## 结论

Style D V2 已形成可以由用户直接审图的 Unity 原型：同一权威世界在 WORLD、REGION、CITY_DISTANCE 和近景使用不同表现顶点密度；河流、森林、山地、谷地和平原仍读取同一 Global Cell、DEM、河流目录和自然地表。没有建立第二套地图、没有 SubCell、没有复制外部项目代码或资产。

`PRODUCTION_STATUS = STYLE_D_STRATEGIC_LANDSCAPE_V2_READY_FOR_USER_REVIEW`

这不是最终视觉完成状态。河流锐弯的一处 canonical source-segment 端点接缝、汇流 union mesh、CITY 低频块状感和连续 LOD morph 仍是 `PARTIAL`。

## 空间与数据合同

| 项目 | 冻结值 |
| --- | --- |
| CRS | `hanworld.albers.china.v0` |
| Global Origin | `(-3417344.395965772, 6199580.451937504)` |
| Grid | `3314 × 2176` |
| Global Cell count | `7,211,264` |
| Cell size | `2000m` |
| Terrain Tile | `8 × 8 Cell` |
| Simulation SubCell | `false` |
| Visual terrain contract | `presentation.han-world.visual-terrain-detail.v2` |

表现层分辨率：WORLD=`1×`，REGION=`2×`，CITY=`4×`，CLOSE_PREVIEW=`8×`。细化顶点以全局投影坐标确定性生成，源高程与 Surface 只读；表现高程不得回写 Domain 或 Persistence。

## 实现摘要

- `StyleDStrategicLandscapeV2.cs`：表现层地形细化、全局坐标微起伏、Surface 双线性权重融合。
- `GlobalRiverVisualGenerator.cs`：曲率感知采样、受限 Miter、Bevel 回退、统一水面/河岸横断面、地形高度采样和网格诊断。
- `GlobalVegetationGenerator.cs`：WORLD 地表密度、REGION 合并树冠簇、CITY 合并单树网格；不创建逐树 GameObject。
- `HanWorldArtProfile.cs`：Style D V2 参数、固定相机和 WORLD/REGION/CITY 细节路由。
- `HanWorldNaturalMapController.cs`：细节级别切换、驻留范围、性能快照、网格关闭与证据截图入口。
- `NaturalTerrainV2.shader`：山脊、谷地、山麓、平原与战略宏观变化；只影响表现。

## 河流状态

机器诊断的 invalid triangle、NaN vertex、extreme miter 和 triangle hole 均为0。水面与河岸共享中心线、宽度和采样，不再使用两套互相漂移的逻辑。

人工审图仍发现：

- `08_STYLE_D_V2_RIVER_SHARP_BEND.png` 有一处输入源线段端点接缝；
- 汇流没有建立 union/junction mesh；
- 河岸材质仍是程序化原型。

因此 `RIVER_MESH_STATUS = PARTIAL`，`RIVER_BANK_STATUS = PASS_WITH_ART_LIMITS`。

## 森林状态

- WORLD：森林由地表密度和色彩表达，不驻留单树，`PASS`。
- REGION：按全局格点确定性生成合并树冠簇，`PASS_WITH_ART_LIMITS`。
- CITY：按全局格点生成合并单树网格与林间空地，`PASS_WITH_ART_LIMITS`。

树冠和树体仍是低多边形程序化原型，后续可以换美术资产，但不得改变密度事实或随机位置合同。

## 性能证据

受控 PlayMode 样本中：

- WORLD：113,568 terrain vertices，约6.48 MiB terrain mesh，2 draw calls；首次世界网格生成约1479.3ms。
- REGION：15,370 vertices，约0.87 MiB，12 draw calls，约140—148ms生成。
- CITY：39,994 vertices，约2.25 MiB，28 draw calls，约153—155ms生成。
- CLOSE_PREVIEW：118,394 vertices，约6.70 MiB，28 draw calls，约153.7ms生成。

这些是批处理原型样本，不是最终GPU基准；高精细节继续依靠 LOD、驻留和网格密度控制，而不是退回2km近景。

## 截图证据与停止边界

核心目录包含严格15张截图：2张冻结 V1、13张 V2。截图由 Unity Game View 自动生成，关闭 Cell Grid 和旧背景依赖。它们是用户审图候选，不是最终 Golden。

另有5张河流专项截图，覆盖直段、缓弯、锐弯、汇流观察位和河岸近景；它们同时证明当前锐弯接缝与汇流视角仍需后续处理。

## 验证结果

- 全工程编译：`PASS`。
- `GlobalSpatial`专项核心回归：`PASS`。
- 完整核心回归：300秒内未完成，在洛阳三年长跑测试阶段触发硬超时；已停止本任务进程树，日志中没有记录失败项。
- Style D V2 EditMode：`5/5 PASS`。
- 空间、自然地图、V1 Style D兼容 EditMode：`14/14 PASS`。
- 完整 EditMode：300秒硬超时，未生成结果XML；不得声称通过。
- Style D V2 PlayMode：`1/1 PASS`，含15张核心截图、5张河流专项截图和LOD转换断言。
- 15份工作簿渲染与公式错误扫描：`PASS`。
- `git diff --check`：`PASS`（仅有既有行尾转换提示）。

本任务到此停止在用户审图：不开始全国生产、不生成河南尹全量高精地形、不制作洛阳城墙、宫城、十二门、市场或住宅资产。
