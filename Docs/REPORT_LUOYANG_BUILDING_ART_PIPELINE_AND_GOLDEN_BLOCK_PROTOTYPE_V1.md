# 洛阳建筑美术生产管线与黄金街区原型 V1 实施报告

## 1. 当前结果

已建立第一版 400×400m 洛阳黄金街区，并接入现有县域世界空间表现。实现会从权威布局包中
确定性选择类别丰富的 8×8 Cell UrbanArea 区块，保留其来源 FacilityId 审计链，并以 16 个
只读表现 Lot 组织住宅、市场、工坊、仓廪和官署/公共五类院落。

当前布局包确定性选址为本地行 `168—175`、列 `232—239`，块内有 4 个可追溯正式 Facility
来源。块内原始类别不足五类，因此缺少的市场、工坊或仓廪样式只生成无 `SourceFacilityId` 的
展示 Lot；它们不得被解释成该位置已经存在对应正式设施。

县域导航现提供“样板街区”入口，可直接聚焦到 Near LOD。该入口没有创建新 Facility，
也没有改变人口、库存、生产、市场、道路、时间或存档。

## 2. 已实现内容

- `CountyGoldenBlockPresentationPlan`
  - 8×8 Cell 稳定选址；
  - 16 个表现 Lot；
  - 五类 Archetype；
  - 6 个变体和四向朝向；
  - 来源 FacilityId 与稳定签名；
  - 明确 `IsDerivedPresentationOnly=true`。
- 世界空间合批表现
  - 街区地面；
  - 主巷和小巷；
  - 院墙、入口和门架；
  - 主屋、配房和屋脊；
  - 三套屋顶色阶；
  - 市场、工坊、仓院、官署差异小品；
  - 庭院树木；
  - 8—12 Renderer 预算。
- 旧聚合层
  - 选中的黄金街区不再生成原有 Far Aggregate，避免双层重叠。
- 玩家入口
  - 县域导航新增“样板街区”；
  - 点击后进入 UrbanArea、聚焦样板区并切换 Near LOD。

## 3. 验证记录

- 全工程编译：`passed`。日志：
  `tmp/skill-verification/compile-20260904-143843-140.out.log`
- 定向 Core：最终编译产物直接复跑 `passed=4 failed=0`，覆盖黄金街区确定性、县域布局不变、
  Far Aggregate 唯一覆盖和地形/聚合稳定性。安全验证入口的新增测试结果日志为
  `tmp/skill-verification/core-tests-20260904-143911-380.out.log`（`passed=1 failed=0`）。
- Unity EditMode：`blocked`。开工检查发现用户 Unity 2022.3.62f3c1 编辑器仍在运行，依照项目规则
  未关闭用户程序、未启动第二个项目实例。
- `git diff --check`：全工作区 `failed`，原因是本任务开始前已经存在的四个 P0Final FBX `.meta`
  尾随空格；本任务修改文件的定向 `git diff --check` 通过，新文件尾随空格扫描通过。

## 4. 当前边界

- 本轮是美术生产规则与可玩查看入口的原型，不是最终 PBR 美术交付。
- 新增巷道和配房是表现性城市肌理，不具备正式道路、导航、Facility、人口和产能语义。
- 54/54 现有正式资产继续作为 Facility 模型权威来源；本轮没有撤销其既有审批状态。
- 尚未把黄金街区规则推广到洛阳全部 UrbanArea。

## 5. 当前状态

`IMPLEMENTED_COMPILE_AND_TARGETED_CORE_PASSED_UNITY_BLOCKED_BY_OPEN_EDITOR`

下一步是在用户退出 Play Mode 并关闭项目编辑器后，通过安全入口运行新增 Unity EditMode，随后在
编辑器中进入 `PlayableDemo → C 县域 → 样板街区` 完成远、中、近人工审图。
