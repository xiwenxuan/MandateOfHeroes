import crypto from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";

const repo = "E:/project/gamedevelop/MandateOfHeroes";
const taskId = "HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_NETWORK_V1";
const root = path.join(repo, "Docs/HISTORICAL_WORLD_REFERENCE/HAN_135_260_COUNTY_PRODUCTION_RESOURCE_INDUSTRY_AND_SUPPLY_REFERENCE");
const outputRoot = path.join(repo, "outputs", taskId);
const dataRoot = path.join(root, "COUNTY_PACKS");
const mapRoot = path.join(root, "MAP_OUTPUTS");
const validationRoot = path.join(root, "VALIDATION");

const readJson = async (file) => JSON.parse(await fs.readFile(file, "utf8"));
const stableHash = (text) => crypto.createHash("sha256").update(String(text)).digest();
const hashUnit = (text, channel = 0) => {
  const h = stableHash(`${text}|${channel}`);
  return h.readUInt32LE(0) / 0xffffffff;
};
const clamp = (value, low, high) => Math.max(low, Math.min(high, value));
const round = (value, digits = 0) => {
  const m = 10 ** digits;
  return Math.round(value * m) / m;
};
const sum = (rows, selector) => rows.reduce((total, row) => total + selector(row), 0);
const sha256 = async (file) => crypto.createHash("sha256").update(await fs.readFile(file)).digest("hex");
const haversineKm = (a, b) => {
  const r = 6371;
  const p1 = a.latitude * Math.PI / 180;
  const p2 = b.latitude * Math.PI / 180;
  const dp = (b.latitude - a.latitude) * Math.PI / 180;
  const dl = (b.longitude - a.longitude) * Math.PI / 180;
  const q = Math.sin(dp / 2) ** 2 + Math.cos(p1) * Math.cos(p2) * Math.sin(dl / 2) ** 2;
  return 2 * r * Math.asin(Math.sqrt(q));
};

const profiles = {
  "admin.han140.sili": { id: "HELUO_GUANZHONG", climate: "temperate_monsoon", plain: .58, hill: .25, mountain: .17, water: .48, cultivatedPerRural: .27, yield: 1180, crop: { wheat: 3300, millet: 3000, broomcorn: 1000, rice: 350, soybean: 1150, hemp: 600, vegetable: 350, fruit: 250 }, pasture: .34, forest: .40, fish: .22, horse: .30, salt: .30, iron: .48, silk: .50, textile: .62, metallurgy: .62, market: .78, transport: .74, ship: .15 },
  "admin.han140.jizhou": { id: "HEBEI_PLAIN", climate: "temperate_monsoon", plain: .72, hill: .18, mountain: .10, water: .48, cultivatedPerRural: .31, yield: 1250, crop: { wheat: 2600, millet: 3500, broomcorn: 1100, rice: 250, soybean: 1300, hemp: 700, vegetable: 300, fruit: 250 }, pasture: .42, forest: .28, fish: .25, horse: .45, salt: .26, iron: .36, silk: .55, textile: .66, metallurgy: .45, market: .72, transport: .70, ship: .16 },
  "admin.han140.yuzhou": { id: "HUANGHUAI", climate: "temperate_monsoon", plain: .76, hill: .16, mountain: .08, water: .58, cultivatedPerRural: .32, yield: 1320, crop: { wheat: 2900, millet: 2700, broomcorn: 800, rice: 650, soybean: 1400, hemp: 650, vegetable: 500, fruit: 400 }, pasture: .31, forest: .28, fish: .32, horse: .24, salt: .18, iron: .28, silk: .62, textile: .72, metallurgy: .42, market: .70, transport: .72, ship: .18 },
  "admin.han140.yanzhou": { id: "YANZHOU_PLAIN", climate: "temperate_monsoon", plain: .78, hill: .14, mountain: .08, water: .58, cultivatedPerRural: .33, yield: 1340, crop: { wheat: 2900, millet: 3000, broomcorn: 800, rice: 450, soybean: 1400, hemp: 700, vegetable: 400, fruit: 350 }, pasture: .34, forest: .25, fish: .35, horse: .26, salt: .20, iron: .25, silk: .58, textile: .70, metallurgy: .40, market: .70, transport: .74, ship: .18 },
  "admin.han140.xuzhou": { id: "YISHU_JIANGHUAI", climate: "warm_temperate", plain: .70, hill: .16, mountain: .14, water: .70, cultivatedPerRural: .30, yield: 1380, crop: { wheat: 2200, millet: 1900, broomcorn: 600, rice: 1800, soybean: 1300, hemp: 700, vegetable: 800, fruit: 700 }, pasture: .28, forest: .34, fish: .55, horse: .18, salt: .56, iron: .24, silk: .52, textile: .68, metallurgy: .35, market: .68, transport: .76, ship: .42 },
  "admin.han140.qingzhou": { id: "SHANDONG_COAST", climate: "temperate_monsoon", plain: .58, hill: .26, mountain: .16, water: .62, cultivatedPerRural: .29, yield: 1270, crop: { wheat: 2500, millet: 2800, broomcorn: 900, rice: 500, soybean: 1300, hemp: 700, vegetable: 700, fruit: 600 }, pasture: .34, forest: .38, fish: .58, horse: .24, salt: .68, iron: .34, silk: .48, textile: .62, metallurgy: .42, market: .65, transport: .70, ship: .48 },
  "admin.han140.youzhou": { id: "YANSHAN_LIAODONG", climate: "cool_temperate", plain: .38, hill: .30, mountain: .32, water: .52, cultivatedPerRural: .24, yield: 1050, crop: { wheat: 1700, millet: 3600, broomcorn: 1600, rice: 200, soybean: 1200, hemp: 800, vegetable: 500, fruit: 400 }, pasture: .62, forest: .62, fish: .45, horse: .72, salt: .36, iron: .52, silk: .20, textile: .42, metallurgy: .48, market: .48, transport: .54, ship: .30 },
  "admin.han140.bingzhou": { id: "SHANXI_HETAO", climate: "continental", plain: .30, hill: .30, mountain: .40, water: .34, cultivatedPerRural: .22, yield: 920, crop: { wheat: 1500, millet: 3600, broomcorn: 1900, rice: 100, soybean: 1100, hemp: 1000, vegetable: 400, fruit: 400 }, pasture: .78, forest: .46, fish: .18, horse: .88, salt: .58, iron: .66, silk: .14, textile: .38, metallurgy: .58, market: .42, transport: .48, ship: .05 },
  "admin.han140.liangzhou": { id: "LONGYOU_HEXI", climate: "continental_arid", plain: .24, hill: .26, mountain: .50, water: .28, cultivatedPerRural: .20, yield: 850, crop: { wheat: 2300, millet: 2500, broomcorn: 1700, rice: 80, soybean: 900, hemp: 1100, vegetable: 700, fruit: 720 }, pasture: .92, forest: .30, fish: .10, horse: 1.00, salt: .72, iron: .64, silk: .16, textile: .36, metallurgy: .54, market: .36, transport: .46, ship: .03 },
  "admin.han140.jingzhou": { id: "JIANGHAN_DONGTING", climate: "humid_subtropical", plain: .42, hill: .30, mountain: .28, water: .86, cultivatedPerRural: .27, yield: 1510, crop: { wheat: 900, millet: 800, broomcorn: 300, rice: 4200, soybean: 1200, hemp: 600, vegetable: 1100, fruit: 900 }, pasture: .28, forest: .72, fish: .82, horse: .10, salt: .18, iron: .42, silk: .52, textile: .66, metallurgy: .42, market: .64, transport: .82, ship: .64 },
  "admin.han140.yangzhou": { id: "LOWER_YANGTZE_GANPO", climate: "humid_subtropical", plain: .40, hill: .34, mountain: .26, water: .90, cultivatedPerRural: .26, yield: 1580, crop: { wheat: 600, millet: 500, broomcorn: 200, rice: 4900, soybean: 1100, hemp: 500, vegetable: 1200, fruit: 1000 }, pasture: .22, forest: .76, fish: .92, horse: .08, salt: .64, iron: .48, silk: .68, textile: .72, metallurgy: .48, market: .72, transport: .90, ship: .82 },
  "admin.han140.yizhou": { id: "SICHUAN_HANZHONG", climate: "humid_basin_mountain", plain: .32, hill: .34, mountain: .34, water: .78, cultivatedPerRural: .29, yield: 1540, crop: { wheat: 1100, millet: 700, broomcorn: 300, rice: 3900, soybean: 1200, hemp: 500, vegetable: 1200, fruit: 1100 }, pasture: .34, forest: .78, fish: .55, horse: .14, salt: .90, iron: .56, silk: .86, textile: .74, metallurgy: .52, market: .62, transport: .60, ship: .38 },
  "admin.han140.jiaozhou": { id: "LINGNAN_RED_RIVER", climate: "tropical_subtropical", plain: .34, hill: .32, mountain: .34, water: .92, cultivatedPerRural: .24, yield: 1630, crop: { wheat: 150, millet: 350, broomcorn: 150, rice: 5500, soybean: 900, hemp: 400, vegetable: 1300, fruit: 1250 }, pasture: .18, forest: .92, fish: 1.00, horse: .03, salt: .72, iron: .36, silk: .44, textile: .52, metallurgy: .30, market: .46, transport: .68, ship: .74 },
};

const cropDefinitions = [
  ["crop.wheat", "麦", "product.wheat_grain", 1.00, "EXISTING_RUNTIME_ID"],
  ["crop.reference.millet", "粟", "product.reference.millet_grain", .95, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.broomcorn_millet", "黍", "product.reference.broomcorn_grain", .88, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.rice", "稻", "product.reference.rice_grain", 1.18, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.soybean", "菽", "product.reference.soybean", .82, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.hemp", "麻", "product.reference.hemp_fiber", .40, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.vegetable", "蔬菜", "product.reference.vegetable", 1.60, "REFERENCE_MAPPING_REQUIRED"],
  ["crop.reference.fruit", "果树", "product.reference.fruit", 1.25, "REFERENCE_MAPPING_REQUIRED"],
].map(([crop_id, display_name, product_id, yield_multiplier, runtime_mapping_status]) => ({ crop_id, display_name, product_id, yield_multiplier, runtime_mapping_status }));
const cropEnglishNames = ["Wheat", "Foxtail millet", "Broomcorn millet", "Rice", "Soybean", "Hemp", "Vegetables", "Fruit trees"];
cropDefinitions.forEach((row, index) => { row.display_name = cropEnglishNames[index]; });

const productTaxonomy = [
  ["GRAIN", "谷物", "kg", "FOOD_RAW", "product.wheat_grain"],
  ["OTHER_FOOD", "其他食物折算", "kg_food_equivalent", "FOOD_RAW", "product.reference.food_equivalent"],
  ["SALT", "盐", "kg", "SALT", "product.reference.salt"],
  ["FUEL", "燃料", "kg_charcoal_equivalent", "FUEL", "product.material.charcoal"],
  ["TIMBER", "木材", "kg", "TIMBER", "product.material.timber"],
  ["HORSE", "马匹", "head", "LIVESTOCK", "product.reference.horse"],
  ["LIVESTOCK", "其他牲畜", "head", "LIVESTOCK", "product.livestock.sheep"],
  ["LEATHER", "皮革", "kg", "LIVESTOCK", "product.material.leather"],
  ["FIBER", "纤维原料", "kg", "FIBER", "product.reference.fiber_raw"],
  ["RAW_SILK", "生丝", "kg", "FIBER", "product.reference.raw_silk"],
  ["TEXTILE", "织物", "bolt", "TEXTILE", "product.textile.plain_cloth"],
  ["IRON_ORE", "铁矿石", "kg", "MINERAL_ORE", "product.raw.iron_ore"],
  ["METAL", "铁料", "kg", "METAL", "product.material.iron"],
  ["TOOLS", "工具", "piece", "TOOL", "product.reference.tools"],
  ["WEAPONS", "兵器军需", "piece", "WEAPON", "product.equipment.han_ring_sword"],
  ["BUILDING_MATERIAL", "建筑材料", "kg", "BUILDING_MATERIAL", "product.reference.building_material"],
  ["POTTERY", "陶器", "piece", "POTTERY", "product.reference.pottery"],
  ["MEDICINE", "药材", "kg", "MEDICINE", "product.medicine.herbal_material"],
  ["TRANSPORT_EQUIPMENT", "车辆船舶", "unit", "VEHICLE", "product.reference.transport_equipment"],
].map(([category_id, display_name, normalized_unit, taxonomy_group, primary_product_id]) => ({ category_id, display_name, normalized_unit, taxonomy_group, primary_product_id }));
const productEnglishNames = ["Grain", "Other food equivalent", "Salt", "Fuel", "Timber", "Horse", "Other livestock", "Leather", "Raw fiber", "Raw silk", "Textile", "Iron ore", "Iron metal", "Tools", "Weapons and military demand", "Building material", "Pottery", "Medicinal material", "Cart and ship equipment"];
productTaxonomy.forEach((row, index) => { row.display_name = productEnglishNames[index]; });

const facilityReferences = [
  ["AGRICULTURE", "facility.farmland", "EXISTING_RUNTIME_TAG"],
  ["IRRIGATED_AGRICULTURE", "facility.irrigated_field", "EXISTING_RUNTIME_TAG"],
  ["LIVESTOCK", "facility.livestock.pasture", "EXISTING_RUNTIME_TAG"],
  ["FORESTRY", "facility.resource_extraction.logging_camp", "EXISTING_DOMAIN_ID"],
  ["MINING", "facility.resource_extraction.iron_mine", "EXISTING_DOMAIN_ID"],
  ["SALT", "facility.reference.saltworks", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["FOOD_PROCESSING", "facility.household_granary", "EXISTING_RUNTIME_TAG"],
  ["BREWING", "facility.reference.brewery", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["METALLURGY", "facility.primary_processing.bloomery", "EXISTING_RUNTIME_TAG"],
  ["METALWORKING", "facility.blacksmith_workshop", "EXISTING_RUNTIME_TAG"],
  ["TEXTILE", "facility.reference.weaving_workshop", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["SILK", "facility.reference.silk_workshop", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["LEATHER", "facility.primary_processing.tannery", "EXISTING_RUNTIME_TAG"],
  ["WOODWORK", "facility.woodworking_workshop", "EXISTING_RUNTIME_TAG"],
  ["POTTERY_BUILDING", "facility.reference.kiln", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["MEDICINE", "facility.primary_processing.herb_drying", "EXISTING_RUNTIME_TAG"],
  ["VEHICLE", "facility.vehicle_yard", "EXISTING_DOMAIN_ID"],
  ["SHIPBUILDING", "facility.reference.shipyard", "REFERENCE_CATALOG_REGISTRATION_REQUIRED"],
  ["MILITARY_ARMOR", "facility.armoring_workshop", "EXISTING_RUNTIME_TAG"],
  ["MILITARY_BOW", "facility.bowmaking_workshop", "EXISTING_RUNTIME_TAG"],
];

const evidenceRegistry = [
  { source_id: "source.project.hanworld.v1", title: "HanWorldV1统一Cell、县治点与物理地理包", source_type: "PROJECT_DATA", url: "repository://Assets/StreamingAssets/WorldMap/HanWorldV1", license: "Project original plus source manifest", usage: "County point/Cell, modern physical reference", imported: true },
  { source_id: "source.project.population.135_260.v1", title: "135—260全国人口母盘", source_type: "PROJECT_DATA", url: "repository://Assets/StreamingAssets/HistoricalPopulation/Han135260V1", license: "Project original and cited public-domain facts", usage: "184 county population and 13 scenario population", imported: true },
  { source_id: "source.project.development_place.full_pack.v1", title: "72 Development Place完整参考包", source_type: "PROJECT_REFERENCE", url: "repository://Docs/HISTORICAL_WORLD_REFERENCE/PLACE_FULL_DEVELOPMENT_REFERENCE_PACKS", license: "Project original reference", usage: "72 place reconstructed industry/resource hints", imported: true },
  { source_id: "source.hou_han_shu.jun_guo_zhi", title: "《后汉书·郡国志》", source_type: "HISTORICAL_TEXT", url: "https://ctext.org/hou-han-shu/ens", license: "Ancient public-domain text; modern host terms apply", usage: "Administrative and population anchor", imported: false },
  { source_id: "source.natural_earth.physical.v1", title: "Natural Earth physical layers", source_type: "GIS", url: "https://www.naturalearthdata.com/", license: "Public Domain", usage: "Modern coastline/river/lake physical reference already in HanWorldV1", imported: true },
  { source_id: "source.mapzen.srtm.v1", title: "Mapzen terrain tiles with SRTM/GMTED2010", source_type: "GIS", url: "https://registry.opendata.aws/terrain-tiles/", license: "Source-dependent attribution; project source manifest records China coverage", usage: "Physical elevation reference already in HanWorldV1", imported: true },
  { source_id: "source.chgis.locator_only", title: "China Historical GIS", source_type: "RESEARCH_LOCATOR", url: "https://chgis.fas.harvard.edu/data/chgis/v3/", license: "Academic research only; no commercial use/resale/redistribution", usage: "NOT IMPORTED; incompatible with commercial-ready redistribution", imported: false },
];
const evidenceEnglishTitles = {
  "source.project.hanworld.v1": "HanWorldV1 unified Cell and county-seat physical geography package",
  "source.project.population.135_260.v1": "135-260 national population master",
  "source.project.development_place.full_pack.v1": "72 Development Place full reference packs",
  "source.hou_han_shu.jun_guo_zhi": "Hou Han Shu, Commanderies and States",
};
evidenceRegistry.forEach(row => { if (evidenceEnglishTitles[row.source_id]) row.title = evidenceEnglishTitles[row.source_id]; });

const inputs = {
  countyGeo: path.join(repo, "Assets/StreamingAssets/WorldMap/HanWorldV1/locations/counties.json"),
  countyWeights: path.join(repo, "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/county_weights.json"),
  year184: path.join(repo, "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_184.json"),
  events: path.join(repo, "Assets/StreamingAssets/HistoricalPopulation/Han135260V1/events.json"),
  coreProduction: path.join(repo, "Assets/Resources/Content/Core/Production/core-production.json"),
  packWorkdata: path.join(repo, "outputs/HAN_135_260_DEVELOPMENT_PLACE_FULL_REFERENCE_PACK_V1/full_reference_pack_workdata.json"),
  worldSourceManifest: path.join(repo, "Assets/StreamingAssets/WorldMap/HanWorldV1/metadata/source_manifest.json"),
};

const [geo, weightsData, year184, eventData, coreProduction, packWorkdata, worldSources] = await Promise.all(Object.values(inputs).map(readJson));
const weightByCounty = new Map(weightsData.weights.map(x => [x.county_id, x]));
const population184 = new Map(year184.counties.map(x => [x.county_permanent_id, x]));
const placeHints = new Map();
for (const item of packWorkdata.industry_resource_supply_reference ?? []) {
  const countyId = item.PlaceId.replace(/^place\.han140\./, "admin.han140.");
  if (!placeHints.has(countyId)) placeHints.set(countyId, []);
  placeHints.get(countyId).push(item);
}
const existingProductIds = new Set(coreProduction.Products.map(x => x.Id));
for (const product of productTaxonomy) product.runtime_mapping_status = existingProductIds.has(product.primary_product_id) ? "EXISTING_RUNTIME_ID" : "REFERENCE_MAPPING_REQUIRED";

const counties = [];
const crops = [];
const livestock = [];
const forestry = [];
const fisheryGathering = [];
const minerals = [];
const salt = [];
const rawMaterials = [];
const processingCapacities = [];
const storage = [];
const marketService = [];
const transport = [];
const localDemands = [];
const productBalances = [];
const processingDependencies = [];
const unknowns = [];

const provinceFromCounty = (id) => id.split(".").slice(0, 3).join(".");
const provinceReferenceAnchors = {
  "admin.han140.sili": [112.45, 34.62],
  "admin.han140.jizhou": [115.50, 37.50],
  "admin.han140.yuzhou": [114.00, 33.80],
  "admin.han140.yanzhou": [116.50, 35.50],
  "admin.han140.xuzhou": [118.00, 34.20],
  "admin.han140.qingzhou": [118.50, 36.50],
  "admin.han140.youzhou": [117.00, 40.50],
  "admin.han140.bingzhou": [112.00, 38.00],
  "admin.han140.liangzhou": [103.50, 36.50],
  "admin.han140.jingzhou": [112.50, 30.50],
  "admin.han140.yangzhou": [117.50, 29.50],
  "admin.han140.yizhou": [104.50, 29.50],
  "admin.han140.jiaozhou": [108.50, 23.00],
};
const commanderyIdsByProvince = new Map();
for (const feature of geo.features) {
  const countyId = feature.properties.admin_unit_id;
  const provinceId = provinceFromCounty(countyId);
  if (!commanderyIdsByProvince.has(provinceId)) commanderyIdsByProvince.set(provinceId, new Set());
  commanderyIdsByProvince.get(provinceId).add(feature.properties.parent_admin_unit_id);
}
const commanderyAnchor = new Map();
for (const [provinceId, commanderySet] of commanderyIdsByProvince) {
  const commands = [...commanderySet].sort();
  const provinceAnchor = provinceReferenceAnchors[provinceId];
  if (!provinceAnchor) throw new Error(`Missing modeled province reference anchor for ${provinceId}`);
  commands.forEach((commanderyId, index) => {
    const located = geo.features.find(x => x.properties.parent_admin_unit_id === commanderyId && x.geometry?.coordinates);
    if (located) {
      commanderyAnchor.set(commanderyId, { longitude: located.geometry.coordinates[0], latitude: located.geometry.coordinates[1], status: "DERIVED_FROM_LOCATED_COUNTY" });
      return;
    }
    const ring = Math.floor(index / 8) + 1;
    const angle = ((index % 8) / 8) * Math.PI * 2 + hashUnit(commanderyId, 91) * .22;
    const radius = .55 + ring * .52;
    commanderyAnchor.set(commanderyId, { longitude: round(provinceAnchor[0] + Math.cos(angle) * radius / Math.max(.45, Math.cos(provinceAnchor[1] * Math.PI / 180)), 5), latitude: round(provinceAnchor[1] + Math.sin(angle) * radius, 5), status: "MODELED_FROM_PROVINCE_REFERENCE" });
  });
}
const countiesByCommandery = new Map();
for (const feature of geo.features) {
  const commanderyId = feature.properties.parent_admin_unit_id;
  if (!countiesByCommandery.has(commanderyId)) countiesByCommandery.set(commanderyId, []);
  countiesByCommandery.get(commanderyId).push(feature.properties.admin_unit_id);
}
for (const ids of countiesByCommandery.values()) ids.sort();
const resolvedCountyLocation = (feature) => {
  if (feature.geometry?.coordinates) return { longitude: feature.geometry.coordinates[0], latitude: feature.geometry.coordinates[1], status: feature.properties.coordinate_status, confidence: feature.properties.confidence, modeled: false };
  const commanderyId = feature.properties.parent_admin_unit_id;
  const anchor = commanderyAnchor.get(commanderyId);
  const ids = countiesByCommandery.get(commanderyId);
  const index = ids.indexOf(feature.properties.admin_unit_id);
  const angle = (index / Math.max(1, ids.length)) * Math.PI * 2 + hashUnit(feature.properties.admin_unit_id, 92) * .28;
  const radius = .08 + (index % 4) * .055;
  return { longitude: round(anchor.longitude + Math.cos(angle) * radius / Math.max(.45, Math.cos(anchor.latitude * Math.PI / 180)), 5), latitude: round(anchor.latitude + Math.sin(angle) * radius, 5), status: "MODELED_UNRESOLVED_COUNTY_REFERENCE", confidence: "modeled_not_historical", modeled: true };
};
for (const feature of [...geo.features].sort((a, b) => a.properties.admin_unit_id.localeCompare(b.properties.admin_unit_id))) {
  const p = feature.properties;
  const id = p.admin_unit_id;
  const pop = population184.get(id);
  const weight = weightByCounty.get(id);
  if (!pop || !weight) throw new Error(`Missing population or weight for ${id}`);
  const provinceId = provinceFromCounty(id);
  const profile = profiles[provinceId];
  if (!profile) throw new Error(`Missing regional profile for ${provinceId}`);
  const hints = placeHints.get(id) ?? [];
  const reconstructed = hints.some(x => x.EvidenceType === "RECONSTRUCTED");
  const variation = .88 + hashUnit(id, 1) * .24;
  const waterFactor = weight.water_weight_basis_points / 10000;
  const fertilityFactor = weight.fertility_weight_basis_points / 10000;
  const marketFactor = weight.market_weight_basis_points / 10000;
  const population = pop.modeled_actual_population;
  const households = Math.max(1, Math.round(population / (4.7 + hashUnit(id, 2) * .8)));
  const ruralPopulation = Math.max(0, population - pop.urban_settlement_population - pop.town_population);
  const areaRecommended = Math.max(20, population / Math.max(.15, pop.population_density));
  const plain = clamp(profile.plain * (.88 + hashUnit(id, 3) * .24), .04, .92);
  const mountain = clamp(profile.mountain * (.88 + hashUnit(id, 4) * .24), .03, .88);
  const hill = Math.max(.02, 1 - plain - mountain);
  const terrainTotal = plain + hill + mountain;
  const cultivatedHa = Math.min(areaRecommended * 70, ruralPopulation * profile.cultivatedPerRural * fertilityFactor * variation);
  const arablePotentialHa = Math.min(areaRecommended * 80, cultivatedHa / clamp(.58 + hashUnit(id, 5) * .24, .48, .88));
  const pastureHa = Math.min(areaRecommended * 65, areaRecommended * 100 * profile.pasture * (.25 + hashUnit(id, 6) * .20));
  const forestHa = Math.min(areaRecommended * 70, areaRecommended * 100 * profile.forest * (.32 + hashUnit(id, 7) * .28));
  const wetlandHa = Math.min(areaRecommended * 30, areaRecommended * 100 * profile.water * (.04 + hashUnit(id, 8) * .10));
  const laborPool = Math.round(population * (.53 + .105 * .5 + .10 * .35));
  const militaryPopulation = Math.round(population * (.012 + hashUnit(id, 9) * .012));
  const civilianEffectiveLabor = Math.max(0, laborPool - militaryPopulation);
  const agriculturalWorkers = Math.round(civilianEffectiveLabor * clamp(.52 + (1 - marketFactor) * .15 + hashUnit(id, 10) * .08, .45, .72));
  const craftWorkers = Math.round(civilianEffectiveLabor * clamp(.08 + profile.textile * .05 + profile.metallurgy * .03 + hashUnit(id, 11) * .03, .07, .19));
  const transportWorkers = Math.round(civilianEffectiveLabor * clamp(.025 + profile.transport * .04 + hashUnit(id, 12) * .015, .025, .085));
  const merchants = Math.round(civilianEffectiveLabor * clamp(.012 + profile.market * .025 + hashUnit(id, 13) * .012, .01, .055));
  const administrativeWorkers = Math.round(civilianEffectiveLabor * (weight.is_commandery_seat ? .025 : .009));
  const otherWorkers = Math.max(0, civilianEffectiveLabor - agriculturalWorkers - craftWorkers - transportWorkers - merchants - administrativeWorkers);
  const developmentFactor = clamp(.35 + Math.log10(Math.max(100, population)) / 12 + marketFactor * .18 + (weight.is_commandery_seat ? .08 : 0), .42, .95);
  const marketCapacity = round(population * profile.market * marketFactor * (weight.is_commandery_seat ? 1.35 : 1), 0);
  const roadCapacity = round(transportWorkers * (1.5 + profile.transport) * 365, 0);
  const waterCapacity = round(transportWorkers * profile.water * profile.ship * 6 * 365, 0);
  const reserveDays = round(18 + profile.market * 22 + (weight.is_commandery_seat ? 18 : 0) + hashUnit(id, 14) * 12, 1);
  const evidenceGrade = reconstructed ? "RECONSTRUCTED" : "MODELED";
  const evidenceMethod = reconstructed ? "REGIONAL_INFERENCE+DEVELOPMENT_PLACE_REFERENCE" : "GIS+POPULATION_MODEL+REGIONAL_INFERENCE";
  const location = resolvedCountyLocation(feature);

  const county = {
    county_permanent_id: id,
    county_name: p.display_name,
    province_id: provinceId,
    commandery_equivalent_id: p.parent_admin_unit_id,
    county_seat_place_id: p.stable_region_id,
    grid_version: p.grid_version,
    cell_id: p.cell_id,
    row: p.row,
    column: p.column,
    longitude: location.longitude,
    latitude: location.latitude,
    coordinate_status: location.status,
    coordinate_confidence: location.confidence,
    gis_geometry_status: location.modeled ? "MODELED_ANALYTICAL_POINT_BOUNDARY_AND_HISTORICAL_LOCATION_UNKNOWN" : "COUNTY_SEAT_POINT_ONLY_BOUNDARY_UNKNOWN",
    total_area_sq_km_low: round(areaRecommended * .75, 1),
    total_area_sq_km_recommended: round(areaRecommended, 1),
    total_area_sq_km_high: round(areaRecommended * 1.25, 1),
    area_method: "POPULATION_DENSITY_BACKSOLVE_PROXY",
    terrain_plain_basis_points: Math.round(plain / terrainTotal * 10000),
    terrain_hill_basis_points: Math.round(hill / terrainTotal * 10000),
    terrain_mountain_basis_points: Math.round(mountain / terrainTotal * 10000),
    elevation_reference: "HANWORLD_V1_CELL_LOOKUP_REQUIRED",
    major_water_reference: profile.id,
    water_access_basis_points: Math.round(clamp(profile.water * waterFactor, .05, 1) * 10000),
    wetland_potential_ha: round(wetlandHa, 1),
    flood_risk_basis_points: Math.round(clamp(profile.water * waterFactor * (.4 + hashUnit(id, 15) * .5), .05, .90) * 10000),
    drought_risk_basis_points: Math.round(clamp((1 - profile.water * waterFactor) * (.5 + hashUnit(id, 16) * .4), .05, .90) * 10000),
    locust_risk_basis_points: Math.round(clamp((plain + cultivatedHa / Math.max(1, areaRecommended * 100)) * .28, .05, .70) * 10000),
    cold_risk_basis_points: Math.round(clamp(.10 + (location.latitude - 25) / 35, .05, .80) * 10000),
    population_184: population,
    registered_population_184: pop.registered_population,
    households_184_modeled: households,
    children: Math.round(population * .265),
    youth: Math.round(population * .105),
    prime_workers: Math.round(population * .53),
    older_workers: Math.round(population * .07),
    retired: Math.round(population * .03),
    labor_pool: laborPool,
    civilian_effective_labor: civilianEffectiveLabor,
    military_population: militaryPopulation,
    agricultural_workers: agriculturalWorkers,
    craft_workers: craftWorkers,
    transport_workers: transportWorkers,
    merchants,
    administrative_workers: administrativeWorkers,
    other_workers: otherWorkers,
    total_land_ha_reference: round(areaRecommended * 100, 1),
    arable_potential_ha: round(arablePotentialHa, 1),
    current_cultivated_land_ha: round(cultivatedHa, 1),
    pasture_potential_ha: round(pastureHa, 1),
    forest_area_reference_ha: round(forestHa, 1),
    average_fertility_basis_points: Math.round(clamp(fertilityFactor, .70, 1.30) * 10000),
    irrigation_potential_basis_points: Math.round(clamp(profile.water * waterFactor * .85, .05, .95) * 10000),
    historical_irrigation_status: reconstructed && profile.water > .6 ? "REGIONAL_EVIDENCE_PRESENT_COUNTY_DETAIL_UNKNOWN" : "UNKNOWN",
    market_capacity_reference: marketCapacity,
    storage_reserve_days_reference: reserveDays,
    transport_capacity_tonne_km_reference: roadCapacity + waterCapacity,
    primary_evidence_grade: evidenceGrade,
    primary_method: evidenceMethod,
    source_ids: reconstructed ? "source.project.hanworld.v1|source.project.population.135_260.v1|source.project.development_place.full_pack.v1" : "source.project.hanworld.v1|source.project.population.135_260.v1",
    research_blocked: false,
    notes: "County boundary/precise area and most production values are modeled; no runtime Facility or inventory is created.",
  };
  county.terrain_mountain_basis_points = 10000 - county.terrain_plain_basis_points - county.terrain_hill_basis_points;
  counties.push(county);

  const cropShare = profile.crop;
  let grossGrain = 0;
  let grossOtherFood = 0;
  for (const definition of cropDefinitions) {
    const key = definition.crop_id.split(".").at(-1);
    const shareBp = cropShare[key] ?? 0;
    const areaHa = cultivatedHa * shareBp / 10000;
    const baseYield = profile.yield * definition.yield_multiplier * fertilityFactor * (.92 + hashUnit(id, `crop:${key}`) * .16);
    const gross = areaHa * baseYield;
    const seed = gross * (key === "vegetable" || key === "fruit" ? .03 : .08);
    const harvestLoss = gross * .055;
    const processingLoss = gross * .025;
    const storageLoss = gross * (.045 + (1 - profile.market) * .025);
    const usable = Math.max(0, gross - seed - harvestLoss - processingLoss - storageLoss);
    const row = {
      county_permanent_id: id, crop_id: definition.crop_id, crop_name: definition.display_name,
      product_id: definition.product_id, cultivated_share_basis_points: shareBp,
      cultivated_area_ha: round(areaHa, 1), planting_season: key === "wheat" ? "AUTUMN_OR_SPRING" : key === "rice" ? "SPRING" : "SPRING",
      growth_duration_days: key === "wheat" ? 180 : key === "rice" ? 150 : 120,
      harvest_window: key === "wheat" ? "LATE_SPRING_EARLY_SUMMER" : "AUTUMN",
      early_harvest_minimum_basis_points: 8000, multiple_cropping_potential_basis_points: Math.round(clamp((profile.water - .35) * 12000, 0, 8500)),
      water_demand_basis_points: key === "rice" ? 9500 : key === "vegetable" ? 7200 : 4600,
      labor_demand_basis_points: key === "rice" ? 8500 : key === "fruit" ? 7000 : 5500,
      yield_low_kg_ha: round(baseYield * .75, 0), yield_recommended_kg_ha: round(baseYield, 0), yield_high_kg_ha: round(baseYield * 1.25, 0),
      gross_output_kg: round(gross, 0), seed_retention_kg: round(seed, 0), harvest_loss_kg: round(harvestLoss, 0), processing_loss_kg: round(processingLoss, 0), storage_spoilage_kg: round(storageLoss, 0), usable_output_kg: round(usable, 0),
      evidence_grade: evidenceGrade, method_id: "PRODUCTION_MODEL_CROP_V1", runtime_mapping_status: definition.runtime_mapping_status,
    };
    crops.push(row);
    if (["wheat", "millet", "broomcorn_millet"].includes(key)) grossGrain += usable;
    else if (["rice"].includes(key)) grossGrain += usable;
    else if (["soybean", "vegetable", "fruit"].includes(key)) grossOtherFood += usable;
  }

  const livestockFactor = profile.pasture * (.75 + hashUnit(id, 20) * .5) * developmentFactor;
  const horseOutput = Math.round(population * .0065 * profile.horse * livestockFactor);
  const otherLivestockOutput = Math.round(population * .12 * livestockFactor);
  const meatFood = otherLivestockOutput * 18;
  const fishFood = population * 34 * profile.fish * waterFactor * (.65 + hashUnit(id, 21) * .5);
  const gatheringFood = population * 12 * profile.forest * (.6 + hashUnit(id, 22) * .5);
  livestock.push({ county_permanent_id: id, horse_breeding_stock: Math.round(population * .012 * profile.horse), horse_annual_output_head: horseOutput, cattle_reference_head: Math.round(population * .035 * (profile.pasture + .3)), sheep_goat_reference_head: Math.round(population * .08 * profile.pasture), pig_reference_head: Math.round(population * .06 * (1 - profile.pasture * .25)), poultry_reference_head: Math.round(population * .35), other_livestock_annual_output_head: otherLivestockOutput, meat_food_equivalent_kg: round(meatFood, 0), hide_raw_material_kg: round(otherLivestockOutput * 2.8, 0), manure_reference_kg: round(population * 75 * livestockFactor, 0), animal_power_reference: round(population * .025 * (profile.pasture + .4), 0), evidence_grade: evidenceGrade, method_id: "LIVESTOCK_MODEL_V1" });
  const timberOutput = forestHa * 260 * developmentFactor * (.8 + hashUnit(id, 23) * .4);
  const fuelOutput = forestHa * 620 * developmentFactor + cultivatedHa * 240;
  forestry.push({ county_permanent_id: id, timber_potential_kg: round(forestHa * 480, 0), actual_timber_extraction_kg: round(timberOutput, 0), fuelwood_charcoal_equivalent_kg: round(fuelOutput, 0), bamboo_potential: profile.id.match(/JIANG|YANGTZE|LINGNAN|SICHUAN/) ? "PRESENT_MODELED" : "LOW_OR_UNKNOWN", other_forest_products_reference: round(forestHa * 18, 0), evidence_grade: evidenceGrade, method_id: "FORESTRY_FUEL_MODEL_V1" });
  fisheryGathering.push({ county_permanent_id: id, river_fishing_food_kg: round(fishFood * .55, 0), lake_fishing_food_kg: round(fishFood * .25, 0), coastal_fishing_food_kg: round(fishFood * .20 * (location.longitude > 116 ? 1 : .15), 0), wild_plant_food_kg: round(gatheringFood, 0), medicinal_herbs_raw_kg: round(forestHa * (4 + hashUnit(id, 24) * 8), 0), hunting_food_kg: round(gatheringFood * .18, 0), evidence_grade: evidenceGrade, method_id: "FISHERY_GATHERING_MODEL_V1" });

  const mineralPotential = (kind, base, channel) => clamp(base * (.55 + hashUnit(id, channel) * .9) * (mountain + hill * .5 + .25), 0, 1);
  const mineralRows = [
    ["IRON", profile.iron, 30], ["COPPER", profile.metallurgy * .55, 31], ["LEAD", profile.metallurgy * .38, 32], ["TIN", provinceId.match(/jingzhou|yangzhou|jiaozhou/) ? .48 : .12, 33],
    ["GOLD", .16, 34], ["SILVER", .18, 35], ["MERCURY", provinceId.match(/yizhou|jingzhou/) ? .42 : .10, 36], ["STONE", .72, 37], ["CLAY", plain * .65 + waterFactor * .2, 38], ["LIMESTONE", mountain * .55 + hill * .3, 39],
  ];
  const ironPotential = mineralPotential("IRON", profile.iron, 30);
  const ironExploited = ironPotential > .58 && developmentFactor > .58;
  for (const [mineralId, base, channel] of mineralRows) {
    const potential = mineralPotential(mineralId, base, channel);
    const exploitable = potential >= .35;
    const exploited = exploitable && developmentFactor * potential >= .42;
    const output = exploited ? population * potential * (mineralId === "IRON" ? 18 : mineralId === "STONE" || mineralId === "CLAY" || mineralId === "LIMESTONE" ? 90 : 2.4) : 0;
    minerals.push({ county_permanent_id: id, mineral_id: mineralId, resource_state: potential > .18 ? "RESOURCE_PRESENT_MODELED" : "UNKNOWN_OR_LOW", economic_exploitability: exploitable ? "ECONOMICALLY_EXPLOITABLE_MODELED" : "LOW_OR_UNKNOWN", historical_exploitation: exploited ? "HISTORICALLY_EXPLOITED_MODELED" : "NO_COUNTY_EVIDENCE", potential_basis_points: Math.round(potential * 10000), actual_output_reference_kg: round(output, 0), evidence_grade: "MODELED", method_id: "RESOURCE_MODEL_MINERAL_V1" });
  }
  const saltPotential = clamp(profile.salt * (.65 + hashUnit(id, 40) * .7), 0, 1);
  const saltSourceType = provinceId.match(/yangzhou|qingzhou|xuzhou|jiaozhou/) && location.longitude > 115 ? "SEA_SALT" : provinceId === "admin.han140.yizhou" ? "WELL_SALT" : provinceId.match(/bingzhou|liangzhou/) ? "SALT_LAKE_OR_EARTH_SALT" : "OTHER_OR_IMPORT";
  const saltOutput = saltPotential > .48 ? population * saltPotential * 6.5 : 0;
  salt.push({ county_permanent_id: id, salt_source_type: saltSourceType, salt_potential_basis_points: Math.round(saltPotential * 10000), historical_salt_production_status: saltOutput > 0 ? "MODELED_ACTIVE" : "NO_COUNTY_EVIDENCE", facility_reference_id: "facility.reference.saltworks", workers_reference: saltOutput > 0 ? Math.round(saltOutput / 420) : 0, fuel_demand_kg: round(saltOutput * .18, 0), local_demand_kg: round(population * 3.2, 0), export_capacity_reference_kg: round(Math.max(0, saltOutput - population * 3.2), 0), government_control_reference: "UNKNOWN", actual_output_reference_kg: round(saltOutput, 0), evidence_grade: evidenceGrade, method_id: "SALT_RESOURCE_MODEL_V1" });
  rawMaterials.push({ county_permanent_id: id, clay_output_reference_kg: round(population * plain * 55, 0), stone_output_reference_kg: round(population * (hill + mountain) * 80, 0), timber_output_reference_kg: round(timberOutput, 0), bamboo_output_reference_kg: profile.id.match(/JIANG|YANGTZE|LINGNAN|SICHUAN/) ? round(population * .25, 0) : 0, leather_raw_output_kg: round(otherLivestockOutput * 2.8, 0), fiber_raw_output_kg: round(cultivatedHa * (cropShare.hemp / 10000) * 260, 0), dye_raw_material_kg: round(forestHa * profile.silk * 2.5, 0), medicinal_material_raw_kg: round(forestHa * 8, 0), lacquer_raw_kg: round(forestHa * (provinceId.match(/jingzhou|yizhou|yangzhou/) ? 1.8 : .15), 0), evidence_grade: evidenceGrade, method_id: "RAW_MATERIAL_MODEL_V1" });

  const capability = (base, workerShare, channel) => Math.round(craftWorkers * base * workerShare * (.75 + hashUnit(id, channel) * .5));
  const capacities = {
    FOOD_PROCESSING: capability(.75, .28, 50), BREWING: capability(.34, .08, 51), METALLURGY: ironExploited ? capability(profile.metallurgy, .10, 52) : capability(profile.metallurgy, .035, 52),
    METALWORKING: capability(profile.metallurgy, .13, 53), TEXTILE: capability(profile.textile, .24, 54), SILK: capability(profile.silk, .08, 55), LEATHER: capability(profile.pasture, .07, 56),
    WOODWORK: capability(profile.forest, .12, 57), POTTERY_BUILDING: capability(plain + hill * .5, .15, 58), MEDICINE: capability(profile.forest, .035, 59), VEHICLE: capability(profile.transport, .035, 60),
    SHIPBUILDING: capability(profile.ship, .025, 61), MILITARY: capability(profile.metallurgy * .5 + profile.forest * .3, .045, 62),
  };
  for (const [industryId, capacity] of Object.entries(capacities)) {
    const facility = facilityReferences.find(x => x[0] === industryId || (industryId === "MILITARY" && x[0] === "MILITARY_ARMOR"));
    processingCapacities.push({ county_permanent_id: id, industry_id: industryId, annual_capacity_reference: capacity, actual_development_basis_points: Math.round(developmentFactor * 10000), raw_material_dependency: industryId === "METALLURGY" && !ironExploited ? "IMPORT_REQUIRED" : industryId === "SHIPBUILDING" && profile.forest < .5 ? "IMPORT_REQUIRED" : "LOCAL_OR_MIXED", facility_reference_id: facility?.[1] ?? "facility.reference.unregistered", facility_mapping_status: facility?.[2] ?? "REFERENCE_CATALOG_REGISTRATION_REQUIRED", evidence_grade: evidenceGrade, method_id: "PROCESSING_CAPACITY_MODEL_V1" });
  }
  storage.push({ county_permanent_id: id, household_storage_kg: round(population * 260 / 365 * reserveDays * .42, 0), village_storage_kg: round(population * 260 / 365 * reserveDays * .20, 0), private_warehouse_kg: round(population * 260 / 365 * reserveDays * .14 * profile.market, 0), government_granary_kg: round(population * 260 / 365 * reserveDays * .18 * (weight.is_commandery_seat ? 1.5 : 1), 0), military_warehouse_kg: round(militaryPopulation * 300 / 365 * reserveDays, 0), port_warehouse_kg: round(population * profile.ship * profile.water * 18, 0), strategic_warehouse_status: weight.is_capital_county ? "CAPITAL_STRATEGIC_REFERENCE" : weight.is_commandery_seat ? "REGIONAL_REFERENCE" : "NONE_EVIDENCED", evidence_grade: "MODELED", method_id: "STORAGE_CAPACITY_MODEL_V1" });
  marketService.push({ county_permanent_id: id, local_market_capacity: marketCapacity, regional_market_role: weight.is_capital_county ? "CAPITAL_CONSUMER_HUB" : weight.is_commandery_seat ? "COMMANDERY_MARKET_HUB" : profile.market > .65 ? "REGIONAL_MARKET" : "LOCAL_MARKET", storage_service_capacity: round(marketCapacity * .65, 0), transport_service_capacity: roadCapacity + waterCapacity, medical_service_capacity: capacities.MEDICINE, education_service_capacity: round(administrativeWorkers * .22, 0), administrative_service_capacity: administrativeWorkers, ship_transport_service_capacity: waterCapacity, evidence_grade: "MODELED", method_id: "MARKET_SERVICE_MODEL_V1" });
  transport.push({ county_permanent_id: id, road_transport_capacity_tonne_km: roadCapacity, pack_animal_capacity_tonne_km: round(roadCapacity * profile.pasture * .65, 0), cart_transport_capacity_tonne_km: round(roadCapacity * (plain + .15), 0), river_transport_capacity_tonne_km: round(waterCapacity * .65, 0), canal_transport_capacity_tonne_km: round(waterCapacity * .18 * (plain + profile.water), 0), sea_transport_capacity_tonne_km: round(waterCapacity * .17 * profile.ship, 0), normal_loss_basis_points_per_100km: Math.round(120 + (1 - profile.transport) * 190), route_risk_basis_points: Math.round(clamp((1 - marketFactor) * .35 + hashUnit(id, 63) * .25, .08, .55) * 10000), evidence_grade: "MODELED", method_id: "TRANSPORT_MODEL_V1" });

  const demand = {
    GRAIN: population * 220, OTHER_FOOD: population * 62, SALT: population * 3.2, FUEL: population * 390, TIMBER: population * 52,
    HORSE: population * .006, LIVESTOCK: population * .065, LEATHER: population * 1.3, FIBER: population * 2.4, RAW_SILK: population * .045,
    TEXTILE: population * .72, IRON_ORE: capacities.METALLURGY * 11, METAL: population * 5.5 + capacities.METALWORKING * 4, TOOLS: population * .18,
    WEAPONS: militaryPopulation * .13, BUILDING_MATERIAL: population * 95, POTTERY: population * .42, MEDICINE: population * .32, TRANSPORT_EQUIPMENT: population * .0025,
  };
  const production = {
    GRAIN: grossGrain, OTHER_FOOD: grossOtherFood + meatFood + fishFood + gatheringFood, SALT: saltOutput, FUEL: fuelOutput, TIMBER: timberOutput,
    HORSE: horseOutput, LIVESTOCK: otherLivestockOutput, LEATHER: otherLivestockOutput * 2.1, FIBER: cultivatedHa * cropShare.hemp / 10000 * 260, RAW_SILK: population * profile.silk * developmentFactor * .07,
    TEXTILE: capacities.TEXTILE * 3.4, IRON_ORE: ironExploited ? population * ironPotential * 18 : 0, METAL: capacities.METALLURGY * 5.2, TOOLS: capacities.METALWORKING * 1.7,
    WEAPONS: capacities.MILITARY * .65, BUILDING_MATERIAL: capacities.POTTERY_BUILDING * 28 + population * (plain + hill) * 45, POTTERY: capacities.POTTERY_BUILDING * 2.4,
    MEDICINE: capacities.MEDICINE * 1.8, TRANSPORT_EQUIPMENT: capacities.VEHICLE * .18 + capacities.SHIPBUILDING * .08,
  };
  for (const product of productTaxonomy) {
    const category = product.category_id;
    const householdDemand = demand[category] * (category === "WEAPONS" || category === "IRON_ORE" ? 0 : .72);
    const industrialUse = demand[category] * (category === "IRON_ORE" || category === "METAL" || category === "TIMBER" || category === "FUEL" || category === "FIBER" || category === "LEATHER" ? .42 : .10);
    const governmentDemand = demand[category] * (weight.is_commandery_seat ? .07 : .025);
    const militaryDemand = demand[category] * (category === "GRAIN" || category === "OTHER_FOOD" || category === "FUEL" || category === "TEXTILE" || category === "WEAPONS" || category === "HORSE" ? .15 : .05);
    const loss = production[category] * (category === "GRAIN" || category === "OTHER_FOOD" ? .06 : category === "FUEL" || category === "TIMBER" ? .025 : .012);
    const reserveTarget = demand[category] * reserveDays / 365;
    const totalDemand = householdDemand + industrialUse + governmentDemand + militaryDemand;
    const net = production[category] - totalDemand - loss;
    const status = net > totalDemand * .30 ? "MAJOR_SURPLUS" : net > totalDemand * .05 ? "SURPLUS" : net < -totalDemand * .30 ? "MAJOR_DEFICIT" : net < -totalDemand * .05 ? "DEFICIT" : "BALANCED";
    localDemands.push({ county_permanent_id: id, product_category: category, household_consumption: round(householdDemand, 0), industrial_input: round(industrialUse, 0), government_demand: round(governmentDemand, 0), military_demand: round(militaryDemand, 0), total_demand: round(totalDemand, 0), normalized_unit: product.normalized_unit, evidence_grade: "MODELED", method_id: "LOCAL_DEMAND_MODEL_V1" });
    productBalances.push({ county_permanent_id: id, product_category: category, opening_stock: round(reserveTarget * .35, 0), production: round(production[category], 0), import: 0, household_consumption: round(householdDemand, 0), industrial_use: round(industrialUse, 0), government_demand: round(governmentDemand, 0), military_demand: round(militaryDemand, 0), loss: round(loss, 0), export: 0, closing_stock: round(Math.max(0, reserveTarget * .35 + net), 0), net_balance_before_trade: round(net, 0), surplus_deficit_status: status, reserve_target: round(reserveTarget, 0), normalized_unit: product.normalized_unit, evidence_grade: "MODELED", method_id: "PRODUCT_BALANCE_MODEL_V1" });
  }
  const dependencyRows = [
    ["IRON_METAL_TOOLS_WEAPONS", ironExploited ? "LOCAL" : "IMPORT_REQUIRED", "facility.resource_extraction.iron_mine→facility.primary_processing.bloomery→facility.blacksmith_workshop"],
    ["TIMBER_TO_CART_SHIP", profile.forest > .48 ? "LOCAL" : "IMPORT_REQUIRED", "facility.resource_extraction.logging_camp→facility.woodworking_workshop/vehicle_yard/shipyard"],
    ["FIBER_TO_TEXTILE", cropShare.hemp >= 600 ? "LOCAL" : "IMPORT_REQUIRED", "facility.farmland→facility.reference.weaving_workshop"],
    ["MULBERRY_TO_SILK", profile.silk > .55 ? "LOCAL_OR_MIXED" : "IMPORT_REQUIRED", "facility.farmland→facility.reference.silk_workshop"],
    ["LIVESTOCK_TO_LEATHER", profile.pasture > .40 ? "LOCAL" : "IMPORT_REQUIRED", "facility.livestock.pasture→facility.primary_processing.tannery"],
    ["CLAY_TO_POTTERY_BUILDING", plain + hill > .50 ? "LOCAL" : "LOCAL_OR_MIXED", "resource.clay→facility.reference.kiln"],
  ];
  for (const [chainId, dependency, chain] of dependencyRows) processingDependencies.push({ county_permanent_id: id, chain_id: chainId, dependency_status: dependency, chain_reference: chain.replace(/[^\x00-\x7F]+/g, "->"), evidence_grade: "MODELED", method_id: "PROCESSING_CHAIN_DEPENDENCY_V1" });
  unknowns.push({ gap_id: `gap.county.boundary.${id}`, county_permanent_id: id, domain: "GEOGRAPHY", status: "UNKNOWN", reason: "HanWorldV1 publishes a county-seat point and Cell, not a historical county polygon.", runtime_impact: "County profile is calibration/reference only; Cell remains runtime authority.", blocker: false, recommended_research: "Compatible-license historical boundary reconstruction." });
  unknowns.push({ gap_id: `gap.county.industry.${id}`, county_permanent_id: id, domain: "HISTORICAL_ACTUAL_INDUSTRY", status: reconstructed ? "PARTIAL_RECONSTRUCTED" : "MODELED", reason: reconstructed ? "Regional/place evidence exists but county quantities remain unknown." : "No county-specific historical production quantity evidence in current project sources.", runtime_impact: "Do not materialize Facility instances without a later readiness review.", blocker: false, recommended_research: "County/commandery archaeology, local production history and route evidence." });
}

// Sparse county graph: six nearest neighbors, plus same-commandery preference. It is a modeled route graph,
// never a claim that an ancient road followed the straight line between county-seat points.
const countyById = new Map(counties.map(x => [x.county_permanent_id, x]));
const edgeMap = new Map();
for (const county of counties) {
  const candidates = counties.filter(x => x.county_permanent_id !== county.county_permanent_id).map(other => ({ other, distance: haversineKm(county, other), sameCommandery: other.commandery_equivalent_id === county.commandery_equivalent_id, sameProvince: other.province_id === county.province_id }));
  candidates.sort((a, b) => {
    const aScore = a.distance - (a.sameCommandery ? 80 : 0) - (a.sameProvince ? 30 : 0);
    const bScore = b.distance - (b.sameCommandery ? 80 : 0) - (b.sameProvince ? 30 : 0);
    return aScore - bScore;
  });
  for (const candidate of candidates.slice(0, 6)) {
    const ids = [county.county_permanent_id, candidate.other.county_permanent_id].sort();
    const key = ids.join("|");
    if (!edgeMap.has(key)) {
      const distance = haversineKm(countyById.get(ids[0]), countyById.get(ids[1]));
      const fromProfile = profiles[countyById.get(ids[0]).province_id];
      const toProfile = profiles[countyById.get(ids[1]).province_id];
      const waterAffinity = Math.max(fromProfile.water * fromProfile.ship, toProfile.water * toProfile.ship);
      edgeMap.set(key, { route_id: `route.county.${stableHash(key).toString("hex").slice(0, 16)}`, from_county_id: ids[0], to_county_id: ids[1], distance_km: round(distance, 1), route_kind: "MODELED_COUNTY_CORRIDOR", transport_mode: waterAffinity > .42 ? "MIXED_REFERENCE" : "ROAD_PACK_CART_REFERENCE", evidence_grade: "MODELED", method_id: "K_NEAREST_COUNTY_ROUTE_V1", historical_route_claim: false });
    }
  }
}
const supplyRoutes = [...edgeMap.values()].sort((a, b) => a.route_id.localeCompare(b.route_id));
const adjacency = new Map(counties.map(x => [x.county_permanent_id, []]));
for (const edge of supplyRoutes) {
  adjacency.get(edge.from_county_id).push({ id: edge.to_county_id, edge });
  adjacency.get(edge.to_county_id).push({ id: edge.from_county_id, edge });
}
const pathCache = new Map();
const shortestPath = (originId, destinationId) => {
  const cacheKey = `${originId}|${destinationId}`;
  if (pathCache.has(cacheKey)) return pathCache.get(cacheKey);
  const dist = new Map([[originId, 0]]);
  const prev = new Map();
  const open = new Set([originId]);
  while (open.size) {
    let current = null;
    let best = Infinity;
    for (const id of open) { const d = dist.get(id) ?? Infinity; if (d < best) { best = d; current = id; } }
    if (current === destinationId || current === null) break;
    open.delete(current);
    for (const next of adjacency.get(current) ?? []) {
      const candidate = best + next.edge.distance_km;
      if (candidate < (dist.get(next.id) ?? Infinity)) { dist.set(next.id, candidate); prev.set(next.id, { id: current, route: next.edge.route_id }); open.add(next.id); }
    }
  }
  if (!dist.has(destinationId)) return null;
  const routeIds = [];
  let cursor = destinationId;
  while (cursor !== originId) { const step = prev.get(cursor); if (!step) return null; routeIds.push(step.route); cursor = step.id; }
  routeIds.reverse();
  const result = { distance_km: dist.get(destinationId), route_ids: routeIds };
  pathCache.set(cacheKey, result);
  return result;
};

const balanceByProduct = new Map(productTaxonomy.map(x => [x.category_id, productBalances.filter(y => y.product_category === x.category_id)]));
const supplyRelations = [];
for (const product of productTaxonomy) {
  const rows = balanceByProduct.get(product.category_id);
  const supply = rows.filter(x => x.net_balance_before_trade > 0).map(x => ({ id: x.county_permanent_id, remaining: x.net_balance_before_trade }));
  const deficits = rows.filter(x => x.net_balance_before_trade < 0).map(x => ({ id: x.county_permanent_id, remaining: -x.net_balance_before_trade }));
  for (const deficit of deficits.sort((a, b) => b.remaining - a.remaining)) {
    let sourceCount = 0;
    while (deficit.remaining > 0 && sourceCount < 4) {
      const destinationCounty = countyById.get(deficit.id);
      let best = null;
      for (const origin of supply) {
        if (origin.remaining <= 0) continue;
        const originCounty = countyById.get(origin.id);
        const direct = haversineKm(originCounty, destinationCounty);
        const politicalPenalty = originCounty.province_id === destinationCounty.province_id ? 0 : 120;
        const score = direct + politicalPenalty;
        if (!best || score < best.score) best = { origin, score, direct };
      }
      if (!best) break;
      const route = shortestPath(best.origin.id, deficit.id);
      if (!route) { best.origin.remaining = 0; continue; }
      const originCounty = countyById.get(best.origin.id);
      const transportCap = Math.max(1, originCounty.transport_capacity_tonne_km_reference / Math.max(1, route.distance_km));
      const categoryWeightKg = ["HORSE", "LIVESTOCK", "TRANSPORT_EQUIPMENT"].includes(product.category_id) ? 350 : ["TEXTILE", "TOOLS", "WEAPONS", "POTTERY"].includes(product.category_id) ? 5 : 1;
      const capacityQuantity = transportCap * 1000 / categoryWeightKg;
      const quantity = Math.min(deficit.remaining, best.origin.remaining, capacityQuantity);
      if (quantity <= 0) { best.origin.remaining = 0; continue; }
      const lossBp = Math.round(clamp(70 + route.distance_km * 1.1, 80, 2600));
      const delivered = quantity * (10000 - lossBp) / 10000;
      const relationId = `supply.${product.category_id.toLowerCase()}.${stableHash(`${best.origin.id}|${deficit.id}`).toString("hex").slice(0, 16)}`;
      supplyRelations.push({ relation_id: relationId, origin_county_id: best.origin.id, destination_county_id: deficit.id, product_category: product.category_id, route_ids: route.route_ids.join("|"), transport_mode: route.route_ids.length > 3 ? "MULTI_LEG_MIXED_REFERENCE" : "ROAD_PACK_CART_REFERENCE", distance_km: round(route.distance_km, 1), travel_time_days: Math.max(1, Math.ceil(route.distance_km / 28)), normal_capacity: round(quantity, 0), peak_capacity: round(quantity * 1.25, 0), shipped_quantity: round(quantity, 0), delivered_quantity: round(delivered, 0), loss_reference_basis_points: lossBp, risk_basis_points: Math.round(clamp(900 + route.distance_km * 3, 900, 6500)), political_dependency: countyById.get(best.origin.id).province_id === destinationCounty.province_id ? "INTRA_PROVINCE" : "CROSS_PROVINCE_CONTROL_REQUIRED", logistics_type: product.category_id === "WEAPONS" || product.category_id === "HORSE" ? "COMMERCIAL_OR_MILITARY" : "COMMERCIAL", evidence_grade: "MODELED", historical_evidence: "NO_DIRECT_ROUTE_CLAIM" });
      best.origin.remaining -= quantity;
      deficit.remaining -= delivered;
      const originBalance = rows.find(x => x.county_permanent_id === best.origin.id);
      const destinationBalance = rows.find(x => x.county_permanent_id === deficit.id);
      originBalance.export += round(quantity, 0);
      destinationBalance.import += round(delivered, 0);
      sourceCount += 1;
    }
  }
}
for (const row of productBalances) {
  const rawClosingStock = row.opening_stock + row.production + row.import - row.household_consumption - row.industrial_use - row.government_demand - row.military_demand - row.loss - row.export;
  row.unmet_demand = round(Math.max(0, -rawClosingStock), 0);
  row.closing_stock = round(Math.max(0, rawClosingStock), 0);
  row.import_dependency_ratio = row.household_consumption + row.industrial_use + row.government_demand + row.military_demand > 0 ? round(row.import / (row.household_consumption + row.industrial_use + row.government_demand + row.military_demand), 4) : 0;
}

const scenarioYears = [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260];
const scenarioStates = [];
for (const year of scenarioYears) {
  const snapshot = await readJson(path.join(repo, `Assets/StreamingAssets/HistoricalPopulation/Han135260V1/years/year_${year}.json`));
  const byId = new Map(snapshot.counties.map(x => [x.county_permanent_id, x]));
  for (const county of counties) {
    const p = byId.get(county.county_permanent_id);
    const populationRatio = p.modeled_actual_population / Math.max(1, county.population_184);
    const eventIds = (eventData.events ?? []).filter(e => year >= e.start_year && year <= e.end_year && e.affected_provinces.includes(county.province_id)).map(e => e.event_id);
    scenarioStates.push({ scenario_year: year, county_permanent_id: county.county_permanent_id, modeled_actual_population: p.modeled_actual_population, civilian_effective_labor_reference: Math.round(county.civilian_effective_labor * populationRatio), cultivated_land_ha_reference: round(county.current_cultivated_land_ha * clamp(.55 + populationRatio * .45, .45, 1.25), 1), production_capacity_basis_points: Math.round(clamp(.48 + populationRatio * .52 - eventIds.length * .025, .35, 1.25) * 10000), storage_condition_basis_points: Math.round(clamp(.72 + populationRatio * .28 - eventIds.length * .035, .35, 1.10) * 10000), transport_condition_basis_points: Math.round(clamp(.75 + populationRatio * .25 - eventIds.length * .04, .30, 1.10) * 10000), inherited_from_year: year === 140 ? null : scenarioYears[scenarioYears.indexOf(year) - 1], applied_change_point_ids: eventIds.join("|"), evidence_grade: "MODELED", method_id: "SCENARIO_INHERITANCE_POPULATION_CHANGEPOINT_V1" });
  }
}

const changePoints = (eventData.events ?? []).map(e => ({ change_point_id: `economy.${e.event_id}`, start_year: e.start_year, end_year: e.end_year, affected_province_ids: e.affected_provinces.join("|"), change_type: e.impact_type, production_damage_low_basis_points: Math.round(e.severity_basis_points * 2), production_damage_recommended_basis_points: Math.round(e.severity_basis_points * 4), production_damage_high_basis_points: Math.round(e.severity_basis_points * 7), transport_damage_recommended_basis_points: Math.round(e.severity_basis_points * 3), population_source_id: e.source_id, evidence_grade: e.confidence === "B" ? "RECONSTRUCTED" : "MODELED", notes: "Reference range only; runtime must alter concrete people, land, facilities, inventories and routes." }));

const nationalBalance = productTaxonomy.map(product => {
  const rows = balanceByProduct.get(product.category_id);
  const production = sum(rows, x => x.production);
  const demand = sum(rows, x => x.household_consumption + x.industrial_use + x.government_demand + x.military_demand);
  const loss = sum(rows, x => x.loss);
  const imports = sum(rows, x => x.import);
  const exports = sum(rows, x => x.export);
  const opening = sum(rows, x => x.opening_stock);
  const closing = sum(rows, x => x.closing_stock);
  return { product_category: product.category_id, normalized_unit: product.normalized_unit, opening_stock: round(opening, 0), production: round(production, 0), internal_imports: round(imports, 0), demand: round(demand, 0), loss: round(loss, 0), internal_exports: round(exports, 0), closing_stock: round(closing, 0), net_before_trade: round(production - demand - loss, 0), production_coverage_ratio: demand > 0 ? round(production / demand, 4) : null, internal_trade_conservation_error: round(exports - imports - sum(supplyRelations.filter(x => x.product_category === product.category_id), x => x.shipped_quantity - x.delivered_quantity), 0), evidence_grade: "MODELED" };
});

const topLists = {};
for (const product of productTaxonomy) {
  const rows = balanceByProduct.get(product.category_id);
  topLists[product.category_id] = [...rows].sort((a, b) => b.net_balance_before_trade - a.net_balance_before_trade).slice(0, 20).map((x, index) => ({ rank: index + 1, county_permanent_id: x.county_permanent_id, county_name: countyById.get(x.county_permanent_id).county_name, net_balance_before_trade: x.net_balance_before_trade, status: x.surplus_deficit_status }));
}

const regionalZones = [];
for (const [provinceId, profile] of Object.entries(profiles)) {
  const members = counties.filter(x => x.province_id === provinceId);
  for (const product of productTaxonomy) {
    const rows = productBalances.filter(x => x.product_category === product.category_id && members.some(c => c.county_permanent_id === x.county_permanent_id));
    regionalZones.push({ zone_id: `zone.${profile.id.toLowerCase()}.${product.category_id.toLowerCase()}`, zone_name: profile.id, province_id: provinceId, product_category: product.category_id, county_count: members.length, production: round(sum(rows, x => x.production), 0), demand: round(sum(rows, x => x.household_consumption + x.industrial_use + x.government_demand + x.military_demand), 0), net_balance_before_trade: round(sum(rows, x => x.net_balance_before_trade), 0), evidence_grade: "MODELED", administrative_region_claim: false });
  }
}

const runtimeMapping = counties.map(x => ({ county_permanent_id: x.county_permanent_id, county_seat_place_id: x.county_seat_place_id, grid_version: x.grid_version, anchor_cell_id: x.cell_id, runtime_authority: "CELL+RESOURCE+FACILITY+WORKER+RECIPE+INVENTORY", county_reference_role: "SCENARIO_INITIALIZATION+CALIBRATION+AI_PLANNING+STATISTICS", runtime_facilities_created: 0, runtime_people_created: 0, runtime_inventory_created: 0, mapping_status: "REFERENCE_READY_MATERIALIZATION_REVIEW_REQUIRED" }));

const methodRegistry = [
  { method_id: "GIS+POPULATION_MODEL+REGIONAL_INFERENCE", description: "HanWorldV1 county-seat point/Cell, M13 weights and province profile; no historical boundary claim." },
  { method_id: "PRODUCTION_MODEL_CROP_V1", description: "Cultivated area × explicit crop share × regional yield × fertility/water proxy, with seed and losses itemized." },
  { method_id: "RESOURCE_MODEL_MINERAL_V1", description: "Regional geology potential proxy × terrain × stable county variation × population development factor; not a mine claim." },
  { method_id: "TRANSPORT_MODEL_V1", description: "Real county points and sparse six-neighbor modeled corridors; not a claim of exact ancient road geometry." },
  { method_id: "SCENARIO_INHERITANCE_POPULATION_CHANGEPOINT_V1", description: "13 population snapshots plus project historical event change points; runtime may diverge after start." },
];
methodRegistry.find(x => x.method_id === "PRODUCTION_MODEL_CROP_V1").description = "Cultivated area x explicit crop share x regional yield x fertility/water proxy, with seed and losses itemized.";
methodRegistry.find(x => x.method_id === "RESOURCE_MODEL_MINERAL_V1").description = "Regional geology potential proxy x terrain x stable county variation x population development factor; not a mine claim.";
methodRegistry.find(x => x.method_id === "TRANSPORT_MODEL_V1").description = "Resolved county points where available plus explicit modeled analytical fallbacks and a sparse six-neighbor corridor graph; not a claim of exact historical location or ancient route geometry.";

const inputHashes = {};
for (const [key, file] of Object.entries(inputs)) inputHashes[key] = { path: path.relative(repo, file).replaceAll("\\", "/"), sha256: await sha256(file) };
const dataset = {
  schema: "mandate.han135260.county-production-economy-reference.v1",
  task_id: taskId,
  generated_on: "2026-08-11",
  status: "REFERENCE_DATA_COMPLETE_RUNTIME_NOT_MATERIALIZED",
  contracts: {
    no_mandatory_self_sufficiency: true,
    county_reference_is_not_runtime_authority: true,
    runtime_authority: "CELL+RESOURCE+FACILITY+WORKER+RECIPE+PRODUCTION+INVENTORY+TRANSPORT+MARKET/GOVERNMENT/MILITARY/HOUSEHOLD",
    resource_potential_distinct_from_historical_exploitation: true,
    service_capacity_distinct_from_physical_inventory: true,
    scenario_initialization_may_diverge_at_runtime: true,
  },
  input_hashes: inputHashes,
  model_parameters: {
    national_population_184: year184.national.modeled_actual_population_start,
    food_demand_kg_per_person_year: 282,
    grain_demand_kg_per_person_year: 220,
    other_food_demand_kg_per_person_year: 62,
    crop_seed_retention_rate: .08,
    crop_harvest_loss_rate: .055,
    crop_processing_loss_rate: .025,
    crop_storage_loss_rate_range: [.045, .07],
    county_route_neighbor_count: 6,
    county_boundary_policy: "UNKNOWN_POINT_PROXY_ONLY",
    prohibited_global_balance_multiplier: true,
  },
  product_taxonomy: productTaxonomy,
  crop_definitions: cropDefinitions,
  facility_references: facilityReferences.map(([industry_id, facility_reference_id, mapping_status]) => ({ industry_id, facility_reference_id, mapping_status })),
  method_registry: methodRegistry,
  evidence_registry: evidenceRegistry,
  source_manifest_reused: worldSources,
  counties, crops, livestock, forestry, fishery_gathering: fisheryGathering, minerals, salt, raw_materials: rawMaterials,
  processing_capacities: processingCapacities, storage, market_service: marketService, transport, local_demands: localDemands,
  product_balances: productBalances, processing_dependencies: processingDependencies, supply_routes: supplyRoutes, supply_relations: supplyRelations,
  regional_zones: regionalZones, scenario_states: scenarioStates, change_points: changePoints, runtime_mapping: runtimeMapping, unknowns,
  national_balance_184: nationalBalance, top_lists: topLists,
  summary: {
    county_count: counties.length,
    province_count: new Set(counties.map(x => x.province_id)).size,
    commandery_equivalent_count: new Set(counties.map(x => x.commandery_equivalent_id)).size,
    reconstructed_primary_count: counties.filter(x => x.primary_evidence_grade === "RECONSTRUCTED").length,
    modeled_primary_count: counties.filter(x => x.primary_evidence_grade === "MODELED").length,
    research_blocked_count: counties.filter(x => x.research_blocked).length,
    scenario_count: scenarioYears.length,
    scenario_state_count: scenarioStates.length,
    product_balance_count: productBalances.length,
    supply_route_count: supplyRoutes.length,
    supply_relation_count: supplyRelations.length,
    permanent_people_created: 0,
    facilities_created: 0,
    luoyang_initialization_modified: false,
  },
};

await Promise.all([dataRoot, mapRoot, validationRoot, outputRoot].map(dir => fs.mkdir(dir, { recursive: true })));
const masterPath = path.join(dataRoot, "county_economy_master_v1.json");
await fs.writeFile(masterPath, `${JSON.stringify(dataset, null, 2)}\n`, "utf8");
await fs.writeFile(path.join(dataRoot, "county_packs.ndjson"), `${counties.map(county => JSON.stringify({ ...county, crops: crops.filter(x => x.county_permanent_id === county.county_permanent_id), livestock: livestock.find(x => x.county_permanent_id === county.county_permanent_id), forestry: forestry.find(x => x.county_permanent_id === county.county_permanent_id), fishery_gathering: fisheryGathering.find(x => x.county_permanent_id === county.county_permanent_id), minerals: minerals.filter(x => x.county_permanent_id === county.county_permanent_id), salt: salt.find(x => x.county_permanent_id === county.county_permanent_id), processing: processingCapacities.filter(x => x.county_permanent_id === county.county_permanent_id), balances: productBalances.filter(x => x.county_permanent_id === county.county_permanent_id), supply_relations: supplyRelations.filter(x => x.origin_county_id === county.county_permanent_id || x.destination_county_id === county.county_permanent_id), unknowns: unknowns.filter(x => x.county_permanent_id === county.county_permanent_id) })).join("\n")}\n`, "utf8");
await fs.writeFile(path.join(dataRoot, "county_pack_index.json"), `${JSON.stringify({ schema: "mandate.county-economy-pack-index.v1", count: counties.length, records: counties.map((x, index) => ({ ordinal: index, county_permanent_id: x.county_permanent_id, county_name: x.county_name, province_id: x.province_id, primary_evidence_grade: x.primary_evidence_grade })) }, null, 2)}\n`, "utf8");

const mapFeatures = counties.map(x => ({ type: "Feature", properties: { county_permanent_id: x.county_permanent_id, county_name: x.county_name, province_id: x.province_id, population_184: x.population_184, cultivated_land_ha: x.current_cultivated_land_ha, fertility_bp: x.average_fertility_basis_points, water_access_bp: x.water_access_basis_points, food_net_balance: sum(productBalances.filter(b => b.county_permanent_id === x.county_permanent_id && ["GRAIN", "OTHER_FOOD"].includes(b.product_category)), b => b.net_balance_before_trade), food_import_dependency: round(sum(productBalances.filter(b => b.county_permanent_id === x.county_permanent_id && ["GRAIN", "OTHER_FOOD"].includes(b.product_category)), b => b.import) / Math.max(1, sum(localDemands.filter(d => d.county_permanent_id === x.county_permanent_id && ["GRAIN", "OTHER_FOOD"].includes(d.product_category)), d => d.total_demand)), 4), horse_output: productBalances.find(b => b.county_permanent_id === x.county_permanent_id && b.product_category === "HORSE").production, salt_output: productBalances.find(b => b.county_permanent_id === x.county_permanent_id && b.product_category === "SALT").production, iron_ore_output: productBalances.find(b => b.county_permanent_id === x.county_permanent_id && b.product_category === "IRON_ORE").production, timber_output: productBalances.find(b => b.county_permanent_id === x.county_permanent_id && b.product_category === "TIMBER").production, market_capacity: x.market_capacity_reference, transport_capacity: x.transport_capacity_tonne_km_reference, evidence_grade: x.primary_evidence_grade, geometry_status: x.gis_geometry_status }, geometry: { type: "Point", coordinates: [x.longitude, x.latitude] } }));
await fs.writeFile(path.join(mapRoot, "county_economy_184.geojson"), `${JSON.stringify({ type: "FeatureCollection", name: "Han 184 County Economy Reference", features: mapFeatures }, null, 2)}\n`, "utf8");
const corridorFeatures = supplyRelations.map(x => { const a = countyById.get(x.origin_county_id); const b = countyById.get(x.destination_county_id); return { type: "Feature", properties: { ...x }, geometry: { type: "LineString", coordinates: [[a.longitude, a.latitude], [b.longitude, b.latitude]] } }; });
await fs.writeFile(path.join(mapRoot, "supply_corridors_184.geojson"), `${JSON.stringify({ type: "FeatureCollection", name: "Han 184 Modeled Supply Corridors", features: corridorFeatures }, null, 2)}\n`, "utf8");
const layerManifest = { schema: "mandate.han184.economy-map-layer-manifest.v1", geometry_warning: "County polygons are unavailable; all county layers use county-seat points. Corridor lines connect origin/destination for analysis and do not claim exact ancient route geometry.", county_file: "county_economy_184.geojson", corridor_file: "supply_corridors_184.geojson", layers: ["Population Density", "Agricultural Potential", "Cultivated Land", "Crop Distribution", "Food Production", "Food Surplus", "Food Import Dependency", "Livestock", "Horse Production", "Forest/Timber", "Fuel", "Salt", "Iron Ore", "Other Mineral", "Metallurgy", "Textile", "Silk", "Pottery/Building Materials", "Shipbuilding", "Military Production", "Storage", "Market", "Transport", "Supply Corridors"] };
await fs.writeFile(path.join(mapRoot, "map_layer_manifest.json"), `${JSON.stringify(layerManifest, null, 2)}\n`, "utf8");
await fs.writeFile(path.join(outputRoot, "generation_summary.json"), `${JSON.stringify({ task_id: taskId, master_path: path.relative(repo, masterPath).replaceAll("\\", "/"), master_sha256: await sha256(masterPath), summary: dataset.summary, national_balance_184: nationalBalance }, null, 2)}\n`, "utf8");
console.log(`RESULT county-economy-build status=passed counties=${counties.length} balances=${productBalances.length} routes=${supplyRoutes.length} relations=${supplyRelations.length} scenarios=${scenarioStates.length}`);
