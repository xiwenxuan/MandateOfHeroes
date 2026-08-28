# LUOYANG-PRODUCTION-BUILDING-MODULAR-KIT-AND-HIGH-FREQUENCY-CITY-FABRIC-V1

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 1. 任务目标

在既有36类洛阳程序化Facility模型和2,084项显式视觉绑定之上，把使用量最高的10类从
“Cube/Cylinder审图组合”推进为可复用的生产化程序模块：保持稳定Model ID、Facility身份、
Global Cell和Runtime Binding不变，增加生产资产变体、入口/放置锚点、原创自定义网格和三级LOD。

本轮10类覆盖184年洛阳开局1,800/2,084项Facility，覆盖率约86.4%。其余284项仍保持已经通过
验收的程序化V1表现，后续按A级地标差异化和中低频城市织物任务继续生产。

## 2. 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/MAP_ART_RESOURCE_PLAN.md`；
- `Docs/TASK_LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1.md`；
- `Docs/TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`；
- `Docs/TASK_LUOYANG_PLAYABLE_VERTICAL_SLICE_MAP_PRESENTATION_AND_CONSTRUCTION_ASSET_LIBRARY_V1.md`。

## 3. 基线事实

1. 合并模型目录已有36个稳定Model/Asset ID，正式2,084项Facility和61种Definition均已显式绑定。
2. 当前仓库没有正式FBX/OBJ/Blend/Prefab/Material建筑资产；现有建筑由运行时基础几何组合生成。
3. 高频10类及开局使用量为：住宅552、旱田361、道路359、工坊94、园圃92、仓库85、城墙76、
   宫墙70、客栈驿舍60、牧场51，合计1,800。
4. Model存在和可实例化不等于玩家拥有建设权限。宫墙仍是政府/历史初始化/事件向资产；本任务不修改
   通用建设菜单、蓝图权限、成本、工期、产权或建设结算。

## 4. 冻结决策

1. 新增独立`mandate.luoyang-production-building-kit.v1`内容合同，不改写既有36模型目录，不升级存档。
2. 生产配置必须以稳定Model ID引用既有模型，以独立Asset Variant ID标识生产变体。
3. 每项配置必须声明Cell中心/地形中心放置锚点、入口或连接锚点、地形贴合要求和三级LOD模块集合。
4. 自定义网格采用项目原创运行时Mesh并由工厂缓存复用；材质继续复用既有东汉中原调色板。
5. LOD0保留完整模块，LOD1保留战略轮廓，LOD2仅保留远景识别形；三个层级不得生成Collider。
6. 生产表现只存在于Presentation层，不能成为Facility存在、完成建设、占地、权限或存档权威。
7. 本任务只声明“生产模块V1可审图”，不冒充艺术家最终FBX、烘焙贴图、正式性能量产或最终美术定稿。

## 5. 实施范围

### 5.1 内容合同

- 冻结10个高频Profile、10个生产Asset Variant、开局使用量和1,800/2,084覆盖口径；
- 校验Model、Module、LOD列表、入口锚点、材质集和自定义Primitive ID；
- LOD2必须是LOD1子集，入口锚点不得超出声明的单Cell表现占地。

### 5.2 原创可复用网格

- 夯土收分块；
- 瓦面檐板；
- 田垄；
- 地形垫层；
- 中拱道路；
- 低多边形树冠；
- 墙顶/台基收分件；
- 八棱木构件。

网格由稳定Primitive ID按需生成并缓存，同一工厂内跨模型、跨LOD共享，不为1,800个Facility复制
独立Mesh资产。

### 5.3 运行时装配

- 高频模型实例附带Production Profile、Asset Variant、LOD Profile、地形贴合和锚点元数据；
- 每个实例建立LOD0/LOD1/LOD2与Unity `LODGroup`；
- 非高频26类继续走既有兼容工厂，不强行伪装为生产资产；
- `LUOYANG KIT`审图入口继续在36个正式Global Cell同屏检查，高频10类自动切换生产层级。

## 6. 不在范围内

- 不新增或移动Facility，不改变2,084项开局事实；
- 不修改人口、家户、岗位、库存、产权、建设菜单、建设结算或Save Schema；
- 不完成南宫/北宫/永安宫、十二城门、明堂/辟雍等A级地标的独立高模差异化；
- 不完成室内、导航、最终碰撞、攻城损毁、废墟、Addressables、全城Streaming或GPU量产基准；
- 不复制《三国志11》或任何商业游戏的模型、贴图、布局、图标、代码和数据。

## 7. 验收标准

1. 生产目录恰好10个唯一Profile/Model/Asset Variant，使用量恰好1,800，开局总量恰好2,084。
2. 10个Model均存在于既有36模型目录；所有Primitive覆盖和LOD模块均引用真实Module ID。
3. 10个生产实例均有有效放置/入口锚点、三级`LODGroup`、自定义Mesh和零Collider。
4. 八种原创自定义Mesh按Primitive ID缓存复用；非高频模型保持兼容实例化。
5. 运行时`LUOYANG KIT`保留36个模型，其中恰好10个启用生产配置；WORLD视角模型归零。
6. 输出1600×1000实际Unity Game View证据，不以概念图替代运行时证据。
7. 全工程编译、相关核心测试、目标EditMode、目标PlayMode、`git diff --check`和差异审阅分别记录。

## 8. 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_PRODUCTION_BUILDING_KIT_V1/`

## 9. 下一阶段门禁

本任务验收后，下一建模任务按以下顺序进入：

1. A级地标独立轮廓：南宫、北宫、永安宫、太学、明堂、辟雍、灵台、太仓、武库、濯龙园；
2. 十二城门和宫门的身份、朝向、门楼/瓮城差异化；
3. 市场、车马行、学塾、官署、军营等中频城市织物生产化；
4. 在代表性街坊完成Draw Call、LOD切换、遮挡和Streaming预算后，才允许全洛阳批量部署。

## 10. 执行记录

- 2026-08-27：完成任务书、独立生产内容合同、10项配置、八种原创网格、入口/放置锚点、三级LOD
  装配和非高频模型兼容路径。
- 全工程编译通过；NuGet漏洞源因当前网络不可达产生NU1900警告，既有`WorldMapPipelineTests.cs`
  保留3条CS0649警告，无编译错误。
- 相关核心合同测试通过1/1：冻结10个Model、1,800/2,084覆盖量和LOD子集不变量。
- Unity EditMode目标类通过2/2：验证内容目录、八种缓存Mesh、锚点、三级LOD、零Collider和兼容回退。
- Unity PlayMode图形化目标类通过1/1：36项模型同屏，恰好10项启用生产配置，切回WORLD后模型归零，
  并输出1600×1000实际Game View。
- 无图形批处理下同步调用`Camera.Render()`触发Unity 2022动态批处理原生崩溃；安全运行器均终止本任务
  进程。按视觉证据要求改用`-UseGraphics`后通过，未出现模型合同或测试断言失败。
- 当前结论是“高频生产模块V1目标验收通过、可供用户审图”。全量核心/Unity分组回归、最终艺术资产
  和全城性能量产仍是后续独立门禁。
