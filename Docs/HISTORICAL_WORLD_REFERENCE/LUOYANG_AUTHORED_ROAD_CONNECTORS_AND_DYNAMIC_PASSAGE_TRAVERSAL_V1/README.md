# 洛阳身份化道路连接与动态门桥通行 V1 证据

任务入口：`Docs/TASK_LUOYANG_AUTHORED_ROAD_CONNECTORS_AND_DYNAMIC_PASSAGE_TRAVERSAL_V1.md`

本目录证明：

- 28 条上一阶段临时断点边已获得稳定 Connector ID、来源边、逐格折线路径与玩法重建证据标签；
- 精化层为 379 节点、402 边，其中 334 条严格道路边、28 条身份化连接、40 条门桥双侧接近边；
- 20 个门桥具有 Domain 会话态，关闭/毁坏会阻断寻路，受损会提高代价；
- CITY 仍有 549 个独立选择 Trigger，最终美术 Prefab 仍不附加 Collider；
- 截图由图形 PlayMode 生成并通过非空白像素方差断言。

视觉图例：青色为严格道路/门桥接近边，橙色为玩法重建连接，黄色为当前选择，红色叉为关闭门桥。
这些连接是格级玩法重建，不是史实精确道路。

限制：门桥状态尚未进入 WorldState 或存档迁移，不跨读档；人物尺度 NavMesh、门扇动画、守军、
权限、围城、桥梁载重、洪水和维修均未实现。

证据文件：

- `Screenshots/01_DENSE_CITY_MODELED_CONNECTORS_AND_CLOSED_GATE.png`；
- `metrics.json`；
- Unity 结果和日志路径记录在任务书第八节。
