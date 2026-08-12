# 《统一地图美术规范》生成说明

## 依赖安装

需要 Python 3.11+ 与 `python-docx`。项目正式环境优先使用 Codex 工作区随附的 Python；如在独立环境运行，可执行：

```bash
python -m pip install python-docx
```

脚本不访问网络，也不引用网络图片。

## 生成命令

在仓库根目录执行：

```bash
python scripts/generate_map_art_spec.py
```

脚本会重复覆盖并自检：

```text
deliverables/03_统一地图美术规范.docx
```

自检失败时进程返回非零状态码。

## 文档章节结构

文档包含独立封面、文档控制页、自动目录、27章正文和4个附录。正文覆盖统一坐标、多尺度连续性、固定与动态分层、各层级美术、情报、地形道路、状态、PSB／PSD、输出、LOD、交接、中山连续样板与验收。

## 如何修改版本号

编辑 `scripts/generate_map_art_spec.py` 顶部的：

```python
VERSION = "V0.1"
```

版本调整后必须同步修改并登记 `02_地图坐标与对象锚点表.xlsx` 的版本信息。

## 如何修改文件编号

编辑脚本顶部的：

```python
DOC_ID = "MAP-ART-SPEC-001"
```

文件编号必须稳定、唯一，不使用“最终版”或“最新版”。

## 如何更新正文

正文由 `build_document()` 生成。优先修改对应章节的数据表、段落或清单，不要直接编辑生成后的DOCX作为唯一来源。修改后重新运行脚本，并完成结构检查与渲染检查。

## 如何替换插图占位框

搜索脚本中的 `add_figure_placeholder` 调用。正式插图确认来源、许可、版本和用途后，可将该调用替换为 `doc.add_picture(...)`，并保留原图号与图题。不得使用来源不明的网络图片；AI辅助图不得作为历史考据结论。

## 如何刷新Word目录

打开Word后右键目录：

```text
更新域
→ 更新整个目录
```

文档已设置“打开时更新域”，但首次打开仍建议人工确认页码。

## 如何与Excel版本对应

`02_地图坐标与对象锚点表.xlsx` 是对象ID、世界坐标、父子关系、锚点、LOD登记和状态编码的权威来源；本Word规范负责美术分层、视觉表现和资产输出。两份文件版本必须共同登记。发生冲突时，画师与程序都不得自行猜测或移动对象，由策划、地图负责人和美术负责人共同确认后同步升级两份文件。

## 如何运行质量检查

生成脚本内置检查：可重新打开、文件名、版本、文件编号、章节、附录、样式、目录域、分节、页眉页脚、表格和插图占位。项目文档检查执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1 `
  -DocumentationOnly
```

DOCX视觉检查使用文档工具包的 `render_docx.py` 渲染为逐页PNG，再检查乱码、裁切、表格、页眉页脚和异常空白页。

## 当前版本

```text
V0.1
文件编号：MAP-ART-SPEC-001
状态：第一版／待项目确认
```
