---
name: review-architecture
description: Playbook for reviewing layering, dependency direction, and cohesion of the framework, host, and module projects under src/ (or the whole solution) using architecture-reviewer.
---

# Skill: Review Architecture

## Purpose

Assess the structural health of one project under `src/` (framework, host, or module) or the whole solution — layering, dependency direction, shared-kernel cohesion, module boundaries — without assuming structure that hasn't been verified.

## Inputs

- The target scope: one project or the whole solution (ask if not specified).
- Existing `docs/architecture/architecture.md` and `docs/architecture/dependency-graph.md`, if generated, as a baseline.

## Workflow

1. **Scope**: confirm the exact project(s) to review.
2. **Delegate**: invoke [architecture-reviewer](../../agents/architecture-reviewer.md).
3. **Verify, don't assume**: the dependency picture must come from actual `.csproj`/`.slnx` references, not folder-name conventions.
4. **Report**: findings ranked by severity, each tied to a concrete file/reference.
5. **Optionally persist**: if the user asks to record the findings in docs, hand off to [sync-docs](../sync-docs/SKILL.md) — never update docs automatically.

## Expected Outputs

- A scoped architectural assessment: dependency direction, layering observations, boundary issues.
- Prioritized findings with rationale and suggested fixes.

## Best Practices

- Never expand the review to sibling projects without flagging it first.
- Don't propose a full re-architecture unless asked.
- This is a read-only skill — no code changes.
