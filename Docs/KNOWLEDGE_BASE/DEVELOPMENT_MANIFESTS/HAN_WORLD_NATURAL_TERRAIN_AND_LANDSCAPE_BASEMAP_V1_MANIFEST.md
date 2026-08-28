# HAN-WORLD-NATURAL-TERRAIN-AND-LANDSCAPE-BASEMAP-V1 开发清单

## 已交付

- 冻结 Global Cell 与 2km DEM 上的统一自然地表合同。
- 8×8 Cell（16km）Terrain Tile 技术索引和共享边生成规则。
- WORLD 低 LOD 与 REGION 3×3 Tile 运行时 Mesh 地形。
- 独立批处理河流带和植被 Mesh；不以逐 Cell 或逐树 GameObject 表现。
- Global → Terrain → Cell 和 Floating Origin 往返绑定。
- 无旧背景图时仍可独立显示的 Unity 场景。
- 10 张截图、12 份工作簿、实施报告和机器验收摘要。

## 冻结与暂定边界

- Terrain Tile 冻结为 8×8 Global Cell。
- Streaming Unit 暂定为 24×24 Cell / 3×3 Tile，不是永久世界身份。
- 全国 112,880 个 Tile 是可派生索引，不是预烘焙 GameObject。
- Natural Earth 河流是许可兼容的现代自然参考，不宣称为 184 年精确河道。
- 洛水缺少当前许可源中的唯一可归属折线，保留 `NOT_PROVEN_SOURCE_GAP`，不得伪造。

## 验收基线

- 全工程编译：PASS。
- 核心回归：709/709 PASS（12 组）。
- 自然地貌专项核心合同：4/4 PASS。
- Unity EditMode：4/4 PASS。
- Unity PlayMode：2/2 PASS。
- Global Origin、7,211,264 个 Cell 和永久 Cell ID：均未改变。
