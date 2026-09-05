# 任务书：统一世界州—郡国—县行政边界与县域规划视角 V1

## 一、任务定位

### 1.1 任务名称

统一世界州—郡国—县行政边界与县域规划视角 V1

建议任务文件名：

TASK_WORLD_ADMINISTRATIVE_BOUNDARIES_AND_COUNTY_PLANNING_VIEW_V1.md

如仓库当前已有连续 Milestone / P 编号，则沿用现有编号，不为本任务重新整理历史编号。

---

## 二、任务背景

当前下一阶段不再直接继续开发“洛阳城市视角”。

经过设计讨论，正式空间结构收敛为：

统一世界地图
→ 州 / 郡国 / 县行政边界
→ 选择具体县
→ 进入县域规划视角
→ 在同一县域中连续缩放
   → 县域整体
   → 村庄 / 农业 / 庄园
   → 城区
   → 街区
   → Facility
→ 人物近景

正式冻结以下原则：

1. 县域是地方规划的基础空间。
2. 城市不是独立的规划地图层级。
3. 城市只是县域内部的 UrbanArea / Settlement 空间。
4. 县域规划、城区规划、街区观察和 Facility 选择必须继续使用同一张统一世界地图。
5. 不因为洛阳、成都、邺等地点较著名，就额外创造一个“城市级规划空间”。
6. 洛阳与普通县在空间规划层级上遵守同一规则。
7. 州、郡国、县边界属于行政空间；势力控制、土地所有权和 Facility 所有权是另外的世界事实。
8. 地点名称按开局年份从已有历史名称资料中解析；世界创建完成后，本局显示名称固定，不再因为时间推进自动改名。

本任务的目标不是立即加入道路拖拽、住宅区划分或完整城市建设，而是先建立后续所有规划玩法依赖的正确空间基础。

---

# 三、现有项目底座

实施前必须先审计并复用现有正式数据和系统，不得平行再造第二套行政地图。

重点复用：

- 统一世界 Cell 网格。
- Stable Geography / StablePopulationRegion。
- AdministrativeUnit / 行政层级数据。
- 140 年行政与稳定地理数据。
- 现有 1182 项县级目录。
- 州、郡国、县行政父子关系。
- game_location_crosswalk。
- 地点稳定 ID。
- 地点时期名称资料。
- 世界地图、道路、河流、Facility 和现有地图相机。
- 当前地图缩放、平移和选择逻辑。
- 当前有限认知规则。
- 当前确定性世界与存档合同。

现有资料中的记录数量只作为开工参考。

任务开始时必须重新统计仓库当前实际数量。

如果实际数量已经变化：

以当前正式仓库数据为准。

禁止为了匹配旧文档中的数字修改正式数据。

---

# 四、正式地图空间结构

本项目不得建立以下五套相互独立地图：

WorldMap
ProvinceMap
CommanderyMap
CountyMap
CityMap

正确结构必须始终是：

One World
One Geography
One Cell Grid
One Facility World

不同所谓“地图层级”只是同一个世界空间上的：

Camera Scale
LOD
Administrative Overlay
Information Projection
Selection Scope
Planning Tools

因此：

天下视角
州域阅读
郡国阅读
县域规划
城区规划
街区观察

都不得成为第二套世界数据。

---

# 五、正式行政层级

本任务至少建立以下三级行政空间：

Province
州

Commandery / Kingdom / CapitalRegion
郡 / 国 / 与郡同级的京畿行政区域

County
县

逻辑层级为：

Province
└─ CommanderyEquivalent
   └─ County

郡、国、尹等历史行政名称可以有各自类型，但地图空间系统不得因此写三套重复逻辑。

应以稳定行政 Region 为基础，例如：

AdministrativeRegion
- Id
- RegionLevel
- RegionType
- ParentRegionId
- StableGeographyId
- DisplayName
- GeometryStatus
- Confidence
- Provisional

字段名称根据当前工程结构调整，不要求机械照搬上述名称。

---

# 六、县域正式成为基础规划空间

从本任务开始冻结：

“县域”是普通地方建设、土地规划、聚落发展和城市扩张的基础空间单位。

一个县域内部可以存在：

County
├─ 县城 / 主城
├─ UrbanArea
├─ 其他聚落
├─ 村庄
├─ 庄园
├─ 农田
├─ 林地
├─ 草地
├─ 水域
├─ 河流
├─ 渠道
├─ 道路
├─ 桥梁
├─ 渡口
├─ 驿站
├─ 市场
├─ 工坊
├─ 仓储
├─ 官署
├─ 军事设施
└─ 其他 Cell / Facility

必须明确：

County != City

County != UrbanArea

County != Facility

UrbanArea != Facility

县域是行政/空间范围。

城市只是县域内部形成的城市化区域。

Facility 是县域内部的具体设施。

---

# 七、取消独立“城市规划地图”概念

后续不得形成：

进入县域规划
→ 再进入城市地图
→ 再加载 City Scene

正确流程：

县域规划
→ 相机缩放
→ 城区
→ 相机继续缩放
→ 街区
→ 相机继续缩放
→ Facility

整个过程仍是同一县域和同一世界空间。

“城市规划”只是县域规划视角中的一个缩放和工具状态，不是新的世界层级。

---

# 八、洛阳的正式空间定位

不考虑洛阳是否为帝都，只按照统一空间层级处理。

结构为：

河南尹
→ 洛阳县
→ 洛阳县内的洛阳 UrbanArea
→ 街区
→ Facility

因此后续不再把正式模式命名为：

洛阳城市视角

而应统一使用：

洛阳｜县域规划

进入“洛阳｜县域规划”意味着查看整个洛阳县域。

默认相机可以优先把洛阳主要城区放在视觉中心，但玩家缩小或平移以后必须能够看到：

- 城外道路；
- 农田；
- 村庄；
- 庄园；
- 水系；
- 产业；
- 其他县域 Cell；
- 邻县连续地理。

同理：

涿县｜县域规划
广宗｜县域规划
邺｜县域规划
成都｜县域规划

全部使用同一套规则。

---

# 九、UrbanArea 必须允许动态变化

本任务暂不实现完整城市建设，但结构必须允许未来：

农村 Cell
→ 修建道路
→ 新建住宅
→ 新建市场 / 工坊
→ 人口与产业增加
→ 城市化连续
→ UrbanArea 扩张

UrbanArea 可以：

- 扩张；
- 收缩；
- 超过旧城墙；
- 与城外聚落连接；
- 因战争破坏出现非连续区域。

因此：

UrbanArea 绝不能成为县域规划地图的固定边界。

城墙范围也不得等于 UrbanArea。

正式概念必须继续区分：

AdministrativeRegion
行政县域

UrbanArea
城市化范围

FortifiedBoundary
城防范围

Settlement / FacilityGroup
具体聚落和建筑群

---

# 十、第一阶段核心成果：州—郡国—县边界

本任务最重要的可见成果是：

在现有统一世界地图上正式显示：

州界
郡国界
县界

这些边界必须来自世界行政地理事实，不得只是一层手绘装饰。

---

# 十一、Cell 与行政区域

开工后首先审计当前 Cell 与行政数据的正式关系。

目标上，每一个已经纳入正式行政空间的 Cell 都必须能够确定：

ProvinceRegionId
CommanderyRegionId
CountyRegionId

具体实现可以通过直接字段、空间索引或稳定映射获得。

重点是：

同一个 Cell 的行政归属只能有一个权威来源。

不得同时维护：

WorldCountyId
MapCountyId
VisualCountyId
PlanningCountyId

四套不同的县域事实。

PlanningCountyId 可以作为“当前玩家选择了哪个县”的 ViewState，但不能成为另一个行政身份。

---

# 十二、Cell 的县级唯一归属

对于已经纳入县级行政地图的 Cell：

一个 Cell 在同一时刻只能属于一个县。

不能出现：

Cell X
同时属于 County A 和 County B

边界可能是 approximate 或 provisional。

但行政归属必须唯一。

这是未来以下系统的基础：

- 县域选择；
- 官员权限；
- 税收；
- 土地审批；
- 征役；
- 治安；
- 县级建设；
- 县级人口统计；
- 县级财政；
- 县级市场；
- 县级 AI。

---

# 十三、行政边界生成

行政边界必须通过相邻 Cell 行政归属差异产生。

例如：

Cell A.CountyRegionId != Cell B.CountyRegionId
→ County Boundary

Cell A.CommanderyRegionId != Cell B.CommanderyRegionId
→ Commandery Boundary

Cell A.ProvinceRegionId != Cell B.ProvinceRegionId
→ Province Boundary

方格世界优先读取 Cell 共享边。

不通过两 Cell 中心连线猜边界。

---

# 十四、同一边的多级行政意义

同一 Cell 边可能同时满足：

县不同
郡国不同
州不同

逻辑上它同时属于：

County Boundary
Commandery Boundary
Province Boundary

但视觉层不得机械画三条重叠线。

建议：

BoundarySegment
- IsCountyBoundary
- IsCommanderyBoundary
- IsProvinceBoundary

然后 Renderer 根据缩放和优先级选择视觉样式。

优先级建议：

Province > Commandery > County

同一位置只显示当前尺度下最合理的边界表现。

---

# 十五、禁止手工描边作为权威行政区

严禁：

在 Unity Scene 里人工摆一圈 LineRenderer
→ 宣称这是正式县界

正确链路：

Administrative Geography
→ Cell Region Assignment
→ Boundary Builder
→ Boundary Cache
→ Renderer

行政边界必须能够从正式行政空间重新生成。

允许开发 Debug Tool 可视化边界。

不允许 Debug Line 成为正式世界事实。

---

# 十六、边界精度与历史资料

当前历史地理数据本身已经允许：

geometry_status:
- none
- approximate
- provisional
- verified

以及：

confidence
provisional
notes
source

因此本任务必须严格区分：

“游戏当前需要一个可使用的行政边界”

和

“我们拥有完全精确的东汉县界”

不是同一件事。

V1 可以使用：

approximate
provisional

行政区。

但不得把它们伪装成 verified。

---

# 十七、没有精确县界时的处理

某县如果已经拥有：

- 稳定 CountyId；
- 县名；
- 父级郡国；
- 州；
- 县治或近似定位；
- 稳定地理参考；

但是缺少可靠 polygon：

允许建立 provisional county region。

可依据当前稳定地理、邻接、地形和已有近似空间进行确定性分配。

但必须保存其精度状态。

禁止：

为了全国地图好看，人工制造“历史精确县界”。

---

# 十八、边界系统必须允许以后重新校准

边界渲染系统不能依赖某一版具体行政 polygon。

以后历史地理数据提高精度时，应能够：

更新 Cell → AdministrativeRegion
→ 重建 Boundary Cache
→ 地图自动得到新边界

而不需要重写：

- 地图 UI；
- 县域选择；
- 县域规划入口；
- 相机系统；
- 行政标签系统。

---

# 十九、1182县的正确使用边界

现有县级目录可以作为县级行政母版。

但本任务不是：

“逐个手工制作1182张县地图”。

本任务需要做到：

1. 行政边界系统的架构可以支持全国县级规模。
2. 已有正式县级映射的区域能正确显示。
3. 未达到 verified 精度的县保持明确的 provisional / approximate 状态。
4. 不因为内容尚未完整制作，就删除正式存在的县级行政身份。
5. 不要求1182县现在全部拥有完整 Facility、村庄和城市内容。

---

# 二十、行政边界、势力控制和产权必须分开

必须从本任务开始在地图概念上严格区分：

A. Administrative Boundary

州 / 郡国 / 县

表示行政空间。

B. Political / Actual Control

表示当前谁实际控制该 Cell / 地区。

C. Ownership

表示具体：

- 土地；
- Facility；
- 庄园；
- 产业；
- 房屋；

属于谁。

例如可以合法存在：

行政：
广宗县

实际控制：
黄巾组织

某 Cell Owner：
张氏家族

某 Facility Owner：
当地商人

不得因为黄巾占领广宗：

把广宗的 CountyId 改成黄巾。

---

# 二十一、行政边界视觉层级

正常玩家视图至少提供三级行政边界：

州界：
最强

郡国界：
中等

县界：
最细

具体颜色、虚实线、宽度等按照当前项目原创地图美术方向设计。

不得复制其他商业游戏行政边界视觉。

---

# 二十二、缩放 LOD

行政边界不能永远全部显示。

建议：

远距离 / 天下尺度：

显示：
- 州界；
- 州名；
- 必要的主要郡国界。

中距离 / 区域尺度：

显示：
- 州界；
- 郡国界；
- 郡国名称。

近距离：

显示：
- 郡国界；
- 县界；
- 县名。

进入县域规划：

显示：
- 当前县界重点突出；
- 邻县界；
- 重要地理；
- 县内道路 / 聚落 / Facility LOD。

实际缩放阈值不得直接照抄本任务书数值。

应根据现有世界地图相机和实际可读性校准。

---

# 二十三、行政标签 LOD

禁止天下视角直接显示全部县名。

必须建立行政标签 LOD。

例如：

Far:
Province Labels

Medium:
Commandery Labels

Near:
County Labels

County Planning:
Current County + Neighbor Counties

标签切换只影响 Presentation。

不得改变行政事实。

---

# 二十四、行政区域选择

玩家必须能够点击地图中的行政区域。

县级选择不能要求点击县界线。

正确逻辑：

Mouse World Position
→ Resolve Cell
→ Cell.CountyRegionId
→ Selected County

即：

玩家点击县域内部任何有效位置，都能得到该县。

---

# 二十五、根据缩放决定默认行政选择层

允许地图根据缩放自动确定选择层：

远：
州

中：
郡国

近：
县

也可以提供明确的行政层级查看选项：

州
郡国
县

但不得打开三个独立地图页面。

---

# 二十六、县域选中效果

玩家选中某个县以后：

当前县：
高亮

邻县：
正常或轻度弱化

远方：
根据相机和地图表现正常处理

选中县至少可以显示：

- 本局固定显示名；
- CountyId（Debug /高级信息）；
- 所属郡国；
- 所属州；
- RegionType；
- GeometryStatus；
- 当前实际控制者（如果玩家已知）；
- Cell 数量；
- 主要聚落；
- 主要道路。

人口、库存、资源、兵力等仍然必须遵守有限认知。

选择县域不得成为全知地图。

---

# 二十七、县域规划入口

县域选择界面增加：

进入县域规划

执行后：

CurrentMapMode = CountyPlanning
PlanningCountyId = SelectedCountyId
CameraFocus = SelectedCountyBounds

这里的：

CameraFocus

绝对不能变成：

PlayerLocation

玩家选择远方县并进入县域规划：

不得把人物瞬移过去。

---

# 二十八、县域规划必须仍然使用统一世界地图

禁止：

LoadScene("LuoyangCounty")

禁止：

CloneCountyWorld()

禁止：

CreateCountySimulation()

禁止：

CreateLocalFacilitiesFromWorldFacilities()

正确处理：

统一世界地图继续存在

只修改：

- MapViewMode；
- Camera；
- LOD；
- AdministrativeOverlay；
- SelectionScope；
- PlanningOverlay；
- UI。

---

# 二十九、县域规划默认镜头

进入县域规划时：

相机必须自动 Fit 当前县域范围。

默认画面应让玩家首先理解：

“这是一个完整县域。”

而不是：

“这是县城的一小块”。

如果当前县存在主要 UrbanArea：

可以让主要城区落在视觉重点位置。

但仍应能够理解：

- 县界；
- 城区位置；
- 城外空间；
- 对外道路。

---

# 三十、县域规划允许看到邻县

进入县域规划后，不建议把县外全部裁掉。

相机可以保留适量 margin。

目标效果：

当前县：
完整、高亮、主要操作空间

邻县：
继续显示地形、道路、水系和必要聚落

这样玩家可以理解：

- 跨县道路；
- 河流流向；
- 山谷连续；
- 邻县位置；
- 将来的跨县物流。

县外显示不能因此赋予玩家规划权限。

---

# 三十一、县域规划中的连续缩放

进入县域规划后，应允许继续滚轮缩放：

县域远景
↓
聚落 / 农业尺度
↓
城区尺度
↓
街区尺度
↓
Facility尺度

这些都属于：

CountyPlanning

不要在城区尺度自动：

Load City View

不要在 Facility 尺度自动：

生成第二套近景世界。

---

# 三十二、人物近景暂不属于本任务重构重点

本任务不负责完成正式人物近景视觉纠错。

但县域规划的设计不得阻碍以后：

县域规划
→ 放大到 Facility
→ 进入人物近景

人物近景仍然必须引用同一个：

CellId
FacilityId
PersonId

后续另立任务处理：

- 人物近景巨大 Cell；
- 战略模型尺度错误；
- 人物尺度建筑群；
- 街巷；
- NPC生活表现。

---

# 三十三、地点显示名称规则

本任务同时正式冻结地点名称规则。

规则不是：

“所有剧本永远使用一个固定名字”。

也不是：

“游戏运行期间跟着历史年份不断自动改名”。

正确规则：

World Creation
→ Read ScenarioStartYear
→ Resolve historical name for that starting year
→ Freeze this world's DisplayName
→ Do not auto-rename during later simulation

---

# 三十四、稳定ID与显示名称分离

任何地点都必须：

StableLocationId
永久不变

WorldDisplayName
创建世界时决定

例如一个稳定地点具有时期名称资料：

早期：
秣陵

后期：
建业

那么：

184年开局
→ 本局名称 = 秣陵

220年开局
→ 本局名称 = 建业

184年开局以后即使游戏推进到220年之后：

仍然显示：
秣陵

不自动改成建业。

---

# 三十五、名称资料来源

名称解析必须优先使用项目现有已整理资料。

例如：

- names_by_period；
- 历史城市资料；
- 行政地名数据；
- 已有别名/时期名称映射。

不得重新建立第二套：

ScenarioNameTable

除非当前正式地点定义确实缺少可表达此规则的结构。

如果现有资料不足：

使用正式 fallback 名称。

不得凭开发人员记忆补历史名称。

---

# 三十六、名称解析范围

名称解析规则至少可用于：

- 州；
- 郡 / 国；
- 县；
- 城市 / 治所；
- 关隘；
- 港口；
- 重要聚落。

前提是现有资料存在相应时期名称。

---

# 三十七、世界创建后名称固定

世界创建完成以后：

不得因为 Date Advance 自动修改：

- 地图标签；
- Location DisplayName；
- 行政区标题；
- 县域规划标题；
- 已生成任务显示名；
- 玩家已知地点名称。

稳定身份始终依赖 ID，不依赖中文名字。

---

# 三十八、县域规划 UI 命名

系统模式固定叫：

县域规划

具体标题使用：

本局已经冻结的地点显示名称
+
县域规划

例如：

洛阳｜县域规划

涿县｜县域规划

广宗｜县域规划

邺｜县域规划

成都｜县域规划

不要求所有 UI 都机械显示：

洛阳县域规划视角

详细行政信息可以另外显示：

所属州
所属郡国
行政等级

---

# 三十九、ViewState 与 WorldState 分离

建议整理非世界事实的地图状态：

MapViewState
- ViewMode
- SelectedAdministrativeRegionId
- PlanningCountyId
- CameraState
- OverlayState
- LabelLevel

具体结构根据当前 Presentation 架构调整。

这些字段默认属于：

UI / Presentation State

不得直接写进永久世界事实。

除非项目已有正式的 UI 偏好存储机制。

---

# 四十、进入县域规划不得改变世界

进入：

XX｜县域规划

必须满足：

WorldTimeDelta = 0

并且不能改变：

- PlayerPersonId；
- PlayerLocation；
- Person 状态；
- Household 状态；
- Facility 状态；
- Inventory；
- Market；
- Production；
- Population；
- Military；
- Command Queue；
- Event Queue；
- RNG世界结果。

---

# 四十一、禁止因地图观察触发模拟

禁止出现：

OpenCountyPlanning()
→ AdvanceDay()

禁止：

ZoomMap()
→ RecalculateMarket()

禁止：

SelectCounty()
→ LoadPopulationAndRegenerate()

禁止：

ShowFacility()
→ RerollVisualState()

禁止：

EnterPlanning()
→ RecalculateProduction()

地图视角只能：

读取
聚合
投影
显示

不能成为领域结算入口。

---

# 四十二、进入县域规划不等于取得建设权限

本任务必须从结构上区分：

“玩家正在查看这个县”

和

“玩家拥有这个县的规划权限”。

未来真正建设仍必须检查：

- 当前人物身份；
- 官职；
- 组织职位；
- 行政授权；
- Cell Owner；
- 土地权；
- 财政；
- 材料；
- 劳力；
- 法律或政策；
- 其他世界条件。

进入县域规划：

绝对不能自动给予全县建设权。

---

# 四十三、本任务不开发正式建设工具

本轮不开发：

- 道路拖拽建设；
- 拆路；
- 住宅区划分；
- 市场区划分；
- 工坊区划分；
- 农业区划分；
- 建筑蓝图放置；
- 城墙扩建；
- 水利施工；
- 土地征收；
- 建筑拆迁；
- Facility新建；
- Facility升级。

这些属于后续“县域规划建设工具”任务。

本轮只建立它们依赖的：

行政空间
+
县域选择
+
县域规划模式
+
连续地图尺度

---

# 四十四、本任务不重做洛阳城区

本轮不负责：

- 洛阳2084 Facility视觉重构；
- 巨大 Cell 方框纠错；
- 洛阳人物尺度建筑群；
- 市场正式 UI；
- 城市 NPC 表现；
- 洛阳完整建筑美术；
- 完整室内。

不要在本轮借机继续堆洛阳表现层。

先把空间层级做对。

---

# 四十五、边界缓存

行政边界不能每帧重新扫描全国 Cell。

必须建立缓存。

推荐逻辑：

Administrative Geography Revision
→ Boundary Build
→ Boundary Cache
→ Render

只在真正影响行政几何的变化发生时重建。

Camera Pan：

不得重建全国边界。

Camera Zoom：

不得重新计算行政归属。

切换 Overlay：

不得重新生成世界边界事实。

---

# 四十六、边界数据应支持 Chunk / Region 化

全国县级边界数量较大。

禁止：

每一个 Cell 边
=
一个永久 GameObject + 一个 LineRenderer

优先使用适合当前 Unity 地图结构的：

- chunk mesh；
- combined mesh；
- batched line geometry；
- pooled render chunks；
- GPU-friendly representation；
- spatial culling。

具体方案由开发审计现有地图 Renderer 后决定。

---

# 四十七、边界视觉与边界事实分离

应区分：

BoundaryTopology
权威边界拓扑

BoundaryPresentation
颜色、线宽、透明度、LOD

改变地图美术：

不得改变行政事实。

以后更换：

- 绢本地图；
- 军府舆图；
- 势力图；
- 地形图；

仍应复用同一 BoundaryTopology。

---

# 四十八、有限认知规则

基础行政地名和公开行政边界可以按现有地图知识规则显示。

但进入县域规划不能自动获得：

- 未知矿脉；
- 私人仓库库存；
- 隐藏组织设施；
- 敌军秘密部署；
- 未知生产能力；
- 私人账册；
- 未知道路状态；
- 未侦察资源。

专题层仍然读取既有有限认知系统。

---

# 四十九、Debug Overlay

本任务应增加行政空间 Debug 能力。

至少可以显示：

CellId
CountyRegionId
CommanderyRegionId
ProvinceRegionId
GeometryStatus
Confidence
Provisional
BoundaryLevel

选中县时可额外显示：

CountyId
ParentCommandery
ParentProvince
CellCount
BoundarySegmentCount

Debug 默认关闭。

不得污染普通玩家地图。

---

# 五十、地图层级 Debug

建议提供开发快捷 Overlay：

Province
Commandery
County
All

用于快速确认：

- 内部边界是否消失；
- 父级边界是否正确；
- 相邻县是否连续；
- Cell行政归属是否存在洞；
- 一个 Cell 是否错误归属多个县。

不要求把这些 Debug 控件作为正式玩家 UI。

---

# 五十一、行政数据完整性检查

本任务必须建立或补充行政地理校验。

至少检查：

1. County 的 ParentRegion 存在。
2. CommanderyEquivalent 的 ParentProvince 存在。
3. 不允许行政父级循环。
4. CountyId 唯一。
5. RegionId 唯一。
6. 已纳入行政网格的 Cell 有唯一县级归属。
7. County 对应的上级郡国和州可以解析。
8. 不同 Region 不因显示名称相同发生误合并。
9. 地图引用使用 ID，不使用显示名 Join。

---

# 五十二、名称解析自动测试

至少覆盖：

Case A：

某地点具有两个时期名称。

StartYear 早于变化点。

结果：

DisplayName = EarlyName

Case B：

StartYear 晚于变化点。

结果：

DisplayName = LaterName

Case C：

从 EarlyName 开局。

推进时间超过历史改名年份。

结果：

DisplayName 仍然 = EarlyName

Case D：

没有对应时期名称。

结果：

使用正式 fallback 名称。

Case E：

名称不同但 StableLocationId 相同。

结果：

所有世界引用保持不变。

---

# 五十三、Boundary Determinism 测试

同一：

Administrative Data
Stable Geography
Cell Grid
Version

重复生成行政边界。

必须得到一致：

Province Boundary Summary
Commandery Boundary Summary
County Boundary Summary

不得使用：

UnityEngine.Random
DateTime.Now
GetInstanceID()
Dictionary无序遍历

决定边界结果。

---

# 五十四、Boundary Adjacency 测试

对相邻 Cell：

如果：

CountyId 相同

则：

不得产生 County Boundary。

如果：

CountyId 不同

则：

必须产生 County Boundary。

对郡国和州执行相同合同。

---

# 五十五、边界层级测试

构造：

Case 1：
县不同，郡相同，州相同。

结果：

只有县级语义边界。

Case 2：
县不同，郡不同，州相同。

结果：

具有县界 + 郡界语义。

Case 3：
州不同。

结果：

必须具有州界语义。

Renderer 根据当前 LOD 选择表现。

---

# 五十六、County Picking 测试

地图任意点击某有效 Cell：

SelectedCountyId
必须等于：
Cell.CountyRegionId

禁止通过：

最近县城
最近标签
最近边界线

猜县域。

---

# 五十七、进入县域规划测试

选中 County A：

执行：

EnterCountyPlanning(A)

必须：

PlanningCountyId == A

ViewMode == CountyPlanning

CameraTarget == A的空间范围

同时：

PlayerLocation 不变。

---

# 五十八、远方县观察测试

玩家人物位于 County A。

地图选择 County B。

进入：

County B｜县域规划

必须：

Player.CurrentCounty == A

PlanningCountyId == B

禁止人物瞬移。

---

# 五十九、世界状态无变化测试

进入县域规划前后比较至少：

WorldTime
PlayerPersonId
PlayerLocation
PopulationSummary
FacilitySummary
InventorySummary
MarketSummary
WorldDeterministicSummary

全部必须一致。

如果已有正式 Hash / Summary 工具，应直接复用。

不要再写一套 Demo 专用摘要。

---

# 六十、缩放测试

从天下尺度连续缩放到县域尺度：

必须正确切换：

州界
→ 郡国界
→ 县界

不得：

- 边界闪烁严重；
- 重复叠线；
- 标签全部堆叠；
- 行政区域突然错位；
- Camera Zoom 导致重新生成行政事实。

---

# 六十一、Unity EditMode 测试

至少覆盖：

- AdministrativeRegion validation；
- Cell administrative assignment；
- Boundary generation；
- Boundary hierarchy；
- County picking；
- ViewState；
- County planning entry；
- name-at-world-creation resolution；
- frozen world display name；
- deterministic boundary summary。

新增测试必须全部通过。

现有适用 EditMode 全量回归必须通过。

---

# 六十二、Unity PlayMode 测试

至少验证：

Case A：

正式入口启动成功。

Case B：

打开统一世界地图。

Case C：

缩放以后可以看到州 / 郡国 / 县边界 LOD。

Case D：

点击一个县可以选中正确 County。

Case E：

点击“进入县域规划”。

相机正确聚焦。

Case F：

进入县域规划后人物不移动。

Case G：

县外邻接地理仍可见。

Case H：

退出县域规划以后回到原地图阅读状态。

具体测试实现应复用当前正式 PlayableDemo / 正式地图入口。

不要另外建立一套永远只供测试使用的假地图作为最终验收对象。

---

# 六十三、优先验证区域

全国边界系统必须按全国架构实现。

但人工视觉验收不需要一开始检查全部1182县。

首批至少选取几个结构不同的区域：

1. 洛阳所在区域；
2. 涿县所在区域；
3. 广宗 / 钜鹿附近；
4. 邺附近；
5. 一个跨州边界区域。

用于同时验证：

- 县界；
- 郡界；
- 州界；
- 普通县；
- 著名城市所在县；
- 中级行政区边界；
- 跨州边界。

不得只在洛阳周边硬编码通过。

---

# 六十四、真实 Game View 截图验收

本任务完成后必须输出真实 Unity Game View 截图。

至少包括：

01_world_province_boundaries.png

要求：
可以看出州界层级。

02_world_commandery_boundaries.png

要求：
中等缩放可辨认郡国边界。

03_world_county_boundaries.png

要求：
近距离能辨认县界。

04_county_selected.png

要求：
选中一个县，县域整体高亮。

05_county_planning_overview.png

要求：
进入一个县的县域规划，完整县域可理解。

06_county_planning_neighbor_context.png

要求：
当前县高亮，同时可以看见邻县、跨县道路/水系连续关系。

至少一个县使用洛阳。

但不能只提供洛阳。

---

# 六十五、分辨率验收

至少检查：

1280×720

1920×1080

重点观察：

- 行政标签；
- 县域选择面板；
- 进入县域规划按钮；
- 当前县标题；
- 地图边界；
- 相机默认构图。

不能因为较低分辨率导致主要地图功能无法操作。

---

# 六十六、性能记录

本任务需要建立行政边界显示基线。

记录至少：

World Map 当前 FPS
州界显示 FPS
郡国界显示 FPS
县界显示 FPS
进入县域规划耗时
Boundary Build耗时
Boundary Cache大小
Boundary Render对象/Chunk数量
GC Alloc明显峰值

不得为了漂亮性能数据删除正式行政区域。

---

# 六十七、禁止的性能作弊

禁止：

只显示少数开发过的县，其余县当不存在。

禁止：

Camera外行政区域从世界事实中删除。

禁止：

根据镜头随机生成假县界。

禁止：

每次进入县域规划重新随机分配 Cell。

允许：

远处不渲染县界。

允许：

远处使用低成本行政表现。

允许：

Chunk Cull。

表现可以按 LOD 减少。

行政事实不能消失。

---

# 六十八、存档边界

本任务原则上不因为：

- Camera；
- 边界渲染；
- ViewMode；
- Label；
- Overlay；

升级世界存档。

地点“开局年份名称解析并冻结”如果当前 WorldState 已有合适的世界名称快照 / ScenarioSnapshot，应复用。

如果确实需要新增持久字段来保存“本局冻结显示名”：

必须先审计现有：

ScenarioSnapshot
LocationSnapshot
names_by_period
世界创建流程

优先使用既有稳定世界快照体系。

只有无法表达时才允许增加持久字段。

若增加持久字段：

必须执行正式顺序迁移。

旧存档不得根据“当前读档年份”重新解析名称。

必须根据其原世界开局年份确定稳定结果。

---

# 六十九、禁止名称成为稳定引用

任何系统禁止：

FindCounty("洛阳")

禁止：

if (name == "广宗")

禁止：

Dictionary<显示名, County>

正式引用必须使用：

StableRegionId
CountyId
LocationId

显示名称只负责 UI。

---

# 七十、代码组织

不要把本任务全部塞进一个：

WorldMapController.cs

建议根据现有架构合理拆分：

Domain / Geography
- 行政Region权威结构

Simulation / Application
- 行政查询
- World creation name resolution

Presentation
- AdministrativeBoundaryBuilder / Projection
- BoundaryRenderer
- AdministrativeSelection
- CountyPlanningView
- MapLabelLOD
- MapInput

具体命名根据当前项目已有结构调整。

重点要求：

Presentation 不得直接修改世界行政事实。

---

# 七十一、不得重复已有系统

开工前必须搜索：

Administrative
Region
StableRegion
County
Commandery
Province
MapMaster
ScenarioSnapshot
MapView
Boundary
Cell
Crosswalk

确认现有实现。

如果已有部分功能：

扩展正式实现。

不要并行新建：

AdministrativeSystemV2
NewCountyMap
WorldMap2

除非现有架构确有不可修复的问题，并在报告中明确说明。

---

# 七十二、实施顺序

必须按照以下顺序推进。

## Step 0：开工快照

记录：

- 当前 HEAD；
- 当前分支；
- 工作区状态；
- Unity版本；
- 当前正式玩家入口；
- 当前行政数据记录数量；
- 当前县级目录数量；
- 当前稳定地理节点数量；
- 当前地点交叉数量；
- 当前存档版本；
- 当前核心测试数量；
- 当前 Unity 测试数量。

---

## Step 1：行政数据审计

确认：

州
郡国
县

是否可以通过稳定 ID 建立完整父级关系。

记录：

- 已映射县数量；
- provisional数量；
- approximate数量；
- verified数量；
- unresolved数量。

不要为了通过测试修改历史事实。

---

## Step 2：Cell行政归属审计

确认当前 Cell 如何关联：

County
Commandery
Province

如果已有正式路径：

复用。

如果缺失统一投影：

建立一个权威行政空间查询层。

---

## Step 3：行政完整性校验

先确保：

Cell
→ County
→ Commandery
→ Province

链路可解析。

再开始画边界。

---

## Step 4：Boundary Topology

实现：

Cell adjacency
→ Administrative Boundary topology

先通过纯逻辑测试。

此阶段不要急着做复杂美术。

---

## Step 5：Boundary Cache

实现稳定缓存和 Chunk。

确保 Camera 移动不会重算全国行政拓扑。

---

## Step 6：Boundary Renderer

接入当前统一世界地图。

先完成：

州界

再：

郡国界

最后：

县界。

---

## Step 7：LOD与标签

实现：

州
郡国
县

随缩放的信息密度变化。

---

## Step 8：行政区域点击

实现：

Mouse
→ Cell
→ Region

完成县域高亮和详情。

---

## Step 9：县域规划 ViewMode

实现：

EnterCountyPlanning()

只切换：

Camera
LOD
Overlay
Selection scope
UI

不修改世界。

---

## Step 10：相机与邻县上下文

确保：

当前县完整可读

同时保留：

邻县
道路
河流
地形

连续关系。

---

## Step 11：开局名称解析

复用现有时期名称资料。

实现：

ScenarioStartYear
→ Resolve DisplayName
→ Freeze Name For World

并通过测试。

---

## Step 12：自动测试

运行所有新增定向测试。

---

## Step 13：全量回归

运行当前项目正式：

- 编译；
- 核心测试；
- EditMode；
- PlayMode；
- Project Load；
- git diff --check。

---

## Step 14：人工截图和视觉检查

输出任务规定截图。

确认没有只在 Debug 数据层“理论正确”，而玩家地图无法阅读的问题。

---

# 七十三、任务报告

完成后新增正式报告。

建议：

REPORT_WORLD_ADMINISTRATIVE_BOUNDARIES_AND_COUNTY_PLANNING_VIEW_V1.md

报告至少包含：

1. 实际行政数据数量。
2. 实际可解析州数量。
3. 实际可解析郡国数量。
4. 实际可解析县数量。
5. provisional / approximate / verified / unresolved统计。
6. Cell行政归属方式。
7. Boundary生成方式。
8. Boundary Cache结构。
9. 行政边界与势力控制如何分离。
10. County Picking实现方式。
11. 县域规划 ViewMode实现方式。
12. 地点开局名称冻结实现方式。
13. 世界状态无变化证据。
14. 性能结果。
15. 测试实际结果。
16. 截图路径。
17. 已知历史地理精度限制。
18. 下一阶段建议。

---

# 七十四、系统总纲更新

任务通过以后更新：

GAME_SYSTEMS_MASTER_AND_STATUS.md

地图相关状态必须准确表述为：

已有行政边界可视化和县域规划入口

不得写成：

完整县域建设已经完成

不得写成：

1182县完整历史地理已经完成

不得写成：

洛阳城市建设完成

明确剩余：

- 县域规划建设工具；
- 洛阳县域正式空间恢复；
- 城市/聚落建筑群；
- 人物近景；
- Facility功能交互；
- 正式市场UI；
- 城市生活表现。

---

# 七十五、本任务明确不做

本任务不做：

1. 完整历史县界考证。
2. 为1182县逐个手工建模。
3. 洛阳完整城市美术。
4. 洛阳人物尺度建筑群。
5. 正式道路建设。
6. 正式建筑放置。
7. 天际线式分区工具。
8. 农田规划工具。
9. 城墙建设。
10. Facility功能重构。
11. 正式市场UI。
12. NPC城市生活。
13. 完整室内。
14. 新的战场地图。
15. 为行政区复制第二套人口。
16. 为县域创建独立模拟世界。

---

# 七十六、S0阻断

出现以下任一问题不得完成任务：

- 正式玩家入口无法运行；
- 行政数据损坏；
- Cell行政归属大量丢失；
- 存档无法加载；
- 行政边界生成导致世界状态变化；
- 进入县域规划导致玩家瞬移；
- 进入县域规划推进世界时间；
- 同一个 Cell 出现多个县级权威归属；
- 地点稳定ID因名称规则改变；
- 大量既有 Facility / Cell 被删除。

---

# 七十七、S1阻断

以下任一问题修复前不得完成：

- 县界与实际 Cell 行政归属明显错位；
- 大量县域不可点击；
- 点击县域经常选中邻县；
- 州界 / 郡界 / 县界视觉完全无法区分；
- 放大缩小过程中边界严重闪烁或错乱；
- 县名完全重叠导致地图不可读；
- 进入县域规划只能看到县城而看不到县域；
- 县域规划把邻县地理全部裁掉；
- 县界显示造成无法正常浏览地图的性能问题；
- 地点名称在同一局内随着年份推进自行改变。

---

# 七十八、正式验收标准

## A. 行政数据

- [ ] 州级 Region 可解析。
- [ ] 郡国级 Region 可解析。
- [ ] 县级 Region 可解析。
- [ ] County → Commandery → Province 父级关系有效。
- [ ] 已进入正式行政网格的 Cell 县级归属唯一。
- [ ] 行政引用使用稳定ID。

## B. 行政边界

- [ ] 州界可以生成。
- [ ] 郡国界可以生成。
- [ ] 县界可以生成。
- [ ] 同行政区内部不产生错误内部边界。
- [ ] 行政差异处产生正确边界。
- [ ] 边界生成具有确定性。
- [ ] 边界不依赖 Scene 人工描线。

## C. 地图表现

- [ ] 远景可以阅读州界。
- [ ] 中景可以阅读郡国界。
- [ ] 近景可以阅读县界。
- [ ] 行政标签具有LOD。
- [ ] 不会在天下图堆满全部县名。
- [ ] 当前县高亮清晰。

## D. 县域选择

- [ ] 点击有效 Cell 可以获得正确 CountyId。
- [ ] 县域内部任意合理位置均可选择。
- [ ] 不依赖点击边界线。
- [ ] 选中县可以查看所属郡国和州。

## E. 县域规划

- [ ] 可以从统一世界地图进入县域规划。
- [ ] 不加载第二套县地图。
- [ ] 相机 Fit 整个县域。
- [ ] 当前县完整可理解。
- [ ] 邻县地理仍然可见。
- [ ] 道路和水系保持连续。
- [ ] 县域规划中可以继续缩放到城区和Facility尺度。
- [ ] 城市没有成为第二套规划地图。

## F. 世界状态

进入县域规划前后：

- [ ] WorldTime不变。
- [ ] PlayerPersonId不变。
- [ ] PlayerLocation不变。
- [ ] Population不变。
- [ ] Facility状态不变。
- [ ] Inventory不变。
- [ ] Market不变。
- [ ] World deterministic summary不变。

## G. 名称

- [ ] 地点名称可按开局年份解析。
- [ ] 较早和较晚开局可以得到不同历史时期名称。
- [ ] 同一地点稳定ID不变。
- [ ] 世界创建后本局显示名称固定。
- [ ] 后续年份推进不自动改名。
- [ ] 缺失时期名称时有明确fallback。

## H. 工程

- [ ] 全工程编译通过。
- [ ] 新增核心测试全部通过。
- [ ] 当前适用核心全量测试通过。
- [ ] Unity EditMode通过。
- [ ] Unity PlayMode通过。
- [ ] Project Load通过。
- [ ] git diff --check通过。
- [ ] 没有误提交Library、Temp、Logs、tmp等缓存。
- [ ] 没有覆盖用户无关工作区修改。

---

# 七十九、完成后的正确玩家体验

完成以后玩家应当得到：

统一世界
↓
看到州界
↓
继续放大
↓
看到郡国
↓
继续放大
↓
看到县界
↓
点击“洛阳”
↓
洛阳县域高亮
↓
进入
“洛阳｜县域规划”
↓
相机看到整个洛阳县
↓
继续缩放
↓
看到洛阳城区
↓
继续缩放
↓
看到街区 / Facility

但是从始至终：

没有第二个洛阳
没有第二套县地图
没有第二套Facility
没有重新生成人口
没有人物瞬移
没有世界时间变化

---

# 八十、下一任务门

只有本任务通过后，才进入下一阶段。

下一阶段建议正式定为：

洛阳县域空间恢复与规划基底 V1

重点处理：

洛阳县域
├─ 洛阳UrbanArea
├─ 城墙 / 城门
├─ 宫区
├─ 城郊
├─ 村庄
├─ 农田
├─ 道路
├─ 水系
├─ 产业
└─ Facility空间布局

之后再进入：

县域规划建设工具 V1

届时正式研究并实现：

- 道路拖拽；
- 建设蓝图；
- 土地用途规划；
- 城市扩张；
- 村镇发展；
- 城墙扩建；
- 水利；
- 大型Facility选址；
- AI / 玩家建设权限；
- 材料、劳力、资金和工期结算。

在行政边界与县域规划空间正式通过以前，不继续在错误的“独立城市地图”思路上叠加建设功能。

---

# 八十一、最终完成定义

本任务真正完成的标准不是：

“地图上出现了几条州郡县线。”

而是：

项目第一次拥有一套可扩展的统一行政空间系统：

州
→ 郡国
→ 县
→ Cell

玩家第一次可以从统一世界地图准确选择一个县，并直接进入该县真实世界空间的规划视角。

同时正式确立：

县域是地方规划空间；
城市是县域内部UrbanArea；
城市不是第二张规划地图；
行政边界不等于势力控制；
行政边界不等于产权；
地点显示名根据开局年份确定；
本局创建后名称固定；
所有地图尺度共享同一个世界、同一批Cell和同一批Facility。

本任务通过以后，后续洛阳、涿县、广宗、邺、成都以及其他县域全部必须建立在这一统一合同之上，不再为著名城市单独创造平行空间体系。
