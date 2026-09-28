<!--
Template: Module Overview (Backend, Modular Monolith)
Used by: skills/analyze-module/SKILL.md, skills/generate-docs/SKILL.md
Output location: src/docs/architecture/modules/<ModuleName>.md for a single-project module (the common case —
  see src/docs/architecture/architecture.md § Layering). Only use the nested
  src/docs/architecture/modules/<ModuleName>/overview.md form for a split module that also has a
  domain-model.md and/or per-project overview.md files alongside it.
Do not populate this file itself — copy its structure into the generated output.
-->

# Module Overview: <ModuleName>

## Purpose

_What business capability this module owns, in business terms._

## Internal Layering

For a split module (`<ModuleName>.Domain`/`.Application`/`.Infrastructure`/`.Api`), use one row per project:

| Project | Responsibility | Notes |
|---|---|---|
| `<ModuleName>.Domain` | | |
| `<ModuleName>.Application` | | |
| `<ModuleName>.Infrastructure` | | |
| `<ModuleName>.Api` | | |

For a single-project module (the common case), replace the table above with one row per internal folder instead (e.g. `Domain/<Feature>/`, `Application/`, `Data/`, `Services/`, `Controllers/`) — see `src/docs/architecture/modules/Location.md`/`Orders.md` for the actual pattern used.

## Public Contract

_Routes exposed by `<ModuleName>.Api`'s controllers (what client apps rely on) and the DTOs/seam interfaces in `<ModuleName>.Contracts` (the only project other modules may reference). Link to [../api.md](../api.md) section if generated._

## Data Access

_This module's `DbContext`, key entities, and whether it shares the physical database with other modules (expected) vs. truly isolated storage._

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| Shared/building-blocks | project | |

## Depended On By

_Other modules or client app(s) that consume this module's public contract. A reference from another module to any project of this module other than its `<ModuleName>.Contracts` is a boundary violation — flag it._

## Notable Conventions

_Anything distinct about this module's conventions vs. other modules._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
