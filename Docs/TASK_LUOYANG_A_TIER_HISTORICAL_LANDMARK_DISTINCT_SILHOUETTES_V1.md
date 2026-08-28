# LUOYANG-A-TIER-HISTORICAL-LANDMARK-DISTINCT-SILHOUETTES-V1

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 1. 任务目标

在既有36类洛阳Facility基础模型和高频生产模块包之上，为184年洛阳10个A级历史设施建立按
`FacilityId`绑定的独立战略轮廓：南宫、北宫、永安宫、太学、明堂、辟雍、灵台、太仓、武库、
濯龙园。

本任务保持原Facility身份、Global Cell、基础Model ID、产权、控制权、建设权限和存档不变；新增的
是设施级Asset Variant、Silhouette、原创程序网格组合、三级LOD和直接审图入口。交付定位是可直接
摆放并可继续替换为最终FBX的程序化V1，不冒充考古单体复原或最终艺术资产。

## 2. 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/TASK_LUOYANG_184_HISTORICAL_V1.md`；
- `Docs/TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`；
- `Docs/TASK_LUOYANG_PRODUCTION_BUILDING_MODULAR_KIT_AND_HIGH_FREQUENCY_CITY_FABRIC_V1.md`；
- `Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/facilities.json`；
- 既有Facility模型目录、显式绑定目录和高频生产模块目录。

## 3. 冻结历史设施清单

| 设施 | Facility ID | 正式Cell | 基础模型 | 置信度 / 精度 | V1独立轮廓 |
|---|---|---:|---|---|---|
| 南宫 | `facility.instance.luoyang.184.south_palace` | 4,127,973 | 宫殿建筑群 | HistoricalAnchor / Probable | 双朝院轴线 |
| 北宫 | `facility.instance.luoyang.184.north_palace` | 4,098,147 | 宫殿建筑群 | HistoricalAnchor / Probable | 双阙高台 |
| 永安宫 | `facility.instance.luoyang.184.yongan_palace` | 4,101,458 | 宫殿建筑群 | HistoricalReconstruction / Approximate | 偏轴曲院与园景 |
| 太学 | `facility.instance.luoyang.184.taixue` | 4,154,491 | 太学组合 | HistoricalAnchor / Probable | 三列讲堂院 |
| 明堂 | `facility.instance.luoyang.184.mingtang` | 4,161,110 | 礼制建筑 | HistoricalAnchor / Probable | 方形重台中堂 |
| 辟雍 | `facility.instance.luoyang.184.biyong` | 4,161,116 | 礼制建筑 | HistoricalAnchor / Probable | 环水中堂 |
| 灵台 | `facility.instance.luoyang.184.lingtai` | 4,161,107 | 观象台 | HistoricalAnchor / Probable | 四级收分高台 |
| 太仓 | `facility.instance.luoyang.184.taicang` | 4,134,598 | 国家粮仓 | HistoricalAnchor / Approximate | 四廪阵列 |
| 武库 | `facility.instance.luoyang.184.arsenal` | 4,134,604 | 武库 | HistoricalAnchor / Approximate | 封闭军械围院 |
| 濯龙园 | `facility.instance.luoyang.184.zhuolong_garden` | 4,101,464 | 皇家苑囿 | HistoricalReconstruction / Approximate | 池台林苑 |

正式坐标、置信度和来源来自既有184年洛阳设施数据。表中“V1独立轮廓”是战略表现解释，不把未知
院落数量、建筑尺度、屋顶细节或考古缺口伪装为确定史实。

## 4. 冻结决策

1. 继续引用既有稳定Base Model ID，不增加普通Facility Definition，不升级Save Schema。
2. 以精确Facility ID选择地标变体；Definition级绑定仍负责普通同类设施，防止南宫/北宫/永安宫
   再次共用同一轮廓，也防止明堂/辟雍混淆。
3. 每项地标必须保存正式Cell、史料置信度、空间精度、来源ID、历史说明和受限Availability。
4. Availability只允许`Government`、`Military`、`HistoricalInit`、`Event`；本任务不授予`Player`
   或普通AI建设权。
5. LOD0保留完整院落组合，LOD1保留近景轮廓，LOD2保留世界层识别形；三层均不生成Collider。
6. 新增庑殿/攒尖式收分屋顶和环形礼制水岸原创缓存Mesh，继续复用既有东汉中原共享材质与程序网格。
7. `LANDMARKS`审图入口必须在10个正式Global Cell摆放，而非把地标挪到虚构展示格。

## 5. 实施范围

### 5.1 内容合同

- 新增`mandate.luoyang-historical-landmark-kit.v1`；
- 冻结10个Facility/Profile/Asset Variant/Silhouette及真实Cell映射；
- 校验基础Model、材质、模块占地、LOD子集、入口锚点、史料字段和受限Availability；
- 缺失或错误Facility ID、Cell、来源、轮廓或建设限制时拒绝加载。

### 5.2 运行时装配

- 工厂按`RuntimeBindingId == FacilityId`优先选择地标变体；
- 地标实例记录Profile、Asset Variant、Silhouette、Facility、置信度和空间精度；
- 非地标设施继续使用生产模块或既有36类基础模型，不改变兼容路径；
- 地标使用独立LOD0/LOD1/LOD2、放置锚点和入口锚点。

### 5.3 地图审图

- 新增独立`LANDMARKS`按钮和战略镜头；
- 镜头覆盖洛阳北宫至南郊礼制区的正式20×13 Cell范围；
- 同屏检查10个真实Cell、10个独立轮廓、零重复占格和切回WORLD后的清理行为；
- 输出1600×1000实际Unity Game View证据。

## 6. 不在范围内

- 不新增、删除或移动任何Facility；
- 不改变人口、家户、岗位、库存、产权、建设菜单、成本、工期、结算、控制权或存档；
- 不完成十二城门/宫门的身份与门楼差异化；
- 不完成室内、导航、最终碰撞、攻城损毁、废墟、Addressables或全城Streaming；
- 不宣称最终FBX、贴图烘焙、考古单体复原或全量美术生产完成；
- 不复制《三国志11》或任何商业游戏的模型、贴图、布局、图标、代码和数据。

## 7. 验收标准

1. 内容目录恰好10个唯一Facility/Profile/Asset Variant/Silhouette，且与正式设施Cell逐项相等。
2. 三座宫殿分别具有双朝院、双阙高台、曲院轮廓；明堂与辟雍分别具有方台与环水轮廓。
3. 10个运行时实例全部按Facility ID命中地标变体，具备三级LOD、锚点、历史元数据和零Collider。
4. 10个LOD0几何签名互异，非地标Runtime Binding保持兼容回退。
5. `LANDMARKS`镜头使用10个正式Global Cell，同屏模型数恰好10，切回WORLD后归零。
6. 输出1600×1000实际Unity Game View，不以概念图替代运行时证据。
7. 全工程编译、相关核心测试、目标EditMode、目标图形化PlayMode、`git diff --check`和差异审阅分别记录。

## 8. 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1/`

## 9. 下一阶段门禁

本任务通过后，下一洛阳建模任务为十二城门与宫门身份化V1：先依据既有门名、方向、Cell和宫城归属
冻结设施清单，再制作门楼、阙、城墙连接和有限瓮城差异；之后进入市场、官署、军营、学塾等中频
城市织物生产化。最终FBX替换和全城量产仍须独立性能与美术门禁。

## 10. 执行记录

- 2026-08-27：建立独立内容合同和10项设施级Profile，冻结正式Facility ID、Cell、基础Model、史料
  置信度、空间精度、来源、建设限制、Asset Variant、Silhouette、锚点和三级LOD。
- 完成73个数据定义模块、庑殿/攒尖式收分屋顶与环形礼制水岸原创缓存Mesh；三座宫殿、明堂/辟雍、
  太学、灵台、太仓、武库和濯龙园均具有互异LOD0几何签名。
- 运行时工厂按精确Facility ID选择地标变体；非地标实例继续兼容生产模块或既有基础模型。
- 新增`LANDMARKS`审图入口，在10个正式Global Cell摆放10项地标；切回WORLD后模型归零。
- 全工程编译通过；目标核心合同测试1/1、Unity EditMode 3/3、图形化PlayMode 1/1通过，输出
  1600×1000实际Game View。NuGet漏洞源当前网络不可达产生NU1900警告，既有3条CS0649警告保留，
  无编译错误。
- 沙箱内首次Unity EditMode未创建启动日志，安全运行器在45秒门限终止本任务进程；按项目验证规则
  在沙箱外重试后通过。该事件不是模型或测试断言失败。
- 当前结论为“10项A级历史地标独立程序化轮廓V1目标验收通过、可供用户审图”。全量核心/Unity
  分组回归、最终FBX/贴图、考古细部和全城性能量产仍是后续独立门禁。
