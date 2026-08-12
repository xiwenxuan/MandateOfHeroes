import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.env.MANDATE_REPO_ROOT;
if (!repo) throw new Error("MANDATE_REPO_ROOT is required");

const docRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "CITY_DEVELOPMENT_PACKS");
const registryRoot = path.join(repo, "Docs", "KNOWLEDGE_BASE", "REGISTRY");
const outputRoot = path.join(repo, "outputs", "HAN_135_260_CORE_CITY_DEVELOPMENT_PACK_AND_UPGRADE_PROTOCOL_V1");
const previewRoot = path.join(outputRoot, "previews");
const workdata = JSON.parse(await fs.readFile(path.join(outputRoot, "core_city_development_pack_workdata.json"), "utf8"));
await fs.mkdir(previewRoot, { recursive: true });

const palette = {
  title: "#263B45",
  header: "#526E78",
  accent: "#C8923B",
  light: "#E9F0F2",
  border: "#CAD5D8",
  white: "#FFFFFF",
};

function safeValue(value) {
  if (value === null || value === undefined) return "";
  if (Array.isArray(value)) return value.join(" | ");
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}

function headersFor(rows) {
  const headers = [];
  const seen = new Set();
  for (const row of rows) {
    for (const key of Object.keys(row || {})) {
      if (!seen.has(key)) {
        seen.add(key);
        headers.push(key);
      }
    }
  }
  return headers.length ? headers : ["Notes"];
}

function columnName(index) {
  let value = index + 1;
  let name = "";
  while (value > 0) {
    const rem = (value - 1) % 26;
    name = String.fromCharCode(65 + rem) + name;
    value = Math.floor((value - 1) / 26);
  }
  return name;
}

function addDataSheet(workbook, sheetName, title, rows, tableName) {
  const sheet = workbook.worksheets.add(sheetName);
  sheet.showGridLines = false;
  const headers = headersFor(rows);
  const lastColumn = columnName(headers.length - 1);
  const data = rows.map(row => headers.map(header => safeValue(row?.[header])));
  const endRow = Math.max(3, 3 + data.length);

  sheet.getRange(`A1:${lastColumn}1`).merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange(`A1:${lastColumn}1`).format = {
    fill: palette.title,
    font: { bold: true, color: palette.white, size: 14 },
    rowHeight: 28,
    verticalAlignment: "center",
  };
  sheet.getRange("A2").values = [["记录数"]];
  sheet.getRange("B2").formulas = [[data.length ? `=COUNTA(A4:A${3 + data.length})` : "=0"]];
  sheet.getRange("A2:B2").format = {
    fill: palette.light,
    font: { bold: true, color: palette.title },
    borders: { preset: "outside", style: "thin", color: palette.border },
  };
  sheet.getRange(`A3:${lastColumn}3`).values = [headers];
  sheet.getRange(`A3:${lastColumn}3`).format = {
    fill: palette.header,
    font: { bold: true, color: palette.white },
    wrapText: true,
    verticalAlignment: "center",
    rowHeight: 30,
    borders: { preset: "outside", style: "thin", color: palette.border },
  };
  if (data.length) {
    sheet.getRange(`A4:${lastColumn}${3 + data.length}`).values = data;
    sheet.getRange(`A4:${lastColumn}${3 + data.length}`).format = {
      verticalAlignment: "top",
      wrapText: true,
      borders: { preset: "inside", style: "thin", color: palette.border },
    };
    const table = sheet.tables.add(`A3:${lastColumn}${3 + data.length}`, true, tableName);
    table.style = "TableStyleMedium2";
  }
  const used = sheet.getRange(`A1:${lastColumn}${endRow}`);
  used.format.autofitColumns();
  for (let col = 0; col < headers.length; col++) {
    const sampleLengths = data.slice(0, 40).map(row => String(row[col] ?? "").length);
    const longest = Math.max(String(headers[col]).length, ...sampleLengths, 0);
    const width = Math.min(38, Math.max(12, longest + 3));
    sheet.getRange(`${columnName(col)}:${columnName(col)}`).format.columnWidth = width;
  }
  sheet.freezePanes.freezeRows(3);
  sheet.freezePanes.freezeColumns(1);
  return { sheet, headers, lastColumn, endRow };
}

async function scanWorkbook(workbook, label) {
  const errors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 100 },
    summary: `${label} formula errors`,
    maxChars: 3000,
  });
  const text = errors.ndjson || "";
  const matches = text.split("\n").filter(line => line.includes("#REF!") || line.includes("#DIV/0!") || line.includes("#VALUE!") || line.includes("#NAME?") || line.includes("#N/A"));
  if (matches.length) throw new Error(`${label} contains formula errors: ${matches.slice(0, 3).join(" | ")}`);
}

async function renderAll(workbook, label, sheetNames) {
  const dir = path.join(previewRoot, label);
  await fs.mkdir(dir, { recursive: true });
  let count = 0;
  for (const sheetName of sheetNames) {
    const preview = await workbook.render({ sheetName, range: "A1:L25", scale: 1, format: "png" });
    await fs.writeFile(path.join(dir, `${String(count + 1).padStart(2, "0")}_${sheetName}.png`), new Uint8Array(await preview.arrayBuffer()));
    count++;
  }
  return count;
}

async function exportWorkbook(workbook, destination) {
  await fs.mkdir(path.dirname(destination), { recursive: true });
  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(destination);
}

async function loadReport() {
  try {
    return JSON.parse(await fs.readFile(path.join(outputRoot, "workbook_build_report.json"), "utf8"));
  } catch {
    return { schema: "mandate.core-city-development-pack-workbooks.v1", workbooks: [], previewCount: 0, formulaErrors: 0 };
  }
}

async function saveReport(report) {
  report.workbooks = [...new Set(report.workbooks)].sort();
  await fs.writeFile(path.join(outputRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2) + "\n", "utf8");
}

const summaryBooks = [
  ["01_CORE_CITY_DEVELOPMENT_PACK_MASTER.xlsx", "核心城市开发包总表", workdata.master],
  ["02_CORE_CITY_HISTORICAL_PERSON_COVERAGE.xlsx", "核心城市历史人物覆盖", workdata.person_coverage],
  ["03_CORE_CITY_CLAN_FAMILY_COVERAGE.xlsx", "核心城市宗族与家庭覆盖", workdata.clan_family_coverage],
  ["04_CORE_CITY_FACILITY_REFERENCE_COVERAGE.xlsx", "核心城市设施参考覆盖", workdata.facility_coverage],
  ["05_CORE_CITY_HINTERLAND_AND_SETTLEMENT_NETWORK.xlsx", "核心城市腹地与聚落网络", workdata.hinterland_network],
  ["06_CORE_CITY_POPULATION_LAYER_REFERENCE.xlsx", "核心城市人口层级参考", workdata.population_layers],
  ["07_CORE_CITY_HISTORICAL_STATE_AND_CHANGEPOINT_PLAN.xlsx", "核心城市历史状态与变化点计划", workdata.historical_states],
  ["08_CITY_DEVELOPMENT_PACK_UPGRADE_REGISTRY.xlsx", "城市开发包升级登记", workdata.upgrade_registry],
];

const citySheetMap = [
  ["00_INDEX", "开发包模块索引", "modules"],
  ["01_IDENTITY_ADMIN", "身份与行政", "identity"],
  ["02_HISTORICAL_STATES", "历史状态", "states"],
  ["03_POPULATION", "人口层级", "populations"],
  ["04_URBAN_FORM", "城市形态", "urban_form"],
  ["05_FACILITIES", "设施参考", "facilities"],
  ["06_HISTORICAL_PERSONS", "历史人物在场", "people"],
  ["07_CLAN_FAMILY_ESTATE", "宗族家庭与产业", "clans"],
  ["08_INDUSTRY_AGRICULTURE", "产业与农业", "industry"],
  ["09_TRANSPORT_SETTLEMENTS", "交通与聚落网络", "transport"],
  ["10_MILITARY", "军事层", "military_rows"],
  ["11_SCENARIO_SNAPSHOTS", "剧本切片", "states"],
  ["12_CHANGEPOINTS", "变化点", "states"],
  ["13_DEVELOPMENT_MAPPING", "开发映射", "modules"],
  ["14_SOURCES", "来源", "sources"],
  ["15_UNKNOWNS", "未知项", "unknown_rows"],
];

async function buildSummary() {
  const report = await loadReport();
  for (let index = 0; index < summaryBooks.length; index++) {
    const [filename, title, rows] = summaryBooks[index];
    const workbook = Workbook.create();
    addDataSheet(workbook, "说明", `${title}｜说明`, [{
      Schema: workdata.schema,
      GeneratedOn: workdata.generated_on,
      Scope: "REFERENCE_ONLY",
      RuntimeChanges: 0,
      AutomaticDepthChanges: 0,
      Note: "Pack Ready 不等于自动升档；任何运行时落地仍需独立任务与用户确认。",
    }], `SummaryNote${index + 1}`);
    addDataSheet(workbook, "数据", title, rows, `SummaryData${index + 1}`);
    addDataSheet(workbook, "来源", `${title}｜来源`, workdata.sources, `SummarySources${index + 1}`);
    await scanWorkbook(workbook, filename);
    report.previewCount += await renderAll(workbook, `summary_${index + 1}`, ["说明", "数据", "来源"]);
    const destination = path.join(docRoot, filename);
    await exportWorkbook(workbook, destination);
    report.workbooks.push(destination);
  }
  await saveReport(report);
}

async function buildCity(slug) {
  const city = workdata.cities[slug];
  if (!city) throw new Error(`Unknown city slug: ${slug}`);
  const workbook = Workbook.create();
  citySheetMap.forEach(([sheetName, title, key], index) => {
    const raw = city[key] ?? [];
    const rows = Array.isArray(raw) ? raw : [raw];
    addDataSheet(workbook, sheetName, `${city.label}｜${title}`, rows, `City${slug}${index}`.replace(/[^A-Za-z0-9]/g, ""));
  });
  await scanWorkbook(workbook, slug);
  const report = await loadReport();
  report.previewCount += await renderAll(workbook, `city_${slug}`, citySheetMap.map(item => item[0]));
  const destination = path.join(docRoot, city.directory, "CITY_DEVELOPMENT_DATA.xlsx");
  await exportWorkbook(workbook, destination);
  report.workbooks.push(destination);
  await saveReport(report);
}

const registryConfig = {
  documents: ["PROJECT_DOCUMENT_REGISTRY.xlsx", "DocumentId"],
  domain_map: ["PROJECT_CANONICAL_DOMAIN_MAP.xlsx", "DomainId"],
  design_decisions: ["DESIGN_DECISION_REGISTRY.xlsx", "DecisionId"],
  open_decisions: ["OPEN_DECISION_REGISTRY.xlsx", "DecisionId"],
  implementation_gaps: ["IMPLEMENTATION_GAP_REGISTER.xlsx", "GapId"],
  research_gaps: ["RESEARCH_GAP_REGISTER.xlsx", "GapId"],
};

function mergeRows(existing, updates, key) {
  const result = [...existing];
  const indexByKey = new Map(result.map((row, index) => [String(row?.[key] ?? ""), index]));
  for (const update of updates) {
    const id = String(update?.[key] ?? "");
    if (id && indexByKey.has(id)) result[indexByKey.get(id)] = { ...result[indexByKey.get(id)], ...update };
    else {
      indexByKey.set(id, result.length);
      result.push(update);
    }
  }
  return result;
}

async function renderExisting(filename, label) {
  const input = await FileBlob.load(path.join(registryRoot, filename));
  const workbook = await SpreadsheetFile.importXlsx(input);
  const inspected = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 2000 });
  const names = [];
  for (const line of (inspected.ndjson || "").split("\n")) {
    try {
      const row = JSON.parse(line);
      if (row.name) names.push(row.name);
    } catch {}
  }
  const targets = names.slice(0, 2);
  if (targets.length) await renderAll(workbook, `${label}_before`, targets);
}

async function buildRegistries() {
  const report = await loadReport();
  for (const [kind, [filename, key]] of Object.entries(registryConfig)) {
    await renderExisting(filename, `registry_${kind}`);
    const rows = mergeRows(workdata.registry_existing[kind] || [], workdata.registry_updates[kind] || [], key);
    const workbook = Workbook.create();
    addDataSheet(workbook, "说明", `${filename}｜维护说明`, [{
      Registry: filename,
      UpdateTask: "HAN-135-260-CORE-CITY-DEVELOPMENT-PACK-AND-UPGRADE-PROTOCOL-V1",
      ExistingRowsPreserved: (workdata.registry_existing[kind] || []).length,
      AddedOrUpdatedRows: (workdata.registry_updates[kind] || []).length,
      Note: "本次仅登记城市开发包、决策与缺口，不创建运行时世界事实。",
    }], `RegNote${key}`.replace(/[^A-Za-z0-9]/g, ""));
    addDataSheet(workbook, "数据", filename, rows, `RegData${key}`.replace(/[^A-Za-z0-9]/g, ""));
    await scanWorkbook(workbook, filename);
    report.previewCount += await renderAll(workbook, `registry_${kind}_after`, ["说明", "数据"]);
    const destination = path.join(registryRoot, filename);
    await exportWorkbook(workbook, destination);
    report.workbooks.push(destination);
  }
  await saveReport(report);
}

async function buildRoster() {
  const rosterDir = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE", "DEVELOPMENT_PLACE_ROSTER_V1");
  const rosterFile = path.join(rosterDir, "01_DEVELOPMENT_PLACE_ROSTER.xlsx");
  const input = await FileBlob.load(rosterFile);
  const before = await SpreadsheetFile.importXlsx(input);
  await renderAll(before, "roster_before", ["说明", "数据", "来源"]);

  const rosterJson = JSON.parse(await fs.readFile(path.join(repo, "outputs", "DEVELOPMENT_PLACE_ROSTER_AND_REFERENCE_READINESS_V1", "development_place_roster_workdata.json"), "utf8"));
  const workbook = Workbook.create();
  addDataSheet(workbook, "说明", "开发地点名册｜说明", [{
    Schema: rosterJson.schema,
    RecordCount: rosterJson.roster.length,
    PackFieldsAdded: "PackStatus|CityDevelopmentPack|PackReviewDate",
    DepthChanges: 0,
    Note: "72项不是永久白名单；D0/D1可按协议申请升档，Pack Ready 不自动改变 DevelopmentDepth。",
  }], "RosterNote");
  addDataSheet(workbook, "数据", "开发地点名册", rosterJson.roster, "RosterData");
  addDataSheet(workbook, "来源", "开发地点名册｜来源", rosterJson.sources || [], "RosterSources");
  await scanWorkbook(workbook, "development roster");
  const report = await loadReport();
  report.previewCount += await renderAll(workbook, "roster_after", ["说明", "数据", "来源"]);
  await exportWorkbook(workbook, rosterFile);
  report.workbooks.push(rosterFile);
  await saveReport(report);
}

const [mode, arg] = process.argv.slice(2);
if (mode === "summary") await buildSummary();
else if (mode === "city") await buildCity(arg);
else if (mode === "registries") await buildRegistries();
else if (mode === "roster") await buildRoster();
else throw new Error("Usage: summary | city <SLUG> | registries | roster");
