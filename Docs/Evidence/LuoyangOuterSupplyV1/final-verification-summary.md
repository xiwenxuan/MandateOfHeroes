# Final Verification Summary

## Result

`ACCEPTED`

All remediation Acceptance Gates A–L passed on the final source state.

| Area | Result |
|---|---|
| Compile | PASS |
| Full core regression | PASS, 799/799, failed 0 |
| Core source fingerprint | `C584F7855DD8A8B5B8F39DBC6E88F113A19471BDED49AE413E92C773BEB11C79` |
| Inclusive population | PASS, 700,000/700,000, gap 0 |
| Permanent households | PASS, 142,980 |
| Outer residence capacity | PASS, 451,487 capacity for 430,000 residents |
| Legacy food definitions | PASS, 3/3 |
| Agriculture scheduling | PASS, 135/135 |
| Agriculture 30 day / 1 year | PASS / PASS |
| Food conservation | PASS, Difference 0 |
| Gate interruption / recovery | PASS |
| Wood regression | PASS |
| V78 save/load | PASS |
| Replay | PASS, 3/3 agriculture and 3/3 gate interruption |
| Unity Project Load | PASS |
| Unity EditMode | PASS, 8/8 |
| Unity PlayMode | PASS, 3/3 with graphics |
| Performance | ACCEPTABLE FOR V1 |
| `git diff --check` | PASS |

The 700,000-person runtime uses compact records and ordinal/household access. The only
`WorldState.People` scans found in the task call chain are explicit read-only catchment
audit/projection construction; they are not per-frame or per-world-tick population
simulation. Daily living settlement uses the compact workforce/household runtime, and
agriculture uses a persistent due index rather than a full farm scan per tick.

The complete grouped core result is stored at
`tmp/core-test-groups/outer-supply-final2-20260829/aggregate.json`. Unity summaries and
logs remain under `tmp/unity-validation`; the stable conclusions are captured in this
tracked evidence directory.
