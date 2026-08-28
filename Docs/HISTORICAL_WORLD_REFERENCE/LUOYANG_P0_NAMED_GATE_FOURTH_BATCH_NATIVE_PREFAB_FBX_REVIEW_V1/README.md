# 洛阳 P0 命名城门第四批原生 Prefab、FBX 与审模证据 V1

当前状态：`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

历史说明：本证据包冻结用户决定前的候选图片和`PENDING/false`状态；用户已于2026-08-27接受四件，
当前批准状态见
[`LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`](../LUOYANG_P0_NAMED_GATE_FOURTH_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md)。

本证据包对应54槽位清单中最低剩余P0评审序号`11/12/13/14`：谷门、津门、开阳门、旄门。
四件均为项目原创战略地图审模候选，不改变既有Facility、Model、Asset Variant、Global Cell、方向、
建设权限、模拟或存档。

## 审图入口

- [四门总览](Screenshots/luoyang_p0_named_gate_batch4_overview_v1.png)
- [谷门近景](Screenshots/luoyang_p0_gumen_candidate_v1.png)
- [津门近景](Screenshots/luoyang_p0_jinmen_candidate_v1.png)
- [开阳门近景](Screenshots/luoyang_p0_kaiyangmen_candidate_v1.png)
- [旄门近景](Screenshots/luoyang_p0_maomen_candidate_v1.png)

五张图片均为1600×1000受控Unity实际Game View，不是生成式重绘。

## 视觉检查目标

- 谷门：北向单楼在总览与近景中保持单一主楼轮廓，脊饰和望柱可见。
- 津门：石质引道、两侧水带与标识柱形成“津口”识别，但不解释为具体历史水工复原。
- 开阳门：主楼高度和双阙在同批四门中形成最强仪典轮廓。
- 旄门：厚实紧凑门楼与守门杆形成低宽防守轮廓。
- 四门按北180°、南0°、南0°、东270°摆放；五图主体完整、无裁切或地形遮挡。
- Cell线只作战略地图比例背景；总览中心黄色选择框不是新增Facility。

## 机器与来源证据

- 机器目录：
  `Assets/StreamingAssets/WorldMap/LuoyangP0NamedGateFourthBatchV1/luoyang_p0_named_gate_fourth_batch_v1.json`。
- 原生Prefab：`Assets/Resources/Art/Han/Luoyang/P0Batch4/`。
- FBX与来源清单：`Assets/ArtSource/Han/Luoyang/P0Batch4/`。
- 来源清单覆盖56个项目源/依赖及元数据文件、2个工具链文件和4个FBX，SHA-256为
  `a709f0b53267a0630fcb8fb207fca908484db13b6c3aedf898d2608878d40785`；连续生成两次一致。
- 四件都必须有三个非空且严格递减LOD、完整材质、放置/内外通行锚点、真实FBX和零Collider。
- 已验证四件LOD Renderer总数`50 / 31 / 12`；运行时加载真实Prefab且不触发程序回退，同时保持
  `FinalArtApproved=false`；强制资源缺失时回退既有城门轮廓并继续保持false。

## 用户门禁

本证据包的决定状态固定为`PENDING`。本轮执行不会替用户接受四门，也不授权第五批或其余38个
未触及槽位。
