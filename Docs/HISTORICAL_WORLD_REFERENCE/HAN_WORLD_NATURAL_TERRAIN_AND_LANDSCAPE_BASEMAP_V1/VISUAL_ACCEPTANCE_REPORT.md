# 自然世界地图视觉验收报告

## 结论

Unity PlayMode 已在 `HanWorldNaturalBasemap` 场景中生成 10 张 1280×720 截图。截图均由真实 `HanWorldV1/elevation.bin` 地形网格、投影河流带和批处理植被生成，没有加载洛阳旧背景图、全国背景贴图或行政色块底图。

| 证据 | 验收项 | 状态 | 结论 |
| --- | --- | --- | --- |
| `01_WORLD_NATURAL_MAP_CLEAN.png` | 全国连续自然地图 | PASS | 海陆、平原、山地与高原分区可辨识 |
| `02_NORTH_CHINA_PLAIN.png` | 华北平原 | PASS | 低起伏地表成立 |
| `03_MOUNTAIN_REGION.png` | 山地 | PASS | 真实 DEM 高差与植被批次可见 |
| `04_MAJOR_RIVER_REGION.png` | 主要河流 | PASS | 河流为投影线要素生成的带状 Mesh，不是蓝色 Cell |
| `05_FOREST_REGION.png` | 森林 | PASS | 森林由合并 Mesh 表现，无逐树 GameObject |
| `06_HENAN_YIN_NATURAL_REGION.png` | 河南尹 | PASS | Region 只改变加载范围和精度，不生成独立地图 |
| `07_LUOYANG_AREA_WITHOUT_CITY_BACKGROUND.png` | 洛阳自然地表 | PASS | 旧城市背景关闭后仍可定位并显示地形 |
| `08_TERRAIN_TILE_SEAM_CLOSEUP.png` | Tile 接缝 | PASS | 可见范围无裂缝；机器共享边误差为 0m |
| `09_CELL_OVERLAY_DEBUG.png` | Global Cell 对齐 | PASS | 调试线仅作验证层，不是地图主体 |
| `10_BACKGROUND_OFF_WORLD.png` | Background-Off | PASS | 全国自然地图独立存在 |

## 已知视觉限制

- V1 的全国高程源本身是 2km 采样，因此近距离仍能看到低多边形轮廓；这是真实分辨率上限，不应以虚构细节掩盖。
- 当前材质为统一自然色基线，尚未进入最终水墨渲染、季节、天气、雾、岸线细化和远景大气阶段。
- Natural Earth 河流属于现代自然参考层，不等于 184 年精确河道。洛水只有历史文字依据而缺少当前许可源中的可唯一归属折线，状态保留为 `NOT_PROVEN_SOURCE_GAP`；没有伪造线位。
- REGION 植被已合批，但物种、密度生态校准和碰撞仍为后续高精阶段。

## 证据目录

全部截图位于本目录的 `Screenshots/` 子目录；PlayMode XML 与安全运行摘要位于 `tmp/unity-validation/`，本地临时日志不作为正式项目内容提交。
