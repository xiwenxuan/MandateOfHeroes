# M17-P0：数据驱动作物、产品、配方与生产内容合同任务书

## 1. 任务性质

- 类型：架构底座、领域合同、内容加载、存档兼容、模拟重构、回归测试和文档同步；
- 状态：已完成；
- 所属阶段：M17正式农业—仓储生产链的前置纠偏；
- 全局位置：不改变系统总纲的开发顺序，先解决现有M17固定`CropKind`与单一粮食合同，
  再继续扩大作物、加工、市场和军需；
- 权威设计：
  [`PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md`](PRODUCTION_AGRICULTURE_INDUSTRY_AND_PROGRESSION_DESIGN.md)。

## 2. 背景与当前事实

执行中M17已经建立：

- V8农业工单和生产账本原型；
- 小麦播种、劳动承诺、确定性收获、家庭粮仓和损耗审计；
- 五种控制方式共用一套农业结算的目标合同；
- V7至V8生产集合初始化和快照测试方向。

但当前代码仍使用固定`CropKind`枚举，`AgricultureWorkOrderState`只记录作物枚举，市场使用
另一套`CommodityState`，家庭粮食仍是单一余额。这些实现可以作为守恒与工单底座，不能
作为支持地方品种、MOD作物、多级加工、产品批次和职业成长的最终合同。

## 3. 本阶段目标

建立第一版正式的数据驱动生产内容底座，并把现有小麦—粮仓链迁移为其首个消费者：

```text
核心内容包中的作物、产品、配方和方法定义
→ 内容注册、稳定ID与引用校验
→ 工单只保存定义ID和世界状态引用
→ 小麦种子投入与麦粒产出按定义结算
→ 既有家庭粮账、仓储、消费和税收继续兼容
→ 增加新作物或MOD内容无需修改枚举或存档结构
```

本任务完成的是**内容与第一条生产链合同**，不宣称已经完成全部产品批次、市场经济、
职业突破、太守委任和军需产业。

## 4. 持久化假设与发布边界

当前仓库中的V8农业字段仍属于执行中、未正式收尾的M17工作。默认实施策略为：

1. 在M17正式验收和对外发布前直接把V8原型字段改为数据驱动合同；
2. 保持`WorldState.CurrentSchemaVersion == 8`；
3. 保留V7至V8顺序迁移，以及V7人口存储模式和永久人物事实；
4. 不为未发布的中间V8原型额外制造V9迁移。

执行前必须检查Git历史、远端和用户存档。如果中间V8已经被发布、交付或形成需要兼容的
真实存档，则停止“原地改V8”，改为V8至V9顺序迁移并补充中间V8迁移测试，不能静默破坏。

## 5. 架构边界

### 5.1 `Mandate.Domain`

负责：

- 稳定内容ID值对象或校验规则；
- 作物、地方品种、产品、配方、生产方法的纯C#定义合同；
- 工单、产品/种子引用、库存事务和持久状态；
- 不变量，不依赖Unity表现层或资源系统。

### 5.2 内容加载层

建立无Unity引擎依赖的内容加载与验证边界。优先建立独立`Mandate.Content`程序集，引用
`Mandate.Domain`和项目既有JSON能力，负责：

- 读取核心内容包和测试内容包；
- 规范化ID与固定加载顺序；
- 构建只读注册表；
- 校验重复ID、缺失引用、配方图、单位和历史元数据；
- 计算已解析内容哈希和内容清单。

如果实施审计证明新增程序集会破坏当前核心测试入口，可以先提交独立架构说明并同步修改
核心测试构建入口；不得为了省事把内容读取写进`Mandate.Domain`或Unity场景组件。

### 5.3 `Mandate.Simulation`

负责：

- 通过只读内容注册表解析ID；
- 创建、推进和结算农业工单；
- 把定义参数、世界环境、人物能力和确定性随机组合为结果；
- 写入库存和生产账本；
- 不硬编码“Wheat必定产出grain”的内容关系。

### 5.4 `Mandate.Persistence`

负责：

- V7至最终V8迁移；
- 工单、定义ID、内容清单与必要世界状态往返；
- 缺失内容和未来版本拒绝策略；
- 保护M15-P6人口侧车包、永久身份和冷热扩展合同。

### 5.5 `Mandate.Presentation`

本阶段不制作完整生产UI，只允许为验证内容加载增加最小只读调试输出；不得把定义或生产
事实只保存在MonoBehaviour、场景或ScriptableObject实例中。

## 6. 纳入范围

### 6.1 通用规则

1. 在`AGENTS.md`和数据设计中确立“开放内容使用稳定ID与数据定义，封闭协议状态才使用
   枚举”的硬规则；
2. 新增内容不升级存档结构；持久结构变化才升级存档版本；
3. 缺失内容保留原ID并显式报告，不静默替换、删除或重随机。

### 6.2 内容定义

建立首版：

- `CropDefinition`；
- `CropVarietyDefinition`；
- `ProductDefinition`；
- `RecipeDefinition`；
- `ProductionMethodDefinition`；
- 内容包、内容清单、定义来源和版本元数据；
- 只读注册表及验证报告。

首版字段应足够支持现有小麦链以及后续稻、粟、黍、豆、麻、桑和加工产品，不要求一次
实现详细土壤、品质和全部批次字段，但不得用新的固定枚举封死这些扩展点。

### 6.3 首个核心内容包

至少提供原创数据定义：

```text
crop.wheat
crop_variety.wheat.prototype_northern
product.wheat_seed
product.wheat_grain
recipe.field.grow_wheat
method.farming.prototype_dryland
```

原型地方品种必须明确标注为玩法补全或测试内容，不能伪装成已有史料认证品种。

另增加一个只用于测试的MOD内容包，至少包含：

```text
crop.mod_test.example
product.mod_test.example_seed
product.mod_test.example_harvest
recipe.mod_test.grow_example
```

该包用于证明扩展内容不需要修改代码枚举和世界存档版本，不进入正式历史内容。

### 6.4 M17农业工单迁移

1. 删除公开持久合同中的`CropKind`；
2. 将工单改为引用`crop_definition_id`、`crop_variety_definition_id`或世界品种ID、
   `recipe_definition_id`和`method_definition_id`；
3. 将硬编码小麦默认值迁入核心内容包；
4. 创建工单时验证定义、配方、设施、输入产品和知识前置；
5. 保留`ProductionControlMode`和`ProductionOrderStatus`等封闭协议枚举；
6. 账本事件可以暂保留封闭事件类型，但必须增加产品ID、数量、单位和来源引用，避免只能
   表达“grain”；
7. 五种控制方式继续使用同一种工单和结算公式。

### 6.5 市场与家庭粮账兼容

1. 明确`commodity.grain`是旧市场聚合商品还是具体产品的兼容视图；
2. 当前阶段允许家庭`Grain`/`SeedGrain`继续作为兼容余额，但必须由产品事务同步，不能成为
   第二个独立产出源；
3. 建立校验确保家庭粮账、家庭粮仓和产品事务不重复计数；
4. 文档记录未来迁移到通用产品批次和多仓库库存的路线；
5. 不在本任务中同时重写全部贸易、军需、医疗和UI消费者。

## 7. 不纳入范围

- 全部历史作物和地方品种的资料录入；
- 通用多仓库产品批次的完整实现；
- 面粉、酿造、桑蚕、纺织、药物、冶铁和造船的正式生产链；
- 地块级土壤、水文、病虫害和逐日生长动画；
- 职业突破、师承秘籍和完整人物生产流派代码；
- 动态市场、商队、店铺、契约、地方特产和价格重构；
- 太守、军团和政权级目标指令UI；
- 军需征发、劫掠和设施破坏的新代码；
- 提交、推送或创建拉取请求。

这些内容已进入完整设计，不因本任务分期而删除。

## 8. 工作包

### WP1：发布边界与现状审计

- 检查V8是否已经进入Git历史、远端或用户可用存档；
- 列出`CropKind`、`commodity.grain`、家庭粮账、工单和账本的全部消费者；
- 确认程序集引用和核心测试构建入口；
- 形成不破坏M15-P6人口合同的迁移决定。

### WP2：内容合同与注册表

- 新增纯C#定义对象和稳定ID校验；
- 新增内容包加载、固定排序、注册、哈希和验证；
- 拒绝重复ID、缺失引用、空ID、非法单位和非法配方图；
- 测试核心包与MOD测试包共同加载。

### WP3：首个生产内容包

- 编写小麦、种子、麦粒、种植配方和旱作方法定义；
- 标注内容来源等级和原型参数性质；
- 确保定义文件不包含商业游戏数据、文本或素材。

### WP4：农业工单重构

- 将固定作物枚举改为定义ID引用；
- 从注册表读取基准投入、产量、时间和产品关系；
- 将输入预留、产出、入仓和损耗账绑定产品ID；
- 保持现有确定性、劳动冲突和仓容守恒。

### WP5：存档与兼容

- 根据WP1决定最终V8或V9迁移；
- 保存内容清单和工单定义引用；
- 完成旧版本迁移、当前版本往返、未来版本拒绝和缺失内容报告；
- 验证永久人物、家庭、设施和M15-P6人口存储模式不变。

### WP6：测试与文档同步

- 更新核心测试和Unity EditMode测试；
- 更新总纲状态、确定性存档、数据内容底座、M17任务书和Skill路由；
- 生成明确的验证结果，不提前填写完成记录。

### 8.1 预期交付物

- `Assets/Scripts/Mandate.Domain/`中的生产内容定义与持久状态合同；
- 优先位于`Assets/Scripts/Mandate.Content/`的无Unity内容注册、加载和验证程序集；
- `Assets/Content/Core/Production/`或经实施审计确认的等价核心内容包目录；
- 独立测试内容包，不进入正式历史内容；
- 重构后的`AgricultureProductionSystem`和M17村庄接入；
- 最终V8或必要时V9的顺序迁移、快照和内容清单；
- 内容校验、农业守恒、确定性、迁移和缺失MOD回归测试；
- 更新后的总纲、数据底座、存档设计、M17任务书、Skill路由与完成记录。

目录在实施时可依据现有Unity导入和核心测试入口做等价调整，但程序集依赖方向、稳定ID、
内容与状态分离及验收合同不得弱化。

## 9. 必须保持的不变量

1. 每个人物永久ID、家户关系和M15-P6人口存储合同不变；
2. 作物、产品、配方和方法通过稳定ID引用，不通过显示名称或列表位置引用；
3. 输入产品只能被同一时段的一个有效预留消耗；
4. 生产输出等于入库、转运、副产品、废料和明确损耗之和；
5. 仓储不能超过容量；
6. 家庭粮账与产品/设施库存不能互相重复创造数量；
7. 关注程度和控制方式不进入实际产量公式；
8. 同一事实、世界种子、工单ID、阶段和日期得到相同结果；
9. 新增内容包不能要求修改`CropKind`一类枚举；
10. 缺失定义不能自动映射为其他产品；
11. V7迁移不能删除、合并或重随机人物；
12. 失败迁移不能覆盖原存档。

## 10. 验收标准

### 10.1 内容与扩展

1. 核心小麦链完全由内容定义驱动，模拟代码不再引用`CropKind.Wheat`；
2. 代码公开合同中不存在固定`CropKind`；
3. 测试MOD包能够新增作物、种子、收获物和配方，不修改领域枚举或世界存档版本；
4. 内容注册表稳定排序，并生成可重复的解析哈希；
5. 重复ID、缺失产品、缺失配方、非法单位和非法零成本增殖循环被明确拒绝。

### 10.2 生产与守恒

6. 种子不足不创建工单或改变库存；
7. 五种控制方式在相同事实下得到相同产量与账本；
8. 仓容不足时只存入可用容量，余量进入明确损耗；
9. 产品ID、数量、来源工单和库存变化能够完整审计；
10. 同一测试世界按逐日推进和批量推进得到相同生产终值；
11. 200—500人村庄全年循环不重复播种、收获或计粮。

### 10.3 存档与兼容

12. V7按最终决定迁移后保留全部永久人物、家庭和人口存储模式；
13. 当前版本往返保留内容清单、工单定义ID、账本和库存；
14. 零、负数、未来和不支持版本按合同拒绝；
15. 缺失测试MOD时不静默替换或删除产品，报告原ID和受影响对象；
16. 如果WP1确认中间V8已发布，必须有V8至V9迁移和中间V8回归样本。

### 10.4 工程验证

17. 全工程编译通过；
18. 核心测试输出`RESULT passed=N failed=0`；
19. 受控Unity EditMode测试产生有效XML且零失败；
20. `git diff --check`通过；
21. 差异审阅确认只包含本任务文件和执行所需的直接修改；
22. 总纲状态没有把后续产品链、市场、成长或委任误报为已实现。

## 11. 建议测试清单

至少新增或调整：

- `ContentRegistry_LoadsCoreProductionPackDeterministically`；
- `ContentRegistry_RejectsDuplicateAndMissingReferences`；
- `RecipeGraph_RejectsFreeQuantityGeneratingCycle`；
- `ProductionContent_ModCropRequiresNoEnumOrSchemaChange`；
- `Agriculture_OrderResolvesDefinitionsByStableId`；
- `Agriculture_FiveControlModesProduceSameFacts`；
- `Agriculture_RejectsMissingDefinitionWithoutChangingWorld`；
- `Agriculture_ProductLedgerBalancesInputOutputAndLoss`；
- `Agriculture_SameSeedAndDurationProducesSameSnapshot`；
- `Snapshot_V7MigratesToFinalProductionContractWithoutPopulationChange`；
- `Snapshot_RoundTripPreservesProductionContentReferences`；
- `Snapshot_MissingModPreservesOriginalIdsAndReportsFailure`；
- `VillageLife_OneYearUsesDataDrivenWheatChainWithoutDoubleCounting`。

## 12. 执行顺序与检查点

```text
检查点A：完成WP1，只读确认发布边界和消费者
→ 检查点B：内容注册表与独立测试通过
→ 检查点C：小麦核心包可以独立加载和校验
→ 检查点D：农业工单完成定义ID迁移并保持守恒
→ 检查点E：存档迁移与缺失内容策略通过
→ 检查点F：完整工程、核心、Unity和差异验收
```

任一检查点发现以下情况必须暂停并报告，不能静默选择：

- 已发布V8与“原地修改V8”假设冲突；
- 内容程序集需要反向依赖Unity表现层；
- 既有家庭粮账和商品库存无法在不重复计数的情况下兼容；
- 缺失内容修复会删除玩家财产或人物历史；
- 当前工作区的用户修改与本任务文件发生无法隔离的实质冲突。

## 13. 验证命令

实现完成后按项目统一入口执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1
```

外部工具硬超时300秒；Unity测试只能通过`Tools/Run-UnityTestsSafe.ps1`。如果Unity编辑器
已打开造成项目锁，记录为`blocked`并报告，不能擅自关闭用户程序，也不能用编译或核心测试
替代Unity证据。

## 14. 完成记录

### 14.1 发布边界决定

执行前检查当前HEAD、全部本地分支和远端跟踪引用：`ProductionState.cs`、M17任务书和
V8农业合同均未进入任何Git提交，也没有已交付V8存档证据。因此本阶段在未发布V8内完成
纠偏，保持`WorldState.CurrentSchemaVersion == 8`，没有制造无意义的V9。V7至V8迁移继续
保留全部永久人物和M15-P6人口存储模式，并初始化核心生产内容清单。

### 14.2 实现结果

- 删除公开`CropKind`枚举；农业工单改为稳定作物、地方品种、产品、配方、方法和单位ID；
- 在`Mandate.Domain`建立纯C#生产内容定义、注册表、稳定排序、SHA-256内容哈希、内容清单、
  JSON序列化和引用校验；
- 当前解决方案的核心测试入口由既有四程序集组成，因此没有提前增加空壳
  `Mandate.Content`程序集；内容合同与Unity表现无关，未来扩大内容规模时可以按设计无损
  拆分程序集；
- 新增核心生产JSON资源，定义小麦、北方小麦原型品种、种子、麦粒、种植配方和原型旱作法；
- Unity表现层从`Resources`加载核心JSON并把只读注册表注入世界模拟；无Unity运行使用
  具有相同解析哈希的内置核心包；
- 注册表拒绝重复ID、缺失引用、非法数量、非法单位、无投入产出和直接免费增殖配方；
- 世界快照保存包ID、版本、顺序、内容哈希和解析哈希；缺失MOD或内容变更时显式拒绝，
  保留原清单引用，不静默替换；
- 小麦投入、产量、入仓和损耗完全由配方与方法定义读取，五种控制方式继续共享同一结算；
- 生产账本增加产品ID和单位；劳动账使用封闭事件类型及劳动日单位；
- 家庭`Grain`/`SeedGrain`仍是M17兼容余额，但只由同一生产事务同步，产品账本不是第二个
  独立产出源；`commodity.grain`继续是地区市场聚合视图，尚未迁移成通用批次库存。

### 14.3 回归覆盖

新增或加强的证据包括：

- 核心JSON资源与内置核心包解析哈希一致；
- 重复、缺失和直接免费增殖定义被拒绝且注册失败保持原注册表不变；
- 测试MOD新增作物、品种、产品、配方和方法，不修改枚举或存档版本；
- 缺失测试MOD显式报告原包ID；
- 篡改活动工单定义ID会在写快照前被拒绝；
- 五种控制方式事实一致、种子不足无副作用、仓容损耗可审计；
- 200—500人村庄一年形成每户一张完成工单，存档往返保留内容引用；
- V7迁移到V8后人物顺序、人口存储模式和生产内容清单保持合同。

### 14.4 验证记录

最终验证证据：

```text
全工程编译：passed
核心测试：RESULT passed=129 failed=0
Unity EditMode：total=130 passed=130 failed=0
Unity结果：tmp/unity-EditMode-20260802-171716.xml
Unity日志：tmp/unity-EditMode-20260802-171716.log
git diff --check：passed
```

Unity首次在工作区沙箱内未于45秒启动窗口生成日志，安全脚本只终止其拥有的进程；依据
项目测试规则，以同一`Tools/Run-UnityTestsSafe.ps1`在沙箱外重试一次后取得上述通过XML。
未关闭用户程序，没有超出300秒硬超时。
