# HAN-WORLD-ZHONGHUA-SANGUOZHI-INSPIRED-MAP-STYLE-PROTOTYPE-V1 任务书

## 目标

在不复制《中华三国志》代码、地图或美术资产的前提下，审计其公开源码的地图组织思想，并在《群雄志：仕途》唯一权威世界上建设第四套可切换地图表现候选 `STYLE_D_ZHONGHUA_SANGUOZHI_FUSION`。

## 强制边界

- 唯一事实来源仍是项目冻结的 3314×2176、2000m Global Cell、DEM、河流与自然地表数据；
- 外部仓库只能位于 Unity 工程外，不得进入 `Assets/`；
- 仓库、代码和资源许可证必须分开判断；无法确认时默认禁止复制；
- Style D 只能改变 Presentation，不得修改历史地理、行政、人口、设施或存档；
- 固定 WORLD、REGION、山体、河流、森林、平原、中间缩放与城市距离镜头；
- 必须形成 10 张 Game View、14 份工作簿、研究/许可/来源/Style D 报告和机器摘要；
- 不自动推广全国、不进入河南尹高精阶段、不开始洛阳建筑、不提交、不推送。

## 实施结果

- 固定候选仓库 HEAD：`50f00168e005f7e5d8576e5adc215b1fbe2f8fa5`；
- GitHub API 已枚举 3 branch、2 tag、297 commit、2 contributor 和未截断完整目录树；
- 完整 Git clone 连续受到 GitHub 443 连接失败/重置，`SOURCE_CLONED=NO`，属于任务定义的硬阻断；
- 候选仓库 API 返回 `license=null`，因此本轮对其代码与资产均执行净室边界；项目旧审计确认的 `k2lizheng/ZHSan` Ms-PL 只适用于其固定提交，不能外推到本候选仓库；
- 已实现稳定 Style D Profile、八个固定镜头、DEM 派生的坡度/起伏/脊/谷/山体/平原/森林/河谷特征通道和 Shader 融合；
- Style D 使用连续森林面，不生成树点式 canopy batch；
- Unity EditMode 2/2、PlayMode 1/1 通过并产出 10 张真实 Game View；
- 全国推广、河南尹高精和洛阳城市阶段均保持 `BLOCKED_PENDING_USER_APPROVAL`。

正式报告入口：

`HISTORICAL_WORLD_REFERENCE/HAN_WORLD_ZHONGHUA_SANGUOZHI_INSPIRED_MAP_STYLE_PROTOTYPE_V1/README.md`

## 最终验证（2026-08-16）

- 全工程编译：通过；
- 核心回归：按固定清单指纹拆分为 12 组，累计 `709/709` 通过，汇总运行 ID 为 `style-d-final-20260816`；
- Unity ProjectLoadSmoke：通过；
- Unity EditMode：Style D 专项 `2/2` 通过；
- Unity PlayMode：Style D 专项 `1/1` 通过并生成 10 张真实 Game View；
- `git diff --check`：通过（仅报告既有 CRLF/LF 提示）；
- 完整 Git clone：仍受 GitHub 443 连接失败硬阻断，因此源码研究状态不得写成“完整 clone 已完成”。
