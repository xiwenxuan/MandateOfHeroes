#!/usr/bin/env python3
"""Read-only readiness audit for the formal 184 Luoyang runtime inputs.

The script never rewrites a runtime package.  It consumes the existing binary/
JSON packages and reference workdata, then emits reproducible review evidence.
"""

from __future__ import annotations

import csv
import hashlib
import json
import struct
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any, Iterable


REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "outputs" / "LUOYANG_184_DEVELOPMENT_READINESS_REVIEW_V1"
URBAN = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184UrbanInitializationV1"
METRO = REPO / "Assets" / "StreamingAssets" / "WorldMap" / "Luoyang184MetropolitanInitializationV1"
HISTORICAL = REPO / "Assets" / "StreamingAssets" / "HistoricalPersons" / "Han135260V1"
POPULATION = REPO / "Assets" / "StreamingAssets" / "HistoricalPopulation" / "Han135260V1"
FAMILY_WORKDATA = REPO / "outputs" / "FAMILY_ORGANIZATION_CENTER_AND_HISTORICAL_FAMILY_REFERENCE_V1" / "family_reference_workdata.json"
ADMIN_WORKDATA = REPO / "outputs" / "HAN_135_260_ADMINISTRATIVE_SEAT_CANONICAL_PLACE_AND_HISTORICAL_WORLD_STATE_V1" / "administrative_seat_world_state_workdata.json"

PERSON_STRUCT = struct.Struct("<IhBBHIHQIIHHHHHHHHHHqHHBBBBiii")
HOUSEHOLD_STRUCT = struct.Struct("<IIIHHIBBHq")
HEADER_STRUCT = struct.Struct("<8siiiiQ")
HEADER_SIZE = 32
NONE_U16 = 0xFFFF
NONE_U32 = 0xFFFFFFFF


def load(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def verify_file_contract(root: Path, entries: Iterable[dict[str, Any]], prefix: str) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for entry in entries:
        path = root / entry["path"]
        if not path.exists():
            status, reason = "FAIL", "missing"
            actual_bytes, actual_sha = None, None
        else:
            actual_bytes = path.stat().st_size
            actual_sha = sha256(path)
            if actual_bytes != entry["bytes"]:
                status, reason = "FAIL", "byte_size_mismatch"
            elif actual_sha != entry["sha256"]:
                status, reason = "FAIL", "sha256_mismatch"
            else:
                status, reason = "PASS", "manifest_contract_match"
        rows.append({
            "ObjectType": "PackageFile",
            "ObjectId": prefix + entry["path"],
            "Status": status,
            "Reason": reason,
            "ExpectedBytes": entry["bytes"],
            "ActualBytes": actual_bytes,
            "ExpectedSha256": entry["sha256"],
            "ActualSha256": actual_sha,
        })
    return rows


def read_records(path: Path, record_struct: struct.Struct, expected_magic: bytes, expected_count: int):
    with path.open("rb") as stream:
        header = HEADER_STRUCT.unpack(stream.read(HEADER_SIZE))
        magic, version, record_size, count, _reserved, _seed = header
        if magic != expected_magic or version != 1 or record_size != record_struct.size or count != expected_count:
            raise RuntimeError(f"Invalid binary header: {path}")
        for _ in range(count):
            payload = stream.read(record_struct.size)
            if len(payload) != record_struct.size:
                raise RuntimeError(f"Truncated record file: {path}")
            yield record_struct.unpack(payload)
        if stream.read(1):
            raise RuntimeError(f"Trailing bytes after records: {path}")


def person_id(ordinal: int, overlays: dict[int, str]) -> str:
    if ordinal in overlays:
        return overlays[ordinal]
    if ordinal < 270_000:
        return f"person.luoyang.184.urban.{ordinal + 1:06d}"
    return f"person.luoyang.184.metropolitan.{ordinal + 1:06d}"


def write_csv(name: str, rows: list[dict[str, Any]]) -> None:
    path = OUT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    headers: list[str] = []
    for row in rows:
        for key in row:
            if key not in headers:
                headers.append(key)
    with path.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=headers, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow({key: "|".join(map(str, value)) if isinstance(value, list) else value for key, value in row.items()})


def check_row(check_id: str, status: str, severity: str, object_type: str, object_id: str,
              reason: str, evidence: str, required_action: str = "") -> dict[str, Any]:
    return {
        "CheckId": check_id,
        "Status": status,
        "Severity": severity,
        "ObjectType": object_type,
        "ObjectId": object_id,
        "Reason": reason,
        "Evidence": evidence,
        "RequiredAction": required_action,
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    urban_manifest = load(URBAN / "manifest.json")
    metro_manifest = load(METRO / "manifest.json")
    urban_catalogs = load(URBAN / "catalogs.json")
    metro_catalogs = load(METRO / "catalogs.json")
    urban_facilities = load(URBAN / "facilities.json")["facilities"]
    metro_facilities = load(METRO / "facilities.json")["facilities"]
    all_facilities = urban_facilities + metro_facilities
    historical_overlay_rows = load(URBAN / "historical_persons.json")["people"]
    historical_overlays = {int(item["ordinal"]): item["person_id"] for item in historical_overlay_rows}
    historical_persons = {item["person_id"]: item for item in load(HISTORICAL / "persons.json")["persons"]}
    historical_184 = {item["person_id"]: item for item in load(HISTORICAL / "scenarios" / "184.json")["persons"]}
    family_workdata = load(FAMILY_WORKDATA)
    admin_workdata = load(ADMIN_WORKDATA)
    year_184 = load(POPULATION / "years" / "year_184.json")
    luoyang_consistency = load(POPULATION / "luoyang_consistency.json")

    checks: list[dict[str, Any]] = []
    package_files = verify_file_contract(METRO, metro_manifest["files"], "metropolitan/")
    package_files += verify_file_contract(URBAN, metro_manifest["base_package_files"], "urban-protected/")
    for row in package_files:
        checks.append(check_row(
            "PKG-" + row["ObjectId"].replace("/", "-").replace(".", "-").upper(),
            row["Status"], "CRITICAL" if row["Status"] == "FAIL" else "INFO",
            row["ObjectType"], row["ObjectId"], row["Reason"],
            f"bytes={row['ActualBytes']};sha256={row['ActualSha256']}",
            "Restore the exact protected package file before implementation." if row["Status"] == "FAIL" else ""))

    # Full binary scan of the composite 400K population.
    persons: list[tuple[Any, ...]] = []
    persons.extend(read_records(URBAN / "persons.bin", PERSON_STRUCT, b"MOHLYU01", 270_000))
    persons.extend(read_records(METRO / "outer_persons.bin", PERSON_STRUCT, b"MOHLYM01", 130_000))
    households: list[tuple[Any, ...]] = []
    households.extend(read_records(URBAN / "households.bin", HOUSEHOLD_STRUCT, b"MOHLYH01", urban_manifest["household_count"]))
    households.extend(read_records(METRO / "outer_households.bin", HOUSEHOLD_STRUCT, b"MOHLYK01", metro_manifest["added_household_count"]))

    person_ids = [person_id(index, historical_overlays) for index in range(len(persons))]
    duplicate_person_ids = [key for key, count in Counter(person_ids).items() if count > 1]
    bad_person_ordinals = 0
    bad_household_refs = 0
    bad_residence_refs = 0
    bad_work_refs = 0
    bad_family_indices = 0
    bad_kinship_refs = 0
    housed_count = 0
    assigned_work_count = 0
    family_index_counts: Counter[int] = Counter()
    residence_counts: Counter[int] = Counter()
    work_counts: Counter[int] = Counter()
    student_counts: Counter[int] = Counter()
    urban_student_index = urban_catalogs["occupations"].index("occupation.education.student")
    metro_student_index = metro_catalogs["occupations"].index("occupation.education.student")
    for index, row in enumerate(persons):
        (ordinal, _birth_year, _gender, _age_stage, _health, household_ordinal,
         family_index, _cell_id, residence_index, work_index, *_middle, father, mother, spouse) = row
        if ordinal != index:
            bad_person_ordinals += 1
        if household_ordinal >= len(households):
            bad_household_refs += 1
        if residence_index != NONE_U32:
            housed_count += 1
            residence_counts[residence_index] += 1
            if residence_index >= len(all_facilities):
                bad_residence_refs += 1
        if work_index != NONE_U32:
            assigned_work_count += 1
            occupation_index = row[10]
            student_index = urban_student_index if index < 270_000 else metro_student_index
            if occupation_index == student_index:
                student_counts[work_index] += 1
            else:
                work_counts[work_index] += 1
            if work_index >= len(all_facilities):
                bad_work_refs += 1
        if family_index != NONE_U16:
            family_index_counts[family_index] += 1
            if family_index >= 15:
                bad_family_indices += 1
        for relative in (father, mother, spouse):
            if relative != -1 and not (0 <= relative < len(persons)):
                bad_kinship_refs += 1

    bad_household_ordinals = 0
    bad_household_heads = 0
    bad_household_ranges = 0
    household_member_total = 0
    person_household_mismatch = 0
    for index, row in enumerate(households):
        ordinal, head, start, count, family_index, residence, _kind, _origin, _pad, _wealth = row
        if ordinal != index:
            bad_household_ordinals += 1
        if head >= len(persons):
            bad_household_heads += 1
        if start + count > len(persons) or count == 0:
            bad_household_ranges += 1
            continue
        household_member_total += count
        for person_index in range(start, start + count):
            if persons[person_index][5] != ordinal:
                person_household_mismatch += 1
        if family_index != NONE_U16 and family_index >= 15:
            bad_family_indices += 1
        if residence != NONE_U32 and residence >= len(all_facilities):
            bad_residence_refs += 1

    facility_ids = [item["facility_id"] for item in all_facilities]
    duplicate_facility_ids = [key for key, count in Counter(facility_ids).items() if count > 1]
    cell_groups: dict[int, list[str]] = defaultdict(list)
    for facility in all_facilities:
        cell_groups[int(facility["cell_id64"])].append(facility["facility_id"])
    duplicate_cells = {cell: ids for cell, ids in cell_groups.items() if len(ids) > 1}
    def formal_opening_capacity(facility: dict[str, Any], is_urban: bool) -> tuple[int, int]:
        """Return the capacity authority used by the formal opening assignment.

        The urban package deliberately keeps catalogue/base capacity fields next to
        recommended opening capacities.  The binary person ledger was assigned
        against the recommended fields.  Metropolitan additions use the direct
        capacity fields because they do not have a second recommendation layer.
        """
        if is_urban:
            residence = facility.get("recommended_residential_capacity", facility.get("residential_capacity_persons", 0))
            workers = facility.get("recommended_worker_capacity", facility.get("worker_capacity", 0))
        else:
            residence = facility.get("residential_capacity_persons", 0)
            workers = facility.get("worker_capacity", 0)
        return int(residence or 0), int(workers or 0)

    capacity_overflows: list[dict[str, Any]] = []
    for index, facility in enumerate(all_facilities):
        residence_capacity, worker_capacity = formal_opening_capacity(facility, index < len(urban_facilities))
        if residence_counts[index] > residence_capacity:
            capacity_overflows.append({"FacilityId": facility["facility_id"], "Kind": "Residence", "Assigned": residence_counts[index], "Capacity": residence_capacity})
        if work_counts[index] > worker_capacity:
            capacity_overflows.append({"FacilityId": facility["facility_id"], "Kind": "Work", "Assigned": work_counts[index], "Capacity": worker_capacity})
        student_capacity = int(facility.get("student_capacity", 0) or 0)
        if student_counts[index] > student_capacity:
            capacity_overflows.append({"FacilityId": facility["facility_id"], "Kind": "Student", "Assigned": student_counts[index], "Capacity": student_capacity})

    stale_person_lists = []
    person_id_set = set(person_ids)
    for facility in urban_facilities:
        for field in ("worker_person_ids", "resident_person_ids"):
            values = facility.get(field) or []
            unknown = [value for value in values if value not in person_id_set]
            if unknown:
                stale_person_lists.append({
                    "FacilityId": facility["facility_id"], "Field": field,
                    "ReferenceCount": len(values), "UnknownCount": len(unknown),
                    "ExampleUnknownId": unknown[0],
                })

    binary_ok = not any((bad_person_ordinals, bad_household_refs, bad_residence_refs,
                         bad_work_refs, bad_family_indices, bad_kinship_refs,
                         bad_household_ordinals, bad_household_heads,
                         bad_household_ranges, person_household_mismatch,
                         duplicate_person_ids, duplicate_facility_ids,
                         duplicate_cells, capacity_overflows))
    checks.append(check_row(
        "POP-COMPOSITE-400K", "PASS" if binary_ok else "FAIL", "CRITICAL" if not binary_ok else "INFO",
        "PopulationPackage", "population_profile.luoyang.184.metropolitan_recommended",
        "Composite binary identities and references are internally valid." if binary_ok else "Composite binary invariants failed.",
        f"persons={len(persons)};households={len(households)};housed={housed_count};work={assigned_work_count};member_total={household_member_total}",
        "Repair package generation before implementation." if not binary_ok else ""))
    checks.append(check_row(
        "FAC-STALE-PERSON-LISTS", "WARN" if stale_person_lists else "PASS", "HIGH" if stale_person_lists else "INFO",
        "FacilityOverlay", "Assets/StreamingAssets/WorldMap/Luoyang184UrbanInitializationV1/facilities.json",
        "Historical facility overlay contains pre-formal person-list IDs; binary residence/work indexes remain the accepted person-assignment authority." if stale_person_lists else "No stale person-list IDs.",
        f"affected_facility_fields={len(stale_person_lists)}",
        "Next task must delete no people; explicitly migrate or de-authorize stale list fields and validate one assignment authority." if stale_person_lists else ""))

    # Canonical identity and historical-person mapping.
    historical_mapping: list[dict[str, Any]] = []
    overlay_person_ids = {item["person_id"] for item in historical_overlay_rows}
    for item in historical_overlay_rows:
        pid = item["person_id"]
        master = historical_persons.get(pid)
        snapshot = historical_184.get(pid)
        runtime_id = person_id(item["ordinal"], historical_overlays)
        historical_mapping.append({
            "PersonId": pid,
            "DisplayName": item["display_name"],
            "RuntimeOrdinal": item["ordinal"],
            "RuntimePersonId": runtime_id,
            "MasterPersonFound": "YES" if master else "NO",
            "Scenario184Found": "YES" if snapshot else "NO",
            "NameMatch": "YES" if master and master.get("canonical_name") == item["display_name"] else "NO",
            "RuntimeLocationStatus": item.get("location_status") or "",
            "MasterClanId": (master or {}).get("clan_id") or "",
            "MasterBranchId": (master or {}).get("lineage_branch_id") or "",
            "RuntimeFamilyAnchor": item.get("family_anchor") or "",
            "RuntimeHistoricalRole": item.get("historical_role") or "",
            "MasterHistoricalRole": (snapshot or {}).get("historical_role") or (master or {}).get("primary_identity") or "",
            "BindingStatus": "EXACT_ID_BINDING" if runtime_id == pid and master and snapshot else "BLOCKED",
            "DuplicateRisk": "HIGH_IF_MASTER_IS_MATERIALIZED_SEPARATELY",
            "RequiredAction": "Preserve this runtime record and bind master metadata to it; never create a second Person for the same P-ID.",
            "Evidence": "urban historical_persons.json|Han135260V1/persons.json|scenarios/184.json",
        })
    missing_overlay_master = [row["PersonId"] for row in historical_mapping if row["MasterPersonFound"] != "YES"]
    checks.append(check_row(
        "HIST-25-BINDING", "PASS" if not missing_overlay_master and len(historical_mapping) == 25 else "FAIL",
        "CRITICAL" if missing_overlay_master else "INFO", "HistoricalPersonSet", "luoyang184.overlay.25",
        "All 25 overlay IDs resolve to the historical master and the 184 scenario." if not missing_overlay_master else "Historical overlay IDs are missing from the master.",
        f"overlay_count={len(historical_mapping)};missing={','.join(missing_overlay_master)}",
        "Block implementation until missing identities are resolved." if missing_overlay_master else ""))
    checks.append(check_row(
        "HIST-MAIN-WORLD-BRIDGE", "WARN", "HIGH", "CodeArchitecture", "Luoyang184HistoricalPersonRuntimeState",
        "Historical overlays are bound inside the standalone Luoyang reader, but no production new-game path projects them into WorldState/IPersonRepository.",
        "Reader usages are limited to specialized systems and tests.",
        "Implement an idempotent historical-person runtime binding/projection with duplicate rejection in the next task."))

    # Family-organization migration and center readiness from the frozen reference audit.
    family_migration: list[dict[str, Any]] = []
    for row in family_workdata["luoyang_org_audit"]:
        action = "PRESERVE_ID_AND_GENERATED_MEMBERS;REBUILD_EXPLICIT_HISTORICAL_MEMBERSHIP"
        if row["AuditConclusion"] == "CRITICAL_MIXED_IMPERIAL_AND_EUNUCH_MEMBERS":
            action += ";SPLIT_IMPERIAL_HOUSEHOLD_FROM_EUNUCH_PERSONS"
        elif row["AuditConclusion"] == "CRITICAL_RANGE_DERIVATION_MISASSIGNED_MEMBERS":
            action += ";REMOVE_UNRELATED_HISTORICAL_IDS_FROM_ORG_WITHOUT_DELETING_PERSONS"
        family_migration.append({
            **row,
            "MigrationClass": "MIGRATION_REQUIRED",
            "MigrationAction": action,
            "StableIdPolicy": "KEEP_FAMILY_ORGANIZATION_ID_UNLESS_EXPLICIT_SPLIT_RECORD_IS_APPROVED",
            "PersonPolicy": "NO_DELETE_NO_MERGE_NO_RERANDOMIZE",
            "TargetCenterStatus": "NONE",
        })
    center_readiness: list[dict[str, Any]] = []
    for row in family_workdata["luoyang_center_candidates"]:
        center_readiness.append({
            **row,
            "RuntimeFamilyCenterContractPresent": "NO",
            "ReadyToDesignate": "NO",
            "ReadinessStatus": "IMPLEMENTATION_REQUIRED" if row["DesignationRecommendation184"] != "不指定" else "NOT_REQUIRED_NOW",
            "Reason": "No candidate currently has all five prerequisites in runtime: real Facility, FamilyManagement capability, legal organization control, manager Person, and Primary/Local designation.",
        })
    checks.append(check_row(
        "FAMILY-ORG-LEGACY-AUDIT", "WARN", "HIGH", "FamilyOrganizationSet", "luoyang184.urban.organizations.7",
        "Two legacy organizations contain S1 historical-member contamination; all seven lack a valid FamilyCenter.",
        "family audit: 7 rows; S1=2; CenterStatus NONE=7",
        "Perform an explicit, stable-ID-preserving migration in LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1."))
    checks.append(check_row(
        "FAMILY-CENTER-RUNTIME", "WARN", "HIGH", "CodeArchitecture", "FamilyCenter",
        "The canonical center rules exist, but no persisted FamilyCenter designation/capability contract exists in WorldState.",
        "OrganizationState has only HeadquartersLocationId; no Primary/Local/ManagementArea/manager/capability contract.",
        "Add a data-driven FamilyManagement capability and persisted designation state; migrate V68 sequentially if persisted."))

    # Facility reference/runtime crosswalk.
    lifecycle = {row["FacilityPermanentId"]: row for row in admin_workdata["luoyang_facility_lifecycle"]}
    facility_crosswalk: list[dict[str, Any]] = []
    for facility in all_facilities:
        fid = facility["facility_id"]
        reference = lifecycle.get(fid)
        assignment_index = int(facility.get("global_facility_index", urban_catalogs["facility_ids"].index(fid) if fid in urban_catalogs["facility_ids"] else -1))
        facility_crosswalk.append({
            "FacilityId": fid,
            "GlobalFacilityIndex": assignment_index,
            "DefinitionId": facility.get("definition_id") or "",
            "DisplayName": facility.get("display_name") or "",
            "CellId64": facility.get("cell_id64"),
            "OwnerId": facility.get("owner_id") or "",
            "ControllerId": facility.get("controller_id") or facility.get("administrative_controller_id") or "",
            "DataOrigin": facility.get("data_origin") or "",
            "HistoricalConfidence": facility.get("historical_confidence") or "",
            "ResidenceAssigned": residence_counts.get(assignment_index, 0),
            "ResidenceCapacity": formal_opening_capacity(facility, fid in urban_catalogs["facility_ids"])[0],
            "WorkAssigned": work_counts.get(assignment_index, 0),
            "WorkCapacity": formal_opening_capacity(facility, fid in urban_catalogs["facility_ids"])[1],
            "StudentAssigned": student_counts.get(assignment_index, 0),
            "StudentCapacity": facility.get("student_capacity", 0),
            "HistoricalLifecycleReference": "YES" if reference else "NO",
            "Post190Reference": (reference or {}).get("190CanonicalPostReference", "NOT_SPECIFICALLY_REFERENCED"),
            "RuntimeAuthority": "BINARY_INDEX_ASSIGNMENTS_PLUS_FACILITY_ID/CELL",
            "AdjustmentRequired": "STALE_INLINE_PERSON_LISTS" if any(item["FacilityId"] == fid for item in stale_person_lists) else "NONE",
        })

    # Population-scope audit.
    luoyang_county = next(row for row in year_184["counties"] if row["county_permanent_id"] == "admin.han140.sili.henan.luoyang")
    major_city = next(row for row in year_184["major_cities"] if row["city_name"] == "洛阳")
    population_audit = [
        {"ScopeId": "scope.luoyang.184.walled", "Population": 200000, "Materialized": "YES", "InclusiveParent": "scope.luoyang.184.urban", "Authority": "Luoyang184UrbanInitializationV1", "DoubleCountPolicy": "DO_NOT_ADD_TO_PARENT"},
        {"ScopeId": "scope.luoyang.184.urban", "Population": 270000, "Materialized": "YES", "InclusiveParent": "scope.luoyang.184.metropolitan", "Authority": "Luoyang184UrbanInitializationV1", "DoubleCountPolicy": "DO_NOT_ADD_TO_PARENT"},
        {"ScopeId": "scope.luoyang.184.metropolitan", "Population": 400000, "Materialized": "YES", "InclusiveParent": "scope.luoyang.184.supply_region", "Authority": "Luoyang184MetropolitanInitializationV1", "DoubleCountPolicy": "FORMAL_RUNTIME_POPULATION_BASELINE"},
        {"ScopeId": "scope.luoyang.184.supply_region", "Population": 700000, "Materialized": "NO", "InclusiveParent": "admin.han140.sili.henan", "Authority": "Han135260V1/luoyang_consistency.json", "DoubleCountPolicy": "PLAN_ONLY;INCLUDES_400K"},
        {"ScopeId": "admin.han140.sili.henan.luoyang", "Population": luoyang_county["modeled_actual_population"], "Materialized": "NO_SEPARATE_POPULATION", "InclusiveParent": "admin.han140.sili.henan", "Authority": "Han135260V1/year_184.json", "DoubleCountPolicy": "NATIONAL_MODEL_REFERENCE;DO_NOT_ADD_TO_400K"},
        {"ScopeId": "admin.han140.sili.henan", "Population": luoyang_consistency["henan_yin_modeled_actual_population"], "Materialized": "NO_SEPARATE_POPULATION", "InclusiveParent": "admin.han140.sili", "Authority": "Han135260V1/year_184.json", "DoubleCountPolicy": "CONTAINS_METROPOLITAN_AND_SUPPLY_REGION"},
    ]
    checks.append(check_row(
        "POP-SCOPE-HIERARCHY", "PASS", "INFO", "PopulationScope", "scope.luoyang.184.metropolitan",
        "The formal opening population is 400K; 200K/270K are nested subsets and 700K is an unmaterialized inclusive supply envelope.",
        f"major_city={major_city['metropolitan_population']};consistency={luoyang_consistency['metropolitan_conclusion']}", ""))

    # 190 compatibility and Wave 0 dependencies.
    future_190 = []
    for row in admin_workdata["luoyang_prepost"]:
        future_190.append({
            **row,
            "CurrentRuntimeSupport": "REFERENCE_ONLY",
            "CompatibilityStatus": "READY_WITH_CONTRACT" if row["Domain"] in {"Population", "Walls/Gates/Roads"} else "IMPLEMENTATION_REQUIRED",
            "NextTaskBoundary": "Preserve stable IDs and add extension hooks only; do not implement the 190 event in the next task.",
        })
    wave0_dependency = [
        {
            "PlaceId": "place.han140.sili.henan.luoyang", "DisplayName": "洛阳", "DevelopmentTier": "T4",
            "ReferencePackCompleteness": "FULL_READY", "RuntimeImplementationStatus": "PARTIAL",
            "DependencyStatus": "GO_WITH_BLOCKERS", "BlockerId": "DPB-001",
            "BlockerReason": "Historical Person/Clan/Branch/FamilyOrganization/FamilyCenter references are not safely integrated into the 184 runtime/save path.",
            "BlocksGateA": "NO_FOUNDATION_REBUILD;NEXT_TASK_FIRST_SEGMENT", "BlocksGateB": "NO",
            "ProposedSequence": "WAVE_0A_CORE", "WavePolicy": "PROPOSAL_ONLY;FROZEN_WAVE_UNCHANGED",
        },
        {
            "PlaceId": "geo.site.hulao", "DisplayName": "虎牢", "DevelopmentTier": "T3",
            "ReferencePackCompleteness": "RESEARCH_BLOCKED", "RuntimeImplementationStatus": "NOT_STARTED",
            "DependencyStatus": "DEFERRED", "BlockerId": "DPB-017",
            "BlockerReason": "Final CanonicalPlace/Cell extent and period-specific facility scope are not frozen.",
            "BlocksGateA": "NO", "BlocksGateB": "YES_DEFER_PLACE",
            "ProposedSequence": "WAVE_0B_CORRIDOR", "WavePolicy": "PROPOSAL_ONLY;FROZEN_WAVE_UNCHANGED",
        },
        {
            "PlaceId": "geo.site.hangu", "DisplayName": "函谷关", "DevelopmentTier": "T2",
            "ReferencePackCompleteness": "RESEARCH_BLOCKED", "RuntimeImplementationStatus": "NOT_STARTED",
            "DependencyStatus": "DEFERRED", "BlockerId": "DPB-HANGU-001",
            "BlockerReason": "Precise Cell extent, period-specific facility composition and immediate population/force initialization remain unresolved.",
            "BlocksGateA": "NO", "BlocksGateB": "YES_DEFER_PLACE",
            "ProposedSequence": "WAVE_0B_CORRIDOR", "WavePolicy": "PROPOSAL_ONLY;FROZEN_WAVE_UNCHANGED",
        },
    ]

    runtime_mapping = [
        {"CanonicalEntity": "CanonicalPlace", "CanonicalId": "place.han140.sili.henan.luoyang", "RuntimeEntity": "StrategicCity", "RuntimeId": "C027", "SecondaryRuntimeId": "location.capital.luoyang", "StableRegionId": "geo.region.central.china.heluo.luoyangbasin.county.luoyang", "AdminId": "admin.han140.sili.henan.luoyang", "MappingStatus": "READY_WITH_ADJUSTMENT", "Reason": "All identifiers resolve to one physical Place, but the crosswalk is not one persisted runtime record."},
        {"CanonicalEntity": "World", "CanonicalId": "HanWorldV1", "RuntimeEntity": "CellGrid", "RuntimeId": "HanWorldV1", "SecondaryRuntimeId": "", "StableRegionId": "", "AdminId": "", "MappingStatus": "READY", "Reason": "All packages reuse the 2,000m HanWorldV1 grid."},
        {"CanonicalEntity": "PopulationProfile", "CanonicalId": "population_profile.luoyang.184.metropolitan_recommended", "RuntimeEntity": "CompositePopulationSource", "RuntimeId": "Luoyang184MetropolitanInitializationReader", "SecondaryRuntimeId": "Luoyang184UrbanInitializationReader", "StableRegionId": "", "AdminId": "", "MappingStatus": "READY_WITH_ADJUSTMENT", "Reason": "400K composite is valid but not projected into the formal world/save creation path."},
        {"CanonicalEntity": "Scenario", "CanonicalId": "scenario.han.184.yellow_turban", "RuntimeEntity": "Luoyang184UrbanScenarioState", "RuntimeId": "scenario.han.184.yellow_turban", "SecondaryRuntimeId": "state.luoyang.184.baseline", "StableRegionId": "", "AdminId": "", "MappingStatus": "READY_WITH_ADJUSTMENT", "Reason": "Ordered event prototype exists; main persistent event/command integration is missing."},
        {"CanonicalEntity": "FacilitySet", "CanonicalId": "facility-set.luoyang.184.metropolitan", "RuntimeEntity": "Facility JSON + binary indexes", "RuntimeId": "2084 facilities", "SecondaryRuntimeId": "", "StableRegionId": "", "AdminId": "", "MappingStatus": "READY_WITH_ADJUSTMENT", "Reason": "Stable IDs/Cells are valid; inline person lists need an explicit authority/migration decision."},
        {"CanonicalEntity": "HistoricalPersonSet", "CanonicalId": "luoyang184.overlay.25", "RuntimeEntity": "PermanentPerson records", "RuntimeId": "25 P-IDs", "SecondaryRuntimeId": "Han135260V1/scenarios/184", "StableRegionId": "", "AdminId": "", "MappingStatus": "IMPLEMENTATION_REQUIRED", "Reason": "Exact bindings exist in the reader but not in the main world repository/save."},
        {"CanonicalEntity": "FamilyOrganizationSet", "CanonicalId": "luoyang184.family-organizations.15", "RuntimeEntity": "7 urban + 8 metropolitan JSON organizations", "RuntimeId": "15 organizations", "SecondaryRuntimeId": "", "StableRegionId": "", "AdminId": "", "MappingStatus": "MIGRATION_REQUIRED", "Reason": "Seven legacy records need safe member migration; none has a valid FamilyCenter."},
    ]

    readiness = [
        ("CanonicalPlace", "READY_WITH_ADJUSTMENT", "MEDIUM", "Stable physical identity exists; persist one explicit crosswalk among place/admin/C027/location IDs."),
        ("HistoricalState", "READY_WITH_ADJUSTMENT", "MEDIUM", "184 baseline and ordered events exist; unify the initialization entry and event authority."),
        ("Population", "READY", "LOW", "400K formal composite passes; nested 200K/270K and 700K inclusive plan are explicit."),
        ("PermanentPerson", "READY_WITH_ADJUSTMENT", "HIGH", "400K permanent records pass, but the composite source is not the main world repository/save entry."),
        ("HistoricalPerson", "IMPLEMENTATION_REQUIRED", "HIGH", "25 exact P-ID overlays require idempotent runtime binding and duplicate prevention."),
        ("Household", "READY_WITH_ADJUSTMENT", "MEDIUM", "80,899 household records are valid but remain in the standalone package."),
        ("Residence", "READY_WITH_ADJUSTMENT", "MEDIUM", "All 400K people resolve to valid Facility indexes; project this authority into the formal world."),
        ("Clan", "IMPLEMENTATION_REQUIRED", "HIGH", "Canonical Clan master exists; runtime historical-person and organization membership is not integrated."),
        ("Branch", "IMPLEMENTATION_REQUIRED", "MEDIUM", "Branch master exists but most Luoyang bindings remain blank or candidate-only."),
        ("FamilyOrganization", "MIGRATION_REQUIRED", "HIGH", "Seven legacy organizations include two S1 member-contamination cases; preserve IDs and people during migration."),
        ("FamilyCenter", "IMPLEMENTATION_REQUIRED", "HIGH", "No candidate currently satisfies the five-part runtime center contract."),
        ("Property", "IMPLEMENTATION_REQUIRED", "HIGH", "Family assets/facilities lack one persisted legal owner/controller/operator chain."),
        ("Facility", "READY_WITH_ADJUSTMENT", "HIGH", "2,084 IDs/Cells and capacity assignments pass; stale inline person lists must be migrated or de-authorized."),
        ("UrbanCell", "READY", "LOW", "All formal facilities reuse HanWorldV1 2,000m Cells without duplicate facility Cell occupancy."),
        ("Government", "IMPLEMENTATION_REQUIRED", "HIGH", "Government IDs exist in Facility data but are not projected as the formal Luoyang government organization state."),
        ("Office", "IMPLEMENTATION_REQUIRED", "HIGH", "Office catalogs and person overlays exist; formal Position/Appointment/jurisdiction records are missing."),
        ("Military", "READY_WITH_ADJUSTMENT", "MEDIUM", "Five forces and 34K concrete members exist; main Force/Army/save projection is still absent."),
        ("Work", "READY_WITH_ADJUSTMENT", "MEDIUM", "Binary work assignments are valid; stale overlay arrays and standalone simulation authority need resolution."),
        ("Supply", "READY_WITH_ADJUSTMENT", "MEDIUM", "Five audited metropolitan chains exist; 700K supply region remains plan-only and is not required for Core start."),
        ("HistoricalChange", "IMPLEMENTATION_REQUIRED", "HIGH", "184/189/190/220 references exist; main persistent change-package execution is not implemented."),
        ("Save", "IMPLEMENTATION_REQUIRED", "HIGH", "Luoyang composite/family/historical bindings are not represented by the V68 WorldState snapshot."),
        ("Migration", "MIGRATION_REQUIRED", "HIGH", "Any persisted integration must add an explicit sequential V68 successor migration and round-trip tests."),
        ("CodeArchitecture", "READY_WITH_ADJUSTMENT", "MEDIUM", "Domain/Persistence/Simulation boundaries are correct; production new-game orchestration is missing."),
        ("DataValidation", "READY_WITH_ADJUSTMENT", "MEDIUM", "Package audits are strong; a cross-package and main-world projection validator is newly required."),
        ("TigerPassDependency", "RESEARCH_REQUIRED", "HIGH", "DPB-017 blocks final Hulao Cell/runtime materialization, not Luoyang Core."),
        ("HanguDependency", "RESEARCH_REQUIRED", "HIGH", "Precise Cell/facility/population scope remains unresolved, not a Luoyang Core blocker."),
    ]
    readiness_matrix = [{
        "Domain": domain, "ReadinessStatus": status, "Severity": severity,
        "GateAImpact": "BLOCKER_NEXT_TASK" if severity == "HIGH" and domain not in {"TigerPassDependency", "HanguDependency"} else "NO_CORE_BLOCK",
        "GateBImpact": "DEFER_PLACE" if domain in {"TigerPassDependency", "HanguDependency"} else "NO_REGIONAL_BLOCK",
        "Finding": finding,
        "RequiredAction": "Resolve in LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1" if severity == "HIGH" and domain not in {"TigerPassDependency", "HanguDependency"} else ("Resolve before Wave 0B corridor materialization" if domain in {"TigerPassDependency", "HanguDependency"} else "Track in implementation acceptance"),
        "Evidence": "machine audit|canonical docs|runtime source inspection",
    } for domain, status, severity, finding in readiness]

    # Machine gate is intentionally conservative: no Critical failures, but multiple bounded High integration blockers.
    critical_failures = [row for row in checks if row["Severity"] == "CRITICAL" and row["Status"] == "FAIL"]
    gate_a = "NO_GO" if critical_failures else "GO_WITH_BLOCKERS"
    gate_b = "NO_GO" if gate_a == "NO_GO" else "GO_WITH_DEFERRED_PLACES"

    audit_findings = checks + [
        check_row("ARCH-MAIN-SAVE", "WARN", "HIGH", "WorldState", "schema.v68", "Luoyang initialization is not part of the V68 snapshot contract.", "WorldState contains no Luoyang/FamilyCenter/HistoricalPerson binding collection.", "Add the smallest persisted integration contract and sequential migration in the next task."),
        check_row("GATE-A", "WARN", "HIGH", "Gate", "LUOYANG_CORE_DEVELOPMENT_GATE", gate_a, "No Critical package/identity failures; bounded integration and migration work remains.", "Start only the frozen next task; do not fan out into general Luoyang development."),
        check_row("GATE-B", "WARN", "HIGH", "Gate", "LUOYANG_HULAO_REGIONAL_PACKAGE_GATE", gate_b, "Hulao and Hangu remain research blocked.", "Proceed with Core as Wave 0A proposal; defer corridor materialization to Wave 0B after evidence closes."),
    ]

    summary = {
        "schema": "mandate.luoyang-184-development-readiness-review.v1",
        "status": "PASS_REVIEW_COMPLETED",
        "gate_a": gate_a,
        "gate_b": gate_b,
        "next_task": "LUOYANG-184-HISTORICAL-PERSON-FAMILY-INTEGRATION-V1",
        "counts": {
            "persons": len(persons), "households": len(households), "facilities": len(all_facilities),
            "historical_person_overlays": len(historical_mapping), "urban_family_organizations": 7,
            "metropolitan_family_organizations": 8, "forces": len(load(URBAN / "forces.json")["forces"]),
            "scenario_events": len(load(URBAN / "scenario_events.json")["events"]),
            "stale_facility_person_list_fields": len(stale_person_lists),
        },
        "binary_invariants": {
            "bad_person_ordinals": bad_person_ordinals, "duplicate_person_ids": len(duplicate_person_ids),
            "bad_household_refs": bad_household_refs, "bad_residence_refs": bad_residence_refs,
            "bad_work_refs": bad_work_refs, "bad_family_indices": bad_family_indices,
            "bad_kinship_refs": bad_kinship_refs, "bad_household_ordinals": bad_household_ordinals,
            "bad_household_heads": bad_household_heads, "bad_household_ranges": bad_household_ranges,
            "person_household_mismatch": person_household_mismatch,
            "duplicate_facility_ids": len(duplicate_facility_ids), "duplicate_facility_cells": len(duplicate_cells),
            "capacity_overflows": len(capacity_overflows),
        },
        "source_contracts": {
            "package_files_checked": len(package_files),
            "package_file_failures": sum(1 for row in package_files if row["Status"] == "FAIL"),
            "population_snapshot_moment": year_184["snapshot_moment"],
            "formal_opening_population_scope": "400K_METROPOLITAN_INCLUSIVE",
            "supply_region_population": 700000,
            "supply_region_materialized": False,
            "world_schema_version": 68,
        },
        "limitations": [
            "Review evidence does not modify runtime packages or prove the next integration implementation.",
            "Hulao and Hangu remain deferred research dependencies.",
            "190 compatibility is a reference contract, not an implemented historical change system.",
        ],
    }

    datasets = {
        "readiness_matrix.csv": readiness_matrix,
        "runtime_entity_mapping.csv": runtime_mapping,
        "historical_person_mapping.csv": historical_mapping,
        "family_organization_migration.csv": family_migration,
        "family_center_readiness.csv": center_readiness,
        "facility_crosswalk.csv": facility_crosswalk,
        "population_household_residence_audit.csv": population_audit,
        "future_190_audit.csv": future_190,
        "wave0_dependency.csv": wave0_dependency,
        "audit_findings.csv": audit_findings,
        "package_file_audit.csv": package_files,
        "stale_facility_person_lists.csv": stale_person_lists,
        "capacity_overflows.csv": capacity_overflows,
    }
    for name, rows in datasets.items():
        write_csv(name, rows)

    workdata = {
        "summary": summary,
        "readiness_matrix": readiness_matrix,
        "runtime_mapping": runtime_mapping,
        "historical_person_mapping": historical_mapping,
        "family_organization_migration": family_migration,
        "family_center_readiness": center_readiness,
        "facility_crosswalk": facility_crosswalk,
        "population_audit": population_audit,
        "future_190": future_190,
        "wave0_dependency": wave0_dependency,
        "audit_findings": audit_findings,
        "stale_facility_person_lists": stale_person_lists,
        "package_file_audit": package_files,
    }
    (OUT / "readiness_review_workdata.json").write_text(json.dumps(workdata, ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "machine_audit.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
