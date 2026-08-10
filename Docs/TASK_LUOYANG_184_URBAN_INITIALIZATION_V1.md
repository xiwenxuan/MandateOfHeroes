# LUOYANG-184-URBAN-INITIALIZATION-V1 执行任务书

状态：已完成并通过验收（2026-08-10）。

## 1. 目标与边界

在 `HanWorldV1`、2,000米Cell和既有洛阳历史设施原型上，正式物化184年洛阳连续城市区人口、家户、住宅、岗位、家族组织、军队与运行事件。正式人口口径固定为：城墙内200,000人、连续城市区270,000人、都市圈候选400,000人、供给区规划700,000人。

本任务只物化270,000人。400,000和700,000保留为后续容量规划，不自动生成；20,542、50K、100K、250K和500K仍是隔离的工程/压力Profile，不得混入正式场景。

## 2. 不变量

- 每个人从出生起拥有永久身份；不得合并、删除、替换或重新随机。
- 每名人物关联一个Household；Household与FamilyOrganization保持分离。
- 每名正式城市人物消耗一个真实住宅容量槽；现役军人使用真实兵营容量。
- 就业和学生状态引用真实Facility岗位/学位；不得要求100%就业。
- 继续使用同一CellId64，不创建SubCell，不创建第二套洛阳地图。
- 历史人物复用既有PersonId；城外或未知锚点不得强塞进洛阳。
- 历史、历史复原、玩法补全、工程测试和压力测试来源必须可区分。
- 184事件必须改变Person、Force、岗位覆盖或后勤压力等世界事实，而不是只显示文本。

## 3. 已实现内容

- 270,000条80字节定长PermanentPerson记录与53,992条Household记录。
- 25名洛阳历史人物与3名明确城外/未知历史锚点。
- 7个FamilyOrganization，共1,400名成员；逐组织人数为20、250、300、350、250、100、130。
- 1,230项Facility逐条审计；742项活动城市设施承担270,000住宅容量、160,000岗位、30,000学位、供水和储粮。
- 五支逐人编制军队，共34,000人；兵数标为历史命令锚点与C级工程重建，不冒充史载精确数字。
- 10个按稳定顺序推进的184事件，覆盖人物活动/位置、军队部署、岗位暂停、军事供给和运输压力。
- Domain纯C#合同、Persistence二进制/JSON读取器、Simulation事件与分块审计系统。
- 10项规定Excel主文件；04与06因指定表格工具的单工作簿内存上限，采用主索引加三个90,000行明细分卷，人物仍全部逐行存在。

## 4. 交付路径

- 正式运行包：`Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/`
- 生成器：`MapPipeline/scripts/build_luoyang_184_urban_initialization_v1.py`
- 独立审计器：`MapPipeline/scripts/validate_luoyang_184_urban_initialization_v1.py`
- 结构化输入：`MapPipeline/config/luoyang_184_urban_initialization_v1.json`
- Excel与报告：`outputs/LUOYANG_184_URBAN_INITIALIZATION_V1/`
- 领域合同：`Assets/Scripts/Mandate.Domain/Luoyang184UrbanInitializationState.cs`
- 持久化读取器：`Assets/Scripts/Mandate.Persistence/Luoyang184UrbanInitializationReader.cs`
- 运行系统：`Assets/Scripts/Mandate.Simulation/Luoyang184UrbanInitializationSystem.cs`

## 5. 验收结果

- 独立全量审计：PASS；270,000 Person、53,992 Household、1,230 Facility、10事件全部检查。
- 全工程编译：PASS。
- Luoyang筛选核心回归：PASS。
- Unity EditMode：5/5 PASS；完整人物扫描、家户覆盖、家族人数、事件推进和分块tick均通过。
- Unity PlayMode：1/1 PASS；加载4,096人分块且没有增加GameObject。
- Unity实测日人物审计tick约165.965ms；月家户tick约11.213ms。
- 270K人员二进制21,600,032字节；同格式400K估计32,000,032字节；700K自动生成关闭。
- `git diff --check`：PASS。

详细口径与差异说明见：

- `outputs/LUOYANG_184_URBAN_INITIALIZATION_V1/11_184洛阳城市初始化报告_V1.md`
- `outputs/LUOYANG_184_URBAN_INITIALIZATION_V1/12_LUOYANG_184_URBAN_INITIALIZATION_V1_AUDIT.md`

## 6. 明确延期

本任务不代表全国77城已经正式初始化，也不包含完整攻城器械、全套宫廷/太学玩法、全国认知地图、正式主存档迁移或700,000人供给区自动生成。这些内容必须另立任务，并继续遵守永久人物、统一Facility、同一世界Cell和来源分级合同。
