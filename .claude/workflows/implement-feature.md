# Workflow: Implement Feature

Triggered by requests to add/implement new functionality or change existing code — in the solution's projects (framework, host, module) or their tests, in the client app under `clients/admin/`, or both. This is the canonical procedure for any code change; [create-feature](../skills/create-feature/SKILL.md) adds only what is specific to a feature spanning both stacks.

## Steps

1. **Confirm scope.** Identify which project(s) the change belongs in — framework (`Shared`, `Infrastructure`, `Persistence`, `EventBusMassTransitRabbitMQ`), host (`StarterKit.WebApi`, `StarterKit.AppHost`, `StarterKit.ServiceDefaults`), module (`Identity`, `Identity.Contracts`, `Identity.Web`, `Notifications`, `Notifications.Contracts`), tests (`tests/Framework.Tests`, `tests/Identity.Tests`, `tests/Notifications.Tests`), and/or the client app (`clients/admin/`, whose rules are in its own `CLAUDE.md`) — and, for a new building block, whether it belongs in the framework at all or stays in the owning module. Ask if ambiguous. Read only the files directly relevant (target project or feature, its direct dependencies, similar existing code as reference).
2. **Use appropriate agents** for design questions before planning:
   - [dotnet-architect](../agents/dotnet-architect.md) for project placement, extension points, and library choices.
   - [ddd-modeler](../agents/ddd-modeler.md) for any change to DDD building blocks (entity bases, value objects, domain events).
   - [api-designer](../agents/api-designer.md) if controller bases, endpoint registration, the response envelope, versioning, or an endpoint the client consumes change.
   - [efcore-specialist](../agents/efcore-specialist.md) if persistence behavior or provider support changes.
   - [nextjs-architect](../agents/nextjs-architect.md) for the client app's routing/data-fetching/state decisions.
3. **If the change spans backend and client**, settle the API contract first (routes, request/response types, error cases) — see [create-feature](../skills/create-feature/SKILL.md).
4. **Produce an implementation plan**: files to add/change on each side touched, approach, any public-API/API-contract/breaking-change implications for consuming modules or the client, and test strategy (what tests would be added, not run yet). Present it to the user.
5. **Wait for explicit approval** before writing any code. This gate applies to every code change, not just large ones — see root `CLAUDE.md` §2.9 and [AI_CONTEXT.md](../AI_CONTEXT.md) (Code-Change Workflow Gate).
6. **Implement incrementally**, delegating the code changes: backend → [dotnet-developer](../agents/dotnet-developer.md), client app → [nextjs-developer](../agents/nextjs-developer.md). Small, independently verifiable steps — confirm it builds at each step (basic sanity, not the test suite). If the plan called for new tests, writing that test code is part of this step.
7. **Present the implemented code back to the user for review.** Stop here — do not chain into running tests or updating docs.
8. **Run the test suite, and/or check coverage with [testing-reviewer](../agents/testing-reviewer.md), only when the user explicitly asks** in a follow-up instruction.
9. **If both stacks changed and the user asks to verify it**, run [api-contract-reviewer](../agents/api-contract-reviewer.md) to confirm the client matches the backend's final contract.
10. **Update documentation only if requested**, as a separate explicit instruction — offer to at the end (see [end-session](end-session.md)), but act only if the user says yes.

## Rules

- Never skip the plan-approval gate because a change "looks small", and never chain from "implementation done" into running the test suite or updating docs.
- Don't add a framework abstraction unless it is genuinely reused across modules.
- Prefer extending existing patterns (in the project or in the client app) over inventing new ones.

## Output

- An approved plan (surfaced to the user before any code was written).
- Working, incrementally built code, presented back for review before tests/docs are touched.
- A clear breaking-change flag if any public API or API contract changed.
