# 洛阳人物尺度近景地图与局部导航 V1：证据摘要

## 1. 固定输入

```text
Baseline: e0ab8740d33763d5bb88fd1414a2e224ed8200c3
Branch: codex/m23-p4-quality-artisan-growth
Unity: 2022.3.62f3c1
Save schema: V77
Map version: luoyang.local-map.master.v1
Map SHA-256: 894004b3bd1b09acba46c753e09efdd0d6b91303b2ab1489e44857c1da8f2b18
```

## 2. Spatial / Facility Audit

| 指标 | 结果 |
|---|---:|
| LocalSpace | 5,980 |
| Facility Capability | 2,084 / 2,084 |
| Facility Footprint | 2,084 / 2,084 |
| Access Required / Valid | 1,560 / 1,560 |
| Access Point | 1,580 |
| Blocking Geometry Required / Valid | 1,156 / 1,156 |
| Road Facility | 359 / 359 |
| Gate-type Facility | 18 / 18 |
| 主要历史城门 | 12 / 12 |
| Bridge | 2 / 2 |
| Navigation Node | 1,959 |
| Navigation Edge | 1,976 |
| Cross-cell Transition | 4,920 |
| 无合法局部表达而被明确拒绝的战略边 | 6 |
| Critical Invalid / NaN / Infinity | 0 / 0 / 0 |

被拒绝的 6 条战略边没有被静默放行；它们作为确定性审计集合进入地图摘要。

## 3. 自动验证

| 验证 | 结果 | 临时证据 |
|---|---|---|
| Full Compile | PASS | `tmp/core-test-groups/luoyang-local-v1-final2-20260829/compile-*.log` |
| Targeted Core | 19/19 PASS | 最终766项固定清单中的19项 `LuoyangLocalMap*` 测试 |
| Full Core Regression | 766/766 PASS | `tmp/core-test-groups/luoyang-local-v1-final2-20260829/aggregate.json` |
| Introduced Core Regression | 0 | 同上 |
| git diff --check | PASS | 最终验证控制台结果 |
| Unity EditMode | BLOCKED / NOT RUN | `tmp/unity-validation/unity-EditMode-20260829-052440-179.summary.json` |
| Unity PlayMode | BLOCKED / NOT RUN | `tmp/unity-validation/unity-PlayMode-20260829-052539-572.summary.json` |

EditMode 与 PlayMode 均在没有既有 Unity/Hub 进程的条件下启动。Unity 分别取得任务 PID 45776、
48852，但 45 秒内没有创建非空启动日志或 XML；安全入口只终止本次拥有的进程并返回
`blocked/125`。因此这不是测试失败，也不是 Unity PASS。

## 4. Domain 性能采样

采样在同一已编译程序集和正式 WorldMap 输入上执行，不替代 Unity 帧性能证据。

| 指标 | 本机采样 |
|---|---:|
| 完整 Local Map 生成 | 660.2841 ms |
| 生成期 Managed Memory 增量 | 36,990,872 bytes |
| 生成期 Working Set 增量 | 76,861,440 bytes |
| Route 样本 | 50 |
| Route 成功 | 50 / 50 |
| Route 中位数 | 1.2130 ms |
| Route P95 | 2.6585 ms |
| Route 最大值 | 9.6555 ms |
| Route 平均边数 | 44.08 |

Unity Streaming Load/Unload、GameObject、Mesh、Collider、Frame Time、GC Allocation 和 Nav Rebuild
测试代码已实现，但本轮 Unity 未进入测试框架，相关数据必须标为“未取得”，不能用 Domain 采样
替代。

最终差异审阅额外修正了 Streaming Cell 卸载时运行时道路 Mesh 的显式释放；上述最终编译与
766/766 回归均基于修正后的源码指纹
`FCE88450B907883034FACD1BC5474B8A5029CA52D7108CB31E22CAD3B0F7786E`。

## 5. 证据边界

- `tmp/` 中机器日志、XML、缓存和 Unity 临时输出不提交仓库；
- 本摘要只固化可复核数字和临时证据路径；
- Unity 门禁解除并取得新证据前，Formal Acceptance 必须保持 `NOT ACCEPTED`。
