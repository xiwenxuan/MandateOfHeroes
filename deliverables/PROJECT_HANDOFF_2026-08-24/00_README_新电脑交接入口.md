# 《群雄志：仕途》新电脑开发交接入口

> 交接生成日期：2026-08-24（Asia/Shanghai）
> 项目英文名：MandateOfHeroes
> 当前迁移目录：`J:\project\MandateOfHeroes`
> 本交接包：`J:\project\MandateOfHeroes_HANDOFF`

## 1. 最重要的结论

本次迁移得到的是一个包含完整 `.git` 历史、已提交代码、未提交修改、未跟踪正式成果、
Unity 工程、历史资料、地图数据、测试、工具和现有 Build 的可继续开发目录。

新电脑交接时必须同时保留：

```text
MandateOfHeroes/
MandateOfHeroes_HANDOFF/
```

如果新电脑不再使用 `J:` 盘，项目可以放到其他绝对路径；项目内部应继续使用仓库相对路径，
不要批量把 `J:\project` 写进代码或配置。

## 2. 五步恢复

1. 安装 Git for Windows、Unity Hub 和 Unity `2022.3.62f3c1`。
2. 将 `MandateOfHeroes` 与本交接目录完整复制到新电脑本地磁盘。
3. 在命令行进入项目目录，执行 `git status -sb`；预期状态见
   [02_仓库状态与迁移验收.md](02_仓库状态与迁移验收.md)。
4. 在 Unity Hub 中选择“添加/从磁盘添加项目”，指向 `MandateOfHeroes` 根目录。
5. 等待 Unity 首次重建 `Library`，然后先打开 `Assets/Scenes/PlayableDemo.unity`；
   当前地图审图打开 `Assets/Scenes/HanWorldArtDirectionLab.unity`。

## 3. 禁止操作

在完成新电脑首轮验收前，禁止：

- 只从 GitHub 重新克隆后把它当作最新项目；远端不包含全部本地状态；
- 执行 `git reset --hard`、`git clean -fd`、Unity Reimport All 或手工清理正式数据；
- 删除旧 `E:\project\gamedevelop\MandateOfHeroes` 备份；
- 把当前 310 个未跟踪项当作垃圾；其中包含正式地图、运行时代码、测试和历史资料；
- 提交、推送或重写分支历史，除非先完成范围审计；
- 从商业游戏复制地图、UI、素材、文本或代码。

## 4. 文档阅读顺序

接手开发的人或 AI 应按以下顺序读取项目内文件：

1. `AGENTS.md`
2. `.codex/skills/mandate-unity-development/SKILL.md`
3. `Docs/AI_PROJECT_BRIEF.md`
4. `.codex/skills/mandate-unity-development/references/task-routing.md`
5. `Docs/KNOWLEDGE_BASE/README_PROJECT_KNOWLEDGE_BASE.md`
6. `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md`
7. 当前任务对应的领域设计、任务书、代码与测试

不要一次把所有旧任务书当成现行设计。任务书是阶段合同和历史记录；系统总纲、知识库和
领域 L1 文档负责当前规则。

## 5. 本交接包内容

- [01_环境依赖与安装清单.md](01_环境依赖与安装清单.md)
- [02_仓库状态与迁移验收.md](02_仓库状态与迁移验收.md)
- [03_首次启动与验证流程.md](03_首次启动与验证流程.md)
- [04_项目文档与当前开发入口.md](04_项目文档与当前开发入口.md)
- [05_新AI会话启动提示词.md](05_新AI会话启动提示词.md)
- `handoff_manifest.json`：机器可读的迁移摘要

项目内另有 `Docs/GPT_HANDOFF/`，用于向网页版 ChatGPT 上传最小项目资料包。
