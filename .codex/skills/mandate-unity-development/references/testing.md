# Testing

`AGENTS.md` is authoritative for timeouts and process ownership. This reference defines test evidence and task-appropriate coverage.

## Standard sequence

For code changes, run:

1. whole-solution compilation;
2. the core regression runner;
3. one controlled Unity test;
4. `git diff --check`.

Use `scripts/verify-project.ps1` for the standard sequence.

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

## Unity evidence

- Invoke only `Tools/Run-UnityTestsSafe.ps1`.
- Preserve the emitted PID, log path, and result path.
- A project lock caused by the user's open Unity editor is `blocked`, not `failed`.
- On timeout, retain the log tail and identify the owned process tree that was stopped.
- If Unity creates no startup log before the safe runner's startup watchdog while running in the
  workspace sandbox, stop that owned process and retry the same safe runner at most once with the
  required sandbox escalation. Do not weaken the process, timeout, log, or XML requirements.

## Minimum coverage

- Domain change: invariant, success path, boundary, and deterministic repeatability.
- Persistence change: current round trip, older-version migration, invalid-version rejection, and invariant validation.
- Simulation change: state transition, resource accounting, failure path, and repeatable results.
- Presentation or serialized Unity change: reference integrity, scene/prefab loading, and relevant EditMode or PlayMode behavior.
- Defect fix: a test that fails before the fix and passes after it.
