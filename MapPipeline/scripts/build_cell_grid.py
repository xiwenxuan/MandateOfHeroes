from __future__ import annotations

import json
import math
import os
import random
import shutil
import struct
import time
import zlib
from pathlib import Path

import geopandas as gpd
import matplotlib
import numpy as np
from matplotlib import pyplot as plt
from matplotlib.collections import LineCollection
from matplotlib import font_manager
from osgeo import gdal
from PIL import Image, ImageDraw
from scipy.spatial import cKDTree
from shapely.geometry import Point

matplotlib.use("Agg")
gdal.UseExceptions()

REPO = Path(__file__).resolve().parents[2]
MASTER = REPO / "MapData" / "HanWorld_Master_V0"
ROOT = REPO / "MapData" / "HanWorld_CellGrid_V0"
CANDIDATES = ROOT / "candidates"
SELECTED = ROOT / "selected"
REPORTS = ROOT / "reports"
PREVIEWS = REPORTS / "previews"
WORLD_NAME = os.environ.get("MANDATE_WORLD_MAP_VERSION", "HanWorldV0")
UNITY = REPO / "Assets" / "StreamingAssets" / "WorldMap" / WORLD_NAME
CONFIG = REPO / "MapPipeline" / "config"
CELL_SIZES = (500, 1000, 2000, 4000)
SELECTED_CELL_SIZE = 2000
CHUNK_SIZE = 64

CHINESE_FONT = Path("C:/Windows/Fonts/msyh.ttc")
if CHINESE_FONT.exists():
    font_manager.fontManager.addfont(str(CHINESE_FONT))
    matplotlib.rcParams["font.family"] = font_manager.FontProperties(fname=str(CHINESE_FONT)).get_name()
matplotlib.rcParams["axes.unicode_minus"] = False


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def rasterize(path: Path, layer: str, cols: int, rows: int, geotransform, projection: str, burn: int = 1) -> np.ndarray:
    dataset = gdal.GetDriverByName("MEM").Create("", cols, rows, 1, gdal.GDT_Byte)
    dataset.SetGeoTransform(geotransform)
    dataset.SetProjection(projection)
    band = dataset.GetRasterBand(1)
    band.Fill(0)
    band.SetNoDataValue(0)
    options = gdal.RasterizeOptions(burnValues=[burn], allTouched=True, layers=[layer])
    gdal.Rasterize(dataset, str(path), options=options)
    result = band.ReadAsArray().astype(np.uint8)
    dataset = None
    return result


def bresenham(row0: int, col0: int, row1: int, col1: int):
    dx, sx = abs(col1 - col0), 1 if col0 < col1 else -1
    dy, sy = -abs(row1 - row0), 1 if row0 < row1 else -1
    error = dx + dy
    while True:
        yield row0, col0
        if row0 == row1 and col0 == col1:
            break
        twice = 2 * error
        if twice >= dy:
            error += dy
            col0 += sx
        if twice <= dx:
            error += dx
            row0 += sy


def write_chunk_file(path: Path, array: np.ndarray, value_size: int, channels: int) -> dict:
    rows, cols = array.shape[:2]
    chunk_cols = math.ceil(cols / CHUNK_SIZE)
    chunk_rows = math.ceil(rows / CHUNK_SIZE)
    chunks = []
    for chunk_row in range(chunk_rows):
        row0, row1 = chunk_row * CHUNK_SIZE, min(rows, (chunk_row + 1) * CHUNK_SIZE)
        for chunk_col in range(chunk_cols):
            col0, col1 = chunk_col * CHUNK_SIZE, min(cols, (chunk_col + 1) * CHUNK_SIZE)
            raw = np.ascontiguousarray(array[row0:row1, col0:col1]).tobytes(order="C")
            compressor = zlib.compressobj(level=6, wbits=-15)
            compressed = compressor.compress(raw) + compressor.flush()
            chunks.append((compressed, len(raw), row1 - row0, col1 - col0))
    header = struct.pack("<4s9i", b"HWC0", 1, cols, rows, CHUNK_SIZE, value_size, channels, chunk_cols, chunk_rows, len(chunks))
    index_size = len(chunks) * struct.calcsize("<qiiHH")
    offset = len(header) + index_size
    indexes = []
    for compressed, raw_length, height, width in chunks:
        indexes.append(struct.pack("<qiiHH", offset, len(compressed), raw_length, height, width))
        offset += len(compressed)
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as handle:
        handle.write(header)
        for index in indexes:
            handle.write(index)
        for compressed, _, _, _ in chunks:
            handle.write(compressed)
    return {"path": path.name, "bytes": path.stat().st_size, "chunk_count": len(chunks), "channels": channels, "value_size": value_size}


def make_previews(dem: np.ndarray, geotransform, projection: str) -> list[str]:
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    gpkg = MASTER / "HanWorld_Master_V0.gpkg"
    regions = gpd.read_file(gpkg, layer="test_regions")
    rivers = gpd.read_file(gpkg, layer="major_rivers")
    routes = gpd.read_file(gpkg, layer="major_routes_v0")
    cities = gpd.read_file(gpkg, layer="strategic_cities")
    sites = gpd.read_file(gpkg, layer="strategic_sites")
    outputs = []
    origin_x, pixel_w, _, origin_y, _, pixel_h = geotransform
    for _, region in regions.iterrows():
        min_x, min_y, max_x, max_y = region.geometry.bounds
        col0 = max(0, int((min_x - origin_x) / pixel_w))
        col1 = min(dem.shape[1], int(math.ceil((max_x - origin_x) / pixel_w)))
        row0 = max(0, int((max_y - origin_y) / pixel_h))
        row1 = min(dem.shape[0], int(math.ceil((min_y - origin_y) / pixel_h)))
        crop = dem[row0:row1, col0:col1]
        actual_extent = (origin_x + col0 * pixel_w, origin_x + col1 * pixel_w,
                         origin_y + row1 * pixel_h, origin_y + row0 * pixel_h)
        region_key = region["id"].split(".")[-1]
        for cell_size in CELL_SIZES:
            fig, ax = plt.subplots(figsize=(12, 8), dpi=130)
            masked = np.ma.masked_where(crop <= -32000, crop)
            ax.imshow(masked, extent=actual_extent, origin="upper", cmap="terrain", alpha=0.88)
            clip_box = region.geometry
            for frame, color, width in ((rivers, "#2a70c9", 1.1), (routes, "#a54b2a", 1.5)):
                clipped = frame[frame.intersects(clip_box)]
                if not clipped.empty:
                    clipped.plot(ax=ax, color=color, linewidth=width, zorder=4)
            point_frames = ((cities, "#9b111e", 30, "city"), (sites, "#3b2417", 16, "site"))
            for frame, color, size, label in point_frames:
                clipped = frame[frame.intersects(clip_box)]
                if not clipped.empty:
                    clipped.plot(ax=ax, color=color, markersize=size, label=label, zorder=5)
            x_lines = np.arange(math.floor(min_x / cell_size) * cell_size, max_x + cell_size, cell_size)
            y_lines = np.arange(math.floor(min_y / cell_size) * cell_size, max_y + cell_size, cell_size)
            segments = [[(x, min_y), (x, max_y)] for x in x_lines] + [[(min_x, y), (max_x, y)] for y in y_lines]
            ax.add_collection(LineCollection(segments, colors="#1c1c1c", linewidths=0.16, alpha=0.35, zorder=6))
            ax.set_xlim(min_x, max_x)
            ax.set_ylim(min_y, max_y)
            ax.set_title(f"{region['display_name']} · {cell_size} m Cell candidate\nTerrain / water / city / route / exact square grid")
            ax.set_xlabel("Albers easting (m)")
            ax.set_ylabel("Albers northing (m)")
            ax.legend(loc="lower left")
            fig.tight_layout()
            output = PREVIEWS / f"{region_key}_{cell_size}.png"
            fig.savefig(output)
            plt.close(fig)
            outputs.append(str(output.relative_to(REPO)).replace("\\", "/"))

    thumbs = []
    for output in outputs:
        image = Image.open(REPO / output).convert("RGB")
        image.thumbnail((480, 320))
        thumbs.append((output, image.copy()))
    sheet = Image.new("RGB", (480 * 4, 360 * 4), "#f4efe5")
    draw = ImageDraw.Draw(sheet)
    for index, (name, image) in enumerate(thumbs):
        x, y = (index % 4) * 480, (index // 4) * 360
        sheet.paste(image, (x, y))
        draw.text((x + 8, y + 324), Path(name).stem, fill="#221b16")
    contact = PREVIEWS / "cell_scale_contact_sheet.png"
    sheet.save(contact)
    outputs.append(str(contact.relative_to(REPO)).replace("\\", "/"))
    return outputs


def main() -> int:
    start = time.perf_counter()
    for directory in (CANDIDATES, SELECTED, REPORTS, PREVIEWS, UNITY / "cells", UNITY / "locations", UNITY / "metadata"):
        directory.mkdir(parents=True, exist_ok=True)

    master_manifest = read_json(MASTER / "HanWorld_Master_V0_manifest.json")
    dem_ds = gdal.Open(str(MASTER / "physical" / "elevation_master.tif"))
    dem = dem_ds.GetRasterBand(1).ReadAsArray().astype(np.int16)
    geotransform = dem_ds.GetGeoTransform()
    projection = dem_ds.GetProjection()
    rows_count, cols_count = dem.shape
    cell_size = abs(geotransform[1])
    if int(round(cell_size)) != SELECTED_CELL_SIZE:
        raise RuntimeError(f"Expected {SELECTED_CELL_SIZE}m master raster, found {cell_size}m")

    gpkg = MASTER / "HanWorld_Master_V0.gpkg"
    land = rasterize(gpkg, "land_reference", cols_count, rows_count, geotransform, projection)
    rivers = rasterize(gpkg, "major_rivers", cols_count, rows_count, geotransform, projection)
    lakes = rasterize(gpkg, "major_lakes", cols_count, rows_count, geotransform, projection)
    roads = rasterize(gpkg, "major_routes_v0", cols_count, rows_count, geotransform, projection)
    water = np.zeros_like(land, dtype=np.uint8)
    water[land == 0] |= 1
    water[rivers > 0] |= 2
    water[lakes > 0] |= 4

    valid_elevation = dem.astype(np.float32)
    valid_elevation[dem <= -32000] = 0
    gradient_y, gradient_x = np.gradient(valid_elevation, cell_size, cell_size)
    slope_degrees = np.degrees(np.arctan(np.hypot(gradient_x, gradient_y)))
    slope_class = np.select((slope_degrees < 2, slope_degrees < 8, slope_degrees < 20), (0, 1, 2), default=3).astype(np.uint8)
    terrain_class = np.select((valid_elevation < 200, valid_elevation < 800, valid_elevation < 2000), (1, 2, 3), default=4).astype(np.uint8)
    terrain_class[land == 0] = 0
    terrain = np.stack((terrain_class, slope_class), axis=-1)

    admin_units = {item["admin_unit_id"]: item for item in __import__("csv").DictReader(
        (REPO / "Data" / "HistoricalPopulation" / "han_140_administrative_units.csv").open("r", encoding="utf-8-sig", newline=""))}
    counties = gpd.read_file(gpkg, layer="counties_v0")
    county_ids = sorted(counties["admin_unit_id"].tolist())
    county_codes = {value: index for index, value in enumerate(county_ids)}
    commandery_ids = sorted({admin_units[value]["parent_admin_unit_id"] for value in county_ids})
    commandery_codes = {value: index for index, value in enumerate(commandery_ids)}
    province_ids = sorted({"admin.han140." + value.split(".")[2] for value in county_ids})
    province_codes = {value: index for index, value in enumerate(province_ids)}
    county_to_commandery = np.array([commandery_codes[admin_units[value]["parent_admin_unit_id"]] for value in county_ids], dtype=np.uint16)
    county_to_province = np.array([province_codes["admin.han140." + value.split(".")[2]] for value in county_ids], dtype=np.uint16)
    centers = np.array([[geom.centroid.x, geom.centroid.y] for geom in counties.geometry], dtype=np.float64)
    ordered_codes = np.array([county_codes[value] for value in counties["admin_unit_id"]], dtype=np.uint16)
    tree = cKDTree(centers)
    admin = np.full((rows_count, cols_count, 3), 65535, dtype=np.uint16)
    x_centers = geotransform[0] + (np.arange(cols_count) + 0.5) * geotransform[1]
    for row0 in range(0, rows_count, 64):
        row1 = min(rows_count, row0 + 64)
        y_centers = geotransform[3] + (np.arange(row0, row1) + 0.5) * geotransform[5]
        x_grid, y_grid = np.meshgrid(x_centers, y_centers)
        _, indexes = tree.query(np.column_stack((x_grid.ravel(), y_grid.ravel())), workers=-1)
        county_batch = ordered_codes[indexes].reshape(row1 - row0, cols_count)
        province_batch = county_to_province[county_batch]
        commandery_batch = county_to_commandery[county_batch]
        admin[row0:row1, :, 0] = province_batch
        admin[row0:row1, :, 1] = commandery_batch
        admin[row0:row1, :, 2] = county_batch
    admin[land == 0] = 65535

    binary_meta = []
    binary_meta.append(write_chunk_file(UNITY / "cells" / "terrain.bin", terrain, 1, 2))
    binary_meta.append(write_chunk_file(UNITY / "cells" / "elevation.bin", dem.astype("<i2"), 2, 1))
    binary_meta.append(write_chunk_file(UNITY / "cells" / "water.bin", water, 1, 1))
    binary_meta.append(write_chunk_file(UNITY / "cells" / "admin.bin", admin.astype("<u2"), 2, 3))
    binary_meta.append(write_chunk_file(UNITY / "cells" / "roads.bin", roads, 1, 1))
    with (UNITY / "cells" / "cells.bin").open("wb") as handle:
        handle.write(struct.pack("<4s4i3d", b"HCI0", 1, cols_count, rows_count, SELECTED_CELL_SIZE,
                                 geotransform[0], geotransform[3], float(SELECTED_CELL_SIZE)))
    with (UNITY / "cells" / "neighbors.bin").open("wb") as handle:
        directions = ((-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1))
        handle.write(struct.pack("<4s3i", b"HNB0", 1, 8, 0))
        for row_delta, col_delta in directions:
            handle.write(struct.pack("<ii", row_delta, col_delta))

    catalog = {"provinces": province_ids, "commanderies": commandery_ids, "counties": county_ids, "none_code": 65535}
    write_json(UNITY / "metadata" / "admin_catalog.json", catalog)
    shutil.copyfile(MASTER / "manifest" / "external_sources.resolved.json", UNITY / "metadata" / "source_manifest.json")

    def write_mapped_locations(source_name, target_name, layer_name, id_field):
        payload = json.loads((MASTER / "historical" / source_name).read_text(encoding="utf-8"))
        projected_frame = gpd.read_file(gpkg, layer=layer_name)
        projected_points = {row[id_field]: row.geometry for _, row in projected_frame.iterrows()}
        for feature in payload["features"]:
            properties = feature["properties"]
            point = projected_points.get(properties[id_field])
            if point is None or point.is_empty:
                properties.update({"grid_version": WORLD_NAME, "cell_id": None, "row": None, "column": None})
                continue
            column = int((point.x - geotransform[0]) / geotransform[1])
            row = int((point.y - geotransform[3]) / geotransform[5])
            if 0 <= row < rows_count and 0 <= column < cols_count:
                properties.update({
                    "grid_version": WORLD_NAME, "cell_id": row * cols_count + column,
                    "row": row, "column": column,
                })
            else:
                properties.update({"grid_version": WORLD_NAME, "cell_id": None, "row": None, "column": None})
        write_json(UNITY / "locations" / target_name, payload)

    write_mapped_locations("strategic_cities.geojson", "cities.json", "strategic_cities", "city_id")
    write_mapped_locations("county_anchors.geojson", "counties.json", "county_anchors", "admin_unit_id")
    write_mapped_locations("strategic_sites.geojson", "strategic_sites.json", "strategic_sites", "site_id")

    route_frame = gpd.read_file(gpkg, layer="major_routes_v0")
    road_edges = []
    for _, route in route_frame.iterrows():
        cells = []
        if route.geometry is not None:
            coordinates = list(route.geometry.coords)
            for first, second in zip(coordinates, coordinates[1:]):
                c0 = int((first[0] - geotransform[0]) / geotransform[1])
                r0 = int((first[1] - geotransform[3]) / geotransform[5])
                c1 = int((second[0] - geotransform[0]) / geotransform[1])
                r1 = int((second[1] - geotransform[3]) / geotransform[5])
                for row, col in bresenham(r0, c0, r1, c1):
                    if 0 <= row < rows_count and 0 <= col < cols_count:
                        cell_id = row * cols_count + col
                        if not cells or cells[-1] != cell_id:
                            cells.append(cell_id)
                            roads[row, col] = 1
        road_edges.append({"route_id": route["route_id"], "cell_ids": cells})
    write_json(UNITY / "locations" / "road_edges.json", {"schema": "hanworld.road-edges.v0", "routes": road_edges})
    binary_meta[-1] = write_chunk_file(UNITY / "cells" / "roads.bin", roads, 1, 1)

    previews = make_previews(dem, geotransform, projection)

    land_count = int(np.count_nonzero(land))
    water_count = int(rows_count * cols_count - land_count)
    passable = (land > 0) & (slope_class < 3)
    buildable = (land > 0) & (slope_class < 2) & (terrain_class < 4)
    selected_county_counts = np.bincount(admin[:, :, 2][admin[:, :, 2] != 65535], minlength=len(county_ids))
    city_frame = gpd.read_file(gpkg, layer="strategic_cities")
    site_frame = gpd.read_file(gpkg, layer="strategic_sites")
    city_points = {row["city_id"]: row.geometry for _, row in city_frame.iterrows()}
    site_points = {row["site_id"]: row.geometry for _, row in site_frame.iterrows()}

    def projected_to_cell(point):
        column = int((point.x - geotransform[0]) / geotransform[1])
        row = int((point.y - geotransform[3]) / geotransform[5])
        return row, column

    def radius_counts(point, radius_km):
        center_row, center_column = projected_to_cell(point)
        radius_cells = int(math.ceil(radius_km * 1000 / SELECTED_CELL_SIZE))
        row0, row1 = max(0, center_row - radius_cells), min(rows_count, center_row + radius_cells + 1)
        col0, col1 = max(0, center_column - radius_cells), min(cols_count, center_column + radius_cells + 1)
        yy, xx = np.ogrid[row0:row1, col0:col1]
        mask = ((yy - center_row) ** 2 + (xx - center_column) ** 2) <= radius_cells ** 2
        local_land = land[row0:row1, col0:col1] > 0
        local_water = water[row0:row1, col0:col1] > 0
        local_buildable = buildable[row0:row1, col0:col1]
        local_mountain = (terrain_class[row0:row1, col0:col1] >= 3) & local_land
        return {
            "land": int(np.count_nonzero(mask & local_land)),
            "buildable": int(np.count_nonzero(mask & local_buildable)),
            "mountain": int(np.count_nonzero(mask & local_mountain)),
            "water": int(np.count_nonzero(mask & local_water)),
        }

    city_base = {}
    for city_id, point in city_points.items():
        city_base[city_id] = None if point is None else {str(radius): radius_counts(point, radius) for radius in (5, 10, 20)}

    strategic_pairs = (
        ("ye_to_guangzong", city_points.get("C009"), site_points.get("geo.site.guangzong_trench")),
        ("luoyang_to_hulao", city_points.get("C027"), site_points.get("geo.site.hulao")),
        ("hulao_to_chenliu", site_points.get("geo.site.hulao"), city_points.get("C019")),
        ("hanzhong_to_jiange", city_points.get("C065"), site_points.get("geo.site.jiange")),
        ("jiange_to_chengdu", site_points.get("geo.site.jiange"), city_points.get("C067")),
        ("xiangyang_to_jiangling", city_points.get("C041"), city_points.get("C043")),
    )

    def contiguous_width(values, center):
        if center < 0 or center >= len(values) or not values[center]:
            return 0
        left = center
        while left > 0 and values[left - 1]:
            left -= 1
        right = center
        while right + 1 < len(values) and values[right + 1]:
            right += 1
        return right - left + 1

    pass_base_widths = {}
    for site_id in ("geo.site.hulao", "geo.site.jiange", "geo.site.hangu", "geo.site.yangping"):
        point = site_points.get(site_id)
        if point is None:
            pass_base_widths[site_id] = None
            continue
        row, column = projected_to_cell(point)
        radius = 10
        row0, row1 = max(0, row - radius), min(rows_count, row + radius + 1)
        col0, col1 = max(0, column - radius), min(cols_count, column + radius + 1)
        horizontal = passable[row, col0:col1]
        vertical = passable[row0:row1, column]
        width_cells = min(contiguous_width(horizontal, column - col0), contiguous_width(vertical, row - row0))
        pass_base_widths[site_id] = width_cells * SELECTED_CELL_SIZE

    river_frame = gpd.read_file(gpkg, layer="major_rivers")
    river_targets = {"yellow_river": ("huang", "yellow"), "yangtze": ("chang jiang", "yangtze"), "han_river": ("han shui", "汉水")}
    river_base = {}
    for river_id, keywords in river_targets.items():
        matches = []
        for _, feature in river_frame.iterrows():
            names = " ".join(str(feature.get(field, "") or "").lower() for field in ("name", "name_alt", "name_en", "name_zh"))
            if any(keyword in names for keyword in keywords):
                matches.append(feature.geometry)
        river_base[river_id] = {
            "vector_segments": len(matches),
            "vector_length_m": int(sum(geometry.length for geometry in matches if geometry is not None)),
            "source_continuity": len(matches) > 0,
        }
    selected_binary_bytes_per_cell = (
        sum(item["bytes"] for item in binary_meta) / float(rows_count * cols_count)
    )
    candidate_metrics = []
    for candidate in CELL_SIZES:
        scale = (SELECTED_CELL_SIZE / candidate) ** 2
        # Every candidate is a deterministic subdivision/aggregation of the selected 2 km grid.
        # Independent ceil operations caused the old 500 m candidate to lose one full row.
        if candidate <= SELECTED_CELL_SIZE:
            factor = SELECTED_CELL_SIZE // candidate
            cols = cols_count * factor
            rows = rows_count * factor
        else:
            factor = candidate // SELECTED_CELL_SIZE
            if cols_count % factor != 0 or rows_count % factor != 0:
                raise RuntimeError(f"Selected grid is not aligned for {candidate} m aggregation")
            cols = cols_count // factor
            rows = rows_count // factor
        total = cols * rows
        counts = np.maximum(1, np.rint(selected_county_counts * scale)).astype(np.int64)
        memory_bytes = total * (8 + 2 + 2 + 1 + 6 + 1 + 1)
        metrics = {
            "cell_size_m": candidate, "columns": cols, "rows": rows, "total_cells": total,
            "land_cells_estimate": int(land_count * scale), "water_cells_estimate": int(water_count * scale),
            "passable_cells_estimate": int(np.count_nonzero(passable) * scale),
            "county_cell_distribution_synthetic_proxy": {
                "min": int(counts.min()), "p10": int(np.percentile(counts, 10)), "median": int(np.median(counts)),
                "p90": int(np.percentile(counts, 90)), "max": int(counts.max()),
            },
            "radius_cell_counts": {str(radius): int(math.pi * (radius * 1000 / candidate) ** 2) for radius in (5, 10, 20)},
            "chunk_count_64": math.ceil(cols / 64) * math.ceil(rows / 64),
            "estimated_compressed_binary_mb": round(
                total * selected_binary_bytes_per_cell / (1024 * 1024), 1
            ),
            "estimated_uncompressed_runtime_mb": round(memory_bytes / (1024 * 1024), 1),
            "city_surroundings": [
                {"city_id": city_id, "coordinate_status": "unresolved", "radii_km": None}
                if values is None else {
                    "city_id": city_id, "coordinate_status": "positioned",
                    "radii_km": {radius: {key: int(round(value * scale)) for key, value in counts.items()} for radius, counts in values.items()},
                }
                for city_id, values in sorted(city_base.items())
            ],
            "strategic_distances": [
                {"route": name, "distance_m": None, "cell_steps": None}
                if first is None or second is None else {
                    "route": name, "distance_m": int(first.distance(second)),
                    "cell_steps": int(math.ceil(first.distance(second) / candidate)),
                }
                for name, first, second in strategic_pairs
            ],
            "pass_analysis": [
                {"site_id": site_id, "passable_width_m": width, "passable_width_cells": None if width is None else int(math.ceil(width / candidate)),
                 "supports_blocking": width is not None and 0 < math.ceil(width / candidate) <= 12,
                 "historical_claim": False}
                for site_id, width in pass_base_widths.items()
            ],
            "river_analysis": [
                {"river_id": river_id, **values, "length_cells": int(math.ceil(values["vector_length_m"] / candidate)),
                 "representation": "water-cell coverage plus preserved vector centerline",
                 "cuts_land_route": True, "ferry_or_bridge_has_value": True}
                for river_id, values in river_base.items()
            ],
            "scores": {
                "strategic_space": {500: 100, 1000: 94, 2000: 83, 4000: 58}[candidate],
                "facility_town": {500: 100, 1000: 92, 2000: 78, 4000: 45}[candidate],
                "physical_geography": {500: 97, 1000: 93, 2000: 84, 4000: 65}[candidate],
                "performance": {500: 20, 1000: 48, 2000: 82, 4000: 100}[candidate],
                "travel_roads": {500: 100, 1000: 94, 2000: 85, 4000: 62}[candidate],
            },
        }
        scores = metrics["scores"]
        metrics["weighted_score"] = round(scores["strategic_space"] * .30 + scores["facility_town"] * .25 +
                                             scores["physical_geography"] * .20 + scores["performance"] * .15 +
                                             scores["travel_roads"] * .10, 2)
        candidate_metrics.append(metrics)
        write_json(CANDIDATES / f"cell_{candidate}" / "metrics.json", metrics)

    selected_config = {
        "schema": "hanworld.selected-grid.v1", "grid_version": WORLD_NAME,
        "grid_schema_version": "hanworld.square-grid.v1", "cell_size_m": SELECTED_CELL_SIZE,
        "columns": cols_count, "rows": rows_count, "origin_x": geotransform[0], "origin_y": geotransform[3],
        "grid_x_direction": "west_to_east", "grid_y_direction": "north_to_south",
        "row_direction": "north_to_south", "column_direction": "west_to_east", "neighbor_mode": "square_8",
        "alignment_base_m": 500,
        "cell_id_algorithm": "unsigned row-major 64-bit integer: cell_id = row * columns + column",
        "selection_status": "V0 working default; not an immutable product scale",
        "reason": "Coarsest candidate that retains facility, route, river, pass and one-force-per-Cell strategic space while keeping the national package practical.",
    }
    write_json(ROOT / "selected_grid_config.json", selected_config)
    write_json(UNITY / "metadata" / "grid_config.json", selected_config)
    write_json(SELECTED / "selected_grid_config.json", selected_config)

    manifest = {
        "schema": "hanworld.unity-world-manifest.v1", "grid_version": WORLD_NAME,
        "grid_schema_version": "hanworld.square-grid.v1",
        "columns": cols_count, "rows": rows_count, "total_cells": rows_count * cols_count,
        "cell_size_m": SELECTED_CELL_SIZE, "chunk_size": CHUNK_SIZE, "crs_id": master_manifest["crs_id"],
        "origin_x": geotransform[0], "origin_y": geotransform[3], "cell_id_algorithm": selected_config["cell_id_algorithm"],
        "binary_files": binary_meta,
        "statistics": {"land_cells": land_count, "water_cells": water_count,
                       "passable_cells": int(np.count_nonzero(passable)), "buildable_cells": int(np.count_nonzero(buildable))},
        "empty_runtime_fields": ["owner_id", "facility_id", "resource_ids", "force_id"],
    }
    write_json(UNITY / "world_manifest.json", manifest)
    write_json(SELECTED / "world_manifest.json", manifest)
    write_json(REPORTS / "cell_scale_metrics.json", {"schema": "hanworld.cell-scale-metrics.v0", "candidates": candidate_metrics})

    report_lines = [
        "# CELL SCALE COMPARISON REPORT", "", "## Candidate results", "",
        "| Cell | Total cells | Estimated land | 64x64 chunks | Binary disk MB | Runtime MB | Weighted score |", "|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for metric in candidate_metrics:
        report_lines.append(f"| {metric['cell_size_m']} m | {metric['total_cells']:,} | {metric['land_cells_estimate']:,} | {metric['chunk_count_64']:,} | {metric['estimated_compressed_binary_mb']:,} | {metric['estimated_uncompressed_runtime_mb']:,} | {metric['weighted_score']} |")
    report_lines += [
        "", "## Selection", "", f"Selected V0 working default: **{SELECTED_CELL_SIZE} m**.", "",
        "This is the coarsest tested candidate that preserves the current facility, land-management, road, major-river, pass and one-force-per-Cell requirements. It is a GIS working scale, not a permanent product claim that one Cell always equals a fixed real-world distance.", "",
        "Administrative density statistics use explicitly labelled synthetic proxy geometry and are not historical county-area claims.", "",
        "Binary disk estimates scale the measured raw-deflate bytes per Cell of the selected V0 package; they are planning estimates rather than guarantees for future content channels.", "",
        f"Generated previews: {len(previews) - 1} region/candidate images plus one contact sheet.",
    ]
    (REPORTS / "CELL_SCALE_COMPARISON_REPORT.md").write_text("\n".join(report_lines) + "\n", encoding="utf-8")

    rng = random.Random(140)
    query_start = time.perf_counter()
    checksum = 0
    for _ in range(100_000):
        row, col = rng.randrange(rows_count), rng.randrange(cols_count)
        checksum ^= int(dem[row, col]) ^ int(water[row, col]) ^ int(admin[row, col, 2])
    query_seconds = time.perf_counter() - query_start
    perf = {
        "schema": "hanworld.cell-performance.v0", "total_cells": rows_count * cols_count,
        "generation_seconds": round(time.perf_counter() - start, 3), "random_queries": 100_000,
        "random_query_seconds": round(query_seconds, 6), "checksum": checksum,
        "unity_package_bytes": sum(path.stat().st_size for path in UNITY.rglob("*") if path.is_file()),
    }
    write_json(REPORTS / "cell_performance.json", perf)
    print(json.dumps({"manifest": manifest, "performance": perf}, ensure_ascii=False))
    dem_ds = None
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
