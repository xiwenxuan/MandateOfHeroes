# 洛阳资源与农业设施生产化 V1 任务书

任务 ID：`LUOYANG-RESOURCE-AND-AGRICULTURE-PRODUCTION-V1`
状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`
范围：184年洛阳开局9处林场、6处采石场、5处矿场与6处稻田的生产级程序化战略表现
前置：全城建筑性能预算与批处理、低频防御设施生产化 V1

## 一、任务目标

把当前35项未生产化设施中的26项资源/农业设施升级为可直接复用的程序化战略资产：

- 林场形成“人工管理林带＋木料堆场”的生产轮廓；
- 采石场形成露天分级采石台阶、切石面与石料堆轮廓；
- 矿场形成浅层平硐入口、木支护与废石堆轮廓；
- 稻田形成浅水田面、田埂分区、作物带与进水口轮廓；
- 26项全部使用正式Facility ID和Global Cell审图，并进入既有8×8空间批次＋材质合批路径。

本任务只增加静态内容合同和Presentation。它不新增资源体、储量、产量、矿脉、灌溉网络、库存、
劳作人口或结算事实，也不修改Facility、产权、控制权、建设权限与Save Schema。

## 二、真实数据与历史边界审计

| 类型 | Definition | 开局数 | 稳定Model | Asset Variant |
|---|---|---:|---|---|
| 林场 | `facility.resource.forestry` | 9 | `model.han.luoyang.resource.forestry.v1` | `HAN_LUOYANG_FORESTRY_PRODUCTION_A` |
| 采石场 | `facility.resource.quarry` | 6 | `model.han.luoyang.resource.mine_quarry.v1` | `HAN_LUOYANG_QUARRY_TERRACE_A` |
| 矿场 | `facility.resource.mine` | 5 | `model.han.luoyang.resource.mine_quarry.v1` | `HAN_LUOYANG_MINE_ADIT_A` |
| 稻田 | `facility.agriculture.rice_field` | 6 | `model.han.luoyang.agriculture.rice_field.v1` | `HAN_LUOYANG_RICE_PADDY_BUNDED_A` |
| 合计 | 4类 | 26 | 3种 | 4种互异资产 |

26项对应26个唯一Global Cell，范围为Column 2030—2060、Row 1228—1256；权限均为
Player/Ai/Family/Government/HistoricalInit。现有数据的`historical_confidence`统一为
`GameplayReconstruction`，`spatial_precision`统一为`Approximate`，`historical_class`统一为
`GeneratedForTest`，证据等级为C且没有逐项`source_ids`。因此这些Cell是项目当前权威开局位置，
不是考古定位；模型是汉代中原通用生产形态的原创玩法重建，不得称为东汉洛阳遗址复原。

矿场与采石场有意共享基础Model。生产Profile和Asset Variant必须按正式Definition/Facility解析；
仅凭共享Model且缺少运行时Definition时不得猜测为矿场或采石场，必须回退基础Model。

## 三、冻结合同

新增`mandate.luoyang-resource-agriculture-production-kit.v1`：

- 恰好4个Definition Profile、26个正式Facility ID和26个唯一Cell；
- 用量固定为林场9、采石场6、矿场5、稻田6；
- 四类均具有独立Asset Variant、Production Role、Evidence Basis、放置/入口锚点和三级LOD；
- Profile权限必须与基础Model完全一致；模块不得越过单Cell占地，LOD2必须为LOD1子集；
- 矿场/采石场共享Model但几何签名、Profile和Asset Variant必须互异；
- 生产覆盖由2,023提升至2,049/2,084，剩余35项。

## 四、表现方案

### 林场

采用规则林列、树冠、切割木料堆和简易木作棚，强调“受管理并有木料周转”的设施轮廓。树木和木料
不代表资源体、林木年龄、可采储量或一次采伐结果。

### 采石场

采用露天阶梯台面、切石面、方整石料和木质作业架。只表达采石设施类型，不声明石层种类、储量、
实际开采进度或洛阳具体采石遗址。

### 矿场

采用浅层平硐入口、木支护、暗色洞口和废石堆，避免无证据的深井、卷扬塔或工业化构筑物。只表达
矿场识别性，不声明矿种、矿脉、储量和产出。

### 稻田

采用浅水田面、外围田埂、内部田埂、作物带和简易进水口。中国国家博物馆汉代农业资料作为时代形态
参照，但“石田塘”和画像砖并非洛阳逐址证据；静态水面不代表已实现灌溉、水文或作物生长模拟。

## 五、史料与原创边界

- 中国国家博物馆《中国古代基本陈列·秦汉》：汉代铁制农具普及及水利发展背景；
- 中国国家博物馆“石田塘”：汉代水田/塘堰模型的比较形态证据，不作为洛阳定位证据；
- 中国国家博物馆“收获渔猎画像砖”：汉代水稻收获劳动与农具的比较形态证据；
- 中国国家博物馆“冶铁画像石”：汉代铁作业场景的时代语汇，仅作工具/作业氛围比较，不外推矿址；
- 项目`TASK_M23_P2`：资源体与Facility必须保持身份分离，本模型不得制造资源事实；
- 全部几何、配色和组合由项目程序化模块原创生成，不复制商业游戏或外部馆藏资产。

## 六、审图与证据

- `RESOURCES`总览：26项全部位于权威Cell；
- 林场/矿场/北部采石带：Row 1253的连续生产线；
- 南部采石场：Row 1228、Column 2030—2032的阶梯轮廓；
- 稻田：Row 1256、Column 2050与2053—2057的六块田；
- 预览只放大表现，切回WORLD后实例和Renderer归零。

## 七、实施清单

- [x] 审计26项正式Facility、唯一Cell、历史精度、权限和共享Model边界。
- [x] 新增资源/农业生产静态合同、真实计划源和严格校验。
- [x] 制作林场、采石场、矿场、稻田三级LOD与锚点。
- [x] 接入模型工厂、全城合批、真实Cell审图、相机和`RESOURCES`入口。
- [x] 完成核心、EditMode、图形化PlayMode、截图、状态和差异验收。

## 八、验收标准

1. 4个Profile用量9/6/5/6，总计26；生产覆盖恰好2,049/2,084。
2. 26个Facility ID和Cell互异，并全部解析到冻结Definition与Model。
3. 四种Asset Variant和几何签名互异；矿场/采石场共享Model但不得误解析。
4. 权限与基础Model一致，模块位于0.88 Cell占地，LOD2是LOD1子集。
5. 26项真实Cell预览无Collider，三级LOD与全城LOD2合批可用，切回WORLD后归零。
6. 四张1600×1000实际Game View写入证据目录。
7. 全工程编译、相关核心、目标EditMode、图形PlayMode和`git diff --check`分别记录。

## 九、范围外

- 资源体、矿脉、林木、储量、品质、采掘/采伐、作物生长、灌溉、库存和产出结算；
- 最终考古复原、FBX、贴图烘焙、动画、碰撞、导航、工人演出与室内；
- 修改建设权限、产权、控制权、人口、路径和存档；
- 直接扩张剩余35项低频设施。

## 十、执行顺序

1. 冻结4类Profile、Facility清单、证据边界和共享Model解析规则；
2. 写入静态JSON、Domain校验与Persistence真实计划源；
3. 接入四种三级LOD、模型工厂、合批、审图镜头和GUI；
4. 补齐核心/EditMode/PlayMode与四张实际Game View；
5. 分层验证、更新系统总纲并按剩余35项重新冻结下一任务。

## 十一、实施结果

- 新增4个Profile并冻结9/6/5/6项正式Facility；生产覆盖达到2,049/2,084；
- 林场、采石场、矿场和稻田均使用独立Asset Variant与三级LOD；共享基础Model的矿场/采石场
  只按正式Facility/Definition解析，未知绑定回退基础Model；
- 26项全部按正式Global Cell审图，无Collider，切回WORLD后实例和Renderer归零；
- 全工程编译通过；相关核心合同1/1、目标EditMode 3/3、图形PlayMode 1/1通过；
- 受影响的全城批处理EditMode 3/3和图形PlayMode 1/1回归通过；最密549设施窗口为1,673个
  LOD2源模块、95个Renderer/Combined Mesh、18,148个顶点、27.412ms构建和94.32% Renderer降幅，
  预算通过；
- 四张1600×1000实际Game View已写入证据目录，`git diff --check`通过。

本轮尝试执行全量核心时，既有全量集合在300秒硬超时内未完成，安全脚本在受限环境中不能终止
其子进程，随后仅终止本任务启动的PID；任务相关核心与Unity定向回归均有明确结果文件。受限环境
Unity首次启动未产生日志，安全入口已终止本任务PID，沙箱外同过滤器重试通过。

## 十二、下一顺序

后续“洛阳剩余低频公共、礼制与医疗设施生产化收口 V1”已经完成目标门禁：复用南宫、北宫、
永安宫、太学、明堂、辟雍、灵台、太仓、武库和濯龙园10项既有A级身份资产，并为9项诊所、6项
通用礼制堂、4项公共庭院、4项公共广场和2项中央官署建立5套程序化Profile。当前视觉生产覆盖为
2,084/2,084，但仍不得解释为最终FBX、考古复原、设施功能模拟或全量回归完成。
