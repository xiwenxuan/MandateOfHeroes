# 洛阳 Cell Traversal V1：既有空间审计与迁移证据

## 1. 审计结论

| 分类 | 处理 | 结论 |
|---|---|---|
| REUSE | Global Cell、一个 Cell 一个 Facility 占位、Facility 身份、LocalSpace 几何、Footprint、Entrance、Road Centerline、Gate/Bridge 几何、Streaming Chunk、Unity Anchor、V77 Person Location | 保留既有世界事实与有价值的表现数据 |
| EXTEND | 四向端口、内部拓扑、移动能力、Facility Access、动态条件、人物尺度距离/成本、CellRoute | 在 Domain 增加通用数据合同和确定性规划器 |
| REFACTOR | `LuoyangHumanScaleLocalRoutePlanner`、`MovePersonCommand` 路线生成、Unity 路径/道路投影 | 跨 Cell 权威改读 CellTraversal；旧图仅供兼容和表现展开 |
| REMOVE FROM AUTHORITY | LocalNav 图对跨 Cell 路线选择的决定权 | 不删除旧数据，不再把它当第二套正式世界拓扑 |

没有重建 Person、Facility、Road、Gate、Bridge，也没有把 LocalSpace 变成 SubCell。

## 2. 正式数据覆盖

```text
Cell Profile: 5980 / 5980
Facility Profile: 2084 / 2084
Ports: 4 per Profile
Road Facility: 359
Gate-type Facility: 18
Bridge: 2
RoadRequired Facility: 18
```

`RoadRequired` 的 18 项来自已有真实正面道路关系：7 个坞堡，以及已有道路正面的仓库/官仓。
其余没有道路正面的同类设施不自动改路，保持 `Optional`：

| 类型 | 已有道路正面 | 无道路正面 | 本任务处理 |
|---|---:|---:|---|
| Commercial Warehouse | 0 | 37 | 37 项 Optional |
| Storage Warehouse | 7 | 10 | 7 项 RoadRequired，10 项 Optional |
| Public Granary | 4 | 28 | 4 项 RoadRequired，28 项 Optional |
| Storage Granary | 0 | 1 | 1 项 Optional |
| Fortified Manor | 7 | 0 | 7 项 RoadRequired |

该策略以现有正式数据为依据，既不会将道路要求硬编码成全部建筑，也不会为了通过校验虚构道路。

## 3. 通行合同

- 四向是封闭空间协议；对角接触不能构成通行；
- 相邻 Cell 必须有互为相反方向且兼容的端口；
- Straight、Corner、T、Cross、Terminal 等内部拓扑决定入端口能否连接出端口；
- 建筑入口允许到达目的地，但 `PassThroughAllowed=false` 时不能穿楼；
- 道路端口来自已有 RoadConnection；Foot 可使用允许的非道路端口，Cart/PackAnimal 不能借
  非道路地表绕过道路能力；
- Gate、Bridge、Road 和 Facility 状态使用动态正式对象条件，关闭/损毁不会改写静态拓扑；
- 距离与成本来自端口和 Cell Traversal Metric，不按战略 Cell 的 2km 尺度直接结算。

## 4. V77 兼容

V77 已能保存路段正式对象、通行条件、前后节点、LocalSpace、Cell 和厘米坐标，因此本任务无需新增
持久字段或版本迁移。新命令由 CellRoute 生成这些字段；旧 V77 命令保留原条件识别，继续安全恢复。
地图/世界数据没有改变，旧位置不会被重新随机或移动。

## 5. 性能与非物化边界

固定指纹完整回归中的实测：

```text
profiles=5980
build_ms=60
managed_delta=14006144 bytes
game_objects=0
unity_streaming_load_ms=92
unity_streaming_update_load_ms=0
unity_streaming_update_unload_ms=1
unity_streaming_objects=19
unity_streaming_meshes=9
unity_streaming_colliders=9
```

CellTraversal 是紧凑 Domain 数据；建立全国/地区格子通行数据不要求为每个 Cell 创建 GameObject。
Unity 的 3×3 Streaming 继续只物化视野附近表现对象。

## 6. 证据路径

- 核心聚合：`tmp/core-test-groups/luoyang-cell-traversal-v1-20260829/aggregate.json`
- 性能日志：`tmp/core-test-groups/luoyang-cell-traversal-v1-20260829/group-11/core-tests-group-11-chunk-3-20260829-103434-377.out.log`
- Unity EditMode：`tmp/unity-validation/unity-EditMode-20260829-104723-335.xml`
- Unity PlayMode：`tmp/unity-validation/unity-PlayMode-20260829-104753-536.xml`
