import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.cwd();
const workRoot = path.join(repo, "outputs", "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1");
const finalRoot = path.join(repo, "Docs", "FAMILY_ORGANIZATION_REFERENCE_V1");
const previewRoot = path.join(workRoot, "previews");
const inspectRoot = path.join(workRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(workRoot, "family_reference_workdata.json"), "utf8"));
await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  ["action_matrix", "03_FamilyManagement_Action_Matrix_V1.xlsx", "FamilyManagement动作权限矩阵", "区分个人行为、组织行为、Local、Primary、REMOTE、Facility与管理者要求。"],
  ["clan_spatial", "04_135-260重要HistoricalClan空间状态参考.xlsx", "135—260重要HistoricalClan空间状态参考", "39个Canonical Clan的本籍、Branch、成员与Estate层事实；不自动建立组织或中心。"],
  ["scenario_snapshots", "05_13Scenario_HistoricalFamilySpatialSnapshots.xlsx", "13 Scenario历史家族空间快照", "39 Clan×13剧本切片；Clan状态不得反推FamilyOrganization或FamilyCenter。"],
  ["initialization_reference", "06_FamilyOrganizationInitializationReference.xlsx", "FamilyOrganization初始化参考", "Scenario+Clan+Branch到候选组织边界的桥梁；所有行仅供审核，不实例化。"],
  ["residence_estate_assets", "07_HistoricalResidence_Estate_FamilyAsset_Reference.xlsx", "历史住宅、庄园与家族资产证据参考", "分离Residence、Estate、FamilyAsset与FamilyCenterEvidence；CanHost不等于已存在中心。"],
  ["luoyang_people", "08_184洛阳历史人物与家族空间参考.xlsx", "184洛阳历史人物与家族空间参考", "保留25人基线并增加研究/排除候选；只按证据定位，不强填精确Facility Cell。"],
  ["luoyang_org_audit", "09_184洛阳现有FamilyOrganization一致性审计.xlsx", "184洛阳现有FamilyOrganization一致性审计", "审计7个现有组织的成员、Clan、Facility与中心状态；不直接改写运行时。"],
  ["luoyang_center_candidates", "10_184洛阳FamilyCenter候选与开发建议.xlsx", "184洛阳FamilyCenter候选与开发建议", "给出Primary/Local/不指定建议；当前没有候选可直接物化。"],
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
  const seen = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!seen.includes(key)) seen.push(key);
  return seen;
}

async function buildWorkbook([key, file, title, purpose]) {
  const rows = data[key];
  if (!rows?.length) throw new Error(`${file}: no rows`);
  const headers = headersFor(rows);
  const wb = Workbook.create();
  const summary = wb.worksheets.add("说明");
  const sheet = wb.worksheets.add("数据");
  summary.showGridLines = false;
  sheet.showGridLines = false;
  summary.getRange("A1:H1").merge();
  summary.getRange("A1").values = [[title]];
  summary.getRange("A1:H1").format = { fill: "#4A5A42", font: { bold: true, color: "#FFFFFF", size: 18 }, rowHeight: 30, verticalAlignment: "center" };
  const details = [
    ["用途", purpose],
    ["记录数", rows.length],
    ["证据等级", "HISTORICAL=史实；RECONSTRUCTED=保守复原；MODELED=玩法/数据模型；UNKNOWN=待研究。"],
    ["核心边界", "Clan、Branch、Household、FamilyOrganization、FamilyCenter分离；成员存在不等于组织或中心存在。"],
    ["中心成立", "真实Facility + FamilyManagement能力 + 组织合法产权/控制 + Primary/Local指定 + 真实管理者Person。"],
    ["运行时边界", "本工作簿是开发参考，不创建全国组织、家户、资产、庄园或FamilyCenter。"],
  ];
  for (let i = 0; i < details.length; i++) {
    const row = 3 + i;
    summary.getRange(`A${row}`).values = [[details[i][0]]];
    summary.getRange(`B${row}:H${row}`).merge();
    summary.getRange(`B${row}`).values = [[details[i][1]]];
  }
  summary.getRange("A3:A8").format = { fill: "#D8E0D2", font: { bold: true, color: "#273225" }, verticalAlignment: "top" };
  summary.getRange("B3:H8").format = { fill: "#F5F0E3", font: { color: "#2D312B" }, wrapText: true, verticalAlignment: "top" };
  summary.getRange("A3:H8").format.borders = { preset: "all", style: "thin", color: "#C8C0AF" };
  summary.getRange("A:A").format.columnWidth = 15;
  summary.getRange("B:H").format.columnWidth = 21;

  const matrix = [headers, ...rows.map(row => headers.map(header => normalize(row[header])))];
  const endCol = colName(headers.length - 1);
  sheet.getRange(`A1:${endCol}${matrix.length}`).values = matrix;
  sheet.freezePanes.freezeRows(1);
  sheet.freezePanes.freezeColumns(Math.min(3, headers.length));
  sheet.getRange(`A1:${endCol}1`).format = { fill: "#627156", font: { bold: true, color: "#FFFFFF", size: 10 }, wrapText: true, rowHeight: 34, verticalAlignment: "center" };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = { font: { color: "#20231F", size: 9 }, wrapText: true, verticalAlignment: "top" };
  sheet.getRange(`A1:${endCol}${matrix.length}`).format.borders = { preset: "all", style: "thin", color: "#D6D0C4" };
  const tableName = `T${key.replace(/[^A-Za-z0-9]/g, "").slice(0, 20)}`;
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, tableName);
  for (let i = 0; i < headers.length; i++) {
    const header = headers[i], col = colName(i);
    let width = 18;
    if (/Id|Ids|Source|Region|Area|Facility|Reference/.test(header)) width = 30;
    if (/Notes|Reason|Conclusion|Unknown|Description|Requirement|Before|Advice|Rule/.test(header)) width = 42;
    if (/Name|Status|Kind|Category|Level|Grade/.test(header)) width = 24;
    sheet.getRange(`${col}:${col}`).format.columnWidth = width;
  }
  if (matrix.length > 1) {
    for (let row = 2; row <= matrix.length; row++) {
      if (row % 2 === 0) sheet.getRange(`A${row}:${endCol}${row}`).format.fill = "#FAF8F1";
    }
  }
  await fs.mkdir(finalRoot, { recursive: true });
  const output = path.join(finalRoot, file);
  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(output);
  const summaryPreview = await wb.render({ sheetName: "说明", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${key}_说明.png`), new Uint8Array(await summaryPreview.arrayBuffer()));
  const previewEndCol = colName(Math.min(headers.length, 9) - 1);
  const dataPreview = await wb.render({ sheetName: "数据", range: `A1:${previewEndCol}${Math.min(matrix.length, 26)}`, autoCrop: "all", scale: 0.72, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${key}_数据.png`), new Uint8Array(await dataPreview.arrayBuffer()));
  const inspect = await wb.inspect({ kind: "workbook,sheet,table,region,formula", maxChars: 24000, tableMaxRows: 10, tableMaxCols: 14, tableMaxCellChars: 160 });
  await fs.writeFile(path.join(inspectRoot, `${key}.inspect.ndjson`), inspect.ndjson ?? JSON.stringify(inspect), "utf8");
  const formulaScan = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
  const scanText = formulaScan.ndjson ?? JSON.stringify(formulaScan);
  await fs.writeFile(path.join(inspectRoot, `${key}.formula-scan.ndjson`), scanText, "utf8");
  const errors = ["#REF!", "#DIV/0!", "#VALUE!", "#NAME?", "#N/A"].filter(token => scanText.includes(token) && !scanText.includes('"matchCount":0'));
  if (errors.length) throw new Error(`${file}: formula errors ${errors.join(",")}`);
  return { file: path.relative(repo, output).replaceAll("\\", "/"), rows: rows.length, columns: headers.length, formulaErrors: 0 };
}

const report = [];
for (const spec of specs) report.push(await buildWorkbook(spec));
await fs.writeFile(path.join(workRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2), "utf8");
console.log(JSON.stringify(report, null, 2));
