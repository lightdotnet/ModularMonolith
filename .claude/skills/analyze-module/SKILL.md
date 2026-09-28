---
name: analyze-module
description: Playbook for analyzing a single backend module (a flat project or project-group directly under src/, e.g. src/Identity.Api + src/Identity.Contracts) — its internal layering, public contract, and dependencies — without expanding scope to sibling modules or the whole solution.
---

# Skill: Analyze Module

## Purpose

Build (or refresh) an understanding of exactly one Modular Monolith module: its internal layers (either a single project organized by folder, or split into `Domain`/`Application`/`Infrastructure`/`Api` projects), what it exposes publicly via its `.Contracts` seam, and what it depends on. Narrower than [analyze-solution](analyze-solution.md) (the whole backend), broader than [analyze-project](analyze-project.md) (a single layer/project within the module).

## Inputs

- The specific module to analyze — its project(s) directly under `src/` (e.g. `src/<Module>` or `src/<Module>.Api` + `src/<Module>.Contracts`) — ask if not specified.

## Workflow

1. **Locate the module**: find its project(s) directly under `src/` — a single `<Module>`/`<Module>.Api` project, or a split `<Module>.{Domain,Application,Infrastructure,Api}` set — plus its `<Module>.Contracts` seam project.
2. **Map internal layering**: confirm dependency direction within the module (`Api → Application → Domain`, `Infrastructure → Application`/`Domain`) — delegate to [architecture-reviewer](../agents/architecture-reviewer.md) if it needs a full assessment.
3. **Map external dependencies**: delegate to [dependency-analyzer](../agents/dependency-analyzer.md) for what this module references (shared/building-blocks project, NuGet packages) and, importantly, whether it reaches into any other module directly (a boundary violation).
4. **Read its public contract**: the module's `Api` project (routes/DTOs) — this is what other modules and client apps can rely on.
5. **Update docs, if requested**: write/update a scoped entry in `src/CLAUDE.md` (Modules table) and/or `src/docs/architecture/modules/<Name>.md` using the [module-overview template](../docs/templates/module-overview.md). Only when explicitly asked.

## Expected Outputs

- A description of the module's internal layering, public contract, and dependencies (internal and external).
- Explicit flag of any cross-module boundary violation found.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named module; note references to other modules or client apps but don't fully analyze them unless asked.
- Summarize public surface rather than reproducing full source.
- Don't assume a module's internal layering matches another module's until verified — templates can drift as an app grows.
