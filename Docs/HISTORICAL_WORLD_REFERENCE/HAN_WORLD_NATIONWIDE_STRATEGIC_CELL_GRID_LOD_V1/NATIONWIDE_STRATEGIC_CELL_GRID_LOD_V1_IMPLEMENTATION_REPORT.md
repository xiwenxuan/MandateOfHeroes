# 全国战略格 LOD V1 实施报告

## 结论

全国格子化已经接入现有地图控制器。世界总览用 32×32 Cell 的纯视觉 LOD 引导格保持可读性与单批性能；进入任意合法 Cell 后切换到逐个 2km 精确格。两层都读取同一 `WorldMapCellId`，没有新增世界分区或存档结构。

当前门禁：`NATIONWIDE_GRID_IMPLEMENTED_STATIC_CHECKS_PASSED_UNITY_RUNTIME_BLOCKED`。

## 几何预算

| 层级 | 覆盖 | 表现列表 | 逻辑边段 | 顶点 | 渲染对象 |
|---|---:|---:|---:|---:|---:|
| WORLD 全国引导格 | 7,211,264 Cell | 0 个逐格 ID | 14,316 | 57,264 | 1 |
| REGION 精确格 | 当前 24×24、576 Cell | 576 个稳定 ID | 1,200 + 高亮 | 约 4,800 + 高亮 | 2 |

WORLD 引导格低于 65,535 顶点，可使用 16 位网格索引。全国覆盖计数不等于同时创建七百万个格面。

## 验证结果

| 阶段 | 结果 | 证据 |
|---|---|---|
| 离线战略格几何执行检查 | PASS | `RESULT passed=17 failed=0` |
| Roslyn C# 语法检查 | PASS | `RESULT parsed=4 errors=0` |
| 全工程编译 | BLOCKED | 本机缺少 Unity 2022.3.62f3c1 和受支持的 VS/MSBuild/.NET 4.7.1 targeting pack |
| 核心测试 | NOT RUN | 全工程编译环境未建立 |
| Unity EditMode / PlayMode | BLOCKED | 项目指定 Editor 未安装；现有 Unity 6 无有效许可证 |
| 全国 Game View 截图 | NOT GENERATED | 无有效 Unity 运行时，不能用概念图替代 |
| `git diff --check` | PASS | 无 whitespace error；仅有用户既有 Knowledge Base 文件的换行提示 |

## 后续验证入口

安装并激活 Unity 2022.3.62f3c1 后，运行 `ExplicitStrategicCellMapV1Tests` 与 `ExplicitStrategicCellMapV1PlayModeTests`。PlayMode 会先验证全国总览 LOD，再验证河南尹、洛阳和山地精确格。

## 概念预演

`Concept/NATIONWIDE_STRATEGIC_CELL_GRID_LOD_V1_CONCEPT_NOT_RUNTIME.png` 基于既有 Style D 全国 Game View，仅用于判断全国总览的网格密度。它由内置 ImageGen 生成，不是 Unity 输出、不进入 `Screenshots/`，也不得登记为 Golden 或性能证据。
