# 洛阳三层视角、全城构图恢复与人物近景视觉纠错 V1 实施报告

## 1. 结论

本轮把 `Assets/Scenes/PlayableDemo.unity` 的普通玩家入口统一为同一本世界账上的三层视角：

```text
M = World / 天下战略视角
C = City / 洛阳全城视角
F = Person / 玩家真实位置近景
```

`LuoyangPlayableViewState` 只保存当前视角、地点焦点、Facility 焦点和玩家 Person 引用；它不进入
`WorldState`、不升级存档，也不拥有第二套人物、Facility、库存、市场或时间。城市观察建筑只修改
`ViewFocusFacilityId`，正式移动仍由既有 CellRoute、通行和世界命令完成。

本报告的最终验收结论必须以本文件第 7、8 节记录的真实测试、截图和性能证据为准。没有结果文件的
测试不会在此报告为通过。

## 2. 开工与权威数据

| 项目 | 实际值 |
|---|---:|
| 开工 HEAD | `940c4381da4cbb893c0882fd28e68914397af897` |
| 分支 | `codex/m23-p4-quality-artisan-growth` |
| Unity | `2022.3.62f3c1 (96770f904ca7)` |
| 正式入口 | `Assets/Scenes/PlayableDemo.unity` |
| World Save Schema | `79`（本任务未改变） |
| 洛阳正式 Facility | `2,084` |
| Facility 定义 | `61` |
| 城市资产变体 | `54` |
| 构图区 | `6` |
| 洛阳 Cell | `5,980` |
| Road Facility | `359` |
| Gate-type Facility | `18` |
| Bridge Facility | `2` |

城市视角完整投影 2,084 项真实 Facility，以 64 个低成本空间批次显示；没有沿用旧的 549 项审图
窗口冒充全城，也没有删除镜头外的 Facility 世界事实。城市层不会为 70 万永久人物创建 GameObject
或启动全员导航。

## 3. 三层视角结构

### 3.1 World

复用 `HanWorldNaturalMapController` 和正式全国 Cell 地图。打开 World 视角只切换相机、地图 LOD、
标签和表现对象，不推进时间。当前人物不因选择或查看洛阳而移动。

### 3.2 City

`LuoyangCityViewProjection` 从正式 Whole-City Composition、Facility、54 个 AssetVariant 和六类
District 生成只读全城投影。全城模式恢复城墙、城门、道路、水系、南北宫、市场、官署、太仓、武库、
南郊及外围的连续关系，并按远/中缩放控制标签密度。

左键拾取从实际城市投影返回正式 `FacilityId`。建筑卡显示名称、类型、区域/空间信息、Owner、
Controller、开放状态、Access 摘要和 Nearfield Visual Profile；`Enter`/按钮聚焦，`Home` 回到全城。

### 3.3 Person

默认 Person 模式不再显示九个巨大 `LOCAL_TERRAIN_*` Cell 地板、战略棋盘/Footprint Debug，也不
加载全城城市尺度建筑模型。它使用连续近景地面、人物尺度道路、入口和确定性院落/屋顶/墙段占位。
P0-1 占位几何限制在人物可读尺寸，正式城墙、城门和桥梁也使用独立上限，不能继承战略 footprint
后直接巨幅放大。

## 4. FacilityId 与世界身份

City Visual 和 Nearfield Visual 均反向引用同一正式 `FacilityId`。至少以下真实样本进入自动测试：

| 类型 | FacilityId |
|---|---|
| 市场 | `facility.instance.luoyang.v1.recommended.000325` |
| 广阳门 | `facility.instance.luoyang.184.gate.guangyangmen` |
| 武库 | `facility.instance.luoyang.184.arsenal` |
| 太仓 | `facility.instance.luoyang.184.taicang` |

`NearfieldFacilityVisualDefinition` 的对应实现为 `LuoyangNearfieldVisualProfile`，提供稳定的
`ProfileId`、Capability、StableVariantIndex、Height、StructuralProxy 和 `ClusterHookId`；正式
`LocalMap` 的 Footprint、Entrance、Facing，以及 Whole-City Composition 的 District 继续作为空间
来源。它们共同形成可重建的近景表现定义，而不是第二套 Facility。Streaming 卸载/重载使用稳定 ID，
不重新随机入口或建筑变化。

## 5. 人物近景纠错内容

本任务从默认玩家画面移除或隔离了：

- 每个战略 Cell 直接生成的巨大正方形地板；
- Cell 边框、棋盘和 Facility Footprint 调试填充；
- 全城战略/城市模型在 Person 视角中的错误复用；
- “数百米空白 + 中央单一灰盒”的旧验证构图；
- 2.4 倍玩家表现缩放，玩家恢复为 `1.0` 人物尺度。

Debug 测试入口仍可显式打开 `LOCAL_TERRAIN_*` 和 Footprint，以保留空间诊断能力；普通
`PlayerDefault` 始终关闭。

最初证据曾暴露 S1：正式 Cell 间距直接进入近景后，目标建筑与邻居相隔约 2 km；同时只绘制
`BlocksPedestrian=true` 的 Footprint，使市场等非阻挡 Facility 可能退化成“空地 + 一座孤楼”。最终
实现增加 `LuoyangNearfieldUrbanContextProjection`：从同一正式 Facility 集合稳定选择焦点及最近的
八个结构性邻居，投影到人物尺度街区挂点，并生成两条低成本街巷占位。九个代理全部保留正式
`FacilityId`，Collider 关闭，紧凑坐标只供 P0-1 表现，不能参与移动、Access、Owner、库存或模拟
结算。正式 Player 路线、Facility Footprint 与入口权威没有改变。

## 6. 世界状态保护

自动测试在 Person → City → World → City → Facility Observation → Person 前后比较完整
`WorldSnapshotSerializer` 输出，并分别检查 PlayerPersonId、Player Facility、Cell 和
`ViewFocusFacilityId`。验收合同为：

```text
WorldSnapshot before == WorldSnapshot after
WorldTimeDelta == 0
PlayerLocation before == PlayerLocation after
Observed FacilityId == City selected FacilityId
F returns camera to the player's real location
```

Save Schema 继续为 79；本任务没有持久化 ViewMode、Camera、标签、选择或 Visual Profile。

## 7. 自动验证

最终测试数字在受控测试完成后写入。编译只报告既有环境类 `NU1900`：受限网络下无法读取 NuGet
漏洞数据源；程序集仍完整生成。本任务新增代码没有新增 C# 编译 Warning。

| 门禁 | 结果 | 证据 |
|---|---|---|
| 全工程编译 | 待最终汇总 | `tmp/skill-verification/` |
| 本任务目标 Core | 待最终汇总 | `tmp/skill-verification/` |
| 完整 Core | 待最终汇总 | `tmp/core-test-groups/luoyang-p01-20260901/` |
| Project Load | 待执行 | `tmp/unity-validation/` |
| Unity EditMode | 待执行 | `tmp/unity-validation/` 或分组目录 |
| Unity PlayMode | 待执行 | `tmp/unity-validation/` |
| `git diff --check` | PASS | 仅有工作区既有换行转换提示，无 whitespace error |

## 8. 截图、分辨率与性能

真实 Unity Game View 证据目录：

```text
Docs/Evidence/LuoyangP01ThreeLevelViewV1/
```

图形 PlayMode 固定生成并逐张人工复核了以下真实 Unity Camera/Game 画面；六张均为 1280×720：

1. `01_world_view_luoyang.png`：正式全国自然地图和洛阳位置；
2. `02_luoyang_city_overview.png`：2,084 Facility 全城构图、城墙、宫区、水系和外围；
3. `03_luoyang_city_mid_zoom.png`：中景密度、道路和市场 Facility；
4. `04_luoyang_facility_selection.png`：广阳门正式 FacilityId 选中标记；
5. `05_luoyang_person_nearfield.png`：人物、街巷和紧凑相邻院落，不显示战略 Cell 方框；
6. `06_luoyang_nearfield_urban_scale.png`：人物、两条街巷和九个正式 FacilityId 的人物尺度代理关系。

`performance-baseline.json` 的真实图形批处理基线如下。批处理主屏幕报告 640×480，证据 Camera 使用
固定 1280×720 RenderTexture；FPS 为进入对应视角后连续 60 帧的非缩放时间采样。

| 视角 | FPS | 进入耗时 | 活动 GameObject | Managed GC 增量 |
|---|---:|---:|---:|---:|
| World | 5.441 | 11,013.876 ms | 14 | 0 B |
| City | 107.736 | 428.715 ms | 572 | 2,510,848 B |
| Person | 94.459 | 354.738 ms | 766 | 7,057,408 B |

测试机为 Windows 11 10.0.26200、AMD Ryzen 9 8940HX、32 logical processors、Unity
2022.3.62f3c1。City 与 Person 浏览达到本任务可用标准；World 首次构建约 11 秒且采样仅 5.4 FPS，
是正式全国自然地图既有的明确性能债，不能用删减 Cell/Facts 伪装修复。任务 S1 针对的洛阳 City
浏览没有触发。1280×720 与 1920×1080 的 HUD/标签裁切复核结果在最终人工分辨率检查后写入。

## 9. 边界与后续

本任务不等于“洛阳城市玩法全部完成”。以下内容仍明确未完成：

- P0-2：12—16 套正式人物尺度建筑群与 40—60 个组合模块；
- P0-3：正式 Facility Capability/Operation 功能交互面板；
- P0-4：正式市场行情、买卖、委托、商旅准备与货物 UI；
- P1-1：城市 NPC 生活、关注演出和人群表现；
- M26：20—30 分钟独立人类盲玩最终门禁仍未完成。

只有本报告第 7、8 节取得真实通过结果、六张 Game View 不存在 S0/S1 后，才允许把 P0-1 标为
`ACCEPTED` 并进入 P0-2。P0-2 只能替换 Nearfield Visual 模块，不能改变本任务冻结的同一世界、
同一人物和跨视角 `FacilityId` 合同。
