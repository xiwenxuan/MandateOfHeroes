# 《中华三国志》地图渲染净室研究报告

## 架构结论

候选代码是 VS2008/XNA 3.0 时代的分程序集、插件式 2D tile 地图。地图主体不是 DEM 驱动 3D 地形：`MainMapLayer.Draw` 遍历当前显示 tile/map-tile，选择纹理与装饰纹理并用显式深度绘制；`MapLayer` 在 Normal/Routeway 两种层之间切换；`MapViewSelector` 负责地图对象选择覆盖；`MapPanel` 按 tile 坐标修改 terrain index 并加载/保存地图；`RoutewayEditor` 独立维护道路延伸、切断和方向。

值得借鉴的是“战略信息分层、只绘当前视域、地图/道路/对象编辑分离、稳定的图层优先级”，不是旧 XNA、全局状态、tile 贴图或资源组织。

## 十条净室转化原则

1. 地图事实、表现风格和交互图层分离；
2. WORLD/REGION 只改变精度和镜头，不建立第二套世界；
3. 只生成/绘制当前 LOD 与视域需要的数据；
4. 山、河、森林、平原按战略意义形成宏观面与骨架；
5. 道路与普通地表是独立语义层；
6. 地图选择覆盖不改写世界事实；
7. 编辑器、运行时渲染和领域对象保持边界；
8. 图层顺序必须稳定、可测试；
9. 固定镜头做同源 A/B 对比，禁止偷换地图；
10. 开放内容使用稳定 ID，资产和源码分别追踪许可。

不应移植：XNA/WinForms UI、全局 `GlobalVariables`、巨型类、逐 tile 贴图表、硬编码资源路径、SpriteBatch 深度魔数、旧序列化结构和任何许可证不清的内容。

## 最终 32 问

1. 是否真正 clone 到本地？否；Git 443 连接失败/重置，是当前硬阻断。
2. clone 哪个仓库？尝试 `kpxp/ZhongHuaSanGuoZhi-New-Code`，均未完成。
3. HEAD SHA？`50f00168e005f7e5d8576e5adc215b1fbe2f8fa5`。
4. 是否检查全部 branch/tag？是；通过 API 枚举 3 branch、2 tag，但不是本地 refs。
5. 身份可信度？`MEDIUM_HIGH`：README 指向官方论坛并署名团队，但不是明确官方组织仓库。
6. 许可证是否明确？候选仓库不明确；API `license=null`。另一个已审计仓库的 Ms-PL 不外推。
7. 代码和美术许可是否一致？不能认定一致，资源必须单独审计。
8. 哪些只能研究不能复制？本候选全部代码，以及所有地图、贴图、头像、UI、字体、音乐、音效和 MOD 资源。
9. 哪些地图模块最重要？`MainMapLayer/MapEditor/DixingBianjiqi/MapLayerPlugin/MapViewSelectorPlugin/RoutewayEditorPlugin`。
10. MapEditor 如何组织地图？`MapPanel` 按 tile 坐标读写 terrain index、建筑占地和显示区域，并显式 Load/Save。
11. DixingBianjiqi 如何处理地形？`MainMapLayer` 准备/筛选显示 tile、选纹理、重算目标矩形并分层绘制；它是 tile 系统，不是 DEM 3D。
12. 山如何表现？源实现主要依赖地形 tile/装饰纹理；没有发现可直接采用的 DEM 山体算法。
13. 森林如何表现？属于地形/贴图语义，未发现独立连续森林模拟；本项目改为自然地表权重驱动的连续面。
14. 河流如何表现？静态审计未确认独立河流 mesh；主要表现链仍在 tile/layer 系统。本项目保留自己的权威河流几何。
15. 道路如何表现？`RoutewayLayer` 独立于 Normal layer，`RoutewayEditor` 提供延伸、切断和方向编辑。
16. Map Layer 如何组织？`CurrentMapLayer` 在 Normal/Routeway 间切换，Draw 以稳定深度显示激活/非激活控制。
17. Zoom/View 如何实现？已确认视口、显示 tile、destination 重算和对象选择覆盖；未把 `MapViewSelector` 错写成镜头缩放器。
18. 最值得借鉴的十条原则？见本报告“十条净室转化原则”。
19. 哪些旧实现绝不能移植？见“不应移植”清单。
20. Style D 是否不只是换色 Terrain？是；新增八类 DEM/地表派生特征、两组 UV 通道、宏观融合和森林批次策略。
21. 山是否形成可读山系？原型样本中已形成连续山体与脊谷；仍待用户美术评审。
22. 森林是否形成林区而非树点？机制上是；Style D canopy batch=0，森林由连续地表权重表达。
23. 河流是否成为战略骨架？已保持跨尺度可见并增加河谷上下文；当前河岸锯齿/三角接缝仍是美术债。
24. 平原是否摆脱单一绿地毯？是；使用平原权重、宏观噪声与独立土黄绿色调。
25. WORLD 是否更像三国战略山河地图？相对 CURRENT 明显增强，但最终审美结论由用户决定。
26. REGION 是否保持真实 3D？是；仍是同一 DEM/Cell 的 3D mesh。
27. WORLD→REGION 是否连续？坐标和事实连续；LOD 重建仍是当前技术实现。
28. CITY 能否自然进入真实 Terrain？架构允许同 Global/Floating Origin 落位；本轮未建设城市。
29. 是否修改历史地理事实？否。
30. 是否复制未经许可的中三资产？否。
31. 是否自动推广全国？否。
32. 是否开始洛阳建筑？否。
