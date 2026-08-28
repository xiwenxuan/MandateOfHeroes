# 洛阳十二城门与宫门身份化 V1 任务书

任务 ID：`LUOYANG-TWELVE-CITY-AND-PALACE-GATE-IDENTITY-V1`
状态：已完成目标验收，待用户审图
范围：洛阳 184 年历史初始化的十二座大城门与两座宫门
前置：洛阳设施模型覆盖、生产建筑模块套件、十处 A 级历史地标独立轮廓 V1

## 一、目标

把现有统一城门/宫门占位模型升级为 14 个设施级身份模型，使地图在战略视距下可以直接辨认：

- 具体是哪一座门；
- 门朝向与所连城垣轴线；
- 城门或宫门的身份层级；
- 门楼、双阙、双通道、短外院等轮廓差异；
- 权威 Facility ID、Global Cell 与史料置信度。

本任务不制作考古级复原，不修改城防规则、设施状态、存档结构或建设权限。

## 二、权威范围

权威入口为：

- `Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/facilities.json`
- `MapData/Luoyang184Historical_V1/reports/05_FORTIFICATION_MODEL_REPORT.md`
- `Docs/TASK_LUOYANG_184_HISTORICAL_V1.md`

冻结设施如下：

| 类别 | 门名 | Facility ID | Cell | Grid | 设施方向 | 空间精度 |
|---|---|---|---:|---:|---|---|
| 城门 | 广阳门 | `facility.instance.luoyang.184.gate.guangyangmen` | 4,131,278 | 2034,1246 | west | Probable |
| 城门 | 谷门 | `facility.instance.luoyang.184.gate.gumen` | 4,084,888 | 2040,1232 | north | Approximate |
| 城门 | 津门 | `facility.instance.luoyang.184.gate.jinmen` | 4,144,537 | 2037,1250 | south | Probable |
| 城门 | 开阳门 | `facility.instance.luoyang.184.gate.kaiyangmen` | 4,144,549 | 2049,1250 | south | Probable |
| 城门 | 旄门 | `facility.instance.luoyang.184.gate.maomen` | 4,131,296 | 2052,1246 | east | Approximate |
| 城门 | 平城门 | `facility.instance.luoyang.184.gate.pingchengmen` | 4,144,545 | 2045,1250 | south | Probable |
| 城门 | 上东门 | `facility.instance.luoyang.184.gate.shangdongmen` | 4,098,156 | 2052,1236 | east | Probable |
| 城门 | 上西门 | `facility.instance.luoyang.184.gate.shangximen` | 4,098,138 | 2034,1236 | west | Probable |
| 城门 | 夏门 | `facility.instance.luoyang.184.gate.xiamen` | 4,084,894 | 2046,1232 | north | Approximate |
| 城门 | 小苑门 | `facility.instance.luoyang.184.gate.xiaoyuanmen` | 4,144,541 | 2041,1250 | south | Probable |
| 城门 | 雍门 | `facility.instance.luoyang.184.gate.yongmen` | 4,114,708 | 2034,1241 | west | Probable |
| 城门 | 中东门 | `facility.instance.luoyang.184.gate.zhongdongmen` | 4,114,726 | 2052,1241 | east | Probable |
| 宫门 | 北宫南门 | `facility.instance.luoyang.184.north_palace_gate.1240.2043` | 4,111,403 | 2043,1240 | 原数据为空；视觉南向 | Approximate |
| 宫门 | 南宫北门 | `facility.instance.luoyang.184.south_palace_gate.1242.2043` | 4,118,031 | 2043,1242 | 原数据为空；视觉北向 | Approximate |

同一设施数据中还有 4 个 `facility.military.gate` 通用推荐设施。它们服务玩法重建，不是本任务的十二大城门或宫门，明确排除。

## 三、史实与表现边界

1. 十二城门的门名、方向、Facility ID、Cell、置信度和空间精度直接读取权威设施数据。
2. 两座宫门的 `gate_direction` 在权威设施数据中为空。V1 只依据“北宫南门”“南宫北门”的显示名称派生南向/北向视觉朝向；不回写 Facility，不把派生值冒充世界事实。
3. 门楼高低、双阙、双通道、短外院、植被或引道标记属于战略视距身份设计，不等同于考古立面结论。
4. 2 km Global Cell 是战略抽象。门楼为可读性放大，不代表实际占地尺度。
5. 历史身份配置只允许 `Government`、`Military`、`HistoricalInit`、`Event`；不得借用基础通用城门模型中的 `Player`/`Ai` 建造权限。

## 四、模型方案

### 4.1 数据合同

新增 `mandate.luoyang-gate-identity-kit.v1`：

- 14 个 Facility 级 Profile；
- 14 个独立 Asset Variant 与 Silhouette ID；
- 城门/宫门稳定分类 ID；
- Facility 原始方向、视觉朝向与方向依据分栏保存；
- 门外、门内双通行锚点；
- 三档 LOD 模块清单；
- 史料来源、置信度与表现边界。

### 4.2 轮廓分工

V1 使用共享汉代中原材质和程序化模块，不复制任何商业游戏资产：

- 广阳门：短外院；
- 谷门：北向单楼；
- 津门：石质引道标记；
- 开阳门：高门楼与双阙；
- 旄门：紧凑守门楼；
- 平城门：宽阔双通道；
- 上东门：双阙；
- 上西门：偏置望楼；
- 夏门：双塔；
- 小苑门：小门楼与苑囿植被标记；
- 雍门：宽门楼；
- 中东门：中型双通道；
- 北宫南门：宫门双阙；
- 南宫北门：宫门重檐礼仪轮廓。

### 4.3 方向与摆放

模型统一以本地南向为零旋转：

- south：0°；
- west：90°；
- north：180°；
- east：270°。

地图预览必须使用 14 个权威 Cell，不使用人为评审网格；每座门的运行时绑定 ID 必须等于对应 Facility ID。
为使单格建筑在覆盖十八行十八列的全城门视图中仍可阅读，预览表现层统一放大 1.65 倍；该缩放不进入模型数据、占地规则或世界事实。

## 五、实施项

- [x] 冻结 14 座门的 Facility、Cell、方向、史料与排除项。
- [x] 新增城门身份数据目录与严格领域校验。
- [x] 新增持久化读取源，不改变存档版本。
- [x] 模型工厂支持 14 个身份 Profile、三档 LOD、双通行锚点和零碰撞展示。
- [x] 地图加入权威落格预览、朝向旋转、专用相机和 `GATES` 入口。
- [x] 完成核心、EditMode、PlayMode、截图和差异验收。

## 六、验收标准

1. 数据恰好包含 12 座大城门与 2 座宫门，且不包含 4 个通用推荐门。
2. 14 个 Facility ID、Cell、Grid、原始方向与视觉朝向逐项吻合冻结表。
3. 14 个 Asset Variant、Silhouette ID 与 LOD0 几何签名互不重复。
4. 每个实例有三档 LOD、门外/门内双锚点，无表现层 Collider。
5. 地图上以四向规则旋转，所连城垣轴线正确。
6. 宫门视觉方向明确标记为名称派生，不修改权威设施数据。
7. 核心测试、目标 EditMode、目标 PlayMode 与图形截图验收通过。

## 七、不在本任务范围

- 城门开闭动画、门扇破坏、守军演出；
- 完整瓮城、冲车、投石、地道和攻城后勤；
- 城墙连续网格与门洞布尔切割；
- 修改历史设施数值或存档协议；
- 允许玩家直接建造这些历史身份门。

## 八、后续顺序

完成本任务后进入“洛阳中频城市肌理建筑 V1”：市场、客舍/商队院、学校、官署和军营等；之后进行全城建筑性能预算与 LOD/批处理验收。

## 九、验收结果（2026-08-27）

- 全工程编译：通过；项目锁定 Unity `2022.3.62f3c1` 对应程序集全部编译成功。
- 核心合同测试：1/1 通过，过滤器 `LuoyangGateIdentityKit_FreezesTwelveCityAndTwoPalaceGates`。
- EditMode：3/3 通过，过滤器 `Mandate.Tests.LuoyangGateIdentityV1Tests`。
- 图形 PlayMode：1/1 通过，过滤器 `Mandate.Tests.LuoyangGateIdentityV1PlayModeTests`。
- `git diff --check`：通过；只保留工作区既有换行提示。
- 视觉证据：`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_TWELVE_CITY_AND_PALACE_GATE_IDENTITY_V1/Screenshots/01_FOURTEEN_GATE_IDENTITIES_ON_AUTHORITATIVE_LUOYANG_CELLS.png`。

以上是目标验收，不替代全量核心/Unity回归，也不代表最终美术、完整城墙连续网格或攻城玩法完成。
