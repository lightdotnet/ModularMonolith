---
name: api
description: Playbook for designing or reviewing the framework's HTTP API conventions (controller bases, endpoint registration, response envelope, versioning, auth attributes) using the api-designer agent, with explicit breaking-change awareness.
---

# Skill: API

## Purpose

Handle API design/review tasks for the conventions in `src/Infrastructure/Endpoints` and `src/Infrastructure/Modularity`, with explicit attention to backward compatibility — every module's HTTP contract is built on them.

## Inputs

- The specific convention/endpoint in question.
- Whether this is new design or a review of an existing/proposed change.

## Workflow

1. **Scope**: confirm the target and whether this is new design or review of a change.
2. **Delegate**: invoke [api-designer](../../agents/api-designer.md) with the scoped contract/convention.
3. **Classify changes**: for any change to an existing convention, explicitly classify it as additive or breaking before proceeding.
4. **Report**: proposed/reviewed design with rationale and explicit breaking-change flags, including what consuming modules/clients would need to change if breaking.

## Expected Outputs

- A concrete design or review, with a clear breaking/additive classification for any change.

## Best Practices

- Always flag breaking changes explicitly — never let one slip through as "just a small tweak."
- Don't design speculative future endpoints beyond what's requested.
- Match existing conventions over generic REST best practices when they conflict.
