# 正式 Git Baseline 提交范围整理与审计

- 审计日期：2026-08-12
- 分支：`codex/m23-p4-quality-artisan-growth`
- 起始状态：71 个 modified、5,436 个 untracked
- 目标：从 Git 仓库完整恢复可编译、可验证、依赖完整的正式项目状态
- 禁止方式：本轮未使用 `git add .` 或 `git add -A`

## 1. 分类结论

完善忽略规则前，5,436 个未跟踪文件分类如下：

| 分类 | 数量 | 处理 |
|---|---:|---|
| 正式 Reference / Data / Knowledge Base 候选 | 1,711 | 纳入；扣除可再生成的 V72 原始证据 |
| 正式 Runtime / Unity 候选 | 711 | 纳入 |
| 正式 Tooling 候选 | 3 | 纳入 |
| `outputs/` 与非正式生成输出候选 | 2,944 | 仅纳入 3 个被正式引用的 `deliverables/` 资产 |
| 缓存、预览、检查 sidecar | 67 | 排除 |
| 无法判断 | 0 | 无 |

新忽略规则生效后，可见未跟踪候选降为 2,405 个：Reference/Data 1,691、Runtime/Unity 711、Tooling 3。新规则从原始未跟踪集合排除 3,031 个文件。所有排除文件仍保留在本地。

此外，历史提交中已有 114 个 `outputs/` 文件。本次仅用 `git rm --cached` 从索引移除，未删除本地文件。另有 121 个工作簿预览文件从暂存区排除。

## 2. 正式纳入文件与目录

### Commit A：历史 Reference、全国数据与 Knowledge Base

- `Docs/`：核心设计、正式任务书、验收报告、Historical World Reference、Knowledge Base、Registry 和 GPT 交接资料。
- `Data/`：历史人口、历史人物、Clan、Scenario 等正式源数据。
- `MapData/`：可复原地图和洛阳正式数据源；工作目录与原始候选缓存除外。
- `MapPipeline/`：正式构建、验证、数据生成脚本及配置；Python/Node 缓存除外。
- 3 个明确的正式交付资产：地图坐标表、地图美术规范、洛阳历史地图。
- 根目录正式 README 与地图说明。
- `.gitattributes`：与正式数据同时建立字节精确检出合同，避免中间提交破坏清单哈希。

该组包含：

- 135—260 Historical World Reference；
- 1,182 县生产、资源、产业、贸易与供应母版；
- 72 地点 Full Development Reference Pack；
- 全国人口、历史人物、Clan/Family、Scenario、行政治所与 Canonical Place 资料；
- 洛阳 184 人口、家庭、人物、家族、Facility、工作、生产、消费与历史事件资料；
- V71/V72 正式任务书、报告、模型清单和聚合实验结论；
- Knowledge Base 的 Domain Map、Document/Decision/Open/Gap/Conflict/Research Registry。

### Commit B：累计 Runtime 与 Unity 正式资产

- `Assets/Resources/`：正式内容定义、美术与运行时配置。
- `Assets/Scenes/`：正式验证和游戏场景。
- `Assets/Scripts/`：Domain、Simulation、Persistence、Presentation 累计正式代码。
- `Assets/StreamingAssets/`：运行时真实依赖的地图、人口、人物和洛阳数据包。
- `Assets/Tests/`：正式 EditMode/PlayMode 测试与程序集配置。
- `ProjectSettings/`：Unity 项目、构建场景和服务配置；不包含用户级设置。

累计代码共同修改 `WorldState`、`WorldSnapshotMigrator` 和多个领域类型。V71/V72 不能脱离此前洛阳、市场、医疗、后勤、家庭和人口代码单独编译，因此累计 Runtime 作为一个依赖完整的代码提交，不人为拆出会破坏中间编译的子提交。

### Commit C：Tooling、规则与审计

- `.gitignore`。
- `.codex/skills/mandate-unity-development/` 正式 Skill、路由与验证脚本。
- `Tools/`：核心测试器、安全 Unity 测试器、AI 离线训练等正式工具。
- `scripts/`：地图坐标表和地图美术规范生成工具。
- 本审计文档。
- 将历史 `outputs/` 从版本索引移除的记录。

## 3. 明确排除目录与文件

- `tmp/`、`outputs/`、`cache/`、Unity `Library/Temp/Logs/obj/bin`。
- IDE、用户环境、`.env`、证书、密钥文件。
- `MapPipeline/.python/`、`.cache/`、`sources/cache/`、所有 `__pycache__/` 与 `*.pyc`。
- `artifact_tool_sidecars/`、`*.inspect.ndjson`、`previews/`、`workbook_previews/`、`renders/`。
- `MapData/**/working/` 与 `MapData/**/candidates/raw/`。
- 本地 checkpoint、gzip 派生检查点、截图、工作簿预览、debug dump、沙盒产物。
- V72 可重新生成的大型原始证据：`ARENA/*.jsonl`、`arena_metrics.csv`、`MODEL/training_dataset.jsonl`。

V72 聚合结果、模型权重、Feature Schema、Training Config、Manifest、Evaluation、正式工作簿和报告继续纳入。排除原始逐行证据不影响 Runtime，且可由正式测试和训练脚本重建。

## 4. 可重新生成文件与无法判断文件

可重新生成内容包括：

- `outputs/` 下的构建中间结果、运行证据、截图与检查 sidecar；
- Arena 逐决策/逐事件 JSONL、检查点 CSV、神经网络训练逐行数据集；
- MapPipeline 工作目录、候选原始缓存、Python 字节码和工具缓存；
- Unity、IDE 和构建系统生成目录。

这些文件未因排除而删除。顶层分类没有无法判断项；`deliverables/` 也没有整体忽略，未来新增资产仍须逐文件审计。

## 5. V71 与 V72 依赖

V71 依赖：

- `HistoricalEventState`、`LivingWorldRuntimeState`、`WorldState`；
- `LivingWorldDecisionSystem`、`HistoricalEventRuntime/System`、`WorldSimulator`；
- `WorldSnapshotMigrator` V70→V71 迁移；
- 条件式重大历史事件、稳定 Seed、LOD 调度、Simulation Arena 测试；
- 洛阳 V70 生活经济闭环及正式人口、Facility、市场、运输与组织账数据。

V72 依赖：

- 全部 V71 依赖；
- `LivingWorldDecisionPolicyV2`、`WorldSimulationArena`、`NeuralPolicyModelReader`；
- V71→V72 迁移、Policy Profile、Goal、Model ID 与有限 DecisionMemory；
- 离线 MLP 训练脚本、版本化模型/Manifest/Feature Schema/Config/Evaluation；
- Policy、Seed、Event、Arena、Save 和守恒测试，以及 V72 任务书、总报告和 Knowledge Base 登记。

## 6. 大文件与重复数据审计

正式候选中没有大于 100 MB 的单文件。主要大文件：

- 66.44 MB：1,182 县经济母版 JSON，正式 Reference，纳入。
- 33.49 MB：1,182 县逐县 Pack NDJSON，正式 Reference，纳入。
- 34.33 MB：洛阳 50 万人压力档人物二进制源数据，正式测试/构建输入，纳入。
- 20.60 MB：洛阳都市初始化运行时人物数据，Runtime 依赖，纳入。
- 81.75 MB：V72 逐决策 Trace，可重新生成且 Runtime 不读取，排除。

MapData 源数据与 StreamingAssets 运行包是有意的“构建源→运行时包”双层合同，职责不同，不按临时重复副本处理。

## 7. 凭据、本机路径与数据字节合同

- 未发现 GitHub Token、OpenAI Key、私钥、证书私钥或用户密码。
- `ProjectSettings.asset` 的 `metroCertificatePassword` 是空字段，不是凭据。
- 8 个 Artifact Tool 脚本的 Codex 缓存绝对路径已改为 `MANDATE_ARTIFACT_TOOL_ENTRY` 或 `@oai/artifact-tool` 包解析。
- README 与 AI 交接文档的项目绝对路径已改为 `<repo-root>`。
- 两个历史输入来源的桌面绝对路径已改为 `external-input/<filename>`。
- `.env`、证书、密钥及用户级 IDE 配置继续由 `.gitignore` 阻止提交。
- 干净索引验证发现部分 JSON 数据包清单记录的是 CRLF 原始字节，而旧 `.gitattributes` 会在新检出中转换为 LF，破坏字节数和 SHA-256。已将 `Assets/StreamingAssets/`、`MapData/`、`Data/` 定义为字节精确数据根并显式重规范化索引。
- 重导出的干净副本中，历史人物包、洛阳城市包、都会圈包及跨包依赖均为 0 个哈希失配。

## 8. `.gitignore` 补充

新增规则覆盖：`outputs/`、通用 Python/Node 缓存、Artifact Tool sidecar、工作簿预览/渲染，以及 V72 可再生成的 Arena/训练逐行数据。没有因文件量大而忽略 `Docs/HISTORICAL_WORLD_REFERENCE/`、`Docs/KNOWLEDGE_BASE/`、`Data/`、`MapData/`、`Assets/StreamingAssets/` 或正式 `deliverables/`。

## 9. 仅含拟提交文件的验证

验证副本：从 Git 索引导出到短路径，并只补充本机生成的 `.sln`/`.csproj` 与现有 Unity `Library` 连接；不含 `outputs/` 和工作簿预览目录。

| 验证项 | 结果 |
|---|---|
| `git diff --cached --check` | 通过 |
| 全工程编译 | 通过 |
| 核心回归 | 657/657 通过；第 6 组因 300 秒单次预算拆为 43+12 两段，证据刷新测试使用显式环境开关 |
| Unity EditMode 全量单进程 | 已真实启动并进入测试；300 秒硬超时后安全终止，无残留进程，未声称全量通过 |
| V71/V72 定向 Unity EditMode | 17/17 通过 |
| Unity PlayMode 全量 | 9/9 通过 |
| 洛阳 PlayMode Smoke | 通过 |
| 全国人口深度验证 | 通过：126 年、13 州、105 区域、1,182 县、148,932 县年记录 |
| 1,182 县经济母版 | 主验证 32/32、交付验证 13/13 通过 |
| 洛阳历史原型 | 通过：20,542 人、4,498 户、1,230 设施 |
| 洛阳世界多档 | 通过：low/recommended/high 三档 |
| 洛阳人口压力 | 通过：20,542 至 500,000 人五档 |
| 洛阳都会圈 | 通过：400,000 人、80,899 户、2,084 设施合同 |
| 包字节完整性 | 历史人物、城市、都会圈及跨包依赖全部通过 |

已识别但不通过扩大提交范围掩盖的工具债：

- 地图总管线当前 Python 环境缺少 `geopandas`；没有在本轮擅自安装依赖。
- 7 类历史 Reference/Knowledge Base 校验器仍把已排除的 `outputs/` 工作数据当成输入，无法在纯仓库副本独立运行。
- 洛阳城市初始化 Python 校验器还要求 `outputs/` 工作簿；对应正式运行时合同已经由核心回归、Unity PlayMode、洛阳 Smoke 和跨包哈希验证覆盖。

这些是验证工具可移植性债，不构成把临时 `outputs/` 纳入 Baseline 的理由；后续应让校验器读取正式 Reference/Data，或在临时目录内自行生成所需工作数据。

## 10. 提交门禁与拆分

所有暂存使用显式目录或文件路径。Commit A 只增加 Reference/Data，不改变编译代码；Commit B 加入全部累计 Runtime 依赖并以本审计记录的干净索引结果验收；Commit C 加入验证工具、规则、审计，并移除历史 `outputs/` 索引项。拆分过程中任何代码提交都不得处于无法编译状态。
