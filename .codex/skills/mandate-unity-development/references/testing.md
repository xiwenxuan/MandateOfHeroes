# Testing

`AGENTS.md` is authoritative for timeouts and process ownership. This reference defines test evidence and task-appropriate coverage.

## Standard sequence

For code changes, run:

1. whole-solution compilation;
2. the core regression runner;
3. one controlled Unity test;
4. `git diff --check`.

Use `scripts/verify-project.ps1` for compilation plus a targeted core and Unity smoke. The current complete
core and EditMode suites exceed a reliable single-process workflow, so complete regression evidence uses the
grouped runners documented below rather than one unbounded unified invocation.

On Windows, the unified verifier creates a case-insensitive child-process environment and collapses duplicate
`Path`/`PATH` variants to the longest value before launching MSBuild or Roslyn. This is process-local: it does not
change user or machine environment variables. The compile stage supports Visual Studio 2022 Build Tools and uses
an installed complete dotnet SDK as the `Microsoft.NET.Sdk` fallback for Unity-generated project files. When the
project-locked editor is installed, the targeted core runner uses that editor's Unity Mono runtime so Unity package
assemblies are not incorrectly rejected by Windows CLR strong-name validation.

## Standard invocation

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1
```

For document-only changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1 `
  -DocumentationOnly
```

Use `-SkipUnity` only when Unity integration is not required or is blocked. State the reason in the delivery report. Do not use it to hide a Unity-facing regression.

## Result vocabulary

- `passed`: the command exited successfully and produced its required summary or result.
- `failed`: the command completed with a nonzero exit code or reported failing tests.
- `blocked`: environmental state, such as an open Unity editor, prevented execution.
- `not run`: the task category explicitly did not require the stage.

Do not infer Unity success from its process exit alone. Require its test-result XML. Do not infer core-test success without a `RESULT passed=N failed=0` summary.

## Complete core regression groups

The core suite can exceed the 300-second boundary when executed in one process. Prepare one immutable run,
execute each group as a separate bounded invocation, and aggregate only after all groups finish:

```powershell
$runId = "manual-20260805"
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-CoreTestGroupsSafe.ps1 `
  -RunId $runId -GroupCount 12 -PrepareOnly -TimeoutSeconds 300
1..12 | ForEach-Object {
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-CoreTestGroupsSafe.ps1 `
    -RunId $runId -GroupCount 12 -GroupIndex $_ -TimeoutSeconds 300
}
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-CoreTestGroupsSafe.ps1 `
  -RunId $runId -GroupCount 12 -AggregateOnly
```

Preparation compiles the solution and runner, discovers tests from the actual test assembly, and writes source
and binary fingerprints. Each group requires an exact test-name set. Aggregation rejects changed sources or
binaries, missing groups, missing tests, unexpected tests and duplicates. For Codex-controlled work, invoke one
group per bounded external call and retain the per-group logs and JSON. A complete core claim requires the final
aggregate `total=N passed=N failed=0`; a filtered runner result is only targeted evidence.

The 2026-08-05 M25-P21 baseline discovered 364 core tests. Twelve groups passed 364/364 (31 tests in groups
1-4 and 30 tests in groups 5-12). The slowest group took approximately 155.4 seconds, so current complete core
groups retain the project-wide 300-second hard limit. Its final aggregate is under
`tmp/core-test-groups/m25p21-final4-20260805/aggregate.json`.

`verify-project.ps1 -CoreTestFilter <substring-or-exact-filter>` is the bounded targeted-core option. Exact
multi-test filters use `exact:name1;name2`. Omitting the filter retains the legacy all-in-one core behavior and
must not be used as complete evidence when it times out; use the grouped aggregate instead.

## Unity evidence

- Invoke only `Tools/Run-UnityTestsSafe.ps1`.
- Preserve the emitted PID, log path, and result path.
- A project lock caused by the user's open Unity editor is `blocked`, not `failed`.
- On timeout, retain the log tail and identify the owned process tree that was stopped.
- If Unity creates no startup log before the safe runner's startup watchdog while running in the
  Codex workspace sandbox, stop that owned process and retry the same safe runner at most once with
  the required sandbox escalation. The 2026-08-05 controlled comparison confirmed this boundary:
  the sandboxed `EngineSmoke` produced no log in 45 seconds, while the same safe runner passed outside
  the sandbox in 16.153 seconds. Do not weaken the process, timeout, log, or XML requirements.

### Layered Unity diagnostics

`Tools/Run-UnityTestsSafe.ps1` supports explicit modes. Use the smallest mode that can answer the
current question:

```powershell
# Unity engine/licensing startup and normal exit.
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityTestsSafe.ps1 `
  -Mode EngineSmoke -TimeoutSeconds 60

# Real project load, package resolution and compilation without Test Runner.
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityTestsSafe.ps1 `
  -Mode ProjectLoadSmoke -TimeoutSeconds 120

# One exact EditMode smoke test with required XML.
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityTestsSafe.ps1 `
  -Mode EditModeTests `
  -TestFilter Mandate.Tests.WorldKernelTests.WorldScheduler_OrdersStableIdsAndHonorsCadence `
  -TimeoutSeconds 120
```

Every run writes a Unity log, stdout/stderr evidence notes and a JSON summary under
`tmp/unity-validation`. Test modes additionally require a non-empty, parseable NUnit XML. Result codes
distinguish project lock, launch failure, compilation failure, invalid XML, total timeout, startup timeout,
missing XML and actual test failure.

On this Unity 2022.3 China build, Test Runner can finish and write complete XML but keep consuming CPU during
native editor shutdown. The safe runner allows a bounded natural-exit grace period, then may terminate only its
owned Unity process tree. Such a run is `passed` only when the XML is complete, reports zero failures and the
JSON records `forcedCleanupAfterResult=true`; an absent or incomplete XML remains `blocked` or `failed`.

### Complete EditMode regression groups

The current EditMode suite is too large to rely on one near-300-second process. Generate a unique run ID and
execute each group as a separate external invocation:

```powershell
$runId = "manual-20260805"
1..24 | ForEach-Object {
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityEditModeGroupsSafe.ps1 `
    -RunId $runId -GroupCount 24 -GroupIndex $_ -UseGraphics -TimeoutSeconds 300
}
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnityEditModeGroupsSafe.ps1 `
  -RunId $runId -GroupCount 24 -AggregateOnly
```

For Codex-controlled work, do not put all groups into one unbounded foreground call. Run one group per bounded
external invocation, preserve its PID/log/XML, and aggregate only after every group has completed. The manifest
uses a source fingerprint and exact test-name sets, so stale or mixed group results cannot satisfy aggregation.
Discovery maps each `[Test]` method to its containing public test class; files with multiple test classes must
therefore produce their real Unity full names rather than assigning every method to the first class in the file.
The group runner accepts 1-32 groups. Increase the group count instead of the timeout when suite growth or a
particular distribution places a group near the absolute 300-second limit. The M25-P21 baseline passed 365/365
in 16 groups, but groups 14 and 15 took approximately 289 and 291 seconds. M25-P22 therefore uses 24 groups
after the enlarged 370-test suite caused a 16-group distribution to exceed the same limit. Retain the
300-second hard limit and never extend it without explicit user approval.

When an exact test still exceeds 300 seconds after it has been isolated from its group, classify it explicitly
before using the user's approved exception. The safe runners accept `-TimeoutClass SlowDeterminism` with a
maximum of 900 seconds only for an allowlisted exact long-horizon deterministic replay. Standard runs remain
capped at 300 seconds, and the JSON summary plus group metadata record the timeout class and matched exact test.
For the current suite, the classified tests are
`Mandate.Tests.WorldKernelTests.Simulation_SaveResumeMatchesContinuousRun`, which executes a 730-day continuous
simulation and a 365-day save/resume path, and
`Mandate.Tests.WorldKernelTests.FoodRuntime_FormalWorldIsDeterministicForOneYear`, which advances two formal
food worlds for one year and compares their complete deterministic state, and
`Mandate.Tests.WorldKernelTests.IntegratedOneYearStabilityTests_FormalWorldHasNoEconomicInvariantFailure`, which
executes the formal one-year integrated economy invariant gate and was measured just above the ordinary 300-second
ceiling; `Mandate.Tests.WorldKernelTests.LuoyangLiving_365DayCropAndConservationRemainStable`,
`Mandate.Tests.WorldKernelTests.LuoyangT4_OneSevenThirtyOneYearThreeYearSixYearRemainValid`, and
`Mandate.Tests.WorldKernelTests.OuterAgricultureLongRunTests_AllRecordsRunForOneWorldYearWithoutDuplicateHarvest`
are the explicit 365-day, six-year, and one-year Luoyang long-horizon gates. In the 1102-test / 32-group suite,
their immutable group was still actively advancing deterministic world time when the standard runner terminated it
at 300.508 seconds. A grouped rerun may use the exception only when at least one classified exact test is in the
selected immutable group.

When the maximum 32-way distribution still places a deterministic Unity asset builder together with other
integration tests just above 300 seconds, use the separate `-TimeoutClass AssetBuildIntegration` exception.
It is capped at 600 seconds and requires an allowlisted exact Builder test in the immutable group; it must not be
used for ordinary logic or performance tests. The current classified Builder is
`Mandate.Tests.EditMode.LuoyangP0NativePrefabArtDeliveryV1Tests.BuildAssets_CreatesFourReplaceableThreeLodPrefabs`.

Use `-TimeoutClass AssetManifestIntegration` only when the maximum distribution combines the complete 240-file,
38-FBX FinalRemaining manifest validation with other bounded integration tests and exceeds 300 seconds. This
class is capped at 600 seconds and requires
`Mandate.Tests.EditMode.LuoyangRemainingFinalAssetV1Tests.SourceManifest_Freezes240FilesAnd38ValidatedFbxSources`
in the immutable group.

The 2026-08-05 M25-P28 core baseline discovered and passed 401/401 tests in 32 groups. In that distribution,
group 24 took about 141 seconds and group 27 about 185 seconds; keep these long-running groups in separate
external invocations. The aggregate evidence is
`tmp/core-test-groups/m25p28-final-20260805/aggregate.json`.

`verify-project.ps1 -UnityTestFilter <exact-name>` is an explicit smoke option. Omitting the filter preserves
the existing complete-test behavior; never present a filtered verification as the complete EditMode suite.

## Minimum coverage

- Domain change: invariant, success path, boundary, and deterministic repeatability.
- Persistence change: current round trip, older-version migration, invalid-version rejection, and invariant validation.
- Simulation change: state transition, resource accounting, failure path, and repeatable results.
- Presentation or serialized Unity change: reference integrity, scene/prefab loading, and relevant EditMode or PlayMode behavior.
- Defect fix: a test that fails before the fix and passes after it.
