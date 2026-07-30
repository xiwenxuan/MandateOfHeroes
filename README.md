# 群雄志：仕途（Mandate of Heroes）

一款以中国古代群雄割据时代为灵感的开源单机角色扮演战略游戏。

玩家不必永远扮演势力君主，而是作为世界中的一名武将生活：投仕、在野、执行任务、
结交人物、积累功绩、获得官职，也可以叛离、自立并最终统一天下。大地图层负责城池、
军团和势力竞争，个人层负责人物关系、任务、成长与选择。

## 项目位置

`E:\project\gamedevelop\MandateOfHeroes`

后续 Unity 工程请创建或打开此目录，不再使用上一级已有的 Godot 工程。

## 当前阶段

项目处于预制作阶段。详细方案见：

- `Docs/DEVELOPMENT_PLAN.md`
- `Docs/GAME_VISION_AND_GAMEPLAY.md`
- `Docs/DATA_AND_CONTENT_FOUNDATION.md`
- `Docs/PREPRODUCTION_BACKLOG.md`
- `Docs/HISTORICAL_CITY_LIST.md`
- `Docs/CITY_UNION_MASTER.md`
- `Docs/SERIES_REFERENCE_AUDIT.md`
- `Docs/LEGAL_AND_ASSETS.md`
- `Docs/HISTORICAL_EVENTS_182_190.md`
- `Docs/HISTORICAL_CHARACTERS_FIRST_50.md`
- `Docs/PROTOTYPE_MAP_184_ZHUO_GUANGZONG.md`
- `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`
- `Docs/SANDBOX_NPC_AI.md`
- `Docs/TASK_M1_PLAYABLE_ENTRY.md`

## 运行可玩原型

在Unity中打开`Assets/Scenes/SimulationDashboard.unity`，点击播放按钮。游戏首先进入主菜单，可以：

- 创建姓名、年龄、性别和身份均可自定义的人物；
- 选择刘备、关羽、张世平、陈医师等世界中的现有人物；
- 以军人、县吏、商人或医者身份开始；
- 在六个原型地点之间沿真实道路旅行；
- 查看当前人物、家庭、组织职位和地区状态；
- 接受与当前地点和身份匹配的任务；
- 推进世界时间并使用内存保存和读取；
- 从游戏内进入开发观察台。

开发观察台继续支持：

- 推进一天或30天；
- 查看地点粮价与治安；
- 查看NPC月度重点；
- 让刘备从涿县徒步前往中山；
- 测试内存存档与读取。
- 按组织和职位接受县吏、军人、行商任务。
- 查看家庭家产、债务、在世成员、月度生活事件与家主继承。
- 查看六地粮食、布帛、盐和战马价格，并体验张世平的中山—涿县商旅。
- 在黄巾起事后命令冀州官军进军广宗，观察军粮、士气、伤亡与野战结果。
- 让商人向驻军售粮、让军队从当地市场采购，或通过运粮任务补充前线军粮。
- 让医者购买药材并救治战斗产生的伤兵，使恢复者重新返回部队。

当前界面是验证核心循环的程序员美术版本，正式地图、美术和交互将在后续里程碑替换。

## 开源策略

- 自研游戏代码计划采用 MIT License。
- 美术、音乐和音效优先采用 CC0，其他素材必须逐项登记许可证。
- 不包含任何《三国志》系列游戏的程序、图像、音乐、剧本或数据文件。
- 游戏名称、界面、美术与具体文本均采用原创表达。
