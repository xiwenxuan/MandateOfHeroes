# LUOYANG-FINAL-LOW-FREQUENCY-CIVIC-RITUAL-MEDICAL-PRODUCTION-CLOSURE-V1

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 1. 任务目标

完成184年洛阳2,084项开局Facility的最后35项视觉生产收口。在不新增、删除、移动或改写任何
Facility的前提下，复用10项已验收A级历史地标身份资产，并为9项医馆、6项通用礼制堂、4项公共
庭院、4项公共广场和2项中央官署建立5套程序化、可复用、可直接摆放的三级LOD资产，使视觉生产
覆盖由2,049/2,084达到2,084/2,084。

本交付是东汉中原半写实、中低模、战略微缩程序化V1；它不冒充考古单体复原或最终艺术家FBX。

## 2. 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/TASK_LUOYANG_FACILITY_MODEL_COVERAGE_AND_A_TIER_COMPOSITION_V1.md`；
- `Docs/TASK_LUOYANG_A_TIER_HISTORICAL_LANDMARK_DISTINCT_SILHOUETTES_V1.md`；
- `Docs/TASK_LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1.md`；
- `Docs/TASK_LUOYANG_RESOURCE_AND_AGRICULTURE_PRODUCTION_V1.md`；
- 正式Urban与Metropolitan开局Facility数据、36项基础模型目录、显式视觉绑定和地标目录。

## 3. 冻结清单

| 生产组 | Definition | 数量 | 基础Model | 生产方式 |
|---|---|---:|---|---|
| 南宫、北宫、永安宫 | `facility.government.court_hall` | 3 | 宫殿组合 | 精确Facility ID复用A级地标 |
| 明堂、辟雍 | `facility.public.ritual_hall` | 2 | 礼制堂 | 精确Facility ID复用A级地标 |
| 太学 | `facility.education.academy` | 1 | 太学组合 | 精确Facility ID复用A级地标 |
| 濯龙园 | `facility.historical.imperial_garden` | 1 | 皇家苑囿 | 精确Facility ID复用A级地标 |
| 灵台 | `facility.public.observatory` | 1 | 观象台 | 精确Facility ID复用A级地标 |
| 太仓 | `facility.storage.granary` | 1 | 国家粮仓 | 精确Facility ID复用A级地标 |
| 武库 | `facility.storage.warehouse` | 1 | 武库 | 精确Facility ID覆盖并复用A级地标 |
| 医馆 | `facility.service.clinic` | 9 | 医馆 | 药材小院程序化资产 |
| 通用礼制堂 | `facility.public.ritual_hall` | 6 | 礼制堂 | 通用台堂程序化资产 |
| 公共庭院 | `facility.public.courtyard` | 4 | 庭院广场 | 围合庭院程序化变体 |
| 公共广场 | `facility.public.plaza` | 4 | 庭院广场 | 开放广场程序化变体 |
| 中央官署 | `facility.historical.central_office` | 2 | 中央官署 | 轴院官署程序化资产 |

合计35项、11类Definition、10种基础Model、12个生产Profile，其中7个地标复用Profile覆盖10项
实名设施，5个程序化Profile覆盖25项普通设施。

## 4. 冻结决策

1. 精确Facility ID优先于Definition和Model。明堂、辟雍必须继续命中各自地标资产；同Model的6项
   通用礼制堂只能命中通用礼制资产。
2. 公共庭院与公共广场继续共用稳定基础Model ID，但以Facility Definition/Facility ID消歧为围合
   庭院和开放广场两个不同Asset Variant；未知的仅Model绑定不得猜测其中一种。
3. 地标复用保持原地标Asset Variant、轮廓、历史置信度、空间精度、来源、三级LOD和受限建设权限。
4. 新程序化资产的Availability必须逐项等于既有模型目录，不改变Player、AI、Family、Government、
   HistoricalInit或Event权限。
5. 每个新程序化资产在单Cell足迹内提供LOD0/LOD1/LOD2、放置锚点和入口锚点；三级均不生成Collider。
6. 全城批处理继续读取LOD2；静态表现不得创建医药库存、诊疗、礼制事件、官署权力、产权、建设或
   生产结算事实。

## 5. 实施范围

### 5.1 内容合同

- 新增`mandate.luoyang-final-civic-ritual-medical-production-kit.v1`；
- 冻结35个正式Facility ID、Cell、Definition、Model和12个生产Profile；
- 校验10项地标复用、25项程序化生产、建设权限、模块边界、LOD子集和零重复占格；
- 任何缺失Facility、错误Model、地标错Cell、跨Cell模块或身份冲突均拒绝加载。

### 5.2 运行时装配与全城批处理

- 模型工厂按Facility ID优先识别地标和最后收口Profile；
- 地标实例同时记录原地标合同与收口生产合同，实际几何和Asset Variant继续来自地标目录；
- 25项普通设施按Facility ID/Definition选择5个新资产；只在不歧义时允许Model级预览回退；
- 全城2,084项轻量计划和最密窗口批处理把最后35项纳入现有LOD2路径。

### 5.3 审图入口

- 新增`CIVIC`入口和四组战略镜头：35项总览、9项医馆视野、8项礼制堂分布视野、庭院/广场/中央官署视野；
- 全部预览使用正式Global Cell，不搬到虚构展示格；
- 输出1600×1000实际Unity Game View，切回WORLD后模型归零。

## 6. 不在范围内

- 不修改Save Schema、Domain世界事实、设施建设规则、成本、工期、控制权、产权或库存；
- 不把模型表现解释成新增医疗、宗庙、礼仪、行政或资源模拟；
- 不制作室内、导航、碰撞、损毁、废墟、最终FBX、贴图烘焙、Addressables或全国量产；
- 不复制《三国志11》或其他商业游戏的模型、贴图、布局、UI、数据或代码。

## 7. 验收标准

1. 目录恰好12个Profile、35个唯一Facility：10项地标复用、25项程序化生产。
2. 11类Definition数量严格为9/8/4/4/3/2/1/1/1/1/1，模型与正式绑定完全一致。
3. 明堂/辟雍与6项通用礼制堂不混淆；4项庭院与4项广场使用不同Asset Variant。
4. 10项地标保留原地标Asset Variant和LOD；25项普通设施均有三级LOD、锚点、零Collider。
5. 全城批处理仍保留2,084项计划、64个空间批次并满足既有Renderer/顶点/耗时预算。
6. 四个审图镜头使用正式Cell并分别覆盖35项总览、9项医馆、8项礼制堂和10项庭院/广场/中央官署；切回WORLD后归零。
7. 全工程编译、定向核心测试、目标EditMode、图形化PlayMode、`git diff --check`和差异审阅分别记录。

## 8. 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1/`

## 9. 下一阶段门禁

本任务通过后，洛阳2,084项开局Facility达到程序化视觉生产全覆盖。下一步不再增加基础覆盖数字，
而进入“洛阳全城建筑视觉验收与可替换最终资产清单V1”：集中审查当前程序化资产的轮廓、比例、
材质、重复度和最终FBX替换优先级；任何最终资产替换仍需保持稳定Model/Asset/Facility身份与性能预算。

## 10. 执行记录

- 2026-08-27：完成35项正式Facility审计，冻结12个Profile：7个复用Profile覆盖10项地标，5个
  程序化Profile覆盖25项普通设施；生产覆盖达到2,084/2,084。
- 完成模型工厂与全城LOD2批处理接入。明堂/辟雍继续使用各自地标Asset Variant，6项通用礼制堂
  使用独立通用轮廓；庭院和广场同Model不同Asset Variant，未知Model-only绑定不猜测。
- 全工程编译通过；相关核心合同1/1、目标EditMode 3/3、图形PlayMode 1/1通过；受影响的全城
  批处理EditMode 3/3和图形PlayMode 1/1通过。
- 最密549设施窗口为1,669个LOD2源模块、93个Renderer/Combined Mesh、17,476个顶点、27.0894ms（最新回归）
  构建、94.43% Renderer降幅，预算通过。
- 四张1600×1000实际Game View已写入证据目录，切回WORLD后实例和Renderer归零。
- `git diff --check`通过；最终差异审阅未发现本任务新增的空白错误或越界改动。
- 沙箱内Unity首次启动未创建日志，安全运行器在45秒门限终止本任务PID；按项目验证规则在沙箱外
  使用同一过滤器重试。首次目标EditMode揭示濯龙园复用Profile遗漏地标目录中的Government权限，
  修正后又揭示通用礼制堂Model-only回退仍会猜测类型；收紧模型唯一性规则后3/3通过。
- 本轮只执行任务相关定向验证，不把结果扩大为全量核心/Unity回归或最终美术验收通过。
