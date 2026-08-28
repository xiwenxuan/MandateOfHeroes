# 洛阳实际全城构图与地形融合 V1 任务书

任务 ID：`LUOYANG_ACTUAL_WHOLE_CITY_COMPOSITION_AND_TERRAIN_INTEGRATION_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-08-27

范围：184年洛阳2,084项开局Facility的全城视觉锚点、功能构图区、道路朝向与地形采样落位

## 一、任务目标

在54/54最终建筑资产已经激活后，把“模型齐备”推进到“同一洛阳地图中可以组成城市”：

- 为2,084项Facility逐项建立确定性 Visual Local Anchor；
- 保留每项原有Facility ID、Definition、Model、Asset Variant、Global Cell和8×8表现批次；
- 形成宫城政务、里坊住宅、市肆工坊、城防、交通水利、农业资源六类构图区；
- 道路、沟渠和墙体使用相邻Cell推导直线、转角、端点和交叉连接；
- 普通建筑面向最近的真实道路Facility，并在本Cell内形成320米战略表现前场；
- 最密24×24窗口的549项建筑按偏移后的全局坐标重新采样现有Terrain高度；
- 继续复用既有8×8空间＋材质合批和54项最终Prefab的LOD2模块。

本任务不建立SubCell。局部偏移、旋转、比例、地表和连接Profile只属于Presentation，不进入土地、
产权、建设、人口、战斗或存档权威。

## 二、权威输入

- 1,230项城内Facility：`Luoyang184UrbanInitializationV1/facilities.json`；
- 854项都会区Facility：`Luoyang184MetropolitanInitializationV1/facilities.json`；
- 全城表现计划：2,084 Facility、64个8×8表现批次、最密窗口549 Facility；
- 最终资产清单：54个稳定Asset Variant映射2,084 Facility；
- Global Cell：2,000米权威网格；
- 当前Terrain：同一全国DEM、Floating Origin与Region高度采样器。

## 三、冻结构图合同

| 合同项 | 冻结值 |
|---|---:|
| Facility视觉锚点 | 2,084 |
| 最终Asset Variant | 54 |
| 构图区 | 6 |
| 最密常驻窗口 | 24×24 Cell / 549 Facility |
| 普通建筑道路前场偏移 | 320米，仍在原Cell内 |
| 最大局部单轴偏移 | 420米 |
| 道路/沟渠/墙体中心线偏移 | 0米 |
| 创建Simulation SubCell | 否 |
| 修改Save Schema | 否 |

六个稳定构图区ID：

1. `district.luoyang.palace-civic-core.v1`；
2. `district.luoyang.residential-wards.v1`；
3. `district.luoyang.market-workshop-band.v1`；
4. `district.luoyang.defense-ring.v1`；
5. `district.luoyang.water-transport-network.v1`；
6. `district.luoyang.agricultural-resource-hinterland.v1`。

这些是可扩展的稳定命名空间ID，不是封闭枚举，也不是新增行政区或模拟Region。

## 四、实施方案

1. Domain建立全城构图合同、锚点、构图区、地表Profile和连接Profile的严格校验。
2. 根据Facility Definition分配功能构图区，不移动任何权威Facility或Global Cell。
3. 对道路、沟渠、城墙和宫墙检查四向相邻同族Facility，确定中心线朝向与连接形态。
4. 对其他设施查找距离最近、稳定ID排序优先的真实道路Facility，生成朝路前场和90度正交朝向。
5. Presentation把Cell中心与Visual Local Anchor合成全局表现坐标，再调用现有Terrain高度采样器接地。
6. 合批矩阵使用构图比例、构图朝向和最终Prefab LOD2模块，保持原性能预算与清理生命周期。
7. `CITY`审查入口复用现有最密窗口相机，输出1600×1000 Unity实际Game View。

## 五、验收标准

1. 2,084项锚点、2,084个唯一Cell和54个Asset Variant完整一致。
2. 六个构图区均非空，合计恰好2,084项。
3. 同一输入重复生成逐项完全一致，不使用运行时随机数。
4. 所有局部偏移均小于420米，不创建SubCell；走廊设施保持Cell中心线。
5. 最密窗口恰好549项，全部要求Terrain Grounding。
6. 549项最终资产继续满足原空间合批预算，无建筑Collider，切回WORLD后清理完毕。
7. 全工程编译、定向核心、目标EditMode、目标图形PlayMode和`git diff --check`分别记录。

## 六、明确边界

- 本任务不选择或冻结全国自然地图最终Style；现有Style D只作为当前审查Profile。
- 当前接地仍使用全国2km DEM/Region采样；河南尹与洛阳高分辨率DEM仍是后续专项。
- 不完成室内、碰撞代理、导航、桥梁通行、城门开闭、损毁动画或攻城。
- 不把六类构图区冒充精确考古分区；史实锚点继续服从既有Facility来源与置信度。
- 不全量常驻2,084个高精GameObject；全城索引保持2,084项，当前审查窗口常驻549项。

## 七、实施清单

- [x] 新增2,084项全城构图Domain合同与确定性生成规则。
- [x] 接入54项最终Asset Variant、六类构图区与道路/走廊连接Profile。
- [x] 接入Cell内Visual Local Anchor、Terrain重采样和构图比例合批矩阵。
- [x] 新增定向EditMode与图形PlayMode验收。
- [x] 建立证据目录并更新总纲、地图计划与任务路由。
- [x] 完成目标验证并回填执行结果。

## 八、执行结果（2026-08-27）

| 项目 | 结果 |
|---|---|
| 全城构图锚点 / 唯一Cell | 2,084 / 2,084，通过 |
| 最终Asset Variant | 54，通过 |
| 六类构图区 | 农业资源746、城防184、市肆工坊258、宫城政务100、里坊住宅324、交通水利472 |
| 走廊中心线 | 524项道路/沟渠/墙体，Cell内偏移为0 |
| 最密窗口 | 549项全部Terrain Grounding |
| 合批 | 97 Renderer、17,512顶点、24.0933ms、94.20% Renderer降幅，预算通过 |
| 全工程编译 | 通过；`tmp/skill-verification/compile-20260827-231327-959.out.log` |
| 定向核心合同 | 1/1通过；`tmp/skill-verification/core-tests-20260827-231346-142.out.log` |
| 目标EditMode | 3/3通过；`tmp/unity-validation/unity-EditMode-20260827-230832-620.summary.json` |
| 目标图形PlayMode | 1/1通过；`tmp/unity-validation/unity-PlayMode-20260827-230855-780.summary.json` |
| 既有549批处理图形回归 | 1/1通过；`tmp/unity-validation/unity-PlayMode-20260827-231048-601.summary.json` |

首次沙箱内Unity启动在45秒无日志门禁处被安全终止；按测试规则在允许环境使用同一安全脚本重试后
通过，未遗留本任务Unity进程。验证为定向证据，不扩大为完整核心、EditMode或PlayMode套件通过。

## 九、后续顺序

本V1通过后，下一阶段应优先建立洛阳建筑选择范围、独立碰撞代理与道路/桥门通行导航；随后再把
都会区外的农业供给、运输和入城节点实体化。高分辨率DEM、最终自然光照和材质Golden需要单独门禁，
不能由本任务的接地成功替代。
