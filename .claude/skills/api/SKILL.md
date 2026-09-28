---
name: api
description: Playbook for designing or reviewing the backend's REST API surface using the api-designer agent, with explicit breaking-change awareness for the client app(s) that consume it.
---

# Skill: API

## Purpose

Handle API design/review tasks (REST contracts, DTOs, error shape) for a specific backend module's `Api` project, with explicit attention to backward compatibility since the Next.js frontend depends directly on this contract.

## Inputs

- The target module and the specific endpoint/contract in question.
- Whether this is new API design or a review of an existing/proposed change.

## Workflow

1. **Scope**: confirm the target module and whether this is new design or review of a change.
2. **Delegate**: invoke [api-designer](../agents/api-designer.md) with the scoped contract/API.
3. **Classify changes**: for any change to an existing endpoint, explicitly classify as additive or breaking before proceeding.
4. **Check conventions**: compare against existing conventions across modules (naming, versioning, error shape) via `src/docs/conventions/coding-conventions.md` (§ API Conventions) if populated.
5. **Check frontend impact**: for changes to an existing endpoint, delegate to [api-contract-reviewer](../agents/api-contract-reviewer.md) to find actual frontend call sites affected.
6. **Report**: proposed/reviewed contract with rationale and explicit breaking-change flags, including what each affected client app needs to change if breaking.

## Expected Outputs

- A concrete contract design or review, with a clear breaking/additive classification for any change.
- A list of frontend call sites affected, if the change touches an existing endpoint.

## Best Practices

- Always flag breaking changes explicitly — never let one slip through as "just a small tweak."
- Don't design speculative future endpoints beyond what's requested.
- Match existing conventions across modules over generic REST best practices when they conflict.
