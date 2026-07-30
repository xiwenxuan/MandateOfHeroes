# MandateOfHeroes 项目执行规则

## 项目专属开发 Skill

1. 涉及本项目 Unity/C# 功能、领域模型、世界模拟、战斗、人物、地图、任务、
   经济、存档、场景、Prefab、ScriptableObject、测试或里程碑验收时，必须使用
   `.codex/skills/mandate-unity-development/`。
2. 若当前 Codex 会话已发现 `mandate-unity-development`，按正常 Skill 触发规则
   使用；若未出现在可用 Skill 列表中，必须手动完整读取其 `SKILL.md` 后再行动，
   不得以“未自动发现”为由跳过。
3. 按 `SKILL.md` 的路由说明只读取当前任务所需参考文件。存档和确定性任务读取
   `references/persistence.md`，内容与序列化数据任务读取
   `references/content-and-data.md`，交付前读取
   `references/delivery-template.md`。
4. 代码任务优先通过
   `.codex/skills/mandate-unity-development/scripts/verify-project.ps1`
   执行统一验证；纯文档任务使用其 `-DocumentationOnly` 模式。
5. 本文件和用户当轮指令的优先级高于 Skill。Skill 不得扩大授权、自动提交、
   自动推送、关闭用户 Unity 编辑器或弱化以下防卡死规则。
6. Skill、程序集、存档版本、Unity 版本或测试入口发生变化时，必须同步复核
   Skill 参考资料和验证脚本。

## 防卡死硬规则

1. 禁止在前台直接执行 Unity、构建工具、测试工具或其他可能长期驻留的进程。
2. Unity 批处理测试必须通过 `Tools/Run-UnityTestsSafe.ps1` 启动。
3. 所有外部工具默认硬超时为 300 秒；确有必要延长时，必须先向用户说明原因并取得同意。
4. 长任务必须以后台进程启动，并以不超过 5 秒的间隔轮询状态；轮询期间持续保留进程 ID、日志路径和结果路径。
5. 到达硬超时后必须主动终止本次启动的进程树，报告超时阶段和日志尾部，禁止继续无期限等待。
6. 启动 Unity 批处理前必须检查现有 Unity 进程。若编辑器已打开，停止批处理并报告项目锁冲突，不得擅自关闭用户的编辑器。
7. 如果外层工具调用被中断，下一次工作必须先检查并清理“由本任务启动且已超过超时”的遗留进程。
8. 优先使用 `Tools/CoreTestRunner.cs` 完成纯 C# 快速回归；Unity 测试只用于程序集导入、序列化和编辑器集成验收。
9. 未取得测试结果文件或明确的测试运行汇总，不得声称“测试通过”。
10. 每轮开发先做编译，再做快速核心测试，最后做一次受控 Unity 测试；任一步失败均先修复，不提交未验证版本。

## 版本提交规则

- 只提交当前任务范围内的文件。
- 提交前执行 `git diff --check`、全工程编译和核心测试。
- Unity 测试若因环境锁定无法运行，必须在任务书中明确记录，不得以编译通过替代。
