# LUOYANG-184-URBAN-INITIALIZATION-V1 AUDIT

## Status

**PASSED**

## Evidence

- Package integrity: 10/10 manifest-listed files verified before audit summary finalization.
- Permanent persons: 270,000/270,000; Historical=25; GeneratedHistoricalPopulation=269,975; test/stress origins=0.
- Households: 53,992; member coverage=270,000; no overlap or gap.
- Facilities: 1,230 audited; residential capacity/occupancy=270,000/270,000; worker capacity/occupancy=160,000/154,962.
- Historical runtime: 25 internal anchors + 3 explicit outside/unknown anchors; no second PersonId namespace.
- Forces: 5 definitions, 34,000 exact person memberships.
- Events: 10 ordered definitions; Person, Force, work-pause, military-supply and transport effects verified.
- EditMode: 5/5 passed (`tmp\unity-validation\unity-EditMode-20260810-074009-971.summary.json`).
- PlayMode: 1/1 passed (`tmp\unity-validation\unity-PlayMode-20260810-074043-647.summary.json`).
- Full compile: passed. Filtered Luoyang core regression: passed.

## Performance

- Generator core build: 1923.024ms.
- Serialized 270K persons: 21,600,032 bytes.
- Estimated 400K persons: 32,000,032 bytes.
- Unity daily audit tick: 165.965ms.
- Unity monthly household tick: 11.213ms.
- Visual actor cap: 256.
- Chunk size: 4096.
- 700K auto-generation: disabled.

## Accepted reconciliation

- The rounded 166,000 available-labour baseline resolves to 165,982 actual people after preserving 18 palace dependent children and two 70+ non-labour dependants.
- Actual employed population is 154,962; actual age-eligible unemployed population is 11,020. No child or 70+ dependant was fabricated as unemployed merely to match a rounded macro total.
- 04 and 06 audit tables are sharded into three 90,000-row detail workbooks behind their required main index workbooks because the mandated spreadsheet tool exceeded 12GB on a 270,000-row monolith. Runtime and identity data remain fully materialized.
