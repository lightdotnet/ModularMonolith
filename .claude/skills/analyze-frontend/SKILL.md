---
name: analyze-frontend
description: Playbook for analyzing all of clients/ — which client apps exist, what each is for, and how they relate — without going deep into any single app (use analyze-client for that) or into the backend.
---

# Skill: Analyze Frontend

## Purpose

Build (or refresh) a top-level understanding of the `clients/` folder: which app(s) exist, what each is for, and their shared conventions (if any). The frontend counterpart to [analyze-solution](analyze-solution.md) — this is the "index" pass across all client apps, not a deep dive into one. For a deep dive into a single named app, use [analyze-client](analyze-client.md).

## Inputs

- Usually none beyond "analyze the frontend/clients" — this skill's job is partly to discover how many client apps exist in the first place.

## Workflow

1. **Enumerate apps**: `Glob clients/*/` to find every app folder. Don't assume there's exactly one.
2. **For each app found**, get a lightweight summary (name, stack, purpose) — delegate a deeper pass to [analyze-client](analyze-client.md) only if the user wants per-app detail, not automatically for every app.
3. **Map shared conventions/tooling**: is there a root `clients/package.json`/workspace config (monorepo tooling like Turborepo/pnpm workspaces) shared across apps, or is each app fully independent? Verify, don't assume.
4. **Map backend integration at a glance**: which apps call the backend API, and whether they share an API client package or each maintain their own.
5. **Update docs, if requested**: write/update each app's `clients/<app-name>/CLAUDE.md` and/or root `docs/frontend-overview.md` (the clients index — see [frontend-overview template](../docs/templates/frontend-overview.md)). Only when explicitly asked.

## Expected Outputs

- A list of client apps found under `clients/`, each with a one-line purpose/stack summary.
- Whether apps share tooling/conventions or are fully independent.
- Optionally, updated documentation (only if requested).

## Best Practices

- Don't go deep into any one app's routes/components here — that's [analyze-client](analyze-client.md).
- Don't assume `clients/` has exactly one app just because that's common for a new project — always `Glob` first.
- Don't assume shared conventions across apps until verified (e.g. a shared UI kit or API client package actually referenced by more than one app).
