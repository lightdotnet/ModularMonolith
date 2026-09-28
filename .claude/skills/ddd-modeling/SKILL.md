---
name: ddd-modeling
description: Playbook for designing or reviewing a backend module's domain model — aggregates, invariants, value objects, domain events — using the ddd-modeler agent.
---

# Skill: DDD Modeling

## Purpose

Handle tactical Domain-Driven Design for one module's domain layer: get aggregate boundaries and invariants right, keep behavior on the model rather than in handlers, and use domain events for anything that crosses an aggregate or module boundary.

## Inputs

- The target module and the aggregate/feature in scope (ask if not specified — model one aggregate at a time).
- The specific task: design a new aggregate, place a new business rule, or review an existing `Domain/` folder.

## Workflow

1. **Scope**: name the module and the single aggregate/feature. Don't model the whole module at once.
2. **Delegate**: invoke [ddd-modeler](../agents/ddd-modeler.md) with the task and scope.
3. **Persistence check**: if the model changes the schema, hand the shape to [efcore-specialist](../agents/efcore-specialist.md) for mapping — separate step, model first.
4. **Layer-direction check**: if the change adds a dependency out of the domain, have [architecture-reviewer](../agents/architecture-reviewer.md) confirm the direction still holds.
5. **Cross-module reach**: if a rule needs another module's data, stop — route it through that module's `Contracts` seam (a DI interface or a denormalized snapshot label), never a direct domain reference.
6. **Report**: design or findings ranked — invariant/boundary correctness > model expressiveness > naming/style.

## Expected Outputs

- An aggregate map (roots, boundaries, invariants) in the [domain-model template](../docs/templates/domain-model.md) shape, or a reviewed `Domain/` folder with ranked findings.
- A clear statement of where each business rule lives and why.

## Best Practices

- Keep logic on the aggregate, not the command handler or a service class.
- A value object for anything with no identity of its own; don't over-formalize plain enums.
- No Specification for a single-use by-id lookup — inline `Where`/`FirstOrDefaultAsync` instead.
- Cross-aggregate consistency is eventual (a domain event), not a single transaction.
- This is a design skill — code changes still go through the plan-approval gate.
