# 战争空间、野战、建筑攻坚与战斗结算架构草案

## Document Governance

- Purpose：记录战争空间、Formation、野战、建筑攻坚、城防和战斗结果回写的未来架构。
- Authority：L2 DESIGN DRAFT；供未来战争原型和正式设计收口使用，不覆盖现行 Runtime、存档或 L1 领域规则。
- Status：`DESIGN_RECORDED_IMPLEMENTATION_DEFERRED`。
- RelatedCanonicalDocs：`UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md`、`UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`、`WORLD_SIMULATION_FOUNDATION.md`、`GAME_SYSTEMS_MASTER_AND_STATUS.md`。
- RuntimeImpact：无。
- SaveSchemaImpact：无。
- Supersedes：无。

### 开工前必须处理的兼容项

本草案记录新的空间化战争方向，但当前不修改既有权威合同。未来正式实施战争前必须显式裁决：

1. `UNIFIED_COMBAT_WARFARE_AND_AUTHORITY.md` 第 28 节目前把城墙和城门描述为真实 Facility；本草案第 65—69 节把城墙描述为 `PlanningCell Edge` 上的防御结构、把城门描述为特殊 Passage Structure。两者在领域建模上尚未正式统一。
2. `UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md` 的“一支独立 Force 占一个 2000m Cell”继续作为战略投影；本草案中的 Formation Boundary、WorldPosition 和 50m PlanningCell 战术查询尚未接入该权威合同。
3. `WORLD_SIMULATION_FOUNDATION.md` 中的“详细战场”只能解释为同一世界事实的细节投影或 Scene 表现，不能成为第二套战场权威世界。

在完成上述裁决、原型和存档设计前，不得据本草案直接修改运行时结构。

---

# 1. 文档状态

**状态：DESIGN DRAFT / IMPLEMENTATION DEFERRED**

本文件用于记录已经形成的战争系统架构共识。

当前只作为未来开发基础。

当前项目开发优先级仍然是：

```text
县域地图
建筑建模
Golden Block
建设模式
县域Presentation
```

本文件建立后：

不得立即开始战争实现。

---

# 2. 战争系统总体目标

战争必须发生在：

> **同一个永久世界。**

不得创建：

```text
FieldBattleWorld
SiegeWorld
BattleCopy
TemporaryCombatMap
```

作为第二套权威世界。

县域中的：

```text
Terrain
PlanningCell
Road
River
Facility
Wall
Gate
Person
Army
Inventory
```

同时也是战争使用的世界事实。

---

# 3. M / C / F与战争的关系

正式主视角继续保持：

```text
M 天下
C 县域
F 人物
```

## M 天下

负责：

```text
军队战略位置
跨县移动
战略Route
战争总体态势
补给线
跨县进军
```

## C 县域

是正式战争发生空间。

包括：

```text
野战
营寨
设施攻防
城墙
城门
攻城
军队实体化
```

## F 人物

用于：

```text
重要人物
个人冲突
小规模战斗
人物观察
```

不得因为进入战争创建另一张地图。

---

# 4. 两级Cell体系继续保持

天下：

```text
Strategic Cell
2000m × 2000m
```

县域：

```text
PlanningCell
50m × 50m
```

并保持：

```text
1 × 2000m Strategic Cell
=
40 × 40 个50m PlanningCell
```

战争不新增第三套正式Cell。

---

# 5. 50m PlanningCell在战争中的定位

50m Cell不是：

```text
一格一个Army
一格固定500人
纯棋盘Token槽位
```

它负责：

```text
地形
GroundElevation
可用空间
四向通行
道路
河流
桥
城墙Edge
Gate
Breach
建筑占地
战术空间查询
```

军队和人物仍然存在于：

```text
WorldPosition
```

---

# 6. Cell Grid显示原则

Cell始终存在。

Grid是否显示只是Presentation。

## 普通县域

```text
50m Grid隐藏
```

## 建设模式

```text
50m Grid显示
```

并且这里显示的就是正式：

`PlanningCell50m`

不得新增5m BuildCell或其他第二套建设Cell。

## 战争

当前方向采用：

> 情境显示。

候选规则：

```text
普通战斗观察
→ Grid隐藏

部署
→ Grid明显显示

选择部队 / 发布命令
→ 附近Cell淡显

工事 / 营寨 / 攻城器械规划
→ Grid显示

城墙交互
→ 强调Cell Edge

战术Overlay
→ 按需显示Cell

Debug
→ 可以全开
```

战争中是否永久显示Grid：

**尚未冻结。**

---

# 7. Cell四口继续属于Cell

正式保持：

```text
North
East
South
West
```

四口代表：

相邻Cell之间的通行。

例如：

```text
Open
BlockedByWater
OpenByBridge
BlockedByWall
OpenByGate
BlockedByClosedGate
OpenThroughBreach
```

建筑入口不属于这套四口。

---

# 8. 建筑入口仍属于Facility

Facility继续遵守：

```text
Position
Rotation
Footprint
Height
Collision
Entrances[]
```

一栋建筑可以：

```text
1个入口
2个入口
多个入口
```

不规定“所有建筑固定四门”。

战争中建筑门可以根据Definition拥有：

```text
Open
Closed
Locked
Damaged
Destroyed
Controlled
```

等状态。

---

# 9. 人口始终是真实Person

战争不得创造：

```text
虚拟500兵
临时兵数字
与人口世界无关的战场人口
```

士兵原则上来源于真实：

`Person`

因此战争结束以后：

```text
死亡
受伤
被俘
逃散
```

都可以最终作用到真实永久人口。

这也是本项目区别于普通军团战争游戏的重要原则。

---

# 10. 真实Person不等于实时独立Combat Agent

必须严格区分：

```text
Person是真实永久人口
```

与：

```text
每个Person都是实时高频AI Agent
```

后者不成立。

例如：

```text
50,000真实作战Person
```

不意味着需要：

```text
50,000 NavMeshAgent
50,000 MonoBehaviour Update
50,000独立目标搜索
```

大规模战争必须采用Formation聚合。

---

# 11. Army层级

未来建议形成：

```text
Person
↓
Formation
↓
Army
↓
Strategic Army Projection
```

---

# 12. Person

真实永久人物。

记录：

- 身份；
- 装备；
- 技能；
- 伤势；
- 经历；
- 家庭；
- 所属。

---

# 13. Formation

县域战场上的主要战术对象。

Formation不是虚构人口。

它只是：

> 对真实Army成员进行战术组织。

Formation人数：

**不固定。**

例如：

```text
80
300
1,200
3,000
8,000
```

理论上都可以存在。

具体合理规模取决于战争规模和指挥层级。

---

# 14. Army

Army由：

```text
多个Formation
+
指挥人员
+
后勤
+
运输
+
工程
+
其他随军体系
```

组成。

M天下主要显示Army。

C县域战争展开Formation。

---

# 15. 超大型战争人口必须区分

史料中可能出现几十万级甚至更高名义参战数字。

游戏架构不能把：

```text
Army Total Population
```

全部解释成：

```text
Frontline Combatants
```

建议未来至少区分：

```text
Army Attached Population
Combat Effectives
Support Personnel
Transport / Logistics
Wounded / Unavailable
```

具体比例：

当前不冻结。

---

# 16. Formation空间基础

当前方向是：

> 从人数推导阵型占地，而不是规定“一格多少兵”。

基础模型：

```text
FormationArea
=
PersonCount
×
AreaPerPerson
```

然后根据阵型宽深比例：

```text
Width
Depth
```

形成：

`Formation Boundary`

---

# 17. AreaPerPerson

不同兵种、状态和阵型可以拥有不同：

```text
AreaPerPerson
```

例如：

- 密集步兵；
- 普通步兵；
- 弓弩展开；
- 骑兵；
- 行军纵队。

具体平方米数：

**尚未冻结。**

必须通过未来战争原型校准。

---

# 18. Formation Boundary

Formation拥有：

```text
AnchorPosition
Facing
Width
Depth
Boundary
```

Boundary会覆盖：

```text
若干PlanningCell50m
```

但：

```text
Formation ≠ 一个Cell
```

---

# 19. Cell可用面积

一个50m Cell：

```text
50 × 50
=
2500㎡
```

但有效可用空间可能减少。

概念上：

```text
CellUsableArea
=
2500㎡
-
建筑
-
水体
-
不可通行区域
-
其他物理障碍
```

Formation Boundary必须考虑真实可用空间。

---

# 20. Movement基本原则

军队不是：

```text
一回合移动N格
```

也不是：

```text
格A瞬移格B
```

正式方向：

```text
50m Cell
负责高层路径 / 通行 / 地形成本

WorldPosition
负责Formation真实位置

Formation Anchor
沿世界空间移动
```

---

# 21. 两级寻路思想

## 一级

使用：

```text
PlanningCell topology
Road
Water
Wall
Gate
Terrain
```

决定高层路线。

## 二级

在局部世界空间中：

使用：

```text
Road Geometry
Facility Footprint
Wall Geometry
Entrance
```

形成连续移动路线。

是否使用Unity NavMesh：

只能作为HOT表现辅助。

不得成为永久世界权威移动真相。

---

# 22. 道路与Formation

道路不是Cell中心连线。

正式Road拥有：

```text
Road Geometry
```

Formation可沿真实道路移动。

道路影响：

```text
移动速度
可通过宽度
行军组织
```

---

# 23. 阵型变化

Formation进入：

```text
道路
城门
山谷
桥梁
城市街道
```

时，可以改变：

```text
Width
Depth
```

例如：

```text
Battle Line
→ March Column
```

具体自动变阵与玩家命令规则：

以后原型决定。

---

# 24. 城门 / 桥 / 通道

未来通道可以具有：

```text
PassageWidth
```

Formation当前：

```text
Width
```

如果超过通道宽度：

不能以当前阵型通过。

可以：

```text
Change Formation
```

之后通过。

这是后续可能形成：

- 城门瓶颈；
- 桥梁瓶颈；
- 山谷瓶颈；

的重要玩法。

---

# 25. 野战不创建独立Battle Map

野战直接发生在：

`C 县域`

真实空间。

战场可能位于：

```text
平原
农田
官道
树林
山坡
河边
村庄
城外
```

地形本身就是战争条件。

---

# 26. 野战Formation最低状态

未来V1候选至少需要：

```text
FormationId
ArmyId

PersonCount
TroopComposition

AnchorPosition
Facing

FormationType
Width
Depth

Morale
Cohesion
Fatigue

CurrentOrder
```

最终字段以正式原型为准。

---

# 27. FormationType

首版无需几十种阵法。

候选最小集合：

```text
Line
Column
Dense
Loose
```

这里只是原型方向。

具体是否保留这四种：

尚未正式冻结。

---

# 28. 野战接触核心：Combat Frontage

野战不应该：

```text
整个Formation所有人同时攻击
```

真正参与直接近战的人数由：

`Combat Frontage`

决定。

两支Formation的Boundary发生接触：

```text
Boundary A
vs
Boundary B
```

得到实际接触长度。

接触长度决定：

当前能够直接参与战斗的人数。

---

# 29. 后排人员仍然有作用

未处于Frontage的人员并非无效。

他们承担：

```text
纵深
轮换
补充
维持阵型
预备
士气
```

因此大Formation仍然具有纵深意义。

---

# 30. 战斗胜负不是“总战斗力数字直接对撞”

禁止最终简化成：

```text
A战斗力 50,000
B战斗力 45,000
A直接胜利
```

正确方向：

```text
先由空间决定谁正在接战
↓
再由接战人员质量产生Combat Pressure
↓
产生伤亡 / Morale / Cohesion变化
↓
Formation推进 / 后退 / 崩溃
↓
下一轮重新计算
```

---

# 31. 每个Person的战斗能力应该影响战争

每个Person未来可以向战斗系统贡献：

```text
Attack
Defense
Ranged
Endurance
MoraleWeight
Exposure
```

具体字段根据现有人物属性系统映射。

不得平行创建第二套人物属性。

---

# 32. Person Combat Contribution

个人战斗贡献可以来源于：

```text
人物属性
技能
训练
武器
护甲
盾牌
健康
伤势
疲劳
经验
```

因此：

训练、装备和人物培养最终真正影响战争。

---

# 33. FormationCombatSummary

大规模战争不能每个Combat Tick重新扫描全部Person。

Formation应维护可增量更新的：

`CombatSummary`

例如：

```text
HealthyCombatants
AverageAttack
AverageDefense
AverageRanged

WeaponDistribution
ArmorDistribution
Training
VeteranRatio
```

人员：

- 加入；
- 死亡；
- 受伤；
- 换装备；

时更新。

---

# 34. 战斗结算核心候选

概念模型：

```text
AttackPressure
=
ActiveCombatants
×
CombatQuality
×
Formation
×
Terrain
×
Morale
×
Fatigue
×
Leadership
×
Flank
```

防御侧对应：

```text
DefenseResistance
```

具体数学公式：

**当前不冻结。**

必须通过未来战斗原型校准。

---

# 35. 战斗结果至少不只有死亡

一次战斗结算应该产生：

```text
死亡
受伤
Morale变化
Cohesion变化
Fatigue变化
推进 / 后退
```

---

# 36. Morale

Formation不应该：

```text
必须死到0
才失败
```

人员还很多也可能因为：

```text
高伤亡
侧击
背击
将领受伤 / 阵亡
被包围
邻阵溃败
疲劳
```

发生：

```text
Routing
```

---

# 37. Cohesion

Cohesion代表：

> 阵型还能不能作为一个有效组织继续战斗。

可能发生：

```text
Steady
Shaken
Disordered
Routing
```

具体状态名称以后可以调整。

---

# 38. 战斗胜利

局部战线胜利主要由：

```text
敌方撤退
敌方溃败
敌方投降
敌方失去目标区域
```

决定。

不是只比较谁杀死更多人。

整个Army胜负以后再综合：

```text
主要阵线
关键地形
Army Morale
退路
指挥
主动撤退
```

---

# 39. 伤亡最终必须回写真实Person

Formation层可能计算：

```text
DeathCount
SevereWoundCount
LightWoundCount
CapturedCount
```

之后再从真实Roster选择：

具体哪些Person：

- 死亡；
- 受伤；
- 被俘。

因此战斗结果真正进入永久人口世界。

---

# 40. 小规模Person Combat

小规模战斗可以采用更高Person粒度。

例如：

```text
个人决斗
抓捕
庄园械斗
商队冲突
数十人袭击
```

可以真的让Person拥有：

```text
位置
攻击目标
个人战斗
受伤
```

---

# 41. 大规模Formation Combat

人数扩大后：

采用Formation结算。

但是底层成员仍然是真实Person。

---

# 42. Person / Formation切换阈值

具体：

```text
多少人以下Person级
多少人以上Formation级
```

当前：

**不冻结。**

必须未来通过：

```text
30
100
300
500
1000
```

等规模压力原型后决定。

---

# 43. Named Person

玩家、重要武将、历史人物等：

可以在Formation中拥有更高Presentation和Combat Detail。

但是：

重要人物的高细节：

不得迫使所有普通士兵也使用同样计算粒度。

---

# 44. 野战远程

远程战斗权威层不要求每支箭做完整物理命中。

可按：

```text
射程
LOS
高程
射手人数
武器
目标密度
目标阵型
遮挡
```

结算Volley。

箭矢模型主要属于Presentation。

---

# 45. 高度

继续采用既定高度基础：

```text
EffectiveElevation
=
GroundElevation
+
StructureHeight
+
UnitHeightOffset
```

用于未来：

```text
LOS
射界
遮挡
```

---

# 46. 骑兵

未来骑兵可使用：

```text
速度
方向
阵型
目标状态
地形
```

形成：

`Charge`

主要影响：

```text
伤亡
Morale
Cohesion
```

不需要模拟每匹马的完整永久物理。

---

# 47. 侧击与背击

Formation拥有：

`Facing`

因此可根据攻击方向判断：

```text
Front
Flank
Rear
```

侧击 / 背击主要影响：

```text
Morale
Cohesion
Combat Efficiency
```

---

# 48. 建筑战斗总体方向

建筑不能只是：

```text
一个HP条
+
里面一个守军数字
```

但也不制作所有建筑的完整通用室内地图。

正式方向：

> 建筑拥有真实外部物理 + 轻量内部战斗结构。

---

# 49. Defensible Structure

可防守设施未来统一进入：

```text
DefensibleSpace / AssaultableStructure
```

概念。

适用于：

```text
城门
城墙附属
箭塔
军营
仓库
官署
庄园
大型市场院
其他可防守Facility
```

---

# 50. FacilityCombatProfile

未来可以表达：

```text
InteriorCapacity
GarrisonCapacity

Entrances[]
EntranceWidth[]

StructuralProtection
DefensiveCover

CombatZones[]
ZoneConnections[]

FireRisk
BreachPoints
```

具体字段以后开发时再冻结。

---

# 51. 不制作通用室内地图

普通建筑仍然：

```text
人物走到Entrance
↓
InsideFacility
```

战争也不要求：

```text
每个房间
家具
室内NavMesh
走廊
床位
```

全部物理模拟。

---

# 52. 建筑内部采用Combat Zone

例如：

普通仓库：

```text
Exterior
↓
Entrance
↓
Interior
```

大型官署：

```text
Exterior
↓
Main Gate
↓
Courtyard
↓
Inner Area
```

军营可能：

```text
Outer Gate
↓
Outer Camp
↓
Inner Camp
```

这些是逻辑战斗区。

不是完整室内地图。

---

# 53. 建筑攻坚核心：Combat Frontage

建筑内100人：

不代表100人同时交战。

真正同时接战人数受到：

```text
Door Width
Gate Width
Passage Width
Combat Zone Frontage
```

限制。

例如：

100名守军中：

可能只有：

```text
6
10
20
```

人同时在门口正面接战。

其余：

- 后备；
- 轮换；
- 支援；
- 等待。

---

# 54. 建筑入口攻坚

未来可形成：

```text
接近
↓
选择Entrance / Wall / WeakPoint
↓
攻击
↓
形成突破
↓
进入
↓
Combat Zone战斗
↓
守军死亡 / 溃败 / 投降
↓
占领
```

---

# 55. 统一设施战争动作

当前完整方向包括：

```text
攻击结构
攻击守军
强攻入口
招降
封锁
放火
绕过
```

但V1实现时不一定一次全部开发。

---

# 56. 建筑结构与守军必须分离

继续冻结：

```text
Structure
Defense
Control
Ownership
```

四者不能混成一个HP。

---

# 57. Structure

至少：

```text
Durability
OperationalState
```

---

# 58. Defense

至少：

```text
Garrison
Morale
CombatState
```

---

# 59. Control

至少：

```text
Controller
```

---

# 60. Ownership

继续：

```text
Owner
```

战争占领：

通常改变Controller。

不自动改变法律Owner。

---

# 61. Durability=0不代表自动占领

继续冻结：

```text
Durability = 0
→ Disabled / Breached / Non-operational
```

不代表：

```text
Controller自动变化
```

如果守军仍在：

仍然需要继续解决守军。

---

# 62. 守军消失不要求建筑先归零

如果：

```text
Garrison死亡
撤退
投降
```

攻击者实际取得控制：

可以完整占领：

```text
Durability仍然很高
```

的Facility。

---

# 63. 无守军设施

敌方Facility：

如果没有守军，并且攻击方满足实际军事控制条件：

可以直接占领。

不必：

先把建筑打坏。

---

# 64. Disabled与Destroyed分离

继续冻结：

```text
Disabled
=
普通维修可恢复

Destroyed
=
普通维修不能恢复
需要重建 / 重构
```

---

# 65. 城墙

城墙继续：

```text
Fortification on PlanningCell Edge
```

不是占据整个Cell的Facility。

---

# 66. Wall Segment

每段墙独立拥有：

```text
Durability
Height
Controller
Defense state
```

未来可以：

```text
受损
失能
形成Breach
被占领
```

---

# 67. Gate

Gate是：

```text
Wall上的特殊Passage Structure
```

拥有：

```text
Open
Closed
Locked
Controlled
Damaged
Destroyed
Breached
```

等未来状态。

---

# 68. 城门攻坚

未来可以形成：

```text
攻门
↓
破坏Gate
↓
守军压制
↓
进入Gate Passage
↓
夺取Gatehouse
↓
打开城内外通道
```

---

# 69. 箭塔 / 瞭望台

仍然是正常Facility。

拥有：

```text
Footprint
Height
Entrance
Durability
Capacity
Garrison
Controller
```

可以：

```text
攻击结构
压制守军
占领
摧毁
绕过
```

---

# 70. 攻城不创建第二张Siege Map

攻城仍在：

`C 县域`

当前真实空间。

使用：

```text
Terrain
Wall
Gate
Tower
Road
Facility
Formation
```

同一套世界事实。

---

# 71. 攻城与建筑攻坚共享底座

不能开发：

```text
SiegeCombatSystem
BuildingCombatSystem
FortCombatSystem
```

三个互不相干系统。

应尽量共享：

```text
Combat Frontage
Structure
Entrance
Garrison
Morale
Control
Damage
Occupation
```

---

# 72. 攻城器械

未来：

```text
攻城高台
投石设备
撞门设备
营寨
```

都应成为真实县域空间对象 / Facility / 工事。

本文件只记录方向。

当前不实现。

---

# 73. 战争中的50m Grid

当前候选方向：

Grid不是战斗视觉必须永久存在的棋盘。

而是：

> 战术解释工具。

尤其：

```text
部署
移动命令
攻城工事
城墙Edge
控制区
高程
射界
```

时显示。

具体最终表现：

以后视觉原型决定。

---

# 74. 战争性能原则

必须冻结：

```text
真实Person
≠
实时独立AI Agent
```

---

# 75. Formation作为主要高频战争单位

超大规模战争可能拥有：

```text
几十万真实Person
```

但高频权威计算对象应主要是：

```text
几十个 / 数百个Formation
```

而不是几十万个Person Agent。

---

# 76. Render Soldier与真实人数分离

继续冻结：

```text
Simulation Population
≠
Rendered Soldier Count
```

例如：

```text
Formation真实5000人
```

画面可以根据LOD只显示：

```text
少量代表士兵
```

真实战斗仍按5000人计算。

---

# 77. 代表士兵

未来可以使用：

```text
GPU Instancing
Batch
Representative Soldiers
```

营造军阵密度。

不要求：

一真实Person = 一个GameObject。

---

# 78. Combat Update Frequency

权威战争不需要：

```text
60次/秒
```

与画面FPS完全一致。

未来可以使用不同频率：

```text
Formation Movement
Combat Frontage
Morale
Logistics
```

分层更新。

具体频率：

当前不冻结。

---

# 79. HOT / COLD

战争继续遵守：

> HOT/COLD只改变Presentation成本，不改变战争结果。

当前正在观察：

可以显示：

- 士兵；
- 动画；
- 箭矢；
- 尘土。

COLD：

继续按同一正式战争规则推进。

---

# 80. Camera不能改变战斗结果

禁止：

```text
打开战场
→ 战斗开始

关闭战场
→ 战斗暂停

Near
→ 更精确所以结果变化
```

Observation不能改变WorldState。

---

# 81. 超大型战争

几十万人规模时：

不要求：

所有人员同时进入Near战术细节。

Army可以分为：

```text
前军
中军
后军
左右翼
预备
后勤
```

其中真正接战部分进入高细节Formation Combat。

其余保持：

```text
Reserve
March
Camp
Support
```

状态。

---

# 82. 小规模与大会战共享人物账

无论：

```text
10人械斗
100人庄园战
5000人野战
10万人大会战
```

最终：

```text
死亡
受伤
俘虏
```

都写回同一真实Person世界。

---

# 83. 当前尚未冻结的重要参数

以下全部必须保留为：

`TO BE PROTOTYPED`

不能在本文件写成正式常数：

```text
AreaPerPerson具体数值
Formation推荐人数
Formation最小/最大人数
Person Combat切换阈值
战斗Tick频率
伤亡公式
士气公式
Cohesion公式
阵型具体宽深比
骑兵冲锋公式
远程Volley公式
建筑Combat Zone具体规模
Entrance Frontage转换公式
```

---

# 84. 后续正式战争开发前必须先做性能原型

未来战争开工前：

先做技术原型矩阵，例如：

```text
100 vs 100
1,000 vs 1,000
5,000 vs 5,000
20,000 vs 20,000
50,000 vs 50,000
100,000 vs 100,000
```

记录：

```text
Person数量
Formation数量
Frontage数量
Cell Query
Battle Tick
Casualty assignment
Memory
GC
FPS
Rendered soldiers
```

再冻结战斗粒度。

---

# 85. 当前战争开发状态

正式记录：

```text
WARFARE ARCHITECTURE
=
DESIGN RECORDED

WARFARE IMPLEMENTATION
=
DEFERRED
```

不得因为本设计文档已经详细：

就把系统状态写成：

```text
WARFARE IMPLEMENTED
```

---

# 86. 当前开发重心

当前正式优先级重新收口为：

```text
1. 洛阳县域地图Presentation
2. Golden Block
3. 建筑模型美化
4. 模块化院落和建筑Art Pipeline
5. 建设模式显示正式50m PlanningCell
6. Facility / 城邑 / 村落 / 庄园视觉
7. Far / Mid / Near整体美术规则
8. 全洛阳推广
```

战争设计暂时停止。

---

# 87. 最近建筑方向补充

当前Golden Block已经证明：

```text
建筑进入World-Space
```

技术方向成立。

下一阶段重点不是战争。

而是解决：

```text
建筑过度抽象
盒子 + 屋顶感强
屋顶重复
院落层次不足
地面生活层不足
大型Facility只是放大普通建筑
```

---

# 88. 建筑美术下一步重点

优先完善：

```text
建筑轮廓
屋顶类型
屋脊
檐口
台基
门楼
院墙
侧房
仓棚
市场摊位
作业物
夯土地
石板
树木
井
货架
木料
推车
旗帜
兵器架
```

---

# 89. Golden Block继续作为唯一主要美术试验场

不要先全县同时修改2,084个Facility。

优先：

```text
Golden Block
↓
达到可接受正式美术规则
↓
抽取模块和程序规则
↓
推广全洛阳
```

---

# 90. 建设模式Cell规则

再次明确记录：

进入：

`建设规划`

以后显示的方格：

就是现有正式：

```text
50m PlanningCell
```

不是新增：

```text
5m Build Cell
10m Build Cell
```

普通县域：

```text
50m Grid隐藏
```

建设：

```text
50m Grid显示
```

退出建设：

```text
50m Grid隐藏
```

建筑继续：

```text
Position
Rotation
Footprint
Entrance
```

不等于Cell。

---

# 91. 战争文档到此冻结

以后如果重新开始讨论战争：

必须先读取本文件。

新结论：

应更新本文件。

不得重新从零讨论：

- Cell战争空间；
- Formation；
- Person；
- 建筑攻坚；
- Combat Frontage；
- Owner / Controller；
- 野战与攻城统一；

除非明确决定推翻现有设计。
