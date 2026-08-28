# 洛阳 P0 地标第二批多角度审模与决策对照板 V1 任务书

状态：`LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1`

> 兼容说明（2026-08-27）：本任务及其决策板是用户决定前的历史审模证据。用户随后回复“全部接受”，
> 四项当前批准状态已由
> `TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md` 接管。下文
> `PENDING` 与 `FinalArtApproved=false` 描述的是生成决策板时的状态，保留用于审计。

## 1. 任务目标

承接北宫、永安宫、太学、辟雍四件原生 Prefab/FBX 候选，在不推定用户已经接受模型的前提下，
修正现有太学与辟雍近景被评审地形线框遮挡的问题，并为每件建立前斜、后斜、低角三个固定机位。
最终把十二张 Unity 实机图排成四张可直接逐件判断的无裁剪决策板。

本任务只提高审模证据的完整性和可读性。用户本轮“继续执行”不等于“接受四件”，因此四项
`FinalArtApproved=false`，也不授权第三批或其余 46 个槽位。

## 2. 冻结范围

- 建筑范围仍为北宫、永安宫、太学、辟雍，评审顺序仍为 `1/2/3/5`。
- 不修改四个 Prefab、FBX、Mesh、Material、LOD、锚点、Collider 或来源清单。
- 不移动权威 Facility 或历史 Global Cell；只把四件 PreviewOnly 审模实例放到已经验证的平缓评审板
  Cell `(1240,2040)/(1240,2046)/(1246,2040)/(1246,2046)`。
- 不改变建设权限、产权、控制、人口、库存、模拟、Save Schema 或全城批处理语义。
- 输入图片只允许等比例缩放和排版，禁止裁剪、调色、补画或生成式修改。

## 3. 审模矩阵

| 建筑 | 前斜 | 后斜 | 低角 | 检查重点 |
|---|---|---|---|---|
| 北宫 | 双阙、高台与正面轴线 | 后殿闭合和双阙背影 | 台阶、柱列、重叠檐口 | 宫城制高与双阙识别 |
| 永安宫 | 偏轴主院、园池和前厅 | 后院、园木与院落闭合 | 水池、侧厅和屋檐高差 | 园院身份与非对称性 |
| 太学 | 列堂、讲席和中庭 | 后排讲堂及院落深度 | 前阶、列堂屋檐与讲席 | 学宫列堂而非宫殿 |
| 辟雍 | 环水礼堂和前桥 | 后桥、环水与礼堂轮廓 | 水环、台基和中心礼堂 | 环水礼制身份 |

## 4. 运行时与证据要求

1. 新增第二批独立多角度合同，固定 `4 × 3 = 12` 个互异相机 ID。
2. 前后斜机位使用相同审模 Cell 且相差 180 度；低角俯角保持 28—34 度。
3. 第二批审模状态显示总览、上一/下一建筑、上一/下一角度及当前索引。
4. 所有近景校验建筑包围盒中心、安全画幅和中心视线无遮挡。
5. 输出 1 张总览和 12 张 1600×1000 Unity Game View。
6. 每件三张近景生成一张 3000×900 决策板，显示 `DECISION: PENDING` 与
   `FINAL ART APPROVAL: FALSE`。
7. 无时间戳机器清单记录 12 个输入、4 个输出的相对路径、尺寸和 SHA-256；重复生成哈希一致。

## 5. 验收门禁

- 全工程编译与第二批定向核心合同通过。
- 多角度相机 EditMode、13 图图形 PlayMode、既有第二批五视图和最密 549 Facility 批处理回归通过。
- 四张决策板逐张视觉检查，输入/输出清单与重复生成确定性通过。
- `git diff --check`、手写文件尾随空白、JSON 解析与范围审阅通过。

## 6. 当前边界与下一步

本任务完成后状态只能进入：

`LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_DECISION_BOARDS_READY_FOR_USER_DECISION_V1`

用户仍需分别对北宫、永安宫、太学、辟雍给出“接受 / 修改 / 否决”。只有明确接受项才允许另开最终
批准登记任务；不得由本任务自动改为 `FinalArtApproved=true`。

## 7. 执行记录

- 已新增 `presentation.luoyang.p0-landmark-second-batch.multi-angle-review.v1`，冻结 4 件 × 3 角度、
  12 个互异相机 ID；运行时已支持第二批总览、上一/下一建筑和上一/下一角度循环。
- 四件 PreviewOnly 审模实例已移动到既有平缓评审板 Cell
  `(1240,2040)/(1240,2046)/(1246,2040)/(1246,2046)`；没有移动权威 Facility 或 Global Cell。
- 已生成一张总览和十二张 1600×1000 Unity Game View。自动检查覆盖实际 Prefab、无程序回退、
  画幅中心、安全边界、中心视线无遮挡和退出清理；人工逐板检查确认主体不再被地形线遮挡。
- 已新增确定性决策板脚本，把 12 张原图等比例排成四张 3000×900 决策板；无时间戳清单记录
  输入/输出路径、尺寸与 SHA-256。连续生成两遍后 4 张 PNG 与 JSON 共 5 个哈希逐项一致；最终
  清单 SHA-256 为 `8e56e063b483f09c80869f6b473c85b0791dfd9d924a5294578d18a4cb3518a7`。
- 全工程编译和定向核心 1/1 通过；项目加载冒烟、目标 EditMode 2/2、13 图图形 PlayMode 1/1、
  既有第二批五视图图形 PlayMode 1/1、最密 549 Facility 批处理图形 PlayMode 1/1 通过。
- 主要结果分别为
  `tmp/unity-validation/unity-EditMode-20260827-170659-969.summary.json`、
  `tmp/unity-validation/unity-PlayMode-20260827-170759-856.summary.json`、
  `tmp/unity-validation/unity-PlayMode-20260827-171359-612.summary.json` 与
  `tmp/unity-validation/unity-PlayMode-20260827-171548-857.summary.json`。
- 第二批原来源清单 SHA-256 仍为
  `3adea5941eea4bda596040a13eb10f42215807a844655db7a0fbaec73fbd5eba`；模型、Prefab、FBX、
  材质、LOD、锚点、Collider、建设规则、模拟与存档均未改变。
- PowerShell 解析、13 图/4 板尺寸、机器清单路径和哈希回验、两份 JSON 解析、`git diff --check`、
  本任务手写文件尾随空白和范围审阅通过；工作区既有两条换行格式提示不属于本任务失败。
- 四项决策仍为 `PENDING`、`FinalArtApproved=false`。下一门禁是用户逐件“接受 / 修改 / 否决”，
  不自动启动第三批或修改其余 46 个槽位。
- 后续兼容更新（2026-08-27）：用户已对四项回复“全部接受”，最终激活任务已登记决定并在真实
  Prefab/FBX门禁通过后激活四项`FinalArtApproved=true`。本任务的PNG和无时间戳JSON继续保留为
  接受前输入证据，不回写其历史`PENDING/false`标记；第三批仍未授权。
