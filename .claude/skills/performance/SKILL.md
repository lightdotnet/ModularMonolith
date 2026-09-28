---
name: performance
description: Playbook for diagnosing and fixing performance issues (allocations, async misuse, hot paths, frontend bundle/render issues) using performance-reviewer, efcore-specialist, or nextjs-architect.
---

# Skill: Performance

## Purpose

Diagnose and address concrete performance issues in scoped code — never a speculative repo-wide performance pass.

## Inputs

- The specific code/symptom (e.g. "this endpoint is slow," "this page re-renders too much").
- Any available profiling/benchmark data, if the user has it.

## Workflow

1. **Scope**: identify the specific hot path or symptom in question, and which side of the stack it's on.
2. **Delegate diagnosis**:
   - [performance-reviewer](../agents/performance-reviewer.md) for backend CLR/async/algorithmic issues.
   - [efcore-specialist](../agents/efcore-specialist.md) if the bottleneck is a database query.
   - [nextjs-architect](../agents/nextjs-architect.md) for frontend bundle size, hydration cost, or re-render issues.
3. **Distinguish measured vs. theoretical**: prefer fixes backed by actual profiling/benchmark evidence; flag unmeasured suggestions as such.
4. **Weigh readability**: avoid micro-optimizations that meaningfully hurt clarity for marginal, unmeasured gains.
5. **Report/implement**: present the diagnosis and fix; implement only after the user agrees the tradeoff is worth it.

## Expected Outputs

- A concrete diagnosis tied to the actual code (file:line), not a generic checklist.
- A fix with honestly-described expected impact.

## Best Practices

- Don't optimize cold/rarely-called code.
- Don't run load tests against shared/production infrastructure without explicit confirmation.
- Prefer measured evidence over intuition when recommending a change.
