---
name: nextjs
description: Playbook for Next.js/React/TypeScript structural work in the client app under clients/admin/ — route/feature structure, data-fetching pattern choice, component boundaries, and frontend performance — using the nextjs-architect agent.
---

# Skill: Next.js

## Purpose

Handle frontend structural tasks (new route/feature shape, data-fetching approach, state management choice, bundle/render concerns) in `clients/admin/`, keeping the app consistent with the patterns it already has rather than introducing a competing one per feature.

## Inputs

- The route/feature/area in scope (confirm the app with `Glob clients/*/` — today `clients/admin/` is the only one).
- The specific task: new route/page, new data-fetching need, component structure question, or a performance concern.

## Workflow

1. **Identify the area in scope**: which route under `src/app/` and which feature under `src/modules/` (or `src/features/`) this touches.
2. **Delegate**: invoke [nextjs-architect](../../agents/nextjs-architect.md) with the specific task and scope.
3. **For anything calling the backend**: confirm the contract with [api-designer](../../agents/api-designer.md) (new/changed endpoint) or [api-contract-reviewer](../../agents/api-contract-reviewer.md) (existing endpoint) rather than guessing the shape.
4. **Report**: the recommended structure/pattern with rationale. Any resulting code change goes through the plan-approval gate (root `CLAUDE.md` §2.9) and is implemented by [nextjs-developer](../../agents/nextjs-developer.md).

## Expected Outputs

- A concrete route/component/data-fetching design consistent with the app's existing conventions.
- For performance concerns: a specific diagnosis (unnecessary client bundle, waterfalled fetch, avoidable re-render) tied to the actual code.

## Best Practices

- Default to Server Components; justify each `"use client"` boundary.
- Don't introduce a second state-management/data-fetching library without a concrete reason and the user's agreement.
- Reuse the existing backend-client layer (`src/lib/server/`) rather than scattering ad hoc `fetch` calls.
