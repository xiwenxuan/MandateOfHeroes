# SPATIAL ORIGIN SUMMARY

1. 全国坐标系是什么？
   `Han World China-centered Albers Equal Area V0`；ID=`hanworld.albers.china.v0`；单位=`meter`。

2. 全国唯一 Global Origin 具体是多少？
   `(-3417344.395965772, 6199580.451937504)`。

3. Global Origin 具体代表哪个角点？
   `GLOBAL_GRID_NORTHWEST_CORNER`，即规则母格网和 Cell(0,0) 的西北/左上角。

4. Cell(0,0) 具体在哪里？
   ID=`cell.hanworld.v0.0`；Row=0；Column=0；范围 X=[-3417344.395965772, -3415344.395965772]、Y=[6197580.451937504, 6199580.451937504]；中心=(-3416344.395965772, 6198580.451937504)。

5. 全国 3314×2176 母格网实际坐标范围是多少？
   X=[-3417344.395965772, 3210655.604034228]，宽 6628000.0m；Y=[1847580.451937504, 6199580.451937504]，高 4352000.0m。当前没有单独缩小的 Valid Mask，Valid World Extent 等于 Grid Envelope。

6. 河南尹 Region Local Origin 具体是多少？
   Global=(262655.6040342278, 3511580.451937504)，定义为生产 Region 的 `SOUTHWEST_CORNER`。

7. 河南尹 Local(0,0) 对应全国哪个坐标？
   严格对应 Global=(262655.6040342278, 3511580.451937504)。

8. 河南尹 Local(0,0) 对应哪个 Global Cell？
   对应 `cell.hanworld.v0.4452542`（Row=1343, Column=1840）的西南角；它不是该 Cell 的中心。

9. 洛阳 Canonical Anchor 具体是多少？
   Place=`C027`；Global=(670561.5475446532, 3717065.2005044892)；Cell=`cell.hanworld.v0.4114717`（Row=1241, Column=2043）。该点为 `approximate/medium` 证据，不表示精确宫城。

10. 洛阳在河南尹局部坐标中具体是多少？
    Henan Local=(407905.9435104254, 205484.74856698513)。

## 三个固定抽样 Cell

| Sample | CellPermanentId | Row | Column | MinX | MinY | CenterX | CenterY | HenanLocalCenterX | HenanLocalCenterY |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| LUOYANG_URBAN_CANONICAL_ANCHOR_CELL | cell.hanworld.v0.4114717 | 1241 | 2043 | 668655.6040342278 | 3715580.451937504 | 669655.6040342278 | 3716580.451937504 | 407000.0 | 205000.0 |
| LUOYANG_OUTER_SUBURB_CELL | cell.hanworld.v0.4114731 | 1241 | 2057 | 696655.6040342278 | 3715580.451937504 | 697655.6040342278 | 3716580.451937504 | 435000.0 | 205000.0 |
| HENAN_YIN_FAR_OVERLAY_CELL | cell.hanworld.v0.4366390 | 1317 | 1852 | 286655.6040342278 | 3563580.451937504 | 287655.6040342278 | 3564580.451937504 | 25000.0 | 53000.0 |

## 公式核验

- Cell X/Y 公式最大误差：0.0m。
- Global → RegionLocal → Global 最大往返误差：0.0m。
- 全国 Grid 宽度差：0.0m；高度差：0.0m。
