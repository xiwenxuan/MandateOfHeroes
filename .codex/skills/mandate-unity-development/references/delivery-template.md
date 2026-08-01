# Delivery template

Use this structure and omit empty sections:

```markdown
Outcome:
- What is now implemented or diagnosed.

Changed:
- Important file and behavior changes.
- Working-tree changes deliberately left outside the task.

Validation:
- Full compile: passed/failed/not run; command and summary.
- Core tests: passed/failed/not run; `RESULT passed=N failed=N`.
- Unity tests: passed/failed/blocked/not run; result and log paths.
- Diff check: passed/failed.

Limitations:
- Known gaps, environmental blockers, or compatibility constraints.

Next:
- The single most useful next action, when applicable.
```

Never write “all tests passed” when a required stage was blocked or not run. State whether changes were committed or intentionally left uncommitted.
