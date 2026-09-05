# 任务书：M/C/F 三主视角尺度收口与县域主沙盘 Presentation LOD 重构 V1

## 1. 目标

将普通玩家地图入口收口为三个主视角：

- `M 天下`：州、郡国、县、战略军队、山河与少量跨县主干道路；
- `C 县域`：县域建设、经济、生产、治理与局部战争的主沙盘；
- `F 人物`：人物室外移动、建筑交互、战斗与 Facility 玩法入口。

城区不是第四张地图，而是 `C 县域` 的 `UrbanArea` 子视图。县域内按相机尺度使用
`Far / Mid / Near` 三档表现，不改变任何正式世界事实。

## 2. 不可变合同

- 洛阳仍使用 320×640、50 米、204,800 Cell 的正式县域包；
- 2,084 个 Facility、334 条道路边、144 条城防边和四个 Portal 不删除、不改号；
- LOD、Overlay、相机、聚合图形均为可丢弃 Presentation；
- 不升级 `WorldState` Schema，不建立第二套道路、Facility、人口或建设权威；
- `PlanningCell` 只在 `C 县域 / 建设 / Near` 显示；`F 人物`不显示规划格；
- Existing Road 使用低饱和土褐色，Draft 使用青色，Invalid 使用红色；
- 不复制任何商业游戏素材。

## 3. LOD 合同

| 层级 | 决策目的 | 道路 | Facility | 城防 | 50m格 |
|---|---|---|---|---|---|
| Far | 整县空间结构 | R0、主要R1 | 地标和密度/聚合 | 城墙轮廓、城门 | 关闭 |
| Mid | 城区与干路 | R0/R1、主要R2 | 地标和分块代表 | 主要墙段、城门 | 关闭 |
| Near | 建设与局部交互 | 当前视窗内R0—R3 | 实体占地、入口 | 墙边、城门 | 仅建设子视图 |

进入阈值和退出阈值必须分离，避免缩放边界抖动。道路、设施和城防必须按当前视窗裁剪，
不得在县域全览把正式数据全部画成黄线和黄点云。

## 4. Overlay 合同

县域统一提供以下开关：

1. 行政；
2. 道路；
3. 河流；
4. 城防；
5. 格网；
6. 规划。

天下默认关闭 2km 战略格，默认只显示跨县战略道路骨架；“交通详图”才展开全部正式路线。

## 5. 实施范围

- 建立只读 `CountyMapPresentationStack`；
- 建立道路 `R0/R1/R2/R3` 重要度及 Far/Mid/Near 选择；
- 建立 Facility 地标、聚合代表、实体细节三档表现；
- 建立城区、农业和六分区低频底色；
- 建立城防轮廓、墙边和城门分级；
- 将县域道路/设施/城防从底图像素点云拆为屏幕空间矢量表现；
- 补齐 M 天下交通专题、县域图例和六类 Overlay；
- 增加 Core、EditMode、PlayMode 与真实 Game View 证据入口。

## 6. 验证顺序

```text
全工程编译
→ M/C/F与LOD定向Core
→ 完整Core分组
→ Unity Project Load
→ EditMode
→ PlayMode
→ git diff --check
→ 范围审阅
```

Unity 只允许通过项目安全入口运行。已有编辑器占用项目时不得关闭用户程序，应准确记录为
`BLOCKED`。

## 7. 截图证据

目录：`Docs/Evidence/McfViewScaleCountySandboxPresentationLodV1/`

固定输出任务书规定的 16 张截图，从 `01_world_strategic_default.png` 到
`16_person_view_no_grid.png`；其中保留旧问题镜头作为 Before，其余为同一正式运行入口的 After。

## 8. 完成定义

自动门禁全部通过并保持正式 `PlayableDemo / C 县域 / 洛阳 / 县域全览` Game View 运行时，
状态可写为 `IMPLEMENTED_AND_AUTOMATED_ACCEPTANCE_PASSED_READY_FOR_USER_REVIEW`。
用户明确验收前不得写为 `ACCEPTED`。
