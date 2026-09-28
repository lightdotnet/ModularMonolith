<!--
Template: Architecture (Backend or one Client App)
Used by: skills/generate-docs/SKILL.md, skills/sync-docs/SKILL.md
Output location: src/docs/architecture/architecture.md or clients/<app-name>/docs/architecture/architecture.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Architecture: <Backend | Client app name>

## Layering

_Backend: layers actually observed per module (Domain/Application/Infrastructure/Api). Client app: route/component/data-layer organization actually observed._

## Dependency Direction

_The conceptual rule and whether it holds (e.g. expected layer order, cross-module/cross-feature boundary verified or violated). Link to [dependency-graph.md](dependency-graph.md) for the full project-reference/import diagram and package list rather than repeating it here — this section explains what the direction means, that file proves it._

## Key Design Patterns

_Patterns actually in use (e.g. CQRS, mediator, repository on the backend; server actions, colocated data fetching in this client app) — only if verified in code._

## Shared Kernel / Common Building Blocks Used

_Which shared/common project(s) or packages this depends on, and how — for a client app, include any package shared with sibling apps under `clients/`._

## Module/Route Boundaries

_Backend: which modules exist and their boundary rules. Client app: which route groups/features exist and how they're isolated._

## Known Architectural Risks / Debt

| Finding | Severity | Notes |
|---|---|---|

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
