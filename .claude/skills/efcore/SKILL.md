---
name: efcore
description: Playbook for EF Core work — the Persistence framework project (BaseDbContext, audit, domain-event dispatch, repositories, multi-provider support), module DbContexts, per-provider migrations, entity-configuration conventions, migration review, and query performance — using the efcore-specialist agent.
---

# Skill: EF Core

## Purpose

Handle EF Core tasks for `src/Persistence`, the module contexts built on its conventions (e.g. Identity's `IdentityDbContext`), and the per-provider migrators, keeping all supported providers working.

## Inputs

- The specific type/area in scope (base context, a builder extension, a repository, migration support, a module context, a migration, or a query).
- The task: design, review a migration, diagnose a slow query, or review configuration.

## Workflow

1. **Identify the scope** explicitly; don't assume a single repo-wide `DbContext`. Candidate locations: `src/Persistence` (tests in `tests/Framework.Tests/Persistence/`), a module's context such as `src/Identity/Infrastructure/Persistence/` (tests in `tests/Identity.Tests/Infrastructure/Persistence/`), and the migrators under `src/Migrations/*` (workflow in [migrations.md](../../../docs/conventions/migrations.md)).
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
- Treat `BaseDbContext` and shared base entities as high-risk to change — module contexts inherit them or reuse their extensions (Identity's context derives from the ASP.NET Core Identity context and reuses Persistence's audit/dispatch extensions).
