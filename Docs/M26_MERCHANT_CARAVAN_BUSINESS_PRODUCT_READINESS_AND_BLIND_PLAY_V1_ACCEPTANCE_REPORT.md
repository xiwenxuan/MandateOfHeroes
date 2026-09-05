# M26 商旅 / 商号玩法收口与独立盲玩 V1 验收报告

## 结论

`NOT ACCEPTED / BLOCKED`。

代码、编译、Core、Unity EditMode 与目标 PlayMode 自动化基线已经通过；Gate F 的中山—涿县主路线现已接入全国 `R003`，并复用正式 `RouteState/JourneyState/CivilianFreight/CellRoute` 权威链。任务书要求的合格独立玩家 20—30 分钟盲玩仍未执行，不能由 Codex 自行宣告通过。

## 版本基线

- 分支：`codex/m23-p4-quality-artisan-growth`
- 基线提交：`940c4381da4cbb893c0882fd28e68914397af897`
- Final Commit：`NONE`
- Unity：`2022.3.62f3c1`
- Save：V79；living-world checkpoint：v8
- World Rules：1；core content：11.1.0；Han Food：2.1.0

## 已实现范围

普通玩家入口能够建立真实商户家户与商号成员/职位，读取真实市场报价、容量、路线风险、运输成本和库存；买货、启程、事件选择、到货、出售、净利润与长期选择均落到正式世界账本。启程以守恒调度事务接管玩家自有货物，全国 R003 生成四向 CellRoute，途中损失和到站出售引用同一正式货运单。失败路径保持世界可继续，Save/Load 和三次重放保持确定性。详情见 `Docs/Evidence/M26MerchantProductReadinessV1/`。

## 自动化验收

- 全工程编译：PASS。
- Core 冻结聚合：883/883，0 失败；源码指纹 `774FF1E1D07730691503713F89A76FBECCAF2CB0987F0118D67400E1C88096EE`；结果为 `tmp/core-test-groups/m26-gatef-final3-20260901/aggregate.json`。
- Unity EditMode 冻结聚合：1087/1087，0 失败；结果为 `tmp/unity-editmode-groups/m26product-unity-editmode-final3-g32-20260831/aggregate.json`。
- 最终 Gate F / 玩家文案 Unity EditMode 专项：4/4，0 失败；结果为 `tmp/unity-tests/m26-gate-f-final3-editmode-20260901/unity-EditMode-20260901-114458-781.summary.json`。
- M26 目标 PlayMode：1/1，测试用例 0.527 秒；结果为 `tmp/unity-tests/m26-gate-f-final3-playmode-20260901/unity-PlayMode-20260901-114600-574.summary.json`。
- ProjectLoad：沙箱外 8.195 秒通过；沙箱内 code 125 属已确认启动边界。
- P0Batch4 来源清单：56/56，漂移 0。

## 人工盲玩门禁

- 状态：`NOT_RUN`
- testerQualified：`false`
- 时长：N/A
- S0 / S1 / S2 / S3：`UNKNOWN / NOT RUN`
- 证据包：`Docs/Evidence/M26MerchantProductReadinessV1/blind-play/`

不得把自动化 PlayMode 解释为独立玩家盲玩，也不得填写虚构的帧率、缺陷或观察结论。

## 剩余阻塞

1. 由未参与开发、未阅读内部实现说明的合格玩家完成 20—30 分钟独立盲玩并填写时间线、缺陷和证据索引。
Gate F 自动化整改已闭合，不再是剩余阻塞。涿县端点仍明确标为低置信临时玩法代理，不能据此宣称精确历史县城坐标。

最终 `git diff --check` 与范围审阅已经通过。完整 Core 分组 5 与 26 首次只因脚本 240 秒默认值被安全终止，分别在 300 秒普通门禁内复验通过；没有产品断言失败。当前未获得提交或推送授权，因此 Final Commit 继续记录为 `NONE`；这不改变上述产品验收阻塞结论。
