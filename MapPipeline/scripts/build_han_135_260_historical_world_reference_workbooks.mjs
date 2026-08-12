import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const repo = process.cwd();
const workRoot = path.join(repo, "outputs", "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1");
const finalRoot = path.join(repo, "Docs", "HISTORICAL_WORLD_REFERENCE");
const previewRoot = path.join(workRoot, "previews");
const inspectRoot = path.join(workRoot, "inspections");
const data = JSON.parse(await fs.readFile(path.join(workRoot, "historical_world_reference_workdata.json"), "utf8"));

await fs.mkdir(finalRoot, { recursive: true });
await fs.mkdir(previewRoot, { recursive: true });
await fs.mkdir(inspectRoot, { recursive: true });

const specs = [
  {
    key: "annual",
    file: "01_135-260逐年历史世界状态索引.xlsx",
    title: "135—260逐年历史世界状态索引",
    purpose: "126年连续人口状态、年度事件与Scenario入口；年度记录继承前一年，不复制世界。",
  },
  {
    key: "commanderies",
    file: "03_105郡国历史开发参考索引.xlsx",
    title: "105郡国历史开发参考索引",
    purpose: "140年稳定行政索引、州部父子关系、县数、城市数与研究状态。",
  },
  {
    key: "counties",
    file: "04_1182县历史开发参考索引.xlsx",
    title: "1182县级单位历史开发参考索引",
    purpose: "全量县级稳定ID、名称、隶属、坐标证据与研究状态；不把未定位点补成史实。",
  },
  {
    key: "cities",
    file: "05_77战略城市历史开发参考索引.xlsx",
    title: "77战略城市历史开发参考索引",
    purpose: "77城全量骨架、8个CITY-S详档入口及184年模型人口口径。",
  },
  {
    key: "persons",
    file: "06_135-260历史人物地理分布开发参考.xlsx",
    title: "135—260历史人物地理分布开发参考",
    purpose: "1202名历史人物的籍贯、主要地域、Clan/Branch与位置研究覆盖；1202不是上限。",
  },
  {
    key: "clans",
    file: "07_135-260历史宗族地理分布开发参考.xlsx",
    title: "135—260历史宗族地理分布开发参考",
    purpose: "39个Clan的郡望、本籍、Presence与成员覆盖；Clan不等于运行时家族组织。",
  },
  {
    key: "events",
    file: "13_135-260重大历史事件区域影响参考.xlsx",
    title: "135—260重大历史事件区域影响参考",
    purpose: "现有人口模型事件对区域、死亡、迁徙、出生和登记的影响合同。",
  },
  {
    key: "sources",
    file: "历史资料来源总索引.xlsx",
    title: "历史资料来源总索引",
    purpose: "人口、人物、地图与第一批CITY-S官方研究入口的统一来源索引。",
  },
];

const preferredHeaders = {
  annual: ["year", "inherits_from_year", "scenario", "registered_population_start", "registered_population_end", "modeled_actual_population_start", "modeled_actual_population_end", "births", "natural_deaths", "war_deaths", "epidemic_deaths", "disaster_deaths", "net_migration", "registration_loss", "registration_recovery", "annual_change", "annual_change_rate", "change_event_ids", "change_event_names", "historical_anchors", "evidence_level", "evidence_type", "notes"],
  commanderies: ["commandery_id", "display_name", "province_id", "province_name", "county_count", "strategic_city_count", "valid_from_year", "valid_to_year", "confidence", "source", "geometry_status", "evidence_type", "research_status"],
  counties: ["county_id", "display_name", "commandery_id", "commandery_name", "province_id", "province_name", "longitude", "latitude", "coordinate_status", "confidence", "historical_claim", "source_ids", "strategic_city_id", "development_status", "evidence_type"],
  cities: ["city_id", "display_name", "historical_name", "detail_level", "admin_reference", "province_id", "longitude", "latitude", "coordinate_status", "confidence", "source_ids", "population_184_walled", "population_184_urban", "population_184_metro", "population_184_county", "population_evidence", "document", "research_status"],
  persons: ["person_id", "canonical_name", "birth_year_low", "birth_year_high", "death_year_low", "death_year_high", "tier", "primary_identity", "native_region_id", "native_county_id", "native_place_text", "primary_historical_region_id", "clan_id", "branch_id", "location_record_count", "resolved_location_count", "evidence_level", "research_status", "source_id"],
  clans: ["clan_id", "canonical_clan_name", "surname", "clan_type", "commandery_region_id", "county_region_id", "primary_region_id", "start_year", "end_year", "major_clan", "presence_count", "known_member_count", "evidence_level", "research_status", "notes"],
  events: ["event_id", "name", "start_year", "end_year", "impact_type", "affected_provinces", "affected_province_names", "severity_basis_points", "mortality_share_basis_points", "migration_share_basis_points", "birth_impact_basis_points", "registration_impact_basis_points", "source_id", "confidence", "evidence_type", "world_effect_contract"],
  sources: ["source_id", "source_type", "title", "author_or_editor", "edition_or_host", "url_or_locator", "access_date", "reliability_class", "evidence_scope", "license_note", "notes"],
};

function colName(index) {
  let n = index + 1;
  let name = "";
  while (n > 0) {
    const r = (n - 1) % 26;
    name = String.fromCharCode(65 + r) + name;
    n = Math.floor((n - 1) / 26);
  }
  return name;
}

function normalize(value) {
  if (value === undefined || value === null) return null;
  if (Array.isArray(value)) return value.join("|");
  if (typeof value === "object") return JSON.stringify(value);
  return value;
}

async function build(spec) {
  const rows = data[spec.key];
  const headers = preferredHeaders[spec.key];
  const wb = Workbook.create();
  const summary = wb.worksheets.add("说明");
  const sheet = wb.worksheets.add("数据");
  summary.showGridLines = false;
  sheet.showGridLines = false;

  summary.getRange("A1:H1").merge();
  summary.getRange("A1").values = [[spec.title]];
  summary.getRange("A1:H1").format = {
    fill: "#3A4A3F",
    font: { bold: true, color: "#FFFFFF", size: 18 },
    rowHeight: 30,
    verticalAlignment: "center",
  };
  summary.getRange("A3").values = [["用途"]];
  summary.getRange("B3:H4").merge();
  summary.getRange("B3").values = [[spec.purpose]];
  summary.getRange("A5").values = [["记录数"]];
  summary.getRange("B5").formulas = [[`=COUNTA('数据'!A2:A${rows.length + 1})`]];
  summary.getRange("A6").values = [["证据标签"]];
  summary.getRange("B6:H6").merge();
  summary.getRange("B6").values = [["HISTORICAL=史料/考古；RECONSTRUCTED=保守复原；MODELED=项目模型；UNKNOWN=待研究"]];
  summary.getRange("A7").values = [["统一边界"]];
  summary.getRange("B7:H8").merge();
  summary.getRange("B7").values = [["稳定ID不得静默改指；140行政截面不是全时段不变；代理几何与模型人口不冒充史实；260年不是世界终点。"]];
  summary.getRange("A3:A7").format = { fill: "#D8E1D5", font: { bold: true, color: "#26342B" }, verticalAlignment: "top" };
  summary.getRange("B3:H8").format = { fill: "#F5F1E6", font: { color: "#2E332F" }, wrapText: true, verticalAlignment: "top" };
  summary.getRange("A3:H8").format.borders = { preset: "all", style: "thin", color: "#C8C1B4" };
  summary.getRange("A1:H8").format.columnWidth = 18;
  summary.getRange("A:A").format.columnWidth = 14;
  summary.getRange("B:H").format.columnWidth = 20;

  const matrix = [headers, ...rows.map(row => headers.map(h => normalize(row[h])))];
  const endCol = colName(headers.length - 1);
  const dataRange = sheet.getRange(`A1:${endCol}${matrix.length}`);
  dataRange.values = matrix;
  sheet.freezePanes.freezeRows(1);
  sheet.freezePanes.freezeColumns(Math.min(2, headers.length));
  sheet.getRange(`A1:${endCol}1`).format = {
    fill: "#445E4D",
    font: { bold: true, color: "#FFFFFF" },
    wrapText: true,
    rowHeight: 32,
    verticalAlignment: "center",
  };
  sheet.getRange(`A2:${endCol}${matrix.length}`).format = {
    font: { color: "#222222", size: 10 },
    wrapText: true,
    verticalAlignment: "top",
  };
  dataRange.format.borders = { preset: "all", style: "thin", color: "#D9D4C8" };
  sheet.tables.add(`A1:${endCol}${matrix.length}`, true, `${spec.key[0].toUpperCase()}${spec.key.slice(1)}Table`);

  for (let i = 0; i < headers.length; i++) {
    const header = headers[i];
    const col = colName(i);
    let width = 16;
    if (header.includes("id") || header.includes("region") || header.includes("reference") || header.includes("source")) width = 28;
    if (header.includes("notes") || header.includes("scope") || header.includes("contract") || header.includes("document") || header.includes("url")) width = 42;
    if (header.includes("name") || header.includes("status") || header.includes("type")) width = 22;
    sheet.getRange(`${col}:${col}`).format.columnWidth = width;
  }
  if (spec.key === "annual") {
    const rateCol = colName(headers.indexOf("annual_change_rate"));
    sheet.getRange(`${rateCol}2:${rateCol}${matrix.length}`).format.numberFormat = "0.0000%";
  }
  if (spec.key === "counties" || spec.key === "cities") {
    for (const h of ["longitude", "latitude"]) {
      const idx = headers.indexOf(h);
      if (idx >= 0) sheet.getRange(`${colName(idx)}2:${colName(idx)}${matrix.length}`).format.numberFormat = "0.0000";
    }
  }

  const xlsx = await SpreadsheetFile.exportXlsx(wb);
  await xlsx.save(path.join(finalRoot, spec.file));

  const summaryPreview = await wb.render({ sheetName: "说明", autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${spec.key}_说明.png`), new Uint8Array(await summaryPreview.arrayBuffer()));
  const dataPreview = await wb.render({ sheetName: "数据", range: `A1:${colName(Math.min(headers.length, 8) - 1)}${Math.min(matrix.length, 30)}`, autoCrop: "all", scale: 0.8, format: "png" });
  await fs.writeFile(path.join(previewRoot, `${spec.key}_数据.png`), new Uint8Array(await dataPreview.arrayBuffer()));

  const inspect = await wb.inspect({ kind: "workbook,sheet,table,region,formula", maxChars: 12000, tableMaxRows: 8, tableMaxCols: 10, tableMaxCellChars: 100 });
  await fs.writeFile(path.join(inspectRoot, `${spec.key}.inspect.ndjson`), inspect.ndjson ?? JSON.stringify(inspect), "utf8");
  const formulas = await wb.inspect({ kind: "formula", sheetId: "说明", range: "A1:H10", maxChars: 3000 });
  const computed = await wb.inspect({ kind: "region", sheetId: "说明", range: "A1:H10", maxChars: 6000 });
  const formulaText = (formulas.ndjson ?? JSON.stringify(formulas)) + "\n" + (computed.ndjson ?? JSON.stringify(computed));
  const errors = ["#REF!", "#DIV/0!", "#VALUE!", "#NAME?", "#N/A"].filter(token => formulaText.includes(token));
  if (errors.length) throw new Error(`${spec.file} formula errors: ${errors.join(",")}`);
  return { file: spec.file, rows: rows.length, headers: headers.length, formulaErrors: 0 };
}

const report = [];
for (const spec of specs) report.push(await build(spec));
await fs.writeFile(path.join(workRoot, "workbook_build_report.json"), JSON.stringify(report, null, 2), "utf8");
console.log(JSON.stringify(report));
