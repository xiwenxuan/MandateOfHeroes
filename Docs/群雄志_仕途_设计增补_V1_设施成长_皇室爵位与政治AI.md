# 《群雄志：仕途》设计增补 V1
## Facility 成长、皇室主脉、王国爵位、自立与政治 AI

> 本文档承接既有《FACILITY_CATALOG_V1》与《洛阳184建设蓝图 V1》。
> 仅归纳其后已经讨论并形成共识的新增内容；未确定的具体数值、AI权重、课程细目等统一保留为后续平衡项。

---

# 一、Facility 体系进一步收口

## 1. BaseType / Variant / Instance 分离

后续不因为皇宫、家族、官府、民间等使用场景不同，就不断创造新的设施类型。

统一采用：

```text
BaseType
↓
Variant / Profile
↓
Instance
```

例如：

```text
Barracks
→ Palace Profile
→ 北宫某军营
```

```text
Barracks
→ Family Profile
→ 张氏军营
```

两者底层都仍是 `Barracks`。

园苑同理：

```text
Garden
→ Imperial Profile
→ 濯龙园
```

```text
Garden
→ Family Profile
→ 张氏后园
```

核心原则：

> 历史名称、所有者、使用场景不同，不等于新的 Facility BaseType。只有核心作用机制真的不同，才新增 BaseType。

---

# 二、Facility 五种成长方式

不再将传统“建筑 Lv.1 → Lv.2 → Lv.3”作为唯一底层成长方式。

## 1. Parameter Upgrade｜参数升级

同一个 Cell、同一个 Facility，提高已有能力：

```text
PopulationCapacity
WorkerCapacity
StorageCapacity
ServiceCapacity
Durability
Quality
Efficiency
ParallelProductionCapacity
```

不增加 Cell。

## 2. Equipment / Module｜设备与内部模块

增加不值得独立占据世界 Cell 的内部硬件和小型功能。

例如铁坊：

```text
炉具
风箱
铁砧
安全设备
```

住宅：

```text
会客空间
小书房
安防设施
```

### Module 与独立 Facility 的边界

如果一个新增功能已经具有以下特征之一：

- 大量独立工作人员；
- 独立库存；
- 独立生产流程；
- 可以独立损坏或被占领；
- 有明显独立服务对象；
- 玩家有必要单独管理；
- 具有明显地图空间意义；

则应建设为新的 Facility Cell，而不是继续塞进 Module。

## 3. Recipe / Process Expansion｜配方、工艺与服务内容扩展

Facility 可以在不增加 Cell 的情况下，通过新增可执行内容扩大功能。

统一可包括：

```text
Recipe
Process
Course
Treatment
TrainingProgram
ServiceOperation
```

例如铁坊：

```text
锄、镰
↓
增加：
剑
刀
矛头
甲片
```

厨房：

```text
粥、饭
↓
增加：
肉食
宴席
药膳
```

太学：

```text
基础经学
↓
增加：
律令
算学
兵学
医学
天文
……
```

军营也可以通过新增 `TrainingProgram` 扩展训练内容。

### 掌握配方不等于立即能生产

实际执行至少需要：

```text
Facility Capability
+
已掌握 Recipe / Process
+
Required Equipment
+
Qualified Worker
+
Material
+
Time
```

因此可以出现：

```text
已掌握高级甲具配方
但缺少高级炉具
且缺熟练工匠
→ 暂时无法生产
```

## 4. Convert / Refit｜改造转型

同一个 Cell 经过真实施工改变用途、Profile 或配置。

例如：

```text
普通住宅
→ 家族特殊住宅
```

```text
Warehouse
→ Arsenal Profile
→ 武库
```

仍需：

- Owner / 建造权；
- 权限；
- 材料；
- 工人；
- 时间。

不能瞬间切换。

## 5. Spatial Expansion｜空间扩张

当单 Cell Facility 达到合理能力上限后：

> 必须通过新增 Cell 和新 Facility 继续扩张，而不能无限升级。

例如：

```text
族宅满
→ 获取新 Cell
→ 再建族宅
```

```text
太学容量满
→ 新建第二 Academy / Library
→ 太学 Complex 扩大
```

```text
北宫空间不足
→ 获取相邻 Cell
→ 新建新的宫廷 Facility
```

该规则用于保证：

- 土地价值；
- 城市扩张；
- 庄园扩张；
- 宫城扩张；
- 城墙扩张；

始终具有实际意义。

---

# 三、太学与藏书系统进一步明确

## 1. 太学定位

太学不再只是历史地标或 Facility 骨架，而是第一批需要真正深入玩法的高级特殊设施。

```text
BaseType = Academy
Variant = ImperialAcademy
Instance = 太学
```

核心 Capabilities：

```text
Education
AcademicResearch
LibraryAccess
Assembly
```

## 2. 太学培养真实 Person

太学的实际培养流程：

```text
真实教师 Person
+
真实学生 Person
+
课程
+
学习时间
+
人物适性
+
藏书支持
↓
人物属性 / 技能 / 知识成长
```

不是：

```text
洛阳文化 +10%
```

### 课程可扩展

课程通过 `CourseDefinition` 定义。

未来可包括：

```text
经学
礼学
律令
算学
兵学
医学
天文
农业
工艺
……
```

没有想清楚的课程暂不写死，保留后续开发。

## 3. 藏书是真实世界资产

太学藏书不采用简单的：

```text
LibraryLevel = 5
```

而建议最终对应：

```text
BookDefinition
BookCopy / CollectionEntry
```

书籍可以提供：

```text
Knowledge
CourseSupport
RecipeKnowledge
ProcessKnowledge
HistoricalContent
```

由此形成：

```text
获得典籍
↓
阅读 / 研究
↓
Person 掌握知识
↓
组织掌握工艺
↓
Facility 解锁新的 Operation / Recipe / Course
```

Library 也应是通用体系，可用于：

- 太学；
- 官府；
- 皇宫；
- 家族；
- 私人书院；
- 家族藏书楼。

---

# 四、皇宫人口与皇室主脉

## 1. 皇宫不是整个刘氏宗室的住宅区

皇宫核心人口只包含当朝皇帝的主脉家庭。

主要包括：

```text
皇帝
后妃
太子
尚未外居的皇子
尚未外居的皇女
必要的上一代核心皇室成员
```

不包括：

```text
所有刘氏宗室
所有诸侯王
所有远支皇族
```

因此皇宫皇室人口天然保持相对有限，不会随着洛阳人口同比增长。

## 2. 非继承皇子最终分出去

皇子生命周期：

```text
皇子出生
↓
宫廷成长
↓
是否为皇位继承人？
```

如果是：

```text
太子
→ 留在中央皇室主线
```

如果不是：

```text
封王
→ 可暂时留京
→ 皇帝命令就国
→ 离开皇宫
→ 建立自己的王府组织
```

不得做成：

```text
年龄达到某值
→ 自动搬出皇宫
```

因为封王和就国之间允许存在时间差。

---

# 五、皇帝核心家庭本质上仍是特殊 FamilyOrganization

皇帝家不建立另一套完全独立玩法。

皇室原则上可以使用整个统一 Facility Catalog，例如：

```text
Residence
Garden
Kitchen
Warehouse
Stable
Library
Barracks
Workshop
Farmland
Plantation
……
```

184年历史初始化时，不主动给皇宫配置大片普通麦田、粟田等庄园式主粮产区。

但后续如果皇帝、玩家或 AI 取得土地：

> 系统上允许建设普通产业和农业 Facility。

皇室真正特殊的地方主要是：

1. 核心人口规模有限；
2. 中央皇室只保留一条继承主线；
3. 非继承皇子会分出建立王府；
4. 皇帝同时拥有特殊国家政治权力。

---

# 六、皇室继承：单一中央继承线

中央皇室可理解为：

```text
皇帝
├─ 太子 → 接续中央皇室主线
├─ 皇子A → 封王 → 王府A
├─ 皇子B → 封王 → 王府B
└─ 皇女 → 婚嫁 / 外居
```

皇帝死亡后：

```text
实际皇位继承人
↓
成为新皇帝
↓
接管 ImperialHousehold 核心地位
```

中央宫廷组织资产不在诸皇子之间拆分。

继承人不能写死“长子”，应使用：

```text
ImperialHeirId
```

支持：

- 立太子；
- 废太子；
- 改立；
- 皇位继承争议。

---

# 七、国家资产、皇室资产与个人资产分离

至少区分：

```text
State Assets
国家公共资产

Imperial Household Assets
皇室 / 宫廷组织资产

Emperor Personal Assets
皇帝 Person 私人资产
```

例如：

```text
太仓
→ 国家资产
```

```text
北宫宫仓
→ ImperialHousehold 资产
```

```text
皇帝私人珍宝
→ Emperor Person 私人资产
```

皇帝死亡以后：

- 国家资产不进入私人遗产；
- 皇室组织资产继续属于 ImperialHousehold；
- 皇帝个人资产才走个人遗产规则。

---

# 八、诸侯王正式接入现有爵位体系

不单独创造“诸侯王系统”。

> 王属于现有爵位体系中的特殊高等级爵位。

爵位继续负责：

```text
身份
爵禄
食邑 / 封邑权益
继承
社会地位
```

默认：

> 爵位不自动给予行政权或兵权。

---

# 九、王国“国”与郡同级

东汉地方行政层级上：

```text
普通地区 → 郡
皇子封王地区 → 国
```

游戏中统一：

```text
AdministrativeLevel = CommanderyEquivalent
```

RegionType：

```text
Commandery
Kingdom
```

即：

```text
郡 / 国
```

属于同一级行政区。

---

# 十、“国属于王”不等于“国境内产权属于王”

例如：

```text
陈国
NominalLord = 陈王
```

只表示：

- 王的封国；
- 王爵与区域绑定；
- 王府享有制度规定的封国收益；
- 王具有特殊政治地位。

绝不表示：

```text
陈国全部 Cell
Owner = 陈王
```

国境内仍可存在：

```text
百姓私人 Cell
家族 Cell
官府 Cell
公共 Facility
王府自己的 Cell
```

继续坚持：

> 封国关系 ≠ Cell 产权。

---

# 十一、国相是王国中与太守对应的郡级行政长官

普通郡：

```text
太守
```

王国：

```text
国相
```

二者可以统一成：

```text
CivilOfficeBaseType = CommanderyGovernor
```

Variant：

```text
GrandAdministrator
= 太守

KingdomChancellor
= 国相
```

因此：

```text
王
→ 爵位 / 封国 / 收益 / 王府

国相
→ 实际郡级行政治理
```

王不自动兼任太守或国相。

---

# 十二、王府与王国行政分离

皇子就国以后形成自己的：

```text
PrinceHousehold
```

可以拥有：

```text
王府特殊住宅
普通住宅
Garden
Warehouse
Kitchen
Stable
产业
土地
……
```

但：

```text
王府
≠
王国政府
```

王府服务于王及其家庭。

王国行政则由国相等王国官员负责。

---

# 十三、自立门户重新定义

“自立门户”不是：

```text
H1 → H2
```

也不是必须从汉廷直接脱离。

它是：

> 从当前上级或政治集团中脱离，建立自己的势力。

例如：

```text
张三
PoliticalRole = Subject
AllegianceTarget = 曹操
```

执行自立门户：

```text
PoliticalRole:
Subject → Ruler

AllegianceTarget:
曹操 → Self
```

并建立：

```text
OwnPolity
```

这个操作可以从属于任何上级的人物发生，不要求原先一定直接属于汉廷。

---

# 十四、从自立为 Ruler 开始进入“君主玩法”

一旦成功成为 `Ruler`，玩家或 AI 即拥有自己的政治集团，并获得类似《三国志》系列“君主”的势力权限：

- 任命本势力 CivilOffice；
- 任命 MilitaryOffice；
- 管理势力公共财政；
- 调动所属公共军队；
- 决定公共建设；
- 管理行政；
- 外交；
- 战争。

但：

> 成为 Ruler 不等于反汉，也不等于称王、称帝。

---

# 十五、政治身份与对汉关系拆分

不再把 H1～H5 当作一条“晋升等级”。

至少拆成以下轴：

## 1. PoliticalRole

```text
Subject
Ruler
Emperor
```

回答：

> 是否是一个政治集团的最高领导人？

## 2. AllegianceTarget

```text
HanCourt
某个 Person
Self
```

回答：

> 当前实际跟随谁？

## 3. HanRelation

回答：

> 当前政治集团和汉廷是什么关系？

第一版可包括：

```text
HanSubject
正常奉汉臣属

HanAutonomous
奉汉自治

HanSeparatist
名义奉汉、事实割据

IndependentFromHan
公开独立 / 叛汉
```

## 4. SovereignClaim

```text
None
King
Emperor
```

称王、称帝不再与“是否自立门户”混为一件事。

---

# 十六、汉末政治必须允许灰区

## 1. 正常奉汉

接受汉廷正常统辖，原则遵守命令。

## 2. 奉汉自治

已经形成自己的政治军事集团，但仍承认汉室。

拥有君主玩法权限。

对具体汉廷命令可以：

```text
Comply
Partial
Delay
Refuse
```

## 3. 名义奉汉、事实割据

继续使用汉官号、汉年号等名义，但原则上已经不接受汉廷实际控制。

## 4. 公开独立

正式脱离汉廷。

## 5. 自称王 / 帝

通过 `SovereignClaim` 建立更高政治主张。

---

# 十七、汉廷命令单独判断

不能简单写：

```text
HanAutonomous = 永远执行50%命令
```

每一道：

```text
ImperialOrder
```

都单独进行 AI 决策。

输出可包括：

```text
Comply
Partial
Delay
Refuse
```

判断因素：

- 命令内容；
- 当前军事实力；
- 财政；
- 与皇帝关系；
- 与目标势力关系；
- 自身利益；
- 性格；
- 家族利益；
- 地区安全。

---

# 十八、控制皇帝的触发条件

不是占领某个名为“洛阳”的城市就自动获得皇帝处置权。

必须真正：

```text
控制 Emperor Person
+
控制皇帝当前所在地 / 宫城
+
皇帝实际上无法自由脱离己方控制
```

皇帝已经离开洛阳：

> 仅占洛阳不能触发皇帝处置。

---

# 十九、控制皇帝后的五种实际政治结果

## 1. 保留当前皇帝 + 交权

```text
继续拥立当前皇帝
+
把中央实际权力还给皇帝
```

结果：

```text
自己的独立政治集团并入汉廷
Ruler → Subject
```

私人财产、FamilyOrganization、私人 Cell、私人产业、合法爵位等不因此消失。

## 2. 保留当前皇帝 + 不交权

```text
皇帝继续在位
+
自己的势力集团继续存在
+
自己继续掌握中央实权
```

相当于：

```text
ControlsEmperor = true
```

典型权臣 / 挟天子路线。

## 3. 废旧帝 + 另立某人 + 交权

```text
废旧帝
↓
立新皇帝
↓
中央权力真正交给新帝
↓
自身集团并入新汉廷
```

## 4. 废旧帝 + 另立某人 + 不交权

```text
废旧帝
↓
立新帝
↓
自己的政治集团仍保留
↓
继续控制中央
```

同样：

```text
ControlsEmperor = true
```

## 5. 自立为帝

```text
自己成为皇帝
↓
原势力集团保留并升格
↓
建立自己的皇帝法统
```

原汉帝再进入后续处理：

- 废黜；
- 软禁；
- 放逐；
- 封爵；
- 逃亡；
- 其他后续玩法。

---

# 二十、“交权”必须是真实世界操作

交权不能只是一个政治标签变化。

至少意味着：

```text
独立行政任命权
→ 汉廷

势力公共财政
→ 汉廷体系

所属公共军队
→ 汉朝军队体系

公共行政 Facility
→ 汉廷控制链

直属公共官员
→ 汉朝官职体系
```

但：

```text
私人土地
私人产业
个人财富
FamilyOrganization资产
合法爵位
```

不因交权自动被没收。

---

# 二十一、AI 自立：允许，但必须低频

核心原则：

> 任何真正具备政治基础的人理论上都可以自立，但自立是低频、高影响行为，不是 AI 的常规成长目标。

绝大多数 AI 应具有较强的既有效忠惯性。

真正天然强烈倾向自己做君主的人应是少数。

---

# 二十二、高野心不等于必然自立

高 Ambition 可以表现为：

- 想升官；
- 想统兵；
- 想封侯；
- 想掌握更大权力；
- 想成为权臣；
- 想扩大地盘。

只有当现有政治体系长期无法满足其利益，且独立条件成熟时，自立才成为高权重选择。

---

# 二十三、忠诚不是一个单一数字

底层至少应区分：

```text
PersonalLoyaltyToLord
对当前主君的私人忠诚

HanLegitimacyAffinity
对汉室正统的认同

OrganizationCohesion
对当前政治集团的依附

FamilyInterest
家族利益权重

Ambition
个人野心
```

因此完全允许：

```text
野心很高
+
对主君忠诚很高
→ 仍长期不自立
```

---

# 二十四、自立需要“意愿 × 可行性 × 机会”

禁止：

```text
Ambition > 80
→ CreatePolity
```

AI至少要判断：

- 实际控制多少军队；
- 是否拥有稳定地盘；
- 是否有财政；
- 是否有支持自己的官员；
- 是否有支持自己的将领；
- 地方豪族是否支持；
- 当前上级有多强；
- 上级是否能立即镇压；
- 周围强敌情况；
- 家族资产和家属位置；
- 独立后能否长期生存。

因此可以理解为：

```text
IndependenceDecision
=
Desire
× Feasibility
× Opportunity
```

只有多项条件共同成熟，才真正执行。

---

# 二十五、效忠关系具有惯性

建议引入：

```text
AllegianceInertia
```

概念。

长期跟随某主君的人，如果：

- 多次受其提拔；
- 与其并肩作战；
- 家属位于势力腹地；
- 部下与该势力高度绑定；
- 产业和土地均位于势力内部；

则脱离成本显著提高。

AI不应该每月重新判断一次：

> “我要不要造反？”

应主要在重大事件时重新评估，例如：

```text
主君死亡
继承危机
势力惨败
被夺职
受到逮捕威胁
中央崩溃
主君长期敌视
自身军力显著膨胀
皇帝更替
重大政变
```

---

# 二十六、改投其他强主应比自立更常见

一个 AI 不满当前上级时，正常行为优先级更接近：

```text
继续效忠
↓
争取职位 / 利益
↓
消极服从
↓
改投其他强主
↓
自立门户
```

所以：

> 背主不等于自立。

自立应当是成本最高的政治选择之一。

---

# 二十七、自立时，下属不能自动全部跟随

例如太守张三自立：

```text
张三
→ 自立门户
```

不能自动：

```text
辖下全部县令
全部将领
全部军队
全部官署
100%跟随张三
```

核心下属应根据：

- 对张三关系；
- 对原主君忠诚；
- 对汉室认同；
- 自身野心；
- 家族利益；
- 风险；
- 当前军事实力；

重新判断。

可能出现：

```text
跟随张三
继续奉旧主
回归汉廷
转投第三方
自己另行自立
```

因此真正适合自立的人，需要已经形成自己的政治和军事班底。

后续可增加：

```text
FactionCohesion
```

或等价指标。

---

# 二十八、政治世界应形成自然的“分裂—兼并”曲线

目标不是：

```text
所有太守全部自立
→ 全国几十上百个势力
```

而应自然形成：

```text
中央稳定
→ 自立极少

中央削弱
→ 自治集团增加

中央崩溃
→ 少数有实力者真正自立

割据高峰
→ 势力数量达到峰值

兼并战争
→ 势力逐渐减少
```

AI生成和政治参数应整体偏向：

> 稳定、忠诚、既有关系惯性。

而不是默认鼓励所有人物独立。

---

# 二十九、玩家与 AI 共用同一政治规则

玩家没有特殊的“系统作弊式君主按钮”。

玩家点击：

```text
自立门户
```

AI则根据上述因素计算是否自立。

底层行为一致：

```text
CreatePolity
SetPoliticalRole(Ruler)
AllegianceTarget = Self
FollowersReevaluate
EstablishTreasury
EstablishCivilOfficeSystem
EstablishMilitaryCommand
AssignActuallyControlledTerritory
```

之后再决定自己的：

```text
HanRelation
SovereignClaim
```

---

# 三十、当前建议的数据骨架

## Person / Political Actor

```text
PoliticalRole
AllegianceTarget

PersonalLoyaltyToLord
HanLegitimacyAffinity
OrganizationCohesion
FamilyInterest
Ambition
RiskTolerance

Relationships
Titles
CivilOffice
MilitaryOffice
```

## Polity

```text
PolityId
RulerPersonId
HanRelation
SovereignClaim
ControlsEmperor

Treasury
CivilOfficeSystem
MilitaryCommandSystem
ControlledTerritory
Diplomacy
```

## Emperor

```text
CurrentEmperorPersonId
ImperialHeirId
CurrentLocation
Controller
```

## Imperial Order

```text
Issuer
Target
Content
OrderCompliance:
- Comply
- Partial
- Delay
- Refuse
```

---

# 三十一、当前仍留待后续开发/平衡的内容

以下内容暂不在本版本写死：

- Facility具体升级数值；
- 单Cell各种Facility最终容量；
- 全部课程；
- 全部BookDefinition；
- Recipe/Process具体解锁速度；
- AI性格精确分布比例；
- AI政治决策权重；
- 自立成功概率公式；
- FactionCohesion公式；
- 王爵具体封国收益比例；
- 王府具体官属细节；
- 多帝并立的完整法统系统；
- 废帝后的全部处置分支；
- 皇帝秘密诏令与权臣冲突的深层玩法；
- 称王、称帝的具体外交和合法性惩罚。

这些均保留为后续专门系统设计。

---

# 三十二、最终设计原则

> Facility 的成长不依赖单一建筑等级，而通过参数、设备、配方/工艺、改造和新增Cell共同实现。

> 皇帝核心家庭不是整个宗室，而是当朝皇帝主脉；非继承皇子通过封王、就国建立新的王府组织。

> 王属于爵位体系，王国属于与郡同级的行政区域；王享有封国身份与收益，国相负责实际郡级行政，封国关系不改变Cell产权。

> 自立门户是从任何现有上级关系中脱离并建立自己Polity的行为，与“是否奉汉”是两个不同维度。

> 成为Ruler后即可拥有完整势力君主玩法，但仍可奉汉、事实割据、公开独立、称王或最终称帝。

> 控制皇帝后，真正的核心选择是：保留还是废立皇帝，以及交权还是继续掌权；自立为帝则另行建立自己的皇帝法统。

> AI允许自立，但必须强烈受到忠诚惯性、现实实力、班底支持、风险和机会约束。绝大多数人物应优先维持现有效忠或改投强主，而不是轻易建立新势力。

> 汉末世界应从中央稳定逐渐走向自治、割据和少数势力自立，再通过战争兼并重新减少势力数量，而不是所有地方官自动碎裂成君主。

---

# 文档结束
