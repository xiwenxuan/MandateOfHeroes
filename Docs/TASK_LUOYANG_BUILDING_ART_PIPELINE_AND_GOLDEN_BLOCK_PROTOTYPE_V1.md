# 任务书：洛阳建筑美术生产管线与黄金街区原型 V1

## 一、任务定位

### 1.1 任务名称

**洛阳建筑美术生产管线与黄金街区原型 V1**

英文标识：`LUOYANG_BUILDING_ART_PIPELINE_AND_GOLDEN_BLOCK_PROTOTYPE_V1`

### 1.2 背景

洛阳县域已经具备 512km²、320×640 个 50m PlanningCell、2,084 项正式 Facility、道路、
水渠、城防和 Far/Mid/Near 表现层。54/54 Facility 模型槽也已有项目原创 Prefab、FBX 来源和
三层 LOD，但现有普通建筑与县域聚合层仍主要承担“模型覆盖”和“空间可读性”职责：建筑形体、
院落组织、材质变化、街区肌理和环境小品尚未达到正式城市美术质量。

用户提供的《城市：天际线》截图仅作为城市密度、道路—地块—建筑群组织和 LOD 思路参考；
不得复制其模型、贴图、UI、地图或其他商业资产。

### 1.3 本轮目标

在不改变任何世界事实的前提下，先完成一个可直接进入、可审图、可度量的洛阳黄金街区原型，
用它冻结后续普通建筑量产的表现规则，而不是立即无差别重做全部 54 类资产。

## 二、不可变边界

- 继续使用同一个 2km 世界与 50m 县域空间合同。
- 保持 512km²、204,800 个 PlanningCell 和 2,084 项 Facility 不变。
- 不新增或删除 Facility，不改变 FacilityId、DefinitionId、所有权、入口、正式 Footprint。
- 不改变人口、人物、家户、库存、生产、市场、钱粮、日期或行政归属。
- 不升级 World Schema V79，不写入存档。
- 样板街区的院间巷道、院墙、配房、摊位、树木均为 `derived presentation only`；
  它们不拥有 Road/Facility ID，也不提供通行、产能、容量或库存。
- 现有正式道路、水渠、城防和可选 Facility 仍是权威交互对象。

## 三、范围

### 3.1 400×400m 黄金街区

- 从洛阳 UrbanArea 内的正式 Facility 中，按 8×8 个 50m Cell 分桶。
- 优先选择设施类别最丰富、其次设施数量最多的区块；并以行列作稳定决胜。
- 保存完整来源 FacilityId 列表与确定性签名，保证同一布局包每次得到同一结果。
- 该区块替换自身原有 Far Aggregate，避免聚合方盒与新街区重叠。

### 3.2 五类模块化建筑族

1. 住宅院落 `ResidenceCourtyard`
2. 沿街市场 `MarketFrontage`
3. 工坊作业院 `WorkshopYard`
4. 仓廪复合院 `WarehouseCompound`
5. 官署/公共院落 `CivicCourtyard`

每类由夯土墙体、木构门架、主屋、配房、屋顶和类别小品组合；共提供 6 个稳定形体/朝向变化，
并使用暖瓦、深瓦、风化灰瓦三套共享色阶，降低重复感。

### 3.3 街区环境层

- 街区夯土地表；
- 十字主巷和次级院间小巷；
- 有入口缺口的院墙与门楼；
- 市场摊位、工坊作业物、仓院容器、官署台基；
- 确定性庭院树木；
- 阴影和现有暖/冷双光源适配。

### 3.4 玩家入口

县域主导航增加“样板街区”按钮。点击后：

- 保持县域主视角和 UrbanArea 子视图；
- 将镜头聚焦至黄金街区；
- 切入 Near 表现 LOD；
- 保留中键平移、右键旋转和滚轮缩放；
- 明确提示该街区为只读表现原型。

## 四、技术方案

### 4.1 数据与职责

- `CountyGoldenBlockPresentationPlan`：从正式布局包派生街区边界、来源 Facility、16 个表现 Lot、
  五类 Archetype、变体、朝向与稳定签名。
- `LuoyangCountyWorldSpacePresentationController`：以合批 Mesh 生成街区地面、巷道、墙体、建筑、
  木构、三套屋顶、道具和树木。
- `LuoyangCountyPlanningPresentationController`：提供聚焦样板街区的相机入口。
- `PlayableLuoyangGameController`：提供普通玩家可见的“样板街区”按钮。

### 4.2 性能口径

- 不建立逐 Cell GameObject。
- 16 个 Lot 按材质/部件合并为不超过 12 个 Renderer。
- 不给表现建筑增加 Collider、NavMeshObstacle、动画器或独立 Update。
- Far/Mid/Near 共用同一街区缓存，不因切换视角重建世界事实。

### 4.3 后续美术生产管线

本原型通过审图后，量产阶段按以下层次推进：

- P0 地标：宫殿、城门、太学、明堂等独特多部件资产；
- P1 功能建筑：住宅、市场、工坊、仓廪、官署等可交互 Facility Prefab；
- P2 城市肌理：配房、棚屋、院墙、摊位、树木和地表小品；
- P3 远景资产：合批轮廓、简化 Mesh 或 Impostor。

普通建筑建议使用共享夯土、木构、瓦、茅草、石材图集，并在量产前以真实目标设备验证面数、
材质数、阴影和 DrawCall 预算；本任务不提前把建议面数写成最终硬门槛。

## 五、验收标准

### 5.1 Core

- 同一布局两次生成的街区坐标、Lot、朝向、变体和签名完全一致。
- 街区固定为 8×8 Cell，Lot 固定为 16。
- 五类建筑族均出现。
- 所有来源 FacilityId 可在正式 2,084 项布局中追溯。
- 生成前后布局 Fingerprint 不变。

### 5.2 Unity EditMode

- 县域世界空间中存在 `Urban Fabric/Luoyang Golden Block V1`。
- 街区使用 8—12 个合批 Renderer。
- 点击聚焦后保持 UrbanArea，并进入 Near LOD。
- 正式 Facility 总数仍为 2,084。

### 5.3 人工审图

- 远景可辨识该块为连续街区，而不是一个大型方盒。
- 中景可识别院墙、巷道和不同屋顶色阶。
- 近景可辨识五类院落用途和小品差异。
- 不出现与正式 Facility 重叠的旧 Aggregate。
- 不把表现巷道或配房误写成正式道路/Facility。

## 六、非目标

- 最终 PBR 贴图、考古复原级建筑、室内、人物动画、碰撞和导航；
- 把样板街区一次性铺满整个洛阳；
- 正式施工事务、材料运输、工期、取消、拆除或 AI 建设；
- 修改人口容量、生产效率或市场玩法；
- 宣称已达到《城市：天际线》的最终画面质量。

## 七、交付状态定义

- 代码和定向 Core 通过，但 Unity 因用户编辑器打开而不能受控运行：
  `IMPLEMENTED_COMPILE_AND_TARGETED_CORE_PASSED_UNITY_BLOCKED_BY_OPEN_EDITOR`
- Unity EditMode 通过并完成人工审图前：
  `IMPLEMENTED_AUTOMATION_PASSED_READY_FOR_USER_REVIEW`
- 只有用户明确接受截图和操作效果后：`ACCEPTED`
