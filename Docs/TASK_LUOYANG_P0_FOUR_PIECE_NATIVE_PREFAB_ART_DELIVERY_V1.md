# 洛阳 P0 四件套原生 Prefab 美术交付 V1 任务书

> 后续状态：V2 精修、多角度审查和用户接受已经完成；`TASK_LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1.md` 已生成并回读验证四个真实 FBX，四件套现为 `FinalArtApproved=true`。

## 1. 任务结论

本任务承接“洛阳 P0 最终资产四件套垂直切片 V1”，把南宫、明堂、广阳门、北宫南门从程序化集成候选推进为四套实际存在、可被运行时直接加载的 Unity 原生 Prefab 美术候选。

当前交付状态为：

`READY_FOR_USER_REVIEW_FINAL_ART_APPROVAL_PENDING`

这表示四套 Prefab、六个材质、四个共享网格、三级 LOD、稳定锚点、零 Collider 和运行时加载均已完成自动验收；它们仍是项目原创的战略地图审图候选，没有独立 FBX/DCC 源、手绘贴图或用户最终批准，因此 `FinalArtApproved` 必须继续为 `false`。

## 2. 目标与边界

### 目标

- 在四个既有 Resources 路径生成真实 Prefab，而非继续只展示运行时程序化对象。
- 四套资产共用夯土、朱红、灰绿瓦、石、木、青铜六材质和四种项目原创 Unity 网格。
- 每个 Prefab 恰有三个非空 LOD；每个 Renderer 有材质；全部稳定锚点存在；不得包含 Collider。
- 运行时优先加载原生 Prefab，只有资源缺失时才回退原程序候选。
- 生成一张总览和四张近景，供用户按固定机位审图。

### 不在本轮

- 不修改 Facility、Model、Asset Variant、Profile、Global Cell、史料置信度或建设权限。
- 不修改 Domain 世界事实、Simulation、存档结构、碰撞、导航、室内或损毁系统。
- 不把 Unity 原生候选冒充考古复原、独立 FBX 终模、最终贴图或用户批准美术。
- 不批量替换其余 50 个资产槽位。

## 3. 四件套交付

| 建筑 | Resources Prefab | LOD0 识别重点 | 稳定身份 |
|---|---|---|---|
| 南宫 | `Art/Han/Luoyang/P0Final/SouthPalace` | 双朝院、双正殿、两侧廊与前阶 | 原南宫 Facility/Model/Asset/Cell 不变 |
| 明堂 | `Art/Han/Luoyang/P0Final/Mingtang` | 方形三重台、中心礼殿、四角柱与栏 | 原明堂 Facility/Model/Asset/Cell 不变 |
| 广阳门 | `Art/Han/Luoyang/P0Final/Guangyangmen` | 门道、双墙、门楼与短瓮城 | 原广阳门 Facility/Model/Asset/Cell 不变 |
| 北宫南门 | `Art/Han/Luoyang/P0Final/NorthPalaceSouthGate` | 中央门楼、双阙、南向通道 | 原北宫南门 Facility/Model/Asset/Cell 不变 |

## 4. 实施内容

- 新增 `Mandate.Editor` 编辑器程序集和可重复执行的 `LuoyangP0NativePrefabArtBuilder`。
- 生成 4 个 Prefab、6 个 Material、4 个共享 Mesh；重复执行时更新生成器自有资产并保留既有 `.meta` GUID。
- P0 机器清单改为 `native_prefab_with_procedural_fallback`，四项 `ArtistPrefabPresent=true`、`FinalArtApproved=false`。
- 运行时加载分支、程序化回退分支和严格 Prefab 合同均保留。
- 新增生成器合同测试，并把原 P0 EditMode/PlayMode 回归切换到真实 Prefab 在位状态。
- 完成项目原创来源登记，未引入任何商业游戏模型、贴图或界面素材。

## 5. 验收结果

| 门禁 | 结果 |
|---|---|
| 全工程 C# 编译 | 通过，含新增 `Mandate.Editor` 程序集 |
| 定向核心合同 | 1/1 通过 |
| 原生资产生成/合同 EditMode | 1/1 通过 |
| 既有 P0 EditMode 回归 | 4/4 通过 |
| 图形 PlayMode 加载与截图 | 1/1 通过；四项均加载 Prefab，回退均未激活 |
| 受影响全城批处理图形回归 | 1/1 通过；1,673 源模块合并为 97 Renderer、17,512 顶点，24.0398ms |
| 截图证据 | 5 张 1600×1000 PNG 已生成 |
| `git diff --check` | 通过 |

全量核心套件未在本任务中重新宣称通过；上游任务记录的 300 秒超时事实仍保留。

## 6. 证据与复现

- 资产生成：Unity 菜单 `Mandate/Luoyang/Build P0 Native Prefab Art V1`。
- 资产路径：`Assets/Resources/Art/Han/Luoyang/P0Final/`。
- 源说明：`Assets/ArtSource/Han/Luoyang/P0Final/README.md`。
- 截图路径：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_NATIVE_PREFAB_ART_DELIVERY_V1/Screenshots/`。
- Unity 汇总：`tmp/unity-validation/unity-EditMode-20260827-131714-375.summary.json`、`unity-EditMode-20260827-132023-112.summary.json`、`unity-PlayMode-20260827-132054-533.summary.json`、`unity-PlayMode-20260827-132824-104.summary.json`。

## 7. 下一步门禁

用户审模已经接受，Unity 原生源也已建立 SHA-256 归档。下一门禁只剩四个冻结路径对应的真实独立 DCC/FBX 源及其一致性验证；完成前不得将 `FinalArtApproved` 改为 `true`，也不开始其余 50 个槽位的批量替换。
