# Core City Development Packs V1

本目录把第一批10个重点城市从“已有历史资料”提升为可直接供后续开发任务消费的城市切片。Pack引用人口、人物、Clan、Scenario和Facility的Canonical母库，不复制第二套世界事实。

## 第一批

- [洛阳（place.han140.sili.henan.luoyang）](LUOYANG/README.md)：DEVELOPMENT_READY，完整度 96/100。
- [长安（place.han140.sili.jingzhao.changan）](CHANGAN/README.md)：READY_WITH_MODELED_GAPS，完整度 86/100。
- [邺（place.han140.jizhou.wei.ye）](YE/README.md)：READY_WITH_MODELED_GAPS，完整度 85/100。
- [许昌（place.han140.yuzhou.yingchuan.xu）](XU/README.md)：READY_WITH_MODELED_GAPS，完整度 88/100。
- [成都（place.han140.yizhou.shu.chengdu）](CHENGDU/README.md)：READY_WITH_MODELED_GAPS，完整度 84/100。
- [襄阳（place.han140.jingzhou.nan.xiangyang）](XIANGYANG/README.md)：READY_WITH_MODELED_GAPS，完整度 85/100。
- [江陵（place.han140.jingzhou.nan.jiangling）](JIANGLING/README.md)：READY_WITH_MODELED_GAPS，完整度 84/100。
- [建业（place.han140.yangzhou.danyang.moling）](JIANYE/README.md)：READY_WITH_MODELED_GAPS，完整度 83/100。
- [合肥（place.han140.yangzhou.jiujiang.hefei）](HEFEI/README.md)：READY_WITH_MODELED_GAPS，完整度 79/100。
- [南郑（place.han140.yizhou.hanzhong.nanzheng）](HANZHONG_CANONICAL_PLACE/README.md)：READY_WITH_MODELED_GAPS，完整度 80/100。

## 使用顺序

1. 先读[Development Pack Standard](CITY_DEVELOPMENT_PACK_STANDARD_V1.md)。
2. 通过`CanonicalPlaceId`进入对应城市Pack。
3. 核对`DEVELOPMENT_READINESS.md`与`SOURCES_AND_UNKNOWNS.md`。
4. 只有Pack通过后，才可提出DevelopmentDepth调整；仍须用户/开发计划确认。
5. 确认后另开Runtime/Cell/Facility/Population/Family/Unity任务；Pack本身不修改存档或运行世界。

## 长期边界

- 72个DevelopmentPlaceRoster是V1计划，不是永久白名单。
- D0—D5是可调整的制作深度，不是历史城市等级。
- Pack Ready不等于自动升格，也不等于运行时已经实现。
- 升格只补资料与表现/实现精度，不得删除、重建或重新随机既有世界对象。
- 汉中使用战略Label“汉中”，实际CanonicalPlace为`place.han140.yizhou.hanzhong.nanzheng`（南郑）。

## 汇总工作簿

根目录8份工作簿分别覆盖完整度、人物、家族、Facility、供给网络、人口分层、历史状态和未来升格Registry。验收结论见[完整度报告](CORE_CITY_DEVELOPMENT_PACK_COMPLETENESS_REPORT_V1.md)。

## FDRP V1 兼容说明

首批 10 城 Pack 继续作为详细历史证据输入保留；所有 72 个正式地点的当前统一标准和 T1—T4 术语由 [`../PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md`](../PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS/README.md) 负责。旧 Pack 分数与 D 档不自动转换为运行时实现，也不覆盖新主表的三个独立状态维度。
