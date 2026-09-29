---
name: generate-docs
description: Playbook for generating new documentation from code for a specific scope of the solution, using the templates in .claude/docs/templates/.
---

# Skill: Generate Docs

## Purpose

Produce new documentation for a specific project or the solution that doesn't yet have generated docs, using the standard templates — never speculative.

## Inputs

- The target scope (the solution or one project).
- Which doc type is wanted (solution overview, project overview, architecture, database, domain model, coding conventions, dependency graph, development guide) — see [docs/templates/](../../docs/templates/).

## Workflow

1. **Scope and doc type**: confirm exactly what's being documented and which template applies.
2. **Read the template**: load the matching file from `.claude/docs/templates/` for structure.
3. **Delegate**: invoke [documentation-writer](../../agents/documentation-writer.md) with the scope and template.
4. **Verify facts**: every claim must trace to actual code inspected for this scope — mark anything unverifiable as `unknown` rather than guessing.
5. **Write output**: place the result as a flat file under `src/docs/architecture/<doc-type>.md` (overview, architecture, database, dependency-graph, per-project overviews) or `src/docs/conventions/<doc-type>.md` (coding-conventions, development-guide).
6. **Report**: summarize what was generated and flag any gaps found.

## Expected Outputs

- A new markdown file under `src/docs/` following the template structure, populated only with verified facts.
- A short summary of what was generated and any open questions.

## Best Practices

- Don't generate docs for scopes not requested, even if adjacent.
- If a doc already exists for this scope, use [sync-docs](../sync-docs/SKILL.md) instead of overwriting it.
- Follow the doc-writing rules in [sync-documentation](../../workflows/sync-documentation.md) — they apply to a first-time write too.
