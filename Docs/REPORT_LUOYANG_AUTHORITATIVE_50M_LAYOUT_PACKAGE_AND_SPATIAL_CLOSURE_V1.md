# 洛阳权威50m布局数据包与县域空间闭环 V1 实施报告

## 1. 当前结论

本轮已完成版本化洛阳50m布局数据包，并让P1县域运行时从该包读取坐标、Footprint、
Entrance方向、道路/水渠几何、城防和Portal。原先代码内即时缩放只保留在生成脚本中，
不再是运行时布局权威。

数据合同门已通过编译和8项定向Core验证；受控Unity测试没有进入测试框架：Unity在45秒内
未创建启动日志，安全脚本终止了本任务启动的PID并记录`blocked/125`。因此当前状态为：

`IMPLEMENTED_CORE_ACCEPTED_UNITY_STARTUP_GATE_BLOCKED`

这不是代码测试失败，也不能写成完整Unity验收通过。

## 2. 权威语义与历史边界

布局包ID为：

`mandate.luoyang.county-layout-50m.runtime-authority.v1`

“Runtime Authority”表示后续地图与建设系统只从该包读取候选布局。包内同时冻结：

- `runtime_authoritative = true`
- `historically_exact = false`
- `mutates_world_state = false`
- `changes_save_schema = false`

2,084项Facility仍保留旧CellId64和源Row/Column。只有1项候选仍落在旧2km父Tile；其余
2,083项继续标记`GameplayReconstruction/Provisional`，没有写回WorldState或开局源文件。

## 3. 数据包结果

| 项目 | 结果 |
| --- | ---: |
| JSON大小 | 4,422,872 bytes |
| Facility | 2,084 |
| Road节点 / 边 | 359 / 334 |
| Canal节点 / 边 | 19 / 17 |
| Fortification边 | 144（门14、墙130） |
| County Portal | 4 |
| District UrbanArea候选 | 6 |
| 全城UrbanArea候选 | 1 |
| 候选中心唯一数 | 2,084 |

布局生成指纹为
`851858dce31b849166be9dc7e496a9283baf9bc68fc8e25f4a8a14d14ed4a358`；
当前JSON文件SHA-256为
`c486af5cfa75335cceef4c0738357cf4de0a6f24ed8e8a34c76e5ea1f1a63a58`。

源文件也被逐个冻结并在加载时校验：

| 源包 | Facility | SHA-256 |
| --- | ---: | --- |
| Luoyang184UrbanInitializationV1 | 1,230 | `8e98b126cad345ef200fc3f65bd677229cc4f487e71de964e100815941a469f0` |
| Luoyang184MetropolitanInitializationV1 | 854 | `c11ce33a29b585a52a969789a8bb5541be8fc46d726b4508db657ce35b8e347d` |

## 4. 空间闭环方式

- Facility：每项记录源锚点、候选Local Row/Column、尺寸厘米、四分之一转角、Entrance方向、
  六类District、历史置信度、空间精度及三个Provenance。
- Road/Canal：只把源Row/Column曼哈顿距离为1的同类节点连边；运行时沿候选端点绘制直线格带。
- Fortification：每项正式fortification Facility对应一条PlanningCell Edge；未有方向资料时继续
  使用确定性东向候选，边界才转西向。
- Portal：四边各一项，吸附最近正式Road候选；邻县和Route仍为candidate/unknown。
- UrbanArea：按六类Facility候选点生成稳定凸包，另生成全城凸包。它们是审阅几何，不是行政边界，
  也不是东汉洛阳精确城郭考证。

地图控制器新增“布局闭环/网络几何”视图，可同时查看六区外包络、道路、水渠、墙门和Portal；
仍使用单张320×640纹理，不创建逐格GameObject。

## 5. 自动验证

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | PASS | `tmp/skill-verification/compile-20260903-095232-115.out.log` |
| 洛阳50m定向Core | 8/8 PASS | `tmp/skill-verification/core-tests-20260903-095254-368.out.log` |
| Unity EditMode定向 | BLOCKED / 125 | `tmp/unity-validation/unity-EditMode-20260903-094459-513.summary.json` |
| 任务范围diff check | PASS | 本轮定向检查无输出 |
| 全工作区diff check | BLOCKED | 4个既有P0Final FBX `.meta`尾随空格，与本任务无关 |

Core覆盖布局头与计数、正式Facility身份和源锚点、P1反向读取、道路/水渠四邻全集、
Portal边界、UrbanArea凸包及重复加载确定性。

## 6. 未完成与下一步

1. Unity EditMode与图形PlayMode需在Unity能生成启动日志后重新运行；本轮未获得新增视觉证据。
2. 当前道路/水渠只闭合已有四邻资料，不代表洛阳县域全部官道与天然河流已经考证。
3. 城防方向、Entrance方向、分区凸包和四个Portal仍是Provisional。
4. World Schema仍为V79；没有建设事务、地块产权、施工阶段或存档迁移。

下一步进入“洛阳县域规划建设工具 V1”前，应以本包为只读底座实现选格、Footprint占用、
道路可达、地形/水体/城防阻挡、蓝图预览与可撤销命令候选；不得另建第二套城市账。
