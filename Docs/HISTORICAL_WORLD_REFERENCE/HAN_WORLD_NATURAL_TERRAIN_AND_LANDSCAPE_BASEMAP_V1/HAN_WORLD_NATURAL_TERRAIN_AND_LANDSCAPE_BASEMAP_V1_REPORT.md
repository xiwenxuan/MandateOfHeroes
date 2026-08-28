# 东汉全国自然地貌统一大地图 V1 实施与验收报告

## 结论

本阶段建立了全国统一自然底图运行时原型：世界、河南尹和洛阳共用同一 `HanWorldV1` Global Cell 与 DEM；Unity 可在完全不加载旧背景图时显示全国和区域自然地形，区域 Terrain Tile 可按需生成，河流和植被为独立批处理 Visual Feature。专项 EditMode 4/4、PlayMode 2/2、自然地貌核心合同 4/4，以及完整核心分组回归 709/709 均已通过。

## 数据与来源

- DEM：`MapData/HanWorld_Master_V0/physical/elevation_master.tif`，运行时使用同源 `HanWorldV1/cells/elevation.bin`。
- 尺寸：3314×2176，分辨率 2000m，变换和全国冻结原点完全一致。
- 有效高程范围与 651,260 个 NoData/海域样本已经审计；NoData 不会作为错误山峰进入 Mesh。
- 河流：Natural Earth 10m 现代自然参考层，共生成 233 个投影 Feature；不声明为东汉河道精确复原。
- 许可证：Natural Earth 公共领域；Mapzen/USGS 来源与署名义务沿用既有正式清单。

## Terrain Tile 决策

真实 DEM 在 4×4、8×8、16×16 候选上，对华北平原、山地、主要河流和河南尹—洛阳样区执行了 3×3/5×5 驻留测试。Unity 实测中，8×8 的 3×3 窗口为 729 顶点/1152 三角形，5×5 为 2025 顶点/3200 三角形；它在更新粒度、索引数量、碰撞规模和 48/80km 驻留范围之间平衡最好。故冻结：

- Terrain Tile = 8×8 Global Cell = 16km。
- 全国派生索引 = 272×415 = 112,880 个。
- 最后一列只包含 2 列 Global Cell，仍由统一公式派生。
- 24×24 Cell / 3×3 Tile Streaming Unit 仅为 V1 暂定值，未上升为世界身份。
- 16×16 没有被沿用为 Terrain Tile；它继续只表示模拟聚合。

## 连续性与绑定

Tile 顶点高程按全局格网顶点采样：每个顶点平均其相邻有效 Cell 的源高程。因此相邻 Tile 请求的是同一个全局顶点，抽样东西/南北边的最大误差为 `0.0m`。`TerrainCellBinding` 使用冻结原点和 Cell 尺寸完成 Global↔Unity↔Global 往返；更换浮动原点只改变本地坐标，不改变 CellPermanentId。洛阳 `(670561.5475446532, 3717065.2005044892)` 仍返回 `cell.hanworld.v0.4114717`。

## 28 项回答

1. Global Origin 没有变化。
2. 7,211,264 个 Cell 没有变化。
3. 没有生成第二套 Cell。
4. Natural Basemap 由运行时 Mesh Terrain 组成，不是背景图片。
5. 关闭旧 Background 后全国地图仍存在。
6. Terrain 来自统一 Global DEM/同源运行时高程二进制。
7. 不同 Terrain Tile 共享统一全局采样边。
8. 最大边缘高程误差为 0.0m。
9. 最终 Tile 为 8×8 Cell。
10. 选择原因是 4/8/16 真实 DEM 和 3×3/5×5 Unity 实测后的粒度—开销平衡。
11. 16×16 未直接采用为 Terrain Tile，仍是模拟聚合。
12. Streaming Unit 只暂定为 24×24 Cell，尚未冻结为长期协议。
13. Terrain 和 Global Cell 使用相同原点、方向、尺寸和边界公式。
14. Terrain 拾取可返回正确 CellPermanentId。
15. Floating Origin 移动后 Cell ID 保持不变。
16. 全国主要山地已由真实高程形成高度。
17. 平原在全国和区域视角可明显识别。
18. 河谷通过低于邻域的地形分类与真实 DEM 起伏可识别，但 2km 源限制了小河谷细节。
19. 已有许可折线的主要河流成为真实 Visual Feature。
20. 河流不再依赖蓝色 Cell；Cell 水体只保留底层分类。
21. 森林通过合并 Vegetation Mesh 表现。
22. 没有生成大量独立树 GameObject；区域植被每个窗口最多一个批次对象。
23. 表面采用顶点连续颜色与共享 Mesh，未按 Cell 创建硬边色块；2km 低模轮廓仍是当前主要颗粒感。
24. 最大视觉缺陷是 2km DEM 和 V1 基础材质不足以支撑近景最终美术。
25. 河南尹 Natural Terrain 已成立，仍是全国世界的一组 Cell。
26. 洛阳 Anchor 已落到真实自然 Terrain。
27. 关闭洛阳旧 Background 后，洛阳地区仍有自然世界。
28. 已适合进入河南尹高精地形阶段，但应先补充更高分辨率许可 DEM、历史水系核对和 LOD/Streaming 压力测试。

## 工具与运行时边界

- Domain：可扩展 Surface 稳定 ID、Tile 技术索引、Cell 绑定。
- Simulation：确定性 Terrain Mesh 数据生成与共享边算法。
- Persistence：同源高程读取、自然底图配置、投影河流目录。
- Presentation：WORLD/REGION 摄像机、Terrain、河流、植被、调试网格与证据截图。
- 全国 112,880 个 Tile 是可派生索引，不是预烘焙 GameObject；WORLD 常驻 1 个低 LOD Mesh，REGION 当前常驻 3×3 Tile。

## 验证状态

机器结果以 `validation_summary.json` 为准。Unity 专项结果已有非空 XML；全量核心回归首次单进程运行超过 300 秒，依照项目规则改用 12 组稳定验证并聚合通过 709/709，未把首次超时写成通过。
