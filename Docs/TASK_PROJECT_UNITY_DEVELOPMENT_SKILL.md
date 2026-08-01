# 项目专属 Unity 开发 Skill 建设记录

## 1. 文档性质

本文件是 `mandate-unity-development` Skill 的建设、重构与验证记录，不是后续任务的规则入口。

正式执行顺序如下：

1. 用户当前明确指令；
2. 仓库根目录 `AGENTS.md`；
3. `.codex/skills/mandate-unity-development/SKILL.md`；
4. Skill 的 `references/`；
5. 当前任务直接相关的 `Docs/` 设计与任务文档；
6. 现有代码、测试和存档事实。

如果本文件与上述入口冲突，以更高层级为准。

## 2. 建设目标

项目专属 Skill 用于统一 MandateOfHeroes 的 Unity/C# 开发、诊断、评审、文档、测试和交付流程，重点解决：

- 开工前能够找到正确的设计、代码与测试；
- 项目硬规则、执行步骤和领域知识分层存放；
- 长时间 Unity 或测试进程有明确上限、归属和结果证据；
- 文档任务与代码任务采用不同但可审计的验证路径；
- 人口、战争、角色、地图、AI、存档等领域能够按需加载资料；
- 完成报告区分通过、失败、阻塞和未执行。

## 3. 最终结构

```text
.codex/skills/mandate-unity-development/
├── SKILL.md
├── agents/
│   └── openai.yaml
├── scripts/
│   ├── run-child.ps1
│   └── verify-project.ps1
└── references/
    ├── task-routing.md
    ├── architecture.md
    ├── testing.md
    ├── persistence.md
    ├── content-and-data.md
    └── delivery-template.md
```

各层职责：

- `AGENTS.md`：唯一的仓库级硬规则与防卡死约束；
- `SKILL.md`：每次任务都要执行的精简工作流；
- `task-routing.md`：按领域选择设计文档，不以里程碑编号推断优先级；
- 其余 references：架构、测试、存档、内容和交付的专项说明；
- scripts：有边界的统一验证入口；
- 本文件：保留建设缘由与验证历史。

## 4. 关键设计决定

### 4.1 避免规则复制

永久身份、确定性、存档兼容、资源守恒、许可证和进程安全等硬规则只在 `AGENTS.md` 维护。Skill 和任务书通过链接引用，不再复制整套规则。

### 4.2 渐进式加载

每次实质性项目任务先读取 `task-routing.md`，随后只读取当前领域需要的设计文档。
涉及系统状态、跨系统边界、生产建设科研或下一任务规划时，先读取
`Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`。人口、家庭、永久身份、AI 注意力、
事件调度和分区存储必须读取：

- `Docs/TASK_M12_PERMANENT_POPULATION_AND_ATTENTION.md`
- `Docs/WORLD_SIMULATION_FOUNDATION.md`
- `Docs/DETERMINISTIC_SIMULATION_AND_SAVE.md`

`Docs/TASK_M7_POPULATION_LEDGER.md` 作为较早的实现记录保留，但不能覆盖 M12 的永久人口设计。

### 4.3 任务编号不等于优先级

M7、M11、M12 等编号只表示里程碑身份。实际开发顺序由用户当前指令和有效开发计划决定。

### 4.4 验证分流

- 纯文档或 Skill 文本变更：使用 `verify-project.ps1 -DocumentationOnly`；
- C# 代码变更：完整编译、核心回归、受控 Unity 测试、差异检查；
- Unity 集成受环境阻塞：报告为 `blocked`，不得宣称通过；
- 所有长任务默认受 300 秒硬上限和进程树归属约束。

## 5. 初始建设验证记录

项目专属 Skill 初次建立时完成了以下验证：

- Skill 结构和 frontmatter 快速校验通过；
- PowerShell 脚本语法解析通过；
- 文档模式统一验证入口通过；
- 全解决方案编译通过；
- 核心回归测试通过，历史记录为 `RESULT passed=104 failed=0`；
- Unity 批处理测试因编辑器占用项目而记录为阻塞，没有关闭用户的 Unity。

以上是建立时的历史证据，不代表后续代码始终通过。每个新任务必须重新报告它实际执行的验证。

## 6. 维护规则

发生以下变化时，应同步检查 Skill：

- 仓库目录、程序集或测试入口改变；
- 存档架构、稳定 ID、随机流或世界时间模型改变；
- 新增需要强制读取的领域总设计；
- Unity 安全运行脚本或结果格式改变；
- 交付状态定义改变。

维护时优先更新权威来源，并检查链接和脚本；不要把整套规则重新复制回任务书。

## 7. 当前状态

该 Skill 已建立并纳入仓库工作流。后续重构新增统一任务路由，修正了永久人口与
注意力设计的强制读取关系，并将 `GAME_SYSTEMS_MASTER_AND_STATUS.md` 纳入系统状态、
跨系统设计和全局建设顺序的统一入口；本文件仅保留历史记录。
