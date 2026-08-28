# 洛阳低频防御设施生产化 V1 任务书

任务 ID：`LUOYANG-LOW-FREQUENCY-DEFENSE-PRODUCTION-V1`
状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`
范围：184年洛阳开局18座各类城门、7座坞堡和3座烽燧的生产级程序化战略表现
前置：城门身份化、全城建筑性能预算与批处理、基础设施生产化 V1

## 一、任务目标

把剩余89项低频设施中的28项防御设施升级为可直接复用的生产资产：

- 十二座历史城门和两座宫门继续使用既有设施级身份资产，不复制或降级为统一门楼；
- 4座通用军用门建立带寨墙、门楼和双塔的通用生产Profile；
- 7座坞堡建立完整围墙、南向门道、主厅和角楼轮廓；
- 3座烽燧建立阶梯夯土台、望台、栏杆、火盆和信号火轮廓；
- 28项全部使用正式Facility ID和Global Cell审图，并进入既有8×8空间批次＋材质合批路径。

本任务只增加静态内容合同和Presentation。它不修改Facility、建设权限、城防数值、守军、战斗、
损毁、物资结算、人口或Save Schema。

## 二、真实数据审计

| 类型 | Definition | 开局数 | 稳定Model | 生产方式 |
|---|---|---:|---|---|
| 历史城门 | `facility.fortification.city_gate` | 12 | `model.han.buildable.city_gate.segment.v1` | 复用12项身份资产 |
| 宫门 | `facility.fortification.palace_gate` | 2 | `model.han.luoyang.fortification.palace_gate.v1` | 复用2项身份资产 |
| 通用军用门 | `facility.military.gate` | 4 | `model.han.buildable.city_gate.segment.v1` | 新增通用防御Profile |
| 坞堡 | `facility.military.fortified_manor` | 7 | `model.han.luoyang.military.fortified_manor.v1` | 新增坞堡Profile |
| 烽燧 | `facility.military.beacon` | 3 | `model.han.luoyang.military.beacon.v1` | 新增烽燧Profile |
| 合计 | 5类 | 28 | 4种 | 14复用＋14新增程序化实例 |

28项对应28个唯一Global Cell，范围为Column 2025—2065、Row 1216—1250。十二城门方向读取Facility；
两座宫门继续使用名称派生方向；4座通用军用门原数据没有方向，本任务只以
`presentation.default_south.unoriented_facility`作为审图默认南向，不回写世界事实。坞堡和烽燧
不建立虚构朝向事实。

## 三、冻结合同

新增`mandate.luoyang-low-frequency-defense-production-kit.v1`：

- 恰好5个Definition Profile、28个正式Facility ID和28个唯一Cell；
- 两个Identity Reuse Profile必须完整覆盖既有14个城门身份Profile；
- 三个Procedural Profile必须与基础Model权限完全一致，并具有独立Asset Variant、Defense Role、
  Facing Policy、放置/入口锚点和三级LOD；
- 复用Profile不重复保存门楼几何；实际实例继续使用各自身份Asset Variant和LOD；
- 模块不得越过基础Model单Cell占地，LOD2必须为LOD1子集；
- 生产覆盖由1,995提升至2,023/2,084，剩余61项。

## 四、表现方案

### 通用军用门

采用低矮寨墙、双守门塔、门楼、灰瓦屋顶和短木栅翼，区别于十二座有名都城门。基础Model仍保留
Player/Ai/Government/Military/HistoricalInit/Event权限，新Profile不得扩大该集合。

### 坞堡

采用闭合夯土围墙、南向门道、主厅和双角楼，形成家族/军政复用的防御庄园轮廓。入口锚点只用于
表现和未来接入，不声明现有道路或守军。

### 烽燧

采用分级夯土台、顶部望台、木栏、金属火盆和信号火。信号火为静态审图构件，不代表当前已点燃、
发现敌情或建立烽火传播模拟。

## 五、审图与证据

- `DEFENSE`总览：28项全部位于权威Cell；
- 坞堡/通用门细节：Row 1223的7座坞堡和4座通用门；
- 烽燧细节：Row 1216、Column 2064—2065的两座相邻烽燧；
- 预览只放大表现，切回WORLD后实例和Renderer归零。

## 六、实施清单

- [x] 审计28项真实Facility、唯一Cell、方向、权限和身份复用边界。
- [x] 新增防御生产静态合同、真实计划源和严格校验。
- [x] 制作通用军用门、坞堡、烽燧三级LOD与锚点。
- [x] 接入模型工厂、全城合批、真实Cell审图、相机和`DEFENSE`入口。
- [x] 完成核心、EditMode、图形化PlayMode、截图、状态和差异验收。

## 七、验收标准

1. 5个Profile用量12/2/4/7/3，总计28；生产覆盖恰好2,023/2,084。
2. 28个Facility ID和Cell互异，并全部解析到冻结Definition与Model。
3. 14项有名城门继续使用14个互异身份Asset Variant；不得被通用门Profile覆盖。
4. 3个新增程序化Profile权限与基础Model完全一致，几何、角色、锚点和LOD签名互异。
5. 4座无方向通用门只使用显式表现默认方向；不修改Facility数据。
6. 28项真实Cell预览无Collider，三级LOD与全城LOD2合批可用，切回WORLD后归零。
7. 全工程编译、相关核心、目标EditMode、图形PlayMode和`git diff --check`分别记录。

## 八、范围外

- 城门开闭、攻城、守军、视野、烽火传播、损毁、维修和路径规则；
- 最终考古复原、FBX、贴图烘焙、动画、碰撞、导航与室内；
- 修改权限、产权、控制权、库存、结算、人口或存档；
- 直接扩张剩余61项低频设施。

## 九、实施结果

- 新增5个Profile并冻结12/2/4/7/3项正式Facility；生产覆盖达到2,023/2,084；
- 12座历史城门和2座宫门继续使用既有14项身份资产，4座通用军用门、7座坞堡和3座烽燧
  使用3套新增程序化三级LOD；
- 28项全部按正式Global Cell审图；4座无方向通用军门仅采用显式Presentation默认南向；
- 全工程编译通过；相关核心测试1/1、目标EditMode 3/3、图形PlayMode 1/1通过；
- 接入后的全城最密549设施窗口为1,674个LOD2源模块、95个Renderer、18,228个顶点、
  28.1913ms构建和94.32% Renderer降幅，预算通过；
- 三张1600×1000实际Game View已写入证据目录；`git diff --check`通过。

本次只执行本任务相关核心与Unity测试以及受影响的全城批处理定向回归，不扩大为全量核心/Unity
回归通过。两次受限环境Unity启动没有生成日志，安全入口均只终止本任务PID；相同过滤器随后在
沙箱外受控Unity环境通过。

## 十、下一顺序

剩余61项重新审计为15类Definition。下一任务冻结为“洛阳资源与农业设施生产化 V1”：
`facility.resource.forestry` 9项、`facility.resource.quarry` 6项、`facility.resource.mine` 5项和
`facility.agriculture.rice_field` 6项，共26项；完成后生产覆盖应提升至2,049/2,084，剩余35项。
下一任务必须先审计26个正式Cell、资源权限和稻田/林场/采掘设施的地形表现边界，不得把表现
模型解释为已实现产出、储量、采掘、灌溉或库存模拟。
