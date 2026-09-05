# 任务书：洛阳全城建筑语言与 Presentation LOD 推广 V1

任务 ID：`LUOYANG_CITYWIDE_BUILDING_LANGUAGE_AND_PRESENTATION_LOD_ROLLOUT_V1`

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

日期：2026-09-05

## 一、任务目标

把 Golden Block V2 已建立的住宅、市场、工坊、仓廪、官署五套
`BuildingPresentationProfile` 推广到洛阳县域正式 Facility 表现，使县域从技术点阵和同质方盒升级为
可辨认的汉代城市建筑群，同时继续使用已有 54 类正式模型资产和 Far/Mid/Near 分层。

本任务是只读 Presentation 推广，不把 Golden Block 的 16 个表现 Lot 复制为世界实体，也不把
通用院落覆盖到宫殿、城门和其他具名地标。

## 二、不可变边界

- 512km²、320×640 个 50m `PlanningCell`、204,800 个 Cell 和 2,084 项 Facility 不变。
- 不新增、删除或移动 Facility、Road、Water、Fortification、Entrance 或 Footprint。
- 不改变人口、永久人物、家户、库存、生产、市场、权限、所有权、日期、行政归属或世界结算。
- World Schema 保持 V79，不新增存档字段。
- `Facility != Cell`；不引入 5m/10m 微型权威格网。
- 54 类正式模型、地标和城门身份继续走现有模型解析链路；通用建筑语言只是环境表现。
- 不复制《三国志11》《城市：天际线》或其他商业游戏资产。

## 三、数据合同

新增 `CountyCitywideBuildingLanguagePlan`：

1. 读取同一份洛阳 50m 正式布局和声明指纹；
2. 对非道路、非城防、非农业的 1,056 项建筑型 Facility 做稳定分类；
3. 其中 158 项 Major Facility 标记为“保留正式模型身份”，不由通用院落覆盖；
4. 其余 898 项普通建筑生成只读上下文计划；Golden Block 8×8 范围内的 3 项普通建筑由样板街区接管，
   因而全城 Mid 通用层实际绘制 895 项；
5. 每项记录正式 FacilityId、ProfileId、稳定模块变体和是否保留正式身份；
6. 同一布局、FacilityId、Profile 与旋转必须得到相同签名。

五族定义补齐洛阳现有 Definition：

- 住宅：城市里坊、乡村聚落、家族庄园、历史里坊；
- 市场：市场、商铺群、客舍、商旅院、驿站；
- 工坊：工业与资源加工设施；
- 仓廪：仓库、仓廪、商业货栈和公仓；
- 官署：政府、公共、礼制、教育、军事，以及学校和医馆。

## 四、三层表现

### Far

- 保持 606 个 8×8 聚合单元和 158 项正式地标的既有预算；
- 聚合建筑不再统一使用同一种方盒，而是读取五族 Profile 的主建筑比例、密度和屋顶形状；
- 住宅双坡、市场棚檐、工坊低坡、仓廪长脊、官署四坡在轮廓上可区分。

### Mid

- 普通 Far 聚合层关闭，启用全城五族共享 Mesh 环境层；
- 895 项普通建筑按真实 50m 位置、Footprint、旋转和高度生成院落、台基、墙、门、主辅屋、屋顶、
  小品与庭树；
- 合并为最多 12 个共享 Renderer，不创建逐 Facility GameObject、Collider、Animator 或 Update；
- 158 项具名/重大设施继续使用现有正式模型批处理；Golden Block 继续作为局部美术基准。

### Near

- 继续使用现有 54 类正式模型和最多 96 项局部详细对象；
- 不叠加全城通用建筑体，避免与正式模型重合；
- 建设 Ghost 与 Draft 继续复用相同 Profile，且只存在于规划会话。

## 五、性能预算

| 项目 | 预算 |
|---|---:|
| Mid 通用建筑 Facility | 895 |
| 共享 Renderer / Material | 8—12 |
| 合并三角形 | <600,000 |
| 逐 Facility GameObject | 0 |
| Collider / Animator | 0 |
| Near 正式详细对象 | ≤96 |

## 六、验收顺序

1. 全工程编译；
2. Core：1,056 项唯一覆盖、五族均非空、158 项正式身份保留、稳定签名和世界不变；
3. Unity EditMode：895 项上下文、8—12 Renderer/Material、三角预算、无 Collider/逐项对象；
4. Unity PlayMode：`PlayableDemo → C 县域 → 洛阳城区/Golden Block Mid`，确认通用建筑层启用、
   Far 聚合层关闭、2,084 项 Facility 与 Schema V79 不变；
5. `git diff --check` 与范围审阅。

## 七、当前执行结果

- 全工程编译：通过；日志
  `tmp/skill-verification/compile-20260905-093326-922.out.log`。
- 定向 Core：2/2 通过；日志
  `tmp/skill-verification/core-tests-20260905-093343-561.out.log`。
- Unity EditMode：精确目标 1/1 通过；汇总：
  `tmp/unity-citywide-building-language-v1-final-editmode/unity-EditMode-20260905-093624-548.summary.json`。
- Unity PlayMode：图形化精确目标 1/1 通过；汇总：
  `tmp/unity-citywide-building-language-v1-final-playmode/unity-PlayMode-20260905-093935-432.summary.json`。
- Unity 首次沙箱内 EditMode 启动未在 45 秒内生成日志；安全入口清理了本次启动的 PID 38376，随后按规则在
  沙箱外唯一重试并通过，没有关闭用户程序。
- `git diff --check`：当前仍被本任务之外既有的四个 P0Final FBX `.meta` 尾随空格阻塞。

目标自动验证已经通过，但在用户完成实际县域视图审图前，不得写为 `ACCEPTED` 或最终美术完成。

## 八、非目标

- 最终 PBR、考古级复原、室内、人物导航、损毁与攻城动画；
- 正式材料、钱粮、工期、施工队、拆除或 AI 建设；
- 重新生成 54 类正式资产或改变其 Facility 绑定；
- 修改未加载县域结算、人口规模或世界模拟精度。
