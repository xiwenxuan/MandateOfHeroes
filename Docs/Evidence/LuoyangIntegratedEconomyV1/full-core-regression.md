# Full Core Regression

Current runner discovery: 858 tests.

The suite was partitioned because three existing categories exceed a cumulative 300-second foreground window: multi-year Luoyang simulation, multi-seed annual simulation, and full save/resume simulation. The user explicitly authorized a higher limit for legitimately long tests when classified. All ordinary batches retained the 300-second safety limit; only identified exact tests used an extended direct window.

Coverage:

- Regular runner entries excluding `FoodRuntime_FormalWorldIsDeterministicForOneYear`: indices 0–856, 857/857 PASS across bounded batches.
- `FoodRuntime_FormalWorldIsDeterministicForOneYear`: exact 1/1 PASS under the classified extended window.
- Other exact classified long tests, including integrated one-year stability, four five-seed annual groups, and save/resume versus continuous run: PASS; each is also included in the 857 regular-entry coverage accounting.
- Previous integrated scenarios: 22/22 PASS.
- Final exact integrated family: PASS.
- Compile: PASS.
- `git diff --check`: PASS during every bounded verification batch.

Final result: `858/858 PASS`, failures 0, introduced regressions 0.

After the full run, final review narrowed the player projection from all city bindings to public market/government bindings and added an actual unit-price field. This localized change was verified by a fresh compile, the three directly affected core tests (3/3), Unity EditMode (3/3) and Supply Card PlayMode (1/1). The first sandboxed Unity retry produced no startup log and was recorded as an environment-layer block; the single permitted unrestricted retry passed.
