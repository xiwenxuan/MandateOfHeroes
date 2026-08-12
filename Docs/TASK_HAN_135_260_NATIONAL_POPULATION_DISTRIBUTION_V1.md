# HAN-135-260-NATIONAL-POPULATION-DISTRIBUTION-V1 任务书与完成记录

## 1. 状态

状态：**已完成（2026-08-10）**。

本任务建立135—260年连续、可查询、可追溯和逐级守恒的全国历史人口母盘。
它不生成全国Permanent Person，也不修改已经物化的洛阳400,000人包。

## 2. 权威输入

- `Data/HistoricalPopulation/han_140_sources.json`；
- `Data/HistoricalPopulation/han_140_population_records.csv`；
- `Data/HistoricalPopulation/han_140_administrative_units.csv`；
- M13的105项郡国人口来源、13州和1182县永久ID；
- 140年9,698,630户、49,150,220口国家锚点；
- 157年《晋书·地理志》10,677,960户、56,486,856口国家锚点；
- `LUOYANG-184-METROPOLITAN-INITIALIZATION-V1`的20万/27万/40万局部校准。

M13有效分项合计49,207,358口与140篇末国家锚点相差57,138口。V1不覆盖任何
史籍或校录原值，而以单列`NationalAnchorReconciliation`将时间线调和到国家锚点。

## 3. 完成范围

1. 135—260共126个年度，逐年保存RegisteredPopulation和ModeledActualPopulation；
2. 13州、105个CommanderyEquivalent和1182县的同一条年度时间线；
3. 全国—州—郡国—县—县内聚落结构逐年误差严格为0；
4. 135—139连续回推并自然收敛至140国家锚点；
5. 184_START为53,500,000推定实际人口，184_END为51,500,000，黄巾冲击未提前扣除；
6. 战争、疫病、迁徙、屯田、殖民与恢复均使用稳定事件ID和数据定义；
7. 县级分配使用治所/首都角色及确定性土地、水源、市场代理权重，未平均分；
8. 城乡、男女、五档年龄和军民口径进入郡国记录，军事人口包含在总人口内；
9. 13个正式剧本Snapshot和12个重要时间点均直接读取年度分片；
10. 运行时按年分片，读取任意年份不要求126年全部常驻内存；
11. 184洛阳40万都市圈结论为`PASS`；
12. 洛阳70万供给区结论为`KEEP_700K`，但它是包含40万都市圈的供给包络，不是
    40万再加70万；本任务只给出全国一致性结论，没有物化新增30万人；
13. 全国Permanent Person生成数为0。

## 4. 数据与程序

### 4.1 模型输入

- `Data/HistoricalPopulation/han_135_260_population_model_v1.json`；
- `Data/HistoricalPopulation/han_135_260_population_events_v1.json`；
- `Data/HistoricalPopulation/han_135_260_population_sources_v1.json`。

### 4.2 构建与校验

- `MapPipeline/scripts/build_han_135_260_population_distribution_v1.py`；
- `MapPipeline/scripts/validate_han_135_260_population_distribution_v1.py`。

### 4.3 运行时合同

- `Assets/StreamingAssets/HistoricalPopulation/Han135260V1/manifest.json`；
- `years/year_135.json`至`years/year_260.json`；
- `scenario_index.json`与13个场景引用；
- `annual_population.json`、`events.json`、`administrative_timeline.json`、
  `county_weights.json`、`major_city_timeline.json`、`luoyang_consistency.json`；
- `HanNationalPopulationState`领域合同；
- `HanNationalPopulationDatasetReader`按年读取、场景读取及文件哈希校验；
- `HanHistoricalPopulationQuerySystem`全国、州、郡国、县、城市与场景查询入口。

## 5. 正式交付

`outputs/HAN_135_260_NATIONAL_POPULATION_DISTRIBUTION_V1/`包含：

- 01全国年度总表；
- 02州级年度分布；
- 03郡国年度分布；
- 04县级主索引及135—260六个分卷，共148,932条；
- 05主要城市人口锚点；
- 06历史人口事件；
- 07行政人口映射时间轴；
- 08模型参数与假设；
- 09人口守恒与一致性审计；
- 10洛阳全国母盘一致性审计；
- 11全国人口分布研究报告；
- 12含13个正式剧本工作表的Snapshot工作簿；
- 性能、深度验证及逐工作簿渲染/公式检查证据。

## 6. 验收结果

- Python深度校验：126/126年度通过；
- 13州、105郡国等价单位、1182县永久ID完整；
- 148,932条县年记录完整；
- 140登记人口严格为49,150,220；
- 157登记人口严格为56,486,856；
- 每年实际人口、登记人口与迁徙守恒误差均为0；
- 13个剧本Snapshot全部引用同一时间线文件哈希；
- 17个正式工作簿、29个工作表完成渲染检查；
- 公式错误扫描为0；
- 全工程编译通过；
- 任务相关核心测试9/9通过；
- Unity EditMode受控测试1/1通过（16.185秒）；沙盒内首次无启动日志被安全终止，
  同一安全脚本在沙盒外重试通过；
- `git diff --check`通过，仅显示既有换行符警告；
- 无残留Unity或CoreTestRunner进程。

## 7. 限制与后续

- 140和157之外多数精确数字为B/C级历史重建或模型值，不得称为史籍实录；
- V1保护永久地理连续性，但没有声称穷尽126年内每次郡县改置；
- 除184洛阳外，多数城市人口仍为C级模型；
- 下一步可依据`KEEP_700K`结论另立洛阳供给区物化任务，但必须继续避免400K/700K
  重复计数；
- 全国Person化必须另立`HAN-NATIONAL-PERSON-MATERIALIZATION-ARCHITECTURE-V1`，并继续
  遵守M12缩尺、永久身份与累计容量规则。
