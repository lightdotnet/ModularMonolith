---
name: analyze-solution
description: Playbook for analyzing the whole .NET solution (StarterKit.slnx at the repo root) — its projects, how they depend on each other, and what each is for.
---

# Skill: Analyze Solution

## Purpose

Build (or refresh) an understanding of the solution as a whole: its projects, how they relate, and what each is for. For a single project use [analyze-project](../analyze-project/SKILL.md) instead.

## Inputs

- A request about the solution as a whole ("analyze the solution/backend/framework").

## Workflow

1. **Locate the solution**: open `StarterKit.slnx` at the repo root and enumerate the projects it actually includes.
2. **Map dependencies**: delegate to [dependency-analyzer](../../agents/dependency-analyzer.md) to build the real project/package dependency graph, flagging direction violations.
3. **Understand structure**: delegate to [architecture-reviewer](../../agents/architecture-reviewer.md) if a structural/layering assessment is also wanted; otherwise just describe what's observed.
4. **Read minimally**: open only the files needed to describe each project's responsibility (namespaces, key public types) — not every file.
5. **Update docs, if requested**: write/update `src/docs/architecture/overview.md` using the [solution-overview template](../../docs/templates/solution-overview.md). Only when explicitly asked.

## Expected Outputs

- A description of the solution: its projects, their responsibilities, and how they depend on each other.
- A dependency graph/table for the solution.
- Optionally, updated documentation (only if requested).

## Best Practices

- Prefer delegating deep dependency/architecture work to the relevant agent rather than doing it all inline.
- If the user actually wants one project, redirect to [analyze-project](../analyze-project/SKILL.md).
