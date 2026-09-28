---
name: review-architecture
description: Playbook for reviewing backend layering/module boundaries or frontend structure using architecture-reviewer or nextjs-architect.
---

# Skill: Review Architecture

## Purpose

Assess the structural health of a specific backend module (or the whole backend solution) or a frontend area — layering, dependency direction, boundary cohesion — without assuming uniform structure across modules.

## Inputs

- The target scope: a backend module/solution, or a frontend area (ask if not specified).
- Existing `src/docs/architecture/architecture.md` (backend scope) or `clients/<app-name>/docs/architecture/architecture.md` (frontend scope) content, if any, as a baseline to compare against.

## Workflow

1. **Scope**: confirm the exact module/solution/frontend area to review.
2. **Delegate**: invoke [architecture-reviewer](../agents/architecture-reviewer.md) for backend scope, or [nextjs-architect](../agents/nextjs-architect.md) for frontend scope.
3. **Verify, don't assume**: the agent should build its dependency picture from actual `.csproj`/`.sln` references (backend) or actual imports/route structure (frontend), not folder-name conventions.
4. **Report**: present findings ranked by severity, each tied to a concrete file/reference.
5. **Optionally persist**: if the user asks to update documentation with the findings, hand off to [sync-docs](sync-docs.md) — do not update `src/docs/architecture/architecture.md`/`clients/<app-name>/docs/architecture/architecture.md` automatically.

## Expected Outputs

- A scoped architectural assessment: dependency direction, layering observations, boundary issues.
- Prioritized findings with rationale and suggested fixes.
- Explicit note of anything out of scope that would need a follow-up review.

## Best Practices

- Never expand the review to sibling modules or client apps without flagging it first.
- Don't propose a full re-architecture unless asked; report findings and let the user decide.
- This is a read-only skill — no code changes.
