<!--
Template: Solution Overview (Backend)
Used by: skills/analyze-solution/SKILL.md, workflows/analyze-solution.md
Output location: src/docs/architecture/overview.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Backend Solution Overview

## Purpose

_What this backend is for, in one or two sentences._

## Modules

| Module | Path | Responsibility | Status |
|---|---|---|---|

## Shared/Host Projects

| Project | Path | Responsibility |
|---|---|---|
| Shared/building-blocks | `src/Shared` (verify actual path) | |
| Composition-root host | `src/Host` (verify actual path) | |

## Dependency Graph

_One or two sentences: overall direction and whether any circular reference/cross-module boundary violation was found. Link to [dependency-graph.md](dependency-graph.md) for the full diagram and package list rather than repeating it here._

## Entry Points

_The hosted API and how it's started (`Program.cs` in the host project)._

## Data Access

_DbContext(s) per module, if any. Link to [database.md](database.md) doc if generated._

## External Dependencies

_Notable third-party NuGet packages and why they're used._

## Client Integration

_Which client app(s) under `clients/` consume this backend and how — see [../../../docs/frontend-overview.md](../../../docs/frontend-overview.md) (clients index) and root [docs/integration.md](../../../docs/integration.md)._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
