# 洛阳 P0 地标第三批原生 Prefab、FBX 与审模证据 V1

历史状态：`LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_SOURCE_READY_FOR_USER_REVIEW_V1`

后续当前状态：用户已接受四项，最终激活证据见
`../LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1/README.md`。

本证据包对应54槽位清单中最低剩余P0评审序号`6/7/8/9`：灵台、太仓、武库、濯龙园。四件均为
项目原创战略地图审模候选，不改变既有Facility、Model、Asset Variant、Global Cell、建设权限、
模拟或存档。

## 审图入口

- [四件总览](Screenshots/luoyang_p0_landmark_batch3_overview_v1.png)
- [灵台近景](Screenshots/luoyang_p0_lingtai_candidate_v1.png)
- [太仓近景](Screenshots/luoyang_p0_taicang_candidate_v1.png)
- [武库近景](Screenshots/luoyang_p0_arsenal_candidate_v1.png)
- [濯龙园近景](Screenshots/luoyang_p0_zhuolong_garden_candidate_v1.png)

五张图片均为1600×1000 Unity实际Game View，没有生成式重绘。

## 视觉检查

- 灵台：四级收分高台、南阶和观测杆在近景与总览中形成最高竖向轮廓。
- 太仓：四个仓廪以2×2阵列、圆仓体和独立覆顶形成容量识别，不宣称历史实测数量。
- 武库：围墙、门楼、北库与内院军械架形成闭合防护轮廓。
- 濯龙园：池、桥、亭和树冠形成低矮非对称苑囿轮廓；它是可撤销游戏复原。
- 五图主体均在画面内且未被地形遮挡；Cell线保留为战略地图比例背景，不代表最终地表美术。

## 机器与来源证据

- 机器目录：
  `Assets/StreamingAssets/WorldMap/LuoyangP0LandmarkThirdBatchV1/luoyang_p0_landmark_third_batch_v1.json`。
- 原生Prefab：`Assets/Resources/Art/Han/Luoyang/P0Batch3/`。
- FBX与来源清单：`Assets/ArtSource/Han/Luoyang/P0Batch3/`。
- 来源清单覆盖60个项目源/依赖及元数据文件、2个工具链文件和4个FBX。
- 本包冻结的候选期清单SHA-256为
  `8d286a6013c9c83c111c2c57b8e9f3fac071de5d82acdaee8c71cf0243a5d444`；当前接受后清单由最终激活任务负责。
- 四件均有三个非空且严格递减LOD、完整材质、放置/入口锚点、真实FBX和零Collider。
- 候选期运行时加载真实Prefab且未触发程序回退；当时最终批准为false。

## 用户门禁

本包保留用户决定前的`PENDING`、`FinalArtApproved=false`历史输入。用户随后在该五视图上下文中明确
回复“接受”，最终状态不回写到历史图片；本证据包及后续决定均不授权第四批或未触及的42个槽位。
