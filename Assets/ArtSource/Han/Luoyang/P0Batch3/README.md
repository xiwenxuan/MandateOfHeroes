# 洛阳 P0 地标第三批最终激活源说明

- 当前任务：`LUOYANG_P0_LANDMARK_THIRD_BATCH_USER_ACCEPTANCE_AND_FINAL_ACTIVATION_V1`。
- 源血统任务：`LUOYANG_P0_LANDMARK_THIRD_BATCH_NATIVE_PREFAB_FBX_REVIEW_V1`。
- 当前状态：灵台、太仓、武库、濯龙园四件已由用户接受，Unity 原生 Prefab 与真实 FBX 已最终激活。
- 选择依据：54槽位冻结清单中，在已激活首批与第二批之后，最低剩余P0评审序号`6/7/8/9`。
- Prefab：`Assets/Resources/Art/Han/Luoyang/P0Batch3/`。
- FBX：本目录下`Lingtai.fbx`、`Taicang.fbx`、`Arsenal.fbx`、`ZhuolongGarden.fbx`。
- 来源清单：`luoyang_p0_landmark_third_batch_source_manifest_v1.json`，SHA-256为
  `40e1ccad3af83e9b16119df73b435bc2ae1d9b46c97af9db5087904a53fc50c2`。
- 工具链：Unity FBX Exporter 4.2.1与Autodesk FBX SDK Unity绑定4.2.1，Unity Companion License。
- 锚点映射：稳定ID中的点号在FBX节点中可逆转换为下划线。
- 来源：全部几何由本项目参数化生成器独立制作，复用本项目首批与第二批的材质和基础网格；未复制、
  转换或仿制商业游戏资产。
- 审批边界：四项静态`FinalArtApproved=true`；运行时只有真实Prefab加载成功时批准为真，程序回退
  实例强制为false。
- 本任务不授权第四批或其余42个最终资产槽位。
