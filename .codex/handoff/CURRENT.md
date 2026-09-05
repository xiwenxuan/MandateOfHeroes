# 当前项目交接单

## 元数据

- 交接编号：`MOH-HANDOFF-20260905-2227`
- 生成时间：`2026-09-05 22:27:28 +02:00`
- 项目名称：`MandateOfHeroes`
- 项目根目录：`E:/project/MandateOfHeroes`
- 来源聊天框：`三国志`
- 来源聊天框 ID：`01a03d16-463c-7942-8b10-e06e0ecb606e`
- 接管状态：`CLAIMED`
- 接管时间：`2026-09-05 23:14:10 +02:00`
- 新聊天框：`执行新聊天框部分`
- 新聊天框 ID：`01a0735f-f1ba-75e2-9a5c-1fac0bfbaa68`
- 核验结果：项目根目录、分支、HEAD、上游差异、工作区计数、World Schema、Unity 版本、必读路径、V2 证据与当前实现均已实时核对；来源聊天框当前空闲，本聊天框为活动接管方；可按“下一步动作”继续 V3 `Step 0—2`。
- 状态差异：无实质漂移。实时工作区仍为默认 `284` 条、展开 `471` 个文件条目（`61` 个 tracked 修改、`410` 个逐文件 untracked），`Unity.exe` 为 `0`；仅观察到 `6` 个 Unity Hub 进程与 `1` 个 Unity Licensing Client（PID `25024`），均不构成编辑器项目锁。
- 历史档案：[`history/2026-09-05_22-27_MandateOfHeroes_三国志.md`](history/2026-09-05_22-27_MandateOfHeroes_三国志.md)
- 当前任务书归档：[`assets/MOH-HANDOFF-20260905-2227/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md`](assets/MOH-HANDOFF-20260905-2227/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md)

## 当前目标

在洛阳 `Golden Block` 400m×400m 样板街区内，建立可推广到整个洛阳县域的东汉建筑艺术语言和模块化院落表现。当前重点是普通 `Mid` 视距下的建筑轮廓、院落秩序、屋顶、台基、地面结合、尺度和五类 Facility 的可辨识性，使住宅、市场、工坊、仓廪、官署即使关闭标签也能区分，并保持现有批量渲染、LOD 和 50m 建设交互。

## 当前任务

- 任务：`洛阳 Golden Block 建筑艺术语言、模块化院落与 Mid 视距定型 V3`。
- 性质：县域建筑美术正式定型任务；只在 Golden Block 样板区收口，不推广全洛阳。
- 当前阶段：V3 `Step 0—2` 已完成，开工快照、当前 V2 Mid/Near Before 和分类视觉问题清单均已冻结；`Step 3` 尚未开始，本轮未修改 V3 建筑表现代码。
- 当前范围：Presentation 建筑本体、Compound、Roof/Foundation/Ground/Props/Vegetation、稳定变化、尺度校准、Far/Mid/Near、Ghost 与正式 50m Grid 兼容，以及相应测试和证据。
- 明确延期：全县推广、正式 `ConstructionProject`、拆迁、建筑升级、战争、室内、5m/10m 微型 Cell。

## 用户已确认决策

- 地图职责采用 `M 天下 / C 县域 / F 人物`；建设和城市经营聚焦独立加载的县域地图，天下层负责跨县战略移动与宏观信息。
- 洛阳县域使用现有 512km² 权威空间和 50m `PlanningCell`；当前包为 320×640、204,800 个 Cell。未打开县域不加载完整表现，但世界事实仍按日结算。
- Facility 不等于 Cell。普通设施可占一个 Cell 的部分空间；大型设施可以覆盖多个 50m Cell，并由多个表现模块组成一个 Compound，但仍只有一个正式 FacilityId。
- 不增加 5m/10m 微型权威 Cell；50m Cell 继续承担建设选址、Footprint、通行和验证语义。
- 建筑表现分为功能事实与外观模块。模块、院墙、配房、摊位、树木等不得自行成为 Facility，不进入人口、库存、产能或存档权威。
- 玩家要的是偏《三国志11》的彩色立体战略沙盘和可读建筑群，不采用此前水墨淡化方案，也不复制任何商业游戏资产。
- 县域建设交互应借鉴《城市：天际线》和《了不起的修仙模拟器》的易用性：普通浏览隐藏全量格网，进入建设规划后显示 50m Grid、Ghost、Footprint、Entrance、Road Access、旋转和合法性反馈。
- 洛阳建筑、地标与空间数据需要历史依据和置信度记录；表现优化不得篡改历史位置置信度。
- 全国人口采用既有全国县人口分布权威，不再使用为洛阳单独包装的 400,000 人“都市圈”或 700,000 人“外围供给区”口径。
- 随机变化必须由稳定 ID 和确定性种子派生，不得使用运行时随机导致重载换样。
- 当前 V3 必须先过 Golden Block 人工视觉门，未确认前不得推广全县或标记 `ACCEPTED`。

## 用户否决或废弃内容

- 废弃旧的洛阳都市圈 400,000 人包和外围供给区 700,000 人包；不得恢复为人口权威。
- 否决把县域主画面做成抽象线、点、色块或大面积空白底板的呈现。
- 否决此前偏水墨淡化的天下地图风格；目标改为更接近《三国志11》的彩色立体战略沙盘语言。
- 否决“一 Facility 一个无层次方盒 + 简单换色”的建筑表达。
- 否决通过新增 5m/10m Cell 解决建筑细节；大型建筑应通过多 Cell Footprint 与 Cell 内表现模块组合实现。
- 否决在 Golden Block 未通过用户人工验收前直接铺满全洛阳。

## 已完成内容

- 现有总纲已记录 `M 天下 / C 县域 / F 人物`、50m 县域空间、县域 LOD 和 PlayableDemo 路由的阶段性实现；最后一条已写入总纲的里程碑是第 55 节，状态仍为 Unity 重验收待完成。
- [`Docs/REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md`](../../Docs/REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md) 记录 V2 已完成代码、配置、定向 Core、显式编译、Unity Project Load、EditMode、PlayMode 和 28 张 1920×1080 当前证据；其正式状态为 `IMPLEMENTED_AUTOMATION_PASSED_CURRENT_EVIDENCE_READY_HISTORICAL_BASELINE_PENDING_USER_REVIEW`，明确不是 `ACCEPTED`。
- V2 保持 World Schema 79、2,084 项正式 Facility、64 个 Golden Block 正式 Cell、16 个只读表现 Lot；没有新增世界建筑或改动经济事实。
- [`Docs/REPORT_LUOYANG_CITYWIDE_BUILDING_LANGUAGE_AND_PRESENTATION_LOD_ROLLOUT_V1.md`](../../Docs/REPORT_LUOYANG_CITYWIDE_BUILDING_LANGUAGE_AND_PRESENTATION_LOD_ROLLOUT_V1.md) 记录了 1,056 项建筑型 Facility 的五族只读表现计划、158 项 Major Facility 身份保留以及 Far/Mid/Near 分层底座。
- 已有 `CountyBuildingPresentationProfile`、`CountyGoldenBlockPresentation`、`CountyCitywideBuildingPresentation`、县域世界空间/规划控制器、编辑器审阅菜单以及对应 Core/EditMode/PlayMode 测试文件，供 V3 复用和扩展。
- 当前 V3 原始临时任务书已复制到本交接目录的长期附件中。

## 正在进行的内容

- V3 尚未产生实现差异；当前正在进行的是旧聊天框归档与跨聊天接管。
- 工作区包含大量此前阶段未提交的代码、测试、证据、任务书和报告；它们横跨县域空间、地图路由、建设工具、Golden Block、商旅和历史资产，不能在未逐项归属前合并、删除或回滚。
- V2 历史同机位 `01_golden_block_v1_before.png` 不存在；V2 报告保留该事实，未伪造历史 Before。

## 尚未完成内容

- 按 V3 任务书保存真实 V2 Mid/Near Before，并建立当前建筑抽象问题清单。
- 完成五类建筑灰模轮廓审阅；只有关闭标签仍能区分后，才进入屋顶、檐口、墙体、台基、院墙/院门和 Compound 细化。
- 完成 Ground Treatment、道路/入口关系、克制的道具和植被、Stable Variation、Asset Scale Calibration、Presentation Importance、LOD、Batch/Instance/Material 和 Cache。
- 将建设 Ghost 升级为复用 V3 Profile，回归 SingleCell、MultiCell、Footprint、Entrance、Rotation、Road Access、Placement Validation、Draft/Undo、退出 Grid 隐藏与 No World Mutation。
- 执行 V3 定向 Core、Core 全量、全工程编译、Unity Project Load、EditMode、PlayMode、性能测量和至少 32 张真实 Game View 证据。
- 完成 V2/V3 同镜头 Before/After、V3 实施报告和系统总纲更新。
- 等待用户对 V3 Golden Block 明确人工验收；在此之前不得标记 `ACCEPTED` 或推广全洛阳。
- M26 20—30 分钟独立人类盲玩在总纲中仍是独立未关闭门禁，不得被建筑美术任务自动视为完成。

## 项目实时状态

- Git 分支：`codex/m23-p4-quality-artisan-growth`
- 上游：`origin/codex/m23-p4-quality-artisan-growth`
- HEAD：`940c4381da4cbb893c0882fd28e68914397af897`
- 与上游关系：本地领先 2 个提交、落后 0 个提交（`git rev-list --left-right --count @{u}...HEAD` 返回 `0 2`）。
- 工作区是否干净：否。
- 交接前状态：283 个变更条目，其中 61 个已跟踪修改、222 个未跟踪条目、0 个删除、0 个重命名。
- 交接后状态：284 个默认 `git status --porcelain=v1` 变更条目，其中 61 个已跟踪修改、223 个未跟踪条目、0 个删除、0 个重命名。使用 `--untracked-files=all` 展开后为 471 个文件条目，其中 61 个已跟踪修改、410 个未跟踪文件。Git 默认会折叠未跟踪目录；新建的 `.codex/handoff/` 因而显示为一个条目，但目录内实际新增 4 个文件。新聊天框仍须实时复核。
- 已修改文件：包括 Skill 路由/测试说明、洛阳历史资产清单与 FBX、Domain/Simulation/Persistence/Presentation、Shader、测试、总纲、README、工具脚本和 ProjectSettings。
- 未跟踪文件：包括双尺度/50m 县域空间、行政区划、县域规划、地图路由、Golden Block/全城建筑表现、编辑器审阅菜单、测试、任务书、报告和证据目录。
- 用户原有修改：长会话遗留工作区的逐文件作者归属无法可靠重建；除本交接新增项外，一律视为需要原样保留的既有修改，不得擅自覆盖或清理。
- 本次交接新增：`CURRENT.md`、`INDEX.md`、本次历史档案，以及 V3 任务书归档副本，共 4 个未跟踪文件。
- 旧聊天此前新增：项目根目录 `TASK_UNIVERSAL_PROJECT_CHAT_HANDOFF_V1.md`，当前未跟踪。
- 未提交内容：上述脏工作区全部尚未提交；本交接不暂存、不提交。
- 未推送内容：当前分支有 2 个本地提交尚未推送；本交接未尝试网络推送。
- 外部进程：生成交接时没有 `Unity.exe`/Unity Editor 进程，仅有 `Unity.Licensing.Client`；未关闭任何用户程序。
- 接管执行后状态：默认 `git status` 为 `285` 条，`--untracked-files=all` 展开为 `477` 个文件条目；相对开工基线只新增 `Docs/Evidence/LuoyangGoldenBlockBuildingArtLanguageAndCompoundV3/` 下 6 个 Step 0—2 证据文件。正式复采集重写了既有未跟踪 V2 证据目录中的 28 张截图和指标，但没有改变 Git 条目数量。
- 接管执行后的外部进程：正式 V2 审阅实例 `Unity.exe` PID `8464` 仍保持打开且响应正常；这是本聊天框启动的实例。

## 验证状态

- 编译：V3 未执行；本次仅为文档交接，不用历史 V2 编译结果替代 V3。
- 核心测试：V3 未执行；V2 报告中的历史定向 Core 结果仅作为 V2 证据。
- 编辑器测试：V3 未执行；V2 报告记录其历史 EditMode 已通过。
- PlayMode 测试：V3 未执行；V2 报告记录其历史 PlayMode 已通过。
- 人工验收：V3 未开始；V2 报告明确仍待用户最终确认，且历史 V1 Before 缺失。
- V3 `Step 0—2`：已完成。记录见 [`Docs/Evidence/LuoyangGoldenBlockBuildingArtLanguageAndCompoundV3/V3_STEP_0_2_BASELINE_AND_VISUAL_ISSUES.md`](../../Docs/Evidence/LuoyangGoldenBlockBuildingArtLanguageAndCompoundV3/V3_STEP_0_2_BASELINE_AND_VISUAL_ISSUES.md)。
- 当前 V2 正式复采集：通过 `LuoyangGoldenBlockBuildModeV2FinalReviewMenu` 在 `PlayableDemo` 生成 `02—29` 共 28 张 `1920×1080`、28 个唯一 SHA-256 的截图；日志出现 `LUOYANG_GOLDEN_BLOCK_BUILD_MODE_V2_EVIDENCE_READY`。当前 Mid/Near 已分别冻结为 `01_golden_block_v2_before.png` 与 `01b_golden_block_v2_near_before.png`，旧的 2026-09-04 Mid/Near 亦按原 SHA-256 保留。
- 本交接文档：已运行 `verify-project.ps1 -DocumentationOnly`，命令退出码为 1；失败来自交接前已有的 `Guangyangmen.fbx.meta`、`Mingtang.fbx.meta`、`NorthPalaceSouthGate.fbx.meta`、`SouthPalace.fbx.meta` 行尾空格，`git diff --check` 因而失败。本次未越权修改这些历史资产。
- 交接材料专项检查：`CURRENT.md`、`INDEX.md`、历史档案均无行尾空格且以 LF 结尾；必读项目路径全部存在。V3 任务书归档副本与原临时附件 SHA-256 均为 `BB337BB5694C470E1E368172EE794B9660252A15733EF2D6D844D416860C37C1`，字节完全一致；原附件本身没有末尾 LF，因此归档副本也原样保留该特征。
- 接管后文档门禁：重新运行 `verify-project.ps1 -DocumentationOnly`，退出码仍为 `1`，且仍只命中上述 4 个既有 FBX `.meta` 行尾空格。任务范围专项检查通过：`CURRENT.md`、`INDEX.md`、V3 Step 0—2 报告均无行尾空格、使用 LF 且有末尾换行；冻结图片的哈希/尺寸一致，指标 JSON 可解析且与正式采集源文件字节一致。
- 未执行项目及原因：按照交接任务书，旧聊天框完成归档后不得继续新的项目开发或启动 V3 工程验证。

## 当前阻塞

- 没有阻止 V3 后续工作的硬环境阻塞；Step 0 工作区范围快照已完成，仍须保护全部既有修改。
- 文档门禁的全工作区 `git diff --check` 被上述 4 个既有 FBX `.meta` 行尾空格阻断；这是需要后续单独归属和修复的工作区问题，不是交接文档损坏。
- V2 的真实 V1 同机位 Before 不存在；当前 V2 Mid/Near 已作为真实 V3 Before 保存，不得将其描述成 V1 历史图。
- V3 包含分阶段人工视觉门；五类灰模轮廓或普通 Mid 画面未获确认时，不得越级推广全县。
- 既有全县 rollout 消费共享 `CountyBuildingPresentationProfileCatalog.HanLuoyangV2`，而 V3 明确只在 Golden Block 收口；Step 3 必须先隔离 V3 Profile/selector，不能直接修改共享 V2 并让全县同步变化。

## 下一步动作

从 V3 `Step 3` 继续前，先审计 Golden Block 与全县 rollout 的 Profile/selector 调用链，建立仅作用于 Golden Block 的版本化 V3 路径或等价隔离；全县继续消费既有 V2，直至 Golden Block 通过规定的灰模、Mid 与最终人工门。隔离确认后再整理 V3 `BuildingPresentationProfile`，不得提前推广全县或标记 `ACCEPTED`。

## 必读文件

1. [`AGENTS.md`](../../AGENTS.md)
2. [`.codex/skills/mandate-unity-development/SKILL.md`](../skills/mandate-unity-development/SKILL.md)
3. [`Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`](../../Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md)
4. [`Docs/AI_PROJECT_BRIEF.md`](../../Docs/AI_PROJECT_BRIEF.md)
5. [`assets/MOH-HANDOFF-20260905-2227/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md`](assets/MOH-HANDOFF-20260905-2227/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_LANGUAGE_AND_COMPOUND_V3.md)
6. [`Docs/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md`](../../Docs/TASK_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md)
7. [`Docs/REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md`](../../Docs/REPORT_LUOYANG_GOLDEN_BLOCK_BUILDING_ART_AND_50M_BUILD_MODE_V2.md)
8. [`Docs/REPORT_LUOYANG_CITYWIDE_BUILDING_LANGUAGE_AND_PRESENTATION_LOD_ROLLOUT_V1.md`](../../Docs/REPORT_LUOYANG_CITYWIDE_BUILDING_LANGUAGE_AND_PRESENTATION_LOD_ROLLOUT_V1.md)
9. [`Assets/Scripts/Mandate.Presentation/CountyBuildingPresentationProfile.cs`](../../Assets/Scripts/Mandate.Presentation/CountyBuildingPresentationProfile.cs)
10. [`Assets/Scripts/Mandate.Presentation/CountyGoldenBlockPresentation.cs`](../../Assets/Scripts/Mandate.Presentation/CountyGoldenBlockPresentation.cs)
11. [`Assets/Scripts/Mandate.Presentation/CountyCitywideBuildingPresentation.cs`](../../Assets/Scripts/Mandate.Presentation/CountyCitywideBuildingPresentation.cs)
12. [`Assets/Scripts/Mandate.Presentation/LuoyangCountyPlanningPresentationController.cs`](../../Assets/Scripts/Mandate.Presentation/LuoyangCountyPlanningPresentationController.cs)
13. [`Assets/Tests/EditMode/LuoyangGoldenBlockPresentationV1Tests.cs`](../../Assets/Tests/EditMode/LuoyangGoldenBlockPresentationV1Tests.cs)
14. [`Assets/Tests/PlayMode/LuoyangGoldenBlockBuildModeV2PlayModeTests.cs`](../../Assets/Tests/PlayMode/LuoyangGoldenBlockBuildModeV2PlayModeTests.cs)
15. [`history/2026-09-05_22-27_MandateOfHeroes_三国志.md`](history/2026-09-05_22-27_MandateOfHeroes_三国志.md)

## 禁止事项

- 不得删除、清理、覆盖或批量回滚当前 284 个 `git status` 变更条目；先实时核验并做范围归属。
- 不得把聊天摘要、V2 报告或旧测试结果当作 V3 已实现、已通过或已验收。
- 不得创建第二套 Facility、人口、库存、生产、道路、时间、经济或存档权威。
- 不得新增 5m/10m 微型 Cell；不得改变 50m PlanningCell、2,084 Facility、512km² 县域或 World Schema 79，除非用户另行明确授权并完成迁移设计。
- 不得让表现模块、道具或院落子建筑成为正式 Facility 或进入正式库存。
- 不得使用不稳定运行时随机；不得复制《三国志11》《城市：天际线》或其他商业游戏资产。
- 不得在 Golden Block V3 人工视觉门通过前推广全洛阳、启动战争实现或正式 ConstructionProject。
- 不得伪造缺失的历史 Before、测试结果、性能数据或用户验收。
- 未经用户明确要求，不得提交、推送、创建 PR、删除数据、关闭 Unity 或归档旧聊天框。
