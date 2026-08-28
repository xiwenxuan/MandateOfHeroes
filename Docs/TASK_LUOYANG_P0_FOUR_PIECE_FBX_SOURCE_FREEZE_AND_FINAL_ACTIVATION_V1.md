# 洛阳 P0 四件套 FBX 源冻结与最终激活 V1 任务书

## 1. 任务目标

关闭南宫、明堂、广阳门、北宫南门四件套唯一剩余的独立 FBX 源门禁：在不改变用户已接受视觉、稳定身份、权威 Cell、建设权限、Simulation 或 Save 的前提下，从四套 Unity 原生 Prefab 导出真实可编辑 FBX，重新导入 Unity 完成一致性验证，并在全部门禁通过后激活四项 `FinalArtApproved=true`。

本任务的最终批准仅指“用户已接受的洛阳战略地图 P0 四件套 V2 资产已具备冻结 FBX 源并可进入运行时正式槽位”，不把资产提升为考古复原、手绘/PBR 贴图终稿、室内、碰撞、导航、损毁或其他 50 个槽位的最终美术。

## 2. 固定范围

- 只处理南宫、明堂、广阳门、北宫南门四个既有 P0 槽位。
- 保持 Facility、Model、Asset Variant、Profile、Global Cell、史料元数据及权限不变。
- 保持四套用户接受的 V2 Unity Prefab、六材质、四共享网格、三级 LOD 和运行时程序回退。
- 生成目录中已经冻结的四个 `.fbx`，不得增加或改指第五个目标。
- 运行时只有真实 Prefab 成功加载时才能显示最终批准；若资源缺失并进入程序回退，实例级批准必须降为 `false`。
- 不修改 Save Schema，不制作手绘贴图，不批量替换其余 50 个槽位。

## 3. 工具链与许可

- Unity：`2022.3.62f3c1`。
- FBX Exporter：`com.unity.formats.fbx@4.2.1`。
- Autodesk FBX SDK Unity 绑定：`com.autodesk.fbx@4.2.1`。
- 两个包均由 Unity Package Manager 从项目既有 registry 解析，许可为 Unity Companion License。
- 项目输出的建筑几何、材质与 FBX 均为项目原创；未复制或转换任何商业游戏模型、贴图或界面素材。

## 4. 实施方案

1. 在 `Mandate.Editor` 接入官方 FBX Exporter 编辑器程序集。
2. 新增 `LuoyangP0FbxSourceExporter`，按静态目录中的冻结路径逐件导出。
3. 在临时 Prefab 内容副本上移除 `LODGroup` 组件，但保留 `LOD0/LOD1/LOD2` 命名层级，避免官方 LOD 访问器遗漏独立锚点；源 Prefab 不修改。
4. 为锚点添加导出专用极小三角标记，使 FBX/DCC 中保留锚点节点；这些标记不进入运行时 Prefab。
5. 记录官方兼容命名映射：稳定锚点 ID 的点号在 FBX 中可逆转换为下划线，例如 `anchor.p0.south_palace.placement -> anchor_p0_south_palace_placement`。
6. 用 Unity `ModelImporter` 重新导入四个 FBX，验证三档 LOD 渲染器数量、全部材质、锚点位置、整体几何包围盒和零 Collider。
7. 生成包含 42 个源/元数据文件、2 个工具链文件及 4 个 FBX 哈希的最终源归档清单。
8. 只有上述验证通过后，更新静态合同、运行时状态和 `FinalArtApproved`。

## 5. 最终交付

| 建筑 | FBX | 大小（字节） | SHA-256 |
|---|---|---:|---|
| 南宫 | `SouthPalace.fbx` | 91,662 | `c913c5f513addc69a8a40a7b5e9f008bc54e37850097488c94537a13deaf5876` |
| 明堂 | `Mingtang.fbx` | 83,342 | `e3bf4193cad72741a5dd9a7ef91f43a66d97947e7b5f8d6c948dd235807129dc` |
| 广阳门 | `Guangyangmen.fbx` | 95,927 | `a74cbffa068ad1fa9b8300c2c4feab47121b0033d128debfeea04ca2053beb5b` |
| 北宫南门 | `NorthPalaceSouthGate.fbx` | 101,908 | `4c57ad5f40db6c5eba335d433e299e2715960add2d35cf1f4f38b199ff7814f2` |

最终源清单为 `Assets/ArtSource/Han/Luoyang/P0Final/luoyang_p0_final_source_archive_manifest_v1.json`，当前 SHA-256 为 `434b66cbaba391c43f6eaf557c542576a74199dc1652e7f5c822fe1dea8280dd`。

官方导出器会写入导出元数据，因此重复导出后 FBX 字节哈希允许变化；每次重新导出必须重新执行 Unity 语义一致性测试并再生成最终哈希清单，不得把旧哈希沿用到新文件。

## 6. 状态结果

当前状态为：

`LUOYANG_P0_FOUR_PIECE_USER_ACCEPTED_FBX_SOURCE_VALIDATED_FINAL_ART_ACTIVATED_V1`

- 四件套用户决定继续为 `ACCEPTED_ALL_FOUR`。
- 四个 FBX 均存在、非空、可由 Unity 重新导入，并与获批 Prefab 的 LOD、材质、锚点和几何一致。
- 静态内容合同进入 `source_archive.unity_native_and_fbx_complete.v1`。
- 四项 `ArtistPrefabPresent=true`、`FinalArtApproved=true`。
- 运行时若实际加载 Prefab，实例显示最终批准；若回退程序候选，实例最终批准为 `false`。
- 其余 50 个最终资产槽位仍未授权批量替换。

## 7. 验收门禁

1. 官方包与依赖均冻结为 `4.2.1`，项目加载编译通过。
2. 四个冻结 FBX 目标均存在且大于 1 KiB。
3. Unity 回读验证四项三级 LOD、Renderer 数量、材质、锚点映射、锚点位置、几何包围盒和 Collider 合同。
4. 最终清单覆盖 42 个当前源/`.meta` 文件和 2 个工具链文件，路径唯一且 SHA-256 与磁盘一致。
5. Domain、静态目录、运行时、EditMode 和 PlayMode 对最终批准状态一致。
6. 全工程编译、定向核心、受控 Unity 测试、批处理回归、`git diff --check` 和范围审阅通过。

## 8. 证据入口

### 验收记录

| 门禁 | 结果 |
|---|---|
| Unity 官方包解析与项目加载 | 通过；ProjectLoadSmoke 26.283 秒 |
| 全工程 C# 编译 | 通过；`compile-20260827-155043-569.out.log` |
| 定向核心合同 | 1/1 通过；`core-tests-20260827-155059-746.out.log` |
| FBX 导出与 Unity 回读 EditMode | 1/1 通过；`unity-EditMode-20260827-155133-068.summary.json` |
| 最终源清单与批准合同 EditMode | 1/1 通过；`unity-EditMode-20260827-160518-847.summary.json` |
| 既有 P0 身份/LOD/回退 EditMode | 4/4 通过；`unity-EditMode-20260827-155310-521.summary.json` |
| 最终 Prefab 实际加载 PlayMode | 1/1 通过；`unity-PlayMode-20260827-155401-442.summary.json` |
| 最密 549 Facility 合批 PlayMode | 1/1 通过；`unity-PlayMode-20260827-155508-282.summary.json` |
| 最终清单重复生成 | 通过；SHA-256 保持 `434b66cbaba391c43f6eaf557c542576a74199dc1652e7f5c822fe1dea8280dd` |
| `git diff --check`、尾随空白与范围审阅 | 通过 |

完整核心套件和完整 EditMode 套件未在本任务中运行；上述结果是直接相关的定向回归，不得扩写为全量测试通过。

- 交付索引：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_P0_FOUR_PIECE_FBX_SOURCE_FREEZE_AND_FINAL_ACTIVATION_V1/README.md`。
- 导出器：`Assets/Editor/Mandate.Editor/LuoyangP0FbxSourceExporter.cs`。
- 导出测试：`Assets/Tests/EditMode/LuoyangP0FbxSourceExportV1Tests.cs`。
- 最终激活测试：`Assets/Tests/EditMode/LuoyangP0FbxFinalActivationV1Tests.cs`。
- 清单生成器：`MapPipeline/scripts/build_luoyang_p0_final_source_archive_manifest_v1.ps1`。

## 9. 下一步边界

本任务完成后，四件套 P0 竖切片关闭。下一项建筑美术工作必须另行选择剩余 50 个替换槽位中的有限批次并重新走史料、模型、LOD、来源、审图和运行时门禁；不得因为本四件套完成而自动批量批准其余槽位。
