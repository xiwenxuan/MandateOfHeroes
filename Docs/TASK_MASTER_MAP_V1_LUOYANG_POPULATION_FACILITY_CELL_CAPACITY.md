# MASTER-MAP-V1：洛阳人口—Facility—Cell容量验证与统一世界原型任务书

## 1. 目标

本阶段在 MASTER-MAP-V0 的历史地理母版上建立洛阳结构性纵向切片，回答四个问题：

1. 2000 米 Cell 能否容纳永久人物、家庭、住宅、生产、公共设施、道路和军事实体；
2. 人口、劳动、居住、设施与 Cell 是否使用同一套可追溯世界事实；
3. 洛阳城市锚点、动态城市占地、洛阳—虎牢战争走廊与县级分布能否共存；
4. 若 2000 米通过，是否仍有必要重建 1000 米对照世界。

本任务不把结构原型宣称为全国设施填充、完整城市 AI、最终人口平衡或主存档正式接入。

## 2. 已执行范围

- 将候选网格改为以 2000 米网格为整数细分/聚合基准，消除各档独立 `ceil` 造成的行数错位；
- 发布版本化 `HanWorldV1`，保留正在被旧场景使用的 `HanWorldV0`；
- 建立 `GridSchemaVersion + GridX + GridY + CellId64` 合同；
- 建立一 Cell 一 Owner、一基础 Facility、最多一 Force，以及仅 Owner 可建设的不变量；
- 建立 42 种数据驱动 Facility 容量母表，覆盖住宅、农业、资源、工业、商业、服务、道路、公共与军事；
- 按 M24 的全国 50 万、100 万、200 万实际永久人物档，将史料河南尹占比投影到洛阳测试区；
- 为三档生成每个 Person 与 Household 的稳定 ID、家庭关系、居所、劳动资格、职业、技能、活动和工作地；
- 建立玩家 3 Cell、普通家族 8 Facility、豪族 120 Facility 的统一产权与委任样例；
- 建立洛阳锚点、动态城市 Footprint、虎牢节点、道路和独立 Force 事实；
- 建立 Unity 洛阳专题验证场景、人口/设施专题图、连续缩放、Cell 点击与容量警告；
- 建立 Python 全量数据审计、EditMode 不变量/查询基准和 PlayMode 场景测试。

## 3. 实测结果

|人口档|洛阳 Person|Household|Facility Cell|可开发利用率|人口翻倍投影利用率|
|---|---:|---:|---:|---:|---:|
|低档（全国 50 万）|10,271|2,231|602|10.49%|20.98%|
|推荐档（全国 100 万）|20,542|4,498|1,057|18.41%|36.83%|
|高档（全国 200 万）|41,084|8,997|1,970|34.32%|68.64%|

推荐档共有 5,980 个测试区 Cell、5,740 个可开发 Cell、1,057 个已开发 Cell，保留 4,683 个未开发可用 Cell。
三档合计写出 71,897 个永久 Person。推荐结论为 `RecommendedCellScale = 2000m`；未触发 1000 米重建。

## 4. 验收合同

- 生成器：`MapPipeline/scripts/build_luoyang_world_v1.py`；
- 数据审计：`MapPipeline/scripts/validate_luoyang_world_v1.py`；
- 一键复现：`MapPipeline/Build-LuoyangWorldV1.ps1`；
- Unity 入口：`Assets/Scenes/LuoyangWorldValidation.unity`；
- 数据包：`Assets/StreamingAssets/WorldMap/HanWorldV1` 与 `LuoyangWorldV1`；
- 完整证据：`MapData/LuoyangWorld_V1/reports/01` 至 `10`；
- Unity 查询基准：`tmp/unity-validation/cell-query-benchmark-v1.json`（EditMode 执行后生成）。

代码验收必须按项目规则依次完成全工程编译、核心测试、受控 Unity 测试、`git diff --check` 和范围审阅；
测试未产生结果文件时，不得把本任务状态写为最终通过。

## 5. 最终验收记录（2026-08-09）

- Python 全量数据审计：通过；
- 全工程编译：通过；
- 分组核心回归：524/524，通过，0 失败；
- Unity EditMode：13/13，通过，0 失败；
- Unity PlayMode：1/1，通过，0 失败；
- `git diff --check`：通过；
- 最终状态：**PASS**，2000 米为推荐 Cell 尺度，未触发 1000 米对照重建。
