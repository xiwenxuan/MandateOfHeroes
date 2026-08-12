# LUOYANG-184-HISTORICAL-V1：184年东汉洛阳历史初始地图、统一设施、人口岗位与城防原型任务书

## 1. 目标与边界

在既有 `HanWorldV1`、2000m Cell与洛阳推荐人口档上，建设可重生成、可查询、可由Unity场景运行的184年
东汉洛阳历史城市竖切片。原型必须使用同一世界、同一CellId64、同一永久人物和同一Facility事实，不建立
城内第二地图或SubCell，不复制商业游戏资产。

本阶段交付历史来源分级、洛阳城郭与十二门、南北宫及宫墙、护城壕、主要宫署/市场/仓储/教育/礼制设施、
真实人物住房和岗位、地方建设压力、多Cell蓝图、城防通行V0、正式PNG和Unity验证场景。

## 2. 强制规则

本任务建立并落实：

1. `RULE-LY-001`：正式人工建筑必须拥有数据定义和持久设施状态，禁止装饰POI冒充世界事实。
2. `RULE-HOUSING-001`：永久住房容量只按Person计；兵营只收现役Person；无住房者不删除。
3. `RULE-JOB-001`：真实Person填充设施岗位；资格与适配度分开；无最低工人不正常产出。
4. `RULE-AI-BALANCE-001`：AI读取实际住房、就业、技能、粮食、治安等压力，不读固定Cell配比。
5. `RULE-BUILD-002`：历史生成、玩家和AI共享带方向、道路、模块与施工阶段的多Cell蓝图。

年代固定为184年东汉。史实、合理复原和玩法补全分别记录，后世曹魏/北魏形态不得混入。

## 3. 实施清单

- [x] 审计2000m Cell表达力，并声明操作性抽象边界。
- [x] 建立设施、按人住房、岗位资格/适配、AI压力、蓝图与城防领域合同。
- [x] 复用20,542名永久Person与4,498户，不删除、合并或重新随机。
- [x] 生成十二大城门、外城墙、南北宫独立宫墙/宫门、护城壕和虎牢方向。
- [x] 生成南宫、北宫、永安宫、濯龙园、中央官署、太仓、武库、市场、太学、明堂、辟雍、灵台、兵营与里坊。
- [x] 所有可见历史建筑保存Owner、Controller、用途、能力、工人/服务和未来玩法钩子。
- [x] 生成岗位目录、真实Person岗位引用、空缺与技能短缺及AI压力快照。
- [x] 生成玩家/历史生成器/AI共享的多Cell建设蓝图。
- [x] 生成七份报告、独立Python审计和正式历史地图PNG。
- [x] Unity场景读取184包，支持连续缩放、Facility选择、历史/住房/岗位/城防专题和城门状态演示。
- [x] 完成全工程编译、全核心回归、Unity EditMode与PlayMode及差异审阅。

## 4. 主要产物

- 领域：`Assets/Scripts/Mandate.Domain/HistoricalCityFacilityState.cs`
- 持久读取：`Assets/Scripts/Mandate.Persistence/Luoyang184HistoricalPrototype.cs`
- 表现：`Assets/Scripts/Mandate.Presentation/LuoyangWorldValidationController.cs`
- 数据：`Assets/StreamingAssets/WorldMap/Luoyang184HistoricalV1/`
- 完整人口证据：`MapData/Luoyang184Historical_V1/population/`
- 生成与审计：`MapPipeline/Build-Luoyang184HistoricalV1.ps1` 及对应Python脚本
- 正式地图：`deliverables/LUOYANG_184_HISTORICAL_V1/LUOYANG_184_HISTORICAL_MAP_V1.png`
- 报告：`MapData/Luoyang184Historical_V1/reports/01` 至 `07`

## 5. 验收条件

必须依次通过全工程编译、独立数据审计、全核心测试、受控Unity EditMode、受控Unity PlayMode、
`git diff --check` 和范围审阅。测试无结果文件或明确汇总时不得声称通过。

## 6. 明确延期

完整冲车、投石机、地道、火攻、攻城后勤、完整礼制/太学/朝廷玩法、完整Blueprint UI、全国城市历史复原和
正式主存档升级延期。延期不等于删除设计，也不得在当前状态中写成已实现。

## 7. 2026-08-09执行与验证记录

- 一键生成与独立Python审计：PASS；20,542 Person、4,498 Household、1,230 Facility、173个184历史/复原
  Facility、十二座大城门、130段墙、80段护城壕、七份报告和1,266,193字节正式PNG。
- 全工程编译：PASS；证据在 `tmp/skill-verification/compile-20260809-092527-105.out.log`。
- 新增纯C#目标测试：PASS，5/5。
- 全量核心回归：PASS，529/529，32组；聚合证据
  `tmp/core-test-groups/luoyang184-final-20260809/aggregate.json`。
- Unity EditMode：PASS，9/9；证据
  `tmp/unity-validation/unity-EditMode-20260809-092650-198.summary.json` 及同名XML。
- Unity PlayMode：PASS，1/1；场景实际完成读取、历史设施选择、城防专题、城门状态和缩放验证。完整XML
  写出后Unity未在15秒宽限期自然退出，安全启动器只终止本次PID，并按项目规则记录
  `forcedCleanupAfterResult=true`；证据
  `tmp/unity-validation/unity-PlayMode-20260809-092719-034.summary.json`。
- `git diff --check` 与最终范围审阅：PASS（最终交付前执行；工作区既有无关修改未纳入本任务结论）。
