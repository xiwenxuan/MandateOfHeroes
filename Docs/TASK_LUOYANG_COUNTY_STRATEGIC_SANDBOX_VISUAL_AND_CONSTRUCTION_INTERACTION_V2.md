# 任务书：洛阳县域战略沙盘视觉纠错与城市建设交互分层重构 V2

## 一、任务定位

### 1.1 任务名称

**洛阳县域战略沙盘视觉纠错与城市建设交互分层重构 V2**

建议任务文件名：

`TASK_LUOYANG_COUNTY_STRATEGIC_SANDBOX_VISUAL_AND_CONSTRUCTION_INTERACTION_V2.md`

建议实施报告：

`REPORT_LUOYANG_COUNTY_STRATEGIC_SANDBOX_VISUAL_AND_CONSTRUCTION_INTERACTION_V2.md`

---

# 二、任务背景

当前洛阳县域已经完成：

```text
50m PlanningCell详细空间
↓
洛阳50m布局数据包
↓
Facility / Road / Water / Fortification / Portal空间闭环
↓
M / C / F三主视角
↓
County Far / Mid / Near信息LOD
↓
World-Space县域Presentation首版
↓
建设Draft / Placement Validation基础
```

但当前县域仍然不能视为正式可玩的主沙盘。

当前已经明确存在以下问题：

1. 地形三角面绕序错误，造成法线朝下，县域地形被光照成黑色；
2. Far LOD仍绘制大量Facility实体盒子，远景叠成中央灰白矩形；
3. 当前约16×32km完整县域被一次压进小窗口，普通50m级对象在Far不可能保持单体可读；
4. 城区、村落、农田、道路、城墙虽然已经进入World-Space，但视觉层次仍然不足；
5. 当前建设模式仍然偏“验证器”，尚未形成成熟城市建设工具的操作体验；
6. 当前正式世界经济施工闭环尚未接入，因此本任务不能提前伪造完整材料、价格、工期和维护体系。

因此下一阶段正式目标不是：

```text
继续给当前画面增加更多点、线、模型
```

而是：

> **把C县域真正重构成“战略沙盘 + 城区经营 + 近景建设”的三层可玩空间。**

---

# 三、核心设计方向

正式采用：

> **县域观察借鉴《三国志11》式斜俯视战略沙盘的信息组织思想；建设操作借鉴成熟城市建设游戏的鼠标Ghost、拖拽、吸附、分类工具栏和Overlay交互思想。**

这里只借鉴抽象设计方法。

禁止复制任何商业游戏的：

- 具体地图；
- 模型；
- UI布局；
- 图标；
- Shader；
- 纹理；
- 数值；
- 代码；
- 镜头固定参数。

---

# 四、本任务与前一World-Space任务的关系

前一阶段已经证明：

```text
县域世界空间Presentation架构方向成立
```

但当前效果仍然属于：

```text
World-Space技术原型
```

本任务不是推翻World-Space系统。

本任务是在其上完成：

```text
基础渲染纠错
+
LOD视觉语义收口
+
县域战略沙盘构图
+
Far聚合
+
Mid经营阅读
+
Near建设玩法
+
建设输入和UI重构
```

---

# 五、本任务完成后的正式C县域结构

正式冻结：

```text
C 县域
│
├── Far：县域战略总览
│
├── Mid：城区 / 聚落经营观察
│
└── Near：建设与具体Facility操作
```

三者全部属于：

```text
MainView = County
```

不得新建三个独立世界或三张地图。

---

# 六、Far / Mid / Near不是三个世界状态

Far / Mid / Near只能改变：

```text
Camera
Renderer
LOD
Aggregate
Label
Overlay
Interaction Detail
```

不得改变：

```text
WorldTime
Person
Facility
Road
Water
Fortification
Population
Inventory
Owner
Controller
```

---

# 七、正式三层职责

## 7.1 Far：县域战略总览

目标：

> 玩家一眼看懂整个洛阳县的地理、城邑和交通骨架。

主要显示：

```text
山地与高程
河流
主要水渠
主要官道
少量县域主干路
洛阳城区整体轮廓
城墙整体
主要城门
村落
农田
树林
大型庄园 / 大型Facility
少量地标
县域边界
```

默认隐藏：

```text
普通单体Facility
普通街巷
50m Planning Grid
Facility Entrance
Road Graph Node
Facility中心点
Urban Candidate Hull
Debug Chunk
内部ID
```

---

## 7.2 Mid：城区 / 聚落经营观察

目标：

> 玩家能够理解洛阳城区、村庄和生产空间是如何组织的。

主要显示：

```text
主干道路
次级道路
城区街区
建筑群
大型Facility
市场
官署
工坊
仓储
农田
水渠
城墙
城门
村落建筑
庄园
```

允许：

```text
选中正式Facility
查看Facility信息
查看街区 / 区域摘要
```

默认仍不显示：

```text
全县50m Planning Grid
全县Debug节点
所有Facility技术中心点
```

---

## 7.3 Near：建设与具体操作

目标：

> 玩家在局部真实空间中进行具体建设规划和Facility操作。

显示：

```text
具体Facility
真实Footprint
Entrance
详细道路
道路宽度
Wall Edge / Wall Mesh
Gate
Canal
局部PlanningCell50m
Building Ghost
Road Draft
Wall Draft
Canal Draft
Collision
Road Access
建设Validation
```

---

# 八、阶段一必须先做：基础画面纠错

不得先做建设UI。

必须首先关闭当前明显的基础渲染错误。

---

# 九、S0：修复Terrain Triangle Winding / Normal

当前已知：

`LuoyangCountyWorldSpacePresentationController.cs`

约line 447附近存在地形三角形绕序问题。

症状：

```text
Terrain Normal向下
↓
正面光照错误
↓
地表大面积发黑
```

必须优先修正：

```text
Triangle Winding
Normals
Tangents（如当前材质需要）
```

---

# 十、地形修复验收

修复以后必须证明：

```text
草地
丘陵
农田
道路周边
河谷
```

在标准县域灯光下：

- 不再大面积黑面；
- 不依赖双面材质强行掩盖错误；
- Camera旋转后受光连续；
- 相邻Chunk法线方向一致。

---

# 十一、禁止用双面Shader掩盖法线错误

不得通过：

```text
Cull Off
```

或：

```text
Unlit Terrain
```

绕过基础Mesh错误作为最终解决方案。

除非特定Presentation对象本身确实需要双面材质。

---

# 十二、Terrain确定性测试

相同：

```text
Elevation
Terrain
Chunk
```

必须产生相同：

```text
Vertices
Triangles
Normals
Bounds
```

---

# 十三、地形受光修复后再调视觉

正式顺序：

```text
修Triangle Winding
↓
修Normals
↓
验证地形
↓
再调Material
↓
再调Directional Light
↓
再调Fog
```

禁止在错误Normal上反复调灯光。

---

# 十四、县域光照方向

V2建议使用：

```text
暖色低角度主光
+
柔和环境光
+
轻度远景雾
```

目标：

- 山体有层次；
- 建筑有体积；
- 城墙能读出高度；
- 不产生强烈黑面；
- 不追求写实HDR。

---

# 十五、本任务不做昼夜最终系统

只建立适合正式县域沙盘阅读的稳定昼间原型。

不得扩成：

- 全天候光照；
- 四季；
- 雨雪；
- 完整昼夜周期。

---

# 十六、Far当前Facility盒子问题必须关闭

当前已知：

`LuoyangCountyWorldSpacePresentationController.cs`

约line 690附近Far仍绘制大量Facility盒子。

当前约1,568个或开工时实际Far可见Facility：

会在远景中心叠成：

```text
灰白巨大矩形 / 实体堆积
```

本任务必须彻底改变Far Facility表现。

---

# 十七、Far普通Facility禁止逐个实体显示

Far模式：

```text
Ordinary Facility Detail Renderer = OFF
```

普通Facility不得：

```text
一个Facility
→ 一个盒子
```

继续绘制。

---

# 十八、Facility数据仍全部保留

必须强调：

```text
不显示
≠
删除
```

所有正式Facility继续存在。

至少比较：

```text
FacilityIdsBefore
==
FacilityIdsAfter
```

---

# 十九、Far采用Presentation Aggregate

Far应该生成：

```text
Urban Aggregate
Village Aggregate
Estate Aggregate
Facility Density
Landmark Presentation
```

而不是普通Facility逐个绘制。

---

# 二十、8×8 PlanningCell聚合作为首版候选

允许使用：

```text
8 × 8 PlanningCell50m
≈ 400m × 400m
```

作为首版Far视觉聚合单元候选。

但必须明确：

> **它只是Presentation Bucket。**

不得建立正式：

```text
UrbanBlockWorldState
```

---

# 二十一、8×8不是永久固定规则

必须实测：

- Far摄像机；
- 720p；
- 1080p；
- 洛阳城区密度。

如果：

```text
8×8太粗
或
8×8仍太密
```

允许调整。

报告必须记录最终选择。

---

# 二十二、Far Aggregate不能只是“大盒子”

绝对禁止：

```text
1568个小盒子
↓
变成几十个大盒子
```

然后宣称完成。

Far Aggregate至少应该表达：

```text
建筑密度
建筑高度起伏
街区方向
道路切割
功能区差异
屋顶 / 建筑肌理
```

---

# 二十三、Far城市应呈现“建筑肌理”

远处看到的洛阳应类似：

```text
密集但有起伏的屋顶群
+
道路切割出的城市纹理
+
大型地标
+
连续城墙
```

而不是：

```text
灰色方形平台
```

---

# 二十四、Urban Aggregate生成

建议输入：

```text
Facility Positions
Facility Footprints
Facility Categories
Facility Heights
Road Geometry
Zone / UrbanArea
```

生成可重建的：

`UrbanPresentationAggregate`

---

# 二十五、Aggregate不是正式Facility

Aggregate：

- 没有Owner；
- 没有Inventory；
- 没有FacilityId作为世界资产；
- 不进入生产；
- 不进入存档世界事实。

---

# 二十六、Aggregate点击行为

Far如果点击Aggregate：

允许：

```text
聚焦城区
↓
进入Mid
```

或：

```text
显示区域摘要
```

不得把Aggregate当作一座正式Facility。

---

# 二十七、地标单独保留

Far必须支持：

`Landmark Presentation`

普通Facility：

```text
Aggregate
```

地标：

```text
保留独立简化轮廓 / 模型
```

---

# 二十八、Landmark Presentation Priority

建议建立纯Presentation元数据：

```text
LandmarkPresentationPriority
```

例如：

```text
Ordinary
Important
MajorLandmark
```

---

# 二十九、首批地标候选

在正式数据能可靠识别的前提下，可优先考虑：

```text
皇宫 / 宫城相关主要Facility
重要城门
太学
明堂
大型官署
大型军事Facility
```

实际使用哪些：

必须以当前正式FacilityDefinition和数据为准。

不得凭名字创造不存在的正式Facility。

---

# 三十、Landmark Priority不修改世界事实

它只是：

```text
Presentation metadata
```

不改变：

- Facility属性；
- 历史地位；
- Owner；
- Controller。

---

# 三十一、Far必须删除中央“实体堆积矩形”

这是V2的明确视觉阻断项。

最终Far：

不能再出现当前中央：

> 大量盒子重叠形成的巨大灰白色长方块。

---

# 三十二、Far最终应该看到城

最低视觉语义：

```text
城墙包围
↓
内部建筑密集
↓
道路穿城
↓
有大型地标
↓
城外有农业 / 村落
```

用户无需Debug即可理解：

> “这是洛阳城区。”

---

# 三十三、县域尺寸问题必须通过LOD解决

当前整个县约：

```text
16km × 32km
```

或开工时实际范围。

不得试图让：

```text
50m Facility
```

在完整县域Far镜头中仍保持单体可读。

这是不合理目标。

---

# 三十四、正式冻结信息原则

```text
Far看结构
Mid看组织
Near看对象
```

---

# 三十五、Far道路

Far仅显示：

```text
官道 / 战略主干
少量关键县域主干
```

隐藏：

```text
普通街道
巷路
局部Facility接入路
```

---

# 三十六、Far道路视觉必须更强

保留的官道需要：

- 有真实宽度；
- 比普通Mid小路更显著；
- 从地形中可辨；
- 与城门相连；
- 向县界Portal方向延伸。

---

# 三十七、官道不能重新变成GUI线

必须继续使用：

```text
Road Ribbon / Road Mesh
```

---

# 三十八、Mid道路

Mid显示：

```text
官道
县域主干
城区主要街道
村落连接路
```

---

# 三十九、Near道路

Near再展开：

```text
局部道路
Facility接入
建设相关道路
```

---

# 四十、道路表面风格

V2首版建议：

```text
官道
→ 较宽、偏浅土色 / 夯土色

主干
→ 中等宽度

普通道路
→ 较窄

小路
→ 低视觉权重
```

不需要现代柏油路风格。

---

# 四十一、道路路口必须继续优化

至少保证：

```text
T Junction
Cross Junction
Merge
```

在Mid / Near不呈现明显：

```text
Ribbon互相穿插
```

---

# 四十二、Far河流

Far河流是重要战略地理。

应比当前更明显。

至少：

- 宽度可辨；
- 与Terrain谷地一致；
- 与道路形成桥/绕行关系的视觉基础；
- 不只靠纯蓝色识别。

---

# 四十三、Far城墙

Far必须把洛阳城墙作为城市最重要轮廓之一。

表现：

```text
连续城墙带
+
主要城门
+
少量主要塔楼轮廓
```

---

# 四十四、城墙Far不显示每一Edge Debug细节

底层仍然：

`Fortification Edge`

Far只显示：

连续简化轮廓。

---

# 四十五、Mid城防

Mid展开：

```text
Wall Segment
Gate
Corner Tower
Major Tower
```

---

# 四十六、Near城防

Near显示实际：

```text
Cell Edge对应墙体
Gate结构
Tower Facility
```

---

# 四十七、城区候选黄色边界默认关闭

当前巨大黄色斜线属于：

城区候选 / Urban Candidate边界。

正式玩家模式：

```text
Default = OFF
```

---

# 四十八、Urban Candidate Hull只进入Debug

仅允许：

```text
Developer Overlay
Urban Audit
Evidence
Spatial Diagnostic
```

显示。

---

# 四十九、不得用Hull代替真正城区视觉

正式玩家判断：

“哪里是城区”

应该通过：

```text
城墙
建筑密度
道路
地标
城市肌理
```

理解。

不是靠黄色框。

---

# 五十、Far村落

Far必须开始体现：

```text
Village Aggregate
```

村落建议显示：

```text
少量建筑群
+
道路连接
+
周围农田
```

---

# 五十一、村落不能只是一个彩色点

至少要形成：

> 小型聚落轮廓。

---

# 五十二、Far农业

当前县域Far不能让城外全部变成同一种草绿色。

必须至少体现：

```text
Agriculture Patch
```

---

# 五十三、农田视觉

V2可以使用：

```text
田块
田埂方向
不同浅色地表
少量作物纹理
```

不要求：

逐株作物。

---

# 五十四、农田来源

优先读取：

- 正式农业Facility；
- 土地用途；
- 当前生产数据；
- 已有农业区域。

如果不足：

允许创建：

`Derived Presentation Agriculture`

但必须明确：

只用于视觉。

---

# 五十五、Far树林与自然植被

至少支持：

```text
山地林区
河岸植被
稀疏树丛
```

帮助区分：

山
平原
农田
城市
水系。

---

# 五十六、植被性能

必须使用：

```text
Batch
GPU Instancing
Chunk
```

不得：

```text
一棵树一个完整逻辑MonoBehaviour
```

---

# 五十七、Far视觉构图验收

Far第一眼的信息优先级建议：

```text
Terrain relief
↓
River
↓
Urban Area / Wall
↓
Major Road
↓
Village / Agriculture
↓
Landmark
↓
Other Detail
```

---

# 五十八、Far失败判据

如果用户第一眼首先看到：

```text
方盒
节点
Debug线
格子
候选边界
```

Far仍然失败。

---

# 五十九、Mid正式定位

Mid不是：

Far简单放大。

Mid必须从：

```text
战略沙盘
```

开始进入：

```text
城市经营阅读
```

---

# 六十、Mid Facility显示

Mid主要显示：

```text
Building Cluster
Major Facility
Important Facility
街区建筑群
```

普通Facility可以开始出现，但不得一口气全县全部最高细节。

---

# 六十一、Mid建筑群要体现功能差异

如果正式FacilityCategory允许：

至少可区分：

```text
住宅
市场
工坊
仓储
官署
军事
```

主要通过：

- 建筑形态；
- 体量；
- 密度；
- 屋顶；
- 少量色调。

避免现代彩色分区块。

---

# 六十二、Mid允许选Facility

如果当前点击某个Mid建筑群：

能够稳定解析具体Facility时：

允许打开Facility信息。

如果当前仍是Aggregate：

应先进入更近LOD或区域摘要。

不得错误选择错误FacilityId。

---

# 六十三、Mid城市不能变成独立City Map

全程仍然：

```text
County World-Space
```

相机只是聚焦城区。

---

# 六十四、Near正式定位

Near的核心不是：

“画面最漂亮”。

而是：

> **让玩家能够准确进行具体建设和Facility操作。**

---

# 六十五、进入建设规划自动聚焦

点击：

`建设规划`

以后：

系统应将镜头从Far / Mid：

自动调整到适合具体建设的Near尺度。

---

# 六十六、建设默认聚焦目标

优先级：

```text
当前已选地块
↓
当前选中Facility附近
↓
当前UrbanArea
↓
玩家当前所在地
↓
洛阳默认建设测试区
```

具体根据当前上下文。

---

# 六十七、建设镜头不是传送

Camera Focus：

不得改变：

```text
Player Position
Person Location
WorldState
```

---

# 六十八、2.4×1.2km只作为默认相机目标

1080p下：

可以把约：

```text
2.4km × 1.2km
```

作为首版Near建设镜头可视范围候选。

但必须明确：

```text
这是Camera Presentation参数
```

不是：

```text
ConstructionArea世界规则
```

---

# 六十九、玩家可以继续移动建设镜头

进入建设以后：

仍然可以：

```text
Pan
Zoom
Rotate
```

整个县域仍是一张连续地图。

---

# 七十、建设底部工具栏

Near Planning模式增加正式底部分类工具栏。

建议一级分类：

```text
道路
住宅
市场
工坊
仓储
官署
军事
水利
区域
工具
```

具体分类根据已有FacilityDefinition调整。

---

# 七十一、没有正式PlacementProfile的建筑不显示

不能把所有FacilityDefinition无条件放进建设栏。

只有具有：

```text
合法PlacementProfile
```

的类型进入正式建设工具。

---

# 七十二、工具栏卡片信息

建筑卡至少显示：

```text
建筑名称
类别
Footprint
容量（如已有正式定义）
道路要求
主要用途
```

---

# 七十三、材料 / 工期 / 维护费显示边界

这是V2必须严格控制的事项。

如果当前已有正式数据：

```text
显示真实数据
```

如果只有可靠规划估算：

```text
明确标记“规划估算”
```

如果尚未建立正式建设合同：

显示：

```text
正式施工阶段接入后计算
```

或干脆暂不显示。

---

# 七十四、禁止为了建设卡片硬编码假经济数据

禁止：

```text
住宅
木材100
工期5天
维护20
```

只为了让UI看起来完整。

正式ConstructionProject下一阶段才建立最终经济合同。

---

# 七十五、容量同样必须来自正式定义

如果FacilityDefinition已有：

```text
Capacity
```

可以显示。

否则：

不伪造。

---

# 七十六、Building Ghost

Near建设时：

鼠标移动显示：

```text
真实3D / 2.5D Building Ghost
```

---

# 七十七、Ghost必须来自同一Presentation Resolver

优先使用：

```text
Facility Model / Proxy
```

的半透明版本。

不能另做一套与最终Facility尺寸不同的Ghost。

---

# 七十八、Ghost状态

至少：

```text
绿色
= Valid

黄色
= Conditional / Warning

红色
= Invalid
```

具体色值保持当前项目风格。

---

# 七十九、Ghost不能只靠颜色

同时通过：

```text
Footprint
Entrance
Road connection
Collision highlight
错误提示
```

表达状态。

---

# 八十、Ghost地形关系

Ghost必须：

- 贴Terrain；
- 使用正确Rotation；
- Footprint对应正式Placement Validator；
- 不因视觉平整自动绕过坡度规则。

---

# 八十一、局部50m Grid

建设模式：

只显示：

```text
鼠标附近
Ghost附近
当前选区
道路 / 墙 / 水渠拖拽范围
```

---

# 八十二、禁止建设模式全县铺满50m格

不能一次显示：

约20万PlanningCell。

---

# 八十三、Grid渐隐

建议：

```text
Near Cursor
→ 清晰

外围
→ 渐淡

远处
→ 隐藏
```

使格子成为：

工具。

而不是地图主视觉。

---

# 八十四、Grid贴地

规划格必须：

```text
World-Space
+
Terrain Surface
```

而不是屏幕GUI线。

---

# 八十五、道路建设工具

继续采用：

```text
拖拽道路
```

交互。

---

# 八十六、道路Draft视觉

Road Draft继续保持规划语义，例如：

```text
青色半透明Road Ribbon
```

与Existing Road明显区分。

---

# 八十七、Road Draft至少支持

```text
起点
拖动
终点
吸附
基本弯道
合法 / 非法
```

---

# 八十八、道路吸附

至少支持：

```text
Existing Road端点
Road Segment
Gate
合法Portal方向
```

根据现有实现决定。

---

# 八十九、道路Draft仍不是正式Road

本任务绝对不创建：

正式Road。

---

# 九十、城墙工具

采用：

```text
Cell Edge连续拖拽
```

---

# 九十一、Wall Draft视觉

从旧线段升级为：

```text
半透明World-Space Wall Preview
```

沿合法Cell Edge。

---

# 九十二、Wall Draft仍不改变Cell Port

Draft阶段：

正式通行拓扑不变。

---

# 九十三、水渠工具

采用：

```text
World-Space Canal Preview
```

支持拖拽。

---

# 九十四、水渠仍使用自己的空间规则

不能把：

```text
Road Draft
Wall Draft
Canal Draft
```

变成一个没有领域差异的万能Line Tool。

---

# 九十五、区域工具

支持：

```text
Brush
Rectangle
```

形成：

`Draft Zone`

---

# 九十六、区域仍然只是规划

不得：

```text
涂住宅区
→ 正式LandUse改变
```

不得：

```text
涂住宅区
→ 自动造房
```

---

# 九十七、建设右侧信息面板

Near模式增加：

`Selected Object / Planning Detail`

右侧面板。

---

# 九十八、选中Existing Facility时至少显示

根据当前已有正式字段：

```text
名称
类型
Owner
Controller
Operational State
Capacity
Workers
Inventory摘要
Production摘要
Entrance / Road Access
```

只显示真实存在的数据。

---

# 九十九、升级功能边界

如果正式升级系统尚未完成：

右侧面板不得提供假：

`升级`

按钮。

可以：

```text
隐藏
```

或：

```text
尚未开放
```

---

# 一百、正式拆除边界

Existing Facility仍不能：

点击拆除立即消失。

V2建设工具中的“拆除”只允许：

```text
删除Draft
```

如果选中正式Facility：

显示：

```text
正式设施拆除需要拆迁项目
```

后续另立任务。

---

# 一百零一、Draft移动

继续只允许：

Draft。

---

# 一百零二、Draft复制

只复制：

```text
Definition
Placement Candidate
```

不复制：

- FacilityId；
- Owner；
- Controller；
- Inventory；
- Workers。

---

# 一百零三、Eyedropper

点击正式Facility：

进入同类建筑规划工具。

读取：

```text
FacilityDefinitionId
PlacementProfile
```

不复制世界状态。

---

# 一百零四、建设输入合同

建议冻结：

```text
Left Click
= 当前Primary Tool操作

R
= 旋转建筑

Right Click
= 取消当前工具 / 当前操作

Middle Drag
= Pan

Alt + Right Drag
= Camera Rotate

Mouse Wheel
= Zoom
```

---

# 一百零五、右键取消与镜头旋转必须彻底分开

`RightClick`

和：

`Alt + RightDrag`

必须解析为不同Input Intent。

不得同时触发。

---

# 一百零六、Far/Mid中正常相机操作

没有进入建设工具时：

保留正常县域沙盘相机。

---

# 一百零七、自动LOD与建设LOD关系

正常相机：

根据尺度自动：

```text
Far
Mid
Near
```

进入建设模式：

最低强制到：

`Near-compatible detail`

但允许玩家进一步缩放。

---

# 一百零八、退出建设后恢复正常LOD逻辑

不得把建设Grid / Ghost残留到普通County模式。

---

# 一百零九、Far / Mid / Near切换Hysteresis继续保留

此前已完成稳定滞回。

V2不得破坏。

---

# 一百一十、Facility Detail的屏幕可读性判断

继续建议：

```text
Projected Screen Size
```

参与Facility显示判断。

不要只依赖：

`Camera zoom < X`

---

# 一百一十一、Far地标优先

即使普通Facility被聚合：

Major Landmark仍可保留简化独立表现。

---

# 一百一十二、Mid大型Facility优先

较大：

- 官署；
- 市场；
- 仓储；
- 军事Facility；

比普通住宅更早展开。

---

# 一百一十三、Near具体Facility全部来自Spatial Culling

Near只展开：

当前可见Chunk及Margin。

不得全县2,084 Facility全部近景渲染。

---

# 一百一十四、Far Aggregate同样必须Chunk化

进入洛阳Far：

不能每帧重新扫描全部Facility。

允许加载时构建稳定：

```text
Aggregate Cache
```

---

# 一百一十五、Aggregate Cache不是World Save

只属于：

Presentation Cache。

可以删除重建。

---

# 一百一十六、Cache Key

至少考虑：

```text
Layout Package Hash
Presentation Version
Facility Presentation Revision
```

---

# 一百一十七、正式道路变化的未来兼容

未来ConstructionProject新建Road以后：

必须能够只失效相关Road Presentation Chunk。

V2只建立接口。

不做正式Construction。

---

# 一百一十八、正式Facility变化的未来兼容

未来新Facility完成：

只更新：

相关Facility / Aggregate Chunk。

不要未来每完工一栋房子重建整个洛阳县。

---

# 一百一十九、城墙未来兼容

同理：

Wall新增 / 破坏

局部重建Presentation。

---

# 一百二十、地形视觉仍必须保持世界空间物理关系

道路、墙、建筑、河流必须共享同一：

```text
County World Coordinate
```

---

# 一百二十一、不得为了构图人为移动Facility

如果某些候选位置：

视觉不好看，

不能直接把Facility挪到“更好看的地方”。

必须区分：

```text
Presentation问题
vs
Layout candidate问题
```

如确实属于布局候选问题：

单独记录。

不得在V2偷偷修改权威布局。

---

# 一百二十二、道路同理

不得为了让道路更“像游戏”：

随意重画正式Road Geometry。

---

# 一百二十三、城墙同理

不得为了让城墙更方正：

修改Fortification Edge事实。

---

# 一百二十四、县域总览构图

Far镜头默认构图应尽量保证：

```text
县域主体
+
洛阳城
+
山地
+
河流
+
出城官道
```

处于有效屏幕范围。

---

# 一百二十五、默认县域相机

第一次进入：

`C 县域`

应该使用：

适合县域总览的斜俯视Camera Preset。

---

# 一百二十六、县域相机不是固定镜头

用户可以：

Pan / Rotate / Zoom。

Preset只是进入默认。

---

# 一百二十七、城区按钮

县域内部：

`洛阳城区`

点击后：

相机自动聚焦UrbanArea并进入适合Mid阅读的尺度。

不加载第二张地图。

---

# 一百二十八、建设按钮

点击：

`建设规划`

相机自动聚焦适合Near建设。

---

# 一百二十九、县域总览按钮

点击：

`县域总览`

返回Far预设。

---

# 一百三十、三按钮只是Camera / Tool State

不得：

```text
切换数据源
```

---

# 一百三十一、本任务不修改M天下核心

M天下继续承担：

州 / 郡国 / 县和战略道路。

V2重点只处理C县域。

---

# 一百三十二、本任务不大规模重做F人物

但必须检查：

C和F是否继续共享：

```text
Facility Position
Road Position
Terrain Position
```

---

# 一百三十三、F人物兼容

如果当前F人物Presentation尚未完全接World-Space：

报告必须准确说明。

不要把C县域完成误写为F人物最终完成。

---

# 一百三十四、本任务不制作通用建筑内部

继续：

```text
Entrance
↓
InsideFacility
↓
玩法 / 管理UI
```

---

# 一百三十五、本任务不做正式ConstructionProject

再次明确。

不得接：

```text
正式建设项目
材料消耗
具体劳工
世界时间施工
存档施工
```

---

# 一百三十六、本任务不做维护费正式结算

即使UI未来预留：

本轮不建立假的Maintenance Economy。

---

# 一百三十七、本任务不做建筑升级

不实现：

Upgrade Project。

---

# 一百三十八、本任务不做正式拆迁

不实现：

Demolition Project。

---

# 一百三十九、本任务不做NPC自动扩城

Zone只作为规划。

---

# 一百四十、本任务不做攻城战

虽然城墙、地形和高差会成为未来战争基础：

本任务不做：

```text
LOS
攻击
箭塔射击
城墙损伤
攻城器械
```

---

# 一百四十一、视觉高度必须为未来战争可复用

Terrain：

有GroundElevation。

Facility：

有正式或明确Presentation Height。

Wall：

有Height。

Tower：

有Height。

报告中必须区分：

```text
Formal Gameplay Height
vs
Presentation Fallback Height
```

---

# 一百四十二、远景雾

建议加入轻量：

`Distance Fog`

用于：

- 减少远处硬边；
- 增强山川层次；
- 增强沙盘感。

---

# 一百四十三、Fog不得遮玩法

不能导致：

- 城墙消失；
- 河流看不到；
- 官道不可读；
- 城区模糊成一团。

---

# 一百四十四、县域战略沙盘不是微缩模型玩具

避免过度：

- 塑料模型感；
- 高饱和；
- 夸张景深；
- Miniature tilt-shift。

保持：

古代县域战略沙盘。

---

# 一百四十五、材质方向

V2先追求：

```text
低饱和
自然地表
土路
浅色石/土墙
统一古代建筑色调
```

不要求最终水墨。

---

# 一百四十六、屋顶与建筑群

Far/Mid建筑群至少应拥有：

```text
屋顶层次
高度差
密度变化
```

避免：

纯立方体矩阵。

---

# 一百四十七、无模型Facility Proxy继续优化

如果没有正式模型：

Proxy至少根据：

```text
Footprint
Height
Category
EntranceFacing
Stable Hash
```

确定性生成。

---

# 一百四十八、Proxy可以有有限风格变体

例如：

```text
住宅屋顶
工坊屋顶
仓储屋顶
官署屋顶
军事屋顶
```

必须来自稳定规则。

不得运行时随机。

---

# 一百四十九、已有模型缩放继续校准

Prefab：

必须与：

```text
Facility Footprint
```

匹配。

---

# 一百五十、Asset Scale Calibration

如果现有模型比例不统一：

建立/复用：

```text
AssetPresentationScale
```

元数据。

---

# 一百五十一、禁止全局粗暴统一Scale

不得：

```text
所有建筑Scale×10
```

解决。

---

# 一百五十二、Far地标模型允许简化

可以采用：

```text
Simplified Landmark Mesh
```

减少性能。

---

# 一百五十三、Mid建筑群合批

优先：

```text
GPU Instance
Batch Renderer
```

复用现有系统。

---

# 一百五十四、Near交互对象

Near需要点击的Facility：

可以建立轻量：

```text
Selection Proxy / Collider
```

但不是世界权威。

---

# 一百五十五、Selection Collider不能全县全部高成本常驻

按当前可见空间管理。

---

# 一百五十六、Overlay继续保留

至少：

```text
地形
区划
道路
设施
城防
规划
```

已有六类图层继续使用。

---

# 一百五十七、Far Overlay要克制

普通Far：

默认只显示必要基础世界。

技术Overlay默认关闭。

---

# 一百五十八、建设Near Overlay

Near允许快速切换：

```text
道路
供水（如有正式数据）
地形
产权（如正式数据足够）
规划
```

---

# 一百五十九、产权不足不得伪造

如果PlanningCell没有完整正式产权：

不做假产权热力图。

---

# 一百六十、材料Overlay本任务不做

正式ConstructionProject以后再接。

---

# 一百六十一、核心测试：Terrain Winding

验证所有Terrain Chunk：

法线方向合法。

---

# 一百六十二、核心测试：Terrain Lighting Basis

至少验证：

```text
Average Normal.y > 0
```

或项目等效确定性几何判据。

不能用视觉截图作为唯一法线测试。

---

# 一百六十三、核心测试：Far ordinary Facility suppression

Far：

普通Facility Detail Render Request数量必须大幅降低。

不能返回全部普通Facility盒子。

---

# 一百六十四、核心测试：Landmark retention

Far：

指定Landmark仍保留。

---

# 一百六十五、核心测试：Aggregate coverage

Aggregate包含的Facility集合：

与预期普通Facility一致。

不得丢失或重复。

---

# 一百六十六、核心测试：Aggregate deterministic

同一布局：

Aggregate summary一致。

---

# 一百六十七、核心测试：Aggregate不是World Asset

生成Aggregate：

Facility集合和WorldSummary不变。

---

# 一百六十八、核心测试：Far/Mid/Near

同一Camera路径：

稳定得到：

```text
Far
Mid
Near
```

并保持Hysteresis。

---

# 一百六十九、核心测试：Far road filtering

Far只返回高重要度道路。

---

# 一百七十、核心测试：Far planning suppression

Far：

Planning Grid=OFF。

---

# 一百七十一、核心测试：Near local grid

Planning Near：

只返回局部Cell窗口。

---

# 一百七十二、核心测试：Construction camera focus

进入Planning：

只改变Camera/ViewState。

不改变World。

---

# 一百七十三、核心测试：2.4×1.2km不是世界规则

无论Camera默认可视范围如何：

Planning可以平移到县域其他位置。

---

# 一百七十四、核心测试：Ghost

Ghost和Placement Validator：

Position / Rotation / Footprint一致。

---

# 一百七十五、核心测试：Road Draft

World-Space Draft：

底层Draft geometry不变。

---

# 一百七十六、核心测试：Wall Draft

仍沿Cell Edge。

---

# 一百七十七、核心测试：Canal Draft

仍使用Canal geometry。

---

# 一百七十八、核心测试：UI economy boundary

没有正式ConstructionDefinition字段时：

UI不得返回伪造Cost / Duration。

---

# 一百七十九、核心测试：No World Mutation

完整执行：

```text
Far
Mid
Near
Planning
Ghost
Road Draft
Wall Draft
Canal Draft
Overlay
Camera Focus
```

以后：

```text
WorldTime
Person
Facility
Inventory
Market
Road
Water
Fortification
Population
Owner
Controller
WorldSummary
```

完全一致。

---

# 一百八十、核心测试：Legacy Debug

Legacy IMGUI开关：

只改变Presentation。

---

# 一百八十一、Unity EditMode最低覆盖

至少：

```text
Terrain winding
Terrain normals
Far facility aggregation
Landmark retention
Aggregate coverage
Road filtering
Camera LOD
Planning camera preset
Local planning grid
Ghost
Road Draft
Wall Draft
Canal Draft
UI data boundary
No World Mutation
```

---

# 一百八十二、Unity PlayMode必须从正式入口执行

使用：

```text
PlayableDemo
↓
C 县域
↓
洛阳
```

---

# 一百八十三、PlayMode第一阶段：Far

必须实际确认：

1. 地形不再黑；
2. 山体有正常明暗；
3. 中央灰白巨大方块消失；
4. 普通Facility盒子Far不再全部绘制；
5. 洛阳城区Far可以识别；
6. 城墙围合关系清楚；
7. 官道可读；
8. 河流可读；
9. 城外有农田；
10. 有村落；
11. Urban Candidate黄色大线默认消失。

---

# 一百八十四、PlayMode第二阶段：Mid

确认：

1. 街区逐渐展开；
2. 建筑群有体量；
3. 大型Facility可辨；
4. 市场/工坊/仓储/官署等至少部分能通过模型或建筑类型理解；
5. 街道出现；
6. 水渠出现；
7. 城门和道路关系正确；
8. 没有大面积Debug线。

---

# 一百八十五、PlayMode第三阶段：Near

确认：

1. 具体Facility显示；
2. Facility不是7像素点；
3. Footprint和模型大致吻合；
4. Entrance方向合理；
5. Road有宽度；
6. Wall/Gate可辨；
7. Planning功能可进入。

---

# 一百八十六、PlayMode建设入口

点击：

`建设规划`

必须：

```text
自动拉近
↓
进入Near
↓
聚焦合理区域
↓
底部建设工具栏出现
```

---

# 一百八十七、PlayMode建筑Ghost

至少完成：

```text
选择住宅 / 仓库
↓
鼠标移动
↓
3D Ghost跟随
↓
R旋转
↓
Valid / Warning / Invalid
↓
创建Draft
```

---

# 一百八十八、PlayMode局部Grid

必须确认：

```text
鼠标附近显示
远处渐隐
```

没有全县20万格。

---

# 一百八十九、PlayMode Road Draft

拖一段道路。

看到：

```text
青色World-Space道路带
```

---

# 一百九十、PlayMode Wall Draft

沿Edge拖一段墙。

看到：

```text
半透明墙体预览
```

---

# 一百九十一、PlayMode Canal Draft

拖一小段水渠。

看到：

```text
Channel Preview
```

---

# 一百九十二、PlayMode输入

确认：

```text
Left Click
R
Right Click
Middle Drag
Alt+RightDrag
Wheel
```

无冲突。

---

# 一百九十三、PlayMode UI数据真实性

检查建筑卡：

如果无正式材料 / 工期：

不得显示伪造数字。

---

# 一百九十四、PlayMode退出Planning

退出后：

```text
Grid消失
Ghost消失
Tool清理
县域世界仍正常
```

---

# 一百九十五、PlayMode M/C/F

至少：

```text
C县域
↓
F人物
↓
M天下
↓
C县域
```

县域沙盘恢复正常。

---

# 一百九十六、Before / After证据

必须使用同一Camera或尽可能同构Camera输出：

```text
V1 World-Space问题画面
VS
V2 Far修复后
```

重点看：

- 黑色Terrain；
- 中央灰白方块；
- 黄色候选线；
- 城区可读性。

---

# 一百九十七、截图要求

至少输出：

`01_v1_far_before.png`

当前问题Far。

`02_v2_far_terrain_fixed.png`

Terrain法线修复后的Far。

`03_v2_far_final.png`

最终Far。

`04_v2_mid.png`

Mid。

`05_v2_near.png`

Near。

`06_v2_terrain_hills.png`

山体与正常光照。

`07_v2_river.png`

河流。

`08_v2_major_road.png`

官道。

`09_v2_urban_aggregate.png`

Far洛阳城区建筑肌理。

`10_v2_landmarks.png`

Far地标。

`11_v2_wall_gate.png`

城墙与Gate。

`12_v2_village.png`

村落。

`13_v2_farmland.png`

农田。

`14_v2_vegetation.png`

树林。

`15_v2_mid_building_clusters.png`

Mid建筑群。

`16_v2_near_facility.png`

Near具体Facility。

`17_v2_planning_entry.png`

建设模式入口。

`18_v2_construction_toolbar.png`

底部分类栏。

`19_v2_building_ghost_valid.png`

合法Ghost。

`20_v2_building_ghost_invalid.png`

非法Ghost。

`21_v2_local_grid.png`

局部50m Grid。

`22_v2_road_draft.png`

Road Draft。

`23_v2_wall_draft.png`

Wall Draft。

`24_v2_canal_draft.png`

Canal Draft。

`25_v2_facility_info_panel.png`

Facility右侧信息。

`26_v2_debug_overlay.png`

Debug打开。

`27_v2_debug_off.png`

正式干净画面。

---

# 一百九十八、720p验收

1280×720下：

Far必须仍然能看懂：

```text
城
墙
河
路
山
村
田
```

不能再次退化成：

点和线。

---

# 一百九十九、1080p重点验收

1920×1080作为本轮主要体验基准。

必须检查：

Far / Mid / Near完整过渡。

---

# 二百、Far体验标准

用户不看Debug、不看图例：

也应该能回答：

```text
洛阳城在哪里？
城墙在哪里？
主要官道从哪里进城？
河流在哪里？
山地在哪里？
城外哪里是农田？
哪里有村落？
```

---

# 二百零一、Mid体验标准

用户应该能回答：

```text
城区主要道路怎么走？
哪里是较密建筑区？
哪里有大型设施？
城门在哪里？
城内外道路如何连接？
```

---

# 二百零二、Near体验标准

用户应该能够：

```text
准确选择一栋Facility
理解建筑位置
理解Entrance
理解道路接入
进行建设规划
```

---

# 二百零三、性能记录

必须至少记录：

```text
Far FPS
Mid FPS
Near FPS

Far Draw Calls
Mid Draw Calls
Near Draw Calls

Far Visible Facility Detail Count
Far Aggregate Count

Mid Visible Clusters
Near Visible Facilities

Terrain Triangles
Road Triangles
Wall Triangles
Vegetation Instance Count

Cold Entry Time
Warm Entry Time

Far→Mid transition cost
Mid→Near transition cost

Planning Mode Enter
Building Ghost Update P50/P95
Local Grid Update P50/P95
Road Draft Preview P50/P95

GC
Memory
```

---

# 二百零四、Far Facility目标不是0世界对象

Far可以保留：

- Landmark；
- Major Facility。

但普通Facility Detail应大幅减少。

报告记录：

```text
Before ordinary facility render count
After ordinary facility render count
```

---

# 二百零五、Far聚合性能

Aggregate生成：

不得每帧重新构建。

---

# 二百零六、相机连续缩放性能

快速Wheel Zoom：

不能因为每次LOD变化重新生成整个县域导致长卡顿。

---

# 二百零七、Terrain修复不得造成新裂缝

检查Chunk边界：

- 高度；
- Normal；
- Material。

不能出现明显接缝。

---

# 二百零八、性能不能靠关闭核心地理获取

不能为了FPS：

隐藏所有：

- 城墙；
- 河流；
-官道；
- 城区。

必须保持Far正确语义。

---

# 二百零九、S0阻断

以下任一情况发生：

任务不得完成。

- Terrain法线仍错误；
- Terrain仍大面积黑面；
- 用双面Shader掩盖Winding错误；
- Far仍逐个绘制全部普通Facility盒子；
- 中央巨大灰白实体块仍明显存在；
- Urban Candidate黄色大边界仍默认显示；
- Far不再能识别洛阳城区；
- Presentation修改Facility Position；
- Presentation修改Road事实；
- Presentation修改Fortification；
- Presentation修改WorldTime；
- World Schema未经批准升级；
- 8×8 Aggregate成为正式WorldState；
- 建设Near限制玩家只能在固定2.4×1.2km区域；
- 建筑UI伪造材料/工期/维护费；
- Ghost直接创建正式Facility；
- Road Draft直接创建正式Road；
- Wall Draft修改正式Cell Port；
- Canal Draft修改正式Water；
- Zone Brush修改正式LandUse。

---

# 二百一十、S1阻断

以下问题修复前：

不得用户验收。

- Far仍像调试器；
- 城区仍是灰块；
- 建筑Aggregate没有城市肌理；
- Landmark全部消失；
- 山地视觉高差不明显；
- 河流不易阅读；
- 官道不易阅读；
- 城墙不形成整体轮廓；
- Gate与道路错位；
- 村落只剩一个点；
- 城外全部同一绿色；
- Mid突然刷出大量盒子；
- Near Facility比例明显错误；
- Planning Grid全县显示；
- Ghost仍是2D矩形；
- Road Draft仍是GUI线；
- Wall Draft仍是GUI线；
- Canal Draft仍是GUI线；
- 右键与相机旋转冲突；
- 进入建设模式没有自动进入合理Near尺度；
- 底部工具栏难以使用；
- LOD闪烁严重；
- Camera操作明显卡顿。

---

# 二百一十一、自动验收清单

## A. Terrain

- [ ] Triangle Winding正确。
- [ ] Normal正确。
- [ ] Chunk边界正常。
- [ ] Terrain正常受光。
- [ ] 山地有层次。
- [ ] 不使用错误掩盖方案。

## B. Far

- [ ] 普通Facility不逐个绘制。
- [ ] 中央灰白矩形消失。
- [ ] Urban Aggregate存在。
- [ ] Aggregate有城市肌理。
- [ ] Landmark保留。
- [ ] 城墙轮廓清楚。
- [ ] 官道清楚。
- [ ] 河流清楚。
- [ ] 村落存在。
- [ ] 农田存在。
- [ ] 植被有层次。
- [ ] 黄色Urban Hull默认隐藏。

## C. Mid

- [ ] 建筑群展开。
- [ ] 街区可读。
- [ ] 主要道路展开。
- [ ] 大型Facility展开。
- [ ] 水渠可读。
- [ ] 城墙分段可读。
- [ ] 可以检查Facility。

## D. Near

- [ ] 具体Facility。
- [ ] Footprint。
- [ ] Entrance。
- [ ] Road宽度。
- [ ] Wall / Gate。
- [ ] Canal。
- [ ] Facility Selection。

## E. Planning

- [ ] 点击建设后自动进入Near。
- [ ] Camera只是聚焦，不改World。
- [ ] 底部分类工具栏。
- [ ] Building Ghost。
- [ ] R旋转。
- [ ] Local Grid。
- [ ] Validation。
- [ ] Road Draft。
- [ ] Wall Draft。
- [ ] Canal Draft。
- [ ] Zone Brush。
- [ ] RightClick Cancel。
- [ ] Middle Pan。
- [ ] Alt+RightRotate。
- [ ] Wheel Zoom。

## F. UI真实性

- [ ] 有正式数据才显示成本。
- [ ] 估算有明确“估算”标签。
- [ ] 无正式数据不伪造。
- [ ] Existing Facility信息来自正式WorldState。
- [ ] 未完成升级系统不提供假升级。
- [ ] 正式Facility不能直接拆除。

## G. 世界状态

- [ ] WorldTime不变。
- [ ] FacilityId不变。
- [ ] FacilityPosition不变。
- [ ] Inventory不变。
- [ ] Person不变。
- [ ] Population不变。
- [ ] Market不变。
- [ ] Road不变。
- [ ] Water不变。
- [ ] Fortification不变。
- [ ] Owner不变。
- [ ] Controller不变。
- [ ] WorldSummary不变。

## H. 工程

- [ ] 编译通过。
- [ ] V2定向Core通过。
- [ ] Core全量通过。
- [ ] Project Load PASS或准确BLOCKED。
- [ ] EditMode PASS或准确BLOCKED。
- [ ] PlayMode PASS或准确BLOCKED。
- [ ] 当前任务diff check通过。
- [ ] 任务外既有FBX `.meta`问题未擅自修改。
- [ ] 无任务自动Unity进程遗留。
- [ ] 最终人工Unity实例保持打开。

---

# 二百一十二、实施顺序

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
正式玩家入口
洛阳布局包SHA
Facility数量
Far实际Facility显示数量
Road数量
Water数量
Fortification数量
当前Terrain Chunk数量
现有Batch Renderer
现有Building Asset系统
现有LOD
Core数量
Unity测试状态
```

不得覆盖用户现有修改。

---

## Step 1：复现当前视觉问题

使用当前正式Game View：

固定：

```text
Far
Mid
Near
```

截图。

必须保存：

- 黑Terrain；
- 中央灰白实体块；
- Urban Hull；
- 当前建设界面。

这些作为Before证据。

---

## Step 2：修Terrain Triangle Winding

只修几何错误。

先不要同时大改灯光。

---

## Step 3：验证Normals

Core + Unity视觉。

---

## Step 4：修Terrain Lighting

在正确Normals基础上：

调主光、环境光和基础材质。

---

## Step 5：关闭Far普通Facility Detail

停止大量盒子绘制。

---

## Step 6：建立Far Aggregate

首版可以测试8×8 PlanningCell聚合。

---

## Step 7：优化Aggregate形态

禁止大矩形。

加入：

```text
密度
体量
街道切割
高度变化
屋顶层次
```

---

## Step 8：建立Landmark Priority

Far保留大型地标。

---

## Step 9：Far Road骨架

强化官道，隐藏次要道路。

---

## Step 10：Far Water

强化主要河流。

---

## Step 11：Far Fortification

强化整体城墙和Gate。

---

## Step 12：默认关闭Urban Candidate Hull

只保留Debug入口。

---

## Step 13：Far Village

建立村落聚合。

---

## Step 14：Far Agriculture

建立农田Patch。

---

## Step 15：Far Vegetation

建立山林/河岸植被。

---

## Step 16：Far验收

在继续Mid前：

必须确认Far已经不再像数据调试图。

---

## Step 17：Mid建筑群

接Building Cluster。

---

## Step 18：Mid道路

展开城区主要道路。

---

## Step 19：Mid Facility选择

保证正式ID映射。

---

## Step 20：Near Facility Detail

具体模型 / Proxy。

---

## Step 21：县域Camera Preset

整理：

```text
县域总览
洛阳城区
建设规划
```

三个Camera / Tool入口。

---

## Step 22：建设Near Camera

建立合理默认可视尺度。

2.4×1.2km只作为体验调校候选。

---

## Step 23：底部建设栏

完成分类和卡片。

---

## Step 24：UI数据真实性

只显示已有正式数据。

---

## Step 25：World-Space Ghost

接正式Facility Resolver。

---

## Step 26：Local Planning Grid

只显示附近区域。

---

## Step 27：Road Draft

升级World-Space Ribbon Preview。

---

## Step 28：Wall Draft

升级World-Space Wall Preview。

---

## Step 29：Canal Draft

升级World-Space Channel Preview。

---

## Step 30：Zone Brush

保留规划层语义。

---

## Step 31：输入状态

整理：

```text
Left
R
Right
Middle
Alt+Right
Wheel
```

---

## Step 32：右侧Facility信息

接真实数据。

---

## Step 33：性能和Culling

避免新的Presentation过载。

---

## Step 34：Core定向

全部V2测试。

---

## Step 35：Core全量

执行正式全量回归。

---

## Step 36：Unity Project Load

使用安全流程。

---

## Step 37：Unity EditMode

执行V2定向。

---

## Step 38：Unity PlayMode

执行Far / Mid / Near / Planning正式流程。

---

## Step 39：截图

生成至少27张真实Game View证据。

---

## Step 40：Before / After

使用相同或等价镜头。

---

## Step 41：实施报告

完整记录。

---

## Step 42：系统总纲

准确更新。

---

## Step 43：最终打开Unity

进入：

```text
PlayableDemo
→ C 县域
→ 洛阳
```

停留在正式县域沙盘。

不得关闭。

---

# 二百一十三、实施报告要求

建立：

`REPORT_LUOYANG_COUNTY_STRATEGIC_SANDBOX_VISUAL_AND_CONSTRUCTION_INTERACTION_V2.md`

至少记录：

1. 开工HEAD。
2. Branch。
3. Workspace状态。
4. World Schema。
5. Unity版本。
6. 洛阳布局SHA。
7. Facility实际数量。
8. Far修改前Facility Detail数量。
9. 当前Road数量。
10. Water数量。
11. Fortification数量。
12. Triangle Winding问题根因。
13. Triangle Winding修复。
14. Normal验证。
15. Terrain Lighting。
16. Far修改前截图。
17. Far Facility suppression。
18. Aggregate Bucket方案。
19. 为什么选最终聚合粒度。
20. Aggregate构成算法。
21. Aggregate是否只有Presentation。
22. Landmark规则。
23. Far Road规则。
24. Far River规则。
25. Far Wall规则。
26. Urban Candidate Hull默认状态。
27. Village表现。
28. Agriculture表现。
29. Vegetation表现。
30. Far最终截图。
31. Mid建筑群。
32. Mid道路。
33. Mid Facility选择。
34. Near Facility。
35. Facility Model / Proxy。
36. Camera Preset。
37. 建设默认Near范围。
38. 底部工具栏。
39. 建筑卡数据来源。
40. 哪些数据尚未正式接入。
41. Building Ghost。
42. Local Planning Grid。
43. Road Draft。
44. Wall Draft。
45. Canal Draft。
46. Zone Brush。
47. Input State。
48. Facility右侧信息面板。
49. Spatial Culling。
50. Presentation Cache。
51. LOD Hysteresis。
52. Far/Mid/Near性能。
53. Planning性能。
54. Memory / GC。
55. No World Mutation。
56. Core结果。
57. Project Load。
58. EditMode。
59. PlayMode。
60. 环境BLOCKED如有。
61. Before/After目录。
62. 正式截图目录。
63. 用户人工验收状态。
64. 下一阶段建议。

---

# 二百一十四、系统总纲更新

更新：

`GAME_SYSTEMS_MASTER_AND_STATUS.md`

任务通过后可以准确写：

> 洛阳县域战略沙盘Presentation已完成V2分层重构：Far以山川、河流、官道、城墙、城市肌理、村落、农田和地标表现完整县域，不再逐个显示普通Facility；Mid提供街区、建筑群和经营对象阅读；Near提供具体Facility和World-Space建设工具。地形法线、Far设施堆积和城区候选边界等主要视觉问题已纠正，建设规划已切换为局部50m格、3D Ghost及道路/城墙/水渠世界空间预览。

不得写：

- 最终县域美术完成；
- 正式建设经济完成；
- ConstructionProject完成；
- 材料和工期闭环完成；
- NPC建设完成；
- 攻城完成；
- 全国县域全部具象化完成。

---

# 二百一十五、下一阶段正式任务门

只有本任务：

- 自动验收完成；
- Unity正式视觉通过；
- 用户人工确认县域沙盘和建设交互合格；

以后：

才进入：

**《洛阳县域正式建设事务、资源劳力与存档闭环 V1》**

---

# 二百一十六、下一阶段正式建设链

届时才实现：

```text
Draft
↓
提交正式建设
↓
建设权限
↓
土地使用权 / 许可 / 空间Reservation
↓
资金
↓
真实ProductBatch材料
↓
具体Person劳力
↓
运输
↓
ConstructionProject
↓
世界时间施工
↓
ConstructionSite
↓
阶段完成
↓
正式Facility / Road / Fortification / Canal
```

---

# 二百一十七、土地规则提前冻结

下一阶段不能写：

```text
扣除土地
```

正式语义应该是：

```text
检查产权 / 使用权
↓
取得许可 / 划拨 / 购买 / 征用（按已有制度）
↓
空间Reservation
↓
建设占用
```

土地本身不是像木材一样被“消耗掉”的资源。

---

# 二百一十八、正式施工前不能再Planning Undo

下一阶段：

Draft可以Undo。

Formal Construction开始以后：

只能：

```text
CancelConstruction
```

已发生：

- 时间；
- 材料；
- 劳动；
- 运输；
- 资金；

不得Ctrl+Z恢复。

---

# 二百一十九、强制最终用户现场验收

自动验证完成后：

**不得直接结束任务。**

必须打开正式Unity。

---

# 二百二十、最终人工验收入口

执行：

```text
PlayableDemo
↓
C 县域
↓
洛阳
```

首先停留：

`县域总览 Far`

---

# 二百二十一、用户现场Far检查

用户至少检查：

1. 地面是否还有大面积黑色。
2. 山体光照是否正常。
3. 洛阳城是否能够一眼看出。
4. 中央巨大灰白矩形是否消失。
5. 普通Facility是否不再Far逐个显示。
6. 地标是否仍存在。
7. 城墙是否形成整体围合。
8. 城门是否能辨认。
9. 官道是否明显。
10. 河流是否明显。
11. 城外是否有农田。
12. 是否有村落。
13. 是否有树林/植被层次。
14. 黄色城区候选大线是否默认消失。

---

# 二百二十二、用户现场Mid检查

15. 点击“洛阳城区”。
16. 相机是否自然进入Mid。
17. 建筑群是否展开。
18. 道路是否逐渐展开。
19. 大型Facility是否可辨。
20. 市场/工坊/官署/仓储等空间是否有初步差异。
21. 城门和道路是否对齐。
22. 城墙分段是否可辨。
23. Facility是否可以查看。

---

# 二百二十三、用户现场Near / Planning检查

24. 点击“建设规划”。
25. 相机是否自动进入合理Near尺度。
26. 是否不是固定不可移动的小窗口。
27. 底部建设分类栏是否出现。
28. 选择一栋建筑。
29. 3D Ghost是否跟随鼠标。
30. R旋转是否正常。
31. 绿色/黄色/红色是否合理。
32. 是否能看到Footprint。
33. 是否能理解Entrance和Road Access。
34. 50m格是否只在附近出现。
35. 远处格网是否渐隐/隐藏。
36. 拖一段道路。
37. 是否显示青色道路带。
38. 拖一段城墙。
39. 是否显示墙体预览。
40. 拖一段水渠。
41. 是否显示水渠预览。
42. Right Click是否正确取消。
43. 中键是否平移。
44. Alt+右键是否旋转镜头。
45. 滚轮是否缩放。
46. 建筑卡是否没有伪造材料/工期。
47. 右侧Facility信息是否来自真实世界字段。

---

# 二百二十四、用户现场Debug检查

48. 打开Debug。
49. 查看原Facility点。
50. 查看Road Graph。
51. 查看Urban Candidate Hull。
52. 关闭Debug。
53. 确认正式画面重新干净。

---

# 二百二十五、用户现场M/C/F检查

54. F进入人物。
55. 确认没有Planning Grid残留。
56. M返回天下。
57. C再次进入县域。
58. World-Space沙盘正常恢复。
59. 没有世界时间或资产变化。

---

# 二百二十六、最终默认交接画面

最终建议停留：

```text
C 县域
洛阳
Far / Mid之间
```

使用斜俯视战略沙盘镜头。

画面同时能看到：

```text
洛阳城区
城墙
城门
官道
河流
农田
村落
山地
```

并且：

```text
没有黑色Terrain
没有中央灰白Facility方块
没有巨大黄色Urban Candidate Hull
没有全县Planning Grid
没有Debug节点
```

---

# 二百二十七、Unity安全要求

如果用户当前已有Unity：

优先使用当前实例。

不得为了自动测试：

强行启动第二个冲突实例。

---

# 二百二十八、自动测试PID规则

只能终止：

本任务自动测试自己创建并记录的PID。

不得关闭：

用户已有Unity / Hub。

---

# 二百二十九、最终人工实例必须保持打开

全部验证完成以后：

```text
Unity
Play Mode
Game View
```

保持运行。

**不要关闭Unity。**

**不要退出Play Mode。**

**不要关闭Game View。**

**不要由自动清理脚本终止人工验收实例。**

---

# 二百三十、Unity环境BLOCKED

如果自动Unity验证因为：

项目已经被另一个Unity实例占用

而无法运行：

准确记录：

```text
BLOCKED_BY_EXISTING_UNITY_INSTANCE
```

不得：

标记PASS。

优先使用当前已打开Editor中的：

`Mandate > Validation`

菜单执行证据采集。

---

# 二百三十一、任务状态

如果：

- 代码完成；
- Core通过；
- Unity自动验证通过；
- 截图通过；
- 人工Review Game View已准备；

则：

`IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`

如果Core通过但Unity仍环境阻断：

使用准确的：

`IMPLEMENTED_CORE_ACCEPTANCE_PASSED_UNITY_ENVIRONMENT_BLOCKED_READY_FOR_USER_REVIEW_WHERE_AVAILABLE`

或项目当前等效正式状态。

---

# 二百三十二、不得提前ACCEPTED

只有用户现场明确回复：

```text
通过
可以
没问题
验收通过
继续下一阶段
```

以后：

才能改为：

`ACCEPTED`

---

# 二百三十三、用户现场不通过时

如果用户认为：

- Terrain仍怪异；
- 城市仍是一块；
- Far仍像Debug；
- Mid建筑群不自然；
- 官道不清楚；
- 农田太假；
- 城墙太弱；
- Camera不好操作；
- Near建设不顺手；
- Grid太多；
- Ghost不准确；

必须：

```text
记录问题
↓
修Presentation / Interaction
↓
定向Core
↓
全量Core
↓
Unity验证
↓
重新截图
↓
更新报告
↓
重新打开Game View
↓
重新人工验收
```

---

# 二百三十四、最终完成定义

本任务真正完成，不是：

> “修好了黑色地面并把几个盒子隐藏掉。”

而是正式建立：

```text
C 县域
=
一个可以长期游玩的县域主沙盘
```

Far：

```text
看山川
看城市
看城墙
看官道
看河流
看村落
看农业
```

Mid：

```text
看街区
看建筑群
看市场
看工坊
看仓储
看官署
看县城经营空间
```

Near：

```text
看具体Facility
看入口
看道路
看50m局部空间
做建设规划
拖Road
拖Wall
拖Canal
放Building Ghost
```

并且整个过程始终：

```text
只有一套正式县域世界事实
```

Presentation只负责：

```text
怎么显示
怎么聚合
怎么操作
```

不负责重新创造：

```text
Facility
Road
Water
Wall
Population
Inventory
WorldTime
```

---

# 二百三十五、最终执行顺序

请严格按照以下顺序执行：

```text
开工快照
↓
保存V1问题画面作为Before
↓
修Terrain Triangle Winding
↓
修Terrain Normals
↓
验证地形正面受光
↓
再调Terrain Material / Lighting / Fog
↓
禁止Far逐个绘制普通Facility
↓
建立Far Presentation Aggregate
↓
禁止Aggregate只是巨大矩形盒
↓
加入建筑密度 / 高度 / 街道切割 / 屋顶肌理
↓
建立Landmark Presentation Priority
↓
Far保留地标
↓
Far强化官道
↓
Far强化河流
↓
Far强化城墙 / 城门
↓
默认隐藏Urban Candidate Hull
↓
接Far村落
↓
接Far农田
↓
接Far树林 / 植被
↓
先完成Far视觉验收
↓
建立Mid建筑群
↓
展开Mid道路
↓
接Mid大型Facility与Facility选择
↓
完善Near具体Facility
↓
整理县域总览 / 洛阳城区 / 建设规划Camera Preset
↓
建设规划自动进入Near
↓
2.4×1.2km只作为Camera体验候选
↓
建立底部建设分类栏
↓
只显示已有真实经济/容量数据
↓
无正式数据不伪造材料 / 工期 / 维护
↓
建立World-Space Building Ghost
↓
建立局部50m Planning Grid
↓
建立World-Space Road Draft
↓
建立World-Space Wall Draft
↓
建立World-Space Canal Draft
↓
保留Zone Draft规划语义
↓
整理Left / R / Right / Middle / Alt+Right / Wheel输入
↓
接Existing Facility右侧信息
↓
优化Spatial Culling / Batch / Cache
↓
验证Far / Mid / Near Hysteresis
↓
验证M / C / F切换
↓
验证No World Mutation
↓
执行V2定向Core
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
记录性能
↓
生成至少27张真实Game View证据
↓
生成V1 / V2同镜头Before / After
↓
完成实施报告
↓
更新GAME_SYSTEMS_MASTER_AND_STATUS.md
↓
最后使用正式Unity进入PlayableDemo
↓
进入C县域洛阳
↓
停留在Far / Mid斜俯视县域沙盘
↓
保持Play Mode和Game View运行
↓
交给用户现场人工验收
```

不得在Terrain法线错误未修复时先大量调整灯光。

不得继续让Far绘制全部普通Facility。

不得把8×8聚合变成新的世界单位。

不得把2.4×1.2km建设镜头变成固定建设边界。

不得为了UI完整伪造建设材料、工期和维护费用。

不得提前创建ConstructionProject。

不得让Draft修改正式Facility、Road、Water、Fortification或LandUse。

不得通过修改权威50m数据解决Presentation问题。

不得重新建立独立“城市地图”。

不得在本任务完成并经用户人工验收以前进入正式建设经济与施工闭环。