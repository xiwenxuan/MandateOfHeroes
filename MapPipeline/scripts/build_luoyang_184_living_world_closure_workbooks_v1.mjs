import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(artifactEntry).href);
const repo = "E:/project/gamedevelop/MandateOfHeroes";
const outputDir = path.join(repo, "outputs/LUOYANG_184_PERSON_WORK_PRODUCTION_CONSUMPTION_CLOSURE_V1");
const previewDir = path.join(outputDir, "previews/workbooks");
const evidence = JSON.parse(await fs.readFile(path.join(outputDir, "runtime_evidence.json"), "utf8"));
await fs.mkdir(previewDir, { recursive: true });

const statusRows = evidence.workforce_status.map(x => ({
  Status: x.status, Count: x.count,
  AverageEffectiveLaborBasisPoints: x.average_effective_labor_basis_points,
}));
const ageRows = evidence.workforce_age_bands.map(x => ({ AgeBand: x.age_band, Count: x.count }));
const workerSample = evidence.workforce_sample.map(x => ({
  PersonOrdinal: x.PersonOrdinal, HouseholdOrdinal: x.HouseholdOrdinal,
  FacilityIndex: x.FacilityIndex, OccupationIndex: x.OccupationIndex,
  ActivityIndex: x.ActivityIndex, Age: x.Age, Status: x.Status,
  EffectiveLaborBasisPoints: x.EffectiveLaborBasisPoints,
  FoodDemandMilliunits: x.CumulativeFoodDemandMilliunits,
  FoodConsumedMilliunits: x.CumulativeFoodConsumedMilliunits,
}));
const facilities = evidence.facilities.map(x => ({
  FacilityIndex: x.FacilityIndex, FacilityId: x.FacilityId,
  DefinitionId: x.DefinitionId, OwnerId: x.OwnerId, RecipeId: x.RecipeId,
  InputProductId: x.InputProductId, OutputProductId: x.OutputProductId,
  MinimumWorkers: x.MinimumWorkers, OptimalWorkers: x.OptimalWorkers,
  AssignedWorkers: x.AssignedWorkers, EffectiveWorkersBasisPoints: x.EffectiveWorkersBasisPoints,
  ProductionProgressBasisPoints: x.ProductionProgressBasisPoints,
  CycleStartedDay: x.CycleStartedDay, CycleDueDay: x.CycleDueDay,
  InputQuantity: x.InputQuantity, OutputQuantity: x.OutputQuantity,
  Status: x.Status, StopReasonId: x.StopReasonId, AiResponseActionId: x.AiResponseActionId,
}));
const crops = evidence.crops.map(x => ({
  FieldId: x.FieldId, FacilityIndex: x.FacilityIndex, FacilityId: x.FacilityId,
  CellId64: String(x.CellId64), CropProductId: x.CropProductId,
  StorageInventoryId: x.StorageInventoryId, PlantingDay: x.PlantingDay,
  FullMaturityDay: x.FullMaturityDay, EarlyHarvestMinimumBasisPoints: x.EarlyHarvestMinimumBasisPoints,
  MaturityBasisPoints: x.MaturityBasisPoints, FullYieldMilliunits: x.FullYieldMilliunits,
  AssignedWorkers: x.AssignedWorkers, Phase: x.Phase, HarvestedDay: x.HarvestedDay,
  ActualYieldMilliunits: x.ActualYieldMilliunits, StoredYieldMilliunits: x.StoredYieldMilliunits,
  LostYieldMilliunits: x.LostYieldMilliunits, HarvestQualityBasisPoints: x.HarvestQualityBasisPoints,
}));
const inventories = evidence.inventories.map(x => ({
  InventoryId: x.Id, OwnerKind: x.OwnerKind, OwnerId: x.OwnerId,
  FacilityId: x.FacilityId, ProductId: x.ProductId,
  QuantityMilliunits: x.QuantityMilliunits, CapacityMilliunits: x.CapacityMilliunits,
  TransitionalReferenceSupply: x.IsTransitionalReferenceSupply,
}));
const flows = evidence.inventory_flows.map(x => ({
  FlowId: x.Id, Day: x.Day, OperationId: x.OperationId, ProductId: x.ProductId,
  SourceInventoryId: x.SourceInventoryId, DestinationInventoryId: x.DestinationInventoryId,
  QuantityMilliunits: x.QuantityMilliunits, LossMilliunits: x.LossMilliunits,
  FacilityId: x.FacilityId, HouseholdOrdinal: x.HouseholdOrdinal, PersonId: x.PersonId,
}));
const allHouseholds = evidence.households.map(x => ({
  HouseholdOrdinal: x.HouseholdOrdinal, HeadPersonOrdinal: x.HeadPersonOrdinal,
  MemberStartOrdinal: x.MemberStartOrdinal, MemberCount: x.MemberCount, Wealth: x.Wealth,
  DailyFoodDemandMilliunits: x.DailyFoodDemandMilliunits,
  CumulativeFoodDemandMilliunits: x.CumulativeFoodDemandMilliunits,
  CumulativeFoodAcquiredMilliunits: x.CumulativeFoodAcquiredMilliunits,
  CumulativeFoodConsumedMilliunits: x.CumulativeFoodConsumedMilliunits,
  CumulativeFoodShortageMilliunits: x.CumulativeFoodShortageMilliunits,
  FoodSecurityBasisPoints: x.FoodSecurityBasisPoints,
  LastAcquisitionSourceId: x.LastAcquisitionSourceId, AiResponseActionId: x.AiResponseActionId,
}));
// The 80,899 authoritative rows remain in runtime_evidence.json and the
// checkpoint.  A stratified audit sample keeps the review workbook usable
// within the artifact runtime's bounded memory budget.
const households = allHouseholds.filter((_, index) =>
  index < 1000 || index >= allHouseholds.length - 1000 || index % 40 === 0);
const markets = evidence.markets.map(x => ({
  ProductId: x.ProductId, BasePrice: x.BasePrice, CurrentPriceBasisPoints: x.CurrentPriceBasisPoints,
  SupplyMilliunits: x.SupplyMilliunits, DemandMilliunits: x.DemandMilliunits,
  TransferredMilliunits: x.TransferredMilliunits, FailedDemandMilliunits: x.FailedDemandMilliunits,
}));
const snapshots = evidence.day_snapshots.map(x => ({
  Day: x.Day, FoodStockMilliunits: x.FoodStockMilliunits, FoodDemandMilliunits: x.FoodDemandMilliunits,
  FoodProducedMilliunits: x.FoodProducedMilliunits, FoodImportedMilliunits: x.FoodImportedMilliunits,
  FoodConsumedMilliunits: x.FoodConsumedMilliunits, FoodLostMilliunits: x.FoodLostMilliunits,
  FoodShortageMilliunits: x.FoodShortageMilliunits, ActiveProductionFacilities: x.ActiveProductionFacilities,
  IdleDueWorker: x.IdleDueWorker, IdleDueInput: x.IdleDueInput, OutputBlocked: x.OutputBlocked,
  HouseholdShortageCount: x.HouseholdShortageCount, HarvestableCrops: x.HarvestableCrops,
  MatureCrops: x.MatureCrops,
}));
const shortage = evidence.shortage_responses.map(x => ({
  Id: x.Id, SubjectKindId: x.SubjectKindId, SubjectId: x.SubjectId, ResourceId: x.ResourceId,
  Level: x.Level, ResponseActionId: x.ResponseActionId, DetectedDay: x.DetectedDay,
  DeficitMilliunits: x.DeficitMilliunits,
}));
const householdResponses = Object.values(allHouseholds.reduce((acc, x) => {
  const key = x.AiResponseActionId || "none";
  acc[key] ??= { AiResponseActionId: key, HouseholdCount: 0, ShortageMilliunits: 0 };
  acc[key].HouseholdCount++;
  acc[key].ShortageMilliunits += x.CumulativeFoodShortageMilliunits;
  return acc;
}, {}));
const conservation = [evidence.conservation];
const operationBalance = Object.values(flows.reduce((acc, x) => {
  acc[x.OperationId] ??= { OperationId: x.OperationId, FlowCount: 0, QuantityMilliunits: 0, LossMilliunits: 0 };
  acc[x.OperationId].FlowCount++;
  acc[x.OperationId].QuantityMilliunits += x.QuantityMilliunits;
  acc[x.OperationId].LossMilliunits += x.LossMilliunits;
  return acc;
}, {}));

const specs = [
  ["01_LUOYANG_WORKFORCE_RUNTIME_AUDIT.xlsx", "洛阳184劳动力运行审计", [["状态汇总", statusRows], ["年龄结构", ageRows], ["人物样本", workerSample]]],
  ["02_LUOYANG_FACILITY_PRODUCTION_RUNTIME_STATE.xlsx", "洛阳184设施生产运行状态", [["设施生产", facilities]]],
  ["03_LUOYANG_AGRICULTURE_CROP_CYCLE_AUDIT.xlsx", "洛阳184农业作物周期审计", [["作物周期", crops]]],
  ["04_LUOYANG_INVENTORY_FLOW_AUDIT.xlsx", "洛阳184库存流转审计", [["库存结余", inventories], ["流转账", flows]]],
  ["05_LUOYANG_HOUSEHOLD_CONSUMPTION_AUDIT.xlsx", "洛阳184家户消费审计（全量证据在JSON/检查点，表内为分层样本）", [["家户消费样本", households], ["家户响应汇总", householdResponses]]],
  ["06_LUOYANG_MARKET_SUPPLY_DEMAND_AUDIT.xlsx", "洛阳184市场供需审计", [["市场", markets], ["时间切片", snapshots]]],
  ["07_LUOYANG_FOOD_AND_BASIC_GOODS_BALANCE.xlsx", "洛阳184粮食与基本品平衡", [["年度时间切片", snapshots], ["库存结余", inventories]]],
  ["08_LUOYANG_SHORTAGE_AND_RESPONSE_AUDIT.xlsx", "洛阳184短缺与响应审计", [["当前短缺", shortage], ["家户响应汇总", householdResponses]]],
  ["10_LUOYANG_PRODUCTION_CONSUMPTION_CONSERVATION_AUDIT.xlsx", "洛阳184生产消费守恒审计", [["粮食守恒", conservation], ["操作汇总", operationBalance]]],
];

function col(index) {
  let n = index + 1, out = "";
  while (n > 0) { const r = (n - 1) % 26; out = String.fromCharCode(65 + r) + out; n = Math.floor((n - 1) / 26); }
  return out;
}
function scalar(value) {
  if (value === null || value === undefined) return "";
  if (Array.isArray(value)) return value.join("|");
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}
function headers(rows) {
  const result = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!result.includes(key)) result.push(key);
  return result.length ? result : ["Status"];
}
function writeData(sheet, rows, tableName) {
  sheet.showGridLines = false;
  const hs = headers(rows);
  const matrix = [hs, ...rows.map(row => hs.map(key => scalar(row[key])))];
  sheet.getRangeByIndexes(0, 0, matrix.length, hs.length).values = matrix;
  const end = col(hs.length - 1);
  sheet.getRange(`A1:${end}1`).format = { fill: "#183B4E", font: { bold: true, color: "#FFFFFF" }, wrapText: true, verticalAlignment: "center" };
  if (rows.length) {
    sheet.tables.add(`A1:${end}${matrix.length}`, true, tableName);
    sheet.getRange(`A2:${end}${matrix.length}`).format = { verticalAlignment: "top", borders: { preset: "all", style: "thin", color: "#D7E1E5" } };
  }
  sheet.freezePanes.freezeRows(1);
  hs.forEach((header, i) => {
    const lower = header.toLowerCase();
    let width = 17;
    if (lower.includes("id")) width = 34;
    if (lower.includes("reason") || lower.includes("response")) width = 36;
    sheet.getRange(`${col(i)}:${col(i)}`).format.columnWidth = width;
  });
  sheet.getRange("1:1").format.rowHeight = 34;
  return { full: `A1:${end}${matrix.length}`, preview: `A1:${end}${Math.min(matrix.length, 60)}` };
}
function writeCover(sheet, title, firstDataSheet) {
  sheet.showGridLines = false;
  sheet.getRange("A1:H2").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H2").format = { fill: "#173B4D", font: { bold: true, color: "#FFFFFF", size: 20 }, verticalAlignment: "center" };
  sheet.getRange("A4:B13").values = [
    ["指标", "值"], ["PermanentPerson", evidence.protected_counts.persons],
    ["Household", evidence.protected_counts.households], ["Facility", evidence.protected_counts.facilities],
    ["运行天数", evidence.summary.LastSimulatedDay], ["新增人物/家户/设施", "0 / 0 / 0"],
    ["供应状态", evidence.summary.SupplyStatusId], ["供应区域依赖", evidence.summary.SupplyRegionDependency],
    ["数据行数（公式）", ""], ["证据源", "runtime_evidence.json"],
  ];
  sheet.getRange("B12").formulas = [[`=COUNTA('${firstDataSheet}'!A:A)-1`]];
  sheet.getRange("A4:B4").format = { fill: "#2F687A", font: { bold: true, color: "#FFFFFF" } };
  sheet.getRange("A5:A13").format = { fill: "#DDEBF2", font: { bold: true, color: "#173B4D" } };
  sheet.getRange("A4:B13").format.borders = { preset: "all", style: "thin", color: "#B7C9D1" };
  sheet.getRange("A4:A13").format.columnWidth = 25;
  sheet.getRange("B4:B13").format.columnWidth = 68;
  sheet.getRange("A15:H18").merge();
  sheet.getRange("A15").values = [["边界：本表只审计受保护的400,000 Person、80,899 Household、2,084 Facility派生运行状态。不得从表格反向新增、合并、删除或重新随机人物；参考供应只来自既有5条供应链，短缺不会用假库存填补。"]];
  sheet.getRange("A15:H18").format = { fill: "#FFF2CC", font: { color: "#7F6000", italic: true }, wrapText: true, verticalAlignment: "center" };
}

const manifest = [];
for (let i = 0; i < specs.length; i++) {
  const [file, title, sheetSpecs] = specs[i];
  const workbook = Workbook.create();
  const cover = workbook.worksheets.add("说明");
  writeCover(cover, title, sheetSpecs[0][0]);
  const ranges = new Map([["说明", "A1:H18"]]);
  for (let j = 0; j < sheetSpecs.length; j++) {
    const [name, rows] = sheetSpecs[j];
    const sheet = workbook.worksheets.add(name);
    const range = writeData(sheet, rows, `T${i + 1}_${j + 1}`);
    ranges.set(name, range.preview);
  }
  const output = path.join(outputDir, file);
  await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
  const formulaErrors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 200 }, summary: "formula errors" });
  const keyRange = await workbook.inspect({ kind: "table", range: `说明!A1:B18`, include: "values,formulas", tableMaxRows: 20, tableMaxCols: 4, maxChars: 10000 });
  await fs.writeFile(path.join(previewDir, `${file}.inspect.txt`), `${formulaErrors.ndjson}\n${keyRange.ndjson}\n`, "utf8");
  for (const sheet of workbook.worksheets.items) {
    const preview = await workbook.render({ sheetName: sheet.name, range: ranges.get(sheet.name), autoCrop: "all", scale: 0.75, format: "png" });
    const previewPath = path.join(previewDir, `${file.replace(/\.xlsx$/i, "")}__${sheet.name}.png`);
    await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));
    manifest.push({ workbook: file, sheet: sheet.name, preview: previewPath.replaceAll("\\", "/") });
  }
}
await fs.writeFile(path.join(previewDir, "render_manifest.json"), JSON.stringify(manifest, null, 2), "utf8");
console.log(JSON.stringify({ status: "PASS", workbooks: specs.length, renderedSheets: manifest.length, outputDir }, null, 2));
