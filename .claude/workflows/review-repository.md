# Workflow: Review Repository

Triggered by broad, read-only review requests ("review the codebase," "audit this repo").

## Steps

1. **Confirm scope.** Even a "repository review" should be scoped to what's practical — confirm whether the user means one project (framework, host, or module), the tests, the whole solution, the client app (`clients/admin/`), or all of it.
2. **Use multiple agents**, each covering its domain, over the confirmed scope:
   - Backend: [architecture-reviewer](../agents/architecture-reviewer.md), [code-reviewer](../agents/code-reviewer.md), [dependency-analyzer](../agents/dependency-analyzer.md); [efcore-specialist](../agents/efcore-specialist.md) if `Persistence` is in scope; [api-designer](../agents/api-designer.md) if the endpoint conventions are in scope; [ddd-modeler](../agents/ddd-modeler.md) if the DDD building blocks are in scope.
   - Client app: [nextjs-architect](../agents/nextjs-architect.md), [frontend-code-reviewer](../agents/frontend-code-reviewer.md).
   - Either side: [security-reviewer](../agents/security-reviewer.md), [performance-reviewer](../agents/performance-reviewer.md), [testing-reviewer](../agents/testing-reviewer.md).
   - Both sides in scope: [api-contract-reviewer](../agents/api-contract-reviewer.md) to check the client is in sync with the backend.
3. **Produce prioritized findings**: merge each agent's output into one report, ordered by severity/impact across domains (security/correctness before style nits).
4. **Do not modify code.** This workflow is strictly read-only/advisory.
5. **Offer next steps**: suggest which findings warrant a follow-up (e.g. [refactor](../skills/refactor/SKILL.md), [implement-feature](implement-feature.md)) without applying them.

## Output

- A single, prioritized findings report, each finding attributed to its domain (backend / client app / integration) with file references.
- No code changes.
- Suggested follow-up actions, left for the user to approve.
