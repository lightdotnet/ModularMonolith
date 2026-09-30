---
name: api
description: Playbook for designing or reviewing the framework's HTTP API conventions (controller bases, endpoint registration, response envelope, versioning, auth attributes) and the endpoints built on them using the api-designer agent, with explicit breaking-change awareness for consuming modules and the client app.
---

# Skill: API

## Purpose

Handle API design/review tasks for the conventions in `src/Infrastructure/Endpoints` and `src/Infrastructure/Modularity` and the module endpoints built on them, with explicit attention to backward compatibility — every module's HTTP contract is built on them, and the client app under `clients/admin/` consumes those contracts directly.

## Inputs

- The specific convention/endpoint in question.
- Whether this is new design or a review of an existing/proposed change.

## Workflow

1. **Scope**: confirm the target and whether this is new design or review of a change.
2. **Delegate**: invoke [api-designer](../../agents/api-designer.md) with the scoped contract/convention.
3. **Classify changes**: for any change to an existing convention or endpoint, explicitly classify it as additive or breaking before proceeding.
4. **Check client impact**: for a change to an existing endpoint, delegate to [api-contract-reviewer](../../agents/api-contract-reviewer.md) to find the actual client call sites affected; cross-cutting contract facts are in root `docs/integration.md`.
5. **Report**: proposed/reviewed design with rationale and explicit breaking-change flags, including what consuming modules or the client would need to change if breaking.

## Expected Outputs

- A concrete design or review, with a clear breaking/additive classification for any change.
- The affected client call sites, if the change touches an existing endpoint.

## Best Practices

- Always flag breaking changes explicitly — never let one slip through as "just a small tweak."
- Don't design speculative future endpoints beyond what's requested.
- Match existing conventions over generic REST best practices when they conflict.
