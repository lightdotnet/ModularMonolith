---
name: clean-architecture-split
description: Playbook for deciding whether a single-project backend module has outgrown its shape and, if so, executing the split into <Module>.{Domain,Application,Infrastructure,Api} — using dotnet-architect then architecture-reviewer.
---

# Skill: Clean Architecture Split

## Purpose

Move one backend module from a single `<Module>.Api` project to the four-project Clean Architecture layout the convention allows but no module currently uses. Both the decision (is it warranted?) and the execution (what moves where) run through here.

## Inputs

- The target module and why a split is being considered (e.g. the `Domain/` folder is large and independently testable, `Application/` handlers are hard to isolate from EF Core, a second delivery mechanism is coming).
- Confirmation the single-project default genuinely no longer fits — the bar is high; every module ships single-project today and that stays the default.

## Workflow

1. **Justify or stop**: with [dotnet-architect](../agents/dotnet-architect.md), decide whether the split earns its cost. If not, stop here — a well-organized single project is the norm.
2. **Carve-up plan** (dotnet-architect produces the exact move list):
   - `<Module>.Domain` ← entities, value objects, domain events, domain services, specifications.
   - `<Module>.Application` ← commands/queries + handlers, validators, application-service interfaces.
   - `<Module>.Infrastructure` ← the `DbContext`, EF configuration, external adapters.
   - `<Module>.Api` ← controllers, DI composition (`<Module>Module.cs`).
   - `<Module>.Contracts` **does not move** — it stays the sole cross-module seam.
3. **Wire updates**: list every `ProjectReference` change — `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` point at the project holding the `DbContext` (now `<Module>.Infrastructure`), the composition-root host references `<Module>.Api`, `<Module>.Tests` `InternalsVisibleTo` follows the internal types.
4. **Plan-approval gate**: a structural refactor touching many files — present the full plan and wait for explicit approval (root `CLAUDE.md` §2.7 / §2.9).
5. **Execute incrementally**, building between steps.
6. **Validate direction**: run [architecture-reviewer](../agents/architecture-reviewer.md) — `Api → Application → Domain`, `Infrastructure → Application, Domain`, nothing points back into `Api`.

## Expected Outputs

- A go/no-go decision with rationale, or (if go) an approved carve-up plan executed with dependency direction verified.
- Updated `src/CLAUDE.md` Modules table and the module's own doc, only if the user asks for the doc sync.

## Best Practices

- The default is single-project — split only a module that has genuinely outgrown it, never preemptively or "for consistency."
- Never let the split change the module's public contract (`<Module>.Contracts`) or its HTTP surface — it's an internal restructure.
- Don't mix the split with a feature change or a domain-model redesign — one structural move at a time.
