---
name: efcore
description: Playbook for EF Core work — entity/DbContext design, migration review, and query performance — using the efcore-specialist agent. One DbContext per module is the intended default.
---

# Skill: EF Core

## Purpose

Handle EF Core-specific tasks (model design, migration review, query performance) for a specific module's `DbContext`, respecting the Modular Monolith rule that each module owns its own tables.

## Inputs

- The target module/`DbContext` (ask if not specified — never assume "the" DbContext; the intended default is one per module).
- The specific task: design a new entity, review a migration, diagnose a slow query, or review existing configuration.

## Workflow

1. **Identify the DbContext in scope**: locate the owning module explicitly; don't assume there's only one context in the backend.
2. **Delegate**: invoke [efcore-specialist](../agents/efcore-specialist.md) with the specific task and scope.
3. **For migrations**: review the specific migration file(s) for destructive operations before considering them safe to apply.
4. **For performance**: get the actual query/LINQ shape from the user or the code, not a hypothetical.
5. **Watch for cross-module reach**: if a query needs data owned by another module, flag it — that should go through the owning module's contract, not a direct join across `DbContext`s/schemas.
6. **Report**: findings/design ranked by risk (data loss/boundary violation > performance > style), with concrete fixes.

## Expected Outputs

- A reviewed/designed entity, DbContext configuration, or migration.
- For performance tasks: a specific diagnosis and fix tied to the actual query.

## Best Practices

- Never apply migrations to a real database without explicit confirmation.
- Treat shared base entities in the building-blocks project as higher-risk to change — they may affect multiple modules' contexts.
- Don't recommend schema changes without noting migration impact for that module.
