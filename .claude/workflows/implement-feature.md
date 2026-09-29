# Workflow: Implement Feature

Triggered by requests to add/implement new functionality or change existing code in the framework projects or their tests. This is the canonical procedure for any code change.

## Steps

1. **Confirm scope.** Identify which project(s) the change belongs in (`Shared`, `Infrastructure`, `Persistence`, `tests/Framework.Tests`) and whether it belongs in the framework at all. Ask if ambiguous. Read only the files directly relevant (target project, its direct dependencies, similar existing types as reference).
2. **Use appropriate agents** for design questions before planning:
   - [dotnet-architect](../agents/dotnet-architect.md) for project placement, extension points, and library choices.
   - [ddd-modeler](../agents/ddd-modeler.md) for any change to DDD building blocks (entity bases, value objects, domain events).
   - [api-designer](../agents/api-designer.md) if controller bases, endpoint registration, the response envelope, or versioning change.
   - [efcore-specialist](../agents/efcore-specialist.md) if persistence behavior or provider support changes.
3. **Produce an implementation plan**: files to add/change, approach, any public-API/breaking-change implications for consuming modules, and test strategy (what tests would be added, not run yet). Present it to the user.
4. **Wait for explicit approval** before writing any code. This gate applies to every code change, not just large ones — see root `CLAUDE.md` §2.9 and [AI_CONTEXT.md](../AI_CONTEXT.md) (Code-Change Workflow Gate).
5. **Implement incrementally**, delegating the code changes to [dotnet-developer](../agents/dotnet-developer.md). Small, independently verifiable steps — confirm it builds at each step (basic sanity, not the test suite). If the plan called for new tests, writing that test code is part of this step.
6. **Present the implemented code back to the user for review.** Stop here — do not chain into running tests or updating docs.
7. **Run the test suite, and/or check coverage with [testing-reviewer](../agents/testing-reviewer.md), only when the user explicitly asks** in a follow-up instruction.
8. **Update documentation only if requested**, as a separate explicit instruction — offer to at the end (see [end-session](end-session.md)), but act only if the user says yes.

## Rules

- Never skip the plan-approval gate because a change "looks small", and never chain from "implementation done" into running the test suite or updating docs.
- Don't add a framework abstraction unless it is genuinely reused across modules.
- Prefer extending existing patterns over inventing new ones.

## Output

- An approved plan (surfaced to the user before any code was written).
- Working, incrementally built code, presented back for review before tests/docs are touched.
- A clear breaking-change flag if any public API changed.
