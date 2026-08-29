# 洛阳外围供应 V1：验证摘要

## 已取得的机器证据

- 数据查询：869 Cell、854 Facility、33 Settlement、135 Agriculture Unit、22 Storage、267 Road；
  关键引用错误 0，初始化并建立 5,980 Cell 通行计划耗时 2,503 ms。
- 人口事实：正式世界已物化 400,000 Person，其中外围增量 130,000 Person、26,907 Household；
  700,000 包含式目标仍差 300,000，不把目标清单当作人物事实。
- 食品切片：统一农业收获、正式市场、真实 Person/Container、CellRoute、Gate、到货和家庭消费完成，
  `FormalFoodConservationAuditor.Difference = 0`。
- 木材切片：真实 `ResourceBody` 被采集工单扣减并生成木料批次，随后走同一市场、民运、CellRoute、
  Gate 和目的仓；木材 `产品批次 + 资源余量` 前后相等。
- 中断：Gate/Bridge 关闭等待、重开恢复、道路阻断、驮运越野改道、车辆拒绝非法越野均有核心测试。
- 边界：来源不足、承运人不可用无状态变更；目的仓满时等待，Save/Load 后扩容只结算一次。
- 确定性：城门中断场景 3/3 完整快照一致；V77→V78 顺序迁移和 V78 往返通过。
- 完整核心：固定源码指纹 `4E78A51849247D3A19DCD06DC3330E53C0A2A9A8426EB39B02887556FC7B141C`，
  12组共793/793通过、失败0；聚合文件为
  `tmp/core-test-groups/luoyang-outer-supply-v1-20260829/aggregate.json`。两项多年确定性慢测按精确名称
  使用900秒授权分类，其余保持300秒。

## Unity 环境证据

受控 EditMode 启动分别使用 45 秒和经用户授权的 120 秒启动门禁。两次均在生成 Unity 启动日志和
测试 XML 之前以 `blocked/125` 结束；只终止本任务启动的进程，没有关闭用户程序。证据位于：

- `tmp/luoyang-outer-supply/unity-editmode-presentation/`
- `tmp/luoyang-outer-supply/unity-editmode-presentation-retry/`

因此不能把 Unity 专项测试写成 PASS，也不能取得 Loaded Objects、Frame Time、GC 和截图证据。

## 尚未闭合

1. 700,000 包含式人口目标尚缺 300,000 个永久人物及其家庭/设施承载；
2. 受保护紧凑包仍是查询来源，尚未把全部 135 农业记录和实际城市需求长期编排进 V78 正式世界；
3. 豆、黍、粟旧内容 ID 尚无正式定义；
4. Unity EditMode/PlayMode 与图形性能证据受当前宿主启动环境阻塞。

结论：代码垂直切片与核心证据可保留，但当前任务整体为 `NOT ACCEPTED`。
