# 洛阳 P0 四件套审模决策对照板 V1 任务书

> 后续状态：用户已接受四件套，四个真实 FBX 已完成 Unity 回读验证并由 `TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 激活 `FinalArtApproved=true`。

## 1. 任务目标

承接 `TASK_LUOYANG_P0_FOUR_PIECE_MULTI_ANGLE_TURNTABLE_REVIEW_PACK_V1.md`，把南宫、明堂、广阳门、北宫南门各自的前斜、后斜、低角三张 Unity 实机图排为一张可直接横向比较的决策板，并建立机器可核验的来源哈希清单与逐件决策单。

本任务只解决“十二张散图不便逐件判断”的审阅问题，不修改四件套模型，也不推定用户已经批准最终美术。

## 2. 固定范围

- 输入只允许使用多角度转台审查包 V1 的十二张 1600×1000 近景 Game View。
- 每座建筑生成一张 3000×900 决策板，按前斜、后斜、低角从左到右排列。
- 对输入图片只做等比例缩放和排版；禁止裁剪、调色、补画、生成式修改或改变镜头内容。
- 决策板必须显示检查重点、`DECISION: PENDING` 和 `FINAL ART APPROVAL: FALSE`。
- 生成清单必须记录输入、输出相对路径、尺寸与 SHA-256，且不得含易变时间戳。
- 不修改 Prefab、Mesh、Material、LOD、锚点、Collider、Facility、Global Cell、权限、Simulation 或 Save。
- 不建立最终 DCC/FBX 源，不开始其余 50 个最终资产槽位。

## 3. 决策板矩阵

| 建筑 | 重点判断 |
|---|---|
| 南宫 | 双朝院轴线、后侧闭合、屋檐/柱列/台阶层次 |
| 明堂 | 三重台、后侧体量、礼殿制高点和四向阶道 |
| 广阳门 | 贯通门道、短瓮城/角楼、墙体与门楼高差 |
| 北宫南门 | 中央门楼、双阙、门洞/屋脊/旗杆层次 |

## 4. 交付物

1. 可重复执行的 PowerShell 生成脚本。
2. 四张 3000×900 PNG 决策板。
3. 一份无时间戳 JSON 机器清单，记录全部输入和输出 SHA-256。
4. 一份中文审阅索引和逐件“接受 / 修改 / 否决”填写模板。
5. 多角度任务书、系统总纲、地图美术资源计划和任务路由的承接说明。

## 5. 自动验收

1. 恰好识别 4 座建筑、每座 3 张输入，共 12 个互异源文件。
2. 所有源图片尺寸均为 1600×1000，生成前后 SHA-256 不变。
3. 恰好输出 4 张 3000×900 PNG，文件非空且可重新解码。
4. 同一输入重复生成时，输出 PNG 与 JSON 哈希保持一致。
5. 清单中所有源路径、输出路径、尺寸和 SHA-256 与磁盘事实一致。
6. 文档校验、`git diff --check`、尾随空白检查和范围审阅通过。

## 6. 状态门禁

自动验收完成后，状态只能进入：

`P0_FOUR_PIECE_REVIEW_DECISION_BOARDS_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`

用户仍需对南宫、明堂、广阳门、北宫南门逐项给出“接受 / 修改 / 否决”。只有四项均明确接受，才允许另开最终批准与独立 DCC/FBX 源归档任务。

## 7. 实施结果

当前状态为：

`P0_FOUR_PIECE_REVIEW_DECISION_BOARDS_V1_READY_FOR_USER_DECISION_FINAL_APPROVAL_PENDING`

- 已建立 `MapPipeline/scripts/build_luoyang_p0_review_decision_boards_v1.ps1`，从冻结的十二张多角度 Game View 生成四张决策板。
- 每张决策板为 3000×900，三张源图按 960×600 等比例缩放横排；源图与目标框比例均为 16:10，未发生裁剪或比例变形。
- 南宫、明堂、广阳门、北宫南门分别显示建筑特定检查重点，以及待定决策和最终批准为否的明确门禁。
- 机器清单固定合同 `presentation.luoyang.p0-four-piece.review-decision-board.v1`，记录 12 个互异输入和 4 个输出的路径、尺寸、SHA-256。
- 连续执行两次后四张 PNG 与 JSON 共五个文件哈希完全一致。
- 四张决策板已逐张视觉检查，标题、三视角、检查重点和决策栏均完整可读。

## 8. 验收记录

| 门禁 | 结果 |
|---|---|
| 首次生成 | `pieces=4 sources=12 boards=4` 通过 |
| 输入/输出清单核验 | 4 件、12 源、4 板、批准为 false 通过 |
| 重复生成确定性 | 5/5 文件哈希一致 |
| 四张决策板视觉检查 | 通过 |
| 文档模式统一验证 | 通过 |
| `git diff --check`、尾随空白与范围审阅 | 通过 |
| Unity/C# 编译 | 未运行；本任务未修改 Unity/C# 或序列化资产 |
| 核心测试 | 未运行；本任务未修改领域、模拟或存档 |
| Unity 测试 | 未运行；复用上一任务已验证的十二张 Game View，不修改 Unity 内容 |

本任务的验证边界是决策辅助制品和生成工具，不重新宣称 Unity/C#、核心或 Unity 测试通过；十二张输入 Game View 的 Unity 运行时证据继续由上一任务书及其 XML/JSON 汇总负责。

## 9. 证据与复现

- 执行脚本：`powershell -NoProfile -ExecutionPolicy Bypass -File MapPipeline/scripts/build_luoyang_p0_review_decision_boards_v1.ps1`。
- 审阅索引：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1/README.md`。
- 决策板：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1/Boards/`。
- 机器清单：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_REVIEW_DECISION_BOARD_V1/Machine/luoyang_p0_four_piece_review_decision_board_manifest_v1.json`。

## 10. 下一步

用户已于 2026-08-27 对整套决策板回复“接受”，按本任务上下文登记为南宫、明堂、广阳门、北宫南门四件全部接受。后续 `TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md` 已登记决定并归档 Unity 原生源；独立 DCC/FBX 尚未到位，因此 `FinalArtApproved` 继续为 `false`。
