# LUOYANG-BUILDABLE-FACILITY-MODEL-KIT-V1

状态：`IMPLEMENTED_TARGET_TESTS_PASSED_FORMAL_UNITY2022_VERIFICATION_BLOCKED`

用户于 2026-08-26 接受东汉半写实、中低模、战略微缩建筑方向，并确认第一批制作范围为：

1. 住宅；
2. 仓库；
3. 工坊；
4. 市场；
5. 战地医院；
6. 城墙；
7. 城门。

本任务把已经接受的概念图落实为可由 Unity 直接实例化并放置到正式 Global Cell 上的程序化
模型组合。它延续既有 `FacilityDefinition → BuildBlueprint → FacilityVisualProfile → 模块资产`
合同，不建立第二套建设事实，不改变存档结构，也不把模型预览写成已经存在的 Facility。

## 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/UNIFIED_WORLD_FACILITY_AUTHORITY_AND_POLITICAL_AI.md`；
- `Docs/TASK_LUOYANG_PLAYABLE_VERTICAL_SLICE_MAP_PRESENTATION_AND_CONSTRUCTION_ASSET_LIBRARY_V1.md`；
- `Docs/TASK_HAN_WORLD_EXPLICIT_STRATEGIC_CELL_MAP_V1.md`；
- `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_BUILDABLE_FACILITY_MODELING_CONCEPT_V1/Concept/`。

## 已冻结决策

- 四类通用蓝图仍只有住宅、仓库、工坊和市场；玩家、AI与历史初始化复用现有入口。
- 战地医院继续使用既有军队权限、木料、皮革、资金、劳动和维护专项建设合同，不伪装成通用民用蓝图。
- 城墙与城门使用既有城防 Facility 和五 Cell 复合蓝图语义；单项模型仍各自落在一个正式 Cell 内。
- 南宫与其他历史独特建筑不进入第一批普通建设模型。
- 一个模型实例绑定一个正式 `WorldMapCellId`；局部模块只负责 Cell 内表现，不创建 SubCell。
- 一 Cell 约 2000 米；模型采用战略表现放大，视觉尺寸不得回写为建筑真实占地。
- 所有模型使用稳定命名空间 ID 与数据清单；普通内容扩展不依赖新增枚举或存档升级。

## 实施范围

### 1. 数据清单

建立 `mandate.han-buildable-facility-model-catalog.v1`，每项保存：

- 稳定 Model ID；
- Facility Definition / Visual Profile /来源建设合同；
- 单 Cell 战略表现占地比；
- 材料调色板与模块化组合；
- 模块 primitive、位置、旋转和缩放。

### 2. 七类模型组合

- 住宅：围院、门、主屋、厢房、灰瓦屋面；
- 仓库：围院、仓房、粮仓容器、装卸区；
- 工坊：作业厅、棚屋、窑炉、工作台；
- 市场：市棚、摊位、公共交易院；
- 战地医院：无现代标志的木布医帐、辅助帐与物资箱；
- 城墙：夯土墙身、垛口和守望台；
- 城门：门洞、双侧墙体、门楼和道路通道。

### 3. 直接摆放

- 提供按 Model ID、正式 Cell ID、Runtime Binding ID和四向旋转实例化的接口；
- 根节点位于 Cell 中心和地表高度；
- 模块边界不得越过本模型声明的单 Cell 表现占地；
- 运行时组件只保存表现绑定，不成为 Facility、产权或建设完成权威；
- 全国 WORLD 视角不生成建筑模型，只有 Region/City/Close 视角按需实例化。

### 4. 审查入口

在现有自然地图场景加入独立 `BUILDINGS` 审查开关和固定相机。审查模式在洛阳附近七个互异
Global Cell 上各放置一类模型，并保留显式战略格，以验证“一格一设施”的可读性。

## 不在范围内

- 不制作最终艺术家手工 FBX、高模、贴图烘焙或正式 LOD Mesh；
- 不新增第二批农田、驿舍、医馆学舍、官署或兵营模型；
- 不改变四类通用建设菜单、战地医院领域结算或城防复合蓝图规则；
- 不修改 Save Schema、不迁移存档、不物化新的洛阳 Facility；
- 不复制任何商业游戏素材、模型、界面或地图数据。

## 验收标准

1. 数据清单正好包含七个唯一稳定 Model ID，并通过重复、缺失、非法缩放和越界校验。
2. 七类模型均可生成非空 GameObject 层级，带渲染器、无不必要 Collider，并共享有限材料调色板。
3. 七个审查实例绑定七个互异正式 Cell；所有模块保持在单 Cell 表现边界内。
4. 战地医院不存在红十字或现代标志；南宫不出现在普通模型目录。
5. 关闭审查模式后不保留模型实例；切换 WORLD 视角不批量生成建筑。
6. 现有四类通用蓝图行为和既有显式战略格测试保持通过。
7. 全工程编译、核心测试、目标 EditMode、目标 PlayMode、视觉证据、`git diff --check`分别记录。

## 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_BUILDABLE_FACILITY_MODEL_KIT_V1/`

## 2026-08-26 执行结果

- 已建立七项数据驱动程序化模型清单，共 90 个可复用几何模块、9 种共享材质；
- 已提供 `Model ID + WorldMapCellId + Runtime Binding ID + 四向旋转` 的直接摆放接口；
- 已把七项模型资产 ID 接入既有 `FacilityVisualProfile`，并修正工坊被 `shop` 子串误判为市场的问题；
- 已增加 `BUILDINGS` 固定审查相机，在洛阳附近七个互异正式 Global Cell 各摆放一类模型；
- 审查模式隐藏植被以避免遮挡，关闭审查或切换 WORLD 后销毁模型实例并恢复植被；
- 已生成实际 Unity Game View 证据：
  `Screenshots/01_FIRST_BATCH_SEVEN_MODELS_ON_STRATEGIC_CELLS.png`；
- 隔离 Unity 6000.5 兼容副本完成全脚本编译，目标与直接相关回归共 11/11 通过：
  新增 EditMode 4/4、新增 PlayMode 1/1、战略格 4/4、四蓝图端到端 1/1、蓝图/视觉绑定分离 1/1；
- 正式项目锁定 Unity 2022.3.62f3c1，但当前主机未安装该 Editor，项目验证脚本要求的 Visual Studio
  MSBuild 也不存在，因此正式版本编译、Core Test Runner 和原生 2022.3 Unity 测试仍标记为环境阻塞，
  未据此伪报通过；隔离验证没有升级或改写正式工作区的 Unity 版本。
