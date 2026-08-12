import fs from "node:fs/promises";
import path from "node:path";

const repo = "E:/project/gamedevelop/MandateOfHeroes";
const taskId = "HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1";
const root = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE");
const masterPath = path.join(root, "COUNTY_PACKS/county_economy_master_v1.json");
const reportPath = path.join(root, "VALIDATION/data_validation_report.json");
const outputPath = path.join(repo, "outputs", taskId, "validation_summary.json");
const readJson = async p => JSON.parse(await fs.readFile(p, "utf8"));
const exists = async p => fs.access(p).then(() => true, () => false);
const d = await readJson(masterPath);
const checks = [];
const check = (id, condition, actual, expected, detail = "") => checks.push({ id, status: condition ? "PASS" : "FAIL", actual, expected, detail });
const unique = rows => new Set(rows).size;
const ids = d.counties.map(x => x.county_permanent_id);
const countySet = new Set(ids);
const products = new Set(d.product_taxonomy.map(x => x.category_id));
const scenarioYears = [...new Set(d.scenario_states.map(x => x.scenario_year))].sort((a, b) => a - b);

check("COUNTY_COUNT", d.counties.length === 1182, d.counties.length, 1182);
check("COUNTY_ID_UNIQUE", unique(ids) === 1182, unique(ids), 1182);
check("PROVINCE_COUNT", unique(d.counties.map(x => x.province_id)) === 13, unique(d.counties.map(x => x.province_id)), 13);
check("COMMANDERY_COUNT", unique(d.counties.map(x => x.commandery_equivalent_id)) === 105, unique(d.counties.map(x => x.commandery_equivalent_id)), 105);
check("POPULATION_184", d.counties.reduce((n, x) => n + x.population_184, 0) === 53500000, d.counties.reduce((n, x) => n + x.population_184, 0), 53500000);
check("SCENARIO_COUNT", scenarioYears.length === 13, scenarioYears.length, 13);
check("SCENARIO_STATE_COUNT", d.scenario_states.length === 1182 * 13, d.scenario_states.length, 15366);
check("SCENARIO_COVERAGE", scenarioYears.every(year => d.scenario_states.filter(x => x.scenario_year === year).length === 1182), scenarioYears.map(year => `${year}:${d.scenario_states.filter(x => x.scenario_year === year).length}`).join("|"), "1182 per scenario");
check("PRODUCT_TAXONOMY_UNIQUE", unique([...products]) === d.product_taxonomy.length, unique([...products]), d.product_taxonomy.length);
check("PRODUCT_BALANCE_COVERAGE", d.product_balances.length === 1182 * d.product_taxonomy.length, d.product_balances.length, 1182 * d.product_taxonomy.length);
check("PRODUCT_BALANCE_KEYS", d.product_balances.every(x => countySet.has(x.county_permanent_id) && products.has(x.product_category)), d.product_balances.filter(x => !countySet.has(x.county_permanent_id) || !products.has(x.product_category)).length, 0);
check("NONNEGATIVE_CLOSING_STOCK", d.product_balances.every(x => x.closing_stock >= 0), d.product_balances.filter(x => x.closing_stock < 0).length, 0);
check("UNMET_DEMAND_EXPLICIT", d.product_balances.every(x => Number.isFinite(x.unmet_demand) && x.unmet_demand >= 0), d.product_balances.filter(x => !Number.isFinite(x.unmet_demand) || x.unmet_demand < 0).length, 0);
check("BALANCE_EQUATION", d.product_balances.every(x => Math.abs((x.opening_stock + x.production + x.import - x.household_consumption - x.industrial_use - x.government_demand - x.military_demand - x.loss - x.export + x.unmet_demand) - x.closing_stock) <= 2), d.product_balances.filter(x => Math.abs((x.opening_stock + x.production + x.import - x.household_consumption - x.industrial_use - x.government_demand - x.military_demand - x.loss - x.export + x.unmet_demand) - x.closing_stock) > 2).length, 0, "Tolerance covers integer rounding only.");
check("TRADE_CONSERVATION", d.national_balance_184.every(x => Math.abs(x.internal_trade_conservation_error) <= 2), d.national_balance_184.filter(x => Math.abs(x.internal_trade_conservation_error) > 2).length, 0);
check("ROUTE_ENDPOINTS", d.supply_routes.every(x => countySet.has(x.from_county_id) && countySet.has(x.to_county_id) && x.distance_km > 0), d.supply_routes.filter(x => !countySet.has(x.from_county_id) || !countySet.has(x.to_county_id) || x.distance_km <= 0).length, 0);
const routeSet = new Set(d.supply_routes.map(x => x.route_id));
check("SUPPLY_RELATION_ROUTES", d.supply_relations.every(x => x.route_ids.split("|").every(id => routeSet.has(id))), d.supply_relations.filter(x => !x.route_ids.split("|").every(id => routeSet.has(id))).length, 0);
check("SUPPLY_RELATION_ORIGIN_PRODUCTION", d.supply_relations.every(x => d.product_balances.some(b => b.county_permanent_id === x.origin_county_id && b.product_category === x.product_category && b.production > 0)), d.supply_relations.filter(x => !d.product_balances.some(b => b.county_permanent_id === x.origin_county_id && b.product_category === x.product_category && b.production > 0)).length, 0);
check("TRANSPORT_LOSS_EXPLICIT", d.supply_relations.every(x => x.shipped_quantity >= x.delivered_quantity && x.loss_reference_basis_points > 0), d.supply_relations.filter(x => x.shipped_quantity < x.delivered_quantity || x.loss_reference_basis_points <= 0).length, 0);
check("LOCATION_STATUS_EXPLICIT", d.counties.every(x => Number.isFinite(x.longitude) && Number.isFinite(x.latitude) && x.coordinate_status && x.gis_geometry_status), d.counties.filter(x => !Number.isFinite(x.longitude) || !Number.isFinite(x.latitude) || !x.coordinate_status || !x.gis_geometry_status).length, 0);
check("MODELED_LOCATION_DISCLOSURE", d.counties.filter(x => x.coordinate_status === "MODELED_UNRESOLVED_COUNTY_REFERENCE").every(x => x.coordinate_confidence === "modeled_not_historical" && x.gis_geometry_status.includes("HISTORICAL_LOCATION_UNKNOWN")), d.counties.filter(x => x.coordinate_status === "MODELED_UNRESOLVED_COUNTY_REFERENCE" && (x.coordinate_confidence !== "modeled_not_historical" || !x.gis_geometry_status.includes("HISTORICAL_LOCATION_UNKNOWN"))).length, 0);
check("LAND_COVERAGE", d.counties.every(x => x.total_land_ha_reference > 0 && x.current_cultivated_land_ha >= 0 && x.arable_potential_ha >= x.current_cultivated_land_ha), d.counties.filter(x => !(x.total_land_ha_reference > 0 && x.current_cultivated_land_ha >= 0 && x.arable_potential_ha >= x.current_cultivated_land_ha)).length, 0);
check("TERRAIN_BASIS_POINT_CONSERVATION", d.counties.every(x => Math.abs(x.terrain_plain_basis_points + x.terrain_hill_basis_points + x.terrain_mountain_basis_points - 10000) <= 2 && x.terrain_mountain_basis_points > 0), d.counties.filter(x => Math.abs(x.terrain_plain_basis_points + x.terrain_hill_basis_points + x.terrain_mountain_basis_points - 10000) > 2 || x.terrain_mountain_basis_points <= 0).length, 0);
check("RESOURCE_COVERAGE", [d.livestock, d.forestry, d.fishery_gathering, d.salt, d.storage, d.market_service, d.transport, d.runtime_mapping].every(rows => rows.length === 1182), [d.livestock.length, d.forestry.length, d.fishery_gathering.length, d.salt.length, d.storage.length, d.market_service.length, d.transport.length, d.runtime_mapping.length].join("|"), "1182 each");
check("CROP_COVERAGE", d.crops.length === 1182 * d.crop_definitions.length, d.crops.length, 1182 * d.crop_definitions.length);
check("PROCESSING_CHAIN_COVERAGE", d.processing_dependencies.length === 1182 * 6, d.processing_dependencies.length, 1182 * 6);
check("UNKNOWN_BOUNDARY_COVERAGE", d.unknowns.filter(x => x.domain === "GEOGRAPHY").length === 1182, d.unknowns.filter(x => x.domain === "GEOGRAPHY").length, 1182);
check("REFERENCE_ONLY_NO_PEOPLE", d.summary.permanent_people_created === 0, d.summary.permanent_people_created, 0);
check("REFERENCE_ONLY_NO_FACILITIES", d.summary.facilities_created === 0, d.summary.facilities_created, 0);
check("LUOYANG_UNCHANGED", d.summary.luoyang_initialization_modified === false, d.summary.luoyang_initialization_modified, false);
check("MAP_COUNTY_GEOJSON", await exists(path.join(root, "MAP_OUTPUTS/county_economy_184.geojson")), await exists(path.join(root, "MAP_OUTPUTS/county_economy_184.geojson")), true);
check("MAP_SUPPLY_GEOJSON", await exists(path.join(root, "MAP_OUTPUTS/supply_corridors_184.geojson")), await exists(path.join(root, "MAP_OUTPUTS/supply_corridors_184.geojson")), true);

const failed = checks.filter(x => x.status === "FAIL");
const report = {
  schema: "mandate.county-economy-validation.v1",
  task_id: taskId,
  generated_at_utc: new Date().toISOString(),
  status: failed.length ? "FAILED" : "PASSED",
  checks,
  counts: { passed: checks.length - failed.length, failed: failed.length, total: checks.length },
  evidence_summary: { reconstructed_counties: d.summary.reconstructed_primary_count, modeled_counties: d.summary.modeled_primary_count, resolved_county_points: d.counties.filter(x => x.coordinate_status !== "MODELED_UNRESOLVED_COUNTY_REFERENCE").length, modeled_analytical_points: d.counties.filter(x => x.coordinate_status === "MODELED_UNRESOLVED_COUNTY_REFERENCE").length },
  scope_guards: { runtime_materialization: false, save_schema_changed: false, unity_code_changed: false },
};
await fs.mkdir(path.dirname(reportPath), { recursive: true });
await fs.mkdir(path.dirname(outputPath), { recursive: true });
await fs.writeFile(reportPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
await fs.writeFile(outputPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
console.log(`RESULT county-economy-validation status=${report.status.toLowerCase()} passed=${report.counts.passed} failed=${report.counts.failed}`);
if (failed.length) {
  for (const row of failed) console.error(`FAIL ${row.id} actual=${row.actual} expected=${row.expected}`);
  process.exitCode = 1;
}
