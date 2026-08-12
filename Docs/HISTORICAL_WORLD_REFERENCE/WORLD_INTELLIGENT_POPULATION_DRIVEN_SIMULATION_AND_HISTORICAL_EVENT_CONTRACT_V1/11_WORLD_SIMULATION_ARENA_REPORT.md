# World Simulation Arena V1

Arena 是确定性开发实验台，不是第二套世界模拟。每次运行固定 Scenario ID、World Seed、Policy Set、Duration 与 Agent State ID；它调用同一个 Signal、Policy、Validation 和世界推进入口，记录每日人口、产品数量、活动订单、在途运输、重大事件以及逐主体决策轨迹。

V1 已支持 Rule、Utility 和 Neural Adapter 比较；神经适配器只输出 Action Score，不保存或生成世界事实，不进行在线训练。测试桩支持人口增长、短缺、盈余、贸易、战争断路、洛阳事件等场景；本轮完成接口和 Smoke，平衡参数与正式训练数据仍属下一阶段。

基础性能档位为 100、1,000、1,182 个县级 Agent，另行记录事件 watcher 与订单/运输批次。此档位只验证调度结构，不代表全国5,350万人同时 HOT 的性能保证。
