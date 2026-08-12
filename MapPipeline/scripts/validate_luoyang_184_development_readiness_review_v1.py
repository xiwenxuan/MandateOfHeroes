#!/usr/bin/env python3
"""Validate the Luoyang 184 readiness-review deliverables.

This validator reads documentation, audit JSON and XLSX ZIP/XML structures. It
does not author spreadsheets or modify runtime packages.
"""

from __future__ import annotations

import json
import zipfile
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1"
DOC = REPO / "Docs" / "HISTORICAL_WORLD_REFERENCE" / "LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1"
SUMMARY_PATH = DOC / "validation_summary.json"

WORKBOOKS = {
    "01_LUOYANG_184_DEVELOPMENT_READINESS_MATRIX.xlsx": 4,
    "02_LUOYANG_RUNTIME_ENTITY_MAPPING_AUDIT.xlsx": 2,
    "03_LUOYANG_HISTORICAL_PERSON_RUNTIME_MAPPING.xlsx": 2,
    "04_LUOYANG_CLAN_FAMILYORGANIZATION_MIGRATION_PLAN.xlsx": 2,
    "05_LUOYANG_FAMILYCENTER_IMPLEMENTATION_READINESS.xlsx": 2,
    "06_LUOYANG_FACILITY_HISTORICAL_REFERENCE_RUNTIME_CROSSWALK.xlsx": 3,
    "07_LUOYANG_POPULATION_HOUSEHOLD_RESIDENCE_AUDIT.xlsx": 2,
    "09_LUOYANG_190_FUTURE_COMPATIBILITY_AUDIT.xlsx": 2,
    "10_LUOYANG_HULAO_WAVE0_DEPENDENCY_REVIEW.xlsx": 2,
}

REQUIRED_DOCS = [
    "LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1_REPORT.md",
    "08_LUOYANG_184_INITIALIZATION_REFERENCE.md",
    "11_NEXT_IMPLEMENTATION_TASK_SCOPE.md",
]


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def validate_xlsx(path: Path, expected_sheets: int) -> dict[str, object]:
    require(path.exists() and path.stat().st_size > 5_000, f"missing/empty workbook: {path}")
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        sheets = [name for name in names if name.startswith("xl/worksheets/sheet") and name.endswith(".xml")]
        require(len(sheets) == expected_sheets, f"sheet count mismatch: {path.name}: {len(sheets)}")
        bad_tokens: list[str] = []
        for name in names:
            if not name.endswith(".xml"):
                continue
            text = archive.read(name).decode("utf-8", errors="ignore")
            for token in ("#REF!", "#DIV/0!", "#VALUE!", "#NAME?", "#NUM!"):
                if token in text:
                    bad_tokens.append(f"{name}:{token}")
        require(not bad_tokens, f"formula error tokens: {path.name}: {bad_tokens[:5]}")
    return {"file": path.name, "bytes": path.stat().st_size, "sheets": expected_sheets, "formula_error_tokens": 0}


def main() -> None:
    audit = json.loads((OUT / "machine_audit.json").read_text(encoding="utf-8"))
    require(audit["status"] == "PASS_REVIEW_COMPLETED", "machine review status")
    require(audit["gate_a"] == "GO_WITH_BLOCKERS", "Gate A contract")
    require(audit["gate_b"] == "GO_WITH_DEFERRED_PLACES", "Gate B contract")
    require(audit["next_task"] == "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1", "next task contract")
    require(all(value == 0 for value in audit["binary_invariants"].values()), "binary invariant failure")
    require(audit["source_contracts"]["package_file_failures"] == 0, "protected package failure")
    require(audit["counts"]["persons"] == 400_000, "person count")
    require(audit["counts"]["households"] == 80_899, "household count")
    require(audit["counts"]["facilities"] == 2_084, "facility count")
    require(audit["counts"]["historical_person_overlays"] == 25, "historical overlay count")

    for name in REQUIRED_DOCS:
        require((DOC / name).exists(), f"missing document: {name}")
    report = (DOC / REQUIRED_DOCS[0]).read_text(encoding="utf-8")
    scope = (DOC / REQUIRED_DOCS[2]).read_text(encoding="utf-8")
    require("GO_WITH_BLOCKERS" in report and "GO_WITH_DEFERRED_PLACES" in report, "report gates")
    for index in range(1, 31):
        require(f"{index}. **" in report, f"report answer {index}")
    require("LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1" in scope, "scope task name")
    require("## OUT_OF_SCOPE" in scope, "scope out-of-scope section")
    require("不得删除、合并或重随机" in report or "不删除、合并、重随机" in report, "permanent person policy")

    workbook_results = [validate_xlsx(DOC / name, sheets) for name, sheets in WORKBOOKS.items()]
    render_manifest = json.loads((OUT / "previews" / "review_workbooks" / "render_manifest.json").read_text(encoding="utf-8"))
    require(len(render_manifest) == 21, "all review workbook sheets rendered")
    for item in render_manifest:
        require(Path(item["preview"]).exists(), f"missing preview: {item['preview']}")
    registry_after = list((OUT / "previews" / "registries" / "after").glob("*.png"))
    require(len(registry_after) == 12, "all updated registry sheets rendered")

    registry_summary = json.loads((OUT / "previews" / "registries" / "registry_update_summary.json").read_text(encoding="utf-8"))
    expected_registry_additions = {
        "PROJECT_DOCUMENT_REGISTRY.xlsx": 13,
        "PROJECT_CANONICAL_DOMAIN_MAP.xlsx": 1,
        "IMPLEMENTATION_GAP_REGISTER.xlsx": 7,
        "RESEARCH_GAP_REGISTER.xlsx": 2,
        "OPEN_DECISION_REGISTRY.xlsx": 1,
        "DOCUMENT_CONFLICT_REGISTER.xlsx": 2,
    }
    for row in registry_summary:
        require(row["added"] == expected_registry_additions[row["file"]], f"registry additions: {row['file']}")
    require("DESIGN_DECISION_REGISTRY.xlsx" not in {row["file"] for row in registry_summary}, "design decisions must remain untouched")

    summary = {
        "schema": "mandate.luoyang-184-development-readiness-review.validation.v1",
        "status": "PASS",
        "validated_on": "2026-08-11",
        "gate_a": "GO_WITH_BLOCKERS",
        "gate_b": "GO_WITH_DEFERRED_PLACES",
        "next_task": "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1",
        "formal_opening_scope": {
            "persons": 400_000,
            "households": 80_899,
            "facilities": 2_084,
            "historical_person_bindings": 25,
            "supply_region_700k_materialized": False,
        },
        "machine_invariants": audit["binary_invariants"],
        "protected_package_files": {
            "checked": audit["source_contracts"]["package_files_checked"],
            "failures": audit["source_contracts"]["package_file_failures"],
        },
        "workbooks": workbook_results,
        "visual_qa": {"review_workbook_sheets_rendered": 21, "updated_registry_sheets_rendered": 12},
        "next_task_blockers": [
            "main-world/new-game/save projection",
            "idempotent historical Person binding",
            "stable-ID FamilyOrganization migration",
            "persisted FamilyCenter five-prerequisite contract",
            "Facility inline-person-list authority migration",
        ],
        "deferred_places": ["geo.site.hulao", "geo.site.hangu"],
        "runtime_code_changed_by_this_task": False,
        "unity_required_for_this_review": False,
    }
    SUMMARY_PATH.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (OUT / "validation_summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
