from __future__ import annotations

import json
import zipfile
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1"
DOC = REPO / "Docs" / "FAMILY_ORGANIZATION_REFERENCE_V1"


def fail(message):
    raise SystemExit(f"FAIL: {message}")


data = json.loads((OUT / "family_reference_workdata.json").read_text(encoding="utf-8"))
expected_counts = {
    "action_matrix": 22,
    "clan_spatial": 39,
    "scenario_snapshots": 39 * 13,
    "luoyang_org_audit": 7,
}
for key, count in expected_counts.items():
    if len(data.get(key, [])) != count:
        fail(f"{key} expected {count}, got {len(data.get(key, []))}")

years = sorted({row["Year"] for row in data["scenario_snapshots"]})
if years != [140, 184, 189, 194, 200, 207, 214, 219, 223, 227, 234, 249, 260]:
    fail(f"scenario years mismatch: {years}")
if any(row["InitializationDecision"] != "REFERENCE_ONLY_DO_NOT_INSTANTIATE" for row in data["initialization_reference"]):
    fail("initialization reference contains an instantiating decision")
reference_ids = [row["ReferenceId"] for row in data["initialization_reference"]]
if len(reference_ids) != len(set(reference_ids)):
    fail("initialization reference contains duplicate stable IDs")
if any(row["CenterStatus"] != "NONE" for row in data["luoyang_org_audit"]):
    fail("existing Luoyang organization incorrectly claims a center")
if any(row["ExistingFacilityId"] for row in data["luoyang_center_candidates"]):
    fail("candidate incorrectly claims an existing Facility")
if not any(row["ReviewSet"] == "ADDITIONAL_RESEARCH_CANDIDATE" for row in data["luoyang_people"]):
    fail("Luoyang people set was not expanded beyond the existing 25")

markdown = [
    "01_FamilyOrganization_Clan_Branch_Household_Center关系规范_V1.md",
    "02_FamilyCenter设计规则_V1.md",
    "11_135-260家族空间与FamilyCenter开发参考报告_V1.md",
]
workbooks = [
    "03_FamilyManagement_Action_Matrix_V1.xlsx",
    "04_135-260重要HistoricalClan空间状态参考.xlsx",
    "05_13Scenario_HistoricalFamilySpatialSnapshots.xlsx",
    "06_FamilyOrganizationInitializationReference.xlsx",
    "07_HistoricalResidence_Estate_FamilyAsset_Reference.xlsx",
    "08_184洛阳历史人物与家族空间参考.xlsx",
    "09_184洛阳现有FamilyOrganization一致性审计.xlsx",
    "10_184洛阳FamilyCenter候选与开发建议.xlsx",
]
for name in markdown:
    path = DOC / name
    if not path.exists() or path.stat().st_size < 1000:
        fail(f"missing or undersized markdown: {name}")
for name in workbooks:
    path = DOC / name
    if not path.exists() or path.stat().st_size < 5000:
        fail(f"missing or undersized workbook: {name}")
    if not zipfile.is_zipfile(path):
        fail(f"not a valid xlsx container: {name}")

center_text = (DOC / markdown[1]).read_text(encoding="utf-8")
for token in ("FROZEN", "OPEN_WITH_RECOMMENDATION", "FamilyManagement", "ManagementAreaId", "DISABLED/UNSTAFFED"):
    if token not in center_text:
        fail(f"center rules missing token: {token}")
if center_text.count("| FROZEN |") != 19 or center_text.count("| OPEN_WITH_RECOMMENDATION |") != 1:
    fail("20-question freeze table does not contain 19 FROZEN + 1 OPEN_WITH_RECOMMENDATION")

report = {
    "status": "PASS",
    "counts": {key: len(value) for key, value in data.items()},
    "scenario_years": years,
    "markdown_files": markdown,
    "workbook_files": workbooks,
    "invariants": [
        "Clan does not auto-create FamilyOrganization",
        "Member presence does not prove FamilyCenter",
        "All existing Luoyang V1 organizations have CenterStatus NONE",
        "No runtime nationwide organization/facility materialization",
    ],
}
(OUT / "validation_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
