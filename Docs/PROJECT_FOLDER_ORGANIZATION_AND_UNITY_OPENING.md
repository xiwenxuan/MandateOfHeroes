# 项目文件夹整理与 Unity 打开指南

状态：`WORKSPACE_ORGANIZATION_V1_APPLIED`

本指南规定仓库根目录的长期用途、Unity 工程入口、可再生缓存和后续整理边界。
目标是让 Unity、Git、测试工具、地图数据管线和美术交付互不混淆，同时避免通过
资源管理器批量移动 Unity 资产而破坏 `.meta` GUID 和序列化引用。

## 1. 唯一 Unity 工程入口

Unity Hub 或 Unity Editor 只打开同时包含以下三个目录的仓库根目录：

```text
MandateOfHeroes/
├─ Assets/
├─ Packages/
└─ ProjectSettings/
```

工程锁定版本为 `2022.3.62f3c1`。本机匹配编辑器的默认位置为：

```text
C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe
```

不要用 Unity 6000.x 打开并升级本工程。版本升级必须作为独立任务处理，包含包兼容、
序列化、场景、Prefab、编译、EditMode 和 PlayMode 验收。

推荐入口：

```powershell
# 只读检查工程根、版本、包 JSON、Build Settings、.meta/GUID 和测试遗留场景
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\Inspect-UnityProjectWorkspace.ps1

# 检查通过后，用 ProjectVersion.txt 指定的已安装编辑器打开项目
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\Open-UnityProject.ps1
```

也可以在 Unity Hub 中执行 `Add/Open`，选择仓库根目录；Hub 显示的 Editor Version 应为
`2022.3.62f3c1`。

## 2. 目录职责

### Unity 会导入并参与序列化的目录

| 目录 | 职责 | 整理规则 |
| --- | --- | --- |
| `Assets/` | 游戏代码、场景、Prefab、材质、FBX、Resources、测试 | 资源移动必须连同 `.meta`，优先在 Unity 内完成；禁止资源管理器批量重排 |
| `Packages/` | Unity 包清单和锁文件 | `manifest.json` 与 `packages-lock.json` 一起维护 |
| `ProjectSettings/` | 编辑器、渲染、输入、Build Settings 等项目设置 | 作为序列化合同审阅，禁止用不同大版本静默升级 |

`Assets/` 当前一级目录已经按职责分为 `ArtSource`、`Editor`、`Resources`、`Scenes`、
`Scripts`、`Shaders`、`StreamingAssets` 和 `Tests`，本轮不做高风险重排。

### Unity 不导入的长期源文件目录

| 目录 | 职责 |
| --- | --- |
| `Docs/` | 设计、任务书、验收、历史资料和截图证据 |
| `Data/` | 非 Unity 运行时的权威或交换数据 |
| `MapData/` | 地图源数据、派生数据与审核数据；大型工作文件按 `.gitignore` 规则排除 |
| `MapPipeline/` | 地图处理管线与依赖说明 |
| `Tools/` | 编译、测试、验证、工程检查和 Unity 打开工具 |
| `scripts/` | 早期地图文档/表格生成器；后续迁移到 `Tools/Generators/` 需单独验证引用 |
| `.codex/` | 项目开发 Skill、验证入口及其参考资料 |
| `deliverables/` | 可交付的表格、文档、图片和归档交接包 |

把大体积 GIS、报告截图和交付文档放在 `Assets/` 外，是当前正确做法：Unity 打开项目时
不会导入这些文件。

### 本地可再生目录

以下目录由 `.gitignore` 排除，不应提交：

| 目录 | 当前审计体积 | 处理策略 |
| --- | ---: | --- |
| `Library/` | 约 316.59 MiB | 正常保留，可显著加快下次打开；仅在缓存损坏且 Unity 已关闭时重建 |
| `tmp/` | 约 376.26 MiB | 保存当前编译、核心测试和 Unity 验收证据；任务提交/归档后再按批次清理 |
| `Builds/` | 约 213.95 MiB | 本地构建产物；确认交付已归档后可单独清理 |
| `Logs/`、`obj/`、`UserSettings/` | 小型本地生成内容 | 保持忽略；故障诊断结束后可重建 |
| `.vscode/`、`*.csproj`、`*.sln` | IDE/Unity 生成入口 | 可保留以便开发，丢失后可由 Unity 重新生成 |

不要为了“目录看起来干净”日常删除 `Library/`；删除后第一次打开会重新导入约 231 MiB
的 `Assets/`，耗时反而更长。

## 3. 本轮已执行的安全整理

1. 新增只读工程预检 `Tools/Inspect-UnityProjectWorkspace.ps1`。
2. 新增锁定版本打开入口 `Tools/Open-UnityProject.ps1`。
3. 新增可恢复的测试场景隔离工具 `Tools/Quarantine-UnityGeneratedTestScenes.ps1`。
4. 将四个中断测试留下的 `Assets/InitTestScene*.unity(.meta)` 移到
   `tmp/workspace-quarantine/unity-generated-test-scenes/<timestamp>/`；文件未删除，并保存 SHA-256 清单。
5. `.gitignore` 明确排除 Unity Test Framework 的临时 bootstrap 场景。
6. 将根目录旧交接包归档到 `deliverables/PROJECT_HANDOFF_2026-08-24/`；原始清单保留为
   2026-08-24 的历史快照，不把其中旧机器路径或旧 Git 状态当作当前事实。

本轮不移动任何已跟踪 Unity 资产，不更改任何 `.meta` GUID，不删除 `Library`、`tmp`、
`Builds`，也不提交或推送现有业务开发改动。

## 4. 后续整理方案

### 阶段 A：工程入口与安全清洁（本轮）

- 固定唯一 Unity 根目录和编辑器版本。
- 检查包文件、Build Settings、`.meta` 完整性和重复 GUID。
- 隔离测试遗留场景，归档旧交接资料。
- 保留 `Library` 以优化日常启动。

### 阶段 B：开发证据和构建产物归档（当前功能改动提交后）

- 按任务保留最终 `tmp/skill-verification`、`tmp/unity-validation` 汇总和必要日志。
- 将需要长期交付的报告/截图转入对应 `Docs/HISTORICAL_WORLD_REFERENCE/...`。
- 确认构建包已有发布或校验记录后，按具体批次清理 `Builds/` 和过期 `tmp/`；不得整目录盲删。

### 阶段 C：Unity 资产内部重构（单独任务）

- 先提交或暂存当前洛阳功能改动，建立可回退基线。
- 生成资源路径/GUID/引用清单。
- 只在 Unity Editor 内按小批次移动 `Scenes`、`Prefabs`、`Materials`、`Meshes` 等资产。
- 每批执行全工程编译、目标 EditMode/PlayMode、场景/Prefab 引用验证和 `git diff --check`。
- 不把 `Docs`、原始 GIS、构建包或临时测试证据移入 `Assets/`。

## 5. 日常开发流程

```text
拉取/切换分支
→ git status 确认未提交改动
→ Inspect-UnityProjectWorkspace.ps1
→ Open-UnityProject.ps1 或 Unity Hub 打开仓库根
→ 在 Assets 内通过 Unity 管理序列化资产
→ 使用项目安全测试入口验收
→ 只暂存当前任务文件
```

若预检只报告 `InitTestScene` 警告，先运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\Quarantine-UnityGeneratedTestScenes.ps1
```

隔离是移动到仓库内已忽略的 `tmp`，不是删除；需要调查时可按清单恢复原文件及 `.meta`。
