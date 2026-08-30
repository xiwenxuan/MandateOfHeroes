# Unity Final Re-Acceptance Evidence

- Unity: `2022.3.62f3c1 (1623fc0bbb97)`
- Project Load Smoke: PASS, 50.334s, `tmp/unity-validation/unity-ProjectLoadSmoke-20260830-053159-419.summary.json`
- EditMode: 9/9 PASS, 215.079s outer duration, `tmp/unity-validation/unity-EditMode-20260830-053320-218.summary.json`
- Graphical PlayMode: 4/4 PASS, 54.415s outer duration, `tmp/unity-validation/unity-PlayMode-20260830-053812-224.summary.json`
- Post-review limited-knowledge EditMode: first sandbox attempt was blocked before startup-log creation; the single permitted retry passed 3/3 in 68.423s, `tmp/unity-validation/unity-EditMode-20260830-094011-290.summary.json`.
- Post-review Supply Card PlayMode: 1/1 PASS in 32.374s, `tmp/unity-validation/unity-PlayMode-20260830-094228-118.summary.json`.
- Ordinary player Supply Card: PASS in PlayMode; projection read leaves deterministic state hash unchanged.
- Player merchant action: PASS in PlayMode; creates a player-directed shipment with cargo in a formal mobile inventory container.
- Presentation performance: 6,291ms initialization, 2 loaded GameObjects, 1,816,477-byte allocation delta, 20 frames averaging 6.328ms, maximum 124.539ms.
- EngineSmoke: not repeated after Project Load, EditMode and graphical PlayMode all entered the same installed Editor successfully. It is a diagnostic probe, not an additional Gate Q requirement.
- User Unity/Hub processes closed: none.

Result: `PASS / ACCEPTABLE FOR V1`.
