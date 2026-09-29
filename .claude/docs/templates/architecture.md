<!--
Template: Architecture
Used by: skills/generate-docs/SKILL.md, skills/sync-docs/SKILL.md
Output location: src/docs/architecture/architecture.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Architecture

## Layering

_The framework projects actually observed and each one's responsibility (shared kernel, hosting infrastructure, persistence, integration-event bus)._

## Dependency Direction

_The rule and whether it holds (e.g. `Infrastructure → Shared`, `Persistence → Shared`, `Shared` → no solution project, no framework project → business module). Link to [dependency-graph.md](dependency-graph.md) for the full diagram rather than repeating it here._

## Key Design Patterns

_Patterns actually in use (e.g. Result pattern, mediator pipeline behaviours, domain-event dispatch on save, module registration) — only if verified in code._

## Extension Points for Modules

_How a business module plugs into the framework (module registration, controller bases, DbContext base) — verified, not assumed._

## Known Architectural Risks / Debt

| Finding | Severity | Notes |
|---|---|---|

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
