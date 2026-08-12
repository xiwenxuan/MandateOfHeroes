import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repo = process.env.MANDATE_REPO_ROOT ? path.resolve(process.env.MANDATE_REPO_ROOT) : path.resolve(scriptDir, "../..");
const taskRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "ADMINISTRATIVE_SEAT_AND_WORLD_STATE_V1");
const luoyangRoot = path.join(taskRoot, "11_LUOYANG_MAJOR_HISTORICAL_WORLD_STATES");
const registryRoot = path.join(repo, "Docs", "KNOWLEDGE_BASE", "REGISTRY");
const registryBaselineRoot = process.env.MANDATE_REGISTRY_BASELINE_ROOT ? path.resolve(process.env.MANDATE_REGISTRY_BASELINE_ROOT) : registryRoot;
const outputRoot = path.join(repo, "outputs", "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1");
const previewRoot = path.join(outputRoot, "previews");
const beforePreviewRoot = path.join(outputRoot, "previews_before_registry_update");
const inspectRoot = path.join(outputRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(outputRoot, "administrative_seat_world_state_workdata.json"), "utf8"));
const kbBase = JSON.parse(await fs.readFile(path.join(repo, "outputs", "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1", "knowledge_base_workdata.json"), "utf8"));

await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(beforePreviewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  { key: "administrative_seats", root: taskRoot, file: "01_135-260行政单位与重要历史治所总表.xlsx", title: "135—260行政单位与重要历史治所总表", purpose: "稀疏治所时间轴；HistoricalSeatReference不等于RuntimeAdministrativeSeat。", extras: [["州治Scenario", "province_scenario_seats"]] },
  { key: "canonical_places", root: taskRoot, file: "02_135-260_CanonicalPhysicalPlace_Master.xlsx", title: "135—260 Canonical Physical Place Master", purpose: "133个既有Core Settlement的物理地点母版，不创建第二套Place ID。", extras: [["名称时间线", "place_name_timeline"]] },
  { key: "strategic_crosswalk", root: taskRoot, file: "03_77战略名称与CanonicalPlace关系表.xlsx", title: "77战略名称与CanonicalPlace关系表", purpose: "战略显示名、行政区、治所和物理Place分离；全部77项逐条交叉。" },
  { key: "core_seat_crosswalk", root: taskRoot, file: "04_133CoreSettlement_SeatRole_Crosswalk.xlsx", title: "133 Core Settlement Seat Role Crosswalk", purpose: "133个既有Place在13 Scenario中的行政与战略角色。" },
  { key: "priority_places", root: taskRoot, file: "05_250PriorityCounty_ImportantPlace_And_SeatReference.xlsx", title: "250 Priority County Important Place and Seat Reference", purpose: "县是行政区域；县治和其他聚落/关隘/港渡/战场分别记录。" },
  { key: "scenario_snapshots", root: taskRoot, file: "06_13Scenario_ImportantPlace_WorldStateSnapshot_Index.xlsx", title: "13 Scenario Important Place World State Snapshot Index", purpose: "直接Scenario开局的状态索引；连续游玩不使用未来Snapshot校正。" },
  { key: "change_points", root: taskRoot, file: "07_HistoricalMajorChangePoint_Master.xlsx", title: "Historical Major ChangePoint Master", purpose: "只记录会显著改变世界空间状态的重大事件候选。" },
  { key: "change_packages", root: taskRoot, file: "08_HistoricalChangePackage_Reference.xlsx", title: "Historical ChangePackage Reference", purpose: "Canonical/Variant/Prevented/Transformed参考包；本轮不实现运行时代码。" },
  { key: "series_cross", root: taskRoot, file: "09_三国志系列重要地点名称交叉参考.xlsx", title: "三国志系列重要地点名称交叉参考", purpose: "只保留合法抽象重要性槽；未导入商业数据库、坐标、数值、UI、资产或文本。" },
  { key: "development_candidates", root: taskRoot, file: "10_DevelopmentRelevantPlaceCandidateMaster.xlsx", title: "Development Relevant Place Candidate Master", purpose: "未来开发候选全集，不在本任务决定最终A级/B级/C级Roster。" },
  { key: "luoyang_timeline", root: luoyangRoot, file: "LuoyangHistoricalStateTimeline.xlsx", title: "Luoyang Historical State Timeline", purpose: "184—223洛阳重要状态继承；始终复用同一Place/Cell/Facility ID。" },
  { key: "luoyang_changepoints", root: luoyangRoot, file: "LuoyangMajorChangePoints.xlsx", title: "Luoyang Major ChangePoints", purpose: "洛阳重大历史世界状态节点候选。" },
  { key: "luoyang_prepost", root: luoyangRoot, file: "Luoyang190PrePostReference.xlsx", title: "Luoyang 190 Pre/Post Reference", purpose: "将迁都/焚毁拆解到人口、组织、设施、库存和交通，不用单一破坏百分比。" },
  { key: "luoyang_facility_lifecycle", root: luoyangRoot, file: "LuoyangFacilityLifecycleReference.xlsx", title: "Luoyang Facility Lifecycle Reference", purpose: "重要Facility保持稳定ID；不确定后果使用MODELED或UNKNOWN。" },
  { key: "luoyang_population_migration", root: luoyangRoot, file: "LuoyangPopulationMigrationReference.xlsx", title: "Luoyang Population Migration Reference", purpose: "迁徙、逃亡、死亡和留居都作用于已有永久Person/Household。" },
  { key: "luoyang_person_family", root: luoyangRoot, file: "LuoyangPersonFamilyMovementReference.xlsx", title: "Luoyang Person / Family Movement Reference", purpose: "历史人物跨Scenario复用同一PermanentPersonId；FamilyOrganization不复制。" },
];

const registrySpecs = [
  { base: "b01_document_registry", update: "document_registry", file: "PROJECT_DOCUMENT_REGISTRY.xlsx", title: "Project Document Registry", purpose: "项目文档路径、Authority、Status和替代关系。" },
  { base: "b02_domain_map", update: "domain_map", file: "PROJECT_CANONICAL_DOMAIN_MAP.xlsx", title: "Project Canonical Domain Map", purpose: "Domain的L0/L1/L2/L3读取入口和Canonical缺口。" },
  { base: "b03_design_decisions", update: "design_decisions", file: "DESIGN_DECISION_REGISTRY.xlsx", title: "Design Decision Registry", purpose: "已冻结设计决策；本轮新增行政区、Place、Seat和历史世界状态规则。" },
  { base: "b04_open_decisions", update: "open_decisions", file: "OPEN_DECISION_REGISTRY.xlsx", title: "Open Decision Registry", purpose: "证据不足或需跨系统决定的问题保持OPEN。" },
  { base: "b05_document_conflicts", update: "document_conflicts", file: "DOCUMENT_CONFLICT_REGISTER.xlsx", title: "Document Conflict Register", purpose: "旧Location/历史锚点粗模型与当前Canonical规则的冲突。" },
  { base: "b06_implementation_gaps", update: "implementation_gaps", file: "IMPLEMENTATION_GAP_REGISTER.xlsx", title: "Implementation Gap Register", purpose: "Reference已明确但Runtime尚未实现的Place/Seat/ChangePackage合同。" },
  { base: "b07_research_gaps", update: "research_gaps", file: "RESEARCH_GAP_REGISTER.xlsx", title: "Research Gap Register", purpose: "战略映射、治所、系列出现与洛阳190设施证据缺口。" },
];

function colName(index) {
  let n = index + 1;
  let result = "";
  while (n > 0) {
    const r = (n - 1) % 26;
    result = String.fromCharCode(65 + r) + result;
    n = Math.floor((n - 1) / 26);
  }
  return result;
}

function normalize(value) {
  if (value === undefined || value === null) return null;
  if (Array.isArray(value)) return value.join("|");
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}

function headersFor(rows) {
  const headers = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!headers.includes(key)) headers.push(key);
  return headers;
}

function widthFor(header) {
  if (/Notes|Description|Interpretation|Implication|Policy|Unknown|Change|Expected|Reason|Evidence|Conflict|Question|Requirement|DoNot|Reference/.test(header)) return 42;
  if (/Path|Source|Id$|Ids$|Place|County|Region|Facility|Administrative|Scenario/.test(header)) return 30;
  if (/Name|Title|Status|Type|Level|Role|Priority|State/.test(header)) return 24;
  if (/Count|Year|From|To|Score|Date/.test(header)) return 14;
  return 18;
}

function safeName(value) {
  const ascii = value.replace(/[^A-Za-z0-9_-]/g, "_").replace(/_+/g, "_").replace(/^_+|_+$/g, "") || "sheet";
  let hash = 2166136261;
  for (const char of value) hash = Math.imul(hash ^ char.codePointAt(0), 16777619) >>> 0;
  return `${ascii.slice(0, 64)}_${hash.toString(16)}`;
}

function writeDataset(sheet, rows, tableName) {
  if (!rows?.length) throw new Error(`${sheet.name}: no rows`);
  const headers = headersFor(rows);
  const matrix = [headers, ...rows.map(row => headers.map(header => normalize(row[header])))];
  const endCol = colName(headers.length - 1);
  sheet.showGridLines = false;
  sheet.getRange(`A1:${endCol}${matrix.length}`).values = matrix;
  sheet.freezePanes.freezeRows(1);
  sheet.freezePanes.freezeColumns(Math.min(3, headers.length));
  sheet.getRange(`A1:${endCol}1`).format = {
    fill: "#425A4A",
    font: { bold: true, color: "#FFFFFF", size: 10 },
    wrapText: true,
    rowHeight: 38,
    verticalAlignment: "center",
  };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = {
    font: { color: "#202820", size: 9 },
    wrapText: true,
    verticalAlignment: "top",
  };
  for (let row = 2; row <= matrix.length; row++) {
    if (row % 2 === 0) sheet.getRange(`A${row}:${endCol}${row}`).format.fill = "#F8F5EA";
  }
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, tableName.slice(0, 250));
  for (let i = 0; i < headers.length; i++) {
    const column = colName(i);
    sheet.getRange(`${column}:${column}`).format.columnWidth = widthFor(headers[i]);
    if (/Count|Year|ValidFrom|ValidTo|Score/.test(headers[i])) {
      sheet.getRange(`${column}2:${column}${matrix.length}`).format.numberFormat = "#,##0";
    }
  }
  return { headers, rows: matrix.length, endCol };
}

function addSummary(wb, title, purpose, dataRows, sourceCount, authority = "L3 Historical / Content Reference") {
  const sheet = wb.worksheets.getItem("说明");
  sheet.showGridLines = false;
  sheet.getRange("A1:H1").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H1").format = { fill: "#304B3B", font: { bold: true, color: "#FFFFFF", size: 18 }, rowHeight: 34, verticalAlignment: "center" };
  const details = [
    ["Purpose", purpose],
    ["Records", null],
    ["Sources", sourceCount],
    ["Authority", authority],
    ["Evidence", "HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN；UNKNOWN不等于NONE。"],
    ["Identity", "稳定ID不因改名、Role、Scenario或状态变化而改变。"],
    ["Runtime boundary", "Reference/Candidate不自动创建或覆盖运行时Place、Facility、Person、Organization或Seat。"],
    ["Update protocol", "Scenario Snapshot + Major ChangePoint + Inherited State；不逐年复制世界。"],
  ];
  for (let i = 0; i < details.length; i++) {
    const row = i + 3;
    sheet.getRange(`A${row}`).values = [[details[i][0]]];
    sheet.getRange(`B${row}:H${row}`).merge();
    if (details[i][1] !== null) sheet.getRange(`B${row}`).values = [[details[i][1]]];
  }
  sheet.getRange("B4").formulas = [[`=COUNTA('数据'!A2:A${dataRows + 1})`]];
  sheet.getRange("A3:A10").format = { fill: "#D9E4DA", font: { bold: true, color: "#24342A" }, verticalAlignment: "top" };
  sheet.getRange("B3:H10").format = { fill: "#F7F2E7", font: { color: "#2A312D" }, wrapText: true, verticalAlignment: "top" };
  sheet.getRange("A3:H10").format.borders = { preset: "outside", style: "thin", color: "#BFB9AA" };
  sheet.getRange("A:A").format.columnWidth = 18;
  sheet.getRange("B:H").format.columnWidth = 22;
  return sheet;
}

function addSources(wb) {
  const rows = data.sources.map(row => ({
    SourceId: row.source_id,
    SourceType: row.source_type,
    Title: row.title,
    URLOrLocator: row.url_or_locator,
    EvidenceScope: row.evidence_scope,
    LicenseNote: row.license_note,
  }));
  const sheet = wb.worksheets.add("来源");
  writeDataset(sheet, rows, "TSourceReference");
}

async function renderAndInspect(wb, prefix) {
  const sheetNames = wb.worksheets.items.map(sheet => sheet.name);
  for (const sheetName of sheetNames) {
    const preview = await wb.render({ sheetName, range: "A1:J30", scale: 1, format: "png" });
    await fs.writeFile(path.join(previewRoot, `${safeName(prefix)}_${safeName(sheetName)}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
  const inspect = await wb.inspect({ kind: "workbook,sheet,table", maxChars: 10000, tableMaxRows: 6, tableMaxCols: 10, tableMaxCellChars: 120 });
  const errors = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 300 }, summary: "final formula error scan" });
  await fs.writeFile(path.join(inspectRoot, `${safeName(prefix)}.inspect.ndjson`), inspect.ndjson, "utf8");
  await fs.writeFile(path.join(inspectRoot, `${safeName(prefix)}.formula-scan.ndjson`), errors.ndjson, "utf8");
  return { sheetNames, formulaScan: errors.ndjson };
}

const report = { workbooks: [], registries: [], formulaErrors: 0 };

for (const spec of specs) {
  const rows = data[spec.key];
  const wb = Workbook.create();
  wb.worksheets.add("说明");
  const dataSheet = wb.worksheets.add("数据");
  writeDataset(dataSheet, rows, `T${safeName(spec.key)}`);
  for (const [sheetName, key] of spec.extras ?? []) {
    const sheet = wb.worksheets.add(sheetName);
    writeDataset(sheet, data[key], `T${safeName(key)}`);
  }
  addSources(wb);
  addSummary(wb, spec.title, spec.purpose, rows.length, data.sources.length);
  await fs.mkdir(spec.root, { recursive: true });
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  const outputPath = path.join(spec.root, spec.file);
  await xlsx.save(outputPath);
  const verification = await renderAndInspect(wb, spec.key);
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(verification.formulaScan)) report.formulaErrors += 1;
  report.workbooks.push({ file: path.relative(repo, outputPath).replaceAll("\\", "/"), records: rows.length, sheets: verification.sheetNames });
}

for (const spec of registrySpecs) {
  const existingPath = path.join(registryRoot, spec.file);
  const currentBlob = await FileBlob.load(path.join(registryBaselineRoot, spec.file));
  const currentWb = await SpreadsheetFile.importXlsx(currentBlob);
  for (const sheetName of ["说明", "数据"]) {
    const preview = await currentWb.render({ sheetName, range: "A1:J25", scale: 1, format: "png" });
    await fs.writeFile(path.join(beforePreviewRoot, `${safeName(spec.file)}_${safeName(sheetName)}.png`), new Uint8Array(await preview.arrayBuffer()));
  }

  const baseRows = kbBase[spec.base];
  const updateRows = data.registry_updates[spec.update];
  const keyField = Object.keys(baseRows[0])[0];
  const updateIds = new Set(updateRows.map(row => row[keyField]));
  const rows = [...baseRows.filter(row => !updateIds.has(row[keyField])), ...updateRows];
  const wb = Workbook.create();
  wb.worksheets.add("说明");
  const sheet = wb.worksheets.add("数据");
  writeDataset(sheet, rows, `T${safeName(spec.update)}`);
  addSummary(wb, spec.title, spec.purpose, rows.length, 0, "L2 Project Governance Registry");
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(existingPath);
  const verification = await renderAndInspect(wb, `registry_${spec.update}`);
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(verification.formulaScan)) report.formulaErrors += 1;
  report.registries.push({ file: path.relative(repo, existingPath).replaceAll("\\", "/"), baseRecords: baseRows.length, addedRecords: updateRows.length, totalRecords: rows.length });
}

await fs.writeFile(path.join(outputRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2) + "\n", "utf8");
console.log(JSON.stringify(report, null, 2));
