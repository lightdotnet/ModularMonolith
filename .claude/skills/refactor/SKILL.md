---
name: refactor
description: Playbook for safely refactoring scoped code in the framework projects (src/) or their tests without changing observable behavior or breaking the public API modules build on.
---

# Skill: Refactor

## Purpose

Restructure existing code for clarity/maintainability without changing behavior, with explicit care around the seam that matters here: the public API of the framework projects (`Shared`, `Infrastructure`, `Persistence`, `EventBusMassTransitRabbitMQ`), which every module depends on.

## Inputs

- The specific code/project to refactor and the motivation (e.g. duplication, unclear structure, outdated pattern).
- Confirmation of scope: which project(s), and whether any public type/member would change.

## Workflow

1. **Confirm scope and motivation** — avoid refactoring "while you're in there" beyond what was asked.
2. **Check public-API impact**: determine whether any public signature, base type, or extension method would change. If yes, flag it as a breaking change for consuming modules and confirm with the user before proceeding.
3. **Note the baseline**: identify existing tests in `tests/Framework.Tests` covering the target (don't run them yet) — if coverage looks thin, say so in the plan; characterization tests may need to be added first.
4. **Present a plan and wait for explicit approval** before touching code (root `CLAUDE.md` §2.9). If the refactor reshapes a DDD building block, run [ddd-modeler](../../agents/ddd-modeler.md) first so the target shape is deliberate.
5. **Refactor incrementally**, delegating the edits to [dotnet-developer](../../agents/dotnet-developer.md) once approved: small steps, each verified by building (not by running the test suite).
6. **Present the refactored code back to the user for review.** Stop here.
7. **Verify no behavior change, once asked**: when the user explicitly requests it, run the tests (adding characterization tests first if the area was undertested).

## Expected Outputs

- An approved refactor plan, agreed before any code was touched.
- Refactored code with unchanged observable behavior (unless explicitly agreed otherwise), presented back for review.
- An explicit breaking-change flag if any public API changed.

## Best Practices

- Never mix refactoring with new functionality in the same change.
- Treat any public signature change in a framework project as breaking until proven otherwise.
- Prefer the smallest refactor that achieves the stated goal.
