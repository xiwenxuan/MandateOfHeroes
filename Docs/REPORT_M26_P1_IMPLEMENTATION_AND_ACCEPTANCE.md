# M26-P1 商旅—家族候选实现与验收报告

## 一、结论

2026-08-06 已完成 M26-P1 的可运行候选代码、内容数据、Unity 玩家入口和定向自动验证。
当前状态不是“最终完成”：独立 20—30 分钟人工盲玩尚无证据。M26-P2 已开始把布帛迁移为
正式产品批次和商队承运容器；完整家庭订单、托管和商号仓库仍未完成。

## 二、玩家路线

正式入口：`Assets/Scenes/PlayableDemo.unity` → 播放 → “商旅—家族体验（推荐）”。

候选路线：

```text
中山商人沈衡与家庭债务
→ 自有本钱 / 中山商行垫款
→ 查看有来源、日期和可靠度的涿县布价口信
→ 按中山实时价购买六匹布
→ 与苏双沿中山—涿县道路旅行
→ 折轴车事件：停车相助 / 留守护货 / 拒绝介入
→ 到达后按涿县实时价交付，按实际数量领取佣金
→ 偿还家债 / 购置货车
→ 根据真实选择生成不同后续目标
```

拒绝、资金不足、地点不符、口粮不足与重复提交均返回原因且不结算；途中选择允许货损或受伤，
实际交付数量决定成交与佣金，路线不要求唯一“正确答案”。

## 三、实现边界

- Domain：数据驱动目标/事件定义；M26-P2 增加商品到稳定产品ID的映射、正式布帛产品和商旅
  购入/损耗/售出库存流水，旧枚举值不变。
- Simulation：目标初始化、行动可用性、筹资、采购、旅行、确定性事件、交付、家庭长期选择与
  后续目标；权威结果先写世界账，再交给表现层。
- Persistence：复用任务、人生事件、家庭债务、人物/组织钱财、关系和行程；M26-P2 升级 V65，
  V64 只补商品—产品映射并刷新内容清单，不追溯创造货物或交易。
- Presentation：主菜单推荐入口、目标/行情卡、六段行动信息、结果进度与跳过；结果 ID 防止同一
  表现重复播放，跳过不重新执行命令。
- 内容：`Assets/Resources/Content/Core/Gameplay/merchant-household-p1.json`，全部为本项目原创
  文本与玩法参数，不含商业游戏资产。

## 四、自动验证证据

### 4.0 M26-P2 正式商队货物增量

2026-08-06 增量已通过全工程编译及以下定向核心回归：M26-P2/V64迁移 4/4、原 M26-P1 7/7、
内容包一致性与旧迁移 2/2、旧交易/旅行/市场存档兼容 4/4。详细合同见
`TASK_M26_P2_STRATEGIC_WORLD_AND_CARAVAN_GAMEPLAY_INTEGRATION.md`。

Unity Editor PID 2432 在验证时占用项目；本次未擅自关闭用户程序，也未声称新的双地图和正式批次
表现已经通过 Unity EditMode。原 4.2/4.3 证据只证明当时的 P1 候选，不替代本次增量的 Unity 验收。

### 4.1 全工程编译与核心测试

命令：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1 `
  -CoreTestFilter M26P1_ -SkipUnity
```

结果：编译通过；核心测试 7/7 通过；`git diff --check` 通过。

核心覆盖：两种筹资账守恒与重复提交、目标和有限行情、采购—旅行—事件—交付—还债闭环、
确定性事件与存档往返、购车分支及后续目标。

### 4.2 Unity EditMode

安全脚本：`Tools/Run-UnityTestsSafe.ps1`，过滤器 `M26P1`。

结果：9/9 通过；摘要：
`tmp/unity-validation/unity-EditMode-20260806-161716-268.summary.json`；NUnit XML：
`tmp/unity-validation/unity-EditMode-20260806-161716-268.xml`。

除七项核心流程外，Unity 额外验证 Resources 内容加载和表现跳过/重复结果保护。

### 4.3 Unity PlayMode

过滤器：`PlayableDemo_StartsWithCameraAndDashboard`。

结果：1/1 通过；摘要：
`tmp/unity-validation/unity-PlayMode-20260806-161818-451.summary.json`；NUnit XML：
`tmp/unity-validation/unity-PlayMode-20260806-161818-451.xml`。

项目加载冒烟亦通过：
`tmp/unity-validation/unity-ProjectLoadSmoke-20260806-160531-607.summary.json`。

## 五、未完成验收与阻断

### 5.1 独立人工试玩

未执行。必须由未阅读任务书和实现代码的试玩者按
`TASK_M26_P1A_PLAYABILITY_BASELINE_AUDIT.md` 完成 20—30 分钟盲玩，记录完成时间、迷失点、
重复操作、文本跳过、失败恢复、截图/录像和 S0—S3 缺陷。开发者自测与自动测试不能替代。

### 5.2 正式非食品市场合同

没有建立 Demo 专用价格或货币账。M26-P2 后，`commodity.cloth` 仍是地方报价入口，但通过
`ProductDefinitionId` 映射到 `product.textile.plain_cloth`；采购生成家庭所有的正式批次并装入
人物承运容器，损耗和售出留下库存流水，采购/销售继续改变共享地点行情、库存、人物钱财及
交易记录。

M25 的 `FormalCountyMarketSystem` 当前要求：

- 商品已经是生产内容包中带 `product.market` 标签的产品；
- 买卖双方是县内具体家庭；
- 货物位于一致的家庭粮仓 `ProductBatch`；
- 通过 `FormalMarketOrder`、预留、托管资金和正式成交结算。

目前地方公开市场仍是匿名聚合库存，不等同于 M25 的家庭对家庭正式订单、现金托管和家庭仓库。
M26-P2 关闭的是“具体布帛批次—商队承运—城市库存回写”缺口；完整商号订单、仓库与产业竞争
仍须后续任务，完成前不得声称正式商业网络已经全部达标。

## 六、人工验收表

| 项目 | 状态 | 证据 |
| --- | --- | --- |
| 两分钟内理解人物、家庭与目标 | 待独立试玩 | 待填写 |
| 无开发观察台完成采购—旅行—事件—交付 | 待独立试玩 | 待填写 |
| 行动前理解耗时、成本与已知风险 | 待独立试玩 | 待填写 |
| 能解释至少两项世界后果 | 待独立试玩 | 待填写 |
| 拒绝/失败后继续游戏 | 自动规则已测，人工待验 | 待填写 |
| 20—30 分钟无不可跳过长等待 | 待独立试玩 | 待填写 |
| 文本无内部类型名或明显占位语 | 开发审阅通过，人工待验 | 待填写 |

人工证据和非食品正式市场适配完成、S0/S1 修复并重新执行完整适用回归后，方可将任务书状态
更新为“完成”。
