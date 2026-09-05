# Unity Validation

状态：自动化 Unity 验收通过；人工盲玩未执行。

项目锁定 Unity `2022.3.62f3c1`，所有 Unity 运行均经 `Tools/Run-UnityTestsSafe.ps1`。

- 沙箱内 ProjectLoad 在生成启动日志前 120.815 秒阻塞，code 125；安全脚本终止了仅本次启动的 PID。
- 按项目 Skill 要求在沙箱外重试，同一 ProjectLoad 8.195 秒通过，证明项目、许可证和编译入口可用，阻塞属于 Codex 沙箱启动边界。
- EditMode 冻结批次：`m26product-unity-editmode-final3-g32-20260831`，指纹 `A14053737C5D5F607E03B1E37F0FC4AE2BB623160ACB94C8FC45A77A47E163C8`，1087/1087，0 失败。
- Gate F PlayMode 首次在沙箱内于启动日志生成前超时，安全入口返回 code 125 并只终止本次启动的 PID 17248；未关闭用户程序。最终源码在沙箱外同一正式过滤1/1通过，工具总耗时12.18秒、测试用例0.527秒；结果为 `tmp/unity-tests/m26-gate-f-final3-playmode-20260901/unity-PlayMode-20260901-114600-574.summary.json`。
- 最终 EditMode 正式过滤覆盖全国R003货运权威、途中存档续跑、V78→V79迁移和普通玩家文案防内部ID泄漏，4/4通过；工具总耗时42.68秒、测试用例合计3.690秒；结果为 `tmp/unity-tests/m26-gate-f-final3-editmode-20260901/unity-EditMode-20260901-114458-781.summary.json`。
- 首次 PlayMode 调用因错误命名空间过滤得到 0 执行、code 123；修正为真实全名后通过，不计为测试失败。

EditMode 过程中发现并修复 `HarvestWorkerTests` 未激活正式经济权威的夹具缺口；新指纹完整批次的第 31 组已通过。P0Batch4 来源清单随后按仓库实际文件重新冻结，独立审计 56/56、漂移 0，第 32 组通过。
