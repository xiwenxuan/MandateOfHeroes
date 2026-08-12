# HAN-135-260-DEVELOPMENT-PLACE-FULL-REFERENCE-PACK-V1 任务书

## 目标

在不改变既有 72 地点名册和开发波次的前提下，将旧 D2—D5 制作深度无损迁移为 T1—T4，并为每个正式地点建立同一标准的完整开发参考包。完整表示所有问题已经审计，不表示所有问题都有肯定史料答案。

## 不可变边界

- 正式名册固定为 72 项；本任务不增删地点、不重新评分、不改变 Wave。
- 映射固定为 `D2→T1`、`D3→T2`、`D4→T3`、`D5→T4`；旧文件保留为历史证据。
- D0/D1 不再属于特殊 Development Place 档位；名册外地点没有 T0。
- `DevelopmentTier`、`ReferencePackCompleteness`、`RuntimeImplementationStatus` 相互独立。
- 每个 T1—T4 地点使用相同的 25 模块参考标准。
- 允许 `HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN / NO_EVIDENCE / NOT_APPLICABLE`，禁止伪造 Cell、设施、人口、人物在场、宗族中心或永久聚落。
- 事件地点必须区分永久聚落、永久地理地点、事件依赖复合体、战场区域和未解析空间。战役名望不等于永久聚落。
- 事件设施只在事件真实发生时通过统一 Facility 类型建立；事件未发生则不应用建造包。
- 本任务不修改 Runtime、Unity、Scene、Prefab、Save Schema、人口、设施实例、军营或家庭组织。

## 交付物

1. `Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/` 当前权威入口、术语、标准、升级协议和完备性报告。
2. `DEVELOPMENT_PLACE_MASTER.xlsx` 与 10 份专题汇总工作簿。
3. 72 个 `PACKS/<StablePlaceId>/` 目录，每个包含 README、25 表工作簿、来源与未知项。
4. 事件依赖地点主表，至少覆盖官渡、街亭、五丈原、赤壁和祁山。
5. 更新 16 份现有 Development Manifest、知识库 7 份登记表、系统总纲、历史资料入口、知识库入口和任务路由。
6. 可重复执行的生成器、工作簿生成器、验证器和结果报告。

## 执行批次

- A：T4 + T3，共 16 地点。
- B：T2，共 33 地点。
- C：T1，共 23 地点。

批次只控制制作顺序，不改变完整参考标准。

## 验收

- 72 地点 ID、名称、Wave 与旧名册逐项一致；T1/T2/T3/T4 为 23/33/15/1。
- 72 个目录与 72 份工作簿齐全，每份恰好 25 张规定工作表。
- 完整度状态使用诚实且可审计；研究阻塞不得伪造为史实。
- 事件建造包含触发、设施类型、原因、用途、工人/兵力、材料、工期与后处置；未触发时不得应用。
- 旧 D 术语工作簿不覆盖；新当前主表不使用 D 术语作为现行档位。
- 全部工作簿用 artifact-tool 生成、检查并逐表渲染；文档验证、专用验证、`git diff --check` 和范围审阅通过。

## 状态

2026-08-11：已执行；最终结论以 `outputs/HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1/validation_report.json` 为准。
