# 洛阳50m县域运行时布局数据包 V1 证据索引

- `layout-audit.json`：包体、源文件、Facility和网络计数及指纹。
- 正式数据包：`Assets/StreamingAssets/WorldMap/Luoyang50mCountyLayoutV1/luoyang_50m_county_layout_v1.json`。
- 确定性生成器：`Tools/Build-Luoyang50mCountyLayoutPackage.ps1`。
- Core：`tmp/skill-verification/core-tests-20260903-095254-368.out.log`，8/8通过。
- Unity：`tmp/unity-validation/unity-EditMode-20260903-094459-513.summary.json`，启动日志前阻塞。

本证据包确认的是运行时数据权威、来源可追踪和几何闭环，不确认历史精确坐标。
