---
name: ddd-modeling
description: Playbook for designing or reviewing DDD building blocks in src/Shared (entity bases, value objects, domain events) or a domain model built on them — aggregates, invariants, value objects, domain events — using the ddd-modeler agent.
---

# Skill: DDD Modeling

## Purpose

Handle tactical Domain-Driven Design: keep the shared building blocks in `src/Shared` sound, get aggregate boundaries and invariants right in any model built on them, keep behavior on the model rather than in handlers, and use domain events for anything that crosses an aggregate or module boundary.

## Inputs

- The building block or aggregate in scope (ask if not specified — one at a time).
- The specific task: design a new building block/aggregate, place a new business rule, or review existing types.

## Workflow

1. **Scope**: name the single building block or aggregate. Don't model everything at once.
2. **Delegate**: invoke [ddd-modeler](../../agents/ddd-modeler.md) with the task and scope.
3. **Persistence check**: if the change affects how entities, audit fields, or domain events are persisted, hand the shape to [efcore-specialist](../../agents/efcore-specialist.md) — separate step, model first.
4. **Direction check**: if the change adds a dependency out of `Shared`, have [architecture-reviewer](../../agents/architecture-reviewer.md) confirm the direction still holds.
5. **Report**: design or findings ranked — invariant/boundary correctness > model expressiveness > naming/style.

## Expected Outputs

- An aggregate map or building-block design in the [domain-model template](../../docs/templates/domain-model.md) shape, or ranked review findings.
- A clear statement of where each business rule lives and why.

## Best Practices

- Keep logic on the aggregate, not the command handler or a service class.
- A value object for anything with no identity of its own; don't over-formalize plain enums.
- Cross-aggregate consistency is eventual (a domain event), not a single transaction.
- This is a design skill — code changes still go through the plan-approval gate.
