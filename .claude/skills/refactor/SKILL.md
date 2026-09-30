---
name: refactor
description: Playbook for safely refactoring scoped code — the framework, host, and module projects under src/ (or their tests), or the client app under clients/admin/ — without changing observable behavior or breaking the public API of the framework, a module's .Contracts, or the HTTP contract the client consumes.
---

# Skill: Refactor

## Purpose

Restructure existing code for clarity/maintainability without changing behavior, with explicit care around the seams that matter here: the public API of the framework projects (`Shared`, `Infrastructure`, `Persistence`, `EventBusMassTransitRabbitMQ`), which every module depends on; a module's `.Contracts` project (e.g. `Identity.Contracts`), which other modules build against; and a module's HTTP routes and request/response shapes, which the client app under `clients/admin/` consumes.

## Inputs

- The specific code/project/client-app area to refactor and the motivation (e.g. duplication, unclear structure, outdated pattern).
- Confirmation of scope: which project(s) or client-app area, and whether any public type/member or API route/shape would change.

## Workflow

1. **Confirm scope and motivation** — avoid refactoring "while you're in there" beyond what was asked.
2. **Check public-API and contract impact**: determine whether any public signature, base type, or extension method in a framework project or a module's `.Contracts` would change, or any route/request/response shape the client calls. If yes, flag it as a breaking change for consuming modules or the client (check client call sites with [api-contract-reviewer](../../agents/api-contract-reviewer.md)) and confirm with the user before proceeding.
3. **Note the baseline**: identify existing tests covering the target — in `tests/Framework.Tests` (framework projects) or the module's `tests/<Module>.Tests` (e.g. `tests/Identity.Tests`, `tests/Notifications.Tests`) — without running them yet; if coverage looks thin, say so in the plan; characterization tests may need to be added first. The client app has no test suite, so state that a client refactor is verified by lint/build and manual review only.
4. **Present a plan and wait for explicit approval** before touching code (root `CLAUDE.md` §2.9). If the refactor reshapes a DDD building block, run [ddd-modeler](../../agents/ddd-modeler.md) first so the target shape is deliberate.
5. **Refactor incrementally**, delegating the edits once approved to [dotnet-developer](../../agents/dotnet-developer.md) (backend) or [nextjs-developer](../../agents/nextjs-developer.md) (client app): small steps, each verified by building (not by running the test suite).
6. **Present the refactored code back to the user for review.** Stop here.
7. **Verify no behavior change, once asked**: when the user explicitly requests it, run the tests (adding characterization tests first if the area was undertested).

## Expected Outputs

- An approved refactor plan, agreed before any code was touched.
- Refactored code with unchanged observable behavior (unless explicitly agreed otherwise), presented back for review.
- An explicit breaking-change flag if any public API or API contract changed.

## Best Practices

- Never mix refactoring with new functionality in the same change.
- Treat any public signature change in a framework project or a module's `.Contracts`, and any route/shape change the client consumes, as breaking until proven otherwise.
- Prefer the smallest refactor that achieves the stated goal.
