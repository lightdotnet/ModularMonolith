---
name: efcore
description: Playbook for EF Core work — the Persistence framework project (BaseDbContext, audit, domain-event dispatch, repositories, multi-provider support), entity-configuration conventions, migration review, and query performance — using the efcore-specialist agent.
---

# Skill: EF Core

## Purpose

Handle EF Core tasks for `src/Persistence` and the conventions it gives every module's own `DbContext`, keeping all supported providers working.

## Inputs

- The specific type/area in scope (base context, a builder extension, a repository, migration support, or a query).
- The task: design, review a migration, diagnose a slow query, or review configuration.

## Workflow

1. **Identify the scope** explicitly; don't assume a single repo-wide `DbContext`.
2. **Delegate**: invoke [efcore-specialist](../../agents/efcore-specialist.md) with the task and scope.
3. **For migrations**: review the specific migration file(s) for destructive operations before considering them safe to apply.
4. **For performance**: get the actual query/LINQ shape from the user or the code, not a hypothetical.
5. **Check every provider**: a change to base persistence behavior must hold for InMemory, PostgreSQL, MSSQL, and Sqlite.
6. **Report**: findings/design ranked by risk (data loss / provider breakage > performance > style), with concrete fixes.

## Expected Outputs

- A reviewed/designed persistence building block, configuration, or migration.
- For performance tasks: a specific diagnosis and fix tied to the actual query.

## Best Practices

- Never apply migrations to a real database without explicit confirmation.
- Treat `BaseDbContext` and shared base entities as high-risk to change — every module's context inherits them.
