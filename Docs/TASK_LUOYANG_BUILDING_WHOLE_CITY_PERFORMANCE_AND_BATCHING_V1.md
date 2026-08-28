# 洛阳建筑全城性能预算与批处理 V1 任务书

任务 ID：`LUOYANG-BUILDING-WHOLE-CITY-PERFORMANCE-AND-BATCHING-V1`
状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`
范围：184年洛阳2,084项开局Facility的表现计划、空间合批和最密窗口压力验收
前置：设施模型覆盖、高频生产模块、A级地标、城门身份化和中频城市肌理 V1

## 一、任务目标

在不把2,084项设施展开为数千个常驻院落GameObject的前提下，建立可重复的全城建筑表现预算：

- 全城保留2,084项Facility与Model绑定的轻量计划；
- 以8×8 Global Cell作为纯Presentation空间合批单位；
- 当前24×24审查窗口只装配其实际设施的LOD2模块；
- 同一8×8批次内按材质合并Mesh，把建筑Renderer提交组限制在预算内；
- 以最密窗口验证Renderer、顶点、Mesh复用、构建耗时、视锥/遮挡粒度和清理行为。

本任务只改变Presentation。8×8不是Chunk、Region、行政、模拟、存档或Facility聚合语义；
24×24仍是当前审查窗口，不冻结为最终Streaming Unit。

## 二、真实数据审计

权威输入：

- `Luoyang184UrbanInitializationV1/facilities.json`：1,230项；
- `Luoyang184MetropolitanInitializationV1/facilities.json`：854项；
- `LuoyangFacilityModelCoverageV1/luoyang_facility_model_bindings_v1.json`：61种Definition显式模型绑定。

审计结果：

| 指标 | 结果 |
|---|---:|
| Facility | 2,084 |
| 互异Global Cell | 2,084 |
| 同Cell多Facility | 0 |
| Grid范围 | Column 2013—2104；Row 1202—1266 |
| 8×8表现批次 | 64 |
| 单批次理论/实际峰值 | 64 / 64 |
| 24×24有设施窗口 | 11 |
| 最密窗口 | Column 2040—2063；Row 1224—1247 |
| 最密窗口Facility | 549 |
| 最密窗口8×8批次 | 9 |

## 三、冻结预算

| 预算项 | 上限/下限 |
|---|---:|
| Resident Facility | ≤576；本次预期549 |
| Resident 8×8批次 | ≤9 |
| 建筑Renderer/Draw Submit上界 | ≤200 |
| 合并建筑顶点 | ≤250,000 |
| 批次构建耗时 | ≤3,000ms |
| 相对未合批模块Renderer降幅 | ≥85% |

“Draw Submit上界”按单材质、单SubMesh的建筑Renderer组计数，是建筑侧保守上界；最终GPU
Draw Call仍受渲染管线、阴影、SRP Batcher和平台影响，不把Editor统计冒充所有硬件的最终帧预算。

## 四、实施方案

1. Domain冻结预算、轻量Facility记录、8×8空间批次与24×24窗口选择不变量。
2. Persistence只读取Facility ID、Definition、Cell/Grid和显式Model绑定，不加载人物/家户重数据。
3. 模型工厂提供只读LOD2批处理模块描述，继续复用既有材质和程序化Primitive Mesh。
4. Presentation对最密窗口549项Facility按“8×8批次＋材质”合并，输出单材质单SubMesh MeshRenderer。
5. 每个合批对象保持独立Bounds并允许Unity遮挡剔除；切回WORLD时销毁合并Mesh和对象。
6. 城门、A级地标继续按Facility ID取身份化LOD2；普通设施只使用既有稳定Model ID。

## 五、范围边界

### 纳入

- 全城2,084项轻量表现索引和64个表现批次；
- 最密24×24实际窗口的549项LOD2压力场景；
- 合并Mesh生命周期、专用`BATCH`相机和实际Game View；
- 建筑侧Renderer/顶点/构建耗时和降幅指标。

### 排除

- Addressables、AssetBundle、磁盘异步加载和最终Streaming调度；
- 最终FBX/贴图、GPU Instancing管线、烘焙Occlusion数据和平台级Frame Debugger验收；
- 全城LOD0常驻、人物演出、室内、导航、碰撞、损毁和攻城；
- 修改Facility、Cell、权限、产权、结算、人口或Save Schema。

## 六、实施清单

- [x] 审计2,084项Facility、唯一Cell、64个8×8批次与最密窗口。
- [x] 新增预算合同、轻量全城计划源和严格校验。
- [x] 接入LOD2按材质合批、Mesh回收、专用相机和`BATCH`入口。
- [x] 完成核心、EditMode、图形化PlayMode、指标JSON、截图与差异验收。
- [x] 更新总纲、地图美术计划和任务路由。

## 七、验收标准

1. 全城计划恰好2,084项、61种Definition、2,084个唯一Cell，并只引用36项目录中的35种开局Model；
   `field_hospital`在目录中保留，但184年开局数据未使用。
2. 8×8表现批次恰好64个；最密24×24窗口恰好549项、9个批次。
3. 549项均使用真实Facility ID、Cell和显式Model绑定，不创建世界事实。
4. 城门/地标/高频/中频优先使用其既有LOD2，低频使用兼容模块。
5. 建筑Renderer组、顶点、构建时间和Renderer降幅满足冻结预算，且无Collider。
6. 切回WORLD后合并Mesh与建筑Renderer归零，不遗留本任务启动的Unity进程。
7. 全工程编译、相关核心、目标EditMode、图形PlayMode、`git diff --check`分别记录。

## 八、验收结果（2026-08-27）

| 指标 | 实测 | 预算 | 结果 |
|---|---:|---:|---|
| 全城Facility/表现批次 | 2,084 / 64 | 精确值 | 通过 |
| 最密窗口Facility/表现批次 | 549 / 9 | ≤576 / ≤9 | 通过 |
| 未合批LOD2模块Renderer | 1,673 | 基线 | 已记录 |
| 合批Renderer/Combined Mesh | 97 / 97 | ≤200 | 通过 |
| 互异源Mesh | 11 | 只读指标 | 已记录 |
| 合并顶点 | 17,512 | ≤250,000 | 通过 |
| 合批构建耗时 | 22.9509ms（2026-08-27 P0四件套接入后回归） | ≤3,000ms | 通过 |
| Renderer降幅 | 94.43% | ≥85% | 通过 |
| 空间遮挡标记/预算总判定 | 是 / 通过 | 必须通过 | 通过 |

验证记录：

- 全工程编译：通过；
- 相关核心合同：1/1通过；
- 目标EditMode：3/3通过；
- 图形化PlayMode：1/1通过；
- `git diff --check`：通过；
- 指标：`HISTORICAL_WORLD_REFERENCE/LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1/luoyang_building_batch_metrics_v1.json`；
- 截图：`HISTORICAL_WORLD_REFERENCE/LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1/Screenshots/01_DENSEST_549_FACILITY_BATCHED_WINDOW.png`。

本轮只执行目标测试，不把结果扩大为全量核心/Unity回归通过。`22.9509ms`是2026-08-27 P0四件套接入后最新本机Unity Editor
当前一次构建测量，不是所有硬件的稳定帧时间或平台GPU结论。

## 九、后续决策

性能门禁已经通过。后续基础设施、低频防御、资源农业和最后低频公共/礼制/医疗设施专项也已完成
目标门禁，程序化视觉生产覆盖达到2,084/2,084。下一建筑阶段转为全城视觉验收和最终资产替换
优先级清单，不再继续增加基础覆盖计数。

本次通过只证明当前Unity 2022审查环境中的LOD2空间合批预算成立，不等于最终平台性能、全城高精
模型、最终Streaming Unit或Addressables Streaming完成。
