# Workflow: Implement Feature

Triggered by requests to add/implement new functionality, on the backend, one or more client apps, or both.

## Steps

1. **Read relevant documentation.** Check `src/CLAUDE.md`/`src/docs/` for backend module(s) and/or `clients/<app-name>/CLAUDE.md`/`clients/<app-name>/docs/` for client app(s) — only the sections relevant to this feature's scope. Use `/context-backend`, `/context-frontend`, or `/context-full` ([commands/](../commands/)) to load the right set.
2. **Confirm scope.** Identify which module(s) and/or client app(s) the feature belongs in, and whether it spans both stacks. Ask if ambiguous — including which app(s), once `clients/` has more than one.
3. **Use appropriate agents** for design questions before planning:
   - [dotnet-architect](../agents/dotnet-architect.md) for backend module/structural decisions and strategic DDD (bounded context, cross-module integration mechanism).
   - [ddd-modeler](../agents/ddd-modeler.md) for the domain model of any backend feature that adds or reshapes one — aggregate boundaries, invariants, value objects, domain events. This is the default design step for backend domain work, not an afterthought.
   - [api-designer](../agents/api-designer.md) if new/changed backend API surface is involved.
   - [efcore-specialist](../agents/efcore-specialist.md) if data access/schema changes are involved.
   - [nextjs-architect](../agents/nextjs-architect.md) for a client app's routing/data-fetching/state decisions.
4. **If the feature spans both stacks, settle the API contract first** (routes, DTOs, error cases) before client implementation begins — see [create-feature skill](../skills/create-feature.md).
5. **Produce an implementation plan**: files to add/change on each side touched, approach, any API-contract/breaking-change implications, and test strategy (what tests would be added, not run yet). Present it to the user.
6. **Wait for explicit approval** before writing any code. This gate applies to every code-add/modify/feature request, not just large ones — see root `CLAUDE.md` §2.9 and [AI_CONTEXT.md](../AI_CONTEXT.md) (Code-Change Workflow Gate).
7. **Implement incrementally**, delegating the actual code changes: backend code → [dotnet-developer](../agents/dotnet-developer.md), client-app code → [nextjs-developer](../agents/nextjs-developer.md) (each runs only now, after the plan is approved). Small, independently verifiable steps — confirm it builds at each step (basic sanity, not the test suite) rather than one large change across both stacks at once. If the plan called for new tests, writing that test code is part of this step.
8. **Present the implemented code back to the user for review.** Stop here — do not chain automatically into running tests or updating docs.
9. **Run the automated test suite, and/or check coverage with [testing-reviewer](../agents/testing-reviewer.md), only when the user explicitly asks for it** in a follow-up instruction after reviewing the code.
10. **If both stacks changed and the user asks to verify it**, run [api-contract-reviewer](../agents/api-contract-reviewer.md) to confirm every affected client app actually matches the backend's final contract.
11. **Update documentation only if requested**, as a separate explicit instruction from the same user review step. Do not update `src/docs/**`, `clients/<app-name>/docs/**`, `src/CLAUDE.md`, or `clients/<app-name>/CLAUDE.md` automatically — offer to, at the end (see [end-session](end-session.md)), but only act if the user says yes.

## Output

- An approved plan (surfaced to the user before any code was written).
- Working, incrementally-built code, on whichever side(s) of the stack the feature required, presented back for review before tests/docs are touched.
- A clear breaking-change flag if any module contract or API surface changed.
- Test-suite execution, contract verification, and documentation updates only when the user explicitly asks for each, after reviewing the code.
