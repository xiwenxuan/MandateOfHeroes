# 洛阳50m县域真实规模原型与Facility迁移验证 V1 实施报告

## 1. 结论

本轮已在正式Domain/Persistence/Simulation/Presentation程序集完成洛阳512km²候选县域：

```text
16km×32km
= 320×640 PlanningCell50m
= 204,800格
= 8×16 StrategicTile2Km
= 800个16×16 Chunk
```

2,084项既有Facility全部进入非持久迁移候选，稳定ID、Definition、Model、旧CellId64、
资料精度和六类城市分区保持不变；没有新增或删除正式Facility，也没有修改World Schema V79。

决策必须拆成两部分：

- **技术门 Decision A（通过）**：真实格数、全部设施、紧凑分区、确定性Hash、HOT/WARM/COLD和Unity单Renderer均通过。
- **历史定位门 Decision B（未关闭）**：旧设施布局横跨约184×130km，不能直接解释为512km²洛阳县域；2,083项候选位置仍需权威50m布局包替换。

当前状态：`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`。

> 2026-09-03后续说明：权威运行时布局包与空间闭环已经由
> `TASK_LUOYANG_AUTHORITATIVE_50M_LAYOUT_PACKAGE_AND_SPATIAL_CLOSURE_V1.md`实现；P1加载器现已
> 反向读取该包。此更新关闭“运行时仍即时缩放”的数据合同缺口，但不关闭历史精确位置门，
> 也不授权V79存档迁移或正式建设写入。

## 2. 数据边界与迁移方式

既有2,084项Facility源范围为Row 1202—1266、Column 2013—2104，即65×92个2km
战略格包络。候选县域使用设施中位数为中心，读取HanWorldV1 Row 1236—1243、
Column 2036—2051的8×16个真实父级战略格。

候选位置按旧包络的相对Row/Column确定性缩放到320×640格。源格唯一性使2,084个
候选中心无碰撞，但这种缩放只保持相对结构，不是史实测绘：

| 项目 | 数量 |
| --- | ---: |
| 原Facility | 2,084 |
| 候选中心唯一 | 2,084 |
| 保持原2km父Tile | 1 |
| 玩法重建候选 | 2,083 |
| Facility Definition | 61 |
| WholeCityComposition分区 | 6 |
| Road Facility | 359 |
| Fortification Facility | 144 |
| Canal / Bridge / Well Facility | 19 / 2 / 16 |

每个候选记录同时保存源坐标与候选PlanningCell，并统一标记
`spatial-provenance.gameplay-reconstruction.provisional.v1`。候选Footprint使用类别默认值，
不会把旧`StrategicFootprintRatio`误写成现实米制尺寸。

## 3. 地形、道路、水系和Portal

地形、高程、坡度、道路和水状态读取HanWorldV1父级Cell，再展开到40×40子格。所选
128个父格中有21个Road Cell、0个Water Cell。这个0是正式输入事实，不能用视觉河流覆盖。

为验证局部水网结构，本轮另由19个正式Canal Facility的旧相邻关系派生122个候选水格；
该计数与HanWorldV1水格严格分离。四个县界Portal吸附到最接近四边的正式Road Facility候选，
邻县和Route均标记`candidate/unknown`，不宣称历史道路交点已经考证。

144项fortification Facility投影为PlanningCell Edge，墙与门继续使用P0的双向通行合同。
六个既有城市构图区全部进入候选布局，但本轮没有把其点云外包络冻结为正式UrbanArea边界。

## 4. 流式与性能结果

Unity 2022.3.62f3c1图形PlayMode实测：

| 指标 | 结果 |
| --- | ---: |
| PlanningCell | 204,800 |
| Chunk | 800 |
| 紧凑数组 | 2,457,600 bytes |
| 端到端构建 | 1,478.335 ms |
| 构建期托管内存差值 | 40,230,912 bytes |
| HOT P50 / P95 | 81.991 / 87.085 ms |
| WARM P50 / P95 | 87.835 / 90.490 ms |
| COLD P50 / P95 | 79.195 / 82.150 ms |
| PlanningCell GameObject | 0 |
| PlanningCell Renderer | 1 |

端到端构建包括正式内容目录、2,084项设施、WholeCityComposition、本地地图依赖和
HanWorldV1读取；40MB是构建前后托管堆差值，不等同于稳定驻留内存。HOT/WARM/COLD
外层测量包含每次前后约2.46MB空间Hash完整性校验，因此WARM/COLD时间不代表最终无Hash
运行成本。三档切换后的Hash均保持
`da5fc7094370e0306910f70a1d7589ccddeb5d5bd94812bb1cae4ecfddb54a14`。

Unity使用一张640×320数据纹理和一个Quad Renderer表现全部格与设施点，不创建逐格对象。

## 5. 验证

| 阶段 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | 通过 | `tmp/skill-verification/compile-20260903-023752-141.out.log` |
| 新增Core | 4/4通过 | `tmp/skill-verification/core-tests-20260903-023523-413.out.log` |
| P0双尺度Core回归 | 29/29通过 | `tmp/skill-verification/core-tests-20260903-023758-272.out.log` |
| Unity EditMode原型 | 2/2通过 | `tmp/unity-validation/unity-EditMode-20260903-023252-395.summary.json` |
| Unity EditMode场景 | 1/1通过 | `tmp/unity-validation/unity-EditMode-20260903-023053-981.summary.json` |
| Unity图形PlayMode | 1/1通过 | `tmp/unity-validation/unity-PlayMode-20260903-023321-266.summary.json` |
| 任务范围diff检查 | 通过 | 全仓仍被既存4个P0Final FBX `.meta`尾随空格阻塞 |

首次沙箱内Unity运行在启动日志生成前触发45秒安全门禁；沙箱外同一安全脚本成功，未关闭
用户程序。三张图已经人工检查，不是空BackBuffer；索引见
`Docs/Evidence/Luoyang50mCountySpatialPrototypeV1/README.md`。

## 6. 未完成项与下一步

本轮没有完成或批准：历史精确50m Facility坐标、正式UrbanArea边界、真实黄河/洛水几何、
县界道路交点、建筑米制Footprint/Entrance、建设工具、地块权属、正式局部导航、人物物化、
战争物化、Addressables最终流式或存档迁移。

下一任务应是“洛阳权威50m布局数据包与县域空间闭环 V1”：为2,084项Facility逐项给出
稳定候选坐标、Footprint/Entrance来源等级，补录道路/河渠/城墙/Portal几何，形成可审阅
UrbanArea；只有该数据门通过后，才进入正式存档兼容设计和建设玩法接入。
