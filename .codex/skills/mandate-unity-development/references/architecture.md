# Architecture

## Dependency direction

Keep dependencies directed from presentation and orchestration toward stable domain concepts:

```text
Mandate.Presentation
        |
Mandate.Simulation ---- Mandate.Persistence
        \                 /
           Mandate.Domain
```

Confirm actual `.asmdef` and `.csproj` references before changing dependencies. Do not introduce a reverse dependency from `Mandate.Domain` to Unity-facing code.

## Main assemblies

- `Mandate.Domain`: state, value objects, invariants, stable IDs, deterministic random support, and rules that can run without Unity.
- `Mandate.Simulation`: world progression and systems such as population, education, travel, trade, warfare, medicine, construction, and NPC decisions.
- `Mandate.Persistence`: snapshot serialization, schema validation, and version migration.
- `Mandate.Presentation`: dashboard, map rendering, input, views, and Unity-facing presentation.
- `Mandate.Domain.Tests`: core NUnit regression suite, including `WorldKernelTests`.

## Important locations

- Production C#: `Assets/Scripts/`
- EditMode tests: `Assets/Tests/EditMode/`
- Main prototype scene: `Assets/Scenes/SimulationDashboard.unity`
- Design and milestone documents: `Docs/`
- Deterministic core runner: `Tools/CoreTestRunner.cs`
- Controlled Unity test entry: `Tools/Run-UnityTestsSafe.ps1`
- Generated build output: `Temp/bin/Debug/`

## Placement rules

- Put invariants and state transitions in Domain when they do not require Unity.
- Put multi-entity or time-driven orchestration in Simulation.
- Put save conversion and migration in Persistence.
- Put Unity components, scene wiring, rendering, and input in Presentation.
- Add tests beside the existing EditMode suite and keep core cases runnable outside the Unity editor when possible.
