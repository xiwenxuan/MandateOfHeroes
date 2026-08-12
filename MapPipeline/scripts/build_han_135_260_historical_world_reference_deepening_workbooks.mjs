import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.cwd();
const workRoot = path.join(repo, "outputs", "HAN_135_260_HISTORICAL_WORLD_REFERENCE_DEEPENING_V1");
const finalRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "DEEPENING_V1");
const previewRoot = path.join(workRoot, "previews");
const inspectRoot = path.join(workRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(workRoot, "deepening_workdata.json"), "utf8"));
await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  ["core_settlements", "01_135-260核心历史聚落总索引.xlsx", "135—260核心历史聚落总索引", "13州治、105郡国治所候选与77战略城市去重后的Canonical Place网络。"],
  ["seat_timeline", "02_135-260州郡国县治所时间轴.xlsx", "135—260州郡国县治所时间轴", "稀疏治所角色时间轴；UNKNOWN与争议期不强填唯一治所。"],
  ["priority_counties", "03_135-260重点县开发参考索引.xlsx", "135—260重点县开发参考索引", "按核心聚落、历史人物、Clan与Estate Reference价值筛选，不以固定数量为目标。"],
  ["estate_references", "08_135-260历史豪族与庄园锚点总索引.xlsx", "135—260历史豪族与庄园锚点总索引", "Clan、Branch、Estate与FamilyOrganization严格分离；本表不物化运行时地产。"],
  ["industry_resources", "09_135-260重点产业与资源区域开发参考.xlsx", "135—260重点产业与资源区域开发参考", "州部产业与资源开发入口；不自动生成矿脉、田地或设施。"],
  ["transport_nodes", "10_135-260重点交通节点开发参考.xlsx", "135—260重点交通节点开发参考", "18条既有路线与31个节点的物流、旅行、军运和情报开发合同。"],
  ["military_spaces", "11_135-260重要军事空间与战役开发参考.xlsx", "135—260重要军事空间与战役开发参考", "15个战役/军事空间参考；区域不冒充精确战场边界。"],
  ["annual_changes", "13_135-260核心地点年度变化总索引.xlsx", "135—260核心地点年度变化总索引", "只记录变化；未列字段继承Master和上一条变化，避免逐年复制世界。"],
  ["sources", "14_历史世界深化资料来源总索引.xlsx", "历史世界深化资料来源总索引", "V1来源与本轮正史、学术、考古及项目合同入口；每条Claim仍需指向SourceId。"],
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

async function buildWorkbook({ key, file, title, purpose, rows, destination = finalRoot, reportKey = key }) {
  if (!rows.length) throw new Error(`${file}: no rows`);
  const headers = headersFor(rows);
  const wb = Workbook.create();
  const summary = wb.worksheets.add("说明");
  const sheet = wb.worksheets.add("数据");
  summary.showGridLines = false; sheet.showGridLines = false;
  summary.getRange("A1:H1").merge(); summary.getRange("A1").values = [[title]];
  summary.getRange("A1:H1").format = { fill: "#3E5141", font: { bold: true, color: "#FFFFFF", size: 18 }, rowHeight: 30, verticalAlignment: "center" };
  const details = [
    ["用途", purpose], ["记录数", rows.length],
    ["证据标签", "HISTORICAL=史实；RECONSTRUCTED=保守复原；MODELED=项目模型；UNKNOWN=待研究"],
    ["继承合同", "Master → 查询年之前最新Timeline/Change Event → Scenario Snapshot；未变化字段不得逐年复制。"],
    ["边界", "稳定ID不得静默改指；代理几何、模型人口和候选治所不冒充史实；260年不是世界终点。"],
  ];
  for (let i = 0; i < details.length; i++) {
    const row = 3 + i; summary.getRange(`A${row}`).values = [[details[i][0]]]; summary.getRange(`B${row}:H${row}`).merge(); summary.getRange(`B${row}`).values = [[details[i][1]]];
  }
  summary.getRange("A3:A7").format = { fill: "#D7E0D5", font: { bold: true, color: "#26342B" }, verticalAlignment: "top" };
  summary.getRange("B3:H7").format = { fill: "#F6F1E5", font: { color: "#2E332F" }, wrapText: true, verticalAlignment: "top" };
  summary.getRange("A3:H7").format.borders = { preset: "all", style: "thin", color: "#C8C1B4" };
  summary.getRange("A:A").format.columnWidth = 14; summary.getRange("B:H").format.columnWidth = 21;

  const matrix = [headers, ...rows.map(row => headers.map(h => normalize(row[h])))];
  const endCol = colName(headers.length - 1);
  sheet.getRange(`A1:${endCol}${matrix.length}`).values = matrix;
  sheet.freezePanes.freezeRows(1); sheet.freezePanes.freezeColumns(Math.min(2, headers.length));
  sheet.getRange(`A1:${endCol}1`).format = { fill: "#536B57", font: { bold: true, color: "#FFFFFF" }, wrapText: true, rowHeight: 32, verticalAlignment: "center" };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = { font: { color: "#222222", size: 10 }, wrapText: true, verticalAlignment: "top" };
  sheet.getRange(`A1:${endCol}${matrix.length}`).format.borders = { preset: "all", style: "thin", color: "#D9D4C8" };
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, `T${reportKey.replace(/[^A-Za-z0-9]/g, "").slice(0, 18)}`);
  for (let i = 0; i < headers.length; i++) {
    const h = headers[i], col = colName(i); let width = 16;
    if (/id|source|reference|region|county|place/.test(h)) width = 28;
    if (/notes|unknown|description|content|implication|scope|method/.test(h)) width = 42;
    if (/name|type|status|reason/.test(h)) width = 22;
    sheet.getRange(`${col}:${col}`).format.columnWidth = width;
  }
  await fs.mkdir(destination, { recursive: true });
  const xlsx = await SpreadsheetFile.exportXlsx(wb); await xlsx.save(path.join(destination, file));
  const summaryPreview = await wb.render({ sheetName: "说明", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${reportKey}_说明.png`), new Uint8Array(await summaryPreview.arrayBuffer()));
  const dataPreview = await wb.render({ sheetName: "数据", range: `A1:${colName(Math.min(headers.length, 8)-1)}${Math.min(matrix.length, 28)}`, autoCrop: "all", scale: 0.75, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${reportKey}_数据.png`), new Uint8Array(await dataPreview.arrayBuffer()));
  const inspect = await wb.inspect({ kind: "workbook,sheet,table,region,formula", maxChars: 16000, tableMaxRows: 8, tableMaxCols: 12, tableMaxCellChars: 120 });
  await fs.writeFile(path.join(inspectRoot, `${reportKey}.inspect.ndjson`), inspect.ndjson ?? JSON.stringify(inspect), "utf8");
  const computed = await wb.inspect({ kind: "region", sheetId: "说明", range: "A1:H9", maxChars: 5000 });
  const text = computed.ndjson ?? JSON.stringify(computed);
  const errors = ["#REF!", "#DIV/0!", "#VALUE!", "#NAME?", "#N/A"].filter(token => text.includes(token));
  if (errors.length) throw new Error(`${file}: formula errors ${errors.join(",")}`);
  return { file: path.relative(repo, path.join(destination, file)).replaceAll("\\", "/"), rows: rows.length, columns: headers.length, formulaErrors: 0 };
}

const report = [];
for (const [key, file, title, purpose] of specs) report.push(await buildWorkbook({ key, file, title, purpose, rows: data[key] }));

const p0Core = data.core_settlements.filter(row => row.priority === "P0");
for (const core of p0Core) {
  const cityId = core.city_ids.split("|").find(id => data.p0_reference.some(row => row.city_id === id));
  const cityName = core.city_names.split("|")[0] || core.display_name;
  const entries = data.p0_reference.filter(row => row.city_id === cityId);
  const dirs = await fs.readdir(path.join(finalRoot, "04_CORE_SETTLEMENTS"), { withFileTypes: true });
  const dir = dirs.find(d => d.isDirectory() && d.name.startsWith("P0_") && d.name.includes(core.display_name));
  if (!dir) throw new Error(`P0 directory missing: ${core.display_name}`);
  report.push(await buildWorkbook({ key: "p0_reference", file: "02-12_结构化时间轴与开发参考.xlsx", title: `${cityName}核心城市结构化参考`, purpose: "合并行政、人口、城市发展、人物、Clan/Branch、设施、事件与剧本切片入口；详见同目录00_Master。", rows: entries, destination: path.join(finalRoot, "04_CORE_SETTLEMENTS", dir.name), reportKey: `p0_${cityId}` }));
}

await fs.writeFile(path.join(workRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2), "utf8");
console.log(JSON.stringify(report, null, 2));
