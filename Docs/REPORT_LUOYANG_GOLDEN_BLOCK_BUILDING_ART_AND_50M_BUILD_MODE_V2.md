# 洛阳 Golden Block 建筑美术定型与 50m Cell 建设模式 V2 实施报告

## 1. 当前结论

本轮已完成 Golden Block V2 的代码、内容配置、定向 Core、显式 C# 编译、Unity Project Load、
EditMode 和 PlayMode 门禁。五类建筑已从类别
硬编码组合升级为数据驱动 `BuildingPresentationProfile`，普通街区和建设 Ghost/Draft 共用同一套
院落模块语言；建设模式直接显示正式 50m PlanningCell，并区分 Hover、Selected、Covered 和
真实 Footprint。没有增加 5m/10m Cell，没有改变 2,084 项正式 Facility 或任何世界经济事实。

受控门禁解除后，Unity 2022.3.62f3c1 已在沙箱外按安全入口完成 Project Load、定向 EditMode 和
定向 PlayMode。截图菜单随后在可见编辑器中生成 `02—29` 共 28 张当前 V2 画面和指标 JSON；
完整性复核为 28/28 张 1920×1080、0 个重复哈希组，且自动回到
`PlayableDemo → C 县域 → Golden Block Mid`，Grid/Debug 关闭。当前可见 Unity 编辑器
保持打开供用户审阅。真实历史 V1 同机位图 `01_golden_block_v1_before.png` 在仓库中不存在，工具
按任务合同没有伪造。因此当前状态是：

`IMPLEMENTED_AUTOMATION_PASSED_CURRENT_EVIDENCE_READY_HISTORICAL_BASELINE_PENDING_USER_REVIEW`

这不是 `ACCEPTED`；当前实现和 28 张 V2 证据已可审阅，但真实 V1 Before 与用户最终确认仍待完成。

## 2. 开工与权威基线

| 项目 | 记录 |
| --- | --- |
| 开工 HEAD | `940c4381da4cbb893c0882fd28e68914397af897` |
| Branch | `codex/m23-p4-quality-artisan-growth` |
| Workspace | `E:/project/MandateOfHeroes` |
| Unity | `2022.3.62f3c1` |
| World Schema | `79`，未修改 |
| 县域布局包 | `mandate.luoyang.county-layout-50m.runtime-authority.v1` |
| 布局文件 SHA-256 | `C486AF5CFA75335CCEEF4C0738357CF4DE0A6F24ED8E8A34C76E5EA1F1A63A58` |
| Golden Block | 行 `168—175`、列 `232—239` |
| 正式尺寸 | `8×8×50m = 400×400m` |
| 正式 Cell | `64` |
| 表现 Lot | `16`，只读派生构图，不是新 Cell/Facility |
| 正式 Facility | `2,084`，未增删 |

## 3. V1 问题与 V2 建筑美术管线

V1 已能表达连续街区、五类院落、墙、巷、屋顶和小品，但模块组合仍集中在渲染器类别分支，
屋顶/台基/院门差异偏弱，Ghost 仍容易退化成简单体块，建筑栏还可能暴露与本轮目标无关的军用
候选。V2 以五套稳定 Profile 收口这些问题。

| 建筑族 | Profile | 模块与轮廓 | 地面/围合/道具 |
| --- | --- | --- | --- |
| 住宅 | `presentation.building.han.residence.v2` | 主屋、可选配房、双坡屋顶 | 夯土地、家宅墙门、生活堆物、庭树 |
| 市场 | `presentation.building.han.market.v2` | 临街主厅、开放棚、摊位 | 硬化前场、宽门、货物摊位、稀疏树木 |
| 工坊 | `presentation.building.han.workshop.v2` | 低坡作业棚、长棚 | 开放作业场、木栅、材料堆、少树 |
| 仓廪 | `presentation.building.han.granary.v2` | 多列长仓、长脊 | 正式台基围墙、宽门、装卸堆物 |
| 官署 | `presentation.building.han.government.v2` | 抬高四坡正厅、厢房、门楼 | 中轴庭院、正式围墙、标志物、成对树 |

Profile 同时记录重要度、模块密度、对称度、道路轴向、Far/Mid/Near 策略和尺度。模块可按稳定
FNV-1a 规则选择可选项；相同 `profile + source id + salt` 的模块、屋顶变体和签名完全一致，不使用
帧数、时间、实例 ID 或非确定性随机数。屋顶除暖瓦/深瓦/风化瓦色阶外，还包含 Domestic Gable、
Market Canopy、Workshop Low Gable、Granary Long Gable、Civic Raised Hip 五种轮廓及屋脊/檐口。

V2 使用统一资产尺度 `1.0` 和真实米制 Footprint；表现模块围绕 Facility 锚点排布，但不改写
Facility 的权威 Footprint/Entrance。Golden Block 以 11 组共享材质/批次生成地面、巷道、墙、
台基、木构、屋面、屋脊、门楼、道具和植被。当前采集结果为 86 个可见模块、21 个道具、18 个
植被实例、6,176 个三角形、11 个材质组；最终采集时 `Time.smoothDeltaTime` 换算近似 152.84 FPS。
该 FPS 只代表本机当前编辑器采样，不替代平台 GPU、Profiler Draw Call、Memory/GC 正式基准。

## 4. 50m Cell 建设模式

- 普通县域/Golden Block Mid：Grid 关闭。
- 建设规划 Near：直接显示现有正式 50m Grid，没有 5m/10m Cell。
- Cell 表现：低强度 Normal 线、Hover、Selected、Covered Cells 和真实米制 Footprint 轮廓。
- Grid 由共享网格生成并按当前局部视口裁剪；`PlanningCellGameObjectCount == 0`。
- 建筑栏：住宅、市场、工坊、仓廪、官署五类；展示用途、尺寸和道路条件，不伪造成本/工期。
- 烽燧数据保留用于权限回归，但不在五类建筑栏；官署候选不授予正式玩家/AI 建设权限。

Ghost、Draft 和既有 Golden Block 均解析同一 Profile 模块。Ghost 显示建筑组合、真实 Footprint、
Covered Cells、主入口和道路方向；`R` 后重新计算旋转尺寸和入口。110×80m 大型市场覆盖多个正式
Cell，小型住宅/工坊/官署可以只占 50m Cell 的一部分。Validation 继续使用现有地形、坡度、
水体、城防、设施碰撞、净空和道路接入规则。Draft 只进入 `CountyPlanningSession`，Undo/Redo 不会
改变正式世界序列化快照。

县域相机继续使用中键平移、`Alt+右键`旋转、滚轮缩放、普通右键取消；普通 Golden Block 聚焦
使用 Mid，单院落与建设模式使用 Near。

## 5. 代码与数据交付

- `CountyBuildingPresentationProfile.cs`：五类 Profile、模块模板、稳定变体与目录。
- `CountyGoldenBlockPresentation.cs`：V2 计划、Profile/ModulePlan 和稳定签名。
- `LuoyangCountyWorldSpacePresentationController.cs`：数据驱动院落、屋顶/台基/墙门/地面/小品、
  五态 Grid、Profile Ghost 和 Draft。
- `LuoyangCountyPlanningPresentationController*.cs`：Mid/Build Mode/单院落聚焦、悬停与选择、
  建筑栏候选和预览验证。
- `PlayableLuoyangGameController.cs`：五类底部建筑卡片。
- `facility_placement_profiles_v1.json`：复用既有模型/合同加入官署规划表现 Profile，总数 6；
  玩家面对的建筑栏仍严格为五类。
- `LuoyangGoldenBlockBuildModeV2FinalReviewMenu.cs`：正式 PlayableDemo 路由、02—29 图和指标采集。
- Core/EditMode/PlayMode 测试：稳定性、64 Cell/16 Lot、Profile、Grid、Ghost、跨格、Draft 与世界不变。

## 6. 验证结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 全工程编译 | PASS | `tmp/skill-verification/compile-20260904-170357-916.out.log`；显式纳入新增 Editor/PlayMode 文件后无 C# error |
| 定向 Core | PASS 4/4 | `tmp/skill-verification/core-tests-20260904-170422-266.out.log` |
| Golden 坐标审计 | PASS 1/1 | `tmp/skill-verification/core-tests-20260904-165754-329.out.log`，签名 `1493761582906843595` |
| Unity Project Load | PASS | `tmp/unity-golden-block-v2-project-load-escalated/unity-ProjectLoadSmoke-20260904-192841-667.summary.json`，72.229s |
| Unity EditMode | PASS 1/1 | `tmp/unity-golden-block-v2-editmode-final/unity-EditMode-20260904-193035-989.summary.json`，18.195s |
| Unity PlayMode | PASS 1/1 | `tmp/unity-golden-block-v2-playmode-rerun2/unity-PlayMode-20260904-193733-464.summary.json`，38.208s |
| V2 图形证据 | PASS 28/28 | `02—29` 全部为非空 1920×1080 PNG，SHA-256 重复组为 0；菜单报告 `EVIDENCE_READY` |
| 历史 V1 Before | MISSING | `01_golden_block_v1_before.png` 不存在；按合同不伪造 |
| 全工作区 diff check | 既有阻塞 | 本任务开始前四个 `Assets/ArtSource/Han/Luoyang/P0Final/*.fbx.meta` 存在尾随空格；未越权修改 |
| 本任务范围检查 | PASS | 本任务代码、测试、任务书、报告和指标文件无新增 whitespace error |

Core 通过项：

1. `LuoyangCountyPlanningProfiles_ReuseSixExistingContracts`
2. `LuoyangGoldenBlockV2_ProfilesAreDistinctAndStable`
3. `LuoyangGoldenBlockV2_UsesSixtyFourFormalCellsNotLots`
4. `LuoyangGoldenBlock_IsDeterministicAuditableAndPresentationOnly`

编译日志仍包含 NuGet 漏洞索引网络 warning 和既有测试 DTO 的未赋值 warning，均不是本任务编译
错误。全工作区 `git diff --check` 的 FBX `.meta` 问题与本轮文件无关，保留用户既有状态。

## 7. 证据、性能与人工验收

证据目录为：

`Docs/Evidence/LuoyangGoldenBlockBuildingArtAnd50mBuildModeV2`

正式菜单已生成 `02_golden_block_v2_overview.png` 至
`29_build_mode_exit_grid_hidden.png` 的 28 张当前实现图，并写入 Profile、64 Cell、16 Lot、模块、
道具、树木、三角形、材质、近似 FPS 和 World Schema 指标。抽查总览、五类院落、Grid 开关、
Hover/Selected、住宅/官署 Ghost、旋转、跨 Cell、道路入口、非法红态、Draft 和退出画面，文件均
可读取；Grid 在建设模式显示、退出后隐藏，合法为绿色、非法为红色，Golden Block 中景恢复。

真实 `01_golden_block_v1_before.png` 当前仓库不存在；工具没有拿 V2 画面冒充历史 Before。因此
V1/V2 同镜头比较、Profiler Draw Call、Memory/GC 和用户人工审图仍为 `PENDING`。若后续取得真实
历史图，应只补入约定文件名，不重新解释世界事实或修改当前画面。

当前 Unity 已保持在 `PlayableDemo → C 县域 → Golden Block Mid`，Grid 和 Debug 关闭。自动门禁
已通过，当前状态为
`IMPLEMENTED_AUTOMATION_PASSED_CURRENT_EVIDENCE_READY_HISTORICAL_BASELINE_PENDING_USER_REVIEW`；
只有用户明确接受后才是 `ACCEPTED`。

## 8. 世界不变与当前边界

本轮没有修改人口、永久 Person、Household、正式 Facility/Road/Water/Fortification、库存、生产、
市场、钱粮、所有权、控制权、日期、行政归属或存档结构。Golden Block 的 16 Lot、配房、墙门、
巷道、道具和树木只属于 Presentation。世界 Schema 保持 79。

本轮没有完成全洛阳 2,084 Facility 最终模型、正式施工经济、AI/NPC 建设、室内、碰撞、导航、
战争或考古级复原，也不宣称达到《三国志11》或《城市：天际线》的最终成片质量。

## 9. 下一阶段建议

仅在 Golden Block V2 获得用户人工验收后，启动
《洛阳县域建筑美术规则推广与 Far / Mid / Near 全县接入 V1》：把已确认的 Profile、共享模块、
LOD 和批处理规则推广至全县正式 Facility，并逐类补齐最终美术资产；在通过前不展开全量铺设。
