# 《中华三国志》代码与资产许可边界

## 结论

候选仓库 `kpxp/ZhongHuaSanGuoZhi-New-Code` 的 GitHub License API 为 `null`，根目录页面未显示 LICENSE。公开仓库、免费游戏和“代码开源”的宣传表述不足以替代具体许可证文本。因此对该固定快照：

```text
CODE_LICENSE_STATUS = UNRESOLVED
ASSET_LICENSE_STATUS = UNRESOLVED_AND_SEPARATE
DIRECT_CODE_REUSE = FORBIDDEN
DIRECT_ASSET_REUSE = FORBIDDEN
```

## 与旧审计的关系

项目已有 `Docs/ZHSAN_OPEN_SOURCE_LICENSE_AND_INTEGRATION_AUDIT.md`，它在 2026-08-06 对 `k2lizheng/ZHSan` 的固定提交 `851e0a222af214ba65b9881fb90411248a56f3d1` 确认了 Microsoft Public License。该结论只覆盖被审计仓库/提交及许可证所覆盖的代码，不能自动授权：

- 本任务的 `kpxp` 仓库固定快照；
- 完整游戏下载中的地图、头像、音乐、音效、字体、视频和 UI；
- MOD/DLC 或第三方素材；
- 《中华三国志》名称与标志。

## 本轮执行

- 没有把外部仓库放入 Unity `Assets/`；
- 没有复制或翻译外部 C#/XNA 代码；
- 没有复制地图数据、河流数据、贴图、颜色表、字体、音乐或 UI；
- 只记录模块分工、方法职责和战略地图所解决的问题；
- Style D 的特征提取、Profile、Shader、相机与测试均在本项目现有架构内独立编写。

未来若要直接使用任一外部文件，必须重新固定仓库、提交、文件级许可证和资产来源，并按许可证隔离登记。
