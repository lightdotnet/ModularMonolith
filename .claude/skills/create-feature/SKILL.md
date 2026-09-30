---
name: create-feature
description: Playbook for a feature that spans a backend module (src/) and the client app (clients/admin/) — settle the API contract first, then coordinate backend and client work. The shared plan → approval → implement → review procedure lives in workflows/implement-feature.md.
---

# Skill: Create Feature

## Purpose

Cover what is specific to a full-stack feature: a backend endpoint plus the client UI that calls it, planned together so the contract between them is settled before code is written. Everything else — design-agent selection, the plan → approval gate, implementation, presenting the code back, and tests/contract checks/docs only on explicit request — follows [implement-feature](../../workflows/implement-feature.md) and is not repeated here.

## Inputs

- A description of the desired feature/behavior.
- Which backend module(s) and which client app(s) it touches — confirm rather than assume.

## Workflow

1. **Run [implement-feature](../../workflows/implement-feature.md) steps 1–2** (scope, design agents).
2. **Settle the contract first**: agree the API shape (routes, request/response types, error cases) with [api-designer](../../agents/api-designer.md) before any client code is planned against it. For a change to an existing endpoint, classify it as additive or breaking and list the affected client call sites.
3. **Plan both sides together**: the plan (implement-feature step 4) names the backend files and the client files, the agreed contract, and the order of work.
4. **Coordinate implementation** (after approval): backend first via [dotnet-developer](../../agents/dotnet-developer.md), confirmed to build, then the client against the real contract via [nextjs-developer](../../agents/nextjs-developer.md) — or client first only against the explicitly agreed contract. Keep steps small and reviewable.
5. **Continue with [implement-feature](../../workflows/implement-feature.md) steps 7–10** (present the code; tests, [api-contract-reviewer](../../agents/api-contract-reviewer.md), and docs only when the user explicitly asks).

## Expected Outputs

- An agreed API contract, recorded in the approved plan before any client code was written.
- Backend and client changes that match that contract, presented back for review per implement-feature.

## Best Practices

- Don't let a client guess at a contract that hasn't been implemented yet — either the backend exists first, or the contract is explicitly agreed before client work starts.
- A contract change ripples into every client that consumes the endpoint — see [AI_CONTEXT.md § Full-Stack Safety Rules](../../AI_CONTEXT.md#full-stack-safety-rules).
