# 洛阳水渠、水井与桥梁基础设施模型生产化 V1 任务书

任务 ID：`LUOYANG-CANAL-WELL-BRIDGE-INFRASTRUCTURE-PRODUCTION-V1`
状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`
范围：184年洛阳开局19项水渠、16项水井和2项桥梁的生产级程序化战略表现
前置：设施模型覆盖、中频城市肌理和全城建筑性能预算与批处理 V1

## 一、任务目标

把既有基础覆盖模型升级为三类可直接复用的原创程序化基础设施资产：

- 水渠按真实相邻Cell形成连续水面、渠岸、压顶和检修步道；
- 水井形成井台、井圈、井架、汲水构件和遮棚，而非普通院落；
- 桥梁形成水道、桥台、桥面、桥墩和栏杆，并保留水路/道路双向连接锚点；
- 37项审图全部使用正式Facility ID和Global Cell，不伪造展示位置；
- 三级LOD继续接入全城8×8“空间批次＋材质”合批路径。

本任务只升级静态内容数据与Presentation。Facility、产权、建设权限、材料结算、人口和Save Schema
保持不变；连接推导只用于表现，不成为新的水利模拟或道路通行事实。

## 二、真实数据审计

权威输入：

- `Luoyang184UrbanInitializationV1/facilities.json`；
- `Luoyang184MetropolitanInitializationV1/facilities.json`；
- `LuoyangFacilityModelCoverageV1/luoyang_facility_model_bindings_v1.json`。

| 类型 | Definition | 开局数 | 稳定Model |
|---|---|---:|---|
| 水渠 | `facility.public.canal` | 19 | `model.han.luoyang.public.canal.v1` |
| 水井 | `facility.public.well` | 16 | `model.han.luoyang.public.well.v1` |
| 桥梁 | `facility.public.bridge` | 2 | `model.han.luoyang.public.bridge.v1` |
| 合计 | 3种 | 37 | 3种 |

数据边界为Column 2018—2079、Row 1206—1266，37项对应37个唯一Global Cell。19项水渠与2项桥梁
按既有四邻接Cell形成两条水系，共4个端点和17个直线内部节点；当前正式数据没有转角、三通、四通
或孤立水系节点。该拓扑由坐标确定性派生，不写回Facility。

## 三、冻结合同

新增静态内容合同 `mandate.luoyang-infrastructure-production-kit.v1`：

- 三个Profile保持既有Model ID与Availability集合；
- 每个Profile具有独立Asset Variant、Infrastructure Role、Alignment Mode、连接/服务锚点与三级LOD；
- 水渠和桥梁按东/西/南/北四邻接掩码派生端点/直线/转角/三通/四通开放稳定ID；
- 当前两条真实水系均按东—西轴对齐，水井保持点设施；
- LOD2必须是LOD1子集，模块不得越过基础Model声明的单Cell占地；
- 高频＋中频＋本任务生产Profile覆盖由1,958提升为1,995/2,084，剩余89项仍为低频生产缺口。

## 四、表现方案

### 水渠

低矮连续渠床和水面延伸到Cell东西边界；石/夯土渠岸、压顶与检修步道保持战略视距可读性。
当前17格主渠和桥渠4格支段按真实坐标连成两条水平水系。

### 水井

井台和井圈使用环形/圆形轮廓，双柱、横梁、汲水轴、绳桶和遮棚建立独立剪影。其连接锚点是汲水
服务点和步行接近点，不把分散井位连成水渠。

### 桥梁

水面沿东西方向衔接相邻水渠；桥面跨南北方向，配置桥台、桥墩与栏杆。水道连接锚点与道路连接
锚点分开，避免把桥面方向误写成水流方向。

## 五、审图与证据

- `INFRA`总览：37项全部位于权威Cell；
- 主渠细节：Row 1227、Column 2029—2045的17格连续水渠；
- 桥渠细节：Row 1254、Column 2053—2056的2桥＋2渠连续段；
- 预览只放大表现，不改变占地或世界事实；切回WORLD后模型归零。

## 六、实施清单

- [x] 审计37项真实Facility、唯一Cell、权限和水系拓扑。
- [x] 新增基础设施静态合同、真实计划源和严格校验。
- [x] 制作水渠、水井、桥梁三级LOD与连接锚点。
- [x] 接入模型工厂、全城合批旋转、真实Cell审图和`INFRA`入口。
- [x] 完成核心、EditMode、图形化PlayMode、三张截图、状态和差异验收。

## 七、验收标准

1. Profile恰好3项，用量19/16/2，总计37；生产覆盖恰好1,995/2,084。
2. 37项Facility ID和Cell互异，并全部解析到预期Model。
3. 水系恰好2个连通分量、4个端点、17个直线节点；16口井为点设施。
4. Profile权限与基础Model完全相同；普通内容增加不改变Save Schema。
5. 三类Asset Variant、角色、锚点和LOD0几何签名互异；LOD2可进入既有全城合批。
6. 真实Cell预览生成37个实例、无Collider，切回WORLD后归零。
7. 全工程编译、相关核心、目标EditMode、图形PlayMode、`git diff --check`分别记录。

## 八、范围外

- 水流量、灌溉收益、桥梁通行、道路寻路、损坏/维修等模拟规则；
- 最终考古复原、FBX、贴图烘焙、粒子水流、动画、碰撞和导航；
- Addressables、最终Streaming Unit、平台GPU与烘焙Occlusion验收；
- 修改37项Facility、权限、产权、库存、结算、人口或存档。

## 九、执行与验收结果（2026-08-27）

- 全工程编译：通过；
- 相关核心合同：1/1通过；
- 目标EditMode：3/3通过；
- 图形化PlayMode：1/1通过；
- 静态/运行审计：3个Profile、19/16/2项真实Facility、37个唯一Cell、2条水系、4个端点、
  17个直线内部节点和16个离散井位均符合冻结合同；
- 全城生产Profile覆盖：1,995/2,084，剩余89项；
- `git diff --check`：通过；
- 三张1600×1000实际Game View已写入
  `HISTORICAL_WORLD_REFERENCE/LUOYANG_CANAL_WELL_BRIDGE_INFRASTRUCTURE_PRODUCTION_V1/Screenshots/`。

审图中移除了逐Cell重复闸架，并把水渠检修步道改为连续低矮地形构件，消除了主渠上的横向栅格
干扰；这只是原创程序化战略表现修正，不改变Facility、水系模拟或世界坐标。本次只执行本任务
定向核心与Unity测试，不能扩大为全量核心/Unity回归通过。首次受限环境EditMode启动没有生成日志，
安全入口只终止本任务PID；相同过滤器随后在正式Unity环境通过。

## 十、下一顺序

后续“洛阳低频防御设施生产化 V1”已经完成目标门禁，生产覆盖由1,995提升至2,023/2,084。
剩余61项已审计为15类Definition；下一任务冻结为9项林场、6项采石场、5项矿山和6项稻田组成的
“洛阳资源与农业设施生产化 V1”，不在本任务内扩张模型范围。
