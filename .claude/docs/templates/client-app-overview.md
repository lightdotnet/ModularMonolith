<!--
Template: Client App Overview
Used by: skills/analyze-client/SKILL.md, agents/nextjs-architect.md
Output location: clients/<app-name>/docs/architecture/overview.md
Do not populate this file itself — copy its structure into the generated output.
-->

# Client App Overview: <app-name>

## Purpose

_What this specific client app is for (e.g. the primary end-user web app, an internal admin console)._

## Structure

- **Router**: _App Router (`app/`) or Pages Router (`pages/`) — verified, not assumed_
- **Package manager**: _verified_
- **Data fetching approach**: _server components / server actions / React Query / SWR / other — verified_
- **State management**: _verified_
- **Styling**: _verified_

## Key Routes/Areas

| Route/Area | Path | Responsibility | Notes |
|---|---|---|---|

## Backend Integration

_This app's API client layer (e.g. `src/modules/*/api/<feature>.api.ts` over the named clients in `src/lib/server/backend-api.ts`), how it's generated/maintained, and which backend modules it calls. Link to [../../../../src/docs/architecture/api.md](../../../../src/docs/architecture/api.md) if generated. See root [docs/integration.md](../../../../docs/integration.md)._

## Auth Flow

_How this app authenticates against the backend (cookies/JWT/session), and where tokens are stored._

## External Dependencies

_Notable third-party npm packages this app uses and why._

## Relationship to Other Client Apps

_Does this app share code/tooling with sibling apps under `clients/`, or is it fully independent? Only note what's verified._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
