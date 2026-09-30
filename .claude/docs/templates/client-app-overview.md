<!--
Template: Client App Overview
Used by: skills/analyze-client/SKILL.md, skills/generate-docs/SKILL.md
Output location: clients/<app-name>/docs/architecture/overview.md
Do not populate this file itself — copy its structure into the generated output.
Layering, dependency direction, and design patterns belong in the app's architecture.md; package references in its dependency-graph.md — link, don't repeat.
-->

# Client App Overview: <app-name>

_One short intro paragraph: what this client app is for, and links to the app's architecture.md and the root [docs/integration.md](../../../../docs/integration.md) for the backend ↔ client boundary._

## Functional Areas

_What the app lets its users do, grouped by area — at stable-structure altitude, not a page census._

## Structure

- **Router**: _App Router (`src/app/`) or Pages Router (`pages/`) — verified, not assumed_
- **Package manager**: _verified_
- **Data fetching approach**: _Server Components / Server Actions / client-side library — verified_
- **State management**: _verified_
- **Styling**: _verified_

## Key Routes/Areas

| Route/Area | Path | Responsibility | Notes |
|---|---|---|---|

## Backend Integration

_This app's API call layer (e.g. `<feature>.api.ts` files over the named backend clients in `src/lib/server/`), how it's maintained (hand-written or generated), and which backend modules it calls. Cross-cutting contract facts live in [docs/integration.md](../../../../docs/integration.md)._

## Auth Flow

_How this app authenticates against the backend (cookies/JWT/session), and where tokens are stored._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
