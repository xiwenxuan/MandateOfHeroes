# 东汉全国自然地图美术方向与渲染 V1 正式报告

最终状态：`HAN_WORLD_ART_DIRECTION_V1_CANDIDATES_READY`。

`STYLE_A/B/C = READY_FOR_REVIEW`；`USER_SELECTED_STYLE = PENDING`；`NATIONWIDE_STYLE_ROLLOUT = BLOCKED_PENDING_USER_APPROVAL`。

## 已实现

- `HanWorldArtProfile` 统一覆盖地形 tint、垂直夸张、坡度/曲率/山脊/谷地、macro variation、河流宽度/色调、森林尺度/色调、Sun/Ambient/Fog 和 Camera 参数；
- STYLE A/B/C 共用 `Mandate/Natural Terrain V2` Shader、同一 DEM、Global Cell、河流、森林、Floating Origin 和 Cell Picking；
- `HanWorldArtDirectionLab` 可在 PlayMode 通过 STYLE A/B/C 按钮即时切换，不建立第二套世界；
- 固定三组 WORLD/REGION 相机，生成 18 张独立候选和 3 张三联图；
- Profile 切换测试证明同一局部原点拾取的 Global Cell 不变，112,880 个 8×8 Terrain Tile 索引不变；
- 12 份工作簿、视觉评价和运行性能样本已输出。

## 性能解释

三套候选均为 3 个材质、1 个 Shader Variant；WORLD 采样为 2 个 draw-call 级运行对象，REGION 为 12 个（含九个关闭地表绘制但保留驻留/碰撞语义的正式 Tile）。A/B/C 内存与批次数量相同，差异主要是 Profile 参数。当前图形批处理没有可靠 GPU timestamp，因此值为 0/Unavailable，未伪造。CPU 观察均值见工作簿；STYLE B 本轮样本最低，但差异不足以证明稳定排名。

## 27项最终回答

1. 已建立三套正式 Style Candidate。
2. 是，完全相同的世界事实数据；只改变 Presentation Profile。
3. A 是半写实自然、真实性基准。
4. B 是低饱和国风半写实战略沙盘。
5. C 是强化山河与态势识别的战略沙盘。
6. A 最接近真实自然。
7. B 最有中国历史题材气质。
8. C 战略地图可读性最好。
9. B 的题材、可读性和近景过渡最平衡；A 也易兼容；城市实物仍未证明。
10. 架构成本基本相同；本轮观察 B 最低，但不作为稳定结论。
11. A 的 REGION 观察值最高、C 的 WORLD 观察值最高；均更像采样波动而非结构差异。
12. 是，三套 WORLD 使用同一冻结 Camera。
13. 是，三套 REGION 使用同一冻结 Camera。
14. 是，Slope/Curvature/Ridge/Valley 和三档垂直夸张使山体比 V2 更有层次。
15. 河流已统一进入 Profile 色调、宽度和大气体系；深水/反射/细河岸仍为缺口。
16. 森林已进入统一色调与尺度；程序化树冠仍是明确缺口。
17. 是，三套分别建立 Sun、Ambient 与色温关系。
18. 是，WORLD/REGION 分别建立 near-mid-far 线性空气透视。
19. 已不明显像 GIS Viewer。
20. 仍能看出 Procedural Prototype，主要来自树冠、河岸和 2km 近景表面；因此不是最终美术。
21. STYLE B 已达到可审查的国风半写实方向。
22. STYLE B 不过于水墨，仍是统一 3D Terrain。
23. STYLE C 尚未过于卡通，但鲜绿与夸张是下一轮重点门禁。
24. STYLE A 不像卫星贴图，仍是项目自产程序化 3D 表面。
25. 用户最终选择：`PENDING_USER_DECISION`。
26. 没有推广全国正式风格。
27. 没有进入河南尹高精 Terrain。

## 推荐但非决定

`CODEX_RECOMMENDED_STYLE = STYLE_B`，原因是它在真实地理、题材识别、战略可读性和未来 3D 城市兼容之间最平衡。此推荐不等于用户选择。

## 停止条件

本阶段到此停止。用户选择 A、B、C 或提出修改方向前，不启动全国推广、河南尹高精 Terrain、洛阳城墙、建筑或道路。
