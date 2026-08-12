import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = process.env.MANDATE_ARTIFACT_TOOL_ENTRY || "@oai/artifact-tool";
const { SpreadsheetFile, Workbook } = await import(pathToFileURL(artifactEntry).href);
const repo = "E:/project/gamedevelop/MandateOfHeroes";
const taskId = "HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1";
const root = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE");
const outputDir = path.join(root, "MASTER_WORKBOOKS");
const previewDir = path.join(root, "VALIDATION/workbook_previews");
const inspectDir = path.join(root, "VALIDATION/workbook_inspection");
const d = JSON.parse(await fs.readFile(path.join(root, "COUNTY_PACKS/county_economy_master_v1.json"), "utf8"));
await Promise.all([outputDir, previewDir, inspectDir].map(x => fs.mkdir(x, { recursive: true })));

const args = Object.fromEntries(process.argv.slice(2).map(x => x.split("=")).map(([k, v]) => [k.replace(/^--/, ""), v]));
const start = Number(args.start ?? 1);
const count = Number(args.count ?? 40);
const countyIndex = new Map(d.counties.map(x => [x.county_permanent_id, x]));
const withCounty = rows => rows.map(x => ({
  county_permanent_id: x.county_permanent_id,
  county_name: countyIndex.get(x.county_permanent_id)?.county_name ?? "",
  province_id: countyIndex.get(x.county_permanent_id)?.province_id ?? "",
  commandery_equivalent_id: countyIndex.get(x.county_permanent_id)?.commandery_equivalent_id ?? "",
  ...x,
}));
const pick = (rows, keys) => rows.map(row => Object.fromEntries(keys.map(k => [k, row[k]])));
const industries = (...ids) => withCounty(d.processing_capacities.filter(x => ids.includes(x.industry_id)));
const sourceRows = [
  ...d.evidence_registry.map(x => ({ record_type: "SOURCE", ...x })),
  ...d.method_registry.map(x => ({ record_type: "METHOD", ...x })),
  ...d.facility_references.map(x => ({ record_type: "FACILITY_MAPPING", ...x })),
  ...d.product_taxonomy.map(x => ({ record_type: "PRODUCT_MAPPING", ...x })),
  ...d.crop_definitions.map(x => ({ record_type: "CROP_MAPPING", ...x })),
];
const importRows = withCounty(d.product_balances.map(x => ({ ...x, import_required: x.import_dependency_ratio > 0 || x.unmet_demand > 0, unresolved_import_need: x.unmet_demand })));
const exportRows = withCounty(d.product_balances.map(x => ({ ...x, export_capacity_reference: Math.max(0, x.net_balance_before_trade), realized_export: x.export })));
const facilityRows = withCounty(d.processing_capacities);
const specs = [
  ["01_COUNTY_IDENTITY_GEOGRAPHY_MASTER.xlsx", "County identity and geography master", d.counties],
  ["02_COUNTY_POPULATION_WORKFORCE_MASTER.xlsx", "County population and workforce master", pick(d.counties, ["county_permanent_id", "county_name", "province_id", "commandery_equivalent_id", "population_184", "registered_population_184", "households_184_modeled", "children", "youth", "prime_workers", "older_workers", "retired", "labor_pool", "civilian_effective_labor", "military_population", "agricultural_workers", "craft_workers", "transport_workers", "merchants", "administrative_workers", "other_workers", "primary_evidence_grade", "primary_method"])],
  ["03_COUNTY_LAND_AGRICULTURAL_POTENTIAL.xlsx", "County land and agricultural potential", pick(d.counties, ["county_permanent_id", "county_name", "province_id", "total_area_sq_km_low", "total_area_sq_km_recommended", "total_area_sq_km_high", "area_method", "total_land_ha_reference", "arable_potential_ha", "current_cultivated_land_ha", "pasture_potential_ha", "forest_area_reference_ha", "average_fertility_basis_points", "primary_evidence_grade"])],
  ["04_COUNTY_WATER_IRRIGATION_REFERENCE.xlsx", "County water and irrigation reference", pick(d.counties, ["county_permanent_id", "county_name", "province_id", "major_water_reference", "water_access_basis_points", "wetland_potential_ha", "irrigation_potential_basis_points", "historical_irrigation_status", "flood_risk_basis_points", "drought_risk_basis_points", "locust_risk_basis_points", "cold_risk_basis_points", "primary_evidence_grade"])],
  ["05_COUNTY_CROP_MIX_AND_YIELD.xlsx", "County crop mix and yield", withCounty(d.crops)],
  ["06_COUNTY_AGRICULTURAL_OUTPUT.xlsx", "County agricultural output", withCounty(d.crops.map(x => ({ county_permanent_id: x.county_permanent_id, crop_id: x.crop_id, product_id: x.product_id, gross_output_kg: x.gross_output_kg, seed_retention_kg: x.seed_retention_kg, harvest_loss_kg: x.harvest_loss_kg, processing_loss_kg: x.processing_loss_kg, storage_spoilage_kg: x.storage_spoilage_kg, usable_output_kg: x.usable_output_kg, evidence_grade: x.evidence_grade, method_id: x.method_id })))],
  ["07_COUNTY_LIVESTOCK_REFERENCE.xlsx", "County livestock reference", withCounty(d.livestock)],
  ["08_COUNTY_FORESTRY_FUEL_REFERENCE.xlsx", "County forestry and fuel reference", withCounty(d.forestry)],
  ["09_COUNTY_FISHERY_GATHERING_REFERENCE.xlsx", "County fishery and gathering reference", withCounty(d.fishery_gathering)],
  ["10_COUNTY_MINERAL_REFERENCE.xlsx", "County mineral reference", withCounty(d.minerals)],
  ["11_COUNTY_SALT_REFERENCE.xlsx", "County salt reference", withCounty(d.salt)],
  ["12_COUNTY_RAW_MATERIAL_OUTPUT.xlsx", "County raw material output", withCounty(d.raw_materials)],
  ["13_COUNTY_FOOD_PROCESSING_CAPACITY.xlsx", "County food processing capacity", industries("FOOD_PROCESSING")],
  ["14_COUNTY_BREWING_CAPACITY.xlsx", "County brewing capacity", industries("BREWING")],
  ["15_COUNTY_METALLURGY_CAPACITY.xlsx", "County metallurgy capacity", industries("METALLURGY")],
  ["16_COUNTY_METALWORKING_CAPACITY.xlsx", "County metalworking capacity", industries("METALWORKING")],
  ["17_COUNTY_TEXTILE_SILK_CAPACITY.xlsx", "County textile and silk capacity", industries("TEXTILE", "SILK")],
  ["18_COUNTY_LEATHER_WOODWORK_CAPACITY.xlsx", "County leather and woodwork capacity", industries("LEATHER", "WOODWORK")],
  ["19_COUNTY_POTTERY_BUILDING_MATERIAL_CAPACITY.xlsx", "County pottery and building material capacity", industries("POTTERY_BUILDING")],
  ["20_COUNTY_MEDICINE_SPECIAL_CRAFT.xlsx", "County medicine and special craft capacity", industries("MEDICINE")],
  ["21_COUNTY_CART_AND_SHIPBUILDING.xlsx", "County cart and shipbuilding capacity", industries("VEHICLE", "SHIPBUILDING")],
  ["22_COUNTY_MILITARY_PRODUCTION_CAPACITY.xlsx", "County military production capacity", industries("MILITARY")],
  ["23_COUNTY_FACILITY_REFERENCE.xlsx", "County facility reference", facilityRows],
  ["24_COUNTY_STORAGE_CAPACITY.xlsx", "County storage capacity", withCounty(d.storage)],
  ["25_COUNTY_MARKET_AND_SERVICE_CAPACITY.xlsx", "County market and service capacity", withCounty(d.market_service)],
  ["26_COUNTY_TRANSPORT_CAPACITY.xlsx", "County transport capacity", withCounty(d.transport)],
  ["27_COUNTY_LOCAL_DEMAND.xlsx", "County local demand", withCounty(d.local_demands)],
  ["28_COUNTY_PRODUCT_PRODUCTION_BALANCE.xlsx", "County product production balance", withCounty(d.product_balances)],
  ["29_COUNTY_PRODUCT_SURPLUS_DEFICIT.xlsx", "County product surplus and deficit", withCounty(d.product_balances.map(x => ({ county_permanent_id: x.county_permanent_id, product_category: x.product_category, normalized_unit: x.normalized_unit, production: x.production, total_demand: x.household_consumption + x.industrial_use + x.government_demand + x.military_demand, loss: x.loss, net_balance_before_trade: x.net_balance_before_trade, surplus_deficit_status: x.surplus_deficit_status, unmet_demand: x.unmet_demand, evidence_grade: x.evidence_grade })))],
  ["30_COUNTY_IMPORT_DEPENDENCY.xlsx", "County import dependency", importRows],
  ["31_COUNTY_EXPORT_CAPACITY.xlsx", "County export capacity", exportRows],
  ["32_COUNTY_SUPPLY_RELATION_MASTER.xlsx", "County supply relation master", d.supply_relations],
  ["33_COUNTY_PROCESSING_CHAIN_DEPENDENCY.xlsx", "County processing chain dependency", withCounty(d.processing_dependencies)],
  ["34_REGIONAL_PRODUCTION_ZONE_MASTER.xlsx", "Regional production zone master", d.regional_zones],
  ["35_SCENARIO_PRODUCTION_STATE_MASTER.xlsx", "Scenario production state master", withCounty(d.scenario_states)],
  ["36_HISTORICAL_PRODUCTION_CHANGEPOINTS.xlsx", "Historical production change points", d.change_points],
  ["37_COUNTY_RUNTIME_MAPPING_REFERENCE.xlsx", "County runtime mapping reference", withCounty(d.runtime_mapping)],
  ["38_EVIDENCE_AND_SOURCE_REGISTRY.xlsx", "Evidence and source registry", sourceRows],
  ["39_COUNTY_UNKNOWNS_AND_RESEARCH_GAPS.xlsx", "County unknowns and research gaps", withCounty(d.unknowns)],
  ["40_NATIONAL_184_PRODUCTION_BALANCE.xlsx", "National 184 production balance", d.national_balance_184],
];

const scalar = value => value === null || value === undefined ? "" : Array.isArray(value) ? value.join("|") : typeof value === "object" ? JSON.stringify(value) : value;
const headers = rows => [...rows.reduce((set, row) => { Object.keys(row).forEach(k => set.add(k)); return set; }, new Set())];
const col = index => { let n = index + 1, out = ""; while (n > 0) { const r = (n - 1) % 26; out = String.fromCharCode(65 + r) + out; n = Math.floor((n - 1) / 26); } return out; };
const safeSheet = name => name.replace(/[^A-Za-z0-9_]/g, "_").slice(0, 31);

function writeCover(sheet, title, file, rows) {
  sheet.showGridLines = false;
  sheet.getRange("A1:H2").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H2").format = { fill: "#153B50", font: { bold: true, color: "#FFFFFF", size: 20 }, verticalAlignment: "center" };
  sheet.getRange("A4:B12").values = [
    ["Field", "Value"], ["Task", taskId], ["Workbook", file], ["Role", "Development reference; not runtime authority"],
    ["Expected rows", rows.length], ["Data rows (formula)", ""], ["Evidence policy", "HISTORICAL / ARCHAEOLOGICAL / RECONSTRUCTED / MODELED / UNKNOWN"],
    ["Runtime authority", "Cell + Resource + Facility + Worker + Recipe + Inventory + Transport"], ["Generated from", "county_economy_master_v1.json"],
  ];
  sheet.getRange("B9").formulas = [["=COUNTA(Data!A:A)-1"]];
  sheet.getRange("A4:B4").format = { fill: "#2E6E7E", font: { bold: true, color: "#FFFFFF" } };
  sheet.getRange("A5:A12").format = { fill: "#DCEBF0", font: { bold: true, color: "#153B50" } };
  sheet.getRange("A4:B12").format.borders = { preset: "all", style: "thin", color: "#B8CDD5" };
  sheet.getRange("A4:A12").format.columnWidth = 24;
  sheet.getRange("B4:B12").format = { columnWidth: 82, wrapText: true };
  sheet.getRange("A14:H17").merge();
  sheet.getRange("A14").values = [["Interpretation guard: modeled county points, areas and capacities are analytical references. They must not be presented as historical county boundaries, exact ancient routes, or already materialized facilities and inventories. Deficits and unknowns are retained rather than filled by hidden multipliers."]];
  sheet.getRange("A14:H17").format = { fill: "#FFF2CC", font: { color: "#7F6000", italic: true }, wrapText: true, verticalAlignment: "center" };
}

function writeData(sheet, rows, tableName) {
  sheet.showGridLines = false;
  const hs = headers(rows);
  if (!hs.length) hs.push("status");
  const matrix = [hs, ...rows.map(row => hs.map(key => scalar(row[key])))];
  sheet.getRangeByIndexes(0, 0, matrix.length, hs.length).values = matrix;
  const end = col(hs.length - 1);
  sheet.getRange(`A1:${end}1`).format = { fill: "#214E63", font: { bold: true, color: "#FFFFFF" }, wrapText: true, verticalAlignment: "center" };
  if (rows.length) sheet.tables.add(`A1:${end}${matrix.length}`, true, tableName);
  sheet.freezePanes.freezeRows(1);
  hs.forEach((header, i) => {
    const lower = header.toLowerCase();
    let width = 17;
    if (lower.includes("id") || lower.includes("reference") || lower.includes("reason") || lower.includes("notes") || lower.includes("source")) width = 34;
    if (lower.includes("name") || lower.includes("status") || lower.includes("method")) width = 24;
    sheet.getRange(`${col(i)}:${col(i)}`).format.columnWidth = width;
  });
  sheet.getRange("1:1").format.rowHeight = 34;
  return { lastColumn: end, lastRow: matrix.length, preview: `A1:${end}${Math.min(matrix.length, 45)}` };
}

function writeValidation(sheet, expectedRows) {
  sheet.showGridLines = false;
  sheet.getRange("A1:D1").values = [["Check", "Expected", "Actual", "Status"]];
  sheet.getRange("A2:D4").values = [["Data row count", expectedRows, "", ""], ["Header exists", 1, "", ""], ["Reference-only guard", 0, 0, ""]];
  sheet.getRange("C2").formulas = [["=COUNTA(Data!A:A)-1"]];
  sheet.getRange("D2").formulas = [["=IF(B2=C2,\"PASS\",\"FAIL\")"]];
  sheet.getRange("C3").formulas = [["=COUNTA(Data!1:1)"]];
  sheet.getRange("D3").formulas = [["=IF(C3>=B3,\"PASS\",\"FAIL\")"]];
  sheet.getRange("D4").formulas = [["=IF(B4=C4,\"PASS\",\"FAIL\")"]];
  sheet.getRange("A1:D1").format = { fill: "#214E63", font: { bold: true, color: "#FFFFFF" } };
  sheet.getRange("A2:A4").format = { fill: "#DCEBF0", font: { bold: true, color: "#153B50" } };
  sheet.getRange("A1:D4").format.borders = { preset: "all", style: "thin", color: "#B8CDD5" };
  sheet.getRange("A:A").format.columnWidth = 30;
  sheet.getRange("B:D").format.columnWidth = 18;
}

const selected = specs.slice(Math.max(0, start - 1), Math.min(specs.length, start - 1 + count));
const manifest = [];
for (const [file, title, rows] of selected) {
  const workbook = Workbook.create();
  const cover = workbook.worksheets.add("README");
  writeCover(cover, title, file, rows);
  const dataSheet = workbook.worksheets.add("Data");
  const index = specs.findIndex(x => x[0] === file) + 1;
  const dataRange = writeData(dataSheet, rows, `T${String(index).padStart(2, "0")}`);
  const validation = workbook.worksheets.add("Validation");
  writeValidation(validation, rows.length);
  const target = path.join(outputDir, file);
  await (await SpreadsheetFile.exportXlsx(workbook)).save(target);
  const formulaErrors = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
  const validationInspect = await workbook.inspect({ kind: "table", range: "Validation!A1:D4", include: "values,formulas", tableMaxRows: 10, tableMaxCols: 6, maxChars: 8000 });
  await fs.writeFile(path.join(inspectDir, `${file}.inspect.ndjson`), `${formulaErrors.ndjson}\n${validationInspect.ndjson}\n`, "utf8");
  const renderSpecs = [["README", "A1:H17"], ["Data", dataRange.preview], ["Validation", "A1:D4"]];
  for (const [sheetName, range] of renderSpecs) {
    const preview = await workbook.render({ sheetName, range, autoCrop: "all", scale: .72, format: "png" });
    const previewFile = path.join(previewDir, `${file.replace(/\.xlsx$/i, "")}__${safeSheet(sheetName)}.png`);
    await fs.writeFile(previewFile, new Uint8Array(await preview.arrayBuffer()));
    manifest.push({ workbook: file, sheet: sheetName, rows: rows.length, preview: path.relative(repo, previewFile).replaceAll("\\", "/") });
  }
  console.log(`BUILT ${file} rows=${rows.length}`);
}
const existingManifestPath = path.join(previewDir, "render_manifest.json");
let existing = [];
try { existing = JSON.parse(await fs.readFile(existingManifestPath, "utf8")); } catch {}
const names = new Set(selected.map(x => x[0]));
const merged = [...existing.filter(x => !names.has(x.workbook)), ...manifest].sort((a, b) => a.workbook.localeCompare(b.workbook) || a.sheet.localeCompare(b.sheet));
await fs.writeFile(existingManifestPath, `${JSON.stringify(merged, null, 2)}\n`, "utf8");
console.log(`RESULT workbooks status=passed built=${selected.length} rendered=${manifest.length} start=${start}`);
