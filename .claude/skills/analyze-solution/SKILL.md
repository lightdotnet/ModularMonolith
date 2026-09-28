---
name: analyze-solution
description: Playbook for analyzing the backend solution (src/, one .sln) — its modules, projects, and dependencies.
---

# Skill: Analyze Solution

## Purpose

Build (or refresh) an understanding of the backend solution under `src/`: its modules, how they relate, and what each is for. This repo has one backend solution — scope is the whole backend, not a single module (use [analyze-module](analyze-module.md) for that) or a single project (use [analyze-project](analyze-project.md)).

## Inputs

- Confirmation that the request is about the backend (`src/`), not a client app (`clients/` — use [analyze-frontend](analyze-frontend.md) for the whole `clients/` folder, or [analyze-client](analyze-client.md) for one named app).

## Workflow

1. **Locate the solution**: find the `.sln` file under `src/` and enumerate the modules/projects it actually includes.
2. **Map dependencies**: delegate to [dependency-analyzer](../agents/dependency-analyzer.md) to build the real project/package dependency graph, flagging any cross-module boundary violations.
3. **Understand structure**: delegate to [architecture-reviewer](../agents/architecture-reviewer.md) if a structural/layering assessment is also wanted; otherwise just describe what's observed.
4. **Read minimally**: open only the files needed to describe each module's responsibility (e.g. namespaces, key public types) — not every file in every module.
5. **Update docs, if requested**: if the user wants this persisted, write/update `src/CLAUDE.md` (Modules table) and/or `src/docs/architecture/overview.md` using the [solution-overview template](../docs/templates/solution-overview.md). Do this only when explicitly asked.

## Expected Outputs

- A description of the backend solution: its modules, their responsibilities, and how they depend on each other.
- A dependency graph/table for the solution.
- Optionally, updated documentation (only if requested).

## Best Practices

- Prefer delegating deep dependency/architecture work to the relevant agent rather than doing it all inline.
- Don't assume every module follows the same internal layering until verified — Modular Monolith modules can still drift.
- If the user actually wants one module, redirect to [analyze-module](analyze-module.md) instead of doing a full-solution pass.
