# 洛阳 P0 命名城门第四批原生 Prefab、FBX 与审模候选 V1 任务书

当前状态：`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

历史说明：本文件保留用户决定前的候选生产与验收事实。用户已于2026-08-27接受第四批四件，当前
批准状态以`TASK_LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1.md`为准。

## 1. 任务目标

承接前三批共12项最终激活结果，按54槽位冻结清单选取最低剩余P0评审序号`11/12/13/14`：
谷门、津门、开阳门、旄门。为四座命名城门制作项目原创三级LOD Unity原生Prefab、真实FBX、
稳定放置/内外通行锚点、运行时热替换、程序回退和五视图审模证据。

用户要求“给出下一步任务书，并执行”按既有顺序解释为授权这个有限第四批进入候选生产，不等于
用户已经接受模型。四项必须保持`FinalArtApproved=false`；不得据此启动第五批或批准其余槽位。

## 2. 冻结选择与历史依据

| 顺序 | 城门 | Facility ID | 权威 Cell | 方向 | 史料边界与战略轮廓 |
|---:|---|---|---:|---|---|
| 11 | 谷门 | `facility.instance.luoyang.184.gate.gumen` | 4,084,888 | 北 | HistoricalAnchor / Approximate；单楼为战略识别设计 |
| 12 | 津门 | `facility.instance.luoyang.184.gate.jinmen` | 4,144,537 | 南 | HistoricalAnchor / Probable；石质引道只强化“津”字识别，不证明具体水工形制 |
| 13 | 开阳门 | `facility.instance.luoyang.184.gate.kaiyangmen` | 4,144,549 | 南 | HistoricalAnchor / Probable；高楼双阙为战略识别设计 |
| 14 | 旄门 | `facility.instance.luoyang.184.gate.maomen` | 4,131,296 | 东 | HistoricalAnchor / Approximate；紧凑守门楼为战略识别设计 |

广阳门的评审序号是10，但已经在第一批最终激活，因此不重复选择。四门的名称、Cell、方向、
史料置信度、来源、模型与Asset Variant全部逐项复用既有城门身份目录和54槽位清单；本任务不创造
新的历史断言。

## 3. 冻结边界

- 保持Facility、Model、Asset Variant、Profile、Global Cell、方向、建设权限和史料元数据不变。
- 每件恰好三个非空且Renderer数量严格递减的LOD，具有放置、外侧通行、内侧通行锚点，材质完整且
  零Collider。
- 本地模型统一以南向为零旋转；审图摆放必须按既有`VisualFacing`应用南0°、西90°、北180°、东270°。
- 运行时优先加载真实Prefab；资源缺失时回退既有命名城门程序轮廓。
- 无论真实Prefab还是回退，用户审模前实例`FinalArtApproved=false`。
- 全城远景批处理继续使用稳定城门LOD2模块，不将FBX节点或评审板Cell变成世界事实。
- 不修改人口、岗位、物资、产权、控制、Simulation、Save Schema或权威Facility位置。
- 不宣称考古单体复原、PBR终稿、室内、导航、碰撞、开闭/损毁动画或最终美术批准。

## 4. 实施内容

1. 新增第四批Domain/Persistence目录，交叉验证城门身份目录和最终资产评审清单。
2. 制作谷门北向单楼、津门石质引道、开阳门高楼双阙、旄门紧凑守门楼四套原创原生Prefab。
3. 复用项目首批六种材质、第二批水体材质及四个项目原生基础网格，不引入外部素材。
4. 用Unity FBX Exporter 4.2.1导出四个真实FBX，并回读检查层级、锚点和零Collider。
5. 在运行时工厂新增第四批独立身份与待审状态，接入2×2评审板、权威朝向和五个固定镜头。
6. 输出一张总览和四张1600×1000 Unity Game View，并冻结来源文件、工具链和FBX哈希。

## 5. 模型结果

| 城门 | 原生Prefab | FBX | LOD0识别重点 |
|---|---|---|---|
| 谷门 | `P0Batch4/Gumen.prefab` | `Gumen.fbx` | 北向单楼、墙台、脊饰与望柱 |
| 津门 | `P0Batch4/Jinmen.prefab` | `Jinmen.fbx` | 石质引道、两侧水带与津口标识柱 |
| 开阳门 | `P0Batch4/Kaiyangmen.prefab` | `Kaiyangmen.fbx` | 高门楼、双阙和仪典脊饰 |
| 旄门 | `P0Batch4/Maomen.prefab` | `Maomen.fbx` | 紧凑楼体、厚墙台和守门标识杆 |

- 四件LOD Renderer总数为`50 / 31 / 12`，每件均严格递减。
- 四个FBX大小分别为73,435 / 74,823 / 45,979 / 72,748字节，均大于1 KiB并由Unity重新导入。
- 四项`ArtistPrefabPresent=true`、`FinalArtApproved=false`。
- 评审板朝向依次为北180°、南0°、南0°、东270°。

## 6. 来源归档

- 来源清单：
  `Assets/ArtSource/Han/Luoyang/P0Batch4/luoyang_p0_named_gate_fourth_batch_source_manifest_v1.json`。
- 覆盖56个项目源/依赖及`.meta`文件、2个工具链文件和4个真实FBX。
- 清单SHA-256：`a709f0b53267a0630fcb8fb207fca908484db13b6c3aedf898d2608878d40785`。
- 清单连续生成两次哈希一致。
- 模型、材质和网格均为项目原创；FBX工具链为Unity Companion License。

## 7. 验收门禁与执行记录

| 门禁 | 要求 | 当前结果 |
|---|---|---|
| 全工程C#编译 | 0 error | 通过；`tmp/skill-verification/compile-20260827-203202-864.out.log` |
| 定向核心身份合同 | 1/1 | 通过；`tmp/skill-verification/core-tests-20260827-203253-187.out.log` |
| ProjectLoadSmoke | Unity 2022项目加载成功 | 通过；`unity-ProjectLoadSmoke-20260827-202252-989.summary.json` |
| 原生Prefab与三级LOD EditMode | 1/1 | 通过；`unity-EditMode-20260827-202450-709.summary.json` |
| 四FBX导出与Unity回读 EditMode | 1/1 | 通过；`unity-EditMode-20260827-202523-355.summary.json` |
| 来源哈希与待审门禁 EditMode | 1/1 | 通过；`unity-EditMode-20260827-202616-503.summary.json` |
| 真实Prefab加载且批准保持false EditMode | 1/1 | 通过；`unity-EditMode-20260827-202649-339.summary.json` |
| Prefab缺失回退且批准保持false EditMode | 1/1 | 通过；`unity-EditMode-20260827-203317-494.summary.json` |
| 五视图图形PlayMode | 1/1并输出5张图 | 通过；`unity-PlayMode-20260827-202727-530.summary.json` |
| 最密549 Facility批处理图形PlayMode | 1/1 | 通过；`unity-PlayMode-20260827-203031-581.summary.json` |
| 五图人工检查 | 主体完整、身份可辨、无主体裁切或地形遮挡 | 通过 |
| 来源清单 | 56个源/依赖及元数据、2个工具链、4个FBX逐项哈希 | 通过；重复哈希一致 |
| `git diff --check`与范围审阅 | 通过 | 通过；仅有工作区既存换行警告 |

完整核心、完整EditMode和完整PlayMode套件不属于本任务的最低门禁；如未运行，不得声称全量通过。

沙箱内首次ProjectLoadSmoke因45秒内无启动日志被安全终止；按验证规则在外部受控环境重试通过。
批处理回归首次过滤器使用了错误命名空间，执行0项并被工具拒绝；使用正确完整测试名重跑1/1通过。
这两次均未被计为通过，也没有遗留Unity进程。

## 8. 证据入口

- 证据索引：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1/README.md`。
- 生成器：`Assets/Editor/Mandate.Editor/LuoyangP0NamedGateFourthBatchArtBuilder.cs`。
- FBX导出器：`Assets/Editor/Mandate.Editor/LuoyangP0NamedGateFourthBatchFbxExporter.cs`。
- 来源说明：`Assets/ArtSource/Han/Luoyang/P0Batch4/README.md`。

## 9. 用户门禁与下一步

本任务完成后只把四座城门交付为可审候选。只有用户在五视图或后续审图材料上明确接受，才能另建
“用户接受与最终激活”任务，将对应静态标志更新为true。第五批和其余38个未触及槽位仍需单独授权。
