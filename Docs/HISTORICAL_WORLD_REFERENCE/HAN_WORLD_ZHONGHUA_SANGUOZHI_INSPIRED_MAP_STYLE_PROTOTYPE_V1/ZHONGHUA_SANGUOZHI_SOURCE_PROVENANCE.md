# 《中华三国志》地图源码来源记录

## 候选仓库

| 字段 | 结果 |
| --- | --- |
| Repository | `https://github.com/kpxp/ZhongHuaSanGuoZhi-New-Code` |
| Default branch | `master` |
| HEAD SHA | `50f00168e005f7e5d8576e5adc215b1fbe2f8fa5` |
| HEAD date/message | 2016-05-20，`事件新增是否选项` |
| Branches | `master`, `stable-28`, `stable-29` |
| Tags | `v.28`, `v.29` |
| Commits | 297 |
| Contributors | `kpxp` 271，`luiges90` 26 |
| Repository API size | 57,280 KB |
| License API | `null` |
| Recursive tree | 1,914 entries，`truncated=false` |

README 标识官方论坛，要求 VS2008 + XNA 3.0，并说明资源文件不在源码仓库内；README 署名原作者 Clip_on，并列出程序、美术与剧本贡献者。因此该仓库与游戏存在较强关联，但没有单一“官方组织仓库 + 根 LICENSE”证据，身份置信度评为 `MEDIUM_HIGH`，不能仅凭名称推定全部权利。

## Git clone 记录

正式外部参考根：`E:/project/gamedevelop/_external_reference/`。

尝试过：

- 标准 `git clone --no-single-branch`：300 秒内未取得对象，按硬规则终止；
- `git ls-remote`：GitHub 443 无法连接；
- 已登录 GitHub CLI 的 `gh repo clone`：接收连接被重置；
- `--filter=blob:none --no-checkout`：GitHub 443 无法连接。

保留的失败目录没有被删除：

- `ZhongHuaSanGuoZhi-New-Code/`
- `ZhongHuaSanGuoZhi-New-Code-gh/`
- `ZhongHuaSanGuoZhi-New-Code-partial/`

因此：

```text
SOURCE_CLONED = NO
FULL_LOCAL_GIT_HISTORY = NO
HARD_BLOCK = GITHUB_GIT_TRANSPORT_443_FAILURE_OR_RESET
```

## API 静态审计补偿

已登录 GitHub API 可以访问，故在固定 HEAD 上完成：

- 全部分支、标签、提交和贡献者枚举；
- 完整递归树枚举；
- 8 个指定模块共 1,534 文件、101,698,850 bytes 的清单统计；
- 六个核心文件的实际内容读取与类/方法签名提取。

关键固定引用：

- `MapLayerPlugin/MapLayerPlugin/MapLayer.cs`：`MapLayer.Draw`(line 19)、`Initialize`(46)、`Update`(83)；
- `MapLayerPlugin/MapLayerPlugin/MapLayerPlugin.cs`：`Draw`(28)、`Initialize`(33)、`SetScreen`(61)、`Update`(66)；
- `MapViewSelectorPlugin/MapViewSelectorPlugin/MapViewSelector.cs`：`Draw`(42)、`SetDisplayOffset`(210)、`Update`(262)；
- `RoutewayEditorPlugin/RoutewayEditorPlugin/RoutewayEditor.cs`：`CutRouteway`(81)、`Draw`(119)、`ExtendRouteway`(259)、`Update`(735)；
- `DixingBianjiqi/MainMapLayer.cs`：`Draw`(418)、`PrepareMap`(650)、`ReCalculateTileDestination`(724)、`ResetDisplayingTiles`(746)；
- `MapEditor/MapEditor/MapPanel.cs`：`ChangeMapData`(92)、`DoPaint`(156)、`LoadMapData`(242)、`SaveMapData`(281)。

API 静态审计不等同于完整本地 clone，不能把第一层最终状态写成 `ZHONGHUA_SANGUOZHI_SOURCE_RESEARCH_V1_COMPLETE`。
