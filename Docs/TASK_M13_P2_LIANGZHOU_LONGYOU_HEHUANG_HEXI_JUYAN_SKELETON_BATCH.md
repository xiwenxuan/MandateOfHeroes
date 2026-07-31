# M13-P2任务书：凉州陇右—河湟—河西走廊—居延边地稳定地理骨架

## 一、任务定位

本任务继续执行
[`TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md`](TASK_M13_HAN_140_POPULATION_AND_STABLE_GEOGRAPHY.md)
的P2阶段，覆盖凉州刺史部陇西、汉阳、武都、金城、安定、北地、武威、
张掖、酒泉、敦煌十郡以及张掖属国、张掖居延属国。完成后，凉州十二项
人口来源全部具有临时稳定地理映射。

本批按高原、河谷、山地、绿洲走廊和内陆河尾闾建立稳定地理身份：

- `geo.region.northwest.china.longyouweishui`：陇右黄土高原与渭水上游；
- `geo.region.northwest.china.longnanqinba`：陇南山地与秦巴北缘；
- `geo.region.northwest.china.hehuangyellowupper`：河湟谷地与黄河上游；
- `geo.region.northwest.china.loessnorthwest`：陇东宁南黄土高原与鄂尔多斯南缘；
- `geo.region.northwest.china.hexicorridor`：河西走廊与祁连山北麓绿洲；
- `geo.region.northwest.china.juyanblackriverlower`：黑河下游与居延绿洲。

## 二、交付范围

| 140年行政来源 | 新稳定地理ID | 稳定显示名 | 父级 |
|---|---|---|---|
| 陇西郡 | `geo.region.northwest.china.longyouweishui.centraltaoweivalleys` | 洮河渭河上游与陇中盆谷地理区 | 陇右黄土高原与渭水上游宏区 |
| 汉阳郡 | `geo.region.northwest.china.longyouweishui.southeastweishuibasin` | 渭水上游与陇山东麓地理区 | 陇右黄土高原与渭水上游宏区 |
| 武都郡 | `geo.region.northwest.china.longnanqinba.centraljialingmountaincorridor` | 嘉陵江上游与陇南山地走廊地理区 | 陇南山地与秦巴北缘宏区 |
| 金城郡 | `geo.region.northwest.china.hehuangyellowupper.easternyellowriverbasin` | 河湟东缘与黄河上游盆谷地理区 | 河湟谷地与黄河上游宏区 |
| 安定郡 | `geo.region.northwest.china.loessnorthwest.southcentraljinghehills` | 泾水上游与陇东宁南黄土丘陵地理区 | 陇东宁南黄土高原与鄂尔多斯南缘宏区 |
| 北地郡 | `geo.region.northwest.china.loessnorthwest.northeastordosmargin` | 鄂尔多斯南缘与黄土高原北部地理区 | 陇东宁南黄土高原与鄂尔多斯南缘宏区 |
| 武威郡 | `geo.region.northwest.china.hexicorridor.eastshiyanghebasin` | 石羊河流域与河西走廊东部绿洲地理区 | 河西走廊与祁连山北麓绿洲宏区 |
| 张掖郡 | `geo.region.northwest.china.hexicorridor.centralheiheoasis` | 黑河中游与河西走廊中部绿洲地理区 | 河西走廊与祁连山北麓绿洲宏区 |
| 酒泉郡 | `geo.region.northwest.china.hexicorridor.centralwestjiuquanoasis` | 河西走廊中西部与酒泉绿洲地理区 | 河西走廊与祁连山北麓绿洲宏区 |
| 敦煌郡 | `geo.region.northwest.china.hexicorridor.westdunhuangshulebasin` | 疏勒河下游与敦煌盆地绿洲地理区 | 河西走廊与祁连山北麓绿洲宏区 |
| 张掖属国 | `geo.region.northwest.china.hexicorridor.centralnorthfrontiercorridor` | 河西走廊中部北缘边地走廊地理区 | 河西走廊与祁连山北麓绿洲宏区 |
| 张掖居延属国 | `geo.region.northwest.china.juyanblackriverlower.northernterminaloasis` | 黑河下游与居延尾闾绿洲地理区 | 黑河下游与居延绿洲宏区 |

每个行政来源使用一条`single_provisional_commandery_bucket_v1`人口覆盖映射，
权重为10,000基点。本批新增6个宏区、12个`commandery_area`和12条映射。
所有坐标留空，`geometry_status=provisional`且`provisional=true`。

## 三、史料、异文、地理与人口约束

- 十二个行政来源继续使用P1校录的《后汉书》卷三十三户口记录；
- 十二项原始合计为102,491户、419,268口，有效合计为109,491户、465,899口；
- 十个郡继续按`commandery`保存，两个属国继续按`other`保存；
- 酒泉郡原始口数继续留空，46,631口只保存在显式修正字段；
- 敦煌郡原始748户继续保留，7,748户只保存在显式修正字段；
- 北地3,122户、武威10,042户及其公开转录异文继续保存在行政记录中；
- 现代地貌只用于临时索引，不表示东汉边界、属国辖境、屯田范围、古水系或
  绿洲面积已经精确复原。

现代地理交叉核对资料：

- 中国科学院地球环境研究所《彩色的自然长廊——河西走廊》：
  <https://ieexa.cas.cn/kp/kpwz/202203/t20220304_6385366.html>
- 甘肃政务服务网《定西市》自然地理概况：
  <https://zwfw.gansu.gov.cn/dingxi/tsfw/zsyzfwzq/QXGK/art/2023/art_cd82dc66c2a245a9981daf4a8c65e991.html>
- 湟源县人民政府河湟谷地资料：
  <https://www.huangyuan.gov.cn/index.php?c=show&id=2202&s=special>
- 张掖市人民政府黑河上中下游与居延海资料：
  <https://www.zhangye.gov.cn/zyszfxxgk/zfwj_5652/zfwj/agwzlfl/zzbf_5654/202009/t20200918_486693.html>

## 四、明确不做

- 不绘制精确郡界、属国界、县道边界、屯田区、古河道、绿洲范围或质心坐标；
- 不把现代甘肃、宁夏、青海、内蒙古、陕西或四川边界当作东汉边界；
- 不拆分人口到狄道、冀县、武都、允吾、临泾、富平、姑臧、觻得、禄福、
  敦煌或居延等县城节点；
- 不把张掖属国并入张掖郡，也不把张掖居延属国并入普通河西绿洲桶；
- 不覆盖酒泉、敦煌原值，不把北地、武威异文转化为新增人口修正；
- 不填入`game_location_crosswalk.csv`，该工作保留至P3；
- 不修改Unity场景、运行时地点、存档版本或永久人物；
- 不宣称P2全国105个郡国稳定映射已经完成。

## 五、验收标准

1. 稳定地理表累计127条，包含38个根宏区和89个郡国尺度子区；
2. 映射表累计89条，覆盖89个唯一行政来源；
3. 本批18个稳定ID和12个行政来源无遗漏、重复或孤立引用；
4. 六个宏区的直接子区数依次为2、1、1、2、5、1；
5. 凉州十郡、两属国人口来源全部拥有一条P2临时映射；
6. 原始合计仍为102,491户、419,268口，有效合计仍为109,491户、465,899口；
7. 酒泉缺口、敦煌疑户、北地与武威异文继续可审计；
8. 每个新增来源的映射权重严格等于10,000基点；
9. 新增几何与映射全部标为临时、坐标全部留空，游戏地点交叉表仍为空；
10. 专项数据验证、失败样例、确定性审计、全工程编译、核心回归与
    `git diff --check`通过。

## 六、完成记录

- 状态：已完成
- 完成日期：2026-07-31
- 稳定地理：累计127条，其中38个根宏区、89个郡国尺度子区；
- 人口映射：累计89条、错误0条；凉州十郡两属国覆盖12/12；
- 游戏地点交叉表：仍为0条，未提前进入P3；
- 存档影响：无
- Unity序列化影响：无
- 专项数据验证：通过（稳定地理127、映射89、交叉表0）；
- 专项回归测试：36/36通过；
- 全工程编译：通过；
- 核心回归测试：104/104通过；
- Unity测试：未运行；本批仅修改离线CSV、JSON审计产物、文档和
  PowerShell校验，不涉及Unity运行时、场景或序列化；
- `git diff --check`：通过；
- 下一阶段建议：继续P2第十四批，优先建立并州太行北段—汾河谷地—
  河套平原—阴山南麓稳定地理骨架。
