#!/usr/bin/env python3
"""Validate the documentation deliverables for HAN-135-260 historical world V1."""

from __future__ import annotations

import json
import zipfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs" / "HISTORICAL_WORLD_REFERENCE"
OUT = ROOT / "outputs" / "HAN_135_260_HISTORICAL_WORLD_REFERENCE_V1"

EXPECTED = {
    "years": 126,
    "provinces": 13,
    "commanderies": 105,
    "counties": 1182,
    "cities": 77,
    "city_s": 8,
    "persons": 1202,
    "clans": 39,
    "branches": 15,
    "scenarios": 13,
    "routes": 18,
    "sites": 31,
}

WORKBOOKS = [
    "01_135-260逐年历史世界状态索引.xlsx",
    "03_105郡国历史开发参考索引.xlsx",
    "04_1182县历史开发参考索引.xlsx",
    "05_77战略城市历史开发参考索引.xlsx",
    "06_135-260历史人物地理分布开发参考.xlsx",
    "07_135-260历史宗族地理分布开发参考.xlsx",
    "13_135-260重大历史事件区域影响参考.xlsx",
    "历史资料来源总索引.xlsx",
]


def require(condition: bool, message: str):
    if not condition:
        raise AssertionError(message)


def main():
    workdata = json.loads((OUT / "historical_world_reference_workdata.json").read_text(encoding="utf-8"))
    require(workdata["coverage"] == EXPECTED, f"coverage mismatch: {workdata['coverage']}")
    require(len(list((DOC / "02_PROVINCES").glob("*.md"))) == 13, "province document count")
    city_docs = list((DOC / "05_CITIES").glob("*.md"))
    require(len(city_docs) == 77, "city document count")
    require(sum("CITY-S" in p.name for p in city_docs) == 8, "CITY-S document count")
    require(len(list((DOC / "14_SCENARIOS").glob("*.md"))) == 13, "scenario document count")
    require(len(list((DOC / "15_TEMPLATES").glob("*.md"))) == 5, "template count")
    for cid in ["C027", "C031", "C009", "C025", "C067", "C041", "C043", "C056"]:
        matches = list((DOC / "05_CITIES").glob(f"{cid}_*_CITY-S_*.md"))
        require(len(matches) == 1, f"missing CITY-S {cid}")
        require("source.web." in matches[0].read_text(encoding="utf-8"), f"CITY-S source missing {cid}")

    for filename in WORKBOOKS:
        path = DOC / filename
        require(path.exists() and path.stat().st_size > 5000, f"workbook missing/empty: {filename}")
        require(zipfile.is_zipfile(path), f"invalid xlsx zip: {filename}")
        with zipfile.ZipFile(path) as archive:
            names = set(archive.namelist())
            require("xl/workbook.xml" in names, f"workbook.xml missing: {filename}")
            xml = archive.read("xl/workbook.xml").decode("utf-8")
            require("说明" in xml and "数据" in xml, f"required sheets missing: {filename}")

    build_report = json.loads((OUT / "workbook_build_report.json").read_text(encoding="utf-8"))
    require(len(build_report) == 8, "workbook build report count")
    require(all(x["formulaErrors"] == 0 for x in build_report), "formula errors reported")
    require(len(list((OUT / "previews").glob("*.png"))) == 16, "preview count")
    require(len(list((OUT / "inspections").glob("*.inspect.ndjson"))) == 8, "inspection count")

    report = {
        "schema": "mandate.historical-world-reference-validation.v1",
        "status": "PASS",
        "coverage": EXPECTED,
        "markdown_counts": {"provinces": 13, "cities": 77, "city_s": 8, "scenarios": 13, "templates": 5},
        "workbooks": len(WORKBOOKS),
        "rendered_previews": 16,
        "formula_errors": 0,
        "runtime_changed": False,
    }
    (OUT / "validation_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
