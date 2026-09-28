# Workflow: Review Repository

Triggered by broad, read-only review requests ("review the codebase," "audit this repo").

## Steps

1. **Confirm scope.** Even a "repository review" should be scoped to what's practical and relevant — confirm whether the user means the backend, one or more client apps, or all of it (this repo has one backend solution but potentially several client apps under `clients/` — `Glob clients/*/` first so "the whole repo" isn't silently read as "just one app").
2. **Use multiple agents**, each covering its domain, over the confirmed scope:
   - Backend: [architecture-reviewer](../agents/architecture-reviewer.md), [code-reviewer](../agents/code-reviewer.md), [efcore-specialist](../agents/efcore-specialist.md) (if data access in scope), [api-designer](../agents/api-designer.md) (if API surface in scope).
   - Each client app in scope: [nextjs-architect](../agents/nextjs-architect.md), [frontend-code-reviewer](../agents/frontend-code-reviewer.md) — run per app, don't assume findings from one app apply to another.
   - Both: [security-reviewer](../agents/security-reviewer.md), [performance-reviewer](../agents/performance-reviewer.md), [testing-reviewer](../agents/testing-reviewer.md), [dependency-analyzer](../agents/dependency-analyzer.md).
   - If both stacks are in scope: [api-contract-reviewer](../agents/api-contract-reviewer.md) to check each client is actually in sync with the backend.
3. **Produce prioritized findings**: merge each agent's output into one report, ordered by severity/impact across domains (e.g. security/correctness issues before style nits).
4. **Do not modify code.** This workflow is strictly read-only/advisory — findings are reported, not applied.
5. **Offer next steps**: suggest which findings might warrant a follow-up (e.g. [refactor](../skills/refactor/SKILL.md), [implement-feature](implement-feature.md) for fixes) without applying them automatically.

## Output

- A single, prioritized findings report spanning the agents used, each finding attributed to its domain (backend/named client app/integration) with file references.
- No code changes.
- Suggested follow-up actions, left for the user to approve.
