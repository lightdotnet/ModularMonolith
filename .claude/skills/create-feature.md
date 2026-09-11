---
name: create-feature
description: Playbook for adding new functionality that may span a backend module (src/), one or more client apps (clients/), or both, from plan to incremental implementation.
---

# Skill: Create Feature

## Purpose

Guide the addition of new functionality without assuming repo-wide impact, and without skipping planning for anything non-trivial. Most features in this app touch both stacks (a backend endpoint + the client UI that calls it) — plan both sides together so the contract between them is settled before code is written. If the feature needs to reach more than one client app, confirm that explicitly rather than assuming.

## Inputs

- A description of the desired feature/behavior.
- Which side(s) it touches: a new/changed backend module, new/changed UI in one or more named client apps, or both (ask if unclear — including which client app(s), once `clients/` has more than one).
- Any relevant existing documentation for the target scope: `src/CLAUDE.md`/`src/docs/` (backend), `clients/<app-name>/CLAUDE.md`/`clients/<app-name>/docs/` (clients). See the `/context-backend`/`/context-frontend`/`/context-full` commands ([commands/](../commands/)) to load the right set.

## Workflow

1. **Scope**: confirm which module(s) and/or client app(s) the feature touches. If it could plausibly need a new module vs. extending an existing one, ask.
2. **Read minimally**: read only the files/projects directly relevant (target module, its direct dependencies, the relevant client app(s), existing similar features as reference).
3. **Delegate design questions**:
   - [dotnet-architect](../agents/dotnet-architect.md) for backend module/structural decisions and strategic DDD (bounded context, cross-module integration mechanism).
   - [ddd-modeler](../agents/ddd-modeler.md) for the domain model of any backend feature that adds or reshapes one — aggregate boundaries, invariants, value objects, domain events. This is the default design step for backend domain work, not an afterthought.
   - [api-designer](../agents/api-designer.md) if new/changed backend API surface is involved.
   - [efcore-specialist](../agents/efcore-specialist.md) if data access is involved.
   - [nextjs-architect](../agents/nextjs-architect.md) for a client app's routing/data-fetching/state decisions.
4. **Settle the contract first** if the feature spans both stacks: agree the API shape (routes, DTOs, error cases) before writing client code against it — treat it as a small design step, not an afterthought.
5. **Produce a plan**: outline the approach, files to add/change on each side (naming which client app(s) if more than one is touched), and any API-contract implications. Present it before writing code (see [workflows/implement-feature.md](../workflows/implement-feature.md)).
6. **Wait for explicit approval** before implementing — this gate applies to every add/modify/feature request, not just large ones (see root `CLAUDE.md` §2.9).
7. **Implement incrementally**, delegating the code changes to [dotnet-developer](../agents/dotnet-developer.md) (backend) and [nextjs-developer](../agents/nextjs-developer.md) (client app), each only after the plan is approved: backend first (confirming it builds) then each client against the real contract, or vice versa — small, reviewable steps rather than one large change across the whole stack at once. If the plan included tests, write that test code as part of this step.
8. **Present the implemented code back to the user for review.** Stop here — don't automatically proceed to running tests or updating docs.
9. **Run tests / check coverage only when asked**: once the user has reviewed the code and explicitly requests it, run the automated test suite and/or use [testing-reviewer](../agents/testing-reviewer.md) to check coverage of edge cases.
10. **Verify the contract holds, if asked**: if both sides changed and the user wants it checked, run [api-contract-reviewer](../agents/api-contract-reviewer.md) to confirm every affected client actually matches what the backend now exposes.
11. **Docs**: update documentation only if the user requests it, as a separate follow-up (see [sync-docs](sync-docs.md)).

## Expected Outputs

- An approved implementation plan, agreed before any code was written.
- Working code changes scoped to the target module(s)/client app(s), presented back for review before tests/docs are touched.
- A clear note of any API contract added/changed.
- Test execution, contract verification, and documentation updates only once the user explicitly asks for each.

## Best Practices

- Don't introduce a new shared/building-blocks abstraction unless the feature genuinely needs to be reused across modules (backend) or across client apps.
- Keep the change scoped to the target module(s); if it turns out to require touching another module's internals, stop and confirm with the user first.
- Prefer extending existing patterns (in the module or in the client app) over inventing new ones.
- Don't let a client guess at a contract that hasn't been implemented yet — either the backend exists first, or the contract is explicitly agreed before client work starts.
- Never skip the plan-approval gate because a change "looks small" — the user asked for this checkpoint on every add/modify/feature request, not just big ones.
- Never chain straight from "implementation done" into running the test suite or updating docs — both wait for the user's explicit say-so after they've reviewed the code.
