# 法律与素材原则

## Document Governance

- Purpose：定义外部代码、数据、素材、许可证和原创表达边界。
- Authority：L1 CANONICAL SYSTEM SPEC。
- Covers：许可证、来源、第三方内容和禁止复制。
- DoesNotCover：具体美术风格实现或历史资料真伪。
- Supersedes：早期零散素材建议。
- SupersededBy：无。
- RelatedCanonicalDocs：`MAP_ART_RESOURCE_PLAN.md`、`KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`。
- Status：CANONICAL。

本项目只借鉴“宏观战略与个人角色扮演结合”这一玩法方向，不复制商业游戏的受保护表达。

禁止加入：

- 商业游戏提取的头像、地图、模型、音乐、音效、字体或 UI。
- 商业游戏的程序、反编译代码、数据库和剧本文本。
- 高度近似的标题、标志、界面布局和宣传材料。

允许并鼓励：

- 依据公共领域历史资料自行创作人物和事件数据。
- 自制或明确采用 CC0 的素材。
- MIT、BSD、Apache-2.0 等兼容依赖，但必须记录来源。

## 第三方软件与源码

“免费游玩”“公开仓库”“可以查看源代码”和“具有可再分发的开源许可证”是不同概念。
接入任何第三方源码前，必须固定原始仓库、提交号、版权所有者、SPDX 许可证和下载日期，
并区分代码、数据、文档、商标与素材的授权范围。

弱互惠许可证只能在完成兼容性审计后使用。以 Microsoft Public License（Ms-PL）为例：

- 可复制、修改和分发具有该许可证的代码；
- 分发原代码或衍生源码时，相关部分必须继续使用 Ms-PL 并附完整许可证；
- 原有版权、专利、商标和署名声明必须保留；
- Ms-PL 代码不得被误标为本项目 MIT 代码；
- 未被代码许可证覆盖的地图、美术、音乐、数据和商标不得一并导入。

《中华三国志》的专项审计与首批转换边界见
[`ZHSAN_OPEN_SOURCE_LICENSE_AND_INTEGRATION_AUDIT.md`](ZHSAN_OPEN_SOURCE_LICENSE_AND_INTEGRATION_AUDIT.md)。

每项第三方软件或源码必须记录：名称、作者/版权所有者、原始仓库、固定提交、下载或审计日期、
许可证及版本、直接复制或独立重写状态、修改说明、项目内位置和所需 NOTICE/许可证文件。

## 第三方素材登记字段

每项外部素材必须记录：

- 名称
- 作者
- 原始页面
- 下载日期
- 许可证及其版本
- 是否修改
- 在项目中的文件位置

OpenGameArt、itch.io 和 Freesound 上的资源许可证并不统一，不能仅因为“免费下载”就加入仓库。

## 项目原创 AI 辅助美术登记

| 名称 | 生成工具与日期 | 输入与修改状态 | 项目内位置 | 用途与限制 |
| --- | --- | --- | --- | --- |
| 中山城镇全景 V1 | OpenAI 内置图像生成工具，2026-08-06 | 无外部参考图；由项目原创提示词生成，未使用商业游戏素材 | `Assets/Resources/Art/Towns/zhongshan-town-overview-v1.png` | 仅作为城镇页氛围底图，不作为历史证据、地理数据或建筑世界事实；动态建筑、权限和状态继续由代码与存档叠加 |

该图提示词要求原创东汉中山城镇、绢本设色与水墨线描表达，并明确排除商业游戏的地图、
界面、构图、角色和素材复刻。后续若进行人工重绘、裁切或颜色调整，应在本表继续记录修改状态。

## MASTER-MAP-V0 外部地理源

| 名称 | 发布者与版本 | 许可/使用边界 | 项目内位置 |
| --- | --- | --- | --- |
| Natural Earth 1:10m land/coastline/lakes/rivers | Natural Earth，具体版本见来源清单 | Public Domain；只作现代物理参考，不作为汉代海岸、河道或行政边界证据 | `MapData/HanWorld_Master_V0/physical/` |
| Mapzen Terrain Tiles GeoTIFF 1.1 | Mapzen/Linux Foundation，AWS Open Data 托管 | 中国范围底层使用公开领域 SRTM/GMTED2010；保留“Mapzen；SRTM/GMTED2010 data courtesy of USGS”署名；不用于导航 | `MapData/HanWorld_Master_V0/physical/elevation_master.tif` |
| 东汉140年稳定行政与地理目录 | 本项目原创整理及已登记史料 | 复用原稳定ID与来源等级；自动空间代理不构成历史边界主张 | `Data/HistoricalPopulation/` 与母版历史/行政图层 |

逐源下载地址、日期、商业使用、再分发、原始 CRS、处理说明与 SHA-256 见
`MapData/HanWorld_Master_V0/manifest/external_sources.resolved.json`；117 个高程分块的逐文件哈希见
`MapData/HanWorld_Master_V0/manifest/elevation_tiles.resolved.json`。大型下载缓存不进入仓库。
