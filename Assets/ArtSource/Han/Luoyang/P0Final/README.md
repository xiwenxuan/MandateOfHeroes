# 洛阳 P0 四件套 Prefab 与 FBX 最终源说明

- 交付阶段：`LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1`
- 来源：项目原创，由仓库内 Unity Editor 生成器参数化生成。
- 生成器：`Assets/Editor/Mandate.Editor/LuoyangP0NativePrefabArtBuilder.cs`
- 运行时资产：`Assets/Resources/Art/Han/Luoyang/P0Final/`
- 许可：项目原创；未复制或转换任何商业游戏模型、贴图或材质。
- 当前形态：V2 Unity 原生 Prefab、材质、网格资产和四个真实 FBX；每件含三档逐级简化 LOD、稳定锚点，且不含碰撞体。
- V2 修订：补强屋脊、檐带、门扇、台阶、铺地、阙楼和旗杆等远景识别特征，并重设四件套审查镜头。
- 用户决定：2026-08-27 对南宫、明堂、广阳门、北宫南门四件全部接受。
- FBX 工具链：Unity FBX Exporter 4.2.1 与 Autodesk FBX SDK Unity 绑定 4.2.1，均为 Unity Companion License。
- 锚点映射：官方兼容规则把点号转换为下划线，最终清单保存稳定 ID 到 FBX 节点名的可逆映射。
- 最终源归档：生成器、导出器、目录、Prefab、材质、网格、4 FBX 及相应 `.meta` 共 42 个文件由
  `luoyang_p0_final_source_archive_manifest_v1.json` 记录路径、长度与 SHA-256。
- 最终批准：四项 `FinalArtApproved=true`；运行时只有真实 Prefab 成功加载时显示为真，程序回退实例保持假。
- 尚未交付：手绘/PBR 贴图、考古复原级细节、室内、碰撞、导航和损毁；这些不属于本次战略地图四件套批准。

重新生成时使用 Unity 菜单 `Mandate/Luoyang/Build P0 Native Prefab Art V2`。
生成器按固定路径更新自有资产，并保留既有 `.meta` GUID。
重新导出 FBX 时使用 `Mandate/Luoyang/Export P0 Accepted FBX Sources V1`，随后必须重跑 Unity 回读测试并重新生成最终源清单。
