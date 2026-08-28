# 洛阳 P0 四件套 FBX 源冻结与最终激活 V1 证据索引

## 状态

`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

南宫、明堂、广阳门、北宫南门四个用户接受的 V2 资产已通过 Unity FBX Exporter 4.2.1 生成真实 FBX，并由 Unity 重新导入验证三级 LOD、材质、锚点位置、几何包围盒和零 Collider。四项静态 `FinalArtApproved` 已激活；运行时仍要求真实 Prefab 加载成功，否则实例批准自动降为 `false`。

## 机器证据

- 最终源清单：`Assets/ArtSource/Han/Luoyang/P0Final/luoyang_p0_final_source_archive_manifest_v1.json`。
- 清单生成器：`MapPipeline/scripts/build_luoyang_p0_final_source_archive_manifest_v1.ps1`。
- FBX 导出器：`Assets/Editor/Mandate.Editor/LuoyangP0FbxSourceExporter.cs`。
- Unity 回读测试：`Assets/Tests/EditMode/LuoyangP0FbxSourceExportV1Tests.cs`。
- 最终激活测试：`Assets/Tests/EditMode/LuoyangP0FbxFinalActivationV1Tests.cs`。

## 四件套

| 建筑 | FBX 路径 | 导入状态 |
|---|---|---|
| 南宫 | `Assets/ArtSource/Han/Luoyang/P0Final/SouthPalace.fbx` | `PRESENT_UNITY_REIMPORT_VALIDATED` |
| 明堂 | `Assets/ArtSource/Han/Luoyang/P0Final/Mingtang.fbx` | `PRESENT_UNITY_REIMPORT_VALIDATED` |
| 广阳门 | `Assets/ArtSource/Han/Luoyang/P0Final/Guangyangmen.fbx` | `PRESENT_UNITY_REIMPORT_VALIDATED` |
| 北宫南门 | `Assets/ArtSource/Han/Luoyang/P0Final/NorthPalaceSouthGate.fbx` | `PRESENT_UNITY_REIMPORT_VALIDATED` |

## 边界

- FBX 锚点使用官方兼容命名规则：点号转换为下划线；最终清单保存完整可逆映射。
- 导出专用锚点标记不进入运行时 Prefab。
- 本交付仍是项目原创战略地图资产，不是考古复原或手绘/PBR 贴图终稿。
- 其余 50 个替换槽位没有随本任务获得批量批准。
