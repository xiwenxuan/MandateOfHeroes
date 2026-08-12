import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.env.MANDATE_REPO_ROOT;
if (!repo) throw new Error("MANDATE_REPO_ROOT is required");
const out = path.join(repo, "outputs", "HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1");
const doc = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS");
const packRoot = path.join(doc, "PACKS");
const registryRoot = path.join(repo, "Docs", "KNOWLEDGE_BASE", "REGISTRY");
const previewRoot = path.join(out, "previews");
const data = JSON.parse(await fs.readFile(path.join(out, "full_reference_pack_workdata.json"), "utf8"));
await fs.mkdir(previewRoot, { recursive: true });

const colors = { title: "#263B45", header: "#526E78", light: "#E9F0F2", white: "#FFFFFF", border: "#CAD5D8" };
const safe = value => value === null || value === undefined ? "" : Array.isArray(value) ? value.join(" | ") : typeof value === "object" ? JSON.stringify(value) : value;
function colName(index) { let n = index + 1, s = ""; while (n) { const r = (n - 1) % 26; s = String.fromCharCode(65 + r) + s; n = Math.floor((n - 1) / 26); } return s; }
function headersFor(rows) { const result = [], seen = new Set(); for (const row of rows) for (const key of Object.keys(row || {})) if (!seen.has(key)) { seen.add(key); result.push(key); } return result.length ? result : ["Notes"]; }

function addSheet(workbook, sheetName, title, rawRows, tableName) {
  const rows = Array.isArray(rawRows) ? rawRows : [rawRows];
  const headers = headersFor(rows);
  const last = colName(headers.length - 1);
  const titleLast = colName(Math.min(headers.length, 12) - 1);
  const values = rows.map(row => headers.map(h => safe(row?.[h])));
  const sheet = workbook.worksheets.add(sheetName);
  sheet.showGridLines = false;
  sheet.getRange(`A1:${titleLast}1`).merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange(`A1:${titleLast}1`).format = { fill: colors.title, font: { bold: true, color: colors.white, size: 14 }, rowHeight: 28, verticalAlignment: "center" };
  sheet.getRange("A2:B2").values = [["记录数", values.length]];
  sheet.getRange("A2:B2").format = { fill: colors.light, font: { bold: true, color: colors.title }, borders: { preset: "outside", style: "thin", color: colors.border } };
  sheet.getRange(`A3:${last}3`).values = [headers];
  sheet.getRange(`A3:${last}3`).format = { fill: colors.header, font: { bold: true, color: colors.white }, wrapText: true, rowHeight: 30, verticalAlignment: "center", borders: { preset: "outside", style: "thin", color: colors.border } };
  if (values.length) {
    sheet.getRange(`A4:${last}${3 + values.length}`).values = values;
    sheet.getRange(`A4:${last}${3 + values.length}`).format = { wrapText: true, verticalAlignment: "top", borders: { preset: "inside", style: "thin", color: colors.border } };
    const table = sheet.tables.add(`A3:${last}${3 + values.length}`, true, tableName.replace(/[^A-Za-z0-9]/g, "").slice(0, 200));
    table.style = "TableStyleMedium2";
  }
  for (let i = 0; i < headers.length; i++) {
    const longest = Math.max(String(headers[i]).length, ...values.slice(0, 100).map(r => String(r[i] ?? "").length), 0);
    sheet.getRange(`${colName(i)}:${colName(i)}`).format.columnWidth = Math.min(38, Math.max(12, longest + 3));
  }
  sheet.freezePanes.freezeRows(3);
  sheet.freezePanes.freezeColumns(1);
  return sheet;
}

async function scan(workbook, label) {
  const result = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: `${label} formula errors`, maxChars: 3000 });
  const text = result.ndjson || "";
  if (/#REF!|#DIV\/0!|#VALUE!|#NAME\?|#N\/A/.test(text)) throw new Error(`${label} contains formula errors`);
}

async function renderSheets(workbook, label, names) {
  const dir = path.join(previewRoot, label);
  await fs.mkdir(dir, { recursive: true });
  let count = 0;
  for (const name of names) {
    const png = await workbook.render({ sheetName: name, range: "A1:L25", scale: 1, format: "png" });
    await fs.writeFile(path.join(dir, `${String(++count).padStart(2, "0")}_${name}.png`), new Uint8Array(await png.arrayBuffer()));
  }
  return count;
}

async function exportBook(workbook, destination) {
  await fs.mkdir(path.dirname(destination), { recursive: true });
  const blob = await SpreadsheetFile.exportXlsx(workbook);
  await blob.save(destination);
}

async function report() {
  try { return JSON.parse(await fs.readFile(path.join(out, "workbook_build_report.json"), "utf8")); }
  catch { return { schema: "mandate.han135260.development-place-full-reference-pack-workbooks.v1", workbooks: [], previewCount: 0, formulaErrors: 0, completedModes: [] }; }
}
async function saveReport(r) { r.workbooks = [...new Set(r.workbooks)].sort(); r.completedModes = [...new Set(r.completedModes)]; await fs.writeFile(path.join(out, "workbook_build_report.json"), JSON.stringify(r, null, 2) + "\n", "utf8"); }

const summarySpecs = [
  ["DEVELOPMENT_PLACE_MASTER.xlsx", "Development Place 当前主表", data.master],
  ["01_FULL_PACK_COMPLETENESS_MASTER.xlsx", "完整参考包完备性主表", data.completeness],
  ["02_EVENT_DEPENDENT_SITE_MASTER.xlsx", "事件依赖地点主表", data.event_sites],
  ["03_PLACE_HISTORICAL_PERSON_COVERAGE.xlsx", "地点历史人物覆盖", data.person_coverage],
  ["04_PLACE_CLAN_FAMILY_ESTATE_COVERAGE.xlsx", "地点宗族家庭庄园覆盖", data.clan_family_estate_coverage],
  ["05_PLACE_FACILITY_REFERENCE_COVERAGE.xlsx", "地点设施参考覆盖", data.facility_coverage],
  ["06_PLACE_POPULATION_AND_SETTLEMENT_REFERENCE.xlsx", "地点人口与聚落参考", data.population_settlement_reference],
  ["07_PLACE_INDUSTRY_RESOURCE_SUPPLY_REFERENCE.xlsx", "地点产业资源供给参考", data.industry_resource_supply_reference],
  ["08_PLACE_TRANSPORT_AND_HINTERLAND_REFERENCE.xlsx", "地点交通与腹地参考", data.transport_hinterland_reference],
  ["09_PLACE_MILITARY_AND_EVENT_STATE_REFERENCE.xlsx", "地点军事与事件状态参考", data.military_event_reference],
  ["10_PLACE_DEVELOPMENT_PACK_UPGRADE_REGISTRY.xlsx", "地点开发包升级登记", data.upgrade_registry],
];

async function buildSummaries() {
  const r = await report();
  for (let i = 0; i < summarySpecs.length; i++) {
    const [file, title, rows] = summarySpecs[i];
    const wb = Workbook.create();
    addSheet(wb, "说明", `${title}｜说明`, [{ Schema: data.schema, Scope: data.scope, RosterCount: data.roster_count, RuntimeChanges: data.runtime_changes, Note: "T档、参考完整度和运行时状态相互独立；旧D标签仅作映射。" }], `SummaryNote${i}`);
    addSheet(wb, "数据", title, rows, `SummaryData${i}`);
    addSheet(wb, "来源", `${title}｜来源`, data.sources, `SummarySource${i}`);
    await scan(wb, file);
    r.previewCount += await renderSheets(wb, `summary_${String(i + 1).padStart(2, "0")}`, ["说明", "数据", "来源"]);
    const destination = path.join(doc, file);
    await exportBook(wb, destination);
    r.workbooks.push(destination);
  }
  r.completedModes.push("summaries");
  await saveReport(r);
}

async function buildPack(slug) {
  const pack = data.packs[slug];
  if (!pack) throw new Error(`Unknown pack ${slug}`);
  const wb = Workbook.create();
  const name = pack.identity[0].CanonicalName;
  data.sheet_contract.forEach((sheetName, i) => {
    const module = pack.modules.find(x => x.Sheet === sheetName)?.Module;
    const rows = pack[module] ?? [];
    addSheet(wb, sheetName, `${name}｜${module}`, rows, `Pack${i}${slug}`);
  });
  await scan(wb, slug);
  const r = await report();
  r.previewCount += await renderSheets(wb, `pack_${slug}`, data.sheet_contract);
  const destination = path.join(packRoot, slug, "PLACE_DEVELOPMENT_REFERENCE.xlsx");
  await exportBook(wb, destination);
  r.workbooks.push(destination);
  r.completedModes.push(`pack:${slug}`);
  await saveReport(r);
}

const registrySpecs = {
  documents: ["PROJECT_DOCUMENT_REGISTRY.xlsx", "DocumentId"],
  domain_map: ["PROJECT_CANONICAL_DOMAIN_MAP.xlsx", "DomainId"],
  design_decisions: ["DESIGN_DECISION_REGISTRY.xlsx", "DecisionId"],
  open_decisions: ["OPEN_DECISION_REGISTRY.xlsx", "DecisionId"],
  implementation_gaps: ["IMPLEMENTATION_GAP_REGISTER.xlsx", "GapId"],
  research_gaps: ["RESEARCH_GAP_REGISTER.xlsx", "GapId"],
  document_conflicts: ["DOCUMENT_CONFLICT_REGISTER.xlsx", "ConflictId"],
};
function mergeRows(existing, updates, key) { const rows = existing.map(x => ({ ...x })); const pos = new Map(rows.map((x, i) => [String(x[key] ?? ""), i])); for (const update of updates) { const id = String(update[key] ?? ""); if (id && pos.has(id)) rows[pos.get(id)] = { ...rows[pos.get(id)], ...update }; else { pos.set(id, rows.length); rows.push({ ...update }); } } return rows; }

async function renderExistingRegistry(filename, label) {
  const blob = await FileBlob.load(path.join(registryRoot, filename));
  const wb = await SpreadsheetFile.importXlsx(blob);
  const inspected = await wb.inspect({ kind: "sheet", include: "id,name", maxChars: 3000 });
  const names = [];
  for (const line of (inspected.ndjson || "").split("\n")) { try { const row = JSON.parse(line); if (row.name) names.push(row.name); } catch {} }
  if (names.length) return renderSheets(wb, `${label}_before`, names.slice(0, 2));
  return 0;
}

async function buildRegistries() {
  const r = await report();
  for (const [kind, [filename, key]] of Object.entries(registrySpecs)) {
    r.previewCount += await renderExistingRegistry(filename, `registry_${kind}`);
    const rows = mergeRows(data.registry_existing[kind] || [], data.registry_updates[kind] || [], key);
    const wb = Workbook.create();
    addSheet(wb, "说明", `${filename}｜维护说明`, [{ Registry: filename, UpdateTask: "HAN-135-260-DEVELOPMENT-PLACE-FULL-REFERENCE-PACK-V1", ExistingRowsPreserved: (data.registry_existing[kind] || []).length, AddedOrUpdatedRows: (data.registry_updates[kind] || []).length, Note: "本轮登记FDRP、T1-T4、事件依赖地点和显式缺口；不修改运行时世界。" }], `RegistryNote${kind}`);
    addSheet(wb, "数据", filename, rows, `RegistryData${kind}`);
    await scan(wb, filename);
    r.previewCount += await renderSheets(wb, `registry_${kind}_after`, ["说明", "数据"]);
    const destination = path.join(registryRoot, filename);
    await exportBook(wb, destination);
    r.workbooks.push(destination);
  }
  r.completedModes.push("registries");
  await saveReport(r);
}

const [mode, arg] = process.argv.slice(2);
if (mode === "summaries") await buildSummaries();
else if (mode === "pack") await buildPack(arg);
else if (mode === "pack-range") {
  const [startText, endText] = process.argv.slice(3);
  const slugs = Object.keys(data.packs);
  const start = Number(startText), end = Number(endText);
  if (!Number.isInteger(start) || !Number.isInteger(end) || start < 0 || end > slugs.length || start >= end) throw new Error("Usage: pack-range <start-inclusive> <end-exclusive>");
  for (const slug of slugs.slice(start, end)) await buildPack(slug);
}
else if (mode === "registries") await buildRegistries();
else throw new Error("Usage: summaries | pack <SLUG> | pack-range <start> <end> | registries");
