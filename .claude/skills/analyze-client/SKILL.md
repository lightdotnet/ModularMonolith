---
name: analyze-client
description: Playbook for analyzing a client app under clients/ (today only clients/admin/) — its route/feature structure, data-fetching approach, backend integration, and dependencies — without expanding scope into the backend.
---

# Skill: Analyze Client

## Purpose

Build (or refresh) an understanding of one client app: its routes/features, how data flows in from the backend API, and its key dependencies. The frontend counterpart to [analyze-project](../analyze-project/SKILL.md).

## Inputs

- The app to analyze — `Glob clients/*/` first; today `clients/admin/` is the only one. Ask if more than one exists and the request doesn't say which.
- Optionally, a narrower area (a route or feature) within the app.

## Workflow

1. **Start from verified docs**: read `clients/<app-name>/CLAUDE.md`, then only the files under its `docs/` the request needs — don't re-derive from code what's already documented.
2. **Locate the scope**: enumerate top-level routes under `src/app/` and features under `src/modules/`/`src/features/` — only as deep as the request needs.
3. **Map dependencies**: delegate to [dependency-analyzer](../../agents/dependency-analyzer.md) for the app's `package.json` if dependency detail is wanted.
4. **Assess structure**: delegate to [nextjs-architect](../../agents/nextjs-architect.md) if a structural assessment (data-fetching pattern, state management, component organization) is wanted.
5. **Map backend integration**: identify which `<feature>.api.ts` files and named backend clients (`src/lib/server/backend-api.ts`) the scope uses and which backend endpoints they call — delegate to [api-contract-reviewer](../../agents/api-contract-reviewer.md) if drift needs checking.
6. **Update docs, only if requested**: `clients/<app-name>/docs/architecture/overview.md` follows the [client-app-overview template](../../docs/templates/client-app-overview.md); go through [sync-docs](../sync-docs/SKILL.md) / [generate-docs](../generate-docs/SKILL.md).

## Expected Outputs

- A description of the app's (or the scoped area's) structure, responsibility, and backend API dependencies.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named app; note references to the backend but don't analyze `src/` unless asked.
- Read minimally — open only the files needed to describe the scoped area, not every component.
