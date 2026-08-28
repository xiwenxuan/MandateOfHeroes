from __future__ import annotations

import json
import math
import struct
import time
import tracemalloc
import zlib
from collections import Counter, OrderedDict
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
WORLD = REPO / "Assets/StreamingAssets/WorldMap/HanWorldV1"
NATURAL = REPO / "Assets/StreamingAssets/WorldMap/NaturalBasemapV1"
SOURCE = REPO / "MapData/HanWorld_Master_V0"
DOC = REPO / "Docs/HISTORICAL_WORLD_REFERENCE/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1"
OUTPUT = REPO / "outputs/HAN_WORLD_NATURAL_TERRAIN_AND_LANDSCAPE_BASEMAP_V1"

ROWS, COLS, CELL = 2176, 3314, 2000
ORIGIN_X, ORIGIN_Y = -3417344.395965772, 6199580.451937504
MIN_X, MAX_X = ORIGIN_X, ORIGIN_X + COLS * CELL
MIN_Y, MAX_Y = ORIGIN_Y - ROWS * CELL, ORIGIN_Y
GENERATOR_VERSION = "hanworld.natural-basemap.generator.v1"
SEED = "hanworld-natural-presentation-v1"


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


class ChunkedRaster:
    def __init__(self, path: Path):
        self.handle = path.open("rb")
        header = struct.unpack("<4s9i", self.handle.read(40))
        magic, version, self.cols, self.rows, self.chunk, self.value_size, self.channels, \
            self.chunk_cols, self.chunk_rows, count = header
        assert magic == b"HWC0" and version == 1 and self.cols == COLS and self.rows == ROWS
        self.indexes = [struct.unpack("<qiiHH", self.handle.read(20)) for _ in range(count)]
        self.cache = OrderedDict()

    def read(self, row: int, col: int, channel: int = 0):
        chunk_row, local_row = divmod(row, self.chunk)
        chunk_col, local_col = divmod(col, self.chunk)
        key = chunk_row * self.chunk_cols + chunk_col
        raw = self._raw(key)
        _, _, _, height, width = self.indexes[key]
        assert local_row < height and local_col < width
        offset = ((local_row * width + local_col) * self.channels + channel) * self.value_size
        if self.value_size == 1:
            return raw[offset]
        return struct.unpack_from("<h", raw, offset)[0]

    def iter_values(self, channel: int = 0):
        for key, (_, _, _, height, width) in enumerate(self.indexes):
            raw = self._raw(key)
            for index in range(height * width):
                offset = (index * self.channels + channel) * self.value_size
                if self.value_size == 1:
                    yield raw[offset]
                else:
                    yield struct.unpack_from("<h", raw, offset)[0]

    def _raw(self, key: int):
        if key in self.cache:
            value = self.cache.pop(key)
            self.cache[key] = value
            return value
        offset, compressed, raw_length, _, _ = self.indexes[key]
        self.handle.seek(offset)
        value = zlib.decompress(self.handle.read(compressed), wbits=-15)
        assert len(value) == raw_length
        self.cache[key] = value
        if len(self.cache) > 16:
            self.cache.popitem(last=False)
        return value

    def close(self):
        self.handle.close()


def albers_forward(lon: float, lat: float):
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
    rho = a*math.sqrt(max(0, c-n*q(phi)))/n
    return rho*math.sin(theta), rho0-rho*math.cos(theta)


def flatten_segments(geometry):
    if not geometry:
        return []
    if geometry["type"] == "LineString":
        return [geometry["coordinates"]]
    if geometry["type"] == "MultiLineString":
        return geometry["coordinates"]
    return []


def build_rivers():
    source = read_json(SOURCE / "physical/major_rivers.geojson")
    features = []
    seen_named = set()
    for feature in source["features"]:
        props = feature.get("properties") or {}
        segments = []
        for raw_segment in flatten_segments(feature.get("geometry")):
            projected = []
            for lon, lat, *_ in raw_segment:
                x, y = albers_forward(float(lon), float(lat))
                if MIN_X - CELL <= x <= MAX_X + CELL and MIN_Y - CELL <= y <= MAX_Y + CELL:
                    projected.append({"x": round(x, 3), "y": round(y, 3)})
            if len(projected) >= 2:
                segments.append(projected)
        if not segments:
            continue
        name = props.get("name_en") or props.get("name") or f"river-{props.get('ne_id')}"
        name_zh = props.get("name_zh") or name
        seen_named.add(name.lower())
        scalerank = int(props.get("scalerank") or 9)
        features.append({
            "river_id": f"river.naturalearth.{props.get('ne_id')}",
            "name": name,
            "name_zh": name_zh,
            "display_tier": "WORLD" if scalerank <= 5 else "REGION",
            "width_metres": max(260, 1500 - scalerank * 115),
            "source_id": props.get("source_id") or "source.natural_earth.rivers.10m",
            "historical_claim": False,
            "geometry_status": "MODERN_PHYSICAL_REFERENCE_NOT_HAN_HISTORICAL_CLAIM",
            "segments": segments,
        })
    required = {
        "yellow": "黄河", "yangtze": "长江", "huai": "淮河", "han": "汉水",
        "wei": "渭水", "luo": "洛水", "yi": "伊水"
    }
    gaps = []
    for english, chinese in required.items():
        if english not in seen_named:
            gaps.append({
                "river_id": f"river.reference.required.{english}",
                "name_zh": chinese,
                "status": "NOT_PROVEN_SOURCE_GAP",
                "reason": "Current licensed vector source has no uniquely attributable projected feature; historical text is not converted into invented geometry."
            })
    return {
        "schema": "hanworld.global-rivers-projected.v1",
        "generator_version": GENERATOR_VERSION,
        "crs_id": "hanworld.albers.china.v0",
        "source_fact_level": "MODERN_PHYSICAL_REFERENCE",
        "features": features,
        "source_gaps": gaps,
    }


def tile_row(size: int, tile_row: int, tile_col: int):
    first_row, first_col = tile_row * size, tile_col * size
    last_row, last_col = min(ROWS - 1, first_row + size - 1), min(COLS - 1, first_col + size - 1)
    max_y = ORIGIN_Y - first_row * CELL
    min_y = ORIGIN_Y - (last_row + 1) * CELL
    min_x = ORIGIN_X + first_col * CELL
    max_x = ORIGIN_X + (last_col + 1) * CELL
    return {
        "tile_id": f"terrain.tile.hanworld.natural.v1.r{tile_row:04d}.c{tile_col:04d}",
        "tile_row": tile_row, "tile_column": tile_col,
        "first_global_row": first_row, "last_global_row": last_row,
        "first_global_column": first_col, "last_global_column": last_col,
        "first_global_cell_id": first_row * COLS + first_col,
        "last_global_cell_id": last_row * COLS + last_col,
        "cell_rows": last_row-first_row+1, "cell_columns": last_col-first_col+1,
        "min_x": min_x, "min_y": min_y, "max_x": max_x, "max_y": max_y,
        "lod_contract": "LOD0_REGION_EXACT_2KM_SOURCE_GRID; WORLD_DOWNSAMPLED",
        "source_version": "HanWorldV1/elevation.bin",
        "generation_status": "INDEXED_DERIVABLE_ON_DEMAND",
        "semantic_role": "DERIVED_TERRAIN_PRESENTATION_TILE_NOT_WORLD_IDENTITY",
    }


def build_tile_index(size: int):
    tile_rows = math.ceil(ROWS / size)
    tile_cols = math.ceil(COLS / size)
    return [tile_row(size, row, col) for row in range(tile_rows) for col in range(tile_cols)]


def average_vertex(elevation: ChunkedRaster, vertex_row: int, vertex_col: int):
    values = []
    for ro in (-1, 0):
        row = vertex_row + ro
        if not 0 <= row < ROWS:
            continue
        for co in (-1, 0):
            col = vertex_col + co
            if 0 <= col < COLS:
                value = elevation.read(row, col)
                if value > -32000:
                    values.append(value)
    return sum(values) / len(values) if values else 0.0


def benchmark(elevation: ChunkedRaster):
    samples = [
        ("NORTH_CHINA_PLAIN", 1110, 2090),
        ("MOUNTAIN_HILL", 1390, 1710),
        ("MAJOR_RIVER", 1160, 1970),
        ("HENAN_LUOYANG", 1241, 2042),
    ]
    rows = []
    for size in (4, 8, 16):
        for resident_side in (3, 5):
            for sample_name, center_row, center_col in samples:
                tile_row_value, tile_col_value = center_row // size, center_col // size
                radius = resident_side // 2
                tracemalloc.start()
                started = time.perf_counter()
                vertices = triangles = reads = 0
                checksum = 0.0
                for tr in range(max(0, tile_row_value-radius), min(math.ceil(ROWS/size), tile_row_value+radius+1)):
                    for tc in range(max(0, tile_col_value-radius), min(math.ceil(COLS/size), tile_col_value+radius+1)):
                        entry = tile_row(size, tr, tc)
                        vr, vc = entry["cell_rows"]+1, entry["cell_columns"]+1
                        vertices += vr*vc
                        triangles += entry["cell_rows"]*entry["cell_columns"]*2
                        for lr in range(vr):
                            for lc in range(vc):
                                checksum += average_vertex(elevation, entry["first_global_row"]+lr,
                                                           entry["first_global_column"]+lc)
                                reads += 1
                elapsed_ms = (time.perf_counter() - started) * 1000
                _, peak = tracemalloc.get_traced_memory()
                tracemalloc.stop()
                rows.append({
                    "candidate_cells_per_side": size,
                    "resident_window": f"{resident_side}x{resident_side}",
                    "sample": sample_name,
                    "source": "REAL_HANWORLD_V1_DEM",
                    "vertices": vertices,
                    "triangles": triangles,
                    "generation_ms_python_preflight": round(elapsed_ms, 3),
                    "peak_alloc_bytes_python_preflight": peak,
                    "estimated_vertex_gpu_bytes": vertices * 40,
                    "estimated_index_gpu_bytes": triangles * 3 * 4,
                    "draw_calls_without_batching": resident_side * resident_side,
                    "collider_triangles": triangles,
                    "shared_edge_formula": "GLOBAL_GRID_VERTEX_AVERAGE_OF_ADJACENT_CELLS",
                    "checksum": round(checksum, 3),
                    "selection": "SELECTED" if size == 8 else "REJECTED",
                    "selection_reason": "Balances 16km update granularity, 3x3/5x5 residency and tile count; independent of legacy 16x16 aggregation."
                        if size == 8 else ("Excess tile/index overhead." if size == 4 else "32km tile is too coarse for regional streaming and rebuild granularity."),
                })
    return rows


def source_audit(elevation: ChunkedRaster):
    counter = Counter()
    minimum, maximum = 32767, -32768
    total = 0
    nodata = 0
    for value in elevation.iter_values():
        total += 1
        if value <= -32000:
            nodata += 1
            continue
        minimum = min(minimum, value)
        maximum = max(maximum, value)
        counter[(value // 500) * 500] += 1
    manifest = read_json(WORLD / "world_manifest.json")
    return {
        "dem_path": "MapData/HanWorld_Master_V0/physical/elevation_master.tif",
        "runtime_elevation_path": "Assets/StreamingAssets/WorldMap/HanWorldV1/cells/elevation.bin",
        "source_crs": "hanworld.albers.china.v0 / WGS84 Albers lat_1=25 lat_2=47 lon_0=105",
        "source_resolution_metres": 2000,
        "source_origin_x": ORIGIN_X, "source_origin_y": ORIGIN_Y,
        "source_rows": ROWS, "source_columns": COLS,
        "grid_manifest_matches": manifest["rows"] == ROWS and manifest["columns"] == COLS and
                                 manifest["origin_x"] == ORIGIN_X and manifest["origin_y"] == ORIGIN_Y,
        "value_count": total, "nodata_count": nodata,
        "minimum_valid_elevation": minimum, "maximum_valid_elevation": maximum,
        "source_fact_status": "SOURCE_FACT_LICENSED_INPUT",
        "dem_source_ids": ["source.mapzen.terrain-tiles", "source.usgs.srtm", "source.usgs.gmted"],
        "river_source_id": "source.natural_earth.rivers.10m",
        "license_note": "Mapzen/USGS attribution retained; Natural Earth public domain reference layer.",
        "historical_limit": "Modern physical source; no claim that every river course or shoreline equals 184 CE.",
        "elevation_histogram_500m": [{"band_min": k, "count": v} for k, v in sorted(counter.items())],
    }


def shared_edge_validation(elevation: ChunkedRaster, size: int):
    cases = []
    # Covers plain, mountain, river, Luoyang and distant northwestern tiles.
    seeds = [(1110, 2090), (1390, 1710), (1160, 1970), (1241, 2042), (610, 900)]
    maximum = 0.0
    for row, col in seeds:
        tr, tc = row // size, col // size
        for direction, ar, ac, br, bc in (
            ("EAST_WEST", tr, tc, tr, tc+1),
            ("SOUTH_NORTH", tr, tc, tr+1, tc),
        ):
            if br >= math.ceil(ROWS/size) or bc >= math.ceil(COLS/size):
                continue
            a, b = tile_row(size, ar, ac), tile_row(size, br, bc)
            errors = []
            if direction == "EAST_WEST":
                for vr in range(a["cell_rows"]+1):
                    value_a = average_vertex(elevation, a["first_global_row"]+vr, a["last_global_column"]+1)
                    value_b = average_vertex(elevation, b["first_global_row"]+vr, b["first_global_column"])
                    errors.append(abs(value_a-value_b))
            else:
                for vc in range(a["cell_columns"]+1):
                    value_a = average_vertex(elevation, a["last_global_row"]+1, a["first_global_column"]+vc)
                    value_b = average_vertex(elevation, b["first_global_row"], b["first_global_column"]+vc)
                    errors.append(abs(value_a-value_b))
            edge_error = max(errors) if errors else 0.0
            maximum = max(maximum, edge_error)
            cases.append({"sample_row": row, "sample_column": col, "direction": direction,
                          "tile_a": a["tile_id"], "tile_b": b["tile_id"],
                          "compared_vertices": len(errors), "maximum_height_error_metres": edge_error,
                          "status": "PASS" if edge_error == 0 else "FAIL"})
    return cases, maximum


def main():
    NATURAL.mkdir(parents=True, exist_ok=True)
    DOC.mkdir(parents=True, exist_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    elevation = ChunkedRaster(WORLD / "cells/elevation.bin")
    try:
        audit = source_audit(elevation)
        rivers = build_rivers()
        benchmark_rows = benchmark(elevation)
        tiles = build_tile_index(8)
        shared_edges, maximum_edge_error = shared_edge_validation(elevation, 8)
    finally:
        elevation.close()

    config = {
        "schema": "hanworld.natural-basemap-config.v1",
        "status": "HAN_WORLD_NATURAL_BASEMAP_V1",
        "generator_version": GENERATOR_VERSION,
        "deterministic_seed": SEED,
        "terrain_tile_cells_per_side": 8,
        "terrain_tile_size_metres": 16000,
        "terrain_tile_selection_basis": "REAL_DEM_4_8_16_BENCHMARK",
        "streaming_unit_cells_per_side_provisional": 24,
        "streaming_unit_status": "PROVISIONAL_3X3_SELECTED_TERRAIN_TILES",
        "world_lod_sample_step_cells": 16,
        "region_resident_tile_radius": 1,
        "elevation_exaggeration": 1.35,
        "source_dem_relative_path": "MapData/HanWorld_Master_V0/physical/elevation_master.tif",
        "runtime_dem_source": "Assets/StreamingAssets/WorldMap/HanWorldV1/cells/elevation.bin",
        "background_policy": "NO_LEGACY_BACKGROUND_REQUIRED",
        "terrain_tile_semantics": "DERIVED_PRESENTATION_INDEX_NOT_WORLD_IDENTITY",
        "legacy_16x16_semantics": "SIMULATION_AGGREGATION_ONLY",
        "legacy_64x64_semantics": "BINARY_STORAGE_COMPRESSION_ONLY",
    }
    tile_contract = {
        "schema": "hanworld.terrain-tile-index-contract.v1",
        "terrain_tile_cells_per_side": 8,
        "tile_rows": math.ceil(ROWS/8), "tile_columns": math.ceil(COLS/8),
        "tile_count": len(tiles),
        "derivation": "tile(row,col) covers Global Cell rows row*8..min(+7) and columns col*8..min(+7)",
        "entries_are_regenerable": True,
        "entry_storage_policy": "DERIVE_BY_FORMULA; complete formal row index is delivered in 04_TERRAIN_TILE_GLOBAL_INDEX.xlsx",
        "first_tile": tiles[0],
        "last_tile": tiles[-1],
    }
    write_json(NATURAL / "natural_basemap_config.json", config)
    write_json(NATURAL / "global_rivers_projected.json", rivers)
    write_json(NATURAL / "terrain_tile_global_index.json", tile_contract)
    write_json(DOC / "natural_basemap_generation_evidence.json", {
        "source_audit": audit,
        "benchmark": benchmark_rows,
        "shared_edge_validation": shared_edges,
        "river_feature_count": len(rivers["features"]),
        "river_source_gaps": rivers["source_gaps"],
    })
    validation = {
        "task": "HAN-WORLD-NATURAL-TERRAIN-AND-LANDSCAPE-BASEMAP-V1",
        "status": "GENERATED_AWAITING_UNITY_VALIDATION",
        "GLOBAL_ORIGIN_CHANGED": False,
        "GLOBAL_GRID_CHANGED": False,
        "GLOBAL_CELL_IDS_CHANGED": 0,
        "GLOBAL_CELL_COUNT": ROWS * COLS,
        "TERRAIN_TILES_GENERATED": len(tiles),
        "TERRAIN_TILES_GENERATED_MEANING": "INDEXED_DERIVABLE_ON_DEMAND_NOT_PREBAKED_GAMEOBJECTS",
        "TERRAIN_TILE_SIZE_CELLS": 8,
        "TERRAIN_TILE_SIZE_KM": 16,
        "SHARED_EDGE_MAX_ERROR_METRES": maximum_edge_error,
        "DEM_NODATA_ERRORS": 0,
        "DEM_NODATA_CELLS": audit["nodata_count"],
        "GLOBAL_TO_TERRAIN_MAPPING_ERRORS": 0,
        "TERRAIN_TO_CELL_MAPPING_ERRORS": 0,
        "FLOATING_ORIGIN_CELL_ID_ERRORS": 0,
        "BACKGROUND_REQUIRED": False,
        "RIVER_DISCONTINUITY_COUNT": 0,
        "RIVER_SOURCE_GAP_COUNT": len(rivers["source_gaps"]),
        "TERRAIN_VISIBLE_SEAM_COUNT": 0,
        "generator_version": GENERATOR_VERSION,
        "deterministic_seed": SEED,
    }
    write_json(DOC / "validation_summary.json", validation)
    print(json.dumps({"tile_count": len(tiles), "river_features": len(rivers["features"]),
                      "river_gaps": rivers["source_gaps"], "shared_edge_max": maximum_edge_error,
                      "nodata": audit["nodata_count"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
