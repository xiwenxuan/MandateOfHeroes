# PlayableFormalWorldMapV1 验证摘要

- 日期：2026-09-01
- 全工程编译：PASS
- 目标 Core：2/2 PASS
- Core 方法：
  - `PlayableFormalWorldMap_PlannedRouteUsesR003WithoutMutation`
  - `PlayableFormalWorldMap_DepartureReadsFreightProgress`
- Core 日志：`tmp/skill-verification/core-tests-20260901-122909-446.out.log`
- 完整 Core 单进程尝试：超过普通 300 秒硬上限，且清理阶段出现 `taskkill Access denied`；没有明确汇总，按 BLOCKED 记录，不冒充通过。
- 差异检查：PASS
- Unity EditMode / PlayMode：均 BLOCKED，安全脚本返回稳定代码 120；检测到用户已打开的 Unity PID 21736。
- Unity 结果摘要：`tmp/unity-validation/playable-formal-world-map-v1/unity-PlayMode-20260901-122625-327.summary.json`
- 处置：没有关闭用户程序，没有启动并行 Unity，没有把环境门禁冒充测试通过。
