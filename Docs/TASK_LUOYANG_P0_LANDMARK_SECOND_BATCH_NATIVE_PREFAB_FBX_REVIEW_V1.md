# 洛阳 P0 地标第二批原生 Prefab、FBX 与审模候选 V1 任务书

状态：`LUOYANG_P0_LANDMARK_SECOND_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

> 兼容说明（2026-08-27）：本任务记录的是用户接受前的源就绪阶段。用户随后对北宫、永安宫、
> 太学、辟雍回复“全部接受”，当前状态与批准合同由
> `TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md` 接管；下文的
> `FinalArtApproved=false` 与旧清单哈希仅是当时证据，不代表当前状态。

## 1. 任务目标

在首批南宫、明堂、广阳门、北宫南门已经完成用户接受和 FBX 最终激活后，按全城最终资产清单的
`ReviewOrder` 选择最低序的四个剩余 P0 身份槽位：北宫、永安宫、太学、辟雍。为其建立项目原创的
三级 LOD Unity 原生 Prefab、真实 FBX 源、稳定锚点、运行时热替换、程序回退和独立审图证据。

本轮只把第二批推进到“源完整、可运行、可审模候选”。用户尚未审阅这四件，因此
`FinalArtApproved=false`；不得沿用首批接受决定，也不得自动批准其余 46 个槽位。

## 2. 冻结选择

| 顺序 | 建筑 | Facility ID | 替换槽位 | 权威 Cell |
|---:|---|---|---|---:|
| 1 | 北宫 | `facility.instance.luoyang.184.north_palace` | `HAN_LANDMARK_NORTH_PALACE_TWIN_TOWER_A` | 4,098,147 |
| 2 | 永安宫 | `facility.instance.luoyang.184.yongan_palace` | `HAN_LANDMARK_YONGAN_PALACE_GARDEN_COURT_A` | 4,101,458 |
| 3 | 太学 | `facility.instance.luoyang.184.taixue` | `HAN_LANDMARK_TAIXUE_LECTURE_ROWS_A` | 4,154,491 |
| 5 | 辟雍 | `facility.instance.luoyang.184.biyong` | `HAN_LANDMARK_BIYONG_RING_WATER_A` | 4,161,116 |

顺序 0、4 已由首批南宫、明堂占用；顺序 10、22 已由首批广阳门、北宫南门占用。本批是“最低序
剩余 P0”有限选择，不改变全城 54 槽位清单。

## 3. 冻结边界

- 保持 Facility、Model、Asset Variant、Profile、Global Cell、史料置信度、来源和权限不变。
- 继续使用项目冻结的 Unity FBX Exporter 4.2.1；新增资产全部为项目原创。
- 每件必须具有恰好三个非空 LOD、完整材质、放置/入口锚点和零 Collider。
- 运行时优先加载第二批真实 Prefab；缺失时回退原地标程序轮廓，并保持最终批准为假。
- 全城远景批处理继续使用已验证的地标 LOD2 数据，不把 FBX 节点变成世界事实。
- 不升级 Save Schema，不改变建设权限、人口、岗位、库存、产权、控制、模拟或 Facility 位置。
- 不宣称考古单体复原、手绘/PBR 贴图终稿、室内、导航、碰撞、损毁或最终美术批准。

## 4. 实施内容

1. 新增第二批机器目录与严格身份、顺序、来源、Prefab/FBX 路径和批准状态合同。
2. 制作北宫双阙高台、永安宫偏轴园院、太学列堂院、辟雍环水礼堂四套原创三级 LOD Prefab。
3. 导出四个真实 FBX，并用 Unity `ModelImporter` 回读验证层级、材质、锚点、包围盒和 Collider。
4. 接入运行时工厂、第二批审图板和五个固定镜头；真实 Prefab 加载成功但最终批准保持假。
5. 生成候选源 SHA-256 清单，更新总纲、资产计划、许可登记和任务路由。

## 5. 验收门禁

1. 目录恰好选择 ReviewOrder 1、2、3、5，且与地标和全城最终资产清单逐项相等。
2. 四个 Prefab 和四个 FBX 均存在；每项三级 LOD 非空、材质完整、锚点齐全且无 Collider。
3. 运行时四项均加载真实 Prefab、未启用回退、`FinalArtApproved=false`。
4. 第二批审图板只含四项，五个固定镜头和截图可复核，退出后对象清理。
5. 全工程编译、定向核心、目标 EditMode、图形 PlayMode、受影响合批回归、`git diff --check`
   和范围审阅分别记录。

## 6. 执行记录

- 已建立严格目录、Domain/Persistence合同和最低剩余P0顺序校验；四项身份、Cell、Model、Asset
  Variant及史料来源与既有地标目录和54槽位最终资产清单逐项相等。
- 已生成北宫、永安宫、太学、辟雍四个项目原创Unity原生Prefab、四个真实FBX、2个批次专用材质、
  3个批次专用Mesh；每件均为三个非空LOD、稳定放置/入口锚点、完整材质且无Collider。
- 已接入运行时Resources优先加载、程序化回退、第二批独立审图板、总览及四个近景相机；四件真实
  Prefab均成功加载，回退未激活，退出世界视图后对象与预览状态清理。
- 已输出5张1600×1000 Unity Game View；人工检查确认四类身份可辨。后续多角度决策板任务已将
  PreviewOnly审模实例移到平缓评审Cell并重新生成五图，太学与辟雍主体的地形线遮挡项已经关闭。
- 候选源清单覆盖54个源/依赖及`.meta`文件、2个工具链锁定文件和4个FBX，逐项保存长度与SHA-256；
  清单自身SHA-256为`3adea5941eea4bda596040a13eb10f42215807a844655db7a0fbaec73fbd5eba`。
- 全工程编译、定向核心1/1、模型/FBX EditMode 3/3、来源清单EditMode 1/1、第二批图形PlayMode
  1/1、最密549 Facility批处理图形回归1/1、首批最终资产图形回归1/1均通过。
- 本轮没有改Save Schema、Facility位置、建设权限、模拟或其余46槽位；没有把候选模型写成考古
  复原或最终美术。
- `git diff --check`、本任务手写文件行尾空白检查、两份JSON解析和范围审阅通过；只出现工作区既有
  两条换行格式提示。本轮执行的是定向核心与目标/受影响Unity测试，不宣称全量核心或全量Unity通过。
- 后续兼容更新（2026-08-27）：用户已全部接受，来源清单按最终激活合同重新生成，当前SHA-256为
  `9b380964802400ef7a96838b758b68be48df8063e0380d7b3712c1301baa3142`；四项静态
  `FinalArtApproved=true`，程序回退实例仍为false。

## 7. 下一门禁

本批完成后必须由用户逐件接受、要求修改或拒绝。只有接受项才能进入最终源冻结和
`FinalArtApproved=true` 激活；当前审图入口已更新为
`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_MULTI_ANGLE_REVIEW_AND_DECISION_BOARDS_V1.md`。不得据本任务
自动启动第三批或修改其余槽位。

该门禁已由用户“全部接受”关闭；最终状态入口为
`TASK_LUOYANG_P0_LANDMARK_SECOND_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`。第三批仍未授权。
