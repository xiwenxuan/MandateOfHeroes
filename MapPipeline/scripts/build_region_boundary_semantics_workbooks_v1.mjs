import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const entry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY;
if (!entry) throw new Error("MANDATE_ARTIFACT_TOOL_ENTRY is required");
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(entry).href);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repo = process.env.MANDATE_REPO_ROOT || path.resolve(scriptDirectory, "../..");
const output = path.join(repo, "outputs/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1");
const destination = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_REGION_CELL_BOUNDARY_AND_TECHNICAL_BLOCK_SEMANTICS_CORRECTION_V1");
const previewDir = path.join(output, "previews");
const inspectionDir = path.join(output, "inspections");
const workbookCopyDir = path.join(output, "workbooks");
await fs.mkdir(destination, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
await fs.mkdir(inspectionDir, { recursive: true });
await fs.mkdir(workbookCopyDir, { recursive: true });
const data = JSON.parse(await fs.readFile(path.join(output, "workbook_workdata.json"), "utf8"));

const specs = [
  ["01", "01_REGION_CELL_MEMBERSHIP_CONTRACT.xlsx", "Region Cell Membership Contract"],
  ["02", "02_REGION_CELL_EDGE_BOUNDARY_AUDIT.xlsx", "Region Cell Edge Boundary Audit"],
  ["03", "03_HENAN_YIN_REGION_CELL_MEMBERSHIP_AUDIT.xlsx", "Henan Yin Region Cell Membership Audit"],
  ["04", "04_TECHNICAL_BLOCK_SEMANTICS_AUDIT.xlsx", "Technical Block Semantics Audit"],
  ["05", "05_LEGACY_16X16_CHUNK_RECLASSIFICATION.xlsx", "Legacy 16x16 Chunk Reclassification"],
  ["06", "06_TERRAIN_STREAMING_BLOCK_DECISION_STATUS.xlsx", "Terrain Streaming Block Decision Status"],
  ["07", "07_REGION_BOUNDARY_NEIGHBOR_VALIDATION.xlsx", "Region Boundary Neighbor Validation"],
];

function columnName(index) {
  let value = index + 1;
  let result = "";
  while (value) {
    const remainder = (value - 1) % 26;
    result = String.fromCharCode(65 + remainder) + result;
    value = Math.floor((value - 1) / 26);
  }
  return result;
}

function headers(rows) {
  const result = [];
  for (const row of rows) for (const key of Object.keys(row)) if (!result.includes(key)) result.push(key);
  return result;
}

function normalize(value) {
  if (value === null || value === undefined) return null;
  if (typeof value === "boolean") return value ? "TRUE" : "FALSE";
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}

function safeName(value) {
  return value.replace(/[^A-Za-z0-9_-]/g, "_").slice(0, 72);
}

function writeData(sheet, rows, tableName) {
  const hs = headers(rows);
  const endColumn = columnName(hs.length - 1);
  const matrix = [hs, ...rows.map(row => hs.map(header => normalize(row[header])))];
  sheet.showGridLines = false;
  sheet.getRange(`A1:${endColumn}${matrix.length}`).values = matrix;
  sheet.freezePanes.freezeRows(1);
  sheet.freezePanes.freezeColumns(Math.min(3, hs.length));
  sheet.getRange(`A1:${endColumn}1`).format = {
    fill: "#294F45",
    font: { bold: true, color: "#FFFFFF", size: 10 },
    wrapText: true,
    rowHeight: 36,
    verticalAlignment: "center",
  };
  sheet.getRange(`A2:${endColumn}${matrix.length}`).format = {
    font: { color: "#25312C", size: 9 },
    verticalAlignment: "top",
  };
  for (let row = 2; row <= Math.min(matrix.length, 30000); row++) {
    if (row % 2 === 0) sheet.getRange(`A${row}:${endColumn}${row}`).format.fill = "#F6F1E6";
  }
  for (let column = 0; column < hs.length; column++) {
    const header = hs[column];
    let width = 18;
    if (/^Field$/.test(header)) width = 52;
    if (/^Value$/.test(header)) width = 42;
    if (/PermanentId|Authority|Purpose|Status|Name|Decision|Validation|Membership/.test(header)) width = 32;
    if (/PermanentId|TechnicalBlockId/.test(header)) width = 44;
    if (/GlobalX|GlobalY|Start|End|CellId64|Count|Row|Column/.test(header)) width = 20;
    sheet.getRange(`${columnName(column)}:${columnName(column)}`).format.columnWidth = width;
  }
  sheet.tables.add(`A1:${endColumn}${matrix.length}`, true, tableName);
  return { rowCount: rows.length, columnCount: hs.length, endColumn };
}

const report = [];
for (const [key, file, title] of specs) {
  const rows = data[key];
  if (!rows?.length) throw new Error(`${key} has no workbook data`);
  const workbook = Workbook.create();
  const intro = workbook.worksheets.add("说明");
  intro.showGridLines = false;
  intro.getRange("A1:H1").merge();
  intro.getRange("A1").values = [[title]];
  intro.getRange("A1:H1").format = {
    fill: "#24483E",
    font: { bold: true, color: "#FFFFFF", size: 17 },
    rowHeight: 40,
    verticalAlignment: "center",
  };
  intro.getRange("A3:B8").values = [
    ["Status", "REGION_CELL_BOUNDARY_CONTRACT_FROZEN"],
    ["Authority", "IncludedGlobalCellIds / complete Global Cells"],
    ["Semantic correction", "16x16 is technical spatial/simulation aggregation, not Terrain or Streaming"],
    ["Data rows", null],
    ["Stable identity", "No Global Cell or legacy 16x16 technical-block ID changed"],
    ["Next gate", "MAP-TERRAIN-STREAMING-BLOCK-SIZE-BENCHMARK-V1"],
  ];
  intro.getRange("A3:A8").format = { fill: "#DCE9E1", font: { bold: true, color: "#25372F" } };
  intro.getRange("B3:B8").format = { fill: "#F7F2E7", wrapText: true };
  intro.getRange("A:A").format.columnWidth = 25;
  intro.getRange("B:B").format.columnWidth = 70;

  const sheet = workbook.worksheets.add("数据");
  const meta = writeData(sheet, rows, `TRegionBoundary${key}`);
  intro.getRange("B6").formulas = [[`=COUNTA('数据'!A2:A${meta.rowCount + 1})`]];
  intro.getRange("B6").format.numberFormat = "#,##0";

  const xlsx = await SpreadsheetFile.exportXlsx(workbook);
  await xlsx.save(path.join(destination, file));
  await fs.copyFile(path.join(destination, file), path.join(workbookCopyDir, file));
  const preview = await workbook.render({
    sheetName: "数据",
    range: `A1:${meta.endColumn}${Math.min(meta.rowCount + 1, 15)}`,
    scale: 0.8,
    format: "png",
  });
  const previewName = `${key}_${safeName(title)}.png`;
  await fs.writeFile(path.join(previewDir, previewName), new Uint8Array(await preview.arrayBuffer()));
  const introPreview = await workbook.render({ sheetName: "说明", range: "A1:H8", scale: 0.9, format: "png" });
  await fs.writeFile(path.join(previewDir, `${key}_intro.png`), new Uint8Array(await introPreview.arrayBuffer()));
  const inspect = await workbook.inspect({ kind: "workbook,sheet,table", maxChars: 6000, tableMaxRows: 5, tableMaxCols: 12 });
  const errors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 100 },
    summary: "formula errors",
  });
  await fs.writeFile(path.join(inspectionDir, `${key}.inspect.ndjson`), inspect.ndjson, "utf8");
  await fs.writeFile(path.join(inspectionDir, `${key}.errors.ndjson`), errors.ndjson, "utf8");
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(errors.ndjson)) throw new Error(`${file} formula error`);
  report.push({ file, rows: meta.rowCount, columns: meta.columnCount, preview: path.join(previewDir, previewName) });
}

await fs.writeFile(path.join(output, "workbook_build_report.json"),
  JSON.stringify({ status: "PASS", workbooks: report }, null, 2) + "\n", "utf8");
console.log(JSON.stringify({
  status: "PASS",
  count: report.length,
  rows: report.reduce((sum, item) => sum + item.rows, 0),
}, null, 2));
