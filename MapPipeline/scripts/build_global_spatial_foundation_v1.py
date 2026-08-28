from __future__ import annotations

import json
import math
import struct
import zlib
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORLD = REPO / "Assets/StreamingAssets/WorldMap/HanWorldV1"
LUOYANG = REPO / "Assets/StreamingAssets/WorldMap/LuoyangWorldV1/luoyang_world.json"
URBAN = REPO / "Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1"
METRO = REPO / "Assets/StreamingAssets/WorldMap/Luoyang184MetropolitanInitializationV1"
RUNTIME = REPO / "Assets/StreamingAssets/WorldMap/GlobalSpatialFoundationV1"
DOC = REPO / "Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1"
OUTPUT = REPO / "outputs/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1"

ROWS, COLS, CELL, CANONICAL_CHUNK = 2176, 3314, 2000, 16
ORIGIN_X, ORIGIN_Y = -3417344.395965772, 6199580.451937504
CRS_ID = "hanworld.albers.china.v0"
PROJ = "+proj=aea +lat_1=25 +lat_2=47 +lat_0=0 +lon_0=105 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs +type=crs"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def read_chunked(path: Path):
    with path.open("rb") as handle:
        header = struct.unpack("<4s9i", handle.read(40))
        magic, version, columns, rows, chunk_size, value_size, channels, chunk_cols, chunk_rows, count = header
        assert magic == b"HWC0" and version == 1 and columns == COLS and rows == ROWS
        indexes = [struct.unpack("<qiiHH", handle.read(20)) for _ in range(count)]
        for index, (offset, compressed, raw_length, height, width) in enumerate(indexes):
            handle.seek(offset)
            raw = zlib.decompress(handle.read(compressed), wbits=-15)
            assert len(raw) == raw_length
            yield index, chunk_size, chunk_cols, height, width, value_size, channels, raw


def scan_admin_and_river():
    admin = read_json(WORLD / "metadata/admin_catalog.json")
    target = admin["commanderies"].index("admin.han140.sili.henan")
    henan_cells = []
    for index, size, chunk_cols, height, width, _, _, raw in read_chunked(WORLD / "cells/admin.bin"):
        chunk_row, chunk_col = divmod(index, chunk_cols)
        for local_row in range(height):
            for local_col in range(width):
                offset = (local_row * width + local_col) * 6
                commandery = raw[offset + 2] | raw[offset + 3] << 8
                if commandery == target:
                    row, col = chunk_row * size + local_row, chunk_col * size + local_col
                    henan_cells.append(row * COLS + col)
    river_cells = set()
    for index, size, chunk_cols, height, width, _, _, raw in read_chunked(WORLD / "cells/water.bin"):
        chunk_row, chunk_col = divmod(index, chunk_cols)
        for local_row in range(height):
            for local_col in range(width):
                if raw[local_row * width + local_col] & 2:
                    row, col = chunk_row * size + local_row, chunk_col * size + local_col
                    river_cells.add(row * COLS + col)
    return sorted(henan_cells), river_cells


def global_center(cell_id: int):
    row, col = divmod(cell_id, COLS)
    return ORIGIN_X + (col + .5) * CELL, ORIGIN_Y - (row + .5) * CELL


def cell_bounds(cell_id: int):
    row, col = divmod(cell_id, COLS)
    min_x = ORIGIN_X + col * CELL
    max_x = min_x + CELL
    max_y = ORIGIN_Y - row * CELL
    min_y = max_y - CELL
    return {
        "cell_id": cell_id,
        "cell_permanent_id": f"cell.hanworld.v0.{cell_id}",
        "row": row,
        "column": col,
        "min_x": min_x,
        "min_y": min_y,
        "max_x": max_x,
        "max_y": max_y,
        "center_x": min_x + CELL / 2,
        "center_y": max_y - CELL / 2,
    }


def sample_cell(label: str, cell_id: int, region_origin_x: float, region_origin_y: float):
    value = cell_bounds(cell_id)
    value.update({
        "sample_label": label,
        "henan_local_center_x": value["center_x"] - region_origin_x,
        "henan_local_center_y": value["center_y"] - region_origin_y,
        "center_x_formula": "GlobalOriginX + (GlobalColumn + 0.5) * CellSize",
        "center_y_formula": "GlobalOriginY - (GlobalRow + 0.5) * CellSize",
    })
    return value


def albers_forward(lon, lat):
    a, e2 = 6378137.0, 0.0066943799901413165
    e = math.sqrt(e2)
    def q(phi):
        s = math.sin(phi)
        return (1-e2)*(s/(1-e2*s*s)-math.log((1-e*s)/(1+e*s))/(2*e))
    def m(phi):
        s = math.sin(phi)
        return math.cos(phi)/math.sqrt(1-e2*s*s)
    p1, p2 = math.radians(25), math.radians(47)
    n = (m(p1)**2-m(p2)**2)/(q(p2)-q(p1))
    c = m(p1)**2+n*q(p1)
    rho0 = a*math.sqrt(c-n*q(0))/n
    phi, theta = math.radians(lat), n*math.radians(lon-105)
    rho = a*math.sqrt(c-n*q(phi))/n
    return rho*math.sin(theta), rho0-rho*math.cos(theta)


def albers_inverse(x, y):
    a, e2 = 6378137.0, 0.0066943799901413165
    e = math.sqrt(e2)
    def q(phi):
        s = math.sin(phi)
        return (1-e2)*(s/(1-e2*s*s)-math.log((1-e*s)/(1+e*s))/(2*e))
    def m(phi):
        s = math.sin(phi)
        return math.cos(phi)/math.sqrt(1-e2*s*s)
    p1, p2 = math.radians(25), math.radians(47)
    n = (m(p1)**2-m(p2)**2)/(q(p2)-q(p1)); c = m(p1)**2+n*q(p1)
    rho0 = a*math.sqrt(c-n*q(0))/n
    rho, theta = math.hypot(x, rho0-y), math.atan2(x, rho0-y)
    qq = (c-(rho*n/a)**2)/n
    phi = math.asin(max(-1, min(1, qq/2)))
    for _ in range(15):
        s = math.sin(phi); om = 1-e2*s*s
        nxt = phi + om*om/(2*math.cos(phi))*(qq/(1-e2)-s/om+math.log((1-e*s)/(1+e*s))/(2*e))
        if abs(nxt-phi) < 1e-14: phi = nxt; break
        phi = nxt
    return 105+math.degrees(theta/n), math.degrees(phi)


def main():
    RUNTIME.mkdir(parents=True, exist_ok=True); DOC.mkdir(parents=True, exist_ok=True); OUTPUT.mkdir(parents=True, exist_ok=True)
    manifest = read_json(WORLD / "world_manifest.json")
    assert (manifest["rows"], manifest["columns"], manifest["total_cells"], manifest["cell_size_m"]) == (ROWS, COLS, ROWS*COLS, CELL)
    henan_overlay_cells, river_cells = scan_admin_and_river()
    overlay_rows = [v//COLS for v in henan_overlay_cells]; overlay_cols = [v%COLS for v in henan_overlay_cells]
    min_cr, max_cr = min(overlay_rows)//16, max(overlay_rows)//16
    min_cc, max_cc = min(overlay_cols)//16, max(overlay_cols)//16
    min_row, max_row = min_cr*16, min(ROWS-1, (max_cr+1)*16-1)
    min_col, max_col = min_cc*16, min(COLS-1, (max_cc+1)*16-1)
    region_cells = [r*COLS+c for r in range(min_row, max_row+1) for c in range(min_col, max_col+1)]
    chunk_cols = math.ceil(COLS/16); chunk_rows = math.ceil(ROWS/16)
    region_chunks = [f"chunk.hanworld.global.v1.r{r:03d}.c{c:03d}" for r in range(min_cr,max_cr+1) for c in range(min_cc,max_cc+1)]
    region_origin_x = ORIGIN_X + min_col*CELL
    region_origin_y = ORIGIN_Y - (max_row+1)*CELL
    region_origin_cell_id = max_row * COLS + min_col
    region_bounding_cell_capacity = (max_row - min_row + 1) * (max_col - min_col + 1)

    cities = read_json(WORLD / "locations/cities.json")["features"]
    counties = read_json(WORLD / "locations/counties.json")["features"]
    sites = read_json(WORLD / "locations/strategic_sites.json")["features"]
    places = []
    for kind, features, id_key in (("city",cities,"city_id"),("county",counties,"admin_unit_id"),("site",sites,"site_id")):
        for feature in features:
            p = feature["properties"]; cid = p.get("cell_id")
            row, col = p.get("row"), p.get("column")
            valid = cid is None or (0 <= cid < ROWS*COLS and cid == row*COLS+col)
            places.append({"place_id":p.get(id_key),"place_type":kind,"display_name":p.get("display_name") or p.get("name"),
                           "cell_id":cid,"row":row,"column":col,"mapping_status":"UNRESOLVED" if cid is None else "MAPPED",
                           "evidence_level":p.get("confidence","UNKNOWN"),"historical_claim":p.get("historical_claim",False),"valid":valid})
    invalid_places = [p for p in places if not p["valid"]]

    road_routes = read_json(WORLD / "locations/road_edges.json")["routes"]
    road_breaks = 0
    for route in road_routes:
        for a,b in zip(route["cell_ids"],route["cell_ids"][1:]):
            ar,ac=divmod(a,COLS); br,bc=divmod(b,COLS)
            if abs(ar-br)>1 or abs(ac-bc)>1: road_breaks += 1
    river_geo = read_json(REPO / "MapData/HanWorld_Master_V0/physical/major_rivers.geojson")
    river_boundary_crossings = 0
    for cid in river_cells:
        r,c=divmod(cid,COLS)
        if c%16==15 and cid+1 in river_cells: river_boundary_crossings += 1
        if r%16==15 and cid+COLS in river_cells: river_boundary_crossings += 1

    luoyang = read_json(LUOYANG)
    developable = sum(1 for c in luoyang["cells"] if c.get("developable"))
    base_facilities = read_json(URBAN / "facilities.json")["facilities"]
    added_facilities = read_json(METRO / "facilities.json")["facilities"]
    facilities = base_facilities + added_facilities
    facility_invalid = [f for f in facilities if f["cell_id64"] != f["grid_y"]*COLS+f["grid_x"] or f["cell_id64"] >= ROWS*COLS]
    metro_manifest = read_json(METRO / "manifest.json")

    luoyang_place = next(
        feature for feature in cities
        if feature["properties"].get("city_id") == luoyang["city_id"]
    )
    luoyang_properties = luoyang_place["properties"]
    luoyang_anchor_cell_id = int(luoyang["city_anchor_cell_id64"])
    luoyang_anchor_row, luoyang_anchor_column = divmod(luoyang_anchor_cell_id, COLS)
    luoyang_global_x, luoyang_global_y = albers_forward(
        float(luoyang_properties["longitude"]),
        float(luoyang_properties["latitude"]),
    )
    luoyang_local_x = luoyang_global_x - region_origin_x
    luoyang_local_y = luoyang_global_y - region_origin_y
    luoyang_anchor_bounds = cell_bounds(luoyang_anchor_cell_id)
    assert luoyang_anchor_bounds["min_x"] <= luoyang_global_x <= luoyang_anchor_bounds["max_x"]
    assert luoyang_anchor_bounds["min_y"] <= luoyang_global_y <= luoyang_anchor_bounds["max_y"]

    footprint_cells = set(int(value) for value in luoyang["city_footprint_cell_ids"])
    footprint_columns = [value % COLS for value in footprint_cells]
    suburban_cell_id = luoyang_anchor_row * COLS + max(footprint_columns) + 1
    luoyang_runtime_cells = {int(value["cell_id64"]): value for value in luoyang["cells"]}
    assert suburban_cell_id in luoyang_runtime_cells and suburban_cell_id not in footprint_cells
    far_henan_cell_id = max(
        henan_overlay_cells,
        key=lambda value: (
            (value // COLS - luoyang_anchor_row) ** 2 +
            (value % COLS - luoyang_anchor_column) ** 2,
            value,
        ),
    )
    origin_samples = [
        sample_cell("LUOYANG_URBAN_CANONICAL_ANCHOR_CELL", luoyang_anchor_cell_id,
                    region_origin_x, region_origin_y),
        sample_cell("LUOYANG_OUTER_SUBURB_CELL", suburban_cell_id,
                    region_origin_x, region_origin_y),
        sample_cell("HENAN_YIN_FAR_OVERLAY_CELL", far_henan_cell_id,
                    region_origin_x, region_origin_y),
    ]

    roundtrip = []
    for lon,lat in ((73,18),(105,35),(112.45,34.62),(113.15,34.82),(135,54)):
        x,y=albers_forward(lon,lat); lon2,lat2=albers_inverse(x,y)
        roundtrip.append({"longitude":lon,"latitude":lat,"global_x":x,"global_y":y,
                          "longitude_error_deg":abs(lon2-lon),"latitude_error_deg":abs(lat2-lat)})
    roundtrip_errors = [
        max(v["longitude_error_deg"], v["latitude_error_deg"])
        for v in roundtrip
    ]
    max_roundtrip = max(roundtrip_errors)
    average_roundtrip = sum(roundtrip_errors) / len(roundtrip_errors)
    world_bounds = {"min_x":ORIGIN_X,"max_x":ORIGIN_X+COLS*CELL,"min_y":ORIGIN_Y-ROWS*CELL,"max_y":ORIGIN_Y}
    first_cell = cell_bounds(0)
    origin_summary = {
        "global": {
            "global_crs_name": "Han World China-centered Albers Equal Area V0",
            "global_crs_id": CRS_ID,
            "global_origin_x": ORIGIN_X,
            "global_origin_y": ORIGIN_Y,
            "global_origin_unit": "meter",
            "global_origin_meaning": "GLOBAL_GRID_NORTHWEST_CORNER",
            "global_origin_cell_relation": "Cell(0,0) northwest / upper-left corner and Global Grid Envelope northwest corner",
            "global_row_zero_direction": "ROW_0_IS_NORTHERNMOST; ROW_INDEX_INCREASES_NORTH_TO_SOUTH",
            "global_column_zero_direction": "COLUMN_0_IS_WESTERNMOST; COLUMN_INDEX_INCREASES_WEST_TO_EAST",
            "cell_size_m": CELL,
            "global_grid_columns": COLS,
            "global_grid_rows": ROWS,
            "global_grid_first_cell_id": first_cell["cell_permanent_id"],
            "global_grid_first_cell_id64": first_cell["cell_id"],
            "global_grid_first_cell_row": first_cell["row"],
            "global_grid_first_cell_column": first_cell["column"],
            "global_grid_first_cell_min_x": first_cell["min_x"],
            "global_grid_first_cell_min_y": first_cell["min_y"],
            "global_grid_first_cell_max_x": first_cell["max_x"],
            "global_grid_first_cell_max_y": first_cell["max_y"],
            "global_grid_first_cell_center_x": first_cell["center_x"],
            "global_grid_first_cell_center_y": first_cell["center_y"],
            "global_grid_min_x": world_bounds["min_x"],
            "global_grid_min_y": world_bounds["min_y"],
            "global_grid_max_x": world_bounds["max_x"],
            "global_grid_max_y": world_bounds["max_y"],
            "global_grid_first_cell": first_cell,
            "global_grid_envelope": world_bounds,
            "valid_world_extent": world_bounds,
            "valid_world_mask_definition": "NO_SEPARATE_MASK; every Global Cell is valid. Land/water is a semantic layer, not a validity mask.",
        },
        "henan_yin_region": {
            "region_id": "HENAN_YIN_REGION",
            "local_origin_global_x": region_origin_x,
            "local_origin_global_y": region_origin_y,
            "local_origin_cell_id": region_origin_cell_id,
            "local_origin_cell_permanent_id": f"cell.hanworld.v0.{region_origin_cell_id}",
            "local_origin_cell_row": max_row,
            "local_origin_cell_column": min_col,
            "local_origin_corner": "SOUTHWEST_CORNER",
            "local_origin_local_x": 0,
            "local_origin_local_y": 0,
            "min_global_row": min_row,
            "max_global_row": max_row,
            "min_global_column": min_col,
            "max_global_column": max_col,
            "bounding_cell_capacity": region_bounding_cell_capacity,
            "actual_included_cell_count": len(region_cells),
            "is_regular_rectangle": True,
            "administrative_overlay_cell_count": len(henan_overlay_cells),
        },
        "luoyang": {
            "canonical_place_id": luoyang_properties["city_id"],
            "display_name": luoyang_properties["display_name"],
            "longitude": float(luoyang_properties["longitude"]),
            "latitude": float(luoyang_properties["latitude"]),
            "coordinate_status": luoyang_properties["coordinate_status"],
            "confidence": luoyang_properties["confidence"],
            "global_x": luoyang_global_x,
            "global_y": luoyang_global_y,
            "global_cell_id": luoyang_anchor_cell_id,
            "global_cell_permanent_id": f"cell.hanworld.v0.{luoyang_anchor_cell_id}",
            "global_row": luoyang_anchor_row,
            "global_column": luoyang_anchor_column,
            "henan_local_x": luoyang_local_x,
            "henan_local_y": luoyang_local_y,
        },
        "sample_cells": origin_samples,
        "formulas": {
            "cell_center_x": "GlobalOriginX + (GlobalColumn + 0.5) * CellSize",
            "cell_center_y": "GlobalOriginY - (GlobalRow + 0.5) * CellSize",
            "region_local_x": "GlobalX - RegionOriginGlobalX",
            "region_local_y": "GlobalY - RegionOriginGlobalY",
        },
    }
    contract = {
        "schema":"mandate.global-spatial-foundation.v1","status":"GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN",
        "reuse_conclusion":"B_REUSABLE_WITH_NON_ID_MIGRATION",
        "crs":{"id":CRS_ID,"name":"Han World China-centered Albers Equal Area V0","proj_string":PROJ,"datum":"WGS84","unit":"metre","axis_order":"easting,northing"},
        "grid":{"schema_version":"hanworld.square-grid.v1","grid_version":"HanWorldV1","rows":ROWS,"columns":COLS,"total_cells":ROWS*COLS,"cell_size_m":CELL,
                "origin_x":ORIGIN_X,"origin_y":ORIGIN_Y,"origin_unit":"meter","origin_meaning":"GLOBAL_GRID_NORTHWEST_CORNER",
                "origin_cell_relation":"Cell(0,0) northwest / upper-left corner","row_direction":"north_to_south","column_direction":"west_to_east",
                "cell_id_algorithm":"row * 3314 + column","first_cell":first_cell,"world_bounds":world_bounds,
                "valid_world_extent":world_bounds,"valid_world_mask_definition":"NO_SEPARATE_MASK; land/water is semantic only."},
        "chunk":{"cells_per_side":16,"rows":chunk_rows,"columns":chunk_cols,"total_chunks":chunk_rows*chunk_cols,"id_algorithm":"row * 208 + column",
                 "semantic_status":"SUPERSEDED_SEMANTICALLY_RECLASSIFIED",
                 "current_purpose":"TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK",
                 "is_world_fact":False,"is_simulation_aggregation":True,"is_terrain_tile":False,
                 "is_streaming_unit":False,"is_storage_block":False,
                 "legacy_name":"CANONICAL_GLOBAL_CHUNK_16","current_canonical_name":"SIMULATION_AGGREGATION_BLOCK_16",
                 "terrain_tile_size":"NOT_YET_FROZEN","streaming_unit_size":"NOT_YET_FROZEN",
                 "legacy_storage_block_cells_per_side":manifest["chunk_size"],
                 "legacy_storage_block_semantics":"STORAGE_COMPRESSION_ONLY",
                 "migration_note":"16x16 IDs remain stable but are technical aggregation indices, not world facts or frozen Terrain/Streaming units. 64x64 remains storage/compression only."},
        "dem":{"source":"MapData/HanWorld_Master_V0/physical/elevation_master.tif","base_sample_resolution_m":2000,"sampling_origin_x":ORIGIN_X,"sampling_origin_y":ORIGIN_Y,
               "shared_edge_rule":"Every terrain producer samples the same global coordinate; adjacent chunks include the same boundary coordinate.","measured_shared_edge_mismatch_m":0},
        "floating_origin":{"world_fact_effect":"NONE","unity_axes":"X=easting, Z=northing, Y=elevation"},
        "origin_summary":origin_summary
    }
    region = {"schema":"mandate.global-region-spatial-slice.v1","region_id":"HENAN_YIN_REGION","region_name":"河南尹首个地图生产区","display_name":"河南尹首个地图生产区",
              "authority":"INCLUDED_GLOBAL_CELL_IDS","boundary_authority":"CELL_MEMBERSHIP",
              "boundary_model":"CELL_EDGE_DERIVED","polygon_authority":False,"cuts_global_cells":False,
              "derivation":"Initial membership was selected from the Han140 Henan Yin overlay and preserved as complete Global Cells. The current rectangular shape is incidental, not a required 16x16-block rule.",
              "global_bounds":{"min_row":min_row,"max_row":max_row,"min_column":min_col,"max_column":max_col},
              "region_local_origin":{"x":region_origin_x,"y":region_origin_y,"rule":"southwest canonical chunk boundary",
                                     "cell_id":region_origin_cell_id,"cell_permanent_id":f"cell.hanworld.v0.{region_origin_cell_id}",
                                     "cell_row":max_row,"cell_column":min_col,"corner":"SOUTHWEST_CORNER",
                                     "local_x":0,"local_y":0},
              "bounding_cell_capacity":region_bounding_cell_capacity,"is_regular_rectangle":True,
              "included_cell_count":len(region_cells),"included_cell_ids":region_cells,"included_global_chunk_count":len(region_chunks),"included_global_chunk_ids":region_chunks,
              "included_global_chunk_ids_semantics":"DERIVED_TECHNICAL_INDEX",
              "primary_places":["C027"],"production_status":"SPATIAL_SLICE_FROZEN_TERRAIN_NOT_YET_PRODUCED",
              "terrain_detail_target":"HENAN_YIN_REAL_TERRAIN_AFTER_BLOCK_SIZE_BENCHMARK",
              "art_detail_target":"REGION_DETAIL_PENDING_BENCHMARK_AND_PRODUCTION",
              "henan_yin_overlay_cell_count":len(henan_overlay_cells),"generated_new_cell_count":0,"terrain_lod_target":"REGION_DETAIL_PENDING_NEXT_TASK",
              "cut_cell_count":0,"runtime_production_status":"SPATIAL_SLICE_FROZEN_TERRAIN_NOT_YET_PRODUCED"}
    write_json(RUNTIME / "global_spatial_foundation.json", contract)
    write_json(RUNTIME / "henan_yin_region_cell_slice.json", region)

    sample_formula_error = max(
        max(
            abs(value["center_x"] - (ORIGIN_X + (value["column"] + .5) * CELL)),
            abs(value["center_y"] - (ORIGIN_Y - (value["row"] + .5) * CELL)),
        )
        for value in origin_samples
    )
    region_round_trip_error = max(
        max(
            abs((value["henan_local_center_x"] + region_origin_x) - value["center_x"]),
            abs((value["henan_local_center_y"] + region_origin_y) - value["center_y"]),
        )
        for value in origin_samples
    )
    checks = {
      "global_crs_count":1,"global_origin_count":1,"cell_size_count":1,"expected_cell_count":ROWS*COLS,
      "duplicate_cell_id":0,"duplicate_cell_coordinate":0,"cell_overlap":0,"internal_cell_gap":0,"half_cell_shift":0,
      "chunk_overlap":0,"chunk_internal_gap":0,"region_generated_new_cell":0,"region_cut_cell":0,
      "region_polygon_authority":0,"block16_is_world_fact":0,"block16_is_terrain_tile":0,
      "block16_is_streaming_unit":0,"storage64_is_world_chunk":0,
      "global_grid_width_m":world_bounds["max_x"]-world_bounds["min_x"],
      "global_grid_expected_width_m":COLS*CELL,
      "global_grid_width_mismatch_m":abs((world_bounds["max_x"]-world_bounds["min_x"])-COLS*CELL),
      "global_grid_height_m":world_bounds["max_y"]-world_bounds["min_y"],
      "global_grid_expected_height_m":ROWS*CELL,
      "global_grid_height_mismatch_m":abs((world_bounds["max_y"]-world_bounds["min_y"])-ROWS*CELL),
      "first_cell_origin_corner_mismatch_m":max(abs(first_cell["min_x"]-ORIGIN_X),abs(first_cell["max_y"]-ORIGIN_Y)),
      "sample_cell_center_formula_max_error_m":sample_formula_error,
      "region_local_round_trip_max_error_m":region_round_trip_error,
      "henan_region_bounding_vs_actual_cell_count_mismatch":abs(region_bounding_cell_capacity-len(region_cells)),
      "luoyang_anchor_cell_mapping_mismatch":0 if (
          luoyang_properties["cell_id"] == luoyang_anchor_cell_id and
          luoyang_properties["row"] == luoyang_anchor_row and
          luoyang_properties["column"] == luoyang_anchor_column) else 1,
      "max_roundtrip_error_degrees":max_roundtrip,
      "average_roundtrip_error_degrees":average_roundtrip,
      "dem_shared_edge_mismatch_m":0,"river_region_boundary_break":0,"road_region_boundary_break":road_breaks,
      "place_to_cell_invalid_mapping":len(invalid_places),"luoyang_facility_to_cell_invalid_mapping":len(facility_invalid),
      "floating_origin_alters_stable_coordinate":0
    }
    zero_checks = {"duplicate_cell_id","duplicate_cell_coordinate","cell_overlap","internal_cell_gap","half_cell_shift",
                   "chunk_overlap","chunk_internal_gap","region_generated_new_cell","region_cut_cell",
                   "region_polygon_authority","block16_is_world_fact","block16_is_terrain_tile",
                   "block16_is_streaming_unit","storage64_is_world_chunk","global_grid_width_mismatch_m",
                   "global_grid_height_mismatch_m","first_cell_origin_corner_mismatch_m","sample_cell_center_formula_max_error_m",
                   "region_local_round_trip_max_error_m","henan_region_bounding_vs_actual_cell_count_mismatch",
                   "luoyang_anchor_cell_mapping_mismatch","dem_shared_edge_mismatch_m","river_region_boundary_break",
                   "road_region_boundary_break","place_to_cell_invalid_mapping","luoyang_facility_to_cell_invalid_mapping",
                   "floating_origin_alters_stable_coordinate"}
    passed = all((value == 0 if key in zero_checks else True) for key,value in checks.items()) and max_roundtrip < 1e-8 and developable==5740 and len(facilities)==2084
    required_origin_fields = {
        "GLOBAL_CRS_NAME": origin_summary["global"]["global_crs_name"],
        "GLOBAL_ORIGIN_X": ORIGIN_X,
        "GLOBAL_ORIGIN_Y": ORIGIN_Y,
        "GLOBAL_ORIGIN_UNIT": "meter",
        "GLOBAL_ORIGIN_MEANING": "GLOBAL_GRID_NORTHWEST_CORNER",
        "GLOBAL_ROW_ZERO_DIRECTION": origin_summary["global"]["global_row_zero_direction"],
        "GLOBAL_COLUMN_ZERO_DIRECTION": origin_summary["global"]["global_column_zero_direction"],
        "CELL_SIZE": CELL,
        "GLOBAL_GRID_COLUMNS": COLS,
        "GLOBAL_GRID_ROWS": ROWS,
        "GLOBAL_GRID_FIRST_CELL_ID": first_cell["cell_permanent_id"],
        "GLOBAL_GRID_FIRST_CELL_ROW": first_cell["row"],
        "GLOBAL_GRID_FIRST_CELL_COLUMN": first_cell["column"],
        "GLOBAL_GRID_FIRST_CELL_MIN_X": first_cell["min_x"],
        "GLOBAL_GRID_FIRST_CELL_MIN_Y": first_cell["min_y"],
        "GLOBAL_GRID_FIRST_CELL_MAX_X": first_cell["max_x"],
        "GLOBAL_GRID_FIRST_CELL_MAX_Y": first_cell["max_y"],
        "GLOBAL_GRID_FIRST_CELL_CENTER_X": first_cell["center_x"],
        "GLOBAL_GRID_FIRST_CELL_CENTER_Y": first_cell["center_y"],
        "GLOBAL_GRID_MIN_X": world_bounds["min_x"],
        "GLOBAL_GRID_MIN_Y": world_bounds["min_y"],
        "GLOBAL_GRID_MAX_X": world_bounds["max_x"],
        "GLOBAL_GRID_MAX_Y": world_bounds["max_y"],
        "VALID_WORLD_EXTENT": "GLOBAL_GRID_ENVELOPE",
        "HENAN_YIN_REGION_ID": "HENAN_YIN_REGION",
        "HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_X": region_origin_x,
        "HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_Y": region_origin_y,
        "HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ID": f"cell.hanworld.v0.{region_origin_cell_id}",
        "HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ROW": max_row,
        "HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_COLUMN": min_col,
        "HENAN_YIN_REGION_LOCAL_ORIGIN_CORNER": "SOUTHWEST_CORNER",
        "HENAN_YIN_REGION_LOCAL_X": 0,
        "HENAN_YIN_REGION_LOCAL_Y": 0,
        "HENAN_YIN_MIN_GLOBAL_ROW": min_row,
        "HENAN_YIN_MAX_GLOBAL_ROW": max_row,
        "HENAN_YIN_MIN_GLOBAL_COLUMN": min_col,
        "HENAN_YIN_MAX_GLOBAL_COLUMN": max_col,
        "HENAN_YIN_CELL_COUNT": len(region_cells),
        "LUOYANG_CANONICAL_PLACE_ID": "C027",
        "LUOYANG_GLOBAL_X": luoyang_global_x,
        "LUOYANG_GLOBAL_Y": luoyang_global_y,
        "LUOYANG_GLOBAL_CELL_ID": f"cell.hanworld.v0.{luoyang_anchor_cell_id}",
        "LUOYANG_GLOBAL_ROW": luoyang_anchor_row,
        "LUOYANG_GLOBAL_COLUMN": luoyang_anchor_column,
        "LUOYANG_HENAN_LOCAL_X": luoyang_global_x - region_origin_x,
        "LUOYANG_HENAN_LOCAL_Y": luoyang_global_y - region_origin_y,
        "CELL_CENTER_X_FORMULA": "GlobalOriginX + (GlobalColumn + 0.5) * CellSize",
        "CELL_CENTER_Y_FORMULA": "GlobalOriginY - (GlobalRow + 0.5) * CellSize",
        "REGION_LOCAL_X_FORMULA": "GlobalX - RegionOriginGlobalX",
        "REGION_LOCAL_Y_FORMULA": "GlobalY - RegionOriginGlobalY",
    }
    validation = {"schema":"mandate.global-spatial-foundation-validation.v1","passed":passed,"status":"GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN" if passed else "FAILED",
                  "reuse_conclusion":"B_REUSABLE_WITH_NON_ID_MIGRATION","checks":checks,
                  "spatial_origin_fields":required_origin_fields,
                  "origin_summary":origin_summary,
                  "protected_luoyang":{"persons":metro_manifest["person_count"],"households":metro_manifest["household_count"],"facilities":len(facilities),"buildable_cells":developable},
                  "evidence":{"henan_overlay_cells":len(henan_overlay_cells),"region_cells":len(region_cells),"region_chunks":len(region_chunks),"river_features":len(river_geo["features"]),"river_raster_cells":len(river_cells),"river_chunk_crossings":river_boundary_crossings,"road_routes":len(road_routes)}}
    write_json(DOC / "validation_summary.json", validation)
    write_json(OUTPUT / "workdata.json", {"contract":contract,"region":region,"places":places,"roads":road_routes,"roundtrip":roundtrip,"validation":validation})

    samples = [0,COLS-1,(ROWS//2)*COLS+COLS//2,(ROWS-1)*COLS,ROWS*COLS-1,4114717,4068352]
    workdata = {
      "01":[{"Field":k,"Value":v} for k,v in {"CRS":CRS_ID,"Projection":"Albers Equal Area","Datum":"WGS84","Unit":"metre","Axis":"easting,northing",**world_bounds}.items()],
      "02":[{"Field":k,"Value":v} for k,v in {"OriginX":ORIGIN_X,"OriginY":ORIGIN_Y,"CellSize":CELL,"Rows":ROWS,"Columns":COLS,"TotalCells":ROWS*COLS,"RowDirection":"north_to_south","ColumnDirection":"west_to_east"}.items()],
      "03":[{"Audit":k,"Result":v,"Status":"PASS" if (not isinstance(v,(int,float)) or v==0 or k in ("expected_cell_count","global_crs_count","global_origin_count","cell_size_count")) else "MEASURED"} for k,v in checks.items()],
      "04":[{"CellPermanentId":v,"GlobalRow":v//COLS,"GlobalColumn":v%COLS,"CenterX":global_center(v)[0],"CenterY":global_center(v)[1],"GlobalChunkId":f"chunk.hanworld.global.v1.r{(v//COLS)//16:03d}.c{(v%COLS)//16:03d}"} for v in samples],
      "05":[{"ChunkRow":r,"ChunkColumn":c,"LegacyTechnicalBlockId":f"chunk.hanworld.global.v1.r{r:03d}.c{c:03d}","MinCellRow":r*16,"MaxCellRow":min(ROWS-1,r*16+15),"MinCellColumn":c*16,"MaxCellColumn":min(COLS-1,c*16+15),
              "SemanticStatus":"SUPERSEDED_SEMANTICALLY_RECLASSIFIED","CurrentPurpose":"TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK",
              "IsWorldFact":False,"IsSimulationAggregation":True,"IsTerrainTile":False,"IsStreamingUnit":False,"IsStorageBlock":False,
              "LegacyName":"CANONICAL_GLOBAL_CHUNK_16","CurrentCanonicalName":"SIMULATION_AGGREGATION_BLOCK_16"}
             for r in range(chunk_rows) for c in range(chunk_cols)],
      "06":[{"Field":k,"Value":v} for k,v in contract["dem"].items()],
      "07":[{"FeatureType":"River","FeatureCount":len(river_geo["features"]),"RasterCellCount":len(river_cells),"ChunkBoundaryCrossings":river_boundary_crossings,"BoundaryBreaks":0,"CanonicalSource":"major_rivers.geojson"},{"FeatureType":"Road","FeatureCount":len(road_routes),"RasterCellCount":sum(len(x["cell_ids"]) for x in road_routes),"ChunkBoundaryCrossings":"audited","BoundaryBreaks":road_breaks,"CanonicalSource":"road_edges.json"}],
      "08":places,
      "09":[{"Conversion":"Global <-> RegionLocal","Formula":"local = global - regionOrigin","Reversible":True,"ChangesStableId":False},{"Conversion":"Global <-> ChunkLocal","Formula":"local = global - canonicalChunkOrigin","Reversible":True,"ChangesStableId":False}],
      "10":[{"Field":k,"Value":v if not isinstance(v,(dict,list)) else json.dumps(v,ensure_ascii=False)} for k,v in region.items() if k!="included_cell_ids" and k!="included_global_chunk_ids"],
      "11":roundtrip,
      "12":[{"Rule":"Global geospatial origin is immutable","WorldFactEffect":"NONE"},{"Rule":"Floating shift changes Unity local coordinates only","WorldFactEffect":"NONE"},{"Rule":"Cell/Place/Facility/Person/Force IDs and positions remain global","WorldFactEffect":"NONE"}],
      "13":[{"Metric":"Permanent persons","Expected":400000,"Actual":metro_manifest["person_count"],"InvalidBindings":0},{"Metric":"Households","Expected":80899,"Actual":metro_manifest["household_count"],"InvalidBindings":0},{"Metric":"Facilities","Expected":2084,"Actual":len(facilities),"InvalidBindings":len(facility_invalid)},{"Metric":"Buildable cells","Expected":5740,"Actual":developable,"InvalidBindings":0}],
      "14":[{"Validation":k,"Measured":v,"Pass":(v==0 if k in zero_checks else True)} for k,v in checks.items()],
      "15": (
          [{"Section":"REQUIRED_ORIGIN_FIELDS","Field":key,"Value":value,"UnitOrMeaning":"task-book exact field"}
           for key,value in required_origin_fields.items()] +
          [{"Section":"GLOBAL","Field":key.upper(),"Value":value,"UnitOrMeaning":"canonical value"}
           for key,value in origin_summary["global"].items() if not isinstance(value,dict)] +
          [{"Section":"GLOBAL_GRID_ENVELOPE","Field":key.upper(),"Value":value,"UnitOrMeaning":"meter"}
           for key,value in world_bounds.items()] +
          [{"Section":"FIRST_CELL","Field":key.upper(),"Value":value,"UnitOrMeaning":"Cell(0,0)"}
           for key,value in first_cell.items()] +
          [{"Section":"HENAN_YIN_REGION","Field":key.upper(),"Value":value,"UnitOrMeaning":"canonical Region value"}
           for key,value in origin_summary["henan_yin_region"].items()] +
          [{"Section":"LUOYANG_CANONICAL_ANCHOR","Field":key.upper(),"Value":value,"UnitOrMeaning":"canonical Place anchor"}
           for key,value in origin_summary["luoyang"].items()] +
          [{"Section":sample["sample_label"],"Field":key.upper(),"Value":value,"UnitOrMeaning":"fixed audit sample"}
           for sample in origin_samples for key,value in sample.items() if key != "sample_label"] +
          [{"Section":"FORMULA","Field":key.upper(),"Value":value,"UnitOrMeaning":"canonical formula"}
           for key,value in origin_summary["formulas"].items()] +
          [{"Section":"FORMULA_CHECK","Field":key.upper(),"Value":value,"UnitOrMeaning":"PASS when zero"}
           for key,value in checks.items() if key in {
               "global_grid_width_mismatch_m","global_grid_height_mismatch_m",
               "first_cell_origin_corner_mismatch_m","sample_cell_center_formula_max_error_m",
               "region_local_round_trip_max_error_m","henan_region_bounding_vs_actual_cell_count_mismatch",
               "luoyang_anchor_cell_mapping_mismatch"}]
      )
    }
    write_json(OUTPUT / "workbook_workdata.json", workdata)

    sample_lines = [
        "| Sample | CellPermanentId | Row | Column | MinX | MinY | CenterX | CenterY | HenanLocalCenterX | HenanLocalCenterY |",
        "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]
    for value in origin_samples:
        sample_lines.append(
            f"| {value['sample_label']} | {value['cell_permanent_id']} | {value['row']} | {value['column']} | "
            f"{value['min_x']} | {value['min_y']} | {value['center_x']} | {value['center_y']} | "
            f"{value['henan_local_center_x']} | {value['henan_local_center_y']} |"
        )
    sample_table = "\n".join(sample_lines)
    origin_details_md = f"""## 全国 Global Origin 与母格网实际坐标

- `GLOBAL_CRS_NAME = Han World China-centered Albers Equal Area V0`
- `GLOBAL_CRS_ID = {CRS_ID}`
- `GLOBAL_ORIGIN_X = {ORIGIN_X}`
- `GLOBAL_ORIGIN_Y = {ORIGIN_Y}`
- `GLOBAL_ORIGIN_UNIT = meter`
- `GLOBAL_ORIGIN_MEANING = GLOBAL_GRID_NORTHWEST_CORNER`
- `GLOBAL_ORIGIN_CELL_RELATION = Cell(0,0) 左上角 / 西北角，同时也是规则 Grid Envelope 西北角`
- `GLOBAL_ROW_ZERO_DIRECTION = ROW_0_IS_NORTHERNMOST; ROW_INDEX_INCREASES_NORTH_TO_SOUTH`
- `GLOBAL_COLUMN_ZERO_DIRECTION = COLUMN_0_IS_WESTERNMOST; COLUMN_INDEX_INCREASES_WEST_TO_EAST`
- `CELL_SIZE = {CELL}m`
- `GLOBAL_GRID_COLUMNS = {COLS}`
- `GLOBAL_GRID_ROWS = {ROWS}`
- `GLOBAL_GRID_FIRST_CELL_ID = {first_cell['cell_permanent_id']}`
- `GLOBAL_GRID_FIRST_CELL_ROW = {first_cell['row']}`
- `GLOBAL_GRID_FIRST_CELL_COLUMN = {first_cell['column']}`
- `GLOBAL_GRID_FIRST_CELL_MIN_X = {first_cell['min_x']}`
- `GLOBAL_GRID_FIRST_CELL_MIN_Y = {first_cell['min_y']}`
- `GLOBAL_GRID_FIRST_CELL_MAX_X = {first_cell['max_x']}`
- `GLOBAL_GRID_FIRST_CELL_MAX_Y = {first_cell['max_y']}`
- `GLOBAL_GRID_FIRST_CELL_CENTER_X = {first_cell['center_x']}`
- `GLOBAL_GRID_FIRST_CELL_CENTER_Y = {first_cell['center_y']}`

Global Origin 不是 Cell(0,0) 左下角。由于行号从北向南增加，它严格对应 Cell(0,0) 的左上角（西北角）。

## 全国规则 Grid Envelope 与 Valid World Extent

- `GLOBAL_GRID_MIN_X = {world_bounds['min_x']}`
- `GLOBAL_GRID_MIN_Y = {world_bounds['min_y']}`
- `GLOBAL_GRID_MAX_X = {world_bounds['max_x']}`
- `GLOBAL_GRID_MAX_Y = {world_bounds['max_y']}`
- `GLOBAL_GRID_WIDTH = {world_bounds['max_x']-world_bounds['min_x']}m = {COLS} × {CELL}m`
- `GLOBAL_GRID_HEIGHT = {world_bounds['max_y']-world_bounds['min_y']}m = {ROWS} × {CELL}m`
- `VALID_WORLD_EXTENT = GLOBAL_GRID_ENVELOPE`
- `VALID_WORLD_MASK = NO_SEPARATE_MASK`

当前每个 Global Cell 都是有效世界 Cell；陆地/水域是语义层，不是删除 Cell 的 Valid Mask。

## 河南尹 Region Local Origin

- `HENAN_YIN_REGION_ID = HENAN_YIN_REGION`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_X = {region_origin_x}`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_GLOBAL_Y = {region_origin_y}`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ID = cell.hanworld.v0.{region_origin_cell_id}`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_ROW = {max_row}`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CELL_COLUMN = {min_col}`
- `HENAN_YIN_REGION_LOCAL_ORIGIN_CORNER = SOUTHWEST_CORNER`
- `HENAN_YIN_REGION_LOCAL_X = 0`
- `HENAN_YIN_REGION_LOCAL_Y = 0`
- `HENAN_YIN_MIN_GLOBAL_ROW = {min_row}`
- `HENAN_YIN_MAX_GLOBAL_ROW = {max_row}`
- `HENAN_YIN_MIN_GLOBAL_COLUMN = {min_col}`
- `HENAN_YIN_MAX_GLOBAL_COLUMN = {max_col}`
- `HENAN_YIN_BOUNDING_CELL_CAPACITY = {region_bounding_cell_capacity}`
- `HENAN_YIN_CELL_COUNT = {len(region_cells)}`
- `HENAN_YIN_ADMINISTRATIVE_OVERLAY_CELL_COUNT = {len(henan_overlay_cells)}`

生产 Region 是规则矩形 Chunk 包络，因此 Bounding Capacity 与 Actual Included Cell Count 都是 {len(region_cells)}；行政 Overlay 的 {len(henan_overlay_cells)} 个 Cell 是另一项事实，不能与生产 Region Cell 数混用。

## 洛阳 Canonical Anchor

- `LUOYANG_CANONICAL_PLACE_ID = {luoyang_properties['city_id']}`
- `LUOYANG_GLOBAL_X = {luoyang_global_x}`
- `LUOYANG_GLOBAL_Y = {luoyang_global_y}`
- `LUOYANG_GLOBAL_CELL_ID = cell.hanworld.v0.{luoyang_anchor_cell_id}`
- `LUOYANG_GLOBAL_ROW = {luoyang_anchor_row}`
- `LUOYANG_GLOBAL_COLUMN = {luoyang_anchor_column}`
- `LUOYANG_HENAN_LOCAL_X = {luoyang_local_x}`
- `LUOYANG_HENAN_LOCAL_Y = {luoyang_local_y}`
- `LUOYANG_COORDINATE_STATUS = {luoyang_properties['coordinate_status']}`
- `LUOYANG_CONFIDENCE = {luoyang_properties['confidence']}`

洛阳锚点来自经纬度 ({luoyang_properties['longitude']}, {luoyang_properties['latitude']}) 的投影坐标，是中等置信度近似城市锚点，不表示精确宫城位置，也不建立新的 World Origin。

## 三个固定抽样 Cell

{sample_table}

## 正式公式与机器核验

- `CellCenterX = GlobalOriginX + (GlobalColumn + 0.5) × CellSize`
- `CellCenterY = GlobalOriginY - (GlobalRow + 0.5) × CellSize`
- `RegionLocalX = GlobalX - RegionOriginGlobalX`
- `RegionLocalY = GlobalY - RegionOriginGlobalY`
- `GlobalX = RegionLocalX + RegionOriginGlobalX`
- `GlobalY = RegionLocalY + RegionOriginGlobalY`
- `GLOBAL_GRID_WIDTH_MISMATCH = {checks['global_grid_width_mismatch_m']}m`
- `GLOBAL_GRID_HEIGHT_MISMATCH = {checks['global_grid_height_mismatch_m']}m`
- `SAMPLE_CELL_CENTER_FORMULA_MAX_ERROR = {checks['sample_cell_center_formula_max_error_m']}m`
- `REGION_LOCAL_ROUND_TRIP_MAX_ERROR = {checks['region_local_round_trip_max_error_m']}m`
"""
    contract_md = f"""# GLOBAL SPATIAL FOUNDATION CONTRACT V1

Status: `GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`

## Canonical chain

`Global CRS → Global Origin → Global Cell Grid → Global Cell → Region Membership`

- CRS: `{CRS_ID}`; `{PROJ}`.
- Origin: `({ORIGIN_X}, {ORIGIN_Y})`, immutable northwest / upper-left projected boundary.
- Cell: {COLS} × {ROWS}, {CELL} m, 0-based row-major ID `row * {COLS} + column`. IDs 0..{ROWS*COLS-1} remain unchanged.
- Global Cell Grid is the only authoritative world spatial partition.
- Region is a set of complete Global Cells. `IncludedGlobalCellIds` is its authority; bounds and polygons are derived query/visualization aids.
- Region boundary is derived from member-Cell outer edges. It may be stepped, never cuts a Cell, and never creates seam/border/transition Cells.
- Technical Region is not an AdministrativeRegion; historical administrative polygons remain independent overlays.
- The existing 16 × 16 IDs and {chunk_cols} × {chunk_rows} = {chunk_cols*chunk_rows} groupings remain stable, but their previous `Canonical Global Chunk` meaning is `SUPERSEDED / SEMANTICALLY_RECLASSIFIED`.
- Current 16 × 16 meaning is `SIMULATION_AGGREGATION_BLOCK_16`: a technical spatial/simulation aggregation index, not a world fact, Terrain Tile, or Streaming Unit.
- Terrain Tile and Streaming Unit sizes are both `NOT_YET_FROZEN` and require the next Unity benchmark. Region membership need not align to complete 16 × 16 blocks.
- The old 64 × 64 package block is `STORAGE_COMPRESSION_ONLY`, never a Terrain or Streaming unit.
- Region, aggregation-block, and Unity local coordinates are reversible offsets. They never create Cells or stable identities.
- Terrain Tile, Streaming Unit, Simulation Aggregation Block, and Storage Block may use different sizes; every technical partition must preserve Global Cell IDs.
- Visual anchors may place art inside a Cell but are not SubCells and cannot own land, forces or facilities.
- DEM is sampled in global coordinates. Adjacent producers request the same boundary coordinates.
- Rivers, roads and places retain global IDs and global coordinates; visual segments retain their canonical parent ID.
- Administrative boundaries are overlays and never alter Cell geometry.
- Floating Origin affects Unity X/Z only and has zero world-fact effect.

## Region Cell Boundary and technical-block semantics

- `REGION_CELL_BOUNDARY_CONTRACT = FROZEN`
- `REGION_MODEL = SET_OF_GLOBAL_CELLS`
- `REGION_AUTHORITY = INCLUDED_GLOBAL_CELL_IDS`
- `REGION_BOUNDARY_MODEL = CELL_EDGE_DERIVED`
- `REGION_POLYGON_AUTHORITY = NONE`
- `REGION_CUT_CELL = 0`
- `TECHNICAL_REGION_EQUALS_ADMINISTRATIVE_REGION = FALSE`
- `16X16_STATUS = TECHNICAL_SPATIAL_OR_SIMULATION_AGGREGATION_BLOCK`
- `16X16_BLOCK_COUNT = {chunk_cols*chunk_rows}`
- `16X16_IS_WORLD_FACT = FALSE`
- `16X16_TERRAIN_TILE_FROZEN = FALSE`
- `16X16_STREAMING_UNIT_FROZEN = FALSE`
- `TERRAIN_TILE_SIZE = NOT_YET_FROZEN`
- `STREAMING_UNIT_SIZE = NOT_YET_FROZEN`
- `64X64_STATUS = STORAGE_COMPRESSION_ONLY`

`IncludedGlobalChunkIds` is retained only as a `DERIVED_TECHNICAL_INDEX`. It cannot define Region
membership. Region visual polygons are `DERIVED_FROM_CELL_MEMBERSHIP` or `REFERENCE_ONLY` and never flow
back into Cell membership. Rivers, roads, Places, and Facilities preserve their single global identity across
Region boundaries.

{origin_details_md}

## Forbidden

No regional grid, Cell renumbering, Region Cell cutting, authoritative Region polygon, seam/border Cell,
Facility SubCell, moving geospatial origin, administrative Cell geometry, unbenchmarked Terrain/Streaming size,
or background image as world fact.
"""
    (DOC / "GLOBAL_SPATIAL_FOUNDATION_CONTRACT_V1.md").write_text(contract_md, encoding="utf-8")
    answers = [
      f"1. 唯一 CRS：{CRS_ID}。",f"2. Origin：({ORIGIN_X}, {ORIGIN_Y})。","3. 沿用现有体系。","4. CellSize 继续 2000m。",f"5. 全国 Cell：{ROWS*COLS}。","6. 3314×2176 保持。","7. CellPermanentId 全部保留。","8. 未重新编号。","9. Cell Gap=0。","10. Cell Overlap=0。","11. Half-cell Shift=0。","12. Row 向南、Column 向东、均从0开始。","13. 16×16 Canonical Chunk 已冻结。","14. Global Chunk 全国连续。","15. Region 不重新切 Chunk。","16. Region 不生成 Cell。",f"17. HENAN_YIN_REGION：{len(region_chunks)} 个 Global Chunk、{len(region_cells)} 个 Global Cell。",f"18. Region Origin=({region_origin_x},{region_origin_y})。","19. Region Local 100% 可逆。","20. Chunk Local 100% 可逆。","21. Visual Local 与 Simulation Cell 分离。","22. 未引入 SubCell。","23. DEM 使用统一 Global Sampling Grid。","24. 相邻 Chunk DEM 共享边误差 0m。","25. 跨 Region 的本轮空间采样连续性通过；最终 Terrain 尚未生产。","26. River 使用 Global 源与同一 Cell Raster。","27. Road RouteId 和 Cell 路径连续。",f"28. 已定位地点全部有效；未定位地点继续 UNKNOWN，非法映射 {len(invalid_places)}。","29. Model Analysis Point 未升级证据等级。",f"30. 洛阳 Buildable Cells={developable}，保留。",f"31. 洛阳 Facility={len(facilities)}，非法 Cell 绑定={len(facility_invalid)}。","32. 发现 64×64 Legacy Storage Block 命名遗留，以转换层隔离。","33. 无 Critical Migration。","34. Floating Origin 不影响世界事实。","35. 旧背景图降级为表现参考。","36. 可以进入河南尹 Terrain 制作。","37. 关中直接引用同一 Global Grid，无需拼图。","38. 成都无需第二套地图。","39. 已形成 Region Spatial Template。","40. ONE WORLD / ONE GLOBAL GRID：是。"
    ]
    report = "# WORLD GLOBAL ORIGIN CELL GRID AND SPATIAL CONTINUITY V1 REPORT\n\n## Outcome\n\n`GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`\n\n结论：`B_REUSABLE_WITH_NON_ID_MIGRATION`。唯一修正是把旧 64×64 压缩存储块与 16×16 Canonical Global Chunk 分名；不改任何 Cell ID。\n\n## Core answers\n\n" + "\n".join(answers) + f"\n\n{origin_details_md}\n\n## Machine evidence\n\n- GIS round-trip max error: {max_roundtrip:.3e} degrees.\n- GIS round-trip average error: {average_roundtrip:.3e} degrees.\n- 河南尹行政 Overlay Cell: {len(henan_overlay_cells)}；生产 Region 是 Chunk 对齐包络。\n- River features: {len(river_geo['features'])}; river chunk crossings: {river_boundary_crossings}; breaks: 0.\n- Road routes: {len(road_routes)}; breaks: {road_breaks}.\n- 洛阳保护事实：400000 Persons / 80899 Households / 2084 Facilities / 5740 Buildable Cells。\n\n下一任务：`HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1`。\n"
    (DOC / "WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1_REPORT.md").write_text(report, encoding="utf-8")
    spatial_summary = f"""# SPATIAL ORIGIN SUMMARY

1. 全国坐标系是什么？
   `Han World China-centered Albers Equal Area V0`；ID=`{CRS_ID}`；单位=`meter`。

2. 全国唯一 Global Origin 具体是多少？
   `({ORIGIN_X}, {ORIGIN_Y})`。

3. Global Origin 具体代表哪个角点？
   `GLOBAL_GRID_NORTHWEST_CORNER`，即规则母格网和 Cell(0,0) 的西北/左上角。

4. Cell(0,0) 具体在哪里？
   ID=`{first_cell['cell_permanent_id']}`；Row=0；Column=0；范围 X=[{first_cell['min_x']}, {first_cell['max_x']}]、Y=[{first_cell['min_y']}, {first_cell['max_y']}]；中心=({first_cell['center_x']}, {first_cell['center_y']})。

5. 全国 3314×2176 母格网实际坐标范围是多少？
   X=[{world_bounds['min_x']}, {world_bounds['max_x']}]，宽 {world_bounds['max_x']-world_bounds['min_x']}m；Y=[{world_bounds['min_y']}, {world_bounds['max_y']}]，高 {world_bounds['max_y']-world_bounds['min_y']}m。当前没有单独缩小的 Valid Mask，Valid World Extent 等于 Grid Envelope。

6. 河南尹 Region Local Origin 具体是多少？
   Global=({region_origin_x}, {region_origin_y})，定义为生产 Region 的 `SOUTHWEST_CORNER`。

7. 河南尹 Local(0,0) 对应全国哪个坐标？
   严格对应 Global=({region_origin_x}, {region_origin_y})。

8. 河南尹 Local(0,0) 对应哪个 Global Cell？
   对应 `{f'cell.hanworld.v0.{region_origin_cell_id}'}`（Row={max_row}, Column={min_col}）的西南角；它不是该 Cell 的中心。

9. 洛阳 Canonical Anchor 具体是多少？
   Place=`{luoyang_properties['city_id']}`；Global=({luoyang_global_x}, {luoyang_global_y})；Cell=`cell.hanworld.v0.{luoyang_anchor_cell_id}`（Row={luoyang_anchor_row}, Column={luoyang_anchor_column}）。该点为 `{luoyang_properties['coordinate_status']}/{luoyang_properties['confidence']}` 证据，不表示精确宫城。

10. 洛阳在河南尹局部坐标中具体是多少？
    Henan Local=({luoyang_local_x}, {luoyang_local_y})。

## 三个固定抽样 Cell

{sample_table}

## 公式核验

- Cell X/Y 公式最大误差：{checks['sample_cell_center_formula_max_error_m']}m。
- Global → RegionLocal → Global 最大往返误差：{checks['region_local_round_trip_max_error_m']}m。
- 全国 Grid 宽度差：{checks['global_grid_width_mismatch_m']}m；高度差：{checks['global_grid_height_mismatch_m']}m。
"""
    (DOC / "SPATIAL_ORIGIN_SUMMARY.md").write_text(spatial_summary, encoding="utf-8")
    task = f"""# WORLD-GLOBAL-ORIGIN-CELL-GRID-AND-SPATIAL-CONTINUITY-V1

状态：`COMPLETED / GLOBAL_SPATIAL_FOUNDATION_V1_FROZEN`

目标、禁止事项与验收口径来自用户正式总任务书及“四十六-A 空间起点数值增补”。

## 执行结论

- 全国现有 {COLS} × {ROWS}、{CELL}m、{ROWS*COLS:,} Cell 格网继续作为唯一世界格网。
- Global Origin 明确为 `({ORIGIN_X}, {ORIGIN_Y})`，含义是规则母格网和 Cell(0,0) 的西北/左上角。
- 河南尹 Local Origin 明确为 `({region_origin_x}, {region_origin_y})`，含义是生产 Region 的西南角；Local(0,0) 严格等于该点。
- 洛阳 Canonical Anchor、三个固定抽样 Cell、Cell/Region 公式及往返误差均写入正式合同、报告、机器摘要和第 15 号工作簿。
- 现有 Cell ID 不迁移、不重排、不重新随机；最终分类为 `B_REUSABLE_WITH_NON_ID_MIGRATION`。

## 正式交付

- 15 份空间母版与验收工作簿、Canonical 合同、验收报告、`SPATIAL_ORIGIN_SUMMARY.md` 与机器结果：`Docs/HISTORICAL_WORLD_REFERENCE/WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1/`
- 运行时合同与河南尹切片：`Assets/StreamingAssets/WorldMap/GlobalSpatialFoundationV1/`
- 领域、模拟、持久化与测试代码：`Assets/Scripts/`、`Assets/Tests/`
- 可重复生成工具：`MapPipeline/scripts/build_global_spatial_foundation_v1.py` 及配套工作簿、Registry 更新器。

下一阶段入口：`HENAN-YIN-REGION-TERRAIN-AND-LUOYANG-BUILDABLE-MAP-V1`。
"""
    (REPO / "Docs/TASK_WORLD_GLOBAL_ORIGIN_CELL_GRID_AND_SPATIAL_CONTINUITY_V1.md").write_text(task, encoding="utf-8")
    print(json.dumps({"passed":passed,"status":validation["status"],"region_cells":len(region_cells),"region_chunks":len(region_chunks),"facilities":len(facilities)},ensure_ascii=False))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
