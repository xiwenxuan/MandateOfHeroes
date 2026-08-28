# LUOYANG-WHOLE-CITY-VISUAL-REVIEW-AND-REPLACEABLE-FINAL-ASSET-MANIFEST-V1

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 1. 任务目标

在洛阳2,084项开局Facility已经达到程序化视觉生产全覆盖后，执行第一次全城建筑视觉验收，建立
可被艺术家FBX/Prefab逐项替换、可由自动化校验、不会破坏稳定身份的最终资产清单V1。

本任务不继续提高“有无模型”的覆盖数字，而把运行时真正使用的54个Asset Variant冻结为替换槽位，
逐项记录来源Profile、基础Model、使用量、代表Facility、审阅分组、视觉债、优先级和目标交付物。

## 2. 权威输入

- `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`；
- `Docs/MAP_ART_RESOURCE_PLAN.md`与`Docs/LEGAL_AND_ASSETS.md`；
- `Docs/TASK_LUOYANG_BUILDING_WHOLE_CITY_PERFORMANCE_AND_BATCHING_V1.md`；
- `Docs/TASK_LUOYANG_FINAL_LOW_FREQUENCY_CIVIC_RITUAL_MEDICAL_PRODUCTION_CLOSURE_V1.md`；
- 已验收的高频、地标、门、城市肌理、基础设施、防御、资源农业和最终公共建筑内容目录；
- 2,084项全城轻量表现计划及正式Facility/Definition/Model绑定。

## 3. 真实运行资产审计

36个基础Model不是最终替换粒度。按模型工厂实际优先级解析2,084项Facility后，真正显示的是54个互异
Asset Variant：

| 来源 | 实际替换槽位 | 覆盖Facility |
|---|---:|---:|
| 高频生产模块 | 10 | 1,800 |
| 中频城市肌理 | 5 | 158 |
| 渠井桥基础设施 | 3 | 37 |
| 实名城门/宫门身份资产 | 14 | 14 |
| 普通防御资产 | 3 | 14 |
| 资源农业资产 | 4 | 26 |
| 实名历史地标资产 | 10 | 10 |
| 医疗/通用礼制/官署/公共空间 | 5 | 25 |
| 合计 | 54 | 2,084 |

防御和最终收口目录中的9个`REUSE_*`只是复用声明，不是运行时Asset Variant，因此不得登记为最终
替换槽位；14项门必须指向各自门身份资产，10项地标必须指向各自地标资产。

## 4. 视觉验收标尺

所有分值使用1—5级生产就绪度：1为程序占位，3为战略视距可读，5为最终艺术验收候选。分数是
当前项目的美术生产判断，不是考古真实性等级。

| 审阅组 | 槽位/Facility | 轮廓 | 比例 | 材质 | 变化 | 优先级 |
|---|---:|---:|---:|---:|---:|---|
| 实名历史地标 | 10/10 | 3 | 2 | 1 | 4 | P0 |
| 实名城门与宫门 | 14/14 | 3 | 2 | 1 | 3 | P0 |
| 高频城市与生产织物 | 10/1,800 | 3 | 2 | 1 | 1 | P1 |
| 中频城市肌理 | 5/158 | 3 | 2 | 1 | 2 | P2 |
| 渠井桥基础设施 | 3/37 | 3 | 2 | 1 | 2 | P2 |
| 普通防御设施 | 3/14 | 3 | 2 | 1 | 2 | P2 |
| 医疗礼制官署功能建筑 | 3/17 | 3 | 2 | 1 | 2 | P2 |
| 资源与农业场地 | 4/26 | 3 | 2 | 1 | 2 | P3 |
| 公共庭院与广场 | 2/8 | 2 | 2 | 1 | 2 | P3 |

当前共同缺口是共享平色材质、非艺术家FBX、缺少正式贴图与最终比例标定。高频组另有1,800项重复
曝光问题；地标和门虽然使用量低，但实名身份、历史辨识与玩家认知风险最高。

## 5. 冻结优先级

| 优先级 | 槽位 | 影响Facility | 决策 |
|---|---:|---:|---|
| P0 身份关键 | 24 | 24 | 先完成史料图板、独立轮廓、正式材质和艺术家LOD |
| P1 高频曝光 | 10 | 1,800 | 每类至少三套模块变化，优先解决全城重复与材质单一 |
| P2 系统可读 | 14 | 226 | 保持功能/连接/防御语义，完成街面、地形和功能道具接口 |
| P3 环境支撑 | 6 | 34 | 在P0—P2稳定后补足场地融合、地表细节和环境道具 |

## 6. 替换合同

1. 最终资产必须继续使用清单中的稳定`ModelId`、`AssetVariantId`、`SourceProfileId`和Facility绑定；
   `AssetVariantId`本身就是替换槽位，不因从程序几何换成FBX而改ID。
2. 替换不得改变Facility位置、Definition、Cell、建设权限、产权、控制权、模拟、库存或Save Schema。
3. 每项候选至少交付Prefab、FBX源、LOD0/LOD1/LOD2、材质/贴图、枢轴与入口/连接接口；高频资产
   还必须交付变化集，基础设施必须交付地形/网络接口。
4. 外部资产必须先登记作者、来源、版本、下载日期、许可证、修改状态和再分发边界；无清晰兼容
   许可证的素材不得进入候选槽位。
5. 程序化V1继续作为可运行回退，直到对应最终候选通过相同身份、LOD、足迹、零Collider要求和
   全城性能回归；本清单不把“待替换”写成“已完成最终美术”。

## 7. 实施范围

- 新增`mandate.luoyang-final-asset-review-manifest.v1`及54项机器可读清单；
- 运行时按既有工厂优先级对2,084项Facility逐项解析并拒绝漏项、错Profile、错Model、错Asset、
  错使用量或错误代表Facility；
- 为建筑实例附加只读审阅元数据，不改变其世界事实；
- 新增`ASSET QA`入口和54项PreviewOnly审阅板，按P0/P1/P2/P3分行；
- 输出全54项、P0身份24项、P1高频10项和P2/P3支撑20项四张实际Game View。

审阅板使用正式Global Cell作为PreviewOnly展示槽，但不是设施真实位置；清单中的
`RepresentativeCellId64`才是代表Facility的正式来源Cell。全城实际位置继续由2,084项正式计划验证。

## 8. 不在范围内

- 不制作或导入最终FBX、贴图、材质包、动画、室内、导航、碰撞、损毁或废墟；
- 不修改Facility、建设规则、世界模拟、Save Schema或全城空间批次语义；
- 不把视觉就绪分数当作历史置信度，也不补造未知考古尺度；
- 不复制《三国志11》或其他商业游戏的模型、贴图、布局、UI、代码或数据；
- 不宣称全量核心/Unity回归、最终平台GPU或最终艺术验收完成。

## 9. 验收标准

1. 清单恰好9个审阅组、54个互异实际Asset Variant和2,084项使用量，无`REUSE_*`伪槽位。
2. P0/P1/P2/P3分别为24/10/14/6个槽位，影响24/1,800/226/34项Facility。
3. 2,084项正式Facility按工厂优先级全部且仅解析到一个清单槽位；54个代表Facility逐项解析到
   相同Model/Asset并具备三级LOD、零Collider。
4. 替换槽位ID等于现有Asset Variant ID，稳定身份政策和许可证准入字段完整。
5. 54项审阅板使用54个唯一PreviewOnly Cell；四个固定镜头切换不改变清单和正式世界位置。
6. 输出四张1600×1000实际Game View，切回WORLD后实例与Renderer归零。
7. 全工程编译、定向核心、目标EditMode、图形PlayMode、`git diff --check`和差异审阅分别记录。

## 10. 交付目录

`Docs/HISTORICAL_WORLD_REFERENCE/LUOYANG_WHOLE_CITY_VISUAL_REVIEW_AND_REPLACEABLE_FINAL_ASSET_MANIFEST_V1/`

## 11. 下一阶段门禁

本任务后的首批南宫、明堂、广阳门、北宫南门已完成用户接受、真实FBX回读和最终激活；第二批按
最低剩余P0评审序号选取的北宫、永安宫、太学、辟雍也已由用户全部接受，并在真实Prefab/FBX门禁
通过后激活`FinalArtApproved=true`。第三批按评审序号6/7/8/9完成灵台、太仓、武库、濯龙园的原生
Prefab、真实FBX与五视图候选，随后已由用户全部接受并激活`FinalArtApproved=true`。不得自动启动
第四批或批量替换另外42个未触及槽位。

兼容说明（2026-08-27）：上述段落保留本任务完成时的历史授权边界。第四批谷门、津门、开阳门、
旄门随后已完成用户接受与最终激活；用户又明确预接受并授权开发剩余38项，当前结果见
`TASK_LUOYANG_REMAINING_38_USER_PREACCEPTED_FINAL_ASSET_COMPLETION_V1.md`。54槽位、2,084项Facility、
稳定身份和优先级合同未改变，当前最终激活进度为54/54、剩余0。

## 12. 执行记录

- 2026-08-27：完成2,084项实际资产解析审计，冻结54个替换槽位、9个审阅组和四级优先级。
- 已建立严格内容合同、加载源、全城Facility→Asset解析计划、实例审阅元数据、54项审阅板与四个
  固定镜头，并输出四张1600×1000实际Game View。
- 首轮目标EditMode发现高频生产建筑虽使用Production几何，但实例`AssetId`仍回落到基础模型资产；
  已修正工厂解析，使10项高频生产Profile直接暴露稳定`AssetVariantId`，未改变几何、Facility、
  建设权限、模拟或存档事实。
- 全工程编译通过；定向核心合同1/1通过；目标EditMode 3/3、图形PlayMode 1/1通过；受影响的全城
  批处理EditMode 3/3和图形PlayMode 1/1回归通过；`git diff --check`通过。
- 最新全城回归仍为最密549项Facility、1,669个LOD2源模块、93个Renderer/Combined Mesh、17,476
  个顶点、27.0894ms构建和94.43% Renderer降幅，冻结预算通过。
- 本轮只执行定向核心与目标/受影响Unity测试，不扩大为全量核心或全量Unity回归通过；当前54项仍
  是程序化V1和最终资产替换槽位，不是最终FBX、贴图或美术验收完成。
- 兼容更新（2026-08-27）：首批4项已由后续任务完成最终激活；第二批北宫、永安宫、太学、辟雍
  已形成项目原创Prefab、真实FBX及多角度决策板，随后由用户全部接受并完成
  `FinalArtApproved=true`激活。第三批灵台、太仓、武库、濯龙园形成项目原创Prefab、真实FBX与
  五视图候选后也已由用户全部接受并最终激活。本任务的54槽位、优先级和稳定替换合同均未改变；
  随后第四批4项及用户预接受的剩余38项也已完成原生Prefab、FBX、来源与运行时门禁；当前54项
  全部激活，未批准槽位为0。本任务最初的程序化V1状态继续作为历史审计事实保留。
