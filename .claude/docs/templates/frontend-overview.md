<!--
Template: Frontend Overview (Clients Index)
Used by: skills/analyze-frontend/SKILL.md
Output location: docs/frontend-overview.md (repository root — indexes all apps under clients/, not owned by a single app)
Do not populate this file itself — copy its structure into the generated output.
This is the index across ALL apps under clients/ — for one app's detail, see client-app-overview.md.
-->

# Frontend Overview (Clients Index)

## Purpose

_What the client-side of this repo is for, in one or two sentences. Note explicitly whether there's one app or several._

## Client Apps

| App | Path | Purpose | Stack | Docs |
|---|---|---|---|---|
| | `clients/<app-name>/` | | | [overview](../clients/<app-name>/docs/architecture/overview.md) |

## Shared Tooling Across Apps

_Is there a workspace/monorepo tool (Turborepo, pnpm/yarn workspaces) at the `clients/` root, a shared UI kit, or a shared API client package reused by more than one app? Only note what's verified — apps may be fully independent._

## Backend Integration (summary)

_Which apps call the backend API and how (per-app detail belongs in each app's own overview). Link to [the backend architecture doc](../src/docs/architecture/architecture.md) and [integration.md](integration.md) rather than repeating the contract._

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
