import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repo = process.env.MANDATE_REPO_ROOT ? path.resolve(process.env.MANDATE_REPO_ROOT) : path.resolve(scriptDir, "../..");
const docRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "DEVELOPMENT_PLACE_ROSTER_V1");
const registryRoot = path.join(repo, "Docs", "KNOWLEDGE_BASE", "REGISTRY");
const outputRoot = path.join(repo, "outputs", "DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1");
const previewRoot = path.join(outputRoot, "previews");
const beforePreviewRoot = path.join(outputRoot, "previews_before_registry_update");
const inspectRoot = path.join(outputRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(outputRoot, "development_place_roster_workdata.json"), "utf8"));
const kbBase = JSON.parse(await fs.readFile(path.join(repo, "outputs", "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1", "knowledge_base_workdata.json"), "utf8"));
const prior = JSON.parse(await fs.readFile(path.join(repo, "outputs", "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1", "administrative_seat_world_state_workdata.json"), "utf8"));

await fs.mkdir(docRoot, { recursive: true });
await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(beforePreviewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  { key: "roster", file: "01_DEVELOPMENT_PLACE_ROSTER.xlsx", title: "Development Place Roster V1", purpose: "正式冻结重点地点制作深度、优先级和开发波次；不是历史等级或运行时事实。" },
  { key: "historical_state_plan", file: "02_DEVELOPMENT_PLACE_HISTORICAL_STATE_PLAN.xlsx", title: "Development Place Historical State Plan", purpose: "逐Place选择需要专项支持的Scenario与Major ChangePoint，不复制13套完整世界。" },
  { key: "readiness", file: "03_DEVELOPMENT_PLACE_REFERENCE_READINESS_MATRIX.xlsx", title: "Development Place Reference Readiness Matrix", purpose: "独立评估地理、人口、人物、家族、设施、Cell、美术与运行时准备度。" },
  { key: "blockers", file: "04_DEVELOPMENT_PLACE_BLOCKER_REGISTER.xlsx", title: "Development Place Blocker Register", purpose: "区分历史研究、数据映射、设计和实现阻塞。" },
  { key: "region_slices", file: "05_DEVELOPMENT_REGION_SLICE_CANDIDATES.xlsx", title: "Development Region Slice Candidates", purpose: "CanonicalPlace + Road + Cell范围的开发工作包；不是新世界实体。" },
  { key: "wave_plan", file: "06_DEVELOPMENT_WAVE_PLAN_V1.xlsx", title: "Development Wave Plan V1", purpose: "按准备度、复用、系统覆盖和成本安排项目顺序；不是历史价值排名。" },
  { key: "d4_d5", file: "07_D4_D5_PLACE_MASTER.xlsx", title: "D4 / D5 Place Master", purpose: "真正需要独立Manifest的深度制作地点。" },
  { key: "d2_d3", file: "08_D2_D3_ACCESSIBLE_PLACE_MASTER.xlsx", title: "D2 / D3 Accessible Place Master", purpose: "玩家可访问地点及重要地区玩法中心，不要求旗舰制作深度。" },
  { key: "nonurban", file: "09_NON_URBAN_STRATEGIC_PLACE_MASTER.xlsx", title: "Non-Urban Strategic Place Master", purpose: "关隘、港渡、要塞、战场和交通点；MilitarySpace不自动变成CanonicalPlace。" },
  { key: "reference_gaps", file: "10_DEVELOPMENT_PLACE_REFERENCE_GAP_PRIORITY.xlsx", title: "Development Place Reference Gap Priority", purpose: "只保留会阻塞已批准开发波次的资料缺口。" },
];

const registrySpecs = [
  { base: "b01_document_registry", prior: "document_registry", update: "document_registry", file: "PROJECT_DOCUMENT_REGISTRY.xlsx", title: "Project Document Registry" },
  { base: "b02_domain_map", prior: "domain_map", update: "domain_map", file: "PROJECT_CANONICAL_DOMAIN_MAP.xlsx", title: "Project Canonical Domain Map" },
  { base: "b03_design_decisions", prior: "design_decisions", update: "design_decisions", file: "DESIGN_DECISION_REGISTRY.xlsx", title: "Design Decision Registry" },
  { base: "b04_open_decisions", prior: "open_decisions", update: "open_decisions", file: "OPEN_DECISION_REGISTRY.xlsx", title: "Open Decision Registry" },
  { base: "b06_implementation_gaps", prior: "implementation_gaps", update: "implementation_gaps", file: "IMPLEMENTATION_GAP_REGISTER.xlsx", title: "Implementation Gap Register" },
  { base: "b07_research_gaps", prior: "research_gaps", update: "research_gaps", file: "RESEARCH_GAP_REGISTER.xlsx", title: "Research Gap Register" },
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
  if (/Notes|Description|Decision|Reason|Why|Action|Gap|Blocker|Reference|Scope|Value|Systems|Included|Missing|Required/.test(header)) return 42;
  if (/Id$|Ids$|Place|Scenario|Role|State|Status|Readiness|Priority|Wave|Depth/.test(header)) return 26;
  if (/Year|Count|Score|Cost|Severity/.test(header)) return 15;
  return 20;
}

function safeName(value) {
  const ascii = value.replace(/[^A-Za-z0-9_-]/g, "_").replace(/_+/g, "_").replace(/^_+|_+$/g, "") || "sheet";
  let hash = 2166136261;
  for (const char of value) hash = Math.imul(hash ^ char.codePointAt(0), 16777619) >>> 0;
  return `${ascii.slice(0, 60)}_${hash.toString(16)}`;
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
    fill: "#344F42", font: { bold: true, color: "#FFFFFF", size: 10 }, wrapText: true,
    rowHeight: 38, verticalAlignment: "center",
  };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = {
    font: { color: "#202820", size: 9 }, wrapText: true, verticalAlignment: "top",
  };
  for (let row = 2; row <= matrix.length; row++) {
    if (row % 2 === 0) sheet.getRange(`A${row}:${endCol}${row}`).format.fill = "#F7F3E8";
  }
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, tableName.slice(0, 240));
  for (let i = 0; i < headers.length; i++) {
    const column = colName(i);
    sheet.getRange(`${column}:${column}`).format.columnWidth = widthFor(headers[i]);
    if (/Year|Count|Score/.test(headers[i])) sheet.getRange(`${column}2:${column}${matrix.length}`).format.numberFormat = "#,##0";
  }
  return { headers, endCol, rowCount: rows.length };
}

function addSummary(wb, title, purpose, rows, authority = "L2 Current Development Input") {
  const sheet = wb.worksheets.getItem("说明");
  sheet.showGridLines = false;
  sheet.getRange("A1:H1").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H1").format = { fill: "#294539", font: { bold: true, color: "#FFFFFF", size: 18 }, rowHeight: 36, verticalAlignment: "center" };
  const details = [
    ["Purpose", purpose], ["Records", null], ["Authority", authority],
    ["Depth contract", "D0-D5是项目制作深度，不是行政等级、城市等级或历史事实。"],
    ["Identity contract", "CanonicalPlace、Cell与Facility稳定身份不因DevelopmentDepth或Wave改变。"],
    ["Evidence contract", "UNKNOWN不等于NONE；Reference/Candidate不等于Runtime Implementation。"],
    ["Wave contract", "Wave只表示项目顺序，不表示历史重要性和世界层级。"],
    ["Runtime boundary", "本轮不生成新Place、Facility、Person、FamilyCenter、ChangePackage或Save迁移。"],
  ];
  for (let i = 0; i < details.length; i++) {
    const row = i + 3;
    sheet.getRange(`A${row}`).values = [[details[i][0]]];
    sheet.getRange(`B${row}:H${row}`).merge();
    if (details[i][1] !== null) sheet.getRange(`B${row}`).values = [[details[i][1]]];
  }
  sheet.getRange("B4").formulas = [[`=COUNTA('数据'!A2:A${rows + 1})`]];
  sheet.getRange("A3:A10").format = { fill: "#D9E4DA", font: { bold: true, color: "#24342A" }, verticalAlignment: "top" };
  sheet.getRange("B3:H10").format = { fill: "#F7F2E7", font: { color: "#2A312D" }, wrapText: true, verticalAlignment: "top" };
  sheet.getRange("A3:H10").format.borders = { preset: "outside", style: "thin", color: "#BFB9AA" };
  sheet.getRange("A:A").format.columnWidth = 19;
  sheet.getRange("B:H").format.columnWidth = 22;
}

function addSources(wb) {
  const rows = data.sources.map(row => ({
    SourceId: row.source_id, SourceType: row.source_type, Title: row.title,
    URLOrLocator: row.url_or_locator, EvidenceScope: row.evidence_scope, LicenseNote: row.license_note,
  }));
  const sheet = wb.worksheets.add("来源");
  writeDataset(sheet, rows, "TDevelopmentPlaceSources");
}

async function renderAndInspect(wb, prefix) {
  const sheetNames = wb.worksheets.items.map(sheet => sheet.name);
  for (const sheetName of sheetNames) {
    const preview = await wb.render({ sheetName, range: "A1:J30", scale: 1, format: "png" });
    await fs.writeFile(path.join(previewRoot, `${safeName(prefix)}_${safeName(sheetName)}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
  const inspection = await wb.inspect({ kind: "workbook,sheet,table", maxChars: 9000, tableMaxRows: 7, tableMaxCols: 12, tableMaxCellChars: 120 });
  const errors = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 300 }, summary: "final formula error scan" });
  await fs.writeFile(path.join(inspectRoot, `${safeName(prefix)}.inspect.ndjson`), inspection.ndjson, "utf8");
  await fs.writeFile(path.join(inspectRoot, `${safeName(prefix)}.formula-scan.ndjson`), errors.ndjson, "utf8");
  return { sheetNames, errors: errors.ndjson };
}

const report = { workbooks: [], registries: [], formulaErrors: 0, previews: 0 };
for (const spec of specs) {
  const rows = data[spec.key];
  const wb = Workbook.create();
  wb.worksheets.add("说明");
  const dataSheet = wb.worksheets.add("数据");
  writeDataset(dataSheet, rows, `T${safeName(spec.key)}`);
  addSources(wb);
  addSummary(wb, spec.title, spec.purpose, rows.length);
  const outputPath = path.join(docRoot, spec.file);
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(outputPath);
  const verification = await renderAndInspect(wb, spec.key);
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(verification.errors)) report.formulaErrors += 1;
  report.previews += verification.sheetNames.length;
  report.workbooks.push({ file: path.relative(repo, outputPath).replaceAll("\\", "/"), records: rows.length, sheets: verification.sheetNames });
}

function mergeRows(baseRows, priorRows, currentRows) {
  const keyField = Object.keys(baseRows[0])[0];
  const merged = new Map(baseRows.map(row => [row[keyField], row]));
  for (const row of priorRows ?? []) merged.set(row[keyField], row);
  for (const row of currentRows ?? []) merged.set(row[keyField], row);
  return [...merged.values()];
}

for (const spec of registrySpecs) {
  const existingPath = path.join(registryRoot, spec.file);
  const blob = await FileBlob.load(existingPath);
  const current = await SpreadsheetFile.importXlsx(blob);
  for (const sheetName of ["说明", "数据"]) {
    const preview = await current.render({ sheetName, range: "A1:J25", scale: 1, format: "png" });
    await fs.writeFile(path.join(beforePreviewRoot, `${safeName(spec.file)}_${safeName(sheetName)}.png`), new Uint8Array(await preview.arrayBuffer()));
  }
  const rows = mergeRows(kbBase[spec.base], prior.registry_updates[spec.prior], data.registry_updates[spec.update]);
  const wb = Workbook.create();
  wb.worksheets.add("说明");
  const dataSheet = wb.worksheets.add("数据");
  writeDataset(dataSheet, rows, `T${safeName(spec.update)}`);
  addSummary(wb, spec.title, "知识库治理登记；保留既有记录并追加Development Place Roster V1。", rows.length, "L2 Project Governance Registry");
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(existingPath);
  const verification = await renderAndInspect(wb, `registry_${spec.update}`);
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(verification.errors)) report.formulaErrors += 1;
  report.previews += verification.sheetNames.length;
  report.registries.push({ file: path.relative(repo, existingPath).replaceAll("\\", "/"), records: rows.length, added: data.registry_updates[spec.update].length });
}

await fs.writeFile(path.join(outputRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2) + "\n", "utf8");
console.log(JSON.stringify(report, null, 2));
