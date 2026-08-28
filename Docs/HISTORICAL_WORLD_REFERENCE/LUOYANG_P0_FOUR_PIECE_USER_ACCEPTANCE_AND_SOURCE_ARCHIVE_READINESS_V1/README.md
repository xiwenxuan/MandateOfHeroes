# 洛阳 P0 四件套用户接受登记与源资产归档就绪 V1

> 本目录记录最终激活前的历史快照。后续 FBX 源冻结与最终激活已经完成，当前入口为 `../LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1/README.md`；下述“四个 FBX 缺失/批准为 false”仅描述当时状态。

## 当前结论

- 用户决定：南宫、明堂、广阳门、北宫南门四件全部接受。
- 用户决定日期：2026-08-27。
- Unity 原生源：生成器、P0 目录、4 Prefab、6 Material、4 Mesh 及 `.meta` 已完成 SHA-256 归档。
- 独立 DCC/FBX：四个冻结目标均缺失。
- `FinalArtApproved`：保持 `false`，等待独立源到位或用户明确改变旧门禁。

当前状态：

`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_UNITY_NATIVE_SOURCE_ARCHIVED_INDEPENDENT_DCC_FBX_REQUIRED_FINAL_ACTIVATION_PENDING`

## 权威文件

- 任务书：`Docs/TASK_LUOYANG_P0_FOUR_PIECE_USER_ACCEPTANCE_AND_SOURCE_ARCHIVE_READINESS_V1.md`
- 静态内容目录：`Assets/StreamingAssets/WorldMap/LuoyangP0FinalAssetVerticalSliceV1/luoyang_p0_final_asset_vertical_slice_v1.json`
- Unity 原生源说明：`Assets/ArtSource/Han/Luoyang/P0Final/README.md`
- 源归档机器清单：`Assets/ArtSource/Han/Luoyang/P0Final/luoyang_p0_source_archive_manifest_v1.json`
- 生成脚本：`MapPipeline/scripts/build_luoyang_p0_source_archive_manifest_v1.ps1`

## 冻结 FBX 目标

| 建筑 | 目标路径 | 当前状态 |
|---|---|---|
| 南宫 | `Assets/ArtSource/Han/Luoyang/P0Final/SouthPalace.fbx` | 缺失 |
| 明堂 | `Assets/ArtSource/Han/Luoyang/P0Final/Mingtang.fbx` | 缺失 |
| 广阳门 | `Assets/ArtSource/Han/Luoyang/P0Final/Guangyangmen.fbx` | 缺失 |
| 北宫南门 | `Assets/ArtSource/Han/Luoyang/P0Final/NorthPalaceSouthGate.fbx` | 缺失 |

不得以空文件、重命名资源或未验证导出物满足本门禁。真实源到位后，必须验证模型对应关系、三级 LOD、材质、稳定锚点、零 Collider、Prefab 重建一致性和源文件许可。
