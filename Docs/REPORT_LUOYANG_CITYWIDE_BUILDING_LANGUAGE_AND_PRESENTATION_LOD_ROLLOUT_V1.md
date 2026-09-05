# 洛阳全城建筑语言与 Presentation LOD 推广 V1 实施报告

状态：`IMPLEMENTED_TARGET_VERIFICATION_PASSED_READY_FOR_USER_REVIEW`

## 已完成

- 新增确定性的 `CountyCitywideBuildingLanguagePlan`，对 2,084 项正式 Facility 中的 1,056 项建筑型
  Facility 建立五族只读表现记录。
- 扩充 Profile 的 Definition 覆盖，使乡村住宅、庄园、商铺、客舍、商旅院、驿站、学校、医馆、
  公仓和商业货栈不再落入错误的默认住宅样式。
- 158 项 Major Facility 保留现有正式模型身份；Golden Block 范围由样板街区接管，不重复绘制。
- Far 聚合轮廓开始读取 Profile 主体比例和屋顶形状；官署使用四坡轮廓，仓廪使用长脊，工坊使用低坡。
- Mid 新增最多 11 组共享 Mesh：三类地面、围墙、建筑主体、木构、三类屋顶、活动小品和植被。
- Mid 的正式模型集合补入全部 Far Landmark，避免通用上下文层关闭聚合后遗漏具名地标。
- Near 继续使用既有正式模型和 96 项上限，没有增加逐 Facility Collider、Animator 或 Update。
- 新增 2 个核心合同测试、1 个 Unity EditMode 测试和 1 个 Unity PlayMode 测试。

## 世界不变量

没有修改 512km²、204,800 个 50m Cell、2,084 项 Facility、布局指纹、人口、库存、生产、市场、
日期、行政归属或 World Schema V79。新增层为 `derived presentation only`。

## 验证

| 门禁 | 结果 |
|---|---|
| 全工程编译 | 通过 |
| 定向 Core | 2/2 通过 |
| Unity EditMode | 精确目标 1/1 通过；汇总：`tmp/unity-citywide-building-language-v1-final-editmode/unity-EditMode-20260905-093624-548.summary.json` |
| Unity PlayMode | 图形化精确目标 1/1 通过；汇总：`tmp/unity-citywide-building-language-v1-final-playmode/unity-PlayMode-20260905-093935-432.summary.json` |
| `git diff --check` | 被既有四个 P0Final FBX `.meta` 尾随空格阻塞 |

本任务文件的定向尾随空格检查和 tracked scoped `git diff --check` 均通过；全局失败未由本任务新增。
首次沙箱内 EditMode 启动因 45 秒内没有启动日志被安全终止；按测试规则在沙箱外唯一重试后通过，
PlayMode 也在同一受控入口下通过，两个 Unity 进程均自然退出且未关闭用户程序。

这是目标验证，不是完整 Core/EditMode/PlayMode 回归，也不是用户视觉接受或最终美术批准。
