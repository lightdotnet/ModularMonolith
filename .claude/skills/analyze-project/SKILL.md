---
name: analyze-project
description: Playbook for analyzing a single named backend project (.csproj) — e.g. one layer of one module (Domain/Application/Infrastructure/Api) — its responsibility, public surface, and dependencies, without expanding scope to the whole module or solution.
---

# Skill: Analyze Project

## Purpose

Build (or refresh) an understanding of exactly one backend project: what it's responsible for, what it exposes publicly, and what it depends on. Narrower than [analyze-module](../analyze-module/SKILL.md) (a whole module) and [analyze-solution](../analyze-solution/SKILL.md) (the whole backend).

## Inputs

- The specific `.csproj` or project name to analyze (ask if ambiguous — split modules each have a similarly-named layer, e.g. an `Application` project). This skill applies to Clean-Architecture-split modules only; for a single-project module (all current modules) use [analyze-module](../analyze-module/SKILL.md) instead.

## Workflow

1. **Locate the project**: find the `.csproj` file and its containing folder (which module and layer it belongs to).
2. **Read its references**: `<ProjectReference>` and `<PackageReference>` entries — delegate to [dependency-analyzer](../../agents/dependency-analyzer.md) if the graph is non-trivial, and note if it references anything outside its own module (a boundary flag for [architecture-reviewer](../../agents/architecture-reviewer.md)).
3. **Read its public surface**: enumerate public types/members at a summary level (don't paste full file contents unless asked).
4. **Identify responsibility**: infer the project's purpose from its actual contents (namespaces, key types), not its name/layer alone.
5. **Update docs, if requested**: write/update the module's row in `src/CLAUDE.md` (Modules table) and/or `src/docs/architecture/modules/<ModuleName>/<ProjectName>/overview.md` (the nested per-project form that exists only for a split module) using the [project-overview template](../../docs/templates/project-overview.md). Only when explicitly asked.

## Expected Outputs

- A description of the project's responsibility, public surface, and dependencies.
- Optionally, updated documentation (only if requested).

## Best Practices

- Stay within the named project; note references to other projects but don't fully analyze them unless asked.
- Summarize public surface rather than reproducing full source.
- Don't assume the project's role from its name/layer alone — verify against actual contents.
