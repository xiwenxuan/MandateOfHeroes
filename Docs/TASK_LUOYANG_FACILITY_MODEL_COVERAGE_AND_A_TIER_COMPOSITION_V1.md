# LUOYANG-FACILITY-MODEL-COVERAGE-AND-A-TIER-COMPOSITION-V1

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 1. 目标

在用户已接受的东汉中原半写实、中低模、战略微缩方向上，把第一批七类模型扩展为可覆盖184年
洛阳正式组合世界全部2,084项Facility的程序化模型与显式视觉绑定。实现必须继续使用同一Global Cell、
同一Facility身份和既有Runtime Binding，不生成2,084个互不复用的模型，不把预览对象写入世界事实。

## 2. 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/TASK_LUOYANG_184_HISTORICAL_V1.md`；
- `Docs/TASK_LUOYANG_184_URBAN_INITIALIZATION_V1.md`；
- `Docs/TASK_LUOYANG_184_METROPOLITAN_INITIALIZATION_V1.md`；
- `Docs/TASK_LUOYANG_PLAYABLE_VERTICAL_SLICE_MAP_PRESENTATION_AND_CONSTRUCTION_ASSET_LIBRARY_V1.md`；
- `Docs/TASK_LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1.md`。

## 3. 冻结决策

1. 保留既有住宅、仓库、工坊、市场、战地医院、城墙、城门七项稳定Model/Asset ID。
2. 新增29项洛阳覆盖模型，总目录为36项，不更改Save Schema或任何Facility记录。
3. 开局2,084项Facility使用显式`FacilityDefinitionId → ModelId`数据绑定；武库因运行定义沿用
   `facility.storage.warehouse`，额外使用带历史依据的`FacilityId → ModelId`覆盖。
4. 道路、水渠、桥梁等作为线性或基础设施模块表现，不解释为普通院落。
5. 南宫、北宫、永安宫、宫墙、宫门、太仓、武库、太学、明堂、辟雍、灵台、中央官署与濯龙园
   使用历史初始化/事件向资产；普通玩家不能因模型存在而绕过建设与权限规则。
6. 一个审图实例仍绑定一个正式Global Cell；局部模块只负责单Cell内战略微缩表现，不创建SubCell。
7. 本轮交付为原创程序化3D V1，不冒充最终艺术家FBX、烘焙贴图、正式LOD、损毁或废墟量产资产。

## 4. 实施范围

### 4.1 模型补全

- 农业：旱作田、水田、园圃、牧场；
- 官署宫殿：地方官署、中央官署、宫殿建筑群；
- 教育：学塾、太学；
- 军事：营盘、烽火台、坞堡；
- 服务：车马行、客栈驿舍、医馆；
- 公共与资源：水井、水渠、桥梁、庭院广场、林场、矿场/采石场；
- 礼制：礼制大殿、灵台；
- A级历史组合：宫墙、宫门、太仓、武库、皇家苑囿；
- 线性基础设施：道路段。

### 4.2 显式绑定

- 数据清单覆盖正式Urban与Metropolitan包中全部61种开局Facility Definition；
- 保留额外历史定义和战地医院定义，普通内容扩展继续使用稳定命名空间ID；
- 修复粟田误判工坊、太学误判公共设施、里坊误判公共设施、坞堡误判住宅、宫殿误判普通官署；
- 未绑定的新Definition仍保留现有公共设施兼容回退，但验收不得依赖回退覆盖当前2,084项Facility。

### 4.3 审图入口

- 既有`BUILDINGS`入口继续审查第一批七类；
- 新增`LUOYANG KIT`独立固定相机，在36个互异正式Global Cell审查完整目录；
- WORLD视角不生成建筑；关闭审图后销毁全部预览实例并恢复植被。

## 5. 不在范围内

- 不修改人口、家户、岗位、库存、产权、建设结算或存档；
- 不物化新的洛阳Facility，不移动既有2,084项Facility；
- 不制作十二城门十二套完全独立的高模；命名、朝向和运行身份继续由Facility数据决定；
- 不完成全城Addressables/Streaming、室内空间、最终碰撞、导航或攻城损毁；
- 不复制任何商业游戏模型、贴图、布局、UI或代码。

## 6. 验收标准

1. 补充目录恰好29个唯一Model/Asset ID，合并目录恰好36个。
2. 正式2,084项Facility、61种Definition全部通过显式稳定ID解析到存在的Model。
3. 武库Facility实例覆盖到武库模型；粟田、太学、里坊、坞堡和宫殿回归案例全部正确。
4. 36项模型均可生成Renderer、无不必要Collider、模块不越过声明的单Cell表现占地。
5. `LUOYANG KIT`在36个互异正式Cell生成36个预览实例并输出实际Game View证据。
6. WORLD视角模型数归零；表现预览不创建、完成或修改Facility。
7. 全工程编译、核心测试、受控Unity目标测试、`git diff --check`和差异审阅分别记录。

## 7. 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1/`

## 8. 2026-08-27 目标验收记录

- 全工程编译通过；使用项目锁定的 Unity 2022.3.62f3c1，并以已安装的 Visual Studio 2022
  Build Tools 配合本机 .NET SDK 解析器完成解决方案编译。现有 `WorldMapPipelineTests.cs` 保留
  3 条 CS0649 警告，无编译错误。
- 相关核心测试通过 4/4；本轮只运行与洛阳建设蓝图、视觉绑定和历史宫殿权限直接相关的定向用例，
  未运行全部 709 项核心回归。
- Unity EditMode 目标类首次运行 1/5：四项失败均来自中央官署模型 `gatehouse.roof` 超出声明的
  单 Cell 占地 0.01。将该模块 `ScaleZ` 从 0.20 收紧为 0.18 后，重新运行通过 5/5；合并目录
  7 + 29 = 36 项，静态边界复核为 0 项越界。
- Unity PlayMode 图形化目标测试通过 1/1：`LUOYANG KIT` 在 36 个互异正式 Global Cell 生成
  36 项模型，切回 WORLD 后模型归零，并输出 1600×1000 的实际 Game View 截图。
- 当前结论为“目标自动化验收通过、可供用户审图”。程序化 3D V1、全量核心/Unity 分组回归和最终
  艺术家资产验收仍是独立门禁，因此不得标记为最终美术完成。
