# 任务书：洛阳县域规划建设工具 V1

## 1. 任务定位

本任务在现有统一世界账本、2km 战略层和洛阳 50m 县域空间包上，交付玩家可操作的
建设规划工具。正式入口为：

```text
PlayableDemo → M 天下 → 选择洛阳 → 洛阳｜县域规划 → 建设
```

V1 只创建非持久化 `DraftBuildingBlueprint`。它不是正式建设事务，不扣材料、劳力、
钱粮或土地，不推进日期，不新增或删除 `Facility`，不改变人物、人口、家户、产权、
道路、水系、城墙、导航和世界结算。World Schema 保持 V79。

## 2. 权威输入与边界

- 县域：`admin.han140.sili.henan.luoyang`。
- 县域面积：512 km²；320×640，共 204,800 个 50m PlanningCell。
- 布局输入：`mandate.luoyang.county-layout-50m.runtime-authority.v1`。
- 既有 Facility：2,084 项；稳定 ID 与正式定义保持不变。
- 道路：359 个道路 Cell、334 条闭环边；V1 在规划层统一解释为现有普通道路能力。
- 水渠：19 节点、17 边；城防边 144；县域 Portal 4。
- 2,083 项非确认位置仍为 gameplay reconstruction/provisional，规划工具不得把它们
  改写为史实精确坐标。
- PlanningCell 是空间索引，不是建筑；四向端口是通行事实，不是建筑入口。

## 3. 玩家功能

玩家必须能够：

1. 进入和退出洛阳县域规划。
2. 在 50m 规划窗中点击 Cell，读取高程、地形、坡度、用地、水体、四向通行、
   既有 Facility、墙门、Portal 与最近道路信息。
3. 在五个现有正式候选中切换建筑。
4. 查看按厘米合同换算的实体 Footprint、覆盖 Cell、主要入口和入口—道路连接。
5. 按 0°/90°/180°/270° 旋转建筑；入口坐标与朝向同步旋转。
6. 区分可落位、条件性落位和不可落位；显示稳定的首要原因。
7. 连续创建多个草案，阻止草案互相重叠，并支持 Undo/Redo。
8. 对真实洛阳布局复核既有 Facility、水体、城墙、道路接入和县界阻挡。
9. 在规划地图内按住中键平移局部规划窗、按住右键旋转地图视角；地图旋转与
   `Tab` 建筑旋转彼此独立，旋转后的鼠标点选必须反算到正确 PlanningCell。

预览只在 Cell、旋转或建筑类型改变时重算，不随每个鼠标像素全量重算。

## 4. 数据驱动落位合同

`FacilityPlacementProfile` 至少包含：

- profile、Facility definition、blueprint/build contract、model 稳定 ID；
- 宽、长、高厘米值；
- 允许的四个旋转；
- 一个主要入口及可选辅助入口的实体偏移与外向方向；
- 允许/禁止地形和最大坡度；
- 道路要求、最低道路类别、入口最大距离；
- 水体、城防、既有 Facility 重叠策略；
- 净距、放置类别、Availability 和来源。

V1 冻结的候选为：

| 玩家名称 | Facility 定义 | 现有模型/合同 | 实体占地 |
|---|---|---|---:|
| 普通住处 | `facility.residential.urban_quarter` | 现有住宅模型与建造合同 | 32×28m |
| 仓库 | `facility.storage.warehouse` | 现有仓库模型与建造合同 | 60×45m |
| 工坊 | `facility.industry.workshop` | 现有工坊模型与建造合同 | 45×35m |
| 烽燧 | `facility.military.beacon` | 现有洛阳军用烽燧模型合同 | 30×30m |
| 大型市集组 | `facility.commercial.market` | 现有市场模型的多格组合 | 110×80m |

烽燧不因出现在规划菜单中获得普通玩家建设权；有效位置只生成带军政权限提示的
条件性草案。

## 5. 中央校验器

`FacilityPlacementValidator` 是唯一落位判定入口，返回：

- `PlacementValidationState`；
- 稳定排序的 blocking reasons 和 warnings；
- 实际覆盖 PlanningCell；
- 道路接入状态与连接几何；
- 冲突对象稳定 ID；
- 最低/最高高程与最大坡度。

道路接入状态固定为：`Connected`、`TooFar`、`Blocked`、`WrongSide`、`NoRoad`；
无需道路时为 `NotRequired`。判断基于旋转后的主要入口、道路 Cell 实体范围、直达路径、
水体、城墙和既有 Facility 障碍，不使用 UI 名称或字符串猜测。

稳定阻挡类别至少覆盖：县界外、不可建设、禁用地形、坡度、水体、道路占用、城防、
Portal、既有 Facility、草案、无路、过远、受阻和入口背向。

## 6. 索引与表现预算

- 既有 Facility 建立 Cell occupancy index；单次预览不得遍历全部 2,084 项。
- 道路、水体、城防、Portal 和草案分别建立索引。
- 县域底图使用一张 640×320 运行时纹理；PlanningCell GameObject 数必须为 0。
- 玩家默认查看 48×24 Cell、2.4×1.2km 的局部规划窗，以保证实体 Footprint 可读；
  县域权威范围仍是完整 512 km²。
- 预览、入口和道路连线由轻量叠加层绘制；草案不进入正式移动或导航。

## 7. 自动验收

核心测试必须覆盖：

- 配置与现有定义/模型/合同一致；
- 小型、大型、多 Cell 和四向旋转；
- 县界、既有 Facility、水体、城防和道路失败；
- 有效道路接入；
- 草案碰撞、Undo、Redo 和确定性；
- 世界日期、人口、Facility 和空间哈希不变；
- 索引候选有界及 P50/P95 指标。

Unity EditMode 验证单纹理、零逐格对象、配置和真实阻挡。Unity PlayMode 必须从
`PlayableDemo` 的正式玩家控制器进入规划，创建两个草案、撤销一个草案，并再次证明
正式世界快照完全一致。

证据目录固定为 `Docs/Evidence/LuoyangCountyPlanningToolsV1/`，包含任务书规定的 12 张
状态图及 `planning_performance_v1.json`。

## 8. 人工验收

自动验收后，另启一个 Unity GUI 实例，打开 `PlayableDemo`、进入 Play Mode，并停留在
洛阳县域建设规划的有效住宅预览。不得由自动测试清理该实例。只有用户明确回复通过后，
状态才可改为 `ACCEPTED`。

## 9. 本任务之后

V1 通过人工验收后，才可另立任务开发正式建设事务、用地/权限、材料、工期、施工阶段、
取消退款、AI 建设和存档迁移。道路规划、城墙规划、水利和 NPC 自主建设不属于本任务。
