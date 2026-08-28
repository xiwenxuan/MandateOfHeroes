# 洛阳中频城市肌理建筑 V1 任务书

任务 ID：`LUOYANG-MEDIUM-FREQUENCY-URBAN-FABRIC-V1`
状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`
范围：洛阳184年开局市场/商铺、商队院、学校、地方官署与军营五类中频Facility表现
前置：设施模型覆盖、高频生产模块、A级地标、十二城门与宫门身份化 V1

## 一、任务目标

把高频住宅、田地、道路等之外最能形成城市生活观感的五类模型升级为可复用生产级战略院落：

- 市场与商铺形成临街摊面；
- 商队院体现宽车门、院墙、马厩和货物；
- 学校形成讲堂、东西学舍和内院；
- 地方官署形成门、照壁、两厢和正堂轴线；
- 军营形成营栅、帐幕、指挥屋、望台与旗帜。

新增内容只替换Presentation模型，不创建Facility、不改变数量、位置、权限、结算或存档。

## 二、真实数据审计

统计来源：

- `Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/facilities.json`
- `Assets/StreamingAssets/WorldMap/Luoyang184MetropolitanInitializationV1/facilities.json`
- `Assets/StreamingAssets/WorldMap/LuoyangFacilityModelCoverageV1/luoyang_facility_model_bindings_v1.json`

两包合计恰好2,084项开局Facility。本任务冻结：

| 生产模型 | Facility Definition | 开局数 | 肌理角色 |
|---|---|---:|---|
| 市场/商铺 | `facility.commercial.market`、`facility.commercial.shop_cluster` | 48 | 临街商业界面 |
| 商队院 | `facility.service.caravan_yard` | 45 | 车马与货运服务院 |
| 学校 | `facility.service.school` | 39 | 教育讲堂院 |
| 地方官署 | `facility.government.local_office`、`facility.public.county_office` | 16 | 地方行政轴院 |
| 军营 | `facility.military.barracks`、`facility.military.camp` | 10 | 军事营盘 |
| 合计 | 8个开局Definition | 158 | — |

高频生产包已覆盖1,800项；本任务完成后，高频＋中频生产Profile覆盖1,958/2,084项，约94.0%。A级地标和城门身份资产独立存在，不在此统计中重复计数。

## 三、范围边界

### 纳入

- 五个既有稳定Model ID；
- 五个独立Asset Variant；
- 原创模块化院落、入口/放置锚点和三级LOD；
- 15格代表街坊审图：每类出现3次并使用不同朝向；
- 与原模型完全相同的可用权限。

### 排除

- 水渠19、水井16、桥梁2等基础设施：进入基础设施表现专项；
- 矿场、采石场、林场等城外资源设施；
- 医馆、礼制建筑、特殊官署、宫殿等低频/身份建筑；
- 158项Facility的全城逐对象实例化与Streaming；
- 最终FBX、贴图烘焙、室内空间、人物演出和损毁状态。

## 四、数据与架构方案

新增 `mandate.luoyang-medium-frequency-urban-fabric-kit.v1` 静态内容合同：

- 每个Profile记录Model ID、覆盖Definition、开局使用量；
- `FabricRoleId`、`DensityClassId`、`StreetInterfaceId`使用开放稳定ID；
- Profile权限必须与基础模型集合完全相同，不能因美术升级扩大建造权限；
- 每个院落模块不得超过基础模型0.88 Cell的冻结占地；
- LOD2必须是LOD1子集；
- 缓存程序化Mesh由现有共享Mesh库复用。

Domain只保存数据合同与不变量，Persistence读取静态JSON，Presentation负责Mesh、相机、摆放和UI；不升级Save Schema。

## 五、15格审图方案

专用`FABRIC`视图以洛阳附近15个互异正式Global Cell组成3×5代表街坊，每隔一格摆放一座，五类各出现3次并轮换0°/90°/180°/270°。

这些是标记为PreviewOnly的模型审查实例，不声称对应Cell在世界账中存在该Facility；正式Facility数量和坐标仍以开局数据为准。为战略视距可读性，预览统一放大1.15倍，该缩放不进入占地或世界事实。

## 六、实施清单

- [x] 审计2,084项开局Facility并冻结五类158项。
- [x] 新增中频城市肌理数据合同、严格校验与读取源。
- [x] 制作五套院落模块和三级LOD。
- [x] 接入模型工厂、15格街坊计划、专用相机和`FABRIC`入口。
- [x] 完成核心、EditMode、PlayMode、截图和差异验收。

## 七、验收标准

1. Profile恰好5项，统计48/45/39/16/10，总计158。
2. Definition集合与开局统计吻合，最终生产覆盖记为1,958/2,084。
3. 五个Asset Variant、角色、街面接口和LOD0几何签名互异。
4. 权限与基础模型完全一致，官署/军营不得获得Player/Ai权限。
5. 每个实例有三级LOD、入口与放置锚点，无Collider。
6. 15个预览Cell互异，每类恰好3个，四向轮换。
7. 核心合同、目标EditMode和图形PlayMode通过并生成1600×1000 Game View。

## 八、执行与验收结果（2026-08-27）

- 全工程编译：通过；`Path/PATH`在验证子进程内归一为单一`Path`。
- 相关核心合同：1/1通过。
- 目标EditMode：3/3通过。
- 图形化PlayMode：1/1通过。
- 数据静态审计：5个Profile、158项用途、60个LOD0模块均未越出0.88 Cell占地。
- 截图：`HISTORICAL_WORLD_REFERENCE/LUOYANG_MEDIUM_FREQUENCY_URBAN_FABRIC_V1/Screenshots/01_FIFTEEN_CELL_MEDIUM_FREQUENCY_URBAN_FABRIC.png`，1600×1000。
- `git diff --check`：通过。

本次只执行了本任务定向核心和Unity测试，没有把全量回归写成已完成。首次受限环境EditMode
启动未在45秒内生成日志，安全脚本只终止本次PID；随后同过滤器非沙箱重试通过。

## 九、下一步

本任务之后执行的全城性能、基础设施和低频防御设施专项均已完成目标门禁；生产覆盖现为
2,023/2,084。下一批冻结为9项林场、6项采石场、5项矿山和6项稻田组成的资源与农业设施，不在
本任务中追溯扩张模型范围。
