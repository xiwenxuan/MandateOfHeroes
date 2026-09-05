# 任务书：洛阳50m县域真实规模原型与Facility迁移验证 V1

## 1. 任务定位

本任务承接“双尺度统一世界地图、50m县域空间与流式分区架构决策验证 V1”的
Decision A，在不改变 World Schema V79、不写回正式开局包的前提下，建立洛阳
512km² 县域候选空间并验证现有2,084项Facility迁移、分区装载和Unity表现容量。

## 2. 冻结合同

- 全国战略层继续使用2km Global Cell；50m PlanningCell只在打开县域时物化。
- 洛阳候选县域为16km×32km，即320×640、204,800个PlanningCell。
- 1个2km StrategicTile严格对应40×40个PlanningCell；县域覆盖8×16个战略Tile。
- Chunk边长16格，总数800；禁止为每格创建GameObject或MonoBehaviour。
- 2,084个既有Facility的稳定ID、Definition、Model、旧CellId64和资料精度不得改变。
- 当前旧Facility锚点跨越92×65个2km Cell，无法在512km²内保持原坐标。本任务只建立
  `gameplay-reconstruction` 候选位置，并同时保留旧锚点；候选位置不是历史断言。
- Terrain、Water和Road底图必须读取HanWorldV1；不得把摄像机加载状态变成世界结算条件。
- 本任务不改Person、Household、Inventory、生产、所有权、控制权或正式Facility账。

## 3. 实施范围

1. 建立204,800格紧凑县域分区，并从8×16个真实战略Cell展开地形、水系和道路。
2. 确定性迁移2,084项Facility，保留源坐标与候选坐标、资料精度、分区和尺寸来源。
3. 由既有fortification/road Facility派生候选墙门与四向县界Portal，明确标记未知邻县。
4. 对HOT/WARM/COLD装载、Facility footprint索引、内存、Chunk和确定性Hash实测。
5. 建立Unity单图/批次表现，证明不创建204,800个场景对象。
6. 输出自动测试、性能数据、视觉证据和迁移风险结论。

## 4. 验收门槛

- PlanningCell=204,800，Chunk=800，紧凑数组=2,457,600 bytes。
- Facility=2,084且ID唯一、源Cell不变、候选中心均在县域内。
- District=6；road Facility=359；fortification、water、road来源计数可审计。
- 相同输入重复构建的空间Hash完全一致；HOT/WARM/COLD不修改正式世界摘要。
- HOT索引覆盖全部2,084项Facility；COLD驻留格、Chunk和Facility索引均为0。
- Unity中PlanningCell GameObject=0，地图表现对象保持常数级。
- 全工程编译、核心测试、受控Unity测试、任务范围diff check通过。

## 5. 决策门

- A：容量与迁移技术结构通过，可进入历史位置补录/正式空间闭环；仍不得升级存档。
- B：容量通过，但旧2km布局无法作为512km²精确县域，先建立权威50m布局数据包。
- C：容量或确定性失败，回到分区/索引方案。

本任务预期允许技术门为A、资料门为B；两项必须分别报告，禁止合并成“正式迁移完成”。
