# 任务书：洛阳 Golden Block 建筑艺术语言、模块化院落与 Mid 视距定型 V3

## 一、任务定位

### 1.1 任务名称

**洛阳 Golden Block 建筑艺术语言、模块化院落与 Mid 视距定型 V3**

建议任务文件名：

`TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md`

建议实施报告：

`REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md`

### 1.2 任务性质

本任务是：

**县域建筑美术正式定型任务。**

当前已经完成或正在形成：

```text
洛阳50m县域世界空间
↓
Golden Block 400m×400m样板街区
↓
模块化建筑技术原型
↓
50m PlanningCell建设模式
```

但当前建筑仍然明显属于：

```text
Blockout
技术样板
程序化盒体
```

还没有形成：

> **能够推广到整个洛阳、以后推广到其他县域的东汉建筑视觉语言。**

因此本任务不继续扩大系统范围。

本任务只集中解决：

```text
建筑到底应该长什么样
院落应该怎么组织
不同Facility如何一眼区分
Far / Mid / Near分别显示什么
如何在保持批量渲染的同时去掉“积木感”
```

---

# 二、当前开发主线

当前正式优先级继续保持：

```text
县域地图
↓
Golden Block
↓
建筑美术
↓
建筑Presentation Pipeline
↓
50m Cell建设模式
↓
全洛阳建筑推广
↓
县域整体视觉Pass
↓
正式ConstructionProject
```

战争系统：

```text
Design Recorded
Implementation Deferred
```

本任务禁止继续战争实现。

---

# 三、任务核心目标

本任务完成以后：

Golden Block中的：

```text
住宅
市场
工坊
仓廪
官署
```

五类Facility在普通Mid视距下：

**不看标签也应该具有明显不同的建筑轮廓和空间组织。**

建筑从：

```text
盒子
+
简单坡屋顶
+
少量换色
```

升级为：

```text
Facility
↓
BuildingPresentationProfile
↓
Compound Layout
↓
主建筑
↓
侧房 / 附属建筑
↓
屋顶体系
↓
台基
↓
门楼 / 院门
↓
院墙
↓
地面处理
↓
生活 / 生产道具
↓
树木 / 植被
↓
Far / Mid / Near LOD
```

---

# 四、本任务最重要的验收问题

最终必须真正回答：

> **如果关闭所有建筑名称和开发标签，玩家还能不能大致判断哪里是住宅、市场、工坊、仓廪和官署？**

如果答案仍然是否定：

本任务不能视为完成。

---

# 五、硬架构边界

本任务只修改：

```text
Presentation
Art Pipeline
Building Modules
Materials
LOD
Batch Rendering
Golden Block Presentation
```

不得修改：

```text
World Schema
FacilityId
Facility Position
Facility Rotation
Facility Footprint
Facility Entrance
Population
Person
Inventory
Production
Capacity
Owner
Controller
Road Authority
Water Authority
Fortification Authority
WorldTime
```

---

# 六、Golden Block继续只是Presentation样板区

Golden Block继续保持：

```text
400m × 400m
```

或开工时实际正式尺寸。

它不得成为：

```text
GoldenBlockWorld
第二套城市
第二套Facility
第二套经济
```

---

# 七、开工必须重新读取实际Golden Block状态

记录：

```text
实际Golden Block坐标
实际宽高
实际PlanningCell范围
实际表现地块数量
实际Renderer数量
实际Material数量
实际模块数量
现有五类建筑规则
```

任务书中的历史数字：

```text
400m × 400m
8×8个50m PlanningCell
16个Presentation Plot
8—12个合批Renderer
```

只作为当前参考。

以实际仓库为准。

---

# 八、正式空间原则继续保持

M天下：

```text
2000m Strategic Cell
```

C县域：

```text
50m PlanningCell
```

建设模式显示的格子：

就是现有正式：

`PlanningCell50m`

---

# 九、本任务禁止新增微型Cell

不得新增：

```text
5m Cell
10m Cell
Build Cell
Sub Cell
Micro Cell
```

普通县域：

```text
50m Grid隐藏
```

建设规划：

```text
50m Grid显示
```

退出建设：

```text
50m Grid隐藏
```

---

# 十、Facility继续不等于Cell

建筑美化以后仍然保持：

```text
Facility
=
Position
Rotation
Footprint
Height
Entrance
```

例如：

```text
20×30m住宅
```

仍然可以只占：

一个50m PlanningCell的一部分。

大型官署可以跨多个Cell。

---

# 十一、本任务第一原则：先解决轮廓，再解决细节

优化顺序必须：

```text
Silhouette
↓
Mass / Proportion
↓
Compound Layout
↓
Roof Language
↓
Foundation
↓
Wall / Gate
↓
Ground Treatment
↓
Props
↓
Material
↓
Small Detail
```

不得：

先增加窗户、瓦片、雕刻和模型面数，

却继续保留：

```text
所有建筑都像矩形盒子
```

---

# 十二、建立正式BuildingPresentationProfile

必须建立或整理现有：

`BuildingPresentationProfile`

或等效Presentation数据结构。

目的：

```text
FacilityDefinition
↓
Presentation配置
↓
模块组合
```

而不是：

```text
Controller
if category == Residential
...
if category == Market
...
```

无限硬编码。

---

# 十三、BuildingPresentationProfile最低表达能力

概念上至少需要：

```text
FacilityDefinitionId / Category

PresentationCategory
PresentationImportance

CompoundStyle
AxisPreference
SymmetryPreference

MainModuleSet[]
SecondaryModuleSet[]
AuxiliaryModuleSet[]

RoofFamily
RoofVariationSet

FoundationFamily
StepFamily

WallFamily
GateFamily

GroundTreatment

PropSet
VegetationStyle

Density
OpenSpaceRatio

FarPresentationMode
MidPresentationMode
NearPresentationMode

ScaleCalibration
HeightFallback

StableVariationRule
```

具体字段名称根据现有代码风格决定。

---

# 十四、普通美术数据不得进入World Schema

BuildingPresentationProfile属于：

```text
Presentation Content
```

可以：

- 修改；
- 新增；
- 版本化；
- 重建缓存。

不得升级正式世界存档。

---

# 十五、建立统一模块库

本任务重点不是制作几十栋完整独立建筑。

而是建立：

> **少量高质量、高复用、可以组合的模块库。**

---

# 十六、基础屋体模块

至少考虑：

```text
SmallHouse
MediumHouse
LargeHall
SideHouse
LongHouse
LongWarehouse
WorkshopHall
WorkshopShed
OpenShed
GateHouse
FormalGateHouse
TowerBody
CorridorSegment
```

是否全部实现：

根据现有资源和任务范围。

但至少需要覆盖五类Golden Block建筑。

---

# 十七、基础屋体必须有真实体量差异

不能继续：

```text
同一个Cube
Scale X/Y/Z
```

形成所有建筑。

至少通过：

```text
墙身高度
长度
宽度
柱 / 墙面节奏
入口位置
屋檐关系
```

形成不同体量。

---

# 十八、屋顶体系是本任务S1重点

当前建筑抽象感的重要来源之一：

> 屋顶形态太相似。

因此必须建立：

`Roof Family`

而不是只建立：

`Roof Color`

---

# 十九、首批屋顶至少应形成

```text
普通民居双坡屋顶
侧房低屋顶
仓储长屋顶
工坊较低宽屋顶
官署较高等级屋顶
门楼屋顶
大型堂屋屋顶
塔楼屋顶
```

不要求考古级最终定型。

但必须体现：

```text
功能
体量
等级
```

差异。

---

# 二十、屋顶最低结构要求

至少考虑：

```text
坡度
屋脊高度
屋脊厚度
屋檐外挑
屋顶厚度
屋面长度
屋面宽度
```

避免：

```text
三角棱柱直接扣在盒子上
```

---

# 二十一、屋檐

Mid距离必须能看见：

```text
檐口外挑
檐下阴影
```

这是去除“纸盒感”的重要元素。

---

# 二十二、屋脊

屋脊不只是装饰线。

应成为：

建筑轮廓组成部分。

---

# 二十三、建筑墙体不能继续是纯无层次方盒

至少加入有限：

```text
基脚
墙面分段
柱 / 立面节奏
入口凹凸
檐下过渡
```

不要求复杂木构精雕。

---

# 二十四、建立Foundation体系

建筑与地面之间必须增加：

```text
Foundation
```

---

# 二十五、首批Foundation候选

```text
LowEarthFoundation
RaisedEarthFoundation
FormalFoundation
StoneEdgeFoundation
```

具体保持东汉风格候选和项目现有资产能力。

---

# 二十六、建筑不得继续直接插入Terrain

正确关系：

```text
Terrain
↓
Ground Treatment
↓
Foundation
↓
Building
```

---

# 二十七、入口台阶

根据建筑等级：

允许：

```text
普通低台阶
正式台阶
门前平台
```

---

# 二十八、Facility应该大量采用Compound表现

正式冻结：

> **Facility不等于一栋房。**

一个Facility可以由多个Presentation模块组成。

---

# 二十九、Compound仍然只对应一个FacilityId

例如：

```text
FAC-GOV-001 官署
```

Presentation可以是：

```text
正堂
左侧房
右侧房
门楼
院墙
前院
树木
地面
```

但世界中仍然只有：

```text
FAC-GOV-001
```

---

# 三十、禁止模块转成正式Facility

以下默认只是Presentation：

```text
侧房
门楼
院墙
棚
树木
市场摊位
柴堆
木料
货物
井
装饰
```

除非当前世界已有对应正式对象。

---

# 三十一、Compound Layout必须受Footprint约束

所有Presentation模块：

原则上必须位于：

```text
Facility Footprint
```

或正式允许的附属Presentation范围内。

不得为了好看：

把官署侧房生成到邻居Facility里面。

---

# 三十二、Compound布局应尊重Entrance

主门 / 门楼：

尽量与正式：

```text
Facility Entrance
```

一致。

---

# 三十三、Compound布局应尊重道路

如果正式Entrance面向道路：

Presentation应尽量形成：

```text
Road
↓
门前空间
↓
Gate
↓
Courtyard
↓
Main Building
```

---

# 三十四、住宅建筑语言

住宅V3至少应形成：

```text
低体量
主屋
一个或多个侧房
生活院
普通院墙
普通院门
树木
柴堆 / 水缸 / 日常小物
```

---

# 三十五、住宅不应全部相同

通过Stable Variation允许：

```text
单侧房
双侧房
长屋
不同院墙
不同树木
不同屋顶组合
```

---

# 三十六、市场建筑语言

市场V3核心不是：

```text
一栋Market Building
```

而是：

```text
开放空间
+
摊位
+
棚架
+
货物
+
道路朝向
+
通行空间
```

---

# 三十七、市场Open Space Ratio必须明显高于住宅

市场不能被建筑填满。

---

# 三十八、市场建筑应围绕交易空间组织

目标视觉：

```text
Road
↓
入口开放
↓
摊位 / 棚
↓
交易空地
↓
少量固定建筑
```

---

# 三十九、工坊建筑语言

工坊V3至少：

```text
主工坊
作业棚
材料堆
生产院
粗糙地面
辅助仓棚
```

---

# 四十、工坊不能靠颜色区分

玩家应通过：

```text
作业空间
材料
棚
开放院
```

识别。

---

# 四十一、仓廪建筑语言

仓廪至少：

```text
长体量仓房
较规整排列
较大门
装卸场
少量辅助建筑
货堆
```

---

# 四十二、仓廪不能只是住宅拉长

LongWarehouse应有独立：

```text
比例
屋顶
墙体
门
```

语言。

---

# 四十三、官署建筑语言

官署是Golden Block本轮最重要高级样板。

必须体现：

```text
轴线
门楼
前院
正堂
侧房
较正式院墙
较高等级台基
较正式屋顶
规整地面
```

---

# 四十四、官署应该通过空间秩序体现等级

不是：

```text
把普通住宅Scale放大
```

---

# 四十五、大型Facility规则

任何大型Facility：

优先通过：

```text
模块数量
院落数量
建筑等级
台基
屋顶
门楼
```

增加体量。

禁止主要依赖：

```text
Global Scale
```

---

# 四十六、建立Presentation Importance

建议使用：

```text
P0 Ordinary
P1 Significant
P2 Major
P3 Landmark
```

或现有等效规则。

---

# 四十七、Presentation Importance只影响美术表现

它可以影响：

```text
模块复杂度
Far显示
Mid展开距离
Near细节
地标优先级
```

不得影响：

```text
生产
容量
防御
产权
```

---

# 四十八、P0 Ordinary

例如：

```text
普通住宅
小工坊
普通小仓
```

Far：

聚合。

Mid：

正常显示。

Near：

完整基础细节。

---

# 四十九、P1 Significant

例如：

```text
市场
大型工坊
较大仓储
```

Mid更早展开。

---

# 五十、P2 Major

例如：

```text
大型官署
大型军营
重要仓储
```

Far可以保留明显轮廓。

---

# 五十一、P3 Landmark

例如正式数据能够确定的：

```text
宫殿
太学
明堂
重要城门
重要塔楼
```

Far独立保留。

本任务Golden Block不要求制作全部P3。

只建立管线兼容能力。

---

# 五十二、Mid是本任务主要视觉距离

本轮所有建筑审阅优先：

```text
Mid
```

原因：

未来县域经营大多数时间会停留在这一尺度。

---

# 五十三、Mid必须实现的画面质量

用户应能看见：

```text
院落
屋顶差异
建筑体量
道路
市场开放空间
工坊生产院
仓储装卸空间
官署轴线
院墙
树木
地面
```

---

# 五十四、Far不需要Near模型复杂度

Far主要显示：

```text
Roof Mass
Urban Fabric
Major Buildings
Landmarks
```

不要渲染：

- 摊位小物；
- 柴堆；
- 台阶小细节；
- 全部院内道具。

---

# 五十五、Near增加细节

Near允许增加：

```text
门
台阶
柱
货物
市场摊位
作业物
井
柴堆
树木
院内小径
```

---

# 五十六、Far / Mid / Near必须共享同一Facility

LOD切换：

不得重建世界对象。

---

# 五十七、建筑与地面关系作为独立系统处理

必须建立：

`GroundTreatment`

或复用现有等效。

---

# 五十八、GroundTreatment最低类别

至少：

```text
ResidentialYard
MarketGround
WorkshopGround
GranaryLoadingGround
GovernmentCourtyard
```

---

# 五十九、住宅地面

应体现：

```text
生活院
夯土
少量草土混合
```

---

# 六十、市场地面

应体现：

```text
高踩踏开放地面
交易区
通行区
```

---

# 六十一、工坊地面

应体现：

```text
作业场
裸土
材料使用痕迹
```

---

# 六十二、仓廪地面

应体现：

```text
装卸区
较规整硬地
```

---

# 六十三、官署地面

应体现：

```text
较整齐前院
正式通行轴线
```

---

# 六十四、本任务不要求真实污渍模拟

GroundTreatment首先是：

Presentation。

不发展：

动态泥泞 / 磨损系统。

---

# 六十五、道路与院落之间要有过渡

不能：

```text
Road Ribbon
直接撞进Building Mesh
```

至少有：

```text
门前空地
入口连接
```

---

# 六十六、院墙

本任务至少形成两个等级：

```text
Ordinary Wall
Formal Wall
```

如当前资源支持：

可增加：

`Simple Fence`

---

# 六十七、院墙高度和厚度要合理

不能：

住宅院墙比官署正堂还高。

---

# 六十八、门楼

门楼是重要轮廓资产。

至少：

```text
普通院门
普通门房
正式门楼
```

形成等级差。

---

# 六十九、生活 / 生产道具系统

建立：

`PropSet`

数据驱动。

---

# 七十、住宅PropSet

候选：

```text
柴堆
水缸
木架
小推车
树木
```

---

# 七十一、市场PropSet

候选：

```text
摊位
棚
货架
箱袋
推车
```

---

# 七十二、工坊PropSet

候选：

```text
木料
作业架
容器
工棚物件
```

---

# 七十三、仓廪PropSet

候选：

```text
袋
箱
货堆
装卸物
```

---

# 七十四、官署PropSet

应更加克制。

不要把官署堆满民间杂物。

---

# 七十五、Prop不进入正式库存

如果只是Presentation：

不得因为画了：

```text
十个粮袋
```

就增加：

ProductBatch。

---

# 七十六、树木和植被

Golden Block树木用于：

```text
尺度
阴影
生活感
院落分隔
```

不能遮挡主要建筑。

---

# 七十七、不同Compound可拥有不同VegetationStyle

例如：

住宅：

稍多生活树木。

市场：

少。

仓廪：

少。

官署：

较规整。

---

# 七十八、Stable Variation必须全面使用

同类建筑产生变化时：

必须根据：

```text
Stable FacilityId
或
稳定Presentation Key
```

生成。

---

# 七十九、Stable Variation可以决定

```text
屋顶变体
侧房数量
左右侧房
院墙变化
树木
Prop组合
轻微地面差异
```

---

# 八十、不得用运行时随机

禁止：

```text
UnityEngine.Random
System.Random未固定Seed
DateTime
FrameCount
GetInstanceID
```

---

# 八十一、Golden Block样板Presentation-only建筑处理

当前Golden Block可能存在：

```text
Presentation-only模块
```

必须明确区分：

```text
Formal Facility Driven
Presentation Sample
```

---

# 八十二、样板模块不改变Facility数量

美化以后仍然必须满足：

```text
Formal Facility Count Before
==
Formal Facility Count After
```

---

# 八十三、建筑模型缩放问题

本任务必须检查现有资产：

```text
Prefab Bounds
vs
Facility Footprint
```

---

# 八十四、建立或复用Asset Scale Calibration

不得：

```text
所有模型统一Scale×N
```

---

# 八十五、比例验收

至少检查：

```text
门高
人尺度
墙高
建筑高度
道路宽度
院落大小
```

视觉关系。

---

# 八十六、可以加入统一Human Scale Reference

仅作为Debug：

在Golden Block Near中允许显示一个：

```text
人形比例参考
```

帮助检查建筑尺寸。

普通玩家默认隐藏。

---

# 八十七、Building Ghost继续复用正式建筑管线

建设模式Ghost：

必须直接复用：

```text
BuildingPresentationProfile
Compound Layout
```

---

# 八十八、Ghost不得退回盒子

住宅Ghost：

至少看起来像住宅Compound。

官署Ghost：

至少看起来像官署Compound。

---

# 八十九、Ghost只更换规划材质

尽量：

```text
正式模型
↓
Ghost Material Override
```

而不是第二套Ghost模型。

---

# 九十、Ghost Footprint仍由正式Placement系统决定

Presentation模块不能扩出合法Footprint后：

仍显示Valid。

---

# 九十一、建设模式继续使用50m Cell

本任务必须保证建筑美术升级后：

```text
普通模式
Grid OFF

建设模式
50m PlanningCell Grid ON
```

继续正常。

---

# 九十二、50m Grid不能被建筑美术破坏

包括：

- 被GroundTreatment遮住；
- Z-Fighting；
- 被大屋顶完全挡住；
- Hover看不清。

---

# 九十三、Grid与建筑层级

建议：

```text
Grid
→ 地面层

Footprint
→ 地面高亮层

Building Ghost
→ 世界空间

Entrance Marker
→ Above Ground UI
```

---

# 九十四、本任务不改变Grid语义

不得因为建筑小于Cell而增加细分Grid。

---

# 九十五、建筑在Cell内部的具体位置

仍然读取：

现有正式：

```text
Position
```

或Draft Position。

不新增：

微型Cell索引。

---

# 九十六、Golden Block建设模式必须继续验证

```text
小建筑
<
50m Cell

大型建筑
>
50m Cell
```

两种情况。

---

# 九十七、住宅单Cell样例

至少准备：

一个明显小于50m×50m的住宅候选。

---

# 九十八、大型官署MultiCell样例

至少准备：

一个跨多个50m Cell的官署或大型候选。

---

# 九十九、Covered Cell只是辅助

不得让：

```text
CoveredPlanningCells
```

替代：

真实Footprint。

---

# 一百、入口必须通过模型可看懂

Near / Build Mode中：

主要Entrance应能在模型上找到对应：

```text
门 / 门楼
```

---

# 一百零一、道路关系

模型的主门朝向：

应尽量匹配正式：

`EntranceFacing`

和Road Access。

---

# 一百零二、发现Entrance数据异常时

不得Presentation自动改数据。

必须报告：

```text
Layout / Entrance Candidate Issue
```

---

# 一百零三、道路方向对Compound Layout的影响

允许：

```text
Compound内部模块布局
```

根据正式道路方向决定：

门楼位置
主轴方向
市场开放面。

但不得改变Facility Position。

---

# 一百零四、Mid建筑选择

BuildingPresentation升级后：

点击实际模型 / Selection Proxy：

必须仍然返回：

同一个正式FacilityId。

---

# 一百零五、Presentation模块不得各自成为选择对象

点击：

```text
官署侧房
```

默认仍然选择：

整个官署Facility。

---

# 一百零六、如果未来要选择子模块

另立正式：

`FacilitySubstructure`

设计。

本任务不做。

---

# 一百零七、材质体系

建立少量：

`Material Family`

---

# 一百零八、首批Material Family候选

```text
EarthWall
WoodStructure
OrdinaryRoof
FormalRoof
EarthFoundation
StoneFoundation
PackedEarth
RoadEarth
```

---

# 一百零九、禁止一Facility一个Material实例

优先：

```text
SharedMaterial
MaterialPropertyBlock
Atlas
Instancing
```

---

# 一百一十、建筑颜色必须统一古代城邑语境

目标：

```text
土
木
灰褐
低饱和屋顶
```

---

# 一百一十一、类别差异主要靠结构

禁止：

```text
住宅红
市场蓝
工坊黄
仓库绿
官署紫
```

功能颜色编码。

---

# 一百一十二、Far / Mid / Near材质策略

Far：

减少材质复杂度。

Mid：

主要视觉层。

Near：

增加有限细节。

---

# 一百一十三、批量渲染必须继续保持

当前Golden Block已经使用：

少量合批Renderer。

V3必须继续：

```text
Batch
GPU Instancing
Merged Mesh
```

方向。

---

# 一百一十四、禁止逐模块MonoBehaviour爆炸

不能：

```text
每个侧房
每个摊位
每个树
每个柴堆
```

都拥有自己的完整Update。

---

# 一百一十五、Prop优先实例化 / 合批

---

# 一百一十六、Vegetation优先实例化

---

# 一百一十七、Compound Build Cache

允许：

```text
BuildingPresentationCache
```

按Facility/样板Key缓存。

---

# 一百一十八、Cache不是世界存档

删除以后：

可从Profile + Stable Key重建。

---

# 一百一十九、Cache重建必须确定性

---

# 一百二十、V3第一阶段只做“建筑本体”

实施顺序必须严格。

第一阶段：

```text
轮廓
屋顶
墙体
台基
Compound
```

暂时少放Prop。

---

# 一百二十一、第一阶段视觉门

五类建筑在：

```text
灰模 / 基础材质
```

状态下，

就应该能初步区分。

如果灰模仍然分不出来：

禁止通过贴图、颜色、树木掩盖。

---

# 一百二十二、第二阶段才做环境结合

包括：

```text
Ground Treatment
院墙
道路连接
树木
```

---

# 一百二十三、第三阶段才加道具

---

# 一百二十四、第四阶段再验证建设模式

确保：

```text
50m Grid
Ghost
建筑美术
```

共存。

---

# 一百二十五、本任务不推广全洛阳

这是硬边界。

不得：

```text
Golden Block还没人工通过
↓
直接跑全县2084 Facility
```

---

# 一百二十六、本任务不重做全县Far

Far全县规则推广：

下一任务处理。

---

# 一百二十七、本任务不做村落最终美术

可以为未来模块兼容。

但本任务主要只做Golden Block。

---

# 一百二十八、本任务不做庄园最终美术

---

# 一百二十九、本任务不做城墙最终重建

Golden Block若能看到城墙：

保证兼容。

但正式城墙/城门美术另行深化。

---

# 一百三十、本任务不做水墨最终Shader

---

# 一百三十一、本任务不做ConstructionProject

禁止实现：

```text
材料
劳力
工期
资金
施工
完工
```

---

# 一百三十二、本任务不做正式拆迁

---

# 一百三十三、本任务不做建筑升级

---

# 一百三十四、本任务不做战争

战争文档已经归档。

禁止新增：

```text
Formation
Combat
Cell Capacity
Facility Combat Profile
Siege
```

代码。

---

# 一百三十五、本任务不做室内

继续：

```text
Entrance
↓
InsideFacility
↓
玩法 / 管理UI
```

---

# 一百三十六、历史与艺术边界

当前Golden Block：

是：

```text
东汉建筑风格游戏化候选
```

不得声称：

```text
精确复原某条洛阳街区
```

---

# 一百三十七、历史位置置信度不改变

现有：

`provisional`

仍然是：

`provisional`

---

# 一百三十八、资产许可要求

只使用：

```text
现有项目合法资产
原创程序模块
已登记许可资源
```

---

# 一百三十九、禁止复制商业游戏建筑

不得：

- 临摹具体模型；
- 提取贴图；
- 导入商业游戏资产；
- 复制UI；
- 复制Shader。

---

# 一百四十、Core测试：Profile解析

每类Golden Block Facility：

获得稳定合法Profile。

---

# 一百四十一、Core测试：五类差异

至少验证五类：

产生不同的：

```text
CompoundStyle
ModuleSet
RoofFamily
GroundTreatment
PropSet
```

---

# 一百四十二、Core测试：Stable Variation

同一输入：

重复生成相同Presentation Summary。

---

# 一百四十三、Core测试：不同Facility变化

两个不同Stable Key的同类住宅：

允许产生不同Presentation变体。

---

# 一百四十四、Core测试：模块不增加Facility

模块数量变化前后：

正式Facility数量不变。

---

# 一百四十五、Core测试：Footprint约束

所有Compound主体模块：

不得超出允许Presentation Footprint。

---

# 一百四十六、Core测试：Entrance对应

主门：

与正式Entrance方向一致。

---

# 一百四十七、Core测试：Asset Scale

Model Bounds经过Calibration后：

不得严重超出Facility Footprint。

---

# 一百四十八、Core测试：Far/Mid/Near

同一Facility在三档返回：

合法Presentation策略。

---

# 一百四十九、Core测试：Mid优先

五类Golden Block：

Mid全部存在可读表现。

---

# 一百五十、Core测试：Material数量

不允许Stable Variation导致：

大量唯一材质实例。

---

# 一百五十一、Core测试：Cache

删除Presentation Cache后：

重建Summary一致。

---

# 一百五十二、Core测试：50m Grid兼容

普通模式：

```text
Grid OFF
```

Build Mode：

```text
Grid ON
```

不受建筑V3影响。

---

# 一百五十三、Core测试：Ghost Profile

Ghost使用同一Profile。

---

# 一百五十四、Core测试：SingleCell建筑

住宅：

真实Footprint小于Cell时：

仍保持原尺寸。

---

# 一百五十五、Core测试：MultiCell建筑

大型官署：

正确覆盖多个PlanningCell。

---

# 一百五十六、Core测试：Rotation

模型、Compound、Footprint、Entrance：

同步旋转。

---

# 一百五十七、Core测试：No World Mutation

完整执行：

```text
打开Golden Block
↓
Mid观察
↓
Near观察
↓
进入建设
↓
显示50m Grid
↓
选择住宅
↓
Ghost
↓
R旋转
↓
创建Draft
↓
Undo
↓
退出建设
```

以后比较：

```text
WorldTime
Person
Population
FacilityIds
FacilityPosition
Inventory
Market
Road
Water
Fortification
Owner
Controller
WorldSummary
```

全部一致。

---

# 一百五十八、Unity EditMode最低覆盖

至少：

```text
BuildingPresentationProfile
Five Category Rules
Compound Layout
Roof Family
Foundation
Ground Treatment
Stable Variation
Scale Calibration
Cache
Facility Mapping
50m Grid Compatibility
Ghost
Rotation
No World Mutation
```

---

# 一百五十九、Unity PlayMode正式入口

使用：

```text
PlayableDemo
↓
C 县域
↓
样板街区
```

---

# 一百六十、PlayMode阶段A：关闭所有标签

必须提供开发审阅开关：

```text
Building Labels OFF
```

用于真正判断：

五类建筑是否可以凭视觉识别。

---

# 一百六十一、住宅验收

检查：

```text
主屋
侧房
院落
院墙
院门
生活地面
树 / 日常道具
```

---

# 一百六十二、市场验收

检查：

```text
开放空间
摊位
棚
货物
通行
道路关系
```

---

# 一百六十三、工坊验收

检查：

```text
作业院
工棚
材料区
生产感
```

---

# 一百六十四、仓廪验收

检查：

```text
长体量
大门
装卸场
仓储感
```

---

# 一百六十五、官署验收

检查：

```text
轴线
门楼
前院
正堂
侧房
台基
正式院墙
```

---

# 一百六十六、PlayMode灰模/基础材质审阅

建议提供一次：

```text
Reduced Material / Neutral Review
```

开发证据。

目的：

证明建筑差异不是主要依靠颜色。

不要求进入正式玩家UI。

---

# 一百六十七、PlayMode Mid验收

这是本任务最重要镜头。

必须：

```text
Mid
```

整体看起来已经从：

```text
程序积木街区
```

明显升级为：

```text
具有东汉建筑语言的街区
```

---

# 一百六十八、PlayMode Near验收

Near重点检查：

```text
门
台阶
檐口
墙
地面
道具
比例
```

---

# 一百六十九、PlayMode Far兼容

Golden Block缩远后：

建筑细节应合理简化。

不能：

所有小道具仍然高成本显示。

---

# 一百七十、建设模式验收

点击：

`建设规划`

---

# 一百七十一、必须显示正式50m Cell

无：

```text
5m
10m
其他微型格
```

---

# 一百七十二、Grid和新建筑必须同时可读

---

# 一百七十三、住宅Ghost

必须看起来像：

住宅Compound。

---

# 一百七十四、官署Ghost

必须保留：

官署主要轮廓。

---

# 一百七十五、Ghost Valid / Warning / Invalid

继续正常。

---

# 一百七十六、Footprint与模型一致

---

# 一百七十七、Entrance可辨

---

# 一百七十八、Road Access可理解

---

# 一百七十九、退出建设

Grid隐藏。

新建筑美术保持。

---

# 一百八十、720p验收

1280×720：

必须能：

- 区分主要建筑类型；
- 看懂道路；
- 建设Grid可用；
- UI不完全遮挡街区。

---

# 一百八十一、1080p重点验收

1920×1080：

作为正式Mid建筑美术验收基准。

---

# 一百八十二、截图要求

至少输出：

`01_golden_block_v2_before.png`

当前V2/旧样板。

`02_v3_neutral_silhouette_review.png`

弱材质/轮廓审阅。

`03_v3_golden_block_mid.png`

V3正式Mid。

`04_v3_golden_block_near.png`

Near。

`05_v3_residential.png`

住宅。

`06_v3_market.png`

市场。

`07_v3_workshop.png`

工坊。

`08_v3_granary.png`

仓廪。

`09_v3_government.png`

官署。

`10_v3_roof_families.png`

屋顶体系。

`11_v3_eaves_and_ridges.png`

檐口和屋脊。

`12_v3_foundations.png`

台基。

`13_v3_gatehouses.png`

院门/门楼。

`14_v3_courtyard_walls.png`

院墙。

`15_v3_ground_treatment_residential.png`

住宅地面。

`16_v3_ground_treatment_market.png`

市场地面。

`17_v3_ground_treatment_workshop.png`

工坊地面。

`18_v3_ground_treatment_granary.png`

仓储地面。

`19_v3_ground_treatment_government.png`

官署地面。

`20_v3_residential_props.png`

住宅生活层。

`21_v3_market_props.png`

市场。

`22_v3_workshop_props.png`

工坊。

`23_v3_granary_props.png`

仓廪。

`24_v3_stable_variations.png`

同类建筑确定性变化。

`25_v3_mid_labels_off.png`

关闭标签后的Mid。

`26_v3_far_lod.png`

Far兼容。

`27_v3_build_mode_grid.png`

正式50m Grid。

`28_v3_residential_ghost.png`

住宅Ghost。

`29_v3_government_ghost.png`

官署Ghost。

`30_v3_multicell_footprint.png`

大型建筑跨Cell。

`31_v3_entrance_road_relation.png`

入口与道路。

`32_v3_build_mode_exit.png`

退出建设Grid隐藏。

---

# 一百八十三、必须制作Before / After同镜头

至少：

```text
旧Golden Block
VS
V3 Golden Block
```

同一个Mid Camera。

---

# 一百八十四、Before / After核心评价

必须重点判断：

```text
是不是仍然像积木
五类建筑是否能认出来
大型建筑是否不再只是放大版住宅
建筑是否真正落地
院落是否形成
街区是否有生活层
```

---

# 一百八十五、性能记录

至少：

```text
V2 Renderer Count
V3 Renderer Count

Draw Calls
Batches
Triangles
Materials
Unique Materials
Visible Modules
Visible Props
Vegetation Instances

Far FPS
Mid FPS
Near FPS
Build Mode FPS

Golden Block Enter Time
Warm Re-enter Time

Presentation Build Time
Cache Rebuild Time

Ghost Update P50/P95
Grid Cost

Memory
GC
```

---

# 一百八十六、性能目标

不要求现在冻结最终全洛阳指标。

但V3必须证明：

> **这套美术语言有推广可能。**

不能得到：

```text
400×400m很好看
但是未来全洛阳绝对无法运行
```

的结果。

---

# 一百八十七、Renderer原则

继续使用：

```text
Batch
Instance
Merged Mesh
Shared Material
```

---

# 一百八十八、道具LOD

Far：

基本不显示小Prop。

Mid：

显示关键Prop。

Near：

显示更多。

---

# 一百八十九、Vegetation LOD

同理。

---

# 一百九十、S0阻断

以下任一情况不得完成：

- World Schema升级；
- FacilityId变化；
- Facility Position变化；
- Facility Entrance被Presentation改写；
- Golden Block新增正式Facility；
- Compound模块成为正式Facility；
- Presentation Prop修改Inventory；
- 新增5m/10m Build Cell；
- 50m PlanningCell建设规则被改变；
- Ghost创建正式Facility；
- WorldTime因Presentation变化；
- 普通建筑模型变化导致正式Capacity变化；
- 道路权威被美术模块改写；
- 未知许可证资产被接入；
- 战争代码被顺手开发；
- 正式ConstructionProject被提前开发。

---

# 一百九十一、S1阻断

以下问题修复前不得用户验收：

- 五类建筑关闭标签后仍难以区分；
- 住宅仍是单盒子；
- 市场仍像住宅换皮；
- 工坊没有作业空间；
- 仓廪仍像拉长住宅；
- 官署仍像放大住宅；
- 大型建筑主要靠Scale；
- 屋顶仍只有颜色差异；
- 檐口仍完全没有层次；
- 建筑大量直接插地；
- 官署缺少明显轴线和等级；
- 院墙与门楼比例怪异；
- 道具过度堆叠掩盖建筑问题；
- Stable Variation运行后每次不同；
- Material实例数量失控；
- Renderer/Draw Call明显爆炸；
- Ghost继续使用简单盒子；
- 50m Grid被新地面材质遮挡；
- 新建筑美术导致建设Selection失败；
- Mid表现仍明显像程序化积木；
- 720p无法阅读；
- 1080p Mid明显卡顿。

---

# 一百九十二、自动验收清单

## A. Art Language

- [ ] 住宅轮廓成立。
- [ ] 市场轮廓成立。
- [ ] 工坊轮廓成立。
- [ ] 仓廪轮廓成立。
- [ ] 官署轮廓成立。
- [ ] 关闭标签后可大致区分。
- [ ] 大型建筑不靠单纯Scale。

## B. Roof

- [ ] 多种屋顶形态。
- [ ] 屋脊存在。
- [ ] 檐口存在。
- [ ] 高低等级明显。
- [ ] 仓房/官署/民居屋面不同。

## C. Compound

- [ ] 主建筑。
- [ ] 侧房。
- [ ] 院落。
- [ ] 院墙。
- [ ] 院门/门楼。
- [ ] 仍然只对应一个Facility。

## D. Ground

- [ ] 建筑有台基。
- [ ] 住宅地面。
- [ ] 市场地面。
- [ ] 工坊地面。
- [ ] 仓储地面。
- [ ] 官署地面。
- [ ] 建筑不直接插入草地。

## E. Props

- [ ] 住宅适量。
- [ ] 市场适量。
- [ ] 工坊适量。
- [ ] 仓储适量。
- [ ] 官署克制。
- [ ] Prop不进入World Inventory。

## F. Pipeline

- [ ] BuildingPresentationProfile数据驱动。
- [ ] Stable Variation。
- [ ] 无Unity Random。
- [ ] Asset Scale Calibration。
- [ ] Presentation Importance。
- [ ] Far/Mid/Near LOD。
- [ ] 可缓存。
- [ ] 可重建。

## G. Performance

- [ ] Batch Renderer继续使用。
- [ ] Shared Material。
- [ ] Unique Material未失控。
- [ ] 无逐模块重Update。
- [ ] Mid操作流畅。
- [ ] 有推广全洛阳的可能。

## H. Build Mode Compatibility

- [ ] 普通模式50m Grid隐藏。
- [ ] Build Mode显示正式50m Grid。
- [ ] 无第二套Cell。
- [ ] Building Ghost复用Profile。
- [ ] SingleCell建筑保持真实尺寸。
- [ ] MultiCell建筑正确。
- [ ] Entrance正确。
- [ ] Rotation正确。
- [ ] Placement Validation不变。
- [ ] 退出后Grid隐藏。

## I. World State

- [ ] FacilityIds不变。
- [ ] Facility Position不变。
- [ ] Facility Rotation不变。
- [ ] Facility Footprint不变。
- [ ] Population不变。
- [ ] Person不变。
- [ ] Inventory不变。
- [ ] Production不变。
- [ ] Market不变。
- [ ] Road不变。
- [ ] Water不变。
- [ ] Fortification不变。
- [ ] Owner不变。
- [ ] Controller不变。
- [ ] WorldTime不变。
- [ ] WorldSummary不变。

## J. 工程

- [ ] 全工程编译通过。
- [ ] V3定向Core通过。
- [ ] Core全量通过。
- [ ] Project Load PASS或准确BLOCKED。
- [ ] EditMode PASS或准确BLOCKED。
- [ ] PlayMode PASS或准确BLOCKED。
- [ ] 任务范围diff check通过。
- [ ] 既有任务外FBX `.meta`问题未擅自修改。
- [ ] 无自动测试遗留Unity进程。
- [ ] 最终人工Review实例保持打开。

---

# 一百九十三、实施顺序

必须严格按照以下顺序实施。

## Step 0：开工快照

记录：

```text
HEAD
Branch
Workspace
World Schema
Unity版本
当前Unity PID
Golden Block坐标
Golden Block尺寸
PlanningCell范围
Facility数量
现有模块
现有Renderer
Material
现有Building Registry
现有Batch Renderer
现有LOD
现有BuildingPresentation实现
当前Core数量
Unity测试状态
```

---

## Step 1：固定V2 Before镜头

必须保留：

旧Golden Block Mid / Near。

---

## Step 2：建立视觉问题清单

按：

```text
轮廓
屋顶
Compound
台基
地面
道具
比例
LOD
```

分类记录。

---

## Step 3：整理BuildingPresentationProfile

---

## Step 4：建立灰模建筑轮廓库

暂时使用：

统一或简单材质。

目标：

先解决建筑形态。

---

## Step 5：住宅灰模V3

---

## Step 6：市场灰模V3

---

## Step 7：工坊灰模V3

---

## Step 8：仓廪灰模V3

---

## Step 9：官署灰模V3

---

## Step 10：灰模审阅门

关闭功能色差。

确认：

五类建筑仍可区分。

如果不能：

回到Step 4—9。

不得进入材质阶段。

---

## Step 11：屋顶体系

建立Roof Family。

---

## Step 12：屋脊和檐口

---

## Step 13：Foundation / 台阶

---

## Step 14：院墙 / 院门 / 门楼

---

## Step 15：Ground Treatment

---

## Step 16：道路 / Entrance空间关系

---

## Step 17：住宅道具

---

## Step 18：市场道具

---

## Step 19：工坊道具

---

## Step 20：仓廪道具

---

## Step 21：官署环境细节

---

## Step 22：树木和Vegetation Style

---

## Step 23：Stable Variation

---

## Step 24：Asset Scale Calibration

---

## Step 25：Presentation Importance

---

## Step 26：Far/Mid/Near LOD

重点保证Mid。

---

## Step 27：Batch / Instance优化

---

## Step 28：Cache

---

## Step 29：Golden Block普通Mid视觉验收

必须先过。

如果仍像积木：

不得进入建设兼容验收。

---

## Step 30：建设Grid兼容

确认：

仍显示正式50m PlanningCell。

---

## Step 31：Building Ghost V3

复用正式V3 Profile。

---

## Step 32：SingleCell住宅

---

## Step 33：MultiCell官署

---

## Step 34：Entrance / Rotation

---

## Step 35：Placement Validation回归

---

## Step 36：Draft / Undo回归

---

## Step 37：Core定向

---

## Step 38：Core全量

---

## Step 39：Unity Project Load

---

## Step 40：Unity EditMode

---

## Step 41：Unity PlayMode

---

## Step 42：性能测量

---

## Step 43：至少32张证据截图

---

## Step 44：V2 / V3 Before-After

---

## Step 45：实施报告

---

## Step 46：系统总纲

---

## Step 47：最终打开正式Unity

进入：

```text
PlayableDemo
→ C 县域
→ 样板街区
```

停留：

```text
Golden Block V3 Mid
```

保持运行。

---

# 一百九十四、实施报告要求

建立：

`REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md`

至少记录：

1. 开工HEAD。
2. Branch。
3. Workspace。
4. World Schema。
5. Unity版本。
6. Golden Block位置。
7. Golden Block尺寸。
8. PlanningCell范围。
9. Facility数量。
10. V2 Renderer数。
11. V2 Material数。
12. V2主要视觉问题。
13. BuildingPresentationProfile。
14. Module Library。
15. 五类灰模规则。
16. 灰模差异截图。
17. Roof Family。
18. Roof Variation。
19. Eaves。
20. Ridges。
21. Foundation。
22. Steps。
23. Wall Family。
24. Gate Family。
25. Compound Layout。
26. Residential规则。
27. Market规则。
28. Workshop规则。
29. Granary规则。
30. Government规则。
31. Ground Treatment。
32. PropSet。
33. Vegetation Style。
34. Stable Variation。
35. Asset Scale Calibration。
36. Presentation Importance。
37. Far LOD。
38. Mid LOD。
39. Near LOD。
40. Batch / Instance架构。
41. Cache。
42. V3 Renderer数。
43. V3 Material数。
44. Draw Call。
45. Triangle。
46. Props。
47. Vegetation Instances。
48. Mid性能。
49. Build Mode性能。
50. 50m Grid兼容。
51. Building Ghost。
52. SingleCell验证。
53. MultiCell验证。
54. Entrance。
55. Rotation。
56. Placement Validation。
57. No World Mutation。
58. Core结果。
59. Project Load。
60. EditMode。
61. PlayMode。
62. 环境BLOCKED如有。
63. V2/V3 Before-After。
64. 截图目录。
65. 用户人工验收状态。
66. 下一阶段建议。

---

# 一百九十五、系统总纲更新

更新：

`GAME_SYSTEMS_MASTER_AND_STATUS.md`

如果本任务自动验证通过，可以准确写：

> 洛阳Golden Block已完成建筑艺术语言V3原型：住宅、市场、工坊、仓廪和官署从方盒式Blockout升级为数据驱动的模块化Compound表现，建立差异化轮廓、屋顶体系、台基、院墙、门楼、地面处理和生活/生产道具，并通过Stable Variation和Far/Mid/Near LOD保持确定性及批量渲染。Golden Block继续使用正式50m PlanningCell建设模式，建筑仍按真实Footprint与Entrance存在。

不得写：

- 全洛阳建筑最终完成；
- 全国建筑美术完成；
- 精确历史复原；
- 正式ConstructionProject完成；
- 建筑升级完成；
- 拆迁完成；
- 战争完成；
- 室内完成。

---

# 一百九十六、下一阶段门

只有用户明确确认：

> Golden Block V3建筑已经不再过度抽象，五类建筑和街区整体方向可以接受。

才进入：

**《洛阳县域建筑艺术规则全县推广与 Far/Mid/Near 接入 V1》**

---

# 一百九十七、全县推广任务目标预告

下一阶段才处理：

```text
Golden Block Profile
↓
应用到全洛阳Existing Facility
↓
2084 Facility
↓
Far Aggregate
↓
Mid Cluster
↓
Near Detail
↓
村落
↓
庄园
↓
城区
```

---

# 一百九十八、如Golden Block仍不满意

不得推广。

继续在Golden Block调整：

```text
轮廓
屋顶
比例
Compound
Ground Treatment
Props
```

直到用户确认。

---

# 一百九十九、正式Construction继续延期

顺序继续：

```text
Golden Block V3
↓
全县建筑推广
↓
县域整体视觉Pass
↓
正式ConstructionProject
↓
材料
↓
人物劳力
↓
工期
↓
施工表现
```

---

# 二百、强制最终人工验收

自动验证结束以后：

不得直接结束。

必须打开正式Unity。

---

# 二百零一、人工验收入口

进入：

```text
PlayableDemo
↓
C 县域
↓
样板街区
```

默认：

```text
Mid
Labels OFF
Debug OFF
50m Grid OFF
```

---

# 二百零二、第一轮：建筑轮廓验收

用户至少检查：

1. 住宅不看标签能否认出。
2. 市场不看标签能否认出。
3. 工坊不看标签能否认出。
4. 仓廪不看标签能否认出。
5. 官署不看标签能否认出。
6. 是否仍主要靠颜色区分。
7. 是否还有明显方盒感。
8. 大建筑是否还是简单放大。
9. 屋顶是否真正有形态差异。
10. 檐口是否有层次。
11. 屋脊是否形成轮廓。
12. 台基是否让建筑真正落地。

---

# 二百零三、第二轮：Compound验收

13. 住宅是否有院落。
14. 工坊是否有作业院。
15. 官署是否有轴线。
16. 仓廪是否有装卸区。
17. 市场是否有开放空间。
18. 侧房是否合理。
19. 院墙是否合理。
20. 门楼是否合理。
21. 一个Compound是否仍然明显属于一个Facility，而不是杂乱多栋独立设施。

---

# 二百零四、第三轮：环境结合

22. 建筑与道路关系。
23. 建筑与院地关系。
24. 建筑是否仍插在草地上。
25. 住宅生活层。
26. 市场货摊。
27. 工坊材料。
28. 仓储货物。
29. 官署是否过度堆道具。
30. 树木是否遮挡建筑。
31. Mid整体是否已经像正式街区。

---

# 二百零五、第四轮：Near

32. 门。
33. 台阶。
34. 墙。
35. 屋檐。
36. 道具。
37. 模型比例。
38. 人尺度参考是否合理。
39. Entrance是否对应实际门。

---

# 二百零六、第五轮：50m建设模式

用户点击：

`建设规划`

然后检查：

40. 正式50m Grid出现。
41. 没有额外微型格。
42. Grid不遮挡建筑。
43. 住宅Ghost是否保持住宅Compound。
44. 官署Ghost是否保持官署Compound。
45. 小建筑是否明显小于Cell。
46. 大建筑是否跨Cell。
47. Covered Cell是否正确。
48. Entrance是否正确。
49. R旋转是否正常。
50. Road Access是否正常。
51. Draft是否正常。
52. 退出建设后Grid是否隐藏。

---

# 二百零七、性能现场检查

53. Mid平移是否顺畅。
54. Mid旋转是否顺畅。
55. Near缩放是否顺畅。
56. Build Mode是否明显掉帧。
57. LOD切换是否有严重爆闪。

---

# 二百零八、最终默认交接画面

最终停留：

```text
C 县域
→ 样板街区
→ Mid
```

默认：

```text
Labels OFF
Debug OFF
50m Grid OFF
```

画面至少同时展示：

```text
住宅
市场
工坊
仓廪
官署
道路
院墙
门楼
地面
树木
```

让用户第一眼直接判断：

> 建筑是不是还太抽象。

---

# 二百零九、Unity安全要求

如果用户已有Unity：

优先使用当前实例。

不得启动冲突第二实例。

---

# 二百一十、自动测试实例

只能终止：

本任务自己启动并记录PID的Unity进程。

---

# 二百一十一、人工Review实例

不得自动关闭。

完成以后：

```text
Unity
Play Mode
Game View
```

保持打开。

---

# 二百一十二、环境阻断

如果自动Unity验证因为：

已有Editor

阻断：

必须准确记录：

```text
BLOCKED_BY_OPEN_EDITOR
```

不得标PASS。

可使用当前Editor中的项目Validation菜单完成允许的验证。

---

# 二百一十三、任务状态

如果：

```text
编译
Core
Project Load
EditMode
PlayMode
截图
```

全部通过，

并已准备人工Game View：

```text
IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW
```

如果Unity被现有Editor阻断：

使用当前项目准确BLOCKED状态。

---

# 二百一十四、用户未确认前不得ACCEPTED

只有用户明确：

```text
通过
可以
建筑方向可以
验收通过
继续推广全洛阳
```

以后：

才能标记：

`ACCEPTED`

---

# 二百一十五、用户认为建筑仍抽象时

不得解释：

“这是程序化建筑所以只能这样。”

必须继续调整Golden Block。

优先检查：

```text
Silhouette
Compound
Roof
Foundation
Ground
Scale
Props
```

而不是先增加更多贴图。

---

# 二百一十六、最终完成定义

本任务真正完成，不是：

> “建筑比之前多了屋檐和摊位。”

而是：

Golden Block第一次建立一套：

> **能够支撑整个县域建筑生成的东汉游戏建筑语言。**

具体表现为：

```text
Facility
↓
不再等于一个盒子

Facility
↓
可以成为一个Compound

不同Facility
↓
拥有不同空间秩序

普通住宅
↓
像住宅

市场
↓
像市场

工坊
↓
像工坊

仓廪
↓
像仓廪

官署
↓
像官署
```

与此同时：

```text
只有一套Facility世界事实
只有一套50m PlanningCell
没有新微型Cell
没有新增世界建筑
没有人口变化
没有库存变化
没有产能变化
没有时间变化
没有战争实现
没有正式施工实现
```

---

# 二百一十七、最终执行顺序

请严格执行：

```text
开工快照
↓
保存Golden Block旧Mid / Near Before
↓
梳理当前建筑抽象问题
↓
建立或整理BuildingPresentationProfile
↓
建立基础建筑灰模模块库
↓
先完成住宅灰模轮廓
↓
完成市场灰模轮廓
↓
完成工坊灰模轮廓
↓
完成仓廪灰模轮廓
↓
完成官署灰模轮廓
↓
关闭标签 / 弱化材质做轮廓审阅
↓
如果五类仍分不出来则继续修改
↓
五类轮廓通过后再进入屋顶阶段
↓
建立Roof Family
↓
增加屋脊
↓
增加檐口
↓
增加墙体层次
↓
建立Foundation / Steps
↓
建立院墙
↓
建立院门 / 门楼
↓
正式建立Compound Layout
↓
让主门匹配正式Entrance
↓
让Compound布局尊重Road方向
↓
建立住宅Ground Treatment
↓
建立市场Ground Treatment
↓
建立工坊Ground Treatment
↓
建立仓廪Ground Treatment
↓
建立官署Ground Treatment
↓
增加少量住宅生活道具
↓
增加市场摊位 / 货物
↓
增加工坊作业物
↓
增加仓廪货物 / 装卸物
↓
增加克制的官署环境
↓
增加Vegetation Style
↓
建立Stable Variation
↓
建立Asset Scale Calibration
↓
建立Presentation Importance
↓
完成Far / Mid / Near LOD
↓
重点完成Mid正式视觉
↓
优化Batch / Instancing / Materials
↓
建立/更新Presentation Cache
↓
先完成Golden Block普通Mid视觉内部验收
↓
只有Mid不再明显像积木后继续
↓
验证现有正式50m PlanningCell建设模式
↓
禁止新增5m/10m Build Cell
↓
验证Grid不遮新建筑
↓
把Building Ghost升级为复用V3 Profile
↓
验证住宅SingleCell真实Footprint
↓
验证官署MultiCell Footprint
↓
验证Entrance
↓
验证R Rotation
↓
验证Road Access
↓
验证Placement Validator未变化
↓
验证Draft / Undo
↓
验证退出建设Grid隐藏
↓
验证No World Mutation
↓
执行V3定向Core
↓
执行Core全量回归
↓
执行Unity Project Load
↓
执行Unity EditMode
↓
执行Unity PlayMode
↓
准确记录任何Unity环境BLOCKED
↓
测量Renderer / Draw Call / Material / Triangle / FPS / Memory / GC
↓
生成至少32张真实Game View证据
↓
生成旧版 / V3同镜头Before-After
↓
完成实施报告
↓
更新GAME_SYSTEMS_MASTER_AND_STATUS.md
↓
不得推广全洛阳
↓
最后打开正式Unity
↓
进入PlayableDemo
↓
进入C县域
↓
进入样板街区
↓
关闭标签
↓
关闭Debug
↓
停留在Golden Block Mid
↓
保持Play Mode和Game View运行
↓
交给用户现场判断建筑是否已经不再过度抽象
```

不得在Golden Block V3人工通过前推广全洛阳。

不得通过颜色代替建筑类型轮廓。

不得通过增加面数掩盖Compound和比例问题。

不得新增第二套建筑世界事实。

不得新增5m/10m建设Cell。

不得提前实现战争。

不得提前实现正式ConstructionProject。