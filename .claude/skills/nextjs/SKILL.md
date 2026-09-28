---
name: nextjs
description: Playbook for Next.js/React/TypeScript work — route/feature structure, data-fetching pattern choice, and component design — using the nextjs-architect agent.
---

# Skill: Next.js

## Purpose

Handle frontend structural tasks (new route/feature shape, data-fetching approach, state management choice) for a given app under `clients/<app-name>/`, keeping that app consistent with whatever pattern is already established rather than introducing a competing one per feature.

## Inputs

- The target client app and the route/feature/area under it (ask which app if `clients/` has more than one and it's not obvious).
- The specific task: new route/page, new data-fetching need, component structure question, or a performance concern (bundle size, hydration).

## Workflow

1. **Identify the app and area in scope**: which `clients/<app-name>/`, and which route/feature under its `app/` (or `pages/`) this touches.
2. **Delegate**: invoke [nextjs-architect](../agents/nextjs-architect.md) with the specific task and scope.
3. **Check existing patterns first**: look at that app's `lib/` (or equivalent) and neighboring routes/components before proposing a new data-fetching or state-management approach — reuse what's established in that app.
4. **For anything calling the backend**: confirm the API contract with [api-designer](../agents/api-designer.md)/[api-contract-reviewer](../agents/api-contract-reviewer.md) rather than guessing the shape.
5. **Report**: the recommended structure/pattern with rationale, and what (if anything) needs to change in shared frontend infrastructure (API client, layout, providers).

## Expected Outputs

- A concrete route/component/data-fetching design consistent with existing frontend conventions.
- For performance concerns: a specific diagnosis (unnecessary client bundle, missing memoization, waterfalled fetch) tied to the actual code.

## Best Practices

- Default to Server Components; justify each `"use client"` boundary.
- Don't introduce a second state-management/data-fetching library without a concrete reason and the user's agreement.
- Reuse the existing API client layer rather than writing ad hoc `fetch` calls scattered across components.
