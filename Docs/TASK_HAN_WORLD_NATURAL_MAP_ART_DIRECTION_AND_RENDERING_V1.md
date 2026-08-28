# HAN-WORLD-NATURAL-MAP-ART-DIRECTION-AND-RENDERING-V1

## 目标

在唯一 `hanworld.albers.china.v0`、3314×2176 Global Cell、2000m Cell、真实 DEM、正式河流与森林输入上，建立三套只改变表现、不改变世界事实的自然地图候选：

- `STYLE_A_REALISTIC_NATURAL`：半写实自然；
- `STYLE_B_CHINESE_SEMI_REALISTIC`：国风半写实战略沙盘；
- `STYLE_C_STRATEGIC_SANDBOX`：强调战略可读性的三国沙盘。

正式完成状态只能是 `HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY`；`USER_SELECTED_STYLE` 保持 `PENDING`，全国推广保持 `BLOCKED_PENDING_USER_APPROVAL`。

## 保护边界

- 不重做 GIS、DEM、Global Origin、Global Cell、Region、Terrain Tile；
- 不移动河流、CanonicalPlace，不伪造洛水；
- 不制作河南尹高精 Terrain、洛阳城市、城墙、建筑或道路；
- 三套候选必须使用同一世界数据、同一样板坐标、同一 WORLD/REGION 相机；
- 禁止背景贴图、商业游戏资产和纯二维水墨候选。

## 实施清单

1. 建立 `HanWorldArtProfile` 与三个稳定 Profile ID；
2. 以一个 Shader 架构实现 palette、slope、curvature、ridge、valley、macro、lighting、fog、water、forest 参数；
3. 建立 `HanWorldArtDirectionLab`，运行时无需重载世界数据即可切换 STYLE A/B/C；
4. 冻结中原平原、山地河谷、森林丘陵三组 WORLD/REGION 相机；
5. 生成 18 张真实 Game View 候选和 3 张三联对比图；
6. 记录 CPU/GPU、draw calls、material、shader variant、vegetation、river、memory；不可获得的 GPU 时间明确报告，不伪造；
7. 输出 12 份工作簿、视觉报告、总报告和机器摘要；
8. 执行编译、核心回归、Terrain/River/Forest/Profile/Camera/Floating-Origin/Cell-Picking/PlayMode、差异、凭据、路径和残留进程检查。

## 用户门禁

Codex 可推荐 STYLE B，但不得填写用户选择。任务结束后停止，等待用户选择 A、B、C 或提出基于某套继续修改。
