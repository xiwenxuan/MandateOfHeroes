---
name: mandate-unity-development
description: Develop, diagnose, review, plan, test, and deliver Unity/C# work in the MandateOfHeroes repository. Use for domain, world simulation, combat, characters, maps, missions, economy, production, construction, research, governance, permanent population, persistence, content data, ScriptableObjects, scenes, prefabs, editor integration, milestone documents, compilation, core regression tests, or controlled Unity tests.
---

# MandateOfHeroes Unity Development

Use this skill as the project's execution workflow. `AGENTS.md` remains the authoritative repository rule set.

## Start

1. Locate the repository root and read `AGENTS.md` completely.
2. Inspect `git status` and preserve unrelated or user-authored changes.
3. Classify the request as read-only review/diagnosis, documentation, or implementation/fix.
4. Read [task-routing.md](references/task-routing.md), then load only the relevant design documents and references. Use `Docs/GAME_SYSTEMS_MASTER_AND_STATUS.md` whenever the task decides system status, cross-system boundaries, production/construction/research rules, or the next development milestone.
5. Inspect existing production code and tests before proposing a new abstraction.
6. State assumptions that change persistence, architecture, public behavior, or task scope.

## Load references

- Always use [task-routing.md](references/task-routing.md) for substantive project work.
- Read [architecture.md](references/architecture.md) before choosing code placement or dependencies.
- Read [testing.md](references/testing.md) before compiling or running tests.
- Read [persistence.md](references/persistence.md) for saves, DTOs, migrations, stable identity, deterministic random, time, permanent population, or partitioned storage.
- Read [content-and-data.md](references/content-and-data.md) for historical content, assets, ScriptableObjects, scenes, prefabs, and serialized Unity content.
- Read [delivery-template.md](references/delivery-template.md) before reporting completion.

## Execute

- For review or explanation, inspect and report without modifying the repository.
- For diagnosis, gather evidence and explain the cause; implement only when requested.
- For documentation work, keep edits scoped and run documentation-mode validation.
- For implementation or fixes, make the smallest coherent change and add proportionate tests.
- Keep hard invariants in `AGENTS.md`; do not duplicate or weaken them in task documents.
- Do not infer priority from milestone numbers. The user's current request and active plan determine priority.

## Validate

Use the unified verification entry point:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1
```

For documentation-only work:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File `
  .codex\skills\mandate-unity-development\scripts\verify-project.ps1 `
  -DocumentationOnly
```

Use `-SkipUnity` only when Unity integration is not required or is environmentally blocked, and report the reason. Stop at a failed stage; never merge `passed`, `failed`, `blocked`, and `not run` into one claim.

## Deliver

Follow [delivery-template.md](references/delivery-template.md). Report the outcome, scoped files, compilation, core tests, Unity tests, diff validation, limitations, and the most relevant next step.

Do not commit, push, create a pull request, delete user data, close Unity, or expand the task without authorization.
