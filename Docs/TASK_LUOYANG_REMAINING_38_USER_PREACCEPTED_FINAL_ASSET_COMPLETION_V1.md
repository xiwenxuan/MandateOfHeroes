# 洛阳剩余38项用户预接受最终建筑资产完成 V1 任务书

状态：`LUOYANG_REMAINING_38_USER_PREACCEPTED_NATIVE_PREFAB_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

## 1. 用户决定与任务目标

2026-08-27，用户明确要求“直接开发剩下的38个，不用审批直接接受”。本任务据此将54槽位清单中
尚未激活的评审序号`15—21、23—53`统一登记为`PREACCEPTED_ALL_REMAINING_38`，一次完成项目原创
Unity原生Prefab、三级LOD、稳定锚点、真实FBX、来源哈希、运行时加载和最终批准门禁。

预接受只替代逐件人工审图决定，不跳过技术门禁。任一资源只有真实Prefab成功载入并通过身份、LOD、
材质、锚点和零Collider检查时，运行时`FinalAssetApproved=true`；程序回退始终为false。

## 2. 冻结范围

| 优先级 | 槽位 | Facility使用量 | 本轮资产类型 |
|---|---:|---:|---|
| P0 身份关键 | 8 | 8 | 平城门、上东门、上西门、夏门、小苑门、雍门、中东门、南宫北门 |
| P1 高频暴露 | 10 | 1,800 | 住宅、旱田、道路、工坊、园圃、仓储、城墙、宫墙、驿舍、牧场 |
| P2 系统可读 | 14 | 226 | 市肆、商旅院、学校、地方官署、军营、渠、井、桥、军门、坞堡、烽燧、医馆、礼堂、中枢官署 |
| P3 环境支撑 | 6 | 34 | 林业、采石、矿井、水田、公共院落、公共广场 |
| 合计 | 38 | 2,068 | 与先前16项共同覆盖54槽位和2,084项正式Facility |

38项的Facility、Model、Asset Variant、Source Profile、历史依据、代表Cell、优先级与替换槽位全部
逐项继承
[`TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md`](TASK_LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1.md)
及其上游生产、地标、城门、城市织理、基础设施、防御、资源农业、公共礼制医疗目录；本任务不创造
新的历史位置或建筑功能断言。

## 3. 美术建模结果

- 资源根：`Assets/Resources/Art/Han/Luoyang/FinalRemaining/`。
- 38个独立Unity原生Prefab，文件名保留评审序号和稳定Asset Variant。
- 每件根节点恰好一个`LODGroup`和3个非空LOD，LOD0 Renderer数大于LOD2，材质引用完整。
- 共用22个项目原创材质和12个项目原创基础/模块网格，保持东汉中原战略微缩模型语言。
- 城门保留放置与内外通行锚点；其他资产保留其现有稳定放置/入口锚点。
- 38件全部零Collider；碰撞、导航、室内、开闭/损毁动画不在本轮范围。
- 这批是战略地图最终替换资产，不宣称考古单体复原、商业级PBR贴图终稿或可进入室内模型。

## 4. FBX与来源归档

- FBX根：`Assets/ArtSource/Han/Luoyang/FinalRemaining/`。
- 38个真实FBX均由Unity FBX Exporter 4.2.1导出并经Unity `ModelImporter`回读。
- FBX总大小2,302,293字节，单件31,883—106,046字节，全部大于1 KiB。
- 来源清单：
  `Assets/ArtSource/Han/Luoyang/FinalRemaining/luoyang_remaining_38_final_asset_source_manifest_v1.json`。
- 清单覆盖240个项目源/依赖及`.meta`文件、2个工具链文件、38个Prefab、22个Material、12个Mesh
  和38个FBX；所有记录逐项冻结长度与SHA-256。
- 清单连续生成两次SHA-256均为
  `19d27e5ac9f287c4ad841fe65db7db300f9a07f873d744d2ad914dd049091612`。
- 模型、材质和网格均为项目原创；FBX工具链按Unity Companion License登记，未复制、转换或仿制
  商业游戏资产。

## 5. 机器合同与运行时接入

- 机器目录：
  `Assets/StreamingAssets/WorldMap/LuoyangRemainingFinalAssetsV1/luoyang_remaining_final_assets_v1.json`。
- 决定记录：`decision.luoyang-remaining-38.preaccepted.2026-08-27.v1`。
- 目录严格要求38项、2,068使用量、优先级`8/10/14/6`、固定评审序号和逐项54槽位匹配。
- `HanBuildableFacilityModelFactory`在既有生产轮廓解析后按稳定Asset Variant加载对应Resources Prefab；
  真实资源通过门禁时批准，资源缺失或合同不合法时沿用既有程序轮廓并否决批准。
- 全54项审阅实例统一暴露最终资产运行时状态；先前16项保持既有批准事实，本轮38项完成接入后达到
  `54/54 FinalAssetApproved=true`的真实Prefab场景状态。
- 不修改Facility位置、建设权限、人口、岗位、产权、库存、Simulation、Save Schema或批处理世界事实。

## 6. 验收门禁与执行结果

| 门禁 | 结果 |
|---|---|
| 全工程C#编译 | 通过；`tmp/skill-verification/compile-20260827-222453-302.out.log` |
| 定向核心合同 | 1/1通过；`tmp/skill-verification/core-tests-20260827-222543-662.out.log` |
| 最终ProjectLoadSmoke | 通过；`tmp/unity-validation/unity-ProjectLoadSmoke-20260827-222945-559.summary.json` |
| 38 Prefab生成 | 1/1通过；`unity-EditMode-20260827-220939-685.summary.json` |
| 重载后三级LOD/材质/锚点/零Collider | 1/1通过；`unity-EditMode-20260827-221105-843.summary.json` |
| 38 FBX导出与Unity回读 | 1/1通过；`unity-EditMode-20260827-221153-635.summary.json` |
| 真实Prefab批准与强制回退否决 | 1/1通过；`unity-EditMode-20260827-221908-130.summary.json` |
| 240文件逐项哈希与38 FBX来源门禁 | 1/1通过；`unity-EditMode-20260827-222103-637.summary.json` |
| 全54项真实Prefab图形PlayMode | 1/1通过；`unity-PlayMode-20260827-222151-837.summary.json` |
| 最密549 Facility批处理图形PlayMode | 1/1通过；`unity-PlayMode-20260827-222318-233.summary.json` |
| 来源清单重复生成 | 通过；两次SHA-256一致 |
| `git diff --check` | 通过；仅报告工作区既存换行转换提示 |

完整核心、完整EditMode和完整PlayMode套件未运行，不得据此声称全量回归通过。

## 7. 执行中纠错记录

- 第一轮Prefab保存把与另一个MonoBehaviour同文件的元数据组件序列化为`m_Script fileID=0`；已将
  `LuoyangFinalAssetPrefabMetadata`拆为同名独立脚本，重新生成后38/38可由Unity重载。
- 首次来源哈希测试误引入测试程序集未引用的Newtonsoft；已改用Unity `JsonUtility`，不扩大程序集依赖。
- Windows PowerShell对脚本内中文常量产生展示字段乱码；已改为稳定ASCII显示名并重新生成目录，
  所有稳定身份ID和历史依据未变化。
- 一次沙箱内ProjectLoadSmoke在45秒启动日志门禁处被安全终止；使用同一受控脚本在允许环境重试通过，
  没有遗留Unity进程。

## 8. 证据入口与完成边界

- 证据索引：
  `Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1/README.md`。
- 全54项总览：
  `Screenshots/luoyang_all_54_final_assets_activated_v1.png`，1600×1000 Unity实际Game View。
- 生成器：`Assets/Editor/Mandate.Editor/LuoyangRemainingFinalAssetArtBuilder.cs`。
- FBX导出器：`Assets/Editor/Mandate.Editor/LuoyangRemainingFinalAssetFbxExporter.cs`。

本任务完成后，洛阳54个最终资产替换槽位已全部激活，未完成槽位为0。后续工作应转入实际全城构图、
镜头尺度、材质精修、碰撞/导航或特定建筑的更高质量重制，不再继续创建“第五批最终资产槽位”。
