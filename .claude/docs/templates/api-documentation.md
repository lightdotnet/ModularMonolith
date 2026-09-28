<!--
Template: API Documentation
Used by: skills/generate-docs/SKILL.md, skills/sync-docs/SKILL.md
Output location: src/docs/architecture/api.md
Do not populate this file itself — copy its structure into the generated output.
-->

# API Documentation: Backend

## API Type

_REST/JSON, API-only controllers — verified per module, not assumed._

## Versioning Strategy

_How this API versions changes (e.g. URL segment, header)._

## Endpoints by Module

| Module | Route | Method | Description | Since version |
|---|---|---|---|---|

## Request/Response Contracts

_Key DTOs/contracts and their shape, per module._

## Error Contract

_How errors are represented (status codes, error DTO shape) — should be consistent across modules._

## Authentication / Authorization

_How clients authenticate, what authorization model applies per endpoint._

## Client Consumption

| Client app | API client location | Hand-written or generated? |
|---|---|---|
| | `clients/<app-name>/src/modules/*/api/<feature>.api.ts` via `src/lib/server/backend-api.ts` (or equivalent) | |

Drift between this contract and each client is checked by the api-contract-reviewer agent.

## Breaking Change History

| Version | Change | Client migration notes (per affected app) |
|---|---|---|

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: <date>_
