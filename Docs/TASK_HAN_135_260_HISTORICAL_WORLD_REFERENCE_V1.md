# HAN-135-260-HISTORICAL-WORLD-REFERENCE-V1 任务书与执行记录

## 1. 任务目标

在不创建第二套运行时世界、不批量生成全国人口/家户/设施实例的前提下，把现有地图、人口、历史人物、Clan和Scenario资料整理为135—260年历史世界开发参考库。参考库必须让开发者区分史实、保守复原、项目模型和未知项，并能按年、州、郡国、县、城市、人物、Clan、事件和剧本切片查询。

## 2. 范围

- 建立`MasterWorld → AnnualChangeIndex → ScenarioSnapshot`资料结构。
- 全量覆盖126年、13州、105郡国、1182县级单位、77战略城市和13个Scenario。
- 接入1202名既有人物、39个Clan与15个Branch，但不把既有数量当作上限。
- 建立洛阳、长安、邺、许、成都、襄阳、江陵、建业8个CITY-S第一轮详档。
- 建立Facility、产业、交通、军事地理和行政设施专题参考。
- 建立5种后续研究模板、来源总索引和25项最终覆盖报告。

## 3. 不在范围

- Unity运行时代码、场景、Prefab、主存档迁移或全国世界实例生成。
- 为1182县自动编造资源、道路、设施、人口、家户或精确边界。
- 解决既有205个地点、64条关系和P0175在219年切片的重叠问题。
- 复制商业游戏内容，或把代理几何、模型人口、后世遗存冒充本时期史实。

## 4. 实施批次

| 批次 | 内容 | 验收 |
|---|---|---|
| A | 审计现有地图、人口、人物、Clan、Scenario与来源 | 数量与稳定ID一致 |
| B | 建立目录、总参考、证据合同与5个专题 | 导航完整、状态不夸大 |
| C | 建立126年、105郡国、1182县、77城、人物、Clan、事件、来源工作簿 | 使用规定表格工具；记录数与底座一致 |
| D | 建立13州、77城、13剧本档案 | 8个CITY-S详档，其余明确为骨架 |
| E | 建立模板、25项覆盖报告并接入项目路由 | 文档校验与差异审阅通过 |

## 5. 关键合同

1. 证据类型只有`HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN`四种开发标签；原数据的A/B/C置信等级继续保留。
2. 140年行政截面是稳定索引，不表示135—260名称、隶属和控制从未变化。
3. 史料人口用于参考；实际开局按硬件缩尺，永久人物不可删除、合并或重随机。
4. 洛阳供给圈约70万人包含都市圈约40万人，不得相加；河南尹人口是更大行政区域口径。
5. 城市、县、Facility、路线和资源的未知字段保留为空，不能用看似精确的自动结果掩盖研究缺口。
6. Scenario引用Master和逐年变化，不复制一套全国世界；260年不是模拟终点。

## 6. 交付物

- [`HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md`](HISTORICAL_WORLD_REFERENCE/README_历史世界开发参考资料索引.md)
- [`HISTORICAL_WORLD_REFERENCE/00_WORLD/00_135-260历史世界开发总参考_V1.md`](HISTORICAL_WORLD_REFERENCE/00_WORLD/00_135-260历史世界开发总参考_V1.md)
- 8份正式索引工作簿、13份州部参考、77份城市参考、13份Scenario参考、5份专题、5份模板。
- [`HISTORICAL_WORLD_REFERENCE/HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md`](HISTORICAL_WORLD_REFERENCE/HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1_最终覆盖报告.md)
- 可复现生成器：`MapPipeline/scripts/build_han_135_260_historical_world_reference_v1.py`与`build_han_135_260_historical_world_reference_workbooks.mjs`。

## 7. 执行结果

已完成第一轮交付：126年、13州、105郡国、1182县、77城、8个CITY-S、1202人物、39 Clan、15 Branch和13 Scenario均通过数量审计。8份工作簿均已生成说明页与数据页、渲染预览、结构检查和公式错误扫描。

这表示“开发参考资料骨架完成”，不表示全国县级历史考证、城市内部复原或运行时接入完成。69个非CITY-S城市、县级精确地点、低置信度路线/节点和人物关系缺口继续属于研究债。

## 8. 验证口径

- 本任务为纯文档/资料任务：执行`verify-project.ps1 -DocumentationOnly`。
- 另执行定量覆盖验证、工作簿结构检查、公式错误扫描、渲染预览检查、`git diff --check`与范围审阅。
- 不运行全工程编译、核心测试或Unity测试；这些验证不能证明或否定本轮纯资料交付。
