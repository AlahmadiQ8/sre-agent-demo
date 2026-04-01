# Spec Conventions

Guidelines for reading and executing spec documents in this project.

## Spec Lifecycle

Each spec has a status in its header:

| Status | Meaning |
|--------|---------|
| `COMPLETE ✅` | All tasks finished. **Read-only context** — do not modify code described here unless a newer spec explicitly says to. |
| `IN PROGRESS 🔧` | Active work. Execute tasks in dependency order. |
| `DRAFT 📝` | Not yet approved. Do not implement. |

## How to Read a Spec

1. **Start with the status** — skip COMPLETE specs unless a newer spec references them for context.
2. **Read the implementation plan** — tasks are numbered and grouped into phases with explicit dependencies.
3. **Execute one task at a time** — mark each task done before moving to the next.
4. **Tasks with `t` suffix** (e.g., `3t`) are test tasks — they validate the preceding implementation task.

## Task Format

Each task row contains:

| Column | Purpose |
|--------|---------|
| `#` | Task ID (used for dependency references) |
| `Task` | Short name |
| `Description` | Full implementation details — treat this as the source of truth for what to build |

## Referencing Existing Code

Newer specs should reference existing code by path rather than re-describing it:
- ✅ `Extend src/ContosoBank/Services/ChaosService.cs to add a new TriggerXyz method`
- ❌ `Create a chaos service that...` (it already exists)

## Dependencies Between Specs

When a task in `02-spec.md` depends on code from `01-spec.md`, state it as:
> **Depends on:** `01-spec.md` (complete)

This tells the implementer to read the existing code rather than re-build it.

## Spec File Naming

```
specs/
  00-conventions.md    ← this file (never changes)
  01-spec.md           ← original feature set (COMPLETE)
  02-spec.md           ← next feature set
  03-spec.md           ← ...
```
