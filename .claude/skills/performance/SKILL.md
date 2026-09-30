---
name: performance
description: Playbook for diagnosing and fixing performance issues (allocations, async misuse, hot paths, per-request framework overhead, EF Core queries, client-app bundle/render cost) using performance-reviewer, efcore-specialist, or nextjs-architect.
---

# Skill: Performance

## Purpose

Diagnose and address concrete performance issues in scoped code — never a speculative solution-wide performance pass.

## Inputs

- The specific code/symptom (e.g. "this pipeline behaviour adds latency," "this query is slow," "this admin page re-renders too much").
- Any available profiling/benchmark data, if the user has it.

## Workflow

1. **Scope**: identify the specific hot path or symptom in question, and which side (backend or client app) it's on.
2. **Delegate diagnosis**:
   - [performance-reviewer](../../agents/performance-reviewer.md) for CLR/async/algorithmic issues.
   - [efcore-specialist](../../agents/efcore-specialist.md) if the bottleneck is a database query or persistence hook.
   - [nextjs-architect](../../agents/nextjs-architect.md) for client-app bundle size, hydration cost, waterfalled fetches, or re-render issues.
3. **Distinguish measured vs. theoretical**: prefer fixes backed by actual profiling/benchmark evidence; flag unmeasured suggestions as such.
4. **Weigh readability**: avoid micro-optimizations that meaningfully hurt clarity for marginal, unmeasured gains.
5. **Report**: present the diagnosis and fix; any code change goes through the plan-approval gate (root `CLAUDE.md` §2.9).

## Expected Outputs

- A concrete diagnosis tied to the actual code (file:line), not a generic checklist.
- A fix with honestly described expected impact.

## Best Practices

- Don't optimize cold/rarely-called code.
- Don't run load tests against shared/production infrastructure without explicit confirmation.
