---
name: mandate-unity-development
description: Develop, diagnose, refactor, test, and deliver Unity/C# changes in the MandateOfHeroes repository. Use for work involving its domain model, world simulation, combat, characters, maps, missions, economy, persistence, ScriptableObjects, scenes, prefabs, editor integration, milestone task documents, compilation, core regression tests, or controlled Unity tests.
---

# MandateOfHeroes Unity Development

Follow the repository's rules and leave evidence for every validation claim.

## Establish authority and scope

1. Locate the repository root from the current workspace.
2. Read `AGENTS.md` completely before taking task actions.
3. Treat `AGENTS.md` and the user's current request as authoritative. Never weaken them with this skill.
4. Read the task's directly related files under `Docs/`.
5. Inspect `git status` before editing. Preserve unrelated and user-authored changes.
6. Search existing production code and tests before designing a new abstraction.
7. State any assumption that changes persistence, architecture, public behavior, or task scope.

Read references conditionally:

- Read [architecture.md](references/architecture.md) to locate code or decide dependency placement.
- Read [testing.md](references/testing.md) before compiling or running tests.
- Read [persistence.md](references/persistence.md) for saves, DTOs, migrations, random behavior, time progression, or world-state changes.
- Read [content-and-data.md](references/content-and-data.md) for ScriptableObjects, static content, historical data, scenes, prefabs, or third-party assets.
- Read [delivery-template.md](references/delivery-template.md) before reporting completion.

## Choose the workflow

- For implementation or fixes: inspect, edit, compile, run core tests, run one controlled Unity test when required, then review the diff.
- For diagnosis only: use read-only inspection and report evidence; do not implement unless asked.
- For document-only changes: edit and run `git diff --check`; record compilation and tests as not run because only documentation changed.
- For review or explanation: inspect and report; do not mutate external systems or the repository unless requested.

## Implement safely

1. Keep domain rules in pure C# where practical.
2. Do not make presentation state the sole source of authoritative game state.
3. Use the project's deterministic random and stable-ID mechanisms.
4. Serialize independent DTO/state structures rather than complex runtime objects.
5. Add a migration and round-trip coverage when changing persisted state.
6. Add a regression test that reproduces a fixed defect.
7. Avoid unrelated formatting, cleanup, or refactors.
8. Do not add code, data, or assets without compatible and recorded licensing.

## Validate in order

For code changes, perform these steps in order:

1. Compile the whole solution.
2. Run the fast core regression suite based on `Tools/CoreTestRunner.cs`.
3. Run one controlled Unity test through `Tools/Run-UnityTestsSafe.ps1`.
4. Run `git diff --check` and inspect the scoped diff.

Use [verify-project.ps1](scripts/verify-project.ps1) as the unified entry point. Use `-SkipUnity` only for document-only work or when Unity is blocked, and record why.

Stop on failure and fix the failing stage before continuing. Do not substitute compilation for testing. Never call a test passed without a result file or explicit test summary.

## Protect long-running tools

1. Never run Unity, a build, or a test tool as an unbounded foreground process.
2. Use a 300-second default hard timeout.
3. Start long tools in the background and poll at intervals no longer than five seconds.
4. Retain the process ID, log path, and result path while polling.
5. On timeout, terminate only the process tree started for the current task and report the log tail.
6. Check for an existing Unity process before batch tests. Report a project-lock conflict and do not close the user's editor.
7. After an interrupted outer call, inspect and clean up only stale, over-time processes created by that task.

## Deliver

Report:

- the outcome and scoped files changed;
- compilation status and evidence;
- core-test status and explicit summary;
- Unity-test status, result path, or blocking evidence;
- known limitations and the most relevant next step.

Do not commit, push, create a pull request, delete user data, close Unity, or expand the task without authorization.
