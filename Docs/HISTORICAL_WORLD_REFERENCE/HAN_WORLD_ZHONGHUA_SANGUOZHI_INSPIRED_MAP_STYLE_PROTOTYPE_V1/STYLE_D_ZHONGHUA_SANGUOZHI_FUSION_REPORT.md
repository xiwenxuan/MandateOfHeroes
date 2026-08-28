# STYLE D 《中华三国志》启发融合原型报告

## 实现

稳定 ID：`art.han-world.zhonghua-sanguozhi-fusion.v1`。

每个现有 Terrain mesh 生成后，`ZhonghuaFusionTerrainFeatureAnalyzer` 只读源高程和 `NaturalSurfaceBlend`，计算：

- 梯度坡度：相邻高程中心差分；
- 局部起伏：3×3 最大高程减最小高程；
- 凸度：中心高程减四邻均值；正值形成 ridge，负值形成 valley；
- 山体：高程 smoothstep 与坡度、起伏融合；
- 平原：低山体、低坡度、低起伏的互补权重；
- 森林面：forest/sparse woodland 的主次地表权重；
- 河谷：river/lake/riverbank/wetland 与 valley/plain 融合；
- basin：valley 与 plain 的组合。

结果写入 mesh 的 UV1/UV2 表现通道；Shader 读取后分别控制山体土石色、连续森林色、河谷蓝绿色、平原土黄色以及脊亮谷暗。Domain、Simulation、Persistence、Global Cell、DEM 和河流几何没有被修改。

Style D 在 REGION 关闭单独 canopy quad 批次，用地表特征表达整片林区；这不删除森林事实，切回 A/B/C 后旧 canopy 表现仍可生成。

## 固定镜头

`CAM_STYLE_D_WORLD`、`CAM_STYLE_D_REGION`、`CAM_STYLE_D_MOUNTAIN`、`CAM_STYLE_D_RIVER`、`CAM_STYLE_D_FOREST`、`CAM_STYLE_D_PLAIN`、`CAM_STYLE_D_WORLD_TO_REGION_MID`、`CAM_STYLE_D_CITY_DISTANCE_PREVIEW`。

CURRENT 与 Style D 的 WORLD/REGION 比较使用完全相同的 camera preset。

## 证据

正式运行：`outputs/20260816-1732-style-d/`。

- 10 张 1280×720 Game View 均通过非空、亮度跨度和颜色细节断言；
- CURRENT WORLD 与 Style D WORLD 文件不同；CURRENT REGION 与 Style D REGION 文件不同；
- EditMode 2/2、PlayMode 1/1；
- Style D REGION `vegetation_batches=0`，而 CURRENT REGION 为 1；
- 采样 WORLD Terrain 生成约 1.53s，REGION 约 0.147–0.156s；这些是测试机采样，不是正式性能预算。

## 人工视觉自审

已成立：全国尺度山地/平原分区更强，区域尺度是连续 3D，山体形成较明显连片体块，森林不再显示为树点，河流跨尺度可见，平原不再是单一绿色。

仍有限制：河流 ribbon 在部分急弯/地形交界出现锯齿和三角形断续；森林面主要靠色调而缺少中近景层次；2km mesh 在 CITY 距离仍显粗；没有城市、道路、建筑或军队图标。本阶段必须保持 `PROTOTYPE_READY`，不能写成最终美术完成。

```text
STYLE_D_ZHONGHUA_SANGUOZHI_FUSION_PROTOTYPE_READY
NATIONWIDE_ROLLOUT = BLOCKED_PENDING_USER_APPROVAL
HENAN_YIN_HIGH_DETAIL = BLOCKED_PENDING_USER_APPROVAL
LUOYANG_CITY = BLOCKED_PENDING_USER_APPROVAL
```

## 最终验证（2026-08-16）

全工程编译、ProjectLoadSmoke、Style D EditMode `2/2`、PlayMode `1/1` 均通过。完整核心回归使用固定测试清单分为 12 组运行，累计 `709/709` 通过；统一长跑曾超时，该次结果未被冒充为通过，最终以分组汇总 `style-d-final-20260816` 为正式证据。
