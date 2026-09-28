---
name: refactor
description: Playbook for safely refactoring scoped backend or client-app code in this repo without changing observable behavior or breaking the other side(s) of the stack.
---

# Skill: Refactor

## Purpose

Restructure existing code for clarity/maintainability without changing behavior, with explicit care around the two seams that matter here: a backend module's public contract (its `Api`), and cross-stack usage of it from any app under `clients/`.

## Inputs

- The specific code/module/area to refactor and the motivation (e.g. duplication, unclear structure, outdated pattern).
- Confirmation of scope: one project/module, one client app's area, or does it touch the shared/building-blocks project or an API contract one or more clients depend on?

## Workflow

1. **Confirm scope and motivation**: what's being refactored and why — avoid refactoring "while you're in there" beyond what was asked.
2. **Check contract impact**: if the target is a module's `Api` project or the shared/building-blocks project, determine whether any public signature/route/DTO would change. If yes, flag as a potential breaking change for other modules or any client app that consumes it, and confirm with the user before proceeding.
3. **Note the baseline**: identify existing tests covering the target code (don't run them yet) — if coverage looks thin, flag that in the plan; characterization tests may need to be added first so behavior changes would actually be caught.
4. **Present a plan and wait for explicit approval** before touching code — this gate applies even to refactors, see root `CLAUDE.md` §2.9. If the refactor reshapes a backend module's domain model (aggregate boundaries, where an invariant lives, entity vs. value object), run [ddd-modeler](../agents/ddd-modeler.md) first so the target shape is deliberate, not incidental.
5. **Refactor incrementally**, delegating the edits to [dotnet-developer](../agents/dotnet-developer.md) (backend) or [nextjs-developer](../agents/nextjs-developer.md) (client app) once the plan is approved: small steps, each independently verifiable by building (not by running the full test suite yet) rather than one large rewrite.
6. **Present the refactored code back to the user for review.** Stop here — don't automatically run the test suite next.
7. **Verify no behavior change, once asked**: when the user explicitly requests it, run existing tests (adding characterization tests first if the area was undertested) to confirm nothing observable changed.
8. **Report**: summarize what changed structurally and confirm no observable behavior changed (or explicitly flag what did, if intentional).

## Expected Outputs

- An approved refactor plan, agreed before any code was touched.
- Refactored code with unchanged observable behavior (unless explicitly agreed otherwise), presented back for review before the test suite runs.
- A summary of the structural change and why.
- An explicit breaking-change flag if any module contract or API surface changed.
- Test-suite verification only once the user explicitly asks for it.

## Best Practices

- Never mix refactoring with new functionality in the same change — keep them separable.
- For a module's `Api`/shared building-blocks, treat any public signature change as breaking until proven otherwise.
- Prefer the smallest refactor that achieves the stated goal; don't use a refactor request as license for a broader rewrite.
- Don't skip the plan-approval gate or auto-run the test suite just because a refactor "feels safe" — both still wait for the user.
