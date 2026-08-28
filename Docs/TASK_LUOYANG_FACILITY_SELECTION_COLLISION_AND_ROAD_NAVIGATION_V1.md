# 洛阳建筑选择、碰撞代理与道路通行图 V1 任务书

任务 ID：`LUOYANG_FACILITY_SELECTION_COLLISION_AND_ROAD_NAVIGATION_V1`

状态：`TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-08-28

范围：184年洛阳2,084项Facility的独立选择代理、最密CITY窗口碰撞触发器，以及道路—城门—桥梁
静态通行图

## 一、任务目标

承接已经完成的54/54最终资产与全城构图，把洛阳从“可以看见整座城市”推进到“可以识别并选中具体
建筑，且道路、城门与桥梁具有可查询的静态通行关系”：

- 为2,084项Facility逐项建立独立、确定性的选择范围合同；
- CITY最密24×24窗口只实例化549个轻量BoxCollider触发器；
- 射线只从独立代理取得Facility ID，不给最终美术Prefab添加Collider；
- 为359个道路格、18个城门/宫门/军门格和2个桥格建立稳定通行节点；
- 区分严格四邻接道路边、当前数据断点的临时连接边，以及桥门接入道路边；
- 提供确定性路径查询、道路叠加层和已选建筑高亮；
- 切回WORLD时清理549个触发器、叠加层和选择状态。

选择代理和通行图是由权威Facility与构图数据派生的运行时/表现合同，不成为第二套Facility、道路事实
或存档权威。

## 二、源数据审计

| 项目 | 实际数据 |
|---|---:|
| 全城Facility | 2,084 |
| 最密CITY常驻Facility | 549 |
| 道路 `facility.public.road` | 359 |
| 城门 | 12 |
| 宫门 | 2 |
| 军事门 | 4 |
| 桥 | 2 |
| 严格四邻接道路边 | 334 |
| 严格四邻接道路连通片 | 29 |
| 最大原始连通片 | 115个道路格 |

359个道路Facility当前没有可用的`network_id`分组。若只接受四邻接，整张洛阳道路图会保持29个断开的
连通片。本任务不得把这些断点静默写成史实道路，因此临时连接边必须保留单独Profile和
`Provisional=true`。

## 三、冻结合同

| 合同项 | 冻结值 |
|---|---:|
| 全城选择代理定义 | 2,084 |
| CITY运行时触发器 | 549 |
| 通行节点 | 379 |
| 严格道路边 | 334 |
| 临时道路断点连接边 | 28 |
| 城门/桥接入边 | 20 |
| 通行边合计 | 382 |
| Collider形态 | 独立BoxCollider、`isTrigger=true` |
| 最终美术Prefab Collider | 仍为0 |
| 创建Simulation SubCell | 否 |
| 修改Save Schema | 否 |

稳定Profile：

- `presentation.collision.selection-trigger.v1`；
- `navigation.edge.road-cardinal-adjacency.v1`；
- `navigation.edge.provisional-road-gap-connector.v1`；
- `navigation.edge.gate-or-bridge-to-road.v1`。

## 四、实施方案

1. Domain从2,084项构图锚点派生同Cell内选择范围，保留Facility ID、Cell、局部偏移和Definition。
2. 选择范围按道路/水渠、墙体、桥门、田地与普通建筑使用不同占地Profile；任何半轴不跨越1,000米
   Cell半宽。
3. Presentation在CITY最密窗口为549项Facility建立独立GameObject和BoxCollider触发器；不修改54项
   最终Prefab，也不让Collider参与模拟阻挡。
4. 点击/测试射线使用`Physics.RaycastAll`并只筛选带稳定Facility ID的选择代理，最近命中成为当前选择。
5. 道路严格边只连接东西南北相邻道路格；29个原始连通片使用稳定距离和Facility ID同分裁决生成
   28条临时最小连接边。
6. 18个门和2个桥按最近道路与稳定ID排序接入；路径查询按稳定邻接排序执行，不使用运行时随机数。
7. CITY显示青色通行叠加层与黄色选择边框；WORLD切换销毁独立运行时根。

## 五、验收标准

1. 2,084项选择代理及其Proxy ID、Facility ID、Cell均唯一一致。
2. CITY恰好常驻549个独立BoxCollider，全部为Trigger、尺寸有效，美术批次对象仍无Collider。
3. 通行图恰好379节点、382边；334/28/20三类边可分别审计。
4. 同一输入重复生成的代理、边和路径逐项一致。
5. 射线可命中具体Facility并显示选择高亮；道路叠加层具有非空Mesh。
6. 截图必须具有足够像素方差，Null Graphics或纯背景图不能通过证据门禁。
7. 切回WORLD后运行时代理、选择状态和交互根均清零。
8. 全工程编译、定向核心、目标EditMode、目标图形PlayMode、相关图形回归和`git diff --check`
   分别记录。

## 六、明确边界

- 本V1是战略CITY选择触发器，不是角色、车辆、投射物或攻城单位的实体碰撞体。
- 本V1是静态Cell级道路图，不是烘焙Unity NavMesh、局部步行网格或战术寻路。
- 28条临时边只修复当前数据连通性，不能描述为史实道路；后续道路细化必须逐条替换或删除。
- 城门当前按静态可通行节点处理；开闭、守卫、权限、围城、损坏和修复仍未实现。
- 桥梁当前只有静态通行资格；载重、洪水、损毁、维修和队伍宽度仍未实现。
- 不改变Facility、Global Cell、产权、建设许可、人口、AI、模拟时间或Save Schema。

## 七、实施清单

- [x] 新增2,084项选择代理Domain合同和严格校验。
- [x] 新增379节点、382边的确定性道路—门—桥通行图。
- [x] 新增549个CITY运行时独立BoxCollider触发器。
- [x] 新增射线选择、稳定Facility ID回读和选择高亮。
- [x] 新增道路叠加层和WORLD清理生命周期。
- [x] 新增核心、EditMode、PlayMode和非空白截图验收。
- [x] 建立证据目录并更新总纲、地图资源计划与任务路由。
- [x] 相关既有图形回归与最终统一验证回填。

## 八、当前执行结果

| 项目 | 结果 |
|---|---|
| 最终统一全工程编译 | 通过；`tmp/skill-verification/compile-20260828-000940-123.out.log` |
| 最终统一核心合同 | 1/1通过；`tmp/skill-verification/core-tests-20260828-000956-290.out.log` |
| 最终统一EditMode | 3/3通过；`tmp/unity-validation/unity-EditMode-20260828-000958-970.summary.json` |
| 目标图形PlayMode | 1/1通过；`tmp/unity-validation/unity-PlayMode-20260828-000447-887.summary.json` |
| 既有全城构图图形回归 | 1/1通过；`tmp/unity-validation/unity-PlayMode-20260828-000846-087.summary.json` |
| 截图像素方差 | 通过；非空白像素断言已进入PlayMode测试 |
| `git diff --check` | 通过；最终统一验证已包含 |

最初两次PlayMode误用了`-Graphics`而不是安全脚本要求的`-UseGraphics`，Unity以Null Graphics Device
输出纯背景图。该图片被新增像素方差断言拒绝，不计为图形通过；使用正确参数重跑后才形成上表正式结果。
以上均为本任务定向证据，不替代完整核心、EditMode或PlayMode套件回归。

## 九、下一步

本V1关闭的是战略视角下“选不到建筑、没有可审计道路图”的缺口。下一阶段应优先做洛阳道路数据
细化与城门/桥梁动态通行状态，把28条临时连接边逐步替换为真实路段；之后再做角色尺度局部导航、
外围农业供给与运输入城节点，不应直接把当前Cell图冒充最终NavMesh。

兼容更新（2026-08-28）：后续
`LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1` 已建立只读包装本基础图的
402边精化层。旧382边、28条`Provisional`和20条单侧接入边继续只用于本任务历史验收；当前运行时
使用28条身份化玩法重建连接和40条门桥双侧接近边，不应再把旧临时边当成当前通行口径。
