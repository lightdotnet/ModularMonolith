---
name: analyze-client
description: Playbook for analyzing a single named client app (clients/<app-name>/) — its route/feature structure, data-fetching approach, and dependencies — without expanding scope into other client apps or the backend.
---

# Skill: Analyze Client

## Purpose

Build (or refresh) a deep understanding of exactly one client app: its routes/features, how data flows in from the backend API, and its key dependencies. Narrower than [analyze-frontend](analyze-frontend.md) (the index across all of `clients/`); the frontend counterpart to [analyze-module](analyze-module.md).

## Inputs

- The specific app name/folder under `clients/` to analyze (ask if not specified, or if `clients/` has more than one app and it's not obvious which one the user means).

## Workflow

1. **Locate the app**: confirm `clients/<app-name>/` exists and identify the router style in use (`app/` App Router vs `pages/` Pages Router) and enumerate top-level routes/features.
2. **Map dependencies**: delegate to [dependency-analyzer](../agents/dependency-analyzer.md) for that app's `package.json`/lockfile contents.
3. **Understand structure**: delegate to [nextjs-architect](../agents/nextjs-architect.md) if a structural assessment (data-fetching pattern, state management, component organization) is wanted.
4. **Map backend integration**: identify the API client layer (e.g. `clients/<app-name>/lib/api/`) and which backend endpoints this app actually calls — delegate to [api-contract-reviewer](../agents/api-contract-reviewer.md) if contract drift needs checking.
5. **Read minimally**: open only the files needed to describe the scoped area's responsibility — not every component in the app.
6. **Update docs, if requested**: write/update `clients/<app-name>/CLAUDE.md` and/or `clients/<app-name>/docs/architecture/overview.md` using the [client-app-overview template](../docs/templates/client-app-overview.md). Only when explicitly asked.

## Expected Outputs

- A description of the app's structure, the scoped area's responsibility, and its backend API dependencies.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named app; note references to other client apps or the backend but don't fully analyze them unless asked.
- Don't assume a data-fetching/state-management pattern from one client app applies to another — verify per app.
- Prefer delegating structural and dependency-graph work to the relevant agent rather than doing it all inline.
