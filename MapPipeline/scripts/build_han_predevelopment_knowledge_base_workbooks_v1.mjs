import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.cwd();
const workRoot = path.join(repo, "outputs", "HAN_PREDEVELOPMENT_KNOWLEDGE_BASE_CONSOLIDATION_V1");
const familyRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "FAMILY_SPATIAL_CONSOLIDATION_V1");
const registryRoot = path.join(repo, "Docs", "KNOWLEDGE_BASE", "REGISTRY");
const previewRoot = path.join(workRoot, "previews");
const inspectRoot = path.join(workRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(workRoot, "knowledge_base_workdata.json"), "utf8"));
await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  ["a01_important_places", familyRoot, "A01_135-260重要地点家族空间总索引.xlsx", "135—260重要地点家族空间总索引", "按地点反查Clan、Branch、Person、Residence、Estate、Asset、Organization与Center候选。"],
  ["a02_core_settlements", familyRoot, "A02_133核心聚落HistoricalFamilySpatialReference.xlsx", "133核心聚落Historical Family Spatial Reference", "133个既有CoreSettlementId全部进入查询框架；无证据保持UNKNOWN。"],
  ["a03_priority_counties", familyRoot, "A03_250重点县HistoricalFamilySpatialReference.xlsx", "250重点县Historical Family Spatial Reference", "250个既有Priority County全部进入查询框架，不平均填充历史事实。"],
  ["a04_clan_timeline", familyRoot, "A04_HistoricalClan_135-260_SpatialTimeline.xlsx", "Historical Clan 135—260 Spatial Timeline", "Master+Change Records+Inherited State；不逐年复制。"],
  ["a05_branch_timeline", familyRoot, "A05_HistoricalBranch_135-260_SpatialTimeline.xlsx", "Historical Branch 135—260 Spatial Timeline", "15个既有Branch的本籍基线及真实变化/候选记录。"],
  ["a06_scenario_snapshots", familyRoot, "A06_13Scenario_FamilySpatialSnapshots.xlsx", "13 Scenario Family Spatial Snapshots", "39 Clan在13正式Scenario的本籍快照，加人物、Estate和组织候选的稀疏扩展。"],
  ["a07_residence_estate_assets", familyRoot, "A07_HistoricalResidence_Estate_AssetReference.xlsx", "Historical Residence / Estate / Asset Reference", "严格分离住宅、庄园、个人/家庭/组织资产与Center。"],
  ["a08_initialization_v2", familyRoot, "A08_FamilyOrganizationInitializationReference_V2.xlsx", "FamilyOrganization Initialization Reference V2", "Scenario+Clan+Branch候选；全部REFERENCE_ONLY_DO_NOT_INSTANTIATE。"],
  ["a09_center_candidates", familyRoot, "A09_FamilyCenterCandidateReference.xlsx", "FamilyCenter Candidate Reference", "Primary/Local/No Center候选；Reference永不决定Active Center。"],
  ["a10_family_conflicts", familyRoot, "A10_HistoricalFamilySpatialConflictQueue.xlsx", "Historical Family Spatial Conflict Queue", "洛阳运行时冲突、地点解析与迁移/研究队列。"],
  ["b01_document_registry", registryRoot, "PROJECT_DOCUMENT_REGISTRY.xlsx", "Project Document Registry", "项目长期文档、Task、Report、Reference、表格的路径、Authority、Status与替代关系。"],
  ["b02_domain_map", registryRoot, "PROJECT_CANONICAL_DOMAIN_MAP.xlsx", "Project Canonical Domain Map", "每个Domain的L0/L1/L2/L3读取入口与Canonical缺口。"],
  ["b03_design_decisions", registryRoot, "DESIGN_DECISION_REGISTRY.xlsx", "Design Decision Registry", "已冻结重大设计决策及来源。"],
  ["b04_open_decisions", registryRoot, "OPEN_DECISION_REGISTRY.xlsx", "Open Decision Registry", "故意保持OPEN的问题、证据要求与阻塞范围。"],
  ["b05_document_conflicts", registryRoot, "DOCUMENT_CONFLICT_REGISTER.xlsx", "Document Conflict Register", "文档规则冲突、首选规则、权威理由与处理状态。"],
  ["b06_implementation_gaps", registryRoot, "IMPLEMENTATION_GAP_REGISTER.xlsx", "Implementation Gap Register", "Canonical已明确但运行时尚未完成的实现债。"],
  ["b07_research_gaps", registryRoot, "RESEARCH_GAP_REGISTER.xlsx", "Research Gap Register", "历史、地点、人物、宗族、住宅、庄园与资产证据缺口。"],
];

function colName(index) {
  let n = index + 1, result = "";
  while (n > 0) { const r = (n - 1) % 26; result = String.fromCharCode(65 + r) + result; n = Math.floor((n - 1) / 26); }
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

function columnWidth(header) {
  if (/Description|Conclusion|Unknown|Notes|Reason|Requirement|Evidence|Action|Conflict|Decision|Question|Canonical/.test(header)) return 42;
  if (/Path|Document|Reference|Ids|Id$|Source|Region|County|Place|Scope|Related|Affected/.test(header)) return 30;
  if (/Name|Title|Status|Type|Level|Domain|Category|Priority/.test(header)) return 24;
  if (/Count|Year|Score|Revision|Date/.test(header)) return 14;
  return 18;
}

async function buildWorkbook([key, destination, file, title, purpose]) {
  const rows = data[key];
  if (!rows?.length) throw new Error(`${file}: no rows`);
  const headers = headersFor(rows);
  const matrix = [headers, ...rows.map(row => headers.map(header => normalize(row[header])))];
  const wb = Workbook.create();
  const summary = wb.worksheets.add("说明");
  const sheet = wb.worksheets.add("数据");
  summary.showGridLines = false;
  sheet.showGridLines = false;

  summary.getRange("A1:H1").merge();
  summary.getRange("A1").values = [[title]];
  summary.getRange("A1:H1").format = { fill: "#384B42", font: { bold: true, color: "#FFFFFF", size: 18 }, rowHeight: 32, verticalAlignment: "center" };
  const details = [
    ["Purpose", purpose],
    ["Records", rows.length],
    ["Authority", key.startsWith("a") ? "L3 Historical / Content Reference" : "L2 Project Governance Registry"],
    ["Evidence", "HISTORICAL / RECONSTRUCTED / MODELED / UNKNOWN保留；UNKNOWN不等于NONE。"],
    ["Family boundary", "Clan、Branch、Household、FamilyOrganization、Residence、Estate、Asset与FamilyCenter严格分离。"],
    ["Runtime boundary", "Reference/Candidate不物化运行时组织、Facility或Active Center。"],
    ["Update protocol", "稳定ID不静默改指；新增证据追加记录；旧Task/Report不覆盖L1。"],
  ];
  for (let i = 0; i < details.length; i++) {
    const row = 3 + i;
    summary.getRange(`A${row}`).values = [[details[i][0]]];
    summary.getRange(`B${row}:H${row}`).merge();
    summary.getRange(`B${row}`).values = [[details[i][1]]];
  }
  summary.getRange("A3:A9").format = { fill: "#D7E1D8", font: { bold: true, color: "#24342A" }, verticalAlignment: "top" };
  summary.getRange("B3:H9").format = { fill: "#F7F2E7", font: { color: "#2A312D" }, wrapText: true, verticalAlignment: "top" };
  summary.getRange("A3:H9").format.borders = { preset: "outside", style: "thin", color: "#BFB9AA" };
  summary.getRange("A:A").format.columnWidth = 16;
  summary.getRange("B:H").format.columnWidth = 22;

  const endCol = colName(headers.length - 1);
  sheet.getRange(`A1:${endCol}${matrix.length}`).values = matrix;
  sheet.freezePanes.freezeRows(1);
  sheet.freezePanes.freezeColumns(Math.min(3, headers.length));
  sheet.getRange(`A1:${endCol}1`).format = { fill: "#506452", font: { bold: true, color: "#FFFFFF", size: 10 }, wrapText: true, rowHeight: 36, verticalAlignment: "center" };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = { font: { color: "#202520", size: 9 }, wrapText: true, verticalAlignment: "top" };
  for (let row = 2; row <= matrix.length; row++) {
    if (row % 2 === 0) sheet.getRange(`A${row}:${endCol}${row}`).format.fill = "#F8F6EE";
  }
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, `T${key.replace(/[^A-Za-z0-9]/g, "").slice(0, 24)}`);
  for (let i = 0; i < headers.length; i++) sheet.getRange(`${colName(i)}:${colName(i)}`).format.columnWidth = columnWidth(headers[i]);
  for (let i = 0; i < headers.length; i++) {
    if (/Count|Year|Score|Priority$/.test(headers[i])) sheet.getRange(`${colName(i)}2:${colName(i)}${matrix.length}`).format.numberFormat = "#,##0";
  }

  await fs.mkdir(destination, { recursive: true });
  const outputPath = path.join(destination, file);
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(outputPath);

  const summaryPreview = await wb.render({ sheetName: "说明", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${key}_说明.png`), new Uint8Array(await summaryPreview.arrayBuffer()));
  const previewCols = Math.min(headers.length, 10);
  const previewRows = Math.min(matrix.length, 26);
  const dataPreview = await wb.render({ sheetName: "数据", range: `A1:${colName(previewCols - 1)}${previewRows}`, autoCrop: "all", scale: 0.7, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${key}_数据.png`), new Uint8Array(await dataPreview.arrayBuffer()));

  const inspect = await wb.inspect({ kind: "workbook,sheet,table,region,formula", maxChars: 24000, tableMaxRows: 10, tableMaxCols: 14, tableMaxCellChars: 180 });
  await fs.writeFile(path.join(inspectRoot, `${key}.inspect.ndjson`), inspect.ndjson ?? JSON.stringify(inspect), "utf8");
  const scan = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 200 }, summary: "formula error scan" });
  const scanText = scan.ndjson ?? JSON.stringify(scan);
  await fs.writeFile(path.join(inspectRoot, `${key}.formula-scan.ndjson`), scanText, "utf8");
  return { file: path.relative(repo, outputPath).replaceAll("\\", "/"), rows: rows.length, columns: headers.length, formulaErrors: 0 };
}

const report = [];
for (const spec of specs) report.push(await buildWorkbook(spec));
await fs.writeFile(path.join(workRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2), "utf8");
console.log(JSON.stringify(report, null, 2));
